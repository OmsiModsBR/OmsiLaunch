# CLI Examples

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> British English edition of the [canonical page](../../../reference/cli-examples.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

Minimal, correct invocations of `OmsiLaunch.exe` for OmsiLaunch `0.1.0-beta4`, each with its expected process exit code and a note of what is mutated and restored. Every example runs from the OMSI installation root (`<OMSI_PATH>`) unless stated otherwise; the syntax is defined in the [CLI reference](cli.md) and the exit codes in [exit codes](exit-codes.md). `--json` may be added to any command to obtain the structured envelope.

## Conventions

- **Mutates**: files or OMSI state changed by the command. "session overlay" means a file snapshotted into the transaction, applied before OMSI starts and restored byte-for-byte when the session ends.
- **Restored**: what is undone when the session ends (normal stop, Ctrl+C, tray, `session stop`, `/observe-seconds`) or by recovery.
- Runtime writes (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) change OMSI's memory only; they are never reverted because OMSI is terminated at stop.
- Placeholders: `<OMSI_PATH>` is the OMSI 2 installation that contains the OmsiLaunch package (for example `C:\OMSI 2`); `<OTHER_OMSI_PATH>` another installation; `<SPEC_PATH>` and `<ITX_PATH>` a LaunchSpec file and an Internet Textures profile of yours; `<HANDLE>` is a handle printed by the preceding `list` or `create` command and `<BASE64>` is Base64 pixel data. Every other value is a literal that works on a standard OMSI 2 installation (`grundorf-quick` is the example profile defined on this page).
- Quote a path that contains spaces and do not end a quoted path with `\` (Windows argument parsing turns `\"` into a literal quote): `"C:\OMSI 2"`, not `"C:\OMSI 2\"`.
- Every command line on this page is parsed by the documentation gate (`tests/OmsiLaunch.DocumentationTests`, gate `examples`); the identity, discovery, planning and client examples were also executed against a real installation (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

## Identity and discovery (no session)

```text
OmsiLaunch.exe /version
```
Exit `0`. Prints `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. Mutates nothing.

```text
OmsiLaunch.exe profiles --json
```
Exit `0`. Lists the supported `Omsi.exe` hashes and their validation status. Mutates nothing.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Exit `0`. Public capability catalogue; `help <family>` filters it. Mutates nothing.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Exit `0` (both forms are identical). Reports running `Omsi.exe` processes and whether an OmsiLaunch owner answers for this installation. Mutates nothing.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Exit `0` (`2` for an unknown category). Read-only discovery; junction cycles are skipped. Mutates nothing. The `Identity` values printed here are the exact strings that `/map`, `/saved`, `/vehicle-scope` and a `LaunchSpec` expect (for example `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

## Planning and validation

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Exit `0` when the plan is `READY`, `1` when `NOT RUNNABLE` (for example `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Mutates nothing; OMSI is not started.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Exit `0`/`1` as above; `/validate` is an alias of `/plan`. The JSON is the raw `SessionPlan` (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Exit `1`. `/date`, `/time`, `/year`, `/weather*` and player-vehicle flags are accepted but not applied by this build; the plan carries `OL_E_CAPABILITY_UNAVAILABLE` and is not runnable.

```text
OmsiLaunch.exe /last /plan
```
Exit `1`. `LAST_MAP_STATE` is unavailable for this profile (`OL_E_CAPABILITY_UNAVAILABLE`).

## Starting sessions (owner mode)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Exit `0` when the session ends in `Completed`, `1` on `Failed` or a non-runnable plan. Mutates: session overlays `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_<lang>.bmp` (managed splash is the default), `closecheck` handling, the startup handoff. Restored: every overlay, byte-for-byte, when the session ends. The console stays attached until OMSI exits, the tray "End session" is confirmed, a client sends `session stop`, or Ctrl+C is pressed.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Exit `0`. Same as above, but the session is stopped 8 s after reaching `Running` (earlier on tray/pipe stop). Used by validation scripts.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Exit `0`/`1`. SAVED_SITUATION: map, time and position come from the `.osn` (`situations\Linie 5.osn` ships with OMSI 2 and starts on Berlin-Spandau with a player bus). The value is the situation identity printed by `/list:Situations` (installation-relative, case-insensitive); a bare file name such as `Linie 5.osn` does not resolve (`OL_E_SITUATION_NOT_FOUND`, exit `1`). `/map` or `/entrypoint-index` with `/saved` is rejected with exit `2`. Mutations and restore as for NEW_MAP. OMSI itself records the situation's map in `options.cfg` `[last_map]`; that OMSI write is not reverted unless a `/set` overlays `options.cfg` (see [transactions and recovery](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Exit `0`. Mutates: `options.cfg` (session overlay, semantic token/vector patch; CP1252 bytes preserved) plus the splash overlays. Restored: `options.cfg` and the splash files exactly (RV-005, RV-006). `/set:graphics.texture=...` exits `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` exits `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Exit `0`. Mutates: no splash overlay; only the startup handoff and `closecheck` handling. Restored: nothing to restore for the splash.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Exit `0` (`1` with `OL_E_SESSION_PRESENTATION_INVALID` when the directory or a BMP is missing or not 640x480 24-bit). Mutates: `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_PTB.bmp` from the custom directory (session overlay). Restored: both files exactly.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Exit `0`. Mutates: nothing on disk beyond the splash overlays; the in-process downloader is suppressed for the session.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Exit `0` (`2` with `OL_E_ITX_PROFILE_REQUIRED` if the profile is omitted; `1` for an invalid profile or a target outside `Texture\`). Mutates: `Texture\standard.itx` (session overlay); every target listed in the profile and `Texture\standard.ipr` are session deletions. Restored: overlay removed, deleted originals restored; files OMSI created at those paths during the session are removed as session by-products.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Exit `0`. Waits up to 300 s (+5 s) for `Running` instead of the default 180 s. `/shutdown-timeout:60` is accepted but has no effect in this build.

### Predefined session profile

Profile file `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Exit `0`. Mutates: `options.cfg` (preset settings, session overlay) and the splash overlays. Restored: all of them. Adding `/map:...` or `/set:graphics.maxFPS=60` exits `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); omitting `/predefined-profile-index` exits `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). The packaged example `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` shows the full schema, but as shipped its `new:` block requests `date`, `time` and `weather`, which this build cannot apply: planning it with `/new` yields `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); remove those keys before use.

### LaunchSpec file

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Exit `0`/`1`. The packaged example selects Grundorf, entrypoint index `1`, managed splash, native internet textures; `RootPath: "."` resolves to the executable's directory. Mutations as for the explicit NEW_MAP example. A spec with an unknown property exits `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); a missing file exits `6` (`OL_E_SPEC_NOT_FOUND`); a file above 1 MiB exits `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Exit `0`/`1`. The explicit installation `<OTHER_OMSI_PATH>` wins over the spec's `RootPath`; `/startup-timeout` overrides the spec's `Behavior.StartupTimeoutSeconds` only because it was given.

### Silent (detached) start

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Exit `0` as soon as `OmsiLaunchW.exe` has been started (`{"delegated": true, "host_process_id": <pid>}`); `7` if `OmsiLaunchW.exe` is missing (`OL_E_WINDOWS_HOST_MISSING`) or could not start. The launcher returns immediately and does not keep the caller's console or pipes open: a script that captures its output gets end-of-file at once (runtime closure `T04`). The session itself runs in `OmsiLaunchW.exe`: no console output, failures as message boxes, tray icon available. Check progress with `session status`, `events watch` and `.omsilaunch\diagnostics\<sessionId>-host.log`. Full reference: [OmsiLaunchW.exe](omsilaunchw.md).

## Controlling a running session (client mode)

Run these from the same installation directory while an owner is running. Each exits `4` (`OL_E_NO_ACTIVE_SESSION`) when no owner answers and `7` on a control error.

```text
OmsiLaunch.exe session status --json
```
Exit `0`. Returns `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Mutates nothing.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Exit `0` (`events watch` runs until Ctrl+C). Bounded runtime events (`gameplay.entered`, D3D lifecycle events, ...). Mutates nothing.

```text
OmsiLaunch.exe session stop
```
Exit `0` (`{"accepted": true, "session_id": "..."}`). Requests the canonical stop: OMSI is terminated, overlays are restored by the owner, the journal is deleted. The client returns immediately; the owner process exits after restore.

## Runtime reads

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Exit `0` with the `RuntimeCommandResult` (`Succeeded`, `Values`) in the envelope. Mutates nothing. 8 s timeout (`OL_E_RUNTIME_REQUEST_TIMEOUT`, exit `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Exit `0`; `2` when a required argument is missing (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, reported before the request is sent); `7` when the plugin rejects the request: `OL_E_RUNTIME_OPERATION_FAILED` with the specific reason in `Values.detail`, for example `OL_E_RUNTIME_OBJECT_HANDLE_STALE` for a handle that no longer identifies the same object or `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Handles are session-scoped and come from the preceding `list`. Variable, constant and curve names are defined by each vehicle model: take them from the `list` result. The names above were listed for `rv-000001`, the player bus of a `situations\Linie 5.osn` session. Mutates nothing.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Exit `0`. Operations without a hierarchical route, or any route, can be addressed by operation id. `timetable.track-entries.list` is a bounded list: on `situations\Linie 5.osn` it returned 137 of 825 entries with `truncated=true` (documentation audit runtime retest). Mutates nothing.

## Runtime writes

```text
OmsiLaunch.exe time set --minute=30
```
Exit `0`. Mutates OMSI's in-memory clock (validated: write, read-back, restore by a second `time set`). Not reverted at stop.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Exit `0` (`2` when `--family` is missing for `camera lock`). Mutates camera state for the session. `camera lock` needs a PlayerVehicle (for example a `/saved` session); in a headless `/new` session it fails (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` in `Values.detail`). Lock and unlock were runtime-validated with a saved situation (families 0, 2 and 1 with camera read-back). Not reverted at stop; the lock policy ends with the session.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Exit `0` (`2` if `handle`, `name` or `value` is missing). Mutates one numeric script variable of that vehicle. Not reverted.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Exit `0`. Writes OMSI's source weather record and stops live METAR updates; OMSI derives fog, light, actual wind and humidity from it on its next unpaused weather tick. Every value is checked before anything is written (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, exit `7`). A derived key such as `relative_humidity` returns `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` and names the key to set instead. Not reverted. EXPERIMENTAL: runtime validation pending.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Exit `0`. Switches to live METAR weather for that station; OMSI downloads it on its next weather update. EXPERIMENTAL: runtime validation pending.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Exit `0`. `scripts string set` goes through OMSI's own `SetStringVariable` and returns the value read back; a name the vehicle script does not declare returns `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (exit `7`). `scripts trigger` raises a trigger as OMSI's input handlers do (`1` press, `0` release); the vehicle script runs it at once. Trigger and variable names are defined by each vehicle model; the ones above are examples. Not reverted. EXPERIMENTAL: runtime validation pending.

## Spawn

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Exit `0` with the new `rv-NNNNNN` handle in `Values` (`2` when `--model` is missing; `7` on `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). 30 s client timeout. Mutates the road-vehicle collection (one vehicle added); does not assign the player vehicle. Not reverted; the vehicle disappears with OMSI at stop.

## D3D textures

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` is the `handle` value printed by `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` must decode to `width * height * 4` bytes for the 32-bit formats (8 x 8 x 4 = 256 bytes) and to at most 48 KiB. Exit `0`; `2` for missing required arguments; `7` for `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (second release, or describe after release), `OL_E_D3D_STALE_RESOURCE_HANDLE` (a handle from before a device reset, or from another session), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Creates GPU resources owned by the session; released explicitly or when OMSI ends. No files are touched.

## Owner-side single runtime operation

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Exit `0`. Starts a session, runs `time.read` once after `Running` (5 s timeout), writes `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, keeps running for 5 s, stops and restores. A runtime failure is printed as `runtime_error` and does not end the session.

## Recovery

```text
OmsiLaunch.exe /recovery-status --json
```
Exit `0`: `{"pending": false, ...}` when no journal exists, `{"pending": true, "recovered": false}` when one does. Exit `7` (`OL_E_INSTALLATION_BUSY`) while an owner holds the installation. Mutates nothing.

```text
OmsiLaunch.exe /recover --json
```
Exit `0` when nothing was pending or the restore completed (`recovered: true`; `diagnostics` may contain `restore.session-artifact-removed` and `OL_W_RESTORE_FOREIGN_FILE_RETAINED`); exit `8` when the journal was pending and still is (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); exit `7` while an owner or the journalled OMSI holds the installation (`OL_E_INSTALLATION_BUSY`); exit `10` (`OL_E_INTERNAL`) when the restored bytes fail verification (`Restore hash mismatch` or `Restore presence mismatch`; the journal stays pending). Mutates: restores every journalled file from `.omsilaunch\backup\<sessionId>\` after verifying its SHA-256, then deletes the journal and the backup directory. Refused with `OL_E_INSTALLATION_BUSY` while the journalled `Omsi.exe` is still alive (runtime closure `S04`, `S04b`, `F01`).

## Exit-code quick check (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
