# Installation

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Übersetzung der [englischen Originalseite](../../../getting-started/installation.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite erläutert, was OmsiLaunch `0.1.0-beta4` voraussetzt, welchen OMSI-Build es unterstützt, wie das Release-Paket in ein Installationsstammverzeichnis von OMSI installiert wird und wie Sie die Installation mit `/version` und `/plan` überprüfen, bevor Sie eine Sitzung starten. Der Paketinhalt ist unter [Paketierung](../reference/packaging.md) spezifiziert; der erste Start ist unter [Erste Sitzung](first-session.md) beschrieben.

<a id="requirements"></a>
## Voraussetzungen

| Voraussetzung | Detail | Fehler, wenn sie fehlt |
|---|---|---|
| Windows 10 oder neuer, 64-Bit | Der Controller prüft `Environment.OSVersion.Version.Major >= 10`, ein x64-Betriebssystem und einen x64-Prozess. | Plan nicht ausführbar mit `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (oder `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), Exit `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` erfordert `Microsoft.NETCore.App` 6.0 und `Microsoft.WindowsDesktop.App` 6.0 (Windows Forms wird von der Tray-Anzeige verwendet). Die Shims finden sie mit `nethost.dll`. | `OmsiLaunch.exe` beendet sich vor jeglicher Ausgabe mit Shim-Code `102`..`106`; `OmsiLaunchW.exe` zeigt `OmsiLaunch could not start the .NET host (code N).` an. |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` erfordert `Microsoft.NETCore.App` 6.0 für x86, da das Plugin innerhalb der 32-Bit-`Omsi.exe` läuft. Das x86-Paket der .NET 6 Desktop Runtime erfüllt diese Anforderung ebenfalls. | Das Plugin startet nicht innerhalb von OMSI; die Sitzung erreicht `Running` nicht (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), Exit `1`, Dateien wiederhergestellt. |
| Installiertes OMSI 2 | Eine 32-Bit-`Omsi.exe` mit einer OMSI-2-Version (2.x) sowie `maps\` und `Vehicles\` im Stammverzeichnis. Es ist kein bestimmter Hash der Programmdatei erforderlich; OMSI 2.3.004 ist das bekannte, zur Laufzeit validierte Layout, und bei anderen Builds wird jede Capability im Prozess aufgelöst und verifiziert. | `OL_E_UNSUPPORTED_BUILD`; Plan nicht ausführbar, Exit `1`. Eine auf dem Build nicht verfügbare Capability schlägt mit `OL_E_CAPABILITY_UNRESOLVED` fehl. Siehe [Kompatibilität](../reference/compatibility.md). |
| Beschreibbares Installationsstammverzeichnis | Die Transaktion schreibt `.omsilaunch\`, Overlays unter `GUI\`, `Texture\` und `options.cfg` und stellt sie wieder her; das Stammverzeichnis muss für den aktuellen Benutzer beschreibbar sein (vermeiden Sie `Program Files` ohne entsprechende Berechtigungen). | `OL_E_INSTALLATION_NOT_WRITABLE`, Exit `1`. |
| Ein Benutzer, ein Eigentümer pro Installation | Die Installations-Lease `Local\OmsiLaunch.Installation.<sha256(root)>` und die Steuerungs-Pipe gelten pro Anmeldesitzung. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, Exit `7`. |

Beide Runtimes sind separate Downloads von Microsoft; installieren Sie für .NET 6 die x64 Desktop Runtime und die x86 Runtime (oder die x86 Desktop Runtime). Keine weitere Komponente ist erforderlich. Keine Daten verlassen den Rechner.

<a id="confirm-the-omsi-build"></a>
## OMSI-Build bestätigen

Ersetzen Sie `<OMSI_PATH>` durch Ihr OMSI-2-Verzeichnis (zum Beispiel `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

Der Hash muss einer der beiden oben aufgeführten sein. `OmsiLaunch.exe profiles` gibt nach der Installation dieselbe Liste mit ihrem Validierungsstatus aus.

<a id="install-the-package"></a>
## Paket installieren

1. Laden Sie `OmsiLaunch-0.1.0-beta4.zip` und `OmsiLaunch-0.1.0-beta4.zip.sha256` herunter; überprüfen Sie die Prüfsumme (`Get-FileHash` muss dem Wert in der `.sha256`-Datei entsprechen).
2. Entpacken Sie das Archiv **direkt in das Installationsstammverzeichnis von OMSI** (den Ordner, der `Omsi.exe` enthält). Das Archiv ist für dieses Stammverzeichnis aufgebaut:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` und die übrigen `OmsiLaunch.*.dll`-Controller-Assemblys, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` im Stammverzeichnis;
   - die Plugin-Gesamtheit (Closure) des permanenten Plugins `plugins\OmsiLaunch.*` (9 Dateien) neben Ihren vorhandenen Plugins, die niemals angerührt werden;
   - `.omsilaunch\` mit den Startbild-Assets, der Offline-Dokumentation und Beispielen.
3. Belassen Sie `release-manifest.json` neben `OmsiLaunch.exe`. Diese Datei ermöglicht es jedem Start, die installierten Plugin-Dateien per SHA-256 zu überprüfen (`plugin.integrity.reference = manifest`); ohne sie werden nur Vorhandensein und Selbstkonsistenz geprüft (`plugin.integrity.reference = self`).
4. Verschieben oder benennen Sie nichts unter `plugins\OmsiLaunch.*` um, und legen Sie keine Plugin-Binärdateien in `.omsilaunch\` ab.

Ein Upgrade ist derselbe Vorgang: Entpacken Sie das neue Paket über die alten Dateien, während keine Sitzung läuft und keine Recovery (Absturzwiederherstellung) aussteht (`OmsiLaunch.exe /recovery-status`). Die Plugin-Hashes und das Manifest müssen immer aus demselben Paket stammen (andernfalls `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Überprüfen

Führen Sie im OMSI-Stammverzeichnis aus (das Installationsargument ist standardmäßig das Verzeichnis, das `OmsiLaunch.exe` enthält):

```text
OmsiLaunch.exe /version
```
Erwartet: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; Exit `0`. Exit `102`..`106` bedeutet, dass die x64-.NET-6-Runtime fehlt oder das Paket unvollständig ist.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Erwartet: die unterstützten Hashes, dann die in dieser Installation gefundenen Karten; Exit `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Erwartet: `Plan: READY profile=Omsi23004` (auf dem bekannten Layout; sonst `unrecognized`) und Exit `0` (verwenden Sie eine beliebige Kartenidentität aus `/list:Maps`; der Einstiegspunkt-Index muss ein angezeigter Index dieser Karte sein, siehe `/list:Entrypoints /map:<identity>`). Mit `--json` listet der Plan `TouchedFiles` (die Startbild-Overlays), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED` und auf dem bekannten Layout `RESOLVED_KNOWN_LAYOUT` für die nativen Start-Capabilities), `Diagnostics` (einschließlich `plugin.integrity.reference`) und `IsRunnable` auf. `Plan: NOT RUNNABLE` mit Exit `1` nennt den Grund in `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). Die Planung startet niemals OMSI und schreibt niemals in die Installation.

<a id="where-things-live-afterwards"></a>
## Wo sich danach was befindet

| Pfad | Inhalt |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Host-Trace jeder Sitzung (die 50 neuesten Sitzungen werden aufbewahrt) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Protokoll der Tray-Anzeige |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Nur vorhanden, solange eine Transaktion aussteht; siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Ihre vordefinierten Sitzungsprofile; siehe [Sitzungsprofile](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Assets des verwalteten Startbilds |
| `<root>\.omsilaunch\docs\` | Diese Dokumentation, offline (beginnen Sie bei `README.md`; die CLI-Referenz ist `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Beispiel-LaunchSpec und Beispiel-Sitzungsprofil |

<a id="uninstall"></a>
## Deinstallation

Beenden Sie jede Sitzung, führen Sie `OmsiLaunch.exe /recovery-status` aus (und `/recover`, falls etwas aussteht), und löschen Sie dann die Produktdateien im Stammverzeichnis, `plugins\OmsiLaunch.*` und `.omsilaunch\`. Details unter [Paketierung](../reference/packaging.md).

<a id="next"></a>
## Weiter

[Erste Sitzung](first-session.md) · [CLI-Referenz](../reference/cli.md) · [Bekannte Einschränkungen](../reference/known-limitations.md)
