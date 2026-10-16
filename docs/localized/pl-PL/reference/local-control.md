# Lokalna płaszczyzna sterowania

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/local-control.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Lokalna płaszczyzna sterowania to punkt końcowy oparty na potoku nazwanym (named pipe), przez który działający właściciel OmsiLaunch (proces, który uruchomił sesję) przyjmuje semantyczne polecenia sesji od innych procesów na tym samym komputerze: stan, zdarzenia, zatrzymanie i publiczne operacje runtime. Ta strona specyfikuje punkt końcowy w postaci zaimplementowanej w `tools\OmsiLaunch.Cli\LocalControlPlane.cs` oraz procedurę obsługi żądań w `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): nazewnictwo potoku, ramkowanie, wersję protokołu, polecenia, powiązanie z sesją, kody błędów, model zaufania, okno dostępności oraz sposób komunikacji z nim z innych narzędzi. Polecenia klienta CLI (`session status`, `session stop`, `events read`, `events watch`, ścieżki poleceń takie jak `time get`) są cienkimi nakładkami na ten protokół; patrz [dokumentacja CLI](cli.md). Same operacje runtime są wyspecyfikowane w [sterowaniu runtime](runtime-control.md); alternatywą działającą w procesie integratora jest [publiczne API](public-api.md).

<a id="summary"></a>
## Podsumowanie

| Właściwość | Wartość |
|---|---|
| Transport | Potok nazwany Windows, `PipeDirection.InOut`, tryb bajtowy, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, bufory wejściowy i wyjściowy o rozmiarze jednej ramki (65,540 bajtów) |
| Nazwa potoku | `OmsiLaunch.Control.0.1.<key>`, gdzie `<key>` to pierwsze 16 znaków szesnastkowych skrótu SHA-256 z bajtów UTF-8 pełnej ścieżki katalogu głównego instalacji, z usuniętym końcowym `\`, zapisanych wielkimi literami (`LocalControlPlane.PipeNameFor`) |
| Ramkowanie | 4-bajtowy prefiks długości `int32` little-endian, a po nim tyle bajtów JSON w UTF-8; jedno żądanie i jedna odpowiedź na połączenie |
| Maksymalny rozmiar komunikatu | 65,536 bajtów dla żądania i dla odpowiedzi (`MaxMessageBytes`) |
| Wersja protokołu | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); na niezgodność odpowiada się kodem `OL_E_CONTROL_PROTOCOL` |
| Polecenia | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Powiązanie z sesją | `session.stop` i `runtime.execute` wymagają, aby `Arguments.session_id` był równy identyfikatorowi aktywnej sesji |
| Dostępność | Od chwili, w której sesja osiągnęła `Running` (po ewentualnym wsadzie weryfikacyjnym), do chwili, gdy sesja osiągnie `Completed` lub `Failed`, a właściciel zwolni punkt końcowy |
| Zakres | Jeden punkt końcowy na katalog główny instalacji; dwie instalacje tego samego użytkownika nigdy nie współdzielą potoku |
| Stabilność | STABLE_BETA (testy offline w `OmsiLaunch.WindowsUiTests`; dowody z runtime: RV-003 – równoczesne odczyty stanu podczas trwającego kilka sekund wywołania natywnego, runda A RA-008 oraz zestaw testów surowych ramek z domknięcia runtime `R04`: 17 zniekształconych, zbyt dużych, z niewłaściwym protokołem, z nieznanym poleceniem, niepowiązanych i z niewłaściwą sesją ramek, na które odpowiedziano właściwym typowanym błędem, a po każdej z nich udanym `session.status`; zablokowany klient nie blokuje innych; powiązane zatrzymanie w trakcie trwającego tworzenia pojazdu kończy sesję) |

<a id="pipe-name-derivation"></a>
## Wyznaczanie nazwy potoku

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` jest jedyną definicją tożsamości instalacji, używaną także przez dzierżawę instalacji: rozwiązuje pełną ścieżkę (w tym segmenty `.` i `..`, `/` lub `\`, powtórzone separatory) i usuwa końcowe separatory, z wyjątkiem katalogu głównego dysku. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` i `d:\omsi 2` dają tę samą nazwę; `D:\OMSI 2` i `E:\OMSI 2` dają różne nazwy. Klient zawsze wyznacza nazwę na podstawie instalacji zawierającej uruchamiany przez niego plik wykonywalny (`AppContext.BaseDirectory`), chyba że wywołujący API przekaże jawnie katalog główny. `0.1` w nazwie to wersja protokołu, dzięki czemu przyszły protokół może współistnieć na tym samym komputerze.

<a id="framing-and-encoding"></a>
## Ramkowanie i kodowanie

- Zapis: należy zserializować rekord za pomocą `System.Text.Json` z opcjami domyślnymi, sprawdzić `length <= 65536` (w przeciwnym razie `OL_E_CONTROL_MESSAGE_TOO_LARGE`), zapisać `BitConverter.GetBytes((int)length)` (4 bajty, little-endian w Windows), zapisać ładunek i opróżnić bufor.
- Odczyt: należy odczytać dokładnie 4 bajty; odrzucić `length < 0` lub `length > 65536` z `OL_E_CONTROL_MESSAGE_INVALID`; odczytać dokładnie `length` bajtów; zdeserializować. Połączenie zamknięte przed odczytaniem pełnej ramki jest przez właściciela po cichu porzucane (bez odpowiedzi). Na odrzuconą ramkę udzielana jest odpowiedź nawet wtedy, gdy klient zapisał już więcej bajtów, niż odczytuje właściciel: bufory potoku mieszczą całą ramkę, więc zapis klienta się kończy i klient może odczytać odpowiedź (domknięcie runtime BUG-02; przed poprawką taki klient i właściciel blokowali się obaj na swoich zapisach).
- Właściciel zapisuje każdą odpowiedź z terminem 5 s: klient, który się połączy, wyśle żądanie i nigdy nie odczyta odpowiedzi, nie może zajmować zadania właściciela dłużej.
- Nazwy właściwości w żądaniu są zapisywane w **PascalCase i rozróżniają wielkość liter** (`ProtocolVersion`, `Command`, `Arguments`), ponieważ właściciel deserializuje z opcjami domyślnymi. Odpowiedzi używają `Ok`, `Result`, `ErrorCode`, `Message`. Wartości wyliczeń są serializowane jako liczby całkowite; wartości `Guid` jako ciągi znaków.
- Jedno żądanie na połączenie: właściciel odczytuje jedno żądanie, zapisuje jedną odpowiedź i zamyka potok. Dla każdego żądania należy otworzyć nowe połączenie.

<a id="request"></a>
### Żądanie

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` musi być identyfikatorem zwróconym przez `session.status` dla działającej sesji (powyższa wartość pochodzi z rzeczywistej sesji).

`Arguments` jest opcjonalne (`null` lub pominięte) dla `session.status` i `session.events`. Klucze argumentów są porównywane porządkowo (z rozróżnianiem wielkości liter).

<a id="response"></a>
### Odpowiedź

Ramki przechwycone z rzeczywistego właściciela (domknięcie runtime `R04`, pakiet końcowy). Udane `runtime.execute`:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Odrzucone `session.stop` bez aktywnego `session_id`:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Polecenia

| Polecenie | Argumenty | Wynik przy `Ok=true` | Uwagi |
|---|---|---|---|
| `session.status` | brak | `SessionStatus`: `SessionId` (GUID jako ciąg znaków), `State` (liczba całkowita `SessionState`; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (tablica `{Code, Message, Data}`), `RuntimeEvents` (tablica) | Tylko do odczytu. Klienci odczytują je jako pierwsze, aby uzyskać `SessionId`. |
| `session.events` | brak | Tablica `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (monotoniczny `int64`), `Data` (mapa ciągów znaków) | Tylko do odczytu, lista ograniczona; `events watch` odpytuje je i wypisuje wpisy z `Sequence` większym niż ostatnio widziany. |
| `session.stop` | `session_id` (wymagany) | `{"accepted": true, "session_id": "..."}` | Żąda kanonicznego zatrzymania (`TerminateProcess` na OMSI, a następnie przywracanie). Zwraca sterowanie natychmiast; stan sesji można obserwować przez `session.status`, dopóki punkt końcowy nie zniknie. |
| `runtime.execute` | `operation` (wymagany), `session_id` (wymagany) oraz własne argumenty operacji (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (przydzielany przez właściciela, począwszy od `50001`), `Succeeded`, `ErrorCode`, `Values` (mapa ciągów znaków) | Weryfikowane za pomocą `PublicCapabilityRegistry.ValidateRuntimeArguments` przed wykonaniem: nieznana lub wewnętrzna operacja → `OL_E_RUNTIME_OPERATION_UNKNOWN`; brak wymaganej wartości → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Limit czasu 8 s, 30 s dla `road-vehicles.spawn`. Na niepowodzenie runtime odpowiada się `Ok=false` z `Result` ustawionym na `RuntimeCommandResult`, `ErrorCode` na jego kod i `Message` równym `Runtime operation was rejected.` Klucze wyniku zaczynające się od `internal_` lub kończące się na `_address`, `_pointer`, `_vmt` są usuwane przez API, zanim trafią do potoku. |
| cokolwiek innego | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

Właściciel obsługuje połączenia współbieżnie (każdy przyjęty klient jest obsługiwany we własnym zadaniu), więc `session.status` nadal odpowiada, gdy trwa kilkusekundowe wywołanie natywne, takie jak `road-vehicles.spawn`.

<a id="session-id-binding"></a>
## Powiązanie z identyfikatorem sesji

Polecenia modyfikujące muszą wskazywać sesję, na której działają. CLI implementuje `TryRequestBoundAsync`: wysyła `session.status` (750 ms), pobiera `Result.SessionId` i powtarza żądanie z `session_id` dodanym do `Arguments`. Na brakujący, niemożliwy do sparsowania lub inny identyfikator odpowiada się kodem `OL_E_CONTROL_SESSION_MISMATCH`. Jeśli właściciel nie zgłosi identyfikatora sesji, klient zgłasza `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Metadane odpowiedzi i obcięta historia zdarzeń

`Metadata` ma wartość `null`, chyba że odpowiedź zawiera informację o sobie samej. `session.status` i `session.events` zwracają historię zdarzeń właściciela (najwyżej 256 zdarzeń, najnowsze na końcu). Gdy ta historia nie mieści się w jednej ramce 64 KiB, najstarsze zdarzenia są usuwane, aż się zmieści, a `Metadata` to odnotowuje:

| Klucz | Znaczenie |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Zdarzenia w tej odpowiedzi |
| `events_dropped_count` | Najstarsze zdarzenia usunięte, aby zmieścić się w ramce |
| `events_available_count` | Zdarzenia, które właściciel miał dla tej odpowiedzi |
| `events_first_returned_sequence` | `Sequence` pierwszego zwróconego zdarzenia |

Bez `Metadata` odpowiedź nie jest obcięta przez płaszczyznę sterowania. `Sequence` jest monotoniczny w obu przypadkach, więc klient może też wykryć zdarzenia, które sam właściciel usunął poza swoją 256-elementową historię. CLI wypisuje te metadane w kopercie `--json`, a w trybie tekstowym – uwagę.

<a id="error-codes"></a>
## Kody błędów

| Kod | Pochodzenie | Znaczenie |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | właściciel / klient | `ProtocolVersion` żądania nie jest `0.1`; odpowiedzi właściciela nie udało się zdekodować lub była pusta; właściciel zamknął połączenie bez odpowiedzi; połączenie zostało zerwane po nawiązaniu; albo właściciel nie zgłosił identyfikatora sesji. |
| `OL_E_CONTROL_MESSAGE_INVALID` | właściciel / klient | Prefiks długości poza zakresem (w ten sposób zbyt duża ramka żądania jest odrzucana przed odczytaniem jej ładunku), pusta ramka, JSON `null`, żądanie bez `Command` lub JSON, którego nie udało się zdekodować. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | zapis po stronie klienta | Własne żądanie klienta przekracza 65,536 bajtów; jest to zgłaszane klientowi i nic nie jest wysyłane. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | zapis po stronie właściciela | Odpowiedź właściciela przekracza 65,536 bajtów; właściciel odpowiada tym typowanym błędem. `session.status` i `session.events` przycinają swoją historię zdarzeń (od najstarszych), aby się zmieścić, więc go nie generują. |
| `OL_E_CONTROL_SESSION_MISMATCH` | właściciel | `session.stop` / `runtime.execute` bez aktywnego `session_id`. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | właściciel | Nieobsługiwana nazwa polecenia. |
| `OL_E_CONTROL_HANDLER_FAILED` | właściciel | Odpowiedzi procedury obsługi nie udało się zserializować albo procedura obsługi zgłosiła wyjątek bez kodu `OL_E_` w komunikacie; jeśli kod jest obecny, zwracany jest zamiast tego ten kod (na przykład `OL_E_SESSION_NOT_RUNNING` z `ExecuteRuntimeAsync` po zakończeniu OMSI). |
| `OL_E_CONTROL_FAILED` | klient CLI | Kod domyślny, gdy odpowiedź ma `Ok=false` bez `ErrorCode`. |
| `OL_E_TIMEOUT` | klient | Klient był połączony z właścicielem, który nie odpowiedział w ramach limitu czasu. |
| `OL_E_NO_ACTIVE_SESSION` | klient CLI | Nie udało się osiągnąć żadnego punktu końcowego właściciela (`TryRequestAsync` zwraca `null` tylko wtedy, gdy nie powiedzie się samo połączenie). Po nawiązaniu połączenia każde niepowodzenie jest jednym z powyższych typowanych kodów. Wyjście `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | klient i właściciel | Weryfikacja `runtime.execute` względem powierzchni publicznej (wyjście `2` w CLI). |

Na każde znane niepowodzenie odpowiada się typowanym błędem, więc klient nigdy nie pomyli awarii z brakiem właściciela. Dwa przypadki kończą się bez ramki odpowiedzi: klient, który rozłącza się przed wysłaniem pełnego żądania (nie ma komu odpowiedzieć), oraz żądanie wciąż w toku w chwili zakończenia sesji (właściciel anuluje oczekujące odpowiedzi podczas zamykania punktu końcowego; klient widzi koniec strumienia, który klient CLI zgłasza jako `OL_E_CONTROL_PROTOCOL` „The owner closed the connection without a reply”). Klient, który wysłał żądanie i rozłącza się przed odczytaniem odpowiedzi, nie wpływa na właściciela. Uruchomienie właściciela odmawia kontynuacji, gdy jakakolwiek odpowiedź (w tym typowany błąd) nadejdzie z punktu końcowego instalacji, ponieważ odpowiedź dowodzi obecności właściciela. CLI mapuje odpowiedzi na kody wyjścia zgodnie z opisem w [kodach wyjścia](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Model zaufania (zaakceptowane ryzyko)

- `PipeOptions.CurrentUserOnly` ogranicza potok do użytkownika Windows (i poziomu integralności), który jest właścicielem sesji; ta sama opcja po stronie klienta weryfikuje, czy serwer należy do tego samego użytkownika.
- **Każdy proces działający jako ten sam użytkownik Windows może odczytywać stan, zatrzymać sesję i wykonywać publiczne operacje runtime.** Nie ma dodatkowego uwierzytelniania, tokenu ani autoryzacji poszczególnych klientów. Jest to udokumentowane, zaakceptowane ryzyko dla wersji beta; nie należy uruchamiać sesji OmsiLaunch na koncie współdzielonym z niezaufanym oprogramowaniem.
- Potok nigdy nie udostępnia adresów natywnych, uchwytów ani surowych operacji na pamięci; akceptowane są wyłącznie publiczne identyfikatory operacji z `PublicCapabilityRegistry`, a wewnętrzne prymitywy badawcze (`internal.*`) są odrzucane przed jakimkolwiek wyszukaniem sesji.
- Komunikaty są ograniczone (64 KiB) i ramkowane; na zniekształconą lub zbyt dużą ramkę udzielana jest odpowiedź albo jest ona porzucana bez wpływu na sesję.
- Jeśli nazwa potoku należy już do innego procesu w chwili, gdy właściciel zaczyna nasłuchiwać (`IOException`/`UnauthorizedAccessException` przy tworzeniu), właściciel kontynuuje sesję bez punktu końcowego i zapisuje błąd w `LocalControlPlane.ListenFault`; klienci widzą wtedy `OL_E_NO_ACTIVE_SESSION`. Taką sesję należy zakończyć za pomocą ikony w obszarze powiadomień lub Ctrl+C.

<a id="availability-window"></a>
## Okno dostępności

1. Działa `StartSessionAsync`; punkt końcowy jeszcze nie istnieje. Drugie uruchomienie `OmsiLaunch.exe` dla tego samego katalogu głównego przed startem sprawdza `session.status` przez 250 ms i kończy się niepowodzeniem z `OL_E_SESSION_ALREADY_ACTIVE` tylko wtedy, gdy właściciel już odpowiada; dwóch właścicieli rywalizujących podczas uruchamiania jest szeregowanych przez dzierżawę instalacji (`OL_E_INSTALLATION_BUSY`).
2. Sesja osiąga `Running`; wsady weryfikacyjne (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), jeśli ich zażądano, kończą się.
3. `LocalControlPlane.Start()`: punkt końcowy przyjmuje połączenia. Własna operacja `/runtime:` właściciela, jeśli jest, wykonywana jest po tym momencie.
4. Punkt końcowy pozostaje dostępny w stanach `ProcessExited`, `Restoring` i `CleaningRuntime` (stan zgłasza te stany), dopóki sesja nie osiągnie `Completed` lub `Failed`.
5. `DisposeAsync` anuluje nasłuchiwanie, czeka na zakończenie trwających zadań klientów, a właściciel wywołuje `CloseAsync`. Po tym nazwa potoku przestaje istnieć.

Klienci powinni używać krótkich limitów czasu połączenia (CLI używa 750 ms dla stanu/zatrzymania/zdarzeń) i traktować „brak punktu końcowego” jako „brak aktywnej sesji dla tej instalacji”.

<a id="using-the-protocol-from-other-tooling"></a>
## Korzystanie z protokołu z innych narzędzi

<a id="c-net-6-or-later"></a>
### C# (.NET 6 lub nowszy)

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

Obiekty anonimowe są serializowane do `ProtocolVersion`/`Command`/`Arguments` dokładnie tak, jak oczekuje właściciel. `TimeoutException`/`OperationCanceledException` przy `ConnectAsync` oznacza, że dla danego katalogu głównego nie ma właściciela.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` i `Convert.ToHexString` wymagają .NET 5 lub nowszego, dlatego ten przykład wymaga PowerShell 7; Windows PowerShell 5.1 (.NET Framework) nie może ustawić `CurrentUserOnly`.

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

`ConvertTo-Json` zachowuje wielkość liter kluczy tablicy mieszającej, więc `ProtocolVersion`, `Command` i `Arguments` są emitowane dokładnie tak, jak zostały zapisane.

<a id="using-the-cli-as-the-client"></a>
### Użycie CLI jako klienta

Gdy własny klient nie jest potrzebny, należy wywołać `OmsiLaunch.exe` z katalogu instalacji: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. CLI wykonuje uzgadnianie stanu/powiązania i mapuje odpowiedzi na [kody wyjścia](exit-codes.md) (`0` powodzenie, `2` weryfikacja argumentów, `4` brak właściciela, `7` odrzucenie).

<a id="relationship-to-other-channels"></a>
## Relacja z innymi kanałami

- Skrzynka runtime między właścicielem a wtyczką działającą w procesie OMSI (mapowana w pamięci, 64 KiB, jedno żądanie naraz, powiązana z sesją) jest odrębnym, prywatnym kanałem; płaszczyzna sterowania jedynie przekazuje do niej żądania przez `IOmsiLaunch.ExecuteRuntimeAsync`.
- Dzierżawa instalacji (`Local\OmsiLaunch.Installation.<sha256(root)>`) jest nazwanym semaforem, a nie częścią tego protokołu; gwarantuje jednego właściciela na instalację w ramach sesji logowania.
- Pozycja „End session” ikony w obszarze powiadomień (`Zakończ sesję` w polskim interfejsie) i `session.stop` płaszczyzny sterowania sygnalizują to samo żądanie zatrzymania po stronie właściciela; patrz [ikona w obszarze powiadomień Windows](windows-tray.md).
