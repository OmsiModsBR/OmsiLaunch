# Ejemplos de la CLI

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Traducción de la [página original en inglés](../../../reference/cli-examples.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Invocaciones mínimas y correctas de `OmsiLaunch.exe` para OmsiLaunch `0.1.0-beta4`, cada una con su código de salida esperado del proceso y una nota sobre qué se modifica y qué se restaura. Salvo que se indique lo contrario, cada ejemplo se ejecuta desde la raíz de la instalación de OMSI (`<OMSI_PATH>`); la sintaxis se define en la [referencia de la CLI](cli.md) y los códigos de salida en [códigos de salida](exit-codes.md). Se puede agregar `--json` a cualquier comando para obtener el envoltorio estructurado.

<a id="conventions"></a>
## Convenciones

- **Modifica**: archivos o estado de OMSI que el comando cambia. "Overlay de sesión" significa un archivo del que se toma un snapshot en la transacción, que se aplica antes de que OMSI inicie y que se restaura byte por byte cuando la sesión finaliza.
- **Restaura**: lo que se deshace cuando la sesión finaliza (detención normal, Ctrl+C, bandeja, `session stop`, `/observe-seconds`) o mediante la recuperación.
- Las escrituras de runtime (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) solo cambian la memoria de OMSI; nunca se revierten, porque OMSI se termina en la detención.
- Marcadores de posición: `<OMSI_PATH>` es la instalación de OMSI 2 que contiene el paquete de OmsiLaunch (por ejemplo, `C:\OMSI 2`); `<OTHER_OMSI_PATH>`, otra instalación; `<SPEC_PATH>` y `<ITX_PATH>`, un archivo LaunchSpec y un perfil de Internet Textures propios; `<HANDLE>` es un handle impreso por el comando `list` o `create` anterior y `<BASE64>` son datos de píxeles en Base64. Todos los demás valores son literales que funcionan en una instalación estándar de OMSI 2 (`grundorf-quick` es el perfil de ejemplo definido en esta página).
- Ponga entre comillas las rutas que contengan espacios y no termine una ruta entre comillas con `\` (el análisis de argumentos de Windows convierte `\"` en una comilla literal): `"C:\OMSI 2"`, no `"C:\OMSI 2\"`.
- Cada línea de comandos de esta página la analiza el gate de documentación (`tests/OmsiLaunch.DocumentationTests`, gate `examples`); los ejemplos de identidad, descubrimiento, planificación y cliente también se ejecutaron contra una instalación real (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identidad y descubrimiento (sin sesión)

```text
OmsiLaunch.exe /version
```
Salida `0`. Imprime `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. No modifica nada.

```text
OmsiLaunch.exe profiles --json
```
Salida `0`. Enumera los hashes de `Omsi.exe` compatibles y su estado de validación. No modifica nada.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Salida `0`. Catálogo público de capacidades; `help <family>` lo filtra. No modifica nada.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Salida `0` (ambas formas son idénticas). Informa los procesos `Omsi.exe` en ejecución y si un propietario de OmsiLaunch responde para esta instalación. No modifica nada.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Salida `0` (`2` para una categoría desconocida). Descubrimiento de solo lectura; los ciclos de junctions se omiten. No modifica nada. Los valores `Identity` impresos aquí son exactamente las cadenas que esperan `/map`, `/saved`, `/vehicle-scope` y un `LaunchSpec` (por ejemplo, `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Planificación y validación

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Salida `0` cuando el plan está `READY`, `1` cuando está `NOT RUNNABLE` (por ejemplo, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). No modifica nada; OMSI no se inicia.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Salida `0`/`1` como en el caso anterior; `/validate` es un alias de `/plan`. El JSON es el `SessionPlan` sin procesar (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Salida `1`. `/date`, `/time`, `/year`, `/weather*` y los flags del vehículo del jugador se aceptan, pero este build no los aplica; el plan incluye `OL_E_CAPABILITY_UNAVAILABLE` y no es ejecutable.

```text
OmsiLaunch.exe /last /plan
```
Salida `1`. `LAST_MAP_STATE` no está disponible para este perfil (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Inicio de sesiones (modo propietario)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Salida `0` cuando la sesión finaliza en `Completed`, `1` en `Failed` o con un plan no ejecutable. Modifica: overlays de sesión `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_<lang>.bmp` (el splash administrado es el valor por defecto), el manejo de `closecheck` y el handoff de arranque. Restaura: cada overlay, byte por byte, cuando la sesión finaliza. La consola permanece conectada hasta que OMSI termina, se confirma "End session" (finalizar sesión) en la bandeja, un cliente envía `session stop` o se presiona Ctrl+C.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Salida `0`. Igual que el anterior, pero la sesión se detiene 8 s después de alcanzar `Running` (antes, ante una detención desde la bandeja o por pipe). Lo usan los scripts de validación.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Salida `0`/`1`. SAVED_SITUATION: el mapa, la hora y la posición provienen del `.osn` (`situations\Linie 5.osn` viene incluido con OMSI 2 y comienza en Berlin-Spandau con un autobús del jugador). El valor es la identidad de la situación impresa por `/list:Situations` (relativa a la instalación, sin distinguir mayúsculas de minúsculas); un nombre de archivo simple como `Linie 5.osn` no se resuelve (`OL_E_SITUATION_NOT_FOUND`, salida `1`). `/map` o `/entrypoint-index` junto con `/saved` se rechaza con salida `2`. Modificaciones y restauración como en NEW_MAP. El propio OMSI registra el mapa de la situación en `[last_map]` de `options.cfg`; esa escritura de OMSI no se revierte, salvo que un `/set` superponga `options.cfg` (consulte [transacciones y recuperación](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Salida `0`. Modifica: `options.cfg` (overlay de sesión, parche semántico de tokens/vectores; se conservan los bytes CP1252) más los overlays de splash. Restaura: `options.cfg` y los archivos de splash exactamente (RV-005, RV-006). `/set:graphics.texture=...` sale con `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` sale con `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Salida `0`. Modifica: ningún overlay de splash; solo el handoff de arranque y el manejo de `closecheck`. Restaura: no hay nada que restaurar en cuanto al splash.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Salida `0` (`1` con `OL_E_SESSION_PRESENTATION_INVALID` cuando falta el directorio o un BMP, o no es de 640x480 y 24 bits). Modifica: `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_PTB.bmp` a partir del directorio personalizado (overlay de sesión). Restaura: ambos archivos exactamente.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Salida `0`. Modifica: nada en disco aparte de los overlays de splash; el descargador dentro del proceso se suprime durante la sesión.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Salida `0` (`2` con `OL_E_ITX_PROFILE_REQUIRED` si se omite el perfil; `1` para un perfil no válido o un destino fuera de `Texture\`). Modifica: `Texture\standard.itx` (overlay de sesión); cada destino enumerado en el perfil y `Texture\standard.ipr` son eliminaciones de la sesión. Restaura: se quita el overlay y se restauran los originales eliminados; los archivos que OMSI creó en esas rutas durante la sesión se eliminan como subproductos de la sesión.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Salida `0`. Espera hasta 300 s (+5 s) a `Running` en lugar de los 180 s por defecto. `/shutdown-timeout:60` se acepta, pero no tiene efecto en este build.

<a id="predefined-session-profile"></a>
### Perfil de sesión predefinido

Archivo de perfil `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

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
Salida `0`. Modifica: `options.cfg` (ajustes del preset, overlay de sesión) y los overlays de splash. Restaura: todos ellos. Agregar `/map:...` o `/set:graphics.maxFPS=60` sale con `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); omitir `/predefined-profile-index` sale con `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). El ejemplo empaquetado `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` muestra el esquema completo, pero tal como se distribuye su bloque `new:` solicita `date`, `time` y `weather`, que este build no puede aplicar: planificarlo con `/new` da como resultado `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); quite esas claves antes de usarlo.

<a id="launchspec-file"></a>
### Archivo LaunchSpec

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Salida `0`/`1`. El ejemplo empaquetado selecciona Grundorf, el índice de punto de entrada `1`, splash administrado e Internet Textures nativas; `RootPath: "."` se resuelve al directorio del archivo ejecutable. Modificaciones como en el ejemplo explícito de NEW_MAP. Un spec con una propiedad desconocida sale con `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); un archivo inexistente sale con `6` (`OL_E_SPEC_NOT_FOUND`); un archivo de más de 1 MiB sale con `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Salida `0`/`1`. La instalación explícita `<OTHER_OMSI_PATH>` prevalece sobre el `RootPath` del spec; `/startup-timeout` sobrescribe el `Behavior.StartupTimeoutSeconds` del spec solo porque se indicó.

<a id="silent-detached-start"></a>
### Inicio silencioso (desacoplado)

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Salida `0` en cuanto se inició `OmsiLaunchW.exe` (`{"delegated": true, "host_process_id": <pid>}`); `7` si falta `OmsiLaunchW.exe` (`OL_E_WINDOWS_HOST_MISSING`) o no se pudo iniciar. El lanzador regresa de inmediato y no mantiene abiertos la consola ni los pipes del llamador: un script que captura su salida recibe fin de archivo al instante (cierre de runtime `T04`). La sesión en sí se ejecuta en `OmsiLaunchW.exe`: sin salida de consola, errores como cuadros de mensaje e ícono de la bandeja disponible. Consulte el progreso con `session status`, `events watch` y `.omsilaunch\diagnostics\<sessionId>-host.log`. Referencia completa: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Control de una sesión en ejecución (modo cliente)

Ejecute estos comandos desde el mismo directorio de instalación mientras un propietario está en ejecución. Cada uno sale con `4` (`OL_E_NO_ACTIVE_SESSION`) cuando ningún propietario responde y con `7` ante un error de control.

```text
OmsiLaunch.exe session status --json
```
Salida `0`. Devuelve `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. No modifica nada.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Salida `0` (`events watch` se ejecuta hasta Ctrl+C). Eventos de runtime acotados (`gameplay.entered`, eventos del ciclo de vida de D3D, ...). No modifica nada.

```text
OmsiLaunch.exe session stop
```
Salida `0` (`{"accepted": true, "session_id": "..."}`). Solicita la detención canónica: OMSI se termina, el propietario restaura los overlays y se elimina el journal. El cliente regresa de inmediato; el proceso propietario termina después de la restauración.

<a id="runtime-reads"></a>
## Lecturas de runtime

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
Salida `0` con el `RuntimeCommandResult` (`Succeeded`, `Values`) en el envoltorio. No modifica nada. Timeout de 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, salida `7`).

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
Salida `0`; `2` cuando falta un argumento obligatorio (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, se informa antes de enviar la solicitud); `7` cuando el plugin rechaza la solicitud: `OL_E_RUNTIME_OPERATION_FAILED` con el motivo específico en `Values.detail`, por ejemplo, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` para un handle que ya no identifica el mismo objeto, u `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Los handles tienen alcance de sesión y provienen del `list` anterior. Los nombres de variables, constantes y curvas los define cada modelo de vehículo: tómelos del resultado de `list`. Los nombres anteriores se enumeraron para `rv-000001`, el autobús del jugador de una sesión de `situations\Linie 5.osn`. No modifica nada.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Salida `0`. Las operaciones sin ruta jerárquica, o cualquier ruta, se pueden invocar por id de operación. `timetable.track-entries.list` es una lista acotada: en `situations\Linie 5.osn` devolvió 137 de 825 entradas con `truncated=true` (nueva prueba en runtime de la auditoría de documentación). No modifica nada.

<a id="runtime-writes"></a>
## Escrituras de runtime

```text
OmsiLaunch.exe time set --minute=30
```
Salida `0`. Modifica el reloj en memoria de OMSI (validado: escritura, relectura y restauración mediante un segundo `time set`). No se revierte en la detención.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Salida `0` (`2` cuando falta `--family` en `camera lock`). Modifica el estado de la cámara durante la sesión. `camera lock` necesita un PlayerVehicle (por ejemplo, una sesión `/saved`); en una sesión `/new` headless (sin vehículo del jugador) falla (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` en `Values.detail`). El bloqueo y el desbloqueo se validaron en runtime con una situación guardada (familias 0, 2 y 1, con relectura de la cámara). No se revierte en la detención; la política de bloqueo termina con la sesión.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Salida `0` (`2` si falta `handle`, `name` o `value`). Modifica una variable numérica de script de ese vehículo. No se revierte.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Salida `0`. Escribe el registro de clima de origen de OMSI y detiene las actualizaciones de METAR en vivo; OMSI deriva la niebla, la luz, el viento actual y la humedad a partir de él en su siguiente ciclo de clima sin pausa. Cada valor se comprueba antes de escribir nada (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, salida `7`). Una clave derivada, como `relative_humidity`, devuelve `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` e indica la clave que debe establecerse en su lugar. No se revierte. EXPERIMENTAL: validación en runtime pendiente.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Salida `0`. Cambia al clima METAR en vivo de esa estación; OMSI lo descarga en su siguiente actualización de clima. EXPERIMENTAL: validación en runtime pendiente.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Salida `0`. `scripts string set` pasa por el `SetStringVariable` propio de OMSI y devuelve el valor leído de vuelta; un nombre que el script del vehículo no declara devuelve `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (salida `7`). `scripts trigger` activa un trigger como lo hacen los manejadores de entrada de OMSI (`1` presionar, `0` soltar); el script del vehículo lo ejecuta de inmediato. Los nombres de triggers y de variables los define cada modelo de vehículo; los anteriores son ejemplos. No se revierte. EXPERIMENTAL: validación en runtime pendiente.

## Spawn

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Salida `0` con el nuevo handle `rv-NNNNNN` en `Values` (`2` cuando falta `--model`; `7` ante `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Timeout de 30 s en el cliente. Modifica la colección de vehículos de tráfico (se agrega un vehículo); no asigna el vehículo del jugador. No se revierte; el vehículo desaparece con OMSI en la detención.

<a id="d3d-textures"></a>
## Texturas D3D

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` es el valor `handle` impreso por `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` debe decodificarse en `width * height * 4` bytes para los formatos de 32 bits (8 x 8 x 4 = 256 bytes) y en 48 KiB como máximo. Salida `0`; `2` cuando faltan argumentos obligatorios; `7` ante `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (segunda liberación, o describe después de la liberación), `OL_E_D3D_STALE_RESOURCE_HANDLE` (un handle anterior a un reinicio del dispositivo, o de otra sesión), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Crea recursos de GPU propiedad de la sesión; se liberan explícitamente o cuando OMSI termina. No se modifica ningún archivo.

<a id="owner-side-single-runtime-operation"></a>
## Operación de runtime única del lado del propietario

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Salida `0`. Inicia una sesión, ejecuta `time.read` una vez después de `Running` (timeout de 5 s), escribe `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, sigue en ejecución durante 5 s, se detiene y restaura. Un error de runtime se imprime como `runtime_error` y no finaliza la sesión.

<a id="recovery"></a>
## Recuperación

```text
OmsiLaunch.exe /recovery-status --json
```
Salida `0`: `{"pending": false, ...}` cuando no existe ningún journal, `{"pending": true, "recovered": false}` cuando existe uno. Salida `7` (`OL_E_INSTALLATION_BUSY`) mientras un propietario mantiene la instalación. No modifica nada.

```text
OmsiLaunch.exe /recover --json
```
Salida `0` cuando no había nada pendiente o la restauración se completó (`recovered: true`; `diagnostics` puede contener `restore.session-artifact-removed` y `OL_W_RESTORE_FOREIGN_FILE_RETAINED`); salida `8` cuando el journal estaba pendiente y sigue estándolo (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); salida `7` mientras un propietario o el OMSI registrado en el journal mantiene la instalación (`OL_E_INSTALLATION_BUSY`); salida `10` (`OL_E_INTERNAL`) cuando los bytes restaurados no superan la verificación (`Restore hash mismatch` o `Restore presence mismatch`; el journal sigue pendiente). Modifica: restaura cada archivo registrado en el journal desde `.omsilaunch\backup\<sessionId>\` después de verificar su SHA-256 y luego elimina el journal y el directorio de copias de seguridad. Se rechaza con `OL_E_INSTALLATION_BUSY` mientras el `Omsi.exe` registrado en el journal siga activo (cierre de runtime `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Comprobación rápida del código de salida (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
