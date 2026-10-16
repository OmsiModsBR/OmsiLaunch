# Códigos de salida

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Traducción de la [página original en inglés](../../../reference/exit-codes.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página enumera cada código de salida de proceso que pueden devolver `OmsiLaunch.exe` y `OmsiLaunchW.exe`: el contrato gestionado público `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), los códigos del shim nativo `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` y `OmsiLaunch.WindowsHost.cpp`) y las reglas de clasificación que `CliProgram.Classify` aplica a cualquier excepción que escape (`tools\OmsiLaunch.Cli\Program.cs`). Los llamadores deben deducir la semántica a partir del código y del envelope de error estructurado, nunca del texto del mensaje. Los códigos de error se catalogan en [errores](errors.md); los comandos que producen cada código están en la [referencia de la CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Códigos de salida públicos (`PublicExitCode`)

| Código | Nombre de la enumeración | Significado | Cuándo |
|---|---|---|---|
| 0 | `Success` | El comando se completó. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` con un plan ejecutable; una sesión que terminó en `Completed`; `/silent` una vez iniciado `OmsiLaunchW.exe`; un comando de cliente reenviado al que el propietario respondió con `Ok=true`; `/recover` cuando no había nada pendiente o la restauración se completó. |
| 1 | `SessionFailed` | Un plan no era ejecutable, o una sesión propia terminó en `Failed`. | `/plan` que notifica `NOT RUNNABLE`; un lanzamiento cuyo plan no es ejecutable (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` además muestra el último diagnóstico `OL_E_` en un cuadro de mensaje); `OL_E_PLAN_NOT_RUNNABLE` generado por `StartSessionAsync` (nueva planificación en el inicio); la sesión no llegó a `Running` dentro del timeout de arranque; la sesión terminó en `Failed`. |
| 2 | `InvalidArguments` | La línea de comandos, la especificación, el perfil o los argumentos de runtime se rechazaron antes o durante el despacho. | Flag o ruta desconocidos, valor ausente, valor fuera de rango; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` y `OL_E_RUNTIME_ARGUMENT_REQUIRED` (localmente o devueltos por el propietario); uso impreso para un comando que no se puede despachar; cualquier `ArgumentException`, `FormatException`, `InvalidDataException` u `OverflowException`. |
| 3 | `UnsupportedProfile` | La plataforma o la build de OMSI no se admite. | Una excepción que escapa cuyo código empieza por `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Ten en cuenta que, si las mismas condiciones se detectan durante la planificación, el plan deja de ser ejecutable y se devuelve `1` en su lugar. |
| 4 | `NoActiveSession` | Un comando de cliente no encontró ningún propietario. | `session status`, `session stop`, `events read`, `events watch` o una operación de runtime reenviada cuando el endpoint de control local de esta instalación no responde (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Un timeout escapó como excepción. | Cualquier `TimeoutException` (`OL_E_TIMEOUT` cuando el mensaje no incluye ningún código; en caso contrario, el código incrustado, como `OL_E_RUNTIME_REQUEST_TIMEOUT`). El propietario responde a los timeouts de cliente reenviados con `Ok=false`, que devuelven `7`, no `5`. |
| 6 | `NotFound` | No se encontró un archivo o directorio. | `FileNotFoundException` / `DirectoryNotFoundException` (`OL_E_NOT_FOUND` predeterminado), por ejemplo `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` cuando se genera como excepción, o un directorio de instalación inexistente durante `/list`. |
| 7 | `OperationRejected` | El comando era válido pero se rechazó, o un comando reenviado falló en el propietario. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; cada respuesta de control `Ok=false` distinta de los dos códigos de argumento (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); cualquier otra excepción que escape con un código `OL_E_` que no esté clasificado en otro lugar (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | No se pudo restaurar una transacción duradera. | `/recover` cuando el diario estaba pendiente y sigue pendiente; cualquier excepción que escape cuyo código empiece por `OL_E_RECOVERY_` o sea `OL_E_RESTORE_FAILED` (por ejemplo, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` durante la propia restauración de una sesión). |
| 10 | `InternalError` | Una excepción inesperada sin código `OL_E_`. | Se notifica como `OL_E_INTERNAL` con la categoría `internal`; el mensaje es el texto de la excepción. |

El código `9` no está asignado.

<a id="native-shim-exit-codes"></a>
## Códigos de salida del shim nativo

Los devuelve `OmsiLaunch.exe` / `OmsiLaunchW.exe` antes de que se ejecute el controlador gestionado. No se solapan con `PublicExitCode`, de modo que un llamador puede distinguir un fallo de arranque del host de un resultado del controlador. `OmsiLaunchW.exe` muestra además `OmsiLaunch could not start the .NET host (code N).` en un cuadro de mensaje.

| Código | Significado | Causa |
|---|---|---|
| 100 | No se pudo resolver la ruta del ejecutable | `GetModuleFileNameW` falló. |
| 101 | No se pudo dividir en tokens la línea de comandos | `CommandLineToArgvW` devolvió null. |
| 102 | Falló el sondeo de la ubicación de `hostfxr` | Falló la consulta de tamaño de `get_hostfxr_path`: no hay instalado ningún runtime de .NET adecuado (se requiere el runtime x64 de .NET 6). |
| 103 | No se pudo obtener la ruta de `hostfxr` | Falló la segunda llamada a `get_hostfxr_path`. |
| 104 | No se pudo cargar la biblioteca `hostfxr` | Falló `LoadLibraryW` sobre el `hostfxr.dll` resuelto. |
| 105 | Faltan exportaciones necesarias de `hostfxr` | No se encontraron `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` o `hostfxr_close`. |
| 106 | No se pudo inicializar el host gestionado | `hostfxr_initialize_for_dotnet_command_line` falló para `OmsiLaunch.Controller.dll` (falta `OmsiLaunch.Controller.runtimeconfig.json`, falta `Microsoft.WindowsDesktop.App` 6.0 x64 o el paquete está dañado). |

<a id="classification-rules-cliprogramclassify"></a>
## Reglas de clasificación (`CliProgram.Classify`)

Cada excepción que escapa de `CliProgram.RunAsync` se convierte en un envelope de error (`CliInput.WriteError`) y en un código de salida mediante `CliProgram.ReportFailure`, que llama a `Classify`. Los fallos de análisis se tratan de la misma manera antes del despacho (salida `2`). Las reglas se aplican en este orden:

1. Se extrae el primer token `OL_E_` del mensaje de la excepción (`ExtractCode`): el código es la secuencia más larga de letras ASCII, dígitos y `_` que empieza en `OL_E_`. Los códigos se muestran literalmente en `error.code`.
2. `SessionProfileException` → su propio `Code`, categoría `invalid_argument`, salida `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → código extraído u `OL_E_INVALID_ARGUMENT`, categoría `invalid_argument`, salida `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → código extraído u `OL_E_NOT_FOUND`, categoría `not_found`, salida `6`.
5. `TimeoutException` → código extraído u `OL_E_TIMEOUT`, categoría `runtime`, salida `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, categoría `session`, salida `7`.
7. En otro caso, cuando se extrajo un código:
   - empieza por `OL_E_RECOVERY_` o es igual a `OL_E_RESTORE_FAILED` → categoría `transaction`, salida `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → categoría `session`, salida `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → categoría `session`, salida `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → categoría `invalid_argument`, salida `2`;
   - empieza por `OL_E_UNSUPPORTED_` → categoría `unsupported_profile`, salida `3`;
   - cualquier otro código → categoría `runtime` para `InvalidOperationException` e `IOException`, en otro caso `internal`; salida `7`.
8. Ningún código → `OL_E_INTERNAL`, categoría `internal`, salida `10`.

Las respuestas de cliente reenviadas no pasan por `Classify`: `CliProgram.ReportForwarded` devuelve `4` si no hay endpoint, `2` para `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` para cualquier otra respuesta `Ok=false` y `0` para `Ok=true`.

<a id="scripting-guidance"></a>
## Recomendaciones para scripts

- Trata `0` como éxito y todo lo demás como fallo; bifurca según el código numérico y, después, según `error.code` del envelope `--json`.
- Un lanzamiento de sesión solo vuelve después de que la sesión haya terminado y sus archivos se hayan restaurado; `1` significa que la transacción se ejecutó pero OMSI falló o el plan se rechazó, no que queden archivos modificados (un diario residual lo notifica `/recovery-status`).
- `100`..`106` significan que el paquete o el runtime de .NET están dañados; consulta [instalación](../getting-started/installation.md) y [empaquetado](packaging.md).
