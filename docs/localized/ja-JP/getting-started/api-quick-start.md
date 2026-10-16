# 公開 API クイックスタート

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../getting-started/api-quick-start.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、独自の .NET プログラムから OmsiLaunch セッションを 1 つ開始し、読み取り、終了します。パッケージが OMSI 2 のディレクトリにインストールされていること（[インストール](installation.md)を参照）、およびそこで `OmsiLaunch.exe /plan` が動作すること（[最初のセッション](first-session.md)を参照）を前提とします。完全な契約は[公開 API リファレンス](../reference/public-api.md)にあります。

プレースホルダー: `<OMSI_PATH>` は、`Omsi.exe` と OmsiLaunch パッケージを含む OMSI 2 のインストール環境です（例: `C:\OMSI 2`）。

<a id="1-project"></a>
## 1. プロジェクト

API は `<OMSI_PATH>` 内のパッケージアセンブリとして提供されます。NuGet パッケージはありません。x64 の `net6.0-windows` コンソールプロジェクトからこれらを参照してください。`OmsiLaunch.Api`、`OmsiLaunch.Core`、`OmsiLaunch.Process` が公開サーフェスです。その他のアセンブリはそれらの依存関係であり、プログラムの隣にコピーする必要があります。

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

`OmsiPath` 内の `<OMSI_PATH>` を実際のパスに置き換えてください。

<a id="2-program"></a>
## 2. プログラム

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

インストール環境を唯一の引数として実行します。

```powershell
dotnet run -- "<OMSI_PATH>"
```

実行される処理: プランが検査され（実行可能でない場合は何も開始されません）、OMSI が起動して Grundorf を読み込み、プログラムが時計を 1 回読み取り、その後セッションを終了します。OMSI は終了させられ、OmsiLaunch が変更したすべてのファイルが復元されます。インストール環境ごとに許可されるオーナーは 1 つだけです。先に同じインストール環境の `OmsiLaunch.exe` セッションをすべて停止してください。そうしないと `StartSessionAsync` が `OL_E_INSTALLATION_BUSY` を報告します。

<a id="3-next-steps"></a>
## 3. 次のステップ

- その他のランタイム操作（車両、時刻表、スクリプト、D3D）: [ランタイム制御](../reference/runtime-control.md)と[ケイパビリティ](../reference/capabilities.md)。
- spec での設定、スプラッシュ、Internet Textures: [LaunchSpec](../reference/launchspec.md)。
- コードからセッションプロファイルを開始する: [公開 API、セッションプロファイル](../reference/public-api.md#session-profiles-omsilaunchcore)。
- 別のプロセス（例: `OmsiLaunch.exe`）が所有するセッションを制御する: [ローカル制御](../reference/local-control.md)。
- エラー: [エラー](../reference/errors.md)。ライフサイクル: [セッションのライフサイクル](../concepts/session-lifecycle.md)。
