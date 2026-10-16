# Exit Codes

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> British English edition of the [canonical page](../../../reference/exit-codes.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page lists every process exit code that `OmsiLaunch.exe` and `OmsiLaunchW.exe` can return: the public managed contract `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), the native shim codes `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` and `OmsiLaunch.WindowsHost.cpp`), and the classification rules `CliProgram.Classify` applies to any escaped exception (`tools\OmsiLaunch.Cli\Program.cs`). Callers must infer semantics from the code and from the structured error envelope, never from message text. Error codes are catalogued in [errors](errors.md); the commands that produce each code are in the [CLI reference](cli.md).

## Public exit codes (`PublicExitCode`)

| Code | Enum name | Meaning | When |
|---|---|---|---|
| 0 | `Success` | The command completed. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` with a runnable plan; a session that ended in `Completed`; `/silent` once `OmsiLaunchW.exe` started; a forwarded client command that the owner answered with `Ok=true`; `/recover` when nothing was pending or the restore completed. |
| 1 | `SessionFailed` | A plan was not runnable, or an owned session ended in `Failed`. | `/plan` reporting `NOT RUNNABLE`; a launch whose plan is not runnable (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` also shows the last `OL_E_` diagnostic in a message box); `OL_E_PLAN_NOT_RUNNABLE` raised by `StartSessionAsync` (re-plan at start); the session did not reach `Running` within the startup timeout; the session terminated in `Failed`. |
| 2 | `InvalidArguments` | The command line, spec, profile or runtime arguments were rejected before or during dispatch. | Unknown flag or route, missing value, value out of range; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` and `OL_E_RUNTIME_ARGUMENT_REQUIRED` (locally or returned by the owner); usage printed for an undispatchable command; any `ArgumentException`, `FormatException`, `InvalidDataException` or `OverflowException`. |
| 3 | `UnsupportedProfile` | The platform or OMSI build is not supported. | An escaped exception whose code starts with `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Note that the same conditions found during planning make the plan not runnable and return `1` instead. |
| 4 | `NoActiveSession` | A client command found no owner. | `session status`, `session stop`, `events read`, `events watch`, or a forwarded runtime operation when the local control endpoint of this installation does not answer (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | A timeout escaped as an exception. | Any `TimeoutException` (`OL_E_TIMEOUT` when the message carries no code, otherwise the embedded code such as `OL_E_RUNTIME_REQUEST_TIMEOUT`). Forwarded client timeouts are answered by the owner as `Ok=false` and return `7`, not `5`. |
| 6 | `NotFound` | A file or directory was not found. | `FileNotFoundException` / `DirectoryNotFoundException` (`OL_E_NOT_FOUND` default), for example `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` when raised as an exception, a missing installation directory during `/list`. |
| 7 | `OperationRejected` | The command was valid but refused, or a forwarded command failed at the owner. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; every `Ok=false` control reply other than the two argument codes (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); any other escaped exception carrying an `OL_E_` code that is not classified elsewhere (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | A durable transaction could not be restored. | `/recover` when the journal was pending and remains pending; any escaped exception whose code starts with `OL_E_RECOVERY_` or is `OL_E_RESTORE_FAILED` (for example `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` during a session's own restore). |
| 10 | `InternalError` | An unexpected exception without an `OL_E_` code. | Reported as `OL_E_INTERNAL` with category `internal`; the message is the exception text. |

Code `9` is not assigned.

## Native shim exit codes

Returned by `OmsiLaunch.exe` / `OmsiLaunchW.exe` before the managed controller runs. They are disjoint from `PublicExitCode` so a caller can tell a host-startup failure from a controller result. `OmsiLaunchW.exe` additionally shows `OmsiLaunch could not start the .NET host (code N).` in a message box.

| Code | Meaning | Cause |
|---|---|---|
| 100 | Executable path could not be resolved | `GetModuleFileNameW` failed. |
| 101 | Command line could not be tokenised | `CommandLineToArgvW` returned null. |
| 102 | `hostfxr` location probe failed | `get_hostfxr_path` size query failed: no matching .NET runtime is installed (the x64 .NET 6 runtime is required). |
| 103 | `hostfxr` path could not be retrieved | Second `get_hostfxr_path` call failed. |
| 104 | `hostfxr` library could not be loaded | `LoadLibraryW` on the resolved `hostfxr.dll` failed. |
| 105 | Required `hostfxr` exports are missing | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` or `hostfxr_close` not found. |
| 106 | Managed host could not be initialised | `hostfxr_initialize_for_dotnet_command_line` failed for `OmsiLaunch.Controller.dll` (missing `OmsiLaunch.Controller.runtimeconfig.json`, missing `Microsoft.WindowsDesktop.App` 6.0 x64, or a damaged package). |

## Classification rules (`CliProgram.Classify`)

Every exception that escapes `CliProgram.RunAsync` is turned into an error envelope (`CliInput.WriteError`) and an exit code by `CliProgram.ReportFailure`, which calls `Classify`. Parsing failures are handled the same way before dispatch (exit `2`). The rules apply in this order:

1. The first `OL_E_` token in the exception message is extracted (`ExtractCode`): the code is the maximal run of ASCII letters, digits and `_` starting at `OL_E_`. Codes are surfaced verbatim in `error.code`.
2. `SessionProfileException` → its own `Code`, category `invalid_argument`, exit `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → extracted code or `OL_E_INVALID_ARGUMENT`, category `invalid_argument`, exit `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → extracted code or `OL_E_NOT_FOUND`, category `not_found`, exit `6`.
5. `TimeoutException` → extracted code or `OL_E_TIMEOUT`, category `runtime`, exit `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, category `session`, exit `7`.
7. Otherwise, when a code was extracted:
   - starts with `OL_E_RECOVERY_` or equals `OL_E_RESTORE_FAILED` → category `transaction`, exit `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → category `session`, exit `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → category `session`, exit `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → category `invalid_argument`, exit `2`;
   - starts with `OL_E_UNSUPPORTED_` → category `unsupported_profile`, exit `3`;
   - any other code → category `runtime` for `InvalidOperationException` and `IOException`, otherwise `internal`; exit `7`.
8. No code at all → `OL_E_INTERNAL`, category `internal`, exit `10`.

Forwarded client replies bypass `Classify`: `CliProgram.ReportForwarded` returns `4` for no endpoint, `2` for `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` for any other `Ok=false` reply, and `0` for `Ok=true`.

## Scripting guidance

- Treat `0` as success and everything else as failure; branch on the numeric code, then on `error.code` from the `--json` envelope.
- A session launch returns only after the session ended and its files were restored; `1` means the transaction ran but OMSI failed or the plan was rejected, not that files were left modified (a residual journal is reported by `/recovery-status`).
- `100`..`106` mean the package or the .NET runtime is broken; see [installation](../getting-started/installation.md) and [packaging](packaging.md).
