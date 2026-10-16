# Справочник по CLI

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Перевод [исходной страницы на английском языке](../../../reference/cli.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Эта страница — полный нормативный справочник по командной строке OmsiLaunch `0.1.0-beta4`: три исполняемых файла, грамматика аргументов, порядок диспетчеризации, все командные слова, все иерархические маршруты, все флаги, выходные конверты и поведение каждой команды при ошибках. Она составлена на основе `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` и двух нативных shim-обёрток в `tools\OmsiLaunch.Bootstrapper`. Результаты процесса перечислены в разделе [коды выхода](exit-codes.md), коды ошибок — в разделе [ошибки](errors.md), готовые примеры вызовов — в разделе [примеры CLI](cli-examples.md).

<a id="executables"></a>
## Исполняемые файлы

| Файл | Подсистема | Роль | Отличия |
|---|---|---|---|
| `OmsiLaunch.exe` | Консоль | Нативный загрузчик (`OmsiLaunch.Bootstrapper.cpp`): определяет собственный каталог, разбирает командную строку на токены с помощью `CommandLineToArgvW`, находит `hostfxr` через `nethost.dll` и запускает `OmsiLaunch.Controller.dll` с теми же аргументами. | Вывод в консоль выполняется; код выхода процесса — это код выхода управляемого контроллера либо код shim-обёртки `100`..`106`, если хост .NET не удалось запустить. |
| `OmsiLaunchW.exe` | Windows (GUI) | Та же shim-обёртка (`OmsiLaunch.WindowsHost.cpp`), собранная для подсистемы Windows. Перед запуском контроллера она устанавливает переменную окружения `OMSILAUNCH_WINDOWS_HOST=1`. | Консоли нет: вывод в консоль подавляется, если не указан `--json` (`WindowsHost.SuppressConsole`), ошибки показываются в окнах сообщений (`WindowsHost.ShowFailure`: сообщение, `Code: OL_E_...` и подсказка `See .omsilaunch\diagnostics for details.`), а сбой shim-обёртки `100`..`106` показывается как `OmsiLaunch could not start the .NET host (code N).` Полное описание поведения: [справочник по OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Управляемый (x64, `net6.0-windows`, Windows Forms) | Сам контроллер. Пользователи никогда не вызывают его напрямую; обе shim-обёртки передают путь к контроллеру первым аргументом хоста, поэтому он никогда не появляется в публичном списке аргументов. | Требуется среда выполнения .NET 6 x64 с `Microsoft.WindowsDesktop.App`; см. раздел [установка](../getting-started/installation.md). |

Файл `nethost.dll` должен находиться рядом с shim-обёртками. Сами shim-обёртки аргументы не читают; каждый аргумент доходит до `CliInput.Parse` без изменений, поэтому `OmsiLaunch.exe` и `OmsiLaunchW.exe` принимают в точности одинаковый синтаксис.

<a id="invocation-model"></a>
## Модель вызова

<a id="argument-grammar-cliinputparse"></a>
### Грамматика аргументов (`CliInput.Parse`)

| Форма | Значение |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Флаг. Ключ нечувствителен к регистру; значение — всё, что стоит после первого `:`. Неизвестные ключи приводят к ошибке `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), код выхода `2`. |
| `--key=value` | Runtime-аргумент для выбранной runtime-операции (например, `--handle=rv-000001`). Любой токен `--`, содержащий `=`, является runtime-аргументом и никогда не является флагом. |
| `--json`, `/json` | Структурированный вывод (см. [Форматы вывода](#output-formats)). `--json` — единственный токен `--` без `=`, имеющий смысл; он разбирается как флаг `/json`. |
| слово без префикса | Если командное слово ещё не встречалось и это слово входит в список [командных слов](#command-words), оно становится командой. Если командное слово уже есть, каждое последующее слово без префикса является командным словом (маршрутом). В противном случае первое слово без префикса считается корневым каталогом установки, а каждое последующее слово без префикса добавляется к маршруту. |

Следствия: иерархический маршрут (`time get`) нельзя сочетать с аргументом установки, стоящим после него (`time get D:\OMSI` — это неизвестный маршрут `time get d:\omsi`, код выхода `2`). Вызов `D:\OMSI time get` принимается, но это **режим владельца** (запускается новый сеанс, и операция выполняется в нём один раз). Ошибки разбора (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) и ошибки профиля сеанса (`SessionProfileException`) сообщаются до того, как что-либо будет выполнено, всегда с кодом выхода `2`.

<a id="installation-root"></a>
### Корневой каталог установки

- Явный аргумент установки в виде слова без префикса имеет приоритет над `RootPath` в файле `/spec` (`CliInput.BuildSpecAsync`).
- `.` означает каталог, в котором находится исполняемый файл (`AppContext.BaseDirectory`), и никогда не означает рабочий каталог вызывающей стороны (`CliInput.ResolveInstallationRoot`). На это опирается портативный пакет.
- Если аргумент не указан, операции режима владельца (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) также используют каталог исполняемого файла. Путь нормализуется с помощью `Path.GetFullPath`.
- Команды в режиме клиента никогда не принимают аргумент установки: они обращаются к конечной точке локального управления той установки, в которой находится исполняемый файл (`AppContext.BaseDirectory`). См. раздел [локальное управление](local-control.md).

<a id="owner-and-client"></a>
### Владелец и клиент

- **Владелец**: процесс, который планирует, запускает, контролирует и восстанавливает сеанс (`OwnerSession.RunAsync`). Он удерживает аренду установки (lease) (`Local\OmsiLaunch.Installation.<sha256(root)>`) и транзакцию конфигурации, предоставляет конечную точку локального управления, пока сеанс жив, и показывает [значок в трее](windows-tray.md). На одну установку приходится ровно один владелец: если владелец уже отвечает на `session.status` через конечную точку управления, второй запуск завершается ошибкой `OL_E_SESSION_ALREADY_ACTIVE` (код выхода `7`).
- **Клиент**: любой вызов без аргумента установки, отправляющий `session status`, `session stop`, `events read`, `events watch` или runtime-операцию. Он пересылается через локальный канал управления (pipe); при отсутствии владельца он завершается ошибкой `OL_E_NO_ACTIVE_SESSION` (код выхода `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Порядок диспетчеризации (`CliProgram.RunAsync`)

1. `/silent` (если процесс ещё не выполняется под `OmsiLaunchW.exe`): запустить `OmsiLaunchW.exe` из каталога исполняемого файла через `ShellExecute` (без наследования дескрипторов) с теми же аргументами за вычетом `/silent`/`--silent`, записать конверт `silent` (`delegated`, `host_process_id`) и вернуть `0`. Консольный процесс не ждёт завершения сеанса; см. [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` возвращают `7`.
2. `/version`: конверт `version` с полями `product`, `version` (информационная версия сборки, проставляемая из `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); код выхода `0`.
3. `capabilities`: конверт со всеми дескрипторами `PublicStableBeta` или `PublicExperimental` из канонического каталога `PublicCapabilityRegistry` (с его операциями, нативными возможностями, подтверждениями и примечанием); код выхода `0`.
4. `help [family]`: конверт `help` с полями `usage`, `product_version`, `protocol_version`, `family` и публичными `commands` (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), при необходимости отфильтрованными по семейству; код выхода `0`.
5. `profiles`: конверт с полем `requirement` (`OMSI 2`), отчётом `installation` о целевом корневом каталоге (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), списком `known_layouts` и `predicted_capabilities`, определёнными по исполняемому файлу (авторитетным остаётся определение в процессе при запуске); код выхода `0`.
6. Runtime-операция клиента (без аргумента установки, с маршрутом или `/runtime:`): аргументы проверяются через `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, код выхода `2`), затем `runtime.execute` пересылается с тайм-аутом 8 s (30 s для `road-vehicles.spawn`).
7. Клиентские `session status` (750 ms), `session stop` (привязан к идентификатору активного сеанса, 750 ms), `events read` (750 ms), `events watch` (опрос каждые 250 ms до нажатия Ctrl+C).
8. `detect` или **полное отсутствие аргументов** (нет установки, нет команды, нет `/?`, нет `/spec`, нет флага запуска, нет флага восстановления, нет `/list`): перечислить процессы `Omsi` и опросить конечную точку управления (250 ms); конверт `detect`; код выхода `0`.
9. `/?` или `/help`: вывести текст справки об использовании, код выхода `0`. Любой другой вызов, в котором есть командное слово, но нет маршрута для диспетчеризации (например, только `d3d` или `session status D:\OMSI`), выводит текст справки и завершается с кодом `2`.
10. Режим владельца. Предварительные условия: `plugins\OmsiLaunch.Plugin.opl` и `plugins\OmsiLaunch.Native.x86.dll` должны находиться рядом с исполняемым файлом (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, код выхода `7`). Файл `release-manifest.json` рядом с исполняемым файлом, если он есть, задаёт ожидаемые хеши плагинов.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; конверт `recover` с полями `pending`, `recovered`, `diagnostics`; код выхода `8` только в том случае, если восстановление было запрошено и не завершилось, иначе `0`.
12. `/list:<category>`: `DiscoverAsync`; конверт `content.list`; код выхода `0`. `/list:SessionProfiles` вместо этого использует `ListSessionProfilesAsync`; конверт `session-profiles.list`.
13. Построить `LaunchSpec` (`BuildSpecAsync`), спланировать его (`PlanSessionAsync`), вывести план. `/plan` или `/validate`: код выхода `0`, если `IsRunnable`, иначе `1`. Неисполнимый план никогда не запускает OMSI (код выхода `1`); под `OmsiLaunchW.exe` запуск с неисполнимым планом показывает его последнее диагностическое сообщение `OL_E_` в окне сообщения (аудит документации, BUG-06). При планировании также проверяется установленное замыкание постоянного плагина (набор его файлов) по `release-manifest.json`, поэтому отсутствующий или изменённый плагин делает план неисполнимым (`OL_E_PERMANENT_PLUGIN_*`).
14. Проверить наличие уже работающего владельца (`OL_E_SESSION_ALREADY_ACTIVE`, код выхода `7`), затем `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Жизненный цикл владельца (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. Начиная с этого момента каждый путь завершения доходит до `CloseAsync` в блоке `finally`: исключения, Ctrl+C (`Console.CancelKeyPress`), закрытие консоли / выход из системы (`AppDomain.ProcessExit` с бюджетом 4 s на остановку и восстановление; всё, что не успело выполниться, восстанавливается по журналу при следующем запуске), пункт трея «End session», `session.stop` через pipe и `/observe-seconds`.
2. Значок в трее создаётся, если в спецификации не задан `Presentation.SuppressTrayIcon`.
3. Ожидание состояния `Running` в течение `StartupTimeoutSeconds + 5` секунд. Состояние выводится. Если состояние не `Running`, код выхода `1` (`OmsiLaunchW.exe` показывает `The OMSI session did not reach gameplay.` с последним диагностическим сообщением `OL_E_` или `OL_E_SESSION_START_FAILED`).
4. Выполняются проверочные пакеты (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) и записывают свои артефакты.
5. Запускается конечная точка локального управления.
6. `/runtime:<operation>` выполняется один раз (5 s, 15 s для `road-vehicles.spawn`); результат записывается в `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` и выводится. Сбой runtime-команды никогда не завершает сеанс (вместо этого выводится `runtime_error`).
7. Ожидание: с `/observe-seconds:n` сеанс останавливается через `n` секунд **или** раньше — при остановке через трей или pipe либо при выходе OMSI; без этого флага владелец ждёт, пока OMSI не завершится или не будет запрошена остановка.
8. Выводится итоговое состояние; код выхода `0`, если `Completed`, иначе `1`.

`session.stop`, пункт трея «End session», Ctrl+C и `CloseAsync` запрашивают каноническую остановку: OMSI принудительно завершается с помощью `TerminateProcess` (собственная процедура завершения OMSI не выполняется, и OMSI не перезаписывает `options.cfg`), после чего восстанавливается каждый файл, которым владеет сеанс. См. разделы [жизненный цикл сеанса](../concepts/session-lifecycle.md) и [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Командные слова

Все слова, принимаемые на первой позиции (`CliInput.CommandWordsAccepted`):

| Слово | Назначение | Режим | Примечания |
|---|---|---|---|
| `capabilities` | Список публичных возможностей | Локально, без сеанса | Конверт `capabilities`. |
| `profiles` | Список поддерживаемых вариантов `Omsi.exe` | Локально, без сеанса | Конверт `profiles`. |
| `detect` | Сообщает о процессах `Omsi.exe` и активном владельце | Локально, без сеанса | Также используется по умолчанию, если аргументы не указаны. Состояния: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, для отдельного процесса `UNKNOWN_BINARY_FOUND`, если двоичный файл невозможно проанализировать; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Справка об использовании и каталог публичных команд | Локально, без сеанса | `help <family>` фильтрует по семейству возможностей (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Клиент | Ровно одно последующее слово; всё остальное выводит справку, код выхода `2`. `session plan`/`session start` — это имена маршрутов API, а не слова CLI: используйте `/plan` и `/new`. |
| `events` | `events read`, `events watch` | Клиент | `read` один раз возвращает ограниченный список событий; `watch` каждые 250 ms выводит каждое новое событие (по `Sequence`) в виде конверта `events.watch` до нажатия Ctrl+C (код выхода `0`), `4`, если владелец не отвечает, `7` при ошибке управления. |
| `time` | `time get`, `time set` | Клиентский маршрут | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Клиентский маршрут | |
| `map` | `map get` | Клиентский маршрут | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Клиентский маршрут | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Клиентский маршрут | |
| `player` | `player get` | Клиентский маршрут | |
| `humans` | `humans list`, `humans get`, `humans summary` | Клиентский маршрут | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Клиентский маршрут | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Клиентский маршрут | |
| `constants` | `constants list`, `constants get` | Клиентский маршрут | |
| `curves` | `curves list`, `curves evaluate` | Клиентский маршрут | |
| `hof` | `hof get` | Клиентский маршрут | |
| `drivers` | `drivers list` | Клиентский маршрут | |
| `tickets` | `tickets get` | Клиентский маршрут | |
| `d3d` | Зарезервированное слово семейства | Нет | У `d3d` **нет иерархического маршрута**: `d3d texture ...` — неизвестный маршрут (код выхода `2`), а одно только `d3d` выводит справку (код выхода `2`). Операции D3D вызываются через `/runtime:d3d.status`, `/runtime:d3d.texture.create` и так далее (см. [Операции без маршрута](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Иерархические маршруты

`CliInput.HierarchicalRoutes` сопоставляет маршрут в нижнем регистре с идентификатором runtime-операции. Все маршруты требуют сеанса в состоянии `Running` и выполняются через runtime-почтовый ящик (mailbox) (`ExecuteRuntimeAsync`). Runtime-записи изменяют только состояние OMSI в памяти: они никогда не затрагивают файлы, не входят в транзакцию конфигурации и **не** откатываются при остановке (OMSI принудительно завершается). Стабильность определяется `PublicCapabilityRegistry` и [матрицей проверки](../status/runtime-validation-status.md); подробности и поля результатов приведены в разделе [runtime-управление](runtime-control.md).

| Маршрут | Runtime-операция | Вид | Требует Running | Изменяет OMSI | Участие в восстановлении | Стабильность | Примечания |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Да | Нет | Нет | STABLE_BETA | Поля часов и календаря. |
| `time set` | `time.set` | Write | Да | Да (часы в памяти) | Нет, не откатывается | EXPERIMENTAL | Например, `--minute=<0..59>`; запись, обратное чтение и восстановление проверены 2026-09-20. |
| `weather get` | `weather.read` | Read | Да | Нет | Нет | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Да | Да (погода в памяти) | Нет, не откатывается | EXPERIMENTAL | Например, `--wind_speed=5 --cloud_type="Cumulus 1"`. Ручная погода: обновления живой METAR прекращаются. Проверка в runtime ожидается. |
| `weather metar set` | `weather.metar.set` | Write | Да | Да (погода в памяти; OMSI сохраняет ICAO в `options.cfg`, а сеанс его восстанавливает) | Нет, не откатывается | EXPERIMENTAL | `--icao=<ICAO>`. Живая погода METAR, как её задаёт диалог погоды OMSI. Проверка в runtime ожидается. |
| `weather actual get` | `weather.actual.read` | Read | Да | Нет | Нет | EXPERIMENTAL | Состояние контроллера фактической погоды / ICAO. |
| `map get` | `map.read` | Read | Да | Нет | Нет | STABLE_BETA | Имя карты, файл, описание, количество тайлов, диапазон лет и сторона движения; повторно проверено в runtime на исправленном слоте карты. |
| `camera get` | `camera.read` | Read | Да | Нет | Нет | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Да | Да (скалярные параметры камеры, например `--field_of_view=`) | Нет, не откатывается | EXPERIMENTAL | Запись и обратное чтение FOV проверены. |
| `camera lock` | `camera.lock` | Action | Да | Да (политика в пределах сеанса) | Нет | EXPERIMENTAL | Требует `--family=<0..3>` (водитель=0, пассажир=1, внешняя=2, карта=3), необязательно `--preset=<n>` (семейство 0 или 1). Нужно транспортное средство игрока (например, сохранённая ситуация). Проверено в runtime в итоговом наборе runtime-проверок (`CAM01`); строка `RuntimeValidation` в реестре по-прежнему содержит `STATICALLY_VALIDATED` (см. [возможности](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Да | Да | Нет | EXPERIMENTAL | Снимает политику, установленную `camera lock` (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Да | Нет | Нет | STABLE_BETA | Возвращает handle вида `rv-NNNNNN`, действующие в пределах сеанса. |
| `vehicles get` | `road-vehicle.read` | Read | Да | Нет | Нет | STABLE_BETA | Требует `--handle=`. Устаревший handle: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Да | Нет | Нет | STABLE_BETA | Количества и состояние игрока, без handle. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Да | Да (добавляет один RoadVehicle) | Нет, не удаляется | EXPERIMENTAL | Требует `--model=Vehicles\...\*.bus`. Тайм-аут клиента 30 s, тайм-аут владельца 15 s. Не назначает транспортное средство игрока. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Да | Да | Нет | EXPERIMENTAL | Профилированный `PlaceRandomBus`. |
| `player get` | `player-vehicle.read` | Read | Да | Нет | Нет | STABLE_BETA | Семантический null, если транспортного средства игрока нет. |
| `humans list` | `humans.list` | Read | Да | Нет | Нет | EXPERIMENTAL | Возвращает handle вида `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Да | Нет | Нет | EXPERIMENTAL | Требует `--handle=`. |
| `humans summary` | `humans.read` | Read | Да | Нет | Нет | EXPERIMENTAL | Только количества. |
| `timetable get` | `timetable.read` | Read | Да | Нет | Нет | STABLE_BETA | Состояние менеджера расписания. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Да | Нет | Нет | STABLE_BETA | Часть возможности `timetable.read`; подтверждение пакетным чтением 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable lines list` | `timetable.lines.list` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable tours list` | `timetable.tours.list` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable logs list` | `timetable.logs.read` | Read | Да | Нет | Нет | STABLE_BETA | То же. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Да | Нет | Нет | EXPERIMENTAL | Номера ТС, которые OMSI не отдаёт свободному ИИ (зарезервированы рейсами расписания или фиксированными списками ИИ). Постраничный список (`cursor`, `limit`, не более 256 на страницу). |
| `map tiles list` | `map.tiles.list` | Read | Да | Нет | Нет | EXPERIMENTAL | Тайлы карты: позиция в сетке, собственный индекс, файл и состояние загрузки, количество объектов, путей и сплайнов. Постраничный список (`cursor`, `limit`, не более 256 на страницу). |
| `drivers list` | `drivers.read` | Read | Да | Нет | Нет | EXPERIMENTAL | Записи водителей. |
| `tickets get` | `tickets.read` | Read | Да | Нет | Нет | EXPERIMENTAL | Записи наборов билетов. |
| `hof get` | `vehicle.hofs.read` | Read | Да | Нет | Нет | STABLE_BETA | Требует `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Да | Нет | Нет | STABLE_BETA | Требует `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Да | Нет | Нет | STABLE_BETA | Требует `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Да | Нет | Нет | STABLE_BETA | Требует `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Да | Нет | Нет | STABLE_BETA | Требует `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Да | Нет | Нет | EXPERIMENTAL | Требует `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Да | Нет | Нет | EXPERIMENTAL | Требует `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Да | Да (переменная скрипта) | Нет, не откатывается | EXPERIMENTAL | Требует `--handle=`, `--name=`, `--value=` (конечное число). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Да | Нет | Нет | EXPERIMENTAL | Требует `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Да | Нет | Нет | EXPERIMENTAL | Требует `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Да | Да (состояние скрипта в памяти) | Нет, не откатывается | EXPERIMENTAL | Требует `--handle=`, `--name=`, `--value=`. Собственный `SetStringVariable` OMSI. Проверка в runtime ожидается. |
| `scripts trigger` | `vehicle.trigger` | Action | Да | Да (выполняет код скрипта ТС) | Нет, не откатывается | EXPERIMENTAL | Требует `--handle=`, `--name=`, `--value=` (`1` нажатие, `0` отпускание, `2` перетаскивание). Проверка в runtime ожидается. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Да | Да (воспроизводит звуки ТС) | Нет, не откатывается | EXPERIMENTAL | Требует `--handle=`, `--trigger=`; необязателен `--file=`. Проверка в runtime ожидается. |

<a id="operations-without-a-route"></a>
### Операции без маршрута

У следующих публичных идентификаторов операций (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) нет иерархического маршрута; они вызываются через `/runtime:<operation>` с `--key=value` или `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (обязательны `width`, `height`, `format`; `levels` необязателен), `d3d.texture.describe` (`handle`; `level` необязателен), `d3d.texture.update` (обязательны `handle`, `width`, `height`, `pixels_base64`; `level`, `x`, `y` необязательны), `d3d.texture.release` (`handle`). Операции D3D имеют статус EXPERIMENTAL; жизненный цикл текстур и инвалидация при сбросе устройства (device reset) проверены в runtime (итоговый набор runtime-проверок `H02`, `D01`; см. [возможности](capabilities.md)). `timetable.track-entries.list` и `timetable.tour-entries.list` — ограниченные списки: результат, который не помещается в runtime-слот, сокращается (`truncated=true`). `internal.road-vehicles.make-basic` имеет статус INTERNAL и отклоняется с `OL_E_RUNTIME_OPERATION_UNKNOWN` как CLI, так и API.

<a id="flags"></a>
## Флаги

Все флаги из `CliInput.KnownFlags`. «Фаза» — это *launch-time* (формирует `LaunchSpec`/план нового сеанса), *runtime* (действует на работающий сеанс) или *control* (меняет поведение самого CLI). Флаги, которые разбираются только для совместимости (`CliInput.AcceptedNoEffectFlags`), отмечены в своей строке.

<a id="control-and-output"></a>
### Управление и вывод

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/?` | `/?` | выкл. | control | STABLE_BETA | Выводит текст справки, код выхода `0`. |
| `/help` | `/help` | выкл. | control | STABLE_BETA | То же, что `/?`. (Слово без префикса `help` вместо этого возвращает структурированный каталог.) |
| `/version` | `/version` | выкл. | control | STABLE_BETA | Конверт `version`, код выхода `0`. Обрабатывается раньше всех остальных команд, кроме `/silent`. |
| `/json` | `/json` или `--json` | выкл. | control | STABLE_BETA | Выводит JSON-конверты (envelope); также принудительно включает вывод в консоль даже под `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | выкл. | control | STABLE_BETA | Подавляет вывод об успехе, предназначенный для человека. Вывод JSON (`/json`) и ошибки по-прежнему записываются; код выхода не меняется. |
| `/silent` | `/silent` (также `--silent`) | выкл. | control | EXPERIMENTAL | Передаёт всю командную строку в `OmsiLaunchW.exe` и возвращает `0`, как только процесс хоста запущен. Результат сеанса сообщают `OmsiLaunchW.exe` (окна сообщений, значок в трее), `.omsilaunch\diagnostics` и конечная точка локального управления. Делегирование и диалоги ошибок проверены в runtime (итоговый набор runtime-проверок `T04`); см. [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | control | REMOVED | Удалён в 0.1.0-beta4: отклоняется с `OL_E_INVALID_ARGUMENT` (код выхода `2`). Владелец сеанса всегда обслуживает конечную точку локального управления. |
| `/verbose` | `/verbose` | выкл. | launch-time | PARTIAL | `DiagnosticsSpec.Verbose`. Значения переносятся в спецификацию; их действие ограничено трассировкой хоста в `.omsilaunch\diagnostics`. |
| `/log` | `/log` | вкл. (`DiagnosticsSpec.Log` по умолчанию `true`) | launch-time | PARTIAL | `DiagnosticsSpec.Log`. Фактически всегда включён. |
| `/logall` | `/logall` | выкл. | launch-time | PARTIAL | Одновременно устанавливает `Verbose`, `ProcessTrace`, `PluginTrace` и `NativeTrace`. |
| `/omsi-logall` | `/omsi-logall` | выкл. | launch-time | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | выкл. | launch-time | PARTIAL | Псевдоним `/trace-process`. |
| `/trace-process` | `/trace-process` | выкл. | launch-time | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | выкл. | launch-time | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: плагин сообщает каждую runtime-операцию (`plugin.trace.operation`: операция, исход, код, длительность) в трассировку хоста сеанса. |
| `/trace-native` | `/trace-native` | выкл. | launch-time | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: нативный слой сообщает каждую установку и снятие хука (`native.trace`) в трассировку хоста сеанса. |

<a id="planning-validation-and-harnesses"></a>
### Планирование, проверка и тестовые обвязки

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/plan` | `/plan` | выкл. | launch-time | STABLE_BETA | Строит и выводит `SessionPlan`, не запуская OMSI. Код выхода `0`, если `IsRunnable`, иначе `1`. Требует выбора запуска (`/new`, `/saved`, `/spec` или аргумента установки); одиночный `/plan` без всего остального выполняет `detect`. |
| `/validate` | `/validate` | выкл. | launch-time | STABLE_BETA | В этой сборке идентичен `/plan`. |
| `/runtime-batch` | `/runtime-batch` | выкл. | runtime (владелец) | INTERNAL | Проверочная обвязка: после `Running` выполняет набор операций чтения и записывает `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | выкл. | runtime (владелец) | INTERNAL | Проверочная обвязка: чтения плюс `time.set`, `camera.set` и `vehicle.variable.set` с восстановлением; записывает `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | выкл. | runtime (владелец) | INTERNAL | Проверочная обвязка для жизненного цикла текстур D3D; записывает `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | нет | runtime | STABLE_BETA (диспетчеризация) | Выбирает публичную runtime-операцию по идентификатору. Режим клиента (без аргумента установки): пересылается владельцу. Режим владельца: выполняется один раз после `Running`. Неизвестные идентификаторы: `OL_E_RUNTIME_OPERATION_UNKNOWN`, код выхода `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (можно повторять) | нет | runtime | STABLE_BETA (диспетчеризация) | Runtime-аргумент; эквивалентен `--key=value`. Нет `=`: `/runtime-arg requires key=value`, код выхода `2`. |

<a id="world-selection"></a>
### Выбор мира

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` — режим по умолчанию, но запуск запрашивается, только если присутствует один из флагов `/new`, `/saved`, `/last`, `/spec` | launch-time | STABLE_BETA | NEW_MAP. Требует `/map` и `/entrypoint-index` (план без индекса точки входа в представленном списке сообщает `OL_E_ENTRYPOINT_REQUIRED`; без `/map` карта не определяется). `/new` никогда не выбирает карту неявно. |
| `/saved` | `/saved:<file.osn>` | нет | launch-time | STABLE_BETA | SAVED_SITUATION. Карта и позиция берутся из `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` вместе с `/saved` отклоняются (код выхода `2`). Сохранённая ситуация не найдена: `OL_E_SITUATION_NOT_FOUND`; не найдена её карта: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | нет | launch-time | EXPERIMENTAL | LAST_MAP_STATE: загружает собственный `laststn.osn` карты (его записывает OMSI при закрытии карты) через ветку последнего состояния формы Start. Требует `/map`; карта без `laststn.osn` даёт `OL_E_SITUATION_NOT_FOUND`. Никогда не выбирается самый новый `.osn` по эвристике. Проверка в runtime ожидается. |
| `/map` | `/map:<identity>` (например, `maps\Grundorf\global.cfg`) | нет | launch-time | STABLE_BETA | Идентификатор карты для `/new` или область поиска для `/list:Entrypoints`. Неизвестная карта: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | нет | launch-time | EXPERIMENTAL | Точка входа по точной метке (с учётом регистра) или по идентификатору из `/list:entrypoints`. OMSI показывает только первую запись для каждой метки; более поздняя запись с той же меткой отклоняется (`OL_E_ENTRYPOINT_NOT_PRESENTED`), как и неизвестная (`OL_E_ENTRYPOINT_NOT_FOUND`). Проверка в runtime ожидается. Взаимоисключается с `/entrypoint-index` (идентификатор имеет приоритет и сбрасывает индекс). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | нет | launch-time | STABLE_BETA | Индекс точки входа в представленном списке (нумерация с 1, как её показывает OMSI). Обязателен для исполнимого плана NEW_MAP. |

<a id="date-time-and-weather"></a>
### Дата, время и погода

Эти флаги принимаются и переносятся в `LaunchSpec`. Дата, время и год запуска применяются только к сеансам NEW_MAP: при `/saved` или `/last` планировщик записывает их как `UNAVAILABLE` **и добавляет `OL_E_CAPABILITY_UNAVAILABLE`, поэтому план НЕИСПОЛНИМ (код выхода `1`)**, так как сохранённая ситуация хранит собственные часы. Погода применяется в любом режиме мира. Все они `EXPERIMENTAL`: нативный путь запуска закрыт статически, проверка в runtime ожидается. Файл `/spec` или профиль сеанса, задающие эти значения, дают тот же результат.

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` или `/date:system` | не задано | launch-time | EXPERIMENTAL | `DateSpec` явный/системный: дата начала сеанса NEW_MAP (см. [launchspec](launchspec.md#datespec-timespec-yearspec)). Значение, которое не удаётся разобрать: `OL_E_INVALID_ARGUMENT`, код выхода `2`; невозможная дата: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` или `/time:system` | не задано | launch-time | EXPERIMENTAL | `TimeSpec` явный/системный: время начала сеанса NEW_MAP. |
| `/year` | `/year:<n>` или `/year:system` | не задано | launch-time | EXPERIMENTAL | `YearSpec`: год начала сеанса NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | не задано | launch-time | EXPERIMENTAL | `WeatherMode.Preset`: файл из каталога `weather` установки, по имени (`Regen`) или по пути (`weather\Regen.owt`); отсутствующий файл даёт `OL_E_WEATHER_INVALID` при планировании. Загружается собственным `loadWeather` OMSI сразу после запуска мира; живая METAR остаётся выключенной. Проверка в runtime ожидается. |
| `/weather-icao` | `/weather-icao:<code>` | не задано | launch-time | EXPERIMENTAL | `WeatherMode.Icao`: живая погода METAR для станции ICAO из четырёх символов (иначе `OL_E_WEATHER_INVALID`). OMSI сохраняет станцию в `options.cfg`, а сеанс его восстанавливает. Проверка в runtime ожидается. |
| `/weather-real` | `/weather-real` | не задано | launch-time | EXPERIMENTAL | `WeatherMode.RealCurrent`: живая погода METAR для станции, уже настроенной в OMSI; без неё запуск завершается с `OL_E_WEATHER_APPLY_FAILED`. Действует последний из `/weather`, `/weather-icao`, `/weather-real`. Любая погода запуска переопределяет погоду сохранённой ситуации. |

<a id="player-vehicle"></a>
### Транспортное средство игрока

Принимаются и сопоставляются с установкой, но не применяются runtime: каждое заданное поле получает `UNAVAILABLE` и добавляет `OL_E_CAPABILITY_UNAVAILABLE` (план НЕИСПОЛНИМ, код выхода `1`).

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | не задано | launch-time | UNAVAILABLE | Определяется первым (`OL_E_VEHICLE_NOT_FOUND`, если неизвестно). |
| `/repaint` | `/repaint:<id>` | не задано | launch-time | UNAVAILABLE | Определяется только вместе с `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | не задано | launch-time | UNAVAILABLE | `OL_E_HOF_NOT_FOUND`, если неизвестно. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | нет | launch-time | STABLE_BETA | Одна операция [управления HOF](hof-management.md) сеанса: `HofSourceKind` и `Source`. Требует хотя бы один `/hof-target`. Заменяет `HofManagement` из `/spec`; при пресете профиля сеанса с `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Неверный формат: код выхода `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (можно повторять) | нет | launch-time | STABLE_BETA | Целевое ТС ниже `Vehicles\`. Без `/hof-source`: код выхода `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (можно повторять) | все файлы | launch-time | STABLE_BETA | Ограничивает источник этими именами файлов. Без `/hof-source`: код выхода `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | launch-time | STABLE_BETA | `HofApplyMode`. Без `/hof-source`: код выхода `2`. |
| `/module` | `/module:<id>` (можно повторять) | нет | launch-time | EXPERIMENTAL | Запрашивает [runtime-модуль](runtime-modules.md) для сеанса (`LaunchSpec.Modules.Enable`, добавляется к модулям файла `/spec`). Запрошенный модуль, который не удаётся загрузить, делает план неисполнимым с кодом `OL_E_MODULE_*` (код выхода `1`). Его операции вызываются через `/runtime:module.<id>.<operation>` и `/runtime-arg`. При пресете профиля сеанса, задающем для него `enabled: false`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (можно повторять) | нет | launch-time | EXPERIMENTAL | Не допускает модуль в сеанс, включая модуль, загружаемый по умолчанию, по его карте или по файлу параметров (`LaunchSpec.Modules.Disable`). При пресете, задающем для него `enabled: true`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Без значения: код выхода `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (можно повторять) | нет | launch-time | EXPERIMENTAL | Задаёт параметр модуля (`LaunchSpec.Modules.Settings`); имеет приоритет над файлом `/spec`, файлом параметров и значением по умолчанию. Ключ, который задаёт и выбранный пресет: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Не в формате `<id>.<key>=<value>`: код выхода `2`. Неверное значение или необъявленный ключ: диагностическое сообщение плана `OL_E_MODULE_SETTING_INVALID` (код выхода `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` без значения означает `true` для булева параметра | нет | launch-time | EXPERIMENTAL | Флаг, который установленный модуль объявляет для одного из своих параметров (`flag` в его манифесте; перечисляются через `/list:RuntimeModules`). Разрешается при планировании сеанса и равносилен `/module-setting:<id>.<key>=<value>`. Флаг, который не объявляет ни один установленный модуль, или флаг вне запуска: `Unknown argument`, код выхода `2`. Флаг, объявленный двумя модулями: `OL_E_MODULE_SETTING_INVALID`, код выхода `2`. Флаг модуля никогда не заменяет встроенный флаг. |
| `/fleet` | `/fleet:<n>` | не задано | launch-time | UNAVAILABLE | Бортовой номер. |
| `/registration` | `/registration:<text>` | не задано | launch-time | UNAVAILABLE | Регистрационный номер. |
| `/no-vehicle` | `/no-vehicle` | выкл. | launch-time | STABLE_BETA | Удаляет транспортное средство игрока из исходных данных (`/spec` или профиль). Безвреден. |

<a id="configuration-overlays"></a>
### Overlay конфигурации

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (можно повторять; ключи нечувствительны к регистру) | нет | launch-time | STABLE_BETA | Семантический overlay (временная замена файла на время сеанса) для `options.cfg` из `ConfigurationCatalog` (например, `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Неизвестный ключ: `OL_E_UNKNOWN_SETTING` (код выхода `2`); ключ только для чтения (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (код выхода `2`); значение вне диапазона или с неверным форматом: `OL_E_INVALID_SETTING_VALUE` при построении overlay. Overlay — это изменение в рамках сеанса: для него создаётся снимок (snapshot), он применяется до запуска OMSI и побайтово восстанавливается при остановке (RV-005 `RUNTIME_VALIDATED`). Конфликт с ключом, которым владеет пресет выбранного профиля: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Отображение заставки

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (без учёта регистра) | `Managed` | launch-time | STABLE_BETA | `Managed`: входящие в пакет 24-битные BMP размером 640x480 один раз копируются в `<root>\.omsilaunch\assets\splash`, а `GUI\NewSplashscreen_ENG.bmp` и `GUI\NewSplashscreen_<lang>.bmp` подменяются через overlay в рамках транзакции и точно восстанавливаются (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (псевдонимы): файлы OMSI не затрагиваются. Значение не указано: `/splash requires Unset, Native, or Managed`, код выхода `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (также `pt-BR`, `de`, `fr`, `en`; любое другое значение заменяется на `ENG`) | `[language]` из `options.cfg`, иначе `ENG` | launch-time | STABLE_BETA | Выбирает локализованный целевой файл. |
| `/splash-assets` | `/splash-assets:<directory>` (относительные пути разрешаются внутри корневого каталога установки) | `<root>\.omsilaunch\assets\splash`, иначе набор из пакета | launch-time | STABLE_BETA | Пользовательский каталог ресурсов; должен содержать `ENG.bmp`, а для неанглийского языка — `<lang>.bmp`. Ошибки: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (в плане выводятся как `OL_E_SESSION_PRESENTATION_INVALID`; план неисполним). |

<a id="internet-textures"></a>
### Интернет-текстуры

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | launch-time | EXPERIMENTAL | `Native`: без изменений. `Disabled`: профилированный внутрипроцессный загрузчик подавляется. `Override`: указанный профиль `.itx` подменяется через overlay как `Texture\standard.itx`; каждая цель HTTP(S), указанная в нём, а также `Texture\standard.ipr` становятся удалениями в рамках сеанса (удаляются на время сеанса и восстанавливаются при остановке). Значение не указано: код выхода `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | нет | launch-time | EXPERIMENTAL | Обязателен с `Override` (`OL_E_ITX_PROFILE_REQUIRED`, код выхода `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (должны быть пары строк URL/цель с URL `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (цели должны разрешаться внутри `Texture\` без абсолютных путей, `..` и точек повторного анализа). |

<a id="session-profiles"></a>
### Профили сеанса

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | нет | launch-time | STABLE_BETA (компиляция; офлайн-тесты `OmsiLaunch.ProfileTests`) | Загружает `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (см. [профили сеанса](session-profiles.md)). Требует `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, код выхода `2`). Блок `new:` применяется только с `/new`; `compatibility.maps` применяется для `/new` и `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Явные флаги, конфликтующие с полем, которым владеет профиль, отклоняются с `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): карта/точка входа/дата/время/год/погода, если ими владеет блок `new:`; ключи `/set`, которыми владеет пресет; флаги заставки, если у пресета есть `presentation`; флаги интернет-текстур, если у него есть `internet-textures`; тайм-ауты, если у него есть `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | нет | launch-time | STABLE_BETA | Выбирает пресет по `index`. Вне диапазона: код выхода `2`. |

<a id="launchspec-file"></a>
### Файл LaunchSpec

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | нет | launch-time | STABLE_BETA (загрузчик проверен офлайн-тестами; семантика сеанса идентична флагам) | Загружает JSON-файл `LaunchSpec` в качестве исходных данных (см. [LaunchSpec](launchspec.md)) и отмечает, что запрошен запуск. Правила (`LaunchSpecJson`): файл должен существовать (`OL_E_SPEC_NOT_FOUND`, код выхода `6`); не более 1 MiB (`OL_E_SPEC_TOO_LARGE`, код выхода `2`); корень должен быть объектом (`OL_E_SPEC_INVALID`); имена свойств нечувствительны к регистру; комментарии `//` и завершающие запятые допускаются; глубина не более 32; каждое неизвестное свойство отклоняется с указанием его JSON-пути (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, код выхода `2`). |

**Приоритет** (`CliInput.BuildSpecAsync`): значения по умолчанию → файл `/spec` → `/predefined-profile` (заменяет `Installation` и `World`, затем применяет профиль) → явные флаги. Явный аргумент установки имеет приоритет над `RootPath` в спецификации. `/no-vehicle` удаляет транспортное средство игрока из спецификации; `/vehicle` и родственные флаги объединяются с ним поле за полем. Ключи `/set` объединяются с `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` переопределяют значения, только если указаны. `/startup-timeout` и `/shutdown-timeout` переопределяют значения, только если указаны; `Presentation.SuppressTrayIcon` берётся только из спецификации (флага нет). Флаги диагностики объединяются по ИЛИ с `Diagnostics` из спецификации.

<a id="content-discovery"></a>
### Обнаружение контента

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; категории — значения `ContentQueryKind` `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (без учёта регистра) | нет | локально, без сеанса | STABLE_BETA | `DiscoverAsync` по установке; конверт `content.list` с элементами `Identity`, `Kind`, `DisplayName`; код выхода `0`. `SessionProfiles` отвечает через `ListSessionProfilesAsync` (конверт `session-profiles.list`), а `RuntimeModules` — через `ListRuntimeModulesAsync` (конверт `runtime-modules.list`: идентификатор, версия, активация, операции и корректность каждого модуля, а также его параметры с типом, значением по умолчанию и флагом). Неизвестная категория: `Unknown discovery category`, код выхода `2`. Точки повторного анализа (junction/символические ссылки) пропускаются, файлы OMSI читаются в кодировке Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | нет | локально | STABLE_BETA | Область поиска, передаваемая для всех категорий, кроме `Entrypoints`, которая использует в качестве области `/map`. |

<a id="timeouts-and-observation"></a>
### Тайм-ауты и наблюдение

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` секунд | значение из спецификации/профиля, иначе `180` | launch-time | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. Владелец ждёт `Running` в течение этого значения плюс 5 s; `OL_E_STARTUP_TIMEOUT` завершает сеанс с кодом выхода `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` секунд | значение из спецификации/профиля, иначе `30` | launch-time | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: при `/cooperative-shutdown` — сколько супервизор ждёт, пока OMSI завершится сама, прежде чем принудительно её завершить; без этого флага OMSI завершается сразу, и значение не действует. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | выкл. | launch-time | EXPERIMENTAL | `Behavior.CooperativeShutdown`: завершение работающего сеанса просит OMSI выйти через её собственную процедуру выхода (ту, что выполняется после подтверждения выхода пользователем: она сохраняет `options.cfg` и `laststn.osn` карты, затем закрывается) и ждёт `ShutdownTimeoutSeconds`, прежде чем принудительно завершить процесс. Включается явно, пока не проверено в runtime; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`, когда OMSI пришлось завершить принудительно. Файлы сеанса восстанавливаются после выхода в любом случае. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | нет (работа до выхода OMSI или запроса остановки) | runtime (владелец) | STABLE_BETA | Верхняя граница фазы работы: через `n` секунд в состоянии `Running` запрашивается каноническая остановка. Остановка через трей или pipe либо выход OMSI завершают её раньше. `0` останавливает сразу после `Running`. |

<a id="recovery"></a>
### Восстановление после сбоя

| Флаг | Синтаксис и значения | По умолчанию | Фаза | Стабильность | Поведение |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | выкл. | локально | STABLE_BETA | Сообщает, является ли `<root>\.omsilaunch\journal.json` незавершённым (`pending`), никогда ничего не восстанавливает; код выхода `0`. Берёт аренду установки: `OL_E_INSTALLATION_BUSY` (код выхода `7`), пока её удерживает владелец. |
| `/recover` | `/recover` | выкл. | локально | STABLE_BETA | Восстанавливает незавершённый журнал (резервные копии сначала сверяются с SHA-256 снимка; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` сообщаются в `diagnostics`). Код выхода `0`, если незавершённых транзакций не было или восстановление завершилось; `8`, если журнал был незавершённым и таким остался. Отклоняется с `OL_E_INSTALLATION_BUSY`, пока жив записанный в журнал процесс OMSI (PID, время создания, путь к exe) или — для журнала после `HandoffCreated` без PID — любой `Omsi.exe` из этого корневого каталога. Каждый запуск сеанса автоматически выполняет такое же восстановление перед чтением установки. |
| `/clear-module-data` | `/clear-module-data:<module id>` | нет | локально, без сеанса | EXPERIMENTAL | Удаляет каталог данных модуля `<root>\.omsilaunch\module-data\<id>` и больше ничего; конверт `module-data.clear` с полями `module` и `cleared` (`false`, если удалять было нечего); код выхода `0`. Захватывает аренду установки (lease): `OL_E_INSTALLATION_BUSY` (код выхода `7`), пока сеанс владеет установкой. Каталог данных модуля, достигнутый через ссылку, отклоняется (`OL_E_MODULE_INTEGRITY`); ссылка внутри него удаляется как ссылка. |

<a id="output-formats"></a>
## Форматы вывода

- **Конверт успеха** (`CliInput.WriteEnvelope`, с `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, с отступами. Пересланные ответы `session status` и `events read` добавляют член `metadata`, если более старые события были опущены, чтобы уместиться во фрейм управления (`events_dropped_count`, см. [локальное управление](local-control.md)). Без `--json` выводится только `<object>` в виде JSON с отступами, а после него — `Note: <n> older events were omitted to fit the control frame.`, если события были отброшены.
- **Конверт ошибки** (`CliInput.WriteError`, с `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Без `--json`: `OL_E_<CODE>: message` в одной строке. Категории: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Под `OmsiLaunchW.exe` тот же код и сообщение показываются в окне сообщения.
- **План и состояние** (`CliInput.Write`): записи `SessionPlan`, `SessionStatus` и `RuntimeCommandResult` выводятся как JSON с отступами **без** конверта. Без `--json` план выводится кратко как `Plan: READY profile=Omsi23004_692EBFBF` или `Plan: NOT RUNNABLE profile=...`; остальные записи по-прежнему выводятся как JSON. Значения перечислений сериализуются как целые числа (`SessionState.Running` — `14`, `Completed` — `18`, `Failed` — `19`).
- Имена команд, используемые в конвертах: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile`, а также идентификатор runtime-операции для пересланных runtime-команд.
- Под `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) в консоль ничего не выводится, если не указан `--json`.

<a id="errors-per-command"></a>
## Ошибки по командам

| Команда | Типичные коды ошибок | Код выхода |
|---|---|---|
| Любая ошибка разбора | `OL_E_INVALID_ARGUMENT`, коды профиля сеанса (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Клиентский маршрут, `/runtime` (клиент) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, возвращённые владельцем, например `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Предварительные проверки владельца | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); незавершённый журнал, который не удалось восстановить (`8`) | `7`, `8` |
| `/list` | неизвестная категория (`2`); `OL_E_INSTALLATION_NOT_FOUND`/отсутствующие каталоги (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, запуск | диагностические сообщения плана: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (установленное замыкание плагина), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (информационное) | `1` |
| Запуск сеанса | `OL_E_PLAN_NOT_RUNNABLE` (повторное планирование при запуске, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (обычно сообщаются при планировании как диагностическое сообщение плана, код выхода `1`; `7` — только если файлы плагина изменились между планированием и запуском), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (сеанс `Failed`, `1`) | `1`, `7` |
| Необработанное исключение в любом месте | классифицируется `CliProgram.Classify` (см. [коды выхода](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Окружение

| Переменная | Кем устанавливается | Действие |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: вывод в консоль подавлен, ошибки показываются в окнах сообщений, `/silent` повторно не делегируется. |

<a id="see-also"></a>
## См. также

[Примеры CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [коды выхода](exit-codes.md) · [ошибки](errors.md) · [локальное управление](local-control.md) · [трей Windows](windows-tray.md) · [runtime-управление](runtime-control.md) · [возможности](capabilities.md) · [LaunchSpec](launchspec.md) · [профили сеанса](session-profiles.md) · [упаковка](packaging.md) · [совместимость](compatibility.md) · [известные ограничения](known-limitations.md) · [публичный API](public-api.md)
