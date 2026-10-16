# Первый сеанс

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Перевод [исходной страницы на английском языке](../../../getting-started/first-session.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице пошагово разобран первый управляемый сеанс OMSI с OmsiLaunch `0.1.0-beta4`: планирование без запуска OMSI, запуск с явно заданными флагами, запуск с предопределённым профилем сеанса, управление сеансом и его остановка, а также поиск диагностики после завершения. Предполагается, что пакет установлен, как описано на странице [установка](installation.md). Все флаги описаны в [справочнике CLI](../reference/cli.md); дополнительные варианты вызова приведены в [примерах CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Что делает сеанс

Сеанс — это транзакция вокруг одного процесса OMSI: OmsiLaunch делает снимок (snapshot) файлов, которые он затронет (по умолчанию это два растровых изображения заставки в `GUI\`, а также `options.cfg`, если запрошены overlay через `/set`), записывает устойчивый журнал транзакции в `.omsilaunch\`, применяет overlay (временная замена файла на время сеанса), запускает `Omsi.exe` с постоянным плагином, ожидает перехода в игровой процесс (`Running`), сохраняет управляемость сеанса, а в конце завершает OMSI и побайтно восстанавливает каждый затронутый файл. `/new` никогда не выбирает карту или точку входа молча: обе должны быть заданы явно либо взяты из файла `/spec` или профиля сеанса.

<a id="1-plan-nothing-is-started"></a>
## 1. Планирование (ничего не запускается)

Запускайте из корневого каталога OMSI; установкой по умолчанию считается каталог, в котором находится `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

План должен содержать `"IsRunnable": true` (код выхода `0`). В нём перечислены `TouchedFiles` и `PlannedMutations`, так что можно точно увидеть, какие overlay применит сеанс. Прежде чем продолжить, устраните все диагностические сообщения `OL_E_`; пока ничего не записано.

Пример спецификации из пакета делает то же самое с помощью файла:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Запуск с явно заданными флагами

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Что происходит, по порядку:

1. Выводится план (`Plan: READY profile=Omsi23004` на известной раскладке).
2. Восстановление после сбоя по более старому незавершённому журналу, если он есть, получение аренды установки (lease), снимок, журнал, overlay, проверка целостности плагина, запуск `Omsi.exe`.
3. Появляется значок в трее (`OmsiLaunch is running`); см. [значок в трее Windows](../reference/windows-tray.md).
4. При переходе в игровой процесс состояние `Running` выводится в виде JSON (`"State": 14`). Тайм-аут запуска по умолчанию — 180 s (изменить его можно с помощью `/startup-timeout:<1..600>`).
5. Консоль остаётся подключённой до завершения сеанса. Не закрывайте окно консоли, чтобы остановить сеанс: используйте один из способов остановки, описанных ниже.

Необязательные дополнения для первого запуска:

- `/set:graphics.maxFPS=60` (overlay файла `options.cfg`, восстанавливается в конце);
- `/splash:Unset`, чтобы не трогать заставку OMSI, или `/splash-language:DEU`, чтобы выбрать локализованную управляемую заставку;
- `/observe-seconds:30` для автоматической остановки через 30 s после `Running` (удобно для smoke-теста);
- `--json` для структурированного вывода.

Флаги, запрашивающие дату, время, год, погоду или транспортное средство игрока (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...), принимаются, но эта сборка не может их применить: план становится неисполнимым (`NOT RUNNABLE`) с `OL_E_CAPABILITY_UNAVAILABLE`. Не указывайте их.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Запуск с предопределённым профилем сеанса

Профиль сеанса — это YAML-файл `<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, который фиксирует карту, точку входа и до 99 пресетов параметров (схема `omsilaunch.session-profile/v1`; полный справочник — в разделе [профили сеанса](../reference/session-profiles.md)). Создайте `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Затем:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Правила, которые следует помнить: `id` должен совпадать с именем каталога; индекс — `1..99`; явные флаги, которые переопределили бы поле, принадлежащее профилю (`/map`, `/entrypoint-index`, ключ `/set`, принадлежащий пресету, флаги заставки, если у пресета есть `presentation`), отклоняются с `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (код выхода `2`); блок `new:` применяется только с `/new`; при `/saved:<file.osn>` карта сохранённой ситуации должна быть указана в `compatibility.maps`. Входящий в пакет файл `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` иллюстрирует полную схему, но его блок `new:` задаёт `date`, `time` и `weather`, которые эта сборка применить не может, поэтому копировать его следует только после удаления этих ключей.

<a id="4-control-the-running-session"></a>
## 4. Управление запущенным сеансом

Из второй консоли в том же каталоге (без аргумента установки):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Эти команды проходят через локальный управляющий канал (pipe) этой установки ([локальное управление](../reference/local-control.md)); код выхода `4` означает, что здесь не запущен ни один владелец.

<a id="5-stop"></a>
## 5. Остановка

Любой из этих способов завершает сеанс одинаково (OMSI принудительно завершается, затем восстанавливается каждый затронутый файл, затем удаляются журнал и резервная копия):

| Способ | Примечания |
|---|---|
| Значок в трее → `End session` → подтверждение | Доступно в сеансах как `OmsiLaunch.exe`, так и `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | Из другой консоли; возвращает управление сразу, восстановление завершает владелец. |
| Ctrl+C в консоли владельца | Запрашивает остановку; владелец дожидается восстановления и только потом завершается. |
| `/observe-seconds:<n>` | Автоматическая остановка через `n` секунд после `Running`. |
| OMSI завершается сама | Владелец обнаруживает `ProcessExited` и выполняет восстановление. |

Собственная процедура завершения OMSI не выполняется, поэтому OMSI не перезаписывает `options.cfg` при выходе; так задумано, чтобы восстановление было точным. Если закрыть окно консоли владельца кнопкой X, на восстановление остаётся всего 4 s; если оно не успело завершиться, его завершит по журналу следующий запуск (или `OmsiLaunch.exe /recover`). Код выхода владельца равен `0`, если сеанс завершился в состоянии `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Где смотреть после завершения

| Расположение | Содержимое |
|---|---|
| Вывод консоли / `--json` | План, состояние `Running`, итоговое состояние (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Трассировка хоста для сеанса (границы транзакции, запуск процесса, передача управления плагину, переход в игровой процесс, восстановление). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Результат операции `/runtime:`, выполненной владельцем. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | События индикатора в трее. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` после чистого завершения. `true` означает, что остался журнал; выполните `OmsiLaunch.exe /recover`. |

Если сеанс не дошёл до игрового процесса, итоговое состояние содержит диагностическое сообщение `OL_E_` о причине сбоя (например, `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), код выхода равен `1`, а файлы в любом случае восстановлены. См. [коды выхода](../reference/exit-codes.md), [ошибки](../reference/errors.md) и [известные ограничения](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Запуск без консоли

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

делегирует работу `OmsiLaunchW.exe` и сразу возвращает `0`. У сеанса нет консоли; сбои отображаются в окнах сообщений (message box), а значок в трее — единственный видимый элемент. Чтобы следить за сеансом, используйте `session status`, `events watch` и каталог диагностики. Полное описание поведения оконного хоста Windows приведено в [справочнике OmsiLaunchW.exe](../reference/omsilaunchw.md).
