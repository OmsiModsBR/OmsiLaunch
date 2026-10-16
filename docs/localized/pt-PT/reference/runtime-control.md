# Controlo de runtime

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Tradução da [página original em inglês](../../../reference/runtime-control.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

O controlo de runtime é o conjunto de operações de leitura, escrita e ação que o OmsiLaunch executa dentro de uma sessão do OMSI em execução. Esta página explica as três formas de lhe aceder (API do proprietário, cliente CLI, plano de controlo local), como os pedidos viajam do chamador até ao plugin e de volta, como funcionam os handles e as identidades dos pedidos, que timeouts se aplicam, o que deliberadamente não é registado no journal, e as convenções de argumentos e resultados, com um exemplo prático para cada família. O inventário de operações propriamente dito, com argumentos, chaves de resultado e erros, está em [capacidades](capabilities.md). Fontes: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` e `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` e `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Três pontos de entrada

| Ponto de entrada | Quem | Percurso | Timeout |
| --- | --- | --- | --- |
| API do proprietário | Um integrador que iniciou a sessão dentro do processo com `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> validação pelo registo -> pesquisa da sessão -> mailbox | `TimeSpan` fornecido pelo chamador |
| CLI do proprietário (`/runtime:<op>`) | O processo `OmsiLaunch.exe` que é proprietário da sessão | uma operação executada logo após `Running`, com o resultado escrito em `.omsilaunch\diagnostics\<session>-runtime-operation.json` e na consola; a sessão continua | 5 s (15 s para `road-vehicles.spawn`) |
| Cliente CLI | Qualquer invocação de `OmsiLaunch.exe` **sem** argumento de instalação, por exemplo `OmsiLaunch.exe time get` | named pipe `runtime.execute` para o proprietário da instalação onde se encontra o executável -> o proprietário chama `ExecuteRuntimeAsync` | 8 s (30 s para `road-vehicles.spawn`) tanto do lado do cliente como do lado do proprietário |
| Plano de controlo local | Qualquer processo do mesmo utilizador do Windows | o mesmo protocolo de pipe que o cliente CLI; ver [controlo local](local-control.md) | como acima |

Todos os percursos terminam em `ExecuteRuntimeAsync`, que impõe a fronteira pública por esta ordem:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: uma operação que não consta de `PublicRuntimeOperationIds` (incluindo todas as operações `internal.*`) devolve `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; um argumento obrigatório em falta ou vazio devolve `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Nenhum dos casos toca na sessão.
2. Pesquisa da sessão (`KeyNotFoundException` para um handle desconhecido), `OL_E_RUNTIME_SESSION_MISMATCH` quando `RuntimeCommand.SessionId` difere do handle, `OL_E_SESSION_NOT_RUNNING` salvo se o estado for `Running`.
3. Pedido à mailbox (ver abaixo); depois, `ScrubInternalValues` remove qualquer chave de resultado que comece por `internal_` ou termine em `_address`, `_pointer`, `_vmt`.

O cliente CLI e o plano de controlo executam eles próprios o passo 1 antes de contactarem o proprietário, pelo que um nome de comando inválido é comunicado com o código de saída 2 mesmo quando não há nenhuma sessão ativa (`OL_E_RUNTIME_OPERATION_UNKNOWN` e `OL_E_RUNTIME_ARGUMENT_REQUIRED` correspondem a `InvalidArguments`; as restantes rejeições a 7; a ausência de proprietário a 4).

O endpoint do plano de controlo só existe enquanto o proprietário está em `Running`, após o arranque e depois de concluído qualquer harness `/runtime-batch`, `/runtime-write-batch` ou `/d3d-batch`. Os comandos que alteram estado (`runtime.execute`, `session.stop`) têm de incluir o `session_id` ativo; a CLI obtém-no automaticamente a partir de `session.status` (caso contrário, `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Identidade dos pedidos e a mailbox

Um `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` é serializado num envelope `RuntimeCommandWire` (magic `OLRC`, versão 1, cabeçalho de 72 bytes, SHA-256 do payload JSON) e colocado na mailbox (caixa de correio) de 64 KiB e de pedido único da sessão (`OmsiLaunch.Runtime.<sessionId>`). O plugin consulta a mailbox a cada 50 ms na thread de UI do OMSI, executa aí a operação e escreve a resposta.

Regras que tornam o canal robusto:

| Regra | Efeito |
| --- | --- |
| Pedido único | Um pedido de cada vez por sessão; o anfitrião serializa os chamadores com um semáforo. Um slot que ainda está em `requested` quando chega um novo pedido dá `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Vinculação à sessão | O plugin ignora (e limpa) um pedido cujo GUID de sessão não é o seu; o anfitrião rejeita uma resposta cujo id de sessão ou de pedido não corresponda (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Quando o prazo expira, o anfitrião repõe o slot em inativo e lança `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Resposta tardia | O plugin só publica uma resposta se o slot ainda contiver o id do seu pedido; a resposta a um pedido abandonado é descartada. Se, ainda assim, uma chegar, o pedido seguinte do anfitrião encontra um slot `responded` obsoleto, descarta-o e prossegue; se essa resposta obsoleta tiver o **mesmo** id de pedido que o novo pedido, o anfitrião lança `OL_E_RUNTIME_REQUEST_ID_REUSED`. Por isso, os chamadores nunca devem reutilizar um id de pedido dentro de uma sessão. |
| Tamanho | Um pedido maior do que o slot é rejeitado antes de ser colocado (`ArgumentOutOfRangeException`). Uma página de lista que não caiba é encurtada pelo plugin (últimas linhas descartadas, `truncated=true`, `returned_count` mais pequeno, `next_cursor` na primeira linha descartada); qualquer outra resposta demasiado grande é substituída por um erro tipado `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Canal fechado | Depois de a sessão terminar, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Os ids de pedido são `ulong` escolhidos pelo chamador. A CLI usa intervalos fixos: `10_001` para `/runtime:`, `50_001+` para pedidos reencaminhados pelo plano de controlo, `1+` para o lote de leitura, `20_000+` para o lote D3D, `30_000+` para `D3DRuntimeApi`. Os integradores devem usar um contador monotonamente crescente por sessão.

<a id="handles-and-stale-detection"></a>
## Handles e deteção de handles obsoletos

| Prefixo | Tipo | Emitido por | Formato |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + seis dígitos decimais (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + seis dígitos decimais |
| `d3dtex-` | Textura D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Os handles são opacos e têm âmbito de sessão: são gerados pelo plugin, nunca codificam um endereço e não têm significado noutra sessão. Quando um handle é resolvido, o plugin verifica que o endereço a que corresponde ainda está na coleção viva do OMSI (à data da última leitura da lista; `road-vehicles.list` e `humans.list` atualizam a vista) e relê uma impressão digital do objeto (a VMT do Delphi mais o ponteiro de definição do veículo, ou o índice do modelo do humano). Uma discrepância significa que o objeto nativo foi destruído e o seu endereço reutilizado: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. O mesmo token é devolvido de novo para o mesmo objeto vivo ao longo de leituras repetidas da lista. Ponto cego residual: um objeto da mesma classe e definição, recriado no mesmo endereço entre duas leituras da lista, não pode ser distinguido. Os handles D3D são validados pela ponte nativa: um handle desconhecido ou de outra sessão dá `OL_E_D3D_STALE_RESOURCE_HANDLE`, um handle libertado dá `OL_E_D3D_RESOURCE_RELEASED`, e uma mudança de geração do dispositivo marca as texturas como `STALE`.

Os handles nunca são endereços nativos e nunca devem ser analisados, comparados numericamente, persistidos entre sessões nem passados a outra sessão: devem ser tratados como strings opacas válidas para a sessão que os emitiu.

Evidência de runtime: um handle D3D libertado continua rejeitado depois de serem criadas novas texturas, e um handle de uma sessão anterior é rejeitado pela seguinte (fecho de runtime `H02`); um reset do dispositivo D3D invalida todas as texturas vivas (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). A deteção de handles obsoletos de RoadVehicle e Human após remoção natural (RV-002) não tem produtor seguro em runtime (o OMSI não removeu objetos nas janelas de observação e nenhuma operação pública remove um); está coberta offline.

<a id="what-is-not-journaled"></a>
## O que não é registado no journal

As alterações de runtime mudam apenas a memória do OMSI. **Não são registadas no journal da transação e não são restauradas** quando a sessão termina: `time.set`, `camera.set`, `camera.lock` (a política para quando o plugin encerra), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` e todos os recursos `d3d.texture.*`. Desaparecem com o processo do OMSI, que o OmsiLaunch termina no fim da sessão sem deixar que o OMSI persista nada (ver [transações e recuperação](../concepts/transactions-and-recovery.md)). Nada na família de runtime toca no sistema de ficheiros.

<a id="argument-and-result-conventions"></a>
## Convenções de argumentos e resultados

- Os argumentos são pares chave/valor de strings. Na CLI, `--key=value` a seguir às palavras de comando torna-se um argumento de runtime (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` é o equivalente para `/runtime:<op>`. Os números usam a cultura invariante (separador decimal `.`); os booleanos são `true`/`false`.
- As palavras de comando correspondem a ids de operação através de `CliInput.HierarchicalRoutes` (por exemplo `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). A tabela completa de rotas está na [referência da CLI](cli.md). As operações D3D não têm rota por palavras de comando; usar `/runtime:d3d.status` ou `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Os resultados são dicionários planos de strings. As listas usam chaves `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) com os campos de página `count`, `returned_count`, `truncated` e `next_cursor` (ver [paginação](capabilities.md#pagination)).
- As falhas incluem `Succeeded=false`, um `ErrorCode` `OL_E_` e, para falhas do lado do plugin, `detail` (e `native_status` para D3D, `exception` para falhas inesperadas).

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

Um envelope de erro tem a forma `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Sem `--json`, a CLI imprime o objeto de resultado como JSON indentado ou `CODE: message`.

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

`ExecuteRuntimeAsync` lança exceções para violações de fronteira posteriores à validação pelo registo (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) e devolve `Succeeded=false` para rejeições pelo registo e erros do lado do plugin.

<a id="worked-examples"></a>
## Exemplos práticos

Todos os exemplos da CLI pressupõem que está a executar um proprietário para a instalação que contém `OmsiLaunch.exe` (iniciado, por exemplo, com `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, ou com `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` quando um exemplo precisa de um veículo do jogador) e são executados a partir de uma segunda consola na mesma instalação.

| Família | Comando | O que faz |
| --- | --- | --- |
| Sessão | `OmsiLaunch.exe session status --json` | Lê `SessionId`, `State`, os diagnósticos e a lista limitada de eventos de runtime. |
| Hora | `OmsiLaunch.exe time get` e depois `OmsiLaunch.exe time set --hour=7 --minute=30` | Lê o relógio; define-o através do `SetTime` perfilado e devolve a releitura. |
| Meteorologia | `OmsiLaunch.exe weather get` e `OmsiLaunch.exe weather actual get` | Lê o estado meteorológico atual e o estado real/ICAO. `OmsiLaunch.exe weather set --wind_speed=5` define meteorologia manual (o registo de origem do OMSI; o METAR em direto pára) e `OmsiLaunch.exe weather metar set --icao=EDDF` passa para meteorologia METAR em direto. Ambos são EXPERIMENTAL até estarem validados em runtime. |
| Mapa | `OmsiLaunch.exe map get` | Identidade do mapa, nome, descrição, número de tiles, intervalo de anos e lado de circulação. |
| Câmara | `OmsiLaunch.exe camera set --field_of_view=50` e depois `OmsiLaunch.exe camera lock --family=0 --preset=1` e `OmsiLaunch.exe camera unlock` | Escreve o FOV; fixa a família do motorista com o preset 1 (requer um veículo do jogador, por exemplo uma situação guardada); liberta a política. |
| Veículos | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Contagens; handles; um snapshot. |
| Criação (spawn) | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Cria um RoadVehicle de IA (timeout de 30 s); devolve `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` invoca `PlaceRandomBus`. |
| Jogador | `OmsiLaunch.exe player get` | `present=false` num arranque headless; caso contrário, o snapshot do jogador. |
| Humanos | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Contagens; handles; um snapshot. |
| Horário | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Contagens do gestor; linhas de trajetos limitadas; registos do horário. |
| Scripts | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Listagem/leitura/escrita de variáveis numéricas; leitura/escrita de variáveis de string; triggers de script e de som (as escritas de strings e os triggers são EXPERIMENTAL até estarem validados em runtime). |
| Constantes e curvas | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Constantes do veículo e avaliação de curvas. Os nomes são específicos do modelo: usar os nomes devolvidos pelo comando `list` (estes foram listados para o autocarro do jogador de `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Metadados HOF da definição do veículo. |
| Motoristas e bilhetes | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Registos de motoristas; pacote de bilhetes. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` e depois `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` e `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Ciclo de vida da textura na thread de renderização; `<HANDLE>` é o `handle` impresso por `create`; `--pixels_base64` tem de descodificar para `width * height * 4` bytes (formatos de 32 bits) e no máximo 48 KiB. |
| Eventos | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Lista limitada de eventos; observação por consulta periódica (250 ms) até Ctrl+C. |
| Paragem | `OmsiLaunch.exe session stop` | Pede a paragem canónica: o OMSI é terminado e a transação é restaurada pelo proprietário. |

<a id="stability"></a>
## Estabilidade

O canal em si (`runtime.command-channel`) é `STABLE_BETA`: a integridade do formato de transmissão, a vinculação à sessão, o descarte de respostas tardias e a rejeição tipada de tamanho excessivo estão cobertos offline por `runtime-command.wire-guard`, `runtime-command.session-binding` e `runtime-command.late-response-ignored`, e em runtime por RV-003 (sessão `5f641c8d`), pela Ronda A RA-019 (um cliente abandonou `road-vehicles.spawn` após 250 ms; o pedido seguinte foi bem-sucedido) e pela bateria de testes do canal do fecho de runtime `R03` (timeout, cancelamento, id de pedido reutilizado, rejeição pelo plugin, operação interna, sessão errada e argumento em falta, cada um seguido de um pedido bem-sucedido). A estabilidade por operação está em [capacidades](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Reutilização do canal após falhas

Todos os percursos terminais de um pedido de runtime deixam a mailbox reutilizável: sucesso, um erro tipado, uma resposta malformada, demasiado grande, de outra sessão ou com id de pedido errado, timeout, cancelamento pelo chamador e falhas de descodificação terminam todos numa única limpeza que repõe o slot em inativo e limpa o comprimento e o cabeçalho do envelope, de modo que nada de um pedido anterior pode ser lido pelo seguinte. Um pedido ou uma resposta remanescente encontrado quando um novo pedido começa é limpo primeiro (`OL_E_RUNTIME_REQUEST_ID_REUSED` se tiver o id do novo pedido). Do lado do plugin, uma exceção dentro de uma operação é respondida com `OL_E_RUNTIME_OPERATION_FAILED`, um resultado maior do que o slot com `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (um envelope fixo e pequeno), um pedido para outra sessão com `OL_E_RUNTIME_SESSION_MISMATCH`, e um pedido que o anfitrião abandonou nunca é respondido. Estas regras estão cobertas offline (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); os percursos que um chamador consegue produzir (timeout, cancelamento, id reutilizado, rejeições tipadas, resposta tardia) têm também evidência de runtime (RA-019, `R03`). As respostas corrompidas, de outra sessão ou demasiado grandes não podem ser produzidas a partir do exterior do produto e permanecem cobertas apenas offline.
