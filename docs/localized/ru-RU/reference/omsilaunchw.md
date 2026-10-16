# Справочник по OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Перевод [исходной страницы на английском языке](../../../reference/omsilaunchw.md) для OmsiLaunch 0.1.0-beta3. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

`OmsiLaunchW.exe` — хост контроллера OmsiLaunch для подсистемы Windows (GUI). Он принимает ту же командную строку, что и `OmsiLaunch.exe`, и выполняет тот же код контроллера (`OmsiLaunch.Controller.dll`). Единственное отличие — способ вывода результатов: окна консоли нет, ошибки показываются в окнах сообщений (message box), а запущенный сеанс виден только по его [значку в трее](windows-tray.md).

Авторитетные источники: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (нативный shim), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) и `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Сравнение OmsiLaunch.exe и OmsiLaunchW.exe

| Аспект | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Подсистема | Консольная. При запуске из Проводника (Explorer) открывает окно консоли. | Windows (GUI). Окна консоли нет. |
| Нативный shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Окружение | не изменяется | задаёт `OMSILAUNCH_WINDOWS_HOST=1` для процесса контроллера до запуска .NET |
| Аргументы | разбираются на токены с помощью `CommandLineToArgvW` и передаются контроллеру без изменений | так же, поэтому оба хоста принимают в точности одни и те же флаги и команды ([справочник CLI](cli.md)) |
| Текстовый вывод | записывается в stdout | подавляется, если не указан `--json` (тогда JSON-конверты (envelope) записываются в stdout, который вызывающая сторона может перенаправить) |
| Ошибки | `OL_E_...: message` или JSON-конверт ошибки | те же правила вывода в консоль **плюс** окно сообщения для каждой ошибки (см. [Диалоги ошибок](#failure-dialogs)) |
| `/silent` | запускает `OmsiLaunchW.exe` с остальными аргументами и возвращает `0` | игнорируется: команда и так выполняется в Windows-хосте |
| Значок в трее | показывается для сеанса владельца | показывается для сеанса владельца |
| Пути остановки | трей, `session stop`, выход из OMSI, `/observe-seconds`, Ctrl+C, закрытие консоли | трей, `session stop`, выход из OMSI, `/observe-seconds` (консоли нет, поэтому Ctrl+C и закрытие консоли неприменимы) |
| Коды выхода | [`PublicExitCode`](exit-codes.md) `0`..`10`, коды shim `100`..`106` | те же коды |

<a id="how-it-starts"></a>
## Как происходит запуск

1. Shim определяет собственный путь (`GetModuleFileNameW`) и ожидает найти `OmsiLaunch.Controller.dll` в том же каталоге.
2. Он разбирает командную строку на токены (`CommandLineToArgvW`) и задаёт `OMSILAUNCH_WINDOWS_HOST=1`.
3. Он находит `hostfxr` через входящий в пакет `nethost.dll`, загружает его, инициализирует контроллер с аргументами (путь к контроллеру не входит в список аргументов, который видит парсер CLI) и запускает его.
4. Shim возвращает код выхода контроллера без изменений.

Если любой шаг до запуска контроллера завершается сбоем, shim показывает окно сообщения с заголовком `OmsiLaunch` и текстом `OmsiLaunch could not start the .NET host (code N).` и завершается с этим кодом:

| Код | Шаг, завершившийся сбоем |
| --- | --- |
| `100` | не удалось определить путь к исполняемому файлу |
| `101` | не удалось разобрать командную строку на токены |
| `102` | сбой поиска расположения `hostfxr` (обычно: не установлена среда выполнения .NET 6 x64) |
| `103` | не удалось получить путь к `hostfxr` |
| `104` | не удалось загрузить `hostfxr` |
| `105` | отсутствуют необходимые экспорты `hostfxr` |
| `106` | не удалось инициализировать управляемый хост (например, отсутствует `OmsiLaunch.Controller.dll` или его конфигурация среды выполнения, либо не установлена среда выполнения Windows Desktop) |

`OmsiLaunch.exe` использует ту же таблицу, но ничего не выводит. Эти диалоги не воспроизводились в runtime (см. [состояние проверки в runtime](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Запуск

Прямой запуск из ярлыка, скрипта или другой программы:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Через `OmsiLaunch.exe` с флагом `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Разместите исполняемые файлы в установке OMSI 2 (структура пакета описана в разделе [упаковка](packaging.md)); если аргумент установки не указан, установкой считается каталог, содержащий исполняемый файл. Явно заданная установка передаётся первым аргументом, как и для `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Поскольку это GUI-программа, `cmd.exe` и Проводник не ждут её завершения. Чтобы дождаться завершения и прочитать код выхода из скрипта, используйте `start /wait OmsiLaunchW.exe ...` в `cmd.exe` или `Start-Process -Wait -PassThru` в PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Делегирование через /silent

`OmsiLaunch.exe ... /silent` (или `--silent`), если он ещё не выполняется под `OmsiLaunchW.exe`, делает следующее (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Ищет `OmsiLaunchW.exe` в каталоге `OmsiLaunch.exe`. Если файл отсутствует: `OL_E_WINDOWS_HOST_MISSING`, код выхода `7`.
2. Запускает `OmsiLaunchW.exe` через `ShellExecute` (`UseShellExecute = true`) с текущим каталогом и всеми аргументами, кроме `/silent`/`--silent`, в исходном порядке. `ShellExecute` не передаёт дескрипторы вызывающей стороны новому процессу, поэтому вызывающая сторона, которая перехватывает вывод `OmsiLaunch.exe /silent`, не блокируется на всё время сеанса (закрытие в runtime BUG-03). Если процесс не возвращён: `OL_E_WINDOWS_HOST_START_FAILED`, код выхода `7`.
3. Записывает конверт `silent` и сразу завершается с кодом `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

Код выхода `0` означает только то, что `OmsiLaunchW.exe` был запущен. Об исходе сеанса (ошибки планирования, уже активный владелец, неудачный запуск) сообщает `OmsiLaunchW.exe` своими диалогами и диагностикой, и после этого два процесса независимы: `OmsiLaunch.exe` завершился, а `OmsiLaunchW.exe` является владельцем сеанса. Чтобы увидеть сеанс, используйте `OmsiLaunch.exe session status`.

`/silent` применяется раньше любой другой команды, поэтому `OmsiLaunch.exe /silent session status` тоже выполняет `session status` внутри `OmsiLaunchW.exe`, где её вывод подавляется. Используйте `/silent` только для запусков.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Что делает каждая команда под OmsiLaunchW.exe

| Командная строка | Результат |
| --- | --- |
| без аргументов | молча выполняет `detect` и завершается с кодом `0` (ничего не показывается) |
| запуск (`/new`, `/saved:...`, `/spec:...`, профиль сеанса) с исполнимым планом при отсутствии владельца | становится владельцем сеанса: значок в трее во время запуска и работы; завершается, когда завершается сеанс (`0` — сеанс завершён, `1` — сбой) |
| запуск с неисполнимым планом | диалог с последним диагностическим сообщением `OL_E_` плана (запасной вариант — `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), код выхода `1` (аудит документации BUG-06; до исправления процесс завершался молча) |
| запуск, когда для установки уже активен владелец | диалог `OL_E_SESSION_ALREADY_ACTIVE`, код выхода `7`; запущенный сеанс не затрагивается |
| запуск, который не достигает состояния `Running` | диалог `The OMSI session did not reach gameplay.` с последним диагностическим сообщением `OL_E_`, код выхода `1`. Пока диалог открыт, супервизор сеанса завершает OMSI и восстанавливает файлы; процесс завершается после закрытия диалога и окончания `CloseAsync` |
| недопустимый аргумент, неизвестный флаг или недопустимый профиль сеанса | диалог с `OL_E_INVALID_ARGUMENT` или кодом `OL_E_SESSION_PROFILE_*`, код выхода `2` |
| команда клиента (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) при отсутствии владельца | диалог `OL_E_NO_ACTIVE_SESSION`, код выхода `4` |
| команда клиента, которую владелец отклоняет | диалог с кодом отклонения (например, `OL_E_CONTROL_SESSION_MISMATCH` или код ошибки runtime), код выхода `7` (`2` для неизвестной операции или отсутствующего аргумента) |
| успешная команда клиента или обнаружения (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | видимого вывода нет, если не указан `--json` и stdout не перенаправлен; код выхода такой же, как у `OmsiLaunch.exe` |
| `/plan` или `/validate` | диалога нет даже для неисполнимого плана; код выхода `0` или `1` |
| любой другой сбой контроллера | диалог с классифицированным кодом `OL_E_`; код выхода согласно [кодам выхода](exit-codes.md) |

<a id="failure-dialogs"></a>
## Диалоги ошибок

Каждая ошибка, которую вывел бы `OmsiLaunch.exe`, также показывается в модальном окне сообщения (`WindowsHost.ShowFailure`), даже если указан `--json`:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` — это сообщение об ошибке, а при сбое сеанса — сообщение последнего диагностического сообщения `OL_E_`. Известное ограничение: при сбое, о котором сообщил плагин, сообщением служат необработанные данные сбоя плагина (например, `{"name":"world.failed",...}`); строка `Code:` при этом верна (см. [известные ограничения](known-limitations.md)). Диалог модальный, и процесс завершается после его закрытия. Подтверждения в runtime: ошибка аргумента, отсутствие активного сеанса и сбой до начала игрового процесса (закрытие в runtime `T04`).

<a id="session-tray-and-exit"></a>
## Сеанс, трей и завершение

Сеанс, владельцем которого является `OmsiLaunchW.exe`, ведёт себя в точности так же, как сеанс, которым владеет `OmsiLaunch.exe` (см. [жизненный цикл сеанса](../concepts/session-lifecycle.md)):

- Значок в трее появляется сразу после запуска сеанса, ещё до того, как OMSI дойдёт до игрового процесса, если в файле `/spec` не задан `Presentation.SuppressTrayIcon`. С `SuppressTrayIcon` видимого интерфейса нет вовсе; остановите сеанс командой `OmsiLaunch.exe session stop` или закрыв OMSI.
- Сеанс завершается, когда OMSI завершает работу, когда в трее подтверждается пункт `End session` (завершить сеанс), когда клиент отправляет `session stop` или когда истекает `/observe-seconds`. Затем OmsiLaunch завершает OMSI, если она ещё работает, восстанавливает каждый изменённый им файл, освобождает аренду установки (lease), удаляет значок из трея и завершается.
- Если сам процесс `OmsiLaunchW.exe` будет принудительно завершён, при следующем запуске OmsiLaunch для этой установки будет восстановлена незавершённая транзакция (см. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Быстрый старт

1. Установите пакет в каталог OMSI 2 ([установка](../getting-started/installation.md)).
2. Создайте ярлык для `OmsiLaunchW.exe` с аргументами сеанса, например `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Запустите его. OMSI запускается без окна консоли; значок OmsiLaunch появляется в области уведомлений.
4. Щёлкните значок правой кнопкой мыши → `Status`, чтобы увидеть сеанс, или `End session` → `End session`, чтобы завершить его.
5. Если что-то пошло не так, диалог покажет код ошибки; подробности находятся в `<OMSI_PATH>\.omsilaunch\diagnostics`.
