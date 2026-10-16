# Capability

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Traduzione della [pagina originale in inglese](../../../reference/capabilities.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina documenta ciò che OmsiLaunch è in grado di fare. La sua fonte è il catalogo canonico delle capability, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): ogni capability (funzionalità), ogni operazione runtime pubblica con i suoi argomenti, le sue route CLI e la sua capability nativa, e uno stato di evidenza per capability vi sono dichiarati una volta sola. L'insieme delle operazioni runtime, la validazione degli argomenti, le route dei comandi CLI, il controllo nativo per operazione nel plugin, `GetCapabilitiesAsync` e la [tabella del catalogo](#capability-catalog) qui sotto ne sono derivati. Per ogni operazione runtime la pagina indica gli argomenti, le chiavi del risultato e gli errori. Come invocare un'operazione (route CLI, envelope API, timeout, handle) è descritto in [controllo runtime](runtime-control.md); l'evidenza per sessione si trova in [stato della validazione a runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificazione e stabilità

`PublicCapabilityClassification` ha quattro valori. Corrispondono al vocabolario di stabilità come segue, con declassamenti dove l'evidenza è incompleta:

| Classificazione | Significato | Stabilità |
| --- | --- | --- |
| `PublicStableBeta` | Pubblica, parte del contratto Beta, validata a runtime | `STABLE_BETA` (declassata a `PARTIAL` dove indicato) |
| `PublicExperimental` | Pubblica, può cambiare, validata a runtime o validata staticamente | `EXPERIMENTAL` (declassata a `PARTIAL` dove indicato) |
| `InternalOnly` | Primitiva di ricerca; non raggiungibile tramite l'API pubblica o la CLI | `INTERNAL` |
| `Unsupported` | Riconosciuta ai fini della diagnostica, rifiutata o assente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` ed `Event`. Ogni capability runtime richiede una sessione `Running` (`RequiresSession` nel registro) e la propria capability nativa risolta e verificata sul build in esecuzione; un'operazione la cui capability nativa non è disponibile viene rifiutata con `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` contrassegna le capability che dipendono da target nativi (vedere [compatibilità](compatibility.md)).

I risultati runtime sono dizionari di stringhe. Le chiavi che iniziano con `internal_` o terminano con `_address`, `_pointer` o `_vmt` vengono rimosse al confine dell'API (`ScrubInternalValues`) e non sono elencate qui. Le chiavi del risultato nelle tabelle delle operazioni che seguono sono gli insiemi esatti di chiavi restituiti dal prodotto: sono stati acquisiti eseguendo ogni operazione pubblica in una sessione reale (acquisizione per la documentazione della chiusura runtime, sessione `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; le righe degli elenchi sono scritte come `<row>.<n>.<field>`).

Come viene segnalato il fallimento di un'operazione (`CurrentRuntimeControl.Execute`):

| Fallimento | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rifiuto da parte del registro (operazione sconosciuta o `internal.*`, argomento obbligatorio mancante) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | nessuno |
| Un rifiuto deliberato del prodotto (una chiave meteo derivata in `weather.set`) | il codice specifico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (frase) |
| Un errore D3D | il codice `OL_E_D3D_*` specifico | `detail`, `native_status` |
| Un errore tipizzato lato plugin (handle obsoleto, nome sconosciuto, valore fuori intervallo, nessun veicolo del giocatore, ...) | il codice specifico, per esempio `OL_E_RUNTIME_OBJECT_HANDLE_STALE` oppure `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (il messaggio), `exception` (nome del tipo .NET) |
| Un'operazione la cui capability nativa non è risolta sul build in esecuzione | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (la capability nativa) |
| Un'eccezione imprevista lato plugin senza un codice | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Un risultato che non entra nella mailbox da 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (gli elenchi paginati vengono invece accorciati e proseguono con `next_cursor`, vedere [paginazione](#pagination)) | nessuno |

La colonna `Errors` di ciascuna tabella indica i codici specifici; arrivano in `ErrorCode` attraverso il plugin, il bridge, il piano di controllo, l'API e la CLI senza alterazioni.

<a id="capability-catalog"></a>
## Catalogo delle capability

Una riga per ogni voce del catalogo, generata da `PublicCapabilityRegistry.All` (il gate della documentazione fallisce quando questa tabella è obsoleta). `Session` è `Running` per le capability che richiedono una sessione in esecuzione. `Operations` sono gli id concreti delle operazioni runtime (righe di contratto in [operazioni runtime pubbliche](#public-runtime-operations)). `Native capability` indica le voci del catalogo dei target nativi (vedere [compatibilità](compatibility.md)) che devono essere risolte e verificate sul build in esecuzione. `Evidence` usa un unico vocabolario:

| Evidenza | Significato |
| --- | --- |
| RUNTIME_VALIDATED | Osservata in una sessione OMSI reale; gli id di sessione sono in [stato della validazione a runtime](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implementata e verificata offline; l'effetto a runtime non è stato osservato. |
| PARTIAL | Funziona per una parte del suo contratto; la nota indica la parte ancora aperta. |
| RUNTIME_VALIDATION_REQUIRED | Implementata, in attesa della propria validazione a runtime. |
| UNAVAILABLE | Riconosciuta ai fini della diagnostica e rifiutata, oppure non implementata; richiederla all'avvio rende il piano non eseguibile. |

Un'operazione che cambia qualcosa indica il proprio criterio di modifica dopo il suo id (`PublicRuntimeOperation.MutationPolicy`); nessuna operazione runtime viene registrata nel journal:

| Criterio di modifica | Significato |
| --- | --- |
| `none` | Solo letture (non mostrato). |
| `omsi-state` | Modifica lo stato in memoria di OMSI; nulla lo annulla; termina con il processo OMSI. |
| `session-resource` | Crea o modifica una risorsa di proprietà della sessione, rilasciata esplicitamente o al termine della sessione. |
| `session-policy` | Installa un criterio che il plugin riapplica finché non viene rimosso o la sessione termina. |

<!-- generated:capability-catalog:begin -->
| Capability | Classificazione | Tipo | Sessione | Operazioni | Capability nativa | Evidenza | CLI | Nota |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: gli eventi del plugin viaggiano attraverso un ring numerato; un lettore che resta indietro riceve telemetry.lost con il conteggio. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 round A: layout noto. |
| `boot.headless-start` | `PublicStableBeta` | Action | no |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | no |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI presenta il primo record di ciascuna etichetta esatta; l'avvio lo seleziona tramite tale etichetta. Chiusa staticamente; validazione a runtime in sospeso. |
| `world.saved-situation` | `PublicStableBeta` | Action | no |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | no |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: il ramo nativo è chiuso staticamente (BI-006); validazione a runtime in sospeso. Mai un'euristica sul file .osn più recente. |
| `world.explicit-date` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: scritta nell'orologio di avvio di OMSI prima di NewSituation (analisi statica); validazione a runtime in sospeso. |
| `world.explicit-time` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: scritta nell'orologio di avvio di OMSI prima di NewSituation (analisi statica); validazione a runtime in sospeso. |
| `world.explicit-year` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: scritta nell'orologio di avvio di OMSI prima di NewSituation (analisi statica); validazione a runtime in sospeso. |
| `world.weather` | `PublicExperimental` | Action | no |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): applicato subito dopo l'avvio del mondo, tramite il loadWeather proprio di OMSI o i suoi flag METAR; sovrascrive il meteo di una situazione salvata. Chiusa staticamente; validazione a runtime in sospeso. |
| `player-vehicle.model` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; risolta ai fini della diagnostica, non applicata. |
| `player-vehicle.repaint` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false in un avvio headless; lo snapshot completo con una situazione salvata. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: la sequenza nativa di assegnazione è chiusa staticamente (research/reports/beta4-static/entrypoint-last-player-vehicle.md section 3) ma non implementata prima della validazione a runtime; prevista per una beta successiva. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transazionale, validato offline. |
| `input.controller` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transazionale, validato offline. |
| `presentation.splash` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | no |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 round B: la soppressione viene applicata e riportata (internet-textures.suppressed) attraverso il ring di telemetria. |
| `internet-textures.override` | `PublicExperimental` | Write | no |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Chiusura runtime I01. |
| `session-profiles` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | Avvio con profilo RA-009; 1..99 e individuazione offline. |
| `hof.management` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 round A C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 chiusa staticamente: SetActualDateTime copia soltanto l'orologio del PC nell'orologio di avvio; le date di avvio usano world.explicit-date. Superata: l'ABI nativa e le postcondizioni non sono chiuse. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: scrive actWeather (Weather+0x08), il record da cui il tick meteo di OMSI deriva i valori; chiusa staticamente. I valori derivati (umidità, fattori di luce, vento effettivo) restano in sola lettura (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Validazione a runtime in sospeso. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Richiede un PlayerVehicle (una situazione salvata ne fornisce uno); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE in una NEW_MAP headless. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | Il rilevamento dell'handle obsoleto dopo una rimozione naturale (RV-002) non ha un produttore runtime sicuro ed è validato offline. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paged) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paged), `timetable.trips.list` (paged), `timetable.lines.list` (paged), `timetable.rv-files.list` (paged), `timetable.track-entries.list` (paged), `timetable.bus-stops.list` (paged), `timetable.station-links.list` (paged), `timetable.tours.list` (paged), `timetable.profiles.list` (paged), `timetable.tour-entries.list` (paged), `timetable.logs.read` (paged), `timetable.no-rv-numbers.list` (paged) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paged), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paged), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Scritture: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI copia una stringa con la forma di una costante; chiusa staticamente. Validazione a runtime in sospeso. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): TriggerXML/SoundTrigger virtuali tramite gli slot VMT profilati; chiusa staticamente. Validazione a runtime in sospeso. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Reset del dispositivo osservato (resetting, restored, invalidazione della generazione); una transizione lost distinta non è stata prodotta (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` è `InternalOnly`: esiste nel plugin a scopo di ricerca (restituisce un indirizzo nativo) e viene rifiutata al confine dell'API e dal piano di controllo locale, che per primi validano entrambi il nome dell'operazione rispetto a `PublicRuntimeOperationIds`; la CLI non ha alcuna route per essa. `session.stop` termina OMSI; l'arresto cooperativo non è implementato.

<a id="pagination"></a>
## Paginazione

Ogni operazione di elenco (contrassegnata «(paged)» nel [catalogo delle capability](#capability-catalog)) accetta due argomenti facoltativi e risponde con quattro campi di pagina:

| Argomento | Significato |
| --- | --- |
| `cursor` | Da dove inizia la pagina: il `next_cursor` della pagina precedente. Omesso: la prima pagina. Un cursore appartenente a un altro elenco, oppure malformato, produce `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Righe desiderate (un intero positivo). Maggiore della pagina massima dell'elenco: si usa il massimo. Non un intero positivo: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Chiave del risultato | Significato |
| --- | --- |
| `count` | Tutti i record presenti in OMSI. |
| `returned_count` | Righe in questa risposta. |
| `truncated` | `true` quando altri record seguono questa pagina. |
| `next_cursor` | Presente solo quando altri record seguono: passarlo come `cursor` per la pagina successiva. |

Il cursore è una chiave stabile, non una posizione. Per gli array di record (orario, nomi degli script, costanti, curve, HOF, conducenti, biglietti, log) le righe hanno chiave `<row>.<n>.<field>` (o `name.<n>`) secondo l'indice assoluto del record e il cursore indica l'indice successivo, quindi la prima pagina è uguale alla risposta della Beta 3. Per le collezioni di handle (`road-vehicles.list`, `humans.list`) le righe vengono restituite in ordine di handle e il cursore indica l'ultimo handle restituito: un oggetto che compare tra due pagine riceve un handle successivo e arriva in una pagina successiva; nessuno viene ripetuto. Trattare i valori dei cursori come opachi.

Dimensioni massime di pagina: 1000 per `road-vehicles.list` e `humans.list`; 512 per `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` e `vehicle.curves.list`; 256 per `timetable.tour-entries.list`, `timetable.logs.read` e `vehicle.hofs.read`; 128 per le altre liste dell'orario, `drivers.read` e `tickets.read`. Quando le righe di una pagina superano comunque la mailbox da 64 KiB, il plugin scarta le ultime righe finché la risposta non entra e `next_cursor` prosegue dalla prima riga scartata, quindi nessun elenco fallisce a causa delle sue dimensioni.

<a id="public-runtime-operations"></a>
## Operazioni runtime pubbliche

Questo è l'elenco esatto degli id di operazione che un frontend pubblico può inoltrare (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Ognuna richiede `SessionState.Running`; nessuna viene registrata nel journal né ripristinata. Gli argomenti obbligatori sono imposti dal registro prima che la mailbox venga usata (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); gli argomenti facoltativi sono validati dal plugin. Codici di errore comuni a tutte le operazioni: `OL_E_RUNTIME_OPERATION_UNKNOWN` (non presente in questo elenco), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (il plugin non ha un'implementazione), `OL_E_RUNTIME_OPERATION_FAILED` (eccezione imprevista nel processo; valori `detail` ed `exception`).

<a id="time"></a>
### Ora

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `time.read` | Read | nessuno | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | facoltativi `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimale); almeno uno | le chiavi di `time.read` dopo la chiamata profilata a `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Meteo

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | nessuno | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` e i valori sorgente `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, i fattori di luce e le umidità sono i valori per tick di OMSI; `cloud_type` è il numero del tipo di nuvola risolto | |
| `weather.set` | Write | almeno uno tra `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Intervalli: nebbia 0..100000, `lightness` e valori del terreno 0..1, velocità del vento 0..200, direzione del vento 0..360, temperatura e punto di rugiada -60..60, pressione 850..1100, precipitazione 0..2, tasso 0..255, altezza delle nuvole 0..20000, `cloud_type` da 1 a 64 caratteri della code page 1252 (`-1` = nessuna nuvola), booleani `true`/`false`/`1`/`0` | le chiavi di `weather.read`; con `cloud_type` anche `cloud_type_resolved` (`false` quando OMSI non conosce il tipo di nuvola: non ne disegna alcuna) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (una chiave derivata: `relative_humidity`, `absolute_humidity`, i fattori di luce, `cloud_transparency`, `precipitation_set`, `wet_ground`; il dettaglio indica la chiave da impostare), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (chiave sconosciuta), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 lettere o cifre) | le chiavi di `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | nessuno | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mappa

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `map.read` | Read | nessuno | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Telecamera

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | nessuno | `family` (0 conducente, 1 passeggero, 2 esterna, 3 mappa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | facoltativi `family` (0..3), `field_of_view` (10..170); almeno uno | le chiavi di `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obbligatorio `family` (0..3); facoltativo `preset` (0..255, solo con la famiglia 0 o 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. La policy viene riapplicata ogni 100 ms fino a `camera.unlock`; una riapplicazione fallita emette l'evento `camera.lock.degraded` una volta per ogni errore distinto. |
| `camera.unlock` | Action | nessuno | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Veicoli stradali e giocatore

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | nessuno | `count`, `ai_collection`, `player_index` (l'indice grezzo del veicolo del giocatore di OMSI, riportato così come letto; usare `present` di `player-vehicle.read` per sapere se esiste un veicolo del giocatore) | |
| `road-vehicles.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` è la posizione nell'elenco di OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | obbligatorio `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | nessuno | `present` (`false` quando OMSI non ha un veicolo del giocatore); quando è `true`: `player_index` più tutte le chiavi di `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obbligatorio `model` (`Vehicles\...\*.bus` canonico, al massimo 240 caratteri, senza `..`, deve esistere all'interno dell'installazione) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Non assegna il PlayerVehicle. Richiede diversi secondi; usare il timeout client di 30 s. |
| `road-vehicles.place-random` | Action | facoltativi `ai_type` (0..255, predefinito 0), `group` (0..65535, predefinito 1), `type` (-1..65535, predefinito -1), `scheduled` (0..1, predefinito 0), `tour` (0..65535, predefinito 0), `line` (0..65535, predefinito 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Human

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | nessuno | `count` | |
| `humans.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `human.<n>.handle` (`hb-NNNNNN`; `<n>` è la posizione nell'elenco di OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | obbligatorio `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Orario, conducenti, biglietti

Gli elenchi dell'orario, `drivers.read` e `tickets.read` sono paginati (vedere [paginazione](#pagination)); «campi di pagina» qui sotto indica `count`, `returned_count`, `truncated` e `next_cursor`, e ogni elenco accetta i `cursor` e `limit` facoltativi.

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | nessuno | `invalid` (`true`/`false`) e i conteggi dei record `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | `cursor`, `limit` facoltativi | campi di pagina; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `no_rv.<n>.group`, `no_rv.<n>.number`: i numeri di veicolo che OMSI tiene fuori dall'uso libero da parte dell'AI (l'elenco di stringhe dei numeri riservati di OMSI, letto tramite il suo layout; `OL_E_RUNTIME_VALUE_INVALID` quando l'elenco non ha la forma attesa). Al massimo 256 per pagina. Analisi statica; validazione a runtime in sospeso. |
| `map.tiles.list` | Read | `cursor`, `limit` facoltativi | campi di pagina; `tile.<n>.x`, `tile.<n>.y` (posizione nella griglia), `tile.<n>.tile_index` (l'indice proprio della tile nell'elenco delle tile della mappa), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` con `tile.<n>.error` per una tile che non può essere letta. Al massimo 256 per pagina. Posizione e indice da analisi statica; validazione a runtime in sospeso. |
| `drivers.read` | Read | `cursor`, `limit` facoltativi | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | `cursor`, `limit` facoltativi | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Script, costanti, curve e HOF del veicolo (legati a un handle)

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | `handle` obbligatorio; `cursor`, `limit` facoltativi | `handle`, campi di pagina, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obbligatori `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obbligatori `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (riletto) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | `handle` obbligatorio; `cursor`, `limit` facoltativi | `handle`, campi di pagina, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obbligatori `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | `handle` obbligatorio, `name` (con distinzione tra maiuscole e minuscole, come lo dichiara lo script, fino a 128 caratteri), `value` (da 1 a 1024 caratteri) | `handle`, `name`, `value` riletto dopo la scrittura | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (OMSI stesso ignorerebbe silenziosamente un nome sconosciuto), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | `handle` obbligatorio, `name` (code page 1252, fino a 128 caratteri), `value` (`1` esegue `name`, `0` esegue `name_off`, `2` esegue `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. Il punto di ingresso dello script viene eseguito in modo sincrono; un nome che lo script non definisce non produce alcun effetto. Una pressione è normalmente seguita da un rilascio |
| `vehicle.sound-trigger` | Action | `handle` obbligatorio, `trigger` (code page 1252, fino a 128 caratteri); `file` facoltativo (fino a 260 caratteri) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (anche: il veicolo non ha un pacchetto sonoro), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | `handle` obbligatorio; `cursor`, `limit` facoltativi | `handle`, campi di pagina, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obbligatori `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | `handle` obbligatorio; `cursor`, `limit` facoltativi | `handle`, campi di pagina, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obbligatori `handle`, `name`, `x` (float finito; un `x` mancante o non numerico produce `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolazione lineare) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | `handle` obbligatorio; `cursor`, `limit` facoltativi | `handle`, campi di pagina, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

Le operazioni D3D vengono eseguite sul thread principale/di rendering di OMSI tramite il bridge nativo. Gli handle delle texture hanno la forma `d3dtex-<sessionId N>-<16 hex digits>`.

| Operazione | Tipo | Argomenti | Chiavi del risultato | Errori |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | nessuno | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obbligatori `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); facoltativo `levels` (0..16, predefinito 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obbligatorio `handle`; facoltativo `level` (0..15, predefinito 0) | le 13 chiavi di `d3d.texture.create`, per il livello richiesto | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, più gli errori di create |
| `d3d.texture.update` | Action | obbligatori `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (al massimo 48 KiB dopo la decodifica); facoltativi `level` (0..15, predefinito 0), `x` (0..4095, predefinito 0), `y` (0..4095, predefinito 0) | le 13 chiavi di `d3d.texture.create` dopo l'aggiornamento | `OL_E_D3D_INVALID_PIXEL_BUFFER`, più gli errori di describe |
| `d3d.texture.release` | Action | obbligatorio `handle` | le 13 chiavi di `d3d.texture.create` con `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` in caso di rilascio ripetuto, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Le operazioni D3D fallite restituiscono i valori `detail` e `native_status`. I metodi di estensione di `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) incapsulano queste operazioni per chi utilizza l'API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` restituisce un `Capability(Name, Available, EvidenceState, Reason)` per ogni voce del catalogo che non è `InternalOnly`. `Name` è l'id di catalogo e `EvidenceState` la sua evidenza di catalogo. `Available` viene valutato per l'installazione:

- Le voci `Unsupported` non sono mai disponibili; `Reason` riporta la nota del catalogo.
- Ogni voce richiede la piattaforma corrente, e ogni voce tranne `runtime.current-windows-x64` richiede un'installazione di OMSI 2.
- Una voce con capability native sul layout noto riporta `native: RESOLVED_KNOWN_LAYOUT`. Su un altro build di OMSI 2 il file eseguibile viene risolto staticamente: `native: STATICALLY_RESOLVED`, oppure `native: PENDING_IN_PROCESS_RESOLUTION` con l'indicazione di ciò che il file non ha risolto. Una voce in sospeso viene comunque riportata come disponibile, perché decide la risoluzione nel processo; se questa fallisce, l'operazione o l'avvio fallisce con `OL_E_CAPABILITY_UNRESOLVED`.

Il planner riporta le capability per piano in `SessionPlan.RequiredCapabilities` e `SessionPlan.UnsupportedRequestedFeatures` con gli stessi id di catalogo (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Una funzionalità richiesta ma non supportata riporta la propria evidenza di catalogo; una capability nativa richiesta riporta `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Limitazioni note

- Gli handle sono legati alla sessione e vengono segnalati come `OL_E_RUNTIME_OBJECT_HANDLE_STALE` quando il loro oggetto non esiste più. Un handle di veicolo è associato al suo VMT, alla sua definizione e all'`IDCode` di OMSI (un numero univoco nel processo che ogni oggetto della mappa riceve alla costruzione e che non viene mai riutilizzato), quindi un oggetto ricreato allo stesso indirizzo viene sempre distinto. I human sono slot in pool che vivono per l'intera sessione della mappa: un handle di human smette di risolversi quando il human scompare (`RenderMe` 0) o lo slot viene generato di nuovo (nuova stazione di destinazione o partenza).
- I risultati sono limitati alla mailbox da 64 KiB; ogni elenco è paginato (vedere [paginazione](#pagination)).
- Nessuno spostamento di veicoli tra tile, nessuna scrittura del calendario, nessuna assegnazione headless del PlayerVehicle. Le scritture di variabili stringa, i trigger di script e le scritture del meteo sono `EXPERIMENTAL` (validazione a runtime in sospeso). Vedere [limitazioni note](known-limitations.md).
