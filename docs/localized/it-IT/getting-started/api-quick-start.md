# Guida rapida all'API pubblica

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Traduzione della [pagina originale in inglese](../../../getting-started/api-quick-start.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina avvia, legge e termina una sessione OmsiLaunch da un programma .NET proprio. Si presuppone che il pacchetto sia installato nella directory di OMSI 2 (vedere [installazione](installation.md)) e che `OmsiLaunch.exe /plan` funzioni in tale directory (vedere [prima sessione](first-session.md)). Il contratto completo si trova nel [riferimento dell'API pubblica](../reference/public-api.md).

Segnaposto: `<OMSI_PATH>` è l'installazione di OMSI 2 che contiene `Omsi.exe` e il pacchetto OmsiLaunch (per esempio `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Progetto

L'API viene distribuita come gli assembly del pacchetto in `<OMSI_PATH>`; non esiste un pacchetto NuGet. Referenziarli da un progetto console x64 `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process` costituiscono la superficie pubblica; gli altri assembly sono loro dipendenze e devono essere copiati accanto al programma.

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

Sostituire `<OMSI_PATH>` in `OmsiPath` con il percorso reale.

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

Eseguirlo con l'installazione come unico argomento:

```powershell
dotnet run -- "<OMSI_PATH>"
```

Che cosa accade: il piano viene verificato (non viene avviato nulla se non è eseguibile), OMSI si avvia e carica Grundorf, il programma legge l'orologio una volta, poi termina la sessione. OMSI viene terminato e ogni file modificato da OmsiLaunch viene ripristinato. È consentito un solo owner per installazione: arrestare prima qualsiasi sessione di `OmsiLaunch.exe` per la stessa installazione, altrimenti `StartSessionAsync` segnala `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Passi successivi

- Altre operazioni runtime (veicoli, orario, script, D3D): [controllo a runtime](../reference/runtime-control.md) e [capability](../reference/capabilities.md).
- Impostazioni, splash screen e Internet Textures (texture da Internet) nella spec: [LaunchSpec](../reference/launchspec.md).
- Avvio di un profilo di sessione dal codice: [API pubblica, profili di sessione](../reference/public-api.md#session-profiles-omsilaunchcore).
- Controllo di una sessione di proprietà di un altro processo (per esempio `OmsiLaunch.exe`): [controllo locale](../reference/local-control.md).
- Errori: [errori](../reference/errors.md); ciclo di vita: [ciclo di vita della sessione](../concepts/session-lifecycle.md).
