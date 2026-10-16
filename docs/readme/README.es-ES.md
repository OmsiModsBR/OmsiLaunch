<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Control de sesiones para OMSI 2.</strong></p>
<p align="center">Código abierto · Programable · Impulsado por la comunidad</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <strong>Español (España)</strong> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Esta es una traducción del [README canónico en inglés (EE. UU.)](../../README.md). Si ambos difieren, prevalece el README en inglés (EE. UU.).

# OmsiLaunch

**OmsiLaunch** es una capa de código abierto para lanzar OMSI 2 mediante
programación, gestionar sesiones y controlar la simulación en ejecución.
Planifica una sesión a partir de una descripción declarativa, aplica cada cambio
temporal de configuración dentro de una transacción con diario, inicia OMSI, lo
observa hasta llegar a la fase de juego, permite que las herramientas lean y
modifiquen la simulación en ejecución a través de una API pública y restaura
cada archivo que ha modificado cuando la sesión finaliza.

Es infraestructura para lanzadores, herramientas, automatización e integraciones
de la comunidad. No es un lanzador gráfico.

> **Define la sesión, no los clics.**

## Estado: 0.1.0-beta4

La beta pública actual es **`0.1.0-beta4`**. La Beta 4 añade compatibilidad por
capacidad, módulos de runtime, perfiles de sesión del 1 al 99 y un conjunto de
funciones de inicio y de runtime. La compatibilidad, el catálogo de
capacidades, la paginación, los errores tipados y el puente de módulos se
observaron en sesiones reales de OMSI (rondas de validación A y B). Las
funciones añadidas después de esas rondas son `EXPERIMENTAL`: cerradas mediante
análisis estático y probadas solo offline, con la validación en runtime
pendiente. Algunas siguen siendo `STATICALLY_VALIDATED`, `PARTIAL` o
`UNAVAILABLE`. Quedan dos elementos de runtime abiertos porque no pueden
producirse de forma segura: la fase de juego con Steam LAA y la eliminación
natural de vehículos de carretera y humanos (RV-002). La página de
[estado de la validación en runtime](../localized/es-ES/status/runtime-validation-status.md) es
el registro de referencia de lo que se ejecutó con OMSI y de lo que se ejecutó solo offline.

Es una beta: la API pública, la CLI y los formatos de archivo están marcados como
`STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` o `UNAVAILABLE` por miembro
y aún pueden cambiar antes de la 1.0.

## Alcance de OMSI admitido

OmsiLaunch admite exactamente un build de OMSI 2 y se niega a iniciar cualquier
cosa que no reconozca.

| Elemento | Alcance |
| --- | --- |
| Build de OMSI | Perfil `Omsi23004_692EBFBF`: `Omsi.exe` con SHA-256 `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` (OMSI 2.3.004). `STABLE_BETA`; todas las validaciones en runtime se ejecutaron con este archivo. |
| Ejecutable Steam LAA | El SHA-256 `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` se acepta mediante lista de permitidos. Solo se validaron la huella y la planificación; la fase de juego **no** se validó en runtime. `PARTIAL`. |
| Builds desconocidos | Se rechazan con `OL_E_UNSUPPORTED_BUILD`. El hash se vuelve a comprobar en cada planificación y en cada inicio. |
| Sistema operativo | Windows 10 o posterior, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (controlador) y .NET 6 Runtime **x86** (el plugin se ejecuta dentro del `Omsi.exe` de 32 bits). |

Detalles: [Compatibilidad](../localized/es-ES/reference/compatibility.md) e
[Instalación](../localized/es-ES/getting-started/installation.md).

## Qué ofrece

**Sesiones.** Una sesión se planifica a partir de un `LaunchSpec` (flags de la
CLI, un archivo JSON, un perfil de sesión o la API), se valida sin efectos
secundarios (`/plan`) y después se inicia, se observa mediante las transiciones
de `SessionState` y se finaliza. Detener una sesión termina OMSI de forma
forzada, para que OMSI no pueda sobrescribir los archivos que están a punto de
restaurarse. Consulta [Ciclo de vida de la sesión](../localized/es-ES/concepts/session-lifecycle.md).

**Transacciones y recuperación.** Cada cambio de configuración se limita a la
sesión. OmsiLaunch toma un snapshot de cada archivo que modifica, lo registra en
el diario, aplica el cambio, lo verifica y lo restaura, también después de un
fallo (`/recovery-status`, `/recover`, `RecoverPendingAsync`). No ofrece edición
permanente de la configuración. Consulta
[Transacciones y recuperación](../localized/es-ES/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Una interfaz de referencia sobre la misma API
pública, sin lógica de OMSI propia: descubrimiento, planificación, inicio de
sesiones y comandos de cliente contra una sesión en ejecución. Consulta la
[referencia de la CLI](../localized/es-ES/reference/cli.md)
y los [ejemplos de la CLI](../localized/es-ES/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** El host del subsistema de Windows para accesos directos.
Acepta la misma línea de comandos sin ventana de consola e informa de los
errores en cuadros de mensaje.
Consulta [OmsiLaunchW.exe](../localized/es-ES/reference/omsilaunchw.md).

**Bandeja de Windows.** Cada sesión propietaria muestra un icono en el área de
notificación con una ventana de estado y una acción «End session» (finalizar la
sesión). La acción sigue la misma ruta de detención que `session stop`. Consulta
[Bandeja de Windows](../localized/es-ES/reference/windows-tray.md).

**Perfiles de sesión.** Paquetes declarativos `profile.yaml`
(`omsilaunch.session-profile/v1`) en
`.omsilaunch\session-profiles\<id>\`, para que los autores de contenido puedan
distribuir sesiones reproducibles que se inician con un solo comando. Consulta
[Perfiles de sesión](../localized/es-ES/reference/session-profiles.md).

**API pública y control en runtime.** `OmsiLaunch.Api` (`IOmsiLaunch`) es la
superficie de producto preferida. Las operaciones de runtime, como la hora, la
meteorología, el mapa, la cámara, el horario, los vehículos, los humanos, las
variables de script y las texturas D3D, están limitadas a la sesión, se validan
con el perfil de build y se direccionan mediante handles semánticos opacos, nunca
mediante punteros nativos. Los resultados están acotados por un slot de runtime
de 64 KiB. Consulta la [referencia de la API pública](../localized/es-ES/reference/public-api.md) y
[Control en runtime](../localized/es-ES/reference/runtime-control.md).

**Control local.** Una canalización con nombre por instalación, vinculada al
`session_id` activo, permite que otros procesos del mismo usuario lean el estado
y los eventos, detengan la sesión y ejecuten operaciones de runtime públicas.
Consulta [Control local / IPC](../localized/es-ES/reference/local-control.md).

**Capacidades.** Cada capacidad y cada operación de runtime pública está
catalogada con su estabilidad. Las capacidades experimentales y no disponibles
se enumeran, no se ocultan (`OmsiLaunch.exe capabilities`). Consulta
[Capacidades](../localized/es-ES/reference/capabilities.md).

**Plugin permanente.** El conjunto de archivos del plugin que se ejecuta dentro
del proceso se instala una sola vez en `plugins\OmsiLaunch.*`. Antes de cada
inicio se comprueba con las entradas SHA-256 de `release-manifest.json`. Los
plugins de terceros nunca se tocan. Consulta
[Modelo de plugin permanente](../localized/es-ES/concepts/permanent-plugin.md).

## Inicio rápido

Extrae el paquete de la versión en la raíz de la instalación de OMSI 2 y, a
continuación, ejecuta los siguientes comandos desde ese directorio:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Mientras esa sesión se ejecuta, una segunda consola en el mismo directorio puede
consultarla o finalizarla:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Guía paso a paso: [Primera sesión](../localized/es-ES/getting-started/first-session.md). Para
integradores de .NET: [Inicio rápido de la API pública](../localized/es-ES/getting-started/api-quick-start.md).

## Documentación

La documentación completa está indexada en [`docs/localized/es-ES/README.md`](../localized/es-ES/README.md).
Las páginas en inglés (EE. UU.) de `docs/` son canónicas y normativas.

| Tema | Página |
| --- | --- |
| API pública | [docs/localized/es-ES/reference/public-api.md](../localized/es-ES/reference/public-api.md) |
| CLI | [docs/localized/es-ES/reference/cli.md](../localized/es-ES/reference/cli.md) |
| Ejemplos de la CLI | [docs/localized/es-ES/reference/cli-examples.md](../localized/es-ES/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/es-ES/reference/omsilaunchw.md](../localized/es-ES/reference/omsilaunchw.md) |
| Bandeja de Windows | [docs/localized/es-ES/reference/windows-tray.md](../localized/es-ES/reference/windows-tray.md) |
| Perfiles de sesión | [docs/localized/es-ES/reference/session-profiles.md](../localized/es-ES/reference/session-profiles.md) |
| Control en runtime | [docs/localized/es-ES/reference/runtime-control.md](../localized/es-ES/reference/runtime-control.md) |
| Control local / IPC | [docs/localized/es-ES/reference/local-control.md](../localized/es-ES/reference/local-control.md) |
| Capacidades | [docs/localized/es-ES/reference/capabilities.md](../localized/es-ES/reference/capabilities.md) |
| Errores y códigos de salida | [docs/localized/es-ES/reference/errors.md](../localized/es-ES/reference/errors.md), [docs/localized/es-ES/reference/exit-codes.md](../localized/es-ES/reference/exit-codes.md) |
| Empaquetado | [docs/localized/es-ES/reference/packaging.md](../localized/es-ES/reference/packaging.md) |
| Limitaciones conocidas | [docs/localized/es-ES/reference/known-limitations.md](../localized/es-ES/reference/known-limitations.md) |
| Estado de la validación en runtime | [docs/localized/es-ES/status/runtime-validation-status.md](../localized/es-ES/status/runtime-validation-status.md) |

### Documentación en otros idiomas

La documentación está traducida a 14 configuraciones regionales en
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). Las traducciones
se elaboraron a partir de las páginas canónicas en inglés (EE. UU.) y se
validaron mecánicamente con ellas. La revisión editorial por hablantes nativos
no formó parte de la versión Beta 4 y puede realizarse después de la
publicación. Cuando una traducción y la página en inglés no coinciden, prevalece
la página en inglés.

## Limitaciones y elementos abiertos

Estas son las limitaciones más importantes. La lista completa está en
[Limitaciones conocidas](../localized/es-ES/reference/known-limitations.md).

- **Un único build de OMSI.** Steam LAA es `PARTIAL`: la fase de juego, las
  lecturas, los comandos y la detención requieren una instalación auténtica de
  Steam y no se validaron en runtime.
- **No disponible en esta beta:** iniciar desde el último estado del mapa
  (`/last`), fecha, hora, año o meteorología explícitos al iniciar y asignación
  del vehículo del jugador al iniciar. Solicitar cualquiera de ellos hace que el
  plan no sea ejecutable, en lugar de ignorarse en silencio.
- **Las escrituras en runtime son limitadas.** `weather.set`, las escrituras del
  calendario, las escrituras de variables de cadena y la reubicación de
  vehículos no están disponibles. Los cambios en runtime no se registran en el
  diario y no se restauran.
- **Vida útil de los handles.** La detección de handles obsoletos para vehículos
  de carretera y humanos que desaparecen de forma natural (RV-002) no tiene un
  productor seguro en runtime y solo está validada offline.
- **Resultados acotados.** Las listas largas se truncan (`truncated=true`). No
  hay paginación.
- **La detención es forzada.** El cierre propio de OMSI no se ejecuta y el
  estado de OMSI no guardado se pierde.
- **Modelo de confianza del mismo usuario.** Cualquier proceso del mismo usuario
  de Windows puede acceder al plano de control local.

## Descarga

Descarga **`OmsiLaunch-0.1.0-beta4.zip`** y su archivo `.sha256` desde la
[página de versiones](https://github.com/lmonteirotech/OmsiLaunch/releases).
Extráelo directamente en la raíz de OMSI admitida. El paquete contiene el
controlador (`OmsiLaunch.exe`, `OmsiLaunchW.exe`), sus dependencias, el conjunto
de archivos del plugin permanente con `release-manifest.json`, los recursos de
la pantalla de presentación (splash), un ejemplo de sesión y la documentación
offline en `.omsilaunch\docs\`. Estructura del paquete y desinstalación limpia:
[Empaquetado](../localized/es-ES/reference/packaging.md).

## Compilación desde el código fuente

Requisitos:

- Windows 10 o posterior, x64.
- .NET 6 SDK, con los runtimes de .NET 6 x64 y x86 para ejecutar las pruebas.
- Visual Studio con la carga de trabajo de C++ para MSBuild (conjunto de
  herramientas de plataforma `v145`) y un SDK de Windows 10, para los tres
  proyectos nativos: `OmsiLaunch.Native.x86` (Win32), y
  `OmsiLaunch.Bootstrapper` y `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` contiene los proyectos administrados y los conjuntos de
pruebas. El único punto de entrada offline lo compila todo y ejecuta todos los
conjuntos de pruebas offline; nunca lanza OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` omite los proyectos nativos y la prueba de regresión del
empaquetado. `-SkipDocs` omite los controles de documentación. La salida de la
compilación va a `artifacts\`, que no está bajo control de versiones.
`tools\New-ReleasePackage.ps1` prepara, calcula los hashes, valida y comprime
un paquete de la versión a partir de una compilación existente. Consulta
[Empaquetado](../localized/es-ES/reference/packaging.md) y
[Pruebas y validación](../project/TESTING-AND-VALIDATION.md).

| Ruta | Contenido |
| --- | --- |
| `src/` | Bibliotecas del producto: API, núcleo, configuración, contenido, interop, proceso, plugin, perfil de build, frontera nativa x86 |
| `tools/` | CLI y host de Windows (`OmsiLaunch.Cli`), shims nativos (`OmsiLaunch.Bootstrapper`), host de pruebas offline, scripts de empaquetado y validación, herramientas de localización |
| `tests/` | Conjuntos de pruebas unitarias, de integración, de perfil, de UI de Windows y de documentación |
| `docs/` | Documentación canónica y sus traducciones en `docs/localized/` |
| `examples/` | Ejemplos de LaunchSpec y de sesión |
| `assets/` | Identidad visual, iconos y recursos del paquete |
| `third_party/` | Notas sobre la procedencia upstream |

Resúmenes para mantenedores: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Comunidad y licencia

OmsiLaunch es un proyecto de código abierto orientado a la comunidad. Es
independiente de otros lanzadores de OMSI, y las herramientas compatibles de la
comunidad pueden construirse sobre él.

OmsiLaunch se distribuye bajo la licencia [LGPL-3.0-only](../../LICENSE). Consulta los
[avisos de terceros](../../THIRD-PARTY-NOTICES.md) para conocer la procedencia del
código fuente incorporado y los avisos aplicables.
