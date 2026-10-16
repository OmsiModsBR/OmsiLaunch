# Référence de la CLI

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Traduction de la [page originale en anglais](../../../reference/cli.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page est la référence complète et normative de la ligne de commande d'OmsiLaunch `0.1.0-beta4` : les trois exécutables, la grammaire des arguments, l'ordre de dispatch, chaque mot de commande, chaque route hiérarchique, chaque option, les enveloppes de sortie et le comportement en cas d'erreur de chaque commande. Elle est générée à partir de `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), de `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` et des deux shims natifs situés sous `tools\OmsiLaunch.Bootstrapper`. Les résultats de processus sont listés dans [codes de sortie](exit-codes.md) ; les codes d'erreur dans [erreurs](errors.md) ; des invocations commentées dans [exemples de CLI](cli-examples.md).

<a id="executables"></a>
## Exécutables

| Fichier | Sous-système | Rôle | Différences |
|---|---|---|---|
| `OmsiLaunch.exe` | Console | Bootstrapper natif (`OmsiLaunch.Bootstrapper.cpp`) : il résout son propre répertoire, découpe la ligne de commande avec `CommandLineToArgvW`, localise `hostfxr` via `nethost.dll` et exécute `OmsiLaunch.Controller.dll` avec les mêmes arguments. | La sortie console est écrite ; le code de sortie du processus est celui du contrôleur managé, ou un code de shim `100`..`106` si l'hôte .NET n'a pas pu être démarré. |
| `OmsiLaunchW.exe` | Windows (GUI) | Même shim (`OmsiLaunch.WindowsHost.cpp`) compilé pour le sous-système Windows. Il définit la variable d'environnement `OMSILAUNCH_WINDOWS_HOST=1` avant de démarrer le contrôleur. | Pas de console : la sortie console est supprimée sauf si `--json` est indiqué (`WindowsHost.SuppressConsole`), les échecs sont affichés dans des boîtes de message (`WindowsHost.ShowFailure` : message, `Code: OL_E_...` et l'indication `See .omsilaunch\diagnostics for details.`), et un échec du shim `100`..`106` est affiché sous la forme `OmsiLaunch could not start the .NET host (code N).` Comportement complet : [référence d'OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Managé (x64, `net6.0-windows`, Windows Forms) | Le contrôleur lui-même. Il n'est jamais invoqué directement par les utilisateurs ; les deux shims passent le chemin du contrôleur comme premier argument de l'hôte, de sorte qu'il n'apparaît jamais dans la liste publique des arguments. | Nécessite le runtime .NET 6 x64 avec `Microsoft.WindowsDesktop.App` ; voir [installation](../getting-started/installation.md). |

`nethost.dll` doit se trouver à côté des shims. Les shims ne lisent eux-mêmes aucun argument ; chaque argument parvient inchangé à `CliInput.Parse`, de sorte que `OmsiLaunch.exe` et `OmsiLaunchW.exe` acceptent exactement la même syntaxe.

<a id="invocation-model"></a>
## Modèle d'invocation

<a id="argument-grammar-cliinputparse"></a>
### Grammaire des arguments (`CliInput.Parse`)

| Forme | Signification |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Une option. La clé est insensible à la casse ; la valeur est tout ce qui suit le premier `:`. Les clés inconnues échouent avec `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), sortie `2`. |
| `--key=value` | Un argument runtime pour l'opération runtime sélectionnée (par exemple `--handle=rv-000001`). Tout jeton `--` contenant `=` est un argument runtime, jamais une option. |
| `--json`, `/json` | Sortie structurée (voir [Formats de sortie](#output-formats)). `--json` est le seul jeton `--` sans `=` qui ait un sens ; il est analysé comme l'option `/json`. |
| mot nu | Si aucun mot de commande n'a encore été rencontré et que le mot fait partie des [mots de commande](#command-words), il devient la commande. Dès qu'un mot de commande est présent, chaque mot nu suivant est un mot de commande (la route). Sinon, le premier mot nu est la racine de l'installation et tout mot nu ultérieur est ajouté à la route. |

Conséquences : une route hiérarchique (`time get`) ne peut pas être combinée avec un argument d'installation placé après elle (`time get D:\OMSI` est la route inconnue `time get d:\omsi`, sortie `2`). `D:\OMSI time get` est accepté mais relève du **mode propriétaire** (une nouvelle session est démarrée et l'opération s'exécute une fois à l'intérieur). Les erreurs d'analyse (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) et les erreurs de profil de session (`SessionProfileException`) sont signalées avant toute exécution, toujours avec la sortie `2`.

<a id="installation-root"></a>
### Racine de l'installation

- Un argument d'installation explicite sous forme de mot nu l'emporte sur `RootPath` dans un fichier `/spec` (`CliInput.BuildSpecAsync`).
- `.` désigne le répertoire qui contient l'exécutable (`AppContext.BaseDirectory`), jamais le répertoire de travail de l'appelant (`CliInput.ResolveInstallationRoot`). Un paquet portable s'appuie sur ce comportement.
- Lorsque l'argument est omis, les opérations du mode propriétaire (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) utilisent également le répertoire de l'exécutable. Le chemin est normalisé avec `Path.GetFullPath`.
- Les commandes du mode client ne prennent jamais d'argument d'installation : elles s'adressent au point de terminaison de contrôle local de l'installation dans laquelle se trouve l'exécutable (`AppContext.BaseDirectory`). Voir [contrôle local](local-control.md).

<a id="owner-and-client"></a>
### Propriétaire et client

- **Propriétaire** : le processus qui planifie, démarre, supervise et restaure une session (`OwnerSession.RunAsync`). Il détient le bail d'installation (`Local\OmsiLaunch.Installation.<sha256(root)>`) et la transaction de configuration, expose le point de terminaison de contrôle local tant que la session est active et affiche l'[icône de la zone de notification](windows-tray.md). Exactement un propriétaire par installation : si un propriétaire répond déjà à `session.status` sur le point de terminaison de contrôle, un second lancement échoue avec `OL_E_SESSION_ALREADY_ACTIVE` (sortie `7`).
- **Client** : toute invocation sans argument d'installation qui envoie `session status`, `session stop`, `events read`, `events watch` ou une opération runtime. Elle est transmise via le pipe de contrôle local ; sans propriétaire, elle échoue avec `OL_E_NO_ACTIVE_SESSION` (sortie `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Ordre de dispatch (`CliProgram.RunAsync`)

1. `/silent` (lorsque le processus ne s'exécute pas déjà sous `OmsiLaunchW.exe`) : démarre `OmsiLaunchW.exe` depuis le répertoire de l'exécutable via `ShellExecute` (sans héritage de handles) avec les mêmes arguments moins `/silent`/`--silent`, écrit l'enveloppe `silent` (`delegated`, `host_process_id`) et renvoie `0`. Le processus console n'attend pas la session ; voir [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` renvoient `7`.
2. `/version` : enveloppe `version` avec `product`, `version` (version informationnelle de l'assembly, estampillée à partir de `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`) ; sortie `0`.
3. `capabilities` : enveloppe contenant chaque descripteur `PublicStableBeta` ou `PublicExperimental` du catalogue canonique `PublicCapabilityRegistry` (avec ses opérations, capacités natives, preuves et note) ; sortie `0`.
4. `help [family]` : enveloppe `help` avec `usage`, `product_version`, `protocol_version`, `family` et les `commands` publiques (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), éventuellement filtrées par famille ; sortie `0`.
5. `profiles` : enveloppe avec `requirement` (`OMSI 2`), le rapport `installation` de la racine cible (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), les `known_layouts` et les `predicted_capabilities` résolues à partir du fichier exécutable (la résolution dans le processus au démarrage fait foi) ; sortie `0`.
6. Opération runtime client (pas d'argument d'installation et une route ou `/runtime:`) : les arguments sont validés par `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, sortie `2`), puis `runtime.execute` est transmis avec un timeout de 8 s (30 s pour `road-vehicles.spawn`).
7. Client `session status` (750 ms), `session stop` (lié à l'identifiant de la session active, 750 ms), `events read` (750 ms), `events watch` (interroge toutes les 250 ms jusqu'à Ctrl+C).
8. `detect`, ou **aucun argument du tout** (pas d'installation, pas de commande, pas de `/?`, pas de `/spec`, pas d'option de lancement, pas d'option de récupération, pas de `/list`) : énumère les processus `Omsi` et sonde le point de terminaison de contrôle (250 ms) ; enveloppe `detect` ; sortie `0`.
9. `/?` ou `/help` : affiche le texte d'utilisation, sortie `0`. Toute autre invocation qui comporte un mot de commande mais aucune route pouvant être dispatchée (par exemple `d3d` seul ou `session status D:\OMSI`) affiche le texte d'utilisation et sort avec `2`.
10. Mode propriétaire. Préconditions : `plugins\OmsiLaunch.Plugin.opl` et `plugins\OmsiLaunch.Native.x86.dll` doivent exister à côté de l'exécutable (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, sortie `7`). `release-manifest.json` à côté de l'exécutable, lorsqu'il est présent, fournit les hashs attendus du plugin.
11. `/recovery-status` / `/recover` : `RecoverPendingAsync` ; enveloppe `recover` avec `pending`, `recovered`, `diagnostics` ; sortie `8` uniquement lorsqu'une restauration a été demandée et ne s'est pas achevée, sinon `0`.
12. `/list:<category>` : `DiscoverAsync` ; enveloppe `content.list` ; sortie `0`. `/list:SessionProfiles` utilise à la place `ListSessionProfilesAsync` ; enveloppe `session-profiles.list`.
13. Construction du `LaunchSpec` (`BuildSpecAsync`), planification (`PlanSessionAsync`), affichage du plan. `/plan` ou `/validate` : sortie `0` si `IsRunnable`, sinon `1`. Un plan non exécutable ne démarre jamais OMSI (sortie `1`) ; sous `OmsiLaunchW.exe`, un lancement avec un plan non exécutable affiche son dernier diagnostic `OL_E_` dans une boîte de message (audit de documentation BUG-06). La planification vérifie également la closure du plugin permanent installé par rapport à `release-manifest.json`, de sorte qu'un plugin manquant ou modifié rend le plan non exécutable (`OL_E_PERMANENT_PLUGIN_*`).
14. Recherche d'un propriétaire existant (`OL_E_SESSION_ALREADY_ACTIVE`, sortie `7`), puis `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Cycle de vie du propriétaire (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. À partir de là, chaque chemin de sortie atteint `CloseAsync` dans un bloc `finally` : exceptions, Ctrl+C (`Console.CancelKeyPress`), fermeture de la console / déconnexion (`AppDomain.ProcessExit` avec un budget de 4 s pour l'arrêt et la restauration ; ce qui reste est récupéré par le journal au démarrage suivant), « End session » (`Terminer la session`) dans la zone de notification, `session.stop` via le pipe et `/observe-seconds`.
2. L'icône de la zone de notification est créée, sauf si `Presentation.SuppressTrayIcon` est défini dans la spec.
3. Attente de `Running` pendant `StartupTimeoutSeconds + 5` secondes. L'état est affiché. Si l'état n'est pas `Running`, sortie `1` (`OmsiLaunchW.exe` affiche `The OMSI session did not reach gameplay.` avec le dernier diagnostic `OL_E_` ou `OL_E_SESSION_START_FAILED`).
4. Les lots de validation (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) s'exécutent et écrivent leurs artefacts.
5. Le point de terminaison de contrôle local démarre.
6. `/runtime:<operation>` s'exécute une fois (5 s, 15 s pour `road-vehicles.spawn`) ; le résultat est écrit dans `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` et affiché. Une commande runtime en échec ne met jamais fin à la session (`runtime_error` est affiché à la place).
7. Attente : avec `/observe-seconds:n`, la session est arrêtée après `n` secondes **ou** plus tôt lors d'un arrêt par la zone de notification ou le pipe, ou lorsque OMSI se termine ; sans cette option, le propriétaire attend que OMSI se termine ou qu'un arrêt soit demandé.
8. L'état final est affiché ; sortie `0` si `Completed`, `1` sinon.

`session.stop`, « End session » (`Terminer la session`) dans la zone de notification, Ctrl+C et `CloseAsync` demandent tous l'arrêt canonique : OMSI est terminé avec `TerminateProcess` (la routine d'arrêt propre à OMSI ne s'exécute pas et `options.cfg` n'est pas réécrit par OMSI), puis chaque fichier appartenant à la session est restauré. Voir [cycle de vie de la session](../concepts/session-lifecycle.md) et [transactions et récupération](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Mots de commande

Chaque mot accepté en première position (`CliInput.CommandWordsAccepted`) :

| Mot | Objet | Mode | Remarques |
|---|---|---|---|
| `capabilities` | Lister les capacités publiques | Local, sans session | Enveloppe `capabilities`. |
| `profiles` | Lister les variantes de `Omsi.exe` prises en charge | Local, sans session | Enveloppe `profiles`. |
| `detect` | Signaler les processus `Omsi.exe` et un propriétaire actif | Local, sans session | C'est aussi le comportement par défaut lorsqu'aucun argument n'est fourni. États : `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, `UNKNOWN_BINARY_FOUND` par processus lorsque le binaire ne peut pas être inspecté ; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Utilisation et catalogue public des commandes | Local, sans session | `help <family>` filtre par famille de capacités (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Client | Exactement un mot à la suite ; toute autre forme affiche l'utilisation, sortie `2`. `session plan`/`session start` sont des noms de routes de l'API, pas des mots de la CLI : utilisez `/plan` et `/new`. |
| `events` | `events read`, `events watch` | Client | `read` renvoie une fois la liste bornée des événements ; `watch` affiche chaque nouvel événement (selon `Sequence`) sous forme d'enveloppe `events.watch` toutes les 250 ms jusqu'à Ctrl+C (sortie `0`), `4` lorsqu'aucun propriétaire ne répond, `7` en cas d'erreur de contrôle. |
| `time` | `time get`, `time set` | Route client | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Route client | |
| `map` | `map get` | Route client | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Route client | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Route client | |
| `player` | `player get` | Route client | |
| `humans` | `humans list`, `humans get`, `humans summary` | Route client | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Route client | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Route client | |
| `constants` | `constants list`, `constants get` | Route client | |
| `curves` | `curves list`, `curves evaluate` | Route client | |
| `hof` | `hof get` | Route client | |
| `drivers` | `drivers list` | Route client | |
| `tickets` | `tickets get` | Route client | |
| `d3d` | Mot de famille réservé | Aucun | `d3d` n'a **aucune route hiérarchique** : `d3d texture ...` est une route inconnue (sortie `2`) et `d3d` seul affiche l'utilisation (sortie `2`). Les opérations D3D s'atteignent avec `/runtime:d3d.status`, `/runtime:d3d.texture.create`, etc. (voir [Opérations sans route](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Routes hiérarchiques

`CliInput.HierarchicalRoutes` associe une route en minuscules à un identifiant d'opération runtime. Toutes les routes nécessitent une session `Running` et sont exécutées via la boîte aux lettres runtime (`ExecuteRuntimeAsync`). Les écritures runtime ne modifient que l'état en mémoire d'OMSI : elles ne touchent jamais de fichiers, ne font pas partie de la transaction de configuration et ne sont **pas** annulées à l'arrêt (OMSI est terminé). La stabilité suit `PublicCapabilityRegistry` et la [matrice de validation](../status/runtime-validation-status.md) ; les détails et les champs de résultat figurent dans [contrôle runtime](runtime-control.md).

| Route | Opération runtime | Type | Nécessite Running | Modifie OMSI | Participation à la restauration | Stabilité | Remarques |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Oui | Non | Aucune | STABLE_BETA | Champs d'horloge et de calendrier. |
| `time set` | `time.set` | Write | Oui | Oui (horloge en mémoire) | Aucune, non annulée | EXPERIMENTAL | Par exemple `--minute=<0..59>` ; écriture, relecture et restauration validées le 2026-09-20. |
| `weather get` | `weather.read` | Read | Oui | Non | Aucune | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Oui | Oui (météo en mémoire) | Aucune, non annulé | EXPERIMENTAL | Par exemple `--wind_speed=5 --cloud_type="Cumulus 1"`. Météo manuelle : les mises à jour METAR en direct s'arrêtent. Validation à l'exécution en attente. |
| `weather metar set` | `weather.metar.set` | Write | Oui | Oui (météo en mémoire ; OMSI enregistre l'ICAO dans `options.cfg`, que la session restaure) | Aucune, non annulé | EXPERIMENTAL | `--icao=<ICAO>`. Météo METAR en direct, comme la définit la boîte de dialogue météo d'OMSI. Validation à l'exécution en attente. |
| `weather actual get` | `weather.actual.read` | Read | Oui | Non | Aucune | EXPERIMENTAL | État du contrôleur météo actuelle/ICAO. |
| `map get` | `map.read` | Read | Oui | Non | Aucune | STABLE_BETA | Nom, fichier, description, nombre de tuiles, plage d'années et sens de circulation de la carte ; revalidé à l'exécution sur le slot de carte corrigé. |
| `camera get` | `camera.read` | Read | Oui | Non | Aucune | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Oui | Oui (scalaires de caméra, p. ex. `--field_of_view=`) | Aucune, non annulée | EXPERIMENTAL | Écriture et relecture du FOV validées. |
| `camera lock` | `camera.lock` | Action | Oui | Oui (politique limitée à la session) | Aucune | EXPERIMENTAL | Nécessite `--family=<0..3>` (conducteur=0, passager=1, extérieur=2, carte=3), `--preset=<n>` facultatif (famille 0 ou 1). Nécessite un véhicule du joueur (par exemple une situation enregistrée). Validé à l'exécution dans la closure runtime (`CAM01`) ; la chaîne `RuntimeValidation` du registre indique toujours `STATICALLY_VALIDATED` (voir [capacités](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Oui | Oui | Aucune | EXPERIMENTAL | Libère la politique définie par `camera lock` (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Oui | Non | Aucune | STABLE_BETA | Renvoie des handles `rv-NNNNNN` limités à la session. |
| `vehicles get` | `road-vehicle.read` | Read | Oui | Non | Aucune | STABLE_BETA | Nécessite `--handle=`. Handle obsolète : `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Oui | Non | Aucune | STABLE_BETA | Décomptes et état du joueur, sans handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Oui | Oui (ajoute un RoadVehicle) | Aucune, non supprimé | EXPERIMENTAL | Nécessite `--model=Vehicles\...\*.bus`. Timeout client de 30 s, timeout propriétaire de 15 s. N'attribue pas le véhicule du joueur. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Oui | Oui | Aucune | EXPERIMENTAL | `PlaceRandomBus` profilé. |
| `player get` | `player-vehicle.read` | Read | Oui | Non | Aucune | STABLE_BETA | Null sémantique lorsqu'il n'y a pas de véhicule du joueur. |
| `humans list` | `humans.list` | Read | Oui | Non | Aucune | EXPERIMENTAL | Renvoie des handles `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Oui | Non | Aucune | EXPERIMENTAL | Nécessite `--handle=`. |
| `humans summary` | `humans.read` | Read | Oui | Non | Aucune | EXPERIMENTAL | Décomptes uniquement. |
| `timetable get` | `timetable.read` | Read | Oui | Non | Aucune | STABLE_BETA | État du gestionnaire d'horaires. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Oui | Non | Aucune | STABLE_BETA | Fait partie de la capacité `timetable.read` ; preuves de lecture par lot du 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable lines list` | `timetable.lines.list` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable tours list` | `timetable.tours.list` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable logs list` | `timetable.logs.read` | Read | Oui | Non | Aucune | STABLE_BETA | Idem. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Oui | Non | Aucune | EXPERIMENTAL | Numéros de véhicule qu'OMSI exclut de l'utilisation libre par l'IA (réservés par des services d'horaire ou des listes d'IA fixes). Paginé (`cursor`, `limit`, au plus 256 par page). |
| `map tiles list` | `map.tiles.list` | Read | Oui | Non | Aucune | EXPERIMENTAL | Les tuiles de la carte : position dans la grille, index propre, fichier et état de chargement, nombre d'objets, de chemins et de splines. Paginé (`cursor`, `limit`, au plus 256 par page). |
| `drivers list` | `drivers.read` | Read | Oui | Non | Aucune | EXPERIMENTAL | Enregistrements de conducteurs. |
| `tickets get` | `tickets.read` | Read | Oui | Non | Aucune | EXPERIMENTAL | Enregistrements de jeux de billets. |
| `hof get` | `vehicle.hofs.read` | Read | Oui | Non | Aucune | STABLE_BETA | Nécessite `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Oui | Non | Aucune | STABLE_BETA | Nécessite `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Oui | Non | Aucune | STABLE_BETA | Nécessite `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Oui | Non | Aucune | STABLE_BETA | Nécessite `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Oui | Non | Aucune | STABLE_BETA | Nécessite `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Oui | Non | Aucune | EXPERIMENTAL | Nécessite `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Oui | Non | Aucune | EXPERIMENTAL | Nécessite `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Oui | Oui (variable de script) | Aucune, non annulée | EXPERIMENTAL | Nécessite `--handle=`, `--name=`, `--value=` (nombre fini). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Oui | Non | Aucune | EXPERIMENTAL | Nécessite `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Oui | Non | Aucune | EXPERIMENTAL | Nécessite `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Oui | Oui (état de script en mémoire) | Aucune, non annulé | EXPERIMENTAL | Nécessite `--handle=`, `--name=`, `--value=`. Le `SetStringVariable` d'OMSI lui-même. Validation à l'exécution en attente. |
| `scripts trigger` | `vehicle.trigger` | Action | Oui | Oui (exécute du code de script du véhicule) | Aucune, non annulé | EXPERIMENTAL | Nécessite `--handle=`, `--name=`, `--value=` (`1` appui, `0` relâchement, `2` glissement). Validation à l'exécution en attente. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Oui | Oui (joue des sons du véhicule) | Aucune, non annulé | EXPERIMENTAL | Nécessite `--handle=`, `--trigger=` ; `--file=` facultatif. Validation à l'exécution en attente. |

<a id="operations-without-a-route"></a>
### Opérations sans route

Ces identifiants d'opération publics (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) n'ont pas de route hiérarchique et s'invoquent avec `/runtime:<operation>` accompagné de `--key=value` ou `/runtime-arg:key=value` : `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` obligatoires ; `levels` facultatif), `d3d.texture.describe` (`handle` ; `level` facultatif), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` obligatoires ; `level`, `x`, `y` facultatifs), `d3d.texture.release` (`handle`). Les opérations D3D sont EXPERIMENTAL ; le cycle de vie des textures et l'invalidation lors d'une réinitialisation du périphérique sont validés à l'exécution (closure runtime `H02`, `D01` ; voir [capacités](capabilities.md)). `timetable.track-entries.list` et `timetable.tour-entries.list` sont des listes bornées : un résultat qui ne tient pas dans le slot runtime est raccourci (`truncated=true`). `internal.road-vehicles.make-basic` est INTERNAL et est rejeté avec `OL_E_RUNTIME_OPERATION_UNKNOWN` par la CLI comme par l'API.

<a id="flags"></a>
## Options

Chaque option de `CliInput.KnownFlags`. La « phase » est *lancement* (façonne le `LaunchSpec`/le plan d'une nouvelle session), *runtime* (agit sur une session en cours d'exécution) ou *contrôle* (modifie le comportement de la CLI elle-même). Les options analysées uniquement par compatibilité (`CliInput.AcceptedNoEffectFlags`) sont signalées sur leur ligne.

<a id="control-and-output"></a>
### Contrôle et sortie

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/?` | `/?` | désactivée | contrôle | STABLE_BETA | Affiche le texte d'utilisation, sortie `0`. |
| `/help` | `/help` | désactivée | contrôle | STABLE_BETA | Identique à `/?`. (Le mot nu `help` renvoie à la place le catalogue structuré.) |
| `/version` | `/version` | désactivée | contrôle | STABLE_BETA | Enveloppe `version`, sortie `0`. Évaluée avant toute autre commande, sauf `/silent`. |
| `/json` | `/json` ou `--json` | désactivée | contrôle | STABLE_BETA | Émet des enveloppes JSON ; force aussi la sortie console, même sous `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | désactivée | contrôle | STABLE_BETA | Supprime la sortie lisible en cas de succès. La sortie JSON (`/json`) et les erreurs sont toujours écrites ; le code de sortie est inchangé. |
| `/silent` | `/silent` (également `--silent`) | désactivée | contrôle | EXPERIMENTAL | Délègue toute la ligne de commande à `OmsiLaunchW.exe` et renvoie `0` dès que le processus hôte a démarré. Le résultat de la session est signalé par `OmsiLaunchW.exe` (boîtes de message, icône de la zone de notification), `.omsilaunch\diagnostics` et le point de terminaison de contrôle local. La délégation et les boîtes de dialogue d'échec sont validées à l'exécution (closure runtime `T04`) ; voir [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | contrôle | REMOVED | Supprimée dans 0.1.0-beta4 : rejetée avec `OL_E_INVALID_ARGUMENT` (code de sortie `2`). Le propriétaire de la session sert toujours le point de terminaison de contrôle local. |
| `/verbose` | `/verbose` | désactivée | lancement | PARTIAL | `DiagnosticsSpec.Verbose`. Les valeurs sont transportées dans la spec ; leur effet se limite à la trace de l'hôte sous `.omsilaunch\diagnostics`. |
| `/log` | `/log` | activée (`DiagnosticsSpec.Log` vaut `true` par défaut) | lancement | PARTIAL | `DiagnosticsSpec.Log`. Toujours activée en pratique. |
| `/logall` | `/logall` | désactivée | lancement | PARTIAL | Définit ensemble `Verbose`, `ProcessTrace`, `PluginTrace` et `NativeTrace`. |
| `/omsi-logall` | `/omsi-logall` | désactivée | lancement | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | désactivée | lancement | PARTIAL | Alias de `/trace-process`. |
| `/trace-process` | `/trace-process` | désactivée | lancement | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | désactivée | lancement | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace` : le plugin signale chaque opération runtime (`plugin.trace.operation` : opération, résultat, code, durée) dans la trace de l'hôte de la session. |
| `/trace-native` | `/trace-native` | désactivée | lancement | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace` : la couche native signale chaque installation et suppression de hook (`native.trace`) dans la trace de l'hôte de la session. |

<a id="planning-validation-and-harnesses"></a>
### Planification, validation et harnais

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/plan` | `/plan` | désactivée | lancement | STABLE_BETA | Construit et affiche le `SessionPlan`, sans démarrer OMSI. Sortie `0` si `IsRunnable`, `1` sinon. Nécessite une sélection de lancement (`/new`, `/saved`, `/spec` ou un argument d'installation) ; `/plan` seul, sans rien d'autre, exécute `detect`. |
| `/validate` | `/validate` | désactivée | lancement | STABLE_BETA | Identique à `/plan` dans ce build. |
| `/runtime-batch` | `/runtime-batch` | désactivée | runtime (propriétaire) | INTERNAL | Harnais de validation : après `Running`, exécute l'ensemble des opérations de lecture et écrit `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | désactivée | runtime (propriétaire) | INTERNAL | Harnais de validation : lectures plus `time.set`, `camera.set` et `vehicle.variable.set` avec restauration ; écrit `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | désactivée | runtime (propriétaire) | INTERNAL | Harnais de validation du cycle de vie des textures D3D ; écrit `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | aucune | runtime | STABLE_BETA (dispatch) | Sélectionne une opération runtime publique par son identifiant. Mode client (pas d'argument d'installation) : transmise au propriétaire. Mode propriétaire : exécutée une fois après `Running`. Identifiants inconnus : `OL_E_RUNTIME_OPERATION_UNKNOWN`, sortie `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (répétable) | aucune | runtime | STABLE_BETA (dispatch) | Argument runtime ; équivalent à `--key=value`. `=` manquant : `/runtime-arg requires key=value`, sortie `2`. |

<a id="world-selection"></a>
### Sélection du monde

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` est le mode par défaut, mais un lancement n'est demandé que si l'une des options `/new`, `/saved`, `/last`, `/spec` est présente | lancement | STABLE_BETA | NEW_MAP. Nécessite `/map` et `/entrypoint-index` (un plan sans index de point d'entrée présenté signale `OL_E_ENTRYPOINT_REQUIRED` ; sans `/map`, aucune carte n'est résolue). `/new` ne sélectionne jamais une carte de manière implicite. |
| `/saved` | `/saved:<file.osn>` | aucune | lancement | STABLE_BETA | SAVED_SITUATION. La carte et la position proviennent du `.osn` ; `/map`, `/entrypoint`, `/entrypoint-index` sont rejetées avec `/saved` (sortie `2`). Situation manquante : `OL_E_SITUATION_NOT_FOUND` ; sa carte manquante : `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | aucune | lancement | EXPERIMENTAL | LAST_MAP_STATE : charge le `laststn.osn` propre à la carte (écrit par OMSI à la fermeture de la carte) via la branche « dernier état » du formulaire Start. Nécessite `/map` ; une carte sans `laststn.osn` donne `OL_E_SITUATION_NOT_FOUND`. Jamais d'heuristique du `.osn` le plus récent. Validation à l'exécution en attente. |
| `/map` | `/map:<identity>` (par exemple `maps\Grundorf\global.cfg`) | aucune | lancement | STABLE_BETA | Identité de la carte pour `/new`, ou portée de `/list:Entrypoints`. Inconnue : `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | aucune | lancement | EXPERIMENTAL | Point d'entrée par son libellé exact (sensible à la casse) ou par une identité issue de `/list:entrypoints`. OMSI ne présente que le premier enregistrement de chaque libellé ; un enregistrement ultérieur qui le partage est refusé (`OL_E_ENTRYPOINT_NOT_PRESENTED`), de même qu'un enregistrement inconnu (`OL_E_ENTRYPOINT_NOT_FOUND`). Validation à l'exécution en attente. Mutuellement exclusive avec `/entrypoint-index` (l'identité l'emporte et efface l'index). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | aucune | lancement | STABLE_BETA | Index du point d'entrée dans la liste présentée (à partir de 1, tel qu'OMSI le présente). Obligatoire pour un plan NEW_MAP exécutable. |

<a id="date-time-and-weather"></a>
### Date, heure et météo

Ces options sont acceptées et transportées dans le `LaunchSpec`. La date, l'heure et l'année de démarrage ne s'appliquent qu'aux sessions NEW_MAP : avec `/saved` ou `/last`, le planificateur les enregistre comme `UNAVAILABLE` **et ajoute `OL_E_CAPABILITY_UNAVAILABLE`, de sorte que le plan est NON EXÉCUTABLE (sortie `1`)**, car une situation enregistrée conserve sa propre horloge. La météo s'applique à tous les modes de monde. Toutes sont `EXPERIMENTAL` : le chemin de démarrage natif est fermé statiquement et la validation à l'exécution est en attente. Un fichier `/spec` ou un profil de session qui les définit a le même effet.

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` ou `/date:system` | non définie | lancement | EXPERIMENTAL | `DateSpec` explicite/système : la date de départ d'une session NEW_MAP (voir [launchspec](launchspec.md#datespec-timespec-yearspec)). Valeur non analysable : `OL_E_INVALID_ARGUMENT`, sortie `2` ; date impossible : `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` ou `/time:system` | non définie | lancement | EXPERIMENTAL | `TimeSpec` explicite/système : l'heure de départ d'une session NEW_MAP. |
| `/year` | `/year:<n>` ou `/year:system` | non définie | lancement | EXPERIMENTAL | `YearSpec` : l'année de départ d'une session NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | non définie | lancement | EXPERIMENTAL | `WeatherMode.Preset` : un fichier du dossier `weather` de l'installation, par nom (`Regen`) ou par chemin (`weather\Regen.owt`) ; un fichier absent donne `OL_E_WEATHER_INVALID` à la planification. Chargé par le `loadWeather` d'OMSI lui-même juste après le démarrage du monde ; le METAR en direct reste désactivé. Validation à l'exécution en attente. |
| `/weather-icao` | `/weather-icao:<code>` | non définie | lancement | EXPERIMENTAL | `WeatherMode.Icao` : météo METAR en direct pour une station OACI de quatre caractères (`OL_E_WEATHER_INVALID` sinon). OMSI enregistre la station dans `options.cfg`, que la session restaure. Validation à l'exécution en attente. |
| `/weather-real` | `/weather-real` | non définie | lancement | EXPERIMENTAL | `WeatherMode.RealCurrent` : météo METAR en direct pour la station qu'OMSI a déjà configurée ; sans station, le démarrage échoue avec `OL_E_WEATHER_APPLY_FAILED`. La dernière option parmi `/weather`, `/weather-icao`, `/weather-real` l'emporte. Toute météo de démarrage remplace la météo d'une situation enregistrée. |

<a id="player-vehicle"></a>
### Véhicule du joueur

Acceptées et résolues par rapport à l'installation, mais non appliquées par le runtime : chaque champ défini est `UNAVAILABLE` et ajoute `OL_E_CAPABILITY_UNAVAILABLE` (plan NON EXÉCUTABLE, sortie `1`).

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | non définie | lancement | UNAVAILABLE | Résolue en premier (`OL_E_VEHICLE_NOT_FOUND` si inconnue). |
| `/repaint` | `/repaint:<id>` | non définie | lancement | UNAVAILABLE | Résolue uniquement conjointement avec `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | non définie | lancement | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` si inconnue. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | aucune | lancement | STABLE_BETA | Une opération de session de [gestion des HOF](hof-management.md) : `HofSourceKind` et `Source`. Nécessite au moins un `/hof-target`. Remplace le `HofManagement` d'un `/spec` ; avec un préréglage de profil de session qui contient `hofs` : `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Mal formée : sortie `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (répétable) | aucune | lancement | STABLE_BETA | Un véhicule cible sous `Vehicles\`. Sans `/hof-source` : sortie `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (répétable) | tous les fichiers | lancement | STABLE_BETA | Restreint la source à ces noms de fichiers. Sans `/hof-source` : sortie `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | lancement | STABLE_BETA | `HofApplyMode`. Sans `/hof-source` : sortie `2`. |
| `/module` | `/module:<id>` (répétable) | aucune | lancement | EXPERIMENTAL | Demande un [module runtime](runtime-modules.md) pour la session (`LaunchSpec.Modules.Enable`, ajouté aux modules d'un fichier `/spec`). Un module demandé qui ne peut pas être chargé rend le plan non exécutable avec son code `OL_E_MODULE_*` (sortie `1`). Ses opérations sont atteintes avec `/runtime:module.<id>.<operation>` et `/runtime-arg`. Avec un préréglage de profil de session qui définit `enabled: false` pour lui : `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (répétable) | aucune | lancement | EXPERIMENTAL | Garde un module hors de la session, y compris un module chargé par défaut, par sa carte ou par le fichier de paramètres (`LaunchSpec.Modules.Disable`). Avec un préréglage qui définit `enabled: true` pour lui : `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Sans valeur : sortie `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (répétable) | aucune | lancement | EXPERIMENTAL | Définit un paramètre de module (`LaunchSpec.Modules.Settings`) ; l'emporte sur le fichier `/spec`, le fichier de paramètres et la valeur par défaut. Une clé que le préréglage sélectionné définit aussi : `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Pas de la forme `<id>.<key>=<value>` : sortie `2`. Valeur invalide ou clé non déclarée : diagnostic de plan `OL_E_MODULE_SETTING_INVALID` (sortie `1`). |
| `/<module flag>` | `/<flag>:<value>` ; `/<flag>` seule vaut `true` pour un paramètre booléen | aucune | lancement | EXPERIMENTAL | Une option qu'un module installé déclare pour l'un de ses paramètres (`flag` dans son manifeste ; listée par `/list:RuntimeModules`). Elle est résolue lorsqu'une session est planifiée et équivaut à `/module-setting:<id>.<key>=<value>`. Une option qu'aucun module installé ne déclare, ou utilisée en dehors d'un lancement : `Unknown argument`, sortie `2`. Une option déclarée par deux modules : `OL_E_MODULE_SETTING_INVALID`, sortie `2`. Une option de module ne remplace jamais une option intégrée. |
| `/fleet` | `/fleet:<n>` | non définie | lancement | UNAVAILABLE | Numéro de flotte. |
| `/registration` | `/registration:<text>` | non définie | lancement | UNAVAILABLE | Immatriculation. |
| `/no-vehicle` | `/no-vehicle` | désactivée | lancement | STABLE_BETA | Efface tout véhicule du joueur provenant de la base (`/spec` ou profil). Sans effet indésirable. |

<a id="configuration-overlays"></a>
### Overlays de configuration

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (répétable ; clés insensibles à la casse) | aucune | lancement | STABLE_BETA | Overlay sémantique de `options.cfg` issu de `ConfigurationCatalog` (par exemple `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Clé inconnue : `OL_E_UNKNOWN_SETTING` (sortie `2`) ; clé en lecture seule (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`) : `OL_E_SETTING_NOT_WRITABLE` (sortie `2`) ; valeur hors plage ou mal formée : `OL_E_INVALID_SETTING_VALUE` lors de la construction de l'overlay. L'overlay est une mutation de session : capturé dans un instantané, appliqué avant le démarrage d'OMSI, restauré octet pour octet à l'arrêt (RV-005 `RUNTIME_VALIDATED`). Conflit avec une clé détenue par le préréglage d'un profil sélectionné : `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Présentation de l'écran de démarrage

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (insensible à la casse) | `Managed` | lancement | STABLE_BETA | `Managed` : les BMP 640x480 24 bits du paquet sont copiés une fois dans `<root>\.omsilaunch\assets\splash`, et `GUI\NewSplashscreen_ENG.bmp` ainsi que `GUI\NewSplashscreen_<lang>.bmp` sont remplacés par overlay de manière transactionnelle et restaurés à l'identique (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (alias) : fichiers OMSI non modifiés. Valeur manquante : `/splash requires Unset, Native, or Managed`, sortie `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (également `pt-BR`, `de`, `fr`, `en` ; toute autre valeur se replie sur `ENG`) | `[language]` de `options.cfg`, sinon `ENG` | lancement | STABLE_BETA | Sélectionne le fichier cible localisé. |
| `/splash-assets` | `/splash-assets:<directory>` (les chemins relatifs sont résolus sous la racine de l'installation) | `<root>\.omsilaunch\assets\splash`, sinon l'ensemble fourni dans le paquet | lancement | STABLE_BETA | Répertoire de ressources personnalisé ; doit contenir `ENG.bmp` et, pour une langue autre que l'anglais, `<lang>.bmp`. Erreurs : `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (remontées sous la forme `OL_E_SESSION_PRESENTATION_INVALID` dans le plan ; non exécutable). |

<a id="internet-textures"></a>
### Textures Internet

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | lancement | EXPERIMENTAL | `Native` : aucune modification. `Disabled` : le téléchargeur profilé intégré au processus est neutralisé. `Override` : le profil `.itx` indiqué est appliqué par overlay en tant que `Texture\standard.itx` ; chaque cible HTTP(S) qu'il liste ainsi que `Texture\standard.ipr` deviennent des suppressions de session (supprimées pour la session, restaurées à l'arrêt). Valeur manquante : sortie `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | aucune | lancement | EXPERIMENTAL | Obligatoire avec `Override` (`OL_E_ITX_PROFILE_REQUIRED`, sortie `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (doit être composé de paires de lignes URL/cible avec des URL `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (les cibles doivent se résoudre sous `Texture\`, sans chemins absolus, sans `..` ni points d'analyse). |

<a id="session-profiles"></a>
### Profils de session

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | aucune | lancement | STABLE_BETA (compilation ; `OmsiLaunch.ProfileTests` hors ligne) | Charge `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (voir [profils de session](session-profiles.md)). Nécessite `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, sortie `2`). Le bloc `new:` ne s'applique qu'avec `/new` ; `compatibility.maps` est appliqué pour `/new` et `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Les options explicites qui entrent en collision avec un champ détenu par le profil sont rejetées avec `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`) : carte/point d'entrée/date/heure/année/météo lorsque le bloc `new:` les détient, clés `/set` détenues par le préréglage, options d'écran de démarrage lorsque le préréglage contient `presentation`, options de textures Internet lorsqu'il contient `internet-textures`, timeouts lorsqu'il contient `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | aucune | lancement | STABLE_BETA | Sélectionne le préréglage par son `index`. Hors plage : sortie `2`. |

<a id="launchspec-file"></a>
### Fichier LaunchSpec

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | aucune | lancement | STABLE_BETA (chargeur testé hors ligne ; sémantique de session identique à celle des options) | Charge un fichier JSON `LaunchSpec` comme base (voir [LaunchSpec](launchspec.md)) et marque un lancement comme demandé. Règles (`LaunchSpecJson`) : le fichier doit exister (`OL_E_SPEC_NOT_FOUND`, sortie `6`) ; 1 MiB au maximum (`OL_E_SPEC_TOO_LARGE`, sortie `2`) ; la racine doit être un objet (`OL_E_SPEC_INVALID`) ; noms de propriétés insensibles à la casse ; commentaires `//` et virgules finales autorisés ; profondeur de 32 au maximum ; chaque propriété inconnue est rejetée avec son chemin JSON (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, sortie `2`). |

**Priorité** (`CliInput.BuildSpecAsync`) : valeurs par défaut → fichier `/spec` → `/predefined-profile` (remplace `Installation` et `World`, puis applique le profil) → options explicites. Un argument d'installation explicite l'emporte sur `RootPath` dans la spec. `/no-vehicle` efface le véhicule du joueur de la spec ; `/vehicle` et les options associées y sont fusionnées champ par champ. Les clés `/set` sont fusionnées dans `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` ne remplacent la valeur que lorsqu'elles sont indiquées. `/startup-timeout` et `/shutdown-timeout` ne remplacent la valeur que lorsqu'elles sont indiquées ; `Presentation.SuppressTrayIcon` provient uniquement de la spec (aucune option). Les options de diagnostic sont combinées par OU avec les `Diagnostics` de la spec.

<a id="content-discovery"></a>
### Découverte du contenu

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/list` | `/list:<category>` ; les catégories sont les valeurs de `ContentQueryKind` `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (insensibles à la casse) | aucune | local, sans session | STABLE_BETA | `DiscoverAsync` sur l'installation ; enveloppe `content.list` avec des entrées `Identity`, `Kind`, `DisplayName` ; sortie `0`. `SessionProfiles` répond avec `ListSessionProfilesAsync` (enveloppe `session-profiles.list`) et `RuntimeModules` avec `ListRuntimeModulesAsync` (enveloppe `runtime-modules.list` : l'id, la version, l'activation, les opérations et la validité de chaque module, ainsi que ses paramètres avec type, valeur par défaut et option). Catégorie inconnue : `Unknown discovery category`, sortie `2`. Les points d'analyse (jonctions/liens symboliques) sont ignorés, les fichiers OMSI sont lus en Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | aucune | local | STABLE_BETA | Portée transmise pour chaque catégorie sauf `Entrypoints`, qui utilise `/map` comme portée. |

<a id="timeouts-and-observation"></a>
### Timeouts et observation

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` secondes | valeur de la spec/du profil, sinon `180` | lancement | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. Le propriétaire attend `Running` pendant cette valeur plus 5 s ; `OL_E_STARTUP_TIMEOUT` met fin à la session avec la sortie `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` secondes | valeur de la spec/du profil, sinon `30` | lancement | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds` : avec `/cooperative-shutdown`, la durée pendant laquelle le superviseur attend qu'OMSI se ferme avant de le terminer ; sans cette option, OMSI est terminé immédiatement et la valeur n'a aucun effet. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | désactivée | lancement | EXPERIMENTAL | `Behavior.CooperativeShutdown` : terminer une session en cours demande à OMSI de se fermer par sa propre routine de sortie (celle qui s'exécute après qu'un utilisateur a confirmé la fermeture : elle enregistre `options.cfg` et le `laststn.osn` de la carte, puis ferme) et attend `ShutdownTimeoutSeconds` avant de le terminer. Opt-in tant que non validé à l'exécution ; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` lorsque OMSI a dû être terminé. Les fichiers de la session sont restaurés après la sortie dans les deux cas. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | aucune (exécution jusqu'à ce que OMSI se termine ou qu'un arrêt soit demandé) | runtime (propriétaire) | STABLE_BETA | Borne supérieure de la phase d'exécution : après `n` secondes en `Running`, l'arrêt canonique est demandé. Un arrêt par la zone de notification ou le pipe, ou la fin d'OMSI, y met fin plus tôt. `0` arrête immédiatement après `Running`. |

<a id="recovery"></a>
### Récupération

| Option | Syntaxe et valeurs | Défaut | Phase | Stabilité | Comportement |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | désactivée | local | STABLE_BETA | Indique si `<root>\.omsilaunch\journal.json` est en attente (`pending`), ne restaure jamais ; sortie `0`. Prend le bail d'installation : `OL_E_INSTALLATION_BUSY` (sortie `7`) tant qu'un propriétaire le détient. |
| `/recover` | `/recover` | désactivée | local | STABLE_BETA | Restaure un journal en attente (sauvegardes d'abord vérifiées par rapport au SHA-256 de l'instantané ; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` signalés dans `diagnostics`). Sortie `0` lorsque rien n'était en attente ou que la restauration s'est achevée ; `8` lorsqu'un journal était en attente et le reste. Refusée avec `OL_E_INSTALLATION_BUSY` tant que le processus OMSI journalisé (PID, heure de création, chemin de l'exe) ou, pour un journal ayant dépassé `HandoffCreated` sans PID, tout `Omsi.exe` issu de cette racine est actif. Chaque démarrage de session effectue automatiquement la même récupération avant de lire l'installation. |
| `/clear-module-data` | `/clear-module-data:<module id>` | aucune | local, sans session | EXPERIMENTAL | Supprime le répertoire de données du module `<root>\.omsilaunch\module-data\<id>` et rien d'autre ; enveloppe `module-data.clear` avec `module` et `cleared` (`false` lorsqu'il n'y avait rien à supprimer) ; sortie `0`. Prend le bail d'installation : `OL_E_INSTALLATION_BUSY` (sortie `7`) tant qu'une session détient l'installation. Un dossier de données de module atteint par un lien est refusé (`OL_E_MODULE_INTEGRITY`) ; un lien situé à l'intérieur est supprimé en tant que lien. |

<a id="output-formats"></a>
## Formats de sortie

- **Enveloppe de succès** (`CliInput.WriteEnvelope`, avec `--json`) : `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, indentée. Les réponses transmises de `session status` et `events read` ajoutent un membre `metadata` lorsque des événements plus anciens ont été omis pour tenir dans la trame de contrôle (`events_dropped_count`, voir [contrôle local](local-control.md)). Sans `--json`, seul `<object>` est affiché en JSON indenté, suivi de `Note: <n> older events were omitted to fit the control frame.` lorsque des événements ont été abandonnés.
- **Enveloppe d'erreur** (`CliInput.WriteError`, avec `--json`) : `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Sans `--json` : `OL_E_<CODE>: message` sur une ligne. Catégories : `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Sous `OmsiLaunchW.exe`, le même code et le même message sont affichés dans une boîte de message.
- **Plan et état** (`CliInput.Write`) : les enregistrements `SessionPlan`, `SessionStatus` et `RuntimeCommandResult` sont affichés en JSON indenté **sans** enveloppe. Sans `--json`, un plan est résumé sous la forme `Plan: READY profile=Omsi23004_692EBFBF` ou `Plan: NOT RUNNABLE profile=...` ; les autres enregistrements sont toujours affichés en JSON. Les valeurs d'énumération sont sérialisées en entiers (`SessionState.Running` vaut `14`, `Completed` vaut `18`, `Failed` vaut `19`).
- Noms de commande utilisés dans les enveloppes : `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile`, et l'identifiant de l'opération runtime pour les commandes runtime transmises.
- Sous `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`), rien n'est écrit sur la console sauf si `--json` est indiqué.

<a id="errors-per-command"></a>
## Erreurs par commande

| Commande | Codes d'erreur typiques | Sortie |
|---|---|---|
| Tout échec d'analyse | `OL_E_INVALID_ARGUMENT`, codes de profil de session (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Route client, `/runtime` (client) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`) ; `OL_E_NO_ACTIVE_SESSION` (`4`) ; `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` renvoyés par le propriétaire, p. ex. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`) ; `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Vérifications préalables du propriétaire | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`) ; `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`) ; en attente mais non récupéré (`8`) | `7`, `8` |
| `/list` | catégorie inconnue (`2`) ; `OL_E_INSTALLATION_NOT_FOUND`/répertoires manquants (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`) ; `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, lancement | diagnostics du plan : `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (closure du plugin installé), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informatif) | `1` |
| Démarrage de la session | `OL_E_PLAN_NOT_RUNNABLE` (nouvelle planification au démarrage, `1`) ; `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normalement signalés par la planification comme diagnostic du plan, sortie `1` ; `7` uniquement si les fichiers du plugin changent entre la planification et le démarrage), `OL_E_INSTALLATION_BUSY` (`7`) ; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (session `Failed`, `1`) | `1`, `7` |
| Exception non gérée, où que ce soit | classée par `CliProgram.Classify` (voir [codes de sortie](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Environnement

| Variable | Définie par | Effet |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive` : sortie console supprimée, échecs affichés dans des boîtes de message, `/silent` non redélégué. |

<a id="see-also"></a>
## Voir aussi

[Exemples de CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [codes de sortie](exit-codes.md) · [erreurs](errors.md) · [contrôle local](local-control.md) · [zone de notification Windows](windows-tray.md) · [contrôle runtime](runtime-control.md) · [capacités](capabilities.md) · [LaunchSpec](launchspec.md) · [profils de session](session-profiles.md) · [empaquetage](packaging.md) · [compatibilité](compatibility.md) · [limitations connues](known-limitations.md) · [API publique](public-api.md)
