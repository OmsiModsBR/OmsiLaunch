# Riferimento di OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Traduzione della [pagina originale in inglese](../../../reference/omsilaunchw.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

`OmsiLaunchW.exe` è l'host del controller OmsiLaunch per il sottosistema Windows (GUI). Accetta la stessa riga di comando di `OmsiLaunch.exe` ed esegue lo stesso codice del controller (`OmsiLaunch.Controller.dll`). L'unica differenza è il modo in cui comunica l'esito: non c'è alcuna finestra della console, gli errori vengono mostrati come finestre di messaggio e una sessione in esecuzione è visibile solo tramite la sua [icona nell'area di notifica](windows-tray.md).

Fonti autorevoli: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (lo shim nativo), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) e `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Confronto tra OmsiLaunch.exe e OmsiLaunchW.exe

| Aspetto | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Sottosistema | Console. Apre una finestra della console quando viene avviato da Esplora risorse. | Windows (GUI). Nessuna finestra della console. |
| Shim nativo | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Ambiente | invariato | imposta `OMSILAUNCH_WINDOWS_HOST=1` per il processo del controller prima dell'avvio di .NET |
| Argomenti | suddivisi in token con `CommandLineToArgvW` e passati invariati al controller | lo stesso, quindi entrambi gli host accettano esattamente gli stessi flag e comandi ([riferimento CLI](cli.md)) |
| Output testuale | scritto su stdout | soppresso, a meno che non sia indicato `--json` (in tal caso gli envelope JSON vengono scritti su stdout, che un chiamante può reindirizzare) |
| Errori | `OL_E_...: message` o un envelope di errore JSON | le stesse regole per l'output della console, **più** una finestra di messaggio per ogni errore (vedere [Finestre di dialogo di errore](#failure-dialogs)) |
| `/silent` | avvia `OmsiLaunchW.exe` con gli altri argomenti e restituisce `0` | ignorato: il comando viene già eseguito nell'host Windows |
| Icona nell'area di notifica | mostrata per una sessione owner | mostrata per una sessione owner |
| Percorsi di arresto | area di notifica, `session stop`, uscita da OMSI, `/observe-seconds`, Ctrl+C, chiusura della console | area di notifica, `session stop`, uscita da OMSI, `/observe-seconds` (non c'è console, quindi Ctrl+C e la chiusura della console non si applicano) |
| Codici di uscita | [`PublicExitCode`](exit-codes.md) `0`..`10`, codici dello shim `100`..`106` | gli stessi codici |

<a id="how-it-starts"></a>
## Come si avvia

1. Lo shim risolve il proprio percorso (`GetModuleFileNameW`) e si aspetta `OmsiLaunch.Controller.dll` nella stessa directory.
2. Suddivide la riga di comando in token (`CommandLineToArgvW`) e imposta `OMSILAUNCH_WINDOWS_HOST=1`.
3. Individua `hostfxr` tramite il `nethost.dll` incluso nel pacchetto, lo carica, inizializza il controller con gli argomenti (il percorso del controller non fa parte dell'elenco di argomenti visto dal parser della CLI) e lo esegue.
4. Lo shim restituisce invariato il codice di uscita del controller.

Se uno qualsiasi dei passaggi precedenti all'esecuzione del controller non riesce, lo shim mostra una finestra di messaggio intitolata `OmsiLaunch` con il testo `OmsiLaunch could not start the .NET host (code N).` ed esce con quel codice:

| Codice | Passaggio non riuscito |
| --- | --- |
| `100` | non è stato possibile risolvere il percorso dell'eseguibile |
| `101` | non è stato possibile suddividere in token la riga di comando |
| `102` | la ricerca della posizione di `hostfxr` non è riuscita (di solito: il runtime .NET 6 x64 non è installato) |
| `103` | non è stato possibile recuperare il percorso di `hostfxr` |
| `104` | non è stato possibile caricare `hostfxr` |
| `105` | mancano export di `hostfxr` necessari |
| `106` | non è stato possibile inizializzare l'host gestito (ad esempio manca `OmsiLaunch.Controller.dll` o la sua configurazione di runtime, oppure il runtime Windows Desktop non è presente) |

`OmsiLaunch.exe` usa la stessa tabella ma non stampa nulla. Queste finestre di dialogo non sono state prodotte a runtime (vedere [stato della validazione a runtime](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Avvio

Avvio diretto, da un collegamento, da uno script o da un altro programma:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Tramite `OmsiLaunch.exe` con `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Collocare gli eseguibili nell'installazione di OMSI 2 (la struttura del pacchetto, vedere [pacchetti](packaging.md)); senza argomento di installazione, l'installazione è la directory che contiene l'eseguibile. Un'installazione esplicita viene passata come primo argomento, come con `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Poiché si tratta di un programma GUI, `cmd.exe` ed Esplora risorse non ne attendono la conclusione. Per attenderla e leggere il codice di uscita da uno script, usare `start /wait OmsiLaunchW.exe ...` in `cmd.exe` oppure `Start-Process -Wait -PassThru` in PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delega con /silent

`OmsiLaunch.exe ... /silent` (o `--silent`), quando non è già in esecuzione sotto `OmsiLaunchW.exe`, esegue le operazioni seguenti (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Cerca `OmsiLaunchW.exe` nella directory di `OmsiLaunch.exe`. Se manca: `OL_E_WINDOWS_HOST_MISSING`, uscita `7`.
2. Avvia `OmsiLaunchW.exe` tramite `ShellExecute` (`UseShellExecute = true`) con la directory corrente e tutti gli argomenti tranne `/silent`/`--silent`, nell'ordine originale. `ShellExecute` non passa gli handle del chiamante al nuovo processo, quindi un chiamante che cattura l'output di `OmsiLaunch.exe /silent` non resta bloccato per tutta la durata della sessione (chiusura di runtime BUG-03). Se non viene restituito alcun processo: `OL_E_WINDOWS_HOST_START_FAILED`, uscita `7`.
3. Scrive l'envelope `silent` ed esce immediatamente con `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

L'uscita `0` significa soltanto che `OmsiLaunchW.exe` è stato avviato. L'esito della sessione (errori di pianificazione, un owner già attivo, un avvio non riuscito) viene comunicato da `OmsiLaunchW.exe` con le proprie finestre di dialogo e diagnostiche, e da quel momento i due processi sono indipendenti: `OmsiLaunch.exe` è terminato e `OmsiLaunchW.exe` è il proprietario della sessione (owner). Usare `OmsiLaunch.exe session status` per vedere la sessione.

`/silent` viene applicato prima di ogni altro comando, quindi anche `OmsiLaunch.exe /silent session status` esegue `session status` all'interno di `OmsiLaunchW.exe`, dove il suo output viene soppresso. Usare `/silent` solo per gli avvii.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Effetto di ogni comando sotto OmsiLaunchW.exe

| Riga di comando | Risultato |
| --- | --- |
| nessun argomento | esegue `detect` in modo silenzioso ed esce con `0` (non viene mostrato nulla) |
| un avvio (`/new`, `/saved:...`, `/spec:...`, un profilo di sessione) con un piano eseguibile e nessun owner | diventa il proprietario della sessione: icona nell'area di notifica durante l'avvio e l'esecuzione; esce quando la sessione termina (`0` completata, `1` non riuscita) |
| un avvio il cui piano non è eseguibile | finestra di dialogo con l'ultima diagnostica `OL_E_` del piano (in alternativa `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), uscita `1` (audit della documentazione BUG-06; prima della correzione usciva senza segnalazioni) |
| un avvio mentre un owner è già attivo per l'installazione | finestra di dialogo `OL_E_SESSION_ALREADY_ACTIVE`, uscita `7`; la sessione in esecuzione non viene influenzata |
| un avvio che non raggiunge `Running` | finestra di dialogo `The OMSI session did not reach gameplay.` con l'ultima diagnostica `OL_E_`, uscita `1`. OMSI viene terminato e i file vengono ripristinati dal supervisore della sessione mentre la finestra di dialogo è aperta; il processo esce dopo la chiusura della finestra di dialogo e il completamento di `CloseAsync` |
| un argomento non valido, un flag sconosciuto o un profilo di sessione non valido | finestra di dialogo con `OL_E_INVALID_ARGUMENT` o il codice `OL_E_SESSION_PROFILE_*`, uscita `2` |
| un comando client (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) senza owner | finestra di dialogo `OL_E_NO_ACTIVE_SESSION`, uscita `4` |
| un comando client rifiutato dall'owner | finestra di dialogo con il codice di rifiuto (ad esempio `OL_E_CONTROL_SESSION_MISMATCH` o un codice di errore runtime), uscita `7` (`2` per un'operazione sconosciuta o un argomento mancante) |
| un comando client o di individuazione riuscito (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | nessun output visibile, a meno che non sia indicato `--json` e stdout sia reindirizzato; codice di uscita come per `OmsiLaunch.exe` |
| `/plan` o `/validate` | nessuna finestra di dialogo, nemmeno per un piano non eseguibile; uscita `0` o `1` |
| qualsiasi altro errore del controller | finestra di dialogo con il codice `OL_E_` classificato; codice di uscita secondo i [codici di uscita](exit-codes.md) |

<a id="failure-dialogs"></a>
## Finestre di dialogo di errore

Ogni errore che `OmsiLaunch.exe` stamperebbe viene mostrato anche come finestra di messaggio modale (`WindowsHost.ShowFailure`), anche quando è indicato `--json`:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` è il messaggio di errore oppure, per un errore della sessione, il messaggio dell'ultima diagnostica `OL_E_`. Limitazione nota: per un errore segnalato dal plugin, il messaggio è il payload di errore grezzo del plugin (ad esempio `{"name":"world.failed",...}`); la riga `Code:` è corretta (vedere [limitazioni note](known-limitations.md)). La finestra di dialogo è modale e il processo esce dopo la sua chiusura. Evidenza di runtime: errore di argomento, nessuna sessione attiva ed errore prima del gameplay (chiusura di runtime `T04`).

<a id="session-tray-and-exit"></a>
## Sessione, area di notifica e uscita

Una sessione di proprietà di `OmsiLaunchW.exe` si comporta esattamente come una di proprietà di `OmsiLaunch.exe` (vedere [ciclo di vita della sessione](../concepts/session-lifecycle.md)):

- L'icona nell'area di notifica compare non appena la sessione è stata avviata, prima che OMSI raggiunga il gameplay, a meno che in un file `/spec` non sia impostato `Presentation.SuppressTrayIcon`. Con `SuppressTrayIcon` non c'è alcuna superficie visibile; arrestare la sessione con `OmsiLaunch.exe session stop` o chiudendo OMSI.
- La sessione termina quando OMSI esce, quando `End session` viene confermato nell'area di notifica, quando un client invia `session stop` o quando trascorre il tempo di `/observe-seconds`. OmsiLaunch quindi termina OMSI se è ancora in esecuzione, ripristina ogni file che ha modificato, rilascia il lease dell'installazione, rimuove l'icona dall'area di notifica ed esce.
- Se lo stesso `OmsiLaunchW.exe` viene terminato forzatamente, il successivo avvio di OmsiLaunch per quell'installazione recupera la transazione in sospeso (vedere [transazioni e recupero](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Avvio rapido

1. Installare il pacchetto nella directory di OMSI 2 ([installazione](../getting-started/installation.md)).
2. Creare un collegamento a `OmsiLaunchW.exe` con gli argomenti della sessione, ad esempio `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Avviarlo. OMSI si avvia senza finestra della console; l'icona di OmsiLaunch compare nell'area di notifica.
4. Fare clic con il pulsante destro sull'icona → `Status` per vedere la sessione, oppure `End session` → `End session` per terminarla.
5. Se qualcosa va storto, la finestra di dialogo mostra il codice di errore; i dettagli si trovano in `<OMSI_PATH>\.omsilaunch\diagnostics`.
