# Compatibility

<!-- l10n: source=reference/compatibility.md source-sha256=9f3a7654d57410845e20c92eccc746868cbac665b4d937581f16c3818d7b7b88 -->
> British English edition of the [canonical page](../../../reference/compatibility.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

OmsiLaunch works with an installed **OMSI 2**. It does not require one particular `Omsi.exe` file: every native assumption it makes inside OMSI (a function, a patch site, a class, a VMT slot, a global, a field offset) is resolved and verified individually, in process, before it is used, and a capability is available only when all of its assumptions are verified. This page states what "OMSI 2 installed" means, how builds are identified, how capabilities are resolved, and the operating-system and runtime requirements. Sources: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

## Requirement: OMSI 2 installed

A plan is refused with `OL_E_UNSUPPORTED_BUILD` only when the installation is not an OMSI 2 installation:

| Check | Refused when |
| --- | --- |
| `Omsi.exe` | missing from the installation root |
| Image | not a 32-bit x86 PE image |
| Version | its version resource does not carry an OMSI 2 version (2.x) |
| Installation | the root has no `maps\` or `Vehicles\` directory |

The executable's SHA-256, size and exact version are **not** compatibility criteria. They are recorded as diagnostics (`omsi.build.detected`, `OmsiLaunch.exe profiles`) and serve as evidence.

## Build identity and capability resolution

| Concept | Meaning |
| --- | --- |
| Code identity | SHA-256 of the executable's main code section. A known identity selects a known layout as a fast path. Variants that differ only in headers (for example a Large Address Aware copy) share the code identity. |
| Known layout | `Omsi23004`: OMSI 2.3.004 (file version 2.2.032), code identity `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Every runtime validation in the [status page](../status/runtime-validation-status.md) ran on this layout. |
| Native target | One assumption of the native boundary, declared in the build catalogue with its guard. |
| Capability | A group of targets, for example `world.new-map` or `time.read`. Available only when every target is resolved **and** verified. |

How a target is resolved, in the plugin, before OmsiLaunch patches anything:

1. **Known identity:** the reference address of the known layout is tried first, and it must still pass the target's guard.
2. **Otherwise, or when the guard fails**, build-independent strategies are tried:
   - Delphi RTTI class name (`vmtSelfPtr` checked);
   - published methods, fields and properties of a class (RTTI tables);
   - VMT slot holding a resolved method;
   - a masked code signature that must match exactly once (operands that depend on the layout are wildcards), capturing an address, a global (directly or through its pointer cell) or a field displacement from inside a method of the field's own class.
3. **Nothing matches uniquely and passes the guard:** the target stays unresolved. There is **no fallback to a fixed address**.

Guards: masked entry bytes for functions and patch sites, the call destination for a patched call, `vmtSelfPtr` and the RTTI class name for classes, the held method for VMT slots, and a writable data section for globals. Object layouts read by the runtime readers (vehicle, human, weather, timetable records and so on) have no build-independent structural validation yet. They are verified only for a known code identity, so the capabilities that read them are unavailable on a build with an unknown identity.

On the known layout every capability resolves. On a build with an unknown identity, the capabilities whose targets resolve through the strategies above (the headless start, NEW_MAP, saved situations, Internet Textures suppression, time read and set, D3D) can be available, and each is verified individually; layout-dependent reads are unavailable.

## Where compatibility is decided

| Stage | What happens |
| --- | --- |
| Planning (`PlanSessionAsync`, `/plan`) | OMSI 2 check (`omsi.omsi2`, `OL_E_UNSUPPORTED_BUILD` otherwise). The file image is resolved as a **prediction**: required native capabilities carry `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED`, or `PENDING_IN_PROCESS_RESOLUTION` with warning `OL_W_CAPABILITY_PENDING`. An unknown code identity adds warning `OL_W_BUILD_UNRECOGNIZED`. Warnings do not make a plan non-runnable. |
| Start (`StartSessionAsync`) | The spec is re-planned; a plan that is no longer runnable is rejected with `OL_E_PLAN_NOT_RUNNABLE`. |
| In process (plugin start) | The loaded image is resolved (authoritative; a DRM-protected executable can differ from its file). The report is sent as telemetry `native.capabilities`. If `boot.headless-start` and the world-mode capability (`world.new-map` or `world.saved-situation`) are not available, the session fails with `OL_E_CAPABILITY_UNRESOLVED` before any hook, and the files are restored. If they are available but the live Start form is not in the expected state (for example another plugin patched it), the session fails with `OL_E_BUILD_VALIDATION_FAILED`. |
| Runtime operations | An operation whose capability is unavailable is rejected with `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) without touching OMSI memory. |

`OmsiLaunch.exe profiles --json` reports the installation (`is_omsi2`, `build_id`, `identity`, file and code hashes, `file_variant`), the known layouts, and the capabilities predicted from the file.

## Operating system and architecture

`CurrentWindowsX64Platform.Detect` computes `RuntimePlatformInfo`. The current platform is supported only when all of the following hold:

| Requirement | Check | Error when violated |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 or later | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-bit Windows and a 64-bit host process | `OSArchitecture == X64` and `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Writable installation | Root directory exists, is not read-only, and contains `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` also reports `OmsiArchitecture` and `PluginArchitecture` as `X86` (OMSI is a 32-bit process; the plugin closure is x86 and runs under WOW64), `LegacyPlatform=false`, and `Wow64Available`. ARM64 Windows is not supported even where x64 emulation exists, because the host process must itself be x64.

## .NET requirements

| Component | Runtime | Notes |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | The native bootstrapper locates the runtime through `hostfxr` via the packaged `nethost.dll`. A missing runtime is reported by the shim (exit codes 100-106; see [CLI](cli.md) and [exit codes](exit-codes.md)). |
| Plugin closure (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hosted by DNNE 2.0.6 inside `Omsi.exe` | Requires the x86 .NET 6 Desktop/Core runtime to be installed on the machine; the 64-bit runtime alone is not sufficient for the plugin. |
| Native bridge (`plugins\OmsiLaunch.Native.x86.dll`) | native x86 | Loaded only from `plugins\` (see [permanent plugin](../concepts/permanent-plugin.md)). It contains no OMSI address: it receives the verified target table from the plugin. |

## Legacy platforms

Windows 7, Windows 8.x, Windows XP and other NT 6 and earlier systems are outside the current support boundary. `RuntimePlatformInfo.LegacyPlatform` is always `false` and no legacy adapter exists; the field and the `IPluginNativeServices` seam exist only so that a future legacy adapter could be added without changing the public API (see `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nothing in this release runs on those systems.

## Steam and Large Address Aware notes

- A Large Address Aware copy that differs only in its headers has the same code identity as its original and uses the same known layout.
- The Steam executable is DRM-protected on disk, so its file can differ from its loaded image: the plan may report `OL_W_CAPABILITY_PENDING`, and the in-process resolution decides.
- Steam launches OMSI itself; a session must be started through `OmsiLaunch.exe` so that the handoff exists. Started from Steam, the permanent plugin stays inert (no handoff, no hooks).

## Related pages

- [Known limitations](known-limitations.md)
- [Runtime validation status](../status/runtime-validation-status.md)
- [Installation](../getting-started/installation.md)
