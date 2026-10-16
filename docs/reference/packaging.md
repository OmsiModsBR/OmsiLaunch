# Packaging and Release Layout

This page describes the OmsiLaunch `0.1.0-beta4` release package: what `tools\New-ReleasePackage.ps1` produces, the fields of `release-manifest.json`, how the controller uses the manifest at runtime to verify the permanent plugin closure, how the package is installed into and removed from an OMSI installation root, what the `.omsilaunch` directory contains after use, and the validation scripts (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). Product identity comes from `OmsiLaunch.Version.props`. Installation steps for users are in [installation](../getting-started/installation.md); the plugin closure's runtime role is in [permanent plugin](../concepts/permanent-plugin.md).

## Product identity (`OmsiLaunch.Version.props`)

| Property | Value | Used for |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | Manifest `product`, Windows `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro` | Windows `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/lmonteirotech/OmsiLaunch` | Windows `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | Manifest `product_version`, Windows `ProductVersion`, assembly informational version (`/version`), public ZIP name |
| `OmsiLaunchManagedVersion` | `0.1.0` | Managed assembly version base |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Assembly and Windows file version |
| `OmsiLaunchPackageAlias` | `current` | Manifest `package_alias`, staging folder and alias ZIP name |

`Directory.Build.props` sets `InformationalVersion` to `OmsiLaunchProductVersion` without a source revision, so `OmsiLaunch.exe /version` prints exactly `0.1.0-beta4`.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (default output `artifacts\release`) stages a package from already built artifacts. Writing to `artifacts\release` when `OmsiLaunch-<product_version>.zip` already exists is refused unless `-AllowOverwritePublished` is given; candidate packages go to another directory (the offline validation uses `artifacts\candidate\post-round-a`).

Before staging, every build output is checked for staleness.

- **`OmsiLaunch.Native.x86.dll`: by content, not by timestamp.** The native build writes `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (target `WriteOmsiLaunchNativeBuildReceipt` in the `.vcxproj`): the SHA-256 of the DLL it produced (`output=`) and of every source it was built from (`source=<sha256>|<path>`: the `.cpp`, the `.rc`, the `.vcxproj` and `OmsiLaunch.Version.props`). Packaging refuses the DLL when its hash is not the recorded output (`does not match its build receipt`: a stale or foreign copy, whatever its timestamp), when a recorded source changed (`Native source changed after the recorded build`), when a native source is not covered by the receipt, or when the receipt is missing.
- **Shims and managed assemblies: by timestamp.** Each must not be older than the sources of its own project (`.cpp`/`.rc`/`.vcxproj` of each shim; each managed assembly's own project). A stale input aborts packaging with `Stale build artifact`.

The package is then assembled in a brand-new, uniquely named staging directory (`.staging-<guid>` in the output directory), so no file from an earlier run can enter the closure. The previous `OmsiLaunch-current` folder and archives are replaced only after every check below has passed.

| Source | Destination in package |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | root |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| CLI `assets\splash\*.bmp` (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | root |
| every file under `docs\` except `docs\localized\` (the English documentation, same directory structure) | `.omsilaunch\docs\` (so `.omsilaunch\docs\reference\cli.md`, the path printed by the CLI usage text, exists; documentation audit BUG-08). Links from `docs\README.md` to the repository-root summaries (`PUBLIC-API.md` and others) resolve only in the source repository. |
| `docs\localized\LOCALIZATION-MANIFEST.md` and `docs\localized\<locale>\**` for each locale listed in that manifest (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` and `ja-JP`) | `.omsilaunch\docs\localized\` (same structure); a listed locale that is missing aborts the script |

Then it hashes every staged file, writes `release-manifest.json` at the package root as UTF-8 **without** a BOM (the result no longer depends on the PowerShell edition), runs `Test-ReleasePackageIntegrity.ps1` on the stage, re-compares every staged plugin file and `OmsiLaunch.Native.x86.dll` with its build output, compresses the stage, **extracts the archive into a fresh temporary directory and validates the extracted closure against the same manifest** (the archived manifest must be byte-identical to the validated one), then publishes the stage as `OmsiLaunch-current`, the archive as `OmsiLaunch-current.zip`, copies it to `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) and writes `OmsiLaunch-0.1.0-beta4.zip.sha256` containing `<SHA-256>  <file name>`. Any missing artifact aborts the script. The script does not build; run `Invoke-OfflineValidation.ps1` (or the individual `dotnet build` / MSBuild steps) first.

## Package layout

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Only the root product files, `plugins\OmsiLaunch.*` and `.omsilaunch\` are product-owned. Third-party plugins under `plugins\` are never enumerated, copied, hashed, removed or restored by OmsiLaunch.

## `release-manifest.json`

| Field | Type | Meaning |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`; must match `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | `build_id` and `code_sha256` of each known layout (`Omsi23004`, `13ec110a...c028`); a fast path, not a requirement |
| `configuration` | string | `Release` or `Debug` |
| `generated_utc` | string | ISO-8601 build time |
| `files[]` | object[] | `path` (forward slashes, relative to the package root), `bytes`, `sha256` (upper-case hex) for every packaged file |

The manifest is data, never executable policy: the controller reads only the `plugins/` entries. The reader accepts the file with or without a UTF-8 BOM (manifests written by Windows PowerShell 5.1 before this correction carry one).

## Runtime use of the manifest (plugin integrity)

Before every plan and start, `OmsiLaunchService.LoadArtifacts` builds the expected plugin closure (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. The controller looks for `release-manifest.json` beside `OmsiLaunch.exe` (`AppContext.BaseDirectory`). When present, `ReleaseManifest.TryReadPluginHashes` extracts the `plugins/*` hashes (`OL_E_RELEASE_MANIFEST_INVALID` if the file cannot be read as a manifest).
2. Each installed file `<root>\plugins\OmsiLaunch.*` is hashed (SHA-256) and compared:
   - with a manifest: against the manifest hash; the plan diagnostic `plugin.integrity.reference = manifest` is recorded. Missing file → `OL_E_PERMANENT_PLUGIN_MISSING`; file present but not listed → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; hash differs → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - without a manifest (development layout, or an installation that omitted the manifest): only presence and self-consistency against the packaged copy beside the controller can be checked; `plugin.integrity.reference = self`.
3. A failure marks the plan not runnable (`OL_E_RUNTIME_ARTIFACT_MISSING` with the detail) or rejects the start (exit `7`).

Plugin files are never staged, snapshotted, restored or removed by a session; the closure is a permanent part of the installation. The x86 `OmsiLaunch.Native.x86.dll` is loaded from `plugins\` only; the managed assemblies declare `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

## Installation into the OMSI root

1. Verify the archive: compare `OmsiLaunch-0.1.0-beta4.zip` against `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Extract the archive **directly into the OMSI installation root** (the directory containing `Omsi.exe`). This places the root files, `plugins\OmsiLaunch.*` (beside any third-party plugins) and `.omsilaunch\`.
3. Keep `release-manifest.json` beside `OmsiLaunch.exe`: it enables manifest-based plugin integrity. The manifest and the binaries must come from the same package: new binaries over an older manifest (or the reverse) make every start fail with `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` now copies the manifest together with the product files; it used to skip it, which left an older manifest beside newer binaries (Round A RA-007).
4. Check coherence read-only with `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` must be `true`.
5. Do not move plugin binaries into `.omsilaunch\` and do not rename `plugins\OmsiLaunch.*`.
6. Verify with `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` and a `/plan` (see [first session](../getting-started/first-session.md)).

Existing `.omsilaunch\assets\splash\*.bmp` files are never overwritten by a session (an explicitly managed asset set persists); overwriting them by extracting a new package is a deliberate user action.

## The `.omsilaunch` directory after use

| Path | Created by | Lifetime |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | package, or copied on first managed-splash session | persistent |
| `docs\`, `examples\` | package | persistent |
| `session-profiles\<id>\profile.yaml` | user | persistent; see [session profiles](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | every session | retained for the 50 newest sessions; older session-prefixed files are deleted when a new session starts |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, validation harnesses | same retention (session prefix) |
| `diagnostics\tray-host.log` | tray indicator | persistent, appended |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | persistent (not session-prefixed) |
| `journal.json` | transaction | exists from `Prepared` until `Restored`; a leftover means recovery is pending (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | transaction | snapshots of touched files; removed after restore |

No data leaves the machine. See [transactions and recovery](../concepts/transactions-and-recovery.md).

## Uninstall

1. Make sure no session is running (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) and no recovery is pending (`OmsiLaunch.exe /recovery-status`; run `/recover` if `pending` is `true`) so OMSI files are already restored.
2. Delete `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Leave other plugins untouched.
3. Delete the root product files listed in the layout above (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Delete `.omsilaunch\` (this removes your session profiles and diagnostics). Never delete it while `journal.json` exists.

A completed session restores every file it owned, so no further cleanup is needed. Files OMSI itself writes while it runs (for example `[last_map]` in `options.cfg`, caches, `laststn.osn`, logs) are OMSI's normal state and are not reverted; see [transactions and recovery](../concepts/transactions-and-recovery.md).

## Validation scripts

| Script | Purpose | Touches OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Builds `OmsiLaunch.sln` with warnings as errors, the three native projects (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) through MSBuild, then runs every offline suite: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests`, and `OmsiLaunch.DocumentationTests` unless skipped, then the packaging regression `Test-PackagingPipeline.ps1` (skipped with `-SkipNative`). Prints `OFFLINE VALIDATION PASSED`/`FAILED`. | No |
| `tools\New-ReleasePackage.ps1` | Staleness guard, stage, manifest, integrity self-check, ZIP, checksum (above). | No |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifies that the manifest lists exactly the packaged files with matching size and SHA-256, that the required closure (three executables, the controller, the nine permanent plugin files including `OmsiLaunch.Native.x86.dll`) is present and that the configuration is `Release`. With `-InstallationRoot` it compares the installation's product files with the package **read-only**. Exit `0` = coherent. | No (read-only) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produces a candidate package from clean staging under `artifacts\candidate\post-round-a` and requires the staged closure and the re-extracted archive to pass integrity. Proves the integrity gate rejects a tampered DLL, a tampered or old `Native.x86`, a stale plugin copy, a deleted listed file, an unexpected file, a changed or malformed manifest hash, duplicate entries (exact, by case, by separator), parent and absolute paths and invalid JSON; that the packager rejects an old `Native.x86` placed in the build output (even with a newer timestamp) and a receipt whose sources changed; and that the published archive is never overwritten. Build outputs are restored byte for byte. Run by `Invoke-OfflineValidation.ps1`. | No |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Extracts the ZIP to `artifacts\release\identity-verification`, checks `product`/`product_version`/`package_alias`, and verifies `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` of every `.exe`/`.dll` except `nethost.dll` and `YamlDotNet.dll`, the `InternalName`/`OriginalFilename` of both shims, and that `OmsiLaunch.exe` carries an embedded icon. | No |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Validates the packaged Release executable against a real installation: manifest must be `Release`, contain no `Debug` or `runtime/plugin/` paths and install `plugins/OmsiLaunch.*`; the four splash assets must exist. Runs three `/plan` cases (managed default, managed custom assets, `/splash:Unset`). With `-RunOmsi` (requires `-InstallPackage`) it launches each case with `/observe-seconds`, watches `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_PTB.bmp` during the session, and asserts exact restore, no `Omsi.exe`, no `journal.json`, exit `0`, unchanged third-party plugin hashes and an unchanged permanent plugin set. Writes `.omsilaunch\diagnostics\release-presentation-validation.json`. | Yes with `-RunOmsi` (session-scoped, restored) |

Both `Test-*` scripts read `OmsiLaunch.Version.props` to know the expected version.
