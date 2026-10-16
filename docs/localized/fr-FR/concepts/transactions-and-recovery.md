# Transactions et récupération

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Traduction de la [page originale en anglais](../../../concepts/transactions-and-recovery.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Chaque session OmsiLaunch qui touche un fichier OMSI le fait à l'intérieur d'une transaction durable et journalisée : les octets originaux sont sauvegardés avant d'être remplacés, le journal enregistre jusqu'où la session est allée, et la restauration vérifie chaque sauvegarde avant de la réécrire. Cette page décrit cette transaction telle qu'elle est implémentée par `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) et pilotée par `OmsiLaunchService.StartAsync`, `SuperviseAsync` et `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), avec les fichiers d'entrée calculés par `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Elle s'adresse aux utilisateurs qui ont besoin de savoir ce qu'une session modifie et ce que fait la récupération, ainsi qu'aux intégrateurs qui ont besoin des garanties exactes.

<a id="what-a-session-changes"></a>
## Ce qu'une session modifie

Seuls les **overlays temporaires** entrent dans la transaction. Ils sont calculés avant l'ouverture de la transaction et restaurés à sa fermeture.

| Entrée de session | Fichier(s) | Type |
| --- | --- | --- |
| `/set:<key>=<value>`, `settings` du profil, `LaunchSpec.Environment.*` | `options.cfg` (correctifs sémantiques de jetons ; octets CP1252 préservés, UTF-8/UTF-16 marqués d'un BOM respectés) | overlay |
| Écran de démarrage géré (`SplashMode.Managed`, la valeur par défaut) | `GUI\NewSplashscreen_ENG.bmp` et `GUI\NewSplashscreen_<language>.bmp` | overlay (le fichier localisé est créé par la transaction lorsque l'installation n'en a pas) |
| Textures Internet `Override` | `Texture\standard.itx` | overlay |
| Textures Internet `Override` | chaque cible listée dans le `.itx`, plus `Texture\standard.ipr` | suppression de session |
| Toujours | `closecheck` (lorsqu'il est absent avant la session) | suppression de session |

Les fichiers permanents du produit ne **participent pas** à la transaction : la closure du plugin sous `plugins\OmsiLaunch.*` (uniquement validée, voir [plugin permanent](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (copiés une seule fois, jamais supprimés), les diagnostics sous `.omsilaunch\diagnostics`, les paquets de profils de session, ainsi que la documentation et les exemples de la version. Les plugins tiers et tous les autres fichiers OMSI ne sont jamais énumérés, copiés, supprimés ni restaurés.

OMSI continue lui-même d'écrire son propre état pendant qu'une session s'exécute, exactement comme lors d'un démarrage normal d'OMSI : `options.cfg` (par exemple `[last_map]` lorsque la session charge une autre carte, réécrit à l'entrée dans le jeu), `Texture\standard.ipr`, les caches d'horaires et de lightmaps (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, le profil de conducteur sous `Drivers\`, et ses journaux. Une écriture sur un chemin appartenant à la session (ci-dessus) est annulée par la restauration ; toute autre écriture d'OMSI persiste après la session, exactement comme après une exécution directe d'OMSI. Preuves d'exécution de la série de clôture : une session de situation enregistrée sur une autre carte a laissé `[last_map]` modifié parce qu'elle n'appliquait pas d'overlay à `options.cfg` (`CAM01`), tandis que les sessions `/set` ont restauré `options.cfg` à l'identique (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## États de la transaction

`TransactionState` est persisté dans le journal après chaque transition. Les valeurs sont sérialisées sous forme d'entiers par `System.Text.Json`.

| Valeur | État | Écrit lorsque |
| --- | --- | --- |
| 0 | `Prepared` | Les instantanés de chaque chemin d'overlay et de suppression ont été pris et leurs sauvegardes vidées sur le disque. Rien n'a encore changé dans l'installation. C'est l'obligation de récupération : à partir d'ici, un plantage laisse un journal récupérable. |
| 1 | `Applied` | Chaque overlay a été écrit de manière atomique et chaque suppression effectuée. |
| 2 | `RuntimeDeployed` | L'intégrité du plugin permanent a été validée pour ce démarrage (rien n'est déployé ; le nom est historique). |
| 3 | `HandoffCreated` | Le handoff de démarrage, le slot de télémétrie et la boîte aux lettres runtime existent sous forme de mémoire partagée nommée. |
| 4 | `ProcessStarted` | `Omsi.exe` a été créé. Le journal contient désormais aussi `ProcessId`, `ProcessStartFileTimeUtc` (heure de création, ticks UTC) et `ExecutablePath`. |
| 5 | `ProcessExited` | Le superviseur a confirmé la fin du processus (fin naturelle ou `TerminateProcess`). |
| 6 | `Restoring` | La restauration a commencé. |
| 7 | `Restored` | Chaque fichier possédé a été restauré et vérifié. Immédiatement après, le journal est supprimé et `backup\<session>` retiré. |
| 8 | `Completed` | Déclaré dans l'enum mais jamais persisté ; une transaction terminée n'a pas de journal. |

Le cycle de vie d'une session normale est donc : instantané -> `Prepared` -> overlays écrits / suppressions effectuées -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal supprimé -> `backup\<session>` retiré. Les valeurs publiques de `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` et `Completed` suivent la même progression vue de l'extérieur (voir [cycle de vie de la session](session-lifecycle.md)).

Une session sans aucune modification de fichier possédé écrit tout de même un journal pour son cycle de vie ; sa restauration est une opération vide vérifiée.

<a id="journal-file"></a>
## Fichier journal

Chemin : `<root>\.omsilaunch\journal.json`. Il existe au plus un journal par installation ; sa présence signifie « une transaction est en attente ».

Champs de `TransactionJournal` :

| Champ | Type | Signification |
| --- | --- | --- |
| `SessionId` | GUID | Session propriétaire du journal ; également le nom du répertoire de sauvegarde (format `N`). |
| `State` | entier | `TransactionState` ci-dessus. |
| `Files` | tableau de `JournalFile` | Une entrée par chemin possédé. |
| `ProcessId` | entier ou null | PID d'OMSI, à partir de `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long ou null | Heure de création d'OMSI (ticks UTC), à partir de `ProcessStarted`. |
| `ExecutablePath` | chaîne ou null | Chemin complet du `Omsi.exe` lancé, à partir de `ProcessStarted`. |

Champs de `JournalFile` :

| Champ | Type | Signification |
| --- | --- | --- |
| `RelativePath` | chaîne | Chemin relatif à la racine de l'installation (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Indique si le fichier existait avant la session. |
| `Sha256` | chaîne hexadécimale | SHA-256 des octets originaux (d'un tableau d'octets vide lorsque `Existed` est faux). |
| `BackupPath` | chaîne | Chemin absolu de la copie de sauvegarde (écrit uniquement lorsque `Existed`). |
| `AppliedSha256` | chaîne hexadécimale ou null | SHA-256 des octets d'overlay que la session a écrits sur ce chemin ; null pour les suppressions de session. C'est l'empreinte de propriété des fichiers initialement absents. |
| `LastWriteTimeUtcTicks` | long ou null | Heure de dernière écriture d'origine. |
| `CreationTimeUtcTicks` | long ou null | Heure de création d'origine. |
| `Attributes` | entier ou null | `FileAttributes` d'origine (y compris `ReadOnly`). |
| `SessionDeletion` | bool | Vrai pour les chemins que la session a demandé de maintenir absents (cibles `.itx`, `Texture\standard.ipr`, `closecheck`). |

Les journaux écrits par des builds antérieurs sans `AppliedSha256` ni les champs de métadonnées restent lisibles ; voir [Fichiers initialement absents](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Organisation des sauvegardes

| Chemin | Contenu |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Un répertoire par session, créé avec le journal `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Octets originaux exacts d'un fichier possédé existant. Les fichiers initialement absents n'ont pas de sauvegarde. |

Les sauvegardes et le journal sont écrits via un fichier temporaire (`<path>.omsilaunch.tmp`), en écriture directe (write-through) plus un `Flush(true)` explicite, puis un `File.Move` atomique avec écrasement. Le fichier temporaire est toujours supprimé, même en cas d'échec. Le même chemin d'écriture est utilisé pour les overlays et pour les originaux restaurés, de sorte qu'aucun fichier `*.omsilaunch.tmp` ne survit à une opération terminée.

Les sauvegardes ne sont supprimées qu'après la suppression du journal qui les référençait. Un échec de suppression de `backup\<session>` est purement cosmétique et n'annule jamais une restauration vérifiée.

<a id="restore"></a>
## Restauration

`RestoreAsync` s'exécute après `ProcessExited` (ou pendant la récupération). Pour chaque chemin journalisé :

| État d'origine | Action |
| --- | --- |
| Existait | Le hash des octets de la sauvegarde est calculé et comparé à `Sha256` ; une différence interrompt l'opération avec `OL_E_RECOVERY_BACKUP_CORRUPT` avant toute écriture. Les octets sont ensuite écrits de manière atomique (l'attribut d'un fichier actuel en lecture seule est d'abord retiré), et l'heure de création, l'heure de dernière écriture et les attributs sont restaurés (`RestoreMetadata` ; les échecs sur les métadonnées sont ignorés afin qu'un problème de permissions ne puisse pas bloquer une restauration exacte à l'octet près). |
| Absent, désormais présent, `AppliedSha256` connu | Le hash des octets actuels est calculé. S'il est égal à `AppliedSha256`, le fichier est l'overlay propre à la session et il est supprimé. Sinon, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` interrompt la restauration et le journal est conservé. |
| Absent, désormais présent, suppression de session, le journal a atteint `ProcessStarted` | Le fichier est un sous-produit de la session (OMSI s'est exécuté alors que le bail d'installation était détenu et il était demandé que ce chemin reste absent). Il est supprimé et signalé par le diagnostic `restore.session-artifact-removed` avec le SHA-256 du contenu supprimé. |
| Absent, désormais présent, suppression de session, le processus n'a jamais démarré | Le fichier provient de l'extérieur de la session. Il est conservé, signalé comme `OL_W_RESTORE_FOREIGN_FILE_RETAINED` avec son SHA-256, et la transaction se termine malgré tout. |
| Absent, désormais présent, aucune preuve de propriété (journal antérieur aux empreintes) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` ; le journal est conservé. |
| Absent, toujours absent | Rien à faire. |

Une fois tous les fichiers traités, `VerifyRestoredSnapshots` relit chaque chemin : le hash des originaux existants doit être égal à `Sha256`, les chemins initialement absents doivent être absents, sauf s'ils ont été explicitement conservés. Ce n'est qu'alors que `Restored` est persisté, le journal supprimé (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` s'il subsiste) et le répertoire de sauvegarde retiré. Un plantage entre `Restored` et la suppression du journal ne provoque qu'une réexécution idempotente.

Les notes de restauration (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) apparaissent sous forme d'entrées `LaunchDiagnostic` dans `SessionStatus.Diagnostics` (avec `sha256` dans `Data`) et dans `RecoveryStatus.Diagnostics`, de sorte que rien n'est supprimé ni conservé silencieusement.

<a id="originally-absent-files-and-ownership"></a>
### Fichiers initialement absents et propriété

Un overlay écrit sur un chemin qui n'existait pas est supprimé lors de la restauration **uniquement si son contenu correspond toujours à ce que la session a appliqué** (`AppliedSha256`). Si quelque chose d'autre l'a remplacé pendant la session, la restauration échoue avec `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` et le journal est conservé pour inspection.

Un chemin de suppression de session (cibles `.itx`, `Texture\standard.ipr`, `closecheck`) qui n'existait pas avant mais existe après est jugé selon qu'OMSI s'est exécuté ou non sous cette transaction : si le journal a atteint `ProcessStarted`, le fichier est un sous-produit de la session et il est supprimé (`restore.session-artifact-removed`) ; si le processus n'a jamais démarré, le fichier est conservé et signalé comme `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, et le journal se termine malgré tout.

### `closecheck`

`closecheck` est le propre marqueur de plantage d'OMSI (présent lorsqu'OMSI ne s'est pas arrêté proprement). Deux règles s'appliquent :

- S'il existe **avant** la session et que `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` vaut `true` (la valeur par défaut), il est supprimé définitivement avant l'ouverture de la transaction, enregistré par le diagnostic `closecheck.stale-removed` avec son SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` si la suppression échoue). Il s'agit d'une modification permanente documentée, pas d'un participant à la transaction. Avec l'option à `false`, le marqueur reste et OMSI affiche son avertissement.
- S'il n'existe **pas** avant la session, `closecheck` est ajouté comme suppression de session. Comme la session se termine par `TerminateProcess` (la routine d'arrêt d'OMSI ne s'exécute pas), le marqueur posé par OMSI au démarrage est toujours présent ensuite ; il est supprimé lors de la restauration en tant qu'artefact de session.

<a id="early-recovery-order"></a>
## Ordre de la récupération anticipée

À chaque `StartSessionAsync`, après la prise du bail d'installation et avant que quoi que ce soit ne lise l'installation active :

1. Une transaction de récupération seule vérifie la présence de `journal.json`. S'il est présent, `RestorePendingAsync` s'exécute immédiatement, de sorte que les overlays et la langue de l'écran de démarrage de la nouvelle session proviennent des fichiers **originaux**, jamais des restes d'une session précédente.
2. Si cette récupération échoue avec `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (un journal antérieur aux empreintes), la récupération est **différée** : la nouvelle session construit ses overlays, et la nouvelle transaction retente la récupération en utilisant ses propres octets planifiés comme preuve de propriété (un fichier initialement absent dont le contenu est égal au nouvel overlay est accepté comme appartenant à OmsiLaunch). Tout autre échec de récupération fait échouer le démarrage.
3. Ce n'est qu'ensuite que la closure du plugin est validée, le hash de `Omsi.exe` calculé, `closecheck` traité, et la nouvelle transaction préparée et appliquée.

La trace de l'hôte enregistre `PENDING_JOURNAL_RECOVERED` ou `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Récupération après plantage et vivacité du propriétaire

La récupération ne remplace jamais de fichiers sous un OMSI en cours d'exécution. `RestorePendingAsync` refuse avec `OL_E_INSTALLATION_BUSY` tant que le propriétaire journalisé est actif :

| Contenu du journal | Test de vivacité |
| --- | --- |
| `ProcessId` et `ProcessStartFileTimeUtc` enregistrés | Le processus portant ce PID doit être en cours d'exécution, son heure de démarrage doit correspondre (ce qui rejette la réutilisation de PID) et, lorsque `ExecutablePath` est enregistré, son module principal doit être ce chemin (un processus actif sans rapport ne peut pas retenir la transaction). |
| Pas de PID, état entre `HandoffCreated` (inclus) et `ProcessExited` (exclu) | L'hôte s'est arrêté entre `CreateProcess` et l'écriture du journal. Tout `Omsi.exe` dont le module principal est `<root>\Omsi.exe` est considéré comme le propriétaire. |
| Pas de PID, autres états | Non actif ; la récupération se poursuit. |

La récupération explicite est exposée par `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, qui renvoie `RecoveryStatus(Pending, Recovered, Diagnostics)` ; elle prend d'abord le bail d'installation (`OL_E_INSTALLATION_BUSY` lorsqu'un autre propriétaire le détient). Dans la CLI, `/recovery-status` produit un rapport sans restaurer et `/recover` restaure ; le code de sortie 8 (`TransactionRecoveryFailed`) est renvoyé lorsqu'une restauration a été demandée et que le journal est toujours en attente ensuite. Voir [CLI](../reference/cli.md) et [API publique](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Restauration différée en fin de session

Si le superviseur ne peut pas confirmer qu'OMSI s'est terminé (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED`, ou une erreur de nettoyage signalée comme `OL_E_PROCESS_CLEANUP_FAILED`), la session échoue avec `OL_E_RESTORE_DEFERRED` et le journal est délibérément conservé : remplacer des fichiers de l'installation alors qu'OMSI pourrait encore les lire n'est pas sûr. Le démarrage suivant (ou `/recover`) restaure une fois le processus disparu. Une restauration qui échoue pour toute autre raison termine la session avec `OL_E_RESTORE_FAILED` ; le journal subsiste jusqu'à ce que chaque original possédé soit restauré et vérifié.

<a id="the-installation-lease"></a>
## Le bail d'installation

Le bail est un sémaphore nommé `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` avec un compteur de 1. La racine est normalisée par `InstallationLease.NormalizeRoot` (chemin complet, séparateurs de fin supprimés sauf pour une racine de lecteur), de sorte que `C:\OMSI`, `C:\OMSI\` et `c:\omsi\sub\..` partagent un même bail ; le nom du pipe de contrôle local utilise la même normalisation. Il est pris par `StartSessionAsync` (état `AcquiringInstallationLock`) et par `RecoverPendingAsync`, et libéré lorsque la tâche de cycle de vie de la session se termine ou que l'appel de récupération retourne. `OL_E_INSTALLATION_BUSY` est levé immédiatement lorsqu'il ne peut pas être pris (aucune attente).

Limitations acceptées (documentées, aucune modification prévue) :

- Portée `Local\` : un propriétaire par installation **par session de connexion Windows**. Deux utilisateurs interactifs sur la même machine ne sont pas mutuellement exclus.
- Un sémaphore n'est pas libéré par un plantage tant qu'un autre processus détient encore un handle sur lui ; contrairement à un mutex abandonné, il n'a pas de propriétaire. Un détenteur obsolète laisse l'installation en `OL_E_INSTALLATION_BUSY` jusqu'à la fermeture de ce handle.
- Tout processus du même utilisateur Windows peut créer le nom en premier et le détenir.

<a id="omsilaunch-directory"></a>
## Répertoire `.omsilaunch`

| Entrée | Durée de vie | Propriétaire |
| --- | --- | --- |
| `journal.json` | Temporaire ; n'existe que tant qu'une transaction est en attente | Transaction |
| `backup\<sessionId>\*.bin` | Temporaire ; supprimé après le journal | Transaction |
| `diagnostics\<sessionId>-host.log` | Permanent ; la rétention conserve les 50 sessions les plus récentes (les anciens fichiers préfixés par une session sont supprimés au démarrage d'une nouvelle session) | Trace de l'hôte |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanent (même rétention) | CLI |
| `diagnostics\tray-host.log` | Permanent | Hôte de la zone de notification Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Ressources permanentes du produit ; copiées une seule fois depuis le paquet, jamais écrasées ni supprimées | Ressources visuelles de session |
| `session-profiles\<id>\` | Permanent ; installé par l'utilisateur ou l'auteur du contenu | Utilisateur |
| `profiles\` | Ni créé ni lu par le code actuel ; réservé | aucun |
| `docs\`, `examples\` | Permanent ; fourni par le paquet de version | Paquet |

Aucune donnée ne quitte la machine ; les diagnostics sont uniquement des fichiers locaux. Voir aussi [répertoire `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Les modifications runtime ne sont pas journalisées

Les opérations de contrôle runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, textures D3D) ne modifient que l'état en mémoire d'OMSI. Elles ne sont pas enregistrées dans le journal et ne sont pas restaurées ; elles disparaissent avec le processus. Voir [contrôle runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Modes d'échec et codes d'erreur

| Code | Signification | Journal ensuite |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Bail détenu par un autre propriétaire, ou le processus OMSI journalisé est toujours actif | conservé |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Restauration demandée pour une transaction avec des instantanés mais sans journal sur le disque | n/a |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | Le hash d'une sauvegarde diffère de l'empreinte de l'instantané ; rien n'a été écrit | conservé |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Un chemin d'overlay initialement absent contient désormais un contenu que la session n'a pas écrit | conservé |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal antérieur aux empreintes avec un chemin initialement absent qui existe désormais ; seule une nouvelle session avec des octets planifiés identiques peut le clore | conservé (différé) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `journal.json` n'a pas pu être supprimé après une restauration vérifiée | conservé (la réexécution est idempotente) |
| `OL_E_RESTORE_DEFERRED` | Fin d'OMSI non confirmée ; restauration reportée au démarrage suivant | conservé |
| `OL_E_RESTORE_FAILED` | Tout autre échec de restauration (différence de présence ou de hash après restauration, erreur d'E/S) | conservé |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Le `closecheck` obsolète n'a pas pu être supprimé avant la transaction | pas encore de journal |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Avertissement : un fichier étranger sur un chemin de suppression de session a été conservé | terminé |
| `OL_E_PLAN_NOT_RUNNABLE` | La nouvelle planification au démarrage a constaté que la spécification n'est plus exécutable (par exemple un `Omsi.exe` modifié) ; aucune transaction n'est ouverte | aucun |

La CLI associe `OL_E_RECOVERY_*` et `OL_E_RESTORE_FAILED` au code de sortie 8 et `OL_E_INSTALLATION_BUSY` au code de sortie 7 ; voir [codes de sortie](../reference/exit-codes.md).

<a id="evidence"></a>
## Preuves

Les tests hors ligne de `tools/OmsiLaunch.TestHost` couvrent les chemins de la transaction : `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Preuves d'exécution (matrice de validation) : RV-005 et RV-006 (overlay appliqué et restauration exacte à l'octet près, sessions `1e8e0548-...` et le lot de présentation), RV-008 réussite de la fin prématurée (session `0dc40570-...`).

Preuves d'exécution (série de clôture runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`) :
- suppression en tant qu'artefacts de session des cibles `.itx` avec le vrai téléchargeur d'OMSI, lors d'un arrêt normal et après une interruption du propriétaire suivie de `/recover` (S-01, `I01`, `I02`) ;
- récupération anticipée avant la construction des overlays (S-05, `S05`) ;
- restauration des métadonnées et de la lecture seule plus nettoyage des sauvegardes (S-12, `S12a`, `S12b`) ;
- `/recover` sous le bail, avec un OMSI orphelin et dans la fenêtre antérieure au PID (S-04, `S04`, `S04b`) ;
- échecs de démarrage et restauration en échec suivie de `/recover` (reste de RV-008, `SF01`, `SF02`, `F01`) ;
- préservation de CP1252 (S-07, `C01`).

La branche de récupération différée antérieure aux empreintes reste couverte hors ligne uniquement. Voir [état de la validation à l'exécution](../status/runtime-validation-status.md).
