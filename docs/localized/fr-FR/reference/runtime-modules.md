# Modules runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> Traduction de la [page originale en anglais](../../../reference/runtime-modules.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Un module runtime ajoute un comportement à une session OMSI gérée : une fonctionnalité qui s'exécute avec la session (par exemple le HUD d'une carte, ou des optimisations de chargement), sans devenir une partie d'OmsiLaunch lui-même. Le pont le charge, ne lui donne que ce qu'il déclare, et supprime tout ce qu'il détient lorsque la session se termine ou que le module échoue. Cette page est la référence normative des modules runtime tels qu'implémentés par `src/OmsiLaunch.Runtime.Modules` (manifeste, paramètres, éligibilité, hôte de cycle de vie), le planificateur, les programmes de session du contrôleur (`ModuleSessionPrograms`, `ModuleSessionProcess`), l'environnement de modules du plugin et les options de module de la [CLI](cli.md).

Stabilité : `EXPERIMENTAL`. Le protocole de pont 1 a été validé à l'exécution lors de la série B (modules gérés, activation par contenu, opérations, `disable-module` et `fail-session` ; composants natifs avec hooks IAT ; voir l'[état de la validation à l'exécution](../status/runtime-validation-status.md)). Le protocole de pont 2 (paramètres, activation par défaut, fichier de paramètres, programmes de session, conflits de plugins, notifications, `get_value`) est validé hors ligne uniquement. Contrôles bloquants hors ligne : les contrôles `module.*` de `tests/OmsiLaunch.ModuleTests` (manifeste, intégrité, sélection et activation, paramètres, fichier de paramètres, conflits de plugins, programmes de session démarrés via leur job, un échec injecté dans chaque phase sous chaque politique d'échec, cycle de vie du composant natif, nettoyage, handoff), `plugin-runtime.modules-lifecycle` (un vrai module géré chargé par l'environnement du plugin), `runtime-modules.native-component` (un vrai composant natif qui hooke un import et lit des valeurs via le pont), `runtime-modules.plugin-conflict` et `native-hooks.self-check` (le moteur de hooks). Le protocole de pont 3 (Beta 5 : opérations du catalogue dans le processus, le tick, la capture d'entrée, les entrées de contenu communautaire, les épingles facultatives avec l'instantané pris au moment du plan, `module pack`, les options des jobs auxiliaires, la version d'OMSI) est validé hors ligne uniquement : les contrôles `module.*` de `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` et `ui-input.hook-self-check` des tests d'intégration du plugin, et `omsi-version.literal` des tests unitaires. La prise en main du menu pause et la capture d'entrée n'ont pas été observées dans une session OMSI réelle.

Le chargement est ouvert : tout module installé sous `plugins\OmsiLaunch.Modules` peut être chargé, quel que soit son auteur. OmsiLaunch ne se porte pas garant de ce qu'un module fait dans OMSI. Ce qu'il garantit, c'est la [frontière](#security-boundary) : les modules ne s'exécutent jamais dans le processus OmsiLaunch, et rien de ce qu'OmsiLaunch propose ne permet à un module d'aller au-delà des droits propres d'OMSI.

<a id="where-modules-live"></a>
## Emplacement des modules

Chaque module est un répertoire sous le dossier du plugin permanent :

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

Le nom du répertoire doit être égal à l'`id` du module. Les modules sont installés par celui qui les distribue ; une session n'y écrit, n'y sauvegarde et n'y restaure jamais de fichiers, et les modules ne modifient jamais les fichiers de l'installation via OmsiLaunch (un manifeste n'a aucun moyen de déclarer un overlay d'installation). Les données inscriptibles d'un module se trouvent dans son [répertoire de données](#data-directory).

<a id="manifest-modulejson"></a>
## Manifeste (`module.json`)

Schéma `omsilaunch.runtime-module/v1`. Les clés inconnues sont rejetées : un manifeste écrit pour un schéma ultérieur est donc refusé au lieu d'être compris à moitié. Tous les chemins sont relatifs au répertoire du module (les chemins de contenu liés sont relatifs à la racine OMSI), utilisent `\` ou `/`, et ne peuvent contenir ni `..` ni lettre de lecteur.

| Clé | Obligatoire | Signification |
| --- | --- | --- |
| `schema` | oui | `omsilaunch.runtime-module/v1`. |
| `id` | oui | Identifiant du module : lettres minuscules, chiffres et `-`, de 2 à 48 caractères, commençant par une lettre ou un chiffre. C'est aussi le nom de son répertoire et son espace de noms d'opérations. |
| `version` | oui | `major.minor.patch`, avec un `-suffix` facultatif. |
| `name` | non | Nom d'affichage (128 caractères au plus). |
| `bridge_protocol` | oui | `{ "min": n, "max": n }` : les protocoles de pont avec lesquels le module fonctionne. Cette build sert les protocoles `1`, `2` et `3` ; un module est accepté lorsque sa plage inclut l'un d'eux. |
| `activation` | non | `requested` (par défaut) : chargé lorsque la session le demande. `content` : également chargé lorsque la session exécute la carte liée (nécessite `content_binding`). `default` (protocole 2) : chargé dans toutes les sessions sauf s'il est désactivé (voir [choix des modules](#choosing-modules-for-a-session)) ; il ne peut pas avoir de `content_binding`. |
| `required_capabilities` | non | Capacités sans lesquelles le module ne peut pas fonctionner (voir [capacités](#capabilities)). |
| `optional_capabilities` | non | Capacités que le module utilise lorsqu'elles sont disponibles ; il les vérifie avec `IsCapabilityAvailable`. |
| `content_binding` | non | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }` : le module ne s'exécute que sur cette carte, avec ces fichiers inchangés. |
| `assets` | non | Fichiers lus par le module, chacun sous la forme `{ "path", "sha256" }`. À partir du protocole 3, le `sha256` est facultatif (voir [intégrité](#integrity)). |
| `components` | oui | Exactement un composant `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }` ; éventuellement un composant `native` (`.dll`), des composants `auxiliary` (`.exe`) et, avec le protocole 2, un composant `host` (`.exe`, le [programme de session](#session-programs) du module). À partir du protocole 3, le `sha256` d'un composant est facultatif, et un composant `auxiliary` peut porter `priority` (`normal`, `below-normal` ou `idle`) et `memory_limit_mb` (64 à 65536) : le job object du programme les applique dès sa première instruction (voir [programmes auxiliaires](#auxiliary-programs)). |
| `failure_policy` | oui | `fail-session`, `disable-module` ou `degrade` (voir [échecs](#failures)). |
| `operations` | non | Opérations que le module expose pendant l'exécution de la session : `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | non | Protocole 2. Les paramètres que le module accepte (voir [paramètres](#settings)). |
| `conflicts` | non | Protocole 2. `{ "plugins": [ "<file>.dll" ] }` : DLL de plugins OMSI qui ne doivent pas s'exécuter à côté du module (voir [conflits](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | non | Protocole 3. Opérations du catalogue de capacités publiques que le module exécute dans le processus, par exemple `["time.read", "camera.set"]` (voir [opérations dans le processus](#operations-in-process)). Nécessite la capacité `bridge.runtime`. |
| `tick_ms` | non | Protocole 3. Le module veut un rappel périodique toutes les `tick_ms` millisecondes : un multiple de 50, de 50 à 1000 (voir [tick](#tick)). Nécessite la capacité `bridge.tick`. |
| `content_inputs` | non | Protocole 3. Contenu communautaire que le module lit, localisé par le pont : `[ { "kind": "realmap-package", "required": false } ]` (voir [contenu communautaire](#community-content)). |
| `timeouts` | non | Budgets en millisecondes : `arm_ms`, `gameplay_ready_ms`, `stop_ms` (5000 chacun par défaut, 30000 au plus) ; avec le protocole 2 également `prepare_ms` (120000 par défaut, 1800000 au plus) et `host_stop_ms` (10000 par défaut, 60000 au plus) pour le programme de session. Les opérations ont 2000 ms par défaut. |
| `diagnostics` | non | `{ "trace": true\|false }`. |

Un manifeste qui utilise une fonctionnalité du protocole 2 (`settings`, `conflicts`, activation `default`, un composant `host`, `prepare_ms` ou `host_stop_ms`) doit déclarer `bridge_protocol.min` à 2. Un manifeste qui utilise une fonctionnalité du protocole 3 (`runtime_operations`, `tick_ms`, `content_inputs`, une capacité `bridge.runtime`, `bridge.tick` ou `bridge.input`, un `priority` ou `memory_limit_mb` auxiliaire, ou un composant ou asset sans `sha256`) doit déclarer `bridge_protocol.min` à 3.

Chaque fichier nommé par `components` et `assets` peut être épinglé par son SHA-256 (obligatoire avant le protocole 3, facultatif à partir de celui-ci : voir [intégrité](#integrity)). `module.json` ne peut pas s'épingler lui-même.

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
## Intégrité

Chaque fichier que le manifeste nomme doit exister et ne doit pas être atteint via une jonction ou un lien symbolique, et chaque fichier que l'auteur a épinglé doit avoir son SHA-256 épinglé ; sinon le module est refusé avec `OL_E_MODULE_INTEGRITY`. Cela détecte un module corrompu ou mis à jour à moitié. Ce n'est pas une décision de confiance : un module n'est vérifié par rapport à aucune liste, signature ou éditeur.

À partir du protocole 3, une épingle est facultative, de sorte qu'un auteur n'a jamais à maintenir des hashes à la main, et le plan prend toujours un **instantané** : un SHA-256 unique calculé sur chaque fichier que le manifeste nomme et sur son contenu, consigné dans le plan (`PlannedModule.SnapshotSha256`) et transporté jusqu'au plugin dans le handoff de démarrage (version de protocole filaire 8). L'instantané garantit que les fichiers évalués par le plan sont ceux qui s'exécutent : le plugin ne charge un module que lorsque son `module.json` a le SHA-256 évalué par le plan et que ses fichiers ont encore le contenu de l'instantané, et le contrôleur et le pont comparent de nouveau l'instantané juste avant le démarrage d'un programme de session, d'un composant natif ou d'un programme auxiliaire. Un module dont les fichiers ont changé après la planification n'est pas chargé (`OL_E_MODULE_INTEGRITY`, « the module's files changed after planning »). L'instantané couvre chaque module, épinglé ou non ; une épingle permet en outre à qui publie un module de détecter une copie corrompue ou mise à jour à moitié dès qu'elle est découverte.

La vérification est répétée là où le code du module démarre : le plugin ne charge un module que si son `module.json` a toujours le SHA-256 évalué par le plan et que chaque épingle et l'instantané tiennent toujours, et le contrôleur ne démarre un programme de session qu'après avoir vérifié l'épingle de ce programme et l'instantané.

<a id="packing-a-module"></a>
### Empaqueter un module

`OmsiLaunch.exe /pack-module:<module directory>` écrit les épingles des fichiers que nomme un `module.json` (ses composants et assets), de sorte qu'un auteur ne modifie jamais un SHA-256 à la main. Il ne réécrit que les valeurs `sha256` (le reste du manifeste est conservé ; le fichier est réindenté), relit le résultat avec l'analyseur de manifeste avant d'écrire quoi que ce soit, et rapporte chaque fichier (enveloppe `module.pack` : `module`, `directory`, `written`, `intact`, `files` avec `path`, `sha256`, `pinned`, `mismatch`, `written`). Il travaille sur le répertoire qui lui est donné et ne touche jamais une installation. `/verify-module:<module directory>` rapporte la même chose sans écrire (`module.verify`) ; il sort avec `7` lorsqu'un fichier épinglé ne correspond pas à son épingle (un fichier sans épingle n'est pas une divergence). Les deux sortent avec `2` pour un manifeste qui ne s'analyse pas ou un fichier manquant.

<a id="security-boundary"></a>
## Frontière de sécurité

OmsiLaunch ne contrôle pas ce qu'un module fait dans OMSI : ses composants gérés et natifs s'exécutent dans le processus OMSI avec les mêmes droits que n'importe quel plugin OMSI. OmsiLaunch garantit ce qui suit :

1. Le processus OmsiLaunch ne charge jamais de code de module. Le code du module s'exécute dans OMSI, dans un programme auxiliaire démarré depuis OMSI, ou dans le programme de session du module.
2. Les programmes qu'OmsiLaunch démarre pour les modules sont des fichiers du propre répertoire du module, vérifiés par rapport à leur épingle, démarrés avec une ligne de commande qu'OmsiLaunch fixe. Un programme de session s'exécute dans un objet job qui le termine avec la session, n'autorise aucun second processus (il ne peut pas démarrer d'autres programmes) et refuse les modifications des paramètres système, des paramètres d'affichage, du bureau, ainsi que la fermeture de session ou l'arrêt de l'ordinateur. Il n'hérite que de ses deux handles de pipe.
3. Tout ce qu'un module déclare reste dans son répertoire, et ses données inscriptibles dans `.omsilaunch\module-data\<id>`.
4. Les canaux d'OmsiLaunch (contrôle local, handoff de démarrage, télémétrie, commandes runtime) restent limités à l'utilisateur courant et liés à la session ; aucun ne permet à un appelant de démarrer un programme, de lire ou d'écrire des fichiers hors de l'installation, ni de modifier les paramètres système.
5. OmsiLaunch n'élève jamais les privilèges. Un module s'exécute avec les droits de l'utilisateur qui a démarré la session.
6. Le texte provenant d'un module (notifications, événements, valeurs) est traité comme des données : borné, sans caractères de contrôle, jamais exécuté, ouvert ni suivi comme un lien.

<a id="capabilities"></a>
## Capacités

`required_capabilities` et `optional_capabilities` nomment soit une capacité native de la build en cours (la colonne des capacités natives du [catalogue des capacités](capabilities.md#capability-catalog), par exemple `time.read` ou `d3d.texture`), soit une fonctionnalité du pont :

| Fonctionnalité du pont | Ce que le module obtient |
| --- | --- |
| `bridge.events` | `Emit` : événements runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground` : threads d'arrière-plan suivis. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess` : processus auxiliaires liés à la session. |
| `bridge.hooks.iat` | Composant natif : hooks sur les imports de `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Composant natif : hooks sur les méthodes du périphérique Direct3D 9 actif (`d3d9:device.<method>`). Nécessite la capacité native `d3d.texture`. |
| `bridge.runtime` | Protocole 3. `IRuntimeModuleContext.Runtime` et le `run_operation` natif : les opérations du catalogue que le manifeste liste dans `runtime_operations`. |
| `bridge.tick` | Protocole 3. Un rappel périodique : `ITickingModule.Tick` et l'export natif `omsilaunch_module_native_v1_tick`. |
| `bridge.input` | Protocole 3. `IRuntimeModuleContext.Input` : capture des messages clavier et souris du jeu pour le menu propre au module. Nécessite la capacité native `ui.input.capture`. |

Un nom qui n'est ni l'un ni l'autre est refusé à la planification (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). Sur une build d'OMSI 2 sans disposition connue, une capacité native que le fichier exécutable ne résout pas est décidée dans le processus ; si elle n'y est pas résolue, le module n'est pas chargé.

<a id="choosing-modules-for-a-session"></a>
## Choix des modules pour une session

Un module rejoint une session de quatre façons ; `PlannedModule.Activation` indique laquelle :

| Activation | Quand |
| --- | --- |
| `requested` | La session le demande : `LaunchSpec.Modules.Enable`, `/module:<id>`, ou `enabled: true` dans le préréglage de profil de session sélectionné. |
| `settings-file` | `enabled=true` dans sa section du [fichier de paramètres](#the-settings-file). |
| `content` | Son activation est `content` et la session exécute sa carte liée. |
| `default` | Son activation est `default`. |

Un module est écarté de la session par `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` dans le préréglage, ou `enabled=false` dans le fichier de paramètres (diagnostic de plan `module.disabled`). Une demande explicite l'emporte sur le fichier de paramètres. Activer et désactiver le même module dans une seule demande donne `OL_E_MODULE_SETTING_INVALID`.

Règles de planification : un module demandé qui ne peut pas être chargé rend le plan non exécutable avec son code `OL_E_MODULE_*`. Un module qui a rejoint la session de lui-même (fichier de paramètres, contenu, défaut) et ne peut pas être chargé n'arrête pas la session : le plan signale `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Paramètres

Un module déclare ses paramètres dans `settings` ; chaque entrée :

| Clé | Obligatoire | Signification |
| --- | --- | --- |
| `key` | oui | `[a-z][a-z0-9_]{0,31}`, différent de `enabled`. |
| `type` | oui | `boolean`, `integer`, `choice` ou `text`. |
| `default` | oui | La valeur lorsque rien d'autre ne la définit : un booléen, un entier, une chaîne, ou `"auto"` lorsque `auto` vaut true. |
| `auto` | non | `true` : la valeur `auto` est acceptée et transmise au module, qui décide à l'exécution. |
| `min`, `max` | non | Bornes entières. |
| `choices` | pour `choice` | Les valeurs acceptées (`[a-z0-9][a-z0-9_.-]{0,31}`, différentes de `auto`). |
| `max_length` | non | Limite de longueur du texte (1..4096, 256 par défaut). Les caractères de contrôle sont refusés. |
| `flag` | non | Une option de ligne de commande pour ce paramètre, `[a-z][a-z0-9-]{2,31}` : `/<flag>:<value>`, ou `/<flag>` seul pour `true` sur un booléen. |
| `description` | non | 256 caractères au plus. |

Les valeurs sont normalisées : les booléens acceptent `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0` ; les choix sont comparés sans tenir compte de la casse ; les entiers sont vérifiés par rapport à leurs bornes.

La valeur effective de chaque paramètre est, dans l'ordre :

1. la demande : `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, l'option propre à un module, ou `modules.<id>.settings` dans le préréglage de profil de session sélectionné. Comme pour tous les autres champs de préréglage, la ligne de commande ne peut pas non plus définir une clé que le préréglage définit (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`) ;
2. le [fichier de paramètres](#the-settings-file) ;
3. la valeur par défaut du manifeste.

Une valeur demandée invalide, ou une clé demandée que le module ne déclare pas, rend le plan non exécutable (`OL_E_MODULE_SETTING_INVALID`). Une entrée du fichier de paramètres inutilisable est ignorée avec `OL_W_MODULE_SETTING_IGNORED`, tout comme les paramètres demandés pour un module que la session ne charge pas. Le plan liste chaque valeur effective avec sa source (`request`, `settings-file` ou `default`) dans `PlannedModule.Settings`.

Le module reçoit chaque clé déclarée avec sa valeur effective : `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` dans un composant natif, et l'objet `settings` du contexte d'un programme de session.

<a id="the-settings-file"></a>
### Le fichier de paramètres

`<OMSI root>\.omsilaunch\modules.ini`, à côté des profils de session. OmsiLaunch ne fait que le lire.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Les sections sont des identifiants de module ; les lignes `key=value` définissent des paramètres, `enabled` active ou désactive le module pour toutes les sessions ; les lignes commençant par `;` ou `#` sont des commentaires. Le fichier peut faire 256 KiB au plus. Tout ce qu'il ne peut pas utiliser (une section qui n'est pas un identifiant de module, une ligne hors section, une clé inconnue, une valeur invalide) est signalé par `OL_W_MODULE_SETTING_IGNORED` et ignoré.

<a id="session-programs"></a>
## Programmes de session

Un module peut déclarer un composant `host` : son programme de session, un exécutable de son propre répertoire. Lorsque le module figure dans le plan, le contrôleur :

1. vérifie l'épingle du programme et crée le répertoire de données du module ;
2. le démarre avant OMSI, depuis le répertoire du module, avec la ligne de commande `"<program>" --omsilaunch-session <session id>`, suspendu jusqu'à ce qu'il soit dans son job (voir la [frontière de sécurité](#security-boundary)) ;
3. écrit le contexte comme première ligne sur son entrée standard ;
4. attend que le programme écrive `{"type":"ready"}`, au plus `prepare_ms` ; ce n'est qu'alors qu'il démarre OMSI ;
5. après la création d'OMSI, écrit `{"type":"omsi-started","pid":<process id>}` ;
6. à la fin de la session, après la sortie d'OMSI et avant toute restauration de fichier, écrit `{"type":"stop"}`, attend `host_stop_ms` au plus, puis termine le programme.

Les deux directions utilisent du JSON UTF-8, un objet par ligne. Le contexte :

| Champ | Signification |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identité. |
| `installation_root`, `module_directory`, `data_directory` | Chemins complets. |
| `channel` | Le canal nommé (named pipe) que le programme peut servir pour les composants du module dans OMSI : `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. Le programme le crée, restreint à l'utilisateur courant, et vérifie que le client est le processus d'OMSI. Les composants le lisent avec `get_value("session_channel")` ou `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | Les paramètres effectifs du module. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` de la session. |
| `session_files` | Fichiers relatifs à l'installation que possède la transaction de session. Le programme ne doit pas les garder ouverts ni mappés : ils sont restaurés à la fin de la session. |

Ce que le programme peut écrire en plus de `ready` :

| Ligne | Effet |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Événement runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Événement runtime `module.<id>.notice` (voir [notifications](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Diagnostic de session avec ce code catalogué (`OL_E_MODULE_OPERATION_FAILED` sinon). |

Les autres lignes, les lignes de plus de 64 KiB et tout ce qui suit 10 000 lignes sont ignorés. Le contrôleur enregistre `module.<id>.host.started`, `host.ready` (avec `prepare_ms`), `host.failed`, `host.exited` et `host.killed`.

Un programme qui n'est pas prêt dans `prepare_ms`, qui se termine avant d'être prêt, ou dont l'épingle ne tient pas, fait échouer la préparation de son module : sous `fail-session`, la session ne démarre pas (`OL_E_MODULE_SESSION_FAILED`) ; sous `disable-module`, le programme est terminé et le module est laissé hors d'OMSI ; sous `degrade`, le module reste (et le programme continue de s'exécuter s'il le peut encore). Le `StartupTimeoutSeconds` de la session compte à partir du moment où OMSI est démarré, la préparation ne le consomme donc pas.

<a id="conflicts-with-omsi-plugins"></a>
## Conflits avec les plugins OMSI

`conflicts.plugins` nomme des DLL de plugins OMSI qui ne doivent pas s'exécuter à côté du module, par exemple un ancien plugin qui installe les mêmes hooks. À la planification, OmsiLaunch lit la section `[dll]` de chaque `plugins\*.opl` ; si l'un charge une DLL en conflit, le module est refusé avec `OL_E_MODULE_CONFLICT` et la raison nomme le `.opl`. Dans OMSI, un module dont la DLL en conflit est tout de même chargée n'est pas chargé. OmsiLaunch ne modifie jamais les fichiers d'un autre plugin ; le désactiver relève de la décision de l'utilisateur.

<a id="notices"></a>
## Notifications

Un événement de module nommé `notice` (issu de `Emit("notice", ...)`, d'un `emit` de composant natif, ou d'une ligne notice d'un programme de session) est affiché par l'icône de notification d'OmsiLaunch sous forme de bulle dont le titre nomme le module. Seuls `title` (63 caractères au plus) et `text` (255 caractères au plus) sont conservés, les caractères de contrôle sont supprimés, et rien de ce qu'il contient n'est ouvert ni exécuté. Un programme de session qui doit informer l'utilisateur avant le démarrage d'OMSI (lorsque l'icône de notification n'est pas encore affichée) utilise sa propre fenêtre.

<a id="data-directory"></a>
## Répertoire de données

`<OMSI root>\.omsilaunch\module-data\<id>\` est le répertoire inscriptible du module pour cette installation : `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")`, et le `data_directory` du programme de session. Il est créé à la première utilisation. Il ne fait pas partie de la transaction de session : ce qu'un module y écrit subsiste après la session. `/clear-module-data:<id>` le supprime (et rien d'autre) ; la commande est refusée tant qu'une session possède l'installation, et un répertoire atteint via un lien est supprimé en tant que lien, jamais suivi.

<a id="lifecycle"></a>
## Cycle de vie

| Étape | Où | Ce qui se passe |
| --- | --- | --- |
| 1. Découverte | contrôleur | `plugins\OmsiLaunch.Modules\*` est lu : manifeste et intégrité (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Éligibilité | contrôleur, à la planification | Les modules qui [rejoignent la session](#choosing-modules-for-a-session) sont évalués : utilisable, protocole de pont, capacités, liaison de contenu, conflits de plugins, paramètres. Le résultat est `SessionPlan.PlannedModules`. |
| 3. Préparation | contrôleur, au démarrage | Les [programmes de session](#session-programs) des modules sont démarrés et attendus. Ensuite les modules éligibles sont transmis au plugin dans le handoff de démarrage (version 6) : leurs répertoires, le SHA-256 de chaque `module.json` évalué, et leurs paramètres effectifs. |
| 4. Armement | plugin, avant le démarrage du monde | Chaque module est revérifié et chargé (composant géré dans son propre contexte de chargement). Son composant natif, s'il y en a un, est chargé et démarré en premier, puis `Arm(context)` est appelé. |
| 5. Gameplay prêt | plugin | Après `gameplay.entered`, lorsqu'un module chargé déclare `bridge.hooks.d3d9`, le pont commence à observer le périphérique Direct3D (`d3d.ready`) ; il appelle ensuite le `gameplay_ready` du composant natif et `GameplayReady()`. Pour une session NEW_MAP, la carte liée est revérifiée par rapport à la carte que le démarrage a chargée. |
| 6. Runtime | plugin | Les opérations `module.<id>.<operation>` atteignent `Execute`. Un module avec `tick_ms` reçoit des ticks (voir [tick](#tick)) ; un module avec `runtime_operations` les exécute dans le processus (voir [opérations dans le processus](#operations-in-process)). |
| 7. Arrêt | plugin, puis contrôleur | Lorsque la session se termine (ou que le module échoue), `Stop()` est appelé. Après la sortie d'OMSI, les programmes de session sont arrêtés. |
| 8. Nettoyage | plugin | Tout ce que le module a suivi est libéré dans l'ordre inverse et vérifié comme disparu ; le composant natif est arrêté en dernier et ses hooks sont retirés ; sa DLL reste chargée, inerte, jusqu'à la sortie d'OMSI (un thread peut encore être en train de revenir via un detour). |

Chaque callback en processus s'exécute sur le thread d'interface d'OMSI, comme les propres opérations runtime du produit. Le pont mesure chaque callback par rapport à son budget : un callback qui retourne en retard fait échouer sa phase (`OL_E_MODULE_PHASE_TIMEOUT`). Le pont ne peut pas interrompre un callback qui ne retourne jamais ; un tel module bloque OMSI, et la session se termine au timeout de démarrage ou par `session stop` comme n'importe quelle autre session.

<a id="failures"></a>
## Échecs

Un échec est une exception ou un dépassement dans une phase ou une opération quelconque, une capacité non résolue dans le processus, une liaison de contenu qui ne tient pas dans le processus, un plugin en conflit chargé dans OMSI, ou un programme de session qui ne devient pas prêt. La `failure_policy` du module décide de la suite. Dans tous les cas, les ressources du module sont libérées. Un tick qui lève une exception, renvoie une valeur non nulle ou dépasse son intervalle est un échec, comme celui d'une opération.

| Politique | Échec en préparation, chargement, armement ou gameplay prêt | Échec dans une opération | Échec à l'arrêt |
| --- | --- | --- | --- |
| `fail-session` | Module libéré ; `module.session-failure` ; la session échoue avec `OL_E_MODULE_SESSION_FAILED`, OMSI est arrêté et les fichiers sont restaurés. Un échec avant le démarrage du monde empêche le démarrage. | Identique. | Signalé (`module.failed`) ; la session continue jusqu'à sa fin. |
| `disable-module` | Module libéré et désactivé ; la session continue. | Identique. | Signalé ; module libéré. |
| `degrade` | Comme `disable-module`, sauf que l'échec d'un programme de session laisse le module dans la session. | L'appel en échec renvoie son code ; le module reste actif ; `module.degraded` est signalé une fois par code. | Signalé ; module libéré. |

Un module désactivé ou en échec répond à ses opérations par `OL_E_MODULE_DISABLED`. Les échecs de module apparaissent comme diagnostics de session (`SessionStatus.Diagnostics`) avec leur code `OL_E_MODULE_*` et comme événements runtime.

<a id="cleanup"></a>
## Nettoyage

Ce qu'un module acquiert via son contexte est suivi : les ressources enregistrées avec `Track`, les threads démarrés avec `RunBackground`, les processus démarrés avec `StartAuxiliaryProcess`. À l'arrêt ou en cas d'échec, le pont annule `Stopping`, puis les libère dans l'ordre inverse. Les processus auxiliaires sont aussi placés dans un objet job à terminaison à la fermeture, de sorte qu'ils se terminent avec OMSI même si OMSI est tué ; le job d'un programme de session le termine avec la session OmsiLaunch, même si OmsiLaunch lui-même est tué. Une ressource encore vivante après la libération (un processus qui ne s'est pas terminé, un thread qui ne s'est pas arrêté) est signalée par `OL_E_MODULE_CLEANUP_INCOMPLETE`. Les threads ou processus qu'un module crée sans passer par son contexte ne sont pas suivis.

Après un plantage, il n'y a rien à récupérer pour les modules : ils ne détiennent aucun fichier de l'installation, et leur mémoire, leurs threads et leurs processus liés au job se terminent avec le processus OMSI ou le processus OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Composants natifs et hooks

Un composant natif est une DLL 32 bits implémentant l'interface C de `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h` :

| Export | Appelé |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | À l'armement, avant le `Arm` géré. Une valeur non nulle fait échouer la phase. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Facultatif ; après l'entrée en gameplay. |
| `void omsilaunch_module_native_v1_stop(void)` | À l'arrêt ou en cas d'échec, avant le retrait de ses hooks. Ensuite la table du pont refuse tout appel et la DLL reste chargée, inerte, jusqu'à la sortie d'OMSI. |

La table du pont donne au composant `request_hook`, `emit` (événements runtime `module.<id>.<name>` avec un objet JSON de chaînes) et, à partir du protocole 2, `get_value`. Un composant lit le `size` de la table avant d'utiliser `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copie une valeur en UTF-8 avec un zéro terminal et place dans `*length` le nombre d'octets nécessaires sans lui ; elle renvoie `0`, `1` (argument), `2` (nom inconnu), `11` (après l'arrêt) ou `12` (tampon trop petit). À partir du protocole 3, la table contient aussi `run_operation` (voir [opérations dans le processus](#operations-in-process)) et `d3d_is_game_device(context, device)` : si un pointeur de périphérique reçu par un hook Direct3D est le périphérique propre au jeu (`1`), un autre (`0` ; le formulaire de sélection du véhicule crée un second périphérique avec la même table de méthodes, de sorte qu'un detour `Present` est appelé pour les deux et ne devrait dessiner que pour celui du jeu), `10` lorsqu'aucun périphérique n'est encore observé, `11` lorsque `bridge.hooks.d3d9` n'est pas déclaré ou après l'arrêt. Le pointeur est seulement comparé, jamais utilisé, et le pont n'en fournit jamais. Un module qui garde des ressources Direct3D dans le pool par défaut hooke lui-même `d3d9:device.Reset` (ce qu'il fait avant d'appeler l'original est « périphérique perdu », après son retour avec succès c'est « périphérique restauré »). Un composant vérifie le `size` de la table avant d'utiliser un membre du protocole 3 (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Deux exports facultatifs complètent le composant : `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (voir [tick](#tick)) et `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, la façon dont le composant géré appelle le composant natif via `IRuntimeModuleContext.NativeComponent.Execute` (JSON en entrée, JSON en sortie, la même convention de tampon que `run_operation`). Les noms que `get_value` renvoie :

| Nom | Valeur |
| --- | --- |
| `setting.<key>` | La valeur effective d'un paramètre déclaré. |
| `data_directory` | Le [répertoire de données](#data-directory) du module. |
| `module_directory` | Le répertoire propre du module. |
| `session_id` | L'identifiant de la session. |
| `module_version` | La version du manifeste. |
| `product_version` | La version du produit OmsiLaunch. |
| `session_channel` | Le pipe du [programme de session](#session-programs) ; inconnu pour un module qui n'en a pas. |
| `omsi_version` | La version produit d'OMSI que le jeu rapporte sur lui-même (protocole 3 ; voir [la version d'OMSI](#the-omsi-version)). |

Les hooks sont demandés uniquement par cible symbolique, pendant `start` ou `gameplay_ready`, pour une cible dont le manifeste déclare la capacité :

| Cible | Hooks | Capacité |
| --- | --- | --- |
| `iat:<dll>!<function>` | Un import de `Omsi.exe`, par exemple `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` ou `EndScene` du périphérique Direct3D 9 actif (à demander dans `gameplay_ready`, lorsque le périphérique existe). | `bridge.hooks.d3d9` |

`request_hook` renvoie dans `*original` la fonction qui permet de poursuivre la chaîne (un detour doit l'appeler). Son résultat : `0` succès, `1` argument, `2` cible inconnue, `3` le site ne contient pas ce qu'il devrait, `4` le site est hooké par quelqu'un d'extérieur au pont, `5` protection mémoire, `6` registre plein, `7` ordre, `8` mémoire, `9` un thread exécutait les octets à patcher, `10` pas encore de périphérique Direct3D, `11` non autorisé maintenant ou capacité non déclarée.

Chaque hook que le pont installe, pour le produit ou pour un module, est enregistré dans un seul registre avec son propriétaire (`module:<id>` pour un module) :

- Un hook n'est installé que sur la valeur que le site devrait contenir (la fonction importée ; une fonction d'un module nommé `d3d9.dll`, du runtime système ou d'un wrapper), ou empilé sur le hook du sommet du registre sur ce site. Un site déjà modifié par quelqu'un d'autre est refusé (`4`), jamais écrasé.
- Les hooks d'un même site sont retirés en dernier entré, premier sorti ; un hook sur lequel un autre est empilé reste jusqu'à ce que celui-ci soit retiré.
- Le propre hook `Reset` Direct3D du produit figure dans le registre, de sorte que les hooks de périphérique des modules s'empilent dessus.
- Au nettoyage, le pont retire tous les hooks du module après le retour de `stop`. Si l'un ne peut pas être retiré (quelqu'un d'autre a modifié le site), le nettoyage signale `OL_E_MODULE_CLEANUP_INCOMPLETE`. La DLL d'un composant natif n'est jamais déchargée tant qu'OMSI s'exécute, de sorte qu'un detour par lequel on revient encore reste valide.

Le moteur prend aussi en charge les hooks sur les prologues de fonctions avec un trampoline (les autres threads sont suspendus pendant la modification des octets, et les octets attendus exacts sont d'abord vérifiés), mais aucune cible de prologue n'est encore offerte aux modules : de telles cibles proviennent du catalogue de cibles de la build avec des octets vérifiés.

<a id="operations"></a>
## Opérations

Une opération de module est `module.<id>.<operation>`. Elle n'existe que pour une session dont le plan a chargé le module (`PlannedModule.Operations`) ; sinon `ExecuteRuntimeAsync` répond `OL_E_MODULE_NOT_LOADED` ou `OL_E_MODULE_OPERATION_UNKNOWN`. Les arguments obligatoires sont vérifiés, et les arguments non déclarés sont refusés (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Un échec typé d'un module conserve son code lorsqu'il s'agit d'un code public catalogué ; tout autre code devient `OL_E_MODULE_OPERATION_FAILED` avec le code du module dans `Values["module_code"]`. Les clés de résultat qui suivent la convention de nommage interne sont supprimées, comme pour toute opération runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## Programmes auxiliaires

Un module démarre l'un de ses composants `auxiliary` (un `.exe` épinglé de son propre répertoire) avec `StartAuxiliaryProcess`. À partir du protocole 3, le pont crée le programme **suspendu**, le place dans le job object de la session et ne le reprend qu'ensuite, de sorte qu'un enfant qu'il démarre immédiatement ne peut pas s'exécuter hors du job. Le job termine le programme et tout ce qu'il a démarré lorsque le module s'arrête ou qu'OMSI se termine (kill on close) et porte le `priority` (`below-normal`, `idle`) et le `memory_limit_mb` du manifeste dès la première instruction du programme. Contrairement à un [programme de session](#session-programs), le job d'un programme auxiliaire autorise les processus enfants (un programme qui embarque une vue web en a besoin) ; chacun d'eux est dans le job. Avant son démarrage, le programme est de nouveau vérifié : son épingle lorsqu'il en a une, et toujours l'instantané du plan.

<a id="operations-in-process"></a>
## Opérations dans le processus

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (géré) et `run_operation` (natif) exécutent une opération du [catalogue de capacités publiques](capabilities.md#public-runtime-operations) dans le processus, sur le thread d'interface d'OMSI, avec le même registre, les mêmes résultats `OL_E_*` typés, les mêmes vérifications d'arguments et la même politique de mutation que le canal de contrôle runtime, et sans le canal. Seules les opérations que le manifeste liste dans `runtime_operations` sont autorisées : toute autre reçoit la réponse `OL_E_MODULE_OPERATION_NOT_DECLARED` avant d'atteindre OMSI, et les opérations `internal.*` et `module.*` ne peuvent jamais être listées. Une opération dont la capacité native n'est pas résolue par la build répond `OL_E_CAPABILITY_UNRESOLVED`, comme partout ailleurs.

La liste est une information de transparence et de planification, pas une frontière de sécurité : un module s'exécute déjà dans OMSI avec les droits de l'utilisateur. Le plan l'affiche (`PlannedModule.RuntimeOperations`), et une opération qui modifie l'état d'OMSI y porte sa politique de mutation, par exemple `camera.set (omsi-state)`. Un module peut exécuter des opérations à partir de `Arm` (le monde peut ne pas exister encore ; l'opération répond alors sa propre erreur typée). Les résultats sont bornés (1 MiB) ; un résultat plus grand est `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Après l'arrêt du module, chaque appel répond `OL_E_MODULE_DISABLED`.

Le `run_operation(context, operation, arguments_json, buffer, capacity, length)` natif prend l'identifiant du catalogue et un objet JSON de chaînes (ou null) et écrit `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. Il renvoie `0` lorsqu'il a répondu (l'échec propre à l'opération se trouve dans le JSON), `1` pour un argument, `11` après l'arrêt et `12` lorsque le tampon est trop petit (`*length` est la taille nécessaire).

## Tick

Un module qui déclare `tick_ms` et la capacité `bridge.tick` est appelé environ toutes les `tick_ms` millisecondes, sur le thread d'interface d'OMSI, depuis le timer d'interface de 50 ms du plugin : `ITickingModule.Tick(TimeSpan elapsed)` sur le composant géré et l'export natif facultatif `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. L'intervalle est un multiple de 50 entre 50 et 1000. Un tick n'est pas par trame : un module qui dessine hooke la méthode Direct3D `Present` et utilise le tick pour lire des données. Les ticks démarrent après `gameplay.entered` et s'arrêtent avec le module. Un tick qui lève une exception, renvoie une valeur non nulle (natif) ou dure plus longtemps que son intervalle est un échec de ce module : sous `degrade` il est signalé une fois par code (`module.degraded`) et le module continue de s'exécuter (les ticks manqués sont ignorés, jamais mis en file) ; sous `disable-module` et `fail-session` la politique s'applique. Une boucle modale imbriquée d'OMSI maintient le déclenchement du timer d'interface, de sorte qu'un tick ne se réentre jamais lui-même.

<a id="community-content"></a>
## Contenu communautaire

`content_inputs` déclare du contenu que le module lit et que son auteur ne peut pas connaître lors de la construction du module, parce que la communauté le produit, pour une carte, après le module. Il n'y a pas de SHA-256 à épingler ; le pont se contente de le localiser et de vérifier que sa lecture est sûre. Le seul type aujourd'hui est `realmap-package` : le dossier `openomsi-realmap` d'une carte, `<OMSI root>\maps\<folder>\openomsi-realmap`, écrit par l'outillage de paquet de carte openOMSI.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` indique si la carte a un paquet (ne pas en avoir n'est pas une erreur) et, lorsqu'elle en a un, son répertoire, son nombre de fichiers, sa taille totale et un `Fingerprint`. L'empreinte est un détecteur de changement calculé sur le chemin relatif, la taille et la date d'écriture de chaque fichier : utilisez-la comme clé de cache ; ce n'est jamais un contrôle d'intégrité ni une décision de confiance. Le paquet doit se trouver dans l'installation, aucune de ses parties ne peut être un point d'analyse (reparse point), et il est borné (50,000 fichiers, 4 GiB) ; sinon la réponse est `OL_E_MODULE_CONTENT_INPUT_INVALID`. Un module qui n'a pas déclaré le type reçoit `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. Le paquet est une donnée produite par des tiers : lisez-le en code géré, traitez chaque valeur comme non fiable et dégradez par ressource. Une entrée `required: true` fait refuser le module par le plan (`OL_E_MODULE_CONTENT_BINDING`) lorsque la carte de la session n'a pas de paquet.

<a id="input-capture"></a>
## Capture d'entrée

Un module qui remplace le menu pause d'OMSI par le sien a besoin du clavier et de la souris du jeu. OMSI lit ses touches de simulation via DirectInput et sa souris via des messages de fenêtre envoyés à la fenêtre de vue du formulaire principal ; tant que sa propre pause est active (l'octet de pause natif), la simulation, le rendu, les clics 3D et les commandes DirectInput sont retenus par OMSI lui-même. `IRuntimeModuleContext.Input` (capacité `bridge.input`, capacité native `ui.input.capture`) ajoute le reste : `Capture(true)` installe un unique hook `WH_GETMESSAGE` local au thread sur le thread d'interface, et les messages clavier et souris qui appartiennent à l'arbre de fenêtres du jeu (le formulaire principal, ses enfants et le formulaire de pause natif masqué) sont enregistrés comme de simples valeurs `ModuleInputEvent` (type, touche virtuelle, coordonnées client de la fenêtre de vue, bouton, delta de molette, modificateurs) et retirés de la file, de sorte que ni OMSI ni la VCL n'agissent dessus. `Drain()` renvoie les événements (256 au plus sont conservés). Rien n'est sous-classé, aucune fonction d'état des touches de user32 n'est hookée et rien n'est envoyé.

Un seul module capture à la fois. La capture est libérée lorsque le module s'arrête ou échoue, quoi qu'ait fait `Stop`. Les boîtes de dialogue modales natives (enregistrer, options, nouvelle partie) ont besoin d'une vraie entrée : libérez la capture avant d'en demander une. La mécanique du hook est validée hors ligne avec une fenêtre de substitution (`ui-input.hook-self-check`) ; son comportement face à la vraie fenêtre du jeu n'est pas encore observé.

<a id="the-omsi-version"></a>
## La version d'OMSI

`IRuntimeModuleContext.OmsiVersion` et `get_value("omsi_version")` donnent la version produit d'OMSI que le jeu rapporte sur lui-même, par exemple `2.3.004` : le texte qu'OMSI écrit après `Version:` à la ligne 7 de son `logfile.txt`, lu depuis l'image en cours d'exécution. Ce n'est **jamais** la version de fichier PE de l'exécutable (la build de référence porte `FileVersion` `2.2.032`). Lorsque l'image n'a pas un tel texte unique, la version du profil de build est utilisée lorsque l'identité du code est connue, sinon `unknown`. Le hash de l'exécutable, la taille de son code, les adresses et le nom du profil ne sont pas exposés. Utilisez la version pour du texte et pour choisir un comportement ; les capacités (`IsCapabilityAvailable`) restent la source de référence de ce qui fonctionne.

<a id="writing-a-module-contracts"></a>
## Écrire un module (contrats)

Le composant géré référence `OmsiLaunch.Runtime.Modules` et `OmsiLaunch.Api` et implémente `IRuntimeModule`. Le pont lie ces deux références à ses propres assemblies chargés (quel que soit le contexte de chargement dans lequel s'exécute le plugin), de sorte qu'une copie livrée avec le module est ignorée :

| Membre | Appelé |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Étape 4. Acquérir les ressources via le contexte. |
| `void GameplayReady()` | Étape 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Étape 6, pour chaque opération déclarée (sans le préfixe `module.<id>.`). Lever `ModuleOperationException(code, message)` pour un échec typé. |
| `void Stop()` | Étape 7. |

`IRuntimeModuleContext` offre `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (un jeton d'annulation), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess`, et à partir du protocole 3 `Runtime` (voir [opérations dans le processus](#operations-in-process)), `Content` (voir [contenu communautaire](#community-content)), `Input` (voir [capture d'entrée](#input-capture)) et `NativeComponent` (le composant natif chargé, ou null). Un composant géré qui veut des ticks implémente aussi `ITickingModule`. Rien dans le contexte n'expose d'adresses, le hash de l'exécutable ni la disposition de son code. Le composant doit pouvoir être chargé par le plugin 32 bits (AnyCPU ou x86, .NET 6) ; une copie de `OmsiLaunch.Runtime.Modules` ou `OmsiLaunch.Api` à côté de lui est ignorée.

<a id="known-limitations"></a>
## Limitations connues

- Les composants natifs peuvent hooker les imports de `Omsi.exe` et les méthodes du périphérique Direct3D ; les hooks sur les prologues de fonctions OMSI et sur les sites d'appel ne sont pas encore offerts aux modules.
- La prise en main du menu pause (`ui.pause.*`) et la capture d'entrée sont conçues statiquement et validées hors ligne ; elles n'ont pas été observées dans une session OMSI réelle, et `ui.pause-menu.replace` n'est résolue que sur une identité de code connue (certains de ses champs n'ont aucune dérivation indépendante de la build).
- La vérification de liaison de contenu dans le processus utilise la carte que le démarrage a demandée (NEW_MAP) ; pour une situation enregistrée, la liaison est vérifiée à la planification par rapport à la carte de la situation.
- Les hooks de périphérique sont des hooks de vtable. Lorsqu'un autre composant réécrit les méthodes d'origine du périphérique (observé sur l'installation de régression peu après le début du gameplay), un hook de périphérique cesse d'être appelé et ne peut pas être retiré ; le nettoyage signale alors `OL_E_MODULE_CLEANUP_INCOMPLETE` et le composant reste chargé. Une méthode de périphérique déjà hookée par quelque chose d'extérieur au registre est refusée (4) ; l'événement de refus nomme le module qui possède la méthode.
- Les modules s'arrêtent à une sortie normale d'OMSI ; un arrêt de session termine OMSI, de sorte que le nettoyage des modules au sein d'une session n'a lieu que via une politique d'échec.
- Les notifications sont affichées une fois que l'icône de notification existe, après le démarrage d'OMSI ; un programme de session affiche sa propre fenêtre pour tout ce qu'il doit dire avant.
