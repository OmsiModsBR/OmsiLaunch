# Limitações conhecidas

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> Tradução da [página original em inglês](../../../reference/known-limitations.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página enumera, a partir do código, tudo o que no OmsiLaunch 0.1.0-beta4 é `UNAVAILABLE`, `PARTIAL` ou um risco aceite, para que os utilizadores e integradores não construam sobre comportamentos que o produto não oferece. Cada linha indica a limitação, a sua estabilidade, a razão da sua existência e onde está documentada em detalhe. A documentação inglesa é normativa; as cópias localizadas em `docs/localized/` não são mantidas ao mesmo nível e podem estar desatualizadas (ver a última secção).

<a id="compatibility"></a>
## Compatibilidade

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| As capacidades que leem disposições de objetos (veículos, humanos, meteorologia, mapa, câmara, horário, scripts, motoristas, bilhetes) só são verificadas numa disposição conhecida (`Omsi23004`); noutra build do OMSI 2 estão indisponíveis (`OL_E_CAPABILITY_UNRESOLVED`) até existirem validadores estruturais | `PARTIAL` em builds desconhecidas | [compatibilidade](compatibility.md) |
| O executável Steam genuíno (protegido por DRM) não foi validado em runtime; uma cópia LAA controlada foi | `PARTIAL` para Steam | [compatibilidade](compatibility.md) |
| Apenas a disposição conhecida foi validada em runtime; a resolução independente da build é verificada offline face ao executável de referência com a sua identidade retida | `EXPERIMENTAL` em builds desconhecidas | [estado da validação em runtime](../status/runtime-validation-status.md) |
| Apenas Windows 10+ x64; sem Windows 7/8, sem XP, sem ARM64 | `UNAVAILABLE` | [compatibilidade](compatibility.md) |
| O plugin precisa do runtime .NET 6 **x86**, além do runtime x64 usado pelo controlador | | [compatibilidade](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Início do mundo e opções de lançamento

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) está implementado a partir da análise estática do ramo de último estado do OMSI e validado apenas offline | `EXPERIMENTAL` | [CLI](cli.md) |
| Data, hora e ano explícitos ou do sistema (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` do perfil) aplicam-se apenas a arranques NEW_MAP e são validados offline; a validação em runtime está pendente. O dia 31 de dezembro de um ano bissexto não pode ser pedido (o calendário do OMSI dá a volta no dia 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Predefinição de meteorologia, ICAO e meteorologia real atual no início (`/weather*`, `new.weather`) são aplicadas logo após o arranque do mundo através do carregamento de meteorologia e das flags METAR do próprio OMSI (fechado estaticamente, BI-003); a validação em runtime está pendente. A meteorologia que o OMSI mostra na primeiríssima imagem pode ainda ser a predefinida do mapa | `EXPERIMENTAL` | [CLI](cli.md) |
| Modelo, repaint, HOF, número de frota e matrícula do veículo do jogador no início (família `/vehicle`, `PlayerVehicleSpec`) são resolvidos face ao catálogo de conteúdos, mas não aplicados; pedi-los torna o plano não executável. A sequência nativa de atribuição (colocação através da caixa de diálogo de posição do OMSI, `MakeVehicle`, os passos de finalização por unidade, `CopyTempListIntoMainList`, o índice do jogador) está fechada estaticamente, mas cria e regista um veículo ativo e não é implementada antes de poder ser validada em runtime; está prevista para uma beta posterior. Uma situação guardada ou `/last` restaura o seu próprio veículo do jogador | `UNAVAILABLE` (BI-007) | `player.assign-headless` em [capacidades](capabilities.md) |
| O ponto de entrada por identidade (`/entrypoint:<identity>`) seleciona a etiqueta exata que o OMSI apresenta (fechado estaticamente, BI-001); um registo posterior que partilhe uma etiqueta não pode ser selecionado, tal como na própria caixa de diálogo do OMSI. Validação em runtime pendente | `EXPERIMENTAL` | capacidade do plano `world.entrypoint-identity` |
| As definições de teclado e controlador por chave (`Environment.Keyboard`, `Environment.Controllers`) não têm chaves catalogadas: em vez disso, substituem-se ficheiros inteiros com `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| O encerramento cooperativo (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) é opt-in e validado apenas offline; por predefinição, o OMSI é terminado quando uma sessão termina e `ShutdownTimeoutSeconds` não tem efeito | `EXPERIMENTAL` | [CLI](cli.md) |
| As flags de diagnóstico `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` preenchem `DiagnosticsSpec` sem mais nenhum efeito (o trace do host em `.omsilaunch\diagnostics` é sempre escrito); `/trace-plugin` e `/trace-native` acrescentam-lhe eventos de trace do plugin e nativos | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch` e `/d3d-batch` são harnesses de validação | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fim da sessão e controlo de processos

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| A paragem da sessão é uma terminação forçada: `session.stop`, "End session" na área de notificação, Ctrl+C e `CloseAsync` conduzem todos a `TerminateProcess`. A rotina de encerramento do OMSI não é executada, o OMSI não reescreve `options.cfg` nem os seus registos ao sair, e qualquer estado do OMSI não guardado perde-se. Isto é deliberado: impede que o OMSI escreva por cima dos ficheiros restaurados. | por conceção | [ciclo de vida da sessão](../concepts/session-lifecycle.md) |
| O encerramento cooperativo por WM_CLOSE com timeout e terminação como alternativa não está implementado | `UNAVAILABLE` (decisão de produto, S-11; o OMSI ignorou `WM_CLOSE` enviado à sua janela principal na ronda de fecho de runtime) | [estado da validação em runtime](../status/runtime-validation-status.md) |
| Ao fechar a consola ou ao terminar a sessão do Windows (logoff), o proprietário dispõe de um orçamento de 4 s para parar e restaurar; o que ficar por fazer é recuperado pelo journal no início seguinte | fecho da consola validado em runtime; logoff não exercitado | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transação, recuperação e lease

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| O lease da instalação é um semáforo `Local\`: um proprietário por instalação **por sessão de início de sessão (logon)**; não é imposto entre utilizadores; não é libertado enquanto outro processo mantiver um handle; qualquer processo do mesmo utilizador pode reter o nome | risco aceite (S-18) | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A recuperação é recusada (`OL_E_INSTALLATION_BUSY`) enquanto o processo do OMSI registado no journal, ou, para um journal sem PID, qualquer `Omsi.exe` dessa raiz, estiver em execução | por conceção | como acima |
| Um caminho de overlay originalmente ausente cujo conteúdo mudou durante a sessão bloqueia o restauro (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) até ser inspecionado | por conceção | como acima |
| Os journals anteriores às impressões digitais de propriedade só podem ser fechados por uma sessão com bytes planeados idênticos (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | como acima |
| Só são restaurados os caminhos pertencentes à sessão. As escritas próprias do OMSI durante uma sessão (`[last_map]` de `options.cfg` quando nenhuma definição sobrepõe `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, perfil de motorista, registos) persistem, tal como após um início direto do OMSI | por conceção | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A remoção de um `closecheck` obsoleto antes de uma sessão é permanente (registada, não restaurada) quando `SuppressStaleClosecheckWarning` é true | por conceção | como acima |

<a id="runtime-control"></a>
## Controlo de runtime

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `weather.set` e `weather.metar.set` escrevem o registo de meteorologia de origem do OMSI (fechado estaticamente); a persistência ao longo do ciclo meteorológico, a resolução do tipo de nuvem e a transferência METAR aguardam validação em runtime. Os valores derivados (humidade, fatores de luz, vento atual) não podem ser definidos; enquanto o OMSI está em pausa, só se atualizam após retomar | `EXPERIMENTAL` | [capacidades](capabilities.md) |
| `SetActualDateTime` apenas copia o relógio do PC para o relógio de arranque do OMSI (análise estática); o OmsiLaunch não o chama e define ele próprio o relógio de arranque | por conceção | `calendar.set-actual-date-time` |
| As escritas de variáveis de texto, os triggers com nome e os triggers de som passam pelos próprios métodos do OMSI (fechado estaticamente, BI-004); o seu efeito nos scripts dos veículos aguarda validação em runtime. Apenas os veículos rodoviários são alvos | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Sem relocalização de veículos, sem reassociação espacial entre tiles, sem autoridade de transformação segura para o ODE; os campos de posição são só de leitura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` precisam de um PlayerVehicle; o início headless de NEW_MAP não tem nenhum (uma situação guardada fornece um) | por conceção (BI-007) | RV-004 |
| As mutações de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texturas D3D) não são registadas no journal nem restauradas | por conceção | [controlo de runtime](runtime-control.md) |
| A obsolescência dos handles é decidida a partir do número de instância nunca reutilizado do próprio OMSI (`IDCode`) e, para humanos, a partir do desaparecimento e do reaparecimento; é validada offline. A remoção natural (RV-002: carros de IA a sair da rede de caminhos carregada) ainda tem de ser observada em runtime, e ainda não é emitido nenhum evento `entity.removed` | `PARTIAL` | [controlo de runtime](runtime-control.md) |
| Os resultados estão limitados pela mailbox de 64 KiB: as listas longas são truncadas (`truncated=true`); os dados de píxeis estão limitados a 48 KiB por `d3d.texture.update` | por conceção | [capacidades](capabilities.md) |
| Canal de pedido único: um pedido de cada vez por sessão; um slot ocupado resulta em `OL_E_RUNTIME_CHANNEL_BUSY`; os ids de pedido não podem ser reutilizados | por conceção | [controlo de runtime](runtime-control.md) |
| A telemetria é um anel de 64 eventos: um anfitrião que fique mais de 64 eventos para trás perde os mais antigos e é informado de quantos (`telemetry.lost`) | `EXPERIMENTAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| Os hooks de dispositivo Direct3D de um módulo são hooks de vtable: numa instalação em que outro componente repõe os métodos originais do dispositivo (observado na instalação de regressão pouco depois de a jogabilidade começar), deixam de ser chamados e não podem ser removidos na limpeza (`OL_E_MODULE_CLEANUP_INCOMPLETE`; o componente permanece carregado) | `PARTIAL` | [módulos de runtime](../../../reference/runtime-modules.md#native-components-and-hooks) |
| O carregamento de módulos de runtime é aberto: o OmsiLaunch não verifica quem fez um módulo; garante apenas que os módulos nunca correm dentro do processo do OmsiLaunch e que um programa de sessão fica confinado à sua tarefa (sem segundo processo, terminado com a sessão) | por conceção (E21) | [módulos de runtime](../../../reference/runtime-modules.md#security-boundary) |
| O protocolo 2 de módulos (definições, ativação por predefinição, o ficheiro de definições, programas de sessão, conflitos de plugins, avisos, `get_value`) é validado apenas offline | `EXPERIMENTAL` | [módulos de runtime](../../../reference/runtime-modules.md) |
| A reposição do dispositivo D3D foi observada em runtime (`resetting`, `restored`, invalidação por geração); não foi produzida uma transição `lost` distinta, porque o dispositivo do OMSI passou diretamente a `DEVICENOTRESET` | `PARTIAL` (RV-007) | [estado da validação em runtime](../status/runtime-validation-status.md) |
| As listas de runtime são paginadas (`cursor`, `limit`, `next_cursor`): uma página contém no máximo as linhas máximas da lista e o que cabe no slot de runtime de 64 KiB | `EXPERIMENTAL` | [capacidades](capabilities.md#pagination) |
| Alguns campos avançados do grafo de mapa/tile/caminho/objeto não são expostos; os leitores de runtime são snapshots tipados condicionados ao perfil, nunca acesso arbitrário à memória | por conceção | [capacidades](capabilities.md) |
| As leituras de memória dentro do processo seguem o padrão verificar-e-depois-usar contra um OMSI em execução; uma mutação concorrente do OMSI entre a verificação e a leitura pode produzir um snapshot inconsistente (`OL_E_RUNTIME_OPERATION_FAILED`) | risco aceite (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plano de controlo local e modelo de confiança

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Modelo de confiança do mesmo utilizador: o named pipe (`CurrentUserOnly`), os mapeamentos de memória de handoff/telemetria/runtime e o semáforo do lease são acessíveis a qualquer processo do mesmo utilizador do Windows. Um processo desses pode ler o estado, parar a sessão ou executar operações de runtime depois de ler o `session_id`. | risco aceite (S-06, S-30) | [controlo local](local-control.md) |
| O endpoint de controlo só existe enquanto o proprietário está em `Running`; um cliente vê `OL_E_NO_ACTIVE_SESSION` (saída 4) durante o arranque e após o fim da sessão | por conceção | [controlo local](local-control.md) |
| Se outro processo já for dono do nome do pipe, o proprietário continua a executar sem endpoint (`ListenFault`), e um segundo lançamento pode comunicar erradamente `OL_E_SESSION_ALREADY_ACTIVE` | risco aceite | [controlo local](local-control.md) |
| `.omsilaunch\` herda a ACL da raiz do OMSI; não é aplicado nenhum controlo de acesso explícito | risco aceite (S-31) | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnóstico e saída

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Os diagnósticos são apenas ficheiros locais (`.omsilaunch\diagnostics`); nada é enviado e não existe comunicação remota | por conceção | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A retenção mantém as 50 sessões mais recentes; os diagnósticos mais antigos com prefixo de sessão são eliminados quando uma nova sessão se inicia | por conceção | como acima |
| A saída JSON e os diagnósticos incluem caminhos da instalação (`RootPath`, diretórios de recursos, caminhos `.itx`) | por conceção (dados locais) | |
| A caixa de diálogo de falha da sessão de `OmsiLaunchW.exe` mostra como mensagem os dados de falha do plugin (por exemplo `{"name":"world.failed",...}`) em vez de uma frase; a linha `Code:` está correta | cosmético | [área de notificação do Windows](windows-tray.md) |
| Um pedido D3D rejeitado pela ponte nativa antes de qualquer chamada Direct3D comunica `native_status` corretamente, mas o seu texto `detail` indica `HRESULT 0x00000000` | cosmético | [capacidades](capabilities.md) |
| A janela de estado da área de notificação é um snapshot da sessão planeada tirado quando é aberta; não se atualiza e não mostra valores do OMSI em tempo real | por conceção | [área de notificação do Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentação

As páginas inglesas em `docs/` são a documentação normativa desta versão. `docs/localized/<locale>/` contém traduções das mesmas páginas da `0.1.0-beta4` (ver [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); onde uma tradução diferir do texto inglês, o texto inglês e o código são a referência autorizada. As páginas históricas e legadas aí listadas estão disponíveis apenas em inglês.

Relacionado: [capacidades](capabilities.md), [estado da validação em runtime](../status/runtime-validation-status.md), [erros](errors.md).
