# OmsiLaunchW.exe Reference

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> British English edition of the [canonical page](../../../reference/omsilaunchw.md) for OmsiLaunch 0.1.0-beta3. The US English page is normative: where the two differ, it and the code take precedence.

`OmsiLaunchW.exe` is the Windows-subsystem (GUI) host of the OmsiLaunch controller. It accepts the same command line as `OmsiLaunch.exe` and runs the same controller code (`OmsiLaunch.Controller.dll`). The only difference is how it reports: there is no console window, failures are shown as message boxes, and a running session is visible only through its [tray icon](windows-tray.md).

Authoritative sources: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (the native shim), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) and `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

## OmsiLaunch.exe and OmsiLaunchW.exe compared

| Aspect | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsystem | Console. Opens a console window when started from Explorer. | Windows (GUI). No console window. |
| Native shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Environment | unchanged | sets `OMSILAUNCH_WINDOWS_HOST=1` for the controller process before .NET starts |
| Arguments | tokenised with `CommandLineToArgvW` and passed unchanged to the controller | the same, so both hosts accept exactly the same flags and commands ([CLI reference](cli.md)) |
| Text output | written to stdout | suppressed, unless `--json` is given (then the JSON envelopes are written to stdout, which a caller can redirect) |
| Errors | `OL_E_...: message` or a JSON error envelope | the same console output rules, **plus** a message box for every error (see [Failure dialogues](#failure-dialogs)) |
| `/silent` | starts `OmsiLaunchW.exe` with the other arguments and returns `0` | ignored: the command already runs in the Windows host |
| Tray icon | shown for an owner session | shown for an owner session |
| Stop paths | tray, `session stop`, OMSI exit, `/observe-seconds`, Ctrl+C, closing the console | tray, `session stop`, OMSI exit, `/observe-seconds` (there is no console, so Ctrl+C and console close do not apply) |
| Exit codes | [`PublicExitCode`](exit-codes.md) `0`..`10`, shim codes `100`..`106` | the same codes |

## How it starts

1. The shim resolves its own path (`GetModuleFileNameW`) and expects `OmsiLaunch.Controller.dll` in the same directory.
2. It tokenises the command line (`CommandLineToArgvW`) and sets `OMSILAUNCH_WINDOWS_HOST=1`.
3. It locates `hostfxr` through the packaged `nethost.dll`, loads it, initialises the controller with the arguments (the controller path is not part of the argument list the CLI parser sees) and runs it.
4. The shim returns the controller's exit code unchanged.

If any step before the controller runs fails, the shim shows a message box titled `OmsiLaunch` with the text `OmsiLaunch could not start the .NET host (code N).` and exits with that code:

| Code | Failed step |
| --- | --- |
| `100` | the executable path could not be resolved |
| `101` | the command line could not be tokenised |
| `102` | the `hostfxr` location probe failed (usually: the .NET 6 x64 runtime is not installed) |
| `103` | the `hostfxr` path could not be retrieved |
| `104` | `hostfxr` could not be loaded |
| `105` | required `hostfxr` exports are missing |
| `106` | the managed host could not be initialised (for example `OmsiLaunch.Controller.dll` or its runtime configuration is missing, or the Windows Desktop runtime is absent) |

`OmsiLaunch.exe` uses the same table but prints nothing. These dialogues have not been produced at runtime (see [runtime validation status](../status/runtime-validation-status.md)).

## Starting it

Direct start, from a shortcut, a script or another program:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Through `OmsiLaunch.exe` with `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Place the executables in the OMSI 2 installation (the package layout, see [packaging](packaging.md)); with no installation argument, the installation is the directory that contains the executable. An explicit installation is passed as the first argument, as with `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Because it is a GUI program, `cmd.exe` and Explorer do not wait for it. To wait and read the exit code from a script, use `start /wait OmsiLaunchW.exe ...` in `cmd.exe` or `Start-Process -Wait -PassThru` in PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

## /silent delegation

`OmsiLaunch.exe ... /silent` (or `--silent`), when not already running under `OmsiLaunchW.exe`, does the following (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. It looks for `OmsiLaunchW.exe` in the directory of `OmsiLaunch.exe`. If it is missing: `OL_E_WINDOWS_HOST_MISSING`, exit `7`.
2. It starts `OmsiLaunchW.exe` through `ShellExecute` (`UseShellExecute = true`) with the current directory and every argument except `/silent`/`--silent`, in the original order. `ShellExecute` does not pass the caller's handles to the new process, so a caller that captures the output of `OmsiLaunch.exe /silent` is not blocked for the life of the session (runtime closure BUG-03). If no process is returned: `OL_E_WINDOWS_HOST_START_FAILED`, exit `7`.
3. It writes the `silent` envelope and exits `0` immediately:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

Exit `0` means only that `OmsiLaunchW.exe` was started. The session outcome (planning errors, an owner already active, a failed start) is reported by `OmsiLaunchW.exe` with its own dialogues and diagnostics, and the two processes are independent afterwards: `OmsiLaunch.exe` has ended, and `OmsiLaunchW.exe` is the session owner. Use `OmsiLaunch.exe session status` to see the session.

`/silent` is applied before every other command, so `OmsiLaunch.exe /silent session status` also runs `session status` inside `OmsiLaunchW.exe`, where its output is suppressed. Use `/silent` only for launches.

## What each command does under OmsiLaunchW.exe

| Command line | Result |
| --- | --- |
| no arguments | runs `detect` silently and exits `0` (nothing is shown) |
| a launch (`/new`, `/saved:...`, `/spec:...`, a session profile) with a runnable plan and no owner | becomes the session owner: tray icon while starting and running; exits when the session ends (`0` completed, `1` failed) |
| a launch whose plan is not runnable | dialogue with the plan's last `OL_E_` diagnostic (fallback `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), exit `1` (documentation audit BUG-06; before the fix it exited silently) |
| a launch while an owner is already active for the installation | `OL_E_SESSION_ALREADY_ACTIVE` dialogue, exit `7`; the running session is not affected |
| a launch that does not reach `Running` | dialogue `The OMSI session did not reach gameplay.` with the last `OL_E_` diagnostic, exit `1`. OMSI is terminated and the files are restored by the session supervisor while the dialogue is open; the process exits after the dialogue is closed and `CloseAsync` has finished |
| an invalid argument, unknown flag or invalid session profile | dialogue with `OL_E_INVALID_ARGUMENT` or the `OL_E_SESSION_PROFILE_*` code, exit `2` |
| a client command (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) with no owner | `OL_E_NO_ACTIVE_SESSION` dialogue, exit `4` |
| a client command that the owner rejects | dialogue with the rejection code (for example `OL_E_CONTROL_SESSION_MISMATCH` or a runtime error code), exit `7` (`2` for an unknown operation or a missing argument) |
| a successful client or discovery command (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | no visible output unless `--json` is given and stdout is redirected; exit code as for `OmsiLaunch.exe` |
| `/plan` or `/validate` | no dialogue, even for a plan that is not runnable; exit `0` or `1` |
| any other controller failure | dialogue with the classified `OL_E_` code; exit code per [exit codes](exit-codes.md) |

<a id="failure-dialogs"></a>
## Failure dialogues

Every error that `OmsiLaunch.exe` would print is also shown as a modal message box (`WindowsHost.ShowFailure`), even when `--json` is given:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` is the error message, or for a session failure the message of the last `OL_E_` diagnostic. Known limitation: for a failure reported by the plugin, the message is the plugin's raw failure payload (for example `{"name":"world.failed",...}`); the `Code:` line is correct (see [known limitations](known-limitations.md)). The dialogue is modal and the process exits after it is closed. Runtime evidence: argument error, no active session and a failure before gameplay (runtime closure `T04`).

## Session, tray and exit

A session owned by `OmsiLaunchW.exe` behaves exactly like one owned by `OmsiLaunch.exe` (see [session lifecycle](../concepts/session-lifecycle.md)):

- The tray icon appears as soon as the session has started, before OMSI reaches gameplay, unless `Presentation.SuppressTrayIcon` is set in a `/spec` file. With `SuppressTrayIcon` there is no visible surface at all; stop the session with `OmsiLaunch.exe session stop` or by closing OMSI.
- The session ends when OMSI exits, when `End session` is confirmed in the tray, when a client sends `session stop`, or when `/observe-seconds` elapses. OmsiLaunch then terminates OMSI if it is still running, restores every file it changed, releases the installation lease, removes the tray icon and exits.
- If `OmsiLaunchW.exe` itself is killed, the next OmsiLaunch start for that installation recovers the pending transaction (see [transactions and recovery](../concepts/transactions-and-recovery.md)).

## Quick start

1. Install the package into the OMSI 2 directory ([installation](../getting-started/installation.md)).
2. Create a shortcut to `OmsiLaunchW.exe` with the arguments of the session, for example `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Start it. OMSI starts without a console window; the OmsiLaunch icon appears in the notification area.
4. Right-click the icon → `Status` to see the session, or `End session` → `End session` to end it.
5. If something goes wrong, the dialogue shows the error code; details are in `<OMSI_PATH>\.omsilaunch\diagnostics`.
