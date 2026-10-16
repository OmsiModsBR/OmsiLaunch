# Erste Sitzung

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Übersetzung der [englischen Originalseite](../../../getting-started/first-session.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite führt durch die erste verwaltete OMSI-Sitzung mit OmsiLaunch `0.1.0-beta4`: Planen, ohne OMSI zu starten, Starten mit expliziten Flags, Starten mit einem vordefinierten Sitzungsprofil, Steuern und Beenden der Sitzung sowie das Auffinden der Diagnosen im Anschluss. Sie setzt voraus, dass das Paket wie unter [Installation](installation.md) beschrieben installiert ist. Jedes Flag ist in der [CLI-Referenz](../reference/cli.md) spezifiziert; weitere Aufrufe finden Sie unter [CLI-Beispiele](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Was eine Sitzung tut

Eine Sitzung ist eine Transaktion um einen OMSI-Prozess: OmsiLaunch erstellt Snapshots der Dateien, die es anrühren wird (standardmäßig die beiden Startbild-Bitmaps unter `GUI\`, dazu `options.cfg`, wenn `/set`-Overlays angefordert werden), schreibt ein dauerhaftes Journal unter `.omsilaunch\`, wendet die Overlays an, startet `Omsi.exe` mit dem permanenten Plugin, wartet, bis das Spielgeschehen erreicht ist (`Running`), hält die Sitzung steuerbar und beendet am Ende OMSI und stellt jede angerührte Datei Byte für Byte wieder her. `/new` wählt niemals stillschweigend eine Karte oder einen Einstiegspunkt aus: Beide müssen angegeben werden oder aus einer `/spec`-Datei bzw. einem Sitzungsprofil stammen.

<a id="1-plan-nothing-is-started"></a>
## 1. Planen (nichts wird gestartet)

Führen Sie die Befehle im OMSI-Stammverzeichnis aus; die Installation ist standardmäßig das Verzeichnis, das `OmsiLaunch.exe` enthält.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

Der Plan muss `"IsRunnable": true` angeben (Exit `0`). Er listet `TouchedFiles` und `PlannedMutations` auf, sodass Sie genau sehen, was die Sitzung mit Overlays überlagern wird. Beheben Sie jede `OL_E_`-Diagnose, bevor Sie fortfahren; es wurde nichts geschrieben.

Die mitgelieferte Beispielspezifikation tut dasselbe mit einer Datei:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Mit expliziten Flags starten

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Was in dieser Reihenfolge geschieht:

1. Der Plan wird ausgegeben (`Plan: READY profile=Omsi23004` auf dem bekannten Layout).
2. Recovery eines älteren ausstehenden Journals, Erwerb der Lease, Snapshot, Journal, Overlays, Integritätsprüfung des Plugins, Start von `Omsi.exe`.
3. Das Tray-Symbol erscheint (`OmsiLaunch is running` (`OmsiLaunch wird ausgeführt`)); siehe [Windows-Tray](../reference/windows-tray.md).
4. Sobald das Spielgeschehen erreicht ist, wird der Status `Running` als JSON ausgegeben (`"State": 14`). Das Standard-Timeout für den Start beträgt 180 s (mit `/startup-timeout:<1..600>` änderbar).
5. Die Konsole bleibt verbunden, bis die Sitzung endet. Schließen Sie das Konsolenfenster nicht, um zu stoppen: Verwenden Sie eine der unten beschriebenen Stoppmethoden.

Optionale Ergänzungen für den ersten Lauf:

- `/set:graphics.maxFPS=60` (ein `options.cfg`-Overlay, das am Ende wiederhergestellt wird);
- `/splash:Unset`, um den OMSI-Startbild unverändert zu lassen, oder `/splash-language:DEU`, um das lokalisierte verwaltete Startbild auszuwählen;
- `/observe-seconds:30`, um 30 s nach `Running` automatisch zu stoppen (nützlich für einen Smoke-Test);
- `--json` für strukturierte Ausgabe.

Flags, die ein Datum, eine Uhrzeit, ein Jahr, ein Wetter oder ein Spielerfahrzeug anfordern (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...), werden akzeptiert, können von diesem Build aber nicht angewendet werden: Der Plan wird `NOT RUNNABLE` mit `OL_E_CAPABILITY_UNAVAILABLE`. Lassen Sie sie weg.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Mit einem vordefinierten Sitzungsprofil starten

Ein Sitzungsprofil ist eine YAML-Datei unter `<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, die die Karte, den Einstiegspunkt und bis zu 99 Voreinstellungen von Einstellungen festlegt (Schema `omsilaunch.session-profile/v1`; vollständige Referenz unter [Sitzungsprofile](../reference/session-profiles.md)). Erstellen Sie `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
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
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Dann:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Zu beachtende Regeln: Die `id` muss dem Verzeichnisnamen entsprechen; der Index ist `1..99`; explizite Flags, die ein vom Profil verwaltetes Feld überschreiben würden (`/map`, `/entrypoint-index`, ein `/set`-Schlüssel, den die Voreinstellung verwaltet, Startbild-Flags, wenn die Voreinstellung `presentation` enthält), werden mit `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` abgelehnt (Exit `2`); der `new:`-Block gilt nur mit `/new`; mit `/saved:<file.osn>` muss die Karte der Situation unter `compatibility.maps` aufgeführt sein. Die mitgelieferte `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` veranschaulicht das vollständige Schema, ihr `new:`-Block setzt jedoch `date`, `time` und `weather`, die dieser Build nicht anwenden kann; kopieren Sie sie daher erst, nachdem Sie diese Schlüssel entfernt haben.

<a id="4-control-the-running-session"></a>
## 4. Laufende Sitzung steuern

Aus einer zweiten Konsole im selben Verzeichnis (ohne Installationsargument):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Diese Befehle laufen über die lokale Steuerungs-Pipe dieser Installation ([lokale Steuerung](../reference/local-control.md)); Exit `4` bedeutet, dass hier kein Eigentümer läuft.

<a id="5-stop"></a>
## 5. Beenden

Jede dieser Methoden beendet die Sitzung auf dieselbe Weise (OMSI wird beendet, dann wird jede angerührte Datei wiederhergestellt, dann werden Journal und Backup gelöscht):

| Methode | Hinweise |
|---|---|
| Tray-Symbol → `End session` (`Sitzung beenden`) → bestätigen | Sowohl in Sitzungen von `OmsiLaunch.exe` als auch von `OmsiLaunchW.exe` verfügbar. |
| `OmsiLaunch.exe session stop` | Aus einer anderen Konsole; kehrt sofort zurück, der Eigentümer schließt die Wiederherstellung ab. |
| Ctrl+C in der Konsole des Eigentümers | Fordert den Stopp an; der Eigentümer wartet vor dem Beenden auf die Wiederherstellung. |
| `/observe-seconds:<n>` | Automatischer Stopp `n` Sekunden nach `Running`. |
| OMSI beendet sich selbst | Der Eigentümer erkennt `ProcessExited` und stellt wieder her. |

Die eigene Beendigungsroutine von OMSI läuft nicht, daher schreibt OMSI `options.cfg` beim Beenden nicht neu; das ist beabsichtigt, damit die Wiederherstellung exakt ist. Schließen Sie das Konsolenfenster des Eigentümers mit der X-Schaltfläche, bleiben der Wiederherstellung nur 4 s; wurde sie nicht abgeschlossen, vervollständigt der nächste Start (oder `OmsiLaunch.exe /recover`) sie anhand des Journals. Der Exitcode des Eigentümers ist `0`, wenn die Sitzung in `Completed` endete.

<a id="6-where-to-look-afterwards"></a>
## 6. Wo Sie danach nachsehen

| Ort | Inhalt |
|---|---|
| Konsole / `--json`-Ausgabe | Plan, Status `Running`, Endstatus (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Der Host-Trace der Sitzung (Transaktionsgrenzen, Prozessstart, Plugin-Übergabe, Spielgeschehen erreicht, Wiederherstellung). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Ergebnis einer vom Eigentümer ausgeführten `/runtime:`-Operation. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Ereignisse der Tray-Anzeige. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` nach einem sauberen Ende. `true` bedeutet, dass ein Journal zurückgeblieben ist; führen Sie `OmsiLaunch.exe /recover` aus. |

Hat die Sitzung das Spielgeschehen nicht erreicht, enthält der Endstatus die fehlgeschlagene `OL_E_`-Diagnose (zum Beispiel `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), der Exitcode ist `1`, und die Dateien wurden trotzdem wiederhergestellt. Siehe [Exitcodes](../reference/exit-codes.md), [Fehler](../reference/errors.md) und [bekannte Einschränkungen](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Ausführung ohne Konsole

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delegiert an `OmsiLaunchW.exe` und gibt sofort `0` zurück. Die Sitzung hat keine Konsole; Fehler erscheinen als Meldungsfenster, und das Tray-Symbol ist die einzige sichtbare Oberfläche. Verwenden Sie `session status`, `events watch` und das Diagnoseverzeichnis, um sie zu verfolgen. Das vollständige Verhalten des Windows-Hosts ist in der [Referenz zu OmsiLaunchW.exe](../reference/omsilaunchw.md) beschrieben.
