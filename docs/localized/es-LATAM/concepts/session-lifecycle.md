# Ciclo de vida de la sesión

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Traducción de la [página original en inglés](../../../concepts/session-lifecycle.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página describe cómo una sesión de OmsiLaunch avanza por `SessionState` desde `Created` hasta `Completed` o `Failed`: qué componente establece cada estado, qué eventos de telemetría del plugin impulsan las transiciones, cómo funciona el timeout de arranque, qué significa detener (terminación forzada), qué devuelve `WaitForAsync`, qué estados son terminales, qué estados nunca o apenas son observables y qué garantías ofrece el propietario de la CLI. Todo lo que aparece aquí se toma de `OmsiLaunchService.StartAsync`, `SuperviseAsync` y `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) y `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Páginas relacionadas: [API pública](../reference/public-api.md), [códigos de error](../reference/errors.md), [transacciones y recuperación](transactions-and-recovery.md), [plugin permanente](permanent-plugin.md), [control de runtime](../reference/runtime-control.md), [plano de control local](../reference/local-control.md), [bandeja de Windows](../reference/windows-tray.md), [referencia de la CLI](../reference/cli.md), [estado de la validación en runtime](../status/runtime-validation-status.md), [el directorio `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Descripción general

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## Referencia de `SessionState`

Valores en orden de declaración. "Establecido por" nombra el código que llama a `Move`/`Fail`; "Observable" indica si `GetStatusAsync`/`WaitForAsync` pueden verlo en la práctica.

| # | Estado | Establecido por | Observable | Significado |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (valor inicial de la sesión activa) | Brevemente | La sesión está registrada; todavía no ocurrió nada. |
| 1 | `ValidatingPlatform` | nadie | No | Declarado, nunca establecido por el servicio actual (la validación de la plataforma ocurre en `PlanSessionAsync`, que no tiene estado de sesión). |
| 2 | `Planning` | nadie | No | Declarado, nunca establecido (la planificación ocurre antes de que exista una sesión; la nueva planificación en `StartSessionAsync` también precede al registro). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Sí | Se está adquiriendo el lease de la instalación. Falla: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Sí | Un `journal.json` pendiente se restaura antes de leer la instalación activa; se valida el conjunto de archivos del plugin permanente (`plugin.integrity.reference`), se calcula el hash de `Omsi.exe`, se siembran los assets de splash y se elimina un `closecheck` obsoleto. Fallas: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (el proceso registrado en el journal sigue vivo). |
| 5 | `Snapshotting` | `StartAsync` | Prácticamente no | Se establece inmediatamente antes de `ApplyingConfiguration` sin ningún await entre ambos; el snapshot en sí se toma dentro de `ApplyAsync`. Transitorio y no observable. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Sí | Se escribe `journal.json` (`Prepared`), se respaldan los originales, se escriben los overlays y se eliminan las rutas de eliminación de la sesión (`Applied`). Fallas: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, errores de E/S. |
| 7 | `DeployingRuntime` | `StartAsync` | Sí | Estado del journal `RuntimeDeployed`. No se despliega ningún archivo: el conjunto de archivos del plugin es permanente. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Sí | Existen el handoff (`OmsiLaunch.Handoff.<id>`), el slot de telemetría (`OmsiLaunch.Telemetry.<id>`) y el buzón de runtime (`OmsiLaunch.Runtime.<id>`); estado del journal `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Sí | `CreateProcessW` para `<root>\Omsi.exe` con directorio de trabajo `<root>`. Fallas: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Sí | El proceso existe, se registró `process.started`, el journal está en `ProcessStarted`, el supervisor se inició y `StartSessionAsync` retorna. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` en `plugin.started` | Sí | El plugin permanente leyó un handoff válido para esta sesión. A partir de aquí, un timeout es `OL_E_STARTUP_TIMEOUT` en lugar de `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` en `world.starting` | Sí (solo NEW_MAP) | El plugin invocó el inicio nativo de NEW_MAP en el thread de la interfaz de OMSI. Las situaciones guardadas emiten `world.situation.starting`, que no está mapeado, por lo que una sesión SAVED_SITUATION pasa de `PluginBootstrap` directamente a `Running`. |
| 13 | `EnteringGameplay` | nadie | No | Declarado, nunca establecido: `gameplay.entered` lleva la sesión directamente a `Running`. |
| 14 | `Running` | `ApplyTelemetry` en `gameplay.entered` | Sí | Se alcanzó el gameplay. `ExecuteRuntimeAsync` está permitido; el propietario de la CLI abre el plano de control local; la bandeja muestra una sesión en ejecución. |
| 15 | `ProcessExited` | `SuperviseAsync` | Sí (solo sesiones exitosas) | OMSI terminó (de forma natural o por terminación forzada); el journal está en `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (y la ruta de falla del inicio) | Sí (solo sesiones exitosas) | Cada archivo que pertenece a la sesión se restaura desde su copia de seguridad verificada; se eliminan los artefactos de la sesión. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Sí (solo sesiones exitosas) | Restauración verificada, journal y copias de seguridad eliminados; los almacenes de runtime están a punto de liberarse. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Sí, terminal | Almacenes liberados, buzón cerrado, lease liberado, ninguna falla registrada. |
| 19 | `Failed` | `LiveSession.Fail` desde `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Sí, terminal | Se registró un diagnóstico de falla. El estado es persistente: las llamadas posteriores a `Move` se ignoran, por lo que una sesión fallida nunca muestra `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, aunque la terminación y la restauración igualmente se ejecutan. |

Estados terminales: `Completed` y `Failed`. Después de cualquiera de ellos, `WaitForAsync` retorna de inmediato y `CloseAsync` retorna sin solicitar una detención.

Verificación de "nunca establecido": una búsqueda en el código base de `SessionState.ValidatingPlatform`, `SessionState.Planning` y `SessionState.EnteringGameplay` encuentra solo la declaración del enum; `SessionState.Snapshotting` aparece una vez, seguido inmediatamente por `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Fase de inicio (`StartSessionAsync`)

1. Rechazar un plan no ejecutable (`OL_E_PLAN_NOT_RUNNABLE`), volver a planificar la especificación (recalculando el hash de `Omsi.exe`, volviendo a resolver el contenido y volviendo a comprobar el conjunto de archivos del plugin) y rechazarla de nuevo si ya no es ejecutable. Registrar la sesión activa (`Created`).
2. Crear la traza del host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (los archivos más antiguos con prefijo de sesión que superan las 50 sesiones más recientes se eliminan). Rechazar `StartupTimeoutSeconds` fuera de 1..600 (`ArgumentOutOfRangeException`; se anula el registro de la sesión).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → recuperar un journal pendiente (un journal anterior a las huellas de propiedad que no puede probar la propiedad se posterga y se reintenta cuando ya existen los overlays de esta sesión), validar el conjunto de archivos del plugin permanente, calcular el hash del archivo ejecutable, eliminar un `closecheck` obsoleto, construir la transacción (overlays: parches de `options.cfg`, BMP de splash administrados, `Texture\standard.itx`; eliminaciones: destinos de ITX, `Texture\standard.ipr`, `closecheck` cuando no existe).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`; luego se inicia la tarea del supervisor y se devuelve el handle.
5. Cualquier excepción en los pasos 3–4 se captura: la sesión queda `Failed` con `OL_E_START_SESSION` (mensaje interno), un proceso ya creado se termina y se espera su salida, los almacenes se liberan, la transacción se restaura (`OL_E_RESTORE_FAILED` si falla) o, si no se pudo confirmar la salida de OMSI, queda pendiente con `OL_E_RESTORE_DEFERRED`; el lease se libera. En este caso `StartSessionAsync` igualmente devuelve el handle; consulte `GetStatusAsync`.

La nueva planificación conserva el `SessionId` del llamador, por lo que el id del handle es igual a `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Supervisión (`SuperviseAsync`)

El supervisor se ejecuta en una tarea del thread pool y repite un ciclo cada 100 ms hasta que OMSI termina o se solicita una detención:

1. Leer todos los eventos de telemetría que el plugin confirmó desde la última lectura, en orden (un anillo de eventos; un lector que se quedó atrás recibe `telemetry.lost` con la cantidad). Cada evento se agrega a `RuntimeEvents` y se mapea mediante `ApplyTelemetry`.
2. Si la sesión está `Failed`, salir del ciclo.
3. Si la sesión todavía no está en `Running` y el plazo (`StartupTimeoutSeconds` después de la entrada del supervisor) ya venció: `Fail` con `OL_E_STARTUP_TIMEOUT` cuando se alcanzó `PluginBootstrap`; de lo contrario, `OL_E_PLUGIN_NOT_LOADED`; salir del ciclo.

Después del ciclo: si OMSI terminó antes de `Running` y no se registró ninguna falla, `Fail` con `OL_E_PROCESS_EXITED_EARLY`. Luego, haya fallado o no la sesión: terminar OMSI si sigue vivo, esperar su salida, marcar el journal como `ProcessExited`, pasar a `ProcessExited`, restaurar (`Restoring` → `CleaningRuntime`) o registrar `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, liberar el handle del proceso, el handoff, el slot de telemetría y el buzón de runtime, liberar el lease y pasar a `Completed` salvo que el estado sea `Failed`. Una falla dentro del propio supervisor se registra como `OL_E_PROCESS_SUPERVISION` (los problemas de limpieza, como `OL_E_PROCESS_CLEANUP_FAILED`) y se ejecuta la misma ruta de terminación y restauración.

Como `Failed` es persistente, la única evidencia de que una sesión fallida se restauró es la ausencia de `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` en sus diagnósticos (y la ausencia de `journal.json`); las notas de restauración (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecen en ambos casos.

<a id="telemetry-events"></a>
## Eventos de telemetría

El plugin publica muestras JSON `{ "name": ..., "data": {...} }` en el slot de telemetría; el host registra cada muestra nueva como un `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emitido por | Acción del host |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` después de leer un handoff válido | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: falta `OMSILAUNCH_HANDOFF_NAME`, o el handoff no se puede leer o verificar | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: el handoff solicita un modo de mundo distinto de NEW_MAP/SAVED_SITUATION, un inicio no headless, vehículo del jugador, modos de fecha/hora o una identidad de situación vacía | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: falló la validación del build dentro del proceso | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | solo se registra |
| `headless.arm.failed` | `PluginRuntime.Start`: no se pudo armar el hook nativo de inicio headless | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | solo se registra |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` cuando `InternetTextures.Mode` es `Disabled` | solo se registra |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 o 4) | NEW_MAP: OMSI todavía no está listo; el inicio se reintenta en el siguiente tick del temporizador de la interfaz | solo se registra |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | ruta de éxito de NEW_MAP | solo se registra |
| `world.failed` (`native_status`) | NEW_MAP: el inicio nativo devolvió una falla | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | ruta SAVED_SITUATION | solo se registra (sin cambio de estado) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: el inicio nativo devolvió una falla | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: campos de selección del punto de entrada o `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | fin del inicio del mundo | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` una vez que alguna operación `d3d.*` activó la sonda | solo se registra |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` cuando volver a aplicar un `camera.lock` activo lanza una excepción (se informa una vez por cada error distinto) | solo se registra |
| JSON no válido | cualquiera | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Advertencias: el slot contiene una sola muestra, por lo que los eventos emitidos dentro de un mismo sondeo de 100 ms del host pueden perderse (el plugin suprime los eventos de ciclo de vida durante 2 s después de `gameplay.entered` y nunca publica un evento D3D en el tick que publica `gameplay.entered`, de modo que no se pierde el límite de `Running`). `RuntimeEvents` conserva los 256 eventos más recientes; los más antiguos se descartan. No es un registro sin pérdidas. Lea los eventos mediante `GetStatusAsync`, `session.events` en el plano de control, o `events read|watch` en la CLI.

<a id="startup-timeout"></a>
## Timeout de arranque

| Elemento | Valor |
| --- | --- |
| Origen | `LaunchSpec.Behavior.StartupTimeoutSeconds` (predeterminado 180; 1..600; CLI `/startup-timeout`, perfil `behavior.startup-timeout`). |
| Inicio del reloj | Cuando la tarea del supervisor entra en su ciclo (después de que se devolvió el handle). |
| Vencimiento antes de `PluginBootstrap` | `Failed` con `OL_E_PLUGIN_NOT_LOADED`. |
| Vencimiento después de `PluginBootstrap`, antes de `Running` | `Failed` con `OL_E_STARTUP_TIMEOUT`. |
| Después de `Running` | No se aplica ningún timeout; la sesión dura hasta que OMSI termina o se solicita una detención. |
| Propietario de la CLI | Espera `StartupTimeoutSeconds + 5` segundos a `Running`; si falla, imprime el estado; bajo `OmsiLaunchW.exe` muestra un diálogo con el último diagnóstico `OL_E_` (código de respaldo `OL_E_SESSION_START_FAILED`) y sale con 1 después de `CloseAsync`. |

`ShutdownTimeoutSeconds` se incluye en la especificación pero no se consume: no hay espera de apagado.

<a id="stop-semantics"></a>
## Semántica de detención

Toda solicitud de detención es la misma solicitud canónica: `StopAsync(handle)` desde la API, `CloseAsync` sobre una sesión no terminal, `session.stop` en el plano de control local (vinculado al id de la sesión activa), "End session" (finalizar sesión) en la bandeja, Ctrl+C o el cierre de la consola en el propietario de la CLI, y el fin de `/observe-seconds`.

| Paso | Detalle |
| --- | --- |
| 1 | Se establece `StopRequested` en la sesión activa; el llamador retorna de inmediato. |
| 2 | En un plazo de 100 ms el supervisor sale de su ciclo y llama a `TerminateProcess(Omsi.exe, 1)`. Es una terminación forzada: la rutina de apagado de OMSI no se ejecuta, OMSI no reescribe `options.cfg` y no aparece ningún diálogo de guardado. Esto es deliberado, para que OMSI no pueda sobrescribir archivos que la transacción está por restaurar. |
| 3 | El supervisor espera a que el proceso termine, registra `ProcessExited`, restaura exactamente cada archivo que pertenece a la sesión (incluido el marcador `closecheck` que OMSI escribió durante la sesión, que se convierte en una nota `restore.session-artifact-removed`), elimina el journal y las copias de seguridad, libera los almacenes de runtime (las llamadas posteriores a `ExecuteRuntimeAsync` lanzan `OL_E_RUNTIME_CHANNEL_CLOSED` o `OL_E_SESSION_NOT_RUNNING`), libera el lease y pasa a `Completed`. |
| Salida natural | Si OMSI termina por sí solo después de `Running` (el usuario cierra OMSI), se ejecuta la misma ruta sin terminación forzada y la sesión se completa normalmente. Antes de `Running` es `OL_E_PROCESS_EXITED_EARLY`. |
| Apagado cooperativo | No implementado. Enviar `WM_CLOSE` y esperar `ShutdownTimeoutSeconds` no está implementado (decisión de producto; OMSI ignoró `WM_CLOSE` enviado a su ventana principal en la ronda de cierre de runtime, `L05b`) ([estado de la validación en runtime](../status/runtime-validation-status.md)). |
| Estado del lado del runtime | Todo lo que se cambia mediante operaciones de runtime (reloj, cámara, vehículos generados, variables de script, texturas D3D) es estado dentro del proceso y desaparece con el proceso; nunca se restaura ni se conserva. |

<a id="waitforasync-semantics"></a>
## Semántica de `WaitForAsync`

| Situación | Resultado |
| --- | --- |
| La sesión alcanza el estado solicitado | Devuelve el estado con `State == requested`. |
| La sesión alcanza primero un estado terminal | Retorna de inmediato con `Completed` o `Failed` (revise `Diagnostics`). |
| Vence el timeout | Devuelve el estado actual (sin excepción). Compare `State` con lo que solicitó. |
| El estado solicitado ya pasó (o nunca se establece: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, en la práctica `Snapshotting`) | Espera hasta un estado terminal o hasta el timeout. |
| El llamador cancela | `OperationCanceledException`. |
| Handle desconocido o cerrado | `KeyNotFoundException`. |

El intervalo de sondeo es de 100 ms, por lo que las transiciones observadas se retrasan hasta 100 ms respecto de las reales.

<a id="owner-lifecycle-guarantees-cli"></a>
## Garantías del ciclo de vida del propietario (CLI)

`OwnerSession.RunAsync` en `tools/OmsiLaunch.Cli/Program.cs` es el propietario de referencia.

| Garantía | Detalle |
| --- | --- |
| Propietario único | Antes de iniciar, la CLI sondea el pipe de control; si un propietario responde, se niega con `OL_E_SESSION_ALREADY_ACTIVE` (salida 7). El lease impone la misma regla entre procesos. |
| Toda ruta de salida llega a `CloseAsync` | A partir de `StartSessionAsync`, las excepciones, Ctrl+C (`CancelKeyPress`), el cierre de la consola o del inicio de sesión de Windows (`ProcessExit`: se solicita la detención y el propietario espera hasta 4 s a `Completed`; lo que quede pendiente lo recupera el journal en el siguiente inicio), la detención desde la bandeja, `session.stop` del plano de control, el vencimiento de `/observe-seconds` y la finalización natural terminan todos en el bloque `finally`, que libera el plano de control y la bandeja y espera `CloseAsync`. |
| `/observe-seconds` es un límite superior | Las solicitudes de detención desde la bandeja o el plano de control igualmente finalizan la sesión antes. |
| Plano de control solo durante Running | El endpoint de named pipe se crea después de `Running` (y después de cualquier lote de validación `INTERNAL`) y se libera antes de `CloseAsync`; en otros momentos los clientes reciben `OL_E_NO_ACTIVE_SESSION`. |
| Código de salida | 0 cuando el estado final es `Completed`, 1 cuando es `Failed` o no se alcanzó el gameplay, 8 cuando una recuperación solicitada no se completó ([códigos de salida](../reference/exit-codes.md)). |
| Diagnósticos | Traza del host y artefactos de operaciones de runtime en `<root>\.omsilaunch\diagnostics`, log de la bandeja `tray-host.log`; ningún dato sale de la computadora. |

Los integradores que escriban su propio propietario deben reproducir las dos primeras garantías: un solo `StartSessionAsync` por instalación a la vez y `CloseAsync` en toda ruta.

<a id="failure-map"></a>
## Mapa de fallas

| Fase | Estado al fallar | Diagnósticos que verá |
| --- | --- | --- |
| Plan | ninguno (sin sesión) | `OL_E_PLAN_NOT_RUNNABLE` lanzado por `StartSessionAsync`; los propios códigos `OL_E_` del plan ([validación de LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Inicio (del lease a la creación del proceso) | `Failed` | `OL_E_START_SESSION` con el código interno; posiblemente `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Arranque del plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Inicio del mundo | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| En ejecución | `Failed` solo ante fallas del supervisor | `OL_E_PROCESS_SUPERVISION`; los errores de operaciones de runtime nunca hacen fallar la sesión. |
| Terminación y restauración | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Toda ruta de falla igualmente intenta la terminación y la restauración; un journal que quede se recupera en el siguiente inicio o mediante `RecoverPendingAsync` / `/recover` ([transacciones y recuperación](transactions-and-recovery.md)).
