# Le plugin permanent

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Traduction de la [page originale en anglais](../../../concepts/permanent-plugin.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

OmsiLaunch contrôle OMSI depuis l'intérieur du processus OMSI grâce à un plugin installé une seule fois, sous `plugins\OmsiLaunch.*`, en tant que partie du produit. Il n'est jamais préparé, copié, capturé dans un instantané, restauré ni supprimé par une session. Cette page explique ce que contient la closure, comment OMSI la charge, comment l'hôte la valide avant chaque démarrage, comment l'hôte et le plugin communiquent (handoff, télémétrie, boîte aux lettres runtime), et ce que fait le plugin lorsqu'OMSI est démarré sans OmsiLaunch. Sources : `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, les trois stores en mémoire partagée), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` et `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## La closure (9 fichiers)

| Fichier sous `plugins\` | Rôle |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Descripteur de plugin OMSI. Son contenu est `[dll]` suivi de `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Shim d'exports natif x86 généré par DNNE 2.0.6. Exporte l'ABI de plugin d'OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) et héberge le runtime .NET. Porte la ressource de version du produit. |
| `OmsiLaunch.Plugin.dll` | Plugin managé (`net6.0-windows`, x86) : `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Manifeste des dépendances .NET du plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Configuration du runtime .NET (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Formats de transmission et enregistrements publics partagés avec l'hôte. |
| `OmsiLaunch.Builds.Omsi23004.dll` | Le profil de build : empreinte de l'exécutable, variables globales, dispositions des objets, adresses des méthodes. |
| `OmsiLaunch.Interop.dll` | Lecteurs et écrivains de mémoire internes au processus, construits sur le profil. |
| `OmsiLaunch.Native.x86.dll` | Pont natif (C++) : validation du build, hook de démarrage headless, application de l'heure, MakeVehicle, PlaceRandomBus, suppression des textures Internet, accès au périphérique D3D9. |

`RuntimeArtifactSet.Load` dérive cette liste du propre répertoire `plugins\` du contrôleur : les quatre fichiers nommés (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), chaque `OmsiLaunch.*.dll` de ce répertoire à l'exception de `OmsiLaunch.PluginNE.dll`, et le pont natif. `OmsiLaunch.Plugin.dll` doit en faire partie. Des DLL arbitraires ne sont jamais embarquées dans OMSI. L'outil de création de paquets (`tools/New-ReleasePackage.ps1`) écrit exactement les neuf fichiers ci-dessus.

<a id="how-omsi-loads-it"></a>
## Comment OMSI la charge

1. OMSI énumère `plugins\*.opl` et charge la DLL nommée dans `OmsiLaunch.Plugin.opl` : `OmsiLaunch.PluginNE.dll`.
2. Le shim DNNE démarre dans le processus OMSI le runtime .NET 6 x86 décrit par `OmsiLaunch.Plugin.runtimeconfig.json` et résout les exports managés de `OmsiLaunch.Plugin.dll`.
3. OMSI appelle `PluginStart`. OMSI peut l'appeler plus d'une fois pendant le démarrage ; seul le premier appel est pris en compte (garde `Interlocked.Exchange`), car un démarrage réussi détient un hook natif à usage unique. Les appels ultérieurs retournent immédiatement.
4. `PluginStart` vérifie d'abord `OMSILAUNCH_INTERNET_TEXTURES_MODE` : lorsqu'il vaut `Disabled`, la suppression native du téléchargeur est appliquée (`internet-textures.suppressed`, ou `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` lit le handoff (ci-dessous), valide le build dans le processus, arme le hook de démarrage headless, ouvre la boîte aux lettres runtime et planifie le démarrage du monde sur le thread d'interface d'OMSI via un callback `SetTimer`. Aucune commande runtime n'est jamais exécutée sur un thread de travail IPC ; tout s'exécute dans le callback du timer, sur le thread d'interface d'origine d'OMSI.
6. `PluginFinalize` supprime le timer, arrête le runtime (`NativeD3DShutdown`) et restaure le correctif des textures Internet.

Les exports `AccessVariable`, `AccessTrigger`, `AccessStringVariable` et `AccessSystemVariable` sont vides ; OmsiLaunch n'utilise pas le canal de plugin d'OMSI dédié aux variables de script.

<a id="integrity-validation-before-every-start"></a>
## Validation de l'intégrité avant chaque démarrage

`RuntimeArtifactSet.ValidateInstalled` s'exécute pendant `StartSessionAsync` (après la récupération anticipée, avant la préparation de la transaction) et pendant `PlanSessionAsync` (présence uniquement, via `LoadArtifacts`). Le diagnostic de plan `plugin.integrity.reference` indique quelle référence a été utilisée.

| Situation | Référence | Vérification par fichier | Erreurs |
| --- | --- | --- | --- |
| `release-manifest.json` présent à côté de `OmsiLaunch.exe` (paquet installé) | `manifest` | Le fichier installé doit exister et son SHA-256 doit être égal à l'entrée du manifeste pour `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (le manifeste n'a pas d'entrée pour un fichier requis), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Pas de manifeste (disposition de développement) | `self` | Présence plus cohérence interne : le hash du fichier installé doit être égal à celui du fichier du propre répertoire `plugins\` du contrôleur | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifeste illisible ou mal formé (tableau `files` absent, entrée sans `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Closure incomplète dans le répertoire du contrôleur | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (plan non exécutable) ; la CLI signale en outre `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` lorsque `plugins\OmsiLaunch.Plugin.opl` ou `OmsiLaunch.Native.x86.dll` est absent |

Seules les entrées `plugins/` du manifeste sont utilisées ; le manifeste est une donnée, jamais une politique. Le manifeste est généré par l'outil de création de paquets avec le SHA-256 de chaque fichier préparé. Lorsque le contrôleur s'exécute depuis la racine OMSI (la disposition de la version), la source et la destination sont le même répertoire ; sans manifeste, la vérification se réduit donc à la présence et à la cohérence interne, c'est pourquoi les paquets publiés contiennent toujours `release-manifest.json`. Correction en cas de différence : réinstallez le paquet afin que `plugins\` et `release-manifest.json` concordent.

Avant de patcher quoi que ce soit, le plugin résout et vérifie chaque cible native du catalogue du build par rapport à l'image chargée, puis transmet la table vérifiée à `OmsiLaunch.Native.x86.dll`, qui ne contient aucune adresse OMSI (télémétrie `native.capabilities`). Un démarrage dont la capacité `boot.headless-start` ou la capacité de mode monde est indisponible échoue avec `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`) ; un formulaire Start actif dans un état inattendu échoue avec `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Voir [compatibilité](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## De l'hôte vers le plugin : variables d'environnement

`CreateProcessW` démarre `Omsi.exe` avec l'environnement parent plus :

| Variable | Valeur | Consommateur |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID de la session (format `D`) | `CurrentRuntimeControl` étiquette les handles D3D avec lui (format `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` ouvre ce mapping en lecture seule |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` ou `Override` | `PluginStart` (seul `Disabled` a un effet dans le processus) |

Les trois mappings sont créés par l'hôte avant le démarrage du processus (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) et libérés à la fin de la tâche de cycle de vie de la session. Ce sont des objets noyau nommés avec la DACL par défaut de l'utilisateur qui lance ; tout processus du même utilisateur peut les ouvrir (modèle de confiance « même utilisateur » accepté, voir [limitations connues](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## Le handoff de démarrage

Un enregistrement mappé en mémoire, en lecture seule et à disposition fixe (`StartupHandoffWire`, magic `OLSH`, version 4 ; la version 3 est toujours acceptée par le lecteur). L'en-tête de 64 octets contient le magic, la version, la taille de l'en-tête, la taille totale, le GUID de la session, la taille de la charge utile et le SHA-256 de la charge utile. La charge utile contient `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 préfixé par sa longueur), `PresentedEntrypointIndex`, `WorldMode`, des indicateurs (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` et `TimeMode`. Le plugin recalcule le hash de la charge utile et rejette toute différence (`plugin.handoff.invalid`, erreur hôte `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Un mapping de plus de 1 MiB ou dont la taille est incohérente est rejeté de la même manière.

Le plugin n'accepte un handoff que lorsque `WorldMode` vaut `NewMap` ou `SavedSituation`, que `HeadlessStart` est défini, que `PlayerVehicleEnabled` n'est pas défini, que les modes de date et d'heure valent tous deux `Unset`, et qu'une situation enregistrée nomme son `.osn`. Tout le reste donne `plugin.request.unsupported` (erreur hôte `OL_E_CAPABILITY_UNAVAILABLE`). L'hôte définit toujours `HeadlessStart`.

<a id="telemetry-slot"></a>
## Slot de télémétrie

`OmsiLaunch.Telemetry.<session>` est un anneau d'événements (Beta 4, `TelemetryRingWire`) : un en-tête de 64 octets (magic `OLTR`, version 1, nombre de slots, taille de slot, séquence validée) et 64 slots numérotés de 2048 octets, chacun contenant un événement JSON UTF-8 `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` est l'horloge de session monotone du plugin). Le plugin est le seul écrivain : il invalide un slot, l'écrit, valide la séquence du slot, puis publie la séquence de l'anneau. L'hôte lit, dans l'ordre, tous les 100 ms, chaque événement validé depuis sa dernière lecture ; un slot écrasé pendant sa copie est compté comme perdu, jamais livré incohérent. Un hôte qui prend plus de 64 événements de retard reçoit l'événement runtime `telemetry.lost` avec le nombre exact. Un événement plus grand qu'un slot est remplacé par `telemetry.oversized` qui le nomme. Les événements sont ajoutés à `SessionStatus.RuntimeEvents` (limité aux 256 derniers) et pilotent le cycle de vie sémantique (`plugin.started`, `world.starting`, `gameplay.entered`, événements de modules et d'échec).

<a id="runtime-command-mailbox"></a>
## Boîte aux lettres des commandes runtime

`OmsiLaunch.Runtime.<session>` est une boîte aux lettres de 64 KiB à requête unique en vol : `state` (int32 à 0 : 0 inactif, 1 demandé, 2 répondu), `length` (int32 à 4), enveloppe à 8. Les enveloppes sont des enregistrements `RuntimeCommandWire` (magic `OLRC`, version 1, en-tête de 72 octets contenant le type, la longueur totale, le GUID de la session, l'identifiant de requête, la longueur de la charge utile et le SHA-256 de la charge utile JSON UTF-8). Le plugin interroge la boîte aux lettres depuis son timer du thread d'interface (50 ms une fois le monde chargé), exécute la commande sur ce thread et ne publie la réponse que si le slot contient toujours le même identifiant de requête ; une requête abandonnée par l'hôte sur timeout ne reçoit jamais de réponse. Une réponse trop volumineuse est remplacée par une erreur typée `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Les détails et les timeouts se trouvent dans [contrôle runtime](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Politique de recherche des DLL

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` et les assemblys de la CLI déclarent `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Les imports natifs (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) sont résolus uniquement depuis le propre répertoire de l'assembly (`plugins\`) ou le répertoire système de Windows ; la racine OMSI et `PATH` ne sont jamais sondés. `OmsiLaunch.Native.x86.dll` est donc chargé depuis `plugins\` et nulle part ailleurs.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Quand OMSI est démarré sans OmsiLaunch

Comme la closure est permanente, OMSI charge `OmsiLaunch.PluginNE.dll` à chaque démarrage, y compris les démarrages depuis Steam ou le bureau. Dans ce cas :

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` est absent, donc aucun correctif du téléchargeur n'est appliqué.
- `OMSILAUNCH_HANDOFF_NAME` est absent, donc `PluginRuntime.Start` émet `plugin.handoff.invalid` et renvoie `false`. `CurrentTelemetrySink.Emit` retourne immédiatement lorsque `OMSILAUNCH_TELEMETRY_NAME` n'est pas défini, de sorte que rien n'est écrit nulle part.
- Aucune validation du build, aucun hook natif, aucune boîte aux lettres, aucun timer. Le plugin reste chargé mais inerte ; OMSI se comporte comme si le plugin n'était pas là.
- `PluginFinalize` à la fermeture d'OMSI appelle la routine native de restauration et `NativeD3DShutdown`, qui ne font rien lorsque rien n'a été installé.

Il n'est donc pas nécessaire de supprimer le `.opl` pour exécuter OMSI normalement.

<a id="non-interference-with-third-party-plugins"></a>
## Absence d'interférence avec les plugins tiers

Les sessions n'énumèrent, ne hachent, ne copient, ne suppriment ni ne restaurent jamais d'autres fichiers dans `plugins\`. Le plugin n'utilise pas le canal `AccessVariable` d'OMSI et ne touche pas à l'état des autres plugins. Les seuls correctifs internes au processus sont le hook profilé de démarrage headless (une redirection de VMT à usage unique armée pour la session), la suppression facultative des textures Internet (restaurée dans `PluginFinalize`) et l'interception de `Reset` du périphérique D3D9 utilisée pour le suivi du cycle de vie des textures.

<a id="difference-from-omsihook"></a>
## Différence avec OmsiHook

OmsiLaunch n'a **aucune dépendance runtime** envers OmsiHook ni envers aucun binaire OmsiHook : les seuls paquets référencés sont `DNNE` 2.0.6 et `YamlDotNet` 15.1.2, et il n'y a aucun `using OmsiHook` ni aucun P/Invoke vers des DLL OmsiHook nulle part dans le produit. Ce qu'OmsiLaunch partage avec OmsiHook, ce sont des connaissances dérivées : les dispositions des objets et plusieurs wrappers de lecture ont été rapprochés à partir du checkout épinglé d'OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) par rapport à l'exécutable exact `Omsi23004_692EBFBF`. L'attribution et les conditions de licence figurent dans `THIRD-PARTY-NOTICES.md` et la matrice de réutilisation par fichier dans `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook injecte un processus séparé et expose des pointeurs bruts ; OmsiLaunch s'exécute dans le processus, n'expose que des handles opaques limités à la session et retire toute adresse native des résultats publics (voir [capacités](../reference/capabilities.md)).
