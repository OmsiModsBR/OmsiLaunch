# Szybki start z publicznym API

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../getting-started/api-quick-start.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Na tej stronie jedna sesja OmsiLaunch jest uruchamiana, odczytywana i kończona z własnego programu .NET. Zakłada się, że pakiet jest zainstalowany w katalogu OMSI 2 (zob. [instalacja](installation.md)) i że działa w nim `OmsiLaunch.exe /plan` (zob. [pierwsza sesja](first-session.md)). Pełny kontrakt opisuje [dokumentacja publicznego API](../reference/public-api.md).

Symbole zastępcze: `<OMSI_PATH>` to instalacja OMSI 2 zawierająca `Omsi.exe` i pakiet OmsiLaunch (na przykład `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Projekt

API jest dostarczane jako zestawy pakietu w `<OMSI_PATH>`; nie ma pakietu NuGet. Należy odwołać się do nich z projektu konsolowego x64 `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` i `OmsiLaunch.Process` stanowią publiczną powierzchnię; pozostałe zestawy są ich zależnościami i muszą zostać skopiowane obok programu.

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

W `OmsiPath` należy zastąpić `<OMSI_PATH>` rzeczywistą ścieżką.

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

Program uruchamia się z instalacją jako jedynym argumentem:

```powershell
dotnet run -- "<OMSI_PATH>"
```

Co się dzieje: plan jest sprawdzany (nic nie jest uruchamiane, jeśli plan jest niemożliwy do uruchomienia), OMSI startuje i wczytuje Grundorf, program jednokrotnie odczytuje zegar, a następnie kończy sesję. OMSI jest kończony, a każdy plik zmieniony przez OmsiLaunch zostaje przywrócony. Dozwolony jest tylko jeden właściciel na instalację: najpierw należy zatrzymać każdą sesję `OmsiLaunch.exe` dla tej samej instalacji, w przeciwnym razie `StartSessionAsync` zgłosi `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Następne kroki

- Więcej operacji runtime (pojazdy, rozkład jazdy, skrypty, D3D): [sterowanie runtime](../reference/runtime-control.md) i [możliwości](../reference/capabilities.md).
- Ustawienia, ekran startowy i tekstury internetowe w specyfikacji: [LaunchSpec](../reference/launchspec.md).
- Uruchamianie profilu sesji z kodu: [publiczne API, profile sesji](../reference/public-api.md#session-profiles-omsilaunchcore).
- Sterowanie sesją należącą do innego procesu (na przykład `OmsiLaunch.exe`): [lokalne sterowanie](../reference/local-control.md).
- Błędy: [błędy](../reference/errors.md); cykl życia: [cykl życia sesji](../concepts/session-lifecycle.md).
