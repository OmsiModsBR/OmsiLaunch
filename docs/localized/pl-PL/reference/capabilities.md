# Możliwości

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/capabilities.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona dokumentuje, co potrafi OmsiLaunch. Jej źródłem jest kanoniczny katalog możliwości, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): każda możliwość, każda publiczna operacja runtime wraz z argumentami, ścieżkami CLI i możliwością natywną oraz jeden stan dowodów na możliwość są tam zadeklarowane tylko raz. Zestaw operacji runtime, walidacja argumentów, ścieżki poleceń CLI, bramka natywna wtyczki dla każdej operacji, `GetCapabilitiesAsync` oraz poniższa [tabela katalogu](#capability-catalog) są z niego wyprowadzone. Dla każdej operacji runtime strona podaje argumenty, klucze wyniku i błędy. Sposób wywołania operacji (ścieżki poleceń CLI, koperta API, limity czasu, uchwyty) opisano w [sterowaniu runtime](runtime-control.md); dowody dla poszczególnych sesji znajdują się w [stanie weryfikacji runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Klasyfikacja i stabilność

`PublicCapabilityClassification` ma cztery wartości. Odpowiadają one słownictwu stabilności w następujący sposób, z obniżeniem poziomu tam, gdzie dowody są niepełne:

| Klasyfikacja | Znaczenie | Stabilność |
| --- | --- | --- |
| `PublicStableBeta` | Publiczna, objęta kontraktem Beta, zweryfikowana w runtime | `STABLE_BETA` (obniżona do `PARTIAL` tam, gdzie to zaznaczono) |
| `PublicExperimental` | Publiczna, może się zmienić, zweryfikowana w runtime lub zweryfikowana statycznie | `EXPERIMENTAL` (obniżona do `PARTIAL` tam, gdzie to zaznaczono) |
| `InternalOnly` | Prymityw badawczy; niedostępny przez publiczne API ani CLI | `INTERNAL` |
| `Unsupported` | Rozpoznawana na potrzeby diagnostyki, odrzucana lub nieobecna | `UNAVAILABLE` |

`PublicCapabilityKind` rozróżnia `Read`, `Write`, `Action` i `Event`. Każda możliwość runtime wymaga sesji w stanie `Running` (`RequiresSession` w rejestrze) oraz tego, aby jej możliwość natywna była rozwiązana i zweryfikowana w działającym buildzie; operacja, której możliwość natywna jest niedostępna, jest odrzucana z `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` oznacza możliwości zależne od celów natywnych (zob. [zgodność](compatibility.md)).

Wyniki runtime są słownikami ciągów znaków. Klucze zaczynające się od `internal_` lub kończące się na `_address`, `_pointer` albo `_vmt` są usuwane na granicy API (`ScrubInternalValues`) i nie zostały tu wymienione. Klucze wyniku w poniższych tabelach operacji są dokładnymi zestawami kluczy zwracanymi przez produkt: zostały przechwycone przez wykonanie każdej publicznej operacji w rzeczywistej sesji (przechwycenie dokumentacji w rundzie domknięcia runtime, sesja `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; wiersze list zapisywane są jako `<row>.<n>.<field>`).

Sposób zgłaszania niepowodzenia operacji (`CurrentRuntimeControl.Execute`):

| Niepowodzenie | `ErrorCode` | `Values` |
| --- | --- | --- |
| Odrzucenie przez rejestr (nieznana operacja lub operacja `internal.*`, brak wymaganego argumentu) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | brak |
| Celowe odrzucenie przez produkt (klucz pochodny w `weather.set`) | konkretny kod (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (zdanie) |
| Niepowodzenie D3D | konkretny kod `OL_E_D3D_*` | `detail`, `native_status` |
| Typowane niepowodzenie po stronie wtyczki (nieaktualny uchwyt, nieznana nazwa, wartość spoza zakresu, brak pojazdu gracza, ...) | konkretny kod, na przykład `OL_E_RUNTIME_OBJECT_HANDLE_STALE` lub `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (komunikat), `exception` (nazwa typu .NET) |
| Operacja, której możliwość natywna nie jest rozwiązana w działającym buildzie | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (możliwość natywna) |
| Nieoczekiwany wyjątek po stronie wtyczki bez kodu | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Wynik, który nie mieści się w skrzynce 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (listy stronicowane są zamiast tego skracane i kontynuowane przez `next_cursor`, patrz [stronicowanie](#pagination)) | brak |

Kolumna „Błędy” (`Errors`) każdej tabeli wymienia konkretne kody; docierają one w `ErrorCode` przez wtyczkę, mostek, płaszczyznę sterowania, API i CLI bez zmian.

<a id="capability-catalog"></a>
## Katalog możliwości

Jeden wiersz na wpis katalogu, generowany z `PublicCapabilityRegistry.All` (bramka dokumentacji kończy się błędem, gdy ta tabela jest nieaktualna). `Session` ma wartość `Running` dla możliwości wymagających działającej sesji. `Operations` to konkretne identyfikatory operacji runtime (wiersze kontraktu w [publicznych operacjach runtime](#public-runtime-operations)). `Native capability` wymienia wpisy katalogu celów natywnych (zob. [zgodność](compatibility.md)), które muszą być rozwiązane i zweryfikowane w działającym buildzie. `Evidence` używa jednego słownika:

| Dowody | Znaczenie |
| --- | --- |
| RUNTIME_VALIDATED | Zaobserwowane w rzeczywistej sesji OMSI; identyfikatory sesji znajdują się w [stanie weryfikacji runtime](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Zaimplementowane i zweryfikowane offline; efekt w runtime nie został zaobserwowany. |
| PARTIAL | Działa dla części swojego kontraktu; uwaga wskazuje otwartą część. |
| RUNTIME_VALIDATION_REQUIRED | Zaimplementowane, oczekuje na weryfikację w runtime. |
| UNAVAILABLE | Rozpoznawane na potrzeby diagnostyki i odrzucane albo niezaimplementowane; zażądanie tego przy starcie czyni plan niemożliwym do uruchomienia. |

Operacja, która coś zmienia, podaje swoją politykę mutacji po swoim identyfikatorze (`PublicRuntimeOperation.MutationPolicy`); żadna operacja runtime nie jest zapisywana w dzienniku:

| Polityka mutacji | Znaczenie |
| --- | --- |
| `none` | Tylko odczyty (nie jest wyświetlana). |
| `omsi-state` | Zmienia stan OMSI w pamięci; nic go nie cofa; kończy się wraz z procesem OMSI. |
| `session-resource` | Tworzy lub zmienia zasób należący do sesji, zwalniany jawnie lub wraz z końcem sesji. |
| `session-policy` | Instaluje politykę, którą wtyczka stosuje ponownie, dopóki nie zostanie usunięta lub sesja się nie skończy. |

<!-- generated:capability-catalog:begin -->
| Możliwość | Klasyfikacja | Rodzaj | Sesja | Operacje | Możliwość natywna | Dowody | CLI | Uwaga |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | nie |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | nie |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | nie |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: zdarzenia wtyczki przechodzą przez numerowany pierścień; czytelnik, który zostanie w tyle, otrzymuje telemetry.lost z liczbą utraconych zdarzeń. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | nie |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | nie |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | nie |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4, runda A: znany układ. |
| `boot.headless-start` | `PublicStableBeta` | Action | nie |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | nie |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | nie |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | nie |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI prezentuje pierwszy rekord każdej dokładnej etykiety; uruchamianie wybiera go według tej etykiety. Zamknięte statycznie; weryfikacja w runtime w toku. |
| `world.saved-situation` | `PublicStableBeta` | Action | nie |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | nie |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: gałąź natywna jest zamknięta statycznie (BI-006); weryfikacja w runtime w toku. Nigdy heurystyka najnowszego .osn. |
| `world.explicit-date` | `PublicExperimental` | Action | nie |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: zapisywane do zegara startowego OMSI przed NewSituation (analiza statyczna); weryfikacja w runtime w toku. |
| `world.explicit-time` | `PublicExperimental` | Action | nie |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: zapisywane do zegara startowego OMSI przed NewSituation (analiza statyczna); weryfikacja w runtime w toku. |
| `world.explicit-year` | `PublicExperimental` | Action | nie |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: zapisywane do zegara startowego OMSI przed NewSituation (analiza statyczna); weryfikacja w runtime w toku. |
| `world.weather` | `PublicExperimental` | Action | nie |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): stosowane zaraz po uruchomieniu świata, przez własne loadWeather OMSI lub jego flagi METAR; nadpisuje pogodę zapisanej sytuacji. Zamknięte statycznie; weryfikacja w runtime w toku. |
| `player-vehicle.model` | `Unsupported` | Action | nie |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; rozwiązywane na potrzeby diagnostyki, nie stosowane. |
| `player-vehicle.repaint` | `Unsupported` | Action | nie |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | nie |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | nie |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | nie |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false przy starcie bez interfejsu; pełna migawka przy zapisanej sytuacji. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: natywna sekwencja przypisania jest zamknięta statycznie (research/reports/beta4-static/entrypoint-last-player-vehicle.md section 3), ale nie jest zaimplementowana przed weryfikacją w runtime; planowana na późniejszą betę. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | nie |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | nie |  |  | STATICALLY_VALIDATED |  | Beta 4: nakładka transakcyjna, zweryfikowana offline. |
| `input.controller` | `PublicExperimental` | Write | nie |  |  | STATICALLY_VALIDATED |  | Beta 4: nakładka transakcyjna, zweryfikowana offline. |
| `presentation.splash` | `PublicStableBeta` | Write | nie |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | nie |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4, runda B: wyłączenie jest stosowane i zgłaszane (internet-textures.suppressed) przez pierścień telemetrii. |
| `internet-textures.override` | `PublicExperimental` | Write | nie |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Domknięcie runtime I01. |
| `session-profiles` | `PublicStableBeta` | Action | nie |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 uruchomienie profilu; 1..99 i wykrywanie offline. |
| `hof.management` | `PublicStableBeta` | Write | nie |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4, runda A, C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (stronicowane) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | nie |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | nie |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | nie |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | nie |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | nie |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | nie |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | nie |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 zamknięte statycznie: SetActualDateTime tylko kopiuje zegar komputera do zegara startowego; daty startu używają world.explicit-date. Zastąpione: natywne ABI i warunki końcowe nie są zamknięte. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: zapisuje actWeather (Weather+0x08), rekord, z którego takt pogody OMSI wyprowadza wartości; zamknięte statycznie. Wartości pochodne (wilgotność, współczynniki światła, rzeczywisty wiatr) pozostają tylko do odczytu (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Weryfikacja w runtime w toku. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (stronicowane) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Wymaga PlayerVehicle (zapisana sytuacja go zapewnia); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE przy NEW_MAP bez interfejsu. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (stronicowane) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | Wykrywanie nieaktualności po naturalnym usunięciu (RV-002) nie ma bezpiecznego producenta w runtime i jest weryfikowane offline. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (stronicowane) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (stronicowane), `timetable.trips.list` (stronicowane), `timetable.lines.list` (stronicowane), `timetable.rv-files.list` (stronicowane), `timetable.track-entries.list` (stronicowane), `timetable.bus-stops.list` (stronicowane), `timetable.station-links.list` (stronicowane), `timetable.tours.list` (stronicowane), `timetable.profiles.list` (stronicowane), `timetable.tour-entries.list` (stronicowane), `timetable.logs.read` (stronicowane), `timetable.no-rv-numbers.list` (stronicowane) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (stronicowane), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (stronicowane), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Zapisy: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI kopiuje ciąg o kształcie stałej; zamknięte statycznie. Weryfikacja w runtime w toku. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): wirtualne TriggerXML/SoundTrigger przez profilowane sloty VMT; zamknięte statycznie. Weryfikacja w runtime w toku. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (stronicowane), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (stronicowane), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (stronicowane) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (stronicowane) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Zaobserwowano reset urządzenia (resetting, restored, unieważnienie generacji); odrębne przejście lost nie zostało wywołane (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` ma status `InternalOnly`: istnieje we wtyczce na potrzeby badań (zwraca adres natywny) i jest odrzucana na granicy API oraz przez lokalną płaszczyznę sterowania, które najpierw sprawdzają nazwę operacji względem `PublicRuntimeOperationIds`; CLI nie ma dla niej ścieżki. `session.stop` kończy OMSI; kooperacyjne zamykanie nie jest zaimplementowane.

<a id="pagination"></a>
## Stronicowanie

Każda operacja listowa (oznaczona „(stronicowane)” w [katalogu możliwości](#capability-catalog)) przyjmuje dwa opcjonalne argumenty i odpowiada czterema polami strony:

| Argument | Znaczenie |
| --- | --- |
| `cursor` | Od którego miejsca zaczyna się strona: `next_cursor` poprzedniej strony. Pominięty: pierwsza strona. Kursor z innej listy lub zniekształcony to `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Żądana liczba wierszy (dodatnia liczba całkowita). Większa niż maksymalna strona listy: używana jest wartość maksymalna. Niebędąca dodatnią liczbą całkowitą: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Klucz wyniku | Znaczenie |
| --- | --- |
| `count` | Wszystkie rekordy, które przechowuje OMSI. |
| `returned_count` | Wiersze w tej odpowiedzi. |
| `truncated` | `true`, gdy po tej stronie następują kolejne rekordy. |
| `next_cursor` | Obecny tylko wtedy, gdy następują kolejne rekordy: należy przekazać go jako `cursor` dla następnej strony. |

Kursor jest stabilnym kluczem, a nie pozycją. Dla tablic rekordów (rozkład jazdy, nazwy skryptów, stałe, krzywe, pliki HOF, kierowcy, bilety, logi) wiersze są kluczowane `<row>.<n>.<field>` (lub `name.<n>`) bezwzględnym indeksem rekordu, a kursor wskazuje następny indeks, więc pierwsza strona jest równa odpowiedzi z Beta 3. Dla kolekcji uchwytów (`road-vehicles.list`, `humans.list`) wiersze są zwracane w kolejności uchwytów, a kursor wskazuje ostatni zwrócony uchwyt: obiekt, który pojawi się między dwiema stronami, dostaje późniejszy uchwyt i trafia na późniejszą stronę; żaden nie jest powtarzany. Wartości kursora należy traktować jako nieprzezroczyste.

Maksymalne rozmiary stron: 1000 dla `road-vehicles.list` i `humans.list`; 512 dla `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` i `vehicle.curves.list`; 256 dla `timetable.tour-entries.list`, `timetable.logs.read` i `vehicle.hofs.read`; 128 dla pozostałych list rozkładu jazdy, `drivers.read` i `tickets.read`. Gdy wiersze strony nadal przekraczają skrzynkę 64 KiB, wtyczka odrzuca ostatnie wiersze, aż odpowiedź się zmieści, a `next_cursor` kontynuuje od pierwszego odrzuconego wiersza, więc żadna lista nie kończy się błędem z powodu rozmiaru.

<a id="public-runtime-operations"></a>
## Publiczne operacje runtime

Jest to dokładna lista identyfikatorów operacji, które publiczny frontend może przekazywać dalej (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Każda z nich wymaga `SessionState.Running`; żadna nie jest zapisywana w dzienniku ani przywracana. Wymagane argumenty są egzekwowane przez rejestr, zanim zostanie użyta skrzynka runtime (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); argumenty opcjonalne weryfikuje wtyczka. Wspólne kody błędów dla wszystkich operacji: `OL_E_RUNTIME_OPERATION_UNKNOWN` (brak na tej liście), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (wtyczka nie ma implementacji), `OL_E_RUNTIME_OPERATION_FAILED` (nieoczekiwany wyjątek w procesie; wartości `detail` i `exception`).

<a id="time"></a>
### Czas

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `time.read` | Read | brak | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcjonalne `hour` (0..23), `minute` (0..59), `second` (0..59.999, dziesiętne); co najmniej jeden | klucze `time.read` po profilowanym `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Pogoda

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | brak | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` oraz wartości źródłowe `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, współczynniki światła i wilgotności to wartości OMSI z każdego taktu; `cloud_type` to rozwiązany numer typu chmur | |
| `weather.set` | Write | co najmniej jeden z `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Zakresy: mgła 0..100000, jasność i wartości gruntu 0..1, prędkość wiatru 0..200, kierunek wiatru 0..360, temperatura i punkt rosy -60..60, ciśnienie 850..1100, opad 0..2, natężenie 0..255, wysokość chmur 0..20000, `cloud_type` 1..64 znaków strony kodowej 1252 (`-1` = brak chmur), wartości logiczne `true`/`false`/`1`/`0` | klucze `weather.read`; z `cloud_type` także `cloud_type_resolved` (`false`, gdy OMSI nie zna typu chmur: nie rysuje żadnych) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (klucz pochodny: `relative_humidity`, `absolute_humidity`, współczynniki światła, `cloud_transparency`, `precipitation_set`, `wet_ground`; szczegół wskazuje klucz, który należy ustawić), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (nieznany klucz), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 litery lub cyfry) | klucze `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | brak | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `map.read` | Read | brak | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Kamera

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | brak | `family` (0 kierowca, 1 pasażer, 2 zewnętrzna, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcjonalne `family` (0..3), `field_of_view` (10..170); co najmniej jeden | klucze `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | wymagany `family` (0..3); opcjonalny `preset` (0..255, tylko z rodziną 0 lub 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Polityka jest ponownie stosowana co 100 ms aż do `camera.unlock`; nieudane ponowne zastosowanie emituje zdarzenie `camera.lock.degraded` raz dla każdego odrębnego błędu. |
| `camera.unlock` | Action | brak | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Pojazdy drogowe i gracz

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | brak | `count`, `ai_collection`, `player_index` (surowy indeks pojazdu gracza w OMSI, zgłaszany tak, jak został odczytany; aby ustalić, czy pojazd gracza istnieje, należy użyć `present` z `player-vehicle.read`) | |
| `road-vehicles.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` to pozycja na liście OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | wymagany `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | brak | `present` (`false`, gdy OMSI nie ma pojazdu gracza); gdy `true`: `player_index` oraz każdy klucz `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | wymagany `model` (kanoniczna ścieżka `Vehicles\...\*.bus`, najwyżej 240 znaków, bez `..`, musi istnieć w obrębie instalacji) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Nie przypisuje pojazdu PlayerVehicle. Trwa kilka sekund; należy użyć limitu czasu klienta 30 s. |
| `road-vehicles.place-random` | Action | opcjonalne `ai_type` (0..255, domyślnie 0), `group` (0..65535, domyślnie 1), `type` (-1..65535, domyślnie -1), `scheduled` (0..1, domyślnie 0), `tour` (0..65535, domyślnie 0), `line` (0..65535, domyślnie 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Postacie

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | brak | `count` | |
| `humans.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `human.<n>.handle` (`hb-NNNNNN`; `<n>` to pozycja na liście OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | wymagany `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Rozkład jazdy, kierowcy, bilety

Listy rozkładu jazdy, `drivers.read` i `tickets.read` są stronicowane (zob. [stronicowanie](#pagination)); „pola strony” poniżej oznaczają `count`, `returned_count`, `truncated` i `next_cursor`, a każda lista przyjmuje opcjonalne `cursor` i `limit`.

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | brak | `invalid` (`true`/`false`) oraz liczby rekordów `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | opcjonalne `cursor`, `limit` | pola strony; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `no_rv.<n>.group`, `no_rv.<n>.number`: numery pojazdów, które OMSI wyłącza z wolnego użycia przez AI (lista zarezerwowanych numerów jako ciągów znaków OMSI, odczytywana przez jego własny układ; `OL_E_RUNTIME_VALUE_INVALID`, gdy lista nie ma oczekiwanego kształtu). Najwyżej 256 na stronę. Analiza statyczna; weryfikacja w runtime w toku. |
| `map.tiles.list` | Read | opcjonalne `cursor`, `limit` | pola strony; `tile.<n>.x`, `tile.<n>.y` (położenie w siatce), `tile.<n>.tile_index` (własny indeks kafla na liście kafli mapy), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` z `tile.<n>.error` dla kafla, którego nie można odczytać. Najwyżej 256 na stronę. Położenie i indeks z analizy statycznej; weryfikacja w runtime w toku. |
| `drivers.read` | Read | opcjonalne `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | opcjonalne `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Skrypty pojazdu, stałe, krzywe, HOF (w zakresie uchwytu)

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | wymagany `handle`; opcjonalne `cursor`, `limit` | `handle`, pola strony, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | wymagane `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | wymagane `handle`, `name`, `value` (skończona liczba zmiennoprzecinkowa) | `handle`, `name`, `requested_value`, `value` (odczyt kontrolny) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | wymagany `handle`; opcjonalne `cursor`, `limit` | `handle`, pola strony, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | wymagane `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | wymagane `handle`, `name` (z rozróżnieniem wielkości liter, tak jak deklaruje je skrypt, do 128 znaków), `value` (1..1024 znaków) | `handle`, `name`, `value` odczytane z powrotem po zapisie | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (sam OMSI po cichu zignorowałby nieznaną nazwę), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | wymagane `handle`, `name` (strona kodowa 1252, do 128 znaków), `value` (`1` uruchamia `name`, `0` uruchamia `name_off`, `2` uruchamia `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. Punkt wejścia skryptu jest wykonywany synchronicznie; nazwa, której skrypt nie definiuje, nic nie robi. Po naciśnięciu zwykle następuje zwolnienie |
| `vehicle.sound-trigger` | Action | wymagane `handle`, `trigger` (strona kodowa 1252, do 128 znaków); opcjonalny `file` (do 260 znaków) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (także: pojazd nie ma pakietu dźwięków), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | wymagany `handle`; opcjonalne `cursor`, `limit` | `handle`, pola strony, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | wymagane `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | wymagany `handle`; opcjonalne `cursor`, `limit` | `handle`, pola strony, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | wymagane `handle`, `name`, `x` (skończona liczba zmiennoprzecinkowa; brakujący lub nienumeryczny `x` daje `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolacja liniowa) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | wymagany `handle`; opcjonalne `cursor`, `limit` | `handle`, pola strony, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Operacje D3D są wykonywane w wątku głównym/wątku renderowania OMSI za pośrednictwem mostka natywnego. Uchwyty tekstur mają postać `d3dtex-<sessionId N>-<16 hex digits>`.

| Operacja | Rodzaj | Argumenty | Klucze wyniku | Błędy |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | brak | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | wymagane `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcjonalny `levels` (0..16, domyślnie 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | wymagany `handle`; opcjonalny `level` (0..15, domyślnie 0) | 13 kluczy `d3d.texture.create` dla żądanego poziomu | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED` oraz błędy create |
| `d3d.texture.update` | Action | wymagane `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (najwyżej 48 KiB po zdekodowaniu); opcjonalne `level` (0..15, domyślnie 0), `x` (0..4095, domyślnie 0), `y` (0..4095, domyślnie 0) | 13 kluczy `d3d.texture.create` po aktualizacji | `OL_E_D3D_INVALID_PIXEL_BUFFER` oraz błędy describe |
| `d3d.texture.release` | Action | wymagany `handle` | 13 kluczy `d3d.texture.create` z `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` przy powtórnym zwolnieniu, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Nieudane operacje D3D zwracają wartości `detail` i `native_status`. Metody rozszerzające `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) opakowują te operacje dla użytkowników API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` zwraca jeden `Capability(Name, Available, EvidenceState, Reason)` na każdy wpis katalogu, który nie jest `InternalOnly`. `Name` to identyfikator z katalogu, a `EvidenceState` to dowody z katalogu. `Available` jest oceniane dla instalacji:

- Wpisy `Unsupported` nigdy nie są dostępne; `Reason` zawiera uwagę z katalogu.
- Każdy wpis wymaga bieżącej platformy, a każdy wpis poza `runtime.current-windows-x64` wymaga instalacji OMSI 2.
- Wpis z możliwościami natywnymi w znanym układzie zgłasza `native: RESOLVED_KNOWN_LAYOUT`. W innym buildzie OMSI 2 plik wykonywalny jest rozwiązywany statycznie: `native: STATICALLY_RESOLVED` albo `native: PENDING_IN_PROCESS_RESOLUTION` ze wskazaniem, czego plik nie rozwiązał. Wpis oczekujący jest nadal zgłaszany jako dostępny, ponieważ rozstrzyga rozwiązywanie w procesie; jeśli się nie powiedzie, operacja lub start kończy się błędem `OL_E_CAPABILITY_UNRESOLVED`.

Planista zgłasza możliwości dla poszczególnych planów w `SessionPlan.RequiredCapabilities` i `SessionPlan.UnsupportedRequestedFeatures` z tymi samymi identyfikatorami katalogu (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Nieobsługiwana zażądana funkcja niesie swoje dowody z katalogu; wymagana możliwość natywna niesie `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` lub `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Znane ograniczenia

- Uchwyty mają zakres sesji i są zgłaszane jako `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, gdy tylko ich obiekt przestanie istnieć. Uchwyt pojazdu jest powiązany z jego VMT, jego definicją i `IDCode` OMSI (unikatowym w procesie numerem, który każdy obiekt mapy otrzymuje przy tworzeniu i który nigdy nie jest używany ponownie), więc obiekt utworzony ponownie pod tym samym adresem jest zawsze odróżniany. Postacie to pulowane sloty żyjące przez całą sesję mapy: uchwyt postaci przestaje się rozwiązywać, gdy postać zniknie (`RenderMe` 0) albo slot zostanie utworzony ponownie (nowy przystanek docelowy lub odjazd).
- Wyniki są ograniczone do skrzynki 64 KiB; każda lista jest stronicowana (zob. [stronicowanie](#pagination)).
- Brak przenoszenia pojazdów między kafelkami, brak zapisu kalendarza, brak przypisywania pojazdu PlayerVehicle w trybie headless. Zapisy zmiennych tekstowych, wyzwalacze skryptów i zapisy pogody są `EXPERIMENTAL` (weryfikacja w runtime w toku). Patrz [znane ograniczenia](known-limitations.md).
