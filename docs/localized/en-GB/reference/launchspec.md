# LaunchSpec reference

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> British English edition of the [canonical page](../../../reference/launchspec.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page is the normative reference for `LaunchSpec`, the request record that describes one OmsiLaunch session: its C# shape (`OmsiLaunch.Api`), its JSON file form as loaded by the CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), every property with its type, default, validation rule and current effect, the validation rules that make a plan non-runnable, and the precedence between CLI flags, spec files and session profiles. Only what the current code does is documented.

Related pages: [public API](public-api.md), [CLI reference](cli.md), [session profiles](session-profiles.md), [error codes](errors.md), [session lifecycle](../concepts/session-lifecycle.md), [capabilities](capabilities.md).

## Where a LaunchSpec comes from

| Source | How it becomes a `LaunchSpec` |
| --- | --- |
| API | The integrator constructs the record and passes it to `PlanSessionAsync`. |
| CLI flags | `CliInput.BuildSpecAsync` starts from built-in defaults (NEW_MAP, everything unset, `Behavior` defaults) and applies flags. |
| `/spec:<path>` JSON file | Loaded by `LaunchSpecJson.LoadAsync`, then used as the seed that CLI flags override (see [precedence](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Session profile (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` writes the profile's world, settings, presentation, internet-textures and behaviour into the seed and records `SessionProfile` metadata. |

The [complete example](#complete-example) below is verified against the record shape. A copy of the minimal example ships as `examples/release-session.example.json` (and `.omsilaunch\examples\release-session.example.json` in the release package).

## JSON form

| Rule | Detail |
| --- | --- |
| Serialiser | `System.Text.Json` with `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; no converters are registered. |
| Property names | The C# property names (`Installation`, `RootPath`, ...). Matching is case-insensitive on load; the CLI writes them in PascalCase. |
| Enums | Integers (there is no string-enum converter). `"Mode": 0` is valid; `"Mode": "NewMap"` is rejected as malformed JSON. Values are listed in [Enumerations](#enumerations). |
| `OptionalValue<T>` | An object `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (the value is ignored), 1 = `Set` (the value must be present and non-null; a `Set` with a null value is not validated and behaves as an invalid value). An omitted `OptionalValue` member is `Unset`. The read-only `IsSet` member appears in output written by the CLI and is accepted and ignored on load. |
| Optional records | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile`, `HofManagement`, `Modules` may be `null` or omitted; the `Effective*` accessors substitute defaults. |
| Required records | `Installation`, `World`, `Date`, `Time`, `Environment` (with all eight dictionaries, use `{}`), `Behavior` must be present objects. They are not validated: a `null` or missing one fails later with a null reference, which the CLI reports as `OL_E_INTERNAL` (exit 10) or `OL_E_INVALID_ARGUMENT` (exit 2). |
| Unknown properties | Rejected before binding: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (the path uses the member names as written in the file). Dictionary contents (`Environment.*`) are not checked as properties. |
| Root | Must be a JSON object: `OL_E_SPEC_INVALID`. Maximum nesting depth 32. |
| File size | At most 1 MiB (1 048 576 bytes): `OL_E_SPEC_TOO_LARGE`. Missing file: `OL_E_SPEC_NOT_FOUND`. |
| Comments and trailing commas | `//` and `/* */` comments and trailing commas are accepted. |
| Malformed JSON | The parser exception is not translated: the CLI reports `OL_E_INTERNAL` with exit code 10. |
| Encoding | UTF-8 (a BOM is tolerated by the reader). Backslashes in identities must be escaped (`"maps\\Grundorf\\global.cfg"`); forward slashes are accepted for map, situation, vehicle and HOF identities. |

## Complete example

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // optional pin of Omsi.exe
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // must be true
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30,             // with CooperativeShutdown
    "CooperativeShutdown": false              // opt-in
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

An explicit date, when the build supports it, is written as `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` and a time as `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. Both apply to NEW_MAP sessions only (`EXPERIMENTAL`; see below).

## Property reference

Column "Consumed" states what the current code does with the value. Stability uses the vocabulary of the [public API page](public-api.md#stability-vocabulary).

### `LaunchSpec` (root)

| Property | JSON type | Required | Default when omitted | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec` object | yes | none | yes | `STABLE_BETA` |
| `World` | `WorldSpec` object | yes | none | yes | `STABLE_BETA` |
| `Date` | `DateSpec` object | yes | none | yes, NEW_MAP only (see [`DateSpec`](#datespec-timespec-yearspec)) | `EXPERIMENTAL` |
| `Time` | `TimeSpec` object | yes | none | yes, NEW_MAP only | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | no | `Unset` | resolved for diagnostics; any set field is non-runnable | `PARTIAL` |
| `Environment` | `EnvironmentSpec` object | yes | none | yes (semantic `options.cfg` overlay) | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec` object | yes | none | partly (see record) | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec` object or null | no | `null` → `EffectiveYear` = mode `Unset` | yes, NEW_MAP only | `EXPERIMENTAL` |
| `Weather` | `WeatherSpec` object or null | no | `null` → `EffectiveWeather` = mode `Unset` | yes, every world mode, applied right after the world starts (see [`WeatherSpec`](#weatherspec)) | `EXPERIMENTAL` |
| `Input` | `InputSpec` object or null | no | `null` → `EffectiveInput` = both unset | yes (see [`InputSpec`](#inputspec)) | `EXPERIMENTAL` |
| `Diagnostics` | `DiagnosticsSpec` object or null | no | `null` → `EffectiveDiagnostics` = defaults | carried only | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec` object or null | no | `null` → `EffectivePresentation` = managed splash, no language, no custom directory, tray shown | yes | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec` object or null | no | `null` → `EffectiveInternetTextures` = `Native` | yes | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata` object or null | no | `null` | provenance only (`session_profile.selected` plan diagnostic) | `STABLE_BETA` |
| `HofManagement` | `HofManagementSpec` object or null | no | `null` → `EffectiveHofManagement` = no operations | yes (session HOF overlays, see [HOF management](hof-management.md)) | `STABLE_BETA` |
| `Modules` | `RuntimeModulesSpec` object (`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`) or null | no | `null` → `EffectiveModules` = nothing requested, disabled or set | no (modules never touch installation files through OmsiLaunch; see [runtime modules](runtime-modules.md)) | `EXPERIMENTAL` |

Read-only accessors (present in CLI JSON output, ignored on load): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, `EffectiveHofManagement`, `EffectiveModules`.

### `InstallationSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | required | Directory containing `Omsi.exe` and `plugins\`. See [path rules](#path-rules). Empty/whitespace → `OL_E_INSTALLATION_NOT_FOUND`. | yes | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string or null | `null` | `null`, or 64 hexadecimal digits (otherwise `OL_E_INVALID_ARGUMENT`). | Optional pin: when set, the plan is not runnable unless the SHA-256 of `Omsi.exe` equals it (`OL_E_UNSUPPORTED_BUILD`, data `expected_sha256` and `file_sha256`). | `STABLE_BETA` |

### `WorldSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | required | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` is an obsolete alias with the same value 2). | yes; `LastMapState` needs `MapIdentity` and the map's `laststn.osn` (`OL_E_MAP_NOT_FOUND`, `OL_E_SITUATION_NOT_FOUND`) | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | For `NewMap`: required, of the form `maps\<dir>\global.cfg` (case-insensitive, `/` accepted, no `..`) and installed. Ignored for `SavedSituation` (the `.osn` provides the map). | yes (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | For `SavedSituation`: required, an installed `situations\...\<file>.osn` identity (as returned by `DiscoverAsync(Situations)` / `/list:situations`). | yes (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | For `NewMap` without `EntrypointIdentity`: required, `>= 0`, an index into OMSI's presented entrypoint list for the map. Sent to the plugin as `-1` when unset. | yes (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | An exact (case-sensitive) entrypoint label or a discovery identity. The planner resolves it to the label OMSI presents and the startup selects that label (`world.entrypoint-identity`); a record OMSI does not present is refused (`OL_E_ENTRYPOINT_NOT_PRESENTED`), an unknown one too (`OL_E_ENTRYPOINT_NOT_FOUND`). | carried | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec` (read-only) | computed | `Mode` = `Identity` when `EntrypointIdentity` is set, else `PresentedIndex` when the index is set, else `Unset`; `PresentedIndex`, `Identity` mirror the inputs. | derived | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | required (`Year`: `0` when the record is null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | NEW_MAP: `Explicit` uses `Value`, `System` the PC clock when the session is planned; `Unset` keeps OMSI's own value (its options and Start form). The controller resolves the request into OMSI's start clock (year, day of the year, hour, minute, second; capabilities `world.explicit-date`, `world.explicit-time`, `world.explicit-year`), the startup handoff (version 7) carries it, and the plugin writes it right before OMSI's `NewSituation`, so OMSI derives the season and calendar from the requested date before the map loads. After the start the running clock is read back; a mismatch, or OMSI using a fixed start time, fails the start with `OL_E_DATE_TIME_APPLY_FAILED`. `SavedSituation`: a mode other than `Unset` is `OL_E_CAPABILITY_UNAVAILABLE` (a situation carries its own time). | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: a real calendar date (`Day` valid for the month and year); `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59; years 1900..2200. `YearSpec` sets the year (and the year of `Date`, when both are given). 31 December of a leap year is refused: OMSI's calendar cannot represent day 366. Invalid values: `OL_E_DATE_TIME_APPLY_FAILED`. Must be set when `Mode` is `Explicit` and unset otherwise (`OL_E_INVALID_ARGUMENT`). | yes | `EXPERIMENTAL` |

### `WeatherSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0` when the record is null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Any mode but `Unset` requires `world.weather`; the plugin applies it right after the world starts (every world mode; it overrides a saved situation's weather). A failure fails the start with `OL_E_WEATHER_APPLY_FAILED`. | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | A file of the installation's `weather` folder, by name or `weather\<name>.owt`; checked at plan time (`OL_E_WEATHER_INVALID`). | handoff | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Four letters or digits (`OL_E_WEATHER_INVALID` otherwise); used by `Icao` mode. | handoff | `EXPERIMENTAL` |

### `PlayerVehicleSpec` (inside `PlayerVehicle`)

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Installed `Vehicles\...\<file>.bus` identity, else `OL_E_VEHICLE_NOT_FOUND`. | resolved into `ResolvedContent`; then `player-vehicle.model` unsupported → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | A repaint identity of `Model` (`<cti>#item:<n>`), else `OL_E_REPAINT_NOT_FOUND`; only checked when `Model` is set. | same | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | Installed `Vehicles\...\<file>.hof`, else `OL_E_HOF_NOT_FOUND`. | same | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Any string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Any string. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (read-only) | computed | `true` when `Model` is set. `PlayerVehicle.IsSet` is what the handoff carries as `PlayerVehicleEnabled`. | derived | `PARTIAL` |

A `PlayerVehicle` with `Presence` 1 and all fields unset is accepted and has no effect. Any set field makes the plan non-runnable on this build (`UNAVAILABLE`).

### `EnvironmentSpec`

| Property | Type | Default | Consumed | Stability |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | `IReadOnlyDictionary<string, OptionalValue<string>>` each, required (`{}` when empty) | none | yes | `STABLE_BETA` |

The eight groups are concatenated; the group a key is placed in has no effect. Each entry with `Presence` 1 is a semantic setting from `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); the key selects the target file (`options.cfg` for every current key) and token. Planning checks that the key exists (`OL_E_UNKNOWN_SETTING`) and is writable (`OL_E_SETTING_NOT_WRITABLE`); the value is validated only at start (`OL_E_INVALID_SETTING_VALUE`, reported as a `Failed` session with `OL_E_START_SESSION`). Keys are case-insensitive. Unset entries are ignored. The CLI `/set:<key>=<value>` flag writes into `General`; session-profile `settings` are merged into `General` as well.

| Key | Value | Notes |
| --- | --- | --- |
| `general.language` | string | `[language]` token |
| `general.radio` | string | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | presence tokens (`autoSave` is the inverse of `noAutoSave`) |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | integer 10..200 | |
| `graphics.tileDistance` | integer 1..20 | |
| `graphics.maxObjectDistanceMeters` | number 20..5000 | |
| `graphics.minObjectScreenPercent` | number 0..10 | stored divided by 100 |
| `graphics.minReflectionObjectScreenPercent` | number 0..50 | stored divided by 100 |
| `graphics.maxObjectComplexity` | integer 0..3 | |
| `graphics.maxMapComplexity` | integer 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | presence tokens |
| `graphics.stencilShadows` | `true` / `false` | written as `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | one `smokesystems` block |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | presence tokens |
| `simulation.ticketSelling` | integer 0..2 | |
| `simulation.maintenance` | integer 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | two OMSI tokens at once (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | presence tokens |
| `traffic.randomVehicles` | integer 0..1000 | component 0 of the multiline `AIMaxCountRandom` block (runtime validated, matrix RV-005) |
| `traffic.humans` | integer 0..1000 | component 1 of `AIMaxCountRandom` |
| `traffic.factorPercent` | number 1..300 | |
| `traffic.parkedVehiclesPercent` | number 0..100 | |
| `traffic.scheduledVehicles` | number 0..1000 | |
| `traffic.scheduledLinePriority` | number 1..4 | |
| `traffic.passengerFactorPercent` | number 0..200 | |
| `sound.stereo` | number 0..100 | |
| `sound.maxSimultaneousSounds` | number 5..1000 | |
| `sound.masterVolume` | number 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | rejected | known but not writable → `OL_E_SETTING_NOT_WRITABLE` |

Patched files keep their encoding (Windows-1252 bytes preserved; BOM-marked UTF-8/UTF-16 respected) and line endings.

### `LaunchBehaviorSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | `true` only | The exact restore is mandatory: `false` is rejected with `OL_E_INVALID_ARGUMENT`. | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | any | `true`: a `closecheck` file that exists before the session is permanently removed at start (diagnostic `closecheck.stale-removed` with its SHA-256; failure `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: an existing `closecheck` is left alone and is not a session deletion. The `closecheck` OMSI writes during the session is always removed at restore. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (`ArgumentOutOfRangeException` from `StartSessionAsync` otherwise; CLI `/startup-timeout` enforces 1..600; profiles require > 0). Time budget from supervisor start to `Running`; on expiry the session fails with `OL_E_STARTUP_TIMEOUT` (plugin started) or `OL_E_PLUGIN_NOT_LOADED`. | yes | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600 (CLI `/shutdown-timeout`) | With `CooperativeShutdown`: the time OMSI gets to quit on its own before it is terminated. Without it OMSI is terminated at once. | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | any (CLI `/cooperative-shutdown`) | `true`: ending a running session asks OMSI to quit through its own exit routine (`TForm_main.CloseOMSI`: options and the map's `laststn.osn` are saved, plugins finalised) instead of terminating it; after `ShutdownTimeoutSeconds` it is terminated (`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`). The restore runs after the exit, so session-owned files still return to their original bytes. | `EXPERIMENTAL` |

### `InputSpec`

| Property | Type | Default | Consumed | Stability |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Path (absolute, or relative to the installation) of a keyboard file in OMSI's format. The session uses it in place of `Inputs\keyboard.cfg` (a transaction overlay: the installation's file is restored byte for byte at the end). The file must exist, be at most 1 MiB and contain at least one `[entry]` binding; otherwise `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Path of a controller file in OMSI's format, used in place of `Inputs\gamectrler.cfg` for the session in the same way. It must contain at least one `[ctrl]` device; otherwise `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| Property | Type | Default | Consumed | Stability |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | No consumer in `src/`. The host trace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` is always written. | `PARTIAL` (carried, currently no effect) |
| `Verbose` | bool | `false` | No consumer. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | No consumer. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | No consumer. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Startup handoff flag: the plugin writes `plugin.trace.operation` events (operation, outcome, code, duration in microseconds) for every runtime operation; they reach `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` through the telemetry ring. | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | Startup handoff flag: the native layer reports every hook installation and removal (`native.trace`: boundary, owner, target, result) into the same host trace. | `EXPERIMENTAL` |

The CLI flags `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` populate these booleans (`/logall` sets `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); they are ORed with the spec values.

### `SessionPresentationSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (alias `Native`): OMSI's own splash files are untouched. `1` `Managed`: OmsiLaunch overlays `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_<LANG>.bmp` for the session (restored exactly afterwards). | yes | `STABLE_BETA` (matrix RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (case-insensitive); any other value normalises to `ENG`. When unset, the `[language]` value of `options.cfg` is read and normalised the same way. | yes (managed splash only) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Directory containing `ENG.bmp` and `<LANG>.bmp` (640×480, 24-bit BMP). See [path rules](#path-rules). Missing directory: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; missing file: `OL_E_SPLASH_ASSET_MISSING`; wrong format: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. When unset, `<root>\.omsilaunch\assets\splash` is used (seeded once from the package), else the packaged `assets\splash`. | yes (managed splash only) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` suppresses the standalone Windows tray indicator of the CLI owner. | CLI owner only; the API has no tray. There is no CLI flag; it can only come from a spec file. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: nothing changes. `1` `Disabled`: the plugin suppresses OMSI's in-process downloader (telemetry `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: the `.itx` profile is overlaid as `Texture\standard.itx`; its target files and `Texture\standard.ipr` become session deletions. | yes | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Required for `Override` (`OL_E_ITX_PROFILE_REQUIRED`). A text file with pairs of lines: absolute `http`/`https` URL, then a target path relative to the installation root that contains a `Texture\` component, is not rooted, has no `..`, does not start with `\` and traverses no junction/symlink (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). See [path rules](#path-rules). | yes | `EXPERIMENTAL` |

### `HofManagementSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Operations` | array of `HofOperationSpec` | none | Applied in order; see [HOF management](hof-management.md) for resolution, conflict rules and limits. | yes | `STABLE_BETA` |

`HofOperationSpec` members: `SourceKind` (`HofSourceKind` int), `Source` (string), `TargetVehicles` (array of vehicle identities or folders below `Vehicles\`, at least one), `Mode` (`HofApplyMode` int, default `0` `Add`), `Files` (array of `.hof` file names or null for all). Problems are plan diagnostics `OL_E_HOF_*`.

### `RuntimeModulesSpec`

| Property | Type | Default | Valid values | Consumed | Stability |
| --- | --- | --- | --- | --- | --- |
| `Enable` | array of module ids | none | Ids of modules installed below `plugins\OmsiLaunch.Modules`; see [runtime modules](runtime-modules.md). A requested module that cannot be loaded is a plan diagnostic `OL_E_MODULE_*` (plan not runnable). | yes | `EXPERIMENTAL` |
| `Disable` | array of module ids or null | none | Keeps a default, content or settings-file module out of the session (plan diagnostic `module.disabled`). A module both enabled and disabled is `OL_E_MODULE_SETTING_INVALID`. Read-only accessor `EffectiveDisable`. | yes | `EXPERIMENTAL` |
| `Settings` | object of strings or null | none | `"<module id>.<key>": "<value>"`. Wins over the settings file `.omsilaunch\modules.ini` and the manifest default. An invalid value or a key the module does not declare is `OL_E_MODULE_SETTING_INVALID` (plan not runnable); settings for a module the session does not load are `OL_W_MODULE_SETTING_IGNORED`. The effective values are in `PlannedModule.Settings`. Read-only accessor `EffectiveSettings`. | yes | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Property | Type | Consumed | Stability |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | strings / int | Recorded in the plan diagnostic `session_profile.selected` (`Data["session_profile.*"]`). Not otherwise consumed; normally filled by the session-profile compiler, not by hand. | `STABLE_BETA` |

## Enumerations

| Enum | Values (JSON integer) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (obsolete alias; it is OMSI's native last-map-state branch, never the newest `.osn`) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `HofSourceKind` | `Collection` = 0, `Directory` = 1, `Vehicle` = 2, `File` = 3 |
| `HofApplyMode` | `Add` = 0, `Mirror` = 1 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (read-only `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Integers outside the declared range are stored as-is by the serialiser and behave as unknown values (for example an unknown `WorldMode` is neither NEW_MAP nor SAVED_SITUATION and yields a plan with no world capability; the plugin would reject it, but the CLI replaces the mode anyway, see precedence).

## Validation rules and non-runnable diagnostics

`PlanSessionAsync` runs `LaunchValidation.Validate` and then `SessionPlanner.PlanAsync`. A plan is runnable exactly when no diagnostic code starts with `OL_E_`. The complete set:

| Diagnostic | Condition | Source |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` empty or whitespace | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` without a set value or with month/day out of range; `Time.Mode` = `Explicit` without a set value or with hour/minute/second out of range | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` or `Time.Value` set while the mode is not `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` with `MapIdentity` unset or not of the form `maps\...\global.cfg` (validation); `NewMap` with an identity that is not installed (planner) | both |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` without `EntrypointIdentity` and with `PresentedEntrypointIndex` unset or negative | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, map installed, no `EntrypointIdentity`, `PresentedEntrypointIndex` unset (`world.presented-entrypoint` unavailable) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` without `SituationIdentity` (validation) or with an identity that is not installed (planner) | both |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: the map named inside the `.osn` is not installed | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | not Windows 10+ on an x64 OS with an x64 host process (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | root directory missing, read-only attribute set, or no `plugins\` subdirectory (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | Not an OMSI 2 installation: `Omsi.exe` missing, not a 32-bit x86 PE image, no OMSI 2 (2.x) version, or no `maps\` / `Vehicles\` directory (`omsi.omsi2`). The hash is not a criterion. | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `EntrypointIdentity` set; `Date`/`Time`/`Year` mode not `Unset` with `SavedSituation`; `Weather` mode not `Unset`; any `PlayerVehicle` field set | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` not installed (in addition to `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | an `Environment` key that is not in the catalogue / not writable | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | building the splash/ITX plan threw; the message carries `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` or `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | the plugin closure reference (`OmsiLaunchRuntimePaths`) or the release manifest cannot be loaded (message may carry `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Not validated at plan time (fails at start as a `Failed` session with `OL_E_START_SESSION`): setting values (`OL_E_INVALID_SETTING_VALUE`), permanent plugin integrity (`OL_E_PERMANENT_PLUGIN_*`), lease availability (`OL_E_INSTALLATION_BUSY`), `StartupTimeoutSeconds` range (thrown by `StartSessionAsync`).

Informational plan diagnostics: `plugin.integrity.reference` (message `manifest` or `self`), `session_profile.selected`.

## Precedence: CLI flags vs spec file vs session profile

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) builds the effective spec in this order:

1. Seed = built-in defaults, or the `/spec` file when given.
2. Installation root = the explicit installation argument if given, else the seed's `RootPath`; then `.`/empty → executable directory, `Path.GetFullPath`. An explicit installation argument always wins over the spec's `RootPath`.
3. Session profile (`/predefined-profile` + `/predefined-profile-index`): explicit CLI arguments that touch a profile-owned field are rejected with `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (world fields only in NEW_MAP mode; `/set` keys present in the preset; splash flags when the preset has `presentation`; internet-texture flags when it has `internet-textures`; timeouts when it has `behavior`). The seed's `World` is replaced by a fresh one (only the CLI world mode survives), then the profile's `new:` block (NEW_MAP only), `settings` (into `General`), `presentation`, `internet-textures`, `behavior` and `SessionProfile` metadata are applied. `compatibility.maps` is enforced for NEW_MAP and SAVED_SITUATION.
4. World: the CLI world mode always wins (`/new` default, `/saved:<osn>`, `/last`); the spec file's `World.Mode` is replaced. To run a saved situation from a spec, pass `/saved:`. `/map` and `/entrypoint`/`/entrypoint-index` override the seed; a CLI `/entrypoint` identity clears the index; `/saved` with `/map` or entrypoint flags is `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` override the seed when given (`system` selects `DateTimeMode.System`).
6. `/no-vehicle` clears `PlayerVehicle`; individual `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` override individual fields of the seed's player vehicle.
7. `/set:<key>=<value>` entries are added to `Environment.General` (key checked, value not); the other seven groups come from the seed unchanged.
8. `/startup-timeout` and `/shutdown-timeout` override the seed only when given; otherwise spec, then profile, then defaults 180 s / 30 s apply. `ShutdownTimeoutSeconds` takes effect with `/cooperative-shutdown` (which is ORed with the spec value).
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` override the seed when given; `SuppressTrayIcon` comes from the seed only.
10. Diagnostics flags are ORed with the seed.

Result: explicit CLI flag > session profile > spec file > built-in default, except that a CLI flag conflicting with a profile-owned field is an error rather than an override.

## Path rules

| Path | API behaviour | CLI behaviour |
| --- | --- | --- |
| `Installation.RootPath` | Used as given: relative paths resolve against the process working directory in file operations. Pass an absolute path. The lease, journal and content catalogue normalise it with `Path.GetFullPath`. | `.` or empty = the directory that contains `OmsiLaunch.exe`, never the caller's working folder; an explicit installation argument wins over the spec; the result is made absolute. |
| `Presentation.CustomAssetDirectory` | Absolute, or relative to `Installation.RootPath`. Must exist. | Same (`/splash-assets`). A session-profile `assets` path is confined to the profile package and stored absolute. |
| `InternetTextures.OverrideProfilePath` | Resolved with `Path.GetFullPath`, i.e. relative to the process working directory, not to the installation root. Must exist. | Same (`/internet-textures-profile`). A session-profile `profile` path is confined to the package and stored absolute. |
| ITX target lines | Relative to the installation root; must contain a `Texture\` component; no root, no `..`, no leading `\`, no junction/symlink component. | Same. |
| Content identities (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Installation-relative, case-insensitive, `/` accepted; never absolute. | Same. |

## Carried but not applied

| Field | Current effect | Stability |
| --- | --- | --- |
| `Diagnostics.Log`, `Verbose`, `OmsiLogAll`, `ProcessTrace` | none (host trace always written) | `PARTIAL` |
| `PlayerVehicle.*` (any set field) | content resolved for diagnostics, plan non-runnable (`UNAVAILABLE`) | `PARTIAL` |
| `SessionProfile` | provenance diagnostic only | `STABLE_BETA` |
