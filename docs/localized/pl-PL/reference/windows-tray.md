# Wskaźnik w obszarze powiadomień Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/windows-tray.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Każda samodzielna sesja właściciela (uruchomiona przez `OmsiLaunch.exe` lub `OmsiLaunchW.exe`) wyświetla ikonę w obszarze powiadomień, która informuje o sesji i pozwala użytkownikowi ją zakończyć. Ta strona opisuje wskaźnik w postaci zaimplementowanej przez `SessionTrayIndicator`, `StatusWindow` i `StopConfirmationWindow` w `tools\OmsiLaunch.Cli\WindowsHost.cs`, prezenter tylko do odczytu `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) oraz rejestr napisów `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), a także okna dialogowe błędu `OmsiLaunchW.exe` z `WindowsHost`. Ikona w obszarze powiadomień jest wyłącznie adapterem prezentacji: nie jest właścicielem ani OMSI, ani odzyskiwania, a jej akcja zatrzymania sygnalizuje tę samą kanoniczną ścieżkę zatrzymania właściciela co `session stop` (zob. [dokumentacja CLI](cli.md) i [lokalne sterowanie](local-control.md)).

<a id="when-the-icon-exists"></a>
## Kiedy ikona istnieje

| Etap | Zachowanie |
|---|---|
| Utworzenie | Natychmiast po powrocie z `StartSessionAsync`, zanim sesja osiągnie stan `Running`, chyba że ustawiono `SuppressTrayIcon`. Ikona istnieje więc w stanach `StartingProcess`, `WaitingForPlugin`, `StartingWorld` i `EnteringGameplay`. |
| Budżet czasu uruchamiania | Wątek interfejsu użytkownika (`STA`, w tle, o nazwie `OmsiLaunch tray`) musi opublikować ikonę w ciągu 2 s. W przeciwnym razie, a także przy dowolnym wyjątku podczas tworzenia, wskaźnik jest likwidowany, a sesja działa dalej **bez** ikony; w logu zapisywany jest wpis `startup-timeout` lub wyjątek. Powolny start nigdy nie pozostawia osieroconej widocznej ikony. |
| Usunięcie | W bloku `finally` właściciela, po zakończeniu sesji (pomyślnym lub z błędem), a przed `CloseAsync`. Likwidacja przekazuje polecenie zamknięcia do wątku interfejsu użytkownika (zamyka menu, okno potwierdzenia i okno stanu, a następnie kończy pętlę komunikatów), czeka na zakończenie wątku do 2 s (po przekroczeniu zapisywany jest wpis `dispose-timeout`), ukrywa i likwiduje `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (pole `LaunchSpec`, `Presentation.SuppressTrayIcon`, domyślnie `false`). Można je ustawić przez `/spec` i API; nie ma flagi CLI. Integratorzy, którzy wyświetlają własny element interfejsu sesji, ustawiają je na `true`; poza tym nic w sesji się nie zmienia. |
| Hosty | Ikonę wyświetlają zarówno `OmsiLaunch.exe` (konsola), jak i `OmsiLaunchW.exe` (podsystem Windows); sesje `OmsiLaunchW.exe` uruchomione z `/silent` nie mają żadnej innej widocznej powierzchni. |

<a id="icon-and-tooltip"></a>
## Ikona i etykietka narzędzia

- Ikona: ikona skojarzona z uruchomionym plikiem wykonywalnym (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, osadzona ikona OmsiLaunch), a w razie niepowodzenia `SystemIcons.Application`.
- Tekst etykietki narzędzia (tooltip): `Tray.Running` (`OmsiLaunch is running`) (`OmsiLaunch jest uruchomiony`). Tekst nie zmienia się podczas zatrzymywania (nie ma jeszcze napisu „zatrzymywanie”; ikona pozostaje bez zmian, dopóki właściciel jej nie usunie).
- Style wizualne są włączone (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interakcja

| Action | Wynik |
|---|---|
| Kliknięcie prawym przyciskiem | Ustawia okno wskaźnika jako okno pierwszoplanowe (wymagane dla menu ikon powiadomień; bez tego menu może ignorować kliknięcia i nigdy się nie zamknąć, gdy OMSI jest na pierwszym planie), a następnie otwiera menu kontekstowe w rzeczywistym położeniu kursora (`Cursor.Position`, a nie współrzędne zdarzenia, ponieważ `NotifyIcon` może zgłaszać `(0,0)` dla zdarzeń obsługiwanych przez powłokę). Menu jest ograniczane do obszaru roboczego ekranu, na którym znajduje się kursor. |
| Dwukrotne kliknięcie | Otwiera okno stanu (tak samo jak pozycja menu `Status`). |
| Kliknięcie lewym przyciskiem | Brak akcji. |
| Pozycja menu `Status` (`Tray.Status`) | Otwiera (lub aktywuje, jeśli jest już otwarte) okno stanu tylko do odczytu. |
| Separator | |
| Pozycja menu `End session` (`Tray.EndSession`, opis ułatwień dostępu `Tray.EndSessionDescription`) | Otwiera okno dialogowe potwierdzenia. |

<a id="status-window-read-only-snapshot"></a>
### Okno stanu (migawka tylko do odczytu)

Otwierane pozycją menu `Status` (`Stan`) lub dwukrotnym kliknięciem ikony (`SessionTrayIndicator.ShowStatus`). Jest to wyśrodkowane okno dialogowe o stałym, automatycznie dobieranym rozmiarze, bez wpisu na pasku zadań, z jednym przyciskiem `Close` (`Status.Close`; zamyka je także `Escape`). Wybranie `Status`, gdy okno jest już otwarte, aktywuje to okno bez jego ponownego budowania (zachowuje ono migawkę z pierwszego otwarcia). Błąd budowania okna jest zapisywany w `tray-host.log`; nie wpływa on na sesję.

**Jest to migawka, a nie widok na żywo.** `SessionStatusPresenter.Create(plan, status, ui)` wykonuje się raz, przy otwarciu okna: odczytuje rozwiązany `SessionPlan` sesji (specyfikację, która została faktycznie zaplanowana) i jeden `SessionStatus` (tylko jego `State`). Dopóki okno pozostaje otwarte, nic nie jest odświeżane, a okno nigdy nie odpytuje OMSI (żadnej operacji runtime, żadnych wartości telemetrii). Aby zobaczyć nowszy stan, należy je zamknąć i otworzyć ponownie.

Tytuł i nagłówek okna: ten sam tekst – `Status.SessionRunning` (`Session is running`) (`Sesja jest uruchomiona`), gdy `SessionStatus.State` ma wartość `Running`, w przeciwnym razie surowa nazwa `SessionState` (np. `WaitingForPlugin` przy otwarciu podczas uruchamiania, ponieważ ikona istnieje przed stanem `Running`). `Status.Title` (`OmsiLaunch session status`) jest zdefiniowany w tabeli napisów, ale nie jest używany w tym wydaniu.

Sekcje pojawiają się w poniższej kolejności, a sekcja bez pól jest pomijana. Każda wartość pochodzi z zaplanowanego `LaunchSpec`/`SessionPlan`, nigdy z OMSI.

| Sekcja (etykieta angielska) | Pole (etykieta angielska) | Wyświetlane, gdy | Wartość | Źródło (publiczny odpowiednik) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | zawsze | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (dowolny inny tryb; nigdy nieosiągany, ponieważ `LastMapState` jest niemożliwy do uruchomienia) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | plan rozpoznał identyfikator zawartości `map` | `DisplayName` mapy (nazwa katalogu mapy, np. `Grundorf`), w przeciwnym razie nazwa pliku identyfikatora bez rozszerzenia | Wpis `SessionPlan.ResolvedContent` z `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` zwraca ten sam `DisplayName` |
| `Session` | `Situation` | `SavedSituation` z identyfikatorem sytuacji | nazwa pliku bez rozszerzenia (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | zażądano punktu wejścia | identyfikator punktu wejścia, jeśli ustawiony (w tym wydaniu nigdy możliwy do uruchomienia), w przeciwnym razie prezentowany indeks jako liczba całkowita (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | użyto profilu sesji (`/predefined-profile`) | `name` profilu | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | jak wyżej, gdy ustawienie wstępne ma nazwę | `name` ustawienia wstępnego | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | zażądano jawnej/systemowej daty, godziny lub pogody | `DD/MM/YYYY` lub `System`; `HH:MM:SS` lub `System`; kod ICAO, nazwa ustawienia wstępnego lub `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | zażądano pola pojazdu gracza | żądany identyfikator/wartość | `Spec.PlayerVehicle` |
| `Configuration` | jedno pole na każde ustawienie semantyczne | ustawienie zostało ustawione (`/set`, `settings` profilu, `LaunchSpec.Environment.*`) i jest znane `ConfigurationCatalog` | żądana wartość; `%` dopisywany dla kluczy kończących się na `Percent`, ` m` dla kluczy kończących się na `DistanceMeters`. Etykietą jest klucz ustawienia, w którym każda część rozdzielona kropką zaczyna się wielką literą (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; te same wartości to `PlannedMutations` planu |
| `Presentation` | `Splash` | zawsze | `Managed` lub `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | zawsze | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

Sekcja `Environment` (`Środowisko`) pojawia się w działającej sesji Beta 4, gdy zażądano daty, godziny, roku lub pogody (te żądania są eksperymentalne, zob. [znane ograniczenia](known-limitations.md)). Sekcja `Vehicle` (`Pojazd`) nigdy nie może pojawić się w działającej sesji: zażądanie dowolnego pola pojazdu gracza sprawia, że plan jest niemożliwy do uruchomienia, więc taka sesja w ogóle się nie rozpoczyna. Istnieje ona z myślą o przyszłym buildzie i jest pokryta testem offline prezentera.

Dowody z runtime (interfejs pt-BR, runda runtime closure `T01`): sesja NEW_MAP na mapie Grundorf wyświetliła `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Te same dane są dostępne dla narzędzi bez ikony w obszarze powiadomień: `session status` przez [lokalną płaszczyznę sterowania](local-control.md) zwraca `SessionId`, `State`, `Diagnostics` i `RuntimeEvents` (na żywo), a wynik `/plan --json` właściciela (lub `PlanSessionAsync`) zawiera zaplanowaną specyfikację, rozpoznaną zawartość i zaplanowane zmiany, które okno podsumowuje.

<a id="end-session-with-confirmation"></a>
### Zakończenie sesji (z potwierdzeniem)

1. `StopConfirmationWindow`: tytuł `End session?` (`Zakończyć sesję?`), komunikat `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (`OMSI 2 zostanie zamknięty, a zarządzana sesja OmsiLaunch zostanie zakończona.`), przyciski `End session` (`Zakończ sesję`; domyślny, `DialogResult.OK`) i `Cancel` (`Anuluj`; `Escape`). Drugie żądanie, gdy okno dialogowe jest otwarte, aktywuje je zamiast otwierać kolejne.
2. Po `OK` wskaźnik wywołuje `requestCanonicalStop`, co kończy sygnał `controlStopped` właściciela; następnie właściciel wywołuje `StopAsync`: OMSI jest kończony za pomocą `TerminateProcess`, a każdy plik należący do sesji zostaje przywrócony. Wskaźnik nigdy sam nie kończy OMSI.
3. Jeśli żądanie zgłosi wyjątek, błąd jest zapisywany w logu i wyświetlany jest `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`) (`Nie można było zakończyć sesji. OMSI i zarządzana sesja pozostają aktywne.`).
4. Wskaźnik nie potwierdza powodzenia; ikona znika, gdy właściciel zakończy przywracanie i zlikwiduje wskaźnik (runda runtime closure `T01`: właściciel zakończył działanie 607 ms po potwierdzeniu `End session`).
5. `Cancel` (lub zamknięcie okna dialogowego) nic nie robi: sesja nadal działa (`T01`).
6. Zatrzymanie, które nadejdzie z innego źródła (`session stop`, Ctrl+C, `/observe-seconds`, zakończenie OMSI), gdy okno stanu lub okno dialogowe potwierdzenia jest otwarte, zamyka je w ramach likwidacji wskaźnika; właściciel nie czeka na użytkownika (`T02`: właściciel zakończył działanie 725 ms po zatrzymaniu przez potok przy obu otwartych oknach).

<a id="explorer-restart"></a>
## Ponowne uruchomienie Eksploratora

`TrayWindow` to ukryte okno natywne, które rejestruje komunikat okna `TaskbarCreated`. Gdy Eksplorator (powłoka) zostanie ponownie uruchomiony, rozgłasza ten komunikat, a wskaźnik ponownie dodaje ikonę (`Visible = false; Visible = true`). Runda runtime closure `T01`: po zakończeniu `explorer.exe` i jego ponownym uruchomieniu przez Windows ikona wróciła do `Shell_TrayWnd`, a menu i okno stanu nadal działały.

<a id="localization"></a>
## Lokalizacja

`WindowsUiStrings.Resolve` kieruje się **kulturą interfejsu Windows** (`CultureInfo.CurrentUICulture`), nigdy językiem zawartości OMSI ani językiem profilu sesji. Kolejność rozpoznawania: dokładna nazwa kultury, następnie dwuliterowy kod języka, następnie angielski. Każdy klucz, którego brakuje w tłumaczeniu, przyjmuje wartość angielską.

| Klucze kultury | Język |
|---|---|
| `en`, `en-US`, `en-GB` | angielski (domyślny i zastępczy) |
| `pt-BR` | portugalski brazylijski. `pt-PT` (oraz samo `pt`) celowo korzysta z wersji angielskiej. |
| `de`, `de-DE` | niemiecki |
| `fr`, `fr-FR` | francuski |
| `pl`, `pl-PL` | polski |

Zlokalizowane napisy obejmują etykietkę narzędzia, dwie pozycje menu, okno stanu (nagłówek, tytuły sekcji, etykiety pól, `Close`), wartości trybu i prezentacji oraz okno dialogowe potwierdzenia. Glosariusz jest utrzymywany w `docs\windows-ui-localization.md`; test offline `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) weryfikuje rozpoznawanie kultury i prezenter.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Okna dialogowe błędu `OmsiLaunchW.exe`

Gdy ustawiono `OMSILAUNCH_WINDOWS_HOST=1` (ustawiane przez `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` zastępuje wyjście błędów konsoli modalnym oknem komunikatu o tytule `OmsiLaunch` (ikona błędu): `<message>`, pusty wiersz, `Code: OL_E_...`, pusty wiersz, `See .omsilaunch\diagnostics for details.` Jest ono wyświetlane przy każdym `CliInput.WriteError` (błędy argumentów, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, sklasyfikowane wyjątki), gdy plan uruchomienia jest niemożliwy do uruchomienia (komunikat zastępczy `The session plan is not runnable.`; audyt dokumentacji BUG-06), oraz gdy sesja nie osiągnie stanu `Running` (`The OMSI session did not reach gameplay.` z ostatnim komunikatem diagnostycznym `OL_E_` albo `OL_E_SESSION_START_FAILED`, jeśli takiego nie ma). Pełny opis zachowania `OmsiLaunchW.exe` zawiera [dokumentacja OmsiLaunchW.exe](omsilaunchw.md). W `OmsiLaunch.exe` ta sama funkcja nic nie robi. Błąd uruchomienia hosta .NET (kody shima `100`..`106`) jest wyświetlany przez sam natywny shim; zob. [kody wyjścia](exit-codes.md).

<a id="log-location"></a>
## Lokalizacja logu

`<root>\.omsilaunch\diagnostics\tray-host.log`, jeden wiersz na wpis: znacznik czasu UTC w formacie ISO-8601, znak tabulacji, a następnie wpis. Wpisy: `created`, `removed`, `startup-timeout`, `startup-cancelled` (likwidacja wyprzedziła uruchamianie i pętla została pominięta), `dispose-timeout` oraz pełne teksty wyjątków dla błędów interfejsu użytkownika. Logowanie działa w miarę możliwości (best effort) i nigdy nie zgłasza wyjątków. Logi hosta sesji (`<sessionId>-host.log`) są zapisywane w tym samym katalogu przez właściciela; log wskaźnika nie ma prefiksu sesji i nie jest przycinany przez retencję 50 sesji.

<a id="lifecycle-guarantees"></a>
## Gwarancje cyklu życia

- Wskaźnik nigdy nie jest właścicielem sesji: nie może uruchomić OMSI, nie może przywracać plików i nie może ominąć ścieżki zatrzymania właściciela.
- Każda ścieżka zakończenia właściciela (normalne zakończenie, zakończenie OMSI, Ctrl+C, zamknięcie konsoli, zatrzymanie przez potok, wyjątek, `/observe-seconds`) likwiduje wskaźnik przed `CloseAsync`, więc żadna ikona nie przetrwa swojej sesji, z wyjątkiem sytuacji, gdy proces właściciela zostanie bezwarunkowo zabity (Windows usuwa osierocone ikony przy następnym najechaniu myszą).
- Tworzenie i likwidacja są serializowane za pomocą blokady: likwidacja, która wygra wyścig, sprawia, że wątek interfejsu użytkownika pomija swoją pętlę komunikatów i natychmiast sprząta.
- Cała praca Windows Forms odbywa się w dedykowanym wątku STA; inne wątki jedynie przekazują do niego zadania przez ukrytą kontrolkę marshallingu.

<a id="residual-caveats-from-the-code-comments"></a>
## Pozostałe zastrzeżenia (z komentarzy w kodzie)

- Nie istnieje tekst etykietki narzędzia „zatrzymywanie”; ikona pokazuje `OmsiLaunch is running` (`OmsiLaunch jest uruchomiony`), dopóki nie zostanie usunięta.
- `NotifyIcon` może zgłaszać współrzędne myszy `(0,0)` dla zdarzeń obsługiwanych przez powłokę; zamiast nich odczytywane jest położenie kursora.
- Komunikaty ikony w obszarze powiadomień nadal docierają, gdy okno dialogowe potwierdzenia jest modalne; drugie potwierdzenie nie jest nakładane.
- Jeśli wątek interfejsu użytkownika nie zakończy się w budżecie likwidacji wynoszącym 2 s, właściciel kontynuuje bez czekania (`dispose-timeout`).
- Okno stanu jest migawką zaplanowanych wartości wykonaną przy jego otwarciu; nie jest odświeżane i nigdy nie odczytuje danych z OMSI.

<a id="runtime-evidence"></a>
## Dowody z runtime

Zaobserwowane w rzeczywistych sesjach na autoryzowanej instalacji w rundzie runtime closure (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, interfejs Windows pt-BR; zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md)):

- Ikona jest rejestrowana w rzeczywistym obszarze powiadomień (`Shell_TrayWnd`) pod `OmsiLaunchW.exe` i usuwana po przywróceniu (`T01`..`T04`).
- „End session” z potwierdzeniem uruchamia kanoniczne zatrzymanie i dokładne przywrócenie; `Cancel` pozostawia sesję uruchomioną (`T01`, `T03`).
- Ikona jest ponownie tworzona po ponownym uruchomieniu Eksploratora (`TaskbarCreated`, `T01`).
- Okna stanu i potwierdzenia są zamykane przez właściciela, gdy nadejdzie zatrzymanie, a one są otwarte (`T02`).
- Okna dialogowe błędu `OmsiLaunchW.exe` dla błędu argumentu (`OL_E_INVALID_ARGUMENT`), braku aktywnej sesji (`OL_E_NO_ACTIVE_SESSION`) i sesji, która kończy się niepowodzeniem przed rozpoczęciem rozgrywki (`OL_E_WORLD_START_FAILED`) (`T04`). W ostatnim przypadku okno dialogowe wyświetla jako komunikat dane błędu przekazane przez wtyczkę.
- `/silent` odłącza się: program uruchamiający kończy działanie, a host Windows utrzymuje sesję (`T04`).
- Budżet 4 s dla `ProcessExit` przy zamknięciu konsoli (`L04`, właściciel konsolowy).

Nie uzyskano: okien dialogowych kodów wyjścia shima programu rozruchowego (`100`..`106`).
