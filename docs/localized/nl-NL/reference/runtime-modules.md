# Runtimemodules

<!-- l10n: source=reference/runtime-modules.md source-sha256=faf61b29436d1f69013b0c49667534ef079e31f7b34300b538935788654a55df -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/runtime-modules.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Een runtimemodule voegt gedrag toe aan een beheerde OMSI-sessie: een functie die met de sessie meedraait (bijvoorbeeld de HUD van een kaart of laadoptimalisaties), zonder deel van OmsiLaunch zelf te worden. De brug laadt de module, geeft hem alleen wat hij declareert en verwijdert alles wat hij vasthoudt wanneer de sessie eindigt of de module faalt. Deze pagina is de normatieve referentie voor runtimemodules zoals geïmplementeerd door `src/OmsiLaunch.Runtime.Modules` (manifest, instellingen, geschiktheid, levenscyclushost), de planner, de sessieprogramma's van de controller (`ModuleSessionPrograms`, `ModuleSessionProcess`), de moduleomgeving van de plugin en de modulevlaggen van de [CLI](cli.md).

Stabiliteit: `EXPERIMENTAL`. Brugprotocol 1 is in ronde B runtime-gevalideerd (beheerde modules, contentactivering, operaties, `disable-module` en `fail-session`; native componenten met IAT-hooks; zie de [status van de runtimevalidatie](../status/runtime-validation-status.md)). Brugprotocol 2 (instellingen, standaardactivering, het instellingenbestand, sessieprogramma's, pluginconflicten, meldingen, `get_value`) is alleen offline gevalideerd. Offline gates: de `module.*`-gates van `tests/OmsiLaunch.ModuleTests` (manifest, integriteit, selectie en activering, instellingen, het instellingenbestand, pluginconflicten, sessieprogramma's gestart via hun job, een in elke fase geïnjecteerde fout onder elk faalbeleid, levenscyclus van native componenten, opruimen, handoff), `plugin-runtime.modules-lifecycle` (een echte beheerde module geladen door de omgeving van de plugin), `runtime-modules.native-component` (een echte native component die een import hookt en waarden leest via de brug), `runtime-modules.plugin-conflict` en `native-hooks.self-check` (de hook-engine).

Laden is open: elke module die onder `plugins\OmsiLaunch.Modules` is geïnstalleerd kan worden geladen, door wie hij ook is gemaakt. OmsiLaunch staat niet in voor wat een module binnen OMSI doet. Wat het wel garandeert is de [grens](#security-boundary): modules draaien nooit binnen het OmsiLaunch-proces, en niets wat OmsiLaunch biedt laat een module verder reiken dan de eigen rechten van OMSI.

<a id="where-modules-live"></a>
## Waar modules staan

Elke module is een map onder de permanente pluginmap:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

De mapnaam moet gelijk zijn aan de `id` van de module. Modules worden geïnstalleerd door wie ze distribueert; een sessie schrijft, maakt geen snapshot van en herstelt daar nooit bestanden, en modules wijzigen via OmsiLaunch nooit installatiebestanden (een manifest kent geen manier om een installatie-overlay te declareren). De schrijfbare gegevens van een module staan in zijn [gegevensmap](#data-directory).

## Manifest (`module.json`)

Schema `omsilaunch.runtime-module/v1`. Onbekende sleutels worden geweigerd, zodat een manifest dat voor een later schema is geschreven wordt geweigerd in plaats van half begrepen. Alle paden zijn relatief ten opzichte van de modulemap (gebonden contentpaden zijn relatief ten opzichte van de OMSI-hoofdmap), gebruiken `\` of `/` en mogen geen `..` of stationsaanduiding bevatten.

| Sleutel | Vereist | Betekenis |
| --- | --- | --- |
| `schema` | ja | `omsilaunch.runtime-module/v1`. |
| `id` | ja | Module-id: kleine letters, cijfers en `-`, 2 tot 48 tekens, beginnend met een letter of cijfer. Tevens de mapnaam en de operatienaamruimte van de module. |
| `version` | ja | `major.minor.patch`, optioneel `-suffix`. |
| `name` | nee | Weergavenaam (maximaal 128 tekens). |
| `bridge_protocol` | ja | `{ "min": n, "max": n }`: de brugprotocollen waarmee de module werkt. Deze build ondersteunt protocollen `1` en `2`; een module wordt geaccepteerd wanneer zijn bereik een van beide bevat. |
| `activation` | nee | `requested` (standaard): geladen wanneer de sessie erom vraagt. `content`: ook geladen wanneer de sessie de gebonden kaart uitvoert (vereist `content_binding`). `default` (protocol 2): geladen in elke sessie tenzij hij is uitgeschakeld (zie [modules kiezen](#choosing-modules-for-a-session)); hij kan geen `content_binding` hebben. |
| `required_capabilities` | nee | Capabilities waarzonder de module niet kan werken (zie [capabilities](#capabilities)). |
| `optional_capabilities` | nee | Capabilities die de module gebruikt wanneer ze beschikbaar zijn; hij controleert ze met `IsCapabilityAvailable`. |
| `content_binding` | nee | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: de module draait alleen op die kaart, met die bestanden ongewijzigd. |
| `assets` | nee | Bestanden die de module leest, elk `{ "path", "sha256" }`. |
| `components` | ja | Precies één `managed`-component `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; optioneel één `native`-component (`.dll`), `auxiliary`-componenten (`.exe`) en, met protocol 2, één `host`-component (`.exe`, het [sessieprogramma](#session-programs) van de module). |
| `failure_policy` | ja | `fail-session`, `disable-module` of `degrade` (zie [fouten](#failures)). |
| `operations` | nee | Operaties die de module beschikbaar stelt terwijl de sessie draait: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | nee | Protocol 2. De instellingen die de module accepteert (zie [instellingen](#settings)). |
| `conflicts` | nee | Protocol 2. `{ "plugins": [ "<file>.dll" ] }`: OMSI-plugin-DLL's die niet naast de module mogen draaien (zie [conflicten](#conflicts-with-omsi-plugins)). |
| `timeouts` | nee | Budgetten in milliseconden: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (elk standaard 5000, maximaal 30000); met protocol 2 ook `prepare_ms` (standaard 120000, maximaal 1800000) en `host_stop_ms` (standaard 10000, maximaal 60000) voor het sessieprogramma. Operaties hebben standaard 2000 ms. |
| `diagnostics` | nee | `{ "trace": true\|false }`. |

Een manifest dat een functie van protocol 2 gebruikt (`settings`, `conflicts`, activering `default`, een `host`-component, `prepare_ms` of `host_stop_ms`) moet `bridge_protocol.min` 2 declareren.

Elk bestand dat door `components` en `assets` wordt genoemd, is vastgezet met zijn SHA-256. `module.json` kan zichzelf niet vastzetten.

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
## Integriteit

Elk vastgezet bestand moet bestaan, mag niet via een junction of symbolische koppeling worden bereikt en moet zijn vastgezette SHA-256 hebben; anders wordt de module geweigerd met `OL_E_MODULE_INTEGRITY`. Dit vangt een module op die beschadigd of half bijgewerkt is. Het is geen vertrouwensbeslissing: een module wordt niet getoetst aan een lijst, handtekening of uitgever.

De controle wordt herhaald op de plek waar de code van de module start: de plugin laadt een module alleen als zijn `module.json` nog steeds de SHA-256 heeft die het plan heeft geëvalueerd en elke vastgezette waarde nog klopt, en de controller start een sessieprogramma pas nadat de vastzetting van dat programma is geverifieerd.

<a id="security-boundary"></a>
## Beveiligingsgrens

OmsiLaunch heeft geen controle over wat een module binnen OMSI doet: zijn beheerde en native componenten draaien in het OMSI-proces met dezelfde rechten als elke OMSI-plugin. OmsiLaunch garandeert het volgende:

1. Het OmsiLaunch-proces laadt nooit modulecode. Modulecode draait binnen OMSI, in een hulpprogramma dat vanuit OMSI wordt gestart, of in het sessieprogramma van de module.
2. De programma's die OmsiLaunch voor modules start, zijn bestanden uit de eigen map van de module, geverifieerd tegen hun vastzetting en gestart met een opdrachtregel die OmsiLaunch vastlegt. Een sessieprogramma draait in een jobobject dat het met de sessie beëindigt, staat geen tweede proces toe (het kan geen andere programma's starten) en weigert wijzigingen aan systeemparameters, weergave-instellingen, het bureaublad en afmelden of afsluiten. Het erft alleen zijn twee pipe-handles.
3. Alles wat een module declareert blijft binnen zijn map, en zijn schrijfbare gegevens binnen `.omsilaunch\module-data\<id>`.
4. De OmsiLaunch-kanalen (local control, opstart-handoff, telemetrie, runtimeopdrachten) blijven beperkt tot de huidige gebruiker en gebonden aan de sessie; geen ervan laat een aanroeper een programma starten, bestanden buiten de installatie lezen of schrijven, of systeeminstellingen wijzigen.
5. OmsiLaunch verhoogt nooit rechten. Een module draait met de rechten van de gebruiker die de sessie heeft gestart.
6. Tekst die van een module komt (meldingen, events, waarden) wordt als gegevens behandeld: begrensd, zonder besturingstekens, nooit uitgevoerd, geopend of als koppeling gevolgd.

## Capabilities

`required_capabilities` en `optional_capabilities` noemen ofwel een native capability van de draaiende build (de kolom native capability van de [capabilitycatalogus](capabilities.md#capability-catalog), bijvoorbeeld `time.read` of `d3d.texture`) ofwel een brugfunctie:

| Brugfunctie | Wat de module krijgt |
| --- | --- |
| `bridge.events` | `Emit`: runtime-events `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: bijgehouden achtergrondthreads. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: hulpprocessen die aan de sessie zijn gebonden. |
| `bridge.hooks.iat` | Native component: hooks op imports van `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Native component: hooks op methoden van het actieve Direct3D 9-apparaat (`d3d9:device.<method>`). Vereist de native capability `d3d.texture`. |

Een naam die geen van beide is, wordt bij het plannen geweigerd (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). Op een OMSI 2-build zonder bekende layout wordt een native capability die het uitvoerbare bestand niet oplost, in het proces beslist; als ze daar niet wordt opgelost, wordt de module niet geladen.

<a id="choosing-modules-for-a-session"></a>
## Modules kiezen voor een sessie

Een module neemt op een van vier manieren deel aan een sessie; `PlannedModule.Activation` geeft aan welke:

| Activering | Wanneer |
| --- | --- |
| `requested` | De sessie vraagt erom: `LaunchSpec.Modules.Enable`, `/module:<id>` of `enabled: true` in de geselecteerde preset van het sessieprofiel. |
| `settings-file` | `enabled=true` in zijn sectie van het [instellingenbestand](#the-settings-file). |
| `content` | Zijn activering is `content` en de sessie voert zijn gebonden kaart uit. |
| `default` | Zijn activering is `default`. |

Een module wordt buiten de sessie gehouden door `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` in de preset of `enabled=false` in het instellingenbestand (plandiagnose `module.disabled`). Een expliciete aanvraag gaat voor het instellingenbestand. Dezelfde module in één aanvraag zowel inschakelen als uitschakelen is `OL_E_MODULE_SETTING_INVALID`.

Planregels: een aangevraagde module die niet kan worden geladen, maakt het plan niet uitvoerbaar met zijn `OL_E_MODULE_*`-code. Een module die uit zichzelf deelnam (instellingenbestand, content, standaard) en niet kan worden geladen, stopt de sessie niet: het plan meldt `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Instellingen

Een module declareert zijn instellingen in `settings`; elke invoer:

| Sleutel | Vereist | Betekenis |
| --- | --- | --- |
| `key` | ja | `[a-z][a-z0-9_]{0,31}`, niet `enabled`. |
| `type` | ja | `boolean`, `integer`, `choice` of `text`. |
| `default` | ja | De waarde wanneer niets anders haar instelt: een boolean, een geheel getal, een tekenreeks, of `"auto"` wanneer `auto` true is. |
| `auto` | nee | `true`: de waarde `auto` wordt geaccepteerd en aan de module doorgegeven, die tijdens de uitvoering beslist. |
| `min`, `max` | nee | Gehele grenzen. |
| `choices` | voor `choice` | De geaccepteerde waarden (`[a-z0-9][a-z0-9_.-]{0,31}`, niet `auto`). |
| `max_length` | nee | Limiet voor tekstlengte (1..4096, standaard 256). Besturingstekens worden geweigerd. |
| `flag` | nee | Een opdrachtregelvlag voor deze instelling, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, of `/<flag>` alleen voor `true` bij een boolean. |
| `description` | nee | Maximaal 256 tekens. |

Waarden worden genormaliseerd: booleans accepteren `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; keuzes worden zonder hoofdlettergevoeligheid vergeleken; gehele getallen worden aan hun grenzen getoetst.

De effectieve waarde van elke instelling is, in volgorde:

1. de aanvraag: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, een eigen vlag van de module, of `modules.<id>.settings` in de geselecteerde preset van het sessieprofiel. Zoals bij elk ander presetveld kan de opdrachtregel niet ook een sleutel instellen die de preset instelt (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. het [instellingenbestand](#the-settings-file);
3. de standaardwaarde uit het manifest.

Een ongeldige aangevraagde waarde, of een aangevraagde sleutel die de module niet declareert, maakt het plan niet uitvoerbaar (`OL_E_MODULE_SETTING_INVALID`). Een invoer in het instellingenbestand die niet kan worden gebruikt, wordt genegeerd met `OL_W_MODULE_SETTING_IGNORED`, evenals aangevraagde instellingen voor een module die de sessie niet laadt. Het plan toont elke effectieve waarde met haar bron (`request`, `settings-file` of `default`) in `PlannedModule.Settings`.

De module ontvangt elke gedeclareerde sleutel met haar effectieve waarde: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` in een native component, en het `settings`-object van de context van een sessieprogramma.

<a id="the-settings-file"></a>
### Het instellingenbestand

`<OMSI root>\.omsilaunch\modules.ini`, naast de sessieprofielen. OmsiLaunch leest het alleen.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Secties zijn module-id's; regels `key=value` stellen instellingen in, `enabled` schakelt de module voor elke sessie in of uit; regels die met `;` of `#` beginnen zijn commentaar. Het bestand mag maximaal 256 KiB groot zijn. Alles wat niet bruikbaar is (een sectie die geen module-id is, een regel buiten een sectie, een onbekende sleutel, een ongeldige waarde) wordt gemeld met `OL_W_MODULE_SETTING_IGNORED` en genegeerd.

<a id="session-programs"></a>
## Sessieprogramma's

Een module mag één `host`-component declareren: zijn sessieprogramma, een uitvoerbaar bestand uit zijn eigen map. Wanneer de module in het plan staat, doet de controller het volgende:

1. verifieert de vastzetting van het programma en maakt de gegevensmap van de module aan;
2. start het vóór OMSI, vanuit de modulemap, met de opdrachtregel `"<program>" --omsilaunch-session <session id>`, opgeschort totdat het in zijn job zit (zie de [beveiligingsgrens](#security-boundary));
3. schrijft de context als eerste regel op de standaardinvoer ervan;
4. wacht tot het programma `{"type":"ready"}` schrijft, hoogstens `prepare_ms`; pas daarna start het OMSI;
5. schrijft nadat OMSI is aangemaakt `{"type":"omsi-started","pid":<process id>}`;
6. schrijft aan het einde van de sessie, nadat OMSI is afgesloten en voordat een bestand wordt hersteld, `{"type":"stop"}`, wacht hoogstens `host_stop_ms` en beëindigt dan het programma.

Beide richtingen gebruiken UTF-8-JSON, één object per regel. De context:

| Veld | Betekenis |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identiteit. |
| `installation_root`, `module_directory`, `data_directory` | Volledige paden. |
| `channel` | De named pipe die het programma mag aanbieden voor de componenten van de module in OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. Het programma maakt haar aan, beperkt tot de huidige gebruiker, en controleert dat de client het proces van OMSI is. Componenten lezen haar met `get_value("session_channel")` of `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | De effectieve instellingen van de module. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` van de sessie. |
| `session_files` | Installatierelatieve bestanden die de sessietransactie bezit. Het programma mag ze niet geopend of gemapt houden: ze worden hersteld wanneer de sessie eindigt. |

Wat het programma behalve `ready` mag schrijven:

| Regel | Effect |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Runtime-event `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Runtime-event `module.<id>.notice` (zie [meldingen](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Sessiediagnose met die gecatalogiseerde code (anders `OL_E_MODULE_OPERATION_FAILED`). |

Andere regels, regels langer dan 64 KiB en alles na 10.000 regels worden genegeerd. De controller registreert `module.<id>.host.started`, `host.ready` (met `prepare_ms`), `host.failed`, `host.exited` en `host.killed`.

Een programma dat niet binnen `prepare_ms` gereed is, afsluit voordat het gereed is, of waarvan de vastzetting niet klopt, laat de voorbereiding van zijn module mislukken: onder `fail-session` start de sessie niet (`OL_E_MODULE_SESSION_FAILED`); onder `disable-module` wordt het programma beëindigd en blijft de module buiten OMSI; onder `degrade` blijft de module (en blijft het programma doordraaien als dat nog kan). De `StartupTimeoutSeconds` van de sessie telt vanaf het moment dat OMSI wordt gestart, dus de voorbereiding verbruikt hem niet.

<a id="conflicts-with-omsi-plugins"></a>
## Conflicten met OMSI-plugins

`conflicts.plugins` noemt OMSI-plugin-DLL's die niet naast de module mogen draaien, bijvoorbeeld een oudere plugin die dezelfde hooks installeert. Bij het plannen leest OmsiLaunch de sectie `[dll]` van elke `plugins\*.opl`; als er een een conflicterende DLL laadt, wordt de module geweigerd met `OL_E_MODULE_CONFLICT` en noemt de reden de `.opl`. Binnen OMSI wordt een module waarvan de conflicterende DLL toch is geladen, niet geladen. OmsiLaunch wijzigt nooit de bestanden van een andere plugin; die uitschakelen is de beslissing van de gebruiker.

<a id="notices"></a>
## Meldingen

Een module-event met de naam `notice` (van `Emit("notice", ...)`, de `emit` van een native component of de meldingsregel van een sessieprogramma) wordt door het OmsiLaunch-systeemvakpictogram getoond als een ballonmelding waarvan de titel de module noemt. Alleen `title` (maximaal 63 tekens) en `text` (maximaal 255 tekens) blijven behouden, besturingstekens worden verwijderd en niets erin wordt geopend of uitgevoerd. Een sessieprogramma dat de gebruiker moet informeren voordat OMSI start (wanneer het systeemvakpictogram nog niet getoond wordt), gebruikt zijn eigen venster.

<a id="data-directory"></a>
## Gegevensmap

`<OMSI root>\.omsilaunch\module-data\<id>\` is de schrijfbare map van de module voor deze installatie: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` en de `data_directory` van het sessieprogramma. Ze wordt bij het eerste gebruik aangemaakt. Ze maakt geen deel uit van de sessietransactie: wat een module daar schrijft blijft na de sessie bestaan. `/clear-module-data:<id>` verwijdert haar (en niets anders); het wordt geweigerd zolang een sessie de installatie in bezit heeft, en een map die via een koppeling wordt bereikt, wordt als koppeling verwijderd en nooit gevolgd.

<a id="lifecycle"></a>
## Levenscyclus

| Stap | Waar | Wat er gebeurt |
| --- | --- | --- |
| 1. Ontdekking | controller | `plugins\OmsiLaunch.Modules\*` wordt gelezen: manifest en integriteit (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Geschiktheid | controller, bij het plannen | De modules die [aan de sessie deelnemen](#choosing-modules-for-a-session) worden geëvalueerd: bruikbaar, brugprotocol, capabilities, contentbinding, pluginconflicten, instellingen. Het resultaat is `SessionPlan.PlannedModules`. |
| 3. Voorbereiden | controller, bij de start | De [sessieprogramma's](#session-programs) van de modules worden gestart en afgewacht. Daarna reizen de geschikte modules in de opstart-handoff (versie 6) naar de plugin: hun mappen, de SHA-256 van elke geëvalueerde `module.json` en hun effectieve instellingen. |
| 4. Arm | plugin, voordat de wereld start | Elke module wordt opnieuw geverifieerd en geladen (beheerde component in een eigen laadcontext). Zijn native component, indien aanwezig, wordt eerst geladen en gestart, daarna wordt `Arm(context)` aangeroepen. |
| 5. Gameplay gereed | plugin | Na `gameplay.entered`, wanneer een geladen module `bridge.hooks.d3d9` declareert, begint de brug het Direct3D-apparaat te observeren (`d3d.ready`); daarna roept ze `gameplay_ready` van de native component en `GameplayReady()` aan. Voor een NEW_MAP-sessie wordt de gebonden kaart opnieuw vergeleken met de kaart die de start heeft geladen. |
| 6. Runtime | plugin | Operaties `module.<id>.<operation>` bereiken `Execute`. |
| 7. Stoppen | plugin, daarna controller | Wanneer de sessie eindigt (of de module faalt), wordt `Stop()` aangeroepen. Nadat OMSI is afgesloten, worden de sessieprogramma's gestopt. |
| 8. Opruimen | plugin | Alles wat de module bijhield wordt in omgekeerde volgorde vrijgegeven en geverifieerd verdwenen; de native component wordt als laatste gestopt en zijn hooks worden verwijderd; zijn DLL blijft geladen, inert, tot OMSI afsluit (een thread kan nog via een detour terugkeren). |

Elke callback in het proces draait op de UI-thread van OMSI, zoals de eigen runtime-operaties van het product. De brug meet elke callback aan zijn budget: een callback die te laat terugkeert, laat zijn fase mislukken (`OL_E_MODULE_PHASE_TIMEOUT`). De brug kan een callback die nooit terugkeert niet onderbreken; zo'n module laat OMSI vastlopen, en de sessie eindigt bij zijn opstart-timeout of door `session stop`, zoals elke andere sessie.

<a id="failures"></a>
## Fouten

Een fout is een uitzondering of een overschrijding in een fase of operatie, een in het proces niet opgeloste capability, een contentbinding die in het proces niet klopt, een conflicterende plugin die in OMSI is geladen, of een sessieprogramma dat niet gereed raakt. Het `failure_policy` van de module bepaalt wat volgt. In elk geval worden de resources van de module vrijgegeven.

| Beleid | Fout bij voorbereiden, laden, arm of gameplay gereed | Fout in een operatie | Fout bij stoppen |
| --- | --- | --- | --- |
| `fail-session` | Module vrijgegeven; `module.session-failure`; de sessie mislukt met `OL_E_MODULE_SESSION_FAILED`, OMSI wordt gestopt en bestanden worden hersteld. Een fout voordat de wereld start voorkomt de start. | Hetzelfde. | Gemeld (`module.failed`); de sessie gaat door tot haar einde. |
| `disable-module` | Module vrijgegeven en uitgeschakeld; de sessie gaat door. | Hetzelfde. | Gemeld; module vrijgegeven. |
| `degrade` | Als `disable-module`, behalve dat de fout van een sessieprogramma de module in de sessie laat. | De falende aanroep geeft zijn code terug; de module blijft actief; `module.degraded` wordt eenmaal per code gemeld. | Gemeld; module vrijgegeven. |

Een uitgeschakelde of gefaalde module beantwoordt zijn operaties met `OL_E_MODULE_DISABLED`. Modulefouten verschijnen als sessiediagnoses (`SessionStatus.Diagnostics`) met hun `OL_E_MODULE_*`-code en als runtime-events.

<a id="cleanup"></a>
## Opruimen

Wat een module via zijn context verkrijgt, wordt bijgehouden: resources geregistreerd met `Track`, threads gestart met `RunBackground`, processen gestart met `StartAuxiliaryProcess`. Bij stoppen of falen annuleert de brug `Stopping` en geeft ze deze daarna in omgekeerde volgorde vrij. Hulpprocessen worden ook in een jobobject met kill-on-close geplaatst, zodat ze met OMSI eindigen, zelfs als OMSI wordt beëindigd; de job van een sessieprogramma beëindigt het met de OmsiLaunch-sessie, zelfs als OmsiLaunch zelf wordt beëindigd. Een resource die na het vrijgeven nog leeft (een proces dat niet is afgesloten, een thread die niet is geëindigd) wordt gemeld als `OL_E_MODULE_CLEANUP_INCOMPLETE`. Threads of processen die een module zonder zijn context aanmaakt, worden niet bijgehouden.

Na een crash valt er voor modules niets te herstellen: ze houden geen installatiebestanden vast, en hun geheugen, threads en aan een job gebonden processen eindigen met het OMSI-proces of het OmsiLaunch-proces.

<a id="native-components-and-hooks"></a>
## Native componenten en hooks

Een native component is een 32-bits DLL die de C-interface in `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h` implementeert:

| Export | Aangeroepen |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | Bij arm, vóór de beheerde `Arm`. Niet-nul laat de fase mislukken. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Optioneel; nadat de gameplay is betreden. |
| `void omsilaunch_module_native_v1_stop(void)` | Bij stoppen of falen, voordat zijn hooks worden verwijderd. Daarna weigert de brugtabel elke aanroep en blijft de DLL geladen, inert, tot OMSI afsluit. |

De brugtabel geeft de component `request_hook`, `emit` (runtime-events `module.<id>.<name>` met een JSON-object van tekenreeksen) en, vanaf protocol 2, `get_value`. Een component leest de `size` van de tabel voordat ze `get_value` gebruikt (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` kopieert een waarde als UTF-8 met een afsluitende nul en zet `*length` op de benodigde bytes zonder die nul; het geeft `0`, `1` (argument), `2` (onbekende naam), `11` (na stoppen) of `12` (buffer te klein) terug. Namen:

| Naam | Waarde |
| --- | --- |
| `setting.<key>` | De effectieve waarde van een gedeclareerde instelling. |
| `data_directory` | De [gegevensmap](#data-directory) van de module. |
| `module_directory` | De eigen map van de module. |
| `session_id` | De sessie-id. |
| `module_version` | De manifestversie. |
| `product_version` | De productversie van OmsiLaunch. |
| `session_channel` | De pipe van het [sessieprogramma](#session-programs); onbekend voor een module zonder sessieprogramma. |

Hooks worden alleen via een symbolisch doel aangevraagd, tijdens `start` of `gameplay_ready`, voor een doel waarvan het manifest de capability declareert:

| Doel | Hookt | Capability |
| --- | --- | --- |
| `iat:<dll>!<function>` | Een import van `Omsi.exe`, bijvoorbeeld `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` of `EndScene` van het actieve Direct3D 9-apparaat (vraag het aan in `gameplay_ready`, wanneer het apparaat bestaat). | `bridge.hooks.d3d9` |

`request_hook` geeft de functie om de keten voort te zetten terug in `*original` (een detour moet haar aanroepen). Het resultaat: `0` succes, `1` argument, `2` onbekend doel, `3` de locatie bevat niet wat ze zou moeten bevatten, `4` de locatie is gehookt door iemand buiten de brug, `5` geheugenbeveiliging, `6` register vol, `7` volgorde, `8` geheugen, `9` een thread voerde de te patchen bytes uit, `10` nog geen Direct3D-apparaat, `11` nu niet toegestaan of capability niet gedeclareerd.

Elke hook die de brug installeert, voor het product of een module, wordt in één register vastgelegd met zijn eigenaar (`module:<id>` voor een module):

- Een hook wordt alleen geïnstalleerd over de waarde die de locatie zou moeten bevatten (de geïmporteerde functie; een functie van een module met de naam `d3d9.dll`, de systeemruntime of een wrapper), of gestapeld over de bovenste hook van het register zelf op die locatie. Een locatie die al door iemand anders is gewijzigd, wordt geweigerd (`4`), nooit overschreven.
- Hooks op één locatie worden in last-in-first-out-volgorde verwijderd; een hook met een andere erbovenop blijft staan tot die andere is verwijderd.
- De eigen Direct3D-`Reset`-hook van het product staat in het register, zodat apparaathooks van modules erop stapelen.
- Bij het opruimen verwijdert de brug elke hook van de module nadat `stop` is teruggekeerd. Als er een niet kan worden verwijderd (iemand anders heeft de locatie gewijzigd), meldt het opruimen `OL_E_MODULE_CLEANUP_INCOMPLETE`. De DLL van een native component wordt nooit ontladen zolang OMSI draait, zodat een detour waar nog doorheen wordt teruggekeerd geldig blijft.

De engine ondersteunt ook hooks op functieprologen met een trampoline (andere threads worden opgeschort terwijl de bytes veranderen, en de exacte verwachte bytes worden eerst gecontroleerd), maar er wordt nog geen prologendoel aan modules aangeboden: zulke doelen komen uit de doelcatalogus van de build met geverifieerde bytes.

<a id="operations"></a>
## Operaties

Een moduleoperatie is `module.<id>.<operation>`. Ze bestaat alleen voor een sessie waarvan het plan de module heeft geladen (`PlannedModule.Operations`); anders antwoordt `ExecuteRuntimeAsync` met `OL_E_MODULE_NOT_LOADED` of `OL_E_MODULE_OPERATION_UNKNOWN`. Vereiste argumenten worden gecontroleerd en niet-gedeclareerde argumenten worden geweigerd (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Een getypeerde fout van een module behoudt zijn code wanneer dat een gecatalogiseerde openbare code is; elke andere code wordt `OL_E_MODULE_OPERATION_FAILED` met de code van de module in `Values["module_code"]`. Resultaatsleutels die de interne naamconventie volgen worden verwijderd, zoals bij elke runtime-operatie.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="writing-a-module-contracts"></a>
## Een module schrijven (contracten)

De beheerde component verwijst naar `OmsiLaunch.Runtime.Modules` en `OmsiLaunch.Api` en implementeert `IRuntimeModule`. De brug bindt deze twee verwijzingen aan haar eigen geladen assemblies (in welke laadcontext de plugin ook draait), dus een met de module meegeleverde kopie wordt genegeerd:

| Lid | Aangeroepen |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Stap 4. Verkrijg resources via de context. |
| `void GameplayReady()` | Stap 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Stap 6, voor elke gedeclareerde operatie (zonder het voorvoegsel `module.<id>.`). Werp `ModuleOperationException(code, message)` voor een getypeerde fout. |
| `void Stop()` | Stap 7. |

`IRuntimeModuleContext` biedt `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `SessionChannelName`, `Stopping` (een annuleringstoken), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground` en `StartAuxiliaryProcess`. Niets daarin legt adressen, het uitvoerbare bestand of zijn identiteit bloot. De component moet door de 32-bits plugin laadbaar zijn (AnyCPU of x86, .NET 6). Zet elk bestand vast dat de programma's van de module laden (voor een .NET-programma: zijn `.dll` naast de `.exe`), zodat de integriteitscontrole ze dekt.

<a id="known-limitations"></a>
## Bekende beperkingen

- Native componenten kunnen imports van `Omsi.exe` en methoden van het Direct3D-apparaat hooken; hooks op prologen en aanroeplocaties van OMSI-functies worden nog niet aan modules aangeboden.
- De contentbindingscontrole in het proces gebruikt de kaart die de start heeft aangevraagd (NEW_MAP); voor een opgeslagen situatie wordt de binding bij het plannen gecontroleerd tegen de kaart van de situatie.
- Apparaathooks zijn vtable-hooks. Waar een andere component de oorspronkelijke methoden van het apparaat terugschrijft (waargenomen op de regressie-installatie kort nadat de gameplay start), wordt een apparaathook niet meer aangeroepen en kan hij niet worden verwijderd; het opruimen meldt dan `OL_E_MODULE_CLEANUP_INCOMPLETE` en de component blijft geladen. Een apparaatmethode die al door iets buiten het register is gehookt, wordt geweigerd (4); het weigeringsevent noemt de module die de methode bezit.
- Modules stoppen bij een normale OMSI-afsluiting; een sessiestop beëindigt OMSI, dus het opruimen van modules binnen een sessie gebeurt alleen via een faalbeleid.
- Meldingen worden getoond zodra het systeemvakpictogram bestaat, nadat OMSI is gestart; een sessieprogramma toont zijn eigen venster voor alles wat het daarvóór moet zeggen.
