# Ciclo de vida de la sesión

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Traducción de la [página original en inglés](../../../concepts/session-lifecycle.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página describe cómo avanza una sesión de OmsiLaunch por `SessionState` desde `Created` hasta `Completed` o `Failed`: qué componente establece cada estado, qué eventos de telemetría del plugin impulsan las transiciones, cómo funciona el timeout de arranque, qué significa detener (terminación forzada), qué devuelve `WaitForAsync`, qué estados son terminales, qué estados no son observables nunca o casi nunca, y qué garantías ofrece el propietario de la CLI. Todo lo que aquí figura procede de `OmsiLaunchService.StartAsync`, `SuperviseAsync` y `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) y `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Páginas relacionadas: [API pública](../reference/public-api.md), [códigos de error](../reference/errors.md), [transacciones y recuperación](transactions-and-recovery.md), [plugin permanente](permanent-plugin.md), [control de runtime](../reference/runtime-control.md), [plano de control local](../reference/local-control.md), [bandeja de Windows](../reference/windows-tray.md), [referencia de la CLI](../reference/cli.md), [estado de la validación en runtime](../status/runtime-validation-status.md), [el directorio `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Visión general

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

Valores en el orden de declaración. «Establecido por» nombra el código que llama a `Move`/`Fail`; «Observable» indica si `GetStatusAsync`/`WaitForAsync` pueden verlo en la práctica.

| # | Estado | Establecido por | Observable | Significado |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (valor inicial de la sesión activa) | Brevemente | La sesión está registrada; todavía no ha ocurrido nada. |
| 1 | `ValidatingPlatform` | nadie | No | Declarado, nunca lo establece el servicio actual (la validación de la plataforma se realiza en `PlanSessionAsync`, que no tiene estado de sesión). |
| 2 | `Planning` | nadie | No | Declarado, nunca se establece (la planificación ocurre antes de que exista una sesión; la nueva planificación en `StartSessionAsync` también precede al registro). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Sí | Se está adquiriendo el lease de la instalación. Error: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Sí | Se restaura un `journal.json` pendiente antes de leer la instalación activa; se valida el conjunto de archivos del plugin permanente (`plugin.integrity.reference`), se calcula el hash de `Omsi.exe`, se siembran los recursos del splash y se elimina un `closecheck` obsoleto. Errores: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (proceso registrado en el diario todavía vivo). |
| 5 | `Snapshotting` | `StartAsync` | Prácticamente no | Se establece inmediatamente antes de `ApplyingConfiguration` sin ningún await entre ambos; el snapshot en sí se toma dentro de `ApplyAsync`. Transitorio y no observable. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Sí | Se escribe `journal.json` (`Prepared`), se hacen copias de seguridad de los originales, se escriben los overlays y se eliminan las eliminaciones de sesión (`Applied`). Errores: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, errores de E/S. |
| 7 | `DeployingRuntime` | `StartAsync` | Sí | Estado del diario `RuntimeDeployed`. No se despliega ningún archivo: el conjunto de archivos del plugin es permanente. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Sí | Existen el handoff (`OmsiLaunch.Handoff.<id>`), el slot de telemetría (`OmsiLaunch.Telemetry.<id>`) y el buzón de runtime (`OmsiLaunch.Runtime.<id>`); estado del diario `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Sí | `CreateProcessW` para `<root>\Omsi.exe` con directorio de trabajo `<root>`. Errores: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Sí | El proceso existe, `process.started` está registrado, el diario está en `ProcessStarted`, el supervisor se ha iniciado y `StartSessionAsync` retorna. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` al recibir `plugin.started` | Sí | El plugin permanente ha leído un handoff válido para esta sesión. A partir de aquí, un timeout es `OL_E_STARTUP_TIMEOUT` en lugar de `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` al recibir `world.starting` | Sí (solo NEW_MAP) | El plugin ha invocado el inicio nativo NEW_MAP en el thread de la UI de OMSI. Las situaciones guardadas emiten `world.situation.starting`, que no está mapeado, por lo que una sesión SAVED_SITUATION pasa de `PluginBootstrap` directamente a `Running`. |
| 13 | `EnteringGameplay` | nadie | No | Declarado, nunca se establece: `gameplay.entered` lleva la sesión directamente a `Running`. |
| 14 | `Running` | `ApplyTelemetry` al recibir `gameplay.entered` | Sí | Se ha llegado a la fase de juego. `ExecuteRuntimeAsync` está permitido; el propietario de la CLI abre el plano de control local; la bandeja muestra una sesión en ejecución. |
| 15 | `ProcessExited` | `SuperviseAsync` | Sí (solo sesiones correctas) | OMSI ha terminado (de forma natural o por terminación), el diario está en `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (y la ruta de fallo del inicio) | Sí (solo sesiones correctas) | Cada archivo propiedad de la sesión se restaura a partir de su copia de seguridad verificada; se eliminan los artefactos de la sesión. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Sí (solo sesiones correctas) | Restauración verificada, diario y copias de seguridad eliminados; los almacenes del runtime están a punto de liberarse. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Sí, terminal | Almacenes liberados, buzón cerrado, lease liberado, ningún fallo registrado. |
| 19 | `Failed` | `LiveSession.Fail` desde `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Sí, terminal | Se ha registrado un diagnóstico de fallo. El estado es persistente: las llamadas posteriores a `Move` se ignoran, por lo que una sesión fallida nunca muestra `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, aunque la terminación y la restauración se sigan ejecutando. |

Estados terminales: `Completed` y `Failed`. Tras cualquiera de ellos, `WaitForAsync` retorna inmediatamente y `CloseAsync` retorna sin solicitar una detención.

Verificación de «nunca se establece»: una búsqueda en el código base de `SessionState.ValidatingPlatform`, `SessionState.Planning` y `SessionState.EnteringGameplay` solo encuentra la declaración del enum; `SessionState.Snapshotting` aparece una vez, seguido inmediatamente de `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Fase de inicio (`StartSessionAsync`)

1. Se rechaza un plan no ejecutable (`OL_E_PLAN_NOT_RUNNABLE`), se vuelve a planificar la especificación (recalculando el hash de `Omsi.exe`, resolviendo de nuevo el contenido y volviendo a comprobar el conjunto de archivos del plugin) y se rechaza otra vez si ya no es ejecutable. Se registra la sesión activa (`Created`).
2. Se crea la traza del host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (los archivos antiguos con prefijo de sesión que exceden las 50 sesiones más recientes se eliminan). Se rechaza un `StartupTimeoutSeconds` fuera de 1..600 (`ArgumentOutOfRangeException`; la sesión se da de baja del registro).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → se recupera un diario pendiente (un diario anterior a las huellas que no puede demostrar la propiedad se aplaza y se reintenta una vez que existen los overlays de esta sesión), se valida el conjunto de archivos del plugin permanente, se calcula el hash del ejecutable, se elimina un `closecheck` obsoleto y se construye la transacción (overlays: parches de `options.cfg`, BMP del splash gestionado, `Texture\standard.itx`; eliminaciones: destinos ITX, `Texture\standard.ipr`, `closecheck` cuando no existe).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`; después se inicia la tarea del supervisor y se devuelve el handle.
5. Cualquier excepción en los pasos 3–4 se captura: la sesión pasa a `Failed` con `OL_E_START_SESSION` (mensaje interno), un proceso ya creado se termina y se espera su salida, se liberan los almacenes, se restaura la transacción (`OL_E_RESTORE_FAILED` si falla) o, si no se pudo confirmar la salida de OMSI, se deja pendiente con `OL_E_RESTORE_DEFERRED`; se libera el lease. En este caso `StartSessionAsync` sigue devolviendo el handle; consulta `GetStatusAsync`.

La nueva planificación conserva el `SessionId` del llamador, por lo que el id del handle es igual a `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Supervisión (`SuperviseAsync`)

El supervisor se ejecuta en una tarea del grupo de threads y repite un bucle cada 100 ms hasta que OMSI ha terminado o se ha solicitado una detención:

1. Se lee cada evento de telemetría que el plugin ha confirmado desde la última lectura, en orden (un anillo de eventos; un lector que se ha quedado atrás recibe `telemetry.lost` con el recuento). Cada evento se añade a `RuntimeEvents` y se mapea mediante `ApplyTelemetry`.
2. Si la sesión está en `Failed`, se sale del bucle.
3. Si la sesión todavía no está en `Running` y ha vencido el plazo (`StartupTimeoutSeconds` desde la entrada en el supervisor): `Fail` con `OL_E_STARTUP_TIMEOUT` si se llegó a `PluginBootstrap`; en caso contrario, `OL_E_PLUGIN_NOT_LOADED`; se sale del bucle.

Después del bucle: si OMSI terminó antes de `Running` y no se registró ningún fallo, `Fail` con `OL_E_PROCESS_EXITED_EARLY`. A continuación, haya fallado o no la sesión: se termina OMSI si sigue vivo, se espera su salida, se marca el diario como `ProcessExited`, se pasa a `ProcessExited`, se restaura (`Restoring` → `CleaningRuntime`) o se registra `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, se liberan el handle del proceso, el handoff, el slot de telemetría y el buzón de runtime, se libera el lease y se pasa a `Completed`, salvo que el estado sea `Failed`. Un fallo dentro del propio supervisor se registra como `OL_E_PROCESS_SUPERVISION` (los problemas de limpieza como `OL_E_PROCESS_CLEANUP_FAILED`) y se ejecuta la misma ruta de terminación y restauración.

Como `Failed` es persistente, la única evidencia de que una sesión fallida se restauró es la ausencia de `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` en sus diagnósticos (y la ausencia de `journal.json`); las notas de restauración (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecen en ambos casos.

<a id="telemetry-events"></a>
## Eventos de telemetría

El plugin publica muestras JSON `{ "name": ..., "data": {...} }` en el slot de telemetría; el host registra cada muestra nueva como un `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emitido por | Acción del host |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` tras leer un handoff válido | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: falta `OMSILAUNCH_HANDOFF_NAME`, o el handoff es ilegible o no verificable | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: el handoff pide un modo de mundo distinto de NEW_MAP/SAVED_SITUATION, un inicio no headless, vehículo del jugador, modos de fecha/hora o una identidad de situación vacía | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: ha fallado la validación del build dentro del proceso | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | solo se registra |
| `headless.arm.failed` | `PluginRuntime.Start`: no se pudo armar el hook nativo de inicio headless | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | solo se registra |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` cuando `InternetTextures.Mode` es `Disabled` | solo se registra |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 o 4) | NEW_MAP: OMSI aún no está listo; el inicio se reintenta en el siguiente tick del temporizador de la UI | solo se registra |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | ruta correcta de NEW_MAP | solo se registra |
| `world.failed` (`native_status`) | NEW_MAP: el inicio nativo ha devuelto un fallo | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | ruta de SAVED_SITUATION | solo se registra (sin cambio de estado) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: el inicio nativo ha devuelto un fallo | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: campos de selección del punto de entrada o `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | final del inicio del mundo | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` una vez que cualquier operación `d3d.*` ha activado la sonda | solo se registra |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` cuando volver a aplicar un `camera.lock` activo lanza una excepción (se notifica una vez por cada error distinto) | solo se registra |
| JSON no válido | cualquiera | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Advertencias: el slot contiene una sola muestra, por lo que los eventos emitidos dentro de un mismo sondeo de 100 ms del host pueden perderse (el plugin suprime los eventos de ciclo de vida durante 2 s tras `gameplay.entered` y nunca publica un evento D3D en el tick que publica `gameplay.entered`, de modo que no se pierde el límite de `Running`). `RuntimeEvents` conserva los 256 eventos más recientes; los más antiguos se descartan. No es un registro sin pérdidas. Lee los eventos mediante `GetStatusAsync`, `session.events` en el plano de control o `events read|watch` en la CLI.

<a id="startup-timeout"></a>
## Timeout de arranque

| Elemento | Valor |
| --- | --- |
| Origen | `LaunchSpec.Behavior.StartupTimeoutSeconds` (predeterminado 180; 1..600; CLI `/startup-timeout`, perfil `behavior.startup-timeout`). |
| Inicio del reloj | Cuando la tarea del supervisor entra en su bucle (después de devolver el handle). |
| Vencimiento antes de `PluginBootstrap` | `Failed` con `OL_E_PLUGIN_NOT_LOADED`. |
| Vencimiento después de `PluginBootstrap`, antes de `Running` | `Failed` con `OL_E_STARTUP_TIMEOUT`. |
| Después de `Running` | No se aplica ningún timeout; la sesión dura hasta que OMSI termina o se solicita una detención. |
| Propietario de la CLI | Espera `StartupTimeoutSeconds + 5` segundos a `Running`; si falla, imprime el estado, con `OmsiLaunchW.exe` muestra un cuadro de diálogo con el último diagnóstico `OL_E_` (código alternativo `OL_E_SESSION_START_FAILED`) y termina con 1 tras `CloseAsync`. |

`ShutdownTimeoutSeconds` se incluye en la especificación, pero no se utiliza: no hay espera de cierre.

<a id="stop-semantics"></a>
## Semántica de la detención

Toda solicitud de detención es la misma solicitud canónica: `StopAsync(handle)` desde la API, `CloseAsync` sobre una sesión no terminal, `session.stop` en el plano de control local (vinculado al id de la sesión activa), «End session» (finalizar la sesión) en la bandeja, Ctrl+C o el cierre de la consola en el propietario de la CLI, y el final de `/observe-seconds`.

| Paso | Detalle |
| --- | --- |
| 1 | Se establece `StopRequested` en la sesión activa; el llamador retorna inmediatamente. |
| 2 | En menos de 100 ms el supervisor sale de su bucle y llama a `TerminateProcess(Omsi.exe, 1)`. Se trata de una terminación forzada: la rutina de cierre de OMSI no se ejecuta, OMSI no reescribe `options.cfg` y no aparece ningún cuadro de diálogo de guardado. Es deliberado, para que OMSI no pueda sobrescribir archivos que la transacción está a punto de restaurar. |
| 3 | El supervisor espera a que el proceso termine, registra `ProcessExited`, restaura exactamente cada archivo propiedad de la sesión (incluido el marcador `closecheck` que OMSI escribió durante la sesión, que se convierte en una nota `restore.session-artifact-removed`), elimina el diario y las copias de seguridad, libera los almacenes del runtime (las llamadas posteriores a `ExecuteRuntimeAsync` lanzan `OL_E_RUNTIME_CHANNEL_CLOSED` o `OL_E_SESSION_NOT_RUNNING`), libera el lease y pasa a `Completed`. |
| Salida natural | Si OMSI termina por sí mismo después de `Running` (el usuario cierra OMSI), se ejecuta la misma ruta sin terminación y la sesión se completa con normalidad. Antes de `Running` es `OL_E_PROCESS_EXITED_EARLY`. |
| Cierre cooperativo | No implementado. Enviar `WM_CLOSE` y esperar `ShutdownTimeoutSeconds` no está implementado (decisión de producto; OMSI ignoró `WM_CLOSE` enviado a su ventana principal en la ronda de cierre de runtime, `L05b`) ([estado de la validación en runtime](../status/runtime-validation-status.md)). |
| Estado del lado del runtime | Todo lo que se modifica mediante operaciones de runtime (reloj, cámara, vehículos generados, variables de script, texturas D3D) es estado interno del proceso y desaparece con él; nunca se restaura ni se conserva. |

<a id="waitforasync-semantics"></a>
## Semántica de `WaitForAsync`

| Situación | Resultado |
| --- | --- |
| La sesión llega al estado solicitado | Devuelve el estado con `State == requested`. |
| La sesión llega antes a un estado terminal | Retorna inmediatamente con `Completed` o `Failed` (consulta `Diagnostics`). |
| Vence el timeout | Devuelve el estado actual (sin excepción). Compara `State` con lo que pediste. |
| El estado solicitado ya ha pasado (o nunca se establece: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, en la práctica `Snapshotting`) | Espera hasta un estado terminal o el timeout. |
| El llamador cancela | `OperationCanceledException`. |
| Handle desconocido o cerrado | `KeyNotFoundException`. |

El intervalo de sondeo es de 100 ms, por lo que las transiciones observadas van hasta 100 ms por detrás de las reales.

<a id="owner-lifecycle-guarantees-cli"></a>
## Garantías del ciclo de vida del propietario (CLI)

`OwnerSession.RunAsync` en `tools/OmsiLaunch.Cli/Program.cs` es el propietario de referencia.

| Garantía | Detalle |
| --- | --- |
| Propietario único | Antes de iniciar, la CLI sondea la canalización de control; si responde un propietario, se niega con `OL_E_SESSION_ALREADY_ACTIVE` (salida 7). El lease impone la misma regla entre procesos. |
| Toda ruta de salida llega a `CloseAsync` | A partir de `StartSessionAsync`, las excepciones, Ctrl+C (`CancelKeyPress`), el cierre de la consola o el cierre de sesión de Windows (`ProcessExit`: se solicita la detención y el propietario espera hasta 4 s a `Completed`; lo que quede lo recupera el diario en el siguiente inicio), la detención desde la bandeja, `session.stop` del plano de control, el vencimiento de `/observe-seconds` y la finalización natural terminan todos en el bloque `finally`, que libera el plano de control y la bandeja y espera a `CloseAsync`. |
| `/observe-seconds` es un límite superior | Las solicitudes de detención desde la bandeja o el plano de control siguen pudiendo finalizar antes la sesión. |
| Plano de control solo mientras está en Running | El endpoint de la canalización con nombre se crea después de `Running` (y después de cualquier lote de validación `INTERNAL`) y se libera antes de `CloseAsync`; en otros momentos los clientes reciben `OL_E_NO_ACTIVE_SESSION`. |
| Código de salida | 0 cuando el estado final es `Completed`, 1 cuando es `Failed` o no se llegó a la fase de juego, 8 cuando una recuperación solicitada no se completó ([códigos de salida](../reference/exit-codes.md)). |
| Diagnósticos | Traza del host y artefactos de las operaciones de runtime en `<root>\.omsilaunch\diagnostics`, registro de la bandeja `tray-host.log`; ningún dato sale del ordenador. |

Los integradores que escriban su propio propietario deben reproducir las dos primeras garantías: un único `StartSessionAsync` por instalación a la vez, y `CloseAsync` en todas las rutas.

<a id="failure-map"></a>
## Mapa de fallos

| Fase | Estado al fallar | Diagnósticos que verás |
| --- | --- | --- |
| Plan | ninguno (no hay sesión) | `OL_E_PLAN_NOT_RUNNABLE` lanzado por `StartSessionAsync`; los códigos `OL_E_` propios del plan ([validación de LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Inicio (del lease a la creación del proceso) | `Failed` | `OL_E_START_SESSION` con el código interno; posiblemente `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Arranque del plugin | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Inicio del mundo | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| En ejecución | `Failed` solo ante fallos del supervisor | `OL_E_PROCESS_SUPERVISION`; los errores de las operaciones de runtime nunca hacen fallar la sesión. |
| Terminación y restauración | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Toda ruta de fallo sigue intentando la terminación y la restauración; un diario que permanezca se recupera en el siguiente inicio o mediante `RecoverPendingAsync` / `/recover` ([transacciones y recuperación](transactions-and-recovery.md)).
