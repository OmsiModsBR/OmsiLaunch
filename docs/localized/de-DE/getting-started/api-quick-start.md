# Schnellstart mit der öffentlichen API

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Übersetzung der [englischen Originalseite](../../../getting-started/api-quick-start.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite startet, liest und beendet eine OmsiLaunch-Sitzung aus Ihrem eigenen .NET-Programm. Sie setzt voraus, dass das Paket im OMSI-2-Verzeichnis installiert ist (siehe [Installation](installation.md)) und dass `OmsiLaunch.exe /plan` dort funktioniert (siehe [erste Sitzung](first-session.md)). Der vollständige Vertrag steht in der [Referenz der öffentlichen API](../reference/public-api.md).

Platzhalter: `<OMSI_PATH>` ist die OMSI-2-Installation, die `Omsi.exe` und das OmsiLaunch-Paket enthält (z. B. `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Projekt

Die API wird als Paket-Assemblies in `<OMSI_PATH>` ausgeliefert; es gibt kein NuGet-Paket. Referenzieren Sie sie aus einem x64-Konsolenprojekt mit `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` und `OmsiLaunch.Process` bilden die öffentliche Oberfläche; die übrigen Assemblies sind deren Abhängigkeiten und müssen neben Ihr Programm kopiert werden.

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

Ersetzen Sie `<OMSI_PATH>` in `OmsiPath` durch den tatsächlichen Pfad.

<a id="2-program"></a>
## 2. Programm

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

Führen Sie es mit der Installation als einzigem Argument aus:

```powershell
dotnet run -- "<OMSI_PATH>"
```

Was geschieht: Der Plan wird geprüft (es wird nichts gestartet, wenn er nicht ausführbar ist), OMSI startet und lädt Grundorf, das Programm liest einmal die Uhr aus und beendet dann die Sitzung. OMSI wird beendet und jede von OmsiLaunch geänderte Datei wird wiederhergestellt. Pro Installation ist nur ein Eigentümer zulässig: Beenden Sie zuerst jede `OmsiLaunch.exe`-Sitzung für dieselbe Installation, andernfalls meldet `StartSessionAsync` den Fehler `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Nächste Schritte

- Weitere Runtime-Operationen (Fahrzeuge, Fahrplan, Skripte, D3D): [Runtime-Steuerung](../reference/runtime-control.md) und [Capabilities](../reference/capabilities.md).
- Einstellungen, Startbild und Internettexturen in der Spezifikation: [LaunchSpec](../reference/launchspec.md).
- Ein Sitzungsprofil aus Code starten: [öffentliche API, Sitzungsprofile](../reference/public-api.md#session-profiles-omsilaunchcore).
- Eine Sitzung steuern, deren Eigentümer ein anderer Prozess ist (z. B. `OmsiLaunch.exe`): [lokale Steuerung](../reference/local-control.md).
- Fehler: [Fehler](../reference/errors.md); Lebenszyklus: [Sitzungslebenszyklus](../concepts/session-lifecycle.md).
