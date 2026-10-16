# OmsiLaunch-Dokumentation

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Übersetzung der [englischen Originalseite](../../README.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Dies ist die normative englische Dokumentation für OmsiLaunch `0.1.0-beta4`, die
Basislinie nach der Härtung. OmsiLaunch bietet programmierbaren Start,
Sitzungseigentümerschaft und Runtime-Steuerung für ein installiertes OMSI 2; jede native Capability
wird pro Build aufgelöst und verifiziert (siehe [Kompatibilität](reference/compatibility.md)). Jede Seite unter `docs/` beschreibt, was der aktuelle Code
tut; wenn eine Seite und der Code voneinander abweichen, gilt der Code, und die Seite ist ein Fehler.

Durchgehend verwendetes Stabilitätsvokabular: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Flags, die geparst werden, aber nichts bewirken, sind als
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` gekennzeichnet. Nichts wird als
zur Laufzeit validiert bezeichnet, sofern der
[Status der Runtime-Validierung](status/runtime-validation-status.md) dies nicht angibt.

<a id="who-reads-what"></a>
## Wer liest was

| Zielgruppe | Hier beginnen | Danach |
| --- | --- | --- |
| Anwender (CLI, Verknüpfungen, Sitzungsprofile) | [Installation](getting-started/installation.md), [Erste Sitzung](getting-started/first-session.md) | [CLI-Referenz](reference/cli.md), [CLI-Beispiele](reference/cli-examples.md), [Sitzungsprofile](reference/session-profiles.md), [Windows-Tray](reference/windows-tray.md), [Exitcodes](reference/exit-codes.md) |
| Integratoren (`OmsiLaunch.Api`, lokale IPC) | [Schnellstart zur öffentlichen API](getting-started/api-quick-start.md), [Referenz der öffentlichen API](reference/public-api.md), [LaunchSpec-Referenz](reference/launchspec.md) | [Sitzungslebenszyklus](concepts/session-lifecycle.md), [Runtime-Steuerung](reference/runtime-control.md), [Capabilities](reference/capabilities.md), [Lokale Steuerung / IPC](reference/local-control.md), [Fehlerreferenz](reference/errors.md) |
| Maintainer (Release, Validierung, Grenzen) | [Paketierung](reference/packaging.md), [Modell des permanenten Plugins](concepts/permanent-plugin.md) | [Transaktionen und Recovery](concepts/transactions-and-recovery.md), [Kompatibilität](reference/compatibility.md), [Bekannte Einschränkungen](reference/known-limitations.md), [Status der Runtime-Validierung](status/runtime-validation-status.md) |

## Navigation

| Seite | Zweck |
| --- | --- |
| [Erste Schritte](getting-started/first-session.md) | Eine Sitzung aus dem OMSI-Stammverzeichnis planen, starten, beobachten und beenden. |
| [Installation](getting-started/installation.md) | Voraussetzungen, Entpacken des Pakets in das OMSI-Stammverzeichnis, Überprüfung mit `/version`, saubere Entfernung. |
| [Schnellstart zur öffentlichen API](getting-started/api-quick-start.md) | Ein vollständiges .NET-Programm, das eine Sitzung plant, startet, ausliest und beendet. |
| [CLI-Referenz](reference/cli.md) | Jedes Flag, jedes Befehlswort und jede hierarchische Route von `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [CLI-Beispiele](reference/cli-examples.md) | Befehlszeilen zum Kopieren und Einfügen für häufige Aufgaben. |
| [LaunchSpec-Referenz](reference/launchspec.md) | Jede `LaunchSpec`-Eigenschaft und jeder Enum-Wert, JSON-Laderegeln für `/spec`. |
| [HOF-Verwaltung](reference/hof-management.md) | Sitzungsbezogenes Kopieren und Spiegeln von HOF-Dateien in Fahrzeugordner: Quellen, Ziele, Konflikte, Wiederherstellung. |
| [Runtime-Module](../../reference/runtime-modules.md) | Module, die mit einer Sitzung innerhalb von OMSI laufen: Manifest, Vertrauen, Lebenszyklus, Fehlerrichtlinien, Operationen. |
| [Referenz der Sitzungsprofile](reference/session-profiles.md) | `profile.yaml`-Schema `omsilaunch.session-profile/v1`, Schlüssel, Grenzwerte, Vorrang. |
| [Referenz der öffentlichen API](reference/public-api.md) | `IOmsiLaunch`, öffentliche Records und Enums, Stabilität pro Member. |
| [Inventar der öffentlichen API](reference/public-api-inventory.md) | Generierte Liste aller öffentlichen Typen und Member mit Signatur und Stabilität. |
| [Runtime-Steuerung](reference/runtime-control.md) | Runtime-Befehlskanal, Timeouts, Handles, Stopp-Semantik. |
| [Capabilities-Referenz](reference/capabilities.md) | Capability-Katalog und jede öffentliche Runtime-Operations-ID mit ihrer Klassifizierung. |
| [Sitzungslebenszyklus](concepts/session-lifecycle.md) | `SessionState`-Übergänge, was `StartSessionAsync` zusagt, wie eine Sitzung endet. |
| [Transaktionen und Recovery](concepts/transactions-and-recovery.md) | Journal-Zustände, Backups, Überprüfung der Wiederherstellung, Löschungen während der Sitzung, Recovery nach Absturz. |
| [Modell des permanenten Plugins](concepts/permanent-plugin.md) | Die `plugins\OmsiLaunch.*`-Plugin-Closure, manifestbasierte Integrität, was eine Sitzung niemals anrührt. |
| [Lokale Steuerung / IPC](reference/local-control.md) | Named-Pipe-Protokoll `0.1`, Endpunkt pro Installation, `session_id`-Bindung, Vertrauensmodell. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | Der Windows-Host (ohne Konsole): Unterschiede zu `OmsiLaunch.exe`, `/silent`, Dialoge, Exitcodes. |
| [Windows-Tray](reference/windows-tray.md) | Anzeige im Infobereich: Symbol, Menü, Statusfenster Feld für Feld, Sitzung beenden, Neustart des Explorers. |
| [Fehlerreferenz](reference/errors.md) | Jeder `OL_E_*`- / `OL_W_*`-Code mit Kategorie und Bedeutung. |
| [Exitcodes](reference/exit-codes.md) | `PublicExitCode`-Werte 0 bis 10 und Shim-Codes des Bootstrappers 100 bis 106. |
| [Paketierung / Installationslayout](reference/packaging.md) | Dateien im Release-ZIP, `release-manifest.json`, `.omsilaunch\`-Layout. |
| [Kompatibilität / Unterstützte OMSI-Builds](reference/compatibility.md) | Der eine unterstützte `Omsi.exe`-Hash, der akzeptierte Steam-LAA-Hash, Plattformanforderungen. |
| [Bekannte Einschränkungen](reference/known-limitations.md) | Was in dieser Beta nicht unterstützt, teilweise umgesetzt oder ein akzeptiertes Risiko ist. |
| [Status der Runtime-Validierung](status/runtime-validation-status.md) | Was unter OMSI lief, was nur offline lief, was noch eine echte Sitzung benötigt. |

Seiten, die für Maintainer normativ bleiben: die
[`README.md`](../../../README.md) des Repositorys, die Spezifikationen unter `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) und die Projektseiten unter
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Sie fassen zusammen; die
oben genannten Seiten sind die ausführliche Referenz. Historische Seiten sind im
[Dokumentationsmanifest](../../DOCUMENTATION-MANIFEST.md) aufgeführt.

<a id="how-this-documentation-is-kept-in-sync"></a>
## Wie diese Dokumentation synchron gehalten wird

Ein Dokumentations-Gate, `tests\OmsiLaunch.DocumentationTests`, wird gegen
`OmsiLaunch.Api` und `OmsiLaunch.Core` kompiliert und vergleicht die oben genannten Seiten mit dem
Code, der die öffentliche Oberfläche definiert:

| Gate | Prüft |
| --- | --- |
| `docs.cli-flags` | Jeder `CliInput.KnownFlags`-Eintrag erscheint in der CLI-Referenz als `` `/flag` `` oder `` `/flag:` ``; jeder `CliInput.AcceptedNoEffectFlags`-Eintrag ist in seiner Zeile als `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` gekennzeichnet; jedes Befehlswort und jede `CliInput.HierarchicalRoutes`-Route erscheint mit ihrer Runtime-Operation; jeder `PublicExitCode`-Wert hat eine `| n |`-Zeile in der Exitcode-Tabelle. |
| `docs.capabilities` | Jede `PublicCapabilityRegistry.All`-ID, jeder `PublicCapabilityRegistry.PublicRuntimeOperationIds`-Eintrag und jeder `PublicCapabilityClassification`-Name erscheint in der Capabilities-Referenz. |
| `docs.errors` | Jeder `PublicErrorCodes.All`-Code erscheint in der Fehlerreferenz, und kein `OL_E_*`- / `OL_W_*`-Literal in `src\` oder `tools\OmsiLaunch.Cli\` fehlt in `PublicErrorCodes`. |
| `docs.public-api` | Jeder exportierte Typ von `OmsiLaunch.Api`, jeder Enum-Wert und jeder `IOmsiLaunch`-Member erscheint in der Referenz der öffentlichen API, und alle fünf Stabilitätswörter werden verwendet. |
| `docs.launchspec` | Jede von `LaunchSpec` aus erreichbare öffentliche Eigenschaft und jeder Wert ihrer Enums erscheint in der LaunchSpec-Referenz. |
| `docs.session-profiles` | Jeder `SessionProfileCompiler.SchemaKeys`-Schlüssel, der Schemabezeichner und der Grenzwert von `256 KiB` erscheinen in der Referenz der Sitzungsprofile. |
| `docs.structure` | Jede Seite in der Navigationstabelle existiert. |
| `docs.links` | Jeder relative Link in `docs\**\*.md` (ausgenommen `docs\localized\`) und in den `*.md`-Dateien auf Stammebene wird zu einer Datei oder einem Verzeichnis aufgelöst. |
| `docs.localization` | Jedes in `docs\localized\LOCALIZATION-MANIFEST.md` aufgeführte Gebietsschema enthält jede Seite des lokalisierten Seitensatzes; jede Seite behält die Überschriften, Tabellen und Codeblöcke der englischen Seite, jeden Inline-Code-Abschnitt (Flags, Capability- und Operations-IDs, Fehlercodes, Schlüssel, Bezeichner) und jeden Link bei, und ihre relativen Links werden aufgelöst. |

Das Gate ist eine der Testsuiten, die von `tools\Invoke-OfflineValidation.ps1` ausgeführt werden
(überspringen Sie es mit `-SkipDocs`). Es läuft offline, startet niemals OMSI und lässt den
Build fehlschlagen, wenn ein Flag, eine Route, eine Capability, ein Fehlercode, ein Enum-Wert oder ein öffentlicher Typ
undokumentiert oder ein Link defekt ist. Es prüft keinen Fließtext, daher kann eine Seite
das Verhalten dennoch falsch beschreiben; melden Sie dies als Fehler gegen die Seite.

<a id="translations"></a>
## Übersetzungen

`docs\localized\<locale>\` enthält vollständige Übersetzungen dieser `0.1.0-beta4`-Dokumentation
für `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` und `ja-JP`. Der Seitensatz, die Stammverzeichnisse der Gebietsschemas und die
absichtlich nicht übersetzten Seiten sind in
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md) aufgeführt.
Übersetzungen behalten jeden Befehl, jedes Flag, jeden Bezeichner, jeden Fehlercode und jedes Beispiel
der englischen Seiten unverändert bei, und das `docs.localization`-Gate prüft dies.
Die englischen Seiten bleiben die normative Quelle: Wo eine Übersetzung von ihnen
abweicht, sind die englische Seite und der Code maßgeblich, und die Übersetzung
ist ein Fehler.
