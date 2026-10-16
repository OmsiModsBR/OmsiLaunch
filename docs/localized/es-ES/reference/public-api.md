# Referencia de la API pública (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> Traducción de la [página original en inglés](../../../reference/public-api.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página es la referencia normativa de la API pública gestionada de OmsiLaunch 0.1.0-beta4: el ensamblado `OmsiLaunch.Api` (contratos) y el punto de entrada para integradores `OmsiLaunchService` en `OmsiLaunch.Core`. Documenta únicamente lo que hace el código actual. Todo lo que un integrador puede llamar, recibir u observar figura aquí con su nivel de estabilidad; lo que no aparece no es una superficie de integración.

El [inventario de la API pública](public-api-inventory.md) generado enumera cada tipo y miembro público de `OmsiLaunch.Api`, `OmsiLaunch.Core` y `OmsiLaunch.Process` con su firma y su estabilidad; un control de documentación falla cuando el inventario y los ensamblados difieren. Esta página explica la semántica.

Páginas relacionadas: [referencia de LaunchSpec](launchspec.md), [códigos de error](errors.md), [ciclo de vida de la sesión](../concepts/session-lifecycle.md), [transacciones y recuperación](../concepts/transactions-and-recovery.md), [control de runtime](runtime-control.md), [capacidades](capabilities.md), [plano de control local](local-control.md), [códigos de salida](exit-codes.md), [estado de la validación en runtime](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Vocabulario de estabilidad

| Nivel | Significado en esta página |
| --- | --- |
| `STABLE_BETA` | El contrato está congelado para la línea de protocolo 0.1 y la ruta está validada en runtime en `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Se puede llamar y está probado, pero el contrato o la evidencia de runtime pueden cambiar antes de que pase a ser estable. |
| `PARTIAL` | Presente en el contrato; solo una parte del comportamiento está implementada o validada (el texto indica qué parte). |
| `INTERNAL` | Público en el ensamblado por motivos técnicos (el puente comparte el tipo), pero no es una superficie de integración; puede cambiar sin previo aviso. |
| `UNAVAILABLE` | Presente en el contrato, pero la compilación actual lo rechaza. |

<a id="assembly-overview"></a>
## Visión general de los ensamblados

| Ensamblado | Función para los integradores |
| --- | --- |
| `OmsiLaunch.Api` | Contratos puros: records, enums, `IOmsiLaunch`, registro de capacidades, catálogo de errores, formatos de transmisión, utilidades D3D. No contiene ningún `IntPtr`, `nint`, handle de Win32, dirección nativa ni objeto de proceso. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (la implementación de `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` y `CurrentWindowsX64Platform` (el único adaptador de plataforma), `InstallationLease`. Necesarios para construir el servicio. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Ensamblados de implementación. Sus tipos públicos son `INTERNAL` para los integradores. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Punto de entrada: `OmsiLaunchService` y `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parámetro | Valor válido | No válido / predeterminado |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (espacio de nombres `OmsiLaunch.Process`). Detecta la plataforma, crea el proceso de OMSI con `CreateProcessW`, espera a que termine y lo termina. | No se distribuye ninguna otra implementación. Un `IRuntimePlatform` personalizado es `INTERNAL`. |
| `PluginBuildDirectory` | Directorio que contiene los archivos de referencia del conjunto de archivos del plugin permanente: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` y cada `OmsiLaunch.*.dll` del conjunto gestionado (debe incluir `OmsiLaunch.Plugin.dll`). En un paquete instalado es `<package>\plugins`. | Directorio o archivo ausente: `PlanSessionAsync` devuelve un plan no ejecutable con `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Ruta de `OmsiLaunch.Native.x86.dll` (en el paquete: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Igual que en la fila anterior. |
| `ReleaseManifestPath` | `release-manifest.json` junto a `OmsiLaunch.exe`, cuando existe. Proporciona el SHA-256 esperado de cada archivo de `plugins/` (`plugin.integrity.reference = manifest`). | `null` (estructura de desarrollo): de los archivos instalados solo se comprueba su presencia y su coherencia interna con el conjunto de referencia (`plugin.integrity.reference = self`). Manifiesto mal formado: `OL_E_RELEASE_MANIFEST_INVALID`. |

El servicio lee estas rutas en cada `PlanSessionAsync` y `StartSessionAsync`; nunca copia, prepara ni elimina archivos del plugin (consulta [plugin permanente](../concepts/permanent-plugin.md)). La CLI construye el servicio exactamente así (`tools/OmsiLaunch.Cli/Program.cs`):

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

Crea un único servicio por proceso y compártelo. Estabilidad: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Reglas de propiedad de la sesión

| Regla | Detalle |
| --- | --- |
| Un propietario por instalación | `StartSessionAsync` adquiere el lease de la instalación, un semáforo con nombre `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, y lo mantiene hasta que el supervisor ha restaurado la instalación. Un segundo inicio sobre la misma raíz desde cualquier proceso de la misma sesión de inicio de Windows falla con `OL_E_INSTALLATION_BUSY` (notificado como una sesión `Failed`, consulta `StartSessionAsync`). El lease es por sesión de inicio de Windows, no abarca varias sesiones de inicio, y no se libera mientras otro proceso mantenga un handle hacia él (riesgo aceptado). |
| Los handles son locales al proceso | `SessionHandle` envuelve el `Guid` de la sesión. Solo tiene sentido para la instancia de `OmsiLaunchService` que lo devolvió. Un handle construido a partir de un `Guid` conocido en otro proceso (o en otra instancia del servicio) produce `KeyNotFoundException`. El control entre procesos pasa por el [plano de control local](local-control.md), no por handles. |
| Llama siempre a `CloseAsync` | Desde `StartSessionAsync` en adelante, el proceso es propietario de una transacción duradera. `CloseAsync` solicita la detención canónica cuando es necesario, espera al supervisor (salida del proceso, restauración exacta, liberación del lease) y olvida la sesión. Debe llamarse en todas las rutas de salida, incluso después de un estado `Failed`. Sin ella, la entrada de la sesión permanece en memoria; la restauración en sí la realiza el supervisor en cualquier caso. |
| Las sesiones fallidas siguen siendo sesiones | Un inicio que falla después de que `StartSessionAsync` haya retornado notifica `SessionState.Failed`; el handle sigue siendo válido para `GetStatusAsync`/`WaitForAsync` hasta `CloseAsync`. |
| Los planes se vuelven a comprobar | `StartSessionAsync` vuelve a calcular el hash de `Omsi.exe` y vuelve a planificar la especificación; un plan que ya no es ejecutable se rechaza con `OL_E_PLAN_NOT_RUNNABLE`. |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

Hechos comunes a todos los métodos:

- Los handles desconocidos o ya cerrados lanzan `KeyNotFoundException` («Unknown OmsiLaunch session.»).
- Ningún método requiere una sesión en ejecución (Running), salvo `ExecuteRuntimeAsync`.
- Las excepciones que llevan un código de OmsiLaunch colocan el código al principio de `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). La CLI extrae los códigos de los mensajes de la misma manera (`CliProgram.Classify`).
- Instalación compatible: OMSI 2. Las capacidades nativas se resuelven y se verifican por build en el proceso; una capacidad no disponible en un build falla con `OL_E_CAPABILITY_UNRESOLVED`. Consulta [compatibilidad](compatibility.md).

<a id="complete-minimal-example"></a>
### Ejemplo mínimo completo

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Finalidad | Compilar un `LaunchSpec` en un `SessionPlan` sin iniciar OMSI: validar la especificación, detectar la plataforma, obtener la huella de `Omsi.exe`, resolver las identidades de contenido, calcular las mutaciones de archivos planificadas, enumerar las capacidades requeridas y no compatibles, y decidir `IsRunnable`. Capacidad pública `session.plan`. |
| Parámetros | `spec`: un `LaunchSpec` completamente rellenado (consulta la [referencia de LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (los ocho diccionarios) y `Behavior` no deben ser null; los miembros opcionales pueden ser `null`. `RootPath` debería ser un directorio absoluto; una raíz vacía se registra como `OL_E_INSTALLATION_NOT_FOUND`, pero la sonda de plataforma sobre una ruta vacía lanza `ArgumentException` antes de que se devuelva el plan, así que nunca pases una raíz vacía. |
| Devuelve | `SessionPlan` con un `SessionId` nuevo, `BuildProfileId` = la disposición conocida (`"Omsi23004"`) o `"unrecognized"` para un build de OMSI 2 sin disposición conocida, el `Spec` de entrada, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (rutas de destino `plugins\OmsiLaunch.*` más `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (un `PlannedModule` por cada [módulo de runtime](runtime-modules.md) solicitado o activado por contenido; accesor `EffectivePlannedModules`). `IsRunnable` es `true` exactamente cuando ningún código de diagnóstico empieza por `OL_E_`. Los diagnósticos informativos (`plugin.integrity.reference` con el mensaje `self` o `manifest`, `session_profile.selected`) nunca hacen que un plan deje de ser ejecutable. |
| Errores incluidos en el resultado | Todo error de planificación es un diagnóstico, no una excepción: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (el mensaje lleva el código de splash/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (el conjunto de archivos del plugin instalado en `plugins\` se verifica contra el manifiesto de la versión durante la planificación), `OL_E_RUNTIME_ARTIFACT_MISSING` (el mensaje puede llevar `OL_E_RELEASE_MANIFEST_INVALID`). Condiciones completas: [reglas de validación de LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Lanza | `OperationCanceledException` si el token ya está cancelado a la entrada (el único punto de comprobación); `ArgumentException`/`NotSupportedException` para rutas raíz sintácticamente no válidas; `NullReferenceException`/`ArgumentNullException` para miembros obligatorios null; `System.Text.Json.JsonException` para un manifiesto de la versión sintácticamente no válido. |
| Cancelación | Se comprueba una vez a la entrada. Después, la planificación es trabajo síncrono sobre el sistema de archivos. |
| Requiere una sesión en ejecución | No. |
| Modifica el estado de OMSI | No. |
| Modifica el sistema de archivos | No (lee `Omsi.exe`, archivos de contenido, el conjunto de archivos del plugin y el manifiesto). Aquí no se validan los valores de los ajustes (solo la existencia de la clave y si se puede escribir); un valor no válido falla en el inicio con `OL_E_INVALID_SETTING_VALUE`. |
| Transacción / restauración | Ninguna. |
| Limitaciones | Solicitar cualquier modo de `Date`/`Time`/`Year` distinto de `Unset`, cualquier modo de `Weather` distinto de `Unset`, cualquier campo de `PlayerVehicle`, documentos de `Input`, `EntrypointIdentity` o `WorldMode.LastMapState` produce `OL_E_CAPABILITY_UNAVAILABLE` y un plan no ejecutable en esta build (entradas de `UnsupportedRequestedFeatures` que llevan su evidencia de catálogo, `UNAVAILABLE` o `PARTIAL`). |
| Estabilidad | `STABLE_BETA`. |
| Ejemplo | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Finalidad | Iniciar una sesión gestionada y transaccional de OMSI a partir de un plan ejecutable: adquirir el lease de la instalación, recuperar un diario obsoleto, validar el conjunto de archivos del plugin permanente, tomar el snapshot de los archivos de sesión y aplicar sus overlays, crear el handoff de arranque, el slot de telemetría y el buzón de runtime, iniciar `Omsi.exe`, registrar el proceso en el diario y entregar la sesión a un supervisor en segundo plano. Capacidad pública `session.start`. |
| Parámetros | `plan`: un `SessionPlan` con `IsRunnable == true`. La especificación contenida en el plan se vuelve a planificar; del plan del llamador solo se conserva `plan.SessionId`. `plan.Spec.Behavior.StartupTimeoutSeconds` debe estar entre 1..600. |
| Devuelve | `SessionHandle(plan.SessionId)` en cuanto `Omsi.exe` se ha creado y registrado (estado `WaitingForPlugin`), o en cuanto la ruta de inicio ha fallado (estado `Failed`). No espera a la fase de juego; usa `WaitForAsync(session, SessionState.Running, ...)`. |
| Lanza | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` cuando `plan.IsRunnable` es false; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` cuando el nuevo plan no es ejecutable (por ejemplo, `Omsi.exe` ha cambiado, se ha eliminado contenido o falta el conjunto de archivos del plugin); `InvalidOperationException("Duplicate session id.")` cuando todavía está registrada una sesión con el mismo id (llama antes a `CloseAsync`); `ArgumentOutOfRangeException` cuando `StartupTimeoutSeconds` está fuera de 1..600; `OperationCanceledException` cuando se cancela antes o durante la nueva planificación; y todo lo que lanza `PlanSessionAsync`. En todos los casos en que se lanza una excepción no se registra ninguna sesión. |
| Errores incluidos en el resultado | Cualquier fallo posterior a la nueva planificación se captura dentro de la ruta de inicio: la sesión se registra, su estado es `Failed` y sus diagnósticos contienen `OL_E_START_SESSION`, cuyo mensaje es el mensaje interno (que empieza por el código interno cuando lo hay): `OL_E_INSTALLATION_BUSY` (lease ocupado o un proceso de OMSI registrado en el diario sigue vivo), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (solo cuando el reintento diferido con los overlays de esta sesión sigue sin poder demostrar la propiedad), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. La limpieza puede añadir `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (salida de OMSI no confirmada; el diario se conserva) u `OL_E_RESTORE_FAILED`. Los fallos posteriores los notifica el supervisor (consulta [ciclo de vida de la sesión](../concepts/session-lifecycle.md)). |
| Cancelación | Antes o durante la nueva planificación: lanza una excepción. Después, el token se pasa a la transacción y a la creación del proceso; una cancelación en ese punto se trata como cualquier fallo de inicio (`Failed` + `OL_E_START_SESSION: The operation was canceled.`): el proceso (si se ha creado) se termina y la instalación se restaura. |
| Requiere una sesión en ejecución | No. |
| Modifica el estado de OMSI | Sí: crea el proceso de OMSI con las variables de entorno `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Modifica el sistema de archivos | Sí, dentro de la raíz de la instalación: `.omsilaunch\diagnostics\<sessionId>-host.log` (retención: las 50 sesiones más recientes), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (se copia una vez para el splash gestionado), overlays de la sesión (parches de `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), eliminaciones de la sesión (destinos ITX, `Texture\standard.ipr`, `closecheck`) y eliminación permanente de un `closecheck` obsoleto preexistente cuando `SuppressStaleClosecheckWarning` es true (diagnóstico `closecheck.stale-removed`). |
| Transacción / restauración | Abre la transacción (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Todas las rutas de salida de la sesión terminan en una restauración. Consulta [transacciones y recuperación](../concepts/transactions-and-recovery.md). |
| Limitaciones | Solo `WorldMode.NewMap` con `PresentedEntrypointIndex` y `WorldMode.SavedSituation` llegan a la fase de juego. `WorldMode.LastMapState` es `UNAVAILABLE`. Las solicitudes de vehículo del jugador nunca llegan a este método, porque no son ejecutables en el momento de la planificación. |
| Estabilidad | `STABLE_BETA` (los ciclos de vida NEW_MAP y SAVED_SITUATION están validados en runtime). |
| Ejemplo | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidad | Leer el estado semántico del ciclo de vida, los diagnósticos recogidos hasta el momento y la lista acotada de eventos de runtime. Capacidad pública `session.status`. |
| Parámetros | `session`: un handle devuelto por `StartSessionAsync` y todavía no cerrado. |
| Devuelve | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: un snapshot inmutable (las matrices se copian bajo el bloqueo de la sesión). `RuntimeEvents` nunca es `null` para una sesión activa. |
| Lanza | `KeyNotFoundException` para handles desconocidos o cerrados. En los demás casos, nunca lanza excepciones. |
| Cancelación | El token se ignora (la llamada se completa de forma síncrona). |
| Requiere una sesión en ejecución | No. |
| Modifica OMSI / sistema de archivos / transacción | No / No / Ninguna. |
| Estabilidad | `STABLE_BETA`. |
| Ejemplo | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Finalidad | Sondear (cada 100 ms) hasta que la sesión esté en `state` o en un estado terminal (`Completed`, `Failed`), o hasta que venza el timeout; después, devolver el estado actual. |
| Parámetros | `state`: cualquier `SessionState`. Esperar un estado transitorio que ya se ha superado (o que nunca se establece, consulta [ciclo de vida de la sesión](../concepts/session-lifecycle.md)) espera hasta un estado terminal o hasta el timeout. `timeout`: cualquier `TimeSpan` no negativo o `Timeout.InfiniteTimeSpan`. |
| Devuelve | El estado en el momento en que terminó la espera. Si vence el timeout, se devuelve el estado, no una excepción: comprueba `State` tú mismo. Una espera de `Running` que termina en `Failed` retorna inmediatamente con los diagnósticos del fallo. |
| Lanza | `KeyNotFoundException`; `OperationCanceledException` cuando se cancela el token del llamador (solo se propaga la cancelación del llamador; el timeout interno no). |
| Cancelación | El token del llamador se respeta en cada intervalo de 100 ms. |
| Requiere una sesión en ejecución | No. |
| Modifica OMSI / sistema de archivos / transacción | No / No / Ninguna. |
| Estabilidad | `STABLE_BETA`. |
| Ejemplo | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidad | Solicitar la detención canónica. Establece el flag de detención y retorna inmediatamente; el supervisor detecta el flag dentro de su bucle de 100 ms, llama a `TerminateProcess` sobre `Omsi.exe`, espera a la salida, marca el diario como `ProcessExited`, restaura todos los archivos propiedad de la sesión y libera el lease. Se trata de una terminación forzada: la rutina de cierre propia de OMSI no se ejecuta y OMSI no reescribe `options.cfg` al salir (deliberado, protege la transacción). El cierre cooperativo con `WM_CLOSE` no está implementado (decisión de producto; en el cierre de validación en runtime, OMSI no se cerró en los 30 s siguientes a `WM_CLOSE`, `L05b`). Capacidad pública `session.stop`. |
| Parámetros | `session`. |
| Devuelve | Una tarea completada; no espera a la terminación ni a la restauración. Usa `WaitForAsync(session, SessionState.Completed, ...)` para observar la finalización. |
| Lanza | `KeyNotFoundException`. |
| Cancelación | El token se ignora. |
| Requiere una sesión en ejecución | No. Idempotente; una detención solicitada antes de que arranque el supervisor se atiende en cuanto arranca; una detención sobre una sesión terminal no hace nada. |
| Modifica el estado de OMSI | Sí: termina el proceso de OMSI (código de salida 1). |
| Modifica el sistema de archivos | Indirectamente: desencadena la restauración, la eliminación del diario y la eliminación de las copias de seguridad por parte del supervisor. |
| Transacción / restauración | Desencadena `ProcessExited` → `Restoring` → `Restored`. Los cambios del lado del runtime realizados mediante `ExecuteRuntimeAsync` (escrituras del reloj, vehículos generados, variables de script, texturas D3D) no se restauran; desaparecen con el proceso. |
| Estabilidad | `STABLE_BETA`. |
| Ejemplo | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidad | Liberar el handle del consumidor sin dejar abandonada la transacción: si la sesión no es terminal, solicitar la detención canónica; después, esperar la tarea del ciclo de vida del supervisor (salida del proceso, restauración, liberación del lease); por último, olvidar la sesión. |
| Parámetros | `session`. |
| Devuelve | Se completa cuando la sesión es terminal y se ha eliminado. Una vez que retorna, el handle es desconocido (`KeyNotFoundException` en cualquier llamada posterior, incluida una segunda `CloseAsync`). |
| Lanza | `KeyNotFoundException`; `OperationCanceledException` si el llamador cancela mientras se espera al supervisor. En ese caso la sesión no se elimina y el supervisor sigue en ejecución; vuelve a llamar a `CloseAsync`. |
| Cancelación | Se aplica solo a la espera; nunca cancela la restauración. |
| Requiere una sesión en ejecución | No. |
| Modifica el estado de OMSI | Sí, cuando la sesión sigue activa (igual que `StopAsync`). |
| Modifica el sistema de archivos | Indirectamente (restauración por parte del supervisor). |
| Transacción / restauración | Garantiza que la transacción se lleva hasta su finalización antes de liberar el handle (cuando se llegó a iniciar el supervisor). Para una sesión que falló antes de que arrancara el supervisor, la ruta de inicio ya ha restaurado o ha notificado `OL_E_RESTORE_DEFERRED`. |
| Estabilidad | `STABLE_BETA`. |
| Ejemplo | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Finalidad | Ejecutar una operación de runtime pública dentro del proceso de OMSI en ejecución a través del buzón de la sesión, de una sola solicitud en curso (asignado en memoria, 64 KiB, solicitud vinculada al id de sesión y al id de solicitud). El plugin ejecuta la operación en el thread de la UI de OMSI. Catálogo de operaciones: [control de runtime](runtime-control.md) y [capacidades](capabilities.md). |
| Parámetros | `command.SessionId` debe ser igual a `session.SessionId`. `command.RequestId`: `ulong` elegido por el llamador; usa un contador estrictamente creciente por proceso (las utilidades D3D empiezan en 30 000, el propietario de la CLI en 10 001/50 000). `command.Operation`: un id de operación pública de `PublicCapabilityRegistry.PublicRuntimeOperationIds` (por ejemplo `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: valores de cadena indexados por nombres ordinales; los nombres obligatorios de cada operación se obtienen de `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: se mide desde el momento en que la solicitud se deposita en el buzón (no cuenta la espera en cola detrás de otro comando en curso). La CLI usa 5 s (15 s para `road-vehicles.spawn`) como propietario y 8 s / 30 s como cliente. |
| Orden de las comprobaciones | 1. Validación del registro (antes de buscar la sesión): operación desconocida o `internal.*` → resultado `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; falta un argumento obligatorio (ausente o solo espacios en blanco) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Búsqueda de la sesión → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Estado distinto de `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Solicitud al buzón. 6. Se eliminan los valores del resultado cuya clave empieza por `internal_` o termina en `_address`, `_pointer`, `_vmt`. |
| Devuelve | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. Si tiene éxito, `Values` contiene las cadenas semánticas de la operación (documentadas por operación en [control de runtime](runtime-control.md)). |
| Errores incluidos en el resultado | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (registro); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (el resultado del plugin superó el buzón; los resultados de listas acotadas se acortan con `truncated=true` en su lugar); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` con una clave derivada); todos los códigos `OL_E_D3D_*` (con `Values["detail"]` y `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (la capacidad nativa de la operación no está resuelta en el build en ejecución); y todo fallo tipado del lado del plugin con su propio código en `ErrorCode` y el mensaje en `Values["detail"]` (Beta 4; solo un fallo sin un código catalogado es `OL_E_RUNTIME_OPERATION_FAILED`). Códigos que llegan de este modo: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (comprobaciones del lado del plugin), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Consulta los [códigos de error](errors.md). |
| Lanza | `KeyNotFoundException`; `InvalidOperationException` con `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (buzón ya liberado por el supervisor), `OL_E_RUNTIME_CHANNEL_BUSY` (el slot aún contiene una solicitud abandonada), `OL_E_RUNTIME_REQUEST_ID_REUSED` (en el slot sigue habiendo una respuesta obsoleta para el mismo id de solicitud); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (respuesta corrupta, ajena o que no coincide); `ArgumentOutOfRangeException` cuando la solicitud serializada supera el buzón; `OperationCanceledException`. |
| Cancelación | Se respeta mientras se espera la barrera por sesión y cada 20 ms mientras se sondea la respuesta. Cancelar con la solicitud en curso no restablece el slot: la siguiente llamada sobre esa sesión puede fallar con `OL_E_RUNTIME_CHANNEL_BUSY` hasta que el plugin publique su respuesta (que entonces se descarta por obsoleta). Es preferible el timeout; un timeout restablece el slot y una respuesta tardía se detecta y se descarta. |
| Requiere una sesión en ejecución | Sí (`SessionState.Running`); de lo contrario se lanza `OL_E_SESSION_NOT_RUNNING`. El buzón existe hasta que el supervisor lo libera durante la restauración. |
| Modifica el estado de OMSI | Depende de la operación: las operaciones `Read` no; las operaciones `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) modifican estado dentro del proceso que no se restaura. |
| Modifica el sistema de archivos | El host no escribe nada. OMSI puede escribir sus propios archivos como consecuencia (no se rastrean). |
| Transacción / restauración | Ninguna. |
| Limitaciones | Un comando en curso por sesión (las llamadas sobre la misma sesión se serializan). La solicitud y la respuesta están limitadas cada una a 64 KiB menos 8 bytes; las cargas de píxeles D3D, a 48 KiB. `internal.road-vehicles.make-basic` es `INTERNAL` e inaccesible. `weather.set` y `weather.metar.set` son `EXPERIMENTAL`. `timetable.logs.read` no está acotada y puede devolver `OL_E_RUNTIME_RESPONSE_TOO_LARGE` con horarios grandes. `camera.lock` es `EXPERIMENTAL`; necesita un vehículo del jugador y está validada en runtime (`CAM01`), aunque la cadena `RuntimeValidation` del registro sigue indicando `STATICALLY_VALIDATED`. Los handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) tienen ámbito de sesión. |
| Estabilidad | Transporte y contrato `STABLE_BETA`; la estabilidad de cada operación sigue `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`), con las excepciones indicadas arriba. |
| Ejemplo | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidad | Devolver el catálogo de capacidades evaluado para una instalación: un `Capability(Name, Available, EvidenceState, Reason)` por cada entrada de `PublicCapabilityRegistry` que no es `InternalOnly`. `Name` es el id del catálogo y `EvidenceState` la evidencia del catálogo (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` se calcula a partir de la plataforma, la comprobación de OMSI 2, la clasificación y las capacidades nativas que necesita la entrada (consulta [capacidades](capabilities.md#getcapabilitiesasync)). |
| Parámetros | `installation.RootPath`: la instalación que se sondea (plataforma, `Omsi.exe`). `ExpectedExecutableSha256` se ignora. |
| Devuelve | Una entrada por cada entrada de catálogo no interna, por ejemplo `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` en la disposición conocida), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relación con `PublicCapabilityRegistry` | Se deriva de él: el registro declara las capacidades, sus operaciones y su evidencia; esta llamada las evalúa para una instalación. |
| Lanza | `OperationCanceledException` a la entrada; `ArgumentException` para una ruta raíz vacía. |
| Cancelación | Se comprueba una vez a la entrada. |
| Requiere una sesión en ejecución | No. |
| Modifica OMSI / sistema de archivos / transacción | No / No / Ninguna. |
| Estabilidad | Contrato de la llamada `STABLE_BETA`; el contenido de la lista es un inventario mantenido a mano: `PARTIAL`. |
| Ejemplo | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Finalidad | Enumerar el contenido instalado y devolver identidades canónicas que se pueden usar en un `LaunchSpec`. El descubrimiento omite los puntos de reanálisis (los ciclos de uniones no pueden bloquearlo), lee los archivos de OMSI como Windows-1252 (se respetan UTF-8/UTF-16 marcados con BOM) y nunca sigue vínculos simbólicos. |
| Parámetros | `query` y `scope` según la tabla siguiente. `scope` es obligatorio para `Entrypoints` (identidad del mapa), `Repaints`, `FleetNumbers`, `Registrations` (identidad del vehículo). |
| Devuelve | Lista ordenada de `ContentIdentity(Identity, Kind, DisplayName)`. Las identidades son rutas relativas a la instalación con barras invertidas; las comparaciones no distinguen mayúsculas de minúsculas. |
| Lanza | `OperationCanceledException` a la entrada; `ArgumentException` cuando se consulta `Entrypoints` sin ámbito o la raíz está vacía; `FileNotFoundException` (sin código `OL_E_`; la CLI lo asigna a `OL_E_NOT_FOUND`) cuando el mapa o el vehículo del ámbito no está instalado. Una raíz o un directorio de contenido inexistente produce una lista vacía, no un error. |
| Cancelación | Se comprueba una vez a la entrada. |
| Requiere una sesión en ejecución | No. |
| Modifica OMSI / sistema de archivos / transacción | No / No / Ninguna. |
| Estabilidad | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (todo plan validado en runtime se resuelve a través de ellos). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (solo evidencia estática). |
| Ejemplo | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Valores de `ContentQueryKind` y resultados:

| Valor | Ámbito | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | ninguno | `maps\<dir>\global.cfg` | `map` | nombre del directorio del mapa |
| `Situations` | ninguno | `situations\...\<file>.osn` | `situation` | identidad del mapa al que hace referencia el `.osn` (puede ser `null`) |
| `Vehicles` | ninguno | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` o nombre del archivo |
| `Repaints` | identidad del vehículo (obligatoria; sin ella: lista vacía) | `<cti path>#item:<ordinal>` | `repaint` | nombre de `[item]` |
| `Hofs` | ninguno | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | identidad del vehículo (obligatoria; sin ella: lista vacía) | ruta de origen de `[number]` relativa al vehículo | `fleet-number` | `null` |
| `Registrations` | identidad del vehículo (obligatoria; sin ella: lista vacía) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | primera línea de valor (`null` para free) |
| `Addons` | ninguno | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | identidad del mapa (obligatoria; sin ella: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | etiqueta del punto de entrada |
| `SessionProfiles` | ninguno | nombre del directorio del paquete (el id de `/predefined-profile`) | `session-profile`, o `session-profile-invalid` cuando el paquete no compila | `name` del perfil, o el código `OL_E_SESSION_PROFILE_*` de un paquete no válido |
| `RuntimeModules` | ninguno | id del módulo | `runtime-module`, o `runtime-module-invalid` cuando no se puede cargar | `name` del módulo, o el código `OL_E_MODULE_*` |

Un punto de entrada se selecciona mediante `PresentedEntrypointIndex` o mediante `EntrypointIdentity` (una identidad de descubrimiento o una etiqueta exacta). El planificador resuelve una identidad a la etiqueta que presenta OMSI (OMSI presenta solo el primer registro de cada etiqueta exacta) y el plan la enumera en `ResolvedContent` con el tipo `entrypoint`; `world.entrypoint-identity` es `EXPERIMENTAL` hasta que se valide en runtime.

### `ListSessionProfilesAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidad | Enumerar cada paquete de perfil de sesión bajo `<root>\.omsilaunch\session-profiles\` con sus presets, para que un frontend pueda ofrecerlos sin analizar YAML. CLI `/list:SessionProfiles`. |
| Devuelve | Un `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` por cada directorio de paquete, ordenado por `Id`. `Presets` son `SessionProfilePresetSummary(Index, Id, Name)` en orden de índice. Cada paquete se compila con las mismas reglas estrictas que un lanzamiento (`SessionProfileCompiler`); un paquete que no compila se enumera con `IsValid = false`, su código `OL_E_SESSION_PROFILE_*` y su mensaje, y sin presets. Sin directorio de perfiles: lista vacía. |
| Lanza | `OperationCanceledException` a la entrada. Los errores de compilación se notifican en el resultado, no se lanzan. |
| Requiere una sesión en ejecución | No. |
| Modifica OMSI / sistema de archivos / transacción | No / No / Ninguna. |
| Estabilidad | `STABLE_BETA` (control offline `session-profiles.discovery`). |
| Ejemplo | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidad | Descubrir los [módulos de runtime](runtime-modules.md) instalados bajo `plugins\OmsiLaunch.Modules` con su integridad y sus ajustes. Solo lectura. La carga es abierta: ningún módulo se comprueba contra una lista o una firma. |
| Devuelve | Un `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` por cada directorio de módulo, ordenado por nombre de directorio. `Activation` es `requested`, `content` o `default`; `IsValid` es false con un código `OL_E_MODULE_*` cuando el módulo no se puede cargar; `Settings` enumera los ajustes que declara el módulo como `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Lanza | `OperationCanceledException` a la entrada. |
| Requiere una sesión en ejecución | No. CLI: `/list:RuntimeModules` (envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspecto | Detalle |
| --- | --- |
| Firma | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Finalidad | Notificar o completar una transacción duradera obsoleta (`<root>\.omsilaunch\journal.json`) que ha dejado un propietario que se bloqueó. Toma el lease de la instalación mientras dura la llamada, de modo que nunca restaura por debajo de una sesión que se está iniciando. Capacidad pública `session.recover`; CLI `/recovery-status` y `/recover`. |
| Parámetros | `installation.RootPath`: la raíz de la instalación (normalizada con `Path.GetFullPath`). `restore`: `false` = solo notificar; `true` = restaurar, verificar y eliminar el diario y las copias de seguridad. |
| Devuelve | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = existía un diario cuando empezó la llamada; `Recovered` = se solicitó una restauración, se ejecutó y no queda ningún diario; `Diagnostics` = notas de la restauración (`restore.session-artifact-removed` con `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), vacío cuando no se ha restaurado nada. |
| Lanza | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` cuando el lease está ocupado; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` cuando el PID + la hora de creación + la ruta del ejecutable del diario siguen coincidiendo con un proceso vivo o (con el diario más allá de `HandoffCreated` y sin PID) hay en ejecución cualquier `Omsi.exe` de esa raíz; `IOException` con `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` o un mensaje de verificación («Restore hash mismatch: ...», «Restore presence mismatch: ...»); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` para un diario corrupto; `ArgumentException` para una raíz vacía; `OperationCanceledException`. Siempre que lanza una excepción después de iniciada una restauración, el diario se conserva y la siguiente llamada la repite de forma idempotente. |
| Cancelación | Se pasa a las escrituras del diario y de las copias de seguridad; cancelar a mitad de la restauración deja el diario pendiente. |
| Requiere una sesión en ejecución | No (se niega mientras haya un propietario activo). |
| Modifica el estado de OMSI | No. |
| Modifica el sistema de archivos | Solo con `restore == true`: reescribe los originales a partir de copias de seguridad verificadas (bytes, hora de última escritura, hora de creación, atributos; se gestionan los originales de solo lectura; escritura directa + vaciado; no queda ningún `*.omsilaunch.tmp`), elimina los artefactos de la sesión y elimina `journal.json` y `backup\<sessionId>`. |
| Transacción / restauración | Completa la transacción pendiente (`Restoring` → `Restored` → diario eliminado). |
| Estabilidad | `STABLE_BETA`: ruta de notificación y recuperación tras una salida prematura (matriz RV-008), recuperación tras una restauración fallida (cierre de validación en runtime `F01`), rechazo con un propietario activo y en la ventana previa al PID (`S04`) y recuperación diferida previa a la huella durante el inicio (`S05`); consulta [estado de la validación en runtime](../status/runtime-validation-status.md). |
| Ejemplo | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Tipos del contrato

<a id="optional-values-and-semantic-primitives"></a>
### Valores opcionales y primitivas semánticas

| Tipo | Definición | Notas |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, `Unset` estático, `Set(T)` estático | Distingue «no solicitado» de «solicitado con un valor». La forma JSON se documenta en la [referencia de LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Enum de tipo byte. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Solo se valida con `DateTimeMode.Explicit` (mes 1..12, día 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Solo se valida con `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Familia LaunchSpec

Todos los records siguientes se documentan propiedad por propiedad en la [referencia de LaunchSpec](launchspec.md); esta tabla fija el inventario de tipos.

| Tipo | Finalidad | Estabilidad |
| --- | --- | --- |
| `LaunchSpec` | Record raíz de la solicitud, con los descriptores de acceso `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, que sustituyen los miembros opcionales `null` por los valores predeterminados. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (fijación opcional de `Omsi.exe`, que se comprueba al planificar). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Selección del mundo. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto de `LastMapState`; nunca significa «el .osn más reciente»). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (se calcula a partir de `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Cualquier modo distinto de `Unset` hace que el plan no sea ejecutable. | `PARTIAL` (evidencia del plan `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Cualquier modo distinto de `Unset` hace que el plan no sea ejecutable. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Cualquier campo establecido hace que el plan no sea ejecutable. | `PARTIAL` |
| `EnvironmentSpec` | Ocho grupos `IReadOnlyDictionary<string, OptionalValue<string>>` de ajustes semánticos de `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; cualquier valor establecido hace que el plan no sea ejecutable. | `PARTIAL` |
| `DiagnosticsSpec` | Seis booleanos; se transportan, no se consumen. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Procedencia de un perfil de sesión compilado (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [Gestión de HOF](hof-management.md) de la sesión: `Operations` (lista de `HofOperationSpec`). Campo `LaunchSpec.HofManagement`; accesor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Módulos de runtime](runtime-modules.md) de la sesión: `Enable` (ids de módulo), `Disable` (ids de módulo que se mantienen fuera), `Settings` (`"<module id>.<key>"` a valor); accesores `EffectiveDisable`, `EffectiveSettings`. Campo `LaunchSpec.Modules`; accesor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | La evaluación de un módulo en el plan: `Id`, `Version`, `Status` (`ELIGIBLE` o `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` o `default`), `Settings`; `IsEligible`, `EffectiveSettings`. Constantes `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Un ajuste de módulo efectivo: `Key`, `Value`, `Source` (`request`, `settings-file` o `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Vista de descubrimiento de un módulo instalado; consulta `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Un ajuste que declara un módulo: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Una operación de HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | La lanza la planificación de HOF (`OmsiLaunch.Core`); `Code` es un código `OL_E_HOF_*`. La API pública lo notifica como diagnósticos del plan. | `STABLE_BETA` |
| `SessionProfileSummary` | Vista de descubrimiento de un paquete (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Consulta `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Un preset en un resumen (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (se transporta; la restauración se hace siempre), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, predeterminado 180), `ShutdownTimeoutSeconds` (se transporta, no se consume). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Tipos de plan y de estado

| Tipo | Campos | Notas |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (un `Guid` nuevo por plan), `BuildProfileId` (`"Omsi23004"` o `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (lista de `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (rutas relativas distintas de `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` con `STATICALLY_VALIDATED` o `UNAVAILABLE`; las capacidades nativas de inicio con `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (entradas `Capability` de las funcionalidades solicitadas pero no admitidas), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Un record público: puede editarse o quedar obsoleto, por lo que `StartSessionAsync` vuelve a planificar. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, SO x64 y host x64), `LegacyPlatform` (siempre `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (todos iguales a `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` es un valor de `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), salvo en `SessionPlan.RequiredCapabilities`: un requisito de plataforma o de contenido es `STATICALLY_VALIDATED` o `UNAVAILABLE`, y una capacidad nativa es `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Las mutaciones de presentación usan las claves `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (mapa de cadenas opcional). | Los códigos que empiezan por `OL_E_` son errores, los `OL_W_` son advertencias y todo lo demás es informativo. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Los diagnósticos de la sesión no incluyen los diagnósticos del plan. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (hora de recepción en el host), `Sequence` (base 1, por sesión), `Data`, `SessionTickMilliseconds` (el reloj monotónico de sesión del plugin cuando emitió el evento; nulo para los eventos del host). | Acotado a los 256 eventos más recientes (se descartan los más antiguos). Los eventos del plugin llegan en orden a través del anillo de telemetría; los eventos perdidos por un desbordamiento del anillo los cuenta `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Local al proceso. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Consulta `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Consulta `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Enum de tipo byte, en orden de declaración: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. El servicio actual nunca establece `ValidatingPlatform`, `Planning` ni `EnteringGameplay`; `Snapshotting` es transitorio y prácticamente inobservable. Estados terminales: `Completed`, `Failed`. Semántica completa: [ciclo de vida de la sesión](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Tipos de control de runtime

| Tipo | Definición | Estabilidad |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Códec estático que usan el host y el plugin para el envelope del buzón: número mágico `0x4F4C5243` («OLRC»), versión 1, cabecera little-endian de 72 bytes (número mágico, versión, tipo 1 = solicitud / 2 = respuesta, longitud total, `Guid` de la sesión, id de solicitud, longitud de la carga útil, SHA-256 de la carga útil) seguida de una carga útil JSON en UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: público porque lo comparten ambos extremos del puente; no es una superficie de integración; el formato puede cambiar con la versión del protocolo. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)`: lo que el host publica al plugin en el archivo asignado en memoria `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)`: un módulo planificado que transporta el handoff (`StartupHandoff.Modules`). | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: el reloj de inicio resuelto de una sesión NEW_MAP que transporta el handoff (`StartupHandoff.Start`); un campo nulo conserva el valor propio de OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Códec: número mágico `0x4F4C5348`, versión 7 (lee de la 3 a la 7; la versión 5 añade los módulos planificados, la 6 los ajustes efectivos de cada módulo y la 7 la fecha y hora de inicio; los bits de flag 0x04 y 0x08 transportan `PluginTrace` y `NativeTrace`), cabecera de 64 bytes con integridad de la carga mediante SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Anillo de eventos del plugin al host en la asignación `OMSILAUNCH_TELEMETRY_NAME`: número mágico `0x52544C4F` («OLTR»), versión 1, 64 slots numerados de 2048 bytes. `Initialize`, `Write`, `IsRing`, `ReadSince` (devuelve los eventos posteriores a un número de secuencia y cuántos se perdieron). | `INTERNAL` |

El plugin rechaza un handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`) salvo que `WorldMode` sea `NewMap` o `SavedSituation`, `HeadlessStart` sea true, `PlayerVehicleEnabled` sea false, ambos modos de fecha y hora sean `Unset` y una situación guardada tenga una identidad no vacía. El planificador aplica las mismas restricciones antes, así que un plan ejecutable nunca provoca este rechazo.

<a id="capability-registry-types"></a>
### Tipos del registro de capacidades

| Tipo | Finalidad |
| --- | --- |
| `PublicCapabilityRegistry` | El catálogo canónico de capacidades. `ProtocolVersion` (`"0.1"`), `All` (cada `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (los 48 ids de operación concretos que puede reenviar un frontend), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (ruta de comando a id de operación), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (devuelve `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Lo aplican `ExecuteRuntimeAsync`, la CLI, el plano de control local y el plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (un valor de `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; derivados `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (admite `cursor`/`limit`; consulta la [paginación](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Qué cambia una operación de runtime: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | El vocabulario de evidencias: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Catálogo completo: [capacidades](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Métodos de extensión de `D3DRuntimeApi`

Envoltorios tipados sobre `ExecuteRuntimeAsync` para las operaciones `d3d.*` (`EXPERIMENTAL`, capacidad `PublicExperimental` `d3d.texture`). Asignan los ids de solicitud a partir de un contador de ámbito de proceso que empieza en 30 000 y usan un timeout predeterminado de 5 s (salvo `GetD3DStatusAsync`, que exige uno).

| Método | Operación | Argumentos y límites |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | ninguno |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | anchura/altura 1..4096, niveles 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | nivel 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, anchura/altura 1..4096, píxeles ≤ 48 KiB (codificados en Base64 en la transmisión) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | una liberación repetida se rechaza con `OL_E_D3D_RESOURCE_RELEASED` |

Tipos: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` con `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Errores: un resultado fallido se vuelve a lanzar como `OmsiRuntimeException(Code, detail)`, donde `Code` es el `ErrorCode` del resultado (u `OL_E_RUNTIME_OPERATION_FAILED` si no lo hay) y el mensaje es `"<code>: <Values["detail"]>"`; un resultado correcto sin valores, o una cadena de estado del dispositivo desconocida, lanza `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Todo lo que lanza `ExecuteRuntimeAsync` se propaga sin cambios. La gestión del reset del dispositivo está validada en runtime: un reset hace pasar el dispositivo por `Resetting` de vuelta a `Ready` e invalida todas las texturas activas (`OL_E_D3D_STALE_RESOURCE_HANDLE`, cierre de validación en runtime `D01`); la entrada del catálogo `d3d.texture` lo registra. La transición `Lost` no se puede producir desde fuera del producto y solo está cubierta offline.

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### Tipos del contrato de proceso

| Tipo | Contenido |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Lo usa solo la CLI ([códigos de salida](exit-codes.md)); la API nunca termina el proceso. |
| `PublicErrorCategory` | Constantes de cadena usadas en los envelopes de error de la CLI y del control: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Una `const string` por código (143: 142 errores `OL_E_` y 1 advertencia `OL_W_`) y `All`, el catálogo de `PublicErrorDescriptor(Code, Category)`. Categorías: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Referencia: [códigos de error](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Propiedad `Code` más el mensaje; solo la lanza `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (identidad de la instalación y contención de rutas)

Estabilidad: `STABLE_BETA` (funciones puras, sin E/S, sin estado de OMSI, sin modificaciones del sistema de archivos, sin participación en transacciones, sin necesidad de una sesión en ejecución). Es la definición única que usan el lease de la instalación, el nombre de la canalización de control local, el confinamiento de los recursos de los perfiles de sesión, la validación de destinos de las texturas de Internet y la ruta del modelo para la generación en runtime.

| Miembro | Comportamiento |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` sin separadores finales, excepto en una raíz de unidad (`C:\`), que se conserva. Resuelve los segmentos `.` y `..`, trata `/` y `\` por igual y colapsa los separadores repetidos. **No** resuelve uniones ni vínculos simbólicos. Lanza `ArgumentException` para una raíz null o en blanco. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` en mayúsculas. Las grafías léxicamente equivalentes de una misma raíz (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) comparten una clave; las raíces distintas (`C:\OMSI-A`, `C:\OMSI-B`) nunca. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Resuelve `candidate` (relativo a `root`, o absoluto) y devuelve `true` solo cuando se encuentra estrictamente por debajo de `root`; `relativePath` es la grafía canónica con `\`. Usa los segmentos de `Path.GetRelativePath`, así que un directorio hermano como `C:\OMSI-A\x` nunca está dentro de `C:\OMSI`; la propia raíz, otros volúmenes y los escapes con `..` devuelven `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Divide por `/` y `\`, descartando los segmentos vacíos. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Perfiles de sesión (`OmsiLaunch.Core`)

Estabilidad: `EXPERIMENTAL`. Estos tipos compilan un [perfil de sesión](session-profiles.md) YAML (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, esquema `omsilaunch.session-profile/v1`) en un `LaunchSpec`. La CLI `/predefined-profile:<id> /predefined-profile-index:<n>` usa exactamente estas llamadas; un integrador puede usarlas para iniciar un perfil a través de la API.

| Miembro | Comportamiento |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Lee y valida el paquete. `id` debe ser un nombre de directorio simple (en caso contrario, `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` es 1..99 (`MinPresetIndex`..`MaxPresetIndex`; en caso contrario, `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Archivo ausente: `OL_E_SESSION_PROFILE_NOT_FOUND`; mayor que `MaxBytes` (256 KiB), YAML no válido, anclas, claves desconocidas o un `id` distinto del nombre del directorio: `OL_E_SESSION_PROFILE_INVALID`; otro `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Las rutas de los recursos quedan confinadas al paquete (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Todo fallo es una `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Devuelve `baseline` con el perfil aplicado: para `NewMap`, el bloque `new` del perfil (mapa, punto de entrada y cualquier fecha/hora/año/meteorología); los `settings` del preset combinados sobre `Environment.General`; los `Presentation`, `InternetTextures` y `Behavior` del preset, cuando existen; y `SessionProfile` = `profile.Metadata`. Para `NewMap` también comprueba el mapa contra la lista `compatibility` del perfil (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | La comprobación de compatibilidad para los demás modos (para `SavedSituation`, el mapa se lee del `.osn`). La CLI la llama después de construir el `WorldSpec` definitivo. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` y las claves aceptadas en cada mapeo YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | El paquete cargado; `Preset` es solo el preset seleccionado. |
| `SessionProfileException(string code, string message)` | `IOException` con `Code` (uno de los códigos `OL_E_SESSION_PROFILE_*`); el mensaje es `"<code>: <message>"`. |

Además, la CLI rechaza los flags de línea de comandos que entran en conflicto con el perfil (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); esa comprobación no forma parte del compilador. Consulta [perfiles de sesión](session-profiles.md#precedence-and-override-conflicts) para ver el orden completo de combinación.

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### Tipos de plataforma (`OmsiLaunch.Process`)

| Tipo | Estabilidad | Uso |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` como tipo del parámetro del constructor de `OmsiLaunchService` | Detecta la plataforma, comprueba si se puede escribir, inicia, observa, termina y espera a `Omsi.exe`. Pasa `new CurrentWindowsX64Platform()`; implementarlo por tu cuenta no está soportado. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | La única implementación: host Windows x64, `CreateProcessW` para `Omsi.exe`, `TerminateProcess` para la detención canónica. Sus métodos los llama el servicio; los integradores solo lo construyen. Miembros (compartidos con `IRuntimePlatform`): `Detect(root)` devuelve el `RuntimePlatformInfo` del plan; `ValidateCurrent(info)` lanza `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` cuando el host no puede ejecutar una sesión; `IsInstallationWritable(root)` respalda `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` crea `Omsi.exe` y registra la identidad del proceso (PID, hora de creación, ruta y el hash calculado por el servicio; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` y `Terminate` lo observan y lo finalizan. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Públicos en el ensamblado porque el servicio y las pruebas los comparten. No son una superficie de integración; `LaunchedProcess` envuelve internamente los handles del proceso y del thread de OMSI (no son miembros públicos) y `IOmsiLaunch` nunca lo devuelve. |

<a id="thread-safety"></a>
## Seguridad para subprocesos

- `OmsiLaunchService` admite llamadas concurrentes sobre sesiones distintas: las sesiones residen en un `ConcurrentDictionary` y toda modificación de una sesión se hace bajo el bloqueo privado de esa sesión.
- Las llamadas concurrentes sobre la misma sesión son seguras, pero se serializan donde importa: `ExecuteRuntimeAsync` toma una barrera por sesión, de modo que un segundo comando espera al primero (su timeout empieza cuando se deposita).
- El supervisor se ejecuta en una tarea del grupo de threads (`Task.Run`) desde el momento en que `StartSessionAsync` retorna hasta que la sesión es terminal; sondea la telemetría y el proceso cada 100 ms. Los llamadores nunca ejecutan código del supervisor.
- `StopAsync` y `GetStatusAsync` se completan de forma síncrona y se pueden llamar desde cualquier thread, incluso dentro de un controlador `ProcessExit` (la CLI lo hace con un margen de 4 s).
- Ninguna llamada de la API está ligada a un thread; ninguna requiere un contexto de sincronización.

<a id="what-is-not-in-the-api"></a>
## Lo que no está en la API

- Ningún `IntPtr`, `nint`, handle de Win32, dirección nativa, puntero de VMT ni objeto de proceso. Los valores del resultado cuyas claves empiezan por `internal_` o terminan en `_address`, `_pointer`, `_vmt` se eliminan antes de que un resultado salga de `ExecuteRuntimeAsync`.
- Ninguna operación de runtime `internal.*`: `internal.road-vehicles.make-basic` es `InternalOnly` en el registro y devuelve `OL_E_RUNTIME_OPERATION_UNKNOWN` desde la API y desde la CLI.
- Ninguna lectura ni escritura directa de la memoria de OMSI, ningún acceso a nivel de archivo a la instalación más allá de lo que declara un `LaunchSpec`.
- Ningún handle entre procesos: el [plano de control local](local-control.md) es la única vía entre procesos y solo acepta `session.status`, `session.events`, `session.stop` y `runtime.execute`.
- Ninguna aplicación del vehículo del jugador al inicio. El cierre cooperativo, `LAST_MAP_STATE`, la fecha/hora/meteorología de inicio y los overlays de documentos de teclado/mando son `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Resumen de estabilidad

| Superficie | Estabilidad |
| --- | --- |
| Constructor de `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transporte de `ExecuteRuntimeAsync`; operaciones `PublicStableBeta` | `STABLE_BETA` |
| Operaciones `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| miembros de la especificación de fecha/hora/meteorología/entrada, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| miembros de la especificación del vehículo del jugador | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implementaciones de `IRuntimePlatform`, todos los ensamblados de implementación | `INTERNAL` |
| `WorldMode.LastSituation`, operaciones `internal.*` | `UNAVAILABLE` |
