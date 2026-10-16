# Référence de OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Traduction de la [page originale en anglais](../../../reference/omsilaunchw.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

`OmsiLaunchW.exe` est l'hôte du sous-système Windows (GUI) du contrôleur OmsiLaunch. Il accepte la même ligne de commande que `OmsiLaunch.exe` et exécute le même code de contrôleur (`OmsiLaunch.Controller.dll`). La seule différence réside dans la manière dont il rend compte : il n'y a pas de fenêtre de console, les échecs sont affichés sous forme de boîtes de message et une session en cours n'est visible que par son [icône de la zone de notification](windows-tray.md).

Sources qui font foi : `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (le shim natif), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) et `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Comparaison de OmsiLaunch.exe et OmsiLaunchW.exe

| Aspect | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Sous-système | Console. Ouvre une fenêtre de console lorsqu'il est démarré depuis l'Explorateur. | Windows (GUI). Pas de fenêtre de console. |
| Shim natif | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Environnement | inchangé | définit `OMSILAUNCH_WINDOWS_HOST=1` pour le processus du contrôleur avant le démarrage de .NET |
| Arguments | découpés avec `CommandLineToArgvW` et transmis inchangés au contrôleur | identique, de sorte que les deux hôtes acceptent exactement les mêmes options et commandes ([référence CLI](cli.md)) |
| Sortie texte | écrite sur stdout | supprimée, sauf si `--json` est indiqué (les enveloppes JSON sont alors écrites sur stdout, qu'un appelant peut rediriger) |
| Erreurs | `OL_E_...: message` ou une enveloppe JSON d'erreur | les mêmes règles de sortie console, **plus** une boîte de message pour chaque erreur (voir [Boîtes de dialogue d'échec](#failure-dialogs)) |
| `/silent` | démarre `OmsiLaunchW.exe` avec les autres arguments et renvoie `0` | ignoré : la commande s'exécute déjà dans l'hôte Windows |
| Icône de notification | affichée pour une session propriétaire | affichée pour une session propriétaire |
| Chemins d'arrêt | zone de notification, `session stop`, sortie d'OMSI, `/observe-seconds`, Ctrl+C, fermeture de la console | zone de notification, `session stop`, sortie d'OMSI, `/observe-seconds` (il n'y a pas de console, donc Ctrl+C et la fermeture de la console ne s'appliquent pas) |
| Codes de sortie | [`PublicExitCode`](exit-codes.md) `0`..`10`, codes du shim `100`..`106` | les mêmes codes |

<a id="how-it-starts"></a>
## Démarrage interne

1. Le shim résout son propre chemin (`GetModuleFileNameW`) et attend `OmsiLaunch.Controller.dll` dans le même répertoire.
2. Il découpe la ligne de commande (`CommandLineToArgvW`) et définit `OMSILAUNCH_WINDOWS_HOST=1`.
3. Il localise `hostfxr` au moyen du `nethost.dll` fourni dans le paquet, le charge, initialise le contrôleur avec les arguments (le chemin du contrôleur ne fait pas partie de la liste d'arguments que voit l'analyseur de la CLI) et l'exécute.
4. Le shim renvoie le code de sortie du contrôleur sans le modifier.

Si une étape antérieure à l'exécution du contrôleur échoue, le shim affiche une boîte de message intitulée `OmsiLaunch` avec le texte `OmsiLaunch could not start the .NET host (code N).` et se termine avec ce code :

| Code | Étape en échec |
| --- | --- |
| `100` | le chemin de l'exécutable n'a pas pu être résolu |
| `101` | la ligne de commande n'a pas pu être découpée |
| `102` | la recherche de l'emplacement de `hostfxr` a échoué (généralement : le runtime .NET 6 x64 n'est pas installé) |
| `103` | le chemin de `hostfxr` n'a pas pu être récupéré |
| `104` | `hostfxr` n'a pas pu être chargé |
| `105` | des exports requis de `hostfxr` sont manquants |
| `106` | l'hôte managé n'a pas pu être initialisé (par exemple `OmsiLaunch.Controller.dll` ou sa configuration de runtime est manquant, ou le runtime Windows Desktop est absent) |

`OmsiLaunch.exe` utilise le même tableau mais n'affiche rien. Ces boîtes de dialogue n'ont pas été produites à l'exécution (voir [état de la validation à l'exécution](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Lancement

Démarrage direct, depuis un raccourci, un script ou un autre programme :

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Par l'intermédiaire de `OmsiLaunch.exe` avec `/silent` :

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Placez les exécutables dans l'installation d'OMSI 2 (la disposition du paquet, voir [empaquetage](packaging.md)) ; sans argument d'installation, l'installation est le répertoire qui contient l'exécutable. Une installation explicite est transmise comme premier argument, comme avec `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Comme il s'agit d'un programme GUI, `cmd.exe` et l'Explorateur ne l'attendent pas. Pour l'attendre et lire le code de sortie depuis un script, utilisez `start /wait OmsiLaunchW.exe ...` dans `cmd.exe` ou `Start-Process -Wait -PassThru` dans PowerShell :

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Délégation /silent

`OmsiLaunch.exe ... /silent` (ou `--silent`), lorsqu'il ne s'exécute pas déjà sous `OmsiLaunchW.exe`, effectue les opérations suivantes (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`) :

1. Il recherche `OmsiLaunchW.exe` dans le répertoire de `OmsiLaunch.exe`. S'il est absent : `OL_E_WINDOWS_HOST_MISSING`, sortie `7`.
2. Il démarre `OmsiLaunchW.exe` via `ShellExecute` (`UseShellExecute = true`) avec le répertoire courant et tous les arguments sauf `/silent`/`--silent`, dans l'ordre d'origine. `ShellExecute` ne transmet pas les handles de l'appelant au nouveau processus, de sorte qu'un appelant qui capture la sortie de `OmsiLaunch.exe /silent` n'est pas bloqué pendant toute la durée de la session (clôture runtime BUG-03). Si aucun processus n'est renvoyé : `OL_E_WINDOWS_HOST_START_FAILED`, sortie `7`.
3. Il écrit l'enveloppe `silent` et se termine immédiatement avec `0` :

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

La sortie `0` signifie seulement que `OmsiLaunchW.exe` a été démarré. Le résultat de la session (erreurs de planification, un propriétaire déjà actif, un démarrage en échec) est signalé par `OmsiLaunchW.exe` avec ses propres boîtes de dialogue et diagnostics, et les deux processus sont ensuite indépendants : `OmsiLaunch.exe` s'est terminé, et `OmsiLaunchW.exe` est le propriétaire de la session. Utilisez `OmsiLaunch.exe session status` pour voir la session.

`/silent` est appliqué avant toute autre commande, de sorte que `OmsiLaunch.exe /silent session status` exécute aussi `session status` dans `OmsiLaunchW.exe`, où sa sortie est supprimée. N'utilisez `/silent` que pour les lancements.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Effet de chaque commande sous OmsiLaunchW.exe

| Ligne de commande | Résultat |
| --- | --- |
| aucun argument | exécute `detect` silencieusement et se termine avec `0` (rien n'est affiché) |
| un lancement (`/new`, `/saved:...`, `/spec:...`, un profil de session) avec un plan exécutable et sans propriétaire | devient le propriétaire de la session : icône de notification pendant le démarrage et l'exécution ; se termine à la fin de la session (`0` terminée, `1` en échec) |
| un lancement dont le plan n'est pas exécutable | boîte de dialogue avec le dernier diagnostic `OL_E_` du plan (à défaut `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), sortie `1` (audit de documentation BUG-06 ; avant la correction, il se terminait silencieusement) |
| un lancement alors qu'un propriétaire est déjà actif pour l'installation | boîte de dialogue `OL_E_SESSION_ALREADY_ACTIVE`, sortie `7` ; la session en cours n'est pas affectée |
| un lancement qui n'atteint pas `Running` | boîte de dialogue `The OMSI session did not reach gameplay.` avec le dernier diagnostic `OL_E_`, sortie `1`. OMSI est arrêté et les fichiers sont restaurés par le superviseur de session pendant que la boîte de dialogue est ouverte ; le processus se termine après la fermeture de la boîte de dialogue et l'achèvement de `CloseAsync` |
| un argument invalide, une option inconnue ou un profil de session invalide | boîte de dialogue avec `OL_E_INVALID_ARGUMENT` ou le code `OL_E_SESSION_PROFILE_*`, sortie `2` |
| une commande client (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) sans propriétaire | boîte de dialogue `OL_E_NO_ACTIVE_SESSION`, sortie `4` |
| une commande client que le propriétaire rejette | boîte de dialogue avec le code de rejet (par exemple `OL_E_CONTROL_SESSION_MISMATCH` ou un code d'erreur runtime), sortie `7` (`2` pour une opération inconnue ou un argument manquant) |
| une commande client ou de découverte réussie (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | aucune sortie visible, sauf si `--json` est indiqué et que stdout est redirigé ; code de sortie identique à celui de `OmsiLaunch.exe` |
| `/plan` ou `/validate` | aucune boîte de dialogue, même pour un plan non exécutable ; sortie `0` ou `1` |
| tout autre échec du contrôleur | boîte de dialogue avec le code `OL_E_` classifié ; code de sortie selon les [codes de sortie](exit-codes.md) |

<a id="failure-dialogs"></a>
## Boîtes de dialogue d'échec

Chaque erreur que `OmsiLaunch.exe` afficherait est également présentée dans une boîte de message modale (`WindowsHost.ShowFailure`), même lorsque `--json` est indiqué :

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` est le message d'erreur ou, pour un échec de session, le message du dernier diagnostic `OL_E_`. Limitation connue : pour un échec signalé par le plugin, le message est la charge utile brute de l'échec du plugin (par exemple `{"name":"world.failed",...}`) ; la ligne `Code:` est correcte (voir [limitations connues](known-limitations.md)). La boîte de dialogue est modale et le processus se termine après sa fermeture. Preuves d'exécution : erreur d'argument, absence de session active et échec avant le gameplay (clôture runtime `T04`).

<a id="session-tray-and-exit"></a>
## Session, zone de notification et sortie

Une session dont `OmsiLaunchW.exe` est propriétaire se comporte exactement comme une session dont `OmsiLaunch.exe` est propriétaire (voir [cycle de vie de la session](../concepts/session-lifecycle.md)) :

- L'icône de notification apparaît dès que la session a démarré, avant qu'OMSI n'atteigne le gameplay, sauf si `Presentation.SuppressTrayIcon` est défini dans un fichier `/spec`. Avec `SuppressTrayIcon`, il n'y a aucune surface visible ; arrêtez la session avec `OmsiLaunch.exe session stop` ou en fermant OMSI.
- La session se termine lorsque OMSI se ferme, lorsque `End session` (`Terminer la session`) est confirmé dans la zone de notification, lorsqu'un client envoie `session stop` ou lorsque `/observe-seconds` est écoulé. OmsiLaunch arrête alors OMSI s'il est encore en cours d'exécution, restaure chaque fichier qu'il a modifié, libère le bail d'installation, retire l'icône de notification et se termine.
- Si `OmsiLaunchW.exe` lui-même est tué, le démarrage suivant d'OmsiLaunch pour cette installation récupère la transaction en attente (voir [transactions et récupération](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Démarrage rapide

1. Installez le paquet dans le répertoire d'OMSI 2 ([installation](../getting-started/installation.md)).
2. Créez un raccourci vers `OmsiLaunchW.exe` avec les arguments de la session, par exemple `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Lancez-le. OMSI démarre sans fenêtre de console ; l'icône d'OmsiLaunch apparaît dans la zone de notification.
4. Cliquez avec le bouton droit sur l'icône → `Status` (`État`) pour voir la session, ou `End session` → `End session` (`Terminer la session`) pour la terminer.
5. En cas de problème, la boîte de dialogue affiche le code d'erreur ; les détails se trouvent dans `<OMSI_PATH>\.omsilaunch\diagnostics`.
