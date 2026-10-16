# Kody wyjścia

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/exit-codes.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona wymienia wszystkie kody wyjścia procesu, które mogą zwrócić `OmsiLaunch.exe` i `OmsiLaunchW.exe`: publiczny kontrakt zarządzany `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), kody natywnego shim `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` i `OmsiLaunch.WindowsHost.cpp`) oraz reguły klasyfikacji, które `CliProgram.Classify` stosuje do każdego nieprzechwyconego wyjątku (`tools\OmsiLaunch.Cli\Program.cs`). Kod wywołujący musi wywnioskować znaczenie z kodu i ze strukturalnej koperty błędu (envelope), nigdy z treści komunikatu. Kody błędów są skatalogowane na stronie [błędy](errors.md); polecenia, które zwracają poszczególne kody, opisuje [dokumentacja CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Publiczne kody wyjścia (`PublicExitCode`)

| Kod | Nazwa w wyliczeniu | Znaczenie | Kiedy |
|---|---|---|---|
| 0 | `Success` | Polecenie zostało wykonane. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` z planem możliwym do uruchomienia; sesja zakończona w stanie `Completed`; `/silent` po uruchomieniu `OmsiLaunchW.exe`; przekazane polecenie klienta, na które właściciel odpowiedział `Ok=true`; `/recover`, gdy nic nie oczekiwało lub przywracanie zostało ukończone. |
| 1 | `SessionFailed` | Plan był niemożliwy do uruchomienia albo sesja należąca do właściciela zakończyła się w stanie `Failed`. | `/plan` zgłaszający `NOT RUNNABLE`; uruchomienie, którego plan jest niemożliwy do uruchomienia (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` dodatkowo pokazuje ostatni komunikat diagnostyczny `OL_E_` w oknie komunikatu); `OL_E_PLAN_NOT_RUNNABLE` zgłoszony przez `StartSessionAsync` (ponowne planowanie przy starcie); sesja nie osiągnęła stanu `Running` w limicie czasu uruchamiania; sesja zakończyła się w stanie `Failed`. |
| 2 | `InvalidArguments` | Wiersz poleceń, specyfikacja, profil lub argumenty runtime zostały odrzucone przed dyspozycją lub w jej trakcie. | Nieznana flaga lub ścieżka polecenia, brakująca wartość, wartość spoza zakresu; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` i `OL_E_RUNTIME_ARGUMENT_REQUIRED` (lokalnie lub zwrócone przez właściciela); wypisanie informacji o użyciu dla polecenia, którego nie można przekazać do wykonania; dowolny `ArgumentException`, `FormatException`, `InvalidDataException` lub `OverflowException`. |
| 3 | `UnsupportedProfile` | Platforma lub build OMSI nie jest obsługiwany. | Nieprzechwycony wyjątek, którego kod zaczyna się od `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Te same warunki wykryte podczas planowania sprawiają, że plan jest niemożliwy do uruchomienia, i wtedy zwracany jest kod `1`. |
| 4 | `NoActiveSession` | Polecenie klienta nie znalazło właściciela. | `session status`, `session stop`, `events read`, `events watch` lub przekazana operacja runtime, gdy lokalny punkt końcowy sterowania tej instalacji nie odpowiada (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Przekroczenie limitu czasu (timeout) wydostało się jako wyjątek. | Dowolny `TimeoutException` (`OL_E_TIMEOUT`, gdy komunikat nie zawiera kodu, w przeciwnym razie osadzony kod, np. `OL_E_RUNTIME_REQUEST_TIMEOUT`). Na przekroczenia limitu czasu przekazanych poleceń klienta właściciel odpowiada `Ok=false` i zwracany jest kod `7`, a nie `5`. |
| 6 | `NotFound` | Nie znaleziono pliku lub katalogu. | `FileNotFoundException` / `DirectoryNotFoundException` (domyślnie `OL_E_NOT_FOUND`), na przykład `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` zgłoszony jako wyjątek, brakujący katalog instalacji podczas `/list`. |
| 7 | `OperationRejected` | Polecenie było poprawne, ale zostało odrzucone, albo przekazane polecenie nie powiodło się u właściciela. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; każda odpowiedź sterowania `Ok=false` inna niż dwa kody argumentów (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); każdy inny nieprzechwycony wyjątek z kodem `OL_E_`, który nie jest sklasyfikowany gdzie indziej (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Nie udało się przywrócić trwałej transakcji. | `/recover`, gdy dziennik oczekiwał i nadal oczekuje; każdy nieprzechwycony wyjątek, którego kod zaczyna się od `OL_E_RECOVERY_` lub jest równy `OL_E_RESTORE_FAILED` (na przykład `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` podczas przywracania własnych plików sesji). |
| 10 | `InternalError` | Nieoczekiwany wyjątek bez kodu `OL_E_`. | Zgłaszany jako `OL_E_INTERNAL` z kategorią `internal`; komunikatem jest tekst wyjątku. |

Kod `9` nie jest przypisany.

<a id="native-shim-exit-codes"></a>
## Kody wyjścia natywnego shim

Zwracane przez `OmsiLaunch.exe` / `OmsiLaunchW.exe`, zanim zostanie uruchomiony kontroler zarządzany. Są rozłączne z `PublicExitCode`, dzięki czemu kod wywołujący może odróżnić błąd uruchamiania hosta od wyniku kontrolera. `OmsiLaunchW.exe` dodatkowo pokazuje `OmsiLaunch could not start the .NET host (code N).` w oknie komunikatu.

| Kod | Znaczenie | Przyczyna |
|---|---|---|
| 100 | Nie można ustalić ścieżki pliku wykonywalnego | `GetModuleFileNameW` nie powiodło się. |
| 101 | Nie można podzielić wiersza poleceń na tokeny | `CommandLineToArgvW` zwróciło null. |
| 102 | Sondowanie lokalizacji `hostfxr` nie powiodło się | Zapytanie o rozmiar `get_hostfxr_path` nie powiodło się: nie zainstalowano pasującego runtime .NET (wymagany jest runtime .NET 6 x64). |
| 103 | Nie można pobrać ścieżki `hostfxr` | Drugie wywołanie `get_hostfxr_path` nie powiodło się. |
| 104 | Nie można załadować biblioteki `hostfxr` | `LoadLibraryW` dla ustalonego `hostfxr.dll` nie powiodło się. |
| 105 | Brak wymaganych eksportów `hostfxr` | Nie znaleziono `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` lub `hostfxr_close`. |
| 106 | Nie można zainicjalizować hosta zarządzanego | `hostfxr_initialize_for_dotnet_command_line` nie powiodło się dla `OmsiLaunch.Controller.dll` (brak `OmsiLaunch.Controller.runtimeconfig.json`, brak `Microsoft.WindowsDesktop.App` 6.0 x64 lub uszkodzony pakiet). |

<a id="classification-rules-cliprogramclassify"></a>
## Reguły klasyfikacji (`CliProgram.Classify`)

Każdy wyjątek, który wydostaje się z `CliProgram.RunAsync`, jest zamieniany na kopertę błędu (`CliInput.WriteError`) i kod wyjścia przez `CliProgram.ReportFailure`, które wywołuje `Classify`. Błędy parsowania są obsługiwane w ten sam sposób przed dyspozycją (wyjście `2`). Reguły są stosowane w następującej kolejności:

1. Z komunikatu wyjątku wyodrębniany jest pierwszy token `OL_E_` (`ExtractCode`): kodem jest najdłuższy ciąg liter ASCII, cyfr i `_` zaczynający się od `OL_E_`. Kody są przekazywane dosłownie w `error.code`.
2. `SessionProfileException` → jego własny `Code`, kategoria `invalid_argument`, wyjście `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → wyodrębniony kod lub `OL_E_INVALID_ARGUMENT`, kategoria `invalid_argument`, wyjście `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → wyodrębniony kod lub `OL_E_NOT_FOUND`, kategoria `not_found`, wyjście `6`.
5. `TimeoutException` → wyodrębniony kod lub `OL_E_TIMEOUT`, kategoria `runtime`, wyjście `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, kategoria `session`, wyjście `7`.
7. W pozostałych przypadkach, gdy kod został wyodrębniony:
   - zaczyna się od `OL_E_RECOVERY_` lub jest równy `OL_E_RESTORE_FAILED` → kategoria `transaction`, wyjście `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → kategoria `session`, wyjście `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → kategoria `session`, wyjście `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → kategoria `invalid_argument`, wyjście `2`;
   - zaczyna się od `OL_E_UNSUPPORTED_` → kategoria `unsupported_profile`, wyjście `3`;
   - każdy inny kod → kategoria `runtime` dla `InvalidOperationException` i `IOException`, w przeciwnym razie `internal`; wyjście `7`.
8. Brak jakiegokolwiek kodu → `OL_E_INTERNAL`, kategoria `internal`, wyjście `10`.

Przekazane odpowiedzi klienta omijają `Classify`: `CliProgram.ReportForwarded` zwraca `4` przy braku punktu końcowego, `2` dla `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` dla każdej innej odpowiedzi `Ok=false` oraz `0` dla `Ok=true`.

<a id="scripting-guidance"></a>
## Wskazówki dla skryptów

- Należy traktować `0` jako sukces, a każdą inną wartość jako błąd; rozgałęziać najpierw według kodu liczbowego, a następnie według `error.code` z koperty `--json`.
- Uruchomienie sesji kończy się dopiero po zakończeniu sesji i przywróceniu jej plików; `1` oznacza, że transakcja została wykonana, ale OMSI zakończył się błędem lub plan został odrzucony – nie oznacza, że pliki pozostały zmodyfikowane (pozostały dziennik zgłasza `/recovery-status`).
- `100`..`106` oznaczają uszkodzony pakiet lub runtime .NET; zobacz [instalacja](../getting-started/installation.md) i [pakowanie](packaging.md).
