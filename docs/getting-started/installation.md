# Installation

This page explains what OmsiLaunch `0.1.0-beta4` requires, which OMSI build it supports, how the release package is installed into an OMSI installation root, and how to verify the installation with `/version` and `/plan` before starting a session. The package contents are specified in [packaging](../reference/packaging.md); the first launch is described in [first session](first-session.md).

## Requirements

| Requirement | Detail | Failure when missing |
|---|---|---|
| Windows 10 or later, 64-bit | The controller checks `Environment.OSVersion.Version.Major >= 10`, an x64 operating system and an x64 process. | Plan not runnable with `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (or `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), exit `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` requires `Microsoft.NETCore.App` 6.0 and `Microsoft.WindowsDesktop.App` 6.0 (Windows Forms is used by the tray indicator). The shims locate it with `nethost.dll`. | `OmsiLaunch.exe` exits with shim code `102`..`106` before any output; `OmsiLaunchW.exe` shows `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` requires `Microsoft.NETCore.App` 6.0 for x86, because the plugin runs inside the 32-bit `Omsi.exe`. The x86 .NET 6 Desktop Runtime bundle also satisfies it. | The plugin does not start inside OMSI; the session fails to reach `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), exit `1`, files restored. |
| OMSI 2 installed | A 32-bit `Omsi.exe` with an OMSI 2 (2.x) version, and `maps\` and `Vehicles\` in the root. No particular executable hash is required; OMSI 2.3.004 is the known, runtime-validated layout, and on other builds each capability is resolved and verified in process. | `OL_E_UNSUPPORTED_BUILD`; plan not runnable, exit `1`. A capability unavailable on the build fails with `OL_E_CAPABILITY_UNRESOLVED`. See [compatibility](../reference/compatibility.md). |
| Writable installation root | The transaction writes `.omsilaunch\`, overlays under `GUI\`, `Texture\` and `options.cfg` and restores them; the root must be writable by the current user (avoid `Program Files` without appropriate permissions). | `OL_E_INSTALLATION_NOT_WRITABLE`, exit `1`. |
| One user, one owner per installation | The installation lease `Local\OmsiLaunch.Installation.<sha256(root)>` and the control pipe are per logon session. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, exit `7`. |

Both runtimes are separate downloads from Microsoft; install the x64 Desktop Runtime and the x86 Runtime (or x86 Desktop Runtime) for .NET 6. No other component is required. No data leaves the machine.

## Confirm the OMSI build

Replace `<OMSI_PATH>` with your OMSI 2 directory (for example `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

The hash must be one of the two listed above. `OmsiLaunch.exe profiles` prints the same list with its validation status after installation.

## Install the package

1. Download `OmsiLaunch-0.1.0-beta4.zip` and `OmsiLaunch-0.1.0-beta4.zip.sha256`; verify the checksum (`Get-FileHash` must equal the value in the `.sha256` file).
2. Extract the archive **directly into the OMSI installation root** (the folder that contains `Omsi.exe`). The archive is laid out for that root:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` and the other `OmsiLaunch.*.dll` controller assemblies, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` at the root;
   - the permanent plugin closure `plugins\OmsiLaunch.*` (9 files) beside your existing plugins, which are never touched;
   - `.omsilaunch\` with the splash assets, offline documentation and examples.
3. Keep `release-manifest.json` beside `OmsiLaunch.exe`. It is what lets every start verify the installed plugin files by SHA-256 (`plugin.integrity.reference = manifest`); without it only presence and self-consistency are checked (`plugin.integrity.reference = self`).
4. Do not move or rename anything under `plugins\OmsiLaunch.*` and do not place plugin binaries in `.omsilaunch\`.

Upgrading is the same operation: extract the new package over the old files while no session is running and no recovery is pending (`OmsiLaunch.exe /recovery-status`). The plugin hashes and the manifest must always come from the same package (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` otherwise).

## Verify

Run from the OMSI root (the installation argument defaults to the directory containing `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Expected: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; exit `0`. Exit `102`..`106` means the x64 .NET 6 runtime is missing or the package is incomplete.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Expected: the supported hashes, then the maps discovered in this installation; exit `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Expected: `Plan: READY profile=Omsi23004` (on the known layout; `unrecognized` otherwise) and exit `0` (use any map identity from `/list:Maps`; the entrypoint index must be a presented index of that map, see `/list:Entrypoints /map:<identity>`). With `--json` the plan lists `TouchedFiles` (the splash overlays), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED`, and `RESOLVED_KNOWN_LAYOUT` for the native start capabilities on the known layout), `Diagnostics` (including `plugin.integrity.reference`) and `IsRunnable`. `Plan: NOT RUNNABLE` with exit `1` names the reason in `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). Planning never starts OMSI and never writes to the installation.

## Where things live afterwards

| Path | Content |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Host trace of each session (50 newest sessions retained) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Tray indicator log |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Present only while a transaction is pending; see [transactions and recovery](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Your predefined session profiles; see [session profiles](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Managed splash assets |
| `<root>\.omsilaunch\docs\` | This documentation, offline (start at `README.md`; the CLI reference is `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Example LaunchSpec and session profile |

## Uninstall

Stop any session, run `OmsiLaunch.exe /recovery-status` (and `/recover` if pending), then delete the root product files, `plugins\OmsiLaunch.*` and `.omsilaunch\`. Details in [packaging](../reference/packaging.md).

## Next

[First session](first-session.md) · [CLI reference](../reference/cli.md) · [known limitations](../reference/known-limitations.md)
