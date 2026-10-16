# Controle de runtime

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Tradução da [página original em inglês](../../../reference/runtime-control.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O controle de runtime é o conjunto de operações de leitura, escrita e ação que o OmsiLaunch executa dentro de uma sessão do OMSI em execução. Esta página explica as três formas de acessá-lo (API do proprietário, cliente da CLI, plano de controle local), como as requisições vão do chamador até o plugin e voltam, como funcionam os handles e as identidades de requisição, quais timeouts se aplicam, o que deliberadamente não é registrado no journal e as convenções de argumentos e resultados, com um exemplo prático para cada família. O inventário das operações em si, com argumentos, chaves de resultado e erros, está em [capacidades](capabilities.md). Fontes: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` e `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` e `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Três pontos de entrada

| Ponto de entrada | Quem | Caminho | Timeout |
| --- | --- | --- | --- |
| API do proprietário | Um integrador que iniciou a sessão no próprio processo com `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> validação pelo registro -> consulta da sessão -> mailbox | `TimeSpan` fornecido pelo chamador |
| CLI do proprietário (`/runtime:<op>`) | O processo `OmsiLaunch.exe` que é proprietário da sessão | uma operação executada logo após `Running`, com o resultado gravado em `.omsilaunch\diagnostics\<session>-runtime-operation.json` e no console; a sessão continua | 5 s (15 s para `road-vehicles.spawn`) |
| Cliente da CLI | Qualquer invocação de `OmsiLaunch.exe` **sem** argumento de instalação, por exemplo `OmsiLaunch.exe time get` | named pipe `runtime.execute` até o proprietário da instalação em que o executável se encontra -> o proprietário chama `ExecuteRuntimeAsync` | 8 s (30 s para `road-vehicles.spawn`) tanto no lado do cliente quanto no do proprietário |
| Plano de controle local | Qualquer processo do mesmo usuário do Windows | o mesmo protocolo de pipe do cliente da CLI; veja [controle local](local-control.md) | como acima |

Todo caminho termina em `ExecuteRuntimeAsync`, que impõe a fronteira pública nesta ordem:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: uma operação que não está em `PublicRuntimeOperationIds` (incluindo toda operação `internal.*`) retorna `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; um argumento obrigatório ausente ou em branco retorna `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Nenhum dos dois toca a sessão.
2. Consulta da sessão (`KeyNotFoundException` para um handle desconhecido), `OL_E_RUNTIME_SESSION_MISMATCH` quando `RuntimeCommand.SessionId` difere do handle, `OL_E_SESSION_NOT_RUNNING` a menos que o estado seja `Running`.
3. Requisição ao mailbox (abaixo); depois, `ScrubInternalValues` remove qualquer chave de resultado que comece com `internal_` ou termine com `_address`, `_pointer`, `_vmt`.

O cliente da CLI e o plano de controle executam o passo 1 por conta própria antes de contatar o proprietário, de modo que um nome de comando inválido é informado como código de saída 2 mesmo quando nenhuma sessão está ativa (`OL_E_RUNTIME_OPERATION_UNKNOWN` e `OL_E_RUNTIME_ARGUMENT_REQUIRED` correspondem a `InvalidArguments`; outras rejeições, a 7; ausência de proprietário, a 4).

O endpoint do plano de controle só existe enquanto o proprietário está `Running`, depois da inicialização e depois que qualquer harness `/runtime-batch`, `/runtime-write-batch` ou `/d3d-batch` tiver sido concluído. Comandos que alteram estado (`runtime.execute`, `session.stop`) precisam levar o `session_id` ativo; a CLI o obtém automaticamente de `session.status` (caso contrário, `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Identidade da requisição e o mailbox

Um `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` é serializado em um envelope `RuntimeCommandWire` (magic `OLRC`, versão 1, cabeçalho de 72 bytes, SHA-256 do payload JSON) e colocado no mailbox single-flight de 64 KiB da sessão (`OmsiLaunch.Runtime.<sessionId>`). O plugin consulta o mailbox a cada 50 ms no thread de UI do OMSI, executa a operação ali e grava a resposta.

Regras que tornam o canal robusto:

| Regra | Efeito |
| --- | --- |
| Single flight | Uma requisição por vez por sessão; o host serializa os chamadores com um semáforo. Um slot que ainda está `requested` quando chega uma nova requisição gera `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Vínculo com a sessão | O plugin ignora (e limpa) uma requisição cujo GUID de sessão não é o seu; o host rejeita uma resposta cuja sessão ou id de requisição não corresponde (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Quando o prazo expira, o host redefine o slot para ocioso e lança `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Resposta tardia | O plugin só publica uma resposta se o slot ainda contém o id da sua requisição; a resposta a uma requisição abandonada é descartada. Se mesmo assim uma chegar, a próxima requisição do host encontra um slot `responded` obsoleto, descarta-o e prossegue; se essa resposta obsoleta tiver o **mesmo** id de requisição que a nova requisição, o host lança `OL_E_RUNTIME_REQUEST_ID_REUSED`. Portanto, os chamadores nunca devem reutilizar um id de requisição dentro de uma sessão. |
| Tamanho | Uma requisição maior que o slot é rejeitada antes de ser colocada no mailbox (`ArgumentOutOfRangeException`). Uma página de lista que não cabe é encurtada pelo plugin (últimas linhas descartadas, `truncated=true`, `returned_count` menor, `next_cursor` na primeira linha descartada); qualquer outra resposta grande demais é substituída por um erro tipado `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Canal fechado | Depois que a sessão termina, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Os ids de requisição são `ulong` escolhidos pelo chamador. A CLI usa faixas fixas: `10_001` para `/runtime:`, `50_001+` para requisições encaminhadas pelo plano de controle, `1+` para o batch de leitura, `20_000+` para o batch D3D, `30_000+` para `D3DRuntimeApi`. Os integradores devem usar um contador monotonicamente crescente por sessão.

<a id="handles-and-stale-detection"></a>
## Handles e detecção de obsolescência

| Prefixo | Tipo | Emitido por | Formato |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + seis dígitos decimais (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + seis dígitos decimais |
| `d3dtex-` | Textura D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Os handles são opacos e têm escopo de sessão: são criados pelo plugin, nunca codificam um endereço e não têm significado em outra sessão. Quando um handle é resolvido, o plugin verifica se o endereço ao qual ele corresponde ainda está na coleção viva do OMSI (conforme a última leitura de lista; `road-vehicles.list` e `humans.list` atualizam a visão) e relê uma impressão digital do objeto (a VMT do Delphi mais o ponteiro de definição do veículo, ou o índice de modelo do humano). Uma divergência significa que o objeto nativo foi destruído e seu endereço reutilizado: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. O mesmo token é retornado novamente para o mesmo objeto vivo em leituras de lista repetidas. Ponto cego residual: um objeto da mesma classe e definição recriado no mesmo endereço entre duas leituras de lista não pode ser distinguido. Os handles D3D são validados pela ponte nativa: um handle desconhecido ou de outra sessão gera `OL_E_D3D_STALE_RESOURCE_HANDLE`, um handle liberado gera `OL_E_D3D_RESOURCE_RELEASED`, e uma mudança de geração do dispositivo marca as texturas como `STALE`.

Os handles nunca são endereços nativos e nunca devem ser interpretados, comparados numericamente, persistidos entre sessões ou passados a outra sessão: trate-os como strings opacas válidas para a sessão que os emitiu.

Evidência de runtime: um handle D3D liberado continua rejeitado depois que novas texturas são criadas, e um handle de uma sessão anterior é rejeitado pela seguinte (fechamento de runtime `H02`); um reset do dispositivo D3D invalida todas as texturas vivas (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). A detecção de handles obsoletos de RoadVehicle e Human após remoção natural (RV-002) não tem produtor seguro em runtime (o OMSI não removeu objetos nas janelas de observação e nenhuma operação pública remove um); ela é coberta offline.

<a id="what-is-not-journaled"></a>
## O que não é registrado no journal

As mutações de runtime alteram somente a memória do OMSI. **Elas não são registradas no journal da transação e não são restauradas** quando a sessão termina: `time.set`, `camera.set`, `camera.lock` (a política para quando o plugin é desligado), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` e todo recurso `d3d.texture.*`. Elas desaparecem com o processo do OMSI, que o OmsiLaunch encerra ao final da sessão sem deixar o OMSI persistir nada (veja [transações e recuperação](../concepts/transactions-and-recovery.md)). Nada na família de runtime toca o sistema de arquivos.

<a id="argument-and-result-conventions"></a>
## Convenções de argumentos e resultados

- Os argumentos são pares de chave/valor em string. Na CLI, `--key=value` depois das palavras de comando se torna um argumento de runtime (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` é o equivalente para `/runtime:<op>`. Os números usam a cultura invariável (separador decimal `.`); os booleanos são `true`/`false`.
- As palavras de comando correspondem a ids de operação por meio de `CliInput.HierarchicalRoutes` (por exemplo `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). A tabela completa de rotas está na [referência da CLI](cli.md). As operações D3D não têm rota por palavra de comando; use `/runtime:d3d.status` ou `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Os resultados são dicionários planos de strings. As listas usam chaves `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) com os campos de página `count`, `returned_count`, `truncated` e `next_cursor` (veja [paginação](capabilities.md#pagination)).
- As falhas trazem `Succeeded=false`, um `ErrorCode` `OL_E_` e, para falhas no lado do plugin, `detail` (e `native_status` para D3D, `exception` para falhas inesperadas).

<a id="cli-envelope---json"></a>
### Envelope da CLI (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

Um envelope de erro é `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Sem `--json`, a CLI imprime o objeto de resultado como JSON indentado ou como `CODE: message`.

<a id="api-envelope"></a>
### Envelope da API

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` lança exceção para violações de fronteira após a validação pelo registro (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) e retorna `Succeeded=false` para rejeições pelo registro e erros no lado do plugin.

<a id="worked-examples"></a>
## Exemplos práticos

Todos os exemplos da CLI pressupõem que um proprietário está em execução para a instalação que contém o `OmsiLaunch.exe` (iniciado, por exemplo, com `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, ou `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` quando um exemplo precisa de um veículo do jogador) e são executados a partir de um segundo console na mesma instalação.

| Família | Comando | O que faz |
| --- | --- | --- |
| Sessão | `OmsiLaunch.exe session status --json` | Lê `SessionId`, `State`, diagnósticos e a lista limitada de eventos de runtime. |
| Hora | `OmsiLaunch.exe time get` e depois `OmsiLaunch.exe time set --hour=7 --minute=30` | Lê o relógio; ajusta-o por meio do `SetTime` perfilado e retorna a releitura. |
| Clima | `OmsiLaunch.exe weather get` e `OmsiLaunch.exe weather actual get` | Lê o estado do clima atual e do clima real/ICAO. `OmsiLaunch.exe weather set --wind_speed=5` define o clima manual (o registro de origem do OMSI; o METAR ao vivo para) e `OmsiLaunch.exe weather metar set --icao=EDDF` passa para o clima METAR ao vivo. Ambos são EXPERIMENTAL até serem validados em runtime. |
| Mapa | `OmsiLaunch.exe map get` | Identidade do mapa, nome, descrição, quantidade de tiles, intervalo de anos e lado do tráfego. |
| Câmera | `OmsiLaunch.exe camera set --field_of_view=50` e depois `OmsiLaunch.exe camera lock --family=0 --preset=1` e `OmsiLaunch.exe camera unlock` | Grava o FOV; fixa a família do motorista com o preset 1 (exige um veículo do jogador, por exemplo uma situação salva); libera a política. |
| Veículos | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Contagens; handles; um snapshot. |
| Spawn | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Cria um RoadVehicle de IA (timeout de 30 s); retorna `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` invoca `PlaceRandomBus`. |
| Jogador | `OmsiLaunch.exe player get` | `present=false` em uma inicialização headless; caso contrário, o snapshot do jogador. |
| Humanos | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Contagens; handles; um snapshot. |
| Tabela de horários | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Contagens do gerenciador; linhas limitadas de trajetos; logs da tabela de horários. |
| Scripts | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Listagem/leitura/escrita de variáveis numéricas; leitura/escrita de variáveis string; triggers de script e de som (as escritas de string e os triggers são EXPERIMENTAL até serem validados em runtime). |
| Constantes e curvas | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Constantes do veículo e avaliação de curvas. Os nomes são específicos do modelo: use os nomes retornados pelo comando `list` (estes foram listados para o ônibus do jogador de `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Metadados HOF da definição do veículo. |
| Motoristas e bilhetes | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Registros de motoristas; pacote de bilhetes. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` e depois `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` e `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Ciclo de vida da textura no thread de renderização; `<HANDLE>` é o `handle` impresso por `create`; `--pixels_base64` deve decodificar para `width * height * 4` bytes (formatos de 32 bits) e no máximo 48 KiB. |
| Eventos | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Lista limitada de eventos; acompanhamento por polling (250 ms) até Ctrl+C. |
| Parada | `OmsiLaunch.exe session stop` | Solicita a parada canônica: o OMSI é encerrado e a transação é restaurada pelo proprietário. |

<a id="stability"></a>
## Estabilidade

O canal em si (`runtime.command-channel`) é `STABLE_BETA`: a integridade do protocolo, o vínculo com a sessão, o descarte de respostas tardias e a rejeição tipada por excesso de tamanho são cobertos offline por `runtime-command.wire-guard`, `runtime-command.session-binding` e `runtime-command.late-response-ignored`, e em runtime por RV-003 (sessão `5f641c8d`), pela Rodada A RA-019 (um cliente abandonou `road-vehicles.spawn` após 250 ms; a requisição seguinte teve sucesso) e pela bateria de testes de canal `R03` do fechamento de runtime (timeout, cancelamento, id de requisição reutilizado, rejeição pelo plugin, operação interna, sessão errada e argumento ausente, cada um seguido de uma requisição bem-sucedida). A estabilidade por operação está em [capacidades](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Reutilização do canal após falhas

Todo caminho terminal de uma requisição de runtime deixa o mailbox reutilizável: sucesso, um erro tipado, uma resposta malformada, grande demais, de outra sessão ou com id de requisição errado, timeout, cancelamento pelo chamador e falhas de decodificação terminam todos em uma única limpeza que devolve o slot ao estado ocioso e limpa o comprimento e o cabeçalho do envelope, de modo que nada de uma requisição anterior pode ser lido pela seguinte. Uma requisição ou resposta remanescente encontrada quando uma nova requisição começa é limpa primeiro (`OL_E_RUNTIME_REQUEST_ID_REUSED` se ela tiver o id da nova requisição). No lado do plugin, uma exceção dentro de uma operação é respondida com `OL_E_RUNTIME_OPERATION_FAILED`, um resultado maior que o slot com `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (um envelope fixo e pequeno), uma requisição para outra sessão com `OL_E_RUNTIME_SESSION_MISMATCH`, e uma requisição que o host abandonou nunca é respondida. Essas regras são cobertas offline (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); os caminhos que um chamador consegue produzir (timeout, cancelamento, id reutilizado, rejeições tipadas, resposta tardia) também têm evidência de runtime (RA-019, `R03`). Respostas corrompidas, de outra sessão ou grandes demais não podem ser produzidas de fora do produto e continuam somente offline.
