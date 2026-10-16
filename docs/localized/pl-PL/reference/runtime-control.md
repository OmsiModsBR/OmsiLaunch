# Sterowanie runtime

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/runtime-control.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Sterowanie runtime to zestaw operacji odczytu, zapisu i akcji, które OmsiLaunch wykonuje wewnątrz działającej sesji OMSI. Ta strona wyjaśnia trzy sposoby dostępu do niego (API właściciela, klient CLI, lokalna płaszczyzna sterowania), drogę żądań od wywołującego do wtyczki i z powrotem, działanie uchwytów i identyfikatorów żądań, obowiązujące limity czasu, to, co celowo nie jest zapisywane w dzienniku, oraz konwencje argumentów i wyników, z przykładem dla każdej rodziny operacji. Sam spis operacji, z argumentami, kluczami wyniku i błędami, znajduje się w opisie [możliwości](capabilities.md). Źródła: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` i `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` i `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Trzy punkty wejścia

| Punkt wejścia | Kto | Ścieżka | Limit czasu |
| --- | --- | --- | --- |
| API właściciela | Integrator, który uruchomił sesję w swoim procesie za pomocą `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> weryfikacja w rejestrze -> wyszukanie sesji -> skrzynka runtime | `TimeSpan` podany przez wywołującego |
| CLI właściciela (`/runtime:<op>`) | Proces `OmsiLaunch.exe`, który jest właścicielem sesji | jedna operacja wykonywana tuż po osiągnięciu `Running`, wynik zapisywany do `.omsilaunch\diagnostics\<session>-runtime-operation.json` i na konsolę; sesja trwa dalej | 5 s (15 s dla `road-vehicles.spawn`) |
| Klient CLI | Dowolne wywołanie `OmsiLaunch.exe` **bez** argumentu instalacji, na przykład `OmsiLaunch.exe time get` | potok nazwany `runtime.execute` do właściciela instalacji, w której znajduje się plik wykonywalny -> właściciel wywołuje `ExecuteRuntimeAsync` | 8 s (30 s dla `road-vehicles.spawn`) zarówno po stronie klienta, jak i właściciela |
| Lokalna płaszczyzna sterowania | Dowolny proces tego samego użytkownika Windows | ten sam protokół potoku co klient CLI; patrz [lokalne sterowanie](local-control.md) | jak wyżej |

Każda ścieżka kończy się w `ExecuteRuntimeAsync`, które egzekwuje granicę publiczną w następującej kolejności:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: operacja spoza `PublicRuntimeOperationIds` (w tym każda operacja `internal.*`) zwraca `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; brakujący lub pusty wymagany argument zwraca `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Żaden z tych przypadków nie dotyka sesji.
2. Wyszukanie sesji (`KeyNotFoundException` dla nieznanego uchwytu), `OL_E_RUNTIME_SESSION_MISMATCH`, gdy `RuntimeCommand.SessionId` różni się od uchwytu, `OL_E_SESSION_NOT_RUNNING`, jeśli stan nie jest `Running`.
3. Żądanie do skrzynki runtime (poniżej), a następnie `ScrubInternalValues` usuwa każdy klucz wyniku zaczynający się od `internal_` lub kończący się na `_address`, `_pointer`, `_vmt`.

Klient CLI i płaszczyzna sterowania same wykonują krok 1 przed skontaktowaniem się z właścicielem, dlatego nieprawidłowa nazwa polecenia jest zgłaszana jako kod wyjścia 2 nawet wtedy, gdy żadna sesja nie jest aktywna (`OL_E_RUNTIME_OPERATION_UNKNOWN` i `OL_E_RUNTIME_ARGUMENT_REQUIRED` są mapowane na `InvalidArguments`; inne odrzucenia na 7; brak właściciela na 4).

Punkt końcowy płaszczyzny sterowania istnieje tylko wtedy, gdy właściciel jest w stanie `Running`, po uruchomieniu i po zakończeniu każdej uprzęży testowej `/runtime-batch`, `/runtime-write-batch` lub `/d3d-batch`. Polecenia modyfikujące (`runtime.execute`, `session.stop`) muszą zawierać aktywny `session_id`; CLI pobiera go automatycznie z `session.status` (w przeciwnym razie `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Tożsamość żądania i skrzynka runtime

`RuntimeCommand(SessionId, RequestId, Operation, Arguments)` jest serializowany do koperty `RuntimeCommandWire` (sygnatura `OLRC`, wersja 1, 72-bajtowy nagłówek, SHA-256 ładunku JSON) i umieszczany w 64 KiB skrzynce runtime sesji obsługującej jedno żądanie naraz (`OmsiLaunch.Runtime.<sessionId>`). Wtyczka odpytuje skrzynkę co 50 ms w wątku interfejsu użytkownika OMSI, tam wykonuje operację i zapisuje odpowiedź.

Reguły zapewniające odporność kanału:

| Reguła | Skutek |
| --- | --- |
| Jedno żądanie naraz | Jedno żądanie jednocześnie na sesję; host szereguje wywołujących za pomocą semafora. Slot, który nadal jest w stanie `requested`, gdy nadchodzi nowe żądanie, daje `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Powiązanie z sesją | Wtyczka ignoruje (i czyści) żądanie, którego GUID sesji nie jest jej własnym; host odrzuca odpowiedź, której identyfikator sesji lub żądania nie pasuje (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Limit czasu | Po upływie terminu host przywraca slot do stanu bezczynności i zgłasza `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Spóźniona odpowiedź | Wtyczka publikuje odpowiedź tylko wtedy, gdy slot nadal przechowuje identyfikator jej żądania; odpowiedź na porzucone żądanie jest odrzucana. Jeśli mimo to taka odpowiedź trafi do slotu, następne żądanie hosta znajduje nieaktualny slot w stanie `responded`, odrzuca go i kontynuuje; jeśli ta nieaktualna odpowiedź ma **ten sam** identyfikator żądania co nowe żądanie, host zgłasza `OL_E_RUNTIME_REQUEST_ID_REUSED`. Wywołujący nie mogą więc nigdy ponownie używać identyfikatora żądania w obrębie sesji. |
| Rozmiar | Żądanie większe niż slot jest odrzucane przed umieszczeniem w skrzynce (`ArgumentOutOfRangeException`). Strona listy, która się nie mieści, jest skracana przez wtyczkę (ostatnie wiersze są odrzucane, `truncated=true`, mniejsza wartość `returned_count`, `next_cursor` na pierwszym odrzuconym wierszu); każda inna zbyt duża odpowiedź jest zastępowana typowanym błędem `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Kanał zamknięty | Po zakończeniu sesji: `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Identyfikatory żądań są wartościami `ulong` wybieranymi przez wywołującego. CLI używa stałych zakresów: `10_001` dla `/runtime:`, `50_001+` dla żądań przekazywanych przez płaszczyznę sterowania, `1+` dla wsadu odczytów, `20_000+` dla wsadu D3D, `30_000+` dla `D3DRuntimeApi`. Integratorzy powinni używać monotonicznie rosnącego licznika dla każdej sesji.

<a id="handles-and-stale-detection"></a>
## Uchwyty i wykrywanie nieaktualności

| Prefiks | Typ | Wydawany przez | Format |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + sześć cyfr dziesiętnych (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + sześć cyfr dziesiętnych |
| `d3dtex-` | Tekstura D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Uchwyty są nieprzezroczyste i mają zakres sesji: wydaje je wtyczka, nigdy nie kodują adresu i nie mają znaczenia w innej sesji. Przy rozwiązywaniu uchwytu wtyczka sprawdza, czy adres, na który on wskazuje, nadal znajduje się w aktywnej kolekcji OMSI (według ostatniego odczytu listy; `road-vehicles.list` i `humans.list` odświeżają ten widok), i ponownie odczytuje odcisk obiektu (VMT Delphi plus wskaźnik definicji pojazdu albo indeks modelu postaci). Niezgodność oznacza, że obiekt natywny został zniszczony, a jego adres ponownie użyty: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Dla tego samego aktywnego obiektu przy kolejnych odczytach listy zwracany jest ten sam token. Pozostały martwy punkt: obiektu tej samej klasy i definicji, ponownie utworzonego pod tym samym adresem między dwoma odczytami listy, nie da się odróżnić. Uchwyty D3D weryfikuje mostek natywny: nieznany uchwyt lub uchwyt z innej sesji daje `OL_E_D3D_STALE_RESOURCE_HANDLE`, zwolniony – `OL_E_D3D_RESOURCE_RELEASED`, a zmiana generacji urządzenia oznacza tekstury jako `STALE`.

Uchwyty nigdy nie są adresami natywnymi i nigdy nie należy ich parsować, porównywać liczbowo, utrwalać między sesjami ani przekazywać do innej sesji: należy je traktować jako nieprzezroczyste ciągi znaków ważne w sesji, która je wydała.

Dowody z runtime: zwolniony uchwyt D3D pozostaje odrzucany po utworzeniu nowych tekstur, a uchwyt z poprzedniej sesji jest odrzucany przez następną (domknięcie runtime `H02`); reset urządzenia D3D unieważnia każdą aktywną teksturę (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). Wykrywanie nieaktualności uchwytów RoadVehicle i Human po naturalnym usunięciu (RV-002) nie ma bezpiecznego źródła w runtime (OMSI nie usuwał obiektów w oknach obserwacji, a żadna publiczna operacja nie usuwa obiektu); jest pokryte testami offline.

<a id="what-is-not-journaled"></a>
## Czego nie zapisuje się w dzienniku

Mutacje runtime zmieniają wyłącznie pamięć OMSI. **Nie są rejestrowane w dzienniku transakcji i nie są przywracane** po zakończeniu sesji: `time.set`, `camera.set`, `camera.lock` (polityka przestaje działać, gdy wtyczka się zamyka), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` oraz każdy zasób `d3d.texture.*`. Znikają razem z procesem OMSI, który OmsiLaunch kończy na koniec sesji, nie pozwalając OMSI niczego utrwalić (patrz [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md)). Nic w rodzinie runtime nie dotyka systemu plików.

<a id="argument-and-result-conventions"></a>
## Konwencje argumentów i wyników

- Argumenty są parami klucz/wartość w postaci ciągów znaków. W CLI `--key=value` po słowach polecenia staje się argumentem runtime (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` jest odpowiednikiem dla `/runtime:<op>`. Liczby używają kultury niezmiennej (separator dziesiętny `.`); wartości logiczne to `true`/`false`.
- Słowa polecenia są mapowane na identyfikatory operacji za pomocą `CliInput.HierarchicalRoutes` (na przykład `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). Pełna tabela ścieżek poleceń znajduje się w [dokumentacji CLI](cli.md). Operacje D3D nie mają ścieżki opartej na słowach polecenia; należy użyć `/runtime:d3d.status` lub `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Wyniki są płaskimi słownikami ciągów znaków. Listy używają kluczy `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) z polami strony `count`, `returned_count`, `truncated` i `next_cursor` (zob. [paginacja](capabilities.md#pagination)).
- Niepowodzenia zawierają `Succeeded=false`, kod `ErrorCode` z prefiksem `OL_E_` oraz, w przypadku niepowodzeń po stronie wtyczki, `detail` (a także `native_status` dla D3D i `exception` dla nieoczekiwanych błędów).

<a id="cli-envelope---json"></a>
### Koperta CLI (`--json`)

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

Koperta błędu ma postać `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Bez `--json` CLI wypisuje obiekt wyniku jako JSON z wcięciami lub jako `CODE: message`.

<a id="api-envelope"></a>
### Koperta API

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` zgłasza wyjątek przy naruszeniach granicy wykrytych po weryfikacji w rejestrze (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) i zwraca `Succeeded=false` dla odrzuceń przez rejestr oraz błędów po stronie wtyczki.

<a id="worked-examples"></a>
## Przykłady

Wszystkie przykłady CLI zakładają, że dla instalacji zawierającej `OmsiLaunch.exe` działa właściciel (uruchomiony na przykład poleceniem `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1` albo `OmsiLaunch.exe "/saved:situations\Linie 5.osn"`, gdy przykład wymaga pojazdu gracza), i są uruchamiane z drugiej konsoli w tej samej instalacji.

| Rodzina | Polecenie | Działanie |
| --- | --- | --- |
| Sesja | `OmsiLaunch.exe session status --json` | Odczytuje `SessionId`, `State`, komunikaty diagnostyczne i ograniczoną listę zdarzeń runtime. |
| Czas | `OmsiLaunch.exe time get`, a następnie `OmsiLaunch.exe time set --hour=7 --minute=30` | Odczytuje zegar; ustawia go przez profilowane `SetTime` i zwraca odczyt kontrolny. |
| Pogoda | `OmsiLaunch.exe weather get` i `OmsiLaunch.exe weather actual get` | Odczytuje bieżący stan pogody oraz stan pogody rzeczywistej/ICAO. `OmsiLaunch.exe weather set --wind_speed=5` ustawia pogodę ręczną (źródłowy rekord OMSI; bieżący METAR zostaje zatrzymany), a `OmsiLaunch.exe weather metar set --icao=EDDF` przełącza na bieżącą pogodę METAR. Obie są EXPERIMENTAL do czasu weryfikacji w runtime. |
| Mapa | `OmsiLaunch.exe map get` | Tożsamość mapy, nazwa, opis, liczba kafelków, zakres lat i strona ruchu. |
| Kamera | `OmsiLaunch.exe camera set --field_of_view=50`, a następnie `OmsiLaunch.exe camera lock --family=0 --preset=1` i `OmsiLaunch.exe camera unlock` | Zapisuje FOV; przypina rodzinę kierowcy z ustawieniem wstępnym 1 (wymaga pojazdu gracza, na przykład z zapisanej sytuacji); zwalnia politykę. |
| Pojazdy | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Liczby; uchwyty; jedna migawka. |
| Utworzenie pojazdu | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Tworzy jeden pojazd RoadVehicle sterowany przez AI (limit czasu 30 s); zwraca `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` wywołuje `PlaceRandomBus`. |
| Gracz | `OmsiLaunch.exe player get` | `present=false` przy uruchomieniu bez pojazdu gracza (headless), w przeciwnym razie migawka gracza. |
| Postacie | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Liczby; uchwyty; jedna migawka. |
| Rozkład jazdy | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Liczby menedżera; ograniczone wiersze tras; dzienniki rozkładu jazdy. |
| Skrypty | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Lista/odczyt/zapis zmiennych liczbowych; odczyt/zapis zmiennej tekstowej; wyzwalacze skryptu i dźwięku (zapisy tekstowe i wyzwalacze są EXPERIMENTAL do czasu weryfikacji w runtime). |
| Stałe i krzywe | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Stałe pojazdu i obliczanie wartości krzywej. Nazwy zależą od modelu: należy używać nazw zwróconych przez polecenie `list` (te zostały wypisane dla autobusu gracza z `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Metadane HOF definicji pojazdu. |
| Kierowcy i bilety | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Rekordy kierowców; pakiet biletów. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`, a następnie `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` i `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Cykl życia tekstury w wątku renderowania; `<HANDLE>` to wartość `handle` wypisana przez `create`; `--pixels_base64` po zdekodowaniu musi mieć `width * height * 4` bajtów (formaty 32-bitowe) i najwyżej 48 KiB. |
| Zdarzenia | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Ograniczona lista zdarzeń; obserwacja przez odpytywanie (250 ms) aż do Ctrl+C. |
| Zatrzymanie | `OmsiLaunch.exe session stop` | Żąda kanonicznego zatrzymania: właściciel kończy OMSI i przywraca transakcję. |

<a id="stability"></a>
## Stabilność

Sam kanał (`runtime.command-channel`) ma status `STABLE_BETA`: integralność formatu przesyłu, powiązanie z sesją, odrzucanie spóźnionych odpowiedzi i typowane odrzucanie zbyt dużych danych są pokryte testami offline `runtime-command.wire-guard`, `runtime-command.session-binding` i `runtime-command.late-response-ignored`, a w runtime przez RV-003 (sesja `5f641c8d`), rundę A RA-019 (klient porzucił `road-vehicles.spawn` po 250 ms; następne żądanie zakończyło się powodzeniem) oraz zestaw testów kanału z domknięcia runtime `R03` (limit czasu, anulowanie, ponownie użyty identyfikator żądania, odrzucenie przez wtyczkę, operacja wewnętrzna, niewłaściwa sesja i brakujący argument, a po każdym z nich udane żądanie). Stabilność poszczególnych operacji opisano w [możliwościach](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Ponowne użycie kanału po niepowodzeniach

Każda ścieżka zakończenia żądania runtime pozostawia skrzynkę gotową do ponownego użycia: powodzenie, typowany błąd, zniekształcona, zbyt duża, pochodząca z innej sesji lub mająca niewłaściwy identyfikator żądania odpowiedź, przekroczenie limitu czasu, anulowanie przez wywołującego i błędy dekodowania kończą się jednym sprzątaniem, które przywraca slot do stanu bezczynności oraz czyści długość i nagłówek koperty, dzięki czemu następne żądanie nie może odczytać niczego z wcześniejszego. Pozostałe żądanie lub odpowiedź, znalezione przy rozpoczęciu nowego żądania, są najpierw czyszczone (`OL_E_RUNTIME_REQUEST_ID_REUSED`, jeśli mają identyfikator nowego żądania). Po stronie wtyczki wyjątek wewnątrz operacji skutkuje odpowiedzią `OL_E_RUNTIME_OPERATION_FAILED`, wynik większy niż slot – odpowiedzią `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (stała, mała koperta), żądanie dotyczące innej sesji – odpowiedzią `OL_E_RUNTIME_SESSION_MISMATCH`, a na żądanie porzucone przez hosta odpowiedź nigdy nie jest udzielana. Te reguły są pokryte testami offline (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); ścieżki, które może wywołać wywołujący (limit czasu, anulowanie, ponownie użyty identyfikator, typowane odrzucenia, spóźniona odpowiedź), mają także dowody z runtime (RA-019, `R03`). Uszkodzonych, obcych lub zbyt dużych odpowiedzi nie da się wywołać spoza produktu i pozostają one pokryte tylko offline.
