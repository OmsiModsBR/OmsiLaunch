# Referencia de OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Traducción de la [página original en inglés](../../../reference/omsilaunchw.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

`OmsiLaunchW.exe` es el host del controlador de OmsiLaunch para el subsistema Windows (GUI). Acepta la misma línea de comandos que `OmsiLaunch.exe` y ejecuta el mismo código del controlador (`OmsiLaunch.Controller.dll`). La única diferencia es la forma en que informa los resultados: no hay ventana de consola, los errores se muestran como cuadros de mensaje y una sesión en ejecución solo es visible a través de su [ícono de la bandeja](windows-tray.md).

Fuentes de referencia: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (el shim nativo), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) y `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Comparación entre OmsiLaunch.exe y OmsiLaunchW.exe

| Aspecto | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsistema | Consola. Abre una ventana de consola cuando se inicia desde el Explorador de Windows. | Windows (GUI). Sin ventana de consola. |
| Shim nativo | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Entorno | sin cambios | establece `OMSILAUNCH_WINDOWS_HOST=1` para el proceso del controlador antes de que se inicie .NET |
| Argumentos | se dividen en tokens con `CommandLineToArgvW` y se pasan sin cambios al controlador | lo mismo, por lo que ambos hosts aceptan exactamente los mismos flags y comandos ([referencia de la CLI](cli.md)) |
| Salida de texto | se escribe en stdout | se suprime, salvo que se indique `--json` (en ese caso, los envoltorios JSON se escriben en stdout, que el llamador puede redirigir) |
| Errores | `OL_E_...: message` o un envoltorio JSON de error | las mismas reglas de salida de consola, **además de** un cuadro de mensaje para cada error (consulte [Diálogos de error](#failure-dialogs)) |
| `/silent` | inicia `OmsiLaunchW.exe` con los demás argumentos y devuelve `0` | se ignora: el comando ya se ejecuta en el host de Windows |
| Ícono de la bandeja | se muestra para una sesión con propietario | se muestra para una sesión con propietario |
| Rutas de detención | bandeja, `session stop`, salida de OMSI, `/observe-seconds`, Ctrl+C, cierre de la consola | bandeja, `session stop`, salida de OMSI, `/observe-seconds` (no hay consola, por lo que Ctrl+C y el cierre de la consola no aplican) |
| Códigos de salida | [`PublicExitCode`](exit-codes.md) `0`..`10`, códigos del shim `100`..`106` | los mismos códigos |

<a id="how-it-starts"></a>
## Cómo se inicia

1. El shim resuelve su propia ruta (`GetModuleFileNameW`) y espera encontrar `OmsiLaunch.Controller.dll` en el mismo directorio.
2. Divide la línea de comandos en tokens (`CommandLineToArgvW`) y establece `OMSILAUNCH_WINDOWS_HOST=1`.
3. Localiza `hostfxr` mediante el `nethost.dll` incluido en el paquete, lo carga, inicializa el controlador con los argumentos (la ruta del controlador no forma parte de la lista de argumentos que ve el analizador de la CLI) y lo ejecuta.
4. El shim devuelve sin cambios el código de salida del controlador.

Si falla cualquier paso anterior a la ejecución del controlador, el shim muestra un cuadro de mensaje con el título `OmsiLaunch` y el texto `OmsiLaunch could not start the .NET host (code N).` y termina con ese código:

| Código | Paso fallido |
| --- | --- |
| `100` | no se pudo resolver la ruta del archivo ejecutable |
| `101` | no se pudo dividir la línea de comandos en tokens |
| `102` | falló la búsqueda de la ubicación de `hostfxr` (por lo general: el runtime .NET 6 x64 no está instalado) |
| `103` | no se pudo obtener la ruta de `hostfxr` |
| `104` | no se pudo cargar `hostfxr` |
| `105` | faltan exportaciones requeridas de `hostfxr` |
| `106` | no se pudo inicializar el host administrado (por ejemplo, falta `OmsiLaunch.Controller.dll` o su configuración de runtime, o no está presente el Windows Desktop runtime) |

`OmsiLaunch.exe` usa la misma tabla, pero no imprime nada. Estos diálogos no se han producido en runtime (consulte el [estado de la validación en runtime](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Iniciarlo

Inicio directo, desde un acceso directo, un script u otro programa:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

A través de `OmsiLaunch.exe` con `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Coloque los archivos ejecutables en la instalación de OMSI 2 (la estructura del paquete; consulte [empaquetado](packaging.md)); si no se indica un argumento de instalación, la instalación es el directorio que contiene el archivo ejecutable. Una instalación explícita se pasa como primer argumento, igual que con `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Como es un programa GUI, ni `cmd.exe` ni el Explorador de Windows esperan a que termine. Para esperar y leer el código de salida desde un script, use `start /wait OmsiLaunchW.exe ...` en `cmd.exe` o `Start-Process -Wait -PassThru` en PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delegación con /silent

`OmsiLaunch.exe ... /silent` (o `--silent`), cuando no se está ejecutando ya bajo `OmsiLaunchW.exe`, hace lo siguiente (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Busca `OmsiLaunchW.exe` en el directorio de `OmsiLaunch.exe`. Si no existe: `OL_E_WINDOWS_HOST_MISSING`, salida `7`.
2. Inicia `OmsiLaunchW.exe` mediante `ShellExecute` (`UseShellExecute = true`) con el directorio actual y todos los argumentos excepto `/silent`/`--silent`, en el orden original. `ShellExecute` no pasa los handles del llamador al nuevo proceso, por lo que un llamador que captura la salida de `OmsiLaunch.exe /silent` no queda bloqueado durante toda la vida de la sesión (cierre en runtime de BUG-03). Si no se devuelve ningún proceso: `OL_E_WINDOWS_HOST_START_FAILED`, salida `7`.
3. Escribe el envoltorio `silent` y termina inmediatamente con `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

La salida `0` solo significa que se inició `OmsiLaunchW.exe`. El resultado de la sesión (errores de planificación, un propietario ya activo, un inicio fallido) lo informa `OmsiLaunchW.exe` con sus propios diálogos y diagnósticos, y a partir de ese momento los dos procesos son independientes: `OmsiLaunch.exe` ya terminó y `OmsiLaunchW.exe` es el propietario de la sesión. Use `OmsiLaunch.exe session status` para ver la sesión.

`/silent` se aplica antes que cualquier otro comando, por lo que `OmsiLaunch.exe /silent session status` también ejecuta `session status` dentro de `OmsiLaunchW.exe`, donde su salida se suprime. Use `/silent` solo para inicios.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Qué hace cada comando bajo OmsiLaunchW.exe

| Línea de comandos | Resultado |
| --- | --- |
| sin argumentos | ejecuta `detect` de forma silenciosa y termina con `0` (no se muestra nada) |
| un inicio (`/new`, `/saved:...`, `/spec:...`, un perfil de sesión) con un plan ejecutable y sin propietario | se convierte en el propietario de la sesión: ícono de la bandeja durante el arranque y la ejecución; termina cuando finaliza la sesión (`0` completada, `1` fallida) |
| un inicio cuyo plan no es ejecutable | diálogo con el último diagnóstico `OL_E_` del plan (valor de respaldo `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), salida `1` (auditoría de documentación BUG-06; antes de la corrección terminaba de forma silenciosa) |
| un inicio mientras ya hay un propietario activo para la instalación | diálogo `OL_E_SESSION_ALREADY_ACTIVE`, salida `7`; la sesión en ejecución no se ve afectada |
| un inicio que no llega a `Running` | diálogo `The OMSI session did not reach gameplay.` con el último diagnóstico `OL_E_`, salida `1`. El supervisor de la sesión termina OMSI y restaura los archivos mientras el diálogo está abierto; el proceso termina después de que se cierra el diálogo y de que finaliza `CloseAsync` |
| un argumento no válido, un flag desconocido o un perfil de sesión no válido | diálogo con `OL_E_INVALID_ARGUMENT` o el código `OL_E_SESSION_PROFILE_*`, salida `2` |
| un comando de cliente (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) sin propietario | diálogo `OL_E_NO_ACTIVE_SESSION`, salida `4` |
| un comando de cliente que el propietario rechaza | diálogo con el código de rechazo (por ejemplo, `OL_E_CONTROL_SESSION_MISMATCH` o un código de error de runtime), salida `7` (`2` para una operación desconocida o un argumento faltante) |
| un comando de cliente o de descubrimiento exitoso (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | sin salida visible, salvo que se indique `--json` y stdout esté redirigido; código de salida igual que con `OmsiLaunch.exe` |
| `/plan` o `/validate` | sin diálogo, incluso para un plan que no es ejecutable; salida `0` o `1` |
| cualquier otro error del controlador | diálogo con el código `OL_E_` clasificado; código de salida según los [códigos de salida](exit-codes.md) |

<a id="failure-dialogs"></a>
## Diálogos de error

Todo error que `OmsiLaunch.exe` imprimiría se muestra también como un cuadro de mensaje modal (`WindowsHost.ShowFailure`), incluso cuando se indica `--json`:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` es el mensaje de error o, en el caso de un error de sesión, el mensaje del último diagnóstico `OL_E_`. Limitación conocida: para un error informado por el plugin, el mensaje es el payload de error sin procesar del plugin (por ejemplo, `{"name":"world.failed",...}`); la línea `Code:` es correcta (consulte las [limitaciones conocidas](known-limitations.md)). El diálogo es modal y el proceso termina después de que se cierra. Evidencia de runtime: error de argumento, ninguna sesión activa y un error antes del gameplay (cierre en runtime `T04`).

<a id="session-tray-and-exit"></a>
## Sesión, bandeja y salida

Una sesión cuyo propietario es `OmsiLaunchW.exe` se comporta exactamente igual que una cuyo propietario es `OmsiLaunch.exe` (consulte el [ciclo de vida de la sesión](../concepts/session-lifecycle.md)):

- El ícono de la bandeja aparece en cuanto se inicia la sesión, antes de que OMSI llegue al gameplay, salvo que se establezca `Presentation.SuppressTrayIcon` en un archivo `/spec`. Con `SuppressTrayIcon` no hay ninguna superficie visible; detenga la sesión con `OmsiLaunch.exe session stop` o cerrando OMSI.
- La sesión finaliza cuando OMSI termina, cuando se confirma `End session` (finalizar sesión) en la bandeja, cuando un cliente envía `session stop` o cuando transcurre el tiempo de `/observe-seconds`. Entonces OmsiLaunch termina OMSI si todavía está en ejecución, restaura todos los archivos que modificó, libera el lease de la instalación, quita el ícono de la bandeja y termina.
- Si se fuerza la terminación del propio `OmsiLaunchW.exe`, el siguiente inicio de OmsiLaunch para esa instalación recupera la transacción pendiente (consulte [transacciones y recuperación](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Inicio rápido

1. Instale el paquete en el directorio de OMSI 2 ([instalación](../getting-started/installation.md)).
2. Cree un acceso directo a `OmsiLaunchW.exe` con los argumentos de la sesión, por ejemplo `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Inícielo. OMSI se inicia sin ventana de consola; el ícono de OmsiLaunch aparece en el área de notificación.
4. Haga clic con el botón derecho en el ícono → `Status` (estado) para ver la sesión, o `End session` → `End session` (finalizar sesión) para finalizarla.
5. Si algo sale mal, el diálogo muestra el código de error; los detalles están en `<OMSI_PATH>\.omsilaunch\diagnostics`.
