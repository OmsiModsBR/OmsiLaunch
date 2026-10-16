# Возможности

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Перевод [исходной страницы на английском языке](../../../reference/capabilities.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице описано, что умеет OmsiLaunch. Её источник — канонический каталог возможностей `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): там один раз объявлены каждая возможность, каждая публичная runtime-операция с её аргументами, маршрутами CLI и нативной возможностью, а также одно состояние подтверждения для каждой возможности. Набор runtime-операций, проверка аргументов, маршруты команд CLI, нативная проверка каждой операции в плагине, `GetCapabilitiesAsync` и [таблица каталога](#capability-catalog) ниже выводятся из него. Для каждой runtime-операции на странице указаны аргументы, ключи результата и ошибки. Как вызывать операцию (маршруты CLI, конверт API, тайм-ауты, handle), описано в разделе [runtime-управление](runtime-control.md); подтверждения по сеансам приведены на странице [статус проверки в runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Классификация и стабильность

У `PublicCapabilityClassification` четыре значения. Они соответствуют словарю стабильности следующим образом, с понижением уровня там, где подтверждения неполны:

| Классификация | Значение | Стабильность |
| --- | --- | --- |
| `PublicStableBeta` | Публичная, входит в контракт беты, проверена в runtime | `STABLE_BETA` (понижается до `PARTIAL`, где это отмечено) |
| `PublicExperimental` | Публичная, может измениться, проверена в runtime или проверена статически | `EXPERIMENTAL` (понижается до `PARTIAL`, где это отмечено) |
| `InternalOnly` | Исследовательский примитив; недоступен через публичный API или CLI | `INTERNAL` |
| `Unsupported` | Распознаётся для диагностики, отклоняется или отсутствует | `UNAVAILABLE` |

`PublicCapabilityKind` различает `Read`, `Write`, `Action` и `Event`. Каждой runtime-возможности требуется сеанс в состоянии `Running` (`RequiresSession` в реестре) и её нативная возможность, определённая и проверенная на работающей сборке; операция, нативная возможность которой недоступна, отклоняется с `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` помечает возможности, которые зависят от нативных целей (см. [совместимость](compatibility.md)).

Результаты runtime-операций — это словари строк. Ключи, которые начинаются с `internal_` или заканчиваются на `_address`, `_pointer` или `_vmt`, удаляются на границе API (`ScrubInternalValues`) и здесь не перечислены. Ключи результата в таблицах операций ниже — это точные наборы ключей, которые возвращает продукт: они были получены выполнением каждой публичной операции в реальном сеансе (снятие данных для документации в раунде runtime closure, сеанс `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; строки списков записываются как `<row>.<n>.<field>`).

Как сообщается об ошибке операции (`CurrentRuntimeControl.Execute`):

| Ошибка | `ErrorCode` | `Values` |
| --- | --- | --- |
| Отклонение реестром (неизвестная операция или операция `internal.*`, отсутствует обязательный аргумент) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | нет |
| Намеренное отклонение продуктом (производный ключ погоды в `weather.set`) | конкретный код (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (предложение) |
| Сбой D3D | конкретный код `OL_E_D3D_*` | `detail`, `native_status` |
| Типизированный сбой на стороне плагина (устаревший handle, неизвестное имя, значение вне диапазона, нет транспортного средства игрока, ...) | конкретный код, например `OL_E_RUNTIME_OBJECT_HANDLE_STALE` или `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (сообщение), `exception` (имя типа .NET) |
| Операция, нативная возможность которой не определена на работающей сборке | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (нативная возможность) |
| Неожиданное исключение на стороне плагина без кода | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Результат, который не помещается в почтовый ящик (mailbox) размером 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (постраничные списки вместо этого укорачиваются и продолжаются с `next_cursor`, см. [постраничный вывод](#pagination)) | нет |

В столбце «Ошибки» (`Errors`) каждой таблицы названы конкретные коды; они доходят в `ErrorCode` через плагин, мост, плоскость управления, API и CLI без изменений.

<a id="capability-catalog"></a>
## Каталог возможностей

Одна строка на каждую запись каталога, сгенерирована из `PublicCapabilityRegistry.All` (гейт документации завершается ошибкой, если эта таблица устарела). `Session` равно `Running` для возможностей, которым нужен работающий сеанс. `Operations` — конкретные идентификаторы runtime-операций (строки контракта в разделе [публичные runtime-операции](#public-runtime-operations)). `Native capability` называет записи каталога нативных целей (см. [совместимость](compatibility.md)), которые должны быть определены и проверены на работающей сборке. `Evidence` использует единый словарь:

| Подтверждение | Значение |
| --- | --- |
| RUNTIME_VALIDATED | Наблюдалось в реальном сеансе OMSI; идентификаторы сеансов приведены на странице [статус проверки в runtime](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Реализовано и проверено офлайн; эффект в runtime не наблюдался. |
| PARTIAL | Работает для части контракта; примечание называет открытую часть. |
| RUNTIME_VALIDATION_REQUIRED | Реализовано, проверка в runtime ожидается. |
| UNAVAILABLE | Распознаётся для диагностики и отклоняется либо не реализовано; запрос при запуске делает план неисполнимым. |

Операция, которая что-либо изменяет, называет свою политику изменений после своего идентификатора (`PublicRuntimeOperation.MutationPolicy`); ни одна runtime-операция не заносится в журнал:

| Политика изменений | Значение |
| --- | --- |
| `none` | Только чтение (не показывается). |
| `omsi-state` | Изменяет состояние OMSI в памяти; ничто это не отменяет; изменение заканчивается вместе с процессом OMSI. |
| `session-resource` | Создаёт или изменяет ресурс, которым владеет сеанс, освобождаемый явно или по окончании сеанса. |
| `session-policy` | Устанавливает политику, которую плагин применяет повторно, пока она не будет снята или сеанс не закончится. |

<!-- generated:capability-catalog:begin -->
| Возможность | Классификация | Вид | Сеанс | Операции | Нативная возможность | Подтверждение | CLI | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: события плагина передаются через нумерованное кольцо; читатель, который отстаёт, получает telemetry.lost с количеством. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4, раунд A: известная раскладка. |
| `boot.headless-start` | `PublicStableBeta` | Action | no |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | no |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI показывает первую запись для каждой точной метки; при запуске она выбирается по этой метке. Закрыто статически; проверка в runtime ожидается. |
| `world.saved-situation` | `PublicStableBeta` | Action | no |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | no |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: нативная ветка закрыта статически (BI-006); проверка в runtime ожидается. Никогда не эвристика «самый новый .osn». |
| `world.explicit-date` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: записывается в стартовые часы OMSI перед NewSituation (статический анализ); проверка в runtime ожидается. |
| `world.explicit-time` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: записывается в стартовые часы OMSI перед NewSituation (статический анализ); проверка в runtime ожидается. |
| `world.explicit-year` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: записывается в стартовые часы OMSI перед NewSituation (статический анализ); проверка в runtime ожидается. |
| `world.weather` | `PublicExperimental` | Action | no |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): применяется сразу после запуска мира через собственный loadWeather OMSI или её флаги METAR; переопределяет погоду сохранённой ситуации. Закрыто статически; проверка в runtime ожидается. |
| `player-vehicle.model` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; разрешается для диагностики, но не применяется. |
| `player-vehicle.repaint` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false при headless-запуске; полный снимок с сохранённой ситуацией. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: нативная последовательность назначения закрыта статически (research/reports/beta4-static/entrypoint-last-player-vehicle.md, раздел 3), но не реализована до проверки в runtime; запланирована на одну из следующих бета-версий. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: транзакционный overlay, проверено офлайн. |
| `input.controller` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: транзакционный overlay, проверено офлайн. |
| `presentation.splash` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | no |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4, раунд B: подавление применяется и сообщается (internet-textures.suppressed) через кольцо телеметрии. |
| `internet-textures.override` | `PublicExperimental` | Write | no |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Runtime closure I01. |
| `session-profiles` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009, запуск профиля; 1..99 и обнаружение проверены офлайн. |
| `hof.management` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4, раунд A, C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 закрыто статически: SetActualDateTime лишь копирует часы ПК в стартовые часы; даты запуска используют world.explicit-date. Заменено: нативный ABI и постусловия не закрыты. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: записывает actWeather (Weather+0x08) — запись, из которой OMSI выводит значения на такте погоды; закрыто статически. Производные значения (влажность, коэффициенты освещённости, фактический ветер) остаются только для чтения (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Проверка в runtime ожидается. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Требуется PlayerVehicle (его даёт сохранённая ситуация); при headless NEW_MAP — OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | Для обнаружения устаревшего handle после естественного удаления (RV-002) нет безопасного способа воспроизвести в runtime; проверено офлайн-тестами. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paged) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paged), `timetable.trips.list` (paged), `timetable.lines.list` (paged), `timetable.rv-files.list` (paged), `timetable.track-entries.list` (paged), `timetable.bus-stops.list` (paged), `timetable.station-links.list` (paged), `timetable.tours.list` (paged), `timetable.profiles.list` (paged), `timetable.tour-entries.list` (paged), `timetable.logs.read` (paged), `timetable.no-rv-numbers.list` (paged) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paged), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paged), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Запись: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI копирует строку в форме константы; закрыто статически. Проверка в runtime ожидается. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): виртуальные TriggerXML/SoundTrigger через профилированные слоты VMT; закрыто статически. Проверка в runtime ожидается. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Сброс устройства (Device Reset) наблюдался (resetting, restored, инвалидация поколения); отдельный переход lost не воспроизведён (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` имеет классификацию `InternalOnly`: она существует в плагине для исследований (возвращает нативный адрес) и отклоняется на границе API и локальной плоскостью управления, которые сначала проверяют имя операции по `PublicRuntimeOperationIds`; у CLI для неё нет маршрута. `session.stop` принудительно завершает OMSI; кооперативное завершение не реализовано.

<a id="pagination"></a>
## Постраничный вывод

Каждая операция со списком (в [каталоге возможностей](#capability-catalog) помечена как "(paged)") принимает два необязательных аргумента и отвечает четырьмя полями страницы:

| Аргумент | Значение |
| --- | --- |
| `cursor` | С чего начинается страница: `next_cursor` предыдущей страницы. Опущен: первая страница. Курсор из другого списка или с неверным форматом — `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Сколько строк нужно (положительное целое). Больше максимальной страницы списка: используется максимум. Не положительное целое: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Ключ результата | Значение |
| --- | --- |
| `count` | Все записи, которые хранит OMSI. |
| `returned_count` | Строки в этом ответе. |
| `truncated` | `true`, если после этой страницы есть ещё записи. |
| `next_cursor` | Присутствует, только если есть ещё записи: передайте его как `cursor` для следующей страницы. |

Курсор — это устойчивый ключ, а не позиция. Для массивов записей (расписание, имена скриптов, константы, кривые, HOF, водители, билеты, логи) строки получают ключи `<row>.<n>.<field>` (или `name.<n>`) по абсолютному индексу записи, а курсор называет следующий индекс, так что первая страница совпадает с ответом Beta 3. Для коллекций handle (`road-vehicles.list`, `humans.list`) строки возвращаются в порядке handle, а курсор называет последний возвращённый handle: объект, появившийся между двумя страницами, получает более поздний handle и попадает на более позднюю страницу; повторов нет. Значения курсора следует считать непрозрачными.

Максимальные размеры страниц: 1000 для `road-vehicles.list` и `humans.list`; 512 для `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` и `vehicle.curves.list`; 256 для `timetable.tour-entries.list`, `timetable.logs.read` и `vehicle.hofs.read`; 128 для остальных списков расписания, `drivers.read` и `tickets.read`. Если строки страницы всё равно превышают почтовый ящик 64 KiB, плагин отбрасывает последние строки, пока ответ не уместится, и `next_cursor` продолжается с первой отброшенной строки, поэтому ни один список не завершается ошибкой из-за размера.

<a id="public-runtime-operations"></a>
## Публичные runtime-операции

Это точный список идентификаторов операций, которые публичный фронтенд может передавать дальше (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Каждой из них требуется `SessionState.Running`; ни одна не записывается в журнал транзакции и не восстанавливается. Обязательные аргументы проверяются реестром до использования почтового ящика (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); необязательные аргументы проверяет плагин. Общие коды ошибок для всех операций: `OL_E_RUNTIME_OPERATION_UNKNOWN` (операции нет в этом списке), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (в плагине нет реализации), `OL_E_RUNTIME_OPERATION_FAILED` (неожиданное исключение внутри процесса; значения `detail` и `exception`).

<a id="time"></a>
### Время

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `time.read` | Read | нет | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | необязательные `hour` (0..23), `minute` (0..59), `second` (0..59.999, десятичное); хотя бы один | ключи `time.read` после профилированного вызова `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Погода

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | нет | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, а также исходные значения `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, коэффициенты освещённости и влажности — это значения OMSI на каждом такте; `cloud_type` — определённый номер типа облаков | |
| `weather.set` | Write | хотя бы один из `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Диапазоны: туман 0..100000, `lightness` и значения земли 0..1, скорость ветра 0..200, направление ветра 0..360, температура и точка росы -60..60, давление 850..1100, осадки 0..2, интенсивность 0..255, высота облаков 0..20000, `cloud_type` — 1..64 символа кодовой страницы 1252 (`-1` = нет облаков), булевы значения `true`/`false`/`1`/`0` | ключи `weather.read`; при `cloud_type` также `cloud_type_resolved` (`false`, если OMSI не знает тип облаков: она их не рисует) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (производный ключ: `relative_humidity`, `absolute_humidity`, коэффициенты освещённости, `cloud_transparency`, `precipitation_set`, `wet_ground`; `detail` называет ключ, который следует задать), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (неизвестный ключ), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 буквы или цифры) | ключи `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | нет | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Карта

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `map.read` | Read | нет | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Камера

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | нет | `family` (0 — водитель, 1 — пассажир, 2 — внешняя, 3 — карта), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | необязательные `family` (0..3), `field_of_view` (10..170); хотя бы один | ключи `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | обязательный `family` (0..3); необязательный `preset` (0..255, только с семейством 0 или 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Политика применяется повторно каждые 100 ms до вызова `camera.unlock`; неудачное повторное применение порождает событие `camera.lock.degraded` один раз для каждой отличающейся ошибки. |
| `camera.unlock` | Action | нет | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Дорожные транспортные средства и игрок

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | нет | `count`, `ai_collection`, `player_index` (сырой индекс транспортного средства игрока в OMSI, передаётся в том виде, в каком прочитан; чтобы узнать, существует ли ТС игрока, используйте `present` из `player-vehicle.read`) | |
| `road-vehicles.list` | Read | необязательные `cursor`, `limit` | поля страницы; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` — позиция в списке OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | обязательный `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | нет | `present` (`false`, когда у OMSI нет транспортного средства игрока); при `true`: `player_index` плюс все ключи `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | обязательный `model` (канонический путь `Vehicles\...\*.bus`, не более 240 символов, без `..`, должен существовать внутри установки) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Не назначает PlayerVehicle. Занимает несколько секунд; используйте клиентский тайм-аут 30 s. |
| `road-vehicles.place-random` | Action | необязательные `ai_type` (0..255, по умолчанию 0), `group` (0..65535, по умолчанию 1), `type` (-1..65535, по умолчанию -1), `scheduled` (0..1, по умолчанию 0), `tour` (0..65535, по умолчанию 0), `line` (0..65535, по умолчанию 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Люди

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | нет | `count` | |
| `humans.list` | Read | необязательные `cursor`, `limit` | поля страницы; `human.<n>.handle` (`hb-NNNNNN`; `<n>` — позиция в списке OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | обязательный `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Расписание, водители, билеты

Списки расписания, `drivers.read` и `tickets.read` выдаются постранично (см. [постраничный вывод](#pagination)); «поля страницы» ниже означает `count`, `returned_count`, `truncated` и `next_cursor`, и каждый список принимает необязательные `cursor` и `limit`.

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | нет | `invalid` (`true`/`false`) и счётчики записей `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | необязательные `cursor`, `limit` | поля страницы; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | необязательные `cursor`, `limit` | поля страницы; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | необязательные `cursor`, `limit` | поля страницы; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | необязательные `cursor`, `limit` | поля страницы; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | необязательные `cursor`, `limit` | поля страницы; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | необязательные `cursor`, `limit` | поля страницы; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | необязательные `cursor`, `limit` | поля страницы; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | необязательные `cursor`, `limit` | поля страницы; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | необязательные `cursor`, `limit` | поля страницы; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | необязательные `cursor`, `limit` | поля страницы; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | необязательные `cursor`, `limit` | поля страницы; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | необязательные `cursor`, `limit` | поля страницы; `no_rv.<n>.group`, `no_rv.<n>.number`: номера ТС, которые OMSI не отдаёт свободному ИИ (собственный список строк зарезервированных номеров OMSI, читаемый через её собственную раскладку; `OL_E_RUNTIME_VALUE_INVALID`, если список имеет неожиданную форму). Не более 256 на страницу. Статический анализ; проверка в runtime ожидается. |
| `map.tiles.list` | Read | необязательные `cursor`, `limit` | поля страницы; `tile.<n>.x`, `tile.<n>.y` (позиция в сетке), `tile.<n>.tile_index` (собственный индекс тайла в списке тайлов карты), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` с `tile.<n>.error` для тайла, который не удаётся прочитать. Не более 256 на страницу. Позиция и индекс получены статическим анализом; проверка в runtime ожидается. |
| `drivers.read` | Read | необязательные `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | необязательные `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Скрипты, константы, кривые и HOF транспортного средства (в рамках handle)

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | обязательный `handle`; необязательные `cursor`, `limit` | `handle`, поля страницы, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | обязательные `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | обязательные `handle`, `name`, `value` (конечное число с плавающей точкой) | `handle`, `name`, `requested_value`, `value` (обратное чтение) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | обязательный `handle`; необязательные `cursor`, `limit` | `handle`, поля страницы, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | обязательные `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | обязательные `handle`, `name` (с учётом регистра, как объявляет скрипт, до 128 символов), `value` (1..1024 символа) | `handle`, `name`, `value`, прочитанное обратно после записи | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (сама OMSI молча проигнорировала бы неизвестное имя), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | обязательные `handle`, `name` (кодовая страница 1252, до 128 символов), `value` (`1` запускает `name`, `0` запускает `name_off`, `2` запускает `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. Точка входа скрипта выполняется синхронно; имя, которое скрипт не определяет, ничего не делает. За нажатием обычно следует отпускание |
| `vehicle.sound-trigger` | Action | обязательные `handle`, `trigger` (кодовая страница 1252, до 128 символов); необязательный `file` (до 260 символов) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (также: у ТС нет пакета звуков), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | обязательный `handle`; необязательные `cursor`, `limit` | `handle`, поля страницы, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | обязательные `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | обязательный `handle`; необязательные `cursor`, `limit` | `handle`, поля страницы, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | обязательные `handle`, `name`, `x` (конечное число с плавающей точкой; отсутствующий или нечисловой `x` даёт `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (линейная интерполяция) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | обязательный `handle`; необязательные `cursor`, `limit` | `handle`, поля страницы, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Операции D3D выполняются в главном потоке / потоке рендеринга OMSI через нативный мост. Handle текстур имеют вид `d3dtex-<sessionId N>-<16 hex digits>`.

| Операция | Вид | Аргументы | Ключи результата | Ошибки |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | нет | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | обязательные `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); необязательный `levels` (0..16, по умолчанию 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | обязательный `handle`; необязательный `level` (0..15, по умолчанию 0) | 13 ключей `d3d.texture.create` для запрошенного уровня | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, а также ошибки create |
| `d3d.texture.update` | Action | обязательные `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (не более 48 KiB после декодирования); необязательные `level` (0..15, по умолчанию 0), `x` (0..4095, по умолчанию 0), `y` (0..4095, по умолчанию 0) | 13 ключей `d3d.texture.create` после обновления | `OL_E_D3D_INVALID_PIXEL_BUFFER`, а также ошибки describe |
| `d3d.texture.release` | Action | обязательный `handle` | 13 ключей `d3d.texture.create` с `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` при повторном освобождении, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Неудачные операции D3D возвращают значения `detail` и `native_status`. Методы расширения `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) оборачивают эти операции для потребителей API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` возвращает по одной записи `Capability(Name, Available, EvidenceState, Reason)` на каждую запись каталога, не являющуюся `InternalOnly`. `Name` — идентификатор из каталога, `EvidenceState` — его подтверждение из каталога. `Available` вычисляется для установки:

- Записи `Unsupported` никогда не доступны; `Reason` содержит примечание из каталога.
- Каждой записи нужна текущая платформа, а каждой записи, кроме `runtime.current-windows-x64`, — установка OMSI 2.
- Запись с нативными возможностями на известной раскладке сообщает `native: RESOLVED_KNOWN_LAYOUT`. На другой сборке OMSI 2 исполняемый файл определяется статически: `native: STATICALLY_RESOLVED` либо `native: PENDING_IN_PROCESS_RESOLUTION` с указанием того, что файл не определил. Запись в состоянии pending всё равно сообщается как доступная, потому что решает определение в процессе; если оно завершается неудачей, операция или запуск завершаются с `OL_E_CAPABILITY_UNRESOLVED`.

Планировщик сообщает возможности для каждого плана в `SessionPlan.RequiredCapabilities` и `SessionPlan.UnsupportedRequestedFeatures` с теми же идентификаторами каталога (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Неподдерживаемая запрошенная функция несёт своё подтверждение из каталога; обязательная нативная возможность несёт `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` или `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Известные ограничения

- Handle действуют в рамках сеанса и сообщаются как `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, как только их объект исчез. Handle транспортного средства привязан к его VMT, его определению и `IDCode` OMSI (уникальному в процессе числу, которое получает каждый объект карты при создании и которое никогда не используется повторно), поэтому объект, пересозданный по тому же адресу, всегда отличается. Объекты Human — это пулованные слоты, существующие в течение всего сеанса карты: handle человека перестаёт разрешаться, когда человек исчезает (`RenderMe` 0) или слот создаётся снова (новая целевая остановка или отправление).
- Результаты ограничены почтовым ящиком размером 64 KiB; каждый список выдаётся постранично (см. [постраничный вывод](#pagination)).
- Нет перемещения транспортных средств между тайлами, нет записи календаря, нет назначения PlayerVehicle при headless-запуске. Запись строковых переменных, script triggers и запись погоды — `EXPERIMENTAL` (проверка в runtime ожидается). См. [известные ограничения](known-limitations.md).
