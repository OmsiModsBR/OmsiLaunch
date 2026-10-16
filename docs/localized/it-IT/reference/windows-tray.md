# Indicatore nell'area di notifica di Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Traduzione della [pagina originale in inglese](../../../reference/windows-tray.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Ogni sessione owner autonoma (avviata con `OmsiLaunch.exe` o `OmsiLaunchW.exe`) mostra un'icona nell'area di notifica che segnala la sessione e consente all'utente di terminarla. Questa pagina specifica l'indicatore così come è implementato da `SessionTrayIndicator`, `StatusWindow` e `StopConfirmationWindow` in `tools\OmsiLaunch.Cli\WindowsHost.cs`, dal presenter di sola lettura `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) e dal registro delle stringhe `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), insieme alle finestre di dialogo di errore di `OmsiLaunchW.exe` gestite da `WindowsHost`. Il tray è soltanto un adattatore di presentazione: non possiede né OMSI né il recupero, e la sua azione di arresto segnala lo stesso percorso di arresto canonico dell'owner usato da `session stop` (vedere [riferimento CLI](cli.md) e [controllo locale](local-control.md)).

<a id="when-the-icon-exists"></a>
## Quando esiste l'icona

| Fase | Comportamento |
|---|---|
| Creazione | Subito dopo il ritorno di `StartSessionAsync`, prima che la sessione raggiunga `Running`, a meno che non sia impostato `SuppressTrayIcon`. L'icona esiste quindi durante `StartingProcess`, `WaitingForPlugin`, `StartingWorld` e `EnteringGameplay`. |
| Budget di avvio | Il thread dell'interfaccia (`STA`, in background, denominato `OmsiLaunch tray`) deve pubblicare l'icona entro 2 s. In caso contrario, o in presenza di qualsiasi eccezione durante la creazione, l'indicatore viene rilasciato e la sessione prosegue **senza** icona; viene registrato nel log `startup-timeout` o l'eccezione. Un avvio lento non lascia mai un'icona visibile orfana. |
| Rimozione | Nel blocco `finally` dell'owner, dopo che la sessione si è completata o è fallita e prima di `CloseAsync`. Il rilascio invia la richiesta di chiusura al thread dell'interfaccia (chiude il menu, la conferma e la finestra di stato, quindi termina il ciclo dei messaggi), attende il thread per un massimo di 2 s (se il limite viene superato, viene registrato `dispose-timeout`), nasconde e rilascia il `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (un campo di `LaunchSpec`, `Presentation.SuppressTrayIcon`, predefinito `false`). Impostabile tramite `/spec` e l'API; non esiste un flag della CLI. Gli integratori che mostrano un proprio elemento di interfaccia per la sessione lo impostano a `true`; nient'altro della sessione cambia. |
| Host | Sia `OmsiLaunch.exe` (console) sia `OmsiLaunchW.exe` (sottosistema Windows) mostrano l'icona; le sessioni di `OmsiLaunchW.exe` avviate con `/silent` non hanno alcun'altra superficie visibile. |

<a id="icon-and-tooltip"></a>
## Icona e tooltip

- Icona: l'icona associata all'eseguibile in esecuzione (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, l'icona di OmsiLaunch incorporata), con fallback su `SystemIcons.Application`.
- Testo del tooltip: `Tray.Running` (`OmsiLaunch is running`, cioè «OmsiLaunch è in esecuzione»). Il testo non cambia durante l'arresto (non esiste ancora una stringa «in arresto»; l'icona resta invariata finché l'owner non la rimuove).
- Gli stili visivi sono abilitati (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interazione

| Action | Risultato |
|---|---|
| Clic destro | Porta la finestra del tray in primo piano (necessario per i menu delle icone di notifica; senza questo passaggio il menu può ignorare i clic e non chiudersi mai mentre OMSI è in primo piano), quindi apre il menu contestuale nella posizione reale del cursore (`Cursor.Position`, non le coordinate dell'evento, perché `NotifyIcon` può riportare `(0,0)` per gli eventi ospitati dalla shell). Il menu viene vincolato all'area di lavoro dello schermo sotto il cursore. |
| Doppio clic | Apre la finestra di stato (come la voce di menu `Status`). |
| Clic sinistro | Nessuna azione. |
| Voce di menu `Status` (`Tray.Status`) | Apre (oppure attiva, se è già aperta) la finestra di stato di sola lettura. |
| Separatore | |
| Voce di menu `End session` (`Tray.EndSession`, descrizione accessibile `Tray.EndSessionDescription`) | Apre la finestra di dialogo di conferma per terminare la sessione. |

<a id="status-window-read-only-snapshot"></a>
### Finestra di stato (snapshot di sola lettura)

Viene aperta dalla voce di menu `Status` o da un doppio clic sull'icona (`SessionTrayIndicator.ShowStatus`). È una finestra di dialogo fissa, centrata, dimensionata automaticamente, senza voce nella barra delle applicazioni e con un solo pulsante `Close` (`Status.Close`, «Chiudi»; anche `Escape` la chiude). Scegliere `Status` quando la finestra è già aperta attiva quella finestra senza ricostruirla (mantiene lo snapshot della prima apertura). Un errore nella costruzione della finestra viene scritto in `tray-host.log`; non influisce sulla sessione.

**È uno snapshot (istantanea), non una vista in tempo reale.** `SessionStatusPresenter.Create(plan, status, ui)` viene eseguito una sola volta all'apertura della finestra: legge il `SessionPlan` risolto della sessione (la spec effettivamente pianificata) e un singolo `SessionStatus` (solo il suo `State`). Nulla viene aggiornato mentre la finestra resta aperta, e la finestra non interroga mai OMSI (nessuna operazione runtime, nessun valore di telemetria). Per vedere uno stato più recente, chiuderla e riaprirla.

Titolo e intestazione della finestra: lo stesso testo, `Status.SessionRunning` (`Session is running`, cioè «La sessione è in esecuzione») quando `SessionStatus.State` è `Running`, altrimenti il nome grezzo di `SessionState` (per esempio `WaitingForPlugin` se aperta durante l'avvio, dato che l'icona esiste prima di `Running`). `Status.Title` (`OmsiLaunch session status`) è definito nella tabella delle stringhe ma non è usato da questa release.

Le sezioni compaiono in questo ordine, e una sezione viene omessa quando non ha campi. Ogni valore proviene dal `LaunchSpec`/`SessionPlan` pianificato, mai da OMSI.

| Sezione (etichetta inglese) | Campo (etichetta inglese) | Mostrato quando | Valore | Origine (equivalente pubblico) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | sempre | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (qualsiasi altra modalità; mai raggiunto perché `LastMapState` non è eseguibile) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | il piano ha risolto un'identità di contenuto `map` | il `DisplayName` della mappa (il nome della directory della mappa, per esempio `Grundorf`), altrimenti il nome file dell'identità senza estensione | voce di `SessionPlan.ResolvedContent` con `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` restituisce lo stesso `DisplayName` |
| `Session` | `Situation` | `SavedSituation` con un'identità di situazione | nome file senza estensione (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | è stato richiesto un punto di ingresso | l'identità del punto di ingresso, se impostata (mai eseguibile in questa release), altrimenti l'indice presentato come intero (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | è stato usato un profilo di sessione (`/predefined-profile`) | `name` del profilo | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | come sopra, quando il preset ha un nome | `name` del preset | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | è stata richiesta una data, un'ora o un meteo esplicito/di sistema | `DD/MM/YYYY` o `System`; `HH:MM:SS` o `System`; codice ICAO, nome del preset o `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | è stato richiesto un campo del veicolo del giocatore | l'identità/il valore richiesto | `Spec.PlayerVehicle` |
| `Configuration` | un campo per ogni impostazione semantica | un'impostazione è stata definita (`/set`, `settings` del profilo, `LaunchSpec.Environment.*`) ed è nota a `ConfigurationCatalog` | il valore richiesto; `%` aggiunto per le chiavi che terminano con `Percent`, ` m` per le chiavi che terminano con `DistanceMeters`. L'etichetta è la chiave dell'impostazione con ogni parte separata da punto scritta con l'iniziale maiuscola (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; gli stessi valori sono le `PlannedMutations` del piano |
| `Presentation` | `Splash` | sempre | `Managed` o `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | sempre | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

La sezione `Environment` compare in una sessione Beta 4 in esecuzione quando è stata richiesta una data, un'ora, un anno o un meteo (tali richieste sono sperimentali, vedere [limitazioni note](known-limitations.md)). La sezione `Vehicle` non può mai comparire in una sessione in esecuzione: la richiesta di qualsiasi campo del veicolo del giocatore rende il piano non eseguibile, quindi nessuna sessione di questo tipo viene avviata. Esiste per un build futuro ed è coperta dal test offline del presenter.

Evidenza di runtime (interfaccia pt-BR, closure runtime `T01`): una sessione NEW_MAP su Grundorf ha mostrato `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Gli stessi dati sono disponibili agli strumenti senza il tray: `session status` tramite il [piano di controllo locale](local-control.md) fornisce `SessionId`, `State`, `Diagnostics` e `RuntimeEvents` (in tempo reale), e l'output `/plan --json` dell'owner (oppure `PlanSessionAsync`) fornisce la spec pianificata, i contenuti risolti e le modifiche pianificate che la finestra riassume.

<a id="end-session-with-confirmation"></a>
### Terminare la sessione (con conferma)

1. `StopConfirmationWindow`: titolo `End session?`, messaggio `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (OMSI 2 verrà chiuso e la sessione gestita da OmsiLaunch terminerà), pulsanti `End session` (predefinito, `DialogResult.OK`) e `Cancel` (`Escape`). Una seconda richiesta mentre la finestra di dialogo è aperta la attiva invece di sovrapporne un'altra.
2. Con `OK` il tray chiama `requestCanonicalStop`, che completa il segnale `controlStopped` dell'owner; l'owner chiama quindi `StopAsync`: OMSI viene terminato con `TerminateProcess` e ogni file di proprietà della sessione viene ripristinato. Il tray non termina mai OMSI direttamente.
3. Se la richiesta lancia un'eccezione, l'errore viene registrato nel log e viene mostrato `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`, cioè: non è stato possibile terminare la sessione; OMSI e la sua sessione gestita restano attivi).
4. Il tray non conferma il successo; l'icona scompare quando l'owner termina il ripristino e rilascia l'indicatore (closure runtime `T01`: l'owner è terminato 607 ms dopo la conferma di `End session`).
5. `Cancel` (o la chiusura della finestra di dialogo) non fa nulla: la sessione continua a essere eseguita (`T01`).
6. Un arresto che arriva da altrove (`session stop`, Ctrl+C, `/observe-seconds`, uscita di OMSI) mentre la finestra di stato o la finestra di dialogo di conferma è aperta le chiude come parte del rilascio dell'indicatore; l'owner non attende l'utente (`T02`: l'owner è terminato 725 ms dopo l'arresto tramite pipe con entrambe le finestre aperte).

<a id="explorer-restart"></a>
## Riavvio di Esplora risorse

`TrayWindow` è una finestra nativa nascosta che registra il messaggio di finestra `TaskbarCreated`. Quando Esplora risorse (la shell) si riavvia, trasmette quel messaggio e l'indicatore aggiunge nuovamente l'icona (`Visible = false; Visible = true`). Closure runtime `T01`: dopo che `explorer.exe` è stato terminato e riavviato da Windows, l'icona era di nuovo presente in `Shell_TrayWnd` e il menu e la finestra di stato continuavano a funzionare.

<a id="localization"></a>
## Localizzazione

`WindowsUiStrings.Resolve` segue la **cultura dell'interfaccia di Windows** (`CultureInfo.CurrentUICulture`), mai la lingua dei contenuti di OMSI né la lingua di un profilo di sessione. Ordine di risoluzione: nome esatto della cultura, poi lingua a due lettere, poi inglese. Ogni chiave ricade sull'inglese quando una traduzione non la contiene.

| Chiavi di cultura | Lingua |
|---|---|
| `en`, `en-US`, `en-GB` | Inglese (predefinita e di fallback) |
| `pt-BR` | Portoghese brasiliano. `pt-PT` (e `pt` da solo) ricade deliberatamente sull'inglese. |
| `de`, `de-DE` | Tedesco |
| `fr`, `fr-FR` | Francese |
| `pl`, `pl-PL` | Polacco |

Le stringhe localizzate coprono il tooltip, le due voci di menu, la finestra di stato (intestazione, titoli delle sezioni, etichette dei campi, `Close`), i valori di modalità e di presentazione e la finestra di dialogo di conferma. Il glossario è mantenuto in `docs\windows-ui-localization.md`; il test offline `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifica la risoluzione e il presenter.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Finestre di dialogo di errore di `OmsiLaunchW.exe`

Quando `OMSILAUNCH_WINDOWS_HOST=1` (impostato da `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` sostituisce l'output di errore sulla console con una finestra di messaggio modale intitolata `OmsiLaunch` (icona di errore): `<message>`, riga vuota, `Code: OL_E_...`, riga vuota, `See .omsilaunch\diagnostics for details.` Viene mostrata da ogni `CliInput.WriteError` (errori negli argomenti, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, eccezioni classificate), quando un piano di avvio non è eseguibile (fallback `The session plan is not runnable.`; audit della documentazione BUG-06) e quando la sessione non riesce a raggiungere `Running` (`The OMSI session did not reach gameplay.` con l'ultima diagnostica `OL_E_`, oppure `OL_E_SESSION_START_FAILED` quando non ce n'è alcuna). Il comportamento completo di `OmsiLaunchW.exe` è descritto nel [riferimento di OmsiLaunchW.exe](omsilaunchw.md). Con `OmsiLaunch.exe` la stessa funzione non fa nulla. Un errore di avvio dell'host .NET (codici dello shim `100`..`106`) viene mostrato dallo shim nativo stesso; vedere [codici di uscita](exit-codes.md).

<a id="log-location"></a>
## Posizione del log

`<root>\.omsilaunch\diagnostics\tray-host.log`, una riga per voce: timestamp UTC ISO-8601, un carattere di tabulazione, quindi la voce. Voci: `created`, `removed`, `startup-timeout`, `startup-cancelled` (un rilascio è entrato in race con l'avvio e il ciclo è stato saltato), `dispose-timeout` e i testi completi delle eccezioni per gli errori dell'interfaccia. La registrazione è best effort e non lancia mai eccezioni. I log dell'host di sessione (`<sessionId>-host.log`) vengono scritti dall'owner nella stessa directory; il log del tray non ha il prefisso della sessione e non viene eliminato dalla conservazione delle ultime 50 sessioni.

<a id="lifecycle-guarantees"></a>
## Garanzie del ciclo di vita

- Il tray non possiede mai la sessione: non può avviare OMSI, non può ripristinare file e non può aggirare il percorso di arresto dell'owner.
- Ogni percorso di uscita dell'owner (completamento normale, uscita di OMSI, Ctrl+C, chiusura della console, arresto tramite pipe, eccezione, `/observe-seconds`) rilascia l'indicatore prima di `CloseAsync`, quindi nessuna icona sopravvive alla propria sessione, tranne quando il processo owner viene terminato bruscamente (Windows rimuove le icone orfane al successivo passaggio del mouse).
- Creazione e rilascio sono serializzati sotto un lock: un rilascio che vince la race fa sì che il thread dell'interfaccia salti il proprio ciclo dei messaggi ed esegua subito la pulizia.
- Tutto il lavoro di Windows Forms avviene sul thread STA dedicato; i thread esterni si limitano a inviargli messaggi tramite un controllo di marshalling nascosto.

<a id="residual-caveats-from-the-code-comments"></a>
## Avvertenze residue (dai commenti del codice)

- Non esiste un testo di tooltip «in arresto»; l'icona riporta `OmsiLaunch is running` finché non viene rimossa.
- `NotifyIcon` può riportare coordinate del mouse `(0,0)` per gli eventi ospitati dalla shell; viene invece letta la posizione del cursore.
- I messaggi del tray continuano ad arrivare mentre la finestra di dialogo di conferma è modale; una seconda conferma non viene sovrapposta.
- Se il thread dell'interfaccia non termina entro il budget di rilascio di 2 s, l'owner prosegue senza attendere (`dispose-timeout`).
- La finestra di stato è uno snapshot dei valori pianificati acquisito all'apertura; non viene aggiornata e non legge mai OMSI.

<a id="runtime-evidence"></a>
## Evidenza di runtime

Osservata in sessioni reali sull'installazione autorizzata durante il ciclo di closure runtime (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, interfaccia di Windows pt-BR; vedere lo [stato della validazione a runtime](../status/runtime-validation-status.md)):

- L'icona viene registrata nella reale area di notifica (`Shell_TrayWnd`) con `OmsiLaunchW.exe` e rimossa dopo il ripristino (`T01`..`T04`).
- «End session» con conferma avvia l'arresto canonico e il ripristino esatto; `Cancel` mantiene la sessione in esecuzione (`T01`, `T03`).
- L'icona viene ricreata dopo un riavvio di Esplora risorse (`TaskbarCreated`, `T01`).
- Le finestre di stato e di conferma vengono chiuse dall'owner quando arriva un arresto mentre sono aperte (`T02`).
- Finestre di dialogo di errore di `OmsiLaunchW.exe` per un errore negli argomenti (`OL_E_INVALID_ARGUMENT`), per l'assenza di una sessione attiva (`OL_E_NO_ACTIVE_SESSION`) e per una sessione che fallisce prima del gameplay (`OL_E_WORLD_START_FAILED`) (`T04`). In quest'ultimo caso la finestra di dialogo mostra come messaggio il payload di errore del plugin.
- `/silent` si scollega: il launcher ritorna mentre l'host Windows mantiene la sessione (`T04`).
- Il budget `ProcessExit` di 4 s alla chiusura della console (`L04`, owner su console).

Non prodotte: le finestre di dialogo dei codici di uscita dello shim di bootstrap (`100`..`106`).
