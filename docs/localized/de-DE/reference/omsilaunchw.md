# OmsiLaunchW.exe-Referenz

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Übersetzung der [englischen Originalseite](../../../reference/omsilaunchw.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

`OmsiLaunchW.exe` ist der Host des OmsiLaunch-Controllers für das Windows-Subsystem (GUI). Er akzeptiert dieselbe Befehlszeile wie `OmsiLaunch.exe` und führt denselben Controller-Code aus (`OmsiLaunch.Controller.dll`). Der einzige Unterschied liegt in der Art der Rückmeldung: Es gibt kein Konsolenfenster, Fehler werden als Meldungsfenster angezeigt, und eine laufende Sitzung ist nur über ihr [Tray-Symbol](windows-tray.md) sichtbar.

Maßgebliche Quellen: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (der native Shim), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) und `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## OmsiLaunch.exe und OmsiLaunchW.exe im Vergleich

| Aspekt | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsystem | Konsole. Öffnet ein Konsolenfenster, wenn es aus dem Explorer gestartet wird. | Windows (GUI). Kein Konsolenfenster. |
| Nativer Shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Umgebung | unverändert | setzt `OMSILAUNCH_WINDOWS_HOST=1` für den Controller-Prozess, bevor .NET startet |
| Argumente | werden mit `CommandLineToArgvW` in Tokens zerlegt und unverändert an den Controller übergeben | ebenso, daher akzeptieren beide Hosts exakt dieselben Flags und Befehle ([CLI-Referenz](cli.md)) |
| Textausgabe | wird nach stdout geschrieben | unterdrückt, sofern nicht `--json` angegeben ist (dann werden die JSON-Envelopes nach stdout geschrieben, das ein Aufrufer umleiten kann) |
| Fehler | `OL_E_...: message` oder ein JSON-Fehler-Envelope | dieselben Regeln für die Konsolenausgabe, **zusätzlich** ein Meldungsfenster für jeden Fehler (siehe [Fehlerdialoge](#failure-dialogs)) |
| `/silent` | startet `OmsiLaunchW.exe` mit den übrigen Argumenten und gibt `0` zurück | ignoriert: Der Befehl läuft bereits im Windows-Host |
| Tray-Symbol | wird für eine Eigentümersitzung angezeigt | wird für eine Eigentümersitzung angezeigt |
| Stopppfade | Tray, `session stop`, Beenden von OMSI, `/observe-seconds`, Ctrl+C, Schließen der Konsole | Tray, `session stop`, Beenden von OMSI, `/observe-seconds` (es gibt keine Konsole, daher entfallen Ctrl+C und das Schließen der Konsole) |
| Exitcodes | [`PublicExitCode`](exit-codes.md) `0`..`10`, Shim-Codes `100`..`106` | dieselben Codes |

<a id="how-it-starts"></a>
## Wie er startet

1. Der Shim ermittelt seinen eigenen Pfad (`GetModuleFileNameW`) und erwartet `OmsiLaunch.Controller.dll` im selben Verzeichnis.
2. Er zerlegt die Befehlszeile in Tokens (`CommandLineToArgvW`) und setzt `OMSILAUNCH_WINDOWS_HOST=1`.
3. Er findet `hostfxr` über die mitgelieferte `nethost.dll`, lädt es, initialisiert den Controller mit den Argumenten (der Controller-Pfad ist nicht Teil der Argumentliste, die der CLI-Parser sieht) und führt ihn aus.
4. Der Shim gibt den Exitcode des Controllers unverändert zurück.

Schlägt ein Schritt fehl, bevor der Controller läuft, zeigt der Shim ein Meldungsfenster mit dem Titel `OmsiLaunch` und dem Text `OmsiLaunch could not start the .NET host (code N).` an und beendet sich mit diesem Code:

| Code | Fehlgeschlagener Schritt |
| --- | --- |
| `100` | der Pfad der ausführbaren Datei konnte nicht ermittelt werden |
| `101` | die Befehlszeile konnte nicht in Tokens zerlegt werden |
| `102` | die Suche nach dem Speicherort von `hostfxr` ist fehlgeschlagen (meist: die .NET-6-x64-Runtime ist nicht installiert) |
| `103` | der Pfad von `hostfxr` konnte nicht abgerufen werden |
| `104` | `hostfxr` konnte nicht geladen werden |
| `105` | erforderliche Exporte von `hostfxr` fehlen |
| `106` | der verwaltete Host konnte nicht initialisiert werden (z. B. fehlt `OmsiLaunch.Controller.dll` oder ihre Runtime-Konfiguration, oder die Windows-Desktop-Runtime ist nicht vorhanden) |

`OmsiLaunch.exe` verwendet dieselbe Tabelle, gibt aber nichts aus. Diese Dialoge wurden noch nicht zur Laufzeit erzeugt (siehe [Status der Runtime-Validierung](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Aufruf

Direkter Start, über eine Verknüpfung, ein Skript oder ein anderes Programm:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Über `OmsiLaunch.exe` mit `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Legen Sie die ausführbaren Dateien in der OMSI-2-Installation ab (das Paketlayout, siehe [Paketierung](packaging.md)); ohne Installationsargument ist die Installation das Verzeichnis, das die ausführbare Datei enthält. Eine explizite Installation wird wie bei `OmsiLaunch.exe` als erstes Argument übergeben (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Da es sich um ein GUI-Programm handelt, warten `cmd.exe` und der Explorer nicht darauf. Um in einem Skript zu warten und den Exitcode zu lesen, verwenden Sie `start /wait OmsiLaunchW.exe ...` in `cmd.exe` oder `Start-Process -Wait -PassThru` in PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

## /silent-Delegation

`OmsiLaunch.exe ... /silent` (oder `--silent`) führt, sofern es nicht bereits unter `OmsiLaunchW.exe` läuft, Folgendes aus (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Es sucht `OmsiLaunchW.exe` im Verzeichnis von `OmsiLaunch.exe`. Fehlt die Datei: `OL_E_WINDOWS_HOST_MISSING`, Exitcode `7`.
2. Es startet `OmsiLaunchW.exe` über `ShellExecute` (`UseShellExecute = true`) mit dem aktuellen Verzeichnis und allen Argumenten außer `/silent`/`--silent` in der ursprünglichen Reihenfolge. `ShellExecute` gibt die Handles des Aufrufers nicht an den neuen Prozess weiter, sodass ein Aufrufer, der die Ausgabe von `OmsiLaunch.exe /silent` erfasst, nicht für die gesamte Dauer der Sitzung blockiert wird (Runtime-Abschluss BUG-03). Wird kein Prozess zurückgegeben: `OL_E_WINDOWS_HOST_START_FAILED`, Exitcode `7`.
3. Es schreibt das `silent`-Envelope und beendet sich sofort mit `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

Exitcode `0` bedeutet nur, dass `OmsiLaunchW.exe` gestartet wurde. Das Ergebnis der Sitzung (Planungsfehler, ein bereits aktiver Eigentümer, ein fehlgeschlagener Start) meldet `OmsiLaunchW.exe` mit eigenen Dialogen und Diagnosen, und die beiden Prozesse sind danach voneinander unabhängig: `OmsiLaunch.exe` ist beendet, und `OmsiLaunchW.exe` ist der Sitzungseigentümer. Verwenden Sie `OmsiLaunch.exe session status`, um die Sitzung anzuzeigen.

`/silent` wird vor jedem anderen Befehl angewendet, daher führt auch `OmsiLaunch.exe /silent session status` den Befehl `session status` innerhalb von `OmsiLaunchW.exe` aus, wo seine Ausgabe unterdrückt wird. Verwenden Sie `/silent` nur für Starts.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Was jeder Befehl unter OmsiLaunchW.exe bewirkt

| Befehlszeile | Ergebnis |
| --- | --- |
| keine Argumente | führt `detect` still aus und beendet sich mit `0` (es wird nichts angezeigt) |
| ein Start (`/new`, `/saved:...`, `/spec:...`, ein Sitzungsprofil) mit ausführbarem Plan und ohne Eigentümer | wird zum Sitzungseigentümer: Tray-Symbol während des Starts und des Betriebs; beendet sich, wenn die Sitzung endet (`0` abgeschlossen, `1` fehlgeschlagen) |
| ein Start, dessen Plan nicht ausführbar ist | Dialog mit der letzten `OL_E_`-Diagnose des Plans (Ersatzwert `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), Exitcode `1` (Dokumentationsaudit BUG-06; vor der Korrektur beendete er sich ohne Meldung) |
| ein Start, während für die Installation bereits ein Eigentümer aktiv ist | Dialog `OL_E_SESSION_ALREADY_ACTIVE`, Exitcode `7`; die laufende Sitzung ist nicht betroffen |
| ein Start, der `Running` nicht erreicht | Dialog `The OMSI session did not reach gameplay.` mit der letzten `OL_E_`-Diagnose, Exitcode `1`. OMSI wird beendet und die Dateien werden vom Sitzungs-Supervisor wiederhergestellt, während der Dialog geöffnet ist; der Prozess beendet sich, nachdem der Dialog geschlossen wurde und `CloseAsync` abgeschlossen ist |
| ein ungültiges Argument, ein unbekanntes Flag oder ein ungültiges Sitzungsprofil | Dialog mit `OL_E_INVALID_ARGUMENT` oder dem `OL_E_SESSION_PROFILE_*`-Code, Exitcode `2` |
| ein Client-Befehl (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) ohne Eigentümer | Dialog `OL_E_NO_ACTIVE_SESSION`, Exitcode `4` |
| ein Client-Befehl, den der Eigentümer ablehnt | Dialog mit dem Ablehnungscode (z. B. `OL_E_CONTROL_SESSION_MISMATCH` oder ein Runtime-Fehlercode), Exitcode `7` (`2` bei einer unbekannten Operation oder einem fehlenden Argument) |
| ein erfolgreicher Client- oder Erkennungsbefehl (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | keine sichtbare Ausgabe, sofern nicht `--json` angegeben und stdout umgeleitet ist; Exitcode wie bei `OmsiLaunch.exe` |
| `/plan` oder `/validate` | kein Dialog, auch nicht bei einem nicht ausführbaren Plan; Exitcode `0` oder `1` |
| jeder andere Controller-Fehler | Dialog mit dem klassifizierten `OL_E_`-Code; Exitcode gemäß [Exitcodes](exit-codes.md) |

<a id="failure-dialogs"></a>
## Fehlerdialoge

Jeder Fehler, den `OmsiLaunch.exe` ausgeben würde, wird zusätzlich als modales Meldungsfenster angezeigt (`WindowsHost.ShowFailure`), auch wenn `--json` angegeben ist:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` ist die Fehlermeldung bzw. bei einem Sitzungsfehler die Meldung der letzten `OL_E_`-Diagnose. Bekannte Einschränkung: Bei einem vom Plugin gemeldeten Fehler ist die Meldung die unverarbeitete Fehler-Nutzlast des Plugins (z. B. `{"name":"world.failed",...}`); die `Code:`-Zeile ist korrekt (siehe [bekannte Einschränkungen](known-limitations.md)). Der Dialog ist modal, und der Prozess beendet sich, nachdem er geschlossen wurde. Runtime-Nachweis: Argumentfehler, keine aktive Sitzung und ein Fehler vor Spielbeginn (Runtime-Abschluss `T04`).

<a id="session-tray-and-exit"></a>
## Sitzung, Tray und Beenden

Eine Sitzung, deren Eigentümer `OmsiLaunchW.exe` ist, verhält sich exakt wie eine Sitzung, deren Eigentümer `OmsiLaunch.exe` ist (siehe [Sitzungslebenszyklus](../concepts/session-lifecycle.md)):

- Das Tray-Symbol erscheint, sobald die Sitzung gestartet ist, noch bevor OMSI den Spielbetrieb erreicht, es sei denn, `Presentation.SuppressTrayIcon` ist in einer `/spec`-Datei gesetzt. Mit `SuppressTrayIcon` gibt es überhaupt keine sichtbare Oberfläche; beenden Sie die Sitzung mit `OmsiLaunch.exe session stop` oder durch Schließen von OMSI.
- Die Sitzung endet, wenn OMSI beendet wird, wenn `End session` (`Sitzung beenden`) im Tray bestätigt wird, wenn ein Client `session stop` sendet oder wenn `/observe-seconds` abgelaufen ist. OmsiLaunch beendet dann OMSI, falls es noch läuft, stellt jede von ihm geänderte Datei wieder her, gibt die Installations-Lease frei, entfernt das Tray-Symbol und beendet sich.
- Wird `OmsiLaunchW.exe` selbst gewaltsam beendet, führt der nächste OmsiLaunch-Start für diese Installation die Recovery (Absturzwiederherstellung) der ausstehenden Transaktion durch (siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Schnellstart

1. Installieren Sie das Paket in das OMSI-2-Verzeichnis ([Installation](../getting-started/installation.md)).
2. Erstellen Sie eine Verknüpfung zu `OmsiLaunchW.exe` mit den Argumenten der Sitzung, z. B. `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Starten Sie sie. OMSI startet ohne Konsolenfenster; das OmsiLaunch-Symbol erscheint im Infobereich (Benachrichtigungsbereich).
4. Klicken Sie mit der rechten Maustaste auf das Symbol → `Status`, um die Sitzung anzuzeigen, oder `End session` → `End session` (`Sitzung beenden`), um sie zu beenden.
5. Wenn etwas schiefgeht, zeigt der Dialog den Fehlercode an; Einzelheiten finden Sie in `<OMSI_PATH>\.omsilaunch\diagnostics`.
