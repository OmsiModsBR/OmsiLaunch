# Runtime control

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/runtime-control.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Runtime control is de verzameling lees-, schrijf- en actieoperaties die OmsiLaunch binnen een actieve OMSI-sessie uitvoert. Deze pagina legt uit langs welke drie wegen je deze bereikt (eigenaar-API, CLI-client, lokaal control plane), hoe verzoeken van de aanroeper naar de plugin en terug reizen, hoe handles en verzoekidentiteiten werken, welke timeouts gelden, wat bewust niet in het journal wordt vastgelegd en welke conventies voor argumenten en resultaten gelden, met een uitgewerkt voorbeeld voor elke familie. De inventaris van operaties zelf, met argumenten, resultaatsleutels en fouten, staat in [capabilities](capabilities.md). Bronnen: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` en `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` en `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Drie instappunten

| Instappunt | Wie | Pad | Timeout |
| --- | --- | --- | --- |
| Eigenaar-API | Een integrator die de sessie binnen het eigen proces heeft gestart met `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> registervalidatie -> sessie opzoeken -> mailbox | door de aanroeper opgegeven `TimeSpan` |
| Eigenaar-CLI (`/runtime:<op>`) | Het `OmsiLaunch.exe`-proces dat eigenaar van de sessie is | één operatie, direct na `Running` uitgevoerd; het resultaat wordt naar `.omsilaunch\diagnostics\<session>-runtime-operation.json` en naar de console geschreven; de sessie loopt door | 5 s (15 s voor `road-vehicles.spawn`) |
| CLI-client | Elke aanroep van `OmsiLaunch.exe` **zonder** installatieargument, bijvoorbeeld `OmsiLaunch.exe time get` | named pipe `runtime.execute` naar de eigenaar van de installatie waarin het uitvoerbare bestand staat -> de eigenaar roept `ExecuteRuntimeAsync` aan | 8 s (30 s voor `road-vehicles.spawn`) aan zowel de client- als de eigenaarzijde |
| Lokaal control plane | Elk proces van dezelfde Windows-gebruiker | hetzelfde pipeprotocol als de CLI-client; zie [local control](local-control.md) | zoals hierboven |

Elk pad eindigt in `ExecuteRuntimeAsync`, dat de openbare grens in deze volgorde afdwingt:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: een operatie die niet in `PublicRuntimeOperationIds` staat (inclusief elke `internal.*`-operatie) retourneert `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; een ontbrekend of leeg verplicht argument retourneert `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Geen van beide raakt de sessie.
2. Sessie opzoeken (`KeyNotFoundException` bij een onbekende handle), `OL_E_RUNTIME_SESSION_MISMATCH` als `RuntimeCommand.SessionId` van de handle afwijkt, `OL_E_SESSION_NOT_RUNNING` tenzij de toestand `Running` is.
3. Mailboxverzoek (zie hieronder), waarna `ScrubInternalValues` elke resultaatsleutel verwijdert die begint met `internal_` of eindigt op `_address`, `_pointer`, `_vmt`.

De CLI-client en het control plane voeren stap 1 zelf uit voordat ze contact opnemen met de eigenaar, zodat een ongeldige opdrachtnaam als exitcode 2 wordt gerapporteerd, ook als er geen sessie actief is (`OL_E_RUNTIME_OPERATION_UNKNOWN` en `OL_E_RUNTIME_ARGUMENT_REQUIRED` worden afgebeeld op `InvalidArguments`; andere weigeringen op 7; geen eigenaar op 4).

Het control-plane-eindpunt bestaat alleen zolang de eigenaar `Running` is, na het opstarten en nadat een eventuele `/runtime-batch`-, `/runtime-write-batch`- of `/d3d-batch`-harness is voltooid. Wijzigende opdrachten (`runtime.execute`, `session.stop`) moeten de actieve `session_id` meesturen; de CLI haalt deze automatisch op uit `session.status` (anders `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Verzoekidentiteit en de mailbox

Een `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` wordt geserialiseerd naar een `RuntimeCommandWire`-envelope (magic `OLRC`, versie 1, header van 72 bytes, SHA-256 van de JSON-payload) en klaargezet in de single-flight-mailbox van 64 KiB van de sessie (`OmsiLaunch.Runtime.<sessionId>`). De plugin pollt de mailbox elke 50 ms op de UI-thread van OMSI, voert de operatie daar uit en schrijft het antwoord.

Regels die het kanaal robuust maken:

| Regel | Effect |
| --- | --- |
| Single flight | Eén verzoek tegelijk per sessie; de host serialiseert aanroepers met een semafoor. Een slot dat nog `requested` is wanneer er een nieuw verzoek binnenkomt, geeft `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Sessiebinding | De plugin negeert (en wist) een verzoek waarvan de sessie-GUID niet de eigen GUID is; de host weigert een antwoord waarvan de sessie- of verzoek-id niet overeenkomt (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Als de deadline verstrijkt, zet de host het slot terug op idle en gooit `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Laat antwoord | De plugin publiceert een antwoord alleen als het slot nog de eigen verzoek-id bevat; een antwoord op een opgegeven verzoek wordt verworpen. Als er toch een binnenkomt, vindt het volgende hostverzoek een verouderd `responded`-slot, verwerpt dit en gaat verder; als dat verouderde antwoord **dezelfde** verzoek-id heeft als het nieuwe verzoek, gooit de host `OL_E_RUNTIME_REQUEST_ID_REUSED`. Aanroepers mogen daarom binnen een sessie nooit een verzoek-id hergebruiken. |
| Grootte | Een verzoek dat groter is dan het slot wordt vóór het klaarzetten geweigerd (`ArgumentOutOfRangeException`). Een lijstpagina die niet past, wordt door de plugin ingekort (laatste rijen weggelaten, `truncated=true`, kleinere `returned_count`, `next_cursor` bij de eerste weggelaten rij); elk ander te groot antwoord wordt vervangen door een getypeerde fout `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Kanaal gesloten | Nadat de sessie is beëindigd, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Verzoek-id's zijn `ulong`-waarden die de aanroeper kiest. De CLI gebruikt vaste bereiken: `10_001` voor `/runtime:`, `50_001+` voor doorgestuurde control-plane-verzoeken, `1+` voor de leesbatch, `20_000+` voor de D3D-batch, `30_000+` voor `D3DRuntimeApi`. Integrators horen per sessie een monotoon oplopende teller te gebruiken.

<a id="handles-and-stale-detection"></a>
## Handles en detectie van verouderde handles

| Prefix | Type | Uitgegeven door | Formaat |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + zes decimale cijfers (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + zes decimale cijfers |
| `d3dtex-` | D3D-texture | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Handles zijn ondoorzichtig en gelden per sessie: ze worden door de plugin aangemaakt, coderen nooit een adres en hebben in een andere sessie geen betekenis. Bij het omzetten van een handle controleert de plugin of het adres waarnaar deze verwijst nog in de actieve OMSI-collectie staat (volgens de laatste lijstleesactie; `road-vehicles.list` en `humans.list` vernieuwen dit beeld) en leest een objectvingerafdruk opnieuw (de Delphi-VMT plus de pointer naar de voertuigdefinitie, of de modelindex van de mens). Een afwijking betekent dat het native object is vernietigd en het adres is hergebruikt: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Voor hetzelfde actieve object wordt bij herhaalde lijstleesacties opnieuw hetzelfde token geretourneerd. Resterende blinde vlek: een object van dezelfde klasse en definitie dat tussen twee lijstleesacties op hetzelfde adres opnieuw is aangemaakt, kan niet worden onderscheiden. D3D-handles worden door de native brug gevalideerd: een onbekende handle of een handle van een andere sessie geeft `OL_E_D3D_STALE_RESOURCE_HANDLE`, een vrijgegeven handle `OL_E_D3D_RESOURCE_RELEASED`, en een wijziging van de apparaatgeneratie markeert textures als `STALE`.

Handles zijn nooit native adressen en mogen nooit worden geparseerd, numeriek vergeleken, over sessies heen bewaard of aan een andere sessie doorgegeven: behandel ze als ondoorzichtige strings die geldig zijn voor de sessie die ze heeft uitgegeven.

Runtimebewijs: een vrijgegeven D3D-handle blijft geweigerd nadat nieuwe textures zijn aangemaakt, en een handle uit een vorige sessie wordt door de volgende sessie geweigerd (runtime closure `H02`); een D3D-apparaatreset maakt elke actieve texture ongeldig (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). Detectie van verouderde RoadVehicle- en Human-handles na natuurlijke verwijdering (RV-002) heeft geen veilige runtimeproducent (OMSI heeft in de observatievensters geen objecten verwijderd en geen enkele openbare operatie verwijdert er een); dit is offline gedekt.

<a id="what-is-not-journaled"></a>
## Wat niet in het journal wordt vastgelegd

Runtimewijzigingen veranderen alleen het geheugen van OMSI. **Ze worden niet in het transactiejournal vastgelegd en niet hersteld** wanneer de sessie eindigt: `time.set`, `camera.set`, `camera.lock` (het beleid stopt wanneer de plugin afsluit), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` en elke `d3d.texture.*`-resource. Ze verdwijnen met het OMSI-proces, dat OmsiLaunch aan het einde van de sessie beëindigt zonder OMSI iets te laten opslaan (zie [transacties en recovery](../concepts/transactions-and-recovery.md)). Niets in de runtimefamilie raakt het bestandssysteem.

<a id="argument-and-result-conventions"></a>
## Conventies voor argumenten en resultaten

- Argumenten zijn sleutel/waarde-paren van strings. Op de CLI wordt `--key=value` na de opdrachtwoorden een runtimeargument (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` is het equivalent voor `/runtime:<op>`. Getallen gebruiken de invariante cultuur (`.` als decimaalteken); booleans zijn `true`/`false`.
- Opdrachtwoorden worden via `CliInput.HierarchicalRoutes` op operatie-id's afgebeeld (bijvoorbeeld `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). De volledige routetabel staat in de [CLI-referentie](cli.md). D3D-operaties hebben geen route met opdrachtwoorden; gebruik `/runtime:d3d.status` of `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Resultaten zijn platte string-dictionaries. Lijsten gebruiken sleutels van de vorm `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) met de paginavelden `count`, `returned_count`, `truncated` en `next_cursor` (zie [paginering](capabilities.md#pagination)).
- Fouten bevatten `Succeeded=false`, een `OL_E_`-`ErrorCode` en, bij fouten aan de pluginzijde, `detail` (en `native_status` bij D3D, `exception` bij onverwachte fouten).

### CLI-envelope (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

Een foutenvelope ziet eruit als `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Zonder `--json` drukt de CLI het resultaatobject af als ingesprongen JSON of als `CODE: message`.

### API-envelope

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` gooit een exceptie bij grensovertredingen na de registervalidatie (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) en retourneert `Succeeded=false` bij weigeringen door het register en fouten aan de pluginzijde.

<a id="worked-examples"></a>
## Uitgewerkte voorbeelden

Alle CLI-voorbeelden gaan ervan uit dat er een eigenaar actief is voor de installatie die `OmsiLaunch.exe` bevat (gestart met bijvoorbeeld `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, of `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` als een voorbeeld een spelersvoertuig nodig heeft) en worden uitgevoerd vanuit een tweede console in dezelfde installatie.

| Familie | Opdracht | Wat deze doet |
| --- | --- | --- |
| Sessie | `OmsiLaunch.exe session status --json` | Leest `SessionId`, `State`, diagnosemeldingen en de begrensde lijst van runtimegebeurtenissen. |
| Tijd | `OmsiLaunch.exe time get` en daarna `OmsiLaunch.exe time set --hour=7 --minute=30` | Leest de klok; zet deze via de geprofileerde `SetTime` en retourneert de teruggelezen waarde. |
| Weer | `OmsiLaunch.exe weather get` en `OmsiLaunch.exe weather actual get` | Leest de huidige weertoestand en de actuele/ICAO-weertoestand. `OmsiLaunch.exe weather set --wind_speed=5` stelt handmatig weer in (het bronrecord van OMSI; live METAR stopt) en `OmsiLaunch.exe weather metar set --icao=EDDF` schakelt over op live METAR-weer. Beide zijn EXPERIMENTAL totdat ze runtime-gevalideerd zijn. |
| Kaart | `OmsiLaunch.exe map get` | Identiteit van de kaart, naam, beschrijving, aantal tiles, jaarbereik en verkeerszijde. |
| Camera | `OmsiLaunch.exe camera set --field_of_view=50` en daarna `OmsiLaunch.exe camera lock --family=0 --preset=1` en `OmsiLaunch.exe camera unlock` | Schrijft de FOV; zet de chauffeursfamilie vast met preset 1 (vereist een spelersvoertuig, bijvoorbeeld uit een opgeslagen situatie); heft het beleid op. |
| Voertuigen | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Aantallen; handles; één snapshot. |
| Spawnen | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Maakt één AI-RoadVehicle aan (timeout 30 s); retourneert `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` roept `PlaceRandomBus` aan. |
| Speler | `OmsiLaunch.exe player get` | `present=false` bij een headless start, anders de snapshot van de speler. |
| Mensen | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Aantallen; handles; één snapshot. |
| Dienstregeling | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Aantallen van de manager; begrensde trackrijen; logs van de dienstregeling. |
| Scripts | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Numerieke variabelen weergeven/lezen/schrijven; stringvariabele lezen/schrijven; script- en geluidstriggers (het schrijven van strings en triggers zijn EXPERIMENTAL totdat ze runtime-gevalideerd zijn). |
| Constanten en curves | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Voertuigconstanten en evaluatie van curves. Namen zijn modelspecifiek: gebruik de namen die de `list`-opdracht retourneert (deze zijn weergegeven voor de spelersbus van `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | HOF-metadata van de voertuigdefinitie. |
| Chauffeurs en kaartjes | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Chauffeursrecords; kaartjespakket. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` en daarna `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` en `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Levenscyclus van een texture op de renderthread; `<HANDLE>` is de `handle` die `create` afdrukt; `--pixels_base64` moet decoderen tot `width * height * 4` bytes (32-bits formaten) en maximaal 48 KiB. |
| Gebeurtenissen | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Begrensde lijst van gebeurtenissen; pollend volgen (250 ms) tot Ctrl+C. |
| Stoppen | `OmsiLaunch.exe session stop` | Vraagt de canonieke stop aan: OMSI wordt beëindigd en de transactie wordt door de eigenaar hersteld. |

<a id="stability"></a>
## Stabiliteit

Het kanaal zelf (`runtime.command-channel`) is `STABLE_BETA`: wire-integriteit, sessiebinding, het verwerpen van late antwoorden en getypeerde weigering bij te grote omvang zijn offline gedekt door `runtime-command.wire-guard`, `runtime-command.session-binding` en `runtime-command.late-response-ignored`, en tijdens runtime door RV-003 (sessie `5f641c8d`), Round A RA-019 (een client gaf `road-vehicles.spawn` na 250 ms op; het volgende verzoek slaagde) en de kanaaltestreeks `R03` van de runtime closure (timeout, annulering, hergebruikte verzoek-id, weigering door de plugin, interne operatie, verkeerde sessie en ontbrekend argument, telkens gevolgd door een geslaagd verzoek). De stabiliteit per operatie staat in [capabilities](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Hergebruik van het kanaal na fouten

Elk eindpad van een runtimeverzoek laat de mailbox herbruikbaar achter: succes, een getypeerde fout, een misvormd of te groot antwoord, een antwoord van een andere sessie of met een verkeerde verzoek-id, een timeout, annulering door de aanroeper en decodeerfouten eindigen allemaal in één opruimstap die het slot terugzet op idle en de lengte en de envelope-header wist, zodat niets van een eerder verzoek door het volgende kan worden gelezen. Een achtergebleven verzoek of antwoord dat wordt aangetroffen wanneer een nieuw verzoek begint, wordt eerst gewist (`OL_E_RUNTIME_REQUEST_ID_REUSED` als het de id van het nieuwe verzoek draagt). Aan de pluginzijde wordt een exceptie binnen een operatie beantwoord met `OL_E_RUNTIME_OPERATION_FAILED`, een resultaat dat groter is dan het slot met `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (een vaste, kleine envelope), een verzoek voor een andere sessie met `OL_E_RUNTIME_SESSION_MISMATCH`, en een verzoek dat de host heeft opgegeven, wordt nooit beantwoord. Deze regels zijn offline gedekt (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); de paden die een aanroeper kan veroorzaken (timeout, annulering, hergebruikte id, getypeerde weigeringen, laat antwoord) hebben ook runtimebewijs (RA-019, `R03`). Corrupte, vreemde of te grote antwoorden kunnen niet van buiten het product worden veroorzaakt en blijven alleen offline gedekt.
