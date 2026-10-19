# Capabilities

<!-- l10n: source=reference/capabilities.md source-sha256=e6d9d063c2365c007efc79c0057a4843d1f91b88ba68448cc8b8d7579308de23 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/capabilities.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina beschrijft wat OmsiLaunch kan. De bron is de canonieke capabilitycatalogus, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): elke capability, elke openbare runtime-operatie met haar argumenten, CLI-routes en native capability, en één bewijsstatus per capability worden daar één keer gedeclareerd. De set runtime-operaties, de argumentvalidatie, de CLI-opdrachtroutes, de native poort per operatie van de plugin, `GetCapabilitiesAsync` en de [catalogustabel](#capability-catalog) hieronder zijn ervan afgeleid. Voor elke runtime-operatie vermeldt de pagina de argumenten, resultaatsleutels en fouten. Hoe je een operatie aanroept (CLI-routes, API-envelope, timeouts, handles) staat in [runtime control](runtime-control.md); het bewijs per sessie staat in [status van runtimevalidatie](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificatie en stabiliteit

`PublicCapabilityClassification` heeft vier waarden. Ze worden als volgt op de stabiliteitsterminologie afgebeeld, met afwaarderingen waar het bewijs onvolledig is:

| Classificatie | Betekenis | Stabiliteit |
| --- | --- | --- |
| `PublicStableBeta` | Openbaar, onderdeel van het bètacontract, runtime-gevalideerd | `STABLE_BETA` (afgewaardeerd tot `PARTIAL` waar vermeld) |
| `PublicExperimental` | Openbaar, kan veranderen, runtime-gevalideerd of statisch gevalideerd | `EXPERIMENTAL` (afgewaardeerd tot `PARTIAL` waar vermeld) |
| `InternalOnly` | Onderzoeksprimitief; niet bereikbaar via de openbare API of de CLI | `INTERNAL` |
| `Unsupported` | Herkend voor diagnose, geweigerd of afwezig | `UNAVAILABLE` |

`PublicCapabilityKind` onderscheidt `Read`, `Write`, `Action` en `Event`. Elke runtime-capability vereist een sessie in de toestand `Running` (`RequiresSession` in het register) en haar native capability opgelost en geverifieerd op de draaiende build; een operatie waarvan de native capability niet beschikbaar is, wordt geweigerd met `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` markeert capabilities die van native doelen afhankelijk zijn (zie [compatibiliteit](compatibility.md)).

Runtimeresultaten zijn dictionaries van strings. Sleutels die beginnen met `internal_` of eindigen op `_address`, `_pointer` of `_vmt` worden aan de API-grens verwijderd (`ScrubInternalValues`) en worden hier niet vermeld. De resultaatsleutels in de onderstaande operatietabellen zijn exact de sleutelverzamelingen die het product retourneert: ze zijn vastgelegd door elke openbare operatie in een echte sessie uit te voeren (documentatieopname van de runtime closure, sessie `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; lijstrijen worden geschreven als `<row>.<n>.<field>`).

Hoe het mislukken van een operatie wordt gerapporteerd (`CurrentRuntimeControl.Execute`):

| Fout | `ErrorCode` | `Values` |
| --- | --- | --- |
| Weigering door het register (onbekende of `internal.*`-operatie, ontbrekend verplicht argument) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | geen |
| Een bewuste weigering door het product (een afgeleide weersleutel in `weather.set`) | de specifieke code (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (zin) |
| Een D3D-fout | de specifieke `OL_E_D3D_*`-code | `detail`, `native_status` |
| Een getypeerde fout aan de pluginzijde (verouderde handle, onbekende naam, waarde buiten bereik, geen spelersvoertuig, ...) | de specifieke code, bijvoorbeeld `OL_E_RUNTIME_OBJECT_HANDLE_STALE` of `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (het bericht), `exception` (.NET-typenaam) |
| Een operatie waarvan de native capability op de draaiende build niet is opgelost | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (de native capability) |
| Een onverwachte exceptie aan de pluginzijde zonder code | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Een resultaat dat niet in de mailbox van 64 KiB past | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (gepagineerde lijsten worden in plaats daarvan ingekort en gaan verder met `next_cursor`, zie [paginering](#pagination)) | geen |

De kolom `Errors` (Fouten) van elke tabel noemt de specifieke codes; ze komen ongewijzigd in `ErrorCode` aan via de plugin, de bridge, het control plane, de API en de CLI.

<a id="capability-catalog"></a>
## Capabilitycatalogus

Eén rij per catalogusvermelding, gegenereerd uit `PublicCapabilityRegistry.All` (de documentatiegate faalt wanneer deze tabel verouderd is). `Session` (Sessie) is `Running` voor capabilities die een actieve sessie nodig hebben. `Operations` (Operaties) zijn de concrete operatie-id's voor de runtime (contractrijen in [openbare runtime-operaties](#public-runtime-operations)). `Native capability` noemt de vermeldingen van de catalogus met native doelen (zie [compatibiliteit](compatibility.md)) die op de draaiende build moeten zijn opgelost en geverifieerd. `Evidence` (Bewijs) gebruikt één vocabulaire:

| Bewijs | Betekenis |
| --- | --- |
| RUNTIME_VALIDATED | Waargenomen in een echte OMSI-sessie; de sessie-id's staan in [status van runtimevalidatie](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Geïmplementeerd en offline geverifieerd; het runtime-effect is niet waargenomen. |
| PARTIAL | Werkt voor een deel van zijn contract; de opmerking noemt het openstaande deel. |
| RUNTIME_VALIDATION_REQUIRED | Geïmplementeerd, wacht op zijn runtimevalidatie. |
| UNAVAILABLE | Herkend voor diagnose en geweigerd, of niet geïmplementeerd; het aanvragen bij de start maakt het plan niet uitvoerbaar. |

Een operatie die iets wijzigt, noemt haar mutatiebeleid achter haar id (`PublicRuntimeOperation.MutationPolicy`); geen enkele runtime-operatie wordt in het journal vastgelegd:

| Mutatiebeleid | Betekenis |
| --- | --- |
| `none` | Alleen lezen (niet getoond). |
| `omsi-state` | Wijzigt de toestand van OMSI in het geheugen; niets maakt dit ongedaan; het eindigt met het OMSI-proces. |
| `session-resource` | Maakt of wijzigt een resource in bezit van de sessie, expliciet vrijgegeven of wanneer de sessie eindigt. |
| `session-policy` | Installeert een beleid dat de plugin opnieuw toepast totdat het wordt verwijderd of de sessie eindigt. |

| Capability | Classificatie | Soort | Sessie | Operaties | Native capability | Bewijs | CLI | Opmerking |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | nee |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | nee |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | nee |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: pluginevents reizen via een genummerde ring; een lezer die achterloopt, ontvangt telemetry.lost met het aantal. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | nee |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | nee |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | nee |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 ronde A: bekende indeling. |
| `boot.headless-start` | `PublicStableBeta` | Action | nee |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | nee |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | nee |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | nee |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI toont het eerste record van elk exact label; de start selecteert het op dat label. Statisch afgesloten; runtimevalidatie in afwachting. |
| `world.saved-situation` | `PublicStableBeta` | Action | nee |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | nee |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: de native tak is statisch afgesloten (BI-006); runtimevalidatie in afwachting. Nooit een heuristiek op de nieuwste .osn. |
| `world.explicit-date` | `PublicExperimental` | Action | nee |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: geschreven in de startklok van OMSI vóór NewSituation (statische analyse); runtimevalidatie in afwachting. |
| `world.explicit-time` | `PublicExperimental` | Action | nee |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: geschreven in de startklok van OMSI vóór NewSituation (statische analyse); runtimevalidatie in afwachting. |
| `world.explicit-year` | `PublicExperimental` | Action | nee |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: geschreven in de startklok van OMSI vóór NewSituation (statische analyse); runtimevalidatie in afwachting. |
| `world.weather` | `PublicExperimental` | Action | nee |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): toegepast vlak nadat de wereld start, via het eigen loadWeather van OMSI of zijn METAR-vlaggen; het overschrijft het weer van een opgeslagen situatie. Statisch afgesloten; runtimevalidatie in afwachting. |
| `player-vehicle.model` | `Unsupported` | Action | nee |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; opgelost voor diagnose, niet toegepast. |
| `player-vehicle.repaint` | `Unsupported` | Action | nee |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | nee |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | nee |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | nee |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false bij een headless start; de volledige snapshot met een opgeslagen situatie. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: de native toewijzingsreeks is statisch afgesloten (research/reports/beta4-static/entrypoint-last-player-vehicle.md sectie 3) maar niet geïmplementeerd vóór de runtimevalidatie; gepland voor een latere bèta. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | nee |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | nee |  |  | STATICALLY_VALIDATED |  | Beta 4: transactionele overlay, offline gevalideerd. |
| `input.controller` | `PublicExperimental` | Write | nee |  |  | STATICALLY_VALIDATED |  | Beta 4: transactionele overlay, offline gevalideerd. |
| `presentation.splash` | `PublicStableBeta` | Write | nee |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | nee |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 ronde B: de onderdrukking wordt toegepast en gemeld (internet-textures.suppressed) via de telemetriering. |
| `internet-textures.override` | `PublicExperimental` | Write | nee |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Runtime-afsluitronde I01. |
| `session-profiles` | `PublicStableBeta` | Action | nee |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 profielstart; 1..99 en detectie offline. |
| `hof.management` | `PublicStableBeta` | Write | nee |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 ronde A C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (gepagineerd) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | nee |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | nee |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | nee |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | nee |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | nee |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | nee |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | nee |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 statisch afgesloten: SetActualDateTime kopieert alleen de pc-klok naar de startklok; startdatums gebruiken world.explicit-date. Vervangen: native ABI en postcondities zijn niet afgesloten. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: schrijft actWeather (Weather+0x08), het record waarvan de weertick van OMSI afleidt; statisch afgesloten. Afgeleide waarden (luchtvochtigheid, lichtfactoren, actuele wind) blijven alleen-lezen (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Runtimevalidatie in afwachting. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (gepagineerd) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Vereist een PlayerVehicle (een opgeslagen situatie levert er een); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE bij een headless NEW_MAP. |
| `ui.pause` | `PublicExperimental` | Action | Running | `ui.pause.state`, `ui.pause.take-over` (omsi-state), `ui.pause.restore-native` (omsi-state), `ui.pause.action` (omsi-state) | `ui.pause-menu.replace` | RUNTIME_VALIDATION_REQUIRED | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action --action=<resume|new-game|save|options|quit>` | Beta 5: statisch gevalideerd ontwerp (research beta4-static pause-menu-and-input.md); de native aanroepen zijn nog niet waargenomen in een echte sessie. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (gepagineerd) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | Detectie van verouderde handles na natuurlijke verwijdering (RV-002) heeft geen veilige runtimeproducent en is offline gevalideerd. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (gepagineerd) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (gepagineerd), `timetable.trips.list` (gepagineerd), `timetable.lines.list` (gepagineerd), `timetable.rv-files.list` (gepagineerd), `timetable.track-entries.list` (gepagineerd), `timetable.bus-stops.list` (gepagineerd), `timetable.station-links.list` (gepagineerd), `timetable.tours.list` (gepagineerd), `timetable.profiles.list` (gepagineerd), `timetable.tour-entries.list` (gepagineerd), `timetable.logs.read` (gepagineerd), `timetable.no-rv-numbers.list` (gepagineerd) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (gepagineerd), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (gepagineerd), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Schrijven: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI kopieert een string met de vorm van een constante; statisch afgesloten. Runtimevalidatie in afwachting. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): virtuele TriggerXML/SoundTrigger via de geprofileerde VMT-slots; statisch afgesloten. Runtimevalidatie in afwachting. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (gepagineerd), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (gepagineerd), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (gepagineerd) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (gepagineerd) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Device Reset waargenomen (resetting, restored, ongeldig maken van generaties); een afzonderlijke lost-overgang is niet veroorzaakt (RV-007). |

`internal.make-basic` is `InternalOnly`: de operatie bestaat in de plugin voor onderzoek (ze retourneert een native adres) en wordt geweigerd aan de API-grens en door het lokale control plane, die beide de operatienaam eerst tegen `PublicRuntimeOperationIds` valideren; de CLI heeft er geen route voor. `session.stop` beëindigt OMSI; coöperatief afsluiten is niet geïmplementeerd.

<a id="pagination"></a>
## Paginering

Elke lijstoperatie (gemarkeerd met "(gepagineerd)" in de [capabilitycatalogus](#capability-catalog)) neemt twee optionele argumenten en beantwoordt vier paginavelden:

| Argument | Betekenis |
| --- | --- |
| `cursor` | Waar de pagina begint: de `next_cursor` van de vorige pagina. Weggelaten: de eerste pagina. Een cursor van een andere lijst, of een onjuist gevormde, is `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Gewenste rijen (een positief geheel getal). Groter dan de maximale pagina van de lijst: het maximum wordt gebruikt. Geen positief geheel getal: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Resultaatsleutel | Betekenis |
| --- | --- |
| `count` | Alle records die OMSI bevat. |
| `returned_count` | Rijen in dit antwoord. |
| `truncated` | `true` wanneer er records op deze pagina volgen. |
| `next_cursor` | Alleen aanwezig wanneer er records volgen: geef hem door als `cursor` voor de volgende pagina. |

De cursor is een stabiele sleutel, geen positie. Voor recordarrays (dienstregeling, scriptnamen, constanten, curves, HOF's, chauffeurs, kaartjes, logs) zijn de rijen gesleuteld als `<row>.<n>.<field>` (of `name.<n>`) op de absolute index van het record en noemt de cursor de volgende index, zodat de eerste pagina gelijk is aan het antwoord van Beta 3. Voor handlecollecties (`road-vehicles.list`, `humans.list`) worden de rijen in handlevolgorde geretourneerd en noemt de cursor de laatst geretourneerde handle: een object dat tussen twee pagina's verschijnt, krijgt een latere handle en komt op een latere pagina; er wordt er geen herhaald. Behandel cursorwaarden als ondoorzichtig.

Maximale paginagroottes: 1000 voor `road-vehicles.list` en `humans.list`; 512 voor `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` en `vehicle.curves.list`; 256 voor `timetable.tour-entries.list`, `timetable.logs.read` en `vehicle.hofs.read`; 128 voor de overige dienstregelinglijsten, `drivers.read` en `tickets.read`. Wanneer de rijen van een pagina nog steeds groter zijn dan de mailbox van 64 KiB, laat de plugin de laatste rijen vallen totdat het antwoord past en gaat `next_cursor` verder bij de eerste weggelaten rij, zodat geen enkele lijst mislukt door zijn omvang.

<a id="public-runtime-operations"></a>
## Openbare runtime-operaties

Dit is de exacte lijst van operatie-id's die een openbare frontend mag doorsturen (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Elke operatie vereist `SessionState.Running`; geen enkele wordt in het journal vastgelegd of hersteld. Verplichte argumenten worden door het register afgedwongen voordat de mailbox wordt gebruikt (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); optionele argumenten worden door de plugin gevalideerd. Algemene foutcodes voor alle operaties: `OL_E_RUNTIME_OPERATION_UNKNOWN` (niet in deze lijst), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (de plugin heeft geen implementatie), `OL_E_RUNTIME_OPERATION_FAILED` (onverwachte exceptie binnen het proces; waarden `detail` en `exception`).

<a id="time"></a>
### Tijd

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `time.read` | Read | geen | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | optioneel `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimaal); minstens één | de sleutels van `time.read` na de geprofileerde `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Weer

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | geen | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, en de bronwaarden `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, de lichtfactoren en de luchtvochtigheden zijn de waarden per tick van OMSI; `cloud_type` is het opgeloste nummer van het wolkentype | |
| `weather.set` | Write | minstens één van `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Bereiken: mist 0..100000, lightness en grondwaarden 0..1, windsnelheid 0..200, windrichting 0..360, temperatuur en dauwpunt -60..60, druk 850..1100, neerslag 0..2, snelheid 0..255, wolkenhoogte 0..20000, `cloud_type` 1..64 tekens van codepagina 1252 (`-1` = geen wolken), booleans `true`/`false`/`1`/`0` | de sleutels van `weather.read`; met `cloud_type` ook `cloud_type_resolved` (`false` wanneer OMSI het wolkentype niet kent: het tekent er geen) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (een afgeleide sleutel: `relative_humidity`, `absolute_humidity`, de lichtfactoren, `cloud_transparency`, `precipitation_set`, `wet_ground`; het detail noemt de sleutel die moet worden ingesteld), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (onbekende sleutel), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 letters of cijfers) | de sleutels van `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | geen | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Kaart

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `map.read` | Read | geen | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

### Camera

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | geen | `family` (0 chauffeur, 1 passagier, 2 extern, 3 kaart), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | optioneel `family` (0..3), `field_of_view` (10..170); minstens één | de sleutels van `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | verplicht `family` (0..3); optioneel `preset` (0..255, alleen met familie 0 of 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Het beleid wordt elke 100 ms opnieuw toegepast tot `camera.unlock`; een mislukte hernieuwde toepassing geeft eenmaal per afzonderlijke fout de gebeurtenis `camera.lock.degraded`. |
| `camera.unlock` | Action | geen | `locked` (`false`) | |

<a id="user-interface-pause-menu"></a>
### Gebruikersinterface (pauzemenu)

Neem het native pauzemenu van OMSI over voor het eigen menu van een module (capability `ui.pause-menu.replace`, alleen opgelost op een bekende code-identiteit). De eigen pauzebyte van OMSI blijft gezet, zodat de simulatie, de rendering, 3D-klikken en DirectInput-opdrachten vastgehouden blijven; het native formulier wordt via de VCL verborgen en de acties ervan worden rechtstreeks aangeroepen (geen klik, geen geposte message, geen verplaatst venster). `ui.pause.action` voert de native handler uit vanuit een eenmalige UI-thread-timer nadat het antwoord is verzonden, omdat de handler een native modaal dialoogvenster kan openen: lees de uitkomst uit `ui.pause.state` (`busy` `false`, daarna `last_result`).

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `ui.pause.state` | Read | geen | `paused`, `native_menu_visible`, `main_form_ready`, `in_frame`, `taken_over`, `busy`, `pending_action`, `last_action`, `last_result` (`none`, `resumed`, `game-started`, `returned`, `quit-requested`, `exception`, `refused`) | `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.take-over` | Action | geen | de sleutels van `ui.pause.state` na de overname | `OL_E_UI_NOT_PAUSED`, `OL_E_UI_BUSY`, `OL_E_UI_ALREADY_TAKEN_OVER`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.restore-native` | Action | geen | de sleutels van `ui.pause.state`; toont het native menu opnieuw (de eigen `FormShow` ervan wordt uitgevoerd: OMSI blijft gepauzeerd) | `OL_E_UI_BUSY`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.action` | Action | verplicht `action`: `resume`, `new-game`, `save`, `options` of `quit` | de sleutels van `ui.pause.state` plus `scheduled` (`true`) en `action` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_UI_NOT_PAUSED`, `OL_E_UI_BUSY`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |

<a id="road-vehicles-and-player"></a>
### Wegvoertuigen en speler

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | geen | `count`, `ai_collection`, `player_index` (de ruwe spelersvoertuigindex van OMSI, gerapporteerd zoals gelezen; gebruik `present` van `player-vehicle.read` om te weten of er een spelersvoertuig bestaat) | |
| `road-vehicles.list` | Read | optioneel `cursor`, `limit` | paginavelden; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` is de lijstpositie in OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | verplicht `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | geen | `present` (`false` als OMSI geen spelersvoertuig heeft); bij `true`: `player_index` plus elke sleutel van `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | verplicht `model` (canoniek `Vehicles\...\*.bus`, maximaal 240 tekens, geen `..`, moet onder de installatie bestaan) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Wijst het PlayerVehicle niet toe. Duurt enkele seconden; gebruik de clienttimeout van 30 s. |
| `road-vehicles.place-random` | Action | optioneel `ai_type` (0..255, standaard 0), `group` (0..65535, standaard 1), `type` (-1..65535, standaard -1), `scheduled` (0..1, standaard 0), `tour` (0..65535, standaard 0), `line` (0..65535, standaard 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Mensen

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | geen | `count` | |
| `humans.list` | Read | optioneel `cursor`, `limit` | paginavelden; `human.<n>.handle` (`hb-NNNNNN`; `<n>` is de lijstpositie in OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | verplicht `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Dienstregeling, chauffeurs, kaartjes

Lijsten van de dienstregeling, `drivers.read` en `tickets.read` zijn gepagineerd (zie [paginering](#pagination)); "paginavelden" betekent hieronder `count`, `returned_count`, `truncated` en `next_cursor`, en elke lijst neemt de optionele `cursor` en `limit`.

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | geen | `invalid` (`true`/`false`), en de recordaantallen `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | optioneel `cursor`, `limit` | paginavelden; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | optioneel `cursor`, `limit` | paginavelden; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | optioneel `cursor`, `limit` | paginavelden; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | optioneel `cursor`, `limit` | paginavelden; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | optioneel `cursor`, `limit` | paginavelden; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | optioneel `cursor`, `limit` | paginavelden; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | optioneel `cursor`, `limit` | paginavelden; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | optioneel `cursor`, `limit` | paginavelden; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | optioneel `cursor`, `limit` | paginavelden; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | optioneel `cursor`, `limit` | paginavelden; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | optioneel `cursor`, `limit` | paginavelden; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | optioneel `cursor`, `limit` | paginavelden; `no_rv.<n>.group`, `no_rv.<n>.number`: de voertuignummers die OMSI buiten het vrije AI-gebruik houdt (de lijst met gereserveerde nummers van OMSI, gelezen via zijn eigen indeling; `OL_E_RUNTIME_VALUE_INVALID` wanneer de lijst niet de verwachte vorm heeft). Hoogstens 256 per pagina. Statische analyse; runtimevalidatie in afwachting. |
| `map.tiles.list` | Read | optioneel `cursor`, `limit` | paginavelden; `tile.<n>.x`, `tile.<n>.y` (rasterpositie), `tile.<n>.tile_index` (de eigen index van de tile in de tilelijst van de kaart), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` met `tile.<n>.error` voor een tile die niet kan worden gelezen. Hoogstens 256 per pagina. Positie en index uit statische analyse; runtimevalidatie in afwachting. |
| `drivers.read` | Read | optioneel `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | optioneel `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Voertuigscripts, constanten, curves, HOF (per handle)

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | verplicht `handle`; optioneel `cursor`, `limit` | `handle`, paginavelden, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | verplicht `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | verplicht `handle`, `name`, `value` (eindige float) | `handle`, `name`, `requested_value`, `value` (teruggelezen) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | verplicht `handle`; optioneel `cursor`, `limit` | `handle`, paginavelden, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | verplicht `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | verplicht `handle`, `name` (hoofdlettergevoelig, zoals het script het declareert, tot 128 tekens), `value` (1..1024 tekens) | `handle`, `name`, `value` teruggelezen na het schrijven | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (OMSI zelf zou een onbekende naam stil negeren), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | verplicht `handle`, `name` (codepagina 1252, tot 128 tekens), `value` (`1` voert `name` uit, `0` voert `name_off` uit, `2` voert `name_drag` uit) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. De scriptingang wordt synchroon uitgevoerd; een naam die het script niet definieert, doet niets. Op een indrukken volgt normaal een loslaten |
| `vehicle.sound-trigger` | Action | verplicht `handle`, `trigger` (codepagina 1252, tot 128 tekens); optioneel `file` (tot 260 tekens) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (ook: het voertuig heeft geen geluidspakket), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | verplicht `handle`; optioneel `cursor`, `limit` | `handle`, paginavelden, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | verplicht `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | verplicht `handle`; optioneel `cursor`, `limit` | `handle`, paginavelden, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | verplicht `handle`, `name`, `x` (eindige float; een ontbrekende of niet-numerieke `x` geeft `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (lineaire interpolatie) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | verplicht `handle`; optioneel `cursor`, `limit` | `handle`, paginavelden, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D-operaties worden via de native brug uitgevoerd op de hoofd-/renderthread van OMSI. Texturehandles hebben de vorm `d3dtex-<sessionId N>-<16 hex digits>`.

| Operatie | Soort | Argumenten | Resultaatsleutels | Fouten |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | geen | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | verplicht `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); optioneel `levels` (0..16, standaard 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | verplicht `handle`; optioneel `level` (0..15, standaard 0) | de 13 sleutels van `d3d.texture.create`, voor het gevraagde niveau | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, plus de fouten van create |
| `d3d.texture.update` | Action | verplicht `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (maximaal 48 KiB na decodering); optioneel `level` (0..15, standaard 0), `x` (0..4095, standaard 0), `y` (0..4095, standaard 0) | de 13 sleutels van `d3d.texture.create` na de update | `OL_E_D3D_INVALID_PIXEL_BUFFER`, plus de fouten van describe |
| `d3d.texture.release` | Action | verplicht `handle` | de 13 sleutels van `d3d.texture.create` met `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` bij herhaald vrijgeven, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Mislukte D3D-operaties retourneren de waarden `detail` en `native_status`. De extensiemethoden van `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) omhullen deze operaties voor API-gebruikers.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` retourneert één `Capability(Name, Available, EvidenceState, Reason)` per catalogusvermelding die niet `InternalOnly` is. `Name` is de catalogus-id en `EvidenceState` het catalogusbewijs ervan. `Available` wordt voor de installatie geëvalueerd:

- `Unsupported`-vermeldingen zijn nooit beschikbaar; `Reason` bevat de opmerking uit de catalogus.
- Elke vermelding heeft het huidige platform nodig, en elke vermelding behalve `runtime.current-windows-x64` heeft een OMSI 2-installatie nodig.
- Een vermelding met native capabilities op de bekende indeling meldt `native: RESOLVED_KNOWN_LAYOUT`. Op een andere OMSI 2-build wordt het uitvoerbare bestand statisch opgelost: `native: STATICALLY_RESOLVED`, of `native: PENDING_IN_PROCESS_RESOLUTION` met de naam van wat het bestand niet oploste. Een vermelding in afwachting wordt nog steeds als beschikbaar gemeld, omdat de oplossing in het proces beslist; als die mislukt, mislukt de operatie of de start met `OL_E_CAPABILITY_UNRESOLVED`.

De planner meldt capabilities per plan in `SessionPlan.RequiredCapabilities` en `SessionPlan.UnsupportedRequestedFeatures` met dezelfde catalogus-id's (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Een niet-ondersteunde aangevraagde functie draagt haar catalogusbewijs; een vereiste native capability draagt `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` of `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Bekende beperkingen

- Handles gelden per sessie en worden als `OL_E_RUNTIME_OBJECT_HANDLE_STALE` gerapporteerd zodra hun object verdwenen is. Een voertuighandle is gebonden aan zijn VMT, zijn definitie en de `IDCode` van OMSI (een proces-uniek nummer dat elk kaartobject bij constructie krijgt en nooit hergebruikt), zodat een object dat op hetzelfde adres opnieuw is aangemaakt altijd te onderscheiden is. Mensen zijn gepoolde slots die de hele kaartsessie leven: een mensenhandle lost niet meer op wanneer de mens despawnt (`RenderMe` 0) of het slot opnieuw wordt gespawnd (nieuw doelstation of vertrek).
- Resultaten zijn begrensd tot de mailbox van 64 KiB; elke lijst is gepagineerd (zie [paginering](#pagination)).
- Geen verplaatsing van voertuigen tussen tiles, geen schrijven naar de kalender, geen headless toewijzing van het PlayerVehicle. Het schrijven van stringvariabelen, script-triggers en het weer zijn `EXPERIMENTAL` (runtimevalidatie in afwachting). Zie [bekende beperkingen](known-limitations.md).
