# Esempi CLI

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Traduzione della [pagina originale in inglese](../../../reference/cli-examples.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Invocazioni minime e corrette di `OmsiLaunch.exe` per OmsiLaunch `0.1.0-beta4`, ciascuna con il codice di uscita del processo atteso e una nota su ciò che viene modificato e ripristinato. Ogni esempio viene eseguito dalla radice dell'installazione di OMSI (`<OMSI_PATH>`), salvo indicazione contraria; la sintassi è definita nel [riferimento CLI](cli.md) e i codici di uscita in [codici di uscita](exit-codes.md). `--json` può essere aggiunto a qualsiasi comando per ottenere l'envelope strutturato.

<a id="conventions"></a>
## Convenzioni

- **Modifica**: file o stato di OMSI modificati dal comando. «Overlay di sessione» indica un file di cui viene acquisito uno snapshot nella transazione, applicato prima dell'avvio di OMSI e ripristinato byte per byte al termine della sessione.
- **Ripristinato**: ciò che viene annullato al termine della sessione (arresto normale, Ctrl+C, area di notifica, `session stop`, `/observe-seconds`) o tramite recupero.
- Le scritture runtime (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) modificano solo la memoria di OMSI; non vengono mai annullate perché OMSI viene terminato all'arresto.
- Segnaposto: `<OMSI_PATH>` è l'installazione di OMSI 2 che contiene il pacchetto OmsiLaunch (per esempio `C:\OMSI 2`); `<OTHER_OMSI_PATH>` un'altra installazione; `<SPEC_PATH>` e `<ITX_PATH>` un proprio file LaunchSpec e un proprio profilo Internet Textures (texture da Internet); `<HANDLE>` è un handle stampato dal precedente comando `list` o `create` e `<BASE64>` sono dati di pixel in Base64. Ogni altro valore è un letterale che funziona su un'installazione standard di OMSI 2 (`grundorf-quick` è il profilo di esempio definito in questa pagina).
- Racchiudere tra virgolette un percorso che contiene spazi e non terminare un percorso tra virgolette con `\` (il parsing degli argomenti di Windows trasforma `\"` in una virgoletta letterale): `"C:\OMSI 2"`, non `"C:\OMSI 2\"`.
- Ogni riga di comando di questa pagina viene analizzata dal gate della documentazione (`tests/OmsiLaunch.DocumentationTests`, gate `examples`); gli esempi di identità, individuazione, pianificazione e client sono stati inoltre eseguiti su un'installazione reale (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identità e individuazione (senza sessione)

```text
OmsiLaunch.exe /version
```
Uscita `0`. Stampa `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. Non modifica nulla.

```text
OmsiLaunch.exe profiles --json
```
Uscita `0`. Elenca gli hash di `Omsi.exe` supportati e il relativo stato di validazione. Non modifica nulla.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Uscita `0`. Catalogo pubblico delle capability; `help <family>` lo filtra. Non modifica nulla.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Uscita `0` (le due forme sono identiche). Segnala i processi `Omsi.exe` in esecuzione e se un owner OmsiLaunch risponde per questa installazione. Non modifica nulla.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Uscita `0` (`2` per una categoria sconosciuta). Individuazione in sola lettura; i cicli di junction vengono ignorati. Non modifica nulla. I valori `Identity` stampati qui sono le stringhe esatte che `/map`, `/saved`, `/vehicle-scope` e un `LaunchSpec` si aspettano (per esempio `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Pianificazione e validazione

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Uscita `0` quando il piano è `READY`, `1` quando è `NOT RUNNABLE` (per esempio `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Non modifica nulla; OMSI non viene avviato.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Uscita `0`/`1` come sopra; `/validate` è un alias di `/plan`. Il JSON è il `SessionPlan` grezzo (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Uscita `1`. `/date`, `/time`, `/year`, `/weather*` e i flag del veicolo del giocatore vengono accettati ma non applicati da questo build; il piano contiene `OL_E_CAPABILITY_UNAVAILABLE` e non è eseguibile.

```text
OmsiLaunch.exe /last /plan
```
Uscita `1`. `LAST_MAP_STATE` non è disponibile per questo profilo (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Avvio di sessioni (modalità owner)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Uscita `0` quando la sessione termina in `Completed`, `1` in caso di `Failed` o di piano non eseguibile. Modifica: overlay di sessione `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<lang>.bmp` (lo splash gestito è l'impostazione predefinita), gestione di `closecheck`, l'handoff di avvio. Ripristinato: ogni overlay, byte per byte, al termine della sessione. La console resta collegata finché OMSI termina, viene confermato "End session" dall'area di notifica, un client invia `session stop` o viene premuto Ctrl+C.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Uscita `0`. Come sopra, ma la sessione viene arrestata 8 s dopo aver raggiunto `Running` (prima in caso di arresto dall'area di notifica o tramite pipe). Usato dagli script di validazione.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Uscita `0`/`1`. SAVED_SITUATION: mappa, ora e posizione provengono dal file `.osn` (`situations\Linie 5.osn` è incluso in OMSI 2 e si avvia su Berlin-Spandau con un autobus del giocatore). Il valore è l'identità della situazione stampata da `/list:Situations` (relativa all'installazione, senza distinzione tra maiuscole e minuscole); un semplice nome di file come `Linie 5.osn` non viene risolto (`OL_E_SITUATION_NOT_FOUND`, uscita `1`). `/map` o `/entrypoint-index` insieme a `/saved` vengono rifiutati con uscita `2`. Modifiche e ripristino come per NEW_MAP. OMSI stesso registra la mappa della situazione in `options.cfg` `[last_map]`; questa scrittura di OMSI non viene annullata a meno che un `/set` non sovrapponga un overlay a `options.cfg` (vedere [transazioni e recupero](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Uscita `0`. Modifica: `options.cfg` (overlay di sessione, patch semantica di token/vettori; byte CP1252 preservati) più gli overlay dello splash. Ripristinato: `options.cfg` e i file dello splash esattamente (RV-005, RV-006). `/set:graphics.texture=...` esce con `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` esce con `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Uscita `0`. Modifica: nessun overlay dello splash; solo l'handoff di avvio e la gestione di `closecheck`. Ripristinato: nulla da ripristinare per lo splash.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Uscita `0` (`1` con `OL_E_SESSION_PRESENTATION_INVALID` quando la directory o una BMP manca oppure non è 640x480 a 24 bit). Modifica: `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_PTB.bmp` dalla directory personalizzata (overlay di sessione). Ripristinato: entrambi i file esattamente.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Uscita `0`. Modifica: nulla su disco oltre agli overlay dello splash; il downloader interno al processo viene soppresso per la sessione.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Uscita `0` (`2` con `OL_E_ITX_PROFILE_REQUIRED` se il profilo è omesso; `1` per un profilo non valido o una destinazione esterna a `Texture\`). Modifica: `Texture\standard.itx` (overlay di sessione); ogni destinazione elencata nel profilo e `Texture\standard.ipr` sono eliminazioni di sessione. Ripristinato: overlay rimosso, originali eliminati ripristinati; i file creati da OMSI in quei percorsi durante la sessione vengono rimossi come sottoprodotti della sessione.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Uscita `0`. Attende `Running` fino a 300 s (+5 s) invece dei 180 s predefiniti. `/shutdown-timeout:60` viene accettato ma non ha alcun effetto in questo build.

<a id="predefined-session-profile"></a>
### Profilo di sessione predefinito

File del profilo `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

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
Uscita `0`. Modifica: `options.cfg` (impostazioni del preset, overlay di sessione) e gli overlay dello splash. Ripristinato: tutti. L'aggiunta di `/map:...` o `/set:graphics.maxFPS=60` esce con `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); l'omissione di `/predefined-profile-index` esce con `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). L'esempio incluso nel pacchetto `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` mostra lo schema completo, ma così come viene distribuito il suo blocco `new:` richiede `date`, `time` e `weather`, che questo build non può applicare: pianificarlo con `/new` produce `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); rimuovere tali chiavi prima dell'uso.

<a id="launchspec-file"></a>
### File LaunchSpec

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Uscita `0`/`1`. L'esempio incluso nel pacchetto seleziona Grundorf, indice del punto di ingresso `1`, splash gestito, internet texture native; `RootPath: "."` viene risolto nella directory dell'eseguibile. Modifiche come per l'esempio NEW_MAP esplicito. Uno spec con una proprietà sconosciuta esce con `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); un file mancante esce con `6` (`OL_E_SPEC_NOT_FOUND`); un file superiore a 1 MiB esce con `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Uscita `0`/`1`. L'installazione esplicita `<OTHER_OMSI_PATH>` prevale su `RootPath` dello spec; `/startup-timeout` sovrascrive `Behavior.StartupTimeoutSeconds` dello spec solo perché è stato indicato.

<a id="silent-detached-start"></a>
### Avvio silenzioso (scollegato)

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Uscita `0` non appena `OmsiLaunchW.exe` è stato avviato (`{"delegated": true, "host_process_id": <pid>}`); `7` se `OmsiLaunchW.exe` manca (`OL_E_WINDOWS_HOST_MISSING`) o non è stato possibile avviarlo. Il launcher termina immediatamente e non mantiene aperti la console o le pipe del chiamante: uno script che ne cattura l'output riceve subito la fine del file (chiusura runtime `T04`). La sessione vera e propria viene eseguita in `OmsiLaunchW.exe`: nessun output su console, errori mostrati in finestre di messaggio, icona nell'area di notifica disponibile. Verificare l'avanzamento con `session status`, `events watch` e `.omsilaunch\diagnostics\<sessionId>-host.log`. Riferimento completo: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Controllo di una sessione in esecuzione (modalità client)

Eseguire questi comandi dalla stessa directory di installazione mentre un owner è in esecuzione. Ciascuno esce con `4` (`OL_E_NO_ACTIVE_SESSION`) quando nessun owner risponde e con `7` in caso di errore di controllo.

```text
OmsiLaunch.exe session status --json
```
Uscita `0`. Restituisce `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Non modifica nulla.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Uscita `0` (`events watch` rimane in esecuzione fino a Ctrl+C). Eventi runtime limitati (`gameplay.entered`, eventi del ciclo di vita D3D, ...). Non modifica nulla.

```text
OmsiLaunch.exe session stop
```
Uscita `0` (`{"accepted": true, "session_id": "..."}`). Richiede l'arresto canonico: OMSI viene terminato, gli overlay vengono ripristinati dall'owner, il journal viene eliminato. Il client termina immediatamente; il processo owner termina dopo il ripristino.

<a id="runtime-reads"></a>
## Letture runtime

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
Uscita `0` con il `RuntimeCommandResult` (`Succeeded`, `Values`) nell'envelope. Non modifica nulla. Timeout di 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, uscita `7`).

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
Uscita `0`; `2` quando manca un argomento obbligatorio (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, segnalato prima dell'invio della richiesta); `7` quando il plugin rifiuta la richiesta: `OL_E_RUNTIME_OPERATION_FAILED` con il motivo specifico in `Values.detail`, per esempio `OL_E_RUNTIME_OBJECT_HANDLE_STALE` per un handle che non identifica più lo stesso oggetto oppure `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Gli handle hanno ambito di sessione e provengono dal `list` precedente. I nomi di variabili, costanti e curve sono definiti da ciascun modello di veicolo: ricavarli dal risultato di `list`. I nomi riportati sopra sono stati elencati per `rv-000001`, l'autobus del giocatore di una sessione `situations\Linie 5.osn`. Non modifica nulla.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Uscita `0`. Le operazioni senza route gerarchica, o qualsiasi route, possono essere indirizzate tramite id di operazione. `timetable.track-entries.list` è un elenco limitato: su `situations\Linie 5.osn` ha restituito 137 voci su 825 con `truncated=true` (nuovo test runtime dell'audit della documentazione). Non modifica nulla.

<a id="runtime-writes"></a>
## Scritture runtime

```text
OmsiLaunch.exe time set --minute=30
```
Uscita `0`. Modifica l'orologio in memoria di OMSI (validato: scrittura, rilettura, ripristino tramite un secondo `time set`). Non annullato all'arresto.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Uscita `0` (`2` quando manca `--family` per `camera lock`). Modifica lo stato della telecamera per la sessione. `camera lock` richiede un PlayerVehicle (per esempio una sessione `/saved`); in una sessione `/new` headless fallisce (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` in `Values.detail`). Blocco e sblocco sono stati validati a runtime con una situazione salvata (famiglie 0, 2 e 1 con rilettura della telecamera). Non annullato all'arresto; la policy di blocco termina con la sessione.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Uscita `0` (`2` se manca `handle`, `name` o `value`). Modifica una variabile di script numerica di quel veicolo. Non annullato.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Uscita `0`. Scrive il record meteo sorgente di OMSI e interrompe gli aggiornamenti METAR in tempo reale; OMSI ne deriva nebbia, luce, vento effettivo e umidità al successivo tick meteo non in pausa. Ogni valore viene controllato prima di scrivere qualsiasi cosa (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, uscita `7`). Una chiave derivata come `relative_humidity` restituisce `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` e indica la chiave da impostare al suo posto. Non annullato. EXPERIMENTAL: validazione a runtime in sospeso.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Uscita `0`. Passa al meteo METAR in tempo reale per quella stazione; OMSI lo scarica al successivo aggiornamento meteo. EXPERIMENTAL: validazione a runtime in sospeso.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Uscita `0`. `scripts string set` passa per il `SetStringVariable` di OMSI e restituisce il valore riletto; un nome che lo script del veicolo non dichiara restituisce `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (uscita `7`). `scripts trigger` genera un trigger come fanno i gestori di input di OMSI (`1` pressione, `0` rilascio); lo script del veicolo lo esegue subito. I nomi dei trigger e delle variabili sono definiti da ciascun modello di veicolo; quelli sopra sono esempi. Non annullato. EXPERIMENTAL: validazione a runtime in sospeso.

## Spawn

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Uscita `0` con il nuovo handle `rv-NNNNNN` in `Values` (`2` quando manca `--model`; `7` in caso di `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Timeout client di 30 s. Modifica la collezione dei veicoli stradali (un veicolo aggiunto); non assegna il veicolo del giocatore. Non annullato; il veicolo scompare insieme a OMSI all'arresto.

<a id="d3d-textures"></a>
## Texture D3D

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` è il valore `handle` stampato da `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` deve decodificarsi in `width * height * 4` byte per i formati a 32 bit (8 x 8 x 4 = 256 byte) e in al massimo 48 KiB. Uscita `0`; `2` per argomenti obbligatori mancanti; `7` per `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (secondo rilascio, oppure describe dopo il rilascio), `OL_E_D3D_STALE_RESOURCE_HANDLE` (un handle precedente a un reset del dispositivo, o di un'altra sessione), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Crea risorse GPU di proprietà della sessione; rilasciate esplicitamente o al termine di OMSI. Nessun file viene toccato.

<a id="owner-side-single-runtime-operation"></a>
## Singola operazione runtime lato owner

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Uscita `0`. Avvia una sessione, esegue `time.read` una volta dopo `Running` (timeout di 5 s), scrive `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, resta in esecuzione per 5 s, quindi arresta e ripristina. Un errore runtime viene stampato come `runtime_error` e non termina la sessione.

<a id="recovery"></a>
## Recupero

```text
OmsiLaunch.exe /recovery-status --json
```
Uscita `0`: `{"pending": false, ...}` quando non esiste alcun journal, `{"pending": true, "recovered": false}` quando ne esiste uno. Uscita `7` (`OL_E_INSTALLATION_BUSY`) finché un owner detiene l'installazione. Non modifica nulla.

```text
OmsiLaunch.exe /recover --json
```
Uscita `0` quando non c'era nulla in sospeso o il ripristino è stato completato (`recovered: true`; `diagnostics` può contenere `restore.session-artifact-removed` e `OL_W_RESTORE_FOREIGN_FILE_RETAINED`); uscita `8` quando il journal era in sospeso e lo è ancora (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); uscita `7` finché un owner o il processo OMSI registrato nel journal detiene l'installazione (`OL_E_INSTALLATION_BUSY`); uscita `10` (`OL_E_INTERNAL`) quando i byte ripristinati non superano la verifica (`Restore hash mismatch` o `Restore presence mismatch`; il journal resta in sospeso). Modifica: ripristina ogni file registrato nel journal da `.omsilaunch\backup\<sessionId>\` dopo averne verificato lo SHA-256, quindi elimina il journal e la directory di backup. Rifiutato con `OL_E_INSTALLATION_BUSY` finché l'`Omsi.exe` registrato nel journal è ancora attivo (chiusura runtime `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Verifica rapida del codice di uscita (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
