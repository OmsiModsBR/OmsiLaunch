# Ciclo de vida da sessão

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Tradução da [página original em inglês](../../../concepts/session-lifecycle.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página descreve como uma sessão do OmsiLaunch percorre `SessionState` de `Created` até `Completed` ou `Failed`: qual componente define cada estado, quais eventos de telemetria do plugin conduzem as transições, como funciona o timeout de inicialização, o que significa a parada (encerramento forçado), o que `WaitForAsync` retorna, quais estados são terminais, quais estados nunca ou quase nunca são observáveis e quais garantias o proprietário da CLI oferece. Tudo aqui foi extraído de `OmsiLaunchService.StartAsync`, `SuperviseAsync` e `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) e `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Páginas relacionadas: [API pública](../reference/public-api.md), [códigos de erro](../reference/errors.md), [transações e recuperação](transactions-and-recovery.md), [plugin permanente](permanent-plugin.md), [controle de runtime](../reference/runtime-control.md), [plano de controle local](../reference/local-control.md), [bandeja do Windows](../reference/windows-tray.md), [referência da CLI](../reference/cli.md), [status da validação em runtime](../status/runtime-validation-status.md), [o diretório `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

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

Valores na ordem de declaração. "Definido por" indica o código que chama `Move`/`Fail`; "Observável" diz se `GetStatusAsync`/`WaitForAsync` conseguem vê-lo na prática.

| # | Estado | Definido por | Observável | Significado |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (valor inicial da sessão ativa) | Brevemente | A sessão está registrada; nada aconteceu ainda. |
| 1 | `ValidatingPlatform` | ninguém | Não | Declarado, nunca definido pelo serviço atual (a validação da plataforma acontece em `PlanSessionAsync`, que não tem estado de sessão). |
| 2 | `Planning` | ninguém | Não | Declarado, nunca definido (o planejamento acontece antes de existir uma sessão; o replanejamento em `StartSessionAsync` também precede o registro). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Sim | O lease da instalação está sendo adquirido. Falha: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Sim | Um `journal.json` pendente é restaurado antes de a instalação ativa ser lida; o conjunto de arquivos do plugin permanente é validado (`plugin.integrity.reference`), é calculado o hash do `Omsi.exe`, os assets de splash são semeados e um `closecheck` obsoleto é removido. Falhas: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (processo registrado no journal ainda ativo). |
| 5 | `Snapshotting` | `StartAsync` | Praticamente não | Definido imediatamente antes de `ApplyingConfiguration`, sem nenhum await entre eles; o snapshot em si é feito dentro de `ApplyAsync`. Transitório e não observável. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Sim | `journal.json` é gravado (`Prepared`), os originais são copiados para backup, os overlays são gravados e as exclusões da sessão são removidas (`Applied`). Falhas: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, erros de E/S. |
| 7 | `DeployingRuntime` | `StartAsync` | Sim | Estado do journal `RuntimeDeployed`. Nenhum arquivo é implantado: o conjunto de arquivos do plugin é permanente. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Sim | O handoff (`OmsiLaunch.Handoff.<id>`), o slot de telemetria (`OmsiLaunch.Telemetry.<id>`) e o mailbox de runtime (`OmsiLaunch.Runtime.<id>`) existem; estado do journal `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Sim | `CreateProcessW` para `<root>\Omsi.exe` com diretório de trabalho `<root>`. Falhas: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Sim | O processo existe, `process.started` é registrado, o journal está em `ProcessStarted`, o supervisor é iniciado e `StartSessionAsync` retorna. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` em `plugin.started` | Sim | O plugin permanente leu um handoff válido para esta sessão. A partir daqui, um timeout é `OL_E_STARTUP_TIMEOUT` em vez de `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` em `world.starting` | Sim (somente NEW_MAP) | O plugin invocou o início nativo de NEW_MAP no thread de UI do OMSI. Situações salvas emitem `world.situation.starting`, que não é mapeado, portanto uma sessão SAVED_SITUATION passa de `PluginBootstrap` diretamente para `Running`. |
| 13 | `EnteringGameplay` | ninguém | Não | Declarado, nunca definido: `gameplay.entered` leva a sessão diretamente para `Running`. |
| 14 | `Running` | `ApplyTelemetry` em `gameplay.entered` | Sim | Gameplay alcançado. `ExecuteRuntimeAsync` é permitido; o proprietário da CLI abre o plano de controle local; a bandeja mostra uma sessão em execução. |
| 15 | `ProcessExited` | `SuperviseAsync` | Sim (somente sessões bem-sucedidas) | O OMSI terminou (naturalmente ou por encerramento forçado), o journal está em `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (e o caminho de falha na inicialização) | Sim (somente sessões bem-sucedidas) | Todo arquivo pertencente à sessão é restaurado a partir de seu backup verificado; os artefatos da sessão são removidos. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Sim (somente sessões bem-sucedidas) | Restauração verificada, journal e backups removidos; os stores de runtime estão prestes a ser descartados. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Sim, terminal | Stores descartados, mailbox fechado, lease liberado, nenhuma falha registrada. |
| 19 | `Failed` | `LiveSession.Fail` a partir de `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Sim, terminal | Um diagnóstico de falha foi registrado. O estado é persistente: chamadas posteriores a `Move` são ignoradas, portanto uma sessão com falha nunca mostra `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, embora o encerramento e a restauração ainda sejam executados. |

Estados terminais: `Completed` e `Failed`. Depois de qualquer um deles, `WaitForAsync` retorna imediatamente e `CloseAsync` retorna sem solicitar uma parada.

Verificação de "nunca definido": uma busca no código-fonte por `SessionState.ValidatingPlatform`, `SessionState.Planning` e `SessionState.EnteringGameplay` encontra apenas a declaração do enum; `SessionState.Snapshotting` aparece uma vez, imediatamente seguido de `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Fase de inicialização (`StartSessionAsync`)

1. Rejeitar um plano não apto para execução (`OL_E_PLAN_NOT_RUNNABLE`), replanejar a especificação (recalculando o hash do `Omsi.exe`, resolvendo o conteúdo novamente, verificando de novo o conjunto de arquivos do plugin) e rejeitar novamente se ele não estiver mais apto para execução. Registrar a sessão ativa (`Created`).
2. Criar o trace do host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (arquivos mais antigos com prefixo de sessão além das 50 sessões mais recentes são eliminados). Rejeitar `StartupTimeoutSeconds` fora de 1..600 (`ArgumentOutOfRangeException`; o registro da sessão é desfeito).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → recuperar um journal pendente (um journal anterior aos fingerprints que não consegue provar a propriedade é adiado e tentado de novo assim que os overlays desta sessão existirem), validar o conjunto de arquivos do plugin permanente, calcular o hash do executável, remover um `closecheck` obsoleto, montar a transação (overlays: patches de `options.cfg`, BMPs de splash gerenciado, `Texture\standard.itx`; exclusões: alvos de ITX, `Texture\standard.ipr`, `closecheck` quando ele não existe).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`; em seguida a tarefa do supervisor é iniciada e o handle é retornado.
5. Qualquer exceção nas etapas 3–4 é capturada: a sessão fica `Failed` com `OL_E_START_SESSION` (mensagem interna), um processo já criado é encerrado e aguardado, os stores são descartados, a transação é restaurada (`OL_E_RESTORE_FAILED` em caso de falha) ou, se não foi possível confirmar a saída do OMSI, mantida pendente com `OL_E_RESTORE_DEFERRED`; o lease é liberado. `StartSessionAsync` ainda retorna o handle nesse caso; consulte `GetStatusAsync`.

O replanejamento mantém o `SessionId` do chamador, portanto o id no handle é igual a `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Supervisão (`SuperviseAsync`)

O supervisor é executado em uma tarefa do thread pool e repete o ciclo a cada 100 ms até o OMSI terminar ou uma parada ser solicitada:

1. Ler todo evento de telemetria que o plugin confirmou desde a última leitura, em ordem (um anel de eventos; um leitor que ficou para trás recebe `telemetry.lost` com a contagem). Cada evento é anexado a `RuntimeEvents` e mapeado por `ApplyTelemetry`.
2. Se a sessão estiver `Failed`, sair do ciclo.
3. Se a sessão ainda não estiver `Running` e o prazo (`StartupTimeoutSeconds` após a entrada do supervisor) tiver passado: `Fail` com `OL_E_STARTUP_TIMEOUT` quando `PluginBootstrap` foi alcançado, caso contrário `OL_E_PLUGIN_NOT_LOADED`; sair do ciclo.

Após o ciclo: se o OMSI terminou antes de `Running` e nenhuma falha foi registrada, `Fail` com `OL_E_PROCESS_EXITED_EARLY`. Então, independentemente de a sessão ter falhado ou não: encerrar o OMSI se ele ainda estiver ativo, aguardar a saída, marcar o journal como `ProcessExited`, passar para `ProcessExited`, restaurar (`Restoring` → `CleaningRuntime`) ou registrar `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, descartar o handle do processo, o handoff, o slot de telemetria e o mailbox de runtime, liberar o lease e passar para `Completed`, a menos que o estado seja `Failed`. Uma falha dentro do próprio supervisor é registrada como `OL_E_PROCESS_SUPERVISION` (problemas de limpeza como `OL_E_PROCESS_CLEANUP_FAILED`) e o mesmo caminho de encerramento/restauração é executado.

Como `Failed` é persistente, a única evidência de que uma sessão com falha foi restaurada é a ausência de `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` em seus diagnósticos (e a ausência de `journal.json`); as notas de restauração (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecem em ambos os casos.

<a id="telemetry-events"></a>
## Eventos de telemetria

O plugin publica amostras JSON `{ "name": ..., "data": {...} }` no slot de telemetria; o host registra cada nova amostra como um `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emitido por | Ação do host |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` após ler um handoff válido | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: sem `OMSILAUNCH_HANDOFF_NAME`, handoff ilegível ou não verificável | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: o handoff pede um modo de mundo diferente de NEW_MAP/SAVED_SITUATION, início não headless, veículo do jogador, modos de data/hora ou uma identidade de situação vazia | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: a validação do build dentro do processo falhou | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | apenas registrado |
| `headless.arm.failed` | `PluginRuntime.Start`: não foi possível armar o hook nativo de início headless | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | apenas registrado |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` quando `InternetTextures.Mode` é `Disabled` | apenas registrado |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 ou 4) | NEW_MAP: o OMSI ainda não está pronto; o início é tentado de novo no próximo tick do timer de UI | apenas registrado |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | caminho de sucesso de NEW_MAP | apenas registrado |
| `world.failed` (`native_status`) | NEW_MAP: o início nativo retornou uma falha | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | caminho de SAVED_SITUATION | apenas registrado (sem mudança de estado) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: o início nativo retornou uma falha | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: campos da seleção do ponto de entrada ou `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | fim do início do mundo | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` depois que qualquer operação `d3d.*` ativou o probe | apenas registrado |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` quando a reaplicação de um `camera.lock` ativo lança uma exceção (relatado uma vez por erro distinto) | apenas registrado |
| JSON inválido | qualquer um | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Ressalvas: o slot guarda uma única amostra, portanto eventos emitidos dentro de uma mesma consulta de 100 ms do host podem ser perdidos (o plugin suprime eventos de ciclo de vida por 2 s após `gameplay.entered` e nunca publica um evento D3D no mesmo tick que publica `gameplay.entered`, de modo que a fronteira de `Running` não é perdida). `RuntimeEvents` mantém os 256 eventos mais recentes; os mais antigos são descartados. Não é um log sem perdas. Leia os eventos por meio de `GetStatusAsync`, `session.events` no plano de controle ou `events read|watch` na CLI.

<a id="startup-timeout"></a>
## Timeout de inicialização

| Item | Valor |
| --- | --- |
| Origem | `LaunchSpec.Behavior.StartupTimeoutSeconds` (padrão 180; 1..600; CLI `/startup-timeout`, perfil `behavior.startup-timeout`). |
| Início da contagem | Quando a tarefa do supervisor entra em seu ciclo (depois que o handle foi retornado). |
| Expiração antes de `PluginBootstrap` | `Failed` com `OL_E_PLUGIN_NOT_LOADED`. |
| Expiração após `PluginBootstrap`, antes de `Running` | `Failed` com `OL_E_STARTUP_TIMEOUT`. |
| Após `Running` | Nenhum timeout se aplica; a sessão dura até o OMSI terminar ou uma parada ser solicitada. |
| Proprietário da CLI | Aguarda `StartupTimeoutSeconds + 5` segundos por `Running`; em caso de falha imprime o status, sob `OmsiLaunchW.exe` mostra uma caixa de diálogo com o último diagnóstico `OL_E_` (código de fallback `OL_E_SESSION_START_FAILED`) e sai com 1 após `CloseAsync`. |

`ShutdownTimeoutSeconds` é transportado na especificação, mas não é consumido: não há espera de desligamento.

<a id="stop-semantics"></a>
## Semântica de parada

Toda solicitação de parada é a mesma solicitação canônica: `StopAsync(handle)` pela API, `CloseAsync` em uma sessão não terminal, `session.stop` no plano de controle local (vinculado ao id da sessão ativa), "End session" (encerrar sessão) na bandeja, Ctrl+C ou fechamento do console no proprietário da CLI, e o fim de `/observe-seconds`.

| Etapa | Detalhe |
| --- | --- |
| 1 | `StopRequested` é definido na sessão ativa; o chamador retorna imediatamente. |
| 2 | Em até 100 ms o supervisor sai de seu ciclo e chama `TerminateProcess(Omsi.exe, 1)`. Trata-se de um encerramento forçado: a rotina de desligamento do OMSI não é executada, o OMSI não regrava `options.cfg` e nenhuma caixa de diálogo de salvamento aparece. Isso é intencional, para que o OMSI não possa sobrescrever arquivos que a transação está prestes a restaurar. |
| 3 | O supervisor aguarda a saída do processo, registra `ProcessExited`, restaura exatamente todo arquivo pertencente à sessão (incluindo o marcador `closecheck` que o OMSI gravou durante a sessão, que se torna uma nota `restore.session-artifact-removed`), remove o journal e os backups, descarta os stores de runtime (chamadas posteriores a `ExecuteRuntimeAsync` lançam `OL_E_RUNTIME_CHANNEL_CLOSED` ou `OL_E_SESSION_NOT_RUNNING`), libera o lease e passa para `Completed`. |
| Saída natural | Se o OMSI terminar por conta própria após `Running` (o usuário fecha o OMSI), o mesmo caminho é executado sem encerramento forçado e a sessão é concluída normalmente. Antes de `Running`, é `OL_E_PROCESS_EXITED_EARLY`. |
| Desligamento cooperativo | Não implementado. O envio de `WM_CLOSE` com espera de `ShutdownTimeoutSeconds` não está implementado (decisão de produto; o OMSI ignorou `WM_CLOSE` enviado à sua janela principal na rodada de fechamento de runtime, `L05b`) ([status da validação em runtime](../status/runtime-validation-status.md)). |
| Estado do lado do runtime | Tudo o que foi alterado por operações de runtime (relógio, câmera, veículos gerados, variáveis de script, texturas D3D) é estado interno do processo e desaparece com o processo; nunca é restaurado nem persistido. |

<a id="waitforasync-semantics"></a>
## Semântica de `WaitForAsync`

| Situação | Resultado |
| --- | --- |
| A sessão alcança o estado solicitado | Retorna o status com `State == requested`. |
| A sessão alcança antes um estado terminal | Retorna imediatamente com `Completed` ou `Failed` (verifique `Diagnostics`). |
| O timeout se esgota | Retorna o status atual (sem exceção). Compare `State` com o que você solicitou. |
| O estado solicitado já passou (ou nunca é definido: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, na prática `Snapshotting`) | Aguarda até um estado terminal ou o timeout. |
| O chamador cancela | `OperationCanceledException`. |
| Handle desconhecido ou fechado | `KeyNotFoundException`. |

O intervalo de consulta é de 100 ms, portanto as transições observadas atrasam em até 100 ms em relação às reais.

<a id="owner-lifecycle-guarantees-cli"></a>
## Garantias do ciclo de vida do proprietário (CLI)

`OwnerSession.RunAsync` em `tools/OmsiLaunch.Cli/Program.cs` é o proprietário de referência.

| Garantia | Detalhe |
| --- | --- |
| Proprietário único | Antes de iniciar, a CLI sonda o pipe de controle; se um proprietário responder, ela recusa com `OL_E_SESSION_ALREADY_ACTIVE` (saída 7). O lease impõe a mesma regra entre processos. |
| Todo caminho de saída chega a `CloseAsync` | A partir de `StartSessionAsync`, exceções, Ctrl+C (`CancelKeyPress`), fechamento do console/logoff (`ProcessExit`: a parada é solicitada e o proprietário aguarda até 4 s por `Completed`; o que restar é recuperado pelo journal na próxima inicialização), parada pela bandeja, `session.stop` do plano de controle, expiração de `/observe-seconds` e conclusão natural terminam todos no bloco `finally`, que descarta o plano de controle e a bandeja e aguarda `CloseAsync`. |
| `/observe-seconds` é um limite superior | Solicitações de parada pela bandeja ou pelo plano de controle ainda encerram a sessão antes. |
| Plano de controle somente durante Running | O endpoint do named pipe é criado após `Running` (e após quaisquer lotes de validação `INTERNAL`) e descartado antes de `CloseAsync`; nos demais momentos os clientes recebem `OL_E_NO_ACTIVE_SESSION`. |
| Código de saída | 0 quando o estado final é `Completed`, 1 quando é `Failed` ou o gameplay não foi alcançado, 8 quando uma recuperação solicitada não foi concluída ([códigos de saída](../reference/exit-codes.md)). |
| Diagnósticos | Trace do host e artefatos de operações de runtime em `<root>\.omsilaunch\diagnostics`, log da bandeja `tray-host.log`; nenhum dado sai da máquina. |

Integradores que escrevem seu próprio proprietário devem reproduzir as duas primeiras garantias: um `StartSessionAsync` por instalação de cada vez e `CloseAsync` em todo caminho.

<a id="failure-map"></a>
## Mapa de falhas

| Fase | Estado na falha | Diagnósticos que você verá |
| --- | --- | --- |
| Plano | nenhum (sem sessão) | `OL_E_PLAN_NOT_RUNNABLE` lançado por `StartSessionAsync`; os próprios códigos `OL_E_` do plano ([validação de LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Inicialização (do lease à criação do processo) | `Failed` | `OL_E_START_SESSION` com o código interno; possivelmente `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Bootstrap do plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Início do mundo | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Em execução | `Failed` somente em falhas do supervisor | `OL_E_PROCESS_SUPERVISION`; erros de operações de runtime nunca fazem a sessão falhar. |
| Encerramento e restauração | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Todo caminho de falha ainda tenta o encerramento e a restauração; um journal que permaneça é recuperado na próxima inicialização ou por `RecoverPendingAsync` / `/recover` ([transações e recuperação](transactions-and-recovery.md)).
