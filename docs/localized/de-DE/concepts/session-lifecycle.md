# Sitzungslebenszyklus

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Übersetzung der [englischen Originalseite](../../../concepts/session-lifecycle.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite beschreibt, wie eine OmsiLaunch-Sitzung `SessionState` von `Created` bis `Completed` oder `Failed` durchläuft: welche Komponente jeden Zustand setzt, welche Telemetrieereignisse des Plugins die Übergänge auslösen, wie das Start-Timeout funktioniert, was Stoppen bedeutet (erzwungene Beendigung), was `WaitForAsync` zurückgibt, welche Zustände terminal sind, welche Zustände nie oder kaum beobachtbar sind und welche Garantien der CLI-Eigentümer gibt. Alles hier stammt aus `OmsiLaunchService.StartAsync`, `SuperviseAsync` und `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) und `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Verwandte Seiten: [öffentliche API](../reference/public-api.md), [Fehlercodes](../reference/errors.md), [Transaktionen und Recovery](transactions-and-recovery.md), [permanentes Plugin](permanent-plugin.md), [Runtime-Steuerung](../reference/runtime-control.md), [lokale Steuerungsebene](../reference/local-control.md), [Windows-Tray](../reference/windows-tray.md), [CLI-Referenz](../reference/cli.md), [Runtime-Validierungsstatus](../status/runtime-validation-status.md), [das `.omsilaunch`-Verzeichnis](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Überblick

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
## `SessionState`-Referenz

Werte in Deklarationsreihenfolge. „Gesetzt von“ nennt den Code, der `Move`/`Fail` aufruft; „Beobachtbar“ gibt an, ob `GetStatusAsync`/`WaitForAsync` den Zustand in der Praxis sehen können.

| # | Zustand | Gesetzt von | Beobachtbar | Bedeutung |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (Anfangswert der aktiven Sitzung) | Kurzzeitig | Die Sitzung ist registriert; es ist noch nichts geschehen. |
| 1 | `ValidatingPlatform` | niemand | Nein | Deklariert, vom aktuellen Dienst nie gesetzt (die Plattformvalidierung erfolgt in `PlanSessionAsync`, das keinen Sitzungszustand hat). |
| 2 | `Planning` | niemand | Nein | Deklariert, nie gesetzt (die Planung erfolgt, bevor eine Sitzung existiert; auch die erneute Planung in `StartSessionAsync` geht der Registrierung voraus). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Ja | Die Installations-Lease wird angefordert. Fehler: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Ja | Ein ausstehendes `journal.json` wird wiederhergestellt, bevor die aktive Installation gelesen wird; die Plugin-Closure des permanenten Plugins wird validiert (`plugin.integrity.reference`), `Omsi.exe` wird gehasht, Startbild-Assets werden angelegt, ein veraltetes `closecheck` wird entfernt. Fehler: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (im Journal erfasster Prozess lebt noch). |
| 5 | `Snapshotting` | `StartAsync` | Praktisch nein | Wird unmittelbar vor `ApplyingConfiguration` gesetzt, ohne await dazwischen; der Snapshot selbst wird innerhalb von `ApplyAsync` erstellt. Vorübergehend und nicht beobachtbar. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Ja | `journal.json` wird geschrieben (`Prepared`), Originale werden gesichert, Overlays geschrieben, Sitzungslöschungen entfernt (`Applied`). Fehler: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, E/A-Fehler. |
| 7 | `DeployingRuntime` | `StartAsync` | Ja | Journal-Zustand `RuntimeDeployed`. Es wird keine Datei bereitgestellt: Die Plugin-Closure ist permanent. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Ja | Die Übergabe (`OmsiLaunch.Handoff.<id>`), der Telemetrie-Slot (`OmsiLaunch.Telemetry.<id>`) und die Runtime-Mailbox (`OmsiLaunch.Runtime.<id>`) existieren; Journal-Zustand `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Ja | `CreateProcessW` für `<root>\Omsi.exe` mit dem Arbeitsverzeichnis `<root>`. Fehler: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Ja | Der Prozess existiert, `process.started` ist erfasst, das Journal ist `ProcessStarted`, der Supervisor ist gestartet und `StartSessionAsync` kehrt zurück. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` bei `plugin.started` | Ja | Das permanente Plugin hat eine gültige Übergabe für diese Sitzung gelesen. Ab hier führt ein Timeout zu `OL_E_STARTUP_TIMEOUT` statt zu `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` bei `world.starting` | Ja (nur NEW_MAP) | Das Plugin hat den nativen NEW_MAP-Start auf dem UI-Thread von OMSI aufgerufen. Gespeicherte Situationen senden `world.situation.starting`, das nicht zugeordnet ist; eine SAVED_SITUATION-Sitzung geht daher direkt von `PluginBootstrap` zu `Running` über. |
| 13 | `EnteringGameplay` | niemand | Nein | Deklariert, nie gesetzt: `gameplay.entered` versetzt die Sitzung direkt in `Running`. |
| 14 | `Running` | `ApplyTelemetry` bei `gameplay.entered` | Ja | Das Spielgeschehen ist erreicht. `ExecuteRuntimeAsync` ist erlaubt; der CLI-Eigentümer öffnet die lokale Steuerungsebene; der Tray zeigt eine laufende Sitzung an. |
| 15 | `ProcessExited` | `SuperviseAsync` | Ja (nur erfolgreiche Sitzungen) | OMSI wurde beendet (regulär oder durch Terminierung), das Journal ist `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (und der Pfad für Startfehler) | Ja (nur erfolgreiche Sitzungen) | Jede Datei im Besitz der Sitzung wird aus ihrem verifizierten Backup wiederhergestellt; Sitzungsartefakte werden entfernt. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Ja (nur erfolgreiche Sitzungen) | Wiederherstellung verifiziert, Journal und Backups entfernt; die Runtime-Speicher werden gleich freigegeben. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Ja, terminal | Speicher freigegeben, Mailbox geschlossen, Lease freigegeben, kein Fehler erfasst. |
| 19 | `Failed` | `LiveSession.Fail` aus `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Ja, terminal | Eine Fehlerdiagnose wurde erfasst. Der Zustand ist dauerhaft: Spätere `Move`-Aufrufe werden ignoriert, sodass eine fehlgeschlagene Sitzung nie `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed` anzeigt, obwohl Terminierung und Wiederherstellung dennoch ausgeführt werden. |

Terminale Zustände: `Completed` und `Failed`. Nach einem dieser Zustände kehrt `WaitForAsync` sofort zurück, und `CloseAsync` kehrt zurück, ohne einen Stopp anzufordern.

Überprüfung von „nie gesetzt“: Eine Suche in der Codebasis nach `SessionState.ValidatingPlatform`, `SessionState.Planning` und `SessionState.EnteringGameplay` findet nur die Enum-Deklaration; `SessionState.Snapshotting` kommt einmal vor, unmittelbar gefolgt von `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Startphase (`StartSessionAsync`)

1. Einen nicht ausführbaren Plan ablehnen (`OL_E_PLAN_NOT_RUNNABLE`), die Spezifikation erneut planen (`Omsi.exe` erneut hashen, Inhalte erneut auflösen, die Plugin-Closure erneut prüfen) und erneut ablehnen, wenn sie nicht mehr ausführbar ist. Die aktive Sitzung registrieren (`Created`).
2. Den Host-Trace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` anlegen (ältere Dateien mit Sitzungspräfix jenseits der 50 neuesten Sitzungen werden bereinigt). `StartupTimeoutSeconds` außerhalb von 1..600 ablehnen (`ArgumentOutOfRangeException`; die Registrierung der Sitzung wird aufgehoben).
3. `AcquiringInstallationLock` → Lease. `RecoveringPreviousTransaction` → ein ausstehendes Journal wiederherstellen (ein Journal aus der Zeit vor dem Fingerprint, das die Eigentümerschaft nicht nachweisen kann, wird zurückgestellt und erneut versucht, sobald die Overlays dieser Sitzung existieren), die Plugin-Closure des permanenten Plugins validieren, die ausführbare Datei hashen, ein veraltetes `closecheck` entfernen, die Transaktion aufbauen (Overlays: `options.cfg`-Patches, verwaltete Startbild-BMPs, `Texture\standard.itx`; Löschungen: ITX-Ziele, `Texture\standard.ipr`, `closecheck`, wenn es nicht existiert).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`; danach startet der Supervisor-Task, und das Handle wird zurückgegeben.
5. Jede Ausnahme in den Schritten 3–4 wird abgefangen: Die Sitzung ist `Failed` mit `OL_E_START_SESSION` (innere Meldung), ein bereits erstellter Prozess wird terminiert und abgewartet, die Speicher werden freigegeben, die Transaktion wird wiederhergestellt (`OL_E_RESTORE_FAILED` bei Fehlschlag) oder, falls das Beenden von OMSI nicht bestätigt werden konnte, mit `OL_E_RESTORE_DEFERRED` ausstehend belassen; die Lease wird freigegeben. `StartSessionAsync` gibt auch in diesem Fall das Handle zurück; lesen Sie `GetStatusAsync`.

Die erneute Planung behält die `SessionId` des Aufrufers bei, sodass die ID im Handle gleich `plan.SessionId` ist.

<a id="supervision-superviseasync"></a>
## Überwachung (`SuperviseAsync`)

Der Supervisor läuft in einem Thread-Pool-Task und durchläuft alle 100 ms eine Schleife, bis OMSI beendet ist oder ein Stopp angefordert wurde:

1. Jedes Telemetrieereignis lesen, das das Plugin seit dem letzten Lesen festgeschrieben hat, in der richtigen Reihenfolge (ein Ereignisring; ein Leser, der zurückgefallen ist, erhält `telemetry.lost` mit der Anzahl). Jedes Ereignis wird an `RuntimeEvents` angehängt und von `ApplyTelemetry` abgebildet.
2. Ist die Sitzung `Failed`, die Schleife verlassen.
3. Ist die Sitzung noch nicht `Running` und ist die Frist (`StartupTimeoutSeconds` nach Eintritt in den Supervisor) abgelaufen: `Fail` mit `OL_E_STARTUP_TIMEOUT`, wenn `PluginBootstrap` erreicht wurde, andernfalls `OL_E_PLUGIN_NOT_LOADED`; die Schleife verlassen.

Nach der Schleife: Wurde OMSI vor `Running` beendet und kein Fehler erfasst, `Fail` mit `OL_E_PROCESS_EXITED_EARLY`. Danach, unabhängig davon, ob die Sitzung fehlgeschlagen ist: OMSI terminieren, falls es noch läuft, auf das Beenden warten, das Journal als `ProcessExited` markieren, zu `ProcessExited` wechseln, wiederherstellen (`Restoring` → `CleaningRuntime`) oder `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` erfassen, das Prozess-Handle, die Übergabe, den Telemetrie-Slot und die Runtime-Mailbox freigeben, die Lease freigeben und zu `Completed` wechseln, sofern der Zustand nicht `Failed` ist. Ein Fehler im Supervisor selbst wird als `OL_E_PROCESS_SUPERVISION` erfasst (Bereinigungsprobleme als `OL_E_PROCESS_CLEANUP_FAILED`), und derselbe Terminierungs-/Wiederherstellungspfad wird ausgeführt.

Da `Failed` dauerhaft ist, ist der einzige Nachweis dafür, dass eine fehlgeschlagene Sitzung wiederhergestellt wurde, das Fehlen von `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` in ihren Diagnosen (und das Fehlen von `journal.json`); Wiederherstellungshinweise (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) erscheinen in beiden Fällen.

<a id="telemetry-events"></a>
## Telemetrieereignisse

Das Plugin veröffentlicht JSON-Proben `{ "name": ..., "data": {...} }` im Telemetrie-Slot; der Host erfasst jede neue Probe als `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Gesendet von | Host-Aktion |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` nach dem Lesen einer gültigen Übergabe | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: kein `OMSILAUNCH_HANDOFF_NAME`, nicht lesbare oder nicht verifizierbare Übergabe | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: Die Übergabe fordert einen anderen Weltmodus als NEW_MAP/SAVED_SITUATION, einen nicht-headless Start, ein Spielerfahrzeug, Datums-/Zeitmodi oder eine leere Situationsidentität an | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: Die Build-Validierung im Prozess ist fehlgeschlagen | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | nur erfasst |
| `headless.arm.failed` | `PluginRuntime.Start`: Der native Headless-Start-Hook konnte nicht scharfgeschaltet werden | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | nur erfasst |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart`, wenn `InternetTextures.Mode` gleich `Disabled` ist | nur erfasst |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 oder 4) | NEW_MAP: OMSI ist noch nicht bereit; der Start wird beim nächsten Tick des UI-Timers erneut versucht | nur erfasst |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | Erfolgspfad von NEW_MAP | nur erfasst |
| `world.failed` (`native_status`) | NEW_MAP: Der native Start hat einen Fehler zurückgegeben | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | SAVED_SITUATION-Pfad | nur erfasst (keine Zustandsänderung) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: Der native Start hat einen Fehler zurückgegeben | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: Felder der Einstiegspunktauswahl oder `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | Ende des Weltstarts | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle`, sobald irgendeine `d3d.*`-Operation die Sonde aktiviert hat | nur erfasst |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle`, wenn das erneute Anwenden eines aktiven `camera.lock` eine Ausnahme auslöst (einmal pro unterschiedlichem Fehler gemeldet) | nur erfasst |
| Ungültiges JSON | beliebig | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Einschränkungen: Der Slot enthält eine einzige Probe, sodass Ereignisse, die innerhalb einer Host-Abfrage von 100 ms gesendet werden, verloren gehen können (das Plugin unterdrückt Lebenszyklusereignisse für 2 s nach `gameplay.entered` und veröffentlicht nie ein D3D-Ereignis in dem Tick, in dem `gameplay.entered` veröffentlicht wird, sodass die `Running`-Grenze nicht verpasst wird). `RuntimeEvents` behält die 256 neuesten Ereignisse; ältere werden verworfen. Es ist kein verlustfreies Protokoll. Lesen Sie Ereignisse über `GetStatusAsync`, `session.events` auf der Steuerungsebene oder `events read|watch` in der CLI.

<a id="startup-timeout"></a>
## Start-Timeout

| Element | Wert |
| --- | --- |
| Quelle | `LaunchSpec.Behavior.StartupTimeoutSeconds` (Standard 180; 1..600; CLI `/startup-timeout`, Profil `behavior.startup-timeout`). |
| Uhr startet | Wenn der Supervisor-Task seine Schleife betritt (nachdem das Handle zurückgegeben wurde). |
| Ablauf vor `PluginBootstrap` | `Failed` mit `OL_E_PLUGIN_NOT_LOADED`. |
| Ablauf nach `PluginBootstrap`, vor `Running` | `Failed` mit `OL_E_STARTUP_TIMEOUT`. |
| Nach `Running` | Es gilt kein Timeout; die Sitzung dauert, bis OMSI beendet wird oder ein Stopp angefordert wird. |
| CLI-Eigentümer | Wartet `StartupTimeoutSeconds + 5` Sekunden auf `Running`; bei einem Fehler gibt er den Status aus, zeigt unter `OmsiLaunchW.exe` einen Dialog mit der letzten `OL_E_`-Diagnose an (Ersatzcode `OL_E_SESSION_START_FAILED`) und endet nach `CloseAsync` mit Exitcode 1. |

`ShutdownTimeoutSeconds` wird in der Spezifikation mitgeführt, aber nicht ausgewertet: Es gibt kein Warten auf ein Herunterfahren.

<a id="stop-semantics"></a>
## Stopp-Semantik

Jede Stoppanforderung ist dieselbe kanonische Anforderung: `StopAsync(handle)` aus der API, `CloseAsync` auf einer nicht terminalen Sitzung, `session.stop` auf der lokalen Steuerungsebene (an die ID der aktiven Sitzung gebunden), „End session“ („Sitzung beenden“) im Tray, Ctrl+C oder Schließen der Konsole beim CLI-Eigentümer und das Ende von `/observe-seconds`.

| Schritt | Detail |
| --- | --- |
| 1 | `StopRequested` wird für die aktive Sitzung gesetzt; der Aufrufer kehrt sofort zurück. |
| 2 | Innerhalb von 100 ms verlässt der Supervisor seine Schleife und ruft `TerminateProcess(Omsi.exe, 1)` auf. Dies ist eine erzwungene Beendigung: Die Herunterfahrroutine von OMSI läuft nicht, OMSI schreibt `options.cfg` nicht neu, es erscheint kein Speicherdialog. Das ist beabsichtigt, damit OMSI keine Dateien überschreiben kann, die die Transaktion gleich wiederherstellen wird. |
| 3 | Der Supervisor wartet auf das Beenden des Prozesses, erfasst `ProcessExited`, stellt jede Datei im Besitz der Sitzung exakt wieder her (einschließlich der `closecheck`-Markierung, die OMSI während der Sitzung geschrieben hat und die zu einem `restore.session-artifact-removed`-Hinweis wird), entfernt Journal und Backups, gibt die Runtime-Speicher frei (spätere `ExecuteRuntimeAsync`-Aufrufe lösen `OL_E_RUNTIME_CHANNEL_CLOSED` oder `OL_E_SESSION_NOT_RUNNING` aus), gibt die Lease frei und wechselt zu `Completed`. |
| Reguläres Beenden | Wird OMSI nach `Running` von selbst beendet (der Benutzer schließt OMSI), läuft derselbe Pfad ohne Terminierung, und die Sitzung wird normal abgeschlossen. Vor `Running` ist dies `OL_E_PROCESS_EXITED_EARLY`. |
| Kooperatives Herunterfahren | Nicht implementiert. Das Senden von `WM_CLOSE` und das Warten für `ShutdownTimeoutSeconds` ist nicht implementiert (Produktentscheidung; OMSI ignorierte `WM_CLOSE` an sein Hauptfenster in der Runtime-Abschlussrunde, `L05b`) ([Runtime-Validierungsstatus](../status/runtime-validation-status.md)). |
| Runtime-seitiger Zustand | Alles, was über Runtime-Operationen geändert wurde (Uhr, Kamera, erzeugte Fahrzeuge, Skriptvariablen, D3D-Texturen), ist prozessinterner Zustand und verschwindet mit dem Prozess; es wird nie wiederhergestellt oder persistiert. |

<a id="waitforasync-semantics"></a>
## `WaitForAsync`-Semantik

| Situation | Ergebnis |
| --- | --- |
| Die Sitzung erreicht den angeforderten Zustand | Gibt den Status mit `State == requested` zurück. |
| Die Sitzung erreicht zuerst einen terminalen Zustand | Kehrt sofort mit `Completed` oder `Failed` zurück (prüfen Sie `Diagnostics`). |
| Das Timeout läuft ab | Gibt den aktuellen Status zurück (keine Ausnahme). Vergleichen Sie `State` mit dem angeforderten Zustand. |
| Der angeforderte Zustand ist bereits vorbei (oder wird nie gesetzt: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, faktisch `Snapshotting`) | Wartet bis zu einem terminalen Zustand oder dem Timeout. |
| Der Aufrufer bricht ab | `OperationCanceledException`. |
| Unbekanntes oder geschlossenes Handle | `KeyNotFoundException`. |

Das Abfrageintervall beträgt 100 ms, sodass beobachtete Übergänge den tatsächlichen um bis zu 100 ms nachlaufen.

<a id="owner-lifecycle-guarantees-cli"></a>
## Lebenszyklusgarantien des Eigentümers (CLI)

`OwnerSession.RunAsync` in `tools/OmsiLaunch.Cli/Program.cs` ist der Referenz-Eigentümer.

| Garantie | Detail |
| --- | --- |
| Einziger Eigentümer | Vor dem Start prüft die CLI die Steuerungs-Pipe; antwortet ein Eigentümer, verweigert sie den Start mit `OL_E_SESSION_ALREADY_ACTIVE` (Exitcode 7). Die Lease erzwingt dieselbe Regel prozessübergreifend. |
| Jeder Exit-Pfad erreicht `CloseAsync` | Ab `StartSessionAsync` enden Ausnahmen, Ctrl+C (`CancelKeyPress`), Schließen der Konsole/Abmelden (`ProcessExit`: Ein Stopp wird angefordert, und der Eigentümer wartet bis zu 4 s auf `Completed`; alles Verbleibende wird beim nächsten Start über das Journal per Recovery wiederhergestellt), Stopp über den Tray, `session.stop` der Steuerungsebene, Ablauf von `/observe-seconds` und regulärer Abschluss alle im `finally`-Block, der Steuerungsebene und Tray freigibt und `CloseAsync` abwartet. |
| `/observe-seconds` ist eine Obergrenze | Stoppanforderungen über den Tray oder die Steuerungsebene beenden die Sitzung dennoch früher. |
| Steuerungsebene nur während Running | Der Named-Pipe-Endpunkt wird nach `Running` erstellt (und nach eventuellen `INTERNAL`-Validierungs-Batches) und vor `CloseAsync` freigegeben; Clients erhalten zu anderen Zeiten `OL_E_NO_ACTIVE_SESSION`. |
| Exitcode | 0, wenn der Endzustand `Completed` ist, 1, wenn er `Failed` ist oder das Spielgeschehen nicht erreicht wurde, 8, wenn eine angeforderte Recovery nicht abgeschlossen wurde ([Exitcodes](../reference/exit-codes.md)). |
| Diagnosen | Host-Trace und Artefakte von Runtime-Operationen unter `<root>\.omsilaunch\diagnostics`, Tray-Protokoll `tray-host.log`; keine Daten verlassen den Rechner. |

Integratoren, die einen eigenen Eigentümer schreiben, müssen die ersten beiden Garantien nachbilden: jeweils nur ein `StartSessionAsync` pro Installation und `CloseAsync` auf jedem Pfad.

<a id="failure-map"></a>
## Fehlerübersicht

| Phase | Zustand beim Fehlschlag | Diagnosen, die Sie sehen |
| --- | --- | --- |
| Plan | keiner (keine Sitzung) | `OL_E_PLAN_NOT_RUNNABLE`, ausgelöst von `StartSessionAsync`; die eigenen `OL_E_`-Codes des Plans ([LaunchSpec-Validierung](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Start (Lease bis Prozesserstellung) | `Failed` | `OL_E_START_SESSION` mit dem inneren Code; möglicherweise `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Plugin-Bootstrap | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Weltstart | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Running | `Failed` nur bei Supervisor-Fehlern | `OL_E_PROCESS_SUPERVISION`; Fehler von Runtime-Operationen lassen die Sitzung nie fehlschlagen. |
| Terminierung und Wiederherstellung | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Jeder Fehlerpfad versucht dennoch Terminierung und Wiederherstellung; ein verbleibendes Journal wird beim nächsten Start oder durch `RecoverPendingAsync` / `/recover` per Recovery (Absturzwiederherstellung) verarbeitet ([Transaktionen und Recovery](transactions-and-recovery.md)).
