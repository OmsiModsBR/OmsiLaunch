# Referencia de la CLI

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Traducción de la [página original en inglés](../../../reference/cli.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página es la referencia completa y normativa de la línea de comandos de OmsiLaunch `0.1.0-beta4`: los tres ejecutables, la gramática de argumentos, el orden de despacho, cada palabra de comando, cada ruta jerárquica, cada flag, los envelopes de salida y el comportamiento ante errores de cada comando. Se genera a partir de `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` y los dos shims nativos de `tools\OmsiLaunch.Bootstrapper`. Los resultados del proceso se enumeran en [códigos de salida](exit-codes.md); los códigos de error, en [errores](errors.md); las invocaciones de ejemplo, en [ejemplos de la CLI](cli-examples.md).

<a id="executables"></a>
## Ejecutables

| Archivo | Subsistema | Función | Diferencias |
|---|---|---|---|
| `OmsiLaunch.exe` | Consola | Bootstrapper nativo (`OmsiLaunch.Bootstrapper.cpp`): resuelve su propio directorio, divide la línea de comandos en tokens con `CommandLineToArgvW`, localiza `hostfxr` mediante `nethost.dll` y ejecuta `OmsiLaunch.Controller.dll` con los mismos argumentos. | Se escribe salida en la consola; el código de salida del proceso es el del controlador administrado o un código del shim `100`..`106` si no se pudo iniciar el host de .NET. |
| `OmsiLaunchW.exe` | Windows (GUI) | El mismo shim (`OmsiLaunch.WindowsHost.cpp`) compilado para el subsistema Windows. Establece la variable de entorno `OMSILAUNCH_WINDOWS_HOST=1` antes de iniciar el controlador. | Sin consola: la salida de consola se suprime salvo que se indique `--json` (`WindowsHost.SuppressConsole`), los fallos se muestran como cuadros de mensaje (`WindowsHost.ShowFailure`: mensaje, `Code: OL_E_...` y la indicación `See .omsilaunch\diagnostics for details.`), y un fallo del shim `100`..`106` se muestra como `OmsiLaunch could not start the .NET host (code N).` Comportamiento completo: [referencia de OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Administrado (x64, `net6.0-windows`, Windows Forms) | El propio controlador. Los usuarios nunca lo invocan directamente; ambos shims pasan la ruta del controlador como primer argumento del host, de modo que nunca aparece en la lista pública de argumentos. | Requiere el runtime de .NET 6 x64 con `Microsoft.WindowsDesktop.App`; consulta [instalación](../getting-started/installation.md). |

`nethost.dll` debe estar junto a los shims. Los shims no leen ningún argumento por sí mismos; todos los argumentos llegan sin cambios a `CliInput.Parse`, por lo que `OmsiLaunch.exe` y `OmsiLaunchW.exe` aceptan exactamente la misma sintaxis.

<a id="invocation-model"></a>
## Modelo de invocación

<a id="argument-grammar-cliinputparse"></a>
### Gramática de argumentos (`CliInput.Parse`)

| Forma | Significado |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Un flag. La clave no distingue mayúsculas de minúsculas; el valor es todo lo que sigue al primer `:`. Las claves desconocidas fallan con `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), salida `2`. |
| `--key=value` | Un argumento de runtime para la operación de runtime seleccionada (por ejemplo, `--handle=rv-000001`). Todo token `--` que contenga `=` es un argumento de runtime, nunca un flag. |
| `--json`, `/json` | Salida estructurada (consulta [Formatos de salida](#output-formats)). `--json` es el único token `--` sin `=` que tiene significado; se analiza como el flag `/json`. |
| palabra suelta | Si aún no se ha visto ninguna palabra de comando y la palabra es una de las [palabras de comando](#command-words), se convierte en el comando. Una vez presente una palabra de comando, toda palabra suelta posterior es una palabra de comando (la ruta). En caso contrario, la primera palabra suelta es la raíz de la instalación y cualquier palabra suelta posterior se añade a la ruta. |

Consecuencias: una ruta jerárquica (`time get`) no se puede combinar con un argumento de instalación colocado después de ella (`time get D:\OMSI` es la ruta desconocida `time get d:\omsi`, salida `2`). `D:\OMSI time get` se acepta, pero es **modo propietario** (se inicia una sesión nueva y la operación se ejecuta una vez dentro de ella). Los errores de análisis (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) y los errores del perfil de sesión (`SessionProfileException`) se notifican antes de ejecutar nada, siempre con salida `2`.

<a id="installation-root"></a>
### Raíz de la instalación

- Un argumento de instalación explícito en forma de palabra suelta tiene prioridad sobre `RootPath` en un archivo `/spec` (`CliInput.BuildSpecAsync`).
- `.` significa el directorio que contiene el ejecutable (`AppContext.BaseDirectory`), nunca el directorio de trabajo del llamador (`CliInput.ResolveInstallationRoot`). Un paquete portable depende de esto.
- Cuando se omite el argumento, las operaciones en modo propietario (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) también usan el directorio del ejecutable. La ruta se normaliza con `Path.GetFullPath`.
- Los comandos en modo cliente nunca aceptan un argumento de instalación: se dirigen al endpoint de control local de la instalación en la que reside el ejecutable (`AppContext.BaseDirectory`). Consulta [control local](local-control.md).

<a id="owner-and-client"></a>
### Propietario y cliente

- **Propietario**: el proceso que planifica, inicia, supervisa y restaura una sesión (`OwnerSession.RunAsync`). Mantiene el lease de la instalación (`Local\OmsiLaunch.Installation.<sha256(root)>`) y la transacción de configuración, expone el endpoint de control local mientras la sesión está activa y muestra el [icono de la bandeja](windows-tray.md). Exactamente un propietario por instalación: si un propietario ya responde a `session.status` en el endpoint de control, un segundo lanzamiento falla con `OL_E_SESSION_ALREADY_ACTIVE` (salida `7`).
- **Cliente**: cualquier invocación sin argumento de instalación que envíe `session status`, `session stop`, `events read`, `events watch` o una operación de runtime. Se reenvía por la canalización de control local; sin propietario falla con `OL_E_NO_ACTIVE_SESSION` (salida `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Orden de despacho (`CliProgram.RunAsync`)

1. `/silent` (cuando no se está ejecutando ya bajo `OmsiLaunchW.exe`): inicia `OmsiLaunchW.exe` desde el directorio del ejecutable mediante `ShellExecute` (sin herencia de handles) con los mismos argumentos excepto `/silent`/`--silent`, escribe el envelope `silent` (`delegated`, `host_process_id`) y devuelve `0`. El proceso de consola no espera a la sesión; consulta [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` devuelven `7`.
2. `/version`: envelope `version` con `product`, `version` (versión informativa del ensamblado, tomada de `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); salida `0`.
3. `capabilities`: envelope con todos los descriptores `PublicStableBeta` o `PublicExperimental` del catálogo canónico `PublicCapabilityRegistry` (con sus operaciones, capacidades nativas, evidencia y nota); salida `0`.
4. `help [family]`: envelope `help` con `usage`, `product_version`, `protocol_version`, `family` y los `commands` públicos (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), filtrados opcionalmente por familia; salida `0`.
5. `profiles`: envelope con `requirement` (`OMSI 2`), el informe `installation` de la raíz de destino (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), los `known_layouts` y las `predicted_capabilities` resueltas a partir del archivo ejecutable (la resolución en el proceso al iniciar es la que prevalece); salida `0`.
6. Operación de runtime de cliente (sin argumento de instalación y con una ruta o `/runtime:`): los argumentos se validan con `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, salida `2`) y después se reenvía `runtime.execute` con un timeout de 8 s (30 s para `road-vehicles.spawn`).
7. `session status` de cliente (750 ms), `session stop` (vinculado al id de la sesión activa, 750 ms), `events read` (750 ms), `events watch` (sondea cada 250 ms hasta Ctrl+C).
8. `detect`, o **ningún argumento en absoluto** (sin instalación, sin comando, sin `/?`, sin `/spec`, sin flag de lanzamiento, sin flag de recuperación, sin `/list`): enumera los procesos `Omsi` y sondea el endpoint de control (250 ms); envelope `detect`; salida `0`.
9. `/?` o `/help`: imprime el texto de uso, salida `0`. Cualquier otra invocación que tenga una palabra de comando pero ninguna ruta despachable (por ejemplo, `d3d` sola o `session status D:\OMSI`) imprime el texto de uso y sale con `2`.
10. Modo propietario. Condiciones previas: `plugins\OmsiLaunch.Plugin.opl` y `plugins\OmsiLaunch.Native.x86.dll` deben existir junto al ejecutable (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, salida `7`). `release-manifest.json` junto al ejecutable, cuando existe, proporciona los hashes esperados del plugin.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; envelope `recover` con `pending`, `recovered`, `diagnostics`; salida `8` solo cuando se solicitó una restauración y no se completó; en caso contrario, `0`.
12. `/list:<category>`: `DiscoverAsync`; envelope `content.list`; salida `0`. `/list:SessionProfiles` usa en su lugar `ListSessionProfilesAsync`; envelope `session-profiles.list`.
13. Construye el `LaunchSpec` (`BuildSpecAsync`), lo planifica (`PlanSessionAsync`) e imprime el plan. `/plan` o `/validate`: salida `0` si `IsRunnable`; si no, `1`. Un plan no ejecutable nunca inicia OMSI (salida `1`); bajo `OmsiLaunchW.exe`, un lanzamiento con un plan no ejecutable muestra su último diagnóstico `OL_E_` en un cuadro de mensaje (auditoría de documentación BUG-06). La planificación también verifica el conjunto de archivos del plugin permanente instalado frente a `release-manifest.json`, de modo que un plugin ausente o modificado hace que el plan no sea ejecutable (`OL_E_PERMANENT_PLUGIN_*`).
14. Sondea si existe un propietario (`OL_E_SESSION_ALREADY_ACTIVE`, salida `7`) y después `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Ciclo de vida del propietario (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. A partir de aquí, toda ruta de salida llega a `CloseAsync` en un bloque `finally`: excepciones, Ctrl+C (`Console.CancelKeyPress`), cierre de la consola / cierre de sesión de Windows (`AppDomain.ProcessExit` con un margen de 4 s para detención + restauración; lo que quede lo recupera el diario en el siguiente inicio), «End session» (finalizar la sesión) de la bandeja, `session.stop` por la canalización y `/observe-seconds`.
2. Se crea el icono de la bandeja, salvo que `Presentation.SuppressTrayIcon` esté establecido en el spec.
3. Espera `Running` durante `StartupTimeoutSeconds + 5` segundos. Se imprime el estado. Si el estado no es `Running`, salida `1` (`OmsiLaunchW.exe` muestra `The OMSI session did not reach gameplay.` con el último diagnóstico `OL_E_` u `OL_E_SESSION_START_FAILED`).
4. Se ejecutan los lotes de validación (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) y escriben sus artefactos.
5. Se inicia el endpoint de control local.
6. `/runtime:<operation>` se ejecuta una vez (5 s, 15 s para `road-vehicles.spawn`); el resultado se escribe en `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` y se imprime. Un comando de runtime que falla nunca termina la sesión (en su lugar se imprime `runtime_error`).
7. Espera: con `/observe-seconds:n`, la sesión se detiene tras `n` segundos **o** antes ante una detención desde la bandeja o la canalización, o cuando OMSI termina; sin él, el propietario espera hasta que OMSI termine o se solicite una detención.
8. Se imprime el estado final; salida `0` si `Completed`, `1` en caso contrario.

`session.stop`, «End session» de la bandeja, Ctrl+C y `CloseAsync` solicitan todos la detención canónica: OMSI se termina con `TerminateProcess` (la rutina de cierre propia de OMSI no se ejecuta y OMSI no reescribe `options.cfg`) y después se restauran todos los archivos propiedad de la sesión. Consulta [ciclo de vida de la sesión](../concepts/session-lifecycle.md) y [transacciones y recuperación](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Palabras de comando

Todas las palabras aceptadas en primera posición (`CliInput.CommandWordsAccepted`):

| Palabra | Propósito | Modo | Notas |
|---|---|---|---|
| `capabilities` | Enumera las capacidades públicas | Local, sin sesión | Envelope `capabilities`. |
| `profiles` | Enumera las variantes de `Omsi.exe` compatibles | Local, sin sesión | Envelope `profiles`. |
| `detect` | Informa de los procesos `Omsi.exe` y de un propietario activo | Local, sin sesión | También es el valor predeterminado cuando no se indica ningún argumento. Estados: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, `UNKNOWN_BINARY_FOUND` por proceso cuando no se puede inspeccionar el binario; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Uso y catálogo público de comandos | Local, sin sesión | `help <family>` filtra por familia de capacidades (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Cliente | Exactamente una palabra a continuación; cualquier otra cosa imprime el uso, salida `2`. `session plan`/`session start` son nombres de rutas de la API, no palabras de la CLI: usa `/plan` y `/new`. |
| `events` | `events read`, `events watch` | Cliente | `read` devuelve una vez la lista acotada de eventos; `watch` imprime cada evento nuevo (por `Sequence`) como un envelope `events.watch` cada 250 ms hasta Ctrl+C (salida `0`), `4` cuando no responde ningún propietario, `7` ante un error de control. |
| `time` | `time get`, `time set` | Ruta de cliente | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Ruta de cliente | |
| `map` | `map get` | Ruta de cliente | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Ruta de cliente | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Ruta de cliente | |
| `player` | `player get` | Ruta de cliente | |
| `humans` | `humans list`, `humans get`, `humans summary` | Ruta de cliente | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Ruta de cliente | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Ruta de cliente | |
| `constants` | `constants list`, `constants get` | Ruta de cliente | |
| `curves` | `curves list`, `curves evaluate` | Ruta de cliente | |
| `hof` | `hof get` | Ruta de cliente | |
| `drivers` | `drivers list` | Ruta de cliente | |
| `tickets` | `tickets get` | Ruta de cliente | |
| `d3d` | Palabra de familia reservada | Ninguno | `d3d` **no tiene ruta jerárquica**: `d3d texture ...` es una ruta desconocida (salida `2`) y `d3d` sola imprime el uso (salida `2`). A las operaciones D3D se accede con `/runtime:d3d.status`, `/runtime:d3d.texture.create`, etc. (consulta [Operaciones sin ruta](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Rutas jerárquicas

`CliInput.HierarchicalRoutes` asigna una ruta en minúsculas a un id de operación de runtime. Todas las rutas requieren una sesión en `Running` y se ejecutan a través del buzón de runtime (`ExecuteRuntimeAsync`). Las escrituras de runtime solo cambian el estado en memoria de OMSI: nunca tocan archivos, no forman parte de la transacción de configuración y **no** se revierten en la detención (OMSI se termina). La estabilidad sigue a `PublicCapabilityRegistry` y a la [matriz de validación](../status/runtime-validation-status.md); los detalles y los campos de resultado están en [control de runtime](runtime-control.md).

| Ruta | Operación de runtime | Tipo | Requiere Running | Modifica OMSI | Participación en la restauración | Estabilidad | Notas |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Sí | No | Ninguna | STABLE_BETA | Campos de reloj y calendario. |
| `time set` | `time.set` | Write | Sí | Sí (reloj en memoria) | Ninguna, no se revierte | EXPERIMENTAL | Por ejemplo, `--minute=<0..59>`; escritura, relectura y restauración validadas el 2026-09-20. |
| `weather get` | `weather.read` | Read | Sí | No | Ninguna | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Sí | Sí (meteorología en memoria) | Ninguna, no se revierte | EXPERIMENTAL | Por ejemplo `--wind_speed=5 --cloud_type="Cumulus 1"`. Meteorología manual: se detienen las actualizaciones METAR en vivo. Validación en runtime pendiente. |
| `weather metar set` | `weather.metar.set` | Write | Sí | Sí (meteorología en memoria; OMSI guarda el ICAO en `options.cfg`, que la sesión restaura) | Ninguna, no se revierte | EXPERIMENTAL | `--icao=<ICAO>`. Meteorología METAR en vivo, como la establece el diálogo meteorológico de OMSI. Validación en runtime pendiente. |
| `weather actual get` | `weather.actual.read` | Read | Sí | No | Ninguna | EXPERIMENTAL | Estado del controlador real/ICAO. |
| `map get` | `map.read` | Read | Sí | No | Ninguna | STABLE_BETA | Nombre del mapa, archivo, descripción, número de tiles, rango de años y sentido de circulación; revalidado en runtime sobre el slot de mapa corregido. |
| `camera get` | `camera.read` | Read | Sí | No | Ninguna | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Sí | Sí (escalares de cámara, p. ej. `--field_of_view=`) | Ninguna, no se revierte | EXPERIMENTAL | Escritura/relectura del FOV validada. |
| `camera lock` | `camera.lock` | Action | Sí | Sí (política limitada a la sesión) | Ninguna | EXPERIMENTAL | Requiere `--family=<0..3>` (conductor=0, pasajero=1, exterior=2, mapa=3), `--preset=<n>` opcional (familia 0 o 1). Necesita un vehículo del jugador (por ejemplo, una situación guardada). Validado en runtime en el cierre de runtime (`CAM01`); la cadena `RuntimeValidation` del registro sigue indicando `STATICALLY_VALIDATED` (consulta [capacidades](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Sí | Sí | Ninguna | EXPERIMENTAL | Libera la política establecida por `camera lock` (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Sí | No | Ninguna | STABLE_BETA | Devuelve handles `rv-NNNNNN` limitados a la sesión. |
| `vehicles get` | `road-vehicle.read` | Read | Sí | No | Ninguna | STABLE_BETA | Requiere `--handle=`. Handle obsoleto: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Sí | No | Ninguna | STABLE_BETA | Recuentos y estado del jugador, sin handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Sí | Sí (añade un RoadVehicle) | Ninguna, no se elimina | EXPERIMENTAL | Requiere `--model=Vehicles\...\*.bus`. Timeout de 30 s en el cliente y de 15 s en el propietario. No asigna el vehículo del jugador. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Sí | Sí | Ninguna | EXPERIMENTAL | `PlaceRandomBus` perfilado. |
| `player get` | `player-vehicle.read` | Read | Sí | No | Ninguna | STABLE_BETA | Null semántico cuando no hay vehículo del jugador. |
| `humans list` | `humans.list` | Read | Sí | No | Ninguna | EXPERIMENTAL | Devuelve handles `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Sí | No | Ninguna | EXPERIMENTAL | Requiere `--handle=`. |
| `humans summary` | `humans.read` | Read | Sí | No | Ninguna | EXPERIMENTAL | Solo recuentos. |
| `timetable get` | `timetable.read` | Read | Sí | No | Ninguna | STABLE_BETA | Estado del gestor de horarios. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Sí | No | Ninguna | STABLE_BETA | Parte de la capacidad `timetable.read`; evidencia de lectura por lotes del 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable lines list` | `timetable.lines.list` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable tours list` | `timetable.tours.list` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable logs list` | `timetable.logs.read` | Read | Sí | No | Ninguna | STABLE_BETA | Igual que la anterior. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Sí | No | Ninguna | EXPERIMENTAL | Números de vehículo que OMSI mantiene fuera del uso libre de la IA (reservados por tours de horario o por listas fijas de IA). Paginado (`cursor`, `limit`, como máximo 256 por página). |
| `map tiles list` | `map.tiles.list` | Read | Sí | No | Ninguna | EXPERIMENTAL | Los tiles del mapa: posición en la cuadrícula, índice propio, archivo y estado de carga, recuentos de objetos, trayectos y splines. Paginado (`cursor`, `limit`, como máximo 256 por página). |
| `drivers list` | `drivers.read` | Read | Sí | No | Ninguna | EXPERIMENTAL | Registros de conductores. |
| `tickets get` | `tickets.read` | Read | Sí | No | Ninguna | EXPERIMENTAL | Registros de paquetes de billetes. |
| `hof get` | `vehicle.hofs.read` | Read | Sí | No | Ninguna | STABLE_BETA | Requiere `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Sí | No | Ninguna | STABLE_BETA | Requiere `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Sí | No | Ninguna | STABLE_BETA | Requiere `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Sí | No | Ninguna | STABLE_BETA | Requiere `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Sí | No | Ninguna | STABLE_BETA | Requiere `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Sí | No | Ninguna | EXPERIMENTAL | Requiere `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Sí | No | Ninguna | EXPERIMENTAL | Requiere `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Sí | Sí (variable de script) | Ninguna, no se revierte | EXPERIMENTAL | Requiere `--handle=`, `--name=`, `--value=` (número finito). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Sí | No | Ninguna | EXPERIMENTAL | Requiere `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Sí | No | Ninguna | EXPERIMENTAL | Requiere `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Sí | Sí (estado de script en memoria) | Ninguna, no se revierte | EXPERIMENTAL | Requiere `--handle=`, `--name=`, `--value=`. El propio `SetStringVariable` de OMSI. Validación en runtime pendiente. |
| `scripts trigger` | `vehicle.trigger` | Action | Sí | Sí (ejecuta código del script del vehículo) | Ninguna, no se revierte | EXPERIMENTAL | Requiere `--handle=`, `--name=`, `--value=` (`1` pulsar, `0` soltar, `2` arrastrar). Validación en runtime pendiente. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Sí | Sí (reproduce sonidos del vehículo) | Ninguna, no se revierte | EXPERIMENTAL | Requiere `--handle=`, `--trigger=`; opcional `--file=`. Validación en runtime pendiente. |

<a id="operations-without-a-route"></a>
### Operaciones sin ruta

Estos ids de operación públicos (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) no tienen ruta jerárquica y se invocan con `/runtime:<operation>` más `--key=value` o `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` obligatorios; `levels` opcional), `d3d.texture.describe` (`handle`; `level` opcional), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` obligatorios; `level`, `x`, `y` opcionales), `d3d.texture.release` (`handle`). Las operaciones D3D son EXPERIMENTAL; el ciclo de vida de las texturas y la invalidación por reset del dispositivo están validados en runtime (cierre de runtime `H02`, `D01`; consulta [capacidades](capabilities.md)). `timetable.track-entries.list` y `timetable.tour-entries.list` son listas acotadas: un resultado que no cabe en el slot de runtime se acorta (`truncated=true`). `internal.road-vehicles.make-basic` es INTERNAL y tanto la CLI como la API lo rechazan con `OL_E_RUNTIME_OPERATION_UNKNOWN`.

## Flags

Todos los flags de `CliInput.KnownFlags`. La «fase» es *de lanzamiento* (da forma al `LaunchSpec`/plan de una sesión nueva), *de runtime* (actúa sobre una sesión en ejecución) o *de control* (cambia el comportamiento de la propia CLI). Los flags que solo se analizan por compatibilidad (`CliInput.AcceptedNoEffectFlags`) se indican en su línea.

<a id="control-and-output"></a>
### Control y salida

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/?` | `/?` | desactivado | control | STABLE_BETA | Imprime el texto de uso, salida `0`. |
| `/help` | `/help` | desactivado | control | STABLE_BETA | Igual que `/?`. (La palabra suelta `help` devuelve en cambio el catálogo estructurado). |
| `/version` | `/version` | desactivado | control | STABLE_BETA | Envelope `version`, salida `0`. Se evalúa antes que cualquier otro comando excepto `/silent`. |
| `/json` | `/json` o `--json` | desactivado | control | STABLE_BETA | Emite envelopes JSON; también fuerza la salida de consola incluso bajo `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | desactivado | control | STABLE_BETA | Suprime la salida de éxito legible para personas. La salida JSON (`/json`) y los errores se siguen escribiendo; el código de salida no cambia. |
| `/silent` | `/silent` (también `--silent`) | desactivado | control | EXPERIMENTAL | Delega toda la línea de comandos en `OmsiLaunchW.exe` y devuelve `0` en cuanto se ha iniciado el proceso host. El resultado de la sesión lo notifican `OmsiLaunchW.exe` (cuadros de mensaje, icono de la bandeja), `.omsilaunch\diagnostics` y el endpoint de control local. La delegación y los cuadros de diálogo de error están validados en runtime (cierre de runtime `T04`); consulta [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | control | REMOVED | Eliminado en 0.1.0-beta4: se rechaza con `OL_E_INVALID_ARGUMENT` (código de salida `2`). El propietario de la sesión siempre sirve el endpoint de control local. |
| `/verbose` | `/verbose` | desactivado | de lanzamiento | PARTIAL | `DiagnosticsSpec.Verbose`. Los valores se transportan en el spec; su efecto se limita a la traza del host en `.omsilaunch\diagnostics`. |
| `/log` | `/log` | activado (`DiagnosticsSpec.Log` es `true` de forma predeterminada) | de lanzamiento | PARTIAL | `DiagnosticsSpec.Log`. En la práctica, siempre activado. |
| `/logall` | `/logall` | desactivado | de lanzamiento | PARTIAL | Establece `Verbose`, `ProcessTrace`, `PluginTrace` y `NativeTrace` a la vez. |
| `/omsi-logall` | `/omsi-logall` | desactivado | de lanzamiento | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | desactivado | de lanzamiento | PARTIAL | Alias de `/trace-process`. |
| `/trace-process` | `/trace-process` | desactivado | de lanzamiento | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | desactivado | de lanzamiento | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: el plugin notifica cada operación de runtime (`plugin.trace.operation`: operación, resultado, código, duración) a la traza del host de la sesión. |
| `/trace-native` | `/trace-native` | desactivado | de lanzamiento | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: la capa nativa notifica cada instalación y retirada de hooks (`native.trace`) a la traza del host de la sesión. |

<a id="planning-validation-and-harnesses"></a>
### Planificación, validación y harnesses

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/plan` | `/plan` | desactivado | de lanzamiento | STABLE_BETA | Construye e imprime el `SessionPlan`, sin iniciar OMSI. Salida `0` cuando `IsRunnable`, `1` en caso contrario. Requiere una selección de lanzamiento (`/new`, `/saved`, `/spec` o un argumento de instalación); `/plan` solo, sin nada más, ejecuta `detect`. |
| `/validate` | `/validate` | desactivado | de lanzamiento | STABLE_BETA | Idéntico a `/plan` en esta compilación. |
| `/runtime-batch` | `/runtime-batch` | desactivado | de runtime (propietario) | INTERNAL | Harness de validación: tras `Running`, ejecuta el conjunto de operaciones de lectura y escribe `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | desactivado | de runtime (propietario) | INTERNAL | Harness de validación: lecturas más `time.set`, `camera.set` y `vehicle.variable.set` con restauración; escribe `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | desactivado | de runtime (propietario) | INTERNAL | Harness de validación del ciclo de vida de las texturas D3D; escribe `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | ninguno | de runtime | STABLE_BETA (despacho) | Selecciona una operación de runtime pública por su id. Modo cliente (sin argumento de instalación): se reenvía al propietario. Modo propietario: se ejecuta una vez tras `Running`. Ids desconocidos: `OL_E_RUNTIME_OPERATION_UNKNOWN`, salida `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (repetible) | ninguno | de runtime | STABLE_BETA (despacho) | Argumento de runtime; equivalente a `--key=value`. Si falta `=`: `/runtime-arg requires key=value`, salida `2`. |

<a id="world-selection"></a>
### Selección del mundo

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` es el modo predeterminado, pero solo se solicita un lanzamiento cuando está presente uno de `/new`, `/saved`, `/last`, `/spec` | de lanzamiento | STABLE_BETA | NEW_MAP. Requiere `/map` y `/entrypoint-index` (un plan sin índice del punto de entrada presentado notifica `OL_E_ENTRYPOINT_REQUIRED`; sin `/map` no se resuelve ningún mapa). `/new` nunca selecciona un mapa de forma implícita. |
| `/saved` | `/saved:<file.osn>` | ninguno | de lanzamiento | STABLE_BETA | SAVED_SITUATION. El mapa y la posición proceden del `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` se rechazan junto con `/saved` (salida `2`). Situación inexistente: `OL_E_SITUATION_NOT_FOUND`; su mapa inexistente: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | ninguno | de lanzamiento | EXPERIMENTAL | LAST_MAP_STATE: carga el `laststn.osn` propio del mapa (que OMSI escribe al cerrar el mapa) mediante la rama de último estado del formulario de inicio. Requiere `/map`; un mapa sin `laststn.osn` produce `OL_E_SITUATION_NOT_FOUND`. Nunca una heurística del `.osn` más reciente. Validación en runtime pendiente. |
| `/map` | `/map:<identity>` (por ejemplo, `maps\Grundorf\global.cfg`) | ninguno | de lanzamiento | STABLE_BETA | Identidad del mapa para `/new`, o el ámbito para `/list:Entrypoints`. Desconocido: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | ninguno | de lanzamiento | EXPERIMENTAL | Punto de entrada por su etiqueta exacta (con distinción de mayúsculas y minúsculas) o por una identidad de `/list:entrypoints`. OMSI presenta solo el primer registro de cada etiqueta; un registro posterior que la comparte se rechaza (`OL_E_ENTRYPOINT_NOT_PRESENTED`), y uno desconocido también (`OL_E_ENTRYPOINT_NOT_FOUND`). Validación en runtime pendiente. Mutuamente excluyente con `/entrypoint-index` (la identidad prevalece y borra el índice). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | ninguno | de lanzamiento | STABLE_BETA | Índice del punto de entrada en la lista presentada (empezando en 1, tal como lo presenta OMSI). Obligatorio para un plan NEW_MAP ejecutable. |

<a id="date-time-and-weather"></a>
### Fecha, hora y meteorología

Estos flags se aceptan y se transportan al `LaunchSpec`. La fecha, la hora y el año de inicio se aplican solo a las sesiones NEW_MAP: con `/saved` o `/last` el planificador los registra como `UNAVAILABLE` **y añade `OL_E_CAPABILITY_UNAVAILABLE`, por lo que el plan NO ES EJECUTABLE (salida `1`)**, porque una situación guardada conserva su propio reloj. La meteorología se aplica a todos los modos de mundo. Todos son `EXPERIMENTAL`: la ruta de inicio nativa está cerrada estáticamente y la validación en runtime está pendiente. Un archivo `/spec` o un perfil de sesión que los establezca tiene el mismo efecto.

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` o `/date:system` | sin establecer | de lanzamiento | EXPERIMENTAL | `DateSpec` explícito/sistema: la fecha de inicio de una sesión NEW_MAP (consulta [launchspec](launchspec.md#datespec-timespec-yearspec)). Valor no analizable: `OL_E_INVALID_ARGUMENT`, salida `2`; una fecha imposible: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` o `/time:system` | sin establecer | de lanzamiento | EXPERIMENTAL | `TimeSpec` explícito/sistema: la hora de inicio de una sesión NEW_MAP. |
| `/year` | `/year:<n>` o `/year:system` | sin establecer | de lanzamiento | EXPERIMENTAL | `YearSpec`: el año de inicio de una sesión NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | sin establecer | de lanzamiento | EXPERIMENTAL | `WeatherMode.Preset`: un archivo de la carpeta `weather` de la instalación, por nombre (`Regen`) o por ruta (`weather\Regen.owt`); uno inexistente produce `OL_E_WEATHER_INVALID` en el momento de la planificación. Se carga mediante el propio `loadWeather` de OMSI justo después de que arranque el mundo; el METAR en vivo permanece desactivado. Validación en runtime pendiente. |
| `/weather-icao` | `/weather-icao:<code>` | sin establecer | de lanzamiento | EXPERIMENTAL | `WeatherMode.Icao`: meteorología METAR en vivo para una estación ICAO de cuatro caracteres (`OL_E_WEATHER_INVALID` en caso contrario). OMSI guarda la estación en `options.cfg`, que la sesión restaura. Validación en runtime pendiente. |
| `/weather-real` | `/weather-real` | sin establecer | de lanzamiento | EXPERIMENTAL | `WeatherMode.RealCurrent`: meteorología METAR en vivo para la estación que OMSI ya tiene configurada; sin ella el inicio falla con `OL_E_WEATHER_APPLY_FAILED`. Prevalece el último de `/weather`, `/weather-icao`, `/weather-real`. Cualquier meteorología de inicio sustituye a la de una situación guardada. |

<a id="player-vehicle"></a>
### Vehículo del jugador

Se aceptan y se resuelven frente a la instalación, pero el runtime no los aplica: cada campo establecido es `UNAVAILABLE` y añade `OL_E_CAPABILITY_UNAVAILABLE` (plan NO EJECUTABLE, salida `1`).

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | sin establecer | de lanzamiento | UNAVAILABLE | Se resuelve primero (`OL_E_VEHICLE_NOT_FOUND` si es desconocido). |
| `/repaint` | `/repaint:<id>` | sin establecer | de lanzamiento | UNAVAILABLE | Solo se resuelve junto con `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | sin establecer | de lanzamiento | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` si es desconocido. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | ninguno | de lanzamiento | STABLE_BETA | Una operación de [gestión de HOF](hof-management.md) de la sesión: `HofSourceKind` y `Source`. Requiere al menos un `/hof-target`. Sustituye el `HofManagement` de un `/spec`; con un preset de perfil de sesión que tiene `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Mal formado: salida `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (repetible) | ninguno | de lanzamiento | STABLE_BETA | Un vehículo de destino bajo `Vehicles\`. Sin `/hof-source`: salida `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (repetible) | todos los archivos | de lanzamiento | STABLE_BETA | Restringe el origen a estos nombres de archivo. Sin `/hof-source`: salida `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | de lanzamiento | STABLE_BETA | `HofApplyMode`. Sin `/hof-source`: salida `2`. |
| `/module` | `/module:<id>` (repetible) | ninguno | de lanzamiento | EXPERIMENTAL | Solicita un [módulo de runtime](runtime-modules.md) para la sesión (`LaunchSpec.Modules.Enable`, que se suma a los módulos de un archivo `/spec`). Un módulo solicitado que no se puede cargar hace que el plan no sea ejecutable con su código `OL_E_MODULE_*` (salida `1`). Sus operaciones se alcanzan con `/runtime:module.<id>.<operation>` y `/runtime-arg`. Con un preset de perfil de sesión que establece `enabled: false` para él: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (repetible) | ninguno | de lanzamiento | EXPERIMENTAL | Mantiene un módulo fuera de la sesión, incluido un módulo cargado de forma predeterminada, por su mapa o por el archivo de configuración (`LaunchSpec.Modules.Disable`). Con un preset que establece `enabled: true` para él: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Sin valor: salida `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (repetible) | ninguno | de lanzamiento | EXPERIMENTAL | Establece un ajuste de módulo (`LaunchSpec.Modules.Settings`); prevalece sobre el archivo `/spec`, el archivo de configuración y el valor predeterminado. Una clave que el preset seleccionado también establece: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Que no tenga la forma `<id>.<key>=<value>`: salida `2`. Valor no válido o clave no declarada: diagnóstico del plan `OL_E_MODULE_SETTING_INVALID` (salida `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` solo significa `true` para un ajuste booleano | ninguno | de lanzamiento | EXPERIMENTAL | Un flag que un módulo instalado declara para uno de sus ajustes (`flag` en su manifiesto; se enumeran con `/list:RuntimeModules`). Se resuelve al planificar una sesión y equivale a `/module-setting:<id>.<key>=<value>`. Un flag que ningún módulo instalado declara, o uno usado fuera de un lanzamiento: `Unknown argument`, salida `2`. Un flag declarado por dos módulos: `OL_E_MODULE_SETTING_INVALID`, salida `2`. Un flag de módulo nunca sustituye a un flag integrado. |
| `/fleet` | `/fleet:<n>` | sin establecer | de lanzamiento | UNAVAILABLE | Número de flota. |
| `/registration` | `/registration:<text>` | sin establecer | de lanzamiento | UNAVAILABLE | Matrícula. |
| `/no-vehicle` | `/no-vehicle` | desactivado | de lanzamiento | STABLE_BETA | Quita cualquier vehículo del jugador de la semilla (`/spec` o perfil). Inocuo. |

<a id="configuration-overlays"></a>
### Overlays de configuración

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (repetible; las claves no distinguen mayúsculas de minúsculas) | ninguno | de lanzamiento | STABLE_BETA | Overlay semántico de `options.cfg` a partir de `ConfigurationCatalog` (por ejemplo, `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Clave desconocida: `OL_E_UNKNOWN_SETTING` (salida `2`); clave de solo lectura (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (salida `2`); valor fuera de rango o mal formado: `OL_E_INVALID_SETTING_VALUE` al construir el overlay. El overlay es una mutación de la sesión: se guarda en un snapshot, se aplica antes de iniciar OMSI y se restaura byte a byte en la detención (RV-005 `RUNTIME_VALIDATED`). Conflicto con una clave propiedad del preset de un perfil seleccionado: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Presentación del splash

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (sin distinguir mayúsculas de minúsculas) | `Managed` | de lanzamiento | STABLE_BETA | `Managed`: los BMP empaquetados de 640x480 y 24 bits se copian una vez a `<root>\.omsilaunch\assets\splash`, y `GUI\NewSplashscreen_ENG.bmp` más `GUI\NewSplashscreen_<lang>.bmp` se aplican como overlay de forma transaccional y se restauran exactamente (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (alias): los archivos de OMSI no se tocan. Si falta el valor: `/splash requires Unset, Native, or Managed`, salida `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (también `pt-BR`, `de`, `fr`, `en`; cualquier otro valor recurre a `ENG`) | `[language]` de `options.cfg`; si no, `ENG` | de lanzamiento | STABLE_BETA | Selecciona el archivo de destino localizado. |
| `/splash-assets` | `/splash-assets:<directory>` (las rutas relativas se resuelven bajo la raíz de la instalación) | `<root>\.omsilaunch\assets\splash`; si no, el conjunto empaquetado | de lanzamiento | STABLE_BETA | Directorio de recursos personalizado; debe contener `ENG.bmp` y, para un idioma distinto del inglés, `<lang>.bmp`. Errores: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (se presentan como `OL_E_SESSION_PRESENTATION_INVALID` en el plan; no ejecutable). |

<a id="internet-textures"></a>
### Texturas de Internet

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | de lanzamiento | EXPERIMENTAL | `Native`: sin cambios. `Disabled`: se suprime el descargador perfilado dentro del proceso. `Override`: el perfil `.itx` indicado se aplica como overlay en `Texture\standard.itx`; todos los destinos HTTP(S) enumerados en él más `Texture\standard.ipr` pasan a ser eliminaciones de sesión (se quitan durante la sesión y se restauran en la detención). Si falta el valor: salida `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | ninguno | de lanzamiento | EXPERIMENTAL | Obligatorio con `Override` (`OL_E_ITX_PROFILE_REQUIRED`, salida `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (deben ser pares de líneas URL/destino con URL `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (los destinos deben resolverse bajo `Texture\`, sin rutas absolutas, `..` ni puntos de reanálisis). |

<a id="session-profiles"></a>
### Perfiles de sesión

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | ninguno | de lanzamiento | STABLE_BETA (compilación; `OmsiLaunch.ProfileTests` offline) | Carga `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (consulta [perfiles de sesión](session-profiles.md)). Requiere `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, salida `2`). El bloque `new:` solo se aplica con `/new`; `compatibility.maps` se aplica para `/new` y `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Los flags explícitos que colisionan con un campo propiedad del perfil se rechazan con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): mapa/punto de entrada/fecha/hora/año/meteorología cuando el bloque `new:` es su propietario, claves de `/set` propiedad del preset, flags de splash cuando el preset tiene `presentation`, flags de texturas de Internet cuando tiene `internet-textures`, timeouts cuando tiene `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | ninguno | de lanzamiento | STABLE_BETA | Selecciona el preset por `index`. Fuera de rango: salida `2`. |

<a id="launchspec-file"></a>
### Archivo LaunchSpec

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | ninguno | de lanzamiento | STABLE_BETA (cargador probado offline; semántica de sesión idéntica a la de los flags) | Carga un archivo JSON `LaunchSpec` como semilla (consulta [LaunchSpec](launchspec.md)) y marca que se ha solicitado un lanzamiento. Reglas (`LaunchSpecJson`): el archivo debe existir (`OL_E_SPEC_NOT_FOUND`, salida `6`); como máximo 1 MiB (`OL_E_SPEC_TOO_LARGE`, salida `2`); la raíz debe ser un objeto (`OL_E_SPEC_INVALID`); los nombres de propiedad no distinguen mayúsculas de minúsculas; se admiten comentarios `//` y comas finales; profundidad máxima de 32; toda propiedad desconocida se rechaza con su ruta JSON (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, salida `2`). |

**Precedencia** (`CliInput.BuildSpecAsync`): valores predeterminados → archivo `/spec` → `/predefined-profile` (sustituye `Installation` y `World` y después aplica el perfil) → flags explícitos. Un argumento de instalación explícito prevalece sobre `RootPath` en el spec. `/no-vehicle` quita el vehículo del jugador del spec; `/vehicle` y los flags relacionados se combinan con él campo a campo. Las claves de `/set` se combinan en `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` solo sobrescriben cuando se indican. `/startup-timeout` y `/shutdown-timeout` solo sobrescriben cuando se indican; `Presentation.SuppressTrayIcon` procede únicamente del spec (no hay flag). Los flags de diagnóstico se combinan mediante OR con los `Diagnostics` del spec.

<a id="content-discovery"></a>
### Descubrimiento de contenido

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; las categorías son los valores de `ContentQueryKind` `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (sin distinguir mayúsculas de minúsculas) | ninguno | local, sin sesión | STABLE_BETA | `DiscoverAsync` sobre la instalación; envelope `content.list` con entradas `Identity`, `Kind`, `DisplayName`; salida `0`. `SessionProfiles` responde con `ListSessionProfilesAsync` (envelope `session-profiles.list`) y `RuntimeModules` con `ListRuntimeModulesAsync` (envelope `runtime-modules.list`: el id, la versión, la activación, las operaciones y la validez de cada módulo, y sus ajustes con tipo, valor predeterminado y flag). Categoría desconocida: `Unknown discovery category`, salida `2`. Los puntos de reanálisis (uniones/enlaces simbólicos) se omiten y los archivos de OMSI se leen como Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | ninguno | local | STABLE_BETA | Ámbito que se reenvía para todas las categorías excepto `Entrypoints`, que usa `/map` como ámbito. |

<a id="timeouts-and-observation"></a>
### Timeouts y observación

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` segundos | valor del spec/perfil; si no, `180` | de lanzamiento | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. El propietario espera `Running` durante este valor más 5 s; `OL_E_STARTUP_TIMEOUT` termina la sesión con salida `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` segundos | valor del spec/perfil; si no, `30` | de lanzamiento | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: con `/cooperative-shutdown`, cuánto espera el supervisor a que OMSI se cierre antes de terminarlo; sin él, OMSI se termina de inmediato y el valor no tiene efecto. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | desactivado | de lanzamiento | EXPERIMENTAL | `Behavior.CooperativeShutdown`: al finalizar una sesión en ejecución se le pide a OMSI que se cierre mediante su propia rutina de salida (la que se ejecuta cuando el usuario confirma que quiere salir: guarda `options.cfg` y el `laststn.osn` del mapa, y después se cierra) y se esperan `ShutdownTimeoutSeconds` antes de terminarlo. Opcional hasta que se valide en runtime; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` cuando hubo que terminar OMSI. Los archivos de la sesión se restauran tras la salida en cualquier caso. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | ninguno (se ejecuta hasta que OMSI termine o se solicite una detención) | de runtime (propietario) | STABLE_BETA | Límite superior de la fase de ejecución: tras `n` segundos en `Running` se solicita la detención canónica. Una detención desde la bandeja o la canalización, o la finalización de OMSI, la terminan antes. `0` detiene inmediatamente después de `Running`. |

<a id="recovery"></a>
### Recuperación

| Flag | Sintaxis y valores | Predeterminado | Fase | Estabilidad | Comportamiento |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | desactivado | local | STABLE_BETA | Informa de si `<root>\.omsilaunch\journal.json` está pendiente (`pending`); nunca restaura; salida `0`. Toma el lease de la instalación: `OL_E_INSTALLATION_BUSY` (salida `7`) mientras un propietario lo mantiene. |
| `/recover` | `/recover` | desactivado | local | STABLE_BETA | Restaura un diario pendiente (las copias de seguridad se verifican primero frente al SHA-256 del snapshot; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` se notifican en `diagnostics`). Salida `0` cuando no había nada pendiente o la restauración se completó; `8` cuando había un diario pendiente y sigue estándolo. Se rechaza con `OL_E_INSTALLATION_BUSY` mientras el proceso de OMSI registrado en el diario (PID, hora de creación, ruta del exe) o, en el caso de un diario posterior a `HandoffCreated` sin PID, cualquier `Omsi.exe` de esa raíz sigue vivo. Cada inicio de sesión realiza automáticamente la misma recuperación antes de leer la instalación. |
| `/clear-module-data` | `/clear-module-data:<module id>` | ninguno | local, sin sesión | EXPERIMENTAL | Elimina el directorio de datos del módulo `<root>\.omsilaunch\module-data\<id>` y nada más; envelope `module-data.clear` con `module` y `cleared` (`false` cuando no había nada que eliminar); salida `0`. Toma el lease de la instalación: `OL_E_INSTALLATION_BUSY` (salida `7`) mientras una sesión es propietaria de la instalación. Una carpeta de datos de módulo alcanzada mediante un enlace se rechaza (`OL_E_MODULE_INTEGRITY`); un enlace dentro de ella se quita como enlace. |

<a id="output-formats"></a>
## Formatos de salida

- **Envelope de éxito** (`CliInput.WriteEnvelope`, con `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, con sangría. Las respuestas reenviadas de `session status` y `events read` añaden un miembro `metadata` cuando se omitieron eventos más antiguos para que cupieran en la trama de control (`events_dropped_count`, consulta [control local](local-control.md)). Sin `--json` solo se imprime `<object>` como JSON con sangría, seguido de `Note: <n> older events were omitted to fit the control frame.` cuando se descartaron eventos.
- **Envelope de error** (`CliInput.WriteError`, con `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Sin `--json`: `OL_E_<CODE>: message` en una línea. Categorías: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Bajo `OmsiLaunchW.exe`, el mismo código y el mismo mensaje se muestran en un cuadro de mensaje.
- **Plan y estado** (`CliInput.Write`): los registros `SessionPlan`, `SessionStatus` y `RuntimeCommandResult` se imprimen como JSON con sangría **sin** envelope. Sin `--json`, un plan se resume como `Plan: READY profile=Omsi23004_692EBFBF` o `Plan: NOT RUNNABLE profile=...`; los demás registros se siguen imprimiendo como JSON. Los valores de enumeración se serializan como enteros (`SessionState.Running` es `14`, `Completed` es `18`, `Failed` es `19`).
- Nombres de comando usados en los envelopes: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile` y el id de la operación de runtime para los comandos de runtime reenviados.
- Bajo `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) no se escribe nada en la consola salvo que se indique `--json`.

<a id="errors-per-command"></a>
## Errores por comando

| Comando | Códigos de error habituales | Salida |
|---|---|---|
| Cualquier fallo de análisis | `OL_E_INVALID_ARGUMENT`, códigos del perfil de sesión (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Ruta de cliente, `/runtime` (cliente) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` devueltos por el propietario, p. ej. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Comprobación previa del propietario | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); pendiente pero no recuperado (`8`) | `7`, `8` |
| `/list` | categoría desconocida (`2`); `OL_E_INSTALLATION_NOT_FOUND`/directorios inexistentes (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, lanzamiento | diagnósticos del plan: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (conjunto de archivos del plugin instalado), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informativo) | `1` |
| Inicio de sesión | `OL_E_PLAN_NOT_RUNNABLE` (nueva planificación al iniciar, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normalmente notificados por la planificación como diagnóstico del plan, salida `1`; `7` solo si los archivos del plugin cambian entre la planificación y el inicio), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (sesión `Failed`, `1`) | `1`, `7` |
| Excepción no controlada en cualquier punto | clasificada por `CliProgram.Classify` (consulta [códigos de salida](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Entorno

| Variable | Establecida por | Efecto |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: salida de consola suprimida, fallos como cuadros de mensaje, `/silent` no se vuelve a delegar. |

<a id="see-also"></a>
## Consulta también

[Ejemplos de la CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [códigos de salida](exit-codes.md) · [errores](errors.md) · [control local](local-control.md) · [bandeja de Windows](windows-tray.md) · [control de runtime](runtime-control.md) · [capacidades](capabilities.md) · [LaunchSpec](launchspec.md) · [perfiles de sesión](session-profiles.md) · [empaquetado](packaging.md) · [compatibilidad](compatibility.md) · [limitaciones conocidas](known-limitations.md) · [API pública](public-api.md)
