# Testing And Validation

Session configuration tests require byte-identical restoration after success,
failure, stop, and stale-journal recovery. Non-runtime tests cover lossless
options, negative flags, vectors, ranges, compound blocks, keyboard custom
events, controller preservation, handoff integrity, transaction restore, and
the cross-thread installation lease.

## Offline Validation Entry Point

`tools\Invoke-OfflineValidation.ps1` is the single entry point for everything
that runs without OMSI. It builds `OmsiLaunch.sln` with warnings as errors,
builds the three native projects (`OmsiLaunch.Native.x86` Win32,
`OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64; skip with
`-SkipNative`), then runs each suite executable from `artifacts\bin` and prints
its `PASS` / `FAIL` lines: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`,
`OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`,
`OmsiLaunch.WindowsUiTests` and, unless `-SkipDocs` is given,
`OmsiLaunch.DocumentationTests`. It never launches OMSI and never touches an
OMSI installation; every fixture is a temporary directory. Exit code 1 means at
least one step failed.

`tools\Test-CliLifecyclePolicy.ps1` is superseded by the C# CLI tests in
`tests\OmsiLaunch.WindowsUiTests\CliHardeningTests.cs`. It loads
`OmsiLaunch.Controller.dll` through `Assembly.LoadFrom` and therefore requires
PowerShell 7; Windows PowerShell 5.1 cannot load .NET 6 assemblies and fails
before the first assertion. It is kept for manual use only.

## Offline Suites and Cases

| Suite | File | Cases added or changed by the hardening round |
| --- | --- | --- |
| `OmsiLaunch.TestHost` | `tools\OmsiLaunch.TestHost\HardeningTests.cs` | `options.cp1252-roundtrip` (an `options.cfg` patch keeps CP1252 bytes such as `ç` and writes no U+FFFD); `transaction.deletion-created-during-session` (a deletion path written by OMSI after `ProcessStarted` is removed and recorded as `restore.session-artifact-removed` with its SHA-256); `transaction.deletion-foreign-file-retained` (without a started process the file stays, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` is reported and the journal still completes); `transaction.deletion-recovery-after-crash` (the same decision through `RestorePendingAsync`, backups cleaned); `transaction.metadata-and-backup-cleanup` (last-write time and read-only attribute restored, read-only originals do not block apply, `backup\` removed, no `*.omsilaunch.tmp` left); `transaction.backup-corrupt-rejected` (`OL_E_RECOVERY_BACKUP_CORRUPT`, live file untouched, journal kept); `transaction.recovery-pre-pid-window` (a journal at `HandoffCreated` without a PID is `OL_E_INSTALLATION_BUSY` while an `Omsi.exe` from that root runs, then recovers); `runtime-command.late-response-ignored` (a response after the host timeout is discarded and the next request is staged and answered); `runtime.permanent-plugin-manifest-integrity` (`plugin.integrity.reference = manifest`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` for a stale file the self-check would accept, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`); `telemetry.sequence-samples` (producer sequence distinguishes identical events, an empty or invalidated slot is not a sample); `api.recover-requires-lease` (`RecoverPendingAsync` is `OL_E_INSTALLATION_BUSY` under a held lease, reports without restoring when `restore=false`, restores when `true`, releases the lease); `api.runtime-rejects-internal-operations` (`OL_E_RUNTIME_OPERATION_UNKNOWN` for `internal.road-vehicles.make-basic` and unknown names, `OL_E_RUNTIME_ARGUMENT_REQUIRED` for a missing `model`, result-key scrubbing convention); `diagnostics.retention` (only the 50 newest session-prefixed files survive `HostTrace.Prune`; `tray-host.log` and non-session files stay). |
| `OmsiLaunch.TestHost` | `tools\OmsiLaunch.TestHost\ContentReparseTests.cs` | `content.discovery-junction-cycle` (a junction `maps\A\loop -> maps` does not hang `EnumerateMaps`; the map is reported exactly once); `content.readlines-cp1252` (a `.bus` friendly name with byte `0xE7` decodes as `François Bus`). |
| `OmsiLaunch.UnitTests` | `tests\OmsiLaunch.UnitTests\Program.cs` | `interop.handle-reuse-rejected`: over a fake memory, a vehicle or human handle whose native address is reused by another object, or whose definition changes at the same address, is rejected as `OL_E_RUNTIME_OBJECT_HANDLE_STALE` both on read and on the next list; a removed object that returns at the same address does not revive its handle. |
| `OmsiLaunch.ProfileTests` | `tests\OmsiLaunch.ProfileTests\Program.cs` | `session-profiles.strict-compiler` (unsupported schema, duplicate index, unknown setting, lexical path escape, unknown key) and `session-profiles.reparse-point-rejected` (an `assets` junction that resolves outside the package is `OL_E_SESSION_PROFILE_PATH_ESCAPE`). |
| `OmsiLaunch.WindowsUiTests` | `tests\OmsiLaunch.WindowsUiTests\CliHardeningTests.cs` | `cli.parse-errors` (malformed or out-of-range values such as `/entrypoint-index:abc`, `/observe-seconds:-1`, `/startup-timeout:0`, unknown flags are `ArgumentException`, never a crash; `vehicles summary` and `humans summary` route to `road-vehicles.read` / `humans.read`; timeouts stay unset by default); `cli.spec-strict-loading` (`/spec` timeouts, presentation and settings are honoured, an explicit `/startup-timeout` wins, `OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentacion` and nested paths, `OL_E_SPEC_TOO_LARGE` above `LaunchSpecJson.MaxBytes`, missing file); `cli.exit-code-classification` (`CliProgram.Classify` maps every escaped exception to a `PublicExitCode`, and `SessionFailed` is `1`); `control-plane.binding-and-typed-errors` (per-installation normalised pipe name, `session.stop` carries the active `session_id`, handler exceptions become `OL_E_*` or `OL_E_CONTROL_HANDLER_FAILED`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_MESSAGE_TOO_LARGE`, endpoint survives faulted clients); `owner.close-on-every-exit-path` (`OwnerSession.RunAsync` calls `CloseAsync` after a supervisor fault, a `Failed` start returning exit `1`, and a normal `/observe-seconds` stop). The suite also keeps `windows-ui.localization-and-status`. |
| `OmsiLaunch.IntegrationTests` | `tests\OmsiLaunch.IntegrationTests\Program.cs` | `plugin-runtime.handoff-new-map`, `handoff.saved-situation-v4`, `plugin-runtime.command-channel`, `interop.delphi-memory-primitives` (unchanged by the round). |
| `OmsiLaunch.DocumentationTests` | `tests\OmsiLaunch.DocumentationTests\Program.cs` | The documentation gate: `docs.cli-flags`, `docs.capabilities`, `docs.errors`, `docs.public-api`, `docs.launchspec`, `docs.session-profiles`, `docs.structure`, `docs.links`. It compiles against `OmsiLaunch.Api` and `OmsiLaunch.Core` and fails when a flag, route, capability, public runtime operation, error code, enum value, public type or `LaunchSpec` property is missing from the pages under `docs/`, when a no-effect flag is not marked `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`, when an `OL_E_*` literal in `src\` or `tools\OmsiLaunch.Cli\` is absent from `PublicErrorCodes`, or when a relative link in `docs\` (excluding `localized\`) or a root `*.md` file is broken. See [`docs/README.md`](../README.md). |

None of the cases above launches OMSI. Behaviour they lock down that changes
what happens inside a real session (session-artifact removal, `closecheck`
handling, early recovery, metadata restore, forced termination on every stop
path) still requires a runtime session before it can be called runtime
validated; see
[`docs/status/runtime-validation-status.md`](../status/runtime-validation-status.md).

`OmsiLaunch.Native.x86.vcxproj` is an owned v145 `Debug|Win32`/`Release|Win32`
project and is not built by the managed `.sln` invocation. Any runtime run or
package build that changes `NativeBoundary.cpp` must build that project
explicitly before staging; the deployment manifest consumes
`artifacts\x86\<Configuration>\OmsiLaunch.Native.x86.dll`.

Runtime validation is consolidated by capability matrix. The protected first
baseline remains `NEW_MAP` Grundorf, entrypoint index 1, Nordspitze Bauernhof,
eight seconds RUNNING, requested stop, and normal exact restore.

## Release Presentation Validation

`tools/Test-ReleaseIdentity.ps1` extracts `OmsiLaunch-current.zip` and audits
every distributed OmsiLaunch PE. It verifies the common product/version fields,
the controller's `OmsiLaunch.exe` internal/original filename, and its embedded
icon. `nethost.dll` is intentionally excluded because it is an unmodified
Microsoft runtime dependency, not an OmsiLaunch binary.

`tools/Test-ReleasePresentation.ps1` validates the extracted Release package,
not `artifacts/bin`. It verifies its manifest has configuration `Release`,
contains the permanent `plugins/OmsiLaunch.*` closure and four packaged splash
assets under `.omsilaunch/assets/splash`, and has no Debug or obsolete
`runtime/plugin` path in the manifest. Without
`-RunOmsi` it validates the default-managed, custom-managed, and `Unset` plans.
With `-RunOmsi -InstallPackage`, it installs only the package's product-owned
root and `plugins/OmsiLaunch.*` files into the authorized root, runs each case
through the installed Release CLI, verifies the temporary GUI overlay while the
session is alive, byte-exact GUI restoration, no remaining OMSI process, no
journal, and unchanged third-party plugin hashes. It writes its evidence under
the installation `.omsilaunch/diagnostics` directory.

Wave D D3D evidence uses the same public `StartSessionAsync` path. Session
`3cbd7bb6-d73f-4853-adc8-1213200527d2` validated device acquisition, one owned
QI reference, Reset-hook installation, create/describe, full and rectangular
updates, multiple resources, deterministic repeated-release rejection and
three create/release cycles on OMSI thread `16676`. Session
`fe1e10fd-cc79-484c-aa04-41fc40fef8da` covered forced process exit with a live
texture; `a95d9fb4-66c6-4f7a-8a64-a18d8e3f9225` proved prior-session stale
handle rejection after relaunch. Every run ended with no OMSI process, journal,
lease; `plugins\OmsiLaunch.*` remains as the permanent product installation.
Device loss/Reset tests are blocked
until a safe native lifecycle producer is available; calling Reset directly
from the validation harness is not an acceptable substitute.
