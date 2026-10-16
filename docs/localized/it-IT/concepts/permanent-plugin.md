# Il plugin permanente

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Traduzione della [pagina originale in inglese](../../../concepts/permanent-plugin.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

OmsiLaunch controlla OMSI dall'interno del processo OMSI tramite un plugin che viene installato una sola volta, in `plugins\OmsiLaunch.*`, come parte del prodotto. Non viene mai preparato, copiato, sottoposto a snapshot, ripristinato o rimosso da una sessione. Questa pagina spiega che cosa contiene la chiusura del plugin, come OMSI la carica, come l'host la valida prima di ogni avvio, come comunicano host e plugin (handoff, telemetria, mailbox runtime) e che cosa fa il plugin quando OMSI viene avviato senza OmsiLaunch. Fonti: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, i tre store in memoria condivisa), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` e `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## La chiusura (9 file)

| File in `plugins\` | Ruolo |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Descrittore del plugin OMSI. Il suo contenuto è `[dll]` seguito da `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Shim nativo x86 di esportazione generato da DNNE 2.0.6. Esporta l'ABI dei plugin di OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) e ospita il runtime .NET. Contiene la risorsa della versione del prodotto. |
| `OmsiLaunch.Plugin.dll` | Plugin gestito (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Manifest delle dipendenze .NET del plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Configurazione del runtime .NET (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Formati di trasmissione e record pubblici condivisi con l'host. |
| `OmsiLaunch.Builds.Omsi23004.dll` | Il profilo di build: impronta dell'eseguibile, variabili globali, layout degli oggetti, indirizzi dei metodi. |
| `OmsiLaunch.Interop.dll` | Lettori e scrittori di memoria interni al processo costruiti sul profilo. |
| `OmsiLaunch.Native.x86.dll` | Bridge nativo (C++): validazione del build, hook di avvio headless, applicazione dell'ora, MakeVehicle, PlaceRandomBus, soppressione delle texture da Internet, accesso al dispositivo D3D9. |

`RuntimeArtifactSet.Load` ricava questo elenco dalla directory `plugins\` del controller stesso: i quattro file con nome fisso (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), ogni `OmsiLaunch.*.dll` in quella directory tranne `OmsiLaunch.PluginNE.dll`, e il bridge nativo. `OmsiLaunch.Plugin.dll` deve essere tra questi. DLL arbitrarie non vengono mai trascinate dentro OMSI. Il creatore del pacchetto di rilascio (`tools/New-ReleasePackage.ps1`) scrive esattamente i nove file sopra elencati.

<a id="how-omsi-loads-it"></a>
## Come OMSI lo carica

1. OMSI enumera `plugins\*.opl` e carica la DLL indicata in `OmsiLaunch.Plugin.opl`: `OmsiLaunch.PluginNE.dll`.
2. Lo shim DNNE avvia all'interno del processo OMSI il runtime .NET 6 x86 descritto da `OmsiLaunch.Plugin.runtimeconfig.json` e risolve le esportazioni gestite in `OmsiLaunch.Plugin.dll`.
3. OMSI chiama `PluginStart`. OMSI può chiamarlo più di una volta durante l'avvio; viene onorata solo la prima chiamata (protezione `Interlocked.Exchange`), perché un avvio riuscito possiede un hook nativo monouso. Le chiamate successive restituiscono immediatamente.
4. `PluginStart` controlla per prima cosa `OMSILAUNCH_INTERNET_TEXTURES_MODE`: quando è `Disabled`, viene applicata la soppressione del downloader nativo (`internet-textures.suppressed`, oppure `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` legge l'handoff (vedere sotto), valida il build all'interno del processo, arma l'hook di avvio headless, apre la mailbox runtime e pianifica l'avvio del mondo sul thread dell'interfaccia utente di OMSI tramite un callback `SetTimer`. Nessun comando runtime viene mai eseguito su un thread worker IPC; tutto viene eseguito nel callback del timer, sul thread originale dell'interfaccia utente di OMSI.
6. `PluginFinalize` elimina il timer, arresta il runtime (`NativeD3DShutdown`) e ripristina la patch delle texture da Internet.

Le esportazioni `AccessVariable`, `AccessTrigger`, `AccessStringVariable` e `AccessSystemVariable` sono vuote; OmsiLaunch non usa il canale dei plugin di OMSI per le variabili di script.

<a id="integrity-validation-before-every-start"></a>
## Validazione dell'integrità prima di ogni avvio

`RuntimeArtifactSet.ValidateInstalled` viene eseguito durante `StartSessionAsync` (dopo il recupero anticipato, prima della preparazione della transazione) e durante `PlanSessionAsync` (solo presenza, tramite `LoadArtifacts`). La voce di diagnostica del piano `plugin.integrity.reference` indica quale riferimento è stato usato.

| Situazione | Riferimento | Controllo per file | Errori |
| --- | --- | --- | --- |
| `release-manifest.json` presente accanto a `OmsiLaunch.exe` (pacchetto installato) | `manifest` | Il file installato deve esistere e il suo SHA-256 deve essere uguale alla voce del manifest per `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (il manifest non ha una voce per un file richiesto), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Nessun manifest (layout di sviluppo) | `self` | Presenza più coerenza interna: il file installato deve avere lo stesso hash del file nella directory `plugins\` del controller stesso | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifest illeggibile o malformato (array `files` mancante, voce senza `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Chiusura incompleta nella directory del controller | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (piano non eseguibile); la CLI segnala inoltre `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` quando `plugins\OmsiLaunch.Plugin.opl` o `OmsiLaunch.Native.x86.dll` è assente |

Vengono utilizzate solo le voci `plugins/` del manifest; il manifest è costituito da dati, mai da criteri. Il manifest viene generato dal creatore del pacchetto di rilascio con lo SHA-256 di ogni file preparato. Quando il controller viene eseguito dalla radice di OMSI (il layout di rilascio), origine e destinazione sono la stessa directory; senza un manifest il controllo si riduce quindi a presenza e coerenza interna, ed è per questo che i pacchetti rilasciati contengono sempre `release-manifest.json`. Soluzione in caso di discrepanza: reinstallare il pacchetto in modo che `plugins\` e `release-manifest.json` concordino.

Prima di applicare qualsiasi patch, il plugin risolve e verifica ogni target nativo del catalogo del build rispetto all'immagine caricata e consegna la tabella verificata a `OmsiLaunch.Native.x86.dll`, che non contiene alcun indirizzo di OMSI (telemetria `native.capabilities`). Un avvio in cui la capability `boot.headless-start` o quella della modalità mondo non è disponibile fallisce con `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); un form Start attivo in uno stato inatteso fallisce con `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Vedere [compatibilità](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Dall'host al plugin: variabili d'ambiente

`CreateProcessW` avvia `Omsi.exe` con l'ambiente del processo padre più:

| Variabile | Valore | Consumatore |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID della sessione (formato `D`) | `CurrentRuntimeControl` lo usa per contrassegnare gli handle D3D (formato `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` apre questo mapping in sola lettura |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` o `Override` | `PluginStart` (solo `Disabled` ha un effetto all'interno del processo) |

I tre mapping vengono creati dall'host prima dell'avvio del processo (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) e rilasciati quando termina il task del ciclo di vita della sessione. Sono oggetti kernel con nome con la DACL predefinita dell'utente che esegue l'avvio; qualsiasi processo dello stesso utente può aprirli (modello di fiducia accettato per lo stesso utente, vedere [limitazioni note](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## L'handoff di avvio

Un record mappato in memoria, di sola lettura, a layout fisso (`StartupHandoffWire`, magic `OLSH`, versione 4; la versione 3 è ancora accettata dal lettore). L'intestazione di 64 byte contiene il magic, la versione, la dimensione dell'intestazione, la dimensione totale, il GUID della sessione, la dimensione del payload e lo SHA-256 del payload. Il payload contiene `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 con prefisso di lunghezza), `PresentedEntrypointIndex`, `WorldMode`, i flag (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` e `TimeMode`. Il plugin ricalcola l'hash del payload e rifiuta qualsiasi discrepanza (`plugin.handoff.invalid`, errore dell'host `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Un mapping più grande di 1 MiB o con una dimensione incoerente viene rifiutato allo stesso modo.

Il plugin accetta un handoff solo quando `WorldMode` è `NewMap` o `SavedSituation`, `HeadlessStart` è impostato, `PlayerVehicleEnabled` non è impostato, le modalità di data e ora sono entrambe `Unset` e una situazione salvata indica il proprio `.osn`. Qualsiasi altro caso produce `plugin.request.unsupported` (errore dell'host `OL_E_CAPABILITY_UNAVAILABLE`). L'host imposta sempre `HeadlessStart`.

<a id="telemetry-slot"></a>
## Slot di telemetria

`OmsiLaunch.Telemetry.<session>` è un event ring (Beta 4, `TelemetryRingWire`): un'intestazione di 64 byte (magic `OLTR`, versione 1, numero di slot, dimensione dello slot, sequenza confermata) e 64 slot numerati di 2048 byte, ciascuno contenente un evento JSON UTF-8 `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` è il clock di sessione monotono del plugin). Il plugin è l'unico scrittore: invalida uno slot, lo scrive, conferma la sequenza dello slot e infine pubblica la sequenza del ring. L'host legge, in ordine e ogni 100 ms, ogni evento confermato dall'ultima lettura; uno slot sovrascritto durante la copia viene conteggiato come perso e non viene mai consegnato incoerente. Un host che resta indietro di più di 64 eventi riceve l'evento runtime `telemetry.lost` con il conteggio esatto. Un evento più grande di uno slot viene sostituito da `telemetry.oversized` che lo nomina. Gli eventi vengono aggiunti a `SessionStatus.RuntimeEvents` (limitato agli ultimi 256) e guidano il ciclo di vita semantico (`plugin.started`, `world.starting`, `gameplay.entered`, eventi dei moduli e degli errori).

<a id="runtime-command-mailbox"></a>
## Mailbox dei comandi runtime

`OmsiLaunch.Runtime.<session>` è una mailbox a singola richiesta in volo da 64 KiB: `state` (int32 all'offset 0: 0 inattivo, 1 richiesta inviata, 2 risposta inviata), `length` (int32 all'offset 4), envelope all'offset 8. Gli envelope sono record `RuntimeCommandWire` (magic `OLRC`, versione 1, intestazione di 72 byte con tipo, lunghezza totale, GUID della sessione, id della richiesta, lunghezza del payload e SHA-256 del payload JSON UTF-8). Il plugin esegue il polling della mailbox dal timer del thread dell'interfaccia utente (50 ms una volta caricato il mondo), esegue il comando su quel thread e pubblica la risposta solo se lo slot contiene ancora lo stesso id di richiesta; una richiesta abbandonata dall'host per timeout non riceve mai risposta. Una risposta sovradimensionata viene sostituita da un errore tipizzato `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Dettagli e timeout sono in [controllo runtime](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Criterio di ricerca delle DLL

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` e gli assembly della CLI dichiarano `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Le importazioni native (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) vengono risolte solo dalla directory dell'assembly stesso (`plugins\`) o dalla directory di sistema di Windows; la radice di OMSI e `PATH` non vengono mai esaminati. `OmsiLaunch.Native.x86.dll` viene quindi caricato da `plugins\` e da nessun altro percorso.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Quando OMSI viene avviato senza OmsiLaunch

Poiché la chiusura è permanente, OMSI carica `OmsiLaunch.PluginNE.dll` a ogni avvio, compresi gli avvii da Steam o dal desktop. In tal caso:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` è assente, quindi non viene applicata alcuna patch al downloader.
- `OMSILAUNCH_HANDOFF_NAME` è assente, quindi `PluginRuntime.Start` emette `plugin.handoff.invalid` e restituisce `false`. `CurrentTelemetrySink.Emit` restituisce immediatamente quando `OMSILAUNCH_TELEMETRY_NAME` non è impostato, quindi non viene scritto nulla da nessuna parte.
- Nessuna validazione del build, nessun hook nativo, nessuna mailbox, nessun timer. Il plugin rimane caricato ma inerte; OMSI si comporta come se il plugin non ci fosse.
- `PluginFinalize` all'uscita di OMSI chiama la routine nativa di ripristino e `NativeD3DShutdown`, entrambe operazioni nulle quando non è stato installato nulla.

Il file `.opl` non deve quindi essere rimosso per eseguire OMSI normalmente.

<a id="non-interference-with-third-party-plugins"></a>
## Nessuna interferenza con i plugin di terze parti

Le sessioni non enumerano, non calcolano hash, non copiano, non rimuovono e non ripristinano mai altri file in `plugins\`. Il plugin non usa il canale `AccessVariable` di OMSI e non tocca lo stato di altri plugin. Le uniche patch interne al processo sono l'hook di avvio headless profilato (un reindirizzamento VMT monouso armato per la sessione), la soppressione facoltativa delle texture da Internet (ripristinata in `PluginFinalize`) e l'intercettazione di `Reset` del dispositivo D3D9 usata per tracciare il ciclo di vita delle texture.

<a id="difference-from-omsihook"></a>
## Differenza rispetto a OmsiHook

OmsiLaunch **non ha alcuna dipendenza runtime** da OmsiHook né da alcun binario di OmsiHook: gli unici pacchetti referenziati sono `DNNE` 2.0.6 e `YamlDotNet` 15.1.2, e in nessun punto del prodotto esistono `using OmsiHook` o P/Invoke verso DLL di OmsiHook. Ciò che OmsiLaunch condivide con OmsiHook è conoscenza derivata: i layout degli oggetti e diversi wrapper di lettura sono stati riconciliati a partire dal checkout fissato di OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) con l'eseguibile esatto `Omsi23004_692EBFBF`. L'attribuzione e i termini di licenza sono in `THIRD-PARTY-NOTICES.md` e la matrice di riutilizzo per file in `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook si inietta da un processo separato ed espone puntatori grezzi; OmsiLaunch viene eseguito all'interno del processo, espone solo handle opachi con ambito di sessione e rimuove qualsiasi indirizzo nativo dai risultati pubblici (vedere [capability](../reference/capabilities.md)).
