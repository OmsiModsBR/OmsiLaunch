# LaunchSpec-referentie

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/launchspec.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina is de normatieve referentie voor `LaunchSpec`, het aanvraagrecord dat één OmsiLaunch-sessie beschrijft: de C#-vorm (`OmsiLaunch.Api`), de JSON-bestandsvorm zoals de CLI die laadt (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), elke eigenschap met type, standaardwaarde, validatieregel en huidig effect, de validatieregels die een plan niet uitvoerbaar maken, en de voorrang tussen CLI-vlaggen, spec-bestanden en sessieprofielen. Alleen wat de huidige code doet, is gedocumenteerd.

Gerelateerde pagina's: [openbare API](public-api.md), [CLI-referentie](cli.md), [sessieprofielen](session-profiles.md), [foutcodes](errors.md), [sessielevenscyclus](../concepts/session-lifecycle.md), [capabilities](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## Waar een LaunchSpec vandaan komt

| Bron | Hoe het een `LaunchSpec` wordt |
| --- | --- |
| API | De integrator stelt het record samen en geeft het door aan `PlanSessionAsync`. |
| CLI-vlaggen | `CliInput.BuildSpecAsync` begint met ingebouwde standaardwaarden (NEW_MAP, alles niet ingesteld, standaardwaarden van `Behavior`) en past de vlaggen toe. |
| JSON-bestand `/spec:<path>` | Geladen door `LaunchSpecJson.LoadAsync` en daarna gebruikt als basis die door CLI-vlaggen wordt overschreven (zie [voorrang](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Sessieprofiel (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` schrijft de wereld, instellingen, presentatie, internettextures en het gedrag van het profiel in de basis en legt de metagegevens van `SessionProfile` vast. |

Het [volledige voorbeeld](#complete-example) hieronder is gecontroleerd tegen de vorm van het record. Een kopie van het minimale voorbeeld wordt meegeleverd als `examples/release-session.example.json` (en `.omsilaunch\examples\release-session.example.json` in het releasepakket).

<a id="json-form"></a>
## JSON-vorm

| Regel | Details |
| --- | --- |
| Serializer | `System.Text.Json` met `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; er zijn geen converters geregistreerd. |
| Eigenschapsnamen | De C#-eigenschapsnamen (`Installation`, `RootPath`, ...). Bij het laden is de vergelijking niet hoofdlettergevoelig; de CLI schrijft ze in PascalCase. |
| Enums | Gehele getallen (er is geen converter voor string-enums). `"Mode": 0` is geldig; `"Mode": "NewMap"` wordt geweigerd als ongeldige JSON. De waarden staan in [Enumeraties](#enumerations). |
| `OptionalValue<T>` | Een object `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (de waarde wordt genegeerd), 1 = `Set` (de waarde moet aanwezig zijn en mag niet null zijn; een `Set` met een null-waarde wordt niet gevalideerd en gedraagt zich als een ongeldige waarde). Een weggelaten `OptionalValue`-lid is `Unset`. Het alleen-lezen lid `IsSet` verschijnt in uitvoer die de CLI schrijft en wordt bij het laden geaccepteerd en genegeerd. |
| Optionele records | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile`, `HofManagement`, `Modules` mogen `null` zijn of weggelaten worden; de `Effective*`-accessors vullen standaardwaarden in. |
| Verplichte records | `Installation`, `World`, `Date`, `Time`, `Environment` (met alle acht dictionaries, gebruik `{}`), `Behavior` moeten als object aanwezig zijn. Ze worden niet gevalideerd: een `null` of ontbrekend record mislukt later met een null-referentie, die de CLI meldt als `OL_E_INTERNAL` (exitcode 10) of `OL_E_INVALID_ARGUMENT` (exitcode 2). |
| Onbekende eigenschappen | Geweigerd vóór het binden: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (het pad gebruikt de lidnamen zoals ze in het bestand staan). De inhoud van dictionaries (`Environment.*`) wordt niet als eigenschappen gecontroleerd. |
| Hoofdobject | Moet een JSON-object zijn: `OL_E_SPEC_INVALID`. Maximale nestingdiepte 32. |
| Bestandsgrootte | Maximaal 1 MiB (1 048 576 bytes): `OL_E_SPEC_TOO_LARGE`. Ontbrekend bestand: `OL_E_SPEC_NOT_FOUND`. |
| Commentaar en afsluitende komma's | Commentaar met `//` en `/* */` en afsluitende komma's worden geaccepteerd. |
| Ongeldige JSON | De parserexceptie wordt niet vertaald: de CLI meldt `OL_E_INTERNAL` met exitcode 10. |
| Codering | UTF-8 (een BOM wordt door de lezer getolereerd). Backslashes in identiteiten moeten worden ge-escapet (`"maps\\Grundorf\\global.cfg"`); voorwaartse slashes worden geaccepteerd voor identiteiten van kaarten, situaties, voertuigen en HOF-bestanden. |

<a id="complete-example"></a>
## Volledig voorbeeld

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // optional pin of Omsi.exe
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // must be true
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30,             // with CooperativeShutdown
    "CooperativeShutdown": false              // opt-in
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

Een expliciete datum wordt, als de build dat ondersteunt, geschreven als `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` en een tijd als `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. Beide gelden alleen voor NEW_MAP-sessies (`EXPERIMENTAL`; zie hieronder).

<a id="property-reference"></a>
## Eigenschappenreferentie

De kolom "Verwerkt" vermeldt wat de huidige code met de waarde doet. Stabiliteit gebruikt de terminologie van de [pagina over de openbare API](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (hoofdniveau)

| Eigenschap | JSON-type | Verplicht | Standaard bij weglaten | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Installation` | object `InstallationSpec` | ja | geen | ja | `STABLE_BETA` |
| `World` | object `WorldSpec` | ja | geen | ja | `STABLE_BETA` |
| `Date` | object `DateSpec` | ja | geen | ja, alleen NEW_MAP (zie [`DateSpec`](#datespec-timespec-yearspec)) | `EXPERIMENTAL` |
| `Time` | object `TimeSpec` | ja | geen | ja, alleen NEW_MAP | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | nee | `Unset` | opgelost voor diagnose; elk ingesteld veld is niet uitvoerbaar | `PARTIAL` |
| `Environment` | object `EnvironmentSpec` | ja | geen | ja (semantische overlay van `options.cfg`) | `STABLE_BETA` |
| `Behavior` | object `LaunchBehaviorSpec` | ja | geen | gedeeltelijk (zie record) | `STABLE_BETA` / `PARTIAL` |
| `Year` | object `YearSpec` of null | nee | `null` → `EffectiveYear` = modus `Unset` | ja, alleen NEW_MAP | `EXPERIMENTAL` |
| `Weather` | object `WeatherSpec` of null | nee | `null` → `EffectiveWeather` = modus `Unset` | ja, elke wereldmodus, toegepast vlak nadat de wereld start (zie [`WeatherSpec`](#weatherspec)) | `EXPERIMENTAL` |
| `Input` | object `InputSpec` of null | nee | `null` → `EffectiveInput` = beide niet ingesteld | ja (zie [`InputSpec`](#inputspec)) | `EXPERIMENTAL` |
| `Diagnostics` | object `DiagnosticsSpec` of null | nee | `null` → `EffectiveDiagnostics` = standaardwaarden | alleen meegenomen | `PARTIAL` |
| `Presentation` | object `SessionPresentationSpec` of null | nee | `null` → `EffectivePresentation` = beheerd opstartscherm, geen taal, geen aangepaste map, systeemvakpictogram zichtbaar | ja | `STABLE_BETA` |
| `InternetTextures` | object `InternetTexturesSpec` of null | nee | `null` → `EffectiveInternetTextures` = `Native` | ja | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | object `SessionProfileMetadata` of null | nee | `null` | alleen herkomst (plandiagnose `session_profile.selected`) | `STABLE_BETA` |
| `HofManagement` | object `HofManagementSpec` of null | nee | `null` → `EffectiveHofManagement` = geen bewerkingen | ja (HOF-overlays van de sessie, zie [HOF-beheer](hof-management.md)) | `STABLE_BETA` |
| `Modules` | object `RuntimeModulesSpec` (`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`) of null | nee | `null` → `EffectiveModules` = niets aangevraagd, uitgeschakeld of ingesteld | nee (modules raken via OmsiLaunch nooit installatiebestanden aan; zie [runtimemodules](../../../reference/runtime-modules.md)) | `EXPERIMENTAL` |

Alleen-lezen accessors (aanwezig in JSON-uitvoer van de CLI, genegeerd bij het laden): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, `EffectiveHofManagement`, `EffectiveModules`.

### `InstallationSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | verplicht | Map met `Omsi.exe` en `plugins\`. Zie [padregels](#path-rules). Leeg/alleen witruimte → `OL_E_INSTALLATION_NOT_FOUND`. | ja | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string of null | `null` | `null`, of 64 hexadecimale cijfers (anders `OL_E_INVALID_ARGUMENT`). | Optionele pin: wanneer ingesteld, is het plan niet uitvoerbaar tenzij de SHA-256 van `Omsi.exe` ermee overeenkomt (`OL_E_UNSUPPORTED_BUILD`, gegevens `expected_sha256` en `file_sha256`). | `STABLE_BETA` |

### `WorldSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | verplicht | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` is een verouderde alias met dezelfde waarde 2). | ja; `LastMapState` vereist `MapIdentity` en de `laststn.osn` van de kaart (`OL_E_MAP_NOT_FOUND`, `OL_E_SITUATION_NOT_FOUND`) | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Voor `NewMap`: verplicht, in de vorm `maps\<dir>\global.cfg` (niet hoofdlettergevoelig, `/` geaccepteerd, geen `..`) en geïnstalleerd. Genegeerd voor `SavedSituation` (de `.osn` levert de kaart). | ja (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Voor `SavedSituation`: verplicht, een geïnstalleerde identiteit `situations\...\<file>.osn` (zoals geretourneerd door `DiscoverAsync(Situations)` / `/list:situations`). | ja (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Voor `NewMap` zonder `EntrypointIdentity`: verplicht, `>= 0`, een index in de door OMSI getoonde lijst met instappunten van de kaart. Wordt als `-1` naar de plugin gestuurd wanneer niet ingesteld. | ja (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Een exact (hoofdlettergevoelig) label van een instappunt of een discovery-identiteit. De planner zet het om naar het label dat OMSI toont en de start selecteert dat label (`world.entrypoint-identity`); een record dat OMSI niet toont, wordt geweigerd (`OL_E_ENTRYPOINT_NOT_PRESENTED`), een onbekend record ook (`OL_E_ENTRYPOINT_NOT_FOUND`). | meegenomen | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec` (alleen-lezen) | berekend | `Mode` = `Identity` wanneer `EntrypointIdentity` is ingesteld, anders `PresentedIndex` wanneer de index is ingesteld, anders `Unset`; `PresentedIndex`, `Identity` weerspiegelen de invoer. | afgeleid | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | verplicht (`Year`: `0` wanneer het record null is) | `0` `Unset`, `1` `Explicit`, `2` `System`. | NEW_MAP: `Explicit` gebruikt `Value`, `System` de pc-klok op het moment dat de sessie wordt gepland; `Unset` behoudt de eigen waarde van OMSI (de opties en het Start-formulier). De controller zet de aanvraag om in de startklok van OMSI (jaar, dag van het jaar, uur, minuut, seconde; capabilities `world.explicit-date`, `world.explicit-time`, `world.explicit-year`), de opstart-handoff (versie 7) draagt die mee en de plugin schrijft haar vlak vóór `NewSituation` van OMSI, zodat OMSI het seizoen en de kalender afleidt van de aangevraagde datum voordat de kaart wordt geladen. Na de start wordt de lopende klok teruggelezen; een afwijking, of als OMSI een vaste starttijd gebruikt, laat de start mislukken met `OL_E_DATE_TIME_APPLY_FAILED`. `SavedSituation`: een andere modus dan `Unset` is `OL_E_CAPABILITY_UNAVAILABLE` (een situatie draagt haar eigen tijd). | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: een echte kalenderdatum (`Day` geldig voor de maand en het jaar); `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59; jaren 1900..2200. `YearSpec` stelt het jaar in (en het jaar van `Date`, wanneer beide zijn opgegeven). 31 december van een schrikkeljaar wordt geweigerd: de kalender van OMSI kan dag 366 niet weergeven. Ongeldige waarden: `OL_E_DATE_TIME_APPLY_FAILED`. Moet ingesteld zijn wanneer `Mode` gelijk is aan `Explicit` en anders niet ingesteld (`OL_E_INVALID_ARGUMENT`). | ja | `EXPERIMENTAL` |

### `WeatherSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0` wanneer het record null is | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Elke modus behalve `Unset` vereist `world.weather`; de plugin past het toe vlak nadat de wereld start (elke wereldmodus; het overschrijft het weer van een opgeslagen situatie). Een fout laat de start mislukken met `OL_E_WEATHER_APPLY_FAILED`. | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Een bestand in de map `weather` van de installatie, op naam of `weather\<name>.owt`; gecontroleerd bij het plannen (`OL_E_WEATHER_INVALID`). | handoff | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Vier letters of cijfers (anders `OL_E_WEATHER_INVALID`); gebruikt door de modus `Icao`. | handoff | `EXPERIMENTAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (binnen `PlayerVehicle`)

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Geïnstalleerde identiteit `Vehicles\...\<file>.bus`, anders `OL_E_VEHICLE_NOT_FOUND`. | opgelost in `ResolvedContent`; daarna niet-ondersteund `player-vehicle.model` → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Een repaint-identiteit van `Model` (`<cti>#item:<n>`), anders `OL_E_REPAINT_NOT_FOUND`; wordt alleen gecontroleerd wanneer `Model` is ingesteld. | idem | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | Geïnstalleerd `Vehicles\...\<file>.hof`, anders `OL_E_HOF_NOT_FOUND`. | idem | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Elke string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Elke string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (alleen-lezen) | berekend | `true` wanneer `Model` is ingesteld. `PlayerVehicle.IsSet` is wat de handoff meeneemt als `PlayerVehicleEnabled`. | afgeleid | `PARTIAL` |

Een `PlayerVehicle` met `Presence` 1 en alle velden niet ingesteld wordt geaccepteerd en heeft geen effect. Elk ingesteld veld maakt het plan in deze build niet uitvoerbaar (`UNAVAILABLE`).

### `EnvironmentSpec`

| Eigenschap | Type | Standaard | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | elk `IReadOnlyDictionary<string, OptionalValue<string>>`, verplicht (`{}` wanneer leeg) | geen | ja | `STABLE_BETA` |

De acht groepen worden samengevoegd; de groep waarin een sleutel staat, heeft geen effect. Elke vermelding met `Presence` 1 is een semantische instelling uit `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); de sleutel bepaalt het doelbestand (`options.cfg` voor elke huidige sleutel) en het token. Bij het plannen wordt gecontroleerd of de sleutel bestaat (`OL_E_UNKNOWN_SETTING`) en schrijfbaar is (`OL_E_SETTING_NOT_WRITABLE`); de waarde wordt pas bij de start gevalideerd (`OL_E_INVALID_SETTING_VALUE`, gemeld als een `Failed`-sessie met `OL_E_START_SESSION`). Sleutels zijn niet hoofdlettergevoelig. Niet-ingestelde vermeldingen worden genegeerd. De CLI-vlag `/set:<key>=<value>` schrijft naar `General`; `settings` uit sessieprofielen worden eveneens samengevoegd in `General`.

| Sleutel | Waarde | Opmerkingen |
| --- | --- | --- |
| `general.language` | string | token `[language]` |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | aanwezigheidstokens (`autoSave` is het omgekeerde van `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | geheel getal 10..200 | |
| `graphics.tileDistance` | geheel getal 1..20 | |
| `graphics.maxObjectDistanceMeters` | getal 20..5000 | |
| `graphics.minObjectScreenPercent` | getal 0..10 | opgeslagen gedeeld door 100 |
| `graphics.minReflectionObjectScreenPercent` | getal 0..50 | opgeslagen gedeeld door 100 |
| `graphics.maxObjectComplexity` | geheel getal 0..3 | |
| `graphics.maxMapComplexity` | geheel getal 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | aanwezigheidstokens |
| `graphics.stencilShadows` | `true` / `false` | geschreven als `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | één `smokesystems`-blok |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | aanwezigheidstokens |
| `simulation.ticketSelling` | geheel getal 0..2 | |
| `simulation.maintenance` | geheel getal 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | twee OMSI-tokens tegelijk (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | aanwezigheidstokens |
| `traffic.randomVehicles` | geheel getal 0..1000 | component 0 van het meerregelige blok `AIMaxCountRandom` (runtime-gevalideerd, matrix RV-005) |
| `traffic.humans` | geheel getal 0..1000 | component 1 van `AIMaxCountRandom` |
| `traffic.factorPercent` | getal 1..300 | |
| `traffic.parkedVehiclesPercent` | getal 0..100 | |
| `traffic.scheduledVehicles` | getal 0..1000 | |
| `traffic.scheduledLinePriority` | getal 1..4 | |
| `traffic.passengerFactorPercent` | getal 0..200 | |
| `sound.stereo` | getal 0..100 | |
| `sound.maxSimultaneousSounds` | getal 5..1000 | |
| `sound.masterVolume` | getal 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | geweigerd | bekend maar niet schrijfbaar → `OL_E_SETTING_NOT_WRITABLE` |

Gepatchte bestanden behouden hun codering (Windows-1252-bytes blijven behouden; UTF-8/UTF-16 met BOM wordt gerespecteerd) en regeleinden.

### `LaunchBehaviorSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | alleen `true` | Het exacte herstel is verplicht: `false` wordt geweigerd met `OL_E_INVALID_ARGUMENT`. | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | elke | `true`: een `closecheck`-bestand dat al vóór de sessie bestaat, wordt bij de start permanent verwijderd (diagnose `closecheck.stale-removed` met de SHA-256 ervan; fout `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: een bestaande `closecheck` blijft ongemoeid en is geen sessieverwijdering. De `closecheck` die OMSI tijdens de sessie schrijft, wordt bij het herstel altijd verwijderd. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (anders `ArgumentOutOfRangeException` van `StartSessionAsync`; de CLI-vlag `/startup-timeout` dwingt 1..600 af; profielen vereisen > 0). Tijdsbudget vanaf de start van de supervisor tot `Running`; bij verloop mislukt de sessie met `OL_E_STARTUP_TIMEOUT` (plugin gestart) of `OL_E_PLUGIN_NOT_LOADED`. | ja | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600 (CLI `/shutdown-timeout`) | Met `CooperativeShutdown`: de tijd die OMSI krijgt om zelf af te sluiten voordat het wordt beëindigd. Zonder die vlag wordt OMSI direct beëindigd. | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | elke (CLI `/cooperative-shutdown`) | `true`: het beëindigen van een actieve sessie vraagt OMSI af te sluiten via zijn eigen afsluitroutine (`TForm_main.CloseOMSI`: opties en de `laststn.osn` van de kaart worden opgeslagen, plugins afgerond) in plaats van het te beëindigen; na `ShutdownTimeoutSeconds` wordt het beëindigd (`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`). Het herstel draait na het afsluiten, zodat bestanden in bezit van de sessie nog steeds naar hun oorspronkelijke bytes terugkeren. | `EXPERIMENTAL` |

### `InputSpec`

| Eigenschap | Type | Standaard | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Pad (absoluut, of relatief aan de installatie) van een toetsenbordbestand in het formaat van OMSI. De sessie gebruikt het in plaats van `Inputs\keyboard.cfg` (een transactie-overlay: het bestand van de installatie wordt aan het eind byte voor byte hersteld). Het bestand moet bestaan, hoogstens 1 MiB zijn en minstens één binding `[entry]` bevatten; anders `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Pad van een controllerbestand in het formaat van OMSI, op dezelfde manier voor de sessie gebruikt in plaats van `Inputs\gamectrler.cfg`. Het moet minstens één apparaat `[ctrl]` bevatten; anders `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| Eigenschap | Type | Standaard | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Geen verwerking in `src/`. De hosttrace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` wordt altijd geschreven. | `PARTIAL` (meegenomen, momenteel zonder effect) |
| `Verbose` | bool | `false` | Geen verwerking. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Geen verwerking. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Geen verwerking. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Vlag in de opstart-handoff: de plugin schrijft `plugin.trace.operation`-events (operatie, uitkomst, code, duur in microseconden) voor elke runtime-operatie; ze bereiken `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` via de telemetriering. | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | Vlag in de opstart-handoff: de native laag meldt elke installatie en verwijdering van een hook (`native.trace`: grens, eigenaar, doel, resultaat) in dezelfde hosttrace. | `EXPERIMENTAL` |

De CLI-vlaggen `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` vullen deze booleans (`/logall` stelt `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace` in); ze worden met een OF-bewerking gecombineerd met de waarden uit de spec.

### `SessionPresentationSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`): de eigen opstartschermbestanden van OMSI blijven ongemoeid. `1` `Managed`: OmsiLaunch plaatst voor de sessie een overlay van `GUI\NewSplashscreen_ENG.bmp` en `GUI\NewSplashscreen_<LANG>.bmp` (daarna exact hersteld). | ja | `STABLE_BETA` (matrix RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (niet hoofdlettergevoelig); elke andere waarde wordt genormaliseerd naar `ENG`. Wanneer niet ingesteld, wordt de waarde `[language]` uit `options.cfg` gelezen en op dezelfde manier genormaliseerd. | ja (alleen beheerd opstartscherm) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Map met `ENG.bmp` en `<LANG>.bmp` (640×480, 24-bits BMP). Zie [padregels](#path-rules). Ontbrekende map: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; ontbrekend bestand: `OL_E_SPLASH_ASSET_MISSING`; verkeerde indeling: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Wanneer niet ingesteld, wordt `<root>\.omsilaunch\assets\splash` gebruikt (eenmalig gevuld vanuit het pakket), anders de meegeleverde `assets\splash`. | ja (alleen beheerd opstartscherm) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` onderdrukt het zelfstandige Windows-systeemvakpictogram van de CLI-eigenaar. | Alleen CLI-eigenaar; de API heeft geen systeemvakpictogram. Er is geen CLI-vlag; de waarde kan alleen uit een spec-bestand komen. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: er verandert niets. `1` `Disabled`: de plugin onderdrukt de in-process downloader van OMSI (telemetrie `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: het `.itx`-profiel wordt als overlay `Texture\standard.itx` geplaatst; de doelbestanden ervan en `Texture\standard.ipr` worden sessieverwijderingen. | ja | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Verplicht voor `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Een tekstbestand met regelparen: een absolute `http`/`https`-URL, gevolgd door een doelpad relatief aan de installatiemap dat een `Texture\`-component bevat, niet geroot is, geen `..` bevat, niet met `\` begint en geen junction/symlink doorloopt (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Zie [padregels](#path-rules). | ja | `EXPERIMENTAL` |

### `HofManagementSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Operations` | array van `HofOperationSpec` | geen | In volgorde toegepast; zie [HOF-beheer](hof-management.md) voor oplossing, conflictregels en limieten. | ja | `STABLE_BETA` |

Leden van `HofOperationSpec`: `SourceKind` (`HofSourceKind` int), `Source` (string), `TargetVehicles` (array van voertuigidentiteiten of mappen onder `Vehicles\`, minstens één), `Mode` (`HofApplyMode` int, standaard `0` `Add`), `Files` (array van `.hof`-bestandsnamen of null voor alle). Problemen zijn plandiagnosemeldingen `OL_E_HOF_*`.

### `RuntimeModulesSpec`

| Eigenschap | Type | Standaard | Geldige waarden | Verwerkt | Stabiliteit |
| --- | --- | --- | --- | --- | --- |
| `Enable` | array van module-id's | geen | Id's van modules die onder `plugins\OmsiLaunch.Modules` zijn geïnstalleerd; zie [runtimemodules](../../../reference/runtime-modules.md). Een aangevraagde module die niet kan worden geladen, is een plandiagnose `OL_E_MODULE_*` (plan niet uitvoerbaar). | ja | `EXPERIMENTAL` |
| `Disable` | array van module-id's of null | geen | Houdt een standaardmodule, contentmodule of module uit het instellingenbestand buiten de sessie (plandiagnose `module.disabled`). Een module die zowel is ingeschakeld als uitgeschakeld, is `OL_E_MODULE_SETTING_INVALID`. Alleen-lezen accessor `EffectiveDisable`. | ja | `EXPERIMENTAL` |
| `Settings` | object van strings of null | geen | `"<module id>.<key>": "<value>"`. Heeft voorrang op het instellingenbestand `.omsilaunch\modules.ini` en op de standaardwaarde in het manifest. Een ongeldige waarde of een sleutel die de module niet declareert, is `OL_E_MODULE_SETTING_INVALID` (plan niet uitvoerbaar); instellingen voor een module die de sessie niet laadt, zijn `OL_W_MODULE_SETTING_IGNORED`. De effectieve waarden staan in `PlannedModule.Settings`. Alleen-lezen accessor `EffectiveSettings`. | ja | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Eigenschap | Type | Verwerkt | Stabiliteit |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | strings / int | Vastgelegd in de plandiagnose `session_profile.selected` (`Data["session_profile.*"]`). Verder niet verwerkt; wordt normaal gesproken gevuld door de sessieprofielcompiler, niet handmatig. | `STABLE_BETA` |

<a id="enumerations"></a>
## Enumeraties

| Enum | Waarden (JSON-geheel getal) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (verouderde alias; dit is de native last-map-state-tak van OMSI, nooit de nieuwste `.osn`) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `HofSourceKind` | `Collection` = 0, `Directory` = 1, `Vehicle` = 2, `File` = 3 |
| `HofApplyMode` | `Add` = 0, `Mirror` = 1 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (alleen-lezen `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Gehele getallen buiten het gedeclareerde bereik worden door de serializer ongewijzigd opgeslagen en gedragen zich als onbekende waarden (een onbekende `WorldMode` is bijvoorbeeld noch NEW_MAP noch SAVED_SITUATION en levert een plan op zonder wereld-capability; de plugin zou het weigeren, maar de CLI vervangt de modus hoe dan ook, zie voorrang).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Validatieregels en diagnoses voor niet-uitvoerbare plannen

`PlanSessionAsync` voert `LaunchValidation.Validate` uit en daarna `SessionPlanner.PlanAsync`. Een plan is uitvoerbaar precies wanneer geen enkele diagnosecode met `OL_E_` begint. De volledige set:

| Diagnose | Voorwaarde | Bron |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` leeg of alleen witruimte | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` zonder ingestelde waarde of met maand/dag buiten bereik; `Time.Mode` = `Explicit` zonder ingestelde waarde of met uur/minuut/seconde buiten bereik | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` of `Time.Value` ingesteld terwijl de modus niet `Explicit` is | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` met `MapIdentity` niet ingesteld of niet in de vorm `maps\...\global.cfg` (validatie); `NewMap` met een identiteit die niet geïnstalleerd is (planner) | beide |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` zonder `EntrypointIdentity` en met `PresentedEntrypointIndex` niet ingesteld of negatief | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, kaart geïnstalleerd, geen `EntrypointIdentity`, `PresentedEntrypointIndex` niet ingesteld (`world.presented-entrypoint` niet beschikbaar) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` zonder `SituationIdentity` (validatie) of met een identiteit die niet geïnstalleerd is (planner) | beide |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: de kaart die in de `.osn` wordt genoemd, is niet geïnstalleerd | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | niet Windows 10+ op een x64-besturingssysteem met een x64-hostproces (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | hoofdmap ontbreekt, kenmerk alleen-lezen ingesteld of geen submap `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | Geen OMSI 2-installatie: `Omsi.exe` ontbreekt, is geen 32-bits x86 PE-image, heeft geen OMSI 2-versie (2.x), of er is geen map `maps\` / `Vehicles\` (`omsi.omsi2`). De hash is geen criterium. | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `EntrypointIdentity` ingesteld; modus van `Date`/`Time`/`Year` niet `Unset` met `SavedSituation`; modus van `Weather` niet `Unset`; een veld van `PlayerVehicle` ingesteld | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` niet geïnstalleerd (naast `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | een `Environment`-sleutel die niet in de catalogus staat / niet schrijfbaar is | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | het opbouwen van het splash-/ITX-plan gaf een exceptie; het bericht bevat `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` of `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | de verwijzing naar de plugin-closure (`OmsiLaunchRuntimePaths`) of het releasemanifest kan niet worden geladen (het bericht kan `OL_E_RELEASE_MANIFEST_INVALID` bevatten) | `OmsiLaunchService.PlanSessionAsync` |

Niet gevalideerd tijdens het plannen (mislukt bij de start als `Failed`-sessie met `OL_E_START_SESSION`): instellingswaarden (`OL_E_INVALID_SETTING_VALUE`), integriteit van de permanente plugin (`OL_E_PERMANENT_PLUGIN_*`), beschikbaarheid van de lease (`OL_E_INSTALLATION_BUSY`), bereik van `StartupTimeoutSeconds` (exceptie van `StartSessionAsync`).

Informatieve plandiagnoses: `plugin.integrity.reference` (bericht `manifest` of `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Voorrang: CLI-vlaggen, spec-bestand en sessieprofiel

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) bouwt de effectieve spec in deze volgorde op:

1. Basis = ingebouwde standaardwaarden, of het `/spec`-bestand wanneer opgegeven.
2. Installatiemap = het expliciete installatieargument indien opgegeven, anders de `RootPath` van de basis; daarna `.`/leeg → map van het uitvoerbare bestand, `Path.GetFullPath`. Een expliciet installatieargument heeft altijd voorrang op de `RootPath` van de spec.
3. Sessieprofiel (`/predefined-profile` + `/predefined-profile-index`): expliciete CLI-argumenten die een veld raken dat het profiel beheert, worden geweigerd met `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (wereldvelden alleen in NEW_MAP-modus; `/set`-sleutels die in de preset voorkomen; splash-vlaggen wanneer de preset `presentation` heeft; internettexture-vlaggen wanneer de preset `internet-textures` heeft; timeouts wanneer de preset `behavior` heeft). De `World` van de basis wordt vervangen door een nieuwe (alleen de wereldmodus van de CLI blijft behouden), waarna het `new:`-blok van het profiel (alleen NEW_MAP), `settings` (in `General`), `presentation`, `internet-textures`, `behavior` en de metagegevens van `SessionProfile` worden toegepast. `compatibility.maps` wordt afgedwongen voor NEW_MAP en SAVED_SITUATION.
4. Wereld: de wereldmodus van de CLI heeft altijd voorrang (`/new` standaard, `/saved:<osn>`, `/last`); de `World.Mode` van het spec-bestand wordt vervangen. Geef `/saved:` mee om een opgeslagen situatie vanuit een spec te starten. `/map` en `/entrypoint`/`/entrypoint-index` overschrijven de basis; een CLI-identiteit via `/entrypoint` wist de index; `/saved` met `/map` of instappuntvlaggen levert `OL_E_INVALID_ARGUMENT` op.
5. `/date`, `/time`, `/year`, `/weather*` overschrijven de basis wanneer opgegeven (`system` selecteert `DateTimeMode.System`).
6. `/no-vehicle` wist `PlayerVehicle`; afzonderlijke `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` overschrijven afzonderlijke velden van het spelersvoertuig van de basis.
7. Vermeldingen `/set:<key>=<value>` worden toegevoegd aan `Environment.General` (sleutel gecontroleerd, waarde niet); de andere zeven groepen komen ongewijzigd uit de basis.
8. `/startup-timeout` en `/shutdown-timeout` overschrijven de basis alleen wanneer opgegeven; anders gelden achtereenvolgens de spec, het profiel en de standaardwaarden 180 s / 30 s. `ShutdownTimeoutSeconds` werkt met `/cooperative-shutdown` (dat met OR wordt gecombineerd met de waarde in de spec).
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` overschrijven de basis wanneer opgegeven; `SuppressTrayIcon` komt alleen uit de basis.
10. Diagnosevlaggen worden met een OF-bewerking gecombineerd met de basis.

Resultaat: expliciete CLI-vlag > sessieprofiel > spec-bestand > ingebouwde standaardwaarde, met de uitzondering dat een CLI-vlag die conflicteert met een veld dat het profiel beheert, een fout is in plaats van een overschrijving.

<a id="path-rules"></a>
## Padregels

| Pad | Gedrag in de API | Gedrag in de CLI |
| --- | --- | --- |
| `Installation.RootPath` | Gebruikt zoals opgegeven: relatieve paden worden bij bestandsbewerkingen opgelost ten opzichte van de werkmap van het proces. Geef een absoluut pad op. De lease, het journal en de inhoudscatalogus normaliseren het met `Path.GetFullPath`. | `.` of leeg = de map die `OmsiLaunch.exe` bevat, nooit de werkmap van de aanroeper; een expliciet installatieargument heeft voorrang op de spec; het resultaat wordt absoluut gemaakt. |
| `Presentation.CustomAssetDirectory` | Absoluut, of relatief aan `Installation.RootPath`. Moet bestaan. | Idem (`/splash-assets`). Een `assets`-pad uit een sessieprofiel blijft beperkt tot het profielpakket en wordt absoluut opgeslagen. |
| `InternetTextures.OverrideProfilePath` | Opgelost met `Path.GetFullPath`, dus relatief aan de werkmap van het proces, niet aan de installatiemap. Moet bestaan. | Idem (`/internet-textures-profile`). Een `profile`-pad uit een sessieprofiel blijft beperkt tot het pakket en wordt absoluut opgeslagen. |
| ITX-doelregels | Relatief aan de installatiemap; moeten een `Texture\`-component bevatten; geen root, geen `..`, geen voorloop-`\`, geen junction-/symlinkcomponent. | Idem. |
| Inhoudsidentiteiten (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Relatief aan de installatie, niet hoofdlettergevoelig, `/` geaccepteerd; nooit absoluut. | Idem. |

<a id="carried-but-not-applied"></a>
## Meegenomen maar niet toegepast

| Veld | Huidig effect | Stabiliteit |
| --- | --- | --- |
| `Diagnostics.Log`, `Verbose`, `OmsiLogAll`, `ProcessTrace` | geen (hosttrace wordt altijd geschreven) | `PARTIAL` |
| `PlayerVehicle.*` (elk ingesteld veld) | inhoud opgelost voor diagnose, plan niet uitvoerbaar (`UNAVAILABLE`) | `PARTIAL` |
| `SessionProfile` | alleen herkomstdiagnose | `STABLE_BETA` |
