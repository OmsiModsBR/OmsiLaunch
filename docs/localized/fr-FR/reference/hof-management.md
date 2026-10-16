# Gestion des HOF

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Traduction de la [page originale en anglais](../../../reference/hof-management.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

OMSI lit les fichiers HOF (`*.hof` : destinations, numéros de ligne et listes d'arrêts affichés par l'IBIS/la girouette du véhicule) dans le dossier de chaque véhicule. La gestion des HOF copie des fichiers HOF dans les dossiers de véhicules **pour la durée d'une session** : une carte ou un add-on peut livrer les HOF dont ses lignes ont besoin, et un utilisateur peut réutiliser les HOF d'un véhicule sur d'autres, sans modifier durablement aucun véhicule. Cette page est la référence normative, telle qu'implémentée par `HofPlanner` dans `src/OmsiLaunch.Core/HofPlanner.cs`, le champ `HofManagement` du [LaunchSpec](launchspec.md), les options de la [CLI](cli.md) `/hof-source`, `/hof-target`, `/hof-file` et `/hof-mode` et le bloc `hofs` des [profils de session](session-profiles.md).

Stabilité : `STABLE_BETA` pour la planification, les règles de conflit, le confinement et la restauration transactionnelle (contrôles bloquants hors ligne `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Validé à l'exécution : une session miroir avec restauration exacte et récupération après plantage via `/recover` (voir l'[état de la validation à l'exécution](../status/runtime-validation-status.md)).

La gestion des HOF n'est pas la même chose que `PlayerVehicle.Hof` / `/hof:<id>`, qui sélectionne le HOF du véhicule du joueur au démarrage et est `UNAVAILABLE` dans ce build.

<a id="model"></a>
## Modèle

Une demande est une liste d'opérations (`HofManagementSpec.Operations`). Chaque `HofOperationSpec` comporte :

| Champ | Signification |
| --- | --- |
| `SourceKind`, `Source` | Provenance des fichiers HOF (tableau ci-dessous). |
| `TargetVehicles` | Un ou plusieurs véhicules installés : l'identité d'un fichier `.bus` ou `.ovh` (`Vehicles\MAN_SD200\MAN_SD77.bus`) ou un dossier de véhicule (`Vehicles\MAN_SD200`), tous deux sous `Vehicles\`. Plusieurs identités situées dans un même dossier ne ciblent ce dossier qu'une seule fois. |
| `Mode` | `Add` (par défaut) : copie les HOF de la source dans chaque dossier cible ; les autres HOF qui s'y trouvent sont conservés. `Mirror` : copie les HOF de la source et retire, pour la session, tout autre `.hof` du dossier cible, de sorte que l'ensemble des HOF du dossier soit égal à celui de la source. |
| `Files` | Liste facultative de noms de fichiers à prendre dans la source (insensible à la casse). En son absence : chaque `*.hof` situé directement dans la source. Un nom listé que la source ne contient pas : `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Se résout en |
| --- | --- | --- |
| `Collection` | un simple nom, par exemple `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof` : une collection tenue par l'utilisateur ou par un outil. |
| `Directory` | un répertoire relatif à l'installation, par exemple `maps\RMG Leste\hof` | `*.hof` dans ce répertoire : HOF livrés à côté du contenu d'une carte ou d'un add-on. |
| `Vehicle` | une identité ou un dossier de véhicule, comme pour les cibles | `*.hof` dans le dossier de ce véhicule : réutiliser les HOF d'un véhicule sur d'autres. |
| `File` | un fichier `.hof` relatif à l'installation | ce seul fichier. |

Seuls les fichiers situés directement dans le répertoire source sont utilisés (pas de récursion). Les liens symboliques et les jonctions ne sont jamais suivis (voir [confinement](#confinement)).

<a id="what-a-session-does"></a>
## Ce que fait une session

1. **Planification** (`PlanSessionAsync`, `/plan`). Chaque opération est résolue : les sources sont lues, les cibles sont vérifiées, les conflits sont détectés. Chaque fichier qui sera modifié apparaît dans `SessionPlan.PlannedMutations` avec la `SemanticKey` `hof.add` ou `hof.mirror`, `RequestedValue` = l'identité de la source, et l'`Operation` `exact-file-overlay` (écrit) ou `session-deletion` (retiré par un miroir) ; les chemins font partie de `TouchedFiles`. Un problème rend le plan non exécutable avec les codes ci-dessous.
2. **Démarrage**. Après la récupération d'un journal obsolète, les opérations sont de nouveau résolues à partir des fichiers réels et transmises à la transaction de session : les fichiers écrits sont des overlays, les fichiers retirés par le miroir sont des suppressions de session. Un fichier cible dont les octets sont déjà identiques à la source est laissé tel quel et ne constitue pas une entrée de la transaction.
3. **Fin de session, échec, plantage.** La transaction restaure exactement chaque dossier cible : les fichiers qui existaient retrouvent leurs octets, horodatages et attributs d'origine ; les fichiers créés par la session sont supprimés ; les fichiers retirés par le miroir reviennent. Après un plantage du propriétaire, la même restauration s'exécute au démarrage suivant ou via `/recover` (voir [transactions et récupération](../concepts/transactions-and-recovery.md)).

Rien n'est écrit en dehors des dossiers cibles. Les fichiers sources sont uniquement lus. `.omsilaunch\hof\` n'est jamais modifié par une session.

<a id="conflict-rules"></a>
## Règles de conflit

Les opérations sont appliquées dans l'ordre et doivent être concordantes :

- Deux opérations qui écrivent le même fichier cible avec les mêmes octets sont fusionnées ; avec des octets différents : `OL_E_HOF_CONFLICT`.
- Un miroir qui retirerait un fichier qu'une opération antérieure écrit, ou une opération qui écrit un fichier qu'un miroir antérieur retire : `OL_E_HOF_CONFLICT`.
- Un miroir couvre l'ensemble des HOF de session du dossier : les fichiers existants plus les fichiers que des opérations antérieures y écrivent.
- Une cible HOF également écrite par la présentation de la session (écran de démarrage ou textures Internet) : `OL_E_HOF_CONFLICT` (ne peut pas se produire avec les chemins de présentation actuels, qui sont en dehors de `Vehicles\`).
- Lorsqu'une cible contient déjà un fichier du même nom avec une casse différente, l'orthographe existante est conservée.

## Confinement

Chaque source et chaque cible doivent rester à l'intérieur de l'installation : les chemins enracinés, les segments `..` et les chemins qui sortent de la racine donnent `OL_E_HOF_PATH_ESCAPE` ; il en va de même pour les cibles (et les sources `Vehicle`) situées en dehors de `Vehicles\`, les noms de collection qui ne sont pas un simple nom de répertoire, et tout dossier source ou cible atteint par une jonction ou un lien symbolique. Les fichiers `.hof` qui sont eux-mêmes des points d'analyse (reparse points) sont ignorés.

<a id="limits"></a>
## Limites

| Limite | Valeur | Erreur |
| --- | --- | --- |
| Taille d'un fichier HOF | 8 MiB | `OL_E_HOF_INVALID` |
| Fichiers par opération | 256 | `OL_E_HOF_INVALID` |
| Total d'octets écrits par session | 256 MiB | `OL_E_HOF_INVALID` |
| Cibles par opération | au moins 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Codes d'erreur

| Code | Cause |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | La collection, le répertoire, le véhicule ou le fichier n'existe pas, ne contient aucun `.hof`, ou ne contient pas un fichier nommé dans `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Un fichier ou dossier de véhicule cible n'existe pas. |
| `OL_E_HOF_PATH_ESCAPE` | Un chemin sort de l'installation, une cible n'est pas sous `Vehicles\`, un nom de collection n'est pas un simple nom, ou une jonction/un lien symbolique est traversé. |
| `OL_E_HOF_CONFLICT` | Deux opérations sont en désaccord sur un même fichier cible (voir [règles de conflit](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Aucune cible, aucune source, une source `File` qui n'est pas un `.hof`, une cible qui est un fichier autre que `.bus`/`.ovh`, ou une [limite](#limits) dépassée. |

<a id="using-it"></a>
## Utilisation

Ligne de commande (une seule opération ; plusieurs opérations nécessitent `/spec` ou un profil de session) :

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (JSON `/spec`, énumérations sous forme d'entiers) :

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

Préréglage de profil de session (une source `package` est un répertoire situé dans le paquet du profil) :

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API : définissez `LaunchSpec.HofManagement` et appelez `PlanSessionAsync` / `StartSessionAsync` comme d'habitude ; `/list:Hofs` (`ContentQueryKind.Hofs`) liste les fichiers HOF installés.

Voir aussi : [LaunchSpec](launchspec.md), [profils de session](session-profiles.md), [CLI](cli.md), [erreurs](errors.md), [transactions et récupération](../concepts/transactions-and-recovery.md).
