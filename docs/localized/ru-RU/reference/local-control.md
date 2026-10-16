# Локальная плоскость управления

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Перевод [исходной страницы на английском языке](../../../reference/local-control.md) для OmsiLaunch 0.1.0-beta3. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Локальная плоскость управления — это конечная точка на основе именованного канала (named pipe), через которую запущенный владелец OmsiLaunch (процесс, запустивший сеанс) принимает семантические команды сеанса от других процессов на той же машине: состояние, события, остановку и публичные runtime-операции. На этой странице конечная точка описана так, как она реализована в `tools\OmsiLaunch.Cli\LocalControlPlane.cs`, а обработчик запросов — так, как он реализован в `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): именование канала, фрейминг, версия протокола, команды, привязка к сеансу, коды ошибок, модель доверия, окно доступности и способы обращения к ней из других инструментов. Команды клиента CLI (`session status`, `session stop`, `events read`, `events watch`, маршруты вроде `time get`) — это тонкие обёртки над этим протоколом; см. [справочник CLI](cli.md). Сами runtime-операции описаны на странице [runtime-управление](runtime-control.md); альтернатива для интеграторов, работающая внутри процесса, — [публичный API](public-api.md).

<a id="summary"></a>
## Сводка

| Свойство | Значение |
|---|---|
| Транспорт | Именованный канал Windows, `PipeDirection.InOut`, байтовый режим, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, входной и выходной буферы размером в один фрейм (65 540 байт) |
| Имя канала | `OmsiLaunch.Control.0.1.<key>`, где `<key>` — первые 16 шестнадцатеричных символов SHA-256 от UTF-8-байтов полного пути корневого каталога установки без завершающего `\`, в верхнем регистре (`LocalControlPlane.PipeNameFor`) |
| Фрейминг | 4-байтовый префикс длины `int32` в порядке little-endian, за которым следует указанное число байтов JSON в UTF-8; один запрос и один ответ на соединение |
| Максимальный размер сообщения | 65 536 байт для запроса и для ответа (`MaxMessageBytes`) |
| Версия протокола | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); на несовпадение отвечается кодом `OL_E_CONTROL_PROTOCOL` |
| Команды | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Привязка к сеансу | `session.stop` и `runtime.execute` требуют, чтобы `Arguments.session_id` совпадал с идентификатором активного сеанса |
| Доступность | С момента, когда сеанс достиг состояния `Running` (после любого проверочного пакета), до перехода сеанса в `Completed` или `Failed` и освобождения конечной точки владельцем |
| Область действия | Одна конечная точка на корневой каталог установки; две установки одного пользователя никогда не используют общий канал |
| Стабильность | STABLE_BETA (офлайн-тесты в `OmsiLaunch.WindowsUiTests`; подтверждения в runtime: RV-003 — параллельные запросы состояния во время многосекундного нативного вызова, Round A RA-008, а также набор проверок сырых фреймов `R04` в раунде runtime closure: на каждый из 17 искажённых, слишком больших фреймов, фреймов с неверным протоколом, неизвестной командой, без привязки и с чужим сеансом был дан ответ с соответствующей типизированной ошибкой, после чего успешно выполнялся `session.status`; зависший клиент не блокирует остальных; привязанная остановка во время выполняющегося создания ТС (spawn) завершает сеанс) |

<a id="pipe-name-derivation"></a>
## Вычисление имени канала

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` — единственное определение идентичности установки, которое также используется арендой установки (lease): он разрешает полный путь (включая сегменты `.` и `..`, разделители `/` или `\`, повторяющиеся разделители) и удаляет завершающие разделители, кроме случая корня диска. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` и `d:\omsi 2` дают одно и то же имя; `D:\OMSI 2` и `E:\OMSI 2` дают разные имена. Клиент всегда вычисляет имя по установке, содержащей запущенный им исполняемый файл (`AppContext.BaseDirectory`), если только вызывающая сторона API не передаёт корневой каталог явно. `0.1` внутри имени — это версия протокола, поэтому будущий протокол сможет сосуществовать с текущим на той же машине.

<a id="framing-and-encoding"></a>
## Фрейминг и кодировка

- Запись: запись сериализуется через `System.Text.Json` с параметрами по умолчанию, проверяется условие `length <= 65536` (иначе `OL_E_CONTROL_MESSAGE_TOO_LARGE`), записывается `BitConverter.GetBytes((int)length)` (4 байта, little-endian в Windows), записывается полезная нагрузка, выполняется сброс буфера.
- Чтение: читаются ровно 4 байта; `length < 0` или `length > 65536` отклоняется с кодом `OL_E_CONTROL_MESSAGE_INVALID`; читаются ровно `length` байтов; выполняется десериализация. Соединение, закрывшееся до того, как был прочитан полный фрейм, владелец молча отбрасывает (без ответа). На отклонённый фрейм ответ даётся, даже если клиент уже записал больше байтов, чем читает владелец: буферы канала вмещают целый фрейм, поэтому запись клиента завершается и он может прочитать ответ (runtime closure BUG-02; до исправления такой клиент и владелец оба блокировались на своих операциях записи).
- Владелец записывает каждый ответ с крайним сроком 5 s: клиент, который подключается, отправляет запрос и никогда не читает ответ, не может удерживать задачу владельца дольше.
- Имена свойств в запросе записываются **в PascalCase и чувствительны к регистру** (`ProtocolVersion`, `Command`, `Arguments`), потому что владелец выполняет десериализацию с параметрами по умолчанию. Ответы используют `Ok`, `Result`, `ErrorCode`, `Message`. Значения перечислений сериализуются как целые числа; значения `Guid` — как строки.
- Один запрос на соединение: владелец читает один запрос, записывает один ответ и закрывает канал. Для каждого запроса открывайте новое соединение.

<a id="request"></a>
### Запрос

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` должен быть идентификатором, который `session.status` вернул для запущенного сеанса (значение выше взято из реального сеанса).

`Arguments` необязателен (`null` или отсутствует) для `session.status` и `session.events`. Ключи аргументов сравниваются порядково (с учётом регистра).

<a id="response"></a>
### Ответ

Фреймы, полученные от реального владельца (runtime closure `R04`, финальный пакет). Успешный `runtime.execute`:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Отклонённый `session.stop` без активного `session_id`:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Команды

| Команда | Аргументы | Результат при `Ok=true` | Примечания |
|---|---|---|---|
| `session.status` | нет | `SessionStatus`: `SessionId` (GUID в виде строки), `State` (целое `SessionState`; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (массив `{Code, Message, Data}`), `RuntimeEvents` (массив) | Только чтение. Клиенты сначала читают это, чтобы получить `SessionId`. |
| `session.events` | нет | Массив `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (монотонный `int64`), `Data` (словарь строк) | Только чтение, ограниченный список; `events watch` опрашивает его и выводит записи с `Sequence` больше последнего увиденного. |
| `session.stop` | `session_id` (обязательный) | `{"accepted": true, "session_id": "..."}` | Запрашивает каноническую остановку (`TerminateProcess` для OMSI, затем восстановление). Возвращает управление сразу; состояние сеанса можно наблюдать через `session.status`, пока конечная точка не исчезнет. |
| `runtime.execute` | `operation` (обязательный), `session_id` (обязательный), а также собственные аргументы операции (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (назначается владельцем, начиная с `50001`), `Succeeded`, `ErrorCode`, `Values` (словарь строк) | Перед выполнением проверяется через `PublicCapabilityRegistry.ValidateRuntimeArguments`: неизвестная или внутренняя операция → `OL_E_RUNTIME_OPERATION_UNKNOWN`; отсутствует обязательное значение → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Тайм-аут 8 s, 30 s для `road-vehicles.spawn`. На runtime-сбой отвечается `Ok=false`, при этом `Result` содержит `RuntimeCommandResult`, `ErrorCode` — его код, а `Message` — `Runtime operation was rejected.` Ключи результата, которые начинаются с `internal_` или заканчиваются на `_address`, `_pointer`, `_vmt`, удаляются API до того, как попадут в канал. |
| любая другая | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

Владелец обслуживает соединения параллельно (каждый принятый клиент обслуживается в собственной задаче), поэтому `session.status` продолжает отвечать, пока выполняется многосекундный нативный вызов, например `road-vehicles.spawn`.

<a id="session-id-binding"></a>
## Привязка к идентификатору сеанса

Изменяющие команды должны указывать сеанс, над которым они выполняются. CLI реализует `TryRequestBoundAsync`: он отправляет `session.status` (750 ms), берёт `Result.SessionId` и повторяет запрос, добавив `session_id` в `Arguments`. На отсутствующий, неразбираемый или отличающийся идентификатор отвечается кодом `OL_E_CONTROL_SESSION_MISMATCH`. Если владелец не сообщает идентификатор сеанса, клиент сообщает `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Метаданные ответа и усечённая история событий

`Metadata` равен `null`, если ответ не несёт сведений о самом себе. `session.status` и `session.events` возвращают историю событий владельца (не более 256 событий, новейшие в конце). Если эта история не помещается в один фрейм размером 64 KiB, самые старые события удаляются, пока она не поместится, и `Metadata` сообщает об этом:

| Ключ | Значение |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | События в этом ответе |
| `events_dropped_count` | Самые старые события, удалённые, чтобы уместиться во фрейм |
| `events_available_count` | События, которые владелец имел для этого ответа |
| `events_first_returned_sequence` | `Sequence` первого возвращённого события |

Без `Metadata` ответ плоскостью управления не усечён. `Sequence` монотонен в обоих случаях, поэтому клиент может также обнаружить события, которые владелец сам вытеснил за пределы своей истории из 256 событий. CLI выводит эти метаданные в конверте `--json`, а в текстовом режиме — примечание.

<a id="error-codes"></a>
## Коды ошибок

| Код | Источник | Значение |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | владелец / клиент | `ProtocolVersion` запроса не равен `0.1`; ответ владельца не удалось декодировать или он был пустым; владелец закрыл соединение, не ответив; соединение оборвалось после установки; или владелец не сообщил идентификатор сеанса. |
| `OL_E_CONTROL_MESSAGE_INVALID` | владелец / клиент | Префикс длины вне диапазона (так отклоняется слишком большой фрейм запроса ещё до чтения его полезной нагрузки), пустой фрейм, JSON `null`, запрос без `Command` или JSON, который не удалось декодировать. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | запись на стороне клиента | Собственный запрос клиента превышает 65 536 байт; об этом сообщается клиенту, и ничего не отправляется. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | запись на стороне владельца | Ответ владельца превышает 65 536 байт; владелец отвечает этой типизированной ошибкой. `session.status` и `session.events` обрезают свою историю событий (начиная с самых старых), чтобы уместиться, поэтому эту ошибку они не порождают. |
| `OL_E_CONTROL_SESSION_MISMATCH` | владелец | `session.stop` / `runtime.execute` без активного `session_id`. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | владелец | Неподдерживаемое имя команды. |
| `OL_E_CONTROL_HANDLER_FAILED` | владелец | Ответ обработчика не удалось сериализовать, или обработчик выбросил исключение без кода `OL_E_` в сообщении; если код есть, вместо этого возвращается он (например, `OL_E_SESSION_NOT_RUNNING` от `ExecuteRuntimeAsync` после завершения OMSI). |
| `OL_E_CONTROL_FAILED` | клиент CLI | Код по умолчанию, когда ответ имеет `Ok=false` без `ErrorCode`. |
| `OL_E_TIMEOUT` | клиент | Клиент был подключён к владельцу, который не ответил в пределах тайм-аута. |
| `OL_E_NO_ACTIVE_SESSION` | клиент CLI | Не удалось связаться ни с одной конечной точкой владельца (`TryRequestAsync` возвращает `null` только при сбое самого соединения). После подключения любой сбой — это один из типизированных кодов выше. Код выхода `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | клиент и владелец | Проверка `runtime.execute` на соответствие публичной поверхности (код выхода `2` в CLI). |

На каждый известный сбой отвечается типизированной ошибкой, чтобы клиент никогда не принял сбой за отсутствие владельца. Без фрейма ответа заканчиваются два случая: клиент, отключившийся до отправки полного запроса (отвечать некому), и запрос, который ещё выполняется, когда сеанс завершается (владелец отменяет ожидающие ответы при закрытии конечной точки; клиент видит конец потока, который клиент CLI сообщает как `OL_E_CONTROL_PROTOCOL` «The owner closed the connection without a reply»). Клиент, отправивший запрос и отключившийся до чтения ответа, не влияет на владельца. Запуск владельца отказывается продолжаться, если от конечной точки установки приходит любой ответ (включая типизированную ошибку), потому что ответ доказывает присутствие владельца. Соответствие ответов кодам выхода CLI описано на странице [коды выхода](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Модель доверия (принятый риск)

- `PipeOptions.CurrentUserOnly` ограничивает канал пользователем Windows (и уровнем целостности), которому принадлежит сеанс; клиентская сторона того же параметра проверяет, что сервер принадлежит тому же пользователю.
- **Любой процесс, выполняющийся от имени того же пользователя Windows, может читать состояние, останавливать сеанс и выполнять публичные runtime-операции.** Дополнительной аутентификации, токена или авторизации отдельных клиентов нет. Это задокументированный принятый риск для беты; не запускайте сеансы OmsiLaunch под учётной записью, общей с недоверенным программным обеспечением.
- Канал никогда не раскрывает нативные адреса, дескрипторы или операции с сырой памятью; принимаются только публичные идентификаторы операций из `PublicCapabilityRegistry`, а внутренние исследовательские примитивы (`internal.*`) отклоняются до любого поиска сеанса.
- Сообщения ограничены по размеру (64 KiB) и разбиты на фреймы; на искажённый или слишком большой фрейм даётся ответ, либо он отбрасывается, не затрагивая сеанс.
- Если в момент, когда владелец начинает прослушивание, имя канала уже занято другим процессом (`IOException`/`UnauthorizedAccessException` при создании), владелец продолжает выполнять сеанс без конечной точки и записывает сбой в `LocalControlPlane.ListenFault`; клиенты в этом случае видят `OL_E_NO_ACTIVE_SESSION`. Чтобы завершить такой сеанс, используйте значок в трее или Ctrl+C.

<a id="availability-window"></a>
## Окно доступности

1. Выполняется `StartSessionAsync`; конечной точки ещё нет. Второй запуск `OmsiLaunch.exe` для того же корневого каталога перед стартом опрашивает `session.status` в течение 250 ms и завершается ошибкой `OL_E_SESSION_ALREADY_ACTIVE`, только если владелец уже отвечает; два владельца, одновременно проходящие этап запуска, упорядочиваются арендой установки (`OL_E_INSTALLATION_BUSY`).
2. Сеанс достигает состояния `Running`; проверочные пакеты (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), если они запрошены, завершаются.
3. `LocalControlPlane.Start()`: конечная точка принимает соединения. Собственная операция владельца `/runtime:`, если она задана, выполняется после этого момента.
4. Конечная точка остаётся доступной в состояниях `ProcessExited`, `Restoring` и `CleaningRuntime` (запрос состояния сообщает эти состояния), пока сеанс не перейдёт в `Completed` или `Failed`.
5. `DisposeAsync` отменяет прослушивание, ожидает завершения выполняющихся клиентских задач, и владелец вызывает `CloseAsync`. После этого имя канала больше не существует.

Клиентам следует использовать короткие тайм-ауты подключения (CLI использует 750 ms для status/stop/events) и трактовать «нет конечной точки» как «нет активного сеанса для этой установки».

<a id="using-the-protocol-from-other-tooling"></a>
## Использование протокола из других инструментов

<a id="c-net-6-or-later"></a>
### C# (.NET 6 или новее)

```csharp
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Same result as OmsiLaunch.Api.InstallationPaths.IdentityKey for any root that is not a drive root;
// reference OmsiLaunch.Api and call InstallationPaths.IdentityKey(root) to cover drive roots as well.
static string PipeNameFor(string installationRoot)
{
    var normalized = Path.GetFullPath(installationRoot).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
    var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    return "OmsiLaunch.Control.0.1." + key[..16];
}

static async Task<JsonDocument> SendAsync(string installationRoot, object request, TimeSpan timeout)
{
    using var cancellation = new CancellationTokenSource(timeout);
    await using var pipe = new NamedPipeClientStream(".", PipeNameFor(installationRoot), PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    await pipe.ConnectAsync(cancellation.Token);
    var payload = JsonSerializer.SerializeToUtf8Bytes(request);          // must be <= 65,536 bytes
    await pipe.WriteAsync(BitConverter.GetBytes(payload.Length), cancellation.Token);
    await pipe.WriteAsync(payload, cancellation.Token);
    await pipe.FlushAsync(cancellation.Token);
    var length = new byte[4];
    await ReadExactlyAsync(pipe, length, cancellation.Token);
    var reply = new byte[BitConverter.ToInt32(length)];
    await ReadExactlyAsync(pipe, reply, cancellation.Token);
    return JsonDocument.Parse(reply);
}

static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken token)
{
    var read = 0;
    while (read < buffer.Length)
    {
        var count = await stream.ReadAsync(buffer.AsMemory(read), token);
        if (count == 0) throw new EndOfStreamException("The owner closed the pipe before a full frame was received.");
        read += count;
    }
}

var root = @"C:\OMSI 2";                                        // <OMSI_PATH>
using var status = await SendAsync(root, new { ProtocolVersion = "0.1", Command = "session.status" }, TimeSpan.FromMilliseconds(750));
var sessionId = status.RootElement.GetProperty("Result").GetProperty("SessionId").GetString()!;
using var time = await SendAsync(root,
    new { ProtocolVersion = "0.1", Command = "runtime.execute",
          Arguments = new Dictionary<string, string> { ["operation"] = "time.read", ["session_id"] = sessionId } },
    TimeSpan.FromSeconds(8));
Console.WriteLine(time.RootElement.GetProperty("Result").GetProperty("Values"));
```

Анонимные объекты сериализуются в `ProtocolVersion`/`Command`/`Arguments` в точности так, как ожидает владелец. `TimeoutException`/`OperationCanceledException` при вызове `ConnectAsync` означает, что для этого корневого каталога владельца нет.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` и `Convert.ToHexString` требуют .NET 5 или новее, поэтому для этого примера нужен PowerShell 7; Windows PowerShell 5.1 (.NET Framework) не может задать `CurrentUserOnly`.

```powershell
$root = 'D:\OMSI 2'
$normalized = [IO.Path]::GetFullPath($root).TrimEnd('\').ToUpperInvariant()
$key = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized)))
$pipeName = 'OmsiLaunch.Control.0.1.' + $key.Substring(0, 16)

function Send-OmsiLaunchControl([string] $Json, [int] $TimeoutMilliseconds = 750) {
    $pipe = [IO.Pipes.NamedPipeClientStream]::new('.', $pipeName, [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::CurrentUserOnly)
    try {
        $pipe.Connect($TimeoutMilliseconds)
        $bytes = [Text.Encoding]::UTF8.GetBytes($Json)
        if ($bytes.Length -gt 65536) { throw 'OL_E_CONTROL_MESSAGE_TOO_LARGE' }
        $pipe.Write([BitConverter]::GetBytes([int] $bytes.Length), 0, 4)
        $pipe.Write($bytes, 0, $bytes.Length)
        $pipe.Flush()
        $lengthBytes = [byte[]]::new(4); $read = 0
        while ($read -lt 4) { $n = $pipe.Read($lengthBytes, $read, 4 - $read); if ($n -eq 0) { throw 'OL_E_CONTROL_PROTOCOL: the owner closed the connection without a reply' }; $read += $n }
        $length = [BitConverter]::ToInt32($lengthBytes, 0)
        $buffer = [byte[]]::new($length); $read = 0
        while ($read -lt $length) { $n = $pipe.Read($buffer, $read, $length - $read); if ($n -eq 0) { throw 'OL_E_CONTROL_PROTOCOL: truncated reply' }; $read += $n }
        [Text.Encoding]::UTF8.GetString($buffer) | ConvertFrom-Json
    }
    finally { $pipe.Dispose() }
}

$status = Send-OmsiLaunchControl '{"ProtocolVersion":"0.1","Command":"session.status"}'
$sessionId = $status.Result.SessionId
$request = @{ ProtocolVersion = '0.1'; Command = 'runtime.execute'; Arguments = @{ operation = 'time.read'; session_id = $sessionId } } | ConvertTo-Json -Compress
Send-OmsiLaunchControl $request 8000
```

`ConvertTo-Json` сохраняет регистр ключей хеш-таблицы, поэтому `ProtocolVersion`, `Command` и `Arguments` выводятся в том виде, в каком написаны.

<a id="using-the-cli-as-the-client"></a>
### Использование CLI в качестве клиента

Если собственный клиент не нужен, вызывайте `OmsiLaunch.exe` из каталога установки: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. CLI выполняет рукопожатие status/bind и сопоставляет ответы с [кодами выхода](exit-codes.md) (`0` — успех, `2` — ошибка проверки аргументов, `4` — нет владельца, `7` — отклонено).

<a id="relationship-to-other-channels"></a>
## Связь с другими каналами

- Runtime-почтовый ящик между владельцем и плагином внутри процесса OMSI (отображаемая в память область, 64 KiB, single-flight, привязка к сеансу) — это отдельный закрытый канал; плоскость управления лишь передаёт в него запросы через `IOmsiLaunch.ExecuteRuntimeAsync`.
- Аренда установки (`Local\OmsiLaunch.Installation.<sha256(root)>`) — это именованный семафор, не являющийся частью этого протокола; она гарантирует одного владельца на установку в пределах сеанса входа в систему.
- Пункт «End session» (завершить сеанс) значка в трее и команда `session.stop` плоскости управления передают один и тот же запрос остановки на стороне владельца; см. [трей Windows](windows-tray.md).
