# CLI-voorbeelden

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/cli-examples.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Minimale, correcte aanroepen van `OmsiLaunch.exe` voor OmsiLaunch `0.1.0-beta4`, elk met de verwachte exitcode van het proces en een opmerking over wat wordt gewijzigd en hersteld. Elk voorbeeld wordt uitgevoerd vanuit de OMSI-installatiemap (`<OMSI_PATH>`), tenzij anders vermeld; de syntaxis is gedefinieerd in de [CLI-referentie](cli.md) en de exitcodes in [exitcodes](exit-codes.md). Aan elke opdracht kan `--json` worden toegevoegd om de gestructureerde envelope te krijgen.

<a id="conventions"></a>
## Conventies

- **Wijzigt**: bestanden of OMSI-toestand die door de opdracht worden gewijzigd. "Sessie-overlay" betekent een bestand waarvan een snapshot in de transactie wordt opgenomen, dat wordt toegepast voordat OMSI start en dat byte voor byte wordt hersteld wanneer de sessie eindigt.
- **Hersteld**: wat ongedaan wordt gemaakt wanneer de sessie eindigt (normale stop, Ctrl+C, systeemvak, `session stop`, `/observe-seconds`) of door recovery.
- Runtime-schrijfacties (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) wijzigen alleen het geheugen van OMSI; ze worden nooit teruggedraaid, omdat OMSI bij de stop wordt beëindigd.
- Tijdelijke aanduidingen: `<OMSI_PATH>` is de OMSI 2-installatie die het OmsiLaunch-pakket bevat (bijvoorbeeld `C:\OMSI 2`); `<OTHER_OMSI_PATH>` een andere installatie; `<SPEC_PATH>` en `<ITX_PATH>` een eigen LaunchSpec-bestand en een eigen internettexturesprofiel; `<HANDLE>` is een handle die door de voorafgaande opdracht `list` of `create` is afgedrukt en `<BASE64>` zijn Base64-pixelgegevens. Elke andere waarde is een letterlijke waarde die werkt op een standaard OMSI 2-installatie (`grundorf-quick` is het voorbeeldprofiel dat op deze pagina wordt gedefinieerd).
- Zet een pad met spaties tussen aanhalingstekens en laat een pad tussen aanhalingstekens niet eindigen op `\` (de argumentverwerking van Windows maakt van `\"` een letterlijk aanhalingsteken): `"C:\OMSI 2"`, niet `"C:\OMSI 2\"`.
- Elke opdrachtregel op deze pagina wordt geparseerd door de documentatiegate (`tests/OmsiLaunch.DocumentationTests`, gate `examples`); de voorbeelden voor identiteit, ontdekking, plannen en client zijn ook uitgevoerd tegen een echte installatie (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identiteit en ontdekking (geen sessie)

```text
OmsiLaunch.exe /version
```
Exit `0`. Drukt `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` af. Wijzigt niets.

```text
OmsiLaunch.exe profiles --json
```
Exit `0`. Geeft de ondersteunde `Omsi.exe`-hashes en hun validatiestatus weer. Wijzigt niets.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Exit `0`. Openbare capabilitycatalogus; `help <family>` filtert deze. Wijzigt niets.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Exit `0` (beide vormen zijn identiek). Rapporteert actieve `Omsi.exe`-processen en of een OmsiLaunch-eigenaar voor deze installatie antwoordt. Wijzigt niets.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Exit `0` (`2` voor een onbekende categorie). Alleen-lezen ontdekking; junctioncycli worden overgeslagen. Wijzigt niets. De hier afgedrukte `Identity`-waarden zijn exact de tekenreeksen die `/map`, `/saved`, `/vehicle-scope` en een `LaunchSpec` verwachten (bijvoorbeeld `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Plannen en valideren

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Exit `0` als het plan `READY` is, `1` als het `NOT RUNNABLE` is (bijvoorbeeld `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Wijzigt niets; OMSI wordt niet gestart.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Exit `0`/`1` zoals hierboven; `/validate` is een alias van `/plan`. De JSON is het onbewerkte `SessionPlan` (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Exit `1`. `/date`, `/time`, `/year`, `/weather*` en vlaggen voor het spelersvoertuig worden geaccepteerd, maar niet toegepast door deze build; het plan bevat `OL_E_CAPABILITY_UNAVAILABLE` en is niet uitvoerbaar.

```text
OmsiLaunch.exe /last /plan
```
Exit `1`. `LAST_MAP_STATE` is niet beschikbaar voor dit profiel (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Sessies starten (eigenaarmodus)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Exit `0` als de sessie eindigt in `Completed`, `1` bij `Failed` of een niet-uitvoerbaar plan. Wijzigt: sessie-overlays `GUI\NewSplashscreen_ENG.bmp` en `GUI\NewSplashscreen_<lang>.bmp` (het beheerde opstartscherm is de standaard), de afhandeling van `closecheck`, de opstart-handoff. Hersteld: elke overlay, byte voor byte, wanneer de sessie eindigt. De console blijft gekoppeld tot OMSI afsluit, "End session" in het systeemvak is bevestigd, een client `session stop` verstuurt of Ctrl+C wordt ingedrukt.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Exit `0`. Zoals hierboven, maar de sessie wordt 8 s na het bereiken van `Running` gestopt (eerder bij een stop via systeemvak/pipe). Wordt gebruikt door validatiescripts.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Exit `0`/`1`. SAVED_SITUATION: kaart, tijd en positie komen uit het `.osn`-bestand (`situations\Linie 5.osn` wordt met OMSI 2 meegeleverd en start op Berlin-Spandau met een spelersbus). De waarde is de situatie-identiteit die door `/list:Situations` wordt afgedrukt (relatief ten opzichte van de installatie, niet hoofdlettergevoelig); een losse bestandsnaam zoals `Linie 5.osn` wordt niet gevonden (`OL_E_SITUATION_NOT_FOUND`, exit `1`). `/map` of `/entrypoint-index` in combinatie met `/saved` wordt geweigerd met exit `2`. Wijzigingen en herstel zoals bij NEW_MAP. OMSI legt zelf de kaart van de situatie vast in `options.cfg` `[last_map]`; die schrijfactie van OMSI wordt niet teruggedraaid, tenzij een `/set` een overlay op `options.cfg` aanbrengt (zie [transacties en recovery](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Exit `0`. Wijzigt: `options.cfg` (sessie-overlay, semantische token-/vectorpatch; CP1252-bytes blijven behouden) plus de overlays van het opstartscherm. Hersteld: `options.cfg` en de opstartschermbestanden exact (RV-005, RV-006). `/set:graphics.texture=...` eindigt met exit `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` eindigt met exit `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Exit `0`. Wijzigt: geen overlay van het opstartscherm; alleen de opstart-handoff en de afhandeling van `closecheck`. Hersteld: voor het opstartscherm valt niets te herstellen.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Exit `0` (`1` met `OL_E_SESSION_PRESENTATION_INVALID` als de map of een BMP ontbreekt of geen 24-bits 640x480 is). Wijzigt: `GUI\NewSplashscreen_ENG.bmp` en `GUI\NewSplashscreen_PTB.bmp` vanuit de aangepaste map (sessie-overlay). Hersteld: beide bestanden exact.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Exit `0`. Wijzigt: niets op schijf behalve de overlays van het opstartscherm; de downloader binnen het proces wordt voor de sessie onderdrukt.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Exit `0` (`2` met `OL_E_ITX_PROFILE_REQUIRED` als het profiel ontbreekt; `1` voor een ongeldig profiel of een doel buiten `Texture\`). Wijzigt: `Texture\standard.itx` (sessie-overlay); elk in het profiel vermeld doel en `Texture\standard.ipr` zijn sessieverwijderingen. Hersteld: overlay verwijderd, verwijderde originelen hersteld; bestanden die OMSI tijdens de sessie op die paden heeft aangemaakt, worden als bijproducten van de sessie verwijderd.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Exit `0`. Wacht maximaal 300 s (+5 s) op `Running` in plaats van de standaard 180 s. `/shutdown-timeout:60` wordt geaccepteerd, maar heeft in deze build geen effect.

<a id="predefined-session-profile"></a>
### Vooraf gedefinieerd sessieprofiel

Profielbestand `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
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
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Exit `0`. Wijzigt: `options.cfg` (instellingen van de preset, sessie-overlay) en de overlays van het opstartscherm. Hersteld: al deze bestanden. Het toevoegen van `/map:...` of `/set:graphics.maxFPS=60` eindigt met exit `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); het weglaten van `/predefined-profile-index` eindigt met exit `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Het meegeleverde voorbeeld `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` toont het volledige schema, maar in de meegeleverde vorm vraagt het `new:`-blok om `date`, `time` en `weather`, die deze build niet kan toepassen: het plannen ervan met `/new` levert `NOT RUNNABLE` op (`OL_E_CAPABILITY_UNAVAILABLE`); verwijder die sleutels vóór gebruik.

<a id="launchspec-file"></a>
### LaunchSpec-bestand

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Exit `0`/`1`. Het meegeleverde voorbeeld selecteert Grundorf, instappuntindex `1`, het beheerde opstartscherm en native internettextures; `RootPath: "."` verwijst naar de map van het uitvoerbare bestand. Wijzigingen zoals bij het expliciete NEW_MAP-voorbeeld. Een spec met een onbekende eigenschap eindigt met exit `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); een ontbrekend bestand eindigt met exit `6` (`OL_E_SPEC_NOT_FOUND`); een bestand groter dan 1 MiB eindigt met exit `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Exit `0`/`1`. De expliciete installatie `<OTHER_OMSI_PATH>` heeft voorrang op de `RootPath` van de spec; `/startup-timeout` overschrijft `Behavior.StartupTimeoutSeconds` van de spec alleen omdat de vlag is opgegeven.

<a id="silent-detached-start"></a>
### Stille (losgekoppelde) start

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Exit `0` zodra `OmsiLaunchW.exe` is gestart (`{"delegated": true, "host_process_id": <pid>}`); `7` als `OmsiLaunchW.exe` ontbreekt (`OL_E_WINDOWS_HOST_MISSING`) of niet kon starten. De launcher keert direct terug en houdt de console of pipes van de aanroeper niet open: een script dat de uitvoer opvangt, krijgt meteen end-of-file (runtime-closure `T04`). De sessie zelf draait in `OmsiLaunchW.exe`: geen console-uitvoer, fouten als berichtvensters, systeemvakpictogram beschikbaar. Controleer de voortgang met `session status`, `events watch` en `.omsilaunch\diagnostics\<sessionId>-host.log`. Volledige referentie: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Een actieve sessie besturen (clientmodus)

Voer deze opdrachten uit vanuit dezelfde installatiemap terwijl er een eigenaar actief is. Elke opdracht eindigt met exit `4` (`OL_E_NO_ACTIVE_SESSION`) als geen eigenaar antwoordt en met `7` bij een control-fout.

```text
OmsiLaunch.exe session status --json
```
Exit `0`. Retourneert `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Wijzigt niets.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Exit `0` (`events watch` loopt tot Ctrl+C). Begrensde runtime-events (`gameplay.entered`, D3D-levenscyclusevents, ...). Wijzigt niets.

```text
OmsiLaunch.exe session stop
```
Exit `0` (`{"accepted": true, "session_id": "..."}`). Vraagt de canonieke stop aan: OMSI wordt beëindigd, overlays worden door de eigenaar hersteld en het journal wordt verwijderd. De client keert direct terug; het eigenaarproces sluit af na het herstel.

<a id="runtime-reads"></a>
## Runtime-leesacties

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Exit `0` met het `RuntimeCommandResult` (`Succeeded`, `Values`) in de envelope. Wijzigt niets. Timeout van 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, exit `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Exit `0`; `2` als een vereist argument ontbreekt (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, gemeld voordat de aanvraag wordt verstuurd); `7` als de plugin de aanvraag weigert: `OL_E_RUNTIME_OPERATION_FAILED` met de specifieke reden in `Values.detail`, bijvoorbeeld `OL_E_RUNTIME_OBJECT_HANDLE_STALE` voor een handle die niet langer hetzelfde object identificeert, of `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Handles gelden binnen de sessie en komen uit de voorafgaande `list`. Namen van variabelen, constanten en curven worden door elk voertuigmodel gedefinieerd: neem ze over uit het `list`-resultaat. De bovenstaande namen zijn weergegeven voor `rv-000001`, de spelersbus van een sessie met `situations\Linie 5.osn`. Wijzigt niets.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Exit `0`. Operaties zonder hiërarchische route, of elke willekeurige route, kunnen via de operatie-id worden aangesproken. `timetable.track-entries.list` is een begrensde lijst: op `situations\Linie 5.osn` retourneerde deze 137 van de 825 items met `truncated=true` (runtime-hertest van de documentatie-audit). Wijzigt niets.

<a id="runtime-writes"></a>
## Runtime-schrijfacties

```text
OmsiLaunch.exe time set --minute=30
```
Exit `0`. Wijzigt de klok van OMSI in het geheugen (gevalideerd: schrijven, teruglezen, herstel door een tweede `time set`). Wordt bij de stop niet teruggedraaid.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Exit `0` (`2` als `--family` ontbreekt bij `camera lock`). Wijzigt de cameratoestand voor de sessie. `camera lock` heeft een PlayerVehicle nodig (bijvoorbeeld een `/saved`-sessie); in een headless `/new`-sessie mislukt de opdracht (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` in `Values.detail`). Vergrendelen en ontgrendelen zijn runtime-gevalideerd met een opgeslagen situatie (families 0, 2 en 1, met teruglezen van de camera). Wordt bij de stop niet teruggedraaid; het vergrendelingsbeleid eindigt met de sessie.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Exit `0` (`2` als `handle`, `name` of `value` ontbreekt). Wijzigt één numerieke scriptvariabele van dat voertuig. Wordt niet teruggedraaid.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Exit `0`. Schrijft het bronweerrecord van OMSI en stopt live METAR-updates; OMSI leidt mist, licht, actuele wind en luchtvochtigheid ervan af bij zijn volgende weertick zonder pauze. Elke waarde wordt gecontroleerd voordat er iets wordt geschreven (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, exit `7`). Een afgeleide sleutel zoals `relative_humidity` retourneert `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` en noemt de sleutel die in plaats daarvan moet worden ingesteld. Niet teruggedraaid. EXPERIMENTAL: runtimevalidatie in afwachting.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Exit `0`. Schakelt over op live METAR-weer voor dat station; OMSI downloadt het bij zijn volgende weerupdate. EXPERIMENTAL: runtimevalidatie in afwachting.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Exit `0`. `scripts string set` loopt via het eigen `SetStringVariable` van OMSI en retourneert de teruggelezen waarde; een naam die het voertuigscript niet declareert, retourneert `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (exit `7`). `scripts trigger` activeert een trigger zoals de invoerafhandelaars van OMSI dat doen (`1` indrukken, `0` loslaten); het voertuigscript voert die direct uit. Trigger- en variabelenamen worden per voertuigmodel gedefinieerd; de bovenstaande zijn voorbeelden. Niet teruggedraaid. EXPERIMENTAL: runtimevalidatie in afwachting.

<a id="spawn"></a>
## Spawnen

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Exit `0` met de nieuwe `rv-NNNNNN`-handle in `Values` (`2` als `--model` ontbreekt; `7` bij `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Timeout van 30 s voor de client. Wijzigt de verzameling wegvoertuigen (één voertuig toegevoegd); wijst het spelersvoertuig niet toe. Wordt niet teruggedraaid; het voertuig verdwijnt bij de stop samen met OMSI.

## D3D-textures

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` is de `handle`-waarde die door `create` wordt afgedrukt (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` moet voor de 32-bits formaten worden gedecodeerd naar `width * height * 4` bytes (8 x 8 x 4 = 256 bytes) en naar maximaal 48 KiB. Exit `0`; `2` bij ontbrekende vereiste argumenten; `7` bij `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (tweede vrijgave, of describe na vrijgave), `OL_E_D3D_STALE_RESOURCE_HANDLE` (een handle van vóór een apparaatreset, of uit een andere sessie), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Maakt GPU-resources aan die eigendom zijn van de sessie; deze worden expliciet vrijgegeven of wanneer OMSI eindigt. Er worden geen bestanden aangeraakt.

<a id="owner-side-single-runtime-operation"></a>
## Eén runtime-operatie aan de kant van de eigenaar

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Exit `0`. Start een sessie, voert `time.read` één keer uit na `Running` (timeout van 5 s), schrijft `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, blijft 5 s actief, stopt en herstelt. Een runtimefout wordt afgedrukt als `runtime_error` en beëindigt de sessie niet.

## Recovery

```text
OmsiLaunch.exe /recovery-status --json
```
Exit `0`: `{"pending": false, ...}` als er geen journal bestaat, `{"pending": true, "recovered": false}` als er wel een bestaat. Exit `7` (`OL_E_INSTALLATION_BUSY`) zolang een eigenaar de installatie vasthoudt. Wijzigt niets.

```text
OmsiLaunch.exe /recover --json
```
Exit `0` als er niets openstond of het herstel is voltooid (`recovered: true`; `diagnostics` kan `restore.session-artifact-removed` en `OL_W_RESTORE_FOREIGN_FILE_RETAINED` bevatten); exit `8` als het journal openstond en nog steeds openstaat (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); exit `7` zolang een eigenaar of de in het journal vastgelegde OMSI de installatie vasthoudt (`OL_E_INSTALLATION_BUSY`); exit `10` (`OL_E_INTERNAL`) als de herstelde bytes de verificatie niet doorstaan (`Restore hash mismatch` of `Restore presence mismatch`; het journal blijft openstaan). Wijzigt: herstelt elk in het journal vastgelegd bestand uit `.omsilaunch\backup\<sessionId>\` na controle van de SHA-256 ervan, en verwijdert daarna het journal en de back-upmap. Geweigerd met `OL_E_INSTALLATION_BUSY` zolang de in het journal vastgelegde `Omsi.exe` nog actief is (runtime-closure `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Snelle controle van de exitcode (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
