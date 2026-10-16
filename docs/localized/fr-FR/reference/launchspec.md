# Référence de LaunchSpec

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> Traduction de la [page originale en anglais](../../../reference/launchspec.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page est la référence normative de `LaunchSpec`, l'enregistrement de requête qui décrit une session OmsiLaunch : sa forme C# (`OmsiLaunch.Api`), sa forme de fichier JSON telle que chargée par la CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), chaque propriété avec son type, sa valeur par défaut, sa règle de validation et son effet actuel, les règles de validation qui rendent un plan non exécutable, et la priorité entre les options de la CLI, les fichiers de spécification et les profils de session. Seul ce que fait le code actuel est documenté.

Pages associées : [API publique](public-api.md), [référence de la CLI](cli.md), [profils de session](session-profiles.md), [codes d'erreur](errors.md), [cycle de vie de la session](../concepts/session-lifecycle.md), [capacités](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## Origine d'un LaunchSpec

| Source | Comment elle devient un `LaunchSpec` |
| --- | --- |
| API | L'intégrateur construit l'enregistrement et le transmet à `PlanSessionAsync`. |
| Options de la CLI | `CliInput.BuildSpecAsync` part des valeurs par défaut intégrées (NEW_MAP, tout non défini, valeurs par défaut de `Behavior`) et applique les options. |
| Fichier JSON `/spec:<path>` | Chargé par `LaunchSpecJson.LoadAsync`, puis utilisé comme base que les options de la CLI remplacent (voir [priorité](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Profil de session (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` écrit dans la base le monde, les paramètres, la présentation, les textures Internet et le comportement du profil, et enregistre les métadonnées `SessionProfile`. |

L'[exemple complet](#complete-example) ci-dessous est vérifié par rapport à la forme de l'enregistrement. Une copie de l'exemple minimal est livrée sous `examples/release-session.example.json` (et `.omsilaunch\examples\release-session.example.json` dans le paquet de version).

<a id="json-form"></a>
## Forme JSON

| Règle | Détail |
| --- | --- |
| Sérialiseur | `System.Text.Json` avec `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true` ; aucun convertisseur n'est enregistré. |
| Noms de propriétés | Les noms de propriétés C# (`Installation`, `RootPath`, ...). La correspondance est insensible à la casse au chargement ; la CLI les écrit en PascalCase. |
| Énumérations | Entiers (il n'existe pas de convertisseur d'énumération en chaîne). `"Mode": 0` est valide ; `"Mode": "NewMap"` est rejeté comme JSON mal formé. Les valeurs sont listées dans [Énumérations](#enumerations). |
| `OptionalValue<T>` | Un objet `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (la valeur est ignorée), 1 = `Set` (la valeur doit être présente et non nulle ; un `Set` avec une valeur nulle n'est pas validé et se comporte comme une valeur invalide). Un membre `OptionalValue` omis vaut `Unset`. Le membre en lecture seule `IsSet` apparaît dans la sortie écrite par la CLI ; il est accepté et ignoré au chargement. |
| Enregistrements facultatifs | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile`, `HofManagement`, `Modules` peuvent être `null` ou omis ; les accesseurs `Effective*` substituent les valeurs par défaut. |
| Enregistrements obligatoires | `Installation`, `World`, `Date`, `Time`, `Environment` (avec les huit dictionnaires, utilisez `{}`), `Behavior` doivent être des objets présents. Ils ne sont pas validés : un enregistrement `null` ou manquant échoue plus tard sur une référence nulle, que la CLI signale comme `OL_E_INTERNAL` (sortie 10) ou `OL_E_INVALID_ARGUMENT` (sortie 2). |
| Propriétés inconnues | Rejetées avant la liaison : `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (le chemin utilise les noms de membres tels qu'écrits dans le fichier). Le contenu des dictionnaires (`Environment.*`) n'est pas vérifié en tant que propriétés. |
| Racine | Doit être un objet JSON : `OL_E_SPEC_INVALID`. Profondeur d'imbrication maximale 32. |
| Taille du fichier | Au plus 1 MiB (1 048 576 octets) : `OL_E_SPEC_TOO_LARGE`. Fichier manquant : `OL_E_SPEC_NOT_FOUND`. |
| Commentaires et virgules finales | Les commentaires `//` et `/* */` ainsi que les virgules finales sont acceptés. |
| JSON mal formé | L'exception de l'analyseur n'est pas traduite : la CLI signale `OL_E_INTERNAL` avec le code de sortie 10. |
| Encodage | UTF-8 (une BOM est tolérée par le lecteur). Les barres obliques inverses dans les identités doivent être échappées (`"maps\\Grundorf\\global.cfg"`) ; les barres obliques sont acceptées pour les identités de carte, de situation, de véhicule et de HOF. |

<a id="complete-example"></a>
## Exemple complet

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

Une date explicite, lorsque le build la prend en charge, s'écrit `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` et une heure `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. Les deux ne s'appliquent qu'aux sessions NEW_MAP (`EXPERIMENTAL` ; voir ci-dessous).

<a id="property-reference"></a>
## Référence des propriétés

La colonne « Consommée » indique ce que le code actuel fait de la valeur. La stabilité utilise le vocabulaire de la [page de l'API publique](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (racine)

| Propriété | Type JSON | Obligatoire | Valeur par défaut si omise | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Installation` | objet `InstallationSpec` | oui | aucune | oui | `STABLE_BETA` |
| `World` | objet `WorldSpec` | oui | aucune | oui | `STABLE_BETA` |
| `Date` | objet `DateSpec` | oui | aucune | oui, NEW_MAP uniquement (voir [`DateSpec`](#datespec-timespec-yearspec)) | `EXPERIMENTAL` |
| `Time` | objet `TimeSpec` | oui | aucune | oui, NEW_MAP uniquement | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | non | `Unset` | résolue pour les diagnostics ; tout champ défini est non exécutable | `PARTIAL` |
| `Environment` | objet `EnvironmentSpec` | oui | aucune | oui (overlay sémantique de `options.cfg`) | `STABLE_BETA` |
| `Behavior` | objet `LaunchBehaviorSpec` | oui | aucune | en partie (voir l'enregistrement) | `STABLE_BETA` / `PARTIAL` |
| `Year` | objet `YearSpec` ou null | non | `null` → `EffectiveYear` = mode `Unset` | oui, NEW_MAP uniquement | `EXPERIMENTAL` |
| `Weather` | objet `WeatherSpec` ou null | non | `null` → `EffectiveWeather` = mode `Unset` | oui, tous les modes de monde, appliquée juste après le démarrage du monde (voir [`WeatherSpec`](#weatherspec)) | `EXPERIMENTAL` |
| `Input` | objet `InputSpec` ou null | non | `null` → `EffectiveInput` = les deux non définis | oui (voir [`InputSpec`](#inputspec)) | `EXPERIMENTAL` |
| `Diagnostics` | objet `DiagnosticsSpec` ou null | non | `null` → `EffectiveDiagnostics` = valeurs par défaut | transportée uniquement | `PARTIAL` |
| `Presentation` | objet `SessionPresentationSpec` ou null | non | `null` → `EffectivePresentation` = écran de démarrage géré, pas de langue, pas de répertoire personnalisé, icône de notification affichée | oui | `STABLE_BETA` |
| `InternetTextures` | objet `InternetTexturesSpec` ou null | non | `null` → `EffectiveInternetTextures` = `Native` | oui | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | objet `SessionProfileMetadata` ou null | non | `null` | provenance uniquement (diagnostic de plan `session_profile.selected`) | `STABLE_BETA` |
| `HofManagement` | objet `HofManagementSpec` ou null | non | `null` → `EffectiveHofManagement` = aucune opération | oui (overlays HOF de session, voir [gestion des HOF](hof-management.md)) | `STABLE_BETA` |
| `Modules` | objet `RuntimeModulesSpec` (`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`) ou null | non | `null` → `EffectiveModules` = rien de demandé, désactivé ou défini | non (les modules ne touchent jamais les fichiers de l'installation via OmsiLaunch ; voir [modules runtime](../../../reference/runtime-modules.md)) | `EXPERIMENTAL` |

Accesseurs en lecture seule (présents dans la sortie JSON de la CLI, ignorés au chargement) : `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, `EffectiveHofManagement`, `EffectiveModules`.

### `InstallationSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | chaîne | obligatoire | Répertoire contenant `Omsi.exe` et `plugins\`. Voir les [règles de chemin](#path-rules). Vide/espaces uniquement → `OL_E_INSTALLATION_NOT_FOUND`. | oui | `STABLE_BETA` |
| `ExpectedExecutableSha256` | chaîne ou null | `null` | `null`, ou 64 chiffres hexadécimaux (sinon `OL_E_INVALID_ARGUMENT`). | Épinglage facultatif : lorsqu'il est défini, le plan n'est pas exécutable sauf si le SHA-256 de `Omsi.exe` lui est égal (`OL_E_UNSUPPORTED_BUILD`, données `expected_sha256` et `file_sha256`). | `STABLE_BETA` |

### `WorldSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | obligatoire | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` est un alias obsolète de même valeur 2). | oui ; `LastMapState` nécessite `MapIdentity` et le `laststn.osn` de la carte (`OL_E_MAP_NOT_FOUND`, `OL_E_SITUATION_NOT_FOUND`) | `NewMap`, `SavedSituation` : `STABLE_BETA` ; `LastMapState` : `EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Pour `NewMap` : obligatoire, de la forme `maps\<dir>\global.cfg` (insensible à la casse, `/` accepté, pas de `..`) et installée. Ignorée pour `SavedSituation` (le `.osn` fournit la carte). | oui (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Pour `SavedSituation` : obligatoire, une identité `situations\...\<file>.osn` installée (telle que renvoyée par `DiscoverAsync(Situations)` / `/list:situations`). | oui (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Pour `NewMap` sans `EntrypointIdentity` : obligatoire, `>= 0`, un index dans la liste des points d'entrée présentée par OMSI pour la carte. Envoyé au plugin sous la forme `-1` lorsqu'il n'est pas défini. | oui (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Un libellé de point d'entrée exact (sensible à la casse) ou une identité de découverte. Le planificateur la résout vers le libellé qu'OMSI présente et le démarrage sélectionne ce libellé (`world.entrypoint-identity`) ; un enregistrement qu'OMSI ne présente pas est refusé (`OL_E_ENTRYPOINT_NOT_PRESENTED`), de même qu'un enregistrement inconnu (`OL_E_ENTRYPOINT_NOT_FOUND`). | transportée | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec` (lecture seule) | calculée | `Mode` = `Identity` lorsque `EntrypointIdentity` est défini, sinon `PresentedIndex` lorsque l'index est défini, sinon `Unset` ; `PresentedIndex`, `Identity` reflètent les entrées. | dérivée | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | obligatoire (`Year` : `0` lorsque l'enregistrement est null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | NEW_MAP : `Explicit` utilise `Value`, `System` l'horloge du PC au moment de la planification de la session ; `Unset` conserve la valeur propre d'OMSI (ses options et son formulaire Start). Le contrôleur résout la demande en horloge de départ d'OMSI (année, jour de l'année, heure, minute, seconde ; capacités `world.explicit-date`, `world.explicit-time`, `world.explicit-year`), le handoff de démarrage (version 7) la transporte, et le plugin l'écrit juste avant le `NewSituation` d'OMSI, de sorte qu'OMSI dérive la saison et le calendrier de la date demandée avant le chargement de la carte. Après le démarrage, l'horloge en cours est relue ; une divergence, ou le fait qu'OMSI utilise une heure de départ fixe, fait échouer le démarrage avec `OL_E_DATE_TIME_APPLY_FAILED`. `SavedSituation` : un mode autre que `Unset` donne `OL_E_CAPABILITY_UNAVAILABLE` (une situation porte sa propre heure). | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate` : une vraie date du calendrier (`Day` valide pour le mois et l'année) ; `SemanticTime` : `Hour` 0..23, `Minute` 0..59, `Second` 0..59 ; années 1900..2200. `YearSpec` définit l'année (et l'année de `Date`, lorsque les deux sont fournies). Le 31 décembre d'une année bissextile est refusé : le calendrier d'OMSI ne peut pas représenter le jour 366. Valeurs invalides : `OL_E_DATE_TIME_APPLY_FAILED`. Doit être définie lorsque `Mode` vaut `Explicit` et non définie sinon (`OL_E_INVALID_ARGUMENT`). | oui | `EXPERIMENTAL` |

### `WeatherSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0` lorsque l'enregistrement est null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Tout mode autre que `Unset` nécessite `world.weather` ; le plugin l'applique juste après le démarrage du monde (tous les modes de monde ; il remplace la météo d'une situation enregistrée). Un échec fait échouer le démarrage avec `OL_E_WEATHER_APPLY_FAILED`. | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Un fichier du dossier `weather` de l'installation, par nom ou `weather\<name>.owt` ; vérifié à la planification (`OL_E_WEATHER_INVALID`). | handoff | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Quatre lettres ou chiffres (`OL_E_WEATHER_INVALID` sinon) ; utilisé par le mode `Icao`. | handoff | `EXPERIMENTAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (dans `PlayerVehicle`)

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Identité `Vehicles\...\<file>.bus` installée, sinon `OL_E_VEHICLE_NOT_FOUND`. | résolue dans `ResolvedContent` ; ensuite `player-vehicle.model` non pris en charge → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Une identité de livrée de `Model` (`<cti>#item:<n>`), sinon `OL_E_REPAINT_NOT_FOUND` ; vérifiée uniquement lorsque `Model` est défini. | idem | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | `Vehicles\...\<file>.hof` installé, sinon `OL_E_HOF_NOT_FOUND`. | idem | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | N'importe quelle chaîne. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | N'importe quelle chaîne. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (lecture seule) | calculée | `true` lorsque `Model` est défini. C'est `PlayerVehicle.IsSet` que le handoff transporte sous le nom `PlayerVehicleEnabled`. | dérivée | `PARTIAL` |

Un `PlayerVehicle` avec `Presence` 1 et tous les champs non définis est accepté et n'a aucun effet. Tout champ défini rend le plan non exécutable sur ce build (`UNAVAILABLE`).

### `EnvironmentSpec`

| Propriété | Type | Valeur par défaut | Consommée | Stabilité |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | `IReadOnlyDictionary<string, OptionalValue<string>>` chacune, obligatoire (`{}` si vide) | aucune | oui | `STABLE_BETA` |

Les huit groupes sont concaténés ; le groupe dans lequel une clé est placée n'a aucun effet. Chaque entrée avec `Presence` 1 est un paramètre sémantique de `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`) ; la clé sélectionne le fichier cible (`options.cfg` pour toutes les clés actuelles) et le jeton. La planification vérifie que la clé existe (`OL_E_UNKNOWN_SETTING`) et qu'elle est accessible en écriture (`OL_E_SETTING_NOT_WRITABLE`) ; la valeur n'est validée qu'au démarrage (`OL_E_INVALID_SETTING_VALUE`, signalé comme une session `Failed` avec `OL_E_START_SESSION`). Les clés sont insensibles à la casse. Les entrées non définies sont ignorées. L'option de la CLI `/set:<key>=<value>` écrit dans `General` ; les `settings` des profils de session sont également fusionnés dans `General`.

| Clé | Valeur | Remarques |
| --- | --- | --- |
| `general.language` | chaîne | jeton `[language]` |
| `general.radio` | chaîne | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | jetons de présence (`autoSave` est l'inverse de `noAutoSave`) |
| `graphics.screenRatio` | chaîne | |
| `graphics.maxFPS` | entier 10..200 | |
| `graphics.tileDistance` | entier 1..20 | |
| `graphics.maxObjectDistanceMeters` | nombre 20..5000 | |
| `graphics.minObjectScreenPercent` | nombre 0..10 | stocké divisé par 100 |
| `graphics.minReflectionObjectScreenPercent` | nombre 0..50 | stocké divisé par 100 |
| `graphics.maxObjectComplexity` | entier 0..3 | |
| `graphics.maxMapComplexity` | entier 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | jetons de présence |
| `graphics.stencilShadows` | `true` / `false` | écrit sous la forme `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | un bloc `smokesystems` |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | jetons de présence |
| `simulation.ticketSelling` | entier 0..2 | |
| `simulation.maintenance` | entier 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | deux jetons OMSI à la fois (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | jetons de présence |
| `traffic.randomVehicles` | entier 0..1000 | composante 0 du bloc multiligne `AIMaxCountRandom` (validé à l'exécution, matrice RV-005) |
| `traffic.humans` | entier 0..1000 | composante 1 de `AIMaxCountRandom` |
| `traffic.factorPercent` | nombre 1..300 | |
| `traffic.parkedVehiclesPercent` | nombre 0..100 | |
| `traffic.scheduledVehicles` | nombre 0..1000 | |
| `traffic.scheduledLinePriority` | nombre 1..4 | |
| `traffic.passengerFactorPercent` | nombre 0..200 | |
| `sound.stereo` | nombre 0..100 | |
| `sound.maxSimultaneousSounds` | nombre 5..1000 | |
| `sound.masterVolume` | nombre 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | rejetées | connues mais non accessibles en écriture → `OL_E_SETTING_NOT_WRITABLE` |

Les fichiers modifiés conservent leur encodage (octets Windows-1252 préservés ; UTF-8/UTF-16 marqués par une BOM respectés) et leurs fins de ligne.

### `LaunchBehaviorSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | `true` uniquement | La restauration à l'identique est obligatoire : `false` est rejeté avec `OL_E_INVALID_ARGUMENT`. | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | toutes | `true` : un fichier `closecheck` qui existe avant la session est supprimé définitivement au démarrage (diagnostic `closecheck.stale-removed` avec son SHA-256 ; échec `OL_E_CLOSECHECK_REMOVE_FAILED`). `false` : un `closecheck` existant est laissé tel quel et ne constitue pas une suppression de session. Le `closecheck` qu'OMSI écrit pendant la session est toujours supprimé lors de la restauration. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (sinon `ArgumentOutOfRangeException` levée par `StartSessionAsync` ; l'option de la CLI `/startup-timeout` impose 1..600 ; les profils exigent > 0). Budget de temps entre le démarrage du superviseur et `Running` ; à l'expiration, la session échoue avec `OL_E_STARTUP_TIMEOUT` (plugin démarré) ou `OL_E_PLUGIN_NOT_LOADED`. | oui | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600 (CLI `/shutdown-timeout`) | Avec `CooperativeShutdown` : le temps dont dispose OMSI pour se fermer de lui-même avant d'être terminé. Sans cette option, OMSI est terminé immédiatement. | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | toutes (CLI `/cooperative-shutdown`) | `true` : terminer une session en cours demande à OMSI de se fermer par sa propre routine de sortie (`TForm_main.CloseOMSI` : les options et le `laststn.osn` de la carte sont enregistrés, les plugins finalisés) au lieu de le terminer ; après `ShutdownTimeoutSeconds`, il est terminé (`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`). La restauration s'exécute après la sortie, de sorte que les fichiers détenus par la session retrouvent toujours leurs octets d'origine. | `EXPERIMENTAL` |

### `InputSpec`

| Propriété | Type | Valeur par défaut | Consommée | Stabilité |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Chemin (absolu, ou relatif à l'installation) d'un fichier clavier au format d'OMSI. La session l'utilise à la place de `Inputs\keyboard.cfg` (un overlay de transaction : le fichier de l'installation est restauré octet pour octet à la fin). Le fichier doit exister, faire au plus 1 MiB et contenir au moins une liaison `[entry]` ; sinon `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Chemin d'un fichier de manette au format d'OMSI, utilisé à la place de `Inputs\gamectrler.cfg` pour la session de la même manière. Il doit contenir au moins un périphérique `[ctrl]` ; sinon `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| Propriété | Type | Valeur par défaut | Consommée | Stabilité |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Aucun consommateur dans `src/`. La trace de l'hôte `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` est toujours écrite. | `PARTIAL` (transportée, actuellement sans effet) |
| `Verbose` | bool | `false` | Aucun consommateur. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Aucun consommateur. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Aucun consommateur. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Indicateur du handoff de démarrage : le plugin écrit des événements `plugin.trace.operation` (opération, résultat, code, durée en microsecondes) pour chaque opération runtime ; ils parviennent à `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` par l'anneau de télémétrie. | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | Indicateur du handoff de démarrage : la couche native signale chaque installation et suppression de hook (`native.trace` : frontière, propriétaire, cible, résultat) dans la même trace de l'hôte. | `EXPERIMENTAL` |

Les options de la CLI `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` renseignent ces booléens (`/logall` définit `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`) ; elles sont combinées par OU avec les valeurs de la spécification.

### `SessionPresentationSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`) : les fichiers d'écran de démarrage propres à OMSI ne sont pas modifiés. `1` `Managed` : OmsiLaunch applique en overlay `GUI\NewSplashscreen_ENG.bmp` et `GUI\NewSplashscreen_<LANG>.bmp` pour la session (restaurés à l'identique ensuite). | oui | `STABLE_BETA` (matrice RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (insensible à la casse) ; toute autre valeur est normalisée en `ENG`. Lorsqu'elle n'est pas définie, la valeur `[language]` de `options.cfg` est lue et normalisée de la même manière. | oui (écran de démarrage géré uniquement) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Répertoire contenant `ENG.bmp` et `<LANG>.bmp` (640×480, BMP 24 bits). Voir les [règles de chemin](#path-rules). Répertoire manquant : `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` ; fichier manquant : `OL_E_SPLASH_ASSET_MISSING` ; format incorrect : `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Lorsqu'elle n'est pas définie, `<root>\.omsilaunch\assets\splash` est utilisé (initialisé une fois à partir du paquet), sinon le `assets\splash` du paquet. | oui (écran de démarrage géré uniquement) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` supprime l'indicateur autonome de la zone de notification Windows du propriétaire CLI. | Propriétaire CLI uniquement ; l'API n'a pas d'icône de notification. Il n'existe pas d'option de la CLI ; la valeur ne peut provenir que d'un fichier de spécification. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Propriété | Type | Valeur par défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native` : rien ne change. `1` `Disabled` : le plugin neutralise le téléchargeur intégré au processus d'OMSI (télémétrie `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override` : le profil `.itx` est appliqué en overlay sous `Texture\standard.itx` ; ses fichiers cibles et `Texture\standard.ipr` deviennent des suppressions de session. | oui | `Native` : `STABLE_BETA` ; `Disabled`, `Override` : `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Obligatoire pour `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Un fichier texte composé de paires de lignes : une URL `http`/`https` absolue, puis un chemin cible relatif à la racine de l'installation qui contient un composant `Texture\`, n'est pas enraciné, ne contient pas `..`, ne commence pas par `\` et ne traverse aucune jonction ni aucun lien symbolique (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Voir les [règles de chemin](#path-rules). | oui | `EXPERIMENTAL` |

### `HofManagementSpec`

| Propriété | Type | Défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Operations` | tableau de `HofOperationSpec` | aucune | Appliquées dans l'ordre ; voir [gestion des HOF](hof-management.md) pour la résolution, les règles de conflit et les limites. | oui | `STABLE_BETA` |

Membres de `HofOperationSpec` : `SourceKind` (`HofSourceKind` int), `Source` (chaîne), `TargetVehicles` (tableau d'identités de véhicules ou de dossiers sous `Vehicles\`, au moins un), `Mode` (`HofApplyMode` int, `0` `Add` par défaut), `Files` (tableau de noms de fichiers `.hof` ou null pour tous). Les problèmes sont des diagnostics de plan `OL_E_HOF_*`.

### `RuntimeModulesSpec`

| Propriété | Type | Défaut | Valeurs valides | Consommée | Stabilité |
| --- | --- | --- | --- | --- | --- |
| `Enable` | tableau d'ids de modules | aucune | Ids de modules installés sous `plugins\OmsiLaunch.Modules` ; voir [modules runtime](../../../reference/runtime-modules.md). Un module demandé qui ne peut pas être chargé est un diagnostic de plan `OL_E_MODULE_*` (plan non exécutable). | oui | `EXPERIMENTAL` |
| `Disable` | tableau d'ids de modules ou null | aucune | Garde un module par défaut, de contenu ou du fichier de paramètres hors de la session (diagnostic de plan `module.disabled`). Un module à la fois activé et désactivé donne `OL_E_MODULE_SETTING_INVALID`. Accesseur en lecture seule `EffectiveDisable`. | oui | `EXPERIMENTAL` |
| `Settings` | objet de chaînes ou null | aucune | `"<module id>.<key>": "<value>"`. L'emporte sur le fichier de paramètres `.omsilaunch\modules.ini` et sur la valeur par défaut du manifeste. Une valeur invalide ou une clé que le module ne déclare pas donne `OL_E_MODULE_SETTING_INVALID` (plan non exécutable) ; les paramètres d'un module que la session ne charge pas donnent `OL_W_MODULE_SETTING_IGNORED`. Les valeurs effectives figurent dans `PlannedModule.Settings`. Accesseur en lecture seule `EffectiveSettings`. | oui | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Propriété | Type | Consommée | Stabilité |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | chaînes / int | Enregistrées dans le diagnostic de plan `session_profile.selected` (`Data["session_profile.*"]`). Pas consommées autrement ; normalement renseignées par le compilateur de profils de session, pas à la main. | `STABLE_BETA` |

<a id="enumerations"></a>
## Énumérations

| Énumération | Valeurs (entier JSON) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsolète ; il s'agit de la branche native d'OMSI « dernier état de la carte », jamais du `.osn` le plus récent) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `HofSourceKind` | `Collection` = 0, `Directory` = 1, `Vehicle` = 2, `File` = 3 |
| `HofApplyMode` | `Add` = 0, `Mirror` = 1 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (`Entrypoint.Mode` en lecture seule) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Les entiers hors de la plage déclarée sont stockés tels quels par le sérialiseur et se comportent comme des valeurs inconnues (par exemple, un `WorldMode` inconnu n'est ni NEW_MAP ni SAVED_SITUATION et produit un plan sans capacité de monde ; le plugin le rejetterait, mais la CLI remplace de toute façon le mode, voir la priorité).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Règles de validation et diagnostics de non-exécutabilité

`PlanSessionAsync` exécute `LaunchValidation.Validate` puis `SessionPlanner.PlanAsync`. Un plan est exécutable exactement lorsqu'aucun code de diagnostic ne commence par `OL_E_`. L'ensemble complet :

| Diagnostic | Condition | Source |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` vide ou composé uniquement d'espaces | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` sans valeur définie ou avec un mois/jour hors plage ; `Time.Mode` = `Explicit` sans valeur définie ou avec une heure/minute/seconde hors plage | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` ou `Time.Value` définie alors que le mode ne vaut pas `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` avec `MapIdentity` non définie ou pas de la forme `maps\...\global.cfg` (validation) ; `NewMap` avec une identité qui n'est pas installée (planificateur) | les deux |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` sans `EntrypointIdentity` et avec `PresentedEntrypointIndex` non défini ou négatif | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, carte installée, pas de `EntrypointIdentity`, `PresentedEntrypointIndex` non défini (`world.presented-entrypoint` indisponible) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` sans `SituationIdentity` (validation) ou avec une identité qui n'est pas installée (planificateur) | les deux |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation` : la carte nommée dans le `.osn` n'est pas installée | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | pas Windows 10+ sur un système x64 avec un processus hôte x64 (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | répertoire racine manquant, attribut lecture seule défini ou absence de sous-répertoire `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | Pas une installation OMSI 2 : `Omsi.exe` manquant, pas une image PE x86 32 bits, pas de version OMSI 2 (2.x), ou pas de répertoire `maps\` / `Vehicles\` (`omsi.omsi2`). Le hash n'est pas un critère. | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `EntrypointIdentity` défini ; mode de `Date`/`Time`/`Year` différent de `Unset` avec `SavedSituation` ; mode de `Weather` différent de `Unset` ; un champ quelconque de `PlayerVehicle` défini | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` non installé (en plus de `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | une clé de `Environment` absente du catalogue / non accessible en écriture | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | la construction du plan d'écran de démarrage/ITX a levé une exception ; le message porte `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` ou `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | la référence de la closure du plugin (`OmsiLaunchRuntimePaths`) ou le manifeste de version ne peut pas être chargé (le message peut porter `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Non validé au moment de la planification (échoue au démarrage sous la forme d'une session `Failed` avec `OL_E_START_SESSION`) : valeurs des paramètres (`OL_E_INVALID_SETTING_VALUE`), intégrité du plugin permanent (`OL_E_PERMANENT_PLUGIN_*`), disponibilité du bail (`OL_E_INSTALLATION_BUSY`), plage de `StartupTimeoutSeconds` (levée par `StartSessionAsync`).

Diagnostics de plan informatifs : `plugin.integrity.reference` (message `manifest` ou `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Priorité : options de la CLI, fichier de spécification et profil de session

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) construit la spécification effective dans cet ordre :

1. Base = valeurs par défaut intégrées, ou le fichier `/spec` lorsqu'il est fourni.
2. Racine de l'installation = l'argument d'installation explicite s'il est fourni, sinon le `RootPath` de la base ; ensuite `.`/vide → répertoire de l'exécutable, `Path.GetFullPath`. Un argument d'installation explicite l'emporte toujours sur le `RootPath` de la spécification.
3. Profil de session (`/predefined-profile` + `/predefined-profile-index`) : les arguments explicites de la CLI qui touchent un champ détenu par le profil sont rejetés avec `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (champs de monde uniquement en mode NEW_MAP ; clés `/set` présentes dans le préréglage ; options d'écran de démarrage lorsque le préréglage a `presentation` ; options de textures Internet lorsqu'il a `internet-textures` ; timeouts lorsqu'il a `behavior`). Le `World` de la base est remplacé par un nouveau (seul le mode de monde de la CLI subsiste), puis le bloc `new:` du profil (NEW_MAP uniquement), `settings` (dans `General`), `presentation`, `internet-textures`, `behavior` et les métadonnées `SessionProfile` sont appliqués. `compatibility.maps` est appliqué pour NEW_MAP et SAVED_SITUATION.
4. Monde : le mode de monde de la CLI l'emporte toujours (`/new` par défaut, `/saved:<osn>`, `/last`) ; le `World.Mode` du fichier de spécification est remplacé. Pour exécuter une situation enregistrée à partir d'une spécification, passez `/saved:`. `/map` et `/entrypoint`/`/entrypoint-index` remplacent la base ; une identité `/entrypoint` de la CLI efface l'index ; `/saved` avec `/map` ou des options de point d'entrée donne `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` remplacent la base lorsqu'ils sont fournis (`system` sélectionne `DateTimeMode.System`).
6. `/no-vehicle` efface `PlayerVehicle` ; les options individuelles `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` remplacent les champs individuels du véhicule du joueur de la base.
7. Les entrées `/set:<key>=<value>` sont ajoutées à `Environment.General` (clé vérifiée, valeur non) ; les sept autres groupes proviennent de la base sans modification.
8. `/startup-timeout` et `/shutdown-timeout` ne remplacent la base que lorsqu'ils sont fournis ; sinon s'appliquent la spécification, puis le profil, puis les valeurs par défaut 180 s / 30 s. `ShutdownTimeoutSeconds` prend effet avec `/cooperative-shutdown` (combiné par OU avec la valeur de la spécification).
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` remplacent la base lorsqu'ils sont fournis ; `SuppressTrayIcon` provient uniquement de la base.
10. Les options de diagnostic sont combinées par OU avec la base.

Résultat : option explicite de la CLI > profil de session > fichier de spécification > valeur par défaut intégrée, à ceci près qu'une option de la CLI en conflit avec un champ détenu par le profil est une erreur et non un remplacement.

<a id="path-rules"></a>
## Règles de chemin

| Chemin | Comportement de l'API | Comportement de la CLI |
| --- | --- | --- |
| `Installation.RootPath` | Utilisé tel quel : dans les opérations sur les fichiers, les chemins relatifs sont résolus par rapport au répertoire de travail du processus. Passez un chemin absolu. Le bail, le journal et le catalogue de contenu le normalisent avec `Path.GetFullPath`. | `.` ou vide = le répertoire qui contient `OmsiLaunch.exe`, jamais le dossier de travail de l'appelant ; un argument d'installation explicite l'emporte sur la spécification ; le résultat est rendu absolu. |
| `Presentation.CustomAssetDirectory` | Absolu, ou relatif à `Installation.RootPath`. Doit exister. | Idem (`/splash-assets`). Un chemin `assets` de profil de session est confiné au paquet du profil et stocké sous forme absolue. |
| `InternetTextures.OverrideProfilePath` | Résolu avec `Path.GetFullPath`, c'est-à-dire relativement au répertoire de travail du processus, et non à la racine de l'installation. Doit exister. | Idem (`/internet-textures-profile`). Un chemin `profile` de profil de session est confiné au paquet et stocké sous forme absolue. |
| Lignes cibles ITX | Relatives à la racine de l'installation ; doivent contenir un composant `Texture\` ; pas de racine, pas de `..`, pas de `\` initial, aucun composant jonction/lien symbolique. | Idem. |
| Identités de contenu (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Relatives à l'installation, insensibles à la casse, `/` accepté ; jamais absolues. | Idem. |

<a id="carried-but-not-applied"></a>
## Transporté mais non appliqué

| Champ | Effet actuel | Stabilité |
| --- | --- | --- |
| `Diagnostics.Log`, `Verbose`, `OmsiLogAll`, `ProcessTrace` | aucun (la trace de l'hôte est toujours écrite) | `PARTIAL` |
| `PlayerVehicle.*` (tout champ défini) | contenu résolu pour les diagnostics, plan non exécutable (`UNAVAILABLE`) | `PARTIAL` |
| `SessionProfile` | diagnostic de provenance uniquement | `STABLE_BETA` |
