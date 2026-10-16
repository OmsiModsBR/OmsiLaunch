# Dokumentacja CLI

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/cli.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona stanowi kompletną, normatywną dokumentację wiersza poleceń OmsiLaunch `0.1.0-beta4`: trzech plików wykonywalnych, gramatyki argumentów, kolejności obsługi (dispatch), każdego słowa polecenia, każdej hierarchicznej ścieżki polecenia, każdej flagi, kopert wyjściowych oraz zachowania każdego polecenia w razie błędu. Została wygenerowana na podstawie `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` oraz dwóch natywnych nakładek (shim) w `tools\OmsiLaunch.Bootstrapper`. Wyniki procesów opisano w [kodach wyjścia](exit-codes.md), kody błędów w [błędach](errors.md), a gotowe wywołania w [przykładach CLI](cli-examples.md).

<a id="executables"></a>
## Pliki wykonywalne

| Plik | Podsystem | Rola | Różnice |
|---|---|---|---|
| `OmsiLaunch.exe` | Konsola | Natywny bootstrapper (`OmsiLaunch.Bootstrapper.cpp`): ustala własny katalog, dzieli wiersz poleceń na tokeny za pomocą `CommandLineToArgvW`, lokalizuje `hostfxr` poprzez `nethost.dll` i uruchamia `OmsiLaunch.Controller.dll` z tymi samymi argumentami. | Dane wyjściowe są zapisywane na konsolę; kod wyjścia procesu jest kodem wyjścia zarządzanego kontrolera albo kodem nakładki `100`..`106`, jeśli nie udało się uruchomić hosta .NET. |
| `OmsiLaunchW.exe` | Windows (GUI) | Ta sama nakładka (`OmsiLaunch.WindowsHost.cpp`) zbudowana dla podsystemu Windows. Przed uruchomieniem kontrolera ustawia zmienną środowiskową `OMSILAUNCH_WINDOWS_HOST=1`. | Brak konsoli: dane wyjściowe konsoli są wyciszane, chyba że podano `--json` (`WindowsHost.SuppressConsole`), błędy są wyświetlane w oknach komunikatu (`WindowsHost.ShowFailure`: komunikat, `Code: OL_E_...` oraz wskazówka `See .omsilaunch\diagnostics for details.`), a błąd nakładki `100`..`106` jest wyświetlany jako `OmsiLaunch could not start the .NET host (code N).` Pełny opis zachowania: [dokumentacja OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Zarządzany (x64, `net6.0-windows`, Windows Forms) | Właściwy kontroler. Użytkownicy nigdy nie wywołują go bezpośrednio; obie nakładki przekazują ścieżkę kontrolera jako pierwszy argument hosta, dzięki czemu nigdy nie pojawia się ona na publicznej liście argumentów. | Wymaga runtime .NET 6 x64 z `Microsoft.WindowsDesktop.App`; patrz [instalacja](../getting-started/installation.md). |

`nethost.dll` musi znajdować się obok nakładek. Same nakładki nie odczytują żadnych argumentów; każdy argument trafia bez zmian do `CliInput.Parse`, więc `OmsiLaunch.exe` i `OmsiLaunchW.exe` akceptują dokładnie tę samą składnię.

<a id="invocation-model"></a>
## Model wywołania

<a id="argument-grammar-cliinputparse"></a>
### Gramatyka argumentów (`CliInput.Parse`)

| Postać | Znaczenie |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Flaga. Wielkość liter w kluczu nie ma znaczenia; wartością jest wszystko po pierwszym `:`. Nieznane klucze kończą się błędem `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), kod wyjścia `2`. |
| `--key=value` | Argument runtime dla wybranej operacji runtime (na przykład `--handle=rv-000001`). Każdy token `--` zawierający `=` jest argumentem runtime, nigdy flagą. |
| `--json`, `/json` | Wyjście strukturalne (patrz [Formaty wyjścia](#output-formats)). `--json` jest jedynym znaczącym tokenem `--` bez `=`; jest on interpretowany jako flaga `/json`. |
| samo słowo | Jeśli nie pojawiło się jeszcze żadne słowo polecenia, a słowo jest jednym ze [słów poleceń](#command-words), staje się poleceniem. Gdy słowo polecenia jest już obecne, każde kolejne samo słowo jest słowem polecenia (ścieżką polecenia). W przeciwnym razie pierwsze samo słowo jest katalogiem głównym instalacji, a każde kolejne samo słowo jest dołączane do ścieżki polecenia. |

Konsekwencje: hierarchicznej ścieżki polecenia (`time get`) nie można połączyć z argumentem instalacji umieszczonym po niej (`time get D:\OMSI` jest nieznaną ścieżką polecenia `time get d:\omsi`, kod wyjścia `2`). `D:\OMSI time get` jest akceptowane, ale oznacza **tryb właściciela** (uruchamiana jest nowa sesja, a operacja wykonuje się w niej jednokrotnie). Błędy parsowania (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) oraz błędy profilu sesji (`SessionProfileException`) są zgłaszane, zanim cokolwiek zostanie uruchomione, zawsze z kodem wyjścia `2`.

<a id="installation-root"></a>
### Katalog główny instalacji

- Jawny argument instalacji w postaci samego słowa ma pierwszeństwo przed `RootPath` w pliku `/spec` (`CliInput.BuildSpecAsync`).
- `.` oznacza katalog zawierający plik wykonywalny (`AppContext.BaseDirectory`), nigdy katalog roboczy wywołującego (`CliInput.ResolveInstallationRoot`). Opiera się na tym pakiet przenośny.
- Gdy argument zostanie pominięty, operacje trybu właściciela (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) również używają katalogu pliku wykonywalnego. Ścieżka jest normalizowana za pomocą `Path.GetFullPath`.
- Polecenia trybu klienta nigdy nie przyjmują argumentu instalacji: adresują punkt końcowy lokalnego sterowania instalacji, w której znajduje się plik wykonywalny (`AppContext.BaseDirectory`). Patrz [lokalne sterowanie](local-control.md).

<a id="owner-and-client"></a>
### Właściciel i klient

- **Właściciel**: proces, który planuje, uruchamia, nadzoruje i przywraca sesję (`OwnerSession.RunAsync`). Utrzymuje dzierżawę instalacji (`Local\OmsiLaunch.Installation.<sha256(root)>`) i transakcję konfiguracji, udostępnia punkt końcowy lokalnego sterowania, dopóki sesja trwa, i wyświetla [ikonę w obszarze powiadomień](windows-tray.md). Na instalację przypada dokładnie jeden właściciel: jeśli właściciel już odpowiada na `session.status` w punkcie końcowym sterowania, drugie uruchomienie kończy się błędem `OL_E_SESSION_ALREADY_ACTIVE` (kod wyjścia `7`).
- **Klient**: każde wywołanie bez argumentu instalacji, które wysyła `session status`, `session stop`, `events read`, `events watch` lub operację runtime. Jest ono przekazywane przez potok lokalnego sterowania; bez właściciela kończy się błędem `OL_E_NO_ACTIVE_SESSION` (kod wyjścia `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Kolejność obsługi (`CliProgram.RunAsync`)

1. `/silent` (gdy proces nie działa już pod `OmsiLaunchW.exe`): uruchomienie `OmsiLaunchW.exe` z katalogu pliku wykonywalnego przez `ShellExecute` (bez dziedziczenia uchwytów) z tymi samymi argumentami bez `/silent`/`--silent`, zapisanie koperty `silent` (`delegated`, `host_process_id`) i zwrócenie `0`. Proces konsolowy nie czeka na sesję; patrz [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` zwracają `7`.
2. `/version`: koperta `version` z polami `product`, `version` (informacyjna wersja zestawu, pobierana z `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); kod wyjścia `0`.
3. `capabilities`: koperta z każdym deskryptorem `PublicStableBeta` lub `PublicExperimental` z kanonicznego katalogu `PublicCapabilityRegistry` (wraz z jego operacjami, możliwościami natywnymi, dowodami i notą); kod wyjścia `0`.
4. `help [family]`: koperta `help` z polami `usage`, `product_version`, `protocol_version`, `family` oraz publicznymi `commands` (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), opcjonalnie filtrowanymi według rodziny; kod wyjścia `0`.
5. `profiles`: koperta z `requirement` (`OMSI 2`), raportem `installation` dla docelowego katalogu głównego (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), `known_layouts` oraz `predicted_capabilities` rozwiązanymi z pliku wykonywalnego (rozstrzygające jest rozwiązywanie w procesie przy starcie); kod wyjścia `0`.
6. Operacja runtime klienta (bez argumentu instalacji, ze ścieżką polecenia lub `/runtime:`): argumenty są sprawdzane przez `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, kod wyjścia `2`), a następnie `runtime.execute` jest przekazywane z limitem czasu 8 s (30 s dla `road-vehicles.spawn`).
7. Klienckie `session status` (750 ms), `session stop` (powiązane z identyfikatorem aktywnej sesji, 750 ms), `events read` (750 ms), `events watch` (odpytywanie co 250 ms aż do Ctrl+C).
8. `detect` lub **brak jakichkolwiek argumentów** (brak instalacji, polecenia, `/?`, `/spec`, flagi uruchomienia, flagi odzyskiwania i `/list`): wyliczenie procesów `Omsi` i sondowanie punktu końcowego sterowania (250 ms); koperta `detect`; kod wyjścia `0`.
9. `/?` lub `/help`: wypisanie tekstu pomocy, kod wyjścia `0`. Każde inne wywołanie, które zawiera słowo polecenia, ale nie ma obsługiwanej ścieżki polecenia (na przykład samo `d3d` albo `session status D:\OMSI`), wypisuje tekst pomocy i kończy się kodem `2`.
10. Tryb właściciela. Warunki wstępne: `plugins\OmsiLaunch.Plugin.opl` i `plugins\OmsiLaunch.Native.x86.dll` muszą istnieć obok pliku wykonywalnego (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, kod wyjścia `7`). Plik `release-manifest.json` obok pliku wykonywalnego, jeśli istnieje, dostarcza oczekiwane hashe wtyczek.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; koperta `recover` z polami `pending`, `recovered`, `diagnostics`; kod wyjścia `8` tylko wtedy, gdy zażądano przywrócenia i nie zostało ono ukończone, w przeciwnym razie `0`.
12. `/list:<category>`: `DiscoverAsync`; koperta `content.list`; kod wyjścia `0`. `/list:SessionProfiles` używa zamiast tego `ListSessionProfilesAsync`; koperta `session-profiles.list`.
13. Zbudowanie `LaunchSpec` (`BuildSpecAsync`), zaplanowanie go (`PlanSessionAsync`) i wypisanie planu. `/plan` lub `/validate`: kod wyjścia `0`, jeśli `IsRunnable`, w przeciwnym razie `1`. Plan niemożliwy do uruchomienia nigdy nie uruchamia OMSI (kod wyjścia `1`); pod `OmsiLaunchW.exe` uruchomienie z planem niemożliwym do uruchomienia wyświetla jego ostatni komunikat diagnostyczny `OL_E_` w oknie komunikatu (audyt dokumentacji BUG-06). Planowanie weryfikuje też zainstalowany zestaw plików stałej wtyczki (closure) względem `release-manifest.json`, więc brakująca lub zmieniona wtyczka sprawia, że plan jest niemożliwy do uruchomienia (`OL_E_PERMANENT_PLUGIN_*`).
14. Sondowanie istniejącego właściciela (`OL_E_SESSION_ALREADY_ACTIVE`, kod wyjścia `7`), a następnie `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Cykl życia właściciela (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. Od tego momentu każda ścieżka zakończenia dociera do `CloseAsync` w bloku `finally`: wyjątki, Ctrl+C (`Console.CancelKeyPress`), zamknięcie konsoli / wylogowanie (`AppDomain.ProcessExit` z budżetem 4 s na zatrzymanie i przywrócenie; wszystko, co pozostanie, zostanie odzyskane z dziennika przy następnym uruchomieniu), „End session” w obszarze powiadomień, `session.stop` przez potok oraz `/observe-seconds`.
2. Ikona w obszarze powiadomień jest tworzona, chyba że w specyfikacji ustawiono `Presentation.SuppressTrayIcon`.
3. Oczekiwanie na `Running` przez `StartupTimeoutSeconds + 5` sekund. Stan jest wypisywany. Jeśli stanem nie jest `Running`, kod wyjścia `1` (`OmsiLaunchW.exe` wyświetla `The OMSI session did not reach gameplay.` z ostatnim komunikatem diagnostycznym `OL_E_` lub `OL_E_SESSION_START_FAILED`).
4. Uruchamiane są partie walidacyjne (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), które zapisują swoje artefakty.
5. Uruchamiany jest punkt końcowy lokalnego sterowania.
6. `/runtime:<operation>` jest wykonywane jednokrotnie (5 s, 15 s dla `road-vehicles.spawn`); wynik jest zapisywany do `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` i wypisywany. Nieudane polecenie runtime nigdy nie kończy sesji (zamiast tego wypisywane jest `runtime_error`).
7. Oczekiwanie: z `/observe-seconds:n` sesja jest zatrzymywana po `n` sekundach **lub** wcześniej przy zatrzymaniu z obszaru powiadomień/przez potok albo gdy OMSI się zakończy; bez tej flagi właściciel czeka, aż OMSI się zakończy lub zostanie zażądane zatrzymanie.
8. Wypisywany jest stan końcowy; kod wyjścia `0`, jeśli `Completed`, w przeciwnym razie `1`.

`session.stop`, „End session” w obszarze powiadomień, Ctrl+C i `CloseAsync` żądają kanonicznego zatrzymania: OMSI jest kończony przez `TerminateProcess` (własna procedura zamykania OMSI nie jest wykonywana, a `options.cfg` nie jest ponownie zapisywany przez OMSI), po czym przywracany jest każdy plik należący do sesji. Patrz [cykl życia sesji](../concepts/session-lifecycle.md) oraz [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Słowa poleceń

Każde słowo akceptowane na pierwszej pozycji (`CliInput.CommandWordsAccepted`):

| Słowo | Przeznaczenie | Tryb | Uwagi |
|---|---|---|---|
| `capabilities` | Lista publicznych możliwości | Lokalny, bez sesji | Koperta `capabilities`. |
| `profiles` | Lista obsługiwanych wariantów `Omsi.exe` | Lokalny, bez sesji | Koperta `profiles`. |
| `detect` | Raport o procesach `Omsi.exe` i aktywnym właścicielu | Lokalny, bez sesji | Również wartość domyślna, gdy nie podano argumentów. Stany: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, dla poszczególnych procesów `UNKNOWN_BINARY_FOUND`, gdy pliku binarnego nie można zbadać; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Pomoc i katalog publicznych poleceń | Lokalny, bez sesji | `help <family>` filtruje według rodziny możliwości (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Klient | Dokładnie jedno następne słowo; cokolwiek innego wypisuje pomoc, kod wyjścia `2`. `session plan`/`session start` to nazwy ścieżek API, a nie słowa CLI: należy używać `/plan` i `/new`. |
| `events` | `events read`, `events watch` | Klient | `read` zwraca jednokrotnie ograniczoną listę zdarzeń; `watch` wypisuje każde nowe zdarzenie (według `Sequence`) jako kopertę `events.watch` co 250 ms aż do Ctrl+C (kod wyjścia `0`), `4`, gdy żaden właściciel nie odpowiada, `7` przy błędzie sterowania. |
| `time` | `time get`, `time set` | Ścieżka klienta | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Ścieżka klienta | |
| `map` | `map get` | Ścieżka klienta | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Ścieżka klienta | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Ścieżka klienta | |
| `player` | `player get` | Ścieżka klienta | |
| `humans` | `humans list`, `humans get`, `humans summary` | Ścieżka klienta | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Ścieżka klienta | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Ścieżka klienta | |
| `constants` | `constants list`, `constants get` | Ścieżka klienta | |
| `curves` | `curves list`, `curves evaluate` | Ścieżka klienta | |
| `hof` | `hof get` | Ścieżka klienta | |
| `drivers` | `drivers list` | Ścieżka klienta | |
| `tickets` | `tickets get` | Ścieżka klienta | |
| `d3d` | Zarezerwowane słowo rodziny | Brak | `d3d` **nie ma hierarchicznej ścieżki polecenia**: `d3d texture ...` jest nieznaną ścieżką polecenia (kod wyjścia `2`), a samo `d3d` wypisuje pomoc (kod wyjścia `2`). Operacje D3D są dostępne przez `/runtime:d3d.status`, `/runtime:d3d.texture.create` itd. (patrz [Operacje bez ścieżki polecenia](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Hierarchiczne ścieżki poleceń

`CliInput.HierarchicalRoutes` mapuje ścieżkę polecenia zapisaną małymi literami na identyfikator operacji runtime. Wszystkie ścieżki poleceń wymagają sesji w stanie `Running` i są wykonywane przez skrzynkę runtime (mailbox) (`ExecuteRuntimeAsync`). Zapisy runtime zmieniają wyłącznie stan OMSI w pamięci: nigdy nie dotykają plików, nie są częścią transakcji konfiguracji i **nie** są cofane przy zatrzymaniu (OMSI jest kończony). Stabilność wynika z `PublicCapabilityRegistry` i [macierzy walidacji](../status/runtime-validation-status.md); szczegóły i pola wyników opisano w [sterowaniu runtime](runtime-control.md).

| Ścieżka polecenia | Operacja runtime | Rodzaj | Wymaga Running | Zmienia OMSI | Udział w przywracaniu | Stabilność | Uwagi |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Tak | Nie | Brak | STABLE_BETA | Pola zegara i kalendarza. |
| `time set` | `time.set` | Write | Tak | Tak (zegar w pamięci) | Brak, nie jest cofane | EXPERIMENTAL | Na przykład `--minute=<0..59>`; zapis, odczyt zwrotny i przywrócenie zweryfikowane 2026-09-20. |
| `weather get` | `weather.read` | Read | Tak | Nie | Brak | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Tak | Tak (pogoda w pamięci) | Brak, nie jest cofane | EXPERIMENTAL | Na przykład `--wind_speed=5 --cloud_type="Cumulus 1"`. Pogoda ręczna: bieżące aktualizacje METAR zostają zatrzymane. Weryfikacja w runtime w toku. |
| `weather metar set` | `weather.metar.set` | Write | Tak | Tak (pogoda w pamięci; OMSI zapisuje ICAO w `options.cfg`, co sesja przywraca) | Brak, nie jest cofane | EXPERIMENTAL | `--icao=<ICAO>`. Bieżąca pogoda METAR, tak jak ustawia ją okno pogody OMSI. Weryfikacja w runtime w toku. |
| `weather actual get` | `weather.actual.read` | Read | Tak | Nie | Brak | EXPERIMENTAL | Stan kontrolera pogody rzeczywistej/ICAO. |
| `map get` | `map.read` | Read | Tak | Nie | Brak | STABLE_BETA | Nazwa mapy, plik, opis, liczba kafelków, zakres lat i strona ruchu; ponownie zweryfikowane w runtime na poprawionym slocie mapy. |
| `camera get` | `camera.read` | Read | Tak | Nie | Brak | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Tak | Tak (skalary kamery, np. `--field_of_view=`) | Brak, nie jest cofane | EXPERIMENTAL | Zapis i odczyt zwrotny FOV zweryfikowane. |
| `camera lock` | `camera.lock` | Action | Tak | Tak (zasada o zasięgu sesji) | Brak | EXPERIMENTAL | Wymaga `--family=<0..3>` (kierowca=0, pasażer=1, zewnętrzna=2, mapa=3), opcjonalnie `--preset=<n>` (rodzina 0 lub 1). Potrzebuje pojazdu gracza (na przykład z zapisanej sytuacji). Zweryfikowane w runtime w domknięciu runtime (runtime closure, `CAM01`); ciąg `RuntimeValidation` w rejestrze nadal podaje `STATICALLY_VALIDATED` (patrz [możliwości](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Tak | Tak | Brak | EXPERIMENTAL | Zwalnia zasadę ustawioną przez `camera lock` (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Tak | Nie | Brak | STABLE_BETA | Zwraca uchwyty `rv-NNNNNN` o zasięgu sesji. |
| `vehicles get` | `road-vehicle.read` | Read | Tak | Nie | Brak | STABLE_BETA | Wymaga `--handle=`. Nieaktualny uchwyt: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Tak | Nie | Brak | STABLE_BETA | Liczniki i stan gracza, bez uchwytów. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Tak | Tak (dodaje jeden RoadVehicle) | Brak, nie jest usuwany | EXPERIMENTAL | Wymaga `--model=Vehicles\...\*.bus`. Limit czasu klienta 30 s, limit czasu właściciela 15 s. Nie przypisuje pojazdu gracza. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Tak | Tak | Brak | EXPERIMENTAL | Profilowane `PlaceRandomBus`. |
| `player get` | `player-vehicle.read` | Read | Tak | Nie | Brak | STABLE_BETA | Semantyczne null, gdy nie ma pojazdu gracza. |
| `humans list` | `humans.list` | Read | Tak | Nie | Brak | EXPERIMENTAL | Zwraca uchwyty `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Tak | Nie | Brak | EXPERIMENTAL | Wymaga `--handle=`. |
| `humans summary` | `humans.read` | Read | Tak | Nie | Brak | EXPERIMENTAL | Tylko liczniki. |
| `timetable get` | `timetable.read` | Read | Tak | Nie | Brak | STABLE_BETA | Stan menedżera rozkładu jazdy. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Tak | Nie | Brak | STABLE_BETA | Część możliwości `timetable.read`; dowody z odczytu partiami 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable lines list` | `timetable.lines.list` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable tours list` | `timetable.tours.list` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable logs list` | `timetable.logs.read` | Read | Tak | Nie | Brak | STABLE_BETA | Jak wyżej. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Tak | Nie | Brak | EXPERIMENTAL | Numery pojazdów, które OMSI wyłącza z wolnego użycia przez AI (zarezerwowane przez brygady rozkładu jazdy lub stałe listy AI). Stronicowane (`cursor`, `limit`, najwyżej 256 na stronę). |
| `map tiles list` | `map.tiles.list` | Read | Tak | Nie | Brak | EXPERIMENTAL | Kafle mapy: położenie w siatce, własny indeks, plik i stan załadowania, liczby obiektów, ścieżek i splajnów. Stronicowane (`cursor`, `limit`, najwyżej 256 na stronę). |
| `drivers list` | `drivers.read` | Read | Tak | Nie | Brak | EXPERIMENTAL | Rekordy kierowców. |
| `tickets get` | `tickets.read` | Read | Tak | Nie | Brak | EXPERIMENTAL | Rekordy pakietów biletów. |
| `hof get` | `vehicle.hofs.read` | Read | Tak | Nie | Brak | STABLE_BETA | Wymaga `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Tak | Nie | Brak | STABLE_BETA | Wymaga `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Tak | Nie | Brak | STABLE_BETA | Wymaga `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Tak | Nie | Brak | STABLE_BETA | Wymaga `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Tak | Nie | Brak | STABLE_BETA | Wymaga `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Tak | Nie | Brak | EXPERIMENTAL | Wymaga `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Tak | Nie | Brak | EXPERIMENTAL | Wymaga `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Tak | Tak (zmienna skryptu) | Brak, nie jest cofane | EXPERIMENTAL | Wymaga `--handle=`, `--name=`, `--value=` (liczba skończona). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Tak | Nie | Brak | EXPERIMENTAL | Wymaga `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Tak | Nie | Brak | EXPERIMENTAL | Wymaga `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Tak | Tak (stan skryptu w pamięci) | Brak, nie jest cofane | EXPERIMENTAL | Wymaga `--handle=`, `--name=`, `--value=`. Własne `SetStringVariable` OMSI. Weryfikacja w runtime w toku. |
| `scripts trigger` | `vehicle.trigger` | Action | Tak | Tak (uruchamia kod skryptu pojazdu) | Brak, nie jest cofane | EXPERIMENTAL | Wymaga `--handle=`, `--name=`, `--value=` (`1` naciśnięcie, `0` zwolnienie, `2` przeciągnięcie). Weryfikacja w runtime w toku. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Tak | Tak (odtwarza dźwięki pojazdu) | Brak, nie jest cofane | EXPERIMENTAL | Wymaga `--handle=`, `--trigger=`; opcjonalnie `--file=`. Weryfikacja w runtime w toku. |

<a id="operations-without-a-route"></a>
### Operacje bez ścieżki polecenia

Te publiczne identyfikatory operacji (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) nie mają hierarchicznej ścieżki polecenia i są wywoływane przez `/runtime:<operation>` wraz z `--key=value` lub `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (wymagane `width`, `height`, `format`; opcjonalnie `levels`), `d3d.texture.describe` (`handle`; opcjonalnie `level`), `d3d.texture.update` (wymagane `handle`, `width`, `height`, `pixels_base64`; opcjonalnie `level`, `x`, `y`), `d3d.texture.release` (`handle`). Operacje D3D są EXPERIMENTAL; cykl życia tekstur i unieważnianie przy resecie urządzenia są zweryfikowane w runtime (domknięcie runtime `H02`, `D01`; patrz [możliwości](capabilities.md)). `timetable.track-entries.list` i `timetable.tour-entries.list` są listami ograniczonymi: wynik, który nie mieści się w slocie runtime, jest skracany (`truncated=true`). `internal.road-vehicles.make-basic` jest INTERNAL i jest odrzucana z kodem `OL_E_RUNTIME_OPERATION_UNKNOWN` zarówno przez CLI, jak i przez API.

<a id="flags"></a>
## Flagi

Każda flaga z `CliInput.KnownFlags`. „Faza” to *etap uruchamiania* (kształtuje `LaunchSpec`/plan nowej sesji), *runtime* (działa na uruchomionej sesji) lub *sterowanie* (zmienia zachowanie samego CLI). Flagi parsowane wyłącznie dla zgodności (`CliInput.AcceptedNoEffectFlags`) są oznaczone w swoim wierszu.

<a id="control-and-output"></a>
### Sterowanie i wyjście

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/?` | `/?` | wył. | sterowanie | STABLE_BETA | Wypisuje tekst pomocy, kod wyjścia `0`. |
| `/help` | `/help` | wył. | sterowanie | STABLE_BETA | To samo co `/?`. (Samo słowo `help` zwraca natomiast katalog strukturalny.) |
| `/version` | `/version` | wył. | sterowanie | STABLE_BETA | Koperta `version`, kod wyjścia `0`. Obsługiwana przed każdym innym poleceniem z wyjątkiem `/silent`. |
| `/json` | `/json` lub `--json` | wył. | sterowanie | STABLE_BETA | Emituje koperty JSON; wymusza też wyjście na konsolę nawet pod `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | wył. | sterowanie | STABLE_BETA | Wycisza czytelny dla człowieka komunikat o powodzeniu. Dane wyjściowe JSON (`/json`) i błędy są nadal zapisywane; kod wyjścia się nie zmienia. |
| `/silent` | `/silent` (również `--silent`) | wył. | sterowanie | EXPERIMENTAL | Deleguje cały wiersz poleceń do `OmsiLaunchW.exe`, zwraca `0`, gdy tylko proces hosta się uruchomi. Wynik sesji jest raportowany przez `OmsiLaunchW.exe` (okna komunikatu, ikona w obszarze powiadomień), `.omsilaunch\diagnostics` oraz punkt końcowy lokalnego sterowania. Delegowanie i okna dialogowe błędów są zweryfikowane w runtime (domknięcie runtime `T04`); patrz [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | sterowanie | REMOVED | Usunięta w 0.1.0-beta4: odrzucana z `OL_E_INVALID_ARGUMENT` (kod wyjścia `2`). Właściciel sesji zawsze udostępnia lokalny punkt końcowy sterowania. |
| `/verbose` | `/verbose` | wył. | etap uruchamiania | PARTIAL | `DiagnosticsSpec.Verbose`. Wartości są przenoszone w specyfikacji; ich efekt ogranicza się do śladu hosta w `.omsilaunch\diagnostics`. |
| `/log` | `/log` | wł. (`DiagnosticsSpec.Log` domyślnie ma wartość `true`) | etap uruchamiania | PARTIAL | `DiagnosticsSpec.Log`. W praktyce zawsze włączone. |
| `/logall` | `/logall` | wył. | etap uruchamiania | PARTIAL | Ustawia jednocześnie `Verbose`, `ProcessTrace`, `PluginTrace` i `NativeTrace`. |
| `/omsi-logall` | `/omsi-logall` | wył. | etap uruchamiania | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | wył. | etap uruchamiania | PARTIAL | Alias `/trace-process`. |
| `/trace-process` | `/trace-process` | wył. | etap uruchamiania | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | wył. | etap uruchamiania | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: wtyczka zgłasza każdą operację runtime (`plugin.trace.operation`: operacja, wynik, kod, czas trwania) do śladu hosta sesji. |
| `/trace-native` | `/trace-native` | wył. | etap uruchamiania | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: warstwa natywna zgłasza każdą instalację i usunięcie hooka (`native.trace`) do śladu hosta sesji. |

<a id="planning-validation-and-harnesses"></a>
### Planowanie, walidacja i narzędzia testowe

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/plan` | `/plan` | wył. | etap uruchamiania | STABLE_BETA | Buduje i wypisuje `SessionPlan`, nie uruchamia OMSI. Kod wyjścia `0`, gdy `IsRunnable`, w przeciwnym razie `1`. Wymaga wyboru uruchomienia (`/new`, `/saved`, `/spec` lub argumentu instalacji); samo `/plan` bez niczego więcej wykonuje `detect`. |
| `/validate` | `/validate` | wył. | etap uruchamiania | STABLE_BETA | W tym buildzie identyczne z `/plan`. |
| `/runtime-batch` | `/runtime-batch` | wył. | runtime (właściciel) | INTERNAL | Narzędzie walidacyjne: po osiągnięciu `Running` wykonuje zestaw operacji odczytu i zapisuje `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | wył. | runtime (właściciel) | INTERNAL | Narzędzie walidacyjne: odczyty oraz `time.set`, `camera.set` i `vehicle.variable.set` z przywróceniem; zapisuje `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | wył. | runtime (właściciel) | INTERNAL | Narzędzie walidacyjne dla cyklu życia tekstur D3D; zapisuje `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | brak | runtime | STABLE_BETA (obsługa) | Wybiera publiczną operację runtime według identyfikatora. Tryb klienta (bez argumentu instalacji): przekazywana do właściciela. Tryb właściciela: wykonywana jednokrotnie po osiągnięciu `Running`. Nieznane identyfikatory: `OL_E_RUNTIME_OPERATION_UNKNOWN`, kod wyjścia `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (można powtarzać) | brak | runtime | STABLE_BETA (obsługa) | Argument runtime; odpowiednik `--key=value`. Brak `=`: `/runtime-arg requires key=value`, kod wyjścia `2`. |

<a id="world-selection"></a>
### Wybór świata

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` jest trybem domyślnym, ale uruchomienie jest żądane tylko wtedy, gdy obecna jest jedna z flag `/new`, `/saved`, `/last`, `/spec` | etap uruchamiania | STABLE_BETA | NEW_MAP. Wymaga `/map` i `/entrypoint-index` (plan bez podanego indeksu punktu wejścia zgłasza `OL_E_ENTRYPOINT_REQUIRED`; bez `/map` żadna mapa nie zostaje rozwiązana). `/new` nigdy nie wybiera mapy po cichu. |
| `/saved` | `/saved:<file.osn>` | brak | etap uruchamiania | STABLE_BETA | SAVED_SITUATION. Mapa i pozycja pochodzą z pliku `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` są odrzucane w połączeniu z `/saved` (kod wyjścia `2`). Brak sytuacji: `OL_E_SITUATION_NOT_FOUND`; brak jej mapy: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | brak | etap uruchamiania | EXPERIMENTAL | LAST_MAP_STATE: wczytuje własny `laststn.osn` mapy (zapisany przez OMSI przy zamknięciu mapy) przez gałąź ostatniego stanu formularza Start. Wymaga `/map`; mapa bez `laststn.osn` to `OL_E_SITUATION_NOT_FOUND`. Nigdy heurystyka najnowszego `.osn`. Weryfikacja w runtime w toku. |
| `/map` | `/map:<identity>` (na przykład `maps\Grundorf\global.cfg`) | brak | etap uruchamiania | STABLE_BETA | Tożsamość mapy dla `/new` lub zakres dla `/list:Entrypoints`. Nieznana: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | brak | etap uruchamiania | EXPERIMENTAL | Punkt wejścia według dokładnej etykiety (z rozróżnieniem wielkości liter) lub identyfikatora z `/list:entrypoints`. OMSI prezentuje tylko pierwszy rekord każdej etykiety; późniejszy rekord o tej samej etykiecie jest odrzucany (`OL_E_ENTRYPOINT_NOT_PRESENTED`), podobnie nieznany (`OL_E_ENTRYPOINT_NOT_FOUND`). Weryfikacja w runtime w toku. Wzajemnie wykluczające się z `/entrypoint-index` (tożsamość wygrywa i czyści indeks). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | brak | etap uruchamiania | STABLE_BETA | Indeks punktu wejścia na prezentowanej liście (liczony od 1, tak jak prezentuje go OMSI). Wymagany dla możliwego do uruchomienia planu NEW_MAP. |

<a id="date-time-and-weather"></a>
### Data, czas i pogoda

Te flagi są akceptowane i przenoszone do `LaunchSpec`. Data, czas i rok rozpoczęcia dotyczą wyłącznie sesji NEW_MAP: przy `/saved` lub `/last` planista rejestruje je jako `UNAVAILABLE` **i dodaje `OL_E_CAPABILITY_UNAVAILABLE`, więc plan jest NIEMOŻLIWY DO URUCHOMIENIA (kod wyjścia `1`)**, ponieważ zapisana sytuacja zachowuje własny zegar. Pogoda dotyczy każdego trybu świata. Wszystkie są `EXPERIMENTAL`: natywna ścieżka uruchamiania jest zamknięta statycznie, a weryfikacja w runtime jest w toku. Plik `/spec` lub profil sesji, który je ustawia, ma ten sam skutek.

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` lub `/date:system` | nieustawione | etap uruchamiania | EXPERIMENTAL | `DateSpec` jawne/systemowe: data startu sesji NEW_MAP (zob. [launchspec](launchspec.md#datespec-timespec-yearspec)). Wartość, której nie można sparsować: `OL_E_INVALID_ARGUMENT`, kod wyjścia `2`; niemożliwa data: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` lub `/time:system` | nieustawione | etap uruchamiania | EXPERIMENTAL | `TimeSpec` jawne/systemowe: godzina startu sesji NEW_MAP. |
| `/year` | `/year:<n>` lub `/year:system` | nieustawione | etap uruchamiania | EXPERIMENTAL | `YearSpec`: rok startu sesji NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | nieustawione | etap uruchamiania | EXPERIMENTAL | `WeatherMode.Preset`: plik z folderu `weather` instalacji, podany nazwą (`Regen`) lub ścieżką (`weather\Regen.owt`); brak pliku to `OL_E_WEATHER_INVALID` na etapie planowania. Wczytywany przez własne `loadWeather` OMSI zaraz po uruchomieniu świata; bieżący METAR pozostaje wyłączony. Weryfikacja w runtime w toku. |
| `/weather-icao` | `/weather-icao:<code>` | nieustawione | etap uruchamiania | EXPERIMENTAL | `WeatherMode.Icao`: bieżąca pogoda METAR dla czteroznakowej stacji ICAO (w przeciwnym razie `OL_E_WEATHER_INVALID`). OMSI zapisuje stację w `options.cfg`, co sesja przywraca. Weryfikacja w runtime w toku. |
| `/weather-real` | `/weather-real` | nieustawione | etap uruchamiania | EXPERIMENTAL | `WeatherMode.RealCurrent`: bieżąca pogoda METAR dla stacji, którą OMSI ma już skonfigurowaną; bez niej start kończy się błędem `OL_E_WEATHER_APPLY_FAILED`. Wygrywa ostatnia z flag `/weather`, `/weather-icao`, `/weather-real`. Każda pogoda startowa nadpisuje pogodę zapisanej sytuacji. |

<a id="player-vehicle"></a>
### Pojazd gracza

Akceptowane i rozwiązywane względem instalacji, ale niestosowane przez runtime: każde ustawione pole jest `UNAVAILABLE` i dodaje `OL_E_CAPABILITY_UNAVAILABLE` (plan NIEMOŻLIWY DO URUCHOMIENIA, kod wyjścia `1`).

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | nieustawione | etap uruchamiania | UNAVAILABLE | Rozwiązywane jako pierwsze (`OL_E_VEHICLE_NOT_FOUND`, jeśli nieznane). |
| `/repaint` | `/repaint:<id>` | nieustawione | etap uruchamiania | UNAVAILABLE | Rozwiązywane tylko razem z `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | nieustawione | etap uruchamiania | UNAVAILABLE | `OL_E_HOF_NOT_FOUND`, jeśli nieznane. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | brak | etap uruchamiania | STABLE_BETA | Jedna sesyjna operacja [zarządzania HOF](hof-management.md): `HofSourceKind` i `Source`. Wymaga co najmniej jednego `/hof-target`. Zastępuje `HofManagement` z pliku `/spec`; przy ustawieniu wstępnym profilu sesji z `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Błędny format: kod wyjścia `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (można powtarzać) | brak | etap uruchamiania | STABLE_BETA | Pojazd docelowy poniżej `Vehicles\`. Bez `/hof-source`: kod wyjścia `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (można powtarzać) | wszystkie pliki | etap uruchamiania | STABLE_BETA | Ogranicza źródło do tych nazw plików. Bez `/hof-source`: kod wyjścia `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | etap uruchamiania | STABLE_BETA | `HofApplyMode`. Bez `/hof-source`: kod wyjścia `2`. |
| `/module` | `/module:<id>` (można powtarzać) | brak | etap uruchamiania | EXPERIMENTAL | Żąda [modułu runtime](../../../reference/runtime-modules.md) dla sesji (`LaunchSpec.Modules.Enable`, dodawany do modułów z pliku `/spec`). Zażądany moduł, którego nie można wczytać, czyni plan niemożliwym do uruchomienia z jego kodem `OL_E_MODULE_*` (kod wyjścia `1`). Jego operacje są dostępne przez `/runtime:module.<id>.<operation>` i `/runtime-arg`. Przy ustawieniu wstępnym profilu sesji z `enabled: false` dla niego: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (można powtarzać) | brak | etap uruchamiania | EXPERIMENTAL | Wyklucza moduł z sesji, także moduł wczytywany domyślnie, przez jego mapę lub przez plik ustawień (`LaunchSpec.Modules.Disable`). Przy ustawieniu wstępnym z `enabled: true` dla niego: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Bez wartości: kod wyjścia `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (można powtarzać) | brak | etap uruchamiania | EXPERIMENTAL | Ustawia ustawienie modułu (`LaunchSpec.Modules.Settings`); ma pierwszeństwo przed plikiem `/spec`, plikiem ustawień i wartością domyślną. Klucz, który ustawia także wybrane ustawienie wstępne: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Format inny niż `<id>.<key>=<value>`: kod wyjścia `2`. Nieprawidłowa wartość lub niezadeklarowany klucz: komunikat diagnostyczny planu `OL_E_MODULE_SETTING_INVALID` (kod wyjścia `1`). |
| `/<module flag>` | `/<flag>:<value>`; samo `/<flag>` oznacza `true` dla ustawienia logicznego | brak | etap uruchamiania | EXPERIMENTAL | Flaga, którą zainstalowany moduł deklaruje dla jednego ze swoich ustawień (`flag` w jego manifeście; wymieniana przez `/list:RuntimeModules`). Jest rozwiązywana przy planowaniu sesji i jest tym samym co `/module-setting:<id>.<key>=<value>`. Flaga, której nie deklaruje żaden zainstalowany moduł, lub użyta poza uruchomieniem: `Unknown argument`, kod wyjścia `2`. Flaga zadeklarowana przez dwa moduły: `OL_E_MODULE_SETTING_INVALID`, kod wyjścia `2`. Flaga modułu nigdy nie zastępuje flagi wbudowanej. |
| `/fleet` | `/fleet:<n>` | nieustawione | etap uruchamiania | UNAVAILABLE | Numer taborowy. |
| `/registration` | `/registration:<text>` | nieustawione | etap uruchamiania | UNAVAILABLE | Numer rejestracyjny. |
| `/no-vehicle` | `/no-vehicle` | wył. | etap uruchamiania | STABLE_BETA | Usuwa pojazd gracza ze specyfikacji bazowej (`/spec` lub profilu). Nieszkodliwe. |

<a id="configuration-overlays"></a>
### Nakładki konfiguracji

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (można powtarzać; wielkość liter w kluczach nie ma znaczenia) | brak | etap uruchamiania | STABLE_BETA | Semantyczna nakładka (overlay) na `options.cfg` z `ConfigurationCatalog` (na przykład `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Nieznany klucz: `OL_E_UNKNOWN_SETTING` (kod wyjścia `2`); klucz tylko do odczytu (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (kod wyjścia `2`); wartość spoza zakresu lub niepoprawna: `OL_E_INVALID_SETTING_VALUE` podczas budowania nakładki. Nakładka jest mutacją sesji: jest zapisywana w migawce, stosowana przed uruchomieniem OMSI i przywracana bajt po bajcie przy zatrzymaniu (RV-005 `RUNTIME_VALIDATED`). Konflikt z kluczem należącym do ustawienia wstępnego wybranego profilu: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Prezentacja ekranu startowego

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (wielkość liter bez znaczenia) | `Managed` | etap uruchamiania | STABLE_BETA | `Managed`: dołączone do pakietu 24-bitowe pliki BMP 640x480 są jednorazowo kopiowane do `<root>\.omsilaunch\assets\splash`, a `GUI\NewSplashscreen_ENG.bmp` oraz `GUI\NewSplashscreen_<lang>.bmp` są nakładane transakcyjnie i dokładnie przywracane (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (aliasy): pliki OMSI pozostają nietknięte. Brak wartości: `/splash requires Unset, Native, or Managed`, kod wyjścia `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (również `pt-BR`, `de`, `fr`, `en`; każda inna wartość powoduje powrót do `ENG`) | `[language]` z `options.cfg`, w przeciwnym razie `ENG` | etap uruchamiania | STABLE_BETA | Wybiera zlokalizowany plik docelowy. |
| `/splash-assets` | `/splash-assets:<directory>` (ścieżki względne są rozwiązywane względem katalogu głównego instalacji) | `<root>\.omsilaunch\assets\splash`, w przeciwnym razie zestaw z pakietu | etap uruchamiania | STABLE_BETA | Własny katalog zasobów; musi zawierać `ENG.bmp` oraz, dla języka innego niż angielski, `<lang>.bmp`. Błędy: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (zgłaszane w planie jako `OL_E_SESSION_PRESENTATION_INVALID`; niemożliwy do uruchomienia). |

<a id="internet-textures"></a>
### Tekstury internetowe

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | etap uruchamiania | EXPERIMENTAL | `Native`: bez zmian. `Disabled`: profilowany mechanizm pobierania działający w procesie jest wyłączany. `Override`: podany profil `.itx` jest nakładany jako `Texture\standard.itx`; każdy wymieniony w nim cel HTTP(S) oraz `Texture\standard.ipr` stają się usunięciami sesji (usuwane na czas sesji, przywracane przy zatrzymaniu). Brak wartości: kod wyjścia `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | brak | etap uruchamiania | EXPERIMENTAL | Wymagane z `Override` (`OL_E_ITX_PROFILE_REQUIRED`, kod wyjścia `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (muszą to być pary wierszy URL/cel z adresami URL `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (cele muszą się rozwiązywać wewnątrz `Texture\`, bez ścieżek bezwzględnych, `..` ani punktów ponownej analizy). |

<a id="session-profiles"></a>
### Profile sesji

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | brak | etap uruchamiania | STABLE_BETA (kompilacja; offline `OmsiLaunch.ProfileTests`) | Wczytuje `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (patrz [profile sesji](session-profiles.md)). Wymaga `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, kod wyjścia `2`). Blok `new:` jest stosowany tylko z `/new`; `compatibility.maps` jest egzekwowane dla `/new` i `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Jawne flagi kolidujące z polem należącym do profilu są odrzucane z kodem `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): mapa/punkt wejścia/data/czas/rok/pogoda, gdy należą do bloku `new:`, klucze `/set` należące do ustawienia wstępnego, flagi ekranu startowego, gdy ustawienie wstępne ma `presentation`, flagi tekstur internetowych, gdy ma `internet-textures`, limity czasu, gdy ma `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | brak | etap uruchamiania | STABLE_BETA | Wybiera ustawienie wstępne według `index`. Poza zakresem: kod wyjścia `2`. |

<a id="launchspec-file"></a>
### Plik LaunchSpec

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | brak | etap uruchamiania | STABLE_BETA (loader przetestowany offline; semantyka sesji identyczna jak dla flag) | Wczytuje plik JSON `LaunchSpec` jako specyfikację bazową (patrz [LaunchSpec](launchspec.md)) i oznacza, że zażądano uruchomienia. Reguły (`LaunchSpecJson`): plik musi istnieć (`OL_E_SPEC_NOT_FOUND`, kod wyjścia `6`); najwyżej 1 MiB (`OL_E_SPEC_TOO_LARGE`, kod wyjścia `2`); element główny musi być obiektem (`OL_E_SPEC_INVALID`); wielkość liter w nazwach właściwości nie ma znaczenia; dozwolone są komentarze `//` i końcowe przecinki; głębokość najwyżej 32; każda nieznana właściwość jest odrzucana wraz ze swoją ścieżką JSON (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, kod wyjścia `2`). |

**Pierwszeństwo** (`CliInput.BuildSpecAsync`): wartości domyślne → plik `/spec` → `/predefined-profile` (zastępuje `Installation` i `World`, a następnie stosuje profil) → jawne flagi. Jawny argument instalacji ma pierwszeństwo przed `RootPath` w specyfikacji. `/no-vehicle` usuwa pojazd gracza ze specyfikacji; `/vehicle` i pokrewne flagi są scalane z nim pole po polu. Klucze `/set` są scalane z `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` nadpisują wartości tylko wtedy, gdy zostały podane. `/startup-timeout` i `/shutdown-timeout` nadpisują wartości tylko wtedy, gdy zostały podane; `Presentation.SuppressTrayIcon` pochodzi wyłącznie ze specyfikacji (brak flagi). Flagi diagnostyczne są łączone operacją OR z `Diagnostics` ze specyfikacji.

<a id="content-discovery"></a>
### Wykrywanie zawartości

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; kategoriami są wartości `ContentQueryKind`: `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (wielkość liter bez znaczenia) | brak | lokalnie, bez sesji | STABLE_BETA | `DiscoverAsync` na instalacji; koperta `content.list` z wpisami `Identity`, `Kind`, `DisplayName`; kod wyjścia `0`. `SessionProfiles` odpowiada przez `ListSessionProfilesAsync` (koperta `session-profiles.list`), a `RuntimeModules` przez `ListRuntimeModulesAsync` (koperta `runtime-modules.list`: identyfikator, wersja, aktywacja, operacje i poprawność każdego modułu oraz jego ustawienia z typem, wartością domyślną i flagą). Nieznana kategoria: `Unknown discovery category`, kod wyjścia `2`. Punkty ponownej analizy (junction/dowiązania symboliczne) są pomijane, pliki OMSI są odczytywane w kodowaniu Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | brak | lokalnie | STABLE_BETA | Zakres przekazywany dla każdej kategorii z wyjątkiem `Entrypoints`, która jako zakresu używa `/map`. |

<a id="timeouts-and-observation"></a>
### Limity czasu i obserwacja

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` sekund | wartość ze specyfikacji/profilu, w przeciwnym razie `180` | etap uruchamiania | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. Właściciel czeka na `Running` przez tę wartość plus 5 s; `OL_E_STARTUP_TIMEOUT` kończy sesję z kodem wyjścia `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` sekund | wartość ze specyfikacji/profilu, w przeciwnym razie `30` | etap uruchamiania | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: z `/cooperative-shutdown` – jak długo nadzorca czeka, aż OMSI sam się zamknie, zanim go przerwie; bez tej flagi OMSI jest przerywany od razu, a wartość nie ma efektu. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | wył. | etap uruchamiania | EXPERIMENTAL | `Behavior.CooperativeShutdown`: zakończenie działającej sesji prosi OMSI o zamknięcie przez jego własną procedurę wyjścia (tę, która działa po potwierdzeniu wyjścia przez użytkownika: zapisuje `options.cfg` i `laststn.osn` mapy, a następnie zamyka program) i czeka `ShutdownTimeoutSeconds`, zanim go przerwie. Opcja dobrowolna do czasu weryfikacji w runtime; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`, gdy OMSI musiał zostać przerwany. Pliki sesji są w obu przypadkach przywracane po zakończeniu. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | brak (działanie do zakończenia OMSI lub zażądania zatrzymania) | runtime (właściciel) | STABLE_BETA | Górny limit fazy działania: po `n` sekundach w stanie `Running` żądane jest kanoniczne zatrzymanie. Zatrzymanie z obszaru powiadomień lub przez potok albo zakończenie OMSI kończy ją wcześniej. `0` zatrzymuje natychmiast po osiągnięciu `Running`. |

<a id="recovery"></a>
### Odzyskiwanie

| Flaga | Składnia i wartości | Domyślnie | Faza | Stabilność | Zachowanie |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | wył. | lokalnie | STABLE_BETA | Raportuje, czy `<root>\.omsilaunch\journal.json` oczekuje (`pending`), nigdy nie przywraca; kod wyjścia `0`. Przejmuje dzierżawę instalacji: `OL_E_INSTALLATION_BUSY` (kod wyjścia `7`), dopóki utrzymuje ją właściciel. |
| `/recover` | `/recover` | wył. | lokalnie | STABLE_BETA | Przywraca oczekujący dziennik (kopie zapasowe są najpierw weryfikowane względem SHA-256 z migawki; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` są raportowane w `diagnostics`). Kod wyjścia `0`, gdy nic nie oczekiwało lub przywrócenie zostało ukończone; `8`, gdy dziennik oczekiwał i nadal pozostaje. Odmowa z kodem `OL_E_INSTALLATION_BUSY`, dopóki działa zapisany w dzienniku proces OMSI (PID, czas utworzenia, ścieżka exe) lub – w przypadku dziennika po etapie `HandoffCreated` bez PID – dowolny `Omsi.exe` z tego katalogu głównego. Każde uruchomienie sesji automatycznie wykonuje to samo odzyskiwanie przed odczytaniem instalacji. |
| `/clear-module-data` | `/clear-module-data:<module id>` | brak | lokalnie, bez sesji | EXPERIMENTAL | Usuwa katalog danych modułu `<root>\.omsilaunch\module-data\<id>` i nic więcej; koperta `module-data.clear` z `module` i `cleared` (`false`, gdy nie było nic do usunięcia); kod wyjścia `0`. Zajmuje dzierżawę instalacji: `OL_E_INSTALLATION_BUSY` (kod wyjścia `7`), dopóki sesja jest właścicielem instalacji. Folder danych modułu osiągany przez dowiązanie jest odrzucany (`OL_E_MODULE_INTEGRITY`); dowiązanie wewnątrz niego jest usuwane jako dowiązanie. |

<a id="output-formats"></a>
## Formaty wyjścia

- **Koperta sukcesu** (`CliInput.WriteEnvelope`, z `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, z wcięciami. Przekazywane odpowiedzi `session status` i `events read` dodają element `metadata`, gdy starsze zdarzenia pominięto, aby zmieścić się w ramce sterowania (`events_dropped_count`, patrz [lokalne sterowanie](local-control.md)). Bez `--json` wypisywany jest tylko `<object>` jako JSON z wcięciami, a po nim `Note: <n> older events were omitted to fit the control frame.`, gdy zdarzenia zostały pominięte.
- **Koperta błędu** (`CliInput.WriteError`, z `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Bez `--json`: `OL_E_<CODE>: message` w jednym wierszu. Kategorie: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Pod `OmsiLaunchW.exe` ten sam kod i komunikat są wyświetlane w oknie komunikatu.
- **Plan i stan** (`CliInput.Write`): rekordy `SessionPlan`, `SessionStatus` i `RuntimeCommandResult` są wypisywane jako JSON z wcięciami **bez** koperty. Bez `--json` plan jest streszczany jako `Plan: READY profile=Omsi23004_692EBFBF` lub `Plan: NOT RUNNABLE profile=...`; pozostałe rekordy nadal są wypisywane jako JSON. Wartości wyliczeń są serializowane jako liczby całkowite (`SessionState.Running` to `14`, `Completed` to `18`, `Failed` to `19`).
- Nazwy poleceń używane w kopertach: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile` oraz identyfikator operacji runtime w przypadku przekazywanych poleceń runtime.
- Pod `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) nic nie jest zapisywane na konsolę, chyba że podano `--json`.

<a id="errors-per-command"></a>
## Błędy według poleceń

| Polecenie | Typowe kody błędów | Kod wyjścia |
|---|---|---|
| Dowolny błąd parsowania | `OL_E_INVALID_ARGUMENT`, kody profilu sesji (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Ścieżka klienta, `/runtime` (klient) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` zwracane przez właściciela, np. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Kontrola wstępna właściciela | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); oczekujący, ale nieodzyskany (`8`) | `7`, `8` |
| `/list` | nieznana kategoria (`2`); `OL_E_INSTALLATION_NOT_FOUND`/brakujące katalogi (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, uruchomienie | komunikaty diagnostyczne planu: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (zainstalowany zestaw plików wtyczki), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informacyjny) | `1` |
| Uruchomienie sesji | `OL_E_PLAN_NOT_RUNNABLE` (ponowne planowanie przy uruchomieniu, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (zwykle zgłaszane przez planowanie jako komunikat diagnostyczny planu, kod wyjścia `1`; `7` tylko wtedy, gdy pliki wtyczki zmienią się między planowaniem a uruchomieniem), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (sesja `Failed`, `1`) | `1`, `7` |
| Nieobsłużony wyjątek w dowolnym miejscu | klasyfikowany przez `CliProgram.Classify` (patrz [kody wyjścia](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Środowisko

| Zmienna | Ustawiana przez | Efekt |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: wyjście konsoli wyciszone, błędy w oknach komunikatu, `/silent` nie jest ponownie delegowane. |

<a id="see-also"></a>
## Zobacz też

[Przykłady CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [kody wyjścia](exit-codes.md) · [błędy](errors.md) · [lokalne sterowanie](local-control.md) · [obszar powiadomień Windows](windows-tray.md) · [sterowanie runtime](runtime-control.md) · [możliwości](capabilities.md) · [LaunchSpec](launchspec.md) · [profile sesji](session-profiles.md) · [pakowanie](packaging.md) · [zgodność](compatibility.md) · [znane ograniczenia](known-limitations.md) · [publiczne API](public-api.md)
