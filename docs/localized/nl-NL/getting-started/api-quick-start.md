# Snelstart publieke API

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../getting-started/api-quick-start.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Op deze pagina start je één OmsiLaunch-sessie vanuit je eigen .NET-programma, lees je die uit en beëindig je die. Er wordt van uitgegaan dat het pakket in de OMSI 2-map is geïnstalleerd (zie [installatie](installation.md)) en dat `OmsiLaunch.exe /plan` daar werkt (zie [eerste sessie](first-session.md)). Het volledige contract staat in de [referentie van de publieke API](../reference/public-api.md).

Tijdelijke aanduidingen: `<OMSI_PATH>` is de OMSI 2-installatie die `Omsi.exe` en het OmsiLaunch-pakket bevat (bijvoorbeeld `C:\OMSI 2`).

## 1. Project

De API wordt geleverd als de pakketassembly's in `<OMSI_PATH>`; er is geen NuGet-pakket. Verwijs ernaar vanuit een x64-consoleproject voor `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` en `OmsiLaunch.Process` vormen het publieke oppervlak; de andere assembly's zijn hun afhankelijkheden en moeten naast je programma worden gekopieerd.

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

Vervang `<OMSI_PATH>` in `OmsiPath` door het echte pad.

<a id="2-program"></a>
## 2. Programma

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

Voer het uit met de installatie als enige argument:

```powershell
dotnet run -- "<OMSI_PATH>"
```

Wat er gebeurt: het plan wordt gecontroleerd (er wordt niets gestart als het niet uitvoerbaar is), OMSI start en laadt Grundorf, het programma leest de klok één keer uit en beëindigt daarna de sessie. OMSI wordt beëindigd en elk bestand dat OmsiLaunch heeft gewijzigd, wordt hersteld. Er is maar één eigenaar per installatie toegestaan: stop eerst een eventuele `OmsiLaunch.exe`-sessie voor dezelfde installatie, anders meldt `StartSessionAsync` de fout `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Volgende stappen

- Meer runtime-operaties (voertuigen, dienstregeling, scripts, D3D): [runtimebesturing](../reference/runtime-control.md) en [capabilities](../reference/capabilities.md).
- Instellingen, opstartscherm en internettextures in de spec: [LaunchSpec](../reference/launchspec.md).
- Een sessieprofiel vanuit code starten: [publieke API, sessieprofielen](../reference/public-api.md#session-profiles-omsilaunchcore).
- Een sessie besturen waarvan een ander proces eigenaar is (bijvoorbeeld `OmsiLaunch.exe`): [local control](../reference/local-control.md).
- Fouten: [fouten](../reference/errors.md); levenscyclus: [sessielevenscyclus](../concepts/session-lifecycle.md).
