# Referencia de OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Traducción de la [página original en inglés](../../../reference/omsilaunchw.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

`OmsiLaunchW.exe` es el host del subsistema Windows (GUI) del controlador de OmsiLaunch. Acepta la misma línea de comandos que `OmsiLaunch.exe` y ejecuta el mismo código del controlador (`OmsiLaunch.Controller.dll`). La única diferencia está en la forma de informar: no hay ventana de consola, los errores se muestran como cuadros de mensaje y una sesión en ejecución solo es visible a través de su [icono de la bandeja](windows-tray.md).

Fuentes de referencia: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (el shim nativo), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) y `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Comparación entre OmsiLaunch.exe y OmsiLaunchW.exe

| Aspecto | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsistema | Consola. Abre una ventana de consola cuando se inicia desde el Explorador. | Windows (GUI). Sin ventana de consola. |
| Shim nativo | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Entorno | sin cambios | establece `OMSILAUNCH_WINDOWS_HOST=1` para el proceso del controlador antes de que se inicie .NET |
| Argumentos | se dividen en tokens con `CommandLineToArgvW` y se pasan sin cambios al controlador | lo mismo, de modo que ambos hosts aceptan exactamente los mismos flags y comandos ([referencia de la CLI](cli.md)) |
| Salida de texto | se escribe en stdout | se suprime, salvo que se indique `--json` (entonces los envelopes JSON se escriben en stdout, que el llamador puede redirigir) |
| Errores | `OL_E_...: message` o un envelope JSON de error | las mismas reglas de salida de consola, **más** un cuadro de mensaje para cada error (consulta [Cuadros de diálogo de error](#failure-dialogs)) |
| `/silent` | inicia `OmsiLaunchW.exe` con los demás argumentos y devuelve `0` | se ignora: el comando ya se ejecuta en el host de Windows |
| Icono de la bandeja | se muestra para una sesión con propietario | se muestra para una sesión con propietario |
| Rutas de detención | bandeja, `session stop`, salida de OMSI, `/observe-seconds`, Ctrl+C, cierre de la consola | bandeja, `session stop`, salida de OMSI, `/observe-seconds` (no hay consola, por lo que Ctrl+C y el cierre de la consola no se aplican) |
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
| `100` | no se ha podido resolver la ruta del ejecutable |
| `101` | no se ha podido dividir en tokens la línea de comandos |
| `102` | ha fallado la búsqueda de la ubicación de `hostfxr` (normalmente: el runtime de .NET 6 x64 no está instalado) |
| `103` | no se ha podido obtener la ruta de `hostfxr` |
| `104` | no se ha podido cargar `hostfxr` |
| `105` | faltan exportaciones obligatorias de `hostfxr` |
| `106` | no se ha podido inicializar el host administrado (por ejemplo, falta `OmsiLaunch.Controller.dll` o su configuración de runtime, o no está presente el runtime de Windows Desktop) |

`OmsiLaunch.exe` usa la misma tabla, pero no imprime nada. Estos cuadros de diálogo no se han producido en runtime (consulta el [estado de la validación en runtime](../status/runtime-validation-status.md)).

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

Coloca los ejecutables en la instalación de OMSI 2 (la estructura del paquete; consulta [empaquetado](packaging.md)); si no se indica ningún argumento de instalación, la instalación es el directorio que contiene el ejecutable. Una instalación explícita se pasa como primer argumento, igual que con `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Como es un programa GUI, ni `cmd.exe` ni el Explorador esperan a que termine. Para esperar y leer el código de salida desde un script, usa `start /wait OmsiLaunchW.exe ...` en `cmd.exe` o `Start-Process -Wait -PassThru` en PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delegación con /silent

`OmsiLaunch.exe ... /silent` (o `--silent`), cuando no se está ejecutando ya bajo `OmsiLaunchW.exe`, hace lo siguiente (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Busca `OmsiLaunchW.exe` en el directorio de `OmsiLaunch.exe`. Si no está: `OL_E_WINDOWS_HOST_MISSING`, salida `7`.
2. Inicia `OmsiLaunchW.exe` mediante `ShellExecute` (`UseShellExecute = true`) con el directorio actual y todos los argumentos excepto `/silent`/`--silent`, en el orden original. `ShellExecute` no pasa los handles del llamador al nuevo proceso, de modo que un llamador que captura la salida de `OmsiLaunch.exe /silent` no queda bloqueado durante toda la vida de la sesión (cierre de runtime BUG-03). Si no se devuelve ningún proceso: `OL_E_WINDOWS_HOST_START_FAILED`, salida `7`.
3. Escribe el envelope `silent` y termina inmediatamente con `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

La salida `0` solo significa que se ha iniciado `OmsiLaunchW.exe`. El resultado de la sesión (errores de planificación, un propietario ya activo, un inicio fallido) lo comunica `OmsiLaunchW.exe` con sus propios cuadros de diálogo y diagnósticos, y a partir de ese momento los dos procesos son independientes: `OmsiLaunch.exe` ha terminado y `OmsiLaunchW.exe` es el propietario de la sesión. Usa `OmsiLaunch.exe session status` para ver la sesión.

`/silent` se aplica antes que cualquier otro comando, por lo que `OmsiLaunch.exe /silent session status` también ejecuta `session status` dentro de `OmsiLaunchW.exe`, donde su salida se suprime. Usa `/silent` solo para lanzamientos.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Qué hace cada comando bajo OmsiLaunchW.exe

| Línea de comandos | Resultado |
| --- | --- |
| sin argumentos | ejecuta `detect` de forma silenciosa y termina con `0` (no se muestra nada) |
| un lanzamiento (`/new`, `/saved:...`, `/spec:...`, un perfil de sesión) con un plan ejecutable y sin propietario | se convierte en el propietario de la sesión: icono de la bandeja durante el inicio y la ejecución; termina cuando finaliza la sesión (`0` completada, `1` fallida) |
| un lanzamiento cuyo plan no es ejecutable | cuadro de diálogo con el último diagnóstico `OL_E_` del plan (alternativa `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), salida `1` (auditoría de documentación BUG-06; antes de la corrección terminaba en silencio) |
| un lanzamiento mientras ya hay un propietario activo para la instalación | cuadro de diálogo `OL_E_SESSION_ALREADY_ACTIVE`, salida `7`; la sesión en ejecución no se ve afectada |
| un lanzamiento que no llega a `Running` | cuadro de diálogo `The OMSI session did not reach gameplay.` con el último diagnóstico `OL_E_`, salida `1`. El supervisor de la sesión termina OMSI y restaura los archivos mientras el cuadro de diálogo está abierto; el proceso termina después de que se cierre el cuadro de diálogo y haya finalizado `CloseAsync` |
| un argumento no válido, un flag desconocido o un perfil de sesión no válido | cuadro de diálogo con `OL_E_INVALID_ARGUMENT` o el código `OL_E_SESSION_PROFILE_*`, salida `2` |
| un comando de cliente (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) sin propietario | cuadro de diálogo `OL_E_NO_ACTIVE_SESSION`, salida `4` |
| un comando de cliente que el propietario rechaza | cuadro de diálogo con el código de rechazo (por ejemplo `OL_E_CONTROL_SESSION_MISMATCH` o un código de error de runtime), salida `7` (`2` para una operación desconocida o un argumento que falta) |
| un comando de cliente o de descubrimiento correcto (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | ninguna salida visible salvo que se indique `--json` y stdout esté redirigido; código de salida igual que con `OmsiLaunch.exe` |
| `/plan` o `/validate` | ningún cuadro de diálogo, ni siquiera para un plan que no es ejecutable; salida `0` o `1` |
| cualquier otro error del controlador | cuadro de diálogo con el código `OL_E_` clasificado; código de salida según los [códigos de salida](exit-codes.md) |

<a id="failure-dialogs"></a>
## Cuadros de diálogo de error

Todo error que `OmsiLaunch.exe` imprimiría se muestra también como un cuadro de mensaje modal (`WindowsHost.ShowFailure`), incluso cuando se indica `--json`:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` es el mensaje de error o, en el caso de un fallo de la sesión, el mensaje del último diagnóstico `OL_E_`. Limitación conocida: para un fallo comunicado por el plugin, el mensaje es la carga útil de fallo sin procesar del plugin (por ejemplo `{"name":"world.failed",...}`); la línea `Code:` es correcta (consulta las [limitaciones conocidas](known-limitations.md)). El cuadro de diálogo es modal y el proceso termina después de que se cierre. Evidencia de runtime: error de argumento, ninguna sesión activa y un fallo antes de la fase de juego (cierre de runtime `T04`).

<a id="session-tray-and-exit"></a>
## Sesión, bandeja y salida

Una sesión cuyo propietario es `OmsiLaunchW.exe` se comporta exactamente igual que una cuyo propietario es `OmsiLaunch.exe` (consulta el [ciclo de vida de la sesión](../concepts/session-lifecycle.md)):

- El icono de la bandeja aparece en cuanto se ha iniciado la sesión, antes de que OMSI llegue a la fase de juego, salvo que se establezca `Presentation.SuppressTrayIcon` en un archivo `/spec`. Con `SuppressTrayIcon` no hay ninguna superficie visible; detén la sesión con `OmsiLaunch.exe session stop` o cerrando OMSI.
- La sesión finaliza cuando OMSI termina, cuando se confirma `End session` (finalizar la sesión) en la bandeja, cuando un cliente envía `session stop` o cuando transcurre `/observe-seconds`. A continuación, OmsiLaunch termina OMSI si sigue en ejecución, restaura todos los archivos que ha modificado, libera el lease de la instalación, quita el icono de la bandeja y termina.
- Si se mata el propio `OmsiLaunchW.exe`, el siguiente inicio de OmsiLaunch para esa instalación recupera la transacción pendiente (consulta [transacciones y recuperación](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Inicio rápido

1. Instala el paquete en el directorio de OMSI 2 ([instalación](../getting-started/installation.md)).
2. Crea un acceso directo a `OmsiLaunchW.exe` con los argumentos de la sesión, por ejemplo `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Inícialo. OMSI se inicia sin ventana de consola; el icono de OmsiLaunch aparece en el área de notificación.
4. Haz clic con el botón derecho en el icono → `Status` (estado) para ver la sesión, o `End session` → `End session` (finalizar la sesión, y confirmarlo) para finalizarla.
5. Si algo sale mal, el cuadro de diálogo muestra el código de error; los detalles están en `<OMSI_PATH>\.omsilaunch\diagnostics`.
