# Codici di errore e di diagnostica

<!-- l10n: source=reference/errors.md source-sha256=a3335bf22fcb58419c25d20745f040077113f1db8f973e0cb47d0469e852c5f9 -->
> Traduzione della [pagina originale in inglese](../../../reference/errors.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina è il riferimento normativo per ogni codice di `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 codici di errore `OL_E_` e 4 avvisi `OL_W_`, raggruppati per categoria del catalogo, oltre ai codici di diagnostica informativi che non sono errori. Per ogni codice la pagina indica dove il codice attuale lo solleva, che cosa significa, come arriva al chiamante (eccezione lanciata, campo del risultato, voce di diagnostica, risposta del piano di controllo o envelope della CLI) e che cosa fare. I significati sono ricavati dai punti in cui il codice viene sollevato; quando un codice è definito ma non ha alcun percorso attuale che lo sollevi, la pagina lo indica.

Pagine correlate: [API pubblica](public-api.md), [codici di uscita](exit-codes.md), [LaunchSpec](launchspec.md), [ciclo di vita della sessione](../concepts/session-lifecycle.md), [transazioni e recupero](../concepts/transactions-and-recovery.md), [piano di controllo locale](local-control.md), [controllo runtime](runtime-control.md), [profili di sessione](session-profiles.md), [plugin permanente](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Come arrivano i codici

| Superficie | Significato |
| --- | --- |
| Lanciato | Un'eccezione il cui `Message` inizia con il codice (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). La CLI estrae il codice dal messaggio e lo mappa su un codice di uscita (`CliProgram.Classify`). |
| Diagnostica del piano | Un `LaunchDiagnostic` in `SessionPlan.Diagnostics`; qualsiasi codice `OL_E_` rende `IsRunnable` falso (CLI: piano `NOT RUNNABLE`, uscita 1). |
| Diagnostica della sessione | Un `LaunchDiagnostic` in `SessionStatus.Diagnostics`; lo stato della sessione è `Failed` (CLI: uscita 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` e `OL_E_RESTORE_FAILED` incapsulano un codice interno nel proprio messaggio. |
| Risultato runtime | `RuntimeCommandResult.ErrorCode` con `Succeeded = false`. |
| Dettaglio runtime | `RuntimeCommandResult.ErrorCode` è il codice specifico e `Values["detail"]` il messaggio (con `Values["exception"]`, il tipo .NET). Beta 4: un codice tipizzato lato plugin raggiunge l'API e la CLI invariato; solo un errore privo di un codice di catalogo diventa `OL_E_RUNTIME_OPERATION_FAILED`. |
| Risposta di controllo | `ErrorCode` di una risposta del piano di controllo locale (`LocalControlResponse`). |
| Envelope della CLI | `error.code` nell'envelope `--json`, oppure `<code>: <message>` sulla console; il codice di uscita è indicato. |
| Telemetria | Un nome di evento runtime che l'host mappa su una diagnostica della sessione. |

## Cli

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | È sfuggita una `OperationCanceledException` (Ctrl+C o un'attesa del client annullata). | Envelope della CLI, uscita 7 | Ripetere il comando. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | È sfuggita un'eccezione senza codice `OL_E_`: JSON `/spec` malformato, un membro obbligatorio della spec nullo, un errore imprevisto. | Envelope della CLI, uscita 10 | Leggere il messaggio e `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; correggere l'input; segnalare il problema se non ha spiegazione. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | È sfuggita una `TimeoutException` senza codice; oppure il client del controllo locale era connesso a un owner che non ha risposto entro il timeout (l'owner esiste, quindi il caso non viene segnalato come `OL_E_NO_ACTIVE_SESSION`). (I timeout della mailbox riportano invece `OL_E_RUNTIME_REQUEST_TIMEOUT`.) | Envelope della CLI, risposta di controllo; uscita 5 da `Classify`, uscita 7 per una risposta di controllo | Riprovare; verificare che OMSI e l'owner rispondano. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` non si trova accanto a `OmsiLaunch.exe`. | Envelope della CLI, uscita 7 | Reinstallare il pacchetto. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` di `OmsiLaunchW.exe` non ha restituito alcun processo. | Envelope della CLI, uscita 7 | Controllare i file del pacchetto e i permessi; eseguire senza `/silent` per vedere l'errore. |

<a id="compatibility"></a>
## Compatibilità

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` su `plugin.build.invalid` | Le capability di avvio sono state risolte, ma il form Start attivo o uno dei suoi punti di hook non si trova nello stato atteso quando il plugin predispone l'avvio headless (per esempio un altro plugin lo ha già modificato con patch). | Diagnostica della sessione (`Failed`) | Rimuovere il plugin in conflitto; vedere [compatibilità](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` all'avvio; operazioni runtime) | Una capability nativa necessaria alla richiesta non ha potuto essere risolta e verificata su questo build di OMSI, quindi non è disponibile. All'avvio la sessione fallisce prima di qualsiasi hook e i file vengono ripristinati; un'operazione runtime viene rifiutata senza toccare la memoria di OMSI. La capability è indicata da `Data["capability"]` (runtime) o dal valore `capabilities` della telemetria. | Diagnostica della sessione (`Failed`); risultato runtime | Vedere [compatibilità](compatibility.md): la capability non è disponibile su questo build. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` non disponibile) | L'installazione non è un'installazione di OMSI 2: `Omsi.exe` manca, non è un'immagine PE x86 a 32 bit, non ha una versione di OMSI 2 (2.x), oppure la radice non ha una directory `maps\` o `Vehicles\`. L'hash dell'eseguibile non è un criterio. | Diagnostica del piano | Puntare OmsiLaunch alla radice di un'installazione di OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` non disponibile); viene lanciato anche da `CurrentWindowsX64Platform.ValidateCurrent`, che il servizio non chiama | Non è Windows 10 o successivo, oppure il sistema operativo o il processo host non è x64. | Diagnostica del piano (CLI: uscita 3 quando lanciato) | Eseguire su Windows 10 o successivo a 64 bit. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Solo `CurrentWindowsX64Platform.ValidateCurrent` | L'architettura del sistema operativo o dell'host non è x64. Il servizio non chiama `ValidateCurrent`; nessun percorso attuale lo solleva. | Lanciato (`PlatformNotSupportedException`) solo da quel metodo | Vedere il sorgente `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

<a id="content"></a>
## Contenuti

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validazione: NEW_MAP senza `EntrypointIdentity` e con `PresentedEntrypointIndex` non impostato o negativo. Pianificatore: la mappa non ha alcun punto di ingresso con l'identità richiesta o con l'etichetta esatta (con distinzione tra maiuscole e minuscole). | Diagnostica del piano | Impostare `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) oppure un'identità ottenuta da `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | Il meteo di avvio non può essere applicato: un preset che non è un file della cartella `weather` dell'installazione, oppure un ICAO che non è composto da quattro lettere o cifre. | Diagnostica del piano | Usare un preset installato o un codice di stazione valido. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | Il punto di ingresso richiesto condivide la propria etichetta con un record precedente della mappa; OMSI presenta solo il primo record di ciascuna etichetta, quindi non può essere selezionato all'avvio. | Diagnostica del piano | Scegliere il primo record con quell'etichetta, oppure un altro punto di ingresso. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` non disponibile) | La mappa NEW_MAP è stata risolta, ma non c'è né un indice presentato né un'identità. Accompagna sempre `OL_E_ENTRYPOINT_NOT_FOUND`. | Diagnostica del piano | Come sopra. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` non è un `Vehicles\...\*.hof` installato. | Diagnostica del piano | Usare un'identità ottenuta da `/list:hofs`. (I campi del veicolo del giocatore sono comunque non eseguibili su questo build.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Una sorgente della [gestione HOF](hof-management.md) (collezione, directory, veicolo o file) non esiste, non contiene alcun `.hof`, oppure manca un file indicato in `Files`. | Diagnostica del piano; errore di avvio | Correggere la sorgente o l'elenco dei file. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Un file o una cartella di veicolo di destinazione HOF non è installato. | Diagnostica del piano; errore di avvio | Usare un'identità di veicolo da `/list:vehicles` oppure una cartella esistente sotto `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Un percorso HOF esce dall'installazione, una destinazione non è sotto `Vehicles\`, il nome di una collezione non è un nome semplice, oppure viene attraversata una junction/symlink. | Diagnostica del piano; errore di avvio | Usare percorsi relativi all'installazione, senza `..` e senza link. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Due operazioni HOF scrivono byte diversi nello stesso file, oppure un mirror rimuove un file che un'altra operazione scrive. | Diagnostica del piano; errore di avvio | Riordinare, suddividere o rimuovere le operazioni in conflitto. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Operazione HOF senza destinazioni o senza sorgente, una sorgente file non `.hof`, un file di destinazione che non è `.bus`/`.ovh`, oppure un limite di dimensione o di numero (8 MiB per file, 256 file per operazione, 256 MiB per sessione). | Diagnostica del piano; errore di avvio | Correggere l'operazione. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validazione: NEW_MAP con `MapIdentity` non impostato o non nella forma `maps\<dir>\global.cfg`. Pianificatore: la mappa non è installata. | Diagnostica del piano | Usare un'identità ottenuta da `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | È sfuggita una `FileNotFoundException`/`DirectoryNotFoundException` senza codice, per esempio `/list:repaints` con un `/vehicle-scope` sconosciuto, oppure `/list:entrypoints` con una `/map` sconosciuta. | Envelope della CLI, uscita 6 | Correggere l'identità. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` non è un elemento `.cti` del modello (verificato solo quando `Model` è impostato). | Diagnostica del piano | Usare un'identità ottenuta da `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | La mappa referenziata all'interno del file `.osn` selezionato non è installata. | Diagnostica del piano | Installare la mappa o scegliere un'altra situazione. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION senza `SituationIdentity`, oppure il file `.osn` non è installato. | Diagnostica del piano | Usare un'identità ottenuta da `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` non è un `Vehicles\...\*.bus` installato. | Diagnostica del piano | Usare un'identità ottenuta da `/list:vehicles`. |

<a id="installation"></a>
## Installazione

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | Il lease dell'installazione (blocco esclusivo, `Local\OmsiLaunch.Installation.<hash>`) è detenuto da un altro owner in questa sessione di accesso, oppure un processo OMSI registrato nel journal (PID + ora di creazione + percorso dell'eseguibile; oppure qualsiasi `Omsi.exe` della radice per un journal oltre `HandoffCreated` senza PID) è ancora attivo. | Avvio: diagnostica della sessione tramite `OL_E_START_SESSION`. Recupero: lanciato (`InvalidOperationException` / `IOException`). CLI: uscita 7. | Arrestare l'altro owner (`session stop`) o attendere che OMSI termini, quindi riprovare oppure eseguire `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` è vuoto. | Diagnostica del piano | Passare la directory di installazione. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` non disponibile); anche `ValidateCurrent` | La radice non esiste, ha l'attributo di sola lettura oppure non ha una directory `plugins\`. | Diagnostica del piano | Indicare un'installazione di OMSI reale e scrivibile. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (piano e avvio) | Un file `plugins\OmsiLaunch.*` installato differisce dall'hash in `release-manifest.json` (o dalla chiusura di riferimento in assenza di manifest). | Diagnostica del piano (piano non eseguibile, CLI: uscita 1); diagnostica della sessione tramite `OL_E_START_SESSION` solo se i file cambiano tra la pianificazione e l'avvio | Reinstallare il pacchetto OmsiLaunch in modo che `plugins\` e il manifest concordino. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (piano e avvio) | Il manifest non ha una voce per un file del plugin richiesto. | Diagnostica del piano; diagnostica della sessione tramite `OL_E_START_SESSION` nella stessa race condition descritta sopra | Reinstallare il pacchetto. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (piano e avvio) | Un file `plugins\OmsiLaunch.*` richiesto è assente dall'installazione di OMSI, oppure un file `plugins/` elencato in `release-manifest.json` non è installato. | Diagnostica del piano; diagnostica della sessione tramite `OL_E_START_SESSION` nella stessa race condition descritta sopra | Installare la chiusura del plugin permanente ([installazione](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Solo `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` falso. Non chiamato dal servizio; nessun percorso attuale lo solleva. | Lanciato solo da quel metodo | Vedere il sorgente. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` è vuoto, non è JSON, non ha un array `files`, oppure ha una voce senza `path`/`sha256`, un hash che non è di 64 cifre esadecimali, un percorso assoluto, che contiene `:`, un segmento vuoto, `.` o `..`, oppure un percorso elencato due volte (confronto senza distinzione tra maiuscole e minuscole, `/` e `\` equivalenti). Un BOM UTF-8 è accettato. | Piano: incapsulato in `OL_E_RUNTIME_ARTIFACT_MISSING`; avvio: tramite `OL_E_START_SESSION` | Reinstallare il pacchetto. |

## InvalidArgument

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validazione: `Date.Value`/`Time.Value` impostato mentre la modalità non è `Explicit`. CLI: flag sconosciuto, valore mancante, intero o intervallo non valido, `/saved` combinato con `/map`/`/entrypoint`, route di comando sconosciuta, qualsiasi `ArgumentException`/`FormatException` senza codice. | Diagnostica del piano; envelope della CLI, uscita 2 | Correggere l'argomento. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (avvio) | Il valore di un'impostazione semantica è fuori intervallo, non booleano, non compreso nell'insieme consentito oppure malformato (`graphics.particles` richiede quattro campi). I valori non vengono validati in fase di pianificazione. | Diagnostica della sessione tramite `OL_E_START_SESSION` | Usare un valore della [tabella delle impostazioni](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | La chiave esiste ma non è scrivibile (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Diagnostica del piano; CLI: uscita 2 | Rimuovere la chiave. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | La chiave non è in `ConfigurationCatalog`. | Diagnostica del piano; CLI: uscita 2 | Usare una chiave del catalogo. |

## LaunchSpec

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | La radice non è un oggetto JSON, oppure la deserializzazione non ha prodotto alcun record. | Lanciato (`InvalidDataException`), CLI: uscita 2 | Correggere il file ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | Il file `/spec` non esiste. | Lanciato (`FileNotFoundException`), CLI: uscita 6 | Verificare il percorso. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | Il file supera 1 MiB. | Lanciato (`InvalidDataException`), CLI: uscita 2 | Ridurre il file. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Un membro che non è una proprietà pubblica del record in quella posizione; messaggio `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Lanciato (`InvalidDataException`), CLI: uscita 2 | Rimuovere o rinominare il membro. |

## LocalControl

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Gestore dell'owner (`OwnerSession`) | Il comando non è `session.status`, `session.events`, `session.stop` né `runtime.execute` con un argomento `operation`. | Risposta di controllo; CLI: uscita 7 | Usare un comando supportato. |
| `OL_E_CONTROL_FAILED` | Client CLI (`ReportForwarded`, `CliEventWatch`) | L'owner ha risposto `Ok = false` senza codice di errore. | Envelope della CLI, uscita 7 | Leggere il messaggio; controllare la console e la diagnostica dell'owner. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | Il gestore dell'owner ha lanciato un'eccezione il cui messaggio non contiene alcun codice `OL_E_` (per esempio la sessione era già chiusa), oppure la risposta del gestore non ha potuto essere serializzata. | Risposta di controllo | Leggere `session status`; riavviare l'owner se non è più presente. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (entrambi i lati) | Prefisso di lunghezza negativo o superiore a 64 KiB (compreso un frame di richiesta sovradimensionato), frame vuoto, JSON `null`, una richiesta senza `Command`, oppure JSON non decodificabile. | Risposta di controllo / envelope della CLI | Usare il protocollo documentato ([piano di controllo locale](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (client) | La richiesta serializzata del client stesso supera 64 KiB. Viene segnalato al chiamante; non viene inviato nulla. | Risposta di controllo / envelope della CLI | Ridurre la richiesta. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (owner) | La risposta dell'owner non rientra nel frame da 64 KiB. L'owner risponde con questo errore tipizzato invece di scartare la risposta. `session.status` e `session.events` non lo raggiungono mai: la loro cronologia degli eventi viene ridotta a partire dai più vecchi per rientrare nel limite. | Risposta di controllo / envelope della CLI | Riprovare; per gli eventi, leggerli più spesso. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | Il `ProtocolVersion` della richiesta non è `0.1`; la risposta dell'owner non ha potuto essere decodificata o era vuota; l'owner ha chiuso la connessione senza rispondere oppure la connessione si è interrotta dopo essere stata stabilita; l'owner non ha riportato alcun `SessionId`. | Risposta di controllo / envelope della CLI | Allineare le versioni di client e owner; leggere `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Gestore dell'owner | `session.stop` o `runtime.execute` senza un `session_id` uguale a quello della sessione attiva. | Risposta di controllo; CLI: uscita 7 | Leggere prima `session.status` e associare la richiesta (la CLI lo fa automaticamente). |

<a id="other"></a>
## Altro

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | Il piano passato ha `IsRunnable = false`, oppure la nuova pianificazione all'avvio non è eseguibile (`Omsi.exe` modificato, contenuti rimossi, chiusura del plugin mancante); il messaggio elenca i codici `OL_E_` attuali. | Lanciato (`InvalidOperationException`); CLI: uscita 1 | Pianificare di nuovo e correggere le voci di diagnostica elencate. |

<a id="presentation"></a>
## Presentazione

Tutti sollevati da `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). In fase di pianificazione sono incapsulati in `OL_E_SESSION_PRESENTATION_INVALID` (il messaggio contiene il codice); all'avvio emergono tramite `OL_E_START_SESSION`.

| Codice | Significato / causa tipica | Cosa fare |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | Il file `.itx` è vuoto, ha un numero dispari di righe non vuote, oppure una riga URL non è un URL `http`/`https` assoluto. | Usare coppie di righe URL/destinazione. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (risolto rispetto alla directory di lavoro del processo) non esiste. Lanciato come `FileNotFoundException`. | Passare un percorso `.itx` esistente. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` è `Override` senza `OverrideProfilePath`. CLI: uscita 2 quando lanciato. | Fornire `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Una riga di destinazione è un percorso assoluto, contiene `..`, inizia con `\`, si risolve al di fuori dell'installazione, non ha un componente `Texture\` oppure attraversa una junction/un collegamento simbolico. | Usare destinazioni relative `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` non esiste. | Correggere la directory. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` o `<LANG>.bmp` manca dalla directory degli asset, oppure manca un `assets\splash\<LANG>.bmp` del pacchetto durante il popolamento di `.omsilaunch\assets\splash`. | Fornire i file BMP / reinstallare il pacchetto. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Un BMP della splash screen non è una bitmap `BM` 640×480 a 24 bit. | Convertire l'immagine. |

<a id="process"></a>
## Processo

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (percorsi di errore dell'avvio e del supervisore) | La terminazione di OMSI o l'attesa della sua uscita durante la pulizia dopo un errore ha lanciato un'eccezione; segue il messaggio interno. | Diagnostica della sessione (aggiunta a una sessione `Failed`) | Assicurarsi che non rimanga alcun `Omsi.exe`, quindi eseguire `/recover` se un journal è in sospeso. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` è fallito subito dopo `CreateProcessW` (`Win32=<code>`); il processo viene terminato. | Diagnostica della sessione tramite `OL_E_START_SESSION` | Riprovare; controllare antivirus e permessi. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI è terminato prima di `gameplay.entered` (crash, chiusura di una finestra di dialogo di errore di OMSI, chiusura della finestra). | Diagnostica della sessione (`Failed`); il ripristino viene eseguito | Controllare i log di OMSI e `logfile.txt`; esaminare `RuntimeEvents` per l'ultimo evento del plugin. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` è fallito (`Win32=<code>` nel messaggio). | Diagnostica della sessione tramite `OL_E_START_SESSION` | Risolvere l'errore Win32 (file mancante, accesso negato, criterio). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | Il ciclo del supervisore ha lanciato un'eccezione (lettura della telemetria, attesa/terminazione del processo, scrittura del journal); OMSI viene terminato e si tenta il ripristino. | Diagnostica della sessione (`Failed`) | Leggere il messaggio interno e il log dell'host. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` è fallito (`Win32=<code>`). | All'interno dei messaggi `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Terminare OMSI manualmente, quindi eseguire `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` sull'handle del processo è fallito. | All'interno dei messaggi `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Come sopra. |

## Runtime

«Dettaglio runtime» significa il codice specifico in `ErrorCode`, con il messaggio in `Values["detail"]` e il tipo di eccezione in `Values["exception"]`.

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` con `preset` mentre `family` è 2 (esterna) o 3 (mappa); i preset esistono solo per il conducente (0) e il passeggero (1). | Dettaglio runtime | Omettere `preset` oppure usare la famiglia 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Modalità `Date`/`Time` `Explicit` senza valore o con componenti fuori intervallo. Il nome è storico; si tratta di un errore di validazione in fase di pianificazione. | Diagnostica del piano | Correggere il valore (e tenere presente che data/ora esplicite non sono eseguibili su questo build). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: il percorso `.bus` non esiste sotto la directory di lavoro di OMSI (verificato prima della chiamata nativa, così OMSI non può sostituirlo con un fallback). | Dettaglio runtime | Usare un'identità ottenuta da `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | come sopra | MakeVehicle nativo ha modificato la collezione dei veicoli stradali di più di un oggetto. | Dettaglio runtime (`native_status`, conteggi nel messaggio) | Segnalare il problema; gli oggetti creati restano fino al termine della sessione. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | come sopra | La collezione non è cambiata; OMSI ha rifiutato il veicolo senza segnalarlo. | Dettaglio runtime | Controllare il file `.bus`; provare un altro modello. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | come sopra | Qualsiasi altro stato nativo diverso da zero. | Dettaglio runtime | Segnalare il problema con i conteggi del messaggio. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | La chiamata profilata PlaceRandomBus ha restituito uno stato di errore. | Dettaglio runtime | Riprovare quando il gameplay è stabile; segnalare il problema. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; controlli lato plugin (`time.set` senza `hour`/`minute`/`second`; `camera.set` senza `family`/`field_of_view`; `camera.lock` senza una `family` interpretabile; operazioni su veicoli/curve) | Un argomento obbligatorio manca o è vuoto. | Risultato runtime (registro; CLI: uscita 2) o dettaglio runtime (plugin) | Fornire l'argomento ([controllo runtime](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | La directory/il file di riferimento della chiusura del plugin o il bridge nativo indicati in `OmsiLaunchRuntimePaths` non possono essere caricati (può incapsulare `OL_E_RELEASE_MANIFEST_INVALID`). | Diagnostica del piano | Eseguire da un pacchetto integro. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, harness INTERNAL) | La lettura di base `time.read`/`camera.read` è fallita, quindi il test di scrittura è stato saltato. | Solo artefatto del batch | Non destinato agli utenti. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` è vuoto, più lungo di 240 caratteri, contiene NUL o `..`, non inizia con `Vehicles\` oppure non termina con `.bus`. | Dettaglio runtime | Passare `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | nessuno (mantenuto per compatibilità) | Non viene più emesso. I build precedenti lo sollevavano quando una richiesta annullata restava nello slot; ora ogni percorso terminale di una richiesta reimposta lo slot, e una richiesta o risposta residua trovata all'inizio di una nuova richiesta viene eliminata. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | La mailbox è stata rilasciata perché la sessione sta terminando. | Lanciato (`InvalidOperationException`) | Nessuna azione; la sessione è terminata. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | Lo slot della mailbox conteneva un valore di stato diverso da inattivo, richiesto o con risposta (corruzione). Lo slot viene reimpostato e l'errore viene sollevato; la richiesta successiva funziona normalmente. | Lanciato (`InvalidDataException`) | Riprovare; segnalare il problema se persiste. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | Il puntatore al blocco delle costanti del veicolo è nullo. | Dettaglio runtime | Il veicolo non ha costanti; non c'è nulla da fare. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` non è nella tabella delle costanti del veicolo. | Dettaglio runtime | Elencare prima le costanti. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | L'oggetto creato dallo spawn ha una VMT al di fuori dell'intervallo dell'immagine di OMSI. | Dettaglio runtime | Segnalare il problema. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | come sopra | L'oggetto creato non è nella collezione dei veicoli stradali. | Dettaglio runtime | Segnalare il problema. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Due punti consecutivi della curva hanno la stessa X. | Dettaglio runtime | Problema di contenuto nella curva del veicolo. |
| `OL_E_RUNTIME_CURVE_EMPTY` | come sopra | La curva non ha punti. | Dettaglio runtime | Come sopra. |
| `OL_E_RUNTIME_CURVE_INVALID` | come sopra | Nessun segmento della curva contiene `x`. | Dettaglio runtime | Valutare all'interno del dominio della curva. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | come sopra | `name` è sconosciuto oppure il suo puntatore a funzione è nullo. | Dettaglio runtime | Elencare prima le curve. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | Il puntatore alla definizione del veicolo è nullo. | Dettaglio runtime | L'handle si riferisce a un veicolo privo di dati di definizione. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (modalità owner) | `plugins\OmsiLaunch.Plugin.opl` o `plugins\OmsiLaunch.Native.x86.dll` manca accanto all'eseguibile. | Envelope della CLI, uscita 7 | Reinstallare il pacchetto. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` mancante o vuoto per `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (normalmente il registro li rifiuta prima con `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Dettaglio runtime | Fornire l'handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | L'handle è sconosciuto, l'oggetto ha lasciato la collezione, la generazione dell'indirizzo è avanzata, oppure l'impronta dell'oggetto (VMT + identità di definizione/modello) è cambiata perché l'indirizzo è stato riutilizzato. Punto cieco residuo: stessa classe e stesso modello ricreati allo stesso indirizzo tra due letture dell'elenco. | Dettaglio runtime | Eseguire di nuovo `road-vehicles.list`/`humans.list` e usare il nuovo handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; fallback di `D3DRuntimeApi` | Wrapper generico per gli errori lato plugin; `Values["detail"]` contiene il messaggio (spesso un codice più specifico) e `Values["exception"]` il tipo di eccezione. Anche un'eccezione che sfugge a un'operazione all'interno del dispatcher della mailbox riceve una risposta con questo codice (senza valori), invece di lasciare la richiesta senza risposta. | Risultato runtime | Leggere `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | Il plugin non ha un'implementazione per un'operazione consentita dal registro (disallineamento di versione tra registro e plugin). | Dettaglio runtime | Reinstallare un pacchetto coerente. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | L'operazione non è in `PublicRuntimeOperationIds`, incluse tutte le operazioni `internal.*`. Verificato prima della ricerca della sessione. | Risultato runtime; risposta di controllo; CLI: uscita 2 | Usare un id di operazione pubblico. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` con `preset` mentre non esiste un veicolo del giocatore (le sessioni headless non ne hanno). Viene riportato anche come `code` dell'evento `camera.lock.degraded` quando la riapplicazione fallisce. | Dettaglio runtime / evento runtime | Bloccare senza preset, oppure usare una sessione con un veicolo del giocatore. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Un risultato D3D riuscito non aveva valori, oppure conteneva una stringa di stato del dispositivo sconosciuta. | Lanciato (`OmsiRuntimeException`) | Allineare le versioni di host e plugin. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | Lo slot contiene una risposta obsoleta con lo stesso id di richiesta della nuova richiesta. La risposta obsoleta viene eliminata prima che l'errore venga sollevato. | Lanciato (`InvalidOperationException`) | Usare id di richiesta strettamente crescenti. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Nessuna risposta entro `timeout`; lo slot viene reimpostato e una risposta tardiva viene scartata. | Lanciato (`TimeoutException`); CLI: uscita 5 | Riprovare con un timeout più lungo; verificare che OMSI non sia bloccato (finestra di dialogo modale, caricamento). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | L'envelope della risposta è corrotto, ha una lunghezza non valida (negativa, zero o maggiore dello slot), un id di sessione estraneo o un id di richiesta diverso. Lo slot viene reimpostato prima che l'errore venga sollevato, quindi la richiesta successiva funziona normalmente. | Lanciato (`InvalidDataException`) | Riprovare; segnalare il problema se persiste. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | Il `cursor` di un'operazione di elenco è malformato oppure appartiene a un altro tipo di elenco. | Dettaglio runtime | Passare il `next_cursor` della pagina precedente della stessa operazione, oppure ometterlo. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | Il risultato serializzato supera la mailbox da 64 KiB. I risultati degli elenchi paginati vengono invece accorciati per rientrare nel limite e proseguono con `next_cursor`; nessun risultato di elenco raggiunge questo codice. | Risultato runtime | Usare un'operazione più mirata o un `limit` più piccolo. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | Il puntatore alla definizione o allo stato dello script del veicolo è nullo. | Dettaglio runtime | Il veicolo non ha oggetti script. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; mailbox del plugin | `RuntimeCommand.SessionId` differisce dall'id di sessione dell'handle (lanciato dall'host), oppure una richiesta ha raggiunto un plugin associato a un'altra sessione (restituito dal plugin come risultato tipizzato). | Lanciato (`InvalidOperationException`) / risultato runtime | Costruire il comando con `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` dell'evento `world.failed`) | Il meteo di avvio non ha potuto essere applicato dopo l'avvio del mondo: OMSI non ha alcuna stazione ICAO configurata per `/weather-real`, oppure i suoi oggetti meteo non sono disponibili. La sessione fallisce. | Fallimento della sessione | Configurare una stazione in OMSI oppure usare `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Una chiave di `weather.set` indica un valore che OMSI deriva a ogni tick meteo (umidità, fattori di luce, trasparenza delle nuvole, flag di precipitazione, terreno bagnato); non viene scritto nulla. | Risultato runtime | Impostare la chiave sorgente indicata in `detail` (per esempio `dew_point` invece di `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Chiave di `weather.set` sconosciuta; non viene scritto nulla. | Risultato runtime | Usare una chiave elencata in [capability](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` non è nella tabella delle variabili stringa. | Dettaglio runtime | Elencare prima le variabili stringa. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Un booleano meteo non è `true`/`false`/`1`/`0`. Attualmente irraggiungibile (vedere sopra). | Dettaglio runtime (definito) | Vedere il sorgente. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` non intero, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` può essere -1), `scheduled` 0..1; `vehicle.variable.set`: `value` non finito; `vehicle.curve.evaluate`: `x` non finito. | Dettaglio runtime | Usare un valore compreso nell'intervallo. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` non è nella tabella delle variabili numeriche. | Dettaglio runtime | Elencare prima le variabili. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | Lo slot della variabile o l'indirizzo del valore è nullo. | Dettaglio runtime | La variabile non è materializzata per questo veicolo. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Gli scalari dell'orologio sono stati scritti, ma la chiamata nativa profilata SetTime ha restituito un errore. | Dettaglio runtime | Riprovare; rileggere con `time.read`. |

## RuntimeD3D

Tutti sollevati da `CurrentRuntimeControl` (mappatura `ThrowD3D` dello stato nativo; `detail` contiene l'operazione e l'HRESULT, `native_status` lo stato numerico). Superficie: risultato runtime con il codice in `ErrorCode`; `D3DRuntimeApi` lo rilancia come `OmsiRuntimeException`.

| Codice | Stato nativo / causa | Cosa fare |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: il dispositivo Direct3D è perso. | Attendere `d3d.restored`; ricreare le texture (la generazione è cambiata). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: `width`, `height`, `level`, `x`, `y`, `format` o `handle` mancante o non valido (intervalli: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Correggere gli argomenti. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` non è Base64 valido oppure supera 48 KiB. | Inviare rettangoli più piccoli. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, oppure un nome di `format` sconosciuto (validi: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Usare un formato elencato. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Qualsiasi altro stato; l'HRESULT è in `detail`. | Segnalare il problema indicando l'HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: il dispositivo non è pronto (prima del primo frame o durante l'arresto). | Riprovare dopo `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: è in corso un reset del dispositivo. | Riprovare dopo `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: l'handle della texture è già stato rilasciato. | Non riutilizzare handle rilasciati. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: l'handle appartiene a una generazione precedente del dispositivo; oppure la stringa dell'handle non è `d3dtex-<session>-<hex>` / è zero. | Ricreare la texture. |

<a id="session"></a>
## Sessione

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` su `plugin.request.unsupported` | Piano: richiesti `LastMapState`, `EntrypointIdentity`, modalità data/ora/anno, modalità meteo, campi del veicolo del giocatore o documenti di input (`Requested capability unavailable: <name>`). Telemetria: il plugin ha rifiutato l'handoff (non può accadere per un piano eseguibile). | Diagnostica del piano; diagnostica della sessione | Rimuovere la richiesta non supportata ([limitazioni note](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` su `headless.arm.failed` | Il plugin non è riuscito ad armare in OMSI l'hook monouso di avvio headless. | Diagnostica della sessione (`Failed`) | Verificare il build; segnalare il problema. |
| `OL_E_NO_ACTIVE_SESSION` | Client CLI (`ReportForwarded`, `CliEventWatch`) | Nessun owner risponde sulla pipe di controllo dell'installazione (nessuna sessione, oppure l'owner è ancora in fase di avvio/validazione). | Envelope della CLI, uscita 4 | Avviare una sessione, oppure attendere che sia `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` è trascorso prima di `plugin.started` (OMSI non ha caricato `plugins\OmsiLaunch.Plugin.opl`, oppure è bloccato prima dell'inizializzazione dei plugin). | Diagnostica della sessione (`Failed`) | Controllare la chiusura del plugin, `plugins\OmsiLaunch.Plugin.opl` e il `logfile.txt` di OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` su `plugin.handoff.invalid` o su JSON di telemetria non interpretabile | Il plugin non è riuscito a leggere/verificare l'handoff di avvio (versioni da 3 a 6, SHA-256), oppure ha inviato telemetria non valida. | Diagnostica della sessione (`Failed`) | Allineare le versioni di host e plugin (reinstallare il pacchetto). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Un owner risponde già a `session.status` per questa installazione. | Envelope della CLI, uscita 7 | Usare i comandi client (`session status`, `session stop`, comandi runtime). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | Lo stato della sessione non è `Running`. | Lanciato (`InvalidOperationException`) | Eseguire prima `WaitForAsync(session, SessionState.Running, ...)`. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Non è stato possibile costruire il piano splash/ITX; il messaggio contiene il codice di presentazione. | Diagnostica del piano | Vedere [Presentazione](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (finestra di dialogo di OmsiLaunchW) | Codice di fallback mostrato quando un piano di avvio non è eseguibile o la sessione non ha raggiunto il gameplay, e non esiste alcuna diagnostica `OL_E_`. | Solo finestra di messaggio | Leggere `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` su `world.situation.failed` | L'avvio nativo della situazione salvata ha restituito un errore (`native_status` nell'evento). | Diagnostica della sessione (`Failed`) | Controllare il file `.osn` e la sua mappa. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | Il plugin è stato avviato, ma `Running` non è stato raggiunto entro `StartupTimeoutSeconds`. | Diagnostica della sessione (`Failed`) | Aumentare `/startup-timeout` per le mappe grandi; esaminare `RuntimeEvents` per l'ultimo evento del mondo. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Qualsiasi eccezione nel percorso di avvio; il messaggio è quello interno (che di solito inizia con il codice interno). | Diagnostica della sessione (`Failed`) | Intervenire in base al codice interno. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` su `world.failed` | L'avvio nativo NEW_MAP ha restituito un errore (`native_status` nell'evento). | Diagnostica della sessione (`Failed`) | Controllare la mappa, l'indice del punto di ingresso e i log di OMSI. |

## SessionProfile

Tutti sollevati da `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) o da `CliInput`, lanciati come `SessionProfileException` (una `IOException` con `Code`), CLI: uscita 2. Vedere [profili di sessione](session-profiles.md).

| Codice | Significato / causa tipica | Cosa fare |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | La directory `assets` della splash screen del preset o il file `profile` delle Internet Textures (texture da Internet) non esiste all'interno del pacchetto. | Aggiungere l'asset. |
| `OL_E_SESSION_PROFILE_INVALID` | Violazione strutturale o di un limite: più di 256 KiB, non esattamente un mapping radice, ancore YAML, chiave sconosciuta, chiave obbligatoria mancante, valore non scalare dove è richiesto uno scalare, `id` diverso dal nome della directory, preset non compresi tra 1..99 o `index` duplicato, timeout non positivi, modalità meteo/splash/Internet Textures non supportata, data/ora non `explicit`, YAML non valido, errori di interpretazione di numeri/date. | Correggere il YAML in base al messaggio. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` non è vuoto e non contiene la mappa selezionata (NEW_MAP) o la mappa della situazione selezionata (SAVED_SITUATION). | Scegliere un mondo compatibile. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` non esiste. | Verificare l'id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Un argomento esplicito della CLI riguarda un campo gestito dal profilo/preset selezionato. | Eliminare il flag o scegliere un altro preset. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | L'id contiene `\`, `/`, `:` o `..`; oppure il percorso di un asset è assoluto, esce dal pacchetto o attraversa una junction/un collegamento simbolico. | Mantenere i percorsi all'interno del pacchetto. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` mancante, al di fuori di 1..99, oppure non dichiarato dal profilo. | Usare un indice di preset dichiarato. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` non è `omsilaunch.session-profile/v1`. | Usare lo schema supportato. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | La chiave di un'impostazione del preset è nota ma non scrivibile. | Rimuovere la chiave. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | La chiave di un'impostazione del preset non è nel catalogo. | Usare una chiave del catalogo. |

<a id="transaction"></a>
## Transazione

Vedere [transazioni e recupero](../concepts/transactions-and-recovery.md).

| Codice | Sollevato da | Significato / causa tipica | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Un `closecheck` obsoleto esiste ancora dopo `File.Delete`. | Diagnostica della sessione tramite `OL_E_START_SESSION` | Rimuovere manualmente `<root>\closecheck` (permessi). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Un percorso che non esisteva prima della sessione ora ha un contenuto diverso da quello applicato dalla sessione; non viene rimosso e il journal viene conservato. | Lanciato (`IOException`); all'interno di `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; CLI: uscita 8 | Esaminare il file; rimuoverlo o spostarlo, quindi eseguire `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (journal precedente alle impronte) | Il journal non ha un'impronta del contenuto applicato per un percorso originariamente assente e non di eliminazione, quindi la proprietà non può essere dimostrata. All'avvio della sessione il recupero viene rinviato e ritentato con i byte pianificati di questa sessione; tramite `RecoverPendingAsync` viene lanciato. | Lanciato (`IOException`); CLI: uscita 8 | Avviare una sessione con la stessa spec (che fornisce i byte), oppure esaminare e rimuovere il file, quindi eseguire `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | Lo SHA-256 di un backup non corrisponde allo snapshot registrato nel journal; non viene scritto nulla. | Lanciato; all'interno di `OL_E_RESTORE_FAILED`; CLI: uscita 8 | Ripristinare il file da un proprio backup; eliminare poi il journal solo quando si è certi. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Gli snapshot esistono in memoria ma `journal.json` non c'è più (eliminato durante la sessione). | Lanciato; all'interno di `OL_E_RESTORE_FAILED` | Verificare manualmente i file della sessione. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` esiste ancora dopo l'eliminazione (il ripristino in sé è riuscito ed è stato verificato). | Lanciato; all'interno di `OL_E_RESTORE_FAILED`; CLI: uscita 8 | Eliminare `<root>\.omsilaunch\journal.json` (permessi) oppure eseguire di nuovo `/recover` (idempotente). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (errore di avvio / supervisore) | Non è stato possibile confermare l'uscita di OMSI, quindi i file non sono stati sostituiti mentre OMSI potrebbe ancora usarli; il journal viene conservato. | Diagnostica della sessione (`Failed`) | Dopo che `Omsi.exe` è terminato, eseguire `/recover` (oppure l'avvio successivo esegue il recupero automaticamente). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (errore di avvio / supervisore) | `RestoreAsync` ha lanciato un'eccezione; il messaggio contiene il codice interno; il journal viene conservato. | Diagnostica della sessione (`Failed`); CLI: uscita 8 quando lanciato da `/recover` | Intervenire in base al codice interno, quindi eseguire `/recover`. |

<a id="ui"></a>
## Interfaccia utente

Codici delle operazioni del menu di pausa nativo (`ui.pause.*`).

| Code | Emitted by | Meaning | Channel | Action |
| --- | --- | --- | --- | --- |
| `OL_E_UI_NOT_PAUSED` | `CurrentRuntimeControl` | L'operazione richiede OMSI in pausa con il menu di pausa nativo visibile (take-over, action), e non lo è. | Risultato runtime | Attendere che l'utente metta OMSI in pausa. |
| `OL_E_UI_BUSY` | `CurrentRuntimeControl` | Un'azione del menu di pausa è pianificata o in esecuzione (può trovarsi in una finestra di dialogo modale nativa), oppure la chiamata è arrivata dentro il frame di OMSI. | Risultato runtime | Interrogare `ui.pause.state` finché `busy` non è `false`, poi riprovare. |
| `OL_E_UI_ALREADY_TAKEN_OVER` | `CurrentRuntimeControl` | `ui.pause.take-over` mentre il menu di pausa nativo è già stato rilevato (taken over). | Risultato runtime | Usare `ui.pause.state`, oppure prima `ui.pause.restore-native`. |

<a id="warning"></a>
## Avviso

| Codice | Sollevato da | Significato | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | L'eseguibile di OMSI 2 non ha alcun layout noto (la sua identità del codice non è nel catalogo dei build). Il piano resta eseguibile: ogni capability nativa viene risolta e verificata nel processo, e una che non può essere verificata non è disponibile. | Diagnostica del piano (l'eseguibilità non cambia) | Nessuna azione necessaria; controllare `/plan` e `profiles` per le capability previste. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (supervisore) | È stato richiesto `Behavior.CooperativeShutdown`, ma non è stato possibile chiedere a OMSI di uscire (la sua routine di uscita non è risolta su questo build, oppure OMSI si sta già chiudendo), oppure OMSI non è uscito entro `ShutdownTimeoutSeconds`: è stato invece terminato forzatamente. Il ripristino viene eseguito come di consueto. | Diagnostica della sessione | Nessuna azione: la sessione termina come con una terminazione forzata. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Una capability necessaria all'avvio non ha potuto essere risolta dal file eseguibile (per esempio un eseguibile protetto da DRM la cui immagine caricata differisce dal file). Viene risolta nel processo all'avvio. | Diagnostica del piano (l'eseguibilità non cambia) | Nessuna azione necessaria; un avvio fallisce con `OL_E_CAPABILITY_UNRESOLVED` se non può essere risolta nemmeno nel processo. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Un modulo entrato nella sessione da solo (file di impostazioni, contenuto o attivazione predefinita) non può essere caricato (`Data["code"]` ne indica il motivo, `Data["activation"]` il modo in cui è entrato). | Diagnostica del piano (l'eseguibilità non cambia) | Correggere, disabilitare o rimuovere il modulo. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Una voce di `.omsilaunch\modules.ini` non può essere usata (non è una sezione di modulo, è fuori da una sezione, chiave sconosciuta, valore non valido; si usa il valore predefinito), oppure sono state richieste impostazioni per un modulo che la sessione non carica. | Diagnostica del piano (l'eseguibilità non cambia) | Correggere la voce o la richiesta. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Un percorso di eliminazione della sessione (destinazione ITX, `Texture\standard.ipr`, `closecheck`) non esisteva prima della sessione ed esiste ora, ma nessun processo OMSI è mai stato avviato con questo journal, quindi il file non può essere un sottoprodotto della sessione. Viene conservato e segnalato (messaggio = percorso relativo, `Data["sha256"]`); la transazione viene comunque completata. | Diagnostica della sessione / `RecoveryStatus.Diagnostics` (lo stato non viene influenzato) | Esaminare il file; rimuoverlo manualmente se non desiderato. |

<a id="module"></a>
## Modulo

Moduli runtime (vedere [moduli runtime](runtime-modules.md)). In fase di pianificazione un codice è una diagnostica del piano: per un modulo richiesto rende il piano non eseguibile; per un modulo entrato da solo (file di impostazioni, contenuto, predefinito) il piano riporta invece `OL_W_MODULE_NOT_LOADED`. In una sessione un codice è una diagnostica di sessione (la sessione prosegue, a meno che il codice sia `OL_E_MODULE_SESSION_FAILED`) e, per un'operazione di modulo, è il risultato runtime.

| Codice | Sollevato da | Significato | Superficie | Cosa fare |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` manca, non è un `omsilaunch.runtime-module/v1` valido, oppure il suo `id` differisce dal nome della sua directory. Il messaggio indica il campo. | Diagnostica del piano; `ListRuntimeModulesAsync` | Correggere il manifest. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | L'id di un modulo richiesto non è installato sotto `plugins\OmsiLaunch.Modules`. | Diagnostica del piano | Controllare `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Un file fissato manca, differisce dal suo SHA-256 fissato oppure viene raggiunto attraverso un reparse point; oppure, nel processo, `module.json` è cambiato dopo la pianificazione. | Diagnostica del piano; diagnostica della sessione | Reinstallare il modulo. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | L'intervallo `bridge_protocol` del modulo non include nessuno dei protocolli serviti da questo build (1 e 2). | Diagnostica del piano | Usare una versione del modulo realizzata per questo OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Una capability richiesta è sconosciuta a questo build, non disponibile su questa installazione oppure non risolta nel processo. | Diagnostica del piano; diagnostica della sessione | Vedere [capability](capabilities.md) e `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | La sessione non esegue la mappa associata al modulo, oppure un file associato differisce dal suo SHA-256 fissato. | Diagnostica del piano; diagnostica della sessione | Avviare la mappa associata con il contenuto associato. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | Il modulo ha chiesto di avviare un file che non è uno dei suoi componenti ausiliari. | Risultato runtime all'interno del modulo | Difetto del modulo. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | Il componente gestito non ha potuto essere caricato o il suo tipo di ingresso creato; oppure il programma di sessione del modulo non ha potuto essere avviato o è terminato prima di essere pronto. | Diagnostica della sessione | Controllare il build del modulo (compatibile x86, il tipo di ingresso implementa `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` o `Stop` ha generato un'eccezione. | Diagnostica della sessione | Difetto del modulo; si è applicata la sua `failure_policy`. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Una fase o un'operazione è tornata oltre il proprio budget (`timeouts`, `timeout_ms`), oppure un programma di sessione non è stato pronto entro `prepare_ms`. | Diagnostica della sessione; risultato runtime | Difetto del modulo; si è applicata la sua `failure_policy`. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | Il modulo è stato disabilitato o è fallito in precedenza nella sessione. | Risultato runtime | Vedere le diagnostiche della sessione per il primo errore. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | La sessione non ha caricato questo modulo, oppure non è ancora pronto (prima del gameplay). | Risultato runtime | Richiedere il modulo all'avvio; attendere `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | Il modulo non dichiara tale operazione. | Risultato runtime | Controllare le `operations` del modulo. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | L'operazione è fallita senza un codice di catalogo; il codice proprio di un modulo è in `Values["module_code"]`. | Risultato runtime | Specifico del modulo. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Dopo l'arresto o un errore una risorsa tracciata era ancora attiva (un processo ausiliario che non è terminato, un thread in background che non è finito) oppure il suo rilascio ha generato un'eccezione. | Diagnostica della sessione | Segnalare il modulo; il processo termina comunque con OMSI. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Un modulo la cui `failure_policy` è `fail-session` è fallito; la sessione viene arrestata e ripristinata. | Diagnostica della sessione (stato `Failed`) | Vedere il codice di modulo precedente. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Un'impostazione di modulo richiesta non è valida o non è dichiarata dal modulo, un modulo è sia abilitato sia disabilitato, il nome di un'impostazione non è `<module-id>.<key>`, oppure un flag di modulo è dichiarato da più moduli. | Diagnostica del piano (non eseguibile); errore della CLI | Correggere la richiesta; `/list:RuntimeModules` mostra le impostazioni di ciascun modulo. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Un plugin di OMSI che il modulo elenca sotto `conflicts.plugins` è attivato (`plugins\*.opl`) o caricato in OMSI. Il motivo indica il file `.opl`. | Diagnostica del piano; diagnostica della sessione | Disattivare o rimuovere quel plugin. OmsiLaunch non lo modifica mai. |
| `OL_E_MODULE_OPERATION_NOT_DECLARED` | `ModuleHost` | Un modulo ha chiesto di eseguire in-process un'operazione del catalogo (`IRuntimeModuleContext.Runtime`, `run_operation` nativo) che il suo manifest non elenca in `runtime_operations`. La chiamata non raggiunge mai OMSI. | Risultato runtime | Elencare l'operazione in `runtime_operations` (richiede `bridge.runtime` e `bridge_protocol.min` 3). |
| `OL_E_MODULE_CONTENT_INPUT_UNDECLARED` | `ModuleHost` | Un modulo ha chiesto di individuare contenuti della community (`IRuntimeModuleContext.Content`) di un tipo che il suo manifest non dichiara in `content_inputs`. | Risultato di contesto | Dichiarare il tipo in `content_inputs`. |
| `OL_E_MODULE_CONTENT_INPUT_INVALID` | `RealmapPackage` | Il pacchetto `openomsi-realmap` della mappa non può essere letto in sicurezza: è raggiunto tramite un reparse point, ne contiene uno, ha più di 50,000 file o supera 4 GiB, oppure non può essere letto. | Risultato di contesto | Riparare o rigenerare il pacchetto; il modulo degrada per risorsa. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | riservato | Riservato; un componente nativo riceve invece i risultati numerici degli hook descritti in [moduli runtime](runtime-modules.md#native-components-and-hooks). | nessuna | nessuna |

<a id="non-error-diagnostic-codes"></a>
## Codici di diagnostica che non sono errori

| Codice | Emesso da | Messaggio / dati | Significato |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | messaggio = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` è stato creato e la sua identità è stata registrata. Diagnostica della sessione. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | messaggio = SHA-256 del file rimosso | Un `closecheck` esistente prima della sessione è stato rimosso definitivamente (`SuppressStaleClosecheckWarning = true`). Diagnostica della sessione. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | messaggio = percorso relativo; `Data["sha256"]` | Un percorso di eliminazione della sessione è stato ricreato da OMSI durante una sessione il cui processo era stato avviato; è stato rimosso per ripristinarne l'assenza originale. Diagnostica della sessione / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | messaggio = `manifest` o `self` | Indica quale riferimento usa la validazione del plugin permanente. Diagnostica del piano. |
| `module.planned` | `SessionPlanner` | messaggio = id del modulo; `Data["module"]`, `Data["status"]`, `Data["activation"]` | Il modulo è nel piano. Diagnostica del piano. |
| `module.disabled` | `SessionPlanner` | messaggio = id del modulo | Un modulo di contenuto o predefinito è stato tenuto fuori da una disabilitazione (richiesta o file di impostazioni). Diagnostica del piano. |
| `session_profile.selected` | `SessionPlanner` | messaggio = id del profilo; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Provenienza di una sessione compilata da un profilo di sessione. Diagnostica del piano. |

I nomi degli eventi runtime (`RuntimeEvent.Type`, che non sono voci di diagnostica) sono elencati nel [ciclo di vita della sessione](../concepts/session-lifecycle.md#telemetry-events).
