# Runtime Capabilities

Status: IMPLEMENTATION INVENTORY (the normative, gate-checked catalog is
[`docs/reference/capabilities.md`](../reference/capabilities.md); the
authoritative list of public operation ids is
`PublicCapabilityRegistry.PublicRuntimeOperationIds`)

Runtime evidence in this table was recorded before and during the beta3
hardening round; rows updated after the runtime closure round cite its ids.
A row marked STATICALLY_VALIDATED here may have later runtime evidence; the
authoritative per-item state is
[`docs/status/runtime-validation-status.md`](../status/runtime-validation-status.md).

| Capability | API/channel | Interop | Static evidence | Runtime evidence | Release state |
| --- | --- | --- | --- | --- | --- |
| Session request/response | `ExecuteRuntimeAsync` | profile-independent mailbox | protocol and plugin tests | canonical batch PASS | RUNTIME_PASS |
| Stale-response protection | same mailbox | host-side request/response identity check | `runtime-command.late-response-ignored`: a response arriving after the host timeout is discarded and the next request is staged normally | not run | STATICALLY_VALIDATED |
| Public API boundary | `ExecuteRuntimeAsync` | `PublicCapabilityRegistry` | `api.runtime-rejects-internal-operations`: `internal.*` and unknown operations return `OL_E_RUNTIME_OPERATION_UNKNOWN` before session lookup; missing arguments `OL_E_RUNTIME_ARGUMENT_REQUIRED`; `internal_*`, `*_address`, `*_pointer`, `*_vmt` result keys stripped | not run | STATICALLY_VALIDATED |
| Session stop | `session.stop`, `StopAsync`, `CloseAsync`, tray, Ctrl+C | supervisor `TerminateProcess` | forced termination; OMSI shutdown routine does not run; `closecheck` handled as a session deletion | canonical sessions completed requested stop and exact restore | RUNTIME_PASS |
| Time read | `time.read` | `OmsiTimeAdapter` | pinned OmsiHook plus exact profile | PASS | RUNTIME_PASS |
| Map read | `map.read` | `OmsiRuntimeReaders` | pinned `OmsiMap` layout and corrected `OmsiGlobals.Map` slot | PASS in matrix RV-001 (session `e5454061-d0cb-494f-b5ee-f4c2e3876d92`): Grundorf identity, 19 loaded tiles, plausible metadata. `IMPLEMENTATION-STATUS.md` still records revalidation as required because the earlier wrong-slot read was structurally invalid; the discrepancy is documented, not resolved | RUNTIME_PASS (revalidation requested) |
| Map/tile graph | none | profile trial retained internally | upstream labels `OmsiMap+0x118` as `Kacheln`, but Current live memory did not expose a valid Delphi array header | `map.tiles.list` trial failed safely in sessions `ec85b51a-f69a-4f05-8fb3-638c63f9f20d`, `129482a4-b3a2-448a-830e-4043d11de246`, and `90011081-f5e9-4b31-b907-941bb3a9fd1a`; all cleaned normally | BLOCKED_PROFILE_LAYOUT |
| Weather read | `weather.read` | `OmsiRuntimeReaders` | pinned `OmsiWeather` and active-weather record layouts | PASS: base scalars plus temperature, dew point, pressure, precipitation and rate | RUNTIME_PASS |
| Actual/ICAO weather read | `weather.actual.read` | `OmsiRuntimeReaders` | pinned `OmsiActuWeather` layout | PASS | RUNTIME_PASS |
| Camera read | `camera.read` | `OmsiRuntimeReaders` | pinned wrapper and camera-family evidence | PASS | RUNTIME_PASS |
| Camera FOV mutation | `camera.set` | `OmsiCameraWriter` | pinned camera scalar layout | PASS: `45 -> 46 -> 45` | RUNTIME_PASS |
| Clock mutation | `time.set` | `OmsiTimeAdapter` + profiled `SetTime` | upstream clock fields and native call | PASS: minute `17 -> 18 -> 17` | RUNTIME_PASS |
| Scalar weather mutation | `weather.set` | withheld pending native apply lifecycle | derived and `ActWeather` wind candidates are profiled | FAIL: both candidates are overwritten by the next normal OMSI weather tick; requests are rejected with `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | UNAVAILABLE |
| Semantic presented entrypoint | gated | native `Tform_setpos` exact unique list match | profiled `FormShow`, list-item text and `Button1Click` flow | Partial: canonical run proved `presented index 1 -> raw index 0 -> Nordspitze Bauernhof`; the presented text representation remains unresolved and raw labels are duplicated | RUNTIME_PARTIAL |
| Saved situation startup | `WorldMode.SavedSituation` | profiled Start-form mode/selector/Button1Click sequence | `Tform_start+0x3E4/+0x3F8/+0x434/+0x438/+0x43C`, native `SetChecked`, VMT item-index setter, and `Button1Click` | PASS: `situations\\Baustelle Falkenseer Ch..osn` loaded Berlin-Spandau, reached `gameplay.entered`, remained RUNNING for 8 s, then completed requested stop and normal restore | RUNTIME_PASS |
| Calendar / actual ICAO mutation | none | Delphi string/ABI boundary incomplete | method/profile references | not run | RELEASE_IF_CLOSED |
| Road vehicle collection state | `road-vehicles.read` | profiled `OmsiMyOmsiList` state | pinned OmsiHook container | PASS: count/AI/player index | RUNTIME_PASS |
| Road vehicle detailed telemetry | `road-vehicles.list`, `road-vehicle.read`, `player-vehicle.read` | profiled `MemArrayList` and `OmsiRoadVehicleInst` snapshots | upstream layouts reconciled to `Omsi23004_692EBFBF` | PASS: opaque handle, position, rotation, controls, lighting and AI fields; no-vehicle request returns `present=false` | RUNTIME_PASS |
| Road vehicle / human handle lifetime | opaque `rv-NNNNNN` / `hb-NNNNNN` tokens | generation-scoped collection registry with object fingerprint (VMT plus definition/model index) | `interop.handle-reuse-rejected`: stable snapshot, stale rejection as `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, native-address reuse with and without an intervening list read; residual blind spot is same class and model recreated at the same address between two list reads | Runtime revalidation pending | STATICALLY_VALIDATED |
| Vehicle script variable read | `vehicle.variables.list`, `vehicle.variable.get` | profiled ANSI name table and public-value pointer array | pinned `OmsiComplMapObjInst.GetVariable` chain; profile uses instance `+0x23C` values | PASS: 1,025 names enumerated; `Refresh_Strings=0` | RUNTIME_PASS |
| Vehicle script variable mutation | `vehicle.variable.set` | profiled ANSI name table and public-value pointer array | pinned `OmsiComplMapObjInst.SetVariable` chain; finite float input, live opaque handle validation and immediate read-back | PASS: `Refresh_Strings 0 -> 1 -> 0`; normal cleanup PASS | RUNTIME_PASS |
| Vehicle string-variable read | `vehicle.string-variables.list`, `vehicle.string-variable.get` | profiled ANSI name table and Unicode value array | pinned `OmsiComplMapObjInst.GetStringVariable` chain | PASS: 27 names enumerated; `ident=GRN-V 30` | RUNTIME_PASS |
| Vehicle constants | `vehicle.constants.list`, `vehicle.constant.get` | profiled `ScriptConstants` block | pinned `OmsiConstBlock` constant/name arrays | PASS: `AI_lights_blinkgeberintervall = 0.3`; normal cleanup PASS | RUNTIME_PASS |
| Vehicle curves | `vehicle.curves.list`, `vehicle.curve.evaluate` | profiled `OmsiConstBlock` function and point arrays | pinned curve clamp/linear interpolation semantics | PASS: `AI_Wandler_last(0) = 1300`; normal cleanup PASS | RUNTIME_PASS |
| Vehicle HOF metadata | `vehicle.hofs.read` | profiled vehicle-definition HOF array | pinned `OmsiHOF` UTF-16 name plus ANSI service trip | PASS: 11 entries, including `Grundorf` and `Betriebsfahrt`; normal cleanup PASS | RUNTIME_PASS |
| Drivers | `drivers.read` | profiled driver array | pinned `OmsiDriver` fields | PASS: one `OMSI-Fan` snapshot; normal cleanup PASS | RUNTIME_PASS |
| Ticket pack | `tickets.read` | profiled ticket-pack/ticket records | pinned `OmsiTicketPack`/`OmsiTicket` fields | PASS: five `Berlin_1` ticket records; normal cleanup PASS | RUNTIME_PASS |
| Timetable logs | `timetable.logs.read` | profiled dynamic log array | pinned `OmsiTimeTableLog` record | PASS: valid empty Grundorf collection; normal cleanup PASS | RUNTIME_PASS |
| Place random bus | `road-vehicles.place-random` | profiled native bridge | upstream `TProgMan.PlaceRandomBus` ABI | PASS: raw return `2`, RoadVehicles `2 -> 4`; normal cleanup PASS | RUNTIME_PASS |
| Spawn basic road vehicle | `road-vehicles.spawn` | profiled MakeVehicle bridge | exact one-object collection delta and profiled VMT validation | PASS: public external route remained responsive during native call; count `2 -> 3`, opaque `rv-000003` resolved across repeated reads | RUNTIME_PASS |
| Camera family/preset lock | `camera.lock`, `camera.unlock` | session-scoped in-process policy | current-profile family selector and active driver/passenger preset offsets | PASS: runtime closure `CAM01`, families 0, 2 and 1 with read-back, then unlock (needs a PlayerVehicle) | RUNTIME_VALIDATED |
| Named vehicle/object triggers | none | none | upstream methods require unresolved managed Delphi-string ownership proof | not run | BLOCKED_STRING_OWNERSHIP |
| Sound triggers | none | none | upstream method requires unresolved managed Delphi-string ownership proof | not run | BLOCKED_STRING_OWNERSHIP |
| Human collection count | `humans.read` | Delphi pointer array | pinned OmsiHook global | PASS: 408 | RUNTIME_PASS |
| Human detailed telemetry | `humans.list`, `human.read` | profiled `OmsiHumanBeingInst` snapshots | upstream layouts reconciled to `Omsi23004_692EBFBF` | PASS: opaque handle, movement, target, ticket, seat, station and AI fields | RUNTIME_PASS |
| Timetable manager counts | `timetable.read` | profiled arrays | pinned `OmsiTimeTableMan` | PASS: tracks/trips/stops/lines | RUNTIME_PASS |
| Timetable Tracks / Trips / Lines | `timetable.tracks.list`, `timetable.trips.list`, `timetable.lines.list` | profiled fixed-width `OmsiTT*Internal` records | pinned timetable records reconciled to `Omsi23004_692EBFBF` | PASS: 3 tracks, 3 trips and 2 lines; `76_BH-Kk` / `76` identities | RUNTIME_PASS |
| Timetable BusStops / StationLinks | `timetable.bus-stops.list`, `timetable.station-links.list` | profiled fixed-width `OmsiTTBusstopListEntryInternal` and `OmsiTTStnLinkInternal` records | pinned timetable records reconciled to `Omsi23004_692EBFBF` | PASS: 13 bus stops including `Nordspitze`; 14 station links | RUNTIME_PASS |
| Timetable Tours | `timetable.tours.list` | profiled nested `OmsiTTTourInternal` arrays under Lines | pinned timetable records reconciled to `Omsi23004_692EBFBF` | PASS: 3 tours; line `0`, tour `1`, AI group `Busses`, 72 TourEntries | RUNTIME_PASS |
| Timetable Profiles | `timetable.profiles.list` | profiled nested `OmsiTTProfileInternal` arrays under Trips | pinned timetable records reconciled to `Omsi23004_692EBFBF` | PASS: 3 profiles; `standard`, total time 420, 8 stop times | RUNTIME_PASS |
| Timetable TourEntries | `timetable.tour-entries.list` | profiled nested `OmsiTTTourEntryInternal` arrays under Tours | pinned timetable records reconciled to `Omsi23004_692EBFBF` | PASS: 130 entries; `76_BH-Kk`, trip/profile 0, 14820 to 15240 | RUNTIME_PASS |
| Timetable TrackEntries | `timetable.track-entries.list` | profiled nested `OmsiTTTrackEntryInternal` arrays under Tracks | pinned fixed-width TrackEntry record reconciled to `Omsi23004_692EBFBF`; bounded at 512 entries | PASS: 91 entries; first ID `99`, tile `4`, distance `0` in 93 ms | RUNTIME_PASS |
| Timetable RVFiles | `timetable.rv-files.list` | profiled `OmsiRVFileInternal` records | pinned date/line/list/probability layout reconciled to `Omsi23004_692EBFBF` | PASS: valid empty array on Grundorf in 62 ms | RUNTIME_PASS |
| Timetable NoRVNumbers | none | none | upstream `raw:true` array access conflicts with this profile's observed `+0x24` layout | FAIL: invalid dynamic-array header then invalid raw address; canonical cleanup PASS | UNRESOLVED |
| Vehicle string-variable mutation | not announced | Delphi-string assignment/refcount boundary pending | upstream native methods | not run | BLOCKED_STRING_OWNERSHIP |
| D3D device status | typed `D3DRuntimeApi` / `d3d.status` | profile slot, guarded QI and cooperative-level observer | pinned DXHook plus exact-build slot validation | PASS: `S_OK`, READY, one owned reference, reset hook installed | RUNTIME_PASS |
| D3D texture create/describe | `d3d.texture.create`, `d3d.texture.describe` (typed opaque `D3DTextureHandle`) | bounded native registry, dynamic default-pool texture | pinned texture semantics reconciled to Current | PASS: two formats, level metadata, multiple handles | RUNTIME_PASS |
| D3D texture update | `d3d.texture.update` (typed `D3DTextureUpdate`) | `GetLevelDesc`, bounds, `LockRect`, pitch-aware copy, `UnlockRect` | pinned DXHook behavior without its ownership defects | PASS: full and rectangular updates returned `S_OK` | RUNTIME_PASS |
| D3D texture release | `d3d.texture.release` | atomic retirement and exactly-once COM release | owned-reference graph and generation model | PASS: release, repeated-release rejection and three reuse cycles | RUNTIME_PASS |
| D3D lifecycle events | public `RuntimeEvents` (`d3d.ready/lost/resetting/restored`) | Reset vtable observer plus ordered fixed transition queue | Current-build Reset slot ABI and D3D9 reset contract | `d3d.ready` PASS; `d3d.resetting`/`d3d.restored` PASS in runtime closure `D01` (two OMSI-driven resets); a distinct `lost` transition was not produced | RUNTIME_VALIDATED (except `lost`) |
| D3D stale-handle rejection | session-tagged opaque handle plus device generation | native generation and resource state checks | fixed-width session channel and default-pool reset policy | PASS for released and prior-session handles (`H02`) and for reset invalidation (`D01`, `OL_E_D3D_STALE_RESOURCE_HANDLE`) | RUNTIME_VALIDATED |

## Wave D State

OmsiLaunch owns the D3D9 implementation. It reads the profile-specific device
slot, retains exactly one `QueryInterface<IDirect3DDevice9>` reference, runs
commands through the existing OMSI UI/main/render-owner timer, and exposes no
COM pointer. Default-pool textures are invalidated before observed Reset and
old generations never retarget a replacement resource. Normal texture work,
StopSession, forced exit, relaunch and stale-session rejection are runtime
proven. Two OMSI-driven device resets (runtime closure `D01`, produced by a
test-only display-mode change; no synthetic Reset was invoked) proved the
resetting/restored transitions, generation advance, `OL_E_D3D_RESET_IN_PROGRESS`
during the reset and stale-handle rejection afterwards. A distinct device-loss
transition was not produced (OMSI went straight to `DEVICENOTRESET`).

Unannounced operations are rejected; capability discovery must not imply that a
raw address is safe to write or call.
