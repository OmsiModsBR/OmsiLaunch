# Início rápido da API pública

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Tradução da [página original em inglês](../../../getting-started/api-quick-start.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página inicia, lê e termina uma sessão do OmsiLaunch a partir de um programa .NET próprio. Pressupõe que o pacote está instalado no diretório do OMSI 2 (ver [instalação](installation.md)) e que `OmsiLaunch.exe /plan` funciona nesse local (ver [primeira sessão](first-session.md)). O contrato completo está na [referência da API pública](../reference/public-api.md).

Marcadores de posição (placeholders): `<OMSI_PATH>` é a instalação do OMSI 2 que contém `Omsi.exe` e o pacote do OmsiLaunch (por exemplo `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Projeto

A API é distribuída como os assemblies do pacote em `<OMSI_PATH>`; não existe pacote NuGet. Devem ser referenciados a partir de um projeto de consola x64 `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process` constituem a superfície pública; os restantes assemblies são as suas dependências e têm de ser copiados para junto do programa.

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

Substituir `<OMSI_PATH>` em `OmsiPath` pelo caminho real.

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

Executá-lo com a instalação como único argumento:

```powershell
dotnet run -- "<OMSI_PATH>"
```

O que acontece: o plano é verificado (nada é iniciado se não for executável), o OMSI arranca e carrega Grundorf, o programa lê o relógio uma vez e depois termina a sessão. O OMSI é terminado e cada ficheiro alterado pelo OmsiLaunch é restaurado. Só é permitido um proprietário por instalação: deve parar-se primeiro qualquer sessão de `OmsiLaunch.exe` para a mesma instalação; caso contrário, `StartSessionAsync` reporta `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Próximos passos

- Mais operações de runtime (veículos, horário, scripts, D3D): [controlo de runtime](../reference/runtime-control.md) e [capacidades](../reference/capabilities.md).
- Definições, splash e Internet Textures na especificação: [LaunchSpec](../reference/launchspec.md).
- Iniciar um perfil de sessão a partir do código: [API pública, perfis de sessão](../reference/public-api.md#session-profiles-omsilaunchcore).
- Controlar uma sessão pertencente a outro processo (por exemplo `OmsiLaunch.exe`): [controlo local](../reference/local-control.md).
- Erros: [erros](../reference/errors.md); ciclo de vida: [ciclo de vida da sessão](../concepts/session-lifecycle.md).
