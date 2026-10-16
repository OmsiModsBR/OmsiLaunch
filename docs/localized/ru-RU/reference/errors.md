# Коды ошибок и диагностики

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> Перевод [исходной страницы на английском языке](../../../reference/errors.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Эта страница — нормативный справочник по всем кодам из `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 кодов ошибок `OL_E_` и 4 предупреждения `OL_W_`, сгруппированные по категориям каталога, а также информационные диагностические коды, которые не являются ошибками. Для каждого кода указано, где его выдаёт текущий код, что он означает, как он доходит до вас (выброшенное исключение, поле результата, диагностическое сообщение, ответ плоскости управления или CLI-конверт) и что делать. Значения взяты из мест, где коды выдаются; если код определён, но в текущем коде нет пути, по которому он выдаётся, это указано явно.

Связанные страницы: [публичный API](public-api.md), [коды выхода](exit-codes.md), [LaunchSpec](launchspec.md), [жизненный цикл сеанса](../concepts/session-lifecycle.md), [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md), [локальная плоскость управления](local-control.md), [runtime-управление](runtime-control.md), [профили сеанса](session-profiles.md), [постоянный плагин](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Как коды доходят до вас

| Канал | Значение |
| --- | --- |
| Исключение | Исключение, `Message` которого начинается с кода (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). CLI извлекает код из сообщения и сопоставляет его с кодом выхода (`CliProgram.Classify`). |
| Диагностическое сообщение плана | `LaunchDiagnostic` в `SessionPlan.Diagnostics`; любой код `OL_E_` делает `IsRunnable` равным false (CLI: план `NOT RUNNABLE`, код выхода 1). |
| Диагностическое сообщение сеанса | `LaunchDiagnostic` в `SessionStatus.Diagnostics`; состояние сеанса — `Failed` (код выхода CLI 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` и `OL_E_RESTORE_FAILED` оборачивают в своём сообщении внутренний код. |
| Runtime-результат | `RuntimeCommandResult.ErrorCode` при `Succeeded = false`. |
| Runtime-деталь | `RuntimeCommandResult.ErrorCode` — конкретный код, а `Values["detail"]` — сообщение (вместе с `Values["exception"]`, типом .NET). Beta 4: типизированный код на стороне плагина доходит до API и CLI без изменений; только ошибка без кода из каталога становится `OL_E_RUNTIME_OPERATION_FAILED`. |
| Ответ управления | `ErrorCode` ответа локальной плоскости управления (`LocalControlResponse`). |
| CLI-конверт | `error.code` в JSON-конверте (envelope) `--json` или `<code>: <message>` в консоли; указан код выхода. |
| Телеметрия | Имя runtime-события, которое хост сопоставляет с диагностическим сообщением сеанса. |

## Cli

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Не было перехвачено исключение `OperationCanceledException` (Ctrl+C или отменённое ожидание клиента). | CLI-конверт, код выхода 7 | Повторите команду. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Не было перехвачено исключение без кода `OL_E_`: некорректный JSON в `/spec`, null в обязательном члене спецификации, непредвиденный сбой. | CLI-конверт, код выхода 10 | Прочитайте сообщение и `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; исправьте входные данные; если причина неясна, сообщите о проблеме. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Не было перехвачено исключение `TimeoutException` без кода; либо клиент локального управления подключился к владельцу, который не ответил в пределах тайм-аута (владелец существует, поэтому это не сообщается как `OL_E_NO_ACTIVE_SESSION`). (Тайм-ауты почтового ящика (mailbox) вместо этого несут `OL_E_RUNTIME_REQUEST_TIMEOUT`.) | CLI-конверт, ответ управления; код выхода 5 из `Classify`, код выхода 7 для ответа управления | Повторите попытку; проверьте, что OMSI и владелец отвечают. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` отсутствует рядом с `OmsiLaunch.exe`. | CLI-конверт, код выхода 7 | Переустановите пакет. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` для `OmsiLaunchW.exe` не вернул процесс. | CLI-конверт, код выхода 7 | Проверьте файлы пакета и права доступа; запустите без `/silent`, чтобы увидеть ошибку. |

## Compatibility

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` по `plugin.build.invalid` | Возможности запуска определены, но живая форма Start или одна из точек её хуков не в ожидаемом состоянии, когда плагин подготавливает headless-запуск (например, её уже изменил другой плагин). | Диагностическое сообщение сеанса (`Failed`) | Удалите конфликтующий плагин; см. [совместимость](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Плагин (`plugin.capability.unresolved` при запуске; runtime-операции) | Нативную возможность, которая нужна запросу, не удалось определить и проверить на этой сборке OMSI, поэтому она недоступна. При запуске сеанс завершается неудачей до установки любого хука, а файлы восстанавливаются; runtime-операция отклоняется без обращения к памяти OMSI. `Data["capability"]` (runtime) или значение `capabilities` телеметрии называет её. | Диагностическое сообщение сеанса (`Failed`); runtime-результат | См. [совместимость](compatibility.md): возможность недоступна на этой сборке. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` недоступна) | Установка не является установкой OMSI 2: `Omsi.exe` отсутствует, не является 32-разрядным образом PE x86, не имеет версии OMSI 2 (2.x), либо в корневом каталоге нет каталога `maps\` или `Vehicles\`. Хеш исполняемого файла критерием не является. | Диагностическое сообщение плана | Укажите OmsiLaunch корневой каталог установки OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` недоступна); также выбрасывается `CurrentWindowsX64Platform.ValidateCurrent`, который сервис не вызывает | Не Windows 10 или новее, либо ОС или процесс хоста не x64. | Диагностическое сообщение плана (код выхода CLI 3, если выброшено исключение) | Запускайте на 64-разрядной Windows 10 или новее. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Только `CurrentWindowsX64Platform.ValidateCurrent` | Архитектура ОС или хоста не x64. Сервис не вызывает `ValidateCurrent`; в текущем коде нет пути, по которому код выдаётся. | Выбрасывается (`PlatformNotSupportedException`) только этим методом | См. исходный код `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

## Content

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Проверка: NEW_MAP без `EntrypointIdentity` и с незаданным или отрицательным `PresentedEntrypointIndex`. Планировщик: у карты нет точки входа с запрошенной идентичностью или точной меткой (с учётом регистра). | Диагностическое сообщение плана | Задайте `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) или идентичность из `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | Погоду запуска нельзя применить: пресет, который не является файлом каталога `weather` установки, либо ICAO, не состоящий из четырёх букв или цифр. | Диагностическое сообщение плана | Используйте установленный пресет или допустимый код станции. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | Запрошенная точка входа делит метку с более ранней записью карты; OMSI показывает только первую запись для каждой метки, поэтому при запуске её выбрать нельзя. | Диагностическое сообщение плана | Выберите первую запись с этой меткой или другую точку входа. |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Источник [управления HOF](hof-management.md) (коллекция, каталог, ТС или файл) не существует, не содержит `.hof` или в нём нет файла, названного в `Files`. | Диагностическое сообщение плана; сбой запуска | Исправьте источник или список файлов. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Целевой файл ТС или каталог HOF не установлен. | Диагностическое сообщение плана; сбой запуска | Используйте идентичность ТС из `/list:vehicles` или существующий каталог ниже `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Путь HOF выходит за пределы установки, цель не находится ниже `Vehicles\`, имя коллекции не является простым именем или проходится junction/символическая ссылка. | Диагностическое сообщение плана; сбой запуска | Используйте пути относительно установки без `..` и без ссылок. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Две операции HOF записывают в один файл разные байты, либо зеркалирование удаляет файл, который записывает другая операция. | Диагностическое сообщение плана; сбой запуска | Измените порядок, разделите или удалите конфликтующие операции. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Операция HOF без целей или источника, файл-источник не `.hof`, целевой файл не `.bus`/`.ovh`, либо превышен лимит размера или количества (8 MiB на файл, 256 файлов на операцию, 256 MiB на сеанс). | Диагностическое сообщение плана; сбой запуска | Исправьте операцию. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` недоступна) | Карта NEW_MAP определена, но нет ни представленного индекса, ни идентичности. Всегда сопровождает `OL_E_ENTRYPOINT_NOT_FOUND`. | Диагностическое сообщение плана | То же. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` не является установленным `Vehicles\...\*.hof`. | Диагностическое сообщение плана | Используйте идентичность из `/list:hofs`. (Поля транспортного средства игрока на этом build в любом случае делают план неисполнимым.) |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Проверка: NEW_MAP с незаданным `MapIdentity` или не в форме `maps\<dir>\global.cfg`. Планировщик: карта не установлена. | Диагностическое сообщение плана | Используйте идентичность из `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Не было перехвачено исключение `FileNotFoundException`/`DirectoryNotFoundException` без кода, например `/list:repaints` с неизвестным `/vehicle-scope` или `/list:entrypoints` с неизвестным `/map`. | CLI-конверт, код выхода 6 | Исправьте идентичность. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` не является элементом `.cti` этой модели (проверяется только при заданном `Model`). | Диагностическое сообщение плана | Используйте идентичность из `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | Карта, на которую ссылается выбранный `.osn`, не установлена. | Диагностическое сообщение плана | Установите карту или выберите другую ситуацию. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION без `SituationIdentity`, либо `.osn` не установлен. | Диагностическое сообщение плана | Используйте идентичность из `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` не является установленным `Vehicles\...\*.bus`. | Диагностическое сообщение плана | Используйте идентичность из `/list:vehicles`. |

## Installation

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | Аренду установки (lease) (`Local\OmsiLaunch.Installation.<hash>`) удерживает другой владелец в этом сеансе входа в систему, либо ещё жив процесс OMSI, записанный в журнал (PID + время создания + путь к исполняемому файлу; или любой `Omsi.exe` из корневого каталога для журнала после `HandoffCreated` без PID). | Запуск: диагностическое сообщение сеанса через `OL_E_START_SESSION`. Восстановление после сбоя: выбрасывается (`InvalidOperationException` / `IOException`). Код выхода CLI 7. | Остановите другого владельца (`session stop`) или дождитесь завершения OMSI, затем повторите попытку или выполните `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` пуст. | Диагностическое сообщение плана | Передайте каталог установки. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` недоступна); также `ValidateCurrent` | Корневой каталог не существует, имеет атрибут «только для чтения» или не содержит каталога `plugins\`. | Диагностическое сообщение плана | Укажите реальную установку OMSI, доступную для записи. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (план и запуск) | Установленный файл `plugins\OmsiLaunch.*` отличается от hash в `release-manifest.json` (или от эталонного замыкания плагина при отсутствии манифеста). | Диагностическое сообщение плана (план неисполним, код выхода CLI 1); диагностическое сообщение сеанса через `OL_E_START_SESSION` — только если файлы изменились между планированием и запуском | Переустановите пакет OmsiLaunch, чтобы `plugins\` и манифест совпадали. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (план и запуск) | В манифесте нет записи для обязательного файла плагина. | Диагностическое сообщение плана; диагностическое сообщение сеанса через `OL_E_START_SESSION` при той же гонке, что выше | Переустановите пакет. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (план и запуск) | Обязательный файл `plugins\OmsiLaunch.*` отсутствует в установке OMSI, либо файл `plugins/`, указанный в `release-manifest.json`, не установлен. | Диагностическое сообщение плана; диагностическое сообщение сеанса через `OL_E_START_SESSION` при той же гонке, что выше | Установите замыкание постоянного плагина (набор его файлов) ([установка](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Только `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` равно false. Сервис этот метод не вызывает; в текущем коде нет пути, по которому код выдаётся. | Выбрасывается только этим методом | См. исходный код. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` пуст, не является JSON, не содержит массива `files` или содержит запись без `path`/`sha256`, hash не из 64 шестнадцатеричных цифр, абсолютный путь, путь с `:`, пустым сегментом, сегментом `.` или `..`, либо путь, указанный дважды (сравнение без учёта регистра, `/` и `\` эквивалентны). UTF-8 BOM допускается. | План: обёрнут в `OL_E_RUNTIME_ARTIFACT_MISSING`; запуск: через `OL_E_START_SESSION` | Переустановите пакет. |

## InvalidArgument

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Проверка: `Date.Value`/`Time.Value` задано, а режим не `Explicit`. CLI: неизвестный флаг, отсутствующее значение, неверное целое число или диапазон, `/saved` в сочетании с `/map`/`/entrypoint`, неизвестный маршрут команды, любое `ArgumentException`/`FormatException` без кода. | Диагностическое сообщение плана; CLI-конверт, код выхода 2 | Исправьте аргумент. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (запуск) | Значение семантического параметра вне диапазона, не логическое, не из допустимого набора или имеет неверный формат (`graphics.particles` требует четыре поля). При планировании значения не проверяются. | Диагностическое сообщение сеанса через `OL_E_START_SESSION` | Используйте значение из [таблицы параметров](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | Ключ существует, но недоступен для записи (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Диагностическое сообщение плана; код выхода CLI 2 | Удалите ключ. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | Ключа нет в `ConfigurationCatalog`. | Диагностическое сообщение плана; код выхода CLI 2 | Используйте ключ из каталога. |

## LaunchSpec

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | Корень не является JSON-объектом, либо десериализация не дала записи. | Выбрасывается (`InvalidDataException`), код выхода CLI 2 | Исправьте файл ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | Файл `/spec` не существует. | Выбрасывается (`FileNotFoundException`), код выхода CLI 6 | Проверьте путь. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | Файл превышает 1 MiB. | Выбрасывается (`InvalidDataException`), код выхода CLI 2 | Уменьшите файл. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Член, не являющийся публичным свойством записи в этой позиции; сообщение `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Выбрасывается (`InvalidDataException`), код выхода CLI 2 | Удалите или переименуйте член. |

## LocalControl

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Обработчик владельца (`OwnerSession`) | Команда не является `session.status`, `session.events`, `session.stop` или `runtime.execute` с аргументом `operation`. | Ответ управления; код выхода CLI 7 | Используйте поддерживаемую команду. |
| `OL_E_CONTROL_FAILED` | Клиент CLI (`ReportForwarded`, `CliEventWatch`) | Владелец ответил `Ok = false` без кода ошибки. | CLI-конверт, код выхода 7 | Прочитайте сообщение; проверьте консоль/диагностику владельца. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | Обработчик владельца выбросил исключение, сообщение которого не содержит кода `OL_E_` (например, сеанс уже был закрыт), либо ответ обработчика не удалось сериализовать. | Ответ управления | Прочитайте `session status`; перезапустите владельца, если его больше нет. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (обе стороны) | Префикс длины отрицательный или больше 64 KiB (включая слишком большой фрейм запроса), пустой фрейм, JSON `null`, запрос без `Command` или JSON, который не удалось декодировать. | Ответ управления / CLI-конверт | Используйте документированный протокол ([локальная плоскость управления](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (клиент) | Собственный сериализованный запрос клиента превышает 64 KiB. Сообщается вызывающей стороне; ничего не отправляется. | Ответ управления / CLI-конверт | Уменьшите запрос. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (владелец) | Ответ владельца не помещается во фрейм 64 KiB. Вместо того чтобы отбросить ответ, владелец отвечает этой типизированной ошибкой. `session.status` и `session.events` до неё никогда не доходят: их история событий усекается начиная с самых старых, чтобы уместиться. | Ответ управления / CLI-конверт | Повторите попытку; для событий читайте их чаще. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | `ProtocolVersion` запроса не равен `0.1`; ответ владельца не удалось декодировать или он пуст; владелец закрыл соединение, не ответив, или соединение оборвалось после установления; владелец не сообщил `SessionId`. | Ответ управления / CLI-конверт | Согласуйте версии клиента и владельца; прочитайте `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Обработчик владельца | `session.stop` или `runtime.execute` без `session_id`, равного активному сеансу. | Ответ управления; код выхода CLI 7 | Сначала прочитайте `session.status` и привяжите запрос (CLI делает это автоматически). |

## Other

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | У переданного плана `IsRunnable = false`, либо повторное планирование при запуске даёт неисполнимый план (`Omsi.exe` изменился, контент удалён, отсутствует замыкание плагина); в сообщении перечислены текущие коды `OL_E_`. | Выбрасывается (`InvalidOperationException`); код выхода CLI 1 | Выполните планирование заново и устраните перечисленные диагностические сообщения. |

## Presentation

Все выдаются `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). При планировании они оборачиваются в `OL_E_SESSION_PRESENTATION_INVALID` (сообщение содержит код); при запуске передаются через `OL_E_START_SESSION`.

| Код | Значение / типичная причина | Что делать |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | Файл `.itx` пуст, содержит нечётное число непустых строк, либо строка URL не является абсолютным URL `http`/`https`. | Используйте пары строк «URL / цель». |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (разрешается относительно рабочего каталога процесса) не существует. Выбрасывается как `FileNotFoundException`. | Передайте путь к существующему `.itx`. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` равен `Override` без `OverrideProfilePath`. Код выхода CLI 2, если выброшено исключение. | Укажите `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Строка цели содержит абсолютный путь, содержит `..`, начинается с `\`, разрешается за пределы установки, не содержит компонента `Texture\` или проходит через junction/symlink. | Используйте относительные цели `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` не существует. | Исправьте каталог. |
| `OL_E_SPLASH_ASSET_MISSING` | В каталоге ресурсов отсутствует `ENG.bmp` или `<LANG>.bmp`, либо при заполнении `.omsilaunch\assets\splash` отсутствует упакованный `assets\splash\<LANG>.bmp`. | Предоставьте файлы BMP / переустановите пакет. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | BMP заставки (splash) не является 24-битным растровым изображением `BM` размером 640×480. | Преобразуйте изображение. |

## Process

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (пути сбоя запуска и сбоя супервизора) | Завершение OMSI или ожидание её завершения при очистке после ошибки выбросило исключение; далее следует внутреннее сообщение. | Диагностическое сообщение сеанса (добавляется к сеансу `Failed`) | Убедитесь, что не осталось `Omsi.exe`, затем выполните `/recover`, если есть незавершённый журнал. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` завершился ошибкой сразу после `CreateProcessW` (`Win32=<code>`); процесс завершается. | Диагностическое сообщение сеанса через `OL_E_START_SESSION` | Повторите попытку; проверьте антивирус/права доступа. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI завершилась до `gameplay.entered` (аварийное завершение, закрыт диалог ошибки OMSI, закрыто окно). | Диагностическое сообщение сеанса (`Failed`); выполняется восстановление | Проверьте собственные логи OMSI и `logfile.txt`; найдите в `RuntimeEvents` последнее событие плагина. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` завершился ошибкой (`Win32=<code>` в сообщении). | Диагностическое сообщение сеанса через `OL_E_START_SESSION` | Устраните ошибку Win32 (отсутствующий файл, отказ в доступе, политика). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | Цикл супервизора выбросил исключение (чтение телеметрии, ожидание/завершение процесса, запись журнала); OMSI завершается, выполняется попытка восстановления. | Диагностическое сообщение сеанса (`Failed`) | Прочитайте внутреннее сообщение и лог хоста. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` завершился ошибкой (`Win32=<code>`). | Внутри сообщений `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Завершите OMSI вручную, затем выполните `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` для дескриптора процесса завершился ошибкой. | Внутри сообщений `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | То же. |

## Runtime

«Runtime-деталь» означает конкретный код в `ErrorCode`, сообщение в `Values["detail"]` и тип исключения в `Values["exception"]`.

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` с `preset` при `family`, равном 2 (внешняя) или 3 (карта); пресеты есть только для водителя (0) и пассажира (1). | Runtime-деталь | Не указывайте `preset` или используйте family 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Режим `Date`/`Time` равен `Explicit` без значения или с компонентами вне диапазона. Название историческое; это ошибка проверки на этапе планирования. | Диагностическое сообщение плана | Исправьте значение (и учтите, что явные дата/время на этом build делают план неисполнимым). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: путь `.bus` не существует в рабочем каталоге OMSI (проверяется до нативного вызова, чтобы OMSI не могла подставить запасной вариант). | Runtime-деталь | Используйте идентичность из `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | то же | Нативный MakeVehicle изменил коллекцию дорожных ТС более чем на один объект. | Runtime-деталь (`native_status`, счётчики в сообщении) | Сообщите о проблеме; созданные объекты остаются до конца сеанса. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | то же | Коллекция не изменилась; OMSI молча отклонила ТС. | Runtime-деталь | Проверьте файл `.bus`; попробуйте другую модель. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | то же | Любой другой ненулевой нативный статус. | Runtime-деталь | Сообщите о проблеме, приложив счётчики из сообщения. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | Профилированный вызов PlaceRandomBus вернул статус ошибки. | Runtime-деталь | Повторите попытку, когда игровой процесс стабилизируется; сообщите о проблеме. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; проверки на стороне плагина (`time.set` без `hour`/`minute`/`second`; `camera.set` без `family`/`field_of_view`; `camera.lock` без разбираемого `family`; операции с ТС/кривыми) | Обязательный аргумент отсутствует или пуст. | Runtime-результат (реестр; код выхода CLI 2) или runtime-деталь (плагин) | Передайте аргумент ([runtime-управление](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | Эталонный каталог/файл замыкания плагина или нативный мост, заданные в `OmsiLaunchRuntimePaths`, не удаётся загрузить (может оборачивать `OL_E_RELEASE_MANIFEST_INVALID`). | Диагностическое сообщение плана | Запускайте из неповреждённого пакета. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, INTERNAL-стенд) | Базовое чтение `time.read`/`camera.read` завершилось неудачей, поэтому тест записи пропущен. | Только артефакт пакетного прогона | Не предназначено для пользователей. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` пуст, длиннее 240 символов, содержит NUL или `..`, не начинается с `Vehicles\` или не заканчивается на `.bus`. | Runtime-деталь | Передайте `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | нет (сохранён для совместимости) | Больше не выдаётся. Ранние build выдавали его, когда отменённый запрос оставался в слоте; теперь каждый конечный путь запроса сбрасывает слот, а оставшийся запрос или ответ, обнаруженный в начале нового запроса, очищается. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | Почтовый ящик (mailbox) освобождён, потому что сеанс завершается. | Выбрасывается (`InvalidOperationException`) | Ничего; сеанс окончен. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | Слот почтового ящика содержал значение состояния, отличное от idle, requested или responded (повреждение). Слот сбрасывается, и выдаётся ошибка; следующий запрос работает нормально. | Выбрасывается (`InvalidDataException`) | Повторите попытку; если ошибка повторяется, сообщите о проблеме. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | Указатель на блок констант ТС равен null. | Runtime-деталь | У ТС нет констант; ничего делать не нужно. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` отсутствует в таблице констант ТС. | Runtime-деталь | Сначала получите список констант. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | У объекта, созданного через spawn, VMT находится вне диапазона образа OMSI. | Runtime-деталь | Сообщите о проблеме. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | то же | Созданного объекта нет в коллекции дорожных ТС. | Runtime-деталь | Сообщите о проблеме. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Две соседние точки кривой имеют одинаковый X. | Runtime-деталь | Проблема контента в кривой ТС. |
| `OL_E_RUNTIME_CURVE_EMPTY` | то же | У кривой нет точек. | Runtime-деталь | То же. |
| `OL_E_RUNTIME_CURVE_INVALID` | то же | Ни один сегмент кривой не содержит `x`. | Runtime-деталь | Вычисляйте в пределах области определения кривой. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | то же | `name` неизвестен или его указатель на функцию равен null. | Runtime-деталь | Сначала получите список кривых. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | Указатель на определение ТС равен null. | Runtime-деталь | Handle указывает на ТС без данных определения. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (режим владельца) | Рядом с исполняемым файлом отсутствует `plugins\OmsiLaunch.Plugin.opl` или `plugins\OmsiLaunch.Native.x86.dll`. | CLI-конверт, код выхода 7 | Переустановите пакет. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` отсутствует или пуст для `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (обычно реестр отклоняет такие запросы раньше с `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Runtime-деталь | Передайте handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | Handle неизвестен, объект покинул коллекцию, поколение адреса увеличилось, либо отпечаток объекта (VMT + идентичность определения/модели) изменился, потому что адрес был использован повторно. Остаточная слепая зона: объект того же класса и модели, воссозданный по тому же адресу между двумя чтениями списка. | Runtime-деталь | Снова выполните `road-vehicles.list`/`humans.list` и используйте новый handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; запасной путь `D3DRuntimeApi` | Общая обёртка для сбоев на стороне плагина; `Values["detail"]` содержит сообщение (часто более конкретный код), а `Values["exception"]` — тип исключения. На исключение, вышедшее из операции внутри диспетчера почтового ящика, также отвечается этим кодом (без значений), вместо того чтобы оставить запрос без ответа. | Runtime-результат | Прочитайте `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | У плагина нет реализации операции, которую разрешил реестр (расхождение версий реестра и плагина). | Runtime-деталь | Переустановите согласованный пакет. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | Операции нет в `PublicRuntimeOperationIds`; это касается и всех операций `internal.*`. Проверяется до поиска сеанса. | Runtime-результат; ответ управления; код выхода CLI 2 | Используйте публичный идентификатор операции. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` с `preset` при отсутствии транспортного средства игрока (в headless-сеансах его нет). Также сообщается как `code` события `camera.lock.degraded`, если повторное применение не удалось. | Runtime-деталь / runtime-событие | Выполните фиксацию без пресета или используйте сеанс с транспортным средством игрока. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Успешный результат D3D не содержал значений или содержал неизвестную строку состояния устройства. | Выбрасывается (`OmsiRuntimeException`) | Согласуйте версии хоста и плагина. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | В слоте находится устаревший ответ с тем же идентификатором запроса, что и у нового запроса. Устаревший ответ очищается до выдачи ошибки. | Выбрасывается (`InvalidOperationException`) | Используйте строго возрастающие идентификаторы запросов. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Нет ответа в пределах `timeout`; слот сбрасывается, а запоздавший ответ отбрасывается. | Выбрасывается (`TimeoutException`); код выхода CLI 5 | Повторите попытку с более длинным тайм-аутом; проверьте, что OMSI не заблокирована (модальный диалог, загрузка). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | Конверт ответа повреждён, имеет неверную длину (отрицательную, нулевую или больше слота), чужой идентификатор сеанса или другой идентификатор запроса. Слот сбрасывается до выдачи ошибки, поэтому следующий запрос работает нормально. | Выбрасывается (`InvalidDataException`) | Повторите попытку; если ошибка повторяется, сообщите о проблеме. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (плагин) | `cursor` операции со списком имеет неверный формат или относится к списку другого вида. | Runtime-деталь | Передайте `next_cursor` предыдущей страницы той же операции или опустите его. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | Сериализованный результат превышает почтовый ящик 64 KiB. Постраничные результаты списков вместо этого сокращаются, чтобы уместиться, и продолжаются с `next_cursor`; ни один результат списка не доходит до этого кода. | Runtime-результат | Используйте более узкую операцию или меньший `limit`. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | Указатель на определение или состояние скрипта ТС равен null. | Runtime-деталь | У ТС нет скриптовых объектов. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; почтовый ящик плагина | `RuntimeCommand.SessionId` отличается от идентификатора сеанса handle (выбрасывается хостом), либо запрос дошёл до плагина, привязанного к другому сеансу (возвращается плагином как типизированный результат). | Выбрасывается (`InvalidOperationException`) / runtime-результат | Формируйте команду с `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` события `world.failed`) | Погоду запуска не удалось применить после запуска мира: в OMSI не настроена станция ICAO для `/weather-real` либо её объекты погоды недоступны. Сеанс завершается неудачей. | Сбой сеанса | Настройте станцию в OMSI или используйте `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Ключ `weather.set` называет значение, которое OMSI вычисляет на каждом такте погоды (влажность, коэффициенты освещённости, прозрачность облаков, признак осадков, мокрая земля); ничего не записывается. | Runtime-результат | Задайте исходный ключ, названный в `detail` (например, `dew_point` вместо `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Неизвестный ключ `weather.set`; ничего не записывается. | Runtime-результат | Используйте ключ из списка в разделе [возможности](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` отсутствует в таблице строковых переменных. | Runtime-деталь | Сначала получите список строковых переменных. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Логическое значение погоды не равно `true`/`false`/`1`/`0`. Сейчас недостижимо (см. выше). | Runtime-деталь (определено) | См. исходный код. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` не целое число, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` может быть -1), `scheduled` 0..1; `vehicle.variable.set`: `value` не конечно; `vehicle.curve.evaluate`: `x` не конечно. | Runtime-деталь | Используйте значение из допустимого диапазона. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` отсутствует в таблице числовых переменных. | Runtime-деталь | Сначала получите список переменных. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | Слот переменной или адрес значения равен null. | Runtime-деталь | Переменная не материализована для этого ТС. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Скалярные значения часов записаны, но профилированный нативный вызов SetTime вернул ошибку. | Runtime-деталь | Повторите попытку; прочитайте значение обратно с помощью `time.read`. |

## RuntimeD3D

Все выдаются `CurrentRuntimeControl` (сопоставление нативного статуса в `ThrowD3D`; `detail` содержит операцию и HRESULT, `native_status` — числовой статус). Канал: runtime-результат с кодом в `ErrorCode`; `D3DRuntimeApi` повторно выбрасывает его как `OmsiRuntimeException`.

| Код | Нативный статус / причина | Что делать |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: устройство Direct3D потеряно. | Дождитесь `d3d.restored`; пересоздайте текстуры (поколение изменилось). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: отсутствует или неверен `width`, `height`, `level`, `x`, `y`, `format` или `handle` (диапазоны: width/height 1..4096, уровни 0..16, level 0..15, x/y 0..4095). | Исправьте аргументы. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` не является корректным Base64 или превышает 48 KiB. | Отправляйте прямоугольники меньшего размера. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6 или неизвестное имя `format` (допустимые: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Используйте формат из списка. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Любой другой статус; HRESULT находится в `detail`. | Сообщите о проблеме, указав HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: устройство не готово (до первого кадра или во время остановки). | Повторите попытку после `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: выполняется сброс устройства (device reset). | Повторите попытку после `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: handle текстуры уже освобождён. | Не используйте освобождённые handle повторно. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: handle относится к предыдущему поколению устройства; либо строка handle не соответствует `d3dtex-<session>-<hex>` / равна нулю. | Пересоздайте текстуру. |

## Session

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` по `plugin.request.unsupported` | План: запрошены `LastMapState`, `EntrypointIdentity`, режим даты/времени/года, режим погоды, поля транспортного средства игрока или входные документы (`Requested capability unavailable: <name>`). Телеметрия: плагин отклонил передачу управления (handoff) (для исполнимого плана это невозможно). | Диагностическое сообщение плана; диагностическое сообщение сеанса | Удалите неподдерживаемый запрос ([известные ограничения](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` по `headless.arm.failed` | Плагин не смог взвести в OMSI одноразовый hook headless-запуска. | Диагностическое сообщение сеанса (`Failed`) | Проверьте build; сообщите о проблеме. |
| `OL_E_NO_ACTIVE_SESSION` | Клиент CLI (`ReportForwarded`, `CliEventWatch`) | Ни один владелец не отвечает на управляющем канале (pipe) установки (сеанса нет, либо владелец ещё запускается/выполняет проверку). | CLI-конверт, код выхода 4 | Запустите сеанс или дождитесь, пока он перейдёт в `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` истёк до `plugin.started` (OMSI не загрузила `plugins\OmsiLaunch.Plugin.opl` или зависла до инициализации плагина). | Диагностическое сообщение сеанса (`Failed`) | Проверьте замыкание плагина, `plugins\OmsiLaunch.Plugin.opl` и `logfile.txt` OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` по `plugin.handoff.invalid` или при неразбираемом JSON телеметрии | Плагин не смог прочитать/проверить данные передачи управления при запуске (handoff) (версии 3–6, SHA-256) или отправил некорректную телеметрию. | Диагностическое сообщение сеанса (`Failed`) | Согласуйте версии хоста и плагина (переустановите пакет). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Для этой установки уже есть владелец, который отвечает на `session.status`. | CLI-конверт, код выхода 7 | Используйте команды клиента (`session status`, `session stop`, runtime-команды). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | Состояние сеанса не `Running`. | Выбрасывается (`InvalidOperationException`) | Сначала вызовите `WaitForAsync(session, SessionState.Running, ...)`. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Не удалось построить план заставки/ITX; сообщение содержит код представления. | Диагностическое сообщение плана | См. [Presentation](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (диалог OmsiLaunchW) | Запасной код, который показывается, если план запуска неисполним или сеанс не дошёл до игрового процесса, а диагностического сообщения `OL_E_` нет. | Только окно сообщения (message box) | Прочитайте `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` по `world.situation.failed` | Нативный запуск сохранённой ситуации вернул ошибку (`native_status` в событии). | Диагностическое сообщение сеанса (`Failed`) | Проверьте `.osn` и его карту. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | Плагин запустился, но `Running` не был достигнут в пределах `StartupTimeoutSeconds`. | Диагностическое сообщение сеанса (`Failed`) | Для больших карт увеличьте `/startup-timeout`; найдите в `RuntimeEvents` последнее событие мира. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Любое исключение на пути запуска; сообщение — это внутреннее сообщение (обычно начинается с внутреннего кода). | Диагностическое сообщение сеанса (`Failed`) | Действуйте по внутреннему коду. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` по `world.failed` | Нативный запуск NEW_MAP вернул ошибку (`native_status` в событии). | Диагностическое сообщение сеанса (`Failed`) | Проверьте карту, индекс точки входа и логи OMSI. |

## SessionProfile

Все выдаются `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) или `CliInput` и выбрасываются как `SessionProfileException` (`IOException` с `Code`), код выхода CLI 2. См. [профили сеанса](session-profiles.md).

| Код | Значение / типичная причина | Что делать |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | Каталог `assets` заставки пресета или файл `profile` интернет-текстур не существует внутри пакета. | Добавьте ресурс. |
| `OL_E_SESSION_PROFILE_INVALID` | Нарушение структуры или лимита: больше 256 KiB, не ровно одно корневое отображение, якоря YAML, неизвестный ключ, отсутствует обязательный ключ, не скаляр там, где требуется скаляр, `id` отличается от имени каталога, пресетов не 1..99 или повторяющийся `index`, неположительные тайм-ауты, неподдерживаемый режим погоды/заставки/интернет-текстур, дата/время не `explicit`, некорректный YAML, ошибки разбора чисел/дат. | Исправьте YAML согласно сообщению. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` не пуст и не содержит выбранную карту (NEW_MAP) или карту выбранной ситуации (SAVED_SITUATION). | Выберите совместимый мир. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` не существует. | Проверьте id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Явный аргумент CLI задаёт поле, которым владеет выбранный профиль/пресет. | Уберите флаг или выберите другой пресет. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | Id содержит `\`, `/`, `:` или `..`; либо путь ресурса абсолютный, выходит за пределы пакета или проходит через junction/symlink. | Держите пути внутри пакета. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` отсутствует, вне диапазона 1..99 или не объявлен в профиле. | Используйте объявленный индекс пресета. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` не равна `omsilaunch.session-profile/v1`. | Используйте поддерживаемую схему. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Ключ параметра пресета известен, но недоступен для записи. | Удалите ключ. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Ключа параметра пресета нет в каталоге. | Используйте ключ из каталога. |

## Transaction

См. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md).

| Код | Кем выдаётся | Значение / типичная причина | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Устаревший `closecheck` всё ещё существует после `File.Delete`. | Диагностическое сообщение сеанса через `OL_E_START_SESSION` | Удалите `<root>\closecheck` вручную (права доступа). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Путь, которого не было до сеанса, теперь содержит контент, отличающийся от записанного сеансом; он не удаляется, журнал сохраняется. | Выбрасывается (`IOException`); внутри `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; код выхода CLI 8 | Изучите файл; удалите или переместите его, затем выполните `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (журнал без отпечатков) | В журнале нет отпечатка записанного контента для изначально отсутствовавшего пути, не предназначенного для удаления, поэтому владение доказать нельзя. При запуске сеанса восстановление (recovery) откладывается и повторяется с байтами, запланированными этим сеансом; через `RecoverPendingAsync` код выбрасывается. | Выбрасывается (`IOException`); код выхода CLI 8 | Запустите сеанс с той же спецификацией (она предоставит байты) или изучите и удалите файл, затем выполните `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | SHA-256 резервной копии не совпадает со снимком (snapshot), записанным в журнале; ничего не записывается. | Выбрасывается; внутри `OL_E_RESTORE_FAILED`; код выхода CLI 8 | Восстановите файл из собственной резервной копии; удаляйте журнал, только если вы уверены. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Снимки существуют в памяти, но `journal.json` исчез (удалён во время сеанса). | Выбрасывается; внутри `OL_E_RESTORE_FAILED` | Проверьте файлы сеанса вручную. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` всё ещё существует после удаления (само восстановление прошло успешно и проверено). | Выбрасывается; внутри `OL_E_RESTORE_FAILED`; код выхода CLI 8 | Удалите `<root>\.omsilaunch\journal.json` (права доступа) или снова выполните `/recover` (операция идемпотентна). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (сбой запуска / супервизор) | Завершение OMSI не удалось подтвердить, поэтому файлы не заменялись, пока OMSI может их ещё использовать; журнал сохраняется. | Диагностическое сообщение сеанса (`Failed`) | После завершения `Omsi.exe` выполните `/recover` (или следующий запуск выполнит восстановление автоматически). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (сбой запуска / супервизор) | `RestoreAsync` выбросил исключение; сообщение содержит внутренний код; журнал сохраняется. | Диагностическое сообщение сеанса (`Failed`); код выхода CLI 8, если выброшено из `/recover` | Действуйте по внутреннему коду, затем выполните `/recover`. |

## Warning

| Код | Кем выдаётся | Значение | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Путь, удаляемый сеансом (цель ITX, `Texture\standard.ipr`, `closecheck`), не существовал до сеанса и существует сейчас, но ни один процесс OMSI под этим журналом не запускался, поэтому файл не может быть побочным продуктом сеанса. Он сохраняется, о нём сообщается (сообщение = относительный путь, `Data["sha256"]`); транзакция всё равно завершается. | Диагностическое сообщение сеанса / `RecoveryStatus.Diagnostics` (на состояние не влияет) | Изучите файл; при необходимости удалите его сами. |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | У исполняемого файла OMSI 2 нет известной раскладки (его идентичность кода отсутствует в каталоге сборок). План остаётся исполнимым: каждая нативная возможность определяется и проверяется в процессе, а возможность, которую проверить не удалось, недоступна. | Диагностическое сообщение плана (на исполнимость не влияет) | Ничего не требуется; проверьте предсказанные возможности через `/plan` и `profiles`. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (супервизор) | Запрошен `Behavior.CooperativeShutdown`, но OMSI не удалось попросить выйти (её процедура выхода не определена на этой сборке или OMSI уже закрывается) либо она не вышла за `ShutdownTimeoutSeconds`: OMSI была завершена принудительно. Восстановление выполняется как обычно. | Диагностическое сообщение сеанса | Ничего: сеанс завершается так же, как при принудительном завершении. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Возможность, нужную для запуска, не удалось определить по исполняемому файлу (например, исполняемый файл с защитой DRM, загруженный образ которого отличается от файла). Она определяется в процессе при запуске. | Диагностическое сообщение плана (на исполнимость не влияет) | Ничего не требуется; запуск завершается с `OL_E_CAPABILITY_UNRESOLVED`, если её не удаётся определить и в процессе. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Модуль, который присоединился к сеансу сам (файл параметров, активация по содержимому или по умолчанию), не удаётся загрузить (`Data["code"]` называет причину, `Data["activation"]` — способ присоединения). | Диагностическое сообщение плана (на исполнимость не влияет) | Исправьте, отключите или удалите модуль. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Запись `.omsilaunch\modules.ini` использовать нельзя (не раздел модуля, вне раздела, неизвестный ключ, неверное значение; используется значение по умолчанию) либо параметры запрошены для модуля, который сеанс не загружает. | Диагностическое сообщение плана (на исполнимость не влияет) | Исправьте запись или запрос. |

## Module

Runtime-модули (см. [runtime-модули](../../../reference/runtime-modules.md)). При планировании код — это диагностическое сообщение плана: для запрошенного модуля он делает план неисполнимым; для модуля, присоединившегося сам (файл параметров, содержимое, значение по умолчанию), план сообщает вместо этого `OL_W_MODULE_NOT_LOADED`. В сеансе код — это диагностическое сообщение сеанса (сеанс продолжается, если только код не `OL_E_MODULE_SESSION_FAILED`), а для операции модуля — runtime-результат.

| Код | Кем выдаётся | Значение | Канал | Что делать |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` отсутствует, не является допустимым `omsilaunch.runtime-module/v1` или его `id` отличается от имени каталога. Сообщение называет поле. | Диагностическое сообщение плана; `ListRuntimeModulesAsync` | Исправьте манифест. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Запрошенный идентификатор модуля не установлен ниже `plugins\OmsiLaunch.Modules`. | Диагностическое сообщение плана | Проверьте `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Закреплённый файл отсутствует, отличается от закреплённого SHA-256 или достигается через точку повторного анализа (reparse point); либо в процессе `module.json` изменился после планирования. | Диагностическое сообщение плана; диагностическое сообщение сеанса | Переустановите модуль. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | Диапазон `bridge_protocol` модуля не включает ни одного протокола, который обслуживает эта сборка (1 и 2). | Диагностическое сообщение плана | Используйте версию модуля, созданную для этой версии OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Необходимая возможность неизвестна этой сборке, недоступна на этой установке или не определена в процессе. | Диагностическое сообщение плана; диагностическое сообщение сеанса | См. [возможности](capabilities.md) и `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | Сеанс не запускает привязанную карту модуля либо привязанный файл отличается от закреплённого SHA-256. | Диагностическое сообщение плана; диагностическое сообщение сеанса | Запустите привязанную карту с привязанным содержимым. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | Модуль попросил запустить файл, не являющийся одним из его вспомогательных компонентов. | Runtime-результат внутри модуля | Дефект модуля. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | Управляемый компонент не удалось загрузить или создать его тип точки входа; либо программу сеанса модуля не удалось запустить или она завершилась до готовности. | Диагностическое сообщение сеанса | Проверьте сборку модуля (совместимость с x86, тип точки входа реализует `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` или `Stop` выбросили исключение. | Диагностическое сообщение сеанса | Дефект модуля; применена его `failure_policy`. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Фаза или операция вернулась после истечения своего бюджета (`timeouts`, `timeout_ms`), либо программа сеанса не была готова за `prepare_ms`. | Диагностическое сообщение сеанса; runtime-результат | Дефект модуля; применена его `failure_policy`. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | Модуль был отключён или ранее в сеансе завершился сбоем. | Runtime-результат | Первую ошибку см. в диагностических сообщениях сеанса. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Сеанс не загружал этот модуль, либо он ещё не готов (до начала игры). | Runtime-результат | Запросите модуль при запуске; дождитесь `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Модуль не объявляет такой операции. | Runtime-результат | Проверьте `operations` модуля. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | Операция завершилась ошибкой без кода из каталога; собственный код модуля находится в `Values["module_code"]`. | Runtime-результат | Зависит от модуля. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | После остановки или сбоя отслеживаемый ресурс всё ещё был жив (вспомогательный процесс, который не завершился, фоновый поток, который не закончился) либо его освобождение выбросило исключение. | Диагностическое сообщение сеанса | Сообщите о модуле; процесс в любом случае завершается вместе с OMSI. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Модуль, у которого `failure_policy` равна `fail-session`, завершился сбоем; сеанс остановлен и восстановлен. | Диагностическое сообщение сеанса (состояние `Failed`) | См. предшествующий код модуля. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Запрошенный параметр модуля неверен или не объявлен модулем, модуль одновременно включён и отключён, имя параметра не `<module-id>.<key>` либо флаг модуля объявлен несколькими модулями. | Диагностическое сообщение плана (неисполним); ошибка CLI | Исправьте запрос; `/list:RuntimeModules` показывает параметры каждого модуля. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Плагин OMSI, который модуль перечисляет в `conflicts.plugins`, включён (`plugins\*.opl`) или загружен в OMSI. Причина называет `.opl`. | Диагностическое сообщение плана; диагностическое сообщение сеанса | Отключите или удалите этот плагин. OmsiLaunch его никогда не изменяет. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | зарезервирован | Зарезервирован; нативный компонент вместо этого получает числовые результаты хуков, описанные в разделе [runtime-модули](../../../reference/runtime-modules.md#native-components-and-hooks). | нет | нет |

<a id="non-error-diagnostic-codes"></a>
## Диагностические коды, не являющиеся ошибками

| Код | Кем выдаётся | Сообщение / данные | Значение |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | сообщение = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` создан, его идентичность записана. Диагностическое сообщение сеанса. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | сообщение = SHA-256 удалённого файла | `closecheck`, существовавший до сеанса, был безвозвратно удалён (`SuppressStaleClosecheckWarning = true`). Диагностическое сообщение сеанса. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | сообщение = относительный путь; `Data["sha256"]` | Путь, удаляемый сеансом, был воссоздан OMSI во время сеанса, процесс которого был запущен; он удалён, чтобы восстановить исходное отсутствие файла. Диагностическое сообщение сеанса / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | сообщение = `manifest` или `self` | Какой эталон использует проверка постоянного плагина. Диагностическое сообщение плана. |
| `module.planned` | `SessionPlanner` | сообщение = идентификатор модуля; `Data["module"]`, `Data["status"]`, `Data["activation"]` | Модуль входит в план. Диагностическое сообщение плана. |
| `module.disabled` | `SessionPlanner` | сообщение = идентификатор модуля | Модуль из содержимого или по умолчанию не допущен отключением (запрос или файл параметров). Диагностическое сообщение плана. |
| `session_profile.selected` | `SessionPlanner` | сообщение = id профиля; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Происхождение сеанса, скомпилированного из профиля сеанса. Диагностическое сообщение плана. |

Имена runtime-событий (`RuntimeEvent.Type`, не диагностические сообщения) перечислены на странице [жизненный цикл сеанса](../concepts/session-lifecycle.md#telemetry-events).
