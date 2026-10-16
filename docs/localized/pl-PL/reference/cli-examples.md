# Przykłady CLI

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/cli-examples.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Minimalne, poprawne wywołania `OmsiLaunch.exe` dla OmsiLaunch `0.1.0-beta4`, każde z oczekiwanym kodem wyjścia procesu oraz informacją o tym, co jest zmieniane i przywracane. Każdy przykład jest uruchamiany z katalogu głównego instalacji OMSI (`<OMSI_PATH>`), o ile nie zaznaczono inaczej; składnię zdefiniowano w [dokumentacji CLI](cli.md), a kody wyjścia w [kodach wyjścia](exit-codes.md). Do każdego polecenia można dodać `--json`, aby otrzymać strukturalną kopertę (envelope).

<a id="conventions"></a>
## Konwencje

- **Zmienia**: pliki lub stan OMSI zmieniane przez polecenie. „Nakładka sesji” oznacza plik zapisany w migawce transakcji, zastosowany przed uruchomieniem OMSI i przywrócony bajt po bajcie po zakończeniu sesji.
- **Przywracane**: to, co jest cofane po zakończeniu sesji (zwykłe zatrzymanie, Ctrl+C, obszar powiadomień, `session stop`, `/observe-seconds`) lub przez odzyskiwanie.
- Zapisy runtime (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) zmieniają wyłącznie pamięć OMSI; nigdy nie są cofane, ponieważ OMSI jest kończony przy zatrzymaniu.
- Symbole zastępcze: `<OMSI_PATH>` to instalacja OMSI 2 zawierająca pakiet OmsiLaunch (na przykład `C:\OMSI 2`); `<OTHER_OMSI_PATH>` – inna instalacja; `<SPEC_PATH>` i `<ITX_PATH>` – własny plik LaunchSpec i własny profil tekstur internetowych; `<HANDLE>` to uchwyt wypisany przez poprzedzające polecenie `list` lub `create`, a `<BASE64>` to dane pikseli w Base64. Każda inna wartość jest literałem, który działa w standardowej instalacji OMSI 2 (`grundorf-quick` to przykładowy profil zdefiniowany na tej stronie).
- Ścieżkę zawierającą spacje należy ująć w cudzysłów i nie kończyć ścieżki w cudzysłowie znakiem `\` (parsowanie argumentów w Windows zamienia `\"` w dosłowny cudzysłów): `"C:\OMSI 2"`, a nie `"C:\OMSI 2\"`.
- Każdy wiersz polecenia na tej stronie jest parsowany przez bramkę dokumentacji (`tests/OmsiLaunch.DocumentationTests`, bramka `examples`); przykłady identyfikacji, wykrywania, planowania i trybu klienta zostały ponadto wykonane na rzeczywistej instalacji (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identyfikacja i wykrywanie (bez sesji)

```text
OmsiLaunch.exe /version
```
Kod wyjścia `0`. Wypisuje `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. Niczego nie zmienia.

```text
OmsiLaunch.exe profiles --json
```
Kod wyjścia `0`. Wyświetla obsługiwane hashe `Omsi.exe` i ich stan walidacji. Niczego nie zmienia.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Kod wyjścia `0`. Katalog publicznych możliwości; `help <family>` go filtruje. Niczego nie zmienia.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Kod wyjścia `0` (obie postacie są identyczne). Raportuje uruchomione procesy `Omsi.exe` oraz to, czy właściciel OmsiLaunch odpowiada dla tej instalacji. Niczego nie zmienia.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Kod wyjścia `0` (`2` dla nieznanej kategorii). Wykrywanie tylko do odczytu; cykle punktów połączenia (junction) są pomijane. Niczego nie zmienia. Wypisywane tu wartości `Identity` są dokładnie tymi ciągami, których oczekują `/map`, `/saved`, `/vehicle-scope` i `LaunchSpec` (na przykład `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Planowanie i walidacja

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Kod wyjścia `0`, gdy plan ma stan `READY`, `1`, gdy `NOT RUNNABLE` (na przykład `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Niczego nie zmienia; OMSI nie jest uruchamiany.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Kod wyjścia `0`/`1` jak wyżej; `/validate` jest aliasem `/plan`. JSON to surowy `SessionPlan` (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Kod wyjścia `1`. `/date`, `/time`, `/year`, `/weather*` i flagi pojazdu gracza są akceptowane, ale nie są stosowane przez ten build; plan zawiera `OL_E_CAPABILITY_UNAVAILABLE` i jest niemożliwy do uruchomienia.

```text
OmsiLaunch.exe /last /plan
```
Kod wyjścia `1`. `LAST_MAP_STATE` jest niedostępne dla tego profilu (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Uruchamianie sesji (tryb właściciela)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Kod wyjścia `0`, gdy sesja kończy się stanem `Completed`, `1` przy `Failed` lub planie niemożliwym do uruchomienia. Zmienia: nakładki sesji `GUI\NewSplashscreen_ENG.bmp` i `GUI\NewSplashscreen_<lang>.bmp` (zarządzany ekran startowy jest domyślny), obsługę `closecheck`, przekazanie startowe (handoff). Przywracane: każda nakładka, bajt po bajcie, po zakończeniu sesji. Konsola pozostaje podłączona, dopóki OMSI się nie zakończy, nie zostanie potwierdzone „End session” w obszarze powiadomień, klient nie wyśle `session stop` lub nie zostanie naciśnięte Ctrl+C.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Kod wyjścia `0`. Jak wyżej, ale sesja jest zatrzymywana 8 s po osiągnięciu `Running` (wcześniej przy zatrzymaniu z obszaru powiadomień/przez potok). Używane przez skrypty walidacyjne.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Kod wyjścia `0`/`1`. SAVED_SITUATION: mapa, czas i pozycja pochodzą z pliku `.osn` (`situations\Linie 5.osn` jest dostarczany z OMSI 2 i startuje na mapie Berlin-Spandau z autobusem gracza). Wartość jest tożsamością sytuacji wypisaną przez `/list:Situations` (względną wobec instalacji, bez rozróżniania wielkości liter); sama nazwa pliku, taka jak `Linie 5.osn`, nie zostaje rozwiązana (`OL_E_SITUATION_NOT_FOUND`, kod wyjścia `1`). `/map` lub `/entrypoint-index` razem z `/saved` są odrzucane z kodem wyjścia `2`. Zmiany i przywracanie jak dla NEW_MAP. OMSI sam zapisuje mapę sytuacji w `options.cfg` `[last_map]`; ten zapis OMSI nie jest cofany, chyba że `/set` nakłada `options.cfg` (patrz [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Kod wyjścia `0`. Zmienia: `options.cfg` (nakładka sesji, semantyczna poprawka tokenów/wektorów; bajty CP1252 zachowane) oraz nakładki ekranu startowego. Przywracane: `options.cfg` i pliki ekranu startowego, dokładnie (RV-005, RV-006). `/set:graphics.texture=...` kończy się kodem `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` kończy się kodem `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Kod wyjścia `0`. Zmienia: brak nakładki ekranu startowego; tylko przekazanie startowe i obsługa `closecheck`. Przywracane: dla ekranu startowego nie ma nic do przywrócenia.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Kod wyjścia `0` (`1` z `OL_E_SESSION_PRESENTATION_INVALID`, gdy brakuje katalogu lub pliku BMP albo plik nie ma formatu 640x480 24-bit). Zmienia: `GUI\NewSplashscreen_ENG.bmp` i `GUI\NewSplashscreen_PTB.bmp` z własnego katalogu (nakładka sesji). Przywracane: oba pliki, dokładnie.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Kod wyjścia `0`. Zmienia: nic na dysku poza nakładkami ekranu startowego; mechanizm pobierania działający w procesie jest wyłączony na czas sesji.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Kod wyjścia `0` (`2` z `OL_E_ITX_PROFILE_REQUIRED`, jeśli pominięto profil; `1` dla niepoprawnego profilu lub celu poza `Texture\`). Zmienia: `Texture\standard.itx` (nakładka sesji); każdy cel wymieniony w profilu oraz `Texture\standard.ipr` są usunięciami sesji. Przywracane: nakładka usunięta, usunięte oryginały przywrócone; pliki utworzone przez OMSI pod tymi ścieżkami w trakcie sesji są usuwane jako produkty uboczne sesji.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Kod wyjścia `0`. Czeka na `Running` do 300 s (+5 s) zamiast domyślnych 180 s. `/shutdown-timeout:60` jest akceptowane, ale w tym buildzie nie ma żadnego efektu.

<a id="predefined-session-profile"></a>
### Predefiniowany profil sesji

Plik profilu `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Kod wyjścia `0`. Zmienia: `options.cfg` (ustawienia z ustawienia wstępnego, nakładka sesji) i nakładki ekranu startowego. Przywracane: wszystkie. Dodanie `/map:...` lub `/set:graphics.maxFPS=60` kończy się kodem `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); pominięcie `/predefined-profile-index` kończy się kodem `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Dołączony do pakietu przykład `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` pokazuje pełny schemat, ale w dostarczonej postaci jego blok `new:` żąda `date`, `time` i `weather`, których ten build nie potrafi zastosować: zaplanowanie go z `/new` daje `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); przed użyciem należy usunąć te klucze.

<a id="launchspec-file"></a>
### Plik LaunchSpec

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Kod wyjścia `0`/`1`. Dołączony do pakietu przykład wybiera Grundorf, indeks punktu wejścia `1`, zarządzany ekran startowy i natywne tekstury internetowe; `RootPath: "."` jest rozwiązywane na katalog pliku wykonywalnego. Zmiany jak w jawnym przykładzie NEW_MAP. Specyfikacja z nieznaną właściwością kończy się kodem `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); brakujący plik – kodem `6` (`OL_E_SPEC_NOT_FOUND`); plik większy niż 1 MiB – kodem `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Kod wyjścia `0`/`1`. Jawna instalacja `<OTHER_OMSI_PATH>` ma pierwszeństwo przed `RootPath` ze specyfikacji; `/startup-timeout` nadpisuje `Behavior.StartupTimeoutSeconds` ze specyfikacji tylko dlatego, że zostało podane.

<a id="silent-detached-start"></a>
### Ciche (odłączone) uruchomienie

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Kod wyjścia `0`, gdy tylko `OmsiLaunchW.exe` zostanie uruchomiony (`{"delegated": true, "host_process_id": <pid>}`); `7`, jeśli brakuje `OmsiLaunchW.exe` (`OL_E_WINDOWS_HOST_MISSING`) lub nie udało się go uruchomić. Program uruchamiający wraca natychmiast i nie utrzymuje otwartej konsoli ani potoków wywołującego: skrypt przechwytujący jego wyjście od razu otrzymuje koniec pliku (domknięcie runtime `T04`). Sama sesja działa w `OmsiLaunchW.exe`: bez wyjścia na konsolę, błędy w oknach komunikatu, dostępna ikona w obszarze powiadomień. Postęp można sprawdzić za pomocą `session status`, `events watch` i `.omsilaunch\diagnostics\<sessionId>-host.log`. Pełna dokumentacja: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Sterowanie uruchomioną sesją (tryb klienta)

Poniższe polecenia należy uruchamiać z tego samego katalogu instalacji, gdy działa właściciel. Każde kończy się kodem `4` (`OL_E_NO_ACTIVE_SESSION`), gdy żaden właściciel nie odpowiada, i kodem `7` przy błędzie sterowania.

```text
OmsiLaunch.exe session status --json
```
Kod wyjścia `0`. Zwraca `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Niczego nie zmienia.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Kod wyjścia `0` (`events watch` działa do Ctrl+C). Ograniczona lista zdarzeń runtime (`gameplay.entered`, zdarzenia cyklu życia D3D, ...). Niczego nie zmienia.

```text
OmsiLaunch.exe session stop
```
Kod wyjścia `0` (`{"accepted": true, "session_id": "..."}`). Żąda kanonicznego zatrzymania: OMSI jest kończony, właściciel przywraca nakładki, dziennik jest usuwany. Klient wraca natychmiast; proces właściciela kończy się po przywróceniu.

<a id="runtime-reads"></a>
## Odczyty runtime

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Kod wyjścia `0` z `RuntimeCommandResult` (`Succeeded`, `Values`) w kopercie. Niczego nie zmienia. Limit czasu 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, kod wyjścia `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Kod wyjścia `0`; `2`, gdy brakuje wymaganego argumentu (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, zgłaszane przed wysłaniem żądania); `7`, gdy wtyczka odrzuca żądanie: `OL_E_RUNTIME_OPERATION_FAILED` z konkretną przyczyną w `Values.detail`, na przykład `OL_E_RUNTIME_OBJECT_HANDLE_STALE` dla uchwytu, który nie identyfikuje już tego samego obiektu, lub `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Uchwyty mają zasięg sesji i pochodzą z poprzedzającego `list`. Nazwy zmiennych, stałych i krzywych są definiowane przez każdy model pojazdu: należy je pobrać z wyniku `list`. Powyższe nazwy zostały wyświetlone dla `rv-000001`, autobusu gracza w sesji `situations\Linie 5.osn`. Niczego nie zmienia.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Kod wyjścia `0`. Operacje bez hierarchicznej ścieżki polecenia, a także dowolną ścieżkę, można zaadresować identyfikatorem operacji. `timetable.track-entries.list` jest listą ograniczoną: dla `situations\Linie 5.osn` zwróciła 137 z 825 wpisów z `truncated=true` (ponowny test runtime w audycie dokumentacji). Niczego nie zmienia.

<a id="runtime-writes"></a>
## Zapisy runtime

```text
OmsiLaunch.exe time set --minute=30
```
Kod wyjścia `0`. Zmienia zegar OMSI w pamięci (zweryfikowano: zapis, odczyt zwrotny, przywrócenie drugim `time set`). Nie jest cofane przy zatrzymaniu.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Kod wyjścia `0` (`2`, gdy dla `camera lock` brakuje `--family`). Zmienia stan kamery na czas sesji. `camera lock` wymaga PlayerVehicle (na przykład sesji `/saved`); w sesji `/new` bez pojazdu gracza kończy się błędem (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` w `Values.detail`). Blokada i odblokowanie zostały zweryfikowane w runtime z zapisaną sytuacją (rodziny 0, 2 i 1 z odczytem zwrotnym kamery). Nie jest cofane przy zatrzymaniu; zasada blokady kończy się wraz z sesją.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Kod wyjścia `0` (`2`, jeśli brakuje `handle`, `name` lub `value`). Zmienia jedną liczbową zmienną skryptu tego pojazdu. Nie jest cofane.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Kod wyjścia `0`. Zapisuje źródłowy rekord pogody OMSI i zatrzymuje bieżące aktualizacje METAR; OMSI wyprowadza z niego mgłę, światło, rzeczywisty wiatr i wilgotność przy następnym niewstrzymanym tyknięciu pogody. Każda wartość jest sprawdzana, zanim cokolwiek zostanie zapisane (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, kod wyjścia `7`). Klucz pochodny, taki jak `relative_humidity`, zwraca `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` i wskazuje klucz, który należy ustawić zamiast niego. Nie jest cofane. EXPERIMENTAL: weryfikacja w runtime w toku.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Kod wyjścia `0`. Przełącza na bieżącą pogodę METAR dla tej stacji; OMSI pobiera ją przy następnej aktualizacji pogody. EXPERIMENTAL: weryfikacja w runtime w toku.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Kod wyjścia `0`. `scripts string set` przechodzi przez własne `SetStringVariable` OMSI i zwraca odczytaną z powrotem wartość; nazwa, której skrypt pojazdu nie deklaruje, zwraca `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (kod wyjścia `7`). `scripts trigger` wywołuje wyzwalacz tak jak obsługa wejścia OMSI (`1` naciśnięcie, `0` zwolnienie); skrypt pojazdu wykonuje go od razu. Nazwy wyzwalaczy i zmiennych definiuje każdy model pojazdu; powyższe są przykładami. Nie jest cofane. EXPERIMENTAL: weryfikacja w runtime w toku.

<a id="spawn"></a>
## Dodawanie pojazdów

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Kod wyjścia `0` z nowym uchwytem `rv-NNNNNN` w `Values` (`2`, gdy brakuje `--model`; `7` przy `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Limit czasu klienta 30 s. Zmienia kolekcję pojazdów drogowych (dodawany jest jeden pojazd); nie przypisuje pojazdu gracza. Nie jest cofane; pojazd znika wraz z OMSI przy zatrzymaniu.

<a id="d3d-textures"></a>
## Tekstury D3D

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` to wartość `handle` wypisana przez `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` musi po zdekodowaniu dawać `width * height * 4` bajtów dla formatów 32-bitowych (8 x 8 x 4 = 256 bajtów) i najwyżej 48 KiB. Kod wyjścia `0`; `2` przy brakujących wymaganych argumentach; `7` przy `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (drugie zwolnienie lub describe po zwolnieniu), `OL_E_D3D_STALE_RESOURCE_HANDLE` (uchwyt sprzed resetu urządzenia lub z innej sesji), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Tworzy zasoby GPU należące do sesji; są one zwalniane jawnie lub po zakończeniu OMSI. Żadne pliki nie są modyfikowane.

<a id="owner-side-single-runtime-operation"></a>
## Pojedyncza operacja runtime po stronie właściciela

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Kod wyjścia `0`. Uruchamia sesję, wykonuje `time.read` jednokrotnie po osiągnięciu `Running` (limit czasu 5 s), zapisuje `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, działa przez 5 s, zatrzymuje się i przywraca pliki. Błąd runtime jest wypisywany jako `runtime_error` i nie kończy sesji.

<a id="recovery"></a>
## Odzyskiwanie

```text
OmsiLaunch.exe /recovery-status --json
```
Kod wyjścia `0`: `{"pending": false, ...}`, gdy dziennik nie istnieje, `{"pending": true, "recovered": false}`, gdy istnieje. Kod wyjścia `7` (`OL_E_INSTALLATION_BUSY`), dopóki właściciel utrzymuje instalację. Niczego nie zmienia.

```text
OmsiLaunch.exe /recover --json
```
Kod wyjścia `0`, gdy nic nie oczekiwało lub przywrócenie zostało ukończone (`recovered: true`; `diagnostics` może zawierać `restore.session-artifact-removed` i `OL_W_RESTORE_FOREIGN_FILE_RETAINED`); kod wyjścia `8`, gdy dziennik oczekiwał i nadal oczekuje (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); kod wyjścia `7`, dopóki właściciel lub zapisany w dzienniku proces OMSI utrzymuje instalację (`OL_E_INSTALLATION_BUSY`); kod wyjścia `10` (`OL_E_INTERNAL`), gdy przywrócone bajty nie przejdą weryfikacji (`Restore hash mismatch` lub `Restore presence mismatch`; dziennik pozostaje oczekujący). Zmienia: przywraca każdy zapisany w dzienniku plik z `.omsilaunch\backup\<sessionId>\` po zweryfikowaniu jego SHA-256, a następnie usuwa dziennik i katalog kopii zapasowej. Odmowa z `OL_E_INSTALLATION_BUSY`, dopóki zapisany w dzienniku `Omsi.exe` nadal działa (domknięcie runtime `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Szybkie sprawdzenie kodu wyjścia (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
