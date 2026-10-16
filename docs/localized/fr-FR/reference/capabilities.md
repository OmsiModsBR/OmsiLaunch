# Capacités

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Traduction de la [page originale en anglais](../../../reference/capabilities.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page documente ce que OmsiLaunch sait faire. Sa source est le catalogue canonique des capacités, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`) : chaque capacité, chaque opération runtime publique avec ses arguments, ses routes CLI et sa capacité native, ainsi qu'un état de preuve par capacité y sont déclarés une seule fois. L'ensemble des opérations runtime, la validation des arguments, les routes de commande CLI, le contrôle natif par opération du plugin, `GetCapabilitiesAsync` et le [tableau du catalogue](#capability-catalog) ci-dessous en sont dérivés. Pour chaque opération runtime, la page indique les arguments, les clés de résultat et les erreurs. La manière d'invoquer une opération (routes CLI, enveloppe d'API, timeouts, handles) figure dans le [contrôle runtime](runtime-control.md) ; les preuves par session figurent dans l'[état de la validation à l'exécution](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classification et stabilité

`PublicCapabilityClassification` possède quatre valeurs. Elles correspondent au vocabulaire de stabilité comme suit, avec des rétrogradations lorsque les preuves sont incomplètes :

| Classification | Signification | Stabilité |
| --- | --- | --- |
| `PublicStableBeta` | Publique, dans le contrat de la bêta, validée à l'exécution | `STABLE_BETA` (rétrogradée en `PARTIAL` là où c'est indiqué) |
| `PublicExperimental` | Publique, susceptible de changer, validée à l'exécution ou validée statiquement | `EXPERIMENTAL` (rétrogradée en `PARTIAL` là où c'est indiqué) |
| `InternalOnly` | Primitive de recherche ; inaccessible par l'API publique ou la CLI | `INTERNAL` |
| `Unsupported` | Reconnue à des fins de diagnostic, rejetée ou absente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` et `Event`. Chaque capacité runtime exige une session `Running` (`RequiresSession` dans le registre) et sa capacité native résolue et vérifiée sur le build en cours d'exécution ; une opération dont la capacité native est indisponible est rejetée avec `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` marque les capacités qui dépendent de cibles natives (voir [compatibilité](compatibility.md)).

Les résultats runtime sont des dictionnaires de chaînes. Les clés qui commencent par `internal_` ou se terminent par `_address`, `_pointer` ou `_vmt` sont supprimées à la frontière de l'API (`ScrubInternalValues`) et ne sont pas listées ici. Les clés de résultat des tableaux d'opérations ci-dessous sont les ensembles de clés exacts renvoyés par le produit : elles ont été capturées en exécutant chaque opération publique dans une session réelle (capture de documentation de la clôture runtime, session `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn` ; les lignes de liste sont notées `<row>.<n>.<field>`).

Manière dont l'échec d'une opération est signalé (`CurrentRuntimeControl.Execute`) :

| Échec | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rejet par le registre (opération inconnue ou `internal.*`, argument requis manquant) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | aucune |
| Rejet délibéré par le produit (une clé météo dérivée dans `weather.set`) | le code spécifique (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (phrase) |
| Échec D3D | le code `OL_E_D3D_*` spécifique | `detail`, `native_status` |
| Un échec typé côté plugin (handle obsolète, nom inconnu, valeur hors plage, aucun véhicule du joueur, ...) | le code spécifique, par exemple `OL_E_RUNTIME_OBJECT_HANDLE_STALE` ou `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (le message), `exception` (nom de type .NET) |
| Une opération dont la capacité native n'est pas résolue sur le build en cours d'exécution | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (la capacité native) |
| Une exception inattendue côté plugin, sans code | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Résultat qui ne tient pas dans la boîte aux lettres de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (les listes paginées sont raccourcies à la place et se poursuivent avec `next_cursor`, voir [pagination](#pagination)) | aucune |

La colonne `Errors` de chaque tableau nomme les codes spécifiques ; ils parviennent dans `ErrorCode` inchangés à travers le plugin, le pont, le plan de contrôle, l'API et la CLI.

<a id="capability-catalog"></a>
## Catalogue des capacités

Une ligne par entrée du catalogue, générée à partir de `PublicCapabilityRegistry.All` (le contrôle bloquant de documentation échoue lorsque ce tableau est obsolète). `Session` vaut `Running` pour les capacités qui exigent une session en cours d'exécution. Les `Operations` sont les identifiants concrets des opérations runtime (lignes de contrat dans [opérations runtime publiques](#public-runtime-operations)). `Native capability` nomme les entrées du catalogue des cibles natives (voir [compatibilité](compatibility.md)) qui doivent être résolues et vérifiées sur le build en cours d'exécution. Les `Evidence` utilisent un seul vocabulaire :

| Preuves | Signification |
| --- | --- |
| RUNTIME_VALIDATED | Observée dans une vraie session OMSI ; les identifiants de session figurent dans l'[état de la validation à l'exécution](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implémentée et vérifiée hors ligne ; l'effet à l'exécution n'a pas été observé. |
| PARTIAL | Fonctionne pour une partie de son contrat ; la note nomme la partie ouverte. |
| RUNTIME_VALIDATION_REQUIRED | Implémentée, en attente de sa validation à l'exécution. |
| UNAVAILABLE | Reconnue à des fins de diagnostic et rejetée, ou non implémentée ; la demander au démarrage rend le plan non exécutable. |

Une opération qui modifie quelque chose nomme sa politique de mutation après son identifiant (`PublicRuntimeOperation.MutationPolicy`) ; aucune opération runtime n'est journalisée :

| Politique de mutation | Signification |
| --- | --- |
| `none` | Lectures seules (non affichée). |
| `omsi-state` | Modifie l'état en mémoire d'OMSI ; rien ne l'annule ; cela prend fin avec le processus OMSI. |
| `session-resource` | Crée ou modifie une ressource détenue par la session, libérée explicitement ou à la fin de la session. |
| `session-policy` | Installe une politique que le plugin réapplique jusqu'à ce qu'elle soit retirée ou que la session se termine. |

<!-- generated:capability-catalog:begin -->
| Capacité | Classification | Type | Session | Opérations | Capacité native | Preuves | CLI | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | non |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | non |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | non |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4 : les événements du plugin transitent par un anneau numéroté ; un lecteur en retard reçoit telemetry.lost avec le nombre d'événements perdus. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | non |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | non |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | non |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4, série A : disposition connue. |
| `boot.headless-start` | `PublicStableBeta` | Action | non |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | non |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | non |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | non |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001) : OMSI présente le premier enregistrement de chaque libellé exact ; le démarrage le sélectionne par ce libellé. Fermée statiquement ; validation à l'exécution en attente. |
| `world.saved-situation` | `PublicStableBeta` | Action | non |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | non |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4 : la branche native est fermée statiquement (BI-006) ; validation à l'exécution en attente. Jamais d'heuristique du .osn le plus récent. |
| `world.explicit-date` | `PublicExperimental` | Action | non |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4 : écrite dans l'horloge de départ d'OMSI avant NewSituation (analyse statique) ; validation à l'exécution en attente. |
| `world.explicit-time` | `PublicExperimental` | Action | non |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4 : écrite dans l'horloge de départ d'OMSI avant NewSituation (analyse statique) ; validation à l'exécution en attente. |
| `world.explicit-year` | `PublicExperimental` | Action | non |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4 : écrite dans l'horloge de départ d'OMSI avant NewSituation (analyse statique) ; validation à l'exécution en attente. |
| `world.weather` | `PublicExperimental` | Action | non |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003) : appliquée juste après le démarrage du monde, par le loadWeather d'OMSI lui-même ou ses indicateurs METAR ; elle remplace la météo d'une situation enregistrée. Fermée statiquement ; validation à l'exécution en attente. |
| `player-vehicle.model` | `Unsupported` | Action | non |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007 ; résolu pour les diagnostics, non appliqué. |
| `player-vehicle.repaint` | `Unsupported` | Action | non |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | non |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | non |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | non |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false lors d'un démarrage headless ; l'instantané complet avec une situation enregistrée. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007 : la séquence native d'attribution est fermée statiquement (research/reports/beta4-static/entrypoint-last-player-vehicle.md section 3) mais n'est pas implémentée avant la validation à l'exécution ; prévue pour une bêta ultérieure. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | non |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | non |  |  | STATICALLY_VALIDATED |  | Beta 4 : overlay transactionnel, validé hors ligne. |
| `input.controller` | `PublicExperimental` | Write | non |  |  | STATICALLY_VALIDATED |  | Beta 4 : overlay transactionnel, validé hors ligne. |
| `presentation.splash` | `PublicStableBeta` | Write | non |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | non |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4, série B : la suppression est appliquée et signalée (internet-textures.suppressed) par l'anneau de télémétrie. |
| `internet-textures.override` | `PublicExperimental` | Write | non |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Clôture d'exécution I01. |
| `session-profiles` | `PublicStableBeta` | Action | non |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 démarrage de profil ; 1..99 et découverte hors ligne. |
| `hof.management` | `PublicStableBeta` | Write | non |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4, série A C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | non |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | non |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | non |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | non |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | non |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | non |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | non |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 fermé statiquement : SetActualDateTime se contente de copier l'horloge du PC dans l'horloge de départ ; les dates de départ utilisent world.explicit-date. Remplacé : l'ABI native et les postconditions ne sont pas fermées. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4 : écrit actWeather (Weather+0x08), l'enregistrement dont le tick météo d'OMSI dérive ses valeurs ; fermée statiquement. Les valeurs dérivées (humidité, facteurs de lumière, vent réel) restent en lecture seule (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Validation à l'exécution en attente. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Nécessite un PlayerVehicle (une situation enregistrée en fournit un) ; OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE sur un NEW_MAP headless. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | La détection des handles obsolètes après suppression naturelle (RV-002) n'a pas de producteur runtime sûr et est validée hors ligne. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paged) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paged), `timetable.trips.list` (paged), `timetable.lines.list` (paged), `timetable.rv-files.list` (paged), `timetable.track-entries.list` (paged), `timetable.bus-stops.list` (paged), `timetable.station-links.list` (paged), `timetable.tours.list` (paged), `timetable.profiles.list` (paged), `timetable.tour-entries.list` (paged), `timetable.logs.read` (paged), `timetable.no-rv-numbers.list` (paged) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paged), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paged), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Écritures : scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004) : OMSI copie une chaîne de forme constante ; fermée statiquement. Validation à l'exécution en attente. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004) : TriggerXML/SoundTrigger virtuels via les slots VMT profilés ; fermée statiquement. Validation à l'exécution en attente. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Réinitialisation du périphérique observée (resetting, restored, invalidation de génération) ; une transition lost distincte n'a pas été produite (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` est `InternalOnly` : elle existe dans le plugin à des fins de recherche (elle renvoie une adresse native) et elle est rejetée à la frontière de l'API ainsi que par le plan de contrôle local, qui valident tous deux d'abord le nom de l'opération par rapport à `PublicRuntimeOperationIds` ; la CLI n'a aucune route pour elle. `session.stop` termine OMSI ; l'arrêt coopératif n'est pas implémenté.

## Pagination

Chaque opération de liste (marquée « (paged) » dans le [catalogue des capacités](#capability-catalog)) accepte deux arguments facultatifs et répond avec quatre champs de page :

| Argument | Signification |
| --- | --- |
| `cursor` | Où la page commence : le `next_cursor` de la page précédente. Omis : la première page. Un curseur issu d'une autre liste, ou mal formé, donne `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Nombre de lignes souhaité (un entier positif). Supérieur à la page maximale de la liste : le maximum est utilisé. Pas un entier positif : `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Clé de résultat | Signification |
| --- | --- |
| `count` | Tous les enregistrements qu'OMSI détient. |
| `returned_count` | Lignes de cette réponse. |
| `truncated` | `true` lorsque des enregistrements suivent cette page. |
| `next_cursor` | Présent uniquement lorsque des enregistrements suivent : passez-le comme `cursor` pour la page suivante. |

Le curseur est une clé stable, pas une position. Pour les tableaux d'enregistrements (horaires, noms de scripts, constantes, courbes, HOF, conducteurs, billets, journaux), les lignes sont indexées `<row>.<n>.<field>` (ou `name.<n>`) par l'index absolu de l'enregistrement et le curseur nomme l'index suivant, de sorte que la première page est égale à la réponse de la Beta 3. Pour les collections de handles (`road-vehicles.list`, `humans.list`), les lignes sont renvoyées dans l'ordre des handles et le curseur nomme le dernier handle renvoyé : un objet qui apparaît entre deux pages reçoit un handle plus tardif et arrive sur une page ultérieure ; aucun n'est répété. Traitez les valeurs de curseur comme opaques.

Tailles de page maximales : 1000 pour `road-vehicles.list` et `humans.list` ; 512 pour `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` et `vehicle.curves.list` ; 256 pour `timetable.tour-entries.list`, `timetable.logs.read` et `vehicle.hofs.read` ; 128 pour les autres listes d'horaires, `drivers.read` et `tickets.read`. Lorsque les lignes d'une page dépassent encore la boîte aux lettres de 64 KiB, le plugin supprime les dernières lignes jusqu'à ce que la réponse tienne et `next_cursor` reprend à la première ligne supprimée ; aucune liste n'échoue donc à cause de sa taille.

<a id="public-runtime-operations"></a>
## Opérations runtime publiques

Voici la liste exacte des identifiants d'opération qu'un frontend public peut transmettre (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Chacune exige `SessionState.Running` ; aucune n'est journalisée ni restaurée. Les arguments requis sont contrôlés par le registre avant l'utilisation de la boîte aux lettres (`OL_E_RUNTIME_ARGUMENT_REQUIRED`) ; les arguments facultatifs sont validés par le plugin. Codes d'échec communs à toutes les opérations : `OL_E_RUNTIME_OPERATION_UNKNOWN` (absente de cette liste), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (le plugin n'a pas d'implémentation), `OL_E_RUNTIME_OPERATION_FAILED` (exception inattendue dans le processus ; valeurs `detail` et `exception`).

<a id="time"></a>
### Heure

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `time.read` | Read | aucun | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | `hour` (0..23), `minute` (0..59), `second` (0..59.999, décimal) facultatifs ; au moins un | les clés de `time.read` après le `SetTime` profilé | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Météo

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | aucun | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, ainsi que les valeurs sources `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, les facteurs de lumière et les humidités sont les valeurs par tick d'OMSI ; `cloud_type` est le numéro de type de nuage résolu | |
| `weather.set` | Write | au moins l'un de `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Plages : brouillard 0..100000, luminosité et valeurs du sol 0..1, vitesse du vent 0..200, direction du vent 0..360, température et point de rosée -60..60, pression 850..1100, précipitation 0..2, taux 0..255, hauteur des nuages 0..20000, `cloud_type` 1..64 caractères de la page de code 1252 (`-1` = pas de nuages), booléens `true`/`false`/`1`/`0` | les clés de `weather.read` ; avec `cloud_type`, aussi `cloud_type_resolved` (`false` lorsqu'OMSI ne connaît pas le type de nuage : il n'en dessine aucun) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (une clé dérivée : `relative_humidity`, `absolute_humidity`, les facteurs de lumière, `cloud_transparency`, `precipitation_set`, `wet_ground` ; le détail nomme la clé à définir), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (clé inconnue), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 lettres ou chiffres) | les clés de `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | aucun | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Carte

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `map.read` | Read | aucun | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Caméra

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | aucun | `family` (0 conducteur, 1 passager, 2 extérieure, 3 carte), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | `family` (0..3), `field_of_view` (10..170) facultatifs ; au moins un | les clés de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | `family` (0..3) requis ; `preset` (0..255, uniquement avec la famille 0 ou 1) facultatif | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. La politique est réappliquée toutes les 100 ms jusqu'à `camera.unlock` ; une réapplication en échec émet l'événement `camera.lock.degraded` une fois par erreur distincte. |
| `camera.unlock` | Action | aucun | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Véhicules routiers et joueur

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | aucun | `count`, `ai_collection`, `player_index` (l'index brut du véhicule du joueur dans OMSI, rapporté tel que lu ; utilisez `present` de `player-vehicle.read` pour savoir si un véhicule du joueur existe) | |
| `road-vehicles.list` | Read | `cursor`, `limit` facultatifs | champs de page ; `vehicle.<n>.handle` (`rv-NNNNNN` ; `<n>` est la position dans la liste d'OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | `handle` requis | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | aucun | `present` (`false` lorsque OMSI n'a pas de véhicule du joueur) ; lorsque `true` : `player_index` plus chaque clé de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | `model` requis (`Vehicles\...\*.bus` canonique, au plus 240 caractères, sans `..`, doit exister sous l'installation) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. N'attribue pas le PlayerVehicle. Prend plusieurs secondes ; utilisez le timeout client de 30 s. |
| `road-vehicles.place-random` | Action | `ai_type` (0..255, par défaut 0), `group` (0..65535, par défaut 1), `type` (-1..65535, par défaut -1), `scheduled` (0..1, par défaut 0), `tour` (0..65535, par défaut 0), `line` (0..65535, par défaut 0) facultatifs | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humains

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | aucun | `count` | |
| `humans.list` | Read | `cursor`, `limit` facultatifs | champs de page ; `human.<n>.handle` (`hb-NNNNNN` ; `<n>` est la position dans la liste d'OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | `handle` requis | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Horaires, conducteurs, billets

Les listes d'horaires, `drivers.read` et `tickets.read` sont paginées (voir [pagination](#pagination)) ; « champs de page » ci-dessous désigne `count`, `returned_count`, `truncated` et `next_cursor`, et chaque liste accepte les `cursor` et `limit` facultatifs.

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | aucun | `invalid` (`true`/`false`), ainsi que les nombres d'enregistrements `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | aucun | `count`, `returned_count`, `truncated` ; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | `cursor`, `limit` facultatifs | champs de page ; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | `cursor`, `limit` facultatifs | champs de page ; `no_rv.<n>.group`, `no_rv.<n>.number` : les numéros de véhicule qu'OMSI exclut de l'utilisation libre par l'IA (la liste de chaînes de numéros réservés d'OMSI, lue via sa propre disposition ; `OL_E_RUNTIME_VALUE_INVALID` lorsque la liste n'a pas la forme attendue). Au plus 256 par page. Analyse statique ; validation à l'exécution en attente. |
| `map.tiles.list` | Read | `cursor`, `limit` facultatifs | champs de page ; `tile.<n>.x`, `tile.<n>.y` (position dans la grille), `tile.<n>.tile_index` (l'index propre de la tuile dans la liste des tuiles de la carte), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments` ; `tile.<n>.state` = `unavailable` avec `tile.<n>.error` pour une tuile qui ne peut pas être lue. Au plus 256 par page. Position et index issus de l'analyse statique ; validation à l'exécution en attente. |
| `drivers.read` | Read | `cursor`, `limit` facultatifs | `count`, `selected_index` ; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | `cursor`, `limit` facultatifs | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count` ; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts, constantes, courbes et HOF du véhicule (portée d'un handle)

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | `handle` requis ; `cursor`, `limit` facultatifs | `handle`, champs de page, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | `handle`, `name` requis | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | `handle`, `name`, `value` (flottant fini) requis | `handle`, `name`, `requested_value`, `value` (relu) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | `handle` requis ; `cursor`, `limit` facultatifs | `handle`, champs de page, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | `handle`, `name` requis | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | `handle`, `name` (sensible à la casse, tel que le script le déclare, jusqu'à 128 caractères), `value` (1..1024 caractères) requis | `handle`, `name`, `value` relu après l'écriture | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (OMSI lui-même ignorerait silencieusement un nom inconnu), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | `handle`, `name` (page de code 1252, jusqu'à 128 caractères), `value` (`1` exécute `name`, `0` exécute `name_off`, `2` exécute `name_drag`) requis | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. L'entrée de script s'exécute de façon synchrone ; un nom que le script ne définit pas ne fait rien. Un appui est normalement suivi d'un relâchement |
| `vehicle.sound-trigger` | Action | `handle`, `trigger` (page de code 1252, jusqu'à 128 caractères) requis ; `file` facultatif (jusqu'à 260 caractères) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (aussi : le véhicule n'a pas de pack de sons), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | `handle` requis ; `cursor`, `limit` facultatifs | `handle`, champs de page, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | `handle`, `name` requis | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | `handle` requis ; `cursor`, `limit` facultatifs | `handle`, champs de page, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | `handle`, `name`, `x` (flottant fini ; un `x` manquant ou non numérique donne `OL_E_RUNTIME_ARGUMENT_REQUIRED`) requis | `handle`, `name`, `x`, `value` (interpolation linéaire) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | `handle` requis ; `cursor`, `limit` facultatifs | `handle`, champs de page, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Les opérations D3D s'exécutent sur le thread principal/de rendu d'OMSI par l'intermédiaire du pont natif. Les handles de texture ont la forme `d3dtex-<sessionId N>-<16 hex digits>`.

| Opération | Type | Arguments | Clés de résultat | Erreurs |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | aucun | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`) requis ; `levels` (0..16, par défaut 1) facultatif | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | `handle` requis ; `level` (0..15, par défaut 0) facultatif | les 13 clés de `d3d.texture.create`, pour le niveau demandé | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, plus les erreurs de create |
| `d3d.texture.update` | Action | `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (au plus 48 KiB une fois décodé) requis ; `level` (0..15, par défaut 0), `x` (0..4095, par défaut 0), `y` (0..4095, par défaut 0) facultatifs | les 13 clés de `d3d.texture.create` après la mise à jour | `OL_E_D3D_INVALID_PIXEL_BUFFER`, plus les erreurs de describe |
| `d3d.texture.release` | Action | `handle` requis | les 13 clés de `d3d.texture.create` avec `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` en cas de libération répétée, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Les opérations D3D en échec renvoient les valeurs `detail` et `native_status`. Les méthodes d'extension de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulent ces opérations pour les consommateurs de l'API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` renvoie un `Capability(Name, Available, EvidenceState, Reason)` par entrée du catalogue qui n'est pas `InternalOnly`. `Name` est l'identifiant du catalogue et `EvidenceState` sa preuve de catalogue. `Available` est évalué pour l'installation :

- Les entrées `Unsupported` ne sont jamais disponibles ; `Reason` porte la note du catalogue.
- Chaque entrée exige la plateforme actuelle, et chaque entrée sauf `runtime.current-windows-x64` exige une installation d'OMSI 2.
- Une entrée avec des capacités natives sur la disposition connue rapporte `native: RESOLVED_KNOWN_LAYOUT`. Sur un autre build d'OMSI 2, le fichier exécutable est résolu statiquement : `native: STATICALLY_RESOLVED`, ou `native: PENDING_IN_PROCESS_RESOLUTION` en nommant ce que le fichier n'a pas résolu. Une entrée en attente est tout de même rapportée comme disponible, car la résolution dans le processus tranche ; si elle échoue, l'opération ou le démarrage échoue avec `OL_E_CAPABILITY_UNRESOLVED`.

Le planificateur rapporte des capacités propres à chaque plan dans `SessionPlan.RequiredCapabilities` et `SessionPlan.UnsupportedRequestedFeatures` avec les mêmes identifiants de catalogue (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Une fonctionnalité demandée mais non prise en charge porte sa preuve de catalogue ; une capacité native requise porte `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Limitations connues

- Les handles ont la portée d'une session et sont signalés par `OL_E_RUNTIME_OBJECT_HANDLE_STALE` dès que leur objet a disparu. Un handle de véhicule est lié à sa VMT, à sa définition et à l'`IDCode` d'OMSI (un numéro unique par processus que chaque objet de la carte reçoit à la construction et qui n'est jamais réutilisé), de sorte qu'un objet recréé à la même adresse est toujours distingué. Les humains sont des slots mis en pool qui vivent pendant toute la session de la carte : un handle d'humain cesse de se résoudre lorsque l'humain disparaît (`RenderMe` 0) ou que le slot est de nouveau utilisé (nouvelle station cible ou nouveau départ).
- Les résultats sont bornés à la boîte aux lettres de 64 KiB ; chaque liste est paginée (voir [pagination](#pagination)).
- Pas de déplacement de véhicule d'une tuile à une autre, pas d'écriture du calendrier, pas d'attribution headless du PlayerVehicle. Les écritures de variables chaîne, les déclencheurs de script et les écritures de la météo sont `EXPERIMENTAL` (validation à l'exécution en attente). Voir [limitations connues](known-limitations.md).
