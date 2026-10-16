# CLI-referentie

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/cli.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina is de volledige, normatieve referentie voor de opdrachtregel van OmsiLaunch `0.1.0-beta4`: de drie uitvoerbare bestanden, de argumentgrammatica, de dispatchvolgorde, elk opdrachtwoord, elke hiërarchische route, elke vlag, de uitvoer-envelopes en het foutgedrag van elke opdracht. De pagina is gegenereerd uit `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` en de twee native shims onder `tools\OmsiLaunch.Bootstrapper`. Procesresultaten staan in [exitcodes](exit-codes.md), foutcodes in [fouten](errors.md) en uitgewerkte aanroepen in [CLI-voorbeelden](cli-examples.md).

<a id="executables"></a>
## Uitvoerbare bestanden

| Bestand | Subsysteem | Rol | Verschillen |
|---|---|---|---|
| `OmsiLaunch.exe` | Console | Native bootstrapper (`OmsiLaunch.Bootstrapper.cpp`): bepaalt zijn eigen map, splitst de opdrachtregel op met `CommandLineToArgvW`, zoekt `hostfxr` via `nethost.dll` en voert `OmsiLaunch.Controller.dll` uit met dezelfde argumenten. | Er wordt console-uitvoer geschreven; de exitcode van het proces is de exitcode van de managed controller, of een shimcode `100`..`106` als de .NET-host niet kon worden gestart. |
| `OmsiLaunchW.exe` | Windows (GUI) | Dezelfde shim (`OmsiLaunch.WindowsHost.cpp`), gebouwd voor het Windows-subsysteem. Deze stelt de omgevingsvariabele `OMSILAUNCH_WINDOWS_HOST=1` in voordat de controller wordt gestart. | Geen console: console-uitvoer wordt onderdrukt tenzij `--json` is opgegeven (`WindowsHost.SuppressConsole`), fouten worden als berichtvensters getoond (`WindowsHost.ShowFailure`: bericht, `Code: OL_E_...` en de hint `See .omsilaunch\diagnostics for details.`), en een shimfout `100`..`106` wordt getoond als `OmsiLaunch could not start the .NET host (code N).` Volledig gedrag: [referentie voor OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Managed (x64, `net6.0-windows`, Windows Forms) | De controller zelf. Gebruikers roepen deze nooit rechtstreeks aan; beide shims geven het pad van de controller door als eerste hostargument, zodat het nooit in de openbare argumentenlijst verschijnt. | Vereist de x64-.NET 6-runtime met `Microsoft.WindowsDesktop.App`; zie [installatie](../getting-started/installation.md). |

`nethost.dll` moet naast de shims staan. De shims lezen zelf geen argumenten; elk argument bereikt `CliInput.Parse` ongewijzigd, zodat `OmsiLaunch.exe` en `OmsiLaunchW.exe` precies dezelfde syntaxis accepteren.

<a id="invocation-model"></a>
## Aanroepmodel

<a id="argument-grammar-cliinputparse"></a>
### Argumentgrammatica (`CliInput.Parse`)

| Vorm | Betekenis |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Een vlag. De sleutel is niet hoofdlettergevoelig; de waarde is alles na de eerste `:`. Onbekende sleutels mislukken met `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), exit `2`. |
| `--key=value` | Een runtimeargument voor de geselecteerde runtime-operatie (bijvoorbeeld `--handle=rv-000001`). Elk `--`-token dat `=` bevat, is een runtimeargument en nooit een vlag. |
| `--json`, `/json` | Gestructureerde uitvoer (zie [Uitvoerformaten](#output-formats)). `--json` is het enige `--`-token zonder `=` dat betekenis heeft; het wordt geparseerd als de vlag `/json`. |
| los woord | Als er nog geen opdrachtwoord is gezien en het woord een van de [opdrachtwoorden](#command-words) is, wordt het de opdracht. Zodra er een opdrachtwoord aanwezig is, is elk later los woord een opdrachtwoord (de route). Anders is het eerste losse woord de installatiemap en wordt elk later los woord aan de route toegevoegd. |

Gevolgen: een hiërarchische route (`time get`) kan niet worden gecombineerd met een installatieargument dat erna staat (`time get D:\OMSI` is de onbekende route `time get d:\omsi`, exit `2`). `D:\OMSI time get` wordt geaccepteerd, maar is **eigenaarmodus** (er wordt een nieuwe sessie gestart en de operatie wordt daarin één keer uitgevoerd). Parseerfouten (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) en sessieprofielfouten (`SessionProfileException`) worden gemeld voordat er iets wordt uitgevoerd, altijd met exit `2`.

<a id="installation-root"></a>
### Installatiemap

- Een expliciet installatieargument als los woord heeft voorrang op `RootPath` in een `/spec`-bestand (`CliInput.BuildSpecAsync`).
- `.` betekent de map die het uitvoerbare bestand bevat (`AppContext.BaseDirectory`), nooit de werkmap van de aanroeper (`CliInput.ResolveInstallationRoot`). Een portable pakket is hiervan afhankelijk.
- Als het argument ontbreekt, gebruiken operaties in eigenaarmodus (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) ook de map van het uitvoerbare bestand. Het pad wordt genormaliseerd met `Path.GetFullPath`.
- Opdrachten in clientmodus nemen nooit een installatieargument: ze richten zich tot het lokale control-endpoint van de installatie waarin het uitvoerbare bestand staat (`AppContext.BaseDirectory`). Zie [local control](local-control.md).

<a id="owner-and-client"></a>
### Eigenaar en client

- **Eigenaar**: het proces dat een sessie plant, start, bewaakt en herstelt (`OwnerSession.RunAsync`). Het houdt de installatielease (`Local\OmsiLaunch.Installation.<sha256(root)>`) en de configuratietransactie vast, stelt het lokale control-endpoint beschikbaar zolang de sessie actief is en toont het [systeemvakpictogram](windows-tray.md). Precies één eigenaar per installatie: als een eigenaar al antwoordt op `session.status` via het control-endpoint, mislukt een tweede start met `OL_E_SESSION_ALREADY_ACTIVE` (exit `7`).
- **Client**: elke aanroep zonder installatieargument die `session status`, `session stop`, `events read`, `events watch` of een runtime-operatie verstuurt. Deze wordt doorgestuurd via de local-control-pipe; zonder eigenaar mislukt hij met `OL_E_NO_ACTIVE_SESSION` (exit `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Dispatchvolgorde (`CliProgram.RunAsync`)

1. `/silent` (als het proces nog niet onder `OmsiLaunchW.exe` draait): start `OmsiLaunchW.exe` vanuit de map van het uitvoerbare bestand via `ShellExecute` (zonder overerving van handles) met dezelfde argumenten zonder `/silent`/`--silent`, schrijf de envelope `silent` (`delegated`, `host_process_id`) en retourneer `0`. Het consoleproces wacht niet op de sessie; zie [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` retourneren `7`.
2. `/version`: envelope `version` met `product`, `version` (informatieve assemblyversie, gestempeld vanuit `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); exit `0`.
3. `capabilities`: envelope met elke `PublicStableBeta`- of `PublicExperimental`-descriptor van de canonieke catalogus `PublicCapabilityRegistry` (met zijn operaties, native capabilities, bewijs en toelichting); exit `0`.
4. `help [family]`: envelope `help` met `usage`, `product_version`, `protocol_version`, `family` en de openbare `commands` (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), optioneel gefilterd op familie; exit `0`.
5. `profiles`: envelope met `requirement` (`OMSI 2`), het rapport `installation` van de doelmap (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), de `known_layouts` en de `predicted_capabilities`, bepaald aan de hand van het uitvoerbare bestand (de oplossing in het proces bij de start is gezaghebbend); exit `0`.
6. Runtime-operatie als client (geen installatieargument en een route of `/runtime:`): argumenten worden gevalideerd tegen `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, exit `2`), waarna `runtime.execute` wordt doorgestuurd met een timeout van 8 s (30 s voor `road-vehicles.spawn`).
7. Client-`session status` (750 ms), `session stop` (gebonden aan de actieve sessie-id, 750 ms), `events read` (750 ms), `events watch` (pollt elke 250 ms tot Ctrl+C).
8. `detect`, of **helemaal geen argumenten** (geen installatie, geen opdracht, geen `/?`, geen `/spec`, geen startvlag, geen recoveryvlag, geen `/list`): somt `Omsi`-processen op en test het control-endpoint (250 ms); envelope `detect`; exit `0`.
9. `/?` of `/help`: druk de gebruikstekst af, exit `0`. Elke andere aanroep met een opdrachtwoord maar zonder dispatchbare route (bijvoorbeeld `d3d` alleen of `session status D:\OMSI`) drukt de gebruikstekst af en eindigt met exit `2`.
10. Eigenaarmodus. Voorwaarden: `plugins\OmsiLaunch.Plugin.opl` en `plugins\OmsiLaunch.Native.x86.dll` moeten naast het uitvoerbare bestand staan (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, exit `7`). `release-manifest.json` naast het uitvoerbare bestand levert, indien aanwezig, de verwachte plugin-hashes.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; envelope `recover` met `pending`, `recovered`, `diagnostics`; exit `8` alleen als een herstel is aangevraagd en niet is voltooid, anders `0`.
12. `/list:<category>`: `DiscoverAsync`; envelope `content.list`; exit `0`. `/list:SessionProfiles` gebruikt in plaats daarvan `ListSessionProfilesAsync`; envelope `session-profiles.list`.
13. Bouw de `LaunchSpec` (`BuildSpecAsync`), plan deze (`PlanSessionAsync`) en druk het plan af. `/plan` of `/validate`: exit `0` als `IsRunnable`, anders `1`. Een niet-uitvoerbaar plan start OMSI nooit (exit `1`); onder `OmsiLaunchW.exe` toont een start met een niet-uitvoerbaar plan de laatste `OL_E_`-diagnose in een berichtvenster (documentatie-audit BUG-06). Tijdens het plannen wordt ook de geïnstalleerde permanente plugin-closure gecontroleerd tegen `release-manifest.json`, zodat een ontbrekende of gewijzigde plugin het plan niet uitvoerbaar maakt (`OL_E_PERMANENT_PLUGIN_*`).
14. Test of er al een eigenaar is (`OL_E_SESSION_ALREADY_ACTIVE`, exit `7`) en voer daarna `OwnerSession.RunAsync` uit.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Levenscyclus van de eigenaar (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. Vanaf hier bereikt elk exitpad `CloseAsync` in een `finally`-blok: exceptions, Ctrl+C (`Console.CancelKeyPress`), sluiten van de console / afmelden (`AppDomain.ProcessExit` met een budget van 4 s voor stop + herstel; wat overblijft, wordt bij de volgende start via het journal hersteld), "End session" in het systeemvak, pipe-`session.stop` en `/observe-seconds`.
2. Het systeemvakpictogram wordt aangemaakt, tenzij `Presentation.SuppressTrayIcon` in de spec is ingesteld.
3. Wacht `StartupTimeoutSeconds + 5` seconden op `Running`. De status wordt afgedrukt. Als de toestand niet `Running` is, exit `1` (`OmsiLaunchW.exe` toont `The OMSI session did not reach gameplay.` met de laatste `OL_E_`-diagnose of `OL_E_SESSION_START_FAILED`).
4. Validatiebatches (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) worden uitgevoerd en schrijven hun artefacten.
5. Het lokale control-endpoint start.
6. `/runtime:<operation>` wordt één keer uitgevoerd (5 s, 15 s voor `road-vehicles.spawn`); het resultaat wordt geschreven naar `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` en afgedrukt. Een mislukte runtimeopdracht beëindigt de sessie nooit (in plaats daarvan wordt `runtime_error` afgedrukt).
7. Wachten: met `/observe-seconds:n` wordt de sessie na `n` seconden gestopt, **of** eerder bij een stop via systeemvak/pipe, of wanneer OMSI afsluit; zonder deze vlag wacht de eigenaar tot OMSI afsluit of een stop wordt aangevraagd.
8. De eindstatus wordt afgedrukt; exit `0` bij `Completed`, anders `1`.

`session.stop`, "End session" in het systeemvak, Ctrl+C en `CloseAsync` vragen allemaal de canonieke stop aan: OMSI wordt beëindigd met `TerminateProcess` (de eigen afsluitroutine van OMSI wordt niet uitgevoerd en `options.cfg` wordt niet door OMSI herschreven), waarna elk bestand in bezit van de sessie wordt hersteld. Zie [levenscyclus van de sessie](../concepts/session-lifecycle.md) en [transacties en recovery](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Opdrachtwoorden

Elk woord dat op de eerste positie wordt geaccepteerd (`CliInput.CommandWordsAccepted`):

| Woord | Doel | Modus | Opmerkingen |
|---|---|---|---|
| `capabilities` | Openbare capabilities weergeven | Lokaal, geen sessie | Envelope `capabilities`. |
| `profiles` | Ondersteunde `Omsi.exe`-varianten weergeven | Lokaal, geen sessie | Envelope `profiles`. |
| `detect` | `Omsi.exe`-processen en een actieve eigenaar rapporteren | Lokaal, geen sessie | Ook de standaard als er geen argumenten worden opgegeven. Toestanden: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, per proces `UNKNOWN_BINARY_FOUND` als het binaire bestand niet kan worden geïnspecteerd; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Gebruik en openbare opdrachtcatalogus | Lokaal, geen sessie | `help <family>` filtert op capabilityfamilie (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Client | Precies één volgend woord; al het andere drukt de gebruikstekst af, exit `2`. `session plan`/`session start` zijn API-routenamen, geen CLI-woorden: gebruik `/plan` en `/new`. |
| `events` | `events read`, `events watch` | Client | `read` retourneert de begrensde lijst met events één keer; `watch` drukt elk nieuw event (op `Sequence`) elke 250 ms af als `events.watch`-envelope tot Ctrl+C (exit `0`), `4` als geen eigenaar antwoordt, `7` bij een control-fout. |
| `time` | `time get`, `time set` | Clientroute | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Clientroute | |
| `map` | `map get` | Clientroute | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Clientroute | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Clientroute | |
| `player` | `player get` | Clientroute | |
| `humans` | `humans list`, `humans get`, `humans summary` | Clientroute | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Clientroute | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Clientroute | |
| `constants` | `constants list`, `constants get` | Clientroute | |
| `curves` | `curves list`, `curves evaluate` | Clientroute | |
| `hof` | `hof get` | Clientroute | |
| `drivers` | `drivers list` | Clientroute | |
| `tickets` | `tickets get` | Clientroute | |
| `d3d` | Gereserveerd familiewoord | Geen | `d3d` heeft **geen hiërarchische route**: `d3d texture ...` is een onbekende route (exit `2`) en `d3d` alleen drukt de gebruikstekst af (exit `2`). D3D-operaties worden bereikt met `/runtime:d3d.status`, `/runtime:d3d.texture.create` enzovoort (zie [Operaties zonder route](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Hiërarchische routes

`CliInput.HierarchicalRoutes` koppelt een route in kleine letters aan een runtime-operatie-id. Alle routes vereisen een sessie in `Running` en worden uitgevoerd via de runtime-mailbox (`ExecuteRuntimeAsync`). Runtime-schrijfacties wijzigen alleen de toestand van OMSI in het geheugen: ze raken nooit bestanden aan, maken geen deel uit van de configuratietransactie en worden bij de stop **niet** teruggedraaid (OMSI wordt beëindigd). De stabiliteit volgt `PublicCapabilityRegistry` en de [validatiematrix](../status/runtime-validation-status.md); details en resultaatvelden staan in [runtimebesturing](runtime-control.md).

| Route | Runtime-operatie | Soort | Vereist Running | Wijzigt OMSI | Deelname aan herstel | Stabiliteit | Opmerkingen |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Ja | Nee | Geen | STABLE_BETA | Klok- en kalendervelden. |
| `time set` | `time.set` | Write | Ja | Ja (klok in het geheugen) | Geen, niet teruggedraaid | EXPERIMENTAL | Bijvoorbeeld `--minute=<0..59>`; schrijven, teruglezen en herstel gevalideerd op 2026-09-20. |
| `weather get` | `weather.read` | Read | Ja | Nee | Geen | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Ja | Ja (weer in het geheugen) | Geen, niet teruggedraaid | EXPERIMENTAL | Bijvoorbeeld `--wind_speed=5 --cloud_type="Cumulus 1"`. Handmatig weer: live METAR-updates stoppen. Runtimevalidatie in afwachting. |
| `weather metar set` | `weather.metar.set` | Write | Ja | Ja (weer in het geheugen; OMSI slaat de ICAO op in `options.cfg`, wat de sessie herstelt) | Geen, niet teruggedraaid | EXPERIMENTAL | `--icao=<ICAO>`. Live METAR-weer, zoals het weerdialoogvenster van OMSI het instelt. Runtimevalidatie in afwachting. |
| `weather actual get` | `weather.actual.read` | Read | Ja | Nee | Geen | EXPERIMENTAL | Toestand van de actual/ICAO-controller. |
| `map get` | `map.read` | Read | Ja | Nee | Geen | STABLE_BETA | Kaartnaam, bestand, beschrijving, aantal tegels, jaarbereik en rijzijde; opnieuw runtime-gevalideerd op het gecorrigeerde kaartslot. |
| `camera get` | `camera.read` | Read | Ja | Nee | Geen | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Ja | Ja (camerascalairen, bijv. `--field_of_view=`) | Geen, niet teruggedraaid | EXPERIMENTAL | Schrijven/teruglezen van FOV gevalideerd. |
| `camera lock` | `camera.lock` | Action | Ja | Ja (beleid binnen de sessie) | Geen | EXPERIMENTAL | Vereist `--family=<0..3>` (chauffeur=0, passagier=1, extern=2, kaart=3), optioneel `--preset=<n>` (familie 0 of 1). Heeft een spelersvoertuig nodig (bijvoorbeeld een opgeslagen situatie). Runtime-gevalideerd in de runtime-closure (`CAM01`); de `RuntimeValidation`-tekenreeks van het register zegt nog steeds `STATICALLY_VALIDATED` (zie [capabilities](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Ja | Ja | Geen | EXPERIMENTAL | Heft het beleid op dat door `camera lock` is ingesteld (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Ja | Nee | Geen | STABLE_BETA | Retourneert `rv-NNNNNN`-handles binnen de sessie. |
| `vehicles get` | `road-vehicle.read` | Read | Ja | Nee | Geen | STABLE_BETA | Vereist `--handle=`. Verouderde handle: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Ja | Nee | Geen | STABLE_BETA | Aantallen en spelerstoestand, geen handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Ja | Ja (voegt één RoadVehicle toe) | Geen, niet verwijderd | EXPERIMENTAL | Vereist `--model=Vehicles\...\*.bus`. Timeout van 30 s voor de client, 15 s voor de eigenaar. Wijst het spelersvoertuig niet toe. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Ja | Ja | Geen | EXPERIMENTAL | Geprofileerde `PlaceRandomBus`. |
| `player get` | `player-vehicle.read` | Read | Ja | Nee | Geen | STABLE_BETA | Semantische null als er geen spelersvoertuig is. |
| `humans list` | `humans.list` | Read | Ja | Nee | Geen | EXPERIMENTAL | Retourneert `hb-NNNNNN`-handles. |
| `humans get` | `human.read` | Read | Ja | Nee | Geen | EXPERIMENTAL | Vereist `--handle=`. |
| `humans summary` | `humans.read` | Read | Ja | Nee | Geen | EXPERIMENTAL | Alleen aantallen. |
| `timetable get` | `timetable.read` | Read | Ja | Nee | Geen | STABLE_BETA | Toestand van de dienstregelingmanager. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Ja | Nee | Geen | STABLE_BETA | Onderdeel van de capability `timetable.read`; batchleesbewijs 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable lines list` | `timetable.lines.list` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable tours list` | `timetable.tours.list` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable logs list` | `timetable.logs.read` | Read | Ja | Nee | Geen | STABLE_BETA | Zoals hierboven. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Ja | Nee | Geen | EXPERIMENTAL | Voertuignummers die OMSI buiten het vrije AI-gebruik houdt (gereserveerd door omlopen van de dienstregeling of vaste AI-lijsten). Gepagineerd (`cursor`, `limit`, hoogstens 256 per pagina). |
| `map tiles list` | `map.tiles.list` | Read | Ja | Nee | Geen | EXPERIMENTAL | De tiles van de kaart: rasterpositie, eigen index, bestand en laadtoestand, aantallen objecten, paden en splines. Gepagineerd (`cursor`, `limit`, hoogstens 256 per pagina). |
| `drivers list` | `drivers.read` | Read | Ja | Nee | Geen | EXPERIMENTAL | Chauffeursrecords. |
| `tickets get` | `tickets.read` | Read | Ja | Nee | Geen | EXPERIMENTAL | Records van kaartjespakketten. |
| `hof get` | `vehicle.hofs.read` | Read | Ja | Nee | Geen | STABLE_BETA | Vereist `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Ja | Nee | Geen | STABLE_BETA | Vereist `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Ja | Nee | Geen | STABLE_BETA | Vereist `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Ja | Nee | Geen | STABLE_BETA | Vereist `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Ja | Nee | Geen | STABLE_BETA | Vereist `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Ja | Nee | Geen | EXPERIMENTAL | Vereist `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Ja | Nee | Geen | EXPERIMENTAL | Vereist `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Ja | Ja (scriptvariabele) | Geen, niet teruggedraaid | EXPERIMENTAL | Vereist `--handle=`, `--name=`, `--value=` (eindig getal). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Ja | Nee | Geen | EXPERIMENTAL | Vereist `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Ja | Nee | Geen | EXPERIMENTAL | Vereist `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Ja | Ja (scripttoestand in het geheugen) | Geen, niet teruggedraaid | EXPERIMENTAL | Vereist `--handle=`, `--name=`, `--value=`. Het eigen `SetStringVariable` van OMSI. Runtimevalidatie in afwachting. |
| `scripts trigger` | `vehicle.trigger` | Action | Ja | Ja (voert voertuigscriptcode uit) | Geen, niet teruggedraaid | EXPERIMENTAL | Vereist `--handle=`, `--name=`, `--value=` (`1` indrukken, `0` loslaten, `2` slepen). Runtimevalidatie in afwachting. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Ja | Ja (speelt voertuiggeluiden af) | Geen, niet teruggedraaid | EXPERIMENTAL | Vereist `--handle=`, `--trigger=`; optioneel `--file=`. Runtimevalidatie in afwachting. |

<a id="operations-without-a-route"></a>
### Operaties zonder route

Deze openbare operatie-id's (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) hebben geen hiërarchische route en worden aangeroepen met `/runtime:<operation>` plus `--key=value` of `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` vereist; `levels` optioneel), `d3d.texture.describe` (`handle`; `level` optioneel), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` vereist; `level`, `x`, `y` optioneel), `d3d.texture.release` (`handle`). D3D-operaties zijn EXPERIMENTAL; de levenscyclus van textures en de invalidatie bij een apparaatreset zijn runtime-gevalideerd (runtime-closure `H02`, `D01`; zie [capabilities](capabilities.md)). `timetable.track-entries.list` en `timetable.tour-entries.list` zijn begrensde lijsten: een resultaat dat niet in het runtime-slot past, wordt ingekort (`truncated=true`). `internal.road-vehicles.make-basic` is INTERNAL en wordt door zowel de CLI als de API geweigerd met `OL_E_RUNTIME_OPERATION_UNKNOWN`.

<a id="flags"></a>
## Vlaggen

Elke vlag van `CliInput.KnownFlags`. "Fase" is *starttijd* (bepaalt de `LaunchSpec`/het plan van een nieuwe sessie), *runtime* (werkt op een actieve sessie) of *control* (verandert het gedrag van de CLI zelf). Vlaggen die alleen voor compatibiliteit worden geparseerd (`CliInput.AcceptedNoEffectFlags`), zijn op hun regel gemarkeerd.

<a id="control-and-output"></a>
### Besturing en uitvoer

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/?` | `/?` | uit | control | STABLE_BETA | Druk de gebruikstekst af, exit `0`. |
| `/help` | `/help` | uit | control | STABLE_BETA | Hetzelfde als `/?`. (Het losse woord `help` retourneert in plaats daarvan de gestructureerde catalogus.) |
| `/version` | `/version` | uit | control | STABLE_BETA | Envelope `version`, exit `0`. Wordt vóór elke andere opdracht geëvalueerd, behalve `/silent`. |
| `/json` | `/json` of `--json` | uit | control | STABLE_BETA | Geef JSON-envelopes uit; forceert ook console-uitvoer, zelfs onder `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | uit | control | STABLE_BETA | Onderdrukt de voor mensen leesbare succesuitvoer. JSON-uitvoer (`/json`) en fouten worden nog steeds geschreven; de exitcode verandert niet. |
| `/silent` | `/silent` (ook `--silent`) | uit | control | EXPERIMENTAL | Delegeer de volledige opdrachtregel aan `OmsiLaunchW.exe` en retourneer `0` zodra het hostproces is gestart. De uitkomst van de sessie wordt gerapporteerd door `OmsiLaunchW.exe` (berichtvensters, systeemvakpictogram), `.omsilaunch\diagnostics` en het lokale control-endpoint. De delegatie en de foutdialoogvensters zijn runtime-gevalideerd (runtime-closure `T04`); zie [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | control | REMOVED | Verwijderd in 0.1.0-beta4: geweigerd met `OL_E_INVALID_ARGUMENT` (exitcode `2`). De sessie-eigenaar serveert het lokale control-endpoint altijd. |
| `/verbose` | `/verbose` | uit | starttijd | PARTIAL | `DiagnosticsSpec.Verbose`. Waarden worden in de spec meegenomen; hun effect is beperkt tot de host-trace onder `.omsilaunch\diagnostics`. |
| `/log` | `/log` | aan (`DiagnosticsSpec.Log` staat standaard op `true`) | starttijd | PARTIAL | `DiagnosticsSpec.Log`. In de praktijk altijd aan. |
| `/logall` | `/logall` | uit | starttijd | PARTIAL | Stelt `Verbose`, `ProcessTrace`, `PluginTrace` en `NativeTrace` tegelijk in. |
| `/omsi-logall` | `/omsi-logall` | uit | starttijd | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | uit | starttijd | PARTIAL | Alias van `/trace-process`. |
| `/trace-process` | `/trace-process` | uit | starttijd | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | uit | starttijd | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: de plugin meldt elke runtime-operatie (`plugin.trace.operation`: operatie, uitkomst, code, duur) aan de hosttrace van de sessie. |
| `/trace-native` | `/trace-native` | uit | starttijd | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: de native laag meldt elke installatie en verwijdering van een hook (`native.trace`) aan de hosttrace van de sessie. |

<a id="planning-validation-and-harnesses"></a>
### Plannen, validatie en harnassen

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/plan` | `/plan` | uit | starttijd | STABLE_BETA | Bouw het `SessionPlan` en druk het af, zonder OMSI te starten. Exit `0` bij `IsRunnable`, anders `1`. Vereist een startselectie (`/new`, `/saved`, `/spec` of een installatieargument); `/plan` alleen, zonder iets anders, voert `detect` uit. |
| `/validate` | `/validate` | uit | starttijd | STABLE_BETA | Identiek aan `/plan` in deze build. |
| `/runtime-batch` | `/runtime-batch` | uit | runtime (eigenaar) | INTERNAL | Validatieharnas: voert na `Running` de set leesoperaties uit en schrijft `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | uit | runtime (eigenaar) | INTERNAL | Validatieharnas: leesacties plus `time.set`, `camera.set` en `vehicle.variable.set` met herstel; schrijft `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | uit | runtime (eigenaar) | INTERNAL | Validatieharnas voor de levenscyclus van D3D-textures; schrijft `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | geen | runtime | STABLE_BETA (dispatch) | Selecteer een openbare runtime-operatie op id. Clientmodus (geen installatieargument): doorgestuurd naar de eigenaar. Eigenaarmodus: één keer uitgevoerd na `Running`. Onbekende id's: `OL_E_RUNTIME_OPERATION_UNKNOWN`, exit `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (herhaalbaar) | geen | runtime | STABLE_BETA (dispatch) | Runtimeargument; gelijkwaardig aan `--key=value`. Ontbrekende `=`: `/runtime-arg requires key=value`, exit `2`. |

<a id="world-selection"></a>
### Wereldselectie

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` is de standaardmodus, maar een start wordt alleen aangevraagd als een van `/new`, `/saved`, `/last`, `/spec` aanwezig is | starttijd | STABLE_BETA | NEW_MAP. Vereist `/map` en `/entrypoint-index` (een plan zonder index van een gepresenteerd instappunt meldt `OL_E_ENTRYPOINT_REQUIRED`; zonder `/map` wordt geen kaart bepaald). `/new` selecteert nooit stilzwijgend een kaart. |
| `/saved` | `/saved:<file.osn>` | geen | starttijd | STABLE_BETA | SAVED_SITUATION. Kaart en positie komen uit het `.osn`-bestand; `/map`, `/entrypoint`, `/entrypoint-index` worden in combinatie met `/saved` geweigerd (exit `2`). Ontbrekende situatie: `OL_E_SITUATION_NOT_FOUND`; ontbrekende kaart van de situatie: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | geen | starttijd | EXPERIMENTAL | LAST_MAP_STATE: laadt de eigen `laststn.osn` van de kaart (door OMSI geschreven toen de kaart werd gesloten) via de last-state-tak van het Start-formulier. Vereist `/map`; een kaart zonder `laststn.osn` is `OL_E_SITUATION_NOT_FOUND`. Nooit een heuristiek op de nieuwste `.osn`. Runtimevalidatie in afwachting. |
| `/map` | `/map:<identity>` (bijvoorbeeld `maps\Grundorf\global.cfg`) | geen | starttijd | STABLE_BETA | Kaartidentiteit voor `/new`, of het bereik voor `/list:Entrypoints`. Onbekend: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | geen | starttijd | EXPERIMENTAL | Instappunt op zijn exacte (hoofdlettergevoelige) label of op een identiteit uit `/list:entrypoints`. OMSI toont alleen het eerste record van elk label; een later record met hetzelfde label wordt geweigerd (`OL_E_ENTRYPOINT_NOT_PRESENTED`), een onbekend record ook (`OL_E_ENTRYPOINT_NOT_FOUND`). Runtimevalidatie in afwachting. Sluit `/entrypoint-index` uit (de identiteit heeft voorrang en wist de index). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | geen | starttijd | STABLE_BETA | Index van het instappunt in de gepresenteerde lijst (op 1 gebaseerd, zoals OMSI de lijst presenteert). Vereist voor een uitvoerbaar NEW_MAP-plan. |

<a id="date-time-and-weather"></a>
### Datum, tijd en weer

Deze vlaggen worden geaccepteerd en in de `LaunchSpec` meegenomen. De startdatum, starttijd en het startjaar gelden alleen voor NEW_MAP-sessies: bij `/saved` of `/last` registreert de planner ze als `UNAVAILABLE` **en voegt `OL_E_CAPABILITY_UNAVAILABLE` toe, zodat het plan NIET UITVOERBAAR is (exit `1`)**, omdat een opgeslagen situatie haar eigen klok behoudt. Het weer geldt voor elke wereldmodus. Alle vlaggen zijn `EXPERIMENTAL`: het native startpad is statisch afgesloten en de runtimevalidatie is in afwachting. Een `/spec`-bestand of sessieprofiel dat ze instelt, heeft hetzelfde effect.

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` of `/date:system` | niet ingesteld | starttijd | EXPERIMENTAL | `DateSpec` expliciet/systeem: de startdatum van een NEW_MAP-sessie (zie [launchspec](launchspec.md#datespec-timespec-yearspec)). Niet-parseerbare waarde: `OL_E_INVALID_ARGUMENT`, exit `2`; een onmogelijke datum: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` of `/time:system` | niet ingesteld | starttijd | EXPERIMENTAL | `TimeSpec` expliciet/systeem: de starttijd van een NEW_MAP-sessie. |
| `/year` | `/year:<n>` of `/year:system` | niet ingesteld | starttijd | EXPERIMENTAL | `YearSpec`: het startjaar van een NEW_MAP-sessie (1900..2200). |
| `/weather` | `/weather:<preset>` | niet ingesteld | starttijd | EXPERIMENTAL | `WeatherMode.Preset`: een bestand in de map `weather` van de installatie, op naam (`Regen`) of pad (`weather\Regen.owt`); een ontbrekend bestand is `OL_E_WEATHER_INVALID` bij het plannen. Geladen via het eigen `loadWeather` van OMSI direct nadat de wereld start; live METAR blijft uit. Runtimevalidatie in afwachting. |
| `/weather-icao` | `/weather-icao:<code>` | niet ingesteld | starttijd | EXPERIMENTAL | `WeatherMode.Icao`: live METAR-weer voor een ICAO-station van vier tekens (anders `OL_E_WEATHER_INVALID`). OMSI slaat het station op in `options.cfg`, wat de sessie herstelt. Runtimevalidatie in afwachting. |
| `/weather-real` | `/weather-real` | niet ingesteld | starttijd | EXPERIMENTAL | `WeatherMode.RealCurrent`: live METAR-weer voor het station dat OMSI al heeft ingesteld; zonder station mislukt de start met `OL_E_WEATHER_APPLY_FAILED`. De laatste van `/weather`, `/weather-icao`, `/weather-real` heeft voorrang. Elk startweer overschrijft het weer van een opgeslagen situatie. |

<a id="player-vehicle"></a>
### Spelersvoertuig

Geaccepteerd en bepaald aan de hand van de installatie, maar niet toegepast door de runtime: elk ingesteld veld is `UNAVAILABLE` en voegt `OL_E_CAPABILITY_UNAVAILABLE` toe (plan NIET UITVOERBAAR, exit `1`).

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | niet ingesteld | starttijd | UNAVAILABLE | Wordt als eerste bepaald (`OL_E_VEHICLE_NOT_FOUND` als onbekend). |
| `/repaint` | `/repaint:<id>` | niet ingesteld | starttijd | UNAVAILABLE | Wordt alleen samen met `/vehicle` bepaald (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | niet ingesteld | starttijd | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` als onbekend. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | geen | starttijd | STABLE_BETA | Eén sessiebewerking voor [HOF-beheer](hof-management.md): `HofSourceKind` en `Source`. Vereist minstens één `/hof-target`. Vervangt de `HofManagement` van een `/spec`; met een sessieprofielpreset die `hofs` heeft: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Onjuist gevormd: exit `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (herhaalbaar) | geen | starttijd | STABLE_BETA | Een doelvoertuig onder `Vehicles\`. Zonder `/hof-source`: exit `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (herhaalbaar) | alle bestanden | starttijd | STABLE_BETA | Beperkt de bron tot deze bestandsnamen. Zonder `/hof-source`: exit `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | starttijd | STABLE_BETA | `HofApplyMode`. Zonder `/hof-source`: exit `2`. |
| `/module` | `/module:<id>` (herhaalbaar) | geen | starttijd | EXPERIMENTAL | Vraagt een [runtimemodule](runtime-modules.md) aan voor de sessie (`LaunchSpec.Modules.Enable`, toegevoegd aan de modules van een `/spec`-bestand). Een aangevraagde module die niet kan worden geladen, maakt het plan niet uitvoerbaar met zijn `OL_E_MODULE_*`-code (exit `1`). De operaties ervan worden bereikt met `/runtime:module.<id>.<operation>` en `/runtime-arg`. Met een sessieprofielpreset die er `enabled: false` voor instelt: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (herhaalbaar) | geen | starttijd | EXPERIMENTAL | Houdt een module buiten de sessie, ook een module die standaard, door zijn kaart of door het instellingenbestand wordt geladen (`LaunchSpec.Modules.Disable`). Met een preset die er `enabled: true` voor instelt: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Zonder waarde: exit `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (herhaalbaar) | geen | starttijd | EXPERIMENTAL | Stelt een moduleinstelling in (`LaunchSpec.Modules.Settings`); heeft voorrang op het `/spec`-bestand, het instellingenbestand en de standaardwaarde. Een sleutel die ook de geselecteerde preset instelt: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Niet van de vorm `<id>.<key>=<value>`: exit `2`. Ongeldige waarde of niet gedeclareerde sleutel: plandiagnose `OL_E_MODULE_SETTING_INVALID` (exit `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` alleen betekent `true` voor een booleaanse instelling | geen | starttijd | EXPERIMENTAL | Een vlag die een geïnstalleerde module declareert voor een van zijn instellingen (`flag` in zijn manifest; weergegeven door `/list:RuntimeModules`). Hij wordt bepaald wanneer een sessie wordt gepland en is gelijk aan `/module-setting:<id>.<key>=<value>`. Een vlag die geen geïnstalleerde module declareert, of een vlag buiten een start: `Unknown argument`, exit `2`. Een vlag die twee modules declareren: `OL_E_MODULE_SETTING_INVALID`, exit `2`. Een modulevlag vervangt nooit een ingebouwde vlag. |
| `/fleet` | `/fleet:<n>` | niet ingesteld | starttijd | UNAVAILABLE | Wagennummer. |
| `/registration` | `/registration:<text>` | niet ingesteld | starttijd | UNAVAILABLE | Kenteken. |
| `/no-vehicle` | `/no-vehicle` | uit | starttijd | STABLE_BETA | Wist elk spelersvoertuig uit de basis (`/spec` of profiel). Onschadelijk. |

<a id="configuration-overlays"></a>
### Configuratie-overlays

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (herhaalbaar; sleutels niet hoofdlettergevoelig) | geen | starttijd | STABLE_BETA | Semantische `options.cfg`-overlay uit `ConfigurationCatalog` (bijvoorbeeld `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Onbekende sleutel: `OL_E_UNKNOWN_SETTING` (exit `2`); alleen-lezen sleutel (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (exit `2`); waarde buiten bereik of ongeldig gevormd: `OL_E_INVALID_SETTING_VALUE` bij het opbouwen van de overlay. De overlay is een sessiewijziging: er wordt een snapshot van gemaakt, hij wordt toegepast voordat OMSI start en bij de stop byte voor byte hersteld (RV-005 `RUNTIME_VALIDATED`). Conflicten met een sleutel die eigendom is van een preset van een geselecteerd profiel: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Presentatie van het opstartscherm

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (niet hoofdlettergevoelig) | `Managed` | starttijd | STABLE_BETA | `Managed`: de meegeleverde 24-bits BMP's van 640x480 worden één keer gekopieerd naar `<root>\.omsilaunch\assets\splash`, en `GUI\NewSplashscreen_ENG.bmp` plus `GUI\NewSplashscreen_<lang>.bmp` worden transactioneel als overlay aangebracht en exact hersteld (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (aliassen): OMSI-bestanden blijven onaangeroerd. Ontbrekende waarde: `/splash requires Unset, Native, or Managed`, exit `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (ook `pt-BR`, `de`, `fr`, `en`; al het andere valt terug op `ENG`) | `[language]` uit `options.cfg`, anders `ENG` | starttijd | STABLE_BETA | Selecteert het gelokaliseerde doelbestand. |
| `/splash-assets` | `/splash-assets:<directory>` (relatieve paden worden onder de installatiemap bepaald) | `<root>\.omsilaunch\assets\splash`, anders de meegeleverde set | starttijd | STABLE_BETA | Aangepaste assetmap; moet `ENG.bmp` bevatten en, voor een niet-Engelse taal, `<lang>.bmp`. Fouten: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (in het plan gemeld als `OL_E_SESSION_PRESENTATION_INVALID`; niet uitvoerbaar). |

<a id="internet-textures"></a>
### Internettextures

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | starttijd | EXPERIMENTAL | `Native`: onaangeroerd. `Disabled`: de geprofileerde downloader binnen het proces wordt onderdrukt. `Override`: het opgegeven `.itx`-profiel wordt als overlay `Texture\standard.itx` aangebracht; elk daarin vermeld HTTP(S)-doel plus `Texture\standard.ipr` worden sessieverwijderingen (voor de sessie verwijderd, bij de stop hersteld). Ontbrekende waarde: exit `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | geen | starttijd | EXPERIMENTAL | Vereist bij `Override` (`OL_E_ITX_PROFILE_REQUIRED`, exit `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (moet bestaan uit paren van URL- en doelregels met `http`/`https`-URL's), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (doelen moeten onder `Texture\` uitkomen, zonder absolute paden, `..` of reparse points). |

<a id="session-profiles"></a>
### Sessieprofielen

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | geen | starttijd | STABLE_BETA (compilatie; offline `OmsiLaunch.ProfileTests`) | Laadt `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (zie [sessieprofielen](session-profiles.md)). Vereist `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, exit `2`). Het `new:`-blok geldt alleen met `/new`; `compatibility.maps` wordt afgedwongen voor `/new` en `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Expliciete vlaggen die botsen met een veld dat eigendom is van het profiel, worden geweigerd met `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): kaart/instappunt/datum/tijd/jaar/weer wanneer het `new:`-blok die bezit, `/set`-sleutels die eigendom zijn van de preset, opstartschermvlaggen wanneer de preset `presentation` heeft, internettexturevlaggen wanneer hij `internet-textures` heeft, timeouts wanneer hij `behavior` heeft. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | geen | starttijd | STABLE_BETA | Selecteert de preset op `index`. Buiten bereik: exit `2`. |

<a id="launchspec-file"></a>
### LaunchSpec-bestand

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | geen | starttijd | STABLE_BETA (loader offline getest; sessiesemantiek identiek aan vlaggen) | Laadt een `LaunchSpec`-JSON-bestand als basis (zie [LaunchSpec](launchspec.md)) en markeert dat een start is aangevraagd. Regels (`LaunchSpecJson`): het bestand moet bestaan (`OL_E_SPEC_NOT_FOUND`, exit `6`); maximaal 1 MiB (`OL_E_SPEC_TOO_LARGE`, exit `2`); de root moet een object zijn (`OL_E_SPEC_INVALID`); eigenschapsnamen niet hoofdlettergevoelig; `//`-commentaar en afsluitende komma's toegestaan; diepte maximaal 32; elke onbekende eigenschap wordt geweigerd met haar JSON-pad (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, exit `2`). |

**Voorrang** (`CliInput.BuildSpecAsync`): standaardwaarden → `/spec`-bestand → `/predefined-profile` (vervangt `Installation` en `World` en past daarna het profiel toe) → expliciete vlaggen. Een expliciet installatieargument gaat vóór `RootPath` in de spec. `/no-vehicle` wist het spelersvoertuig van de spec; `/vehicle` en verwante vlaggen worden er veld voor veld in samengevoegd. `/set`-sleutels worden samengevoegd in `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` overschrijven alleen als ze zijn opgegeven. `/startup-timeout` en `/shutdown-timeout` overschrijven alleen als ze zijn opgegeven; `Presentation.SuppressTrayIcon` komt alleen uit de spec (geen vlag). Diagnosevlaggen worden met OR gecombineerd met de `Diagnostics` van de spec.

<a id="content-discovery"></a>
### Inhoud ontdekken

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; categorieën zijn de `ContentQueryKind`-waarden `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (niet hoofdlettergevoelig) | geen | lokaal, geen sessie | STABLE_BETA | `DiscoverAsync` over de installatie; envelope `content.list` met items `Identity`, `Kind`, `DisplayName`; exit `0`. `SessionProfiles` antwoordt met `ListSessionProfilesAsync` (envelope `session-profiles.list`) en `RuntimeModules` met `ListRuntimeModulesAsync` (envelope `runtime-modules.list`: id, versie, activering, operaties en geldigheid van elke module en zijn instellingen met type, standaardwaarde en vlag). Onbekende categorie: `Unknown discovery category`, exit `2`. Reparse points (junctions/symlinks) worden overgeslagen, OMSI-bestanden worden gelezen als Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | geen | lokaal | STABLE_BETA | Bereik dat wordt doorgegeven voor elke categorie behalve `Entrypoints`, die `/map` als bereik gebruikt. |

<a id="timeouts-and-observation"></a>
### Timeouts en observatie

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` seconden | waarde uit spec/profiel, anders `180` | starttijd | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. De eigenaar wacht deze waarde plus 5 s op `Running`; `OL_E_STARTUP_TIMEOUT` beëindigt de sessie met exit `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` seconden | waarde uit spec/profiel, anders `30` | starttijd | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: met `/cooperative-shutdown` hoe lang de supervisor wacht tot OMSI afsluit voordat het wordt beëindigd; zonder die vlag wordt OMSI direct beëindigd en heeft de waarde geen effect. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | uit | starttijd | EXPERIMENTAL | `Behavior.CooperativeShutdown`: het beëindigen van een actieve sessie vraagt OMSI af te sluiten via zijn eigen afsluitroutine (die na bevestiging van de gebruiker draait: ze slaat `options.cfg` en de `laststn.osn` van de kaart op en sluit dan af) en wacht `ShutdownTimeoutSeconds` voordat OMSI wordt beëindigd. Opt-in totdat het runtime-gevalideerd is; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` wanneer OMSI moest worden beëindigd. De bestanden van de sessie worden in beide gevallen na het afsluiten hersteld. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | geen (actief tot OMSI afsluit of een stop wordt aangevraagd) | runtime (eigenaar) | STABLE_BETA | Bovengrens voor de actieve fase: na `n` seconden in `Running` wordt de canonieke stop aangevraagd. Een stop via systeemvak of pipe, of het afsluiten van OMSI, beëindigt de fase eerder. `0` stopt direct na `Running`. |

### Recovery

| Vlag | Syntaxis en waarden | Standaard | Fase | Stabiliteit | Gedrag |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | uit | lokaal | STABLE_BETA | Rapporteert of `<root>\.omsilaunch\journal.json` openstaat (`pending`), herstelt nooit; exit `0`. Neemt de installatielease: `OL_E_INSTALLATION_BUSY` (exit `7`) zolang een eigenaar die vasthoudt. |
| `/recover` | `/recover` | uit | lokaal | STABLE_BETA | Herstelt een openstaand journal (back-ups worden eerst gecontroleerd tegen de SHA-256 van de snapshot; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` worden gemeld in `diagnostics`). Exit `0` als er niets openstond of het herstel is voltooid; `8` als er een journal openstond en dat blijft openstaan. Geweigerd met `OL_E_INSTALLATION_BUSY` zolang het in het journal vastgelegde OMSI-proces (PID, aanmaaktijd, exe-pad) actief is, of, voor een journal voorbij `HandoffCreated` zonder PID, zolang enige `Omsi.exe` uit die root actief is. Elke sessiestart voert dezelfde recovery automatisch uit voordat de installatie wordt gelezen. |
| `/clear-module-data` | `/clear-module-data:<module id>` | geen | lokaal, geen sessie | EXPERIMENTAL | Verwijdert de gegevensmap van de module `<root>\.omsilaunch\module-data\<id>` en niets anders; envelope `module-data.clear` met `module` en `cleared` (`false` als er niets te verwijderen was); exit `0`. Neemt de installatielease: `OL_E_INSTALLATION_BUSY` (exit `7`) zolang een sessie de installatie bezit. Een gegevensmap van een module die via een link wordt bereikt, wordt geweigerd (`OL_E_MODULE_INTEGRITY`); een link erin wordt als link verwijderd. |

<a id="output-formats"></a>
## Uitvoerformaten

- **Envelope bij succes** (`CliInput.WriteEnvelope`, met `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, ingesprongen. Doorgestuurde antwoorden op `session status` en `events read` krijgen een extra lid `metadata` wanneer oudere events zijn weggelaten om in het control-frame te passen (`events_dropped_count`, zie [local control](local-control.md)). Zonder `--json` wordt alleen `<object>` als ingesprongen JSON afgedrukt, gevolgd door `Note: <n> older events were omitted to fit the control frame.` wanneer events zijn weggelaten.
- **Envelope bij fout** (`CliInput.WriteError`, met `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Zonder `--json`: `OL_E_<CODE>: message` op één regel. Categorieën: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Onder `OmsiLaunchW.exe` worden dezelfde code en hetzelfde bericht in een berichtvenster getoond.
- **Plan en status** (`CliInput.Write`): de records `SessionPlan`, `SessionStatus` en `RuntimeCommandResult` worden als ingesprongen JSON **zonder** envelope afgedrukt. Zonder `--json` wordt een plan samengevat als `Plan: READY profile=Omsi23004_692EBFBF` of `Plan: NOT RUNNABLE profile=...`; andere records worden nog steeds als JSON afgedrukt. Enumwaarden worden als gehele getallen geserialiseerd (`SessionState.Running` is `14`, `Completed` is `18`, `Failed` is `19`).
- Opdrachtnamen die in envelopes worden gebruikt: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile`, en de runtime-operatie-id voor doorgestuurde runtimeopdrachten.
- Onder `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) wordt niets naar de console geschreven, tenzij `--json` is opgegeven.

<a id="errors-per-command"></a>
## Fouten per opdracht

| Opdracht | Gebruikelijke foutcodes | Exit |
|---|---|---|
| Elke parseerfout | `OL_E_INVALID_ARGUMENT`, sessieprofielcodes (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Clientroute, `/runtime` (client) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` geretourneerd door de eigenaar, bijv. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Voorafgaande controle van de eigenaar | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); openstaand maar niet hersteld (`8`) | `7`, `8` |
| `/list` | onbekende categorie (`2`); `OL_E_INSTALLATION_NOT_FOUND`/ontbrekende mappen (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, start | plandiagnoses: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (geïnstalleerde plugin-closure), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informatief) | `1` |
| Sessiestart | `OL_E_PLAN_NOT_RUNNABLE` (opnieuw plannen bij de start, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normaal door het plannen gemeld als plandiagnose, exit `1`; `7` alleen als de plugin-bestanden tussen plannen en starten veranderen), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (sessie `Failed`, `1`) | `1`, `7` |
| Onafgehandelde exception, waar dan ook | geclassificeerd door `CliProgram.Classify` (zie [exitcodes](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Omgeving

| Variabele | Ingesteld door | Effect |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: console-uitvoer onderdrukt, fouten als berichtvensters, `/silent` niet opnieuw gedelegeerd. |

<a id="see-also"></a>
## Zie ook

[CLI-voorbeelden](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [exitcodes](exit-codes.md) · [fouten](errors.md) · [local control](local-control.md) · [Windows-systeemvak](windows-tray.md) · [runtimebesturing](runtime-control.md) · [capabilities](capabilities.md) · [LaunchSpec](launchspec.md) · [sessieprofielen](session-profiles.md) · [verpakking](packaging.md) · [compatibiliteit](compatibility.md) · [bekende beperkingen](known-limitations.md) · [openbare API](public-api.md)
