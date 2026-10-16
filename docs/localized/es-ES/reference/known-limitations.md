# Limitaciones conocidas

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> Traducción de la [página original en inglés](../../../reference/known-limitations.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página enumera, a partir del código, todo lo que en OmsiLaunch 0.1.0-beta4 es `UNAVAILABLE`, `PARTIAL` o un riesgo aceptado, para que usuarios e integradores no construyan sobre un comportamiento que el producto no ofrece. Cada fila nombra la limitación, su estabilidad, por qué existe y dónde se documenta en detalle. La documentación en inglés es normativa; las copias traducidas bajo `docs/localized/` no se mantienen al mismo nivel y pueden quedar desfasadas (consulta la última sección).

<a id="compatibility"></a>
## Compatibilidad

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| Las capacidades que leen disposiciones de objetos (vehículos, humanos, meteorología, mapa, cámara, horario, scripts, conductores, billetes) solo se verifican en una disposición conocida (`Omsi23004`); en otro build de OMSI 2 no están disponibles (`OL_E_CAPABILITY_UNRESOLVED`) hasta que existan validadores estructurales | `PARTIAL` en builds desconocidos | [compatibilidad](compatibility.md) |
| El ejecutable genuino de Steam (protegido con DRM) no se ha validado en runtime; sí se validó una copia LAA controlada | `PARTIAL` para Steam | [compatibilidad](compatibility.md) |
| Solo la disposición conocida se ha validado en runtime; la resolución independiente del build se verifica offline con el ejecutable de referencia con su identidad retenida | `EXPERIMENTAL` en builds desconocidos | [estado de validación en runtime](../status/runtime-validation-status.md) |
| Solo Windows 10+ x64; no hay Windows 7/8, ni XP, ni ARM64 | `UNAVAILABLE` | [compatibilidad](compatibility.md) |
| El plugin necesita el runtime de .NET 6 **x86** además del runtime x64 que usa el controlador | | [compatibilidad](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Inicio del mundo y opciones de lanzamiento

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) está implementado a partir del análisis estático de la rama de último estado de OMSI y solo se ha validado offline | `EXPERIMENTAL` | [CLI](cli.md) |
| La fecha, la hora y el año explícitos o del sistema (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` del perfil) se aplican solo a los inicios de NEW_MAP y se han validado offline; la validación en runtime está pendiente. El 31 de diciembre de un año bisiesto no se puede solicitar (el calendario de OMSI desborda el día 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| El preset meteorológico, el ICAO y el tiempo real actual en el inicio (`/weather*`, `new.weather`) se aplican justo después de que arranque el mundo mediante la propia carga de meteorología y los flags METAR de OMSI (cerrado estáticamente, BI-003); la validación en runtime está pendiente. La meteorología que OMSI muestra en el primerísimo fotograma puede seguir siendo la predeterminada del mapa | `EXPERIMENTAL` | [CLI](cli.md) |
| El modelo, el repaint, el HOF, el número de flota y la matrícula del vehículo del jugador en el inicio (familia `/vehicle`, `PlayerVehicleSpec`) se resuelven contra el catálogo de contenido, pero no se aplican; solicitarlos hace que el plan no sea ejecutable. La secuencia de asignación nativa (colocación mediante el diálogo de posición de OMSI, `MakeVehicle`, los pasos de finalización por unidad, `CopyTempListIntoMainList`, el índice del jugador) está cerrada estáticamente, pero crea y registra un vehículo activo y no se implementa antes de poder validarse en runtime; está prevista para una beta posterior. Una situación guardada o `/last` restaura su propio vehículo del jugador | `UNAVAILABLE` (BI-007) | `player.assign-headless` en [capacidades](capabilities.md) |
| El punto de entrada por identidad (`/entrypoint:<identity>`) selecciona la etiqueta exacta que presenta OMSI (cerrado estáticamente, BI-001); un registro posterior que comparte etiqueta no se puede seleccionar, igual que en el propio diálogo de OMSI. Validación en runtime pendiente | `EXPERIMENTAL` | capacidad del plan `world.entrypoint-identity` |
| Los ajustes de teclado y de controladores por clave (`Environment.Keyboard`, `Environment.Controllers`) no tienen claves catalogadas: en su lugar se sustituyen archivos completos con `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| El cierre cooperativo (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) es opcional y solo se ha validado offline; de forma predeterminada se termina OMSI cuando acaba una sesión y `ShutdownTimeoutSeconds` no tiene efecto | `EXPERIMENTAL` | [CLI](cli.md) |
| Los flags de diagnóstico `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` rellenan `DiagnosticsSpec` sin ningún otro efecto (la traza del host bajo `.omsilaunch\diagnostics` se escribe siempre); `/trace-plugin` y `/trace-native` le añaden eventos de traza del plugin y nativos | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch` y `/d3d-batch` son arneses de validación | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fin de la sesión y control de procesos

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| La detención de la sesión es una terminación forzada: `session.stop`, «End session» (finalizar la sesión) en la bandeja, Ctrl+C y `CloseAsync` conducen todos a `TerminateProcess`. La rutina de cierre de OMSI no se ejecuta, OMSI no reescribe `options.cfg` ni sus logs al salir, y se pierde cualquier estado de OMSI no guardado. Es deliberado: evita que OMSI escriba sobre los archivos restaurados. | por diseño | [ciclo de vida de la sesión](../concepts/session-lifecycle.md) |
| El cierre cooperativo mediante WM_CLOSE con un timeout y terminación como alternativa no está implementado | `UNAVAILABLE` (decisión de producto, S-11; OMSI ignoró `WM_CLOSE` enviado a su ventana principal en la ronda de cierre de runtime) | [estado de la validación en runtime](../status/runtime-validation-status.md) |
| Al cerrar la consola o en el cierre de sesión de Windows, el propietario dispone de un presupuesto de 4 s para detener y restaurar; lo que quede pendiente lo recupera el diario en el siguiente inicio | cierre de consola validado en runtime; cierre de sesión de Windows no probado | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transacción, recuperación y lease

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| El lease de la instalación es un semáforo `Local\`: un propietario por instalación **por sesión de inicio de sesión**; no se aplica entre usuarios; no se libera mientras otro proceso tenga un handle; cualquier proceso del mismo usuario puede retener el nombre | riesgo aceptado (S-18) | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| La recuperación se rechaza (`OL_E_INSTALLATION_BUSY`) mientras se ejecuta el proceso de OMSI registrado en el diario o, para un diario sin PID, cualquier `Omsi.exe` de esa raíz | por diseño | como arriba |
| Una ruta de overlay originalmente ausente cuyo contenido cambió durante la sesión bloquea la restauración (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) hasta que se inspeccione | por diseño | como arriba |
| Los diarios anteriores a las huellas de propiedad solo puede cerrarlos una sesión con bytes planificados idénticos (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | como arriba |
| Solo se restauran las rutas propiedad de la sesión. Las escrituras propias de OMSI durante una sesión (`[last_map]` de `options.cfg` cuando ningún ajuste aplica un overlay a `options.cfg`, `Texture\standard.ipr`, cachés, `laststn.osn`, perfil de conductor, logs) persisten, igual que tras un inicio directo de OMSI | por diseño | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| La eliminación de un `closecheck` obsoleto antes de una sesión es permanente (se registra, no se restaura) cuando `SuppressStaleClosecheckWarning` es true | por diseño | como arriba |

<a id="runtime-control"></a>
## Control de runtime

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| `weather.set` y `weather.metar.set` escriben el registro de meteorología de origen de OMSI (cerrado estáticamente); la persistencia a lo largo del tick meteorológico, la resolución del tipo de nube y la descarga del METAR esperan la validación en runtime. Los valores derivados (humedad, factores de luz, viento real) no se pueden establecer; mientras OMSI está en pausa se actualizan solo al reanudar | `EXPERIMENTAL` | [capacidades](capabilities.md) |
| `SetActualDateTime` solo copia el reloj del PC al reloj de inicio de OMSI (análisis estático); OmsiLaunch no lo llama y establece él mismo el reloj de inicio | por diseño | `calendar.set-actual-date-time` |
| Las escrituras de variables de cadena, los triggers con nombre y los triggers de sonido pasan por los propios métodos de OMSI (cerrado estáticamente, BI-004); su efecto en los scripts de los vehículos espera la validación en runtime. Solo los vehículos de carretera son destinos | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| No hay reubicación de vehículos, ni revinculación espacial entre tiles, ni autoridad sobre transformaciones segura para ODE; los campos de posición son de solo lectura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` necesitan un PlayerVehicle; el inicio headless de NEW_MAP no tiene ninguno (una situación guardada proporciona uno) | por diseño (BI-007) | RV-004 |
| Las mutaciones de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texturas D3D) no se registran en el diario ni se restauran | por diseño | [control de runtime](runtime-control.md) |
| La obsolescencia de un handle se decide a partir del propio número de instancia nunca reutilizado de OMSI (`IDCode`) y, en el caso de los humanos, a partir de la desaparición y la reaparición; está validada offline. La retirada natural (RV-002: los coches de la IA que salen de la red de rutas cargada) sigue pendiente de observarse en runtime, y todavía no se emite ningún evento `entity.removed` | `PARTIAL` | [control en runtime](runtime-control.md) |
| Los resultados están acotados por el buzón de 64 KiB: las listas largas se truncan (`truncated=true`); las cargas de píxeles se limitan a 48 KiB por `d3d.texture.update` | por diseño | [capacidades](capabilities.md) |
| Canal de una sola solicitud en vuelo: una solicitud cada vez por sesión; un slot ocupado da `OL_E_RUNTIME_CHANNEL_BUSY`; los ids de solicitud no deben reutilizarse | por diseño | [control de runtime](runtime-control.md) |
| La telemetría es un anillo de 64 eventos: un host que se queda más de 64 eventos atrás pierde los más antiguos y se le informa de cuántos (`telemetry.lost`) | `EXPERIMENTAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| Los hooks del dispositivo Direct3D de un módulo son hooks de vtable: en una instalación en la que otro componente vuelve a escribir los métodos originales del dispositivo (observado en la instalación de regresión poco después de que empiece la fase de juego), dejan de invocarse y no se pueden retirar en la limpieza (`OL_E_MODULE_CLEANUP_INCOMPLETE`; el componente permanece cargado) | `PARTIAL` | [módulos de runtime](../../../reference/runtime-modules.md#native-components-and-hooks) |
| La carga de módulos de runtime es abierta: OmsiLaunch no comprueba quién hizo un módulo; garantiza solo que los módulos nunca se ejecutan dentro del proceso de OmsiLaunch y que un programa de sesión queda confinado a su tarea (sin segundo proceso, terminado con la sesión) | por diseño (E21) | [módulos de runtime](../../../reference/runtime-modules.md#security-boundary) |
| El protocolo 2 de módulos (ajustes, activación predeterminada, el archivo de configuración, programas de sesión, conflictos de plugins, avisos, `get_value`) solo se ha validado offline | `EXPERIMENTAL` | [módulos de runtime](../../../reference/runtime-modules.md) |
| El reset del dispositivo D3D se observó en runtime (`resetting`, `restored`, invalidación de generación); no se produjo una transición `lost` diferenciada porque el dispositivo de OMSI pasó directamente a `DEVICENOTRESET` | `PARTIAL` (RV-007) | [estado de la validación en runtime](../status/runtime-validation-status.md) |
| Las listas de runtime están paginadas (`cursor`, `limit`, `next_cursor`): una página contiene como máximo las filas máximas de la lista y lo que cabe en el slot de runtime de 64 KiB | `EXPERIMENTAL` | [capacidades](capabilities.md#pagination) |
| Algunos campos avanzados del grafo de mapas, tiles, rutas y objetos no se exponen; los lectores de runtime son snapshots tipados condicionados al perfil, nunca acceso arbitrario a memoria | por diseño | [capacidades](capabilities.md) |
| Las lecturas de memoria dentro del proceso siguen el patrón comprobar y luego usar contra un OMSI en ejecución; una mutación concurrente de OMSI entre la comprobación y la lectura puede producir un snapshot incoherente (`OL_E_RUNTIME_OPERATION_FAILED`) | riesgo aceptado (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Plano de control local y modelo de confianza

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| Modelo de confianza del mismo usuario: la canalización con nombre (`CurrentUserOnly`), las asignaciones de memoria de handoff, telemetría y runtime y el semáforo del lease son accesibles para cualquier proceso del mismo usuario de Windows. Un proceso así puede leer el estado, detener la sesión o ejecutar operaciones de runtime una vez que ha leído el `session_id`. | riesgo aceptado (S-06, S-30) | [control local](local-control.md) |
| El endpoint de control solo existe mientras el propietario está en `Running`; un cliente ve `OL_E_NO_ACTIVE_SESSION` (salida 4) durante el arranque y después de que termine la sesión | por diseño | [control local](local-control.md) |
| Si otro proceso ya posee el nombre de la canalización, el propietario sigue ejecutándose sin endpoint (`ListenFault`), y un segundo lanzamiento puede notificar erróneamente `OL_E_SESSION_ALREADY_ACTIVE` | riesgo aceptado | [control local](local-control.md) |
| `.omsilaunch\` hereda la ACL de la raíz de OMSI; no se aplica ningún control de acceso explícito | riesgo aceptado (S-31) | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnóstico y salida

| Limitación | Estabilidad | Detalle |
| --- | --- | --- |
| Los diagnósticos son solo archivos locales (`.omsilaunch\diagnostics`); no se sube nada y no hay notificación remota | por diseño | [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| La retención conserva las 50 sesiones más recientes; los diagnósticos más antiguos con prefijo de sesión se eliminan cuando se inicia una sesión nueva | por diseño | como arriba |
| La salida JSON y los diagnósticos incluyen rutas de la instalación (`RootPath`, directorios de recursos, rutas `.itx`) | por diseño (datos locales) | |
| El cuadro de diálogo de fallo de la sesión de `OmsiLaunchW.exe` muestra como mensaje la carga útil de fallo del plugin (por ejemplo `{"name":"world.failed",...}`) en lugar de una frase; la línea `Code:` es correcta | estético | [bandeja de Windows](windows-tray.md) |
| Una solicitud D3D rechazada por el puente nativo antes de cualquier llamada a Direct3D notifica `native_status` correctamente, pero su texto `detail` indica `HRESULT 0x00000000` | estético | [capacidades](capabilities.md) |
| La ventana de estado de la bandeja es un snapshot de la sesión planificada tomado al abrirse; no se actualiza y no muestra valores de OMSI en tiempo real | por diseño | [bandeja de Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentación

Las páginas en inglés bajo `docs/` son la documentación normativa de esta versión. `docs/localized/<locale>/` contiene traducciones de las mismas páginas de `0.1.0-beta4` (consulta [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); cuando una traducción difiere del texto en inglés, el texto en inglés y el código son la referencia autorizada. Las páginas históricas y heredadas que allí se enumeran solo están disponibles en inglés.

Relacionado: [capacidades](capabilities.md), [estado de la validación en runtime](../status/runtime-validation-status.md), [errores](errors.md).
