# Inicio rápido de la API pública

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Traducción de la [página original en inglés](../../../getting-started/api-quick-start.md) de OmsiLaunch 0.1.0-beta3. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Esta página inicia, lee y finaliza una sesión de OmsiLaunch desde su propio programa .NET. Se supone que el paquete está instalado en el directorio de OMSI 2 (consulte [instalación](installation.md)) y que `OmsiLaunch.exe /plan` funciona allí (consulte [primera sesión](first-session.md)). El contrato completo está en la [referencia de la API pública](../reference/public-api.md).

Marcadores de posición: `<OMSI_PATH>` es la instalación de OMSI 2 que contiene `Omsi.exe` y el paquete de OmsiLaunch (por ejemplo `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Proyecto

La API se distribuye como los ensamblados del paquete en `<OMSI_PATH>`; no hay paquete NuGet. Haga referencia a ellos desde un proyecto de consola `net6.0-windows` x64. `OmsiLaunch.Api`, `OmsiLaunch.Core` y `OmsiLaunch.Process` son la superficie pública; los demás ensamblados son sus dependencias y deben copiarse junto a su programa.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <OmsiPath>&lt;OMSI_PATH&gt;</OmsiPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="$(OmsiPath)\OmsiLaunch.Api.dll" />
    <Reference Include="$(OmsiPath)\OmsiLaunch.Core.dll" />
    <Reference Include="$(OmsiPath)\OmsiLaunch.Process.dll" />
    <Reference Include="$(OmsiPath)\OmsiLaunch.Configuration.dll" />
    <Reference Include="$(OmsiPath)\OmsiLaunch.Content.dll" />
    <Reference Include="$(OmsiPath)\OmsiLaunch.Builds.Omsi23004.dll" />
    <Reference Include="$(OmsiPath)\YamlDotNet.dll" />
  </ItemGroup>
</Project>
```

Reemplace `<OMSI_PATH>` en `OmsiPath` por la ruta real.

<a id="2-program"></a>
## 2. Programa

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var root = args.Length > 0 ? args[0] : @"C:\OMSI 2";                 // <OMSI_PATH>
var plugins = Path.Combine(root, "plugins");
var manifest = Path.Combine(root, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));

// NEW_MAP on Grundorf, first presented entry point. Everything else stays as OMSI has it.
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(root),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable)
{
    foreach (var diagnostic in plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_", StringComparison.Ordinal)))
        Console.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    return 1;
}

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State != SessionState.Running)
    {
        Console.WriteLine($"Did not reach gameplay: {status.State} {status.Diagnostics.LastOrDefault()?.Code}");
        return 1;
    }

    var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
    Console.WriteLine(time.Succeeded ? $"OMSI time {time.Values!["hour"]}:{time.Values["minute"]}" : $"time.read failed: {time.ErrorCode}");

    await launch.StopAsync(session);                                   // terminate OMSI and restore every file
    var final = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(2));
    Console.WriteLine($"Session ended: {final.State}");
    return final.State == SessionState.Completed ? 0 : 1;
}
finally
{
    await launch.CloseAsync(session);                                  // always, on every path
}
```

Ejecútelo con la instalación como único argumento:

```powershell
dotnet run -- "<OMSI_PATH>"
```

Lo que sucede: se comprueba el plan (no se inicia nada si no es ejecutable), OMSI arranca y carga Grundorf, el programa lee el reloj una vez y luego finaliza la sesión. Se termina OMSI y se restaura cada archivo que OmsiLaunch modificó. Solo se permite un propietario por instalación: primero detenga cualquier sesión de `OmsiLaunch.exe` de la misma instalación; de lo contrario, `StartSessionAsync` informa `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Próximos pasos

- Más operaciones de runtime (vehículos, horario, scripts, D3D): [control en runtime](../reference/runtime-control.md) y [capacidades](../reference/capabilities.md).
- Ajustes, splash e Internet Textures en la especificación: [LaunchSpec](../reference/launchspec.md).
- Iniciar un perfil de sesión desde el código: [API pública, perfiles de sesión](../reference/public-api.md#session-profiles-omsilaunchcore).
- Controlar una sesión que pertenece a otro proceso (por ejemplo `OmsiLaunch.exe`): [control local](../reference/local-control.md).
- Errores: [errores](../reference/errors.md); ciclo de vida: [ciclo de vida de la sesión](../concepts/session-lifecycle.md).
