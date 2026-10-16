# Примеры CLI

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Перевод [исходной страницы на английском языке](../../../reference/cli-examples.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Минимальные корректные вызовы `OmsiLaunch.exe` для OmsiLaunch `0.1.0-beta4`, каждый с ожидаемым кодом выхода процесса и пояснением, что изменяется и что восстанавливается. Если не указано иное, каждый пример выполняется из корневого каталога установки OMSI (`<OMSI_PATH>`); синтаксис определён в [справочнике по CLI](cli.md), коды выхода — в разделе [коды выхода](exit-codes.md). К любой команде можно добавить `--json`, чтобы получить структурированный конверт.

<a id="conventions"></a>
## Соглашения

- **Изменяет**: файлы или состояние OMSI, изменяемые командой. «Overlay сеанса» (временная замена файла на время сеанса) означает файл, для которого в транзакции создаётся снимок (snapshot), который применяется до запуска OMSI и побайтово восстанавливается по завершении сеанса.
- **Восстанавливается**: то, что отменяется по завершении сеанса (обычная остановка, Ctrl+C, трей, `session stop`, `/observe-seconds`) или при восстановлении после сбоя.
- Runtime-записи (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) изменяют только память OMSI; они никогда не откатываются, поскольку при остановке OMSI принудительно завершается.
- Заполнители: `<OMSI_PATH>` — установка OMSI 2, содержащая пакет OmsiLaunch (например, `C:\OMSI 2`); `<OTHER_OMSI_PATH>` — другая установка; `<SPEC_PATH>` и `<ITX_PATH>` — ваш файл LaunchSpec и ваш профиль интернет-текстур (Internet Textures); `<HANDLE>` — handle, выведенный предшествующей командой `list` или `create`, а `<BASE64>` — пиксельные данные в Base64. Все остальные значения — литералы, работающие на стандартной установке OMSI 2 (`grundorf-quick` — пример профиля, определённый на этой странице).
- Путь с пробелами следует заключать в кавычки и не заканчивать путь в кавычках символом `\` (разбор аргументов Windows превращает `\"` в литеральную кавычку): `"C:\OMSI 2"`, а не `"C:\OMSI 2\"`.
- Каждая командная строка на этой странице разбирается гейтом документации (`tests/OmsiLaunch.DocumentationTests`, гейт `examples`); примеры идентификации, обнаружения, планирования и клиентские примеры также были выполнены на реальной установке (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Идентификация и обнаружение (без сеанса)

```text
OmsiLaunch.exe /version
```
Код выхода `0`. Выводит `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. Ничего не изменяет.

```text
OmsiLaunch.exe profiles --json
```
Код выхода `0`. Перечисляет поддерживаемые хеши `Omsi.exe` и их статус проверки. Ничего не изменяет.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Код выхода `0`. Каталог публичных возможностей; `help <family>` фильтрует его. Ничего не изменяет.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Код выхода `0` (обе формы идентичны). Сообщает о запущенных процессах `Omsi.exe` и о том, отвечает ли владелец OmsiLaunch для этой установки. Ничего не изменяет.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Код выхода `0` (`2` для неизвестной категории). Обнаружение только для чтения; циклы junction пропускаются. Ничего не изменяет. Выводимые здесь значения `Identity` — это в точности те строки, которые ожидают `/map`, `/saved`, `/vehicle-scope` и `LaunchSpec` (например, `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Планирование и проверка

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Код выхода `0`, если план `READY`, и `1`, если `NOT RUNNABLE` (например, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Ничего не изменяет; OMSI не запускается.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Код выхода `0`/`1`, как выше; `/validate` — псевдоним `/plan`. JSON — это исходный `SessionPlan` (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Код выхода `1`. `/date`, `/time`, `/year`, `/weather*` и флаги транспортного средства игрока принимаются, но в этой сборке не применяются; план содержит `OL_E_CAPABILITY_UNAVAILABLE` и неисполним.

```text
OmsiLaunch.exe /last /plan
```
Код выхода `1`. `LAST_MAP_STATE` недоступно для этого профиля (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Запуск сеансов (режим владельца)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Код выхода `0`, если сеанс завершается в состоянии `Completed`, `1` при `Failed` или неисполнимом плане. Изменяет: overlay сеанса для `GUI\NewSplashscreen_ENG.bmp` и `GUI\NewSplashscreen_<lang>.bmp` (управляемая заставка используется по умолчанию), обработку `closecheck`, передачу управления при запуске (handoff). Восстанавливается: каждый overlay, побайтово, по завершении сеанса. Консоль остаётся подключённой, пока OMSI не завершится, не будет подтверждён пункт трея «End session», клиент не отправит `session stop` или не будет нажато Ctrl+C.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Код выхода `0`. То же, что выше, но сеанс останавливается через 8 s после достижения `Running` (раньше — при остановке через трей или pipe). Используется проверочными скриптами.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Код выхода `0`/`1`. SAVED_SITUATION: карта, время и позиция берутся из `.osn` (`situations\Linie 5.osn` поставляется с OMSI 2 и начинается на карте Berlin-Spandau с автобусом игрока). Значение — это идентификатор сохранённой ситуации, выводимый `/list:Situations` (относительно установки, без учёта регистра); одно только имя файла, например `Linie 5.osn`, не разрешается (`OL_E_SITUATION_NOT_FOUND`, код выхода `1`). `/map` или `/entrypoint-index` вместе с `/saved` отклоняются с кодом выхода `2`. Изменения и восстановление — как для NEW_MAP. Сама OMSI записывает карту ситуации в `[last_map]` файла `options.cfg`; эта запись OMSI не откатывается, если только `/set` не подменяет `options.cfg` через overlay (см. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Код выхода `0`. Изменяет: `options.cfg` (overlay сеанса, семантическая правка токенов/векторов; байты CP1252 сохраняются), а также overlay заставки. Восстанавливается: `options.cfg` и файлы заставки точно (RV-005, RV-006). `/set:graphics.texture=...` завершается с кодом `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` завершается с кодом `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Код выхода `0`. Изменяет: overlay заставки нет; только передача управления при запуске и обработка `closecheck`. Восстанавливается: для заставки восстанавливать нечего.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Код выхода `0` (`1` с `OL_E_SESSION_PRESENTATION_INVALID`, если каталог или BMP отсутствует либо BMP не 24-битный 640x480). Изменяет: `GUI\NewSplashscreen_ENG.bmp` и `GUI\NewSplashscreen_PTB.bmp` из пользовательского каталога (overlay сеанса). Восстанавливается: оба файла точно.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Код выхода `0`. Изменяет: на диске ничего, кроме overlay заставки; внутрипроцессный загрузчик подавляется на время сеанса.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Код выхода `0` (`2` с `OL_E_ITX_PROFILE_REQUIRED`, если профиль не указан; `1` для недопустимого профиля или цели вне `Texture\`). Изменяет: `Texture\standard.itx` (overlay сеанса); каждая цель, указанная в профиле, и `Texture\standard.ipr` становятся удалениями в рамках сеанса. Восстанавливается: overlay удаляется, удалённые оригиналы восстанавливаются; файлы, созданные OMSI по этим путям во время сеанса, удаляются как побочные продукты сеанса.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Код выхода `0`. Ожидает `Running` до 300 s (+5 s) вместо 180 s по умолчанию. `/shutdown-timeout:60` принимается, но в этой сборке ни на что не влияет.

<a id="predefined-session-profile"></a>
### Предопределённый профиль сеанса

Файл профиля `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
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
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Код выхода `0`. Изменяет: `options.cfg` (параметры пресета, overlay сеанса) и overlay заставки. Восстанавливается: всё перечисленное. Добавление `/map:...` или `/set:graphics.maxFPS=60` приводит к коду выхода `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); отсутствие `/predefined-profile-index` приводит к коду выхода `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Пример из пакета `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` демонстрирует полную схему, но в поставляемом виде его блок `new:` запрашивает `date`, `time` и `weather`, которые эта сборка применить не может: планирование его с `/new` даёт `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); перед использованием эти ключи следует удалить.

<a id="launchspec-file"></a>
### Файл LaunchSpec

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Код выхода `0`/`1`. Пример из пакета выбирает Grundorf, индекс точки входа `1`, управляемую заставку, нативные интернет-текстуры; `RootPath: "."` разрешается в каталог исполняемого файла. Изменения — как в явном примере NEW_MAP. Спецификация с неизвестным свойством завершается с кодом `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); отсутствующий файл — с кодом `6` (`OL_E_SPEC_NOT_FOUND`); файл больше 1 MiB — с кодом `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Код выхода `0`/`1`. Явная установка `<OTHER_OMSI_PATH>` имеет приоритет над `RootPath` из спецификации; `/startup-timeout` переопределяет `Behavior.StartupTimeoutSeconds` из спецификации только потому, что он указан.

<a id="silent-detached-start"></a>
### Тихий (отсоединённый) запуск

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Код выхода `0`, как только `OmsiLaunchW.exe` запущен (`{"delegated": true, "host_process_id": <pid>}`); `7`, если `OmsiLaunchW.exe` отсутствует (`OL_E_WINDOWS_HOST_MISSING`) или не удалось его запустить. Запускающий процесс сразу возвращает управление и не удерживает открытыми консоль или каналы (pipe) вызывающей стороны: скрипт, перехватывающий его вывод, сразу получает конец файла (итоговый набор runtime-проверок `T04`). Сам сеанс выполняется в `OmsiLaunchW.exe`: вывода в консоль нет, ошибки показываются в окнах сообщений, значок в трее доступен. Ход выполнения можно проверить с помощью `session status`, `events watch` и `.omsilaunch\diagnostics\<sessionId>-host.log`. Полный справочник: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Управление работающим сеансом (режим клиента)

Эти команды выполняются из того же каталога установки, пока работает владелец. Каждая завершается с кодом `4` (`OL_E_NO_ACTIVE_SESSION`), если владелец не отвечает, и с кодом `7` при ошибке управления.

```text
OmsiLaunch.exe session status --json
```
Код выхода `0`. Возвращает `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Ничего не изменяет.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Код выхода `0` (`events watch` работает до нажатия Ctrl+C). Ограниченный список runtime-событий (`gameplay.entered`, события жизненного цикла D3D, ...). Ничего не изменяет.

```text
OmsiLaunch.exe session stop
```
Код выхода `0` (`{"accepted": true, "session_id": "..."}`). Запрашивает каноническую остановку: OMSI принудительно завершается, владелец восстанавливает overlay, журнал удаляется. Клиент сразу возвращает управление; процесс-владелец завершается после восстановления.

<a id="runtime-reads"></a>
## Runtime-чтение

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Код выхода `0` с `RuntimeCommandResult` (`Succeeded`, `Values`) в конверте. Ничего не изменяет. Тайм-аут 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, код выхода `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Код выхода `0`; `2`, если отсутствует обязательный аргумент (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, сообщается до отправки запроса); `7`, если плагин отклоняет запрос: `OL_E_RUNTIME_OPERATION_FAILED` с конкретной причиной в `Values.detail`, например `OL_E_RUNTIME_OBJECT_HANDLE_STALE` для handle, который больше не указывает на тот же объект, или `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Handle действуют в пределах сеанса и берутся из предшествующей команды `list`. Имена переменных, констант и кривых определяются каждой моделью транспортного средства: их следует брать из результата `list`. Приведённые выше имена получены для `rv-000001` — автобуса игрока в сеансе `situations\Linie 5.osn`. Ничего не изменяет.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Код выхода `0`. К операциям без иерархического маршрута, как и к любому маршруту, можно обратиться по идентификатору операции. `timetable.track-entries.list` — ограниченный список: на `situations\Linie 5.osn` он вернул 137 из 825 записей с `truncated=true` (повторная runtime-проверка в рамках аудита документации). Ничего не изменяет.

<a id="runtime-writes"></a>
## Runtime-запись

```text
OmsiLaunch.exe time set --minute=30
```
Код выхода `0`. Изменяет часы OMSI в памяти (проверено: запись, обратное чтение, восстановление вторым вызовом `time set`). При остановке не откатывается.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Код выхода `0` (`2`, если для `camera lock` не указан `--family`). Изменяет состояние камеры на время сеанса. Для `camera lock` нужен PlayerVehicle (например, сеанс `/saved`); в сеансе `/new` без транспортного средства игрока (headless) команда завершается ошибкой (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` в `Values.detail`). Фиксация и её снятие проверены в runtime с сохранённой ситуацией (семейства 0, 2 и 1 с обратным чтением камеры). При остановке не откатывается; политика фиксации заканчивается вместе с сеансом.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Код выхода `0` (`2`, если отсутствует `handle`, `name` или `value`). Изменяет одну числовую переменную скрипта этого транспортного средства. Не откатывается.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Код выхода `0`. Записывает исходную запись погоды OMSI и прекращает обновления живой METAR; из неё OMSI выводит туман, освещение, фактический ветер и влажность при следующем такте обновления погоды без паузы. Каждое значение проверяется до какой-либо записи (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, код выхода `7`). Производный ключ, такой как `relative_humidity`, возвращает `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` и указывает ключ, который следует задать вместо него. Не откатывается. EXPERIMENTAL: проверка в runtime ожидается.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Код выхода `0`. Переключает на живую погоду METAR для этой станции; OMSI загружает её при следующем обновлении погоды. EXPERIMENTAL: проверка в runtime ожидается.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Код выхода `0`. `scripts string set` проходит через собственный `SetStringVariable` OMSI и возвращает прочитанное обратно значение; имя, которое скрипт ТС не объявляет, возвращает `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (код выхода `7`). `scripts trigger` вызывает триггер так же, как это делают обработчики ввода OMSI (`1` нажатие, `0` отпускание); скрипт ТС выполняет его сразу. Имена триггеров и переменных определяются каждой моделью ТС; приведённые выше — примеры. Не откатывается. EXPERIMENTAL: проверка в runtime ожидается.

<a id="spawn"></a>
## Создание (spawn)

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Код выхода `0` с новым handle `rv-NNNNNN` в `Values` (`2`, если отсутствует `--model`; `7` при `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Тайм-аут клиента 30 s. Изменяет коллекцию дорожных транспортных средств (добавляется одно ТС); не назначает транспортное средство игрока. Не откатывается; ТС исчезает вместе с OMSI при остановке.

<a id="d3d-textures"></a>
## Текстуры D3D

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` — значение `handle`, выведенное командой `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` после декодирования должно давать `width * height * 4` байт для 32-битных форматов (8 x 8 x 4 = 256 байт) и не более 48 KiB. Код выхода `0`; `2` при отсутствии обязательных аргументов; `7` для `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (повторное освобождение или describe после освобождения), `OL_E_D3D_STALE_RESOURCE_HANDLE` (handle, полученный до сброса устройства или в другом сеансе), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Создаёт ресурсы GPU, которыми владеет сеанс; они освобождаются явно или при завершении OMSI. Файлы не затрагиваются.

<a id="owner-side-single-runtime-operation"></a>
## Одиночная runtime-операция на стороне владельца

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Код выхода `0`. Запускает сеанс, один раз выполняет `time.read` после `Running` (тайм-аут 5 s), записывает `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, продолжает работу 5 s, останавливает сеанс и выполняет восстановление. Сбой runtime выводится как `runtime_error` и не завершает сеанс.

<a id="recovery"></a>
## Восстановление после сбоя

```text
OmsiLaunch.exe /recovery-status --json
```
Код выхода `0`: `{"pending": false, ...}`, если журнала нет, `{"pending": true, "recovered": false}`, если он есть. Код выхода `7` (`OL_E_INSTALLATION_BUSY`), пока установку удерживает владелец. Ничего не изменяет.

```text
OmsiLaunch.exe /recover --json
```
Код выхода `0`, если незавершённых транзакций не было или восстановление завершилось (`recovered: true`; `diagnostics` может содержать `restore.session-artifact-removed` и `OL_W_RESTORE_FOREIGN_FILE_RETAINED`); код выхода `8`, если журнал был незавершённым и остаётся таким (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); код выхода `7`, пока установку удерживает владелец или записанный в журнал процесс OMSI (`OL_E_INSTALLATION_BUSY`); код выхода `10` (`OL_E_INTERNAL`), если восстановленные байты не проходят проверку (`Restore hash mismatch` или `Restore presence mismatch`; журнал остаётся незавершённым). Изменяет: восстанавливает каждый записанный в журнал файл из `.omsilaunch\backup\<sessionId>\` после проверки его SHA-256, затем удаляет журнал и каталог резервных копий. Отклоняется с `OL_E_INSTALLATION_BUSY`, пока записанный в журнал `Omsi.exe` ещё жив (итоговый набор runtime-проверок `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Быстрая проверка кода выхода (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
