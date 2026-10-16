# Stała wtyczka

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../concepts/permanent-plugin.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

OmsiLaunch steruje OMSI od wewnątrz procesu OMSI za pomocą wtyczki, która jest instalowana raz, w `plugins\OmsiLaunch.*`, jako część produktu. Sesja nigdy jej nie przygotowuje, nie kopiuje, nie obejmuje migawką, nie przywraca ani nie usuwa. Ta strona wyjaśnia, co zawiera zestaw plików wtyczki (closure), jak OMSI go wczytuje, jak host waliduje go przed każdym uruchomieniem, jak host i wtyczka się komunikują (handoff, telemetria, skrzynka runtime) oraz co robi wtyczka, gdy OMSI zostanie uruchomiony bez OmsiLaunch. Źródła: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, trzy magazyny pamięci współdzielonej), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` i `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## Zestaw plików (9 plików)

| Plik w `plugins\` | Rola |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Deskryptor wtyczki OMSI. Jego zawartość to `[dll]`, a po nim `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Natywny shim eksportów x86 wygenerowany przez DNNE 2.0.6. Eksportuje ABI wtyczek OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) i hostuje runtime .NET. Zawiera zasób wersji produktu. |
| `OmsiLaunch.Plugin.dll` | Zarządzana wtyczka (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Manifest zależności .NET wtyczki. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Konfiguracja runtime .NET (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Formaty komunikacji i publiczne rekordy współdzielone z hostem. |
| `OmsiLaunch.Builds.Omsi23004.dll` | Profil buildu: odcisk pliku wykonywalnego, zmienne globalne, układy obiektów, adresy metod. |
| `OmsiLaunch.Interop.dll` | Mechanizmy odczytu i zapisu pamięci wewnątrz procesu zbudowane na profilu. |
| `OmsiLaunch.Native.x86.dll` | Mostek natywny (C++): walidacja buildu, hook startu headless, zastosowanie czasu, MakeVehicle, PlaceRandomBus, blokowanie tekstur internetowych, dostęp do urządzenia D3D9. |

`RuntimeArtifactSet.Load` wyprowadza tę listę z własnego katalogu `plugins\` kontrolera: cztery nazwane pliki (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), każdy `OmsiLaunch.*.dll` w tym katalogu z wyjątkiem `OmsiLaunch.PluginNE.dll` oraz mostek natywny. `OmsiLaunch.Plugin.dll` musi się wśród nich znajdować. Dowolne biblioteki DLL nigdy nie są wciągane do OMSI. Narzędzie pakujące wydanie (`tools/New-ReleasePackage.ps1`) zapisuje dokładnie dziewięć powyższych plików.

<a id="how-omsi-loads-it"></a>
## Jak OMSI go wczytuje

1. OMSI wylicza `plugins\*.opl` i wczytuje bibliotekę DLL wskazaną w `OmsiLaunch.Plugin.opl`: `OmsiLaunch.PluginNE.dll`.
2. Shim DNNE uruchamia wewnątrz procesu OMSI runtime .NET 6 x86 opisany przez `OmsiLaunch.Plugin.runtimeconfig.json` i rozwiązuje zarządzane eksporty w `OmsiLaunch.Plugin.dll`.
3. OMSI wywołuje `PluginStart`. OMSI może wywołać tę funkcję więcej niż raz podczas uruchamiania; honorowane jest tylko pierwsze wywołanie (zabezpieczenie `Interlocked.Exchange`), ponieważ udany start jest właścicielem jednorazowego natywnego hooka. Późniejsze wywołania kończą się natychmiast.
4. `PluginStart` najpierw sprawdza `OMSILAUNCH_INTERNET_TEXTURES_MODE`: gdy ma wartość `Disabled`, stosowane jest natywne blokowanie mechanizmu pobierania (`internet-textures.suppressed` lub `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` odczytuje handoff (poniżej), waliduje build wewnątrz procesu, uzbraja hook startu headless, otwiera skrzynkę runtime i planuje start świata w wątku interfejsu użytkownika OMSI za pomocą wywołania zwrotnego `SetTimer`. Żadne polecenie runtime nigdy nie jest wykonywane w wątku roboczym IPC; wszystko działa w wywołaniu zwrotnym timera, w oryginalnym wątku interfejsu użytkownika OMSI.
6. `PluginFinalize` usuwa timer, zamyka runtime (`NativeD3DShutdown`) i przywraca poprawkę tekstur internetowych.

Eksporty `AccessVariable`, `AccessTrigger`, `AccessStringVariable` i `AccessSystemVariable` są puste; OmsiLaunch nie korzysta z kanału zmiennych skryptowych wtyczek OMSI.

<a id="integrity-validation-before-every-start"></a>
## Walidacja integralności przed każdym uruchomieniem

`RuntimeArtifactSet.ValidateInstalled` jest wykonywane podczas `StartSessionAsync` (po wczesnym odzyskiwaniu, przed przygotowaniem transakcji) oraz podczas `PlanSessionAsync` (tylko obecność, przez `LoadArtifacts`). Komunikat diagnostyczny planu `plugin.integrity.reference` informuje, której referencji użyto.

| Sytuacja | Referencja | Sprawdzenie każdego pliku | Błędy |
| --- | --- | --- | --- |
| `release-manifest.json` obecny obok `OmsiLaunch.exe` (zainstalowany pakiet) | `manifest` | Zainstalowany plik musi istnieć, a jego SHA-256 musi być równy wpisowi manifestu dla `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (manifest nie ma wpisu dla wymaganego pliku), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Brak manifestu (układ deweloperski) | `self` | Obecność plus spójność wewnętrzna: hash zainstalowanego pliku musi być równy hashowi pliku we własnym katalogu `plugins\` kontrolera | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifest nieczytelny lub nieprawidłowo sformułowany (brak tablicy `files`, wpis bez `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Niekompletny zestaw plików w katalogu kontrolera | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (plan niemożliwy do uruchomienia); CLI dodatkowo zgłasza `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, gdy brakuje `plugins\OmsiLaunch.Plugin.opl` lub `OmsiLaunch.Native.x86.dll` |

Wykorzystywane są tylko wpisy `plugins/` manifestu; manifest to dane, nigdy polityka. Manifest jest generowany przez narzędzie pakujące wydanie z SHA-256 każdego przygotowanego pliku. Gdy kontroler działa z katalogu głównego OMSI (układ wydania), katalog źródłowy i docelowy są tym samym katalogiem; bez manifestu sprawdzenie ogranicza się więc do obecności i spójności wewnętrznej, dlatego wydane pakiety zawsze zawierają `release-manifest.json`. Rozwiązanie w razie niezgodności: ponownie zainstalować pakiet, aby `plugins\` i `release-manifest.json` były zgodne.

Przed załataniem czegokolwiek wtyczka rozwiązuje i weryfikuje każdy natywny cel katalogu buildów względem załadowanego obrazu i przekazuje zweryfikowaną tabelę do `OmsiLaunch.Native.x86.dll`, która nie zawiera żadnego adresu OMSI (telemetria `native.capabilities`). Start, w którym możliwość `boot.headless-start` lub trybu świata jest niedostępna, kończy się błędem `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); aktywny formularz Start w nieoczekiwanym stanie kończy się błędem `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Zob. [zgodność](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Od hosta do wtyczki: zmienne środowiskowe

`CreateProcessW` uruchamia `Omsi.exe` ze środowiskiem procesu nadrzędnego, do którego dodawane są:

| Zmienna | Wartość | Odbiorca |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID sesji (format `D`) | `CurrentRuntimeControl` oznacza nim uchwyty D3D (format `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` otwiera to mapowanie tylko do odczytu |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` lub `Override` | `PluginStart` (tylko `Disabled` ma skutek wewnątrz procesu) |

Trzy mapowania są tworzone przez hosta przed uruchomieniem procesu (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) i zwalniane po zakończeniu zadania cyklu życia sesji. Są to nazwane obiekty jądra z domyślną listą DACL użytkownika uruchamiającego; każdy proces tego samego użytkownika może je otworzyć (zaakceptowany model zaufania w obrębie tego samego użytkownika, zob. [znane ograniczenia](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## Handoff uruchamiania

Rekord mapowany w pamięci, tylko do odczytu, o stałym układzie (`StartupHandoffWire`, magic `OLSH`, wersja 4; wersja 3 jest nadal akceptowana przez czytnik). 64-bajtowy nagłówek zawiera magic, wersję, rozmiar nagłówka, rozmiar całkowity, GUID sesji, rozmiar ładunku i SHA-256 ładunku. Ładunek zawiera `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 poprzedzony długością), `PresentedEntrypointIndex`, `WorldMode`, flagi (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` i `TimeMode`. Wtyczka ponownie oblicza hash ładunku i odrzuca każdą niezgodność (`plugin.handoff.invalid`, błąd hosta `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Mapowanie większe niż 1 MiB lub o niespójnym rozmiarze jest odrzucane w ten sam sposób.

Wtyczka akceptuje handoff tylko wtedy, gdy `WorldMode` ma wartość `NewMap` lub `SavedSituation`, `HeadlessStart` jest ustawiona, `PlayerVehicleEnabled` jest wyczyszczona, oba tryby daty i czasu mają wartość `Unset`, a zapisana sytuacja wskazuje swój plik `.osn`. Wszystko inne to `plugin.request.unsupported` (błąd hosta `OL_E_CAPABILITY_UNAVAILABLE`). Host zawsze ustawia `HeadlessStart`.

<a id="telemetry-slot"></a>
## Slot telemetrii

`OmsiLaunch.Telemetry.<session>` to pierścień zdarzeń (Beta 4, `TelemetryRingWire`): 64-bajtowy nagłówek (sygnatura `OLTR`, wersja 1, liczba slotów, rozmiar slotu, zatwierdzony numer sekwencyjny) oraz 64 numerowane sloty po 2048 bajtów, z których każdy zawiera jedno zdarzenie JSON w UTF-8 `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` to monotoniczny zegar sesji wtyczki). Jedynym zapisującym jest wtyczka: unieważnia slot, zapisuje go, zatwierdza numer sekwencyjny slotu, a następnie publikuje numer sekwencyjny pierścienia. Host co 100 ms odczytuje po kolei każde zdarzenie zatwierdzone od ostatniego odczytu; slot nadpisany w trakcie kopiowania jest liczony jako utracony i nigdy nie jest dostarczany w stanie rozerwanym. Host, który zostanie w tyle o więcej niż 64 zdarzenia, otrzymuje zdarzenie runtime `telemetry.lost` z dokładną liczbą. Zdarzenie większe niż slot jest zastępowane przez `telemetry.oversized`, które je wskazuje. Zdarzenia są dopisywane do `SessionStatus.RuntimeEvents` (ograniczone do ostatnich 256) i sterują semantycznym cyklem życia (`plugin.started`, `world.starting`, `gameplay.entered`, zdarzenia modułów i błędów).

<a id="runtime-command-mailbox"></a>
## Skrzynka poleceń runtime

`OmsiLaunch.Runtime.<session>` to jednozadaniowa skrzynka (mailbox) o rozmiarze 64 KiB: `state` (int32 pod przesunięciem 0: 0 bezczynna, 1 żądanie, 2 odpowiedź), `length` (int32 pod przesunięciem 4), koperta pod przesunięciem 8. Koperty to rekordy `RuntimeCommandWire` (magic `OLRC`, wersja 1, 72-bajtowy nagłówek z rodzajem, długością całkowitą, GUID sesji, identyfikatorem żądania, długością ładunku i SHA-256 ładunku JSON UTF-8). Wtyczka odpytuje skrzynkę z timera w wątku interfejsu użytkownika (co 50 ms po wczytaniu świata), wykonuje polecenie w tym wątku i publikuje odpowiedź tylko wtedy, gdy slot nadal zawiera ten sam identyfikator żądania; żądanie porzucone przez hosta po przekroczeniu limitu czasu nigdy nie otrzymuje odpowiedzi. Zbyt duża odpowiedź jest zastępowana typowanym błędem `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Szczegóły i limity czasu opisano w [sterowaniu runtime](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Polityka wyszukiwania DLL

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` i zestawy CLI deklarują `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Importy natywne (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) są rozwiązywane wyłącznie z katalogu samego zestawu (`plugins\`) lub z katalogu systemowego Windows; katalog główny OMSI i `PATH` nigdy nie są przeszukiwane. `OmsiLaunch.Native.x86.dll` jest więc wczytywany z `plugins\` i z żadnego innego miejsca.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Gdy OMSI jest uruchamiany bez OmsiLaunch

Ponieważ zestaw plików jest stały, OMSI wczytuje `OmsiLaunch.PluginNE.dll` przy każdym uruchomieniu, także przy uruchomieniach ze Steam lub z pulpitu. W takim przypadku:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` nie istnieje, więc żadna poprawka mechanizmu pobierania nie jest stosowana.
- `OMSILAUNCH_HANDOFF_NAME` nie istnieje, więc `PluginRuntime.Start` emituje `plugin.handoff.invalid` i zwraca `false`. `CurrentTelemetrySink.Emit` kończy się natychmiast, gdy `OMSILAUNCH_TELEMETRY_NAME` nie jest ustawiona, więc nic nigdzie nie jest zapisywane.
- Brak walidacji buildu, brak natywnego hooka, brak skrzynki, brak timera. Wtyczka pozostaje wczytana, ale nieaktywna; OMSI zachowuje się tak, jakby wtyczki nie było.
- `PluginFinalize` przy zakończeniu OMSI wywołuje natywną procedurę przywracania i `NativeD3DShutdown`; obie są operacjami pustymi, gdy nic nie zostało zainstalowane.

Plik `.opl` nie musi więc być usuwany, aby normalnie uruchomić OMSI.

<a id="non-interference-with-third-party-plugins"></a>
## Brak ingerencji we wtyczki firm trzecich

Sesje nigdy nie wyliczają, nie obliczają hashy, nie kopiują, nie usuwają ani nie przywracają innych plików w `plugins\`. Wtyczka nie korzysta z kanału `AccessVariable` OMSI i nie modyfikuje stanu innych wtyczek. Jedyne poprawki wewnątrz procesu to profilowany hook startu headless (jednorazowe przekierowanie VMT uzbrajane dla sesji), opcjonalne blokowanie tekstur internetowych (przywracane w `PluginFinalize`) oraz przechwytywanie `Reset` urządzenia D3D9 używane do śledzenia cyklu życia tekstur.

<a id="difference-from-omsihook"></a>
## Różnica względem OmsiHook

OmsiLaunch **nie ma zależności runtime** od OmsiHook ani od żadnego pliku binarnego OmsiHook: jedyne pakiety, do których się odwołuje, to `DNNE` 2.0.6 i `YamlDotNet` 15.1.2, a w całym produkcie nie ma `using OmsiHook` ani wywołań P/Invoke do bibliotek DLL OmsiHook. Tym, co OmsiLaunch rzeczywiście dzieli z OmsiHook, jest wiedza pochodna: układy obiektów i kilka opakowań odczytu zostało uzgodnionych z przypiętą kopią OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) względem dokładnie tego pliku wykonywalnego `Omsi23004_692EBFBF`. Uznanie autorstwa i warunki licencji znajdują się w `THIRD-PARTY-NOTICES.md`, a macierz ponownego użycia dla poszczególnych plików w `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook wstrzykuje osobny proces i udostępnia surowe wskaźniki; OmsiLaunch działa wewnątrz procesu, udostępnia wyłącznie nieprzezroczyste uchwyty o zasięgu sesji i usuwa wszelkie adresy natywne z publicznych wyników (zob. [możliwości](../reference/capabilities.md)).
