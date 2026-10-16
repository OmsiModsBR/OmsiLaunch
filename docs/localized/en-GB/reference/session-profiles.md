# Session Profiles

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> British English edition of the [canonical page](../../../reference/session-profiles.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

A session profile is a declarative YAML package that a content author ships with a map or an add-on so that end users can start a reproducible OmsiLaunch session with one command (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). This page is the normative reference for the `omsilaunch.session-profile/v1` format as implemented by `SessionProfileCompiler` in `src/OmsiLaunch.Core/SessionProfiles.cs`, for the precedence rules applied by the CLI (`CliInput.BuildSpecAsync` and `RejectProfileConflicts` in `tools/OmsiLaunch.Cli/Program.cs`), and for the settings catalogue a profile may write (`ConfigurationCatalog`). Everything a profile can do, the CLI flags and the [LaunchSpec](launchspec.md) can also do; a profile only packages those choices.

Stability: `STABLE_BETA` for parsing, validation, conflict detection and the `settings` / `presentation` / `internet-textures` / `behavior` blocks (offline test `session-profiles.strict-compiler`; the overlay and restore path is runtime validated by RV-005 and RV-006, see [runtime validation status](../status/runtime-validation-status.md)). The `new.date`, `new.time`, `new.year` and `new.weather` keys are `UNAVAILABLE` in this build (see [The `new` block](#new)).

## Package location and naming

| Item | Rule |
| --- | --- |
| Package directory | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Profile file | `<package>\profile.yaml` (exact name, one file) |
| Assets | Any files or directories inside the package directory, referenced by relative path from `presentation.splash.assets` and `internet-textures.profile` |
| `id` | Must be a plain directory name: it must not be empty or whitespace, must not contain `\`, `/` or `:`, and must not contain the sequence `..`. Violations are `OL_E_SESSION_PROFILE_PATH_ESCAPE`. The `id` value declared inside `profile.yaml` must equal the directory name byte for byte (case-sensitive); otherwise `OL_E_SESSION_PROFILE_INVALID`. |
| Selection | `/predefined-profile:<id>` together with `/predefined-profile-index:<n>`. The index is mandatory: `/predefined-profile` without `/predefined-profile-index` fails with `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Missing package | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Release layout | The release package ships an example under `.omsilaunch\examples\session-profiles\rmg-leste\` (see [packaging](packaging.md)). Examples are not profiles: copy a package to `.omsilaunch\session-profiles\<id>\` to make it selectable. |

A profile is installed and removed by the user or the content author. OmsiLaunch never writes into a package, never copies it, and never deletes it. The package directory is not part of any transaction.

## Parsing rules

| Rule | Behaviour | Error |
| --- | --- | --- |
| Size limit | `profile.yaml` must not exceed 256 KiB (262,144 bytes) | `OL_E_SESSION_PROFILE_INVALID` |
| Document shape | Exactly one YAML document whose root node is a mapping | `OL_E_SESSION_PROFILE_INVALID` |
| Schema | `schema` must be exactly `omsilaunch.session-profile/v1` (case-sensitive) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Anchors and aliases | Any node carrying a YAML anchor (`&name`) anywhere in the document is rejected before validation; aliases (`*name`) therefore cannot occur | `OL_E_SESSION_PROFILE_INVALID` ("YAML anchors are not supported.") |
| Unknown keys | Every mapping is closed: a key that is not listed for its context in the tables below is rejected ("Unknown property in `<context>`: `<key>`"). Keys are matched case-sensitively (`Schema:` is an unknown key). The only open mapping is `settings`, whose keys are validated against the settings catalogue instead. | `OL_E_SESSION_PROFILE_INVALID` |
| Scalars | Every leaf value must be a scalar; sequences and mappings where a scalar is expected are rejected ("`<field>` must be a scalar.") | `OL_E_SESSION_PROFILE_INVALID` |
| Numbers | Integers are parsed with the invariant culture (`1`, `30`); decimals in `settings` use `.` as the separator | `OL_E_SESSION_PROFILE_INVALID` |
| Dates and times | `new.date.value` is parsed by `DateOnly.Parse` and `new.time.value` by `TimeOnly.Parse`, both invariant culture; use ISO forms `yyyy-MM-dd` and `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| YAML syntax errors | Reported with the parser message | `OL_E_SESSION_PROFILE_INVALID` ("Invalid YAML: ...") |
| Executable content | YAML is parsed with `YamlDotNet` into a representation tree only; no tags, custom types or code execution are supported |

Backslashes in plain (unquoted) scalars are literal characters. Write Windows paths with a single backslash (`maps\Grundorf\global.cfg`). A doubled backslash in a plain scalar stays doubled in the value; see [The packaged example](#the-packaged-example).

## Key reference

Contexts are named exactly as the compiler names them. Every key listed here is accepted; nothing else is.

### `profile` (root mapping)

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `schema` | string | yes | Literal `omsilaunch.session-profile/v1`. |
| `id` | string | yes | Package identifier; must equal the directory name. |
| `name` | string | yes | Display name; reported in `SessionProfileMetadata.Name`. |
| `author` | string | yes | Author; reported in `SessionProfileMetadata.Author`. |
| `version` | string | yes | Package version string (free form, quote it: `"1.0"`); reported in `SessionProfileMetadata.Version`. |
| `compatibility` | mapping | no | See `compatibility`. |
| `new` | mapping | no | NEW_MAP defaults. See `new`. |
| `presets` | sequence of mappings | yes | 1 to 99 preset entries. Zero, more than 99, or a non-sequence value is `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `maps` | sequence of strings | no | Map identities (`maps\<Map>\global.cfg`) this profile is valid for. `/` is normalised to `\`; comparison is case-insensitive. An absent or empty list means "any map". When non-empty it is enforced for `WorldMode.NewMap` (against the effective `new.map` or `/map`) and for `WorldMode.SavedSituation` (against the map referenced by the selected `.osn`, resolved through the content catalogue). For `WorldMode.LastMapState` no map can be derived, so a non-empty list always fails. Failure: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

The block is read and validated whenever it is present, but it is applied to the spec only when the selected world mode is NEW_MAP (`/new`, the CLI default). Under `/saved:<file.osn>` the block is ignored.

| Key | Type | Required | Applied | Description |
| --- | --- | --- | --- | --- |
| `map` | string | no | yes | Map identity in the normalised form `maps\<Map>\global.cfg` (planning requires this exact shape: starts with `maps\`, ends with `\global.cfg`, no `..`). Sets `WorldSpec.MapIdentity`. |
| `entrypoint-index` | integer | no | yes | Presented entrypoint index (0-based position in OMSI's entrypoint list). Sets `PresentedEntrypointIndex` and clears any entrypoint identity. |
| `entrypoint` | string | no | yes | Raw entrypoint identity. Sets `EntrypointIdentity` and clears the presented index. If both `entrypoint-index` and `entrypoint` are present, `entrypoint` wins because it is applied last. The value is an exact (case-sensitive) label or a discovery identity; OMSI presents only the first record of each label (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapping | no | no (`UNAVAILABLE`) | See `new.date`. |
| `time` | mapping | no | no (`UNAVAILABLE`) | See `new.time`. |
| `year` | integer | no | no (`UNAVAILABLE`) | Explicit year. |
| `weather` | mapping | no | no (`UNAVAILABLE`) | See `new.weather`. |

`date`, `time`, `year` and `weather` are compiled into `DateSpec`, `TimeSpec`, `YearSpec` and `WeatherSpec` with `DateTimeMode.Explicit` / the selected `WeatherMode`. The session planner (`src/OmsiLaunch.Core/SessionPlanner.cs`) requires `world.explicit-date`, `world.explicit-time`, `world.explicit-year` and `world.weather`; all four are `EXPERIMENTAL` (closed statically, runtime validation pending). The start date/time applies to NEW_MAP only; the weather applies to every world mode.

#### `new.date`

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `mode` | string | yes | Must be `explicit` (case-insensitive). Any other value is `OL_E_SESSION_PROFILE_INVALID` ("date must use explicit mode."). |
| `value` | string | yes | `yyyy-MM-dd`. |

#### `new.time`

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `mode` | string | yes | Must be `explicit`. |
| `value` | string | yes | `HH:mm` or `HH:mm:ss`. |

#### `new.weather`

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `mode` | string | yes | `preset`, `icao` or `real` (case-insensitive). Anything else: `OL_E_SESSION_PROFILE_INVALID` ("Unsupported weather mode"). |
| `preset` | string | when `mode: preset` | Weather preset name. |
| `icao` | string | when `mode: icao` | ICAO station code. |

### `preset` (each entry of `presets`)

| Key | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `index` | integer | yes | | 1 to 99, unique within the profile; indexes need not be contiguous. Selected with `/predefined-profile-index`. Duplicate or out-of-range: `OL_E_SESSION_PROFILE_INVALID`; an index that exists nowhere in the profile: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | string | yes | | Preset identifier; reported as `SessionProfileMetadata.PresetId`. |
| `name` | string | yes | | Preset display name; reported as `SessionProfileMetadata.PresetName`. |
| `settings` | mapping | no | none | Semantic `options.cfg` settings, see [Settings](#settings). Keys are matched case-insensitively against the catalogue. |
| `presentation` | mapping | no | inherit | Splash presentation, see `presentation`. When absent the preset inherits the baseline (`/spec` value or the CLI default, `Managed`). |
| `internet-textures` | mapping | no | inherit | See `internet-textures`. |
| `behavior` | mapping | no | inherit | Timeouts, see `behavior`. |
| `hofs` | sequence of mappings | no | inherit | Session HOF operations, see `hofs`. When present they replace the baseline `HofManagement`. |
| `modules` | mapping | no | inherit | Runtime modules, see `modules`. Joined with what a `/spec` file asks for. |

Only the selected preset is applied. Every preset is still parsed and validated, so an error in preset 3 fails a request for preset 1.

### `presentation`

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `splash` | mapping | yes | Required when `presentation` is present ("Presentation requires splash."). See `presentation.splash`. |

#### `presentation.splash`

| Key | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `mode` | string | yes | | `managed` installs OmsiLaunch splash bitmaps for the session (`SplashMode.Managed`). `unset` or `native` preserves OMSI's own splash files (`SplashMode.Unset`; `Native` is an alias). Case-insensitive. Anything else: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | string | no | `ENG` | Locale of the second splash target: `PTB`, `ENG`, `DEU`, `FRA` (aliases `PT-BR`, `EN`, `DE`, `FR`; anything unknown resolves to `ENG` at session build). With `mode: managed` the session overlays `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | string | no | packaged assets | Directory **relative to the package**, containing `ENG.bmp` and, for a non-English `language`, `<language>.bmp`; each must be a 640x480, 24-bit BMP. The directory must exist at profile load (`OL_E_SESSION_PROFILE_ASSET_MISSING`); the files are validated at session start (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Path confinement rules apply. When omitted the installation's `.omsilaunch\assets\splash` (or the packaged defaults) are used. |

A profile cannot set `SessionPresentationSpec.SuppressTrayIcon`; it stays `false` unless a `/spec` sets it.

### `internet-textures`

| Key | Type | Required | Description |
| --- | --- | --- | --- |
| `mode` | string | yes | `native` (`InternetTexturesMode.Native`, OMSI behaves normally), `disabled` (`Disabled`, the profiled in-process downloader is suppressed for the session), `override` (`Override`, a session-scoped `.itx` profile is installed as `Texture\standard.itx`). Case-insensitive; anything else: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | string | required for `override` | Path **relative to the package** of the `.itx` file. Missing key with `override`: `OL_E_SESSION_PROFILE_INVALID`; missing file: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Path confinement rules apply. The file must consist of `URL` / `target` line pairs with `http://` or `https://` URLs (`OL_E_ITX_PROFILE_INVALID` otherwise) and every target must resolve below the installation's `Texture\` directory without traversing a reparse point (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). The listed targets and `Texture\standard.ipr` become session deletions (see [transactions and recovery](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Key | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `startup-timeout` | integer (seconds) | no | 180 | Time allowed from process start to `Running`. Must be positive at profile load; the session additionally requires 1 to 600 at start (`OL_E_START_SESSION` otherwise). Maps to `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | integer (seconds) | no | 30 | Maps to `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: the supervisor terminates OMSI directly and never reads this value. |

When the `behavior` block is present, both timeouts are set (given value or default) and replace the baseline `LaunchBehaviorSpec` entirely, including `RestoreConfiguration` and `SuppressStaleClosecheckWarning`, which revert to their defaults (`true`, `true`).

### `hofs`

Each entry is one [HOF management](hof-management.md) operation and names exactly one source key (`package`, `collection`, `directory`, `vehicle` or `file`); zero or several: `OL_E_SESSION_PROFILE_INVALID`. Existence of sources and targets is checked when the session is planned (`OL_E_HOF_*`), except for `package`.

| Key | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `package` | string | one source key | | Directory **relative to the package** holding the `.hof` files the profile ships. Must exist at profile load (`OL_E_SESSION_PROFILE_ASSET_MISSING`); path confinement rules apply. Becomes a `Directory` source. |
| `collection` | string | one source key | | Name of a collection under `.omsilaunch\hof\`. |
| `directory` | string | one source key | | Installation-relative directory, for example next to map content. |
| `vehicle` | string | one source key | | Vehicle identity or folder whose HOFs are copied. |
| `file` | string | one source key | | Installation-relative `.hof` file. |
| `targets` | sequence of strings | yes | | Target vehicles (identities or folders below `Vehicles\`). Missing or empty: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | sequence of strings | no | all | File names to take from the source. |
| `mode` | string | no | `add` | `add` or `mirror` (case-insensitive); anything else: `OL_E_SESSION_PROFILE_INVALID`. |

A preset with `hofs` combined with a `/hof-source` flag: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

One entry per [runtime module](../../../reference/runtime-modules.md) id (`EXPERIMENTAL`). Each entry is a mapping with these keys (context `module`):

| Key | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `enabled` | `true` or `false` | no | not set | `true` requests the module (`LaunchSpec.Modules.Enable`); `false` keeps it out of the session, including a module loaded by default, by its map or by the settings file (`LaunchSpec.Modules.Disable`). |
| `settings` | mapping | no | none | Module settings by key (`LaunchSpec.Modules.Settings` as `<id>.<key>`). The values are checked against the module's declared settings when the session is planned (`OL_E_MODULE_SETTING_INVALID`). |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

A module id that is not `[a-z0-9][a-z0-9-]{1,47}`, an `enabled` other than `true` or `false`, or a setting key that is not `[a-z][a-z0-9_]{0,31}`: `OL_E_SESSION_PROFILE_INVALID`. A module that is not installed is reported when the session is planned (`OL_E_MODULE_NOT_FOUND` for `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` for settings only).

## Settings

`settings` keys are the semantic names of `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). The compiler accepts a key only if it exists (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) and is writable (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Values are stored as strings and converted into an `options.cfg` patch when the session builds its overlays; an invalid value is therefore detected at `StartSessionAsync`, not at profile load, and fails the session with `OL_E_START_SESSION` whose message carries `OL_E_INVALID_SETTING_VALUE: <key>`. Every setting below writes `options.cfg`; all of them are session-scoped and restored exactly after the session.

Value forms:

- **bool** is `true` or `false` (case-insensitive). For presence tokens the token is added or removed; for inverted tokens (`no_*`) `true` removes the negative token.
- **int** / **decimal** are validated against the range; values with a divisor are stored divided (for example `graphics.minObjectScreenPercent: 5` writes `0.05`).
- **string** is written verbatim.

| Setting key | `options.cfg` token | Type | Range / values | Evidence |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | any | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | any | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (presence) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (presence) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (presence) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (presence) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (presence) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (presence) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | any | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, stored /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, stored /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (presence) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (presence) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, written as `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` or `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (4-line block) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (presence) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (inverted presence) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (both presence tokens) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (presence) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (presence) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (presence) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (presence) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` component 0 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 runtime) |
| `traffic.humans` | `AIMaxCountRandom` component 1 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 runtime) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Catalogue entries that exist but are **not writable** (rejected with `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (superseded by `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

## Path confinement

`presentation.splash.assets` and `internet-textures.profile` are resolved by `Confined(package root, value)`:

1. Rooted paths (`C:\...`), paths starting with `\`, and any path component equal to `..` are rejected.
2. The full path is computed and must start with the package directory.
3. Every existing component below the package root, up to and including the final path, is inspected for the `ReparsePoint` attribute. A junction, directory symbolic link, or file symbolic link anywhere on that path is rejected, as is a component that cannot be inspected (`IOException` / `UnauthorizedAccessException`).

All three failures are `OL_E_SESSION_PROFILE_PATH_ESCAPE`. The same reparse-point rule is applied to `.itx` targets under `Texture\` at session build.

## Precedence and override conflicts

`CliInput.BuildSpecAsync` composes the spec in this order:

1. **Defaults** (NEW_MAP, everything unset, timeouts 180 s / 30 s).
2. **`/spec:<file.json>`**, if given, replaces the defaults entirely.
3. **Installation root**: an explicit installation argument wins over the spec's `RootPath`; `.` means the directory containing the executable.
4. **Profile** (`/predefined-profile` + `/predefined-profile-index`): the package is loaded and `RejectProfileConflicts` runs against the raw CLI arguments **before** anything is merged. The world block of the seed is then reset to an empty `WorldSpec` of the selected mode (a `/spec` world is discarded when a profile is used) and `SessionProfileCompiler.Apply` layers the profile onto the seed: `new` (NEW_MAP only), `settings` (merged over the seed's `Environment.General`, profile wins per key), and `presentation`, `internet-textures`, `behavior` (each replaces the seed block only when the preset defines it).
5. **Remaining CLI arguments** are layered on top: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, weather flags, vehicle flags, `/set`, splash flags, internet-textures flags, `/startup-timeout`, `/shutdown-timeout`. Timeouts from the CLI apply only when given; otherwise the spec/profile/default value stands.
6. **Compatibility check** for non-NEW_MAP modes (`ValidateCompatibility`).

A CLI argument that targets a field the selected profile owns is a conflict, rejected with `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (exit code 2, category `invalid_argument`). The check is per field, not per value: repeating the profile's own value is still a conflict.

| CLI argument | Conflicts when the profile defines | Only in mode |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` or `/entrypoint-index` | `new.entrypoint` or `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | the same `<key>` in the preset's `settings` (case-insensitive) | any |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (any) | any |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (any) | any |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (any) | any |
| `/module:<id>` | `modules.<id>.enabled: false` | any |
| `/no-module:<id>` | `modules.<id>.enabled: true` | any |
| `/module-setting:<id>.<key>=...` or a module flag | `modules.<id>.settings.<key>` | any |

Not conflicts: `/set` keys the preset does not define (they are added), vehicle flags (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; a profile cannot define a player vehicle), and any world argument under `/saved` (the `new` block is not applied there). `/map`, `/entrypoint` and `/entrypoint-index` are invalid together with `/saved` regardless of profiles (`OL_E_INVALID_ARGUMENT`).

## Error codes

| Code | Raised when | CLI exit |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` does not exist | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` is not a plain directory name; `assets` / `profile` leaves the package or traverses a reparse point | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` is not `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | size limit, document shape, anchors, unknown key, missing required key, non-scalar value, bad number/date/time, `id` mismatch, preset count/index rules, unsupported mode words, non-positive timeout, `presentation` without `splash`, `override` without `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` missing, outside 1..99, or no preset with that `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | a `settings` key is not in the catalogue | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | a `settings` key is catalogued but read-only | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | `assets` directory or `profile` file does not exist inside the package | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` is non-empty and the effective map is not listed (or cannot be derived) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | an explicit CLI argument targets a profile-owned field | 2 |

All of these are raised while the command line is being compiled, before planning. They are `SessionProfileException` (or `ArgumentException` for the conflict) and never start a session. The full catalogue is in [errors](errors.md); exit codes in [exit codes](exit-codes.md).

## Discovering installed profiles

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) compiles every package under `.omsilaunch\session-profiles\` and prints one entry per package: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` and its `Presets` (`Index`, `Id`, `Name`) in index order. A package that does not compile is still listed, with `IsValid: false` and the same `OL_E_SESSION_PROFILE_*` code a launch would report. Nothing is written.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

## How a profile appears in the API

After a successful load the spec carries a `SessionProfileMetadata` record in `LaunchSpec.SessionProfile`:

| Field | Source |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | selected preset `id` |
| `PresetIndex` | selected preset `index` |
| `PresetName` | selected preset `name` |
| `PackagePath` | absolute package directory |

The planner adds an informational diagnostic `session_profile.selected` to every `SessionPlan` built from such a spec, with data keys `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` and `session_profile.path`. It does not affect runnability. Integrators using the [public API](public-api.md) directly may call `SessionProfileCompiler.Load` and `SessionProfileCompiler.Apply` from `OmsiLaunch.Core`; the YAML representation never crosses into `OmsiLaunch.Api`.

## Examples

### Example 1: settings-only profile, one preset

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

Run: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. The map and entrypoint come from the command line because the profile defines no `new` block; adding `/set:graphics.maxFPS=60` is allowed, adding `/set:traffic.humans=10` is a conflict.

### Example 2: map-bound profile with three presets and packaged assets

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, with `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` and `textures\offline.itx` inside the package:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

Run: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. With `/saved:situations\mytrip.osn` the `new` block is skipped and the `.osn` must reference `maps\Grundorf\global.cfg`.

### The packaged example

The release ships `docs/examples/session-profiles/rmg-leste/profile.yaml` ([view](../../../examples/session-profiles/rmg-leste/profile.yaml)). It is syntactically valid, matches the schema and would load without error. Two properties keep it from starting a session unchanged in this build:

1. It sets `new.date`, `new.time` and `new.weather`, which make the plan not runnable (see [The `new` block](#new)).
2. Its path values are plain scalars with doubled backslashes (`maps\\RMG Leste\\global.cfg`). YAML keeps them doubled, and map identities are compared textually (after `/` to `\` normalisation only), so `new.map` and `compatibility.maps` would not match the catalogue identity `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` at planning). The `assets` value still resolves because Windows path normalisation collapses doubled separators.

The runnable form for this build is:

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

## Related pages

- [CLI reference](cli.md) for `/predefined-profile`, `/predefined-profile-index`, `/set` and the world flags
- [LaunchSpec](launchspec.md) for the record a profile compiles into
- [Transactions and recovery](../concepts/transactions-and-recovery.md) for how `settings`, splash and `.itx` overlays are applied and restored
- [Capabilities](capabilities.md) and [runtime validation status](../status/runtime-validation-status.md)
