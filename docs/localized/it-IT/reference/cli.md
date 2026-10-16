# Riferimento CLI

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Traduzione della [pagina originale in inglese](../../../reference/cli.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina è il riferimento completo e normativo della riga di comando di OmsiLaunch `0.1.0-beta4`: i tre eseguibili, la grammatica degli argomenti, l'ordine di dispatch, ogni parola di comando, ogni route gerarchica, ogni flag, gli envelope di output e il comportamento in caso di errore di ciascun comando. È generata da `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), da `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` e dai due shim nativi sotto `tools\OmsiLaunch.Bootstrapper`. I risultati del processo sono elencati in [codici di uscita](exit-codes.md); i codici di errore in [errori](errors.md); invocazioni commentate in [esempi CLI](cli-examples.md).

<a id="executables"></a>
## Eseguibili

| File | Sottosistema | Ruolo | Differenze |
|---|---|---|---|
| `OmsiLaunch.exe` | Console | Bootstrapper nativo (`OmsiLaunch.Bootstrapper.cpp`): risolve la propria directory, suddivide la riga di comando in token con `CommandLineToArgvW`, individua `hostfxr` tramite `nethost.dll` ed esegue `OmsiLaunch.Controller.dll` con gli stessi argomenti. | L'output viene scritto sulla console; il codice di uscita del processo è quello del controller gestito, oppure un codice dello shim `100`..`106` se non è stato possibile avviare l'host .NET. |
| `OmsiLaunchW.exe` | Windows (GUI) | Lo stesso shim (`OmsiLaunch.WindowsHost.cpp`) compilato per il sottosistema Windows. Imposta la variabile d'ambiente `OMSILAUNCH_WINDOWS_HOST=1` prima di avviare il controller. | Nessuna console: l'output su console viene soppresso a meno che non sia indicato `--json` (`WindowsHost.SuppressConsole`), gli errori vengono mostrati in finestre di messaggio (`WindowsHost.ShowFailure`: messaggio, `Code: OL_E_...` e l'indicazione `See .omsilaunch\diagnostics for details.`), e un errore dello shim `100`..`106` viene mostrato come `OmsiLaunch could not start the .NET host (code N).` Comportamento completo: [riferimento di OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Gestito (x64, `net6.0-windows`, Windows Forms) | Il controller vero e proprio. Non viene mai invocato direttamente dagli utenti; entrambi gli shim passano il percorso del controller come primo argomento dell'host, in modo che non compaia mai nell'elenco pubblico degli argomenti. | Richiede il runtime .NET 6 x64 con `Microsoft.WindowsDesktop.App`; vedere [installazione](../getting-started/installation.md). |

`nethost.dll` deve trovarsi accanto agli shim. Gli shim non leggono alcun argomento; ogni argomento raggiunge `CliInput.Parse` invariato, quindi `OmsiLaunch.exe` e `OmsiLaunchW.exe` accettano esattamente la stessa sintassi.

<a id="invocation-model"></a>
## Modello di invocazione

<a id="argument-grammar-cliinputparse"></a>
### Grammatica degli argomenti (`CliInput.Parse`)

| Forma | Significato |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Un flag. La chiave non distingue tra maiuscole e minuscole; il valore è tutto ciò che segue il primo `:`. Le chiavi sconosciute falliscono con `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), uscita `2`. |
| `--key=value` | Un argomento runtime per l'operazione runtime selezionata (per esempio `--handle=rv-000001`). Qualsiasi token `--` contenente `=` è un argomento runtime, mai un flag. |
| `--json`, `/json` | Output strutturato (vedere [Formati di output](#output-formats)). `--json` è l'unico token `--` senza `=` che abbia significato; viene interpretato come il flag `/json`. |
| parola semplice | Se non è ancora stata incontrata alcuna parola di comando e la parola è una delle [parole di comando](#command-words), diventa il comando. Una volta presente una parola di comando, ogni parola semplice successiva è una parola di comando (la route). Altrimenti la prima parola semplice è la radice dell'installazione e ogni parola semplice successiva viene aggiunta alla route. |

Conseguenze: una route gerarchica (`time get`) non può essere combinata con un argomento di installazione posto dopo di essa (`time get D:\OMSI` è la route sconosciuta `time get d:\omsi`, uscita `2`). `D:\OMSI time get` viene accettato ma è in **modalità owner** (viene avviata una nuova sessione e l'operazione viene eseguita una volta al suo interno). Gli errori di parsing (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) e gli errori del profilo di sessione (`SessionProfileException`) vengono segnalati prima che venga eseguito qualsiasi cosa, sempre con uscita `2`.

<a id="installation-root"></a>
### Radice dell'installazione

- Un argomento di installazione esplicito come parola semplice prevale su `RootPath` in un file `/spec` (`CliInput.BuildSpecAsync`).
- `.` indica la directory che contiene l'eseguibile (`AppContext.BaseDirectory`), mai la directory di lavoro del chiamante (`CliInput.ResolveInstallationRoot`). Un pacchetto portabile si basa su questo comportamento.
- Quando l'argomento è omesso, anche le operazioni in modalità owner (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) usano la directory dell'eseguibile. Il percorso viene normalizzato con `Path.GetFullPath`.
- I comandi in modalità client non accettano mai un argomento di installazione: si rivolgono all'endpoint di controllo locale dell'installazione in cui si trova l'eseguibile (`AppContext.BaseDirectory`). Vedere [controllo locale](local-control.md).

<a id="owner-and-client"></a>
### Owner e client

- **Owner**: il processo che pianifica, avvia, supervisiona e ripristina una sessione (`OwnerSession.RunAsync`). Detiene il lease dell'installazione (blocco esclusivo, `Local\OmsiLaunch.Installation.<sha256(root)>`) e la transazione di configurazione, espone l'endpoint di controllo locale finché la sessione è attiva e mostra l'[icona nell'area di notifica](windows-tray.md). Esattamente un owner per installazione: se un owner risponde già a `session.status` sull'endpoint di controllo, un secondo avvio fallisce con `OL_E_SESSION_ALREADY_ACTIVE` (uscita `7`).
- **Client**: qualsiasi invocazione senza argomento di installazione che invia `session status`, `session stop`, `events read`, `events watch` o un'operazione runtime. Viene inoltrata tramite la pipe di controllo locale; senza un owner fallisce con `OL_E_NO_ACTIVE_SESSION` (uscita `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Ordine di dispatch (`CliProgram.RunAsync`)

1. `/silent` (se non già in esecuzione sotto `OmsiLaunchW.exe`): avvia `OmsiLaunchW.exe` dalla directory dell'eseguibile tramite `ShellExecute` (senza ereditarietà degli handle) con gli stessi argomenti esclusi `/silent`/`--silent`, scrive l'envelope `silent` (`delegated`, `host_process_id`) e restituisce `0`. Il processo della console non attende la sessione; vedere [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` restituiscono `7`.
2. `/version`: envelope `version` con `product`, `version` (versione informativa dell'assembly, impostata da `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); uscita `0`.
3. `capabilities`: envelope con ogni descrittore `PublicStableBeta` o `PublicExperimental` del catalogo canonico `PublicCapabilityRegistry` (con le sue operazioni, capability native, evidenza e nota); uscita `0`.
4. `help [family]`: envelope `help` con `usage`, `product_version`, `protocol_version`, `family` e i `commands` pubblici (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), facoltativamente filtrati per famiglia; uscita `0`.
5. `profiles`: envelope con `requirement` (`OMSI 2`), il rapporto `installation` della radice di destinazione (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), i `known_layouts` e le `predicted_capabilities` risolte dal file eseguibile (fa fede la risoluzione nel processo all'avvio); uscita `0`.
6. Operazione runtime client (nessun argomento di installazione e una route o `/runtime:`): gli argomenti vengono validati tramite `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, uscita `2`), quindi `runtime.execute` viene inoltrato con un timeout di 8 s (30 s per `road-vehicles.spawn`).
7. Client `session status` (750 ms), `session stop` (vincolato all'id della sessione attiva, 750 ms), `events read` (750 ms), `events watch` (interroga ogni 250 ms fino a Ctrl+C).
8. `detect`, oppure **nessun argomento** (nessuna installazione, nessun comando, nessun `/?`, nessun `/spec`, nessun flag di avvio, nessun flag di recupero, nessun `/list`): enumera i processi `Omsi` e sonda l'endpoint di controllo (250 ms); envelope `detect`; uscita `0`.
9. `/?` o `/help`: stampa il testo di utilizzo, uscita `0`. Qualsiasi altra invocazione che abbia una parola di comando ma nessuna route eseguibile (per esempio `d3d` da solo o `session status D:\OMSI`) stampa il testo di utilizzo ed esce con `2`.
10. Modalità owner. Precondizioni: `plugins\OmsiLaunch.Plugin.opl` e `plugins\OmsiLaunch.Native.x86.dll` devono esistere accanto all'eseguibile (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, uscita `7`). `release-manifest.json` accanto all'eseguibile, se presente, fornisce gli hash attesi dei plugin.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; envelope `recover` con `pending`, `recovered`, `diagnostics`; uscita `8` solo quando è stato richiesto un ripristino che non è stato completato, altrimenti `0`.
12. `/list:<category>`: `DiscoverAsync`; envelope `content.list`; uscita `0`. `/list:SessionProfiles` usa invece `ListSessionProfilesAsync`; envelope `session-profiles.list`.
13. Costruisce il `LaunchSpec` (`BuildSpecAsync`), lo pianifica (`PlanSessionAsync`) e stampa il piano. `/plan` o `/validate`: uscita `0` se `IsRunnable`, altrimenti `1`. Un piano non eseguibile non avvia mai OMSI (uscita `1`); sotto `OmsiLaunchW.exe` un avvio con un piano non eseguibile mostra la sua ultima diagnostica `OL_E_` in una finestra di messaggio (audit della documentazione BUG-06). La pianificazione verifica inoltre la chiusura del plugin permanente installato rispetto a `release-manifest.json`, quindi un plugin mancante o alterato rende il piano non eseguibile (`OL_E_PERMANENT_PLUGIN_*`).
14. Sonda la presenza di un owner esistente (`OL_E_SESSION_ALREADY_ACTIVE`, uscita `7`), quindi `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Ciclo di vita dell'owner (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. Da questo punto ogni percorso di uscita raggiunge `CloseAsync` in un blocco `finally`: eccezioni, Ctrl+C (`Console.CancelKeyPress`), chiusura della console / disconnessione (`AppDomain.ProcessExit` con un budget di 4 s per arresto + ripristino; quanto rimane viene recuperato tramite il journal all'avvio successivo), "End session" dall'area di notifica, `session.stop` tramite pipe e `/observe-seconds`.
2. L'icona nell'area di notifica viene creata a meno che nello spec non sia impostato `Presentation.SuppressTrayIcon`.
3. Attende `Running` per `StartupTimeoutSeconds + 5` secondi. Lo stato viene stampato. Se lo stato non è `Running`, uscita `1` (`OmsiLaunchW.exe` mostra `The OMSI session did not reach gameplay.` con l'ultima diagnostica `OL_E_` o `OL_E_SESSION_START_FAILED`).
4. I batch di validazione (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) vengono eseguiti e scrivono i propri artefatti.
5. Si avvia l'endpoint di controllo locale.
6. `/runtime:<operation>` viene eseguito una volta (5 s, 15 s per `road-vehicles.spawn`); il risultato viene scritto in `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` e stampato. Un comando runtime non riuscito non termina mai la sessione (viene invece stampato `runtime_error`).
7. Attesa: con `/observe-seconds:n` la sessione viene arrestata dopo `n` secondi **oppure** prima, in seguito a un arresto dall'area di notifica o tramite pipe, oppure quando OMSI termina; senza questo flag l'owner attende finché OMSI termina o viene richiesto un arresto.
8. Viene stampato lo stato finale; uscita `0` se `Completed`, altrimenti `1`.

`session.stop`, "End session" dall'area di notifica, Ctrl+C e `CloseAsync` richiedono tutti l'arresto canonico: OMSI viene terminato con `TerminateProcess` (la routine di chiusura propria di OMSI non viene eseguita e `options.cfg` non viene riscritto da OMSI), quindi ogni file di proprietà della sessione viene ripristinato. Vedere [ciclo di vita della sessione](../concepts/session-lifecycle.md) e [transazioni e recupero](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Parole di comando

Ogni parola accettata in prima posizione (`CliInput.CommandWordsAccepted`):

| Parola | Scopo | Modalità | Note |
|---|---|---|---|
| `capabilities` | Elenca le capability pubbliche | Locale, senza sessione | Envelope `capabilities`. |
| `profiles` | Elenca le varianti di `Omsi.exe` supportate | Locale, senza sessione | Envelope `profiles`. |
| `detect` | Segnala i processi `Omsi.exe` e un eventuale owner attivo | Locale, senza sessione | È anche il comportamento predefinito quando non viene fornito alcun argomento. Stati: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, `UNKNOWN_BINARY_FOUND` per singolo processo quando il binario non può essere ispezionato; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Utilizzo e catalogo pubblico dei comandi | Locale, senza sessione | `help <family>` filtra per famiglia di capability (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Client | Esattamente una parola successiva; qualsiasi altra cosa stampa l'utilizzo, uscita `2`. `session plan`/`session start` sono nomi di route dell'API, non parole della CLI: usare `/plan` e `/new`. |
| `events` | `events read`, `events watch` | Client | `read` restituisce una volta l'elenco limitato degli eventi; `watch` stampa ogni nuovo evento (in base a `Sequence`) come envelope `events.watch` ogni 250 ms fino a Ctrl+C (uscita `0`), `4` quando nessun owner risponde, `7` in caso di errore di controllo. |
| `time` | `time get`, `time set` | Route client | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Route client | |
| `map` | `map get` | Route client | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Route client | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Route client | |
| `player` | `player get` | Route client | |
| `humans` | `humans list`, `humans get`, `humans summary` | Route client | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Route client | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Route client | |
| `constants` | `constants list`, `constants get` | Route client | |
| `curves` | `curves list`, `curves evaluate` | Route client | |
| `hof` | `hof get` | Route client | |
| `drivers` | `drivers list` | Route client | |
| `tickets` | `tickets get` | Route client | |
| `d3d` | Parola di famiglia riservata | Nessuna | `d3d` **non ha alcuna route gerarchica**: `d3d texture ...` è una route sconosciuta (uscita `2`) e `d3d` da solo stampa l'utilizzo (uscita `2`). Le operazioni D3D si raggiungono con `/runtime:d3d.status`, `/runtime:d3d.texture.create` e così via (vedere [Operazioni senza route](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Route gerarchiche

`CliInput.HierarchicalRoutes` associa una route in minuscolo a un id di operazione runtime. Tutte le route richiedono una sessione `Running` e vengono eseguite tramite la mailbox runtime (`ExecuteRuntimeAsync`). Le scritture runtime modificano solo lo stato in memoria di OMSI: non toccano mai i file, non fanno parte della transazione di configurazione e **non** vengono annullate all'arresto (OMSI viene terminato). La stabilità segue `PublicCapabilityRegistry` e la [matrice di validazione](../status/runtime-validation-status.md); dettagli e campi dei risultati sono in [controllo runtime](runtime-control.md).

| Route | Operazione runtime | Tipo | Richiede Running | Modifica OMSI | Partecipazione al ripristino | Stabilità | Note |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Sì | No | Nessuna | STABLE_BETA | Campi di orologio e calendario. |
| `time set` | `time.set` | Write | Sì | Sì (orologio in memoria) | Nessuna, non annullata | EXPERIMENTAL | Per esempio `--minute=<0..59>`; scrittura, rilettura e ripristino validati il 2026-09-20. |
| `weather get` | `weather.read` | Read | Sì | No | Nessuna | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Sì | Sì (meteo in memoria) | Nessuna, non annullato | EXPERIMENTAL | Per esempio `--wind_speed=5 --cloud_type="Cumulus 1"`. Meteo manuale: gli aggiornamenti METAR in tempo reale si interrompono. Validazione a runtime in sospeso. |
| `weather metar set` | `weather.metar.set` | Write | Sì | Sì (meteo in memoria; OMSI salva l'ICAO in `options.cfg`, che la sessione ripristina) | Nessuna, non annullato | EXPERIMENTAL | `--icao=<ICAO>`. Meteo METAR in tempo reale, come lo imposta la finestra del meteo di OMSI. Validazione a runtime in sospeso. |
| `weather actual get` | `weather.actual.read` | Read | Sì | No | Nessuna | EXPERIMENTAL | Stato del controller meteo attuale/ICAO. |
| `map get` | `map.read` | Read | Sì | No | Nessuna | STABLE_BETA | Nome della mappa, file, descrizione, numero di tile, intervallo di anni e lato di circolazione; validato nuovamente a runtime sullo slot della mappa corretto. |
| `camera get` | `camera.read` | Read | Sì | No | Nessuna | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Sì | Sì (valori scalari della telecamera, ad es. `--field_of_view=`) | Nessuna, non annullata | EXPERIMENTAL | Scrittura/rilettura del FOV validate. |
| `camera lock` | `camera.lock` | Action | Sì | Sì (policy con ambito di sessione) | Nessuna | EXPERIMENTAL | Richiede `--family=<0..3>` (conducente=0, passeggero=1, esterna=2, mappa=3), `--preset=<n>` facoltativo (famiglia 0 o 1). Richiede un veicolo del giocatore (per esempio una situazione salvata). Validato a runtime nella chiusura runtime (`CAM01`); la stringa `RuntimeValidation` del registro indica ancora `STATICALLY_VALIDATED` (vedere [capability](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Sì | Sì | Nessuna | EXPERIMENTAL | Rilascia la policy impostata da `camera lock` (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Sì | No | Nessuna | STABLE_BETA | Restituisce handle `rv-NNNNNN` con ambito di sessione. |
| `vehicles get` | `road-vehicle.read` | Read | Sì | No | Nessuna | STABLE_BETA | Richiede `--handle=`. Handle obsoleto: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Sì | No | Nessuna | STABLE_BETA | Conteggi e stato del giocatore, nessun handle. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Sì | Sì (aggiunge un RoadVehicle) | Nessuna, non rimosso | EXPERIMENTAL | Richiede `--model=Vehicles\...\*.bus`. Timeout client di 30 s, timeout owner di 15 s. Non assegna il veicolo del giocatore. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Sì | Sì | Nessuna | EXPERIMENTAL | `PlaceRandomBus` profilato. |
| `player get` | `player-vehicle.read` | Read | Sì | No | Nessuna | STABLE_BETA | Null semantico quando non esiste un veicolo del giocatore. |
| `humans list` | `humans.list` | Read | Sì | No | Nessuna | EXPERIMENTAL | Restituisce handle `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Sì | No | Nessuna | EXPERIMENTAL | Richiede `--handle=`. |
| `humans summary` | `humans.read` | Read | Sì | No | Nessuna | EXPERIMENTAL | Solo conteggi. |
| `timetable get` | `timetable.read` | Read | Sì | No | Nessuna | STABLE_BETA | Stato del gestore dell'orario. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Sì | No | Nessuna | STABLE_BETA | Parte della capability `timetable.read`; evidenza di lettura in batch 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable lines list` | `timetable.lines.list` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable tours list` | `timetable.tours.list` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable logs list` | `timetable.logs.read` | Read | Sì | No | Nessuna | STABLE_BETA | Come sopra. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Sì | No | Nessuna | EXPERIMENTAL | Numeri di veicolo che OMSI tiene fuori dall'uso libero da parte dell'AI (riservati dai turni dell'orario o da elenchi AI fissi). Paginato (`cursor`, `limit`, al massimo 256 per pagina). |
| `map tiles list` | `map.tiles.list` | Read | Sì | No | Nessuna | EXPERIMENTAL | Le tile della mappa: posizione nella griglia, indice proprio, file e stato di caricamento, conteggi di oggetti, percorsi e spline. Paginato (`cursor`, `limit`, al massimo 256 per pagina). |
| `drivers list` | `drivers.read` | Read | Sì | No | Nessuna | EXPERIMENTAL | Record dei conducenti. |
| `tickets get` | `tickets.read` | Read | Sì | No | Nessuna | EXPERIMENTAL | Record dei pacchetti di biglietti. |
| `hof get` | `vehicle.hofs.read` | Read | Sì | No | Nessuna | STABLE_BETA | Richiede `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Sì | No | Nessuna | STABLE_BETA | Richiede `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Sì | No | Nessuna | STABLE_BETA | Richiede `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Sì | No | Nessuna | STABLE_BETA | Richiede `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Sì | No | Nessuna | STABLE_BETA | Richiede `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Sì | No | Nessuna | EXPERIMENTAL | Richiede `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Sì | No | Nessuna | EXPERIMENTAL | Richiede `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Sì | Sì (variabile di script) | Nessuna, non annullata | EXPERIMENTAL | Richiede `--handle=`, `--name=`, `--value=` (numero finito). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Sì | No | Nessuna | EXPERIMENTAL | Richiede `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Sì | No | Nessuna | EXPERIMENTAL | Richiede `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Sì | Sì (stato dello script in memoria) | Nessuna, non annullato | EXPERIMENTAL | Richiede `--handle=`, `--name=`, `--value=`. `SetStringVariable` proprio di OMSI. Validazione a runtime in sospeso. |
| `scripts trigger` | `vehicle.trigger` | Action | Sì | Sì (esegue codice dello script del veicolo) | Nessuna, non annullato | EXPERIMENTAL | Richiede `--handle=`, `--name=`, `--value=` (`1` pressione, `0` rilascio, `2` trascinamento). Validazione a runtime in sospeso. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Sì | Sì (riproduce suoni del veicolo) | Nessuna, non annullato | EXPERIMENTAL | Richiede `--handle=`, `--trigger=`; facoltativo `--file=`. Validazione a runtime in sospeso. |

<a id="operations-without-a-route"></a>
### Operazioni senza route

Questi id di operazione pubblici (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) non hanno una route gerarchica e vengono invocati con `/runtime:<operation>` più `--key=value` o `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` obbligatori; `levels` facoltativo), `d3d.texture.describe` (`handle`; `level` facoltativo), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` obbligatori; `level`, `x`, `y` facoltativi), `d3d.texture.release` (`handle`). Le operazioni D3D sono EXPERIMENTAL; il ciclo di vita delle texture e l'invalidazione al reset del dispositivo sono validati a runtime (chiusura runtime `H02`, `D01`; vedere [capability](capabilities.md)). `timetable.track-entries.list` e `timetable.tour-entries.list` sono elenchi limitati: un risultato che non rientra nello slot runtime viene accorciato (`truncated=true`). `internal.road-vehicles.make-basic` è INTERNAL e viene rifiutato con `OL_E_RUNTIME_OPERATION_UNKNOWN` sia dalla CLI sia dall'API.

<a id="flags"></a>
## Flag

Ogni flag di `CliInput.KnownFlags`. La colonna «Fase» indica *avvio* (determina il `LaunchSpec`/piano di una nuova sessione), *runtime* (agisce su una sessione in esecuzione) o *controllo* (modifica il comportamento della CLI stessa). I flag interpretati solo per compatibilità (`CliInput.AcceptedNoEffectFlags`) sono indicati nella rispettiva riga.

<a id="control-and-output"></a>
### Controllo e output

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/?` | `/?` | disattivato | controllo | STABLE_BETA | Stampa il testo di utilizzo, uscita `0`. |
| `/help` | `/help` | disattivato | controllo | STABLE_BETA | Uguale a `/?`. (La parola semplice `help` restituisce invece il catalogo strutturato.) |
| `/version` | `/version` | disattivato | controllo | STABLE_BETA | Envelope `version`, uscita `0`. Valutato prima di ogni altro comando tranne `/silent`. |
| `/json` | `/json` o `--json` | disattivato | controllo | STABLE_BETA | Emette envelope JSON; forza inoltre l'output su console anche sotto `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | disattivato | controllo | STABLE_BETA | Sopprime l'output leggibile dall'utente in caso di successo. L'output JSON (`/json`) e gli errori vengono comunque scritti; il codice di uscita non cambia. |
| `/silent` | `/silent` (anche `--silent`) | disattivato | controllo | EXPERIMENTAL | Delega l'intera riga di comando a `OmsiLaunchW.exe` e restituisce `0` non appena il processo host è stato avviato. L'esito della sessione viene segnalato da `OmsiLaunchW.exe` (finestre di messaggio, icona nell'area di notifica), da `.omsilaunch\diagnostics` e dall'endpoint di controllo locale. La delega e le finestre di dialogo di errore sono validate a runtime (chiusura runtime `T04`); vedere [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | controllo | REMOVED | Rimosso in 0.1.0-beta4: rifiutato con `OL_E_INVALID_ARGUMENT` (codice di uscita `2`). Il proprietario della sessione serve sempre l'endpoint di controllo locale. |
| `/verbose` | `/verbose` | disattivato | avvio | PARTIAL | `DiagnosticsSpec.Verbose`. I valori vengono trasportati nello spec; il loro effetto è limitato alla traccia dell'host sotto `.omsilaunch\diagnostics`. |
| `/log` | `/log` | attivato (`DiagnosticsSpec.Log` è `true` per impostazione predefinita) | avvio | PARTIAL | `DiagnosticsSpec.Log`. Di fatto sempre attivo. |
| `/logall` | `/logall` | disattivato | avvio | PARTIAL | Imposta insieme `Verbose`, `ProcessTrace`, `PluginTrace` e `NativeTrace`. |
| `/omsi-logall` | `/omsi-logall` | disattivato | avvio | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | disattivato | avvio | PARTIAL | Alias di `/trace-process`. |
| `/trace-process` | `/trace-process` | disattivato | avvio | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | disattivato | avvio | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: il plugin riporta ogni operazione runtime (`plugin.trace.operation`: operazione, esito, codice, durata) nella traccia dell'host della sessione. |
| `/trace-native` | `/trace-native` | disattivato | avvio | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: il livello nativo riporta ogni installazione e rimozione di hook (`native.trace`) nella traccia dell'host della sessione. |

<a id="planning-validation-and-harnesses"></a>
### Pianificazione, validazione e harness

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/plan` | `/plan` | disattivato | avvio | STABLE_BETA | Costruisce e stampa il `SessionPlan`, senza avviare OMSI. Uscita `0` quando `IsRunnable`, altrimenti `1`. Richiede una selezione di avvio (`/new`, `/saved`, `/spec` o un argomento di installazione); `/plan` da solo, senza nient'altro, esegue `detect`. |
| `/validate` | `/validate` | disattivato | avvio | STABLE_BETA | Identico a `/plan` in questo build. |
| `/runtime-batch` | `/runtime-batch` | disattivato | runtime (owner) | INTERNAL | Harness di validazione: dopo `Running`, esegue l'insieme delle operazioni di lettura e scrive `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | disattivato | runtime (owner) | INTERNAL | Harness di validazione: letture più `time.set`, `camera.set` e `vehicle.variable.set` con ripristino; scrive `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | disattivato | runtime (owner) | INTERNAL | Harness di validazione per il ciclo di vita delle texture D3D; scrive `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | nessuno | runtime | STABLE_BETA (dispatch) | Seleziona un'operazione runtime pubblica tramite id. Modalità client (nessun argomento di installazione): inoltrata all'owner. Modalità owner: eseguita una volta dopo `Running`. Id sconosciuti: `OL_E_RUNTIME_OPERATION_UNKNOWN`, uscita `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (ripetibile) | nessuno | runtime | STABLE_BETA (dispatch) | Argomento runtime; equivalente a `--key=value`. `=` mancante: `/runtime-arg requires key=value`, uscita `2`. |

<a id="world-selection"></a>
### Selezione del mondo

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` è la modalità predefinita, ma un avvio viene richiesto solo quando è presente uno tra `/new`, `/saved`, `/last`, `/spec` | avvio | STABLE_BETA | NEW_MAP. Richiede `/map` e `/entrypoint-index` (un piano senza un indice di punto di ingresso presentato segnala `OL_E_ENTRYPOINT_REQUIRED`; senza `/map` non viene risolta alcuna mappa). `/new` non seleziona mai una mappa in modo implicito. |
| `/saved` | `/saved:<file.osn>` | nessuno | avvio | STABLE_BETA | SAVED_SITUATION. Mappa e posizione provengono dal file `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` vengono rifiutati insieme a `/saved` (uscita `2`). Situazione mancante: `OL_E_SITUATION_NOT_FOUND`; mappa della situazione mancante: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | nessuno | avvio | EXPERIMENTAL | LAST_MAP_STATE: carica il file `laststn.osn` proprio della mappa (scritto da OMSI alla chiusura della mappa) attraverso il ramo dello stato precedente del form Start. Richiede `/map`; una mappa senza `laststn.osn` produce `OL_E_SITUATION_NOT_FOUND`. Mai un'euristica sul file `.osn` più recente. Validazione a runtime in sospeso. |
| `/map` | `/map:<identity>` (per esempio `maps\Grundorf\global.cfg`) | nessuno | avvio | STABLE_BETA | Identità della mappa per `/new`, oppure l'ambito per `/list:Entrypoints`. Sconosciuta: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | nessuno | avvio | EXPERIMENTAL | Punto di ingresso tramite la sua etichetta esatta (con distinzione tra maiuscole e minuscole) o tramite un'identità di `/list:entrypoints`. OMSI presenta solo il primo record di ciascuna etichetta; un record successivo che la condivide viene rifiutato (`OL_E_ENTRYPOINT_NOT_PRESENTED`), così come uno sconosciuto (`OL_E_ENTRYPOINT_NOT_FOUND`). Validazione a runtime in sospeso. Mutuamente esclusivo con `/entrypoint-index` (l'identità prevale e azzera l'indice). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | nessuno | avvio | STABLE_BETA | Indice del punto di ingresso nell'elenco presentato (a partire da 1, come lo presenta OMSI). Obbligatorio per un piano NEW_MAP eseguibile. |

<a id="date-time-and-weather"></a>
### Data, ora e meteo

Questi flag vengono accettati e trasportati nel `LaunchSpec`. La data, l'ora e l'anno di avvio si applicano solo alle sessioni NEW_MAP: con `/saved` o `/last` il pianificatore li registra come `UNAVAILABLE` **e aggiunge `OL_E_CAPABILITY_UNAVAILABLE`, quindi il piano è NOT RUNNABLE (uscita `1`)**, perché una situazione salvata mantiene il proprio orologio. Il meteo si applica a ogni modalità di mondo. Tutti sono `EXPERIMENTAL`: il percorso di avvio nativo è chiuso staticamente e la validazione a runtime è in sospeso. Un file `/spec` o un profilo di sessione che li imposta ha lo stesso effetto.

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` o `/date:system` | non impostato | avvio | EXPERIMENTAL | `DateSpec` esplicito/di sistema: la data di avvio di una sessione NEW_MAP (vedere [launchspec](launchspec.md#datespec-timespec-yearspec)). Valore non interpretabile: `OL_E_INVALID_ARGUMENT`, uscita `2`; una data impossibile: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` o `/time:system` | non impostato | avvio | EXPERIMENTAL | `TimeSpec` esplicito/di sistema: l'ora di avvio di una sessione NEW_MAP. |
| `/year` | `/year:<n>` o `/year:system` | non impostato | avvio | EXPERIMENTAL | `YearSpec`: l'anno di avvio di una sessione NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | non impostato | avvio | EXPERIMENTAL | `WeatherMode.Preset`: un file della cartella `weather` dell'installazione, per nome (`Regen`) o per percorso (`weather\Regen.owt`); un file mancante produce `OL_E_WEATHER_INVALID` in fase di pianificazione. Caricato tramite il `loadWeather` proprio di OMSI subito dopo l'avvio del mondo; il METAR in tempo reale resta disattivato. Validazione a runtime in sospeso. |
| `/weather-icao` | `/weather-icao:<code>` | non impostato | avvio | EXPERIMENTAL | `WeatherMode.Icao`: meteo METAR in tempo reale per una stazione ICAO di quattro caratteri (altrimenti `OL_E_WEATHER_INVALID`). OMSI salva la stazione in `options.cfg`, che la sessione ripristina. Validazione a runtime in sospeso. |
| `/weather-real` | `/weather-real` | non impostato | avvio | EXPERIMENTAL | `WeatherMode.RealCurrent`: meteo METAR in tempo reale per la stazione che OMSI ha già configurato; senza di essa l'avvio fallisce con `OL_E_WEATHER_APPLY_FAILED`. Prevale l'ultimo tra `/weather`, `/weather-icao`, `/weather-real`. Qualsiasi meteo di avvio sovrascrive il meteo di una situazione salvata. |

<a id="player-vehicle"></a>
### Veicolo del giocatore

Accettati e risolti rispetto all'installazione, ma non applicati dal runtime: ogni campo impostato è `UNAVAILABLE` e aggiunge `OL_E_CAPABILITY_UNAVAILABLE` (piano NOT RUNNABLE, uscita `1`).

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | non impostato | avvio | UNAVAILABLE | Risolto per primo (`OL_E_VEHICLE_NOT_FOUND` se sconosciuto). |
| `/repaint` | `/repaint:<id>` | non impostato | avvio | UNAVAILABLE | Risolto solo insieme a `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | non impostato | avvio | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` se sconosciuto. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | nessuno | avvio | STABLE_BETA | Un'operazione di [gestione HOF](hof-management.md) di sessione: `HofSourceKind` e `Source`. Richiede almeno un `/hof-target`. Sostituisce l'`HofManagement` di un `/spec`; con un preset di profilo di sessione che ha `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Malformato: uscita `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (ripetibile) | nessuno | avvio | STABLE_BETA | Un veicolo di destinazione sotto `Vehicles\`. Senza `/hof-source`: uscita `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (ripetibile) | tutti i file | avvio | STABLE_BETA | Limita la sorgente a questi nomi di file. Senza `/hof-source`: uscita `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | avvio | STABLE_BETA | `HofApplyMode`. Senza `/hof-source`: uscita `2`. |
| `/module` | `/module:<id>` (ripetibile) | nessuno | avvio | EXPERIMENTAL | Richiede un [modulo runtime](runtime-modules.md) per la sessione (`LaunchSpec.Modules.Enable`, aggiunto ai moduli di un file `/spec`). Un modulo richiesto che non può essere caricato rende il piano non eseguibile con il suo codice `OL_E_MODULE_*` (uscita `1`). Le sue operazioni si raggiungono con `/runtime:module.<id>.<operation>` e `/runtime-arg`. Con un preset di profilo di sessione che imposta `enabled: false` per esso: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (ripetibile) | nessuno | avvio | EXPERIMENTAL | Tiene un modulo fuori dalla sessione, compreso un modulo caricato per impostazione predefinita, dalla sua mappa o dal file di impostazioni (`LaunchSpec.Modules.Disable`). Con un preset che imposta `enabled: true` per esso: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Senza valore: uscita `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (ripetibile) | nessuno | avvio | EXPERIMENTAL | Imposta un'impostazione di un modulo (`LaunchSpec.Modules.Settings`); prevale sul file `/spec`, sul file di impostazioni e sul valore predefinito. Una chiave che anche il preset selezionato imposta: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Non nella forma `<id>.<key>=<value>`: uscita `2`. Valore non valido o chiave non dichiarata: diagnostica del piano `OL_E_MODULE_SETTING_INVALID` (uscita `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` da solo significa `true` per un'impostazione booleana | nessuno | avvio | EXPERIMENTAL | Un flag che un modulo installato dichiara per una delle sue impostazioni (`flag` nel suo manifest; elencato da `/list:RuntimeModules`). Viene risolto quando si pianifica una sessione ed equivale a `/module-setting:<id>.<key>=<value>`. Un flag che nessun modulo installato dichiara, oppure usato al di fuori di un avvio: `Unknown argument`, uscita `2`. Un flag dichiarato da due moduli: `OL_E_MODULE_SETTING_INVALID`, uscita `2`. Un flag di modulo non sostituisce mai un flag integrato. |
| `/fleet` | `/fleet:<n>` | non impostato | avvio | UNAVAILABLE | Numero di flotta. |
| `/registration` | `/registration:<text>` | non impostato | avvio | UNAVAILABLE | Targa. |
| `/no-vehicle` | `/no-vehicle` | disattivato | avvio | STABLE_BETA | Rimuove qualsiasi veicolo del giocatore dalla base di partenza (`/spec` o profilo). Innocuo. |

<a id="configuration-overlays"></a>
### Overlay di configurazione

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (ripetibile; chiavi senza distinzione tra maiuscole e minuscole) | nessuno | avvio | STABLE_BETA | Overlay semantico di `options.cfg` da `ConfigurationCatalog` (per esempio `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Chiave sconosciuta: `OL_E_UNKNOWN_SETTING` (uscita `2`); chiave di sola lettura (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (uscita `2`); valore fuori intervallo o malformato: `OL_E_INVALID_SETTING_VALUE` al momento della costruzione dell'overlay. L'overlay è una modifica di sessione: ne viene acquisito uno snapshot, viene applicato prima dell'avvio di OMSI e ripristinato byte per byte all'arresto (RV-005 `RUNTIME_VALIDATED`). Conflitti con una chiave di proprietà del preset di un profilo selezionato: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Presentazione dello splash screen

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (senza distinzione tra maiuscole e minuscole) | `Managed` | avvio | STABLE_BETA | `Managed`: le BMP a 24 bit 640x480 incluse nel pacchetto vengono copiate una volta in `<root>\.omsilaunch\assets\splash` e `GUI\NewSplashscreen_ENG.bmp` più `GUI\NewSplashscreen_<lang>.bmp` vengono sovrapposti in modo transazionale e ripristinati esattamente (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (alias): i file di OMSI non vengono toccati. Valore mancante: `/splash requires Unset, Native, or Managed`, uscita `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (anche `pt-BR`, `de`, `fr`, `en`; qualsiasi altro valore ricade su `ENG`) | `[language]` da `options.cfg`, altrimenti `ENG` | avvio | STABLE_BETA | Seleziona il file di destinazione localizzato. |
| `/splash-assets` | `/splash-assets:<directory>` (i percorsi relativi vengono risolti sotto la radice dell'installazione) | `<root>\.omsilaunch\assets\splash`, altrimenti l'insieme incluso nel pacchetto | avvio | STABLE_BETA | Directory di asset personalizzata; deve contenere `ENG.bmp` e, per una lingua diversa dall'inglese, `<lang>.bmp`. Errori: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (riportati come `OL_E_SESSION_PRESENTATION_INVALID` nel piano; non eseguibile). |

### Internet Textures

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | avvio | EXPERIMENTAL | `Native`: non toccato. `Disabled`: il downloader profilato interno al processo viene soppresso. `Override`: il profilo `.itx` indicato viene sovrapposto come `Texture\standard.itx`; ogni destinazione HTTP(S) elencata in esso più `Texture\standard.ipr` diventano eliminazioni di sessione (rimosse per la durata della sessione, ripristinate all'arresto). Valore mancante: uscita `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | nessuno | avvio | EXPERIMENTAL | Obbligatorio con `Override` (`OL_E_ITX_PROFILE_REQUIRED`, uscita `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (devono essere coppie di righe URL/destinazione con URL `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (le destinazioni devono risolversi sotto `Texture\` senza percorsi assoluti, `..` o reparse point). |

<a id="session-profiles"></a>
### Profili di sessione

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | nessuno | avvio | STABLE_BETA (compilazione; offline `OmsiLaunch.ProfileTests`) | Carica `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (vedere [profili di sessione](session-profiles.md)). Richiede `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, uscita `2`). Il blocco `new:` si applica solo con `/new`; `compatibility.maps` viene imposto per `/new` e `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). I flag espliciti che entrano in conflitto con un campo di proprietà del profilo vengono rifiutati con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): mappa/punto di ingresso/data/ora/anno/meteo quando ne è proprietario il blocco `new:`, chiavi `/set` di proprietà del preset, flag dello splash quando il preset contiene `presentation`, flag delle internet texture quando contiene `internet-textures`, timeout quando contiene `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | nessuno | avvio | STABLE_BETA | Seleziona il preset tramite `index`. Fuori intervallo: uscita `2`. |

<a id="launchspec-file"></a>
### File LaunchSpec

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | nessuno | avvio | STABLE_BETA (loader testato offline; semantica di sessione identica a quella dei flag) | Carica un file JSON `LaunchSpec` come base di partenza (vedere [LaunchSpec](launchspec.md)) e contrassegna un avvio come richiesto. Regole (`LaunchSpecJson`): il file deve esistere (`OL_E_SPEC_NOT_FOUND`, uscita `6`); al massimo 1 MiB (`OL_E_SPEC_TOO_LARGE`, uscita `2`); la radice deve essere un oggetto (`OL_E_SPEC_INVALID`); nomi delle proprietà senza distinzione tra maiuscole e minuscole; commenti `//` e virgole finali consentiti; profondità al massimo 32; ogni proprietà sconosciuta viene rifiutata con il relativo percorso JSON (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, uscita `2`). |

**Precedenza** (`CliInput.BuildSpecAsync`): valori predefiniti → file `/spec` → `/predefined-profile` (sostituisce `Installation` e `World`, quindi applica il profilo) → flag espliciti. Un argomento di installazione esplicito prevale su `RootPath` nello spec. `/no-vehicle` rimuove il veicolo del giocatore dello spec; `/vehicle` e i flag affini vi si fondono campo per campo. Le chiavi `/set` si fondono in `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` sovrascrivono solo se indicati. `/startup-timeout` e `/shutdown-timeout` sovrascrivono solo se indicati; `Presentation.SuppressTrayIcon` proviene solo dallo spec (nessun flag). I flag di diagnostica vengono combinati in OR con `Diagnostics` dello spec.

<a id="content-discovery"></a>
### Individuazione dei contenuti

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; le categorie sono i valori di `ContentQueryKind` `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (senza distinzione tra maiuscole e minuscole) | nessuno | locale, senza sessione | STABLE_BETA | `DiscoverAsync` sull'installazione; envelope `content.list` con voci `Identity`, `Kind`, `DisplayName`; uscita `0`. `SessionProfiles` risponde con `ListSessionProfilesAsync` (envelope `session-profiles.list`) e `RuntimeModules` con `ListRuntimeModulesAsync` (envelope `runtime-modules.list`: id, versione, attivazione, operazioni, validità di ciascun modulo e le sue impostazioni con tipo, valore predefinito e flag). Categoria sconosciuta: `Unknown discovery category`, uscita `2`. I reparse point (junction/collegamenti simbolici) vengono ignorati, i file di OMSI vengono letti come Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | nessuno | locale | STABLE_BETA | Ambito inoltrato per ogni categoria tranne `Entrypoints`, che usa `/map` come ambito. |

<a id="timeouts-and-observation"></a>
### Timeout e osservazione

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` secondi | valore dello spec/profilo, altrimenti `180` | avvio | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. L'owner attende `Running` per questo valore più 5 s; `OL_E_STARTUP_TIMEOUT` termina la sessione con uscita `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` secondi | valore dello spec/profilo, altrimenti `30` | avvio | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: con `/cooperative-shutdown`, per quanto tempo il supervisore attende che OMSI termini prima di terminarlo forzatamente; senza di esso, OMSI viene terminato subito e il valore non ha effetto. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | disattivato | avvio | EXPERIMENTAL | `Behavior.CooperativeShutdown`: la terminazione di una sessione in esecuzione chiede a OMSI di uscire tramite la propria routine di uscita (quella eseguita dopo che un utente conferma l'uscita: salva `options.cfg` e il file `laststn.osn` della mappa, poi chiude) e attende `ShutdownTimeoutSeconds` prima di terminarlo forzatamente. Opt-in finché non validato a runtime; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` quando OMSI ha dovuto essere terminato forzatamente. I file della sessione vengono comunque ripristinati dopo l'uscita. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | nessuno (esecuzione finché OMSI termina o viene richiesto un arresto) | runtime (owner) | STABLE_BETA | Limite superiore della fase di esecuzione: dopo `n` secondi di `Running` viene richiesto l'arresto canonico. Un arresto dall'area di notifica o tramite pipe, oppure la chiusura di OMSI, la termina prima. `0` arresta immediatamente dopo `Running`. |

<a id="recovery"></a>
### Recupero

| Flag | Sintassi e valori | Predefinito | Fase | Stabilità | Comportamento |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | disattivato | locale | STABLE_BETA | Segnala se `<root>\.omsilaunch\journal.json` è in sospeso (`pending`), senza mai ripristinare; uscita `0`. Acquisisce il lease dell'installazione: `OL_E_INSTALLATION_BUSY` (uscita `7`) finché un owner lo detiene. |
| `/recover` | `/recover` | disattivato | locale | STABLE_BETA | Ripristina un journal in sospeso (i backup vengono prima verificati rispetto allo SHA-256 dello snapshot; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` riportati in `diagnostics`). Uscita `0` quando non c'era nulla in sospeso o il ripristino è stato completato; `8` quando un journal era in sospeso e rimane tale. Rifiutato con `OL_E_INSTALLATION_BUSY` finché il processo OMSI registrato nel journal (PID, ora di creazione, percorso dell'eseguibile) oppure, per un journal oltre `HandoffCreated` senza PID, qualsiasi `Omsi.exe` di quella radice è attivo. Ogni avvio di sessione esegue automaticamente lo stesso recupero prima di leggere l'installazione. |
| `/clear-module-data` | `/clear-module-data:<module id>` | nessuno | locale, nessuna sessione | EXPERIMENTAL | Elimina la directory dei dati del modulo `<root>\.omsilaunch\module-data\<id>` e nient'altro; envelope `module-data.clear` con `module` e `cleared` (`false` quando non c'era nulla da eliminare); uscita `0`. Acquisisce il lease dell'installazione: `OL_E_INSTALLATION_BUSY` (uscita `7`) mentre una sessione è proprietaria dell'installazione. Una cartella di dati del modulo raggiunta tramite un link viene rifiutata (`OL_E_MODULE_INTEGRITY`); un link al suo interno viene rimosso come link. |

<a id="output-formats"></a>
## Formati di output

- **Envelope di successo** (`CliInput.WriteEnvelope`, con `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, indentato. Le risposte inoltrate di `session status` ed `events read` aggiungono un membro `metadata` quando eventi più vecchi sono stati esclusi per rientrare nel frame di controllo (`events_dropped_count`, vedere [controllo locale](local-control.md)). Senza `--json` viene stampato solo `<object>` come JSON indentato, seguito da `Note: <n> older events were omitted to fit the control frame.` quando alcuni eventi sono stati scartati.
- **Envelope di errore** (`CliInput.WriteError`, con `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Senza `--json`: `OL_E_<CODE>: message` su una riga. Categorie: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Sotto `OmsiLaunchW.exe` lo stesso codice e lo stesso messaggio vengono mostrati in una finestra di messaggio.
- **Piano e stato** (`CliInput.Write`): i record `SessionPlan`, `SessionStatus` e `RuntimeCommandResult` vengono stampati come JSON indentato **senza** envelope. Senza `--json` un piano viene riassunto come `Plan: READY profile=Omsi23004_692EBFBF` o `Plan: NOT RUNNABLE profile=...`; gli altri record vengono comunque stampati come JSON. I valori enum vengono serializzati come interi (`SessionState.Running` è `14`, `Completed` è `18`, `Failed` è `19`).
- Nomi di comando usati negli envelope: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile` e l'id dell'operazione runtime per i comandi runtime inoltrati.
- Sotto `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) non viene scritto nulla sulla console a meno che non sia indicato `--json`.

<a id="errors-per-command"></a>
## Errori per comando

| Comando | Codici di errore tipici | Uscita |
|---|---|---|
| Qualsiasi errore di parsing | `OL_E_INVALID_ARGUMENT`, codici del profilo di sessione (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Route client, `/runtime` (client) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` restituiti dall'owner, ad es. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Verifiche preliminari dell'owner | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); in sospeso ma non recuperato (`8`) | `7`, `8` |
| `/list` | categoria sconosciuta (`2`); `OL_E_INSTALLATION_NOT_FOUND`/directory mancanti (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, avvio | diagnostiche del piano: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (chiusura del plugin installato), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informativa) | `1` |
| Avvio della sessione | `OL_E_PLAN_NOT_RUNNABLE` (nuova pianificazione all'avvio, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normalmente segnalati dalla pianificazione come diagnostica del piano, uscita `1`; `7` solo se i file del plugin cambiano tra la pianificazione e l'avvio), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (sessione `Failed`, `1`) | `1`, `7` |
| Eccezione non gestita in qualsiasi punto | classificata da `CliProgram.Classify` (vedere [codici di uscita](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Ambiente

| Variabile | Impostata da | Effetto |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: output su console soppresso, errori mostrati in finestre di messaggio, `/silent` non delegato di nuovo. |

<a id="see-also"></a>
## Vedere anche

[Esempi CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [codici di uscita](exit-codes.md) · [errori](errors.md) · [controllo locale](local-control.md) · [area di notifica di Windows](windows-tray.md) · [controllo runtime](runtime-control.md) · [capability](capabilities.md) · [LaunchSpec](launchspec.md) · [profili di sessione](session-profiles.md) · [pacchetti](packaging.md) · [compatibilità](compatibility.md) · [limitazioni note](known-limitations.md) · [API pubblica](public-api.md)
