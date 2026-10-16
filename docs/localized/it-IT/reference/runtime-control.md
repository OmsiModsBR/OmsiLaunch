# Controllo runtime

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Traduzione della [pagina originale in inglese](../../../reference/runtime-control.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Il controllo runtime è l'insieme delle operazioni di lettura, scrittura e azione che OmsiLaunch esegue all'interno di una sessione OMSI in esecuzione. Questa pagina spiega i tre modi per raggiungerlo (API dell'owner, client CLI, piano di controllo locale), come le richieste viaggiano dal chiamante al plugin e ritorno, come funzionano gli handle e le identità delle richieste, quali timeout si applicano, che cosa volutamente non viene registrato nel journal e le convenzioni per argomenti e risultati, con un esempio pratico per ogni famiglia. L'inventario delle operazioni vero e proprio, con argomenti, chiavi del risultato ed errori, si trova in [capability](capabilities.md). Fonti: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` e `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` e `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Tre punti di ingresso

| Punto di ingresso | Chi | Percorso | Timeout |
| --- | --- | --- | --- |
| API dell'owner | Un integratore che ha avviato la sessione nel proprio processo con `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> validazione del registro -> ricerca della sessione -> mailbox | `TimeSpan` fornito dal chiamante |
| CLI dell'owner (`/runtime:<op>`) | Il processo `OmsiLaunch.exe` proprietario della sessione | un'operazione eseguita subito dopo `Running`, con il risultato scritto in `.omsilaunch\diagnostics\<session>-runtime-operation.json` e sulla console; la sessione prosegue | 5 s (15 s per `road-vehicles.spawn`) |
| Client CLI | Qualsiasi invocazione di `OmsiLaunch.exe` **senza** argomento di installazione, per esempio `OmsiLaunch.exe time get` | named pipe `runtime.execute` verso l'owner dell'installazione in cui si trova l'eseguibile -> l'owner chiama `ExecuteRuntimeAsync` | 8 s (30 s per `road-vehicles.spawn`) sia sul lato client sia sul lato owner |
| Piano di controllo locale | Qualsiasi processo dello stesso utente Windows | stesso protocollo pipe del client CLI; vedere [controllo locale](local-control.md) | come sopra |

Ogni percorso termina in `ExecuteRuntimeAsync`, che impone il confine pubblico in quest'ordine:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: un'operazione non presente in `PublicRuntimeOperationIds` (incluse tutte le operazioni `internal.*`) restituisce `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; un argomento obbligatorio mancante o vuoto restituisce `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Nessuno dei due casi tocca la sessione.
2. Ricerca della sessione (`KeyNotFoundException` per un handle sconosciuto), `OL_E_RUNTIME_SESSION_MISMATCH` quando `RuntimeCommand.SessionId` differisce dall'handle, `OL_E_SESSION_NOT_RUNNING` a meno che lo stato non sia `Running`.
3. Richiesta alla mailbox (vedere sotto), dopodiché `ScrubInternalValues` rimuove ogni chiave del risultato che inizia con `internal_` o termina con `_address`, `_pointer`, `_vmt`.

Il client CLI e il piano di controllo eseguono autonomamente il passo 1 prima di contattare l'owner, quindi un nome di comando non valido viene segnalato con il codice di uscita 2 anche quando non è attiva alcuna sessione (`OL_E_RUNTIME_OPERATION_UNKNOWN` e `OL_E_RUNTIME_ARGUMENT_REQUIRED` corrispondono a `InvalidArguments`; gli altri rifiuti a 7; l'assenza di owner a 4).

L'endpoint del piano di controllo esiste solo mentre l'owner è `Running`, dopo l'avvio e dopo il completamento di qualsiasi harness `/runtime-batch`, `/runtime-write-batch` o `/d3d-batch`. I comandi che modificano lo stato (`runtime.execute`, `session.stop`) devono riportare il `session_id` attivo; la CLI lo ottiene automaticamente da `session.status` (altrimenti `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Identità della richiesta e mailbox

Un `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` viene serializzato in un envelope `RuntimeCommandWire` (magic `OLRC`, versione 1, header da 72 byte, SHA-256 del payload JSON) e depositato nella mailbox single-flight da 64 KiB della sessione (`OmsiLaunch.Runtime.<sessionId>`). Il plugin interroga la mailbox ogni 50 ms sul thread dell'interfaccia utente di OMSI, esegue lì l'operazione e scrive la risposta.

Regole che rendono robusto il canale:

| Regola | Effetto |
| --- | --- |
| Single flight | Una richiesta alla volta per sessione; l'host serializza i chiamanti con un semaforo. Uno slot ancora in stato `requested` quando arriva una nuova richiesta produce `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Associazione alla sessione | Il plugin ignora (e cancella) una richiesta il cui GUID di sessione non è il proprio; l'host rifiuta una risposta il cui id di sessione o di richiesta non corrisponde (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Quando la scadenza è superata, l'host riporta lo slot allo stato inattivo e lancia `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Risposta tardiva | Il plugin pubblica una risposta solo se lo slot contiene ancora il suo id di richiesta; la risposta a una richiesta abbandonata viene scartata. Se una risposta arriva comunque, la richiesta successiva dell'host trova uno slot `responded` obsoleto, lo scarta e prosegue; se quella risposta obsoleta riporta lo **stesso** id della nuova richiesta, l'host lancia `OL_E_RUNTIME_REQUEST_ID_REUSED`. I chiamanti non devono quindi mai riutilizzare un id di richiesta all'interno di una sessione. |
| Dimensione | Una richiesta più grande dello slot viene rifiutata prima del deposito (`ArgumentOutOfRangeException`). Una pagina di elenco che non entra viene accorciata dal plugin (ultime righe scartate, `truncated=true`, `returned_count` ridotto, `next_cursor` alla prima riga scartata); qualsiasi altra risposta sovradimensionata viene sostituita da un errore tipizzato `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. |
| Canale chiuso | Dopo la fine della sessione, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Gli id di richiesta sono valori `ulong` scelti dal chiamante. La CLI usa intervalli fissi: `10_001` per `/runtime:`, `50_001+` per le richieste inoltrate dal piano di controllo, `1+` per il batch di lettura, `20_000+` per il batch D3D, `30_000+` per `D3DRuntimeApi`. Gli integratori dovrebbero usare un contatore monotonicamente crescente per ogni sessione.

<a id="handles-and-stale-detection"></a>
## Handle e rilevamento degli handle obsoleti

| Prefisso | Tipo | Emesso da | Formato |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + sei cifre decimali (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + sei cifre decimali |
| `d3dtex-` | Texture D3D | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Gli handle sono opachi e legati alla sessione: vengono generati dal plugin, non codificano mai un indirizzo e non hanno significato in un'altra sessione. Quando un handle viene risolto, il plugin verifica che l'indirizzo a cui corrisponde sia ancora presente nella collezione attiva di OMSI (secondo l'ultima lettura dell'elenco; `road-vehicles.list` e `humans.list` aggiornano la vista) e rilegge un'impronta dell'oggetto (la VMT Delphi più il puntatore alla definizione del veicolo, oppure l'indice del modello del human). Una discrepanza significa che l'oggetto nativo è stato distrutto e il suo indirizzo riutilizzato: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Lo stesso token viene restituito di nuovo per lo stesso oggetto attivo in letture ripetute dell'elenco. Punto cieco residuo: un oggetto della stessa classe e definizione ricreato allo stesso indirizzo tra due letture dell'elenco non può essere distinto. Gli handle D3D sono validati dal bridge nativo: un handle sconosciuto o di un'altra sessione produce `OL_E_D3D_STALE_RESOURCE_HANDLE`, uno rilasciato `OL_E_D3D_RESOURCE_RELEASED`, e un cambio di generazione del dispositivo contrassegna le texture come `STALE`.

Gli handle non sono mai indirizzi nativi e non devono mai essere analizzati, confrontati numericamente, conservati tra sessioni o passati a un'altra sessione: vanno trattati come stringhe opache valide per la sessione che li ha emessi.

Evidenza di runtime: un handle D3D rilasciato resta rifiutato dopo la creazione di nuove texture e un handle di una sessione precedente viene rifiutato da quella successiva (chiusura runtime `H02`); un reset del dispositivo D3D invalida tutte le texture attive (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). Il rilevamento degli handle obsoleti di RoadVehicle e Human dopo la rimozione naturale (RV-002) non ha un produttore runtime sicuro (OMSI non ha rimosso oggetti nelle finestre di osservazione e nessuna operazione pubblica ne rimuove uno); è coperto offline.

<a id="what-is-not-journaled"></a>
## Che cosa non viene registrato nel journal

Le modifiche runtime cambiano solo la memoria di OMSI. **Non vengono registrate nel journal della transazione e non vengono ripristinate** al termine della sessione: `time.set`, `camera.set`, `camera.lock` (la policy si interrompe quando il plugin si arresta), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` e ogni risorsa `d3d.texture.*`. Scompaiono insieme al processo OMSI, che OmsiLaunch termina alla fine della sessione senza consentire a OMSI di rendere persistente alcunché (vedere [transazioni e recupero](../concepts/transactions-and-recovery.md)). Nessuna operazione della famiglia runtime tocca il filesystem.

<a id="argument-and-result-conventions"></a>
## Convenzioni per argomenti e risultati

- Gli argomenti sono coppie chiave/valore di stringhe. Nella CLI, `--key=value` dopo le parole di comando diventa un argomento runtime (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` è l'equivalente per `/runtime:<op>`. I numeri usano la cultura invariante (`.` come separatore decimale); i valori booleani sono `true`/`false`.
- Le parole di comando corrispondono agli id di operazione tramite `CliInput.HierarchicalRoutes` (per esempio `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). La tabella completa delle route si trova nel [riferimento CLI](cli.md). Le operazioni D3D non hanno una route a parole di comando; usare `/runtime:d3d.status` oppure `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- I risultati sono dizionari piatti di stringhe. Gli elenchi usano chiavi `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) con i campi di pagina `count`, `returned_count`, `truncated` e `next_cursor` (vedere [paginazione](capabilities.md#pagination)).
- I fallimenti riportano `Succeeded=false`, un `ErrorCode` `OL_E_` e, per i fallimenti lato plugin, `detail` (più `native_status` per D3D ed `exception` per gli errori imprevisti).

<a id="cli-envelope---json"></a>
### Envelope della CLI (`--json`)

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

Un envelope di errore ha la forma `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Senza `--json` la CLI stampa l'oggetto risultato come JSON indentato oppure come `CODE: message`.

<a id="api-envelope"></a>
### Envelope dell'API

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` lancia un'eccezione per le violazioni del confine successive alla validazione del registro (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) e restituisce `Succeeded=false` per i rifiuti del registro e per gli errori lato plugin.

<a id="worked-examples"></a>
## Esempi pratici

Tutti gli esempi CLI presuppongono che un owner sia in esecuzione per l'installazione che contiene `OmsiLaunch.exe` (avviato, per esempio, con `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, oppure con `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` quando un esempio richiede un veicolo del giocatore) e vengono eseguiti da una seconda console nella stessa installazione.

| Famiglia | Comando | Che cosa fa |
| --- | --- | --- |
| Sessione | `OmsiLaunch.exe session status --json` | Legge `SessionId`, `State`, la diagnostica e l'elenco limitato degli eventi runtime. |
| Ora | `OmsiLaunch.exe time get` poi `OmsiLaunch.exe time set --hour=7 --minute=30` | Legge l'orologio; lo imposta tramite la chiamata profilata a `SetTime` e restituisce la rilettura. |
| Meteo | `OmsiLaunch.exe weather get` e `OmsiLaunch.exe weather actual get` | Legge lo stato meteo corrente e quello reale/ICAO. `OmsiLaunch.exe weather set --wind_speed=5` imposta il meteo manuale (il record sorgente di OMSI; il METAR in tempo reale si interrompe) e `OmsiLaunch.exe weather metar set --icao=EDDF` passa al meteo METAR in tempo reale. Entrambi sono EXPERIMENTAL finché non validati a runtime. |
| Mappa | `OmsiLaunch.exe map get` | Identità della mappa, nome, descrizione, numero di tile, intervallo di anni e lato di circolazione. |
| Telecamera | `OmsiLaunch.exe camera set --field_of_view=50` poi `OmsiLaunch.exe camera lock --family=0 --preset=1` e `OmsiLaunch.exe camera unlock` | Scrive il FOV; blocca la famiglia del conducente con il preset 1 (richiede un veicolo del giocatore, per esempio una situazione salvata); rilascia la policy. |
| Veicoli | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Conteggi; handle; uno snapshot. |
| Spawn | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Genera un RoadVehicle IA (timeout di 30 s); restituisce `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` invoca `PlaceRandomBus`. |
| Giocatore | `OmsiLaunch.exe player get` | `present=false` in un avvio headless, altrimenti lo snapshot del giocatore. |
| Human | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Conteggi; handle; uno snapshot. |
| Orario | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Conteggi del gestore; righe dei tracciati limitate; log dell'orario. |
| Script | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Elenco/lettura/scrittura delle variabili numeriche; lettura/scrittura delle variabili stringa; trigger di script e di suoni (le scritture di stringhe e i trigger sono EXPERIMENTAL finché non validati a runtime). |
| Costanti e curve | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Costanti del veicolo e valutazione delle curve. I nomi dipendono dal modello: usare i nomi restituiti dal comando `list` (questi sono stati elencati per l'autobus del giocatore di `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | Metadati HOF della definizione del veicolo. |
| Conducenti e biglietti | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Record dei conducenti; pacchetto dei biglietti. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` poi `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` e `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Ciclo di vita della texture sul thread di rendering; `<HANDLE>` è l'`handle` stampato da `create`; `--pixels_base64` deve decodificarsi in `width * height * 4` byte (formati a 32 bit) e al massimo 48 KiB. |
| Eventi | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Elenco limitato degli eventi; osservazione a polling (250 ms) fino a Ctrl+C. |
| Arresto | `OmsiLaunch.exe session stop` | Richiede l'arresto canonico: OMSI viene terminato e la transazione viene ripristinata dall'owner. |

<a id="stability"></a>
## Stabilità

Il canale stesso (`runtime.command-channel`) è `STABLE_BETA`: integrità del formato di trasmissione, associazione alla sessione, scarto delle risposte tardive e rifiuto tipizzato delle risposte sovradimensionate sono coperti offline da `runtime-command.wire-guard`, `runtime-command.session-binding` e `runtime-command.late-response-ignored`, e a runtime da RV-003 (sessione `5f641c8d`), dal Round A RA-019 (un client ha abbandonato `road-vehicles.spawn` dopo 250 ms; la richiesta successiva è riuscita) e dalla batteria di test del canale della chiusura runtime `R03` (timeout, annullamento, id di richiesta riutilizzato, rifiuto da parte del plugin, operazione interna, sessione errata e argomento mancante, ciascuno seguito da una richiesta riuscita). La stabilità per singola operazione è indicata in [capability](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Riutilizzo del canale dopo i fallimenti

Ogni percorso terminale di una richiesta runtime lascia la mailbox riutilizzabile: successo, errore tipizzato, risposta malformata, sovradimensionata, di un'altra sessione o con id di richiesta errato, timeout, annullamento da parte del chiamante ed errori di decodifica terminano tutti in un'unica pulizia che riporta lo slot allo stato inattivo e azzera la lunghezza e l'header dell'envelope, così che nulla di una richiesta precedente possa essere letto da quella successiva. Una richiesta o una risposta residua trovata all'avvio di una nuova richiesta viene prima cancellata (`OL_E_RUNTIME_REQUEST_ID_REUSED` se riporta l'id della nuova richiesta). Sul lato plugin, un'eccezione all'interno di un'operazione riceve come risposta `OL_E_RUNTIME_OPERATION_FAILED`, un risultato più grande dello slot `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (un envelope fisso e piccolo), una richiesta per un'altra sessione `OL_E_RUNTIME_SESSION_MISMATCH`, mentre una richiesta abbandonata dall'host non riceve mai risposta. Queste regole sono coperte offline (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); i percorsi che un chiamante può produrre (timeout, annullamento, id riutilizzato, rifiuti tipizzati, risposta tardiva) hanno anche evidenza di runtime (RA-019, `R03`). Le risposte corrotte, di un'altra sessione o sovradimensionate non possono essere prodotte dall'esterno del prodotto e restano coperte solo offline.
