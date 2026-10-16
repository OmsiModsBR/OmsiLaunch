# CLI-Beispiele

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Übersetzung der [englischen Originalseite](../../../reference/cli-examples.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Minimale, korrekte Aufrufe von `OmsiLaunch.exe` für OmsiLaunch `0.1.0-beta4`, jeweils mit dem erwarteten Exitcode des Prozesses und einem Hinweis darauf, was geändert und wiederhergestellt wird. Jedes Beispiel wird, sofern nicht anders angegeben, aus dem OMSI-Installationsstammverzeichnis (`<OMSI_PATH>`) ausgeführt; die Syntax ist in der [CLI-Referenz](cli.md) definiert, die Exitcodes unter [Exitcodes](exit-codes.md). `--json` kann jedem Befehl hinzugefügt werden, um das strukturierte Envelope zu erhalten.

<a id="conventions"></a>
## Konventionen

- **Ändert**: Dateien oder OMSI-Zustand, die der Befehl verändert. „Sitzungs-Overlay“ bezeichnet eine Datei, die als Snapshot in die Transaktion aufgenommen, vor dem Start von OMSI angewendet und beim Ende der Sitzung Byte für Byte wiederhergestellt wird.
- **Wiederhergestellt**: was beim Ende der Sitzung (normaler Stopp, Ctrl+C, Tray, `session stop`, `/observe-seconds`) oder durch die Recovery (Absturzwiederherstellung) rückgängig gemacht wird.
- Runtime-Schreibvorgänge (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) ändern nur den Speicher von OMSI; sie werden nie rückgängig gemacht, da OMSI beim Stopp beendet wird.
- Platzhalter: `<OMSI_PATH>` ist die OMSI-2-Installation, die das OmsiLaunch-Paket enthält (zum Beispiel `C:\OMSI 2`); `<OTHER_OMSI_PATH>` eine andere Installation; `<SPEC_PATH>` und `<ITX_PATH>` eine eigene LaunchSpec-Datei bzw. ein eigenes Internettexturen-Profil; `<HANDLE>` ist ein Handle, das vom vorangehenden `list`- oder `create`-Befehl ausgegeben wurde, und `<BASE64>` sind Base64-kodierte Pixeldaten. Jeder andere Wert ist ein Literal, das auf einer Standardinstallation von OMSI 2 funktioniert (`grundorf-quick` ist das auf dieser Seite definierte Beispielprofil).
- Setzen Sie einen Pfad mit Leerzeichen in Anführungszeichen und beenden Sie einen Pfad in Anführungszeichen nicht mit `\` (die Windows-Argumentauswertung macht aus `\"` ein literales Anführungszeichen): `"C:\OMSI 2"`, nicht `"C:\OMSI 2\"`.
- Jede Befehlszeile auf dieser Seite wird vom Dokumentations-Gate geparst (`tests/OmsiLaunch.DocumentationTests`, Gate `examples`); die Beispiele zu Identität, Inhaltserkennung, Planung und Client wurden außerdem gegen eine echte Installation ausgeführt (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identität und Inhaltserkennung (keine Sitzung)

```text
OmsiLaunch.exe /version
```
Exit `0`. Gibt `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` aus. Ändert nichts.

```text
OmsiLaunch.exe profiles --json
```
Exit `0`. Listet die unterstützten `Omsi.exe`-Hashes und ihren Validierungsstatus auf. Ändert nichts.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Exit `0`. Öffentlicher Capability-Katalog; `help <family>` filtert ihn. Ändert nichts.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Exit `0` (beide Formen sind identisch). Meldet laufende `Omsi.exe`-Prozesse und ob ein OmsiLaunch-Eigentümer für diese Installation antwortet. Ändert nichts.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Exit `0` (`2` bei einer unbekannten Kategorie). Reine Leseerkennung; Junction-Zyklen werden übersprungen. Ändert nichts. Die hier ausgegebenen `Identity`-Werte sind exakt die Zeichenfolgen, die `/map`, `/saved`, `/vehicle-scope` und eine `LaunchSpec` erwarten (zum Beispiel `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Planung und Validierung

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Exit `0`, wenn der Plan `READY` ist, `1` bei `NOT RUNNABLE` (zum Beispiel `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Ändert nichts; OMSI wird nicht gestartet.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Exit `0`/`1` wie oben; `/validate` ist ein Alias von `/plan`. Das JSON ist der unveränderte `SessionPlan` (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Exit `1`. `/date`, `/time`, `/year`, `/weather*` und Spielerfahrzeug-Flags werden akzeptiert, aber von diesem Build nicht angewendet; der Plan enthält `OL_E_CAPABILITY_UNAVAILABLE` und ist nicht ausführbar.

```text
OmsiLaunch.exe /last /plan
```
Exit `1`. `LAST_MAP_STATE` ist für dieses Profil nicht verfügbar (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Sitzungen starten (Eigentümermodus)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Exit `0`, wenn die Sitzung mit `Completed` endet, `1` bei `Failed` oder einem nicht ausführbaren Plan. Ändert: Sitzungs-Overlays `GUI\NewSplashscreen_ENG.bmp` und `GUI\NewSplashscreen_<lang>.bmp` (das verwaltete Startbild ist der Standard), die Behandlung von `closecheck`, die Start-Übergabe. Wiederhergestellt: jedes Overlay, Byte für Byte, wenn die Sitzung endet. Die Konsole bleibt verbunden, bis OMSI sich beendet, „End session“ im Tray bestätigt wird, ein Client `session stop` sendet oder Ctrl+C gedrückt wird.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Exit `0`. Wie oben, aber die Sitzung wird 8 s nach Erreichen von `Running` beendet (früher bei einem Stopp über Tray/Pipe). Wird von Validierungsskripten verwendet.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Exit `0`/`1`. SAVED_SITUATION: Karte, Uhrzeit und Position stammen aus der `.osn` (`situations\Linie 5.osn` wird mit OMSI 2 ausgeliefert und startet auf Berlin-Spandau mit einem Spielerbus). Der Wert ist die von `/list:Situations` ausgegebene Situationsidentität (relativ zur Installation, ohne Beachtung der Groß-/Kleinschreibung); ein reiner Dateiname wie `Linie 5.osn` wird nicht aufgelöst (`OL_E_SITUATION_NOT_FOUND`, Exit `1`). `/map` oder `/entrypoint-index` zusammen mit `/saved` wird mit Exit `2` abgelehnt. Änderungen und Wiederherstellung wie bei NEW_MAP. OMSI selbst schreibt die Karte der Situation in `options.cfg` `[last_map]`; dieser Schreibvorgang von OMSI wird nicht rückgängig gemacht, sofern nicht ein `/set` die Datei `options.cfg` überlagert (siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Exit `0`. Ändert: `options.cfg` (Sitzungs-Overlay, semantischer Token-/Vektor-Patch; CP1252-Bytes bleiben erhalten) sowie die Startbild-Overlays. Wiederhergestellt: `options.cfg` und die Startbilddateien exakt (RV-005, RV-006). `/set:graphics.texture=...` endet mit Exit `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` endet mit Exit `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Exit `0`. Ändert: kein Startbild-Overlay; nur die Start-Übergabe und die Behandlung von `closecheck`. Wiederhergestellt: für das Startbild ist nichts wiederherzustellen.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Exit `0` (`1` mit `OL_E_SESSION_PRESENTATION_INVALID`, wenn das Verzeichnis oder eine BMP fehlt oder nicht 640x480 mit 24 Bit ist). Ändert: `GUI\NewSplashscreen_ENG.bmp` und `GUI\NewSplashscreen_PTB.bmp` aus dem benutzerdefinierten Verzeichnis (Sitzungs-Overlay). Wiederhergestellt: beide Dateien exakt.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Exit `0`. Ändert: nichts auf dem Datenträger außer den Startbild-Overlays; der prozessinterne Downloader wird für die Sitzung unterdrückt.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Exit `0` (`2` mit `OL_E_ITX_PROFILE_REQUIRED`, wenn das Profil fehlt; `1` bei einem ungültigen Profil oder einem Ziel außerhalb von `Texture\`). Ändert: `Texture\standard.itx` (Sitzungs-Overlay); jedes im Profil aufgeführte Ziel und `Texture\standard.ipr` sind Sitzungslöschungen. Wiederhergestellt: Overlay entfernt, gelöschte Originale wiederhergestellt; Dateien, die OMSI während der Sitzung unter diesen Pfaden erstellt hat, werden als Nebenprodukte der Sitzung entfernt.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Exit `0`. Wartet bis zu 300 s (+5 s) auf `Running` statt der standardmäßigen 180 s. `/shutdown-timeout:60` wird akzeptiert, hat in diesem Build aber keine Wirkung.

<a id="predefined-session-profile"></a>
### Vordefiniertes Sitzungsprofil

Profildatei `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Exit `0`. Ändert: `options.cfg` (Einstellungen der Voreinstellung, Sitzungs-Overlay) und die Startbild-Overlays. Wiederhergestellt: alle davon. Das Hinzufügen von `/map:...` oder `/set:graphics.maxFPS=60` endet mit Exit `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); das Weglassen von `/predefined-profile-index` endet mit Exit `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Das mitgelieferte Beispiel `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` zeigt das vollständige Schema, aber im Auslieferungszustand fordert sein Block `new:` die Angaben `date`, `time` und `weather` an, die dieser Build nicht anwenden kann: Die Planung mit `/new` ergibt `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); entfernen Sie diese Schlüssel vor der Verwendung.

<a id="launchspec-file"></a>
### LaunchSpec-Datei

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Exit `0`/`1`. Das mitgelieferte Beispiel wählt Grundorf, Einstiegspunkt-Index `1`, verwaltetes Startbild und native Internettexturen aus; `RootPath: "."` wird zum Verzeichnis der ausführbaren Datei aufgelöst. Änderungen wie beim expliziten NEW_MAP-Beispiel. Eine Spec mit einer unbekannten Eigenschaft endet mit Exit `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); eine fehlende Datei endet mit Exit `6` (`OL_E_SPEC_NOT_FOUND`); eine Datei über 1 MiB endet mit Exit `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Exit `0`/`1`. Die explizite Installation `<OTHER_OMSI_PATH>` hat Vorrang vor dem `RootPath` der Spec; `/startup-timeout` überschreibt `Behavior.StartupTimeoutSeconds` der Spec nur, weil es angegeben wurde.

<a id="silent-detached-start"></a>
### Stiller (losgelöster) Start

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Exit `0`, sobald `OmsiLaunchW.exe` gestartet wurde (`{"delegated": true, "host_process_id": <pid>}`); `7`, wenn `OmsiLaunchW.exe` fehlt (`OL_E_WINDOWS_HOST_MISSING`) oder nicht gestartet werden konnte. Der Launcher kehrt sofort zurück und hält weder die Konsole noch die Pipes des Aufrufers offen: Ein Skript, das seine Ausgabe erfasst, erhält sofort das Dateiende (Runtime-Closure `T04`). Die Sitzung selbst läuft in `OmsiLaunchW.exe`: keine Konsolenausgabe, Fehler als Meldungsfenster, Tray-Symbol verfügbar. Prüfen Sie den Fortschritt mit `session status`, `events watch` und `.omsilaunch\diagnostics\<sessionId>-host.log`. Vollständige Referenz: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Eine laufende Sitzung steuern (Client-Modus)

Führen Sie diese Befehle aus demselben Installationsverzeichnis aus, während ein Eigentümer läuft. Jeder endet mit Exit `4` (`OL_E_NO_ACTIVE_SESSION`), wenn kein Eigentümer antwortet, und mit `7` bei einem Steuerungsfehler.

```text
OmsiLaunch.exe session status --json
```
Exit `0`. Gibt `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents` zurück. Ändert nichts.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Exit `0` (`events watch` läuft bis Ctrl+C). Begrenzte Runtime-Ereignisse (`gameplay.entered`, D3D-Lebenszyklusereignisse, ...). Ändert nichts.

```text
OmsiLaunch.exe session stop
```
Exit `0` (`{"accepted": true, "session_id": "..."}`). Fordert den kanonischen Stopp an: OMSI wird beendet, die Overlays werden vom Eigentümer wiederhergestellt, das Journal wird gelöscht. Der Client kehrt sofort zurück; der Eigentümerprozess beendet sich nach der Wiederherstellung.

<a id="runtime-reads"></a>
## Runtime-Lesevorgänge

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Exit `0` mit dem `RuntimeCommandResult` (`Succeeded`, `Values`) im Envelope. Ändert nichts. Timeout 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, Exit `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Exit `0`; `2`, wenn ein erforderliches Argument fehlt (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, gemeldet, bevor die Anfrage gesendet wird); `7`, wenn das Plugin die Anfrage ablehnt: `OL_E_RUNTIME_OPERATION_FAILED` mit dem konkreten Grund in `Values.detail`, zum Beispiel `OL_E_RUNTIME_OBJECT_HANDLE_STALE` für ein Handle, das nicht mehr dasselbe Objekt bezeichnet, oder `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Handles sind sitzungsbezogen und stammen aus dem vorangehenden `list`. Variablen-, Konstanten- und Kurvennamen werden von jedem Fahrzeugmodell definiert: Entnehmen Sie sie dem Ergebnis von `list`. Die obigen Namen wurden für `rv-000001` aufgelistet, den Spielerbus einer Sitzung mit `situations\Linie 5.osn`. Ändert nichts.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Exit `0`. Operationen ohne hierarchische Route – oder jede Route – lassen sich über die Operations-ID ansprechen. `timetable.track-entries.list` ist eine begrenzte Liste: Auf `situations\Linie 5.osn` gab sie 137 von 825 Einträgen mit `truncated=true` zurück (Runtime-Nachtest des Dokumentationsaudits). Ändert nichts.

<a id="runtime-writes"></a>
## Runtime-Schreibvorgänge

```text
OmsiLaunch.exe time set --minute=30
```
Exit `0`. Ändert die Uhr von OMSI im Speicher (validiert: Schreiben, Zurücklesen, Wiederherstellen durch ein zweites `time set`). Wird beim Stopp nicht rückgängig gemacht.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Exit `0` (`2`, wenn `--family` bei `camera lock` fehlt). Ändert den Kamerazustand für die Sitzung. `camera lock` benötigt ein PlayerVehicle (zum Beispiel eine `/saved`-Sitzung); in einer `/new`-Sitzung im Headless-Betrieb schlägt es fehl (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` in `Values.detail`). Sperren und Entsperren wurden mit einer gespeicherten Situation zur Laufzeit validiert (Familien 0, 2 und 1 mit Zurücklesen der Kamera). Wird beim Stopp nicht rückgängig gemacht; die Sperrrichtlinie endet mit der Sitzung.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Exit `0` (`2`, wenn `handle`, `name` oder `value` fehlt). Ändert eine numerische Skriptvariable dieses Fahrzeugs. Wird nicht rückgängig gemacht.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Exit `0`. Schreibt den Quell-Wetterdatensatz von OMSI und beendet die Live-METAR-Aktualisierungen; OMSI leitet Nebel, Licht, tatsächlichen Wind und Luftfeuchtigkeit beim nächsten nicht pausierten Wetter-Tick daraus ab. Jeder Wert wird geprüft, bevor etwas geschrieben wird (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, Exit `7`). Ein abgeleiteter Schlüssel wie `relative_humidity` liefert `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` und nennt den stattdessen zu setzenden Schlüssel. Wird nicht rückgängig gemacht. EXPERIMENTAL: Runtime-Validierung steht aus.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Exit `0`. Wechselt zum Live-METAR-Wetter für diese Station; OMSI lädt es bei der nächsten Wetteraktualisierung herunter. EXPERIMENTAL: Runtime-Validierung steht aus.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Exit `0`. `scripts string set` läuft über das eigene `SetStringVariable` von OMSI und gibt den zurückgelesenen Wert zurück; ein Name, den das Fahrzeugskript nicht deklariert, liefert `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (Exit `7`). `scripts trigger` löst einen Trigger aus, wie es die Eingabehandler von OMSI tun (`1` drücken, `0` loslassen); das Fahrzeugskript führt ihn sofort aus. Trigger- und Variablennamen werden vom jeweiligen Fahrzeugmodell definiert; die obigen sind Beispiele. Wird nicht rückgängig gemacht. EXPERIMENTAL: Runtime-Validierung steht aus.

## Spawn

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Exit `0` mit dem neuen `rv-NNNNNN`-Handle in `Values` (`2`, wenn `--model` fehlt; `7` bei `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Client-Timeout 30 s. Ändert die Sammlung der Straßenfahrzeuge (ein Fahrzeug hinzugefügt); weist das Spielerfahrzeug nicht zu. Wird nicht rückgängig gemacht; das Fahrzeug verschwindet beim Stopp zusammen mit OMSI.

<a id="d3d-textures"></a>
## D3D-Texturen

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` ist der von `create` ausgegebene `handle`-Wert (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` muss für die 32-Bit-Formate zu `width * height * 4` Bytes dekodieren (8 x 8 x 4 = 256 Bytes) und zu höchstens 48 KiB. Exit `0`; `2` bei fehlenden erforderlichen Argumenten; `7` bei `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (zweite Freigabe oder Beschreiben nach der Freigabe), `OL_E_D3D_STALE_RESOURCE_HANDLE` (ein Handle aus der Zeit vor einem Geräte-Reset oder aus einer anderen Sitzung), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Erstellt GPU-Ressourcen, die der Sitzung gehören; sie werden explizit oder beim Ende von OMSI freigegeben. Es werden keine Dateien berührt.

<a id="owner-side-single-runtime-operation"></a>
## Einzelne Runtime-Operation auf Eigentümerseite

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Exit `0`. Startet eine Sitzung, führt `time.read` nach `Running` einmal aus (Timeout 5 s), schreibt `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, läuft 5 s weiter, stoppt und stellt wieder her. Ein Runtime-Fehler wird als `runtime_error` ausgegeben und beendet die Sitzung nicht.

## Recovery

```text
OmsiLaunch.exe /recovery-status --json
```
Exit `0`: `{"pending": false, ...}`, wenn kein Journal existiert, `{"pending": true, "recovered": false}`, wenn eines existiert. Exit `7` (`OL_E_INSTALLATION_BUSY`), solange ein Eigentümer die Installation hält. Ändert nichts.

```text
OmsiLaunch.exe /recover --json
```
Exit `0`, wenn nichts ausstand oder die Wiederherstellung abgeschlossen wurde (`recovered: true`; `diagnostics` kann `restore.session-artifact-removed` und `OL_W_RESTORE_FOREIGN_FILE_RETAINED` enthalten); Exit `8`, wenn das Journal ausstand und weiterhin aussteht (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); Exit `7`, solange ein Eigentümer oder das im Journal erfasste OMSI die Installation hält (`OL_E_INSTALLATION_BUSY`); Exit `10` (`OL_E_INTERNAL`), wenn die wiederhergestellten Bytes die Prüfung nicht bestehen (`Restore hash mismatch` oder `Restore presence mismatch`; das Journal bleibt ausstehend). Ändert: stellt jede im Journal erfasste Datei aus `.omsilaunch\backup\<sessionId>\` wieder her, nachdem ihr SHA-256 geprüft wurde, und löscht anschließend das Journal und das Backup-Verzeichnis. Wird mit `OL_E_INSTALLATION_BUSY` verweigert, solange das im Journal erfasste `Omsi.exe` noch aktiv ist (Runtime-Closure `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Schnellprüfung des Exitcodes (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
