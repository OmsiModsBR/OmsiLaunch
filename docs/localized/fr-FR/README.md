# Documentation OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Traduction de la [page originale en anglais](../../README.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Ceci est la documentation anglaise normative d'OmsiLaunch `0.1.0-beta4`, la
base de référence post-durcissement. OmsiLaunch fournit le lancement programmable, la
propriété de session et le contrôle runtime pour un OMSI 2 installé ; chaque capacité native
est résolue et vérifiée par build (voir [compatibilité](reference/compatibility.md)). Chaque page sous `docs/` décrit ce que fait le code
actuel ; lorsqu'une page et le code divergent, le code l'emporte et la page est un bogue.

Vocabulaire de stabilité utilisé dans toute la documentation : `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Les options analysées mais sans aucun effet sont marquées
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Rien n'est qualifié de
validé à l'exécution sauf si le
[statut de validation à l'exécution](status/runtime-validation-status.md) l'indique.

<a id="who-reads-what"></a>
## Qui lit quoi

| Public | Commencer ici | Ensuite |
| --- | --- | --- |
| Utilisateurs (CLI, raccourcis, profils de session) | [Installation](getting-started/installation.md), [Première session](getting-started/first-session.md) | [Référence de la CLI](reference/cli.md), [Exemples de CLI](reference/cli-examples.md), [Profils de session](reference/session-profiles.md), [Zone de notification Windows](reference/windows-tray.md), [Codes de sortie](reference/exit-codes.md) |
| Intégrateurs (`OmsiLaunch.Api`, IPC locale) | [Démarrage rapide de l'API publique](getting-started/api-quick-start.md), [Référence de l'API publique](reference/public-api.md), [Référence LaunchSpec](reference/launchspec.md) | [Cycle de vie de la session](concepts/session-lifecycle.md), [Contrôle runtime](reference/runtime-control.md), [Capacités](reference/capabilities.md), [Contrôle local / IPC](reference/local-control.md), [Référence des erreurs](reference/errors.md) |
| Mainteneurs (publication, validation, limites) | [Empaquetage](reference/packaging.md), [Modèle du plugin permanent](concepts/permanent-plugin.md) | [Transactions et récupération](concepts/transactions-and-recovery.md), [Compatibilité](reference/compatibility.md), [Limitations connues](reference/known-limitations.md), [Statut de validation à l'exécution](status/runtime-validation-status.md) |

## Navigation

| Page | Objet |
| --- | --- |
| [Premiers pas](getting-started/first-session.md) | Planifier, démarrer, observer et arrêter une session depuis la racine OMSI. |
| [Installation](getting-started/installation.md) | Prérequis, extraction du paquet dans la racine OMSI, vérification avec `/version`, désinstallation propre. |
| [Démarrage rapide de l'API publique](getting-started/api-quick-start.md) | Un programme .NET complet qui planifie, démarre, lit et arrête une session. |
| [Référence de la CLI](reference/cli.md) | Chaque option, mot de commande et route hiérarchique de `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Exemples de CLI](reference/cli-examples.md) | Lignes de commande à copier-coller pour les tâches courantes. |
| [Référence LaunchSpec](reference/launchspec.md) | Chaque propriété et valeur d'enum de `LaunchSpec`, règles de chargement JSON pour `/spec`. |
| [Gestion des HOF](reference/hof-management.md) | Copie et miroir des HOF limités à la session dans les dossiers de véhicules : sources, cibles, conflits, restauration. |
| [Modules runtime](../../reference/runtime-modules.md) | Modules qui s'exécutent dans OMSI avec une session : manifeste, confiance, cycle de vie, politiques d'échec, opérations. |
| [Référence des profils de session](reference/session-profiles.md) | Schéma `omsilaunch.session-profile/v1` de `profile.yaml`, clés, limites, priorité. |
| [Référence de l'API publique](reference/public-api.md) | `IOmsiLaunch`, records et enums publics, stabilité de chaque membre. |
| [Inventaire de l'API publique](reference/public-api-inventory.md) | Liste générée de chaque type et membre public avec sa signature et sa stabilité. |
| [Contrôle runtime](reference/runtime-control.md) | Canal de commandes runtime, timeouts, handles, sémantique de l'arrêt. |
| [Référence des capacités](reference/capabilities.md) | Catalogue des capacités et chaque identifiant d'opération runtime publique avec sa classification. |
| [Cycle de vie de la session](concepts/session-lifecycle.md) | Transitions de `SessionState`, ce que promet `StartSessionAsync`, comment une session se termine. |
| [Transactions et récupération](concepts/transactions-and-recovery.md) | États du journal, sauvegardes, vérification de la restauration, suppressions de session, récupération après plantage. |
| [Modèle du plugin permanent](concepts/permanent-plugin.md) | La closure du plugin `plugins\OmsiLaunch.*`, l'intégrité fondée sur le manifeste, ce qu'une session ne touche jamais. |
| [Contrôle local / IPC](reference/local-control.md) | Protocole de canal nommé (named pipe) `0.1`, point de terminaison par installation, liaison `session_id`, modèle de confiance. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | L'hôte Windows (sans console) : différences avec `OmsiLaunch.exe`, `/silent`, boîtes de dialogue, codes de sortie. |
| [Zone de notification Windows](reference/windows-tray.md) | Indicateur de la zone de notification : icône, menu, fenêtre d'état champ par champ, End session, redémarrage de l'Explorateur. |
| [Référence des erreurs](reference/errors.md) | Chaque code `OL_E_*` / `OL_W_*` avec sa catégorie et sa signification. |
| [Codes de sortie](reference/exit-codes.md) | Valeurs `PublicExitCode` de 0 à 10 et codes du shim d'amorçage de 100 à 106. |
| [Empaquetage / structure de l'installation](reference/packaging.md) | Fichiers de l'archive ZIP de la version, `release-manifest.json`, structure de `.omsilaunch\`. |
| [Compatibilité / builds d'OMSI pris en charge](reference/compatibility.md) | L'unique hash de `Omsi.exe` pris en charge, le hash Steam LAA accepté, les exigences de plateforme. |
| [Limitations connues](reference/known-limitations.md) | Ce qui n'est pas pris en charge, ce qui est partiel ou constitue un risque accepté dans cette bêta. |
| [Statut de validation à l'exécution](status/runtime-validation-status.md) | Ce qui a été exécuté sous OMSI, ce qui n'a été exécuté que hors ligne, ce qui nécessite encore une vraie session. |

Pages qui restent normatives pour les mainteneurs : le
[`README.md`](../../../README.md) du dépôt, les spécifications sous `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) et les pages de projet sous
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Elles résument ; les
pages ci-dessus constituent la référence détaillée. Les pages historiques sont répertoriées dans le
[manifeste de la documentation](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Comment cette documentation reste synchronisée

Un contrôle bloquant de documentation, `tests\OmsiLaunch.DocumentationTests`, est compilé contre
`OmsiLaunch.Api` et `OmsiLaunch.Core` et compare les pages ci-dessus au
code qui définit la surface publique :

| Contrôle | Vérifications |
| --- | --- |
| `docs.cli-flags` | Chaque entrée de `CliInput.KnownFlags` apparaît dans la référence de la CLI sous la forme `` `/flag` `` ou `` `/flag:` `` ; chaque entrée de `CliInput.AcceptedNoEffectFlags` est marquée `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` sur sa ligne ; chaque mot de commande et chaque route de `CliInput.HierarchicalRoutes` apparaît avec son opération runtime ; chaque valeur de `PublicExitCode` possède une ligne `| n |` dans le tableau des codes de sortie. |
| `docs.capabilities` | Chaque identifiant de `PublicCapabilityRegistry.All`, chaque entrée de `PublicCapabilityRegistry.PublicRuntimeOperationIds` et chaque nom de `PublicCapabilityClassification` apparaît dans la référence des capacités. |
| `docs.errors` | Chaque code de `PublicErrorCodes.All` apparaît dans la référence des erreurs, et aucun littéral `OL_E_*` / `OL_W_*` de `src\` ou de `tools\OmsiLaunch.Cli\` ne manque dans `PublicErrorCodes`. |
| `docs.public-api` | Chaque type exporté de `OmsiLaunch.Api`, chaque valeur d'enum et chaque membre de `IOmsiLaunch` apparaît dans la référence de l'API publique, et les cinq termes de stabilité sont tous utilisés. |
| `docs.launchspec` | Chaque propriété publique accessible depuis `LaunchSpec` et chaque valeur de ses enums apparaît dans la référence LaunchSpec. |
| `docs.session-profiles` | Chaque clé de `SessionProfileCompiler.SchemaKeys`, l'identifiant du schéma et la limite `256 KiB` apparaissent dans la référence des profils de session. |
| `docs.structure` | Chaque page du tableau de navigation existe. |
| `docs.links` | Chaque lien relatif dans `docs\**\*.md` (à l'exclusion de `docs\localized\`) et dans les fichiers `*.md` de la racine pointe vers un fichier ou un répertoire existant. |
| `docs.localization` | Chaque langue répertoriée dans `docs\localized\LOCALIZATION-MANIFEST.md` possède toutes les pages de l'ensemble traduit ; chaque page conserve les titres, tableaux et blocs de code de la page anglaise, chaque span de code inline (options, identifiants de capacités et d'opérations, codes d'erreur, clés, identifiants) et chaque lien, et ses liens relatifs sont résolus. |

Ce contrôle bloquant fait partie des suites exécutées par `tools\Invoke-OfflineValidation.ps1`
(on peut l'ignorer avec `-SkipDocs`). Il s'exécute hors ligne, ne lance jamais OMSI et fait échouer le
build lorsqu'une option, une route, une capacité, un code d'erreur, une valeur d'enum ou un type public
n'est pas documenté ou qu'un lien est cassé. Il ne vérifie pas la prose : une page peut donc encore
être erronée quant au comportement ; signalez-le comme un bogue de la page.

<a id="translations"></a>
## Traductions

`docs\localized\<locale>\` contient des traductions complètes de cette documentation `0.1.0-beta4`
pour `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` et `ja-JP`. L'ensemble des pages, les racines des langues et les
pages volontairement non traduites sont répertoriés dans
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
Les traductions conservent inchangés chaque commande, option, identifiant, code d'erreur et exemple des
pages anglaises, et le contrôle bloquant `docs.localization` le vérifie.
Les pages anglaises restent la source normative : lorsqu'une traduction diverge
d'elles, la page anglaise et le code font foi, et la traduction
est un bogue.
