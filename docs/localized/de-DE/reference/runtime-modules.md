# Runtime-Module

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> Übersetzung der [englischen Originalseite](../../../reference/runtime-modules.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Ein Runtime-Modul ergänzt eine verwaltete OMSI-Sitzung um Verhalten: eine Funktion, die mit der Sitzung läuft (zum Beispiel das HUD einer Karte oder Ladeoptimierungen), ohne Teil von OmsiLaunch selbst zu werden. Die Bridge lädt es, gibt ihm nur das, was es deklariert, und entfernt alles, was es hält, wenn die Sitzung endet oder das Modul fehlschlägt. Diese Seite ist die normative Referenz für Runtime-Module, wie sie von `src/OmsiLaunch.Runtime.Modules` (Manifest, Einstellungen, Eignung, Lifecycle-Host), dem Planer, den Sitzungsprogrammen des Controllers (`ModuleSessionPrograms`, `ModuleSessionProcess`), der Modulumgebung des Plugins und den Modul-Flags der [CLI](cli.md) umgesetzt werden.

Stabilität: `EXPERIMENTAL`. Bridge-Protokoll 1 wurde in Runde B zur Laufzeit validiert (verwaltete Module, Inhaltsaktivierung, Operationen, `disable-module` und `fail-session`; native Komponenten mit IAT-Hooks; siehe [Status der Runtime-Validierung](../status/runtime-validation-status.md)). Bridge-Protokoll 2 (Einstellungen, Standardaktivierung, die Einstellungsdatei, Sitzungsprogramme, Plugin-Konflikte, Hinweise, `get_value`) ist nur offline validiert. Offline-Gates: die `module.*`-Gates von `tests/OmsiLaunch.ModuleTests` (Manifest, Integrität, Auswahl und Aktivierung, Einstellungen, die Einstellungsdatei, Plugin-Konflikte, über ihren Job gestartete Sitzungsprogramme, ein in jeder Phase unter jeder Fehlerrichtlinie injizierter Fehler, Lifecycle der nativen Komponente, Cleanup, Übergabe), `plugin-runtime.modules-lifecycle` (ein echtes verwaltetes Modul, das von der Umgebung des Plugins geladen wird), `runtime-modules.native-component` (eine echte native Komponente, die einen Import hookt und Werte über die Bridge liest), `runtime-modules.plugin-conflict` und `native-hooks.self-check` (die Hook-Engine). Bridge-Protokoll 3 (Beta 5: Katalogoperationen im Prozess, der Tick, die Eingabeerfassung, Community-Inhalte, optionale Festlegungen mit dem Snapshot zur Planungszeit, `module pack`, Optionen für Hilfsjobs, die OMSI-Version) ist nur offline validiert: die `module.*`-Gates von `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` und `ui-input.hook-self-check` der Plugin-Integrationstests sowie `omsi-version.literal` der Unit-Tests. Die Übernahme des Pausenmenüs und die Eingabeerfassung wurden in einer echten OMSI-Sitzung nicht beobachtet.

Das Laden ist offen: Jedes unter `plugins\OmsiLaunch.Modules` installierte Modul kann geladen werden, unabhängig davon, wer es erstellt hat. OmsiLaunch bürgt nicht dafür, was ein Modul innerhalb von OMSI tut. Garantiert wird die [Grenze](#security-boundary): Module laufen nie im OmsiLaunch-Prozess, und nichts, was OmsiLaunch anbietet, lässt ein Modul über die eigenen Rechte von OMSI hinausreichen.

<a id="where-modules-live"></a>
## Wo Module liegen

Jedes Modul ist ein Verzeichnis unterhalb des permanenten Plugin-Ordners:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

Der Verzeichnisname muss der `id` des Moduls entsprechen. Module werden von demjenigen installiert, der sie verteilt; eine Sitzung schreibt, sichert oder stellt dort nie Dateien wieder her, und Module ändern über OmsiLaunch nie Installationsdateien (ein Manifest kann kein Installations-Overlay deklarieren). Die beschreibbaren Daten eines Moduls liegen in seinem [Datenverzeichnis](#data-directory).

## Manifest (`module.json`)

Schema `omsilaunch.runtime-module/v1`. Unbekannte Schlüssel werden abgelehnt, sodass ein für ein späteres Schema geschriebenes Manifest zurückgewiesen statt halb verstanden wird. Alle Pfade sind relativ zum Modulverzeichnis (gebundene Inhaltspfade sind relativ zum OMSI-Stammverzeichnis), verwenden `\` oder `/` und dürfen weder `..` noch ein Laufwerk enthalten.

| Schlüssel | Erforderlich | Bedeutung |
| --- | --- | --- |
| `schema` | ja | `omsilaunch.runtime-module/v1`. |
| `id` | ja | Modul-ID: Kleinbuchstaben, Ziffern und `-`, 2 bis 48 Zeichen, beginnend mit einem Buchstaben oder einer Ziffer. Zugleich sein Verzeichnisname und sein Operations-Namespace. |
| `version` | ja | `major.minor.patch`, optional `-suffix`. |
| `name` | nein | Anzeigename (höchstens 128 Zeichen). |
| `bridge_protocol` | ja | `{ "min": n, "max": n }`: die Bridge-Protokolle, mit denen das Modul arbeitet. Dieser Build bedient die Protokolle `1`, `2` und `3`; ein Modul wird akzeptiert, wenn sein Bereich eines davon enthält. |
| `activation` | nein | `requested` (Standard): wird geladen, wenn die Sitzung es anfordert. `content`: wird zusätzlich geladen, wenn die Sitzung die gebundene Karte ausführt (benötigt `content_binding`). `default` (Protokoll 2): wird in jeder Sitzung geladen, sofern es nicht deaktiviert ist (siehe [Auswahl der Module für eine Sitzung](#choosing-modules-for-a-session)); es darf kein `content_binding` haben. |
| `required_capabilities` | nein | Capabilities, ohne die das Modul nicht arbeiten kann (siehe [Capabilities](#capabilities)). |
| `optional_capabilities` | nein | Capabilities, die das Modul nutzt, wenn sie verfügbar sind; es prüft sie mit `IsCapabilityAvailable`. |
| `content_binding` | nein | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: das Modul läuft nur auf dieser Karte, mit diesen unveränderten Dateien. |
| `assets` | nein | Dateien, die das Modul liest, jeweils `{ "path", "sha256" }`. Ab Protokoll 3 ist das `sha256` optional (siehe [Integrität](#integrity)). |
| `components` | ja | Genau eine `managed`-Komponente `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; optional eine `native`-Komponente (`.dll`), `auxiliary`-Komponenten (`.exe`) und, mit Protokoll 2, eine `host`-Komponente (`.exe`, das [Sitzungsprogramm](#session-programs) des Moduls). Ab Protokoll 3 ist das `sha256` einer Komponente optional, und eine `auxiliary`-Komponente darf `priority` (`normal`, `below-normal` oder `idle`) und `memory_limit_mb` (64 bis 65536) tragen: Das Job-Objekt des Programms wendet sie ab seiner ersten Anweisung an (siehe [Hilfsprogramme](#auxiliary-programs)). |
| `failure_policy` | ja | `fail-session`, `disable-module` oder `degrade` (siehe [Fehler](#failures)). |
| `operations` | nein | Operationen, die das Modul bereitstellt, während die Sitzung läuft: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | nein | Protokoll 2. Die Einstellungen, die das Modul akzeptiert (siehe [Einstellungen](#settings)). |
| `conflicts` | nein | Protokoll 2. `{ "plugins": [ "<file>.dll" ] }`: OMSI-Plugin-DLLs, die nicht neben dem Modul laufen dürfen (siehe [Konflikte mit OMSI-Plugins](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | nein | Protokoll 3. Operationen des öffentlichen Capability-Katalogs, die das Modul im Prozess ausführt, zum Beispiel `["time.read", "camera.set"]` (siehe [Operationen im Prozess](#operations-in-process)). Benötigt die Capability `bridge.runtime`. |
| `tick_ms` | nein | Protokoll 3. Das Modul möchte alle `tick_ms` Millisekunden einen periodischen Callback: ein Vielfaches von 50, 50 bis 1000 (siehe [Tick](#tick)). Benötigt die Capability `bridge.tick`. |
| `content_inputs` | nein | Protokoll 3. Community-Inhalte, die das Modul liest und die die Bridge lokalisiert: `[ { "kind": "realmap-package", "required": false } ]` (siehe [Community-Inhalte](#community-content)). |
| `timeouts` | nein | Budgets in Millisekunden: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (jeweils Standard 5000, höchstens 30000); mit Protokoll 2 außerdem `prepare_ms` (Standard 120000, höchstens 1800000) und `host_stop_ms` (Standard 10000, höchstens 60000) für das Sitzungsprogramm. Operationen haben standardmäßig 2000 ms. |
| `diagnostics` | nein | `{ "trace": true\|false }`. |

Ein Manifest, das eine Funktion von Protokoll 2 verwendet (`settings`, `conflicts`, Aktivierung `default`, eine `host`-Komponente, `prepare_ms` oder `host_stop_ms`), muss `bridge_protocol.min` 2 deklarieren. Ein Manifest, das eine Funktion von Protokoll 3 verwendet (`runtime_operations`, `tick_ms`, `content_inputs`, eine Capability `bridge.runtime`, `bridge.tick` oder `bridge.input`, ein `priority` oder `memory_limit_mb` eines Hilfsprogramms oder eine Komponente oder ein Asset ohne `sha256`), muss `bridge_protocol.min` 3 deklarieren.

Jede von `components` und `assets` genannte Datei darf durch ihren SHA-256 festgelegt sein (vor Protokoll 3 erforderlich, ab Protokoll 3 optional: siehe [Integrität](#integrity)). `module.json` kann sich nicht selbst festlegen.

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## Integrität

Jede Datei, die das Manifest nennt, muss existieren und darf nicht über eine Junction oder einen symbolischen Link erreicht werden, und jede vom Autor festgelegte Datei muss ihren festgelegten SHA-256 haben; andernfalls wird das Modul mit `OL_E_MODULE_INTEGRITY` abgelehnt. Das erkennt ein Modul, das beschädigt oder nur halb aktualisiert ist. Es ist keine Vertrauensentscheidung: Ein Modul wird weder gegen eine Liste noch gegen eine Signatur oder einen Herausgeber geprüft.

Ab Protokoll 3 ist eine Festlegung optional, sodass ein Autor nie Hashes von Hand pflegen muss, und der Plan erstellt immer einen **Snapshot**: einen SHA-256 über jede Datei, die das Manifest nennt, und ihren Inhalt, im Plan festgehalten (`PlannedModule.SnapshotSha256`) und in der Start-Übergabe (Wire-Version 8) zum Plugin transportiert. Der Snapshot garantiert, dass die Dateien, die der Plan ausgewertet hat, die Dateien sind, die laufen: Das Plugin lädt ein Modul nur, wenn seine `module.json` den SHA-256 hat, den der Plan ausgewertet hat, und seine Dateien noch den Inhalt des Snapshots haben, und der Controller und die Bridge vergleichen den Snapshot unmittelbar bevor ein Sitzungsprogramm, eine native Komponente oder ein Hilfsprogramm startet erneut. Ein Modul, dessen Dateien sich nach der Planung geändert haben, wird nicht geladen (`OL_E_MODULE_INTEGRITY`, „the module's files changed after planning“). Der Snapshot deckt jedes Modul ab, festgelegt oder nicht; eine Festlegung erlaubt es zusätzlich demjenigen, der ein Modul veröffentlicht, eine beschädigte oder nur halb aktualisierte Kopie zu erkennen, sobald sie entdeckt wird.

Die Prüfung wird dort wiederholt, wo der Code des Moduls startet: Das Plugin lädt ein Modul nur, wenn seine `module.json` noch den SHA-256 hat, den der Plan ausgewertet hat, und jede Festlegung und der Snapshot weiterhin gelten, und der Controller startet ein Sitzungsprogramm erst, nachdem er die Festlegung dieses Programms und den Snapshot geprüft hat.

<a id="packing-a-module"></a>
### Ein Modul packen

`OmsiLaunch.exe /pack-module:<module directory>` schreibt die Festlegungen für die Dateien, die eine `module.json` nennt (ihre Komponenten und Assets), sodass ein Autor nie einen SHA-256 von Hand bearbeitet. Es schreibt nur die `sha256`-Werte neu (der Rest des Manifests bleibt erhalten; die Datei wird neu eingerückt), liest das Ergebnis vor dem Schreiben mit dem Manifest-Parser erneut ein und meldet jede Datei (Envelope `module.pack`: `module`, `directory`, `written`, `intact`, `files` mit `path`, `sha256`, `pinned`, `mismatch`, `written`). Es arbeitet auf dem angegebenen Verzeichnis und berührt nie eine Installation. `/verify-module:<module directory>` meldet dasselbe, ohne zu schreiben (`module.verify`); es beendet sich mit `7`, wenn eine festgelegte Datei nicht zu ihrer Festlegung passt (eine Datei ohne Festlegung ist keine Abweichung). Beide beenden sich mit `2` bei einem Manifest, das sich nicht parsen lässt, oder einer fehlenden Datei.

<a id="security-boundary"></a>
## Sicherheitsgrenze

OmsiLaunch kontrolliert nicht, was ein Modul innerhalb von OMSI tut: Seine verwalteten und nativen Komponenten laufen im OMSI-Prozess mit denselben Rechten wie jedes OMSI-Plugin. OmsiLaunch garantiert Folgendes:

1. Der OmsiLaunch-Prozess lädt nie Modulcode. Modulcode läuft innerhalb von OMSI, in einem von innerhalb von OMSI gestarteten Hilfsprogramm oder im Sitzungsprogramm des Moduls.
2. Die Programme, die OmsiLaunch für Module startet, sind Dateien des eigenen Verzeichnisses des Moduls, gegen ihre Festlegung geprüft und mit einer von OmsiLaunch fest vorgegebenen Befehlszeile gestartet. Ein Sitzungsprogramm läuft in einem Job-Objekt, das es mit der Sitzung beendet, keinen zweiten Prozess zulässt (es kann keine anderen Programme starten) und Änderungen an Systemparametern, Anzeigeeinstellungen, dem Desktop sowie Abmeldung oder Herunterfahren verweigert. Es erbt nur seine beiden Pipe-Handles.
3. Alles, was ein Modul deklariert, bleibt innerhalb seines Verzeichnisses, und seine beschreibbaren Daten innerhalb von `.omsilaunch\module-data\<id>`.
4. Die OmsiLaunch-Kanäle (lokale Steuerung, Start-Übergabe, Telemetrie, Runtime-Befehle) bleiben auf den aktuellen Benutzer beschränkt und an die Sitzung gebunden; keiner davon erlaubt einem Aufrufer, ein Programm zu starten, Dateien außerhalb der Installation zu lesen oder zu schreiben oder Systemeinstellungen zu ändern.
5. OmsiLaunch erhöht nie die Berechtigungen. Ein Modul läuft mit den Rechten des Benutzers, der die Sitzung gestartet hat.
6. Text, der von einem Modul stammt (Hinweise, Ereignisse, Werte), wird als Daten behandelt: begrenzt, ohne Steuerzeichen, nie ausgeführt, geöffnet oder als Link verfolgt.

## Capabilities

`required_capabilities` und `optional_capabilities` nennen entweder eine native Capability des laufenden Builds (die Spalte der nativen Capability im [Capability-Katalog](capabilities.md#capability-catalog), zum Beispiel `time.read` oder `d3d.texture`) oder ein Bridge-Feature:

| Bridge-Feature | Was das Modul erhält |
| --- | --- |
| `bridge.events` | `Emit`: Runtime-Ereignisse `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: nachverfolgte Hintergrund-Threads. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: an die Sitzung gebundene Hilfsprozesse. |
| `bridge.hooks.iat` | Native Komponente: Hooks auf Importe von `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Native Komponente: Hooks auf Methoden des aktiven Direct3D-9-Geräts (`d3d9:device.<method>`). Benötigt die native Capability `d3d.texture`. |
| `bridge.runtime` | Protokoll 3. `IRuntimeModuleContext.Runtime` und das native `run_operation`: die Katalogoperationen, die das Manifest in `runtime_operations` auflistet. |
| `bridge.tick` | Protokoll 3. Ein periodischer Callback: `ITickingModule.Tick` und der native Export `omsilaunch_module_native_v1_tick`. |
| `bridge.input` | Protokoll 3. `IRuntimeModuleContext.Input`: Erfassung der Tastatur- und Mausnachrichten des Spiels für das eigene Menü des Moduls. Benötigt die native Capability `ui.input.capture`. |

Ein Name, der keines von beiden ist, wird bei der Planung abgelehnt (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). Bei einem OMSI-2-Build ohne bekanntes Layout wird eine native Capability, die die ausführbare Datei nicht auflöst, im Prozess entschieden; wird sie dort nicht aufgelöst, wird das Modul nicht geladen.

<a id="choosing-modules-for-a-session"></a>
## Auswahl der Module für eine Sitzung

Ein Modul nimmt auf eine von vier Arten an einer Sitzung teil; `PlannedModule.Activation` gibt an, welche:

| Aktivierung | Wann |
| --- | --- |
| `requested` | Die Sitzung fordert es an: `LaunchSpec.Modules.Enable`, `/module:<id>` oder `enabled: true` in der ausgewählten Voreinstellung des Sitzungsprofils. |
| `settings-file` | `enabled=true` in seinem Abschnitt der [Einstellungsdatei](#the-settings-file). |
| `content` | Seine Aktivierung ist `content` und die Sitzung führt seine gebundene Karte aus. |
| `default` | Seine Aktivierung ist `default`. |

Ein Modul wird durch `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` in der Voreinstellung oder `enabled=false` in der Einstellungsdatei aus der Sitzung herausgehalten (Plandiagnose `module.disabled`). Eine ausdrückliche Anforderung hat Vorrang vor der Einstellungsdatei. Dasselbe Modul in einer Anforderung zu aktivieren und zu deaktivieren ergibt `OL_E_MODULE_SETTING_INVALID`.

Planungsregeln: Ein angefordertes Modul, das nicht geladen werden kann, macht den Plan mit seinem `OL_E_MODULE_*`-Code nicht ausführbar. Ein Modul, das von selbst teilnahm (Einstellungsdatei, Inhalt, Standard) und nicht geladen werden kann, stoppt die Sitzung nicht: Der Plan meldet `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Einstellungen

Ein Modul deklariert seine Einstellungen in `settings`; jeder Eintrag:

| Schlüssel | Erforderlich | Bedeutung |
| --- | --- | --- |
| `key` | ja | `[a-z][a-z0-9_]{0,31}`, nicht `enabled`. |
| `type` | ja | `boolean`, `integer`, `choice` oder `text`. |
| `default` | ja | Der Wert, wenn nichts anderes ihn setzt: ein Boolean, eine Ganzzahl, eine Zeichenfolge oder `"auto"`, wenn `auto` true ist. |
| `auto` | nein | `true`: der Wert `auto` wird akzeptiert und an das Modul weitergegeben, das zur Laufzeit entscheidet. |
| `min`, `max` | nein | Ganzzahlgrenzen. |
| `choices` | für `choice` | Die akzeptierten Werte (`[a-z0-9][a-z0-9_.-]{0,31}`, nicht `auto`). |
| `max_length` | nein | Längenlimit für Text (1..4096, Standard 256). Steuerzeichen werden abgelehnt. |
| `flag` | nein | Ein Befehlszeilen-Flag für diese Einstellung, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, oder allein `/<flag>` für `true` bei einem Boolean. |
| `description` | nein | Höchstens 256 Zeichen. |

Werte werden normalisiert: Booleans akzeptieren `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; Auswahlwerte werden ohne Beachtung der Groß-/Kleinschreibung abgeglichen; Ganzzahlen werden gegen ihre Grenzen geprüft.

Der effektive Wert jeder Einstellung ist, in dieser Reihenfolge:

1. die Anforderung: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, ein eigenes Flag des Moduls oder `modules.<id>.settings` in der ausgewählten Voreinstellung des Sitzungsprofils. Wie bei jedem anderen Feld einer Voreinstellung kann die Befehlszeile keinen Schlüssel setzen, den auch die Voreinstellung setzt (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. die [Einstellungsdatei](#the-settings-file);
3. der Standardwert des Manifests.

Ein ungültiger angeforderter Wert oder ein angeforderter Schlüssel, den das Modul nicht deklariert, macht den Plan nicht ausführbar (`OL_E_MODULE_SETTING_INVALID`). Ein Eintrag der Einstellungsdatei, der nicht verwendet werden kann, wird mit `OL_W_MODULE_SETTING_IGNORED` ignoriert, ebenso angeforderte Einstellungen für ein Modul, das die Sitzung nicht lädt. Der Plan listet jeden effektiven Wert mit seiner Quelle (`request`, `settings-file` oder `default`) in `PlannedModule.Settings` auf.

Das Modul erhält jeden deklarierten Schlüssel mit seinem effektiven Wert: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` in einer nativen Komponente und das `settings`-Objekt im Kontext eines Sitzungsprogramms.

<a id="the-settings-file"></a>
### Die Einstellungsdatei

`<OMSI root>\.omsilaunch\modules.ini`, neben den Sitzungsprofilen. OmsiLaunch liest sie nur.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Abschnitte sind Modul-IDs; `key=value`-Zeilen setzen Einstellungen, `enabled` schaltet das Modul für jede Sitzung ein oder aus; Zeilen, die mit `;` oder `#` beginnen, sind Kommentare. Die Datei darf höchstens 256 KiB groß sein. Alles, was sie nicht verwenden kann (ein Abschnitt, der keine Modul-ID ist, eine Zeile außerhalb eines Abschnitts, ein unbekannter Schlüssel, ein ungültiger Wert), wird mit `OL_W_MODULE_SETTING_IGNORED` gemeldet und ignoriert.

<a id="session-programs"></a>
## Sitzungsprogramme

Ein Modul kann eine `host`-Komponente deklarieren: sein Sitzungsprogramm, eine ausführbare Datei aus seinem eigenen Verzeichnis. Wenn das Modul im Plan enthalten ist, geht der Controller wie folgt vor:

1. prüft die Festlegung des Programms und erstellt das Datenverzeichnis des Moduls;
2. startet es vor OMSI, aus dem Modulverzeichnis, mit der Befehlszeile `"<program>" --omsilaunch-session <session id>`, angehalten, bis es sich in seinem Job befindet (siehe [Sicherheitsgrenze](#security-boundary));
3. schreibt den Kontext als erste Zeile auf seine Standardeingabe;
4. wartet, bis das Programm `{"type":"ready"}` schreibt, höchstens `prepare_ms`; erst dann startet er OMSI;
5. schreibt, nachdem OMSI erstellt wurde, `{"type":"omsi-started","pid":<process id>}`;
6. schreibt am Ende der Sitzung, nachdem OMSI beendet wurde und bevor eine Datei wiederhergestellt wird, `{"type":"stop"}`, wartet höchstens `host_stop_ms` und beendet dann das Programm.

Beide Richtungen verwenden UTF-8-JSON, ein Objekt pro Zeile. Der Kontext:

| Feld | Bedeutung |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identität. |
| `installation_root`, `module_directory`, `data_directory` | Vollständige Pfade. |
| `channel` | Die Named Pipe, die das Programm für die Komponenten des Moduls in OMSI bereitstellen darf: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. Das Programm erstellt sie, beschränkt auf den aktuellen Benutzer, und prüft, dass der Client der OMSI-Prozess ist. Komponenten lesen sie mit `get_value("session_channel")` oder `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | Die effektiven Einstellungen des Moduls. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` der Sitzung. |
| `session_files` | Installationsrelative Dateien, die der Sitzungstransaktion gehören. Das Programm darf sie nicht geöffnet oder gemappt halten: Sie werden wiederhergestellt, wenn die Sitzung endet. |

Was das Programm außer `ready` schreiben darf:

| Zeile | Wirkung |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Runtime-Ereignis `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Runtime-Ereignis `module.<id>.notice` (siehe [Hinweise](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Sitzungsdiagnose mit diesem katalogisierten Code (sonst `OL_E_MODULE_OPERATION_FAILED`). |

Andere Zeilen, Zeilen mit mehr als 64 KiB und alles nach 10,000 Zeilen werden ignoriert. Der Controller protokolliert `module.<id>.host.started`, `host.ready` (mit `prepare_ms`), `host.failed`, `host.exited` und `host.killed`.

Ein Programm, das nicht innerhalb von `prepare_ms` bereit ist, vor der Bereitschaft endet oder dessen Festlegung nicht gilt, lässt die Vorbereitung seines Moduls fehlschlagen: Unter `fail-session` startet die Sitzung nicht (`OL_E_MODULE_SESSION_FAILED`); unter `disable-module` wird das Programm beendet und das Modul bleibt aus OMSI draußen; unter `degrade` bleibt das Modul erhalten (und das Programm läuft weiter, sofern es noch kann). Das `StartupTimeoutSeconds` der Sitzung zählt ab dem Start von OMSI, sodass die Vorbereitung es nicht aufbraucht.

<a id="conflicts-with-omsi-plugins"></a>
## Konflikte mit OMSI-Plugins

`conflicts.plugins` nennt OMSI-Plugin-DLLs, die nicht neben dem Modul laufen dürfen, zum Beispiel ein älteres Plugin, das dieselben Hooks installiert. Bei der Planung liest OmsiLaunch den Abschnitt `[dll]` jeder `plugins\*.opl`; lädt eine davon eine in Konflikt stehende DLL, wird das Modul mit `OL_E_MODULE_CONFLICT` abgelehnt, und die Begründung nennt die `.opl`. Innerhalb von OMSI wird ein Modul, dessen in Konflikt stehende DLL trotzdem geladen ist, nicht geladen. OmsiLaunch ändert nie die Dateien eines anderen Plugins; es abzuschalten ist die Entscheidung des Benutzers.

<a id="notices"></a>
## Hinweise

Ein Modulereignis mit dem Namen `notice` (aus `Emit("notice", ...)`, dem `emit` einer nativen Komponente oder der Hinweiszeile eines Sitzungsprogramms) wird vom OmsiLaunch-Tray als Ballon angezeigt, dessen Titel das Modul nennt. Nur `title` (höchstens 63 Zeichen) und `text` (höchstens 255 Zeichen) werden behalten, Steuerzeichen werden entfernt, und nichts darin wird geöffnet oder ausgeführt. Ein Sitzungsprogramm, das den Benutzer informieren muss, bevor OMSI startet (wenn der Tray noch nicht angezeigt wird), verwendet sein eigenes Fenster.

<a id="data-directory"></a>
## Datenverzeichnis

`<OMSI root>\.omsilaunch\module-data\<id>\` ist das beschreibbare Verzeichnis des Moduls für diese Installation: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` und das `data_directory` des Sitzungsprogramms. Es wird bei der ersten Verwendung erstellt. Es ist nicht Teil der Sitzungstransaktion: Was ein Modul dort schreibt, bleibt nach der Sitzung erhalten. `/clear-module-data:<id>` löscht es (und sonst nichts); es wird abgelehnt, solange eine Sitzung die Installation besitzt, und ein über einen Link erreichtes Verzeichnis wird als Link entfernt, nie verfolgt.

## Lifecycle

| Schritt | Wo | Was geschieht |
| --- | --- | --- |
| 1. Erkennung | Controller | `plugins\OmsiLaunch.Modules\*` wird gelesen: Manifest und Integrität (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Eignung | Controller, bei der Planung | Die Module, die [an der Sitzung teilnehmen](#choosing-modules-for-a-session), werden ausgewertet: verwendbar, Bridge-Protokoll, Capabilities, Inhaltsbindung, Plugin-Konflikte, Einstellungen. Das Ergebnis ist `SessionPlan.PlannedModules`. |
| 3. Vorbereitung | Controller, beim Start | Die [Sitzungsprogramme](#session-programs) der Module werden gestartet und abgewartet. Danach gelangen die geeigneten Module in der Start-Übergabe (Version 6) zum Plugin: ihre Verzeichnisse, der SHA-256 jeder ausgewerteten `module.json` und ihre effektiven Einstellungen. |
| 4. Arm | Plugin, bevor die Welt startet | Jedes Modul wird erneut geprüft und geladen (verwaltete Komponente in ihrem eigenen Load-Kontext). Seine native Komponente, falls vorhanden, wird zuerst geladen und gestartet, dann wird `Arm(context)` aufgerufen. |
| 5. Gameplay bereit | Plugin | Nach `gameplay.entered`, wenn ein geladenes Modul `bridge.hooks.d3d9` deklariert, beginnt die Bridge, das Direct3D-Gerät zu beobachten (`d3d.ready`); dann ruft sie `gameplay_ready` der nativen Komponente und `GameplayReady()` auf. Bei einer NEW_MAP-Sitzung wird die gebundene Karte erneut gegen die Karte geprüft, die der Start geladen hat. |
| 6. Runtime | Plugin | Operationen `module.<id>.<operation>` erreichen `Execute`. Ein Modul mit `tick_ms` erhält Ticks (siehe [Tick](#tick)); ein Modul mit `runtime_operations` führt diese im Prozess aus (siehe [Operationen im Prozess](#operations-in-process)). |
| 7. Stopp | Plugin, dann Controller | Wenn die Sitzung endet (oder das Modul fehlschlägt), wird `Stop()` aufgerufen. Nachdem OMSI beendet wurde, werden die Sitzungsprogramme gestoppt. |
| 8. Cleanup | Plugin | Alles, was das Modul nachverfolgt hat, wird in umgekehrter Reihenfolge freigegeben und als beseitigt verifiziert; die native Komponente wird zuletzt gestoppt und ihre Hooks werden entfernt; ihre DLL bleibt geladen und inert, bis OMSI beendet wird (ein Thread kann noch durch einen Detour zurückkehren). |

Jeder In-Process-Callback läuft auf dem UI-Thread von OMSI, wie die eigenen Runtime-Operationen des Produkts. Die Bridge misst jeden Callback gegen sein Budget: Ein Callback, der verspätet zurückkehrt, lässt seine Phase fehlschlagen (`OL_E_MODULE_PHASE_TIMEOUT`). Die Bridge kann einen Callback, der nie zurückkehrt, nicht unterbrechen; ein solches Modul hält OMSI an, und die Sitzung endet wie jede andere Sitzung mit ihrem Start-Timeout oder durch `session stop`.

<a id="failures"></a>
## Fehler

Ein Fehler ist eine Ausnahme oder eine Überschreitung in einer beliebigen Phase oder Operation, eine im Prozess nicht aufgelöste Capability, eine im Prozess nicht geltende Inhaltsbindung, ein in OMSI geladenes in Konflikt stehendes Plugin oder ein Sitzungsprogramm, das nicht bereit wird. Die `failure_policy` des Moduls entscheidet, was folgt. In jedem Fall werden die Ressourcen des Moduls freigegeben. Ein Tick, der eine Ausnahme auslöst, einen Wert ungleich null zurückgibt oder sein Intervall überschreitet, ist ein Fehler wie der einer Operation.

| Richtlinie | Fehler in Vorbereitung, Laden, Arm oder Gameplay bereit | Fehler in einer Operation | Fehler im Stopp |
| --- | --- | --- | --- |
| `fail-session` | Modul freigegeben; `module.session-failure`; die Sitzung schlägt mit `OL_E_MODULE_SESSION_FAILED` fehl, OMSI wird gestoppt und Dateien werden wiederhergestellt. Ein Fehler, bevor die Welt startet, verhindert den Start. | Ebenso. | Gemeldet (`module.failed`); die Sitzung läuft bis zu ihrem Ende weiter. |
| `disable-module` | Modul freigegeben und deaktiviert; die Sitzung läuft weiter. | Ebenso. | Gemeldet; Modul freigegeben. |
| `degrade` | Wie `disable-module`, außer dass der Fehler eines Sitzungsprogramms das Modul in der Sitzung belässt. | Der fehlschlagende Aufruf gibt seinen Code zurück; das Modul bleibt aktiv; `module.degraded` wird einmal pro Code gemeldet. | Gemeldet; Modul freigegeben. |

Ein deaktiviertes oder fehlgeschlagenes Modul beantwortet seine Operationen mit `OL_E_MODULE_DISABLED`. Modulfehler erscheinen als Sitzungsdiagnosen (`SessionStatus.Diagnostics`) mit ihrem `OL_E_MODULE_*`-Code und als Runtime-Ereignisse.

## Cleanup

Was ein Modul über seinen Kontext erwirbt, wird nachverfolgt: mit `Track` registrierte Ressourcen, mit `RunBackground` gestartete Threads, mit `StartAuxiliaryProcess` gestartete Prozesse. Beim Stopp oder Fehler bricht die Bridge `Stopping` ab und gibt sie dann in umgekehrter Reihenfolge frei. Hilfsprozesse werden zusätzlich in ein Job-Objekt mit Kill-on-close gelegt, sodass sie mit OMSI enden, selbst wenn OMSI beendet wird; der Job eines Sitzungsprogramms beendet es mit der OmsiLaunch-Sitzung, selbst wenn OmsiLaunch selbst beendet wird. Eine Ressource, die nach der Freigabe noch lebt (ein Prozess, der nicht beendet wurde, ein Thread, der nicht endete), wird als `OL_E_MODULE_CLEANUP_INCOMPLETE` gemeldet. Threads oder Prozesse, die ein Modul ohne seinen Kontext erstellt, werden nicht nachverfolgt.

Nach einem Absturz gibt es für Module nichts wiederherzustellen: Sie halten keine Installationsdateien, und ihr Speicher, ihre Threads und jobgebundenen Prozesse enden mit dem OMSI-Prozess oder dem OmsiLaunch-Prozess.

<a id="native-components-and-hooks"></a>
## Native Komponenten und Hooks

Eine native Komponente ist eine 32-Bit-DLL, die die C-Schnittstelle in `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h` implementiert:

| Export | Aufgerufen |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | Bei Arm, vor dem verwalteten `Arm`. Ein Wert ungleich null lässt die Phase fehlschlagen. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Optional; nachdem das Gameplay betreten wurde. |
| `void omsilaunch_module_native_v1_stop(void)` | Beim Stopp oder Fehler, bevor ihre Hooks entfernt werden. Danach verweigert die Bridge-Tabelle jeden Aufruf, und die DLL bleibt geladen und inert, bis OMSI beendet wird. |

Die Bridge-Tabelle gibt der Komponente `request_hook`, `emit` (Runtime-Ereignisse `module.<id>.<name>` mit einem JSON-Objekt aus Zeichenfolgen) und, ab Protokoll 2, `get_value`. Eine Komponente liest `size` der Tabelle, bevor sie `get_value` verwendet (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` kopiert einen Wert als UTF-8 mit abschließender Null und setzt `*length` auf die benötigten Bytes ohne diese; es gibt `0`, `1` (Argument), `2` (unbekannter Name), `11` (nach dem Stopp) oder `12` (Puffer zu klein) zurück. Ab Protokoll 3 enthält die Tabelle außerdem `run_operation` (siehe [Operationen im Prozess](#operations-in-process)) und `d3d_is_game_device(context, device)`: ob ein Geräte-Pointer, den ein Direct3D-Hook erhalten hat, das eigene Gerät des Spiels ist (`1`) oder ein anderes (`0`; das Fahrzeugauswahlformular erstellt ein zweites Gerät mit derselben Methodentabelle, sodass ein `Present`-Detour für beide aufgerufen wird und nur für das Gerät des Spiels zeichnen sollte), `10`, wenn noch kein Gerät beobachtet wird, `11`, wenn `bridge.hooks.d3d9` nicht deklariert ist oder nach dem Stopp. Der Pointer wird nur verglichen, nie verwendet, und die Bridge gibt nie einen heraus. Ein Modul, das Direct3D-Ressourcen im Default-Pool hält, hookt `d3d9:device.Reset` selbst (was es vor dem Aufruf des Originals tut, ist „Gerät verloren“, nachdem dieses Erfolg zurückgibt, „Gerät wiederhergestellt“). Eine Komponente prüft `size` der Tabelle, bevor sie ein Member von Protokoll 3 verwendet (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Zwei optionale Exporte vervollständigen die Komponente: `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (siehe [Tick](#tick)) und `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, über den die verwaltete Komponente die native über `IRuntimeModuleContext.NativeComponent.Execute` aufruft (JSON hinein, JSON heraus, dieselbe Pufferkonvention wie bei `run_operation`). Die Namen, die `get_value` beantwortet:

| Name | Wert |
| --- | --- |
| `setting.<key>` | Der effektive Wert einer deklarierten Einstellung. |
| `data_directory` | Das [Datenverzeichnis](#data-directory) des Moduls. |
| `module_directory` | Das eigene Verzeichnis des Moduls. |
| `session_id` | Die Sitzungs-ID. |
| `module_version` | Die Version des Manifests. |
| `product_version` | Die OmsiLaunch-Produktversion. |
| `session_channel` | Die Pipe des [Sitzungsprogramms](#session-programs); unbekannt für ein Modul ohne ein solches. |
| `omsi_version` | Die OMSI-Produktversion, die das Spiel über sich selbst meldet (Protokoll 3; siehe [Die OMSI-Version](#the-omsi-version)). |

Hooks werden nur über ein symbolisches Ziel angefordert, während `start` oder `gameplay_ready`, für ein Ziel, dessen Capability das Manifest deklariert:

| Ziel | Hookt | Capability |
| --- | --- | --- |
| `iat:<dll>!<function>` | Einen Import von `Omsi.exe`, zum Beispiel `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` oder `EndScene` des aktiven Direct3D-9-Geräts (in `gameplay_ready` anfordern, wenn das Gerät existiert). | `bridge.hooks.d3d9` |

`request_hook` gibt die Funktion zur Fortsetzung der Kette in `*original` zurück (ein Detour muss sie aufrufen). Sein Ergebnis: `0` Erfolg, `1` Argument, `2` unbekanntes Ziel, `3` die Stelle enthält nicht, was sie enthalten sollte, `4` die Stelle ist von jemandem außerhalb der Bridge gehookt, `5` Speicherschutz, `6` Registry voll, `7` Reihenfolge, `8` Speicher, `9` ein Thread führte die zu patchenden Bytes aus, `10` noch kein Direct3D-Gerät, `11` jetzt nicht erlaubt oder Capability nicht deklariert.

Jeder Hook, den die Bridge installiert, für das Produkt oder ein Modul, wird in einer Registry mit seinem Eigentümer festgehalten (`module:<id>` für ein Modul):

- Ein Hook wird nur über dem Wert installiert, den die Stelle enthalten sollte (die importierte Funktion; eine Funktion eines Moduls namens `d3d9.dll`, der Systemlaufzeit oder eines Wrappers), oder über dem obersten Hook der Registry selbst an dieser Stelle gestapelt. Eine Stelle, die bereits von jemand anderem geändert wurde, wird abgelehnt (`4`), nie überschrieben.
- Hooks an einer Stelle werden nach dem Last-in-first-out-Prinzip entfernt; ein Hook, über dem ein anderer liegt, bleibt, bis dieser entfernt ist.
- Der eigene Direct3D-`Reset`-Hook des Produkts befindet sich in der Registry, sodass Geräte-Hooks von Modulen darauf gestapelt werden.
- Beim Cleanup entfernt die Bridge jeden Hook des Moduls, nachdem `stop` zurückgekehrt ist. Kann einer nicht entfernt werden (jemand anderes hat die Stelle geändert), meldet das Cleanup `OL_E_MODULE_CLEANUP_INCOMPLETE`. Die DLL einer nativen Komponente wird nie entladen, solange OMSI läuft, sodass ein Detour, durch den noch zurückgekehrt wird, gültig bleibt.

Die Engine unterstützt auch Hooks auf Funktionsprologe mit einem Trampolin (andere Threads werden angehalten, während die Bytes geändert werden, und die genau erwarteten Bytes werden zuvor geprüft), aber Modulen wird noch kein Prologziel angeboten: Solche Ziele stammen aus dem Zielkatalog des Builds mit verifizierten Bytes.

<a id="operations"></a>
## Operationen

Eine Modul-Operation ist `module.<id>.<operation>`. Sie existiert nur für eine Sitzung, deren Plan das Modul geladen hat (`PlannedModule.Operations`); andernfalls antwortet `ExecuteRuntimeAsync` mit `OL_E_MODULE_NOT_LOADED` oder `OL_E_MODULE_OPERATION_UNKNOWN`. Erforderliche Argumente werden geprüft, und nicht deklarierte Argumente werden abgelehnt (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Ein typisierter Fehler eines Moduls behält seinen Code, wenn es ein katalogisierter öffentlicher Code ist; jeder andere Code wird zu `OL_E_MODULE_OPERATION_FAILED` mit dem Code des Moduls in `Values["module_code"]`. Ergebnisschlüssel, die der internen Namenskonvention folgen, werden entfernt, wie bei jeder Runtime-Operation.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## Hilfsprogramme

Ein Modul startet eine seiner `auxiliary`-Komponenten (eine festgelegte `.exe` aus seinem eigenen Verzeichnis) mit `StartAuxiliaryProcess`. Ab Protokoll 3 erstellt die Bridge das Programm **angehalten**, legt es in das Job-Objekt der Sitzung und setzt es erst dann fort, sodass ein Kindprozess, den es sofort startet, nicht außerhalb des Jobs laufen kann. Der Job beendet das Programm und alles, was es gestartet hat, wenn das Modul stoppt oder OMSI endet (Kill-on-close), und trägt `priority` (`below-normal`, `idle`) und `memory_limit_mb` des Manifests ab der ersten Anweisung des Programms. Anders als bei einem [Sitzungsprogramm](#session-programs) erlaubt der Job eines Hilfsprogramms Kindprozesse (ein Programm, das eine Web-View einbettet, braucht sie); jeder von ihnen befindet sich im Job. Vor dem Start wird das Programm erneut geprüft: seine Festlegung, wenn es eine hat, und immer der Snapshot des Plans.

<a id="operations-in-process"></a>
## Operationen im Prozess

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (verwaltet) und `run_operation` (nativ) führen eine Operation des [öffentlichen Capability-Katalogs](capabilities.md#public-runtime-operations) im Prozess aus, auf dem UI-Thread von OMSI, mit derselben Registry, denselben typisierten `OL_E_*`-Ergebnissen, denselben Argumentprüfungen und derselben Mutationsrichtlinie wie der Runtime-Steuerkanal, aber ohne den Kanal. Erlaubt sind nur die Operationen, die das Manifest in `runtime_operations` auflistet: Jede andere wird mit `OL_E_MODULE_OPERATION_NOT_DECLARED` beantwortet, bevor sie OMSI erreicht, und `internal.*`- und `module.*`-Operationen können nie aufgelistet werden. Eine Operation, deren native Capability der Build nicht auflöst, antwortet mit `OL_E_CAPABILITY_UNRESOLVED`, wie überall sonst.

Die Liste dient der Transparenz und der Planung, sie ist keine Sicherheitsgrenze: Ein Modul läuft bereits innerhalb von OMSI mit den Rechten des Benutzers. Der Plan zeigt sie (`PlannedModule.RuntimeOperations`), und eine Operation, die den Zustand von OMSI ändert, trägt dort ihre Mutationsrichtlinie, zum Beispiel `camera.set (omsi-state)`. Ein Modul kann ab `Arm` Operationen ausführen (die Welt existiert unter Umständen noch nicht; die Operation antwortet dann mit ihrem eigenen typisierten Fehler). Ergebnisse sind begrenzt (1 MiB); ein größeres Ergebnis ist `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Nachdem das Modul gestoppt wurde, antwortet jeder Aufruf mit `OL_E_MODULE_DISABLED`.

Das native `run_operation(context, operation, arguments_json, buffer, capacity, length)` nimmt die Katalog-ID und ein JSON-Objekt aus Zeichenfolgen (oder null) entgegen und schreibt `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. Es gibt `0` zurück, wenn es geantwortet hat (der eigene Fehler der Operation steht im JSON), `1` bei einem Argument, `11` nach dem Stopp und `12`, wenn der Puffer zu klein ist (`*length` ist die benötigte Größe).

## Tick

Ein Modul, das `tick_ms` und die Capability `bridge.tick` deklariert, wird etwa alle `tick_ms` Millisekunden aufgerufen, auf dem UI-Thread von OMSI, aus dem 50-ms-UI-Timer des Plugins: `ITickingModule.Tick(TimeSpan elapsed)` an der verwalteten Komponente und der optionale native Export `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. Das Intervall ist ein Vielfaches von 50 zwischen 50 und 1000. Ein Tick gilt nicht pro Frame: Ein Modul, das zeichnet, hookt die Direct3D-Methode `Present` und nutzt den Tick, um Daten zu lesen. Ticks beginnen nach `gameplay.entered` und enden mit dem Modul. Ein Tick, der eine Ausnahme auslöst, einen Wert ungleich null zurückgibt (nativ) oder länger als sein Intervall dauert, ist ein Fehler dieses Moduls: Unter `degrade` wird er einmal pro Code gemeldet (`module.degraded`) und das Modul läuft weiter (die verpassten Ticks werden übersprungen, nie in eine Warteschlange gestellt); unter `disable-module` und `fail-session` gilt die Richtlinie. Eine verschachtelte modale Schleife von OMSI lässt den UI-Timer weiterfeuern, sodass ein Tick nie in sich selbst eintritt.

<a id="community-content"></a>
## Community-Inhalte

`content_inputs` deklariert Inhalte, die das Modul liest und die sein Autor beim Erstellen des Moduls nicht kennen kann, weil die Community sie für eine Karte nach dem Modul erstellt. Es gibt keinen SHA-256 zum Festlegen; die Bridge lokalisiert sie nur und prüft, dass das Lesen sicher ist. Die einzige Art heute ist `realmap-package`: der Ordner `openomsi-realmap` einer Karte, `<OMSI root>\maps\<folder>\openomsi-realmap`, geschrieben vom Paket-Tooling für openOMSI-Karten.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` antwortet, ob die Karte ein Paket hat (keines zu haben ist kein Fehler) und, wenn ja, mit seinem Verzeichnis, der Dateianzahl, der Gesamtgröße und einem `Fingerprint`. Der Fingerprint ist ein Änderungsdetektor über relativen Pfad, Größe und Schreibzeit jeder Datei: Verwenden Sie ihn als Cache-Schlüssel; er ist nie eine Integritätsprüfung oder Vertrauensentscheidung. Das Paket muss innerhalb der Installation liegen, kein Teil davon darf ein Reparse-Punkt sein, und es ist begrenzt (50,000 Dateien, 4 GiB); andernfalls lautet die Antwort `OL_E_MODULE_CONTENT_INPUT_INVALID`. Ein Modul, das die Art nicht deklariert hat, erhält `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. Das Paket besteht aus Daten von Dritten: Lesen Sie es in verwaltetem Code, behandeln Sie jeden Wert als nicht vertrauenswürdig und weichen Sie ressourcenweise aus (Degrade). Eine Eingabe mit `required: true` lässt den Plan das Modul ablehnen (`OL_E_MODULE_CONTENT_BINDING`), wenn die Karte der Sitzung kein Paket hat.

<a id="input-capture"></a>
## Eingabeerfassung

Ein Modul, das das Pausenmenü von OMSI durch ein eigenes ersetzt, braucht die Tastatur und die Maus des Spiels. OMSI liest seine Simulationstasten über DirectInput und seine Maus über Fensternachrichten an das Ansichtsfenster des Hauptformulars; solange seine eigene Pause aktiv ist (das native Pausen-Byte), werden die Simulation, das Rendering, 3D-Klicks und DirectInput-Befehle von OMSI selbst angehalten. `IRuntimeModuleContext.Input` (Capability `bridge.input`, native Capability `ui.input.capture`) ergänzt den Rest: `Capture(true)` installiert einen thread-lokalen `WH_GETMESSAGE`-Hook auf dem UI-Thread, und die Tastatur- und Mausnachrichten, die zum Fensterbaum des Spiels gehören (das Hauptformular, seine Kinder und das verborgene native Pausenformular), werden als einfache `ModuleInputEvent`-Werte aufgezeichnet (Art, virtueller Schlüssel, Client-Koordinaten des Ansichtsfensters, Taste, Mausrad-Delta, Modifikatoren) und aus der Warteschlange entfernt, sodass weder OMSI noch die VCL darauf reagieren. `Drain()` gibt die Ereignisse zurück (höchstens 256 werden behalten). Nichts wird subclassed, keine user32-Tastenzustandsfunktion wird gehookt und nichts wird gesendet.

Ein Modul erfasst jeweils allein. Die Erfassung wird freigegeben, wenn das Modul stoppt oder fehlschlägt, was auch immer `Stop` getan hat. Native modale Dialoge (Speichern, Optionen, neues Spiel) brauchen echte Eingaben: Geben Sie die Erfassung frei, bevor Sie einen anfordern. Die Hook-Mechanik ist offline mit einem Stand-in-Fenster validiert (`ui-input.hook-self-check`); ihr Verhalten gegenüber dem echten Spielfenster wurde noch nicht beobachtet.

<a id="the-omsi-version"></a>
## Die OMSI-Version

`IRuntimeModuleContext.OmsiVersion` und `get_value("omsi_version")` liefern die OMSI-Produktversion, die das Spiel über sich selbst meldet, zum Beispiel `2.3.004`: den Text, den OMSI nach `Version:` in Zeile 7 seiner `logfile.txt` schreibt, gelesen aus dem laufenden Image. Sie ist **niemals** die PE-Dateiversion der ausführbaren Datei (der Referenz-Build trägt `FileVersion` `2.2.032`). Hat das Image keinen einzelnen solchen Text, wird die Version des Build-Profils verwendet, wenn die Code-Identität bekannt ist, andernfalls `unknown`. Der Hash der ausführbaren Datei, ihre Codegröße, Adressen und der Profilname werden nicht offengelegt. Verwenden Sie die Version für Texte und für die Wahl des Verhaltens; Capabilities (`IsCapabilityAvailable`) bleiben die maßgebliche Quelle dafür, was funktioniert.

<a id="writing-a-module-contracts"></a>
## Ein Modul schreiben (Verträge)

Die verwaltete Komponente referenziert `OmsiLaunch.Runtime.Modules` und `OmsiLaunch.Api` und implementiert `IRuntimeModule`. Die Bridge bindet diese beiden Referenzen an ihre eigenen geladenen Assemblies (in welchem Load-Kontext das Plugin auch läuft), sodass eine mit dem Modul ausgelieferte Kopie ignoriert wird:

| Member | Aufgerufen |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Schritt 4. Ressourcen über den Kontext erwerben. |
| `void GameplayReady()` | Schritt 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Schritt 6, für jede deklarierte Operation (ohne das Präfix `module.<id>.`). Für einen typisierten Fehler `ModuleOperationException(code, message)` auslösen. |
| `void Stop()` | Schritt 7. |

`IRuntimeModuleContext` bietet `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (ein Cancellation-Token), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess` und ab Protokoll 3 `Runtime` (siehe [Operationen im Prozess](#operations-in-process)), `Content` (siehe [Community-Inhalte](#community-content)), `Input` (siehe [Eingabeerfassung](#input-capture)) und `NativeComponent` (die geladene native Komponente oder null). Eine verwaltete Komponente, die Ticks möchte, implementiert zusätzlich `ITickingModule`. Nichts im Kontext legt Adressen, den Hash der ausführbaren Datei oder ihr Code-Layout offen. Die Komponente muss vom 32-Bit-Plugin ladbar sein (AnyCPU oder x86, .NET 6); eine Kopie von `OmsiLaunch.Runtime.Modules` oder `OmsiLaunch.Api` daneben wird ignoriert. Legen Sie jede Datei fest, die die Programme des Moduls laden (bei einem .NET-Programm: seine `.dll` neben der `.exe`), damit die Integritätsprüfung sie abdeckt.

<a id="known-limitations"></a>
## Bekannte Einschränkungen

- Native Komponenten können Importe von `Omsi.exe` und Methoden des Direct3D-Geräts hooken; Hooks auf Funktionsprologe und Aufrufstellen von OMSI werden Modulen noch nicht angeboten.
- Die Übernahme des Pausenmenüs (`ui.pause.*`) und die Eingabeerfassung sind statisch entworfen und offline validiert; sie wurden in einer echten OMSI-Sitzung nicht beobachtet, und `ui.pause-menu.replace` wird nur bei einer bekannten Code-Identität aufgelöst (einige seiner Felder haben keine buildunabhängige Herleitung).
- Die Prüfung der Inhaltsbindung im Prozess verwendet die Karte, die der Start angefordert hat (NEW_MAP); bei einer gespeicherten Situation wird die Bindung bei der Planung gegen die Karte der Situation geprüft.
- Geräte-Hooks sind vtable-Hooks. Wo eine andere Komponente die ursprünglichen Methoden des Geräts zurückschreibt (beobachtet auf der Regressionsinstallation kurz nach dem Start des Gameplays), wird ein Geräte-Hook nicht mehr aufgerufen und kann nicht entfernt werden; das Cleanup meldet dann `OL_E_MODULE_CLEANUP_INCOMPLETE`, und die Komponente bleibt geladen. Eine Geräte-Methode, die bereits von etwas außerhalb der Registry gehookt ist, wird abgelehnt (4); das Ablehnungsereignis nennt das Modul, dem die Methode gehört.
- Module stoppen bei einem normalen Beenden von OMSI; ein Beenden der Sitzung (`session stop`) terminiert OMSI, sodass das Modul-Cleanup innerhalb einer Sitzung nur über eine Fehlerrichtlinie erfolgt.
- Hinweise werden angezeigt, sobald der Tray existiert, nachdem OMSI gestartet wurde; ein Sitzungsprogramm zeigt sein eigenes Fenster für alles, was es davor mitteilen muss.
