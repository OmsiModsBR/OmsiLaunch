# Cycle de vie de la session

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Traduction de la [page originale en anglais](../../../concepts/session-lifecycle.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page décrit comment une session OmsiLaunch parcourt `SessionState` de `Created` jusqu'à `Completed` ou `Failed` : quel composant définit chaque état, quels événements de télémétrie du plugin déclenchent les transitions, comment fonctionne le timeout de démarrage, ce que signifie l'arrêt (arrêt forcé), ce que renvoie `WaitForAsync`, quels états sont terminaux, quels états ne sont jamais ou à peine observables, et quelles garanties offre le propriétaire CLI. Tout ce qui suit est tiré de `OmsiLaunchService.StartAsync`, `SuperviseAsync` et `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), de `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) et de `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Pages associées : [API publique](../reference/public-api.md), [codes d'erreur](../reference/errors.md), [transactions et récupération](transactions-and-recovery.md), [plugin permanent](permanent-plugin.md), [contrôle runtime](../reference/runtime-control.md), [plan de contrôle local](../reference/local-control.md), [zone de notification Windows](../reference/windows-tray.md), [référence CLI](../reference/cli.md), [état de la validation à l'exécution](../status/runtime-validation-status.md), [le répertoire `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Vue d'ensemble

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## Référence de `SessionState`

Valeurs dans l'ordre de déclaration. « Défini par » nomme le code qui appelle `Move`/`Fail` ; « Observable » indique si `GetStatusAsync`/`WaitForAsync` peut le voir en pratique.

| # | État | Défini par | Observable | Signification |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (valeur initiale de la session active) | Brièvement | La session est enregistrée ; rien ne s'est encore produit. |
| 1 | `ValidatingPlatform` | personne | Non | Déclaré, jamais défini par le service actuel (la validation de la plateforme a lieu dans `PlanSessionAsync`, qui n'a pas d'état de session). |
| 2 | `Planning` | personne | Non | Déclaré, jamais défini (la planification a lieu avant qu'une session existe ; la nouvelle planification dans `StartSessionAsync` précède elle aussi l'enregistrement). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Oui | Le bail d'installation est en cours d'acquisition. Échec : `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Oui | Un `journal.json` en attente est restauré avant la lecture de l'installation active ; la closure du plugin permanent est validée (`plugin.integrity.reference`), le hash de `Omsi.exe` est calculé, les ressources de l'écran de démarrage sont initialisées, un `closecheck` obsolète est supprimé. Échecs : `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (processus journalisé encore actif). |
| 5 | `Snapshotting` | `StartAsync` | Pratiquement non | Défini immédiatement avant `ApplyingConfiguration` sans aucun await entre les deux ; l'instantané lui-même est pris dans `ApplyAsync`. Transitoire et non observable. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Oui | `journal.json` est écrit (`Prepared`), les originaux sont sauvegardés, les overlays écrits, les suppressions de session effectuées (`Applied`). Échecs : `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, erreurs d'E/S. |
| 7 | `DeployingRuntime` | `StartAsync` | Oui | État du journal `RuntimeDeployed`. Aucun fichier n'est déployé : la closure du plugin est permanente. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Oui | Le handoff (`OmsiLaunch.Handoff.<id>`), le slot de télémétrie (`OmsiLaunch.Telemetry.<id>`) et la boîte aux lettres runtime (`OmsiLaunch.Runtime.<id>`) existent ; état du journal `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Oui | `CreateProcessW` pour `<root>\Omsi.exe` avec le répertoire de travail `<root>`. Échecs : `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Oui | Le processus existe, `process.started` est enregistré, le journal est à `ProcessStarted`, le superviseur est démarré et `StartSessionAsync` retourne. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` sur `plugin.started` | Oui | Le plugin permanent a lu un handoff valide pour cette session. À partir d'ici, un timeout donne `OL_E_STARTUP_TIMEOUT` au lieu de `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` sur `world.starting` | Oui (NEW_MAP uniquement) | Le plugin a invoqué le démarrage natif NEW_MAP sur le thread d'interface d'OMSI. Les situations enregistrées émettent `world.situation.starting`, qui n'est pas mappé, de sorte qu'une session SAVED_SITUATION passe directement de `PluginBootstrap` à `Running`. |
| 13 | `EnteringGameplay` | personne | Non | Déclaré, jamais défini : `gameplay.entered` fait passer la session directement à `Running`. |
| 14 | `Running` | `ApplyTelemetry` sur `gameplay.entered` | Oui | Le jeu est atteint. `ExecuteRuntimeAsync` est autorisé ; le propriétaire CLI ouvre le plan de contrôle local ; la zone de notification affiche une session en cours. |
| 15 | `ProcessExited` | `SuperviseAsync` | Oui (sessions réussies uniquement) | OMSI s'est terminé (naturellement ou par arrêt forcé), le journal est à `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (et le chemin d'échec du démarrage) | Oui (sessions réussies uniquement) | Chaque fichier appartenant à la session est restauré à partir de sa sauvegarde vérifiée ; les artefacts de session sont supprimés. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Oui (sessions réussies uniquement) | Restauration vérifiée, journal et sauvegardes supprimés ; les stores runtime sont sur le point d'être libérés. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Oui, terminal | Stores libérés, boîte aux lettres fermée, bail libéré, aucun échec enregistré. |
| 19 | `Failed` | `LiveSession.Fail` depuis `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Oui, terminal | Un diagnostic d'échec a été enregistré. L'état est persistant : les appels `Move` ultérieurs sont ignorés, de sorte qu'une session en échec n'affiche jamais `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, même si l'arrêt forcé et la restauration ont quand même lieu. |

États terminaux : `Completed` et `Failed`. Après l'un ou l'autre, `WaitForAsync` retourne immédiatement et `CloseAsync` retourne sans demander d'arrêt.

Vérification de « jamais défini » : une recherche dans le code source de `SessionState.ValidatingPlatform`, `SessionState.Planning` et `SessionState.EnteringGameplay` ne trouve que la déclaration de l'enum ; `SessionState.Snapshotting` apparaît une seule fois, immédiatement suivi de `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Phase de démarrage (`StartSessionAsync`)

1. Rejeter un plan non exécutable (`OL_E_PLAN_NOT_RUNNABLE`), replanifier la spécification (nouveau calcul du hash de `Omsi.exe`, nouvelle résolution du contenu, nouvelle vérification de la closure du plugin) et rejeter à nouveau si elle n'est plus exécutable. Enregistrer la session active (`Created`).
2. Créer la trace de l'hôte `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (les anciens fichiers préfixés par une session au-delà des 50 sessions les plus récentes sont supprimés). Rejeter `StartupTimeoutSeconds` en dehors de 1..600 (`ArgumentOutOfRangeException` ; la session est désenregistrée).
3. `AcquiringInstallationLock` → bail. `RecoveringPreviousTransaction` → récupérer un journal en attente (un journal antérieur aux empreintes qui ne peut pas prouver la propriété est différé et retenté une fois que les overlays de cette session existent), valider la closure du plugin permanent, calculer le hash de l'exécutable, supprimer un `closecheck` obsolète, construire la transaction (overlays : correctifs de `options.cfg`, BMP gérés de l'écran de démarrage, `Texture\standard.itx` ; suppressions : cibles ITX, `Texture\standard.ipr`, `closecheck` lorsqu'il n'existe pas).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`, puis la tâche du superviseur démarre et le handle est renvoyé.
5. Toute exception aux étapes 3–4 est interceptée : la session passe à `Failed` avec `OL_E_START_SESSION` (message interne), un processus créé est arrêté et attendu, les stores sont libérés, la transaction est restaurée (`OL_E_RESTORE_FAILED` en cas d'échec) ou, si la fin d'OMSI n'a pas pu être confirmée, laissée en attente avec `OL_E_RESTORE_DEFERRED` ; le bail est libéré. `StartSessionAsync` renvoie quand même le handle dans ce cas ; lisez `GetStatusAsync`.

La nouvelle planification conserve le `SessionId` de l'appelant, de sorte que l'identifiant du handle est égal à `plan.SessionId`.

## Supervision (`SuperviseAsync`)

Le superviseur s'exécute dans une tâche du pool de threads et boucle toutes les 100 ms jusqu'à ce qu'OMSI se soit terminé ou qu'un arrêt ait été demandé :

1. Lire chaque événement de télémétrie que le plugin a validé depuis la dernière lecture, dans l'ordre (un anneau d'événements ; un lecteur en retard reçoit `telemetry.lost` avec le nombre d'événements perdus). Chaque événement est ajouté à `RuntimeEvents` et mappé par `ApplyTelemetry`.
2. Si la session est `Failed`, quitter la boucle.
3. Si la session n'est pas encore `Running` et que l'échéance (`StartupTimeoutSeconds` après l'entrée du superviseur) est dépassée : `Fail` avec `OL_E_STARTUP_TIMEOUT` lorsque `PluginBootstrap` a été atteint, sinon `OL_E_PLUGIN_NOT_LOADED` ; quitter la boucle.

Après la boucle : si OMSI s'est terminé avant `Running` et qu'aucun échec n'a été enregistré, `Fail` avec `OL_E_PROCESS_EXITED_EARLY`. Ensuite, que la session ait échoué ou non : arrêter OMSI s'il est encore actif, attendre sa fin, marquer le journal `ProcessExited`, passer à `ProcessExited`, restaurer (`Restoring` → `CleaningRuntime`) ou enregistrer `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, libérer le handle de processus, le handoff, le slot de télémétrie et la boîte aux lettres runtime, libérer le bail, et passer à `Completed` sauf si l'état est `Failed`. Une erreur dans le superviseur lui-même est enregistrée comme `OL_E_PROCESS_SUPERVISION` (les problèmes de nettoyage comme `OL_E_PROCESS_CLEANUP_FAILED`) et le même chemin d'arrêt/restauration s'exécute.

Comme `Failed` est persistant, la seule preuve qu'une session en échec a été restaurée est l'absence de `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` dans ses diagnostics (et l'absence de `journal.json`) ; les notes de restauration (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) apparaissent dans les deux cas.

<a id="telemetry-events"></a>
## Événements de télémétrie

Le plugin publie des échantillons JSON `{ "name": ..., "data": {...} }` dans le slot de télémétrie ; l'hôte enregistre chaque nouvel échantillon comme un `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Émis par | Action de l'hôte |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` après la lecture d'un handoff valide | `Move(PluginBootstrap)` ; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start` : pas de `OMSILAUNCH_HANDOFF_NAME`, handoff illisible ou invérifiable | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start` : le handoff demande un mode de monde autre que NEW_MAP/SAVED_SITUATION, un démarrage non headless, un véhicule du joueur, des modes de date/heure, ou une identité de situation vide | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start` : la validation du build dans le processus a échoué | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | enregistré uniquement |
| `headless.arm.failed` | `PluginRuntime.Start` : le hook natif de démarrage headless n'a pas pu être armé | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | enregistré uniquement |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` lorsque `InternetTextures.Mode` vaut `Disabled` | enregistré uniquement |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 ou 4) | NEW_MAP : OMSI n'est pas encore prêt ; le démarrage est retenté au prochain tick du timer d'interface | enregistré uniquement |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | chemin de réussite NEW_MAP | enregistré uniquement |
| `world.failed` (`native_status`) | NEW_MAP : le démarrage natif a renvoyé un échec | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | chemin SAVED_SITUATION | enregistré uniquement (aucun changement d'état) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION : le démarrage natif a renvoyé un échec | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP : champs de sélection du point d'entrée ou `entrypoint_diagnostics = unavailable` ; SAVED_SITUATION : `situation`) | fin du démarrage du monde | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` dès qu'une opération `d3d.*` a activé la sonde | enregistré uniquement |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` lorsque la réapplication d'un `camera.lock` actif lève une exception (signalé une fois par erreur distincte) | enregistré uniquement |
| JSON invalide | n'importe lequel | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Réserves : le slot contient un seul échantillon, de sorte que des événements émis au cours d'un même cycle d'interrogation de 100 ms de l'hôte peuvent être perdus (le plugin supprime les événements de cycle de vie pendant 2 s après `gameplay.entered` et ne publie jamais d'événement D3D dans le tick qui publie `gameplay.entered`, afin que la frontière `Running` ne soit pas manquée). `RuntimeEvents` conserve les 256 événements les plus récents ; les plus anciens sont abandonnés. Ce n'est pas un journal sans perte. Lisez les événements via `GetStatusAsync`, `session.events` sur le plan de contrôle, ou `events read|watch` dans la CLI.

<a id="startup-timeout"></a>
## Timeout de démarrage

| Élément | Valeur |
| --- | --- |
| Source | `LaunchSpec.Behavior.StartupTimeoutSeconds` (180 par défaut ; 1..600 ; CLI `/startup-timeout`, profil `behavior.startup-timeout`). |
| Début du décompte | Lorsque la tâche du superviseur entre dans sa boucle (après le renvoi du handle). |
| Expiration avant `PluginBootstrap` | `Failed` avec `OL_E_PLUGIN_NOT_LOADED`. |
| Expiration après `PluginBootstrap`, avant `Running` | `Failed` avec `OL_E_STARTUP_TIMEOUT`. |
| Après `Running` | Aucun timeout ne s'applique ; la session dure jusqu'à ce qu'OMSI se termine ou qu'un arrêt soit demandé. |
| Propriétaire CLI | Attend `Running` pendant `StartupTimeoutSeconds + 5` secondes ; en cas d'échec, affiche l'état, affiche sous `OmsiLaunchW.exe` une boîte de dialogue contenant le dernier diagnostic `OL_E_` (code de repli `OL_E_SESSION_START_FAILED`), et se termine avec 1 après `CloseAsync`. |

`ShutdownTimeoutSeconds` est transporté dans la spécification mais n'est pas utilisé : il n'y a pas d'attente d'arrêt.

<a id="stop-semantics"></a>
## Sémantique de l'arrêt

Toute demande d'arrêt est la même demande canonique : `StopAsync(handle)` depuis l'API, `CloseAsync` sur une session non terminale, `session.stop` sur le plan de contrôle local (lié à l'identifiant de la session active), « End session » (`Terminer la session`) dans la zone de notification, Ctrl+C ou la fermeture de la console dans le propriétaire CLI, et la fin de `/observe-seconds`.

| Étape | Détail |
| --- | --- |
| 1 | `StopRequested` est défini sur la session active ; l'appelant retourne immédiatement. |
| 2 | En moins de 100 ms, le superviseur quitte sa boucle et appelle `TerminateProcess(Omsi.exe, 1)`. Il s'agit d'un arrêt forcé : la routine d'arrêt d'OMSI ne s'exécute pas, OMSI ne réécrit pas `options.cfg`, aucune boîte de dialogue d'enregistrement n'apparaît. C'est délibéré, afin qu'OMSI ne puisse pas écraser les fichiers que la transaction s'apprête à restaurer. |
| 3 | Le superviseur attend la fin du processus, enregistre `ProcessExited`, restaure exactement chaque fichier appartenant à la session (y compris le marqueur `closecheck` écrit par OMSI pendant la session, qui devient une note `restore.session-artifact-removed`), supprime le journal et les sauvegardes, libère les stores runtime (les appels ultérieurs à `ExecuteRuntimeAsync` lèvent `OL_E_RUNTIME_CHANNEL_CLOSED` ou `OL_E_SESSION_NOT_RUNNING`), libère le bail et passe à `Completed`. |
| Fin naturelle | Si OMSI se termine de lui-même après `Running` (l'utilisateur ferme OMSI), le même chemin s'exécute sans arrêt forcé et la session se termine normalement. Avant `Running`, c'est `OL_E_PROCESS_EXITED_EARLY`. |
| Arrêt coopératif | Non implémenté. L'envoi de `WM_CLOSE` suivi d'une attente de `ShutdownTimeoutSeconds` n'est pas implémenté (décision produit ; OMSI a ignoré `WM_CLOSE` envoyé à sa fenêtre principale lors de la série de clôture runtime, `L05b`) ([état de la validation à l'exécution](../status/runtime-validation-status.md)). |
| État côté runtime | Tout ce qui est modifié via des opérations runtime (horloge, caméra, véhicules créés en cours de session, variables de script, textures D3D) est un état interne au processus et disparaît avec lui ; il n'est jamais restauré ni persisté. |

<a id="waitforasync-semantics"></a>
## Sémantique de `WaitForAsync`

| Situation | Résultat |
| --- | --- |
| La session atteint l'état demandé | Renvoie l'état avec `State == requested`. |
| La session atteint d'abord un état terminal | Retourne immédiatement avec `Completed` ou `Failed` (consultez `Diagnostics`). |
| Le timeout expire | Renvoie l'état actuel (aucune exception). Comparez `State` avec ce que vous avez demandé. |
| L'état demandé est déjà dépassé (ou jamais défini : `ValidatingPlatform`, `Planning`, `EnteringGameplay`, en pratique `Snapshotting`) | Attend jusqu'à un état terminal ou jusqu'au timeout. |
| L'appelant annule | `OperationCanceledException`. |
| Handle inconnu ou fermé | `KeyNotFoundException`. |

L'intervalle d'interrogation est de 100 ms ; les transitions observées ont donc jusqu'à 100 ms de retard sur les transitions réelles.

<a id="owner-lifecycle-guarantees-cli"></a>
## Garanties du cycle de vie du propriétaire (CLI)

`OwnerSession.RunAsync` dans `tools/OmsiLaunch.Cli/Program.cs` est le propriétaire de référence.

| Garantie | Détail |
| --- | --- |
| Propriétaire unique | Avant de démarrer, la CLI sonde le pipe de contrôle ; si un propriétaire répond, elle refuse avec `OL_E_SESSION_ALREADY_ACTIVE` (code de sortie 7). Le bail applique la même règle entre processus. |
| Chaque chemin de sortie atteint `CloseAsync` | À partir de `StartSessionAsync`, les exceptions, Ctrl+C (`CancelKeyPress`), la fermeture de la console ou la déconnexion (`ProcessExit` : l'arrêt est demandé et le propriétaire attend jusqu'à 4 s `Completed` ; ce qui reste est récupéré par le journal au démarrage suivant), l'arrêt depuis la zone de notification, `session.stop` sur le plan de contrôle, l'expiration de `/observe-seconds` et la fin naturelle aboutissent tous au bloc `finally` qui libère le plan de contrôle et la zone de notification et attend `CloseAsync`. |
| `/observe-seconds` est une borne supérieure | Les demandes d'arrêt depuis la zone de notification ou le plan de contrôle peuvent toujours terminer la session plus tôt. |
| Plan de contrôle uniquement pendant Running | Le point de terminaison du canal nommé (named pipe) est créé après `Running` (et après d'éventuels lots de validation `INTERNAL`) et libéré avant `CloseAsync` ; les clients obtiennent `OL_E_NO_ACTIVE_SESSION` à tout autre moment. |
| Code de sortie | 0 lorsque l'état final est `Completed`, 1 lorsqu'il est `Failed` ou que le jeu n'a pas été atteint, 8 lorsqu'une récupération demandée n'a pas abouti ([codes de sortie](../reference/exit-codes.md)). |
| Diagnostics | Trace de l'hôte et artefacts des opérations runtime sous `<root>\.omsilaunch\diagnostics`, journal de la zone de notification `tray-host.log` ; aucune donnée ne quitte la machine. |

Les intégrateurs qui écrivent leur propre propriétaire doivent reproduire les deux premières garanties : un seul `StartSessionAsync` à la fois par installation, et `CloseAsync` sur chaque chemin.

<a id="failure-map"></a>
## Carte des échecs

| Phase | État lors de l'échec | Diagnostics que vous verrez |
| --- | --- | --- |
| Plan | aucun (pas de session) | `OL_E_PLAN_NOT_RUNNABLE` levé par `StartSessionAsync` ; les propres codes `OL_E_` du plan ([validation de LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Démarrage (du bail à la création du processus) | `Failed` | `OL_E_START_SESSION` avec le code interne ; éventuellement `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Amorçage du plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Démarrage du monde | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| En cours d'exécution | `Failed` uniquement en cas d'erreur du superviseur | `OL_E_PROCESS_SUPERVISION` ; les erreurs des opérations runtime ne font jamais échouer la session. |
| Arrêt forcé et restauration | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Chaque chemin d'échec tente quand même l'arrêt forcé et la restauration ; un journal restant est récupéré au démarrage suivant ou par `RecoverPendingAsync` / `/recover` ([transactions et récupération](transactions-and-recovery.md)).
