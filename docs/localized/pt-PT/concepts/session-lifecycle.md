# Ciclo de vida da sessão

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Tradução da [página original em inglês](../../../concepts/session-lifecycle.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página descreve como uma sessão do OmsiLaunch percorre `SessionState` desde `Created` até `Completed` ou `Failed`: que componente define cada estado, que eventos de telemetria do plugin provocam as transições, como funciona o timeout de arranque, o que significa parar (terminação forçada), o que `WaitForAsync` devolve, que estados são terminais, que estados nunca ou quase nunca são observáveis e que garantias o proprietário CLI oferece. Tudo o que aqui se descreve foi extraído de `OmsiLaunchService.StartAsync`, `SuperviseAsync` e `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) e `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Páginas relacionadas: [API pública](../reference/public-api.md), [códigos de erro](../reference/errors.md), [transações e recuperação](transactions-and-recovery.md), [plugin permanente](permanent-plugin.md), [controlo de runtime](../reference/runtime-control.md), [plano de controlo local](../reference/local-control.md), [área de notificação do Windows](../reference/windows-tray.md), [referência da CLI](../reference/cli.md), [estado da validação em runtime](../status/runtime-validation-status.md), [o diretório `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Visão geral

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## Referência de `SessionState`

Valores pela ordem de declaração. «Definido por» indica o código que chama `Move`/`Fail`; «Observável» indica se `GetStatusAsync`/`WaitForAsync` o conseguem ver na prática.

| # | Estado | Definido por | Observável | Significado |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (valor inicial da sessão ativa) | Brevemente | A sessão está registada; ainda não aconteceu nada. |
| 1 | `ValidatingPlatform` | ninguém | Não | Declarado, nunca definido pelo serviço atual (a validação da plataforma acontece em `PlanSessionAsync`, que não tem estado de sessão). |
| 2 | `Planning` | ninguém | Não | Declarado, nunca definido (o planeamento acontece antes de existir uma sessão; o novo planeamento em `StartSessionAsync` também precede o registo). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Sim | O lease da instalação está a ser adquirido. Falha: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Sim | Um `journal.json` pendente é restaurado antes de a instalação ativa ser lida; o conjunto do plugin permanente é validado (`plugin.integrity.reference`), é calculado o hash de `Omsi.exe`, os recursos do splash são preparados e um `closecheck` obsoleto é removido. Falhas: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (processo registado no journal ainda vivo). |
| 5 | `Snapshotting` | `StartAsync` | Praticamente não | Definido imediatamente antes de `ApplyingConfiguration`, sem nenhum await entre ambos; o snapshot propriamente dito é tirado dentro de `ApplyAsync`. Transitório e não observável. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Sim | `journal.json` é escrito (`Prepared`), é feita cópia de segurança dos originais, os overlays são escritos e as eliminações da sessão são removidas (`Applied`). Falhas: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, erros de E/S. |
| 7 | `DeployingRuntime` | `StartAsync` | Sim | Estado do journal `RuntimeDeployed`. Nenhum ficheiro é implementado: o conjunto do plugin é permanente. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Sim | O handoff (`OmsiLaunch.Handoff.<id>`), o slot de telemetria (`OmsiLaunch.Telemetry.<id>`) e a mailbox de runtime (`OmsiLaunch.Runtime.<id>`) existem; estado do journal `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Sim | `CreateProcessW` para `<root>\Omsi.exe` com o diretório de trabalho `<root>`. Falhas: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Sim | O processo existe, `process.started` está registado, o journal está em `ProcessStarted`, o supervisor foi iniciado e `StartSessionAsync` retorna. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` em `plugin.started` | Sim | O plugin permanente leu um handoff válido para esta sessão. A partir daqui, um timeout é `OL_E_STARTUP_TIMEOUT` em vez de `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` em `world.starting` | Sim (apenas NEW_MAP) | O plugin invocou o arranque nativo NEW_MAP na thread de UI do OMSI. As situações guardadas emitem `world.situation.starting`, que não está mapeado, pelo que uma sessão SAVED_SITUATION passa diretamente de `PluginBootstrap` para `Running`. |
| 13 | `EnteringGameplay` | ninguém | Não | Declarado, nunca definido: `gameplay.entered` passa a sessão diretamente para `Running`. |
| 14 | `Running` | `ApplyTelemetry` em `gameplay.entered` | Sim | Jogabilidade alcançada. `ExecuteRuntimeAsync` é permitido; o proprietário CLI abre o plano de controlo local; a área de notificação mostra uma sessão em execução. |
| 15 | `ProcessExited` | `SuperviseAsync` | Sim (apenas sessões bem-sucedidas) | O OMSI terminou (naturalmente ou por terminação), o journal está em `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (e o caminho de falha no arranque) | Sim (apenas sessões bem-sucedidas) | Cada ficheiro pertencente à sessão é restaurado a partir da sua cópia de segurança verificada; os artefactos da sessão são removidos. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Sim (apenas sessões bem-sucedidas) | Restauro verificado, journal e cópias de segurança removidos; os armazenamentos de runtime estão prestes a ser libertados. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Sim, terminal | Armazenamentos libertados, mailbox fechada, lease libertado, nenhuma falha registada. |
| 19 | `Failed` | `LiveSession.Fail` a partir de `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Sim, terminal | Foi registado um diagnóstico de falha. O estado é persistente: as chamadas `Move` posteriores são ignoradas, pelo que uma sessão falhada nunca mostra `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, embora a terminação e o restauro continuem a ser executados. |

Estados terminais: `Completed` e `Failed`. Depois de qualquer um deles, `WaitForAsync` retorna imediatamente e `CloseAsync` retorna sem pedir uma paragem.

Verificação de «nunca definido»: uma pesquisa no código-fonte por `SessionState.ValidatingPlatform`, `SessionState.Planning` e `SessionState.EnteringGameplay` encontra apenas a declaração do enum; `SessionState.Snapshotting` aparece uma vez, imediatamente seguido de `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Fase de arranque (`StartSessionAsync`)

1. Rejeitar um plano não executável (`OL_E_PLAN_NOT_RUNNABLE`), voltar a planear a especificação (recalculando o hash de `Omsi.exe`, voltando a resolver os conteúdos e voltando a verificar o conjunto do plugin) e rejeitar de novo se já não for executável. Registar a sessão ativa (`Created`).
2. Criar o rastreio do anfitrião `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (os ficheiros mais antigos com prefixo de sessão, para além das 50 sessões mais recentes, são eliminados). Rejeitar `StartupTimeoutSeconds` fora de 1..600 (`ArgumentOutOfRangeException`; o registo da sessão é anulado).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → recuperar um journal pendente (um journal anterior às impressões digitais que não consiga provar a propriedade é adiado e tentado de novo assim que existirem os overlays desta sessão), validar o conjunto do plugin permanente, calcular o hash do executável, remover um `closecheck` obsoleto, construir a transação (overlays: patches de `options.cfg`, BMPs do splash gerido, `Texture\standard.itx`; eliminações: alvos ITX, `Texture\standard.ipr`, `closecheck` quando não existe).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`; depois, a tarefa do supervisor é iniciada e o handle é devolvido.
5. Qualquer exceção nos passos 3–4 é capturada: a sessão fica `Failed` com `OL_E_START_SESSION` (mensagem interna), um processo já criado é terminado e aguardado, os armazenamentos são libertados, a transação é restaurada (`OL_E_RESTORE_FAILED` em caso de falha) ou, se não foi possível confirmar a saída do OMSI, é deixada pendente com `OL_E_RESTORE_DEFERRED`; o lease é libertado. Mesmo neste caso, `StartSessionAsync` devolve o handle; deve-se ler `GetStatusAsync`.

O novo planeamento mantém o `SessionId` do chamador, pelo que o id no handle é igual a `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Supervisão (`SuperviseAsync`)

O supervisor é executado numa tarefa do thread pool e repete o ciclo a cada 100 ms até o OMSI ter terminado ou ter sido pedida uma paragem:

1. Ler todos os eventos de telemetria que o plugin confirmou desde a última leitura, por ordem (um anel de eventos; um leitor que ficou para trás recebe `telemetry.lost` com a contagem). Cada evento é acrescentado a `RuntimeEvents` e mapeado por `ApplyTelemetry`.
2. Se a sessão estiver `Failed`, sair do ciclo.
3. Se a sessão ainda não estiver `Running` e o prazo (`StartupTimeoutSeconds` após a entrada do supervisor) tiver expirado: `Fail` com `OL_E_STARTUP_TIMEOUT` quando `PluginBootstrap` foi alcançado, caso contrário `OL_E_PLUGIN_NOT_LOADED`; sair do ciclo.

Depois do ciclo: se o OMSI terminou antes de `Running` e não foi registada nenhuma falha, `Fail` com `OL_E_PROCESS_EXITED_EARLY`. Depois, quer a sessão tenha falhado quer não: terminar o OMSI se ainda estiver vivo, aguardar a saída, marcar o journal como `ProcessExited`, passar para `ProcessExited`, restaurar (`Restoring` → `CleaningRuntime`) ou registar `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, libertar o handle do processo, o handoff, o slot de telemetria e a mailbox de runtime, libertar o lease e passar para `Completed`, exceto se o estado for `Failed`. Uma falha dentro do próprio supervisor é registada como `OL_E_PROCESS_SUPERVISION` (problemas de limpeza como `OL_E_PROCESS_CLEANUP_FAILED`) e é executado o mesmo caminho de terminação/restauro.

Como `Failed` é persistente, a única evidência de que uma sessão falhada foi restaurada é a ausência de `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` nos seus diagnósticos (e a ausência de `journal.json`); as notas de restauro (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecem em ambos os casos.

<a id="telemetry-events"></a>
## Eventos de telemetria

O plugin publica amostras JSON `{ "name": ..., "data": {...} }` no slot de telemetria; o anfitrião regista cada nova amostra como um `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emitido por | Ação do anfitrião |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` depois de ler um handoff válido | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: sem `OMSILAUNCH_HANDOFF_NAME`, handoff ilegível ou não verificável | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: o handoff pede um modo de mundo diferente de NEW_MAP/SAVED_SITUATION, um arranque não headless, veículo do jogador, modos de data/hora ou uma identidade de situação vazia | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: a validação da build dentro do processo falhou | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | apenas registado |
| `headless.arm.failed` | `PluginRuntime.Start`: não foi possível armar o hook nativo de arranque headless | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | apenas registado |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` quando `InternetTextures.Mode` é `Disabled` | apenas registado |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 ou 4) | NEW_MAP: o OMSI ainda não está pronto; o arranque é tentado de novo no próximo tick do temporizador de UI | apenas registado |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | Caminho de sucesso NEW_MAP | apenas registado |
| `world.failed` (`native_status`) | NEW_MAP: o arranque nativo devolveu uma falha | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | Caminho SAVED_SITUATION | apenas registado (sem mudança de estado) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: o arranque nativo devolveu uma falha | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: campos de seleção do ponto de entrada ou `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | fim do arranque do mundo | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` assim que qualquer operação `d3d.*` tenha ativado a sonda | apenas registado |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` quando reaplicar um `camera.lock` ativo lança uma exceção (comunicado uma vez por erro distinto) | apenas registado |
| JSON inválido | qualquer | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Ressalvas: o slot contém uma única amostra, pelo que eventos emitidos dentro de uma mesma consulta de 100 ms do anfitrião podem perder-se (o plugin suprime os eventos de ciclo de vida durante 2 s após `gameplay.entered` e nunca publica um evento D3D no tick que publica `gameplay.entered`, pelo que a fronteira de `Running` não é perdida). `RuntimeEvents` mantém os 256 eventos mais recentes; os mais antigos são descartados. Não é um registo sem perdas. Os eventos devem ser lidos através de `GetStatusAsync`, de `session.events` no plano de controlo ou de `events read|watch` na CLI.

<a id="startup-timeout"></a>
## Timeout de arranque

| Item | Valor |
| --- | --- |
| Origem | `LaunchSpec.Behavior.StartupTimeoutSeconds` (predefinição 180; 1..600; CLI `/startup-timeout`, perfil `behavior.startup-timeout`). |
| Início da contagem | Quando a tarefa do supervisor entra no seu ciclo (depois de o handle ter sido devolvido). |
| Expiração antes de `PluginBootstrap` | `Failed` com `OL_E_PLUGIN_NOT_LOADED`. |
| Expiração depois de `PluginBootstrap`, antes de `Running` | `Failed` com `OL_E_STARTUP_TIMEOUT`. |
| Depois de `Running` | Não se aplica nenhum timeout; a sessão dura até o OMSI terminar ou ser pedida uma paragem. |
| Proprietário CLI | Aguarda `StartupTimeoutSeconds + 5` segundos por `Running`; em caso de falha imprime o estado, sob `OmsiLaunchW.exe` mostra uma caixa de diálogo com o último diagnóstico `OL_E_` (código de recurso `OL_E_SESSION_START_FAILED`) e sai com 1 após `CloseAsync`. |

`ShutdownTimeoutSeconds` é transportado na especificação, mas não é consumido: não existe espera de encerramento.

<a id="stop-semantics"></a>
## Semântica da paragem

Todos os pedidos de paragem são o mesmo pedido canónico: `StopAsync(handle)` a partir da API, `CloseAsync` numa sessão não terminal, `session.stop` no plano de controlo local (vinculado ao id da sessão ativa), "End session" na área de notificação, Ctrl+C ou fecho da consola no proprietário CLI, e o fim de `/observe-seconds`.

| Passo | Detalhe |
| --- | --- |
| 1 | `StopRequested` é definido na sessão ativa; o chamador retorna imediatamente. |
| 2 | Em menos de 100 ms, o supervisor sai do seu ciclo e chama `TerminateProcess(Omsi.exe, 1)`. Trata-se de uma terminação forçada: a rotina de encerramento do OMSI não é executada, o OMSI não reescreve `options.cfg` e não aparece nenhuma caixa de diálogo para guardar. Isto é intencional, para que o OMSI não possa escrever por cima dos ficheiros que a transação está prestes a restaurar. |
| 3 | O supervisor aguarda a saída do processo, regista `ProcessExited`, restaura exatamente cada ficheiro pertencente à sessão (incluindo o marcador `closecheck` que o OMSI escreveu durante a sessão, que se torna uma nota `restore.session-artifact-removed`), remove o journal e as cópias de segurança, liberta os armazenamentos de runtime (as chamadas posteriores a `ExecuteRuntimeAsync` lançam `OL_E_RUNTIME_CHANNEL_CLOSED` ou `OL_E_SESSION_NOT_RUNNING`), liberta o lease e passa para `Completed`. |
| Saída natural | Se o OMSI terminar por si próprio depois de `Running` (o utilizador fecha o OMSI), é executado o mesmo caminho sem terminação e a sessão conclui normalmente. Antes de `Running`, é `OL_E_PROCESS_EXITED_EARLY`. |
| Encerramento cooperativo | Não implementado. Enviar `WM_CLOSE` e aguardar `ShutdownTimeoutSeconds` não está implementado (decisão de produto; o OMSI ignorou `WM_CLOSE` enviado para a sua janela principal na ronda de fecho de runtime, `L05b`) ([estado da validação em runtime](../status/runtime-validation-status.md)). |
| Estado do lado do runtime | Tudo o que é alterado através de operações de runtime (relógio, câmara, veículos criados, variáveis de script, texturas D3D) é estado dentro do processo e desaparece com o processo; nunca é restaurado nem persistido. |

<a id="waitforasync-semantics"></a>
## Semântica de `WaitForAsync`

| Situação | Resultado |
| --- | --- |
| A sessão alcança o estado pedido | Devolve o estado com `State == requested`. |
| A sessão alcança primeiro um estado terminal | Retorna imediatamente com `Completed` ou `Failed` (verificar `Diagnostics`). |
| O timeout expira | Devolve o estado atual (sem exceção). Comparar `State` com o que foi pedido. |
| O estado pedido já passou (ou nunca é definido: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, na prática `Snapshotting`) | Aguarda até um estado terminal ou até ao timeout. |
| O chamador cancela | `OperationCanceledException`. |
| Handle desconhecido ou fechado | `KeyNotFoundException`. |

O intervalo de consulta é de 100 ms, pelo que as transições observadas ficam atrasadas até 100 ms em relação às reais.

<a id="owner-lifecycle-guarantees-cli"></a>
## Garantias do ciclo de vida do proprietário (CLI)

`OwnerSession.RunAsync` em `tools/OmsiLaunch.Cli/Program.cs` é o proprietário de referência.

| Garantia | Detalhe |
| --- | --- |
| Proprietário único | Antes de iniciar, a CLI sonda o pipe de controlo; se um proprietário responder, recusa com `OL_E_SESSION_ALREADY_ACTIVE` (saída 7). O lease impõe a mesma regra entre processos. |
| Todos os caminhos de saída chegam a `CloseAsync` | A partir de `StartSessionAsync`, exceções, Ctrl+C (`CancelKeyPress`), fecho da consola/fim de sessão do Windows (`ProcessExit`: é pedida a paragem e o proprietário aguarda até 4 s por `Completed`; o que ficar por fazer é recuperado pelo journal no próximo arranque), paragem a partir da área de notificação, `session.stop` do plano de controlo, expiração de `/observe-seconds` e conclusão natural terminam todos no bloco `finally`, que liberta o plano de controlo e a área de notificação e aguarda `CloseAsync`. |
| `/observe-seconds` é um limite superior | Os pedidos de paragem a partir da área de notificação ou do plano de controlo continuam a terminar a sessão mais cedo. |
| Plano de controlo apenas durante Running | O endpoint do named pipe é criado depois de `Running` (e depois de quaisquer lotes de validação `INTERNAL`) e libertado antes de `CloseAsync`; noutras alturas os clientes recebem `OL_E_NO_ACTIVE_SESSION`. |
| Código de saída | 0 quando o estado final é `Completed`, 1 quando é `Failed` ou a jogabilidade não foi alcançada, 8 quando uma recuperação pedida não foi concluída ([códigos de saída](../reference/exit-codes.md)). |
| Diagnósticos | Rastreio do anfitrião e artefactos de operações de runtime em `<root>\.omsilaunch\diagnostics`, registo da área de notificação `tray-host.log`; nenhum dado sai da máquina. |

Os integradores que escrevam o seu próprio proprietário têm de reproduzir as duas primeiras garantias: um único `StartSessionAsync` por instalação de cada vez, e `CloseAsync` em todos os caminhos.

<a id="failure-map"></a>
## Mapa de falhas

| Fase | Estado ao falhar | Diagnósticos que aparecem |
| --- | --- | --- |
| Plano | nenhum (sem sessão) | `OL_E_PLAN_NOT_RUNNABLE` lançado por `StartSessionAsync`; os próprios códigos `OL_E_` do plano ([validação de LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Arranque (do lease até à criação do processo) | `Failed` | `OL_E_START_SESSION` com o código interno; possivelmente `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Bootstrap do plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Arranque do mundo | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Em execução | `Failed` apenas em falhas do supervisor | `OL_E_PROCESS_SUPERVISION`; os erros de operações de runtime nunca fazem falhar a sessão. |
| Terminação e restauro | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Todos os caminhos de falha continuam a tentar a terminação e o restauro; um journal que permaneça é recuperado no próximo arranque ou por `RecoverPendingAsync` / `/recover` ([transações e recuperação](transactions-and-recovery.md)).
