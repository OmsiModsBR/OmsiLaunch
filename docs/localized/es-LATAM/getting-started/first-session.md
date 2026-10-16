# Primera sesión

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Traducción de la [página original en inglés](../../../getting-started/first-session.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página recorre la primera sesión de OMSI administrada con OmsiLaunch `0.1.0-beta4`: planificar sin iniciar OMSI, iniciar con flags explícitos, iniciar con un perfil de sesión predefinido, controlar y detener la sesión, y encontrar los diagnósticos después. Se supone que el paquete está instalado como se describe en [instalación](installation.md). Cada flag se especifica en la [referencia de la CLI](../reference/cli.md); hay más invocaciones en [ejemplos de la CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Qué hace una sesión

Una sesión es una transacción en torno a un proceso de OMSI: OmsiLaunch toma un snapshot de los archivos que va a tocar (de forma predeterminada, los dos bitmaps de splash bajo `GUI\`, más `options.cfg` cuando se solicitan overlays con `/set`), escribe un journal (registro de la transacción) persistente bajo `.omsilaunch\`, aplica los overlays, inicia `Omsi.exe` con el plugin permanente, espera hasta que se entra en el juego (`Running`), mantiene la sesión controlable y, al final, termina OMSI y restaura cada archivo tocado byte por byte. `/new` nunca selecciona un mapa ni un punto de entrada de forma silenciosa: ambos deben indicarse o provenir de un archivo `/spec` o de un perfil de sesión.

<a id="1-plan-nothing-is-started"></a>
## 1. Planificar (no se inicia nada)

Ejecute desde la raíz de OMSI; la instalación toma por defecto el directorio que contiene `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

El plan debe indicar `"IsRunnable": true` (salida `0`). Enumera `TouchedFiles` y `PlannedMutations` para que pueda ver exactamente qué va a superponer la sesión. Corrija cualquier diagnóstico `OL_E_` antes de continuar; no se ha escrito nada.

La especificación de ejemplo incluida en el paquete hace lo mismo con un archivo:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Iniciar con flags explícitos

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Lo que sucede, en orden:

1. Se imprime el plan (`Plan: READY profile=Omsi23004` en el diseño conocido).
2. Recuperación de cualquier journal pendiente anterior, adquisición del lease, snapshot, journal, overlays, comprobación de integridad del plugin, inicio de `Omsi.exe`.
3. Aparece el ícono de la bandeja (`OmsiLaunch is running`, es decir, OmsiLaunch está en ejecución); consulte [bandeja de Windows](../reference/windows-tray.md).
4. Cuando se entra en el juego, el estado `Running` se imprime como JSON (`"State": 14`). El timeout de arranque predeterminado es de 180 s (`/startup-timeout:<1..600>` para cambiarlo).
5. La consola permanece asociada hasta que termina la sesión. No cierre la ventana de la consola para detenerla: use uno de los métodos de detención que se indican más abajo.

Adiciones opcionales para la primera ejecución:

- `/set:graphics.maxFPS=60` (un overlay de `options.cfg`, que se restaura al final);
- `/splash:Unset` para dejar intacta la pantalla de presentación (splash) de OMSI, o `/splash-language:DEU` para elegir el splash administrado localizado;
- `/observe-seconds:30` para detener automáticamente 30 s después de `Running` (útil para una prueba de humo);
- `--json` para obtener salida estructurada.

Los flags que solicitan una fecha, una hora, un año, el clima o un vehículo del jugador (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) se aceptan, pero este build no puede aplicarlos: el plan pasa a `NOT RUNNABLE` con `OL_E_CAPABILITY_UNAVAILABLE`. No los incluya.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Iniciar con un perfil de sesión predefinido

Un perfil de sesión es un archivo YAML en `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` que fija el mapa, el punto de entrada y hasta 99 presets de ajustes (esquema `omsilaunch.session-profile/v1`; referencia completa en [perfiles de sesión](../reference/session-profiles.md)). Cree `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Luego:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Reglas que hay que recordar: el `id` debe ser igual al nombre del directorio; el índice es `1..99`; los flags explícitos que sobrescribirían un campo que pertenece al perfil (`/map`, `/entrypoint-index`, una clave de `/set` que pertenece al preset, los flags de splash cuando el preset tiene `presentation`) se rechazan con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (salida `2`); el bloque `new:` se aplica solo con `/new`; con `/saved:<file.osn>`, el mapa de la situación debe figurar en `compatibility.maps`. El archivo `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` incluido en el paquete ilustra el esquema completo, pero su bloque `new:` establece `date`, `time` y `weather`, que este build no puede aplicar, así que cópielo solo después de quitar esas claves.

<a id="4-control-the-running-session"></a>
## 4. Controlar la sesión en ejecución

Desde una segunda consola en el mismo directorio (sin argumento de instalación):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Estos comandos pasan por el pipe de control local de esta instalación ([control local](../reference/local-control.md)); la salida `4` significa que no hay ningún propietario en ejecución aquí.

<a id="5-stop"></a>
## 5. Detener

Cualquiera de estos métodos finaliza la sesión de la misma manera (se termina OMSI, luego se restaura cada archivo tocado y después se eliminan el journal y la copia de seguridad):

| Método | Notas |
|---|---|
| Ícono de la bandeja → `End session` (finalizar sesión) → confirmar | Disponible tanto en sesiones de `OmsiLaunch.exe` como de `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | Desde otra consola; regresa de inmediato y el propietario completa la restauración. |
| Ctrl+C en la consola del propietario | Solicita la detención; el propietario espera a que termine la restauración antes de salir. |
| `/observe-seconds:<n>` | Detención automática `n` segundos después de `Running`. |
| OMSI termina por sí mismo | El propietario detecta `ProcessExited` y restaura. |

La rutina de cierre propia de OMSI no se ejecuta, por lo que OMSI no reescribe `options.cfg` al salir; esto es intencional para que la restauración sea exacta. Cerrar la ventana de la consola del propietario con el botón X le da a la restauración solo 4 s; si no terminó, el siguiente inicio (u `OmsiLaunch.exe /recover`) la completa a partir del journal. El código de salida del propietario es `0` cuando la sesión terminó en `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Dónde consultar después

| Ubicación | Contenido |
|---|---|
| Salida de la consola / `--json` | Plan, estado `Running`, estado final (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | La traza del host de la sesión (límites de la transacción, inicio del proceso, handoff del plugin, entrada en el juego, restauración). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Resultado de una operación `/runtime:` ejecutada por el propietario. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Eventos del indicador de la bandeja. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` después de un final limpio. `true` significa que quedó un journal; ejecute `OmsiLaunch.exe /recover`. |

Si la sesión no llegó al juego, el estado final incluye el diagnóstico `OL_E_` que falló (por ejemplo `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), el código de salida es `1` y los archivos se restauraron de todos modos. Consulte [códigos de salida](../reference/exit-codes.md), [errores](../reference/errors.md) y [limitaciones conocidas](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Ejecución sin consola

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delega en `OmsiLaunchW.exe` y devuelve `0` de inmediato. La sesión no tiene consola; los fallos aparecen como cuadros de mensaje y el ícono de la bandeja es la única superficie visible. Use `session status`, `events watch` y el directorio de diagnósticos para seguirla. El comportamiento completo del host de Windows está en la [referencia de OmsiLaunchW.exe](../reference/omsilaunchw.md).
