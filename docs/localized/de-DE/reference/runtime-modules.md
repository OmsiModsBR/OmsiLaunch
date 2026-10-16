# Runtime-Module

<!-- l10n: source=reference/runtime-modules.md source-sha256=faf61b29436d1f69013b0c49667534ef079e31f7b34300b538935788654a55df -->
> Übersetzung der [englischen Originalseite](../../../reference/runtime-modules.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Ein Runtime-Modul ergänzt eine verwaltete OMSI-Sitzung um Verhalten: eine Funktion, die mit der Sitzung läuft (zum Beispiel das HUD einer Karte oder Ladeoptimierungen), ohne Teil von OmsiLaunch selbst zu werden. Die Bridge lädt es, gibt ihm nur das, was es deklariert, und entfernt alles, was es hält, wenn die Sitzung endet oder das Modul fehlschlägt. Diese Seite ist die normative Referenz für Runtime-Module, wie sie von `src/OmsiLaunch.Runtime.Modules` (Manifest, Einstellungen, Eignung, Lifecycle-Host), dem Planer, den Sitzungsprogrammen des Controllers (`ModuleSessionPrograms`, `ModuleSessionProcess`), der Modulumgebung des Plugins und den Modul-Flags der [CLI](cli.md) umgesetzt werden.

Stabilität: `EXPERIMENTAL`. Bridge-Protokoll 1 wurde in Runde B zur Laufzeit validiert (verwaltete Module, Inhaltsaktivierung, Operationen, `disable-module` und `fail-session`; native Komponenten mit IAT-Hooks; siehe [Status der Runtime-Validierung](../status/runtime-validation-status.md)). Bridge-Protokoll 2 (Einstellungen, Standardaktivierung, die Einstellungsdatei, Sitzungsprogramme, Plugin-Konflikte, Hinweise, `get_value`) ist nur offline validiert. Offline-Gates: die `module.*`-Gates von `tests/OmsiLaunch.ModuleTests` (Manifest, Integrität, Auswahl und Aktivierung, Einstellungen, die Einstellungsdatei, Plugin-Konflikte, über ihren Job gestartete Sitzungsprogramme, ein in jeder Phase unter jeder Fehlerrichtlinie injizierter Fehler, Lifecycle der nativen Komponente, Cleanup, Übergabe), `plugin-runtime.modules-lifecycle` (ein echtes verwaltetes Modul, das von der Umgebung des Plugins geladen wird), `runtime-modules.native-component` (eine echte native Komponente, die einen Import hookt und Werte über die Bridge liest), `runtime-modules.plugin-conflict` und `native-hooks.self-check` (die Hook-Engine).

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
| `bridge_protocol` | ja | `{ "min": n, "max": n }`: die Bridge-Protokolle, mit denen das Modul arbeitet. Dieser Build bedient die Protokolle `1` und `2`; ein Modul wird akzeptiert, wenn sein Bereich eines davon enthält. |
| `activation` | nein | `requested` (Standard): wird geladen, wenn die Sitzung es anfordert. `content`: wird zusätzlich geladen, wenn die Sitzung die gebundene Karte ausführt (benötigt `content_binding`). `default` (Protokoll 2): wird in jeder Sitzung geladen, sofern es nicht deaktiviert ist (siehe [Auswahl der Module für eine Sitzung](#choosing-modules-for-a-session)); es darf kein `content_binding` haben. |
| `required_capabilities` | nein | Capabilities, ohne die das Modul nicht arbeiten kann (siehe [Capabilities](#capabilities)). |
| `optional_capabilities` | nein | Capabilities, die das Modul nutzt, wenn sie verfügbar sind; es prüft sie mit `IsCapabilityAvailable`. |
| `content_binding` | nein | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: das Modul läuft nur auf dieser Karte, mit diesen unveränderten Dateien. |
| `assets` | nein | Dateien, die das Modul liest, jeweils `{ "path", "sha256" }`. |
| `components` | ja | Genau eine `managed`-Komponente `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; optional eine `native`-Komponente (`.dll`), `auxiliary`-Komponenten (`.exe`) und, mit Protokoll 2, eine `host`-Komponente (`.exe`, das [Sitzungsprogramm](#session-programs) des Moduls). |
| `failure_policy` | ja | `fail-session`, `disable-module` oder `degrade` (siehe [Fehler](#failures)). |
| `operations` | nein | Operationen, die das Modul bereitstellt, während die Sitzung läuft: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | nein | Protokoll 2. Die Einstellungen, die das Modul akzeptiert (siehe [Einstellungen](#settings)). |
| `conflicts` | nein | Protokoll 2. `{ "plugins": [ "<file>.dll" ] }`: OMSI-Plugin-DLLs, die nicht neben dem Modul laufen dürfen (siehe [Konflikte mit OMSI-Plugins](#conflicts-with-omsi-plugins)). |
| `timeouts` | nein | Budgets in Millisekunden: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (jeweils Standard 5000, höchstens 30000); mit Protokoll 2 außerdem `prepare_ms` (Standard 120000, höchstens 1800000) und `host_stop_ms` (Standard 10000, höchstens 60000) für das Sitzungsprogramm. Operationen haben standardmäßig 2000 ms. |
| `diagnostics` | nein | `{ "trace": true\|false }`. |

Ein Manifest, das eine Funktion von Protokoll 2 verwendet (`settings`, `conflicts`, Aktivierung `default`, eine `host`-Komponente, `prepare_ms` oder `host_stop_ms`), muss `bridge_protocol.min` 2 deklarieren.

Jede von `components` und `assets` genannte Datei ist durch ihren SHA-256 festgelegt. `module.json` kann sich nicht selbst festlegen.

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

Jede festgelegte Datei muss existieren, darf nicht über eine Junction oder einen symbolischen Link erreicht werden und muss ihren festgelegten SHA-256 haben; andernfalls wird das Modul mit `OL_E_MODULE_INTEGRITY` abgelehnt. Das erkennt ein Modul, das beschädigt oder nur halb aktualisiert ist. Es ist keine Vertrauensentscheidung: Ein Modul wird weder gegen eine Liste noch gegen eine Signatur oder einen Herausgeber geprüft.

Die Prüfung wird dort wiederholt, wo der Code des Moduls startet: Das Plugin lädt ein Modul nur, wenn seine `module.json` noch den SHA-256 hat, den der Plan ausgewertet hat, und jede Festlegung weiterhin gilt, und der Controller startet ein Sitzungsprogramm erst, nachdem er die Festlegung dieses Programms geprüft hat.

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
| 6. Runtime | Plugin | Operationen `module.<id>.<operation>` erreichen `Execute`. |
| 7. Stopp | Plugin, dann Controller | Wenn die Sitzung endet (oder das Modul fehlschlägt), wird `Stop()` aufgerufen. Nachdem OMSI beendet wurde, werden die Sitzungsprogramme gestoppt. |
| 8. Cleanup | Plugin | Alles, was das Modul nachverfolgt hat, wird in umgekehrter Reihenfolge freigegeben und als beseitigt verifiziert; die native Komponente wird zuletzt gestoppt und ihre Hooks werden entfernt; ihre DLL bleibt geladen und inert, bis OMSI beendet wird (ein Thread kann noch durch einen Detour zurückkehren). |

Jeder In-Process-Callback läuft auf dem UI-Thread von OMSI, wie die eigenen Runtime-Operationen des Produkts. Die Bridge misst jeden Callback gegen sein Budget: Ein Callback, der verspätet zurückkehrt, lässt seine Phase fehlschlagen (`OL_E_MODULE_PHASE_TIMEOUT`). Die Bridge kann einen Callback, der nie zurückkehrt, nicht unterbrechen; ein solches Modul hält OMSI an, und die Sitzung endet wie jede andere Sitzung mit ihrem Start-Timeout oder durch `session stop`.

<a id="failures"></a>
## Fehler

Ein Fehler ist eine Ausnahme oder eine Überschreitung in einer beliebigen Phase oder Operation, eine im Prozess nicht aufgelöste Capability, eine im Prozess nicht geltende Inhaltsbindung, ein in OMSI geladenes in Konflikt stehendes Plugin oder ein Sitzungsprogramm, das nicht bereit wird. Die `failure_policy` des Moduls entscheidet, was folgt. In jedem Fall werden die Ressourcen des Moduls freigegeben.

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

Die Bridge-Tabelle gibt der Komponente `request_hook`, `emit` (Runtime-Ereignisse `module.<id>.<name>` mit einem JSON-Objekt aus Zeichenfolgen) und, ab Protokoll 2, `get_value`. Eine Komponente liest `size` der Tabelle, bevor sie `get_value` verwendet (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` kopiert einen Wert als UTF-8 mit abschließender Null und setzt `*length` auf die benötigten Bytes ohne diese; es gibt `0`, `1` (Argument), `2` (unbekannter Name), `11` (nach dem Stopp) oder `12` (Puffer zu klein) zurück. Namen:

| Name | Wert |
| --- | --- |
| `setting.<key>` | Der effektive Wert einer deklarierten Einstellung. |
| `data_directory` | Das [Datenverzeichnis](#data-directory) des Moduls. |
| `module_directory` | Das eigene Verzeichnis des Moduls. |
| `session_id` | Die Sitzungs-ID. |
| `module_version` | Die Version des Manifests. |
| `product_version` | Die OmsiLaunch-Produktversion. |
| `session_channel` | Die Pipe des [Sitzungsprogramms](#session-programs); unbekannt für ein Modul ohne ein solches. |

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

<a id="writing-a-module-contracts"></a>
## Ein Modul schreiben (Verträge)

Die verwaltete Komponente referenziert `OmsiLaunch.Runtime.Modules` und `OmsiLaunch.Api` und implementiert `IRuntimeModule`. Die Bridge bindet diese beiden Referenzen an ihre eigenen geladenen Assemblies (in welchem Load-Kontext das Plugin auch läuft), sodass eine mit dem Modul ausgelieferte Kopie ignoriert wird:

| Member | Aufgerufen |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Schritt 4. Ressourcen über den Kontext erwerben. |
| `void GameplayReady()` | Schritt 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Schritt 6, für jede deklarierte Operation (ohne das Präfix `module.<id>.`). Für einen typisierten Fehler `ModuleOperationException(code, message)` auslösen. |
| `void Stop()` | Schritt 7. |

`IRuntimeModuleContext` bietet `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `SessionChannelName`, `Stopping` (ein Cancellation-Token), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground` und `StartAuxiliaryProcess`. Nichts darin legt Adressen, die ausführbare Datei oder ihre Identität offen. Die Komponente muss vom 32-Bit-Plugin ladbar sein (AnyCPU oder x86, .NET 6). Legen Sie jede Datei fest, die die Programme des Moduls laden (bei einem .NET-Programm: seine `.dll` neben der `.exe`), damit die Integritätsprüfung sie abdeckt.

<a id="known-limitations"></a>
## Bekannte Einschränkungen

- Native Komponenten können Importe von `Omsi.exe` und Methoden des Direct3D-Geräts hooken; Hooks auf Funktionsprologe und Aufrufstellen von OMSI werden Modulen noch nicht angeboten.
- Die Prüfung der Inhaltsbindung im Prozess verwendet die Karte, die der Start angefordert hat (NEW_MAP); bei einer gespeicherten Situation wird die Bindung bei der Planung gegen die Karte der Situation geprüft.
- Geräte-Hooks sind vtable-Hooks. Wo eine andere Komponente die ursprünglichen Methoden des Geräts zurückschreibt (beobachtet auf der Regressionsinstallation kurz nach dem Start des Gameplays), wird ein Geräte-Hook nicht mehr aufgerufen und kann nicht entfernt werden; das Cleanup meldet dann `OL_E_MODULE_CLEANUP_INCOMPLETE`, und die Komponente bleibt geladen. Eine Geräte-Methode, die bereits von etwas außerhalb der Registry gehookt ist, wird abgelehnt (4); das Ablehnungsereignis nennt das Modul, dem die Methode gehört.
- Module stoppen bei einem normalen Beenden von OMSI; ein Beenden der Sitzung (`session stop`) terminiert OMSI, sodass das Modul-Cleanup innerhalb einer Sitzung nur über eine Fehlerrichtlinie erfolgt.
- Hinweise werden angezeigt, sobald der Tray existiert, nachdem OMSI gestartet wurde; ein Sitzungsprogramm zeigt sein eigenes Fenster für alles, was es davor mitteilen muss.
