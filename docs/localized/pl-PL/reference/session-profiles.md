# Profile sesji

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/session-profiles.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Profil sesji to deklaratywny pakiet YAML, który autor zawartości dostarcza razem z mapą lub dodatkiem, aby użytkownicy końcowi mogli jednym poleceniem uruchomić powtarzalną sesję OmsiLaunch (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Ta strona jest normatywną dokumentacją referencyjną formatu `omsilaunch.session-profile/v1` w postaci zaimplementowanej przez `SessionProfileCompiler` w `src/OmsiLaunch.Core/SessionProfiles.cs`, reguł pierwszeństwa stosowanych przez CLI (`CliInput.BuildSpecAsync` i `RejectProfileConflicts` w `tools/OmsiLaunch.Cli/Program.cs`) oraz katalogu ustawień, które profil może zapisywać (`ConfigurationCatalog`). Wszystko, co potrafi profil, można też zrobić za pomocą flag CLI i rekordu [LaunchSpec](launchspec.md); profil jedynie pakuje te wybory.

Stabilność: `STABLE_BETA` dla parsowania, walidacji, wykrywania konfliktów oraz bloków `settings` / `presentation` / `internet-textures` / `behavior` (test offline `session-profiles.strict-compiler`; ścieżka nakładki (overlay) i przywracania jest zweryfikowana w runtime przez RV-005 i RV-006, zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md)). Klucze `new.date`, `new.time`, `new.year` i `new.weather` są w tym buildzie `UNAVAILABLE` (zob. [Blok `new`](#new)).

<a id="package-location-and-naming"></a>
## Lokalizacja i nazewnictwo pakietu

| Element | Reguła |
| --- | --- |
| Katalog pakietu | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Plik profilu | `<package>\profile.yaml` (dokładna nazwa, jeden plik) |
| Zasoby | Dowolne pliki lub katalogi wewnątrz katalogu pakietu, wskazywane ścieżką względną z `presentation.splash.assets` i `internet-textures.profile` |
| `id` | Musi być zwykłą nazwą katalogu: nie może być pusty ani składać się z samych białych znaków, nie może zawierać `\`, `/` ani `:` i nie może zawierać sekwencji `..`. Naruszenia dają `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Wartość `id` zadeklarowana w `profile.yaml` musi być bajt po bajcie równa nazwie katalogu (z rozróżnianiem wielkości liter); w przeciwnym razie `OL_E_SESSION_PROFILE_INVALID`. |
| Wybór | `/predefined-profile:<id>` razem z `/predefined-profile-index:<n>`. Indeks jest obowiązkowy: `/predefined-profile` bez `/predefined-profile-index` kończy się błędem `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Brak pakietu | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Układ wydania | Pakiet wydania zawiera przykład w `.omsilaunch\examples\session-profiles\rmg-leste\` (zob. [pakowanie](packaging.md)). Przykłady nie są profilami: aby pakiet można było wybrać, należy go skopiować do `.omsilaunch\session-profiles\<id>\`. |

Profil instaluje i usuwa użytkownik lub autor zawartości. OmsiLaunch nigdy nie zapisuje niczego w pakiecie, nigdy go nie kopiuje i nigdy go nie usuwa. Katalog pakietu nie jest częścią żadnej transakcji.

<a id="parsing-rules"></a>
## Reguły parsowania

| Reguła | Zachowanie | Błąd |
| --- | --- | --- |
| Limit rozmiaru | `profile.yaml` nie może przekraczać 256 KiB (262,144 bajtów) | `OL_E_SESSION_PROFILE_INVALID` |
| Kształt dokumentu | Dokładnie jeden dokument YAML, którego węzeł główny jest mapowaniem | `OL_E_SESSION_PROFILE_INVALID` |
| Schemat | `schema` musi mieć dokładnie wartość `omsilaunch.session-profile/v1` (z rozróżnianiem wielkości liter) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Kotwice i aliasy | Każdy węzeł z kotwicą YAML (`&name`) w dowolnym miejscu dokumentu jest odrzucany przed walidacją; aliasy (`*name`) nie mogą więc wystąpić | `OL_E_SESSION_PROFILE_INVALID` („YAML anchors are not supported.”) |
| Nieznane klucze | Każde mapowanie jest zamknięte: klucz, którego nie wymieniono dla danego kontekstu w poniższych tabelach, jest odrzucany („Unknown property in `<context>`: `<key>`”). Klucze są dopasowywane z rozróżnianiem wielkości liter (`Schema:` jest nieznanym kluczem). Jedynym otwartym mapowaniem jest `settings`, którego klucze są zamiast tego walidowane względem katalogu ustawień. | `OL_E_SESSION_PROFILE_INVALID` |
| Skalary | Każda wartość liścia musi być skalarem; sekwencje i mapowania w miejscu oczekiwanego skalara są odrzucane („`<field>` must be a scalar.”) | `OL_E_SESSION_PROFILE_INVALID` |
| Liczby | Liczby całkowite są parsowane z kulturą niezmienną (`1`, `30`); liczby dziesiętne w `settings` używają `.` jako separatora | `OL_E_SESSION_PROFILE_INVALID` |
| Daty i godziny | `new.date.value` jest parsowane przez `DateOnly.Parse`, a `new.time.value` przez `TimeOnly.Parse`, w obu przypadkach z kulturą niezmienną; należy używać form ISO `yyyy-MM-dd` i `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| Błędy składni YAML | Zgłaszane z komunikatem parsera | `OL_E_SESSION_PROFILE_INVALID` („Invalid YAML: ...”) |
| Zawartość wykonywalna | YAML jest parsowany za pomocą `YamlDotNet` wyłącznie do drzewa reprezentacji; tagi, typy niestandardowe ani wykonywanie kodu nie są obsługiwane |

Ukośniki odwrotne w zwykłych (niecytowanych) skalarach są znakami dosłownymi. Ścieżki Windows należy zapisywać z pojedynczym ukośnikiem odwrotnym (`maps\Grundorf\global.cfg`). Podwojony ukośnik odwrotny w zwykłym skalarze pozostaje w wartości podwojony; zob. [Przykład dołączony do pakietu](#the-packaged-example).

<a id="key-reference"></a>
## Dokumentacja kluczy

Konteksty są nazwane dokładnie tak, jak nazywa je kompilator. Każdy wymieniony tu klucz jest akceptowany; żaden inny nie jest.

<a id="profile-root-mapping"></a>
### `profile` (mapowanie główne)

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `schema` | string | tak | Literał `omsilaunch.session-profile/v1`. |
| `id` | string | tak | Identyfikator pakietu; musi być równy nazwie katalogu. |
| `name` | string | tak | Nazwa wyświetlana; zgłaszana w `SessionProfileMetadata.Name`. |
| `author` | string | tak | Autor; zgłaszany w `SessionProfileMetadata.Author`. |
| `version` | string | tak | Ciąg wersji pakietu (dowolna postać, należy ująć go w cudzysłów: `"1.0"`); zgłaszany w `SessionProfileMetadata.Version`. |
| `compatibility` | mapowanie | nie | Zob. `compatibility`. |
| `new` | mapowanie | nie | Wartości domyślne NEW_MAP. Zob. `new`. |
| `presets` | sekwencja mapowań | tak | Od 1 do 99 wpisów ustawień wstępnych. Zero, więcej niż 99 lub wartość niebędąca sekwencją daje `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `maps` | sekwencja ciągów znaków | nie | Identyfikatory map (`maps\<Map>\global.cfg`), dla których profil jest ważny. `/` jest normalizowany do `\`; porównanie nie rozróżnia wielkości liter. Brak listy lub pusta lista oznacza „dowolna mapa”. Niepusta lista jest egzekwowana dla `WorldMode.NewMap` (względem efektywnego `new.map` lub `/map`) oraz dla `WorldMode.SavedSituation` (względem mapy wskazanej przez wybrany plik `.osn`, rozwiązanej przez katalog zawartości). Dla `WorldMode.LastMapState` nie da się ustalić mapy, więc niepusta lista zawsze kończy się błędem. Błąd: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

Blok jest odczytywany i walidowany zawsze, gdy jest obecny, ale do specyfikacji jest stosowany tylko wtedy, gdy wybranym trybem świata jest NEW_MAP (`/new`, domyślny tryb CLI). Przy `/saved:<file.osn>` blok jest ignorowany.

| Klucz | Typ | Wymagany | Stosowany | Opis |
| --- | --- | --- | --- | --- |
| `map` | string | nie | tak | Identyfikator mapy w znormalizowanej postaci `maps\<Map>\global.cfg` (planowanie wymaga dokładnie tego kształtu: zaczyna się od `maps\`, kończy na `\global.cfg`, bez `..`). Ustawia `WorldSpec.MapIdentity`. |
| `entrypoint-index` | liczba całkowita | nie | tak | Indeks prezentowanego punktu wejścia (pozycja liczona od 0 na liście punktów wejścia OMSI). Ustawia `PresentedEntrypointIndex` i czyści ewentualny identyfikator punktu wejścia. |
| `entrypoint` | string | nie | tak | Surowy identyfikator punktu wejścia. Ustawia `EntrypointIdentity` i czyści prezentowany indeks. Jeśli obecne są zarówno `entrypoint-index`, jak i `entrypoint`, wygrywa `entrypoint`, ponieważ jest stosowany jako ostatni. Wartością jest dokładna etykieta (z rozróżnieniem wielkości liter) lub identyfikator z wykrywania; OMSI prezentuje tylko pierwszy rekord każdej etykiety (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapowanie | nie | nie (`UNAVAILABLE`) | Zob. `new.date`. |
| `time` | mapowanie | nie | nie (`UNAVAILABLE`) | Zob. `new.time`. |
| `year` | liczba całkowita | nie | nie (`UNAVAILABLE`) | Jawny rok. |
| `weather` | mapowanie | nie | nie (`UNAVAILABLE`) | Zob. `new.weather`. |

`date`, `time`, `year` i `weather` są kompilowane do `DateSpec`, `TimeSpec`, `YearSpec` i `WeatherSpec` z `DateTimeMode.Explicit` / wybranym `WeatherMode`. Planista sesji (`src/OmsiLaunch.Core/SessionPlanner.cs`) wymaga `world.explicit-date`, `world.explicit-time`, `world.explicit-year` i `world.weather`; wszystkie cztery mają status `EXPERIMENTAL` (zamknięte statycznie, weryfikacja w runtime w toku). Data/godzina startu dotyczy tylko NEW_MAP; pogoda dotyczy każdego trybu świata.

#### `new.date`

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `mode` | string | tak | Musi mieć wartość `explicit` (bez rozróżniania wielkości liter). Każda inna wartość daje `OL_E_SESSION_PROFILE_INVALID` („date must use explicit mode.”). |
| `value` | string | tak | `yyyy-MM-dd`. |

#### `new.time`

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `mode` | string | tak | Musi mieć wartość `explicit`. |
| `value` | string | tak | `HH:mm` lub `HH:mm:ss`. |

#### `new.weather`

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `mode` | string | tak | `preset`, `icao` lub `real` (bez rozróżniania wielkości liter). Cokolwiek innego: `OL_E_SESSION_PROFILE_INVALID` („Unsupported weather mode”). |
| `preset` | string | gdy `mode: preset` | Nazwa ustawienia wstępnego pogody. |
| `icao` | string | gdy `mode: icao` | Kod stacji ICAO. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (każdy wpis w `presets`)

| Klucz | Typ | Wymagany | Wartość domyślna | Opis |
| --- | --- | --- | --- | --- |
| `index` | liczba całkowita | tak | | Od 1 do 99, unikatowy w obrębie profilu; indeksy nie muszą być kolejne. Wybierany za pomocą `/predefined-profile-index`. Duplikat lub wartość spoza zakresu: `OL_E_SESSION_PROFILE_INVALID`; indeks, który nie występuje nigdzie w profilu: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | string | tak | | Identyfikator ustawienia wstępnego; zgłaszany jako `SessionProfileMetadata.PresetId`. |
| `name` | string | tak | | Nazwa wyświetlana ustawienia wstępnego; zgłaszana jako `SessionProfileMetadata.PresetName`. |
| `settings` | mapowanie | nie | brak | Semantyczne ustawienia `options.cfg`, zob. [Ustawienia](#settings). Klucze są dopasowywane do katalogu bez rozróżniania wielkości liter. |
| `presentation` | mapowanie | nie | dziedziczone | Prezentacja ekranu startowego, zob. `presentation`. Gdy brak, ustawienie wstępne dziedziczy wartość bazową (wartość z `/spec` lub domyślną wartość CLI, `Managed`). |
| `internet-textures` | mapowanie | nie | dziedziczone | Zob. `internet-textures`. |
| `behavior` | mapowanie | nie | dziedziczone | Limity czasu, zob. `behavior`. |
| `hofs` | sekwencja mapowań | nie | dziedziczone | Sesyjne operacje HOF, zob. `hofs`. Gdy są obecne, zastępują bazowe `HofManagement`. |
| `modules` | mapowanie | nie | dziedziczone | Moduły runtime, zob. `modules`. Łączone z tym, o co prosi plik `/spec`. |

Stosowane jest tylko wybrane ustawienie wstępne. Każde ustawienie wstępne jest jednak parsowane i walidowane, więc błąd w ustawieniu wstępnym 3 powoduje niepowodzenie żądania ustawienia wstępnego 1.

### `presentation`

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `splash` | mapowanie | tak | Wymagany, gdy obecny jest `presentation` („Presentation requires splash.”). Zob. `presentation.splash`. |

#### `presentation.splash`

| Klucz | Typ | Wymagany | Wartość domyślna | Opis |
| --- | --- | --- | --- | --- |
| `mode` | string | tak | | `managed` instaluje na czas sesji bitmapy ekranu startowego OmsiLaunch (`SplashMode.Managed`). `unset` lub `native` zachowuje własne pliki ekranu startowego OMSI (`SplashMode.Unset`; `Native` jest aliasem). Bez rozróżniania wielkości liter. Cokolwiek innego: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | string | nie | `ENG` | Wersja językowa drugiego docelowego ekranu startowego: `PTB`, `ENG`, `DEU`, `FRA` (aliasy `PT-BR`, `EN`, `DE`, `FR`; każda nieznana wartość jest przy budowaniu sesji rozwiązywana do `ENG`). Przy `mode: managed` sesja nakłada `GUI\NewSplashscreen_ENG.bmp` i `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | string | nie | zasoby z pakietu | Katalog **względny wobec pakietu**, zawierający `ENG.bmp` oraz, dla języka `language` innego niż angielski, `<language>.bmp`; każdy plik musi być 24-bitowym BMP o rozmiarze 640x480. Katalog musi istnieć w chwili wczytania profilu (`OL_E_SESSION_PROFILE_ASSET_MISSING`); pliki są walidowane przy uruchomieniu sesji (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Obowiązują reguły ograniczenia ścieżek. Gdy pominięty, używany jest katalog instalacji `.omsilaunch\assets\splash` (lub domyślne zasoby z pakietu). |

Profil nie może ustawić `SessionPresentationSpec.SuppressTrayIcon`; pozostaje ono `false`, chyba że ustawi je `/spec`.

### `internet-textures`

| Klucz | Typ | Wymagany | Opis |
| --- | --- | --- | --- |
| `mode` | string | tak | `native` (`InternetTexturesMode.Native`, OMSI działa normalnie), `disabled` (`Disabled`, objęty profilem mechanizm pobierania działający w procesie jest blokowany na czas sesji), `override` (`Override`, profil `.itx` o zasięgu sesji jest instalowany jako `Texture\standard.itx`). Bez rozróżniania wielkości liter; cokolwiek innego: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | string | wymagany dla `override` | Ścieżka pliku `.itx` **względna wobec pakietu**. Brak klucza przy `override`: `OL_E_SESSION_PROFILE_INVALID`; brak pliku: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Obowiązują reguły ograniczenia ścieżek. Plik musi składać się z par wierszy `URL` / `target` z adresami URL `http://` lub `https://` (w przeciwnym razie `OL_E_ITX_PROFILE_INVALID`), a każdy cel musi rozwiązywać się do miejsca poniżej katalogu `Texture\` instalacji bez przechodzenia przez punkt ponownej analizy (reparse point) (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Wymienione cele i `Texture\standard.ipr` stają się usunięciami w ramach sesji (zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Klucz | Typ | Wymagany | Wartość domyślna | Opis |
| --- | --- | --- | --- | --- |
| `startup-timeout` | liczba całkowita (sekundy) | nie | 180 | Czas dozwolony od uruchomienia procesu do stanu `Running`. Przy wczytywaniu profilu musi być dodatni; sesja dodatkowo wymaga przy uruchomieniu wartości od 1 do 600 (w przeciwnym razie `OL_E_START_SESSION`). Odpowiada `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | liczba całkowita (sekundy) | nie | 30 | Odpowiada `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: nadzorca kończy OMSI bezpośrednio i nigdy nie odczytuje tej wartości. |

Gdy obecny jest blok `behavior`, ustawiane są oba limity czasu (podana wartość lub domyślna) i całkowicie zastępują bazowy `LaunchBehaviorSpec`, w tym `RestoreConfiguration` i `SuppressStaleClosecheckWarning`, które wracają do wartości domyślnych (`true`, `true`).

### `hofs`

Każdy wpis to jedna operacja [zarządzania HOF](hof-management.md) i wskazuje dokładnie jeden klucz źródła (`package`, `collection`, `directory`, `vehicle` lub `file`); zero lub kilka: `OL_E_SESSION_PROFILE_INVALID`. Istnienie źródeł i celów jest sprawdzane przy planowaniu sesji (`OL_E_HOF_*`), z wyjątkiem `package`.

| Klucz | Typ | Wymagany | Wartość domyślna | Opis |
| --- | --- | --- | --- | --- |
| `package` | string | jeden klucz źródła | | Katalog **względny wobec pakietu** z plikami `.hof`, które dostarcza profil. Musi istnieć przy wczytywaniu profilu (`OL_E_SESSION_PROFILE_ASSET_MISSING`); obowiązują reguły ograniczenia ścieżek. Staje się źródłem `Directory`. |
| `collection` | string | jeden klucz źródła | | Nazwa kolekcji w `.omsilaunch\hof\`. |
| `directory` | string | jeden klucz źródła | | Katalog względny wobec instalacji, np. obok zawartości mapy. |
| `vehicle` | string | jeden klucz źródła | | Identyfikator lub folder pojazdu, którego HOF-y są kopiowane. |
| `file` | string | jeden klucz źródła | | Plik `.hof` względny wobec instalacji. |
| `targets` | sekwencja ciągów znaków | tak | | Pojazdy docelowe (identyfikatory lub foldery poniżej `Vehicles\`). Brak lub pusta: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | sekwencja ciągów znaków | nie | wszystkie | Nazwy plików do pobrania ze źródła. |
| `mode` | string | nie | `add` | `add` lub `mirror` (bez rozróżniania wielkości liter); cokolwiek innego: `OL_E_SESSION_PROFILE_INVALID`. |

Ustawienie wstępne z `hofs` połączone z flagą `/hof-source`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Jeden wpis na identyfikator [modułu runtime](runtime-modules.md) (`EXPERIMENTAL`). Każdy wpis to mapowanie z następującymi kluczami (kontekst `module`):

| Klucz | Typ | Wymagany | Wartość domyślna | Opis |
| --- | --- | --- | --- | --- |
| `enabled` | `true` lub `false` | nie | nieustawione | `true` żąda modułu (`LaunchSpec.Modules.Enable`); `false` wyklucza go z sesji, także moduł wczytywany domyślnie, przez jego mapę lub przez plik ustawień (`LaunchSpec.Modules.Disable`). |
| `settings` | mapowanie | nie | brak | Ustawienia modułu według klucza (`LaunchSpec.Modules.Settings` jako `<id>.<key>`). Wartości są sprawdzane względem ustawień zadeklarowanych przez moduł przy planowaniu sesji (`OL_E_MODULE_SETTING_INVALID`). |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

Identyfikator modułu niezgodny z `[a-z0-9][a-z0-9-]{1,47}`, `enabled` inne niż `true` lub `false` albo klucz ustawienia niezgodny z `[a-z][a-z0-9_]{0,31}`: `OL_E_SESSION_PROFILE_INVALID`. Moduł, który nie jest zainstalowany, jest zgłaszany przy planowaniu sesji (`OL_E_MODULE_NOT_FOUND` dla `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` dla samych ustawień).

<a id="settings"></a>
## Ustawienia

Klucze `settings` to semantyczne nazwy z `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). Kompilator akceptuje klucz tylko wtedy, gdy istnieje (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) i jest zapisywalny (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Wartości są przechowywane jako ciągi znaków i przekształcane w poprawkę `options.cfg`, gdy sesja buduje swoje nakładki; nieprawidłowa wartość jest więc wykrywana w `StartSessionAsync`, a nie przy wczytywaniu profilu, i powoduje niepowodzenie sesji z `OL_E_START_SESSION`, którego komunikat zawiera `OL_E_INVALID_SETTING_VALUE: <key>`. Każde z poniższych ustawień zapisuje `options.cfg`; wszystkie mają zasięg sesji i po jej zakończeniu są dokładnie przywracane.

Postacie wartości:

- **bool** to `true` lub `false` (bez rozróżniania wielkości liter). W przypadku tokenów obecności token jest dodawany lub usuwany; w przypadku tokenów odwróconych (`no_*`) wartość `true` usuwa token negatywny.
- **int** / **decimal** są walidowane względem zakresu; wartości z dzielnikiem są zapisywane po podzieleniu (na przykład `graphics.minObjectScreenPercent: 5` zapisuje `0.05`).
- **string** jest zapisywany dosłownie.

| Klucz ustawienia | Token `options.cfg` | Typ | Zakres / wartości | Dowody |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | dowolna | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | dowolna | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (obecność) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (obecność) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (obecność) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (obecność) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (obecność) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (obecność) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | dowolna | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, zapisywane /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, zapisywane /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (obecność) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (obecność) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, zapisywane jako `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` lub `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (blok 4-wierszowy) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (obecność) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (obecność odwrócona) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (oba tokeny obecności) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (obecność) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (obecność) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (obecność) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (obecność) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | składowa 0 `AIMaxCountRandom` | int | 0..1000 | STATICALLY_VALIDATED (runtime RV-005) |
| `traffic.humans` | składowa 1 `AIMaxCountRandom` | int | 0..1000 | STATICALLY_VALIDATED (runtime RV-005) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Wpisy katalogu, które istnieją, ale **nie są zapisywalne** (odrzucane z `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (zastąpione przez `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Ograniczenie ścieżek

`presentation.splash.assets` i `internet-textures.profile` są rozwiązywane przez `Confined(package root, value)`:

1. Ścieżki zakorzenione (`C:\...`), ścieżki zaczynające się od `\` oraz każda ścieżka ze składową równą `..` są odrzucane.
2. Obliczana jest pełna ścieżka, która musi zaczynać się od katalogu pakietu.
3. Każda istniejąca składowa poniżej katalogu głównego pakietu, aż do ścieżki końcowej włącznie, jest sprawdzana pod kątem atrybutu `ReparsePoint`. Połączenie (junction), dowiązanie symboliczne katalogu lub dowiązanie symboliczne pliku w dowolnym miejscu tej ścieżki jest odrzucane, podobnie jak składowa, której nie da się sprawdzić (`IOException` / `UnauthorizedAccessException`).

Wszystkie trzy błędy to `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Ta sama reguła punktów ponownej analizy jest stosowana przy budowaniu sesji do celów `.itx` w `Texture\`.

<a id="precedence-and-override-conflicts"></a>
## Pierwszeństwo i konflikty nadpisań

`CliInput.BuildSpecAsync` składa specyfikację w następującej kolejności:

1. **Wartości domyślne** (NEW_MAP, wszystko nieustawione, limity czasu 180 s / 30 s).
2. **`/spec:<file.json>`**, jeśli podano, całkowicie zastępuje wartości domyślne.
3. **Katalog główny instalacji**: jawny argument instalacji wygrywa z `RootPath` ze specyfikacji; `.` oznacza katalog zawierający plik wykonywalny.
4. **Profil** (`/predefined-profile` + `/predefined-profile-index`): pakiet jest wczytywany, a `RejectProfileConflicts` działa na surowych argumentach CLI **zanim** cokolwiek zostanie scalone. Blok świata z punktu wyjścia jest następnie resetowany do pustego `WorldSpec` wybranego trybu (świat z `/spec` jest odrzucany, gdy używany jest profil), a `SessionProfileCompiler.Apply` nakłada profil na punkt wyjścia: `new` (tylko NEW_MAP), `settings` (scalane na `Environment.General` z punktu wyjścia, profil wygrywa dla każdego klucza) oraz `presentation`, `internet-textures`, `behavior` (każdy zastępuje blok z punktu wyjścia tylko wtedy, gdy ustawienie wstępne go definiuje).
5. **Pozostałe argumenty CLI** są nakładane na wierzch: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, flagi pogody, flagi pojazdu, `/set`, flagi ekranu startowego, flagi tekstur internetowych, `/startup-timeout`, `/shutdown-timeout`. Limity czasu z CLI są stosowane tylko wtedy, gdy zostały podane; w przeciwnym razie obowiązuje wartość ze specyfikacji/profilu/domyślna.
6. **Sprawdzenie zgodności** dla trybów innych niż NEW_MAP (`ValidateCompatibility`).

Argument CLI, który dotyczy pola należącego do wybranego profilu, jest konfliktem odrzucanym z `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (kod wyjścia 2, kategoria `invalid_argument`). Sprawdzenie odbywa się na poziomie pola, a nie wartości: powtórzenie wartości zdefiniowanej w profilu też jest konfliktem.

| Argument CLI | Konflikt, gdy profil definiuje | Tylko w trybie |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` lub `/entrypoint-index` | `new.entrypoint` lub `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | ten sam `<key>` w `settings` ustawienia wstępnego (bez rozróżniania wielkości liter) | dowolny |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (dowolny) | dowolny |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (dowolny) | dowolny |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (dowolny) | dowolny |
| `/module:<id>` | `modules.<id>.enabled: false` | dowolny |
| `/no-module:<id>` | `modules.<id>.enabled: true` | dowolny |
| `/module-setting:<id>.<key>=...` lub flaga modułu | `modules.<id>.settings.<key>` | dowolny |

Nie są konfliktami: klucze `/set`, których ustawienie wstępne nie definiuje (są dodawane), flagi pojazdu (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; profil nie może definiować pojazdu gracza) oraz dowolny argument świata przy `/saved` (blok `new` nie jest tam stosowany). `/map`, `/entrypoint` i `/entrypoint-index` są nieprawidłowe razem z `/saved` niezależnie od profili (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Kody błędów

| Kod | Zgłaszany, gdy | Kod wyjścia CLI |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` nie istnieje | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` nie jest zwykłą nazwą katalogu; `assets` / `profile` wychodzi poza pakiet lub przechodzi przez punkt ponownej analizy | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` nie ma wartości `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | limit rozmiaru, kształt dokumentu, kotwice, nieznany klucz, brak wymaganego klucza, wartość niebędąca skalarem, błędna liczba/data/godzina, niezgodność `id`, reguły liczby/indeksów ustawień wstępnych, nieobsługiwane słowa trybu, niedodatni limit czasu, `presentation` bez `splash`, `override` bez `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | brak `/predefined-profile-index`, wartość spoza 1..99 lub brak ustawienia wstępnego z tym `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | klucza `settings` nie ma w katalogu | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | klucz `settings` jest w katalogu, ale jest tylko do odczytu | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | katalog `assets` lub plik `profile` nie istnieje wewnątrz pakietu | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` jest niepuste, a efektywnej mapy nie ma na liście (lub nie da się jej ustalić) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | jawny argument CLI dotyczy pola należącego do profilu | 2 |

Wszystkie te błędy są zgłaszane podczas kompilowania wiersza polecenia, przed planowaniem. Są to wyjątki `SessionProfileException` (lub `ArgumentException` w przypadku konfliktu) i nigdy nie uruchamiają sesji. Pełny katalog znajduje się w dokumentacji [błędów](errors.md); kody wyjścia – w dokumentacji [kodów wyjścia](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Wykrywanie zainstalowanych profili

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) kompiluje każdy pakiet w `.omsilaunch\session-profiles\` i wypisuje jeden wpis na pakiet: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` oraz jego `Presets` (`Index`, `Id`, `Name`) w kolejności indeksów. Pakiet, który się nie kompiluje, jest mimo to wypisywany, z `IsValid: false` i tym samym kodem `OL_E_SESSION_PROFILE_*`, który zgłosiłoby uruchomienie. Nic nie jest zapisywane.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Jak profil jest widoczny w API

Po pomyślnym wczytaniu specyfikacja zawiera rekord `SessionProfileMetadata` w `LaunchSpec.SessionProfile`:

| Pole | Źródło |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` wybranego ustawienia wstępnego |
| `PresetIndex` | `index` wybranego ustawienia wstępnego |
| `PresetName` | `name` wybranego ustawienia wstępnego |
| `PackagePath` | bezwzględna ścieżka katalogu pakietu |

Planista dodaje informacyjny komunikat diagnostyczny `session_profile.selected` do każdego `SessionPlan` zbudowanego z takiej specyfikacji, z kluczami danych `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` i `session_profile.path`. Nie wpływa on na możliwość uruchomienia. Integratorzy korzystający bezpośrednio z [publicznego API](public-api.md) mogą wywoływać `SessionProfileCompiler.Load` i `SessionProfileCompiler.Apply` z `OmsiLaunch.Core`; reprezentacja YAML nigdy nie przechodzi do `OmsiLaunch.Api`.

<a id="examples"></a>
## Przykłady

<a id="example-1-settings-only-profile-one-preset"></a>
### Przykład 1: profil zawierający tylko ustawienia, jedno ustawienie wstępne

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

Uruchomienie: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. Mapa i punkt wejścia pochodzą z wiersza polecenia, ponieważ profil nie definiuje bloku `new`; dodanie `/set:graphics.maxFPS=60` jest dozwolone, a dodanie `/set:traffic.humans=10` jest konfliktem.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Przykład 2: profil powiązany z mapą, z trzema ustawieniami wstępnymi i zasobami w pakiecie

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, z plikami `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` i `textures\offline.itx` wewnątrz pakietu:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

Uruchomienie: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Przy `/saved:situations\mytrip.osn` blok `new` jest pomijany, a plik `.osn` musi wskazywać na `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### Przykład dołączony do pakietu

Wydanie zawiera `docs/examples/session-profiles/rmg-leste/profile.yaml` ([podgląd](../../../examples/session-profiles/rmg-leste/profile.yaml)). Jest on poprawny składniowo, zgodny ze schematem i wczytałby się bez błędów. Dwie jego właściwości sprawiają, że w tym buildzie w niezmienionej postaci nie uruchamia sesji:

1. Ustawia `new.date`, `new.time` i `new.weather`, co czyni plan niemożliwym do uruchomienia (zob. [Blok `new`](#new)).
2. Jego wartości ścieżek to zwykłe skalary z podwojonymi ukośnikami odwrotnymi (`maps\\RMG Leste\\global.cfg`). YAML zachowuje je podwojone, a identyfikatory map są porównywane tekstowo (wyłącznie po normalizacji `/` do `\`), więc `new.map` i `compatibility.maps` nie pasowałyby do identyfikatora z katalogu `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` podczas planowania). Wartość `assets` nadal się rozwiązuje, ponieważ normalizacja ścieżek Windows scala podwojone separatory.

Postać możliwa do uruchomienia w tym buildzie:

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## Powiązane strony

- [Dokumentacja CLI](cli.md) – `/predefined-profile`, `/predefined-profile-index`, `/set` i flagi świata
- [LaunchSpec](launchspec.md) – rekord, do którego kompilowany jest profil
- [Transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md) – sposób stosowania i przywracania nakładek `settings`, ekranu startowego i `.itx`
- [Możliwości](capabilities.md) i [stan weryfikacji w runtime](../status/runtime-validation-status.md)
