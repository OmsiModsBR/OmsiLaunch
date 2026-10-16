# 公開 API 快速入門

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../getting-started/api-quick-start.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁示範如何從您自己的 .NET 程式啟動、讀取並結束一個 OmsiLaunch 工作階段。本頁假設套件已安裝在 OMSI 2 目錄中（請參閱[安裝](installation.md)），且 `OmsiLaunch.exe /plan` 在該處可正常運作（請參閱[第一個工作階段](first-session.md)）。完整的合約請見[公開 API 參考](../reference/public-api.md)。

預留位置：`<OMSI_PATH>` 是包含 `Omsi.exe` 與 OmsiLaunch 套件的 OMSI 2 安裝（例如 `C:\OMSI 2`）。

<a id="1-project"></a>
## 1. 專案

API 以 `<OMSI_PATH>` 中的套件組件形式提供；沒有 NuGet 套件。請從 x64 `net6.0-windows` 主控台專案參考這些組件。`OmsiLaunch.Api`、`OmsiLaunch.Core` 與 `OmsiLaunch.Process` 是公開介面；其他組件是它們的相依項目，必須複製到您的程式旁。

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

請將 `OmsiPath` 中的 `<OMSI_PATH>` 替換為實際路徑。

<a id="2-program"></a>
## 2. 程式

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

以安裝目錄作為唯一引數執行：

```powershell
dotnet run -- "<OMSI_PATH>"
```

執行過程：先檢查計畫（若不可執行則不會啟動任何東西），OMSI 啟動並載入 Grundorf，程式讀取一次時鐘，然後結束工作階段。OMSI 會被終止，OmsiLaunch 變更過的每個檔案都會還原。每個安裝只允許一個擁有者：請先停止同一安裝的任何 `OmsiLaunch.exe` 工作階段，否則 `StartSessionAsync` 會回報 `OL_E_INSTALLATION_BUSY`。

<a id="3-next-steps"></a>
## 3. 後續步驟

- 更多執行階段操作（車輛、時刻表、指令碼、D3D）：[執行階段控制](../reference/runtime-control.md)與[功能](../reference/capabilities.md)。
- spec 中的設定、啟動畫面與 Internet Textures（網路材質）：[LaunchSpec](../reference/launchspec.md)。
- 從程式碼啟動工作階段設定檔：[公開 API，工作階段設定檔](../reference/public-api.md#session-profiles-omsilaunchcore)。
- 控制由其他處理程序（例如 `OmsiLaunch.exe`）擁有的工作階段：[本機控制](../reference/local-control.md)。
- 錯誤：[錯誤](../reference/errors.md)；生命週期：[工作階段生命週期](../concepts/session-lifecycle.md)。
