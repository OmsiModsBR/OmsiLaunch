# Início rápido da API pública

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Tradução da [página original em inglês](../../../getting-started/api-quick-start.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página inicia, lê e encerra uma sessão do OmsiLaunch a partir do seu próprio programa .NET. Ela pressupõe que o pacote está instalado no diretório do OMSI 2 (veja [instalação](installation.md)) e que `OmsiLaunch.exe /plan` funciona ali (veja [primeira sessão](first-session.md)). O contrato completo está na [referência da API pública](../reference/public-api.md).

Placeholders: `<OMSI_PATH>` é a instalação do OMSI 2 que contém `Omsi.exe` e o pacote do OmsiLaunch (por exemplo `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Projeto

A API é distribuída como os assemblies do pacote em `<OMSI_PATH>`; não existe pacote NuGet. Referencie-os a partir de um projeto de console x64 `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process` formam a superfície pública; os demais assemblies são dependências deles e precisam ser copiados para junto do seu programa.

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

Substitua `<OMSI_PATH>` em `OmsiPath` pelo caminho real.

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

Execute-o com a instalação como único argumento:

```powershell
dotnet run -- "<OMSI_PATH>"
```

O que acontece: o plano é verificado (nada é iniciado se ele não estiver apto para execução), o OMSI inicia e carrega Grundorf, o programa lê o relógio uma vez e, em seguida, encerra a sessão. O OMSI é encerrado e todos os arquivos que o OmsiLaunch alterou são restaurados. Só é permitido um proprietário por instalação: pare antes qualquer sessão do `OmsiLaunch.exe` na mesma instalação, caso contrário `StartSessionAsync` informa `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Próximos passos

- Mais operações de runtime (veículos, tabela de horários, scripts, D3D): [controle de runtime](../reference/runtime-control.md) e [capacidades](../reference/capabilities.md).
- Configurações, splash e texturas da internet na spec: [LaunchSpec](../reference/launchspec.md).
- Iniciar um perfil de sessão a partir do código: [API pública, perfis de sessão](../reference/public-api.md#session-profiles-omsilaunchcore).
- Controlar uma sessão cujo proprietário é outro processo (por exemplo `OmsiLaunch.exe`): [controle local](../reference/local-control.md).
- Erros: [erros](../reference/errors.md); ciclo de vida: [ciclo de vida da sessão](../concepts/session-lifecycle.md).
