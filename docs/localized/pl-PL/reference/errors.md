# Kody błędów i komunikatów diagnostycznych

<!-- l10n: source=reference/errors.md source-sha256=a3335bf22fcb58419c25d20745f040077113f1db8f973e0cb47d0469e852c5f9 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/errors.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona jest normatywnym opisem każdego kodu z `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 kodów błędów `OL_E_` i 4 ostrzeżeń `OL_W_`, pogrupowanych według kategorii katalogu, a także informacyjnych kodów diagnostycznych, które nie są błędami. Dla każdego kodu podano, w którym miejscu bieżący kod go zgłasza, co oznacza, w jaki sposób trafia do wywołującego (zgłoszony wyjątek, pole wyniku, komunikat diagnostyczny, odpowiedź płaszczyzny sterowania lub koperta CLI) oraz co należy zrobić. Znaczenia zostały ustalone na podstawie miejsc zgłaszania; jeśli kod jest zdefiniowany, ale nie ma obecnie ścieżki zgłaszania, strona wyraźnie to zaznacza.

Powiązane strony: [publiczne API](public-api.md), [kody wyjścia](exit-codes.md), [LaunchSpec](launchspec.md), [cykl życia sesji](../concepts/session-lifecycle.md), [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md), [lokalna płaszczyzna sterowania](local-control.md), [sterowanie runtime](runtime-control.md), [profile sesji](session-profiles.md), [stała wtyczka](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Jak kody docierają do wywołującego

| Powierzchnia | Znaczenie |
| --- | --- |
| Zgłaszany wyjątek | Wyjątek, którego `Message` zaczyna się od kodu (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). CLI wyodrębnia kod z komunikatu i mapuje go na kod wyjścia (`CliProgram.Classify`). |
| Komunikat diagnostyczny planu | `LaunchDiagnostic` w `SessionPlan.Diagnostics`; każdy kod `OL_E_` powoduje, że `IsRunnable` ma wartość false (CLI: plan `NOT RUNNABLE`, kod wyjścia 1). |
| Komunikat diagnostyczny sesji | `LaunchDiagnostic` w `SessionStatus.Diagnostics`; stan sesji to `Failed` (kod wyjścia CLI 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` i `OL_E_RESTORE_FAILED` opakowują w swoim komunikacie kod wewnętrzny. |
| Wynik runtime | `RuntimeCommandResult.ErrorCode` przy `Succeeded = false`. |
| Szczegół runtime | `RuntimeCommandResult.ErrorCode` to konkretny kod, a `Values["detail"]` to komunikat (wraz z `Values["exception"]`, typem .NET). Beta 4: typowany kod po stronie wtyczki dociera do API i CLI bez zmian; tylko błąd bez skatalogowanego kodu staje się `OL_E_RUNTIME_OPERATION_FAILED`. |
| Odpowiedź sterowania | `ErrorCode` odpowiedzi lokalnej płaszczyzny sterowania (`LocalControlResponse`). |
| Koperta CLI | `error.code` w kopercie `--json` albo `<code>: <message>` na konsoli; podany jest kod wyjścia. |
| Telemetria | Nazwa zdarzenia runtime, którą host mapuje na komunikat diagnostyczny sesji. |

## Cli

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Nieobsłużony wyjątek `OperationCanceledException` (Ctrl+C lub anulowane oczekiwanie klienta). | Koperta CLI, kod wyjścia 7 | Ponowić polecenie. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Nieobsłużony wyjątek bez kodu `OL_E_`: niepoprawny JSON w `/spec`, wymagany element specyfikacji o wartości null, nieoczekiwana usterka. | Koperta CLI, kod wyjścia 10 | Przeczytać komunikat i `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; poprawić dane wejściowe; zgłosić, jeśli przyczyna jest niewyjaśniona. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Nieobsłużony wyjątek `TimeoutException` bez kodu; albo klient lokalnego sterowania połączył się z właścicielem, który nie odpowiedział w limicie czasu (właściciel istnieje, więc nie jest to zgłaszane jako `OL_E_NO_ACTIVE_SESSION`). (Przekroczenia limitu czasu skrzynki runtime zamiast tego niosą `OL_E_RUNTIME_REQUEST_TIMEOUT`). | Koperta CLI, odpowiedź sterowania; kod wyjścia 5 z `Classify`, kod wyjścia 7 dla odpowiedzi sterowania | Ponowić; sprawdzić, czy OMSI i właściciel reagują. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | Obok `OmsiLaunch.exe` nie ma `OmsiLaunchW.exe`. | Koperta CLI, kod wyjścia 7 | Ponownie zainstalować pakiet. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` dla `OmsiLaunchW.exe` nie zwrócił procesu. | Koperta CLI, kod wyjścia 7 | Sprawdzić pliki pakietu i uprawnienia; uruchomić bez `/silent`, aby zobaczyć błąd. |

## Compatibility

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` przy `plugin.build.invalid` | Możliwości uruchamiania zostały rozwiązane, ale aktywny formularz Start lub jedno z jego miejsc hooków nie jest w oczekiwanym stanie, gdy wtyczka uzbraja start bez interfejsu (np. inna wtyczka już go załatała). | Komunikat diagnostyczny sesji (`Failed`) | Usunąć konfliktującą wtyczkę; zob. [zgodność](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Wtyczka (`plugin.capability.unresolved` przy starcie; operacje runtime) | Natywna możliwość, której wymaga żądanie, nie mogła zostać rozwiązana i zweryfikowana w tym buildzie OMSI, więc jest niedostępna. Przy starcie sesja kończy się błędem przed jakimkolwiek hookiem, a pliki są przywracane; operacja runtime jest odrzucana bez dotykania pamięci OMSI. Nazywa ją `Data["capability"]` (runtime) lub wartość `capabilities` telemetrii. | Komunikat diagnostyczny sesji (`Failed`); wynik runtime | Zob. [zgodność](compatibility.md): możliwość nie jest dostępna w tym buildzie. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` niedostępne) | Instalacja nie jest instalacją OMSI 2: brak `Omsi.exe`, nie jest to 32-bitowy obraz PE x86, brak wersji OMSI 2 (2.x) albo w katalogu głównym nie ma katalogu `maps\` lub `Vehicles\`. Skrót (hash) pliku wykonywalnego nie jest kryterium. | Komunikat diagnostyczny planu | Wskazać OmsiLaunch katalog główny instalacji OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` niedostępne); zgłaszany także przez `CurrentWindowsX64Platform.ValidateCurrent`, której usługa nie wywołuje | System inny niż Windows 10 lub nowszy albo system operacyjny lub proces hosta nie jest x64. | Komunikat diagnostyczny planu (kod wyjścia CLI 3 przy zgłoszeniu wyjątku) | Uruchamiać w 64-bitowym systemie Windows 10 lub nowszym. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Wyłącznie `CurrentWindowsX64Platform.ValidateCurrent` | Architektura systemu operacyjnego lub hosta nie jest x64. Usługa nie wywołuje `ValidateCurrent`; brak obecnej ścieżki zgłaszania. | Zgłaszany (`PlatformNotSupportedException`) wyłącznie przez tę metodę | Zob. źródło `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

## Content

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Walidacja: NEW_MAP bez `EntrypointIdentity` i z nieustawionym lub ujemnym `PresentedEntrypointIndex`. Planista: mapa nie ma punktu wejścia o żądanym identyfikatorze ani dokładnej etykiecie (z rozróżnieniem wielkości liter). | Komunikat diagnostyczny planu | Ustawić `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) lub identyfikator z `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | Pogody startowej nie można zastosować: ustawienie wstępne, które nie jest plikiem z folderu `weather` instalacji, albo ICAO, które nie składa się z czterech liter lub cyfr. | Komunikat diagnostyczny planu | Użyć zainstalowanego ustawienia wstępnego lub poprawnego kodu stacji. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | Żądany punkt wejścia ma tę samą etykietę co wcześniejszy rekord mapy; OMSI prezentuje tylko pierwszy rekord każdej etykiety, więc nie można go wybrać przy starcie. | Komunikat diagnostyczny planu | Wybrać pierwszy rekord z tą etykietą albo inny punkt wejścia. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` niedostępne) | Mapa NEW_MAP została rozpoznana, ale brak zarówno prezentowanego indeksu, jak i identyfikatora. Zawsze towarzyszy `OL_E_ENTRYPOINT_NOT_FOUND`. | Komunikat diagnostyczny planu | Jak wyżej. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` nie jest zainstalowanym plikiem `Vehicles\...\*.hof`. | Komunikat diagnostyczny planu | Użyć identyfikatora z `/list:hofs`. (Pola pojazdu gracza i tak są w tym buildzie niemożliwe do uruchomienia). |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Źródło [zarządzania HOF](hof-management.md) (kolekcja, katalog, pojazd lub plik) nie istnieje, nie zawiera `.hof` albo brakuje w nim pliku wymienionego w `Files`. | Komunikat diagnostyczny planu; błąd uruchomienia | Poprawić źródło lub listę plików. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Docelowy plik lub folder pojazdu HOF nie jest zainstalowany. | Komunikat diagnostyczny planu; błąd uruchomienia | Użyć identyfikatora pojazdu z `/list:vehicles` lub istniejącego folderu poniżej `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Ścieżka HOF wykracza poza instalację, cel nie leży poniżej `Vehicles\`, nazwa kolekcji nie jest zwykłą nazwą albo przechodzi przez junction/dowiązanie symboliczne. | Komunikat diagnostyczny planu; błąd uruchomienia | Użyć ścieżek względnych wobec instalacji bez `..` i bez dowiązań. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Dwie operacje HOF zapisują różne bajty do jednego pliku albo kopia lustrzana usuwa plik, który zapisuje inna operacja. | Komunikat diagnostyczny planu; błąd uruchomienia | Zmienić kolejność, podzielić lub usunąć konfliktujące operacje. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Operacja HOF bez celów lub źródła, źródło plikowe inne niż `.hof`, plik docelowy inny niż `.bus`/`.ovh` albo przekroczony limit rozmiaru/liczby (8 MiB na plik, 256 plików na operację, 256 MiB na sesję). | Komunikat diagnostyczny planu; błąd uruchomienia | Poprawić operację. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Walidacja: NEW_MAP z nieustawionym `MapIdentity` lub o postaci innej niż `maps\<dir>\global.cfg`. Planista: mapa nie jest zainstalowana. | Komunikat diagnostyczny planu | Użyć identyfikatora z `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Nieobsłużony wyjątek `FileNotFoundException`/`DirectoryNotFoundException` bez kodu, np. `/list:repaints` z nieznanym `/vehicle-scope` albo `/list:entrypoints` z nieznanym `/map`. | Koperta CLI, kod wyjścia 6 | Poprawić identyfikator. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` nie jest elementem `.cti` danego modelu (sprawdzane tylko wtedy, gdy ustawiono `Model`). | Komunikat diagnostyczny planu | Użyć identyfikatora z `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | Mapa, do której odwołuje się wybrany plik `.osn`, nie jest zainstalowana. | Komunikat diagnostyczny planu | Zainstalować mapę lub wybrać inną sytuację. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION bez `SituationIdentity` albo plik `.osn` nie jest zainstalowany. | Komunikat diagnostyczny planu | Użyć identyfikatora z `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` nie jest zainstalowanym plikiem `Vehicles\...\*.bus`. | Komunikat diagnostyczny planu | Użyć identyfikatora z `/list:vehicles`. |

## Installation

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | Dzierżawa instalacji (`Local\OmsiLaunch.Installation.<hash>`) jest utrzymywana przez innego właściciela w tej sesji logowania albo proces OMSI zapisany w dzienniku (PID + czas utworzenia + ścieżka pliku wykonywalnego; lub dowolny `Omsi.exe` z katalogu głównego w przypadku dziennika za etapem `HandoffCreated` bez PID) nadal działa. | Start: komunikat diagnostyczny sesji przez `OL_E_START_SESSION`. Odzyskiwanie: zgłaszany wyjątek (`InvalidOperationException` / `IOException`). Kod wyjścia CLI 7. | Zatrzymać innego właściciela (`session stop`) lub poczekać na zakończenie OMSI, a następnie ponowić albo wykonać `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` jest puste. | Komunikat diagnostyczny planu | Przekazać katalog instalacji. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` niedostępne); także `ValidateCurrent` | Katalog główny nie istnieje, ma atrybut tylko do odczytu lub nie zawiera katalogu `plugins\`. | Komunikat diagnostyczny planu | Wskazać rzeczywistą, zapisywalną instalację OMSI. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (planowanie i start) | Zainstalowany plik `plugins\OmsiLaunch.*` różni się od hasha w `release-manifest.json` (lub od referencyjnego zestawu plików wtyczki, gdy manifestu brak). | Komunikat diagnostyczny planu (plan niemożliwy do uruchomienia, kod wyjścia CLI 1); komunikat diagnostyczny sesji przez `OL_E_START_SESSION` tylko wtedy, gdy pliki zmienią się między planowaniem a startem | Ponownie zainstalować pakiet OmsiLaunch, aby `plugins\` i manifest były zgodne. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (planowanie i start) | Manifest nie zawiera wpisu dla wymaganego pliku wtyczki. | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji przez `OL_E_START_SESSION` przy takim samym wyścigu jak wyżej | Ponownie zainstalować pakiet. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (planowanie i start) | W instalacji OMSI brakuje wymaganego pliku `plugins\OmsiLaunch.*` albo plik `plugins/` wymieniony w `release-manifest.json` nie jest zainstalowany. | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji przez `OL_E_START_SESSION` przy takim samym wyścigu jak wyżej | Zainstalować zestaw plików stałej wtyczki ([instalacja](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Wyłącznie `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` ma wartość false. Nie jest wywoływana przez usługę; brak obecnej ścieżki zgłaszania. | Zgłaszany wyłącznie przez tę metodę | Zob. źródło. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` jest pusty, nie jest JSON-em, nie ma tablicy `files` albo zawiera wpis bez `path`/`sha256`, hash, który nie składa się z 64 cyfr szesnastkowych, ścieżkę bezwzględną, zawierającą `:`, pusty segment, segment `.` lub `..`, albo ścieżkę wymienioną dwukrotnie (porównanie bez rozróżniania wielkości liter, `/` i `\` są równoważne). BOM UTF-8 jest akceptowany. | Planowanie: opakowany w `OL_E_RUNTIME_ARTIFACT_MISSING`; start: przez `OL_E_START_SESSION` | Ponownie zainstalować pakiet. |

## InvalidArgument

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Walidacja: ustawiono `Date.Value`/`Time.Value`, choć tryb nie jest `Explicit`. CLI: nieznana flaga, brak wartości, błędna liczba całkowita lub zakres, `/saved` w połączeniu z `/map`/`/entrypoint`, nieznana ścieżka polecenia, dowolny `ArgumentException`/`FormatException` bez kodu. | Komunikat diagnostyczny planu; koperta CLI, kod wyjścia 2 | Poprawić argument. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (start) | Wartość ustawienia semantycznego jest poza zakresem, nie jest wartością logiczną, nie należy do dozwolonego zbioru lub jest niepoprawnie sformatowana (`graphics.particles` wymaga czterech pól). Wartości nie są walidowane na etapie planowania. | Komunikat diagnostyczny sesji przez `OL_E_START_SESSION` | Użyć wartości z [tabeli ustawień](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | Klucz istnieje, ale nie jest zapisywalny (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Komunikat diagnostyczny planu; kod wyjścia CLI 2 | Usunąć klucz. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | Klucza nie ma w `ConfigurationCatalog`. | Komunikat diagnostyczny planu; kod wyjścia CLI 2 | Użyć klucza z katalogu. |

## LaunchSpec

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | Element główny nie jest obiektem JSON albo deserializacja nie utworzyła rekordu. | Zgłaszany (`InvalidDataException`), kod wyjścia CLI 2 | Poprawić plik ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | Plik `/spec` nie istnieje. | Zgłaszany (`FileNotFoundException`), kod wyjścia CLI 6 | Sprawdzić ścieżkę. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | Plik przekracza 1 MiB. | Zgłaszany (`InvalidDataException`), kod wyjścia CLI 2 | Zmniejszyć plik. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Element, który nie jest publiczną właściwością rekordu w danym miejscu; komunikat `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Zgłaszany (`InvalidDataException`), kod wyjścia CLI 2 | Usunąć element lub zmienić jego nazwę. |

## LocalControl

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Procedura obsługi właściciela (`OwnerSession`) | Polecenie nie jest `session.status`, `session.events`, `session.stop` ani `runtime.execute` z argumentem `operation`. | Odpowiedź sterowania; kod wyjścia CLI 7 | Użyć obsługiwanego polecenia. |
| `OL_E_CONTROL_FAILED` | Klient CLI (`ReportForwarded`, `CliEventWatch`) | Właściciel odpowiedział `Ok = false` bez kodu błędu. | Koperta CLI, kod wyjścia 7 | Przeczytać komunikat; sprawdzić konsolę/diagnostykę właściciela. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | Procedura obsługi właściciela zgłosiła wyjątek, którego komunikat nie zawiera kodu `OL_E_` (np. sesja była już zamknięta), albo nie udało się zserializować odpowiedzi procedury obsługi. | Odpowiedź sterowania | Odczytać `session status`; ponownie uruchomić właściciela, jeśli już nie działa. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (obie strony) | Prefiks długości ujemny lub większy niż 64 KiB (w tym zbyt duża ramka żądania), pusta ramka, JSON `null`, żądanie bez `Command` albo JSON, którego nie udało się zdekodować. | Odpowiedź sterowania / koperta CLI | Stosować udokumentowany protokół ([lokalna płaszczyzna sterowania](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (klient) | Zserializowane żądanie samego klienta przekracza 64 KiB. Zgłaszane wywołującemu; nic nie zostaje wysłane. | Odpowiedź sterowania / koperta CLI | Zmniejszyć żądanie. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (właściciel) | Odpowiedź właściciela nie mieści się w ramce 64 KiB. Zamiast porzucić odpowiedź, właściciel odpowiada tym typowanym błędem. `session.status` i `session.events` nigdy do tego nie prowadzą: ich historia zdarzeń jest przycinana od najstarszych wpisów, aby się zmieściła. | Odpowiedź sterowania / koperta CLI | Ponowić; w przypadku zdarzeń odczytywać je częściej. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | `ProtocolVersion` żądania jest różne od `0.1`; odpowiedzi właściciela nie udało się zdekodować lub była pusta; właściciel zamknął połączenie bez odpowiedzi lub połączenie zostało przerwane po nawiązaniu; właściciel nie podał `SessionId`. | Odpowiedź sterowania / koperta CLI | Uzgodnić wersje klienta i właściciela; odczytać `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Procedura obsługi właściciela | `session.stop` lub `runtime.execute` bez `session_id` równego aktywnej sesji. | Odpowiedź sterowania; kod wyjścia CLI 7 | Najpierw odczytać `session.status` i powiązać żądanie z sesją (CLI robi to automatycznie). |

## Other

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | Przekazany plan ma `IsRunnable = false` albo ponowne planowanie przy starcie daje plan niemożliwy do uruchomienia (zmieniony `Omsi.exe`, usunięta zawartość, brak zestawu plików wtyczki); komunikat wymienia bieżące kody `OL_E_`. | Zgłaszany (`InvalidOperationException`); kod wyjścia CLI 1 | Zaplanować ponownie i usunąć przyczyny wymienionych komunikatów diagnostycznych. |

## Presentation

Wszystkie zgłaszane przez `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Na etapie planowania są opakowywane w `OL_E_SESSION_PRESENTATION_INVALID` (komunikat zawiera kod); przy starcie docierają przez `OL_E_START_SESSION`.

| Kod | Znaczenie / typowa przyczyna | Co zrobić |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | Plik `.itx` jest pusty, ma nieparzystą liczbę niepustych wierszy albo wiersz URL nie jest bezwzględnym adresem URL `http`/`https`. | Stosować pary wierszy URL/cel. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (rozwiązywana względem katalogu roboczego procesu) nie istnieje. Zgłaszany jako `FileNotFoundException`. | Przekazać ścieżkę istniejącego pliku `.itx`. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` ma wartość `Override` bez `OverrideProfilePath`. Kod wyjścia CLI 2 przy zgłoszeniu wyjątku. | Podać `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Wiersz celu jest ścieżką bezwzględną, zawiera `..`, zaczyna się od `\`, wskazuje poza instalację, nie zawiera składnika `Texture\` lub przechodzi przez złącze (junction)/dowiązanie symboliczne. | Stosować względne cele `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` nie istnieje. | Poprawić katalog. |
| `OL_E_SPLASH_ASSET_MISSING` | W katalogu zasobów brakuje `ENG.bmp` lub `<LANG>.bmp` albo podczas wypełniania `.omsilaunch\assets\splash` brakuje spakowanego pliku `assets\splash\<LANG>.bmp`. | Dostarczyć pliki BMP / ponownie zainstalować pakiet. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Plik BMP ekranu startowego nie jest 24-bitową bitmapą `BM` o wymiarach 640×480. | Przekonwertować obraz. |

## Process

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (ścieżki błędu startu i usterki nadzorcy) | Kończenie OMSI lub oczekiwanie na OMSI podczas sprzątania po błędzie zgłosiło wyjątek; dalej następuje komunikat wewnętrzny. | Komunikat diagnostyczny sesji (dodany do sesji `Failed`) | Upewnić się, że nie pozostał żaden `Omsi.exe`, a następnie wykonać `/recover`, jeśli transakcja oczekuje (istnieje dziennik). |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` zakończyło się niepowodzeniem tuż po `CreateProcessW` (`Win32=<code>`); proces jest kończony. | Komunikat diagnostyczny sesji przez `OL_E_START_SESSION` | Ponowić; sprawdzić program antywirusowy/uprawnienia. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI zakończył działanie przed `gameplay.entered` (awaria, zamknięto okno dialogowe błędu OMSI, zamknięto okno). | Komunikat diagnostyczny sesji (`Failed`); wykonywane jest przywracanie | Sprawdzić własne logi OMSI i `logfile.txt`; w `RuntimeEvents` odszukać ostatnie zdarzenie wtyczki. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` zakończyło się niepowodzeniem (`Win32=<code>` w komunikacie). | Komunikat diagnostyczny sesji przez `OL_E_START_SESSION` | Usunąć przyczynę błędu Win32 (brakujący plik, odmowa dostępu, zasady). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | Pętla nadzorcy zgłosiła wyjątek (odczyt telemetrii, oczekiwanie na proces/kończenie procesu, zapis dziennika); OMSI jest kończony i podejmowana jest próba przywrócenia. | Komunikat diagnostyczny sesji (`Failed`) | Przeczytać komunikat wewnętrzny i log hosta. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` zakończyło się niepowodzeniem (`Win32=<code>`). | Wewnątrz komunikatów `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Zakończyć OMSI ręcznie, a następnie wykonać `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` na uchwycie procesu zakończyło się niepowodzeniem. | Wewnątrz komunikatów `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Jak wyżej. |

## Runtime

„Szczegół runtime” (runtime detail) oznacza konkretny kod w `ErrorCode`, komunikat w `Values["detail"]` i typ wyjątku w `Values["exception"]`.

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` z `preset`, gdy `family` ma wartość 2 (zewnętrzna) lub 3 (mapa); ustawienia wstępne istnieją tylko dla kierowcy (0) i pasażera (1). | Szczegół runtime | Pominąć `preset` lub użyć rodziny 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Tryb `Date`/`Time` `Explicit` bez wartości lub ze składnikami poza zakresem. Nazwa jest historyczna; jest to błąd walidacji na etapie planowania. | Komunikat diagnostyczny planu | Poprawić wartość (i pamiętać, że jawna data/godzina jest w tym buildzie niemożliwa do uruchomienia). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: ścieżka `.bus` nie istnieje w katalogu roboczym OMSI (sprawdzane przed wywołaniem natywnym, aby OMSI nie mógł podstawić zastępczego pojazdu). | Szczegół runtime | Użyć identyfikatora z `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | jak wyżej | Natywne MakeVehicle zmieniło kolekcję pojazdów drogowych o więcej niż jeden obiekt. | Szczegół runtime (`native_status`, liczniki w komunikacie) | Zgłosić; utworzone obiekty pozostają do końca sesji. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | jak wyżej | Kolekcja się nie zmieniła; OMSI po cichu odrzucił pojazd. | Szczegół runtime | Sprawdzić plik `.bus`; spróbować innego modelu. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | jak wyżej | Dowolny inny niezerowy status natywny. | Szczegół runtime | Zgłosić wraz z licznikami z komunikatu. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | Sprofilowane wywołanie PlaceRandomBus zwróciło status niepowodzenia. | Szczegół runtime | Ponowić, gdy rozgrywka się ustabilizuje; zgłosić. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; kontrole po stronie wtyczki (`time.set` bez `hour`/`minute`/`second`; `camera.set` bez `family`/`field_of_view`; `camera.lock` bez parsowalnego `family`; operacje na pojazdach/krzywych) | Brak wymaganego argumentu lub jest on pusty. | Wynik runtime (rejestr; kod wyjścia CLI 2) lub szczegół runtime (wtyczka) | Podać argument ([sterowanie runtime](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | Nie można załadować referencyjnego katalogu/pliku zestawu plików wtyczki ani mostka natywnego podanego w `OmsiLaunchRuntimePaths` (może opakowywać `OL_E_RELEASE_MANIFEST_INVALID`). | Komunikat diagnostyczny planu | Uruchamiać z nienaruszonego pakietu. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, harness INTERNAL) | Bazowe `time.read`/`camera.read` zakończyło się niepowodzeniem, więc test zapisu został pominięty. | Wyłącznie artefakt partii (batch) | Nieprzeznaczony dla użytkowników. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` jest pusty, dłuższy niż 240 znaków, zawiera NUL lub `..`, nie zaczyna się od `Vehicles\` lub nie kończy się na `.bus`. | Szczegół runtime | Przekazać `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | brak (zachowany dla zgodności) | Nie jest już emitowany. Wcześniejsze buildy zgłaszały go, gdy anulowane żądanie pozostawało w slocie; obecnie każda ścieżka zakończenia żądania resetuje slot, a pozostałe żądanie lub odpowiedź znalezione na początku nowego żądania są usuwane. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | Skrzynka runtime została zlikwidowana, ponieważ sesja się kończy. | Zgłaszany (`InvalidOperationException`) | Brak działań; sesja się zakończyła. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | Slot skrzynki zawierał wartość stanu inną niż bezczynny, żądanie lub odpowiedź (uszkodzenie). Slot jest resetowany i zgłaszany jest błąd; następne żądanie działa normalnie. | Zgłaszany (`InvalidDataException`) | Ponowić; zgłosić, jeśli problem się utrzymuje. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | Wskaźnik bloku stałych pojazdu ma wartość null. | Szczegół runtime | Pojazd nie ma stałych; nie trzeba nic robić. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` nie występuje w tabeli stałych pojazdu. | Szczegół runtime | Najpierw wyświetlić listę stałych. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | Obiekt utworzony przez spawn ma VMT poza zakresem obrazu OMSI. | Szczegół runtime | Zgłosić. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | jak wyżej | Utworzony obiekt nie znajduje się w kolekcji pojazdów drogowych. | Szczegół runtime | Zgłosić. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Dwa kolejne punkty krzywej mają tę samą wartość X. | Szczegół runtime | Problem z zawartością w krzywej pojazdu. |
| `OL_E_RUNTIME_CURVE_EMPTY` | jak wyżej | Krzywa nie ma punktów. | Szczegół runtime | Jak wyżej. |
| `OL_E_RUNTIME_CURVE_INVALID` | jak wyżej | Żaden segment krzywej nie zawiera `x`. | Szczegół runtime | Obliczać wartość w dziedzinie krzywej. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | jak wyżej | `name` jest nieznana lub wskaźnik jej funkcji ma wartość null. | Szczegół runtime | Najpierw wyświetlić listę krzywych. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | Wskaźnik definicji pojazdu ma wartość null. | Szczegół runtime | Uchwyt wskazuje pojazd bez danych definicji. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (tryb właściciela) | Obok pliku wykonywalnego brakuje `plugins\OmsiLaunch.Plugin.opl` lub `plugins\OmsiLaunch.Native.x86.dll`. | Koperta CLI, kod wyjścia 7 | Ponownie zainstalować pakiet. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | Brak `handle` lub jest on pusty dla `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (zwykle rejestr odrzuca je wcześniej z `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Szczegół runtime | Podać uchwyt. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | Uchwyt jest nieznany, obiekt opuścił kolekcję, generacja adresu się zwiększyła albo odcisk obiektu (VMT + tożsamość definicji/modelu) zmienił się, ponieważ adres został ponownie użyty. Pozostały martwy punkt: ta sama klasa i model utworzone ponownie pod tym samym adresem między dwoma odczytami listy. | Szczegół runtime | Ponownie wykonać `road-vehicles.list`/`humans.list` i użyć nowego uchwytu. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; mechanizm awaryjny `D3DRuntimeApi` | Ogólne opakowanie błędów po stronie wtyczki; `Values["detail"]` zawiera komunikat (często z bardziej szczegółowym kodem), a `Values["exception"]` typ wyjątku. Na wyjątek, który wydostanie się z operacji wewnątrz dyspozytora skrzynki, również odpowiada się tym kodem (bez wartości), zamiast pozostawiać żądanie bez odpowiedzi. | Wynik runtime | Przeczytać `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | Wtyczka nie ma implementacji operacji, na którą zezwolił rejestr (rozbieżność wersji rejestru i wtyczki). | Szczegół runtime | Ponownie zainstalować spójny pakiet. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | Operacji nie ma w `PublicRuntimeOperationIds`; dotyczy to także każdej operacji `internal.*`. Sprawdzane przed wyszukaniem sesji. | Wynik runtime; odpowiedź sterowania; kod wyjścia CLI 2 | Użyć publicznego identyfikatora operacji. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` z `preset`, gdy nie ma pojazdu gracza (sesje headless go nie mają). Zgłaszany także jako `code` zdarzenia `camera.lock.degraded`, gdy ponowne zastosowanie się nie powiedzie. | Szczegół runtime / zdarzenie runtime | Zablokować kamerę bez ustawienia wstępnego lub użyć sesji z pojazdem gracza. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Udany wynik D3D nie zawierał wartości albo nieznany ciąg stanu urządzenia. | Zgłaszany (`OmsiRuntimeException`) | Uzgodnić wersje hosta i wtyczki. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | Slot zawiera nieaktualną odpowiedź z tym samym identyfikatorem żądania co nowe żądanie. Nieaktualna odpowiedź jest usuwana przed zgłoszeniem błędu. | Zgłaszany (`InvalidOperationException`) | Używać ściśle rosnących identyfikatorów żądań. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Brak odpowiedzi w czasie `timeout`; slot jest resetowany, a spóźniona odpowiedź odrzucana. | Zgłaszany (`TimeoutException`); kod wyjścia CLI 5 | Ponowić z dłuższym limitem czasu; sprawdzić, czy OMSI nie jest zablokowany (modalne okno dialogowe, ładowanie). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | Koperta odpowiedzi jest uszkodzona, ma błędną długość (ujemną, zerową lub większą niż slot), obcy identyfikator sesji lub inny identyfikator żądania. Slot jest resetowany przed zgłoszeniem błędu, więc następne żądanie działa normalnie. | Zgłaszany (`InvalidDataException`) | Ponowić; zgłosić, jeśli problem się utrzymuje. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (wtyczka) | `cursor` operacji listowej jest zniekształcony lub należy do innego rodzaju listy. | Szczegół runtime | Przekazać `next_cursor` poprzedniej strony tej samej operacji albo go pominąć. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | Zserializowany wynik przekracza skrzynkę 64 KiB. Stronicowane wyniki list są zamiast tego skracane do rozmiaru skrzynki i kontynuowane przez `next_cursor`; żaden wynik listy nie dociera do tego kodu. | Wynik runtime | Użyć węższej operacji lub mniejszego `limit`. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | Wskaźnik definicji skryptu lub stanu pojazdu ma wartość null. | Szczegół runtime | Pojazd nie ma obiektów skryptu. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; skrzynka wtyczki | `RuntimeCommand.SessionId` różni się od identyfikatora sesji uchwytu (zgłaszane przez hosta) albo żądanie trafiło do wtyczki powiązanej z inną sesją (zwracane przez wtyczkę jako typowany wynik). | Zgłaszany (`InvalidOperationException`) / runtime result | Budować polecenie z `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` zdarzenia `world.failed`) | Pogody startowej nie można było zastosować po uruchomieniu świata: OMSI nie ma skonfigurowanej stacji ICAO dla `/weather-real` albo jego obiekty pogody są niedostępne. Sesja kończy się błędem. | Błąd sesji | Skonfigurować stację w OMSI albo użyć `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Klucz `weather.set` wskazuje wartość, którą OMSI wyprowadza przy każdym takcie pogody (wilgotność, współczynniki światła, przezroczystość chmur, flaga opadów, mokry grunt); nic nie jest zapisywane. | Wynik runtime | Ustawić klucz źródłowy wskazany w `detail` (np. `dew_point` zamiast `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Nieznany klucz `weather.set`; nic nie jest zapisywane. | Wynik runtime | Użyć klucza wymienionego w [możliwościach](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` nie występuje w tabeli zmiennych tekstowych. | Szczegół runtime | Najpierw wyświetlić listę zmiennych tekstowych. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Wartość logiczna pogody nie jest `true`/`false`/`1`/`0`. Obecnie nieosiągalny (zob. wyżej). | Szczegół runtime (zdefiniowany) | Zob. źródło. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` nie jest liczbą całkowitą, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` może mieć wartość -1), `scheduled` 0..1; `vehicle.variable.set`: `value` nie jest skończona; `vehicle.curve.evaluate`: `x` nie jest skończone. | Szczegół runtime | Użyć wartości z zakresu. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` nie występuje w tabeli zmiennych liczbowych. | Szczegół runtime | Najpierw wyświetlić listę zmiennych. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | Slot zmiennej lub adres wartości ma wartość null. | Szczegół runtime | Zmienna nie jest zmaterializowana dla tego pojazdu. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Skalary zegara zostały zapisane, ale sprofilowane natywne wywołanie SetTime zwróciło niepowodzenie. | Szczegół runtime | Ponowić; odczytać wartość z powrotem za pomocą `time.read`. |

## RuntimeD3D

Wszystkie zgłaszane przez `CurrentRuntimeControl` (mapowanie statusu natywnego w `ThrowD3D`; `detail` zawiera operację i HRESULT, `native_status` – status liczbowy). Powierzchnia: runtime result z kodem w `ErrorCode`; `D3DRuntimeApi` zgłasza go ponownie jako `OmsiRuntimeException`.

| Kod | Status natywny / przyczyna | Co zrobić |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: utrata urządzenia Direct3D. | Poczekać na `d3d.restored`; utworzyć tekstury ponownie (zmieniła się generacja). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: brakujące lub nieprawidłowe `width`, `height`, `level`, `x`, `y`, `format` lub `handle` (zakresy: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Poprawić argumenty. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` nie jest prawidłowym ciągiem Base64 lub przekracza 48 KiB. | Wysyłać mniejsze prostokąty. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6 albo nieznana nazwa `format` (prawidłowe: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Użyć formatu z listy. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Dowolny inny status; HRESULT znajduje się w `detail`. | Zgłosić wraz z HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: urządzenie nie jest gotowe (przed pierwszą klatką lub podczas zatrzymywania). | Ponowić po `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: trwa reset urządzenia. | Ponowić po `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: uchwyt tekstury został już zwolniony. | Nie używać ponownie zwolnionych uchwytów. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: uchwyt należy do poprzedniej generacji urządzenia; albo ciąg uchwytu nie ma postaci `d3dtex-<session>-<hex>` / jest zerowy. | Utworzyć teksturę ponownie. |

## Session

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` przy `plugin.request.unsupported` | Plan: zażądano `LastMapState`, `EntrypointIdentity`, trybu daty/godziny/roku, trybu pogody, pól pojazdu gracza lub dokumentów wejściowych (`Requested capability unavailable: <name>`). Telemetria: wtyczka odrzuciła przekazanie (handoff) (nie może się to zdarzyć dla planu możliwego do uruchomienia). | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji | Usunąć nieobsługiwane żądanie ([znane ograniczenia](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` przy `headless.arm.failed` | Wtyczka nie mogła uzbroić jednorazowego haka startu headless w OMSI. | Komunikat diagnostyczny sesji (`Failed`) | Zweryfikować build; zgłosić. |
| `OL_E_NO_ACTIVE_SESSION` | Klient CLI (`ReportForwarded`, `CliEventWatch`) | Żaden właściciel nie odpowiada na potoku sterowania instalacji (brak sesji albo właściciel nadal się uruchamia lub przeprowadza walidację). | Koperta CLI, kod wyjścia 4 | Uruchomić sesję lub poczekać, aż osiągnie stan `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | Upłynął `StartupTimeoutSeconds` przed `plugin.started` (OMSI nie załadował `plugins\OmsiLaunch.Plugin.opl` lub utknął przed inicjalizacją wtyczki). | Komunikat diagnostyczny sesji (`Failed`) | Sprawdzić zestaw plików wtyczki, `plugins\OmsiLaunch.Plugin.opl` i plik `logfile.txt` OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` przy `plugin.handoff.invalid` lub niemożliwym do sparsowania JSON-ie telemetrii | Wtyczka nie mogła odczytać/zweryfikować przekazania startowego (wersje 3 do 6, SHA-256) albo wysłała nieprawidłową telemetrię. | Komunikat diagnostyczny sesji (`Failed`) | Uzgodnić wersje hosta i wtyczki (ponownie zainstalować pakiet). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Właściciel już odpowiada na `session.status` dla tej instalacji. | Koperta CLI, kod wyjścia 7 | Używać poleceń klienta (`session status`, `session stop`, polecenia runtime). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | Stan sesji nie jest `Running`. | Zgłaszany (`InvalidOperationException`) | Najpierw wywołać `WaitForAsync(session, SessionState.Running, ...)`. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Nie udało się zbudować planu ekranu startowego/ITX; komunikat zawiera kod prezentacji. | Komunikat diagnostyczny planu | Zob. [Presentation](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (okno dialogowe OmsiLaunchW) | Kod zastępczy wyświetlany, gdy plan uruchomienia jest niemożliwy do uruchomienia lub sesja nie dotarła do rozgrywki, a nie istnieje żaden komunikat diagnostyczny `OL_E_`. | Wyłącznie okno komunikatu | Przeczytać `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` przy `world.situation.failed` | Natywny start zapisanej sytuacji zwrócił niepowodzenie (`native_status` w zdarzeniu). | Komunikat diagnostyczny sesji (`Failed`) | Sprawdzić plik `.osn` i jego mapę. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | Wtyczka się uruchomiła, ale stan `Running` nie został osiągnięty w czasie `StartupTimeoutSeconds`. | Komunikat diagnostyczny sesji (`Failed`) | Zwiększyć `/startup-timeout` dla dużych map; w `RuntimeEvents` sprawdzić ostatnie zdarzenie świata. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Dowolny wyjątek na ścieżce startu; komunikatem jest komunikat wewnętrzny (zwykle zaczynający się od kodu wewnętrznego). | Komunikat diagnostyczny sesji (`Failed`) | Postępować zgodnie z kodem wewnętrznym. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` przy `world.failed` | Natywny start NEW_MAP zwrócił niepowodzenie (`native_status` w zdarzeniu). | Komunikat diagnostyczny sesji (`Failed`) | Sprawdzić mapę, indeks punktu wejścia i logi OMSI. |

## SessionProfile

Wszystkie zgłaszane przez `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) lub `CliInput` jako `SessionProfileException` (`IOException` z `Code`), kod wyjścia CLI 2. Zob. [profile sesji](session-profiles.md).

| Kod | Znaczenie / typowa przyczyna | Co zrobić |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | Katalog `assets` ekranu startowego ustawienia wstępnego lub plik `profile` tekstur internetowych nie istnieje wewnątrz pakietu. | Dodać zasób. |
| `OL_E_SESSION_PROFILE_INVALID` | Naruszenie struktury lub limitu: ponad 256 KiB, nie dokładnie jedno mapowanie główne, kotwice YAML, nieznany klucz, brak wymaganego klucza, wartość nieskalarna tam, gdzie wymagany jest skalar, `id` różne od nazwy katalogu, ustawienia wstępne spoza 1..99 lub zduplikowany `index`, niedodatnie limity czasu, nieobsługiwany tryb pogody/ekranu startowego/tekstur internetowych, data/godzina inna niż `explicit`, nieprawidłowy YAML, błędy parsowania liczb/dat. | Poprawić YAML zgodnie z komunikatem. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` nie jest puste i nie zawiera wybranej mapy (NEW_MAP) ani mapy wybranej sytuacji (SAVED_SITUATION). | Wybrać zgodny świat. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` nie istnieje. | Sprawdzić identyfikator. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Jawny argument CLI dotyczy pola należącego do wybranego profilu/ustawienia wstępnego. | Usunąć flagę lub wybrać inne ustawienie wstępne. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | Identyfikator zawiera `\`, `/`, `:` lub `..`; albo ścieżka zasobu jest bezwzględna, wychodzi poza pakiet lub przechodzi przez złącze (junction)/dowiązanie symboliczne. | Utrzymywać ścieżki wewnątrz pakietu. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | Brak `/predefined-profile-index`, wartość spoza 1..99 lub niezadeklarowana przez profil. | Użyć zadeklarowanego indeksu ustawienia wstępnego. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` nie jest `omsilaunch.session-profile/v1`. | Użyć obsługiwanego schematu. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Klucz ustawienia w ustawieniu wstępnym jest znany, ale nie jest zapisywalny. | Usunąć klucz. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Klucza ustawienia w ustawieniu wstępnym nie ma w katalogu. | Użyć klucza z katalogu. |

## Transaction

Zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md).

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Nieaktualny plik `closecheck` nadal istnieje po `File.Delete`. | Komunikat diagnostyczny sesji przez `OL_E_START_SESSION` | Ręcznie usunąć `<root>\closecheck` (uprawnienia). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Ścieżka, która nie istniała przed sesją, zawiera teraz treść różną od tej, którą zastosowała sesja; nie jest usuwana, a dziennik zostaje zachowany. | Zgłaszany (`IOException`); wewnątrz `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; kod wyjścia CLI 8 | Sprawdzić plik; usunąć go lub przenieść, a następnie wykonać `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (dziennik sprzed odcisków) | Dziennik nie ma odcisku zastosowanej treści dla ścieżki pierwotnie nieistniejącej, która nie jest ścieżką usunięcia, więc nie można udowodnić własności. Przy starcie sesji odzyskiwanie jest odraczane i ponawiane z zaplanowanymi bajtami tej sesji; przez `RecoverPendingAsync` wyjątek jest zgłaszany. | Zgłaszany (`IOException`); kod wyjścia CLI 8 | Uruchomić sesję z tą samą specyfikacją (dostarcza bajty) albo sprawdzić i usunąć plik, a następnie wykonać `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | SHA-256 kopii zapasowej nie odpowiada migawce zapisanej w dzienniku; nic nie jest zapisywane. | Zgłaszany; wewnątrz `OL_E_RESTORE_FAILED`; kod wyjścia CLI 8 | Przywrócić plik z własnej kopii zapasowej; dziennik usunąć dopiero wtedy, gdy nie ma co do tego wątpliwości. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Migawki istnieją w pamięci, ale brakuje `journal.json` (usunięty w trakcie sesji). | Zgłaszany; wewnątrz `OL_E_RESTORE_FAILED` | Ręcznie zweryfikować pliki sesji. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` nadal istnieje po usunięciu (samo przywrócenie się powiodło i zostało zweryfikowane). | Zgłaszany; wewnątrz `OL_E_RESTORE_FAILED`; kod wyjścia CLI 8 | Usunąć `<root>\.omsilaunch\journal.json` (uprawnienia) lub ponownie uruchomić `/recover` (operacja idempotentna). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (błąd startu / nadzorca) | Nie udało się potwierdzić zakończenia OMSI, więc pliki nie zostały zastąpione, póki OMSI może ich jeszcze używać; dziennik zostaje zachowany. | Komunikat diagnostyczny sesji (`Failed`) | Po zakończeniu `Omsi.exe` uruchomić `/recover` (lub następny start przeprowadzi odzyskiwanie automatycznie). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (błąd startu / nadzorca) | `RestoreAsync` zgłosiło wyjątek; komunikat zawiera kod wewnętrzny; dziennik zostaje zachowany. | Komunikat diagnostyczny sesji (`Failed`); kod wyjścia CLI 8 przy zgłoszeniu z `/recover` | Postępować zgodnie z kodem wewnętrznym, a następnie wykonać `/recover`. |

## Ui

Kody natywnych operacji menu pauzy (`ui.pause.*`).

| Kod | Zgłaszany przez | Znaczenie / typowa przyczyna | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_UI_NOT_PAUSED` | `CurrentRuntimeControl` | Operacja wymaga OMSI w pauzie z wyświetlonym natywnym menu pauzy (przejęcie, akcja), a tak nie jest. | Wynik runtime | Poczekać, aż użytkownik wstrzyma OMSI. |
| `OL_E_UI_BUSY` | `CurrentRuntimeControl` | Akcja menu pauzy jest zaplanowana lub trwa (może tkwić w natywnym oknie modalnym) albo wywołanie nadeszło wewnątrz klatki OMSI. | Wynik runtime | Odpytywać `ui.pause.state`, aż `busy` będzie `false`, a następnie ponowić. |
| `OL_E_UI_ALREADY_TAKEN_OVER` | `CurrentRuntimeControl` | `ui.pause.take-over`, gdy natywne menu pauzy jest już przejęte. | Wynik runtime | Użyć `ui.pause.state` albo najpierw `ui.pause.restore-native`. |

## Warning

| Kod | Zgłaszany przez | Znaczenie | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | Plik wykonywalny OMSI 2 nie ma znanego układu (jego tożsamość kodu nie występuje w katalogu buildów). Plan pozostaje możliwy do uruchomienia: każda możliwość natywna jest rozwiązywana i weryfikowana w procesie, a ta, której nie można zweryfikować, jest niedostępna. | Komunikat diagnostyczny planu (nie wpływa na możliwość uruchomienia) | Nic nie trzeba robić; sprawdzić `/plan` i `profiles`, aby poznać przewidywane możliwości. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (nadzorca) | Zażądano `Behavior.CooperativeShutdown`, ale nie dało się poprosić OMSI o zamknięcie (jego procedura wyjścia jest w tym buildzie nierozwiązana albo OMSI już się zamyka), albo nie zakończył się w `ShutdownTimeoutSeconds`: OMSI został zamiast tego przerwany. Przywracanie przebiega jak zwykle. | komunikat diagnostyczny sesji | Nic: sesja kończy się tak jak przy wymuszonym zakończeniu. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Możliwość potrzebna do startu nie mogła zostać rozwiązana z pliku wykonywalnego (np. plik wykonywalny chroniony DRM, którego załadowany obraz różni się od pliku). Jest rozwiązywana w procesie przy starcie. | Komunikat diagnostyczny planu (nie wpływa na możliwość uruchomienia) | Nic nie trzeba robić; start kończy się błędem `OL_E_CAPABILITY_UNRESOLVED`, jeśli nie da się jej rozwiązać także w procesie. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Moduł, który dołączył do sesji sam (aktywacja przez plik ustawień, zawartość lub domyślna), nie może zostać wczytany (`Data["code"]` podaje powód, `Data["activation"]` sposób dołączenia). | Komunikat diagnostyczny planu (nie wpływa na możliwość uruchomienia) | Naprawić, wyłączyć lub usunąć moduł. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Wpis `.omsilaunch\modules.ini` nie może zostać użyty (to nie sekcja modułu, wpis poza sekcją, nieznany klucz, nieprawidłowa wartość; używana jest wartość domyślna) albo zażądano ustawień dla modułu, którego sesja nie wczytuje. | Komunikat diagnostyczny planu (nie wpływa na możliwość uruchomienia) | Poprawić wpis lub żądanie. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Ścieżka usuwana przez sesję (cel ITX, `Texture\standard.ipr`, `closecheck`) nie istniała przed sesją i istnieje teraz, ale w ramach tego dziennika nigdy nie uruchomiono żadnego procesu OMSI, więc plik nie może być produktem ubocznym sesji. Zostaje zachowany i zgłoszony (komunikat = ścieżka względna, `Data["sha256"]`); transakcja mimo to się kończy. | Komunikat diagnostyczny sesji / `RecoveryStatus.Diagnostics` (bez wpływu na stan) | Sprawdzić plik; w razie potrzeby usunąć go samodzielnie. |

## Module

Moduły runtime (zob. [moduły runtime](runtime-modules.md)). Przy planowaniu kod jest komunikatem diagnostycznym planu: dla zażądanego modułu czyni plan niemożliwym do uruchomienia; dla modułu, który dołączył sam (plik ustawień, zawartość, domyślny), plan zgłasza zamiast tego `OL_W_MODULE_NOT_LOADED`. W sesji kod jest komunikatem diagnostycznym sesji (sesja trwa dalej, chyba że kodem jest `OL_E_MODULE_SESSION_FAILED`), a dla operacji modułu jest wynikiem runtime.

| Kod | Zgłaszany przez | Znaczenie | Powierzchnia | Co zrobić |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | Brak `module.json`, nie jest to poprawny `omsilaunch.runtime-module/v1` albo jego `id` różni się od nazwy katalogu. Komunikat wskazuje pole. | Komunikat diagnostyczny planu; `ListRuntimeModulesAsync` | Poprawić manifest. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Zażądany identyfikator modułu nie jest zainstalowany poniżej `plugins\OmsiLaunch.Modules`. | Komunikat diagnostyczny planu | Sprawdzić `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Przypięty plik nie istnieje, różni się od przypiętego SHA-256 albo jest osiągany przez punkt reparse; albo w procesie `module.json` zmienił się po planowaniu. | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji | Zainstalować moduł ponownie. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | Zakres `bridge_protocol` modułu nie obejmuje żadnego z protokołów obsługiwanych przez ten build (1 i 2). | Komunikat diagnostyczny planu | Użyć wersji modułu przeznaczonej dla tego OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Wymagana możliwość jest nieznana temu buildowi, niedostępna w tej instalacji albo nierozwiązana w procesie. | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji | Zob. [możliwości](capabilities.md) i `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | Sesja nie uruchamia powiązanej mapy modułu albo powiązany plik różni się od przypiętego SHA-256. | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji | Uruchomić powiązaną mapę z powiązaną zawartością. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | Moduł poprosił o uruchomienie pliku, który nie jest jednym z jego komponentów pomocniczych. | Wynik runtime wewnątrz modułu | Wada modułu. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | Nie udało się wczytać komponentu zarządzanego ani utworzyć jego typu wejściowego; albo nie udało się uruchomić programu sesji modułu lub zakończył się, zanim był gotowy. | Komunikat diagnostyczny sesji | Sprawdzić build modułu (zgodny z x86, typ wejściowy implementuje `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` lub `Stop` zgłosiło wyjątek. | Komunikat diagnostyczny sesji | Wada modułu; zastosowano jego `failure_policy`. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Faza lub operacja zwróciła wynik po przekroczeniu budżetu (`timeouts`, `timeout_ms`) albo program sesji nie był gotowy w ciągu `prepare_ms`. | Komunikat diagnostyczny sesji; wynik runtime | Wada modułu; zastosowano jego `failure_policy`. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | Moduł został wyłączony lub wcześniej w sesji zakończył się błędem. | Wynik runtime | Pierwszy błąd należy sprawdzić w komunikatach diagnostycznych sesji. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Sesja nie wczytała tego modułu albo nie jest on jeszcze gotowy (przed rozgrywką). | Wynik runtime | Zażądać modułu przy starcie; poczekać na `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Moduł nie deklaruje takiej operacji. | Wynik runtime | Sprawdzić `operations` modułu. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | Operacja zakończyła się błędem bez skatalogowanego kodu; własny kod modułu jest w `Values["module_code"]`. | Wynik runtime | Zależnie od modułu. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Po zatrzymaniu lub błędzie śledzony zasób nadal istniał (proces pomocniczy, który się nie zakończył, wątek w tle, który się nie skończył) albo jego zwolnienie zgłosiło wyjątek. | Komunikat diagnostyczny sesji | Zgłosić moduł; proces i tak kończy się wraz z OMSI. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Moduł z `failure_policy` równym `fail-session` zakończył się błędem; sesja jest zatrzymywana i przywracana. | Komunikat diagnostyczny sesji (stan `Failed`) | Zob. poprzedzający kod modułu. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Zażądane ustawienie modułu jest nieprawidłowe lub niezadeklarowane przez moduł, moduł jest jednocześnie włączony i wyłączony, nazwa ustawienia nie ma postaci `<module-id>.<key>` albo flaga modułu jest zadeklarowana przez kilka modułów. | Komunikat diagnostyczny planu (niemożliwy do uruchomienia); błąd CLI | Poprawić żądanie; `/list:RuntimeModules` pokazuje ustawienia każdego modułu. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Wtyczka OMSI wymieniona przez moduł w `conflicts.plugins` jest włączona (`plugins\*.opl`) albo wczytana w OMSI. Powód wskazuje `.opl`. | Komunikat diagnostyczny planu; komunikat diagnostyczny sesji | Wyłączyć lub usunąć tę wtyczkę. OmsiLaunch nigdy jej nie zmienia. |
| `OL_E_MODULE_OPERATION_NOT_DECLARED` | `ModuleHost` | Moduł poprosił o wykonanie w procesie operacji katalogu (`IRuntimeModuleContext.Runtime`, natywne `run_operation`), której jego manifest nie wymienia w `runtime_operations`. Wywołanie nigdy nie dociera do OMSI. | Wynik runtime | Wymienić operację w `runtime_operations` (wymaga `bridge.runtime` i `bridge_protocol.min` równego 3). |
| `OL_E_MODULE_CONTENT_INPUT_UNDECLARED` | `ModuleHost` | Moduł poprosił o zlokalizowanie zawartości społeczności (`IRuntimeModuleContext.Content`) rodzaju, którego jego manifest nie deklaruje w `content_inputs`. | Wynik kontekstu | Zadeklarować ten rodzaj w `content_inputs`. |
| `OL_E_MODULE_CONTENT_INPUT_INVALID` | `RealmapPackage` | Pakietu `openomsi-realmap` mapy nie można bezpiecznie odczytać: jest osiągany przez punkt reparse, zawiera taki punkt, ma więcej niż 50,000 plików lub jest większy niż 4 GiB albo nie da się go odczytać. | Wynik kontekstu | Naprawić lub ponownie wygenerować pakiet; moduł degraduje zasób po zasobie. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | zarezerwowany | Zarezerwowany; komponent natywny otrzymuje zamiast tego liczbowe wyniki hooków opisane w [modułach runtime](runtime-modules.md#native-components-and-hooks). | brak | brak |

<a id="non-error-diagnostic-codes"></a>
## Kody diagnostyczne niebędące błędami

| Kod | Emitowany przez | Komunikat / dane | Znaczenie |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | komunikat = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | Utworzono `Omsi.exe` i zapisano jego tożsamość. Komunikat diagnostyczny sesji. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | komunikat = SHA-256 usuniętego pliku | Plik `closecheck` istniejący przed sesją został trwale usunięty (`SuppressStaleClosecheckWarning = true`). Komunikat diagnostyczny sesji. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | komunikat = ścieżka względna; `Data["sha256"]` | Ścieżka usuwana przez sesję została ponownie utworzona przez OMSI w trakcie sesji, której proces się uruchomił; usunięto ją, aby przywrócić pierwotny stan nieistnienia. Komunikat diagnostyczny sesji / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | komunikat = `manifest` lub `self` | Która referencja jest używana przy walidacji stałej wtyczki. Komunikat diagnostyczny planu. |
| `module.planned` | `SessionPlanner` | komunikat = identyfikator modułu; `Data["module"]`, `Data["status"]`, `Data["activation"]` | Moduł jest w planie. Komunikat diagnostyczny planu. |
| `module.disabled` | `SessionPlanner` | komunikat = identyfikator modułu | Moduł wynikający z zawartości lub domyślny został wyłączony (żądaniem lub plikiem ustawień). Komunikat diagnostyczny planu. |
| `session_profile.selected` | `SessionPlanner` | komunikat = identyfikator profilu; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Pochodzenie sesji skompilowanej z profilu sesji. Komunikat diagnostyczny planu. |

Nazwy zdarzeń runtime (`RuntimeEvent.Type`, nie są to komunikaty diagnostyczne) wymieniono w opisie [cyklu życia sesji](../concepts/session-lifecycle.md#telemetry-events).
