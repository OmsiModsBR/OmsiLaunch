# Installation

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Traduction de la [page originale en anglais](../../../getting-started/installation.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page explique ce que requiert OmsiLaunch `0.1.0-beta4`, quel build d'OMSI il prend en charge, comment le paquet de version s'installe dans la racine d'une installation OMSI et comment vérifier l'installation avec `/version` et `/plan` avant de démarrer une session. Le contenu du paquet est spécifié dans [empaquetage](../reference/packaging.md) ; le premier lancement est décrit dans [première session](first-session.md).

<a id="requirements"></a>
## Prérequis

| Prérequis | Détail | Échec en cas d'absence |
|---|---|---|
| Windows 10 ou ultérieur, 64 bits | Le contrôleur vérifie `Environment.OSVersion.Version.Major >= 10`, un système d'exploitation x64 et un processus x64. | Plan non exécutable avec `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (ou `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), sortie `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` requiert `Microsoft.NETCore.App` 6.0 et `Microsoft.WindowsDesktop.App` 6.0 (Windows Forms est utilisé par l'indicateur de la zone de notification). Les shims le localisent avec `nethost.dll`. | `OmsiLaunch.exe` se termine avec un code de shim `102`..`106` avant toute sortie ; `OmsiLaunchW.exe` affiche `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` requiert `Microsoft.NETCore.App` 6.0 pour x86, car le plugin s'exécute dans le processus 32 bits `Omsi.exe`. Le bundle x86 du .NET 6 Desktop Runtime le satisfait également. | Le plugin ne démarre pas dans OMSI ; la session n'atteint pas `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), sortie `1`, fichiers restaurés. |
| OMSI 2 installé | Un `Omsi.exe` 32 bits d'une version OMSI 2 (2.x), avec `maps\` et `Vehicles\` dans la racine. Aucun hash d'exécutable particulier n'est exigé ; OMSI 2.3.004 est la disposition connue, validée à l'exécution, et sur les autres builds chaque capacité est résolue et vérifiée dans le processus. | `OL_E_UNSUPPORTED_BUILD` ; plan non exécutable, sortie `1`. Une capacité indisponible sur le build échoue avec `OL_E_CAPABILITY_UNRESOLVED`. Voir [compatibilité](../reference/compatibility.md). |
| Racine de l'installation accessible en écriture | La transaction écrit `.omsilaunch\`, des overlays sous `GUI\`, `Texture\` et `options.cfg`, puis les restaure ; la racine doit être accessible en écriture par l'utilisateur courant (évitez `Program Files` sans les autorisations appropriées). | `OL_E_INSTALLATION_NOT_WRITABLE`, sortie `1`. |
| Un utilisateur, un propriétaire par installation | Le bail d'installation `Local\OmsiLaunch.Installation.<sha256(root)>` et le pipe de contrôle sont propres à chaque session de connexion Windows (logon session). | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, sortie `7`. |

Les deux runtimes sont des téléchargements distincts proposés par Microsoft ; installez le Desktop Runtime x64 et le Runtime x86 (ou le Desktop Runtime x86) de .NET 6. Aucun autre composant n'est requis. Aucune donnée ne quitte la machine.

<a id="confirm-the-omsi-build"></a>
## Confirmer le build d'OMSI

Remplacez `<OMSI_PATH>` par votre répertoire OMSI 2 (par exemple `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

Le hash doit être l'un des deux indiqués ci-dessus. Après l'installation, `OmsiLaunch.exe profiles` affiche la même liste avec son statut de validation.

<a id="install-the-package"></a>
## Installer le paquet

1. Téléchargez `OmsiLaunch-0.1.0-beta4.zip` et `OmsiLaunch-0.1.0-beta4.zip.sha256` ; vérifiez la somme de contrôle (`Get-FileHash` doit être égal à la valeur du fichier `.sha256`).
2. Extrayez l'archive **directement dans la racine de l'installation OMSI** (le dossier qui contient `Omsi.exe`). L'archive est organisée pour cette racine :
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` et les autres assemblys du contrôleur `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` à la racine ;
   - la closure du plugin permanent `plugins\OmsiLaunch.*` (9 fichiers) à côté de vos plugins existants, qui ne sont jamais modifiés ;
   - `.omsilaunch\` avec les ressources de l'écran de démarrage, la documentation hors ligne et les exemples.
3. Laissez `release-manifest.json` à côté de `OmsiLaunch.exe`. C'est grâce à lui que chaque démarrage vérifie les fichiers du plugin installés par SHA-256 (`plugin.integrity.reference = manifest`) ; sans lui, seules la présence et la cohérence interne sont vérifiées (`plugin.integrity.reference = self`).
4. Ne déplacez ni ne renommez rien sous `plugins\OmsiLaunch.*` et ne placez pas de binaires du plugin dans `.omsilaunch\`.

La mise à niveau est la même opération : extrayez le nouveau paquet par-dessus les anciens fichiers alors qu'aucune session n'est en cours et qu'aucune récupération n'est en attente (`OmsiLaunch.exe /recovery-status`). Les hashes du plugin et le manifeste doivent toujours provenir du même paquet (sinon `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Vérifier

Exécutez depuis la racine OMSI (l'argument d'installation désigne par défaut le répertoire contenant `OmsiLaunch.exe`) :

```text
OmsiLaunch.exe /version
```
Résultat attendu : `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"` ; sortie `0`. Une sortie `102`..`106` signifie que le runtime .NET 6 x64 est absent ou que le paquet est incomplet.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Résultat attendu : les hashes pris en charge, puis les cartes découvertes dans cette installation ; sortie `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Résultat attendu : `Plan: READY profile=Omsi23004` (sur la disposition connue ; `unrecognized` sinon) et sortie `0` (utilisez n'importe quelle identité de carte issue de `/list:Maps` ; l'index du point d'entrée doit être un index présenté de cette carte, voir `/list:Entrypoints /map:<identity>`). Avec `--json`, le plan liste `TouchedFiles` (les overlays de l'écran de démarrage), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED`, et `RESOLVED_KNOWN_LAYOUT` pour les capacités natives de démarrage sur la disposition connue), `Diagnostics` (y compris `plugin.integrity.reference`) et `IsRunnable`. `Plan: NOT RUNNABLE` avec la sortie `1` indique la raison dans `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). La planification ne démarre jamais OMSI et n'écrit jamais dans l'installation.

<a id="where-things-live-afterwards"></a>
## Emplacement des éléments après l'installation

| Chemin | Contenu |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Trace de l'hôte de chaque session (les 50 sessions les plus récentes sont conservées) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Journal de l'indicateur de la zone de notification |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Présents uniquement tant qu'une transaction est en attente ; voir [transactions et récupération](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Vos profils de session prédéfinis ; voir [profils de session](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Ressources gérées de l'écran de démarrage |
| `<root>\.omsilaunch\docs\` | Cette documentation, hors ligne (commencez par `README.md` ; la référence de la CLI est `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Exemple de LaunchSpec et de profil de session |

<a id="uninstall"></a>
## Désinstaller

Arrêtez toute session, exécutez `OmsiLaunch.exe /recovery-status` (et `/recover` si une récupération est en attente), puis supprimez les fichiers du produit à la racine, `plugins\OmsiLaunch.*` et `.omsilaunch\`. Détails dans [empaquetage](../reference/packaging.md).

<a id="next"></a>
## Suite

[Première session](first-session.md) · [Référence de la CLI](../reference/cli.md) · [limitations connues](../reference/known-limitations.md)
