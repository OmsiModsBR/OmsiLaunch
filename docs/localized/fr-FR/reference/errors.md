# Codes d'erreur et de diagnostic

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> Traduction de la [page originale en anglais](../../../reference/errors.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page est la référence normative de chaque code de `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`) : 167 codes d'erreur `OL_E_` et 4 avertissements `OL_W_`, regroupés par catégorie du catalogue, ainsi que les codes de diagnostic informatifs qui ne sont pas des erreurs. Pour chaque code, elle indique où le code actuel le lève, ce qu'il signifie, comment il vous parvient (exception levée, champ de résultat, diagnostic, réponse du plan de contrôle ou enveloppe CLI) et ce qu'il faut faire. Les significations sont tirées des sites de levée ; lorsqu'un code est défini mais n'a aucun chemin de levée actuel, la page l'indique.

Pages associées : [API publique](public-api.md), [codes de sortie](exit-codes.md), [LaunchSpec](launchspec.md), [cycle de vie de la session](../concepts/session-lifecycle.md), [transactions et récupération](../concepts/transactions-and-recovery.md), [plan de contrôle local](local-control.md), [contrôle runtime](runtime-control.md), [profils de session](session-profiles.md), [plugin permanent](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Comment les codes vous parviennent

| Canal | Signification |
| --- | --- |
| Levée | Une exception dont le `Message` commence par le code (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). La CLI extrait le code du message et l'associe à un code de sortie (`CliProgram.Classify`). |
| Diagnostic de plan | Un `LaunchDiagnostic` dans `SessionPlan.Diagnostics` ; tout code `OL_E_` rend `IsRunnable` faux (CLI : plan `NOT RUNNABLE`, sortie 1). |
| Diagnostic de session | Un `LaunchDiagnostic` dans `SessionStatus.Diagnostics` ; l'état de la session est `Failed` (sortie CLI 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` et `OL_E_RESTORE_FAILED` encapsulent un code interne dans leur message. |
| Résultat runtime | `RuntimeCommandResult.ErrorCode` avec `Succeeded = false`. |
| Détail runtime | `RuntimeCommandResult.ErrorCode` est le code spécifique et `Values["detail"]` le message (avec `Values["exception"]`, le type .NET). Beta 4 : un code typé côté plugin parvient inchangé à l'API et à la CLI ; seul un échec sans code catalogué devient `OL_E_RUNTIME_OPERATION_FAILED`. |
| Réponse de contrôle | `ErrorCode` d'une réponse du plan de contrôle local (`LocalControlResponse`). |
| Enveloppe CLI | `error.code` dans l'enveloppe `--json`, ou `<code>: <message>` sur la console ; le code de sortie est indiqué. |
| Télémétrie | Un nom d'événement runtime que l'hôte associe à un diagnostic de session. |

## CLI

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Une `OperationCanceledException` s'est échappée (Ctrl+C ou attente client annulée). | Enveloppe CLI, sortie 7 | Relancez la commande. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Une exception sans code `OL_E_` s'est échappée : JSON `/spec` mal formé, membre obligatoire de la spec à null, erreur inattendue. | Enveloppe CLI, sortie 10 | Lisez le message et `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` ; corrigez l'entrée ; signalez le problème s'il reste inexpliqué. |
| `OL_E_TIMEOUT` | `CliProgram.Classify` ; `LocalControlPlane.TryRequestAsync` | Une `TimeoutException` sans code s'est échappée ; ou le client de contrôle local était connecté à un propriétaire qui n'a pas répondu dans le délai (le propriétaire existe, ce n'est donc pas signalé comme `OL_E_NO_ACTIVE_SESSION`). (Les timeouts de la boîte aux lettres portent `OL_E_RUNTIME_REQUEST_TIMEOUT` à la place.) | Enveloppe CLI, réponse de contrôle ; sortie 5 depuis `Classify`, sortie 7 pour une réponse de contrôle | Réessayez ; vérifiez qu'OMSI et le propriétaire répondent. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` ne se trouve pas à côté de `OmsiLaunch.exe`. | Enveloppe CLI, sortie 7 | Réinstallez le paquet. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` de `OmsiLaunchW.exe` n'a renvoyé aucun processus. | Enveloppe CLI, sortie 7 | Vérifiez les fichiers et les autorisations du paquet ; exécutez sans `/silent` pour voir l'échec. |

<a id="compatibility"></a>
## Compatibilité

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` sur `plugin.build.invalid` | Les capacités de démarrage ont été résolues, mais le formulaire Start actif ou l'un de ses sites de hook n'est pas dans l'état attendu lorsque le plugin arme le démarrage headless (par exemple parce qu'un autre plugin l'a déjà patché). | Diagnostic de session (`Failed`) | Retirez le plugin en conflit ; voir [compatibilité](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` au démarrage ; opérations runtime) | Une capacité native dont la demande a besoin n'a pas pu être résolue et vérifiée sur ce build d'OMSI ; elle est donc indisponible. Au démarrage, la session échoue avant tout hook et les fichiers sont restaurés ; une opération runtime est rejetée sans toucher à la mémoire d'OMSI. `Data["capability"]` (runtime) ou la valeur `capabilities` de la télémétrie la nomme. | Diagnostic de session (`Failed`) ; résultat runtime | Voir [compatibilité](compatibility.md) : la capacité n'est pas disponible sur ce build. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` indisponible) | L'installation n'est pas une installation d'OMSI 2 : `Omsi.exe` est absent, n'est pas une image PE x86 32 bits, n'a pas de version OMSI 2 (2.x), ou la racine n'a pas de répertoire `maps\` ou `Vehicles\`. Le hash de l'exécutable n'est pas un critère. | Diagnostic de plan | Pointez OmsiLaunch vers la racine d'une installation d'OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` indisponible) ; également levé par `CurrentWindowsX64Platform.ValidateCurrent`, que le service n'appelle pas | Pas Windows 10 ou ultérieur, ou le système d'exploitation ou le processus hôte n'est pas x64. | Diagnostic de plan (sortie CLI 3 lorsqu'il est levé) | Exécutez sur Windows 10 ou ultérieur 64 bits. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | `CurrentWindowsX64Platform.ValidateCurrent` uniquement | L'architecture du système d'exploitation ou de l'hôte n'est pas x64. Le service n'appelle pas `ValidateCurrent` ; aucun chemin de levée actuel. | Levé (`PlatformNotSupportedException`) par cette méthode uniquement | Voir la source `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

<a id="content"></a>
## Contenu

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation` ; `SessionPlanner` | Validation : NEW_MAP sans `EntrypointIdentity` et avec `PresentedEntrypointIndex` non défini ou négatif. Planificateur : la carte n'a aucun point d'entrée avec l'identité demandée ou le libellé exact (sensible à la casse). | Diagnostic de plan | Définissez `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) ou une identité issue de `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | La météo de démarrage ne peut pas être appliquée : un préréglage qui n'est pas un fichier du dossier `weather` de l'installation, ou un code OACI qui n'est pas composé de quatre lettres ou chiffres. | Diagnostic de plan | Utilisez un préréglage installé ou un code de station valide. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | Le point d'entrée demandé partage son libellé avec un enregistrement antérieur de la carte ; OMSI ne présente que le premier enregistrement de chaque libellé, il ne peut donc pas être sélectionné au démarrage. | Diagnostic de plan | Choisissez le premier enregistrement portant ce libellé, ou un autre point d'entrée. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` indisponible) | Carte NEW_MAP résolue, mais ni index présenté ni identité. Accompagne toujours `OL_E_ENTRYPOINT_NOT_FOUND`. | Diagnostic de plan | Idem. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` n'est pas un `Vehicles\...\*.hof` installé. | Diagnostic de plan | Utilisez une identité issue de `/list:hofs`. (Les champs du véhicule du joueur sont de toute façon non exécutables sur ce build.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Une source de [gestion des HOF](hof-management.md) (collection, répertoire, véhicule ou fichier) n'existe pas, ne contient aucun `.hof`, ou il manque un fichier nommé dans `Files`. | Diagnostic de plan ; échec de démarrage | Corrigez la source ou la liste de fichiers. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Un fichier ou dossier de véhicule cible HOF n'est pas installé. | Diagnostic de plan ; échec de démarrage | Utilisez une identité de véhicule issue de `/list:vehicles` ou un dossier existant sous `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Un chemin HOF sort de l'installation, une cible n'est pas sous `Vehicles\`, un nom de collection n'est pas un nom simple, ou une jonction/un lien symbolique est traversé. | Diagnostic de plan ; échec de démarrage | Utilisez des chemins relatifs à l'installation, sans `..` et sans liens. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Deux opérations HOF écrivent des octets différents dans un même fichier, ou un miroir supprime un fichier qu'une autre opération écrit. | Diagnostic de plan ; échec de démarrage | Réordonnez, scindez ou supprimez les opérations en conflit. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Opération HOF sans cibles ni source, source de fichier qui n'est pas un `.hof`, fichier cible qui n'est pas `.bus`/`.ovh`, ou limite de taille/nombre (8 MiB par fichier, 256 fichiers par opération, 256 MiB par session). | Diagnostic de plan ; échec de démarrage | Corrigez l'opération. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation` ; `SessionPlanner` | Validation : NEW_MAP avec `MapIdentity` non défini ou pas de la forme `maps\<dir>\global.cfg`. Planificateur : la carte n'est pas installée. | Diagnostic de plan | Utilisez une identité issue de `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Une `FileNotFoundException`/`DirectoryNotFoundException` sans code s'est échappée, par exemple `/list:repaints` avec un `/vehicle-scope` inconnu, ou `/list:entrypoints` avec un `/map` inconnu. | Enveloppe CLI, sortie 6 | Corrigez l'identité. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` n'est pas un élément `.cti` du modèle (vérifié uniquement lorsque `Model` est défini). | Diagnostic de plan | Utilisez une identité issue de `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | La carte référencée dans le `.osn` sélectionné n'est pas installée. | Diagnostic de plan | Installez la carte ou choisissez une autre situation. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation` ; `SessionPlanner` | SAVED_SITUATION sans `SituationIdentity`, ou le `.osn` n'est pas installé. | Diagnostic de plan | Utilisez une identité issue de `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` n'est pas un `Vehicles\...\*.bus` installé. | Diagnostic de plan | Utilisez une identité issue de `/list:vehicles`. |

## Installation

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire` ; `OmsiLaunchService.RecoverPendingAsync` ; `FileConfigurationTransaction.RestorePendingAsync` | Le bail d'installation (`Local\OmsiLaunch.Installation.<hash>`) est détenu par un autre propriétaire dans cette session d'ouverture de session Windows, ou un processus OMSI journalisé (PID + heure de création + chemin de l'exécutable ; ou n'importe quel `Omsi.exe` de la racine pour un journal au-delà de `HandoffCreated` sans PID) est toujours actif. | Démarrage : diagnostic de session via `OL_E_START_SESSION`. Récupération : levé (`InvalidOperationException` / `IOException`). Sortie CLI 7. | Arrêtez l'autre propriétaire (`session stop`) ou attendez qu'OMSI se termine, puis réessayez ou lancez `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` est vide. | Diagnostic de plan | Indiquez le répertoire d'installation. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` indisponible) ; également `ValidateCurrent` | La racine n'existe pas, possède l'attribut lecture seule ou n'a pas de répertoire `plugins\`. | Diagnostic de plan | Désignez une installation OMSI réelle et accessible en écriture. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (planification et démarrage) | Un fichier `plugins\OmsiLaunch.*` installé diffère du hash de `release-manifest.json` (ou de la closure de référence en l'absence de manifeste). | Diagnostic de plan (plan non exécutable, sortie CLI 1) ; diagnostic de session via `OL_E_START_SESSION` uniquement si les fichiers changent entre la planification et le démarrage | Réinstallez le paquet OmsiLaunch afin que `plugins\` et le manifeste concordent. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (planification et démarrage) | Le manifeste n'a pas d'entrée pour un fichier de plugin requis. | Diagnostic de plan ; diagnostic de session via `OL_E_START_SESSION` dans la même situation de concurrence que ci-dessus | Réinstallez le paquet. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (planification et démarrage) | Un fichier `plugins\OmsiLaunch.*` requis est absent de l'installation OMSI, ou un fichier `plugins/` listé dans `release-manifest.json` n'est pas installé. | Diagnostic de plan ; diagnostic de session via `OL_E_START_SESSION` dans la même situation de concurrence que ci-dessus | Installez la closure du plugin permanent ([installation](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | `CurrentWindowsX64Platform.ValidateCurrent` uniquement | `CurrentPlatformSupported` est faux. Non appelé par le service ; aucun chemin de levée actuel. | Levé par cette méthode uniquement | Voir la source. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` est vide, n'est pas du JSON, n'a pas de tableau `files`, ou contient une entrée sans `path`/`sha256`, un hash qui ne comporte pas 64 chiffres hexadécimaux, un chemin absolu (enraciné), contenant `:`, un segment vide, `.` ou `..`, ou un chemin listé deux fois (comparaison insensible à la casse, `/` et `\` équivalents). Un BOM UTF-8 est accepté. | Planification : encapsulé dans `OL_E_RUNTIME_ARTIFACT_MISSING` ; démarrage : via `OL_E_START_SESSION` | Réinstallez le paquet. |

<a id="invalidargument"></a>
## Argument invalide

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation` ; `CliInput.Parse`/`Classify` | Validation : `Date.Value`/`Time.Value` défini alors que le mode n'est pas `Explicit`. CLI : option inconnue, valeur manquante, entier ou plage incorrects, `/saved` combiné avec `/map`/`/entrypoint`, route de commande inconnue, toute `ArgumentException`/`FormatException` sans code. | Diagnostic de plan ; enveloppe CLI, sortie 2 | Corrigez l'argument. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (démarrage) | La valeur d'un paramètre sémantique est hors plage, n'est pas booléenne, n'appartient pas à l'ensemble autorisé ou est mal formée (`graphics.particles` requiert quatre champs). Les valeurs ne sont pas validées au moment de la planification. | Diagnostic de session via `OL_E_START_SESSION` | Utilisez une valeur du [tableau des paramètres](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner` ; `CliInput.BuildSpecAsync` ; `BuildTransactionalOverlays` | La clé existe mais n'est pas modifiable (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Diagnostic de plan ; sortie CLI 2 | Supprimez la clé. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner` ; `CliInput.BuildSpecAsync` ; `BuildTransactionalOverlays` | La clé ne figure pas dans `ConfigurationCatalog`. | Diagnostic de plan ; sortie CLI 2 | Utilisez une clé du catalogue. |

## LaunchSpec

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | La racine n'est pas un objet JSON, ou la désérialisation n'a produit aucun enregistrement. | Levé (`InvalidDataException`), sortie CLI 2 | Corrigez le fichier ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | Le fichier `/spec` n'existe pas. | Levé (`FileNotFoundException`), sortie CLI 6 | Vérifiez le chemin. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | Le fichier dépasse 1 MiB. | Levé (`InvalidDataException`), sortie CLI 2 | Réduisez le fichier. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Un membre qui n'est pas une propriété publique de l'enregistrement à cette position ; message `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Levé (`InvalidDataException`), sortie CLI 2 | Supprimez ou renommez le membre. |

<a id="localcontrol"></a>
## Contrôle local

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Gestionnaire du propriétaire (`OwnerSession`) | La commande n'est pas `session.status`, `session.events`, `session.stop` ou `runtime.execute` avec un argument `operation`. | Réponse de contrôle ; sortie CLI 7 | Utilisez une commande prise en charge. |
| `OL_E_CONTROL_FAILED` | Client CLI (`ReportForwarded`, `CliEventWatch`) | Le propriétaire a répondu `Ok = false` sans code d'erreur. | Enveloppe CLI, sortie 7 | Lisez le message ; consultez la console/les diagnostics du propriétaire. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | Le gestionnaire du propriétaire a levé une exception dont le message ne contient aucun code `OL_E_` (par exemple, la session était déjà fermée), ou la réponse du gestionnaire n'a pas pu être sérialisée. | Réponse de contrôle | Lisez `session status` ; redémarrez le propriétaire s'il a disparu. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (des deux côtés) | Préfixe de longueur négatif ou supérieur à 64 KiB (y compris une trame de requête surdimensionnée), trame vide, JSON `null`, requête sans `Command`, ou JSON impossible à décoder. | Réponse de contrôle / enveloppe CLI | Utilisez le protocole documenté ([plan de contrôle local](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (client) | La requête sérialisée du client lui-même dépasse 64 KiB. Signalé à l'appelant ; rien n'est envoyé. | Réponse de contrôle / enveloppe CLI | Réduisez la requête. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (propriétaire) | La réponse du propriétaire ne tient pas dans la trame de 64 KiB. Le propriétaire répond avec cette erreur typée au lieu d'abandonner la réponse. `session.status` et `session.events` ne l'atteignent jamais : leur historique d'événements est réduit en commençant par les plus anciens pour tenir. | Réponse de contrôle / enveloppe CLI | Réessayez ; pour les événements, lisez plus souvent. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | Le `ProtocolVersion` de la requête n'est pas `0.1` ; la réponse du propriétaire n'a pas pu être décodée ou était vide ; le propriétaire a fermé la connexion sans répondre ou la connexion s'est rompue après avoir été établie ; le propriétaire n'a signalé aucun `SessionId`. | Réponse de contrôle / enveloppe CLI | Alignez les versions du client et du propriétaire ; lisez `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Gestionnaire du propriétaire | `session.stop` ou `runtime.execute` sans `session_id` égal à la session active. | Réponse de contrôle ; sortie CLI 7 | Lisez d'abord `session.status` et liez la requête (la CLI le fait automatiquement). |

<a id="other"></a>
## Autres

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | Le plan transmis a `IsRunnable = false`, ou la nouvelle planification au démarrage n'est pas exécutable (`Omsi.exe` modifié, contenu supprimé, closure du plugin manquante) ; le message liste les codes `OL_E_` actuels. | Levé (`InvalidOperationException`) ; sortie CLI 1 | Planifiez de nouveau et corrigez les diagnostics listés. |

<a id="presentation"></a>
## Présentation

Tous levés par `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Au moment de la planification, ils sont encapsulés dans `OL_E_SESSION_PRESENTATION_INVALID` (le message contient le code) ; au démarrage, ils remontent via `OL_E_START_SESSION`.

| Code | Signification / cause typique | Que faire |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | Le fichier `.itx` est vide, comporte un nombre impair de lignes non vides, ou une ligne d'URL n'est pas une URL `http`/`https` absolue. | Utilisez des paires de lignes URL/cible. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (résolu par rapport au répertoire de travail du processus) n'existe pas. Levé sous forme de `FileNotFoundException`. | Indiquez un chemin `.itx` existant. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` vaut `Override` sans `OverrideProfilePath`. Sortie CLI 2 lorsqu'il est levé. | Fournissez `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Une ligne cible est absolue (enracinée), contient `..`, commence par `\`, se résout hors de l'installation, n'a pas de composant `Texture\`, ou traverse une jonction/un lien symbolique. | Utilisez des cibles relatives `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` n'existe pas. | Corrigez le répertoire. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` ou `<LANG>.bmp` manque dans le répertoire des ressources, ou un `assets\splash\<LANG>.bmp` du paquet manque lors de l'initialisation de `.omsilaunch\assets\splash`. | Fournissez les BMP / réinstallez le paquet. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Un BMP d'écran de démarrage n'est pas un bitmap `BM` 24 bits de 640×480. | Convertissez l'image. |

<a id="process"></a>
## Processus

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (chemins d'échec du démarrage et d'erreur du superviseur) | L'arrêt d'OMSI ou l'attente de sa fin pendant le nettoyage après erreur a levé une exception ; le message interne suit. | Diagnostic de session (ajouté à une session `Failed`) | Assurez-vous qu'aucun `Omsi.exe` ne subsiste, puis lancez `/recover` si un journal est en attente. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` a échoué juste après `CreateProcessW` (`Win32=<code>`) ; le processus est arrêté. | Diagnostic de session via `OL_E_START_SESSION` | Réessayez ; vérifiez l'antivirus/les autorisations. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI s'est terminé avant `gameplay.entered` (plantage, fermeture d'une boîte de dialogue d'erreur d'OMSI, fermeture de la fenêtre). | Diagnostic de session (`Failed`) ; la restauration s'exécute | Consultez les journaux propres à OMSI et `logfile.txt` ; examinez `RuntimeEvents` pour le dernier événement du plugin. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` a échoué (`Win32=<code>` dans le message). | Diagnostic de session via `OL_E_START_SESSION` | Résolvez l'erreur Win32 (fichier manquant, accès refusé, stratégie). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | La boucle du superviseur a levé une exception (lecture de télémétrie, attente/arrêt du processus, écriture du journal) ; OMSI est arrêté et une restauration est tentée. | Diagnostic de session (`Failed`) | Lisez le message interne et le journal de l'hôte. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` a échoué (`Win32=<code>`). | Dans les messages `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Arrêtez OMSI manuellement, puis lancez `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` sur le handle du processus a échoué. | Dans les messages `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Idem. |

## Runtime

« Détail runtime » signifie le code spécifique dans `ErrorCode`, avec le message dans `Values["detail"]` et le type de l'exception dans `Values["exception"]`.

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` avec `preset` alors que `family` vaut 2 (extérieure) ou 3 (carte) ; les préréglages n'existent que pour le conducteur (0) et le passager (1). | Détail runtime | Omettez `preset` ou utilisez la famille 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Mode `Date`/`Time` `Explicit` sans valeur ou avec des composantes hors plage. Le nom est historique ; il s'agit d'une erreur de validation au moment de la planification. | Diagnostic de plan | Corrigez la valeur (et notez que la date/l'heure explicite est non exécutable sur ce build). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn` : le chemin `.bus` n'existe pas sous le répertoire de travail d'OMSI (vérifié avant l'appel natif afin qu'OMSI ne puisse pas lui substituer un modèle de repli). | Détail runtime | Utilisez une identité issue de `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | idem | L'appel natif MakeVehicle a modifié la collection de véhicules routiers de plus d'un objet. | Détail runtime (`native_status`, décomptes dans le message) | Signalez le problème ; les objets créés subsistent jusqu'à la fin de la session. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | idem | La collection n'a pas changé ; OMSI a refusé le véhicule silencieusement. | Détail runtime | Vérifiez le fichier `.bus` ; essayez un autre modèle. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | idem | Tout autre statut natif non nul. | Détail runtime | Signalez le problème avec les décomptes du message. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | L'appel profilé PlaceRandomBus a renvoyé un statut d'échec. | Détail runtime | Réessayez une fois le jeu stabilisé ; signalez le problème. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments` ; vérifications côté plugin (`time.set` sans `hour`/`minute`/`second` ; `camera.set` sans `family`/`field_of_view` ; `camera.lock` sans `family` analysable ; opérations sur les véhicules/courbes) | Un argument requis est manquant ou vide. | Résultat runtime (registre ; sortie CLI 2) ou détail runtime (plugin) | Fournissez l'argument ([contrôle runtime](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | Le répertoire/fichier de référence de la closure du plugin ou le pont natif indiqués dans `OmsiLaunchRuntimePaths` ne peuvent pas être chargés (peut encapsuler `OL_E_RELEASE_MANIFEST_INVALID`). | Diagnostic de plan | Exécutez depuis un paquet intact. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, harnais INTERNAL) | La lecture de référence `time.read`/`camera.read` a échoué, le test d'écriture a donc été ignoré. | Artefact de lot uniquement | Non destiné aux utilisateurs. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` est vide, dépasse 240 caractères, contient NUL ou `..`, ne commence pas par `Vehicles\` ou ne se termine pas par `.bus`. | Détail runtime | Passez `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | aucun (conservé pour compatibilité) | N'est plus émis. Les builds antérieurs le levaient lorsqu'une requête annulée restait dans le slot ; chaque chemin terminal d'une requête réinitialise désormais le slot, et une requête ou une réponse résiduelle trouvée au début d'une nouvelle requête est effacée. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | La boîte aux lettres a été libérée parce que la session se termine. | Levé (`InvalidOperationException`) | Aucune action ; la session est terminée. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | Le slot de la boîte aux lettres contenait une valeur d'état qui n'est ni inactive, ni demandée, ni répondue (corruption). Le slot est réinitialisé et l'erreur est levée ; la requête suivante fonctionne normalement. | Levé (`InvalidDataException`) | Réessayez ; signalez le problème s'il persiste. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | Le pointeur du bloc de constantes du véhicule est nul. | Détail runtime | Le véhicule n'a pas de constantes ; aucune action. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` ne figure pas dans la table des constantes du véhicule. | Détail runtime | Listez d'abord les constantes. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | L'objet créé par l'apparition a une VMT hors de la plage de l'image OMSI. | Détail runtime | Signalez le problème. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | idem | L'objet créé ne figure pas dans la collection de véhicules routiers. | Détail runtime | Signalez le problème. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Deux points consécutifs de la courbe ont le même X. | Détail runtime | Problème de contenu dans la courbe du véhicule. |
| `OL_E_RUNTIME_CURVE_EMPTY` | idem | La courbe n'a aucun point. | Détail runtime | Idem. |
| `OL_E_RUNTIME_CURVE_INVALID` | idem | Aucun segment de la courbe ne contient `x`. | Détail runtime | Évaluez à l'intérieur du domaine de la courbe. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | idem | `name` est inconnu ou son pointeur de fonction est nul. | Détail runtime | Listez d'abord les courbes. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | Le pointeur de définition du véhicule est nul. | Détail runtime | Le handle désigne un véhicule sans données de définition. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (mode propriétaire) | `plugins\OmsiLaunch.Plugin.opl` ou `plugins\OmsiLaunch.Native.x86.dll` manque à côté de l'exécutable. | Enveloppe CLI, sortie 7 | Réinstallez le paquet. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` manquant ou vide pour `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (le registre les rejette normalement d'abord avec `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Détail runtime | Fournissez le handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | Le handle est inconnu, l'objet a quitté la collection, la génération d'adresse a avancé, ou l'empreinte de l'objet (VMT + identité de définition/modèle) a changé parce que l'adresse a été réutilisée. Angle mort résiduel : même classe et même modèle recréés à la même adresse entre deux lectures de liste. | Détail runtime | Exécutez de nouveau `road-vehicles.list`/`humans.list` et utilisez le nouveau handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute` ; `CurrentRuntimeCommandMailbox.TryDispatch` ; repli `D3DRuntimeApi` | Enveloppe générique d'échec côté plugin ; `Values["detail"]` contient le message (souvent un code plus spécifique) et `Values["exception"]` le type d'exception. Une exception qui s'échappe d'une opération dans le répartiteur de la boîte aux lettres reçoit également une réponse avec ce code (sans valeurs) au lieu de laisser la requête sans réponse. | Résultat runtime | Lisez `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | Le plugin n'a pas d'implémentation pour une opération que le registre a autorisée (divergence de version entre registre et plugin). | Détail runtime | Réinstallez un paquet cohérent. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | L'opération ne figure pas dans `PublicRuntimeOperationIds`, ce qui inclut toutes les opérations `internal.*`. Vérifié avant la recherche de la session. | Résultat runtime ; réponse de contrôle ; sortie CLI 2 | Utilisez un identifiant d'opération public. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` avec `preset` alors qu'il n'y a pas de véhicule du joueur (les sessions headless n'en ont pas). Également signalé comme `code` de l'événement `camera.lock.degraded` lorsque la réapplication échoue. | Détail runtime / événement runtime | Verrouillez sans préréglage, ou utilisez une session avec un véhicule du joueur. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Un résultat D3D réussi ne contenait aucune valeur, ou une chaîne d'état de périphérique inconnue. | Levé (`OmsiRuntimeException`) | Alignez les versions de l'hôte et du plugin. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | Le slot contient une réponse obsolète portant le même identifiant de requête que la nouvelle requête. La réponse obsolète est effacée avant que l'erreur soit levée. | Levé (`InvalidOperationException`) | Utilisez des identifiants de requête strictement croissants. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Aucune réponse dans le délai `timeout` ; le slot est réinitialisé et une réponse tardive est ignorée. | Levé (`TimeoutException`) ; sortie CLI 5 | Réessayez avec un timeout plus long ; vérifiez qu'OMSI n'est pas bloqué (boîte de dialogue modale, chargement). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | L'enveloppe de la réponse est corrompue, a une longueur incorrecte (négative, nulle ou supérieure au slot), un identifiant de session étranger ou un identifiant de requête différent. Le slot est réinitialisé avant que l'erreur soit levée, de sorte que la requête suivante fonctionne normalement. | Levé (`InvalidDataException`) | Réessayez ; signalez le problème s'il persiste. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | Le `cursor` d'une opération de liste est mal formé ou appartient à un autre type de liste. | Détail runtime | Passez le `next_cursor` de la page précédente de la même opération, ou omettez-le. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | Le résultat sérialisé dépasse la boîte aux lettres de 64 KiB. Les résultats de liste paginée sont raccourcis pour tenir et se poursuivent avec `next_cursor` ; aucun résultat de liste n'atteint ce code. | Résultat runtime | Utilisez une opération plus étroite ou un `limit` plus petit. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | Le pointeur de définition ou d'état du script du véhicule est nul. | Détail runtime | Le véhicule n'a pas d'objets de script. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync` ; `CurrentRuntimeCommandStore` ; boîte aux lettres du plugin | `RuntimeCommand.SessionId` diffère de l'identifiant de session du handle (levé par l'hôte), ou une requête a atteint un plugin lié à une autre session (renvoyé par le plugin sous forme de résultat typé). | Levé (`InvalidOperationException`) / résultat runtime | Construisez la commande avec `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` de l'événement `world.failed`) | La météo de démarrage n'a pas pu être appliquée après le démarrage du monde : OMSI n'a aucune station OACI configurée pour `/weather-real`, ou ses objets météo sont indisponibles. La session échoue. | Échec de session | Configurez une station dans OMSI ou utilisez `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Une clé de `weather.set` désigne une valeur qu'OMSI dérive à chaque cycle météo (humidité, facteurs de lumière, transparence des nuages, indicateur de précipitation, sol mouillé) ; rien n'est écrit. | Résultat runtime | Définissez la clé source nommée dans `detail` (par exemple `dew_point` au lieu de `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Clé `weather.set` inconnue ; rien n'est écrit. | Résultat runtime | Utilisez une clé listée dans [capacités](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` ne figure pas dans la table des variables de chaîne. | Détail runtime | Listez d'abord les variables de chaîne. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Un booléen météo n'est pas `true`/`false`/`1`/`0`. Actuellement inatteignable (voir ci-dessus). | Détail runtime (défini) | Voir la source. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set` : `hour` 0..23, `minute` 0..59, `second` 0..59.999 ; `camera.set` : `family` 0..3, `field_of_view` 10..170 ; `camera.lock` : `preset` n'est pas un entier, `family` 0..3, `preset` 0..255 ; `road-vehicles.place-random` : `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` peut valoir -1), `scheduled` 0..1 ; `vehicle.variable.set` : `value` non fini ; `vehicle.curve.evaluate` : `x` non fini. | Détail runtime | Utilisez une valeur dans la plage. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` ne figure pas dans la table des variables numériques. | Détail runtime | Listez d'abord les variables. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | L'adresse du slot de la variable ou de sa valeur est nulle. | Détail runtime | La variable n'est pas matérialisée pour ce véhicule. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Les scalaires de l'horloge ont été écrits, mais l'appel natif profilé SetTime a renvoyé un échec. | Détail runtime | Réessayez ; relisez avec `time.read`. |

<a id="runtimed3d"></a>
## Runtime D3D

Tous levés par `CurrentRuntimeControl` (correspondance `ThrowD3D` du statut natif ; `detail` contient l'opération et le HRESULT, `native_status` le statut numérique). Canal : résultat runtime avec le code dans `ErrorCode` ; `D3DRuntimeApi` le relève sous forme de `OmsiRuntimeException`.

| Code | Statut natif / cause | Que faire |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8 : le périphérique Direct3D est perdu. | Attendez `d3d.restored` ; recréez les textures (la génération a changé). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14 : `width`, `height`, `level`, `x`, `y`, `format` ou `handle` manquant ou invalide (plages : width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Corrigez les arguments. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` n'est pas du Base64 valide ou dépasse 48 KiB. | Envoyez des rectangles plus petits. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, ou un nom de `format` inconnu (valides : `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Utilisez un format listé. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Tout autre statut ; le HRESULT figure dans `detail`. | Signalez le problème avec le HRESULT. |
| `OL_E_D3D_NOT_READY` | 7 : le périphérique n'est pas prêt (avant la première image ou pendant l'arrêt). | Réessayez après `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9 : une réinitialisation du périphérique est en cours. | Réessayez après `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13 : le handle de texture a déjà été libéré. | Ne réutilisez pas les handles libérés. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12 : le handle appartient à une génération précédente du périphérique ; ou la chaîne du handle n'est pas `d3dtex-<session>-<hex>` / vaut zéro. | Recréez la texture. |

## Session

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner` ; `ApplyTelemetry` sur `plugin.request.unsupported` | Plan : `LastMapState`, `EntrypointIdentity`, mode date/heure/année, mode météo, champs du véhicule du joueur ou documents d'entrée demandés (`Requested capability unavailable: <name>`). Télémétrie : le plugin a rejeté le handoff (impossible pour un plan exécutable). | Diagnostic de plan ; diagnostic de session | Supprimez la demande non prise en charge ([limitations connues](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` sur `headless.arm.failed` | Le plugin n'a pas pu armer dans OMSI le hook de démarrage headless à usage unique. | Diagnostic de session (`Failed`) | Vérifiez le build ; signalez le problème. |
| `OL_E_NO_ACTIVE_SESSION` | Client CLI (`ReportForwarded`, `CliEventWatch`) | Aucun propriétaire ne répond sur le pipe de contrôle de l'installation (pas de session, ou le propriétaire est encore en cours de démarrage/validation). | Enveloppe CLI, sortie 4 | Démarrez une session, ou attendez qu'elle soit `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` s'est écoulé avant `plugin.started` (OMSI n'a pas chargé `plugins\OmsiLaunch.Plugin.opl`, ou est bloqué avant l'initialisation du plugin). | Diagnostic de session (`Failed`) | Vérifiez la closure du plugin, `plugins\OmsiLaunch.Plugin.opl` et le `logfile.txt` d'OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` sur `plugin.handoff.invalid` ou JSON de télémétrie non analysable | Le plugin n'a pas pu lire/vérifier le handoff de démarrage (versions 3 à 6, SHA-256), ou a envoyé une télémétrie invalide. | Diagnostic de session (`Failed`) | Alignez les versions de l'hôte et du plugin (réinstallez le paquet). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Un propriétaire répond déjà à `session.status` pour cette installation. | Enveloppe CLI, sortie 7 | Utilisez les commandes client (`session status`, `session stop`, commandes runtime). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | L'état de la session n'est pas `Running`. | Levé (`InvalidOperationException`) | Appelez d'abord `WaitForAsync(session, SessionState.Running, ...)`. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Le plan d'écran de démarrage/ITX n'a pas pu être construit ; le message contient le code de présentation. | Diagnostic de plan | Voir [Présentation](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (boîte de dialogue d'OmsiLaunchW) | Code de repli affiché lorsqu'un plan de lancement n'est pas exécutable ou que la session n'a pas atteint le jeu, et qu'aucun diagnostic `OL_E_` n'existe. | Boîte de message uniquement | Lisez `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` sur `world.situation.failed` | Le démarrage natif de la situation enregistrée a renvoyé un échec (`native_status` dans l'événement). | Diagnostic de session (`Failed`) | Vérifiez le `.osn` et sa carte. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | Le plugin a démarré, mais `Running` n'a pas été atteint dans le délai `StartupTimeoutSeconds`. | Diagnostic de session (`Failed`) | Augmentez `/startup-timeout` pour les grandes cartes ; consultez `RuntimeEvents` pour le dernier événement du monde. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Toute exception dans le chemin de démarrage ; le message est le message interne (commençant généralement par le code interne). | Diagnostic de session (`Failed`) | Agissez selon le code interne. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` sur `world.failed` | Le démarrage natif NEW_MAP a renvoyé un échec (`native_status` dans l'événement). | Diagnostic de session (`Failed`) | Vérifiez la carte, l'index du point d'entrée et les journaux d'OMSI. |

<a id="sessionprofile"></a>
## Profil de session

Tous levés par `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) ou `CliInput`, sous forme de `SessionProfileException` (une `IOException` avec `Code`), sortie CLI 2. Voir [profils de session](session-profiles.md).

| Code | Signification / cause typique | Que faire |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | Le répertoire `assets` de l'écran de démarrage du préréglage ou le fichier `profile` des textures Internet n'existe pas dans le paquet. | Ajoutez la ressource. |
| `OL_E_SESSION_PROFILE_INVALID` | Violation de structure ou de limite : plus de 256 KiB, pas exactement un mapping racine, ancres YAML, clé inconnue, clé obligatoire manquante, valeur non scalaire là où un scalaire est requis, `id` différent du nom du répertoire, préréglages hors de 1..99 ou `index` en double, timeouts non positifs, mode météo/écran de démarrage/textures Internet non pris en charge, date/heure autre que `explicit`, YAML invalide, erreurs d'analyse de nombre/date. | Corrigez le YAML selon le message. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` n'est pas vide et ne contient pas la carte sélectionnée (NEW_MAP) ou la carte de la situation sélectionnée (SAVED_SITUATION). | Choisissez un monde compatible. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` n'existe pas. | Vérifiez l'identifiant. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Un argument CLI explicite cible un champ détenu par le profil/préréglage sélectionné. | Retirez l'option ou choisissez un autre préréglage. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | L'identifiant contient `\`, `/`, `:` ou `..` ; ou un chemin de ressource est absolu (enraciné), sort du paquet ou traverse une jonction/un lien symbolique. | Gardez les chemins à l'intérieur du paquet. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` manquant, hors de 1..99, ou non déclaré par le profil. | Utilisez un index de préréglage déclaré. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` n'est pas `omsilaunch.session-profile/v1`. | Utilisez le schéma pris en charge. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Une clé de paramètre d'un préréglage est connue mais n'est pas modifiable. | Supprimez la clé. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Une clé de paramètre d'un préréglage ne figure pas dans le catalogue. | Utilisez une clé du catalogue. |

## Transaction

Voir [transactions et récupération](../concepts/transactions-and-recovery.md).

| Code | Levé par | Signification / cause typique | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Un `closecheck` obsolète existe toujours après `File.Delete`. | Diagnostic de session via `OL_E_START_SESSION` | Supprimez `<root>\closecheck` manuellement (autorisations). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Un chemin qui n'existait pas avant la session contient maintenant un contenu différent de celui appliqué par la session ; il n'est pas supprimé et le journal est conservé. | Levé (`IOException`) ; dans `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION` ; sortie CLI 8 | Examinez le fichier ; supprimez-le ou déplacez-le, puis lancez `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (journal antérieur aux empreintes) | Le journal n'a pas d'empreinte du contenu appliqué pour un chemin initialement absent qui n'est pas une suppression, la propriété ne peut donc pas être prouvée. Au démarrage d'une session, la récupération est différée et retentée avec les octets planifiés de cette session ; via `RecoverPendingAsync`, l'erreur est levée. | Levé (`IOException`) ; sortie CLI 8 | Démarrez une session avec la même spec (qui fournit les octets), ou examinez et supprimez le fichier, puis lancez `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | Le SHA-256 d'une sauvegarde ne correspond pas à l'instantané enregistré dans le journal ; rien n'est écrit. | Levé ; dans `OL_E_RESTORE_FAILED` ; sortie CLI 8 | Restaurez le fichier à partir de votre propre sauvegarde ; ne supprimez ensuite le journal que si vous en êtes certain. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Des instantanés existent en mémoire, mais `journal.json` a disparu (supprimé pendant la session). | Levé ; dans `OL_E_RESTORE_FAILED` | Vérifiez manuellement les fichiers de la session. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` existe toujours après la suppression (la restauration elle-même a réussi et a été vérifiée). | Levé ; dans `OL_E_RESTORE_FAILED` ; sortie CLI 8 | Supprimez `<root>\.omsilaunch\journal.json` (autorisations) ou relancez `/recover` (idempotent). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (échec du démarrage / superviseur) | La fin d'OMSI n'a pas pu être confirmée ; les fichiers n'ont donc pas été remplacés tant qu'il pouvait encore les utiliser ; le journal est conservé. | Diagnostic de session (`Failed`) | Une fois `Omsi.exe` terminé, lancez `/recover` (ou le démarrage suivant effectue la récupération automatiquement). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (échec du démarrage / superviseur) | `RestoreAsync` a levé une exception ; le message contient le code interne ; le journal est conservé. | Diagnostic de session (`Failed`) ; sortie CLI 8 lorsqu'il est levé depuis `/recover` | Agissez selon le code interne, puis lancez `/recover`. |

<a id="warning"></a>
## Avertissement

| Code | Levé par | Signification | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | L'exécutable OMSI 2 n'a pas de disposition connue (son identité de code n'est pas dans le catalogue des builds). Le plan reste exécutable : chaque capacité native est résolue et vérifiée dans le processus, et une capacité qui ne peut pas être vérifiée est indisponible. | Diagnostic de plan (exécutabilité non affectée) | Rien à faire ; consultez `/plan` et `profiles` pour les capacités prédites. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (superviseur) | `Behavior.CooperativeShutdown` a été demandé mais OMSI n'a pas pu être invité à se fermer (sa routine de sortie n'est pas résolue sur ce build, ou OMSI est déjà en cours de fermeture), ou il ne s'est pas fermé dans `ShutdownTimeoutSeconds` : OMSI a été terminé à la place. La restauration s'exécute comme d'habitude. | Diagnostic de session | Rien à faire : la session se termine comme avec un arrêt forcé. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Une capacité dont le démarrage a besoin n'a pas pu être résolue à partir du fichier exécutable (par exemple un exécutable protégé par DRM dont l'image chargée diffère du fichier). Elle est résolue dans le processus au démarrage. | Diagnostic de plan (exécutabilité non affectée) | Rien à faire ; un démarrage échoue avec `OL_E_CAPABILITY_UNRESOLVED` s'il ne peut pas non plus la résoudre dans le processus. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Un module qui a rejoint la session de lui-même (fichier de paramètres, activation par le contenu ou par défaut) ne peut pas être chargé (`Data["code"]` en donne la raison, `Data["activation"]` la façon dont il a rejoint la session). | Diagnostic de plan (exécutabilité non affectée) | Corrigez, désactivez ou supprimez le module. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Une entrée de `.omsilaunch\modules.ini` est inutilisable (pas une section de module, hors section, clé inconnue, valeur invalide ; la valeur par défaut est utilisée), ou des paramètres ont été demandés pour un module que la session ne charge pas. | Diagnostic de plan (exécutabilité non affectée) | Corrigez l'entrée ou la demande. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Un chemin de suppression de session (cible ITX, `Texture\standard.ipr`, `closecheck`) n'existait pas avant la session et existe maintenant, mais aucun processus OMSI n'a jamais démarré sous ce journal, de sorte que le fichier ne peut pas être un sous-produit de la session. Il est conservé et signalé (message = chemin relatif, `Data["sha256"]`) ; la transaction se termine malgré tout. | Diagnostic de session / `RecoveryStatus.Diagnostics` (état non affecté) | Examinez le fichier ; supprimez-le vous-même s'il n'est pas souhaité. |

## Module

Modules runtime (voir [modules runtime](runtime-modules.md)). À la planification, un code est un diagnostic de plan : pour un module demandé, il rend le plan non exécutable ; pour un module qui a rejoint la session de lui-même (fichier de paramètres, contenu, défaut), le plan signale plutôt `OL_W_MODULE_NOT_LOADED`. Dans une session, un code est un diagnostic de session (la session continue sauf si le code est `OL_E_MODULE_SESSION_FAILED`), et pour une opération de module c'est le résultat runtime.

| Code | Levé par | Signification | Canal | Que faire |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` est absent, n'est pas un `omsilaunch.runtime-module/v1` valide, ou son `id` diffère du nom de son répertoire. Le message nomme le champ. | Diagnostic de plan ; `ListRuntimeModulesAsync` | Corrigez le manifeste. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Un id de module demandé n'est pas installé sous `plugins\OmsiLaunch.Modules`. | Diagnostic de plan | Consultez `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Un fichier épinglé est absent, diffère de son SHA-256 épinglé ou est atteint par un point d'analyse ; ou, dans le processus, `module.json` a changé après la planification. | Diagnostic de plan ; diagnostic de session | Réinstallez le module. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | La plage `bridge_protocol` du module n'inclut aucun des protocoles que ce build sert (1 et 2). | Diagnostic de plan | Utilisez une version du module conçue pour cet OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Une capacité requise est inconnue de ce build, indisponible sur cette installation, ou non résolue dans le processus. | Diagnostic de plan ; diagnostic de session | Voir [capacités](capabilities.md) et `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | La session n'exécute pas la carte liée du module, ou un fichier lié diffère de son SHA-256 épinglé. | Diagnostic de plan ; diagnostic de session | Démarrez la carte liée avec le contenu lié. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | Le module a demandé le démarrage d'un fichier qui n'est pas l'un de ses composants auxiliaires. | Résultat runtime à l'intérieur du module | Défaut du module. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | Le composant managé n'a pas pu être chargé ou son type d'entrée créé ; ou le programme de session du module n'a pas pu être démarré ou s'est terminé avant d'être prêt. | Diagnostic de session | Vérifiez le build du module (compatible x86, le type d'entrée implémente `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` ou `Stop` a levé une exception. | Diagnostic de session | Défaut du module ; son `failure_policy` s'est appliqué. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Une phase ou une opération est revenue après son budget (`timeouts`, `timeout_ms`), ou un programme de session n'était pas prêt dans `prepare_ms`. | Diagnostic de session ; résultat runtime | Défaut du module ; son `failure_policy` s'est appliqué. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | Le module a été désactivé ou a échoué plus tôt dans la session. | Résultat runtime | Consultez les diagnostics de session pour le premier échec. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | La session n'a pas chargé ce module, ou il n'est pas encore prêt (avant le gameplay). | Résultat runtime | Demandez le module au démarrage ; attendez `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Le module ne déclare pas une telle opération. | Résultat runtime | Vérifiez les `operations` du module. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | L'opération a échoué sans code catalogué ; le code propre à un module figure dans `Values["module_code"]`. | Résultat runtime | Propre au module. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Après l'arrêt ou un échec, une ressource suivie était encore active (un processus auxiliaire qui ne s'est pas terminé, un thread d'arrière-plan qui ne s'est pas arrêté) ou sa libération a levé une exception. | Diagnostic de session | Signalez le module ; le processus se termine de toute façon avec OMSI. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Un module dont le `failure_policy` est `fail-session` a échoué ; la session est arrêtée et restaurée. | Diagnostic de session (état `Failed`) | Consultez le code de module précédent. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Un paramètre de module demandé est invalide ou non déclaré par le module, un module est à la fois activé et désactivé, un nom de paramètre n'est pas `<module-id>.<key>`, ou une option de module est déclarée par plusieurs modules. | Diagnostic de plan (non exécutable) ; erreur CLI | Corrigez la demande ; `/list:RuntimeModules` affiche les paramètres de chaque module. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Un plugin OMSI que le module liste sous `conflicts.plugins` est activé (`plugins\*.opl`), ou chargé dans OMSI. La raison nomme le `.opl`. | Diagnostic de plan ; diagnostic de session | Désactivez ou supprimez ce plugin. OmsiLaunch ne le modifie jamais. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | réservé | Réservé ; un composant natif reçoit à la place les résultats numériques de hook décrits dans [modules runtime](runtime-modules.md#native-components-and-hooks). | aucun | aucune |

<a id="non-error-diagnostic-codes"></a>
## Codes de diagnostic hors erreur

| Code | Émis par | Message / données | Signification |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | message = PID ; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` a été créé et son identité enregistrée. Diagnostic de session. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | message = SHA-256 du fichier supprimé | Un `closecheck` qui existait avant la session a été définitivement supprimé (`SuppressStaleClosecheckWarning = true`). Diagnostic de session. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | message = chemin relatif ; `Data["sha256"]` | Un chemin de suppression de session a été recréé par OMSI pendant une session dont le processus avait démarré ; il a été supprimé pour rétablir l'absence d'origine. Diagnostic de session / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | message = `manifest` ou `self` | Référence utilisée par la validation du plugin permanent. Diagnostic de plan. |
| `module.planned` | `SessionPlanner` | message = id du module ; `Data["module"]`, `Data["status"]`, `Data["activation"]` | Le module figure dans le plan. Diagnostic de plan. |
| `module.disabled` | `SessionPlanner` | message = id du module | Un module de contenu ou par défaut a été tenu à l'écart par une désactivation (demande ou fichier de paramètres). Diagnostic de plan. |
| `session_profile.selected` | `SessionPlanner` | message = identifiant du profil ; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Provenance d'une session compilée à partir d'un profil de session. Diagnostic de plan. |

Les noms d'événements runtime (`RuntimeEvent.Type`, qui ne sont pas des diagnostics) sont listés dans le [cycle de vie de la session](../concepts/session-lifecycle.md#telemetry-events).
