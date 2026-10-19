# Moduły runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/runtime-modules.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Moduł runtime dodaje zachowanie do zarządzanej sesji OMSI: funkcję działającą razem z sesją (na przykład HUD mapy lub optymalizacje wczytywania), która nie staje się częścią samego OmsiLaunch. Mostek wczytuje moduł, udostępnia mu wyłącznie to, co moduł zadeklarował, i usuwa wszystko, co moduł przechowuje, gdy sesja się kończy lub moduł ulega awarii. Ta strona jest normatywnym opisem modułów runtime w postaci zaimplementowanej w `src/OmsiLaunch.Runtime.Modules` (manifest, ustawienia, kwalifikowanie, host cyklu życia), w planerze, w programach sesji kontrolera (`ModuleSessionPrograms`, `ModuleSessionProcess`), w środowisku modułów wtyczki oraz we flagach modułów w [CLI](cli.md).

Stabilność: `EXPERIMENTAL`. Protokół mostka 1 został zweryfikowany w runtime w rundzie B (moduły zarządzane, aktywacja zawartości, operacje, `disable-module` i `fail-session`; komponenty natywne z hookami IAT; zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md)). Protokół mostka 2 (ustawienia, aktywacja domyślna, plik ustawień, programy sesji, konflikty wtyczek, powiadomienia, `get_value`) jest zweryfikowany tylko offline. Bramki offline: bramki `module.*` z `tests/OmsiLaunch.ModuleTests` (manifest, integralność, wybór i aktywacja, ustawienia, plik ustawień, konflikty wtyczek, programy sesji uruchamiane przez swoje zadanie, awaria wstrzyknięta w każdej fazie przy każdej polityce awarii, cykl życia komponentu natywnego, czyszczenie, przekazanie uruchomieniowe), `plugin-runtime.modules-lifecycle` (rzeczywisty moduł zarządzany wczytany przez środowisko wtyczki), `runtime-modules.native-component` (rzeczywisty komponent natywny hookujący import i odczytujący wartości przez mostek), `runtime-modules.plugin-conflict` oraz `native-hooks.self-check` (silnik hooków). Protokół mostka 3 (Beta 5: operacje katalogu w procesie, tick, przechwytywanie wejścia, dane wejściowe zawartości społeczności, opcjonalne przypięcia z migawką z czasu planowania, `module pack`, opcje zadania programów pomocniczych, wersja OMSI) jest zweryfikowany tylko offline: bramki `module.*` z `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` i `ui-input.hook-self-check` z testów integracyjnych wtyczki oraz `omsi-version.literal` z testów jednostkowych. Przejęcie menu pauzy i przechwytywanie wejścia nie zostały zaobserwowane w rzeczywistej sesji OMSI.

Wczytywanie jest otwarte: można wczytać każdy moduł zainstalowany poniżej `plugins\OmsiLaunch.Modules`, bez względu na to, kto go wykonał. OmsiLaunch nie ręczy za to, co moduł robi wewnątrz OMSI. Gwarantuje natomiast [granicę bezpieczeństwa](#security-boundary): moduły nigdy nie działają wewnątrz procesu OmsiLaunch, a nic, co oferuje OmsiLaunch, nie pozwala modułowi wyjść poza uprawnienia samego OMSI.

<a id="where-modules-live"></a>
## Gdzie znajdują się moduły

Każdy moduł jest katalogiem poniżej folderu stałej wtyczki:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

Nazwa katalogu musi być równa `id` modułu. Moduły instaluje ten, kto je dystrybuuje; sesja nigdy nie zapisuje, nie robi migawek ani nie przywraca tam plików, a moduły nigdy nie zmieniają plików instalacji za pośrednictwem OmsiLaunch (manifest nie ma sposobu, by zadeklarować nakładkę instalacji). Zapisywalne dane modułu znajdują się w jego [katalogu danych](#data-directory).

## Manifest (`module.json`)

Schemat `omsilaunch.runtime-module/v1`. Nieznane klucze są odrzucane, więc manifest napisany dla późniejszego schematu jest odrzucany, zamiast być zrozumiany połowicznie. Wszystkie ścieżki są względne wobec katalogu modułu (ścieżki powiązanej zawartości są względne wobec katalogu głównego OMSI), używają `\` lub `/` i nie mogą zawierać `..` ani litery dysku.

| Klucz | Wymagany | Znaczenie |
| --- | --- | --- |
| `schema` | tak | `omsilaunch.runtime-module/v1`. |
| `id` | tak | Identyfikator modułu: małe litery, cyfry i `-`, od 2 do 48 znaków, zaczyna się od litery lub cyfry. Jest to także nazwa jego katalogu i jego przestrzeń nazw operacji. |
| `version` | tak | `major.minor.patch`, opcjonalnie `-suffix`. |
| `name` | nie | Nazwa wyświetlana (najwyżej 128 znaków). |
| `bridge_protocol` | tak | `{ "min": n, "max": n }`: protokoły mostka, z którymi moduł działa. Ten build obsługuje protokoły `1`, `2` i `3`; moduł jest akceptowany, gdy jego zakres obejmuje jeden z nich. |
| `activation` | nie | `requested` (domyślnie): wczytywany, gdy sesja o to prosi. `content`: wczytywany także wtedy, gdy sesja uruchamia powiązaną mapę (wymaga `content_binding`). `default` (protokół 2): wczytywany w każdej sesji, chyba że jest wyłączony (zob. [wybór modułów](#choosing-modules-for-a-session)); nie może mieć `content_binding`. |
| `required_capabilities` | nie | Możliwości, bez których moduł nie może działać (zob. [możliwości](#capabilities)). |
| `optional_capabilities` | nie | Możliwości, których moduł używa, gdy są dostępne; sprawdza je przez `IsCapabilityAvailable`. |
| `content_binding` | nie | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: moduł działa tylko na tej mapie, z tymi plikami w stanie niezmienionym. |
| `assets` | nie | Pliki, które moduł odczytuje, każdy jako `{ "path", "sha256" }`. Od protokołu 3 `sha256` jest opcjonalny (zob. [integralność](#integrity)). |
| `components` | tak | Dokładnie jeden komponent `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; opcjonalnie jeden komponent `native` (`.dll`), komponenty `auxiliary` (`.exe`) i, w protokole 2, jeden komponent `host` (`.exe`, [program sesji](#session-programs) modułu). Od protokołu 3 `sha256` komponentu jest opcjonalny, a komponent `auxiliary` może mieć `priority` (`normal`, `below-normal` lub `idle`) i `memory_limit_mb` (od 64 do 65536): obiekt zadania (job object) programu stosuje je od jego pierwszej instrukcji (zob. [programy pomocnicze](#auxiliary-programs)). |
| `failure_policy` | tak | `fail-session`, `disable-module` lub `degrade` (zob. [awarie](#failures)). |
| `operations` | nie | Operacje, które moduł udostępnia, gdy sesja działa: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | nie | Protokół 2. Ustawienia akceptowane przez moduł (zob. [ustawienia](#settings)). |
| `conflicts` | nie | Protokół 2. `{ "plugins": [ "<file>.dll" ] }`: pliki DLL wtyczek OMSI, które nie mogą działać obok modułu (zob. [konflikty](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | nie | Protokół 3. Operacje publicznego katalogu możliwości, które moduł wykonuje w procesie, na przykład `["time.read", "camera.set"]` (zob. [operacje w procesie](#operations-in-process)). Wymaga możliwości `bridge.runtime`. |
| `tick_ms` | nie | Protokół 3. Moduł chce okresowego wywołania zwrotnego co `tick_ms` milisekund: wielokrotność 50, od 50 do 1000 (zob. [tick](#tick)). Wymaga możliwości `bridge.tick`. |
| `content_inputs` | nie | Protokół 3. Zawartość społeczności, którą moduł odczytuje, lokalizowana przez mostek: `[ { "kind": "realmap-package", "required": false } ]` (zob. [zawartość społeczności](#community-content)). |
| `timeouts` | nie | Budżety w milisekundach: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (domyślnie po 5000, najwyżej 30000); w protokole 2 także `prepare_ms` (domyślnie 120000, najwyżej 1800000) i `host_stop_ms` (domyślnie 10000, najwyżej 60000) dla programu sesji. Operacje domyślnie mają 2000 ms. |
| `diagnostics` | nie | `{ "trace": true\|false }`. |

Manifest, który używa funkcji protokołu 2 (`settings`, `conflicts`, aktywacja `default`, komponent `host`, `prepare_ms` lub `host_stop_ms`), musi deklarować `bridge_protocol.min` równe 2. Manifest, który używa funkcji protokołu 3 (`runtime_operations`, `tick_ms`, `content_inputs`, możliwość `bridge.runtime`, `bridge.tick` lub `bridge.input`, `priority` lub `memory_limit_mb` komponentu pomocniczego albo komponent lub zasób bez `sha256`), musi deklarować `bridge_protocol.min` równe 3.

Każdy plik wymieniony w `components` i `assets` może być przypięty swoim SHA-256 (wymagane przed protokołem 3, opcjonalne od niego: zob. [integralność](#integrity)). `module.json` nie może przypiąć samego siebie.

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## Integralność

Każdy plik wymieniony w manifeście musi istnieć i nie może być osiągany przez punkt połączenia (junction) ani dowiązanie symboliczne, a każdy plik przypięty przez autora musi mieć przypięty SHA-256; w przeciwnym razie moduł jest odrzucany z kodem `OL_E_MODULE_INTEGRITY`. Dzięki temu wykrywany jest moduł uszkodzony lub zaktualizowany do połowy. Nie jest to decyzja o zaufaniu: moduł nie jest sprawdzany względem żadnej listy, podpisu ani wydawcy.

Od protokołu 3 przypięcie jest opcjonalne, więc autor nigdy nie musi ręcznie utrzymywać skrótów, a plan zawsze robi **migawkę** (snapshot): jeden SHA-256 z każdego pliku wymienionego w manifeście i jego zawartości, zapisany w planie (`PlannedModule.SnapshotSha256`) i przekazany wtyczce w przekazaniu uruchomieniowym (wersja formatu 8). Migawka gwarantuje, że pliki ocenione przez plan są plikami, które działają: wtyczka wczytuje moduł tylko wtedy, gdy jego `module.json` ma SHA-256, który ocenił plan, a jego pliki nadal mają zawartość z migawki, a kontroler i mostek porównują migawkę ponownie tuż przed uruchomieniem programu sesji, komponentu natywnego lub programu pomocniczego. Moduł, którego pliki zmieniły się po planowaniu, nie jest wczytywany (`OL_E_MODULE_INTEGRITY`, „the module's files changed after planning”). Migawka obejmuje każdy moduł, przypięty lub nie; przypięcie dodatkowo pozwala temu, kto publikuje moduł, wykryć uszkodzoną lub zaktualizowaną do połowy kopię, gdy tylko zostanie wykryta.

Sprawdzenie jest powtarzane tam, gdzie startuje kod modułu: wtyczka wczytuje moduł tylko wtedy, gdy jego `module.json` nadal ma SHA-256, który ocenił plan, a każde przypięcie i migawka nadal są spełnione, a kontroler uruchamia program sesji dopiero po zweryfikowaniu przypięcia tego programu i migawki.

<a id="packing-a-module"></a>
### Pakowanie modułu

`OmsiLaunch.exe /pack-module:<module directory>` zapisuje przypięcia dla plików, które wymienia `module.json` (jego komponenty i zasoby), więc autor nigdy nie edytuje SHA-256 ręcznie. Przepisuje wyłącznie wartości `sha256` (reszta manifestu jest zachowana; plik jest ponownie wcinany), przed zapisaniem czegokolwiek ponownie odczytuje wynik parserem manifestu i raportuje każdy plik (koperta `module.pack`: `module`, `directory`, `written`, `intact`, `files` z `path`, `sha256`, `pinned`, `mismatch`, `written`). Działa na katalogu, który otrzymał, i nigdy nie dotyka instalacji. `/verify-module:<module directory>` raportuje to samo bez zapisu (`module.verify`); kończy się kodem `7`, gdy przypięty plik nie zgadza się ze swoim przypięciem (plik bez przypięcia nie jest niezgodnością). Oba kończą się kodem `2` dla manifestu, którego nie da się sparsować, lub brakującego pliku.

<a id="security-boundary"></a>
## Granica bezpieczeństwa

OmsiLaunch nie kontroluje tego, co moduł robi wewnątrz OMSI: jego komponenty zarządzane i natywne działają w procesie OMSI z takimi samymi uprawnieniami jak każda wtyczka OMSI. OmsiLaunch gwarantuje następujące rzeczy:

1. Proces OmsiLaunch nigdy nie wczytuje kodu modułu. Kod modułu działa wewnątrz OMSI, w programie pomocniczym uruchomionym z wnętrza OMSI lub w programie sesji modułu.
2. Programy, które OmsiLaunch uruchamia dla modułów, są plikami z własnego katalogu modułu, zweryfikowanymi względem przypięcia i uruchamianymi z wierszem poleceń ustalonym przez OmsiLaunch. Program sesji działa w obiekcie zadania (job object), który kończy go razem z sesją, nie dopuszcza drugiego procesu (nie może uruchamiać innych programów) i odrzuca zmiany parametrów systemowych, ustawień wyświetlania, pulpitu oraz wylogowanie lub zamknięcie systemu. Dziedziczy wyłącznie dwa uchwyty swoich potoków.
3. Wszystko, co moduł deklaruje, pozostaje wewnątrz jego katalogu, a jego zapisywalne dane wewnątrz `.omsilaunch\module-data\<id>`.
4. Kanały OmsiLaunch (lokalne sterowanie, przekazanie uruchomieniowe, telemetria, polecenia runtime) pozostają ograniczone do bieżącego użytkownika i powiązane z sesją; żaden z nich nie pozwala wywołującemu uruchomić programu, odczytać ani zapisać plików poza instalacją, ani zmienić ustawień systemowych.
5. OmsiLaunch nigdy nie podnosi uprawnień. Moduł działa z uprawnieniami użytkownika, który uruchomił sesję.
6. Tekst pochodzący z modułu (powiadomienia, zdarzenia, wartości) jest traktowany jako dane: ograniczony, bez znaków sterujących, nigdy nie uruchamiany, nie otwierany ani nie śledzony jako łącze.

<a id="capabilities"></a>
## Możliwości

`required_capabilities` i `optional_capabilities` wskazują albo możliwość natywną działającego buildu (kolumna możliwości natywnej w [katalogu możliwości](capabilities.md#capability-catalog), na przykład `time.read` lub `d3d.texture`), albo funkcję mostka:

| Funkcja mostka | Co otrzymuje moduł |
| --- | --- |
| `bridge.events` | `Emit`: zdarzenia runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: śledzone wątki działające w tle. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: procesy pomocnicze powiązane z sesją. |
| `bridge.hooks.iat` | Komponent natywny: hooki na importach `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Komponent natywny: hooki na metodach działającego urządzenia Direct3D 9 (`d3d9:device.<method>`). Wymaga możliwości natywnej `d3d.texture`. |
| `bridge.runtime` | Protokół 3. `IRuntimeModuleContext.Runtime` i natywne `run_operation`: operacje katalogu, które manifest wymienia w `runtime_operations`. |
| `bridge.tick` | Protokół 3. Okresowe wywołanie zwrotne: `ITickingModule.Tick` i eksport natywny `omsilaunch_module_native_v1_tick`. |
| `bridge.input` | Protokół 3. `IRuntimeModuleContext.Input`: przechwytywanie komunikatów klawiatury i myszy gry na potrzeby własnego menu modułu. Wymaga możliwości natywnej `ui.input.capture`. |

Nazwa, która nie jest żadną z nich, jest odrzucana podczas planowania (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). W buildzie OMSI 2 bez znanego układu możliwość natywna, której plik wykonywalny nie rozstrzyga, jest rozstrzygana w procesie; jeśli nie zostanie tam rozstrzygnięta, moduł nie jest wczytywany.

<a id="choosing-modules-for-a-session"></a>
## Wybór modułów dla sesji

Moduł dołącza do sesji na jeden z czterech sposobów; `PlannedModule.Activation` mówi, który to sposób:

| Aktywacja | Kiedy |
| --- | --- |
| `requested` | Sesja o niego prosi: `LaunchSpec.Modules.Enable`, `/module:<id>` lub `enabled: true` w wybranym ustawieniu wstępnym profilu sesji. |
| `settings-file` | `enabled=true` w jego sekcji [pliku ustawień](#the-settings-file). |
| `content` | Jego aktywacja to `content`, a sesja uruchamia jego powiązaną mapę. |
| `default` | Jego aktywacja to `default`. |

Moduł jest wykluczany z sesji przez `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` w ustawieniu wstępnym lub `enabled=false` w pliku ustawień (komunikat diagnostyczny planu `module.disabled`). Jawne żądanie ma pierwszeństwo przed plikiem ustawień. Włączenie i wyłączenie tego samego modułu w jednym żądaniu to `OL_E_MODULE_SETTING_INVALID`.

Reguły planowania: żądany moduł, którego nie można wczytać, powoduje, że plan jest niemożliwy do uruchomienia, z kodem `OL_E_MODULE_*`. Moduł, który dołączył sam (plik ustawień, zawartość, domyślnie) i nie może zostać wczytany, nie zatrzymuje sesji: plan zgłasza `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Ustawienia

Moduł deklaruje swoje ustawienia w `settings`; każdy wpis:

| Klucz | Wymagany | Znaczenie |
| --- | --- | --- |
| `key` | tak | `[a-z][a-z0-9_]{0,31}`, nie `enabled`. |
| `type` | tak | `boolean`, `integer`, `choice` lub `text`. |
| `default` | tak | Wartość, gdy nic innego jej nie ustawia: wartość logiczna, liczba całkowita, ciąg znaków lub `"auto"`, gdy `auto` ma wartość true. |
| `auto` | nie | `true`: wartość `auto` jest akceptowana i przekazywana modułowi, który decyduje o niej w czasie działania. |
| `min`, `max` | nie | Granice liczby całkowitej. |
| `choices` | dla `choice` | Akceptowane wartości (`[a-z0-9][a-z0-9_.-]{0,31}`, nie `auto`). |
| `max_length` | nie | Limit długości tekstu (1..4096, domyślnie 256). Znaki sterujące są odrzucane. |
| `flag` | nie | Flaga wiersza poleceń dla tego ustawienia, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>` lub samo `/<flag>` dla `true` w ustawieniu logicznym. |
| `description` | nie | Najwyżej 256 znaków. |

Wartości są normalizowane: wartości logiczne akceptują `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; wybory są dopasowywane bez rozróżniania wielkości liter; liczby całkowite są sprawdzane względem swoich granic.

Efektywna wartość każdego ustawienia pochodzi, w tej kolejności:

1. z żądania: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, własnej flagi modułu lub `modules.<id>.settings` w wybranym ustawieniu wstępnym profilu sesji. Podobnie jak w przypadku każdego innego pola ustawienia wstępnego, wiersz poleceń nie może jednocześnie ustawiać klucza, który ustawia ustawienie wstępne (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. z [pliku ustawień](#the-settings-file);
3. z wartości domyślnej w manifeście.

Nieprawidłowa żądana wartość lub żądany klucz, którego moduł nie deklaruje, powoduje, że plan jest niemożliwy do uruchomienia (`OL_E_MODULE_SETTING_INVALID`). Wpis pliku ustawień, którego nie można użyć, jest ignorowany z `OL_W_MODULE_SETTING_IGNORED`, podobnie jak żądane ustawienia dla modułu, którego sesja nie wczytuje. Plan wymienia każdą efektywną wartość wraz ze źródłem (`request`, `settings-file` lub `default`) w `PlannedModule.Settings`.

Moduł otrzymuje każdy zadeklarowany klucz z jego efektywną wartością: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` w komponencie natywnym oraz obiekt `settings` w kontekście programu sesji.

<a id="the-settings-file"></a>
### Plik ustawień

`<OMSI root>\.omsilaunch\modules.ini`, obok profili sesji. OmsiLaunch go tylko odczytuje.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Sekcje są identyfikatorami modułów; wiersze `key=value` ustawiają ustawienia, `enabled` włącza lub wyłącza moduł w każdej sesji; wiersze zaczynające się od `;` lub `#` są komentarzami. Plik może mieć najwyżej 256 KiB. Wszystko, czego nie da się użyć (sekcja, która nie jest identyfikatorem modułu, wiersz poza sekcją, nieznany klucz, nieprawidłowa wartość), jest zgłaszane z `OL_W_MODULE_SETTING_IGNORED` i ignorowane.

<a id="session-programs"></a>
## Programy sesji

Moduł może zadeklarować jeden komponent `host`: swój program sesji, plik wykonywalny z własnego katalogu. Gdy moduł znajduje się w planie, kontroler:

1. weryfikuje przypięcie programu i tworzy katalog danych modułu;
2. uruchamia go przed OMSI, z katalogu modułu, z wierszem poleceń `"<program>" --omsilaunch-session <session id>`, wstrzymanego do chwili, gdy znajdzie się w swoim zadaniu (zob. [granica bezpieczeństwa](#security-boundary));
3. zapisuje kontekst jako pierwszy wiersz na jego standardowym wejściu;
4. czeka, aż program zapisze `{"type":"ready"}`, najwyżej przez `prepare_ms`; dopiero wtedy uruchamia OMSI;
5. po utworzeniu OMSI zapisuje `{"type":"omsi-started","pid":<process id>}`;
6. na końcu sesji, po zakończeniu OMSI i przed przywróceniem jakiegokolwiek pliku, zapisuje `{"type":"stop"}`, czeka najwyżej `host_stop_ms`, a następnie kończy program.

Oba kierunki używają JSON w UTF-8, jeden obiekt na wiersz. Kontekst:

| Pole | Znaczenie |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Tożsamość. |
| `installation_root`, `module_directory`, `data_directory` | Pełne ścieżki. |
| `channel` | Potok nazwany, który program może obsługiwać dla komponentów modułu w OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. Program tworzy go z ograniczeniem do bieżącego użytkownika i sprawdza, czy klientem jest proces OMSI. Komponenty odczytują go przez `get_value("session_channel")` lub `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | Efektywne ustawienia modułu. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` sesji. |
| `session_files` | Pliki względne wobec instalacji, których właścicielem jest transakcja sesji. Program nie może ich trzymać otwartych ani zmapowanych: są przywracane, gdy sesja się kończy. |

Co program może zapisać oprócz `ready`:

| Wiersz | Skutek |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Zdarzenie runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Zdarzenie runtime `module.<id>.notice` (zob. [powiadomienia](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Komunikat diagnostyczny sesji z tym skatalogowanym kodem (w przeciwnym razie `OL_E_MODULE_OPERATION_FAILED`). |

Inne wiersze, wiersze dłuższe niż 64 KiB oraz wszystko po 10,000 wierszy są ignorowane. Kontroler zapisuje `module.<id>.host.started`, `host.ready` (z `prepare_ms`), `host.failed`, `host.exited` i `host.killed`.

Program, który nie jest gotowy w ciągu `prepare_ms`, kończy się przed gotowością lub którego przypięcie nie jest spełnione, powoduje niepowodzenie przygotowania swojego modułu: przy `fail-session` sesja nie startuje (`OL_E_MODULE_SESSION_FAILED`); przy `disable-module` program jest kończony, a moduł zostaje pominięty w OMSI; przy `degrade` moduł pozostaje (a program nadal działa, jeśli jeszcze może). `StartupTimeoutSeconds` sesji liczy się od chwili uruchomienia OMSI, więc przygotowanie go nie zużywa.

<a id="conflicts-with-omsi-plugins"></a>
## Konflikty z wtyczkami OMSI

`conflicts.plugins` wskazuje pliki DLL wtyczek OMSI, które nie mogą działać obok modułu, na przykład starszą wtyczkę instalującą te same hooki. Podczas planowania OmsiLaunch odczytuje sekcję `[dll]` każdego `plugins\*.opl`; jeśli któryś wczytuje konfliktujący plik DLL, moduł jest odrzucany z `OL_E_MODULE_CONFLICT`, a przyczyna wskazuje plik `.opl`. Wewnątrz OMSI moduł, którego konfliktujący plik DLL mimo to został wczytany, nie jest wczytywany. OmsiLaunch nigdy nie zmienia plików innej wtyczki; jej wyłączenie jest decyzją użytkownika.

<a id="notices"></a>
## Powiadomienia

Zdarzenie modułu o nazwie `notice` (z `Emit("notice", ...)`, z `emit` komponentu natywnego lub z wiersza powiadomienia programu sesji) jest wyświetlane przez ikonę OmsiLaunch w obszarze powiadomień jako dymek, którego tytuł wskazuje moduł. Zachowywane są tylko `title` (najwyżej 63 znaki) i `text` (najwyżej 255 znaków), znaki sterujące są usuwane i nic z tej treści nie jest otwierane ani uruchamiane. Program sesji, który musi poinformować użytkownika przed uruchomieniem OMSI (gdy ikona w obszarze powiadomień nie jest jeszcze widoczna), używa własnego okna.

<a id="data-directory"></a>
## Katalog danych

`<OMSI root>\.omsilaunch\module-data\<id>\` jest zapisywalnym katalogiem modułu dla tej instalacji: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` oraz `data_directory` programu sesji. Jest tworzony przy pierwszym użyciu. Nie jest częścią transakcji sesji: to, co moduł tam zapisze, pozostaje po sesji. `/clear-module-data:<id>` usuwa go (i nic więcej); jest odrzucane, dopóki sesja jest właścicielem instalacji, a katalog osiągany przez dowiązanie jest usuwany jako dowiązanie, nigdy nie jest śledzony.

<a id="lifecycle"></a>
## Cykl życia

| Krok | Gdzie | Co się dzieje |
| --- | --- | --- |
| 1. Wykrywanie | kontroler | Odczytywane jest `plugins\OmsiLaunch.Modules\*`: manifest i integralność (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Kwalifikowanie | kontroler, podczas planowania | Oceniane są moduły, które [dołączają do sesji](#choosing-modules-for-a-session): użyteczność, protokół mostka, możliwości, powiązanie zawartości, konflikty wtyczek, ustawienia. Wynikiem jest `SessionPlan.PlannedModules`. |
| 3. Przygotowanie | kontroler, podczas startu | [Programy sesji](#session-programs) modułów są uruchamiane i oczekiwane. Następnie kwalifikujące się moduły trafiają do wtyczki w przekazaniu uruchomieniowym (wersja 6): ich katalogi, SHA-256 każdego ocenionego `module.json` oraz ich efektywne ustawienia. |
| 4. Uzbrojenie | wtyczka, przed startem świata | Każdy moduł jest ponownie weryfikowany i wczytywany (komponent zarządzany we własnym kontekście wczytywania). Jego komponent natywny, jeśli istnieje, jest wczytywany i uruchamiany jako pierwszy, po czym wywoływane jest `Arm(context)`. |
| 5. Gotowość rozgrywki | wtyczka | Po `gameplay.entered`, gdy wczytany moduł deklaruje `bridge.hooks.d3d9`, mostek zaczyna obserwować urządzenie Direct3D (`d3d.ready`); następnie wywołuje `gameplay_ready` komponentu natywnego i `GameplayReady()`. W sesji NEW_MAP powiązana mapa jest sprawdzana ponownie względem mapy wczytanej przez start. |
| 6. Runtime | wtyczka | Operacje `module.<id>.<operation>` trafiają do `Execute`. Moduł z `tick_ms` otrzymuje tick (zob. [tick](#tick)); moduł z `runtime_operations` wykonuje je w procesie (zob. [operacje w procesie](#operations-in-process)). |
| 7. Zatrzymanie | wtyczka, następnie kontroler | Gdy sesja się kończy (lub moduł ulega awarii), wywoływane jest `Stop()`. Po zakończeniu OMSI programy sesji są zatrzymywane. |
| 8. Czyszczenie | wtyczka | Wszystko, co moduł śledził, jest zwalniane w odwrotnej kolejności i weryfikowane jako usunięte; komponent natywny jest zatrzymywany jako ostatni, a jego hooki są usuwane; jego DLL pozostaje wczytany, nieaktywny, do zakończenia OMSI (wątek może jeszcze wracać przez detour). |

Każde wywołanie zwrotne w procesie działa w wątku interfejsu użytkownika OMSI, tak jak własne operacje runtime produktu. Mostek mierzy każde wywołanie zwrotne względem jego budżetu: wywołanie, które wraca zbyt późno, kończy swoją fazę niepowodzeniem (`OL_E_MODULE_PHASE_TIMEOUT`). Mostek nie może przerwać wywołania, które nigdy nie wraca; taki moduł zatrzymuje OMSI, a sesja kończy się z upływem limitu czasu uruchamiania lub przez `session stop`, jak każda inna sesja.

<a id="failures"></a>
## Awarie

Awarią jest wyjątek lub przekroczenie budżetu w dowolnej fazie lub operacji, możliwość nierozstrzygnięta w procesie, powiązanie zawartości, które nie jest spełnione w procesie, konfliktująca wtyczka wczytana w OMSI albo program sesji, który nie staje się gotowy. To, co następuje dalej, rozstrzyga `failure_policy` modułu. W każdym przypadku zasoby modułu są zwalniane. Tick, który zgłasza wyjątek, zwraca wartość niezerową lub przekracza swój interwał, jest awarią tak samo jak operacja.

| Polityka | Awaria w przygotowaniu, wczytywaniu, uzbrojeniu lub gotowości rozgrywki | Awaria w operacji | Awaria w zatrzymaniu |
| --- | --- | --- | --- |
| `fail-session` | Moduł zwolniony; `module.session-failure`; sesja kończy się niepowodzeniem z `OL_E_MODULE_SESSION_FAILED`, OMSI jest zatrzymywany, a pliki są przywracane. Awaria przed startem świata uniemożliwia start. | Tak samo. | Zgłaszana (`module.failed`); sesja trwa do swojego końca. |
| `disable-module` | Moduł zwolniony i wyłączony; sesja trwa. | Tak samo. | Zgłaszana; moduł zwolniony. |
| `degrade` | Jak `disable-module`, z tą różnicą, że awaria programu sesji pozostawia moduł w sesji. | Zawodzące wywołanie zwraca swój kod; moduł pozostaje aktywny; `module.degraded` jest zgłaszane raz dla każdego kodu. | Zgłaszana; moduł zwolniony. |

Wyłączony lub uszkodzony moduł odpowiada na swoje operacje kodem `OL_E_MODULE_DISABLED`. Awarie modułów pojawiają się jako komunikaty diagnostyczne sesji (`SessionStatus.Diagnostics`) z kodem `OL_E_MODULE_*` oraz jako zdarzenia runtime.

<a id="cleanup"></a>
## Czyszczenie

To, co moduł zdobywa przez swój kontekst, jest śledzone: zasoby zarejestrowane przez `Track`, wątki uruchomione przez `RunBackground`, procesy uruchomione przez `StartAuxiliaryProcess`. Przy zatrzymaniu lub awarii mostek anuluje `Stopping`, a następnie zwalnia je w odwrotnej kolejności. Procesy pomocnicze są ponadto umieszczane w obiekcie zadania (job object) kończącym je przy zamknięciu, więc kończą się razem z OMSI nawet wtedy, gdy OMSI zostanie zabity; zadanie programu sesji kończy go razem z sesją OmsiLaunch, nawet jeśli sam OmsiLaunch zostanie zabity. Zasób, który nadal żyje po zwolnieniu (proces, który się nie zakończył, wątek, który się nie zakończył), jest zgłaszany jako `OL_E_MODULE_CLEANUP_INCOMPLETE`. Wątki lub procesy, które moduł tworzy bez swojego kontekstu, nie są śledzone.

Po awarii (crashu) nie ma w modułach nic do odzyskiwania: nie przechowują plików instalacji, a ich pamięć, wątki i procesy powiązane z zadaniem kończą się razem z procesem OMSI lub procesem OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Komponenty natywne i hooki

Komponent natywny to 32-bitowy plik DLL implementujący interfejs C z `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Eksport | Wywoływany |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | Przy uzbrojeniu, przed zarządzanym `Arm`. Wartość niezerowa kończy fazę niepowodzeniem. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Opcjonalnie; po wejściu do rozgrywki. |
| `void omsilaunch_module_native_v1_stop(void)` | Przy zatrzymaniu lub awarii, przed usunięciem jego hooków. Potem tabela mostka odrzuca każde wywołanie, a DLL pozostaje wczytany, nieaktywny, do zakończenia OMSI. |

Tabela mostka daje komponentowi `request_hook`, `emit` (zdarzenia runtime `module.<id>.<name>` z obiektem JSON złożonym z ciągów znaków) oraz, od protokołu 2, `get_value`. Komponent odczytuje `size` tabeli przed użyciem `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` kopiuje wartość jako UTF-8 z kończącym zerem i ustawia `*length` na liczbę potrzebnych bajtów bez niego; zwraca `0`, `1` (argument), `2` (nieznana nazwa), `11` (po zatrzymaniu) lub `12` (bufor za mały). Od protokołu 3 tabela zawiera także `run_operation` (zob. [operacje w procesie](#operations-in-process)) oraz `d3d_is_game_device(context, device)`: czy wskaźnik urządzenia, który otrzymał hook Direct3D, jest własnym urządzeniem gry (`1`), czy innym (`0`; formularz wyboru pojazdu tworzy drugie urządzenie z tą samą tabelą metod, więc detour `Present` jest wywoływany dla obu i powinien rysować tylko dla urządzenia gry), `10`, gdy nie obserwuje się jeszcze żadnego urządzenia, `11`, gdy `bridge.hooks.d3d9` nie jest zadeklarowane lub po zatrzymaniu. Wskaźnik jest tylko porównywany, nigdy używany, a mostek nigdy go nie udostępnia. Moduł, który trzyma zasoby Direct3D w puli domyślnej, sam hookuje `d3d9:device.Reset` (to, co robi przed wywołaniem oryginału, to „utrata urządzenia”, a to, co robi po powrocie z sukcesem, to „przywrócenie urządzenia”). Komponent sprawdza `size` tabeli przed użyciem składowej protokołu 3 (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Dwa opcjonalne eksporty uzupełniają komponent: `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (zob. [tick](#tick)) oraz `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, w taki sposób, w jaki komponent zarządzany wywołuje natywny przez `IRuntimeModuleContext.NativeComponent.Execute` (JSON na wejściu, JSON na wyjściu, ta sama konwencja bufora co w `run_operation`). Nazwy, na które odpowiada `get_value`:

| Nazwa | Wartość |
| --- | --- |
| `setting.<key>` | Efektywna wartość zadeklarowanego ustawienia. |
| `data_directory` | [Katalog danych](#data-directory) modułu. |
| `module_directory` | Własny katalog modułu. |
| `session_id` | Identyfikator sesji. |
| `module_version` | Wersja z manifestu. |
| `product_version` | Wersja produktu OmsiLaunch. |
| `session_channel` | Potok [programu sesji](#session-programs); nieznany dla modułu bez takiego programu. |
| `omsi_version` | Wersja produktu OMSI, którą gra podaje o sobie (protokół 3; zob. [wersja OMSI](#the-omsi-version)). |

Hooki są żądane wyłącznie przez cel symboliczny, podczas `start` lub `gameplay_ready`, dla celu, którego możliwość manifest deklaruje:

| Cel | Hookuje | Możliwość |
| --- | --- | --- |
| `iat:<dll>!<function>` | Import `Omsi.exe`, na przykład `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` lub `EndScene` działającego urządzenia Direct3D 9 (należy zażądać w `gameplay_ready`, gdy urządzenie istnieje). | `bridge.hooks.d3d9` |

`request_hook` zwraca w `*original` funkcję pozwalającą kontynuować łańcuch (detour musi ją wywołać). Jego wynik: `0` sukces, `1` argument, `2` nieznany cel, `3` miejsce nie zawiera tego, co powinno, `4` miejsce jest zahookowane przez kogoś spoza mostka, `5` ochrona pamięci, `6` rejestr pełny, `7` kolejność, `8` pamięć, `9` wątek wykonywał bajty do załatania, `10` brak jeszcze urządzenia Direct3D, `11` niedozwolone teraz lub możliwość niezadeklarowana.

Każdy hook, który instaluje mostek, dla produktu lub dla modułu, jest zapisywany w jednym rejestrze wraz z właścicielem (`module:<id>` dla modułu):

- Hook jest instalowany tylko nad wartością, którą miejsce powinno zawierać (importowana funkcja; funkcja modułu o nazwie `d3d9.dll`, systemowego runtime lub opakowania) albo ułożony na szczycie własnego najwyższego hooka rejestru w tym miejscu. Miejsce już zmienione przez kogoś innego jest odrzucane (`4`), nigdy nadpisywane.
- Hooki w jednym miejscu są usuwane w kolejności ostatni wszedł, pierwszy wyszedł; hook, nad którym leży inny, pozostaje, dopóki tamten nie zostanie usunięty.
- Własny hook `Reset` Direct3D produktu jest w rejestrze, więc hooki urządzenia modułów układają się na nim.
- Podczas czyszczenia mostek usuwa każdy hook modułu po powrocie z `stop`. Jeśli któregoś nie da się usunąć (ktoś inny zmienił miejsce), czyszczenie zgłasza `OL_E_MODULE_CLEANUP_INCOMPLETE`. DLL komponentu natywnego nigdy nie jest wyładowywany, dopóki OMSI działa, więc detour, przez który jeszcze wracają wątki, pozostaje poprawny.

Silnik obsługuje także hooki na prologach funkcji z trampoliną (inne wątki są wstrzymywane, gdy bajty się zmieniają, a dokładnie oczekiwane bajty są sprawdzane wcześniej), ale żaden cel prologu nie jest jeszcze oferowany modułom: takie cele pochodzą z katalogu celów buildu ze zweryfikowanymi bajtami.

<a id="operations"></a>
## Operacje

Operacja modułu to `module.<id>.<operation>`. Istnieje tylko dla sesji, której plan wczytał moduł (`PlannedModule.Operations`); w przeciwnym razie `ExecuteRuntimeAsync` odpowiada `OL_E_MODULE_NOT_LOADED` lub `OL_E_MODULE_OPERATION_UNKNOWN`. Wymagane argumenty są sprawdzane, a niezadeklarowane argumenty są odrzucane (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Typowana awaria modułu zachowuje swój kod, gdy jest to skatalogowany kod publiczny; każdy inny kod staje się `OL_E_MODULE_OPERATION_FAILED` z kodem modułu w `Values["module_code"]`. Klucze wyniku zgodne z wewnętrzną konwencją nazewnictwa są usuwane, tak jak w każdej operacji runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## Programy pomocnicze

Moduł uruchamia jeden ze swoich komponentów `auxiliary` (przypięty `.exe` z własnego katalogu) przez `StartAuxiliaryProcess`. Od protokołu 3 mostek tworzy program **wstrzymany**, umieszcza go w obiekcie zadania sesji i dopiero potem go wznawia, więc proces potomny uruchomiony od razu nie może działać poza zadaniem. Zadanie kończy program i wszystko, co ten uruchomił, gdy moduł się zatrzymuje lub OMSI kończy działanie (kill on close), i stosuje `priority` (`below-normal`, `idle`) oraz `memory_limit_mb` z manifestu od pierwszej instrukcji programu. W odróżnieniu od [programu sesji](#session-programs) zadanie programu pomocniczego dopuszcza procesy potomne (program osadzający widok sieciowy ich potrzebuje); każdy z nich jest w zadaniu. Przed uruchomieniem program jest weryfikowany ponownie: jego przypięcie, jeśli je ma, oraz zawsze migawka planu.

<a id="operations-in-process"></a>
## Operacje w procesie

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (zarządzane) i `run_operation` (natywne) wykonują operację [publicznego katalogu możliwości](capabilities.md#public-runtime-operations) w procesie, w wątku interfejsu użytkownika OMSI, z tym samym rejestrem, tymi samymi typowanymi wynikami `OL_E_*`, tymi samymi kontrolami argumentów i tą samą polityką mutacji co kanał sterowania runtime, a bez samego kanału. Dozwolone są tylko operacje, które manifest wymienia w `runtime_operations`: każda inna otrzymuje odpowiedź `OL_E_MODULE_OPERATION_NOT_DECLARED`, zanim dotrze do OMSI, a operacji `internal.*` i `module.*` nigdy nie można wymienić. Operacja, której możliwości natywnej build nie rozstrzyga, odpowiada `OL_E_CAPABILITY_UNRESOLVED`, jak wszędzie indziej.

Lista jest informacją dla przejrzystości i planowania, a nie granicą bezpieczeństwa: moduł już działa wewnątrz OMSI z uprawnieniami użytkownika. Plan ją pokazuje (`PlannedModule.RuntimeOperations`), a operacja zmieniająca stan OMSI niesie tam swoją politykę mutacji, na przykład `camera.set (omsi-state)`. Moduł może wykonywać operacje od `Arm` (świat może jeszcze nie istnieć; operacja odpowiada wtedy własnym typowanym błędem). Wyniki są ograniczone (1 MiB); większy wynik to `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Po zatrzymaniu modułu każde wywołanie odpowiada `OL_E_MODULE_DISABLED`.

Natywne `run_operation(context, operation, arguments_json, buffer, capacity, length)` przyjmuje identyfikator z katalogu i obiekt JSON złożony z ciągów znaków (lub null) i zapisuje `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. Zwraca `0`, gdy odpowiedziało (własna awaria operacji jest wewnątrz JSON), `1` dla argumentu, `11` po zatrzymaniu i `12`, gdy bufor jest za mały (`*length` to potrzebny rozmiar).

## Tick

Moduł, który deklaruje `tick_ms` i możliwość `bridge.tick`, jest wywoływany mniej więcej co `tick_ms` milisekund, w wątku interfejsu użytkownika OMSI, z 50-milisekundowego zegara interfejsu wtyczki: `ITickingModule.Tick(TimeSpan elapsed)` w komponencie zarządzanym i opcjonalny eksport natywny `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. Interwał jest wielokrotnością 50 z zakresu od 50 do 1000. Tick nie przypada na klatkę: moduł, który rysuje, hookuje metodę Direct3D `Present` i używa ticku do odczytu danych. Ticki zaczynają się po `gameplay.entered` i kończą wraz z modułem. Tick, który zgłasza wyjątek, zwraca wartość niezerową (natywny) lub trwa dłużej niż jego interwał, jest awarią tego modułu: przy `degrade` jest zgłaszany raz dla każdego kodu (`module.degraded`), a moduł działa dalej (pominięte ticki są pomijane, nigdy kolejkowane); przy `disable-module` i `fail-session` obowiązuje polityka. Zagnieżdżona pętla modalna OMSI nadal uruchamia zegar interfejsu, więc tick nigdy nie wchodzi w siebie ponownie.

<a id="community-content"></a>
## Zawartość społeczności

`content_inputs` deklaruje zawartość, którą moduł odczytuje, a której jego autor nie może znać podczas budowania modułu, ponieważ tworzy ją społeczność, dla mapy, po module. Nie ma SHA-256 do przypięcia; mostek tylko ją lokalizuje i sprawdza, czy jej odczyt jest bezpieczny. Jedynym rodzajem na dziś jest `realmap-package`: folder `openomsi-realmap` mapy, `<OMSI root>\maps\<folder>\openomsi-realmap`, zapisany przez narzędzia pakietu map openOMSI.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` odpowiada, czy mapa ma pakiet (brak pakietu nie jest błędem) i, jeśli ma, podaje jego katalog, liczbę plików, łączny rozmiar i `Fingerprint`. Odcisk jest wykrywaczem zmian na podstawie względnej ścieżki, rozmiaru i czasu zapisu każdego pliku: należy go używać jako klucza pamięci podręcznej; nigdy nie jest kontrolą integralności ani decyzją o zaufaniu. Pakiet musi znajdować się wewnątrz instalacji, żadna jego część nie może być punktem reparse i jest ograniczony (50,000 plików, 4 GiB); w przeciwnym razie odpowiedzią jest `OL_E_MODULE_CONTENT_INPUT_INVALID`. Moduł, który nie zadeklarował tego rodzaju, otrzymuje `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. Pakiet to dane wytworzone przez osoby trzecie: należy je odczytywać w kodzie zarządzanym, traktować każdą wartość jako niezaufaną i degradować zasób po zasobie. Wejście z `required: true` powoduje, że plan odrzuca moduł (`OL_E_MODULE_CONTENT_BINDING`), gdy mapa sesji nie ma pakietu.

<a id="input-capture"></a>
## Przechwytywanie wejścia

Moduł, który zastępuje menu pauzy OMSI własnym, potrzebuje klawiatury i myszy gry. OMSI odczytuje klawisze symulacji przez DirectInput, a mysz przez komunikaty okna do okna widoku głównego formularza; gdy włączona jest jego własna pauza (natywny bajt pauzy), symulację, renderowanie, kliknięcia 3D i polecenia DirectInput wstrzymuje sam OMSI. `IRuntimeModuleContext.Input` (możliwość `bridge.input`, możliwość natywna `ui.input.capture`) dodaje resztę: `Capture(true)` instaluje jeden lokalny dla wątku hook `WH_GETMESSAGE` w wątku interfejsu użytkownika, a komunikaty klawiatury i myszy należące do drzewa okien gry (formularz główny, jego elementy potomne i ukryty natywny formularz pauzy) są zapisywane jako zwykłe wartości `ModuleInputEvent` (rodzaj, kod wirtualnego klawisza, współrzędne klienta okna widoku, przycisk, delta kółka, modyfikatory) i usuwane z kolejki, więc ani OMSI, ani VCL nie reagują na nie. `Drain()` zwraca zdarzenia (przechowywanych jest najwyżej 256). Nic nie jest poddawane subclassingowi, żadna funkcja stanu klawiszy z user32 nie jest hookowana i nic nie jest wysyłane.

Jednocześnie przechwytuje jeden moduł. Przechwytywanie jest zwalniane, gdy moduł się zatrzymuje lub ulega awarii, bez względu na to, co zrobiło `Stop`. Natywne okna modalne (zapis, opcje, nowa gra) wymagają prawdziwego wejścia: przed wyświetleniem takiego okna należy zwolnić przechwytywanie. Mechanika hooka jest zweryfikowana offline za pomocą okna zastępczego (`ui-input.hook-self-check`); jego zachowanie względem rzeczywistego okna gry nie zostało jeszcze zaobserwowane.

<a id="the-omsi-version"></a>
## Wersja OMSI

`IRuntimeModuleContext.OmsiVersion` i `get_value("omsi_version")` podają wersję produktu OMSI, którą gra podaje o sobie, na przykład `2.3.004`: tekst, który OMSI zapisuje po `Version:` w wierszu 7 swojego `logfile.txt`, odczytany z działającego obrazu. **Nigdy** nie jest to wersja pliku PE pliku wykonywalnego (build referencyjny ma `FileVersion` `2.2.032`). Gdy obraz nie zawiera jednego takiego tekstu, używana jest wersja profilu buildu, jeśli tożsamość kodu jest znana, a w przeciwnym razie `unknown`. Skrót pliku wykonywalnego, rozmiar jego kodu, adresy i nazwa profilu nie są udostępniane. Wersji należy używać do tekstu i do wyboru zachowania; możliwości (`IsCapabilityAvailable`) pozostają źródłem prawdy o tym, co działa.

<a id="writing-a-module-contracts"></a>
## Pisanie modułu (kontrakty)

Komponent zarządzany odwołuje się do `OmsiLaunch.Runtime.Modules` i `OmsiLaunch.Api` oraz implementuje `IRuntimeModule`. Mostek wiąże te dwa odwołania z własnymi wczytanymi zestawami (w jakimkolwiek kontekście wczytywania działa wtyczka), więc kopia dostarczona z modułem jest ignorowana:

| Składowa | Wywoływana |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Krok 4. Zasoby zdobywa się przez kontekst. |
| `void GameplayReady()` | Krok 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Krok 6, dla każdej zadeklarowanej operacji (bez prefiksu `module.<id>.`). Dla typowanej awarii należy zgłosić `ModuleOperationException(code, message)`. |
| `void Stop()` | Krok 7. |

`IRuntimeModuleContext` oferuje `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (token anulowania), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess`, a od protokołu 3 `Runtime` (zob. [operacje w procesie](#operations-in-process)), `Content` (zob. [zawartość społeczności](#community-content)), `Input` (zob. [przechwytywanie wejścia](#input-capture)) i `NativeComponent` (wczytany komponent natywny lub null). Komponent zarządzany, który chce otrzymywać ticki, implementuje także `ITickingModule`. Nic w kontekście nie ujawnia adresów, skrótu pliku wykonywalnego ani układu jego kodu. Komponent musi dać się wczytać przez 32-bitową wtyczkę (AnyCPU lub x86, .NET 6); kopia `OmsiLaunch.Runtime.Modules` lub `OmsiLaunch.Api` obok niego jest ignorowana. Należy przypiąć każdy plik wczytywany przez programy modułu (dla programu .NET: jego `.dll` obok `.exe`), aby integralność je obejmowała.

<a id="known-limitations"></a>
## Znane ograniczenia

- Komponenty natywne mogą hookować importy `Omsi.exe` i metody urządzenia Direct3D; hooki na prologach funkcji OMSI i miejscach wywołań nie są jeszcze oferowane modułom.
- Przejęcie menu pauzy (`ui.pause.*`) i przechwytywanie wejścia są zaprojektowane statycznie i zweryfikowane offline; nie zostały zaobserwowane w rzeczywistej sesji OMSI, a `ui.pause-menu.replace` jest rozstrzygane tylko dla znanej tożsamości kodu (niektóre jego pola nie mają wyprowadzenia niezależnego od buildu).
- Sprawdzenie powiązania zawartości w procesie używa mapy zażądanej przez start (NEW_MAP); dla zapisanej sytuacji powiązanie jest sprawdzane podczas planowania względem mapy tej sytuacji.
- Hooki urządzenia są hookami vtable. Jeśli inny komponent zapisuje z powrotem oryginalne metody urządzenia (zaobserwowano to w instalacji regresyjnej krótko po rozpoczęciu rozgrywki), hook urządzenia przestaje być wywoływany i nie można go usunąć; czyszczenie zgłasza wtedy `OL_E_MODULE_CLEANUP_INCOMPLETE`, a komponent pozostaje wczytany. Metoda urządzenia już zahookowana przez coś spoza rejestru jest odrzucana (4); zdarzenie odmowy wskazuje moduł będący właścicielem tej metody.
- Moduły są zatrzymywane przy normalnym zakończeniu OMSI; zatrzymanie sesji kończy OMSI, więc czyszczenie modułów wewnątrz sesji odbywa się wyłącznie przez politykę awarii.
- Powiadomienia są wyświetlane dopiero wtedy, gdy ikona w obszarze powiadomień istnieje, po uruchomieniu OMSI; program sesji pokazuje własne okno dla wszystkiego, co musi powiedzieć wcześniej.
