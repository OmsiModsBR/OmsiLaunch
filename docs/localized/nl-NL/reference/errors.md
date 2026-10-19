# Fout- en diagnosecodes

<!-- l10n: source=reference/errors.md source-sha256=a3335bf22fcb58419c25d20745f040077113f1db8f973e0cb47d0469e852c5f9 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/errors.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina is de normatieve referentie voor elke code in `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 `OL_E_`-foutcodes en 4 `OL_W_`-waarschuwingen, gegroepeerd per catalogus-categorie, plus de informatieve diagnosecodes die geen fouten zijn. Per code staat beschreven waar de huidige code hem genereert, wat hij betekent, hoe hij bij je terechtkomt (gegooide exceptie, resultaatveld, diagnosemelding, antwoord van het control plane of CLI-envelope) en wat je moet doen. De betekenissen zijn afgeleid van de plaatsen waar de code wordt gegenereerd; waar een code wel gedefinieerd is maar momenteel door geen enkel pad wordt gegenereerd, vermeldt de pagina dat.

Gerelateerde pagina's: [openbare API](public-api.md), [exitcodes](exit-codes.md), [LaunchSpec](launchspec.md), [sessielevenscyclus](../concepts/session-lifecycle.md), [transacties en recovery](../concepts/transactions-and-recovery.md), [lokaal control plane](local-control.md), [runtimebesturing](runtime-control.md), [sessieprofielen](session-profiles.md), [permanente plugin](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Hoe codes bij je terechtkomen

| Kanaal | Betekenis |
| --- | --- |
| Gegooid | Een exceptie waarvan `Message` met de code begint (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). De CLI haalt de code uit het bericht en zet hem om naar een exitcode (`CliProgram.Classify`). |
| Plandiagnose | Een `LaunchDiagnostic` in `SessionPlan.Diagnostics`; elke `OL_E_`-code maakt `IsRunnable` false (CLI: plan `NOT RUNNABLE`, exit 1). |
| Sessiediagnose | Een `LaunchDiagnostic` in `SessionStatus.Diagnostics`; de sessiestatus is `Failed` (CLI exit 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` en `OL_E_RESTORE_FAILED` verpakken een binnenste code in hun bericht. |
| Runtimeresultaat | `RuntimeCommandResult.ErrorCode` met `Succeeded = false`. |
| Runtimedetail | `RuntimeCommandResult.ErrorCode` is de specifieke code en `Values["detail"]` het bericht (met `Values["exception"]`, het .NET-type). Beta 4: een getypeerde code aan de pluginkant bereikt de API en de CLI ongewijzigd; alleen een fout zonder gecatalogiseerde code wordt `OL_E_RUNTIME_OPERATION_FAILED`. |
| Control-antwoord | `ErrorCode` van een antwoord van het lokale control plane (`LocalControlResponse`). |
| CLI-envelope | `error.code` in de `--json`-envelope, of `<code>: <message>` op de console; de exitcode wordt vermeld. |
| Telemetrie | Een naam van een runtime-event die de host omzet naar een sessiediagnose. |

## Cli

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Een `OperationCanceledException` is ontsnapt (Ctrl+C of een geannuleerde wachtactie van de client). | CLI-envelope, exit 7 | Voer de opdracht opnieuw uit. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Een exceptie zonder `OL_E_`-code is ontsnapt: ongeldige `/spec`-JSON, een verplicht spec-lid dat null is, een onverwachte fout. | CLI-envelope, exit 10 | Lees het bericht en `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; corrigeer de invoer; meld het als het onverklaarbaar is. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Een `TimeoutException` zonder code is ontsnapt; of de local-control-client was verbonden met een eigenaar die niet binnen de timeout antwoordde (de eigenaar bestaat, dus dit wordt niet gemeld als `OL_E_NO_ACTIVE_SESSION`). (Timeouts van de mailbox dragen in plaats daarvan `OL_E_RUNTIME_REQUEST_TIMEOUT`.) | CLI-envelope, control-antwoord; exit 5 vanuit `Classify`, exit 7 voor een control-antwoord | Probeer het opnieuw; controleer of OMSI en de eigenaar reageren. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` staat niet naast `OmsiLaunch.exe`. | CLI-envelope, exit 7 | Installeer het pakket opnieuw. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` van `OmsiLaunchW.exe` leverde geen proces op. | CLI-envelope, exit 7 | Controleer de pakketbestanden en de rechten; voer de opdracht uit zonder `/silent` om de fout te zien. |

<a id="compatibility"></a>
## Compatibiliteit

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` bij `plugin.build.invalid` | De startcapabilities zijn opgelost, maar het live Start-formulier of een van zijn hooklocaties is niet in de verwachte toestand wanneer de plugin de headless start inschakelt (bijvoorbeeld doordat een andere plugin het al heeft gepatcht). | Sessiediagnose (`Failed`) | Verwijder de conflicterende plugin; zie [compatibiliteit](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` bij de start; runtime-operaties) | Een native capability die de aanvraag nodig heeft, kon op deze OMSI-build niet worden opgelost en geverifieerd en is dus niet beschikbaar. Bij de start mislukt de sessie vóór enige hook en worden de bestanden hersteld; een runtime-operatie wordt geweigerd zonder het geheugen van OMSI aan te raken. `Data["capability"]` (runtime) of de telemetriewaarde `capabilities` noemt haar. | Sessiediagnose (`Failed`); runtimeresultaat | Zie [compatibiliteit](compatibility.md): de capability is op deze build niet beschikbaar. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` niet beschikbaar) | De installatie is geen OMSI 2-installatie: `Omsi.exe` ontbreekt, is geen 32-bits x86 PE-image, heeft geen OMSI 2-versie (2.x), of de root heeft geen map `maps\` of `Vehicles\`. De hash van het uitvoerbare bestand is geen criterium. | Plandiagnose | Wijs OmsiLaunch naar de hoofdmap van een OMSI 2-installatie. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` niet beschikbaar); wordt ook gegooid door `CurrentWindowsX64Platform.ValidateCurrent`, die de service niet aanroept | Geen Windows 10 of later, of het besturingssysteem of het hostproces is niet x64. | Plandiagnose (CLI exit 3 wanneer gegooid) | Gebruik 64-bits Windows 10 of later. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Alleen `CurrentWindowsX64Platform.ValidateCurrent` | De architectuur van het besturingssysteem of de host is niet x64. De service roept `ValidateCurrent` niet aan; er is momenteel geen pad dat deze code genereert. | Alleen door die methode gegooid (`PlatformNotSupportedException`) | Zie de broncode `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

## Content

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validatie: NEW_MAP zonder `EntrypointIdentity` en met `PresentedEntrypointIndex` niet ingesteld of negatief. Planner: de kaart heeft geen instappunt met de aangevraagde identiteit of het exacte (hoofdlettergevoelige) label. | Plandiagnose | Stel `PresentedEntrypointIndex` in (`/entrypoint-index:<n>`) of een identiteit uit `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | Het startweer kan niet worden toegepast: een preset die geen bestand is in de map `weather` van de installatie, of een ICAO die geen vier letters of cijfers is. | Plandiagnose | Gebruik een geïnstalleerde preset of een geldige stationscode. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | Het aangevraagde instappunt deelt zijn label met een eerder record van de kaart; OMSI toont alleen het eerste record van elk label, dus het kan bij de start niet worden geselecteerd. | Plandiagnose | Kies het eerste record met dat label, of een ander instappunt. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` niet beschikbaar) | De NEW_MAP-kaart is gevonden, maar er is geen getoonde index en geen identiteit. Gaat altijd samen met `OL_E_ENTRYPOINT_NOT_FOUND`. | Plandiagnose | Idem. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` is geen geïnstalleerd `Vehicles\...\*.hof`-bestand. | Plandiagnose | Gebruik een identiteit uit `/list:hofs`. (Velden van het spelersvoertuig zijn op deze build hoe dan ook niet uitvoerbaar.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Een bron voor [HOF-beheer](hof-management.md) (collectie, map, voertuig of bestand) bestaat niet, bevat geen `.hof`, of mist een bestand dat in `Files` wordt genoemd. | Plandiagnose; startfout | Corrigeer de bron of de bestandslijst. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Een HOF-doelvoertuigbestand of -map is niet geïnstalleerd. | Plandiagnose; startfout | Gebruik een voertuigidentiteit uit `/list:vehicles` of een bestaande map onder `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Een HOF-pad verlaat de installatie, een doel staat niet onder `Vehicles\`, een collectienaam is geen gewone naam, of een junction/symlink wordt doorlopen. | Plandiagnose; startfout | Gebruik paden relatief aan de installatie zonder `..` en zonder links. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Twee HOF-bewerkingen schrijven verschillende bytes naar één bestand, of een mirror verwijdert een bestand dat een andere bewerking schrijft. | Plandiagnose; startfout | Wijzig de volgorde van conflicterende bewerkingen, splits of verwijder ze. |
| `OL_E_HOF_INVALID` | `HofPlanner` | HOF-bewerking zonder doelen of bron, een bronbestand dat geen `.hof` is, een doelbestand dat geen `.bus`/`.ovh` is, of een limiet voor grootte/aantal (8 MiB per bestand, 256 bestanden per bewerking, 256 MiB per sessie). | Plandiagnose; startfout | Corrigeer de bewerking. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validatie: NEW_MAP met `MapIdentity` niet ingesteld of niet in de vorm `maps\<dir>\global.cfg`. Planner: de kaart is niet geïnstalleerd. | Plandiagnose | Gebruik een identiteit uit `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Een `FileNotFoundException`/`DirectoryNotFoundException` zonder code is ontsnapt, bijvoorbeeld `/list:repaints` met een onbekende `/vehicle-scope`, of `/list:entrypoints` met een onbekende `/map`. | CLI-envelope, exit 6 | Corrigeer de identiteit. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` is geen `.cti`-item van het model (alleen gecontroleerd als `Model` is ingesteld). | Plandiagnose | Gebruik een identiteit uit `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | De kaart waarnaar de gekozen `.osn` verwijst, is niet geïnstalleerd. | Plandiagnose | Installeer de kaart of kies een andere situatie. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION zonder `SituationIdentity`, of de `.osn` is niet geïnstalleerd. | Plandiagnose | Gebruik een identiteit uit `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` is geen geïnstalleerd `Vehicles\...\*.bus`-bestand. | Plandiagnose | Gebruik een identiteit uit `/list:vehicles`. |

<a id="installation"></a>
## Installatie

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | De installatielease (`Local\OmsiLaunch.Installation.<hash>`) wordt vastgehouden door een andere eigenaar in deze aanmeldsessie, of een in het journal vastgelegd OMSI-proces (PID + aanmaaktijd + pad van het uitvoerbare bestand; of een willekeurige `Omsi.exe` uit de installatiemap bij een journal voorbij `HandoffCreated` zonder PID) is nog actief. | Start: sessiediagnose via `OL_E_START_SESSION`. Recovery: gegooid (`InvalidOperationException` / `IOException`). CLI exit 7. | Stop de andere eigenaar (`session stop`) of wacht tot OMSI is afgesloten, en probeer het dan opnieuw of voer `/recover` uit. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` is leeg. | Plandiagnose | Geef de installatiemap op. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` niet beschikbaar); ook `ValidateCurrent` | De installatiemap bestaat niet, heeft het kenmerk alleen-lezen of heeft geen map `plugins\`. | Plandiagnose | Verwijs naar een echte, beschrijfbare OMSI-installatie. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (plan en start) | Een geïnstalleerd `plugins\OmsiLaunch.*`-bestand wijkt af van de hash in `release-manifest.json` (of, zonder manifest, van de referentie-closure). | Plandiagnose (plan niet uitvoerbaar, CLI exit 1); sessiediagnose via `OL_E_START_SESSION` alleen als de bestanden tussen planning en start veranderen | Installeer het OmsiLaunch-pakket opnieuw, zodat `plugins\` en het manifest overeenkomen. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (plan en start) | Het manifest heeft geen item voor een vereist pluginbestand. | Plandiagnose; sessiediagnose via `OL_E_START_SESSION` bij dezelfde race als hierboven | Installeer het pakket opnieuw. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (plan en start) | Een vereist `plugins\OmsiLaunch.*`-bestand ontbreekt in de OMSI-installatie, of een `plugins/`-bestand dat in `release-manifest.json` staat, is niet geïnstalleerd. | Plandiagnose; sessiediagnose via `OL_E_START_SESSION` bij dezelfde race als hierboven | Installeer de plugin-closure van de permanente plugin ([installatie](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Alleen `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` false. Wordt niet door de service aangeroepen; er is momenteel geen pad dat deze code genereert. | Alleen door die methode gegooid | Zie de broncode. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` is leeg, is geen JSON, heeft geen `files`-array, of heeft een item zonder `path`/`sha256`, een hash die niet uit 64 hexadecimale cijfers bestaat, een pad dat absoluut is, `:` bevat, een leeg segment, een `.`- of `..`-segment bevat, of een pad dat twee keer voorkomt (hoofdletterongevoelig vergeleken, `/` en `\` gelijkwaardig). Een UTF-8-BOM wordt geaccepteerd. | Plan: verpakt in `OL_E_RUNTIME_ARTIFACT_MISSING`; start: via `OL_E_START_SESSION` | Installeer het pakket opnieuw. |

## InvalidArgument

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validatie: `Date.Value`/`Time.Value` ingesteld terwijl de modus niet `Explicit` is. CLI: onbekende vlag, ontbrekende waarde, ongeldig geheel getal of bereik, `/saved` gecombineerd met `/map`/`/entrypoint`, onbekende opdrachtroute, elke `ArgumentException`/`FormatException` zonder code. | Plandiagnose; CLI-envelope exit 2 | Corrigeer het argument. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (start) | De waarde van een semantische instelling valt buiten het bereik, is geen booleaanse waarde, zit niet in de toegestane set of is ongeldig opgebouwd (`graphics.particles` vereist vier velden). Waarden worden niet gevalideerd tijdens het plannen. | Sessiediagnose via `OL_E_START_SESSION` | Gebruik een waarde uit de [tabel met instellingen](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | De sleutel bestaat, maar is niet beschrijfbaar (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Plandiagnose; CLI exit 2 | Verwijder de sleutel. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | De sleutel staat niet in `ConfigurationCatalog`. | Plandiagnose; CLI exit 2 | Gebruik een sleutel uit de catalogus. |

## LaunchSpec

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | De root is geen JSON-object, of de deserialisatie leverde geen record op. | Gegooid (`InvalidDataException`), CLI exit 2 | Corrigeer het bestand ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | Het `/spec`-bestand bestaat niet. | Gegooid (`FileNotFoundException`), CLI exit 6 | Controleer het pad. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | Het bestand is groter dan 1 MiB. | Gegooid (`InvalidDataException`), CLI exit 2 | Maak het bestand kleiner. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Een lid dat op die positie geen openbare property van het record is; bericht `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Gegooid (`InvalidDataException`), CLI exit 2 | Verwijder het lid of geef het een andere naam. |

## LocalControl

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Handler van de eigenaar (`OwnerSession`) | De opdracht is niet `session.status`, `session.events`, `session.stop` of `runtime.execute` met een `operation`-argument. | Control-antwoord; CLI exit 7 | Gebruik een ondersteunde opdracht. |
| `OL_E_CONTROL_FAILED` | CLI-client (`ReportForwarded`, `CliEventWatch`) | De eigenaar antwoordde `Ok = false` zonder foutcode. | CLI-envelope, exit 7 | Lees het bericht; controleer de console/diagnostiek van de eigenaar. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | De handler van de eigenaar gooide een exceptie waarvan het bericht geen `OL_E_`-code bevat (bijvoorbeeld omdat de sessie al gesloten was), of het antwoord van de handler kon niet worden geserialiseerd. | Control-antwoord | Lees `session status`; start de eigenaar opnieuw als die verdwenen is. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (beide kanten) | Lengteprefix negatief of groter dan 64 KiB (inclusief een te groot request-frame), leeg frame, JSON `null`, een request zonder `Command`, of JSON die niet gedecodeerd kon worden. | Control-antwoord / CLI-envelope | Gebruik het gedocumenteerde protocol ([lokaal control plane](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (client) | Het eigen geserialiseerde request van de client is groter dan 64 KiB. Wordt aan de aanroeper gemeld; er wordt niets verzonden. | Control-antwoord / CLI-envelope | Maak het request kleiner. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (eigenaar) | Het antwoord van de eigenaar past niet in het frame van 64 KiB. De eigenaar antwoordt met deze getypeerde fout in plaats van het antwoord te laten vallen. `session.status` en `session.events` bereiken deze fout nooit: hun eventgeschiedenis wordt vanaf het oudste event ingekort tot ze past. | Control-antwoord / CLI-envelope | Probeer het opnieuw; lees events vaker uit. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | De `ProtocolVersion` van het request is niet `0.1`; het antwoord van de eigenaar kon niet worden gedecodeerd of was leeg; de eigenaar sloot de verbinding zonder te antwoorden of de verbinding werd verbroken nadat ze tot stand was gebracht; de eigenaar meldde geen `SessionId`. | Control-antwoord / CLI-envelope | Zorg dat de versies van client en eigenaar overeenkomen; lees `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Handler van de eigenaar | `session.stop` of `runtime.execute` zonder een `session_id` die gelijk is aan de actieve sessie. | Control-antwoord; CLI exit 7 | Lees eerst `session.status` en koppel het request daaraan (de CLI doet dit automatisch). |

<a id="other"></a>
## Overig

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | Het meegegeven plan heeft `IsRunnable = false`, of de herplanning bij de start is niet uitvoerbaar (`Omsi.exe` gewijzigd, content verwijderd, plugin-closure ontbreekt); het bericht vermeldt de huidige `OL_E_`-codes. | Gegooid (`InvalidOperationException`); CLI exit 1 | Plan opnieuw en los de vermelde diagnosemeldingen op. |

<a id="presentation"></a>
## Presentatie

Allemaal gegenereerd door `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Tijdens het plannen worden ze verpakt in `OL_E_SESSION_PRESENTATION_INVALID` (het bericht bevat de code); bij de start komen ze naar buiten via `OL_E_START_SESSION`.

| Code | Betekenis / typische oorzaak | Wat te doen |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | Het `.itx`-bestand is leeg, heeft een oneven aantal niet-lege regels, of een URL-regel is geen absolute `http`/`https`-URL. | Gebruik paren van een URL-regel en een doelregel. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (opgelost ten opzichte van de werkmap van het proces) bestaat niet. Gegooid als `FileNotFoundException`. | Geef een bestaand `.itx`-pad op. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` is `Override` zonder `OverrideProfilePath`. CLI exit 2 wanneer gegooid. | Geef `/internet-textures-profile:<file.itx>` op. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Een doelregel is absoluut, bevat `..`, begint met `\`, verwijst naar een locatie buiten de installatie, mist een `Texture\`-component of loopt via een junction/symlink. | Gebruik relatieve doelen van de vorm `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` bestaat niet. | Corrigeer de map. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` of `<LANG>.bmp` ontbreekt in de assetmap, of een meegeleverde `assets\splash\<LANG>.bmp` ontbreekt bij het vullen van `.omsilaunch\assets\splash`. | Lever de BMP-bestanden aan / installeer het pakket opnieuw. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Een BMP voor het opstartscherm is geen 24-bits `BM`-bitmap van 640×480. | Converteer de afbeelding. |

<a id="process"></a>
## Proces

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (foutpaden bij startfout en supervisor) | Het beëindigen van of wachten op OMSI tijdens het opruimen na een fout gooide een exceptie; het binnenste bericht volgt. | Sessiediagnose (toegevoegd aan een `Failed`-sessie) | Zorg dat er geen `Omsi.exe` meer actief is, en voer daarna `/recover` uit als er een journal openstaat. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` mislukte direct na `CreateProcessW` (`Win32=<code>`); het proces wordt beëindigd. | Sessiediagnose via `OL_E_START_SESSION` | Probeer het opnieuw; controleer antivirus/rechten. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI werd afgesloten vóór `gameplay.entered` (crash, een foutdialoogvenster van OMSI werd gesloten, het venster werd gesloten). | Sessiediagnose (`Failed`); het herstel wordt uitgevoerd | Controleer de eigen logs van OMSI en `logfile.txt`; bekijk `RuntimeEvents` voor het laatste plugin-event. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` is mislukt (`Win32=<code>` in het bericht). | Sessiediagnose via `OL_E_START_SESSION` | Los de Win32-fout op (ontbrekend bestand, toegang geweigerd, beleid). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | De supervisorlus gooide een exceptie (telemetrie lezen, wachten op/beëindigen van het proces, journal schrijven); OMSI wordt beëindigd en er wordt geprobeerd te herstellen. | Sessiediagnose (`Failed`) | Lees het binnenste bericht en de hostlog. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` is mislukt (`Win32=<code>`). | Binnen berichten van `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Beëindig OMSI handmatig en voer daarna `/recover` uit. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` op de proceshandle is mislukt. | Binnen berichten van `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Idem. |

## Runtime

"Runtimedetail" betekent de specifieke code in `ErrorCode`, met het bericht in `Values["detail"]` en het uitzonderingstype in `Values["exception"]`.

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` met `preset` terwijl `family` 2 (extern) of 3 (kaart) is; presets bestaan alleen voor chauffeur (0) en passagier (1). | Runtimedetail | Laat `preset` weg of gebruik family 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | `Date`/`Time`-modus `Explicit` zonder waarde of met componenten buiten het bereik. De naam is historisch; het is een validatiefout tijdens het plannen. | Plandiagnose | Corrigeer de waarde (en houd er rekening mee dat een expliciete datum/tijd op deze build niet uitvoerbaar is). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: het `.bus`-pad bestaat niet onder de werkmap van OMSI (gecontroleerd vóór de native aanroep, zodat OMSI geen fallback kan invullen). | Runtimedetail | Gebruik een identiteit uit `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | idem | De native MakeVehicle-aanroep heeft de wegvoertuigcollectie met meer dan één object gewijzigd. | Runtimedetail (`native_status`, aantallen in het bericht) | Meld het; de aangemaakte objecten blijven bestaan tot de sessie eindigt. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | idem | De collectie is niet gewijzigd; OMSI heeft het voertuig zonder melding geweigerd. | Runtimedetail | Controleer het `.bus`-bestand; probeer een ander model. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | idem | Elke andere native status dan nul. | Runtimedetail | Meld het, met de aantallen uit het bericht. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | De geprofileerde PlaceRandomBus-aanroep gaf een foutstatus terug. | Runtimedetail | Probeer het opnieuw zodra de gameplay stabiel is; meld het. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; controles aan de pluginkant (`time.set` zonder `hour`/`minute`/`second`; `camera.set` zonder `family`/`field_of_view`; `camera.lock` zonder parseerbare `family`; voertuig-/curve-operaties) | Een vereist argument ontbreekt of is leeg. | Runtimeresultaat (register; CLI exit 2) of runtimedetail (plugin) | Geef het argument op ([runtimebesturing](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | De referentiemap/-bestanden van de plugin-closure of de native brug die in `OmsiLaunchRuntimePaths` zijn opgegeven, kunnen niet worden geladen (kan `OL_E_RELEASE_MANIFEST_INVALID` verpakken). | Plandiagnose | Voer uit vanuit een intact pakket. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, INTERNAL harness) | De baseline-`time.read`/`camera.read` is mislukt, dus de schrijftest is overgeslagen. | Alleen batchartefact | Niet bedoeld voor gebruikers. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` is leeg, langer dan 240 tekens, bevat NUL of `..`, begint niet met `Vehicles\` of eindigt niet op `.bus`. | Runtimedetail | Geef `Vehicles\<dir>\<file>.bus` op. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | geen (behouden voor compatibiliteit) | Wordt niet meer gegenereerd. Eerdere builds genereerden deze code wanneer een geannuleerd request in het slot achterbleef; elk eindpad van een request zet het slot nu terug, en een achtergebleven request of antwoord dat aan het begin van een nieuw request wordt aangetroffen, wordt gewist. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | De mailbox is vrijgegeven omdat de sessie aan het eindigen is. | Gegooid (`InvalidOperationException`) | Niets; de sessie is voorbij. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | Het mailbox-slot bevatte een statuswaarde die niet idle, requested of responded is (corruptie). Het slot wordt teruggezet en de fout wordt gegenereerd; het volgende request werkt normaal. | Gegooid (`InvalidDataException`) | Probeer het opnieuw; meld het als het blijft optreden. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | De pointer naar het constantenblok van het voertuig is null. | Runtimedetail | Het voertuig heeft geen constanten; er hoeft niets te gebeuren. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` staat niet in de constantentabel van het voertuig. | Runtimedetail | Vraag eerst de lijst met constanten op. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | Het door spawn aangemaakte object heeft een VMT buiten het adresbereik van de OMSI-image. | Runtimedetail | Meld het. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | idem | Het aangemaakte object staat niet in de wegvoertuigcollectie. | Runtimedetail | Meld het. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Twee opeenvolgende curvepunten hebben dezelfde X. | Runtimedetail | Contentprobleem in de curve van het voertuig. |
| `OL_E_RUNTIME_CURVE_EMPTY` | idem | De curve heeft geen punten. | Runtimedetail | Idem. |
| `OL_E_RUNTIME_CURVE_INVALID` | idem | Geen enkel segment van de curve bevat `x`. | Runtimedetail | Evalueer binnen het domein van de curve. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | idem | `name` is onbekend of de functiepointer ervan is null. | Runtimedetail | Vraag eerst de lijst met curves op. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | De pointer naar de voertuigdefinitie is null. | Runtimedetail | De handle verwijst naar een voertuig zonder definitiegegevens. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (eigenaarmodus) | `plugins\OmsiLaunch.Plugin.opl` of `plugins\OmsiLaunch.Native.x86.dll` ontbreekt naast het uitvoerbare bestand. | CLI-envelope, exit 7 | Installeer het pakket opnieuw. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` ontbreekt of is leeg voor `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (het register weigert deze normaal gesproken eerst met `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Runtimedetail | Geef de handle op. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | De handle is onbekend, het object heeft de collectie verlaten, de adresgeneratie is opgehoogd, of de vingerafdruk van het object (VMT + definitie-/modelidentiteit) is veranderd doordat het adres opnieuw is gebruikt. Resterende blinde vlek: dezelfde klasse en hetzelfde model die tussen twee lijstuitlezingen op hetzelfde adres opnieuw worden aangemaakt. | Runtimedetail | Voer `road-vehicles.list`/`humans.list` opnieuw uit en gebruik de nieuwe handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; `D3DRuntimeApi`-fallback | Generieke wrapper voor fouten aan de pluginkant; `Values["detail"]` bevat het bericht (vaak een specifiekere code) en `Values["exception"]` het type exceptie. Een exceptie die binnen de mailbox-dispatcher uit een operatie ontsnapt, wordt ook met deze code beantwoord (zonder waarden), in plaats van het request onbeantwoord te laten. | Runtimeresultaat | Lees `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | De plugin heeft geen implementatie voor een operatie die het register wel toestond (versieverschil tussen register en plugin). | Runtimedetail | Installeer een consistent pakket. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | De operatie staat niet in `PublicRuntimeOperationIds`; dat geldt ook voor elke `internal.*`-operatie. Gecontroleerd vóór het opzoeken van de sessie. | Runtimeresultaat; control-antwoord; CLI exit 2 | Gebruik een openbare operatie-id. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` met `preset` terwijl er geen spelersvoertuig is (headless sessies hebben er geen). Wordt ook gemeld als `code` van het `camera.lock.degraded`-event wanneer het opnieuw toepassen mislukt. | Runtimedetail / runtime-event | Vergrendel zonder preset, of gebruik een sessie met een spelersvoertuig. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Een geslaagd D3D-resultaat had geen waarden, of een onbekende apparaatstatusstring. | Gegooid (`OmsiRuntimeException`) | Zorg dat de versies van host en plugin overeenkomen. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | Het slot bevat een verouderd antwoord met dezelfde request-id als het nieuwe request. Het verouderde antwoord wordt gewist voordat de fout wordt gegenereerd. | Gegooid (`InvalidOperationException`) | Gebruik strikt oplopende request-id's. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Geen antwoord binnen `timeout`; het slot wordt teruggezet en een laat antwoord wordt genegeerd. | Gegooid (`TimeoutException`); CLI exit 5 | Probeer het opnieuw met een langere timeout; controleer of OMSI niet geblokkeerd is (modaal dialoogvenster, laden). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | De antwoord-envelope is corrupt, heeft een ongeldige lengte (negatief, nul of groter dan het slot), een sessie-id van een andere sessie of een andere request-id. Het slot wordt teruggezet voordat de fout wordt gegenereerd, zodat het volgende request normaal werkt. | Gegooid (`InvalidDataException`) | Probeer het opnieuw; meld het als het blijft optreden. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | De `cursor` van een lijstoperatie is onjuist gevormd of hoort bij een ander soort lijst. | Runtimedetail | Geef de `next_cursor` van de vorige pagina van dezelfde operatie door, of laat hem weg. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | Het geserialiseerde resultaat is groter dan de mailbox van 64 KiB. Resultaten van gepagineerde lijsten worden in plaats daarvan ingekort tot ze passen en gaan verder met `next_cursor`; geen enkel lijstresultaat bereikt deze code. | Runtimeresultaat | Gebruik een beperktere operatie of een kleinere `limit`. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | De pointer naar de scriptdefinitie of de scriptstatus van het voertuig is null. | Runtimedetail | Het voertuig heeft geen scriptobjecten. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; plugin-mailbox | `RuntimeCommand.SessionId` wijkt af van de sessie-id van de handle (gegooid door de host), of een request bereikte een plugin die aan een andere sessie is gekoppeld (door de plugin teruggegeven als getypeerd resultaat). | Gegooid (`InvalidOperationException`) / runtimeresultaat | Stel de opdracht op met `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` van het event `world.failed`) | Het startweer kon niet worden toegepast nadat de wereld was gestart: OMSI heeft voor `/weather-real` geen ICAO-station ingesteld, of zijn weerobjecten zijn niet beschikbaar. De sessie mislukt. | Sessiefout | Stel een station in OMSI in of gebruik `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Een sleutel van `weather.set` noemt een waarde die OMSI bij elke weertick afleidt (luchtvochtigheid, lichtfactoren, wolkentransparantie, neerslagvlag, natte grond); er wordt niets geschreven. | Runtimeresultaat | Stel de bronsleutel in die in `detail` wordt genoemd (bijvoorbeeld `dew_point` in plaats van `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Onbekende sleutel van `weather.set`; er wordt niets geschreven. | Runtimeresultaat | Gebruik een sleutel uit [capabilities](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` staat niet in de tabel met stringvariabelen. | Runtimedetail | Vraag eerst de lijst met stringvariabelen op. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Een booleaanse weerwaarde is niet `true`/`false`/`1`/`0`. Momenteel onbereikbaar (zie hierboven). | Runtimedetail (gedefinieerd) | Zie de broncode. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` geen geheel getal, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` mag -1 zijn), `scheduled` 0..1; `vehicle.variable.set`: `value` niet eindig; `vehicle.curve.evaluate`: `x` niet eindig. | Runtimedetail | Gebruik een waarde binnen het bereik. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` staat niet in de tabel met numerieke variabelen. | Runtimedetail | Vraag eerst de lijst met variabelen op. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | Het variabeleslot of het adres van de waarde is null. | Runtimedetail | De variabele is voor dit voertuig niet gematerialiseerd. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | De klokwaarden zijn geschreven, maar de geprofileerde native SetTime-aanroep gaf een fout terug. | Runtimedetail | Probeer het opnieuw; lees de waarde terug met `time.read`. |

## RuntimeD3D

Allemaal gegenereerd door `CurrentRuntimeControl` (`ThrowD3D`-toewijzing van de native status; `detail` bevat de operatie en de HRESULT, `native_status` de numerieke status). Kanaal: runtimeresultaat met de code in `ErrorCode`; `D3DRuntimeApi` gooit dit opnieuw als `OmsiRuntimeException`.

| Code | Native status / oorzaak | Wat te doen |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: het Direct3D-apparaat is verloren gegaan. | Wacht op `d3d.restored`; maak de textures opnieuw aan (de generatie is veranderd). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: ontbrekende of ongeldige `width`, `height`, `level`, `x`, `y`, `format` of `handle` (bereiken: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Corrigeer de argumenten. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` is geen geldige Base64 of is groter dan 48 KiB. | Stuur kleinere rechthoeken. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, of een onbekende `format`-naam (geldig: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Gebruik een van de vermelde formaten. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Elke andere status; de HRESULT staat in `detail`. | Meld het, met de HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: het apparaat is niet gereed (vóór het eerste frame of tijdens het stoppen). | Probeer het opnieuw na `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: er wordt een apparaatreset uitgevoerd. | Probeer het opnieuw na `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: de texturehandle was al vrijgegeven. | Gebruik vrijgegeven handles niet opnieuw. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: de handle hoort bij een vorige apparaatgeneratie; of de handlestring is niet `d3dtex-<session>-<hex>` / is nul. | Maak de texture opnieuw aan. |

<a id="session"></a>
## Sessie

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` bij `plugin.request.unsupported` | Plan: `LastMapState`, `EntrypointIdentity`, datum-/tijd-/jaarmodus, weermodus, velden van het spelersvoertuig of invoerdocumenten aangevraagd (`Requested capability unavailable: <name>`). Telemetrie: de plugin heeft de handoff geweigerd (kan niet gebeuren bij een uitvoerbaar plan). | Plandiagnose; sessiediagnose | Verwijder de niet-ondersteunde aanvraag ([bekende beperkingen](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` bij `headless.arm.failed` | De plugin kon de eenmalige hook voor de headless start in OMSI niet activeren. | Sessiediagnose (`Failed`) | Controleer de build; meld het. |
| `OL_E_NO_ACTIVE_SESSION` | CLI-client (`ReportForwarded`, `CliEventWatch`) | Geen enkele eigenaar antwoordt op de control-pipe van de installatie (geen sessie, of de eigenaar is nog aan het starten/valideren). | CLI-envelope, exit 4 | Start een sessie, of wacht tot die `Running` is. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` is verstreken vóór `plugin.started` (OMSI heeft `plugins\OmsiLaunch.Plugin.opl` niet geladen, of blijft hangen vóór de initialisatie van de plugin). | Sessiediagnose (`Failed`) | Controleer de plugin-closure, `plugins\OmsiLaunch.Plugin.opl` en `logfile.txt` van OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` bij `plugin.handoff.invalid` of niet-parseerbare telemetrie-JSON | De plugin kon de opstart-handoff niet lezen/verifiëren (versies 3 tot en met 6, SHA-256), of stuurde ongeldige telemetrie. | Sessiediagnose (`Failed`) | Zorg dat de versies van host en plugin overeenkomen (installeer het pakket opnieuw). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Er beantwoordt al een eigenaar `session.status` voor deze installatie. | CLI-envelope, exit 7 | Gebruik clientopdrachten (`session status`, `session stop`, runtimeopdrachten). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | De sessiestatus is niet `Running`. | Gegooid (`InvalidOperationException`) | Roep eerst `WaitForAsync(session, SessionState.Running, ...)` aan. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Het plan voor opstartscherm/ITX kon niet worden opgebouwd; het bericht bevat de presentatiecode. | Plandiagnose | Zie [Presentatie](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (dialoogvenster van OmsiLaunchW) | Fallbackcode die wordt getoond als een startplan niet uitvoerbaar is of de sessie de gameplay niet heeft bereikt, en er geen `OL_E_`-diagnosemelding bestaat. | Alleen berichtvenster | Lees `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` bij `world.situation.failed` | De native start van de opgeslagen situatie gaf een fout terug (`native_status` in het event). | Sessiediagnose (`Failed`) | Controleer de `.osn` en de bijbehorende kaart. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | De plugin is gestart, maar `Running` werd niet binnen `StartupTimeoutSeconds` bereikt. | Sessiediagnose (`Failed`) | Verhoog `/startup-timeout` voor grote kaarten; controleer `RuntimeEvents` op het laatste world-event. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Elke exceptie in het startpad; het bericht is het binnenste bericht (dat meestal met de binnenste code begint). | Sessiediagnose (`Failed`) | Handel op basis van de binnenste code. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` bij `world.failed` | De native NEW_MAP-start gaf een fout terug (`native_status` in het event). | Sessiediagnose (`Failed`) | Controleer de kaart, de instappuntindex en de logs van OMSI. |

## SessionProfile

Allemaal gegenereerd door `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) of `CliInput`, gegooid als `SessionProfileException` (een `IOException` met `Code`), CLI exit 2. Zie [sessieprofielen](session-profiles.md).

| Code | Betekenis / typische oorzaak | Wat te doen |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | De `assets`-map voor het opstartscherm of het `profile`-bestand voor internettextures van de preset bestaat niet binnen het pakket. | Voeg de asset toe. |
| `OL_E_SESSION_PROFILE_INVALID` | Schending van de structuur of van een limiet: meer dan 256 KiB, niet precies één root-mapping, YAML-anchors, onbekende sleutel, ontbrekende verplichte sleutel, niet-scalaire waarde waar een scalaire waarde vereist is, `id` wijkt af van de mapnaam, presets niet 1..99 of dubbele `index`, niet-positieve timeouts, niet-ondersteunde modus voor weer/opstartscherm/internettextures, datum/tijd niet `explicit`, ongeldige YAML, parseerfouten bij getallen/datums. | Corrigeer de YAML volgens het bericht. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` is niet leeg en bevat niet de gekozen kaart (NEW_MAP) of de kaart van de gekozen situatie (SAVED_SITUATION). | Kies een compatibele wereld. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` bestaat niet. | Controleer de id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Een expliciet CLI-argument is gericht op een veld dat in bezit is van het gekozen profiel/de gekozen preset. | Laat de vlag weg of kies een andere preset. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | De id bevat `\`, `/`, `:` of `..`; of een assetpad is absoluut, verlaat het pakket of loopt via een junction/symlink. | Houd paden binnen het pakket. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` ontbreekt, valt buiten 1..99 of is niet door het profiel gedeclareerd. | Gebruik een gedeclareerde presetindex. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` is niet `omsilaunch.session-profile/v1`. | Gebruik het ondersteunde schema. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Een instellingssleutel van een preset is bekend, maar niet beschrijfbaar. | Verwijder de sleutel. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Een instellingssleutel van een preset staat niet in de catalogus. | Gebruik een sleutel uit de catalogus. |

<a id="transaction"></a>
## Transactie

Zie [transacties en recovery](../concepts/transactions-and-recovery.md).

| Code | Gegenereerd door | Betekenis / typische oorzaak | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Een verouderde `closecheck` bestaat nog na `File.Delete`. | Sessiediagnose via `OL_E_START_SESSION` | Verwijder `<root>\closecheck` handmatig (rechten). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Een pad dat vóór de sessie niet bestond, bevat nu content die afwijkt van wat de sessie heeft toegepast; het wordt niet verwijderd en het journal blijft behouden. | Gegooid (`IOException`); binnen `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; CLI exit 8 | Inspecteer het bestand; verwijder of verplaats het en voer daarna `/recover` uit. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (journal van vóór de vingerafdrukken) | Het journal heeft geen vingerafdruk van de toegepaste content voor een oorspronkelijk afwezig pad dat geen verwijderpad is, dus eigendom kan niet worden aangetoond. Bij de start van een sessie wordt de recovery uitgesteld en opnieuw geprobeerd met de geplande bytes van deze sessie; via `RecoverPendingAsync` wordt de code gegooid. | Gegooid (`IOException`); CLI exit 8 | Start een sessie met dezelfde spec (die levert de bytes), of inspecteer en verwijder het bestand, en voer daarna `/recover` uit. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | De SHA-256 van een back-up komt niet overeen met de snapshot die in het journal is vastgelegd; er wordt niets geschreven. | Gegooid; binnen `OL_E_RESTORE_FAILED`; CLI exit 8 | Zet het bestand terug vanuit je eigen back-up; verwijder het journal pas als je het zeker weet. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Er bestaan snapshots in het geheugen, maar `journal.json` is verdwenen (tijdens de sessie verwijderd). | Gegooid; binnen `OL_E_RESTORE_FAILED` | Controleer de sessiebestanden handmatig. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` bestaat nog na het verwijderen (het herstel zelf is geslaagd en geverifieerd). | Gegooid; binnen `OL_E_RESTORE_FAILED`; CLI exit 8 | Verwijder `<root>\.omsilaunch\journal.json` (rechten) of voer `/recover` opnieuw uit (idempotent). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (startfout / supervisor) | Het afsluiten van OMSI kon niet worden bevestigd, dus zijn bestanden niet vervangen zolang OMSI ze mogelijk nog gebruikt; het journal blijft behouden. | Sessiediagnose (`Failed`) | Voer `/recover` uit nadat `Omsi.exe` is afgesloten (of laat de volgende start het herstel automatisch uitvoeren). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (startfout / supervisor) | `RestoreAsync` gooide een exceptie; het bericht bevat de binnenste code; het journal blijft behouden. | Sessiediagnose (`Failed`); CLI exit 8 wanneer gegooid vanuit `/recover` | Handel op basis van de binnenste code en voer daarna `/recover` uit. |

## Ui

Codes van de operaties van het native pauzemenu (`ui.pause.*`).

| Code | Gegenereerd door | Betekenis | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_UI_NOT_PAUSED` | `CurrentRuntimeControl` | De operatie vereist dat OMSI gepauzeerd is met het native pauzemenu zichtbaar (overname, actie), en dat is niet zo. | Runtimeresultaat | Wacht tot de gebruiker OMSI heeft gepauzeerd. |
| `OL_E_UI_BUSY` | `CurrentRuntimeControl` | Een actie van het pauzemenu is ingepland of wordt uitgevoerd (ze kan in een native modaal dialoogvenster staan), of de aanroep kwam binnen het frame van OMSI. | Runtimeresultaat | Poll `ui.pause.state` totdat `busy` `false` is en probeer het opnieuw. |
| `OL_E_UI_ALREADY_TAKEN_OVER` | `CurrentRuntimeControl` | `ui.pause.take-over` terwijl het native pauzemenu al is overgenomen. | Runtimeresultaat | Gebruik `ui.pause.state`, of eerst `ui.pause.restore-native`. |

<a id="warning"></a>
## Waarschuwing

| Code | Gegenereerd door | Betekenis | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | Het OMSI 2-uitvoerbare bestand heeft geen bekende indeling (zijn code-identiteit staat niet in de buildcatalogus). Het plan blijft uitvoerbaar: elke native capability wordt in het proces opgelost en geverifieerd, en een capability die niet kan worden geverifieerd, is niet beschikbaar. | Plandiagnose (uitvoerbaarheid niet beïnvloed) | Niets nodig; controleer `/plan` en `profiles` voor de voorspelde capabilities. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (supervisor) | `Behavior.CooperativeShutdown` was aangevraagd, maar OMSI kon niet worden gevraagd af te sluiten (zijn afsluitroutine is op deze build niet opgelost, of OMSI sluit al af), of het sloot niet af binnen `ShutdownTimeoutSeconds`: OMSI is in plaats daarvan beëindigd. Het herstel draait zoals gebruikelijk. | sessiediagnose | Niets: de sessie eindigt zoals bij een geforceerde beëindiging. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Een capability die de start nodig heeft, kon niet uit het uitvoerbare bestand worden opgelost (bijvoorbeeld een DRM-beschermd uitvoerbaar bestand waarvan het geladen image van het bestand verschilt). Ze wordt bij de start in het proces opgelost. | Plandiagnose (uitvoerbaarheid niet beïnvloed) | Niets nodig; een start mislukt met `OL_E_CAPABILITY_UNRESOLVED` als ze ook in het proces niet kan worden opgelost. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Een module die uit zichzelf aan de sessie deelnam (instellingenbestand, content of standaardactivering) kan niet worden geladen (`Data["code"]` noemt waarom, `Data["activation"]` hoe ze deelnam). | Plandiagnose (uitvoerbaarheid niet beïnvloed) | Repareer, schakel uit of verwijder de module. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Een item van `.omsilaunch\modules.ini` kan niet worden gebruikt (geen modulesectie, buiten een sectie, onbekende sleutel, ongeldige waarde; de standaardwaarde wordt gebruikt), of er zijn instellingen aangevraagd voor een module die de sessie niet laadt. | Plandiagnose (uitvoerbaarheid niet beïnvloed) | Corrigeer het item of de aanvraag. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Een pad voor sessieverwijdering (ITX-doel, `Texture\standard.ipr`, `closecheck`) bestond niet vóór de sessie en bestaat nu wel, maar onder dit journal is nooit een OMSI-proces gestart, dus het bestand kan geen bijproduct van de sessie zijn. Het wordt behouden en gemeld (bericht = relatief pad, `Data["sha256"]`); de transactie wordt toch voltooid. | Sessiediagnose / `RecoveryStatus.Diagnostics` (status wordt niet beïnvloed) | Inspecteer het bestand; verwijder het zelf als je het niet wilt. |

## Module

Runtimemodules (zie [runtimemodules](runtime-modules.md)). Bij het plannen is een code een plandiagnose: voor een aangevraagde module maakt ze het plan niet uitvoerbaar; voor een module die uit zichzelf deelnam (instellingenbestand, content, standaard) meldt het plan in plaats daarvan `OL_W_MODULE_NOT_LOADED`. In een sessie is een code een sessiediagnose (de sessie gaat door, tenzij de code `OL_E_MODULE_SESSION_FAILED` is), en voor een moduleoperatie is het het runtimeresultaat.

| Code | Gegenereerd door | Betekenis | Kanaal | Wat te doen |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` ontbreekt, is geen geldig `omsilaunch.runtime-module/v1`, of zijn `id` wijkt af van de mapnaam. Het bericht noemt het veld. | Plandiagnose; `ListRuntimeModulesAsync` | Corrigeer het manifest. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Een aangevraagde module-id is niet geïnstalleerd onder `plugins\OmsiLaunch.Modules`. | Plandiagnose | Controleer `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Een vastgepind bestand ontbreekt, wijkt af van zijn vastgepinde SHA-256 of wordt via een reparse point bereikt; of, in het proces, `module.json` is na het plannen gewijzigd. | Plandiagnose; sessiediagnose | Installeer de module opnieuw. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | Het bereik `bridge_protocol` van de module bevat geen enkel protocol dat deze build bedient (1 en 2). | Plandiagnose | Gebruik een moduleversie die voor deze OmsiLaunch is gemaakt. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Een vereiste capability is onbekend voor deze build, niet beschikbaar op deze installatie, of niet in het proces opgelost. | Plandiagnose; sessiediagnose | Zie [capabilities](capabilities.md) en `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | De sessie draait de gebonden kaart van de module niet, of een gebonden bestand wijkt af van zijn vastgepinde SHA-256. | Plandiagnose; sessiediagnose | Start de gebonden kaart met de gebonden content. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | De module vroeg om een bestand te starten dat geen van zijn hulpcomponenten is. | Runtimeresultaat binnen de module | Fout in de module. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | De beheerde component kon niet worden geladen of zijn instaptype kon niet worden aangemaakt; of het sessieprogramma van de module kon niet worden gestart of sloot af voordat het gereed was. | Sessiediagnose | Controleer de modulebuild (x86-compatibel, instaptype implementeert `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` of `Stop` gooide een exceptie. | Sessiediagnose | Fout in de module; zijn `failure_policy` is toegepast. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Een fase of een operatie keerde terug na zijn budget (`timeouts`, `timeout_ms`), of een sessieprogramma was niet gereed binnen `prepare_ms`. | Sessiediagnose; runtimeresultaat | Fout in de module; zijn `failure_policy` is toegepast. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | De module was uitgeschakeld of eerder in de sessie mislukt. | Runtimeresultaat | Zie de sessiediagnosemeldingen voor de eerste fout. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | De sessie heeft deze module niet geladen, of ze is nog niet gereed (vóór gameplay). | Runtimeresultaat | Vraag de module aan bij de start; wacht op `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | De module declareert zo'n operatie niet. | Runtimeresultaat | Controleer de `operations` van de module. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | De operatie is mislukt zonder gecatalogiseerde code; de eigen code van een module staat in `Values["module_code"]`. | Runtimeresultaat | Modulespecifiek. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Na stop of fout was een bijgehouden resource nog actief (een hulpproces dat niet afsloot, een achtergrondthread die niet eindigde) of het vrijgeven ervan gooide een exceptie. | Sessiediagnose | Meld de module; het proces eindigt in elk geval met OMSI. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Een module waarvan de `failure_policy` `fail-session` is, is mislukt; de sessie wordt gestopt en hersteld. | Sessiediagnose (status `Failed`) | Zie de voorafgaande modulecode. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Een aangevraagde moduleinstelling is ongeldig of niet door de module gedeclareerd, een module is zowel ingeschakeld als uitgeschakeld, een instellingsnaam heeft niet de vorm `<module-id>.<key>`, of een modulevlag wordt door meerdere modules gedeclareerd. | Plandiagnose (niet uitvoerbaar); CLI-fout | Corrigeer de aanvraag; `/list:RuntimeModules` toont de instellingen van elke module. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Een OMSI-plugin die de module onder `conflicts.plugins` vermeldt, is ingeschakeld (`plugins\*.opl`) of in OMSI geladen. De reden noemt de `.opl`. | Plandiagnose; sessiediagnose | Schakel die plugin uit of verwijder haar. OmsiLaunch wijzigt haar nooit. |
| `OL_E_MODULE_OPERATION_NOT_DECLARED` | `ModuleHost` | Een module vroeg om een catalogusoperatie in het proces uit te voeren (`IRuntimeModuleContext.Runtime`, native `run_operation`) die zijn manifest niet in `runtime_operations` noemt. De aanroep bereikt OMSI nooit. | Runtimeresultaat | Noem de operatie in `runtime_operations` (vereist `bridge.runtime` en `bridge_protocol.min` 3). |
| `OL_E_MODULE_CONTENT_INPUT_UNDECLARED` | `ModuleHost` | Een module vroeg om communitycontent (`IRuntimeModuleContext.Content`) te lokaliseren van een soort die zijn manifest niet in `content_inputs` declareert. | Contextresultaat | Declareer de soort in `content_inputs`. |
| `OL_E_MODULE_CONTENT_INPUT_INVALID` | `RealmapPackage` | Het pakket `openomsi-realmap` van de kaart kan niet veilig worden gelezen: het wordt via een reparsepunt bereikt, bevat er een, heeft meer dan 50.000 bestanden of is groter dan 4 GiB, of kan niet worden gelezen. | Contextresultaat | Herstel of regenereer het pakket; de module degradeert per resource. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | gereserveerd | Gereserveerd; een native component ontvangt in plaats daarvan de numerieke hookresultaten die in [runtimemodules](runtime-modules.md#native-components-and-hooks) zijn beschreven. | geen | geen |

<a id="non-error-diagnostic-codes"></a>
## Diagnosecodes die geen fout zijn

| Code | Gegenereerd door | Bericht / gegevens | Betekenis |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | bericht = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` is aangemaakt en de identiteit ervan is vastgelegd. Sessiediagnose. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | bericht = SHA-256 van het verwijderde bestand | Een `closecheck` die vóór de sessie bestond, is definitief verwijderd (`SuppressStaleClosecheckWarning = true`). Sessiediagnose. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | bericht = relatief pad; `Data["sha256"]` | Een pad voor sessieverwijdering werd door OMSI opnieuw aangemaakt tijdens een sessie waarvan het proces was gestart; het is verwijderd om de oorspronkelijke afwezigheid te herstellen. Sessiediagnose / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | bericht = `manifest` of `self` | Welke referentie de validatie van de permanente plugin gebruikt. Plandiagnose. |
| `module.planned` | `SessionPlanner` | bericht = module-id; `Data["module"]`, `Data["status"]`, `Data["activation"]` | De module zit in het plan. Plandiagnose. |
| `module.disabled` | `SessionPlanner` | bericht = module-id | Een content- of standaardmodule is buiten gehouden door een uitschakeling (aanvraag of instellingenbestand). Plandiagnose. |
| `session_profile.selected` | `SessionPlanner` | bericht = profiel-id; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Herkomst van een sessie die uit een sessieprofiel is samengesteld. Plandiagnose. |

Namen van runtime-events (`RuntimeEvent.Type`, geen diagnosemeldingen) staan vermeld in de [sessielevenscyclus](../concepts/session-lifecycle.md#telemetry-events).
