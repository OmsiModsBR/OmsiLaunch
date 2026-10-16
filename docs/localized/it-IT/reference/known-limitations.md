# Limitazioni note

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> Traduzione della [pagina originale in inglese](../../../reference/known-limitations.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina elenca, a partire dal codice, tutto ciò che in OmsiLaunch 0.1.0-beta4 è `UNAVAILABLE`, `PARTIAL` o un rischio accettato, affinché utenti e integratori non facciano affidamento su comportamenti che il prodotto non fornisce. Ogni riga indica la limitazione, la sua stabilità, il motivo per cui esiste e dove è documentata in dettaglio. La documentazione inglese è normativa; le copie localizzate in `docs/localized/` non sono mantenute allo stesso livello e possono essere in ritardo (vedere l'ultima sezione).

<a id="compatibility"></a>
## Compatibilità

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| Le capability che leggono i layout degli oggetti (veicoli, human, meteo, mappa, telecamera, orario, script, conducenti, biglietti) vengono verificate solo su un layout noto (`Omsi23004`); su un altro build di OMSI 2 non sono disponibili (`OL_E_CAPABILITY_UNRESOLVED`) finché non esisteranno validatori strutturali | `PARTIAL` su build sconosciuti | [compatibilità](compatibility.md) |
| L'eseguibile Steam autentico (protetto da DRM) non è stato validato a runtime; una copia LAA controllata lo è stata | `PARTIAL` per Steam | [compatibilità](compatibility.md) |
| Solo il layout noto è stato validato a runtime; la risoluzione indipendente dal build viene verificata offline rispetto all'eseguibile di riferimento con la sua identità nascosta | `EXPERIMENTAL` su build sconosciuti | [stato della validazione a runtime](../status/runtime-validation-status.md) |
| Solo Windows 10+ x64; nessun supporto per Windows 7/8, XP o ARM64 | `UNAVAILABLE` | [compatibilità](compatibility.md) |
| Il plugin richiede il runtime .NET 6 **x86** oltre al runtime x64 usato dal controller | | [compatibilità](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Avvio del mondo e opzioni di avvio

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) è implementato a partire dall'analisi statica del ramo dello stato precedente di OMSI e validato solo offline | `EXPERIMENTAL` | [CLI](cli.md) |
| Data, ora e anno espliciti o di sistema (`/date`, `/time`, `/year`, `new.date`/`new.time`/`new.year` del profilo) si applicano solo agli avvii NEW_MAP e sono validati offline; la validazione a runtime è in sospeso. Il 31 dicembre di un anno bisestile non può essere richiesto (il calendario di OMSI riporta il giorno 366 all'inizio) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Preset meteo, ICAO e meteo reale attuale all'avvio (`/weather*`, `new.weather`) vengono applicati subito dopo l'avvio del mondo tramite il caricamento del meteo e i flag METAR propri di OMSI (chiusi staticamente, BI-003); la validazione a runtime è in sospeso. Il meteo che OMSI mostra al primissimo frame può essere ancora quello predefinito della mappa | `EXPERIMENTAL` | [CLI](cli.md) |
| Modello del veicolo del giocatore, repaint (livrea), HOF, numero di flotta, targa all'avvio (famiglia `/vehicle`, `PlayerVehicleSpec`) vengono risolti rispetto al catalogo dei contenuti ma non applicati; richiederli rende il piano non eseguibile. La sequenza nativa di assegnazione (posizionamento tramite la finestra di posizione di OMSI, `MakeVehicle`, i passaggi di finalizzazione per unità, `CopyTempListIntoMainList`, l'indice del giocatore) è chiusa staticamente, ma crea e registra un veicolo attivo e non viene implementata prima di poter essere validata a runtime; è prevista per una beta successiva. Una situazione salvata o `/last` ripristina il proprio veicolo del giocatore | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [capability](capabilities.md) |
| Il punto di ingresso per identità (`/entrypoint:<identity>`) seleziona l'etichetta esatta che OMSI presenta (chiuso staticamente, BI-001); un record successivo che condivide un'etichetta non può essere selezionato, come nella finestra di dialogo di OMSI stessa. Validazione a runtime in sospeso | `EXPERIMENTAL` | capability di piano `world.entrypoint-identity` |
| Le impostazioni di tastiera e controller per chiave (`Environment.Keyboard`, `Environment.Controllers`) non hanno chiavi catalogate: si sostituiscono invece interi file con `InputSpec` | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| L'arresto cooperativo (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) è opt-in e validato solo offline; per impostazione predefinita OMSI viene terminato forzatamente alla fine di una sessione e `ShutdownTimeoutSeconds` non ha effetto | `EXPERIMENTAL` | [CLI](cli.md) |
| I flag di diagnostica `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` popolano `DiagnosticsSpec` senza ulteriori effetti (la traccia dell'host in `.omsilaunch\diagnostics` viene sempre scritta); `/trace-plugin` e `/trace-native` vi aggiungono eventi di traccia del plugin e del livello nativo | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` sono harness di validazione | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Fine della sessione e controllo dei processi

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| L'arresto della sessione è una terminazione forzata: `session.stop`, «End session» nell'area di notifica, Ctrl+C e `CloseAsync` portano tutti a `TerminateProcess`. La routine di chiusura di OMSI non viene eseguita, OMSI non riscrive `options.cfg` né i propri log all'uscita, e qualsiasi stato di OMSI non salvato va perso. È una scelta deliberata: impedisce a OMSI di sovrascrivere i file ripristinati. | per scelta progettuale | [ciclo di vita della sessione](../concepts/session-lifecycle.md) |
| L'arresto cooperativo tramite WM_CLOSE con un timeout e una terminazione di riserva non è implementato | `UNAVAILABLE` (decisione di prodotto, S-11; nel ciclo di chiusura a runtime OMSI ha ignorato `WM_CLOSE` inviato alla sua finestra principale) | [stato della validazione a runtime](../status/runtime-validation-status.md) |
| Alla chiusura della console o al logoff l'owner dispone di un budget di 4 s per arrestare e ripristinare; ciò che resta viene recuperato dal journal all'avvio successivo | chiusura della console validata a runtime; logoff non verificato | [transazioni e recupero](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transazione, recupero e lease

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| Il lease dell'installazione (blocco esclusivo) è un semaforo `Local\`: un solo owner per installazione **per sessione di accesso**; non è imposto tra utenti diversi; non viene rilasciato finché un altro processo detiene un handle; qualsiasi processo dello stesso utente può detenere il nome | rischio accettato (S-18) | [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| Il recupero viene rifiutato (`OL_E_INSTALLATION_BUSY`) mentre è in esecuzione il processo OMSI registrato nel journal o, per un journal senza PID, un qualsiasi `Omsi.exe` di quella radice | per scelta progettuale | come sopra |
| Un percorso di overlay originariamente assente il cui contenuto è cambiato durante la sessione blocca il ripristino (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) finché non viene ispezionato | per scelta progettuale | come sopra |
| I journal precedenti ai fingerprint di proprietà possono essere chiusi solo da una sessione con byte pianificati identici (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | come sopra |
| Vengono ripristinati solo i percorsi di proprietà della sessione. Le scritture proprie di OMSI durante una sessione (`options.cfg` `[last_map]` quando nessuna impostazione applica un overlay a `options.cfg`, `Texture\standard.ipr`, cache, `laststn.osn`, profilo del conducente, log) persistono, come dopo un avvio diretto di OMSI | per scelta progettuale | [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| La rimozione di un `closecheck` obsoleto prima di una sessione è permanente (registrata, non ripristinata) quando `SuppressStaleClosecheckWarning` è true | per scelta progettuale | come sopra |

<a id="runtime-control"></a>
## Controllo runtime

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| `weather.set` e `weather.metar.set` scrivono il record meteo sorgente di OMSI (chiuso staticamente); la persistenza rispetto al tick meteo, la risoluzione del tipo di nuvola e il download METAR attendono la validazione a runtime. I valori derivati (umidità, fattori di luce, vento effettivo) non possono essere impostati; mentre OMSI è in pausa si aggiornano solo dopo la ripresa | `EXPERIMENTAL` | [capability](capabilities.md) |
| `SetActualDateTime` copia soltanto l'orologio del PC nell'orologio di avvio di OMSI (analisi statica); OmsiLaunch non lo chiama e imposta da sé l'orologio di avvio | per scelta progettuale | `calendar.set-actual-date-time` |
| Le scritture delle variabili stringa, i trigger con nome e i trigger sonori passano per i metodi propri di OMSI (chiusi staticamente, BI-004); il loro effetto sugli script dei veicoli attende la validazione a runtime. Sono bersaglio solo i veicoli stradali | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Nessun riposizionamento dei veicoli, nessuna riassociazione spaziale tra tile, nessuna autorità di trasformazione sicura per ODE; i campi di posizione sono di sola lettura | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` richiedono un PlayerVehicle; l'avvio headless NEW_MAP non ne ha uno (una situazione salvata lo fornisce) | per scelta progettuale (BI-007) | RV-004 |
| Le modifiche a runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, texture D3D) non sono registrate nel journal e non vengono ripristinate | per scelta progettuale | [controllo runtime](runtime-control.md) |
| L'obsolescenza degli handle viene decisa dal numero di istanza mai riutilizzato di OMSI stesso (`IDCode`) e, per i human, dalla scomparsa e dalla nuova generazione; è validata offline. La rimozione naturale (RV-002: auto AI che lasciano la rete di percorsi caricata) deve ancora essere osservata a runtime e non viene ancora emesso alcun evento `entity.removed` | `PARTIAL` | [controllo runtime](runtime-control.md) |
| I risultati sono limitati dalla mailbox da 64 KiB: gli elenchi lunghi vengono troncati (`truncated=true`); i payload di pixel sono limitati a 48 KiB per `d3d.texture.update` | per scelta progettuale | [capability](capabilities.md) |
| Canale a richiesta singola: una richiesta alla volta per sessione; uno slot occupato restituisce `OL_E_RUNTIME_CHANNEL_BUSY`; gli id di richiesta non devono essere riutilizzati | per scelta progettuale | [controllo runtime](runtime-control.md) |
| La telemetria è un ring di 64 eventi: un host che resta indietro di più di 64 eventi perde i più vecchi e ne viene informato con il conteggio (`telemetry.lost`) | `EXPERIMENTAL` | [plugin permanente](../concepts/permanent-plugin.md) |
| Gli hook sul dispositivo Direct3D di un modulo sono hook sulla vtable: su un'installazione in cui un altro componente riscrive i metodi originali del dispositivo (osservato sull'installazione di regressione poco dopo l'inizio del gameplay), smettono di essere chiamati e non possono essere rimossi durante la pulizia (`OL_E_MODULE_CLEANUP_INCOMPLETE`; il componente resta caricato) | `PARTIAL` | [moduli runtime](runtime-modules.md#native-components-and-hooks) |
| Il caricamento dei moduli runtime è aperto: OmsiLaunch non controlla chi ha realizzato un modulo; garantisce soltanto che i moduli non vengano mai eseguiti all'interno del processo OmsiLaunch e che un programma di sessione sia confinato al proprio compito (nessun secondo processo, terminato con la sessione) | per scelta progettuale (E21) | [moduli runtime](runtime-modules.md#security-boundary) |
| Il protocollo 2 dei moduli (impostazioni, attivazione predefinita, file di impostazioni, programmi di sessione, conflitti tra plugin, notifiche, `get_value`) è validato solo offline | `EXPERIMENTAL` | [moduli runtime](runtime-modules.md) |
| Il reset del dispositivo D3D è stato osservato a runtime (`resetting`, `restored`, invalidazione della generazione); una transizione `lost` distinta non è stata prodotta perché il dispositivo di OMSI è passato direttamente a `DEVICENOTRESET` | `PARTIAL` (RV-007) | [stato della validazione a runtime](../status/runtime-validation-status.md) |
| Gli elenchi runtime sono paginati (`cursor`, `limit`, `next_cursor`): una pagina contiene al massimo le righe massime dell'elenco e ciò che entra nello slot runtime da 64 KiB | `EXPERIMENTAL` | [capability](capabilities.md#pagination) |
| Alcuni campi avanzati del grafo di mappe/tile/percorsi/oggetti non sono esposti; i lettori runtime sono snapshot tipizzati vincolati al profilo, mai accessi arbitrari alla memoria | per scelta progettuale | [capability](capabilities.md) |
| Le letture della memoria in-process seguono lo schema verifica-poi-uso su un OMSI attivo; una modifica concorrente di OMSI tra la verifica e la lettura può produrre uno snapshot incoerente (`OL_E_RUNTIME_OPERATION_FAILED`) | rischio accettato (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Piano di controllo locale e modello di fiducia

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| Modello di fiducia basato sullo stesso utente: la named pipe (`CurrentUserOnly`), i mapping di memoria di handoff/telemetria/runtime e il semaforo del lease sono accessibili a qualsiasi processo dello stesso utente Windows. Un tale processo può leggere lo stato, arrestare la sessione o eseguire operazioni runtime dopo aver letto il `session_id`. | rischio accettato (S-06, S-30) | [controllo locale](local-control.md) |
| L'endpoint di controllo esiste solo mentre l'owner è `Running`; un client riceve `OL_E_NO_ACTIVE_SESSION` (uscita 4) durante l'avvio e dopo la fine della sessione | per scelta progettuale | [controllo locale](local-control.md) |
| Se un altro processo possiede già il nome della pipe, l'owner continua a essere eseguito senza endpoint (`ListenFault`), e un secondo avvio può segnalare erroneamente `OL_E_SESSION_ALREADY_ACTIVE` | rischio accettato | [controllo locale](local-control.md) |
| `.omsilaunch\` eredita l'ACL della radice di OMSI; non viene applicato alcun controllo di accesso esplicito | rischio accettato (S-31) | [transazioni e recupero](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnostica e output

| Limitazione | Stabilità | Dettagli |
| --- | --- | --- |
| La diagnostica è costituita solo da file locali (`.omsilaunch\diagnostics`); nulla viene caricato e non esiste alcuna segnalazione remota | per scelta progettuale | [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| La conservazione mantiene le 50 sessioni più recenti; la diagnostica più vecchia con prefisso di sessione viene eliminata all'avvio di una nuova sessione | per scelta progettuale | come sopra |
| L'output JSON e la diagnostica includono i percorsi dell'installazione (`RootPath`, directory degli asset, percorsi `.itx`) | per scelta progettuale (dati locali) | |
| La finestra di dialogo di errore della sessione di `OmsiLaunchW.exe` mostra come messaggio il payload di errore del plugin (per esempio `{"name":"world.failed",...}`) anziché una frase; la riga `Code:` è corretta | estetico | [area di notifica di Windows](windows-tray.md) |
| Una richiesta D3D rifiutata dal bridge nativo prima di qualsiasi chiamata Direct3D riporta correttamente `native_status`, ma il suo testo `detail` indica `HRESULT 0x00000000` | estetico | [capability](capabilities.md) |
| La finestra di stato dell'area di notifica è uno snapshot (istantanea) della sessione pianificata acquisito alla sua apertura; non si aggiorna e non mostra valori di OMSI in tempo reale | per scelta progettuale | [area di notifica di Windows](windows-tray.md) |

<a id="documentation"></a>
## Documentazione

Le pagine inglesi in `docs/` costituiscono la documentazione normativa di questa release. `docs/localized/<locale>/` contiene le traduzioni delle stesse pagine `0.1.0-beta4` (vedere [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); dove una traduzione differisce dal testo inglese, fanno fede il testo inglese e il codice. Le pagine storiche e legacy elencate lì sono disponibili solo in inglese.

Correlati: [capability](capabilities.md), [stato della validazione a runtime](../status/runtime-validation-status.md), [errori](errors.md).
