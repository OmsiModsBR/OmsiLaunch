# Première session

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Traduction de la [page originale en anglais](../../../getting-started/first-session.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page présente pas à pas la première session OMSI gérée avec OmsiLaunch `0.1.0-beta4` : planifier sans démarrer OMSI, démarrer avec des options explicites, démarrer avec un profil de session prédéfini, contrôler et arrêter la session, puis retrouver les diagnostics. Elle suppose que le paquet est installé comme décrit dans [installation](installation.md). Chaque option est spécifiée dans la [référence de la CLI](../reference/cli.md) ; d'autres invocations figurent dans les [exemples de CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Ce que fait une session

Une session est une transaction autour d'un processus OMSI : OmsiLaunch prend un instantané des fichiers qu'il va modifier (par défaut les deux bitmaps de l'écran de démarrage sous `GUI\`, plus `options.cfg` lorsque des overlays `/set` sont demandés), écrit un journal durable sous `.omsilaunch\`, applique les overlays, démarre `Omsi.exe` avec le plugin permanent, attend l'entrée dans le jeu (`Running`), maintient la session contrôlable et, à la fin, arrête OMSI et restaure octet pour octet chaque fichier modifié. `/new` ne sélectionne jamais implicitement une carte ni un point d'entrée : les deux doivent être indiqués, ou provenir d'un fichier `/spec` ou d'un profil de session.

<a id="1-plan-nothing-is-started"></a>
## 1. Planifier (rien n'est démarré)

Exécutez depuis la racine OMSI ; l'installation désigne par défaut le répertoire contenant `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

Le plan doit indiquer `"IsRunnable": true` (sortie `0`). Il liste `TouchedFiles` et `PlannedMutations`, ce qui vous permet de voir exactement ce que la session va recouvrir par des overlays. Corrigez tout diagnostic `OL_E_` avant de continuer ; rien n'a été écrit.

L'exemple de spec fourni dans le paquet fait la même chose à partir d'un fichier :

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Démarrer avec des options explicites

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Ce qui se passe, dans l'ordre :

1. Le plan est affiché (`Plan: READY profile=Omsi23004` sur la disposition connue).
2. Récupération de tout journal plus ancien en attente, acquisition du bail, instantané, journal, overlays, vérification de l'intégrité du plugin, démarrage de `Omsi.exe`.
3. L'icône de la zone de notification apparaît (`OmsiLaunch is running` (`OmsiLaunch est en cours d'exécution`)) ; voir [zone de notification Windows](../reference/windows-tray.md).
4. Lorsque le jeu est atteint, l'état `Running` est affiché en JSON (`"State": 14`). Le timeout de démarrage par défaut est de 180 s (`/startup-timeout:<1..600>` pour le modifier).
5. La console reste attachée jusqu'à la fin de la session. Ne fermez pas la fenêtre de console pour arrêter : utilisez l'une des méthodes d'arrêt ci-dessous.

Ajouts facultatifs pour la première exécution :

- `/set:graphics.maxFPS=60` (un overlay de `options.cfg`, restauré à la fin) ;
- `/splash:Unset` pour laisser intact l'écran de démarrage d'OMSI, ou `/splash-language:DEU` pour choisir l'écran de démarrage géré localisé ;
- `/observe-seconds:30` pour arrêter automatiquement 30 s après `Running` (utile pour un test de fumée) ;
- `--json` pour une sortie structurée.

Les options qui demandent une date, une heure, une année, une météo ou un véhicule du joueur (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) sont acceptées mais ne peuvent pas être appliquées par ce build : le plan devient `NOT RUNNABLE` avec `OL_E_CAPABILITY_UNAVAILABLE`. Omettez-les.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Démarrer avec un profil de session prédéfini

Un profil de session est un fichier YAML situé sous `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` qui fixe la carte, le point d'entrée et jusqu'à 99 préréglages de paramètres (schéma `omsilaunch.session-profile/v1` ; référence complète dans [profils de session](../reference/session-profiles.md)). Créez `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml` :

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Ensuite :

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Règles à retenir : l'`id` doit être égal au nom du répertoire ; l'index va de `1..99` ; les options explicites qui remplaceraient un champ détenu par le profil (`/map`, `/entrypoint-index`, une clé `/set` détenue par le préréglage, les options d'écran de démarrage lorsque le préréglage a `presentation`) sont rejetées avec `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (sortie `2`) ; le bloc `new:` ne s'applique qu'avec `/new` ; avec `/saved:<file.osn>`, la carte de la situation doit figurer sous `compatibility.maps`. Le fichier `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` fourni dans le paquet illustre le schéma complet, mais son bloc `new:` définit `date`, `time` et `weather`, que ce build ne peut pas appliquer ; ne le copiez donc qu'après avoir supprimé ces clés.

<a id="4-control-the-running-session"></a>
## 4. Contrôler la session en cours

Depuis une seconde console dans le même répertoire (sans argument d'installation) :

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Ces commandes passent par le pipe de contrôle local de cette installation ([contrôle local](../reference/local-control.md)) ; la sortie `4` signifie qu'aucun propriétaire ne s'exécute ici.

<a id="5-stop"></a>
## 5. Arrêter

Chacune de ces méthodes termine la session de la même manière (OMSI est arrêté, puis chaque fichier modifié est restauré, puis le journal et la sauvegarde sont supprimés) :

| Méthode | Remarques |
|---|---|
| Icône de la zone de notification → `End session` (`Terminer la session`) → confirmer | Disponible dans les sessions `OmsiLaunch.exe` comme `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | Depuis une autre console ; rend la main immédiatement, le propriétaire termine la restauration. |
| Ctrl+C dans la console du propriétaire | Demande l'arrêt ; le propriétaire attend la fin de la restauration avant de se terminer. |
| `/observe-seconds:<n>` | Arrêt automatique `n` secondes après `Running`. |
| OMSI se termine de lui-même | Le propriétaire détecte `ProcessExited` et restaure. |

La routine d'arrêt propre à OMSI ne s'exécute pas, si bien qu'OMSI ne réécrit pas `options.cfg` à la sortie ; c'est voulu, afin que la restauration soit exacte. Fermer la fenêtre de console du propriétaire avec le bouton X ne laisse que 4 s à la restauration ; si elle n'est pas terminée, le démarrage suivant (ou `OmsiLaunch.exe /recover`) l'achève à partir du journal. Le code de sortie du propriétaire est `0` lorsque la session s'est terminée dans l'état `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Où regarder ensuite

| Emplacement | Contenu |
|---|---|
| Sortie console / `--json` | Plan, état `Running`, état final (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | La trace de l'hôte de la session (limites de la transaction, démarrage du processus, handoff du plugin, entrée dans le jeu, restauration). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Résultat d'une opération `/runtime:` exécutée par le propriétaire. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Événements de l'indicateur de la zone de notification. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` après une fin propre. `true` signifie qu'un journal subsiste ; exécutez `OmsiLaunch.exe /recover`. |

Si la session n'a pas atteint le jeu, l'état final porte le diagnostic `OL_E_` en échec (par exemple `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), le code de sortie est `1`, et les fichiers ont malgré tout été restaurés. Voir [codes de sortie](../reference/exit-codes.md), [erreurs](../reference/errors.md) et [limitations connues](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Exécution sans console

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

délègue à `OmsiLaunchW.exe` et renvoie `0` immédiatement. La session n'a pas de console ; les échecs apparaissent sous forme de boîtes de message et l'icône de la zone de notification est la seule surface visible. Utilisez `session status`, `events watch` et le répertoire de diagnostics pour la suivre. Le comportement complet de l'hôte Windows est décrit dans la [référence d'OmsiLaunchW.exe](../reference/omsilaunchw.md).
