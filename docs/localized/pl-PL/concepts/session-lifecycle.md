# Cykl życia sesji

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../concepts/session-lifecycle.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona opisuje, jak sesja OmsiLaunch przechodzi przez stany `SessionState` od `Created` do `Completed` lub `Failed`: który komponent ustawia każdy stan, które zdarzenia telemetrii wtyczki wyzwalają przejścia, jak działa limit czasu uruchamiania, co oznacza zatrzymanie (wymuszone zakończenie procesu), co zwraca `WaitForAsync`, które stany są końcowe, które stany nigdy nie są lub prawie nie są obserwowalne oraz jakie gwarancje daje właściciel w CLI. Wszystkie informacje pochodzą z `OmsiLaunchService.StartAsync`, `SuperviseAsync` i `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) oraz `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Powiązane strony: [publiczne API](../reference/public-api.md), [kody błędów](../reference/errors.md), [transakcje i odzyskiwanie](transactions-and-recovery.md), [stała wtyczka](permanent-plugin.md), [sterowanie runtime](../reference/runtime-control.md), [lokalna płaszczyzna sterowania](../reference/local-control.md), [obszar powiadomień Windows](../reference/windows-tray.md), [dokumentacja CLI](../reference/cli.md), [stan walidacji runtime](../status/runtime-validation-status.md), [katalog `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Przegląd

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## Opis `SessionState`

Wartości w kolejności deklaracji. Kolumna „Ustawiany przez” wskazuje kod, który wywołuje `Move`/`Fail`; kolumna „Obserwowalny” mówi, czy `GetStatusAsync`/`WaitForAsync` może go w praktyce zobaczyć.

| # | Stan | Ustawiany przez | Obserwowalny | Znaczenie |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (wartość początkowa aktywnej sesji) | Krótko | Sesja jest zarejestrowana; nic jeszcze się nie wydarzyło. |
| 1 | `ValidatingPlatform` | nikt | Nie | Zadeklarowany, nigdy nie jest ustawiany przez obecną usługę (walidacja platformy odbywa się w `PlanSessionAsync`, który nie ma stanu sesji). |
| 2 | `Planning` | nikt | Nie | Zadeklarowany, nigdy nie jest ustawiany (planowanie odbywa się, zanim sesja istnieje; ponowne planowanie w `StartSessionAsync` również poprzedza rejestrację). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Tak | Trwa uzyskiwanie dzierżawy instalacji. Błąd: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Tak | Oczekujący `journal.json` jest przywracany, zanim aktywna instalacja zostanie odczytana; zestaw plików stałej wtyczki jest walidowany (`plugin.integrity.reference`), obliczany jest hash `Omsi.exe`, zasoby ekranu startowego są umieszczane na miejscu, a nieaktualny `closecheck` jest usuwany. Błędy: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (proces zapisany w dzienniku nadal działa). |
| 5 | `Snapshotting` | `StartAsync` | Praktycznie nie | Ustawiany bezpośrednio przed `ApplyingConfiguration` bez żadnego await pomiędzy; sama migawka (snapshot) jest wykonywana wewnątrz `ApplyAsync`. Przejściowy i nieobserwowalny. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Tak | Zapisywany jest `journal.json` (`Prepared`), tworzone są kopie zapasowe oryginałów, zapisywane nakładki (overlay), usuwane pliki przeznaczone do usunięcia na czas sesji (`Applied`). Błędy: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, błędy we/wy. |
| 7 | `DeployingRuntime` | `StartAsync` | Tak | Stan dziennika `RuntimeDeployed`. Żaden plik nie jest wdrażany: zestaw plików wtyczki jest stały. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Tak | Istnieją handoff (`OmsiLaunch.Handoff.<id>`), slot telemetrii (`OmsiLaunch.Telemetry.<id>`) i skrzynka runtime (`OmsiLaunch.Runtime.<id>`); stan dziennika `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Tak | `CreateProcessW` dla `<root>\Omsi.exe` z katalogiem roboczym `<root>`. Błędy: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Tak | Proces istnieje, zdarzenie `process.started` jest zarejestrowane, dziennik jest w stanie `ProcessStarted`, nadzorca jest uruchomiony, a `StartSessionAsync` zwraca wynik. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` po `plugin.started` | Tak | Stała wtyczka odczytała prawidłowy handoff dla tej sesji. Od tego momentu przekroczenie limitu czasu daje `OL_E_STARTUP_TIMEOUT` zamiast `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` po `world.starting` | Tak (tylko NEW_MAP) | Wtyczka wywołała natywny start NEW_MAP w wątku interfejsu użytkownika OMSI. Zapisane sytuacje emitują `world.situation.starting`, które nie jest mapowane, więc sesja SAVED_SITUATION przechodzi z `PluginBootstrap` bezpośrednio do `Running`. |
| 13 | `EnteringGameplay` | nikt | Nie | Zadeklarowany, nigdy nie jest ustawiany: `gameplay.entered` przenosi sesję od razu do `Running`. |
| 14 | `Running` | `ApplyTelemetry` po `gameplay.entered` | Tak | Rozgrywka została osiągnięta. `ExecuteRuntimeAsync` jest dozwolone; właściciel w CLI otwiera lokalną płaszczyznę sterowania; obszar powiadomień pokazuje działającą sesję. |
| 15 | `ProcessExited` | `SuperviseAsync` | Tak (tylko udane sesje) | OMSI zakończył działanie (samodzielnie lub przez wymuszone zakończenie), dziennik jest w stanie `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (oraz ścieżka błędu uruchamiania) | Tak (tylko udane sesje) | Każdy plik należący do sesji jest przywracany ze zweryfikowanej kopii zapasowej; artefakty sesji są usuwane. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Tak (tylko udane sesje) | Przywrócenie zweryfikowane, dziennik i kopie zapasowe usunięte; magazyny runtime zaraz zostaną zwolnione. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Tak, końcowy | Magazyny zwolnione, skrzynka zamknięta, dzierżawa zwolniona, żaden błąd nie został zarejestrowany. |
| 19 | `Failed` | `LiveSession.Fail` z `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Tak, końcowy | Zarejestrowano komunikat diagnostyczny błędu. Stan jest trwały: późniejsze wywołania `Move` są ignorowane, więc sesja zakończona błędem nigdy nie pokazuje `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, mimo że zakończenie procesu i przywracanie nadal są wykonywane. |

Stany końcowe: `Completed` i `Failed`. Po każdym z nich `WaitForAsync` zwraca wynik natychmiast, a `CloseAsync` kończy się bez żądania zatrzymania.

Weryfikacja stwierdzenia „nigdy nie jest ustawiany”: przeszukanie kodu pod kątem `SessionState.ValidatingPlatform`, `SessionState.Planning` i `SessionState.EnteringGameplay` znajduje wyłącznie deklarację enum; `SessionState.Snapshotting` występuje raz, bezpośrednio przed `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Faza uruchamiania (`StartSessionAsync`)

1. Odrzucenie planu niemożliwego do uruchomienia (`OL_E_PLAN_NOT_RUNNABLE`), ponowne zaplanowanie specyfikacji (ponowne obliczenie hasha `Omsi.exe`, ponowne rozwiązanie zawartości, ponowne sprawdzenie zestawu plików wtyczki) i ponowne odrzucenie, jeśli plan nie jest już możliwy do uruchomienia. Rejestracja aktywnej sesji (`Created`).
2. Utworzenie śladu hosta `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (starsze pliki z prefiksem sesji poza 50 najnowszymi sesjami są usuwane). Odrzucenie `StartupTimeoutSeconds` spoza zakresu 1..600 (`ArgumentOutOfRangeException`; rejestracja sesji jest cofana).
3. `AcquiringInstallationLock` → dzierżawa. `RecoveringPreviousTransaction` → odzyskanie oczekującego dziennika (dziennik sprzed wprowadzenia odcisków, który nie może udowodnić własności, jest odraczany, a odzyskiwanie jest ponawiane, gdy nakładki tej sesji już istnieją), walidacja zestawu plików stałej wtyczki, obliczenie hasha pliku wykonywalnego, usunięcie nieaktualnego `closecheck`, zbudowanie transakcji (nakładki: poprawki `options.cfg`, zarządzane pliki BMP ekranu startowego, `Texture\standard.itx`; usunięcia: cele ITX, `Texture\standard.ipr`, `closecheck`, gdy nie istnieje).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`, po czym uruchamiane jest zadanie nadzorcy i zwracany jest uchwyt.
5. Każdy wyjątek w krokach 3–4 jest przechwytywany: sesja przechodzi w stan `Failed` z `OL_E_START_SESSION` (komunikat wewnętrzny), utworzony proces jest kończony i następuje oczekiwanie na jego zakończenie, magazyny są zwalniane, transakcja jest przywracana (`OL_E_RESTORE_FAILED` w razie błędu) lub, jeśli nie można było potwierdzić zakończenia OMSI, pozostawiana jako oczekująca z `OL_E_RESTORE_DEFERRED`; dzierżawa jest zwalniana. `StartSessionAsync` nadal zwraca w tym przypadku uchwyt; należy odczytać `GetStatusAsync`.

Ponowne planowanie zachowuje `SessionId` wywołującego, więc identyfikator w uchwycie jest równy `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Nadzór (`SuperviseAsync`)

Nadzorca działa jako zadanie w puli wątków i wykonuje pętlę co 100 ms, dopóki OMSI nie zakończy działania lub nie zostanie zażądane zatrzymanie:

1. Odczytanie po kolei każdego zdarzenia telemetrii zatwierdzonego przez wtyczkę od ostatniego odczytu (pierścień zdarzeń; czytelnik, który został w tyle, otrzymuje `telemetry.lost` z liczbą utraconych zdarzeń). Każde zdarzenie jest dopisywane do `RuntimeEvents` i mapowane przez `ApplyTelemetry`.
2. Jeśli sesja jest w stanie `Failed`, wyjście z pętli.
3. Jeśli sesja nie jest jeszcze w stanie `Running`, a termin (`StartupTimeoutSeconds` od wejścia nadzorcy) minął: `Fail` z `OL_E_STARTUP_TIMEOUT`, gdy osiągnięto `PluginBootstrap`, w przeciwnym razie `OL_E_PLUGIN_NOT_LOADED`; wyjście z pętli.

Po pętli: jeśli OMSI zakończył działanie przed `Running` i nie zarejestrowano żadnego błędu, następuje `Fail` z `OL_E_PROCESS_EXITED_EARLY`. Następnie, niezależnie od tego, czy sesja zakończyła się błędem: zakończenie OMSI, jeśli nadal działa, oczekiwanie na zakończenie, oznaczenie dziennika jako `ProcessExited`, przejście do `ProcessExited`, przywrócenie (`Restoring` → `CleaningRuntime`) lub zarejestrowanie `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, zwolnienie uchwytu procesu, handoffu, slotu telemetrii i skrzynki runtime, zwolnienie dzierżawy oraz przejście do `Completed`, chyba że stan to `Failed`. Błąd wewnątrz samego nadzorcy jest rejestrowany jako `OL_E_PROCESS_SUPERVISION` (problemy ze sprzątaniem jako `OL_E_PROCESS_CLEANUP_FAILED`) i wykonywana jest ta sama ścieżka zakończenia procesu i przywracania.

Ponieważ `Failed` jest stanem trwałym, jedynym dowodem, że sesja zakończona błędem została przywrócona, jest brak `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` w jej komunikatach diagnostycznych (oraz brak `journal.json`); uwagi dotyczące przywracania (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) pojawiają się w obu przypadkach.

<a id="telemetry-events"></a>
## Zdarzenia telemetrii

Wtyczka publikuje w slocie telemetrii próbki JSON `{ "name": ..., "data": {...} }`; host rejestruje każdą nową próbkę jako `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emitowane przez | Akcja hosta |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` po odczytaniu prawidłowego handoffu | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: brak `OMSILAUNCH_HANDOFF_NAME`, handoff nieczytelny lub niemożliwy do zweryfikowania | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: handoff żąda trybu świata innego niż NEW_MAP/SAVED_SITUATION, startu innego niż headless, pojazdu gracza, trybów daty/czasu lub pustej tożsamości sytuacji | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: walidacja buildu wewnątrz procesu nie powiodła się | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | tylko rejestrowane |
| `headless.arm.failed` | `PluginRuntime.Start`: nie udało się uzbroić natywnego hooka startu headless | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | tylko rejestrowane |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart`, gdy `InternetTextures.Mode` ma wartość `Disabled` | tylko rejestrowane |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 lub 4) | NEW_MAP: OMSI nie jest jeszcze gotowy; start jest ponawiany przy następnym tyknięciu timera interfejsu użytkownika | tylko rejestrowane |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | ścieżka powodzenia NEW_MAP | tylko rejestrowane |
| `world.failed` (`native_status`) | NEW_MAP: natywny start zwrócił błąd | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | ścieżka SAVED_SITUATION | tylko rejestrowane (bez zmiany stanu) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: natywny start zwrócił błąd | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: pola wyboru punktu wejścia lub `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | koniec startu świata | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle`, gdy dowolna operacja `d3d.*` aktywowała sondę | tylko rejestrowane |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle`, gdy ponowne zastosowanie aktywnego `camera.lock` zgłasza wyjątek (raportowane raz dla każdego odrębnego błędu) | tylko rejestrowane |
| Nieprawidłowy JSON | dowolne | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Zastrzeżenia: slot przechowuje jedną próbkę, więc zdarzenia wyemitowane w trakcie jednego 100-milisekundowego odpytania hosta mogą zostać utracone (wtyczka wstrzymuje zdarzenia cyklu życia przez 2 s po `gameplay.entered` i nigdy nie publikuje zdarzenia D3D w tym samym tyknięciu, w którym publikuje `gameplay.entered`, więc granica `Running` nie zostaje przeoczona). `RuntimeEvents` przechowuje 256 najnowszych zdarzeń; starsze są odrzucane. Nie jest to bezstratny log. Zdarzenia należy odczytywać przez `GetStatusAsync`, `session.events` na płaszczyźnie sterowania lub `events read|watch` w CLI.

<a id="startup-timeout"></a>
## Limit czasu uruchamiania

| Element | Wartość |
| --- | --- |
| Źródło | `LaunchSpec.Behavior.StartupTimeoutSeconds` (domyślnie 180; 1..600; CLI `/startup-timeout`, profil `behavior.startup-timeout`). |
| Początek odliczania | Gdy zadanie nadzorcy wchodzi w swoją pętlę (po zwróceniu uchwytu). |
| Upływ przed `PluginBootstrap` | `Failed` z `OL_E_PLUGIN_NOT_LOADED`. |
| Upływ po `PluginBootstrap`, przed `Running` | `Failed` z `OL_E_STARTUP_TIMEOUT`. |
| Po `Running` | Żaden limit czasu nie obowiązuje; sesja trwa, dopóki OMSI nie zakończy działania lub nie zostanie zażądane zatrzymanie. |
| Właściciel w CLI | Czeka `StartupTimeoutSeconds + 5` sekund na `Running`; w razie niepowodzenia wypisuje stan, w `OmsiLaunchW.exe` wyświetla okno dialogowe z ostatnim komunikatem diagnostycznym `OL_E_` (kod zastępczy `OL_E_SESSION_START_FAILED`) i kończy działanie z kodem 1 po `CloseAsync`. |

`ShutdownTimeoutSeconds` jest przenoszony w specyfikacji, ale nie jest wykorzystywany: nie ma oczekiwania na zamknięcie.

<a id="stop-semantics"></a>
## Semantyka zatrzymania

Każde żądanie zatrzymania jest tym samym kanonicznym żądaniem: `StopAsync(handle)` z API, `CloseAsync` na sesji w stanie niekońcowym, `session.stop` na lokalnej płaszczyźnie sterowania (powiązane z identyfikatorem aktywnej sesji), „End session” w obszarze powiadomień, Ctrl+C lub zamknięcie konsoli we właścicielu w CLI oraz upływ `/observe-seconds`.

| Krok | Szczegóły |
| --- | --- |
| 1 | W aktywnej sesji ustawiany jest `StopRequested`; wywołujący otrzymuje wynik natychmiast. |
| 2 | W ciągu 100 ms nadzorca opuszcza pętlę i wywołuje `TerminateProcess(Omsi.exe, 1)`. Jest to wymuszone zakończenie procesu: procedura zamykania OMSI nie jest wykonywana, OMSI nie zapisuje ponownie `options.cfg`, nie pojawia się okno zapisu. Jest to zamierzone, aby OMSI nie mógł nadpisać plików, które transakcja zaraz przywróci. |
| 3 | Nadzorca czeka na zakończenie procesu, rejestruje `ProcessExited`, dokładnie przywraca każdy plik należący do sesji (w tym znacznik `closecheck` zapisany przez OMSI w trakcie sesji, który staje się uwagą `restore.session-artifact-removed`), usuwa dziennik i kopie zapasowe, zwalnia magazyny runtime (późniejsze wywołania `ExecuteRuntimeAsync` zgłaszają `OL_E_RUNTIME_CHANNEL_CLOSED` lub `OL_E_SESSION_NOT_RUNNING`), zwalnia dzierżawę i przechodzi do `Completed`. |
| Samodzielne zakończenie | Jeśli OMSI zakończy działanie samodzielnie po `Running` (użytkownik zamyka OMSI), wykonywana jest ta sama ścieżka bez wymuszonego zakończenia, a sesja kończy się normalnie. Przed `Running` jest to `OL_E_PROCESS_EXITED_EARLY`. |
| Kooperacyjne zamknięcie | Niezaimplementowane. Wysyłanie `WM_CLOSE` i oczekiwanie przez `ShutdownTimeoutSeconds` nie jest zaimplementowane (decyzja produktowa; w rundzie domknięcia runtime OMSI ignorował `WM_CLOSE` wysłane do swojego okna głównego, `L05b`) ([stan walidacji runtime](../status/runtime-validation-status.md)). |
| Stan po stronie runtime | Wszystko, co zmieniono za pomocą operacji runtime (zegar, kamera, utworzone pojazdy, zmienne skryptowe, tekstury D3D), jest stanem wewnątrz procesu i znika wraz z procesem; nigdy nie jest przywracane ani utrwalane. |

<a id="waitforasync-semantics"></a>
## Semantyka `WaitForAsync`

| Sytuacja | Wynik |
| --- | --- |
| Sesja osiąga żądany stan | Zwraca stan z `State == requested`. |
| Sesja najpierw osiąga stan końcowy | Zwraca natychmiast z `Completed` lub `Failed` (należy sprawdzić `Diagnostics`). |
| Upływa limit czasu | Zwraca bieżący stan (bez wyjątku). Należy porównać `State` z żądanym stanem. |
| Żądany stan już minął (lub nigdy nie jest ustawiany: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, w praktyce `Snapshotting`) | Czeka do osiągnięcia stanu końcowego lub upływu limitu czasu. |
| Wywołujący anuluje | `OperationCanceledException`. |
| Nieznany lub zamknięty uchwyt | `KeyNotFoundException`. |

Interwał odpytywania wynosi 100 ms, więc obserwowane przejścia są opóźnione względem rzeczywistych o maksymalnie 100 ms.

<a id="owner-lifecycle-guarantees-cli"></a>
## Gwarancje cyklu życia właściciela (CLI)

`OwnerSession.RunAsync` w `tools/OmsiLaunch.Cli/Program.cs` jest referencyjnym właścicielem.

| Gwarancja | Szczegóły |
| --- | --- |
| Jeden właściciel | Przed uruchomieniem CLI sonduje potok sterowania; jeśli odpowie właściciel, odmawia z `OL_E_SESSION_ALREADY_ACTIVE` (kod wyjścia 7). Dzierżawa wymusza tę samą regułę między procesami. |
| Każda ścieżka wyjścia prowadzi do `CloseAsync` | Od `StartSessionAsync` wyjątki, Ctrl+C (`CancelKeyPress`), zamknięcie konsoli/wylogowanie (`ProcessExit`: żądane jest zatrzymanie, a właściciel czeka do 4 s na `Completed`; wszystko, co pozostanie, jest odzyskiwane z dziennika przy następnym uruchomieniu), zatrzymanie z obszaru powiadomień, `session.stop` z płaszczyzny sterowania, upływ `/observe-seconds` i naturalne zakończenie – wszystkie kończą się w bloku `finally`, który zwalnia płaszczyznę sterowania i ikonę w obszarze powiadomień oraz oczekuje na `CloseAsync`. |
| `/observe-seconds` jest górnym ograniczeniem | Żądania zatrzymania z obszaru powiadomień lub płaszczyzny sterowania nadal mogą zakończyć sesję wcześniej. |
| Płaszczyzna sterowania tylko w stanie Running | Punkt końcowy potoku nazwanego (named pipe) jest tworzony po `Running` (i po ewentualnych partiach walidacji `INTERNAL`) i zwalniany przed `CloseAsync`; w innych momentach klienci otrzymują `OL_E_NO_ACTIVE_SESSION`. |
| Kod wyjścia | 0, gdy stan końcowy to `Completed`, 1, gdy to `Failed` lub nie osiągnięto rozgrywki, 8, gdy żądane odzyskiwanie nie zostało ukończone ([kody wyjścia](../reference/exit-codes.md)). |
| Diagnostyka | Ślad hosta i artefakty operacji runtime w `<root>\.omsilaunch\diagnostics`, log obszaru powiadomień `tray-host.log`; żadne dane nie opuszczają komputera. |

Integratorzy piszący własnego właściciela muszą odtworzyć dwie pierwsze gwarancje: jedno `StartSessionAsync` na instalację w danym momencie oraz `CloseAsync` na każdej ścieżce.

<a id="failure-map"></a>
## Mapa błędów

| Faza | Stan w chwili błędu | Widoczne komunikaty diagnostyczne |
| --- | --- | --- |
| Planowanie | brak (brak sesji) | `OL_E_PLAN_NOT_RUNNABLE` zgłaszany przez `StartSessionAsync`; własne kody `OL_E_` planu ([walidacja LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Uruchamianie (od dzierżawy do utworzenia procesu) | `Failed` | `OL_E_START_SESSION` z kodem wewnętrznym; ewentualnie `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Inicjalizacja wtyczki | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Start świata | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Działanie | `Failed` tylko przy błędach nadzorcy | `OL_E_PROCESS_SUPERVISION`; błędy operacji runtime nigdy nie powodują niepowodzenia sesji. |
| Zakończenie procesu i przywracanie | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Każda ścieżka błędu nadal podejmuje próbę zakończenia procesu i przywrócenia; dziennik, który pozostanie, jest odzyskiwany przy następnym uruchomieniu lub przez `RecoverPendingAsync` / `/recover` ([transakcje i odzyskiwanie](transactions-and-recovery.md)).
