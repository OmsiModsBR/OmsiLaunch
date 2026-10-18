# Pakowanie i układ wydania

<!-- l10n: source=reference/packaging.md source-sha256=56dbb36369fb47d7531d84d9ff8e6e2050d220420d970194822e6b13afd3d812 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/packaging.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona opisuje pakiet wydania OmsiLaunch `0.1.0-beta4`: co tworzy `tools\New-ReleasePackage.ps1`, pola pliku `release-manifest.json`, sposób, w jaki kontroler wykorzystuje manifest w czasie działania do weryfikacji zestawu plików stałej wtyczki (closure), jak pakiet jest instalowany w katalogu głównym instalacji OMSI i z niego usuwany, co zawiera katalog `.omsilaunch` po użyciu, a także skrypty walidacyjne (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). Tożsamość produktu pochodzi z `OmsiLaunch.Version.props`. Kroki instalacji dla użytkowników opisano w sekcji [instalacja](../getting-started/installation.md); rolę zestawu plików wtyczki w runtime – na stronie [stała wtyczka](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Tożsamość produktu (`OmsiLaunch.Version.props`)

| Właściwość | Wartość | Zastosowanie |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | Pole manifestu `product`, Windows `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro and OmsiModsBR Team` | Windows `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - github.com/lmonteirotech and OmsiModsBR Team - github.com/OmsiModsBR` | Windows `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | Pole manifestu `product_version`, Windows `ProductVersion`, informacyjna wersja zestawu (`/version`), nazwa publicznego archiwum ZIP |
| `OmsiLaunchManagedVersion` | `0.1.0` | Podstawa wersji zestawów zarządzanych |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Wersja zestawu i wersja pliku Windows |
| `OmsiLaunchPackageAlias` | `current` | Pole manifestu `package_alias`, folder przejściowy (staging) i nazwa archiwum ZIP aliasu |

`Directory.Build.props` ustawia `InformationalVersion` na `OmsiLaunchProductVersion` bez rewizji źródeł, dlatego `OmsiLaunch.exe /version` wypisuje dokładnie `0.1.0-beta4`.

<a id="build-toolsnew-releasepackageps1"></a>
## Budowanie (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (domyślny katalog wyjściowy `artifacts\release`) przygotowuje pakiet z już zbudowanych artefaktów. Zapis do `artifacts\release`, gdy `OmsiLaunch-<product_version>.zip` już istnieje, jest odrzucany, chyba że podano `-AllowOverwritePublished`; pakiety kandydujące trafiają do innego katalogu (walidacja offline używa `artifacts\candidate\post-round-a`).

Przed przygotowaniem pakietu każdy wynik budowania jest sprawdzany pod kątem nieaktualności.

- **`OmsiLaunch.Native.x86.dll`: według zawartości, nie według znacznika czasu.** Budowanie natywne zapisuje `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (cel `WriteOmsiLaunchNativeBuildReceipt` w pliku `.vcxproj`): skrót SHA-256 utworzonej biblioteki DLL (`output=`) oraz każdego źródła, z którego ją zbudowano (`source=<sha256>|<path>`: plik `.cpp`, `.rc`, `.vcxproj` i `OmsiLaunch.Version.props`). Pakowanie odrzuca bibliotekę DLL, gdy jej skrót nie jest zapisanym wynikiem (`does not match its build receipt`: nieaktualna lub obca kopia, niezależnie od znacznika czasu), gdy zapisane źródło uległo zmianie (`Native source changed after the recorded build`), gdy źródło natywne nie jest objęte potwierdzeniem budowania lub gdy potwierdzenia brakuje.
- **Shimy i zestawy zarządzane: według znacznika czasu.** Każdy z nich nie może być starszy niż źródła własnego projektu (`.cpp`/`.rc`/`.vcxproj` każdego shimu; własny projekt każdego zestawu zarządzanego). Nieaktualne wejście przerywa pakowanie komunikatem `Stale build artifact`.

Pakiet jest następnie składany w zupełnie nowym katalogu przejściowym o unikalnej nazwie (`.staging-<guid>` w katalogu wyjściowym), dzięki czemu żaden plik z wcześniejszego uruchomienia nie może trafić do zestawu plików. Poprzedni folder `OmsiLaunch-current` i archiwa są zastępowane dopiero po pomyślnym przejściu wszystkich poniższych kontroli.

| Źródło | Miejsce docelowe w pakiecie |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | katalog główny |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| `assets\splash\*.bmp` CLI (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | katalog główny |
| każdy plik w `docs\` z wyjątkiem `docs\localized\` (dokumentacja angielska, ta sama struktura katalogów) | `.omsilaunch\docs\` (dzięki temu istnieje `.omsilaunch\docs\reference\cli.md`, ścieżka wypisywana przez tekst pomocy CLI; audyt dokumentacji BUG-08). Odnośniki z `docs\README.md` do podsumowań w katalogu głównym repozytorium (`PUBLIC-API.md` i inne) działają tylko w repozytorium źródłowym. |
| `docs\localized\LOCALIZATION-MANIFEST.md` oraz `docs\localized\<locale>\**` dla każdej lokalizacji wymienionej w tym manifeście (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` i `ja-JP`) | `.omsilaunch\docs\localized\` (ta sama struktura); brak wymienionej lokalizacji przerywa skrypt |

Następnie skrypt oblicza skrót każdego przygotowanego pliku, zapisuje `release-manifest.json` w katalogu głównym pakietu jako UTF-8 **bez** znacznika BOM (wynik nie zależy już od edycji PowerShell), uruchamia `Test-ReleasePackageIntegrity.ps1` na przygotowanym zestawie, ponownie porównuje każdy przygotowany plik wtyczki i `OmsiLaunch.Native.x86.dll` z odpowiadającym mu wynikiem budowania, kompresuje zestaw, **rozpakowuje archiwum do nowego katalogu tymczasowego i weryfikuje rozpakowany zestaw plików względem tego samego manifestu** (zarchiwizowany manifest musi być identyczny bajt w bajt z zweryfikowanym), po czym publikuje zestaw jako `OmsiLaunch-current`, archiwum jako `OmsiLaunch-current.zip`, kopiuje je do `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) i zapisuje `OmsiLaunch-0.1.0-beta4.zip.sha256` zawierający `<SHA-256>  <file name>`. Brak któregokolwiek artefaktu przerywa skrypt. Skrypt niczego nie buduje; najpierw należy uruchomić `Invoke-OfflineValidation.ps1` (lub poszczególne kroki `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Układ pakietu

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Własnością produktu są wyłącznie pliki produktu w katalogu głównym, `plugins\OmsiLaunch.*` oraz `.omsilaunch\`. Wtyczki innych producentów w `plugins\` nigdy nie są wyliczane, kopiowane, haszowane, usuwane ani przywracane przez OmsiLaunch.

## `release-manifest.json`

| Pole | Typ | Znaczenie |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`; musi być zgodne z `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | `build_id` i `code_sha256` każdego znanego układu (`Omsi23004`, `13ec110a...c028`); ścieżka przyspieszona, nie wymóg |
| `configuration` | string | `Release` lub `Debug` |
| `generated_utc` | string | Czas budowania w formacie ISO-8601 |
| `files[]` | object[] | `path` (ukośniki zwykłe, względem katalogu głównego pakietu), `bytes`, `sha256` (szesnastkowo, wielkie litery) dla każdego spakowanego pliku |

Manifest to dane, nigdy wykonywalna polityka: kontroler odczytuje wyłącznie wpisy `plugins/`. Czytnik akceptuje plik ze znacznikiem BOM UTF-8 lub bez niego (manifesty zapisane przez Windows PowerShell 5.1 przed tą poprawką go zawierają).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Wykorzystanie manifestu w czasie działania (integralność wtyczki)

Przed każdym planowaniem i uruchomieniem `OmsiLaunchService.LoadArtifacts` buduje oczekiwany zestaw plików wtyczki (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. Kontroler szuka pliku `release-manifest.json` obok `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Jeśli plik istnieje, `ReleaseManifest.TryReadPluginHashes` wyodrębnia skróty `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID`, jeśli pliku nie da się odczytać jako manifestu).
2. Dla każdego zainstalowanego pliku `<root>\plugins\OmsiLaunch.*` obliczany jest skrót (SHA-256) i porównywany:
   - z manifestem: ze skrótem z manifestu; zapisywany jest komunikat diagnostyczny planu `plugin.integrity.reference = manifest`. Brak pliku → `OL_E_PERMANENT_PLUGIN_MISSING`; plik obecny, ale niewymieniony → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; różny skrót → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - bez manifestu (układ deweloperski lub instalacja, w której pominięto manifest): można sprawdzić jedynie obecność i spójność z kopią spakowaną obok kontrolera; `plugin.integrity.reference = self`.
3. Niepowodzenie oznacza plan jako niemożliwy do uruchomienia (`OL_E_RUNTIME_ARTIFACT_MISSING` ze szczegółami) lub powoduje odrzucenie uruchomienia (kod wyjścia `7`).

Pliki wtyczki nigdy nie są przygotowywane, obejmowane migawką, przywracane ani usuwane przez sesję; zestaw plików jest stałą częścią instalacji. Biblioteka x86 `OmsiLaunch.Native.x86.dll` jest ładowana wyłącznie z `plugins\`; zestawy zarządzane deklarują `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Instalacja w katalogu głównym OMSI

1. Zweryfikuj archiwum: porównaj `OmsiLaunch-0.1.0-beta4.zip` z `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Rozpakuj archiwum **bezpośrednio do katalogu głównego instalacji OMSI** (katalogu zawierającego `Omsi.exe`). Spowoduje to umieszczenie plików katalogu głównego, `plugins\OmsiLaunch.*` (obok ewentualnych wtyczek innych producentów) oraz `.omsilaunch\`.
3. Pozostaw `release-manifest.json` obok `OmsiLaunch.exe`: umożliwia on sprawdzanie integralności wtyczki na podstawie manifestu. Manifest i pliki binarne muszą pochodzić z tego samego pakietu: nowe pliki binarne ze starszym manifestem (lub odwrotnie) powodują, że każde uruchomienie kończy się błędem `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` kopiuje teraz manifest razem z plikami produktu; wcześniej go pomijał, przez co obok nowszych plików binarnych pozostawał starszy manifest (Round A RA-007).
4. Sprawdź spójność w trybie tylko do odczytu za pomocą `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` musi mieć wartość `true`.
5. Nie przenoś plików binarnych wtyczki do `.omsilaunch\` i nie zmieniaj nazw `plugins\OmsiLaunch.*`.
6. Zweryfikuj instalację poleceniami `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` i `/plan` (zob. [pierwsza sesja](../getting-started/first-session.md)).

Istniejące pliki `.omsilaunch\assets\splash\*.bmp` nigdy nie są nadpisywane przez sesję (jawnie zarządzany zestaw zasobów jest zachowywany); nadpisanie ich przez rozpakowanie nowego pakietu jest świadomym działaniem użytkownika.

<a id="the-omsilaunch-directory-after-use"></a>
## Katalog `.omsilaunch` po użyciu

| Ścieżka | Tworzony przez | Czas życia |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | pakiet lub kopiowany przy pierwszej sesji z zarządzanym ekranem startowym | trwały |
| `docs\`, `examples\` | pakiet | trwały |
| `session-profiles\<id>\profile.yaml` | użytkownik | trwały; zob. [profile sesji](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | każda sesja | zachowywany dla 50 najnowszych sesji; starsze pliki z prefiksem sesji są usuwane przy starcie nowej sesji |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, harnessy walidacyjne | ta sama retencja (prefiks sesji) |
| `diagnostics\tray-host.log` | wskaźnik w obszarze powiadomień | trwały, dopisywany |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | trwałe (bez prefiksu sesji) |
| `journal.json` | transakcja | istnieje od `Prepared` do `Restored`; pozostawiony plik oznacza, że odzyskiwanie oczekuje (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | transakcja | migawki zmienionych plików; usuwane po przywróceniu |

Żadne dane nie opuszczają komputera. Zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Odinstalowanie

1. Upewnij się, że żadna sesja nie jest uruchomiona (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) i żadne odzyskiwanie nie oczekuje (`OmsiLaunch.exe /recovery-status`; uruchom `/recover`, jeśli `pending` ma wartość `true`), tak aby pliki OMSI były już przywrócone.
2. Usuń `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Pozostałych wtyczek nie należy ruszać.
3. Usuń pliki produktu z katalogu głównego wymienione w powyższym układzie (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Usuń `.omsilaunch\` (spowoduje to usunięcie profili sesji i diagnostyki). Nigdy nie usuwaj tego katalogu, dopóki istnieje `journal.json`.

Zakończona sesja przywraca każdy plik, którego była właścicielem, więc dalsze sprzątanie nie jest potrzebne. Pliki zapisywane przez sam OMSI podczas działania (na przykład `[last_map]` w `options.cfg`, pamięci podręczne, `laststn.osn`, logi) stanowią normalny stan OMSI i nie są przywracane; zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Skrypty walidacyjne

| Skrypt | Przeznaczenie | Ingeruje w OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Buduje `OmsiLaunch.sln` z ostrzeżeniami traktowanymi jako błędy oraz trzy projekty natywne (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) za pomocą MSBuild, a następnie uruchamia wszystkie zestawy testów offline: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` oraz – o ile nie pominięto – `OmsiLaunch.DocumentationTests`, a potem test regresji pakowania `Test-PackagingPipeline.ps1` (pomijany przy `-SkipNative`). Wypisuje `OFFLINE VALIDATION PASSED`/`FAILED`. | Nie |
| `tools\New-ReleasePackage.ps1` | Ochrona przed nieaktualnymi artefaktami, przygotowanie zestawu, manifest, samokontrola integralności, ZIP, suma kontrolna (jak wyżej). | Nie |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Sprawdza, czy manifest wymienia dokładnie spakowane pliki ze zgodnym rozmiarem i SHA-256, czy wymagany zestaw plików (trzy pliki wykonywalne, kontroler, dziewięć plików stałej wtyczki łącznie z `OmsiLaunch.Native.x86.dll`) jest obecny oraz czy konfiguracja to `Release`. Z parametrem `-InstallationRoot` porównuje pliki produktu w instalacji z pakietem **w trybie tylko do odczytu**. Kod wyjścia `0` = spójny. | Nie (tylko odczyt) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Tworzy pakiet kandydujący z czystego katalogu przejściowego w `artifacts\candidate\post-round-a` i wymaga, aby przygotowany zestaw plików oraz ponownie rozpakowane archiwum przeszły kontrolę integralności. Dowodzi, że bramka integralności odrzuca zmodyfikowaną bibliotekę DLL, zmodyfikowany lub stary `Native.x86`, nieaktualną kopię wtyczki, usunięty wymieniony plik, nieoczekiwany plik, zmieniony lub błędnie sformatowany skrót w manifeście, zduplikowane wpisy (dokładne, różniące się wielkością liter, różniące się separatorem), ścieżki nadrzędne i bezwzględne oraz nieprawidłowy JSON; że narzędzie pakujące odrzuca stary `Native.x86` umieszczony w wyniku budowania (nawet z nowszym znacznikiem czasu) oraz potwierdzenie, którego źródła się zmieniły; a także że opublikowane archiwum nigdy nie jest nadpisywane. Wyniki budowania są przywracane bajt w bajt. Uruchamiany przez `Invoke-OfflineValidation.ps1`. | Nie |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Rozpakowuje archiwum ZIP do `artifacts\release\identity-verification`, sprawdza `product`/`product_version`/`package_alias` oraz weryfikuje `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` każdego pliku `.exe`/`.dll` z wyjątkiem `nethost.dll` i `YamlDotNet.dll`, `InternalName`/`OriginalFilename` obu shimów, a także to, że `OmsiLaunch.exe` zawiera osadzoną ikonę. | Nie |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Weryfikuje spakowany plik wykonywalny Release na rzeczywistej instalacji: manifest musi mieć konfigurację `Release`, nie może zawierać ścieżek `Debug` ani `runtime/plugin/` i musi instalować `plugins/OmsiLaunch.*`; cztery zasoby ekranu startowego muszą istnieć. Uruchamia trzy przypadki `/plan` (zarządzany domyślny, zarządzany z własnymi zasobami, `/splash:Unset`). Z parametrem `-RunOmsi` (wymaga `-InstallPackage`) uruchamia każdy przypadek z `/observe-seconds`, obserwuje `GUI\NewSplashscreen_ENG.bmp` i `GUI\NewSplashscreen_PTB.bmp` w trakcie sesji oraz sprawdza dokładne przywrócenie, brak `Omsi.exe`, brak `journal.json`, kod wyjścia `0`, niezmienione skróty wtyczek innych producentów i niezmieniony zestaw plików stałej wtyczki. Zapisuje `.omsilaunch\diagnostics\release-presentation-validation.json`. | Tak z `-RunOmsi` (w zakresie sesji, przywracane) |

Oba skrypty `Test-*` odczytują `OmsiLaunch.Version.props`, aby poznać oczekiwaną wersję.
