# Référence de l'API publique (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> Traduction de la [page originale en anglais](../../../reference/public-api.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page est la référence normative de l'API publique managée d'OmsiLaunch 0.1.0-beta4 : l'assembly `OmsiLaunch.Api` (contrats) et le point d'entrée pour les intégrateurs `OmsiLaunchService` dans `OmsiLaunch.Core`. Elle ne documente que ce que fait le code actuel. Tout ce qu'un intégrateur peut appeler, recevoir ou observer est listé ici avec son niveau de stabilité ; tout ce qui n'est pas listé n'est pas une surface d'intégration.

L'[inventaire de l'API publique](public-api-inventory.md) généré liste chaque type et membre public de `OmsiLaunch.Api`, `OmsiLaunch.Core` et `OmsiLaunch.Process` avec sa signature et sa stabilité ; un contrôle bloquant de documentation échoue lorsque l'inventaire et les assemblies divergent. Cette page en explique la sémantique.

Pages associées : [référence LaunchSpec](launchspec.md), [codes d'erreur](errors.md), [cycle de vie de la session](../concepts/session-lifecycle.md), [transactions et récupération](../concepts/transactions-and-recovery.md), [contrôle runtime](runtime-control.md), [capacités](capabilities.md), [plan de contrôle local](local-control.md), [codes de sortie](exit-codes.md), [état de la validation à l'exécution](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Vocabulaire de stabilité

| Niveau | Signification sur cette page |
| --- | --- |
| `STABLE_BETA` | Le contrat est figé pour la ligne de protocole 0.1 et le chemin est validé à l'exécution dans `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Appelable et testé, mais le contrat ou les preuves d'exécution peuvent changer avant de devenir stables. |
| `PARTIAL` | Présent dans le contrat ; seule une partie du comportement est implémentée ou validée (le texte précise laquelle). |
| `INTERNAL` | Public dans l'assembly pour des raisons techniques (le pont partage le type) mais pas une surface d'intégration ; peut changer sans préavis. |
| `UNAVAILABLE` | Présent dans le contrat mais rejeté par le build actuel. |

<a id="assembly-overview"></a>
## Vue d'ensemble des assemblies

| Assembly | Rôle pour les intégrateurs |
| --- | --- |
| `OmsiLaunch.Api` | Contrats purs : records, enums, `IOmsiLaunch`, registre des capacités, catalogue d'erreurs, formats de transmission, helpers D3D. Ne contient aucun `IntPtr`, `nint`, handle Win32, adresse native ni objet processus. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (l'implémentation de `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` et `CurrentWindowsX64Platform` (le seul adaptateur de plateforme), `InstallationLease`. Nécessaire pour construire le service. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Assemblies d'implémentation. Leurs types publics sont `INTERNAL` pour les intégrateurs. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Point d'entrée : `OmsiLaunchService` et `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Paramètre | Valeur valide | Invalide / par défaut |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (namespace `OmsiLaunch.Process`). Il détecte la plateforme, crée le processus OMSI avec `CreateProcessW`, l'attend et y met fin. | Aucune autre implémentation n'est livrée. Un `IRuntimePlatform` personnalisé est `INTERNAL`. |
| `PluginBuildDirectory` | Répertoire qui contient les fichiers de référence de la closure du plugin permanent : `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` et chaque `OmsiLaunch.*.dll` de la closure managée (doit inclure `OmsiLaunch.Plugin.dll`). Dans un paquet installé, il s'agit de `<package>\plugins`. | Répertoire ou fichier manquant : `PlanSessionAsync` renvoie un plan non exécutable avec `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Chemin de `OmsiLaunch.Native.x86.dll` (dans le paquet : `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Comme ci-dessus. |
| `ReleaseManifestPath` | `release-manifest.json` à côté de `OmsiLaunch.exe` lorsqu'il est présent. Fournit le SHA-256 attendu de chaque fichier de `plugins/` (`plugin.integrity.reference = manifest`). | `null` (disposition de développement) : les fichiers installés sont seulement vérifiés quant à leur présence et à leur cohérence avec la closure de référence (`plugin.integrity.reference = self`). Manifeste mal formé : `OL_E_RELEASE_MANIFEST_INVALID`. |

Le service lit ces chemins à chaque `PlanSessionAsync` et `StartSessionAsync` ; il ne copie, ne prépare ni ne supprime jamais de fichiers du plugin (voir [plugin permanent](../concepts/permanent-plugin.md)). La CLI construit le service exactement ainsi (`tools/OmsiLaunch.Cli/Program.cs`) :

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

Créez un seul service par processus et partagez-le. Stabilité : `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Règles de propriété de la session

| Règle | Détail |
| --- | --- |
| Un seul propriétaire par installation | `StartSessionAsync` acquiert le bail d'installation, un sémaphore nommé `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, et le conserve jusqu'à ce que le superviseur ait restauré l'installation. Un second démarrage sur la même racine depuis n'importe quel processus de la même session de connexion Windows (logon session) échoue avec `OL_E_INSTALLATION_BUSY` (signalé comme une session `Failed`, voir `StartSessionAsync`). Le bail vaut par session de connexion, pas entre sessions de connexion différentes, et n'est pas libéré tant qu'un autre processus détient un handle sur lui (risque accepté). |
| Les handles sont locaux au processus | `SessionHandle` encapsule le `Guid` de la session. Il n'a de sens que pour l'instance de `OmsiLaunchService` qui l'a renvoyé. Un handle construit à partir d'un `Guid` connu dans un autre processus (ou une autre instance du service) produit `KeyNotFoundException`. Le contrôle inter-processus passe par le [plan de contrôle local](local-control.md), pas par les handles. |
| Appelez toujours `CloseAsync` | À partir de `StartSessionAsync`, le processus possède une transaction durable. `CloseAsync` demande l'arrêt canonique si nécessaire, attend le superviseur (sortie du processus, restauration exacte, libération du bail) et oublie la session. Il doit être appelé sur chaque chemin de sortie, y compris après un état `Failed`. Sans lui, l'entrée de session reste en mémoire ; la restauration elle-même est effectuée par le superviseur dans tous les cas. |
| Les sessions en échec restent des sessions | Un démarrage qui échoue après le retour de `StartSessionAsync` signale `SessionState.Failed` ; le handle reste valide pour `GetStatusAsync`/`WaitForAsync` jusqu'à `CloseAsync`. |
| Les plans sont revérifiés | `StartSessionAsync` recalcule le hash de `Omsi.exe` et replanifie la spécification ; un plan qui n'est plus exécutable est rejeté avec `OL_E_PLAN_NOT_RUNNABLE`. |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

Points communs à toutes les méthodes :

- Les handles inconnus ou déjà fermés lèvent `KeyNotFoundException` (« Unknown OmsiLaunch session. »).
- Aucune méthode n'exige une session Running, sauf `ExecuteRuntimeAsync`.
- Les exceptions qui portent un code OmsiLaunch placent le code au début de `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). La CLI extrait les codes des messages de la même manière (`CliProgram.Classify`).
- Installation prise en charge : OMSI 2. Les capacités natives sont résolues et vérifiées par build dans le processus ; une capacité indisponible sur un build échoue avec `OL_E_CAPABILITY_UNRESOLVED`. Voir [compatibilité](compatibility.md).

<a id="complete-minimal-example"></a>
### Exemple minimal complet

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Objectif | Compiler un `LaunchSpec` en `SessionPlan` sans démarrer OMSI : valider la spécification, détecter la plateforme, calculer l'empreinte de `Omsi.exe`, résoudre les identités de contenu, calculer les mutations de fichiers planifiées, lister les capacités requises et non prises en charge, et décider de `IsRunnable`. Capacité publique `session.plan`. |
| Paramètres | `spec` : un `LaunchSpec` entièrement renseigné (voir [référence LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (les huit dictionnaires) et `Behavior` doivent être non null ; les membres optionnels peuvent être `null`. `RootPath` doit être un répertoire absolu ; une racine vide est consignée comme `OL_E_INSTALLATION_NOT_FOUND`, mais la sonde de plateforme sur un chemin vide lève `ArgumentException` avant que le plan ne soit renvoyé ; ne passez donc jamais une racine vide. |
| Retour | `SessionPlan` avec un nouveau `SessionId`, `BuildProfileId` = la disposition connue (`"Omsi23004"`) ou `"unrecognized"` pour un build d'OMSI 2 sans disposition connue, le `Spec` d'entrée, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (chemins de destination `plugins\OmsiLaunch.*` plus `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (un `PlannedModule` par [module runtime](runtime-modules.md) demandé ou activé par le contenu ; accesseur `EffectivePlannedModules`). `IsRunnable` vaut `true` exactement lorsqu'aucun code de diagnostic ne commence par `OL_E_`. Les diagnostics informatifs (`plugin.integrity.reference` avec le message `self` ou `manifest`, `session_profile.selected`) ne rendent jamais un plan non exécutable. |
| Erreurs portées par le résultat | Chaque erreur de planification est un diagnostic, pas une exception : `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (le message porte le code de l'écran de démarrage/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (la closure du plugin installée dans `plugins\` est vérifiée par rapport au manifeste de version au moment de la planification), `OL_E_RUNTIME_ARTIFACT_MISSING` (le message peut porter `OL_E_RELEASE_MANIFEST_INVALID`). Conditions complètes : [règles de validation de LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Exceptions levées | `OperationCanceledException` si le jeton est déjà annulé à l'entrée (le seul point de contrôle) ; `ArgumentException`/`NotSupportedException` pour des chemins racine syntaxiquement invalides ; `NullReferenceException`/`ArgumentNullException` pour des membres requis null ; `System.Text.Json.JsonException` pour un manifeste de version syntaxiquement invalide. |
| Annulation | Vérifiée une fois à l'entrée. La planification est ensuite un travail synchrone sur le système de fichiers. |
| Session Running requise | Non. |
| Modifie l'état d'OMSI | Non. |
| Modifie le système de fichiers | Non (lit `Omsi.exe`, les fichiers de contenu, la closure du plugin, le manifeste). Les valeurs des paramètres ne sont pas validées ici (seulement l'existence et l'accessibilité en écriture de la clé) ; une valeur invalide échoue au démarrage avec `OL_E_INVALID_SETTING_VALUE`. |
| Transaction / restauration | Aucune. |
| Limitations | Demander un mode `Date`/`Time`/`Year` autre que `Unset`, un mode `Weather` autre que `Unset`, un champ quelconque de `PlayerVehicle`, des documents `Input`, `EntrypointIdentity` ou `WorldMode.LastMapState` produit `OL_E_CAPABILITY_UNAVAILABLE` et un plan non exécutable sur ce build (entrées de `UnsupportedRequestedFeatures` portant la preuve de leur catalogue, `UNAVAILABLE` ou `PARTIAL`). |
| Stabilité | `STABLE_BETA`. |
| Exemple | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Objectif | Démarrer une session OMSI gérée et transactionnelle à partir d'un plan exécutable : acquérir le bail d'installation, récupérer un journal obsolète, valider la closure du plugin permanent, prendre un instantané des fichiers de session et appliquer leurs overlays, créer le handoff de démarrage, le slot de télémétrie et la boîte aux lettres runtime, démarrer `Omsi.exe`, consigner le processus dans le journal et confier la session à un superviseur en arrière-plan. Capacité publique `session.start`. |
| Paramètres | `plan` : un `SessionPlan` avec `IsRunnable == true`. La spécification contenue dans le plan est replanifiée ; seul `plan.SessionId` est conservé du plan de l'appelant. `plan.Spec.Behavior.StartupTimeoutSeconds` doit être compris entre 1..600. |
| Retour | `SessionHandle(plan.SessionId)` dès que `Omsi.exe` a été créé et consigné (état `WaitingForPlugin`), ou dès que le chemin de démarrage a échoué (état `Failed`). N'attend pas le gameplay ; utilisez `WaitForAsync(session, SessionState.Running, ...)`. |
| Exceptions levées | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` lorsque `plan.IsRunnable` est faux ; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` lorsque la nouvelle planification n'est pas exécutable (par exemple `Omsi.exe` modifié, contenu supprimé, closure du plugin manquante) ; `InvalidOperationException("Duplicate session id.")` lorsqu'une session avec le même identifiant est encore enregistrée (appelez d'abord `CloseAsync`) ; `ArgumentOutOfRangeException` lorsque `StartupTimeoutSeconds` est en dehors de 1..600 ; `OperationCanceledException` en cas d'annulation avant ou pendant la nouvelle planification ; plus tout ce que lève `PlanSessionAsync`. Dans tous les cas où une exception est levée, aucune session n'est enregistrée. |
| Erreurs portées par le résultat | Tout échec postérieur à la nouvelle planification est intercepté dans le chemin de démarrage : la session est enregistrée, son état est `Failed`, et ses diagnostics contiennent `OL_E_START_SESSION` dont le message est le message interne (commençant par le code interne lorsqu'il y en a un) : `OL_E_INSTALLATION_BUSY` (bail détenu ou processus OMSI journalisé encore vivant), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (uniquement lorsque la nouvelle tentative différée avec les overlays de cette session ne parvient toujours pas à prouver la propriété), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. Le nettoyage peut ajouter `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (sortie d'OMSI non confirmée ; journal conservé) ou `OL_E_RESTORE_FAILED`. Les échecs ultérieurs sont signalés par le superviseur (voir [cycle de vie de la session](../concepts/session-lifecycle.md)). |
| Annulation | Avant/pendant la nouvelle planification : levée d'exception. Ensuite, le jeton est transmis à la transaction et à la création du processus ; une annulation à ce stade est traitée comme tout échec de démarrage (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), le processus (s'il a été créé) est arrêté et l'installation restaurée. |
| Session Running requise | Non. |
| Modifie l'état d'OMSI | Oui : crée le processus OMSI avec les variables d'environnement `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Modifie le système de fichiers | Oui, dans la racine de l'installation : `.omsilaunch\diagnostics\<sessionId>-host.log` (rétention : les 50 sessions les plus récentes), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (copié une fois pour l'écran de démarrage géré), overlays de session (correctifs de `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), suppressions de session (cibles ITX, `Texture\standard.ipr`, `closecheck`), et suppression définitive d'un `closecheck` obsolète préexistant lorsque `SuppressStaleClosecheckWarning` vaut true (diagnostic `closecheck.stale-removed`). |
| Transaction / restauration | Ouvre la transaction (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Chaque chemin de sortie de la session aboutit à une restauration. Voir [transactions et récupération](../concepts/transactions-and-recovery.md). |
| Limitations | Seuls `WorldMode.NewMap` avec `PresentedEntrypointIndex` et `WorldMode.SavedSituation` atteignent le gameplay. `WorldMode.LastMapState` est `UNAVAILABLE`. Les demandes de véhicule du joueur n'atteignent jamais cette méthode, car elles sont non exécutables dès la planification. |
| Stabilité | `STABLE_BETA` (les cycles de vie NEW_MAP et SAVED_SITUATION sont validés à l'exécution). |
| Exemple | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Objectif | Lire l'état sémantique du cycle de vie, les diagnostics collectés jusqu'ici et la liste bornée des événements runtime. Capacité publique `session.status`. |
| Paramètres | `session` : un handle renvoyé par `StartSessionAsync` et pas encore fermé. |
| Retour | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)` : un instantané immuable (les tableaux sont copiés sous le verrou de la session). `RuntimeEvents` n'est jamais `null` pour une session active. |
| Exceptions levées | `KeyNotFoundException` pour les handles inconnus/fermés. Ne lève jamais d'exception par ailleurs. |
| Annulation | Le jeton est ignoré (l'appel se termine de façon synchrone). |
| Session Running requise | Non. |
| Modifie OMSI / système de fichiers / transaction | Non / Non / Aucune. |
| Stabilité | `STABLE_BETA`. |
| Exemple | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Objectif | Interroger (toutes les 100 ms) jusqu'à ce que la session soit dans l'état `state`, ou dans un état terminal (`Completed`, `Failed`), ou que le timeout expire ; puis renvoyer l'état courant. |
| Paramètres | `state` : n'importe quel `SessionState`. Attendre un état transitoire déjà dépassé (ou jamais défini, voir [cycle de vie de la session](../concepts/session-lifecycle.md)) revient à attendre un état terminal ou le timeout. `timeout` : n'importe quel `TimeSpan` non négatif ou `Timeout.InfiniteTimeSpan`. |
| Retour | L'état au moment où l'attente s'est terminée. En cas de timeout, l'état est renvoyé, pas une exception : vérifiez vous-même `State`. Une attente de `Running` qui se termine en `Failed` revient immédiatement avec les diagnostics d'échec. |
| Exceptions levées | `KeyNotFoundException` ; `OperationCanceledException` lorsque le jeton de l'appelant est annulé (seule l'annulation par l'appelant se propage ; le timeout interne, non). |
| Annulation | Jeton de l'appelant respecté à chaque intervalle de 100 ms. |
| Session Running requise | Non. |
| Modifie OMSI / système de fichiers / transaction | Non / Non / Aucune. |
| Stabilité | `STABLE_BETA`. |
| Exemple | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Objectif | Demander l'arrêt canonique. Positionne l'indicateur d'arrêt et revient immédiatement ; le superviseur observe l'indicateur dans sa boucle de 100 ms, appelle `TerminateProcess` sur `Omsi.exe`, attend la sortie, marque le journal `ProcessExited`, restaure chaque fichier appartenant à la session et libère le bail. Il s'agit d'un arrêt forcé : la routine d'arrêt propre à OMSI ne s'exécute pas et OMSI ne réécrit pas `options.cfg` à la sortie (délibéré, protège la transaction). L'arrêt coopératif par `WM_CLOSE` n'est pas implémenté (décision produit ; lors de la clôture d'exécution, OMSI ne s'est pas fermé dans les 30 s suivant `WM_CLOSE`, `L05b`). Capacité publique `session.stop`. |
| Paramètres | `session`. |
| Retour | Tâche terminée ; n'attend ni l'arrêt ni la restauration. Utilisez `WaitForAsync(session, SessionState.Completed, ...)` pour observer la fin. |
| Exceptions levées | `KeyNotFoundException`. |
| Annulation | Jeton ignoré. |
| Session Running requise | Non. Idempotent ; un arrêt demandé avant le démarrage du superviseur est honoré dès que celui-ci démarre ; un arrêt sur une session terminale est sans effet. |
| Modifie l'état d'OMSI | Oui : met fin au processus OMSI (code de sortie 1). |
| Modifie le système de fichiers | Indirectement : déclenche la restauration, la suppression du journal et la suppression des sauvegardes par le superviseur. |
| Transaction / restauration | Déclenche `ProcessExited` → `Restoring` → `Restored`. Les modifications côté runtime effectuées via `ExecuteRuntimeAsync` (écritures de l'horloge, véhicules créés en cours de session, variables de script, textures D3D) ne sont pas restaurées ; elles disparaissent avec le processus. |
| Stabilité | `STABLE_BETA`. |
| Exemple | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Objectif | Libérer le handle du consommateur sans laisser la transaction en suspens : si la session n'est pas terminale, demander l'arrêt canonique ; puis attendre la tâche de cycle de vie du superviseur (sortie du processus, restauration, libération du bail) ; puis oublier la session. |
| Paramètres | `session`. |
| Retour | Se termine lorsque la session est terminale et supprimée. Après le retour, le handle est inconnu (`KeyNotFoundException` à tout appel ultérieur, y compris un second `CloseAsync`). |
| Exceptions levées | `KeyNotFoundException` ; `OperationCanceledException` si l'appelant annule pendant l'attente du superviseur. Dans ce cas, la session n'est pas supprimée et le superviseur continue de s'exécuter ; appelez à nouveau `CloseAsync`. |
| Annulation | S'applique uniquement à l'attente ; n'annule jamais la restauration. |
| Session Running requise | Non. |
| Modifie l'état d'OMSI | Oui lorsque la session est encore active (comme `StopAsync`). |
| Modifie le système de fichiers | Indirectement (restauration par le superviseur). |
| Transaction / restauration | Garantit que la transaction est menée à son terme avant la libération du handle (lorsque le superviseur a été démarré). Pour une session qui a échoué avant le démarrage du superviseur, le chemin de démarrage a déjà restauré ou signalé `OL_E_RESTORE_DEFERRED`. |
| Stabilité | `STABLE_BETA`. |
| Exemple | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Objectif | Exécuter une opération runtime publique dans le processus OMSI en cours d'exécution via la boîte aux lettres de session à requête unique (mappée en mémoire, 64 KiB, requête liée à l'identifiant de session et à l'identifiant de requête). Le plugin exécute l'opération sur le thread d'interface d'OMSI. Catalogue des opérations : [contrôle runtime](runtime-control.md) et [capacités](capabilities.md). |
| Paramètres | `command.SessionId` doit être égal à `session.SessionId`. `command.RequestId` : `ulong` choisi par l'appelant ; utilisez un compteur strictement croissant par processus (les helpers D3D commencent à 30 000, le propriétaire CLI à 10 001/50 000). `command.Operation` : un identifiant d'opération publique issu de `PublicCapabilityRegistry.PublicRuntimeOperationIds` (par exemple `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments` : valeurs de type chaîne indexées par des noms ordinaux ; les noms requis pour chaque opération proviennent de `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout` : mesuré à partir du moment où la requête est déposée dans la boîte aux lettres (l'attente derrière une autre commande en cours n'est pas comptée). La CLI utilise 5 s (15 s pour `road-vehicles.spawn`) en tant que propriétaire et 8 s / 30 s en tant que client. |
| Ordre des vérifications | 1. Validation par le registre (avant la recherche de la session) : opération inconnue ou `internal.*` → résultat `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN` ; argument requis manquant (absent ou composé d'espaces) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Recherche de la session → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. État différent de `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Requête dans la boîte aux lettres. 6. Les valeurs de résultat dont la clé commence par `internal_` ou se termine par `_address`, `_pointer`, `_vmt` sont retirées. |
| Retour | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. En cas de succès, `Values` contient les chaînes sémantiques de l'opération (documentées pour chaque opération dans [contrôle runtime](runtime-control.md)). |
| Erreurs portées par le résultat | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (registre) ; `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (le résultat du plugin a dépassé la boîte aux lettres ; les résultats de liste bornée sont raccourcis avec `truncated=true` à la place) ; `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` avec une clé dérivée) ; chaque code `OL_E_D3D_*` (avec `Values["detail"]` et `Values["native_status"]`) ; `OL_E_CAPABILITY_UNRESOLVED` (la capacité native de l'opération n'est pas résolue sur le build en cours d'exécution) ; et chaque échec typé côté plugin avec son propre code dans `ErrorCode` et le message dans `Values["detail"]` (Beta 4 ; seul un échec sans code catalogué est `OL_E_RUNTIME_OPERATION_FAILED`). Codes qui arrivent de cette manière : `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (vérifications côté plugin), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Voir [codes d'erreur](errors.md). |
| Exceptions levées | `KeyNotFoundException` ; `InvalidOperationException` avec `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (boîte aux lettres déjà libérée par le superviseur), `OL_E_RUNTIME_CHANNEL_BUSY` (le slot contient encore une requête abandonnée), `OL_E_RUNTIME_REQUEST_ID_REUSED` (une réponse obsolète pour le même identifiant de requête se trouve encore dans le slot) ; `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")` ; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (réponse corrompue, étrangère ou non concordante) ; `ArgumentOutOfRangeException` lorsque la requête sérialisée dépasse la boîte aux lettres ; `OperationCanceledException`. |
| Annulation | Respectée pendant l'attente de la barrière par session et toutes les 20 ms pendant l'interrogation de la réponse. Une annulation en cours de traitement ne réinitialise pas le slot : l'appel suivant sur cette session peut échouer avec `OL_E_RUNTIME_CHANNEL_BUSY` jusqu'à ce que le plugin publie sa réponse (qui est alors écartée comme obsolète). Préférez le timeout ; un timeout réinitialise le slot et une réponse tardive est détectée et écartée. |
| Session Running requise | Oui (`SessionState.Running`) ; sinon `OL_E_SESSION_NOT_RUNNING` est levée. La boîte aux lettres existe jusqu'à ce que le superviseur la libère pendant la restauration. |
| Modifie l'état d'OMSI | Dépend de l'opération : les opérations `Read`, non ; les opérations `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) modifient un état interne au processus qui n'est pas restauré. |
| Modifie le système de fichiers | Aucune écriture de l'hôte. OMSI peut écrire ses propres fichiers en conséquence (non suivis). |
| Transaction / restauration | Aucune. |
| Limitations | Une seule commande en cours par session (les appels sur une même session sont sérialisés). La requête et la réponse sont chacune limitées à 64 KiB moins 8 octets ; les charges utiles de pixels D3D à 48 KiB. `internal.road-vehicles.make-basic` est `INTERNAL` et inaccessible. `weather.set` et `weather.metar.set` sont `EXPERIMENTAL`. `timetable.logs.read` n'est pas borné et peut renvoyer `OL_E_RUNTIME_RESPONSE_TOO_LARGE` sur de grands horaires. `camera.lock` est `EXPERIMENTAL` ; il nécessite un véhicule du joueur et est validé à l'exécution (`CAM01`), bien que la chaîne `RuntimeValidation` du registre indique encore `STATICALLY_VALIDATED`. Les handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) ont une portée limitée à la session. |
| Stabilité | Transport et contrat `STABLE_BETA` ; la stabilité de chaque opération suit `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`), avec les exceptions ci-dessus. |
| Exemple | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Objectif | Renvoyer le catalogue des capacités évalué pour une installation : un `Capability(Name, Available, EvidenceState, Reason)` par entrée de `PublicCapabilityRegistry` qui n'est pas `InternalOnly`. `Name` est l'identifiant du catalogue, `EvidenceState` la preuve du catalogue (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` est calculé à partir de la plateforme, de la vérification OMSI 2, de la classification et des capacités natives dont l'entrée a besoin (voir [capacités](capabilities.md#getcapabilitiesasync)). |
| Paramètres | `installation.RootPath` : l'installation sondée (plateforme, `Omsi.exe`). `ExpectedExecutableSha256` est ignoré. |
| Retour | Une entrée par entrée de catalogue non interne, par exemple `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` sur la disposition connue), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relation avec `PublicCapabilityRegistry` | Dérivée de celui-ci : le registre déclare les capacités, leurs opérations et leurs preuves ; cet appel les évalue pour une installation. |
| Exceptions levées | `OperationCanceledException` à l'entrée ; `ArgumentException` pour un chemin racine vide. |
| Annulation | Vérifiée une fois à l'entrée. |
| Session Running requise | Non. |
| Modifie OMSI / système de fichiers / transaction | Non / Non / Aucune. |
| Stabilité | Contrat d'appel `STABLE_BETA` ; le contenu de la liste est un inventaire maintenu à la main : `PARTIAL`. |
| Exemple | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Objectif | Énumérer le contenu installé et renvoyer des identités canoniques utilisables dans un `LaunchSpec`. La découverte ignore les points d'analyse (reparse points) (les cycles de jonctions ne peuvent pas la bloquer), lit les fichiers OMSI en Windows-1252 (UTF-8/UTF-16 marqués par un BOM respectés) et ne suit jamais les liens symboliques. |
| Paramètres | `query` et `scope` selon le tableau ci-dessous. `scope` est requis pour `Entrypoints` (identité de carte), `Repaints`, `FleetNumbers`, `Registrations` (identité de véhicule). |
| Retour | Liste triée de `ContentIdentity(Identity, Kind, DisplayName)`. Les identités sont des chemins relatifs à l'installation avec des barres obliques inverses ; les comparaisons sont insensibles à la casse. |
| Exceptions levées | `OperationCanceledException` à l'entrée ; `ArgumentException` lorsque `Entrypoints` est interrogé sans portée ou que la racine est vide ; `FileNotFoundException` (pas de code `OL_E_` ; la CLI la convertit en `OL_E_NOT_FOUND`) lorsque la carte ou le véhicule de la portée n'est pas installé. Une racine ou un répertoire de contenu manquant produit une liste vide, pas une erreur. |
| Annulation | Vérifiée une fois à l'entrée. |
| Session Running requise | Non. |
| Modifie OMSI / système de fichiers / transaction | Non / Non / Aucune. |
| Stabilité | `Maps`, `Situations`, `Vehicles` : `STABLE_BETA` (chaque plan validé à l'exécution est résolu par leur intermédiaire). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons` : `EXPERIMENTAL` (preuves statiques uniquement). |
| Exemple | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Valeurs et résultats de `ContentQueryKind` :

| Valeur | Portée | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | aucune | `maps\<dir>\global.cfg` | `map` | nom du répertoire de la carte |
| `Situations` | aucune | `situations\...\<file>.osn` | `situation` | identité de carte référencée par le `.osn` (peut être `null`) |
| `Vehicles` | aucune | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` ou nom de fichier |
| `Repaints` | identité de véhicule (requise ; sans elle : liste vide) | `<cti path>#item:<ordinal>` | `repaint` | nom `[item]` |
| `Hofs` | aucune | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | identité de véhicule (requise ; sans elle : liste vide) | chemin source `[number]` relatif au véhicule | `fleet-number` | `null` |
| `Registrations` | identité de véhicule (requise ; sans elle : liste vide) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | première ligne de valeur (`null` pour free) |
| `Addons` | aucune | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | identité de carte (requise ; sans elle : `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | libellé du point d'entrée |
| `SessionProfiles` | aucune | nom du répertoire du paquet (l'id de `/predefined-profile`) | `session-profile`, ou `session-profile-invalid` lorsque le paquet ne se compile pas | `name` du profil, ou le code `OL_E_SESSION_PROFILE_*` d'un paquet invalide |
| `RuntimeModules` | aucune | id du module | `runtime-module`, ou `runtime-module-invalid` lorsqu'il ne peut pas être chargé | `name` du module, ou le code `OL_E_MODULE_*` |

Un point d'entrée est sélectionné par `PresentedEntrypointIndex` ou par `EntrypointIdentity` (une identité de découverte ou un libellé exact). Le planificateur résout une identité vers le libellé qu'OMSI présente (OMSI ne présente que le premier enregistrement de chaque libellé exact) et le plan la liste sous `ResolvedContent` avec le type `entrypoint` ; `world.entrypoint-identity` est `EXPERIMENTAL` tant qu'elle n'est pas validée à l'exécution.

### `ListSessionProfilesAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Objectif | Lister chaque paquet de profil de session sous `<root>\.omsilaunch\session-profiles\` avec ses préréglages, afin qu'un frontend puisse les proposer sans analyser le YAML. CLI `/list:SessionProfiles`. |
| Retour | Un `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` par répertoire de paquet, trié par `Id`. `Presets` sont des `SessionProfilePresetSummary(Index, Id, Name)` dans l'ordre des index. Chaque paquet est compilé avec les mêmes règles strictes qu'un lancement (`SessionProfileCompiler`) ; un paquet qui ne se compile pas est listé avec `IsValid = false`, son code `OL_E_SESSION_PROFILE_*` et son message, et sans préréglage. Pas de répertoire de profils : liste vide. |
| Exceptions levées | `OperationCanceledException` à l'entrée. Les erreurs de compilation sont signalées dans le résultat, pas levées. |
| Session Running requise | Non. |
| Modifie OMSI / système de fichiers / transaction | Non / Non / Aucune. |
| Stabilité | `STABLE_BETA` (contrôle hors ligne `session-profiles.discovery`). |
| Exemple | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Objectif | Découvrir les [modules runtime](runtime-modules.md) installés sous `plugins\OmsiLaunch.Modules` avec leur intégrité et leurs paramètres. Lecture seule. Le chargement est ouvert : aucun module n'est vérifié par rapport à une liste ou à une signature. |
| Retour | Un `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` par répertoire de module, trié par nom de répertoire. `Activation` vaut `requested`, `content` ou `default` ; `IsValid` est faux avec un code `OL_E_MODULE_*` lorsque le module ne peut pas être chargé ; `Settings` liste les paramètres que le module déclare sous la forme `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Exceptions levées | `OperationCanceledException` à l'entrée. |
| Session Running requise | Non. CLI : `/list:RuntimeModules` (enveloppe `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspect | Détail |
| --- | --- |
| Signature | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Objectif | Signaler ou achever une transaction durable obsolète (`<root>\.omsilaunch\journal.json`) laissée par un propriétaire qui a planté. Prend le bail d'installation pendant toute la durée de l'appel, de sorte qu'il ne restaure jamais sous une session en cours de démarrage. Capacité publique `session.recover` ; CLI `/recovery-status` et `/recover`. |
| Paramètres | `installation.RootPath` : la racine de l'installation (normalisée avec `Path.GetFullPath`). `restore` : `false` = rapport uniquement ; `true` = restaurer, vérifier, supprimer le journal et les sauvegardes. |
| Retour | `RecoveryStatus(Pending, Recovered, Diagnostics)` : `Pending` = un journal existait au début de l'appel ; `Recovered` = une restauration a été demandée, s'est exécutée, et aucun journal ne subsiste ; `Diagnostics` = notes de restauration (`restore.session-artifact-removed` avec `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), vide lorsque rien n'a été restauré. |
| Exceptions levées | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` lorsque le bail est détenu ; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` lorsque le PID + l'heure de création + le chemin de l'exécutable du journal correspondent encore à un processus vivant, ou (journal au-delà de `HandoffCreated` sans PID) lorsqu'un `Omsi.exe` quelconque de cette racine est en cours d'exécution ; `IOException` avec `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, ou un message de vérification (« Restore hash mismatch: ... », « Restore presence mismatch: ... ») ; `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` pour un journal corrompu ; `ArgumentException` pour une racine vide ; `OperationCanceledException`. Chaque fois qu'une exception est levée après le début d'une restauration, le journal est conservé et l'appel suivant rejoue de manière idempotente. |
| Annulation | Transmise aux écritures du journal/des sauvegardes ; une annulation en cours de restauration laisse le journal en attente. |
| Session Running requise | Non (il refuse tant qu'un propriétaire est actif). |
| Modifie l'état d'OMSI | Non. |
| Modifie le système de fichiers | Uniquement avec `restore == true` : réécrit les originaux à partir des sauvegardes vérifiées (octets, heure de dernière écriture, heure de création, attributs ; originaux en lecture seule pris en charge ; écriture directe (write-through) + vidage ; aucun `*.omsilaunch.tmp` laissé), supprime les artefacts de session, supprime `journal.json` et `backup\<sessionId>`. |
| Transaction / restauration | Achève la transaction en attente (`Restoring` → `Restored` → journal supprimé). |
| Stabilité | `STABLE_BETA` : chemin de rapport et récupération après sortie prématurée (matrice RV-008), récupération après une restauration échouée (clôture d'exécution `F01`), refus en présence d'un propriétaire actif et dans la fenêtre précédant le PID (`S04`) et récupération différée antérieure à l'empreinte au démarrage (`S05`) ; voir [état de la validation à l'exécution](../status/runtime-validation-status.md). |
| Exemple | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Types de contrat

<a id="optional-values-and-semantic-primitives"></a>
### Valeurs optionnelles et primitives sémantiques

| Type | Définition | Remarques |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)` ; `IsSet`, `Unset` statique, `Set(T)` statique | Distingue « non demandé » de « demandé avec une valeur ». La forme JSON est documentée dans la [référence LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Enum de type byte. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Validé uniquement lorsque `DateTimeMode.Explicit` (mois 1..12, jour 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Validé uniquement lorsque `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Famille LaunchSpec

Tous les records ci-dessous sont documentés propriété par propriété dans la [référence LaunchSpec](launchspec.md) ; ce tableau fixe l'inventaire des types.

| Type | Objectif | Stabilité |
| --- | --- | --- |
| `LaunchSpec` | Record racine de la requête, avec les accesseurs `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures` qui substituent des valeurs par défaut aux membres optionnels `null`. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (épinglage facultatif de `Omsi.exe`, vérifié lors de la planification). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Sélection du monde. `WorldMode` : `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsolète de `LastMapState` ; jamais « le .osn le plus récent »). `EntrypointMode` : `Unset`, `PresentedIndex`, `Identity` (calculé à partir de `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation` : `STABLE_BETA` ; `LastMapState` : `UNAVAILABLE` ; `EntrypointMode.Identity` : `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode` : `Unset`, `Explicit`, `System`. Tout mode autre que `Unset` rend le plan non exécutable. | `PARTIAL` (preuve du plan `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode` : `Unset`, `Preset`, `Icao`, `RealCurrent`. Tout mode autre que `Unset` rend le plan non exécutable. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Tout champ défini rend le plan non exécutable. | `PARTIAL` |
| `EnvironmentSpec` | Huit groupes `IReadOnlyDictionary<string, OptionalValue<string>>` de paramètres sémantiques de `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument` ; toute valeur définie rend le plan non exécutable. | `PARTIAL` |
| `DiagnosticsSpec` | Six booléens ; transportés, non consommés. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode` : `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode` : `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Provenance d'un profil de session compilé (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [Gestion des HOF](hof-management.md) de la session : `Operations` (liste de `HofOperationSpec`). Champ `LaunchSpec.HofManagement` ; accesseur `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Modules runtime](runtime-modules.md) de la session : `Enable` (ids de modules), `Disable` (ids de modules tenus à l'écart), `Settings` (`"<module id>.<key>"` vers valeur) ; accesseurs `EffectiveDisable`, `EffectiveSettings`. Champ `LaunchSpec.Modules` ; accesseur `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | L'évaluation d'un module par le plan : `Id`, `Version`, `Status` (`ELIGIBLE` ou `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` ou `default`), `Settings` ; `IsEligible`, `EffectiveSettings`. Constantes `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Un paramètre de module effectif : `Key`, `Value`, `Source` (`request`, `settings-file` ou `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Vue de découverte d'un module installé ; voir `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Un paramètre qu'un module déclare : `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Une opération HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Levée par la planification HOF (`OmsiLaunch.Core`) ; `Code` est un code `OL_E_HOF_*`. L'API publique la signale sous forme de diagnostics de plan. | `STABLE_BETA` |
| `SessionProfileSummary` | Vue de découverte d'un paquet (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Voir `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Un préréglage d'un résumé (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (transporté, la restauration a toujours lieu), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, 180 par défaut), `ShutdownTimeoutSeconds` (transporté, non consommé). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Types de plan et d'état

| Type | Champs | Remarques |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (nouveau `Guid` par plan), `BuildProfileId` (`"Omsi23004"` ou `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (liste de `ContentIdentity` : `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (chemins relatifs distincts de `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` avec `STATICALLY_VALIDATED` ou `UNAVAILABLE` ; capacités natives de démarrage avec `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (entrées `Capability` pour les fonctionnalités demandées mais non prises en charge), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Un record public : il peut être modifié ou devenir obsolète, c'est pourquoi `StartSessionAsync` replanifie. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, OS x64 et hôte x64), `LegacyPlatform` (toujours `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (tous égaux à `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` est une valeur de `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), sauf dans `SessionPlan.RequiredCapabilities` : une exigence de plateforme ou de contenu est `STATICALLY_VALIDATED` ou `UNAVAILABLE`, une capacité native `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Les mutations de présentation utilisent les clés `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (dictionnaire de chaînes optionnel). | Les codes commençant par `OL_E_` sont des erreurs, `OL_W_` des avertissements, tout le reste est informatif. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Les diagnostics de session n'incluent pas les diagnostics du plan. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (heure de réception par l'hôte), `Sequence` (à partir de 1, par session), `Data`, `SessionTickMilliseconds` (l'horloge de session monotone du plugin lorsqu'il a émis l'événement ; null pour les événements de l'hôte). | Limité aux 256 événements les plus récents (les plus anciens sont abandonnés). Les événements du plugin arrivent dans l'ordre par l'anneau de télémétrie ; les événements perdus lors d'un débordement de l'anneau sont comptés par `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Local au processus. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Voir `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Voir `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Enum de type byte, dans l'ordre de déclaration : `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` et `EnteringGameplay` ne sont jamais définis par le service actuel ; `Snapshotting` est transitoire et pratiquement inobservable. États terminaux : `Completed`, `Failed`. Sémantique complète : [cycle de vie de la session](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Types de contrôle runtime

| Type | Définition | Stabilité |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Codec statique utilisé par l'hôte et le plugin pour l'enveloppe de la boîte aux lettres : magic `0x4F4C5243` (« OLRC »), version 1, en-tête little-endian de 72 octets (magic, version, kind 1 = requête / 2 = réponse, longueur totale, `Guid` de session, identifiant de requête, longueur de la charge utile, SHA-256 de la charge utile) suivi d'une charge utile JSON en UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL` : public parce que les deux extrémités du pont le partagent ; pas une surface d'intégration ; le format peut changer avec la version du protocole. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — ce que l'hôte publie à destination du plugin dans le fichier mappé en mémoire `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)` : un module planifié transporté par le handoff (`StartupHandoff.Modules`). | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty` : l'horloge de départ résolue d'une session NEW_MAP transportée par le handoff (`StartupHandoff.Start`) ; un champ nul conserve la valeur propre d'OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Codec : magic `0x4F4C5348`, version 7 (lit 3 à 7 ; la version 5 ajoute les modules planifiés, la version 6 les paramètres effectifs de chaque module, la version 7 la date/heure de départ ; les bits d'indicateur 0x04 et 0x08 portent `PluginTrace` et `NativeTrace`), en-tête de 64 octets avec intégrité de la charge utile par SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Anneau d'événements du plugin vers l'hôte dans le mapping `OMSILAUNCH_TELEMETRY_NAME` : magic `0x52544C4F` ("OLTR"), version 1, 64 slots numérotés de 2048 octets. `Initialize`, `Write`, `IsRing`, `ReadSince` (renvoie les événements postérieurs à un numéro de séquence et le nombre d'événements perdus). | `INTERNAL` |

Le plugin rejette un handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`) sauf si `WorldMode` vaut `NewMap` ou `SavedSituation`, `HeadlessStart` vaut true, `PlayerVehicleEnabled` vaut false, les deux modes de date/heure valent `Unset`, et qu'une situation enregistrée a une identité non vide. Le planificateur applique les mêmes contraintes plus tôt, de sorte qu'un plan exécutable ne déclenche jamais ce rejet.

<a id="capability-registry-types"></a>
### Types du registre des capacités

| Type | Objectif |
| --- | --- |
| `PublicCapabilityRegistry` | Le catalogue canonique des capacités. `ProtocolVersion` (`"0.1"`), `All` (chaque `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (les 48 identifiants d'opération concrets qu'un frontend peut transmettre), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (route de commande vers identifiant d'opération), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (renvoie `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Appliqué par `ExecuteRuntimeAsync`, la CLI, le plan de contrôle local et le plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (une valeur de `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote` ; dérivés `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (accepte `cursor`/`limit`, voir [pagination](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Ce qu'une opération runtime modifie : `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | Le vocabulaire de preuve : `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Catalogue complet : [capacités](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Méthodes d'extension `D3DRuntimeApi`

Wrappers typés au-dessus de `ExecuteRuntimeAsync` pour les opérations `d3d.*` (`EXPERIMENTAL`, capacité `PublicExperimental` `d3d.texture`). Ils allouent les identifiants de requête à partir d'un compteur global au processus commençant à 30 000 et utilisent un timeout par défaut de 5 s (sauf `GetD3DStatusAsync`, qui en exige un).

| Méthode | Opération | Arguments et limites |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | aucun |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | largeur/hauteur 1..4096, niveaux 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | niveau 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)` : x/y 0..4095, largeur/hauteur 1..4096, pixels ≤ 48 KiB (encodés en Base64 lors de la transmission) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | une libération répétée est rejetée avec `OL_E_D3D_RESOURCE_RELEASED` |

Types : `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)` ; `D3DDeviceState` : `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped` ; `D3DTextureHandle(Value)` avec `Value = "d3dtex-<session id N>-<16 hex>"` ; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)` ; `D3DTextureResourceState` : `Live`, `Released`, `Stale` ; `D3DTextureFormat` : `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Erreurs : un résultat en échec est relevé sous la forme `OmsiRuntimeException(Code, detail)`, où `Code` est le `ErrorCode` du résultat (ou `OL_E_RUNTIME_OPERATION_FAILED` s'il est absent) et le message est `"<code>: <Values["detail"]>"` ; un résultat réussi sans valeurs, ou une chaîne d'état de périphérique inconnue, lève `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Tout ce que lève `ExecuteRuntimeAsync` se propage sans modification. La gestion de la réinitialisation du périphérique est validée à l'exécution : une réinitialisation fait passer le périphérique par `Resetting` avant de revenir à `Ready` et invalide chaque texture active (`OL_E_D3D_STALE_RESOURCE_HANDLE`, clôture d'exécution `D01`) ; l'entrée de catalogue `d3d.texture` l'enregistre. La transition `Lost` ne peut pas être produite depuis l'extérieur du produit et n'est couverte que hors ligne.

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### Types de contrat du processus

| Type | Contenu |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Utilisé uniquement par la CLI ([codes de sortie](exit-codes.md)) ; l'API ne termine jamais le processus. |
| `PublicErrorCategory` | Constantes de chaîne utilisées dans les enveloppes d'erreur de la CLI et du contrôle : `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Un `const string` par code (143 : 142 erreurs `OL_E_` et 1 avertissement `OL_W_`) et `All`, le catalogue `PublicErrorDescriptor(Code, Category)`. Catégories : `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Référence : [codes d'erreur](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Propriété `Code` plus message ; levée uniquement par `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (identité de l'installation et confinement des chemins)

Stabilité : `STABLE_BETA` (fonctions pures, aucune E/S, aucun état OMSI, aucune modification du système de fichiers, aucune participation à une transaction, aucune session Running requise). C'est la définition unique utilisée par le bail d'installation, le nom du pipe de contrôle local, le confinement des ressources des profils de session, la validation des cibles des textures Internet et le chemin du modèle d'apparition runtime.

| Membre | Comportement |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` sans séparateurs finaux, sauf pour une racine de lecteur (`C:\`), qui est conservée. Résout les segments `.` et `..`, traite `/` et `\` de la même manière et fusionne les séparateurs répétés. Ne résout **pas** les jonctions ni les liens symboliques. Lève `ArgumentException` pour une racine null ou vide. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` en majuscules. Les écritures lexicalement équivalentes d'une même racine (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) partagent une même clé ; des racines différentes (`C:\OMSI-A`, `C:\OMSI-B`) jamais. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Résout `candidate` (relatif à `root`, ou absolu) et renvoie `true` uniquement lorsqu'il se trouve strictement sous `root` ; `relativePath` est l'écriture canonique avec `\`. Utilise les segments de `Path.GetRelativePath`, de sorte qu'un voisin tel que `C:\OMSI-A\x` n'est jamais à l'intérieur de `C:\OMSI` ; la racine elle-même, les autres volumes et les échappements par `..` renvoient `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Découpe sur `/` et `\`, en supprimant les segments vides. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Profils de session (`OmsiLaunch.Core`)

Stabilité : `EXPERIMENTAL`. Ces types compilent un [profil de session](session-profiles.md) YAML (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, schéma `omsilaunch.session-profile/v1`) en `LaunchSpec`. L'option CLI `/predefined-profile:<id> /predefined-profile-index:<n>` utilise exactement ces appels ; un intégrateur peut les utiliser pour démarrer un profil via l'API.

| Membre | Comportement |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Lit et valide le paquet. `id` doit être un simple nom de répertoire (`OL_E_SESSION_PROFILE_PATH_ESCAPE` sinon) ; `presetIndex` est compris entre 1..99 (`MinPresetIndex`..`MaxPresetIndex` ; `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` sinon). Fichier manquant : `OL_E_SESSION_PROFILE_NOT_FOUND` ; taille supérieure à `MaxBytes` (256 KiB), YAML invalide, ancres, clés inconnues ou `id` différent du nom du répertoire : `OL_E_SESSION_PROFILE_INVALID` ; autre `schema` : `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Les chemins des ressources sont confinés au paquet (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Chaque échec est une `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Renvoie `baseline` avec le profil appliqué : pour `NewMap`, le bloc `new` du profil (carte, point d'entrée, et toute date/heure/année/météo) ; les `settings` du préréglage fusionnés par-dessus `Environment.General` ; les `Presentation`, `InternetTextures` et `Behavior` du préréglage lorsqu'ils sont présents ; et `SessionProfile` = `profile.Metadata`. Pour `NewMap`, il vérifie aussi la carte par rapport à la liste `compatibility` du profil (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | La vérification de compatibilité pour les autres modes (pour `SavedSituation`, la carte est lue dans le `.osn`). La CLI l'appelle une fois le `WorldSpec` final construit. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144`, et les clés acceptées pour chaque mapping YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | Le paquet chargé ; `Preset` est uniquement le préréglage sélectionné. |
| `SessionProfileException(string code, string message)` | `IOException` avec `Code` (l'un des codes `OL_E_SESSION_PROFILE_*`) ; le message est `"<code>: <message>"`. |

La CLI rejette en outre les options de ligne de commande qui entrent en conflit avec le profil (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`) ; cette vérification ne fait pas partie du compilateur. Voir [profils de session](session-profiles.md#precedence-and-override-conflicts) pour l'ordre de fusion complet.

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### Types de plateforme (`OmsiLaunch.Process`)

| Type | Stabilité | Utilisation |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` en tant que type du paramètre de constructeur de `OmsiLaunchService` | Détecte la plateforme, vérifie l'accessibilité en écriture, démarre, observe, arrête et attend `Omsi.exe`. Passez `new CurrentWindowsX64Platform()` ; l'implémenter vous-même n'est pas pris en charge. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | La seule implémentation : hôte Windows x64, `CreateProcessW` pour `Omsi.exe`, `TerminateProcess` pour l'arrêt canonique. Ses méthodes sont appelées par le service ; les intégrateurs se contentent de le construire. Membres (partagés avec `IRuntimePlatform`) : `Detect(root)` renvoie le `RuntimePlatformInfo` du plan ; `ValidateCurrent(info)` lève `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` lorsque l'hôte ne peut pas exécuter de session ; `IsInstallationWritable(root)` sous-tend `OL_E_INSTALLATION_NOT_WRITABLE` ; `StartAsync(request, sha256)` crée `Omsi.exe` et consigne l'identité du processus (PID, heure de création, chemin et hash calculé par le service ; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`) ; `HasExited`, `WaitForExitAsync` et `Terminate` l'observent et y mettent fin. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Publics dans l'assembly parce que le service et les tests les partagent. Pas une surface d'intégration ; `LaunchedProcess` encapsule en interne les handles de processus et de thread d'OMSI (ce ne sont pas des membres publics) et n'est jamais renvoyé par `IOmsiLaunch`. |

<a id="thread-safety"></a>
## Sécurité des threads

- `OmsiLaunchService` prend en charge les appels concurrents sur des sessions différentes : les sessions résident dans un `ConcurrentDictionary` et chaque modification propre à une session se produit sous le verrou privé de cette session.
- Les appels concurrents sur une même session sont sûrs mais sérialisés là où c'est nécessaire : `ExecuteRuntimeAsync` prend une barrière par session, de sorte qu'une seconde commande attend la première (son timeout commence lorsqu'elle est déposée).
- Le superviseur s'exécute sur une tâche du pool de threads (`Task.Run`) depuis le retour de `StartSessionAsync` jusqu'à ce que la session soit terminale ; il interroge la télémétrie et le processus toutes les 100 ms. Les appelants n'exécutent jamais de code du superviseur.
- `StopAsync` et `GetStatusAsync` se terminent de façon synchrone et peuvent être appelés depuis n'importe quel thread, y compris dans un gestionnaire `ProcessExit` (la CLI le fait avec un budget de 4 s).
- Aucun appel de l'API n'est lié à un thread ; aucun n'exige de contexte de synchronisation.

<a id="what-is-not-in-the-api"></a>
## Ce qui ne fait pas partie de l'API

- Aucun `IntPtr`, `nint`, handle Win32, adresse native, pointeur de VMT ni objet processus. Les valeurs de résultat dont les clés commencent par `internal_` ou se terminent par `_address`, `_pointer`, `_vmt` sont supprimées avant qu'un résultat ne quitte `ExecuteRuntimeAsync`.
- Aucune opération runtime `internal.*` : `internal.road-vehicles.make-basic` est `InternalOnly` dans le registre et renvoie `OL_E_RUNTIME_OPERATION_UNKNOWN` depuis l'API et la CLI.
- Aucune lecture ni écriture brute de la mémoire d'OMSI, aucun accès au niveau fichier à l'installation au-delà de ce que déclare un `LaunchSpec`.
- Aucun handle inter-processus : le [plan de contrôle local](local-control.md) est la seule voie inter-processus, et il n'accepte que `session.status`, `session.events`, `session.stop` et `runtime.execute`.
- Pas d'application du véhicule du joueur au démarrage. L'arrêt coopératif, `LAST_MAP_STATE`, la date/heure/météo de départ et les overlays de documents clavier/manette sont `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Résumé de la stabilité

| Surface | Stabilité |
| --- | --- |
| Constructeur de `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transport de `ExecuteRuntimeAsync` ; opérations `PublicStableBeta` | `STABLE_BETA` |
| Opérations `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| membres de spécification date/heure/météo/entrée, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| membres de spécification du véhicule du joueur | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implémentations de `IRuntimePlatform`, toutes les assemblies d'implémentation | `INTERNAL` |
| `WorldMode.LastSituation`, opérations `internal.*` | `UNAVAILABLE` |
