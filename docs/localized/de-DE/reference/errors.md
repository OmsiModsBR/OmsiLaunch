# Fehler- und Diagnosecodes

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> Übersetzung der [englischen Originalseite](../../../reference/errors.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite ist die normative Referenz für jeden Code in `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 `OL_E_`-Fehlercodes und 4 `OL_W_`-Warnungen, gruppiert nach Katalogkategorie, dazu die informativen Diagnosecodes, die keine Fehler sind. Für jeden Code gibt sie an, wo der aktuelle Code ihn auslöst, was er bedeutet, wie er Sie erreicht (ausgelöste Exception, Ergebnisfeld, Diagnose, Antwort der Steuerungsebene (Control Plane) oder CLI-Envelope) und was zu tun ist. Die Bedeutungen sind den Auslösestellen entnommen; wo ein Code definiert ist, aber derzeit keinen Auslösepfad hat, wird dies auf der Seite angegeben.

Verwandte Seiten: [öffentliche API](public-api.md), [Exitcodes](exit-codes.md), [LaunchSpec](launchspec.md), [Sitzungslebenszyklus](../concepts/session-lifecycle.md), [Transaktionen und Recovery](../concepts/transactions-and-recovery.md), [lokale Steuerungsebene](local-control.md), [Runtime-Steuerung](runtime-control.md), [Sitzungsprofile](session-profiles.md), [permanentes Plugin](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Wie Codes Sie erreichen

| Oberfläche | Bedeutung |
| --- | --- |
| Ausgelöst | Eine Exception, deren `Message` mit dem Code beginnt (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). Die CLI extrahiert den Code aus der Meldung und ordnet ihn einem Exitcode zu (`CliProgram.Classify`). |
| Plandiagnose | Eine `LaunchDiagnostic` in `SessionPlan.Diagnostics`; jeder `OL_E_`-Code setzt `IsRunnable` auf false (CLI: Plan `NOT RUNNABLE`, Exitcode 1). |
| Sitzungsdiagnose | Eine `LaunchDiagnostic` in `SessionStatus.Diagnostics`; der Sitzungszustand ist `Failed` (CLI-Exitcode 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` und `OL_E_RESTORE_FAILED` umschließen in ihrer Meldung einen inneren Code. |
| Runtime-Ergebnis | `RuntimeCommandResult.ErrorCode` mit `Succeeded = false`. |
| Runtime-Detail | `RuntimeCommandResult.ErrorCode` ist der spezifische Code und `Values["detail"]` die Meldung (mit `Values["exception"]`, dem .NET-Typ). Beta 4: Ein typisierter Plugin-seitiger Code erreicht die API und die CLI unverändert; nur ein Fehler ohne katalogisierten Code wird zu `OL_E_RUNTIME_OPERATION_FAILED`. |
| Steuerungsantwort | `ErrorCode` einer Antwort der lokalen Steuerungsebene (`LocalControlResponse`). |
| CLI-Envelope | `error.code` im `--json`-Envelope oder `<code>: <message>` auf der Konsole; der Exitcode ist angegeben. |
| Telemetrie | Ein Runtime-Ereignisname, den der Host einer Sitzungsdiagnose zuordnet. |

## CLI

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Eine `OperationCanceledException` ist nach außen gelangt (Ctrl+C oder ein abgebrochenes Warten des Clients). | CLI-Envelope, Exitcode 7 | Wiederholen Sie den Befehl. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Eine Exception ohne `OL_E_`-Code ist nach außen gelangt: fehlerhaftes `/spec`-JSON, ein erforderliches Spec-Element mit null, ein unerwarteter Fehler. | CLI-Envelope, Exitcode 10 | Lesen Sie die Meldung und `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; korrigieren Sie die Eingabe; melden Sie den Fall, wenn er ungeklärt bleibt. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Eine `TimeoutException` ohne Code ist nach außen gelangt; oder der Client der lokalen Steuerung war mit einem Eigentümer verbunden, der nicht innerhalb des Timeouts geantwortet hat (der Eigentümer existiert, daher wird dies nicht als `OL_E_NO_ACTIVE_SESSION` gemeldet). (Mailbox-Timeouts tragen stattdessen `OL_E_RUNTIME_REQUEST_TIMEOUT`.) | CLI-Envelope, Steuerungsantwort; Exitcode 5 aus `Classify`, Exitcode 7 bei einer Steuerungsantwort | Wiederholen Sie den Vorgang; prüfen Sie, ob OMSI und der Eigentümer reagieren. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` liegt nicht neben `OmsiLaunch.exe`. | CLI-Envelope, Exitcode 7 | Installieren Sie das Paket neu. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` von `OmsiLaunchW.exe` hat keinen Prozess zurückgegeben. | CLI-Envelope, Exitcode 7 | Prüfen Sie die Paketdateien und Berechtigungen; führen Sie den Befehl ohne `/silent` aus, um den Fehler zu sehen. |

<a id="compatibility"></a>
## Kompatibilität

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` bei `plugin.build.invalid` | Die Start-Capabilities wurden aufgelöst, aber das aktive Start-Formular oder eine seiner Hook-Stellen ist nicht im erwarteten Zustand, wenn das Plugin den Headless-Start scharfschaltet (z. B. weil ein anderes Plugin es bereits gepatcht hat). | Sitzungsdiagnose (`Failed`) | Entfernen Sie das in Konflikt stehende Plugin; siehe [Kompatibilität](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` beim Start; Runtime-Operationen) | Eine native Capability, die die Anforderung benötigt, konnte auf diesem OMSI-Build nicht aufgelöst und verifiziert werden und ist daher nicht verfügbar. Beim Start schlägt die Sitzung vor jedem Hook fehl und die Dateien werden wiederhergestellt; eine Runtime-Operation wird abgelehnt, ohne den OMSI-Speicher zu berühren. `Data["capability"]` (Runtime) oder der Telemetriewert `capabilities` nennt sie. | Sitzungsdiagnose (`Failed`); Runtime-Ergebnis | Siehe [Kompatibilität](compatibility.md): Die Capability ist auf diesem Build nicht verfügbar. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` nicht verfügbar) | Die Installation ist keine OMSI-2-Installation: `Omsi.exe` fehlt, ist kein 32-Bit-x86-PE-Image, hat keine OMSI-2-Version (2.x), oder das Stammverzeichnis hat kein Verzeichnis `maps\` oder `Vehicles\`. Der Hash der ausführbaren Datei ist kein Kriterium. | Plandiagnose | Richten Sie OmsiLaunch auf ein OMSI-2-Installationsstammverzeichnis. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` nicht verfügbar); außerdem ausgelöst von `CurrentWindowsX64Platform.ValidateCurrent`, das der Dienst nicht aufruft | Nicht Windows 10 oder neuer, oder das Betriebssystem bzw. der Host-Prozess ist nicht x64. | Plandiagnose (CLI-Exitcode 3, wenn ausgelöst) | Führen Sie OmsiLaunch unter 64-Bit-Windows 10 oder neuer aus. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | nur `CurrentWindowsX64Platform.ValidateCurrent` | Die Architektur von Betriebssystem oder Host ist nicht x64. Der Dienst ruft `ValidateCurrent` nicht auf; derzeit kein Auslösepfad. | Nur von dieser Methode ausgelöst (`PlatformNotSupportedException`) | Siehe Quelltext `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

<a id="content"></a>
## Inhalte

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validierung: NEW_MAP ohne `EntrypointIdentity` und mit nicht gesetztem oder negativem `PresentedEntrypointIndex`. Planer: Die Karte hat keinen Einstiegspunkt mit der angeforderten Identität oder exakten (Groß-/Kleinschreibung wird beachtet) Bezeichnung. | Plandiagnose | Setzen Sie `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) oder eine Identität aus `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | Das Startwetter kann nicht angewendet werden: eine Voreinstellung, die keine Datei des Ordners `weather` der Installation ist, oder ein ICAO-Code, der nicht aus vier Buchstaben oder Ziffern besteht. | Plandiagnose | Verwenden Sie eine installierte Voreinstellung oder einen gültigen Stationscode. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | Der angeforderte Einstiegspunkt hat dieselbe Bezeichnung wie ein früherer Datensatz der Karte; OMSI zeigt nur den ersten Datensatz jeder Bezeichnung an, sodass er beim Start nicht ausgewählt werden kann. | Plandiagnose | Wählen Sie den ersten Datensatz mit dieser Bezeichnung oder einen anderen Einstiegspunkt. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` nicht verfügbar) | Die NEW_MAP-Karte wurde aufgelöst, aber es gibt weder einen angezeigten Index noch eine Identität. Tritt immer zusammen mit `OL_E_ENTRYPOINT_NOT_FOUND` auf. | Plandiagnose | Wie oben. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` ist keine installierte `Vehicles\...\*.hof`. | Plandiagnose | Verwenden Sie eine Identität aus `/list:hofs`. (Felder des Spielerfahrzeugs sind auf diesem Build ohnehin nicht ausführbar.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Eine Quelle der [HOF-Verwaltung](hof-management.md) (Collection, Verzeichnis, Fahrzeug oder Datei) existiert nicht, enthält keine `.hof` oder es fehlt eine in `Files` genannte Datei. | Plandiagnose; Startfehler | Korrigieren Sie die Quelle oder die Dateiliste. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Eine HOF-Zielfahrzeugdatei oder ein Zielordner ist nicht installiert. | Plandiagnose; Startfehler | Verwenden Sie eine Fahrzeugidentität aus `/list:vehicles` oder einen vorhandenen Ordner unterhalb von `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Ein HOF-Pfad verlässt die Installation, ein Ziel liegt nicht unterhalb von `Vehicles\`, ein Collection-Name ist kein einfacher Name oder eine Junction/ein symbolischer Link wird durchlaufen. | Plandiagnose; Startfehler | Verwenden Sie installationsrelative Pfade ohne `..` und ohne Links. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Zwei HOF-Operationen schreiben unterschiedliche Bytes in dieselbe Datei, oder eine Spiegelung entfernt eine Datei, die eine andere Operation schreibt. | Plandiagnose; Startfehler | Ordnen Sie in Konflikt stehende Operationen neu an, teilen Sie sie auf oder entfernen Sie sie. |
| `OL_E_HOF_INVALID` | `HofPlanner` | HOF-Operation ohne Ziele oder Quelle, eine Dateiquelle, die keine `.hof` ist, eine Zieldatei, die keine `.bus`/`.ovh` ist, oder eine Größen-/Anzahlgrenze (8 MiB pro Datei, 256 Dateien pro Operation, 256 MiB pro Sitzung). | Plandiagnose; Startfehler | Korrigieren Sie die Operation. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validierung: NEW_MAP mit nicht gesetzter `MapIdentity` oder einer, die nicht die Form `maps\<dir>\global.cfg` hat. Planer: Die Karte ist nicht installiert. | Plandiagnose | Verwenden Sie eine Identität aus `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Eine `FileNotFoundException`/`DirectoryNotFoundException` ohne Code ist nach außen gelangt, z. B. `/list:repaints` mit unbekanntem `/vehicle-scope` oder `/list:entrypoints` mit unbekanntem `/map`. | CLI-Envelope, Exitcode 6 | Korrigieren Sie die Identität. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` ist kein `.cti`-Eintrag des Modells (nur geprüft, wenn `Model` gesetzt ist). | Plandiagnose | Verwenden Sie eine Identität aus `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | Die in der ausgewählten `.osn` referenzierte Karte ist nicht installiert. | Plandiagnose | Installieren Sie die Karte oder wählen Sie eine andere Situation. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION ohne `SituationIdentity`, oder die `.osn` ist nicht installiert. | Plandiagnose | Verwenden Sie eine Identität aus `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` ist keine installierte `Vehicles\...\*.bus`. | Plandiagnose | Verwenden Sie eine Identität aus `/list:vehicles`. |

## Installation

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | Die Installations-Lease (`Local\OmsiLaunch.Installation.<hash>`) wird in dieser Anmeldesitzung von einem anderen Eigentümer gehalten, oder ein im Journal erfasster OMSI-Prozess (PID + Erstellungszeit + Pfad der ausführbaren Datei; oder eine beliebige `Omsi.exe` aus dem Stammverzeichnis bei einem Journal nach `HandoffCreated` ohne PID) läuft noch. | Start: Sitzungsdiagnose über `OL_E_START_SESSION`. Recovery: ausgelöst (`InvalidOperationException` / `IOException`). CLI-Exitcode 7. | Stoppen Sie den anderen Eigentümer (`session stop`) oder warten Sie, bis OMSI beendet ist, und wiederholen Sie den Vorgang oder führen Sie `/recover` aus. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` ist leer. | Plandiagnose | Übergeben Sie das Installationsverzeichnis. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` nicht verfügbar); außerdem `ValidateCurrent` | Das Stammverzeichnis existiert nicht, hat das Schreibschutzattribut oder kein `plugins\`-Verzeichnis. | Plandiagnose | Verweisen Sie auf eine echte, beschreibbare OMSI-Installation. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (Plan und Start) | Eine installierte `plugins\OmsiLaunch.*`-Datei weicht vom Hash in `release-manifest.json` ab (oder ohne Manifest von der Referenz-Closure). | Plandiagnose (Plan nicht ausführbar, CLI-Exitcode 1); Sitzungsdiagnose über `OL_E_START_SESSION` nur, wenn sich die Dateien zwischen Planung und Start ändern | Installieren Sie das OmsiLaunch-Paket neu, sodass `plugins\` und das Manifest übereinstimmen. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (Plan und Start) | Das Manifest hat keinen Eintrag für eine erforderliche Plugin-Datei. | Plandiagnose; Sitzungsdiagnose über `OL_E_START_SESSION` bei derselben Race Condition wie oben | Installieren Sie das Paket neu. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (Plan und Start) | Eine erforderliche `plugins\OmsiLaunch.*`-Datei fehlt in der OMSI-Installation, oder eine in `release-manifest.json` aufgeführte `plugins/`-Datei ist nicht installiert. | Plandiagnose; Sitzungsdiagnose über `OL_E_START_SESSION` bei derselben Race Condition wie oben | Installieren Sie die permanente Plugin-Gesamtheit (Closure) ([Installation](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | nur `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` ist false. Wird vom Dienst nicht aufgerufen; derzeit kein Auslösepfad. | Nur von dieser Methode ausgelöst | Siehe Quelltext. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` ist leer, ist kein JSON, hat kein `files`-Array oder enthält einen Eintrag ohne `path`/`sha256`, einen Hash, der nicht aus 64 Hexadezimalziffern besteht, einen Pfad, der absolut ist, `:` enthält, ein leeres, `.`- oder `..`-Segment hat, oder einen doppelt aufgeführten Pfad (Vergleich ohne Berücksichtigung der Groß-/Kleinschreibung, `/` und `\` gleichwertig). Ein UTF-8-BOM wird akzeptiert. | Plan: umschlossen von `OL_E_RUNTIME_ARTIFACT_MISSING`; Start: über `OL_E_START_SESSION` | Installieren Sie das Paket neu. |

## InvalidArgument

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validierung: `Date.Value`/`Time.Value` gesetzt, obwohl der Modus nicht `Explicit` ist. CLI: unbekanntes Flag, fehlender Wert, ungültige Ganzzahl oder ungültiger Bereich, `/saved` kombiniert mit `/map`/`/entrypoint`, unbekannte Befehlsroute, jede `ArgumentException`/`FormatException` ohne Code. | Plandiagnose; CLI-Envelope, Exitcode 2 | Korrigieren Sie das Argument. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (Start) | Ein semantischer Einstellungswert liegt außerhalb des Bereichs, ist nicht boolesch, nicht in der zulässigen Menge oder fehlerhaft (`graphics.particles` benötigt vier Felder). Werte werden zur Planungszeit nicht validiert. | Sitzungsdiagnose über `OL_E_START_SESSION` | Verwenden Sie einen Wert aus der [Einstellungstabelle](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | Der Schlüssel existiert, ist aber nicht beschreibbar (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Plandiagnose; CLI-Exitcode 2 | Entfernen Sie den Schlüssel. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | Der Schlüssel ist nicht in `ConfigurationCatalog` enthalten. | Plandiagnose; CLI-Exitcode 2 | Verwenden Sie einen Katalogschlüssel. |

## LaunchSpec

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | Die Wurzel ist kein JSON-Objekt, oder die Deserialisierung hat keinen Record erzeugt. | Ausgelöst (`InvalidDataException`), CLI-Exitcode 2 | Korrigieren Sie die Datei ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | Die `/spec`-Datei existiert nicht. | Ausgelöst (`FileNotFoundException`), CLI-Exitcode 6 | Prüfen Sie den Pfad. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | Die Datei ist größer als 1 MiB. | Ausgelöst (`InvalidDataException`), CLI-Exitcode 2 | Verkleinern Sie die Datei. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Ein Element, das an dieser Position keine öffentliche Eigenschaft des Records ist; Meldung `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Ausgelöst (`InvalidDataException`), CLI-Exitcode 2 | Entfernen oder benennen Sie das Element um. |

## LocalControl

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Handler des Eigentümers (`OwnerSession`) | Der Befehl ist nicht `session.status`, `session.events`, `session.stop` oder `runtime.execute` mit einem `operation`-Argument. | Steuerungsantwort; CLI-Exitcode 7 | Verwenden Sie einen unterstützten Befehl. |
| `OL_E_CONTROL_FAILED` | CLI-Client (`ReportForwarded`, `CliEventWatch`) | Der Eigentümer hat `Ok = false` ohne Fehlercode geantwortet. | CLI-Envelope, Exitcode 7 | Lesen Sie die Meldung; prüfen Sie Konsole/Diagnosedaten des Eigentümers. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | Der Handler des Eigentümers hat eine Exception ausgelöst, deren Meldung keinen `OL_E_`-Code trägt (z. B. war die Sitzung bereits geschlossen), oder die Antwort des Handlers konnte nicht serialisiert werden. | Steuerungsantwort | Lesen Sie `session status`; starten Sie den Eigentümer neu, falls er nicht mehr existiert. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (beide Seiten) | Längenpräfix negativ oder größer als 64 KiB (einschließlich eines übergroßen Anfrage-Frames), leerer Frame, JSON `null`, eine Anfrage ohne `Command` oder JSON, das nicht dekodiert werden konnte. | Steuerungsantwort / CLI-Envelope | Verwenden Sie das dokumentierte Protokoll ([lokale Steuerungsebene](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (Client) | Die eigene serialisierte Anfrage des Clients ist größer als 64 KiB. Wird dem Aufrufer gemeldet; es wird nichts gesendet. | Steuerungsantwort / CLI-Envelope | Verkleinern Sie die Anfrage. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (Eigentümer) | Die Antwort des Eigentümers passt nicht in den Frame von 64 KiB. Der Eigentümer antwortet mit diesem typisierten Fehler, statt die Antwort zu verwerfen. `session.status` und `session.events` erreichen ihn nie: Ihr Ereignisverlauf wird, beginnend mit den ältesten Einträgen, passend gekürzt. | Steuerungsantwort / CLI-Envelope | Wiederholen Sie den Vorgang; lesen Sie Ereignisse häufiger. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | Die `ProtocolVersion` der Anfrage ist nicht `0.1`; die Antwort des Eigentümers konnte nicht dekodiert werden oder war leer; der Eigentümer hat die Verbindung ohne Antwort geschlossen oder die Verbindung ist nach ihrem Aufbau abgebrochen; der Eigentümer hat keine `SessionId` gemeldet. | Steuerungsantwort / CLI-Envelope | Gleichen Sie die Versionen von Client und Eigentümer an; lesen Sie `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Handler des Eigentümers | `session.stop` oder `runtime.execute` ohne eine `session_id`, die der aktiven Sitzung entspricht. | Steuerungsantwort; CLI-Exitcode 7 | Lesen Sie zuerst `session.status` und binden Sie die Anfrage (die CLI tut dies automatisch). |

<a id="other"></a>
## Sonstige

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | Der übergebene Plan hat `IsRunnable = false`, oder die erneute Planung beim Start ist nicht ausführbar (`Omsi.exe` geändert, Inhalte entfernt, Plugin-Closure fehlt); die Meldung listet die aktuellen `OL_E_`-Codes auf. | Ausgelöst (`InvalidOperationException`); CLI-Exitcode 1 | Planen Sie erneut und beheben Sie die aufgeführten Diagnosen. |

<a id="presentation"></a>
## Darstellung

Alle werden von `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`) ausgelöst. Zur Planungszeit sind sie in `OL_E_SESSION_PRESENTATION_INVALID` eingeschlossen (die Meldung trägt den Code); beim Start werden sie über `OL_E_START_SESSION` gemeldet.

| Code | Bedeutung / typische Ursache | Vorgehen |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | Die `.itx`-Datei ist leer, hat eine ungerade Anzahl nicht leerer Zeilen, oder eine URL-Zeile ist keine absolute `http`/`https`-URL. | Verwenden Sie Paare aus URL- und Zielzeile. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (aufgelöst relativ zum Arbeitsverzeichnis des Prozesses) existiert nicht. Ausgelöst als `FileNotFoundException`. | Übergeben Sie einen existierenden `.itx`-Pfad. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` ist `Override` ohne `OverrideProfilePath`. CLI-Exitcode 2, wenn ausgelöst. | Geben Sie `/internet-textures-profile:<file.itx>` an. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Eine Zielzeile ist absolut, enthält `..`, beginnt mit `\`, wird außerhalb der Installation aufgelöst, enthält keine `Texture\`-Komponente oder durchläuft eine Junction/einen Symlink. | Verwenden Sie relative Ziele der Form `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` existiert nicht. | Korrigieren Sie das Verzeichnis. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` oder `<LANG>.bmp` fehlt im Asset-Verzeichnis, oder ein mitgeliefertes `assets\splash\<LANG>.bmp` fehlt beim Befüllen von `.omsilaunch\assets\splash`. | Stellen Sie die BMP-Dateien bereit bzw. installieren Sie das Paket neu. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Ein Startbild-BMP ist keine 640×480-24-Bit-`BM`-Bitmap. | Konvertieren Sie das Bild. |

<a id="process"></a>
## Prozess

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (Pfade für Startfehler und Supervisor-Fehler) | Das Beenden von OMSI oder das Warten darauf während der Fehlerbereinigung hat eine Exception ausgelöst; die innere Meldung folgt. | Sitzungsdiagnose (einer `Failed`-Sitzung hinzugefügt) | Stellen Sie sicher, dass keine `Omsi.exe` mehr läuft, und führen Sie dann `/recover` aus, falls ein Journal aussteht. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` ist direkt nach `CreateProcessW` fehlgeschlagen (`Win32=<code>`); der Prozess wird beendet. | Sitzungsdiagnose über `OL_E_START_SESSION` | Wiederholen Sie den Vorgang; prüfen Sie Virenschutz/Berechtigungen. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI wurde vor `gameplay.entered` beendet (Absturz, ein OMSI-Fehlerdialog wurde geschlossen, das Fenster wurde geschlossen). | Sitzungsdiagnose (`Failed`); Wiederherstellung wird ausgeführt | Prüfen Sie die eigenen Logs von OMSI und `logfile.txt`; suchen Sie in `RuntimeEvents` nach dem letzten Plugin-Ereignis. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` ist fehlgeschlagen (`Win32=<code>` in der Meldung). | Sitzungsdiagnose über `OL_E_START_SESSION` | Beheben Sie den Win32-Fehler (fehlende Datei, Zugriff verweigert, Richtlinie). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | Die Supervisor-Schleife hat eine Exception ausgelöst (Telemetrie lesen, auf Prozess warten/Prozess beenden, Journal schreiben); OMSI wird beendet und die Wiederherstellung versucht. | Sitzungsdiagnose (`Failed`) | Lesen Sie die innere Meldung und das Host-Log. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` ist fehlgeschlagen (`Win32=<code>`). | In Meldungen von `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Beenden Sie OMSI manuell und führen Sie dann `/recover` aus. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` auf das Prozess-Handle ist fehlgeschlagen. | In Meldungen von `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Wie oben. |

## Runtime

„Runtime-Detail“ bedeutet den spezifischen Code in `ErrorCode`, mit der Meldung in `Values["detail"]` und dem Ausnahmetyp in `Values["exception"]`.

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` mit `preset`, während `family` 2 (außen) oder 3 (Karte) ist; Voreinstellungen gibt es nur für Fahrer (0) und Fahrgast (1). | Runtime-Detail | Lassen Sie `preset` weg oder verwenden Sie Family 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | `Date`/`Time`-Modus `Explicit` ohne Wert oder mit Komponenten außerhalb des Bereichs. Der Name ist historisch bedingt; es handelt sich um einen Validierungsfehler zur Planungszeit. | Plandiagnose | Korrigieren Sie den Wert (und beachten Sie, dass explizites Datum/explizite Uhrzeit auf diesem Build nicht ausführbar ist). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: Der `.bus`-Pfad existiert nicht unter dem OMSI-Arbeitsverzeichnis (vor dem nativen Aufruf geprüft, damit OMSI keinen Ersatz verwenden kann). | Runtime-Detail | Verwenden Sie eine Identität aus `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | wie oben | Natives MakeVehicle hat die Straßenfahrzeug-Sammlung um mehr als ein Objekt verändert. | Runtime-Detail (`native_status`, Anzahlen in der Meldung) | Melden Sie den Fall; die erzeugten Objekte bleiben bis zum Ende der Sitzung bestehen. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | wie oben | Die Sammlung hat sich nicht verändert; OMSI hat das Fahrzeug stillschweigend abgelehnt. | Runtime-Detail | Prüfen Sie die `.bus`-Datei; versuchen Sie ein anderes Modell. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | wie oben | Jeder andere native Status ungleich null. | Runtime-Detail | Melden Sie den Fall mit den Anzahlen aus der Meldung. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | Der profilierte PlaceRandomBus-Aufruf hat einen Fehlerstatus zurückgegeben. | Runtime-Detail | Wiederholen Sie den Vorgang, sobald das Spielgeschehen stabil ist; melden Sie den Fall. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; Plugin-seitige Prüfungen (`time.set` ohne `hour`/`minute`/`second`; `camera.set` ohne `family`/`field_of_view`; `camera.lock` ohne auswertbares `family`; Fahrzeug-/Kurvenoperationen) | Ein erforderliches Argument fehlt oder ist leer. | Runtime-Ergebnis (Registry; CLI-Exitcode 2) oder Runtime-Detail (Plugin) | Geben Sie das Argument an ([Runtime-Steuerung](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | Das Referenzverzeichnis/die Referenzdatei der Plugin-Closure oder die in `OmsiLaunchRuntimePaths` angegebene native Bridge kann nicht geladen werden (kann `OL_E_RELEASE_MANIFEST_INVALID` umschließen). | Plandiagnose | Führen Sie OmsiLaunch aus einem intakten Paket aus. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, INTERNAL-Harness) | Das Baseline-`time.read`/`camera.read` ist fehlgeschlagen, daher wurde der Schreibtest übersprungen. | Nur Batch-Artefakt | Nicht für Benutzer bestimmt. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` ist leer, länger als 240 Zeichen, enthält NUL oder `..`, beginnt nicht mit `Vehicles\` oder endet nicht auf `.bus`. | Runtime-Detail | Übergeben Sie `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | keine (aus Kompatibilitätsgründen beibehalten) | Wird nicht mehr ausgegeben. Frühere Builds lösten ihn aus, wenn eine abgebrochene Anfrage im Slot verblieb; jeder abschließende Pfad einer Anfrage setzt den Slot jetzt zurück, und eine zu Beginn einer neuen Anfrage vorgefundene verbliebene Anfrage oder Antwort wird gelöscht. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | Die Mailbox wurde verworfen, weil die Sitzung endet. | Ausgelöst (`InvalidOperationException`) | Keines; die Sitzung ist vorbei. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | Der Mailbox-Slot enthielt einen Zustandswert, der weder idle noch requested noch responded ist (Beschädigung). Der Slot wird zurückgesetzt und der Fehler ausgelöst; die nächste Anfrage funktioniert normal. | Ausgelöst (`InvalidDataException`) | Wiederholen Sie den Vorgang; melden Sie den Fall, wenn er fortbesteht. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | Der Zeiger auf den Konstantenblock des Fahrzeugs ist null. | Runtime-Detail | Das Fahrzeug hat keine Konstanten; nichts zu tun. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` ist nicht in der Konstantentabelle des Fahrzeugs enthalten. | Runtime-Detail | Listen Sie zuerst die Konstanten auf. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | Das durch den Spawn erzeugte Objekt hat eine VMT außerhalb des Adressbereichs des OMSI-Images. | Runtime-Detail | Melden Sie den Fall. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | wie oben | Das erzeugte Objekt ist nicht in der Straßenfahrzeug-Sammlung enthalten. | Runtime-Detail | Melden Sie den Fall. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Zwei aufeinanderfolgende Kurvenpunkte haben denselben X-Wert. | Runtime-Detail | Inhaltsproblem in der Kurve des Fahrzeugs. |
| `OL_E_RUNTIME_CURVE_EMPTY` | wie oben | Die Kurve hat keine Punkte. | Runtime-Detail | Wie oben. |
| `OL_E_RUNTIME_CURVE_INVALID` | wie oben | Kein Segment der Kurve enthält `x`. | Runtime-Detail | Werten Sie innerhalb des Definitionsbereichs der Kurve aus. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | wie oben | `name` ist unbekannt oder sein Funktionszeiger ist null. | Runtime-Detail | Listen Sie zuerst die Kurven auf. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | Der Zeiger auf die Fahrzeugdefinition ist null. | Runtime-Detail | Das Handle verweist auf ein Fahrzeug ohne Definitionsdaten. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (Eigentümermodus) | `plugins\OmsiLaunch.Plugin.opl` oder `plugins\OmsiLaunch.Native.x86.dll` fehlt neben der ausführbaren Datei. | CLI-Envelope, Exitcode 7 | Installieren Sie das Paket neu. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` fehlt oder ist leer bei `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (die Registry weist diese normalerweise schon vorher mit `OL_E_RUNTIME_ARGUMENT_REQUIRED` ab). | Runtime-Detail | Geben Sie das Handle an. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | Das Handle ist unbekannt, das Objekt hat die Sammlung verlassen, die Adressgeneration wurde weitergezählt, oder der Objekt-Fingerprint (VMT + Identität von Definition/Modell) hat sich geändert, weil die Adresse wiederverwendet wurde. Verbleibender blinder Fleck: Dieselbe Klasse und dasselbe Modell werden zwischen zwei Listenabfragen an derselben Adresse neu erzeugt. | Runtime-Detail | Führen Sie `road-vehicles.list`/`humans.list` erneut aus und verwenden Sie das neue Handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; `D3DRuntimeApi`-Fallback | Allgemeiner Wrapper für Plugin-seitige Fehler; `Values["detail"]` enthält die Meldung (oft einen spezifischeren Code) und `Values["exception"]` den Exception-Typ. Eine Exception, die einer Operation innerhalb des Mailbox-Dispatchers entkommt, wird ebenfalls mit diesem Code beantwortet (ohne Werte), statt die Anfrage unbeantwortet zu lassen. | Runtime-Ergebnis | Lesen Sie `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | Das Plugin hat keine Implementierung für eine Operation, die die Registry zugelassen hat (Versionsabweichung zwischen Registry und Plugin). | Runtime-Detail | Installieren Sie ein konsistentes Paket neu. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | Die Operation ist nicht in `PublicRuntimeOperationIds` enthalten, einschließlich jeder `internal.*`-Operation. Wird vor der Sitzungssuche geprüft. | Runtime-Ergebnis; Steuerungsantwort; CLI-Exitcode 2 | Verwenden Sie eine öffentliche Operations-ID. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` mit `preset`, während kein Spielerfahrzeug existiert (Headless-Sitzungen haben keines). Wird außerdem als `code` des Ereignisses `camera.lock.degraded` gemeldet, wenn das erneute Anwenden fehlschlägt. | Runtime-Detail / Runtime-Ereignis | Sperren Sie ohne Voreinstellung, oder verwenden Sie eine Sitzung mit Spielerfahrzeug. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Ein erfolgreiches D3D-Ergebnis hatte keine Werte oder eine unbekannte Gerätestatus-Zeichenfolge. | Ausgelöst (`OmsiRuntimeException`) | Gleichen Sie die Versionen von Host und Plugin an. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | Der Slot enthält eine veraltete Antwort mit derselben Anfrage-ID wie die neue Anfrage. Die veraltete Antwort wird gelöscht, bevor der Fehler ausgelöst wird. | Ausgelöst (`InvalidOperationException`) | Verwenden Sie streng aufsteigende Anfrage-IDs. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Keine Antwort innerhalb von `timeout`; der Slot wird zurückgesetzt und eine verspätete Antwort verworfen. | Ausgelöst (`TimeoutException`); CLI-Exitcode 5 | Wiederholen Sie den Vorgang mit einem längeren Timeout; prüfen Sie, ob OMSI blockiert ist (modaler Dialog, Ladevorgang). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | Das Antwort-Envelope ist beschädigt, hat eine ungültige Länge (negativ, null oder größer als der Slot), eine fremde Sitzungs-ID oder eine andere Anfrage-ID. Der Slot wird zurückgesetzt, bevor der Fehler ausgelöst wird, sodass die nächste Anfrage normal funktioniert. | Ausgelöst (`InvalidDataException`) | Wiederholen Sie den Vorgang; melden Sie den Fall, wenn er fortbesteht. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (Plugin) | Der `cursor` einer Listenoperation ist fehlerhaft oder gehört zu einer anderen Art von Liste. | Runtime-Detail | Übergeben Sie den `next_cursor` der vorherigen Seite derselben Operation oder lassen Sie ihn weg. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | Das serialisierte Ergebnis ist größer als die Mailbox von 64 KiB. Seitenweise Listenergebnisse werden stattdessen passend gekürzt und mit `next_cursor` fortgesetzt; kein Listenergebnis erreicht diesen Code. | Runtime-Ergebnis | Verwenden Sie eine engere Operation oder ein kleineres `limit`. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | Der Zeiger auf die Skriptdefinition oder den Skriptzustand des Fahrzeugs ist null. | Runtime-Detail | Das Fahrzeug hat keine Skriptobjekte. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; Plugin-Mailbox | `RuntimeCommand.SessionId` weicht von der Sitzungs-ID des Handles ab (vom Host ausgelöst), oder eine Anfrage hat ein Plugin erreicht, das an eine andere Sitzung gebunden ist (vom Plugin als typisiertes Ergebnis zurückgegeben). | Ausgelöst (`InvalidOperationException`) / Runtime-Ergebnis | Erstellen Sie den Befehl mit `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` des Ereignisses `world.failed`) | Das Startwetter konnte nach dem Start der Welt nicht angewendet werden: OMSI hat für `/weather-real` keine ICAO-Station konfiguriert, oder seine Wetterobjekte sind nicht verfügbar. Die Sitzung schlägt fehl. | Sitzungsfehler | Konfigurieren Sie eine Station in OMSI oder verwenden Sie `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Ein Schlüssel von `weather.set` benennt einen Wert, den OMSI bei jedem Wetter-Tick ableitet (Luftfeuchtigkeit, Lichtfaktoren, Wolkentransparenz, Niederschlags-Flag, nasser Boden); es wird nichts geschrieben. | Runtime-Ergebnis | Setzen Sie den in `detail` genannten Quellschlüssel (z. B. `dew_point` statt `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Unbekannter Schlüssel von `weather.set`; es wird nichts geschrieben. | Runtime-Ergebnis | Verwenden Sie einen Schlüssel aus [Capabilities](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` ist nicht in der Tabelle der String-Variablen enthalten. | Runtime-Detail | Listen Sie zuerst die String-Variablen auf. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Ein boolescher Wetterwert ist nicht `true`/`false`/`1`/`0`. Derzeit nicht erreichbar (siehe oben). | Runtime-Detail (definiert) | Siehe Quelltext. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` keine Ganzzahl, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` darf -1 sein), `scheduled` 0..1; `vehicle.variable.set`: `value` nicht endlich; `vehicle.curve.evaluate`: `x` nicht endlich. | Runtime-Detail | Verwenden Sie einen Wert im zulässigen Bereich. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` ist nicht in der Tabelle der numerischen Variablen enthalten. | Runtime-Detail | Listen Sie zuerst die Variablen auf. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | Der Variablen-Slot oder die Wertadresse ist null. | Runtime-Detail | Die Variable ist für dieses Fahrzeug nicht materialisiert. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Die Uhrzeitwerte wurden geschrieben, aber der profilierte native SetTime-Aufruf hat einen Fehler zurückgegeben. | Runtime-Detail | Wiederholen Sie den Vorgang; lesen Sie mit `time.read` zurück. |

## RuntimeD3D

Alle werden von `CurrentRuntimeControl` ausgelöst (`ThrowD3D`-Zuordnung des nativen Status; `detail` enthält die Operation und das HRESULT, `native_status` den numerischen Status). Oberfläche: Runtime-Ergebnis mit dem Code in `ErrorCode`; `D3DRuntimeApi` löst ihn erneut als `OmsiRuntimeException` aus.

| Code | Nativer Status / Ursache | Vorgehen |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: Das Direct3D-Device ist verloren. | Warten Sie auf `d3d.restored`; erstellen Sie Texturen neu (die Generation hat sich geändert). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: fehlendes oder ungültiges `width`, `height`, `level`, `x`, `y`, `format` oder `handle` (Bereiche: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Korrigieren Sie die Argumente. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` ist kein gültiges Base64 oder größer als 48 KiB. | Senden Sie kleinere Rechtecke. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, oder ein unbekannter `format`-Name (gültig: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Verwenden Sie ein aufgeführtes Format. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Jeder andere Status; das HRESULT steht in `detail`. | Melden Sie den Fall mit dem HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: Das Device ist nicht bereit (vor dem ersten Frame oder während des Beendens). | Wiederholen Sie den Vorgang nach `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: Ein Geräte-Reset läuft. | Wiederholen Sie den Vorgang nach `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: Das Textur-Handle wurde bereits freigegeben. | Verwenden Sie freigegebene Handles nicht erneut. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: Das Handle gehört zu einer früheren Device-Generation; oder die Handle-Zeichenfolge ist nicht `d3dtex-<session>-<hex>` bzw. ist null. | Erstellen Sie die Textur neu. |

<a id="session"></a>
## Sitzung

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` bei `plugin.request.unsupported` | Plan: `LastMapState`, `EntrypointIdentity`, Datums-/Uhrzeit-/Jahresmodus, Wettermodus, Felder des Spielerfahrzeugs oder Eingabedokumente angefordert (`Requested capability unavailable: <name>`). Telemetrie: Das Plugin hat die Übergabe (Handoff) abgelehnt (kann bei einem ausführbaren Plan nicht auftreten). | Plandiagnose; Sitzungsdiagnose | Entfernen Sie die nicht unterstützte Anforderung ([bekannte Einschränkungen](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` bei `headless.arm.failed` | Das Plugin konnte den einmaligen Headless-Start-Hook in OMSI nicht scharf schalten. | Sitzungsdiagnose (`Failed`) | Überprüfen Sie den Build; melden Sie den Fall. |
| `OL_E_NO_ACTIVE_SESSION` | CLI-Client (`ReportForwarded`, `CliEventWatch`) | Kein Eigentümer antwortet auf der Steuerungs-Pipe der Installation (keine Sitzung, oder der Eigentümer startet bzw. validiert noch). | CLI-Envelope, Exitcode 4 | Starten Sie eine Sitzung, oder warten Sie, bis sie `Running` ist. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` ist vor `plugin.started` abgelaufen (OMSI hat `plugins\OmsiLaunch.Plugin.opl` nicht geladen oder hängt vor der Plugin-Initialisierung). | Sitzungsdiagnose (`Failed`) | Prüfen Sie die Plugin-Closure, `plugins\OmsiLaunch.Plugin.opl` und die `logfile.txt` von OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` bei `plugin.handoff.invalid` oder nicht auswertbarem Telemetrie-JSON | Das Plugin konnte die Start-Übergabe nicht lesen/verifizieren (Versionen 3 bis 6, SHA-256) oder hat ungültige Telemetrie gesendet. | Sitzungsdiagnose (`Failed`) | Gleichen Sie die Versionen von Host und Plugin an (installieren Sie das Paket neu). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Für diese Installation antwortet bereits ein Eigentümer auf `session.status`. | CLI-Envelope, Exitcode 7 | Verwenden Sie Client-Befehle (`session status`, `session stop`, Runtime-Befehle). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | Der Sitzungszustand ist nicht `Running`. | Ausgelöst (`InvalidOperationException`) | Rufen Sie zuerst `WaitForAsync(session, SessionState.Running, ...)` auf. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Der Startbild-/ITX-Plan konnte nicht erstellt werden; die Meldung trägt den Darstellungscode. | Plandiagnose | Siehe [Darstellung](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (OmsiLaunchW-Dialog) | Ersatzcode, der angezeigt wird, wenn ein Startplan nicht ausführbar ist oder die Sitzung das Spielgeschehen nicht erreicht hat und keine `OL_E_`-Diagnose vorliegt. | Nur Meldungsfenster | Lesen Sie `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` bei `world.situation.failed` | Der native Start der gespeicherten Situation hat einen Fehler zurückgegeben (`native_status` im Ereignis). | Sitzungsdiagnose (`Failed`) | Prüfen Sie die `.osn` und ihre Karte. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | Das Plugin wurde gestartet, aber `Running` wurde nicht innerhalb von `StartupTimeoutSeconds` erreicht. | Sitzungsdiagnose (`Failed`) | Erhöhen Sie `/startup-timeout` für große Karten; suchen Sie in `RuntimeEvents` nach dem letzten Weltereignis. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Jede Exception im Startpfad; die Meldung ist die innere Meldung (beginnt meist mit dem inneren Code). | Sitzungsdiagnose (`Failed`) | Handeln Sie entsprechend dem inneren Code. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` bei `world.failed` | Der native NEW_MAP-Start hat einen Fehler zurückgegeben (`native_status` im Ereignis). | Sitzungsdiagnose (`Failed`) | Prüfen Sie die Karte, den Einstiegspunkt-Index und die Logs von OMSI. |

## SessionProfile

Alle werden von `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) oder `CliInput` ausgelöst, als `SessionProfileException` (eine `IOException` mit `Code`), CLI-Exitcode 2. Siehe [Sitzungsprofile](session-profiles.md).

| Code | Bedeutung / typische Ursache | Vorgehen |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | Das Startbild-`assets`-Verzeichnis der Voreinstellung oder die Internettexturen-`profile`-Datei existiert nicht innerhalb des Pakets. | Fügen Sie das Asset hinzu. |
| `OL_E_SESSION_PROFILE_INVALID` | Struktur- oder Grenzverletzung: mehr als 256 KiB, nicht genau ein Wurzel-Mapping, YAML-Anker, unbekannter Schlüssel, fehlender erforderlicher Schlüssel, kein Skalar, wo ein Skalar erforderlich ist, `id` weicht vom Verzeichnisnamen ab, Voreinstellungen nicht 1..99 oder doppelter `index`, nicht positive Timeouts, nicht unterstützter Wetter-/Startbild-/Internettexturen-Modus, Datum/Uhrzeit nicht `explicit`, ungültiges YAML, Fehler beim Parsen von Zahlen/Datumswerten. | Korrigieren Sie das YAML gemäß der Meldung. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` ist nicht leer und enthält weder die ausgewählte Karte (NEW_MAP) noch die Karte der ausgewählten Situation (SAVED_SITUATION). | Wählen Sie eine kompatible Welt. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` existiert nicht. | Prüfen Sie die ID. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Ein explizites CLI-Argument zielt auf ein Feld, das dem ausgewählten Profil bzw. der ausgewählten Voreinstellung gehört. | Lassen Sie das Flag weg oder wählen Sie eine andere Voreinstellung. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | Die ID enthält `\`, `/`, `:` oder `..`; oder ein Asset-Pfad ist absolut, verlässt das Paket oder durchläuft eine Junction/einen Symlink. | Halten Sie Pfade innerhalb des Pakets. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` fehlt, liegt außerhalb von 1..99 oder ist im Profil nicht deklariert. | Verwenden Sie einen deklarierten Voreinstellungsindex. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` ist nicht `omsilaunch.session-profile/v1`. | Verwenden Sie das unterstützte Schema. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Ein Einstellungsschlüssel einer Voreinstellung ist bekannt, aber nicht beschreibbar. | Entfernen Sie den Schlüssel. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Ein Einstellungsschlüssel einer Voreinstellung ist nicht im Katalog enthalten. | Verwenden Sie einen Katalogschlüssel. |

<a id="transaction"></a>
## Transaktion

Siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md).

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Eine veraltete `closecheck` existiert nach `File.Delete` noch. | Sitzungsdiagnose über `OL_E_START_SESSION` | Entfernen Sie `<root>\closecheck` manuell (Berechtigungen). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Ein Pfad, der vor der Sitzung nicht existierte, enthält jetzt Inhalt, der von dem abweicht, was die Sitzung angewendet hat; er wird nicht entfernt und das Journal wird beibehalten. | Ausgelöst (`IOException`); innerhalb von `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; CLI-Exitcode 8 | Prüfen Sie die Datei; entfernen oder verschieben Sie sie und führen Sie dann `/recover` aus. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (Journal ohne Fingerprint) | Das Journal hat keinen Fingerprint des angewendeten Inhalts für einen ursprünglich nicht vorhandenen Pfad, der keine Löschung ist, sodass die Eigentümerschaft nicht nachgewiesen werden kann. Beim Sitzungsstart wird die Recovery (Absturzwiederherstellung) zurückgestellt und mit den geplanten Bytes dieser Sitzung erneut versucht; über `RecoverPendingAsync` wird der Fehler ausgelöst. | Ausgelöst (`IOException`); CLI-Exitcode 8 | Starten Sie eine Sitzung mit derselben Spec (liefert die Bytes), oder prüfen und entfernen Sie die Datei und führen Sie dann `/recover` aus. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | Der SHA-256 eines Backups stimmt nicht mit dem im Journal aufgezeichneten Snapshot überein; es wird nichts geschrieben. | Ausgelöst; innerhalb von `OL_E_RESTORE_FAILED`; CLI-Exitcode 8 | Stellen Sie die Datei aus Ihrem eigenen Backup wieder her; löschen Sie das Journal erst dann, wenn Sie sicher sind. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Snapshots existieren im Speicher, aber `journal.json` ist verschwunden (während der Sitzung gelöscht). | Ausgelöst; innerhalb von `OL_E_RESTORE_FAILED` | Überprüfen Sie die Sitzungsdateien manuell. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` existiert nach dem Löschen noch (die Wiederherstellung selbst war erfolgreich und wurde verifiziert). | Ausgelöst; innerhalb von `OL_E_RESTORE_FAILED`; CLI-Exitcode 8 | Löschen Sie `<root>\.omsilaunch\journal.json` (Berechtigungen) oder führen Sie `/recover` erneut aus (idempotent). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (Startfehler / Supervisor) | Das Beenden von OMSI konnte nicht bestätigt werden, daher wurden Dateien nicht ersetzt, solange OMSI sie möglicherweise noch verwendet; das Journal wird beibehalten. | Sitzungsdiagnose (`Failed`) | Führen Sie `/recover` aus, nachdem `Omsi.exe` beendet wurde (oder der nächste Start führt die Recovery automatisch durch). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (Startfehler / Supervisor) | `RestoreAsync` hat eine Exception ausgelöst; die Meldung trägt den inneren Code; das Journal wird beibehalten. | Sitzungsdiagnose (`Failed`); CLI-Exitcode 8, wenn von `/recover` ausgelöst | Handeln Sie entsprechend dem inneren Code und führen Sie dann `/recover` aus. |

<a id="warning"></a>
## Warnung

| Code | Ausgelöst von | Bedeutung | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | Die OMSI-2-Programmdatei hat kein bekanntes Layout (ihre Code-Identität steht nicht im Build-Katalog). Der Plan bleibt ausführbar: Jede native Capability wird im Prozess aufgelöst und verifiziert, und eine, die nicht verifiziert werden kann, ist nicht verfügbar. | Plandiagnose (Ausführbarkeit nicht betroffen) | Nichts erforderlich; prüfen Sie `/plan` und `profiles` auf die vorhergesagten Capabilities. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (Supervisor) | `Behavior.CooperativeShutdown` wurde angefordert, aber OMSI konnte nicht zum Beenden aufgefordert werden (seine Beenden-Routine ist auf diesem Build nicht aufgelöst, oder OMSI schließt bereits), oder es wurde nicht innerhalb von `ShutdownTimeoutSeconds` beendet: OMSI wurde stattdessen terminiert. Die Wiederherstellung läuft wie üblich. | Sitzungsdiagnose | Nichts: Die Sitzung endet wie bei einer erzwungenen Beendigung. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Eine Capability, die der Start benötigt, konnte aus der ausführbaren Datei nicht aufgelöst werden (z. B. eine DRM-geschützte ausführbare Datei, deren geladenes Image von der Datei abweicht). Sie wird beim Start im Prozess aufgelöst. | Plandiagnose (Ausführbarkeit nicht betroffen) | Nichts erforderlich; ein Start schlägt mit `OL_E_CAPABILITY_UNRESOLVED` fehl, wenn sie auch im Prozess nicht aufgelöst werden kann. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Ein Modul, das der Sitzung von selbst beigetreten ist (Einstellungsdatei, Inhalts- oder Standardaktivierung), kann nicht geladen werden (`Data["code"]` nennt den Grund, `Data["activation"]` wie es beigetreten ist). | Plandiagnose (Ausführbarkeit nicht betroffen) | Korrigieren, deaktivieren oder entfernen Sie das Modul. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Ein Eintrag von `.omsilaunch\modules.ini` kann nicht verwendet werden (kein Modulabschnitt, außerhalb eines Abschnitts, unbekannter Schlüssel, ungültiger Wert; der Standardwert wird verwendet), oder es wurden Einstellungen für ein Modul angefordert, das die Sitzung nicht lädt. | Plandiagnose (Ausführbarkeit nicht betroffen) | Korrigieren Sie den Eintrag oder die Anforderung. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Ein Sitzungs-Löschpfad (ITX-Ziel, `Texture\standard.ipr`, `closecheck`) existierte vor der Sitzung nicht und existiert jetzt, aber unter diesem Journal wurde nie ein OMSI-Prozess gestartet, sodass die Datei kein Nebenprodukt der Sitzung sein kann. Sie wird beibehalten und gemeldet (Meldung = relativer Pfad, `Data["sha256"]`); die Transaktion wird dennoch abgeschlossen. | Sitzungsdiagnose / `RecoveryStatus.Diagnostics` (Zustand nicht betroffen) | Prüfen Sie die Datei; entfernen Sie sie selbst, falls sie unerwünscht ist. |

## Module

Runtime-Module (siehe [Runtime-Module](../../../reference/runtime-modules.md)). Bei der Planung ist ein Code eine Plandiagnose: Bei einem angeforderten Modul macht er den Plan nicht ausführbar; bei einem Modul, das von selbst beigetreten ist (Einstellungsdatei, Inhalt, Standard), meldet der Plan stattdessen `OL_W_MODULE_NOT_LOADED`. In einer Sitzung ist ein Code eine Sitzungsdiagnose (die Sitzung läuft weiter, sofern der Code nicht `OL_E_MODULE_SESSION_FAILED` ist), und bei einer Modul-Operation ist er das Runtime-Ergebnis.

| Code | Ausgelöst von | Bedeutung / typische Ursache | Oberfläche | Vorgehen |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` fehlt, ist kein gültiges `omsilaunch.runtime-module/v1`, oder seine `id` weicht vom Verzeichnisnamen ab. Die Meldung nennt das Feld. | Plandiagnose; `ListRuntimeModulesAsync` | Korrigieren Sie das Manifest. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Eine angeforderte Modul-ID ist nicht unterhalb von `plugins\OmsiLaunch.Modules` installiert. | Plandiagnose | Prüfen Sie `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Eine festgelegte Datei fehlt, weicht von ihrem festgelegten SHA-256 ab oder wird über einen Reparse-Punkt erreicht; oder, im Prozess, `module.json` hat sich nach der Planung geändert. | Plandiagnose; Sitzungsdiagnose | Installieren Sie das Modul neu. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | Der Bereich `bridge_protocol` des Moduls enthält keines der Protokolle, die dieser Build bedient (1 und 2). | Plandiagnose | Verwenden Sie eine Modulversion, die für dieses OmsiLaunch erstellt wurde. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Eine erforderliche Capability ist diesem Build unbekannt, auf dieser Installation nicht verfügbar oder im Prozess nicht aufgelöst. | Plandiagnose; Sitzungsdiagnose | Siehe [Capabilities](capabilities.md) und `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | Die Sitzung führt die gebundene Karte des Moduls nicht aus, oder eine gebundene Datei weicht von ihrem festgelegten SHA-256 ab. | Plandiagnose; Sitzungsdiagnose | Starten Sie die gebundene Karte mit den gebundenen Inhalten. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | Das Modul hat darum gebeten, eine Datei zu starten, die keine seiner Hilfskomponenten ist. | Runtime-Ergebnis innerhalb des Moduls | Defekt des Moduls. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | Die verwaltete Komponente konnte nicht geladen oder ihr Einstiegstyp nicht erzeugt werden; oder das Sitzungsprogramm des Moduls konnte nicht gestartet werden oder wurde beendet, bevor es bereit war. | Sitzungsdiagnose | Prüfen Sie den Build des Moduls (x86-kompatibel, Einstiegstyp implementiert `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` oder `Stop` hat eine Ausnahme ausgelöst. | Sitzungsdiagnose | Defekt des Moduls; seine `failure_policy` wurde angewendet. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Eine Phase oder eine Operation kehrte nach ihrem Budget zurück (`timeouts`, `timeout_ms`), oder ein Sitzungsprogramm war nicht innerhalb von `prepare_ms` bereit. | Sitzungsdiagnose; Runtime-Ergebnis | Defekt des Moduls; seine `failure_policy` wurde angewendet. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | Das Modul wurde deaktiviert oder ist früher in der Sitzung fehlgeschlagen. | Runtime-Ergebnis | Den ersten Fehler finden Sie in den Sitzungsdiagnosen. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Die Sitzung hat dieses Modul nicht geladen, oder es ist noch nicht bereit (vor dem Gameplay). | Runtime-Ergebnis | Fordern Sie das Modul beim Start an; warten Sie auf `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Das Modul deklariert keine solche Operation. | Runtime-Ergebnis | Prüfen Sie die `operations` des Moduls. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | Die Operation schlug ohne katalogisierten Code fehl; ein eigener Code des Moduls steht in `Values["module_code"]`. | Runtime-Ergebnis | Modulspezifisch. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Nach Stopp oder Fehler war eine nachverfolgte Ressource noch aktiv (ein Hilfsprozess, der nicht beendet wurde, ein Hintergrund-Thread, der nicht endete) oder ihre Freigabe löste eine Ausnahme aus. | Sitzungsdiagnose | Melden Sie das Modul; der Prozess endet in jedem Fall mit OMSI. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Ein Modul, dessen `failure_policy` `fail-session` ist, ist fehlgeschlagen; die Sitzung wird gestoppt und wiederhergestellt. | Sitzungsdiagnose (Zustand `Failed`) | Siehe den vorangehenden Modulcode. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Eine angeforderte Moduleinstellung ist ungültig oder vom Modul nicht deklariert, ein Modul ist sowohl aktiviert als auch deaktiviert, ein Einstellungsname ist nicht `<module-id>.<key>`, oder ein Modul-Flag wird von mehreren Modulen deklariert. | Plandiagnose (nicht ausführbar); CLI-Fehler | Korrigieren Sie die Anforderung; `/list:RuntimeModules` zeigt die Einstellungen jedes Moduls. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Ein OMSI-Plugin, das das Modul unter `conflicts.plugins` auflistet, ist eingeschaltet (`plugins\*.opl`) oder in OMSI geladen. Die Begründung nennt die `.opl`. | Plandiagnose; Sitzungsdiagnose | Schalten Sie dieses Plugin aus oder entfernen Sie es. OmsiLaunch ändert es nie. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | reserviert | Reserviert; eine native Komponente erhält stattdessen die in [Runtime-Module](../../../reference/runtime-modules.md#native-components-and-hooks) beschriebenen numerischen Hook-Ergebnisse. | keine | keine |

<a id="non-error-diagnostic-codes"></a>
## Diagnosecodes, die keine Fehler sind

| Code | Ausgegeben von | Meldung / Daten | Bedeutung |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | Meldung = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` wurde erstellt und ihre Identität aufgezeichnet. Sitzungsdiagnose. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | Meldung = SHA-256 der entfernten Datei | Eine `closecheck`, die vor der Sitzung existierte, wurde dauerhaft entfernt (`SuppressStaleClosecheckWarning = true`). Sitzungsdiagnose. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | Meldung = relativer Pfad; `Data["sha256"]` | Ein Sitzungs-Löschpfad wurde während einer Sitzung, deren Prozess gestartet worden war, von OMSI neu erstellt; er wurde entfernt, um die ursprüngliche Abwesenheit wiederherzustellen. Sitzungsdiagnose / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | Meldung = `manifest` oder `self` | Welche Referenz die Validierung des permanenten Plugins verwendet. Plandiagnose. |
| `module.planned` | `SessionPlanner` | Meldung = Modul-ID; `Data["module"]`, `Data["status"]`, `Data["activation"]` | Das Modul ist im Plan. Plandiagnose. |
| `module.disabled` | `SessionPlanner` | Meldung = Modul-ID | Ein Inhalts- oder Standardmodul wurde durch eine Deaktivierung (Anforderung oder Einstellungsdatei) herausgehalten. Plandiagnose. |
| `session_profile.selected` | `SessionPlanner` | Meldung = Profil-ID; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Herkunft einer aus einem Sitzungsprofil kompilierten Sitzung. Plandiagnose. |

Runtime-Ereignisnamen (`RuntimeEvent.Type`, keine Diagnosen) sind im [Sitzungslebenszyklus](../concepts/session-lifecycle.md#telemetry-events) aufgeführt.
