# El plugin permanente

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Traducción de la [página original en inglés](../../../concepts/permanent-plugin.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

OmsiLaunch controla OMSI desde dentro del proceso de OMSI mediante un plugin que se instala una sola vez, en `plugins\OmsiLaunch.*`, como parte del producto. Una sesión nunca lo prepara, copia, incluye en un snapshot, restaura ni elimina. Esta página explica qué contiene el conjunto de archivos del plugin, cómo lo carga OMSI, cómo lo valida el host antes de cada inicio, cómo se comunican el host y el plugin (handoff, telemetría, buzón de runtime) y qué hace el plugin cuando OMSI se inicia sin OmsiLaunch. Fuentes: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, los tres almacenes de memoria compartida), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` y `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## El conjunto de archivos (9 archivos)

| Archivo en `plugins\` | Función |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Descriptor de plugin de OMSI. Su contenido es `[dll]` seguido de `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Shim nativo x86 de exportaciones generado por DNNE 2.0.6. Exporta la ABI de plugins de OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) y aloja el runtime de .NET. Incluye el recurso de versión del producto. |
| `OmsiLaunch.Plugin.dll` | Plugin administrado (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Manifiesto de dependencias de .NET del plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Configuración del runtime de .NET (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Formatos de transmisión y registros públicos compartidos con el host. |
| `OmsiLaunch.Builds.Omsi23004.dll` | El perfil del build: huella del ejecutable, variables globales, estructuras de objetos, direcciones de métodos. |
| `OmsiLaunch.Interop.dll` | Lectores y escritores de memoria dentro del proceso construidos sobre el perfil. |
| `OmsiLaunch.Native.x86.dll` | Puente nativo (C++): validación del build, hook de inicio headless, aplicación de la hora, MakeVehicle, PlaceRandomBus, supresión de las texturas de Internet, acceso al dispositivo D3D9. |

`RuntimeArtifactSet.Load` deriva esta lista del propio directorio `plugins\` del controlador: los cuatro archivos con nombre fijo (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), cada `OmsiLaunch.*.dll` de ese directorio excepto `OmsiLaunch.PluginNE.dll`, y el puente nativo. `OmsiLaunch.Plugin.dll` debe estar entre ellos. Nunca se arrastran DLL arbitrarias a OMSI. El empaquetador de la versión (`tools/New-ReleasePackage.ps1`) escribe exactamente los nueve archivos anteriores.

<a id="how-omsi-loads-it"></a>
## Cómo lo carga OMSI

1. OMSI enumera `plugins\*.opl` y carga la DLL indicada en `OmsiLaunch.Plugin.opl`: `OmsiLaunch.PluginNE.dll`.
2. El shim de DNNE inicia dentro del proceso de OMSI el runtime x86 de .NET 6 descrito por `OmsiLaunch.Plugin.runtimeconfig.json` y resuelve las exportaciones administradas de `OmsiLaunch.Plugin.dll`.
3. OMSI llama a `PluginStart`. OMSI puede llamarla más de una vez durante el arranque; solo se atiende la primera llamada (protección con `Interlocked.Exchange`), porque un inicio correcto es propietario de un hook nativo de un solo uso. Las llamadas posteriores retornan inmediatamente.
4. `PluginStart` comprueba primero `OMSILAUNCH_INTERNET_TEXTURES_MODE`: cuando es `Disabled`, se aplica la supresión nativa del descargador (`internet-textures.suppressed`, o `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` lee el handoff (véase más abajo), valida el build dentro del proceso, arma el hook de inicio headless, abre el buzón de runtime y programa el inicio del mundo en el thread de la UI de OMSI mediante una callback de `SetTimer`. Ningún comando de runtime se ejecuta nunca en un thread de trabajo de IPC; todo se ejecuta en la callback del temporizador, en el thread de la UI original de OMSI.
6. `PluginFinalize` detiene el temporizador, cierra el runtime (`NativeD3DShutdown`) y restaura el parche de las texturas de Internet.

Las exportaciones `AccessVariable`, `AccessTrigger`, `AccessStringVariable` y `AccessSystemVariable` están vacías; OmsiLaunch no usa el canal de plugins de variables de script de OMSI.

<a id="integrity-validation-before-every-start"></a>
## Validación de integridad antes de cada inicio

`RuntimeArtifactSet.ValidateInstalled` se ejecuta durante `StartSessionAsync` (después de la recuperación temprana y antes de preparar la transacción) y durante `PlanSessionAsync` (solo presencia, mediante `LoadArtifacts`). El diagnóstico del plan `plugin.integrity.reference` indica qué referencia se utilizó.

| Situación | Referencia | Comprobación por archivo | Errores |
| --- | --- | --- | --- |
| `release-manifest.json` presente junto a `OmsiLaunch.exe` (paquete instalado) | `manifest` | El archivo instalado debe existir y su SHA-256 debe ser igual a la entrada del manifiesto para `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (el manifiesto no tiene entrada para un archivo necesario), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Sin manifiesto (estructura de desarrollo) | `self` | Presencia más coherencia interna: el hash del archivo instalado debe coincidir con el del archivo del propio directorio `plugins\` del controlador | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifiesto ilegible o mal formado (falta el array `files`, entrada sin `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Conjunto de archivos incompleto en el directorio del controlador | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (plan no ejecutable); la CLI notifica además `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` cuando falta `plugins\OmsiLaunch.Plugin.opl` o `OmsiLaunch.Native.x86.dll` |

Solo se utilizan las entradas `plugins/` del manifiesto; el manifiesto son datos, nunca una política. El empaquetador de la versión genera el manifiesto con el SHA-256 de cada archivo preparado. Cuando el controlador se ejecuta desde la raíz de OMSI (la estructura de la versión), el origen y el destino son el mismo directorio; por eso, sin manifiesto, la comprobación se reduce a presencia y coherencia interna, y por eso los paquetes publicados incluyen siempre `release-manifest.json`. Solución ante una discrepancia: reinstalar el paquete para que `plugins\` y `release-manifest.json` concuerden.

Antes de parchear nada, el plugin resuelve y verifica cada destino nativo del catálogo de builds contra la imagen cargada y entrega la tabla verificada a `OmsiLaunch.Native.x86.dll`, que no contiene ninguna dirección de OMSI (telemetría `native.capabilities`). Un inicio cuya capacidad `boot.headless-start` o de modo de mundo no está disponible falla con `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); un formulario de inicio activo en un estado inesperado falla con `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Consulta [compatibilidad](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Del host al plugin: variables de entorno

`CreateProcessW` inicia `Omsi.exe` con el entorno del proceso padre más:

| Variable | Valor | Consumidor |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID de la sesión (formato `D`) | `CurrentRuntimeControl` etiqueta con él los handles D3D (formato `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` abre esta asignación en solo lectura |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` o `Override` | `PluginStart` (solo `Disabled` tiene efecto dentro del proceso) |

El host crea las tres asignaciones antes de que se inicie el proceso (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) y las libera cuando termina la tarea del ciclo de vida de la sesión. Son objetos del kernel con nombre y la DACL predeterminada del usuario que lanza; cualquier proceso del mismo usuario puede abrirlos (modelo de confianza aceptado para el mismo usuario, consulta [limitaciones conocidas](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## El handoff de arranque

Un registro asignado en memoria, de solo lectura y con estructura fija (`StartupHandoffWire`, magic `OLSH`, versión 4; el lector sigue aceptando la versión 3). La cabecera de 64 bytes contiene el magic, la versión, el tamaño de la cabecera, el tamaño total, el GUID de la sesión, el tamaño de la carga útil y el SHA-256 de la carga útil. La carga útil contiene `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 con prefijo de longitud), `PresentedEntrypointIndex`, `WorldMode`, flags (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` y `TimeMode`. El plugin vuelve a calcular el hash de la carga útil y rechaza cualquier discrepancia (`plugin.handoff.invalid`, error del host `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Una asignación mayor de 1 MiB o con un tamaño incoherente se rechaza de la misma manera.

El plugin solo acepta un handoff cuando `WorldMode` es `NewMap` o `SavedSituation`, `HeadlessStart` está activado, `PlayerVehicleEnabled` está desactivado, ambos modos de fecha y hora son `Unset` y una situación guardada nombra su `.osn`. Cualquier otra cosa es `plugin.request.unsupported` (error del host `OL_E_CAPABILITY_UNAVAILABLE`). El host siempre activa `HeadlessStart`.

<a id="telemetry-slot"></a>
## Slot de telemetría

`OmsiLaunch.Telemetry.<session>` es un anillo de eventos (Beta 4, `TelemetryRingWire`): una cabecera de 64 bytes (magia `OLTR`, versión 1, número de slots, tamaño de slot, secuencia confirmada) y 64 slots numerados de 2048 bytes, cada uno con un evento JSON UTF-8 `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` es el reloj monotónico de sesión del plugin). El plugin es el único escritor: invalida un slot, lo escribe, confirma la secuencia del slot y después publica la secuencia del anillo. El host lee todos los eventos confirmados desde su última lectura, en orden, cada 100 ms; un slot sobrescrito mientras se copiaba se cuenta como perdido y nunca se entrega incoherente. Un host que se queda más de 64 eventos atrás recibe el evento de runtime `telemetry.lost` con el recuento exacto. Un evento mayor que un slot se sustituye por `telemetry.oversized`, que lo nombra. Los eventos se añaden a `SessionStatus.RuntimeEvents` (limitado a los 256 últimos) y dirigen el ciclo de vida semántico (`plugin.started`, `world.starting`, `gameplay.entered`, eventos de módulos y de fallo).

<a id="runtime-command-mailbox"></a>
## Buzón de comandos de runtime

`OmsiLaunch.Runtime.<session>` es un buzón de 64 KiB de una sola solicitud en curso: `state` (int32 en 0: 0 inactivo, 1 solicitado, 2 respondido), `length` (int32 en 4), envelope en 8. Los envelopes son registros `RuntimeCommandWire` (magic `OLRC`, versión 1, cabecera de 72 bytes con tipo, longitud total, GUID de la sesión, id de la solicitud, longitud de la carga útil y SHA-256 de la carga útil JSON UTF-8). El plugin sondea el buzón desde su temporizador del thread de la UI (50 ms una vez cargado el mundo), ejecuta el comando en ese thread y publica la respuesta solo si el slot sigue conteniendo el mismo id de solicitud; una solicitud que el host abandonó por timeout nunca recibe respuesta. Una respuesta demasiado grande se sustituye por un error tipado `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Los detalles y los timeouts figuran en [control de runtime](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Política de búsqueda de DLL

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` y los ensamblados de la CLI declaran `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Las importaciones nativas (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) se resuelven solo desde el propio directorio del ensamblado (`plugins\`) o desde el directorio del sistema de Windows; la raíz de OMSI y `PATH` nunca se examinan. Por tanto, `OmsiLaunch.Native.x86.dll` se carga desde `plugins\` y desde ningún otro sitio.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Cuando OMSI se inicia sin OmsiLaunch

Como el conjunto de archivos es permanente, OMSI carga `OmsiLaunch.PluginNE.dll` en cada inicio, incluidos los inicios desde Steam o desde el escritorio. En ese caso:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` no está definida, por lo que no se aplica ningún parche al descargador.
- `OMSILAUNCH_HANDOFF_NAME` no está definida, por lo que `PluginRuntime.Start` emite `plugin.handoff.invalid` y devuelve `false`. `CurrentTelemetrySink.Emit` retorna inmediatamente cuando `OMSILAUNCH_TELEMETRY_NAME` no está definida, así que no se escribe nada en ningún sitio.
- Ni validación del build, ni hook nativo, ni buzón, ni temporizador. El plugin permanece cargado pero inerte; OMSI se comporta como si el plugin no estuviera.
- `PluginFinalize`, al salir de OMSI, llama a la rutina nativa de restauración y a `NativeD3DShutdown`, que no hacen nada cuando no se instaló nada.

Por tanto, no es necesario quitar el `.opl` para ejecutar OMSI con normalidad.

<a id="non-interference-with-third-party-plugins"></a>
## No interferencia con plugins de terceros

Las sesiones nunca enumeran, calculan el hash, copian, eliminan ni restauran otros archivos de `plugins\`. El plugin no usa el canal `AccessVariable` de OMSI y no toca el estado de otros plugins. Los únicos parches dentro del proceso son el hook perfilado de inicio headless (una redirección de VMT de un solo uso armada para la sesión), la supresión opcional de las texturas de Internet (restaurada en `PluginFinalize`) y la interceptación de `Reset` del dispositivo D3D9 que se usa para seguir el ciclo de vida de las texturas.

<a id="difference-from-omsihook"></a>
## Diferencia con OmsiHook

OmsiLaunch **no tiene ninguna dependencia de runtime** de OmsiHook ni de ningún binario de OmsiHook: los únicos paquetes referenciados son `DNNE` 2.0.6 y `YamlDotNet` 15.1.2, y en ninguna parte del producto hay un `using OmsiHook` ni P/Invoke a DLL de OmsiHook. Lo que OmsiLaunch sí comparte con OmsiHook es conocimiento derivado: las estructuras de objetos y varios envoltorios de lectura se contrastaron con el checkout fijado de OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) frente al ejecutable exacto `Omsi23004_692EBFBF`. La atribución y los términos de la licencia están en `THIRD-PARTY-NOTICES.md`, y la matriz de reutilización por archivo en `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook se inyecta desde un proceso independiente y expone punteros sin procesar; OmsiLaunch se ejecuta dentro del proceso, expone únicamente handles opacos limitados a la sesión y elimina cualquier dirección nativa de los resultados públicos (consulta [capacidades](../reference/capabilities.md)).
