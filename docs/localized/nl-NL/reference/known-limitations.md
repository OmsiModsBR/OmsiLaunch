# Bekende beperkingen

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/known-limitations.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina somt, op basis van de code, alles in OmsiLaunch 0.1.0-beta4 op dat `UNAVAILABLE` of `PARTIAL` is of een geaccepteerd risico vormt, zodat gebruikers en integrators niet voortbouwen op gedrag dat het product niet biedt. Elke rij noemt de beperking, de stabiliteit ervan, waarom ze bestaat en waar ze in detail is gedocumenteerd. De Engelse documentatie is normatief; gelokaliseerde kopieën onder `docs/localized/` worden niet op hetzelfde niveau onderhouden en kunnen achterlopen (zie de laatste sectie).

<a id="compatibility"></a>
## Compatibiliteit

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Capabilities die objectindelingen lezen (voertuigen, mensen, weer, kaart, camera, dienstregeling, scripts, chauffeurs, kaartjes) worden alleen op een bekende indeling (`Omsi23004`) geverifieerd; op een andere OMSI 2-build zijn ze niet beschikbaar (`OL_E_CAPABILITY_UNRESOLVED`) totdat er structurele validators bestaan | `PARTIAL` op onbekende builds | [compatibiliteit](compatibility.md) |
| Het echte Steam-uitvoerbare bestand (DRM-beschermd) is niet runtime-gevalideerd; een gecontroleerde LAA-kopie wel | `PARTIAL` voor Steam | [compatibiliteit](compatibility.md) |
| Alleen de bekende indeling is runtime-gevalideerd; de buildonafhankelijke oplossing is offline geverifieerd tegen het referentie-uitvoerbare bestand met zijn identiteit achtergehouden | `EXPERIMENTAL` op onbekende builds | [status van runtimevalidatie](../status/runtime-validation-status.md) |
| Alleen Windows 10+ x64; geen Windows 7/8, geen XP, geen ARM64 | `UNAVAILABLE` | [compatibiliteit](compatibility.md) |
| De plugin heeft de **x86**-runtime van .NET 6 nodig, naast de x64-runtime die de controller gebruikt | | [compatibiliteit](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Wereldstart en startopties

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) is geïmplementeerd op basis van statische analyse van de last-state-tak van OMSI en alleen offline gevalideerd | `EXPERIMENTAL` | [CLI](cli.md) |
| Expliciete of systeemdatum, -tijd en -jaar (`/date`, `/time`, `/year`, profiel `new.date`/`new.time`/`new.year`) gelden alleen voor NEW_MAP-starts en zijn offline gevalideerd; runtimevalidatie is in afwachting. 31 december van een schrikkeljaar kan niet worden aangevraagd (de kalender van OMSI laat dag 366 overlopen) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Weerpreset, ICAO en actueel weer bij de start (`/weather*`, `new.weather`) worden vlak nadat de wereld start toegepast via het eigen weerladen en de METAR-vlaggen van OMSI (statisch afgesloten, BI-003); runtimevalidatie is in afwachting. Het weer dat OMSI op het allereerste frame toont, kan nog het standaardweer van de kaart zijn | `EXPERIMENTAL` | [CLI](cli.md) |
| Model, repaint, HOF, wagennummer en kenteken van het spelersvoertuig bij de start (familie `/vehicle`, `PlayerVehicleSpec`) worden tegen de contentcatalogus opgelost maar niet toegepast; ze aanvragen maakt het plan niet uitvoerbaar. De native toewijzingsreeks (plaatsing via het positiedialoogvenster van OMSI, `MakeVehicle`, de afrondingsstappen per eenheid, `CopyTempListIntoMainList`, de spelersindex) is statisch afgesloten, maar maakt een live voertuig aan en registreert het, en is niet geïmplementeerd voordat ze tijdens runtime kan worden gevalideerd; ze is gepland voor een latere bèta. Een opgeslagen situatie of `/last` herstelt haar eigen spelersvoertuig | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [capabilities](capabilities.md) |
| Een instappunt op identiteit (`/entrypoint:<identity>`) selecteert het exacte label dat OMSI toont (statisch afgesloten, BI-001); een later record met hetzelfde label kan niet worden geselecteerd, net als in het eigen dialoogvenster van OMSI. Runtimevalidatie in afwachting | `EXPERIMENTAL` | plancapability `world.entrypoint-identity` |
| Instellingen voor toetsenbord en controller op sleutel (`Environment.Keyboard`, `Environment.Controllers`) hebben geen gecatalogiseerde sleutels: in plaats daarvan worden hele bestanden vervangen met `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| Coöperatief afsluiten (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) is opt-in en alleen offline gevalideerd; standaard wordt OMSI beëindigd wanneer een sessie eindigt en heeft `ShutdownTimeoutSeconds` geen effect | `EXPERIMENTAL` | [CLI](cli.md) |
| Diagnostiekvlaggen `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` vullen `DiagnosticsSpec` zonder verder effect (de hosttrace onder `.omsilaunch\diagnostics` wordt altijd geschreven); `/trace-plugin` en `/trace-native` voegen er plugin- en nativetracegebeurtenissen aan toe | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` zijn validatieharnassen | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Sessie-einde en procesbesturing

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Het stoppen van een sessie is een geforceerde beëindiging: `session.stop`, "End session" in het systeemvak (sessie beëindigen), Ctrl+C en `CloseAsync` leiden allemaal tot `TerminateProcess`. De afsluitroutine van OMSI wordt niet uitgevoerd, OMSI herschrijft `options.cfg` en zijn logs niet bij het afsluiten, en niet-opgeslagen OMSI-toestand gaat verloren. Dit is bewust: zo wordt voorkomen dat OMSI herstelde bestanden overschrijft. | bewust zo ontworpen | [sessielevenscyclus](../concepts/session-lifecycle.md) |
| Coöperatief afsluiten via WM_CLOSE met een timeout en terugval op beëindiging is niet geïmplementeerd | `UNAVAILABLE` (productbeslissing, S-11; OMSI negeerde in de afsluitende runtimeronde `WM_CLOSE` naar zijn hoofdvenster) | [status van de runtimevalidatie](../status/runtime-validation-status.md) |
| Bij het sluiten van de console of bij afmelden heeft de eigenaar een budget van 4 s om te stoppen en te herstellen; wat overblijft, wordt bij de volgende start via het journal hersteld | sluiten van de console runtime-gevalideerd; afmelden niet getest | [transacties en recovery](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transactie, recovery en lease

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| De installatielease is een semafoor `Local\`: één eigenaar per installatie **per aanmeldsessie**; niet afgedwongen tussen gebruikers; niet vrijgegeven zolang een ander proces een handle vasthoudt; elk proces van dezelfde gebruiker kan de naam bezetten | geaccepteerd risico (S-18) | [transacties en recovery](../concepts/transactions-and-recovery.md) |
| Recovery wordt geweigerd (`OL_E_INSTALLATION_BUSY`) zolang het in het journal vastgelegde OMSI-proces draait, of, bij een journal zonder PID, zolang er een `Omsi.exe` uit die hoofdmap draait | bewust zo ontworpen | zoals hierboven |
| Een oorspronkelijk afwezig overlaypad waarvan de inhoud tijdens de sessie is gewijzigd, blokkeert het herstel (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) totdat het is geïnspecteerd | bewust zo ontworpen | zoals hierboven |
| Journals van vóór de eigendomsvingerafdrukken kunnen alleen worden afgesloten door een sessie met identieke geplande bytes (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | zoals hierboven |
| Alleen paden in bezit van de sessie worden hersteld. De eigen schrijfacties van OMSI tijdens een sessie (`options.cfg` `[last_map]` wanneer geen enkele instelling een overlay op `options.cfg` legt, `Texture\standard.ipr`, caches, `laststn.osn`, chauffeursprofiel, logs) blijven bestaan, net als na een directe OMSI-start | bewust zo ontworpen | [transacties en recovery](../concepts/transactions-and-recovery.md) |
| Het verwijderen van een verouderde `closecheck` vóór een sessie is permanent (vastgelegd, niet hersteld) wanneer `SuppressStaleClosecheckWarning` true is | bewust zo ontworpen | zoals hierboven |

<a id="runtime-control"></a>
## Runtimebesturing

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| `weather.set` en `weather.metar.set` schrijven het bronweerrecord van OMSI (statisch afgesloten); persistentie over de weertick, de oplossing van het wolkentype en de METAR-download wachten op runtimevalidatie. Afgeleide waarden (luchtvochtigheid, lichtfactoren, actuele wind) kunnen niet worden ingesteld; terwijl OMSI gepauzeerd is, worden ze pas na het hervatten bijgewerkt | `EXPERIMENTAL` | [capabilities](capabilities.md) |
| `SetActualDateTime` kopieert alleen de pc-klok naar de startklok van OMSI (statische analyse); OmsiLaunch roept het niet aan en stelt de startklok zelf in | bewust zo ontworpen | `calendar.set-actual-date-time` |
| Het schrijven van stringvariabelen, benoemde triggers en geluidstriggers loopt via de eigen methoden van OMSI (statisch afgesloten, BI-004); hun effect op voertuigscripts wacht op runtimevalidatie. Alleen wegvoertuigen zijn doelen | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Geen verplaatsing van voertuigen, geen ruimtelijke herkoppeling over tiles heen, geen ODE-veilige autoriteit over transformaties; positievelden zijn alleen-lezen | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` vereisen een PlayerVehicle; de headless NEW_MAP-start heeft er geen (een opgeslagen situatie levert er een) | bewust zo ontworpen (BI-007) | RV-004 |
| Runtimewijzigingen (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, D3D-textures) worden niet in het journal vastgelegd en niet hersteld | bewust zo ontworpen | [runtimebesturing](runtime-control.md) |
| De veroudering van handles wordt bepaald aan de hand van het eigen, nooit hergebruikte instantienummer van OMSI (`IDCode`) en, voor mensen, aan de hand van despawn en respawn; dit is offline gevalideerd. Natuurlijke verwijdering (RV-002: AI-auto's die het geladen padennetwerk verlaten) moet nog tijdens runtime worden waargenomen, en er wordt nog geen event `entity.removed` uitgezonden | `PARTIAL` | [runtime control](runtime-control.md) |
| Resultaten zijn begrensd door de mailbox van 64 KiB: lange lijsten worden afgekapt (`truncated=true`); pixelpayloads zijn beperkt tot 48 KiB per `d3d.texture.update` | bewust zo ontworpen | [capabilities](capabilities.md) |
| Kanaal met één verzoek tegelijk: één verzoek per keer per sessie; een bezet slot geeft `OL_E_RUNTIME_CHANNEL_BUSY`; verzoek-id's mogen niet worden hergebruikt | bewust zo ontworpen | [runtimebesturing](runtime-control.md) |
| Telemetrie is een ring van 64 events: een host die meer dan 64 events achterloopt, verliest de oudste en krijgt te horen hoeveel (`telemetry.lost`) | `EXPERIMENTAL` | [permanente plugin](../concepts/permanent-plugin.md) |
| De Direct3D-apparaathooks van een module zijn vtable-hooks: op een installatie waar een andere component de oorspronkelijke methoden van het apparaat terugschrijft (waargenomen op de regressie-installatie kort nadat de gameplay start), worden ze niet meer aangeroepen en kunnen ze bij het opruimen niet worden verwijderd (`OL_E_MODULE_CLEANUP_INCOMPLETE`; de component blijft geladen) | `PARTIAL` | [runtimemodules](../../../reference/runtime-modules.md#native-components-and-hooks) |
| Het laden van runtimemodules is open: OmsiLaunch controleert niet wie een module heeft gemaakt; het garandeert alleen dat modules nooit binnen het OmsiLaunch-proces draaien en dat een sessieprogramma beperkt blijft tot zijn taak (geen tweede proces, beëindigd met de sessie) | bewust zo ontworpen (E21) | [runtimemodules](../../../reference/runtime-modules.md#security-boundary) |
| Moduleprotocol 2 (instellingen, standaardactivering, het instellingenbestand, sessieprogramma's, pluginconflicten, meldingen, `get_value`) is alleen offline gevalideerd | `EXPERIMENTAL` | [runtimemodules](../../../reference/runtime-modules.md) |
| De D3D-apparaatreset is tijdens runtime waargenomen (`resetting`, `restored`, invalidatie van de generatie); een afzonderlijke overgang `lost` werd niet geproduceerd, omdat het apparaat van OMSI rechtstreeks naar `DEVICENOTRESET` ging | `PARTIAL` (RV-007) | [status van de runtimevalidatie](../status/runtime-validation-status.md) |
| Runtimelijsten zijn gepagineerd (`cursor`, `limit`, `next_cursor`): een pagina bevat hoogstens de maximale rijen van de lijst en wat in het runtime-slot van 64 KiB past | `EXPERIMENTAL` | [capabilities](capabilities.md#pagination) |
| Sommige geavanceerde velden van de kaart-, tile-, pad- en objectgraaf worden niet beschikbaar gemaakt; runtimelezers zijn door het profiel afgeschermde, getypeerde snapshots en nooit willekeurige geheugentoegang | bewust zo ontworpen | [capabilities](capabilities.md) |
| Geheugenleesacties in het proces volgen het patroon eerst controleren, dan gebruiken tegen een levende OMSI; een gelijktijdige wijziging door OMSI tussen de controle en het lezen kan een inconsistente snapshot opleveren (`OL_E_RUNTIME_OPERATION_FAILED`) | geaccepteerd risico (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Lokaal control plane en vertrouwensmodel

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Vertrouwensmodel op basis van dezelfde gebruiker: de named pipe (`CurrentUserOnly`), de geheugenmappings voor handoff, telemetrie en runtime en de semafoor van de lease zijn toegankelijk voor elk proces van dezelfde Windows-gebruiker. Zo'n proces kan de status lezen, de sessie stoppen of runtime-operaties uitvoeren zodra het de `session_id` heeft gelezen. | geaccepteerd risico (S-06, S-30) | [local control](local-control.md) |
| Het besturingseindpunt bestaat alleen zolang de eigenaar `Running` is; een client ziet `OL_E_NO_ACTIVE_SESSION` (exitcode 4) tijdens het opstarten en nadat de sessie is beëindigd | bewust zo ontworpen | [local control](local-control.md) |
| Als een ander proces de pipenaam al bezit, blijft de eigenaar draaien zonder eindpunt (`ListenFault`), en kan een tweede start ten onrechte `OL_E_SESSION_ALREADY_ACTIVE` melden | geaccepteerd risico | [local control](local-control.md) |
| `.omsilaunch\` erft de ACL van de OMSI-hoofdmap; er wordt geen expliciet toegangsbeheer toegepast | geaccepteerd risico (S-31) | [transacties en recovery](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostiek en uitvoer

| Beperking | Stabiliteit | Details |
| --- | --- | --- |
| Diagnostiek bestaat alleen uit lokale bestanden (`.omsilaunch\diagnostics`); er wordt niets geüpload en er is geen rapportage op afstand | bewust zo ontworpen | [transacties en recovery](../concepts/transactions-and-recovery.md) |
| De bewaartermijn omvat de 50 nieuwste sessies; oudere diagnostiek met sessievoorvoegsel wordt verwijderd wanneer een nieuwe sessie start | bewust zo ontworpen | zoals hierboven |
| JSON-uitvoer en diagnostiek bevatten installatiepaden (`RootPath`, assetmappen, paden `.itx`) | bewust zo ontworpen (lokale gegevens) | |
| Het sessiefoutdialoogvenster van `OmsiLaunchW.exe` toont de foutpayload van de plugin (bijvoorbeeld `{"name":"world.failed",...}`) als bericht in plaats van een zin; de regel `Code:` is correct | cosmetisch | [Windows-systeemvak](windows-tray.md) |
| Een D3D-verzoek dat door de native brug wordt geweigerd vóór enige Direct3D-aanroep, meldt `native_status` correct, maar de tekst van `detail` zegt `HRESULT 0x00000000` | cosmetisch | [capabilities](capabilities.md) |
| Het statusvenster in het systeemvak is een snapshot van de geplande sessie, gemaakt wanneer het wordt geopend; het wordt niet vernieuwd en toont geen live OMSI-waarden | bewust zo ontworpen | [Windows-systeemvak](windows-tray.md) |

<a id="documentation"></a>
## Documentatie

De Engelse pagina's onder `docs/` vormen de normatieve documentatie voor deze release. `docs/localized/<locale>/` bevat vertalingen van dezelfde pagina's van `0.1.0-beta4` (zie [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); waar een vertaling afwijkt van de Engelse tekst, zijn de Engelse tekst en de code gezaghebbend. De historische en legacy pagina's die daar worden vermeld, zijn alleen in het Engels beschikbaar.

Zie ook: [capabilities](capabilities.md), [status van de runtimevalidatie](../status/runtime-validation-status.md), [fouten](errors.md).
