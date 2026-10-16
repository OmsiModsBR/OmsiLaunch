# OmsiLaunch Documentation

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> British English edition of the [canonical page](../../README.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This is the normative English documentation for OmsiLaunch `0.1.0-beta4`, the
post-hardening baseline. OmsiLaunch provides programmable launch, session
ownership and runtime control for an installed OMSI 2; each native capability
is resolved and verified per build (see [compatibility](reference/compatibility.md)). Every page under `docs/` describes what the current code
does; when a page and the code disagree, the code wins and the page is a bug.

Stability vocabulary used throughout: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Flags that are parsed but do nothing are marked
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Nothing is called
runtime-validated unless the
[runtime validation status](status/runtime-validation-status.md) says so.

## Who reads what

| Audience | Start here | Then |
| --- | --- | --- |
| Users (CLI, shortcuts, session profiles) | [Installation](getting-started/installation.md), [First session](getting-started/first-session.md) | [CLI reference](reference/cli.md), [CLI examples](reference/cli-examples.md), [Session profiles](reference/session-profiles.md), [Windows tray](reference/windows-tray.md), [Exit codes](reference/exit-codes.md) |
| Integrators (`OmsiLaunch.Api`, local IPC) | [Public API quick start](getting-started/api-quick-start.md), [Public API reference](reference/public-api.md), [LaunchSpec reference](reference/launchspec.md) | [Session lifecycle](concepts/session-lifecycle.md), [Runtime control](reference/runtime-control.md), [Capabilities](reference/capabilities.md), [Local control / IPC](reference/local-control.md), [Error reference](reference/errors.md) |
| Maintainers (release, validation, boundaries) | [Packaging](reference/packaging.md), [Permanent plugin model](concepts/permanent-plugin.md) | [Transactions and recovery](concepts/transactions-and-recovery.md), [Compatibility](reference/compatibility.md), [Known limitations](reference/known-limitations.md), [Runtime validation status](status/runtime-validation-status.md) |

## Navigation

| Page | Purpose |
| --- | --- |
| [Getting Started](getting-started/first-session.md) | Plan, start, observe and stop one session from the OMSI root. |
| [Installation](getting-started/installation.md) | Prerequisites, extracting the package into the OMSI root, verifying with `/version`, clean removal. |
| [Public API Quick Start](getting-started/api-quick-start.md) | A complete .NET program that plans, starts, reads and stops one session. |
| [HOF Management](reference/hof-management.md) | Session-scoped HOF copy and mirror into vehicle folders: sources, targets, conflicts, restore. |
| [Runtime Modules](../../reference/runtime-modules.md) | Modules that run inside OMSI with a session: manifest, trust, lifecycle, failure policies, operations. |
| [CLI Reference](reference/cli.md) | Every flag, command word and hierarchical route of `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [CLI Examples](reference/cli-examples.md) | Copy-and-paste command lines for common tasks. |
| [LaunchSpec Reference](reference/launchspec.md) | Every `LaunchSpec` property and enum value, JSON loading rules for `/spec`. |
| [Session Profiles Reference](reference/session-profiles.md) | `profile.yaml` schema `omsilaunch.session-profile/v1`, keys, limits, precedence. |
| [Public API Reference](reference/public-api.md) | `IOmsiLaunch`, public records and enums, stability per member. |
| [Public API Inventory](reference/public-api-inventory.md) | Generated list of every public type and member with signature and stability. |
| [Runtime Control](reference/runtime-control.md) | Runtime command channel, timeouts, handles, stop semantics. |
| [Capabilities Reference](reference/capabilities.md) | Capability catalogue and every public runtime operation id with its classification. |
| [Session Lifecycle](concepts/session-lifecycle.md) | `SessionState` transitions, what `StartSessionAsync` promises, how a session ends. |
| [Transactions and Recovery](concepts/transactions-and-recovery.md) | Journal states, backups, restore verification, session deletions, crash recovery. |
| [Permanent Plugin Model](concepts/permanent-plugin.md) | The `plugins\OmsiLaunch.*` closure, manifest-based integrity, what a session never touches. |
| [Local Control / IPC](reference/local-control.md) | Named-pipe protocol `0.1`, per-installation endpoint, `session_id` binding, trust model. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | The Windows (no console) host: differences from `OmsiLaunch.exe`, `/silent`, dialogues, exit codes. |
| [Windows Tray](reference/windows-tray.md) | Notification-area indicator: icon, menu, status window field by field, End session, Explorer restart. |
| [Error Reference](reference/errors.md) | Every `OL_E_*` / `OL_W_*` code with category and meaning. |
| [Exit Codes](reference/exit-codes.md) | `PublicExitCode` values 0 to 10 and bootstrapper shim codes 100 to 106. |
| [Packaging / Installation Layout](reference/packaging.md) | Files in the release ZIP, `release-manifest.json`, `.omsilaunch\` layout. |
| [Compatibility / Supported OMSI Builds](reference/compatibility.md) | The one supported `Omsi.exe` hash, the accepted Steam LAA hash, platform requirements. |
| [Known Limitations](reference/known-limitations.md) | What is unsupported, partial or an accepted risk in this beta. |
| [Runtime Validation Status](status/runtime-validation-status.md) | What ran under OMSI, what only ran offline, what still needs a real session. |

Pages that remain normative for maintainers: the repository
[`README.md`](../../../README.md), the specifications under `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) and the project pages under
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). They summarise; the
pages above are the detailed reference. Historical pages are listed in the
[documentation manifest](../../DOCUMENTATION-MANIFEST.md).

## How this documentation is kept in sync

A documentation gate, `tests\OmsiLaunch.DocumentationTests`, compiles against
`OmsiLaunch.Api` and `OmsiLaunch.Core` and compares the pages above with the
code that defines the public surface:

| Gate | Checks |
| --- | --- |
| `docs.cli-flags` | Every `CliInput.KnownFlags` entry appears in the CLI reference as `` `/flag` `` or `` `/flag:` ``; every `CliInput.AcceptedNoEffectFlags` entry is marked `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` on its line; every command word and every `CliInput.HierarchicalRoutes` route appears with its runtime operation; every `PublicExitCode` value has a `| n |` row in the exit-code table. |
| `docs.capabilities` | Every `PublicCapabilityRegistry.All` id, every `PublicCapabilityRegistry.PublicRuntimeOperationIds` entry and every `PublicCapabilityClassification` name appears in the capabilities reference. |
| `docs.errors` | Every `PublicErrorCodes.All` code appears in the error reference, and no `OL_E_*` / `OL_W_*` literal in `src\` or `tools\OmsiLaunch.Cli\` is missing from `PublicErrorCodes`. |
| `docs.public-api` | Every exported type of `OmsiLaunch.Api`, every enum value and every `IOmsiLaunch` member appears in the public API reference, and all five stability words are used. |
| `docs.launchspec` | Every public property reachable from `LaunchSpec` and every value of its enums appears in the LaunchSpec reference. |
| `docs.session-profiles` | Every `SessionProfileCompiler.SchemaKeys` key, the schema identifier and the `256 KiB` limit appear in the session-profile reference. |
| `docs.structure` | Every page in the navigation table exists. |
| `docs.links` | Every relative link in `docs\**\*.md` (excluding `docs\localized\`) and in the root `*.md` files resolves to a file or directory. |
| `docs.localization` | Every locale listed in `docs\localized\LOCALIZATION-MANIFEST.md` has every page of the localised set; each page keeps the English page's headings, tables and code blocks, every inline code span (flags, capability and operation ids, error codes, keys, identifiers) and every link, and its relative links resolve. |

The gate is one of the suites run by `tools\Invoke-OfflineValidation.ps1`
(skip it with `-SkipDocs`). It runs offline, never launches OMSI, and fails the
build when a flag, route, capability, error code, enum value or public type is
undocumented or a link is broken. It does not check prose, so a page can still
be wrong about behaviour; report that as a bug against the page.

## Translations

`docs\localized\<locale>\` holds complete translations of this `0.1.0-beta4`
documentation for `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` and `ja-JP`. The page set, the locale roots and the
pages that are intentionally not translated are listed in
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
Translations keep every command, flag, identifier, error code and example of
the English pages unchanged, and the `docs.localization` gate checks that.
The English pages remain the normative source: where a translation disagrees
with them, the English page and the code are authoritative, and the translation
is a bug.
