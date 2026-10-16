# First Session

This page walks through the first managed OMSI session with OmsiLaunch `0.1.0-beta4`: planning without starting OMSI, starting with explicit flags, starting with a predefined session profile, controlling and stopping the session, and finding the diagnostics afterwards. It assumes the package is installed as described in [installation](installation.md). Every flag is specified in the [CLI reference](../reference/cli.md); more invocations are in [CLI examples](../reference/cli-examples.md).

## What a session does

A session is a transaction around one OMSI process: OmsiLaunch snapshots the files it will touch (by default the two splash bitmaps under `GUI\`, plus `options.cfg` when `/set` overlays are requested), writes a durable journal under `.omsilaunch\`, applies the overlays, starts `Omsi.exe` with the permanent plugin, waits until gameplay is entered (`Running`), keeps the session controllable, and at the end terminates OMSI and restores every touched file byte-for-byte. `/new` never selects a map or an entrypoint silently: both must be given, or come from a `/spec` file or a session profile.

## 1. Plan (nothing is started)

Run from the OMSI root; the installation defaults to the directory containing `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

The plan must say `"IsRunnable": true` (exit `0`). It lists `TouchedFiles` and `PlannedMutations` so you can see exactly what the session will overlay. Fix any `OL_E_` diagnostic before continuing; nothing has been written.

The packaged example spec does the same with a file:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

## 2. Start with explicit flags

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

What happens, in order:

1. The plan is printed (`Plan: READY profile=Omsi23004` on the known layout).
2. Recovery of any older pending journal, lease acquisition, snapshot, journal, overlays, plugin integrity check, `Omsi.exe` start.
3. The tray icon appears (`OmsiLaunch is running`); see [Windows tray](../reference/windows-tray.md).
4. When gameplay is entered, the `Running` status is printed as JSON (`"State": 14`). The default startup timeout is 180 s (`/startup-timeout:<1..600>` to change it).
5. The console stays attached until the session ends. Do not close the console window to stop: use one of the stop methods below.

Optional additions for the first run:

- `/set:graphics.maxFPS=60` (an `options.cfg` overlay, restored at the end);
- `/splash:Unset` to leave the OMSI splash screen untouched, or `/splash-language:DEU` to pick the localized managed splash;
- `/observe-seconds:30` to stop automatically 30 s after `Running` (useful for a smoke test);
- `--json` for structured output.

Flags that request a date, time, year, weather or a player vehicle (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) are accepted but cannot be applied by this build: the plan becomes `NOT RUNNABLE` with `OL_E_CAPABILITY_UNAVAILABLE`. Leave them out.

## 3. Start with a predefined session profile

A session profile is a YAML file under `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` that fixes the map, the entrypoint and up to 99 presets of settings (schema `omsilaunch.session-profile/v1`; full reference in [session profiles](../reference/session-profiles.md)). Create `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

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

Then:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Rules to remember: the `id` must equal the directory name; the index is `1..99`; explicit flags that would override a profile-owned field (`/map`, `/entrypoint-index`, a `/set` key the preset owns, splash flags when the preset has `presentation`) are rejected with `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (exit `2`); the `new:` block applies only with `/new`; with `/saved:<file.osn>` the situation's map must be listed under `compatibility.maps`. The packaged `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` illustrates the complete schema, but its `new:` block sets `date`, `time` and `weather`, which this build cannot apply, so copy it only after removing those keys.

## 4. Control the running session

From a second console in the same directory (no installation argument):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

These go through the local control pipe of this installation ([local control](../reference/local-control.md)); exit `4` means no owner is running here.

## 5. Stop

Any of these ends the session the same way (OMSI is terminated, then every touched file is restored, then the journal and backup are deleted):

| Method | Notes |
|---|---|
| Tray icon → `End session` → confirm | Available in both `OmsiLaunch.exe` and `OmsiLaunchW.exe` sessions. |
| `OmsiLaunch.exe session stop` | From another console; returns immediately, the owner finishes the restore. |
| Ctrl+C in the owner console | Requests the stop; the owner waits for the restore before exiting. |
| `/observe-seconds:<n>` | Automatic stop `n` seconds after `Running`. |
| OMSI exits by itself | The owner detects `ProcessExited` and restores. |

OMSI's own shutdown routine does not run, so OMSI does not rewrite `options.cfg` on exit; that is intentional so that the restore is exact. Closing the owner console window with the X button gives the restore only 4 s; if it did not finish, the next start (or `OmsiLaunch.exe /recover`) completes it from the journal. The owner's exit code is `0` when the session ended in `Completed`.

## 6. Where to look afterwards

| Location | Content |
|---|---|
| Console / `--json` output | Plan, `Running` status, final status (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | The host trace of the session (transaction boundaries, process start, plugin handoff, gameplay entered, restore). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Result of a `/runtime:` operation run by the owner. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Tray indicator events. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` after a clean end. `true` means a journal is left; run `OmsiLaunch.exe /recover`. |

If the session did not reach gameplay, the final status carries the failing `OL_E_` diagnostic (for example `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), the exit code is `1`, and the files were restored anyway. See [exit codes](../reference/exit-codes.md), [errors](../reference/errors.md) and [known limitations](../reference/known-limitations.md).

## Running without a console

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delegates to `OmsiLaunchW.exe` and returns `0` immediately. The session has no console; failures appear as message boxes and the tray icon is the only visible surface. Use `session status`, `events watch` and the diagnostics directory to follow it. The complete behaviour of the Windows host is in the [OmsiLaunchW.exe reference](../reference/omsilaunchw.md).
