# Быстрый старт с публичным API

<!-- l10n: source=getting-started/api-quick-start.md source-sha256=cd9b92efa4f35f7a4f8947145e72af4b066eb29e49c6f4435497e0001eadc69c -->
> Перевод [исходной страницы на английском языке](../../../getting-started/api-quick-start.md) для OmsiLaunch 0.1.0-beta3. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице показано, как запустить, прочитать и завершить один сеанс OmsiLaunch из собственной программы на .NET. Предполагается, что пакет установлен в каталог OMSI 2 (см. [установка](installation.md)) и что `OmsiLaunch.exe /plan` там работает (см. [первый сеанс](first-session.md)). Полный контракт описан в [справочнике публичного API](../reference/public-api.md).

Заполнители: `<OMSI_PATH>` — установка OMSI 2, в которой находятся `Omsi.exe` и пакет OmsiLaunch (например, `C:\OMSI 2`).

<a id="1-project"></a>
## 1. Проект

API поставляется в виде сборок пакета в `<OMSI_PATH>`; пакета NuGet нет. Подключите их в качестве ссылок из консольного проекта x64 `net6.0-windows`. `OmsiLaunch.Api`, `OmsiLaunch.Core` и `OmsiLaunch.Process` образуют публичную поверхность; остальные сборки — их зависимости, и они должны быть скопированы рядом с вашей программой.

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

Замените `<OMSI_PATH>` в `OmsiPath` на реальный путь.

<a id="2-program"></a>
## 2. Программа

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

Запустите программу, передав установку единственным аргументом:

```powershell
dotnet run -- "<OMSI_PATH>"
```

Что происходит: план проверяется (если он неисполним, ничего не запускается), OMSI запускается и загружает Grundorf, программа один раз считывает часы, затем завершает сеанс. OMSI принудительно завершается, и каждый файл, изменённый OmsiLaunch, восстанавливается. На одну установку допускается только один владелец: сначала остановите все сеансы `OmsiLaunch.exe` для этой же установки, иначе `StartSessionAsync` сообщит `OL_E_INSTALLATION_BUSY`.

<a id="3-next-steps"></a>
## 3. Дальнейшие шаги

- Другие runtime-операции (транспортные средства, расписание, скрипты, D3D): [runtime-управление](../reference/runtime-control.md) и [возможности](../reference/capabilities.md).
- Параметры, заставка и интернет-текстуры (Internet Textures) в спецификации: [LaunchSpec](../reference/launchspec.md).
- Запуск профиля сеанса из кода: [публичный API, профили сеанса](../reference/public-api.md#session-profiles-omsilaunchcore).
- Управление сеансом, владельцем которого является другой процесс (например, `OmsiLaunch.exe`): [локальное управление](../reference/local-control.md).
- Ошибки: [ошибки](../reference/errors.md); жизненный цикл: [жизненный цикл сеанса](../concepts/session-lifecycle.md).
