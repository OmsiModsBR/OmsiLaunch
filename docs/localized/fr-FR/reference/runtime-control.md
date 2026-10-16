# Contrôle runtime

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Traduction de la [page originale en anglais](../../../reference/runtime-control.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Le contrôle runtime est l'ensemble des opérations de lecture, d'écriture et d'action qu'OmsiLaunch exécute à l'intérieur d'une session OMSI en cours d'exécution. Cette page explique les trois moyens d'y accéder (API du propriétaire, client CLI, plan de contrôle local), comment les requêtes circulent de l'appelant jusqu'au plugin et retour, comment fonctionnent les handles et les identités de requête, quels timeouts s'appliquent, ce qui n'est délibérément pas journalisé, ainsi que les conventions d'arguments et de résultats, avec un exemple commenté pour chaque famille. L'inventaire des opérations lui-même, avec les arguments, les clés de résultat et les erreurs, se trouve dans [capacités](capabilities.md). Sources : `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` et `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` et `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Trois points d'entrée

| Point d'entrée | Qui | Chemin | Timeout |
| --- | --- | --- | --- |
| API du propriétaire | Un intégrateur qui a démarré la session dans son propre processus avec `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> validation par le registre -> recherche de la session -> boîte aux lettres | `TimeSpan` fourni par l'appelant |
| CLI du propriétaire (`/runtime:<op>`) | Le processus `OmsiLaunch.exe` propriétaire de la session | une opération exécutée juste après `Running`, résultat écrit dans `.omsilaunch\diagnostics\<session>-runtime-operation.json` et sur la console ; la session continue | 5 s (15 s pour `road-vehicles.spawn`) |
| Client CLI | Toute invocation de `OmsiLaunch.exe` **sans** argument d'installation, par exemple `OmsiLaunch.exe time get` | canal nommé (named pipe) `runtime.execute` vers le propriétaire de l'installation où se trouve l'exécutable -> le propriétaire appelle `ExecuteRuntimeAsync` | 8 s (30 s pour `road-vehicles.spawn`), côté client comme côté propriétaire |
| Plan de contrôle local | Tout processus du même utilisateur Windows | même protocole de pipe que le client CLI ; voir [contrôle local](local-control.md) | comme ci-dessus |

Tous les chemins aboutissent à `ExecuteRuntimeAsync`, qui applique la frontière publique dans cet ordre :

1. `PublicCapabilityRegistry.ValidateRuntimeArguments` : une opération absente de `PublicRuntimeOperationIds` (y compris toute opération `internal.*`) renvoie `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN` ; un argument requis manquant ou vide renvoie `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Aucun des deux ne touche la session.
2. Recherche de la session (`KeyNotFoundException` pour un handle inconnu), `OL_E_RUNTIME_SESSION_MISMATCH` lorsque `RuntimeCommand.SessionId` diffère du handle, `OL_E_SESSION_NOT_RUNNING` sauf si l'état est `Running`.
3. Requête dans la boîte aux lettres (voir ci-dessous), puis `ScrubInternalValues` supprime toute clé de résultat commençant par `internal_` ou se terminant par `_address`, `_pointer`, `_vmt`.

Le client CLI et le plan de contrôle exécutent eux-mêmes l'étape 1 avant de contacter le propriétaire, de sorte qu'un nom de commande invalide est signalé par le code de sortie 2 même lorsqu'aucune session n'est active (`OL_E_RUNTIME_OPERATION_UNKNOWN` et `OL_E_RUNTIME_ARGUMENT_REQUIRED` correspondent à `InvalidArguments` ; les autres rejets à 7 ; l'absence de propriétaire à 4).

Le point de terminaison du plan de contrôle n'existe que tant que le propriétaire est `Running`, après le démarrage et après l'achèvement de tout harnais `/runtime-batch`, `/runtime-write-batch` ou `/d3d-batch`. Les commandes qui modifient l'état (`runtime.execute`, `session.stop`) doivent porter le `session_id` actif ; la CLI l'obtient automatiquement à partir de `session.status` (`OL_E_CONTROL_SESSION_MISMATCH` sinon).

<a id="request-identity-and-the-mailbox"></a>
## Identité de requête et boîte aux lettres

Un `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` est sérialisé dans une enveloppe `RuntimeCommandWire` (magic `OLRC`, version 1, en-tête de 72 octets, SHA-256 de la charge utile JSON) et déposé dans la boîte aux lettres de la session, de 64 KiB et à requête unique en vol (`OmsiLaunch.Runtime.<sessionId>`). Le plugin interroge la boîte aux lettres toutes les 50 ms sur le thread d'interface d'OMSI, y exécute l'opération et écrit la réponse.

Règles qui rendent le canal robuste :

| Règle | Effet |
| --- | --- |
| Requête unique en vol | Une seule requête à la fois par session ; l'hôte sérialise les appelants au moyen d'un sémaphore. Un slot encore à l'état `requested` à l'arrivée d'une nouvelle requête donne `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Liaison à la session | Le plugin ignore (et efface) une requête dont le GUID de session n'est pas le sien ; l'hôte rejette une réponse dont l'identifiant de session ou de requête ne correspond pas (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Lorsque l'échéance est dépassée, l'hôte remet le slot au repos et lève `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Réponse tardive | Le plugin ne publie une réponse que si le slot contient toujours l'identifiant de sa requête ; une réponse à une requête abandonnée est ignorée. Si l'une d'elles arrive malgré tout, la requête suivante de l'hôte trouve un slot `responded` périmé, l'écarte et poursuit ; si cette réponse périmée porte le **même** identifiant de requête que la nouvelle requête, l'hôte lève `OL_E_RUNTIME_REQUEST_ID_REUSED`. Les appelants ne doivent donc jamais réutiliser un identifiant de requête au sein d'une session. |
| Taille | Une requête plus grande que le slot est rejetée avant le dépôt (`ArgumentOutOfRangeException`). Une page de liste qui ne tient pas est raccourcie par le plugin (dernières lignes supprimées, `truncated=true`, `returned_count` réduit, `next_cursor` sur la première ligne supprimée) ; toute autre réponse trop grande est remplacée par une erreur typée `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Canal fermé | Après la fin de la session, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Les identifiants de requête sont des `ulong` choisis par l'appelant. La CLI utilise des plages fixes : `10_001` pour `/runtime:`, `50_001+` pour les requêtes transmises par le plan de contrôle, `1+` pour le lot de lecture, `20_000+` pour le lot D3D, `30_000+` pour `D3DRuntimeApi`. Les intégrateurs doivent utiliser un compteur strictement croissant par session.

<a id="handles-and-stale-detection"></a>
## Handles et détection des handles obsolètes

| Préfixe | Type | Émis par | Format |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + six chiffres décimaux (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + six chiffres décimaux |
| `d3dtex-` | Texture D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Les handles sont opaques et ont la portée d'une session : ils sont générés par le plugin, n'encodent jamais d'adresse et n'ont aucune signification dans une autre session. Lorsqu'un handle est résolu, le plugin vérifie que l'adresse à laquelle il correspond figure toujours dans la collection OMSI active (telle qu'à la dernière lecture de liste ; `road-vehicles.list` et `humans.list` rafraîchissent cette vue) et relit une empreinte d'objet (la VMT Delphi plus le pointeur de définition du véhicule, ou l'index de modèle de l'humain). Une différence signifie que l'objet natif a été détruit et son adresse réutilisée : `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Le même jeton est renvoyé à nouveau pour le même objet actif lors de lectures de liste répétées. Angle mort résiduel : un objet de la même classe et de la même définition recréé à la même adresse entre deux lectures de liste ne peut pas être distingué. Les handles D3D sont validés par le pont natif : un handle inconnu ou appartenant à une autre session donne `OL_E_D3D_STALE_RESOURCE_HANDLE`, un handle libéré `OL_E_D3D_RESOURCE_RELEASED`, et un changement de génération du périphérique marque les textures `STALE`.

Les handles ne sont jamais des adresses natives et ne doivent jamais être analysés, comparés numériquement, conservés d'une session à l'autre ni transmis à une autre session : traitez-les comme des chaînes opaques valides pour la session qui les a émis.

Preuves d'exécution : un handle D3D libéré reste rejeté après la création de nouvelles textures, et un handle d'une session précédente est rejeté par la suivante (clôture runtime `H02`) ; une réinitialisation du périphérique D3D invalide toutes les textures actives (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). La détection des handles obsolètes RoadVehicle et Human après suppression naturelle (RV-002) n'a pas de producteur runtime sûr (OMSI n'a supprimé aucun objet pendant les fenêtres d'observation et aucune opération publique n'en supprime) ; elle est couverte hors ligne.

<a id="what-is-not-journaled"></a>
## Ce qui n'est pas journalisé

Les modifications runtime ne changent que la mémoire d'OMSI. **Elles ne sont pas enregistrées dans le journal de transaction et ne sont pas restaurées** à la fin de la session : `time.set`, `camera.set`, `camera.lock` (la politique s'arrête lorsque le plugin s'arrête), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` et chaque ressource `d3d.texture.*`. Elles disparaissent avec le processus OMSI, qu'OmsiLaunch termine à la fin de la session sans laisser OMSI enregistrer quoi que ce soit (voir [transactions et récupération](../concepts/transactions-and-recovery.md)). Rien dans la famille runtime ne touche au système de fichiers.

<a id="argument-and-result-conventions"></a>
## Conventions d'arguments et de résultats

- Les arguments sont des paires clé/valeur de chaînes. Dans la CLI, `--key=value` placé après les mots de commande devient un argument runtime (`OmsiLaunch.exe vehicles get --handle=rv-000001`) ; `/runtime-arg:key=value` en est l'équivalent pour `/runtime:<op>`. Les nombres utilisent la culture invariante (séparateur décimal `.`) ; les booléens sont `true`/`false`.
- Les mots de commande correspondent aux identifiants d'opération via `CliInput.HierarchicalRoutes` (par exemple `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). La table complète des routes se trouve dans la [référence de la CLI](cli.md). Les opérations D3D n'ont pas de route par mots de commande ; utilisez `/runtime:d3d.status` ou `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Les résultats sont des dictionnaires de chaînes plats. Les listes utilisent des clés `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) avec les champs de page `count`, `returned_count`, `truncated` et `next_cursor` (voir [pagination](capabilities.md#pagination)).
- Les échecs portent `Succeeded=false`, un `ErrorCode` `OL_E_` et, pour les échecs côté plugin, `detail` (ainsi que `native_status` pour D3D et `exception` pour les erreurs inattendues).

<a id="cli-envelope---json"></a>
### Enveloppe CLI (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

Une enveloppe d'erreur a la forme `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Sans `--json`, la CLI affiche l'objet résultat sous forme de JSON indenté ou `CODE: message`.

<a id="api-envelope"></a>
### Enveloppe de l'API

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` lève une exception pour les violations de frontière survenant après la validation par le registre (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) et renvoie `Succeeded=false` pour les rejets du registre et les erreurs côté plugin.

<a id="worked-examples"></a>
## Exemples commentés

Tous les exemples CLI supposent qu'un propriétaire est en cours d'exécution pour l'installation qui contient `OmsiLaunch.exe` (démarré, par exemple, avec `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, ou avec `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` lorsqu'un exemple nécessite un véhicule du joueur) et sont exécutés depuis une seconde console dans la même installation.

| Famille | Commande | Ce qu'elle fait |
| --- | --- | --- |
| Session | `OmsiLaunch.exe session status --json` | Lit `SessionId`, `State`, les diagnostics et la liste bornée des événements runtime. |
| Heure | `OmsiLaunch.exe time get` puis `OmsiLaunch.exe time set --hour=7 --minute=30` | Lit l'horloge ; la règle via le `SetTime` profilé et renvoie la relecture. |
| Météo | `OmsiLaunch.exe weather get` et `OmsiLaunch.exe weather actual get` | Lit l'état météo courant et l'état météo réel/ICAO. `OmsiLaunch.exe weather set --wind_speed=5` définit une météo manuelle (enregistrement source d'OMSI ; le METAR en direct s'arrête) et `OmsiLaunch.exe weather metar set --icao=EDDF` passe à la météo METAR en direct. Les deux sont EXPERIMENTAL tant qu'elles ne sont pas validées à l'exécution. |
| Carte | `OmsiLaunch.exe map get` | Identité de la carte, nom, description, nombre de tuiles, plage d'années et côté de circulation. |
| Caméra | `OmsiLaunch.exe camera set --field_of_view=50` puis `OmsiLaunch.exe camera lock --family=0 --preset=1` et `OmsiLaunch.exe camera unlock` | Écrit le FOV ; fige la famille conducteur avec le préréglage 1 (nécessite un véhicule du joueur, par exemple une situation enregistrée) ; libère la politique. |
| Véhicules | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Nombres ; handles ; un instantané. |
| Apparition | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Crée un RoadVehicle IA (timeout de 30 s) ; renvoie `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` invoque `PlaceRandomBus`. |
| Joueur | `OmsiLaunch.exe player get` | `present=false` lors d'un démarrage headless, sinon l'instantané du joueur. |
| Humains | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Nombres ; handles ; un instantané. |
| Horaires | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Nombres du gestionnaire ; lignes de trajets bornées ; journaux d'horaires. |
| Scripts | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Liste/lecture/écriture de variables numériques ; lecture/écriture de variables chaîne ; triggers de script et de son (les écritures de chaînes et les triggers sont EXPERIMENTAL tant qu'ils ne sont pas validés à l'exécution). |
| Constantes et courbes | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Constantes du véhicule et évaluation de courbes. Les noms dépendent du modèle : utilisez les noms renvoyés par la commande `list` (ceux-ci ont été listés pour le bus du joueur de `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Métadonnées HOF de la définition du véhicule. |
| Conducteurs et billets | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Enregistrements des conducteurs ; pack de billets. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` puis `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` et `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Cycle de vie d'une texture sur le thread de rendu ; `<HANDLE>` est le `handle` affiché par `create` ; `--pixels_base64` doit se décoder en `width * height * 4` octets (formats 32 bits) et au plus 48 KiB. |
| Événements | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Liste bornée des événements ; surveillance par interrogation (250 ms) jusqu'à Ctrl+C. |
| Arrêt | `OmsiLaunch.exe session stop` | Demande l'arrêt canonique : OMSI est terminé et la transaction restaurée par le propriétaire. |

<a id="stability"></a>
## Stabilité

Le canal lui-même (`runtime.command-channel`) est `STABLE_BETA` : l'intégrité du format filaire, la liaison à la session, l'abandon des réponses tardives et le rejet typé des tailles excessives sont couverts hors ligne par `runtime-command.wire-guard`, `runtime-command.session-binding` et `runtime-command.late-response-ignored`, et à l'exécution par RV-003 (session `5f641c8d`), la série A RA-019 (un client a abandonné `road-vehicles.spawn` après 250 ms ; la requête suivante a réussi) et la batterie de tests de canal de la clôture runtime `R03` (timeout, annulation, identifiant de requête réutilisé, rejet par le plugin, opération interne, mauvaise session et argument manquant, chacun suivi d'une requête réussie). La stabilité de chaque opération est indiquée dans [capacités](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Réutilisation du canal après des échecs

Chaque issue terminale d'une requête runtime laisse la boîte aux lettres réutilisable : succès, erreur typée, réponse malformée, trop grande, d'une autre session ou avec un mauvais identifiant de requête, timeout, annulation par l'appelant et échecs de décodage se terminent tous par un même nettoyage qui remet le slot au repos et efface la longueur et l'en-tête de l'enveloppe, de sorte que rien d'une requête antérieure ne peut être lu par la suivante. Une requête ou une réponse résiduelle trouvée au début d'une nouvelle requête est d'abord effacée (`OL_E_RUNTIME_REQUEST_ID_REUSED` si elle porte l'identifiant de la nouvelle requête). Côté plugin, une exception dans une opération reçoit la réponse `OL_E_RUNTIME_OPERATION_FAILED`, un résultat plus grand que le slot `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (une enveloppe fixe et petite), une requête destinée à une autre session `OL_E_RUNTIME_SESSION_MISMATCH`, et une requête abandonnée par l'hôte ne reçoit jamais de réponse. Ces règles sont couvertes hors ligne (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`) ; les issues qu'un appelant peut provoquer (timeout, annulation, identifiant réutilisé, rejets typés, réponse tardive) disposent également de preuves d'exécution (RA-019, `R03`). Les réponses corrompues, d'une autre session ou trop grandes ne peuvent pas être produites depuis l'extérieur du produit et restent couvertes hors ligne uniquement.
