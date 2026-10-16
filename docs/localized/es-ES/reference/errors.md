# Códigos de error y de diagnóstico

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> Traducción de la [página original en inglés](../../../reference/errors.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página es la referencia normativa de todos los códigos de `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 códigos de error `OL_E_` y 4 advertencias `OL_W_`, agrupados por categoría del catálogo, más los códigos de diagnóstico informativos que no son errores. Para cada código indica dónde lo genera el código actual, qué significa, cómo te llega (excepción lanzada, campo de resultado, diagnóstico, respuesta del plano de control o envelope de la CLI) y qué hacer. Los significados se han tomado de los puntos donde se generan; cuando un código está definido pero no tiene ninguna ruta actual que lo genere, la página lo indica.

Páginas relacionadas: [API pública](public-api.md), [códigos de salida](exit-codes.md), [LaunchSpec](launchspec.md), [ciclo de vida de la sesión](../concepts/session-lifecycle.md), [transacciones y recuperación](../concepts/transactions-and-recovery.md), [plano de control local](local-control.md), [control de runtime](runtime-control.md), [perfiles de sesión](session-profiles.md), [plugin permanente](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Cómo te llegan los códigos

| Superficie | Significado |
| --- | --- |
| Lanzado | Una excepción cuyo `Message` empieza por el código (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). La CLI extrae el código del mensaje y lo asigna a un código de salida (`CliProgram.Classify`). |
| Diagnóstico del plan | Un `LaunchDiagnostic` en `SessionPlan.Diagnostics`; cualquier código `OL_E_` hace que `IsRunnable` sea false (CLI: plan `NOT RUNNABLE`, salida 1). |
| Diagnóstico de la sesión | Un `LaunchDiagnostic` en `SessionStatus.Diagnostics`; el estado de la sesión es `Failed` (salida 1 de la CLI). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` y `OL_E_RESTORE_FAILED` envuelven un código interno en su mensaje. |
| Resultado de runtime | `RuntimeCommandResult.ErrorCode` con `Succeeded = false`. |
| Detalle de runtime | `RuntimeCommandResult.ErrorCode` es el código específico y `Values["detail"]` el mensaje (con `Values["exception"]`, el tipo de .NET). Beta 4: un código tipado del lado del plugin llega sin cambios a la API y a la CLI; solo un fallo sin un código catalogado se convierte en `OL_E_RUNTIME_OPERATION_FAILED`. |
| Respuesta de control | `ErrorCode` de una respuesta del plano de control local (`LocalControlResponse`). |
| Envelope de la CLI | `error.code` en el envelope `--json`, o `<code>: <message>` en la consola; se indica el código de salida. |
| Telemetría | Un nombre de evento de runtime que el host asigna a un diagnóstico de la sesión. |

## Cli

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Se escapó una `OperationCanceledException` (Ctrl+C o una espera del cliente cancelada). | Envelope de la CLI, salida 7 | Vuelve a ejecutar el comando. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Se escapó una excepción sin código `OL_E_`: JSON de `/spec` mal formado, un miembro obligatorio de la spec nulo, un fallo inesperado. | Envelope de la CLI, salida 10 | Lee el mensaje y `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; corrige la entrada; notifícalo si no tiene explicación. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Se escapó una `TimeoutException` sin código; o el cliente de control local estaba conectado a un propietario que no respondió dentro del timeout (el propietario existe, por lo que no se notifica como `OL_E_NO_ACTIVE_SESSION`). (Los timeouts del buzón llevan `OL_E_RUNTIME_REQUEST_TIMEOUT` en su lugar). | Envelope de la CLI, respuesta de control; salida 5 desde `Classify`, salida 7 para una respuesta de control | Vuelve a intentarlo; comprueba que OMSI y el propietario responden. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` no está junto a `OmsiLaunch.exe`. | Envelope de la CLI, salida 7 | Vuelve a instalar el paquete. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` de `OmsiLaunchW.exe` no devolvió ningún proceso. | Envelope de la CLI, salida 7 | Comprueba los archivos del paquete y los permisos; ejecuta sin `/silent` para ver el fallo. |

<a id="compatibility"></a>
## Compatibilidad

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` en `plugin.build.invalid` | Las capacidades de inicio se resolvieron, pero el formulario de inicio activo o uno de sus puntos de hook no está en el estado esperado cuando el plugin arma el inicio headless (por ejemplo, otro plugin ya lo parcheó). | Diagnóstico de la sesión (`Failed`) | Quita el plugin que entra en conflicto; consulta [compatibilidad](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` al iniciar; operaciones de runtime) | Una capacidad nativa que necesita la solicitud no se pudo resolver ni verificar en este build de OMSI, por lo que no está disponible. Al iniciar, la sesión falla antes de cualquier hook y los archivos se restauran; una operación de runtime se rechaza sin tocar la memoria de OMSI. `Data["capability"]` (runtime) o el valor `capabilities` de la telemetría la nombra. | Diagnóstico de la sesión (`Failed`); resultado de runtime | Consulta [compatibilidad](compatibility.md): la capacidad no está disponible en este build. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` no disponible) | La instalación no es una instalación de OMSI 2: falta `Omsi.exe`, no es una imagen PE x86 de 32 bits, no tiene una versión de OMSI 2 (2.x), o la raíz no tiene el directorio `maps\` o `Vehicles\`. El hash del ejecutable no es un criterio. | Diagnóstico del plan | Apunta OmsiLaunch a la raíz de una instalación de OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` no disponible); también lo lanza `CurrentWindowsX64Platform.ValidateCurrent`, al que el servicio no llama | No es Windows 10 o posterior, o el sistema operativo o el proceso host no es x64. | Diagnóstico del plan (salida 3 de la CLI cuando se lanza) | Ejecuta en Windows 10 o posterior de 64 bits. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Solo `CurrentWindowsX64Platform.ValidateCurrent` | La arquitectura del sistema operativo o del host no es x64. El servicio no llama a `ValidateCurrent`; no hay ninguna ruta actual que lo genere. | Lanzado (`PlatformNotSupportedException`) solo por ese método | Consulta el código fuente `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

<a id="content"></a>
## Contenido

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validación: NEW_MAP sin `EntrypointIdentity` y con `PresentedEntrypointIndex` sin establecer o negativo. Planificador: el mapa no tiene ningún punto de entrada con la identidad o la etiqueta exacta (con distinción de mayúsculas y minúsculas) solicitada. | Diagnóstico del plan | Establece `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) o una identidad de `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | La meteorología de inicio no se puede aplicar: un preset que no es un archivo de la carpeta `weather` de la instalación, o un ICAO que no son cuatro letras o dígitos. | Diagnóstico del plan | Usa un preset instalado o un código de estación válido. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | El punto de entrada solicitado comparte etiqueta con un registro anterior del mapa; OMSI presenta solo el primer registro de cada etiqueta, por lo que no se puede seleccionar al iniciar. | Diagnóstico del plan | Elige el primer registro con esa etiqueta, u otro punto de entrada. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` no disponible) | El mapa de NEW_MAP se ha resuelto, pero no hay índice presentado ni identidad. Siempre acompaña a `OL_E_ENTRYPOINT_NOT_FOUND`. | Diagnóstico del plan | Lo mismo. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` no es un `Vehicles\...\*.hof` instalado. | Diagnóstico del plan | Usa una identidad de `/list:hofs`. (De todos modos, los campos del vehículo del jugador no son ejecutables en este build). |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Un origen de [gestión de HOF](hof-management.md) (colección, directorio, vehículo o archivo) no existe, no tiene ningún `.hof`, o le falta un archivo nombrado en `Files`. | Diagnóstico del plan; fallo de inicio | Corrige el origen o la lista de archivos. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Un archivo o carpeta de vehículo de destino de HOF no está instalado. | Diagnóstico del plan; fallo de inicio | Usa una identidad de vehículo de `/list:vehicles` o una carpeta existente bajo `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Una ruta de HOF sale de la instalación, un destino no está bajo `Vehicles\`, el nombre de una colección no es un nombre simple, o se atraviesa una unión/enlace simbólico. | Diagnóstico del plan; fallo de inicio | Usa rutas relativas a la instalación sin `..` y sin enlaces. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Dos operaciones de HOF escriben bytes distintos en un mismo archivo, o una réplica elimina un archivo que escribe otra operación. | Diagnóstico del plan; fallo de inicio | Reordena, divide o quita las operaciones en conflicto. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Operación de HOF sin destinos u origen, un origen de archivo que no es `.hof`, un archivo de destino que no es `.bus`/`.ovh`, o un límite de tamaño/cantidad (8 MiB por archivo, 256 archivos por operación, 256 MiB por sesión). | Diagnóstico del plan; fallo de inicio | Corrige la operación. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validación: NEW_MAP con `MapIdentity` sin establecer o que no tiene la forma `maps\<dir>\global.cfg`. Planificador: el mapa no está instalado. | Diagnóstico del plan | Usa una identidad de `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Se escapó una `FileNotFoundException`/`DirectoryNotFoundException` sin código, por ejemplo `/list:repaints` con un `/vehicle-scope` desconocido, o `/list:entrypoints` con un `/map` desconocido. | Envelope de la CLI, salida 6 | Corrige la identidad. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` no es un elemento `.cti` del modelo (solo se comprueba cuando `Model` está establecido). | Diagnóstico del plan | Usa una identidad de `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | El mapa referenciado dentro del `.osn` seleccionado no está instalado. | Diagnóstico del plan | Instala el mapa o elige otra situación. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION sin `SituationIdentity`, o el `.osn` no está instalado. | Diagnóstico del plan | Usa una identidad de `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` no es un `Vehicles\...\*.bus` instalado. | Diagnóstico del plan | Usa una identidad de `/list:vehicles`. |

<a id="installation"></a>
## Instalación

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | El lease de la instalación (`Local\OmsiLaunch.Installation.<hash>`) lo tiene otro propietario en esta sesión de inicio de sesión de Windows, o sigue vivo un proceso de OMSI registrado en el diario (PID + hora de creación + ruta del ejecutable; o cualquier `Omsi.exe` de la raíz para un diario posterior a `HandoffCreated` sin PID). | Inicio: diagnóstico de la sesión a través de `OL_E_START_SESSION`. Recuperación: lanzado (`InvalidOperationException` / `IOException`). Salida 7 de la CLI. | Detén el otro propietario (`session stop`) o espera a que OMSI termine; después vuelve a intentarlo o usa `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` está vacío. | Diagnóstico del plan | Pasa el directorio de instalación. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` no disponible); también `ValidateCurrent` | La raíz no existe, tiene el atributo de solo lectura o no tiene directorio `plugins\`. | Diagnóstico del plan | Indica una instalación de OMSI real y con permiso de escritura. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (plan e inicio) | Un archivo `plugins\OmsiLaunch.*` instalado difiere del hash de `release-manifest.json` (o del conjunto de archivos de referencia si no hay manifiesto). | Diagnóstico del plan (plan no ejecutable, salida 1 de la CLI); diagnóstico de la sesión a través de `OL_E_START_SESSION` solo si los archivos cambian entre la planificación y el inicio | Vuelve a instalar el paquete de OmsiLaunch para que `plugins\` y el manifiesto coincidan. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (plan e inicio) | El manifiesto no tiene entrada para un archivo obligatorio del plugin. | Diagnóstico del plan; diagnóstico de la sesión a través de `OL_E_START_SESSION` en la misma carrera que arriba | Vuelve a instalar el paquete. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (plan e inicio) | Falta un archivo obligatorio `plugins\OmsiLaunch.*` en la instalación de OMSI, o un archivo de `plugins/` que figura en `release-manifest.json` no está instalado. | Diagnóstico del plan; diagnóstico de la sesión a través de `OL_E_START_SESSION` en la misma carrera que arriba | Instala el conjunto de archivos del plugin permanente ([instalación](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Solo `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` es false. El servicio no lo llama; no hay ninguna ruta actual que lo genere. | Lanzado solo por ese método | Consulta el código fuente. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` está vacío, no es JSON, no tiene la matriz `files`, o tiene una entrada sin `path`/`sha256`, un hash que no son 64 dígitos hexadecimales, una ruta absoluta, que contiene `:`, un segmento vacío, `.` o `..`, o una ruta que aparece dos veces (comparando sin distinguir mayúsculas y minúsculas, con `/` y `\` equivalentes). Se acepta un BOM UTF-8. | Plan: envuelto en `OL_E_RUNTIME_ARTIFACT_MISSING`; inicio: a través de `OL_E_START_SESSION` | Vuelve a instalar el paquete. |

## InvalidArgument

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validación: `Date.Value`/`Time.Value` establecido mientras el modo no es `Explicit`. CLI: flag desconocido, valor ausente, entero o rango incorrecto, `/saved` combinado con `/map`/`/entrypoint`, ruta de comando desconocida, cualquier `ArgumentException`/`FormatException` sin código. | Diagnóstico del plan; envelope de la CLI, salida 2 | Corrige el argumento. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (inicio) | Un valor de ajuste semántico está fuera de rango, no es booleano, no pertenece al conjunto permitido o está mal formado (`graphics.particles` necesita cuatro campos). Los valores no se validan durante la planificación. | Diagnóstico de la sesión a través de `OL_E_START_SESSION` | Usa un valor de la [tabla de ajustes](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | La clave existe pero no admite escritura (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Diagnóstico del plan; salida 2 de la CLI | Quita la clave. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | La clave no está en `ConfigurationCatalog`. | Diagnóstico del plan; salida 2 de la CLI | Usa una clave del catálogo. |

## LaunchSpec

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | La raíz no es un objeto JSON, o la deserialización no produjo ningún registro. | Lanzado (`InvalidDataException`), salida 2 de la CLI | Corrige el archivo ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | El archivo de `/spec` no existe. | Lanzado (`FileNotFoundException`), salida 6 de la CLI | Comprueba la ruta. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | El archivo supera 1 MiB. | Lanzado (`InvalidDataException`), salida 2 de la CLI | Reduce el archivo. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Un miembro que no es una propiedad pública del registro en esa posición; mensaje `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Lanzado (`InvalidDataException`), salida 2 de la CLI | Quita o renombra el miembro. |

## LocalControl

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Manejador del propietario (`OwnerSession`) | El comando no es `session.status`, `session.events`, `session.stop` ni `runtime.execute` con un argumento `operation`. | Respuesta de control; salida 7 de la CLI | Usa un comando compatible. |
| `OL_E_CONTROL_FAILED` | Cliente de la CLI (`ReportForwarded`, `CliEventWatch`) | El propietario respondió `Ok = false` sin código de error. | Envelope de la CLI, salida 7 | Lee el mensaje; revisa la consola/los diagnósticos del propietario. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | El manejador del propietario lanzó una excepción cuyo mensaje no lleva ningún código `OL_E_` (por ejemplo, la sesión ya estaba cerrada), o la respuesta del manejador no se pudo serializar. | Respuesta de control | Consulta `session status`; reinicia el propietario si ya no existe. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (ambos extremos) | Prefijo de longitud negativo o superior a 64 KiB (incluida una trama de solicitud demasiado grande), trama vacía, JSON `null`, una solicitud sin `Command`, o JSON que no se pudo descodificar. | Respuesta de control / envelope de la CLI | Usa el protocolo documentado ([plano de control local](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (cliente) | La propia solicitud serializada del cliente supera 64 KiB. Se notifica al llamador; no se envía nada. | Respuesta de control / envelope de la CLI | Reduce la solicitud. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (propietario) | La respuesta del propietario no cabe en la trama de 64 KiB. El propietario responde con este error tipado en lugar de descartar la respuesta. `session.status` y `session.events` nunca llegan a él: su historial de eventos se recorta empezando por lo más antiguo para que quepa. | Respuesta de control / envelope de la CLI | Vuelve a intentarlo; para los eventos, léelos con más frecuencia. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | El `ProtocolVersion` de la solicitud no es `0.1`; la respuesta del propietario no se pudo descodificar o estaba vacía; el propietario cerró la conexión sin responder o la conexión se rompió después de establecerse; el propietario no notificó ningún `SessionId`. | Respuesta de control / envelope de la CLI | Haz coincidir las versiones del cliente y del propietario; consulta `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Manejador del propietario | `session.stop` o `runtime.execute` sin un `session_id` igual al de la sesión activa. | Respuesta de control; salida 7 de la CLI | Consulta primero `session.status` y vincula la solicitud (la CLI lo hace automáticamente). |

<a id="other"></a>
## Otros

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | El plan recibido tiene `IsRunnable = false`, o la nueva planificación al iniciar no es ejecutable (`Omsi.exe` ha cambiado, se ha eliminado contenido, falta el conjunto de archivos del plugin); el mensaje enumera los códigos `OL_E_` actuales. | Lanzado (`InvalidOperationException`); salida 1 de la CLI | Vuelve a planificar y corrige los diagnósticos enumerados. |

<a id="presentation"></a>
## Presentación

Todos los genera `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Durante la planificación se envuelven en `OL_E_SESSION_PRESENTATION_INVALID` (el mensaje lleva el código); al iniciar se exponen a través de `OL_E_START_SESSION`.

| Código | Significado / causa típica | Qué hacer |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | El archivo `.itx` está vacío, tiene un número impar de líneas no vacías o una línea de URL no es una URL `http`/`https` absoluta. | Usa pares de líneas URL/destino. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (resuelto respecto al directorio de trabajo del proceso) no existe. Se lanza como `FileNotFoundException`. | Pasa una ruta `.itx` existente. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` es `Override` sin `OverrideProfilePath`. Salida 2 de la CLI cuando se lanza. | Proporciona `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Una línea de destino es absoluta, contiene `..`, empieza por `\`, se resuelve fuera de la instalación, carece de un componente `Texture\` o atraviesa una unión (junction) o un enlace simbólico. | Usa destinos relativos `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` no existe. | Corrige el directorio. |
| `OL_E_SPLASH_ASSET_MISSING` | Falta `ENG.bmp` o `<LANG>.bmp` en el directorio de recursos, o falta un `assets\splash\<LANG>.bmp` del paquete al inicializar `.omsilaunch\assets\splash`. | Proporciona los BMP / vuelve a instalar el paquete. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Un BMP del splash no es un mapa de bits `BM` de 640×480 y 24 bits. | Convierte la imagen. |

<a id="process"></a>
## Proceso

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (rutas de fallo de inicio y de fallo del supervisor) | Terminar OMSI o esperar a que termine durante la limpieza tras un error lanzó una excepción; a continuación sigue el mensaje interno. | Diagnóstico de la sesión (añadido a una sesión `Failed`) | Asegúrate de que no queda ningún `Omsi.exe` y, después, usa `/recover` si hay un diario pendiente. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` falló justo después de `CreateProcessW` (`Win32=<code>`); el proceso se termina. | Diagnóstico de la sesión a través de `OL_E_START_SESSION` | Vuelve a intentarlo; revisa el antivirus/los permisos. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI terminó antes de `gameplay.entered` (bloqueo, se cerró un cuadro de diálogo de error de OMSI, se cerró la ventana). | Diagnóstico de la sesión (`Failed`); se ejecuta la restauración | Revisa los registros propios de OMSI y `logfile.txt`; consulta `RuntimeEvents` para ver el último evento del plugin. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` falló (`Win32=<code>` en el mensaje). | Diagnóstico de la sesión a través de `OL_E_START_SESSION` | Resuelve el error Win32 (archivo ausente, acceso denegado, directiva). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | El bucle del supervisor lanzó una excepción (lectura de telemetría, espera/terminación del proceso, escritura del diario); OMSI se termina y se intenta la restauración. | Diagnóstico de la sesión (`Failed`) | Lee el mensaje interno y el registro del host. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` falló (`Win32=<code>`). | Dentro de los mensajes de `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Termina OMSI manualmente y, después, usa `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` sobre el handle del proceso falló. | Dentro de los mensajes de `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Lo mismo. |

## Runtime

«Detalle de runtime» significa el código específico en `ErrorCode`, con el mensaje en `Values["detail"]` y el tipo de excepción en `Values["exception"]`.

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` con `preset` mientras `family` es 2 (externa) o 3 (mapa); los presets solo existen para conductor (0) y pasajero (1). | Detalle de runtime | Omite `preset` o usa la familia 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Modo `Explicit` de `Date`/`Time` sin valor o con componentes fuera de rango. El nombre es histórico; es un error de validación durante la planificación. | Diagnóstico del plan | Corrige el valor (y ten en cuenta que la fecha/hora explícita no es ejecutable en este build). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: la ruta del `.bus` no existe bajo el directorio de trabajo de OMSI (se comprueba antes de la llamada nativa para que OMSI no pueda sustituirlo por uno alternativo). | Detalle de runtime | Usa una identidad de `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | ídem | El MakeVehicle nativo cambió la colección de vehículos de carretera en más de un objeto. | Detalle de runtime (`native_status`, recuentos en el mensaje) | Notifícalo; los objetos creados permanecen hasta que termina la sesión. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | ídem | La colección no cambió; OMSI rechazó el vehículo sin avisar. | Detalle de runtime | Revisa el archivo `.bus`; prueba otro modelo. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | ídem | Cualquier otro estado nativo distinto de cero. | Detalle de runtime | Notifícalo con los recuentos del mensaje. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | La llamada perfilada a PlaceRandomBus devolvió un estado de fallo. | Detalle de runtime | Vuelve a intentarlo cuando la fase de juego sea estable; notifícalo. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; comprobaciones del lado del plugin (`time.set` sin `hour`/`minute`/`second`; `camera.set` sin `family`/`field_of_view`; `camera.lock` sin un `family` analizable; operaciones de vehículos/curvas) | Falta un argumento obligatorio o está en blanco. | Resultado de runtime (registro; salida 2 de la CLI) o detalle de runtime (plugin) | Proporciona el argumento ([control de runtime](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | No se puede cargar el directorio/archivo de referencia del conjunto de archivos del plugin o el puente nativo indicado en `OmsiLaunchRuntimePaths` (puede envolver `OL_E_RELEASE_MANIFEST_INVALID`). | Diagnóstico del plan | Ejecuta desde un paquete íntegro. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, arnés INTERNAL) | Falló la lectura de referencia `time.read`/`camera.read`, por lo que se omitió la prueba de escritura. | Solo artefacto del lote | No está dirigido al usuario. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` está vacío, tiene más de 240 caracteres, contiene NUL o `..`, no empieza por `Vehicles\` o no termina en `.bus`. | Detalle de runtime | Pasa `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | ninguno (se conserva por compatibilidad) | Ya no se emite. Los builds anteriores lo generaban cuando una solicitud cancelada permanecía en el slot; ahora todas las rutas finales de una solicitud restablecen el slot, y una solicitud o respuesta residual encontrada al comienzo de una nueva solicitud se elimina. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | El buzón se ha desechado porque la sesión está terminando. | Lanzado (`InvalidOperationException`) | Ninguna acción; la sesión ha terminado. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | El slot del buzón contenía un valor de estado que no es inactivo, solicitado ni respondido (corrupción). El slot se restablece y se genera el error; la siguiente solicitud funciona con normalidad. | Lanzado (`InvalidDataException`) | Vuelve a intentarlo; notifícalo si persiste. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | El puntero al bloque de constantes del vehículo es nulo. | Detalle de runtime | El vehículo no tiene constantes; no hay nada que hacer. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` no está en la tabla de constantes del vehículo. | Detalle de runtime | Enumera primero las constantes. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | El objeto creado por la generación tiene una VMT fuera del rango de la imagen de OMSI. | Detalle de runtime | Notifícalo. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | ídem | El objeto creado no está en la colección de vehículos de carretera. | Detalle de runtime | Notifícalo. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Dos puntos consecutivos de la curva comparten la misma X. | Detalle de runtime | Problema de contenido en la curva del vehículo. |
| `OL_E_RUNTIME_CURVE_EMPTY` | ídem | La curva no tiene puntos. | Detalle de runtime | Lo mismo. |
| `OL_E_RUNTIME_CURVE_INVALID` | ídem | Ningún segmento de la curva contiene `x`. | Detalle de runtime | Evalúa dentro del dominio de la curva. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | ídem | `name` es desconocido o su puntero de función es nulo. | Detalle de runtime | Enumera primero las curvas. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | El puntero a la definición del vehículo es nulo. | Detalle de runtime | El handle hace referencia a un vehículo sin datos de definición. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (modo propietario) | Falta `plugins\OmsiLaunch.Plugin.opl` o `plugins\OmsiLaunch.Native.x86.dll` junto al ejecutable. | Envelope de la CLI, salida 7 | Vuelve a instalar el paquete. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | Falta `handle` o está en blanco para `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (normalmente el registro los rechaza antes con `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Detalle de runtime | Proporciona el handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | El handle es desconocido, el objeto ha salido de la colección, la generación de direcciones ha avanzado o la huella del objeto (VMT + identidad de la definición/el modelo) ha cambiado porque la dirección se ha reutilizado. Punto ciego residual: la misma clase y el mismo modelo recreados en la misma dirección entre dos lecturas de la lista. | Detalle de runtime | Vuelve a ejecutar `road-vehicles.list`/`humans.list` y usa el nuevo handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; alternativa de `D3DRuntimeApi` | Envoltorio genérico de fallos del lado del plugin; `Values["detail"]` contiene el mensaje (a menudo un código más específico) y `Values["exception"]` el tipo de excepción. Una excepción que se escapa de una operación dentro del despachador del buzón también se responde con este código (sin valores) en lugar de dejar la solicitud sin respuesta. | Resultado de runtime | Lee `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | El plugin no tiene implementación para una operación que el registro permitió (desfase de versiones entre el registro y el plugin). | Detalle de runtime | Vuelve a instalar un paquete coherente. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | La operación no está en `PublicRuntimeOperationIds`, lo que incluye todas las operaciones `internal.*`. Se comprueba antes de buscar la sesión. | Resultado de runtime; respuesta de control; salida 2 de la CLI | Usa un id de operación público. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` con `preset` cuando no hay vehículo del jugador (las sesiones headless no tienen ninguno). También se notifica como `code` del evento `camera.lock.degraded` cuando falla la reaplicación. | Detalle de runtime / evento de runtime | Bloquea sin preset, o usa una sesión con vehículo del jugador. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Un resultado D3D correcto no tenía valores, o tenía una cadena de estado del dispositivo desconocida. | Lanzado (`OmsiRuntimeException`) | Haz coincidir las versiones del host y del plugin. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | El slot contiene una respuesta obsoleta con el mismo id de solicitud que la nueva solicitud. La respuesta obsoleta se elimina antes de generar el error. | Lanzado (`InvalidOperationException`) | Usa ids de solicitud estrictamente crecientes. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | No hay respuesta dentro de `timeout`; el slot se restablece y una respuesta tardía se descarta. | Lanzado (`TimeoutException`); salida 5 de la CLI | Vuelve a intentarlo con un timeout más largo; comprueba que OMSI no está bloqueado (cuadro de diálogo modal, carga). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | El envelope de la respuesta está corrupto, tiene una longitud incorrecta (negativa, cero o mayor que el slot), un id de sesión ajeno o un id de solicitud distinto. El slot se restablece antes de generar el error, de modo que la siguiente solicitud funciona con normalidad. | Lanzado (`InvalidDataException`) | Vuelve a intentarlo; notifícalo si persiste. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | El `cursor` de una operación de lista tiene un formato incorrecto o pertenece a otro tipo de lista. | Detalle de runtime | Pasa el `next_cursor` de la página anterior de la misma operación, u omítelo. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | El resultado serializado supera el buzón de 64 KiB. Los resultados de listas paginadas se acortan para que quepan y continúan con `next_cursor`; ningún resultado de lista llega a este código. | Resultado de runtime | Usa una operación más específica o un `limit` menor. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | El puntero a la definición o al estado del script del vehículo es nulo. | Detalle de runtime | El vehículo no tiene objetos de script. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; buzón del plugin | `RuntimeCommand.SessionId` difiere del id de sesión del handle (lo lanza el host), o una solicitud llegó a un plugin vinculado a otra sesión (el plugin lo devuelve como resultado tipado). | Lanzado (`InvalidOperationException`) / resultado de runtime | Construye el comando con `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` del evento `world.failed`) | La meteorología de inicio no se pudo aplicar después de que arrancara el mundo: OMSI no tiene ninguna estación ICAO configurada para `/weather-real`, o sus objetos meteorológicos no están disponibles. La sesión falla. | Fallo de la sesión | Configura una estación en OMSI o usa `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Una clave de `weather.set` nombra un valor que OMSI deriva en cada tick meteorológico (humedad, factores de luz, transparencia de las nubes, flag de precipitación, suelo mojado); no se escribe nada. | Resultado de runtime | Establece la clave de origen indicada en `detail` (por ejemplo, `dew_point` en lugar de `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Clave de `weather.set` desconocida; no se escribe nada. | Resultado de runtime | Usa una clave de las enumeradas en [capacidades](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` no está en la tabla de variables de cadena. | Detalle de runtime | Enumera primero las variables de cadena. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Un booleano meteorológico no es `true`/`false`/`1`/`0`. Actualmente inalcanzable (véase arriba). | Detalle de runtime (definido) | Consulta el código fuente. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` no es un entero, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` puede ser -1), `scheduled` 0..1; `vehicle.variable.set`: `value` no finito; `vehicle.curve.evaluate`: `x` no finito. | Detalle de runtime | Usa un valor dentro del rango. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` no está en la tabla de variables numéricas. | Detalle de runtime | Enumera primero las variables. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | El slot de la variable o la dirección del valor es nulo. | Detalle de runtime | La variable no está materializada para este vehículo. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Los escalares del reloj se escribieron, pero la llamada nativa perfilada a SetTime devolvió un fallo. | Detalle de runtime | Vuelve a intentarlo; vuelve a leer el valor con `time.read`. |

## RuntimeD3D

Todos los genera `CurrentRuntimeControl` (asignación `ThrowD3D` del estado nativo; `detail` lleva la operación y el HRESULT, `native_status` el estado numérico). Superficie: resultado de runtime con el código en `ErrorCode`; `D3DRuntimeApi` lo vuelve a lanzar como `OmsiRuntimeException`.

| Código | Estado nativo / causa | Qué hacer |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: el dispositivo Direct3D se ha perdido. | Espera a `d3d.restored`; vuelve a crear las texturas (la generación ha cambiado). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: `width`, `height`, `level`, `x`, `y`, `format` o `handle` ausente o no válido (rangos: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Corrige los argumentos. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` no es Base64 válido o supera 48 KiB. | Envía rectángulos más pequeños. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, o un nombre de `format` desconocido (válidos: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Usa un formato de la lista. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Cualquier otro estado; el HRESULT está en `detail`. | Notifícalo con el HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: el dispositivo no está listo (antes del primer fotograma o durante la detención). | Vuelve a intentarlo después de `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: hay un reset del dispositivo en curso. | Vuelve a intentarlo después de `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: el handle de la textura ya se había liberado. | No reutilices handles liberados. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: el handle pertenece a una generación anterior del dispositivo; o la cadena del handle no es `d3dtex-<session>-<hex>` / es cero. | Vuelve a crear la textura. |

<a id="session"></a>
## Sesión

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` en `plugin.request.unsupported` | Plan: se ha solicitado `LastMapState`, `EntrypointIdentity`, el modo de fecha/hora/año, el modo meteorológico, campos del vehículo del jugador o documentos de entrada (`Requested capability unavailable: <name>`). Telemetría: el plugin rechazó el handoff (no puede ocurrir con un plan ejecutable). | Diagnóstico del plan; diagnóstico de la sesión | Quita la solicitud no compatible ([limitaciones conocidas](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` en `headless.arm.failed` | El plugin no pudo armar en OMSI el hook de un solo uso para el inicio headless. | Diagnóstico de la sesión (`Failed`) | Verifica el build; notifícalo. |
| `OL_E_NO_ACTIVE_SESSION` | Cliente de la CLI (`ReportForwarded`, `CliEventWatch`) | Ningún propietario responde en la canalización de control de la instalación (no hay sesión, o el propietario aún se está iniciando/validando). | Envelope de la CLI, salida 4 | Inicia una sesión o espera hasta que esté en `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | Transcurrió `StartupTimeoutSeconds` antes de `plugin.started` (OMSI no cargó `plugins\OmsiLaunch.Plugin.opl`, o está detenido antes de la inicialización del plugin). | Diagnóstico de la sesión (`Failed`) | Revisa el conjunto de archivos del plugin, `plugins\OmsiLaunch.Plugin.opl` y el `logfile.txt` de OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` en `plugin.handoff.invalid` o JSON de telemetría no analizable | El plugin no pudo leer/verificar el handoff de arranque (versiones 3 a 6, SHA-256), o envió telemetría no válida. | Diagnóstico de la sesión (`Failed`) | Haz coincidir las versiones del host y del plugin (vuelve a instalar el paquete). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Ya hay un propietario que responde a `session.status` para esta instalación. | Envelope de la CLI, salida 7 | Usa comandos de cliente (`session status`, `session stop`, comandos de runtime). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | El estado de la sesión no es `Running`. | Lanzado (`InvalidOperationException`) | Primero `WaitForAsync(session, SessionState.Running, ...)`. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | No se pudo construir el plan de splash/ITX; el mensaje lleva el código de presentación. | Diagnóstico del plan | Consulta [Presentación](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (cuadro de diálogo de OmsiLaunchW) | Código de reserva que se muestra cuando un plan de lanzamiento no es ejecutable o la sesión no llegó a la fase de juego, y no existe ningún diagnóstico `OL_E_`. | Solo cuadro de mensaje | Lee `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` en `world.situation.failed` | El inicio nativo de la situación guardada devolvió un fallo (`native_status` en el evento). | Diagnóstico de la sesión (`Failed`) | Revisa el `.osn` y su mapa. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | El plugin se inició, pero no se alcanzó `Running` dentro de `StartupTimeoutSeconds`. | Diagnóstico de la sesión (`Failed`) | Aumenta `/startup-timeout` para mapas grandes; consulta `RuntimeEvents` para ver el último evento del mundo. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Cualquier excepción en la ruta de inicio; el mensaje es el mensaje interno (normalmente empieza por el código interno). | Diagnóstico de la sesión (`Failed`) | Actúa según el código interno. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` en `world.failed` | El inicio nativo de NEW_MAP devolvió un fallo (`native_status` en el evento). | Diagnóstico de la sesión (`Failed`) | Revisa el mapa, el índice del punto de entrada y los registros de OMSI. |

## SessionProfile

Todos los genera `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) o `CliInput`, lanzados como `SessionProfileException` (una `IOException` con `Code`), salida 2 de la CLI. Consulta [perfiles de sesión](session-profiles.md).

| Código | Significado / causa típica | Qué hacer |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | El directorio `assets` del splash del preset o el archivo `profile` de texturas de Internet no existe dentro del paquete. | Añade el recurso. |
| `OL_E_SESSION_PROFILE_INVALID` | Infracción estructural o de límites: más de 256 KiB, no hay exactamente un mapeo raíz, anclas YAML, clave desconocida, falta una clave obligatoria, un valor no escalar donde se exige un escalar, `id` difiere del nombre del directorio, presets fuera de 1..99 o `index` duplicado, timeouts no positivos, modo meteorológico/de splash/de texturas de Internet no compatible, fecha/hora que no es `explicit`, YAML no válido, errores de análisis de números/fechas. | Corrige el YAML según el mensaje. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` no está vacío y no contiene el mapa seleccionado (NEW_MAP) o el mapa de la situación seleccionada (SAVED_SITUATION). | Elige un mundo compatible. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` no existe. | Comprueba el id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Un argumento explícito de la CLI se dirige a un campo que pertenece al perfil/preset seleccionado. | Quita el flag o elige otro preset. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | El id contiene `\`, `/`, `:` o `..`; o una ruta de recurso es absoluta, sale del paquete o atraviesa una unión (junction) o un enlace simbólico. | Mantén las rutas dentro del paquete. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` falta, está fuera de 1..99 o el perfil no lo declara. | Usa un índice de preset declarado. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` no es `omsilaunch.session-profile/v1`. | Usa el esquema compatible. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Una clave de ajuste del preset es conocida pero no admite escritura. | Quita la clave. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Una clave de ajuste del preset no está en el catálogo. | Usa una clave del catálogo. |

<a id="transaction"></a>
## Transacción

Consulta [transacciones y recuperación](../concepts/transactions-and-recovery.md).

| Código | Generado por | Significado / causa típica | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Un `closecheck` obsoleto sigue existiendo después de `File.Delete`. | Diagnóstico de la sesión a través de `OL_E_START_SESSION` | Elimina `<root>\closecheck` manualmente (permisos). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Una ruta que no existía antes de la sesión contiene ahora un contenido distinto del que aplicó la sesión; no se elimina y se conserva el diario. | Lanzado (`IOException`); dentro de `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; salida 8 de la CLI | Examina el archivo; elimínalo o muévelo y, después, usa `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (diario anterior a las huellas) | El diario no tiene huella del contenido aplicado para una ruta que originalmente no existía y no es de eliminación, por lo que no se puede demostrar la propiedad. Al iniciar la sesión, la recuperación se aplaza y se reintenta con los bytes planificados de esta sesión; a través de `RecoverPendingAsync` se lanza. | Lanzado (`IOException`); salida 8 de la CLI | Inicia una sesión con la misma spec (aporta los bytes), o examina y elimina el archivo y, después, usa `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | El SHA-256 de una copia de seguridad no coincide con el snapshot registrado en el diario; no se escribe nada. | Lanzado; dentro de `OL_E_RESTORE_FAILED`; salida 8 de la CLI | Restaura el archivo desde tu propia copia de seguridad; después elimina el diario solo cuando estés seguro. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Existen snapshots en memoria, pero `journal.json` ha desaparecido (eliminado durante la sesión). | Lanzado; dentro de `OL_E_RESTORE_FAILED` | Verifica manualmente los archivos de la sesión. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` sigue existiendo después de eliminarlo (la propia restauración se completó correctamente y se verificó). | Lanzado; dentro de `OL_E_RESTORE_FAILED`; salida 8 de la CLI | Elimina `<root>\.omsilaunch\journal.json` (permisos) o vuelve a ejecutar `/recover` (idempotente). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (fallo de inicio / supervisor) | No se pudo confirmar la salida de OMSI, por lo que los archivos no se reemplazaron mientras OMSI todavía podía estar usándolos; se conserva el diario. | Diagnóstico de la sesión (`Failed`) | Cuando `Omsi.exe` haya terminado, ejecuta `/recover` (o el siguiente inicio recupera automáticamente). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (fallo de inicio / supervisor) | `RestoreAsync` lanzó una excepción; el mensaje lleva el código interno; se conserva el diario. | Diagnóstico de la sesión (`Failed`); salida 8 de la CLI cuando se lanza desde `/recover` | Actúa según el código interno y, después, usa `/recover`. |

<a id="warning"></a>
## Advertencia

| Código | Generado por | Significado | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | El ejecutable de OMSI 2 no tiene una disposición conocida (su identidad de código no figura en el catálogo de builds). El plan sigue siendo ejecutable: cada capacidad nativa se resuelve y se verifica en el proceso, y una que no se puede verificar no está disponible. | Diagnóstico del plan (la ejecutabilidad no se ve afectada) | No hace falta nada; revisa `/plan` y `profiles` para ver las capacidades predichas. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (supervisor) | Se solicitó `Behavior.CooperativeShutdown`, pero no se pudo pedir a OMSI que se cerrara (su rutina de salida no está resuelta en este build, o OMSI ya se está cerrando), o no salió dentro de `ShutdownTimeoutSeconds`: en su lugar se terminó OMSI. La restauración se ejecuta como de costumbre. | Diagnóstico de la sesión | Ninguna: la sesión termina como con una terminación forzada. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Una capacidad que necesita el inicio no se pudo resolver a partir del archivo ejecutable (por ejemplo, un ejecutable protegido con DRM cuya imagen cargada difiere del archivo). Se resuelve en el proceso al iniciar. | Diagnóstico del plan (la ejecutabilidad no se ve afectada) | No hace falta nada; un inicio falla con `OL_E_CAPABILITY_UNRESOLVED` si tampoco se puede resolver en el proceso. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Un módulo que se incorporó a la sesión por sí mismo (archivo de configuración, contenido o activación predeterminada) no se puede cargar (`Data["code"]` indica el motivo y `Data["activation"]` cómo se incorporó). | Diagnóstico del plan (la ejecutabilidad no se ve afectada) | Corrige, desactiva o quita el módulo. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | No se puede usar una entrada de `.omsilaunch\modules.ini` (no es una sección de módulo, está fuera de una sección, clave desconocida, valor no válido; se usa el valor predeterminado), o se solicitaron ajustes para un módulo que la sesión no carga. | Diagnóstico del plan (la ejecutabilidad no se ve afectada) | Corrige la entrada o la solicitud. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Una ruta de eliminación de sesión (destino ITX, `Texture\standard.ipr`, `closecheck`) no existía antes de la sesión y existe ahora, pero nunca se inició ningún proceso de OMSI bajo este diario, por lo que el archivo no puede ser un subproducto de la sesión. Se conserva y se notifica (mensaje = ruta relativa, `Data["sha256"]`); la transacción se completa igualmente. | Diagnóstico de la sesión / `RecoveryStatus.Diagnostics` (no afecta al estado) | Examina el archivo; elimínalo tú mismo si no lo quieres. |

<a id="module"></a>
## Módulo

Módulos de runtime (consulta [módulos de runtime](runtime-modules.md)). En la planificación, un código es un diagnóstico del plan: para un módulo solicitado hace que el plan no sea ejecutable; para un módulo que se incorporó por sí mismo (archivo de configuración, contenido, predeterminado) el plan notifica en su lugar `OL_W_MODULE_NOT_LOADED`. En una sesión, un código es un diagnóstico de la sesión (la sesión continúa salvo que el código sea `OL_E_MODULE_SESSION_FAILED`), y para una operación de módulo es el resultado de runtime.

| Código | Generado por | Significado | Superficie | Qué hacer |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | Falta `module.json`, no es un `omsilaunch.runtime-module/v1` válido, o su `id` difiere del nombre de su directorio. El mensaje indica el campo. | Diagnóstico del plan; `ListRuntimeModulesAsync` | Corrige el manifiesto. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Un id de módulo solicitado no está instalado bajo `plugins\OmsiLaunch.Modules`. | Diagnóstico del plan | Revisa `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Falta un archivo fijado, difiere de su SHA-256 fijado o se alcanza a través de un punto de reanálisis; o, en el proceso, `module.json` cambió después de la planificación. | Diagnóstico del plan; diagnóstico de la sesión | Vuelve a instalar el módulo. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | El intervalo `bridge_protocol` del módulo no incluye ninguno de los protocolos que sirve este build (1 y 2). | Diagnóstico del plan | Usa una versión del módulo hecha para este OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Una capacidad obligatoria es desconocida para este build, no está disponible en esta instalación o no se resolvió en el proceso. | Diagnóstico del plan; diagnóstico de la sesión | Consulta [capacidades](capabilities.md) y `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | La sesión no ejecuta el mapa vinculado del módulo, o un archivo vinculado difiere de su SHA-256 fijado. | Diagnóstico del plan; diagnóstico de la sesión | Inicia el mapa vinculado con el contenido vinculado. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | El módulo pidió iniciar un archivo que no es uno de sus componentes auxiliares. | Resultado de runtime dentro del módulo | Defecto del módulo. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | El componente gestionado no se pudo cargar o no se pudo crear su tipo de entrada; o el programa de sesión del módulo no se pudo iniciar o terminó antes de estar listo. | Diagnóstico de la sesión | Revisa el build del módulo (compatible con x86, el tipo de entrada implementa `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` o `Stop` lanzó una excepción. | Diagnóstico de la sesión | Defecto del módulo; se aplicó su `failure_policy`. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Una fase o una operación volvió después de su presupuesto (`timeouts`, `timeout_ms`), o un programa de sesión no estuvo listo dentro de `prepare_ms`. | Diagnóstico de la sesión; resultado de runtime | Defecto del módulo; se aplicó su `failure_policy`. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | El módulo se desactivó o falló antes en la sesión. | Resultado de runtime | Consulta los diagnósticos de la sesión para ver el primer fallo. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | La sesión no cargó este módulo, o todavía no está listo (antes de la fase de juego). | Resultado de runtime | Solicita el módulo al iniciar; espera a `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | El módulo no declara tal operación. | Resultado de runtime | Revisa las `operations` del módulo. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | La operación falló sin un código catalogado; el código propio de un módulo está en `Values["module_code"]`. | Resultado de runtime | Específico del módulo. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Tras la detención o un fallo, un recurso rastreado seguía vivo (un proceso auxiliar que no terminó, un thread en segundo plano que no finalizó) o su liberación lanzó una excepción. | Diagnóstico de la sesión | Notifica el módulo; el proceso termina con OMSI en cualquier caso. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Falló un módulo cuya `failure_policy` es `fail-session`; la sesión se detiene y se restaura. | Diagnóstico de la sesión (estado `Failed`) | Consulta el código de módulo anterior. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Un ajuste de módulo solicitado no es válido o el módulo no lo declara, un módulo está a la vez activado y desactivado, el nombre de un ajuste no es `<module-id>.<key>`, o un flag de módulo lo declaran varios módulos. | Diagnóstico del plan (no ejecutable); error de la CLI | Corrige la solicitud; `/list:RuntimeModules` muestra los ajustes de cada módulo. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Un plugin de OMSI que el módulo enumera en `conflicts.plugins` está activado (`plugins\*.opl`) o cargado en OMSI. El motivo nombra el `.opl`. | Diagnóstico del plan; diagnóstico de la sesión | Desactiva o quita ese plugin. OmsiLaunch nunca lo cambia. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | reservado | Reservado; un componente nativo recibe en su lugar los resultados numéricos de hook descritos en [módulos de runtime](runtime-modules.md#native-components-and-hooks). | ninguna | ninguna |

<a id="non-error-diagnostic-codes"></a>
## Códigos de diagnóstico que no son errores

| Código | Emitido por | Mensaje / datos | Significado |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | mensaje = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | Se creó `Omsi.exe` y se registró su identidad. Diagnóstico de la sesión. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | mensaje = SHA-256 del archivo eliminado | Un `closecheck` que existía antes de la sesión se eliminó de forma permanente (`SuppressStaleClosecheckWarning = true`). Diagnóstico de la sesión. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | mensaje = ruta relativa; `Data["sha256"]` | OMSI volvió a crear una ruta de eliminación de sesión durante una sesión cuyo proceso se había iniciado; se eliminó para restaurar su ausencia original. Diagnóstico de la sesión / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | mensaje = `manifest` o `self` | Qué referencia usa la validación del plugin permanente. Diagnóstico del plan. |
| `module.planned` | `SessionPlanner` | mensaje = id del módulo; `Data["module"]`, `Data["status"]`, `Data["activation"]` | El módulo está en el plan. Diagnóstico del plan. |
| `module.disabled` | `SessionPlanner` | mensaje = id del módulo | Un módulo de contenido o predeterminado se mantuvo fuera mediante una desactivación (solicitud o archivo de configuración). Diagnóstico del plan. |
| `session_profile.selected` | `SessionPlanner` | mensaje = id del perfil; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Procedencia de una sesión compilada a partir de un perfil de sesión. Diagnóstico del plan. |

Los nombres de los eventos de runtime (`RuntimeEvent.Type`, no son diagnósticos) se enumeran en el [ciclo de vida de la sesión](../concepts/session-lifecycle.md#telemetry-events).
