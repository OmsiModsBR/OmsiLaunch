# Sessieprofielen

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/session-profiles.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Een sessieprofiel is een declaratief YAML-pakket dat een contentauteur met een kaart of add-on meelevert, zodat eindgebruikers met één opdracht een reproduceerbare OmsiLaunch-sessie kunnen starten (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Deze pagina is de normatieve referentie voor het formaat `omsilaunch.session-profile/v1` zoals geïmplementeerd door `SessionProfileCompiler` in `src/OmsiLaunch.Core/SessionProfiles.cs`, voor de voorrangsregels die de CLI toepast (`CliInput.BuildSpecAsync` en `RejectProfileConflicts` in `tools/OmsiLaunch.Cli/Program.cs`) en voor de catalogus van instellingen die een profiel mag schrijven (`ConfigurationCatalog`). Alles wat een profiel kan, kunnen de CLI-vlaggen en de [LaunchSpec](launchspec.md) ook; een profiel verpakt alleen die keuzes.

Stabiliteit: `STABLE_BETA` voor parsing, validatie, conflictdetectie en de blokken `settings` / `presentation` / `internet-textures` / `behavior` (offline test `session-profiles.strict-compiler`; het overlay- en herstelpad is runtime-gevalideerd door RV-005 en RV-006, zie [status van de runtimevalidatie](../status/runtime-validation-status.md)). De sleutels `new.date`, `new.time`, `new.year` en `new.weather` zijn in deze build `UNAVAILABLE` (zie [Het `new`-blok](#new)).

<a id="package-location-and-naming"></a>
## Locatie en naamgeving van het pakket

| Onderdeel | Regel |
| --- | --- |
| Pakketmap | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Profielbestand | `<package>\profile.yaml` (exacte naam, één bestand) |
| Assets | Willekeurige bestanden of mappen binnen de pakketmap, waarnaar met een relatief pad wordt verwezen vanuit `presentation.splash.assets` en `internet-textures.profile` |
| `id` | Moet een gewone mapnaam zijn: niet leeg of alleen witruimte, zonder `\`, `/` of `:` en zonder de reeks `..`. Overtredingen geven `OL_E_SESSION_PROFILE_PATH_ESCAPE`. De `id`-waarde die in `profile.yaml` is gedeclareerd, moet byte voor byte gelijk zijn aan de mapnaam (hoofdlettergevoelig); anders `OL_E_SESSION_PROFILE_INVALID`. |
| Selectie | `/predefined-profile:<id>` samen met `/predefined-profile-index:<n>`. De index is verplicht: `/predefined-profile` zonder `/predefined-profile-index` mislukt met `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Ontbrekend pakket | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Indeling van de release | Het releasepakket bevat een voorbeeld onder `.omsilaunch\examples\session-profiles\rmg-leste\` (zie [packaging](packaging.md)). Voorbeelden zijn geen profielen: kopieer een pakket naar `.omsilaunch\session-profiles\<id>\` om het selecteerbaar te maken. |

Een profiel wordt geïnstalleerd en verwijderd door de gebruiker of de contentauteur. OmsiLaunch schrijft nooit in een pakket, kopieert het nooit en verwijdert het nooit. De pakketmap maakt geen deel uit van een transactie.

<a id="parsing-rules"></a>
## Parseerregels

| Regel | Gedrag | Fout |
| --- | --- | --- |
| Groottelimiet | `profile.yaml` mag niet groter zijn dan 256 KiB (262,144 bytes) | `OL_E_SESSION_PROFILE_INVALID` |
| Documentvorm | Precies één YAML-document waarvan het hoofdknooppunt een mapping is | `OL_E_SESSION_PROFILE_INVALID` |
| Schema | `schema` moet exact `omsilaunch.session-profile/v1` zijn (hoofdlettergevoelig) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Anchors en aliassen | Elk knooppunt met een YAML-anchor (`&name`), waar dan ook in het document, wordt vóór de validatie geweigerd; aliassen (`*name`) kunnen daardoor niet voorkomen | `OL_E_SESSION_PROFILE_INVALID` ("YAML anchors are not supported.") |
| Onbekende sleutels | Elke mapping is gesloten: een sleutel die in de tabellen hieronder niet voor de betreffende context wordt vermeld, wordt geweigerd ("Unknown property in `<context>`: `<key>`"). Sleutels worden hoofdlettergevoelig vergeleken (`Schema:` is een onbekende sleutel). De enige open mapping is `settings`, waarvan de sleutels in plaats daarvan worden gevalideerd tegen de catalogus van instellingen. | `OL_E_SESSION_PROFILE_INVALID` |
| Scalars | Elke bladwaarde moet een scalar zijn; sequences en mappings waar een scalar wordt verwacht, worden geweigerd ("`<field>` must be a scalar.") | `OL_E_SESSION_PROFILE_INVALID` |
| Getallen | Gehele getallen worden geparseerd met de invariante cultuur (`1`, `30`); decimale getallen in `settings` gebruiken `.` als scheidingsteken | `OL_E_SESSION_PROFILE_INVALID` |
| Datums en tijden | `new.date.value` wordt geparseerd door `DateOnly.Parse` en `new.time.value` door `TimeOnly.Parse`, beide met de invariante cultuur; gebruik de ISO-vormen `yyyy-MM-dd` en `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| YAML-syntaxisfouten | Gemeld met het bericht van de parser | `OL_E_SESSION_PROFILE_INVALID` ("Invalid YAML: ...") |
| Uitvoerbare inhoud | YAML wordt met `YamlDotNet` alleen geparseerd naar een representatieboom; tags, aangepaste typen of code-uitvoering worden niet ondersteund |

Backslashes in plain (niet-geciteerde) scalars zijn letterlijke tekens. Schrijf Windows-paden met één backslash (`maps\Grundorf\global.cfg`). Een dubbele backslash in een plain scalar blijft dubbel in de waarde; zie [Het meegeleverde voorbeeld](#the-packaged-example).

<a id="key-reference"></a>
## Sleutelreferentie

Contexten worden precies zo genoemd als de compiler ze noemt. Elke hier vermelde sleutel wordt geaccepteerd; niets anders.

<a id="profile-root-mapping"></a>
### `profile` (hoofdmapping)

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `schema` | string | ja | Letterlijk `omsilaunch.session-profile/v1`. |
| `id` | string | ja | Pakket-id; moet gelijk zijn aan de mapnaam. |
| `name` | string | ja | Weergavenaam; gerapporteerd in `SessionProfileMetadata.Name`. |
| `author` | string | ja | Auteur; gerapporteerd in `SessionProfileMetadata.Author`. |
| `version` | string | ja | Versiestring van het pakket (vrije vorm, zet deze tussen aanhalingstekens: `"1.0"`); gerapporteerd in `SessionProfileMetadata.Version`. |
| `compatibility` | mapping | nee | Zie `compatibility`. |
| `new` | mapping | nee | Standaardwaarden voor NEW_MAP. Zie `new`. |
| `presets` | sequence van mappings | ja | 1 tot 99 presetvermeldingen. Nul, meer dan 99 of een waarde die geen sequence is, geeft `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `maps` | sequence van strings | nee | Kaartidentiteiten (`maps\<Map>\global.cfg`) waarvoor dit profiel geldig is. `/` wordt genormaliseerd naar `\`; de vergelijking is niet hoofdlettergevoelig. Een ontbrekende of lege lijst betekent "elke kaart". Wanneer de lijst niet leeg is, wordt deze afgedwongen voor `WorldMode.NewMap` (tegen de effectieve `new.map` of `/map`) en voor `WorldMode.SavedSituation` (tegen de kaart waarnaar de geselecteerde `.osn` verwijst, opgelost via de inhoudscatalogus). Voor `WorldMode.LastMapState` kan geen kaart worden afgeleid, dus een niet-lege lijst mislukt altijd. Fout: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

Het blok wordt gelezen en gevalideerd zodra het aanwezig is, maar wordt alleen op de spec toegepast wanneer de geselecteerde wereldmodus NEW_MAP is (`/new`, de standaard van de CLI). Onder `/saved:<file.osn>` wordt het blok genegeerd.

| Sleutel | Type | Verplicht | Toegepast | Beschrijving |
| --- | --- | --- | --- | --- |
| `map` | string | nee | ja | Kaartidentiteit in de genormaliseerde vorm `maps\<Map>\global.cfg` (het plannen vereist precies deze vorm: begint met `maps\`, eindigt op `\global.cfg`, geen `..`). Stelt `WorldSpec.MapIdentity` in. |
| `entrypoint-index` | integer | nee | ja | Index van het getoonde instappunt (0-gebaseerde positie in de lijst met instappunten van OMSI). Stelt `PresentedEntrypointIndex` in en wist een eventuele instappuntidentiteit. |
| `entrypoint` | string | nee | ja | Onbewerkte instappuntidentiteit. Stelt `EntrypointIdentity` in en wist de getoonde index. Als zowel `entrypoint-index` als `entrypoint` aanwezig zijn, wint `entrypoint` omdat die als laatste wordt toegepast. De waarde is een exact (hoofdlettergevoelig) label of een detectie-identiteit; OMSI toont alleen het eerste record van elk label (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapping | nee | nee (`UNAVAILABLE`) | Zie `new.date`. |
| `time` | mapping | nee | nee (`UNAVAILABLE`) | Zie `new.time`. |
| `year` | integer | nee | nee (`UNAVAILABLE`) | Expliciet jaar. |
| `weather` | mapping | nee | nee (`UNAVAILABLE`) | Zie `new.weather`. |

`date`, `time`, `year` en `weather` worden gecompileerd naar `DateSpec`, `TimeSpec`, `YearSpec` en `WeatherSpec` met `DateTimeMode.Explicit` / de geselecteerde `WeatherMode`. De sessieplanner (`src/OmsiLaunch.Core/SessionPlanner.cs`) vereist vervolgens `world.explicit-date`, `world.explicit-time`, `world.explicit-year` en `world.weather`; alle vier zijn `EXPERIMENTAL` (statisch afgesloten, runtimevalidatie in afwachting). De start-datum/-tijd geldt alleen voor NEW_MAP; het weer geldt voor elke wereldmodus.

#### `new.date`

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `mode` | string | ja | Moet `explicit` zijn (niet hoofdlettergevoelig). Elke andere waarde geeft `OL_E_SESSION_PROFILE_INVALID` ("date must use explicit mode."). |
| `value` | string | ja | `yyyy-MM-dd`. |

#### `new.time`

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `mode` | string | ja | Moet `explicit` zijn. |
| `value` | string | ja | `HH:mm` of `HH:mm:ss`. |

#### `new.weather`

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `mode` | string | ja | `preset`, `icao` of `real` (niet hoofdlettergevoelig). Al het andere: `OL_E_SESSION_PROFILE_INVALID` ("Unsupported weather mode"). |
| `preset` | string | bij `mode: preset` | Naam van de weerpreset. |
| `icao` | string | bij `mode: icao` | ICAO-stationscode. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (elke vermelding van `presets`)

| Sleutel | Type | Verplicht | Standaard | Beschrijving |
| --- | --- | --- | --- | --- |
| `index` | integer | ja | | 1 tot 99, uniek binnen het profiel; indexen hoeven niet aaneensluitend te zijn. Geselecteerd met `/predefined-profile-index`. Dubbel of buiten bereik: `OL_E_SESSION_PROFILE_INVALID`; een index die nergens in het profiel voorkomt: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | string | ja | | Preset-id; gerapporteerd als `SessionProfileMetadata.PresetId`. |
| `name` | string | ja | | Weergavenaam van de preset; gerapporteerd als `SessionProfileMetadata.PresetName`. |
| `settings` | mapping | nee | geen | Semantische instellingen van `options.cfg`, zie [Instellingen](#settings). Sleutels worden niet hoofdlettergevoelig vergeleken met de catalogus. |
| `presentation` | mapping | nee | overerven | Presentatie van het opstartscherm, zie `presentation`. Wanneer afwezig, erft de preset de basislijn (waarde uit `/spec` of de standaard van de CLI, `Managed`). |
| `internet-textures` | mapping | nee | overerven | Zie `internet-textures`. |
| `behavior` | mapping | nee | overerven | Timeouts, zie `behavior`. |
| `hofs` | sequence van mappings | nee | overerven | HOF-bewerkingen voor de sessie, zie `hofs`. Wanneer aanwezig vervangen ze de `HofManagement` van de basislijn. |
| `modules` | mapping | nee | overerven | Runtimemodules, zie `modules`. Samengevoegd met wat een `/spec`-bestand aanvraagt. |

Alleen de geselecteerde preset wordt toegepast. Elke preset wordt wel geparseerd en gevalideerd, dus een fout in preset 3 laat een aanvraag voor preset 1 mislukken.

### `presentation`

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `splash` | mapping | ja | Verplicht wanneer `presentation` aanwezig is ("Presentation requires splash."). Zie `presentation.splash`. |

#### `presentation.splash`

| Sleutel | Type | Verplicht | Standaard | Beschrijving |
| --- | --- | --- | --- | --- |
| `mode` | string | ja | | `managed` installeert voor de sessie de opstartschermbitmaps van OmsiLaunch (`SplashMode.Managed`). `unset` of `native` behoudt de eigen opstartschermbestanden van OMSI (`SplashMode.Unset`; `Native` is een alias). Niet hoofdlettergevoelig. Al het andere: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | string | nee | `ENG` | Taal van het tweede opstartschermdoel: `PTB`, `ENG`, `DEU`, `FRA` (aliassen `PT-BR`, `EN`, `DE`, `FR`; alles wat onbekend is, wordt bij het opbouwen van de sessie `ENG`). Met `mode: managed` plaatst de sessie een overlay van `GUI\NewSplashscreen_ENG.bmp` en `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | string | nee | meegeleverde assets | Map **relatief aan het pakket** met `ENG.bmp` en, voor een niet-Engelse `language`, `<language>.bmp`; elk bestand moet een 640x480, 24-bits BMP zijn. De map moet bestaan bij het laden van het profiel (`OL_E_SESSION_PROFILE_ASSET_MISSING`); de bestanden worden bij de start van de sessie gevalideerd (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). De regels voor padbeperking zijn van toepassing. Wanneer weggelaten, worden `.omsilaunch\assets\splash` van de installatie (of de meegeleverde standaardbestanden) gebruikt. |

Een profiel kan `SessionPresentationSpec.SuppressTrayIcon` niet instellen; de waarde blijft `false`, tenzij een `/spec` deze instelt.

### `internet-textures`

| Sleutel | Type | Verplicht | Beschrijving |
| --- | --- | --- | --- |
| `mode` | string | ja | `native` (`InternetTexturesMode.Native`, OMSI gedraagt zich normaal), `disabled` (`Disabled`, de geprofileerde in-process downloader wordt voor de sessie onderdrukt), `override` (`Override`, een sessiegebonden `.itx`-profiel wordt geïnstalleerd als `Texture\standard.itx`). Niet hoofdlettergevoelig; al het andere: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | string | verplicht voor `override` | Pad **relatief aan het pakket** van het `.itx`-bestand. Ontbrekende sleutel bij `override`: `OL_E_SESSION_PROFILE_INVALID`; ontbrekend bestand: `OL_E_SESSION_PROFILE_ASSET_MISSING`. De regels voor padbeperking zijn van toepassing. Het bestand moet bestaan uit regelparen `URL` / `target` met URL's die met `http://` of `https://` beginnen (anders `OL_E_ITX_PROFILE_INVALID`), en elk doel moet onder de map `Texture\` van de installatie uitkomen zonder een reparsepunt te doorlopen (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). De vermelde doelen en `Texture\standard.ipr` worden sessieverwijderingen (zie [transacties en recovery](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Sleutel | Type | Verplicht | Standaard | Beschrijving |
| --- | --- | --- | --- | --- |
| `startup-timeout` | integer (seconden) | nee | 180 | Toegestane tijd vanaf de processtart tot `Running`. Moet positief zijn bij het laden van het profiel; de sessie vereist bij de start bovendien 1 tot 600 (anders `OL_E_START_SESSION`). Komt overeen met `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | integer (seconden) | nee | 30 | Komt overeen met `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: de supervisor beëindigt OMSI direct en leest deze waarde nooit. |

Wanneer het `behavior`-blok aanwezig is, worden beide timeouts ingesteld (opgegeven waarde of standaard) en vervangen ze de `LaunchBehaviorSpec` van de basislijn volledig, inclusief `RestoreConfiguration` en `SuppressStaleClosecheckWarning`, die terugvallen op hun standaardwaarden (`true`, `true`).

### `hofs`

Elke vermelding is één bewerking voor [HOF-beheer](hof-management.md) en noemt precies één bronsleutel (`package`, `collection`, `directory`, `vehicle` of `file`); nul of meerdere: `OL_E_SESSION_PROFILE_INVALID`. Het bestaan van bronnen en doelen wordt gecontroleerd wanneer de sessie wordt gepland (`OL_E_HOF_*`), behalve voor `package`.

| Sleutel | Type | Verplicht | Standaard | Beschrijving |
| --- | --- | --- | --- | --- |
| `package` | string | één bronsleutel | | Map **relatief aan het pakket** met de `.hof`-bestanden die het profiel meelevert. Moet bestaan bij het laden van het profiel (`OL_E_SESSION_PROFILE_ASSET_MISSING`); de regels voor padbeperking gelden. Wordt een bron `Directory`. |
| `collection` | string | één bronsleutel | | Naam van een collectie onder `.omsilaunch\hof\`. |
| `directory` | string | één bronsleutel | | Map relatief aan de installatie, bijvoorbeeld naast kaartcontent. |
| `vehicle` | string | één bronsleutel | | Voertuigidentiteit of map waarvan de HOF's worden gekopieerd. |
| `file` | string | één bronsleutel | | `.hof`-bestand relatief aan de installatie. |
| `targets` | sequence van strings | ja | | Doelvoertuigen (identiteiten of mappen onder `Vehicles\`). Ontbrekend of leeg: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | sequence van strings | nee | alle | Bestandsnamen die uit de bron worden genomen. |
| `mode` | string | nee | `add` | `add` of `mirror` (niet hoofdlettergevoelig); anders: `OL_E_SESSION_PROFILE_INVALID`. |

Een preset met `hofs` in combinatie met een vlag `/hof-source`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Eén vermelding per [runtimemodule](../../../reference/runtime-modules.md)-id (`EXPERIMENTAL`). Elke vermelding is een mapping met deze sleutels (context `module`):

| Sleutel | Type | Verplicht | Standaard | Beschrijving |
| --- | --- | --- | --- | --- |
| `enabled` | `true` of `false` | nee | niet ingesteld | `true` vraagt de module aan (`LaunchSpec.Modules.Enable`); `false` houdt haar buiten de sessie, ook een module die standaard, door zijn kaart of door het instellingenbestand wordt geladen (`LaunchSpec.Modules.Disable`). |
| `settings` | mapping | nee | geen | Moduleinstellingen per sleutel (`LaunchSpec.Modules.Settings` als `<id>.<key>`). De waarden worden bij het plannen van de sessie gecontroleerd tegen de gedeclareerde instellingen van de module (`OL_E_MODULE_SETTING_INVALID`). |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

Een module-id die niet `[a-z0-9][a-z0-9-]{1,47}` is, een `enabled` anders dan `true` of `false`, of een instellingssleutel die niet `[a-z][a-z0-9_]{0,31}` is: `OL_E_SESSION_PROFILE_INVALID`. Een module die niet is geïnstalleerd, wordt gemeld wanneer de sessie wordt gepland (`OL_E_MODULE_NOT_FOUND` voor `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` voor alleen instellingen).

<a id="settings"></a>
## Instellingen

De sleutels van `settings` zijn de semantische namen uit `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). De compiler accepteert een sleutel alleen als die bestaat (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) en schrijfbaar is (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Waarden worden als strings opgeslagen en omgezet in een patch van `options.cfg` wanneer de sessie haar overlays opbouwt; een ongeldige waarde wordt daarom pas bij `StartSessionAsync` gedetecteerd, niet bij het laden van het profiel, en laat de sessie mislukken met `OL_E_START_SESSION`, waarvan het bericht `OL_E_INVALID_SETTING_VALUE: <key>` bevat. Elke onderstaande instelling schrijft naar `options.cfg`; ze zijn allemaal sessiegebonden en worden na de sessie exact hersteld.

Waardevormen:

- **bool** is `true` of `false` (niet hoofdlettergevoelig). Bij aanwezigheidstokens wordt het token toegevoegd of verwijderd; bij omgekeerde tokens (`no_*`) verwijdert `true` het negatieve token.
- **int** / **decimal** worden gevalideerd tegen het bereik; waarden met een deler worden gedeeld opgeslagen (`graphics.minObjectScreenPercent: 5` schrijft bijvoorbeeld `0.05`).
- **string** wordt letterlijk geschreven.

| Instellingssleutel | Token in `options.cfg` | Type | Bereik / waarden | Bewijs |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | elke | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | elke | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | elke | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, opgeslagen /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, opgeslagen /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, geschreven als `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` of `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (blok van 4 regels) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (omgekeerde aanwezigheid) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (beide aanwezigheidstokens) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (aanwezigheid) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` component 0 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 runtime) |
| `traffic.humans` | `AIMaxCountRandom` component 1 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 runtime) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Catalogusvermeldingen die bestaan maar **niet schrijfbaar** zijn (geweigerd met `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (vervangen door `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Padbeperking

`presentation.splash.assets` en `internet-textures.profile` worden opgelost door `Confined(package root, value)`:

1. Geroote paden (`C:\...`), paden die met `\` beginnen en elk padcomponent dat gelijk is aan `..` worden geweigerd.
2. Het volledige pad wordt berekend en moet met de pakketmap beginnen.
3. Elk bestaand component onder de pakketroot, tot en met het uiteindelijke pad, wordt gecontroleerd op het kenmerk `ReparsePoint`. Een junction, symbolische koppeling naar een map of symbolische koppeling naar een bestand waar dan ook op dat pad wordt geweigerd, evenals een component dat niet kan worden gecontroleerd (`IOException` / `UnauthorizedAccessException`).

Alle drie de fouten geven `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Dezelfde regel voor reparsepunten wordt bij het opbouwen van de sessie toegepast op `.itx`-doelen onder `Texture\`.

<a id="precedence-and-override-conflicts"></a>
## Voorrang en overschrijvingsconflicten

`CliInput.BuildSpecAsync` stelt de spec in deze volgorde samen:

1. **Standaardwaarden** (NEW_MAP, alles niet ingesteld, timeouts 180 s / 30 s).
2. **`/spec:<file.json>`**, indien opgegeven, vervangt de standaardwaarden volledig.
3. **Installatiemap**: een expliciet installatieargument heeft voorrang op de `RootPath` van de spec; `.` betekent de map die het uitvoerbare bestand bevat.
4. **Profiel** (`/predefined-profile` + `/predefined-profile-index`): het pakket wordt geladen en `RejectProfileConflicts` wordt uitgevoerd tegen de onbewerkte CLI-argumenten, **voordat** er iets wordt samengevoegd. Het wereldblok van de basis wordt daarna teruggezet naar een lege `WorldSpec` van de geselecteerde modus (een `/spec`-wereld wordt verworpen wanneer een profiel wordt gebruikt) en `SessionProfileCompiler.Apply` legt het profiel over de basis: `new` (alleen NEW_MAP), `settings` (samengevoegd over `Environment.General` van de basis, het profiel wint per sleutel), en `presentation`, `internet-textures`, `behavior` (elk vervangt het blok van de basis alleen wanneer de preset het definieert).
5. **Overige CLI-argumenten** worden daarbovenop gelegd: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, weervlaggen, voertuigvlaggen, `/set`, splash-vlaggen, internettexture-vlaggen, `/startup-timeout`, `/shutdown-timeout`. Timeouts uit de CLI gelden alleen wanneer ze zijn opgegeven; anders blijft de waarde uit spec, profiel of standaard staan.
6. **Compatibiliteitscontrole** voor modi anders dan NEW_MAP (`ValidateCompatibility`).

Een CLI-argument dat een veld raakt dat het geselecteerde profiel beheert, is een conflict en wordt geweigerd met `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (exitcode 2, categorie `invalid_argument`). De controle is per veld, niet per waarde: het herhalen van de eigen waarde van het profiel is nog steeds een conflict.

| CLI-argument | Conflicteert wanneer het profiel dit definieert | Alleen in modus |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` of `/entrypoint-index` | `new.entrypoint` of `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | dezelfde `<key>` in `settings` van de preset (niet hoofdlettergevoelig) | elke |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (elke) | elke |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (elke) | elke |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (elke) | elke |
| `/module:<id>` | `modules.<id>.enabled: false` | elke |
| `/no-module:<id>` | `modules.<id>.enabled: true` | elke |
| `/module-setting:<id>.<key>=...` of een modulevlag | `modules.<id>.settings.<key>` | elke |

Geen conflicten: `/set`-sleutels die de preset niet definieert (ze worden toegevoegd), voertuigvlaggen (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; een profiel kan geen spelersvoertuig definiëren) en elk wereldargument onder `/saved` (het `new`-blok wordt daar niet toegepast). `/map`, `/entrypoint` en `/entrypoint-index` zijn ongeldig in combinatie met `/saved`, ongeacht profielen (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Foutcodes

| Code | Wanneer gegenereerd | CLI-exitcode |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` bestaat niet | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` is geen gewone mapnaam; `assets` / `profile` verlaat het pakket of doorloopt een reparsepunt | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` is niet `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | groottelimiet, documentvorm, anchors, onbekende sleutel, ontbrekende verplichte sleutel, niet-scalaire waarde, ongeldig getal/datum/tijd, `id` komt niet overeen, regels voor aantal/index van presets, niet-ondersteunde moduswoorden, niet-positieve timeout, `presentation` zonder `splash`, `override` zonder `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` ontbreekt, ligt buiten 1..99, of er is geen preset met die `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | een sleutel in `settings` staat niet in de catalogus | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | een sleutel in `settings` staat in de catalogus maar is alleen-lezen | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | de map `assets` of het bestand `profile` bestaat niet binnen het pakket | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` is niet leeg en de effectieve kaart staat er niet in (of kan niet worden afgeleid) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | een expliciet CLI-argument raakt een veld dat het profiel beheert | 2 |

Al deze fouten worden gegenereerd terwijl de opdrachtregel wordt gecompileerd, vóór het plannen. Het zijn `SessionProfileException` (of `ArgumentException` voor het conflict) en ze starten nooit een sessie. De volledige catalogus staat in [foutcodes](errors.md); exitcodes in [exitcodes](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Geïnstalleerde profielen ontdekken

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) compileert elk pakket onder `.omsilaunch\session-profiles\` en drukt één vermelding per pakket af: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` en de `Presets` (`Index`, `Id`, `Name`) in volgorde van index. Een pakket dat niet compileert, wordt toch vermeld, met `IsValid: false` en dezelfde code `OL_E_SESSION_PROFILE_*` die een start zou melden. Er wordt niets geschreven.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Hoe een profiel in de API verschijnt

Na een geslaagde lading bevat de spec een `SessionProfileMetadata`-record in `LaunchSpec.SessionProfile`:

| Veld | Bron |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` van de geselecteerde preset |
| `PresetIndex` | `index` van de geselecteerde preset |
| `PresetName` | `name` van de geselecteerde preset |
| `PackagePath` | absolute pakketmap |

De planner voegt aan elk `SessionPlan` dat uit zo'n spec wordt opgebouwd een informatieve diagnose `session_profile.selected` toe, met de gegevenssleutels `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` en `session_profile.path`. Deze heeft geen invloed op de uitvoerbaarheid. Integrators die de [openbare API](public-api.md) rechtstreeks gebruiken, kunnen `SessionProfileCompiler.Load` en `SessionProfileCompiler.Apply` uit `OmsiLaunch.Core` aanroepen; de YAML-representatie komt nooit in `OmsiLaunch.Api` terecht.

<a id="examples"></a>
## Voorbeelden

<a id="example-1-settings-only-profile-one-preset"></a>
### Voorbeeld 1: profiel met alleen instellingen, één preset

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

Uitvoeren: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. De kaart en het instappunt komen van de opdrachtregel omdat het profiel geen `new`-blok definieert; `/set:graphics.maxFPS=60` toevoegen is toegestaan, `/set:traffic.humans=10` toevoegen is een conflict.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Voorbeeld 2: kaartgebonden profiel met drie presets en meegeleverde assets

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, met `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` en `textures\offline.itx` binnen het pakket:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

Uitvoeren: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Met `/saved:situations\mytrip.osn` wordt het `new`-blok overgeslagen en moet de `.osn` verwijzen naar `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### Het meegeleverde voorbeeld

De release bevat `docs/examples/session-profiles/rmg-leste/profile.yaml` ([bekijken](../../../examples/session-profiles/rmg-leste/profile.yaml)). Het is syntactisch geldig, voldoet aan het schema en zou zonder fouten laden. Twee eigenschappen verhinderen dat het in deze build ongewijzigd een sessie start:

1. Het stelt `new.date`, `new.time` en `new.weather` in, waardoor het plan niet uitvoerbaar is (zie [Het `new`-blok](#new)).
2. De padwaarden zijn plain scalars met dubbele backslashes (`maps\\RMG Leste\\global.cfg`). YAML laat ze dubbel en kaartidentiteiten worden tekstueel vergeleken (alleen na normalisatie van `/` naar `\`), zodat `new.map` en `compatibility.maps` niet overeenkomen met de catalogusidentiteit `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` bij het plannen). De waarde `assets` wordt wel opgelost, omdat de Windows-padnormalisatie dubbele scheidingstekens samenvoegt.

De uitvoerbare vorm voor deze build is:

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## Gerelateerde pagina's

- [CLI-referentie](cli.md) voor `/predefined-profile`, `/predefined-profile-index`, `/set` en de wereldvlaggen
- [LaunchSpec](launchspec.md) voor het record waarnaar een profiel wordt gecompileerd
- [Transacties en recovery](../concepts/transactions-and-recovery.md) voor hoe overlays van `settings`, het opstartscherm en `.itx` worden toegepast en hersteld
- [Capabilities](capabilities.md) en [status van de runtimevalidatie](../status/runtime-validation-status.md)
