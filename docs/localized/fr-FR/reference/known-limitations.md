# Limitations connues

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> Traduction de la [page originale en anglais](../../../reference/known-limitations.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page recense, à partir du code, tout ce qui, dans OmsiLaunch 0.1.0-beta4, est `UNAVAILABLE`, `PARTIAL` ou constitue un risque accepté, afin que les utilisateurs et les intégrateurs ne s'appuient pas sur un comportement que le produit ne fournit pas. Chaque ligne nomme la limitation, sa stabilité, sa raison d'être et l'endroit où elle est documentée en détail. La documentation anglaise est normative ; les copies localisées sous `docs/localized/` ne sont pas maintenues au même niveau et peuvent être en retard (voir la dernière section).

<a id="compatibility"></a>
## Compatibilité

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Les capacités qui lisent des dispositions d'objets (véhicules, humains, météo, carte, caméra, horaires, scripts, conducteurs, billets) ne sont vérifiées que sur une disposition connue (`Omsi23004`) ; sur un autre build d'OMSI 2, elles sont indisponibles (`OL_E_CAPABILITY_UNRESOLVED`) tant que des validateurs structurels n'existent pas | `PARTIAL` sur les builds inconnus | [compatibilité](compatibility.md) |
| L'exécutable Steam authentique (protégé par DRM) n'a pas été validé à l'exécution ; une copie LAA contrôlée l'a été | `PARTIAL` pour Steam | [compatibilité](compatibility.md) |
| Seule la disposition connue a été validée à l'exécution ; la résolution indépendante du build est vérifiée hors ligne par rapport à l'exécutable de référence dont l'identité est masquée | `EXPERIMENTAL` sur les builds inconnus | [état de la validation à l'exécution](../status/runtime-validation-status.md) |
| Windows 10+ x64 uniquement ; pas de Windows 7/8, pas de XP, pas d'ARM64 | `UNAVAILABLE` | [compatibilité](compatibility.md) |
| Le plugin nécessite le runtime .NET 6 **x86** en plus du runtime x64 utilisé par le contrôleur | | [compatibilité](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Démarrage du monde et options de lancement

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) est implémenté à partir de l'analyse statique de la branche « dernier état » d'OMSI et n'est validé que hors ligne | `EXPERIMENTAL` | [CLI](cli.md) |
| La date, l'heure et l'année explicites ou système (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` du profil) ne s'appliquent qu'aux démarrages NEW_MAP et sont validées hors ligne ; la validation à l'exécution est en attente. Le 31 décembre d'une année bissextile ne peut pas être demandé (le calendrier d'OMSI boucle sur le jour 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Le préréglage météo, l'ICAO et la météo réelle actuelle au démarrage (`/weather*`, `new.weather`) sont appliqués juste après le démarrage du monde via le chargement météo propre à OMSI et ses indicateurs METAR (fermé statiquement, BI-003) ; la validation à l'exécution est en attente. La météo qu'OMSI affiche à la toute première image peut encore être celle par défaut de la carte | `EXPERIMENTAL` | [CLI](cli.md) |
| Le modèle, la livrée, le HOF, le numéro de flotte et l'immatriculation du véhicule du joueur au démarrage (famille `/vehicle`, `PlayerVehicleSpec`) sont résolus par rapport au catalogue de contenu mais non appliqués ; les demander rend le plan non exécutable. La séquence native d'attribution (placement via la boîte de dialogue de position d'OMSI, `MakeVehicle`, les étapes de finalisation par unité, `CopyTempListIntoMainList`, l'index du joueur) est fermée statiquement, mais elle crée et enregistre un véhicule actif et n'est pas implémentée tant qu'elle ne peut pas être validée à l'exécution ; elle est prévue pour une bêta ultérieure. Une situation enregistrée ou `/last` restaure son propre véhicule du joueur | `UNAVAILABLE` (BI-007) | `player.assign-headless` dans [capacités](capabilities.md) |
| Le point d'entrée par identité (`/entrypoint:<identity>`) sélectionne le libellé exact qu'OMSI présente (fermé statiquement, BI-001) ; un enregistrement ultérieur qui partage un libellé ne peut pas être sélectionné, comme dans la boîte de dialogue propre à OMSI. Validation à l'exécution en attente | `EXPERIMENTAL` | capacité de plan `world.entrypoint-identity` |
| Les paramètres de clavier et de manette par clé (`Environment.Keyboard`, `Environment.Controllers`) n'ont aucune clé cataloguée : des fichiers entiers sont remplacés avec `InputSpec` à la place | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| L'arrêt coopératif (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) est opt-in et n'est validé que hors ligne ; par défaut OMSI est terminé à la fin d'une session et `ShutdownTimeoutSeconds` n'a aucun effet | `EXPERIMENTAL` | [CLI](cli.md) |
| Les options de diagnostic `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` alimentent `DiagnosticsSpec` sans autre effet (la trace de l'hôte sous `.omsilaunch\diagnostics` est toujours écrite) ; `/trace-plugin` et `/trace-native` y ajoutent des événements de trace du plugin et de la couche native | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` sont des harnais de validation | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fin de session et contrôle des processus

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| L'arrêt de session est un arrêt forcé : `session.stop`, « End session » (`Terminer la session`) dans la zone de notification, Ctrl+C et `CloseAsync` aboutissent tous à `TerminateProcess`. La routine d'arrêt d'OMSI ne s'exécute pas, OMSI ne réécrit ni `options.cfg` ni ses journaux à la sortie, et tout état OMSI non enregistré est perdu. C'est délibéré : cela empêche OMSI d'écraser les fichiers restaurés. | par conception | [cycle de vie de la session](../concepts/session-lifecycle.md) |
| L'arrêt coopératif par WM_CLOSE avec un timeout et un repli sur la terminaison n'est pas implémenté | `UNAVAILABLE` (décision produit, S-11 ; OMSI a ignoré `WM_CLOSE` envoyé à sa fenêtre principale lors de la campagne de clôture runtime) | [état de la validation à l'exécution](../status/runtime-validation-status.md) |
| À la fermeture de la console ou à la déconnexion, le propriétaire dispose d'un budget de 4 s pour arrêter et restaurer ; ce qui reste est récupéré grâce au journal au démarrage suivant | fermeture de la console validée à l'exécution ; déconnexion non testée | [transactions et récupération](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transaction, récupération et bail

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Le bail d'installation est un sémaphore `Local\` : un propriétaire par installation **par session de connexion Windows** ; non appliqué entre utilisateurs ; non libéré tant qu'un autre processus détient un handle ; tout processus du même utilisateur peut détenir le nom | risque accepté (S-18) | [transactions et récupération](../concepts/transactions-and-recovery.md) |
| La récupération est refusée (`OL_E_INSTALLATION_BUSY`) tant que le processus OMSI journalisé ou, pour un journal sans PID, tout `Omsi.exe` lancé depuis cette racine est en cours d'exécution | par conception | idem |
| Un chemin d'overlay initialement absent dont le contenu a changé pendant la session bloque la restauration (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) jusqu'à inspection | par conception | idem |
| Les journaux antérieurs aux empreintes de propriété ne peuvent être clos que par une session aux octets planifiés identiques (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | idem |
| Seuls les chemins appartenant à la session sont restaurés. Les écritures propres d'OMSI pendant une session (`options.cfg` `[last_map]` lorsqu'aucun paramètre ne superpose d'overlay à `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, profil de conducteur, journaux) persistent, comme après un démarrage direct d'OMSI | par conception | [transactions et récupération](../concepts/transactions-and-recovery.md) |
| La suppression d'un `closecheck` obsolète avant une session est définitive (enregistrée, non restaurée) lorsque `SuppressStaleClosecheckWarning` vaut true | par conception | idem |

<a id="runtime-control"></a>
## Contrôle runtime

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| `weather.set` et `weather.metar.set` écrivent l'enregistrement météo source d'OMSI (fermé statiquement) ; la persistance au fil des ticks météo, la résolution du type de nuage et le téléchargement METAR attendent la validation à l'exécution. Les valeurs dérivées (humidité, facteurs de lumière, vent réel) ne peuvent pas être définies ; tant qu'OMSI est en pause, elles ne se mettent à jour qu'après la reprise | `EXPERIMENTAL` | [capacités](capabilities.md) |
| `SetActualDateTime` se contente de copier l'horloge du PC dans l'horloge de départ d'OMSI (analyse statique) ; OmsiLaunch ne l'appelle pas et définit lui-même l'horloge de départ | par conception | `calendar.set-actual-date-time` |
| Les écritures de variables chaîne, les déclencheurs nommés et les déclencheurs sonores passent par les méthodes propres à OMSI (fermé statiquement, BI-004) ; leur effet sur les scripts de véhicule attend la validation à l'exécution. Seuls les véhicules routiers sont des cibles | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Pas de relocalisation de véhicule, pas de réassociation spatiale entre tuiles, pas d'autorité de transformation sûre vis-à-vis d'ODE ; les champs de position sont en lecture seule | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` nécessitent un PlayerVehicle ; le démarrage headless NEW_MAP n'en a pas (une situation enregistrée en fournit un) | par conception (BI-007) | RV-004 |
| Les mutations runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, textures D3D) ne sont ni journalisées ni restaurées | par conception | [contrôle runtime](runtime-control.md) |
| L'obsolescence d'un handle est décidée à partir du numéro d'instance jamais réutilisé d'OMSI (`IDCode`) et, pour les humains, de la disparition et de la réapparition ; elle est validée hors ligne. La suppression naturelle (RV-002 : voitures IA quittant le réseau de chemins chargé) reste à observer à l'exécution, et aucun événement `entity.removed` n'est encore émis | `PARTIAL` | [contrôle runtime](runtime-control.md) |
| Les résultats sont bornés par la boîte aux lettres de 64 KiB : les longues listes sont tronquées (`truncated=true`) ; les données de pixels sont limitées à 48 KiB par `d3d.texture.update` | par conception | [capacités](capabilities.md) |
| Canal à requête unique : une requête à la fois par session ; un slot occupé donne `OL_E_RUNTIME_CHANNEL_BUSY` ; les identifiants de requête ne doivent pas être réutilisés | par conception | [contrôle runtime](runtime-control.md) |
| La télémétrie est un anneau de 64 événements : un hôte qui prend plus de 64 événements de retard perd les plus anciens et est informé de leur nombre (`telemetry.lost`) | `EXPERIMENTAL` | [plugin permanent](../concepts/permanent-plugin.md) |
| Les hooks de périphérique Direct3D d'un module sont des hooks de vtable : sur une installation où un autre composant réécrit les méthodes d'origine du périphérique (observé sur l'installation de régression peu après le début du gameplay), ils cessent d'être appelés et ne peuvent pas être retirés au nettoyage (`OL_E_MODULE_CLEANUP_INCOMPLETE` ; le composant reste chargé) | `PARTIAL` | [modules runtime](runtime-modules.md#native-components-and-hooks) |
| Le chargement des modules runtime est ouvert : OmsiLaunch ne vérifie pas qui a fait un module ; il garantit seulement que les modules ne s'exécutent jamais dans le processus OmsiLaunch et qu'un programme de session est confiné à sa tâche (pas de second processus, terminé avec la session) | par conception (E21) | [modules runtime](runtime-modules.md#security-boundary) |
| Le protocole 2 des modules (paramètres, activation par défaut, fichier de paramètres, programmes de session, conflits de plugins, notices, `get_value`) n'est validé que hors ligne | `EXPERIMENTAL` | [modules runtime](runtime-modules.md) |
| La réinitialisation du périphérique D3D a été observée à l'exécution (`resetting`, `restored`, invalidation de génération) ; aucune transition `lost` distincte n'a été produite, car le périphérique d'OMSI est passé directement à `DEVICENOTRESET` | `PARTIAL` (RV-007) | [état de la validation à l'exécution](../status/runtime-validation-status.md) |
| Les listes runtime sont paginées (`cursor`, `limit`, `next_cursor`) : une page contient au plus le nombre maximal de lignes de la liste et ce qui tient dans le slot runtime de 64 KiB | `EXPERIMENTAL` | [capacités](capabilities.md#pagination) |
| Certains champs avancés du graphe de carte/tuiles/chemins/objets ne sont pas exposés ; les lecteurs runtime sont des instantanés typés conditionnés par le profil, jamais un accès mémoire arbitraire | par conception | [capacités](capabilities.md) |
| Les lectures mémoire dans le processus suivent le schéma vérification puis utilisation sur un OMSI actif ; une mutation concurrente d'OMSI entre la vérification et la lecture peut produire un instantané incohérent (`OL_E_RUNTIME_OPERATION_FAILED`) | risque accepté (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plan de contrôle local et modèle de confiance

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Modèle de confiance « même utilisateur » : le canal nommé (named pipe, `CurrentUserOnly`), les mappages mémoire de handoff, de télémétrie et runtime, ainsi que le sémaphore du bail sont accessibles à tout processus du même utilisateur Windows. Un tel processus peut lire l'état, arrêter la session ou exécuter des opérations runtime dès qu'il a lu le `session_id`. | risque accepté (S-06, S-30) | [contrôle local](local-control.md) |
| Le point de terminaison de contrôle n'existe que tant que le propriétaire est `Running` ; un client obtient `OL_E_NO_ACTIVE_SESSION` (sortie 4) pendant le démarrage et après la fin de la session | par conception | [contrôle local](local-control.md) |
| Si un autre processus possède déjà le nom du pipe, le propriétaire continue de s'exécuter sans point de terminaison (`ListenFault`), et un second lancement peut signaler à tort `OL_E_SESSION_ALREADY_ACTIVE` | risque accepté | [contrôle local](local-control.md) |
| `.omsilaunch\` hérite de l'ACL de la racine OMSI ; aucun contrôle d'accès explicite n'est appliqué | risque accepté (S-31) | [transactions et récupération](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostics et sortie

| Limitation | Stabilité | Détail |
| --- | --- | --- |
| Les diagnostics sont uniquement des fichiers locaux (`.omsilaunch\diagnostics`) ; rien n'est téléversé et il n'existe aucun signalement à distance | par conception | [transactions et récupération](../concepts/transactions-and-recovery.md) |
| La rétention conserve les 50 sessions les plus récentes ; les diagnostics plus anciens préfixés par session sont supprimés au démarrage d'une nouvelle session | par conception | idem |
| La sortie JSON et les diagnostics incluent les chemins d'installation (`RootPath`, répertoires de ressources, chemins `.itx`) | par conception (données locales) | |
| La boîte de dialogue d'échec de session de `OmsiLaunchW.exe` affiche comme message la charge utile d'échec du plugin (par exemple `{"name":"world.failed",...}`) plutôt qu'une phrase ; la ligne `Code:` est correcte | cosmétique | [zone de notification Windows](windows-tray.md) |
| Une requête D3D rejetée par le pont natif avant tout appel Direct3D signale `native_status` correctement, mais son texte `detail` indique `HRESULT 0x00000000` | cosmétique | [capacités](capabilities.md) |
| La fenêtre d'état de la zone de notification est un instantané de la session planifiée pris à son ouverture ; elle ne se rafraîchit pas et n'affiche aucune valeur OMSI en direct | par conception | [zone de notification Windows](windows-tray.md) |

## Documentation

Les pages anglaises sous `docs/` constituent la documentation normative de cette version. `docs/localized/<locale>/` contient des traductions des mêmes pages `0.1.0-beta4` (voir [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)) ; lorsqu'une traduction diffère du texte anglais, le texte anglais et le code font foi. Les pages historiques et héritées qui y sont listées ne sont disponibles qu'en anglais.

Voir aussi : [capacités](capabilities.md), [état de la validation à l'exécution](../status/runtime-validation-status.md), [erreurs](errors.md).
