# Ciclo di vita della sessione

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Traduzione della [pagina originale in inglese](../../../concepts/session-lifecycle.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina descrive come una sessione OmsiLaunch attraversa `SessionState` da `Created` a `Completed` o `Failed`: quale componente imposta ciascuno stato, quali eventi di telemetria del plugin guidano le transizioni, come funziona il timeout di avvio, che cosa significa l'arresto (terminazione forzata), che cosa restituisce `WaitForAsync`, quali stati sono terminali, quali stati non sono mai o quasi mai osservabili e quali garanzie offre l'owner della CLI. Tutto il contenuto è tratto da `OmsiLaunchService.StartAsync`, `SuperviseAsync` e `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) e `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Pagine correlate: [API pubblica](../reference/public-api.md), [codici di errore](../reference/errors.md), [transazioni e recupero](transactions-and-recovery.md), [plugin permanente](permanent-plugin.md), [controllo runtime](../reference/runtime-control.md), [piano di controllo locale](../reference/local-control.md), [area di notifica di Windows](../reference/windows-tray.md), [riferimento CLI](../reference/cli.md), [stato della validazione a runtime](../status/runtime-validation-status.md), [la directory `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Panoramica

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
## Riferimento di `SessionState`

Valori nell'ordine di dichiarazione. «Impostato da» indica il codice che chiama `Move`/`Fail`; «Osservabile» indica se `GetStatusAsync`/`WaitForAsync` possono vederlo in pratica.

| # | Stato | Impostato da | Osservabile | Significato |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (valore iniziale della sessione attiva) | Brevemente | La sessione è registrata; non è ancora successo nulla. |
| 1 | `ValidatingPlatform` | nessuno | No | Dichiarato, mai impostato dal servizio attuale (la validazione della piattaforma avviene in `PlanSessionAsync`, che non ha stato di sessione). |
| 2 | `Planning` | nessuno | No | Dichiarato, mai impostato (la pianificazione avviene prima che esista una sessione; anche la ripianificazione in `StartSessionAsync` precede la registrazione). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Sì | È in corso l'acquisizione del lease dell'installazione. Errore: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Sì | Un `journal.json` in sospeso viene ripristinato prima che l'installazione attiva venga letta; la chiusura del plugin permanente viene validata (`plugin.integrity.reference`), viene calcolato l'hash di `Omsi.exe`, vengono predisposti gli asset della splash screen e viene rimosso un `closecheck` obsoleto. Errori: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (processo registrato nel journal ancora attivo). |
| 5 | `Snapshotting` | `StartAsync` | In pratica no | Impostato immediatamente prima di `ApplyingConfiguration` senza alcun await intermedio; lo snapshot vero e proprio viene acquisito all'interno di `ApplyAsync`. Transitorio e non osservabile. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Sì | Viene scritto `journal.json` (`Prepared`), viene eseguito il backup degli originali, vengono scritti gli overlay e rimossi i file oggetto di eliminazione di sessione (`Applied`). Errori: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, errori di I/O. |
| 7 | `DeployingRuntime` | `StartAsync` | Sì | Stato del journal `RuntimeDeployed`. Nessun file viene distribuito: la chiusura del plugin è permanente. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Sì | L'handoff (`OmsiLaunch.Handoff.<id>`), lo slot di telemetria (`OmsiLaunch.Telemetry.<id>`) e la mailbox runtime (`OmsiLaunch.Runtime.<id>`) esistono; stato del journal `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Sì | `CreateProcessW` per `<root>\Omsi.exe` con directory di lavoro `<root>`. Errori: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Sì | Il processo esiste, `process.started` è registrato, il journal è `ProcessStarted`, il supervisore è avviato e `StartSessionAsync` restituisce il controllo. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` su `plugin.started` | Sì | Il plugin permanente ha letto un handoff valido per questa sessione. Da qui in poi un timeout è `OL_E_STARTUP_TIMEOUT` anziché `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` su `world.starting` | Sì (solo NEW_MAP) | Il plugin ha invocato l'avvio nativo NEW_MAP sul thread dell'interfaccia utente di OMSI. Le situazioni salvate emettono `world.situation.starting`, che non è mappato, quindi una sessione SAVED_SITUATION passa da `PluginBootstrap` direttamente a `Running`. |
| 13 | `EnteringGameplay` | nessuno | No | Dichiarato, mai impostato: `gameplay.entered` porta la sessione direttamente a `Running`. |
| 14 | `Running` | `ApplyTelemetry` su `gameplay.entered` | Sì | Gameplay raggiunto. `ExecuteRuntimeAsync` è consentito; l'owner della CLI apre il piano di controllo locale; l'area di notifica mostra una sessione in esecuzione. |
| 15 | `ProcessExited` | `SuperviseAsync` | Sì (solo sessioni riuscite) | OMSI è terminato (naturalmente o per terminazione forzata), il journal è `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (e il percorso di errore di avvio) | Sì (solo sessioni riuscite) | Ogni file di proprietà della sessione viene ripristinato dal relativo backup verificato; gli artefatti di sessione vengono rimossi. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Sì (solo sessioni riuscite) | Ripristino verificato, journal e backup rimossi; gli store runtime stanno per essere rilasciati. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Sì, terminale | Store rilasciati, mailbox chiusa, lease rilasciato, nessun errore registrato. |
| 19 | `Failed` | `LiveSession.Fail` da `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Sì, terminale | È stata registrata una voce di diagnostica di errore. Lo stato è persistente: le chiamate `Move` successive vengono ignorate, quindi una sessione fallita non mostra mai `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, anche se la terminazione e il ripristino vengono comunque eseguiti. |

Stati terminali: `Completed` e `Failed`. Dopo uno qualsiasi dei due, `WaitForAsync` restituisce immediatamente e `CloseAsync` restituisce senza richiedere un arresto.

Verifica di «mai impostato»: una ricerca nel codice sorgente di `SessionState.ValidatingPlatform`, `SessionState.Planning` e `SessionState.EnteringGameplay` trova solo la dichiarazione dell'enum; `SessionState.Snapshotting` compare una sola volta, seguito immediatamente da `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Fase di avvio (`StartSessionAsync`)

1. Rifiutare un piano non eseguibile (`OL_E_PLAN_NOT_RUNNABLE`), ripianificare la specifica (ricalcolando l'hash di `Omsi.exe`, risolvendo di nuovo i contenuti, ricontrollando la chiusura del plugin) e rifiutarla di nuovo se non è più eseguibile. Registrare la sessione attiva (`Created`).
2. Creare la traccia dell'host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (i file più vecchi con prefisso di sessione oltre le 50 sessioni più recenti vengono eliminati). Rifiutare `StartupTimeoutSeconds` al di fuori di 1..600 (`ArgumentOutOfRangeException`; la registrazione della sessione viene annullata).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → recuperare un journal in sospeso (un journal precedente all'introduzione dell'impronta, che non può dimostrare la proprietà, viene rinviato e ritentato una volta che gli overlay di questa sessione esistono), validare la chiusura del plugin permanente, calcolare l'hash dell'eseguibile, rimuovere un `closecheck` obsoleto, costruire la transazione (overlay: patch di `options.cfg`, BMP della splash screen gestita, `Texture\standard.itx`; eliminazioni: destinazioni ITX, `Texture\standard.ipr`, `closecheck` quando non esiste).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`, quindi il task del supervisore si avvia e l'handle viene restituito.
5. Qualsiasi eccezione nei passaggi 3–4 viene intercettata: la sessione passa a `Failed` con `OL_E_START_SESSION` (messaggio interno), un processo eventualmente creato viene terminato e atteso, gli store vengono rilasciati, la transazione viene ripristinata (`OL_E_RESTORE_FAILED` in caso di errore) oppure, se non è stato possibile confermare l'uscita di OMSI, lasciata in sospeso con `OL_E_RESTORE_DEFERRED`; il lease viene rilasciato. Anche in questo caso `StartSessionAsync` restituisce l'handle; leggere `GetStatusAsync`.

La ripianificazione mantiene il `SessionId` del chiamante, quindi l'id nell'handle è uguale a `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Supervisione (`SuperviseAsync`)

Il supervisore viene eseguito su un task del thread pool ed esegue un ciclo ogni 100 ms finché OMSI non è terminato o non è stato richiesto un arresto:

1. Leggere, in ordine, ogni evento di telemetria che il plugin ha confermato dall'ultima lettura (un event ring; un lettore rimasto indietro riceve `telemetry.lost` con il conteggio). Ogni evento viene aggiunto a `RuntimeEvents` e mappato da `ApplyTelemetry`.
2. Se la sessione è `Failed`, uscire dal ciclo.
3. Se la sessione non è ancora `Running` e la scadenza (`StartupTimeoutSeconds` dopo l'ingresso nel supervisore) è trascorsa: `Fail` con `OL_E_STARTUP_TIMEOUT` se è stato raggiunto `PluginBootstrap`, altrimenti `OL_E_PLUGIN_NOT_LOADED`; uscire dal ciclo.

Dopo il ciclo: se OMSI è terminato prima di `Running` e non è stato registrato alcun errore, `Fail` con `OL_E_PROCESS_EXITED_EARLY`. Quindi, indipendentemente dal fatto che la sessione sia fallita o no: terminare OMSI se è ancora attivo, attenderne l'uscita, marcare il journal come `ProcessExited`, passare a `ProcessExited`, ripristinare (`Restoring` → `CleaningRuntime`) oppure registrare `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, rilasciare l'handle del processo, l'handoff, lo slot di telemetria e la mailbox runtime, rilasciare il lease e passare a `Completed` a meno che lo stato non sia `Failed`. Un guasto all'interno del supervisore stesso viene registrato come `OL_E_PROCESS_SUPERVISION` (i problemi di pulizia come `OL_E_PROCESS_CLEANUP_FAILED`) e viene eseguito lo stesso percorso di terminazione/ripristino.

Poiché `Failed` è persistente, l'unica evidenza che una sessione fallita è stata ripristinata è l'assenza di `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` nella sua diagnostica (e l'assenza di `journal.json`); le note di ripristino (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) compaiono in entrambi i casi.

<a id="telemetry-events"></a>
## Eventi di telemetria

Il plugin pubblica campioni JSON `{ "name": ..., "data": {...} }` nello slot di telemetria; l'host registra ogni nuovo campione come `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emesso da | Azione dell'host |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` dopo la lettura di un handoff valido | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: nessun `OMSILAUNCH_HANDOFF_NAME`, handoff illeggibile o non verificabile | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: l'handoff richiede una modalità mondo diversa da NEW_MAP/SAVED_SITUATION, un avvio non headless, il veicolo del giocatore, modalità di data/ora o un'identità di situazione vuota | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: la validazione del build nel processo non è riuscita | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | solo registrato |
| `headless.arm.failed` | `PluginRuntime.Start`: non è stato possibile armare l'hook nativo di avvio headless | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | solo registrato |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` quando `InternetTextures.Mode` è `Disabled` | solo registrato |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 o 4) | NEW_MAP: OMSI non è ancora pronto; l'avvio viene ritentato al tick successivo del timer dell'interfaccia utente | solo registrato |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | percorso di successo NEW_MAP | solo registrato |
| `world.failed` (`native_status`) | NEW_MAP: l'avvio nativo ha restituito un errore | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | percorso SAVED_SITUATION | solo registrato (nessun cambio di stato) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: l'avvio nativo ha restituito un errore | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: campi di selezione del punto di ingresso oppure `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | fine dell'avvio del mondo | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` una volta che una qualsiasi operazione `d3d.*` ha attivato la sonda | solo registrato |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` quando la riapplicazione di un `camera.lock` attivo genera un'eccezione (segnalato una volta per ciascun errore distinto) | solo registrato |
| JSON non valido | qualsiasi | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Avvertenze: lo slot contiene un solo campione, quindi gli eventi emessi durante un unico polling dell'host di 100 ms possono andare persi (il plugin sopprime gli eventi del ciclo di vita per 2 s dopo `gameplay.entered` e non pubblica mai un evento D3D nello stesso tick in cui pubblica `gameplay.entered`, quindi il confine `Running` non viene perso). `RuntimeEvents` conserva i 256 eventi più recenti; quelli più vecchi vengono scartati. Non è un log senza perdite. Leggere gli eventi tramite `GetStatusAsync`, `session.events` sul piano di controllo oppure `events read|watch` nella CLI.

<a id="startup-timeout"></a>
## Timeout di avvio

| Elemento | Valore |
| --- | --- |
| Origine | `LaunchSpec.Behavior.StartupTimeoutSeconds` (predefinito 180; 1..600; CLI `/startup-timeout`, profilo `behavior.startup-timeout`). |
| Inizio del conteggio | Quando il task del supervisore entra nel suo ciclo (dopo la restituzione dell'handle). |
| Scadenza prima di `PluginBootstrap` | `Failed` con `OL_E_PLUGIN_NOT_LOADED`. |
| Scadenza dopo `PluginBootstrap`, prima di `Running` | `Failed` con `OL_E_STARTUP_TIMEOUT`. |
| Dopo `Running` | Non si applica alcun timeout; la sessione dura finché OMSI non termina o non viene richiesto un arresto. |
| Owner della CLI | Attende `StartupTimeoutSeconds + 5` secondi il raggiungimento di `Running`; in caso di errore stampa lo stato, con `OmsiLaunchW.exe` mostra una finestra di dialogo con l'ultima voce di diagnostica `OL_E_` (codice di ripiego `OL_E_SESSION_START_FAILED`) ed esce con 1 dopo `CloseAsync`. |

`ShutdownTimeoutSeconds` è presente nella specifica ma non viene utilizzato: non esiste alcuna attesa di chiusura.

<a id="stop-semantics"></a>
## Semantica dell'arresto

Ogni richiesta di arresto è la stessa richiesta canonica: `StopAsync(handle)` dall'API, `CloseAsync` su una sessione non terminale, `session.stop` sul piano di controllo locale (vincolato all'id della sessione attiva), «End session» nell'area di notifica, Ctrl+C o la chiusura della console nell'owner della CLI e la fine di `/observe-seconds`.

| Passaggio | Dettaglio |
| --- | --- |
| 1 | `StopRequested` viene impostato sulla sessione attiva; il chiamante riprende immediatamente il controllo. |
| 2 | Entro 100 ms il supervisore esce dal suo ciclo e chiama `TerminateProcess(Omsi.exe, 1)`. Si tratta di una terminazione forzata: la routine di chiusura di OMSI non viene eseguita, OMSI non riscrive `options.cfg`, non compare alcuna finestra di dialogo di salvataggio. Ciò è intenzionale, affinché OMSI non possa sovrascrivere i file che la transazione sta per ripristinare. |
| 3 | Il supervisore attende l'uscita del processo, registra `ProcessExited`, ripristina esattamente ogni file di proprietà della sessione (incluso il marcatore `closecheck` scritto da OMSI durante la sessione, che diventa una nota `restore.session-artifact-removed`), rimuove il journal e i backup, rilascia gli store runtime (le successive chiamate a `ExecuteRuntimeAsync` generano `OL_E_RUNTIME_CHANNEL_CLOSED` o `OL_E_SESSION_NOT_RUNNING`), rilascia il lease e passa a `Completed`. |
| Uscita naturale | Se OMSI termina da solo dopo `Running` (l'utente chiude OMSI), viene eseguito lo stesso percorso senza terminazione forzata e la sessione si completa normalmente. Prima di `Running` si ha `OL_E_PROCESS_EXITED_EARLY`. |
| Chiusura cooperativa | Non implementata. L'invio di `WM_CLOSE` e l'attesa di `ShutdownTimeoutSeconds` non sono implementati (decisione di prodotto; OMSI ha ignorato `WM_CLOSE` inviato alla sua finestra principale nel round di chiusura runtime, `L05b`) ([stato della validazione a runtime](../status/runtime-validation-status.md)). |
| Stato lato runtime | Tutto ciò che viene modificato tramite operazioni runtime (orologio, telecamera, veicoli generati, variabili di script, texture D3D) è stato interno al processo e scompare con il processo; non viene mai ripristinato né reso persistente. |

<a id="waitforasync-semantics"></a>
## Semantica di `WaitForAsync`

| Situazione | Risultato |
| --- | --- |
| La sessione raggiunge lo stato richiesto | Restituisce lo stato con `State == requested`. |
| La sessione raggiunge prima uno stato terminale | Restituisce immediatamente con `Completed` o `Failed` (controllare `Diagnostics`). |
| Il timeout scade | Restituisce lo stato corrente (nessuna eccezione). Confrontare `State` con quanto richiesto. |
| Stato richiesto già superato (o mai impostato: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, di fatto `Snapshotting`) | Attende fino a uno stato terminale o al timeout. |
| Il chiamante annulla | `OperationCanceledException`. |
| Handle sconosciuto o chiuso | `KeyNotFoundException`. |

L'intervallo di polling è di 100 ms, quindi le transizioni osservate sono in ritardo rispetto a quelle reali fino a 100 ms.

<a id="owner-lifecycle-guarantees-cli"></a>
## Garanzie del ciclo di vita dell'owner (CLI)

`OwnerSession.RunAsync` in `tools/OmsiLaunch.Cli/Program.cs` è l'owner di riferimento.

| Garanzia | Dettaglio |
| --- | --- |
| Owner unico | Prima dell'avvio, la CLI sonda la pipe di controllo; se un owner risponde, rifiuta con `OL_E_SESSION_ALREADY_ACTIVE` (uscita 7). Il lease impone la stessa regola tra processi diversi. |
| Ogni percorso di uscita raggiunge `CloseAsync` | A partire da `StartSessionAsync`, eccezioni, Ctrl+C (`CancelKeyPress`), chiusura della console/disconnessione (`ProcessExit`: viene richiesto l'arresto e l'owner attende fino a 4 s il raggiungimento di `Completed`; ciò che rimane viene recuperato tramite il journal all'avvio successivo), arresto dall'area di notifica, `session.stop` dal piano di controllo, scadenza di `/observe-seconds` e completamento naturale terminano tutti nel blocco `finally`, che rilascia il piano di controllo e l'area di notifica e attende `CloseAsync`. |
| `/observe-seconds` è un limite superiore | Le richieste di arresto dall'area di notifica o dal piano di controllo terminano comunque la sessione prima. |
| Piano di controllo solo durante Running | L'endpoint della named pipe viene creato dopo `Running` (e dopo eventuali batch di validazione `INTERNAL`) e rilasciato prima di `CloseAsync`; negli altri momenti i client ricevono `OL_E_NO_ACTIVE_SESSION`. |
| Codice di uscita | 0 quando lo stato finale è `Completed`, 1 quando è `Failed` o il gameplay non è stato raggiunto, 8 quando un recupero richiesto non è stato completato ([codici di uscita](../reference/exit-codes.md)). |
| Diagnostica | Traccia dell'host e artefatti delle operazioni runtime in `<root>\.omsilaunch\diagnostics`, log dell'area di notifica `tray-host.log`; nessun dato lascia la macchina. |

Gli integratori che scrivono un proprio owner devono riprodurre le prime due garanzie: un solo `StartSessionAsync` per installazione alla volta e `CloseAsync` su ogni percorso.

<a id="failure-map"></a>
## Mappa degli errori

| Fase | Stato in caso di errore | Diagnostica visibile |
| --- | --- | --- |
| Piano | nessuno (nessuna sessione) | `OL_E_PLAN_NOT_RUNNABLE` generato da `StartSessionAsync`; i codici `OL_E_` propri del piano ([validazione di LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Avvio (dal lease alla creazione del processo) | `Failed` | `OL_E_START_SESSION` con il codice interno; eventualmente `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Bootstrap del plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Avvio del mondo | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Running | `Failed` solo in caso di guasti del supervisore | `OL_E_PROCESS_SUPERVISION`; gli errori delle operazioni runtime non fanno mai fallire la sessione. |
| Terminazione e ripristino | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Ogni percorso di errore tenta comunque la terminazione e il ripristino; un journal che rimane viene recuperato all'avvio successivo oppure da `RecoverPendingAsync` / `/recover` ([transazioni e recupero](transactions-and-recovery.md)).
