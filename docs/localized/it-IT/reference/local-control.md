# Piano di controllo locale

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Traduzione della [pagina originale in inglese](../../../reference/local-control.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Il piano di controllo locale è l'endpoint basato su named pipe attraverso il quale un owner OmsiLaunch in esecuzione (il processo che ha avviato una sessione) accetta comandi di sessione semantici da altri processi sulla stessa macchina: stato, eventi, arresto e operazioni runtime pubbliche. Questa pagina specifica l'endpoint così come è implementato in `tools\OmsiLaunch.Cli\LocalControlPlane.cs` e il gestore delle richieste in `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): denominazione della pipe, framing, versione del protocollo, comandi, associazione alla sessione, codici di errore, modello di fiducia, finestra di disponibilità e modalità di comunicazione da altri strumenti. I comandi del client CLI (`session status`, `session stop`, `events read`, `events watch`, route come `time get`) sono sottili wrapper su questo protocollo; vedere il [riferimento CLI](cli.md). Le operazioni runtime vere e proprie sono specificate in [controllo runtime](runtime-control.md); l'alternativa nel processo per gli integratori è l'[API pubblica](public-api.md).

<a id="summary"></a>
## Riepilogo

| Proprietà | Valore |
|---|---|
| Trasporto | Named pipe di Windows, `PipeDirection.InOut`, modalità byte, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, buffer di ingresso e di uscita pari a un frame (65,540 byte) |
| Nome della pipe | `OmsiLaunch.Control.0.1.<key>` dove `<key>` sono i primi 16 caratteri esadecimali dello SHA-256 calcolato sui byte UTF-8 del percorso completo della radice dell'installazione, senza il `\` finale, in maiuscolo (`LocalControlPlane.PipeNameFor`) |
| Framing | Prefisso di lunghezza `int32` little-endian da 4 byte seguito da altrettanti byte di JSON UTF-8; una richiesta e una risposta per connessione |
| Messaggio massimo | 65,536 byte per una richiesta e per una risposta (`MaxMessageBytes`) |
| Versione del protocollo | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); a una versione non corrispondente si risponde con `OL_E_CONTROL_PROTOCOL` |
| Comandi | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Associazione alla sessione | `session.stop` e `runtime.execute` richiedono che `Arguments.session_id` sia uguale all'id della sessione attiva |
| Disponibilità | Dal momento in cui la sessione ha raggiunto `Running` (dopo un eventuale batch di validazione) finché la sessione non raggiunge `Completed` o `Failed` e l'owner rilascia l'endpoint |
| Ambito | Un endpoint per radice dell'installazione; due installazioni dello stesso utente non condividono mai una pipe |
| Stabilità | STABLE_BETA (test offline in `OmsiLaunch.WindowsUiTests`; evidenza di runtime: RV-003, richiesta di stato concorrente durante una chiamata nativa di diversi secondi, Round A RA-008, e la batteria di frame grezzi della chiusura runtime `R04`: 17 frame malformati, sovradimensionati, con protocollo errato, con comando sconosciuto, non associati e di una sessione errata, a ciascuno dei quali è stato risposto con il relativo errore tipizzato, seguiti da un `session.status` riuscito; un client bloccato non blocca gli altri; un arresto associato durante uno spawn in corso termina la sessione) |

<a id="pipe-name-derivation"></a>
## Derivazione del nome della pipe

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` è l'unica definizione dell'identità dell'installazione, usata anche dal lease dell'installazione (blocco esclusivo): risolve il percorso completo (inclusi i segmenti `.` e `..`, `/` o `\`, separatori ripetuti) e rimuove i separatori finali tranne che per la radice di un'unità. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` e `d:\omsi 2` producono lo stesso nome; `D:\OMSI 2` ed `E:\OMSI 2` producono nomi diversi. Il client deriva sempre il nome dall'installazione che contiene l'eseguibile in esecuzione (`AppContext.BaseDirectory`), a meno che il chiamante dell'API non passi una radice esplicita. Lo `0.1` contenuto nel nome è la versione del protocollo, così che un protocollo futuro possa coesistere sulla stessa macchina.

<a id="framing-and-encoding"></a>
## Framing e codifica

- Scrittura: serializzare il record con le opzioni predefinite di `System.Text.Json`, verificare `length <= 65536` (altrimenti `OL_E_CONTROL_MESSAGE_TOO_LARGE`), scrivere `BitConverter.GetBytes((int)length)` (4 byte, little-endian su Windows), scrivere il payload, eseguire il flush.
- Lettura: leggere esattamente 4 byte; rifiutare `length < 0` o `length > 65536` con `OL_E_CONTROL_MESSAGE_INVALID`; leggere esattamente `length` byte; deserializzare. Una connessione che si chiude prima che sia stato letto un frame completo viene scartata silenziosamente dall'owner (nessuna risposta). A un frame rifiutato viene risposto anche quando il client ha già scritto più byte di quanti l'owner ne legga: i buffer della pipe contengono un frame intero, quindi la scrittura del client si completa e il client può leggere la risposta (chiusura runtime BUG-02; prima della correzione un tale client e l'owner restavano entrambi bloccati nelle rispettive scritture).
- L'owner scrive ogni risposta entro una scadenza di 5 s: un client che si connette, invia una richiesta e non legge mai la risposta non può trattenere un task dell'owner più a lungo.
- I nomi delle proprietà della richiesta sono **in PascalCase e sensibili alle maiuscole** (`ProtocolVersion`, `Command`, `Arguments`), perché l'owner deserializza con le opzioni predefinite. Le risposte usano `Ok`, `Result`, `ErrorCode`, `Message`. I valori enum sono serializzati come interi; i `Guid` come stringhe.
- Una richiesta per connessione: l'owner legge una richiesta, scrive una risposta e chiude la pipe. Aprire una nuova connessione per ogni richiesta.

<a id="request"></a>
### Richiesta

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` deve essere l'id restituito da `session.status` per la sessione in esecuzione (il valore sopra proviene da una sessione reale).

`Arguments` è facoltativo (`null` oppure omesso) per `session.status` e `session.events`. Le chiavi degli argomenti sono confrontate in modo ordinale (sensibile alle maiuscole).

<a id="response"></a>
### Risposta

Frame acquisiti da un owner reale (chiusura runtime `R04`, pacchetto finale). Un `runtime.execute` riuscito:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Un `session.stop` rifiutato perché privo del `session_id` attivo:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Comandi

| Comando | Argomenti | Risultato con `Ok=true` | Note |
|---|---|---|---|
| `session.status` | nessuno | `SessionStatus`: `SessionId` (GUID come stringa), `State` (intero `SessionState`; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (array di `{Code, Message, Data}`), `RuntimeEvents` (array) | Sola lettura. I client lo leggono per primo per ottenere il `SessionId`. |
| `session.events` | nessuno | Array di `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (`int64` monotono), `Data` (mappa di stringhe) | Sola lettura, elenco limitato; `events watch` lo interroga periodicamente e stampa le voci con un `Sequence` superiore all'ultimo visto. |
| `session.stop` | `session_id` (obbligatorio) | `{"accepted": true, "session_id": "..."}` | Richiede l'arresto canonico (`TerminateProcess` su OMSI, poi ripristino). Ritorna immediatamente; lo stato della sessione è osservabile tramite `session.status` finché l'endpoint non scompare. |
| `runtime.execute` | `operation` (obbligatorio), `session_id` (obbligatorio), più gli argomenti propri dell'operazione (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (assegnato dall'owner, a partire da `50001`), `Succeeded`, `ErrorCode`, `Values` (mappa di stringhe) | Validato con `PublicCapabilityRegistry.ValidateRuntimeArguments` prima dell'esecuzione: operazione sconosciuta o interna → `OL_E_RUNTIME_OPERATION_UNKNOWN`; valore obbligatorio mancante → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout di 8 s, 30 s per `road-vehicles.spawn`. A un fallimento runtime si risponde con `Ok=false`, con `Result` impostato al `RuntimeCommandResult`, `ErrorCode` al relativo codice e `Message` pari a `Runtime operation was rejected.` Le chiavi del risultato che iniziano con `internal_` o terminano con `_address`, `_pointer`, `_vmt` vengono rimosse dall'API prima di raggiungere la pipe. |
| qualsiasi altro | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

L'owner serve le connessioni in modo concorrente (ogni client accettato è servito su un proprio task), quindi `session.status` continua a rispondere mentre è in corso una chiamata nativa di diversi secondi come `road-vehicles.spawn`.

<a id="session-id-binding"></a>
## Associazione dell'id di sessione

I comandi che modificano lo stato devono indicare la sessione su cui agiscono. La CLI implementa `TryRequestBoundAsync`: invia `session.status` (750 ms), prende `Result.SessionId` e ripete la richiesta aggiungendo `session_id` ad `Arguments`. A un id mancante, non analizzabile o diverso si risponde con `OL_E_CONTROL_SESSION_MISMATCH`. Se l'owner non riporta un id di sessione, il client segnala `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Metadati della risposta e cronologia degli eventi troncata

`Metadata` è `null` a meno che la risposta non contenga un'informazione su sé stessa. `session.status` e `session.events` restituiscono la cronologia degli eventi dell'owner (al massimo 256 eventi, il più recente per ultimo). Quando tale cronologia non entra in un frame da 64 KiB, gli eventi più vecchi vengono rimossi finché non entra e `Metadata` lo dichiara:

| Chiave | Significato |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Eventi in questa risposta |
| `events_dropped_count` | Eventi più vecchi rimossi per far entrare la risposta nel frame |
| `events_available_count` | Eventi detenuti dall'owner per questa risposta |
| `events_first_returned_sequence` | `Sequence` del primo evento restituito |

Senza `Metadata`, la risposta non è troncata dal piano di controllo. `Sequence` è monotono in entrambi i casi, quindi un client può anche rilevare gli eventi che l'owner stesso ha eliminato oltre la propria cronologia di 256 eventi. La CLI stampa questi metadati nell'envelope `--json` e una nota in modalità testo.

<a id="error-codes"></a>
## Codici di errore

| Codice | Origine | Significato |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | owner / client | Il `ProtocolVersion` della richiesta non è `0.1`; la risposta dell'owner non è stata decodificata o era vuota; l'owner ha chiuso la connessione senza rispondere; la connessione si è interrotta dopo essere stata stabilita; oppure l'owner non ha riportato alcun id di sessione. |
| `OL_E_CONTROL_MESSAGE_INVALID` | owner / client | Prefisso di lunghezza fuori intervallo (un frame di richiesta sovradimensionato viene rifiutato in questo modo prima che il suo payload venga letto), frame vuoto, JSON `null`, richiesta senza `Command`, oppure JSON che non è stato possibile decodificare. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | scrittura del client | La richiesta del client stesso supera 65,536 byte; l'errore viene segnalato al client e non viene inviato nulla. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | scrittura dell'owner | La risposta dell'owner supera 65,536 byte; l'owner risponde con questo errore tipizzato. `session.status` e `session.events` riducono la propria cronologia degli eventi (a partire dai più vecchi) per farla entrare, quindi non lo producono. |
| `OL_E_CONTROL_SESSION_MISMATCH` | owner | `session.stop` / `runtime.execute` senza il `session_id` attivo. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | owner | Nome di comando non supportato. |
| `OL_E_CONTROL_HANDLER_FAILED` | owner | Non è stato possibile serializzare la risposta del gestore, oppure il gestore ha lanciato un'eccezione senza un codice `OL_E_` nel messaggio; se il codice è presente, viene restituito quel codice (per esempio `OL_E_SESSION_NOT_RUNNING` da `ExecuteRuntimeAsync` dopo l'uscita di OMSI). |
| `OL_E_CONTROL_FAILED` | client CLI | Codice predefinito quando una risposta è `Ok=false` senza `ErrorCode`. |
| `OL_E_TIMEOUT` | client | Il client era connesso a un owner che non ha risposto entro il timeout. |
| `OL_E_NO_ACTIVE_SESSION` | client CLI | Non è stato possibile raggiungere alcun endpoint di un owner (`TryRequestAsync` restituisce `null` solo quando fallisce la connessione stessa). Una volta connessi, ogni fallimento è uno dei codici tipizzati sopra. Uscita `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | client e owner | Validazione della superficie pubblica di `runtime.execute` (uscita `2` nella CLI). |

A ogni fallimento noto si risponde con un errore tipizzato, così che un client non scambi mai un guasto per l'assenza di un owner. Due casi terminano senza un frame di risposta: un client che si disconnette prima di inviare una richiesta completa (non c'è nessuno a cui rispondere) e una richiesta ancora in corso quando la sessione termina (l'owner annulla le risposte in sospeso mentre chiude l'endpoint; il client vede la fine dello stream, che il client CLI segnala come `OL_E_CONTROL_PROTOCOL` «The owner closed the connection without a reply»). Un client che ha inviato una richiesta e si disconnette prima di leggere la risposta non ha effetti sull'owner. L'avvio di un owner si rifiuta di procedere quando una qualsiasi risposta (incluso un errore tipizzato) proviene dall'endpoint dell'installazione, perché una risposta dimostra che un owner è presente. La CLI associa le risposte ai codici di uscita come descritto in [codici di uscita](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Modello di fiducia (rischio accettato)

- `PipeOptions.CurrentUserOnly` limita la pipe all'utente Windows (e al livello di integrità) proprietario della sessione; il lato client della stessa opzione verifica che il server appartenga allo stesso utente.
- **Qualsiasi processo eseguito con lo stesso utente Windows può leggere lo stato, arrestare la sessione ed eseguire operazioni runtime pubbliche.** Non esiste alcuna autenticazione aggiuntiva, token o autorizzazione per singolo client. Si tratta di un rischio documentato e accettato per la beta; non eseguire sessioni OmsiLaunch con un account condiviso con software non attendibile.
- La pipe non espone mai indirizzi nativi, handle od operazioni sulla memoria grezza; sono accettati solo gli id di operazione pubblici di `PublicCapabilityRegistry`, e le primitive di ricerca interne (`internal.*`) vengono rifiutate prima di qualsiasi ricerca della sessione.
- I messaggi sono limitati (64 KiB) e suddivisi in frame; a un frame malformato o sovradimensionato si risponde, oppure viene scartato, senza effetti sulla sessione.
- Se il nome della pipe è già posseduto da un altro processo quando l'owner inizia l'ascolto (`IOException`/`UnauthorizedAccessException` alla creazione), l'owner continua a eseguire la sessione senza endpoint e registra il guasto in `LocalControlPlane.ListenFault`; i client vedono allora `OL_E_NO_ACTIVE_SESSION`. Usare l'icona nell'area di notifica o Ctrl+C per terminare una sessione di questo tipo.

<a id="availability-window"></a>
## Finestra di disponibilità

1. `StartSessionAsync` è in esecuzione; l'endpoint non esiste ancora. Un secondo avvio di `OmsiLaunch.exe` per la stessa radice interroga `session.status` per 250 ms prima di avviarsi e fallisce con `OL_E_SESSION_ALREADY_ACTIVE` solo quando un owner risponde già; due owner in competizione durante l'avvio vengono serializzati dal lease dell'installazione (`OL_E_INSTALLATION_BUSY`).
2. La sessione raggiunge `Running`; i batch di validazione (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), se richiesti, vengono completati.
3. `LocalControlPlane.Start()`: l'endpoint accetta connessioni. L'eventuale operazione `/runtime:` dell'owner stesso viene eseguita dopo questo punto.
4. L'endpoint resta attivo durante `ProcessExited`, `Restoring` e `CleaningRuntime` (lo stato riporta questi valori), finché la sessione non è `Completed` o `Failed`.
5. `DisposeAsync` annulla il listener, attende i task dei client in corso e l'owner chiama `CloseAsync`. Da quel momento il nome della pipe non esiste più.

I client dovrebbero usare timeout di connessione brevi (la CLI usa 750 ms per status/stop/events) e interpretare «nessun endpoint» come «nessuna sessione attiva per questa installazione».

<a id="using-the-protocol-from-other-tooling"></a>
## Uso del protocollo da altri strumenti

<a id="c-net-6-or-later"></a>
### C# (.NET 6 o successivo)

```csharp
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Same result as OmsiLaunch.Api.InstallationPaths.IdentityKey for any root that is not a drive root;
// reference OmsiLaunch.Api and call InstallationPaths.IdentityKey(root) to cover drive roots as well.
static string PipeNameFor(string installationRoot)
{
    var normalized = Path.GetFullPath(installationRoot).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
    var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    return "OmsiLaunch.Control.0.1." + key[..16];
}

static async Task<JsonDocument> SendAsync(string installationRoot, object request, TimeSpan timeout)
{
    using var cancellation = new CancellationTokenSource(timeout);
    await using var pipe = new NamedPipeClientStream(".", PipeNameFor(installationRoot), PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    await pipe.ConnectAsync(cancellation.Token);
    var payload = JsonSerializer.SerializeToUtf8Bytes(request);          // must be <= 65,536 bytes
    await pipe.WriteAsync(BitConverter.GetBytes(payload.Length), cancellation.Token);
    await pipe.WriteAsync(payload, cancellation.Token);
    await pipe.FlushAsync(cancellation.Token);
    var length = new byte[4];
    await ReadExactlyAsync(pipe, length, cancellation.Token);
    var reply = new byte[BitConverter.ToInt32(length)];
    await ReadExactlyAsync(pipe, reply, cancellation.Token);
    return JsonDocument.Parse(reply);
}

static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken token)
{
    var read = 0;
    while (read < buffer.Length)
    {
        var count = await stream.ReadAsync(buffer.AsMemory(read), token);
        if (count == 0) throw new EndOfStreamException("The owner closed the pipe before a full frame was received.");
        read += count;
    }
}

var root = @"C:\OMSI 2";                                        // <OMSI_PATH>
using var status = await SendAsync(root, new { ProtocolVersion = "0.1", Command = "session.status" }, TimeSpan.FromMilliseconds(750));
var sessionId = status.RootElement.GetProperty("Result").GetProperty("SessionId").GetString()!;
using var time = await SendAsync(root,
    new { ProtocolVersion = "0.1", Command = "runtime.execute",
          Arguments = new Dictionary<string, string> { ["operation"] = "time.read", ["session_id"] = sessionId } },
    TimeSpan.FromSeconds(8));
Console.WriteLine(time.RootElement.GetProperty("Result").GetProperty("Values"));
```

Gli oggetti anonimi vengono serializzati in `ProtocolVersion`/`Command`/`Arguments` esattamente come l'owner si aspetta. Una `TimeoutException`/`OperationCanceledException` su `ConnectAsync` significa che non esiste un owner per quella radice.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` e `Convert.ToHexString` richiedono .NET 5 o successivo, quindi questo esempio richiede PowerShell 7; Windows PowerShell 5.1 (.NET Framework) non può impostare `CurrentUserOnly`.

```powershell
$root = 'D:\OMSI 2'
$normalized = [IO.Path]::GetFullPath($root).TrimEnd('\').ToUpperInvariant()
$key = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized)))
$pipeName = 'OmsiLaunch.Control.0.1.' + $key.Substring(0, 16)

function Send-OmsiLaunchControl([string] $Json, [int] $TimeoutMilliseconds = 750) {
    $pipe = [IO.Pipes.NamedPipeClientStream]::new('.', $pipeName, [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::CurrentUserOnly)
    try {
        $pipe.Connect($TimeoutMilliseconds)
        $bytes = [Text.Encoding]::UTF8.GetBytes($Json)
        if ($bytes.Length -gt 65536) { throw 'OL_E_CONTROL_MESSAGE_TOO_LARGE' }
        $pipe.Write([BitConverter]::GetBytes([int] $bytes.Length), 0, 4)
        $pipe.Write($bytes, 0, $bytes.Length)
        $pipe.Flush()
        $lengthBytes = [byte[]]::new(4); $read = 0
        while ($read -lt 4) { $n = $pipe.Read($lengthBytes, $read, 4 - $read); if ($n -eq 0) { throw 'OL_E_CONTROL_PROTOCOL: the owner closed the connection without a reply' }; $read += $n }
        $length = [BitConverter]::ToInt32($lengthBytes, 0)
        $buffer = [byte[]]::new($length); $read = 0
        while ($read -lt $length) { $n = $pipe.Read($buffer, $read, $length - $read); if ($n -eq 0) { throw 'OL_E_CONTROL_PROTOCOL: truncated reply' }; $read += $n }
        [Text.Encoding]::UTF8.GetString($buffer) | ConvertFrom-Json
    }
    finally { $pipe.Dispose() }
}

$status = Send-OmsiLaunchControl '{"ProtocolVersion":"0.1","Command":"session.status"}'
$sessionId = $status.Result.SessionId
$request = @{ ProtocolVersion = '0.1'; Command = 'runtime.execute'; Arguments = @{ operation = 'time.read'; session_id = $sessionId } } | ConvertTo-Json -Compress
Send-OmsiLaunchControl $request 8000
```

`ConvertTo-Json` mantiene le maiuscole/minuscole delle chiavi della hashtable, quindi `ProtocolVersion`, `Command` e `Arguments` vengono emessi così come sono scritti.

<a id="using-the-cli-as-the-client"></a>
### Uso della CLI come client

Quando non serve un client personalizzato, invocare `OmsiLaunch.exe` dalla directory dell'installazione: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. La CLI esegue l'handshake di stato/associazione e associa le risposte ai [codici di uscita](exit-codes.md) (`0` ok, `2` validazione degli argomenti, `4` nessun owner, `7` rifiutato).

<a id="relationship-to-other-channels"></a>
## Relazione con gli altri canali

- La mailbox runtime tra l'owner e il plugin nel processo (mappata in memoria, 64 KiB, single-flight, associata alla sessione) è un canale separato e privato; il piano di controllo si limita a inoltrarvi le richieste tramite `IOmsiLaunch.ExecuteRuntimeAsync`.
- Il lease dell'installazione (`Local\OmsiLaunch.Installation.<sha256(root)>`) è un semaforo con nome, non fa parte di questo protocollo; garantisce un unico owner per installazione per sessione di accesso.
- La voce "End session" dell'icona nell'area di notifica e il `session.stop` del piano di controllo segnalano la stessa richiesta di arresto lato owner; vedere [area di notifica di Windows](windows-tray.md).
