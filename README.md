<p align="center">
  <img src="assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Session control for OMSI 2.</strong></p>
<p align="center">Open source · Programmable · Community driven</p>

<p align="center">
  <strong>English (US)</strong> ·
  <a href="docs/readme/README.en-GB.md">English (UK)</a> ·
  <a href="docs/readme/README.pt-BR.md">Português (Brasil)</a> ·
  <a href="docs/readme/README.pt-PT.md">Português (Portugal)</a> ·
  <a href="docs/readme/README.fr-FR.md">Français</a> ·
  <a href="docs/readme/README.de-DE.md">Deutsch</a> ·
  <a href="docs/readme/README.es-ES.md">Español (España)</a> ·
  <a href="docs/readme/README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="docs/readme/README.it-IT.md">Italiano</a> ·
  <a href="docs/readme/README.pl-PL.md">Polski</a> ·
  <a href="docs/readme/README.nl-NL.md">Nederlands</a> ·
  <a href="docs/readme/README.ru-RU.md">Русский</a> ·
  <a href="docs/readme/README.zh-CN.md">简体中文</a> ·
  <a href="docs/readme/README.zh-TW.md">繁體中文</a> ·
  <a href="docs/readme/README.ja-JP.md">日本語</a>
</p>

---

# OmsiLaunch

**OmsiLaunch** is an open-source layer for launching OMSI 2 programmatically,
managing sessions and controlling the running simulation. It plans a session
from a declarative description, applies every temporary configuration change
inside a journaled transaction, starts OMSI, observes it until gameplay, lets
tools read and change the running simulation through a public API, and restores
every file it touched when the session ends.

It is infrastructure for launchers, tools, automation and community
integrations. It is not a graphical launcher.

> **Define the session, not the clicks.**

## Status: 0.1.0-beta4

The current public beta is **`0.1.0-beta4`**. Beta 4 adds per-capability
compatibility, Runtime Modules, session profiles 1..99 and a set of start and
runtime features. Compatibility, the capability catalog, pagination, typed
errors and the modules bridge were observed in live OMSI sessions (validation
rounds A and B). The features added after those rounds are `EXPERIMENTAL`:
closed by static analysis and tested offline only, with runtime validation
pending. Some remain `STATICALLY_VALIDATED`, `PARTIAL` or `UNAVAILABLE`.
Two runtime items are still open because they cannot be produced safely: Steam
LAA gameplay and the natural removal of road vehicles and humans (RV-002). The
[runtime validation status](docs/status/runtime-validation-status.md) page is
the authoritative record of what ran under OMSI and what ran only offline.

It is a beta: the public API, CLI and file formats are marked `STABLE_BETA`,
`EXPERIMENTAL`, `PARTIAL`, `INTERNAL` or `UNAVAILABLE` per member and may still
change before 1.0.

## Supported OMSI scope

OmsiLaunch requires an installed OMSI 2. It does not depend on one particular
`Omsi.exe` file: every native capability is resolved and verified individually
inside OMSI before it is used, and a capability that cannot be verified on a
build is unavailable on that build.

| Item | Scope |
| --- | --- |
| Requirement | OMSI 2 installed: a 32-bit `Omsi.exe` with an OMSI 2 (2.x) version and the `maps\` and `Vehicles\` directories. Anything else is refused with `OL_E_UNSUPPORTED_BUILD`. |
| Known layout | OMSI 2.3.004 (`Omsi23004`). Every capability resolves, and every runtime validation ran on this layout. Hashes identify it as a fast path; they are not a requirement. |
| Other OMSI 2 builds | Each capability is resolved by build-independent strategies (Delphi RTTI, unique code signatures, VMT slots) and verified. Capabilities that read object layouts need a known layout. |
| Steam executable | Resolved in process; a genuine Steam session has **not** been runtime validated. |
| Operating system | Windows 10 or later, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (controller) and .NET 6 Runtime **x86** (the plugin runs inside the 32-bit `Omsi.exe`). |

Details: [Compatibility](docs/reference/compatibility.md) and
[Installation](docs/getting-started/installation.md).

## What it provides

**Sessions.** A session is planned from a `LaunchSpec` (CLI flags, a JSON file,
a Session Profile or the API), validated without side effects (`/plan`), then
started, observed through `SessionState` transitions and ended. Stopping a
session terminates OMSI by force, so OMSI cannot write over the files that are
about to be restored. See [Session lifecycle](docs/concepts/session-lifecycle.md).

**Transactions and recovery.** Every configuration override is scoped to the
session. OmsiLaunch snapshots, journals, applies, verifies and restores each
file it changes, including after a crash (`/recovery-status`, `/recover`,
`RecoverPendingAsync`). It offers no permanent configuration editing. See
[Transactions and recovery](docs/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** A reference frontend over the same public API, with
no OMSI logic of its own: discovery, planning, starting sessions, and client
commands against a running session. See the [CLI reference](docs/reference/cli.md)
and [CLI examples](docs/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** The Windows-subsystem host for shortcuts. It takes the
same command line with no console window and reports failures in message boxes.
See [OmsiLaunchW.exe](docs/reference/omsilaunchw.md).

**Windows tray.** Every owner session shows a notification-area icon with a
status window and an "End session" action. The action takes the same stop path
as `session stop`. See [Windows tray](docs/reference/windows-tray.md).

**Session Profiles.** Declarative `profile.yaml` packages
(`omsilaunch.session-profile/v1`) under
`.omsilaunch\session-profiles\<id>\`, so that content authors can ship
reproducible sessions that start with one command. See
[Session Profiles](docs/reference/session-profiles.md).

**Public API and runtime control.** `OmsiLaunch.Api` (`IOmsiLaunch`) is the
preferred product surface. Runtime operations such as time, weather, map,
camera, timetable, vehicles, humans, script variables and D3D textures are
session-scoped, validated against the build profile, and addressed through opaque
semantic handles, never native pointers. Results are bounded by a 64 KiB
runtime slot. See the [Public API reference](docs/reference/public-api.md) and
[Runtime control](docs/reference/runtime-control.md).

**Local control.** A per-installation named pipe bound to the active
`session_id` lets other same-user processes read status and events, stop the
session and run public runtime operations. See
[Local control / IPC](docs/reference/local-control.md).

**Capabilities.** Every capability and public runtime operation is cataloged
with its stability. Experimental and unavailable capabilities are listed, not
hidden (`OmsiLaunch.exe capabilities`). See
[Capabilities](docs/reference/capabilities.md).

**Permanent plugin.** The in-process plugin closure is installed once under
`plugins\OmsiLaunch.*`. Before every start it is checked against the SHA-256
entries of `release-manifest.json`. Third-party plugins are never touched. See
[Permanent plugin model](docs/concepts/permanent-plugin.md).

## Quick start

Extract the release package into the OMSI 2 installation root, then run the
following commands from that directory:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

While that session runs, a second console in the same directory can query or
end it:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Walkthrough: [First session](docs/getting-started/first-session.md). For .NET
integrators: [Public API quick start](docs/getting-started/api-quick-start.md).

## Documentation

The full documentation set is indexed in [`docs/README.md`](docs/README.md).
The English (US) pages under `docs/` are canonical and normative.

| Topic | Page |
| --- | --- |
| Public API | [docs/reference/public-api.md](docs/reference/public-api.md) |
| CLI | [docs/reference/cli.md](docs/reference/cli.md) |
| CLI examples | [docs/reference/cli-examples.md](docs/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/reference/omsilaunchw.md](docs/reference/omsilaunchw.md) |
| Windows tray | [docs/reference/windows-tray.md](docs/reference/windows-tray.md) |
| Session Profiles | [docs/reference/session-profiles.md](docs/reference/session-profiles.md) |
| Runtime control | [docs/reference/runtime-control.md](docs/reference/runtime-control.md) |
| Local control / IPC | [docs/reference/local-control.md](docs/reference/local-control.md) |
| Capabilities | [docs/reference/capabilities.md](docs/reference/capabilities.md) |
| Errors and exit codes | [docs/reference/errors.md](docs/reference/errors.md), [docs/reference/exit-codes.md](docs/reference/exit-codes.md) |
| Packaging | [docs/reference/packaging.md](docs/reference/packaging.md) |
| Known limitations | [docs/reference/known-limitations.md](docs/reference/known-limitations.md) |
| Runtime validation status | [docs/status/runtime-validation-status.md](docs/status/runtime-validation-status.md) |

### Documentation in other languages

The documentation is translated into 14 locales under
[`docs/localized/`](docs/localized/LOCALIZATION-MANIFEST.md). The translations
were produced from the canonical English (US) pages and validated mechanically
against them. Native-speaker editorial review was not part of the Beta 4 release
and may follow after publication. When a translation and the English page
disagree, the English page is authoritative.

## Limitations and open items

These are the most important limitations. The complete list is in
[Known limitations](docs/reference/known-limitations.md).

- **Steam.** Steam LAA is `PARTIAL`: gameplay, reads, commands and stop
  need a genuine Steam installation and were not runtime validated.
- **Experimental in this beta:** starting from the last map state (`/last`)
  and explicit date, time, year or weather at start are closed statically and
  await runtime validation. **Not available:** player-vehicle assignment at
  start; requesting it makes the plan not runnable instead of being silently
  ignored.
- **Runtime writes are limited.** `weather.set`, `weather.metar.set`,
  string-variable writes and script triggers are experimental until runtime
  validated; calendar writes and vehicle relocation are unavailable. Runtime
  changes are not journaled and are not restored.
- **Handle lifetime.** Stale detection for road vehicles and humans that
  disappear naturally (RV-002) has no safe runtime producer and is validated
  offline only.
- **Paged results.** Runtime lists are paged with `cursor` and `limit` and
  continue at `next_cursor`; a page that does not fit the 64 KiB mailbox is
  shortened and flagged `truncated=true`.
- **Stop is forced by default.** OMSI is terminated, its own shutdown does not
  run, and unsaved OMSI state is lost. The opt-in cooperative shutdown
  (`/cooperative-shutdown`, experimental) runs OMSI's exit routine first.
- **Same-user trust model.** Any process of the same Windows user can reach the
  local control plane.

## Download

Download **`OmsiLaunch-0.1.0-beta4.zip`** and its `.sha256` file from the
[Releases page](https://github.com/OmsiModsBR/OmsiLaunch/releases). Extract
it directly into the supported OMSI root. The package contains the controller
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), its dependencies, the permanent plugin
closure with `release-manifest.json`, splash assets, a session example and the
offline documentation under `.omsilaunch\docs\`. Package layout and clean
removal: [Packaging](docs/reference/packaging.md).

## Building from source

Requirements:

- Windows 10 or later, x64.
- .NET 6 SDK, with the x64 and x86 .NET 6 runtimes for running the tests.
- Visual Studio with the MSBuild C++ workload (platform toolset `v145`) and a
  Windows 10 SDK, for the three native projects: `OmsiLaunch.Native.x86`
  (Win32), and `OmsiLaunch.Bootstrapper` and `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` holds the managed projects and test suites. The single
offline entry point builds everything and runs every offline suite; it never
launches OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` skips the native projects and the packaging regression.
`-SkipDocs` skips the documentation gates. Build output goes to `artifacts\`,
which is not tracked. `tools\New-ReleasePackage.ps1` stages, hashes, validates
and compresses a release package from an existing build. See
[Packaging](docs/reference/packaging.md) and
[Testing and validation](docs/project/TESTING-AND-VALIDATION.md).

| Path | Content |
| --- | --- |
| `src/` | Product libraries: API, core, configuration, content, interop, process, plugin, build profile, native x86 boundary |
| `tools/` | CLI and Windows host (`OmsiLaunch.Cli`), native shims (`OmsiLaunch.Bootstrapper`), offline test host, packaging and validation scripts, localization tooling |
| `tests/` | Unit, integration, profile, Windows UI and documentation test suites |
| `docs/` | Canonical documentation and its translations under `docs/localized/` |
| `examples/` | LaunchSpec and session examples |
| `assets/` | Branding, icons and package assets |
| `third_party/` | Upstream provenance notes |

Maintainer summaries: [PUBLIC-API.md](docs/specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](docs/specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](docs/specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](docs/specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](docs/project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](docs/project/POST-RELEASE-BACKLOG.md).

## Community and license

OmsiLaunch is a community-oriented open-source project. It is independent of
other OMSI launchers, and compatible community tooling can build on it.

OmsiLaunch is licensed under [LGPL-3.0-only](LICENSE). See the
[third-party notices](THIRD-PARTY-NOTICES.md) for the provenance of
incorporated source and the applicable notices.
