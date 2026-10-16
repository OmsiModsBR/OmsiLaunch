# Das permanente Plugin

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Übersetzung der [englischen Originalseite](../../../concepts/permanent-plugin.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

OmsiLaunch steuert OMSI aus dem OMSI-Prozess heraus über ein Plugin, das einmalig unter `plugins\OmsiLaunch.*` als Teil des Produkts installiert wird. Es wird von einer Sitzung nie bereitgestellt, kopiert, in einen Snapshot aufgenommen, wiederhergestellt oder entfernt. Diese Seite erklärt, was die Plugin-Gesamtheit (Closure) enthält, wie OMSI sie lädt, wie der Host sie vor jedem Start validiert, wie Host und Plugin miteinander kommunizieren (Übergabe, Telemetrie, Runtime-Mailbox) und was das Plugin tut, wenn OMSI ohne OmsiLaunch gestartet wird. Quellen: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, die drei Shared-Memory-Speicher), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` und `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## Die Closure (9 Dateien)

| Datei unter `plugins\` | Rolle |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | OMSI-Plugin-Deskriptor. Sein Inhalt ist `[dll]`, gefolgt von `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Nativer x86-Export-Shim, erzeugt von DNNE 2.0.6. Exportiert die Plugin-ABI von OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) und hostet die .NET-Runtime. Enthält die Versionsressource des Produkts. |
| `OmsiLaunch.Plugin.dll` | Verwaltetes Plugin (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | .NET-Abhängigkeitsmanifest für das Plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | .NET-Runtime-Konfiguration (Framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Übertragungsformate und öffentliche Records, die mit dem Host geteilt werden. |
| `OmsiLaunch.Builds.Omsi23004.dll` | Das Build-Profil: Fingerprint der ausführbaren Datei, globale Variablen, Objektlayouts, Methodenadressen. |
| `OmsiLaunch.Interop.dll` | Prozessinterne Speicher-Leser und -Schreiber auf Basis des Profils. |
| `OmsiLaunch.Native.x86.dll` | Native Bridge (C++): Build-Validierung, Headless-Start-Hook, Anwenden der Zeit, MakeVehicle, PlaceRandomBus, Unterdrückung der Internettexturen, Zugriff auf das D3D9-Device. |

`RuntimeArtifactSet.Load` leitet diese Liste aus dem eigenen `plugins\`-Verzeichnis des Controllers ab: die vier benannten Dateien (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), jede `OmsiLaunch.*.dll` in diesem Verzeichnis außer `OmsiLaunch.PluginNE.dll` sowie die native Bridge. `OmsiLaunch.Plugin.dll` muss darunter sein. Beliebige DLLs werden nie in OMSI übernommen. Der Release-Packager (`tools/New-ReleasePackage.ps1`) schreibt genau die neun oben genannten Dateien.

<a id="how-omsi-loads-it"></a>
## Wie OMSI es lädt

1. OMSI zählt `plugins\*.opl` auf und lädt die in `OmsiLaunch.Plugin.opl` genannte DLL: `OmsiLaunch.PluginNE.dll`.
2. Der DNNE-Shim startet die durch `OmsiLaunch.Plugin.runtimeconfig.json` beschriebene x86-.NET-6-Runtime innerhalb des OMSI-Prozesses und löst die verwalteten Exporte in `OmsiLaunch.Plugin.dll` auf.
3. OMSI ruft `PluginStart` auf. OMSI kann es während des Starts mehrfach aufrufen; nur der erste Aufruf wird berücksichtigt (`Interlocked.Exchange`-Schutz), weil ein erfolgreicher Start einen einmaligen nativen Hook besitzt. Spätere Aufrufe kehren sofort zurück.
4. `PluginStart` prüft zuerst `OMSILAUNCH_INTERNET_TEXTURES_MODE`: Ist der Wert `Disabled`, wird die native Unterdrückung des Downloaders angewendet (`internet-textures.suppressed` oder `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` liest die Übergabe (siehe unten), validiert den Build im Prozess, schaltet den Headless-Start-Hook scharf, öffnet die Runtime-Mailbox und plant den Weltstart auf dem UI-Thread von OMSI über einen `SetTimer`-Callback ein. Kein Runtime-Befehl wird je auf einem IPC-Worker-Thread ausgeführt; alles läuft im Timer-Callback, auf dem ursprünglichen UI-Thread von OMSI.
6. `PluginFinalize` beendet den Timer, fährt die Runtime herunter (`NativeD3DShutdown`) und stellt den Internettextur-Patch wieder her.

Die Exporte `AccessVariable`, `AccessTrigger`, `AccessStringVariable` und `AccessSystemVariable` sind leer; OmsiLaunch verwendet den Plugin-Kanal für Skriptvariablen von OMSI nicht.

<a id="integrity-validation-before-every-start"></a>
## Integritätsvalidierung vor jedem Start

`RuntimeArtifactSet.ValidateInstalled` läuft während `StartSessionAsync` (nach der frühen Recovery, bevor die Transaktion vorbereitet wird) und während `PlanSessionAsync` (nur Vorhandensein, über `LoadArtifacts`). Die Plandiagnose `plugin.integrity.reference` meldet, welche Referenz verwendet wurde.

| Situation | Referenz | Prüfung pro Datei | Fehler |
| --- | --- | --- | --- |
| `release-manifest.json` neben `OmsiLaunch.exe` vorhanden (installiertes Paket) | `manifest` | Die installierte Datei muss existieren, und ihr SHA-256 muss dem Manifesteintrag für `plugins/<name>` entsprechen | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (das Manifest hat keinen Eintrag für eine erforderliche Datei), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Kein Manifest (Entwicklungsstruktur) | `self` | Vorhandensein plus Selbstkonsistenz: Der Hash der installierten Datei muss dem der Datei im eigenen `plugins\`-Verzeichnis des Controllers entsprechen | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifest nicht lesbar oder fehlerhaft (`files`-Array fehlt, Eintrag ohne `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Closure im Controller-Verzeichnis unvollständig | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (Plan nicht ausführbar); die CLI meldet zusätzlich `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, wenn `plugins\OmsiLaunch.Plugin.opl` oder `OmsiLaunch.Native.x86.dll` fehlt |

Nur die `plugins/`-Einträge des Manifests werden ausgewertet; das Manifest sind Daten, niemals Richtlinien. Das Manifest wird vom Release-Packager mit dem SHA-256 jeder bereitgestellten Datei erzeugt. Läuft der Controller aus dem OMSI-Stammverzeichnis (die Release-Struktur), sind Quelle und Ziel dasselbe Verzeichnis; ohne Manifest reduziert sich die Prüfung daher auf Vorhandensein und Selbstkonsistenz, weshalb veröffentlichte Pakete immer `release-manifest.json` enthalten. Abhilfe bei einer Abweichung: Installieren Sie das Paket neu, damit `plugins\` und `release-manifest.json` übereinstimmen.

Bevor das Plugin etwas patcht, löst es jedes native Ziel des Build-Katalogs gegen das geladene Image auf, verifiziert es und übergibt die verifizierte Tabelle an `OmsiLaunch.Native.x86.dll`, die keine OMSI-Adresse enthält (Telemetrie `native.capabilities`). Ein Start, dessen `boot.headless-start`- oder Weltmodus-Capability nicht verfügbar ist, schlägt mit `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`) fehl; ein aktives Start-Formular in einem unerwarteten Zustand schlägt mit `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`) fehl. Siehe [Kompatibilität](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Vom Host zum Plugin: Umgebungsvariablen

`CreateProcessW` startet `Omsi.exe` mit der Umgebung des übergeordneten Prozesses plus:

| Variable | Wert | Verbraucher |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | Sitzungs-GUID (`D`-Format) | `CurrentRuntimeControl` kennzeichnet D3D-Handles damit (`N`-Format) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` öffnet dieses Mapping schreibgeschützt |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` oder `Override` | `PluginStart` (nur `Disabled` hat eine prozessinterne Wirkung) |

Die drei Mappings werden vom Host vor dem Start des Prozesses erstellt (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) und freigegeben, wenn der Lebenszyklus-Task der Sitzung endet. Es sind benannte Kernelobjekte mit der Standard-DACL des startenden Benutzers; jeder Prozess desselben Benutzers kann sie öffnen (akzeptiertes Vertrauensmodell für denselben Benutzer, siehe [bekannte Einschränkungen](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## Die Start-Übergabe

Ein speicherabgebildeter, schreibgeschützter Datensatz mit festem Layout (`StartupHandoffWire`, Magic `OLSH`, Version 4; Version 3 wird vom Leser weiterhin akzeptiert). Der 64-Byte-Header enthält Magic, Version, Header-Größe, Gesamtgröße, Sitzungs-GUID, Payload-Größe und den SHA-256 der Payload. Die Payload enthält `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 mit Längenpräfix), `PresentedEntrypointIndex`, `WorldMode`, Flags (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` und `TimeMode`. Das Plugin hasht die Payload erneut und weist jede Abweichung zurück (`plugin.handoff.invalid`, Host-Fehler `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Ein Mapping, das größer als 1 MiB ist oder eine inkonsistente Größe hat, wird auf dieselbe Weise zurückgewiesen.

Das Plugin akzeptiert eine Übergabe nur, wenn `WorldMode` gleich `NewMap` oder `SavedSituation` ist, `HeadlessStart` gesetzt ist, `PlayerVehicleEnabled` nicht gesetzt ist, sowohl Datums- als auch Zeitmodus `Unset` sind und eine gespeicherte Situation ihre `.osn` benennt. Alles andere ist `plugin.request.unsupported` (Host-Fehler `OL_E_CAPABILITY_UNAVAILABLE`). Der Host setzt `HeadlessStart` immer.

<a id="telemetry-slot"></a>
## Telemetrie-Slot

`OmsiLaunch.Telemetry.<session>` ist ein Ereignisring (Beta 4, `TelemetryRingWire`): ein 64-Byte-Header (Magic `OLTR`, Version 1, Slot-Anzahl, Slot-Größe, festgeschriebene Sequenz) und 64 nummerierte Slots zu je 2048 Byte, die jeweils ein UTF-8-JSON-Ereignis `{ "name": ..., "data": { ... }, "tick": <ms> }` enthalten (`tick` ist die monotone Sitzungsuhr des Plugins). Das Plugin ist der einzige Schreiber: Es invalidiert einen Slot, schreibt ihn, schreibt die Sequenz des Slots fest und veröffentlicht dann die Sequenz des Rings. Der Host liest alle 100 ms jedes Ereignis, das seit seinem letzten Lesen festgeschrieben wurde, in der richtigen Reihenfolge; ein Slot, der während des Kopierens überschrieben wurde, wird als verloren gezählt und nie inkonsistent ausgeliefert. Ein Host, der mehr als 64 Ereignisse zurückfällt, erhält das Runtime-Ereignis `telemetry.lost` mit der exakten Anzahl. Ein Ereignis, das größer als ein Slot ist, wird durch `telemetry.oversized` ersetzt, das es benennt. Ereignisse werden an `SessionStatus.RuntimeEvents` angehängt (begrenzt auf die letzten 256) und steuern den semantischen Lebenszyklus (`plugin.started`, `world.starting`, `gameplay.entered`, Modul- und Fehlerereignisse).

<a id="runtime-command-mailbox"></a>
## Runtime-Befehlsmailbox

`OmsiLaunch.Runtime.<session>` ist eine Single-Flight-Mailbox von 64 KiB: `state` (int32 an 0: 0 leer, 1 angefordert, 2 beantwortet), `length` (int32 an 4), Envelope an 8. Envelopes sind `RuntimeCommandWire`-Records (Magic `OLRC`, Version 1, 72-Byte-Header mit Art, Gesamtlänge, Sitzungs-GUID, Anforderungs-ID, Payload-Länge und SHA-256 der UTF-8-JSON-Payload). Das Plugin fragt die Mailbox von seinem UI-Thread-Timer aus ab (50 ms, sobald die Welt geladen ist), führt den Befehl auf diesem Thread aus und veröffentlicht die Antwort nur, wenn der Slot noch dieselbe Anforderungs-ID enthält; eine Anforderung, die der Host wegen eines Timeouts aufgegeben hat, wird nie beantwortet. Eine übergroße Antwort wird durch einen typisierten `OL_E_RUNTIME_RESPONSE_TOO_LARGE`-Fehler ersetzt. Details und Timeouts finden Sie unter [Runtime-Steuerung](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## DLL-Suchrichtlinie

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` und die CLI-Assemblys deklarieren `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Native Importe (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) werden ausschließlich aus dem eigenen Verzeichnis der Assembly (`plugins\`) oder dem Windows-Systemverzeichnis aufgelöst; das OMSI-Stammverzeichnis und `PATH` werden nie durchsucht. `OmsiLaunch.Native.x86.dll` wird daher aus `plugins\` geladen und von nirgendwo sonst.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Wenn OMSI ohne OmsiLaunch gestartet wird

Da die Closure permanent ist, lädt OMSI `OmsiLaunch.PluginNE.dll` bei jedem Start, auch bei Starts über Steam oder den Desktop. In diesem Fall gilt:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` fehlt, daher wird kein Downloader-Patch angewendet.
- `OMSILAUNCH_HANDOFF_NAME` fehlt, daher sendet `PluginRuntime.Start` `plugin.handoff.invalid` und gibt `false` zurück. `CurrentTelemetrySink.Emit` kehrt sofort zurück, wenn `OMSILAUNCH_TELEMETRY_NAME` nicht gesetzt ist, sodass nirgendwohin etwas geschrieben wird.
- Keine Build-Validierung, kein nativer Hook, keine Mailbox, kein Timer. Das Plugin bleibt geladen, aber inaktiv; OMSI verhält sich, als wäre das Plugin nicht vorhanden.
- `PluginFinalize` ruft beim Beenden von OMSI die native Wiederherstellungsroutine und `NativeD3DShutdown` auf; beide sind Nulloperationen, wenn nichts installiert wurde.

Die `.opl` muss daher nicht entfernt werden, um OMSI normal auszuführen.

<a id="non-interference-with-third-party-plugins"></a>
## Keine Beeinträchtigung von Plugins anderer Anbieter

Sitzungen zählen andere Dateien in `plugins\` nie auf, hashen, kopieren, entfernen oder stellen sie wieder her. Das Plugin verwendet den `AccessVariable`-Kanal von OMSI nicht und berührt nicht den Zustand anderer Plugins. Die einzigen prozessinternen Patches sind der profilierte Headless-Start-Hook (eine einmalige VMT-Umleitung, die für die Sitzung scharfgeschaltet wird), die optionale Unterdrückung der Internettexturen (in `PluginFinalize` wiederhergestellt) und das Abfangen von `Reset` des D3D9-Device, das für die Verfolgung des Texturlebenszyklus verwendet wird.

<a id="difference-from-omsihook"></a>
## Unterschied zu OmsiHook

OmsiLaunch hat **keine Runtime-Abhängigkeit** von OmsiHook oder von irgendeiner OmsiHook-Binärdatei: Die einzigen referenzierten Pakete sind `DNNE` 2.0.6 und `YamlDotNet` 15.1.2, und es gibt nirgendwo im Produkt ein `using OmsiHook` oder P/Invoke in OmsiHook-DLLs. Was OmsiLaunch mit OmsiHook teilt, ist abgeleitetes Wissen: Objektlayouts und mehrere Lese-Wrapper wurden anhand des fixierten OmsiHook-Checkouts (`space928/Omsi-Extensions`, Commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) gegen die exakte ausführbare Datei `Omsi23004_692EBFBF` abgeglichen. Die Namensnennung und die Lizenzbedingungen stehen in `THIRD-PARTY-NOTICES.md`, die Wiederverwendungsmatrix pro Datei in `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook injiziert einen separaten Prozess und legt rohe Zeiger offen; OmsiLaunch läuft im Prozess, legt ausschließlich opake, auf die Sitzung beschränkte Handles offen und entfernt jede native Adresse aus öffentlichen Ergebnissen (siehe [Capabilities](../reference/capabilities.md)).
