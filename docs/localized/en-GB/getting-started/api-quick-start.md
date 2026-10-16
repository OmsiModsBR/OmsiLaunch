# Public API Quick Start

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> British English edition of the [canonical page](../../../getting-started/api-quick-start.md) for OmsiLaunch 0.1.0-beta3. The US English page is normative: where the two differ, it and the code take precedence.

This page starts, reads and ends one OmsiLaunch session from your own .NET program. It assumes the package is installed in the OMSI 2 directory (see [installation](installation.md)) and that `OmsiLaunch.exe /plan` works there (see [first session](first-session.md)). The full contract is in the [public API reference](../reference/public-api.md).

Placeholders: `<OMSI_PATH>` is the OMSI 2 installation that contains `Omsi.exe` and the OmsiLaunch package (for example `C:\OMSI 2`).

## 1. Project

The API ships as the package assemblies in `<OMSI_PATH>`; there is no NuGet package. Reference them from an x64 `net6.0-windows` console project. `OmsiLaunch.Api`, `OmsiLaunch.Core` and `OmsiLaunch.Process` are the public surface; the other assemblies are their dependencies and must be copied beside your program.

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

Replace `<OMSI_PATH>` in `OmsiPath` with the real path.

## 2. Program

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

Run it with the installation as the only argument:

```powershell
dotnet run -- "<OMSI_PATH>"
```

What happens: the plan is checked (nothing is started if it is not runnable), OMSI starts and loads Grundorf, the program reads the clock once, then ends the session. OMSI is terminated and every file OmsiLaunch changed is restored. Only one owner per installation is allowed: stop any `OmsiLaunch.exe` session for the same installation first, or `StartSessionAsync` reports `OL_E_INSTALLATION_BUSY`.

## 3. Next steps

- More runtime operations (vehicles, timetable, scripts, D3D): [runtime control](../reference/runtime-control.md) and [capabilities](../reference/capabilities.md).
- Settings, splash and Internet Textures in the spec: [LaunchSpec](../reference/launchspec.md).
- Starting a session profile from code: [public API, session profiles](../reference/public-api.md#session-profiles-omsilaunchcore).
- Controlling a session owned by another process (for example `OmsiLaunch.exe`): [local control](../reference/local-control.md).
- Errors: [errors](../reference/errors.md); lifecycle: [session lifecycle](../concepts/session-lifecycle.md).
