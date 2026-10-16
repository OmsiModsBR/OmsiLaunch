# Exemples de CLI

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Traduction de la [page originale en anglais](../../../reference/cli-examples.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Invocations minimales et correctes de `OmsiLaunch.exe` pour OmsiLaunch `0.1.0-beta4`, chacune avec son code de sortie de processus attendu et une indication de ce qui est modifié et restauré. Chaque exemple s'exécute depuis la racine de l'installation OMSI (`<OMSI_PATH>`), sauf indication contraire ; la syntaxe est définie dans la [référence de la CLI](cli.md) et les codes de sortie dans [codes de sortie](exit-codes.md). `--json` peut être ajouté à n'importe quelle commande pour obtenir l'enveloppe structurée.

## Conventions

- **Modifie** : fichiers ou état d'OMSI modifiés par la commande. « overlay de session » désigne un fichier capturé dans un instantané de la transaction, appliqué avant le démarrage d'OMSI et restauré octet pour octet à la fin de la session.
- **Restauré** : ce qui est annulé à la fin de la session (arrêt normal, Ctrl+C, zone de notification, `session stop`, `/observe-seconds`) ou par la récupération.
- Les écritures runtime (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) ne modifient que la mémoire d'OMSI ; elles ne sont jamais annulées, car OMSI est terminé à l'arrêt.
- Espaces réservés : `<OMSI_PATH>` est l'installation OMSI 2 qui contient le paquet OmsiLaunch (par exemple `C:\OMSI 2`) ; `<OTHER_OMSI_PATH>` une autre installation ; `<SPEC_PATH>` et `<ITX_PATH>` un fichier LaunchSpec et un profil de textures Internet qui vous appartiennent ; `<HANDLE>` est un handle affiché par la commande `list` ou `create` précédente et `<BASE64>` des données de pixels en Base64. Toute autre valeur est un littéral qui fonctionne sur une installation OMSI 2 standard (`grundorf-quick` est le profil d'exemple défini sur cette page).
- Mettez entre guillemets un chemin qui contient des espaces et ne terminez pas un chemin entre guillemets par `\` (l'analyse des arguments de Windows transforme `\"` en guillemet littéral) : `"C:\OMSI 2"`, et non `"C:\OMSI 2\"`.
- Chaque ligne de commande de cette page est analysée par le contrôle bloquant de documentation (`tests/OmsiLaunch.DocumentationTests`, contrôle `examples`) ; les exemples d'identité, de découverte, de planification et de client ont également été exécutés sur une installation réelle (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identité et découverte (sans session)

```text
OmsiLaunch.exe /version
```
Sortie `0`. Affiche `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. Ne modifie rien.

```text
OmsiLaunch.exe profiles --json
```
Sortie `0`. Liste les hashs de `Omsi.exe` pris en charge et leur statut de validation. Ne modifie rien.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Sortie `0`. Catalogue public des capacités ; `help <family>` le filtre. Ne modifie rien.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Sortie `0` (les deux formes sont identiques). Signale les processus `Omsi.exe` en cours d'exécution et indique si un propriétaire OmsiLaunch répond pour cette installation. Ne modifie rien.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Sortie `0` (`2` pour une catégorie inconnue). Découverte en lecture seule ; les cycles de jonctions sont ignorés. Ne modifie rien. Les valeurs `Identity` affichées ici sont les chaînes exactes qu'attendent `/map`, `/saved`, `/vehicle-scope` et un `LaunchSpec` (par exemple `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Planification et validation

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Sortie `0` lorsque le plan est `READY`, `1` lorsqu'il est `NOT RUNNABLE` (par exemple `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Ne modifie rien ; OMSI n'est pas démarré.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Sortie `0`/`1` comme ci-dessus ; `/validate` est un alias de `/plan`. Le JSON est le `SessionPlan` brut (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Sortie `1`. `/date`, `/time`, `/year`, `/weather*` et les options du véhicule du joueur sont acceptées mais non appliquées par ce build ; le plan contient `OL_E_CAPABILITY_UNAVAILABLE` et n'est pas exécutable.

```text
OmsiLaunch.exe /last /plan
```
Sortie `1`. `LAST_MAP_STATE` est indisponible pour ce profil (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Démarrage de sessions (mode propriétaire)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Sortie `0` lorsque la session se termine en `Completed`, `1` en cas de `Failed` ou de plan non exécutable. Modifie : overlays de session `GUI\NewSplashscreen_ENG.bmp` et `GUI\NewSplashscreen_<lang>.bmp` (l'écran de démarrage géré est le comportement par défaut), la gestion de `closecheck`, le handoff de démarrage. Restauré : chaque overlay, octet pour octet, à la fin de la session. La console reste attachée jusqu'à ce que OMSI se termine, que « End session » (`Terminer la session`) soit confirmé dans la zone de notification, qu'un client envoie `session stop` ou que Ctrl+C soit pressé.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Sortie `0`. Identique à l'exemple précédent, mais la session est arrêtée 8 s après avoir atteint `Running` (plus tôt en cas d'arrêt par la zone de notification ou le pipe). Utilisé par les scripts de validation.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Sortie `0`/`1`. SAVED_SITUATION : la carte, l'heure et la position proviennent du `.osn` (`situations\Linie 5.osn` est fourni avec OMSI 2 et démarre sur Berlin-Spandau avec un bus du joueur). La valeur est l'identité de la situation affichée par `/list:Situations` (relative à l'installation, insensible à la casse) ; un simple nom de fichier tel que `Linie 5.osn` n'est pas résolu (`OL_E_SITUATION_NOT_FOUND`, sortie `1`). `/map` ou `/entrypoint-index` avec `/saved` est rejeté avec la sortie `2`. Modifications et restauration comme pour NEW_MAP. OMSI enregistre lui-même la carte de la situation dans `[last_map]` de `options.cfg` ; cette écriture d'OMSI n'est pas annulée, sauf si un `/set` applique un overlay à `options.cfg` (voir [transactions et récupération](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Sortie `0`. Modifie : `options.cfg` (overlay de session, correctif sémantique de jetons/vecteurs ; octets CP1252 préservés) ainsi que les overlays de l'écran de démarrage. Restauré : `options.cfg` et les fichiers de l'écran de démarrage à l'identique (RV-005, RV-006). `/set:graphics.texture=...` sort avec `2` (`OL_E_SETTING_NOT_WRITABLE`) ; `/set:foo=1` sort avec `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Sortie `0`. Modifie : aucun overlay d'écran de démarrage ; uniquement le handoff de démarrage et la gestion de `closecheck`. Restauré : rien à restaurer pour l'écran de démarrage.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Sortie `0` (`1` avec `OL_E_SESSION_PRESENTATION_INVALID` lorsque le répertoire ou un BMP est manquant ou n'est pas au format 640x480 24 bits). Modifie : `GUI\NewSplashscreen_ENG.bmp` et `GUI\NewSplashscreen_PTB.bmp` à partir du répertoire personnalisé (overlay de session). Restauré : les deux fichiers à l'identique.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Sortie `0`. Modifie : rien sur le disque au-delà des overlays de l'écran de démarrage ; le téléchargeur intégré au processus est neutralisé pour la session.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Sortie `0` (`2` avec `OL_E_ITX_PROFILE_REQUIRED` si le profil est omis ; `1` pour un profil invalide ou une cible en dehors de `Texture\`). Modifie : `Texture\standard.itx` (overlay de session) ; chaque cible listée dans le profil ainsi que `Texture\standard.ipr` sont des suppressions de session. Restauré : overlay retiré, originaux supprimés restaurés ; les fichiers qu'OMSI a créés à ces chemins pendant la session sont supprimés en tant que sous-produits de la session.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Sortie `0`. Attend `Running` jusqu'à 300 s (+5 s) au lieu des 180 s par défaut. `/shutdown-timeout:60` est acceptée mais n'a aucun effet dans ce build.

<a id="predefined-session-profile"></a>
### Profil de session prédéfini

Fichier de profil `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml` :

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Sortie `0`. Modifie : `options.cfg` (paramètres du préréglage, overlay de session) et les overlays de l'écran de démarrage. Restauré : tous ces fichiers. Ajouter `/map:...` ou `/set:graphics.maxFPS=60` provoque la sortie `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`) ; omettre `/predefined-profile-index` provoque la sortie `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). L'exemple fourni dans le paquet `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` montre le schéma complet, mais tel qu'il est livré, son bloc `new:` demande `date`, `time` et `weather`, que ce build ne peut pas appliquer : sa planification avec `/new` donne `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`) ; supprimez ces clés avant de l'utiliser.

<a id="launchspec-file"></a>
### Fichier LaunchSpec

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Sortie `0`/`1`. L'exemple fourni dans le paquet sélectionne Grundorf, l'index de point d'entrée `1`, l'écran de démarrage géré et les textures Internet natives ; `RootPath: "."` est résolu vers le répertoire de l'exécutable. Modifications comme pour l'exemple NEW_MAP explicite. Une spec contenant une propriété inconnue sort avec `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`) ; un fichier manquant sort avec `6` (`OL_E_SPEC_NOT_FOUND`) ; un fichier de plus de 1 MiB sort avec `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Sortie `0`/`1`. L'installation explicite `<OTHER_OMSI_PATH>` l'emporte sur le `RootPath` de la spec ; `/startup-timeout` remplace le `Behavior.StartupTimeoutSeconds` de la spec uniquement parce qu'elle a été indiquée.

<a id="silent-detached-start"></a>
### Démarrage silencieux (détaché)

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Sortie `0` dès que `OmsiLaunchW.exe` a été démarré (`{"delegated": true, "host_process_id": <pid>}`) ; `7` si `OmsiLaunchW.exe` est manquant (`OL_E_WINDOWS_HOST_MISSING`) ou n'a pas pu démarrer. Le lanceur rend la main immédiatement et ne maintient pas ouverts la console ni les pipes de l'appelant : un script qui capture sa sortie reçoit aussitôt la fin de fichier (closure runtime `T04`). La session elle-même s'exécute dans `OmsiLaunchW.exe` : pas de sortie console, échecs affichés dans des boîtes de message, icône de la zone de notification disponible. Suivez la progression avec `session status`, `events watch` et `.omsilaunch\diagnostics\<sessionId>-host.log`. Référence complète : [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Contrôle d'une session en cours d'exécution (mode client)

Exécutez ces commandes depuis le même répertoire d'installation pendant qu'un propriétaire est en cours d'exécution. Chacune sort avec `4` (`OL_E_NO_ACTIVE_SESSION`) lorsqu'aucun propriétaire ne répond et avec `7` en cas d'erreur de contrôle.

```text
OmsiLaunch.exe session status --json
```
Sortie `0`. Renvoie `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Ne modifie rien.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Sortie `0` (`events watch` s'exécute jusqu'à Ctrl+C). Événements runtime bornés (`gameplay.entered`, événements du cycle de vie D3D, ...). Ne modifie rien.

```text
OmsiLaunch.exe session stop
```
Sortie `0` (`{"accepted": true, "session_id": "..."}`). Demande l'arrêt canonique : OMSI est terminé, les overlays sont restaurés par le propriétaire, le journal est supprimé. Le client rend la main immédiatement ; le processus propriétaire se termine après la restauration.

<a id="runtime-reads"></a>
## Lectures runtime

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Sortie `0` avec le `RuntimeCommandResult` (`Succeeded`, `Values`) dans l'enveloppe. Ne modifie rien. Timeout de 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, sortie `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Sortie `0` ; `2` lorsqu'un argument obligatoire manque (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, signalé avant l'envoi de la requête) ; `7` lorsque le plugin rejette la requête : `OL_E_RUNTIME_OPERATION_FAILED` avec la raison précise dans `Values.detail`, par exemple `OL_E_RUNTIME_OBJECT_HANDLE_STALE` pour un handle qui n'identifie plus le même objet, ou `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Les handles sont limités à la session et proviennent de la `list` précédente. Les noms de variables, de constantes et de courbes sont définis par chaque modèle de véhicule : reprenez-les du résultat de `list`. Les noms ci-dessus ont été listés pour `rv-000001`, le bus du joueur d'une session `situations\Linie 5.osn`. Ne modifie rien.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Sortie `0`. Les opérations sans route hiérarchique, comme toute route, peuvent être appelées par leur identifiant d'opération. `timetable.track-entries.list` est une liste bornée : sur `situations\Linie 5.osn`, elle a renvoyé 137 entrées sur 825 avec `truncated=true` (nouveau test runtime de l'audit de documentation). Ne modifie rien.

<a id="runtime-writes"></a>
## Écritures runtime

```text
OmsiLaunch.exe time set --minute=30
```
Sortie `0`. Modifie l'horloge en mémoire d'OMSI (validé : écriture, relecture, restauration par un second `time set`). Non annulé à l'arrêt.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Sortie `0` (`2` lorsque `--family` manque pour `camera lock`). Modifie l'état de la caméra pour la session. `camera lock` nécessite un PlayerVehicle (par exemple une session `/saved`) ; dans une session `/new` headless (sans véhicule du joueur), elle échoue (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` dans `Values.detail`). Le verrouillage et le déverrouillage ont été validés à l'exécution avec une situation enregistrée (familles 0, 2 et 1 avec relecture de la caméra). Non annulé à l'arrêt ; la politique de verrouillage prend fin avec la session.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Sortie `0` (`2` si `handle`, `name` ou `value` manque). Modifie une variable de script numérique de ce véhicule. Non annulé.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Sortie `0`. Écrit l'enregistrement météo source d'OMSI et arrête les mises à jour METAR en direct ; OMSI dérive le brouillard, la lumière, le vent réel et l'humidité de cet enregistrement au prochain tick météo non suspendu. Chaque valeur est vérifiée avant toute écriture (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, sortie `7`). Une clé dérivée telle que `relative_humidity` renvoie `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` et nomme la clé à définir à la place. Non annulé. EXPERIMENTAL : validation à l'exécution en attente.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Sortie `0`. Passe à la météo METAR en direct pour cette station ; OMSI la télécharge lors de sa prochaine mise à jour météo. EXPERIMENTAL : validation à l'exécution en attente.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Sortie `0`. `scripts string set` passe par le `SetStringVariable` d'OMSI lui-même et renvoie la valeur relue ; un nom que le script du véhicule ne déclare pas renvoie `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (sortie `7`). `scripts trigger` déclenche un trigger comme le font les gestionnaires d'entrée d'OMSI (`1` appui, `0` relâchement) ; le script du véhicule l'exécute aussitôt. Les noms de triggers et de variables sont définis par chaque modèle de véhicule ; ceux ci-dessus sont des exemples. Non annulé. EXPERIMENTAL : validation à l'exécution en attente.

<a id="spawn"></a>
## Apparition de véhicules

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Sortie `0` avec le nouveau handle `rv-NNNNNN` dans `Values` (`2` lorsque `--model` manque ; `7` en cas de `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Timeout client de 30 s. Modifie la collection de véhicules routiers (un véhicule ajouté) ; n'attribue pas le véhicule du joueur. Non annulé ; le véhicule disparaît avec OMSI à l'arrêt.

<a id="d3d-textures"></a>
## Textures D3D

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` est la valeur `handle` affichée par `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` doit se décoder en `width * height * 4` octets pour les formats 32 bits (8 x 8 x 4 = 256 octets) et en 48 KiB au maximum. Sortie `0` ; `2` en cas d'arguments obligatoires manquants ; `7` pour `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (seconde libération, ou description après libération), `OL_E_D3D_STALE_RESOURCE_HANDLE` (un handle antérieur à une réinitialisation du périphérique, ou provenant d'une autre session), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Crée des ressources GPU appartenant à la session ; elles sont libérées explicitement ou à la fin d'OMSI. Aucun fichier n'est touché.

<a id="owner-side-single-runtime-operation"></a>
## Opération runtime unique côté propriétaire

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Sortie `0`. Démarre une session, exécute `time.read` une fois après `Running` (timeout de 5 s), écrit `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, continue de s'exécuter pendant 5 s, s'arrête et restaure. Un échec runtime est affiché sous la forme `runtime_error` et ne met pas fin à la session.

<a id="recovery"></a>
## Récupération

```text
OmsiLaunch.exe /recovery-status --json
```
Sortie `0` : `{"pending": false, ...}` lorsqu'aucun journal n'existe, `{"pending": true, "recovered": false}` lorsqu'il en existe un. Sortie `7` (`OL_E_INSTALLATION_BUSY`) tant qu'un propriétaire détient l'installation. Ne modifie rien.

```text
OmsiLaunch.exe /recover --json
```
Sortie `0` lorsque rien n'était en attente ou que la restauration s'est achevée (`recovered: true` ; `diagnostics` peut contenir `restore.session-artifact-removed` et `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) ; sortie `8` lorsque le journal était en attente et l'est toujours (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`) ; sortie `7` tant qu'un propriétaire ou l'OMSI journalisé détient l'installation (`OL_E_INSTALLATION_BUSY`) ; sortie `10` (`OL_E_INTERNAL`) lorsque les octets restaurés échouent à la vérification (`Restore hash mismatch` ou `Restore presence mismatch` ; le journal reste en attente). Modifie : restaure chaque fichier journalisé depuis `.omsilaunch\backup\<sessionId>\` après avoir vérifié son SHA-256, puis supprime le journal et le répertoire de sauvegarde. Refusé avec `OL_E_INSTALLATION_BUSY` tant que le `Omsi.exe` journalisé est encore actif (closure runtime `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Vérification rapide du code de sortie (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
