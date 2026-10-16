# 公共 API 快速入门

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../getting-started/api-quick-start.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页介绍如何在您自己的 .NET 程序中启动、读取并结束一个 OmsiLaunch 会话。本页假定包已安装在 OMSI 2 目录中（见[安装](installation.md)），并且 `OmsiLaunch.exe /plan` 可以在该目录中正常工作（见[第一个会话](first-session.md)）。完整的契约见[公共 API 参考](../reference/public-api.md)。

占位符：`<OMSI_PATH>` 是包含 `Omsi.exe` 和 OmsiLaunch 包的 OMSI 2 安装实例（例如 `C:\OMSI 2`）。

<a id="1-project"></a>
## 1. 项目

API 以 `<OMSI_PATH>` 中的包程序集形式提供；没有 NuGet 包。请在 x64 `net6.0-windows` 控制台项目中引用这些程序集。`OmsiLaunch.Api`、`OmsiLaunch.Core` 和 `OmsiLaunch.Process` 是公共接口；其他程序集是它们的依赖项，必须复制到您的程序旁边。

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

请将 `OmsiPath` 中的 `<OMSI_PATH>` 替换为实际路径。

<a id="2-program"></a>
## 2. 程序

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

以安装实例作为唯一参数运行：

```powershell
dotnet run -- "<OMSI_PATH>"
```

运行过程：检查计划（如果计划不可运行，则不启动任何内容），OMSI 启动并加载 Grundorf，程序读取一次时钟，然后结束会话。OMSI 被终止，OmsiLaunch 更改过的每个文件都会被还原。每个安装实例只允许一个所有者：请先停止同一安装实例上的任何 `OmsiLaunch.exe` 会话，否则 `StartSessionAsync` 会报告 `OL_E_INSTALLATION_BUSY`。

<a id="3-next-steps"></a>
## 3. 后续步骤

- 更多运行时操作（车辆、时刻表、脚本、D3D）：[运行时控制](../reference/runtime-control.md)和[能力](../reference/capabilities.md)。
- spec 中的设置项、启动画面和网络纹理（Internet Textures）：[LaunchSpec](../reference/launchspec.md)。
- 从代码启动会话配置档：[公共 API：会话配置档](../reference/public-api.md#session-profiles-omsilaunchcore)。
- 控制由另一个进程（例如 `OmsiLaunch.exe`）拥有的会话：[本地控制](../reference/local-control.md)。
- 错误：[错误](../reference/errors.md)；生命周期：[会话生命周期](../concepts/session-lifecycle.md)。
