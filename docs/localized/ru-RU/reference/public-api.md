# Справочник публичного API (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=03d2b00af2df06cdc28d5fbbec294f9f480584c4817b1dfe8b805a914621f734 -->
> Перевод [исходной страницы на английском языке](../../../reference/public-api.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Эта страница — нормативный справочник по управляемому публичному API OmsiLaunch 0.1.0-beta4: сборке `OmsiLaunch.Api` (контракты) и точке входа для интеграторов `OmsiLaunchService` в `OmsiLaunch.Core`. Здесь описано только то, что делает текущий код. Всё, что интегратор может вызвать, получить или наблюдать, перечислено здесь с уровнем стабильности; всё, что не перечислено, не является поверхностью интеграции.

Сгенерированная [инвентаризация публичного API](public-api-inventory.md) перечисляет каждый публичный тип и член `OmsiLaunch.Api`, `OmsiLaunch.Core` и `OmsiLaunch.Process` с сигнатурой и стабильностью; гейт документации завершается ошибкой, если инвентаризация и сборки расходятся. Эта страница объясняет семантику.

Связанные страницы: [справочник LaunchSpec](launchspec.md), [коды ошибок](errors.md), [жизненный цикл сеанса](../concepts/session-lifecycle.md), [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md), [runtime-управление](runtime-control.md), [возможности](capabilities.md), [локальная плоскость управления](local-control.md), [коды выхода](exit-codes.md), [статус проверки в runtime](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Словарь уровней стабильности

| Уровень | Значение на этой странице |
| --- | --- |
| `STABLE_BETA` | Контракт заморожен для линейки протокола 0.1, а путь проверен в runtime согласно `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Можно вызывать, покрыто тестами, но контракт или подтверждения в runtime могут измениться до того, как он станет стабильным. |
| `PARTIAL` | Присутствует в контракте; реализована или проверена только часть поведения (в тексте указано, какая именно). |
| `INTERNAL` | Публичен в сборке по техническим причинам (тип используется мостом совместно), но не является поверхностью интеграции; может измениться без предупреждения. |
| `UNAVAILABLE` | Присутствует в контракте, но отклоняется текущим билдом. |

<a id="assembly-overview"></a>
## Обзор сборок

| Сборка | Роль для интеграторов |
| --- | --- |
| `OmsiLaunch.Api` | Чистые контракты: records, перечисления, `IOmsiLaunch`, реестр возможностей, каталог ошибок, форматы передачи, вспомогательные методы D3D. Не содержит `IntPtr`, `nint`, дескрипторов Win32, нативных адресов или объектов процессов. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (реализация `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` и `CurrentWindowsX64Platform` (единственный адаптер платформы), `InstallationLease`. Нужна для создания сервиса. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Сборки реализации. Их публичные типы для интеграторов являются `INTERNAL`. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Точка входа: `OmsiLaunchService` и `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Параметр | Допустимое значение | Недопустимое / по умолчанию |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (пространство имён `OmsiLaunch.Process`). Определяет платформу, создаёт процесс OMSI через `CreateProcessW`, ожидает его завершения и завершает его. | Другие реализации не поставляются. Собственная реализация `IRuntimePlatform` является `INTERNAL`. |
| `PluginBuildDirectory` | Каталог, содержащий эталонные файлы замыкания постоянного плагина (набора его файлов): `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` и все `OmsiLaunch.*.dll` управляемого замыкания (обязательно включая `OmsiLaunch.Plugin.dll`). В установленном пакете это `<package>\plugins`. | Отсутствует каталог или файл: `PlanSessionAsync` возвращает неисполнимый план с `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Путь к `OmsiLaunch.Native.x86.dll` (в пакете: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | То же, что выше. |
| `ReleaseManifestPath` | `release-manifest.json` рядом с `OmsiLaunch.exe`, если он есть. Задаёт ожидаемый SHA-256 каждого файла `plugins/` (`plugin.integrity.reference = manifest`). | `null` (раскладка для разработки): установленные файлы проверяются только на наличие и согласованность с эталонным замыканием (`plugin.integrity.reference = self`). Повреждённый манифест: `OL_E_RELEASE_MANIFEST_INVALID`. |

Сервис читает эти пути при каждом вызове `PlanSessionAsync` и `StartSessionAsync`; он никогда не копирует, не размещает и не удаляет файлы плагина (см. [постоянный плагин](../concepts/permanent-plugin.md)). CLI создаёт сервис именно так (`tools/OmsiLaunch.Cli/Program.cs`):

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

Создавайте один сервис на процесс и используйте его совместно. Стабильность: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Правила владения сеансом

| Правило | Подробности |
| --- | --- |
| Один владелец на установку | `StartSessionAsync` захватывает аренду установки (lease) — именованный семафор `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>` — и удерживает её, пока супервизор не восстановит установку. Второй запуск на том же корневом каталоге из любого процесса того же сеанса входа в систему завершается с `OL_E_INSTALLATION_BUSY` (сообщается как сеанс `Failed`, см. `StartSessionAsync`). Аренда действует в пределах сеанса входа в систему, а не между сеансами входа, и не освобождается, пока другой процесс удерживает её дескриптор (принятый риск). |
| Handles локальны для процесса | `SessionHandle` оборачивает `Guid` сеанса. Он имеет смысл только для того экземпляра `OmsiLaunchService`, который его вернул. Handle, построенный из известного `Guid` в другом процессе (или другом экземпляре сервиса), приводит к `KeyNotFoundException`. Межпроцессное управление выполняется через [локальную плоскость управления](local-control.md), а не через handles. |
| Всегда вызывайте `CloseAsync` | Начиная с `StartSessionAsync` процесс владеет долговременной транзакцией. `CloseAsync` при необходимости запрашивает каноническую остановку, ожидает супервизор (выход процесса, точное восстановление, освобождение аренды) и забывает сеанс. Его необходимо вызывать на каждом пути выхода, в том числе после состояния `Failed`. Без него запись о сеансе остаётся в памяти; само восстановление супервизор выполняет в любом случае. |
| Сеансы с ошибкой — тоже сеансы | Запуск, завершившийся ошибкой после возврата `StartSessionAsync`, сообщает `SessionState.Failed`; handle остаётся действительным для `GetStatusAsync`/`WaitForAsync` до вызова `CloseAsync`. |
| Планы перепроверяются | `StartSessionAsync` заново хеширует `Omsi.exe` и выполняет повторное планирование спецификации; план, который больше не является исполнимым, отклоняется с `OL_E_PLAN_NOT_RUNNABLE`. |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

Общие факты для всех методов:

- Неизвестные или уже закрытые handles приводят к исключению `KeyNotFoundException` («Unknown OmsiLaunch session.»).
- Ни один метод, кроме `ExecuteRuntimeAsync`, не требует сеанса в состоянии Running.
- Исключения, несущие код OmsiLaunch, помещают код в начало `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). CLI извлекает коды из сообщений тем же способом (`CliProgram.Classify`).
- Поддерживаемая установка: OMSI 2. Нативные возможности определяются и проверяются отдельно для каждой сборки в процессе; возможность, недоступная на сборке, завершается с `OL_E_CAPABILITY_UNRESOLVED`. См. [совместимость](compatibility.md).

<a id="complete-minimal-example"></a>
### Полный минимальный пример

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Назначение | Скомпилировать `LaunchSpec` в `SessionPlan` без запуска OMSI: проверить спецификацию, определить платформу, снять отпечаток `Omsi.exe`, разрешить идентичности контента, вычислить запланированные изменения файлов, перечислить требуемые и неподдерживаемые возможности и определить `IsRunnable`. Публичная возможность `session.plan`. |
| Параметры | `spec`: полностью заполненный `LaunchSpec` (см. [справочник LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (все восемь словарей) и `Behavior` должны быть не равны null; необязательные члены могут быть `null`. `RootPath` должен быть абсолютным путём к каталогу; пустой корневой путь фиксируется как `OL_E_INSTALLATION_NOT_FOUND`, но проверка платформы на пустом пути выбрасывает `ArgumentException` ещё до возврата плана, поэтому никогда не передавайте пустой корневой путь. |
| Возвращает | `SessionPlan` с новым `SessionId`, `BuildProfileId` = известная раскладка (`"Omsi23004"`) или `"unrecognized"` для сборки OMSI 2 без известной раскладки, входной `Spec`, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (пути назначения `plugins\OmsiLaunch.*` плюс `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (по одному `PlannedModule` для каждого запрошенного или активированного содержимым [runtime-модуля](runtime-modules.md); метод доступа `EffectivePlannedModules`). `IsRunnable` равен `true` ровно тогда, когда ни один код диагностики не начинается с `OL_E_`. Информационные диагностические сообщения (`plugin.integrity.reference` с сообщением `self` или `manifest`, `session_profile.selected`) никогда не делают план неисполнимым. |
| Ошибки в результате | Каждая ошибка планирования — это диагностическое сообщение, а не исключение: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (сообщение содержит код splash/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (установленное замыкание плагина в `plugins\` сверяется с релизным манифестом на этапе планирования), `OL_E_RUNTIME_ARTIFACT_MISSING` (сообщение может содержать `OL_E_RELEASE_MANIFEST_INVALID`). Полные условия: [правила проверки LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Исключения | `OperationCanceledException`, если токен уже отменён на входе (единственная точка проверки); `ArgumentException`/`NotSupportedException` для синтаксически недопустимых корневых путей; `NullReferenceException`/`ArgumentNullException` для обязательных членов, равных null; `System.Text.Json.JsonException` для синтаксически недопустимого релизного манифеста. |
| Отмена | Проверяется один раз на входе. Дальше планирование — синхронная работа с файловой системой. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет состояние OMSI | Нет. |
| Изменяет файловую систему | Нет (читает `Omsi.exe`, файлы контента, замыкание плагина, манифест). Значения параметров здесь не проверяются (только существование ключа и возможность записи); недопустимое значение приводит к ошибке при запуске с `OL_E_INVALID_SETTING_VALUE`. |
| Транзакция / восстановление | Нет. |
| Ограничения | Запрос любого режима `Date`/`Time`/`Year`, кроме `Unset`, любого режима `Weather`, кроме `Unset`, любого поля `PlayerVehicle`, документов `Input`, `EntrypointIdentity` или `WorldMode.LastMapState` на этом билде даёт `OL_E_CAPABILITY_UNAVAILABLE` и неисполнимый план (записи в `UnsupportedRequestedFeatures` с подтверждениями из каталога, `UNAVAILABLE` или `PARTIAL`). |
| Стабильность | `STABLE_BETA`. |
| Пример | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Назначение | Запустить транзакционный управляемый сеанс OMSI по исполнимому плану: захватить аренду установки, восстановить устаревший журнал, проверить замыкание постоянного плагина, сделать снимок (snapshot) файлов сеанса и наложить overlay (временная замена файла на время сеанса), создать startup handoff, слот телеметрии и runtime-почтовый ящик (mailbox), запустить `Omsi.exe`, записать процесс в журнал и передать сеанс фоновому супервизору. Публичная возможность `session.start`. |
| Параметры | `plan`: `SessionPlan` с `IsRunnable == true`. Спецификация внутри плана проходит повторное планирование; из плана вызывающей стороны сохраняется только `plan.SessionId`. `plan.Spec.Behavior.StartupTimeoutSeconds` должен быть в диапазоне 1..600. |
| Возвращает | `SessionHandle(plan.SessionId)` сразу после того, как `Omsi.exe` создан и записан (состояние `WaitingForPlugin`), или сразу после того, как путь запуска завершился ошибкой (состояние `Failed`). Метод не ждёт начала игрового процесса; используйте `WaitForAsync(session, SessionState.Running, ...)`. |
| Исключения | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")`, если `plan.IsRunnable` равен false; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")`, если результат повторного планирования неисполним (например, изменился `Omsi.exe`, удалён контент, отсутствует замыкание плагина); `InvalidOperationException("Duplicate session id.")`, если сеанс с тем же идентификатором ещё зарегистрирован (сначала вызовите `CloseAsync`); `ArgumentOutOfRangeException`, если `StartupTimeoutSeconds` вне диапазона 1..600; `OperationCanceledException` при отмене до или во время повторного планирования; а также всё, что выбрасывает `PlanSessionAsync`. Во всех случаях с исключением сеанс не регистрируется. |
| Ошибки в результате | Любая ошибка после повторного планирования перехватывается внутри пути запуска: сеанс регистрируется, его состояние — `Failed`, а его диагностика содержит `OL_E_START_SESSION`, сообщение которого — внутреннее сообщение (начинающееся с внутреннего кода, если он есть): `OL_E_INSTALLATION_BUSY` (аренда удерживается или процесс OMSI, записанный в журнал, ещё жив), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (только если отложенная повторная попытка с overlays этого сеанса всё равно не может доказать владение), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. Очистка может добавить `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (выход OMSI не подтверждён; журнал сохранён) или `OL_E_RESTORE_FAILED`. О более поздних ошибках сообщает супервизор (см. [жизненный цикл сеанса](../concepts/session-lifecycle.md)). |
| Отмена | До или во время повторного планирования: выбрасывается исключение. После этого токен передаётся транзакции и созданию процесса; отмена на этом этапе обрабатывается как любая ошибка запуска (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), процесс (если он создан) завершается, а установка восстанавливается. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет состояние OMSI | Да: создаёт процесс OMSI с переменными окружения `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Изменяет файловую систему | Да, внутри корневого каталога установки: `.omsilaunch\diagnostics\<sessionId>-host.log` (хранятся 50 последних сеансов), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (копируется один раз для управляемой заставки), overlays сеанса (правки `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), удаления на время сеанса (цели ITX, `Texture\standard.ipr`, `closecheck`), а также безвозвратное удаление ранее существовавшего устаревшего `closecheck`, если `SuppressStaleClosecheckWarning` равен true (диагностика `closecheck.stale-removed`). |
| Транзакция / восстановление | Открывает транзакцию (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Каждый путь выхода из сеанса заканчивается восстановлением. См. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md). |
| Ограничения | До игрового процесса доходят только `WorldMode.NewMap` с `PresentedEntrypointIndex` и `WorldMode.SavedSituation`. `WorldMode.LastMapState` — `UNAVAILABLE`. Запросы транспортного средства игрока никогда не доходят до этого метода, поскольку неисполнимы уже на этапе планирования. |
| Стабильность | `STABLE_BETA` (жизненные циклы NEW_MAP и SAVED_SITUATION проверены в runtime). |
| Пример | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Назначение | Прочитать семантическое состояние жизненного цикла, собранную на данный момент диагностику и ограниченный список runtime-событий. Публичная возможность `session.status`. |
| Параметры | `session`: handle, возвращённый `StartSessionAsync` и ещё не закрытый. |
| Возвращает | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: неизменяемый снимок (массивы копируются под блокировкой сеанса). Для живого сеанса `RuntimeEvents` никогда не равен `null`. |
| Исключения | `KeyNotFoundException` для неизвестных или закрытых handles. В остальных случаях исключений не бывает. |
| Отмена | Токен игнорируется (вызов завершается синхронно). |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет OMSI / файловую систему / транзакцию | Нет / Нет / Нет. |
| Стабильность | `STABLE_BETA`. |
| Пример | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Назначение | Опрашивать (каждые 100 ms), пока сеанс не окажется в состоянии `state` или в терминальном состоянии (`Completed`, `Failed`) либо пока не истечёт тайм-аут; затем вернуть текущий статус. |
| Параметры | `state`: любое значение `SessionState`. Ожидание переходного состояния, которое уже пройдено (или никогда не устанавливается, см. [жизненный цикл сеанса](../concepts/session-lifecycle.md)), длится до терминального состояния или тайм-аута. `timeout`: любой неотрицательный `TimeSpan` или `Timeout.InfiniteTimeSpan`. |
| Возвращает | Статус на момент окончания ожидания. При тайм-ауте возвращается статус, а не исключение: проверяйте `State` самостоятельно. Ожидание `Running`, которое заканчивается состоянием `Failed`, возвращается сразу с диагностикой ошибки. |
| Исключения | `KeyNotFoundException`; `OperationCanceledException`, если отменён токен вызывающей стороны (распространяется только отмена вызывающей стороны; внутренний тайм-аут — нет). |
| Отмена | Токен вызывающей стороны учитывается на каждом такте 100 ms. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет OMSI / файловую систему / транзакцию | Нет / Нет / Нет. |
| Стабильность | `STABLE_BETA`. |
| Пример | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Назначение | Запросить каноническую остановку. Метод устанавливает флаг остановки и сразу возвращается; супервизор замечает флаг в своём цикле 100 ms, вызывает `TerminateProcess` для `Omsi.exe`, ждёт выхода, помечает журнал как `ProcessExited`, восстанавливает все файлы, которыми владеет сеанс, и освобождает аренду. Это принудительное завершение: собственная процедура завершения OMSI не выполняется, и OMSI не перезаписывает `options.cfg` при выходе (так задумано, это защищает транзакцию). Кооперативное завершение через `WM_CLOSE` не реализовано (решение по продукту; в финальной проверке в runtime (runtime closure) OMSI не закрылась в течение 30 s после `WM_CLOSE`, `L05b`). Публичная возможность `session.stop`. |
| Параметры | `session`. |
| Возвращает | Завершённую задачу; не ждёт ни завершения процесса, ни восстановления. Для наблюдения за завершением используйте `WaitForAsync(session, SessionState.Completed, ...)`. |
| Исключения | `KeyNotFoundException`. |
| Отмена | Токен игнорируется. |
| Требуется сеанс в состоянии Running | Нет. Идемпотентен; остановка, запрошенная до запуска супервизора, выполняется, как только он запустится; остановка терминального сеанса ничего не делает. |
| Изменяет состояние OMSI | Да: завершает процесс OMSI (код выхода 1). |
| Изменяет файловую систему | Косвенно: запускает восстановление, удаление журнала и удаление резервных копий супервизором. |
| Транзакция / восстановление | Запускает `ProcessExited` → `Restoring` → `Restored`. Изменения на стороне runtime, сделанные через `ExecuteRuntimeAsync` (запись часов, созданные ТС, переменные скриптов, текстуры D3D), не восстанавливаются; они исчезают вместе с процессом. |
| Стабильность | `STABLE_BETA`. |
| Пример | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Назначение | Освободить handle потребителя, не бросая транзакцию незавершённой: если сеанс не в терминальном состоянии, запросить каноническую остановку; затем дождаться задачи жизненного цикла супервизора (выход процесса, восстановление, освобождение аренды); затем забыть сеанс. |
| Параметры | `session`. |
| Возвращает | Завершается, когда сеанс находится в терминальном состоянии и удалён. После возврата handle становится неизвестным (`KeyNotFoundException` при любом последующем вызове, включая повторный `CloseAsync`). |
| Исключения | `KeyNotFoundException`; `OperationCanceledException`, если вызывающая сторона отменяет операцию во время ожидания супервизора. В этом случае сеанс не удаляется, а супервизор продолжает работу; вызовите `CloseAsync` снова. |
| Отмена | Относится только к ожиданию; восстановление никогда не отменяется. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет состояние OMSI | Да, если сеанс ещё живой (так же, как `StopAsync`). |
| Изменяет файловую систему | Косвенно (восстановление супервизором). |
| Транзакция / восстановление | Гарантирует, что транзакция доведена до конца, прежде чем handle будет освобождён (если супервизор был запущен). Для сеанса, завершившегося ошибкой до запуска супервизора, путь запуска уже выполнил восстановление или сообщил `OL_E_RESTORE_DEFERRED`. |
| Стабильность | `STABLE_BETA`. |
| Пример | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Назначение | Выполнить одну публичную runtime-операцию внутри работающего процесса OMSI через почтовый ящик сеанса, допускающий один запрос за раз (отображаемый в память файл, 64 KiB, запрос привязан к идентификатору сеанса и идентификатору запроса). Плагин выполняет операцию в UI-потоке OMSI. Каталог операций: [runtime-управление](runtime-control.md) и [возможности](capabilities.md). |
| Параметры | `command.SessionId` должен совпадать с `session.SessionId`. `command.RequestId`: `ulong`, выбираемый вызывающей стороной; используйте строго возрастающий счётчик на процесс (вспомогательные методы D3D начинают с 30 000, владелец в CLI — с 10 001/50 000). `command.Operation`: публичный идентификатор операции из `PublicCapabilityRegistry.PublicRuntimeOperationIds` (например, `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: строковые значения с порядковыми (ordinal) именами в качестве ключей; обязательные имена для каждой операции берутся из `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: отсчитывается с момента размещения запроса в почтовом ящике (ожидание в очереди за другой выполняющейся командой не учитывается). CLI использует 5 s (15 s для `road-vehicles.spawn`) в режиме владельца и 8 s / 30 s в режиме клиента. |
| Порядок проверок | 1. Проверка по реестру (до поиска сеанса): неизвестная операция или операция `internal.*` → результат `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; отсутствует обязательный аргумент (не передан или состоит из пробелов) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Поиск сеанса → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Состояние не `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Запрос через почтовый ящик. 6. Значения результата, ключ которых начинается с `internal_` или заканчивается на `_address`, `_pointer`, `_vmt`, удаляются. |
| Возвращает | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. При успехе `Values` содержит семантические строки операции (описаны для каждой операции в разделе [runtime-управление](runtime-control.md)). |
| Ошибки в результате | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (реестр); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (результат плагина превысил размер почтового ящика; результаты ограниченных списков вместо этого сокращаются с `truncated=true`); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` с производным ключом); все коды `OL_E_D3D_*` (с `Values["detail"]` и `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (нативная возможность операции не определена на работающей сборке); и каждая типизированная ошибка на стороне плагина с собственным кодом в `ErrorCode` и сообщением в `Values["detail"]` (Beta 4; только ошибка без кода из каталога — это `OL_E_RUNTIME_OPERATION_FAILED`). Коды, приходящие таким образом: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (проверки на стороне плагина), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. См. [коды ошибок](errors.md). |
| Исключения | `KeyNotFoundException`; `InvalidOperationException` с `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (почтовый ящик уже освобождён супервизором), `OL_E_RUNTIME_CHANNEL_BUSY` (в слоте всё ещё находится брошенный запрос), `OL_E_RUNTIME_REQUEST_ID_REUSED` (в слоте всё ещё находится устаревший ответ с тем же идентификатором запроса); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (повреждённый, чужой или несоответствующий ответ); `ArgumentOutOfRangeException`, если сериализованный запрос превышает размер почтового ящика; `OperationCanceledException`. |
| Отмена | Учитывается во время ожидания гейта сеанса и каждые 20 ms при опросе ответа. Отмена во время выполнения не сбрасывает слот: следующий вызов в этом сеансе может завершиться с `OL_E_RUNTIME_CHANNEL_BUSY`, пока плагин не опубликует свой ответ (который затем отбрасывается как устаревший). Предпочтительнее использовать тайм-аут: тайм-аут сбрасывает слот, а опоздавший ответ распознаётся и отбрасывается. |
| Требуется сеанс в состоянии Running | Да (`SessionState.Running`); иначе выбрасывается `OL_E_SESSION_NOT_RUNNING`. Почтовый ящик существует, пока супервизор не освободит его во время восстановления. |
| Изменяет состояние OMSI | Зависит от операции: операции `Read` не изменяют; операции `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) изменяют состояние внутри процесса, которое не восстанавливается. |
| Изменяет файловую систему | Хост ничего не записывает. OMSI может в результате записать собственные файлы (не отслеживается). |
| Транзакция / восстановление | Нет. |
| Ограничения | Одна выполняющаяся команда на сеанс (вызовы в одном сеансе сериализуются). Запрос и ответ ограничены каждый 64 KiB минус 8 байт; пиксельные данные D3D — 48 KiB. `internal.road-vehicles.make-basic` является `INTERNAL` и недостижима. `weather.set` и `weather.metar.set` — `EXPERIMENTAL`. `timetable.logs.read` не является ограниченным списком и на больших расписаниях может вернуть `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. `camera.lock` — `EXPERIMENTAL`; для неё нужно транспортное средство игрока, и она проверена в runtime (`CAM01`), хотя строка `RuntimeValidation` в реестре всё ещё содержит `STATICALLY_VALIDATED`. Handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) действуют в пределах сеанса. |
| Стабильность | Транспорт и контракт — `STABLE_BETA`; стабильность каждой операции определяется `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`) с указанными выше исключениями. |
| Пример | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Назначение | Вернуть каталог возможностей, оценённый для установки: по одной записи `Capability(Name, Available, EvidenceState, Reason)` на каждую запись `PublicCapabilityRegistry`, не являющуюся `InternalOnly`. `Name` — идентификатор из каталога, `EvidenceState` — подтверждение из каталога (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` вычисляется по платформе, проверке OMSI 2, классификации и нативным возможностям, которые нужны записи (см. [возможности](capabilities.md#getcapabilitiesasync)). |
| Параметры | `installation.RootPath`: проверяемая установка (платформа, `Omsi.exe`). `ExpectedExecutableSha256` игнорируется. |
| Возвращает | Одна запись на каждую неслужебную запись каталога, например `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` на известной раскладке), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Связь с `PublicCapabilityRegistry` | Выводится из него: реестр объявляет возможности, их операции и подтверждения; этот вызов оценивает их для одной установки. |
| Исключения | `OperationCanceledException` на входе; `ArgumentException` для пустого корневого пути. |
| Отмена | Проверяется один раз на входе. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет OMSI / файловую систему / транзакцию | Нет / Нет / Нет. |
| Стабильность | Контракт вызова — `STABLE_BETA`; содержимое списка — поддерживаемая вручную инвентаризация: `PARTIAL`. |
| Пример | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Назначение | Перечислить установленный контент и вернуть канонические идентичности, пригодные для `LaunchSpec`. Обнаружение пропускает точки повторного анализа (reparse points), поэтому циклы junction не могут его подвесить, читает файлы OMSI в кодировке Windows-1252 (UTF-8/UTF-16 с BOM учитываются) и никогда не следует по символическим ссылкам. |
| Параметры | `query` и `scope` согласно таблице ниже. `scope` обязателен для `Entrypoints` (идентичность карты), `Repaints`, `FleetNumbers`, `Registrations` (идентичность транспортного средства). |
| Возвращает | Отсортированный список `ContentIdentity(Identity, Kind, DisplayName)`. Идентичности — пути относительно установки с обратными слешами; сравнение без учёта регистра. |
| Исключения | `OperationCanceledException` на входе; `ArgumentException`, если `Entrypoints` запрашивается без scope или корневой путь пуст; `FileNotFoundException` (без кода `OL_E_`; CLI сопоставляет его с `OL_E_NOT_FOUND`), если карта или транспортное средство из scope не установлены. Отсутствующий корневой каталог или каталог контента даёт пустой список, а не ошибку. |
| Отмена | Проверяется один раз на входе. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет OMSI / файловую систему / транзакцию | Нет / Нет / Нет. |
| Стабильность | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (через них разрешается каждый план, проверенный в runtime). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (только статические подтверждения). |
| Пример | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Значения `ContentQueryKind` и результаты:

| Значение | Scope | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | нет | `maps\<dir>\global.cfg` | `map` | имя каталога карты |
| `Situations` | нет | `situations\...\<file>.osn` | `situation` | идентичность карты, на которую ссылается `.osn` (может быть `null`) |
| `Vehicles` | нет | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` или имя файла |
| `Repaints` | идентичность транспортного средства (обязательна; без неё — пустой список) | `<cti path>#item:<ordinal>` | `repaint` | имя `[item]` |
| `Hofs` | нет | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | идентичность транспортного средства (обязательна; без неё — пустой список) | путь к источнику `[number]` относительно транспортного средства | `fleet-number` | `null` |
| `Registrations` | идентичность транспортного средства (обязательна; без неё — пустой список) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | первая строка значения (`null` для free) |
| `Addons` | нет | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | идентичность карты (обязательна; без неё — `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | метка точки входа |
| `SessionProfiles` | нет | имя каталога пакета (идентификатор для `/predefined-profile`) | `session-profile` или `session-profile-invalid`, если пакет не компилируется | `name` профиля или код `OL_E_SESSION_PROFILE_*` недопустимого пакета |
| `RuntimeModules` | нет | идентификатор модуля | `runtime-module` или `runtime-module-invalid`, если его не удаётся загрузить | `name` модуля или код `OL_E_MODULE_*` |

Точка входа выбирается через `PresentedEntrypointIndex` или `EntrypointIdentity` (идентичность из обнаружения или точная метка). Планировщик определяет идентичность до метки, которую показывает OMSI (OMSI показывает только первую запись для каждой точной метки), и план перечисляет её в `ResolvedContent` с видом `entrypoint`; `world.entrypoint-identity` — `EXPERIMENTAL`, пока не проверено в runtime.

### `ListSessionProfilesAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Назначение | Перечислить все пакеты профилей сеанса в `<root>\.omsilaunch\session-profiles\` вместе с их пресетами, чтобы фронтенд мог предложить их без разбора YAML. CLI: `/list:SessionProfiles`. |
| Возвращает | По одному `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` на каталог пакета, отсортированные по `Id`. `Presets` — это `SessionProfilePresetSummary(Index, Id, Name)` в порядке индексов. Каждый пакет компилируется по тем же строгим правилам, что и при запуске (`SessionProfileCompiler`); пакет, который не компилируется, перечисляется с `IsValid = false`, кодом `OL_E_SESSION_PROFILE_*`, сообщением и без пресетов. Нет каталога профилей: пустой список. |
| Исключения | `OperationCanceledException` на входе. Ошибки компиляции сообщаются в результате, а не выбрасываются. |
| Требуется сеанс в состоянии Running | Нет. |
| Изменяет OMSI / файловую систему / транзакцию | Нет / Нет / Нет. |
| Стабильность | `STABLE_BETA` (офлайн-гейт `session-profiles.discovery`). |
| Пример | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Назначение | Обнаружить [runtime-модули](runtime-modules.md), установленные ниже `plugins\OmsiLaunch.Modules`, с их целостностью и параметрами. Только чтение. Загрузка открыта: ни один модуль не проверяется по списку или подписи. |
| Возвращает | По одному `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` на каталог модуля, отсортированные по имени каталога. `Activation` — `requested`, `content` или `default`; `IsValid` равно false с кодом `OL_E_MODULE_*`, если модуль не удаётся загрузить; `Settings` перечисляет параметры, которые объявляет модуль, как `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Исключения | `OperationCanceledException` на входе. |
| Требуется сеанс в состоянии Running | Нет. CLI: `/list:RuntimeModules` (конверт `runtime-modules.list`). |

### `RecoverPendingAsync`

| Аспект | Подробности |
| --- | --- |
| Сигнатура | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Назначение | Сообщить о незавершённой долговременной транзакции (`<root>\.omsilaunch\journal.json`), оставленной аварийно завершившимся владельцем, или завершить её. На время вызова захватывает аренду установки, поэтому никогда не выполняет восстановление под запускающимся сеансом. Публичная возможность `session.recover`; в CLI — `/recovery-status` и `/recover`. |
| Параметры | `installation.RootPath`: корневой каталог установки (нормализуется через `Path.GetFullPath`). `restore`: `false` — только отчёт; `true` — восстановить, проверить, удалить журнал и резервные копии. |
| Возвращает | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` — журнал существовал в момент начала вызова; `Recovered` — восстановление было запрошено, выполнено, и журнала не осталось; `Diagnostics` — заметки о восстановлении (`restore.session-artifact-removed` с `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), пусто, если ничего не восстанавливалось. |
| Исключения | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")`, если аренда удерживается; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")`, если PID, время создания и путь к исполняемому файлу из журнала всё ещё соответствуют живому процессу или (журнал прошёл `HandoffCreated` без PID) запущен любой `Omsi.exe` из этого корневого каталога; `IOException` с `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` или сообщением проверки («Restore hash mismatch: ...», «Restore presence mismatch: ...»); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` для повреждённого журнала; `ArgumentException` для пустого корневого пути; `OperationCanceledException`. Если исключение возникает после начала восстановления, журнал сохраняется, и следующий вызов идемпотентно повторяет операцию. |
| Отмена | Передаётся в операции записи журнала и резервных копий; отмена посреди восстановления оставляет журнал незавершённым. |
| Требуется сеанс в состоянии Running | Нет (метод отказывается работать, пока владелец активен). |
| Изменяет состояние OMSI | Нет. |
| Изменяет файловую систему | Только при `restore == true`: перезаписывает оригиналы из проверенных резервных копий (байты, время последней записи, время создания, атрибуты; оригиналы только для чтения обрабатываются; сквозная запись + сброс буферов; не остаётся ни одного `*.omsilaunch.tmp`), удаляет артефакты сеанса, удаляет `journal.json` и `backup\<sessionId>`. |
| Транзакция / восстановление | Завершает незавершённую транзакцию (`Restoring` → `Restored` → журнал удалён). |
| Стабильность | `STABLE_BETA`: путь отчёта и восстановление после раннего выхода (матрица RV-008), восстановление после неудачного восстановления (runtime closure `F01`), отказ при живом владельце и в окне до получения PID (`S04`) и отложенное восстановление до снятия отпечатка при запуске (`S05`); см. [статус проверки в runtime](../status/runtime-validation-status.md). |
| Пример | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Типы контракта

<a id="optional-values-and-semantic-primitives"></a>
### Необязательные значения и семантические примитивы

| Тип | Определение | Примечания |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, статический `Unset`, статический `Set(T)` | Различает «не запрошено» и «запрошено со значением». Форма JSON описана в [справочнике LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Перечисление с базовым типом byte. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Проверяется только при `DateTimeMode.Explicit` (месяц 1..12, день 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Проверяется только при `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Семейство LaunchSpec

Все перечисленные ниже records описаны свойство за свойством в [справочнике LaunchSpec](launchspec.md); эта таблица фиксирует перечень типов.

| Тип | Назначение | Стабильность |
| --- | --- | --- |
| `LaunchSpec` | Корневая запись запроса с методами доступа `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, которые подставляют значения по умолчанию вместо необязательных членов, равных `null`. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (необязательная привязка `Omsi.exe`, проверяется при планировании). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Выбор мира. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (устаревший псевдоним `LastMapState`; никогда не означает «самый новый .osn»). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (вычисляется из `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Любой режим, кроме `Unset`, делает план неисполнимым. | `PARTIAL` (подтверждение плана `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Любой режим, кроме `Unset`, делает план неисполнимым. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Любое заданное поле делает план неисполнимым. | `PARTIAL` |
| `EnvironmentSpec` | Восемь групп `IReadOnlyDictionary<string, OptionalValue<string>>` семантических параметров `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; любое заданное значение делает план неисполнимым. | `PARTIAL` |
| `DiagnosticsSpec` | Шесть булевых значений; передаются, но не используются. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (псевдоним), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Происхождение скомпилированного профиля сеанса (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [Управление HOF](hof-management.md) сеанса: `Operations` (список `HofOperationSpec`). Поле `LaunchSpec.HofManagement`; метод доступа `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Runtime-модули](runtime-modules.md) для сеанса: `Enable` (идентификаторы модулей), `Disable` (идентификаторы модулей, которые не допускаются), `Settings` (`"<module id>.<key>"` → значение); методы доступа `EffectiveDisable`, `EffectiveSettings`. Поле `LaunchSpec.Modules`; метод доступа `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | Оценка модуля при планировании: `Id`, `Version`, `Status` (`ELIGIBLE` или `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` или `default`), `Settings`, `RuntimeOperations` (операции каталога, которые модуль выполняет в процессе, каждая с политикой изменения, если она меняет OMSI, например `camera.set (omsi-state)`), `TickMilliseconds`, `SnapshotSha256` (дайджест файлов модуля на этапе планирования, см. [runtime-модули](runtime-modules.md#integrity)); `IsEligible`, `EffectiveSettings`, `EffectiveRuntimeOperations`. Константы `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Один эффективный параметр модуля: `Key`, `Value`, `Source` (`request`, `settings-file` или `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Представление установленного модуля для обнаружения; см. `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Параметр, который объявляет модуль: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Одна операция HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Выбрасывается при планировании HOF (`OmsiLaunch.Core`); `Code` — код `OL_E_HOF_*`. Публичный API сообщает о нём как о диагностических сообщениях плана. | `STABLE_BETA` |
| `SessionProfileSummary` | Представление одного пакета для обнаружения (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). См. `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Один пресет в сводке (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (передаётся; восстановление выполняется всегда), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, по умолчанию 180), `ShutdownTimeoutSeconds` (передаётся, но не используется). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Типы плана и статуса

| Тип | Поля | Примечания |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (новый `Guid` для каждого плана), `BuildProfileId` (`"Omsi23004"` или `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (список `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (уникальные относительные пути из `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` с `STATICALLY_VALIDATED` или `UNAVAILABLE`; нативные возможности запуска — с `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` или `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (записи `Capability` для запрошенных, но неподдерживаемых функций), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Публичная запись: её можно изменить, или она может устареть, поэтому `StartSessionAsync` выполняет повторное планирование. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, ОС x64 и хост x64), `LegacyPlatform` (всегда `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (все равны `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` — значение `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), кроме `SessionPlan.RequiredCapabilities`: требование платформы или содержимого — `STATICALLY_VALIDATED` или `UNAVAILABLE`, нативная возможность — `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` или `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Изменения представления используют ключи `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (необязательный словарь строк). | Коды, начинающиеся с `OL_E_`, — ошибки, с `OL_W_` — предупреждения, всё остальное — информационные сообщения. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Диагностика сеанса не включает диагностику плана. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (время получения хостом), `Sequence` (начиная с 1, в пределах сеанса), `Data`, `SessionTickMilliseconds` (монотонные часы сеанса плагина в момент генерации события; для событий хоста — null). | Ограничено 256 последними событиями (самые старые отбрасываются). События плагина приходят по порядку через кольцо телеметрии; события, потерянные при переполнении кольца, учитываются в `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Локален для процесса. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | См. `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | См. `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Перечисление с базовым типом byte, в порядке объявления: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` и `EnteringGameplay` текущий сервис никогда не устанавливает; `Snapshotting` — переходное состояние, которое практически невозможно наблюдать. Терминальные состояния: `Completed`, `Failed`. Полная семантика: [жизненный цикл сеанса](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Типы runtime-управления

| Тип | Определение | Стабильность |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Статический кодек, который хост и плагин используют для конверта почтового ящика: сигнатура (magic) `0x4F4C5243` («OLRC»), версия 1, заголовок 72 байта в порядке little-endian (сигнатура, версия, вид 1 = запрос / 2 = ответ, общая длина, `Guid` сеанса, идентификатор запроса, длина полезной нагрузки, SHA-256 полезной нагрузки), за которым следует полезная нагрузка JSON в UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: публичен, потому что его используют обе стороны моста; не является поверхностью интеграции; формат может измениться вместе с версией протокола. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — то, что хост публикует для плагина в файле, отображаемом в память, `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256, string Settings, string SnapshotSha256)`: запланированный модуль, передаваемый в handoff (`StartupHandoff.Modules`); `SnapshotSha256` (версия формата 8) — дайджест файлов модуля, снятый при планировании, пустой, если план его не содержал. | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: определённые часы запуска сеанса NEW_MAP, передаваемые в handoff (`StartupHandoff.Start`); поле null сохраняет собственное значение OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Кодек: сигнатура `0x4F4C5348`, версия 8 (читает 3–8; версия 5 добавляет запланированные модули, версия 6 — эффективные параметры каждого модуля, версия 7 — дату и время запуска, версия 8 — снимок файлов каждого модуля при планировании; биты флагов 0x04 и 0x08 несут `PluginTrace` и `NativeTrace`), заголовок 64 байта с контролем целостности полезной нагрузки SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Кольцо событий от плагина к хосту в отображении `OMSILAUNCH_TELEMETRY_NAME`: сигнатура `0x52544C4F` ("OLTR"), версия 1, 64 нумерованных слота по 2048 байт. `Initialize`, `Write`, `IsRing`, `ReadSince` (возвращает события после номера последовательности и сколько было потеряно). | `INTERNAL` |

Плагин отклоняет handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`), если не выполнены все условия: `WorldMode` равен `NewMap` или `SavedSituation`, `HeadlessStart` равен true, `PlayerVehicleEnabled` равен false, оба режима даты и времени — `Unset`, а у сохранённой ситуации непустая идентичность. Планировщик применяет те же ограничения раньше, поэтому исполнимый план никогда к этому не приводит.

<a id="capability-registry-types"></a>
### Типы реестра возможностей

| Тип | Назначение |
| --- | --- |
| `PublicCapabilityRegistry` | Канонический каталог возможностей. `ProtocolVersion` (`"0.1"`), `All` (все `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (48 конкретных идентификаторов операций, которые фронтенд может пересылать), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (маршрут команды → идентификатор операции), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (возвращает `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Соблюдается `ExecuteRuntimeAsync`, CLI, локальной плоскостью управления и плагином. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (значение `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; производные `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (принимает `cursor`/`limit`, см. [постраничный вывод](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Что изменяет runtime-операция: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | Словарь подтверждений: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Полный каталог: [возможности](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Методы расширения `D3DRuntimeApi`

Типизированные обёртки над `ExecuteRuntimeAsync` для операций `d3d.*` (`EXPERIMENTAL`, возможность `d3d.texture` с классификацией `PublicExperimental`). Они выделяют идентификаторы запросов из общего для процесса счётчика, начинающегося с 30 000, и по умолчанию используют тайм-аут 5 s (кроме `GetD3DStatusAsync`, которому тайм-аут нужно передать явно).

| Метод | Операция | Аргументы и ограничения |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | нет |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | ширина/высота 1..4096, уровни 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | уровень 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, ширина/высота 1..4096, пиксели ≤ 48 KiB (при передаче кодируются в Base64) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | повторное освобождение отклоняется с `OL_E_D3D_RESOURCE_RELEASED` |

Типы: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` с `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Ошибки: неуспешный результат повторно выбрасывается как `OmsiRuntimeException(Code, detail)`, где `Code` — это `ErrorCode` результата (или `OL_E_RUNTIME_OPERATION_FAILED`, если он отсутствует), а сообщение — `"<code>: <Values["detail"]>"`; успешный результат без значений или неизвестная строка состояния устройства приводят к исключению `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Всё, что выбрасывает `ExecuteRuntimeAsync`, распространяется без изменений. Обработка сброса устройства (device reset) проверена в runtime: сброс переводит устройство через `Resetting` обратно в `Ready` и делает недействительными все живые текстуры (`OL_E_D3D_STALE_RESOURCE_HANDLE`, runtime closure `D01`); запись каталога `d3d.texture` это фиксирует. Переход в `Lost` невозможно вызвать извне продукта, и он проверен только офлайн-тестами.

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### Типы контракта процесса

| Тип | Содержимое |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Используется только CLI ([коды выхода](exit-codes.md)); API никогда не завершает процесс. |
| `PublicErrorCategory` | Строковые константы, используемые в конвертах ошибок CLI и локального управления: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | По одному `const string` на код (143: 142 ошибки `OL_E_` и 1 предупреждение `OL_W_`) и `All` — каталог `PublicErrorDescriptor(Code, Category)`. Категории: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Справочник: [коды ошибок](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Свойство `Code` плюс сообщение; выбрасывается только `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (идентичность установки и ограничение путей)

Стабильность: `STABLE_BETA` (чистые функции: без ввода-вывода, без состояния OMSI, без изменения файловой системы, без участия в транзакции, сеанс в состоянии Running не требуется). Это единое определение, которое используют аренда установки, имя pipe локального управления, ограничение ресурсов профиля сеанса пределами пакета, проверка целей интернет-текстур (Internet Textures) и путь к модели при runtime-создании (spawn).

| Член | Поведение |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` без завершающих разделителей, за исключением корня диска (`C:\`), который сохраняется. Разрешает сегменты `.` и `..`, одинаково обрабатывает `/` и `\` и схлопывает повторяющиеся разделители. **Не** разрешает junctions и символические ссылки. Выбрасывает `ArgumentException` для корневого пути, равного null или пустого. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` в верхнем регистре. Лексически эквивалентные написания одного корня (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) дают один ключ; разные корни (`C:\OMSI-A`, `C:\OMSI-B`) — никогда. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Разрешает `candidate` (относительно `root` или абсолютный) и возвращает `true` только тогда, когда он находится строго ниже `root`; `relativePath` — каноническое написание с `\`. Использует сегменты `Path.GetRelativePath`, поэтому соседний путь вроде `C:\OMSI-A\x` никогда не считается находящимся внутри `C:\OMSI`; сам корень, другие тома и выходы через `..` возвращают `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Разбивает по `/` и `\`, отбрасывая пустые сегменты. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Профили сеанса (`OmsiLaunch.Core`)

Стабильность: `EXPERIMENTAL`. Эти типы компилируют YAML-[профиль сеанса](session-profiles.md) (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, схема `omsilaunch.session-profile/v1`) в `LaunchSpec`. CLI `/predefined-profile:<id> /predefined-profile-index:<n>` использует именно эти вызовы; интегратор может применять их, чтобы запустить профиль через API.

| Член | Поведение |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Читает и проверяет пакет. `id` должен быть простым именем каталога (иначе `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` — 1..99 (`MinPresetIndex`..`MaxPresetIndex`; иначе `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Отсутствующий файл: `OL_E_SESSION_PROFILE_NOT_FOUND`; размер больше `MaxBytes` (256 KiB), недопустимый YAML, якоря, неизвестные ключи или `id`, отличающийся от имени каталога: `OL_E_SESSION_PROFILE_INVALID`; другое значение `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Пути к ресурсам ограничены пределами пакета (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Каждая ошибка — это `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Возвращает `baseline` с применённым профилем: для `NewMap` — блок `new` профиля (карта, точка входа и любые дата/время/год/погода); `settings` пресета, объединённые поверх `Environment.General`; `Presentation`, `InternetTextures` и `Behavior` пресета, если они заданы; и `SessionProfile` = `profile.Metadata`. Для `NewMap` также проверяется соответствие карты списку `compatibility` профиля (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | Проверка совместимости для остальных режимов (для `SavedSituation` карта читается из `.osn`). CLI вызывает её после построения окончательного `WorldSpec`. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` и допустимые ключи для каждого отображения (mapping) YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | Загруженный пакет; `Preset` — только выбранный пресет. |
| `SessionProfileException(string code, string message)` | `IOException` с `Code` (один из кодов `OL_E_SESSION_PROFILE_*`); сообщение имеет вид `"<code>: <message>"`. |

CLI дополнительно отклоняет флаги командной строки, конфликтующие с профилем (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); эта проверка не входит в компилятор. Полный порядок объединения см. в разделе [профили сеанса](session-profiles.md#precedence-and-override-conflicts).

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### Типы платформы (`OmsiLaunch.Process`)

| Тип | Стабильность | Использование |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` как тип параметра конструктора `OmsiLaunchService` | Определяет платформу, проверяет возможность записи, запускает `Omsi.exe`, наблюдает за ним, завершает его и ожидает его выхода. Передавайте `new CurrentWindowsX64Platform()`; собственная реализация не поддерживается. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | Единственная реализация: хост Windows x64, `CreateProcessW` для `Omsi.exe`, `TerminateProcess` для канонической остановки. Её методы вызывает сервис; интеграторы только создают её экземпляр. Члены (общие с `IRuntimePlatform`): `Detect(root)` возвращает `RuntimePlatformInfo` плана; `ValidateCurrent(info)` выбрасывает `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING`, если хост не может выполнить сеанс; `IsInstallationWritable(root)` лежит в основе `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` создаёт `Omsi.exe` и записывает идентичность процесса (PID, время создания, путь и хеш, вычисленный сервисом; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` и `Terminate` наблюдают за процессом и завершают его. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Публичны в сборке, потому что их совместно используют сервис и тесты. Не являются поверхностью интеграции; `LaunchedProcess` внутренне оборачивает дескрипторы процесса и потока OMSI (они не являются публичными членами) и никогда не возвращается из `IOmsiLaunch`. |

<a id="thread-safety"></a>
## Потокобезопасность

- `OmsiLaunchService` безопасен для параллельных вызовов в разных сеансах: сеансы хранятся в `ConcurrentDictionary`, и каждое изменение отдельного сеанса выполняется под приватной блокировкой этого сеанса.
- Параллельные вызовы в одном сеансе безопасны, но там, где это важно, сериализуются: `ExecuteRuntimeAsync` захватывает гейт сеанса, поэтому вторая команда ждёт первую (её тайм-аут начинается в момент размещения в почтовом ящике).
- Супервизор работает в задаче пула потоков (`Task.Run`) с момента возврата `StartSessionAsync` до перехода сеанса в терминальное состояние; он опрашивает телеметрию и процесс каждые 100 ms. Вызывающие стороны никогда не выполняют код супервизора.
- `StopAsync` и `GetStatusAsync` завершаются синхронно и могут вызываться из любого потока, в том числе внутри обработчика `ProcessExit` (CLI делает это с бюджетом 4 s).
- Ни один вызов API не привязан к потоку; ни один не требует контекста синхронизации.

<a id="what-is-not-in-the-api"></a>
## Чего нет в API

- Нет `IntPtr`, `nint`, дескрипторов Win32, нативных адресов, указателей VMT и объектов процессов. Значения результата, ключи которых начинаются с `internal_` или заканчиваются на `_address`, `_pointer`, `_vmt`, удаляются до того, как результат покинет `ExecuteRuntimeAsync`.
- Нет runtime-операций `internal.*`: `internal.road-vehicles.make-basic` в реестре имеет классификацию `InternalOnly` и возвращает `OL_E_RUNTIME_OPERATION_UNKNOWN` из API и CLI.
- Нет прямого чтения или записи памяти OMSI и нет доступа к файлам установки, кроме того, что объявлено в `LaunchSpec`.
- Нет межпроцессных handles: [локальная плоскость управления](local-control.md) — единственный межпроцессный путь, и она принимает только `session.status`, `session.events`, `session.stop` и `runtime.execute`.
- Нет применения транспортного средства игрока при запуске. Кооперативное завершение, `LAST_MAP_STATE`, дата, время и погода запуска, а также overlays документов клавиатуры и контроллера — `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Сводка стабильности

| Поверхность | Стабильность |
| --- | --- |
| Конструктор `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Транспорт `ExecuteRuntimeAsync`; операции `PublicStableBeta` | `STABLE_BETA` |
| Операции `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| члены спецификации для даты, времени, погоды и ввода, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| члены спецификации транспортного средства игрока | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, реализации `IRuntimePlatform`, все сборки реализации | `INTERNAL` |
| `WorldMode.LastSituation`, операции `internal.*` | `UNAVAILABLE` |
