# Empaquetado y estructura de la release

<!-- l10n: source=reference/packaging.md source-sha256=6fff256ee354401567ab45240b0f155822e6321212dc63ea33ed6bc938b945ad -->
> Traducción de la [página original en inglés](../../../reference/packaging.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página describe el paquete de release `0.1.0-beta4` de OmsiLaunch: qué produce `tools\New-ReleasePackage.ps1`, los campos de `release-manifest.json`, cómo usa el controlador el manifiesto en runtime para verificar el conjunto de archivos del plugin permanente, cómo se instala el paquete en la raíz de una instalación de OMSI y cómo se quita de ella, qué contiene el directorio `.omsilaunch` después del uso, y los scripts de validación (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). La identidad del producto proviene de `OmsiLaunch.Version.props`. Los pasos de instalación para usuarios están en [instalación](../getting-started/installation.md); la función del conjunto de archivos del plugin en runtime está en [plugin permanente](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Identidad del producto (`OmsiLaunch.Version.props`)

| Propiedad | Valor | Se usa para |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | `product` del manifiesto, `ProductName` de Windows |
| `OmsiLaunchCompanyName` | `LMonteiro` | `CompanyName` de Windows |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/OmsiModsBR/OmsiLaunch` | `LegalCopyright` de Windows |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | `product_version` del manifiesto, `ProductVersion` de Windows, versión informativa del ensamblado (`/version`), nombre del ZIP público |
| `OmsiLaunchManagedVersion` | `0.1.0` | Base de la versión de los ensamblados administrados |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Versión del ensamblado y versión de archivo de Windows |
| `OmsiLaunchPackageAlias` | `current` | `package_alias` del manifiesto, carpeta de preparación y nombre del ZIP alias |

`Directory.Build.props` establece `InformationalVersion` en `OmsiLaunchProductVersion` sin revisión de código fuente, por lo que `OmsiLaunch.exe /version` imprime exactamente `0.1.0-beta4`.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (salida predeterminada `artifacts\release`) prepara un paquete a partir de artefactos ya compilados. Se rechaza escribir en `artifacts\release` cuando `OmsiLaunch-<product_version>.zip` ya existe, salvo que se indique `-AllowOverwritePublished`; los paquetes candidatos van a otro directorio (la validación offline usa `artifacts\candidate\post-round-a`).

Antes de la preparación, se comprueba si cada salida del build está desactualizada.

- **`OmsiLaunch.Native.x86.dll`: por contenido, no por marca de tiempo.** El build nativo escribe `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (destino `WriteOmsiLaunchNativeBuildReceipt` en el `.vcxproj`): el SHA-256 de la DLL que produjo (`output=`) y de cada fuente a partir de la cual se compiló (`source=<sha256>|<path>`: el `.cpp`, el `.rc`, el `.vcxproj` y `OmsiLaunch.Version.props`). El empaquetado rechaza la DLL cuando su hash no es la salida registrada (`does not match its build receipt`: una copia desactualizada o ajena, sin importar su marca de tiempo), cuando cambió una fuente registrada (`Native source changed after the recorded build`), cuando una fuente nativa no está cubierta por el recibo o cuando falta el recibo.
- **Shims y ensamblados administrados: por marca de tiempo.** Cada uno no debe ser más antiguo que las fuentes de su propio proyecto (`.cpp`/`.rc`/`.vcxproj` de cada shim; el propio proyecto de cada ensamblado administrado). Una entrada desactualizada aborta el empaquetado con `Stale build artifact`.

Luego el paquete se arma en un directorio de preparación completamente nuevo y con nombre único (`.staging-<guid>` en el directorio de salida), de modo que ningún archivo de una ejecución anterior pueda entrar en el conjunto de archivos. La carpeta `OmsiLaunch-current` anterior y los archivos comprimidos se reemplazan solo después de que pasaron todas las comprobaciones siguientes.

| Origen | Destino en el paquete |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | raíz |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| `assets\splash\*.bmp` de la CLI (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | raíz |
| cada archivo de `docs\` excepto `docs\localized\` (la documentación en inglés, con la misma estructura de directorios) | `.omsilaunch\docs\` (de modo que `.omsilaunch\docs\reference\cli.md`, la ruta que imprime el texto de uso de la CLI, existe; auditoría de documentación BUG-08). Los vínculos de `docs\README.md` a los resúmenes de la raíz del repositorio (`PUBLIC-API.md` y otros) solo se resuelven en el repositorio de código fuente. |
| `docs\localized\LOCALIZATION-MANIFEST.md` y `docs\localized\<locale>\**` para cada configuración regional listada en ese manifiesto (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` y `ja-JP`) | `.omsilaunch\docs\localized\` (misma estructura); si falta una configuración regional listada, el script aborta |

A continuación calcula el hash de cada archivo preparado, escribe `release-manifest.json` en la raíz del paquete como UTF-8 **sin** BOM (el resultado ya no depende de la edición de PowerShell), ejecuta `Test-ReleasePackageIntegrity.ps1` sobre la preparación, vuelve a comparar cada archivo del plugin preparado y `OmsiLaunch.Native.x86.dll` con su salida del build, comprime la preparación, **extrae el archivo comprimido en un directorio temporal nuevo y valida el conjunto de archivos extraído contra el mismo manifiesto** (el manifiesto archivado debe ser idéntico byte a byte al validado), luego publica la preparación como `OmsiLaunch-current` y el archivo comprimido como `OmsiLaunch-current.zip`, lo copia a `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) y escribe `OmsiLaunch-0.1.0-beta4.zip.sha256` con el contenido `<SHA-256>  <file name>`. Cualquier artefacto faltante aborta el script. El script no compila; ejecute primero `Invoke-OfflineValidation.ps1` (o los pasos individuales de `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Estructura del paquete

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Solo los archivos del producto en la raíz, `plugins\OmsiLaunch.*` y `.omsilaunch\` pertenecen al producto. OmsiLaunch nunca enumera, copia, calcula el hash, elimina ni restaura los plugins de terceros en `plugins\`.

## `release-manifest.json`

| Campo | Tipo | Significado |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`; debe coincidir con `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | `build_id` y `code_sha256` de cada diseño conocido (`Omsi23004`, `13ec110a...c028`); una vía rápida, no un requisito |
| `configuration` | string | `Release` o `Debug` |
| `generated_utc` | string | Hora del build en ISO-8601 |
| `files[]` | object[] | `path` (barras diagonales, relativa a la raíz del paquete), `bytes`, `sha256` (hexadecimal en mayúsculas) para cada archivo empaquetado |

El manifiesto es un dato, nunca una política ejecutable: el controlador lee solo las entradas `plugins/`. El lector acepta el archivo con o sin BOM UTF-8 (los manifiestos escritos por Windows PowerShell 5.1 antes de esta corrección tienen uno).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Uso del manifiesto en runtime (integridad del plugin)

Antes de cada plan e inicio, `OmsiLaunchService.LoadArtifacts` construye el conjunto de archivos esperado del plugin (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. El controlador busca `release-manifest.json` junto a `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Cuando está presente, `ReleaseManifest.TryReadPluginHashes` extrae los hashes de `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID` si el archivo no se puede leer como manifiesto).
2. Se calcula el hash (SHA-256) de cada archivo instalado `<root>\plugins\OmsiLaunch.*` y se compara:
   - con manifiesto: contra el hash del manifiesto; se registra el diagnóstico del plan `plugin.integrity.reference = manifest`. Archivo faltante → `OL_E_PERMANENT_PLUGIN_MISSING`; archivo presente pero no listado → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; hash diferente → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - sin manifiesto (estructura de desarrollo, o una instalación que omitió el manifiesto): solo se pueden comprobar la presencia y la autoconsistencia contra la copia empaquetada junto al controlador; `plugin.integrity.reference = self`.
3. Una falla marca el plan como no ejecutable (`OL_E_RUNTIME_ARTIFACT_MISSING` con el detalle) o rechaza el inicio (salida `7`).

Una sesión nunca prepara, incluye en un snapshot, restaura ni elimina los archivos del plugin; el conjunto de archivos es una parte permanente de la instalación. La `OmsiLaunch.Native.x86.dll` x86 se carga solo desde `plugins\`; los ensamblados administrados declaran `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Instalación en la raíz de OMSI

1. Verifique el archivo comprimido: compare `OmsiLaunch-0.1.0-beta4.zip` con `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Extraiga el archivo comprimido **directamente en la raíz de la instalación de OMSI** (el directorio que contiene `Omsi.exe`). Esto coloca los archivos de la raíz, `plugins\OmsiLaunch.*` (junto a cualquier plugin de terceros) y `.omsilaunch\`.
3. Mantenga `release-manifest.json` junto a `OmsiLaunch.exe`: habilita la integridad del plugin basada en el manifiesto. El manifiesto y los binarios deben provenir del mismo paquete: binarios nuevos sobre un manifiesto más antiguo (o al revés) hacen que todo inicio falle con `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` ahora copia el manifiesto junto con los archivos del producto; antes lo omitía, lo que dejaba un manifiesto más antiguo junto a binarios más nuevos (Round A RA-007).
4. Compruebe la coherencia en modo de solo lectura con `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` debe ser `true`.
5. No mueva los binarios del plugin a `.omsilaunch\` y no cambie el nombre de `plugins\OmsiLaunch.*`.
6. Verifique con `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` y un `/plan` (consulte [primera sesión](../getting-started/first-session.md)).

Una sesión nunca sobrescribe los archivos `.omsilaunch\assets\splash\*.bmp` existentes (un conjunto de assets administrado explícitamente persiste); sobrescribirlos al extraer un paquete nuevo es una acción deliberada del usuario.

<a id="the-omsilaunch-directory-after-use"></a>
## El directorio `.omsilaunch` después del uso

| Ruta | Creado por | Duración |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | el paquete, o copiado en la primera sesión con splash administrado | persistente |
| `docs\`, `examples\` | el paquete | persistente |
| `session-profiles\<id>\profile.yaml` | el usuario | persistente; consulte [perfiles de sesión](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | cada sesión | se conserva para las 50 sesiones más recientes; los archivos más antiguos con prefijo de sesión se eliminan cuando se inicia una sesión nueva |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, herramientas de validación | misma retención (prefijo de sesión) |
| `diagnostics\tray-host.log` | indicador de la bandeja | persistente, se agrega al final |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | persistente (sin prefijo de sesión) |
| `journal.json` | la transacción | existe desde `Prepared` hasta `Restored`; si queda uno, hay una recuperación pendiente (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | la transacción | snapshots de los archivos modificados; se eliminan después de la restauración |

Ningún dato sale de la computadora. Consulte [transacciones y recuperación](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Desinstalación

1. Asegúrese de que no haya ninguna sesión en ejecución (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) ni ninguna recuperación pendiente (`OmsiLaunch.exe /recovery-status`; ejecute `/recover` si `pending` es `true`), de modo que los archivos de OMSI ya estén restaurados.
2. Elimine `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. No toque los demás plugins.
3. Elimine los archivos del producto en la raíz listados en la estructura anterior (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Elimine `.omsilaunch\` (esto borra sus perfiles de sesión y sus diagnósticos). Nunca lo elimine mientras exista `journal.json`.

Una sesión completada restaura cada archivo que le pertenecía, por lo que no se necesita ninguna limpieza adicional. Los archivos que OMSI escribe por sí mismo mientras se ejecuta (por ejemplo `[last_map]` en `options.cfg`, cachés, `laststn.osn`, logs) son el estado normal de OMSI y no se revierten; consulte [transacciones y recuperación](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Scripts de validación

| Script | Propósito | Modifica OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Compila `OmsiLaunch.sln` con las advertencias como errores y los tres proyectos nativos (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) mediante MSBuild; luego ejecuta todas las suites offline: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` y `OmsiLaunch.DocumentationTests` salvo que se omita, y después la regresión de empaquetado `Test-PackagingPipeline.ps1` (se omite con `-SkipNative`). Imprime `OFFLINE VALIDATION PASSED`/`FAILED`. | No |
| `tools\New-ReleasePackage.ps1` | Protección contra artefactos desactualizados, preparación, manifiesto, autocomprobación de integridad, ZIP, suma de verificación (arriba). | No |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifica que el manifiesto liste exactamente los archivos empaquetados con tamaño y SHA-256 coincidentes, que el conjunto de archivos requerido (tres archivos ejecutables, el controlador, los nueve archivos del plugin permanente, incluido `OmsiLaunch.Native.x86.dll`) esté presente y que la configuración sea `Release`. Con `-InstallationRoot` compara los archivos del producto de la instalación con el paquete **en modo de solo lectura**. Salida `0` = coherente. | No (solo lectura) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produce un paquete candidato a partir de una preparación limpia en `artifacts\candidate\post-round-a` y exige que el conjunto de archivos preparado y el archivo comprimido reextraído pasen la verificación de integridad. Demuestra que el gate de integridad rechaza una DLL manipulada, un `Native.x86` manipulado o antiguo, una copia desactualizada del plugin, un archivo listado eliminado, un archivo inesperado, un hash del manifiesto modificado o mal formado, entradas duplicadas (exactas, por mayúsculas/minúsculas, por separador), rutas padre y absolutas y JSON no válido; que el empaquetador rechaza un `Native.x86` antiguo colocado en la salida del build (incluso con una marca de tiempo más reciente) y un recibo cuyas fuentes cambiaron; y que el archivo comprimido publicado nunca se sobrescribe. Las salidas del build se restauran byte a byte. Lo ejecuta `Invoke-OfflineValidation.ps1`. | No |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Extrae el ZIP en `artifacts\release\identity-verification`, comprueba `product`/`product_version`/`package_alias` y verifica `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` de cada `.exe`/`.dll` excepto `nethost.dll` y `YamlDotNet.dll`, el `InternalName`/`OriginalFilename` de ambos shims, y que `OmsiLaunch.exe` contenga un ícono incrustado. | No |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Valida el archivo ejecutable Release empaquetado contra una instalación real: el manifiesto debe ser `Release`, no debe contener rutas `Debug` ni `runtime/plugin/` y debe instalar `plugins/OmsiLaunch.*`; los cuatro assets de splash deben existir. Ejecuta tres casos de `/plan` (administrado predeterminado, administrado con assets personalizados, `/splash:Unset`). Con `-RunOmsi` (requiere `-InstallPackage`) inicia cada caso con `/observe-seconds`, vigila `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_PTB.bmp` durante la sesión y verifica la restauración exacta, la ausencia de `Omsi.exe`, la ausencia de `journal.json`, la salida `0`, los hashes sin cambios de los plugins de terceros y un conjunto sin cambios de archivos del plugin permanente. Escribe `.omsilaunch\diagnostics\release-presentation-validation.json`. | Sí con `-RunOmsi` (con alcance de sesión, restaurado) |

Ambos scripts `Test-*` leen `OmsiLaunch.Version.props` para conocer la versión esperada.
