# Znane ograniczenia

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/known-limitations.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona wymienia, na podstawie kodu, wszystko w OmsiLaunch 0.1.0-beta4, co jest `UNAVAILABLE`, `PARTIAL` lub stanowi zaakceptowane ryzyko, tak aby użytkownicy i integratorzy nie opierali się na zachowaniu, którego produkt nie zapewnia. Każdy wiersz podaje ograniczenie, jego stabilność, przyczynę oraz miejsce, w którym zostało szczegółowo udokumentowane. Dokumentacja angielska jest normatywna; zlokalizowane kopie w `docs/localized/` nie są utrzymywane na tym samym poziomie i mogą być nieaktualne (zob. ostatnią sekcję).

<a id="compatibility"></a>
## Zgodność

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Możliwości odczytujące układy obiektów (pojazdy, postacie, pogoda, mapa, kamera, rozkład jazdy, skrypty, kierowcy, bilety) są weryfikowane tylko w znanym układzie (`Omsi23004`); w innym buildzie OMSI 2 są niedostępne (`OL_E_CAPABILITY_UNRESOLVED`), dopóki nie powstaną walidatory strukturalne | `PARTIAL` w nieznanych buildach | [zgodność](compatibility.md) |
| Oryginalny plik wykonywalny Steam (chroniony DRM) nie został zweryfikowany w runtime; kontrolowana kopia LAA została | `PARTIAL` dla Steam | [zgodność](compatibility.md) |
| Zweryfikowany w runtime jest tylko znany układ; rozwiązywanie niezależne od buildu jest weryfikowane offline względem pliku referencyjnego z ukrytą tożsamością | `EXPERIMENTAL` w nieznanych buildach | [stan weryfikacji w runtime](../status/runtime-validation-status.md) |
| Tylko Windows 10+ x64; brak Windows 7/8, brak XP, brak ARM64 | `UNAVAILABLE` | [zgodność](compatibility.md) |
| Wtyczka wymaga runtime .NET 6 w wersji **x86** oprócz runtime x64 używanego przez kontroler | | [zgodność](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Uruchamianie świata i opcje uruchomienia

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) jest zaimplementowany na podstawie analizy statycznej gałęzi ostatniego stanu OMSI i zweryfikowany wyłącznie offline | `EXPERIMENTAL` | [CLI](cli.md) |
| Jawna lub systemowa data, godzina i rok (`/date`, `/time`, `/year`, profil `new.date`/`new.time`/`new.year`) dotyczą tylko uruchomień NEW_MAP i są zweryfikowane offline; weryfikacja w runtime w toku. Nie można zażądać 31 grudnia roku przestępnego (kalendarz OMSI zawija dzień 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Ustawienie wstępne pogody, ICAO i bieżąca rzeczywista pogoda przy starcie (`/weather*`, `new.weather`) są stosowane zaraz po uruchomieniu świata przez własne wczytywanie pogody i flagi METAR OMSI (zamknięte statycznie, BI-003); weryfikacja w runtime w toku. Pogoda, którą OMSI pokazuje w pierwszej klatce, może nadal być domyślną pogodą mapy | `EXPERIMENTAL` | [CLI](cli.md) |
| Model pojazdu gracza, malowanie, HOF, numer taborowy i numer rejestracyjny przy starcie (rodzina `/vehicle`, `PlayerVehicleSpec`) są rozwiązywane względem katalogu zawartości, ale nie stosowane; ich zażądanie czyni plan niemożliwym do uruchomienia. Natywna sekwencja przypisania (ustawienie przez okno pozycji OMSI, `MakeVehicle`, kroki finalizacji dla każdej jednostki, `CopyTempListIntoMainList`, indeks gracza) jest zamknięta statycznie, ale tworzy i rejestruje działający pojazd i nie jest zaimplementowana, zanim będzie można ją zweryfikować w runtime; jest planowana na późniejszą betę. Zapisana sytuacja lub `/last` przywraca własny pojazd gracza | `UNAVAILABLE` (BI-007) | `player.assign-headless` w [możliwościach](capabilities.md) |
| Punkt wejścia według tożsamości (`/entrypoint:<identity>`) wybiera dokładną etykietę prezentowaną przez OMSI (zamknięte statycznie, BI-001); późniejszy rekord o tej samej etykiecie nie może zostać wybrany, tak jak w oknie samego OMSI. Weryfikacja w runtime w toku | `EXPERIMENTAL` | możliwość planu `world.entrypoint-identity` |
| Ustawienia klawiatury i kontrolerów według klucza (`Environment.Keyboard`, `Environment.Controllers`) nie mają skatalogowanych kluczy: zamiast tego całe pliki są zastępowane przez `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| Kooperacyjne zamykanie (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) jest opcją dobrowolną i zweryfikowane wyłącznie offline; domyślnie OMSI jest kończony przy zakończeniu sesji, a `ShutdownTimeoutSeconds` nie ma efektu | `EXPERIMENTAL` | [CLI](cli.md) |
| Flagi diagnostyki `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` wypełniają `DiagnosticsSpec` bez dalszego efektu (ślad hosta w `.omsilaunch\diagnostics` jest zawsze zapisywany); `/trace-plugin` i `/trace-native` dodają do niego zdarzenia śladu wtyczki i warstwy natywnej | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` są harnessami walidacyjnymi | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Zakończenie sesji i sterowanie procesem

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Zatrzymanie sesji jest wymuszonym zakończeniem: `session.stop`, „End session” w obszarze powiadomień, Ctrl+C i `CloseAsync` prowadzą do `TerminateProcess`. Procedura zamykania OMSI nie jest wykonywana, OMSI przy wyjściu nie zapisuje ponownie `options.cfg` ani swoich logów, a każdy niezapisany stan OMSI zostaje utracony. Jest to celowe: zapobiega nadpisaniu przywróconych plików przez OMSI. | zgodnie z projektem | [cykl życia sesji](../concepts/session-lifecycle.md) |
| Kooperacyjne zamykanie przez WM_CLOSE z limitem czasu i awaryjnym wymuszonym zakończeniem nie jest zaimplementowane | `UNAVAILABLE` (decyzja produktowa, S-11; w rundzie domknięcia runtime OMSI zignorował `WM_CLOSE` wysłane do jego głównego okna) | [stan weryfikacji w runtime](../status/runtime-validation-status.md) |
| Przy zamknięciu konsoli lub wylogowaniu właściciel ma budżet 4 s na zatrzymanie i przywrócenie; wszystko, co pozostanie, jest odzyskiwane z dziennika przy następnym uruchomieniu | zamknięcie konsoli zweryfikowane w runtime; wylogowania nie sprawdzano | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transakcja, odzyskiwanie i dzierżawa

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Dzierżawa instalacji jest semaforem `Local\`: jeden właściciel na instalację **w obrębie sesji logowania**; nie jest egzekwowana między użytkownikami; nie jest zwalniana, dopóki inny proces trzyma uchwyt; dowolny proces tego samego użytkownika może zająć tę nazwę | zaakceptowane ryzyko (S-18) | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| Odzyskiwanie jest odrzucane (`OL_E_INSTALLATION_BUSY`), dopóki działa proces OMSI zapisany w dzienniku lub – w przypadku dziennika bez PID – jakikolwiek `Omsi.exe` z tego katalogu głównego | zgodnie z projektem | jak wyżej |
| Pierwotnie nieobecna ścieżka nakładki, której zawartość zmieniła się podczas sesji, blokuje przywracanie (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) do czasu jej sprawdzenia | zgodnie z projektem | jak wyżej |
| Dzienniki sprzed wprowadzenia odcisków własności może zamknąć tylko sesja z identycznymi zaplanowanymi bajtami (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | jak wyżej |
| Przywracane są tylko ścieżki należące do sesji. Własne zapisy OMSI podczas sesji (`options.cfg` `[last_map]`, gdy żadne ustawienie nie nakłada zmian na `options.cfg`, `Texture\standard.ipr`, pamięci podręczne, `laststn.osn`, profil kierowcy, logi) pozostają, tak jak po bezpośrednim uruchomieniu OMSI | zgodnie z projektem | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| Usunięcie nieaktualnego `closecheck` przed sesją jest trwałe (zapisywane, nieprzywracane), gdy `SuppressStaleClosecheckWarning` ma wartość true | zgodnie z projektem | jak wyżej |

<a id="runtime-control"></a>
## Sterowanie runtime

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| `weather.set` i `weather.metar.set` zapisują źródłowy rekord pogody OMSI (zamknięte statycznie); trwałość względem taktu pogody, rozstrzyganie typu chmur i pobieranie METAR czekają na weryfikację w runtime. Wartości pochodnych (wilgotność, współczynniki światła, rzeczywisty wiatr) nie można ustawić; gdy OMSI jest wstrzymany, aktualizują się dopiero po wznowieniu | `EXPERIMENTAL` | [możliwości](capabilities.md) |
| `SetActualDateTime` tylko kopiuje zegar komputera do zegara startowego OMSI (analiza statyczna); OmsiLaunch go nie wywołuje i sam ustawia zegar startowy | z założenia | `calendar.set-actual-date-time` |
| Zapisy zmiennych tekstowych, nazwane wyzwalacze i wyzwalacze dźwięku przechodzą przez własne metody OMSI (zamknięte statycznie, BI-004); ich wpływ na skrypty pojazdów czeka na weryfikację w runtime. Celami są tylko pojazdy drogowe | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Brak przenoszenia pojazdów, brak ponownego wiązania przestrzennego między kafelkami, brak bezpiecznego względem ODE uprawnienia do transformacji; pola pozycji są tylko do odczytu | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` wymagają PlayerVehicle; uruchomienie NEW_MAP bez interakcji go nie zapewnia (zapewnia go zapisana sytuacja) | zgodnie z projektem (BI-007) | RV-004 |
| Mutacje runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, tekstury D3D) nie są zapisywane w dzienniku ani przywracane | zgodnie z projektem | [sterowanie runtime](runtime-control.md) |
| Nieaktualność uchwytu jest ustalana na podstawie nigdy niepowtarzanego numeru instancji OMSI (`IDCode`) oraz, dla postaci, usunięcia i ponownego utworzenia; jest zweryfikowana offline. Naturalne usunięcie (RV-002: samochody AI opuszczające załadowaną sieć ścieżek) nadal czeka na zaobserwowanie w runtime, a zdarzenie `entity.removed` nie jest jeszcze emitowane | `PARTIAL` | [sterowanie runtime](runtime-control.md) |
| Wyniki są ograniczone skrzynką 64 KiB: długie listy są obcinane (`truncated=true`); dane pikseli są ograniczone do 48 KiB na `d3d.texture.update` | zgodnie z projektem | [możliwości](capabilities.md) |
| Kanał jednożądaniowy: jedno żądanie naraz na sesję; zajęty slot daje `OL_E_RUNTIME_CHANNEL_BUSY`; identyfikatorów żądań nie wolno używać ponownie | zgodnie z projektem | [sterowanie runtime](runtime-control.md) |
| Telemetria jest pierścieniem 64 zdarzeń: host, który zostanie w tyle o więcej niż 64 zdarzenia, traci najstarsze i dowiaduje się, ile (`telemetry.lost`) | `EXPERIMENTAL` | [stała wtyczka](../concepts/permanent-plugin.md) |
| Hooki urządzenia Direct3D modułu są hookami vtable: w instalacji, w której inny komponent zapisuje z powrotem oryginalne metody urządzenia (zaobserwowane w instalacji regresyjnej wkrótce po rozpoczęciu rozgrywki), przestają być wywoływane i nie można ich usunąć przy sprzątaniu (`OL_E_MODULE_CLEANUP_INCOMPLETE`; komponent pozostaje wczytany) | `PARTIAL` | [moduły runtime](runtime-modules.md#native-components-and-hooks) |
| Wczytywanie modułów runtime jest otwarte: OmsiLaunch nie sprawdza, kto stworzył moduł; gwarantuje tylko, że moduły nigdy nie działają wewnątrz procesu OmsiLaunch i że program sesji jest ograniczony do swojego zadania (brak drugiego procesu, kończy się wraz z sesją) | z założenia (E21) | [moduły runtime](runtime-modules.md#security-boundary) |
| Protokół modułów 2 (ustawienia, aktywacja domyślna, plik ustawień, programy sesji, konflikty wtyczek, powiadomienia, `get_value`) jest zweryfikowany wyłącznie offline | `EXPERIMENTAL` | [moduły runtime](runtime-modules.md) |
| Reset urządzenia D3D zaobserwowano w runtime (`resetting`, `restored`, unieważnienie generacji); odrębne przejście `lost` nie wystąpiło, ponieważ urządzenie OMSI przeszło bezpośrednio do `DEVICENOTRESET` | `PARTIAL` (RV-007) | [stan weryfikacji w runtime](../status/runtime-validation-status.md) |
| Listy runtime są stronicowane (`cursor`, `limit`, `next_cursor`): strona zawiera najwyżej maksymalną liczbę wierszy listy oraz tyle, ile mieści się w slocie runtime 64 KiB | `EXPERIMENTAL` | [możliwości](capabilities.md#pagination) |
| Niektóre zaawansowane pola grafu map/kafelków/ścieżek/obiektów nie są udostępniane; czytniki runtime to typowane migawki zależne od profilu, nigdy dowolny dostęp do pamięci | zgodnie z projektem | [możliwości](capabilities.md) |
| Odczyty pamięci w procesie działają na zasadzie „sprawdź, potem użyj” wobec działającego OMSI; równoczesna zmiana w OMSI między sprawdzeniem a odczytem może dać niespójną migawkę (`OL_E_RUNTIME_OPERATION_FAILED`) | zaakceptowane ryzyko (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Lokalna płaszczyzna sterowania i model zaufania

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Model zaufania tego samego użytkownika: potok nazwany (`CurrentUserOnly`), mapowania pamięci handoff/telemetrii/runtime oraz semafor dzierżawy są dostępne dla każdego procesu tego samego użytkownika Windows. Taki proces może odczytać stan, zatrzymać sesję lub wykonywać operacje runtime, gdy tylko odczyta `session_id`. | zaakceptowane ryzyko (S-06, S-30) | [lokalne sterowanie](local-control.md) |
| Punkt końcowy sterowania istnieje tylko wtedy, gdy właściciel jest w stanie `Running`; klient otrzymuje `OL_E_NO_ACTIVE_SESSION` (kod wyjścia 4) podczas uruchamiania i po zakończeniu sesji | zgodnie z projektem | [lokalne sterowanie](local-control.md) |
| Jeśli inny proces jest już właścicielem nazwy potoku, właściciel sesji działa dalej bez punktu końcowego (`ListenFault`), a drugie uruchomienie może błędnie zgłosić `OL_E_SESSION_ALREADY_ACTIVE` | zaakceptowane ryzyko | [lokalne sterowanie](local-control.md) |
| `.omsilaunch\` dziedziczy listę ACL katalogu głównego OMSI; nie jest stosowana żadna jawna kontrola dostępu | zaakceptowane ryzyko (S-31) | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostyka i dane wyjściowe

| Ograniczenie | Stabilność | Szczegóły |
| --- | --- | --- |
| Diagnostyka to wyłącznie pliki lokalne (`.omsilaunch\diagnostics`); nic nie jest wysyłane i nie ma zdalnego raportowania | zgodnie z projektem | [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) |
| Retencja obejmuje 50 najnowszych sesji; starsza diagnostyka z prefiksem sesji jest usuwana przy starcie nowej sesji | zgodnie z projektem | jak wyżej |
| Dane wyjściowe JSON i diagnostyka zawierają ścieżki instalacji (`RootPath`, katalogi zasobów, ścieżki `.itx`) | zgodnie z projektem (dane lokalne) | |
| Okno dialogowe błędu sesji w `OmsiLaunchW.exe` pokazuje jako komunikat ładunek błędu wtyczki (na przykład `{"name":"world.failed",...}`) zamiast zdania; wiersz `Code:` jest poprawny | kosmetyczne | [obszar powiadomień Windows](windows-tray.md) |
| Żądanie D3D odrzucone przez mostek natywny przed jakimkolwiek wywołaniem Direct3D poprawnie raportuje `native_status`, ale jego tekst `detail` podaje `HRESULT 0x00000000` | kosmetyczne | [możliwości](capabilities.md) |
| Okno stanu w obszarze powiadomień jest migawką zaplanowanej sesji wykonaną w chwili jego otwarcia; nie odświeża się i nie pokazuje bieżących wartości OMSI | zgodnie z projektem | [obszar powiadomień Windows](windows-tray.md) |

<a id="documentation"></a>
## Dokumentacja

Angielskie strony w `docs/` są dokumentacją normatywną tego wydania. `docs/localized/<locale>/` zawiera tłumaczenia tych samych stron `0.1.0-beta4` (zob. [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); tam, gdzie tłumaczenie różni się od tekstu angielskiego, rozstrzygające są tekst angielski i kod. Wymienione tam strony historyczne i starsze (legacy) są dostępne tylko po angielsku.

Powiązane: [możliwości](capabilities.md), [stan weryfikacji w runtime](../status/runtime-validation-status.md), [błędy](errors.md).
