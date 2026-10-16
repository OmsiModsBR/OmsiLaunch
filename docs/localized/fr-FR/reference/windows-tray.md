# Indicateur de la zone de notification Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Traduction de la [page originale en anglais](../../../reference/windows-tray.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Chaque session propriétaire autonome (démarrée avec `OmsiLaunch.exe` ou `OmsiLaunchW.exe`) affiche une icône dans la zone de notification qui présente la session et permet à l'utilisateur d'y mettre fin. Cette page spécifie l'indicateur tel qu'il est implémenté par `SessionTrayIndicator`, `StatusWindow` et `StopConfirmationWindow` dans `tools\OmsiLaunch.Cli\WindowsHost.cs`, par le présentateur en lecture seule `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) et par le registre de chaînes `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), ainsi que les boîtes de dialogue d'échec de `OmsiLaunchW.exe` de `WindowsHost`. La zone de notification n'est qu'un adaptateur de présentation : elle ne détient ni OMSI ni la récupération, et son action d'arrêt déclenche le même chemin d'arrêt canonique du propriétaire que `session stop` (voir la [référence de la CLI](cli.md) et le [contrôle local](local-control.md)).

<a id="when-the-icon-exists"></a>
## Quand l'icône existe

| Étape | Comportement |
|---|---|
| Création | Immédiatement après le retour de `StartSessionAsync`, avant que la session n'atteigne `Running`, sauf si `SuppressTrayIcon` est défini. L'icône existe donc pendant `StartingProcess`, `WaitingForPlugin`, `StartingWorld` et `EnteringGameplay`. |
| Budget de démarrage | Le thread d'interface (`STA`, en arrière-plan, nommé `OmsiLaunch tray`) doit publier l'icône en moins de 2 s. Sinon, ou en cas d'exception pendant la création, l'indicateur est libéré et la session continue **sans** icône ; `startup-timeout` ou l'exception est journalisé. Un démarrage lent ne laisse jamais une icône visible orpheline. |
| Suppression | Dans le bloc `finally` du propriétaire, après que la session s'est terminée ou a échoué et avant `CloseAsync`. La libération poste l'arrêt au thread d'interface (fermeture du menu, de la confirmation et de la fenêtre d'état, puis fin de la boucle de messages), attend la fin du thread pendant au plus 2 s (`dispose-timeout` journalisé en cas de dépassement), masque et libère le `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (un champ de `LaunchSpec`, `Presentation.SuppressTrayIcon`, par défaut `false`). Définissable via `/spec` et l'API ; il n'existe pas d'option CLI. Les intégrateurs qui affichent leur propre élément d'interface de session le définissent à `true` ; rien d'autre ne change dans la session. |
| Hôtes | `OmsiLaunch.exe` (console) et `OmsiLaunchW.exe` (sous-système Windows) affichent tous deux l'icône ; les sessions `OmsiLaunchW.exe` démarrées avec `/silent` n'ont aucune autre surface visible. |

<a id="icon-and-tooltip"></a>
## Icône et info-bulle

- Icône : l'icône associée à l'exécutable en cours (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, l'icône OmsiLaunch intégrée), avec repli sur `SystemIcons.Application`.
- Texte de l'info-bulle : `Tray.Running` (`OmsiLaunch is running`) (`OmsiLaunch est en cours d'exécution`). Le texte ne change pas pendant l'arrêt (il n'existe pas encore de chaîne « stopping » ; l'icône reste telle quelle jusqu'à ce que le propriétaire la supprime).
- Les styles visuels sont activés (`Application.EnableVisualStyles`).

## Interaction

| Action | Résultat |
|---|---|
| Clic droit | Place la fenêtre de la zone de notification au premier plan (nécessaire pour les menus d'icônes de notification ; sans cela, le menu peut ignorer les clics et ne jamais se fermer lorsque OMSI est au premier plan), puis ouvre le menu contextuel à la position réelle du curseur (`Cursor.Position`, et non les coordonnées de l'événement, car `NotifyIcon` peut signaler `(0,0)` pour les événements hébergés par le shell). Le menu est contraint à la zone de travail de l'écran situé sous le curseur. |
| Double-clic | Ouvre la fenêtre d'état (comme l'élément de menu `Status`). |
| Clic gauche | Aucune action. |
| Élément de menu `Status` (`Tray.Status`) | Ouvre (ou active, si elle est déjà ouverte) la fenêtre d'état en lecture seule. |
| Séparateur | |
| Élément de menu `End session` (`Tray.EndSession`, description accessible `Tray.EndSessionDescription`) | Ouvre la boîte de dialogue de confirmation. |

<a id="status-window-read-only-snapshot"></a>
### Fenêtre d'état (instantané en lecture seule)

Ouverte par l'élément de menu `Status` (`État`) ou par un double-clic sur l'icône (`SessionTrayIndicator.ShowStatus`). Il s'agit d'une boîte de dialogue fixe, centrée, dimensionnée automatiquement, sans entrée dans la barre des tâches et avec un unique bouton `Close` (`Status.Close` ; `Escape` la ferme également). Choisir `Status` alors que la fenêtre est déjà ouverte active cette fenêtre sans la reconstruire (elle conserve l'instantané de sa première ouverture). Un échec de construction de la fenêtre est écrit dans `tray-host.log` ; il n'affecte pas la session.

**Il s'agit d'un instantané, pas d'une vue en direct.** `SessionStatusPresenter.Create(plan, status, ui)` s'exécute une seule fois à l'ouverture de la fenêtre : il lit le `SessionPlan` résolu de la session (la spec réellement planifiée) et un `SessionStatus` (uniquement son `State`). Rien n'est actualisé tant que la fenêtre reste ouverte, et elle n'interroge jamais OMSI (aucune opération runtime, aucune valeur de télémétrie). Fermez-la puis rouvrez-la pour voir un état plus récent.

Titre et en-tête de la fenêtre : le même texte, `Status.SessionRunning` (`Session is running`) (`La session est en cours`) lorsque `SessionStatus.State` vaut `Running`, sinon le nom brut du `SessionState` (par exemple `WaitingForPlugin` si la fenêtre est ouverte pendant le démarrage, puisque l'icône existe avant `Running`). `Status.Title` (`OmsiLaunch session status`) est défini dans la table des chaînes mais n'est pas utilisé par cette version.

Les sections apparaissent dans cet ordre, et une section est omise lorsqu'elle n'a aucun champ. Chaque valeur provient du `LaunchSpec`/`SessionPlan` planifié, jamais d'OMSI.

| Section (libellé anglais) | Champ (libellé anglais) | Affiché lorsque | Valeur | Source (équivalent public) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | toujours | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (tout autre mode ; jamais atteint, car `LastMapState` n'est pas exécutable) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | le plan a résolu une identité de contenu `map` | le `DisplayName` de la carte (le nom du répertoire de la carte, par exemple `Grundorf`), sinon le nom de fichier de l'identité sans extension | Entrée de `SessionPlan.ResolvedContent` avec `Kind = "map"` (NEW_MAP) ; `DiscoverAsync(Maps)` donne le même `DisplayName` |
| `Session` | `Situation` | `SavedSituation` avec une identité de situation | nom de fichier sans extension (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | un point d'entrée a été demandé | l'identité du point d'entrée si elle est définie (jamais exécutable dans cette version), sinon l'index présenté sous forme d'entier (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | un profil de session a été utilisé (`/predefined-profile`) | `name` du profil | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | comme ci-dessus, lorsque le préréglage a un nom | `name` du préréglage | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | une date, une heure ou une météo explicite/système a été demandée | `DD/MM/YYYY` ou `System` ; `HH:MM:SS` ou `System` ; code OACI, nom de préréglage ou `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | un champ du véhicule du joueur a été demandé | l'identité/la valeur demandée | `Spec.PlayerVehicle` |
| `Configuration` | un champ par paramètre sémantique | un paramètre a été défini (`/set`, `settings` du profil, `LaunchSpec.Environment.*`) et est connu de `ConfigurationCatalog` | la valeur demandée ; `%` ajouté pour les clés se terminant par `Percent`, ` m` pour les clés se terminant par `DistanceMeters`. Le libellé est la clé du paramètre dont chaque partie séparée par un point commence par une majuscule (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*` ; les mêmes valeurs sont les `PlannedMutations` du plan |
| `Presentation` | `Splash` | toujours | `Managed` ou `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | toujours | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

La section `Environment` apparaît dans une session Beta 4 en cours d'exécution lorsqu'une date, une heure, une année ou une météo a été demandée (ces demandes sont expérimentales, voir les [limitations connues](known-limitations.md)). La section `Vehicle` ne peut jamais apparaître dans une session en cours d'exécution : demander un champ quelconque du véhicule du joueur rend le plan non exécutable, de sorte qu'aucune session de ce type ne démarre. Elle existe pour un futur build et est couverte par le test hors ligne du présentateur.

Preuves d'exécution (interface pt-BR, closure runtime `T01`) : une session NEW_MAP sur Grundorf a affiché `Sessão em execução` ; `Sessão` : `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1` ; `Apresentação` : `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Les mêmes données sont accessibles aux outils sans passer par la zone de notification : `session status` via le [plan de contrôle local](local-control.md) fournit `SessionId`, `State`, `Diagnostics` et `RuntimeEvents` (en direct), et la sortie `/plan --json` du propriétaire (ou `PlanSessionAsync`) fournit la spec planifiée, le contenu résolu et les mutations planifiées que la fenêtre résume.

<a id="end-session-with-confirmation"></a>
### Terminer la session (avec confirmation)

1. `StopConfirmationWindow` : titre `End session?` (`Terminer la session ?`), message `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (`OMSI 2 sera fermé et la session gérée par OmsiLaunch prendra fin.`), boutons `End session` (par défaut, `DialogResult.OK`) et `Cancel` (`Escape`). Une seconde demande alors que la boîte de dialogue est ouverte active celle-ci au lieu d'en empiler une autre.
2. Sur `OK`, la zone de notification appelle `requestCanonicalStop`, qui complète le signal `controlStopped` du propriétaire ; le propriétaire appelle alors `StopAsync` : OMSI est arrêté avec `TerminateProcess` et chaque fichier détenu par la session est restauré. La zone de notification n'arrête jamais OMSI elle-même.
3. Si la demande lève une exception, l'erreur est journalisée et `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`) est affiché.
4. La zone de notification ne confirme pas la réussite ; l'icône disparaît lorsque le propriétaire termine la restauration et libère l'indicateur (closure runtime `T01` : le propriétaire s'est terminé 607 ms après la confirmation de `End session`).
5. `Cancel` (ou la fermeture de la boîte de dialogue) ne fait rien : la session continue de s'exécuter (`T01`).
6. Un arrêt provenant d'ailleurs (`session stop`, Ctrl+C, `/observe-seconds`, fin d'OMSI) alors que la fenêtre d'état ou la boîte de dialogue de confirmation est ouverte les ferme dans le cadre de la libération de l'indicateur ; le propriétaire n'attend pas l'utilisateur (`T02` : le propriétaire s'est terminé 725 ms après l'arrêt via le pipe, les deux fenêtres étant ouvertes).

<a id="explorer-restart"></a>
## Redémarrage de l'Explorateur

`TrayWindow` est une fenêtre native masquée qui enregistre le message de fenêtre `TaskbarCreated`. Lorsque l'Explorateur (le shell) redémarre, il diffuse ce message et l'indicateur ajoute de nouveau l'icône (`Visible = false; Visible = true`). Closure runtime `T01` : après que `explorer.exe` a été arrêté puis redémarré par Windows, l'icône était de retour dans `Shell_TrayWnd`, et le menu ainsi que la fenêtre d'état continuaient de fonctionner.

<a id="localization"></a>
## Localisation

`WindowsUiStrings.Resolve` suit la **culture de l'interface Windows** (`CultureInfo.CurrentUICulture`), jamais la langue du contenu OMSI ni la langue d'un profil de session. Ordre de résolution : nom exact de la culture, puis langue à deux lettres, puis anglais. Chaque clé se rabat sur l'anglais lorsqu'une traduction ne la contient pas.

| Clés de culture | Langue |
|---|---|
| `en`, `en-US`, `en-GB` | Anglais (par défaut et de repli) |
| `pt-BR` | Portugais du Brésil. `pt-PT` (et `pt` seul) se rabat délibérément sur l'anglais. |
| `de`, `de-DE` | Allemand |
| `fr`, `fr-FR` | Français |
| `pl`, `pl-PL` | Polonais |

Les chaînes localisées couvrent l'info-bulle, les deux éléments de menu, la fenêtre d'état (en-tête, titres de section, libellés de champ, `Close`), les valeurs de mode et de présentation, ainsi que la boîte de dialogue de confirmation. Le glossaire est tenu à jour dans `docs\windows-ui-localization.md` ; le test hors ligne `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) vérifie la résolution et le présentateur.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Boîtes de dialogue d'échec de `OmsiLaunchW.exe`

Lorsque `OMSILAUNCH_WINDOWS_HOST=1` (défini par `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` remplace la sortie d'erreur de la console par une boîte de message modale intitulée `OmsiLaunch` (icône d'erreur) : `<message>`, ligne vide, `Code: OL_E_...`, ligne vide, `See .omsilaunch\diagnostics for details.` Elle est affichée par chaque `CliInput.WriteError` (erreurs d'argument, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, exceptions classifiées), lorsqu'un plan de lancement n'est pas exécutable (repli `The session plan is not runnable.` ; audit de documentation BUG-06) et lorsque la session n'atteint pas `Running` (`The OMSI session did not reach gameplay.` avec le dernier diagnostic `OL_E_`, ou `OL_E_SESSION_START_FAILED` s'il n'y en a aucun). Le comportement complet de `OmsiLaunchW.exe` est décrit dans la [référence d'OmsiLaunchW.exe](omsilaunchw.md). Sous `OmsiLaunch.exe`, la même fonction ne fait rien. Un échec de démarrage de l'hôte .NET (codes du shim `100`..`106`) est affiché par le shim natif lui-même ; voir [codes de sortie](exit-codes.md).

<a id="log-location"></a>
## Emplacement du journal

`<root>\.omsilaunch\diagnostics\tray-host.log`, une ligne par entrée : horodatage UTC ISO-8601, une tabulation, puis l'entrée. Entrées : `created`, `removed`, `startup-timeout`, `startup-cancelled` (une libération est entrée en concurrence avec le démarrage et la boucle a été ignorée), `dispose-timeout`, ainsi que les textes complets des exceptions pour les échecs de l'interface. La journalisation est faite au mieux et ne lève jamais d'exception. Les journaux d'hôte de session (`<sessionId>-host.log`) sont écrits dans le même répertoire par le propriétaire ; le journal de la zone de notification n'est pas préfixé par la session et n'est pas élagué par la rétention de 50 sessions.

<a id="lifecycle-guarantees"></a>
## Garanties du cycle de vie

- La zone de notification ne détient jamais la session : elle ne peut pas démarrer OMSI, ne peut pas restaurer de fichiers et ne peut pas contourner le chemin d'arrêt du propriétaire.
- Chaque chemin de sortie du propriétaire (fin normale, fin d'OMSI, Ctrl+C, fermeture de la console, arrêt via le pipe, exception, `/observe-seconds`) libère l'indicateur avant `CloseAsync`, de sorte qu'aucune icône ne survit à sa session, sauf lorsque le processus propriétaire est tué brutalement (Windows supprime les icônes orphelines au prochain survol de la souris).
- La création et la libération sont sérialisées sous un verrou : une libération qui gagne la course fait que le thread d'interface ignore sa boucle de messages et nettoie immédiatement.
- Tout le travail Windows Forms s'effectue sur le thread STA dédié ; les threads étrangers ne font qu'y poster des opérations via un contrôle de marshalling masqué.

<a id="residual-caveats-from-the-code-comments"></a>
## Réserves résiduelles (issues des commentaires du code)

- Aucun texte d'info-bulle « stopping » n'existe ; l'icône affiche `OmsiLaunch is running` jusqu'à sa suppression.
- `NotifyIcon` peut signaler des coordonnées de souris `(0,0)` pour les événements hébergés par le shell ; la position du curseur est lue à la place.
- Les messages de la zone de notification continuent d'arriver pendant que la boîte de dialogue de confirmation est modale ; une seconde confirmation n'est pas empilée.
- Si le thread d'interface ne se termine pas dans le budget de libération de 2 s, le propriétaire continue sans attendre (`dispose-timeout`).
- La fenêtre d'état est un instantané des valeurs planifiées pris à son ouverture ; elle n'est pas actualisée et ne lit jamais OMSI.

<a id="runtime-evidence"></a>
## Preuves d'exécution

Observées dans des sessions réelles sur l'installation autorisée lors du cycle de closure runtime (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, interface Windows pt-BR ; voir l'[état de la validation à l'exécution](../status/runtime-validation-status.md)) :

- L'icône est enregistrée dans la véritable zone de notification (`Shell_TrayWnd`) sous `OmsiLaunchW.exe` et supprimée après la restauration (`T01`..`T04`).
- « End session » (`Terminer la session`) avec confirmation déclenche l'arrêt canonique et la restauration exacte ; `Cancel` maintient la session en cours d'exécution (`T01`, `T03`).
- L'icône est recréée après un redémarrage de l'Explorateur (`TaskbarCreated`, `T01`).
- Les fenêtres d'état et de confirmation sont fermées par le propriétaire lorsqu'un arrêt survient pendant qu'elles sont ouvertes (`T02`).
- Boîtes de dialogue d'échec de `OmsiLaunchW.exe` pour une erreur d'argument (`OL_E_INVALID_ARGUMENT`), l'absence de session active (`OL_E_NO_ACTIVE_SESSION`) et une session qui échoue avant d'atteindre le jeu (`OL_E_WORLD_START_FAILED`) (`T04`). Pour cette dernière, la boîte de dialogue affiche comme message la charge utile d'échec du plugin.
- `/silent` se détache : le lanceur rend la main tandis que l'hôte Windows conserve la session (`T04`).
- Le budget `ProcessExit` de 4 s lors de la fermeture de la console (`L04`, propriétaire console).

Non produit : les boîtes de dialogue de code de sortie du shim d'amorçage (`100`..`106`).
