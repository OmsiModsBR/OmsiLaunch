# Capabilities

This page documents what OmsiLaunch can do. Its source is the canonical capability catalog, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): every capability, every public runtime operation with its arguments, CLI routes and native capability, and one evidence state per capability are declared there once. The runtime operation set, argument validation, CLI command routes, the plugin's per-operation native gate, `GetCapabilitiesAsync` and the [catalog table](#capability-catalog) below are derived from it. For every runtime operation the page states the arguments, result keys and errors. How to invoke an operation (CLI routes, API envelope, timeouts, handles) is in [runtime control](runtime-control.md); the evidence per session is in [runtime validation status](../status/runtime-validation-status.md).

## Classification and stability

`PublicCapabilityClassification` has four values. They map to the stability vocabulary as follows, with downgrades where evidence is incomplete:

| Classification | Meaning | Stability |
| --- | --- | --- |
| `PublicStableBeta` | Public, in the Beta contract, runtime validated | `STABLE_BETA` (downgraded to `PARTIAL` where noted) |
| `PublicExperimental` | Public, may change, runtime validated or statically validated | `EXPERIMENTAL` (downgraded to `PARTIAL` where noted) |
| `InternalOnly` | Research primitive; not reachable through the public API or CLI | `INTERNAL` |
| `Unsupported` | Recognised for diagnostics, rejected or absent | `UNAVAILABLE` |

`PublicCapabilityKind` distinguishes `Read`, `Write`, `Action` and `Event`. Every runtime capability requires a `Running` session (`RequiresSession` in the registry) and its native capability resolved and verified on the running build; an operation whose native capability is unavailable is rejected with `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` marks capabilities that depend on native targets (see [compatibility](compatibility.md)).

Runtime results are dictionaries of strings. Keys that start with `internal_` or end with `_address`, `_pointer` or `_vmt` are removed at the API boundary (`ScrubInternalValues`) and are not listed here. The result keys in the operation tables below are the exact key sets returned by the product: they were captured by executing every public operation in a real session (runtime closure documentation capture, session `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; list rows are written `<row>.<n>.<field>`).

How an operation failure is reported (`CurrentRuntimeControl.Execute`):

| Failure | `ErrorCode` | `Values` |
| --- | --- | --- |
| Registry rejection (unknown or `internal.*` operation, missing required argument) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | none |
| A deliberate product rejection (a derived weather key in `weather.set`) | the specific code (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (sentence) |
| A D3D failure | the specific `OL_E_D3D_*` code | `detail`, `native_status` |
| A typed plugin-side failure (stale handle, unknown name, value out of range, no player vehicle, ...) | the specific code, for example `OL_E_RUNTIME_OBJECT_HANDLE_STALE` or `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (the message), `exception` (.NET type name) |
| An operation whose native capability is not resolved on the running build | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (the native capability) |
| An unexpected plugin-side exception without a code | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| A result that does not fit the 64 KiB mailbox | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (paged lists are shortened instead and continue with `next_cursor`, see [pagination](#pagination)) | none |

The `Errors` column of each table names the specific codes; they arrive in `ErrorCode` through the plugin, the bridge, the control plane, the API and the CLI unchanged.

## Capability catalog

One row per catalog entry, generated from `PublicCapabilityRegistry.All` (the documentation gate fails when this table is stale). `Session` is `Running` for capabilities that need a running session. `Operations` are the concrete runtime operation ids (contract rows in [public runtime operations](#public-runtime-operations)). `Native capability` names the entries of the native target catalog (see [compatibility](compatibility.md)) that must be resolved and verified on the running build. `Evidence` uses one vocabulary:

| Evidence | Meaning |
| --- | --- |
| RUNTIME_VALIDATED | Observed in a real OMSI session; the session ids are in [runtime validation status](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implemented and verified offline; the runtime effect has not been observed. |
| PARTIAL | Works for part of its contract; the note names the open part. |
| RUNTIME_VALIDATION_REQUIRED | Implemented, awaiting its runtime validation. |
| UNAVAILABLE | Recognised for diagnostics and rejected, or not implemented; requesting it at start makes the plan not runnable. |

An operation that changes something names its mutation policy after its id (`PublicRuntimeOperation.MutationPolicy`); no runtime operation is journaled:

| Mutation policy | Meaning |
| --- | --- |
| `none` | Reads only (not shown). |
| `omsi-state` | Changes OMSI's in-memory state; nothing undoes it; it ends with the OMSI process. |
| `session-resource` | Creates or changes a session-owned resource, released explicitly or when the session ends. |
| `session-policy` | Installs a policy the plugin re-applies until it is removed or the session ends. |

<!-- generated:capability-catalog:begin -->
| Capability | Classification | Kind | Session | Operations | Native capability | Evidence | CLI | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: plugin events travel through a numbered ring; a reader that falls behind receives telemetry.lost with the count. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 round A: known layout. |
| `boot.headless-start` | `PublicStableBeta` | Action | no |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | no |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI presents the first record of each exact label; the startup selects it by that label. Closed statically; runtime validation pending. |
| `world.saved-situation` | `PublicStableBeta` | Action | no |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | no |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: the native branch is closed statically (BI-006); runtime validation pending. Never a newest-.osn heuristic. |
| `world.explicit-date` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: written into OMSI's start clock before NewSituation (static analysis); runtime validation pending. |
| `world.explicit-time` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: written into OMSI's start clock before NewSituation (static analysis); runtime validation pending. |
| `world.explicit-year` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: written into OMSI's start clock before NewSituation (static analysis); runtime validation pending. |
| `world.weather` | `PublicExperimental` | Action | no |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): applied right after the world starts, through OMSI's own loadWeather or its METAR flags; it overrides a saved situation's weather. Closed statically; runtime validation pending. |
| `player-vehicle.model` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; resolved for diagnostics, not applied. |
| `player-vehicle.repaint` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false in a headless start; the full snapshot with a saved situation. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: the native assignment sequence is closed statically (research/reports/beta4-static/entrypoint-last-player-vehicle.md section 3) but not implemented before runtime validation; planned for a later beta. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: transactional overlay, validated offline. |
| `input.controller` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: transactional overlay, validated offline. |
| `presentation.splash` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | no |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 round B: the suppression is applied and reported (internet-textures.suppressed) through the telemetry ring. |
| `internet-textures.override` | `PublicExperimental` | Write | no |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Runtime closure I01. |
| `session-profiles` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 profile start; 1..99 and discovery offline. |
| `hof.management` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 round A C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 closed statically: SetActualDateTime only copies the PC clock into the start clock; start dates use world.explicit-date. Superseded: native ABI and postconditions are not closed. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: writes actWeather (Weather+0x08), the record OMSI's weather tick derives from; closed statically. Derived values (humidity, light factors, actual wind) stay read-only (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Runtime validation pending. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Needs a PlayerVehicle (a saved situation provides one); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE on a headless NEW_MAP. |
| `ui.pause` | `PublicExperimental` | Action | Running | `ui.pause.state`, `ui.pause.take-over` (omsi-state), `ui.pause.restore-native` (omsi-state), `ui.pause.action` (omsi-state) | `ui.pause-menu.replace` | RUNTIME_VALIDATION_REQUIRED | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action --action=<resume|new-game|save|options|quit>` | Beta 5: statically validated design (research beta4-static pause-menu-and-input.md); the native calls are not yet observed in a real session. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | Stale detection after natural removal (RV-002) has no safe runtime producer and is validated offline. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paged) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paged), `timetable.trips.list` (paged), `timetable.lines.list` (paged), `timetable.rv-files.list` (paged), `timetable.track-entries.list` (paged), `timetable.bus-stops.list` (paged), `timetable.station-links.list` (paged), `timetable.tours.list` (paged), `timetable.profiles.list` (paged), `timetable.tour-entries.list` (paged), `timetable.logs.read` (paged), `timetable.no-rv-numbers.list` (paged) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paged), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paged), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Writes: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI copies a constant-shaped string; closed statically. Runtime validation pending. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): virtual TriggerXML/SoundTrigger through the profiled VMT slots; closed statically. Runtime validation pending. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Device Reset observed (resetting, restored, generation invalidation); a distinct lost transition was not produced (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` is `InternalOnly`: it exists in the plugin for research (it returns a native address) and is rejected at the API boundary and by the local control plane, both of which validate the operation name against `PublicRuntimeOperationIds` first; the CLI has no route for it. `session.stop` terminates OMSI; cooperative shutdown is not implemented.

## Pagination

Every list operation (marked "(paged)" in the [capability catalog](#capability-catalog)) takes two optional arguments and answers four page fields:

| Argument | Meaning |
| --- | --- |
| `cursor` | Where the page starts: the `next_cursor` of the previous page. Omitted: the first page. A cursor from another list, or malformed, is `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Rows wanted (a positive integer). Larger than the list's maximum page: the maximum is used. Not a positive integer: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Result key | Meaning |
| --- | --- |
| `count` | All records OMSI holds. |
| `returned_count` | Rows in this reply. |
| `truncated` | `true` when records follow this page. |
| `next_cursor` | Present only when records follow: pass it as `cursor` for the next page. |

The cursor is a stable key, not a position. For record arrays (timetable, script names, constants, curves, HOFs, drivers, tickets, logs) rows are keyed `<row>.<n>.<field>` (or `name.<n>`) by the record's absolute index and the cursor names the next index, so the first page equals the Beta 3 reply. For handle collections (`road-vehicles.list`, `humans.list`) rows are returned in handle order and the cursor names the last returned handle: an object that appears between two pages gets a later handle and comes on a later page; none is repeated. Treat cursor values as opaque.

Maximum page sizes: 1000 for `road-vehicles.list` and `humans.list`; 512 for `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` and `vehicle.curves.list`; 256 for `timetable.tour-entries.list`, `timetable.logs.read` and `vehicle.hofs.read`; 128 for the other timetable lists, `drivers.read` and `tickets.read`. When the rows of a page still exceed the 64 KiB mailbox, the plugin drops the last rows until the reply fits and `next_cursor` continues at the first dropped row, so no list fails because of its size.

## Public runtime operations

This is the exact list of operation ids a public frontend may forward (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Every one requires `SessionState.Running`; none is journaled or restored. Required arguments are enforced by the registry before the mailbox is used (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); optional arguments are validated by the plugin. Common failure codes for all operations: `OL_E_RUNTIME_OPERATION_UNKNOWN` (not in this list), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (plugin has no implementation), `OL_E_RUNTIME_OPERATION_FAILED` (unexpected in-process exception; `detail` and `exception` values).

### Time

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `time.read` | Read | none | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | optional `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); at least one | the `time.read` keys after the profiled `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

### Weather

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | none | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, and the source values `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, the light factors and the humidities are OMSI's per-tick values; `cloud_type` is the resolved cloud-type number | |
| `weather.set` | Write | at least one of `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Ranges: fog 0..100000, lightness and ground values 0..1, wind speed 0..200, wind direction 0..360, temperature and dew point -60..60, pressure 850..1100, precipitation 0..2, rate 0..255, cloud height 0..20000, `cloud_type` 1..64 characters of code page 1252 (`-1` = no clouds), booleans `true`/`false`/`1`/`0` | the `weather.read` keys; with `cloud_type` also `cloud_type_resolved` (`false` when OMSI does not know the cloud type: it draws none) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (a derived key: `relative_humidity`, `absolute_humidity`, the light factors, `cloud_transparency`, `precipitation_set`, `wet_ground`; the detail names the key to set), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (unknown key), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 letters or digits) | the `weather.actual.read` keys | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | none | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

### Map

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `map.read` | Read | none | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

### Camera

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | none | `family` (0 driver, 1 passenger, 2 external, 3 map), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | optional `family` (0..3), `field_of_view` (10..170); at least one | the `camera.read` keys | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | required `family` (0..3); optional `preset` (0..255, only with family 0 or 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. The policy is re-applied every 100 ms until `camera.unlock`; a failing re-application emits the event `camera.lock.degraded` once per distinct error. |
| `camera.unlock` | Action | none | `locked` (`false`) | |

### User interface (pause menu)

Take over OMSI's native pause menu for a module's own menu (capability `ui.pause-menu.replace`, resolved on a known code identity only). OMSI's own pause byte stays set, so the simulation, the rendering, 3D clicks and DirectInput commands stay held; the native form is hidden through the VCL and its actions are called directly (no click, no posted message, no moved window). `ui.pause.action` runs the native handler from a one-shot UI-thread timer after its answer is sent, because the handler can enter a native modal dialog: read the outcome from `ui.pause.state` (`busy` `false`, then `last_result`).

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `ui.pause.state` | Read | none | `paused`, `native_menu_visible`, `main_form_ready`, `in_frame`, `taken_over`, `busy`, `pending_action`, `last_action`, `last_result` (`none`, `resumed`, `game-started`, `returned`, `quit-requested`, `exception`, `refused`) | `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.take-over` | Action | none | the `ui.pause.state` keys after the take-over | `OL_E_UI_NOT_PAUSED`, `OL_E_UI_BUSY`, `OL_E_UI_ALREADY_TAKEN_OVER`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.restore-native` | Action | none | the `ui.pause.state` keys; shows the native menu again (its own `FormShow` runs: OMSI stays paused) | `OL_E_UI_BUSY`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.action` | Action | required `action`: `resume`, `new-game`, `save`, `options` or `quit` | the `ui.pause.state` keys plus `scheduled` (`true`) and `action` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_UI_NOT_PAUSED`, `OL_E_UI_BUSY`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |


### Road vehicles and player

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | none | `count`, `ai_collection`, `player_index` (OMSI's raw player-vehicle index, reported as read; use `player-vehicle.read` `present` to know whether a player vehicle exists) | |
| `road-vehicles.list` | Read | optional `cursor`, `limit` | page fields; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` is OMSI's list position) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | required `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | none | `present` (`false` when OMSI has no player vehicle); when `true`: `player_index` plus every `road-vehicle.read` key | |
| `road-vehicles.spawn` | Action | required `model` (canonical `Vehicles\...\*.bus`, at most 240 characters, no `..`, must exist below the installation) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Does not assign the PlayerVehicle. Takes several seconds; use the 30 s client timeout. |
| `road-vehicles.place-random` | Action | optional `ai_type` (0..255, default 0), `group` (0..65535, default 1), `type` (-1..65535, default -1), `scheduled` (0..1, default 0), `tour` (0..65535, default 0), `line` (0..65535, default 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

### Humans

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | none | `count` | |
| `humans.list` | Read | optional `cursor`, `limit` | page fields; `human.<n>.handle` (`hb-NNNNNN`; `<n>` is OMSI's list position) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | required `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

### Timetable, drivers, tickets

Timetable lists, `drivers.read` and `tickets.read` are paged (see [pagination](#pagination)); "page fields" below means `count`, `returned_count`, `truncated` and `next_cursor`, and every list takes the optional `cursor` and `limit`.

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | none | `invalid` (`true`/`false`), and the record counts `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | optional `cursor`, `limit` | page fields; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | optional `cursor`, `limit` | page fields; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | optional `cursor`, `limit` | page fields; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | optional `cursor`, `limit` | page fields; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | optional `cursor`, `limit` | page fields; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | optional `cursor`, `limit` | page fields; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | optional `cursor`, `limit` | page fields; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | optional `cursor`, `limit` | page fields; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | optional `cursor`, `limit` | page fields; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | optional `cursor`, `limit` | page fields; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | optional `cursor`, `limit` | page fields; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | optional `cursor`, `limit` | page fields; `no_rv.<n>.group`, `no_rv.<n>.number`: the vehicle numbers OMSI keeps out of free AI use (OMSI's reserved-number string list, read through its own layout; `OL_E_RUNTIME_VALUE_INVALID` when the list does not have the expected shape). At most 256 per page. Static analysis; runtime validation pending. |
| `map.tiles.list` | Read | optional `cursor`, `limit` | page fields; `tile.<n>.x`, `tile.<n>.y` (grid position), `tile.<n>.tile_index` (the tile's own index in the map's tile list), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` with `tile.<n>.error` for a tile that cannot be read. At most 256 per page. Position and index from static analysis; runtime validation pending. |
| `drivers.read` | Read | optional `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | optional `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

### Vehicle scripts, constants, curves, HOF (handle-scoped)

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | required `handle`; optional `cursor`, `limit` | `handle`, page fields, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | required `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | required `handle`, `name`, `value` (finite float) | `handle`, `name`, `requested_value`, `value` (read back) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | required `handle`; optional `cursor`, `limit` | `handle`, page fields, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | required `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | required `handle`, `name` (case-sensitive, as the script declares it, up to 128 characters), `value` (1..1024 characters) | `handle`, `name`, `value` read back after the write | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (OMSI itself would ignore an unknown name silently), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | required `handle`, `name` (code page 1252, up to 128 characters), `value` (`1` runs `name`, `0` runs `name_off`, `2` runs `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. The script entry runs synchronously; a name the script does not define does nothing. A press is normally followed by a release |
| `vehicle.sound-trigger` | Action | required `handle`, `trigger` (code page 1252, up to 128 characters); optional `file` (up to 260 characters) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (also: the vehicle has no sound pack), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | required `handle`; optional `cursor`, `limit` | `handle`, page fields, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | required `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | required `handle`; optional `cursor`, `limit` | `handle`, page fields, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | required `handle`, `name`, `x` (finite float; a missing or non-numeric `x` is `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (linear interpolation) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | required `handle`; optional `cursor`, `limit` | `handle`, page fields, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D operations execute on OMSI's main/render thread through the native bridge. Texture handles are `d3dtex-<sessionId N>-<16 hex digits>`.

| Operation | Kind | Arguments | Result keys | Errors |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | none | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | required `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); optional `levels` (0..16, default 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | required `handle`; optional `level` (0..15, default 0) | the 13 `d3d.texture.create` keys, for the requested level | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, plus create errors |
| `d3d.texture.update` | Action | required `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (at most 48 KiB decoded); optional `level` (0..15, default 0), `x` (0..4095, default 0), `y` (0..4095, default 0) | the 13 `d3d.texture.create` keys after the update | `OL_E_D3D_INVALID_PIXEL_BUFFER`, plus describe errors |
| `d3d.texture.release` | Action | required `handle` | the 13 `d3d.texture.create` keys with `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` on repeated release, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Failed D3D operations return `detail` and `native_status` values. The `D3DRuntimeApi` extension methods (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) wrap these operations for API consumers.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` returns one `Capability(Name, Available, EvidenceState, Reason)` per catalog entry that is not `InternalOnly`. `Name` is the catalog id and `EvidenceState` its catalog evidence. `Available` is evaluated for the installation:

- `Unsupported` entries are never available; `Reason` carries the catalog note.
- Every entry needs the current platform, and every entry except `runtime.current-windows-x64` needs an OMSI 2 installation.
- An entry with native capabilities on the known layout reports `native: RESOLVED_KNOWN_LAYOUT`. On another OMSI 2 build the executable file is resolved statically: `native: STATICALLY_RESOLVED`, or `native: PENDING_IN_PROCESS_RESOLUTION` naming what the file did not resolve. A pending entry is still reported available, because the in-process resolution decides; if it fails, the operation or start fails with `OL_E_CAPABILITY_UNRESOLVED`.

The planner reports per-plan capabilities in `SessionPlan.RequiredCapabilities` and `SessionPlan.UnsupportedRequestedFeatures` with the same catalog ids (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). An unsupported requested feature carries its catalog evidence; a required native capability carries `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` or `PENDING_IN_PROCESS_RESOLUTION`.

## Known limitations

- Handles are session-scoped and reported as `OL_E_RUNTIME_OBJECT_HANDLE_STALE` once their object is gone. A vehicle handle is bound to its VMT, its definition and OMSI's `IDCode` (a process-unique number every map object gets at construction and never reused), so an object recreated at the same address is always told apart. Humans are pooled slots that live for the whole map session: a human handle stops resolving when the human despawns (`RenderMe` 0) or the slot is spawned again (new target station or departure).
- Results are bounded to the 64 KiB mailbox; every list is paged (see [pagination](#pagination)).
- No cross-tile vehicle relocation, no calendar writes, no headless PlayerVehicle assignment. String-variable writes, script triggers and weather writes are `EXPERIMENTAL` (runtime validation pending). See [known limitations](known-limitations.md).
