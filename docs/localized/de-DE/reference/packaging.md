# Paketierung und Release-Struktur

<!-- l10n: source=reference/packaging.md source-sha256=8d5d5b27225188339094529a2435a976635969dd53d614c53d768169087e7ffa -->
> Übersetzung der [englischen Originalseite](../../../reference/packaging.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite beschreibt das OmsiLaunch-Release-Paket `0.1.0-beta4`: was `tools\New-ReleasePackage.ps1` erzeugt, die Felder von `release-manifest.json`, wie der Controller das Manifest zur Laufzeit verwendet, um die Plugin-Gesamtheit (Closure) des permanenten Plugins zu verifizieren, wie das Paket in ein OMSI-Installationsstammverzeichnis installiert und daraus entfernt wird, was das `.omsilaunch`-Verzeichnis nach der Verwendung enthält, sowie die Validierungsskripte (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). Die Produktidentität stammt aus `OmsiLaunch.Version.props`. Installationsschritte für Benutzer finden Sie unter [Installation](../getting-started/installation.md); die Rolle der Plugin-Closure zur Laufzeit ist unter [permanentes Plugin](../concepts/permanent-plugin.md) beschrieben.

<a id="product-identity-omsilaunchversionprops"></a>
## Produktidentität (`OmsiLaunch.Version.props`)

| Eigenschaft | Wert | Verwendet für |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | Manifest `product`, Windows `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro` | Windows `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/lmonteirotech/OmsiLaunch` | Windows `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | Manifest `product_version`, Windows `ProductVersion`, informative Assembly-Version (`/version`), öffentlicher ZIP-Name |
| `OmsiLaunchManagedVersion` | `0.1.0` | Basis der verwalteten Assembly-Version |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Assembly- und Windows-Dateiversion |
| `OmsiLaunchPackageAlias` | `current` | Manifest `package_alias`, Staging-Ordner und Alias-ZIP-Name |

`Directory.Build.props` setzt `InformationalVersion` auf `OmsiLaunchProductVersion` ohne Quellrevision, sodass `OmsiLaunch.exe /version` genau `0.1.0-beta4` ausgibt.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (Standardausgabe `artifacts\release`) stellt ein Paket aus bereits gebauten Artefakten zusammen. Das Schreiben nach `artifacts\release` wird verweigert, wenn `OmsiLaunch-<product_version>.zip` bereits existiert, sofern nicht `-AllowOverwritePublished` angegeben ist; Kandidatenpakete werden in ein anderes Verzeichnis geschrieben (die Offline-Validierung verwendet `artifacts\candidate\post-round-a`).

Vor dem Staging wird jede Build-Ausgabe darauf geprüft, ob sie veraltet ist.

- **`OmsiLaunch.Native.x86.dll`: nach Inhalt, nicht nach Zeitstempel.** Der native Build schreibt `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (Target `WriteOmsiLaunchNativeBuildReceipt` in der `.vcxproj`): den SHA-256 der erzeugten DLL (`output=`) und jeder Quelle, aus der sie gebaut wurde (`source=<sha256>|<path>`: die `.cpp`, die `.rc`, die `.vcxproj` und `OmsiLaunch.Version.props`). Die Paketierung weist die DLL zurück, wenn ihr Hash nicht der erfassten Ausgabe entspricht (`does not match its build receipt`: eine veraltete oder fremde Kopie, unabhängig von ihrem Zeitstempel), wenn sich eine erfasste Quelle geändert hat (`Native source changed after the recorded build`), wenn eine native Quelle nicht vom Beleg erfasst ist oder wenn der Beleg fehlt.
- **Shims und verwaltete Assemblys: nach Zeitstempel.** Keines davon darf älter sein als die Quellen seines eigenen Projekts (`.cpp`/`.rc`/`.vcxproj` jedes Shims; das eigene Projekt jeder verwalteten Assembly). Eine veraltete Eingabe bricht die Paketierung mit `Stale build artifact` ab.

Das Paket wird dann in einem völlig neuen, eindeutig benannten Staging-Verzeichnis (`.staging-<guid>` im Ausgabeverzeichnis) zusammengestellt, sodass keine Datei aus einem früheren Lauf in die Closure gelangen kann. Der vorherige Ordner `OmsiLaunch-current` und die Archive werden erst ersetzt, nachdem alle unten aufgeführten Prüfungen bestanden wurden.

| Quelle | Ziel im Paket |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | Stammverzeichnis |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| CLI `assets\splash\*.bmp` (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | Stammverzeichnis |
| jede Datei unter `docs\` außer `docs\localized\` (die englische Dokumentation, gleiche Verzeichnisstruktur) | `.omsilaunch\docs\` (damit `.omsilaunch\docs\reference\cli.md`, der im Verwendungstext der CLI ausgegebene Pfad, existiert; Dokumentationsaudit BUG-08). Links aus `docs\README.md` auf die Zusammenfassungen im Repository-Stammverzeichnis (`PUBLIC-API.md` und andere) funktionieren nur im Quell-Repository. |
| `docs\localized\LOCALIZATION-MANIFEST.md` und `docs\localized\<locale>\**` für jedes in diesem Manifest aufgeführte Gebietsschema (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` und `ja-JP`) | `.omsilaunch\docs\localized\` (gleiche Struktur); ein aufgeführtes, aber fehlendes Gebietsschema bricht das Skript ab |

Anschließend hasht es jede bereitgestellte Datei, schreibt `release-manifest.json` im Paketstammverzeichnis als UTF-8 **ohne** BOM (das Ergebnis hängt nicht mehr von der PowerShell-Edition ab), führt `Test-ReleasePackageIntegrity.ps1` auf dem Staging-Stand aus, vergleicht jede bereitgestellte Plugin-Datei und `OmsiLaunch.Native.x86.dll` erneut mit ihrer Build-Ausgabe, komprimiert den Staging-Stand, **entpackt das Archiv in ein neues temporäres Verzeichnis und validiert die entpackte Closure gegen dasselbe Manifest** (das archivierte Manifest muss bytegleich mit dem validierten sein), veröffentlicht dann den Staging-Stand als `OmsiLaunch-current` und das Archiv als `OmsiLaunch-current.zip`, kopiert es nach `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) und schreibt `OmsiLaunch-0.1.0-beta4.zip.sha256` mit dem Inhalt `<SHA-256>  <file name>`. Jedes fehlende Artefakt bricht das Skript ab. Das Skript baut nicht selbst; führen Sie zuerst `Invoke-OfflineValidation.ps1` aus (oder die einzelnen `dotnet build`- / MSBuild-Schritte).

<a id="package-layout"></a>
## Paketstruktur

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Nur die Produktdateien im Stammverzeichnis, `plugins\OmsiLaunch.*` und `.omsilaunch\` gehören dem Produkt. Plugins von Drittanbietern unter `plugins\` werden von OmsiLaunch nie aufgezählt, kopiert, gehasht, entfernt oder wiederhergestellt.

## `release-manifest.json`

| Feld | Typ | Bedeutung |
|---|---|---|
| `product` | Zeichenfolge | `OmsiLaunch` |
| `product_version` | Zeichenfolge | `0.1.0-beta4` |
| `package_alias` | Zeichenfolge | `current` |
| `control_protocol` | Zeichenfolge | `0.1`; muss `PublicCapabilityRegistry.ProtocolVersion` entsprechen |
| `requirement` | Zeichenfolge | `OMSI 2` |
| `known_layouts` | object[] | `build_id` und `code_sha256` jedes bekannten Layouts (`Omsi23004`, `13ec110a...c028`); ein schneller Pfad, keine Voraussetzung |
| `configuration` | Zeichenfolge | `Release` oder `Debug` |
| `generated_utc` | Zeichenfolge | Build-Zeitpunkt nach ISO-8601 |
| `files[]` | object[] | `path` (Schrägstriche, relativ zum Paketstammverzeichnis), `bytes`, `sha256` (Hex in Großbuchstaben) für jede paketierte Datei |

Das Manifest sind Daten, niemals ausführbare Richtlinien: Der Controller liest nur die `plugins/`-Einträge. Der Leser akzeptiert die Datei mit oder ohne UTF-8-BOM (Manifeste, die vor dieser Korrektur von Windows PowerShell 5.1 geschrieben wurden, enthalten eine).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Verwendung des Manifests zur Laufzeit (Plugin-Integrität)

Vor jeder Planung und jedem Start erstellt `OmsiLaunchService.LoadArtifacts` die erwartete Plugin-Closure (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. Der Controller sucht `release-manifest.json` neben `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Ist sie vorhanden, extrahiert `ReleaseManifest.TryReadPluginHashes` die `plugins/*`-Hashes (`OL_E_RELEASE_MANIFEST_INVALID`, wenn die Datei nicht als Manifest gelesen werden kann).
2. Jede installierte Datei `<root>\plugins\OmsiLaunch.*` wird gehasht (SHA-256) und verglichen:
   - mit einem Manifest: mit dem Hash im Manifest; die Plandiagnose `plugin.integrity.reference = manifest` wird erfasst. Fehlende Datei → `OL_E_PERMANENT_PLUGIN_MISSING`; Datei vorhanden, aber nicht aufgeführt → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; Hash weicht ab → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - ohne Manifest (Entwicklungsstruktur oder eine Installation, bei der das Manifest weggelassen wurde): Es können nur Vorhandensein und Selbstkonsistenz gegenüber der paketierten Kopie neben dem Controller geprüft werden; `plugin.integrity.reference = self`.
3. Ein Fehler kennzeichnet den Plan als nicht ausführbar (`OL_E_RUNTIME_ARTIFACT_MISSING` mit dem Detail) oder weist den Start zurück (Exitcode `7`).

Plugin-Dateien werden von einer Sitzung nie bereitgestellt, in einen Snapshot aufgenommen, wiederhergestellt oder entfernt; die Closure ist ein permanenter Bestandteil der Installation. Die x86-Datei `OmsiLaunch.Native.x86.dll` wird ausschließlich aus `plugins\` geladen; die verwalteten Assemblys deklarieren `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Installation in das OMSI-Stammverzeichnis

1. Verifizieren Sie das Archiv: Vergleichen Sie `OmsiLaunch-0.1.0-beta4.zip` mit `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Entpacken Sie das Archiv **direkt in das OMSI-Installationsstammverzeichnis** (das Verzeichnis, das `Omsi.exe` enthält). Dadurch werden die Dateien im Stammverzeichnis, `plugins\OmsiLaunch.*` (neben etwaigen Plugins von Drittanbietern) und `.omsilaunch\` angelegt.
3. Belassen Sie `release-manifest.json` neben `OmsiLaunch.exe`: Es ermöglicht die manifestbasierte Plugin-Integritätsprüfung. Manifest und Binärdateien müssen aus demselben Paket stammen: Neue Binärdateien über einem älteren Manifest (oder umgekehrt) lassen jeden Start mit `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` fehlschlagen. `Test-ReleasePresentation.ps1 -InstallPackage` kopiert das Manifest jetzt zusammen mit den Produktdateien; früher wurde es übersprungen, wodurch ein älteres Manifest neben neueren Binärdateien zurückblieb (Runde A RA-007).
4. Prüfen Sie die Kohärenz schreibgeschützt mit `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` muss `true` sein.
5. Verschieben Sie keine Plugin-Binärdateien nach `.omsilaunch\` und benennen Sie `plugins\OmsiLaunch.*` nicht um.
6. Verifizieren Sie mit `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` und einem `/plan` (siehe [erste Sitzung](../getting-started/first-session.md)).

Vorhandene `.omsilaunch\assets\splash\*.bmp`-Dateien werden von einer Sitzung nie überschrieben (ein explizit verwalteter Asset-Satz bleibt bestehen); sie durch Entpacken eines neuen Pakets zu überschreiben, ist eine bewusste Aktion des Benutzers.

<a id="the-omsilaunch-directory-after-use"></a>
## Das `.omsilaunch`-Verzeichnis nach der Verwendung

| Pfad | Erstellt von | Lebensdauer |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Paket, oder bei der ersten Sitzung mit verwaltetem Startbild kopiert | dauerhaft |
| `docs\`, `examples\` | Paket | dauerhaft |
| `session-profiles\<id>\profile.yaml` | Benutzer | dauerhaft; siehe [Sitzungsprofile](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | jede Sitzung | für die 50 neuesten Sitzungen aufbewahrt; ältere Dateien mit Sitzungspräfix werden gelöscht, wenn eine neue Sitzung startet |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, Validierungs-Harnesses | gleiche Aufbewahrung (Sitzungspräfix) |
| `diagnostics\tray-host.log` | Tray-Anzeige | dauerhaft, wird fortgeschrieben |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | dauerhaft (ohne Sitzungspräfix) |
| `journal.json` | Transaktion | existiert von `Prepared` bis `Restored`; ein Überbleibsel bedeutet, dass eine Recovery aussteht (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | Transaktion | Snapshots berührter Dateien; nach der Wiederherstellung entfernt |

Keine Daten verlassen den Rechner. Siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Deinstallation

1. Stellen Sie sicher, dass keine Sitzung läuft (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) und keine Recovery aussteht (`OmsiLaunch.exe /recovery-status`; führen Sie `/recover` aus, wenn `pending` gleich `true` ist), damit die OMSI-Dateien bereits wiederhergestellt sind.
2. Löschen Sie `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Lassen Sie andere Plugins unverändert.
3. Löschen Sie die oben in der Struktur aufgeführten Produktdateien im Stammverzeichnis (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Löschen Sie `.omsilaunch\` (dadurch werden Ihre Sitzungsprofile und Diagnosen entfernt). Löschen Sie es niemals, solange `journal.json` existiert.

Eine abgeschlossene Sitzung stellt jede Datei wieder her, die ihr gehörte, sodass keine weitere Bereinigung erforderlich ist. Dateien, die OMSI selbst während der Ausführung schreibt (z. B. `[last_map]` in `options.cfg`, Caches, `laststn.osn`, Protokolle), sind der normale Zustand von OMSI und werden nicht zurückgesetzt; siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Validierungsskripte

| Skript | Zweck | Berührt OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Baut `OmsiLaunch.sln` mit Warnungen als Fehler sowie die drei nativen Projekte (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) über MSBuild und führt dann jede Offline-Suite aus: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` und, sofern nicht übersprungen, `OmsiLaunch.DocumentationTests`, danach die Paketierungs-Regression `Test-PackagingPipeline.ps1` (mit `-SkipNative` übersprungen). Gibt `OFFLINE VALIDATION PASSED`/`FAILED` aus. | Nein |
| `tools\New-ReleasePackage.ps1` | Schutz vor veralteten Artefakten, Staging, Manifest, Integritäts-Selbstprüfung, ZIP, Prüfsumme (siehe oben). | Nein |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifiziert, dass das Manifest genau die paketierten Dateien mit übereinstimmender Größe und SHA-256 aufführt, dass die erforderliche Closure (drei ausführbare Dateien, der Controller, die neun permanenten Plugin-Dateien einschließlich `OmsiLaunch.Native.x86.dll`) vorhanden ist und dass die Konfiguration `Release` ist. Mit `-InstallationRoot` vergleicht es die Produktdateien der Installation **schreibgeschützt** mit dem Paket. Exitcode `0` = kohärent. | Nein (schreibgeschützt) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Erzeugt ein Kandidatenpaket aus sauberem Staging unter `artifacts\candidate\post-round-a` und verlangt, dass die bereitgestellte Closure und das erneut entpackte Archiv die Integritätsprüfung bestehen. Weist nach, dass das Integritäts-Gate eine manipulierte DLL, eine manipulierte oder alte `Native.x86`, eine veraltete Plugin-Kopie, eine gelöschte aufgeführte Datei, eine unerwartete Datei, einen geänderten oder fehlerhaften Manifest-Hash, doppelte Einträge (exakt, nach Groß-/Kleinschreibung, nach Trennzeichen), übergeordnete und absolute Pfade sowie ungültiges JSON zurückweist; dass der Packager eine alte `Native.x86` in der Build-Ausgabe (selbst mit neuerem Zeitstempel) und einen Beleg mit geänderten Quellen zurückweist; und dass das veröffentlichte Archiv nie überschrieben wird. Build-Ausgaben werden bytegenau wiederhergestellt. Wird von `Invoke-OfflineValidation.ps1` ausgeführt. | Nein |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Entpackt die ZIP-Datei nach `artifacts\release\identity-verification`, prüft `product`/`product_version`/`package_alias` und verifiziert `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` jeder `.exe`/`.dll` außer `nethost.dll` und `YamlDotNet.dll`, den `InternalName`/`OriginalFilename` beider Shims sowie, dass `OmsiLaunch.exe` ein eingebettetes Symbol enthält. | Nein |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Validiert die paketierte Release-Programmdatei gegen eine echte Installation: Das Manifest muss `Release` sein, darf keine `Debug`- oder `runtime/plugin/`-Pfade enthalten und muss `plugins/OmsiLaunch.*` installieren; die vier Startbild-Assets müssen existieren. Führt drei `/plan`-Fälle aus (verwalteter Standard, verwaltete benutzerdefinierte Assets, `/splash:Unset`). Mit `-RunOmsi` (erfordert `-InstallPackage`) startet es jeden Fall mit `/observe-seconds`, überwacht `GUI\NewSplashscreen_ENG.bmp` und `GUI\NewSplashscreen_PTB.bmp` während der Sitzung und prüft exakte Wiederherstellung, keine `Omsi.exe`, kein `journal.json`, Exitcode `0`, unveränderte Hashes der Plugins von Drittanbietern und einen unveränderten Satz permanenter Plugins. Schreibt `.omsilaunch\diagnostics\release-presentation-validation.json`. | Ja mit `-RunOmsi` (auf die Sitzung beschränkt, wiederhergestellt) |

Beide `Test-*`-Skripte lesen `OmsiLaunch.Version.props`, um die erwartete Version zu ermitteln.
