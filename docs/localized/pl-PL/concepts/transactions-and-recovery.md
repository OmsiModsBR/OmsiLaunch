# Transakcje i odzyskiwanie

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../concepts/transactions-and-recovery.md) dla OmsiLaunch 0.1.0-beta3. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Każda sesja OmsiLaunch, która modyfikuje plik OMSI, robi to wewnątrz trwałej transakcji zapisywanej w dzienniku: oryginalne bajty są kopiowane do kopii zapasowej, zanim zostaną zastąpione, dziennik rejestruje, jak daleko doszła sesja, a przywracanie weryfikuje każdą kopię zapasową przed jej ponownym zapisaniem. Ta strona opisuje tę transakcję w postaci zaimplementowanej przez `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) i sterowanej przez `OmsiLaunchService.StartAsync`, `SuperviseAsync` i `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), wraz z plikami wejściowymi obliczanymi przez `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Jest przeznaczona dla użytkowników, którzy muszą wiedzieć, co zmienia sesja i co robi odzyskiwanie, oraz dla integratorów, którzy potrzebują dokładnych gwarancji.

<a id="what-a-session-changes"></a>
## Co zmienia sesja

Do transakcji wchodzą wyłącznie **tymczasowe nakładki** (overlay). Są obliczane przed otwarciem transakcji i przywracane przy jej zamknięciu.

| Dane wejściowe sesji | Plik(i) | Rodzaj |
| --- | --- | --- |
| `/set:<key>=<value>`, profil `settings`, `LaunchSpec.Environment.*` | `options.cfg` (semantyczne poprawki tokenów; bajty CP1252 zachowane, UTF-8/UTF-16 oznaczone BOM respektowane) | nakładka |
| Zarządzany ekran startowy (`SplashMode.Managed`, domyślnie) | `GUI\NewSplashscreen_ENG.bmp` i `GUI\NewSplashscreen_<language>.bmp` | nakładka (plik zlokalizowany jest tworzony przez transakcję, gdy instalacja go nie ma) |
| Tekstury internetowe `Override` | `Texture\standard.itx` | nakładka |
| Tekstury internetowe `Override` | każdy cel wymieniony w `.itx`, a także `Texture\standard.ipr` | usunięcie na czas sesji |
| Zawsze | `closecheck` (gdy nie istnieje przed sesją) | usunięcie na czas sesji |

Stałe pliki produktu **nie** są uczestnikami transakcji: zestaw plików wtyczki (closure) w `plugins\OmsiLaunch.*` (tylko walidowany, zob. [stała wtyczka](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (kopiowane raz, nigdy nieusuwane), diagnostyka w `.omsilaunch\diagnostics`, pakiety profili sesji oraz dokumentacja i przykłady z wydania. Wtyczki firm trzecich i wszystkie inne pliki OMSI nigdy nie są wyliczane, kopiowane, usuwane ani przywracane.

Sam OMSI w trakcie sesji nadal zapisuje własny stan, dokładnie tak jak przy zwykłym uruchomieniu OMSI: `options.cfg` (na przykład `[last_map]`, gdy sesja wczytuje inną mapę, zapisywane ponownie przy wejściu do rozgrywki), `Texture\standard.ipr`, pamięci podręczne rozkładów i map oświetlenia (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, profil kierowcy w `Drivers\` oraz swoje logi. Zapis do ścieżki należącej do sesji (powyżej) jest cofany przez przywracanie; każdy inny zapis OMSI pozostaje po sesji, tak samo jak po bezpośrednim uruchomieniu OMSI. Dowody z rundy domknięcia runtime: sesja zapisanej sytuacji na innej mapie pozostawiła zmienione `[last_map]`, ponieważ nie nakładała `options.cfg` (`CAM01`), natomiast sesje z `/set` przywróciły `options.cfg` dokładnie (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Stany transakcji

`TransactionState` jest utrwalany w dzienniku po każdym przejściu. Wartości są serializowane jako liczby całkowite przez `System.Text.Json`.

| Wartość | Stan | Zapisywany, gdy |
| --- | --- | --- |
| 0 | `Prepared` | Wykonano migawki każdej ścieżki nakładki i usunięcia, a ich kopie zapasowe zostały zapisane na dysk. W instalacji nic się jeszcze nie zmieniło. To jest zobowiązanie do odzyskiwania: od tego momentu awaria pozostawia dziennik możliwy do odzyskania. |
| 1 | `Applied` | Każda nakładka została atomowo zapisana, a każde usunięcie wykonane. |
| 2 | `RuntimeDeployed` | Integralność stałej wtyczki została zwalidowana dla tego uruchomienia (nic nie jest wdrażane; nazwa jest historyczna). |
| 3 | `HandoffCreated` | Handoff uruchamiania, slot telemetrii i skrzynka runtime istnieją jako nazwana pamięć współdzielona. |
| 4 | `ProcessStarted` | Utworzono `Omsi.exe`. Dziennik zawiera teraz również `ProcessId`, `ProcessStartFileTimeUtc` (czas utworzenia, tyknięcia UTC) i `ExecutablePath`. |
| 5 | `ProcessExited` | Nadzorca potwierdził zakończenie procesu (samodzielne zakończenie lub `TerminateProcess`). |
| 6 | `Restoring` | Przywracanie się rozpoczęło. |
| 7 | `Restored` | Każdy należący do sesji plik został przywrócony i zweryfikowany. Bezpośrednio potem dziennik jest usuwany, a `backup\<session>` kasowany. |
| 8 | `Completed` | Zadeklarowany w enum, ale nigdy nieutrwalany; ukończona transakcja nie ma dziennika. |

Cykl życia zwykłej sesji wygląda więc następująco: migawka -> `Prepared` -> nakładki zapisane / usunięcia wykonane -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> dziennik usunięty -> `backup\<session>` usunięty. Publiczne wartości `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` i `Completed` śledzą ten sam postęp z zewnątrz (zob. [cykl życia sesji](session-lifecycle.md)).

Sesja bez żadnych modyfikacji należących do niej plików również zapisuje dziennik dla swojego cyklu życia; jej przywrócenie jest zweryfikowaną operacją pustą.

<a id="journal-file"></a>
## Plik dziennika

Ścieżka: `<root>\.omsilaunch\journal.json`. Na instalację przypada najwyżej jeden dziennik; jego obecność oznacza „transakcja oczekuje”.

Pola `TransactionJournal`:

| Pole | Typ | Znaczenie |
| --- | --- | --- |
| `SessionId` | GUID | Sesja będąca właścicielem dziennika; także nazwa katalogu kopii zapasowej (format `N`). |
| `State` | liczba całkowita | `TransactionState` powyżej. |
| `Files` | tablica `JournalFile` | Jeden wpis na każdą ścieżkę należącą do sesji. |
| `ProcessId` | liczba całkowita lub null | PID OMSI, od `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long lub null | Czas utworzenia OMSI (tyknięcia UTC), od `ProcessStarted`. |
| `ExecutablePath` | string lub null | Pełna ścieżka uruchomionego `Omsi.exe`, od `ProcessStarted`. |

Pola `JournalFile`:

| Pole | Typ | Znaczenie |
| --- | --- | --- |
| `RelativePath` | string | Ścieżka względem katalogu głównego instalacji (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Czy plik istniał przed sesją. |
| `Sha256` | string szesnastkowy | SHA-256 oryginalnych bajtów (pustej tablicy bajtów, gdy `Existed` ma wartość false). |
| `BackupPath` | string | Ścieżka bezwzględna kopii zapasowej (zapisywana tylko, gdy `Existed`). |
| `AppliedSha256` | string szesnastkowy lub null | SHA-256 bajtów nakładki, które sesja zapisała w tej ścieżce; null dla usunięć na czas sesji. Jest to odcisk własności dla plików pierwotnie nieistniejących. |
| `LastWriteTimeUtcTicks` | long lub null | Oryginalny czas ostatniego zapisu. |
| `CreationTimeUtcTicks` | long lub null | Oryginalny czas utworzenia. |
| `Attributes` | liczba całkowita lub null | Oryginalne `FileAttributes` (w tym `ReadOnly`). |
| `SessionDeletion` | bool | True dla ścieżek, które sesja kazała utrzymywać jako nieistniejące (cele `.itx`, `Texture\standard.ipr`, `closecheck`). |

Dzienniki zapisane przez wcześniejsze buildy bez `AppliedSha256` i pól metadanych nadal są czytelne; zob. [Pliki pierwotnie nieistniejące](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Układ kopii zapasowych

| Ścieżka | Zawartość |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Jeden katalog na sesję, tworzony wraz z dziennikiem w stanie `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Dokładne oryginalne bajty jednego istniejącego pliku należącego do sesji. Pliki pierwotnie nieistniejące nie mają kopii zapasowej. |

Kopie zapasowe i dziennik są zapisywane za pomocą pliku tymczasowego (`<path>.omsilaunch.tmp`), z zapisem bezpośrednim (write-through) i jawnym `Flush(true)`, a następnie atomowym `File.Move` z nadpisaniem. Plik tymczasowy jest zawsze usuwany, nawet w razie błędu. Ta sama ścieżka zapisu jest używana dla nakładek i przywracanych oryginałów, więc żaden plik `*.omsilaunch.tmp` nie przetrwa ukończonej operacji.

Kopie zapasowe są usuwane dopiero po usunięciu dziennika, który się do nich odwoływał. Niepowodzenie usunięcia `backup\<session>` jest kosmetyczne i nigdy nie cofa zweryfikowanego przywrócenia.

<a id="restore"></a>
## Przywracanie

`RestoreAsync` jest wykonywane po `ProcessExited` (lub w trakcie odzyskiwania). Dla każdej ścieżki zapisanej w dzienniku:

| Stan pierwotny | Action |
| --- | --- |
| Istniał | Obliczany jest hash bajtów kopii zapasowej i porównywany z `Sha256`; niezgodność przerywa operację z `OL_E_RECOVERY_BACKUP_CORRUPT`, zanim cokolwiek zostanie zapisane. Następnie bajty są zapisywane atomowo (bieżącemu plikowi tylko do odczytu najpierw usuwany jest ten atrybut), a czas utworzenia, czas ostatniego zapisu i atrybuty są przywracane (`RestoreMetadata`; błędy metadanych są ignorowane, aby problem z uprawnieniami nie mógł zablokować przywrócenia co do bajtu). |
| Nie istniał, teraz istnieje, `AppliedSha256` znany | Obliczany jest hash bieżących bajtów. Jeśli jest równy `AppliedSha256`, plik jest własną nakładką sesji i zostaje usunięty. W przeciwnym razie `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` przerywa przywracanie, a dziennik jest zachowywany. |
| Nie istniał, teraz istnieje, usunięcie na czas sesji, dziennik osiągnął `ProcessStarted` | Plik jest produktem ubocznym sesji (OMSI działał przy utrzymywanej dzierżawie instalacji, a ta ścieżka miała pozostać nieistniejąca). Jest usuwany i raportowany jako komunikat diagnostyczny `restore.session-artifact-removed` z SHA-256 usuniętej zawartości. |
| Nie istniał, teraz istnieje, usunięcie na czas sesji, proces nigdy nie został uruchomiony | Plik pochodzi spoza sesji. Jest zachowywany, raportowany jako `OL_W_RESTORE_FOREIGN_FILE_RETAINED` ze swoim SHA-256, a transakcja i tak zostaje ukończona. |
| Nie istniał, teraz istnieje, brak dowodu własności (dziennik sprzed wprowadzenia odcisków) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; dziennik jest zachowywany. |
| Nie istniał, nadal nie istnieje | Nic do zrobienia. |

Po przetworzeniu wszystkich plików `VerifyRestoredSnapshots` ponownie odczytuje każdą ścieżkę: istniejące oryginały muszą mieć hash równy `Sha256`, a ścieżki pierwotnie nieistniejące muszą nie istnieć, chyba że zostały jawnie zachowane. Dopiero wtedy utrwalany jest `Restored`, usuwany dziennik (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, jeśli przetrwa) i usuwany katalog kopii zapasowych. Awaria między `Restored` a usunięciem dziennika powoduje jedynie idempotentne ponowne odtworzenie.

Uwagi dotyczące przywracania (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) pojawiają się jako wpisy `LaunchDiagnostic` w `SessionStatus.Diagnostics` (z `sha256` w `Data`) oraz w `RecoveryStatus.Diagnostics`, więc nic nie jest usuwane ani zachowywane bez powiadomienia.

<a id="originally-absent-files-and-ownership"></a>
### Pliki pierwotnie nieistniejące i własność

Nakładka zapisana w ścieżce, która nie istniała, jest usuwana przy przywracaniu **tylko wtedy, gdy jej zawartość nadal odpowiada temu, co zastosowała sesja** (`AppliedSha256`). Jeśli w trakcie sesji coś innego ją zastąpiło, przywracanie kończy się błędem `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, a dziennik jest zachowywany do inspekcji.

Ścieżka usunięcia na czas sesji (cele `.itx`, `Texture\standard.ipr`, `closecheck`), która nie istniała przed sesją, ale istnieje po niej, jest oceniana według tego, czy OMSI działał w ramach tej transakcji: jeśli dziennik osiągnął `ProcessStarted`, plik jest produktem ubocznym sesji i zostaje usunięty (`restore.session-artifact-removed`); jeśli proces nigdy nie został uruchomiony, plik jest zachowywany i raportowany jako `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, a dziennik i tak zostaje ukończony.

### `closecheck`

`closecheck` to własny znacznik awarii OMSI (obecny, gdy OMSI nie zamknął się poprawnie). Obowiązują dwie reguły:

- Jeśli istnieje **przed** sesją, a `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` ma wartość `true` (domyślnie), jest trwale usuwany przed otwarciem transakcji i rejestrowany jako komunikat diagnostyczny `closecheck.stale-removed` ze swoim SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED`, jeśli usunięcie się nie powiedzie). Jest to udokumentowana trwała zmiana, a nie uczestnik transakcji. Przy fladze `false` znacznik pozostaje, a OMSI wyświetla swoje ostrzeżenie.
- Jeśli **nie** istnieje przed sesją, `closecheck` jest dodawany jako usunięcie na czas sesji. Ponieważ sesja kończy się przez `TerminateProcess` (procedura zamykania OMSI nie jest wykonywana), znacznik zapisany przez OMSI przy starcie zawsze pozostaje później na miejscu; jest usuwany przy przywracaniu jako artefakt sesji.

<a id="early-recovery-order"></a>
## Kolejność wczesnego odzyskiwania

Przy każdym `StartSessionAsync`, po uzyskaniu dzierżawy instalacji i zanim cokolwiek odczyta aktywną instalację:

1. Transakcja służąca wyłącznie do odzyskiwania sprawdza obecność `journal.json`. Jeśli istnieje, `RestorePendingAsync` jest wykonywane natychmiast, więc nakładki i język ekranu startowego nowej sesji wynikają z plików **oryginalnych**, nigdy z pozostałości po poprzedniej sesji.
2. Jeśli to odzyskiwanie kończy się błędem `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (dziennik sprzed wprowadzenia odcisków), odzyskiwanie jest **odraczane**: nowa sesja buduje swoje nakładki, a nowa transakcja ponawia odzyskiwanie, używając własnych zaplanowanych bajtów jako dowodu własności (pierwotnie nieistniejący plik, którego zawartość jest równa nowej nakładce, jest akceptowany jako należący do OmsiLaunch). Każdy inny błąd odzyskiwania powoduje niepowodzenie uruchomienia.
3. Dopiero wtedy walidowany jest zestaw plików wtyczki, obliczany hash `Omsi.exe`, obsługiwany `closecheck`, a nowa transakcja przygotowywana i stosowana.

Ślad hosta rejestruje `PENDING_JOURNAL_RECOVERED` lub `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Odzyskiwanie po awarii i aktywność właściciela

Odzyskiwanie nigdy nie zastępuje plików pod działającym OMSI. `RestorePendingAsync` odmawia z `OL_E_INSTALLATION_BUSY`, dopóki właściciel zapisany w dzienniku działa:

| Zawartość dziennika | Test aktywności |
| --- | --- |
| Zarejestrowane `ProcessId` i `ProcessStartFileTimeUtc` | Proces o tym PID musi działać, jego czas uruchomienia musi się zgadzać (odrzuca ponowne użycie PID), a gdy zarejestrowano `ExecutablePath`, jego moduł główny musi mieć tę ścieżkę (działający niepowiązany proces nie może zatrzymać transakcji). |
| Brak PID, stan między `HandoffCreated` (włącznie) a `ProcessExited` (wyłącznie) | Host zakończył działanie między `CreateProcess` a zapisem dziennika. Każdy `Omsi.exe`, którego moduł główny to `<root>\Omsi.exe`, jest traktowany jako właściciel. |
| Brak PID, inne stany | Nieaktywny; odzyskiwanie jest kontynuowane. |

Jawne odzyskiwanie jest udostępnione jako `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, zwracające `RecoveryStatus(Pending, Recovered, Diagnostics)`; najpierw uzyskuje dzierżawę instalacji (`OL_E_INSTALLATION_BUSY`, gdy trzyma ją inny właściciel). W CLI `/recovery-status` raportuje bez przywracania, a `/recover` przywraca; kod wyjścia 8 (`TransactionRecoveryFailed`) jest zwracany, gdy zażądano przywrócenia, a dziennik nadal pozostaje oczekujący. Zob. [CLI](../reference/cli.md) i [publiczne API](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Odroczone przywracanie na końcu sesji

Jeśli nadzorca nie może potwierdzić, że OMSI zakończył działanie (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` lub błąd sprzątania raportowany jako `OL_E_PROCESS_CLEANUP_FAILED`), sesja kończy się błędem `OL_E_RESTORE_DEFERRED`, a dziennik jest celowo zachowywany: zastępowanie plików instalacji, gdy OMSI może je nadal odczytywać, jest niebezpieczne. Następne uruchomienie (lub `/recover`) przywraca pliki, gdy proces już nie istnieje. Przywracanie, które kończy się niepowodzeniem z jakiegokolwiek innego powodu, kończy sesję z `OL_E_RESTORE_FAILED`; dziennik pozostaje, dopóki każdy należący do sesji oryginał nie zostanie przywrócony i zweryfikowany.

<a id="the-installation-lease"></a>
## Dzierżawa instalacji

Dzierżawa to nazwany semafor `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` o liczniku 1. Katalog główny jest normalizowany przez `InstallationLease.NormalizeRoot` (pełna ścieżka, końcowe separatory usunięte z wyjątkiem katalogu głównego dysku), więc `C:\OMSI`, `C:\OMSI\` i `c:\omsi\sub\..` współdzielą jedną dzierżawę; nazwa lokalnego potoku sterowania korzysta z tej samej normalizacji. Dzierżawa jest uzyskiwana przez `StartSessionAsync` (stan `AcquiringInstallationLock`) i przez `RecoverPendingAsync`, a zwalniana, gdy zakończy się zadanie cyklu życia sesji lub wywołanie odzyskiwania zwróci wynik. `OL_E_INSTALLATION_BUSY` jest zgłaszany natychmiast, gdy nie można jej uzyskać (bez oczekiwania).

Zaakceptowane ograniczenia (udokumentowane, bez planowanej zmiany):

- Zakres `Local\`: jeden właściciel na instalację **na sesję logowania**. Dwaj interaktywni użytkownicy na tym samym komputerze nie wykluczają się wzajemnie.
- Semafor nie jest zwalniany przez awarię, dopóki jakikolwiek inny proces nadal trzyma do niego uchwyt; w przeciwieństwie do porzuconego muteksu nie ma właściciela. Nieaktualny posiadacz pozostawia instalację w stanie `OL_E_INSTALLATION_BUSY`, dopóki ten uchwyt nie zostanie zamknięty.
- Każdy proces tego samego użytkownika Windows może utworzyć tę nazwę jako pierwszy i ją utrzymywać.

<a id="omsilaunch-directory"></a>
## Katalog `.omsilaunch`

| Wpis | Czas życia | Właściciel |
| --- | --- | --- |
| `journal.json` | Tymczasowy; istnieje tylko, gdy transakcja oczekuje | Transakcja |
| `backup\<sessionId>\*.bin` | Tymczasowy; usuwany po dzienniku | Transakcja |
| `diagnostics\<sessionId>-host.log` | Trwały; retencja zachowuje 50 najnowszych sesji (starsze pliki z prefiksem sesji są usuwane przy starcie nowej sesji) | Ślad hosta |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Trwały (ta sama retencja) | CLI |
| `diagnostics\tray-host.log` | Trwały | Host obszaru powiadomień Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Trwałe zasoby produktu; kopiowane raz z pakietu, nigdy nienadpisywane ani nieusuwane | Zasoby wizualne sesji |
| `session-profiles\<id>\` | Trwały; instalowany przez użytkownika lub autora zawartości | Użytkownik |
| `profiles\` | Nie jest tworzony ani odczytywany przez obecny kod; zarezerwowany | brak |
| `docs\`, `examples\` | Trwałe; dostarczane przez pakiet wydania | Pakiet |

Żadne dane nie opuszczają komputera; diagnostyka to wyłącznie pliki lokalne. Zob. też [katalog `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Zmiany runtime nie są zapisywane w dzienniku

Operacje sterowania runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, tekstury D3D) zmieniają wyłącznie stan OMSI w pamięci. Nie są rejestrowane w dzienniku i nie są przywracane; znikają wraz z procesem. Zob. [sterowanie runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Tryby awarii i kody błędów

| Kod | Znaczenie | Dziennik po zdarzeniu |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Dzierżawę trzyma inny właściciel lub proces OMSI zapisany w dzienniku nadal działa | zachowany |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Zażądano przywrócenia transakcji mającej migawki, ale bez dziennika na dysku | n/d |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | Hash kopii zapasowej różni się od odcisku migawki; nic nie zostało zapisane | zachowany |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Ścieżka pierwotnie nieistniejącej nakładki zawiera teraz treść, której sesja nie zapisała | zachowany |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Dziennik sprzed wprowadzenia odcisków ze ścieżką pierwotnie nieistniejącą, która teraz istnieje; zamknąć go może tylko nowa sesja z identycznymi zaplanowanymi bajtami | zachowany (odroczony) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | Nie udało się usunąć `journal.json` po zweryfikowanym przywróceniu | zachowany (ponowne odtworzenie jest idempotentne) |
| `OL_E_RESTORE_DEFERRED` | Nie potwierdzono zakończenia OMSI; przywrócenie odłożone do następnego uruchomienia | zachowany |
| `OL_E_RESTORE_FAILED` | Każdy inny błąd przywracania (niezgodność obecności lub hasha po przywróceniu, błąd we/wy) | zachowany |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Nie udało się usunąć nieaktualnego `closecheck` przed transakcją | jeszcze brak |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Ostrzeżenie: obcy plik w ścieżce usunięcia na czas sesji został zachowany | ukończony |
| `OL_E_PLAN_NOT_RUNNABLE` | Ponowne planowanie przy uruchomieniu wykazało, że specyfikacja nie jest już możliwa do uruchomienia (na przykład zmieniony `Omsi.exe`); żadna transakcja nie jest otwierana | brak |

CLI mapuje `OL_E_RECOVERY_*` i `OL_E_RESTORE_FAILED` na kod wyjścia 8, a `OL_E_INSTALLATION_BUSY` na kod wyjścia 7; zob. [kody wyjścia](../reference/exit-codes.md).

<a id="evidence"></a>
## Dowody

Testy offline w `tools/OmsiLaunch.TestHost` pokrywają ścieżki transakcji: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Dowody z runtime (macierz walidacji): RV-005 i RV-006 (nakładka zastosowana i przywrócenie co do bajtu, sesje `1e8e0548-...` oraz partia prezentacji), RV-008 – pozytywny wynik dla wczesnego zakończenia (sesja `0dc40570-...`).

Dowody z runtime (runda domknięcia runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- usunięcie artefaktów sesji dla celów `.itx` z rzeczywistym mechanizmem pobierania OMSI, przy normalnym zatrzymaniu oraz po przerwaniu działania właściciela i `/recover` (S-01, `I01`, `I02`);
- wczesne odzyskiwanie przed zbudowaniem nakładek (S-05, `S05`);
- przywracanie metadanych i atrybutu tylko do odczytu oraz sprzątanie kopii zapasowych (S-12, `S12a`, `S12b`);
- `/recover` pod dzierżawą, z osieroconym OMSI oraz w oknie przed zapisem PID (S-04, `S04`, `S04b`);
- błędy uruchamiania oraz nieudane przywrócenie, po którym wykonano `/recover` (pozostała część RV-008, `SF01`, `SF02`, `F01`);
- zachowanie CP1252 (S-07, `C01`).

Odroczona gałąź odzyskiwania dla dzienników sprzed wprowadzenia odcisków pozostaje pokryta tylko testami offline. Zob. [stan walidacji runtime](../status/runtime-validation-status.md).
