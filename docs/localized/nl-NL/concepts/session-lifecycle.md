# Sessielevenscyclus

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../concepts/session-lifecycle.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina beschrijft hoe een OmsiLaunch-sessie via `SessionState` van `Created` naar `Completed` of `Failed` gaat: welk onderdeel elke toestand instelt, welke telemetrie-events van de plugin de overgangen aansturen, hoe de opstart-timeout werkt, wat stoppen betekent (geforceerde beëindiging), wat `WaitForAsync` retourneert, welke toestanden eindtoestanden zijn, welke toestanden nooit of nauwelijks waarneembaar zijn en welke garanties de CLI-eigenaar geeft. Alles hier is ontleend aan `OmsiLaunchService.StartAsync`, `SuperviseAsync` en `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) en `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Gerelateerde pagina's: [openbare API](../reference/public-api.md), [foutcodes](../reference/errors.md), [transacties en recovery](transactions-and-recovery.md), [permanente plugin](permanent-plugin.md), [runtimebesturing](../reference/runtime-control.md), [lokaal control plane](../reference/local-control.md), [Windows-systeemvak](../reference/windows-tray.md), [CLI-referentie](../reference/cli.md), [status van de runtimevalidatie](../status/runtime-validation-status.md), [de map `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Overzicht

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
## Referentie van `SessionState`

Waarden in declaratievolgorde. "Ingesteld door" noemt de code die `Move`/`Fail` aanroept; "Waarneembaar" geeft aan of `GetStatusAsync`/`WaitForAsync` de toestand in de praktijk kan zien.

| # | Toestand | Ingesteld door | Waarneembaar | Betekenis |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (beginwaarde van de live sessie) | Kort | De sessie is geregistreerd; er is nog niets gebeurd. |
| 1 | `ValidatingPlatform` | niemand | Nee | Gedeclareerd, maar nooit ingesteld door de huidige service (platformvalidatie gebeurt in `PlanSessionAsync`, dat geen sessietoestand heeft). |
| 2 | `Planning` | niemand | Nee | Gedeclareerd, maar nooit ingesteld (planning gebeurt voordat er een sessie bestaat; ook het opnieuw plannen in `StartSessionAsync` vindt plaats vóór de registratie). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Ja | De installatielease wordt verkregen. Fout: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Ja | Een openstaand `journal.json` wordt hersteld voordat de live installatie wordt gelezen; de plugin-closure van de permanente plugin wordt gevalideerd (`plugin.integrity.reference`), van `Omsi.exe` wordt de hash berekend, de assets van het opstartscherm worden geplaatst en een verouderde `closecheck` wordt verwijderd. Fouten: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (het in het journal vastgelegde proces leeft nog). |
| 5 | `Snapshotting` | `StartAsync` | Praktisch niet | Wordt direct vóór `ApplyingConfiguration` ingesteld, zonder await ertussen; de snapshot zelf wordt binnen `ApplyAsync` gemaakt. Kortstondig en niet waarneembaar. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Ja | `journal.json` wordt geschreven (`Prepared`), van de originelen worden back-ups gemaakt, overlays worden geschreven en sessieverwijderingen worden uitgevoerd (`Applied`). Fouten: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, I/O-fouten. |
| 7 | `DeployingRuntime` | `StartAsync` | Ja | Journaltoestand `RuntimeDeployed`. Er wordt geen bestand geïmplementeerd: de plugin-closure is permanent. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Ja | De handoff (`OmsiLaunch.Handoff.<id>`), het telemetrieslot (`OmsiLaunch.Telemetry.<id>`) en de runtime-mailbox (`OmsiLaunch.Runtime.<id>`) bestaan; journaltoestand `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Ja | `CreateProcessW` voor `<root>\Omsi.exe` met werkmap `<root>`. Fouten: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Ja | Het proces bestaat, `process.started` is vastgelegd, het journal staat op `ProcessStarted`, de supervisor is gestart en `StartSessionAsync` keert terug. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` bij `plugin.started` | Ja | De permanente plugin heeft een geldige handoff voor deze sessie gelezen. Vanaf hier is een timeout `OL_E_STARTUP_TIMEOUT` in plaats van `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` bij `world.starting` | Ja (alleen NEW_MAP) | De plugin heeft de native NEW_MAP-start op de UI-thread van OMSI aangeroepen. Opgeslagen situaties sturen `world.situation.starting`, dat niet wordt toegewezen, zodat een SAVED_SITUATION-sessie rechtstreeks van `PluginBootstrap` naar `Running` gaat. |
| 13 | `EnteringGameplay` | niemand | Nee | Gedeclareerd, maar nooit ingesteld: `gameplay.entered` zet de sessie direct op `Running`. |
| 14 | `Running` | `ApplyTelemetry` bij `gameplay.entered` | Ja | Gameplay is bereikt. `ExecuteRuntimeAsync` is toegestaan; de CLI-eigenaar opent het lokale control plane; het systeemvak toont een lopende sessie. |
| 15 | `ProcessExited` | `SuperviseAsync` | Ja (alleen geslaagde sessies) | OMSI is afgesloten (op natuurlijke wijze of door beëindiging), het journal staat op `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (en het pad bij een mislukte start) | Ja (alleen geslaagde sessies) | Elk bestand in bezit van de sessie wordt hersteld vanuit zijn geverifieerde back-up; sessie-artefacten worden verwijderd. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Ja (alleen geslaagde sessies) | Herstel geverifieerd, journal en back-ups verwijderd; de runtime-stores worden zo meteen vrijgegeven. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Ja, eindtoestand | Stores vrijgegeven, mailbox gesloten, lease vrijgegeven, geen fout vastgelegd. |
| 19 | `Failed` | `LiveSession.Fail` vanuit `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Ja, eindtoestand | Er is een foutdiagnose vastgelegd. De toestand is blijvend: latere `Move`-aanroepen worden genegeerd, zodat een mislukte sessie nooit `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed` toont, ook al worden beëindiging en herstel nog steeds uitgevoerd. |

Eindtoestanden: `Completed` en `Failed`. Na een van beide keert `WaitForAsync` onmiddellijk terug en keert `CloseAsync` terug zonder een stop aan te vragen.

Verificatie van "nooit ingesteld": een zoekactie in de codebase naar `SessionState.ValidatingPlatform`, `SessionState.Planning` en `SessionState.EnteringGameplay` vindt alleen de enum-declaratie; `SessionState.Snapshotting` komt één keer voor, direct gevolgd door `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Startfase (`StartSessionAsync`)

1. Een niet-uitvoerbaar plan wordt afgewezen (`OL_E_PLAN_NOT_RUNNABLE`); de specificatie wordt opnieuw gepland (opnieuw de hash van `Omsi.exe` berekenen, content opnieuw resolven, de plugin-closure opnieuw controleren) en opnieuw afgewezen als ze niet langer uitvoerbaar is. De live sessie wordt geregistreerd (`Created`).
2. De host-trace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` wordt aangemaakt (oudere bestanden met sessievoorvoegsel buiten de 50 nieuwste sessies worden opgeschoond). `StartupTimeoutSeconds` buiten 1..600 wordt afgewezen (`ArgumentOutOfRangeException`; de registratie van de sessie wordt ongedaan gemaakt).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → een openstaand journal herstellen (een journal van vóór de vingerafdrukken dat het eigendom niet kan bewijzen, wordt uitgesteld en opnieuw geprobeerd zodra de overlays van deze sessie bestaan), de plugin-closure van de permanente plugin valideren, de hash van het uitvoerbare bestand berekenen, een verouderde `closecheck` verwijderen en de transactie opbouwen (overlays: patches van `options.cfg`, BMP's van het beheerde opstartscherm, `Texture\standard.itx`; verwijderingen: ITX-doelen, `Texture\standard.ipr`, `closecheck` als die niet bestaat).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`, waarna de supervisortaak start en de handle wordt geretourneerd.
5. Elke exceptie in stap 3–4 wordt opgevangen: de sessie wordt `Failed` met `OL_E_START_SESSION` (inner message), een aangemaakt proces wordt beëindigd en er wordt op gewacht, stores worden vrijgegeven, de transactie wordt hersteld (`OL_E_RESTORE_FAILED` bij een fout) of, als het afsluiten van OMSI niet kon worden bevestigd, openstaand gelaten met `OL_E_RESTORE_DEFERRED`; de lease wordt vrijgegeven. `StartSessionAsync` retourneert in dit geval toch de handle; lees `GetStatusAsync`.

Bij het opnieuw plannen blijft de `SessionId` van de aanroeper behouden, zodat de id in de handle gelijk is aan `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Supervisie (`SuperviseAsync`)

De supervisor draait als taak in de threadpool en doorloopt elke 100 ms een lus totdat OMSI is afgesloten of een stop is aangevraagd:

1. Elk telemetrie-event lezen dat de plugin sinds de laatste leesactie heeft vastgelegd, in volgorde (een eventring; een lezer die achterloopt krijgt `telemetry.lost` met het aantal). Elk event wordt aan `RuntimeEvents` toegevoegd en door `ApplyTelemetry` toegewezen.
2. Als de sessie `Failed` is, wordt de lus verlaten.
3. Als de sessie nog niet `Running` is en de deadline (`StartupTimeoutSeconds` na het binnengaan van de supervisor) is verstreken: `Fail` met `OL_E_STARTUP_TIMEOUT` als `PluginBootstrap` was bereikt, anders `OL_E_PLUGIN_NOT_LOADED`; de lus wordt verlaten.

Na de lus: als OMSI vóór `Running` is afgesloten en er geen fout is vastgelegd, volgt `Fail` met `OL_E_PROCESS_EXITED_EARLY`. Daarna, ongeacht of de sessie is mislukt: OMSI beëindigen als het nog leeft, wachten tot het is afgesloten, het journal op `ProcessExited` zetten, naar `ProcessExited` gaan, herstellen (`Restoring` → `CleaningRuntime`) of `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` vastleggen, de proceshandle, handoff, het telemetrieslot en de runtime-mailbox vrijgeven, de lease vrijgeven en naar `Completed` gaan, tenzij de toestand `Failed` is. Een fout in de supervisor zelf wordt vastgelegd als `OL_E_PROCESS_SUPERVISION` (problemen bij het opruimen als `OL_E_PROCESS_CLEANUP_FAILED`) en hetzelfde pad voor beëindiging en herstel wordt doorlopen.

Omdat `Failed` blijvend is, is het enige bewijs dat een mislukte sessie is hersteld het ontbreken van `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` in haar diagnoses (en het ontbreken van `journal.json`); herstelnotities (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) verschijnen in beide gevallen.

<a id="telemetry-events"></a>
## Telemetrie-events

De plugin publiceert JSON-monsters `{ "name": ..., "data": {...} }` in het telemetrieslot; de host legt elk nieuw monster vast als een `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Verzonden door | Actie van de host |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` na het lezen van een geldige handoff | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: geen `OMSILAUNCH_HANDOFF_NAME`, onleesbare of niet-verifieerbare handoff | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: de handoff vraagt om een wereldmodus anders dan NEW_MAP/SAVED_SITUATION, een niet-headless start, een spelersvoertuig, datum-/tijdmodi of een lege situatie-identiteit | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: de buildvalidatie binnen het proces is mislukt | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | alleen vastgelegd |
| `headless.arm.failed` | `PluginRuntime.Start`: de native hook voor de headless start kon niet worden geactiveerd | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | alleen vastgelegd |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` als `InternetTextures.Mode` `Disabled` is | alleen vastgelegd |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 of 4) | NEW_MAP: OMSI is nog niet gereed; de start wordt bij de volgende tick van de UI-timer opnieuw geprobeerd | alleen vastgelegd |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | succespad van NEW_MAP | alleen vastgelegd |
| `world.failed` (`native_status`) | NEW_MAP: de native start heeft een fout geretourneerd | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | SAVED_SITUATION-pad | alleen vastgelegd (geen toestandswijziging) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: de native start heeft een fout geretourneerd | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: velden van de instappuntselectie of `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | einde van de wereldstart | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` zodra een `d3d.*`-operatie de probe heeft geactiveerd | alleen vastgelegd |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` als het opnieuw toepassen van een actieve `camera.lock` een exceptie opwerpt (één keer gemeld per afzonderlijke fout) | alleen vastgelegd |
| Ongeldige JSON | elk onderdeel | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Kanttekeningen: het slot bevat één monster, zodat events die binnen één poll van 100 ms door de host worden verzonden, verloren kunnen gaan (de plugin onderdrukt lifecycle-events gedurende 2 s na `gameplay.entered` en publiceert nooit een D3D-event in de tick die `gameplay.entered` publiceert, zodat de grens `Running` niet wordt gemist). `RuntimeEvents` bewaart de 256 meest recente events; oudere worden verwijderd. Het is geen verliesvrij logboek. Lees events via `GetStatusAsync`, `session.events` op het control plane of `events read|watch` in de CLI.

<a id="startup-timeout"></a>
## Opstart-timeout

| Item | Waarde |
| --- | --- |
| Bron | `LaunchSpec.Behavior.StartupTimeoutSeconds` (standaard 180; 1..600; CLI `/startup-timeout`, profiel `behavior.startup-timeout`). |
| Klok start | Wanneer de supervisortaak haar lus binnengaat (nadat de handle is geretourneerd). |
| Verlopen vóór `PluginBootstrap` | `Failed` met `OL_E_PLUGIN_NOT_LOADED`. |
| Verlopen na `PluginBootstrap`, vóór `Running` | `Failed` met `OL_E_STARTUP_TIMEOUT`. |
| Na `Running` | Er geldt geen timeout; de sessie duurt totdat OMSI wordt afgesloten of een stop wordt aangevraagd. |
| CLI-eigenaar | Wacht `StartupTimeoutSeconds + 5` seconden op `Running`; drukt bij een fout de status af, toont onder `OmsiLaunchW.exe` een dialoogvenster met de laatste `OL_E_`-diagnose (terugvalcode `OL_E_SESSION_START_FAILED`) en sluit na `CloseAsync` af met 1. |

`ShutdownTimeoutSeconds` wordt in de specificatie meegegeven, maar niet gebruikt: er is geen wachttijd bij het afsluiten.

<a id="stop-semantics"></a>
## Stopsemantiek

Elke stopaanvraag is dezelfde canonieke aanvraag: `StopAsync(handle)` vanuit de API, `CloseAsync` op een sessie die zich niet in een eindtoestand bevindt, `session.stop` op het lokale control plane (gebonden aan de actieve sessie-id), "End session" (sessie beëindigen) in het systeemvak, Ctrl+C of het sluiten van de console bij de CLI-eigenaar, en het einde van `/observe-seconds`.

| Stap | Details |
| --- | --- |
| 1 | `StopRequested` wordt op de live sessie ingesteld; de aanroeper keert onmiddellijk terug. |
| 2 | Binnen 100 ms verlaat de supervisor zijn lus en roept hij `TerminateProcess(Omsi.exe, 1)` aan. Dit is een geforceerde beëindiging: de afsluitroutine van OMSI wordt niet uitgevoerd, OMSI herschrijft `options.cfg` niet en er verschijnt geen dialoogvenster om op te slaan. Dit is opzettelijk, zodat OMSI geen bestanden kan overschrijven die de transactie op het punt staat te herstellen. |
| 3 | De supervisor wacht tot het proces is afgesloten, legt `ProcessExited` vast, herstelt elk bestand in bezit van de sessie exact (inclusief de markering `closecheck` die OMSI tijdens de sessie heeft geschreven en die een notitie `restore.session-artifact-removed` wordt), verwijdert het journal en de back-ups, geeft de runtime-stores vrij (latere aanroepen van `ExecuteRuntimeAsync` werpen `OL_E_RUNTIME_CHANNEL_CLOSED` of `OL_E_SESSION_NOT_RUNNING` op), geeft de lease vrij en gaat naar `Completed`. |
| Natuurlijk afsluiten | Als OMSI na `Running` uit zichzelf wordt afgesloten (de gebruiker sluit OMSI), wordt hetzelfde pad zonder beëindiging doorlopen en wordt de sessie normaal voltooid. Vóór `Running` is het `OL_E_PROCESS_EXITED_EARLY`. |
| Coöperatief afsluiten | Niet geïmplementeerd. Het verzenden van `WM_CLOSE` en wachten gedurende `ShutdownTimeoutSeconds` is niet geïmplementeerd (productbeslissing; OMSI negeerde `WM_CLOSE` naar zijn hoofdvenster in de runtime-closure-ronde, `L05b`) ([status van de runtimevalidatie](../status/runtime-validation-status.md)). |
| Toestand aan runtimezijde | Alles wat via runtime-operaties is gewijzigd (klok, camera, gespawnde voertuigen, scriptvariabelen, D3D-textures) is toestand binnen het proces en verdwijnt met het proces; het wordt nooit hersteld of bewaard. |

<a id="waitforasync-semantics"></a>
## Semantiek van `WaitForAsync`

| Situatie | Resultaat |
| --- | --- |
| De sessie bereikt de gevraagde toestand | Retourneert de status met `State == requested`. |
| De sessie bereikt eerst een eindtoestand | Keert onmiddellijk terug met `Completed` of `Failed` (controleer `Diagnostics`). |
| De timeout verloopt | Retourneert de huidige status (geen exceptie). Vergelijk `State` met wat je hebt gevraagd. |
| De gevraagde toestand is al gepasseerd (of wordt nooit ingesteld: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, in feite `Snapshotting`) | Wacht tot een eindtoestand of de timeout. |
| De aanroeper annuleert | `OperationCanceledException`. |
| Onbekende of gesloten handle | `KeyNotFoundException`. |

Het poll-interval is 100 ms, zodat waargenomen overgangen tot 100 ms achterlopen op de werkelijke.

<a id="owner-lifecycle-guarantees-cli"></a>
## Levenscyclusgaranties van de eigenaar (CLI)

`OwnerSession.RunAsync` in `tools/OmsiLaunch.Cli/Program.cs` is de referentie-eigenaar.

| Garantie | Details |
| --- | --- |
| Eén eigenaar | Vóór het starten controleert de CLI de control-pipe; als een eigenaar antwoordt, weigert de CLI met `OL_E_SESSION_ALREADY_ACTIVE` (exitcode 7). De lease dwingt dezelfde regel af over processen heen. |
| Elk afsluitpad bereikt `CloseAsync` | Vanaf `StartSessionAsync` eindigen excepties, Ctrl+C (`CancelKeyPress`), het sluiten van de console of afmelden (`ProcessExit`: er wordt een stop aangevraagd en de eigenaar wacht tot 4 s op `Completed`; wat overblijft, wordt bij de volgende start via het journal hersteld), een stop via het systeemvak, `session.stop` via het control plane, het verlopen van `/observe-seconds` en natuurlijke voltooiing allemaal in het `finally`-blok dat het control plane en het systeemvak vrijgeeft en op `CloseAsync` wacht. |
| `/observe-seconds` is een bovengrens | Stopaanvragen via het systeemvak of het control plane beëindigen de sessie nog steeds eerder. |
| Control plane alleen tijdens Running | Het named-pipe-eindpunt wordt na `Running` aangemaakt (en na eventuele `INTERNAL`-validatiebatches) en vóór `CloseAsync` vrijgegeven; clients krijgen op andere momenten `OL_E_NO_ACTIVE_SESSION`. |
| Exitcode | 0 als de eindtoestand `Completed` is, 1 als deze `Failed` is of gameplay niet is bereikt, 8 als een aangevraagde recovery niet is voltooid ([exitcodes](../reference/exit-codes.md)). |
| Diagnose | Host-trace en artefacten van runtime-operaties onder `<root>\.omsilaunch\diagnostics`, systeemvaklog `tray-host.log`; er verlaten geen gegevens de computer. |

Integrators die hun eigen eigenaar schrijven, moeten de eerste twee garanties reproduceren: één `StartSessionAsync` per installatie tegelijk, en `CloseAsync` op elk pad.

<a id="failure-map"></a>
## Foutenoverzicht

| Fase | Toestand bij de fout | Diagnoses die je ziet |
| --- | --- | --- |
| Plan | geen (geen sessie) | `OL_E_PLAN_NOT_RUNNABLE` opgeworpen door `StartSessionAsync`; de eigen `OL_E_`-codes van het plan ([LaunchSpec-validatie](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Start (van lease tot het aanmaken van het proces) | `Failed` | `OL_E_START_SESSION` met de onderliggende code; mogelijk `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Bootstrap van de plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Wereldstart | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Running | `Failed` alleen bij fouten in de supervisor | `OL_E_PROCESS_SUPERVISION`; fouten in runtime-operaties laten de sessie nooit mislukken. |
| Beëindiging en herstel | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Elk foutpad probeert nog steeds te beëindigen en te herstellen; een journal dat overblijft, wordt bij de volgende start of door `RecoverPendingAsync` / `/recover` hersteld ([transacties en recovery](transactions-and-recovery.md)).
