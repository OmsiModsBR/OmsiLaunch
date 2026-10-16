# Instalación

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Traducción de la [página original en inglés](../../../getting-started/installation.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página explica qué requiere OmsiLaunch `0.1.0-beta4`, qué build de OMSI admite, cómo se instala el paquete de la versión en una raíz de instalación de OMSI y cómo se verifica la instalación con `/version` y `/plan` antes de iniciar una sesión. El contenido del paquete se especifica en [empaquetado](../reference/packaging.md); el primer lanzamiento se describe en [primera sesión](first-session.md).

<a id="requirements"></a>
## Requisitos

| Requisito | Detalle | Fallo si falta |
|---|---|---|
| Windows 10 o posterior, 64 bits | El controlador comprueba `Environment.OSVersion.Version.Major >= 10`, un sistema operativo x64 y un proceso x64. | El plan no es ejecutable, con `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (o `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), salida `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` requiere `Microsoft.NETCore.App` 6.0 y `Microsoft.WindowsDesktop.App` 6.0 (el indicador de la bandeja usa Windows Forms). Los shims lo localizan con `nethost.dll`. | `OmsiLaunch.exe` termina con un código de shim `102`..`106` antes de cualquier salida; `OmsiLaunchW.exe` muestra `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` requiere `Microsoft.NETCore.App` 6.0 para x86, porque el plugin se ejecuta dentro del `Omsi.exe` de 32 bits. El paquete x86 de .NET 6 Desktop Runtime también cumple este requisito. | El plugin no se inicia dentro de OMSI; la sesión no llega a `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), salida `1`, archivos restaurados. |
| OMSI 2 instalado | Un `Omsi.exe` de 32 bits con una versión de OMSI 2 (2.x), y `maps\` y `Vehicles\` en la raíz. No se exige ningún hash concreto del ejecutable; OMSI 2.3.004 es la disposición conocida y validada en runtime, y en otras builds cada capacidad se resuelve y se verifica en el proceso. | `OL_E_UNSUPPORTED_BUILD`; el plan no es ejecutable, salida `1`. Una capacidad no disponible en la build falla con `OL_E_CAPABILITY_UNRESOLVED`. Consulta [compatibilidad](../reference/compatibility.md). |
| Raíz de instalación con permiso de escritura | La transacción escribe `.omsilaunch\`, overlays bajo `GUI\`, `Texture\` y `options.cfg`, y los restaura; el usuario actual debe poder escribir en la raíz (evita `Program Files` sin los permisos adecuados). | `OL_E_INSTALLATION_NOT_WRITABLE`, salida `1`. |
| Un usuario, un propietario por instalación | El lease de la instalación `Local\OmsiLaunch.Installation.<sha256(root)>` y la canalización de control son por sesión de inicio de sesión de Windows. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, salida `7`. |

Ambos runtimes se descargan por separado desde Microsoft; instala el Desktop Runtime x64 y el Runtime x86 (o el Desktop Runtime x86) de .NET 6. No se requiere ningún otro componente. Ningún dato sale del ordenador.

<a id="confirm-the-omsi-build"></a>
## Confirmar la build de OMSI

Sustituye `<OMSI_PATH>` por tu directorio de OMSI 2 (por ejemplo, `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

El hash debe ser uno de los dos indicados arriba. Tras la instalación, `OmsiLaunch.exe profiles` imprime la misma lista con su estado de validación.

<a id="install-the-package"></a>
## Instalar el paquete

1. Descarga `OmsiLaunch-0.1.0-beta4.zip` y `OmsiLaunch-0.1.0-beta4.zip.sha256`; verifica la suma de comprobación (`Get-FileHash` debe coincidir con el valor del archivo `.sha256`).
2. Extrae el archivo comprimido **directamente en la raíz de instalación de OMSI** (la carpeta que contiene `Omsi.exe`). El archivo comprimido está organizado para esa raíz:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` y los demás ensamblados del controlador `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE` y `THIRD-PARTY-NOTICES.md` en la raíz;
   - el conjunto de archivos del plugin permanente `plugins\OmsiLaunch.*` (9 archivos) junto a tus plugins existentes, que nunca se tocan;
   - `.omsilaunch\` con los recursos del splash, la documentación offline y los ejemplos.
3. Mantén `release-manifest.json` junto a `OmsiLaunch.exe`. Es lo que permite que cada inicio verifique los archivos del plugin instalados mediante SHA-256 (`plugin.integrity.reference = manifest`); sin él solo se comprueban la presencia y la coherencia interna (`plugin.integrity.reference = self`).
4. No muevas ni cambies el nombre de nada bajo `plugins\OmsiLaunch.*` y no coloques binarios del plugin en `.omsilaunch\`.

La actualización es la misma operación: extrae el nuevo paquete sobre los archivos antiguos mientras no haya ninguna sesión en ejecución ni ninguna recuperación pendiente (`OmsiLaunch.exe /recovery-status`). Los hashes del plugin y el manifiesto deben proceder siempre del mismo paquete (en caso contrario, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Verificar

Ejecuta desde la raíz de OMSI (el argumento de instalación toma de forma predeterminada el directorio que contiene `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Resultado esperado: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; salida `0`. Una salida `102`..`106` significa que falta el runtime x64 de .NET 6 o que el paquete está incompleto.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Resultado esperado: los hashes admitidos y, a continuación, los mapas detectados en esta instalación; salida `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Resultado esperado: `Plan: READY profile=Omsi23004` (en la disposición conocida; `unrecognized` en otro caso) y salida `0` (usa cualquier identidad de mapa de `/list:Maps`; el índice del punto de entrada debe ser un índice presentado de ese mapa, consulta `/list:Entrypoints /map:<identity>`). Con `--json`, el plan enumera `TouchedFiles` (los overlays del splash), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED`, y `RESOLVED_KNOWN_LAYOUT` para las capacidades nativas de inicio en la disposición conocida), `Diagnostics` (incluido `plugin.integrity.reference`) e `IsRunnable`. `Plan: NOT RUNNABLE` con salida `1` indica el motivo en `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). La planificación nunca inicia OMSI y nunca escribe en la instalación.

<a id="where-things-live-afterwards"></a>
## Dónde queda cada cosa después

| Ruta | Contenido |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Traza del host de cada sesión (se conservan las 50 sesiones más recientes) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Registro del indicador de la bandeja |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Solo presentes mientras hay una transacción pendiente; consulta [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Tus perfiles de sesión predefinidos; consulta [perfiles de sesión](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Recursos del splash gestionado |
| `<root>\.omsilaunch\docs\` | Esta documentación, offline (empieza por `README.md`; la referencia de la CLI es `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | LaunchSpec y perfil de sesión de ejemplo |

<a id="uninstall"></a>
## Desinstalar

Detén cualquier sesión, ejecuta `OmsiLaunch.exe /recovery-status` (y `/recover` si hay algo pendiente) y, a continuación, elimina los archivos del producto de la raíz, `plugins\OmsiLaunch.*` y `.omsilaunch\`. Los detalles están en [empaquetado](../reference/packaging.md).

<a id="next"></a>
## Siguiente

[Primera sesión](first-session.md) · [Referencia de la CLI](../reference/cli.md) · [limitaciones conocidas](../reference/known-limitations.md)
