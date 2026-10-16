# Prima sessione

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Traduzione della [pagina originale in inglese](../../../getting-started/first-session.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina illustra passo per passo la prima sessione OMSI gestita con OmsiLaunch `0.1.0-beta4`: pianificazione senza avviare OMSI, avvio con flag espliciti, avvio con un profilo di sessione predefinito, controllo e arresto della sessione e ricerca della diagnostica al termine. Si presuppone che il pacchetto sia installato come descritto in [installazione](installation.md). Ogni flag è specificato nel [riferimento CLI](../reference/cli.md); altre invocazioni si trovano negli [esempi CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Che cosa fa una sessione

Una sessione è una transazione attorno a un processo OMSI: OmsiLaunch crea uno snapshot dei file che toccherà (per impostazione predefinita le due bitmap della splash screen sotto `GUI\`, più `options.cfg` quando vengono richiesti overlay `/set`), scrive un journal persistente sotto `.omsilaunch\`, applica gli overlay, avvia `Omsi.exe` con il plugin permanente, attende l'ingresso nel gameplay (`Running`), mantiene la sessione controllabile e, al termine, termina OMSI e ripristina byte per byte ogni file toccato. `/new` non seleziona mai in modo implicito una mappa o un punto di ingresso: entrambi devono essere indicati, oppure provenire da un file `/spec` o da un profilo di sessione.

<a id="1-plan-nothing-is-started"></a>
## 1. Pianificare (non viene avviato nulla)

Eseguire dalla radice di OMSI; l'installazione ha come valore predefinito la directory che contiene `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

Il piano deve riportare `"IsRunnable": true` (uscita `0`). Elenca `TouchedFiles` e `PlannedMutations`, in modo da mostrare esattamente che cosa la sessione sovrascriverà con gli overlay. Correggere qualsiasi voce di diagnostica `OL_E_` prima di proseguire; non è stato scritto nulla.

La spec di esempio inclusa nel pacchetto fa lo stesso con un file:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Avviare con flag espliciti

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Che cosa accade, in ordine:

1. Viene stampato il piano (`Plan: READY profile=Omsi23004` sul layout noto).
2. Recupero di un eventuale journal in sospeso precedente, acquisizione del lease, snapshot, journal, overlay, verifica dell'integrità del plugin, avvio di `Omsi.exe`.
3. Compare l'icona nell'area di notifica (`OmsiLaunch is running`); vedere [area di notifica di Windows](../reference/windows-tray.md).
4. All'ingresso nel gameplay, lo stato `Running` viene stampato come JSON (`"State": 14`). Il timeout di avvio predefinito è di 180 s (`/startup-timeout:<1..600>` per modificarlo).
5. La console resta collegata fino al termine della sessione. Non chiudere la finestra della console per arrestare: usare uno dei metodi di arresto descritti sotto.

Aggiunte facoltative per la prima esecuzione:

- `/set:graphics.maxFPS=60` (un overlay di `options.cfg`, ripristinato al termine);
- `/splash:Unset` per lasciare intatta la splash screen di OMSI, oppure `/splash-language:DEU` per scegliere la splash screen gestita localizzata;
- `/observe-seconds:30` per arrestare automaticamente 30 s dopo `Running` (utile per uno smoke test);
- `--json` per un output strutturato.

I flag che richiedono una data, un'ora, un anno, il meteo o un veicolo del giocatore (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) sono accettati ma non possono essere applicati da questo build: il piano diventa `NOT RUNNABLE` con `OL_E_CAPABILITY_UNAVAILABLE`. Ometterli.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Avviare con un profilo di sessione predefinito

Un profilo di sessione è un file YAML in `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` che fissa la mappa, il punto di ingresso e fino a 99 preset di impostazioni (schema `omsilaunch.session-profile/v1`; riferimento completo in [profili di sessione](../reference/session-profiles.md)). Creare `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Quindi:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Regole da ricordare: l'`id` deve essere uguale al nome della directory; l'indice è `1..99`; i flag espliciti che sovrascriverebbero un campo di proprietà del profilo (`/map`, `/entrypoint-index`, una chiave `/set` di proprietà del preset, i flag della splash screen quando il preset ha `presentation`) vengono rifiutati con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (uscita `2`); il blocco `new:` si applica solo con `/new`; con `/saved:<file.osn>` la mappa della situazione deve essere elencata sotto `compatibility.maps`. Il file `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` incluso nel pacchetto illustra lo schema completo, ma il suo blocco `new:` imposta `date`, `time` e `weather`, che questo build non può applicare; quindi copiarlo solo dopo aver rimosso tali chiavi.

<a id="4-control-the-running-session"></a>
## 4. Controllare la sessione in esecuzione

Da una seconda console nella stessa directory (senza argomento dell'installazione):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Questi comandi passano per la pipe di controllo locale di questa installazione ([controllo locale](../reference/local-control.md)); l'uscita `4` indica che qui non è in esecuzione alcun owner.

<a id="5-stop"></a>
## 5. Arrestare

Ciascuno di questi metodi termina la sessione allo stesso modo (OMSI viene terminato, poi ogni file toccato viene ripristinato, poi il journal e il backup vengono eliminati):

| Metodo | Note |
|---|---|
| Icona nell'area di notifica → `End session` → conferma | Disponibile sia nelle sessioni di `OmsiLaunch.exe` sia in quelle di `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | Da un'altra console; ritorna immediatamente, l'owner completa il ripristino. |
| Ctrl+C nella console dell'owner | Richiede l'arresto; l'owner attende il ripristino prima di uscire. |
| `/observe-seconds:<n>` | Arresto automatico `n` secondi dopo `Running`. |
| OMSI termina da solo | L'owner rileva `ProcessExited` ed esegue il ripristino. |

La routine di chiusura propria di OMSI non viene eseguita, quindi OMSI non riscrive `options.cfg` all'uscita; ciò è intenzionale, affinché il ripristino sia esatto. Chiudere la finestra della console dell'owner con il pulsante X concede al ripristino solo 4 s; se non è stato completato, l'avvio successivo (o `OmsiLaunch.exe /recover`) lo completa a partire dal journal. Il codice di uscita dell'owner è `0` quando la sessione è terminata in `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Dove guardare in seguito

| Posizione | Contenuto |
|---|---|
| Output della console / `--json` | Piano, stato `Running`, stato finale (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | La traccia dell'host della sessione (confini della transazione, avvio del processo, handoff del plugin, ingresso nel gameplay, ripristino). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Risultato di un'operazione `/runtime:` eseguita dall'owner. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Eventi dell'indicatore nell'area di notifica. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` dopo una conclusione pulita. `true` indica che è rimasto un journal; eseguire `OmsiLaunch.exe /recover`. |

Se la sessione non ha raggiunto il gameplay, lo stato finale riporta la voce di diagnostica `OL_E_` che ha causato l'errore (per esempio `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), il codice di uscita è `1` e i file sono stati comunque ripristinati. Vedere [codici di uscita](../reference/exit-codes.md), [errori](../reference/errors.md) e [limitazioni note](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Esecuzione senza console

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delega a `OmsiLaunchW.exe` e restituisce `0` immediatamente. La sessione non ha console; gli errori compaiono come finestre di messaggio e l'icona nell'area di notifica è l'unica superficie visibile. Usare `session status`, `events watch` e la directory di diagnostica per seguirla. Il comportamento completo dell'host Windows è descritto nel [riferimento di OmsiLaunchW.exe](../reference/omsilaunchw.md).
