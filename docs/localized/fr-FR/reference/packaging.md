# Création des paquets et organisation de la version

<!-- l10n: source=reference/packaging.md source-sha256=8d5d5b27225188339094529a2435a976635969dd53d614c53d768169087e7ffa -->
> Traduction de la [page originale en anglais](../../../reference/packaging.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page décrit le paquet de version OmsiLaunch `0.1.0-beta4` : ce que produit `tools\New-ReleasePackage.ps1`, les champs de `release-manifest.json`, la façon dont le contrôleur utilise le manifeste à l'exécution pour vérifier la closure du plugin permanent, la façon dont le paquet est installé dans une racine d'installation OMSI et en est retiré, ce que contient le répertoire `.omsilaunch` après utilisation, et les scripts de validation (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). L'identité du produit provient de `OmsiLaunch.Version.props`. Les étapes d'installation destinées aux utilisateurs se trouvent dans [installation](../getting-started/installation.md) ; le rôle de la closure du plugin à l'exécution est décrit dans [plugin permanent](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Identité du produit (`OmsiLaunch.Version.props`)

| Propriété | Valeur | Utilisée pour |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | `product` du manifeste, `ProductName` Windows |
| `OmsiLaunchCompanyName` | `LMonteiro` | `CompanyName` Windows |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/lmonteirotech/OmsiLaunch` | `LegalCopyright` Windows |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | `product_version` du manifeste, `ProductVersion` Windows, version informative de l'assembly (`/version`), nom du ZIP public |
| `OmsiLaunchManagedVersion` | `0.1.0` | Base de la version des assemblys managés |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Version de l'assembly et version de fichier Windows |
| `OmsiLaunchPackageAlias` | `current` | `package_alias` du manifeste, dossier de préparation et nom du ZIP alias |

`Directory.Build.props` définit `InformationalVersion` à `OmsiLaunchProductVersion` sans révision source, de sorte que `OmsiLaunch.exe /version` affiche exactement `0.1.0-beta4`.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (sortie par défaut `artifacts\release`) prépare un paquet à partir d'artefacts déjà compilés. L'écriture dans `artifacts\release` alors que `OmsiLaunch-<product_version>.zip` existe déjà est refusée sauf si `-AllowOverwritePublished` est fourni ; les paquets candidats vont dans un autre répertoire (la validation hors ligne utilise `artifacts\candidate\post-round-a`).

Avant la préparation, chaque sortie de build est vérifiée pour détecter les artefacts périmés.

- **`OmsiLaunch.Native.x86.dll` : par contenu, pas par horodatage.** Le build natif écrit `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (cible `WriteOmsiLaunchNativeBuildReceipt` dans le `.vcxproj`) : le SHA-256 de la DLL qu'il a produite (`output=`) et de chaque source à partir de laquelle elle a été compilée (`source=<sha256>|<path>` : le `.cpp`, le `.rc`, le `.vcxproj` et `OmsiLaunch.Version.props`). La création du paquet refuse la DLL lorsque son hash n'est pas la sortie enregistrée (`does not match its build receipt` : une copie périmée ou étrangère, quel que soit son horodatage), lorsqu'une source enregistrée a changé (`Native source changed after the recorded build`), lorsqu'une source native n'est pas couverte par le reçu, ou lorsque le reçu est absent.
- **Shims et assemblys managés : par horodatage.** Chacun ne doit pas être plus ancien que les sources de son propre projet (`.cpp`/`.rc`/`.vcxproj` de chaque shim ; le propre projet de chaque assembly managé). Une entrée périmée interrompt la création du paquet avec `Stale build artifact`.

Le paquet est ensuite assemblé dans un répertoire de préparation entièrement neuf et nommé de manière unique (`.staging-<guid>` dans le répertoire de sortie), de sorte qu'aucun fichier d'une exécution précédente ne puisse entrer dans la closure. Le dossier `OmsiLaunch-current` et les archives précédents ne sont remplacés qu'une fois toutes les vérifications ci-dessous réussies.

| Source | Destination dans le paquet |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64) : `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | racine |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\` : `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| `assets\splash\*.bmp` de la CLI (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | racine |
| chaque fichier sous `docs\` sauf `docs\localized\` (la documentation anglaise, même arborescence) | `.omsilaunch\docs\` (de sorte que `.omsilaunch\docs\reference\cli.md`, le chemin affiché par le texte d'aide de la CLI, existe ; audit de la documentation BUG-08). Les liens de `docs\README.md` vers les résumés à la racine du dépôt (`PUBLIC-API.md` et autres) ne se résolvent que dans le dépôt source. |
| `docs\localized\LOCALIZATION-MANIFEST.md` et `docs\localized\<locale>\**` pour chaque locale listée dans ce manifeste (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` et `ja-JP`) | `.omsilaunch\docs\localized\` (même arborescence) ; une locale listée manquante interrompt le script |

Ensuite, il calcule le hash de chaque fichier préparé, écrit `release-manifest.json` à la racine du paquet en UTF-8 **sans** BOM (le résultat ne dépend plus de l'édition de PowerShell), exécute `Test-ReleasePackageIntegrity.ps1` sur la préparation, compare à nouveau chaque fichier de plugin préparé et `OmsiLaunch.Native.x86.dll` avec sa sortie de build, compresse la préparation, **extrait l'archive dans un nouveau répertoire temporaire et valide la closure extraite par rapport au même manifeste** (le manifeste archivé doit être identique octet pour octet au manifeste validé), puis publie la préparation sous le nom `OmsiLaunch-current`, l'archive sous le nom `OmsiLaunch-current.zip`, la copie vers `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) et écrit `OmsiLaunch-0.1.0-beta4.zip.sha256` contenant `<SHA-256>  <file name>`. Tout artefact manquant interrompt le script. Le script ne compile pas ; exécutez d'abord `Invoke-OfflineValidation.ps1` (ou les étapes individuelles `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Organisation du paquet

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Seuls les fichiers produit à la racine, `plugins\OmsiLaunch.*` et `.omsilaunch\` appartiennent au produit. Les plugins tiers sous `plugins\` ne sont jamais énumérés, copiés, hachés, supprimés ni restaurés par OmsiLaunch.

## `release-manifest.json`

| Champ | Type | Signification |
|---|---|---|
| `product` | chaîne | `OmsiLaunch` |
| `product_version` | chaîne | `0.1.0-beta4` |
| `package_alias` | chaîne | `current` |
| `control_protocol` | chaîne | `0.1` ; doit correspondre à `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | chaîne | `OMSI 2` |
| `known_layouts` | objet[] | `build_id` et `code_sha256` de chaque disposition connue (`Omsi23004`, `13ec110a...c028`) ; un chemin rapide, pas une exigence |
| `configuration` | chaîne | `Release` ou `Debug` |
| `generated_utc` | chaîne | Heure du build au format ISO-8601 |
| `files[]` | objet[] | `path` (barres obliques, relatif à la racine du paquet), `bytes`, `sha256` (hexadécimal en majuscules) pour chaque fichier du paquet |

Le manifeste est une donnée, jamais une politique exécutable : le contrôleur ne lit que les entrées `plugins/`. Le lecteur accepte le fichier avec ou sans BOM UTF-8 (les manifestes écrits par Windows PowerShell 5.1 avant cette correction en comportent un).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Utilisation du manifeste à l'exécution (intégrité du plugin)

Avant chaque plan et chaque démarrage, `OmsiLaunchService.LoadArtifacts` construit la closure attendue du plugin (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`) :

1. Le contrôleur recherche `release-manifest.json` à côté de `OmsiLaunch.exe` (`AppContext.BaseDirectory`). S'il est présent, `ReleaseManifest.TryReadPluginHashes` extrait les hashes `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID` si le fichier ne peut pas être lu comme un manifeste).
2. Le hash (SHA-256) de chaque fichier installé `<root>\plugins\OmsiLaunch.*` est calculé et comparé :
   - avec un manifeste : au hash du manifeste ; le diagnostic de plan `plugin.integrity.reference = manifest` est enregistré. Fichier manquant → `OL_E_PERMANENT_PLUGIN_MISSING` ; fichier présent mais non listé → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` ; hash différent → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - sans manifeste (disposition de développement, ou installation dans laquelle le manifeste a été omis) : seules la présence et la cohérence interne par rapport à la copie empaquetée à côté du contrôleur peuvent être vérifiées ; `plugin.integrity.reference = self`.
3. Un échec rend le plan non exécutable (`OL_E_RUNTIME_ARTIFACT_MISSING` avec le détail) ou rejette le démarrage (code de sortie `7`).

Les fichiers du plugin ne sont jamais préparés, capturés dans un instantané, restaurés ni supprimés par une session ; la closure est une partie permanente de l'installation. La DLL x86 `OmsiLaunch.Native.x86.dll` n'est chargée que depuis `plugins\` ; les assemblys managés déclarent `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Installation dans la racine OMSI

1. Vérifiez l'archive : comparez `OmsiLaunch-0.1.0-beta4.zip` avec `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Extrayez l'archive **directement dans la racine de l'installation OMSI** (le répertoire qui contient `Omsi.exe`). Cela place les fichiers à la racine, `plugins\OmsiLaunch.*` (à côté des éventuels plugins tiers) et `.omsilaunch\`.
3. Conservez `release-manifest.json` à côté de `OmsiLaunch.exe` : il permet la vérification d'intégrité du plugin basée sur le manifeste. Le manifeste et les binaires doivent provenir du même paquet : de nouveaux binaires avec un manifeste plus ancien (ou l'inverse) font échouer chaque démarrage avec `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` copie désormais le manifeste avec les fichiers du produit ; auparavant il l'ignorait, ce qui laissait un manifeste plus ancien à côté de binaires plus récents (Round A RA-007).
4. Vérifiez la cohérence en lecture seule avec `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>` : `installation_comparison.coherent_with_package` doit valoir `true`.
5. Ne déplacez pas les binaires du plugin dans `.omsilaunch\` et ne renommez pas `plugins\OmsiLaunch.*`.
6. Vérifiez avec `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` et un `/plan` (voir [première session](../getting-started/first-session.md)).

Les fichiers `.omsilaunch\assets\splash\*.bmp` existants ne sont jamais écrasés par une session (un ensemble de ressources explicitement géré persiste) ; les écraser en extrayant un nouveau paquet est une action délibérée de l'utilisateur.

<a id="the-omsilaunch-directory-after-use"></a>
## Le répertoire `.omsilaunch` après utilisation

| Chemin | Créé par | Durée de vie |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | le paquet, ou copié lors de la première session avec écran de démarrage géré | persistant |
| `docs\`, `examples\` | le paquet | persistant |
| `session-profiles\<id>\profile.yaml` | l'utilisateur | persistant ; voir [profils de session](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | chaque session | conservé pour les 50 sessions les plus récentes ; les anciens fichiers préfixés par une session sont supprimés au démarrage d'une nouvelle session |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, harnais de validation | même rétention (préfixe de session) |
| `diagnostics\tray-host.log` | indicateur de la zone de notification | persistant, complété par ajout |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | persistant (non préfixé par une session) |
| `journal.json` | la transaction | existe de `Prepared` jusqu'à `Restored` ; un fichier restant signifie qu'une récupération est en attente (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | la transaction | instantanés des fichiers touchés ; supprimés après la restauration |

Aucune donnée ne quitte la machine. Voir [transactions et récupération](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Désinstallation

1. Assurez-vous qu'aucune session n'est en cours (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) et qu'aucune récupération n'est en attente (`OmsiLaunch.exe /recovery-status` ; exécutez `/recover` si `pending` vaut `true`), afin que les fichiers OMSI soient déjà restaurés.
2. Supprimez `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Ne touchez pas aux autres plugins.
3. Supprimez les fichiers produit à la racine listés dans l'organisation ci-dessus (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Supprimez `.omsilaunch\` (cela supprime vos profils de session et vos diagnostics). Ne le supprimez jamais tant que `journal.json` existe.

Une session terminée restaure chaque fichier qu'elle possédait ; aucun nettoyage supplémentaire n'est donc nécessaire. Les fichiers qu'OMSI écrit lui-même pendant son exécution (par exemple `[last_map]` dans `options.cfg`, les caches, `laststn.osn`, les journaux) constituent l'état normal d'OMSI et ne sont pas rétablis ; voir [transactions et récupération](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Scripts de validation

| Script | Objectif | Touche OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Compile `OmsiLaunch.sln` avec les avertissements traités comme des erreurs, les trois projets natifs (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) via MSBuild, puis exécute toutes les suites hors ligne : `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests`, et `OmsiLaunch.DocumentationTests` sauf si elle est ignorée, puis le test de régression de la création de paquets `Test-PackagingPipeline.ps1` (ignoré avec `-SkipNative`). Affiche `OFFLINE VALIDATION PASSED`/`FAILED`. | Non |
| `tools\New-ReleasePackage.ps1` | Garde contre les artefacts périmés, préparation, manifeste, autovérification de l'intégrité, ZIP, somme de contrôle (voir ci-dessus). | Non |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Vérifie que le manifeste liste exactement les fichiers du paquet avec une taille et un SHA-256 correspondants, que la closure requise (trois exécutables, le contrôleur, les neuf fichiers du plugin permanent dont `OmsiLaunch.Native.x86.dll`) est présente et que la configuration est `Release`. Avec `-InstallationRoot`, compare les fichiers produit de l'installation avec le paquet **en lecture seule**. Code de sortie `0` = cohérent. | Non (lecture seule) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produit un paquet candidat à partir d'une préparation propre sous `artifacts\candidate\post-round-a` et exige que la closure préparée et l'archive réextraite passent le contrôle d'intégrité. Prouve que le contrôle bloquant d'intégrité rejette une DLL altérée, un `Native.x86` altéré ou ancien, une copie périmée du plugin, un fichier listé supprimé, un fichier inattendu, un hash du manifeste modifié ou mal formé, des entrées en double (exactes, par casse, par séparateur), des chemins parents et absolus et du JSON invalide ; que l'outil de création de paquets rejette un ancien `Native.x86` placé dans la sortie de build (même avec un horodatage plus récent) et un reçu dont les sources ont changé ; et que l'archive publiée n'est jamais écrasée. Les sorties de build sont restaurées octet pour octet. Exécuté par `Invoke-OfflineValidation.ps1`. | Non |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Extrait le ZIP dans `artifacts\release\identity-verification`, vérifie `product`/`product_version`/`package_alias`, et vérifie `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` de chaque `.exe`/`.dll` sauf `nethost.dll` et `YamlDotNet.dll`, le `InternalName`/`OriginalFilename` des deux shims, et que `OmsiLaunch.exe` contient une icône intégrée. | Non |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Valide l'exécutable Release empaqueté sur une installation réelle : le manifeste doit être `Release`, ne contenir aucun chemin `Debug` ni `runtime/plugin/` et installer `plugins/OmsiLaunch.*` ; les quatre ressources de l'écran de démarrage doivent exister. Exécute trois cas `/plan` (géré par défaut, géré avec ressources personnalisées, `/splash:Unset`). Avec `-RunOmsi` (nécessite `-InstallPackage`), lance chaque cas avec `/observe-seconds`, surveille `GUI\NewSplashscreen_ENG.bmp` et `GUI\NewSplashscreen_PTB.bmp` pendant la session, et vérifie la restauration exacte, l'absence de `Omsi.exe`, l'absence de `journal.json`, le code de sortie `0`, des hashes inchangés pour les plugins tiers et un ensemble inchangé de fichiers du plugin permanent. Écrit `.omsilaunch\diagnostics\release-presentation-validation.json`. | Oui avec `-RunOmsi` (limité à la session, restauré) |

Les deux scripts `Test-*` lisent `OmsiLaunch.Version.props` pour connaître la version attendue.
