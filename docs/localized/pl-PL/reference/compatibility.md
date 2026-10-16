# Zgodność

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/compatibility.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

OmsiLaunch współpracuje z zainstalowanym **OMSI 2**. Nie wymaga jednego konkretnego pliku `Omsi.exe`: każde natywne założenie, jakie przyjmuje wewnątrz OMSI (funkcja, miejsce łatania, klasa, slot VMT, zmienna globalna, przesunięcie pola), jest rozwiązywane i weryfikowane osobno, w procesie, zanim zostanie użyte, a możliwość jest dostępna tylko wtedy, gdy wszystkie jej założenia zostały zweryfikowane. Ta strona określa, co oznacza „zainstalowany OMSI 2”, jak identyfikowane są buildy, jak rozwiązywane są możliwości oraz jakie są wymagania dotyczące systemu operacyjnego i runtime. Źródła: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Wymaganie: zainstalowany OMSI 2

Plan jest odrzucany z `OL_E_UNSUPPORTED_BUILD` tylko wtedy, gdy instalacja nie jest instalacją OMSI 2:

| Sprawdzenie | Odrzucenie, gdy |
| --- | --- |
| `Omsi.exe` | brak w katalogu głównym instalacji |
| Obraz | nie jest to 32-bitowy obraz PE x86 |
| Wersja | jego zasób wersji nie zawiera wersji OMSI 2 (2.x) |
| Instalacja | w katalogu głównym nie ma katalogu `maps\` ani `Vehicles\` |

SHA-256, rozmiar i dokładna wersja pliku wykonywalnego **nie** są kryteriami zgodności. Są zapisywane jako komunikaty diagnostyczne (`omsi.build.detected`, `OmsiLaunch.exe profiles`) i służą jako dowody.

<a id="build-identity-and-capability-resolution"></a>
## Tożsamość buildu i rozwiązywanie możliwości

| Pojęcie | Znaczenie |
| --- | --- |
| Tożsamość kodu | SHA-256 głównej sekcji kodu pliku wykonywalnego. Znana tożsamość wybiera znany układ jako ścieżkę przyspieszoną. Warianty różniące się tylko nagłówkami (na przykład kopia Large Address Aware) mają wspólną tożsamość kodu. |
| Znany układ | `Omsi23004`: OMSI 2.3.004 (wersja pliku 2.2.032), tożsamość kodu `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Każda weryfikacja w runtime na [stronie stanu](../status/runtime-validation-status.md) została wykonana na tym układzie. |
| Cel natywny | Jedno założenie granicy natywnej, zadeklarowane w katalogu buildów wraz ze swoim strażnikiem (guard). |
| Możliwość | Grupa celów, na przykład `world.new-map` lub `time.read`. Dostępna tylko wtedy, gdy każdy cel jest rozwiązany **i** zweryfikowany. |

Jak cel jest rozwiązywany w wtyczce, zanim OmsiLaunch cokolwiek załata:

1. **Znana tożsamość:** najpierw próbowany jest adres referencyjny znanego układu i musi on nadal przejść strażnika celu.
2. **W przeciwnym razie lub gdy strażnik zawiedzie** próbowane są strategie niezależne od buildu:
   - nazwa klasy w RTTI Delphi (sprawdzane `vmtSelfPtr`);
   - opublikowane metody, pola i właściwości klasy (tabele RTTI);
   - slot VMT zawierający rozwiązaną metodę;
   - maskowana sygnatura kodu, która musi pasować dokładnie raz (operandy zależne od układu są symbolami wieloznacznymi), przechwytująca adres, zmienną globalną (bezpośrednio lub przez jej komórkę wskaźnika) albo przesunięcie pola z wnętrza metody klasy, do której pole należy.
3. **Nic nie pasuje jednoznacznie i nie przechodzi strażnika:** cel pozostaje nierozwiązany. **Nie ma awaryjnego użycia stałego adresu.**

Strażnicy: maskowane bajty wejścia dla funkcji i miejsc łatania, miejsce docelowe wywołania dla załatanego wywołania, `vmtSelfPtr` i nazwa klasy w RTTI dla klas, przechowywana metoda dla slotów VMT oraz zapisywalna sekcja danych dla zmiennych globalnych. Układy obiektów odczytywane przez czytniki runtime (rekordy pojazdu, postaci, pogody, rozkładu jazdy itd.) nie mają jeszcze niezależnej od buildu walidacji strukturalnej. Są weryfikowane tylko dla znanej tożsamości kodu, więc możliwości, które je odczytują, są niedostępne w buildzie o nieznanej tożsamości.

W znanym układzie każda możliwość jest rozwiązywana. W buildzie o nieznanej tożsamości dostępne mogą być możliwości, których cele rozwiązują się przez powyższe strategie (start bez interfejsu, NEW_MAP, zapisane sytuacje, wyłączenie tekstur internetowych, odczyt i ustawianie czasu, D3D), a każda jest weryfikowana osobno; odczyty zależne od układu są niedostępne.

<a id="where-compatibility-is-decided"></a>
## Gdzie zapada decyzja o zgodności

| Etap | Co się dzieje |
| --- | --- |
| Planowanie (`PlanSessionAsync`, `/plan`) | Sprawdzenie OMSI 2 (`omsi.omsi2`, w przeciwnym razie `OL_E_UNSUPPORTED_BUILD`). Obraz pliku jest rozwiązywany jako **przewidywanie**: wymagane możliwości natywne niosą `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` lub `PENDING_IN_PROCESS_RESOLUTION` z ostrzeżeniem `OL_W_CAPABILITY_PENDING`. Nieznana tożsamość kodu dodaje ostrzeżenie `OL_W_BUILD_UNRECOGNIZED`. Ostrzeżenia nie czynią planu niemożliwym do uruchomienia. |
| Start (`StartSessionAsync`) | Specyfikacja jest ponownie planowana; plan, który nie jest już możliwy do uruchomienia, jest odrzucany z `OL_E_PLAN_NOT_RUNNABLE`. |
| W procesie (start wtyczki) | Załadowany obraz jest rozwiązywany (rozstrzygająco; plik wykonywalny chroniony DRM może różnić się od swojego pliku). Raport jest wysyłany jako telemetria `native.capabilities`. Jeśli `boot.headless-start` i możliwość trybu świata (`world.new-map` lub `world.saved-situation`) nie są dostępne, sesja kończy się błędem `OL_E_CAPABILITY_UNRESOLVED` przed jakimkolwiek hookiem, a pliki są przywracane. Jeśli są dostępne, ale aktywny formularz Start nie jest w oczekiwanym stanie (na przykład inna wtyczka go załatała), sesja kończy się błędem `OL_E_BUILD_VALIDATION_FAILED`. |
| Operacje runtime | Operacja, której możliwość jest niedostępna, jest odrzucana z `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) bez dotykania pamięci OMSI. |

`OmsiLaunch.exe profiles --json` raportuje instalację (`is_omsi2`, `build_id`, `identity`, skróty pliku i kodu, `file_variant`), znane układy oraz możliwości przewidziane na podstawie pliku.

<a id="operating-system-and-architecture"></a>
## System operacyjny i architektura

`CurrentWindowsX64Platform.Detect` wyznacza `RuntimePlatformInfo`. Bieżąca platforma jest obsługiwana tylko wtedy, gdy spełnione są wszystkie poniższe warunki:

| Wymaganie | Sprawdzenie | Błąd przy niespełnieniu |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 lub nowszy | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-bitowy Windows i 64-bitowy proces hosta | `OSArchitecture == X64` i `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Instalacja z prawem zapisu | Katalog główny istnieje, nie jest tylko do odczytu i zawiera `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` zgłasza również `OmsiArchitecture` i `PluginArchitecture` jako `X86` (OMSI jest procesem 32-bitowym; zestaw plików wtyczki (closure) jest x86 i działa pod WOW64), `LegacyPlatform=false` oraz `Wow64Available`. Windows ARM64 nie jest obsługiwany, nawet tam, gdzie istnieje emulacja x64, ponieważ sam proces hosta musi być procesem x64.

<a id="net-requirements"></a>
## Wymagania .NET

| Komponent | Runtime | Uwagi |
| --- | --- | --- |
| Kontroler (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Natywny bootstrapper lokalizuje runtime przez `hostfxr` za pomocą dołączonego do pakietu `nethost.dll`. Brak runtime zgłasza shim (kody wyjścia 100-106; zobacz [CLI](cli.md) i [kody wyjścia](exit-codes.md)). |
| Zestaw plików wtyczki (`plugins\OmsiLaunch.Plugin.dll` przez `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hostowany przez DNNE 2.0.6 wewnątrz `Omsi.exe` | Wymaga zainstalowania na komputerze runtime .NET 6 Desktop/Core x86; sam runtime 64-bitowy nie wystarcza dla wtyczki. |
| Mostek natywny (`plugins\OmsiLaunch.Native.x86.dll`) | natywny x86 | Ładowany wyłącznie z `plugins\` (zobacz [stała wtyczka](../concepts/permanent-plugin.md)). Nie zawiera żadnego adresu OMSI: otrzymuje od wtyczki zweryfikowaną tabelę celów. |

<a id="legacy-platforms"></a>
## Starsze platformy

Windows 7, Windows 8.x, Windows XP oraz inne systemy NT 6 i starsze znajdują się poza obecnym zakresem obsługi. `RuntimePlatformInfo.LegacyPlatform` ma zawsze wartość `false` i nie istnieje żaden adapter dla starszych systemów; pole i punkt rozszerzenia `IPluginNativeServices` istnieją wyłącznie po to, aby w przyszłości można było dodać adapter dla starszych systemów bez zmiany publicznego API (zobacz `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nic w tym wydaniu nie działa na tych systemach.

<a id="steam-and-large-address-aware-notes"></a>
## Uwagi dotyczące Steam i Large Address Aware

- Kopia Large Address Aware, która różni się od oryginału tylko nagłówkami, ma tę samą tożsamość kodu co oryginał i używa tego samego znanego układu.
- Plik wykonywalny Steam jest na dysku chroniony DRM, więc jego plik może różnić się od załadowanego obrazu: plan może zgłaszać `OL_W_CAPABILITY_PENDING`, a rozstrzyga rozwiązywanie w procesie. Sesja z oryginalnym plikiem wykonywalnym Steam nie została zweryfikowana w runtime (zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md)).
- Steam sam uruchamia OMSI; sesję należy uruchomić przez `OmsiLaunch.exe`, aby istniało przekazanie (handoff). Po uruchomieniu ze Steam stała wtyczka pozostaje nieaktywna (brak przekazania, brak hooków).

<a id="related-pages"></a>
## Powiązane strony

- [Znane ograniczenia](known-limitations.md)
- [Stan weryfikacji w runtime](../status/runtime-validation-status.md)
- [Instalacja](../getting-started/installation.md)
