# Limitações conhecidas

<!-- l10n: source=reference/known-limitations.md source-sha256=fe1eccc6b9e4b3d93fff33deeecda6b97abd139d7295dba1d6542fb6e57231a7 -->
> Tradução da [página original em inglês](../../../reference/known-limitations.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página lista, a partir do código, tudo o que no OmsiLaunch 0.1.0-beta4 é `UNAVAILABLE`, `PARTIAL` ou um risco aceito, para que usuários e integradores não construam sobre um comportamento que o produto não oferece. Cada linha indica a limitação, sua estabilidade, o motivo de existir e onde ela está documentada em detalhes. A documentação em inglês é normativa; as cópias localizadas em `docs/localized/` não são mantidas no mesmo nível e podem ficar defasadas (veja a última seção).

<a id="compatibility"></a>
## Compatibilidade

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| As capacidades que leem layouts de objetos (veículos, humanos, clima, mapa, câmera, tabela de horários, scripts, motoristas, bilhetes) são verificadas somente em um layout conhecido (`Omsi23004`); em outro build do OMSI 2 elas ficam indisponíveis (`OL_E_CAPABILITY_UNRESOLVED`) até que existam validadores estruturais | `PARTIAL` em builds desconhecidos | [compatibilidade](compatibility.md) |
| Somente o layout conhecido foi validado em runtime; a resolução independente de build é verificada offline contra o executável de referência com sua identidade ocultada | `EXPERIMENTAL` em builds desconhecidos | [status da validação em runtime](../status/runtime-validation-status.md) |
| Somente Windows 10+ x64; sem Windows 7/8, sem XP, sem ARM64 | `UNAVAILABLE` | [compatibilidade](compatibility.md) |
| O plugin precisa do runtime .NET 6 **x86**, além do runtime x64 usado pelo controlador | | [compatibilidade](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Início do mundo e opções de inicialização

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) é implementado a partir da análise estática do ramo de último estado do OMSI e validado somente offline | `EXPERIMENTAL` | [CLI](cli.md) |
| Data, hora e ano explícitos ou do sistema (`/date`, `/time`, `/year`, perfil `new.date`/`new.time`/`new.year`) se aplicam somente a inícios NEW_MAP e são validados offline; a validação em runtime está pendente. 31 de dezembro de um ano bissexto não pode ser solicitado (o calendário do OMSI dá a volta no dia 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Predefinição de clima, ICAO e clima real atual no início (`/weather*`, `new.weather`) são aplicados logo depois que o mundo inicia, pelo carregamento de clima e pelas flags de METAR do próprio OMSI (fechado estaticamente, BI-003); a validação em runtime está pendente. O clima que o OMSI mostra no primeiríssimo frame ainda pode ser o padrão do mapa | `EXPERIMENTAL` | [CLI](cli.md) |
| Modelo do veículo do jogador, pintura (repaint), HOF, número de frota e matrícula no início (família `/vehicle`, `PlayerVehicleSpec`) são resolvidos contra o catálogo de conteúdo, mas não aplicados; solicitá-los torna o plano não apto para execução. A sequência nativa de atribuição (posicionamento pelo diálogo de posição do OMSI, `MakeVehicle`, as etapas de finalização por unidade, `CopyTempListIntoMainList`, o índice do jogador) está fechada estaticamente, mas cria e registra um veículo ativo e não é implementada antes de poder ser validada em runtime; está prevista para um beta posterior. Uma situação salva ou `/last` restaura seu próprio veículo do jogador | `UNAVAILABLE` (BI-007) | `player.assign-headless` em [capacidades](capabilities.md) |
| O ponto de entrada por identidade (`/entrypoint:<identity>`) seleciona o rótulo exato que o OMSI apresenta (fechado estaticamente, BI-001); um registro posterior que compartilha um rótulo não pode ser selecionado, como no próprio diálogo do OMSI. Validação em runtime pendente | `EXPERIMENTAL` | capacidade do plano `world.entrypoint-identity` |
| As configurações de teclado e de controles por chave (`Environment.Keyboard`, `Environment.Controllers`) não têm chaves catalogadas: arquivos inteiros são substituídos com `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| O desligamento cooperativo (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) é opcional e validado somente offline; por padrão, o OMSI é encerrado à força quando uma sessão termina e `ShutdownTimeoutSeconds` não tem efeito | `EXPERIMENTAL` | [CLI](cli.md) |
| As flags de diagnóstico `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` preenchem `DiagnosticsSpec` sem nenhum outro efeito (o trace do host em `.omsilaunch\diagnostics` é sempre gravado); `/trace-plugin` e `/trace-native` acrescentam a ele eventos de trace do plugin e da camada nativa | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` são harnesses de validação | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fim da sessão e controle do processo

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| A parada da sessão é um encerramento forçado: `session.stop`, "End session" na bandeja, Ctrl+C e `CloseAsync` levam todos a `TerminateProcess`. A rotina de encerramento do OMSI não é executada, o OMSI não regrava `options.cfg` nem seus logs ao sair, e qualquer estado não salvo do OMSI é perdido. Isso é deliberado: impede que o OMSI grave por cima dos arquivos restaurados. | por design | [ciclo de vida da sessão](../concepts/session-lifecycle.md) |
| O encerramento cooperativo via WM_CLOSE, com timeout e fallback para encerramento forçado, não está implementado | `UNAVAILABLE` (decisão de produto, S-11; o OMSI ignorou `WM_CLOSE` enviado à sua janela principal na rodada de fechamento em runtime) | [status de validação em runtime](../status/runtime-validation-status.md) |
| No fechamento do console ou no logoff, o proprietário tem um orçamento de 4 s para parar e restaurar; o que restar é recuperado pelo journal no próximo início | fechamento do console validado em runtime; logoff não exercitado | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transação, recuperação e lease

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| O lease da instalação é um semáforo `Local\`: um proprietário por instalação **por sessão de logon**; não é imposto entre usuários diferentes; não é liberado enquanto outro processo mantém um handle; qualquer processo do mesmo usuário pode reter o nome | risco aceito (S-18) | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A recuperação é recusada (`OL_E_INSTALLATION_BUSY`) enquanto o processo do OMSI registrado no journal estiver em execução, ou, para um journal sem PID, enquanto qualquer `Omsi.exe` daquela raiz estiver em execução | por design | como acima |
| Um caminho de overlay originalmente ausente cujo conteúdo mudou durante a sessão bloqueia a restauração (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) até ser inspecionado | por design | como acima |
| Journals anteriores às impressões digitais de propriedade só podem ser fechados por uma sessão com bytes planejados idênticos (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | como acima |
| Somente os caminhos pertencentes à sessão são restaurados. As gravações do próprio OMSI durante uma sessão (`options.cfg` `[last_map]` quando nenhuma configuração sobrepõe `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, perfil de motorista, logs) persistem, como depois de um início direto do OMSI | por design | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A remoção de um `closecheck` obsoleto antes de uma sessão é permanente (registrada, não restaurada) quando `SuppressStaleClosecheckWarning` é true | por design | como acima |

<a id="runtime-control"></a>
## Controle de runtime

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| `weather.set` e `weather.metar.set` gravam o registro de clima de origem do OMSI (fechado estaticamente); a persistência ao longo do ciclo de clima, a resolução do tipo de nuvem e o download do METAR aguardam a validação em runtime. Os valores derivados (umidade, fatores de luz, vento atual) não podem ser definidos; enquanto o OMSI está em pausa, eles só são atualizados depois que a pausa termina | `EXPERIMENTAL` | [capacidades](capabilities.md) |
| `SetActualDateTime` apenas copia o relógio do PC para o relógio de início do OMSI (análise estática); o OmsiLaunch não o chama e define ele mesmo o relógio de início | por projeto | `calendar.set-actual-date-time` |
| As gravações de variáveis string, os triggers nomeados e os triggers de som passam pelos próprios métodos do OMSI (fechado estaticamente, BI-004); seu efeito nos scripts dos veículos aguarda a validação em runtime. Somente veículos rodoviários são alvos | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Sem realocação de veículos, sem religação espacial entre tiles, sem autoridade de transformação segura para o ODE; os campos de posição são somente leitura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` precisam de um PlayerVehicle; o início NEW_MAP headless não tem nenhum (uma situação salva fornece um) | por design (BI-007) | RV-004 |
| As mutações de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texturas D3D) não são registradas no journal nem restauradas | por design | [controle de runtime](runtime-control.md) |
| A obsolescência de handles é decidida a partir do número de instância do próprio OMSI, nunca reutilizado (`IDCode`), e, para humanos, a partir de despawn e respawn; é validada offline. A remoção natural (RV-002: carros de IA saindo da rede de caminhos carregada) ainda precisa ser observada em runtime, e nenhum evento `entity.removed` é emitido ainda | `PARTIAL` | [controle de runtime](runtime-control.md) |
| Os resultados são limitados pelo mailbox de 64 KiB: listas longas são truncadas (`truncated=true`); os payloads de pixels são limitados a 48 KiB por `d3d.texture.update` | por design | [capacidades](capabilities.md) |
| Canal de requisição única: uma requisição por vez por sessão; um slot ocupado resulta em `OL_E_RUNTIME_CHANNEL_BUSY`; ids de requisição não podem ser reutilizados | por design | [controle de runtime](runtime-control.md) |
| A telemetria é um anel de 64 eventos: um host que fica mais de 64 eventos para trás perde os mais antigos e é informado de quantos (`telemetry.lost`) | `EXPERIMENTAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| Os hooks do dispositivo Direct3D de um módulo são hooks de vtable: em uma instalação em que outro componente grava de volta os métodos originais do dispositivo (observado na instalação de regressão pouco depois do início do gameplay), eles deixam de ser chamados e não podem ser removidos na limpeza (`OL_E_MODULE_CLEANUP_INCOMPLETE`; o componente permanece carregado) | `PARTIAL` | [módulos de runtime](runtime-modules.md#native-components-and-hooks) |
| O carregamento de módulos de runtime é aberto: o OmsiLaunch não verifica quem criou um módulo; ele garante apenas que os módulos nunca executam dentro do processo do OmsiLaunch e que um programa de sessão fica confinado à sua tarefa (nenhum segundo processo, encerrado com a sessão) | por projeto (E21) | [módulos de runtime](runtime-modules.md#security-boundary) |
| O protocolo 2 de módulos (configurações, ativação padrão, o arquivo de configurações, programas de sessão, conflitos de plugins, avisos, `get_value`) é validado somente offline | `EXPERIMENTAL` | [módulos de runtime](runtime-modules.md) |
| O reset do dispositivo D3D foi observado em runtime (`resetting`, `restored`, invalidação de geração); uma transição `lost` distinta não foi produzida porque o dispositivo do OMSI foi diretamente para `DEVICENOTRESET` | `PARTIAL` (RV-007) | [status de validação em runtime](../status/runtime-validation-status.md) |
| As listas de runtime são paginadas (`cursor`, `limit`, `next_cursor`): uma página contém no máximo as linhas máximas da lista e o que cabe no slot de runtime de 64 KiB | `EXPERIMENTAL` | [capacidades](capabilities.md#pagination) |
| Alguns campos avançados do grafo de mapas/tiles/caminhos/objetos não são expostos; os leitores de runtime são snapshots tipados controlados pelo perfil, nunca acesso arbitrário à memória | por design | [capacidades](capabilities.md) |
| As leituras de memória dentro do processo seguem o padrão verificar-e-depois-usar contra um OMSI ativo; uma mutação concorrente do OMSI entre a verificação e a leitura pode gerar um snapshot inconsistente (`OL_E_RUNTIME_OPERATION_FAILED`) | risco aceito (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plano de controle local e modelo de confiança

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Modelo de confiança do mesmo usuário: o named pipe (`CurrentUserOnly`), os mapeamentos de memória de handoff/telemetria/runtime e o semáforo do lease são acessíveis a qualquer processo do mesmo usuário do Windows. Esse processo pode ler o status, parar a sessão ou executar operações de runtime depois de ler o `session_id`. | risco aceito (S-06, S-30) | [controle local](local-control.md) |
| O endpoint de controle só existe enquanto o proprietário está em `Running`; um cliente recebe `OL_E_NO_ACTIVE_SESSION` (saída 4) durante a inicialização e depois do fim da sessão | por design | [controle local](local-control.md) |
| Se outro processo já possuir o nome do pipe, o proprietário continua em execução sem endpoint (`ListenFault`), e uma segunda inicialização pode informar incorretamente `OL_E_SESSION_ALREADY_ACTIVE` | risco aceito | [controle local](local-control.md) |
| `.omsilaunch\` herda a ACL da raiz do OMSI; nenhum controle de acesso explícito é aplicado | risco aceito (S-31) | [transações e recuperação](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnósticos e saída

| Limitação | Estabilidade | Detalhe |
| --- | --- | --- |
| Os diagnósticos são apenas arquivos locais (`.omsilaunch\diagnostics`); nada é enviado e não há relatórios remotos | por design | [transações e recuperação](../concepts/transactions-and-recovery.md) |
| A retenção mantém as 50 sessões mais recentes; diagnósticos mais antigos prefixados por sessão são excluídos quando uma nova sessão inicia | por design | como acima |
| A saída JSON e os diagnósticos incluem caminhos da instalação (`RootPath`, diretórios de recursos, caminhos `.itx`) | por design (dados locais) | |
| A caixa de diálogo de falha de sessão do `OmsiLaunchW.exe` mostra o payload de falha do plugin (por exemplo `{"name":"world.failed",...}`) como mensagem, em vez de uma frase; a linha `Code:` está correta | cosmético | [bandeja do Windows](windows-tray.md) |
| Uma requisição D3D rejeitada pela ponte nativa antes de qualquer chamada Direct3D informa `native_status` corretamente, mas seu texto `detail` diz `HRESULT 0x00000000` | cosmético | [capacidades](capabilities.md) |
| A janela de status da bandeja é um snapshot da sessão planejada, tirado quando ela é aberta; ela não é atualizada e não mostra valores do OMSI em tempo real | por design | [bandeja do Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentação

As páginas em inglês em `docs/` são a documentação normativa desta release. `docs/localized/<locale>/` contém traduções das mesmas páginas `0.1.0-beta4` (veja [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); onde uma tradução diferir do texto em inglês, o texto em inglês e o código prevalecem. As páginas históricas e legadas listadas ali estão disponíveis somente em inglês.

Relacionadas: [capacidades](capabilities.md), [status de validação em runtime](../status/runtime-validation-status.md), [erros](errors.md).
