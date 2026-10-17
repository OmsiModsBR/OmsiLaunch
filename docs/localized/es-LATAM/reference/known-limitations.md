# Limitaciones conocidas

<!-- l10n: source=reference/known-limitations.md source-sha256=fe1eccc6b9e4b3d93fff33deeecda6b97abd139d7295dba1d6542fb6e57231a7 -->
> Traducción de la [página original en inglés](../../../reference/known-limitations.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página enumera, a partir del código, todo lo que en OmsiLaunch 0.1.0-beta4 es `UNAVAILABLE`, `PARTIAL` o un riesgo aceptado, para que los usuarios y los integradores no construyan sobre comportamientos que el producto no ofrece. Cada fila nombra la limitación, su estabilidad, por qué existe y dónde se documenta en detalle. La documentación en inglés es normativa; las copias localizadas en `docs/localized/` no se mantienen al mismo nivel y pueden quedar desactualizadas (consulte la última sección).

<a id="compatibility"></a>
## Compatibilidad

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| Las capacidades que leen diseños de objetos (vehículos, humanos, clima, mapa, cámara, horario, scripts, conductores, boletos) se verifican solo en un diseño conocido (`Omsi23004`); en otro build de OMSI 2 no están disponibles (`OL_E_CAPABILITY_UNRESOLVED`) hasta que existan validadores estructurales | `PARTIAL` en builds desconocidos | [compatibilidad](compatibility.md) |
| Solo el diseño conocido se ha validado en runtime; la resolución independiente del build se verifica offline contra el archivo ejecutable de referencia con su identidad oculta | `EXPERIMENTAL` en builds desconocidos | [estado de validación en runtime](../status/runtime-validation-status.md) |
| Solo Windows 10+ x64; no se admiten Windows 7/8, XP ni ARM64 | `UNAVAILABLE` | [compatibilidad](compatibility.md) |
| El plugin necesita el runtime de .NET 6 **x86** además del runtime x64 que usa el controlador | | [compatibilidad](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Inicio del mundo y opciones de inicio

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) está implementado a partir del análisis estático de la rama de último estado de OMSI y se validó solo offline | `EXPERIMENTAL` | [CLI](cli.md) |
| La fecha, la hora y el año explícitos o del sistema (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` del perfil) se aplican solo a inicios NEW_MAP y se validan offline; la validación en runtime está pendiente. No se puede solicitar el 31 de diciembre de un año bisiesto (el calendario de OMSI vuelve a empezar en el día 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Preset de clima, ICAO y clima real actual al inicio (`/weather*`, `new.weather`) se aplican justo después de que el mundo inicia mediante la propia carga de clima y los flags de METAR de OMSI (cerrado estáticamente, BI-003); la validación en runtime está pendiente. El clima que muestra OMSI en el primer fotograma puede seguir siendo el predeterminado del mapa | `EXPERIMENTAL` | [CLI](cli.md) |
| El modelo, el repaint, el HOF, el número de flota y la placa del vehículo del jugador al inicio (familia `/vehicle`, `PlayerVehicleSpec`) se resuelven contra el catálogo de contenido, pero no se aplican; solicitarlos hace que el plan no sea ejecutable. La secuencia nativa de asignación (colocación mediante el diálogo de posición de OMSI, `MakeVehicle`, los pasos de finalización por unidad, `CopyTempListIntoMainList`, el índice del jugador) está cerrada estáticamente, pero crea y registra un vehículo vivo y no se implementa antes de poder validarla en runtime; está prevista para una beta posterior. Una situación guardada o `/last` restaura su propio vehículo del jugador | `UNAVAILABLE` (BI-007) | `player.assign-headless` en [capacidades](capabilities.md) |
| El punto de entrada por identidad (`/entrypoint:<identity>`) selecciona la etiqueta exacta que presenta OMSI (cerrado estáticamente, BI-001); un registro posterior que comparta una etiqueta no puede seleccionarse, igual que en el diálogo propio de OMSI. Validación en runtime pendiente | `EXPERIMENTAL` | capacidad del plan `world.entrypoint-identity` |
| Los ajustes de teclado y de controlador por clave (`Environment.Keyboard`, `Environment.Controllers`) no tienen claves catalogadas: en su lugar se reemplazan archivos completos con `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| El cierre cooperativo (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) es opcional (opt-in) y se validó solo offline; por defecto, OMSI se termina cuando una sesión finaliza y `ShutdownTimeoutSeconds` no tiene efecto | `EXPERIMENTAL` | [CLI](cli.md) |
| Los flags de diagnóstico `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` rellenan `DiagnosticsSpec` sin ningún otro efecto (el trace del host bajo `.omsilaunch\diagnostics` se escribe siempre); `/trace-plugin` y `/trace-native` le agregan eventos de trace del plugin y de la capa nativa | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch` y `/d3d-batch` son harnesses de validación | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fin de la sesión y control del proceso

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| La detención de la sesión es una terminación forzada: `session.stop`, "End session" (finalizar sesión) en la bandeja, Ctrl+C y `CloseAsync` conducen todos a `TerminateProcess`. La rutina de apagado de OMSI no se ejecuta, OMSI no reescribe `options.cfg` ni sus logs al salir y se pierde cualquier estado de OMSI no guardado. Esto es deliberado: evita que OMSI escriba sobre los archivos restaurados. | por diseño | [ciclo de vida de la sesión](../concepts/session-lifecycle.md) |
| No está implementado el apagado cooperativo mediante WM_CLOSE con un timeout y terminación como alternativa | `UNAVAILABLE` (decisión de producto, S-11; OMSI ignoró `WM_CLOSE` enviado a su ventana principal en la ronda de cierre de runtime) | [estado de la validación en runtime](../status/runtime-validation-status.md) |
| Al cerrar la consola o cerrar la sesión de Windows, el propietario tiene un presupuesto de 4 s para detener y restaurar; lo que quede pendiente lo recupera el journal en el siguiente inicio | cierre de consola validado en runtime; cierre de sesión de Windows no probado | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transacción, recuperación y lease

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| El lease de la instalación es un semáforo `Local\`: un propietario por instalación **por sesión de inicio de sesión de Windows**; no se aplica entre usuarios; no se libera mientras otro proceso mantenga un handle; cualquier proceso del mismo usuario puede retener el nombre | riesgo aceptado (S-18) | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| La recuperación se rechaza (`OL_E_INSTALLATION_BUSY`) mientras se ejecuta el proceso de OMSI registrado en el journal o, para un journal sin PID, cualquier `Omsi.exe` de esa raíz | por diseño | igual que arriba |
| Una ruta de overlay originalmente ausente cuyo contenido cambió durante la sesión bloquea la restauración (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) hasta que se inspeccione | por diseño | igual que arriba |
| Los journals anteriores a las huellas de propiedad solo pueden cerrarse mediante una sesión con bytes planificados idénticos (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | igual que arriba |
| Solo se restauran las rutas propiedad de la sesión. Las escrituras propias de OMSI durante una sesión (`[last_map]` de `options.cfg` cuando ningún ajuste aplica un overlay sobre `options.cfg`, `Texture\standard.ipr`, cachés, `laststn.osn`, perfil del conductor, logs) persisten, igual que después de un inicio directo de OMSI | por diseño | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| La eliminación de un `closecheck` obsoleto antes de una sesión es permanente (se registra, no se restaura) cuando `SuppressStaleClosecheckWarning` es true | por diseño | igual que arriba |

<a id="runtime-control"></a>
## Control de runtime

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| `weather.set` y `weather.metar.set` escriben el registro de clima de origen de OMSI (cerrado estáticamente); la persistencia durante el ciclo de clima, la resolución del tipo de nube y la descarga de METAR esperan su validación en runtime. Los valores derivados (humedad, factores de luz, viento actual) no pueden establecerse; mientras OMSI está en pausa se actualizan solo después de reanudar | `EXPERIMENTAL` | [capacidades](capabilities.md) |
| `SetActualDateTime` solo copia el reloj de la PC en el reloj de inicio de OMSI (análisis estático); OmsiLaunch no lo llama y establece por sí mismo el reloj de inicio | por diseño | `calendar.set-actual-date-time` |
| Las escrituras de variables de cadena, los triggers con nombre y los triggers de sonido pasan por los propios métodos de OMSI (cerrado estáticamente, BI-004); su efecto sobre los scripts de vehículo espera su validación en runtime. Solo los vehículos de tráfico son destinos | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| No hay reubicación de vehículos, ni revinculación espacial entre tiles, ni autoridad de transformación segura para ODE; los campos de posición son de solo lectura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` necesitan un PlayerVehicle; el inicio headless de NEW_MAP no tiene ninguno (una situación guardada proporciona uno) | por diseño (BI-007) | RV-004 |
| Las mutaciones de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texturas D3D) no se registran en el journal ni se restauran | por diseño | [control de runtime](runtime-control.md) |
| La obsolescencia de un handle se decide a partir del número de instancia propio de OMSI, que nunca se reutiliza (`IDCode`), y, para los humanos, a partir de la desaparición y la nueva generación; se valida offline. La eliminación natural (RV-002: autos de IA que salen de la red de rutas cargada) todavía debe observarse en runtime, y aún no se emite ningún evento `entity.removed` | `PARTIAL` | [control de runtime](runtime-control.md) |
| Los resultados están acotados por el buzón de 64 KiB: las listas largas se truncan (`truncated=true`); las cargas de píxeles se limitan a 48 KiB por `d3d.texture.update` | por diseño | [capacidades](capabilities.md) |
| Canal de vuelo único: una solicitud a la vez por sesión; un slot ocupado produce `OL_E_RUNTIME_CHANNEL_BUSY`; los ids de solicitud no deben reutilizarse | por diseño | [control de runtime](runtime-control.md) |
| La telemetría es un anillo de 64 eventos: un host que se queda más de 64 eventos atrás pierde los más antiguos y se le informa cuántos (`telemetry.lost`) | `EXPERIMENTAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| Los hooks de dispositivo Direct3D de un módulo son hooks de vtable: en una instalación donde otro componente vuelve a escribir los métodos originales del dispositivo (observado en la instalación de regresión poco después de que comienza el gameplay), dejan de llamarse y no pueden retirarse en la limpieza (`OL_E_MODULE_CLEANUP_INCOMPLETE`; el componente permanece cargado) | `PARTIAL` | [módulos de runtime](runtime-modules.md#native-components-and-hooks) |
| La carga de módulos de runtime es abierta: OmsiLaunch no comprueba quién hizo un módulo; garantiza únicamente que los módulos nunca se ejecutan dentro del proceso de OmsiLaunch y que un programa de sesión queda confinado a su job (sin un segundo proceso, termina con la sesión) | por diseño (E21) | [módulos de runtime](runtime-modules.md#security-boundary) |
| El protocolo 2 de módulos (ajustes, activación por defecto, el archivo de ajustes, programas de sesión, conflictos de plugins, avisos, `get_value`) se validó solo offline | `EXPERIMENTAL` | [módulos de runtime](runtime-modules.md) |
| El reinicio del dispositivo D3D se observó en runtime (`resetting`, `restored`, invalidación por generación); no se produjo una transición `lost` distinta porque el dispositivo de OMSI pasó directamente a `DEVICENOTRESET` | `PARTIAL` (RV-007) | [estado de la validación en runtime](../status/runtime-validation-status.md) |
| Las listas de runtime están paginadas (`cursor`, `limit`, `next_cursor`): una página contiene como máximo las filas máximas de la lista y lo que cabe en el slot de runtime de 64 KiB | `EXPERIMENTAL` | [capacidades](capabilities.md#pagination) |
| Algunos campos avanzados del grafo de mapa, tiles, rutas y objetos no se exponen; los lectores de runtime son snapshots tipados condicionados al perfil, nunca un acceso arbitrario a la memoria | por diseño | [capacidades](capabilities.md) |
| Las lecturas de memoria dentro del proceso siguen el patrón comprobar-y-luego-usar contra un OMSI en vivo; una mutación concurrente de OMSI entre la comprobación y la lectura puede producir un snapshot inconsistente (`OL_E_RUNTIME_OPERATION_FAILED`) | riesgo aceptado (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plano de control local y modelo de confianza

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| Modelo de confianza del mismo usuario: el named pipe (`CurrentUserOnly`), las asignaciones de memoria de handoff, telemetría y runtime, y el semáforo del lease son accesibles para cualquier proceso del mismo usuario de Windows. Un proceso así puede leer el estado, detener la sesión o ejecutar operaciones de runtime una vez que haya leído el `session_id`. | riesgo aceptado (S-06, S-30) | [control local](local-control.md) |
| El endpoint de control existe solo mientras el propietario está en `Running`; un cliente ve `OL_E_NO_ACTIVE_SESSION` (salida 4) durante el arranque y después de que la sesión termina | por diseño | [control local](local-control.md) |
| Si otro proceso ya posee el nombre del pipe, el propietario sigue en ejecución sin endpoint (`ListenFault`), y un segundo inicio puede informar erróneamente `OL_E_SESSION_ALREADY_ACTIVE` | riesgo aceptado | [control local](local-control.md) |
| `.omsilaunch\` hereda la ACL de la raíz de OMSI; no se aplica ningún control de acceso explícito | riesgo aceptado (S-31) | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnóstico y salida

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| Los diagnósticos son solo archivos locales (`.omsilaunch\diagnostics`); no se sube nada y no hay informes remotos | por diseño | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| La retención conserva las 50 sesiones más recientes; los diagnósticos más antiguos con prefijo de sesión se eliminan cuando se inicia una nueva sesión | por diseño | igual que arriba |
| La salida JSON y los diagnósticos incluyen rutas de la instalación (`RootPath`, directorios de assets, rutas `.itx`) | por diseño (datos locales) | |
| El diálogo de fallo de la sesión de `OmsiLaunchW.exe` muestra como mensaje la carga de fallo del plugin (por ejemplo `{"name":"world.failed",...}`) en lugar de una oración; la línea `Code:` es correcta | cosmético | [bandeja de Windows](windows-tray.md) |
| Una solicitud D3D rechazada por el puente nativo antes de cualquier llamada a Direct3D informa `native_status` correctamente, pero su texto `detail` dice `HRESULT 0x00000000` | cosmético | [capacidades](capabilities.md) |
| La ventana de estado de la bandeja es un snapshot de la sesión planificada tomado al abrirse; no se actualiza y no muestra valores de OMSI en vivo | por diseño | [bandeja de Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentación

Las páginas en inglés en `docs/` son la documentación normativa de esta release. `docs/localized/<locale>/` contiene traducciones de las mismas páginas de `0.1.0-beta4` (consulte [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); cuando una traducción difiere del texto en inglés, el texto en inglés y el código son la referencia autorizada. Las páginas históricas y heredadas que se enumeran allí solo están disponibles en inglés.

Relacionado: [capacidades](capabilities.md), [estado de la validación en runtime](../status/runtime-validation-status.md), [errores](errors.md).
