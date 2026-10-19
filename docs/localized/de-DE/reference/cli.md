# CLI-Referenz

<!-- l10n: source=reference/cli.md source-sha256=cb5d0ea7b7ffe1315a6690239af6e0ae85d483e06995f7092e4d92cb75af3737 -->
> Übersetzung der [englischen Originalseite](../../../reference/cli.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite ist die vollständige, normative Referenz für die Befehlszeile von OmsiLaunch `0.1.0-beta4`: die drei ausführbaren Dateien, die Argumentgrammatik, die Dispatch-Reihenfolge, jedes Befehlswort, jede hierarchische Route, jedes Flag, die Ausgabe-Envelopes und das Fehlerverhalten jedes Befehls. Sie wird aus `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` und den beiden nativen Shims unter `tools\OmsiLaunch.Bootstrapper` erzeugt. Prozessergebnisse sind unter [Exitcodes](exit-codes.md) aufgeführt, Fehlercodes unter [Fehler](errors.md), ausgearbeitete Aufrufe unter [CLI-Beispiele](cli-examples.md).

<a id="executables"></a>
## Ausführbare Dateien

| Datei | Subsystem | Rolle | Unterschiede |
|---|---|---|---|
| `OmsiLaunch.exe` | Konsole | Nativer Bootstrapper (`OmsiLaunch.Bootstrapper.cpp`): ermittelt sein eigenes Verzeichnis, zerlegt die Befehlszeile mit `CommandLineToArgvW` in Token, sucht `hostfxr` über `nethost.dll` und führt `OmsiLaunch.Controller.dll` mit denselben Argumenten aus. | Es wird Konsolenausgabe geschrieben; der Exitcode des Prozesses ist der Exitcode des verwalteten Controllers oder ein Shim-Code `100`..`106`, wenn der .NET-Host nicht gestartet werden konnte. |
| `OmsiLaunchW.exe` | Windows (GUI) | Derselbe Shim (`OmsiLaunch.WindowsHost.cpp`), für das Windows-Subsystem gebaut. Er setzt die Umgebungsvariable `OMSILAUNCH_WINDOWS_HOST=1`, bevor er den Controller startet. | Keine Konsole: Konsolenausgabe wird unterdrückt, sofern nicht `--json` angegeben ist (`WindowsHost.SuppressConsole`), Fehler werden als Meldungsfenster angezeigt (`WindowsHost.ShowFailure`: Meldung, `Code: OL_E_...` und der Hinweis `See .omsilaunch\diagnostics for details.`), und ein Shim-Fehler `100`..`106` wird als `OmsiLaunch could not start the .NET host (code N).` angezeigt. Vollständiges Verhalten: [Referenz zu OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Verwaltet (x64, `net6.0-windows`, Windows Forms) | Der Controller selbst. Er wird von Benutzern nie direkt aufgerufen; beide Shims übergeben den Controller-Pfad als erstes Host-Argument, sodass er nie in der öffentlichen Argumentliste erscheint. | Erfordert die x64-.NET-6-Runtime mit `Microsoft.WindowsDesktop.App`; siehe [Installation](../getting-started/installation.md). |

`nethost.dll` muss neben den Shims liegen. Die Shims lesen selbst keine Argumente; jedes Argument erreicht `CliInput.Parse` unverändert, daher akzeptieren `OmsiLaunch.exe` und `OmsiLaunchW.exe` exakt dieselbe Syntax.

<a id="invocation-model"></a>
## Aufrufmodell

<a id="argument-grammar-cliinputparse"></a>
### Argumentgrammatik (`CliInput.Parse`)

| Form | Bedeutung |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Ein Flag. Beim Schlüssel wird die Groß-/Kleinschreibung nicht beachtet; der Wert ist alles nach dem ersten `:`. Unbekannte Schlüssel schlagen mit `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`) fehl, Exit `2`. |
| `--key=value` | Ein Runtime-Argument für die ausgewählte Runtime-Operation (zum Beispiel `--handle=rv-000001`). Jedes `--`-Token, das `=` enthält, ist ein Runtime-Argument, niemals ein Flag. |
| `--json`, `/json` | Strukturierte Ausgabe (siehe [Ausgabeformate](#output-formats)). `--json` ist das einzige `--`-Token ohne `=`, das eine Bedeutung hat; es wird als das Flag `/json` geparst. |
| einzelnes Wort | Wurde noch kein Befehlswort erkannt und ist das Wort eines der [Befehlswörter](#command-words), wird es zum Befehl. Sobald ein Befehlswort vorhanden ist, ist jedes spätere einzelne Wort ein Befehlswort (die Route). Andernfalls ist das erste einzelne Wort das Installationsstammverzeichnis, und jedes spätere einzelne Wort wird an die Route angehängt. |

Folgen: Eine hierarchische Route (`time get`) lässt sich nicht mit einem dahinter angegebenen Installationsargument kombinieren (`time get D:\OMSI` ist die unbekannte Route `time get d:\omsi`, Exit `2`). `D:\OMSI time get` wird akzeptiert, ist aber **Eigentümermodus** (eine neue Sitzung wird gestartet und die Operation läuft einmal darin). Parse-Fehler (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) und Sitzungsprofil-Fehler (`SessionProfileException`) werden gemeldet, bevor irgendetwas ausgeführt wird, immer mit Exit `2`.

<a id="installation-root"></a>
### Installationsstammverzeichnis

- Ein explizites Installationsargument als einzelnes Wort hat Vorrang vor `RootPath` in einer `/spec`-Datei (`CliInput.BuildSpecAsync`).
- `.` bezeichnet das Verzeichnis, das die ausführbare Datei enthält (`AppContext.BaseDirectory`), niemals das Arbeitsverzeichnis des Aufrufers (`CliInput.ResolveInstallationRoot`). Ein portables Paket verlässt sich darauf.
- Wird das Argument weggelassen, verwenden auch Operationen im Eigentümermodus (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) das Verzeichnis der ausführbaren Datei. Der Pfad wird mit `Path.GetFullPath` normalisiert.
- Befehle im Client-Modus nehmen nie ein Installationsargument entgegen: Sie adressieren den lokalen Steuerungsendpunkt der Installation, in der die ausführbare Datei liegt (`AppContext.BaseDirectory`). Siehe [lokale Steuerung](local-control.md).

<a id="owner-and-client"></a>
### Eigentümer und Client

- **Eigentümer**: der Prozess, der eine Sitzung plant, startet, überwacht und wiederherstellt (`OwnerSession.RunAsync`). Er hält die Installations-Lease (`Local\OmsiLaunch.Installation.<sha256(root)>`) und die Konfigurationstransaktion, stellt den lokalen Steuerungsendpunkt bereit, solange die Sitzung besteht, und zeigt das [Tray-Symbol](windows-tray.md) an. Genau ein Eigentümer pro Installation: Beantwortet bereits ein Eigentümer `session.status` am Steuerungsendpunkt, schlägt ein zweiter Start mit `OL_E_SESSION_ALREADY_ACTIVE` (Exit `7`) fehl.
- **Client**: jeder Aufruf ohne Installationsargument, der `session status`, `session stop`, `events read`, `events watch` oder eine Runtime-Operation sendet. Er wird über die lokale Steuerungs-Pipe weitergeleitet; ohne Eigentümer schlägt er mit `OL_E_NO_ACTIVE_SESSION` (Exit `4`) fehl.

<a id="dispatch-order-cliprogramrunasync"></a>
### Dispatch-Reihenfolge (`CliProgram.RunAsync`)

1. `/silent` (wenn nicht bereits unter `OmsiLaunchW.exe` ausgeführt): startet `OmsiLaunchW.exe` aus dem Verzeichnis der ausführbaren Datei über `ShellExecute` (ohne Handle-Vererbung) mit denselben Argumenten ohne `/silent`/`--silent`, schreibt das Envelope `silent` (`delegated`, `host_process_id`) und gibt `0` zurück. Der Konsolenprozess wartet nicht auf die Sitzung; siehe [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` geben `7` zurück.
2. `/version`: Envelope `version` mit `product`, `version` (informative Assembly-Version, aus `OmsiLaunch.Version.props` gestempelt, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); Exit `0`.
3. `capabilities`: Envelope mit jedem Deskriptor `PublicStableBeta` oder `PublicExperimental` aus dem kanonischen Katalog `PublicCapabilityRegistry` (mit seinen Operationen, nativen Capabilities, Nachweisen und Hinweisen); Exit `0`.
4. `help [family]`: Envelope `help` mit `usage`, `product_version`, `protocol_version`, `family` und den öffentlichen `commands` (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), optional nach Familie gefiltert; Exit `0`.
5. `profiles`: Envelope mit `requirement` (`OMSI 2`), dem Bericht `installation` über das Zielstammverzeichnis (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), den `known_layouts` und den `predicted_capabilities`, die aus der ausführbaren Datei aufgelöst wurden (maßgeblich ist die Auflösung im Prozess beim Start); Exit `0`.
6. Client-Runtime-Operation (kein Installationsargument und eine Route oder `/runtime:`): Die Argumente werden gegen `PublicCapabilityRegistry.ValidateRuntimeArguments` geprüft (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, Exit `2`), dann wird `runtime.execute` mit einem Timeout von 8 s (30 s für `road-vehicles.spawn`) weitergeleitet.
7. Client `session status` (750 ms), `session stop` (an die ID der aktiven Sitzung gebunden, 750 ms), `events read` (750 ms), `events watch` (fragt alle 250 ms ab, bis Ctrl+C).
8. `detect` oder **überhaupt keine Argumente** (keine Installation, kein Befehl, kein `/?`, kein `/spec`, kein Startflag, kein Recovery-Flag, kein `/list`): zählt `Omsi`-Prozesse auf und prüft den Steuerungsendpunkt (250 ms); Envelope `detect`; Exit `0`.
9. `/?` oder `/help`: gibt den Verwendungstext aus, Exit `0`. Jeder andere Aufruf, der ein Befehlswort, aber keine dispatchfähige Route hat (zum Beispiel `d3d` allein oder `session status D:\OMSI`), gibt den Verwendungstext aus und endet mit Exit `2`.
10. Eigentümermodus. Voraussetzungen: `plugins\OmsiLaunch.Plugin.opl` und `plugins\OmsiLaunch.Native.x86.dll` müssen neben der ausführbaren Datei vorhanden sein (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, Exit `7`). `release-manifest.json` neben der ausführbaren Datei liefert, sofern vorhanden, die erwarteten Plugin-Hashes.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; Envelope `recover` mit `pending`, `recovered`, `diagnostics`; Exit `8` nur dann, wenn eine Wiederherstellung angefordert wurde und nicht abgeschlossen wurde, andernfalls `0`.
12. `/list:<category>`: `DiscoverAsync`; Envelope `content.list`; Exit `0`. `/list:SessionProfiles` verwendet stattdessen `ListSessionProfilesAsync`; Envelope `session-profiles.list`.
13. Die `LaunchSpec` erstellen (`BuildSpecAsync`), planen (`PlanSessionAsync`) und den Plan ausgeben. `/plan` oder `/validate`: Exit `0`, wenn `IsRunnable`, sonst `1`. Ein nicht ausführbarer Plan startet OMSI nie (Exit `1`); unter `OmsiLaunchW.exe` zeigt ein Start mit nicht ausführbarem Plan dessen letzte `OL_E_`-Diagnose in einem Meldungsfenster an (Dokumentationsaudit BUG-06). Die Planung prüft außerdem die installierte permanente Plugin-Gesamtheit (Closure) gegen `release-manifest.json`, sodass ein fehlendes oder verändertes Plugin den Plan nicht ausführbar macht (`OL_E_PERMANENT_PLUGIN_*`).
14. Prüfung auf einen bereits vorhandenen Eigentümer (`OL_E_SESSION_ALREADY_ACTIVE`, Exit `7`), danach `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Lebenszyklus des Eigentümers (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. Ab hier erreicht jeder Exit-Pfad `CloseAsync` in einem `finally`-Block: Ausnahmen, Ctrl+C (`Console.CancelKeyPress`), Schließen der Konsole / Abmelden (`AppDomain.ProcessExit` mit einem Budget von 4 s für Stopp + Wiederherstellung; alles Verbleibende wird beim nächsten Start über das Journal per Recovery wiederhergestellt), Tray „End session“, Pipe `session.stop` und `/observe-seconds`.
2. Das Tray-Symbol wird erstellt, sofern `Presentation.SuppressTrayIcon` nicht in der Spec gesetzt ist.
3. Es wird `StartupTimeoutSeconds + 5` Sekunden lang auf `Running` gewartet. Der Status wird ausgegeben. Ist der Zustand nicht `Running`, Exit `1` (`OmsiLaunchW.exe` zeigt `The OMSI session did not reach gameplay.` mit der letzten `OL_E_`-Diagnose oder `OL_E_SESSION_START_FAILED` an).
4. Validierungs-Batches (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) laufen und schreiben ihre Artefakte.
5. Der lokale Steuerungsendpunkt startet.
6. `/runtime:<operation>` läuft einmal (5 s, 15 s für `road-vehicles.spawn`); das Ergebnis wird nach `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` geschrieben und ausgegeben. Ein fehlschlagender Runtime-Befehl beendet die Sitzung nie (stattdessen wird `runtime_error` ausgegeben).
7. Warten: Mit `/observe-seconds:n` wird die Sitzung nach `n` Sekunden beendet **oder** früher bei einem Stopp über Tray/Pipe oder wenn OMSI sich beendet; ohne dieses Flag wartet der Eigentümer, bis OMSI sich beendet oder ein Stopp angefordert wird.
8. Der abgeschlossene Status wird ausgegeben; Exit `0` bei `Completed`, sonst `1`.

`session.stop`, Tray „End session“, Ctrl+C und `CloseAsync` fordern alle den kanonischen Stopp an: OMSI wird mit `TerminateProcess` beendet (die eigene Beendigungsroutine von OMSI läuft nicht und `options.cfg` wird von OMSI nicht neu geschrieben), danach wird jede Datei, die der Sitzung gehört, wiederhergestellt. Siehe [Sitzungslebenszyklus](../concepts/session-lifecycle.md) und [Transaktionen und Recovery](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Befehlswörter

Jedes an erster Position akzeptierte Wort (`CliInput.CommandWordsAccepted`):

| Wort | Zweck | Modus | Hinweise |
|---|---|---|---|
| `capabilities` | Öffentliche Capabilities auflisten | Lokal, keine Sitzung | Envelope `capabilities`. |
| `profiles` | Unterstützte `Omsi.exe`-Varianten auflisten | Lokal, keine Sitzung | Envelope `profiles`. |
| `detect` | `Omsi.exe`-Prozesse und einen aktiven Eigentümer melden | Lokal, keine Sitzung | Auch der Standard, wenn keine Argumente angegeben sind. Zustände: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, pro Prozess `UNKNOWN_BINARY_FOUND`, wenn die Binärdatei nicht untersucht werden kann; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Verwendung und öffentlicher Befehlskatalog | Lokal, keine Sitzung | `help <family>` filtert nach Capability-Familie (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Client | Genau ein folgendes Wort; alles andere gibt den Verwendungstext aus, Exit `2`. `session plan`/`session start` sind Routennamen der API, keine CLI-Wörter: Verwenden Sie `/plan` und `/new`. |
| `events` | `events read`, `events watch` | Client | `read` gibt die begrenzte Ereignisliste einmal zurück; `watch` gibt jedes neue Ereignis (nach `Sequence`) alle 250 ms als `events.watch`-Envelope aus, bis Ctrl+C (Exit `0`), `4`, wenn kein Eigentümer antwortet, `7` bei einem Steuerungsfehler. |
| `time` | `time get`, `time set` | Client-Route | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Client-Route | |
| `map` | `map get` | Client-Route | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Client-Route | |
| `ui` | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action` | Client-Route | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Client-Route | |
| `player` | `player get` | Client-Route | |
| `humans` | `humans list`, `humans get`, `humans summary` | Client-Route | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Client-Route | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Client-Route | |
| `constants` | `constants list`, `constants get` | Client-Route | |
| `curves` | `curves list`, `curves evaluate` | Client-Route | |
| `hof` | `hof get` | Client-Route | |
| `drivers` | `drivers list` | Client-Route | |
| `tickets` | `tickets get` | Client-Route | |
| `d3d` | Reserviertes Familienwort | Keiner | `d3d` hat **keine hierarchische Route**: `d3d texture ...` ist eine unbekannte Route (Exit `2`), und `d3d` allein gibt den Verwendungstext aus (Exit `2`). D3D-Operationen werden mit `/runtime:d3d.status`, `/runtime:d3d.texture.create` usw. erreicht (siehe [Operationen ohne Route](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Hierarchische Routen

`CliInput.HierarchicalRoutes` ordnet einer kleingeschriebenen Route eine Runtime-Operations-ID zu. Alle Routen erfordern eine Sitzung im Zustand `Running` und werden über die Runtime-Mailbox ausgeführt (`ExecuteRuntimeAsync`). Runtime-Schreibvorgänge ändern nur den Speicherzustand von OMSI: Sie berühren niemals Dateien, sind nicht Teil der Konfigurationstransaktion und werden beim Stopp **nicht** rückgängig gemacht (OMSI wird beendet). Die Stabilität richtet sich nach `PublicCapabilityRegistry` und der [Validierungsmatrix](../status/runtime-validation-status.md); Details und Ergebnisfelder finden Sie unter [Runtime-Steuerung](runtime-control.md).

| Route | Runtime-Operation | Art | Erfordert Running | Ändert OMSI | Beteiligung an der Wiederherstellung | Stabilität | Hinweise |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Ja | Nein | Keine | STABLE_BETA | Uhr- und Kalenderfelder. |
| `time set` | `time.set` | Write | Ja | Ja (Uhr im Speicher) | Keine, nicht rückgängig gemacht | EXPERIMENTAL | Zum Beispiel `--minute=<0..59>`; Schreiben, Zurücklesen und Wiederherstellen am 2026-09-20 validiert. |
| `weather get` | `weather.read` | Read | Ja | Nein | Keine | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Ja | Ja (Wetter im Arbeitsspeicher) | Keine, nicht zurückgesetzt | EXPERIMENTAL | Zum Beispiel `--wind_speed=5 --cloud_type="Cumulus 1"`. Manuelles Wetter: Die Live-METAR-Aktualisierung endet. Runtime-Validierung steht aus. |
| `weather metar set` | `weather.metar.set` | Write | Ja | Ja (Wetter im Arbeitsspeicher; OMSI speichert den ICAO-Code in `options.cfg`, was die Sitzung wiederherstellt) | Keine, nicht zurückgesetzt | EXPERIMENTAL | `--icao=<ICAO>`. Live-METAR-Wetter, wie es der Wetterdialog von OMSI setzt. Runtime-Validierung steht aus. |
| `weather actual get` | `weather.actual.read` | Read | Ja | Nein | Keine | EXPERIMENTAL | Zustand des Actual-/ICAO-Controllers. |
| `map get` | `map.read` | Read | Ja | Nein | Keine | STABLE_BETA | Kartenname, Datei, Beschreibung, Kachelanzahl, Jahresbereich und Verkehrsseite; auf dem korrigierten Karten-Slot erneut zur Laufzeit validiert. |
| `camera get` | `camera.read` | Read | Ja | Nein | Keine | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Ja | Ja (Kamera-Skalare, z. B. `--field_of_view=`) | Keine, nicht rückgängig gemacht | EXPERIMENTAL | Schreiben/Zurücklesen des FOV validiert. |
| `camera lock` | `camera.lock` | Action | Ja | Ja (sitzungsbezogene Richtlinie) | Keine | EXPERIMENTAL | Erfordert `--family=<0..3>` (Fahrer=0, Fahrgast=1, extern=2, Karte=3), optional `--preset=<n>` (Familie 0 oder 1). Benötigt ein Spielerfahrzeug (zum Beispiel eine gespeicherte Situation). In der Runtime-Closure zur Laufzeit validiert (`CAM01`); die `RuntimeValidation`-Zeichenfolge der Registry lautet weiterhin `STATICALLY_VALIDATED` (siehe [Capabilities](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Ja | Ja | Keine | EXPERIMENTAL | Hebt die durch `camera lock` gesetzte Richtlinie auf (`CAM01`). |
| `ui pause-state` | `ui.pause.state` | Read | Ja | Nein | Keine | EXPERIMENTAL | Das Pausen-Byte von OMSI und der Zustand des nativen Pausenmenüs. |
| `ui pause-take-over` | `ui.pause.take-over` | Action | Ja | Ja (blendet das native Pausenmenü über die VCL aus; OMSI bleibt pausiert) | Keine, nicht zurückgenommen | EXPERIMENTAL | Erfordert, dass OMSI pausiert ist und das Menü angezeigt wird. Noch nicht in einer echten Sitzung beobachtet. |
| `ui pause-restore` | `ui.pause.restore-native` | Action | Ja | Ja (zeigt das native Pausenmenü wieder an) | Keine | EXPERIMENTAL | Gibt das native Menü zurück. |
| `ui pause-action` | `ui.pause.action` | Action | Ja | Ja (die eigene Aktion des nativen Menüs) | Keine, nicht zurückgenommen | EXPERIMENTAL | Erfordert `--action=<resume|new-game|save|options|quit>`; läuft nach der Antwort, lesen Sie das Ergebnis aus `ui pause-state`. |
| `vehicles list` | `road-vehicles.list` | Read | Ja | Nein | Keine | STABLE_BETA | Gibt sitzungsbezogene `rv-NNNNNN`-Handles zurück. |
| `vehicles get` | `road-vehicle.read` | Read | Ja | Nein | Keine | STABLE_BETA | Erfordert `--handle=`. Veraltetes Handle: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Ja | Nein | Keine | STABLE_BETA | Anzahlen und Spielerzustand, keine Handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Ja | Ja (fügt ein RoadVehicle hinzu) | Keine, nicht entfernt | EXPERIMENTAL | Erfordert `--model=Vehicles\...\*.bus`. Client-Timeout 30 s, Eigentümer-Timeout 15 s. Weist das Spielerfahrzeug nicht zu. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Ja | Ja | Keine | EXPERIMENTAL | Profiliertes `PlaceRandomBus`. |
| `player get` | `player-vehicle.read` | Read | Ja | Nein | Keine | STABLE_BETA | Semantisches Null, wenn kein Spielerfahrzeug vorhanden ist. |
| `humans list` | `humans.list` | Read | Ja | Nein | Keine | EXPERIMENTAL | Gibt `hb-NNNNNN`-Handles zurück. |
| `humans get` | `human.read` | Read | Ja | Nein | Keine | EXPERIMENTAL | Erfordert `--handle=`. |
| `humans summary` | `humans.read` | Read | Ja | Nein | Keine | EXPERIMENTAL | Nur Anzahlen. |
| `timetable get` | `timetable.read` | Read | Ja | Nein | Keine | STABLE_BETA | Zustand des Fahrplanmanagers. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Ja | Nein | Keine | STABLE_BETA | Teil der Capability `timetable.read`; Nachweis per Batch-Lesen vom 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable lines list` | `timetable.lines.list` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable tours list` | `timetable.tours.list` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable logs list` | `timetable.logs.read` | Read | Ja | Nein | Keine | STABLE_BETA | Wie oben. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Ja | Nein | Keine | EXPERIMENTAL | Fahrzeugnummern, die OMSI aus der freien KI-Nutzung heraushält (durch Fahrplanumläufe oder feste KI-Listen reserviert). Seitenweise (`cursor`, `limit`, höchstens 256 pro Seite). |
| `map tiles list` | `map.tiles.list` | Read | Ja | Nein | Keine | EXPERIMENTAL | Die Kacheln der Karte: Rasterposition, eigener Index, Datei und Ladezustand, Anzahl der Objekte, Pfade und Splines. Seitenweise (`cursor`, `limit`, höchstens 256 pro Seite). |
| `drivers list` | `drivers.read` | Read | Ja | Nein | Keine | EXPERIMENTAL | Fahrerdatensätze. |
| `tickets get` | `tickets.read` | Read | Ja | Nein | Keine | EXPERIMENTAL | Datensätze der Fahrscheinpakete. |
| `hof get` | `vehicle.hofs.read` | Read | Ja | Nein | Keine | STABLE_BETA | Erfordert `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Ja | Nein | Keine | STABLE_BETA | Erfordert `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Ja | Nein | Keine | STABLE_BETA | Erfordert `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Ja | Nein | Keine | STABLE_BETA | Erfordert `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Ja | Nein | Keine | STABLE_BETA | Erfordert `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Ja | Nein | Keine | EXPERIMENTAL | Erfordert `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Ja | Nein | Keine | EXPERIMENTAL | Erfordert `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Ja | Ja (Skriptvariable) | Keine, nicht rückgängig gemacht | EXPERIMENTAL | Erfordert `--handle=`, `--name=`, `--value=` (endliche Zahl). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Ja | Nein | Keine | EXPERIMENTAL | Erfordert `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Ja | Nein | Keine | EXPERIMENTAL | Erfordert `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Ja | Ja (Skriptzustand im Arbeitsspeicher) | Keine, nicht zurückgesetzt | EXPERIMENTAL | Erfordert `--handle=`, `--name=`, `--value=`. Das eigene `SetStringVariable` von OMSI. Runtime-Validierung steht aus. |
| `scripts trigger` | `vehicle.trigger` | Action | Ja | Ja (führt Fahrzeugskriptcode aus) | Keine, nicht zurückgesetzt | EXPERIMENTAL | Erfordert `--handle=`, `--name=`, `--value=` (`1` drücken, `0` loslassen, `2` ziehen). Runtime-Validierung steht aus. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Ja | Ja (spielt Fahrzeugsounds ab) | Keine, nicht zurückgesetzt | EXPERIMENTAL | Erfordert `--handle=`, `--trigger=`; optional `--file=`. Runtime-Validierung steht aus. |

<a id="operations-without-a-route"></a>
### Operationen ohne Route

Diese öffentlichen Operations-IDs (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) haben keine hierarchische Route und werden mit `/runtime:<operation>` plus `--key=value` oder `/runtime-arg:key=value` aufgerufen: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` erforderlich; `levels` optional), `d3d.texture.describe` (`handle`; `level` optional), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` erforderlich; `level`, `x`, `y` optional), `d3d.texture.release` (`handle`). D3D-Operationen sind EXPERIMENTAL; der Textur-Lebenszyklus und die Invalidierung beim Geräte-Reset sind zur Laufzeit validiert (Runtime-Closure `H02`, `D01`; siehe [Capabilities](capabilities.md)). `timetable.track-entries.list` und `timetable.tour-entries.list` sind begrenzte Listen: Ein Ergebnis, das nicht in den Runtime-Slot passt, wird gekürzt (`truncated=true`). `internal.road-vehicles.make-basic` ist INTERNAL und wird sowohl von der CLI als auch von der API mit `OL_E_RUNTIME_OPERATION_UNKNOWN` abgelehnt.

## Flags

Jedes Flag aus `CliInput.KnownFlags`. „Phase“ ist *Startzeit* (bestimmt die `LaunchSpec`/den Plan einer neuen Sitzung), *Runtime* (wirkt auf eine laufende Sitzung) oder *Steuerung* (ändert das Verhalten der CLI selbst). Flags, die nur aus Kompatibilitätsgründen geparst werden (`CliInput.AcceptedNoEffectFlags`), sind in ihrer Zeile gekennzeichnet.

<a id="control-and-output"></a>
### Steuerung und Ausgabe

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/?` | `/?` | aus | Steuerung | STABLE_BETA | Gibt den Verwendungstext aus, Exit `0`. |
| `/help` | `/help` | aus | Steuerung | STABLE_BETA | Wie `/?`. (Das einzelne Wort `help` gibt stattdessen den strukturierten Katalog zurück.) |
| `/version` | `/version` | aus | Steuerung | STABLE_BETA | Envelope `version`, Exit `0`. Wird vor jedem anderen Befehl außer `/silent` ausgewertet. |
| `/json` | `/json` oder `--json` | aus | Steuerung | STABLE_BETA | Gibt JSON-Envelopes aus; erzwingt außerdem Konsolenausgabe auch unter `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | aus | Steuerung | STABLE_BETA | Unterdrückt die menschenlesbare Erfolgsausgabe. JSON-Ausgabe (`/json`) und Fehler werden weiterhin geschrieben; der Exitcode bleibt unverändert. |
| `/silent` | `/silent` (auch `--silent`) | aus | Steuerung | EXPERIMENTAL | Delegiert die gesamte Befehlszeile an `OmsiLaunchW.exe` und gibt `0` zurück, sobald der Host-Prozess gestartet ist. Das Ergebnis der Sitzung wird von `OmsiLaunchW.exe` (Meldungsfenster, Tray-Symbol), `.omsilaunch\diagnostics` und dem lokalen Steuerungsendpunkt gemeldet. Die Delegierung und die Fehlerdialoge sind zur Laufzeit validiert (Runtime-Closure `T04`); siehe [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | Steuerung | REMOVED | In 0.1.0-beta4 entfernt: wird mit `OL_E_INVALID_ARGUMENT` abgelehnt (Exitcode `2`). Der Sitzungseigentümer stellt den lokalen Steuerungsendpunkt immer bereit. |
| `/verbose` | `/verbose` | aus | Startzeit | PARTIAL | `DiagnosticsSpec.Verbose`. Die Werte werden in der Spec mitgeführt; ihre Wirkung beschränkt sich auf den Host-Trace unter `.omsilaunch\diagnostics`. |
| `/log` | `/log` | an (`DiagnosticsSpec.Log` ist standardmäßig `true`) | Startzeit | PARTIAL | `DiagnosticsSpec.Log`. Faktisch immer aktiv. |
| `/logall` | `/logall` | aus | Startzeit | PARTIAL | Setzt `Verbose`, `ProcessTrace`, `PluginTrace` und `NativeTrace` gemeinsam. |
| `/omsi-logall` | `/omsi-logall` | aus | Startzeit | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | aus | Startzeit | PARTIAL | Alias von `/trace-process`. |
| `/trace-process` | `/trace-process` | aus | Startzeit | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | aus | Startzeit | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: Das Plugin meldet jede Runtime-Operation (`plugin.trace.operation`: Operation, Ergebnis, Code, Dauer) an den Host-Trace der Sitzung. |
| `/trace-native` | `/trace-native` | aus | Startzeit | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: Die native Schicht meldet jede Installation und Entfernung eines Hooks (`native.trace`) an den Host-Trace der Sitzung. |

<a id="planning-validation-and-harnesses"></a>
### Planung, Validierung und Harnesses

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/plan` | `/plan` | aus | Startzeit | STABLE_BETA | Erstellt den `SessionPlan` und gibt ihn aus, startet OMSI nicht. Exit `0` bei `IsRunnable`, sonst `1`. Erfordert eine Startauswahl (`/new`, `/saved`, `/spec` oder ein Installationsargument); `/plan` allein ohne weitere Angaben führt `detect` aus. |
| `/validate` | `/validate` | aus | Startzeit | STABLE_BETA | In diesem Build identisch mit `/plan`. |
| `/runtime-batch` | `/runtime-batch` | aus | Runtime (Eigentümer) | INTERNAL | Validierungs-Harness: Führt nach `Running` den Satz von Leseoperationen aus und schreibt `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | aus | Runtime (Eigentümer) | INTERNAL | Validierungs-Harness: Lesevorgänge plus `time.set`, `camera.set` und `vehicle.variable.set` mit Wiederherstellung; schreibt `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | aus | Runtime (Eigentümer) | INTERNAL | Validierungs-Harness für den D3D-Textur-Lebenszyklus; schreibt `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | keiner | Runtime | STABLE_BETA (Dispatch) | Wählt eine öffentliche Runtime-Operation per ID aus. Client-Modus (kein Installationsargument): wird an den Eigentümer weitergeleitet. Eigentümermodus: wird nach `Running` einmal ausgeführt. Unbekannte IDs: `OL_E_RUNTIME_OPERATION_UNKNOWN`, Exit `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (wiederholbar) | keiner | Runtime | STABLE_BETA (Dispatch) | Runtime-Argument; gleichwertig mit `--key=value`. Fehlendes `=`: `/runtime-arg requires key=value`, Exit `2`. |

<a id="world-selection"></a>
### Weltauswahl

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` ist der Standardmodus, ein Start wird jedoch nur angefordert, wenn eines von `/new`, `/saved`, `/last`, `/spec` vorhanden ist | Startzeit | STABLE_BETA | NEW_MAP. Erfordert `/map` und `/entrypoint-index` (ein Plan ohne angegebenen Index des Einstiegspunkts meldet `OL_E_ENTRYPOINT_REQUIRED`; ohne `/map` wird keine Karte aufgelöst). `/new` wählt nie stillschweigend eine Karte aus. |
| `/saved` | `/saved:<file.osn>` | keiner | Startzeit | STABLE_BETA | SAVED_SITUATION. Karte und Position stammen aus der `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` werden zusammen mit `/saved` abgelehnt (Exit `2`). Fehlende Situation: `OL_E_SITUATION_NOT_FOUND`; fehlende Karte der Situation: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | keiner | Startzeit | EXPERIMENTAL | LAST_MAP_STATE: lädt die eigene `laststn.osn` der Karte (von OMSI beim Schließen der Karte geschrieben) über den Last-State-Zweig des Start-Formulars. Erfordert `/map`; eine Karte ohne `laststn.osn` ergibt `OL_E_SITUATION_NOT_FOUND`. Niemals eine Heuristik nach der neuesten `.osn`. Runtime-Validierung steht aus. |
| `/map` | `/map:<identity>` (zum Beispiel `maps\Grundorf\global.cfg`) | keiner | Startzeit | STABLE_BETA | Kartenidentität für `/new` oder der Geltungsbereich für `/list:Entrypoints`. Unbekannt: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | keiner | Startzeit | EXPERIMENTAL | Einstiegspunkt nach seiner exakten Bezeichnung (Groß-/Kleinschreibung wird beachtet) oder nach einer Identität aus `/list:entrypoints`. OMSI zeigt nur den ersten Datensatz jeder Bezeichnung an; ein späterer Datensatz mit derselben Bezeichnung wird abgelehnt (`OL_E_ENTRYPOINT_NOT_PRESENTED`), ein unbekannter ebenfalls (`OL_E_ENTRYPOINT_NOT_FOUND`). Runtime-Validierung steht aus. Schließt sich gegenseitig mit `/entrypoint-index` aus (die Identität hat Vorrang und löscht den Index). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | keiner | Startzeit | STABLE_BETA | Index des Einstiegspunkts in der angezeigten Liste (1-basiert, wie OMSI ihn anzeigt). Erforderlich für einen ausführbaren NEW_MAP-Plan. |

<a id="date-time-and-weather"></a>
### Datum, Uhrzeit und Wetter

Diese Flags werden akzeptiert und in die `LaunchSpec` übernommen. Startdatum, Uhrzeit und Jahr gelten nur für NEW_MAP-Sitzungen: Mit `/saved` oder `/last` erfasst der Planer sie als `UNAVAILABLE` **und fügt `OL_E_CAPABILITY_UNAVAILABLE` hinzu, sodass der Plan NICHT AUSFÜHRBAR ist (Exit `1`)**, da eine gespeicherte Situation ihre eigene Uhr behält. Das Wetter gilt für jeden Weltmodus. Alle sind `EXPERIMENTAL`: Der native Startpfad ist statisch abgeschlossen, die Runtime-Validierung steht aus. Eine `/spec`-Datei oder ein Sitzungsprofil, das sie setzt, hat dieselbe Wirkung.

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` oder `/date:system` | nicht gesetzt | Startzeit | EXPERIMENTAL | `DateSpec` explizit/System: das Startdatum einer NEW_MAP-Sitzung (siehe [LaunchSpec](launchspec.md#datespec-timespec-yearspec)). Nicht parsbarer Wert: `OL_E_INVALID_ARGUMENT`, Exit `2`; ein unmögliches Datum: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` oder `/time:system` | nicht gesetzt | Startzeit | EXPERIMENTAL | `TimeSpec` explizit/System: die Startzeit einer NEW_MAP-Sitzung. |
| `/year` | `/year:<n>` oder `/year:system` | nicht gesetzt | Startzeit | EXPERIMENTAL | `YearSpec`: das Startjahr einer NEW_MAP-Sitzung (1900..2200). |
| `/weather` | `/weather:<preset>` | nicht gesetzt | Startzeit | EXPERIMENTAL | `WeatherMode.Preset`: eine Datei aus dem Ordner `weather` der Installation, nach Name (`Regen`) oder Pfad (`weather\Regen.owt`); eine fehlende ergibt zur Planungszeit `OL_E_WEATHER_INVALID`. Wird über das eigene `loadWeather` von OMSI direkt nach dem Start der Welt geladen; Live-METAR bleibt aus. Runtime-Validierung steht aus. |
| `/weather-icao` | `/weather-icao:<code>` | nicht gesetzt | Startzeit | EXPERIMENTAL | `WeatherMode.Icao`: Live-METAR-Wetter für eine ICAO-Station mit vier Zeichen (sonst `OL_E_WEATHER_INVALID`). OMSI speichert die Station in `options.cfg`, was die Sitzung wiederherstellt. Runtime-Validierung steht aus. |
| `/weather-real` | `/weather-real` | nicht gesetzt | Startzeit | EXPERIMENTAL | `WeatherMode.RealCurrent`: Live-METAR-Wetter für die Station, die OMSI bereits konfiguriert hat; ohne eine solche schlägt der Start mit `OL_E_WEATHER_APPLY_FAILED` fehl. Das letzte von `/weather`, `/weather-icao`, `/weather-real` hat Vorrang. Jedes Startwetter überschreibt das Wetter einer gespeicherten Situation. |

<a id="player-vehicle"></a>
### Spielerfahrzeug

Wird akzeptiert und gegen die Installation aufgelöst, aber von der Runtime nicht angewendet: Jedes gesetzte Feld ist `UNAVAILABLE` und fügt `OL_E_CAPABILITY_UNAVAILABLE` hinzu (Plan NICHT AUSFÜHRBAR, Exit `1`).

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | nicht gesetzt | Startzeit | UNAVAILABLE | Wird zuerst aufgelöst (`OL_E_VEHICLE_NOT_FOUND`, falls unbekannt). |
| `/repaint` | `/repaint:<id>` | nicht gesetzt | Startzeit | UNAVAILABLE | Wird nur zusammen mit `/vehicle` aufgelöst (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | nicht gesetzt | Startzeit | UNAVAILABLE | `OL_E_HOF_NOT_FOUND`, falls unbekannt. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | keiner | Startzeit | STABLE_BETA | Eine [HOF-Verwaltungs](hof-management.md)-Operation der Sitzung: `HofSourceKind` und `Source`. Erfordert mindestens ein `/hof-target`. Ersetzt das `HofManagement` einer `/spec`; mit einer Voreinstellung eines Sitzungsprofils, die `hofs` enthält: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Fehlerhaft: Exit `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (wiederholbar) | keiner | Startzeit | STABLE_BETA | Ein Zielfahrzeug unterhalb von `Vehicles\`. Ohne `/hof-source`: Exit `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (wiederholbar) | alle Dateien | Startzeit | STABLE_BETA | Beschränkt die Quelle auf diese Dateinamen. Ohne `/hof-source`: Exit `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | Startzeit | STABLE_BETA | `HofApplyMode`. Ohne `/hof-source`: Exit `2`. |
| `/module` | `/module:<id>` (wiederholbar) | keiner | Startzeit | EXPERIMENTAL | Fordert ein [Runtime-Modul](runtime-modules.md) für die Sitzung an (`LaunchSpec.Modules.Enable`, wird zu den Modulen einer `/spec`-Datei hinzugefügt). Ein angefordertes Modul, das nicht geladen werden kann, macht den Plan mit seinem `OL_E_MODULE_*`-Code nicht ausführbar (Exit `1`). Seine Operationen werden mit `/runtime:module.<id>.<operation>` und `/runtime-arg` erreicht. Mit einer Voreinstellung eines Sitzungsprofils, die dafür `enabled: false` setzt: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (wiederholbar) | keiner | Startzeit | EXPERIMENTAL | Hält ein Modul aus der Sitzung heraus, auch ein standardmäßig, durch seine Karte oder durch die Einstellungsdatei geladenes Modul (`LaunchSpec.Modules.Disable`). Mit einer Voreinstellung, die dafür `enabled: true` setzt: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Ohne Wert: Exit `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (wiederholbar) | keiner | Startzeit | EXPERIMENTAL | Setzt eine Moduleinstellung (`LaunchSpec.Modules.Settings`); hat Vorrang vor der `/spec`-Datei, der Einstellungsdatei und dem Standardwert. Ein Schlüssel, den auch die ausgewählte Voreinstellung setzt: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Nicht `<id>.<key>=<value>`: Exit `2`. Ungültiger Wert oder nicht deklarierter Schlüssel: Plandiagnose `OL_E_MODULE_SETTING_INVALID` (Exit `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` allein bedeutet `true` für eine boolesche Einstellung | keiner | Startzeit | EXPERIMENTAL | Ein Flag, das ein installiertes Modul für eine seiner Einstellungen deklariert (`flag` in seinem Manifest; aufgelistet von `/list:RuntimeModules`). Es wird beim Planen einer Sitzung aufgelöst und entspricht `/module-setting:<id>.<key>=<value>`. Ein Flag, das kein installiertes Modul deklariert, oder eines außerhalb eines Starts: `Unknown argument`, Exit `2`. Ein von zwei Modulen deklariertes Flag: `OL_E_MODULE_SETTING_INVALID`, Exit `2`. Ein Modul-Flag ersetzt niemals ein eingebautes Flag. |
| `/fleet` | `/fleet:<n>` | nicht gesetzt | Startzeit | UNAVAILABLE | Fuhrparknummer. |
| `/registration` | `/registration:<text>` | nicht gesetzt | Startzeit | UNAVAILABLE | Kennzeichen. |
| `/no-vehicle` | `/no-vehicle` | aus | Startzeit | STABLE_BETA | Entfernt ein etwaiges Spielerfahrzeug aus der Vorlage (`/spec` oder Profil). Unbedenklich. |

<a id="configuration-overlays"></a>
### Konfigurations-Overlays

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (wiederholbar; Groß-/Kleinschreibung der Schlüssel wird nicht beachtet) | keiner | Startzeit | STABLE_BETA | Semantisches `options.cfg`-Overlay aus `ConfigurationCatalog` (zum Beispiel `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Unbekannter Schlüssel: `OL_E_UNKNOWN_SETTING` (Exit `2`); schreibgeschützter Schlüssel (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (Exit `2`); Wert außerhalb des Bereichs oder fehlerhaft: `OL_E_INVALID_SETTING_VALUE`, wenn das Overlay erstellt wird. Das Overlay ist eine Sitzungsänderung: als Snapshot gesichert, vor dem Start von OMSI angewendet, beim Stopp Byte für Byte wiederhergestellt (RV-005 `RUNTIME_VALIDATED`). Konflikte mit einem Schlüssel, der einer Voreinstellung eines ausgewählten Profils gehört: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Startbild-Darstellung

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (Groß-/Kleinschreibung wird nicht beachtet) | `Managed` | Startzeit | STABLE_BETA | `Managed`: Die mitgelieferten 24-Bit-BMPs mit 640x480 werden einmalig nach `<root>\.omsilaunch\assets\splash` kopiert, und `GUI\NewSplashscreen_ENG.bmp` sowie `GUI\NewSplashscreen_<lang>.bmp` werden transaktional überlagert und exakt wiederhergestellt (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (Aliase): OMSI-Dateien bleiben unberührt. Fehlender Wert: `/splash requires Unset, Native, or Managed`, Exit `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (auch `pt-BR`, `de`, `fr`, `en`; alles andere fällt auf `ENG` zurück) | `[language]` aus `options.cfg`, sonst `ENG` | Startzeit | STABLE_BETA | Wählt die lokalisierte Zieldatei aus. |
| `/splash-assets` | `/splash-assets:<directory>` (relative Pfade werden unterhalb des Installationsstammverzeichnisses aufgelöst) | `<root>\.omsilaunch\assets\splash`, sonst der mitgelieferte Satz | Startzeit | STABLE_BETA | Benutzerdefiniertes Asset-Verzeichnis; muss `ENG.bmp` und, für eine nicht englische Sprache, `<lang>.bmp` enthalten. Fehler: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (im Plan als `OL_E_SESSION_PRESENTATION_INVALID` gemeldet; nicht ausführbar). |

<a id="internet-textures"></a>
### Internettexturen

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | Startzeit | EXPERIMENTAL | `Native`: unberührt. `Disabled`: Der profilierte prozessinterne Downloader wird unterdrückt. `Override`: Das angegebene `.itx`-Profil wird als `Texture\standard.itx` überlagert; jedes darin aufgeführte HTTP(S)-Ziel sowie `Texture\standard.ipr` werden zu Sitzungslöschungen (für die Sitzung entfernt, beim Stopp wiederhergestellt). Fehlender Wert: Exit `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | keiner | Startzeit | EXPERIMENTAL | Erforderlich mit `Override` (`OL_E_ITX_PROFILE_REQUIRED`, Exit `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (muss aus Zeilenpaaren aus URL und Ziel mit `http`/`https`-URLs bestehen), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (Ziele müssen unter `Texture\` aufgelöst werden, ohne absolute Pfade, `..` oder Analysepunkte). |

<a id="session-profiles"></a>
### Sitzungsprofile

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | keiner | Startzeit | STABLE_BETA (Kompilierung; offline `OmsiLaunch.ProfileTests`) | Lädt `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (siehe [Sitzungsprofile](session-profiles.md)). Erfordert `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, Exit `2`). Der Block `new:` gilt nur mit `/new`; `compatibility.maps` wird für `/new` und `/saved` erzwungen (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Explizite Flags, die mit einem dem Profil gehörenden Feld kollidieren, werden mit `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` abgelehnt (`CliInput.RejectProfileConflicts`): Karte/Einstiegspunkt/Datum/Uhrzeit/Jahr/Wetter, wenn der Block `new:` sie besitzt, `/set`-Schlüssel, die der Voreinstellung gehören, Startbild-Flags, wenn die Voreinstellung `presentation` enthält, Internettextur-Flags, wenn sie `internet-textures` enthält, Timeouts, wenn sie `behavior` enthält. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | keiner | Startzeit | STABLE_BETA | Wählt die Voreinstellung über `index` aus. Außerhalb des Bereichs: Exit `2`. |

<a id="launchspec-file"></a>
### LaunchSpec-Datei

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | keiner | Startzeit | STABLE_BETA (Loader offline getestet; Sitzungssemantik identisch mit Flags) | Lädt eine `LaunchSpec`-JSON-Datei als Vorlage (siehe [LaunchSpec](launchspec.md)) und kennzeichnet einen Start als angefordert. Regeln (`LaunchSpecJson`): Die Datei muss existieren (`OL_E_SPEC_NOT_FOUND`, Exit `6`); höchstens 1 MiB (`OL_E_SPEC_TOO_LARGE`, Exit `2`); die Wurzel muss ein Objekt sein (`OL_E_SPEC_INVALID`); bei Eigenschaftsnamen wird die Groß-/Kleinschreibung nicht beachtet; `//`-Kommentare und nachgestellte Kommas sind erlaubt; Tiefe höchstens 32; jede unbekannte Eigenschaft wird mit ihrem JSON-Pfad abgelehnt (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, Exit `2`). |

**Vorrang** (`CliInput.BuildSpecAsync`): Standardwerte → `/spec`-Datei → `/predefined-profile` (ersetzt `Installation` und `World`, wendet dann das Profil an) → explizite Flags. Ein explizites Installationsargument hat Vorrang vor `RootPath` in der Spec. `/no-vehicle` entfernt das Spielerfahrzeug der Spec; `/vehicle` und die zugehörigen Flags werden Feld für Feld damit zusammengeführt. `/set`-Schlüssel werden in `Environment.General` zusammengeführt. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` überschreiben nur, wenn sie angegeben sind. `/startup-timeout` und `/shutdown-timeout` überschreiben nur, wenn sie angegeben sind; `Presentation.SuppressTrayIcon` stammt ausschließlich aus der Spec (kein Flag). Diagnose-Flags werden mit den `Diagnostics` der Spec per ODER verknüpft.

<a id="content-discovery"></a>
### Inhaltserkennung

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; Kategorien sind die `ContentQueryKind`-Werte `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (Groß-/Kleinschreibung wird nicht beachtet) | keiner | lokal, keine Sitzung | STABLE_BETA | `DiscoverAsync` über die Installation; Envelope `content.list` mit Einträgen `Identity`, `Kind`, `DisplayName`; Exit `0`. `SessionProfiles` antwortet mit `ListSessionProfilesAsync` (Envelope `session-profiles.list`) und `RuntimeModules` mit `ListRuntimeModulesAsync` (Envelope `runtime-modules.list`: ID, Version, Aktivierung, Operationen und Gültigkeit jedes Moduls sowie seine Einstellungen mit Typ, Standardwert und Flag). Unbekannte Kategorie: `Unknown discovery category`, Exit `2`. Analysepunkte (Junctions/symbolische Verknüpfungen) werden übersprungen, OMSI-Dateien werden als Windows-1252 gelesen. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | keiner | lokal | STABLE_BETA | Geltungsbereich, der für jede Kategorie außer `Entrypoints` weitergegeben wird; diese verwendet `/map` als Geltungsbereich. |

<a id="timeouts-and-observation"></a>
### Timeouts und Beobachtung

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` Sekunden | Wert aus Spec/Profil, sonst `180` | Startzeit | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. Der Eigentümer wartet diesen Wert plus 5 s auf `Running`; `OL_E_STARTUP_TIMEOUT` beendet die Sitzung mit Exit `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` Sekunden | Wert aus Spec/Profil, sonst `30` | Startzeit | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: mit `/cooperative-shutdown` die Zeit, die der Supervisor auf das Beenden von OMSI wartet, bevor er es terminiert; ohne dieses Flag wird OMSI sofort terminiert und der Wert hat keine Wirkung. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | aus | Startzeit | EXPERIMENTAL | `Behavior.CooperativeShutdown`: Das Beenden einer laufenden Sitzung fordert OMSI auf, sich über seine eigene Beenden-Routine zu schließen (die, die nach der Bestätigung des Beendens durch einen Benutzer abläuft: sie speichert `options.cfg` und die `laststn.osn` der Karte und schließt dann), und wartet `ShutdownTimeoutSeconds`, bevor es terminiert wird. Opt-in, bis zur Runtime-Validierung; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`, wenn OMSI terminiert werden musste. Die Dateien der Sitzung werden nach dem Beenden in jedem Fall wiederhergestellt. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | keiner (läuft, bis OMSI sich beendet oder ein Stopp angefordert wird) | Runtime (Eigentümer) | STABLE_BETA | Obergrenze für die Laufphase: Nach `n` Sekunden im Zustand `Running` wird der kanonische Stopp angefordert. Ein Stopp über Tray oder Pipe oder das Beenden von OMSI beendet sie früher. `0` stoppt unmittelbar nach `Running`. |

### Recovery

| Flag | Syntax und Werte | Standard | Phase | Stabilität | Verhalten |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | aus | lokal | STABLE_BETA | Meldet, ob `<root>\.omsilaunch\journal.json` aussteht (`pending`), stellt nie wieder her; Exit `0`. Übernimmt die Installations-Lease: `OL_E_INSTALLATION_BUSY` (Exit `7`), solange ein Eigentümer sie hält. |
| `/recover` | `/recover` | aus | lokal | STABLE_BETA | Stellt ein ausstehendes Journal wieder her (Backups werden zuerst gegen den SHA-256 des Snapshots geprüft; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` werden in `diagnostics` gemeldet). Exit `0`, wenn nichts ausstand oder die Wiederherstellung abgeschlossen wurde; `8`, wenn ein Journal ausstand und bestehen bleibt. Wird mit `OL_E_INSTALLATION_BUSY` verweigert, solange der im Journal erfasste OMSI-Prozess (PID, Erstellungszeit, Pfad der ausführbaren Datei) oder, bei einem Journal nach `HandoffCreated` ohne PID, irgendein `Omsi.exe` aus diesem Stammverzeichnis aktiv ist. Jeder Sitzungsstart führt dieselbe Recovery automatisch aus, bevor die Installation gelesen wird. |
| `/clear-module-data` | `/clear-module-data:<module id>` | keiner | lokal, keine Sitzung | EXPERIMENTAL | Löscht das Datenverzeichnis `<root>\.omsilaunch\module-data\<id>` des Moduls und sonst nichts; Envelope `module-data.clear` mit `module` und `cleared` (`false`, wenn nichts zu löschen war); Exit `0`. Nimmt die Installations-Lease: `OL_E_INSTALLATION_BUSY` (Exit `7`), solange eine Sitzung die Installation besitzt. Ein über einen Link erreichter Modul-Datenordner wird abgelehnt (`OL_E_MODULE_INTEGRITY`); ein Link darin wird als Link entfernt. |
| `/pack-module` | `/pack-module:<module directory>` | keiner | lokal, keine Sitzung | EXPERIMENTAL | Schreibt die SHA-256-Festlegungen der Dateien, die die `module.json` des Verzeichnisses nennt (Komponenten und Assets), und schreibt nur diese Werte neu; Envelope `module.pack` (`module`, `directory`, `written`, `intact`, `files`); Exit `0`, `2` bei einem Manifest, das sich nicht parsen lässt, oder einer fehlenden Datei. Berührt nie eine Installation. Siehe [Ein Modul packen](runtime-modules.md#packing-a-module). |
| `/verify-module` | `/verify-module:<module directory>` | keiner | lokal, keine Sitzung | EXPERIMENTAL | Meldet dasselbe, ohne zu schreiben (`module.verify`); Exit `0`, wenn jede festgelegte Datei übereinstimmt (eine Datei ohne Festlegung ist keine Abweichung), `7`, wenn eine Festlegung nicht übereinstimmt, `2` bei einem Manifest, das sich nicht parsen lässt, oder einer fehlenden Datei. |

<a id="output-formats"></a>
## Ausgabeformate

- **Erfolgs-Envelope** (`CliInput.WriteEnvelope`, mit `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, eingerückt. Weitergeleitete Antworten auf `session status` und `events read` enthalten zusätzlich ein Element `metadata`, wenn ältere Ereignisse ausgelassen wurden, damit die Antwort in den Steuerungs-Frame passt (`events_dropped_count`, siehe [lokale Steuerung](local-control.md)). Ohne `--json` wird nur `<object>` als eingerücktes JSON ausgegeben, gefolgt von `Note: <n> older events were omitted to fit the control frame.`, wenn Ereignisse verworfen wurden.
- **Fehler-Envelope** (`CliInput.WriteError`, mit `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Ohne `--json`: `OL_E_<CODE>: message` in einer Zeile. Kategorien: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Unter `OmsiLaunchW.exe` werden derselbe Code und dieselbe Meldung in einem Meldungsfenster angezeigt.
- **Plan und Status** (`CliInput.Write`): Die Datensätze `SessionPlan`, `SessionStatus` und `RuntimeCommandResult` werden als eingerücktes JSON **ohne** Envelope ausgegeben. Ohne `--json` wird ein Plan als `Plan: READY profile=Omsi23004_692EBFBF` oder `Plan: NOT RUNNABLE profile=...` zusammengefasst; andere Datensätze werden weiterhin als JSON ausgegeben. Enum-Werte werden als Ganzzahlen serialisiert (`SessionState.Running` ist `14`, `Completed` ist `18`, `Failed` ist `19`).
- In Envelopes verwendete Befehlsnamen: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile` sowie die Runtime-Operations-ID bei weitergeleiteten Runtime-Befehlen.
- Unter `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) wird nichts auf die Konsole geschrieben, sofern nicht `--json` angegeben ist.

<a id="errors-per-command"></a>
## Fehler pro Befehl

| Befehl | Typische Fehlercodes | Exit |
|---|---|---|
| Jeder Parse-Fehler | `OL_E_INVALID_ARGUMENT`, Sitzungsprofil-Codes (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Client-Route, `/runtime` (Client) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); vom Eigentümer zurückgegebene `OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, z. B. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Vorabprüfung des Eigentümers | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); ausstehend, aber nicht wiederhergestellt (`8`) | `7`, `8` |
| `/list` | unbekannte Kategorie (`2`); `OL_E_INSTALLATION_NOT_FOUND`/fehlende Verzeichnisse (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, Start | Plandiagnosen: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (installierte Plugin-Closure), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informativ) | `1` |
| Sitzungsstart | `OL_E_PLAN_NOT_RUNNABLE` (erneute Planung beim Start, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normalerweise von der Planung als Plandiagnose gemeldet, Exit `1`; `7` nur, wenn sich die Plugin-Dateien zwischen Planung und Start ändern), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (Sitzung `Failed`, `1`) | `1`, `7` |
| Unbehandelte Ausnahme an beliebiger Stelle | klassifiziert durch `CliProgram.Classify` (siehe [Exitcodes](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Umgebung

| Variable | Gesetzt von | Wirkung |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: Konsolenausgabe unterdrückt, Fehler als Meldungsfenster, `/silent` wird nicht erneut delegiert. |

<a id="see-also"></a>
## Siehe auch

[CLI-Beispiele](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [Exitcodes](exit-codes.md) · [Fehler](errors.md) · [lokale Steuerung](local-control.md) · [Windows-Tray](windows-tray.md) · [Runtime-Steuerung](runtime-control.md) · [Capabilities](capabilities.md) · [LaunchSpec](launchspec.md) · [Sitzungsprofile](session-profiles.md) · [Paketierung](packaging.md) · [Kompatibilität](compatibility.md) · [bekannte Einschränkungen](known-limitations.md) · [öffentliche API](public-api.md)
