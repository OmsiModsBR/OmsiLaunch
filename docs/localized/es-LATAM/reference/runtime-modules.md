# Módulos de runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> Traducción de la [página original en inglés](../../../reference/runtime-modules.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Un módulo de runtime agrega comportamiento a una sesión administrada de OMSI: una función que se ejecuta con la sesión (por ejemplo, el HUD de un mapa o las optimizaciones de carga), sin pasar a formar parte de OmsiLaunch. El puente lo carga, le da únicamente lo que declara y elimina todo lo que retiene cuando la sesión termina o el módulo falla. Esta página es la referencia normativa de los módulos de runtime tal como los implementan `src/OmsiLaunch.Runtime.Modules` (manifiesto, ajustes, elegibilidad, host del ciclo de vida), el planificador, los programas de sesión del controlador (`ModuleSessionPrograms`, `ModuleSessionProcess`), el entorno de módulos del plugin y los flags de módulos de la [CLI](cli.md).

Estabilidad: `EXPERIMENTAL`. El protocolo de puente 1 se validó en runtime en la ronda B (módulos administrados, activación por contenido, operaciones, `disable-module` y `fail-session`; componentes nativos con hooks de IAT; consulte el [estado de la validación en runtime](../status/runtime-validation-status.md)). El protocolo de puente 2 (ajustes, activación predeterminada, el archivo de ajustes, programas de sesión, conflictos de plugins, avisos, `get_value`) está validado solo offline. Gates offline: los gates `module.*` de `tests/OmsiLaunch.ModuleTests` (manifiesto, integridad, selección y activación, ajustes, el archivo de ajustes, conflictos de plugins, programas de sesión iniciados mediante su job, un fallo inyectado en cada fase bajo cada política de fallos, ciclo de vida de componentes nativos, limpieza, handoff), `plugin-runtime.modules-lifecycle` (un módulo administrado real cargado por el entorno del plugin), `runtime-modules.native-component` (un componente nativo real que engancha una importación y lee valores a través del puente), `runtime-modules.plugin-conflict` y `native-hooks.self-check` (el motor de hooks). El protocolo de puente 3 (Beta 5: operaciones del catálogo en proceso, el tick, la captura de entrada, las entradas de contenido de la comunidad, los pins opcionales con el snapshot tomado al planificar, `module pack`, las opciones del job auxiliar, la versión de OMSI) está validado solo offline: los gates `module.*` de `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` y `ui-input.hook-self-check` de las pruebas de integración del plugin, y `omsi-version.literal` de las pruebas unitarias. La toma del menú de pausa y la captura de entrada no se han observado en una sesión real de OMSI.

La carga es abierta: se puede cargar cualquier módulo instalado bajo `plugins\OmsiLaunch.Modules`, sin importar quién lo haya creado. OmsiLaunch no garantiza lo que un módulo hace dentro de OMSI. Lo que garantiza es el [límite](#security-boundary): los módulos nunca se ejecutan dentro del proceso de OmsiLaunch, y nada de lo que ofrece OmsiLaunch permite que un módulo vaya más allá de los derechos propios de OMSI.

<a id="where-modules-live"></a>
## Dónde residen los módulos

Cada módulo es un directorio bajo la carpeta del plugin permanente:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

El nombre del directorio debe ser igual al `id` del módulo. Los módulos los instala quien los distribuye; una sesión nunca escribe, captura ni restaura archivos allí, y los módulos nunca modifican archivos de la instalación a través de OmsiLaunch (un manifiesto no tiene forma de declarar un overlay de instalación). Los datos escribibles de un módulo residen en su [directorio de datos](#data-directory).

<a id="manifest-modulejson"></a>
## Manifiesto (`module.json`)

Esquema `omsilaunch.runtime-module/v1`. Las claves desconocidas se rechazan, de modo que un manifiesto escrito para un esquema posterior se rechaza en lugar de entenderse a medias. Todas las rutas son relativas al directorio del módulo (las rutas de contenido vinculado son relativas a la raíz de OMSI), usan `\` o `/`, y no pueden contener `..` ni una unidad.

| Clave | Obligatoria | Significado |
| --- | --- | --- |
| `schema` | sí | `omsilaunch.runtime-module/v1`. |
| `id` | sí | Id del módulo: letras minúsculas, dígitos y `-`, de 2 a 48 caracteres, que comience con una letra o un dígito. También es el nombre de su directorio y su espacio de nombres de operaciones. |
| `version` | sí | `major.minor.patch`, opcionalmente `-suffix`. |
| `name` | no | Nombre para mostrar (máximo 128 caracteres). |
| `bridge_protocol` | sí | `{ "min": n, "max": n }`: los protocolos de puente con los que funciona el módulo. Este build atiende los protocolos `1`, `2` y `3`; un módulo se acepta cuando su rango incluye alguno de ellos. |
| `activation` | no | `requested` (predeterminado): se carga cuando la sesión lo solicita. `content`: también se carga cuando la sesión ejecuta el mapa vinculado (requiere `content_binding`). `default` (protocolo 2): se carga en todas las sesiones salvo que esté deshabilitado (consulte [selección de módulos](#choosing-modules-for-a-session)); no puede tener un `content_binding`. |
| `required_capabilities` | no | Capacidades sin las cuales el módulo no puede funcionar (consulte [capacidades](#capabilities)). |
| `optional_capabilities` | no | Capacidades que el módulo usa cuando están disponibles; las comprueba con `IsCapabilityAvailable`. |
| `content_binding` | no | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: el módulo solo se ejecuta en ese mapa, con esos archivos sin cambios. |
| `assets` | no | Archivos que el módulo lee, cada uno `{ "path", "sha256" }`. Desde el protocolo 3 el `sha256` es opcional (consulte [integridad](#integrity)). |
| `components` | sí | Exactamente un componente `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; opcionalmente un componente `native` (`.dll`), componentes `auxiliary` (`.exe`) y, con el protocolo 2, un componente `host` (`.exe`, el [programa de sesión](#session-programs) del módulo). Desde el protocolo 3 el `sha256` de un componente es opcional, y un componente `auxiliary` puede llevar `priority` (`normal`, `below-normal` o `idle`) y `memory_limit_mb` (de 64 a 65536): el job object del programa los aplica desde su primera instrucción (consulte [programas auxiliares](#auxiliary-programs)). |
| `failure_policy` | sí | `fail-session`, `disable-module` o `degrade` (consulte [fallos](#failures)). |
| `operations` | no | Operaciones que el módulo expone mientras la sesión se ejecuta: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | no | Protocolo 2. Los ajustes que acepta el módulo (consulte [ajustes](#settings)). |
| `conflicts` | no | Protocolo 2. `{ "plugins": [ "<file>.dll" ] }`: DLL de plugins de OMSI que no deben ejecutarse junto con el módulo (consulte [conflictos](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | no | Protocolo 3. Operaciones del catálogo público de capacidades que el módulo ejecuta en proceso, por ejemplo `["time.read", "camera.set"]` (consulte [operaciones en proceso](#operations-in-process)). Requiere la capacidad `bridge.runtime`. |
| `tick_ms` | no | Protocolo 3. El módulo quiere un callback periódico cada `tick_ms` milisegundos: un múltiplo de 50, de 50 a 1000 (consulte [tick](#tick)). Requiere la capacidad `bridge.tick`. |
| `content_inputs` | no | Protocolo 3. Contenido de la comunidad que el módulo lee, localizado por el puente: `[ { "kind": "realmap-package", "required": false } ]` (consulte [contenido de la comunidad](#community-content)). |
| `timeouts` | no | Presupuestos en milisegundos: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (5000 por defecto cada uno, máximo 30000); con el protocolo 2 también `prepare_ms` (120000 por defecto, máximo 1800000) y `host_stop_ms` (10000 por defecto, máximo 60000) para el programa de sesión. Las operaciones tienen 2000 ms por defecto. |
| `diagnostics` | no | `{ "trace": true\|false }`. |

Un manifiesto que usa una función del protocolo 2 (`settings`, `conflicts`, activación `default`, un componente `host`, `prepare_ms` o `host_stop_ms`) debe declarar `bridge_protocol.min` 2. Un manifiesto que usa una función del protocolo 3 (`runtime_operations`, `tick_ms`, `content_inputs`, una capacidad `bridge.runtime`, `bridge.tick` o `bridge.input`, un `priority` o `memory_limit_mb` auxiliar, o un componente o asset sin `sha256`) debe declarar `bridge_protocol.min` 3.

Cada archivo nombrado en `components` y `assets` puede fijarse por su SHA-256 (obligatorio antes del protocolo 3, opcional desde él: consulte [integridad](#integrity)). `module.json` no puede fijarse a sí mismo.

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## Integridad

Cada archivo que nombra el manifiesto debe existir y no debe alcanzarse a través de una junction ni de un vínculo simbólico, y cada archivo que el autor fijó debe tener el SHA-256 fijado; de lo contrario, el módulo se rechaza con `OL_E_MODULE_INTEGRITY`. Esto detecta un módulo corrupto o actualizado a medias. No es una decisión de confianza: un módulo no se contrasta con ninguna lista, firma ni editor.

Desde el protocolo 3 una fijación es opcional, de modo que un autor nunca tiene que mantener hashes a mano, y el plan siempre toma un **snapshot**: un SHA-256 sobre cada archivo que nombra el manifiesto y su contenido, registrado en el plan (`PlannedModule.SnapshotSha256`) y transportado al plugin en el handoff de inicio (versión de wire 8). El snapshot es lo que garantiza que los archivos que evaluó el plan son los archivos que se ejecutan: el plugin carga un módulo solo cuando su `module.json` tiene el SHA-256 que evaluó el plan y sus archivos todavía tienen el contenido del snapshot, y el controlador y el puente vuelven a comparar el snapshot justo antes de que arranque un programa de sesión, un componente nativo o un programa auxiliar. Un módulo cuyos archivos cambiaron después de la planificación no se carga (`OL_E_MODULE_INTEGRITY`, "the module's files changed after planning"). El snapshot cubre a todos los módulos, con fijaciones o sin ellas; una fijación permite además que quien publica un módulo detecte una copia corrupta o actualizada a medias en cuanto se descubre.

La comprobación se repite donde arranca el código del módulo: el plugin carga un módulo solo si su `module.json` todavía tiene el SHA-256 que evaluó el plan y todas las fijaciones y el snapshot siguen cumpliéndose, y el controlador inicia un programa de sesión solo después de verificar la fijación de ese programa y el snapshot.

<a id="packing-a-module"></a>
### Empaquetar un módulo

`OmsiLaunch.exe /pack-module:<module directory>` escribe las fijaciones de los archivos que nombra un `module.json` (sus componentes y assets), de modo que un autor nunca edita un SHA-256 a mano. Reescribe solo los valores `sha256` (el resto del manifiesto se conserva; el archivo se vuelve a indentar), vuelve a leer el resultado con el analizador de manifiestos antes de escribir nada e informa de cada archivo (envoltorio `module.pack`: `module`, `directory`, `written`, `intact`, `files` con `path`, `sha256`, `pinned`, `mismatch`, `written`). Trabaja sobre el directorio que se le indica y nunca toca una instalación. `/verify-module:<module directory>` informa lo mismo sin escribir (`module.verify`); termina con `7` cuando un archivo fijado no coincide con su fijación (un archivo sin fijación no es una discrepancia). Ambos terminan con `2` para un manifiesto que no se puede analizar o un archivo que falta.

<a id="security-boundary"></a>
## Límite de seguridad

OmsiLaunch no controla lo que un módulo hace dentro de OMSI: sus componentes administrados y nativos se ejecutan en el proceso de OMSI con los mismos derechos que cualquier plugin de OMSI. OmsiLaunch garantiza lo siguiente:

1. El proceso de OmsiLaunch nunca carga código de módulos. El código del módulo se ejecuta dentro de OMSI, en un programa auxiliar iniciado desde dentro de OMSI, o en el programa de sesión del módulo.
2. Los programas que OmsiLaunch inicia para los módulos son archivos del propio directorio del módulo, verificados contra su fijación e iniciados con una línea de comandos que fija OmsiLaunch. Un programa de sesión se ejecuta en un objeto job que lo finaliza junto con la sesión, no permite un segundo proceso (no puede iniciar otros programas) y rechaza cambios en los parámetros del sistema, la configuración de pantalla, el escritorio y el cierre de sesión o el apagado. Solo hereda sus dos handles de pipe.
3. Todo lo que un módulo declara permanece dentro de su directorio, y sus datos escribibles dentro de `.omsilaunch\module-data\<id>`.
4. Los canales de OmsiLaunch (control local, handoff de arranque, telemetría, comandos de runtime) permanecen restringidos al usuario actual y vinculados a la sesión; ninguno permite que un llamador inicie un programa, lea o escriba archivos fuera de la instalación, ni modifique ajustes del sistema.
5. OmsiLaunch nunca eleva privilegios. Un módulo se ejecuta con los derechos del usuario que inició la sesión.
6. El texto que proviene de un módulo (avisos, eventos, valores) se trata como datos: acotado, sin caracteres de control, nunca se ejecuta, se abre ni se sigue como un vínculo.

<a id="capabilities"></a>
## Capacidades

`required_capabilities` y `optional_capabilities` nombran una capacidad nativa del build en ejecución (la columna de capacidad nativa del [catálogo de capacidades](capabilities.md#capability-catalog), por ejemplo `time.read` o `d3d.texture`) o una función del puente:

| Función del puente | Lo que obtiene el módulo |
| --- | --- |
| `bridge.events` | `Emit`: eventos de runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: threads en segundo plano con seguimiento. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: procesos auxiliares vinculados a la sesión. |
| `bridge.hooks.iat` | Componente nativo: hooks sobre importaciones de `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Componente nativo: hooks sobre métodos del dispositivo Direct3D 9 activo (`d3d9:device.<method>`). Requiere la capacidad nativa `d3d.texture`. |
| `bridge.runtime` | Protocolo 3. `IRuntimeModuleContext.Runtime` y el `run_operation` nativo: las operaciones del catálogo que el manifiesto enumera en `runtime_operations`. |
| `bridge.tick` | Protocolo 3. Un callback periódico: `ITickingModule.Tick` y la exportación nativa `omsilaunch_module_native_v1_tick`. |
| `bridge.input` | Protocolo 3. `IRuntimeModuleContext.Input`: captura de los mensajes de teclado y mouse del juego para el menú propio del módulo. Requiere la capacidad nativa `ui.input.capture`. |

Un nombre que no sea ninguno de los dos se rechaza en la planificación (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). En un build de OMSI 2 sin un diseño conocido, una capacidad nativa que el archivo ejecutable no resuelve se decide en proceso; si no se resuelve allí, el módulo no se carga.

<a id="choosing-modules-for-a-session"></a>
## Selección de módulos para una sesión

Un módulo se incorpora a una sesión de una de cuatro maneras; `PlannedModule.Activation` indica cuál:

| Activación | Cuándo |
| --- | --- |
| `requested` | La sesión lo solicita: `LaunchSpec.Modules.Enable`, `/module:<id>`, o `enabled: true` en el preset de perfil de sesión seleccionado. |
| `settings-file` | `enabled=true` en su sección del [archivo de ajustes](#the-settings-file). |
| `content` | Su activación es `content` y la sesión ejecuta su mapa vinculado. |
| `default` | Su activación es `default`. |

Un módulo queda fuera de la sesión por `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` en el preset, o `enabled=false` en el archivo de ajustes (diagnóstico del plan `module.disabled`). Una solicitud explícita prevalece sobre el archivo de ajustes. Habilitar y deshabilitar el mismo módulo en una misma solicitud es `OL_E_MODULE_SETTING_INVALID`.

Reglas de planificación: un módulo solicitado que no puede cargarse hace que el plan no sea ejecutable, con su código `OL_E_MODULE_*`. Un módulo que se incorporó por sí mismo (archivo de ajustes, contenido, predeterminado) y no puede cargarse no detiene la sesión: el plan informa `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Ajustes

Un módulo declara sus ajustes en `settings`; cada entrada:

| Clave | Obligatoria | Significado |
| --- | --- | --- |
| `key` | sí | `[a-z][a-z0-9_]{0,31}`, distinta de `enabled`. |
| `type` | sí | `boolean`, `integer`, `choice` o `text`. |
| `default` | sí | El valor cuando nada más lo establece: un booleano, un entero, una cadena, o `"auto"` cuando `auto` es true. |
| `auto` | no | `true`: el valor `auto` se acepta y se pasa al módulo, que decide en tiempo de ejecución. |
| `min`, `max` | no | Límites enteros. |
| `choices` | para `choice` | Los valores aceptados (`[a-z0-9][a-z0-9_.-]{0,31}`, distintos de `auto`). |
| `max_length` | no | Límite de longitud del texto (1..4096, 256 por defecto). Los caracteres de control se rechazan. |
| `flag` | no | Un flag de línea de comandos para este ajuste, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, o `/<flag>` solo para `true` en un booleano. |
| `description` | no | Máximo 256 caracteres. |

Los valores se normalizan: los booleanos aceptan `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; las opciones se comparan sin distinguir mayúsculas de minúsculas; los enteros se comprueban contra sus límites.

El valor efectivo de cada ajuste es, por orden:

1. la solicitud: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, un flag propio del módulo, o `modules.<id>.settings` en el preset de perfil de sesión seleccionado. Como en todos los demás campos de un preset, la línea de comandos tampoco puede establecer una clave que el preset ya establece (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. el [archivo de ajustes](#the-settings-file);
3. el valor predeterminado del manifiesto.

Un valor solicitado no válido, o una clave solicitada que el módulo no declara, hace que el plan no sea ejecutable (`OL_E_MODULE_SETTING_INVALID`). Una entrada del archivo de ajustes que no puede usarse se ignora con `OL_W_MODULE_SETTING_IGNORED`, igual que los ajustes solicitados para un módulo que la sesión no carga. El plan enumera cada valor efectivo con su origen (`request`, `settings-file` o `default`) en `PlannedModule.Settings`.

El módulo recibe cada clave declarada con su valor efectivo: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` en un componente nativo, y el objeto `settings` del contexto de un programa de sesión.

<a id="the-settings-file"></a>
### El archivo de ajustes

`<OMSI root>\.omsilaunch\modules.ini`, junto a los perfiles de sesión. OmsiLaunch solo lo lee.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Las secciones son ids de módulo; las líneas `key=value` establecen ajustes, y `enabled` activa o desactiva el módulo para todas las sesiones; las líneas que comienzan con `;` o `#` son comentarios. El archivo puede tener como máximo 256 KiB. Todo lo que no pueda usar (una sección que no es un id de módulo, una línea fuera de una sección, una clave desconocida, un valor no válido) se informa con `OL_W_MODULE_SETTING_IGNORED` y se ignora.

<a id="session-programs"></a>
## Programas de sesión

Un módulo puede declarar un componente `host`: su programa de sesión, un ejecutable de su propio directorio. Cuando el módulo está en el plan, el controlador:

1. verifica la fijación del programa y crea el directorio de datos del módulo;
2. lo inicia antes que OMSI, desde el directorio del módulo, con la línea de comandos `"<program>" --omsilaunch-session <session id>`, suspendido hasta que esté dentro de su job (consulte el [límite de seguridad](#security-boundary));
3. escribe el contexto como primera línea en su entrada estándar;
4. espera hasta que el programa escriba `{"type":"ready"}`, como máximo `prepare_ms`; solo entonces inicia OMSI;
5. una vez creado OMSI, escribe `{"type":"omsi-started","pid":<process id>}`;
6. al final de la sesión, después de que OMSI haya salido y antes de restaurar cualquier archivo, escribe `{"type":"stop"}`, espera como máximo `host_stop_ms` y luego finaliza el programa.

Ambas direcciones usan JSON UTF-8, un objeto por línea. El contexto:

| Campo | Significado |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identidad. |
| `installation_root`, `module_directory`, `data_directory` | Rutas completas. |
| `channel` | El named pipe que el programa puede atender para los componentes del módulo en OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. El programa lo crea, restringido al usuario actual, y comprueba que el cliente sea el proceso de OMSI. Los componentes lo leen con `get_value("session_channel")` o `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | Los ajustes efectivos del módulo. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` de la sesión. |
| `session_files` | Archivos relativos a la instalación que posee la transacción de la sesión. El programa no debe mantenerlos abiertos ni asignados en memoria: se restauran cuando la sesión termina. |

Lo que el programa puede escribir además de `ready`:

| Línea | Efecto |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Evento de runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Evento de runtime `module.<id>.notice` (consulte [avisos](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Diagnóstico de sesión con ese código catalogado (`OL_E_MODULE_OPERATION_FAILED` en caso contrario). |

Las demás líneas, las líneas de más de 64 KiB y todo lo que venga después de 10,000 líneas se ignoran. El controlador registra `module.<id>.host.started`, `host.ready` (con `prepare_ms`), `host.failed`, `host.exited` y `host.killed`.

Un programa que no está listo dentro de `prepare_ms`, que termina antes de estar listo o cuya fijación no se cumple, hace fallar la preparación de su módulo: bajo `fail-session` la sesión no se inicia (`OL_E_MODULE_SESSION_FAILED`); bajo `disable-module` el programa se finaliza y el módulo queda fuera de OMSI; bajo `degrade` el módulo permanece (y el programa sigue ejecutándose si aún puede). El `StartupTimeoutSeconds` de la sesión cuenta desde que se inicia OMSI, de modo que la preparación no lo consume.

<a id="conflicts-with-omsi-plugins"></a>
## Conflictos con plugins de OMSI

`conflicts.plugins` nombra DLL de plugins de OMSI que no deben ejecutarse junto con el módulo, por ejemplo un plugin anterior que instala los mismos hooks. En la planificación, OmsiLaunch lee la sección `[dll]` de cada `plugins\*.opl`; si alguno carga una DLL en conflicto, el módulo se rechaza con `OL_E_MODULE_CONFLICT` y el motivo nombra el `.opl`. Dentro de OMSI, un módulo cuya DLL en conflicto se carga de todos modos no se carga. OmsiLaunch nunca modifica los archivos de otro plugin; desactivarlo es decisión del usuario.

<a id="notices"></a>
## Avisos

Un evento de módulo llamado `notice` (de `Emit("notice", ...)`, de un `emit` de un componente nativo, o de una línea de aviso de un programa de sesión) lo muestra el ícono de la bandeja de OmsiLaunch como un globo cuyo título nombra el módulo. Solo se conservan `title` (máximo 63 caracteres) y `text` (máximo 255 caracteres), se eliminan los caracteres de control y nada de su contenido se abre ni se ejecuta. Un programa de sesión que deba informar al usuario antes de que se inicie OMSI (cuando la bandeja aún no se muestra) usa su propia ventana.

<a id="data-directory"></a>
## Directorio de datos

`<OMSI root>\.omsilaunch\module-data\<id>\` es el directorio escribible del módulo para esta instalación: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")`, y el `data_directory` del programa de sesión. Se crea en el primer uso. No forma parte de la transacción de la sesión: lo que un módulo escribe allí permanece después de la sesión. `/clear-module-data:<id>` lo elimina (y nada más); se rechaza mientras una sesión posea la instalación, y un directorio alcanzado a través de un vínculo se elimina como vínculo, sin seguirlo nunca.

<a id="lifecycle"></a>
## Ciclo de vida

| Paso | Dónde | Qué ocurre |
| --- | --- | --- |
| 1. Descubrimiento | controlador | Se lee `plugins\OmsiLaunch.Modules\*`: manifiesto e integridad (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Elegibilidad | controlador, en la planificación | Se evalúan los módulos que [se incorporan a la sesión](#choosing-modules-for-a-session): utilizable, protocolo de puente, capacidades, vínculo de contenido, conflictos de plugins, ajustes. El resultado es `SessionPlan.PlannedModules`. |
| 3. Preparación | controlador, al iniciar | Se inician y se esperan los [programas de sesión](#session-programs) de los módulos. Luego los módulos elegibles viajan al plugin en el handoff de arranque (versión 6): sus directorios, el SHA-256 de cada `module.json` evaluado y sus ajustes efectivos. |
| 4. Armado | plugin, antes de que el mundo se inicie | Cada módulo se vuelve a verificar y se carga (componente administrado en su propio contexto de carga). Su componente nativo, si lo hay, se carga y se inicia primero; luego se llama a `Arm(context)`. |
| 5. Gameplay listo | plugin | Después de `gameplay.entered`, cuando un módulo cargado declara `bridge.hooks.d3d9`, el puente comienza a observar el dispositivo Direct3D (`d3d.ready`); luego llama a `gameplay_ready` del componente nativo y a `GameplayReady()`. En una sesión NEW_MAP, el mapa vinculado se comprueba de nuevo contra el mapa que cargó el inicio. |
| 6. Runtime | plugin | Las operaciones `module.<id>.<operation>` llegan a `Execute`. Un módulo con `tick_ms` recibe ticks (consulte [tick](#tick)); un módulo con `runtime_operations` las ejecuta en proceso (consulte [operaciones en proceso](#operations-in-process)). |
| 7. Detención | plugin, luego controlador | Cuando la sesión termina (o el módulo falla), se llama a `Stop()`. Después de que OMSI haya salido, se detienen los programas de sesión. |
| 8. Limpieza | plugin | Todo lo que el módulo registró se libera en orden inverso y se verifica que haya desaparecido; el componente nativo se detiene al final y sus hooks se eliminan; su DLL permanece cargada, inerte, hasta que OMSI termine (un thread aún puede estar regresando a través de un detour). |

Cada callback en proceso se ejecuta en el thread de la interfaz de OMSI, igual que las operaciones de runtime propias del producto. El puente mide cada callback contra su presupuesto: un callback que regresa tarde hace fallar su fase (`OL_E_MODULE_PHASE_TIMEOUT`). El puente no puede interrumpir un callback que nunca regresa; un módulo así detiene OMSI, y la sesión termina en su timeout de arranque o con `session stop`, como cualquier otra sesión.

<a id="failures"></a>
## Fallos

Un fallo es una excepción o un exceso de tiempo en cualquier fase u operación, una capacidad no resuelta en proceso, un vínculo de contenido que no se cumple en proceso, un plugin en conflicto cargado en OMSI, o un programa de sesión que no llega a estar listo. La `failure_policy` del módulo decide lo que sigue. En todos los casos se liberan los recursos del módulo. Un tick que lanza una excepción, devuelve un valor distinto de cero o excede su intervalo es un fallo igual que el de una operación.

| Política | Fallo en preparación, carga, armado o gameplay listo | Fallo en una operación | Fallo en la detención |
| --- | --- | --- | --- |
| `fail-session` | Módulo liberado; `module.session-failure`; la sesión falla con `OL_E_MODULE_SESSION_FAILED`, OMSI se detiene y los archivos se restauran. Un fallo antes de que el mundo se inicie impide el inicio. | Igual. | Se informa (`module.failed`); la sesión continúa hasta su fin. |
| `disable-module` | Módulo liberado y deshabilitado; la sesión continúa. | Igual. | Se informa; módulo liberado. |
| `degrade` | Como `disable-module`, salvo que el fallo de un programa de sesión deja el módulo en la sesión. | La llamada que falla devuelve su código; el módulo permanece activo; `module.degraded` se informa una vez por código. | Se informa; módulo liberado. |

Un módulo deshabilitado o fallido responde a sus operaciones con `OL_E_MODULE_DISABLED`. Los fallos de módulos aparecen como diagnósticos de sesión (`SessionStatus.Diagnostics`) con su código `OL_E_MODULE_*` y como eventos de runtime.

<a id="cleanup"></a>
## Limpieza

Lo que un módulo adquiere a través de su contexto tiene seguimiento: los recursos registrados con `Track`, los threads iniciados con `RunBackground`, los procesos iniciados con `StartAuxiliaryProcess`. En la detención o el fallo, el puente cancela `Stopping` y luego los libera en orden inverso. Los procesos auxiliares también se colocan en un objeto job con finalización al cerrar, de modo que terminan con OMSI incluso si OMSI se finaliza a la fuerza; el job de un programa de sesión lo finaliza junto con la sesión de OmsiLaunch, incluso si el propio OmsiLaunch se finaliza a la fuerza. Un recurso que sigue activo después de la liberación (un proceso que no salió, un thread que no terminó) se informa como `OL_E_MODULE_CLEANUP_INCOMPLETE`. Los threads o procesos que un módulo crea sin su contexto no tienen seguimiento.

Tras una caída no hay nada que recuperar para los módulos: no retienen archivos de la instalación, y su memoria, sus threads y los procesos vinculados al job terminan con el proceso de OMSI o el proceso de OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Componentes nativos y hooks

Un componente nativo es una DLL de 32 bits que implementa la interfaz C de `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Exportación | Se llama |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | En el armado, antes del `Arm` administrado. Un valor distinto de cero hace fallar la fase. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Opcional; después de que se entró al gameplay. |
| `void omsilaunch_module_native_v1_stop(void)` | En la detención o el fallo, antes de que se eliminen sus hooks. Después, la tabla del puente rechaza toda llamada y la DLL permanece cargada, inerte, hasta que OMSI termine. |

La tabla del puente le da al componente `request_hook`, `emit` (eventos de runtime `module.<id>.<name>` con un objeto JSON de cadenas) y, desde el protocolo 2, `get_value`. Un componente lee el `size` de la tabla antes de usar `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copia un valor como UTF-8 con un cero final y establece `*length` en los bytes necesarios sin él; devuelve `0`, `1` (argumento), `2` (nombre desconocido), `11` (después de la detención) o `12` (búfer demasiado pequeño). Desde el protocolo 3 la tabla también contiene `run_operation` (consulte [operaciones en proceso](#operations-in-process)) y `d3d_is_game_device(context, device)`: indica si un puntero de dispositivo que recibió un hook de Direct3D es el dispositivo propio del juego (`1`) u otro (`0`; el formulario de selección de vehículo crea un segundo dispositivo con la misma tabla de métodos, de modo que un detour de `Present` se llama para ambos y debería dibujar solo para el del juego), `10` cuando todavía no se observa ningún dispositivo, `11` cuando `bridge.hooks.d3d9` no está declarado o después de la detención. El puntero solo se compara, nunca se usa, y el puente nunca entrega uno. Un módulo que mantiene recursos de Direct3D en el pool predeterminado engancha él mismo `d3d9:device.Reset` (lo que hace antes de llamar al original es "dispositivo perdido"; después de que devuelve éxito es "dispositivo restaurado"). Un componente comprueba el `size` de la tabla antes de usar un miembro del protocolo 3 (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Dos exportaciones opcionales completan el componente: `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (consulte [tick](#tick)) y `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, la forma en que el componente administrado llama al nativo mediante `IRuntimeModuleContext.NativeComponent.Execute` (JSON de entrada, JSON de salida, la misma convención de búfer que `run_operation`). Los nombres que responde `get_value`:

| Nombre | Valor |
| --- | --- |
| `setting.<key>` | El valor efectivo de un ajuste declarado. |
| `data_directory` | El [directorio de datos](#data-directory) del módulo. |
| `module_directory` | El directorio propio del módulo. |
| `session_id` | El id de la sesión. |
| `module_version` | La versión del manifiesto. |
| `product_version` | La versión del producto OmsiLaunch. |
| `session_channel` | El pipe del [programa de sesión](#session-programs); desconocido para un módulo que no tiene uno. |
| `omsi_version` | La versión de producto de OMSI que el juego informa sobre sí mismo (protocolo 3; consulte [la versión de OMSI](#the-omsi-version)). |

Los hooks se solicitan únicamente mediante un destino simbólico, durante `start` o `gameplay_ready`, para un destino cuya capacidad declara el manifiesto:

| Destino | Engancha | Capacidad |
| --- | --- | --- |
| `iat:<dll>!<function>` | Una importación de `Omsi.exe`, por ejemplo `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` o `EndScene` del dispositivo Direct3D 9 activo (solicítelo en `gameplay_ready`, cuando el dispositivo existe). | `bridge.hooks.d3d9` |

`request_hook` devuelve en `*original` la función para continuar la cadena (un detour debe llamarla). Su resultado: `0` éxito, `1` argumento, `2` destino desconocido, `3` el sitio no contiene lo que debería, `4` el sitio está enganchado por alguien ajeno al puente, `5` protección de memoria, `6` registro lleno, `7` orden, `8` memoria, `9` un thread estaba ejecutando los bytes que se iban a parchear, `10` aún no hay dispositivo Direct3D, `11` no permitido ahora o capacidad no declarada.

Todo hook que el puente instala, para el producto o para un módulo, se registra en un único registro con su propietario (`module:<id>` para un módulo):

- Un hook se instala únicamente sobre el valor que el sitio debería contener (la función importada; una función de un módulo llamado `d3d9.dll`, el runtime del sistema o un wrapper), o apilado sobre el hook superior propio del registro en ese sitio. Un sitio ya modificado por otro se rechaza (`4`), nunca se sobrescribe.
- Los hooks de un mismo sitio se eliminan en orden último en entrar, primero en salir; un hook que tiene otro encima permanece hasta que ese se elimina.
- El hook `Reset` de Direct3D propio del producto está en el registro, de modo que los hooks de dispositivo de los módulos se apilan sobre él.
- En la limpieza, el puente elimina todos los hooks del módulo después de que `stop` regrese. Si alguno no puede eliminarse (otro cambió el sitio), la limpieza informa `OL_E_MODULE_CLEANUP_INCOMPLETE`. La DLL de un componente nativo nunca se descarga mientras OMSI se ejecuta, de modo que un detour por el que aún se está regresando sigue siendo válido.

El motor también admite hooks sobre prólogos de funciones con un trampolín (los demás threads se suspenden mientras cambian los bytes, y primero se comprueban los bytes exactos esperados), pero todavía no se ofrece ningún destino de prólogo a los módulos: tales destinos provienen del catálogo de destinos del build con bytes verificados.

<a id="operations"></a>
## Operaciones

Una operación de módulo es `module.<id>.<operation>`. Existe solo para una sesión cuyo plan cargó el módulo (`PlannedModule.Operations`); de lo contrario, `ExecuteRuntimeAsync` responde `OL_E_MODULE_NOT_LOADED` u `OL_E_MODULE_OPERATION_UNKNOWN`. Los argumentos obligatorios se comprueban y los argumentos no declarados se rechazan (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Un fallo tipado de un módulo conserva su código cuando es un código público catalogado; cualquier otro código se convierte en `OL_E_MODULE_OPERATION_FAILED` con el código del módulo en `Values["module_code"]`. Las claves de resultado que siguen la convención de nombres interna se eliminan, como en toda operación de runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## Programas auxiliares

Un módulo inicia uno de sus componentes `auxiliary` (un `.exe` fijado de su propio directorio) con `StartAuxiliaryProcess`. Desde el protocolo 3 el puente crea el programa **suspendido**, lo coloca en el job object de la sesión y solo entonces lo reanuda, de modo que un hijo que inicie de inmediato no puede ejecutarse fuera del job. El job finaliza el programa y todo lo que este inició cuando el módulo se detiene o OMSI termina (kill on close) y aplica el `priority` (`below-normal`, `idle`) y el `memory_limit_mb` del manifiesto desde la primera instrucción del programa. A diferencia de un [programa de sesión](#session-programs), el job de un programa auxiliar permite procesos hijo (un programa que incrusta una vista web los necesita); todos ellos están dentro del job. Antes de iniciarse, el programa se verifica de nuevo: su fijación cuando la tiene, y siempre el snapshot del plan.

<a id="operations-in-process"></a>
## Operaciones en proceso

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (administrado) y `run_operation` (nativo) ejecutan en proceso una operación del [catálogo público de capacidades](capabilities.md#public-runtime-operations), en el thread de la interfaz de OMSI, con el mismo registro, los mismos resultados `OL_E_*` tipados, las mismas comprobaciones de argumentos y la misma política de mutación que el canal de control de runtime, y sin el canal. Solo se permiten las operaciones que el manifiesto enumera en `runtime_operations`: cualquier otra se responde con `OL_E_MODULE_OPERATION_NOT_DECLARED` antes de llegar a OMSI, y las operaciones `internal.*` y `module.*` nunca pueden enumerarse. Una operación cuya capacidad nativa el build no resuelve responde `OL_E_CAPABILITY_UNRESOLVED`, como en todas partes.

La lista es información de transparencia y de planificación, no un límite de seguridad: un módulo ya se ejecuta dentro de OMSI con los derechos del usuario. El plan la muestra (`PlannedModule.RuntimeOperations`), y una operación que cambia el estado de OMSI lleva allí su política de mutación, por ejemplo `camera.set (omsi-state)`. Un módulo puede ejecutar operaciones desde `Arm` (puede que el mundo todavía no exista; la operación responde entonces su propio error tipado). Los resultados están acotados (1 MiB); un resultado mayor es `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Después de que el módulo se detuvo, toda llamada responde `OL_E_MODULE_DISABLED`.

El `run_operation(context, operation, arguments_json, buffer, capacity, length)` nativo recibe el id del catálogo y un objeto JSON de cadenas (o null) y escribe `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. Devuelve `0` cuando respondió (el fallo propio de la operación va dentro del JSON), `1` para un argumento, `11` después de la detención y `12` cuando el búfer es demasiado pequeño (`*length` es el tamaño necesario).

## Tick

Un módulo que declara `tick_ms` y la capacidad `bridge.tick` recibe una llamada aproximadamente cada `tick_ms` milisegundos, en el thread de la interfaz de OMSI, desde el timer de interfaz de 50 ms del plugin: `ITickingModule.Tick(TimeSpan elapsed)` en el componente administrado y la exportación nativa opcional `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. El intervalo es un múltiplo de 50 entre 50 y 1000. Un tick no es por frame: un módulo que dibuja engancha el método `Present` de Direct3D y usa el tick para leer datos. Los ticks comienzan después de `gameplay.entered` y terminan con el módulo. Un tick que lanza una excepción, devuelve un valor distinto de cero (nativo) o tarda más que su intervalo es un fallo de ese módulo: bajo `degrade` se informa una vez por código (`module.degraded`) y el módulo sigue ejecutándose (los ticks perdidos se omiten, nunca se encolan); bajo `disable-module` y `fail-session` se aplica la política. Un bucle modal anidado de OMSI mantiene activo el timer de interfaz, por lo que un tick nunca se reentra a sí mismo.

<a id="community-content"></a>
## Contenido de la comunidad

`content_inputs` declara contenido que el módulo lee y que su autor no puede conocer cuando se compila el módulo, porque la comunidad lo crea, para un mapa, después del módulo. No hay un SHA-256 que fijar; el puente solo lo localiza y comprueba que leerlo es seguro. El único tipo actual es `realmap-package`: la carpeta `openomsi-realmap` de un mapa, `<OMSI root>\maps\<folder>\openomsi-realmap`, escrita por las herramientas de paquetes de mapas de openOMSI.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` responde si el mapa tiene un paquete (no tenerlo no es un error) y, cuando lo tiene, su directorio, la cantidad de archivos, el tamaño total y un `Fingerprint`. La huella es un detector de cambios sobre la ruta relativa, el tamaño y la hora de escritura de cada archivo: úsela como clave de caché; nunca es una comprobación de integridad ni una decisión de confianza. El paquete debe estar dentro de la instalación, ninguna parte de él puede ser un punto de reanálisis y está acotado (50,000 archivos, 4 GiB); de lo contrario la respuesta es `OL_E_MODULE_CONTENT_INPUT_INVALID`. Un módulo que no declaró el tipo recibe `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. El paquete son datos creados por terceros: léalo en código administrado, trate cada valor como no confiable y degrade por recurso. Una entrada `required: true` hace que el plan rechace el módulo (`OL_E_MODULE_CONTENT_BINDING`) cuando el mapa de la sesión no tiene paquete.

<a id="input-capture"></a>
## Captura de entrada

Un módulo que reemplaza el menú de pausa de OMSI por uno propio necesita el teclado y el mouse del juego. OMSI lee sus teclas de simulación mediante DirectInput y su mouse mediante mensajes de ventana a la ventana de vista del formulario principal; mientras su propia pausa está activa (el byte de pausa nativo), OMSI mismo retiene la simulación, el renderizado, los clics 3D y los comandos DirectInput. `IRuntimeModuleContext.Input` (capacidad `bridge.input`, capacidad nativa `ui.input.capture`) agrega el resto: `Capture(true)` instala un hook `WH_GETMESSAGE` local al thread en el thread de la interfaz, y los mensajes de teclado y mouse que pertenecen al árbol de ventanas del juego (el formulario principal, sus hijos y el formulario de pausa nativo oculto) se registran como simples valores `ModuleInputEvent` (tipo, tecla virtual, coordenadas de cliente de la ventana de vista, botón, delta de la rueda, modificadores) y se quitan de la cola, de modo que ni OMSI ni la VCL actúan sobre ellos. `Drain()` devuelve los eventos (se conservan como máximo 256). No se hace subclassing de nada, no se engancha ninguna función de estado de teclas de user32 y no se envía nada.

Un módulo captura a la vez. La captura se libera cuando el módulo se detiene o falla, haga lo que haga `Stop`. Los diálogos modales nativos (guardar, opciones, juego nuevo) necesitan entrada real: libere la captura antes de pedir uno. La mecánica del hook se validó offline con una ventana sustituta (`ui-input.hook-self-check`); su comportamiento frente a la ventana real del juego todavía no se observó.

<a id="the-omsi-version"></a>
## La versión de OMSI

`IRuntimeModuleContext.OmsiVersion` y `get_value("omsi_version")` entregan la versión de producto de OMSI que el juego informa sobre sí mismo, por ejemplo `2.3.004`: el texto que OMSI escribe después de `Version:` en la línea 7 de su `logfile.txt`, leído de la imagen en ejecución. **Nunca** es la versión de archivo PE del ejecutable (el build de referencia lleva `FileVersion` `2.2.032`). Cuando la imagen no tiene un único texto de ese tipo, se usa la versión del perfil de build si se conoce la identidad del código, y en caso contrario `unknown`. El hash del ejecutable, el tamaño de su código, las direcciones y el nombre del perfil no se exponen. Use la versión para texto y para elegir comportamientos; las capacidades (`IsCapabilityAvailable`) siguen siendo la fuente de verdad sobre lo que funciona.

<a id="writing-a-module-contracts"></a>
## Escribir un módulo (contratos)

El componente administrado referencia `OmsiLaunch.Runtime.Modules` y `OmsiLaunch.Api` e implementa `IRuntimeModule`. El puente vincula estas dos referencias a sus propios ensamblados cargados (sea cual sea el contexto de carga en el que se ejecute el plugin), de modo que una copia distribuida con el módulo se ignora:

| Miembro | Se llama |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Paso 4. Adquiera recursos a través del contexto. |
| `void GameplayReady()` | Paso 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Paso 6, para cada operación declarada (sin el prefijo `module.<id>.`). Lance `ModuleOperationException(code, message)` para un fallo tipado. |
| `void Stop()` | Paso 7. |

`IRuntimeModuleContext` ofrece `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (un token de cancelación), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess`, y desde el protocolo 3 `Runtime` (consulte [operaciones en proceso](#operations-in-process)), `Content` (consulte [contenido de la comunidad](#community-content)), `Input` (consulte [captura de entrada](#input-capture)) y `NativeComponent` (el componente nativo cargado, o null). Un componente administrado que quiere ticks también implementa `ITickingModule`. Nada del contexto expone direcciones, el hash del ejecutable ni su disposición de código. El componente debe poder cargarse en el plugin de 32 bits (AnyCPU o x86, .NET 6); se ignora una copia de `OmsiLaunch.Runtime.Modules` o de `OmsiLaunch.Api` junto a él. Fije cada archivo que cargan los programas del módulo (para un programa .NET: su `.dll` junto al `.exe`), de modo que la integridad los cubra.

<a id="known-limitations"></a>
## Limitaciones conocidas

- Los componentes nativos pueden enganchar importaciones de `Omsi.exe` y métodos del dispositivo Direct3D; los hooks sobre prólogos de funciones de OMSI y sitios de llamada todavía no se ofrecen a los módulos.
- La toma del menú de pausa (`ui.pause.*`) y la captura de entrada están diseñadas estáticamente y validadas offline; no se han observado en una sesión real de OMSI, y `ui.pause-menu.replace` se resuelve solo sobre una identidad de código conocida (algunos de sus campos no tienen una derivación independiente del build).
- La comprobación en proceso del vínculo de contenido usa el mapa que solicitó el inicio (NEW_MAP); para una situación guardada, el vínculo se comprueba en la planificación contra el mapa de la situación.
- Los hooks de dispositivo son hooks de vtable. Cuando otro componente vuelve a escribir los métodos originales del dispositivo (observado en la instalación de regresión poco después de que comienza el gameplay), un hook de dispositivo deja de ser llamado y no puede eliminarse; la limpieza informa entonces `OL_E_MODULE_CLEANUP_INCOMPLETE` y el componente permanece cargado. Un método de dispositivo ya enganchado por algo ajeno al registro se rechaza (4); el evento de rechazo nombra el módulo que posee el método.
- Los módulos se detienen en una salida normal de OMSI; una detención de sesión termina OMSI a la fuerza, de modo que la limpieza de módulos dentro de una sesión ocurre solo mediante una política de fallos.
- Los avisos se muestran una vez que existe la bandeja, después de que se inició OMSI; un programa de sesión muestra su propia ventana para todo lo que deba comunicar antes de eso.
