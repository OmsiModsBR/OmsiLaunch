# Moduły runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=faf61b29436d1f69013b0c49667534ef079e31f7b34300b538935788654a55df -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/runtime-modules.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Moduł runtime dodaje zachowanie do zarządzanej sesji OMSI: funkcję działającą razem z sesją (na przykład HUD mapy lub optymalizacje wczytywania), która nie staje się częścią samego OmsiLaunch. Mostek wczytuje moduł, udostępnia mu wyłącznie to, co moduł zadeklarował, i usuwa wszystko, co moduł przechowuje, gdy sesja się kończy lub moduł ulega awarii. Ta strona jest normatywnym opisem modułów runtime w postaci zaimplementowanej w `src/OmsiLaunch.Runtime.Modules` (manifest, ustawienia, kwalifikowanie, host cyklu życia), w planerze, w programach sesji kontrolera (`ModuleSessionPrograms`, `ModuleSessionProcess`), w środowisku modułów wtyczki oraz we flagach modułów w [CLI](cli.md).

Stabilność: `EXPERIMENTAL`. Protokół mostka 1 został zweryfikowany w runtime w rundzie B (moduły zarządzane, aktywacja zawartości, operacje, `disable-module` i `fail-session`; komponenty natywne z hookami IAT; zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md)). Protokół mostka 2 (ustawienia, aktywacja domyślna, plik ustawień, programy sesji, konflikty wtyczek, powiadomienia, `get_value`) jest zweryfikowany tylko offline. Bramki offline: bramki `module.*` z `tests/OmsiLaunch.ModuleTests` (manifest, integralność, wybór i aktywacja, ustawienia, plik ustawień, konflikty wtyczek, programy sesji uruchamiane przez swoje zadanie, awaria wstrzyknięta w każdej fazie przy każdej polityce awarii, cykl życia komponentu natywnego, czyszczenie, przekazanie uruchomieniowe), `plugin-runtime.modules-lifecycle` (rzeczywisty moduł zarządzany wczytany przez środowisko wtyczki), `runtime-modules.native-component` (rzeczywisty komponent natywny hookujący import i odczytujący wartości przez mostek), `runtime-modules.plugin-conflict` oraz `native-hooks.self-check` (silnik hooków).

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
| `bridge_protocol` | tak | `{ "min": n, "max": n }`: protokoły mostka, z którymi moduł działa. Ten build obsługuje protokoły `1` i `2`; moduł jest akceptowany, gdy jego zakres obejmuje jeden z nich. |
| `activation` | nie | `requested` (domyślnie): wczytywany, gdy sesja o to prosi. `content`: wczytywany także wtedy, gdy sesja uruchamia powiązaną mapę (wymaga `content_binding`). `default` (protokół 2): wczytywany w każdej sesji, chyba że jest wyłączony (zob. [wybór modułów](#choosing-modules-for-a-session)); nie może mieć `content_binding`. |
| `required_capabilities` | nie | Możliwości, bez których moduł nie może działać (zob. [możliwości](#capabilities)). |
| `optional_capabilities` | nie | Możliwości, których moduł używa, gdy są dostępne; sprawdza je przez `IsCapabilityAvailable`. |
| `content_binding` | nie | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: moduł działa tylko na tej mapie, z tymi plikami w stanie niezmienionym. |
| `assets` | nie | Pliki, które moduł odczytuje, każdy jako `{ "path", "sha256" }`. |
| `components` | tak | Dokładnie jeden komponent `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; opcjonalnie jeden komponent `native` (`.dll`), komponenty `auxiliary` (`.exe`) i, w protokole 2, jeden komponent `host` (`.exe`, [program sesji](#session-programs) modułu). |
| `failure_policy` | tak | `fail-session`, `disable-module` lub `degrade` (zob. [awarie](#failures)). |
| `operations` | nie | Operacje, które moduł udostępnia, gdy sesja działa: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | nie | Protokół 2. Ustawienia akceptowane przez moduł (zob. [ustawienia](#settings)). |
| `conflicts` | nie | Protokół 2. `{ "plugins": [ "<file>.dll" ] }`: pliki DLL wtyczek OMSI, które nie mogą działać obok modułu (zob. [konflikty](#conflicts-with-omsi-plugins)). |
| `timeouts` | nie | Budżety w milisekundach: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (domyślnie po 5000, najwyżej 30000); w protokole 2 także `prepare_ms` (domyślnie 120000, najwyżej 1800000) i `host_stop_ms` (domyślnie 10000, najwyżej 60000) dla programu sesji. Operacje domyślnie mają 2000 ms. |
| `diagnostics` | nie | `{ "trace": true\|false }`. |

Manifest, który używa funkcji protokołu 2 (`settings`, `conflicts`, aktywacja `default`, komponent `host`, `prepare_ms` lub `host_stop_ms`), musi deklarować `bridge_protocol.min` równe 2.

Każdy plik wymieniony w `components` i `assets` jest przypięty swoim SHA-256. `module.json` nie może przypiąć samego siebie.

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

Każdy przypięty plik musi istnieć, nie może być osiągany przez punkt połączenia (junction) ani dowiązanie symboliczne i musi mieć przypięty SHA-256; w przeciwnym razie moduł jest odrzucany z kodem `OL_E_MODULE_INTEGRITY`. Dzięki temu wykrywany jest moduł uszkodzony lub zaktualizowany do połowy. Nie jest to decyzja o zaufaniu: moduł nie jest sprawdzany względem żadnej listy, podpisu ani wydawcy.

Sprawdzenie jest powtarzane tam, gdzie startuje kod modułu: wtyczka wczytuje moduł tylko wtedy, gdy jego `module.json` nadal ma SHA-256, który ocenił plan, i każde przypięcie nadal jest spełnione, a kontroler uruchamia program sesji dopiero po zweryfikowaniu przypięcia tego programu.

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
| 6. Runtime | wtyczka | Operacje `module.<id>.<operation>` trafiają do `Execute`. |
| 7. Zatrzymanie | wtyczka, następnie kontroler | Gdy sesja się kończy (lub moduł ulega awarii), wywoływane jest `Stop()`. Po zakończeniu OMSI programy sesji są zatrzymywane. |
| 8. Czyszczenie | wtyczka | Wszystko, co moduł śledził, jest zwalniane w odwrotnej kolejności i weryfikowane jako usunięte; komponent natywny jest zatrzymywany jako ostatni, a jego hooki są usuwane; jego DLL pozostaje wczytany, nieaktywny, do zakończenia OMSI (wątek może jeszcze wracać przez detour). |

Każde wywołanie zwrotne w procesie działa w wątku interfejsu użytkownika OMSI, tak jak własne operacje runtime produktu. Mostek mierzy każde wywołanie zwrotne względem jego budżetu: wywołanie, które wraca zbyt późno, kończy swoją fazę niepowodzeniem (`OL_E_MODULE_PHASE_TIMEOUT`). Mostek nie może przerwać wywołania, które nigdy nie wraca; taki moduł zatrzymuje OMSI, a sesja kończy się z upływem limitu czasu uruchamiania lub przez `session stop`, jak każda inna sesja.

<a id="failures"></a>
## Awarie

Awarią jest wyjątek lub przekroczenie budżetu w dowolnej fazie lub operacji, możliwość nierozstrzygnięta w procesie, powiązanie zawartości, które nie jest spełnione w procesie, konfliktująca wtyczka wczytana w OMSI albo program sesji, który nie staje się gotowy. To, co następuje dalej, rozstrzyga `failure_policy` modułu. W każdym przypadku zasoby modułu są zwalniane.

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

Tabela mostka daje komponentowi `request_hook`, `emit` (zdarzenia runtime `module.<id>.<name>` z obiektem JSON złożonym z ciągów znaków) oraz, od protokołu 2, `get_value`. Komponent odczytuje `size` tabeli przed użyciem `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` kopiuje wartość jako UTF-8 z kończącym zerem i ustawia `*length` na liczbę potrzebnych bajtów bez niego; zwraca `0`, `1` (argument), `2` (nieznana nazwa), `11` (po zatrzymaniu) lub `12` (bufor za mały). Nazwy:

| Nazwa | Wartość |
| --- | --- |
| `setting.<key>` | Efektywna wartość zadeklarowanego ustawienia. |
| `data_directory` | [Katalog danych](#data-directory) modułu. |
| `module_directory` | Własny katalog modułu. |
| `session_id` | Identyfikator sesji. |
| `module_version` | Wersja z manifestu. |
| `product_version` | Wersja produktu OmsiLaunch. |
| `session_channel` | Potok [programu sesji](#session-programs); nieznany dla modułu bez takiego programu. |

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

<a id="writing-a-module-contracts"></a>
## Pisanie modułu (kontrakty)

Komponent zarządzany odwołuje się do `OmsiLaunch.Runtime.Modules` i `OmsiLaunch.Api` oraz implementuje `IRuntimeModule`. Mostek wiąże te dwa odwołania z własnymi wczytanymi zestawami (w jakimkolwiek kontekście wczytywania działa wtyczka), więc kopia dostarczona z modułem jest ignorowana:

| Składowa | Wywoływana |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Krok 4. Zasoby zdobywa się przez kontekst. |
| `void GameplayReady()` | Krok 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Krok 6, dla każdej zadeklarowanej operacji (bez prefiksu `module.<id>.`). Dla typowanej awarii należy zgłosić `ModuleOperationException(code, message)`. |
| `void Stop()` | Krok 7. |

`IRuntimeModuleContext` oferuje `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `SessionChannelName`, `Stopping` (token anulowania), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground` i `StartAuxiliaryProcess`. Nic w nim nie ujawnia adresów, pliku wykonywalnego ani jego tożsamości. Komponent musi dać się wczytać przez 32-bitową wtyczkę (AnyCPU lub x86, .NET 6). Należy przypiąć każdy plik wczytywany przez programy modułu (dla programu .NET: jego `.dll` obok `.exe`), aby integralność je obejmowała.

<a id="known-limitations"></a>
## Znane ograniczenia

- Komponenty natywne mogą hookować importy `Omsi.exe` i metody urządzenia Direct3D; hooki na prologach funkcji OMSI i miejscach wywołań nie są jeszcze oferowane modułom.
- Sprawdzenie powiązania zawartości w procesie używa mapy zażądanej przez start (NEW_MAP); dla zapisanej sytuacji powiązanie jest sprawdzane podczas planowania względem mapy tej sytuacji.
- Hooki urządzenia są hookami vtable. Jeśli inny komponent zapisuje z powrotem oryginalne metody urządzenia (zaobserwowano to w instalacji regresyjnej krótko po rozpoczęciu rozgrywki), hook urządzenia przestaje być wywoływany i nie można go usunąć; czyszczenie zgłasza wtedy `OL_E_MODULE_CLEANUP_INCOMPLETE`, a komponent pozostaje wczytany. Metoda urządzenia już zahookowana przez coś spoza rejestru jest odrzucana (4); zdarzenie odmowy wskazuje moduł będący właścicielem tej metody.
- Moduły są zatrzymywane przy normalnym zakończeniu OMSI; zatrzymanie sesji kończy OMSI, więc czyszczenie modułów wewnątrz sesji odbywa się wyłącznie przez politykę awarii.
- Powiadomienia są wyświetlane dopiero wtedy, gdy ikona w obszarze powiadomień istnieje, po uruchomieniu OMSI; program sesji pokazuje własne okno dla wszystkiego, co musi powiedzieć wcześniej.
