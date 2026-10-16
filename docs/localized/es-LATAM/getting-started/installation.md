# Instalación

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Traducción de la [página original en inglés](../../../getting-started/installation.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página explica qué requiere OmsiLaunch `0.1.0-beta4`, qué build de OMSI admite, cómo se instala el paquete de release en la raíz de una instalación de OMSI y cómo verificar la instalación con `/version` y `/plan` antes de iniciar una sesión. El contenido del paquete se especifica en [empaquetado](../reference/packaging.md); el primer inicio se describe en [primera sesión](first-session.md).

<a id="requirements"></a>
## Requisitos

| Requisito | Detalle | Fallo cuando falta |
|---|---|---|
| Windows 10 o posterior, 64 bits | El controlador comprueba `Environment.OSVersion.Version.Major >= 10`, un sistema operativo x64 y un proceso x64. | Plan no ejecutable con `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (o `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), salida `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` requiere `Microsoft.NETCore.App` 6.0 y `Microsoft.WindowsDesktop.App` 6.0 (el indicador de la bandeja usa Windows Forms). Los shims lo localizan con `nethost.dll`. | `OmsiLaunch.exe` termina con un código de shim `102`..`106` antes de cualquier salida; `OmsiLaunchW.exe` muestra `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` requiere `Microsoft.NETCore.App` 6.0 para x86, porque el plugin se ejecuta dentro del `Omsi.exe` de 32 bits. El paquete x86 de .NET 6 Desktop Runtime también cumple este requisito. | El plugin no arranca dentro de OMSI; la sesión no llega a `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), salida `1`, archivos restaurados. |
| OMSI 2 instalado | Un `Omsi.exe` de 32 bits con una versión de OMSI 2 (2.x), y `maps\` y `Vehicles\` en la raíz. No se exige ningún hash concreto del ejecutable; OMSI 2.3.004 es el diseño conocido y validado en runtime, y en otros builds cada capacidad se resuelve y verifica en el proceso. | `OL_E_UNSUPPORTED_BUILD`; plan no ejecutable, salida `1`. Una capacidad no disponible en el build falla con `OL_E_CAPABILITY_UNRESOLVED`. Consulte [compatibilidad](../reference/compatibility.md). |
| Raíz de la instalación con permiso de escritura | La transacción escribe `.omsilaunch\`, overlays bajo `GUI\`, `Texture\` y `options.cfg`, y los restaura; el usuario actual debe poder escribir en la raíz (evite `Program Files` sin los permisos adecuados). | `OL_E_INSTALLATION_NOT_WRITABLE`, salida `1`. |
| Un usuario, un propietario por instalación | El lease de la instalación `Local\OmsiLaunch.Installation.<sha256(root)>` y el pipe de control son por sesión de inicio de sesión de Windows. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, salida `7`. |

Ambos runtimes se descargan por separado desde Microsoft; instale el Desktop Runtime x64 y el Runtime x86 (o el Desktop Runtime x86) de .NET 6. No se requiere ningún otro componente. Ningún dato sale de la computadora.

<a id="confirm-the-omsi-build"></a>
## Confirmar el build de OMSI

Reemplace `<OMSI_PATH>` por su directorio de OMSI 2 (por ejemplo `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

El hash debe ser uno de los dos indicados arriba. Después de la instalación, `OmsiLaunch.exe profiles` imprime la misma lista con su estado de validación.

<a id="install-the-package"></a>
## Instalar el paquete

1. Descargue `OmsiLaunch-0.1.0-beta4.zip` y `OmsiLaunch-0.1.0-beta4.zip.sha256`; verifique la suma de comprobación (`Get-FileHash` debe ser igual al valor del archivo `.sha256`).
2. Extraiga el archivo comprimido **directamente en la raíz de la instalación de OMSI** (la carpeta que contiene `Omsi.exe`). El archivo comprimido está organizado para esa raíz:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` y los demás ensamblados del controlador `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` en la raíz;
   - el conjunto del plugin permanente `plugins\OmsiLaunch.*` (9 archivos) junto a sus plugins existentes, que nunca se tocan;
   - `.omsilaunch\` con los assets de splash, la documentación offline y los ejemplos.
3. Mantenga `release-manifest.json` junto a `OmsiLaunch.exe`. Es lo que permite que cada inicio verifique por SHA-256 los archivos del plugin instalados (`plugin.integrity.reference = manifest`); sin él solo se comprueban la presencia y la coherencia interna (`plugin.integrity.reference = self`).
4. No mueva ni cambie el nombre de nada bajo `plugins\OmsiLaunch.*` y no coloque binarios del plugin en `.omsilaunch\`.

La actualización es la misma operación: extraiga el paquete nuevo sobre los archivos anteriores mientras no haya ninguna sesión en ejecución ni ninguna recuperación pendiente (`OmsiLaunch.exe /recovery-status`). Los hashes del plugin y el manifiesto siempre deben provenir del mismo paquete (de lo contrario, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Verificar

Ejecute desde la raíz de OMSI (el argumento de instalación toma por defecto el directorio que contiene `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Resultado esperado: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; salida `0`. Una salida `102`..`106` significa que falta el runtime .NET 6 x64 o que el paquete está incompleto.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Resultado esperado: los hashes compatibles y, a continuación, los mapas detectados en esta instalación; salida `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Resultado esperado: `Plan: READY profile=Omsi23004` (en el diseño conocido; `unrecognized` en caso contrario) y salida `0` (use cualquier identidad de mapa de `/list:Maps`; el índice del punto de entrada debe ser un índice presentado de ese mapa, consulte `/list:Entrypoints /map:<identity>`). Con `--json`, el plan enumera `TouchedFiles` (los overlays de splash), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED`, y `RESOLVED_KNOWN_LAYOUT` para las capacidades nativas de inicio en el diseño conocido), `Diagnostics` (incluido `plugin.integrity.reference`) e `IsRunnable`. `Plan: NOT RUNNABLE` con salida `1` indica el motivo en `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). La planificación nunca inicia OMSI y nunca escribe en la instalación.

<a id="where-things-live-afterwards"></a>
## Dónde queda cada cosa después

| Ruta | Contenido |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Traza del host de cada sesión (se conservan las 50 sesiones más recientes) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Registro del indicador de la bandeja |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Presentes solo mientras hay una transacción pendiente; consulte [transacciones y recuperación](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Sus perfiles de sesión predefinidos; consulte [perfiles de sesión](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Assets de splash administrados |
| `<root>\.omsilaunch\docs\` | Esta documentación, offline (empiece por `README.md`; la referencia de la CLI es `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | LaunchSpec y perfil de sesión de ejemplo |

<a id="uninstall"></a>
## Desinstalar

Detenga cualquier sesión, ejecute `OmsiLaunch.exe /recovery-status` (y `/recover` si hay una recuperación pendiente) y luego elimine los archivos del producto de la raíz, `plugins\OmsiLaunch.*` y `.omsilaunch\`. Detalles en [empaquetado](../reference/packaging.md).

<a id="next"></a>
## Siguiente paso

[Primera sesión](first-session.md) · [Referencia de la CLI](../reference/cli.md) · [limitaciones conocidas](../reference/known-limitations.md)
