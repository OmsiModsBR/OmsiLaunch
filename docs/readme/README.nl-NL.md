<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Sessiebesturing voor OMSI 2.</strong></p>
<p align="center">Open source · Programmeerbaar · Gedragen door de community</p>

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
  <a href="README.pl-PL.md">Polski</a> ·
  <strong>Nederlands</strong> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Dit is een vertaling van de canonieke [Engelse (US) README](../../README.md). Als de twee van elkaar afwijken, is de Engelse (US) README gezaghebbend.

# OmsiLaunch

**OmsiLaunch** is een opensourcelaag om OMSI 2 programmatisch te starten,
sessies te beheren en de draaiende simulatie te besturen. Het plant een sessie
op basis van een declaratieve beschrijving, voert elke tijdelijke
configuratiewijziging uit binnen een transactie met journal, start OMSI, volgt
het tot de gameplay begint, laat tools de draaiende simulatie via een publieke
API uitlezen en wijzigen, en herstelt elk bestand dat het heeft aangeraakt
wanneer de sessie eindigt.

Het is infrastructuur voor launchers, tools, automatisering en
community-integraties. Het is geen grafische launcher.

> **Definieer de sessie, niet de klikken.**

## Status: 0.1.0-beta4

De huidige publieke bèta is **`0.1.0-beta4`**. Beta 4 voegt compatibiliteit per
capability, Runtime Modules, sessieprofielen 1..99 en een reeks start- en
runtimefuncties toe. Compatibiliteit, de capabilitycatalogus, paginering,
getypeerde fouten en de modulebrug zijn waargenomen in live OMSI-sessies
(validatierondes A en B). De functies die na die rondes zijn toegevoegd, zijn
`EXPERIMENTAL`: afgesloten met statische analyse en alleen offline getest,
met runtimevalidatie nog in afwachting. Enkele zijn nog `STATICALLY_VALIDATED`,
`PARTIAL` of `UNAVAILABLE`. Twee runtime-items staan nog open omdat ze niet
veilig kunnen worden opgewekt: Steam LAA-gameplay en het natuurlijk verdwijnen
van wegvoertuigen en mensen (RV-002). De pagina
[runtimevalidatiestatus](../localized/nl-NL/status/runtime-validation-status.md) is
de gezaghebbende registratie van wat onder OMSI is uitgevoerd en wat alleen offline.

Het is een bèta: de publieke API, de CLI en de bestandsformaten zijn per member
gemarkeerd als `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` of
`UNAVAILABLE` en kunnen vóór 1.0 nog veranderen.

## Ondersteunde OMSI-reikwijdte

OmsiLaunch vereist een geïnstalleerde OMSI 2. Het is niet afhankelijk van één
specifiek `Omsi.exe`-bestand: elke native capability wordt binnen OMSI
afzonderlijk opgelost en geverifieerd voordat ze wordt gebruikt, en een
capability die op een build niet kan worden geverifieerd, is op die build niet
beschikbaar.

| Onderdeel | Reikwijdte |
| --- | --- |
| Vereiste | OMSI 2 geïnstalleerd: een 32-bits `Omsi.exe` met een OMSI 2-versie (2.x) en de mappen `maps\` en `Vehicles\`. Al het andere wordt geweigerd met `OL_E_UNSUPPORTED_BUILD`. |
| Bekende layout | OMSI 2.3.004 (`Omsi23004`). Elke capability wordt opgelost en elke runtimevalidatie is op deze layout uitgevoerd. Hashes identificeren de layout als snelle route; ze zijn geen vereiste. |
| Andere OMSI 2-builds | Elke capability wordt opgelost met buildonafhankelijke strategieën (Delphi RTTI, unieke codesignaturen, VMT-slots) en geverifieerd. Capabilities die objectlayouts lezen, vereisen een bekende layout. |
| Steam-executable | Wordt in het proces opgelost; een echte Steam-sessie is **niet** runtime-gevalideerd. |
| Besturingssysteem | Windows 10 of later, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (controller) en .NET 6 Runtime **x86** (de plugin draait binnen de 32-bits `Omsi.exe`). |

Details: [Compatibiliteit](../localized/nl-NL/reference/compatibility.md) en
[Installatie](../localized/nl-NL/getting-started/installation.md).

## Wat het biedt

**Sessies.** Een sessie wordt gepland op basis van een `LaunchSpec` (CLI-vlaggen,
een JSON-bestand, een sessieprofiel of de API), zonder neveneffecten gevalideerd
(`/plan`), daarna gestart, gevolgd via `SessionState`-overgangen en beëindigd.
Het stoppen van een sessie beëindigt OMSI geforceerd, zodat OMSI de bestanden
die op het punt staan te worden hersteld niet kan overschrijven. Zie
[Sessielevenscyclus](../localized/nl-NL/concepts/session-lifecycle.md).

**Transacties en recovery.** Elke configuratie-override geldt alleen voor de
sessie. OmsiLaunch maakt een snapshot van elk bestand dat het wijzigt, legt het
vast in het journal, past de wijziging toe, verifieert en herstelt het, ook na
een crash (`/recovery-status`, `/recover`, `RecoverPendingAsync`). Permanent
bewerken van de configuratie wordt niet aangeboden. Zie
[Transacties en recovery](../localized/nl-NL/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Een referentiefrontend boven op dezelfde publieke
API, zonder eigen OMSI-logica: detectie, planning, het starten van sessies en
clientopdrachten tegen een draaiende sessie. Zie de [CLI-referentie](../localized/nl-NL/reference/cli.md)
en de [CLI-voorbeelden](../localized/nl-NL/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** De host in het Windows-subsysteem voor snelkoppelingen.
Deze accepteert dezelfde opdrachtregel zonder consolevenster en meldt fouten in
berichtvensters. Zie [OmsiLaunchW.exe](../localized/nl-NL/reference/omsilaunchw.md).

**Windows-systeemvak.** Elke eigenaarsessie toont een pictogram in het systeemvak
(meldingsgebied) met een statusvenster en een actie "End session" (sessie
beëindigen). Die actie volgt hetzelfde stoppad als `session stop`. Zie
[Windows-systeemvak](../localized/nl-NL/reference/windows-tray.md).

**Sessieprofielen.** Declaratieve `profile.yaml`-pakketten
(`omsilaunch.session-profile/v1`) onder
`.omsilaunch\session-profiles\<id>\`, zodat makers van content reproduceerbare
sessies kunnen leveren die met één opdracht starten. Zie
[Sessieprofielen](../localized/nl-NL/reference/session-profiles.md).

**Publieke API en runtimebesturing.** `OmsiLaunch.Api` (`IOmsiLaunch`) is het
voorkeursoppervlak van het product. Runtime-operaties zoals tijd, weer, kaart,
camera, dienstregeling, voertuigen, mensen, scriptvariabelen en D3D-textures
gelden per sessie, worden gevalideerd tegen het buildprofiel en worden
aangesproken via ondoorzichtige semantische handles, nooit via native pointers.
Resultaten zijn begrensd door een runtime-slot van 64 KiB. Zie de
[Referentie publieke API](../localized/nl-NL/reference/public-api.md) en
[Runtimebesturing](../localized/nl-NL/reference/runtime-control.md).

**Local control.** Een named pipe per installatie, gebonden aan de actieve
`session_id`, laat andere processen van dezelfde gebruiker status en events
uitlezen, de sessie stoppen en publieke runtime-operaties uitvoeren. Zie
[Local control / IPC](../localized/nl-NL/reference/local-control.md).

**Capabilities.** Elke capability en elke publieke runtime-operatie is met haar
stabiliteit gecatalogiseerd. Experimentele en niet-beschikbare capabilities
worden vermeld, niet verborgen (`OmsiLaunch.exe capabilities`). Zie
[Capabilities](../localized/nl-NL/reference/capabilities.md).

**Permanente plugin.** De in-process plugin-closure wordt eenmalig onder
`plugins\OmsiLaunch.*` geïnstalleerd. Vóór elke start wordt deze gecontroleerd
tegen de SHA-256-items van `release-manifest.json`. Plugins van derden worden
nooit aangeraakt. Zie
[Model van de permanente plugin](../localized/nl-NL/concepts/permanent-plugin.md).

## Snelstart

Pak het releasepakket uit in de installatiemap van OMSI 2 en voer daarna de
volgende opdrachten uit vanuit die map:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Terwijl die sessie draait, kan een tweede console in dezelfde map haar
opvragen of beëindigen:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Stap voor stap: [Eerste sessie](../localized/nl-NL/getting-started/first-session.md). Voor
.NET-integrators: [Snelstart publieke API](../localized/nl-NL/getting-started/api-quick-start.md).

## Documentatie

De volledige documentatie is geïndexeerd in [`docs/localized/nl-NL/README.md`](../localized/nl-NL/README.md).
De Engelstalige (US) pagina's onder `docs/` zijn canoniek en normatief.

| Onderwerp | Pagina |
| --- | --- |
| Publieke API | [docs/localized/nl-NL/reference/public-api.md](../localized/nl-NL/reference/public-api.md) |
| CLI | [docs/localized/nl-NL/reference/cli.md](../localized/nl-NL/reference/cli.md) |
| CLI-voorbeelden | [docs/localized/nl-NL/reference/cli-examples.md](../localized/nl-NL/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/nl-NL/reference/omsilaunchw.md](../localized/nl-NL/reference/omsilaunchw.md) |
| Windows-systeemvak | [docs/localized/nl-NL/reference/windows-tray.md](../localized/nl-NL/reference/windows-tray.md) |
| Sessieprofielen | [docs/localized/nl-NL/reference/session-profiles.md](../localized/nl-NL/reference/session-profiles.md) |
| Runtimebesturing | [docs/localized/nl-NL/reference/runtime-control.md](../localized/nl-NL/reference/runtime-control.md) |
| Local control / IPC | [docs/localized/nl-NL/reference/local-control.md](../localized/nl-NL/reference/local-control.md) |
| Capabilities | [docs/localized/nl-NL/reference/capabilities.md](../localized/nl-NL/reference/capabilities.md) |
| Fouten en exitcodes | [docs/localized/nl-NL/reference/errors.md](../localized/nl-NL/reference/errors.md), [docs/localized/nl-NL/reference/exit-codes.md](../localized/nl-NL/reference/exit-codes.md) |
| Packaging | [docs/localized/nl-NL/reference/packaging.md](../localized/nl-NL/reference/packaging.md) |
| Bekende beperkingen | [docs/localized/nl-NL/reference/known-limitations.md](../localized/nl-NL/reference/known-limitations.md) |
| Runtimevalidatiestatus | [docs/localized/nl-NL/status/runtime-validation-status.md](../localized/nl-NL/status/runtime-validation-status.md) |

### Documentatie in andere talen

De documentatie is vertaald in 14 locales onder
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). De vertalingen
zijn gemaakt op basis van de canonieke Engelstalige (US) pagina's en er
mechanisch tegen gevalideerd. Een redactionele controle door moedertaalsprekers
maakte geen deel uit van de release van Beta 4 en kan na publicatie volgen. Als
een vertaling en de Engelse pagina van elkaar afwijken, is de Engelse pagina
gezaghebbend.

## Beperkingen en openstaande punten

Dit zijn de belangrijkste beperkingen. De volledige lijst staat in
[Bekende beperkingen](../localized/nl-NL/reference/known-limitations.md).

- **Steam.** Steam LAA is `PARTIAL`: gameplay, uitlezen, opdrachten en
  stoppen vereisen een echte Steam-installatie en zijn niet runtime-gevalideerd.
- **Experimenteel in deze bèta:** starten vanuit de laatste kaartstatus
  (`/last`) en een expliciete datum, tijd, jaar of expliciet weer bij de start
  zijn statisch afgesloten en wachten op runtimevalidatie. **Niet beschikbaar:**
  het toewijzen van een spelersvoertuig bij de start; wie dit aanvraagt, maakt
  het plan niet uitvoerbaar in plaats van dat het stilzwijgend wordt genegeerd.
- **Runtime-schrijfacties zijn beperkt.** `weather.set`, `weather.metar.set`,
  schrijfacties naar stringvariabelen en scripttriggers zijn experimenteel tot
  ze runtime-gevalideerd zijn; schrijfacties naar de kalender en het
  verplaatsen van voertuigen zijn niet beschikbaar. Runtimewijzigingen worden
  niet in het journal vastgelegd en niet hersteld.
- **Levensduur van handles.** Detectie van verouderde handles voor wegvoertuigen
  en mensen die op natuurlijke wijze verdwijnen (RV-002) heeft geen veilige
  runtimebron en is alleen offline gevalideerd.
- **Gepagineerde resultaten.** Runtimelijsten worden gepagineerd met `cursor` en
  `limit` en gaan verder bij `next_cursor`; een pagina die niet in de
  mailbox van 64 KiB past, wordt ingekort en gemarkeerd met `truncated=true`.
- **Stoppen is standaard geforceerd.** OMSI wordt beëindigd, de eigen
  afsluitprocedure van OMSI wordt niet uitgevoerd, en niet-opgeslagen
  OMSI-status gaat verloren. Het optionele coöperatieve afsluiten
  (`/cooperative-shutdown`, experimenteel) voert eerst de afsluitroutine van
  OMSI uit.
- **Vertrouwensmodel op basis van dezelfde gebruiker.** Elk proces van dezelfde
  Windows-gebruiker kan het lokale control plane bereiken.

## Download

Download **`OmsiLaunch-0.1.0-beta4.zip`** en het bijbehorende `.sha256`-bestand
van de [Releases-pagina](https://github.com/lmonteirotech/OmsiLaunch/releases).
Pak het direct uit in de ondersteunde OMSI-hoofdmap. Het pakket bevat de
controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe`), de afhankelijkheden daarvan,
de permanente plugin-closure met `release-manifest.json`, bestanden voor het
opstartscherm, een sessievoorbeeld en de offline documentatie onder
`.omsilaunch\docs\`. Indeling van het pakket en schoon verwijderen:
[Packaging](../localized/nl-NL/reference/packaging.md).

## Bouwen vanaf de broncode

Vereisten:

- Windows 10 of later, x64.
- .NET 6 SDK, met de x64- en x86-runtimes van .NET 6 om de tests uit te voeren.
- Visual Studio met de MSBuild C++-workload (platformtoolset `v145`) en een
  Windows 10 SDK, voor de drie native projecten: `OmsiLaunch.Native.x86`
  (Win32), en `OmsiLaunch.Bootstrapper` en `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` bevat de managed projecten en testsuites. Het ene offline
startpunt bouwt alles en voert elke offline suite uit; het start OMSI nooit:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` slaat de native projecten en de packaging-regressietest over.
`-SkipDocs` slaat de documentatiegates over. De builduitvoer komt in
`artifacts\`, dat niet wordt bijgehouden. `tools\New-ReleasePackage.ps1` stelt
een releasepakket samen uit een bestaande build, berekent de hashes, valideert
en comprimeert het. Zie [Packaging](../localized/nl-NL/reference/packaging.md) en
[Testen en validatie](../project/TESTING-AND-VALIDATION.md).

| Pad | Inhoud |
| --- | --- |
| `src/` | Productbibliotheken: API, core, configuratie, content, interop, proces, plugin, buildprofiel, native x86-grens |
| `tools/` | CLI en Windows-host (`OmsiLaunch.Cli`), native shims (`OmsiLaunch.Bootstrapper`), offline testhost, packaging- en validatiescripts, lokalisatietools |
| `tests/` | Testsuites voor unit-, integratie-, profiel-, Windows-UI- en documentatietests |
| `docs/` | Canonieke documentatie en de vertalingen ervan onder `docs/localized/` |
| `examples/` | LaunchSpec- en sessievoorbeelden |
| `assets/` | Branding, pictogrammen en pakketbestanden |
| `third_party/` | Herkomstnotities van upstream |

Samenvattingen voor beheerders: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Community en licentie

OmsiLaunch is een op de community gericht opensourceproject. Het is
onafhankelijk van andere OMSI-launchers, en compatibele communitytools kunnen
erop voortbouwen.

OmsiLaunch valt onder de licentie [LGPL-3.0-only](../../LICENSE). Zie de
[kennisgevingen van derden](../../THIRD-PARTY-NOTICES.md) voor de herkomst van de
opgenomen broncode en de toepasselijke kennisgevingen.
