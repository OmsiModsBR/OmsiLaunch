<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Sterowanie sesjami OMSI 2.</strong></p>
<p align="center">Otwarte źródło · Programowalność · Rozwijany przez społeczność</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <strong>Polski</strong> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> To jest tłumaczenie kanonicznego [README w języku angielskim (US)](../../README.md). W razie rozbieżności wiążące jest README w języku angielskim (US).

# OmsiLaunch

**OmsiLaunch** to warstwa o otwartym kodzie źródłowym do programowego uruchamiania
OMSI 2, zarządzania sesjami i sterowania działającą symulacją. Planuje sesję
na podstawie deklaratywnego opisu, stosuje każdą tymczasową zmianę konfiguracji
w ramach transakcji zapisywanej w dzienniku, uruchamia OMSI, obserwuje je aż do
rozpoczęcia rozgrywki, pozwala narzędziom odczytywać i zmieniać działającą
symulację przez publiczne API, a po zakończeniu sesji przywraca każdy plik,
który zmieniło.

Jest to infrastruktura dla launcherów, narzędzi, automatyzacji i integracji
społecznościowych. Nie jest to graficzny launcher.

> **Zdefiniuj sesję, nie kliknięcia.**

## Stan: 0.1.0-beta3

Bieżąca publiczna beta to **`0.1.0-beta3`**. Beta 3 jest stanem bazowym po etapie
utwardzania (post-hardening). Większość jej funkcji ma status `RUNTIME_VALIDATED`:
zaobserwowano je w działających sesjach OMSI, w tym w rundzie domykającej
weryfikację runtime z 2026-09-23. Niektóre pozostają `STATICALLY_VALIDATED`
(tylko testy offline), `PARTIAL` lub `UNAVAILABLE`. Dwa elementy runtime są
nadal otwarte, ponieważ nie da się ich bezpiecznie wywołać: rozgrywka w Steam
LAA oraz naturalne usuwanie pojazdów drogowych i postaci (RV-002). Strona
[stan weryfikacji runtime](../localized/pl-PL/status/runtime-validation-status.md) jest
wiążącym zapisem tego, co zostało uruchomione pod OMSI, a co działało tylko offline.

To jest beta: publiczne API, CLI i formaty plików są oznaczone dla każdej składowej
jako `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` lub `UNAVAILABLE`
i mogą się jeszcze zmienić przed wersją 1.0.

## Obsługiwany zakres OMSI

OmsiLaunch obsługuje dokładnie jeden build OMSI 2 i odmawia uruchomienia
czegokolwiek, czego nie rozpoznaje.

| Element | Zakres |
| --- | --- |
| Build OMSI | Profil `Omsi23004_692EBFBF`: `Omsi.exe` z SHA-256 `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` (OMSI 2.3.004). `STABLE_BETA`; każda weryfikacja runtime została przeprowadzona na tym pliku. |
| Plik wykonywalny Steam LAA | SHA-256 `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` jest akceptowany na podstawie listy dozwolonych. Zweryfikowano wyłącznie odcisk (fingerprint) i planowanie; rozgrywka **nie** została zweryfikowana w runtime. `PARTIAL`. |
| Nieznane buildy | Odrzucane z kodem `OL_E_UNSUPPORTED_BUILD`. Hash jest sprawdzany ponownie przy każdym planowaniu i przy każdym uruchomieniu. |
| System operacyjny | Windows 10 lub nowszy, x64. |
| Środowiska uruchomieniowe | .NET 6 Desktop Runtime **x64** (kontroler) i .NET 6 Runtime **x86** (wtyczka działa wewnątrz 32-bitowego `Omsi.exe`). |

Szczegóły: [Zgodność](../localized/pl-PL/reference/compatibility.md) i
[Instalacja](../localized/pl-PL/getting-started/installation.md).

## Co zapewnia

**Sesje.** Sesja jest planowana na podstawie `LaunchSpec` (flagi CLI, plik JSON,
profil sesji lub API), weryfikowana bez skutków ubocznych (`/plan`), a następnie
uruchamiana, obserwowana poprzez przejścia `SessionState` i kończona. Zatrzymanie
sesji wymusza zakończenie OMSI, dzięki czemu OMSI nie może nadpisać plików, które
mają zostać przywrócone. Zob. [Cykl życia sesji](../localized/pl-PL/concepts/session-lifecycle.md).

**Transakcje i odzyskiwanie.** Każde nadpisanie konfiguracji jest ograniczone do
sesji. OmsiLaunch tworzy migawkę, zapisuje w dzienniku, stosuje, weryfikuje
i przywraca każdy zmieniany plik, także po awarii (`/recovery-status`, `/recover`,
`RecoverPendingAsync`). Nie oferuje trwałej edycji konfiguracji. Zob.
[Transakcje i odzyskiwanie](../localized/pl-PL/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Referencyjny frontend oparty na tym samym publicznym API,
bez własnej logiki OMSI: wykrywanie, planowanie, uruchamianie sesji oraz polecenia
klienta wobec działającej sesji. Zob. [Dokumentację CLI](../localized/pl-PL/reference/cli.md)
i [Przykłady CLI](../localized/pl-PL/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** Host podsystemu Windows przeznaczony dla skrótów. Przyjmuje
ten sam wiersz poleceń bez okna konsoli i zgłasza błędy w oknach komunikatów.
Zob. [OmsiLaunchW.exe](../localized/pl-PL/reference/omsilaunchw.md).

**Obszar powiadomień Windows.** Każda sesja właściciela wyświetla ikonę w obszarze
powiadomień z oknem stanu i akcją „End session” (Zakończ sesję). Akcja korzysta
z tej samej ścieżki zatrzymania co `session stop`. Zob. [Obszar powiadomień Windows](../localized/pl-PL/reference/windows-tray.md).

**Profile sesji.** Deklaratywne pakiety `profile.yaml`
(`omsilaunch.session-profile/v1`) w
`.omsilaunch\session-profiles\<id>\`, dzięki którym autorzy treści mogą
dostarczać powtarzalne sesje uruchamiane jednym poleceniem. Zob.
[Profile sesji](../localized/pl-PL/reference/session-profiles.md).

**Publiczne API i sterowanie runtime.** `OmsiLaunch.Api` (`IOmsiLaunch`) jest
preferowaną powierzchnią produktu. Operacje runtime, takie jak czas, pogoda, mapa,
kamera, rozkład jazdy, pojazdy, postacie, zmienne skryptowe i tekstury D3D, są
ograniczone do sesji, weryfikowane względem profilu buildu i adresowane przez
nieprzezroczyste uchwyty semantyczne, nigdy przez natywne wskaźniki. Wyniki są
ograniczone slotem runtime o rozmiarze 64 KiB. Zob. [Dokumentację publicznego API](../localized/pl-PL/reference/public-api.md) i
[Sterowanie runtime](../localized/pl-PL/reference/runtime-control.md).

**Lokalne sterowanie.** Potok nazwany (named pipe) tworzony dla każdej instalacji
i powiązany z aktywnym `session_id` pozwala innym procesom tego samego użytkownika
odczytywać stan i zdarzenia, zatrzymywać sesję i wykonywać publiczne operacje
runtime. Zob.
[Lokalne sterowanie / IPC](../localized/pl-PL/reference/local-control.md).

**Możliwości.** Każda możliwość (capability) i każda publiczna operacja runtime
jest skatalogowana wraz ze swoją stabilnością. Możliwości eksperymentalne
i niedostępne są wymienione, a nie ukryte (`OmsiLaunch.exe capabilities`). Zob.
[Możliwości](../localized/pl-PL/reference/capabilities.md).

**Stała wtyczka.** Zestaw plików wtyczki (closure) działającej w procesie jest
instalowany jednorazowo w `plugins\OmsiLaunch.*`. Przed każdym uruchomieniem jest
sprawdzany względem wpisów SHA-256 z `release-manifest.json`. Wtyczki firm
trzecich nigdy nie są modyfikowane. Zob.
[Model stałej wtyczki](../localized/pl-PL/concepts/permanent-plugin.md).

## Szybki start

Należy rozpakować pakiet wydania do katalogu głównego instalacji OMSI 2, a następnie
uruchomić z tego katalogu następujące polecenia:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Podczas działania tej sesji druga konsola w tym samym katalogu może odpytać ją
o stan lub ją zakończyć:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Przewodnik: [Pierwsza sesja](../localized/pl-PL/getting-started/first-session.md). Dla integratorów
.NET: [Szybki start z publicznym API](../localized/pl-PL/getting-started/api-quick-start.md).

## Dokumentacja

Pełny zestaw dokumentacji jest zindeksowany w [`docs/localized/pl-PL/README.md`](../localized/pl-PL/README.md).
Strony w języku angielskim (US) w `docs/` są kanoniczne i normatywne.

| Temat | Strona |
| --- | --- |
| Publiczne API | [docs/localized/pl-PL/reference/public-api.md](../localized/pl-PL/reference/public-api.md) |
| CLI | [docs/localized/pl-PL/reference/cli.md](../localized/pl-PL/reference/cli.md) |
| Przykłady CLI | [docs/localized/pl-PL/reference/cli-examples.md](../localized/pl-PL/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/pl-PL/reference/omsilaunchw.md](../localized/pl-PL/reference/omsilaunchw.md) |
| Obszar powiadomień Windows | [docs/localized/pl-PL/reference/windows-tray.md](../localized/pl-PL/reference/windows-tray.md) |
| Profile sesji | [docs/localized/pl-PL/reference/session-profiles.md](../localized/pl-PL/reference/session-profiles.md) |
| Sterowanie runtime | [docs/localized/pl-PL/reference/runtime-control.md](../localized/pl-PL/reference/runtime-control.md) |
| Lokalne sterowanie / IPC | [docs/localized/pl-PL/reference/local-control.md](../localized/pl-PL/reference/local-control.md) |
| Możliwości | [docs/localized/pl-PL/reference/capabilities.md](../localized/pl-PL/reference/capabilities.md) |
| Błędy i kody wyjścia | [docs/localized/pl-PL/reference/errors.md](../localized/pl-PL/reference/errors.md), [docs/localized/pl-PL/reference/exit-codes.md](../localized/pl-PL/reference/exit-codes.md) |
| Pakowanie | [docs/localized/pl-PL/reference/packaging.md](../localized/pl-PL/reference/packaging.md) |
| Znane ograniczenia | [docs/localized/pl-PL/reference/known-limitations.md](../localized/pl-PL/reference/known-limitations.md) |
| Stan weryfikacji runtime | [docs/localized/pl-PL/status/runtime-validation-status.md](../localized/pl-PL/status/runtime-validation-status.md) |

### Dokumentacja w innych językach

Dokumentacja jest przetłumaczona na 14 wersji językowych w
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). Tłumaczenia
powstały na podstawie kanonicznych stron w języku angielskim (US) i zostały
z nimi zweryfikowane mechanicznie. Redakcja przez rodzimych użytkowników języka
nie była częścią wydania Beta 3 i może nastąpić po publikacji. Gdy tłumaczenie
i strona angielska są niezgodne, wiążąca jest strona angielska.

## Ograniczenia i otwarte kwestie

Poniżej wymieniono najważniejsze ograniczenia. Pełna lista znajduje się w
[Znanych ograniczeniach](../localized/pl-PL/reference/known-limitations.md).

- **Jeden build OMSI.** Steam LAA ma status `PARTIAL`: rozgrywka, odczyty, polecenia
  i zatrzymanie wymagają prawdziwej instalacji Steam i nie zostały zweryfikowane w runtime.
- **Niedostępne w tej becie:** uruchamianie od ostatniego stanu mapy (`/last`),
  jawne ustawienie daty, godziny, roku lub pogody przy uruchomieniu oraz przypisanie
  pojazdu gracza przy uruchomieniu. Zażądanie któregokolwiek z nich sprawia, że plan
  jest niemożliwy do uruchomienia, zamiast być po cichu ignorowanym.
- **Zapisy runtime są ograniczone.** `weather.set`, zapisy kalendarza, zapisy
  zmiennych tekstowych i przenoszenie pojazdów są niedostępne. Zmiany runtime nie są
  zapisywane w dzienniku i nie są przywracane.
- **Czas życia uchwytów.** Wykrywanie nieaktualnych uchwytów pojazdów drogowych
  i postaci, które znikają w naturalny sposób (RV-002), nie ma bezpiecznego źródła
  w runtime i jest zweryfikowane tylko offline.
- **Ograniczone wyniki.** Długie listy są obcinane (`truncated=true`). Brak
  stronicowania.
- **Zatrzymanie jest wymuszone.** Własna procedura zamykania OMSI nie jest wykonywana,
  a niezapisany stan OMSI zostaje utracony.
- **Model zaufania oparty na tym samym użytkowniku.** Każdy proces tego samego
  użytkownika Windows ma dostęp do lokalnej płaszczyzny sterowania.

## Pobieranie

Należy pobrać **`OmsiLaunch-0.1.0-beta3.zip`** wraz z plikiem `.sha256` ze
[strony wydań](https://github.com/lmonteirotech/OmsiLaunch/releases) i rozpakować
go bezpośrednio do obsługiwanego katalogu głównego OMSI. Pakiet zawiera kontroler
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), jego zależności, zestaw plików stałej wtyczki
wraz z `release-manifest.json`, zasoby ekranu startowego, przykładową sesję oraz
dokumentację offline w `.omsilaunch\docs\`. Układ pakietu i czyste
usuwanie: [Pakowanie](../localized/pl-PL/reference/packaging.md).

## Kompilacja ze źródeł

Wymagania:

- Windows 10 lub nowszy, x64.
- .NET 6 SDK wraz ze środowiskami uruchomieniowymi .NET 6 x64 i x86 do uruchamiania testów.
- Visual Studio z obciążeniem MSBuild C++ (zestaw narzędzi platformy `v145`) oraz
  Windows 10 SDK dla trzech projektów natywnych: `OmsiLaunch.Native.x86`
  (Win32) oraz `OmsiLaunch.Bootstrapper` i `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` zawiera projekty zarządzane i zestawy testów. Jedyny punkt
wejścia offline kompiluje wszystko i uruchamia każdy zestaw testów offline;
nigdy nie uruchamia OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` pomija projekty natywne i test regresji pakowania.
`-SkipDocs` pomija bramki dokumentacji. Wyniki kompilacji trafiają do `artifacts\`,
który nie jest śledzony. `tools\New-ReleasePackage.ps1` przygotowuje, oblicza hashe,
weryfikuje i kompresuje pakiet wydania z istniejącej kompilacji. Zob.
[Pakowanie](../localized/pl-PL/reference/packaging.md) oraz
[Testowanie i weryfikacja](../project/TESTING-AND-VALIDATION.md).

| Ścieżka | Zawartość |
| --- | --- |
| `src/` | Biblioteki produktu: API, rdzeń, konfiguracja, treści, interop, proces, wtyczka, profil buildu, natywna granica x86 |
| `tools/` | CLI i host Windows (`OmsiLaunch.Cli`), natywne shimy (`OmsiLaunch.Bootstrapper`), testowy host offline, skrypty pakowania i weryfikacji, narzędzia lokalizacyjne |
| `tests/` | Zestawy testów jednostkowych, integracyjnych, profili, interfejsu Windows i dokumentacji |
| `docs/` | Kanoniczna dokumentacja i jej tłumaczenia w `docs/localized/` |
| `examples/` | Przykłady LaunchSpec i sesji |
| `assets/` | Branding, ikony i zasoby pakietu |
| `third_party/` | Informacje o pochodzeniu kodu zewnętrznego |

Podsumowania dla opiekunów: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Społeczność i licencja

OmsiLaunch jest projektem open source zorientowanym na społeczność. Jest niezależny
od innych launcherów OMSI, a zgodne narzędzia społecznościowe mogą na nim bazować.

OmsiLaunch jest udostępniany na licencji [LGPL-3.0-only](../../LICENSE). Informacje
o pochodzeniu włączonego kodu źródłowego i obowiązujące noty znajdują się w
[informacjach o komponentach firm trzecich](../../THIRD-PARTY-NOTICES.md).
