# Runtime-управление

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Перевод [исходной страницы на английском языке](../../../reference/runtime-control.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Runtime-управление — это набор операций чтения, записи и действий, которые OmsiLaunch выполняет внутри запущенного сеанса OMSI. На этой странице описаны три способа доступа к нему (API владельца, клиент CLI, локальная плоскость управления), путь запроса от вызывающей стороны до плагина и обратно, устройство handle и идентификаторов запросов, действующие тайм-ауты, что намеренно не записывается в журнал, а также соглашения об аргументах и результатах, с разобранным примером для каждого семейства операций. Сам перечень операций с аргументами, ключами результата и ошибками приведён на странице [возможности](capabilities.md). Источники: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` и `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` и `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Три точки входа

| Точка входа | Кто | Путь | Тайм-аут |
| --- | --- | --- | --- |
| API владельца | Интегратор, который запустил сеанс внутри своего процесса через `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> проверка реестром -> поиск сеанса -> почтовый ящик (mailbox) | `TimeSpan`, заданный вызывающей стороной |
| CLI владельца (`/runtime:<op>`) | Процесс `OmsiLaunch.exe`, который владеет сеансом | одна операция, выполняемая сразу после перехода в `Running`; результат записывается в `.omsilaunch\diagnostics\<session>-runtime-operation.json` и в консоль; сеанс продолжается | 5 s (15 s для `road-vehicles.spawn`) |
| Клиент CLI | Любой вызов `OmsiLaunch.exe` **без** аргумента установки, например `OmsiLaunch.exe time get` | именованный канал (named pipe), команда `runtime.execute` к владельцу той установки, в которой находится исполняемый файл -> владелец вызывает `ExecuteRuntimeAsync` | 8 s (30 s для `road-vehicles.spawn`) и на стороне клиента, и на стороне владельца |
| Локальная плоскость управления | Любой процесс того же пользователя Windows | тот же протокол pipe, что и у клиента CLI; см. [локальное управление](local-control.md) | как выше |

Каждый путь заканчивается в `ExecuteRuntimeAsync`, который обеспечивает соблюдение публичной границы в следующем порядке:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: для операции, которой нет в `PublicRuntimeOperationIds` (включая все операции `internal.*`), возвращается `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; для отсутствующего или пустого обязательного аргумента возвращается `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Ни то, ни другое не затрагивает сеанс.
2. Поиск сеанса (`KeyNotFoundException` для неизвестного handle), `OL_E_RUNTIME_SESSION_MISMATCH`, когда `RuntimeCommand.SessionId` отличается от handle, `OL_E_SESSION_NOT_RUNNING`, если состояние не `Running`.
3. Запрос через почтовый ящик (см. ниже), после чего `ScrubInternalValues` удаляет все ключи результата, которые начинаются с `internal_` или заканчиваются на `_address`, `_pointer`, `_vmt`.

Клиент CLI и плоскость управления сами выполняют шаг 1 до обращения к владельцу, поэтому неверное имя команды сообщается с кодом выхода 2, даже когда активного сеанса нет (`OL_E_RUNTIME_OPERATION_UNKNOWN` и `OL_E_RUNTIME_ARGUMENT_REQUIRED` соответствуют `InvalidArguments`; прочие отклонения — коду 7; отсутствие владельца — коду 4).

Конечная точка плоскости управления существует только пока владелец находится в состоянии `Running`, после запуска и после завершения любого тестового прогона `/runtime-batch`, `/runtime-write-batch` или `/d3d-batch`. Изменяющие команды (`runtime.execute`, `session.stop`) должны передавать активный `session_id`; CLI автоматически получает его из `session.status` (иначе `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Идентификатор запроса и почтовый ящик

`RuntimeCommand(SessionId, RequestId, Operation, Arguments)` сериализуется в конверт `RuntimeCommandWire` (magic `OLRC`, версия 1, заголовок 72 байта, SHA-256 от JSON-полезной нагрузки) и помещается в однозадачный (single-flight) почтовый ящик сеанса размером 64 KiB (`OmsiLaunch.Runtime.<sessionId>`). Плагин опрашивает почтовый ящик каждые 50 ms в UI-потоке OMSI, выполняет там операцию и записывает ответ.

Правила, которые делают канал надёжным:

| Правило | Действие |
| --- | --- |
| Один запрос за раз (single flight) | Один запрос за раз на сеанс; хост упорядочивает вызывающие стороны семафором. Если при поступлении нового запроса слот всё ещё в состоянии `requested`, возвращается `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Привязка к сеансу | Плагин игнорирует (и очищает) запрос, GUID сеанса которого не совпадает с его собственным; хост отклоняет ответ, у которого не совпадает идентификатор сеанса или запроса (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Тайм-аут | Когда крайний срок истекает, хост сбрасывает слот в состояние простоя и выбрасывает `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Запоздалый ответ | Плагин публикует ответ, только если слот всё ещё содержит идентификатор его запроса; ответ на брошенный запрос отбрасывается. Если такой ответ всё же попадёт в слот, следующий запрос хоста обнаружит устаревший слот `responded`, отбросит его и продолжит работу; если этот устаревший ответ несёт **тот же** идентификатор запроса, что и новый запрос, хост выбрасывает `OL_E_RUNTIME_REQUEST_ID_REUSED`. Поэтому вызывающие стороны никогда не должны повторно использовать идентификатор запроса в пределах сеанса. |
| Размер | Запрос, превышающий размер слота, отклоняется до помещения в ящик (`ArgumentOutOfRangeException`). Страница списка, которая не помещается, укорачивается плагином (последние строки отбрасываются, `truncated=true`, меньшее значение `returned_count`, `next_cursor` указывает на первую отброшенную строку); любой другой слишком большой ответ заменяется типизированной ошибкой `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Канал закрыт | После завершения сеанса — `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Идентификаторы запросов — это значения `ulong`, выбираемые вызывающей стороной. CLI использует фиксированные диапазоны: `10_001` для `/runtime:`, `50_001+` для запросов, переданных через плоскость управления, `1+` для пакета чтения, `20_000+` для пакета D3D, `30_000+` для `D3DRuntimeApi`. Интеграторам следует использовать монотонно возрастающий счётчик на каждый сеанс.

<a id="handles-and-stale-detection"></a>
## Handle и обнаружение устаревших handle

| Префикс | Тип | Кто выдаёт | Формат |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + шесть десятичных цифр (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + шесть десятичных цифр |
| `d3dtex-` | Текстура D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Handle непрозрачны и действуют в рамках сеанса: их выдаёт плагин, они никогда не кодируют адрес и не имеют смысла в другом сеансе. При разрешении handle плагин проверяет, что адрес, которому он соответствует, всё ещё присутствует в живой коллекции OMSI (на момент последнего чтения списка; `road-vehicles.list` и `humans.list` обновляют это представление), и повторно считывает отпечаток объекта (Delphi VMT плюс указатель на определение транспортного средства или индекс модели человека). Несовпадение означает, что нативный объект был уничтожен, а его адрес использован повторно: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Для одного и того же живого объекта при повторных чтениях списка возвращается тот же токен. Остаточная слепая зона: объект того же класса и с тем же определением, пересозданный по тому же адресу между двумя чтениями списка, отличить невозможно. Handle D3D проверяются нативным мостом: неизвестный handle или handle чужого сеанса даёт `OL_E_D3D_STALE_RESOURCE_HANDLE`, освобождённый — `OL_E_D3D_RESOURCE_RELEASED`, а смена поколения устройства помечает текстуры как `STALE`.

Handle никогда не являются нативными адресами, и их никогда нельзя разбирать, сравнивать как числа, сохранять между сеансами или передавать в другой сеанс: обращайтесь с ними как с непрозрачными строками, действительными в том сеансе, который их выдал.

Подтверждения в runtime: освобождённый handle D3D остаётся отклонённым после создания новых текстур, а handle из предыдущего сеанса отклоняется следующим сеансом (runtime closure `H02`); сброс устройства D3D (device reset) делает недействительными все живые текстуры (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). Для обнаружения устаревших handle RoadVehicle и Human после естественного удаления объекта (RV-002) нет безопасного способа воспроизвести ситуацию в runtime (OMSI не удаляла объекты в окнах наблюдения, и ни одна публичная операция не удаляет объект); оно покрыто офлайн-тестами.

<a id="what-is-not-journaled"></a>
## Что не записывается в журнал

Runtime-изменения затрагивают только память OMSI. **Они не записываются в журнал транзакции и не восстанавливаются** при завершении сеанса: `time.set`, `camera.set`, `camera.lock` (политика прекращает действовать, когда плагин завершает работу), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` и все ресурсы `d3d.texture.*`. Они исчезают вместе с процессом OMSI, который OmsiLaunch принудительно завершает в конце сеанса, не давая OMSI ничего сохранить (см. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md)). Ничто в семействе runtime-операций не затрагивает файловую систему.

<a id="argument-and-result-conventions"></a>
## Соглашения об аргументах и результатах

- Аргументы — это строковые пары ключ/значение. В CLI `--key=value` после командных слов становится runtime-аргументом (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` — эквивалент для `/runtime:<op>`. Числа используют инвариантную культуру (десятичный разделитель `.`); логические значения — `true`/`false`.
- Командные слова сопоставляются с идентификаторами операций через `CliInput.HierarchicalRoutes` (например, `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). Полная таблица маршрутов приведена в [справочнике CLI](cli.md). У операций D3D нет маршрута из командных слов; используйте `/runtime:d3d.status` или `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Результаты — плоские словари строк. Списки используют ключи вида `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) с полями страницы `count`, `returned_count`, `truncated` и `next_cursor` (см. [постраничный вывод](capabilities.md#pagination)).
- Ошибки содержат `Succeeded=false`, `ErrorCode` с префиксом `OL_E_` и, для сбоев на стороне плагина, `detail` (а также `native_status` для D3D и `exception` для неожиданных сбоев).

<a id="cli-envelope---json"></a>
### Конверт CLI (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

Конверт ошибки имеет вид `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Без `--json` CLI выводит объект результата как JSON с отступами или в виде `CODE: message`.

<a id="api-envelope"></a>
### Конверт API

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` выбрасывает исключение при нарушениях границы после проверки реестром (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) и возвращает `Succeeded=false` при отклонениях реестром и ошибках на стороне плагина.

<a id="worked-examples"></a>
## Разобранные примеры

Во всех примерах CLI предполагается, что для установки, содержащей `OmsiLaunch.exe`, запущен владелец (например, командой `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1` или `OmsiLaunch.exe "/saved:situations\Linie 5.osn"`, когда примеру нужно транспортное средство игрока), а сами примеры выполняются из второй консоли в той же установке.

| Семейство | Команда | Что делает |
| --- | --- | --- |
| Сеанс | `OmsiLaunch.exe session status --json` | Читает `SessionId`, `State`, диагностику и ограниченный список runtime-событий. |
| Время | `OmsiLaunch.exe time get`, затем `OmsiLaunch.exe time set --hour=7 --minute=30` | Читает часы; устанавливает время через профилированный `SetTime` и возвращает результат обратного чтения. |
| Погода | `OmsiLaunch.exe weather get` и `OmsiLaunch.exe weather actual get` | Читает текущее состояние погоды и состояние актуальной погоды / ICAO. `OmsiLaunch.exe weather set --wind_speed=5` задаёт ручную погоду (исходная запись OMSI; живая METAR прекращается), а `OmsiLaunch.exe weather metar set --icao=EDDF` переключает на живую погоду METAR. Обе команды имеют статус EXPERIMENTAL, пока не проверены в runtime. |
| Карта | `OmsiLaunch.exe map get` | Идентификация карты, название, описание, число тайлов, диапазон лет и сторона движения. |
| Камера | `OmsiLaunch.exe camera set --field_of_view=50`, затем `OmsiLaunch.exe camera lock --family=0 --preset=1` и `OmsiLaunch.exe camera unlock` | Записывает FOV; фиксирует семейство камер водителя с пресетом 1 (требуется транспортное средство игрока, например из сохранённой ситуации); снимает политику. |
| ТС | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Счётчики; handle; один снимок. |
| Создание (spawn) | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Создаёт одно RoadVehicle под управлением ИИ (тайм-аут 30 s); возвращает `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` вызывает `PlaceRandomBus`. |
| Игрок | `OmsiLaunch.exe player get` | `present=false` при headless-запуске, иначе снимок ТС игрока. |
| Люди | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Счётчики; handle; один снимок. |
| Расписание | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Счётчики менеджера; ограниченный список строк tracks; логи расписания. |
| Скрипты | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Список, чтение и запись числовых переменных; чтение и запись строковой переменной; триггеры скриптов и звуков (запись строк и триггеры имеют статус EXPERIMENTAL, пока не проверены в runtime). |
| Константы и кривые | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Константы транспортного средства и вычисление кривых. Имена зависят от модели: используйте имена, возвращённые командой `list` (эти были получены для автобуса игрока из `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Метаданные HOF определения транспортного средства. |
| Водители и билеты | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Записи водителей; набор билетов. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`, затем `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` и `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Жизненный цикл текстуры в потоке рендеринга; `<HANDLE>` — это `handle`, выведенный командой `create`; `--pixels_base64` после декодирования должен давать `width * height * 4` байт (32-битные форматы) и не более 48 KiB. |
| События | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Ограниченный список событий; наблюдение с опросом (250 ms) до нажатия Ctrl+C. |
| Остановка | `OmsiLaunch.exe session stop` | Запрашивает каноническую остановку: владелец принудительно завершает OMSI и восстанавливает транзакцию. |

<a id="stability"></a>
## Стабильность

Сам канал (`runtime.command-channel`) имеет статус `STABLE_BETA`: целостность передачи, привязка к сеансу, отбрасывание запоздалых ответов и типизированное отклонение слишком больших ответов покрыты офлайн-тестами `runtime-command.wire-guard`, `runtime-command.session-binding` и `runtime-command.late-response-ignored`, а в runtime — проверкой RV-003 (сеанс `5f641c8d`), Round A RA-019 (клиент бросил `road-vehicles.spawn` через 250 ms; следующий запрос выполнился успешно) и набором проверок канала `R03` в раунде runtime closure (тайм-аут, отмена, повторно использованный идентификатор запроса, отклонение плагином, внутренняя операция, чужой сеанс и отсутствующий аргумент, после каждого из которых следовал успешный запрос). Стабильность отдельных операций указана на странице [возможности](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Повторное использование канала после сбоев

Любой завершающий путь runtime-запроса оставляет почтовый ящик пригодным для повторного использования: успех, типизированная ошибка, искажённый, слишком большой ответ, ответ чужого сеанса или ответ с неверным идентификатором запроса, тайм-аут, отмена вызывающей стороной и ошибки декодирования — всё это заканчивается одной процедурой очистки, которая возвращает слот в состояние простоя и очищает длину и заголовок конверта, так что следующий запрос не может прочитать ничего от предыдущего. Оставшиеся запрос или ответ, обнаруженные при начале нового запроса, сначала очищаются (`OL_E_RUNTIME_REQUEST_ID_REUSED`, если они несут идентификатор нового запроса). На стороне плагина на исключение внутри операции отвечается кодом `OL_E_RUNTIME_OPERATION_FAILED`, на результат, превышающий слот, — кодом `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (фиксированный небольшой конверт), на запрос для другого сеанса — кодом `OL_E_RUNTIME_SESSION_MISMATCH`, а на запрос, брошенный хостом, ответ никогда не даётся. Эти правила покрыты офлайн-тестами (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); пути, которые может вызвать вызывающая сторона (тайм-аут, отмена, повторно использованный идентификатор, типизированные отклонения, запоздалый ответ), имеют также подтверждения в runtime (RA-019, `R03`). Искажённые, чужие или слишком большие ответы невозможно получить извне продукта, и они проверены только офлайн-тестами.
