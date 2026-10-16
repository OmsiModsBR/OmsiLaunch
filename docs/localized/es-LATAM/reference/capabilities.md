# Capacidades

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Traducción de la [página original en inglés](../../../reference/capabilities.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página documenta lo que puede hacer OmsiLaunch. Su fuente es el catálogo canónico de capacidades, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): allí se declara una sola vez cada capacidad, cada operación pública de runtime con sus argumentos, sus rutas de la CLI y su capacidad nativa, y un estado de evidencia por capacidad. El conjunto de operaciones de runtime, la validación de argumentos, las rutas de comandos de la CLI, el gate nativo por operación del plugin, `GetCapabilitiesAsync` y la [tabla del catálogo](#capability-catalog) de más abajo se derivan de él. Para cada operación de runtime, la página indica los argumentos, las claves del resultado y los errores. Cómo invocar una operación (rutas de la CLI, envoltorio de la API, timeouts, handles) se describe en [control de runtime](runtime-control.md); la evidencia por sesión está en el [estado de validación en runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Clasificación y estabilidad

`PublicCapabilityClassification` tiene cuatro valores. Se corresponden con el vocabulario de estabilidad de la siguiente manera, con degradaciones donde la evidencia es incompleta:

| Clasificación | Significado | Estabilidad |
| --- | --- | --- |
| `PublicStableBeta` | Pública, dentro del contrato de la beta, validada en runtime | `STABLE_BETA` (degradada a `PARTIAL` donde se indica) |
| `PublicExperimental` | Pública, puede cambiar, validada en runtime o validada estáticamente | `EXPERIMENTAL` (degradada a `PARTIAL` donde se indica) |
| `InternalOnly` | Primitiva de investigación; no accesible a través de la API pública ni de la CLI | `INTERNAL` |
| `Unsupported` | Reconocida con fines de diagnóstico, rechazada o ausente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` y `Event`. Toda capacidad de runtime requiere una sesión en estado `Running` (`RequiresSession` en el registro) y que su capacidad nativa esté resuelta y verificada en el build en ejecución; una operación cuya capacidad nativa no está disponible se rechaza con `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` marca las capacidades que dependen de destinos nativos (consulte [compatibilidad](compatibility.md)).

Los resultados de runtime son diccionarios de cadenas. Las claves que comienzan con `internal_` o terminan en `_address`, `_pointer` o `_vmt` se eliminan en el límite de la API (`ScrubInternalValues`) y no se listan aquí. Las claves de resultado de las tablas de operaciones que siguen son los conjuntos exactos de claves que devuelve el producto: se capturaron ejecutando cada operación pública en una sesión real (captura de documentación del cierre de runtime, sesión `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; las filas de las listas se escriben `<row>.<n>.<field>`).

Cómo se informa el fallo de una operación (`CurrentRuntimeControl.Execute`):

| Fallo | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rechazo del registro (operación desconocida o `internal.*`, falta un argumento obligatorio) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | ninguno |
| Un rechazo deliberado del producto (una clave de clima derivada en `weather.set`) | el código específico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (oración) |
| Un fallo de D3D | el código `OL_E_D3D_*` específico | `detail`, `native_status` |
| Un fallo tipificado del lado del plugin (handle obsoleto, nombre desconocido, valor fuera de rango, sin vehículo del jugador, ...) | el código específico, por ejemplo `OL_E_RUNTIME_OBJECT_HANDLE_STALE` u `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (el mensaje), `exception` (nombre del tipo .NET) |
| Una operación cuya capacidad nativa no está resuelta en el build en ejecución | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (la capacidad nativa) |
| Una excepción inesperada del lado del plugin sin código | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Un resultado que no cabe en el buzón de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (las listas paginadas, en cambio, se acortan y continúan con `next_cursor`; consulte [paginación](#pagination)) | ninguno |

La columna `Errors` de cada tabla indica los códigos específicos; llegan en `ErrorCode` a través del plugin, el puente, el plano de control, la API y la CLI sin cambios.

<a id="capability-catalog"></a>
## Catálogo de capacidades

Una fila por cada entrada del catálogo, generada a partir de `PublicCapabilityRegistry.All` (el gate de documentación falla cuando esta tabla está desactualizada). `Session` (Sesión) es `Running` para las capacidades que necesitan una sesión en ejecución. `Operations` (Operaciones) son los ids concretos de las operaciones de runtime (las filas de contrato están en [operaciones públicas de runtime](#public-runtime-operations)). `Native capability` (Capacidad nativa) nombra las entradas del catálogo de destinos nativos (consulte [compatibilidad](compatibility.md)) que deben estar resueltas y verificadas en el build en ejecución. `Evidence` (Evidencia) usa un único vocabulario:

| Evidencia | Significado |
| --- | --- |
| RUNTIME_VALIDATED | Observada en una sesión real de OMSI; los ids de sesión están en el [estado de validación en runtime](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implementada y verificada offline; no se ha observado el efecto en runtime. |
| PARTIAL | Funciona para parte de su contrato; la nota indica la parte que sigue abierta. |
| RUNTIME_VALIDATION_REQUIRED | Implementada, a la espera de su validación en runtime. |
| UNAVAILABLE | Reconocida con fines de diagnóstico y rechazada, o no implementada; solicitarla al iniciar hace que el plan no sea ejecutable. |

Una operación que cambia algo indica su política de mutación después de su id (`PublicRuntimeOperation.MutationPolicy`); ninguna operación de runtime se registra en el journal:

| Política de mutación | Significado |
| --- | --- |
| `none` | Solo lecturas (no se muestra). |
| `omsi-state` | Cambia el estado en memoria de OMSI; nada lo deshace; termina con el proceso de OMSI. |
| `session-resource` | Crea o cambia un recurso que pertenece a la sesión, que se libera explícitamente o cuando la sesión termina. |
| `session-policy` | Instala una política que el plugin vuelve a aplicar hasta que se elimina o la sesión termina. |

| Capacidad | Clasificación | Tipo | Sesión | Operaciones | Capacidad nativa | Evidencia | CLI | Nota |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: los eventos del plugin viajan por un anillo numerado; un lector que se queda atrás recibe telemetry.lost con la cantidad. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 ronda A: diseño conocido. |
| `boot.headless-start` | `PublicStableBeta` | Action | no |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | no |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI presenta el primer registro de cada etiqueta exacta; el arranque lo selecciona por esa etiqueta. Cerrada estáticamente; validación en runtime pendiente. |
| `world.saved-situation` | `PublicStableBeta` | Action | no |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | no |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: la rama nativa está cerrada estáticamente (BI-006); validación en runtime pendiente. Nunca una heurística del .osn más reciente. |
| `world.explicit-date` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: se escribe en el reloj de inicio de OMSI antes de NewSituation (análisis estático); validación en runtime pendiente. |
| `world.explicit-time` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: se escribe en el reloj de inicio de OMSI antes de NewSituation (análisis estático); validación en runtime pendiente. |
| `world.explicit-year` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: se escribe en el reloj de inicio de OMSI antes de NewSituation (análisis estático); validación en runtime pendiente. |
| `world.weather` | `PublicExperimental` | Action | no |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): se aplica justo después de que el mundo inicia, mediante el loadWeather propio de OMSI o sus flags de METAR; sobrescribe el clima de una situación guardada. Cerrada estáticamente; validación en runtime pendiente. |
| `player-vehicle.model` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; se resuelve con fines de diagnóstico, no se aplica. |
| `player-vehicle.repaint` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false en un inicio headless; el snapshot completo con una situación guardada. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: la secuencia nativa de asignación está cerrada estáticamente (research/reports/beta4-static/entrypoint-last-player-vehicle.md sección 3) pero no se implementa antes de su validación en runtime; prevista para una beta posterior. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transaccional, validado offline. |
| `input.controller` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transaccional, validado offline. |
| `presentation.splash` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | no |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 ronda B: la supresión se aplica y se informa (internet-textures.suppressed) a través del anillo de telemetría. |
| `internet-textures.override` | `PublicExperimental` | Write | no |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Cierre de runtime I01. |
| `session-profiles` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | Inicio con perfil RA-009; 1..99 y el descubrimiento, offline. |
| `hof.management` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 ronda A C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paginada) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 cerrada estáticamente: SetActualDateTime solo copia el reloj de la PC en el reloj de inicio; las fechas de inicio usan world.explicit-date. Reemplazada: la ABI nativa y las poscondiciones no están cerradas. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: escribe actWeather (Weather+0x08), el registro del que OMSI deriva su ciclo de clima; cerrada estáticamente. Los valores derivados (humedad, factores de luz, viento actual) siguen siendo de solo lectura (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Validación en runtime pendiente. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paginada) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Necesita un PlayerVehicle (una situación guardada aporta uno); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE en un NEW_MAP headless. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paginada) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | La detección de obsolescencia tras una eliminación natural (RV-002) no tiene un productor seguro en runtime y se valida offline. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paginada) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paginada), `timetable.trips.list` (paginada), `timetable.lines.list` (paginada), `timetable.rv-files.list` (paginada), `timetable.track-entries.list` (paginada), `timetable.bus-stops.list` (paginada), `timetable.station-links.list` (paginada), `timetable.tours.list` (paginada), `timetable.profiles.list` (paginada), `timetable.tour-entries.list` (paginada), `timetable.logs.read` (paginada), `timetable.no-rv-numbers.list` (paginada) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paginada), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paginada), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Escrituras: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI copia una cadena con forma de constante; cerrada estáticamente. Validación en runtime pendiente. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): TriggerXML/SoundTrigger virtuales a través de los slots de VMT perfilados; cerrada estáticamente. Validación en runtime pendiente. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paginada), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paginada), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paginada) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paginada) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Reinicio del dispositivo observado (resetting, restored, invalidación de generación); no se produjo una transición lost distinta (RV-007). |

`internal.make-basic` es `InternalOnly`: existe en el plugin con fines de investigación (devuelve una dirección nativa) y se rechaza en el límite de la API y en el plano de control local, que validan primero el nombre de la operación contra `PublicRuntimeOperationIds`; la CLI no tiene ruta para ella. `session.stop` termina OMSI; el cierre cooperativo no está implementado.

<a id="pagination"></a>
## Paginación

Toda operación de lista (marcada "(paginada)" en el [catálogo de capacidades](#capability-catalog)) acepta dos argumentos opcionales y responde con cuatro campos de página:

| Argumento | Significado |
| --- | --- |
| `cursor` | Dónde empieza la página: el `next_cursor` de la página anterior. Si se omite: la primera página. Un cursor de otra lista, o mal formado, es `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Filas deseadas (un entero positivo). Si es mayor que la página máxima de la lista: se usa el máximo. Si no es un entero positivo: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Clave del resultado | Significado |
| --- | --- |
| `count` | Todos los registros que tiene OMSI. |
| `returned_count` | Filas de esta respuesta. |
| `truncated` | `true` cuando hay registros a continuación de esta página. |
| `next_cursor` | Presente solo cuando hay registros a continuación: páselo como `cursor` para la página siguiente. |

El cursor es una clave estable, no una posición. Para los arreglos de registros (horario, nombres de scripts, constantes, curvas, HOF, conductores, boletos, logs), las filas se identifican como `<row>.<n>.<field>` (o `name.<n>`) por el índice absoluto del registro y el cursor nombra el índice siguiente, de modo que la primera página es igual a la respuesta de Beta 3. Para las colecciones de handles (`road-vehicles.list`, `humans.list`), las filas se devuelven en orden de handle y el cursor nombra el último handle devuelto: un objeto que aparece entre dos páginas recibe un handle posterior y llega en una página posterior; ninguno se repite. Trate los valores de cursor como opacos.

Tamaños máximos de página: 1000 para `road-vehicles.list` y `humans.list`; 512 para `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` y `vehicle.curves.list`; 256 para `timetable.tour-entries.list`, `timetable.logs.read` y `vehicle.hofs.read`; 128 para las demás listas de horario, `drivers.read` y `tickets.read`. Cuando las filas de una página siguen superando el buzón de 64 KiB, el plugin descarta las últimas filas hasta que la respuesta cabe y `next_cursor` continúa en la primera fila descartada, de modo que ninguna lista falla por su tamaño.

<a id="public-runtime-operations"></a>
## Operaciones públicas de runtime

Esta es la lista exacta de ids de operación que un frontend público puede reenviar (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Todas requieren `SessionState.Running`; ninguna se registra en el journal ni se restaura. Los argumentos obligatorios los hace cumplir el registro antes de usar el buzón (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); los argumentos opcionales los valida el plugin. Códigos de fallo comunes a todas las operaciones: `OL_E_RUNTIME_OPERATION_UNKNOWN` (no está en esta lista), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (el plugin no tiene implementación), `OL_E_RUNTIME_OPERATION_FAILED` (excepción inesperada dentro del proceso; valores `detail` y `exception`).

<a id="time"></a>
### Hora

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `time.read` | Read | ninguno | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcionales `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); al menos uno | las claves de `time.read` después del `SetTime` perfilado | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Clima

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | ninguno | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` y los valores de origen `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, los factores de luz y las humedades son los valores por ciclo de OMSI; `cloud_type` es el número resuelto del tipo de nube | |
| `weather.set` | Write | al menos uno de `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Rangos: niebla 0..100000, luminosidad y valores del suelo 0..1, velocidad del viento 0..200, dirección del viento 0..360, temperatura y punto de rocío -60..60, presión 850..1100, precipitación 0..2, tasa 0..255, altura de las nubes 0..20000, `cloud_type` de 1..64 caracteres de la página de códigos 1252 (`-1` = sin nubes), booleanos `true`/`false`/`1`/`0` | las claves de `weather.read`; con `cloud_type` también `cloud_type_resolved` (`false` cuando OMSI no conoce el tipo de nube: no dibuja ninguna) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (una clave derivada: `relative_humidity`, `absolute_humidity`, los factores de luz, `cloud_transparency`, `precipitation_set`, `wet_ground`; el detalle nombra la clave que debe establecerse), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (clave desconocida), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 letras o dígitos) | las claves de `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | ninguno | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `map.read` | Read | ninguno | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Cámara

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | ninguno | `family` (0 conductor, 1 pasajero, 2 externa, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcionales `family` (0..3), `field_of_view` (10..170); al menos uno | las claves de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obligatorio `family` (0..3); opcional `preset` (0..255, solo con la familia 0 o 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. La política se vuelve a aplicar cada 100 ms hasta `camera.unlock`; una reaplicación fallida emite el evento `camera.lock.degraded` una vez por cada error distinto. |
| `camera.unlock` | Action | ninguno | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Vehículos de tráfico y jugador

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | ninguno | `count`, `ai_collection`, `player_index` (el índice bruto del vehículo del jugador de OMSI, informado tal como se lee; use `present` de `player-vehicle.read` para saber si existe un vehículo del jugador) | |
| `road-vehicles.list` | Read | opcionales `cursor`, `limit` | campos de página; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` es la posición en la lista de OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | obligatorio `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | ninguno | `present` (`false` cuando OMSI no tiene vehículo del jugador); cuando es `true`: `player_index` más todas las claves de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obligatorio `model` (`Vehicles\...\*.bus` canónico, como máximo 240 caracteres, sin `..`, debe existir dentro de la instalación) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. No asigna el PlayerVehicle. Tarda varios segundos; use el timeout de cliente de 30 s. |
| `road-vehicles.place-random` | Action | opcionales `ai_type` (0..255, predeterminado 0), `group` (0..65535, predeterminado 1), `type` (-1..65535, predeterminado -1), `scheduled` (0..1, predeterminado 0), `tour` (0..65535, predeterminado 0), `line` (0..65535, predeterminado 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humanos

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | ninguno | `count` | |
| `humans.list` | Read | opcionales `cursor`, `limit` | campos de página; `human.<n>.handle` (`hb-NNNNNN`; `<n>` es la posición en la lista de OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | obligatorio `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Horario, conductores, boletos

Las listas de horario, `drivers.read` y `tickets.read` están paginadas (consulte [paginación](#pagination)); "campos de página" a continuación significa `count`, `returned_count`, `truncated` y `next_cursor`, y toda lista acepta los opcionales `cursor` y `limit`.

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | ninguno | `invalid` (`true`/`false`) y los recuentos de registros `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | opcionales `cursor`, `limit` | campos de página; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | opcionales `cursor`, `limit` | campos de página; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | opcionales `cursor`, `limit` | campos de página; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | opcionales `cursor`, `limit` | campos de página; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | opcionales `cursor`, `limit` | campos de página; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | opcionales `cursor`, `limit` | campos de página; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | opcionales `cursor`, `limit` | campos de página; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | opcionales `cursor`, `limit` | campos de página; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | opcionales `cursor`, `limit` | campos de página; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | opcionales `cursor`, `limit` | campos de página; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | opcionales `cursor`, `limit` | campos de página; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | opcionales `cursor`, `limit` | campos de página; `no_rv.<n>.group`, `no_rv.<n>.number`: los números de vehículo que OMSI mantiene fuera del uso libre de la IA (la lista de cadenas de números reservados de OMSI, leída con su propio diseño; `OL_E_RUNTIME_VALUE_INVALID` cuando la lista no tiene la forma esperada). Como máximo 256 por página. Análisis estático; validación en runtime pendiente. |
| `map.tiles.list` | Read | opcionales `cursor`, `limit` | campos de página; `tile.<n>.x`, `tile.<n>.y` (posición en la cuadrícula), `tile.<n>.tile_index` (el índice propio del tile en la lista de tiles del mapa), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` con `tile.<n>.error` para un tile que no puede leerse. Como máximo 256 por página. Posición e índice a partir de análisis estático; validación en runtime pendiente. |
| `drivers.read` | Read | opcionales `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | opcionales `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts del vehículo, constantes, curvas, HOF (con ámbito de handle)

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | obligatorio `handle`; opcionales `cursor`, `limit` | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obligatorios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obligatorios `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (releído) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | obligatorio `handle`; opcionales `cursor`, `limit` | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obligatorios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | obligatorios `handle`, `name` (distingue mayúsculas de minúsculas, tal como lo declara el script, hasta 128 caracteres), `value` (1..1024 caracteres) | `handle`, `name`, `value` releído después de la escritura | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (el propio OMSI ignoraría un nombre desconocido en silencio), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | obligatorios `handle`, `name` (página de códigos 1252, hasta 128 caracteres), `value` (`1` ejecuta `name`, `0` ejecuta `name_off`, `2` ejecuta `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. La entrada del script se ejecuta de forma síncrona; un nombre que el script no define no hace nada. Normalmente a una pulsación le sigue una liberación |
| `vehicle.sound-trigger` | Action | obligatorios `handle`, `trigger` (página de códigos 1252, hasta 128 caracteres); opcional `file` (hasta 260 caracteres) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (también: el vehículo no tiene paquete de sonido), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | obligatorio `handle`; opcionales `cursor`, `limit` | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obligatorios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | obligatorio `handle`; opcionales `cursor`, `limit` | `handle`, campos de página, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obligatorios `handle`, `name`, `x` (float finito; un `x` ausente o no numérico da `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolación lineal) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | obligatorio `handle`; opcionales `cursor`, `limit` | `handle`, campos de página, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Las operaciones D3D se ejecutan en el thread principal/de renderizado de OMSI a través del puente nativo. Los handles de textura tienen la forma `d3dtex-<sessionId N>-<16 hex digits>`.

| Operación | Tipo | Argumentos | Claves del resultado | Errores |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | ninguno | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obligatorios `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcional `levels` (0..16, predeterminado 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obligatorio `handle`; opcional `level` (0..15, predeterminado 0) | las 13 claves de `d3d.texture.create`, para el nivel solicitado | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, más los errores de create |
| `d3d.texture.update` | Action | obligatorios `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (como máximo 48 KiB decodificados); opcionales `level` (0..15, predeterminado 0), `x` (0..4095, predeterminado 0), `y` (0..4095, predeterminado 0) | las 13 claves de `d3d.texture.create` después de la actualización | `OL_E_D3D_INVALID_PIXEL_BUFFER`, más los errores de describe |
| `d3d.texture.release` | Action | obligatorio `handle` | las 13 claves de `d3d.texture.create` con `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` en una liberación repetida, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Las operaciones D3D fallidas devuelven los valores `detail` y `native_status`. Los métodos de extensión de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulan estas operaciones para los consumidores de la API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` devuelve una `Capability(Name, Available, EvidenceState, Reason)` por cada entrada del catálogo que no sea `InternalOnly`. `Name` es el id del catálogo y `EvidenceState` su evidencia del catálogo. `Available` se evalúa para la instalación:

- Las entradas `Unsupported` nunca están disponibles; `Reason` lleva la nota del catálogo.
- Toda entrada necesita la plataforma actual, y toda entrada salvo `runtime.current-windows-x64` necesita una instalación de OMSI 2.
- Una entrada con capacidades nativas en el diseño conocido informa `native: RESOLVED_KNOWN_LAYOUT`. En otro build de OMSI 2, el archivo ejecutable se resuelve estáticamente: `native: STATICALLY_RESOLVED`, o `native: PENDING_IN_PROCESS_RESOLUTION` con lo que el archivo no resolvió. Una entrada pendiente se informa igualmente como disponible, porque decide la resolución en el proceso; si esta falla, la operación o el inicio fallan con `OL_E_CAPABILITY_UNRESOLVED`.

El planificador informa capacidades por plan en `SessionPlan.RequiredCapabilities` y `SessionPlan.UnsupportedRequestedFeatures` con los mismos ids del catálogo (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Una función solicitada no compatible lleva su evidencia del catálogo; una capacidad nativa requerida lleva `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Limitaciones conocidas

- Los handles tienen ámbito de sesión y se informan como `OL_E_RUNTIME_OBJECT_HANDLE_STALE` cuando su objeto desaparece. Un handle de vehículo está ligado a su VMT, su definición y el `IDCode` de OMSI (un número único por proceso que recibe cada objeto del mapa al construirse y que nunca se reutiliza), de modo que un objeto recreado en la misma dirección siempre se distingue. Los humanos son slots reutilizables que viven durante toda la sesión del mapa: un handle de humano deja de resolverse cuando el humano desaparece (`RenderMe` 0) o el slot se vuelve a generar (nueva estación de destino o salida).
- Los resultados están limitados al buzón de 64 KiB; toda lista está paginada (consulte [paginación](#pagination)).
- No hay reubicación de vehículos entre tiles, ni escrituras de calendario, ni asignación headless del PlayerVehicle. Las escrituras de variables de cadena, los triggers de script y las escrituras de clima son `EXPERIMENTAL` (validación en runtime pendiente). Consulte [limitaciones conocidas](known-limitations.md).
