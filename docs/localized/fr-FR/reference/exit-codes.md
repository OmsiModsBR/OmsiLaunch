# Codes de sortie

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Traduction de la [page originale en anglais](../../../reference/exit-codes.md) d’OmsiLaunch 0.1.0-beta3. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page répertorie chaque code de sortie de processus que `OmsiLaunch.exe` et `OmsiLaunchW.exe` peuvent renvoyer : le contrat managé public `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), les codes du shim natif `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` et `OmsiLaunch.WindowsHost.cpp`), et les règles de classification que `CliProgram.Classify` applique à toute exception non interceptée (`tools\OmsiLaunch.Cli\Program.cs`). Les appelants doivent déduire la sémantique du code et de l'enveloppe d'erreur structurée, jamais du texte du message. Les codes d'erreur sont catalogués dans [erreurs](errors.md) ; les commandes qui produisent chaque code figurent dans la [référence de la CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Codes de sortie publics (`PublicExitCode`)

| Code | Nom de l'enum | Signification | Quand |
|---|---|---|---|
| 0 | `Success` | La commande s'est terminée. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status` ; `/plan`/`/validate` avec un plan exécutable ; une session qui s'est terminée dans l'état `Completed` ; `/silent` une fois `OmsiLaunchW.exe` démarré ; une commande client transmise à laquelle le propriétaire a répondu avec `Ok=true` ; `/recover` lorsque rien n'était en attente ou que la restauration s'est terminée. |
| 1 | `SessionFailed` | Un plan n'était pas exécutable, ou une session détenue s'est terminée dans l'état `Failed`. | `/plan` indiquant `NOT RUNNABLE` ; un lancement dont le plan n'est pas exécutable (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ... ; `OmsiLaunchW.exe` affiche en outre le dernier diagnostic `OL_E_` dans une boîte de message) ; `OL_E_PLAN_NOT_RUNNABLE` levé par `StartSessionAsync` (nouvelle planification au démarrage) ; la session n'a pas atteint `Running` dans le timeout de démarrage ; la session s'est terminée dans l'état `Failed`. |
| 2 | `InvalidArguments` | La ligne de commande, la spec, le profil ou les arguments runtime ont été rejetés avant ou pendant la répartition. | Option ou route inconnue, valeur manquante, valeur hors plage ; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`) ; `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` ; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` ; `OL_E_ITX_PROFILE_REQUIRED` ; `OL_E_RUNTIME_OPERATION_UNKNOWN` et `OL_E_RUNTIME_ARGUMENT_REQUIRED` (localement ou renvoyés par le propriétaire) ; aide d'utilisation affichée pour une commande impossible à répartir ; toute `ArgumentException`, `FormatException`, `InvalidDataException` ou `OverflowException`. |
| 3 | `UnsupportedProfile` | La plateforme ou le build d'OMSI n'est pas pris en charge. | Une exception non interceptée dont le code commence par `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Notez que les mêmes conditions détectées pendant la planification rendent le plan non exécutable et renvoient `1` à la place. |
| 4 | `NoActiveSession` | Une commande client n'a trouvé aucun propriétaire. | `session status`, `session stop`, `events read`, `events watch`, ou une opération runtime transmise lorsque le point de terminaison de contrôle local de cette installation ne répond pas (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Un timeout a remonté sous forme d'exception. | Toute `TimeoutException` (`OL_E_TIMEOUT` lorsque le message ne contient aucun code, sinon le code intégré, par exemple `OL_E_RUNTIME_REQUEST_TIMEOUT`). Les timeouts client transmis reçoivent du propriétaire une réponse `Ok=false` et renvoient `7`, et non `5`. |
| 6 | `NotFound` | Un fichier ou un répertoire est introuvable. | `FileNotFoundException` / `DirectoryNotFoundException` (`OL_E_NOT_FOUND` par défaut), par exemple `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` lorsqu'il est levé en tant qu'exception, un répertoire d'installation absent pendant `/list`. |
| 7 | `OperationRejected` | La commande était valide mais a été refusée, ou une commande transmise a échoué côté propriétaire. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED` ; toute réponse de contrôle `Ok=false` autre que les deux codes d'argument (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`) ; toute autre exception non interceptée portant un code `OL_E_` qui n'est pas classé ailleurs (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Une transaction durable n'a pas pu être restaurée. | `/recover` lorsque le journal était en attente et le reste ; toute exception non interceptée dont le code commence par `OL_E_RECOVERY_` ou vaut `OL_E_RESTORE_FAILED` (par exemple `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` pendant la restauration propre à une session). |
| 10 | `InternalError` | Une exception inattendue sans code `OL_E_`. | Signalée comme `OL_E_INTERNAL` avec la catégorie `internal` ; le message est le texte de l'exception. |

Le code `9` n'est pas attribué.

<a id="native-shim-exit-codes"></a>
## Codes de sortie du shim natif

Renvoyés par `OmsiLaunch.exe` / `OmsiLaunchW.exe` avant l'exécution du contrôleur managé. Ils sont disjoints de `PublicExitCode`, de sorte qu'un appelant peut distinguer un échec de démarrage de l'hôte d'un résultat du contrôleur. `OmsiLaunchW.exe` affiche en outre `OmsiLaunch could not start the .NET host (code N).` dans une boîte de message.

| Code | Signification | Cause |
|---|---|---|
| 100 | Le chemin de l'exécutable n'a pas pu être résolu | `GetModuleFileNameW` a échoué. |
| 101 | La ligne de commande n'a pas pu être découpée en arguments | `CommandLineToArgvW` a renvoyé null. |
| 102 | La recherche de l'emplacement de `hostfxr` a échoué | La requête de taille de `get_hostfxr_path` a échoué : aucun runtime .NET correspondant n'est installé (le runtime .NET 6 x64 est requis). |
| 103 | Le chemin de `hostfxr` n'a pas pu être récupéré | Le second appel à `get_hostfxr_path` a échoué. |
| 104 | La bibliothèque `hostfxr` n'a pas pu être chargée | `LoadLibraryW` sur le `hostfxr.dll` résolu a échoué. |
| 105 | Des exports requis de `hostfxr` sont manquants | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` ou `hostfxr_close` introuvable. |
| 106 | L'hôte managé n'a pas pu être initialisé | `hostfxr_initialize_for_dotnet_command_line` a échoué pour `OmsiLaunch.Controller.dll` (`OmsiLaunch.Controller.runtimeconfig.json` manquant, `Microsoft.WindowsDesktop.App` 6.0 x64 manquant, ou paquet endommagé). |

<a id="classification-rules-cliprogramclassify"></a>
## Règles de classification (`CliProgram.Classify`)

Chaque exception qui sort de `CliProgram.RunAsync` est transformée en enveloppe d'erreur (`CliInput.WriteError`) et en code de sortie par `CliProgram.ReportFailure`, qui appelle `Classify`. Les échecs d'analyse sont traités de la même manière avant la répartition (sortie `2`). Les règles s'appliquent dans cet ordre :

1. Le premier jeton `OL_E_` du message de l'exception est extrait (`ExtractCode`) : le code est la plus longue suite de lettres ASCII, de chiffres et de `_` commençant à `OL_E_`. Les codes sont restitués tels quels dans `error.code`.
2. `SessionProfileException` → son propre `Code`, catégorie `invalid_argument`, sortie `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → code extrait ou `OL_E_INVALID_ARGUMENT`, catégorie `invalid_argument`, sortie `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → code extrait ou `OL_E_NOT_FOUND`, catégorie `not_found`, sortie `6`.
5. `TimeoutException` → code extrait ou `OL_E_TIMEOUT`, catégorie `runtime`, sortie `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, catégorie `session`, sortie `7`.
7. Sinon, lorsqu'un code a été extrait :
   - commence par `OL_E_RECOVERY_` ou est égal à `OL_E_RESTORE_FAILED` → catégorie `transaction`, sortie `8` ;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → catégorie `session`, sortie `7` ;
   - `OL_E_PLAN_NOT_RUNNABLE` → catégorie `session`, sortie `1` ;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → catégorie `invalid_argument`, sortie `2` ;
   - commence par `OL_E_UNSUPPORTED_` → catégorie `unsupported_profile`, sortie `3` ;
   - tout autre code → catégorie `runtime` pour `InvalidOperationException` et `IOException`, sinon `internal` ; sortie `7`.
8. Aucun code → `OL_E_INTERNAL`, catégorie `internal`, sortie `10`.

Les réponses client transmises contournent `Classify` : `CliProgram.ReportForwarded` renvoie `4` en l'absence de point de terminaison, `2` pour `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` pour toute autre réponse `Ok=false`, et `0` pour `Ok=true`.

<a id="scripting-guidance"></a>
## Conseils pour les scripts

- Considérez `0` comme un succès et toute autre valeur comme un échec ; branchez sur le code numérique, puis sur `error.code` de l'enveloppe `--json`.
- Un lancement de session ne rend la main qu'une fois la session terminée et ses fichiers restaurés ; `1` signifie que la transaction s'est exécutée mais qu'OMSI a échoué ou que le plan a été rejeté, et non que des fichiers sont restés modifiés (un journal résiduel est signalé par `/recovery-status`).
- `100`..`106` signifient que le paquet ou le runtime .NET est défectueux ; voir [installation](../getting-started/installation.md) et [empaquetage](packaging.md).
