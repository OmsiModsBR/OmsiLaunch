# Коды выхода

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Перевод [исходной страницы на английском языке](../../../reference/exit-codes.md) для OmsiLaunch 0.1.0-beta3. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице перечислены все коды выхода процесса, которые могут вернуть `OmsiLaunch.exe` и `OmsiLaunchW.exe`: публичный управляемый контракт `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), коды нативного shim `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` и `OmsiLaunch.WindowsHost.cpp`), а также правила классификации, которые `CliProgram.Classify` применяет к любому вышедшему наружу исключению (`tools\OmsiLaunch.Cli\Program.cs`). Вызывающая сторона должна определять смысл по коду и по структурированному конверту ошибки, и никогда — по тексту сообщения. Коды ошибок собраны в каталоге [ошибок](errors.md); команды, которые возвращают каждый код, описаны в [справочнике CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Публичные коды выхода (`PublicExitCode`)

| Код | Имя в перечислении | Значение | Когда |
|---|---|---|---|
| 0 | `Success` | Команда выполнена. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` с исполнимым планом; сеанс, завершившийся в состоянии `Completed`; `/silent` после запуска `OmsiLaunchW.exe`; переданная владельцу команда клиента, на которую владелец ответил `Ok=true`; `/recover`, если незавершённой транзакции не было или восстановление завершилось. |
| 1 | `SessionFailed` | План оказался неисполнимым, либо сеанс, которым владеет процесс, завершился в состоянии `Failed`. | `/plan` сообщает `NOT RUNNABLE`; запуск с неисполнимым планом (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` дополнительно показывает последнее диагностическое сообщение `OL_E_` в окне сообщения (message box)); `OL_E_PLAN_NOT_RUNNABLE`, выброшенный методом `StartSessionAsync` (повторное планирование при запуске); сеанс не достиг состояния `Running` за тайм-аут запуска; сеанс завершился в состоянии `Failed`. |
| 2 | `InvalidArguments` | Командная строка, спецификация, профиль или аргументы runtime были отклонены до или во время диспетчеризации. | Неизвестный флаг или маршрут, отсутствующее значение, значение вне допустимого диапазона; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` и `OL_E_RUNTIME_ARGUMENT_REQUIRED` (локально или в ответе владельца); вывод справки об использовании для команды, которую невозможно диспетчеризовать; любые `ArgumentException`, `FormatException`, `InvalidDataException` или `OverflowException`. |
| 3 | `UnsupportedProfile` | Платформа или сборка OMSI не поддерживается. | Вышедшее наружу исключение, код которого начинается с `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Обратите внимание: те же условия, обнаруженные при планировании, делают план неисполнимым, и тогда возвращается `1`. |
| 4 | `NoActiveSession` | Команда клиента не нашла владельца. | `session status`, `session stop`, `events read`, `events watch` или переданная владельцу runtime-операция, когда конечная точка локального управления этой установки не отвечает (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Тайм-аут вышел наружу в виде исключения. | Любое `TimeoutException` (`OL_E_TIMEOUT`, если сообщение не содержит кода, иначе встроенный код, например `OL_E_RUNTIME_REQUEST_TIMEOUT`). На тайм-ауты переданных команд клиента владелец отвечает `Ok=false`, и возвращается `7`, а не `5`. |
| 6 | `NotFound` | Файл или каталог не найден. | `FileNotFoundException` / `DirectoryNotFoundException` (по умолчанию `OL_E_NOT_FOUND`), например `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING`, если он выброшен как исключение, отсутствующий каталог установки при выполнении `/list`. |
| 7 | `OperationRejected` | Команда корректна, но отклонена, либо переданная команда завершилась ошибкой у владельца. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; любой управляющий ответ `Ok=false`, кроме двух кодов аргументов (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); любое другое вышедшее наружу исключение с кодом `OL_E_`, который не классифицирован иначе (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Надёжную (durable) транзакцию не удалось восстановить. | `/recover`, когда журнал был незавершённым и остаётся незавершённым; любое вышедшее наружу исключение, код которого начинается с `OL_E_RECOVERY_` или равен `OL_E_RESTORE_FAILED` (например, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` при собственном восстановлении сеанса). |
| 10 | `InternalError` | Непредвиденное исключение без кода `OL_E_`. | Сообщается как `OL_E_INTERNAL` с категорией `internal`; сообщением служит текст исключения. |

Код `9` не назначен.

<a id="native-shim-exit-codes"></a>
## Коды выхода нативного shim

Возвращаются `OmsiLaunch.exe` / `OmsiLaunchW.exe` до запуска управляемого контроллера. Они не пересекаются с `PublicExitCode`, поэтому вызывающая сторона может отличить сбой запуска хоста от результата контроллера. `OmsiLaunchW.exe` дополнительно показывает `OmsiLaunch could not start the .NET host (code N).` в окне сообщения.

| Код | Значение | Причина |
|---|---|---|
| 100 | Не удалось определить путь к исполняемому файлу | Сбой `GetModuleFileNameW`. |
| 101 | Не удалось разобрать командную строку на токены | `CommandLineToArgvW` вернул null. |
| 102 | Сбой поиска расположения `hostfxr` | Сбой запроса размера в `get_hostfxr_path`: не установлена подходящая среда выполнения .NET (требуется среда выполнения .NET 6 x64). |
| 103 | Не удалось получить путь к `hostfxr` | Сбой второго вызова `get_hostfxr_path`. |
| 104 | Не удалось загрузить библиотеку `hostfxr` | Сбой `LoadLibraryW` для найденного `hostfxr.dll`. |
| 105 | Отсутствуют необходимые экспорты `hostfxr` | Не найдены `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` или `hostfxr_close`. |
| 106 | Не удалось инициализировать управляемый хост | Сбой `hostfxr_initialize_for_dotnet_command_line` для `OmsiLaunch.Controller.dll` (отсутствует `OmsiLaunch.Controller.runtimeconfig.json`, отсутствует `Microsoft.WindowsDesktop.App` 6.0 x64 или пакет повреждён). |

<a id="classification-rules-cliprogramclassify"></a>
## Правила классификации (`CliProgram.Classify`)

Каждое исключение, вышедшее из `CliProgram.RunAsync`, преобразуется в конверт ошибки (`CliInput.WriteError`) и код выхода методом `CliProgram.ReportFailure`, который вызывает `Classify`. Ошибки разбора обрабатываются так же, до диспетчеризации (код выхода `2`). Правила применяются в следующем порядке:

1. Из сообщения исключения извлекается первый токен `OL_E_` (`ExtractCode`): кодом считается максимальная последовательность ASCII-букв, цифр и `_`, начинающаяся с `OL_E_`. Коды передаются в `error.code` дословно.
2. `SessionProfileException` → собственный `Code` исключения, категория `invalid_argument`, код выхода `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → извлечённый код или `OL_E_INVALID_ARGUMENT`, категория `invalid_argument`, код выхода `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → извлечённый код или `OL_E_NOT_FOUND`, категория `not_found`, код выхода `6`.
5. `TimeoutException` → извлечённый код или `OL_E_TIMEOUT`, категория `runtime`, код выхода `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, категория `session`, код выхода `7`.
7. В остальных случаях, если код был извлечён:
   - начинается с `OL_E_RECOVERY_` или равен `OL_E_RESTORE_FAILED` → категория `transaction`, код выхода `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → категория `session`, код выхода `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → категория `session`, код выхода `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → категория `invalid_argument`, код выхода `2`;
   - начинается с `OL_E_UNSUPPORTED_` → категория `unsupported_profile`, код выхода `3`;
   - любой другой код → категория `runtime` для `InvalidOperationException` и `IOException`, иначе `internal`; код выхода `7`.
8. Кода нет вовсе → `OL_E_INTERNAL`, категория `internal`, код выхода `10`.

Ответы на переданные команды клиента минуют `Classify`: `CliProgram.ReportForwarded` возвращает `4`, если нет конечной точки, `2` для `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` для любого другого ответа `Ok=false` и `0` для `Ok=true`.

<a id="scripting-guidance"></a>
## Рекомендации для скриптов

- Считайте `0` успехом, а всё остальное — ошибкой; ветвитесь сначала по числовому коду, затем по `error.code` из конверта `--json`.
- Запуск сеанса возвращает управление только после того, как сеанс завершился и его файлы восстановлены; `1` означает, что транзакция выполнилась, но OMSI завершилась сбоем или план был отклонён, а не то, что файлы остались изменёнными (оставшийся журнал показывает `/recovery-status`).
- `100`..`106` означают, что повреждён пакет или среда выполнения .NET; см. [установку](../getting-started/installation.md) и [упаковку](packaging.md).
