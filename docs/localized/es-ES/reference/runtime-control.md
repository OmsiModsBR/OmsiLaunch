# Control de runtime

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Traducción de la [página original en inglés](../../../reference/runtime-control.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

El control de runtime es el conjunto de operaciones de lectura, escritura y acción que OmsiLaunch ejecuta dentro de una sesión de OMSI en ejecución. Esta página explica las tres formas de acceder a él (API del propietario, cliente de la CLI, plano de control local), cómo viajan las solicitudes desde el llamador hasta el plugin y de vuelta, cómo funcionan los handles y las identidades de solicitud, qué timeouts se aplican, qué no se registra deliberadamente en el diario y las convenciones de argumentos y resultados, con un ejemplo práctico para cada familia. El inventario de operaciones propiamente dicho, con argumentos, claves del resultado y errores, está en [capacidades](capabilities.md). Fuentes: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` y `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` y `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Tres puntos de entrada

| Punto de entrada | Quién | Ruta | Timeout |
| --- | --- | --- | --- |
| API del propietario | Un integrador que inició la sesión dentro de su propio proceso con `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> validación del registro -> búsqueda de la sesión -> buzón | `TimeSpan` proporcionado por el llamador |
| CLI del propietario (`/runtime:<op>`) | El proceso `OmsiLaunch.exe` que es propietario de la sesión | una operación ejecutada justo después de `Running`, con el resultado escrito en `.omsilaunch\diagnostics\<session>-runtime-operation.json` y en la consola; la sesión continúa | 5 s (15 s para `road-vehicles.spawn`) |
| Cliente de la CLI | Cualquier invocación de `OmsiLaunch.exe` **sin** argumento de instalación, por ejemplo `OmsiLaunch.exe time get` | canalización con nombre `runtime.execute` hacia el propietario de la instalación en la que se encuentra el ejecutable -> el propietario llama a `ExecuteRuntimeAsync` | 8 s (30 s para `road-vehicles.spawn`) tanto en el lado del cliente como en el del propietario |
| Plano de control local | Cualquier proceso del mismo usuario de Windows | el mismo protocolo de canalización que el cliente de la CLI; consulta [control local](local-control.md) | como el anterior |

Todas las rutas terminan en `ExecuteRuntimeAsync`, que aplica el límite público en este orden:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: una operación que no está en `PublicRuntimeOperationIds` (incluida cualquier operación `internal.*`) devuelve `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; un argumento obligatorio ausente o en blanco devuelve `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Ninguno de los dos casos toca la sesión.
2. Búsqueda de la sesión (`KeyNotFoundException` para un handle desconocido), `OL_E_RUNTIME_SESSION_MISMATCH` cuando `RuntimeCommand.SessionId` difiere del handle, `OL_E_SESSION_NOT_RUNNING` salvo que el estado sea `Running`.
3. Solicitud al buzón (véase más abajo); después, `ScrubInternalValues` elimina cualquier clave del resultado que empiece por `internal_` o termine en `_address`, `_pointer`, `_vmt`.

El cliente de la CLI y el plano de control ejecutan ellos mismos el paso 1 antes de contactar con el propietario, de modo que un nombre de comando no válido se notifica con el código de salida 2 incluso cuando no hay ninguna sesión activa (`OL_E_RUNTIME_OPERATION_UNKNOWN` y `OL_E_RUNTIME_ARGUMENT_REQUIRED` se corresponden con `InvalidArguments`; los demás rechazos, con 7; la ausencia de propietario, con 4).

El endpoint del plano de control solo existe mientras el propietario está en `Running`, después del arranque y después de que haya terminado cualquier harness `/runtime-batch`, `/runtime-write-batch` o `/d3d-batch`. Los comandos que modifican (`runtime.execute`, `session.stop`) deben llevar el `session_id` activo; la CLI lo obtiene automáticamente de `session.status` (en caso contrario, `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Identidad de la solicitud y el buzón

Un `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` se serializa en un envelope `RuntimeCommandWire` (magic `OLRC`, versión 1, cabecera de 72 bytes, SHA-256 del payload JSON) y se deposita en el buzón de un solo vuelo de 64 KiB de la sesión (`OmsiLaunch.Runtime.<sessionId>`). El plugin sondea el buzón cada 50 ms en el thread de la UI de OMSI, ejecuta allí la operación y escribe la respuesta.

Reglas que hacen robusto el canal:

| Regla | Efecto |
| --- | --- |
| Un solo vuelo | Una solicitud a la vez por sesión; el host serializa a los llamadores con un semáforo. Un slot que sigue en `requested` cuando llega una solicitud nueva da `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Vinculación a la sesión | El plugin ignora (y borra) una solicitud cuyo GUID de sesión no es el suyo; el host rechaza una respuesta cuyo id de sesión o de solicitud no coincide (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Cuando vence el plazo, el host devuelve el slot al estado inactivo y lanza `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Respuesta tardía | El plugin publica una respuesta solo si el slot sigue conteniendo el id de su solicitud; la respuesta a una solicitud abandonada se descarta. Si aun así llega una, la siguiente solicitud del host encuentra un slot `responded` obsoleto, lo descarta y continúa; si esa respuesta obsoleta lleva el **mismo** id de solicitud que la nueva solicitud, el host lanza `OL_E_RUNTIME_REQUEST_ID_REUSED`. Por tanto, los llamadores no deben reutilizar nunca un id de solicitud dentro de una sesión. |
| Tamaño | Una solicitud mayor que el slot se rechaza antes de depositarse (`ArgumentOutOfRangeException`). El plugin acorta una página de lista que no cabe (se descartan las últimas filas, `truncated=true`, `returned_count` menor, `next_cursor` en la primera fila descartada); cualquier otra respuesta sobredimensionada se sustituye por un error tipado `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Canal cerrado | Una vez finalizada la sesión, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Los ids de solicitud son valores `ulong` elegidos por el llamador. La CLI usa rangos fijos: `10_001` para `/runtime:`, `50_001+` para las solicitudes reenviadas del plano de control, `1+` para el lote de lectura, `20_000+` para el lote D3D, `30_000+` para `D3DRuntimeApi`. Los integradores deberían usar un contador monótonamente creciente por sesión.

<a id="handles-and-stale-detection"></a>
## Handles y detección de handles obsoletos

| Prefijo | Tipo | Emitido por | Formato |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + seis dígitos decimales (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + seis dígitos decimales |
| `d3dtex-` | Textura D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Los handles son opacos y están ligados a la sesión: los genera el plugin, nunca codifican una dirección y carecen de significado en otra sesión. Cuando se resuelve un handle, el plugin comprueba que la dirección a la que corresponde sigue estando en la colección activa de OMSI (según la última lectura de lista; `road-vehicles.list` y `humans.list` actualizan la vista) y vuelve a leer una huella del objeto (la VMT de Delphi más el puntero de definición del vehículo, o el índice de modelo del humano). Una discrepancia significa que el objeto nativo se destruyó y su dirección se reutilizó: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Para el mismo objeto activo se devuelve de nuevo el mismo token en lecturas de lista repetidas. Punto ciego residual: un objeto de la misma clase y definición recreado en la misma dirección entre dos lecturas de lista no se puede distinguir. Los handles D3D los valida el puente nativo: un handle desconocido o de otra sesión da `OL_E_D3D_STALE_RESOURCE_HANDLE`, uno liberado `OL_E_D3D_RESOURCE_RELEASED`, y un cambio de generación del dispositivo marca las texturas como `STALE`.

Los handles nunca son direcciones nativas y nunca deben analizarse, compararse numéricamente, conservarse de una sesión a otra ni pasarse a otra sesión: trátalos como cadenas opacas válidas para la sesión que los emitió.

Evidencia de runtime: un handle D3D liberado sigue siendo rechazado después de crear texturas nuevas, y la siguiente sesión rechaza un handle de una sesión anterior (cierre de runtime `H02`); un reset del dispositivo D3D invalida todas las texturas activas (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). La detección de handles obsoletos de RoadVehicle y Human tras la eliminación natural (RV-002) no tiene un productor seguro en runtime (OMSI no eliminó objetos en las ventanas de observación y ninguna operación pública elimina uno); está cubierta offline.

<a id="what-is-not-journaled"></a>
## Qué no se registra en el diario

Las modificaciones de runtime solo cambian la memoria de OMSI. **No se registran en el diario de la transacción ni se restauran** al finalizar la sesión: `time.set`, `camera.set`, `camera.lock` (la política se detiene cuando el plugin se cierra), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` y todos los recursos `d3d.texture.*`. Desaparecen con el proceso de OMSI, que OmsiLaunch termina al final de la sesión sin permitir que OMSI persista nada (consulta [transacciones y recuperación](../concepts/transactions-and-recovery.md)). Nada de la familia de runtime toca el sistema de archivos.

<a id="argument-and-result-conventions"></a>
## Convenciones de argumentos y resultados

- Los argumentos son pares clave/valor de cadenas. En la CLI, `--key=value` después de las palabras de comando se convierte en un argumento de runtime (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` es el equivalente para `/runtime:<op>`. Los números usan la referencia cultural invariable (separador decimal `.`); los booleanos son `true`/`false`.
- Las palabras de comando se asignan a ids de operación mediante `CliInput.HierarchicalRoutes` (por ejemplo `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). La tabla completa de rutas está en la [referencia de la CLI](cli.md). Las operaciones D3D no tienen ruta de palabras de comando; usa `/runtime:d3d.status` o `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Los resultados son diccionarios planos de cadenas. Las listas usan claves `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) con los campos de página `count`, `returned_count`, `truncated` y `next_cursor` (consulta [paginación](capabilities.md#pagination)).
- Los fallos llevan `Succeeded=false`, un `ErrorCode` `OL_E_` y, para los fallos del lado del plugin, `detail` (además de `native_status` para D3D y `exception` para fallos inesperados).

<a id="cli-envelope---json"></a>
### Envelope de la CLI (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

Un envelope de error tiene la forma `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Sin `--json`, la CLI imprime el objeto del resultado como JSON con sangría o como `CODE: message`.

<a id="api-envelope"></a>
### Envelope de la API

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` lanza una excepción ante las infracciones del límite posteriores a la validación del registro (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) y devuelve `Succeeded=false` para los rechazos del registro y los errores del lado del plugin.

<a id="worked-examples"></a>
## Ejemplos prácticos

Todos los ejemplos de la CLI suponen que hay un propietario en ejecución para la instalación que contiene `OmsiLaunch.exe` (iniciado, por ejemplo, con `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, o con `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` cuando un ejemplo necesita un vehículo del jugador) y se ejecutan desde una segunda consola en la misma instalación.

| Familia | Comando | Qué hace |
| --- | --- | --- |
| Sesión | `OmsiLaunch.exe session status --json` | Lee `SessionId`, `State`, los diagnósticos y la lista acotada de eventos de runtime. |
| Hora | `OmsiLaunch.exe time get` y después `OmsiLaunch.exe time set --hour=7 --minute=30` | Lee el reloj; lo ajusta mediante el `SetTime` perfilado y devuelve la relectura. |
| Meteorología | `OmsiLaunch.exe weather get` y `OmsiLaunch.exe weather actual get` | Lee el estado meteorológico actual y el real/ICAO. `OmsiLaunch.exe weather set --wind_speed=5` establece la meteorología manual (el registro de origen de OMSI; el METAR en vivo se detiene) y `OmsiLaunch.exe weather metar set --icao=EDDF` cambia a la meteorología METAR en vivo. Ambos son EXPERIMENTAL hasta que se validen en runtime. |
| Mapa | `OmsiLaunch.exe map get` | Identidad del mapa, nombre, descripción, número de tiles, intervalo de años y sentido de circulación. |
| Cámara | `OmsiLaunch.exe camera set --field_of_view=50` y después `OmsiLaunch.exe camera lock --family=0 --preset=1` y `OmsiLaunch.exe camera unlock` | Escribe el FOV; fija la familia del conductor con el preset 1 (requiere un vehículo del jugador, por ejemplo una situación guardada); libera la política. |
| Vehículos | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Recuentos; handles; un snapshot. |
| Generación | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Crea un RoadVehicle de IA (timeout de 30 s); devuelve `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` invoca `PlaceRandomBus`. |
| Jugador | `OmsiLaunch.exe player get` | `present=false` en un inicio headless; en caso contrario, el snapshot del jugador. |
| Humanos | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Recuentos; handles; un snapshot. |
| Horario | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Recuentos del gestor; filas de trayectos acotadas; registros del horario. |
| Scripts | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Lista/lectura/escritura de variables numéricas; lectura/escritura de variables de cadena; triggers de script y de sonido (las escrituras de cadenas y los triggers son EXPERIMENTAL hasta que se validen en runtime). |
| Constantes y curvas | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Constantes del vehículo y evaluación de curvas. Los nombres dependen del modelo: usa los nombres que devuelve el comando `list` (estos se listaron para el autobús del jugador de `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Metadatos HOF de la definición del vehículo. |
| Conductores y billetes | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Registros de conductores; paquete de billetes. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` y después `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` y `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Ciclo de vida de la textura en el thread de renderizado; `<HANDLE>` es el `handle` que imprime `create`; `--pixels_base64` debe decodificarse en `width * height * 4` bytes (formatos de 32 bits) y 48 KiB como máximo. |
| Eventos | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Lista acotada de eventos; observación por sondeo (250 ms) hasta Ctrl+C. |
| Detención | `OmsiLaunch.exe session stop` | Solicita la detención canónica: el propietario termina OMSI y restaura la transacción. |

<a id="stability"></a>
## Estabilidad

El canal en sí (`runtime.command-channel`) es `STABLE_BETA`: la integridad de la transmisión, la vinculación a la sesión, el descarte de respuestas tardías y el rechazo tipado de tamaños excesivos están cubiertos offline por `runtime-command.wire-guard`, `runtime-command.session-binding` y `runtime-command.late-response-ignored`, y en runtime por RV-003 (sesión `5f641c8d`), la ronda A RA-019 (un cliente abandonó `road-vehicles.spawn` después de 250 ms; la siguiente solicitud tuvo éxito) y la batería de pruebas del canal del cierre de runtime `R03` (timeout, cancelación, id de solicitud reutilizado, rechazo del plugin, operación interna, sesión incorrecta y argumento ausente, cada uno seguido de una solicitud correcta). La estabilidad de cada operación está en [capacidades](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Reutilización del canal tras fallos

Toda ruta terminal de una solicitud de runtime deja el buzón reutilizable: el éxito, un error tipado, una respuesta mal formada, sobredimensionada, de otra sesión o con un id de solicitud incorrecto, el timeout, la cancelación por parte del llamador y los fallos de decodificación terminan todos en una misma limpieza que devuelve el slot al estado inactivo y borra la longitud y la cabecera del envelope, de modo que la siguiente solicitud no puede leer nada de una solicitud anterior. Una solicitud o respuesta residual encontrada al iniciarse una solicitud nueva se borra primero (`OL_E_RUNTIME_REQUEST_ID_REUSED` si lleva el id de la nueva solicitud). En el lado del plugin, una excepción dentro de una operación se responde con `OL_E_RUNTIME_OPERATION_FAILED`, un resultado mayor que el slot con `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (un envelope fijo y pequeño), una solicitud para otra sesión con `OL_E_RUNTIME_SESSION_MISMATCH`, y una solicitud que el host ha abandonado no se responde nunca. Estas reglas están cubiertas offline (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); las rutas que puede producir un llamador (timeout, cancelación, id reutilizado, rechazos tipados, respuesta tardía) también tienen evidencia de runtime (RA-019, `R03`). Las respuestas corruptas, de otra sesión o sobredimensionadas no se pueden producir desde fuera del producto y siguen siendo solo offline.
