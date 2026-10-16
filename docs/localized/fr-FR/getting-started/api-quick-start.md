# Démarrage rapide avec l'API publique

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Traduction de la [page originale en anglais](../../../getting-started/api-quick-start.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page démarre, lit et termine une session OmsiLaunch depuis votre propre programme .NET. Elle suppose que le paquet est installé dans le répertoire d'OMSI 2 (voir [installation](installation.md)) et que `OmsiLaunch.exe /plan` y fonctionne (voir [première session](first-session.md)). Le contrat complet se trouve dans la [référence de l'API publique](../reference/public-api.md).

Espaces réservés : `<OMSI_PATH>` est l'installation d'OMSI 2 qui contient `Omsi.exe` et le paquet OmsiLaunch (par exemple `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Projet

L'API est livrée sous forme des assemblys du paquet dans `<OMSI_PATH>` ; il n'existe pas de paquet NuGet. Référencez-les depuis un projet console x64 `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` et `OmsiLaunch.Process` constituent la surface publique ; les autres assemblys sont leurs dépendances et doivent être copiés à côté de votre programme.

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

Remplacez `<OMSI_PATH>` dans `OmsiPath` par le chemin réel.

<a id="2-program"></a>
## 2. Programme

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

Exécutez-le avec l'installation comme unique argument :

```powershell
dotnet run -- "<OMSI_PATH>"
```

Déroulement : le plan est vérifié (rien n'est démarré s'il n'est pas exécutable), OMSI démarre et charge Grundorf, le programme lit l'horloge une fois, puis termine la session. OMSI est arrêté et chaque fichier modifié par OmsiLaunch est restauré. Un seul propriétaire par installation est autorisé : arrêtez d'abord toute session `OmsiLaunch.exe` pour la même installation, sinon `StartSessionAsync` signale `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Étapes suivantes

- Autres opérations runtime (véhicules, horaire, scripts, D3D) : [contrôle runtime](../reference/runtime-control.md) et [capacités](../reference/capabilities.md).
- Paramètres, écran de démarrage et textures Internet dans la spécification : [LaunchSpec](../reference/launchspec.md).
- Démarrer un profil de session depuis le code : [API publique, profils de session](../reference/public-api.md#session-profiles-omsilaunchcore).
- Contrôler une session dont un autre processus est propriétaire (par exemple `OmsiLaunch.exe`) : [contrôle local](../reference/local-control.md).
- Erreurs : [erreurs](../reference/errors.md) ; cycle de vie : [cycle de vie de la session](../concepts/session-lifecycle.md).
