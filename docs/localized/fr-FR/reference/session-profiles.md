# Profils de session

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Traduction de la [page originale en anglais](../../../reference/session-profiles.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Un profil de session est un paquet YAML déclaratif qu'un auteur de contenu livre avec une carte ou un add-on afin que les utilisateurs finaux puissent démarrer une session OmsiLaunch reproductible avec une seule commande (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Cette page est la référence normative du format `omsilaunch.session-profile/v1` tel qu'implémenté par `SessionProfileCompiler` dans `src/OmsiLaunch.Core/SessionProfiles.cs`, des règles de priorité appliquées par la CLI (`CliInput.BuildSpecAsync` et `RejectProfileConflicts` dans `tools/OmsiLaunch.Cli/Program.cs`) et du catalogue de paramètres qu'un profil peut écrire (`ConfigurationCatalog`). Tout ce qu'un profil peut faire, les options de la CLI et le [LaunchSpec](launchspec.md) peuvent aussi le faire ; un profil ne fait qu'empaqueter ces choix.

Stabilité : `STABLE_BETA` pour l'analyse, la validation, la détection des conflits et les blocs `settings` / `presentation` / `internet-textures` / `behavior` (test hors ligne `session-profiles.strict-compiler` ; le chemin d'overlay et de restauration est validé à l'exécution par RV-005 et RV-006, voir l'[état de la validation à l'exécution](../status/runtime-validation-status.md)). Les clés `new.date`, `new.time`, `new.year` et `new.weather` sont `UNAVAILABLE` dans ce build (voir [Le bloc `new`](#new)).

<a id="package-location-and-naming"></a>
## Emplacement et nommage du paquet

| Élément | Règle |
| --- | --- |
| Répertoire du paquet | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Fichier de profil | `<package>\profile.yaml` (nom exact, un seul fichier) |
| Ressources | Tous fichiers ou répertoires situés dans le répertoire du paquet, référencés par un chemin relatif depuis `presentation.splash.assets` et `internet-textures.profile` |
| `id` | Doit être un simple nom de répertoire : il ne doit pas être vide ni composé uniquement d'espaces, ne doit pas contenir `\`, `/` ou `:`, et ne doit pas contenir la séquence `..`. Les violations donnent `OL_E_SESSION_PROFILE_PATH_ESCAPE`. La valeur `id` déclarée dans `profile.yaml` doit être identique octet pour octet au nom du répertoire (sensible à la casse) ; sinon `OL_E_SESSION_PROFILE_INVALID`. |
| Sélection | `/predefined-profile:<id>` accompagné de `/predefined-profile-index:<n>`. L'index est obligatoire : `/predefined-profile` sans `/predefined-profile-index` échoue avec `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Paquet manquant | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Disposition de la version | Le paquet de version livre un exemple sous `.omsilaunch\examples\session-profiles\rmg-leste\` (voir [empaquetage](packaging.md)). Les exemples ne sont pas des profils : copiez un paquet dans `.omsilaunch\session-profiles\<id>\` pour le rendre sélectionnable. |

Un profil est installé et supprimé par l'utilisateur ou l'auteur du contenu. OmsiLaunch n'écrit jamais dans un paquet, ne le copie jamais et ne le supprime jamais. Le répertoire du paquet ne fait partie d'aucune transaction.

<a id="parsing-rules"></a>
## Règles d'analyse

| Règle | Comportement | Erreur |
| --- | --- | --- |
| Limite de taille | `profile.yaml` ne doit pas dépasser 256 KiB (262,144 octets) | `OL_E_SESSION_PROFILE_INVALID` |
| Forme du document | Exactement un document YAML dont le nœud racine est un mapping | `OL_E_SESSION_PROFILE_INVALID` |
| Schéma | `schema` doit valoir exactement `omsilaunch.session-profile/v1` (sensible à la casse) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Ancres et alias | Tout nœud portant une ancre YAML (`&name`) où que ce soit dans le document est rejeté avant la validation ; des alias (`*name`) ne peuvent donc pas apparaître | `OL_E_SESSION_PROFILE_INVALID` (« YAML anchors are not supported. ») |
| Clés inconnues | Chaque mapping est fermé : une clé qui n'est pas listée pour son contexte dans les tableaux ci-dessous est rejetée (« Unknown property in `<context>`: `<key>` »). Les clés sont comparées en respectant la casse (`Schema:` est une clé inconnue). Le seul mapping ouvert est `settings`, dont les clés sont validées par rapport au catalogue de paramètres. | `OL_E_SESSION_PROFILE_INVALID` |
| Scalaires | Chaque valeur feuille doit être un scalaire ; les séquences et mappings placés là où un scalaire est attendu sont rejetés (« `<field>` must be a scalar. ») | `OL_E_SESSION_PROFILE_INVALID` |
| Nombres | Les entiers sont analysés avec la culture invariante (`1`, `30`) ; les décimaux dans `settings` utilisent `.` comme séparateur | `OL_E_SESSION_PROFILE_INVALID` |
| Dates et heures | `new.date.value` est analysée par `DateOnly.Parse` et `new.time.value` par `TimeOnly.Parse`, toutes deux avec la culture invariante ; utilisez les formes ISO `yyyy-MM-dd` et `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| Erreurs de syntaxe YAML | Signalées avec le message de l'analyseur | `OL_E_SESSION_PROFILE_INVALID` (« Invalid YAML: ... ») |
| Contenu exécutable | Le YAML est analysé avec `YamlDotNet` uniquement en arbre de représentation ; aucun tag, type personnalisé ni exécution de code n'est pris en charge |

Les barres obliques inverses dans les scalaires simples (non entre guillemets) sont des caractères littéraux. Écrivez les chemins Windows avec une seule barre oblique inverse (`maps\Grundorf\global.cfg`). Une barre oblique inverse doublée dans un scalaire simple reste doublée dans la valeur ; voir [L'exemple livré](#the-packaged-example).

<a id="key-reference"></a>
## Référence des clés

Les contextes sont nommés exactement comme le compilateur les nomme. Chaque clé listée ici est acceptée ; aucune autre ne l'est.

<a id="profile-root-mapping"></a>
### `profile` (mapping racine)

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `schema` | chaîne | oui | Littéral `omsilaunch.session-profile/v1`. |
| `id` | chaîne | oui | Identifiant du paquet ; doit être égal au nom du répertoire. |
| `name` | chaîne | oui | Nom d'affichage ; reporté dans `SessionProfileMetadata.Name`. |
| `author` | chaîne | oui | Auteur ; reporté dans `SessionProfileMetadata.Author`. |
| `version` | chaîne | oui | Chaîne de version du paquet (forme libre, mettez-la entre guillemets : `"1.0"`) ; reportée dans `SessionProfileMetadata.Version`. |
| `compatibility` | mapping | non | Voir `compatibility`. |
| `new` | mapping | non | Valeurs par défaut NEW_MAP. Voir `new`. |
| `presets` | séquence de mappings | oui | 1 à 99 entrées de préréglage. Zéro, plus de 99, ou une valeur qui n'est pas une séquence donne `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `maps` | séquence de chaînes | non | Identités de carte (`maps\<Map>\global.cfg`) pour lesquelles ce profil est valide. `/` est normalisé en `\` ; la comparaison est insensible à la casse. Une liste absente ou vide signifie « n'importe quelle carte ». Lorsqu'elle n'est pas vide, elle est appliquée pour `WorldMode.NewMap` (par rapport au `new.map` effectif ou à `/map`) et pour `WorldMode.SavedSituation` (par rapport à la carte référencée par le `.osn` sélectionné, résolue via le catalogue de contenu). Pour `WorldMode.LastMapState`, aucune carte ne peut être déduite, si bien qu'une liste non vide échoue toujours. Échec : `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

Le bloc est lu et validé dès qu'il est présent, mais il n'est appliqué à la spécification que lorsque le mode de monde sélectionné est NEW_MAP (`/new`, la valeur par défaut de la CLI). Sous `/saved:<file.osn>`, le bloc est ignoré.

| Clé | Type | Obligatoire | Appliquée | Description |
| --- | --- | --- | --- | --- |
| `map` | chaîne | non | oui | Identité de carte sous la forme normalisée `maps\<Map>\global.cfg` (la planification exige exactement cette forme : commence par `maps\`, se termine par `\global.cfg`, pas de `..`). Définit `WorldSpec.MapIdentity`. |
| `entrypoint-index` | entier | non | oui | Index du point d'entrée présenté (position à partir de 0 dans la liste des points d'entrée d'OMSI). Définit `PresentedEntrypointIndex` et efface toute identité de point d'entrée. |
| `entrypoint` | chaîne | non | oui | Identité brute du point d'entrée. Définit `EntrypointIdentity` et efface l'index présenté. Si `entrypoint-index` et `entrypoint` sont tous deux présents, `entrypoint` l'emporte car il est appliqué en dernier. La valeur est un libellé exact (sensible à la casse) ou une identité de découverte ; OMSI ne présente que le premier enregistrement de chaque libellé (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapping | non | non (`UNAVAILABLE`) | Voir `new.date`. |
| `time` | mapping | non | non (`UNAVAILABLE`) | Voir `new.time`. |
| `year` | entier | non | non (`UNAVAILABLE`) | Année explicite. |
| `weather` | mapping | non | non (`UNAVAILABLE`) | Voir `new.weather`. |

`date`, `time`, `year` et `weather` sont compilés en `DateSpec`, `TimeSpec`, `YearSpec` et `WeatherSpec` avec `DateTimeMode.Explicit` / le `WeatherMode` sélectionné. Le planificateur de session (`src/OmsiLaunch.Core/SessionPlanner.cs`) exige `world.explicit-date`, `world.explicit-time`, `world.explicit-year` et `world.weather` ; les quatre sont `EXPERIMENTAL` (fermées statiquement, validation à l'exécution en attente). La date/heure de départ ne s'applique qu'à NEW_MAP ; la météo s'applique à tous les modes de monde.

#### `new.date`

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `mode` | chaîne | oui | Doit valoir `explicit` (insensible à la casse). Toute autre valeur donne `OL_E_SESSION_PROFILE_INVALID` (« date must use explicit mode. »). |
| `value` | chaîne | oui | `yyyy-MM-dd`. |

#### `new.time`

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `mode` | chaîne | oui | Doit valoir `explicit`. |
| `value` | chaîne | oui | `HH:mm` ou `HH:mm:ss`. |

#### `new.weather`

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `mode` | chaîne | oui | `preset`, `icao` ou `real` (insensible à la casse). Toute autre valeur : `OL_E_SESSION_PROFILE_INVALID` (« Unsupported weather mode »). |
| `preset` | chaîne | lorsque `mode: preset` | Nom du préréglage météo. |
| `icao` | chaîne | lorsque `mode: icao` | Code de station OACI. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (chaque entrée de `presets`)

| Clé | Type | Obligatoire | Valeur par défaut | Description |
| --- | --- | --- | --- | --- |
| `index` | entier | oui | | 1 à 99, unique dans le profil ; les index n'ont pas besoin d'être contigus. Sélectionné avec `/predefined-profile-index`. Doublon ou hors plage : `OL_E_SESSION_PROFILE_INVALID` ; un index qui n'existe nulle part dans le profil : `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | chaîne | oui | | Identifiant du préréglage ; reporté dans `SessionProfileMetadata.PresetId`. |
| `name` | chaîne | oui | | Nom d'affichage du préréglage ; reporté dans `SessionProfileMetadata.PresetName`. |
| `settings` | mapping | non | aucune | Paramètres sémantiques de `options.cfg`, voir [Paramètres](#settings). Les clés sont comparées au catalogue sans tenir compte de la casse. |
| `presentation` | mapping | non | héritée | Présentation de l'écran de démarrage, voir `presentation`. En son absence, le préréglage hérite de la base (valeur de `/spec` ou valeur par défaut de la CLI, `Managed`). |
| `internet-textures` | mapping | non | héritée | Voir `internet-textures`. |
| `behavior` | mapping | non | héritée | Timeouts, voir `behavior`. |
| `hofs` | séquence de mappings | non | héritée | Opérations HOF de session, voir `hofs`. Lorsqu'elles sont présentes, elles remplacent le `HofManagement` de base. |
| `modules` | mapping | non | héritée | Modules runtime, voir `modules`. Joints à ce qu'un fichier `/spec` demande. |

Seul le préréglage sélectionné est appliqué. Chaque préréglage est néanmoins analysé et validé, si bien qu'une erreur dans le préréglage 3 fait échouer une demande portant sur le préréglage 1.

### `presentation`

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `splash` | mapping | oui | Obligatoire lorsque `presentation` est présent (« Presentation requires splash. »). Voir `presentation.splash`. |

#### `presentation.splash`

| Clé | Type | Obligatoire | Valeur par défaut | Description |
| --- | --- | --- | --- | --- |
| `mode` | chaîne | oui | | `managed` installe les bitmaps d'écran de démarrage d'OmsiLaunch pour la session (`SplashMode.Managed`). `unset` ou `native` préserve les fichiers d'écran de démarrage propres à OMSI (`SplashMode.Unset` ; `Native` est un alias). Insensible à la casse. Toute autre valeur : `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | chaîne | non | `ENG` | Langue de la seconde cible d'écran de démarrage : `PTB`, `ENG`, `DEU`, `FRA` (alias `PT-BR`, `EN`, `DE`, `FR` ; toute valeur inconnue est résolue en `ENG` lors de la construction de la session). Avec `mode: managed`, la session applique en overlay `GUI\NewSplashscreen_ENG.bmp` et `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | chaîne | non | ressources du paquet | Répertoire **relatif au paquet**, contenant `ENG.bmp` et, pour une `language` autre que l'anglais, `<language>.bmp` ; chacun doit être un BMP 640x480 en 24 bits. Le répertoire doit exister au chargement du profil (`OL_E_SESSION_PROFILE_ASSET_MISSING`) ; les fichiers sont validés au démarrage de la session (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Les règles de confinement des chemins s'appliquent. En cas d'omission, le `.omsilaunch\assets\splash` de l'installation (ou les valeurs par défaut du paquet) est utilisé. |

Un profil ne peut pas définir `SessionPresentationSpec.SuppressTrayIcon` ; la valeur reste `false` sauf si un `/spec` la définit.

### `internet-textures`

| Clé | Type | Obligatoire | Description |
| --- | --- | --- | --- |
| `mode` | chaîne | oui | `native` (`InternetTexturesMode.Native`, OMSI se comporte normalement), `disabled` (`Disabled`, le téléchargeur intégré au processus, profilé, est neutralisé pour la session), `override` (`Override`, un profil `.itx` limité à la session est installé sous `Texture\standard.itx`). Insensible à la casse ; toute autre valeur : `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | chaîne | obligatoire pour `override` | Chemin **relatif au paquet** du fichier `.itx`. Clé manquante avec `override` : `OL_E_SESSION_PROFILE_INVALID` ; fichier manquant : `OL_E_SESSION_PROFILE_ASSET_MISSING`. Les règles de confinement des chemins s'appliquent. Le fichier doit être composé de paires de lignes `URL` / `target` avec des URL `http://` ou `https://` (sinon `OL_E_ITX_PROFILE_INVALID`) et chaque cible doit se résoudre sous le répertoire `Texture\` de l'installation sans traverser de point d'analyse (reparse point) (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Les cibles listées et `Texture\standard.ipr` deviennent des suppressions de session (voir [transactions et récupération](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Clé | Type | Obligatoire | Valeur par défaut | Description |
| --- | --- | --- | --- | --- |
| `startup-timeout` | entier (secondes) | non | 180 | Temps accordé entre le démarrage du processus et `Running`. Doit être positif au chargement du profil ; la session exige en outre une valeur de 1 à 600 au démarrage (sinon `OL_E_START_SESSION`). Correspond à `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | entier (secondes) | non | 30 | Correspond à `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT : le superviseur termine OMSI directement et ne lit jamais cette valeur. |

Lorsque le bloc `behavior` est présent, les deux timeouts sont définis (valeur fournie ou valeur par défaut) et remplacent entièrement le `LaunchBehaviorSpec` de base, y compris `RestoreConfiguration` et `SuppressStaleClosecheckWarning`, qui reviennent à leurs valeurs par défaut (`true`, `true`).

### `hofs`

Chaque entrée est une opération de [gestion des HOF](hof-management.md) et nomme exactement une clé de source (`package`, `collection`, `directory`, `vehicle` ou `file`) ; zéro ou plusieurs : `OL_E_SESSION_PROFILE_INVALID`. L'existence des sources et des cibles est vérifiée lors de la planification de la session (`OL_E_HOF_*`), sauf pour `package`.

| Clé | Type | Obligatoire | Valeur par défaut | Description |
| --- | --- | --- | --- | --- |
| `package` | chaîne | une clé de source | | Répertoire **relatif au paquet** contenant les fichiers `.hof` que le profil livre. Doit exister au chargement du profil (`OL_E_SESSION_PROFILE_ASSET_MISSING`) ; les règles de confinement des chemins s'appliquent. Devient une source `Directory`. |
| `collection` | chaîne | une clé de source | | Nom d'une collection sous `.omsilaunch\hof\`. |
| `directory` | chaîne | une clé de source | | Répertoire relatif à l'installation, par exemple à côté du contenu d'une carte. |
| `vehicle` | chaîne | une clé de source | | Identité de véhicule ou dossier dont les HOF sont copiés. |
| `file` | chaîne | une clé de source | | Fichier `.hof` relatif à l'installation. |
| `targets` | séquence de chaînes | oui | | Véhicules cibles (identités ou dossiers sous `Vehicles\`). Absent ou vide : `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | séquence de chaînes | non | tous | Noms de fichiers à prendre dans la source. |
| `mode` | chaîne | non | `add` | `add` ou `mirror` (insensible à la casse) ; toute autre valeur : `OL_E_SESSION_PROFILE_INVALID`. |

Un préréglage avec `hofs` combiné à une option `/hof-source` : `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Une entrée par id de [module runtime](runtime-modules.md) (`EXPERIMENTAL`). Chaque entrée est un mapping avec ces clés (contexte `module`) :

| Clé | Type | Obligatoire | Valeur par défaut | Description |
| --- | --- | --- | --- | --- |
| `enabled` | `true` ou `false` | non | non défini | `true` demande le module (`LaunchSpec.Modules.Enable`) ; `false` le garde hors de la session, y compris un module chargé par défaut, par sa carte ou par le fichier de paramètres (`LaunchSpec.Modules.Disable`). |
| `settings` | mapping | non | aucun | Paramètres du module par clé (`LaunchSpec.Modules.Settings` sous la forme `<id>.<key>`). Les valeurs sont vérifiées par rapport aux paramètres déclarés du module lors de la planification de la session (`OL_E_MODULE_SETTING_INVALID`). |

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

Un id de module qui n'est pas `[a-z0-9][a-z0-9-]{1,47}`, un `enabled` autre que `true` ou `false`, ou une clé de paramètre qui n'est pas `[a-z][a-z0-9_]{0,31}` : `OL_E_SESSION_PROFILE_INVALID`. Un module qui n'est pas installé est signalé lors de la planification de la session (`OL_E_MODULE_NOT_FOUND` pour `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` pour des paramètres seuls).

<a id="settings"></a>
## Paramètres

Les clés de `settings` sont les noms sémantiques de `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). Le compilateur n'accepte une clé que si elle existe (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) et qu'elle est accessible en écriture (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Les valeurs sont stockées sous forme de chaînes et converties en correctif de `options.cfg` lorsque la session construit ses overlays ; une valeur invalide est donc détectée lors de `StartSessionAsync`, et non au chargement du profil, et fait échouer la session avec `OL_E_START_SESSION`, dont le message porte `OL_E_INVALID_SETTING_VALUE: <key>`. Chaque paramètre ci-dessous écrit dans `options.cfg` ; tous sont limités à la session et restaurés à l'identique après la session.

Formes de valeurs :

- **bool** vaut `true` ou `false` (insensible à la casse). Pour les jetons de présence, le jeton est ajouté ou retiré ; pour les jetons inversés (`no_*`), `true` retire le jeton négatif.
- **int** / **decimal** sont validés par rapport à la plage ; les valeurs avec un diviseur sont stockées divisées (par exemple, `graphics.minObjectScreenPercent: 5` écrit `0.05`).
- **string** est écrite telle quelle.

| Clé de paramètre | Jeton `options.cfg` | Type | Plage / valeurs | Preuves |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | toute valeur | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | toute valeur | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (présence) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (présence) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (présence) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (présence) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (présence) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (présence) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | toute valeur | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, stocké /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, stocké /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (présence) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (présence) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, écrit sous la forme `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` ou `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (bloc de 4 lignes) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (présence) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (présence inversée) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (deux jetons de présence) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (présence) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (présence) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (présence) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (présence) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` composante 0 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 à l'exécution) |
| `traffic.humans` | `AIMaxCountRandom` composante 1 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 à l'exécution) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Entrées du catalogue qui existent mais ne sont **pas accessibles en écriture** (rejetées avec `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`) : `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (remplacées par `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Confinement des chemins

`presentation.splash.assets` et `internet-textures.profile` sont résolus par `Confined(package root, value)` :

1. Les chemins enracinés (`C:\...`), les chemins commençant par `\` et tout composant de chemin égal à `..` sont rejetés.
2. Le chemin complet est calculé et doit commencer par le répertoire du paquet.
3. Chaque composant existant sous la racine du paquet, jusqu'au chemin final inclus, est inspecté à la recherche de l'attribut `ReparsePoint`. Une jonction, un lien symbolique de répertoire ou un lien symbolique de fichier situé n'importe où sur ce chemin est rejeté, de même qu'un composant qui ne peut pas être inspecté (`IOException` / `UnauthorizedAccessException`).

Ces trois échecs donnent tous `OL_E_SESSION_PROFILE_PATH_ESCAPE`. La même règle relative aux points d'analyse est appliquée aux cibles `.itx` sous `Texture\` lors de la construction de la session.

<a id="precedence-and-override-conflicts"></a>
## Priorité et conflits de remplacement

`CliInput.BuildSpecAsync` compose la spécification dans cet ordre :

1. **Valeurs par défaut** (NEW_MAP, tout non défini, timeouts 180 s / 30 s).
2. **`/spec:<file.json>`**, s'il est fourni, remplace entièrement les valeurs par défaut.
3. **Racine de l'installation** : un argument d'installation explicite l'emporte sur le `RootPath` de la spécification ; `.` désigne le répertoire contenant l'exécutable.
4. **Profil** (`/predefined-profile` + `/predefined-profile-index`) : le paquet est chargé et `RejectProfileConflicts` s'exécute sur les arguments bruts de la CLI **avant** toute fusion. Le bloc de monde de la base est ensuite réinitialisé en un `WorldSpec` vide du mode sélectionné (un monde issu de `/spec` est abandonné lorsqu'un profil est utilisé) et `SessionProfileCompiler.Apply` superpose le profil à la base : `new` (NEW_MAP uniquement), `settings` (fusionnés par-dessus le `Environment.General` de la base, le profil l'emportant clé par clé), ainsi que `presentation`, `internet-textures`, `behavior` (chacun ne remplaçant le bloc de la base que lorsque le préréglage le définit).
5. **Arguments restants de la CLI**, superposés par-dessus : `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, options météo, options de véhicule, `/set`, options d'écran de démarrage, options de textures Internet, `/startup-timeout`, `/shutdown-timeout`. Les timeouts de la CLI ne s'appliquent que lorsqu'ils sont fournis ; sinon, la valeur de la spécification, du profil ou par défaut est conservée.
6. **Vérification de compatibilité** pour les modes autres que NEW_MAP (`ValidateCompatibility`).

Un argument de la CLI qui cible un champ détenu par le profil sélectionné constitue un conflit, rejeté avec `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (code de sortie 2, catégorie `invalid_argument`). La vérification porte sur le champ, pas sur la valeur : répéter la valeur du profil lui-même reste un conflit.

| Argument de la CLI | En conflit lorsque le profil définit | Uniquement dans le mode |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` ou `/entrypoint-index` | `new.entrypoint` ou `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | la même `<key>` dans les `settings` du préréglage (insensible à la casse) | tous |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (quelconque) | tous |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (quelconque) | tous |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (quelconque) | tous |
| `/module:<id>` | `modules.<id>.enabled: false` | tous |
| `/no-module:<id>` | `modules.<id>.enabled: true` | tous |
| `/module-setting:<id>.<key>=...` ou une option de module | `modules.<id>.settings.<key>` | tous |

Ne constituent pas des conflits : les clés `/set` que le préréglage ne définit pas (elles sont ajoutées), les options de véhicule (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle` ; un profil ne peut pas définir de véhicule du joueur), et tout argument de monde sous `/saved` (le bloc `new` n'y est pas appliqué). `/map`, `/entrypoint` et `/entrypoint-index` sont invalides avec `/saved`, indépendamment des profils (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Codes d'erreur

| Code | Levé lorsque | Sortie CLI |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` n'existe pas | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` n'est pas un simple nom de répertoire ; `assets` / `profile` sort du paquet ou traverse un point d'analyse | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` ne vaut pas `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | limite de taille, forme du document, ancres, clé inconnue, clé obligatoire manquante, valeur non scalaire, nombre/date/heure incorrect, `id` non concordant, règles de nombre/d'index des préréglages, mots de mode non pris en charge, timeout non positif, `presentation` sans `splash`, `override` sans `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` manquant, hors de 1..99, ou aucun préréglage avec cet `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | une clé de `settings` n'est pas dans le catalogue | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | une clé de `settings` est cataloguée mais en lecture seule | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | le répertoire `assets` ou le fichier `profile` n'existe pas dans le paquet | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` n'est pas vide et la carte effective n'y figure pas (ou ne peut pas être déduite) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | un argument explicite de la CLI cible un champ détenu par le profil | 2 |

Toutes ces erreurs sont levées pendant la compilation de la ligne de commande, avant la planification. Ce sont des `SessionProfileException` (ou `ArgumentException` pour le conflit) et elles ne démarrent jamais de session. Le catalogue complet se trouve dans [erreurs](errors.md) ; les codes de sortie dans [codes de sortie](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Découverte des profils installés

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) compile chaque paquet sous `.omsilaunch\session-profiles\` et affiche une entrée par paquet : `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` et ses `Presets` (`Index`, `Id`, `Name`) dans l'ordre des index. Un paquet qui ne se compile pas est tout de même listé, avec `IsValid: false` et le même code `OL_E_SESSION_PROFILE_*` qu'un lancement signalerait. Rien n'est écrit.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Apparence d'un profil dans l'API

Après un chargement réussi, la spécification transporte un enregistrement `SessionProfileMetadata` dans `LaunchSpec.SessionProfile` :

| Champ | Source |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` du préréglage sélectionné |
| `PresetIndex` | `index` du préréglage sélectionné |
| `PresetName` | `name` du préréglage sélectionné |
| `PackagePath` | répertoire absolu du paquet |

Le planificateur ajoute un diagnostic informatif `session_profile.selected` à chaque `SessionPlan` construit à partir d'une telle spécification, avec les clés de données `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` et `session_profile.path`. Il n'affecte pas l'exécutabilité. Les intégrateurs qui utilisent directement l'[API publique](public-api.md) peuvent appeler `SessionProfileCompiler.Load` et `SessionProfileCompiler.Apply` depuis `OmsiLaunch.Core` ; la représentation YAML ne franchit jamais la frontière de `OmsiLaunch.Api`.

<a id="examples"></a>
## Exemples

<a id="example-1-settings-only-profile-one-preset"></a>
### Exemple 1 : profil limité aux paramètres, un préréglage

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

Exécution : `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. La carte et le point d'entrée proviennent de la ligne de commande, car le profil ne définit pas de bloc `new` ; ajouter `/set:graphics.maxFPS=60` est autorisé, ajouter `/set:traffic.humans=10` est un conflit.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Exemple 2 : profil lié à une carte avec trois préréglages et des ressources empaquetées

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, avec `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` et `textures\offline.itx` dans le paquet :

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

Exécution : `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Avec `/saved:situations\mytrip.osn`, le bloc `new` est ignoré et le `.osn` doit référencer `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### L'exemple livré

La version livre `docs/examples/session-profiles/rmg-leste/profile.yaml` ([afficher](../../../examples/session-profiles/rmg-leste/profile.yaml)). Il est syntaxiquement valide, conforme au schéma et se chargerait sans erreur. Deux propriétés l'empêchent de démarrer une session sans modification dans ce build :

1. Il définit `new.date`, `new.time` et `new.weather`, qui rendent le plan non exécutable (voir [Le bloc `new`](#new)).
2. Ses valeurs de chemin sont des scalaires simples avec des barres obliques inverses doublées (`maps\\RMG Leste\\global.cfg`). YAML les conserve doublées, et les identités de carte sont comparées textuellement (après la seule normalisation de `/` en `\`), si bien que `new.map` et `compatibility.maps` ne correspondraient pas à l'identité du catalogue `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` lors de la planification). La valeur `assets` se résout néanmoins, car la normalisation des chemins Windows fusionne les séparateurs doublés.

La forme exécutable pour ce build est :

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
## Pages associées

- [Référence de la CLI](cli.md) pour `/predefined-profile`, `/predefined-profile-index`, `/set` et les options de monde
- [LaunchSpec](launchspec.md) pour l'enregistrement dans lequel un profil est compilé
- [Transactions et récupération](../concepts/transactions-and-recovery.md) pour la façon dont les overlays de `settings`, d'écran de démarrage et `.itx` sont appliqués et restaurés
- [Capacités](capabilities.md) et [état de la validation à l'exécution](../status/runtime-validation-status.md)
