# Dokumentacja OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../README.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

To jest normatywna angielska dokumentacja OmsiLaunch `0.1.0-beta4`, stanu bazowego po etapie utwardzania (post-hardening). OmsiLaunch zapewnia programowalne uruchamianie, własność sesji i sterowanie w czasie działania (runtime) dla zainstalowanego OMSI 2; każda możliwość natywna jest rozwiązywana i weryfikowana osobno dla danego buildu (zob. [zgodność](reference/compatibility.md)). Każda strona w `docs/` opisuje, co robi bieżący kod; gdy strona i kod są niezgodne, rozstrzyga kod, a strona zawiera błąd.

Słownictwo stabilności używane w całej dokumentacji: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Flagi, które są parsowane, ale niczego nie robią, są oznaczone jako
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Nic nie jest nazywane zweryfikowanym w runtime, dopóki nie stwierdza tego strona
[stan weryfikacji runtime](status/runtime-validation-status.md).

<a id="who-reads-what"></a>
## Kto czyta co

| Odbiorcy | Na początek | Następnie |
| --- | --- | --- |
| Użytkownicy (CLI, skróty, profile sesji) | [Instalacja](getting-started/installation.md), [Pierwsza sesja](getting-started/first-session.md) | [Dokumentacja CLI](reference/cli.md), [Przykłady CLI](reference/cli-examples.md), [Profile sesji](reference/session-profiles.md), [Obszar powiadomień Windows](reference/windows-tray.md), [Kody wyjścia](reference/exit-codes.md) |
| Integratorzy (`OmsiLaunch.Api`, lokalne IPC) | [Szybki start z publicznym API](getting-started/api-quick-start.md), [Dokumentacja publicznego API](reference/public-api.md), [Dokumentacja LaunchSpec](reference/launchspec.md) | [Cykl życia sesji](concepts/session-lifecycle.md), [Sterowanie runtime](reference/runtime-control.md), [Możliwości](reference/capabilities.md), [Lokalne sterowanie / IPC](reference/local-control.md), [Dokumentacja błędów](reference/errors.md) |
| Opiekunowie (wydania, weryfikacja, granice) | [Pakowanie](reference/packaging.md), [Model stałej wtyczki](concepts/permanent-plugin.md) | [Transakcje i odzyskiwanie](concepts/transactions-and-recovery.md), [Zgodność](reference/compatibility.md), [Znane ograniczenia](reference/known-limitations.md), [Stan weryfikacji runtime](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Nawigacja

| Strona | Przeznaczenie |
| --- | --- |
| [Pierwsze kroki](getting-started/first-session.md) | Zaplanowanie, uruchomienie, obserwowanie i zatrzymanie jednej sesji z katalogu głównego OMSI. |
| [Instalacja](getting-started/installation.md) | Wymagania wstępne, rozpakowanie pakietu do katalogu głównego OMSI, weryfikacja za pomocą `/version`, czyste usunięcie. |
| [Szybki start z publicznym API](getting-started/api-quick-start.md) | Kompletny program .NET, który planuje, uruchamia, odczytuje i zatrzymuje jedną sesję. |
| [Dokumentacja CLI](reference/cli.md) | Każda flaga, słowo polecenia i hierarchiczna ścieżka polecenia `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Przykłady CLI](reference/cli-examples.md) | Wiersze poleceń do skopiowania dla typowych zadań. |
| [Dokumentacja LaunchSpec](reference/launchspec.md) | Każda właściwość `LaunchSpec` i wartość wyliczenia, reguły wczytywania JSON dla `/spec`. |
| [Zarządzanie HOF](reference/hof-management.md) | Kopiowanie HOF w zakresie sesji i jego powielanie do katalogów pojazdów: źródła, cele, konflikty, przywracanie. |
| [Moduły runtime](reference/runtime-modules.md) | Moduły działające wewnątrz OMSI razem z sesją: manifest, zaufanie, cykl życia, polityki awarii, operacje. |
| [Dokumentacja profili sesji](reference/session-profiles.md) | Schemat `profile.yaml` `omsilaunch.session-profile/v1`, klucze, limity, pierwszeństwo. |
| [Dokumentacja publicznego API](reference/public-api.md) | `IOmsiLaunch`, publiczne rekordy i wyliczenia, stabilność każdej składowej. |
| [Inwentarz publicznego API](reference/public-api-inventory.md) | Wygenerowana lista każdego publicznego typu i składowej wraz z sygnaturą i stabilnością. |
| [Sterowanie runtime](reference/runtime-control.md) | Kanał poleceń runtime, limity czasu, uchwyty, semantyka zatrzymania. |
| [Dokumentacja możliwości](reference/capabilities.md) | Katalog możliwości (capabilities) i każdy publiczny identyfikator operacji runtime wraz z jego klasyfikacją. |
| [Cykl życia sesji](concepts/session-lifecycle.md) | Przejścia `SessionState`, co obiecuje `StartSessionAsync`, jak kończy się sesja. |
| [Transakcje i odzyskiwanie](concepts/transactions-and-recovery.md) | Stany dziennika, kopie zapasowe, weryfikacja przywracania, usunięcia w sesji, odzyskiwanie po awarii. |
| [Model stałej wtyczki](concepts/permanent-plugin.md) | Zestaw plików wtyczki (closure) `plugins\OmsiLaunch.*`, integralność oparta na manifeście, czego sesja nigdy nie dotyka. |
| [Lokalne sterowanie / IPC](reference/local-control.md) | Protokół potoku nazwanego (named pipe) `0.1`, punkt końcowy dla każdej instalacji, powiązanie `session_id`, model zaufania. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | Host Windows (bez konsoli): różnice względem `OmsiLaunch.exe`, `/silent`, okna dialogowe, kody wyjścia. |
| [Obszar powiadomień Windows](reference/windows-tray.md) | Wskaźnik w obszarze powiadomień: ikona, menu, okno stanu pole po polu, zakończenie sesji (End session), ponowne uruchomienie Eksploratora (Explorer). |
| [Dokumentacja błędów](reference/errors.md) | Każdy kod `OL_E_*` / `OL_W_*` wraz z kategorią i znaczeniem. |
| [Kody wyjścia](reference/exit-codes.md) | Wartości `PublicExitCode` od 0 do 10 oraz kody shimu programu rozruchowego od 100 do 106. |
| [Pakowanie / układ instalacji](reference/packaging.md) | Pliki w archiwum ZIP wydania, `release-manifest.json`, układ `.omsilaunch\`. |
| [Zgodność / obsługiwane buildy OMSI](reference/compatibility.md) | Jedyny obsługiwany hash `Omsi.exe`, akceptowany hash Steam LAA, wymagania platformy. |
| [Znane ograniczenia](reference/known-limitations.md) | Co w tej becie jest nieobsługiwane, częściowe lub stanowi zaakceptowane ryzyko. |
| [Stan weryfikacji runtime](status/runtime-validation-status.md) | Co zostało uruchomione pod OMSI, co działało tylko offline, co nadal wymaga prawdziwej sesji. |

Strony, które pozostają normatywne dla opiekunów: [`README.md`](../../../README.md) repozytorium,
specyfikacje w `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) oraz strony projektu w
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Stanowią one podsumowanie; szczegółową dokumentacją są
powyższe strony. Strony historyczne są wymienione w
[manifeście dokumentacji](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Jak dokumentacja jest utrzymywana w zgodności z kodem

Bramka dokumentacji, `tests\OmsiLaunch.DocumentationTests`, jest kompilowana względem
`OmsiLaunch.Api` i `OmsiLaunch.Core` i porównuje powyższe strony z
kodem definiującym publiczną powierzchnię:

| Bramka | Sprawdza |
| --- | --- |
| `docs.cli-flags` | Każdy wpis `CliInput.KnownFlags` występuje w dokumentacji CLI jako `` `/flag` `` lub `` `/flag:` ``; każdy wpis `CliInput.AcceptedNoEffectFlags` jest w swoim wierszu oznaczony jako `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`; każde słowo polecenia i każda ścieżka polecenia `CliInput.HierarchicalRoutes` występuje wraz ze swoją operacją runtime; każda wartość `PublicExitCode` ma wiersz `| n |` w tabeli kodów wyjścia. |
| `docs.capabilities` | Każdy identyfikator `PublicCapabilityRegistry.All`, każdy wpis `PublicCapabilityRegistry.PublicRuntimeOperationIds` i każda nazwa `PublicCapabilityClassification` występuje w dokumentacji możliwości. |
| `docs.errors` | Każdy kod `PublicErrorCodes.All` występuje w dokumentacji błędów i żadnego literału `OL_E_*` / `OL_W_*` w `src\` ani w `tools\OmsiLaunch.Cli\` nie brakuje w `PublicErrorCodes`. |
| `docs.public-api` | Każdy eksportowany typ `OmsiLaunch.Api`, każda wartość wyliczenia i każda składowa `IOmsiLaunch` występuje w dokumentacji publicznego API i używane jest wszystkie pięć słów stabilności. |
| `docs.launchspec` | Każda publiczna właściwość osiągalna z `LaunchSpec` i każda wartość jej wyliczeń występuje w dokumentacji LaunchSpec. |
| `docs.session-profiles` | Każdy klucz `SessionProfileCompiler.SchemaKeys`, identyfikator schematu i limit `256 KiB` występują w dokumentacji profili sesji. |
| `docs.structure` | Każda strona z tabeli nawigacji istnieje. |
| `docs.links` | Każdy względny link w `docs\**\*.md` (z wyłączeniem `docs\localized\`) i w plikach `*.md` w katalogu głównym prowadzi do istniejącego pliku lub katalogu. |
| `docs.localization` | Każda lokalizacja wymieniona w `docs\localized\LOCALIZATION-MANIFEST.md` ma każdą stronę zlokalizowanego zestawu; każda strona zachowuje nagłówki, tabele i bloki kodu strony angielskiej, każdy fragment kodu w tekście (flagi, identyfikatory możliwości i operacji, kody błędów, klucze, identyfikatory) i każdy link, a jej względne linki prowadzą do istniejących celów. |

Bramka jest jednym z zestawów testów uruchamianych przez `tools\Invoke-OfflineValidation.ps1`
(można ją pominąć za pomocą `-SkipDocs`). Działa offline, nigdy nie uruchamia OMSI i powoduje niepowodzenie
buildu, gdy flaga, ścieżka polecenia, możliwość, kod błędu, wartość wyliczenia lub publiczny typ nie
są udokumentowane albo link jest uszkodzony. Nie sprawdza prozy, więc strona może nadal
błędnie opisywać zachowanie; należy to zgłosić jako błąd strony.

<a id="translations"></a>
## Tłumaczenia

`docs\localized\<locale>\` zawiera kompletne tłumaczenia tej dokumentacji `0.1.0-beta4`
dla `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` i `ja-JP`. Zestaw stron, katalogi główne lokalizacji oraz
strony celowo nietłumaczone są wymienione w
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
Tłumaczenia zachowują bez zmian każde polecenie, flagę, identyfikator, kod błędu i przykład
stron angielskich, co sprawdza bramka `docs.localization`.
Strony angielskie pozostają źródłem normatywnym: gdy tłumaczenie jest z nimi niezgodne,
rozstrzygają strona angielska i kod, a tłumaczenie
zawiera błąd.
