# Pierwsza sesja

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../getting-started/first-session.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona prowadzi przez pierwszą zarządzaną sesję OMSI z OmsiLaunch `0.1.0-beta4`: planowanie bez uruchamiania OMSI, uruchomienie z jawnymi flagami, uruchomienie z predefiniowanym profilem sesji, sterowanie sesją i jej zatrzymanie oraz odnalezienie komunikatów diagnostycznych po jej zakończeniu. Zakłada się, że pakiet został zainstalowany zgodnie z opisem na stronie [instalacja](installation.md). Każda flaga jest opisana w [dokumentacji CLI](../reference/cli.md); więcej wywołań zawiera strona [przykłady CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Co robi sesja

Sesja jest transakcją obejmującą jeden proces OMSI: OmsiLaunch wykonuje migawkę (snapshot) plików, które zmodyfikuje (domyślnie dwóch bitmap ekranu startowego w `GUI\`, a także `options.cfg`, gdy żądane są nakładki `/set`), zapisuje trwały dziennik w `.omsilaunch\`, nakłada nakładki (overlay), uruchamia `Omsi.exe` ze stałą wtyczką, czeka na wejście do rozgrywki (`Running`), utrzymuje możliwość sterowania sesją, a na końcu kończy OMSI i przywraca każdy zmodyfikowany plik bajt po bajcie. `/new` nigdy nie wybiera po cichu mapy ani punktu wejścia: oba muszą zostać podane albo pochodzić z pliku `/spec` lub profilu sesji.

<a id="1-plan-nothing-is-started"></a>
## 1. Planowanie (nic nie jest uruchamiane)

Polecenia należy uruchamiać z katalogu głównego OMSI; instalacja domyślnie oznacza katalog zawierający `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

Plan musi zawierać `"IsRunnable": true` (wyjście `0`). Wymienia `TouchedFiles` i `PlannedMutations`, dzięki czemu widać dokładnie, co sesja nałoży. Przed kontynuowaniem należy usunąć przyczynę każdego komunikatu diagnostycznego `OL_E_`; nic nie zostało jeszcze zapisane.

Dołączony do pakietu przykładowy plik spec robi to samo za pomocą pliku:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Uruchomienie z jawnymi flagami

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Co się dzieje, w kolejności:

1. Wypisywany jest plan (`Plan: READY profile=Omsi23004` w znanym układzie).
2. Odzyskanie ewentualnego starszego oczekującego dziennika, uzyskanie dzierżawy, migawka, dziennik, nakładki, sprawdzenie integralności wtyczki, uruchomienie `Omsi.exe`.
3. Pojawia się ikona w obszarze powiadomień (`OmsiLaunch is running` (`OmsiLaunch jest uruchomiony`)); zob. [obszar powiadomień Windows](../reference/windows-tray.md).
4. Po wejściu do rozgrywki stan `Running` jest wypisywany jako JSON (`"State": 14`). Domyślny limit czasu uruchamiania wynosi 180 s (zmienia się go za pomocą `/startup-timeout:<1..600>`).
5. Konsola pozostaje podłączona do czasu zakończenia sesji. Nie należy zamykać okna konsoli w celu zatrzymania: należy użyć jednej z metod zatrzymania opisanych poniżej.

Opcjonalne dodatki przy pierwszym uruchomieniu:

- `/set:graphics.maxFPS=60` (nakładka na `options.cfg`, przywracana na końcu);
- `/splash:Unset`, aby pozostawić ekran startowy OMSI bez zmian, lub `/splash-language:DEU`, aby wybrać zlokalizowany zarządzany ekran startowy;
- `/observe-seconds:30`, aby zatrzymać sesję automatycznie 30 s po osiągnięciu `Running` (przydatne w teście dymnym);
- `--json` dla ustrukturyzowanego wyjścia.

Flagi żądające daty, godziny, roku, pogody lub pojazdu gracza (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) są akceptowane, ale ten build nie może ich zastosować: plan staje się `NOT RUNNABLE` z `OL_E_CAPABILITY_UNAVAILABLE`. Należy je pominąć.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Uruchomienie z predefiniowanym profilem sesji

Profil sesji to plik YAML w `<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, który ustala mapę, punkt wejścia i maksymalnie 99 ustawień wstępnych (preset) z ustawieniami (schemat `omsilaunch.session-profile/v1`; pełny opis na stronie [profile sesji](../reference/session-profiles.md)). Należy utworzyć `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
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
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Następnie:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Reguły do zapamiętania: `id` musi być równe nazwie katalogu; indeks ma wartość `1..99`; jawne flagi, które nadpisałyby pole należące do profilu (`/map`, `/entrypoint-index`, klucz `/set` należący do ustawienia wstępnego, flagi ekranu startowego, gdy ustawienie wstępne ma `presentation`), są odrzucane z `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (wyjście `2`); blok `new:` ma zastosowanie tylko z `/new`; przy `/saved:<file.osn>` mapa sytuacji musi być wymieniona w `compatibility.maps`. Dołączony do pakietu `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` ilustruje pełny schemat, ale jego blok `new:` ustawia `date`, `time` i `weather`, których ten build nie może zastosować, dlatego można go skopiować dopiero po usunięciu tych kluczy.

<a id="4-control-the-running-session"></a>
## 4. Sterowanie działającą sesją

Z drugiej konsoli w tym samym katalogu (bez argumentu instalacji):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Polecenia te przechodzą przez lokalny potok sterowania tej instalacji ([lokalne sterowanie](../reference/local-control.md)); wyjście `4` oznacza, że w tej instalacji nie działa żaden właściciel.

<a id="5-stop"></a>
## 5. Zatrzymanie

Każda z tych metod kończy sesję w ten sam sposób (OMSI jest kończony, następnie przywracany jest każdy zmodyfikowany plik, a potem usuwane są dziennik i kopia zapasowa):

| Metoda | Uwagi |
|---|---|
| Ikona w obszarze powiadomień → `End session` (`Zakończ sesję`) → potwierdzenie | Dostępna zarówno w sesjach `OmsiLaunch.exe`, jak i `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | Z innej konsoli; wraca natychmiast, a właściciel dokańcza przywracanie. |
| Ctrl+C w konsoli właściciela | Żąda zatrzymania; właściciel przed zakończeniem czeka na przywrócenie plików. |
| `/observe-seconds:<n>` | Automatyczne zatrzymanie `n` sekund po osiągnięciu `Running`. |
| OMSI kończy działanie samodzielnie | Właściciel wykrywa `ProcessExited` i przywraca pliki. |

Własna procedura zamykania OMSI nie jest wykonywana, więc OMSI nie zapisuje ponownie `options.cfg` przy wyjściu; jest to zamierzone, aby przywrócenie było dokładne. Zamknięcie okna konsoli właściciela przyciskiem X daje przywracaniu tylko 4 s; jeśli nie zdążyło się zakończyć, następne uruchomienie (lub `OmsiLaunch.exe /recover`) dokończy je na podstawie dziennika. Kod wyjścia właściciela wynosi `0`, gdy sesja zakończyła się w stanie `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Gdzie szukać po zakończeniu

| Lokalizacja | Zawartość |
|---|---|
| Wyjście konsoli / `--json` | Plan, stan `Running`, stan końcowy (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Ślad hosta sesji (granice transakcji, uruchomienie procesu, przekazanie do wtyczki, wejście do rozgrywki, przywracanie). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Wynik operacji `/runtime:` wykonanej przez właściciela. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Zdarzenia wskaźnika w obszarze powiadomień. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` po czystym zakończeniu. `true` oznacza, że pozostał dziennik; należy uruchomić `OmsiLaunch.exe /recover`. |

Jeśli sesja nie osiągnęła rozgrywki, stan końcowy zawiera komunikat diagnostyczny `OL_E_` z przyczyną błędu (na przykład `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), kod wyjścia wynosi `1`, a pliki i tak zostały przywrócone. Zob. [kody wyjścia](../reference/exit-codes.md), [błędy](../reference/errors.md) i [znane ograniczenia](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Uruchamianie bez konsoli

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

przekazuje wykonanie do `OmsiLaunchW.exe` i natychmiast zwraca `0`. Sesja nie ma konsoli; błędy są wyświetlane jako okna komunikatów, a ikona w obszarze powiadomień jest jedynym widocznym elementem. Do śledzenia sesji służą `session status`, `events watch` i katalog diagnostyki. Pełne zachowanie hosta Windows opisuje [dokumentacja OmsiLaunchW.exe](../reference/omsilaunchw.md).
