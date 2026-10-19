# Moduli runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> Traduzione della [pagina originale in inglese](../../../reference/runtime-modules.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Un modulo runtime aggiunge comportamento a una sessione OMSI gestita: una funzionalità che viene eseguita insieme alla sessione (per esempio l'HUD di una mappa o ottimizzazioni di caricamento), senza diventare parte di OmsiLaunch stesso. Il bridge lo carica, gli fornisce solo ciò che dichiara e rimuove tutto ciò che detiene quando la sessione termina o il modulo fallisce. Questa pagina è il riferimento normativo per i moduli runtime come implementati da `src/OmsiLaunch.Runtime.Modules` (manifest, impostazioni, idoneità, host del ciclo di vita), dal planner, dai programmi di sessione del controller (`ModuleSessionPrograms`, `ModuleSessionProcess`), dall'ambiente moduli del plugin e dai flag dei moduli della [CLI](cli.md).

Stabilità: `EXPERIMENTAL`. Il protocollo bridge 1 è stato validato a runtime nel round B (moduli gestiti, attivazione per contenuto, operazioni, `disable-module` e `fail-session`; componenti nativi con hook IAT; vedere [stato della validazione runtime](../status/runtime-validation-status.md)). Il protocollo bridge 2 (impostazioni, attivazione predefinita, file delle impostazioni, programmi di sessione, conflitti con i plugin, notifiche, `get_value`) è validato solo offline. Gate offline: i gate `module.*` di `tests/OmsiLaunch.ModuleTests` (manifest, integrità, selezione e attivazione, impostazioni, file delle impostazioni, conflitti con i plugin, programmi di sessione avviati tramite il proprio job, un errore iniettato in ogni fase con ogni politica di errore, ciclo di vita dei componenti nativi, pulizia, handoff), `plugin-runtime.modules-lifecycle` (un vero modulo gestito caricato dall'ambiente del plugin), `runtime-modules.native-component` (un vero componente nativo che aggancia un import e legge valori tramite il bridge), `runtime-modules.plugin-conflict` e `native-hooks.self-check` (il motore degli hook). Il protocollo bridge 3 (Beta 5: operazioni del catalogo in-process, il tick, la cattura dell'input, gli input di contenuti della community, i pin facoltativi con lo snapshot in fase di pianificazione, `module pack`, le opzioni dei job ausiliari, la versione di OMSI) è validato solo offline: i gate `module.*` di `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` e `ui-input.hook-self-check` dei test di integrazione del plugin, e `omsi-version.literal` dei test unitari. Il take-over del menu di pausa e la cattura dell'input non sono stati osservati in una sessione OMSI reale.

Il caricamento è aperto: qualsiasi modulo installato sotto `plugins\OmsiLaunch.Modules` può essere caricato, chiunque lo abbia realizzato. OmsiLaunch non garantisce ciò che un modulo fa all'interno di OMSI. Ciò che garantisce è il [confine](#security-boundary): i moduli non vengono mai eseguiti all'interno del processo OmsiLaunch, e nulla di ciò che OmsiLaunch offre consente a un modulo di andare oltre i diritti propri di OMSI.

<a id="where-modules-live"></a>
## Dove risiedono i moduli

Ogni modulo è una directory sotto la cartella del plugin permanente:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

Il nome della directory deve coincidere con l'`id` del modulo. I moduli sono installati da chi li distribuisce; una sessione non scrive, non acquisisce snapshot e non ripristina mai file in quella posizione, e i moduli non modificano mai file dell'installazione tramite OmsiLaunch (un manifest non ha alcun modo di dichiarare un overlay dell'installazione). I dati scrivibili di un modulo risiedono nella sua [directory dei dati](#data-directory).

## Manifest (`module.json`)

Schema `omsilaunch.runtime-module/v1`. Le chiavi sconosciute vengono rifiutate, quindi un manifest scritto per uno schema successivo viene respinto invece di essere compreso solo a metà. Tutti i percorsi sono relativi alla directory del modulo (i percorsi dei contenuti associati sono relativi alla radice di OMSI), usano `\` o `/` e non possono contenere `..` né un'unità.

| Chiave | Obbligatoria | Significato |
| --- | --- | --- |
| `schema` | sì | `omsilaunch.runtime-module/v1`. |
| `id` | sì | Id del modulo: lettere minuscole, cifre e `-`, da 2 a 48 caratteri, che inizia con una lettera o una cifra. È anche il nome della sua directory e il suo spazio dei nomi delle operazioni. |
| `version` | sì | `major.minor.patch`, facoltativamente `-suffix`. |
| `name` | no | Nome visualizzato (al massimo 128 caratteri). |
| `bridge_protocol` | sì | `{ "min": n, "max": n }`: i protocolli bridge con cui il modulo funziona. Questo build serve i protocolli `1`, `2` e `3`; un modulo viene accettato quando il suo intervallo include uno di essi. |
| `activation` | no | `requested` (predefinito): caricato quando la sessione lo richiede. `content`: caricato anche quando la sessione esegue la mappa associata (richiede `content_binding`). `default` (protocollo 2): caricato in ogni sessione a meno che non sia disattivato (vedere [scelta dei moduli](#choosing-modules-for-a-session)); non può avere un `content_binding`. |
| `required_capabilities` | no | Capability senza le quali il modulo non può funzionare (vedere [capability](#capabilities)). |
| `optional_capabilities` | no | Capability che il modulo usa quando disponibili; le verifica con `IsCapabilityAvailable`. |
| `content_binding` | no | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: il modulo viene eseguito solo su quella mappa, con quei file invariati. |
| `assets` | no | File letti dal modulo, ciascuno `{ "path", "sha256" }`. Dal protocollo 3 lo `sha256` è facoltativo (vedere [integrità](#integrity)). |
| `components` | sì | Esattamente un componente `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; facoltativamente un componente `native` (`.dll`), componenti `auxiliary` (`.exe`) e, con il protocollo 2, un componente `host` (`.exe`, il [programma di sessione](#session-programs) del modulo). Dal protocollo 3 lo `sha256` di un componente è facoltativo, e un componente `auxiliary` può avere `priority` (`normal`, `below-normal` o `idle`) e `memory_limit_mb` (da 64 a 65536): il job object del programma li applica dalla sua prima istruzione (vedere [programmi ausiliari](#auxiliary-programs)). |
| `failure_policy` | sì | `fail-session`, `disable-module` o `degrade` (vedere [errori](#failures)). |
| `operations` | no | Operazioni che il modulo espone mentre la sessione è in esecuzione: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | no | Protocollo 2. Le impostazioni accettate dal modulo (vedere [impostazioni](#settings)). |
| `conflicts` | no | Protocollo 2. `{ "plugins": [ "<file>.dll" ] }`: DLL di plugin OMSI che non devono essere eseguite accanto al modulo (vedere [conflitti](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | no | Protocollo 3. Operazioni del catalogo pubblico delle capability che il modulo esegue in-process, ad esempio `["time.read", "camera.set"]` (vedere [operazioni in-process](#operations-in-process)). Richiede la capability `bridge.runtime`. |
| `tick_ms` | no | Protocollo 3. Il modulo vuole una callback periodica ogni `tick_ms` millisecondi: un multiplo di 50, da 50 a 1000 (vedere [tick](#tick)). Richiede la capability `bridge.tick`. |
| `content_inputs` | no | Protocollo 3. Contenuti della community che il modulo legge, individuati dal bridge: `[ { "kind": "realmap-package", "required": false } ]` (vedere [contenuti della community](#community-content)). |
| `timeouts` | no | Budget in millisecondi: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (5000 ciascuno per impostazione predefinita, al massimo 30000); con il protocollo 2 anche `prepare_ms` (predefinito 120000, al massimo 1800000) e `host_stop_ms` (predefinito 10000, al massimo 60000) per il programma di sessione. Le operazioni hanno per impostazione predefinita 2000 ms. |
| `diagnostics` | no | `{ "trace": true\|false }`. |

Un manifest che usa una funzionalità del protocollo 2 (`settings`, `conflicts`, attivazione `default`, un componente `host`, `prepare_ms` o `host_stop_ms`) deve dichiarare `bridge_protocol.min` 2. Un manifest che usa una funzionalità del protocollo 3 (`runtime_operations`, `tick_ms`, `content_inputs`, una capability `bridge.runtime`, `bridge.tick` o `bridge.input`, un `priority` o `memory_limit_mb` ausiliario, oppure un componente o un asset senza `sha256`) deve dichiarare `bridge_protocol.min` 3.

Ogni file indicato da `components` e `assets` può essere fissato dal proprio SHA-256 (obbligatorio prima del protocollo 3, facoltativo da esso in poi: vedere [integrità](#integrity)). `module.json` non può fissare sé stesso.

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## Integrità

Ogni file nominato dal manifest deve esistere e non deve essere raggiunto tramite una junction o un collegamento simbolico, e ogni file fissato dall'autore deve avere lo SHA-256 fissato; in caso contrario il modulo viene rifiutato con `OL_E_MODULE_INTEGRITY`. Questo intercetta un modulo corrotto o aggiornato solo a metà. Non è una decisione di fiducia: un modulo non viene verificato rispetto ad alcun elenco, firma o editore.

Dal protocollo 3 un pin è facoltativo, così un autore non deve mai mantenere gli hash a mano, e il piano acquisisce sempre uno **snapshot**: un unico SHA-256 su ogni file nominato dal manifest e sul suo contenuto, registrato nel piano (`PlannedModule.SnapshotSha256`) e trasportato al plugin nell'handoff di avvio (versione wire 8). Lo snapshot è ciò che garantisce che i file valutati dal piano siano i file che vengono eseguiti: il plugin carica un modulo solo quando il suo `module.json` ha lo SHA-256 valutato dal piano e i suoi file hanno ancora il contenuto dello snapshot, e il controller e il bridge confrontano di nuovo lo snapshot subito prima che parta un programma di sessione, un componente nativo o un programma ausiliario. Un modulo i cui file sono cambiati dopo la pianificazione non viene caricato (`OL_E_MODULE_INTEGRITY`, "the module's files changed after planning"). Lo snapshot copre ogni modulo, con pin o senza; un pin permette inoltre a chi pubblica un modulo di rilevare una copia corrotta o aggiornata solo a metà non appena viene individuata.

La verifica viene ripetuta nel punto in cui parte il codice del modulo: il plugin carica un modulo solo se il suo `module.json` ha ancora lo SHA-256 valutato dal piano e ogni pin e lo snapshot sono ancora validi, e il controller avvia un programma di sessione solo dopo aver verificato il pin di quel programma e lo snapshot.

<a id="packing-a-module"></a>
### Creare il pacchetto di un modulo

`OmsiLaunch.exe /pack-module:<module directory>` scrive i pin per i file che un `module.json` nomina (i suoi componenti e asset), così un autore non modifica mai uno SHA-256 a mano. Riscrive solo i valori `sha256` (il resto del manifest viene mantenuto; il file viene re-indentato), rilegge il risultato con il parser del manifest prima di scrivere qualsiasi cosa, e riporta ogni file (envelope `module.pack`: `module`, `directory`, `written`, `intact`, `files` con `path`, `sha256`, `pinned`, `mismatch`, `written`). Opera sulla directory che gli viene fornita e non tocca mai un'installazione. `/verify-module:<module directory>` riporta lo stesso senza scrivere (`module.verify`); termina con `7` quando un file con pin non corrisponde al suo pin (un file senza pin non è una discrepanza). Entrambi terminano con `2` per un manifest che non viene analizzato o un file mancante.

<a id="security-boundary"></a>
## Confine di sicurezza

OmsiLaunch non controlla ciò che un modulo fa all'interno di OMSI: i suoi componenti gestiti e nativi vengono eseguiti nel processo OMSI con gli stessi diritti di qualsiasi plugin OMSI. OmsiLaunch garantisce quanto segue:

1. Il processo OmsiLaunch non carica mai codice dei moduli. Il codice dei moduli viene eseguito all'interno di OMSI, in un programma ausiliario avviato dall'interno di OMSI, o nel programma di sessione del modulo.
2. I programmi che OmsiLaunch avvia per i moduli sono file della directory del modulo stesso, verificati rispetto al proprio pin, avviati con una riga di comando fissata da OmsiLaunch. Un programma di sessione viene eseguito in un job object che lo termina insieme alla sessione, non consente un secondo processo (non può avviare altri programmi) e rifiuta modifiche ai parametri di sistema, alle impostazioni dello schermo, al desktop e alla disconnessione o all'arresto del sistema. Eredita solo i suoi due handle di pipe.
3. Tutto ciò che un modulo dichiara resta all'interno della sua directory, e i suoi dati scrivibili all'interno di `.omsilaunch\module-data\<id>`.
4. I canali di OmsiLaunch (controllo locale, handoff di avvio, telemetria, comandi runtime) restano limitati all'utente corrente e vincolati alla sessione; nessuno di essi consente a un chiamante di avviare un programma, di leggere o scrivere file al di fuori dell'installazione o di modificare le impostazioni di sistema.
5. OmsiLaunch non eleva mai i privilegi. Un modulo viene eseguito con i diritti dell'utente che ha avviato la sessione.
6. Il testo proveniente da un modulo (notifiche, eventi, valori) è trattato come dato: limitato in lunghezza, privo di caratteri di controllo, mai eseguito, aperto o seguito come collegamento.

<a id="capabilities"></a>
## Capability

`required_capabilities` e `optional_capabilities` indicano o una capability nativa del build in esecuzione (la colonna delle capability native del [catalogo delle capability](capabilities.md#capability-catalog), per esempio `time.read` o `d3d.texture`) oppure una funzionalità del bridge:

| Funzionalità del bridge | Cosa ottiene il modulo |
| --- | --- |
| `bridge.events` | `Emit`: eventi runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: thread in background tracciati. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: processi ausiliari vincolati alla sessione. |
| `bridge.hooks.iat` | Componente nativo: hook sugli import di `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Componente nativo: hook sui metodi del dispositivo Direct3D 9 attivo (`d3d9:device.<method>`). Richiede la capability nativa `d3d.texture`. |
| `bridge.runtime` | Protocollo 3. `IRuntimeModuleContext.Runtime` e il `run_operation` nativo: le operazioni del catalogo che il manifest elenca in `runtime_operations`. |
| `bridge.tick` | Protocollo 3. Una callback periodica: `ITickingModule.Tick` e l'export nativo `omsilaunch_module_native_v1_tick`. |
| `bridge.input` | Protocollo 3. `IRuntimeModuleContext.Input`: cattura dei messaggi di tastiera e mouse del gioco per il menu proprio del modulo. Richiede la capability nativa `ui.input.capture`. |

Un nome che non è né l'una né l'altra viene rifiutato in fase di pianificazione (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). Su un build di OMSI 2 senza layout noto, una capability nativa che il file eseguibile non risolve viene decisa in-process; se non viene risolta nemmeno lì, il modulo non viene caricato.

<a id="choosing-modules-for-a-session"></a>
## Scelta dei moduli per una sessione

Un modulo entra in una sessione in uno di quattro modi; `PlannedModule.Activation` indica quale:

| Attivazione | Quando |
| --- | --- |
| `requested` | La sessione lo richiede: `LaunchSpec.Modules.Enable`, `/module:<id>`, oppure `enabled: true` nel preset del profilo di sessione selezionato. |
| `settings-file` | `enabled=true` nella sua sezione del [file delle impostazioni](#the-settings-file). |
| `content` | La sua attivazione è `content` e la sessione esegue la mappa associata. |
| `default` | La sua attivazione è `default`. |

Un modulo viene escluso dalla sessione da `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` nel preset, oppure `enabled=false` nel file delle impostazioni (diagnostica del piano `module.disabled`). Una richiesta esplicita prevale sul file delle impostazioni. Abilitare e disabilitare lo stesso modulo nella stessa richiesta è `OL_E_MODULE_SETTING_INVALID`.

Regole di pianificazione: un modulo richiesto che non può essere caricato rende il piano non eseguibile con il proprio codice `OL_E_MODULE_*`. Un modulo entrato da solo (file delle impostazioni, contenuto, predefinito) che non può essere caricato non ferma la sessione: il piano segnala `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Impostazioni

Un modulo dichiara le proprie impostazioni in `settings`; ogni voce:

| Chiave | Obbligatoria | Significato |
| --- | --- | --- |
| `key` | sì | `[a-z][a-z0-9_]{0,31}`, non `enabled`. |
| `type` | sì | `boolean`, `integer`, `choice` o `text`. |
| `default` | sì | Il valore quando nient'altro lo imposta: un booleano, un intero, una stringa, oppure `"auto"` quando `auto` è true. |
| `auto` | no | `true`: il valore `auto` è accettato e passato al modulo, che decide a runtime. |
| `min`, `max` | no | Limiti interi. |
| `choices` | per `choice` | I valori accettati (`[a-z0-9][a-z0-9_.-]{0,31}`, non `auto`). |
| `max_length` | no | Limite di lunghezza del testo (1..4096, predefinito 256). I caratteri di controllo sono rifiutati. |
| `flag` | no | Un flag da riga di comando per questa impostazione, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, oppure `/<flag>` da solo per `true` su un booleano. |
| `description` | no | Al massimo 256 caratteri. |

I valori vengono normalizzati: i booleani accettano `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; le scelte vengono confrontate senza distinzione tra maiuscole e minuscole; gli interi vengono verificati rispetto ai propri limiti.

Il valore effettivo di ciascuna impostazione è, in ordine:

1. la richiesta: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, un flag proprio del modulo, oppure `modules.<id>.settings` nel preset del profilo di sessione selezionato. Come per ogni altro campo del preset, la riga di comando non può impostare anche una chiave già impostata dal preset (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. il [file delle impostazioni](#the-settings-file);
3. il valore predefinito del manifest.

Un valore richiesto non valido, o una chiave richiesta che il modulo non dichiara, rende il piano non eseguibile (`OL_E_MODULE_SETTING_INVALID`). Una voce del file delle impostazioni che non può essere usata viene ignorata con `OL_W_MODULE_SETTING_IGNORED`, così come le impostazioni richieste per un modulo che la sessione non carica. Il piano elenca ogni valore effettivo con la sua origine (`request`, `settings-file` o `default`) in `PlannedModule.Settings`.

Il modulo riceve ogni chiave dichiarata con il suo valore effettivo: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` in un componente nativo e l'oggetto `settings` del contesto di un programma di sessione.

<a id="the-settings-file"></a>
### Il file delle impostazioni

`<OMSI root>\.omsilaunch\modules.ini`, accanto ai profili di sessione. OmsiLaunch lo legge soltanto.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Le sezioni sono id di modulo; le righe `key=value` impostano le impostazioni, `enabled` attiva o disattiva il modulo per ogni sessione; le righe che iniziano con `;` o `#` sono commenti. Il file può essere grande al massimo 256 KiB. Tutto ciò che non può usare (una sezione che non è un id di modulo, una riga fuori da una sezione, una chiave sconosciuta, un valore non valido) viene segnalato con `OL_W_MODULE_SETTING_IGNORED` e ignorato.

<a id="session-programs"></a>
## Programmi di sessione

Un modulo può dichiarare un componente `host`: il suo programma di sessione, un eseguibile della sua directory. Quando il modulo è nel piano, il controller:

1. verifica il pin del programma e crea la directory dei dati del modulo;
2. lo avvia prima di OMSI, dalla directory del modulo, con la riga di comando `"<program>" --omsilaunch-session <session id>`, sospeso finché non è nel suo job (vedere il [confine di sicurezza](#security-boundary));
3. scrive il contesto come prima riga sul suo standard input;
4. attende che il programma scriva `{"type":"ready"}`, al massimo `prepare_ms`; solo allora avvia OMSI;
5. dopo la creazione di OMSI, scrive `{"type":"omsi-started","pid":<process id>}`;
6. al termine della sessione, dopo l'uscita di OMSI e prima che qualsiasi file venga ripristinato, scrive `{"type":"stop"}`, attende al massimo `host_stop_ms`, quindi termina il programma.

Entrambe le direzioni usano JSON UTF-8, un oggetto per riga. Il contesto:

| Campo | Significato |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identità. |
| `installation_root`, `module_directory`, `data_directory` | Percorsi completi. |
| `channel` | La named pipe che il programma può servire per i componenti del modulo in OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. Il programma la crea, limitata all'utente corrente, e verifica che il client sia il processo di OMSI. I componenti la leggono con `get_value("session_channel")` o `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | Le impostazioni effettive del modulo. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` della sessione. |
| `session_files` | File relativi all'installazione di proprietà della transazione di sessione. Il programma non deve tenerli aperti né mappati: vengono ripristinati quando la sessione termina. |

Ciò che il programma può scrivere oltre a `ready`:

| Riga | Effetto |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Evento runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Evento runtime `module.<id>.notice` (vedere [notifiche](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Diagnostica della sessione con quel codice catalogato (altrimenti `OL_E_MODULE_OPERATION_FAILED`). |

Le altre righe, le righe più lunghe di 64 KiB e tutto ciò che segue le 10,000 righe vengono ignorati. Il controller registra `module.<id>.host.started`, `host.ready` (con `prepare_ms`), `host.failed`, `host.exited` e `host.killed`.

Un programma che non è pronto entro `prepare_ms`, termina prima di essere pronto o il cui pin non è valido, fa fallire la preparazione del proprio modulo: con `fail-session` la sessione non parte (`OL_E_MODULE_SESSION_FAILED`); con `disable-module` il programma viene terminato e il modulo viene escluso da OMSI; con `degrade` il modulo resta (e il programma continua a essere eseguito se può ancora). Il `StartupTimeoutSeconds` della sessione decorre da quando OMSI viene avviato, quindi la preparazione non lo consuma.

<a id="conflicts-with-omsi-plugins"></a>
## Conflitti con i plugin OMSI

`conflicts.plugins` indica le DLL di plugin OMSI che non devono essere eseguite accanto al modulo, per esempio un plugin più vecchio che installa gli stessi hook. In fase di pianificazione, OmsiLaunch legge la sezione `[dll]` di ogni `plugins\*.opl`; se uno carica una DLL in conflitto, il modulo viene rifiutato con `OL_E_MODULE_CONFLICT` e il motivo indica il file `.opl`. All'interno di OMSI, un modulo la cui DLL in conflitto viene comunque caricata non viene caricato. OmsiLaunch non modifica mai i file di un altro plugin; disattivarlo è una decisione dell'utente.

<a id="notices"></a>
## Notifiche

Un evento di modulo di nome `notice` (da `Emit("notice", ...)`, da un `emit` di un componente nativo o da una riga di notifica di un programma di sessione) viene mostrato dall'icona nell'area di notifica di OmsiLaunch come un fumetto il cui titolo indica il modulo. Vengono conservati solo `title` (al massimo 63 caratteri) e `text` (al massimo 255 caratteri), i caratteri di controllo vengono rimossi e nulla in esso viene aperto o eseguito. Un programma di sessione che deve informare l'utente prima dell'avvio di OMSI (quando l'icona nell'area di notifica non è ancora mostrata) usa una propria finestra.

<a id="data-directory"></a>
## Directory dei dati

`<OMSI root>\.omsilaunch\module-data\<id>\` è la directory scrivibile del modulo per questa installazione: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` e il `data_directory` del programma di sessione. Viene creata al primo utilizzo. Non fa parte della transazione di sessione: ciò che un modulo vi scrive resta dopo la sessione. `/clear-module-data:<id>` la elimina (e nient'altro); viene rifiutato mentre una sessione possiede l'installazione, e una directory raggiunta tramite un collegamento viene rimossa come collegamento, mai seguita.

<a id="lifecycle"></a>
## Ciclo di vita

| Passo | Dove | Cosa avviene |
| --- | --- | --- |
| 1. Individuazione | controller | Viene letto `plugins\OmsiLaunch.Modules\*`: manifest e integrità (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Idoneità | controller, in fase di pianificazione | Vengono valutati i moduli che [entrano nella sessione](#choosing-modules-for-a-session): utilizzabilità, protocollo bridge, capability, associazione al contenuto, conflitti con i plugin, impostazioni. Il risultato è `SessionPlan.PlannedModules`. |
| 3. Preparazione | controller, all'avvio | I [programmi di sessione](#session-programs) dei moduli vengono avviati e attesi. Poi i moduli idonei viaggiano verso il plugin nell'handoff di avvio (versione 6): le loro directory, lo SHA-256 di ogni `module.json` valutato e le loro impostazioni effettive. |
| 4. Armamento | plugin, prima dell'avvio del mondo | Ogni modulo viene riverificato e caricato (componente gestito nel proprio contesto di caricamento). Il suo componente nativo, se presente, viene caricato e avviato per primo, poi viene chiamato `Arm(context)`. |
| 5. Gameplay pronto | plugin | Dopo `gameplay.entered`, quando un modulo caricato dichiara `bridge.hooks.d3d9`, il bridge inizia a osservare il dispositivo Direct3D (`d3d.ready`); poi chiama `gameplay_ready` del componente nativo e `GameplayReady()`. Per una sessione NEW_MAP la mappa associata viene ricontrollata rispetto alla mappa caricata dall'avvio. |
| 6. Runtime | plugin | Le operazioni `module.<id>.<operation>` raggiungono `Execute`. Un modulo con `tick_ms` riceve i tick (vedere [tick](#tick)); un modulo con `runtime_operations` le esegue in-process (vedere [operazioni in-process](#operations-in-process)). |
| 7. Arresto | plugin, poi controller | Quando la sessione termina (o il modulo fallisce), viene chiamato `Stop()`. Dopo l'uscita di OMSI, i programmi di sessione vengono arrestati. |
| 8. Pulizia | plugin | Tutto ciò che il modulo ha tracciato viene rilasciato in ordine inverso e verificato come scomparso; il componente nativo viene arrestato per ultimo e i suoi hook vengono rimossi; la sua DLL resta caricata, inerte, fino all'uscita di OMSI (un thread potrebbe ancora tornare attraverso un detour). |

Ogni callback in-process viene eseguito sul thread UI di OMSI, come le operazioni runtime proprie del prodotto. Il bridge misura ogni callback rispetto al proprio budget: un callback che torna in ritardo fa fallire la propria fase (`OL_E_MODULE_PHASE_TIMEOUT`). Il bridge non può interrompere un callback che non ritorna mai; un tale modulo blocca OMSI, e la sessione termina al proprio timeout di avvio o con `session stop` come qualsiasi altra sessione.

<a id="failures"></a>
## Errori

Un errore è un'eccezione o un superamento del budget in qualsiasi fase o operazione, una capability non risolta in-process, un'associazione al contenuto che non regge in-process, un plugin in conflitto caricato in OMSI, oppure un programma di sessione che non diventa pronto. La `failure_policy` del modulo decide cosa segue. In ogni caso le risorse del modulo vengono rilasciate. Un tick che genera un'eccezione, restituisce un valore diverso da zero o supera il proprio intervallo è un errore come quello di un'operazione.

| Politica | Errore in preparazione, caricamento, armamento o gameplay pronto | Errore in un'operazione | Errore nell'arresto |
| --- | --- | --- | --- |
| `fail-session` | Modulo rilasciato; `module.session-failure`; la sessione fallisce con `OL_E_MODULE_SESSION_FAILED`, OMSI viene arrestato e i file vengono ripristinati. Un errore prima dell'avvio del mondo impedisce l'avvio. | Uguale. | Segnalato (`module.failed`); la sessione prosegue fino alla fine. |
| `disable-module` | Modulo rilasciato e disattivato; la sessione prosegue. | Uguale. | Segnalato; modulo rilasciato. |
| `degrade` | Come `disable-module`, salvo che l'errore di un programma di sessione lascia il modulo nella sessione. | La chiamata che fallisce restituisce il proprio codice; il modulo resta attivo; `module.degraded` viene segnalato una volta per codice. | Segnalato; modulo rilasciato. |

Un modulo disattivato o fallito risponde alle proprie operazioni con `OL_E_MODULE_DISABLED`. Gli errori dei moduli compaiono come diagnostica della sessione (`SessionStatus.Diagnostics`) con il proprio codice `OL_E_MODULE_*` e come eventi runtime.

<a id="cleanup"></a>
## Pulizia

Ciò che un modulo acquisisce tramite il proprio contesto viene tracciato: le risorse registrate con `Track`, i thread avviati con `RunBackground`, i processi avviati con `StartAuxiliaryProcess`. All'arresto o in caso di errore il bridge annulla `Stopping`, poi le rilascia in ordine inverso. I processi ausiliari vengono inoltre inseriti in un job object kill-on-close, così terminano con OMSI anche se OMSI viene terminato forzatamente; il job di un programma di sessione lo termina con la sessione OmsiLaunch, anche se OmsiLaunch stesso viene terminato forzatamente. Una risorsa ancora attiva dopo il rilascio (un processo che non è uscito, un thread che non è terminato) viene segnalata come `OL_E_MODULE_CLEANUP_INCOMPLETE`. I thread o i processi che un modulo crea senza il proprio contesto non vengono tracciati.

Dopo un crash non c'è nulla da recuperare per i moduli: non detengono file dell'installazione, e la loro memoria, i loro thread e i processi vincolati al job terminano con il processo OMSI o con il processo OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Componenti nativi e hook

Un componente nativo è una DLL a 32 bit che implementa l'interfaccia C in `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Export | Chiamato |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | All'armamento, prima di `Arm` gestito. Un valore diverso da zero fa fallire la fase. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Facoltativo; dopo l'ingresso nel gameplay. |
| `void omsilaunch_module_native_v1_stop(void)` | All'arresto o in caso di errore, prima che i suoi hook vengano rimossi. Da quel momento la tabella del bridge rifiuta ogni chiamata e la DLL resta caricata, inerte, fino all'uscita di OMSI. |

La tabella del bridge fornisce al componente `request_hook`, `emit` (eventi runtime `module.<id>.<name>` con un oggetto JSON di stringhe) e, dal protocollo 2, `get_value`. Un componente legge il `size` della tabella prima di usare `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copia un valore come UTF-8 con uno zero terminatore e imposta `*length` ai byte necessari senza di esso; restituisce `0`, `1` (argomento), `2` (nome sconosciuto), `11` (dopo l'arresto) o `12` (buffer troppo piccolo). Dal protocollo 3 la tabella contiene anche `run_operation` (vedere [operazioni in-process](#operations-in-process)) e `d3d_is_game_device(context, device)`: indica se un puntatore a dispositivo ricevuto da un hook Direct3D è il dispositivo proprio del gioco (`1`) o un altro (`0`; il form di selezione del veicolo crea un secondo dispositivo con la stessa tabella dei metodi, quindi un detour di `Present` viene chiamato per entrambi e dovrebbe disegnare solo per quello del gioco), `10` quando non è ancora osservato alcun dispositivo, `11` quando `bridge.hooks.d3d9` non è dichiarato o dopo l'arresto. Il puntatore viene solo confrontato, mai usato, e il bridge non ne consegna mai uno. Un modulo che mantiene risorse Direct3D nel pool predefinito aggancia da sé `d3d9:device.Reset` (ciò che fa prima di chiamare l'originale è «dispositivo perso», dopo che questo restituisce successo è «dispositivo ripristinato»). Un componente controlla il `size` della tabella prima di usare un membro del protocollo 3 (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Due export facoltativi completano il componente: `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (vedere [tick](#tick)) e `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, il modo in cui il componente gestito chiama quello nativo tramite `IRuntimeModuleContext.NativeComponent.Execute` (JSON in ingresso, JSON in uscita, la stessa convenzione di buffer di `run_operation`). I nomi a cui risponde `get_value`:

| Nome | Valore |
| --- | --- |
| `setting.<key>` | Il valore effettivo di un'impostazione dichiarata. |
| `data_directory` | La [directory dei dati](#data-directory) del modulo. |
| `module_directory` | La directory propria del modulo. |
| `session_id` | L'id della sessione. |
| `module_version` | La versione del manifest. |
| `product_version` | La versione del prodotto OmsiLaunch. |
| `session_channel` | La pipe del [programma di sessione](#session-programs); sconosciuta per un modulo che non ne ha uno. |
| `omsi_version` | La versione del prodotto OMSI che il gioco riporta di sé (protocollo 3; vedere [la versione di OMSI](#the-omsi-version)). |

Gli hook vengono richiesti solo tramite target simbolico, durante `start` o `gameplay_ready`, per un target la cui capability è dichiarata dal manifest:

| Target | Aggancia | Capability |
| --- | --- | --- |
| `iat:<dll>!<function>` | Un import di `Omsi.exe`, per esempio `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` o `EndScene` del dispositivo Direct3D 9 attivo (da richiedere in `gameplay_ready`, quando il dispositivo esiste). | `bridge.hooks.d3d9` |

`request_hook` restituisce in `*original` la funzione con cui proseguire la catena (un detour deve chiamarla). Il suo risultato: `0` successo, `1` argomento, `2` target sconosciuto, `3` il sito non contiene ciò che dovrebbe, `4` il sito è agganciato da qualcuno al di fuori del bridge, `5` protezione della memoria, `6` registro pieno, `7` ordine, `8` memoria, `9` un thread stava eseguendo i byte da modificare, `10` nessun dispositivo Direct3D ancora, `11` non consentito ora o capability non dichiarata.

Ogni hook che il bridge installa, per il prodotto o per un modulo, è registrato in un unico registro con il proprio proprietario (`module:<id>` per un modulo):

- Un hook viene installato solo sul valore che il sito dovrebbe contenere (la funzione importata; una funzione di un modulo denominato `d3d9.dll`, il runtime di sistema o un wrapper), oppure sovrapposto all'hook più in alto del registro su quel sito. Un sito già modificato da altri viene rifiutato (`4`), mai sovrascritto.
- Gli hook su un sito vengono rimossi in ordine last-in first-out; un hook con un altro sopra di sé resta finché quest'ultimo non viene rimosso.
- L'hook `Reset` Direct3D proprio del prodotto è nel registro, quindi gli hook del dispositivo dei moduli si sovrappongono ad esso.
- Alla pulizia il bridge rimuove ogni hook del modulo dopo il ritorno di `stop`. Se uno non può essere rimosso (qualcun altro ha modificato il sito), la pulizia segnala `OL_E_MODULE_CLEANUP_INCOMPLETE`. La DLL di un componente nativo non viene mai scaricata mentre OMSI è in esecuzione, quindi un detour attraverso cui si sta ancora tornando resta valido.

Il motore supporta anche hook sui prologhi di funzione con un trampolino (gli altri thread vengono sospesi mentre i byte cambiano, e i byte attesi esatti vengono verificati prima), ma nessun target di prologo è ancora offerto ai moduli: tali target provengono dal catalogo dei target del build con byte verificati.

<a id="operations"></a>
## Operazioni

Un'operazione di modulo è `module.<id>.<operation>`. Esiste solo per una sessione il cui piano ha caricato il modulo (`PlannedModule.Operations`); altrimenti `ExecuteRuntimeAsync` risponde `OL_E_MODULE_NOT_LOADED` oppure `OL_E_MODULE_OPERATION_UNKNOWN`. Gli argomenti obbligatori vengono verificati e gli argomenti non dichiarati vengono rifiutati (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Un errore tipizzato di un modulo mantiene il proprio codice quando è un codice pubblico catalogato; qualsiasi altro codice diventa `OL_E_MODULE_OPERATION_FAILED` con il codice del modulo in `Values["module_code"]`. Le chiavi di risultato che seguono la convenzione di denominazione interna vengono rimosse, come per ogni operazione runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## Programmi ausiliari

Un modulo avvia uno dei propri componenti `auxiliary` (un `.exe` con pin della propria directory) con `StartAuxiliaryProcess`. Dal protocollo 3 il bridge crea il programma **sospeso**, lo colloca nel job object della sessione e solo dopo lo riprende, così un figlio che avvia subito non può essere eseguito fuori dal job. Il job termina il programma e tutto ciò che ha avviato quando il modulo si arresta o OMSI esce (kill on close) e applica il `priority` (`below-normal`, `idle`) e il `memory_limit_mb` del manifest dalla prima istruzione del programma. A differenza di un [programma di sessione](#session-programs), il job di un programma ausiliario consente processi figlio (un programma che incorpora una web view ne ha bisogno); ciascuno di essi è nel job. Prima dell'avvio il programma viene verificato di nuovo: il suo pin quando ne ha uno, e sempre lo snapshot del piano.

<a id="operations-in-process"></a>
## Operazioni in-process

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (gestito) e `run_operation` (nativo) eseguono in-process un'operazione del [catalogo pubblico delle capability](capabilities.md#public-runtime-operations), sul thread UI di OMSI, con lo stesso registro, gli stessi risultati `OL_E_*` tipizzati, gli stessi controlli degli argomenti e la stessa policy di modifica del canale di controllo runtime, e senza il canale. Sono consentite solo le operazioni che il manifest elenca in `runtime_operations`: qualsiasi altra riceve `OL_E_MODULE_OPERATION_NOT_DECLARED` prima di raggiungere OMSI, e le operazioni `internal.*` e `module.*` non possono mai essere elencate. Un'operazione la cui capability nativa il build non risolve risponde `OL_E_CAPABILITY_UNRESOLVED`, come ovunque.

L'elenco è un'informazione di trasparenza e di pianificazione, non un confine di sicurezza: un modulo gira già dentro OMSI con i diritti dell'utente. Il piano lo mostra (`PlannedModule.RuntimeOperations`), e un'operazione che cambia lo stato di OMSI vi riporta la propria policy di modifica, ad esempio `camera.set (omsi-state)`. Un modulo può eseguire operazioni da `Arm` in poi (il mondo potrebbe non esistere ancora; l'operazione risponde allora con il proprio errore tipizzato). I risultati sono limitati (1 MiB); un risultato più grande è `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Dopo l'arresto del modulo, ogni chiamata risponde `OL_E_MODULE_DISABLED`.

Il `run_operation(context, operation, arguments_json, buffer, capacity, length)` nativo prende l'id del catalogo e un oggetto JSON di stringhe (o null) e scrive `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. Restituisce `0` quando ha risposto (il fallimento proprio dell'operazione è dentro il JSON), `1` per un argomento, `11` dopo l'arresto e `12` quando il buffer è troppo piccolo (`*length` è la dimensione necessaria).

## Tick

Un modulo che dichiara `tick_ms` e la capability `bridge.tick` viene chiamato circa ogni `tick_ms` millisecondi, sul thread UI di OMSI, dal timer UI da 50 ms del plugin: `ITickingModule.Tick(TimeSpan elapsed)` sul componente gestito e l'export nativo facoltativo `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. L'intervallo è un multiplo di 50 tra 50 e 1000. Un tick non è per frame: un modulo che disegna aggancia il metodo Direct3D `Present` e usa il tick per leggere i dati. I tick iniziano dopo `gameplay.entered` e terminano con il modulo. Un tick che genera un'eccezione, restituisce un valore diverso da zero (nativo) o impiega più del proprio intervallo è un errore di quel modulo: con `degrade` viene riportato una volta per codice (`module.degraded`) e il modulo continua a girare (i tick persi vengono saltati, mai accodati); con `disable-module` e `fail-session` si applica la politica. Un ciclo modale annidato di OMSI mantiene attivo il timer UI, quindi un tick non rientra mai in se stesso.

<a id="community-content"></a>
## Contenuti della community

`content_inputs` dichiara contenuti che il modulo legge e che il suo autore non può conoscere quando il modulo viene compilato, perché li produce la community, per una mappa, dopo il modulo. Non c'è alcun SHA-256 da fissare; il bridge si limita a individuarli e a verificare che leggerli sia sicuro. L'unico tipo oggi è `realmap-package`: la cartella `openomsi-realmap` di una mappa, `<OMSI root>\maps\<folder>\openomsi-realmap`, scritta dagli strumenti del pacchetto mappa openOMSI.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` risponde se la mappa ha un pacchetto (non averne uno non è un errore) e, quando lo ha, la sua directory, il numero di file, la dimensione totale e un `Fingerprint`. Il fingerprint è un rilevatore di modifiche sul percorso relativo, la dimensione e l'ora di scrittura di ogni file: usarlo come chiave di cache; non è mai un controllo di integrità né una decisione di fiducia. Il pacchetto deve trovarsi dentro l'installazione, nessuna sua parte può essere un reparse point, ed è limitato (50,000 file, 4 GiB); in caso contrario la risposta è `OL_E_MODULE_CONTENT_INPUT_INVALID`. Un modulo che non ha dichiarato il tipo riceve `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. Il pacchetto è un dato prodotto da terzi: leggerlo in codice gestito, trattare ogni valore come non attendibile e degradare per risorsa. Un input `required: true` fa sì che il piano rifiuti il modulo (`OL_E_MODULE_CONTENT_BINDING`) quando la mappa della sessione non ha un pacchetto.

<a id="input-capture"></a>
## Cattura dell'input

Un modulo che sostituisce il menu di pausa di OMSI con il proprio ha bisogno della tastiera e del mouse del gioco. OMSI legge i tasti di simulazione tramite DirectInput e il mouse tramite messaggi di finestra alla finestra di vista del form principale; mentre la sua pausa è attiva (il byte di pausa nativo), la simulazione, il rendering, i clic 3D e i comandi DirectInput sono bloccati da OMSI stesso. `IRuntimeModuleContext.Input` (capability `bridge.input`, capability nativa `ui.input.capture`) aggiunge il resto: `Capture(true)` installa un hook `WH_GETMESSAGE` thread-local sul thread UI, e i messaggi di tastiera e mouse che appartengono all'albero di finestre del gioco (il form principale, i suoi figli e il form di pausa nativo nascosto) vengono registrati come semplici valori `ModuleInputEvent` (tipo, tasto virtuale, coordinate client della finestra di vista, pulsante, delta della rotellina, modificatori) e rimossi dalla coda, così né OMSI né la VCL agiscono su di essi. `Drain()` restituisce gli eventi (ne vengono conservati al massimo 256). Nulla viene subclassato, nessuna funzione user32 sullo stato dei tasti viene agganciata e nulla viene inviato.

Un solo modulo alla volta effettua la cattura. La cattura viene rilasciata quando il modulo si arresta o fallisce, qualunque cosa abbia fatto `Stop`. Le finestre di dialogo modali native (salva, opzioni, nuova partita) richiedono input reale: rilasciare la cattura prima di chiederne una. La meccanica dell'hook è validata offline con una finestra sostitutiva (`ui-input.hook-self-check`); il suo comportamento rispetto alla vera finestra del gioco non è ancora osservato.

<a id="the-omsi-version"></a>
## La versione di OMSI

`IRuntimeModuleContext.OmsiVersion` e `get_value("omsi_version")` forniscono la versione del prodotto OMSI che il gioco riporta di sé, ad esempio `2.3.004`: il testo che OMSI scrive dopo `Version:` alla riga 7 del proprio `logfile.txt`, letto dall'immagine in esecuzione. Non è **mai** la versione di file PE dell'eseguibile (il build di riferimento riporta `FileVersion` `2.2.032`). Quando l'immagine non contiene un unico testo di questo tipo, viene usata la versione del profilo di build se l'identità del codice è nota, altrimenti `unknown`. L'hash dell'eseguibile, la dimensione del suo codice, gli indirizzi e il nome del profilo non sono esposti. Usare la versione per il testo e per scegliere il comportamento; le capability (`IsCapabilityAvailable`) restano la fonte di riferimento per ciò che funziona.

<a id="writing-a-module-contracts"></a>
## Scrivere un modulo (contratti)

Il componente gestito fa riferimento a `OmsiLaunch.Runtime.Modules` e `OmsiLaunch.Api` e implementa `IRuntimeModule`. Il bridge associa questi due riferimenti ai propri assembly caricati (qualunque sia il contesto di caricamento in cui gira il plugin), quindi una copia fornita con il modulo viene ignorata:

| Membro | Chiamato |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Passo 4. Acquisire le risorse tramite il contesto. |
| `void GameplayReady()` | Passo 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Passo 6, per ogni operazione dichiarata (senza il prefisso `module.<id>.`). Generare `ModuleOperationException(code, message)` per un errore tipizzato. |
| `void Stop()` | Passo 7. |

`IRuntimeModuleContext` offre `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (un cancellation token), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess`, e dal protocollo 3 `Runtime` (vedere [operazioni in-process](#operations-in-process)), `Content` (vedere [contenuti della community](#community-content)), `Input` (vedere [cattura dell'input](#input-capture)) e `NativeComponent` (il componente nativo caricato, oppure null). Un componente gestito che vuole i tick implementa anche `ITickingModule`. Nulla nel contesto espone indirizzi, l'hash dell'eseguibile o il layout del suo codice. Il componente deve essere caricabile dal plugin a 32 bit (AnyCPU o x86, .NET 6); una copia di `OmsiLaunch.Runtime.Modules` o `OmsiLaunch.Api` accanto ad esso viene ignorata.

<a id="known-limitations"></a>
## Limitazioni note

- I componenti nativi possono agganciare gli import di `Omsi.exe` e i metodi del dispositivo Direct3D; gli hook sui prologhi delle funzioni di OMSI e sui call site non sono ancora offerti ai moduli.
- Il take-over del menu di pausa (`ui.pause.*`) e la cattura dell'input sono progettati staticamente e validati offline; non sono stati osservati in una sessione OMSI reale, e `ui.pause-menu.replace` viene risolta solo su un'identità del codice nota (alcuni suoi campi non hanno una derivazione indipendente dal build).
- La verifica in-process dell'associazione al contenuto usa la mappa richiesta dall'avvio (NEW_MAP); per una situazione salvata l'associazione viene verificata in fase di pianificazione rispetto alla mappa della situazione.
- Gli hook del dispositivo sono hook di vtable. Dove un altro componente riscrive i metodi originali del dispositivo (osservato sull'installazione di regressione poco dopo l'avvio del gameplay), un hook del dispositivo smette di essere chiamato e non può essere rimosso; la pulizia segnala allora `OL_E_MODULE_CLEANUP_INCOMPLETE` e il componente resta caricato. Un metodo del dispositivo già agganciato da qualcosa al di fuori del registro viene rifiutato (4); l'evento di rifiuto indica il modulo che possiede il metodo.
- I moduli si arrestano a una normale uscita di OMSI; un arresto della sessione termina OMSI, quindi la pulizia dei moduli all'interno di una sessione avviene solo tramite una politica di errore.
- Le notifiche vengono mostrate una volta che l'icona nell'area di notifica esiste, dopo l'avvio di OMSI; un programma di sessione mostra una propria finestra per tutto ciò che deve comunicare prima di allora.
