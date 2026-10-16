# Windows-systeemvakindicator

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/windows-tray.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Elke zelfstandige eigenaarsessie (gestart met `OmsiLaunch.exe` of `OmsiLaunchW.exe`) toont een pictogram in het systeemvak (meldingsgebied) dat de sessie weergeeft en waarmee de gebruiker de sessie kan beëindigen. Deze pagina specificeert de indicator zoals die is geïmplementeerd door `SessionTrayIndicator`, `StatusWindow` en `StopConfirmationWindow` in `tools\OmsiLaunch.Cli\WindowsHost.cs`, de alleen-lezen presenter `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) en het stringregister `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), samen met de foutdialoogvensters van `OmsiLaunchW.exe` in `WindowsHost`. Het systeemvak is uitsluitend een presentatieadapter: het is geen eigenaar van OMSI of van de recovery, en de stopactie ervan signaleert hetzelfde canonieke stoppad van de eigenaar als `session stop` (zie de [CLI-referentie](cli.md) en [local control](local-control.md)).

<a id="when-the-icon-exists"></a>
## Wanneer het pictogram bestaat

| Stap | Gedrag |
|---|---|
| Aanmaak | Direct nadat `StartSessionAsync` terugkeert, voordat de sessie `Running` bereikt, tenzij `SuppressTrayIcon` is ingesteld. Het pictogram bestaat dus tijdens `StartingProcess`, `WaitingForPlugin`, `StartingWorld` en `EnteringGameplay`. |
| Opstartbudget | De UI-thread (`STA`, achtergrond, met de naam `OmsiLaunch tray`) moet het pictogram binnen 2 s publiceren. Anders, of bij een exceptie tijdens de aanmaak, wordt de indicator vrijgegeven en gaat de sessie verder **zonder** pictogram; `startup-timeout` of de exceptie wordt gelogd. Een trage start laat nooit een zichtbaar pictogram als wees achter. |
| Verwijdering | In het `finally`-blok van de eigenaar, nadat de sessie is voltooid of mislukt en vóór `CloseAsync`. Bij het vrijgeven wordt het afsluiten naar de UI-thread gepost (het menu, de bevestiging en het statusvenster worden gesloten, daarna wordt de berichtenlus beëindigd), wordt maximaal 2 s op de thread gewacht (bij overschrijding wordt `dispose-timeout` gelogd), en wordt de `NotifyIcon` verborgen en vrijgegeven. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (een `LaunchSpec`-veld, `Presentation.SuppressTrayIcon`, standaard `false`). In te stellen via `/spec` en de API; er is geen CLI-vlag. Integrators die hun eigen sessie-element in de gebruikersinterface tonen, stellen het in op `true`; verder verandert er niets aan de sessie. |
| Hosts | Zowel `OmsiLaunch.exe` (console) als `OmsiLaunchW.exe` (Windows-subsysteem) tonen het pictogram; `OmsiLaunchW.exe`-sessies die met `/silent` zijn gestart, hebben geen ander zichtbaar element. |

<a id="icon-and-tooltip"></a>
## Pictogram en knopinfo

- Pictogram: het pictogram dat aan het actieve uitvoerbare bestand is gekoppeld (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, het ingesloten OmsiLaunch-pictogram), met `SystemIcons.Application` als fallback.
- Tekst van de knopinfo: `Tray.Running` (`OmsiLaunch is running`; betekenis: OmsiLaunch is actief). De tekst verandert niet tijdens het stoppen (er is nog geen "stopping"-string; het pictogram blijft ongewijzigd tot de eigenaar het verwijdert).
- Visuele stijlen zijn ingeschakeld (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interactie

| Action | Resultaat |
|---|---|
| Rechtsklikken | Maakt het systeemvakvenster het voorgrondvenster (vereist voor menu's van meldingspictogrammen; zonder dit kan het menu klikken negeren en nooit sluiten terwijl OMSI op de voorgrond staat), en opent daarna het contextmenu op de werkelijke cursorpositie (`Cursor.Position`, niet de coördinaten van het event, omdat `NotifyIcon` `(0,0)` kan melden voor events die door de shell worden gehost). Het menu wordt binnen het werkgebied van het scherm onder de cursor gehouden. |
| Dubbelklikken | Opent het statusvenster (hetzelfde als het menu-item `Status`). |
| Linksklikken | Geen actie. |
| Menu-item `Status` (`Tray.Status`) | Opent (of activeert, als het al open is) het alleen-lezen statusvenster. |
| Scheidingslijn | |
| Menu-item `End session` (`Tray.EndSession`, toegankelijke beschrijving `Tray.EndSessionDescription`) | Opent het bevestigingsdialoogvenster (sessie beëindigen). |

<a id="status-window-read-only-snapshot"></a>
### Statusvenster (alleen-lezen snapshot)

Wordt geopend via het menu-item `Status` of door te dubbelklikken op het pictogram (`SessionTrayIndicator.ShowStatus`). Het is een gecentreerd dialoogvenster met vaste, automatisch bepaalde grootte, zonder item op de taakbalk en met één knop `Close` (sluiten; `Status.Close`; ook `Escape` sluit het venster). Als `Status` wordt gekozen terwijl het venster al open is, wordt dat venster geactiveerd zonder het opnieuw op te bouwen (het behoudt de snapshot van de eerste keer openen). Een fout bij het opbouwen van het venster wordt naar `tray-host.log` geschreven; dit heeft geen invloed op de sessie.

**Het is een snapshot, geen live weergave.** `SessionStatusPresenter.Create(plan, status, ui)` wordt één keer uitgevoerd wanneer het venster wordt geopend: de presenter leest het opgeloste `SessionPlan` van de sessie (de spec die werkelijk is gepland) en één `SessionStatus` (alleen de `State` daarvan). Zolang het venster open blijft, wordt niets vernieuwd, en OMSI wordt nooit bevraagd (geen runtime-operatie, geen telemetriewaarden). Sluit het venster en open het opnieuw om een nieuwere status te zien.

Venstertitel en kop: dezelfde tekst, `Status.SessionRunning` (`Session is running`; betekenis: de sessie is actief) wanneer `SessionStatus.State` `Running` is, anders de onbewerkte naam van de `SessionState` (bijvoorbeeld `WaitingForPlugin` bij openen tijdens het opstarten, omdat het pictogram al vóór `Running` bestaat). `Status.Title` (`OmsiLaunch session status`) is gedefinieerd in de stringtabel, maar wordt in deze release niet gebruikt.

De secties verschijnen in deze volgorde, en een sectie wordt weggelaten als ze geen velden heeft. Elke waarde komt uit de geplande `LaunchSpec`/`SessionPlan`, nooit uit OMSI.

| Sectie (Engels label) | Veld (Engels label) | Getoond wanneer | Waarde | Bron (openbaar equivalent) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | altijd | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (elke andere modus; wordt nooit bereikt, omdat `LastMapState` niet uitvoerbaar is) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | het plan heeft een `map`-contentidentiteit opgelost | de `DisplayName` van de kaart (de naam van de kaartmap, bijvoorbeeld `Grundorf`), anders de bestandsnaam van de identiteit zonder extensie | `SessionPlan.ResolvedContent`-item met `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` levert dezelfde `DisplayName` |
| `Session` | `Situation` | `SavedSituation` met een situatie-identiteit | bestandsnaam zonder extensie (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | er is een instappunt aangevraagd | de instappuntidentiteit als die is ingesteld (in deze release nooit uitvoerbaar), anders de getoonde index als geheel getal (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | er is een sessieprofiel gebruikt (`/predefined-profile`) | `name` van het profiel | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | zoals hierboven, als de preset een naam heeft | `name` van de preset | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | er is een expliciete datum, tijd of weersituatie of die van het systeem aangevraagd | `DD/MM/YYYY` of `System`; `HH:MM:SS` of `System`; ICAO-code, naam van een preset of `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | er is een veld van het spelersvoertuig aangevraagd | de aangevraagde identiteit/waarde | `Spec.PlayerVehicle` |
| `Configuration` | één veld per semantische instelling | er is een instelling ingesteld (`/set`, `settings` van een profiel, `LaunchSpec.Environment.*`) en die is bekend bij `ConfigurationCatalog` | de aangevraagde waarde; `%` toegevoegd bij sleutels die eindigen op `Percent`, ` m` bij sleutels die eindigen op `DistanceMeters`. Het label is de instellingssleutel waarbij elk door punten gescheiden deel met een hoofdletter begint (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; dezelfde waarden zijn de `PlannedMutations` van het plan |
| `Presentation` | `Splash` | altijd | `Managed` of `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | altijd | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

De sectie `Environment` verschijnt in een actieve Beta 4-sessie wanneer een datum, tijd, jaar of weersituatie is aangevraagd (die aanvragen zijn experimenteel, zie [bekende beperkingen](known-limitations.md)). De sectie `Vehicle` kan nooit verschijnen in een actieve sessie: het aanvragen van een willekeurig veld van het spelersvoertuig maakt het plan niet uitvoerbaar, zodat zo'n sessie nooit start. Ze bestaat voor een toekomstige build en wordt gedekt door de offline test van de presenter.

Runtimebewijs (pt-BR-UI, runtime-closure `T01`): een NEW_MAP-sessie op Grundorf toonde `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Dezelfde gegevens zijn zonder systeemvak beschikbaar voor tools: `session status` via het [lokale control plane](local-control.md) levert `SessionId`, `State`, `Diagnostics` en `RuntimeEvents` (live), en de uitvoer van `/plan --json` van de eigenaar (of `PlanSessionAsync`) levert de geplande spec, de opgeloste content en de geplande wijzigingen die het venster samenvat.

<a id="end-session-with-confirmation"></a>
### Sessie beëindigen (met bevestiging)

1. `StopConfirmationWindow`: titel `End session?` (sessie beëindigen?), bericht `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (OMSI 2 wordt gesloten en de door OmsiLaunch beheerde sessie eindigt), knoppen `End session` (standaard, `DialogResult.OK`) en `Cancel` (annuleren; `Escape`). Een tweede aanvraag terwijl het dialoogvenster open is, activeert het bestaande venster in plaats van er nog een te openen.
2. Bij `OK` roept het systeemvak `requestCanonicalStop` aan, dat het `controlStopped`-signaal van de eigenaar voltooit; de eigenaar roept vervolgens `StopAsync` aan: OMSI wordt met `TerminateProcess` beëindigd en elk bestand in bezit van de sessie wordt hersteld. Het systeemvak beëindigt OMSI nooit zelf.
3. Als de aanvraag een exceptie gooit, wordt de fout gelogd en wordt `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`; de sessie kon niet worden beëindigd, OMSI en de beheerde sessie blijven actief) getoond.
4. Het systeemvak bevestigt het succes niet; het pictogram verdwijnt wanneer de eigenaar klaar is met herstellen en de indicator vrijgeeft (runtime-closure `T01`: de eigenaar eindigde 607 ms nadat `End session` was bevestigd).
5. `Cancel` (of het sluiten van het dialoogvenster) doet niets: de sessie blijft actief (`T01`).
6. Een stop die van elders komt (`session stop`, Ctrl+C, `/observe-seconds`, het afsluiten van OMSI) terwijl het statusvenster of het bevestigingsdialoogvenster open is, sluit die vensters als onderdeel van het vrijgeven van de indicator; de eigenaar wacht niet op de gebruiker (`T02`: de eigenaar eindigde 725 ms na de stop via de pipe, met beide vensters open).

<a id="explorer-restart"></a>
## Herstart van Verkenner (Explorer)

`TrayWindow` is een verborgen native venster dat het vensterbericht `TaskbarCreated` registreert. Wanneer Verkenner (de shell) opnieuw start, verzendt deze dat bericht naar alle vensters en voegt de indicator het pictogram opnieuw toe (`Visible = false; Visible = true`). Runtime-closure `T01`: nadat `explorer.exe` was beëindigd en door Windows opnieuw was gestart, stond het pictogram weer in `Shell_TrayWnd` en bleven het menu en het statusvenster werken.

<a id="localization"></a>
## Lokalisatie

`WindowsUiStrings.Resolve` volgt de **Windows-UI-cultuur** (`CultureInfo.CurrentUICulture`), nooit de contenttaal van OMSI of de taal van een sessieprofiel. Volgorde van resolutie: exacte cultuurnaam, dan de taalcode van twee letters, dan Engels. Elke sleutel valt terug op Engels als een vertaling die sleutel mist.

| Cultuursleutels | Taal |
|---|---|
| `en`, `en-US`, `en-GB` | Engels (standaard en fallback) |
| `pt-BR` | Braziliaans Portugees. `pt-PT` (en kale `pt`) valt bewust terug op Engels. |
| `de`, `de-DE` | Duits |
| `fr`, `fr-FR` | Frans |
| `pl`, `pl-PL` | Pools |

De gelokaliseerde strings omvatten de knopinfo, de twee menu-items, het statusvenster (kop, sectietitels, veldlabels, `Close`), de waarden voor modus en presentatie, en het bevestigingsdialoogvenster. De terminologielijst wordt bijgehouden in `docs\windows-ui-localization.md`; de offline test `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifieert de resolutie en de presenter.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Foutdialoogvensters van `OmsiLaunchW.exe`

Wanneer `OMSILAUNCH_WINDOWS_HOST=1` (ingesteld door `OmsiLaunchW.exe`), vervangt `WindowsHost.ShowFailure` de foutuitvoer op de console door een modaal berichtvenster met de titel `OmsiLaunch` (foutpictogram): `<message>`, lege regel, `Code: OL_E_...`, lege regel, `See .omsilaunch\diagnostics for details.` Het wordt getoond door elke `CliInput.WriteError` (argumentfouten, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, geclassificeerde excepties), wanneer een startplan niet uitvoerbaar is (fallback `The session plan is not runnable.`; documentatie-audit BUG-06) en wanneer de sessie `Running` niet bereikt (`The OMSI session did not reach gameplay.` met de laatste `OL_E_`-diagnosemelding, of `OL_E_SESSION_START_FAILED` als er geen is). Het volledige gedrag van `OmsiLaunchW.exe` staat in de [referentie van OmsiLaunchW.exe](omsilaunchw.md). Onder `OmsiLaunch.exe` doet dezelfde functie niets. Een startfout van de .NET-host (shimcodes `100`..`106`) wordt door de native shim zelf getoond; zie [exitcodes](exit-codes.md).

<a id="log-location"></a>
## Loglocatie

`<root>\.omsilaunch\diagnostics\tray-host.log`, één regel per item: ISO-8601-UTC-tijdstempel, een tab, dan het item. Items: `created`, `removed`, `startup-timeout`, `startup-cancelled` (een vrijgave kruiste het opstarten en de lus werd overgeslagen), `dispose-timeout`, en volledige exceptieteksten bij UI-fouten. Het loggen gebeurt naar beste vermogen en gooit nooit een exceptie. De hostlogs van de sessie (`<sessionId>-host.log`) worden door de eigenaar in dezelfde map geschreven; de systeemvaklog heeft geen sessievoorvoegsel en wordt niet opgeschoond door de bewaarlimiet van 50 sessies.

<a id="lifecycle-guarantees"></a>
## Levenscyclusgaranties

- Het systeemvak is nooit eigenaar van de sessie: het kan OMSI niet starten, geen bestanden herstellen en het stoppad van de eigenaar niet omzeilen.
- Elk afsluitpad van de eigenaar (normale voltooiing, afsluiten van OMSI, Ctrl+C, sluiten van de console, stop via de pipe, exceptie, `/observe-seconds`) geeft de indicator vrij vóór `CloseAsync`, zodat geen pictogram langer bestaat dan zijn sessie, behalve wanneer het eigenaarproces abrupt wordt beëindigd (Windows verwijdert achtergebleven pictogrammen zodra de muis er de volgende keer overheen beweegt).
- Aanmaak en vrijgave worden onder een lock geserialiseerd: een vrijgave die de race wint, zorgt ervoor dat de UI-thread zijn berichtenlus overslaat en direct opruimt.
- Al het Windows Forms-werk gebeurt op de speciale STA-thread; andere threads posten er alleen naartoe via een verborgen marshalling-control.

<a id="residual-caveats-from-the-code-comments"></a>
## Resterende kanttekeningen (uit het commentaar in de code)

- Er bestaat geen knopinfotekst voor "stopping"; het pictogram toont `OmsiLaunch is running` tot het wordt verwijderd.
- `NotifyIcon` kan muiscoördinaten `(0,0)` melden voor events die door de shell worden gehost; in plaats daarvan wordt de cursorpositie gelezen.
- Systeemvakberichten komen nog steeds binnen terwijl het bevestigingsdialoogvenster modaal is; er wordt geen tweede bevestiging over de eerste heen geopend.
- Als de UI-thread niet binnen het vrijgavebudget van 2 s klaar is, gaat de eigenaar verder zonder te wachten (`dispose-timeout`).
- Het statusvenster is een snapshot van geplande waarden die bij het openen wordt gemaakt; het wordt niet vernieuwd en leest nooit uit OMSI.

<a id="runtime-evidence"></a>
## Runtimebewijs

Waargenomen in echte sessies op de geautoriseerde installatie tijdens de runtime-closure-ronde (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, pt-BR-Windows-UI; zie de [status van de runtimevalidatie](../status/runtime-validation-status.md)):

- Het pictogram wordt geregistreerd in het echte systeemvak (`Shell_TrayWnd`) onder `OmsiLaunchW.exe` en na het herstel verwijderd (`T01`..`T04`).
- "End session" met bevestiging voert de canonieke stop en het exacte herstel uit; `Cancel` laat de sessie actief (`T01`, `T03`).
- Het pictogram wordt na een herstart van Verkenner opnieuw aangemaakt (`TaskbarCreated`, `T01`).
- Het status- en het bevestigingsvenster worden door de eigenaar gesloten wanneer er een stop binnenkomt terwijl ze open zijn (`T02`).
- Foutdialoogvensters van `OmsiLaunchW.exe` voor een argumentfout (`OL_E_INVALID_ARGUMENT`), geen actieve sessie (`OL_E_NO_ACTIVE_SESSION`) en een sessie die mislukt vóór de gameplay (`OL_E_WORLD_START_FAILED`) (`T04`). Bij de laatste toont het dialoogvenster de foutpayload van de plugin als bericht.
- `/silent` koppelt los: het startprogramma keert terug terwijl de Windows-host de sessie aanhoudt (`T04`).
- Het `ProcessExit`-budget van 4 s bij het sluiten van de console (`L04`, console-eigenaar).

Niet geproduceerd: de dialoogvensters van de bootstrapper-shim voor exitcodes (`100`..`106`).
