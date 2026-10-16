# Windows Tray Indicator

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> British English edition of the [canonical page](../../../reference/windows-tray.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

Every standalone owner session (started with `OmsiLaunch.exe` or `OmsiLaunchW.exe`) shows a notification-area icon that reports the session and lets the user end it. This page specifies the indicator as implemented by `SessionTrayIndicator`, `StatusWindow` and `StopConfirmationWindow` in `tools\OmsiLaunch.Cli\WindowsHost.cs`, the read-only presenter `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) and the string registry `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), together with the `OmsiLaunchW.exe` failure dialogues of `WindowsHost`. The tray is a presentation adapter only: it owns neither OMSI nor recovery, and its stop action signals the same canonical owner stop path as `session stop` (see [CLI reference](cli.md) and [local control](local-control.md)).

## When the icon exists

| Step | Behaviour |
|---|---|
| Creation | Immediately after `StartSessionAsync` returns, before the session reaches `Running`, unless `SuppressTrayIcon` is set. The icon therefore exists during `StartingProcess`, `WaitingForPlugin`, `StartingWorld` and `EnteringGameplay`. |
| Startup budget | The UI thread (`STA`, background, named `OmsiLaunch tray`) must publish the icon within 2 s. Otherwise, or on any exception during creation, the indicator is disposed and the session continues **without** an icon; `startup-timeout` or the exception is logged. A slow start never orphans a visible icon. |
| Removal | In the owner's `finally` block, after the session completed or failed and before `CloseAsync`. Disposal posts the shutdown to the UI thread (closes the menu, the confirmation and the status window, then ends the message loop), joins the thread for up to 2 s (`dispose-timeout` logged when exceeded), hides and disposes the `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (a `LaunchSpec` field, `Presentation.SuppressTrayIcon`, default `false`). Settable through `/spec` and the API; there is no CLI flag. Integrators that render their own session affordance set it to `true`; nothing else about the session changes. |
| Hosts | Both `OmsiLaunch.exe` (console) and `OmsiLaunchW.exe` (Windows subsystem) show the icon; `OmsiLaunchW.exe` sessions started with `/silent` have no other visible surface. |

## Icon and tooltip

- Icon: the icon associated with the running executable (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, the embedded OmsiLaunch icon), falling back to `SystemIcons.Application`.
- Tooltip text: `Tray.Running` (`OmsiLaunch is running`). The text does not change while stopping (there is no "stopping" string yet; the icon stays as is until the owner removes it).
- Visual styles are enabled (`Application.EnableVisualStyles`).

## Interaction

| Action | Result |
|---|---|
| Right-click | Makes the tray window the foreground window (required for notification-icon menus; without it the menu may ignore clicks and never close while OMSI is in front), then opens the context menu at the real cursor position (`Cursor.Position`, not the event coordinates, because `NotifyIcon` can report `(0,0)` for shell-hosted events). The menu is clamped into the working area of the screen under the cursor. |
| Double-click | Opens the status window (same as the `Status` menu item). |
| Left-click | No action. |
| Menu item `Status` (`Tray.Status`) | Opens (or activates, if already open) the read-only status window. |
| Separator | |
| Menu item `End session` (`Tray.EndSession`, accessible description `Tray.EndSessionDescription`) | Opens the confirmation dialogue. |

### Status window (read-only snapshot)

Opened by the `Status` menu item or a double-click on the icon (`SessionTrayIndicator.ShowStatus`). It is a fixed, centred, auto-sized dialogue with no taskbar entry and a single `Close` button (`Status.Close`; `Escape` also closes it). Choosing `Status` while the window is already open activates that window without rebuilding it (it keeps the snapshot of its first opening). A failure to build the window is written to `tray-host.log`; it does not affect the session.

**It is a snapshot, not a live view.** `SessionStatusPresenter.Create(plan, status, ui)` runs once when the window opens: it reads the session's resolved `SessionPlan` (the spec that was actually planned) and one `SessionStatus` (only its `State`). Nothing is refreshed while the window stays open, and it never queries OMSI (no runtime operation, no telemetry values). Close and reopen it to see a newer state.

Window title and heading: the same text, `Status.SessionRunning` (`Session is running`) when `SessionStatus.State` is `Running`, otherwise the raw `SessionState` name (for example `WaitingForPlugin` when opened during startup, since the icon exists before `Running`). `Status.Title` (`OmsiLaunch session status`) is defined in the string table but not used by this release.

Sections appear in this order, and a section is omitted when it has no fields. Every value comes from the planned `LaunchSpec`/`SessionPlan`, never from OMSI.

| Section (English label) | Field (English label) | Shown when | Value | Source (public equivalent) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | always | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (any other mode; never reached because `LastMapState` is not runnable) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | the plan resolved a `map` content identity | the map's `DisplayName` (the map directory name, for example `Grundorf`), else the identity's file name without extension | `SessionPlan.ResolvedContent` entry with `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` gives the same `DisplayName` |
| `Session` | `Situation` | `SavedSituation` with a situation identity | file name without extension (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | an entrypoint was requested | the entrypoint identity if set (never runnable in this release), else the presented index as an integer (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | a session profile was used (`/predefined-profile`) | profile `name` | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | as above, when the preset has a name | preset `name` | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | an explicit/system date, time or weather was requested | `DD/MM/YYYY` or `System`; `HH:MM:SS` or `System`; ICAO code, preset name or `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | a player-vehicle field was requested | the requested identity/value | `Spec.PlayerVehicle` |
| `Configuration` | one field per semantic setting | a setting was set (`/set`, profile `settings`, `LaunchSpec.Environment.*`) and is known to `ConfigurationCatalog` | the requested value; `%` appended for keys ending in `Percent`, ` m` for keys ending in `DistanceMeters`. The label is the setting key with each dot-separated part capitalised (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; the same values are the `PlannedMutations` of the plan |
| `Presentation` | `Splash` | always | `Managed` or `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | always | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

The `Environment` section appears in a running Beta 4 session when a date, time, year or weather was requested (those requests are experimental, see [known limitations](known-limitations.md)). The `Vehicle` section can never appear in a running session: requesting any player-vehicle field makes the plan not runnable, so no such session starts. It exists for a future build and is covered by the offline presenter test.

Runtime evidence (pt-BR UI, runtime closure `T01`): a NEW_MAP Grundorf session showed `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

The same data is available to tools without the tray: `session status` over the [local control plane](local-control.md) gives `SessionId`, `State`, `Diagnostics` and `RuntimeEvents` (live), and the owner's `/plan --json` output (or `PlanSessionAsync`) gives the planned spec, resolved content and planned mutations that the window summarises.

### End session (with confirmation)

1. `StopConfirmationWindow`: title `End session?`, message `OMSI 2 will be closed and the OmsiLaunch managed session will end.`, buttons `End session` (default, `DialogResult.OK`) and `Cancel` (`Escape`). A second request while the dialogue is open activates it instead of stacking another one.
2. On `OK` the tray calls `requestCanonicalStop`, which completes the owner's `controlStopped` signal; the owner then calls `StopAsync`: OMSI is terminated with `TerminateProcess` and every session-owned file is restored. The tray never terminates OMSI itself.
3. If the request throws, the error is logged and `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`) is shown.
4. The tray does not confirm success; the icon disappears when the owner finishes restoring and disposes the indicator (runtime closure `T01`: the owner ended 607 ms after `End session` was confirmed).
5. `Cancel` (or closing the dialogue) does nothing: the session keeps running (`T01`).
6. A stop that arrives from elsewhere (`session stop`, Ctrl+C, `/observe-seconds`, OMSI exiting) while the status window or the confirmation dialogue is open closes them as part of the indicator's disposal; the owner does not wait for the user (`T02`: owner ended 725 ms after the pipe stop with both windows open).

## Explorer restart

`TrayWindow` is a hidden native window that registers the `TaskbarCreated` window message. When Explorer (the shell) restarts, it broadcasts that message and the indicator re-adds the icon (`Visible = false; Visible = true`). Runtime closure `T01`: after `explorer.exe` was terminated and restarted by Windows, the icon was back in `Shell_TrayWnd` and the menu and status window kept working.

<a id="localization"></a>
## Localisation

`WindowsUiStrings.Resolve` follows the **Windows UI culture** (`CultureInfo.CurrentUICulture`), never the OMSI content language or a session-profile language. Resolution order: exact culture name, then two-letter language, then English. Every key falls back to English when a translation lacks it.

| Culture keys | Language |
|---|---|
| `en`, `en-US`, `en-GB` | English (default and fallback) |
| `pt-BR` | Brazilian Portuguese. `pt-PT` (and bare `pt`) deliberately falls back to English. |
| `de`, `de-DE` | German |
| `fr`, `fr-FR` | French |
| `pl`, `pl-PL` | Polish |

Localised strings cover the tooltip, the two menu items, the status window (heading, section titles, field labels, `Close`), the mode and presentation values, and the confirmation dialogue. The glossary is maintained in `docs\windows-ui-localization.md`; offline test `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifies resolution and the presenter.

<a id="omsilaunchwexe-failure-dialogs"></a>
## `OmsiLaunchW.exe` failure dialogues

When `OMSILAUNCH_WINDOWS_HOST=1` (set by `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` replaces console error output with a modal message box titled `OmsiLaunch` (error icon): `<message>`, blank line, `Code: OL_E_...`, blank line, `See .omsilaunch\diagnostics for details.` It is shown by every `CliInput.WriteError` (argument errors, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, classified exceptions) when a launch plan is not runnable (`The session plan is not runnable.` fallback; documentation audit BUG-06) and when the session fails to reach `Running` (`The OMSI session did not reach gameplay.` with the last `OL_E_` diagnostic, or `OL_E_SESSION_START_FAILED` when there is none). The complete behaviour of `OmsiLaunchW.exe` is in the [OmsiLaunchW.exe reference](omsilaunchw.md). Under `OmsiLaunch.exe` the same function does nothing. A .NET host start failure (shim codes `100`..`106`) is shown by the native shim itself; see [exit codes](exit-codes.md).

## Log location

`<root>\.omsilaunch\diagnostics\tray-host.log`, one line per entry: ISO-8601 UTC timestamp, a tab, then the entry. Entries: `created`, `removed`, `startup-timeout`, `startup-cancelled` (a dispose raced startup and the loop was skipped), `dispose-timeout`, and full exception texts for UI failures. Logging is best effort and never throws. Session host logs (`<sessionId>-host.log`) are written in the same directory by the owner; the tray log is not session-prefixed and is not pruned by the 50-session retention.

## Lifecycle guarantees

- The tray never owns the session: it cannot start OMSI, cannot restore files and cannot bypass the owner's stop path.
- Every owner exit path (normal completion, OMSI exit, Ctrl+C, console close, pipe stop, exception, `/observe-seconds`) disposes the indicator before `CloseAsync`, so no icon outlives its session except when the owner process is killed outright (Windows removes orphaned icons on the next mouse hover).
- Creation and disposal are serialised under a lock: a disposal that wins the race makes the UI thread skip its message loop and clean up immediately.
- All Windows Forms work happens on the dedicated STA thread; foreign threads only post to it through a hidden marshalling control.

## Residual caveats (from the code comments)

- No "stopping" tooltip text exists; the icon reads `OmsiLaunch is running` until it is removed.
- `NotifyIcon` may report `(0,0)` mouse coordinates for shell-hosted events; the cursor position is read instead.
- Tray messages still arrive while the confirmation dialogue is modal; a second confirmation is not stacked.
- If the UI thread does not finish within the 2 s dispose budget the owner continues without waiting (`dispose-timeout`).
- The status window is a snapshot of planned values taken when it opens; it is not refreshed and never reads OMSI.

## Runtime evidence

Observed in real sessions on the authorised installation in the runtime closure round (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, pt-BR Windows UI; see the [runtime validation status](../status/runtime-validation-status.md)):

- The icon is registered in the real notification area (`Shell_TrayWnd`) under `OmsiLaunchW.exe` and removed after restore (`T01`..`T04`).
- "End session" with confirmation drives the canonical stop and exact restore; `Cancel` keeps the session running (`T01`, `T03`).
- The icon is re-created after an Explorer restart (`TaskbarCreated`, `T01`).
- The status and confirmation windows are closed by the owner when a stop arrives while they are open (`T02`).
- `OmsiLaunchW.exe` failure dialogues for an argument error (`OL_E_INVALID_ARGUMENT`), no active session (`OL_E_NO_ACTIVE_SESSION`) and a session that fails before gameplay (`OL_E_WORLD_START_FAILED`) (`T04`). For the last one the dialogue shows the plugin's failure payload as its message.
- `/silent` detaches: the launcher returns while the Windows host keeps the session (`T04`).
- The 4 s `ProcessExit` budget on console close (`L04`, console owner).

Not produced: the bootstrapper shim exit-code dialogues (`100`..`106`).
