# Pakketindeling en releasestructuur

<!-- l10n: source=reference/packaging.md source-sha256=6fff256ee354401567ab45240b0f155822e6321212dc63ea33ed6bc938b945ad -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/packaging.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina beschrijft het releasepakket van OmsiLaunch `0.1.0-beta4`: wat `tools\New-ReleasePackage.ps1` oplevert, de velden van `release-manifest.json`, hoe de controller het manifest tijdens runtime gebruikt om de permanente plugin-closure te verifiëren, hoe het pakket in een OMSI-installatiemap wordt geïnstalleerd en daaruit wordt verwijderd, wat de map `.omsilaunch` na gebruik bevat, en de validatiescripts (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). De productidentiteit komt uit `OmsiLaunch.Version.props`. De installatiestappen voor gebruikers staan in [installatie](../getting-started/installation.md); de rol van de plugin-closure tijdens runtime staat in [permanente plugin](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Productidentiteit (`OmsiLaunch.Version.props`)

| Eigenschap | Waarde | Gebruikt voor |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | Manifest `product`, Windows `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro` | Windows `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/OmsiModsBR/OmsiLaunch` | Windows `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | Manifest `product_version`, Windows `ProductVersion`, informatieve assemblyversie (`/version`), naam van de openbare ZIP |
| `OmsiLaunchManagedVersion` | `0.1.0` | Basis van de managed assemblyversie |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Assemblyversie en Windows-bestandsversie |
| `OmsiLaunchPackageAlias` | `current` | Manifest `package_alias`, stagingmap en naam van de alias-ZIP |

`Directory.Build.props` stelt `InformationalVersion` in op `OmsiLaunchProductVersion` zonder bronrevisie, zodat `OmsiLaunch.exe /version` precies `0.1.0-beta4` afdrukt.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (standaarduitvoer `artifacts\release`) stelt een pakket samen uit reeds gebouwde artefacten. Schrijven naar `artifacts\release` terwijl `OmsiLaunch-<product_version>.zip` al bestaat, wordt geweigerd tenzij `-AllowOverwritePublished` is opgegeven; kandidaatpakketten gaan naar een andere map (de offline validatie gebruikt `artifacts\candidate\post-round-a`).

Vóór het samenstellen wordt elke build-uitvoer gecontroleerd op veroudering.

- **`OmsiLaunch.Native.x86.dll`: op inhoud, niet op tijdstempel.** De native build schrijft `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (target `WriteOmsiLaunchNativeBuildReceipt` in de `.vcxproj`): de SHA-256 van de DLL die hij heeft geproduceerd (`output=`) en van elke bron waaruit hij is gebouwd (`source=<sha256>|<path>`: de `.cpp`, de `.rc`, de `.vcxproj` en `OmsiLaunch.Version.props`). Het verpakken weigert de DLL wanneer de hash niet overeenkomt met de vastgelegde uitvoer (`does not match its build receipt`: een verouderde of vreemde kopie, ongeacht de tijdstempel), wanneer een vastgelegde bron is gewijzigd (`Native source changed after the recorded build`), wanneer een native bron niet door het ontvangstbewijs wordt gedekt, of wanneer het ontvangstbewijs ontbreekt.
- **Shims en managed assembly's: op tijdstempel.** Elk ervan mag niet ouder zijn dan de bronnen van het eigen project (`.cpp`/`.rc`/`.vcxproj` van elke shim; het eigen project van elke managed assembly). Een verouderde invoer breekt het verpakken af met `Stale build artifact`.

Het pakket wordt vervolgens samengesteld in een gloednieuwe stagingmap met een unieke naam (`.staging-<guid>` in de uitvoermap), zodat geen enkel bestand uit een eerdere uitvoering in de closure terecht kan komen. De vorige map `OmsiLaunch-current` en de archieven worden pas vervangen nadat elke onderstaande controle is geslaagd.

| Bron | Bestemming in het pakket |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | hoofdmap |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| CLI `assets\splash\*.bmp` (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | hoofdmap |
| elk bestand onder `docs\` behalve `docs\localized\` (de Engelse documentatie, met dezelfde mapstructuur) | `.omsilaunch\docs\` (zodat `.omsilaunch\docs\reference\cli.md`, het pad dat de CLI-gebruikstekst afdrukt, bestaat; documentatieaudit BUG-08). Koppelingen vanuit `docs\README.md` naar de samenvattingen in de hoofdmap van de repository (`PUBLIC-API.md` en andere) werken alleen in de bronrepository. |
| `docs\localized\LOCALIZATION-MANIFEST.md` en `docs\localized\<locale>\**` voor elke locale die in dat manifest staat (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` en `ja-JP`) | `.omsilaunch\docs\localized\` (dezelfde structuur); een vermelde locale die ontbreekt, breekt het script af |

Daarna berekent het script de hash van elk gestaged bestand, schrijft het `release-manifest.json` in de hoofdmap van het pakket als UTF-8 **zonder** BOM (het resultaat hangt niet langer af van de PowerShell-editie), voert het `Test-ReleasePackageIntegrity.ps1` uit op de stage, vergelijkt het elk gestaged pluginbestand en `OmsiLaunch.Native.x86.dll` opnieuw met de build-uitvoer, comprimeert het de stage, **pakt het het archief uit in een nieuwe tijdelijke map en valideert het de uitgepakte closure tegen hetzelfde manifest** (het gearchiveerde manifest moet byte voor byte identiek zijn aan het gevalideerde), publiceert het vervolgens de stage als `OmsiLaunch-current` en het archief als `OmsiLaunch-current.zip`, kopieert het dit naar `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) en schrijft het `OmsiLaunch-0.1.0-beta4.zip.sha256` met de inhoud `<SHA-256>  <file name>`. Elk ontbrekend artefact breekt het script af. Het script bouwt niet zelf; voer eerst `Invoke-OfflineValidation.ps1` uit (of de afzonderlijke stappen `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Pakketindeling

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

Alleen de productbestanden in de hoofdmap, `plugins\OmsiLaunch.*` en `.omsilaunch\` zijn eigendom van het product. Plugins van derden onder `plugins\` worden door OmsiLaunch nooit opgesomd, gekopieerd, gehasht, verwijderd of hersteld.

## `release-manifest.json`

| Veld | Type | Betekenis |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`; moet overeenkomen met `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | `build_id` en `code_sha256` van elke bekende indeling (`Omsi23004`, `13ec110a...c028`); een snelle route, geen vereiste |
| `configuration` | string | `Release` of `Debug` |
| `generated_utc` | string | Buildtijd in ISO-8601 |
| `files[]` | object[] | `path` (slashes, relatief ten opzichte van de hoofdmap van het pakket), `bytes`, `sha256` (hexadecimaal in hoofdletters) voor elk verpakt bestand |

Het manifest is data, nooit uitvoerbaar beleid: de controller leest alleen de items onder `plugins/`. De lezer accepteert het bestand met of zonder UTF-8-BOM (manifesten die vóór deze correctie door Windows PowerShell 5.1 zijn geschreven, bevatten er een).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Gebruik van het manifest tijdens runtime (pluginintegriteit)

Vóór elk plan en elke start bouwt `OmsiLaunchService.LoadArtifacts` de verwachte plugin-closure op (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. De controller zoekt `release-manifest.json` naast `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Als het aanwezig is, haalt `ReleaseManifest.TryReadPluginHashes` de hashes van `plugins/*` eruit (`OL_E_RELEASE_MANIFEST_INVALID` als het bestand niet als manifest kan worden gelezen).
2. Van elk geïnstalleerd bestand `<root>\plugins\OmsiLaunch.*` wordt de hash (SHA-256) berekend en vergeleken:
   - met een manifest: met de hash in het manifest; de plandiagnose `plugin.integrity.reference = manifest` wordt vastgelegd. Ontbrekend bestand → `OL_E_PERMANENT_PLUGIN_MISSING`; bestand aanwezig maar niet vermeld → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; afwijkende hash → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - zonder manifest (ontwikkelindeling, of een installatie waarbij het manifest is weggelaten): alleen aanwezigheid en zelfconsistentie ten opzichte van de verpakte kopie naast de controller kunnen worden gecontroleerd; `plugin.integrity.reference = self`.
3. Een fout maakt het plan niet uitvoerbaar (`OL_E_RUNTIME_ARTIFACT_MISSING` met de details) of weigert de start (exitcode `7`).

Pluginbestanden worden door een sessie nooit gestaged, in een snapshot opgenomen, hersteld of verwijderd; de closure is een permanent onderdeel van de installatie. De x86-bibliotheek `OmsiLaunch.Native.x86.dll` wordt alleen vanuit `plugins\` geladen; de managed assembly's declareren `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Installatie in de OMSI-hoofdmap

1. Verifieer het archief: vergelijk `OmsiLaunch-0.1.0-beta4.zip` met `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Pak het archief **rechtstreeks uit in de OMSI-installatiemap** (de map die `Omsi.exe` bevat). Daarmee worden de bestanden in de hoofdmap, `plugins\OmsiLaunch.*` (naast eventuele plugins van derden) en `.omsilaunch\` geplaatst.
3. Laat `release-manifest.json` naast `OmsiLaunch.exe` staan: het maakt pluginintegriteit op basis van het manifest mogelijk. Het manifest en de binaire bestanden moeten uit hetzelfde pakket komen: nieuwe binaire bestanden over een ouder manifest (of omgekeerd) laten elke start mislukken met `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` kopieert het manifest nu samen met de productbestanden; vroeger sloeg het dit over, waardoor een ouder manifest naast nieuwere binaire bestanden achterbleef (Round A RA-007).
4. Controleer de samenhang alleen-lezen met `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` moet `true` zijn.
5. Verplaats geen binaire pluginbestanden naar `.omsilaunch\` en wijzig de naam van `plugins\OmsiLaunch.*` niet.
6. Verifieer met `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` en een `/plan` (zie [eerste sessie](../getting-started/first-session.md)).

Bestaande bestanden `.omsilaunch\assets\splash\*.bmp` worden nooit door een sessie overschreven (een expliciet beheerde assetset blijft behouden); ze overschrijven door een nieuw pakket uit te pakken is een bewuste handeling van de gebruiker.

<a id="the-omsilaunch-directory-after-use"></a>
## De map `.omsilaunch` na gebruik

| Pad | Aangemaakt door | Levensduur |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | pakket, of gekopieerd bij de eerste sessie met beheerd opstartscherm | permanent |
| `docs\`, `examples\` | pakket | permanent |
| `session-profiles\<id>\profile.yaml` | gebruiker | permanent; zie [sessieprofielen](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | elke sessie | bewaard voor de 50 nieuwste sessies; oudere bestanden met sessievoorvoegsel worden verwijderd wanneer een nieuwe sessie start |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, validatieharnassen | dezelfde bewaartermijn (sessievoorvoegsel) |
| `diagnostics\tray-host.log` | systeemvakindicator | permanent, aangevuld |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | permanent (zonder sessievoorvoegsel) |
| `journal.json` | transactie | bestaat van `Prepared` tot `Restored`; een achtergebleven exemplaar betekent dat er een recovery openstaat (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | transactie | snapshots van gewijzigde bestanden; verwijderd na het herstel |

Er verlaten geen gegevens de computer. Zie [transacties en recovery](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Verwijderen

1. Zorg ervoor dat er geen sessie actief is (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) en dat er geen recovery openstaat (`OmsiLaunch.exe /recovery-status`; voer `/recover` uit als `pending` `true` is), zodat de OMSI-bestanden al zijn hersteld.
2. Verwijder `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Laat andere plugins ongemoeid.
3. Verwijder de productbestanden in de hoofdmap die in de indeling hierboven staan (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Verwijder `.omsilaunch\` (hiermee worden je sessieprofielen en diagnostiek verwijderd). Verwijder deze map nooit zolang `journal.json` bestaat.

Een voltooide sessie herstelt elk bestand dat in haar bezit was, dus verder opruimen is niet nodig. Bestanden die OMSI zelf schrijft terwijl het draait (bijvoorbeeld `[last_map]` in `options.cfg`, caches, `laststn.osn`, logs) zijn de normale toestand van OMSI en worden niet teruggedraaid; zie [transacties en recovery](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Validatiescripts

| Script | Doel | Raakt OMSI aan |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Bouwt `OmsiLaunch.sln` met waarschuwingen als fouten en de drie native projecten (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) via MSBuild, en voert daarna elke offline suite uit: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests`, en `OmsiLaunch.DocumentationTests` tenzij overgeslagen, en vervolgens de verpakkingsregressietest `Test-PackagingPipeline.ps1` (overgeslagen met `-SkipNative`). Drukt `OFFLINE VALIDATION PASSED`/`FAILED` af. | Nee |
| `tools\New-ReleasePackage.ps1` | Verouderingscontrole, stage, manifest, zelfcontrole van de integriteit, ZIP, checksum (zie hierboven). | Nee |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifieert dat het manifest precies de verpakte bestanden vermeldt met overeenkomende grootte en SHA-256, dat de vereiste closure (drie uitvoerbare bestanden, de controller, de negen permanente pluginbestanden inclusief `OmsiLaunch.Native.x86.dll`) aanwezig is en dat de configuratie `Release` is. Met `-InstallationRoot` vergelijkt het de productbestanden van de installatie **alleen-lezen** met het pakket. Exitcode `0` = samenhangend. | Nee (alleen-lezen) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produceert een kandidaatpakket vanuit een schone staging onder `artifacts\candidate\post-round-a` en vereist dat de gestagede closure en het opnieuw uitgepakte archief de integriteitscontrole doorstaan. Bewijst dat de integriteitsgate een gemanipuleerde DLL, een gemanipuleerde of oude `Native.x86`, een verouderde pluginkopie, een verwijderd vermeld bestand, een onverwacht bestand, een gewijzigde of misvormde manifesthash, dubbele items (exact, via hoofdlettergebruik, via scheidingsteken), bovenliggende en absolute paden en ongeldige JSON weigert; dat de packager een oude `Native.x86` in de build-uitvoer weigert (zelfs met een nieuwere tijdstempel), evenals een ontvangstbewijs waarvan de bronnen zijn gewijzigd; en dat het gepubliceerde archief nooit wordt overschreven. Build-uitvoer wordt byte voor byte hersteld. Wordt uitgevoerd door `Invoke-OfflineValidation.ps1`. | Nee |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Pakt de ZIP uit naar `artifacts\release\identity-verification`, controleert `product`/`product_version`/`package_alias` en verifieert `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` van elke `.exe`/`.dll` behalve `nethost.dll` en `YamlDotNet.dll`, de `InternalName`/`OriginalFilename` van beide shims, en dat `OmsiLaunch.exe` een ingesloten pictogram bevat. | Nee |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Valideert het verpakte Release-uitvoerbare bestand tegen een echte installatie: het manifest moet `Release` zijn, mag geen paden `Debug` of `runtime/plugin/` bevatten en moet `plugins/OmsiLaunch.*` installeren; de vier assets voor het opstartscherm moeten bestaan. Voert drie gevallen met `/plan` uit (beheerd standaard, beheerd met aangepaste assets, `/splash:Unset`). Met `-RunOmsi` (vereist `-InstallPackage`) start het elk geval met `/observe-seconds`, bewaakt het `GUI\NewSplashscreen_ENG.bmp` en `GUI\NewSplashscreen_PTB.bmp` tijdens de sessie en controleert het op exact herstel, geen `Omsi.exe`, geen `journal.json`, exitcode `0`, ongewijzigde hashes van plugins van derden en een ongewijzigde set permanente plugins. Schrijft `.omsilaunch\diagnostics\release-presentation-validation.json`. | Ja met `-RunOmsi` (beperkt tot de sessie, hersteld) |

Beide scripts `Test-*` lezen `OmsiLaunch.Version.props` om de verwachte versie te kennen.
