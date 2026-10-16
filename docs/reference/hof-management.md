# HOF Management

OMSI reads the HOF files (`*.hof`: destinations, route numbers and stop lists shown by the vehicle's IBIS/destination display) from each vehicle's folder. HOF management copies HOF files into vehicle folders **for the duration of one session**: a map or add-on can ship the HOFs its routes need, and a user can reuse the HOFs of one vehicle on others, without permanently changing any vehicle. This page is the normative reference, as implemented by `HofPlanner` in `src/OmsiLaunch.Core/HofPlanner.cs`, the `HofManagement` field of the [LaunchSpec](launchspec.md), the `/hof-source`, `/hof-target`, `/hof-file` and `/hof-mode` [CLI](cli.md) flags and the `hofs` block of [session profiles](session-profiles.md).

Stability: `STABLE_BETA` for planning, conflict rules, confinement and transactional restore (offline gates `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Runtime validated: a mirror session with exact restore and crash recovery through `/recover` (see [runtime validation status](../status/runtime-validation-status.md)).

HOF management is not the same as `PlayerVehicle.Hof` / `/hof:<id>`, which selects the HOF of the player vehicle at start and is `UNAVAILABLE` in this build.

## Model

A request is a list of operations (`HofManagementSpec.Operations`). Each `HofOperationSpec` has:

| Field | Meaning |
| --- | --- |
| `SourceKind`, `Source` | Where the HOF files come from (table below). |
| `TargetVehicles` | One or more installed vehicles: the identity of a `.bus` or `.ovh` file (`Vehicles\MAN_SD200\MAN_SD77.bus`) or a vehicle folder (`Vehicles\MAN_SD200`), both below `Vehicles\`. Several identities in one folder address that folder once. |
| `Mode` | `Add` (default): copy the source HOFs into each target folder; other HOFs there stay. `Mirror`: copy the source HOFs and remove every other `.hof` of the target folder for the session, so the folder's HOF set equals the source set. |
| `Files` | Optional list of file names to take from the source (case-insensitive). Omitted: every `*.hof` directly in the source. A listed name that the source does not contain: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Resolves to |
| --- | --- | --- |
| `Collection` | a plain name, for example `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: a collection kept by the user or a tool. |
| `Directory` | an installation-relative directory, for example `maps\RMG Leste\hof` | `*.hof` in that directory: HOFs shipped next to map or add-on content. |
| `Vehicle` | a vehicle identity or folder, as for targets | `*.hof` in that vehicle's folder: reuse one vehicle's HOFs on others. |
| `File` | an installation-relative `.hof` file | that one file. |

Only files directly in the source directory are used (no recursion). Symbolic links and junctions are never followed (see [confinement](#confinement)).

## What a session does

1. **Planning** (`PlanSessionAsync`, `/plan`). Every operation is resolved: sources are read, targets are checked, conflicts are detected. Each file that will change appears in `SessionPlan.PlannedMutations` with `SemanticKey` `hof.add` or `hof.mirror`, `RequestedValue` = the source identity, and `Operation` `exact-file-overlay` (written) or `session-deletion` (removed by a mirror); the paths are part of `TouchedFiles`. A problem makes the plan non-runnable with the codes below.
2. **Start**. After a stale journal is recovered, the operations are resolved again from the live files and handed to the session transaction: written files are overlays, mirrored-away files are session deletions. A target file whose bytes already equal the source is left alone and is not a transaction entry.
3. **Session end, failure, crash.** The transaction restores every target folder exactly: files that existed get their original bytes, timestamps and attributes back; files the session created are removed; mirrored-away files come back. After an owner crash the same restore runs at the next start or through `/recover` (see [transactions and recovery](../concepts/transactions-and-recovery.md)).

Nothing outside the target folders is written. Source files are only read. `.omsilaunch\hof\` is never modified by a session.

## Conflict rules

Operations are applied in order and must agree:

- Two operations that write the same target file with the same bytes merge; with different bytes: `OL_E_HOF_CONFLICT`.
- A mirror that would remove a file that an earlier operation writes, or an operation that writes a file an earlier mirror removes: `OL_E_HOF_CONFLICT`.
- A mirror covers the folder's session HOF set: the files that exist plus the files earlier operations write there.
- A HOF target that is also written by the session presentation (splash or Internet Textures): `OL_E_HOF_CONFLICT` (cannot happen with the current presentation paths, which are outside `Vehicles\`).
- When a target already has a file with the same name in a different case, the existing spelling is kept.

## Confinement

Every source and target must stay inside the installation: rooted paths, `..` segments and paths that leave the root are `OL_E_HOF_PATH_ESCAPE`; so are targets (and `Vehicle` sources) outside `Vehicles\`, collection names that are not a plain directory name, and any source or target folder reached through a junction or symbolic link. `.hof` files that are themselves reparse points are ignored.

## Limits

| Limit | Value | Error |
| --- | --- | --- |
| Size of one HOF file | 8 MiB | `OL_E_HOF_INVALID` |
| Files per operation | 256 | `OL_E_HOF_INVALID` |
| Total bytes written per session | 256 MiB | `OL_E_HOF_INVALID` |
| Targets per operation | at least 1 | `OL_E_HOF_INVALID` |

## Error codes

| Code | Cause |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | The collection, directory, vehicle or file does not exist, contains no `.hof`, or lacks a file named in `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | A target vehicle file or folder does not exist. |
| `OL_E_HOF_PATH_ESCAPE` | A path leaves the installation, a target is not below `Vehicles\`, a collection name is not a plain name, or a junction/symlink is traversed. |
| `OL_E_HOF_CONFLICT` | Two operations disagree about one target file (see [conflict rules](#conflict-rules)). |
| `OL_E_HOF_INVALID` | No targets, no source, a `File` source that is not `.hof`, a target that is a file other than `.bus`/`.ovh`, or a [limit](#limits) exceeded. |

## Using it

Command line (one operation; several operations need `/spec` or a session profile):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (`/spec` JSON, enums as integers):

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

Session profile preset (a `package` source is a directory inside the profile package):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: set `LaunchSpec.HofManagement` and call `PlanSessionAsync` / `StartSessionAsync` as usual; `/list:Hofs` (`ContentQueryKind.Hofs`) lists installed HOF files.

Related: [LaunchSpec](launchspec.md), [session profiles](session-profiles.md), [CLI](cli.md), [errors](errors.md), [transactions and recovery](../concepts/transactions-and-recovery.md).
