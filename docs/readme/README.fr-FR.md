<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Contrôle de session pour OMSI 2.</strong></p>
<p align="center">Open source · Programmable · Porté par la communauté</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <strong>Français</strong> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Ceci est une traduction du [README canonique en anglais (US)](../../README.md). En cas de divergence, le README en anglais (US) fait foi.

# OmsiLaunch

**OmsiLaunch** est une couche open source permettant de lancer OMSI 2 par
programmation, de gérer des sessions et de contrôler la simulation en cours. Il
planifie une session à partir d'une description déclarative, applique chaque
modification temporaire de configuration dans une transaction journalisée,
démarre OMSI, l'observe jusqu'à la phase de jeu, permet à des outils de lire et
de modifier la simulation en cours via une API publique, et restaure chaque
fichier qu'il a touché à la fin de la session.

C'est une infrastructure pour les lanceurs, les outils, l'automatisation et les
intégrations communautaires. Ce n'est pas un lanceur graphique.

> **Définissez la session, pas les clics.**

## Statut : 0.1.0-beta4

La bêta publique actuelle est **`0.1.0-beta4`**. La bêta 4 ajoute la
compatibilité par capacité, les Runtime Modules, les profils de session 1..99
et un ensemble de fonctionnalités de démarrage et runtime. La compatibilité, le
catalogue des capacités, la pagination, les erreurs typées et le pont des
modules ont été observés dans des sessions OMSI réelles (séries de validation A
et B). Les fonctionnalités ajoutées après ces séries sont `EXPERIMENTAL` :
closes par analyse statique et testées hors ligne uniquement, la validation à
l'exécution restant en attente. Certaines restent `STATICALLY_VALIDATED`,
`PARTIAL` ou `UNAVAILABLE`. Deux points runtime restent ouverts, car ils ne
peuvent pas être produits de façon sûre : la phase de jeu avec Steam LAA et la
disparition naturelle des véhicules routiers et des humains (RV-002). La page
[Statut de la validation à l'exécution](../localized/fr-FR/status/runtime-validation-status.md)
est la référence qui fait foi sur ce qui a été exécuté sous OMSI et ce qui a
été exécuté uniquement hors ligne.

Il s'agit d'une bêta : l'API publique, la CLI et les formats de fichier sont
marqués `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` ou `UNAVAILABLE`
membre par membre et peuvent encore changer avant la version 1.0.

## Périmètre OMSI pris en charge

OmsiLaunch exige qu'OMSI 2 soit installé. Il ne dépend pas d'un fichier
`Omsi.exe` particulier : chaque capacité native est résolue et vérifiée
individuellement dans OMSI avant d'être utilisée, et une capacité qui ne peut
pas être vérifiée sur un build est indisponible sur ce build.

| Élément | Périmètre |
| --- | --- |
| Prérequis | OMSI 2 installé : un `Omsi.exe` 32 bits avec une version d'OMSI 2 (2.x) et les répertoires `maps\` et `Vehicles\`. Tout le reste est refusé avec `OL_E_UNSUPPORTED_BUILD`. |
| Disposition connue | OMSI 2.3.004 (`Omsi23004`). Chaque capacité est résolue, et toutes les validations à l'exécution ont été effectuées sur cette disposition. Les hashes l'identifient par un chemin rapide ; ils ne sont pas une exigence. |
| Autres builds d'OMSI 2 | Chaque capacité est résolue par des stratégies indépendantes du build (RTTI Delphi, signatures de code uniques, slots VMT) et vérifiée. Les capacités qui lisent des dispositions d'objets nécessitent une disposition connue. |
| Exécutable Steam | Résolu dans le processus ; une véritable session Steam n'a **pas** été validée à l'exécution. |
| Système d'exploitation | Windows 10 ou ultérieur, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (contrôleur) et .NET 6 Runtime **x86** (le plugin s'exécute dans le processus 32 bits `Omsi.exe`). |

Détails : [Compatibilité](../localized/fr-FR/reference/compatibility.md) et
[Installation](../localized/fr-FR/getting-started/installation.md).

## Ce qu'il fournit

**Sessions.** Une session est planifiée à partir d'un `LaunchSpec` (options CLI,
fichier JSON, profil de session ou API), validée sans effet de bord (`/plan`),
puis démarrée, observée au fil des transitions de `SessionState` et terminée.
L'arrêt d'une session met fin à OMSI de force, afin qu'OMSI ne puisse pas
écraser les fichiers sur le point d'être restaurés. Voir
[Cycle de vie de la session](../localized/fr-FR/concepts/session-lifecycle.md).

**Transactions et récupération.** Chaque surcharge de configuration est limitée
à la session. OmsiLaunch prend un instantané de chaque fichier qu'il modifie, le
journalise, applique la modification, la vérifie puis restaure le fichier, y
compris après un plantage (`/recovery-status`, `/recover`,
`RecoverPendingAsync`). Il ne propose aucune modification permanente de la
configuration. Voir
[Transactions et récupération](../localized/fr-FR/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Une interface de référence reposant sur la même API
publique, sans logique OMSI propre : découverte, planification, démarrage de
sessions et commandes client envoyées à une session en cours. Voir la
[référence de la CLI](../localized/fr-FR/reference/cli.md) et les
[exemples CLI](../localized/fr-FR/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** L'hôte du sous-système Windows destiné aux raccourcis. Il
accepte la même ligne de commande sans fenêtre de console et signale les échecs
dans des boîtes de message. Voir
[OmsiLaunchW.exe](../localized/fr-FR/reference/omsilaunchw.md).

**Zone de notification Windows.** Chaque session propriétaire affiche une icône
dans la zone de notification, avec une fenêtre d'état et une action
« End session » (`Terminer la session`). Cette action emprunte le même chemin
d'arrêt que `session stop`. Voir
[Zone de notification Windows](../localized/fr-FR/reference/windows-tray.md).

**Profils de session.** Des paquets déclaratifs `profile.yaml`
(`omsilaunch.session-profile/v1`) placés sous
`.omsilaunch\session-profiles\<id>\`, afin que les auteurs de contenu puissent
distribuer des sessions reproductibles qui démarrent avec une seule commande.
Voir [Profils de session](../localized/fr-FR/reference/session-profiles.md).

**API publique et contrôle runtime.** `OmsiLaunch.Api` (`IOmsiLaunch`) est la
surface produit à privilégier. Les opérations runtime telles que l'heure, la
météo, la carte, la caméra, l'horaire, les véhicules, les humains, les variables
de script et les textures D3D sont limitées à la session, validées par rapport
au profil de build et adressées au moyen de handles sémantiques opaques, jamais
de pointeurs natifs. Les résultats sont bornés par un slot runtime de 64 KiB.
Voir la [référence de l'API publique](../localized/fr-FR/reference/public-api.md) et
[Contrôle runtime](../localized/fr-FR/reference/runtime-control.md).

**Contrôle local.** Un canal nommé (named pipe) propre à chaque installation et
lié au `session_id` actif permet à d'autres processus du même utilisateur de
lire l'état et les événements, d'arrêter la session et d'exécuter des
opérations runtime publiques. Voir
[Contrôle local / IPC](../localized/fr-FR/reference/local-control.md).

**Capacités.** Chaque capacité et chaque opération runtime publique sont
cataloguées avec leur stabilité. Les capacités expérimentales et indisponibles
sont listées, et non masquées (`OmsiLaunch.exe capabilities`). Voir
[Capacités](../localized/fr-FR/reference/capabilities.md).

**Plugin permanent.** La closure du plugin exécutée dans le processus est
installée une seule fois sous `plugins\OmsiLaunch.*`. Avant chaque démarrage,
elle est vérifiée par rapport aux entrées SHA-256 de `release-manifest.json`.
Les plugins tiers ne sont jamais touchés. Voir
[Modèle du plugin permanent](../localized/fr-FR/concepts/permanent-plugin.md).

## Démarrage rapide

Extrayez le paquet de version à la racine de l'installation d'OMSI 2, puis
exécutez les commandes suivantes depuis ce répertoire :

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Pendant que cette session s'exécute, une seconde console ouverte dans le même
répertoire peut l'interroger ou la terminer :

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Pas à pas : [Première session](../localized/fr-FR/getting-started/first-session.md). Pour les
intégrateurs .NET : [Démarrage rapide avec l'API publique](../localized/fr-FR/getting-started/api-quick-start.md).

## Documentation

L'ensemble de la documentation est indexé dans [`docs/localized/fr-FR/README.md`](../localized/fr-FR/README.md).
Les pages en anglais (US) sous `docs/` sont canoniques et normatives.

| Sujet | Page |
| --- | --- |
| API publique | [docs/localized/fr-FR/reference/public-api.md](../localized/fr-FR/reference/public-api.md) |
| CLI | [docs/localized/fr-FR/reference/cli.md](../localized/fr-FR/reference/cli.md) |
| Exemples CLI | [docs/localized/fr-FR/reference/cli-examples.md](../localized/fr-FR/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/fr-FR/reference/omsilaunchw.md](../localized/fr-FR/reference/omsilaunchw.md) |
| Zone de notification Windows | [docs/localized/fr-FR/reference/windows-tray.md](../localized/fr-FR/reference/windows-tray.md) |
| Profils de session | [docs/localized/fr-FR/reference/session-profiles.md](../localized/fr-FR/reference/session-profiles.md) |
| Contrôle runtime | [docs/localized/fr-FR/reference/runtime-control.md](../localized/fr-FR/reference/runtime-control.md) |
| Contrôle local / IPC | [docs/localized/fr-FR/reference/local-control.md](../localized/fr-FR/reference/local-control.md) |
| Capacités | [docs/localized/fr-FR/reference/capabilities.md](../localized/fr-FR/reference/capabilities.md) |
| Erreurs et codes de sortie | [docs/localized/fr-FR/reference/errors.md](../localized/fr-FR/reference/errors.md), [docs/localized/fr-FR/reference/exit-codes.md](../localized/fr-FR/reference/exit-codes.md) |
| Empaquetage | [docs/localized/fr-FR/reference/packaging.md](../localized/fr-FR/reference/packaging.md) |
| Limitations connues | [docs/localized/fr-FR/reference/known-limitations.md](../localized/fr-FR/reference/known-limitations.md) |
| Statut de la validation à l'exécution | [docs/localized/fr-FR/status/runtime-validation-status.md](../localized/fr-FR/status/runtime-validation-status.md) |

### Documentation dans d'autres langues

La documentation est traduite dans 14 locales sous
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). Les traductions
ont été produites à partir des pages canoniques en anglais (US) et validées
mécaniquement par rapport à celles-ci. Une relecture éditoriale par des
locuteurs natifs ne faisait pas partie de la version bêta 4 et pourra avoir lieu
après la publication. En cas de divergence entre une traduction et la page
anglaise, la page anglaise fait foi.

## Limitations et points ouverts

Voici les limitations les plus importantes. La liste complète figure dans
[Limitations connues](../localized/fr-FR/reference/known-limitations.md).

- **Steam.** Steam LAA est `PARTIAL` : la phase de jeu, les
  lectures, les commandes et l'arrêt nécessitent une véritable installation
  Steam et n'ont pas été validés à l'exécution.
- **Expérimental dans cette bêta :** le démarrage à partir du dernier état de la
  carte (`/last`) et la date, l'heure, l'année ou la météo explicites au
  démarrage sont clos statiquement et attendent la validation à l'exécution.
  **Indisponible :** l'attribution du véhicule du joueur au démarrage ; la
  demander rend le plan non exécutable au lieu d'être ignorée silencieusement.
- **Les écritures runtime sont limitées.** `weather.set`, `weather.metar.set`,
  les écritures de variables de type chaîne et les déclencheurs de script sont
  expérimentaux tant qu'ils ne sont pas validés à l'exécution ; les écritures du
  calendrier et le déplacement de véhicules sont indisponibles. Les
  modifications runtime ne sont pas journalisées et ne sont pas restaurées.
- **Durée de vie des handles.** La détection des handles obsolètes pour les
  véhicules routiers et les humains qui disparaissent naturellement (RV-002) n'a
  aucun producteur runtime sûr et n'est validée que hors ligne.
- **Résultats paginés.** Les listes runtime sont paginées avec `cursor` et
  `limit` et se poursuivent à `next_cursor` ; une page qui ne tient pas dans la
  boîte aux lettres de 64 KiB est raccourcie et signalée par `truncated=true`.
- **L'arrêt est forcé par défaut.** OMSI est terminé de force, sa propre
  procédure d'arrêt ne s'exécute pas, et l'état OMSI non enregistré est perdu.
  L'arrêt coopératif optionnel (`/cooperative-shutdown`, expérimental) exécute
  d'abord la routine de sortie d'OMSI.
- **Modèle de confiance « même utilisateur ».** Tout processus du même
  utilisateur Windows peut atteindre le plan de contrôle local.

## Téléchargement

Téléchargez **`OmsiLaunch-0.1.0-beta4.zip`** et son fichier `.sha256` depuis la
[page des Releases](https://github.com/lmonteirotech/OmsiLaunch/releases).
Extrayez-le directement dans la racine OMSI prise en charge. Le paquet contient
le contrôleur (`OmsiLaunch.exe`, `OmsiLaunchW.exe`), ses dépendances, la closure
du plugin permanent avec `release-manifest.json`, les ressources de l'écran de
démarrage, un exemple de session et la documentation hors ligne sous
`.omsilaunch\docs\`. Structure du paquet et suppression propre :
[Empaquetage](../localized/fr-FR/reference/packaging.md).

## Compilation à partir des sources

Prérequis :

- Windows 10 ou ultérieur, x64.
- SDK .NET 6, avec les runtimes .NET 6 x64 et x86 pour exécuter les tests.
- Visual Studio avec la charge de travail C++ MSBuild (ensemble d'outils de
  plateforme `v145`) et un SDK Windows 10, pour les trois projets natifs :
  `OmsiLaunch.Native.x86` (Win32), ainsi que `OmsiLaunch.Bootstrapper` et
  `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` contient les projets managés et les suites de tests. Le point
d'entrée hors ligne unique compile l'ensemble et exécute toutes les suites hors
ligne ; il ne lance jamais OMSI :

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` ignore les projets natifs et le test de régression de
l'empaquetage. `-SkipDocs` ignore les contrôles bloquants de documentation. La
sortie de compilation est placée dans `artifacts\`, qui n'est pas suivi par le
contrôle de version. `tools\New-ReleasePackage.ps1` prépare, hache, valide et
compresse un paquet de version à partir d'un build existant. Voir
[Empaquetage](../localized/fr-FR/reference/packaging.md) et
[Tests et validation](../project/TESTING-AND-VALIDATION.md).

| Chemin | Contenu |
| --- | --- |
| `src/` | Bibliothèques du produit : API, cœur, configuration, contenu, interop, processus, plugin, profil de build, frontière native x86 |
| `tools/` | CLI et hôte Windows (`OmsiLaunch.Cli`), shims natifs (`OmsiLaunch.Bootstrapper`), hôte de test hors ligne, scripts d'empaquetage et de validation, outils de localisation |
| `tests/` | Suites de tests unitaires, d'intégration, de profil, d'interface Windows et de documentation |
| `docs/` | Documentation canonique et ses traductions sous `docs/localized/` |
| `examples/` | Exemples de LaunchSpec et de sessions |
| `assets/` | Identité visuelle, icônes et ressources du paquet |
| `third_party/` | Notes de provenance en amont |

Résumés pour les mainteneurs : [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Communauté et licence

OmsiLaunch est un projet open source orienté vers la communauté. Il est
indépendant des autres lanceurs OMSI, et des outils communautaires compatibles
peuvent s'appuyer sur lui.

OmsiLaunch est distribué sous la licence [LGPL-3.0-only](../../LICENSE). Consultez les
[mentions relatives aux tiers](../../THIRD-PARTY-NOTICES.md) pour connaître la
provenance du code source incorporé et les mentions applicables.
