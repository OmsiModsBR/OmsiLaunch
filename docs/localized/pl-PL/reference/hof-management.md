# Zarządzanie HOF

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/hof-management.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

OMSI odczytuje pliki HOF (`*.hof`: cele podróży, numery linii i listy przystanków wyświetlane przez IBIS/wyświetlacz kierunkowy pojazdu) z katalogu każdego pojazdu. Zarządzanie HOF kopiuje pliki HOF do katalogów pojazdów **na czas trwania jednej sesji**: mapa lub dodatek może dostarczyć pliki HOF potrzebne jego liniom, a użytkownik może ponownie wykorzystać pliki HOF jednego pojazdu w innych pojazdach, bez trwałej zmiany któregokolwiek pojazdu. Ta strona jest normatywną dokumentacją referencyjną w postaci zaimplementowanej przez `HofPlanner` w `src/OmsiLaunch.Core/HofPlanner.cs`, pole `HofManagement` rekordu [LaunchSpec](launchspec.md), flagi [CLI](cli.md) `/hof-source`, `/hof-target`, `/hof-file` i `/hof-mode` oraz blok `hofs` [profili sesji](session-profiles.md).

Stabilność: `STABLE_BETA` dla planowania, reguł konfliktów, ograniczenia do instalacji i transakcyjnego przywracania (bramki offline `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Zweryfikowane w runtime: sesja z kopią lustrzaną wraz z dokładnym przywróceniem oraz odzyskiwaniem po awarii przez `/recover` (zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md)).

Zarządzanie HOF to nie to samo co `PlayerVehicle.Hof` / `/hof:<id>`, które wybiera plik HOF pojazdu gracza przy starcie i jest w tym buildzie `UNAVAILABLE`.

## Model

Żądanie jest listą operacji (`HofManagementSpec.Operations`). Każdy `HofOperationSpec` zawiera:

| Pole | Znaczenie |
| --- | --- |
| `SourceKind`, `Source` | Skąd pochodzą pliki HOF (tabela poniżej). |
| `TargetVehicles` | Jeden lub więcej zainstalowanych pojazdów: identyfikator pliku `.bus` lub `.ovh` (`Vehicles\MAN_SD200\MAN_SD77.bus`) albo katalog pojazdu (`Vehicles\MAN_SD200`), w obu przypadkach poniżej `Vehicles\`. Kilka identyfikatorów w jednym katalogu wskazuje ten katalog tylko raz. |
| `Mode` | `Add` (domyślnie): kopiuje źródłowe pliki HOF do każdego katalogu docelowego; pozostałe pliki HOF w tym katalogu pozostają. `Mirror`: kopiuje źródłowe pliki HOF i na czas sesji usuwa każdy inny plik `.hof` z katalogu docelowego, tak aby zestaw plików HOF w katalogu był równy zestawowi źródłowemu. |
| `Files` | Opcjonalna lista nazw plików pobieranych ze źródła (bez rozróżniania wielkości liter). Pominięta: każdy plik `*.hof` znajdujący się bezpośrednio w źródle. Nazwa z listy, której źródło nie zawiera: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Wskazuje na |
| --- | --- | --- |
| `Collection` | zwykła nazwa, na przykład `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: kolekcja utrzymywana przez użytkownika lub narzędzie. |
| `Directory` | katalog względny wobec instalacji, na przykład `maps\RMG Leste\hof` | `*.hof` w tym katalogu: pliki HOF dostarczane obok zawartości mapy lub dodatku. |
| `Vehicle` | identyfikator lub katalog pojazdu, jak w przypadku celów | `*.hof` w katalogu tego pojazdu: ponowne wykorzystanie plików HOF jednego pojazdu w innych. |
| `File` | plik `.hof` względny wobec instalacji | ten jeden plik. |

Używane są wyłącznie pliki znajdujące się bezpośrednio w katalogu źródłowym (bez rekursji). Dowiązania symboliczne i połączenia (junction) nigdy nie są śledzone (zob. [ograniczenie do instalacji](#confinement)).

<a id="what-a-session-does"></a>
## Co robi sesja

1. **Planowanie** (`PlanSessionAsync`, `/plan`). Każda operacja jest rozwiązywana: źródła są odczytywane, cele sprawdzane, konflikty wykrywane. Każdy plik, który ulegnie zmianie, pojawia się w `SessionPlan.PlannedMutations` z `SemanticKey` `hof.add` lub `hof.mirror`, `RequestedValue` = identyfikator źródła oraz `Operation` `exact-file-overlay` (zapisany) lub `session-deletion` (usunięty przez lustrzane odwzorowanie); ścieżki są częścią `TouchedFiles`. Problem sprawia, że plan jest niemożliwy do uruchomienia, z kodami podanymi poniżej.
2. **Start**. Po odzyskaniu nieaktualnego dziennika operacje są ponownie rozwiązywane na podstawie bieżących plików i przekazywane do transakcji sesji: zapisywane pliki są nakładkami (overlay), pliki usunięte przez lustrzane odwzorowanie są usunięciami sesyjnymi. Plik docelowy, którego bajty są już równe źródłu, pozostaje nietknięty i nie jest wpisem transakcji.
3. **Zakończenie sesji, błąd, awaria.** Transakcja dokładnie przywraca każdy katalog docelowy: pliki, które istniały, odzyskują oryginalne bajty, znaczniki czasu i atrybuty; pliki utworzone przez sesję są usuwane; pliki usunięte przez lustrzane odwzorowanie wracają. Po awarii właściciela to samo przywracanie jest wykonywane przy następnym starcie lub za pomocą `/recover` (zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md)).

Poza katalogami docelowymi nic nie jest zapisywane. Pliki źródłowe są tylko odczytywane. `.omsilaunch\hof\` nigdy nie jest modyfikowany przez sesję.

<a id="conflict-rules"></a>
## Reguły konfliktów

Operacje są stosowane w kolejności i muszą być ze sobą zgodne:

- Dwie operacje zapisujące ten sam plik docelowy tymi samymi bajtami są scalane; przy różnych bajtach: `OL_E_HOF_CONFLICT`.
- Lustrzane odwzorowanie, które usunęłoby plik zapisywany przez wcześniejszą operację, lub operacja zapisująca plik usuwany przez wcześniejsze lustrzane odwzorowanie: `OL_E_HOF_CONFLICT`.
- Lustrzane odwzorowanie obejmuje sesyjny zestaw plików HOF katalogu: pliki istniejące oraz pliki zapisywane tam przez wcześniejsze operacje.
- Cel HOF, który jest zapisywany także przez prezentację sesji (ekran startowy lub tekstury internetowe): `OL_E_HOF_CONFLICT` (nie może wystąpić przy obecnych ścieżkach prezentacji, które leżą poza `Vehicles\`).
- Gdy cel ma już plik o tej samej nazwie zapisanej inną wielkością liter, zachowywana jest istniejąca pisownia.

<a id="confinement"></a>
## Ograniczenie do instalacji

Każde źródło i każdy cel muszą pozostawać wewnątrz instalacji: ścieżki bezwzględne (z korzeniem), segmenty `..` i ścieżki wychodzące poza katalog główny dają `OL_E_HOF_PATH_ESCAPE`; tak samo cele (i źródła `Vehicle`) poza `Vehicles\`, nazwy kolekcji niebędące zwykłą nazwą katalogu oraz każdy katalog źródłowy lub docelowy osiągany przez połączenie (junction) lub dowiązanie symboliczne. Pliki `.hof`, które same są punktami ponownej analizy (reparse points), są ignorowane.

<a id="limits"></a>
## Limity

| Limit | Wartość | Błąd |
| --- | --- | --- |
| Rozmiar jednego pliku HOF | 8 MiB | `OL_E_HOF_INVALID` |
| Pliki na operację | 256 | `OL_E_HOF_INVALID` |
| Łączna liczba bajtów zapisanych na sesję | 256 MiB | `OL_E_HOF_INVALID` |
| Cele na operację | co najmniej 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Kody błędów

| Kod | Przyczyna |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | Kolekcja, katalog, pojazd lub plik nie istnieje, nie zawiera żadnego pliku `.hof` albo brakuje w nim pliku wymienionego w `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Docelowy plik lub katalog pojazdu nie istnieje. |
| `OL_E_HOF_PATH_ESCAPE` | Ścieżka wychodzi poza instalację, cel nie leży poniżej `Vehicles\`, nazwa kolekcji nie jest zwykłą nazwą lub przechodzi się przez połączenie (junction)/dowiązanie symboliczne. |
| `OL_E_HOF_CONFLICT` | Dwie operacje są niezgodne co do jednego pliku docelowego (zob. [reguły konfliktów](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Brak celów, brak źródła, źródło `File`, które nie jest plikiem `.hof`, cel będący plikiem innym niż `.bus`/`.ovh` lub przekroczony [limit](#limits). |

<a id="using-it"></a>
## Użycie

Wiersz poleceń (jedna operacja; kilka operacji wymaga `/spec` lub profilu sesji):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (JSON dla `/spec`, wartości wyliczeniowe jako liczby całkowite):

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

Ustawienie wstępne (preset) profilu sesji (źródło `package` to katalog wewnątrz pakietu profilu):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: należy ustawić `LaunchSpec.HofManagement` i jak zwykle wywołać `PlanSessionAsync` / `StartSessionAsync`; `/list:Hofs` (`ContentQueryKind.Hofs`) wyświetla listę zainstalowanych plików HOF.

Powiązane: [LaunchSpec](launchspec.md), [profile sesji](session-profiles.md), [CLI](cli.md), [błędy](errors.md), [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md).
