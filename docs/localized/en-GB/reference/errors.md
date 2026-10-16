# Error and diagnostic codes

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> British English edition of the [canonical page](../../../reference/errors.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page is the normative reference for every code in `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 `OL_E_` error codes and 4 `OL_W_` warnings, grouped by catalogue category, plus the informational diagnostic codes that are not errors. For each code it states where the current code raises it, what it means, how it reaches you (thrown exception, result field, diagnostic, control-plane reply or CLI envelope) and what to do. Meanings are taken from the raise sites; where a code is defined but has no current raise path, the page says so.

Related pages: [public API](public-api.md), [exit codes](exit-codes.md), [LaunchSpec](launchspec.md), [session lifecycle](../concepts/session-lifecycle.md), [transactions and recovery](../concepts/transactions-and-recovery.md), [local control plane](local-control.md), [runtime control](runtime-control.md), [session profiles](session-profiles.md), [permanent plugin](../concepts/permanent-plugin.md).

## How codes reach you

| Surface | Meaning |
| --- | --- |
| Thrown | An exception whose `Message` starts with the code (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). The CLI extracts the code from the message and maps it to an exit code (`CliProgram.Classify`). |
| Plan diagnostic | A `LaunchDiagnostic` in `SessionPlan.Diagnostics`; any `OL_E_` code makes `IsRunnable` false (CLI: plan `NOT RUNNABLE`, exit 1). |
| Session diagnostic | A `LaunchDiagnostic` in `SessionStatus.Diagnostics`; the session state is `Failed` (CLI exit 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` and `OL_E_RESTORE_FAILED` wrap an inner code in their message. |
| Runtime result | `RuntimeCommandResult.ErrorCode` with `Succeeded = false`. |
| Runtime detail | `RuntimeCommandResult.ErrorCode` is the specific code and `Values["detail"]` the message (with `Values["exception"]`, the .NET type). Beta 4: a typed plugin-side code reaches the API and the CLI unchanged; only a failure without a catalogued code becomes `OL_E_RUNTIME_OPERATION_FAILED`. |
| Control reply | `ErrorCode` of a local control plane reply (`LocalControlResponse`). |
| CLI envelope | `error.code` in the `--json` envelope, or `<code>: <message>` on the console; the exit code is given. |
| Telemetry | A runtime event name that the host maps to a session diagnostic. |

## Cli

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | An `OperationCanceledException` escaped (Ctrl+C or a cancelled client wait). | CLI envelope, exit 7 | Retry the command. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | An exception without an `OL_E_` code escaped: malformed `/spec` JSON, a null required spec member, an unexpected fault. | CLI envelope, exit 10 | Read the message and `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; fix the input; report if unexplained. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | A `TimeoutException` without a code escaped; or the local control client was connected to an owner that did not reply within the timeout (the owner exists, so this is not reported as `OL_E_NO_ACTIVE_SESSION`). (Mailbox timeouts carry `OL_E_RUNTIME_REQUEST_TIMEOUT` instead.) | CLI envelope, control reply; exit 5 from `Classify`, exit 7 for a control reply | Retry; check that OMSI and the owner are responsive. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` is not beside `OmsiLaunch.exe`. | CLI envelope, exit 7 | Reinstall the package. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` of `OmsiLaunchW.exe` returned no process. | CLI envelope, exit 7 | Check the package files and permissions; run without `/silent` to see the failure. |

## Compatibility

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` on `plugin.build.invalid` | The start capabilities resolved, but the live Start form or one of its hook sites is not in the expected state when the plugin arms the headless start (for example another plugin already patched it). | Session diagnostic (`Failed`) | Remove the conflicting plugin; see [compatibility](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` at start; runtime operations) | A native capability the request needs could not be resolved and verified on this OMSI build, so it is unavailable. At start the session fails before any hook and the files are restored; a runtime operation is rejected without touching OMSI memory. `Data["capability"]` (runtime) or the telemetry `capabilities` value names it. | Session diagnostic (`Failed`); runtime result | See [compatibility](compatibility.md): the capability is not available on this build. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` unavailable) | The installation is not an OMSI 2 installation: `Omsi.exe` is missing, is not a 32-bit x86 PE image, has no OMSI 2 (2.x) version, or the root has no `maps\` or `Vehicles\` directory. The executable's hash is not a criterion. | Plan diagnostic | Point OmsiLaunch at an OMSI 2 installation root. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` unavailable); also thrown by `CurrentWindowsX64Platform.ValidateCurrent`, which the service does not call | Not Windows 10 or later, or the OS or the host process is not x64. | Plan diagnostic (CLI exit 3 when thrown) | Run on 64-bit Windows 10 or later. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | `CurrentWindowsX64Platform.ValidateCurrent` only | OS or host architecture is not x64. The service does not call `ValidateCurrent`; no current raise path. | Thrown (`PlatformNotSupportedException`) by that method only | See source `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

## Content

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validation: NEW_MAP without `EntrypointIdentity` and with `PresentedEntrypointIndex` unset or negative. Planner: the map has no entrypoint with the requested identity or exact (case-sensitive) label. | Plan diagnostic | Set `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) or an identity from `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | The start weather cannot be applied: a preset that is not a file of the installation's `weather` folder, or an ICAO that is not four letters or digits. | Plan diagnostic | Use an installed preset or a valid station code. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | The requested entrypoint shares its label with an earlier record of the map; OMSI presents only the first record of each label, so it cannot be selected at start. | Plan diagnostic | Choose the first record with that label, or another entrypoint. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` unavailable) | NEW_MAP map resolved, but no presented index and no identity. Always accompanies `OL_E_ENTRYPOINT_NOT_FOUND`. | Plan diagnostic | Same. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` is not an installed `Vehicles\...\*.hof`. | Plan diagnostic | Use an identity from `/list:hofs`. (Player vehicle fields are non-runnable on this build anyway.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | A [HOF management](hof-management.md) source (collection, directory, vehicle or file) does not exist, has no `.hof`, or lacks a file named in `Files`. | Plan diagnostic; start failure | Fix the source or the file list. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | A HOF target vehicle file or folder is not installed. | Plan diagnostic; start failure | Use a vehicle identity from `/list:vehicles` or an existing folder below `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | A HOF path leaves the installation, a target is not below `Vehicles\`, a collection name is not a plain name, or a junction/symlink is traversed. | Plan diagnostic; start failure | Use installation-relative paths without `..` and without links. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Two HOF operations write different bytes to one file, or a mirror removes a file another operation writes. | Plan diagnostic; start failure | Reorder, split or remove conflicting operations. |
| `OL_E_HOF_INVALID` | `HofPlanner` | HOF operation without targets or source, a non-`.hof` file source, a target file that is not `.bus`/`.ovh`, or a size/count limit (8 MiB per file, 256 files per operation, 256 MiB per session). | Plan diagnostic; start failure | Fix the operation. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validation: NEW_MAP with `MapIdentity` unset or not of the form `maps\<dir>\global.cfg`. Planner: the map is not installed. | Plan diagnostic | Use an identity from `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | A `FileNotFoundException`/`DirectoryNotFoundException` without a code escaped, for example `/list:repaints` with an unknown `/vehicle-scope`, or `/list:entrypoints` with an unknown `/map`. | CLI envelope, exit 6 | Correct the identity. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` is not a `.cti` item of the model (checked only when `Model` is set). | Plan diagnostic | Use an identity from `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | The map referenced inside the selected `.osn` is not installed. | Plan diagnostic | Install the map or choose another situation. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION without `SituationIdentity`, or the `.osn` is not installed. | Plan diagnostic | Use an identity from `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` is not an installed `Vehicles\...\*.bus`. | Plan diagnostic | Use an identity from `/list:vehicles`. |

## Installation

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | The installation lease (`Local\OmsiLaunch.Installation.<hash>`) is held by another owner in this logon session, or a journalled OMSI process (PID + creation time + executable path; or any `Omsi.exe` from the root for a journal past `HandoffCreated` without a PID) is still alive. | Start: session diagnostic via `OL_E_START_SESSION`. Recovery: thrown (`InvalidOperationException` / `IOException`). CLI exit 7. | Stop the other owner (`session stop`) or wait for OMSI to exit, then retry or `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` is empty. | Plan diagnostic | Pass the installation directory. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` unavailable); also `ValidateCurrent` | The root does not exist, has the read-only attribute, or has no `plugins\` directory. | Plan diagnostic | Point at a real, writable OMSI installation. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (plan and start) | An installed `plugins\OmsiLaunch.*` file differs from the hash in `release-manifest.json` (or from the reference closure without a manifest). | Plan diagnostic (plan not runnable, CLI exit 1); session diagnostic via `OL_E_START_SESSION` only if the files change between planning and start | Reinstall the OmsiLaunch package so `plugins\` and the manifest agree. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (plan and start) | The manifest has no entry for a required plugin file. | Plan diagnostic; session diagnostic via `OL_E_START_SESSION` in the same race as above | Reinstall the package. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (plan and start) | A required `plugins\OmsiLaunch.*` file is absent from the OMSI installation, or a `plugins/` file listed in `release-manifest.json` is not installed. | Plan diagnostic; session diagnostic via `OL_E_START_SESSION` in the same race as above | Install the permanent plugin closure ([installation](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | `CurrentWindowsX64Platform.ValidateCurrent` only | `CurrentPlatformSupported` false. Not called by the service; no current raise path. | Thrown by that method only | See source. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` is empty, is not JSON, has no `files` array, or has an entry without `path`/`sha256`, a hash that is not 64 hex digits, a path that is rooted, contains `:`, an empty, `.` or `..` segment, or a path listed twice (compared case-insensitively, `/` and `\` equivalent). A UTF-8 BOM is accepted. | Plan: wrapped in `OL_E_RUNTIME_ARTIFACT_MISSING`; start: via `OL_E_START_SESSION` | Reinstall the package. |

## InvalidArgument

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validation: `Date.Value`/`Time.Value` set while the mode is not `Explicit`. CLI: unknown flag, missing value, bad integer or range, `/saved` combined with `/map`/`/entrypoint`, unknown command route, any `ArgumentException`/`FormatException` without a code. | Plan diagnostic; CLI envelope exit 2 | Fix the argument. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (start) | A semantic setting value is out of range, not boolean, not in the allowed set, or malformed (`graphics.particles` needs four fields). Values are not validated at plan time. | Session diagnostic via `OL_E_START_SESSION` | Use a value from the [settings table](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | The key exists but is not writable (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Plan diagnostic; CLI exit 2 | Remove the key. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | The key is not in `ConfigurationCatalog`. | Plan diagnostic; CLI exit 2 | Use a catalogue key. |

## LaunchSpec

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | The root is not a JSON object, or deserialisation produced no record. | Thrown (`InvalidDataException`), CLI exit 2 | Fix the file ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | The `/spec` file does not exist. | Thrown (`FileNotFoundException`), CLI exit 6 | Check the path. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | The file exceeds 1 MiB. | Thrown (`InvalidDataException`), CLI exit 2 | Reduce the file. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | A member that is not a public property of the record at that position; message `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Thrown (`InvalidDataException`), CLI exit 2 | Remove or rename the member. |

## LocalControl

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Owner handler (`OwnerSession`) | The command is not `session.status`, `session.events`, `session.stop` or `runtime.execute` with an `operation` argument. | Control reply; CLI exit 7 | Use a supported command. |
| `OL_E_CONTROL_FAILED` | CLI client (`ReportForwarded`, `CliEventWatch`) | The owner replied `Ok = false` without an error code. | CLI envelope, exit 7 | Read the message; check the owner's console/diagnostics. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | The owner's handler threw an exception whose message carries no `OL_E_` code (for example the session was already closed), or the handler's reply could not be serialised. | Control reply | Read `session status`; restart the owner if it is gone. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (both ends) | Length prefix negative or above 64 KiB (including an oversized request frame), empty frame, JSON `null`, a request without `Command`, or JSON that could not be decoded. | Control reply / CLI envelope | Use the documented protocol ([local control plane](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (client) | The client's own serialised request exceeds 64 KiB. Reported to the caller; nothing is sent. | Control reply / CLI envelope | Reduce the request. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (owner) | The owner's reply does not fit the 64 KiB frame. The owner answers with this typed error instead of dropping the reply. `session.status` and `session.events` never reach it: their event history is trimmed oldest-first to fit. | Control reply / CLI envelope | Retry; for events, read more often. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | The request `ProtocolVersion` is not `0.1`; the owner reply could not be decoded or was empty; the owner closed the connection without replying or the connection broke after it was established; the owner reported no `SessionId`. | Control reply / CLI envelope | Match client and owner versions; read `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Owner handler | `session.stop` or `runtime.execute` without a `session_id` equal to the active session. | Control reply; CLI exit 7 | Read `session.status` first and bind the request (the CLI does this automatically). |

## Other

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | The plan passed in has `IsRunnable = false`, or the re-plan at start is not runnable (`Omsi.exe` changed, content removed, plugin closure missing); the message lists the current `OL_E_` codes. | Thrown (`InvalidOperationException`); CLI exit 1 | Plan again and fix the listed diagnostics. |

## Presentation

All raised by `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). At plan time they are wrapped in `OL_E_SESSION_PRESENTATION_INVALID` (message carries the code); at start they surface via `OL_E_START_SESSION`.

| Code | Meaning / typical cause | What to do |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | The `.itx` file is empty, has an odd number of non-blank lines, or a URL line is not an absolute `http`/`https` URL. | Use URL/target line pairs. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (resolved against the process working directory) does not exist. Thrown as `FileNotFoundException`. | Pass an existing `.itx` path. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` is `Override` without `OverrideProfilePath`. CLI exit 2 when thrown. | Provide `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | A target line is rooted, contains `..`, starts with `\`, resolves outside the installation, lacks a `Texture\` component, or traverses a junction/symlink. | Use `Texture\...` relative targets. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` does not exist. | Fix the directory. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` or `<LANG>.bmp` is missing from the asset directory, or a packaged `assets\splash\<LANG>.bmp` is missing when seeding `.omsilaunch\assets\splash`. | Provide the BMPs / reinstall the package. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | A splash BMP is not a 640×480 24-bit `BM` bitmap. | Convert the image. |

## Process

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (start failure and supervisor fault paths) | Terminating or waiting for OMSI during error cleanup threw; the inner message follows. | Session diagnostic (added to a `Failed` session) | Ensure no `Omsi.exe` remains, then `/recover` if a journal is pending. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` failed right after `CreateProcessW` (`Win32=<code>`); the process is terminated. | Session diagnostic via `OL_E_START_SESSION` | Retry; check antivirus/permissions. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI exited before `gameplay.entered` (crash, an OMSI error dialogue was closed, the window was closed). | Session diagnostic (`Failed`); restore runs | Check OMSI's own logs and `logfile.txt`; look at `RuntimeEvents` for the last plugin event. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` failed (`Win32=<code>` in the message). | Session diagnostic via `OL_E_START_SESSION` | Resolve the Win32 error (missing file, access denied, policy). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | The supervisor loop threw (telemetry read, process wait/terminate, journal write); OMSI is terminated and restore attempted. | Session diagnostic (`Failed`) | Read the inner message and the host log. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` failed (`Win32=<code>`). | Inside `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` messages | Terminate OMSI manually, then `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` on the process handle failed. | Inside `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` messages | Same. |

## Runtime

"Runtime detail" means the specific code in `ErrorCode`, with the message in `Values["detail"]` and the exception type in `Values["exception"]`.

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` with `preset` while `family` is 2 (external) or 3 (map); presets exist only for driver (0) and passenger (1). | Runtime detail | Omit `preset` or use family 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | `Date`/`Time` mode `Explicit` without a value or with out-of-range components. The name is historical; it is a plan-time validation error. | Plan diagnostic | Fix the value (and note explicit date/time is non-runnable on this build). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: the `.bus` path does not exist under the OMSI working directory (checked before the native call so OMSI cannot substitute a fallback). | Runtime detail | Use an identity from `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | same | Native MakeVehicle changed the road-vehicle collection by more than one object. | Runtime detail (`native_status`, counts in the message) | Report; the created objects remain until the session ends. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | same | The collection did not change; OMSI refused the vehicle silently. | Runtime detail | Check the `.bus` file; try another model. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | same | Any other non-zero native status. | Runtime detail | Report with the message counts. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | The profiled PlaceRandomBus call returned a failure status. | Runtime detail | Retry once gameplay is stable; report. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; plugin-side checks (`time.set` with no `hour`/`minute`/`second`; `camera.set` with no `family`/`field_of_view`; `camera.lock` without a parseable `family`; vehicle/curve operations) | A required argument is missing or blank. | Runtime result (registry; CLI exit 2) or runtime detail (plugin) | Supply the argument ([runtime control](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | The plugin closure reference directory/file or the native bridge given in `OmsiLaunchRuntimePaths` cannot be loaded (may wrap `OL_E_RELEASE_MANIFEST_INVALID`). | Plan diagnostic | Run from an intact package. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, INTERNAL harness) | The baseline `time.read`/`camera.read` failed, so the write test was skipped. | Batch artefact only | Not user-facing. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` is empty, longer than 240 characters, contains NUL or `..`, does not start with `Vehicles\` or does not end with `.bus`. | Runtime detail | Pass `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | none (retained for compatibility) | No longer emitted. Earlier builds raised it when a cancelled request stayed in the slot; every terminal path of a request now resets the slot, and a leftover request or response found at the start of a new request is cleared. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | The mailbox was disposed because the session is ending. | Thrown (`InvalidOperationException`) | None; the session is over. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | The mailbox slot held a state value that is not idle, requested or responded (corruption). The slot is reset and the error is raised; the next request works normally. | Thrown (`InvalidDataException`) | Retry; report if persistent. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | The vehicle's constants block pointer is null. | Runtime detail | The vehicle has no constants; nothing to do. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` is not in the vehicle's constant table. | Runtime detail | List constants first. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | The object created by spawn has a VMT outside the OMSI image range. | Runtime detail | Report. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | same | The created object is not in the road-vehicle collection. | Runtime detail | Report. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Two consecutive curve points share the same X. | Runtime detail | Content problem in the vehicle's curve. |
| `OL_E_RUNTIME_CURVE_EMPTY` | same | The curve has no points. | Runtime detail | Same. |
| `OL_E_RUNTIME_CURVE_INVALID` | same | No segment of the curve contains `x`. | Runtime detail | Evaluate inside the curve's domain. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | same | `name` is unknown or its function pointer is null. | Runtime detail | List curves first. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | The vehicle definition pointer is null. | Runtime detail | Handle refers to a vehicle without definition data. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (owner mode) | `plugins\OmsiLaunch.Plugin.opl` or `plugins\OmsiLaunch.Native.x86.dll` is missing beside the executable. | CLI envelope, exit 7 | Reinstall the package. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` missing or blank for `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (the registry normally rejects these first with `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Runtime detail | Supply the handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | The handle is unknown, the object left the collection, the address generation advanced, or the object fingerprint (VMT + definition/model identity) changed because the address was reused. Residual blind spot: same class and model recreated at the same address between two list reads. | Runtime detail | Run `road-vehicles.list`/`humans.list` again and use the new handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; `D3DRuntimeApi` fallback | Generic plugin-side failure wrapper; `Values["detail"]` holds the message (often a more specific code) and `Values["exception"]` the exception type. An exception that escapes an operation inside the mailbox dispatcher is also answered with this code (without values) instead of leaving the request unanswered. | Runtime result | Read `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | The plugin has no implementation for an operation the registry allowed (registry/plugin version drift). | Runtime detail | Reinstall a consistent package. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | The operation is not in `PublicRuntimeOperationIds`, including every `internal.*` operation. Checked before session lookup. | Runtime result; control reply; CLI exit 2 | Use a public operation id. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` with `preset` while there is no player vehicle (headless sessions have none). Also reported as `code` of the `camera.lock.degraded` event when re-application fails. | Runtime detail / runtime event | Lock without a preset, or use a session with a player vehicle. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | A successful D3D result had no values, or an unknown device-state string. | Thrown (`OmsiRuntimeException`) | Match host and plugin versions. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | The slot holds a stale response with the same request id as the new request. The stale response is cleared before the error is raised. | Thrown (`InvalidOperationException`) | Use strictly increasing request ids. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | No response within `timeout`; the slot is reset and a late response is discarded. | Thrown (`TimeoutException`); CLI exit 5 | Retry with a longer timeout; check that OMSI is not blocked (modal dialogue, loading). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | The response envelope is corrupt, has a bad length (negative, zero or larger than the slot), a foreign session id or a different request id. The slot is reset before the error is raised, so the next request works normally. | Thrown (`InvalidDataException`) | Retry; report if persistent. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | The `cursor` of a list operation is malformed or belongs to another kind of list. | Runtime detail | Pass the `next_cursor` of the previous page of the same operation, or omit it. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | The serialised result exceeds the 64 KiB mailbox. Paged list results are shortened to fit instead and continue with `next_cursor`; no list result reaches this code. | Runtime result | Use a narrower operation or a smaller `limit`. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | The vehicle's script definition or state pointer is null. | Runtime detail | The vehicle has no script objects. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; plugin mailbox | `RuntimeCommand.SessionId` differs from the handle's session id (thrown by the host), or a request reached a plugin bound to another session (returned by the plugin as a typed result). | Thrown (`InvalidOperationException`) / runtime result | Build the command with `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`world.failed` event `code`) | The start weather could not be applied after the world started: OMSI has no ICAO station configured for `/weather-real`, or its weather objects are unavailable. The session fails. | Session failure | Configure a station in OMSI or use `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | A `weather.set` key names a value OMSI derives on every weather tick (humidity, light factors, cloud transparency, precipitation flag, wet ground); nothing is written. | Runtime result | Set the source key named in `detail` (for example `dew_point` instead of `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Unknown `weather.set` key; nothing is written. | Runtime result | Use a key listed in [capabilities](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` is not in the string-variable table. | Runtime detail | List string variables first. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | A weather boolean is not `true`/`false`/`1`/`0`. Currently unreachable (see above). | Runtime detail (defined) | See source. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` not an integer, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` may be -1), `scheduled` 0..1; `vehicle.variable.set`: `value` not finite; `vehicle.curve.evaluate`: `x` not finite. | Runtime detail | Use a value in range. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` is not in the numeric variable table. | Runtime detail | List variables first. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | The variable slot or value address is null. | Runtime detail | The variable is not materialised for this vehicle. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | The clock scalars were written but the profiled native SetTime call returned failure. | Runtime detail | Retry; read back with `time.read`. |

## RuntimeD3D

All raised by `CurrentRuntimeControl` (`ThrowD3D` mapping of the native status; `detail` carries the operation and HRESULT, `native_status` the numeric status). Surface: runtime result with the code in `ErrorCode`; `D3DRuntimeApi` rethrows as `OmsiRuntimeException`.

| Code | Native status / cause | What to do |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: the Direct3D device is lost. | Wait for `d3d.restored`; recreate textures (generation changed). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: missing or invalid `width`, `height`, `level`, `x`, `y`, `format` or `handle` (ranges: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Fix the arguments. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` is not valid Base64 or exceeds 48 KiB. | Send smaller rectangles. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, or an unknown `format` name (valid: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Use a listed format. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Any other status; the HRESULT is in `detail`. | Report with the HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: the device is not ready (before the first frame or while stopping). | Retry after `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: a device reset is in progress. | Retry after `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: the texture handle was already released. | Do not reuse released handles. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: the handle belongs to a previous device generation; or the handle string is not `d3dtex-<session>-<hex>` / is zero. | Recreate the texture. |

## Session

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` on `plugin.request.unsupported` | Plan: `LastMapState`, `EntrypointIdentity`, date/time/year mode, weather mode, player-vehicle fields or input documents requested (`Requested capability unavailable: <name>`). Telemetry: the plugin rejected the handoff (cannot happen for a runnable plan). | Plan diagnostic; session diagnostic | Remove the unsupported request ([known limitations](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` on `headless.arm.failed` | The plugin could not arm the one-shot headless start hook in OMSI. | Session diagnostic (`Failed`) | Verify the build; report. |
| `OL_E_NO_ACTIVE_SESSION` | CLI client (`ReportForwarded`, `CliEventWatch`) | No owner answers on the installation's control pipe (no session, or the owner is still starting/validating). | CLI envelope, exit 4 | Start a session, or wait until it is `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` elapsed before `plugin.started` (OMSI did not load `plugins\OmsiLaunch.Plugin.opl`, or is stuck before plugin init). | Session diagnostic (`Failed`) | Check the plugin closure, `plugins\OmsiLaunch.Plugin.opl` and OMSI's `logfile.txt`. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` on `plugin.handoff.invalid` or unparseable telemetry JSON | The plugin could not read/verify the startup handoff (versions 3 to 6, SHA-256), or sent invalid telemetry. | Session diagnostic (`Failed`) | Match host and plugin versions (reinstall the package). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | An owner already answers `session.status` for this installation. | CLI envelope, exit 7 | Use client commands (`session status`, `session stop`, runtime commands). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | The session state is not `Running`. | Thrown (`InvalidOperationException`) | `WaitForAsync(session, SessionState.Running, ...)` first. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | The splash/ITX plan could not be built; the message carries the presentation code. | Plan diagnostic | See [Presentation](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (OmsiLaunchW dialogue) | Fallback code shown when a launch plan is not runnable or the session did not reach gameplay, and no `OL_E_` diagnostic exists. | Message box only | Read `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` on `world.situation.failed` | The native saved-situation start returned a failure (`native_status` in the event). | Session diagnostic (`Failed`) | Check the `.osn` and its map. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | The plugin started, but `Running` was not reached within `StartupTimeoutSeconds`. | Session diagnostic (`Failed`) | Increase `/startup-timeout` for large maps; check `RuntimeEvents` for the last world event. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Any exception in the start path; the message is the inner message (usually starting with the inner code). | Session diagnostic (`Failed`) | Act on the inner code. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` on `world.failed` | The native NEW_MAP start returned a failure (`native_status` in the event). | Session diagnostic (`Failed`) | Check the map, entrypoint index and OMSI's logs. |

## SessionProfile

All raised by `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) or `CliInput`, thrown as `SessionProfileException` (an `IOException` with `Code`), CLI exit 2. See [session profiles](session-profiles.md).

| Code | Meaning / typical cause | What to do |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | The preset's splash `assets` directory or internet-textures `profile` file does not exist inside the package. | Add the asset. |
| `OL_E_SESSION_PROFILE_INVALID` | Structural or limit violation: more than 256 KiB, not exactly one root mapping, YAML anchors, unknown key, missing required key, non-scalar where a scalar is required, `id` differs from the directory name, presets not 1..99 or duplicate `index`, non-positive timeouts, unsupported weather/splash/internet-textures mode, date/time not `explicit`, invalid YAML, number/date parse errors. | Fix the YAML per the message. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` is non-empty and does not contain the selected map (NEW_MAP) or the map of the selected situation (SAVED_SITUATION). | Choose a compatible world. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` does not exist. | Check the id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | An explicit CLI argument targets a field owned by the selected profile/preset. | Drop the flag or choose another preset. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | The id contains `\`, `/`, `:` or `..`; or an asset path is rooted, escapes the package or traverses a junction/symlink. | Keep paths inside the package. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` missing, outside 1..99, or not declared by the profile. | Use a declared preset index. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` is not `omsilaunch.session-profile/v1`. | Use the supported schema. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | A preset setting key is known but not writable. | Remove the key. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | A preset setting key is not in the catalogue. | Use a catalogue key. |

## Transaction

See [transactions and recovery](../concepts/transactions-and-recovery.md).

| Code | Raised by | Meaning / typical cause | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | A stale `closecheck` still exists after `File.Delete`. | Session diagnostic via `OL_E_START_SESSION` | Remove `<root>\closecheck` manually (permissions). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | A path that did not exist before the session now contains content that differs from what the session applied; it is not removed and the journal is kept. | Thrown (`IOException`); inside `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; CLI exit 8 | Inspect the file; remove or move it, then `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (pre-fingerprint journal) | The journal has no applied-content fingerprint for an originally absent, non-deletion path, so ownership cannot be proven. At session start the recovery is deferred and retried with this session's planned bytes; via `RecoverPendingAsync` it is thrown. | Thrown (`IOException`); CLI exit 8 | Start a session with the same spec (supplies the bytes), or inspect and remove the file, then `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | A backup's SHA-256 does not match the snapshot recorded in the journal; nothing is written. | Thrown; inside `OL_E_RESTORE_FAILED`; CLI exit 8 | Restore the file from your own backup; then delete the journal only when you are sure. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Snapshots exist in memory but `journal.json` is gone (deleted during the session). | Thrown; inside `OL_E_RESTORE_FAILED` | Verify the session files manually. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` still exists after deletion (restore itself succeeded and was verified). | Thrown; inside `OL_E_RESTORE_FAILED`; CLI exit 8 | Delete `<root>\.omsilaunch\journal.json` (permissions) or run `/recover` again (idempotent). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (start failure / supervisor) | OMSI's exit could not be confirmed, so files were not replaced while it may still use them; the journal is retained. | Session diagnostic (`Failed`) | After `Omsi.exe` has exited, run `/recover` (or the next start recovers automatically). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (start failure / supervisor) | `RestoreAsync` threw; the message carries the inner code; the journal is retained. | Session diagnostic (`Failed`); CLI exit 8 when thrown from `/recover` | Act on the inner code, then `/recover`. |

## Warning

| Code | Raised by | Meaning | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | The OMSI 2 executable has no known layout (its code identity is not in the build catalogue). The plan stays runnable: every native capability is resolved and verified in process, and one that cannot be verified is unavailable. | Plan diagnostic (runnability not affected) | None needed; check `/plan` and `profiles` for predicted capabilities. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (supervisor) | `Behavior.CooperativeShutdown` was requested but OMSI could not be asked to quit (its exit routine is unresolved on this build, or OMSI is already closing), or it did not exit within `ShutdownTimeoutSeconds`: OMSI was terminated instead. The restore runs as usual. | session diagnostic | None: the session ends as with a forced termination. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | A capability the start needs could not be resolved from the executable file (for example a DRM-protected executable whose loaded image differs from the file). It is resolved in process at start. | Plan diagnostic (runnability not affected) | None needed; a start fails with `OL_E_CAPABILITY_UNRESOLVED` if it cannot be resolved in process either. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | A module that joined the session by itself (settings file, content or default activation) cannot be loaded (`Data["code"]` names why, `Data["activation"]` how it joined). | Plan diagnostic (runnability not affected) | Fix, disable or remove the module. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | An entry of `.omsilaunch\modules.ini` cannot be used (not a module section, outside a section, unknown key, invalid value; the default is used), or settings were requested for a module the session does not load. | Plan diagnostic (runnability not affected) | Correct the entry or the request. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | A session-deletion path (ITX target, `Texture\standard.ipr`, `closecheck`) did not exist before the session and exists now, but no OMSI process ever started under this journal, so the file cannot be a session by-product. It is kept and reported (message = relative path, `Data["sha256"]`); the transaction still completes. | Session diagnostic / `RecoveryStatus.Diagnostics` (state not affected) | Inspect the file; remove it yourself if unwanted. |

## Module

Runtime modules (see [runtime modules](../../../reference/runtime-modules.md)). At planning a code is a plan diagnostic: for a requested module it makes the plan not runnable; for a module that joined by itself (settings file, content, default) the plan reports `OL_W_MODULE_NOT_LOADED` instead. In a session a code is a session diagnostic (the session continues unless the code is `OL_E_MODULE_SESSION_FAILED`), and for a module operation it is the runtime result.

| Code | Raised by | Meaning | Surface | What to do |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` is missing, is not valid `omsilaunch.runtime-module/v1`, or its `id` differs from its directory name. The message names the field. | Plan diagnostic; `ListRuntimeModulesAsync` | Fix the manifest. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | A requested module id is not installed below `plugins\OmsiLaunch.Modules`. | Plan diagnostic | Check `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | A pinned file is missing, differs from its pinned SHA-256 or is reached through a reparse point; or, in process, `module.json` changed after planning. | Plan diagnostic; session diagnostic | Reinstall the module. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | The module's `bridge_protocol` range includes none of the protocols this build serves (1 and 2). | Plan diagnostic | Use a module version made for this OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | A required capability is unknown to this build, unavailable on this installation, or not resolved in process. | Plan diagnostic; session diagnostic | See [capabilities](capabilities.md) and `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | The session does not run the module's bound map, or a bound file differs from its pinned SHA-256. | Plan diagnostic; session diagnostic | Start the bound map with the bound content. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | The module asked to start a file that is not one of its auxiliary components. | Runtime result inside the module | Module defect. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | The managed component could not be loaded or its entry type created; or the module's session program could not be started or exited before it was ready. | Session diagnostic | Check the module build (x86-compatible, entry type implements `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` or `Stop` threw. | Session diagnostic | Module defect; its `failure_policy` applied. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | A phase or an operation returned after its budget (`timeouts`, `timeout_ms`), or a session program was not ready within `prepare_ms`. | Session diagnostic; runtime result | Module defect; its `failure_policy` applied. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | The module was disabled or failed earlier in the session. | Runtime result | See the session diagnostics for the first failure. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | The session did not load this module, or it is not ready yet (before gameplay). | Runtime result | Request the module at start; wait for `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | The module declares no such operation. | Runtime result | Check the module's `operations`. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | The operation failed without a catalogued code; a module's own code is in `Values["module_code"]`. | Runtime result | Module-specific. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | After stop or failure a tracked resource was still alive (an auxiliary process that did not exit, a background thread that did not end) or its release threw. | Session diagnostic | Report the module; the process ends with OMSI in any case. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | A module whose `failure_policy` is `fail-session` failed; the session is stopped and restored. | Session diagnostic (state `Failed`) | See the preceding module code. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | A requested module setting is invalid or not declared by the module, a module is both enabled and disabled, a setting name is not `<module-id>.<key>`, or a module flag is declared by several modules. | Plan diagnostic (not runnable); CLI error | Correct the request; `/list:RuntimeModules` shows each module's settings. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | An OMSI plugin the module lists under `conflicts.plugins` is switched on (`plugins\*.opl`), or loaded in OMSI. The reason names the `.opl`. | Plan diagnostic; session diagnostic | Switch off or remove that plugin. OmsiLaunch never changes it. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | reserved | Reserved; a native component receives the numeric hook results described in [runtime modules](../../../reference/runtime-modules.md#native-components-and-hooks) instead. | none | none |

## Non-error diagnostic codes

| Code | Emitted by | Message / data | Meaning |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | message = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` was created and its identity recorded. Session diagnostic. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | message = SHA-256 of the removed file | A `closecheck` that existed before the session was permanently removed (`SuppressStaleClosecheckWarning = true`). Session diagnostic. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | message = relative path; `Data["sha256"]` | A session-deletion path was recreated by OMSI during a session whose process had started; it was removed to restore original absence. Session diagnostic / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | message = `manifest` or `self` | Which reference the permanent plugin validation uses. Plan diagnostic. |
| `module.planned` | `SessionPlanner` | message = module id; `Data["module"]`, `Data["status"]`, `Data["activation"]` | The module is in the plan. Plan diagnostic. |
| `module.disabled` | `SessionPlanner` | message = module id | A content or default module was kept out by a disable (request or settings file). Plan diagnostic. |
| `session_profile.selected` | `SessionPlanner` | message = profile id; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Provenance of a session compiled from a session profile. Plan diagnostic. |

Runtime event names (`RuntimeEvent.Type`, not diagnostics) are listed in the [session lifecycle](../concepts/session-lifecycle.md#telemetry-events).
