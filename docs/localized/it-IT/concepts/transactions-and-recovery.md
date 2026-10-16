# Transazioni e recupero

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Traduzione della [pagina originale in inglese](../../../concepts/transactions-and-recovery.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Ogni sessione OmsiLaunch che modifica un file di OMSI lo fa all'interno di una transazione durevole registrata nel journal: i byte originali vengono salvati in un backup prima di essere sostituiti, il journal registra fin dove è arrivata la sessione e il ripristino verifica ogni backup prima di riscriverlo. Questa pagina descrive tale transazione così come è implementata da `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) e pilotata da `OmsiLaunchService.StartAsync`, `SuperviseAsync` e `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), insieme ai file di input calcolati da `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). È rivolta agli utenti che devono sapere che cosa modifica una sessione e che cosa fa il recupero, e agli integratori che hanno bisogno delle garanzie esatte.

<a id="what-a-session-changes"></a>
## Che cosa modifica una sessione

Nella transazione entrano solo gli **overlay temporanei**. Vengono calcolati prima dell'apertura della transazione e ripristinati alla sua chiusura.

| Input della sessione | File | Tipo |
| --- | --- | --- |
| `/set:<key>=<value>`, `settings` del profilo, `LaunchSpec.Environment.*` | `options.cfg` (patch semantiche dei token; byte CP1252 preservati, UTF-8/UTF-16 con BOM rispettati) | overlay |
| Splash screen gestita (`SplashMode.Managed`, il valore predefinito) | `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<language>.bmp` | overlay (il file localizzato viene creato dalla transazione se l'installazione non ne ha uno) |
| Internet Textures (texture da Internet) `Override` | `Texture\standard.itx` | overlay |
| Internet Textures `Override` | ogni destinazione elencata nel file `.itx`, più `Texture\standard.ipr` | eliminazione di sessione |
| Sempre | `closecheck` (se assente prima della sessione) | eliminazione di sessione |

I file permanenti del prodotto **non** partecipano alla transazione: la chiusura del plugin in `plugins\OmsiLaunch.*` (solo validata, vedere [plugin permanente](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (copiati una sola volta, mai rimossi), la diagnostica in `.omsilaunch\diagnostics`, i pacchetti dei profili di sessione e la documentazione e gli esempi del rilascio. I plugin di terze parti e ogni altro file di OMSI non vengono mai enumerati, copiati, rimossi o ripristinati.

OMSI stesso continua a scrivere il proprio stato mentre una sessione è in esecuzione, esattamente come in un normale avvio di OMSI: `options.cfg` (ad esempio `[last_map]` quando la sessione carica una mappa diversa, riscritto all'ingresso nel gameplay), `Texture\standard.ipr`, le cache degli orari e delle lightmap (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, il profilo del conducente in `Drivers\` e i suoi log. Una scrittura su un percorso di proprietà della sessione (vedere sopra) viene annullata dal ripristino; ogni altra scrittura di OMSI persiste dopo la sessione, proprio come accadrebbe eseguendo OMSI direttamente. Evidenza del round di chiusura runtime: una sessione da situazione salvata su un'altra mappa ha lasciato `[last_map]` modificato perché non applicava un overlay a `options.cfg` (`CAM01`), mentre le sessioni con `/set` hanno ripristinato esattamente `options.cfg` (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Stati della transazione

`TransactionState` viene reso persistente nel journal dopo ogni transizione. I valori vengono serializzati come interi da `System.Text.Json`.

| Valore | Stato | Scritto quando |
| --- | --- | --- |
| 0 | `Prepared` | Sono stati acquisiti gli snapshot di ogni percorso di overlay e di eliminazione e i relativi backup sono stati scaricati su disco. Nell'installazione non è ancora cambiato nulla. Questo è l'obbligo di recupero: da qui in poi, un crash lascia un journal recuperabile. |
| 1 | `Applied` | Ogni overlay è stato scritto in modo atomico e ogni eliminazione è stata effettuata. |
| 2 | `RuntimeDeployed` | L'integrità del plugin permanente è stata validata per questo avvio (non viene distribuito nulla; il nome è storico). |
| 3 | `HandoffCreated` | L'handoff di avvio, lo slot di telemetria e la mailbox runtime esistono come memoria condivisa con nome. |
| 4 | `ProcessStarted` | `Omsi.exe` è stato creato. Il journal ora contiene anche `ProcessId`, `ProcessStartFileTimeUtc` (ora di creazione, tick UTC) e `ExecutablePath`. |
| 5 | `ProcessExited` | Il supervisore ha confermato l'uscita del processo (uscita naturale o `TerminateProcess`). |
| 6 | `Restoring` | Il ripristino è iniziato. |
| 7 | `Restored` | Ogni file di proprietà è stato ripristinato e verificato. Subito dopo il journal viene eliminato e `backup\<session>` rimosso. |
| 8 | `Completed` | Dichiarato nell'enum ma mai reso persistente; una transazione completata non ha journal. |

Il ciclo di vita di una sessione normale è quindi: snapshot -> `Prepared` -> overlay scritti / eliminazioni effettuate -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal eliminato -> `backup\<session>` rimosso. I valori pubblici di `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` e `Completed` seguono la stessa progressione dall'esterno (vedere [ciclo di vita della sessione](session-lifecycle.md)).

Una sessione senza modifiche a file di sua proprietà scrive comunque un journal per il proprio ciclo di vita; il suo ripristino è un'operazione nulla verificata.

<a id="journal-file"></a>
## File del journal

Percorso: `<root>\.omsilaunch\journal.json`. Esiste al massimo un journal per installazione; la sua presenza significa «una transazione è in sospeso».

Campi di `TransactionJournal`:

| Campo | Tipo | Significato |
| --- | --- | --- |
| `SessionId` | GUID | Sessione proprietaria del journal; è anche il nome della directory di backup (formato `N`). |
| `State` | intero | `TransactionState` descritto sopra. |
| `Files` | array di `JournalFile` | Una voce per ogni percorso di proprietà. |
| `ProcessId` | intero o null | PID di OMSI, a partire da `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long o null | Ora di creazione di OMSI (tick UTC), a partire da `ProcessStarted`. |
| `ExecutablePath` | stringa o null | Percorso completo dell'`Omsi.exe` avviato, a partire da `ProcessStarted`. |

Campi di `JournalFile`:

| Campo | Tipo | Significato |
| --- | --- | --- |
| `RelativePath` | stringa | Percorso relativo alla radice dell'installazione (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Indica se il file esisteva prima della sessione. |
| `Sha256` | stringa esadecimale | SHA-256 dei byte originali (di un array di byte vuoto quando `Existed` è false). |
| `BackupPath` | stringa | Percorso assoluto della copia di backup (scritto solo quando `Existed`). |
| `AppliedSha256` | stringa esadecimale o null | SHA-256 dei byte dell'overlay che la sessione ha scritto in questo percorso; null per le eliminazioni di sessione. È l'impronta di proprietà per i file originariamente assenti. |
| `LastWriteTimeUtcTicks` | long o null | Ora dell'ultima scrittura originale. |
| `CreationTimeUtcTicks` | long o null | Ora di creazione originale. |
| `Attributes` | intero o null | `FileAttributes` originali (incluso `ReadOnly`). |
| `SessionDeletion` | bool | True per i percorsi che la sessione ha chiesto di mantenere assenti (destinazioni `.itx`, `Texture\standard.ipr`, `closecheck`). |

I journal scritti da build precedenti senza `AppliedSha256` e i campi dei metadati sono ancora leggibili; vedere [File originariamente assenti](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Struttura dei backup

| Percorso | Contenuto |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Una directory per sessione, creata insieme al journal `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Byte originali esatti di un file di proprietà esistente. I file originariamente assenti non hanno backup. |

I backup e il journal vengono scritti tramite un file temporaneo (`<path>.omsilaunch.tmp`), in modalità write-through con un `Flush(true)` esplicito, quindi con un `File.Move` atomico con sovrascrittura. Il file temporaneo viene sempre eliminato, anche in caso di errore. Lo stesso percorso di scrittura viene usato per gli overlay e per gli originali ripristinati, quindi nessun file `*.omsilaunch.tmp` sopravvive a un'operazione completata.

I backup vengono rimossi solo dopo che il journal che li referenziava è stato eliminato. Un errore nell'eliminazione di `backup\<session>` è solo estetico e non annulla mai un ripristino verificato.

<a id="restore"></a>
## Ripristino

`RestoreAsync` viene eseguito dopo `ProcessExited` (o durante il recupero). Per ogni percorso registrato nel journal:

| Stato originale | Action |
| --- | --- |
| Esistente | Viene calcolato l'hash dei byte di backup e confrontato con `Sha256`; una discrepanza interrompe l'operazione con `OL_E_RECOVERY_BACKUP_CORRUPT` prima che venga scritto qualsiasi cosa. I byte vengono quindi scritti in modo atomico (a un file corrente di sola lettura viene prima rimosso l'attributo) e vengono ripristinati ora di creazione, ora dell'ultima scrittura e attributi (`RestoreMetadata`; gli errori sui metadati vengono ignorati, affinché un problema di permessi non possa bloccare un ripristino esatto byte per byte). |
| Assente, ora presente, `AppliedSha256` noto | Viene calcolato l'hash dei byte correnti. Se corrispondono ad `AppliedSha256` il file è l'overlay della sessione stessa e viene eliminato. Altrimenti `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` interrompe il ripristino e il journal viene conservato. |
| Assente, ora presente, eliminazione di sessione, journal arrivato a `ProcessStarted` | Il file è un sottoprodotto della sessione (OMSI è stato eseguito con il lease dell'installazione acquisito e per questo percorso era stato chiesto di rimanere assente). Viene eliminato e segnalato come voce di diagnostica `restore.session-artifact-removed` con lo SHA-256 del contenuto rimosso. |
| Assente, ora presente, eliminazione di sessione, processo mai avviato | Il file proviene dall'esterno della sessione. Viene conservato, segnalato come `OL_W_RESTORE_FOREIGN_FILE_RETAINED` con il relativo SHA-256, e la transazione si completa comunque. |
| Assente, ora presente, nessuna evidenza di proprietà (journal precedente all'impronta) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; il journal viene conservato. |
| Assente, ancora assente | Nessuna operazione necessaria. |

Dopo l'elaborazione di tutti i file, `VerifyRestoredSnapshots` rilegge ogni percorso: gli originali esistenti devono avere hash uguale a `Sha256`, i percorsi originariamente assenti devono essere assenti, a meno che non siano stati conservati esplicitamente. Solo allora `Restored` viene reso persistente, il journal eliminato (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` se sopravvive) e la directory di backup rimossa. Un crash tra `Restored` e l'eliminazione del journal causa solo una riesecuzione idempotente.

Le note di ripristino (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) compaiono come voci `LaunchDiagnostic` in `SessionStatus.Diagnostics` (con `sha256` in `Data`) e in `RecoveryStatus.Diagnostics`, quindi nulla viene rimosso o conservato in modo silenzioso.

<a id="originally-absent-files-and-ownership"></a>
### File originariamente assenti e proprietà

Un overlay scritto in un percorso che non esisteva viene rimosso al ripristino **solo se il suo contenuto corrisponde ancora a quanto applicato dalla sessione** (`AppliedSha256`). Se qualcos'altro lo ha sostituito durante la sessione, il ripristino fallisce con `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` e il journal viene conservato per l'ispezione.

Un percorso di eliminazione di sessione (destinazioni `.itx`, `Texture\standard.ipr`, `closecheck`) che non esisteva prima ma esiste dopo viene giudicato in base al fatto che OMSI sia stato eseguito sotto questa transazione: se il journal è arrivato a `ProcessStarted`, il file è un sottoprodotto della sessione e viene rimosso (`restore.session-artifact-removed`); se il processo non si è mai avviato, il file viene conservato e segnalato come `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, e il journal si completa comunque.

### `closecheck`

`closecheck` è il marcatore di crash di OMSI stesso (presente quando OMSI non si è chiuso correttamente). Si applicano due regole:

- Se esiste **prima** della sessione e `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` è `true` (il valore predefinito), viene rimosso in modo permanente prima dell'apertura della transazione e registrato come voce di diagnostica `closecheck.stale-removed` con il relativo SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` se l'eliminazione non riesce). Si tratta di una modifica permanente documentata, non di un partecipante alla transazione. Con il flag a `false` il marcatore rimane e OMSI mostra il proprio avviso.
- Se **non** esiste prima della sessione, `closecheck` viene aggiunto come eliminazione di sessione. Poiché la sessione termina con `TerminateProcess` (la routine di chiusura di OMSI non viene eseguita), il marcatore scritto da OMSI all'avvio è sempre ancora presente in seguito; viene rimosso al ripristino come artefatto di sessione.

<a id="early-recovery-order"></a>
## Ordine del recupero anticipato

A ogni `StartSessionAsync`, dopo l'acquisizione del lease dell'installazione e prima che qualsiasi componente legga l'installazione attiva:

1. Una transazione di solo recupero controlla la presenza di `journal.json`. Se presente, `RestorePendingAsync` viene eseguito immediatamente, in modo che gli overlay e la lingua della splash screen per la nuova sessione derivino dai file **originali**, mai dai residui di una sessione precedente.
2. Se tale recupero fallisce con `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (un journal precedente all'impronta), il recupero viene **rinviato**: la nuova sessione costruisce i propri overlay e la nuova transazione ritenta il recupero usando i propri byte pianificati come evidenza di proprietà (un file originariamente assente il cui contenuto è uguale al nuovo overlay viene accettato come di proprietà di OmsiLaunch). Qualsiasi altro errore di recupero fa fallire l'avvio.
3. Solo a questo punto viene validata la chiusura del plugin, viene calcolato l'hash di `Omsi.exe`, viene gestito `closecheck` e la nuova transazione viene preparata e applicata.

La traccia dell'host registra `PENDING_JOURNAL_RECOVERED` o `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Recupero dopo un crash e verifica dell'owner attivo

Il recupero non sostituisce mai i file sotto un OMSI in esecuzione. `RestorePendingAsync` rifiuta con `OL_E_INSTALLATION_BUSY` finché l'owner registrato nel journal è attivo:

| Contenuto del journal | Verifica di attività |
| --- | --- |
| `ProcessId` e `ProcessStartFileTimeUtc` registrati | Il processo con quel PID deve essere in esecuzione, la sua ora di avvio deve corrispondere (esclude il riutilizzo del PID) e, se `ExecutablePath` è registrato, il suo modulo principale deve essere quel percorso (un processo attivo non correlato non può trattenere la transazione). |
| Nessun PID, stato compreso tra `HandoffCreated` (incluso) e `ProcessExited` (escluso) | L'host è terminato tra `CreateProcess` e la scrittura del journal. Qualsiasi `Omsi.exe` il cui modulo principale sia `<root>\Omsi.exe` viene considerato l'owner. |
| Nessun PID, altri stati | Non attivo; il recupero procede. |

Il recupero esplicito è esposto come `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, che restituisce `RecoveryStatus(Pending, Recovered, Diagnostics)`; acquisisce prima il lease dell'installazione (`OL_E_INSTALLATION_BUSY` quando un altro owner lo detiene). Nella CLI, `/recovery-status` riporta lo stato senza ripristinare e `/recover` ripristina; il codice di uscita 8 (`TransactionRecoveryFailed`) viene restituito quando è stato richiesto un ripristino e il journal è ancora in sospeso al termine. Vedere [CLI](../reference/cli.md) e [API pubblica](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Ripristino rinviato al termine della sessione

Se il supervisore non riesce a confermare l'uscita di OMSI (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` o un guasto di pulizia segnalato come `OL_E_PROCESS_CLEANUP_FAILED`), la sessione fallisce con `OL_E_RESTORE_DEFERRED` e il journal viene deliberatamente conservato: sostituire i file dell'installazione mentre OMSI potrebbe ancora leggerli non è sicuro. L'avvio successivo (o `/recover`) esegue il ripristino una volta che il processo non esiste più. Un ripristino che fallisce per qualsiasi altro motivo termina la sessione con `OL_E_RESTORE_FAILED`; il journal rimane finché ogni originale di proprietà non è stato ripristinato e verificato.

<a id="the-installation-lease"></a>
## Il lease dell'installazione

Il lease (blocco esclusivo) è un semaforo con nome `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` con conteggio 1. La radice viene normalizzata da `InstallationLease.NormalizeRoot` (percorso completo, separatori finali rimossi tranne che per la radice di un'unità), quindi `C:\OMSI`, `C:\OMSI\` e `c:\omsi\sub\..` condividono un unico lease; il nome della pipe di controllo locale usa la stessa normalizzazione. Viene acquisito da `StartSessionAsync` (stato `AcquiringInstallationLock`) e da `RecoverPendingAsync`, e rilasciato quando il task del ciclo di vita della sessione si completa o la chiamata di recupero restituisce. `OL_E_INSTALLATION_BUSY` viene generato immediatamente quando non può essere acquisito (nessuna attesa).

Limitazioni accettate (documentate, nessuna modifica pianificata):

- Ambito `Local\`: un owner per installazione **per sessione di accesso**. Due utenti interattivi sulla stessa macchina non si escludono a vicenda.
- Un semaforo non viene rilasciato da un crash finché un qualsiasi altro processo detiene ancora un handle su di esso; a differenza di un mutex abbandonato, non ha un proprietario. Un detentore obsoleto lascia l'installazione in `OL_E_INSTALLATION_BUSY` finché quell'handle non viene chiuso.
- Qualsiasi processo dello stesso utente Windows può creare il nome per primo e detenerlo.

<a id="omsilaunch-directory"></a>
## Directory `.omsilaunch`

| Voce | Durata | Proprietario |
| --- | --- | --- |
| `journal.json` | Temporaneo; esiste solo mentre una transazione è in sospeso | Transazione |
| `backup\<sessionId>\*.bin` | Temporaneo; rimosso dopo il journal | Transazione |
| `diagnostics\<sessionId>-host.log` | Permanente; la conservazione mantiene le 50 sessioni più recenti (i file più vecchi con prefisso di sessione vengono eliminati all'avvio di una nuova sessione) | Traccia dell'host |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanente (stessa conservazione) | CLI |
| `diagnostics\tray-host.log` | Permanente | Host dell'area di notifica di Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Asset permanenti del prodotto; copiati una sola volta dal pacchetto, mai sovrascritti o rimossi | Asset visivi della sessione |
| `session-profiles\<id>\` | Permanente; installato dall'utente o dall'autore dei contenuti | Utente |
| `profiles\` | Non creato né letto dal codice attuale; riservato | nessuno |
| `docs\`, `examples\` | Permanente; forniti dal pacchetto di rilascio | Pacchetto |

Nessun dato lascia la macchina; la diagnostica è costituita solo da file locali. Vedere anche [directory `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Le modifiche runtime non sono registrate nel journal

Le operazioni di controllo runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, texture D3D) modificano solo lo stato in memoria di OMSI. Non vengono registrate nel journal e non vengono ripristinate; scompaiono con il processo. Vedere [controllo runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Modalità di errore e codici di errore

| Codice | Significato | Journal al termine |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease detenuto da un altro owner, oppure il processo OMSI registrato nel journal è ancora attivo | conservato |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Ripristino richiesto per una transazione con snapshot ma senza journal su disco | n/d |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | L'hash di un backup differisce dall'impronta dello snapshot; non è stato scritto nulla | conservato |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Un percorso di overlay originariamente assente contiene ora un contenuto che la sessione non ha scritto | conservato |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal precedente all'impronta con un percorso originariamente assente che ora esiste; solo una nuova sessione con byte pianificati identici può chiuderlo | conservato (rinviato) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | Non è stato possibile eliminare `journal.json` dopo un ripristino verificato | conservato (la riesecuzione è idempotente) |
| `OL_E_RESTORE_DEFERRED` | Uscita di OMSI non confermata; ripristino rinviato all'avvio successivo | conservato |
| `OL_E_RESTORE_FAILED` | Qualsiasi altro errore di ripristino (discrepanza di presenza o di hash dopo il ripristino, errore di I/O) | conservato |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Non è stato possibile eliminare un `closecheck` obsoleto prima della transazione | non ancora presente |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Avviso: un file estraneo in un percorso di eliminazione di sessione è stato conservato | completato |
| `OL_E_PLAN_NOT_RUNNABLE` | La ripianificazione all'avvio ha rilevato che la specifica non è più eseguibile (ad esempio un `Omsi.exe` modificato); non viene aperta alcuna transazione | nessuno |

La CLI mappa `OL_E_RECOVERY_*` e `OL_E_RESTORE_FAILED` sul codice di uscita 8 e `OL_E_INSTALLATION_BUSY` sul codice di uscita 7; vedere [codici di uscita](../reference/exit-codes.md).

<a id="evidence"></a>
## Evidenza

I test offline in `tools/OmsiLaunch.TestHost` coprono i percorsi della transazione: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Evidenza di runtime (matrice di validazione): RV-005 e RV-006 (overlay applicato e ripristino esatto byte per byte, sessioni `1e8e0548-...` e il batch di presentazione), RV-008 superato per l'uscita anticipata (sessione `0dc40570-...`).

Evidenza di runtime (round di chiusura runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- rimozione come artefatti di sessione delle destinazioni `.itx` con il downloader reale di OMSI, all'arresto normale e dopo l'interruzione dell'owner seguita da `/recover` (S-01, `I01`, `I02`);
- recupero anticipato prima della costruzione degli overlay (S-05, `S05`);
- ripristino dei metadati e dei file di sola lettura più pulizia dei backup (S-12, `S12a`, `S12b`);
- `/recover` sotto il lease, con un OMSI orfano e nella finestra precedente al PID (S-04, `S04`, `S04b`);
- errori di avvio e un ripristino fallito seguito da `/recover` (resto di RV-008, `SF01`, `SF02`, `F01`);
- preservazione di CP1252 (S-07, `C01`).

Il ramo di recupero rinviato per i journal precedenti all'impronta rimane validato solo offline. Vedere [stato della validazione a runtime](../status/runtime-validation-status.md).
