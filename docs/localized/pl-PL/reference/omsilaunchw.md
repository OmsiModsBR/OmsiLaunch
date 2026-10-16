# Dokumentacja OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/omsilaunchw.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

`OmsiLaunchW.exe` to host kontrolera OmsiLaunch działający w podsystemie Windows (GUI). Przyjmuje ten sam wiersz poleceń co `OmsiLaunch.exe` i wykonuje ten sam kod kontrolera (`OmsiLaunch.Controller.dll`). Jedyna różnica dotyczy sposobu raportowania: nie ma okna konsoli, błędy są pokazywane w oknach komunikatu, a działająca sesja jest widoczna wyłącznie dzięki swojej [ikonie w obszarze powiadomień](windows-tray.md).

Źródła wiarygodne: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (natywny shim), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) i `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Porównanie OmsiLaunch.exe i OmsiLaunchW.exe

| Aspekt | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Podsystem | Konsola. Otwiera okno konsoli, gdy jest uruchamiany z Eksploratora (Explorer). | Windows (GUI). Brak okna konsoli. |
| Natywny shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Środowisko | bez zmian | ustawia `OMSILAUNCH_WINDOWS_HOST=1` dla procesu kontrolera przed startem .NET |
| Argumenty | dzielone na tokeny przez `CommandLineToArgvW` i przekazywane do kontrolera bez zmian | tak samo, więc oba hosty przyjmują dokładnie te same flagi i polecenia ([dokumentacja CLI](cli.md)) |
| Wyjście tekstowe | zapisywane na stdout | pomijane, chyba że podano `--json` (wtedy koperty JSON są zapisywane na stdout, które kod wywołujący może przekierować) |
| Błędy | `OL_E_...: message` lub koperta błędu JSON | te same reguły wyjścia konsolowego, **a ponadto** okno komunikatu dla każdego błędu (zobacz [Okna dialogowe błędów](#failure-dialogs)) |
| `/silent` | uruchamia `OmsiLaunchW.exe` z pozostałymi argumentami i zwraca `0` | ignorowane: polecenie już działa w hoście Windows |
| Ikona w obszarze powiadomień | pokazywana dla sesji właściciela | pokazywana dla sesji właściciela |
| Ścieżki zatrzymania | obszar powiadomień, `session stop`, zakończenie OMSI, `/observe-seconds`, Ctrl+C, zamknięcie konsoli | obszar powiadomień, `session stop`, zakończenie OMSI, `/observe-seconds` (nie ma konsoli, więc Ctrl+C i zamknięcie konsoli nie mają zastosowania) |
| Kody wyjścia | [`PublicExitCode`](exit-codes.md) `0`..`10`, kody shim `100`..`106` | te same kody |

<a id="how-it-starts"></a>
## Jak się uruchamia

1. Shim ustala własną ścieżkę (`GetModuleFileNameW`) i oczekuje `OmsiLaunch.Controller.dll` w tym samym katalogu.
2. Dzieli wiersz poleceń na tokeny (`CommandLineToArgvW`) i ustawia `OMSILAUNCH_WINDOWS_HOST=1`.
3. Lokalizuje `hostfxr` za pomocą dołączonego do pakietu `nethost.dll`, ładuje go, inicjalizuje kontroler z argumentami (ścieżka kontrolera nie jest częścią listy argumentów widzianej przez parser CLI) i uruchamia go.
4. Shim zwraca kod wyjścia kontrolera bez zmian.

Jeśli którykolwiek krok przed uruchomieniem kontrolera nie powiedzie się, shim pokazuje okno komunikatu z tytułem `OmsiLaunch` i tekstem `OmsiLaunch could not start the .NET host (code N).` (nie można uruchomić hosta .NET, kod N), a następnie kończy działanie z tym kodem:

| Kod | Nieudany krok |
| --- | --- |
| `100` | nie można ustalić ścieżki pliku wykonywalnego |
| `101` | nie można podzielić wiersza poleceń na tokeny |
| `102` | sondowanie lokalizacji `hostfxr` nie powiodło się (zwykle: nie zainstalowano runtime .NET 6 x64) |
| `103` | nie można pobrać ścieżki `hostfxr` |
| `104` | nie można załadować `hostfxr` |
| `105` | brak wymaganych eksportów `hostfxr` |
| `106` | nie można zainicjalizować hosta zarządzanego (na przykład brakuje `OmsiLaunch.Controller.dll` lub jego konfiguracji runtime albo nie ma runtime Windows Desktop) |

`OmsiLaunch.exe` używa tej samej tabeli, ale niczego nie wypisuje. Te okna dialogowe nie zostały wywołane w czasie działania (zobacz [stan weryfikacji w runtime](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Uruchamianie

Uruchomienie bezpośrednie – ze skrótu, skryptu lub innego programu:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Za pośrednictwem `OmsiLaunch.exe` z `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Pliki wykonywalne należy umieścić w instalacji OMSI 2 (układ pakietu, zobacz [pakowanie](packaging.md)); bez argumentu instalacji instalacją jest katalog zawierający plik wykonywalny. Jawnie wskazaną instalację przekazuje się jako pierwszy argument, tak jak w przypadku `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Ponieważ jest to program GUI, `cmd.exe` i Eksplorator nie czekają na jego zakończenie. Aby w skrypcie poczekać na zakończenie i odczytać kod wyjścia, należy użyć `start /wait OmsiLaunchW.exe ...` w `cmd.exe` lub `Start-Process -Wait -PassThru` w PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delegowanie /silent

`OmsiLaunch.exe ... /silent` (lub `--silent`), jeśli nie działa już pod `OmsiLaunchW.exe`, wykonuje następujące kroki (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Szuka `OmsiLaunchW.exe` w katalogu `OmsiLaunch.exe`. Jeśli go brakuje: `OL_E_WINDOWS_HOST_MISSING`, wyjście `7`.
2. Uruchamia `OmsiLaunchW.exe` przez `ShellExecute` (`UseShellExecute = true`) z bieżącym katalogiem i wszystkimi argumentami oprócz `/silent`/`--silent`, w oryginalnej kolejności. `ShellExecute` nie przekazuje uchwytów wywołującego do nowego procesu, więc kod wywołujący, który przechwytuje wyjście `OmsiLaunch.exe /silent`, nie jest blokowany przez cały czas trwania sesji (zamknięcie runtime BUG-03). Jeśli nie zostanie zwrócony żaden proces: `OL_E_WINDOWS_HOST_START_FAILED`, wyjście `7`.
3. Zapisuje kopertę `silent` i natychmiast kończy działanie z kodem `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

Wyjście `0` oznacza jedynie, że `OmsiLaunchW.exe` został uruchomiony. Wynik sesji (błędy planowania, już aktywny właściciel, nieudany start) zgłasza `OmsiLaunchW.exe` we własnych oknach dialogowych i komunikatach diagnostycznych, a oba procesy są od tej chwili niezależne: `OmsiLaunch.exe` zakończył działanie, a `OmsiLaunchW.exe` jest właścicielem sesji. Do podglądu sesji służy `OmsiLaunch.exe session status`.

`/silent` jest stosowane przed każdym innym poleceniem, więc `OmsiLaunch.exe /silent session status` również wykonuje `session status` wewnątrz `OmsiLaunchW.exe`, gdzie jego wyjście jest pomijane. `/silent` należy używać wyłącznie do uruchamiania sesji.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Działanie poszczególnych poleceń pod OmsiLaunchW.exe

| Wiersz poleceń | Wynik |
| --- | --- |
| brak argumentów | wykonuje po cichu `detect` i kończy działanie z kodem `0` (nic nie jest pokazywane) |
| uruchomienie (`/new`, `/saved:...`, `/spec:...`, profil sesji) z planem możliwym do uruchomienia i bez właściciela | staje się właścicielem sesji: ikona w obszarze powiadomień podczas uruchamiania i działania; kończy działanie po zakończeniu sesji (`0` – ukończona, `1` – nieudana) |
| uruchomienie, którego plan jest niemożliwy do uruchomienia | okno dialogowe z ostatnim komunikatem diagnostycznym `OL_E_` planu (wartość zastępcza `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), wyjście `1` (audyt dokumentacji BUG-06; przed poprawką proces kończył działanie po cichu) |
| uruchomienie, gdy dla instalacji jest już aktywny właściciel | okno dialogowe `OL_E_SESSION_ALREADY_ACTIVE`, wyjście `7`; działająca sesja nie jest naruszana |
| uruchomienie, które nie osiąga stanu `Running` | okno dialogowe `The OMSI session did not reach gameplay.` (sesja OMSI nie osiągnęła rozgrywki) z ostatnim komunikatem diagnostycznym `OL_E_`, wyjście `1`. Podczas gdy okno dialogowe jest otwarte, nadzorca sesji kończy OMSI i przywraca pliki; proces kończy działanie po zamknięciu okna dialogowego i zakończeniu `CloseAsync` |
| nieprawidłowy argument, nieznana flaga lub nieprawidłowy profil sesji | okno dialogowe z `OL_E_INVALID_ARGUMENT` lub kodem `OL_E_SESSION_PROFILE_*`, wyjście `2` |
| polecenie klienta (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) bez właściciela | okno dialogowe `OL_E_NO_ACTIVE_SESSION`, wyjście `4` |
| polecenie klienta odrzucone przez właściciela | okno dialogowe z kodem odrzucenia (na przykład `OL_E_CONTROL_SESSION_MISMATCH` lub kod błędu runtime), wyjście `7` (`2` dla nieznanej operacji lub brakującego argumentu) |
| udane polecenie klienta lub wykrywania (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | brak widocznego wyjścia, chyba że podano `--json` i stdout jest przekierowane; kod wyjścia jak dla `OmsiLaunch.exe` |
| `/plan` lub `/validate` | brak okna dialogowego, nawet dla planu niemożliwego do uruchomienia; wyjście `0` lub `1` |
| każdy inny błąd kontrolera | okno dialogowe ze sklasyfikowanym kodem `OL_E_`; kod wyjścia zgodnie z [kodami wyjścia](exit-codes.md) |

<a id="failure-dialogs"></a>
## Okna dialogowe błędów

Każdy błąd, który `OmsiLaunch.exe` by wypisał, jest również pokazywany jako modalne okno komunikatu (`WindowsHost.ShowFailure`), nawet gdy podano `--json`:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` to komunikat błędu, a w przypadku błędu sesji – komunikat ostatniego komunikatu diagnostycznego `OL_E_`. Znane ograniczenie: dla błędu zgłoszonego przez wtyczkę komunikatem jest surowy ładunek błędu wtyczki (na przykład `{"name":"world.failed",...}`); wiersz `Code:` jest poprawny (zobacz [znane ograniczenia](known-limitations.md)). Okno dialogowe jest modalne, a proces kończy działanie po jego zamknięciu. Dowody z runtime: błąd argumentu, brak aktywnej sesji oraz błąd przed rozpoczęciem rozgrywki (zamknięcie runtime `T04`).

<a id="session-tray-and-exit"></a>
## Sesja, obszar powiadomień i zakończenie

Sesja, której właścicielem jest `OmsiLaunchW.exe`, zachowuje się dokładnie tak samo jak sesja należąca do `OmsiLaunch.exe` (zobacz [cykl życia sesji](../concepts/session-lifecycle.md)):

- Ikona w obszarze powiadomień pojawia się, gdy tylko sesja zostanie uruchomiona, zanim OMSI osiągnie rozgrywkę, chyba że w pliku `/spec` ustawiono `Presentation.SuppressTrayIcon`. Przy `SuppressTrayIcon` nie ma żadnego widocznego elementu; sesję zatrzymuje się poleceniem `OmsiLaunch.exe session stop` lub przez zamknięcie OMSI.
- Sesja kończy się, gdy OMSI zakończy działanie, gdy w obszarze powiadomień zostanie potwierdzone `End session` (`Zakończ sesję`), gdy klient wyśle `session stop` lub gdy upłynie czas `/observe-seconds`. OmsiLaunch kończy wtedy OMSI, jeśli nadal działa, przywraca każdy zmieniony przez siebie plik, zwalnia dzierżawę instalacji, usuwa ikonę z obszaru powiadomień i kończy działanie.
- Jeśli sam proces `OmsiLaunchW.exe` zostanie zabity, następne uruchomienie OmsiLaunch dla tej instalacji odzyskuje oczekującą transakcję (zobacz [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Szybki start

1. Zainstaluj pakiet w katalogu OMSI 2 ([instalacja](../getting-started/installation.md)).
2. Utwórz skrót do `OmsiLaunchW.exe` z argumentami sesji, na przykład `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Uruchom go. OMSI startuje bez okna konsoli; ikona OmsiLaunch pojawia się w obszarze powiadomień.
4. Kliknij ikonę prawym przyciskiem myszy → `Status` (`Stan`), aby zobaczyć sesję, lub `End session` → `End session` (`Zakończ sesję` → `Zakończ sesję`), aby ją zakończyć.
5. Jeśli coś pójdzie nie tak, okno dialogowe pokazuje kod błędu; szczegóły znajdują się w `<OMSI_PATH>\.omsilaunch\diagnostics`.
