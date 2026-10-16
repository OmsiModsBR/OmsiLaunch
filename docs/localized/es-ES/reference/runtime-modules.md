# Módulos de runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=faf61b29436d1f69013b0c49667534ef079e31f7b34300b538935788654a55df -->
> Traducción de la [página original en inglés](../../../reference/runtime-modules.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Un módulo de runtime añade comportamiento a una sesión gestionada de OMSI: una función que se ejecuta con la sesión (por ejemplo, el HUD de un mapa o optimizaciones de carga), sin pasar a formar parte de OmsiLaunch. El puente lo carga, le da solo lo que declara y elimina todo lo que retiene cuando la sesión termina o el módulo falla. Esta página es la referencia normativa de los módulos de runtime tal como los implementan `src/OmsiLaunch.Runtime.Modules` (manifiesto, ajustes, elegibilidad, host del ciclo de vida), el planificador, los programas de sesión del controlador (`ModuleSessionPrograms`, `ModuleSessionProcess`), el entorno de módulos del plugin y los flags de módulo de la [CLI](cli.md).

Estabilidad: `EXPERIMENTAL`. El protocolo de puente 1 se validó en runtime en la ronda B (módulos gestionados, activación por contenido, operaciones, `disable-module` y `fail-session`; componentes nativos con hooks de IAT; consulta el [estado de validación en runtime](../status/runtime-validation-status.md)). El protocolo de puente 2 (ajustes, activación predeterminada, el archivo de ajustes, programas de sesión, conflictos de plugins, avisos, `get_value`) está validado solo offline. Controles offline: los controles `module.*` de `tests/OmsiLaunch.ModuleTests` (manifiesto, integridad, selección y activación, ajustes, el archivo de ajustes, conflictos de plugins, programas de sesión iniciados mediante su job, un fallo inyectado en cada fase bajo cada política de fallo, ciclo de vida del componente nativo, limpieza, handoff), `plugin-runtime.modules-lifecycle` (un módulo gestionado real cargado por el entorno del plugin), `runtime-modules.native-component` (un componente nativo real que engancha una importación y lee valores a través del puente), `runtime-modules.plugin-conflict` y `native-hooks.self-check` (el motor de hooks).

La carga es abierta: se puede cargar cualquier módulo instalado bajo `plugins\OmsiLaunch.Modules`, sea quien sea su autor. OmsiLaunch no responde de lo que un módulo hace dentro de OMSI. Lo que garantiza es el [límite de seguridad](#security-boundary): los módulos nunca se ejecutan dentro del proceso de OmsiLaunch y nada de lo que ofrece OmsiLaunch permite a un módulo ir más allá de los derechos del propio OMSI.

<a id="where-modules-live"></a>
## Dónde viven los módulos

Cada módulo es un directorio bajo la carpeta del plugin permanente:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

El nombre del directorio debe ser igual al `id` del módulo. Los módulos los instala quien los distribuye; una sesión nunca escribe, captura en un snapshot ni restaura archivos allí, y los módulos nunca modifican archivos de la instalación a través de OmsiLaunch (un manifiesto no tiene forma de declarar un overlay de la instalación). Los datos escribibles de un módulo viven en su [directorio de datos](#data-directory).

<a id="manifest-modulejson"></a>
## Manifiesto (`module.json`)

Esquema `omsilaunch.runtime-module/v1`. Las claves desconocidas se rechazan, de modo que un manifiesto escrito para un esquema posterior se rechaza en lugar de entenderse a medias. Todas las rutas son relativas al directorio del módulo (las rutas de contenido enlazado son relativas a la raíz de OMSI), usan `\` o `/` y no pueden contener `..` ni una unidad.

| Clave | Obligatoria | Significado |
| --- | --- | --- |
| `schema` | sí | `omsilaunch.runtime-module/v1`. |
| `id` | sí | Id del módulo: letras minúsculas, dígitos y `-`, de 2 a 48 caracteres, empezando por una letra o un dígito. Es también el nombre de su directorio y su espacio de nombres de operaciones. |
| `version` | sí | `major.minor.patch`, opcionalmente `-suffix`. |
| `name` | no | Nombre para mostrar (como máximo 128 caracteres). |
| `bridge_protocol` | sí | `{ "min": n, "max": n }`: los protocolos de puente con los que funciona el módulo. Esta build sirve los protocolos `1` y `2`; se acepta un módulo cuyo rango incluya uno de ellos. |
| `activation` | no | `requested` (predeterminado): se carga cuando la sesión lo pide. `content`: se carga también cuando la sesión ejecuta el mapa enlazado (necesita `content_binding`). `default` (protocolo 2): se carga en todas las sesiones salvo que esté deshabilitado (consulta [elección de módulos](#choosing-modules-for-a-session)); no puede tener `content_binding`. |
| `required_capabilities` | no | Capacidades sin las que el módulo no puede funcionar (consulta [capacidades](#capabilities)). |
| `optional_capabilities` | no | Capacidades que el módulo usa cuando están disponibles; las comprueba con `IsCapabilityAvailable`. |
| `content_binding` | no | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: el módulo solo se ejecuta en ese mapa, con esos archivos sin cambios. |
| `assets` | no | Archivos que lee el módulo, cada uno `{ "path", "sha256" }`. |
| `components` | sí | Exactamente un componente `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; opcionalmente un componente `native` (`.dll`), componentes `auxiliary` (`.exe`) y, con el protocolo 2, un componente `host` (`.exe`, el [programa de sesión](#session-programs) del módulo). |
| `failure_policy` | sí | `fail-session`, `disable-module` o `degrade` (consulta [fallos](#failures)). |
| `operations` | no | Operaciones que expone el módulo mientras la sesión se ejecuta: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | no | Protocolo 2. Los ajustes que acepta el módulo (consulta [ajustes](#settings)). |
| `conflicts` | no | Protocolo 2. `{ "plugins": [ "<file>.dll" ] }`: DLL de plugins de OMSI que no deben ejecutarse junto al módulo (consulta [conflictos con plugins de OMSI](#conflicts-with-omsi-plugins)). |
| `timeouts` | no | Presupuestos en milisegundos: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (5000 cada uno de forma predeterminada, como máximo 30000); con el protocolo 2 también `prepare_ms` (120000 de forma predeterminada, como máximo 1800000) y `host_stop_ms` (10000 de forma predeterminada, como máximo 60000) para el programa de sesión. Las operaciones tienen 2000 ms de forma predeterminada. |
| `diagnostics` | no | `{ "trace": true\|false }`. |

Un manifiesto que use una función del protocolo 2 (`settings`, `conflicts`, activación `default`, un componente `host`, `prepare_ms` o `host_stop_ms`) debe declarar `bridge_protocol.min` 2.

Cada archivo nombrado en `components` y `assets` queda fijado por su SHA-256. `module.json` no puede fijarse a sí mismo.

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

Todo archivo fijado debe existir, no debe alcanzarse a través de una junction o un vínculo simbólico y debe tener el SHA-256 fijado; en caso contrario, el módulo se rechaza con `OL_E_MODULE_INTEGRITY`. Esto detecta un módulo corrupto o actualizado a medias. No es una decisión de confianza: un módulo no se comprueba contra ninguna lista, firma ni editor.

La comprobación se repite allí donde empieza a ejecutarse el código del módulo: el plugin carga un módulo solo si su `module.json` sigue teniendo el SHA-256 que evaluó el plan y todos los pines siguen cumpliéndose, y el controlador inicia un programa de sesión solo después de verificar el pin de ese programa.

<a id="security-boundary"></a>
## Límite de seguridad

OmsiLaunch no controla lo que un módulo hace dentro de OMSI: sus componentes gestionados y nativos se ejecutan en el proceso de OMSI con los mismos derechos que cualquier plugin de OMSI. OmsiLaunch garantiza lo siguiente:

1. El proceso de OmsiLaunch nunca carga código de módulos. El código de los módulos se ejecuta dentro de OMSI, en un programa auxiliar iniciado desde dentro de OMSI o en el programa de sesión del módulo.
2. Los programas que OmsiLaunch inicia para los módulos son archivos del propio directorio del módulo, verificados contra su pin e iniciados con una línea de comandos que fija OmsiLaunch. Un programa de sesión se ejecuta en un objeto job que lo termina con la sesión, no permite un segundo proceso (no puede iniciar otros programas) y rechaza cambios en los parámetros del sistema, la configuración de pantalla, el escritorio y el cierre de sesión o el apagado de Windows. Solo hereda sus dos handles de canalización.
3. Todo lo que declara un módulo permanece dentro de su directorio, y sus datos escribibles dentro de `.omsilaunch\module-data\<id>`.
4. Los canales de OmsiLaunch (control local, handoff de arranque, telemetría, comandos de runtime) permanecen restringidos al usuario actual y ligados a la sesión; ninguno permite a un llamador iniciar un programa, leer o escribir archivos fuera de la instalación ni cambiar la configuración del sistema.
5. OmsiLaunch nunca eleva privilegios. Un módulo se ejecuta con los derechos del usuario que inició la sesión.
6. El texto que procede de un módulo (avisos, eventos, valores) se trata como datos: acotado, sin caracteres de control, y nunca se ejecuta, se abre ni se sigue como enlace.

<a id="capabilities"></a>
## Capacidades

`required_capabilities` y `optional_capabilities` nombran una capacidad nativa de la build en ejecución (la columna de capacidad nativa del [catálogo de capacidades](capabilities.md#capability-catalog), por ejemplo `time.read` o `d3d.texture`) o una función del puente:

| Función del puente | Qué obtiene el módulo |
| --- | --- |
| `bridge.events` | `Emit`: eventos de runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: threads en segundo plano con seguimiento. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: procesos auxiliares ligados a la sesión. |
| `bridge.hooks.iat` | Componente nativo: hooks sobre importaciones de `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Componente nativo: hooks sobre métodos del dispositivo Direct3D 9 activo (`d3d9:device.<method>`). Necesita la capacidad nativa `d3d.texture`. |

Un nombre que no sea ninguna de las dos cosas se rechaza en la planificación (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). En una build de OMSI 2 sin un diseño conocido, una capacidad nativa que el archivo ejecutable no resuelve se decide en proceso; si tampoco se resuelve allí, el módulo no se carga.

<a id="choosing-modules-for-a-session"></a>
## Elección de módulos para una sesión

Un módulo se incorpora a una sesión de cuatro maneras; `PlannedModule.Activation` indica cuál:

| Activación | Cuándo |
| --- | --- |
| `requested` | La sesión lo pide: `LaunchSpec.Modules.Enable`, `/module:<id>` o `enabled: true` en el preset del perfil de sesión seleccionado. |
| `settings-file` | `enabled=true` en su sección del [archivo de ajustes](#the-settings-file). |
| `content` | Su activación es `content` y la sesión ejecuta su mapa enlazado. |
| `default` | Su activación es `default`. |

Un módulo queda fuera de la sesión por `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` en el preset o `enabled=false` en el archivo de ajustes (diagnóstico del plan `module.disabled`). Una petición explícita prevalece sobre el archivo de ajustes. Habilitar y deshabilitar el mismo módulo en una misma petición es `OL_E_MODULE_SETTING_INVALID`.

Reglas de planificación: un módulo solicitado que no se puede cargar hace que el plan no sea ejecutable, con su código `OL_E_MODULE_*`. Un módulo que se incorporó por sí mismo (archivo de ajustes, contenido, predeterminado) y no se puede cargar no detiene la sesión: el plan informa `OL_W_MODULE_NOT_LOADED`.

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
| `default` | sí | El valor cuando nada más lo establece: un booleano, un entero, una cadena o `"auto"` cuando `auto` es true. |
| `auto` | no | `true`: el valor `auto` se acepta y se pasa al módulo, que decide en tiempo de ejecución. |
| `min`, `max` | no | Límites enteros. |
| `choices` | para `choice` | Los valores aceptados (`[a-z0-9][a-z0-9_.-]{0,31}`, distintos de `auto`). |
| `max_length` | no | Límite de longitud del texto (1..4096, 256 de forma predeterminada). Los caracteres de control se rechazan. |
| `flag` | no | Un flag de línea de comandos para este ajuste, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, o `/<flag>` solo para `true` en un booleano. |
| `description` | no | Como máximo 256 caracteres. |

Los valores se normalizan: los booleanos aceptan `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; las opciones se comparan sin distinguir mayúsculas de minúsculas; los enteros se comprueban contra sus límites.

El valor efectivo de cada ajuste es, por orden:

1. la petición: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, el flag propio de un módulo o `modules.<id>.settings` en el preset del perfil de sesión seleccionado. Como en cualquier otro campo del preset, la línea de comandos no puede establecer también una clave que establece el preset (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. el [archivo de ajustes](#the-settings-file);
3. el valor predeterminado del manifiesto.

Un valor solicitado no válido, o una clave solicitada que el módulo no declara, hace que el plan no sea ejecutable (`OL_E_MODULE_SETTING_INVALID`). Una entrada del archivo de ajustes que no se puede usar se ignora con `OL_W_MODULE_SETTING_IGNORED`, igual que los ajustes solicitados para un módulo que la sesión no carga. El plan enumera cada valor efectivo con su origen (`request`, `settings-file` o `default`) en `PlannedModule.Settings`.

El módulo recibe cada clave declarada con su valor efectivo: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` en un componente nativo y el objeto `settings` del contexto de un programa de sesión.

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

Las secciones son ids de módulo; las líneas `key=value` establecen ajustes, `enabled` activa o desactiva el módulo para todas las sesiones; las líneas que empiezan por `;` o `#` son comentarios. El archivo puede tener como máximo 256 KiB. Todo lo que no puede usar (una sección que no es un id de módulo, una línea fuera de una sección, una clave desconocida, un valor no válido) se informa con `OL_W_MODULE_SETTING_IGNORED` y se ignora.

<a id="session-programs"></a>
## Programas de sesión

Un módulo puede declarar un componente `host`: su programa de sesión, un ejecutable de su propio directorio. Cuando el módulo está en el plan, el controlador:

1. verifica el pin del programa y crea el directorio de datos del módulo;
2. lo inicia antes que OMSI, desde el directorio del módulo, con la línea de comandos `"<program>" --omsilaunch-session <session id>`, suspendido hasta que está en su job (consulta el [límite de seguridad](#security-boundary));
3. escribe el contexto como primera línea en su entrada estándar;
4. espera hasta que el programa escribe `{"type":"ready"}`, como máximo `prepare_ms`; solo entonces inicia OMSI;
5. una vez creado OMSI, escribe `{"type":"omsi-started","pid":<process id>}`;
6. al final de la sesión, después de que OMSI haya terminado y antes de restaurar ningún archivo, escribe `{"type":"stop"}`, espera como máximo `host_stop_ms` y luego termina el programa.

Ambas direcciones usan JSON en UTF-8, un objeto por línea. El contexto:

| Campo | Significado |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identidad. |
| `installation_root`, `module_directory`, `data_directory` | Rutas completas. |
| `channel` | La canalización con nombre que el programa puede servir para los componentes del módulo en OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. La crea el programa, restringida al usuario actual, y comprueba que el cliente es el proceso de OMSI. Los componentes la leen con `get_value("session_channel")` o `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | Los ajustes efectivos del módulo. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` de la sesión. |
| `session_files` | Archivos relativos a la instalación que posee la transacción de la sesión. El programa no debe mantenerlos abiertos ni mapeados: se restauran cuando la sesión termina. |

Lo que el programa puede escribir además de `ready`:

| Línea | Efecto |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Evento de runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Evento de runtime `module.<id>.notice` (consulta [avisos](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Diagnóstico de sesión con ese código catalogado (`OL_E_MODULE_OPERATION_FAILED` en caso contrario). |

Las demás líneas, las líneas de más de 64 KiB y todo lo que viene después de 10,000 líneas se ignoran. El controlador registra `module.<id>.host.started`, `host.ready` (con `prepare_ms`), `host.failed`, `host.exited` y `host.killed`.

Un programa que no está listo en `prepare_ms`, termina antes de estar listo o cuyo pin no se cumple hace fallar la preparación de su módulo: con `fail-session` la sesión no se inicia (`OL_E_MODULE_SESSION_FAILED`); con `disable-module` el programa se termina y el módulo queda fuera de OMSI; con `degrade` el módulo permanece (y el programa sigue ejecutándose si todavía puede). El `StartupTimeoutSeconds` de la sesión cuenta desde que se inicia OMSI, de modo que la preparación no lo consume.

<a id="conflicts-with-omsi-plugins"></a>
## Conflictos con plugins de OMSI

`conflicts.plugins` nombra DLL de plugins de OMSI que no deben ejecutarse junto al módulo, por ejemplo un plugin más antiguo que instala los mismos hooks. En la planificación, OmsiLaunch lee la sección `[dll]` de cada `plugins\*.opl`; si alguno carga una DLL en conflicto, el módulo se rechaza con `OL_E_MODULE_CONFLICT` y el motivo nombra el `.opl`. Dentro de OMSI, un módulo cuya DLL en conflicto se carga de todos modos no se carga. OmsiLaunch nunca modifica los archivos de otro plugin; desactivarlo es decisión del usuario.

<a id="notices"></a>
## Avisos

Un evento de módulo llamado `notice` (de `Emit("notice", ...)`, de un `emit` de un componente nativo o de una línea de aviso de un programa de sesión) lo muestra la bandeja de OmsiLaunch como un globo cuyo título nombra el módulo. Solo se conservan `title` (como máximo 63 caracteres) y `text` (como máximo 255 caracteres), se eliminan los caracteres de control y nada de su contenido se abre ni se ejecuta. Un programa de sesión que deba informar al usuario antes de que se inicie OMSI (cuando la bandeja aún no se muestra) usa su propia ventana.

<a id="data-directory"></a>
## Directorio de datos

`<OMSI root>\.omsilaunch\module-data\<id>\` es el directorio escribible del módulo para esta instalación: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` y el `data_directory` del programa de sesión. Se crea en el primer uso. No forma parte de la transacción de la sesión: lo que un módulo escribe allí permanece después de la sesión. `/clear-module-data:<id>` lo elimina (y nada más); se rechaza mientras una sesión posea la instalación, y un directorio alcanzado a través de un vínculo se elimina como vínculo, sin seguirlo nunca.

<a id="lifecycle"></a>
## Ciclo de vida

| Paso | Dónde | Qué ocurre |
| --- | --- | --- |
| 1. Descubrimiento | controlador | Se lee `plugins\OmsiLaunch.Modules\*`: manifiesto e integridad (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Elegibilidad | controlador, en la planificación | Se evalúan los módulos que [se incorporan a la sesión](#choosing-modules-for-a-session): utilizable, protocolo de puente, capacidades, enlace de contenido, conflictos de plugins, ajustes. El resultado es `SessionPlan.PlannedModules`. |
| 3. Preparación | controlador, al iniciar | Los [programas de sesión](#session-programs) de los módulos se inician y se espera a ellos. Después, los módulos elegibles viajan al plugin en el handoff de arranque (versión 6): sus directorios, el SHA-256 de cada `module.json` evaluado y sus ajustes efectivos. |
| 4. Armado | plugin, antes de que empiece el mundo | Cada módulo se vuelve a verificar y se carga (componente gestionado en su propio contexto de carga). Su componente nativo, si lo hay, se carga y se inicia primero; después se llama a `Arm(context)`. |
| 5. Listo para el juego | plugin | Tras `gameplay.entered`, cuando un módulo cargado declara `bridge.hooks.d3d9`, el puente empieza a observar el dispositivo Direct3D (`d3d.ready`); después llama a `gameplay_ready` del componente nativo y a `GameplayReady()`. En una sesión NEW_MAP, el mapa enlazado se vuelve a comprobar contra el mapa que cargó el inicio. |
| 6. Runtime | plugin | Las operaciones `module.<id>.<operation>` llegan a `Execute`. |
| 7. Detención | plugin, después controlador | Cuando la sesión termina (o el módulo falla), se llama a `Stop()`. Después de que OMSI haya terminado, se detienen los programas de sesión. |
| 8. Limpieza | plugin | Todo lo que el módulo tenía en seguimiento se libera en orden inverso y se verifica que ha desaparecido; el componente nativo se detiene el último y se eliminan sus hooks; su DLL permanece cargada, inerte, hasta que OMSI termina (un thread aún podría estar regresando a través de un detour). |

Todo callback en proceso se ejecuta en el thread de la UI de OMSI, igual que las operaciones de runtime propias del producto. El puente mide cada callback contra su presupuesto: un callback que vuelve tarde hace fallar su fase (`OL_E_MODULE_PHASE_TIMEOUT`). El puente no puede interrumpir un callback que nunca vuelve; un módulo así bloquea OMSI, y la sesión termina en su timeout de arranque o con `session stop`, como cualquier otra sesión.

<a id="failures"></a>
## Fallos

Un fallo es una excepción o un exceso de tiempo en cualquier fase u operación, una capacidad no resuelta en proceso, un enlace de contenido que no se cumple en proceso, un plugin en conflicto cargado en OMSI o un programa de sesión que no llega a estar listo. La `failure_policy` del módulo decide lo que sigue. En todos los casos se liberan los recursos del módulo.

| Política | Fallo en preparación, carga, armado o listo para el juego | Fallo en una operación | Fallo en la detención |
| --- | --- | --- | --- |
| `fail-session` | Módulo liberado; `module.session-failure`; la sesión falla con `OL_E_MODULE_SESSION_FAILED`, OMSI se detiene y los archivos se restauran. Un fallo antes de que empiece el mundo impide el inicio. | Igual. | Se informa (`module.failed`); la sesión continúa hasta su final. |
| `disable-module` | Módulo liberado y deshabilitado; la sesión continúa. | Igual. | Se informa; módulo liberado. |
| `degrade` | Como `disable-module`, salvo que el fallo de un programa de sesión deja el módulo en la sesión. | La llamada que falla devuelve su código; el módulo sigue activo; `module.degraded` se informa una vez por código. | Se informa; módulo liberado. |

Un módulo deshabilitado o fallido responde a sus operaciones con `OL_E_MODULE_DISABLED`. Los fallos de módulos aparecen como diagnósticos de sesión (`SessionStatus.Diagnostics`) con su código `OL_E_MODULE_*` y como eventos de runtime.

<a id="cleanup"></a>
## Limpieza

Lo que un módulo adquiere a través de su contexto tiene seguimiento: los recursos registrados con `Track`, los threads iniciados con `RunBackground` y los procesos iniciados con `StartAuxiliaryProcess`. En la detención o el fallo, el puente cancela `Stopping` y luego los libera en orden inverso. Los procesos auxiliares se colocan además en un objeto job con terminación al cerrar, de modo que terminan con OMSI aunque OMSI se mate; el job de un programa de sesión lo termina con la sesión de OmsiLaunch, aunque se mate al propio OmsiLaunch. Un recurso que sigue vivo después de la liberación (un proceso que no terminó, un thread que no acabó) se informa como `OL_E_MODULE_CLEANUP_INCOMPLETE`. Los threads o procesos que un módulo crea sin su contexto no tienen seguimiento.

Tras un bloqueo no hay nada que recuperar para los módulos: no retienen archivos de la instalación, y su memoria, sus threads y los procesos ligados a un job terminan con el proceso de OMSI o el de OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Componentes nativos y hooks

Un componente nativo es una DLL de 32 bits que implementa la interfaz C de `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Exportación | Se llama |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | En el armado, antes del `Arm` gestionado. Un valor distinto de cero hace fallar la fase. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Opcional; después de entrar en la fase de juego. |
| `void omsilaunch_module_native_v1_stop(void)` | En la detención o el fallo, antes de eliminar sus hooks. Después, la tabla del puente rechaza toda llamada y la DLL permanece cargada, inerte, hasta que OMSI termina. |

La tabla del puente da al componente `request_hook`, `emit` (eventos de runtime `module.<id>.<name>` con un objeto JSON de cadenas) y, desde el protocolo 2, `get_value`. Un componente lee el `size` de la tabla antes de usar `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copia un valor como UTF-8 con un cero final y establece `*length` en los bytes necesarios sin él; devuelve `0`, `1` (argumento), `2` (nombre desconocido), `11` (después de la detención) o `12` (búfer demasiado pequeño). Nombres:

| Nombre | Valor |
| --- | --- |
| `setting.<key>` | El valor efectivo de un ajuste declarado. |
| `data_directory` | El [directorio de datos](#data-directory) del módulo. |
| `module_directory` | El directorio propio del módulo. |
| `session_id` | El id de la sesión. |
| `module_version` | La versión del manifiesto. |
| `product_version` | La versión del producto OmsiLaunch. |
| `session_channel` | La canalización del [programa de sesión](#session-programs); desconocida para un módulo que no tiene uno. |

Los hooks se solicitan solo por destino simbólico, durante `start` o `gameplay_ready`, para un destino cuya capacidad declara el manifiesto:

| Destino | Engancha | Capacidad |
| --- | --- | --- |
| `iat:<dll>!<function>` | Una importación de `Omsi.exe`, por ejemplo `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` o `EndScene` del dispositivo Direct3D 9 activo (solicítalo en `gameplay_ready`, cuando el dispositivo existe). | `bridge.hooks.d3d9` |

`request_hook` devuelve en `*original` la función con la que continuar la cadena (un detour debe llamarla). Su resultado: `0` éxito, `1` argumento, `2` destino desconocido, `3` el sitio no contiene lo que debería, `4` el sitio está enganchado por alguien ajeno al puente, `5` protección de memoria, `6` registro lleno, `7` orden, `8` memoria, `9` un thread estaba ejecutando los bytes que había que parchear, `10` aún no hay dispositivo Direct3D, `11` no permitido ahora o capacidad no declarada.

Cada hook que instala el puente, para el producto o para un módulo, se registra en un único registro con su propietario (`module:<id>` para un módulo):

- Un hook se instala solo sobre el valor que el sitio debería contener (la función importada; una función de un módulo llamado `d3d9.dll`, del runtime del sistema o de un wrapper), o apilado sobre el hook superior del propio registro en ese sitio. Un sitio que ya cambió otro se rechaza (`4`), nunca se sobrescribe.
- Los hooks de un mismo sitio se eliminan en orden último en entrar, primero en salir; un hook que tiene otro encima permanece hasta que se elimina ese.
- El hook `Reset` de Direct3D propio del producto está en el registro, de modo que los hooks de dispositivo de los módulos se apilan sobre él.
- En la limpieza, el puente elimina todos los hooks del módulo después de que `stop` devuelva. Si alguno no se puede eliminar (alguien más cambió el sitio), la limpieza informa `OL_E_MODULE_CLEANUP_INCOMPLETE`. La DLL de un componente nativo nunca se descarga mientras OMSI se ejecuta, de modo que un detour por el que todavía se está regresando sigue siendo válido.

El motor también admite hooks sobre prólogos de funciones con un trampolín (los demás threads se suspenden mientras cambian los bytes y antes se comprueban los bytes esperados exactos), pero todavía no se ofrece ningún destino de prólogo a los módulos: tales destinos proceden del catálogo de destinos de la build con bytes verificados.

<a id="operations"></a>
## Operaciones

Una operación de módulo es `module.<id>.<operation>`. Solo existe para una sesión cuyo plan cargó el módulo (`PlannedModule.Operations`); en caso contrario, `ExecuteRuntimeAsync` responde `OL_E_MODULE_NOT_LOADED` o `OL_E_MODULE_OPERATION_UNKNOWN`. Se comprueban los argumentos obligatorios y se rechazan los argumentos no declarados (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Un fallo tipado de un módulo conserva su código cuando es un código público catalogado; cualquier otro código se convierte en `OL_E_MODULE_OPERATION_FAILED` con el código del módulo en `Values["module_code"]`. Las claves de resultado que siguen la convención de nombres interna se eliminan, como en cualquier operación de runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="writing-a-module-contracts"></a>
## Escribir un módulo (contratos)

El componente gestionado hace referencia a `OmsiLaunch.Runtime.Modules` y `OmsiLaunch.Api` e implementa `IRuntimeModule`. El puente enlaza estas dos referencias con sus propios ensamblados cargados (sea cual sea el contexto de carga en que se ejecute el plugin), de modo que se ignora una copia distribuida con el módulo:

| Miembro | Se llama |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Paso 4. Adquiere recursos a través del contexto. |
| `void GameplayReady()` | Paso 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Paso 6, para cada operación declarada (sin el prefijo `module.<id>.`). Lanza `ModuleOperationException(code, message)` para un fallo tipado. |
| `void Stop()` | Paso 7. |

`IRuntimeModuleContext` ofrece `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `SessionChannelName`, `Stopping` (un token de cancelación), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground` y `StartAuxiliaryProcess`. Nada en él expone direcciones, el ejecutable ni su identidad. El componente debe poder cargarlo el plugin de 32 bits (AnyCPU o x86, .NET 6). Fija todo archivo que carguen los programas del módulo (para un programa .NET: su `.dll` junto al `.exe`), de modo que la integridad los cubra.

<a id="known-limitations"></a>
## Limitaciones conocidas

- Los componentes nativos pueden enganchar importaciones de `Omsi.exe` y métodos del dispositivo Direct3D; los hooks sobre prólogos de funciones de OMSI y sitios de llamada todavía no se ofrecen a los módulos.
- La comprobación del enlace de contenido en proceso usa el mapa que solicitó el inicio (NEW_MAP); para una situación guardada, el enlace se comprueba en la planificación contra el mapa de la situación.
- Los hooks de dispositivo son hooks de vtable. Cuando otro componente vuelve a escribir los métodos originales del dispositivo (observado en la instalación de regresión poco después de empezar la fase de juego), un hook de dispositivo deja de ser llamado y no se puede eliminar; la limpieza informa entonces `OL_E_MODULE_CLEANUP_INCOMPLETE` y el componente permanece cargado. Un método de dispositivo ya enganchado por algo ajeno al registro se rechaza (4); el evento de rechazo nombra el módulo que posee el método.
- Los módulos se detienen en una salida normal de OMSI; una detención de sesión termina OMSI, de modo que la limpieza de módulos dentro de una sesión solo ocurre mediante una política de fallo.
- Los avisos se muestran una vez que existe la bandeja, después de iniciar OMSI; un programa de sesión muestra su propia ventana para cualquier cosa que deba decir antes de eso.
