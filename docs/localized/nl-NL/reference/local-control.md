# Lokaal control plane

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/local-control.md) voor OmsiLaunch 0.1.0-beta3. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Het lokale control plane (local control, lokale besturing) is het named-pipe-eindpunt waarlangs een actieve OmsiLaunch-eigenaar (het proces dat een sessie heeft gestart) semantische sessieopdrachten van andere processen op dezelfde machine aanneemt: status, gebeurtenissen, stoppen en openbare runtime-operaties. Deze pagina specificeert het eindpunt zoals geïmplementeerd in `tools\OmsiLaunch.Cli\LocalControlPlane.cs` en de verzoekhandler in `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): naamgeving van de pipe, framing, protocolversie, opdrachten, sessiebinding, foutcodes, vertrouwensmodel, beschikbaarheidsvenster en hoe je er vanuit andere tooling mee communiceert. De CLI-clientopdrachten (`session status`, `session stop`, `events read`, `events watch`, routes zoals `time get`) zijn dunne wrappers rond dit protocol; zie de [CLI-referentie](cli.md). De runtime-operaties zelf zijn gespecificeerd in [runtime control](runtime-control.md); het alternatief binnen het proces voor integrators is de [openbare API](public-api.md).

<a id="summary"></a>
## Overzicht

| Eigenschap | Waarde |
|---|---|
| Transport | Windows named pipe, `PipeDirection.InOut`, bytemodus, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, invoer- en uitvoerbuffers van één frame (65.540 bytes) |
| Pipenaam | `OmsiLaunch.Control.0.1.<key>`, waarbij `<key>` de eerste 16 hexadecimale tekens zijn van SHA-256 over de UTF-8-bytes van het volledige pad van de installatiemap, met een afsluitende `\` verwijderd, omgezet naar hoofdletters (`LocalControlPlane.PipeNameFor`) |
| Framing | Lengteprefix van 4 bytes als little-endian `int32`, gevolgd door evenveel bytes UTF-8-JSON; één verzoek en één antwoord per verbinding |
| Maximale berichtgrootte | 65.536 bytes voor een verzoek en voor een antwoord (`MaxMessageBytes`) |
| Protocolversie | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); een afwijking wordt beantwoord met `OL_E_CONTROL_PROTOCOL` |
| Opdrachten | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Sessiebinding | `session.stop` en `runtime.execute` vereisen dat `Arguments.session_id` gelijk is aan de actieve sessie-id |
| Beschikbaarheid | Vanaf het moment dat de sessie `Running` heeft bereikt (na een eventuele validatiebatch) totdat de sessie `Completed` of `Failed` bereikt en de eigenaar het eindpunt vrijgeeft |
| Bereik | Eén eindpunt per installatiemap; twee installaties van dezelfde gebruiker delen nooit een pipe |
| Stabiliteit | STABLE_BETA (offline tests in `OmsiLaunch.WindowsUiTests`; runtimebewijs: RV-003 gelijktijdige statusopvraging tijdens een native aanroep van meerdere seconden, Round A RA-008, en de raw-frame-testreeks `R04` van de runtime closure: 17 misvormde, te grote frames, frames met verkeerd protocol, onbekende opdracht, zonder binding en met verkeerde sessie, die elk met hun getypeerde fout werden beantwoord en gevolgd door een geslaagde `session.status`; een vastgelopen client blokkeert andere niet; een gebonden stop tijdens een lopende spawn beëindigt de sessie) |

<a id="pipe-name-derivation"></a>
## Afleiding van de pipenaam

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` is de enige definitie van installatie-identiteit en wordt ook door de installatielease gebruikt: de methode bepaalt het volledige pad (inclusief `.`- en `..`-segmenten, `/` of `\`, herhaalde scheidingstekens) en verwijdert afsluitende scheidingstekens, behalve bij een stationsroot. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` en `d:\omsi 2` leveren dezelfde naam op; `D:\OMSI 2` en `E:\OMSI 2` leveren verschillende namen op. De client leidt de naam altijd af van de installatie die het uitgevoerde bestand bevat (`AppContext.BaseDirectory`), tenzij de API-aanroeper expliciet een root doorgeeft. De `0.1` in de naam is de protocolversie, zodat een toekomstig protocol op dezelfde machine naast dit protocol kan bestaan.

<a id="framing-and-encoding"></a>
## Framing en codering

- Schrijver: serialiseer het record met de standaardopties van `System.Text.Json`, controleer `length <= 65536` (anders `OL_E_CONTROL_MESSAGE_TOO_LARGE`), schrijf `BitConverter.GetBytes((int)length)` (4 bytes, little-endian op Windows), schrijf de payload en flush.
- Lezer: lees precies 4 bytes; weiger `length < 0` of `length > 65536` met `OL_E_CONTROL_MESSAGE_INVALID`; lees precies `length` bytes; deserialiseer. Een verbinding die wordt gesloten voordat een volledig frame is gelezen, wordt door de eigenaar stil verbroken (geen antwoord). Een geweigerd frame wordt ook beantwoord als de client al meer bytes heeft geschreven dan de eigenaar leest: de pipebuffers bevatten een heel frame, zodat de schrijfactie van de client voltooid wordt en de client het antwoord kan lezen (runtime closure BUG-02; vóór de correctie bleven zo'n client en de eigenaar allebei in hun schrijfactie geblokkeerd).
- De eigenaar schrijft elk antwoord met een deadline van 5 s: een client die verbinding maakt, een verzoek stuurt en het antwoord nooit leest, kan een taak van de eigenaar niet langer bezethouden.
- Eigenschapsnamen in het verzoek zijn **PascalCase en hoofdlettergevoelig** (`ProtocolVersion`, `Command`, `Arguments`), omdat de eigenaar met standaardopties deserialiseert. Antwoorden gebruiken `Ok`, `Result`, `ErrorCode`, `Message`. Enumwaarden worden als gehele getallen geserialiseerd, `Guid`'s als strings.
- Eén verzoek per verbinding: de eigenaar leest één verzoek, schrijft één antwoord en sluit de pipe. Open voor elk verzoek een nieuwe verbinding.

<a id="request"></a>
### Verzoek

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` moet de id zijn die `session.status` voor de actieve sessie retourneert (de waarde hierboven komt uit een echte sessie).

`Arguments` is optioneel (`null` of weggelaten) voor `session.status` en `session.events`. Argumentsleutels worden ordinaal vergeleken (hoofdlettergevoelig).

<a id="response"></a>
### Antwoord

Frames die zijn vastgelegd bij een echte eigenaar (runtime closure `R04`, definitief pakket). Een geslaagde `runtime.execute`:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Een geweigerde `session.stop` zonder de actieve `session_id`:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Opdrachten

| Opdracht | Argumenten | Resultaat bij `Ok=true` | Opmerkingen |
|---|---|---|---|
| `session.status` | geen | `SessionStatus`: `SessionId` (GUID als string), `State` (geheel getal `SessionState`; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (array van `{Code, Message, Data}`), `RuntimeEvents` (array) | Alleen lezen. Clients lezen dit eerst om de `SessionId` te verkrijgen. |
| `session.events` | geen | Array van `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (monotone `int64`), `Data` (stringmap) | Alleen lezen, begrensde lijst; `events watch` pollt deze en drukt items af met een `Sequence` boven de laatst geziene. |
| `session.stop` | `session_id` (verplicht) | `{"accepted": true, "session_id": "..."}` | Vraagt de canonieke stop aan (`TerminateProcess` op OMSI, daarna herstel). Keert direct terug; de sessietoestand is via `session.status` te volgen totdat het eindpunt verdwijnt. |
| `runtime.execute` | `operation` (verplicht), `session_id` (verplicht), plus de eigen argumenten van de operatie (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (door de eigenaar toegekend, beginnend bij `50001`), `Succeeded`, `ErrorCode`, `Values` (stringmap) | Vóór uitvoering gevalideerd met `PublicCapabilityRegistry.ValidateRuntimeArguments`: onbekende of interne operatie → `OL_E_RUNTIME_OPERATION_UNKNOWN`; ontbrekende verplichte waarde → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout 8 s, 30 s voor `road-vehicles.spawn`. Een runtimefout wordt beantwoord als `Ok=false`, met `Result` ingesteld op het `RuntimeCommandResult`, `ErrorCode` op de bijbehorende code en `Message` `Runtime operation was rejected.` Resultaatsleutels die beginnen met `internal_` of eindigen op `_address`, `_pointer`, `_vmt` worden door de API verwijderd voordat ze de pipe bereiken. |
| al het andere | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

De eigenaar bedient verbindingen gelijktijdig (elke geaccepteerde client wordt in een eigen taak bediend), zodat `session.status` blijft antwoorden terwijl een native aanroep van meerdere seconden, zoals `road-vehicles.spawn`, bezig is.

<a id="session-id-binding"></a>
## Binding aan de sessie-id

Wijzigende opdrachten moeten de sessie noemen waarop ze betrekking hebben. De CLI implementeert `TryRequestBoundAsync`: deze stuurt `session.status` (750 ms), neemt `Result.SessionId` over en herhaalt het verzoek met `session_id` toegevoegd aan `Arguments`. Een ontbrekende, niet-parseerbare of afwijkende id wordt beantwoord met `OL_E_CONTROL_SESSION_MISMATCH`. Als de eigenaar geen sessie-id rapporteert, meldt de client `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Antwoordmetadata en afgekapte gebeurtenisgeschiedenis

`Metadata` is `null`, tenzij het antwoord een feit over zichzelf bevat. `session.status` en `session.events` retourneren de gebeurtenisgeschiedenis van de eigenaar (maximaal 256 gebeurtenissen, nieuwste als laatste). Als die geschiedenis niet in één frame van 64 KiB past, worden de oudste gebeurtenissen verwijderd tot deze past, en vermeldt `Metadata` dit:

| Sleutel | Betekenis |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Gebeurtenissen in dit antwoord |
| `events_dropped_count` | Oudste gebeurtenissen die zijn verwijderd om in het frame te passen |
| `events_available_count` | Gebeurtenissen die de eigenaar voor dit antwoord had |
| `events_first_returned_sequence` | `Sequence` van de eerste geretourneerde gebeurtenis |

Zonder `Metadata` is het antwoord niet door het control plane afgekapt. `Sequence` is in beide gevallen monotoon, zodat een client ook gebeurtenissen kan detecteren die de eigenaar zelf buiten zijn geschiedenis van 256 gebeurtenissen heeft verwijderd. De CLI toont deze metadata in de `--json`-envelope en als opmerking in tekstmodus.

<a id="error-codes"></a>
## Foutcodes

| Code | Oorsprong | Betekenis |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | eigenaar / client | De `ProtocolVersion` van het verzoek is niet `0.1`; het antwoord van de eigenaar kon niet worden gedecodeerd of was leeg; de eigenaar heeft de verbinding gesloten zonder te antwoorden; de verbinding is verbroken nadat deze tot stand was gebracht; of de eigenaar heeft geen sessie-id gerapporteerd. |
| `OL_E_CONTROL_MESSAGE_INVALID` | eigenaar / client | Lengteprefix buiten bereik (een te groot verzoekframe wordt op deze manier geweigerd voordat de payload wordt gelezen), leeg frame, JSON `null`, verzoek zonder `Command`, of JSON die niet kon worden gedecodeerd. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | schrijver aan de clientzijde | Het eigen verzoek van de client is groter dan 65.536 bytes; dit wordt aan de client gemeld en er wordt niets verzonden. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | schrijver aan de eigenaarzijde | Het antwoord van de eigenaar is groter dan 65.536 bytes; de eigenaar antwoordt met deze getypeerde fout. `session.status` en `session.events` korten hun gebeurtenisgeschiedenis in (oudste eerst) zodat deze past, en veroorzaken deze fout dus niet. |
| `OL_E_CONTROL_SESSION_MISMATCH` | eigenaar | `session.stop` / `runtime.execute` zonder de actieve `session_id`. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | eigenaar | Niet-ondersteunde opdrachtnaam. |
| `OL_E_CONTROL_HANDLER_FAILED` | eigenaar | Het antwoord van de handler kon niet worden geserialiseerd, of de handler gooide een exceptie zonder `OL_E_`-code in het bericht; met een code wordt in plaats daarvan die code geretourneerd (bijvoorbeeld `OL_E_SESSION_NOT_RUNNING` van `ExecuteRuntimeAsync` nadat OMSI is afgesloten). |
| `OL_E_CONTROL_FAILED` | CLI-client | Standaardcode als een antwoord `Ok=false` is zonder `ErrorCode`. |
| `OL_E_TIMEOUT` | client | De client was verbonden met een eigenaar die niet binnen de timeout antwoordde. |
| `OL_E_NO_ACTIVE_SESSION` | CLI-client | Er kon geen eindpunt van een eigenaar worden bereikt (`TryRequestAsync` retourneert alleen `null` als de verbinding zelf mislukt). Zodra er een verbinding is, is elke fout een van de getypeerde codes hierboven. Exitcode `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | client en eigenaar | Validatie van `runtime.execute` tegen het openbare oppervlak (exitcode `2` in de CLI). |

Elke bekende fout wordt beantwoord met een getypeerde fout, zodat een client een storing nooit verwart met een afwezige eigenaar. Twee gevallen eindigen zonder antwoordframe: een client die de verbinding verbreekt voordat een volledig verzoek is verzonden (er is niemand om te antwoorden), en een verzoek dat nog loopt wanneer de sessie eindigt (de eigenaar annuleert openstaande antwoorden terwijl het eindpunt wordt afgesloten; de client ziet het einde van de stream, wat de CLI-client rapporteert als `OL_E_CONTROL_PROTOCOL` "The owner closed the connection without a reply"). Een client die een verzoek heeft verzonden en de verbinding verbreekt voordat het antwoord is gelezen, heeft geen invloed op de eigenaar. Het starten van een eigenaar wordt geweigerd zodra er een antwoord (ook een getypeerde fout) van het eindpunt van de installatie komt, omdat een antwoord bewijst dat er een eigenaar aanwezig is. De CLI beeldt antwoorden af op exitcodes zoals beschreven in [exitcodes](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Vertrouwensmodel (geaccepteerd risico)

- `PipeOptions.CurrentUserOnly` beperkt de pipe tot de Windows-gebruiker (en het integriteitsniveau) die eigenaar is van de sessie; aan de clientzijde controleert dezelfde optie dat de server eigendom is van dezelfde gebruiker.
- **Elk proces dat als dezelfde Windows-gebruiker draait, kan de status lezen, de sessie stoppen en openbare runtime-operaties uitvoeren.** Er is geen aanvullende authenticatie, geen token en geen autorisatie per client. Dit is een gedocumenteerd, geaccepteerd risico voor de bèta; voer OmsiLaunch-sessies niet uit onder een account dat wordt gedeeld met niet-vertrouwde software.
- De pipe stelt nooit native adressen, handles of ruwe geheugenoperaties bloot; alleen de openbare operatie-id's van `PublicCapabilityRegistry` worden geaccepteerd, en interne onderzoeksprimitieven (`internal.*`) worden geweigerd vóór het opzoeken van een sessie.
- Berichten zijn begrensd (64 KiB) en in frames verpakt; een misvormd of te groot frame wordt beantwoord of verworpen zonder de sessie te beïnvloeden.
- Als de pipenaam al eigendom is van een ander proces wanneer de eigenaar begint te luisteren (`IOException`/`UnauthorizedAccessException` bij het aanmaken), blijft de eigenaar de sessie zonder eindpunt uitvoeren en legt de fout vast in `LocalControlPlane.ListenFault`; clients zien dan `OL_E_NO_ACTIVE_SESSION`. Gebruik het systeemvakpictogram of Ctrl+C om zo'n sessie te beëindigen.

<a id="availability-window"></a>
## Beschikbaarheidsvenster

1. `StartSessionAsync` wordt uitgevoerd; het eindpunt bestaat nog niet. Een tweede start van `OmsiLaunch.exe` voor dezelfde root peilt `session.status` gedurende 250 ms voordat deze start, en mislukt alleen met `OL_E_SESSION_ALREADY_ACTIVE` als er al een eigenaar antwoordt; twee eigenaren die tijdens het opstarten met elkaar concurreren, worden door de installatielease geserialiseerd (`OL_E_INSTALLATION_BUSY`).
2. De sessie bereikt `Running`; validatiebatches (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) worden, indien aangevraagd, voltooid.
3. `LocalControlPlane.Start()`: het eindpunt accepteert verbindingen. De eigen `/runtime:`-operatie van de eigenaar, als die er is, wordt na dit punt uitgevoerd.
4. Het eindpunt blijft actief tijdens `ProcessExited`, `Restoring` en `CleaningRuntime` (de status rapporteert die toestanden), totdat de sessie `Completed` of `Failed` is.
5. `DisposeAsync` annuleert de listener en wacht op lopende clienttaken, waarna de eigenaar `CloseAsync` aanroept. Daarna bestaat de pipenaam niet meer.

Clients horen korte verbindingstimeouts te gebruiken (de CLI gebruikt 750 ms voor status/stop/events) en "geen eindpunt" te behandelen als "geen actieve sessie voor deze installatie".

<a id="using-the-protocol-from-other-tooling"></a>
## Het protocol gebruiken vanuit andere tooling

<a id="c-net-6-or-later"></a>
### C# (.NET 6 of later)

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

De anonieme objecten worden geserialiseerd naar `ProtocolVersion`/`Command`/`Arguments`, precies zoals de eigenaar verwacht. Een `TimeoutException`/`OperationCanceledException` bij `ConnectAsync` betekent dat er voor die root geen eigenaar is.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` en `Convert.ToHexString` vereisen .NET 5 of later, dus dit voorbeeld heeft PowerShell 7 nodig; Windows PowerShell 5.1 (.NET Framework) kan `CurrentUserOnly` niet instellen.

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

`ConvertTo-Json` behoudt de hoofdletters van de hashtablesleutels, zodat `ProtocolVersion`, `Command` en `Arguments` precies zo worden uitgevoerd als ze zijn geschreven.

<a id="using-the-cli-as-the-client"></a>
### De CLI als client gebruiken

Als er geen eigen client nodig is, roep je `OmsiLaunch.exe` aan vanuit de installatiemap: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. De CLI voert de status-/bindingshandshake uit en beeldt antwoorden af op [exitcodes](exit-codes.md) (`0` ok, `2` argumentvalidatie, `4` geen eigenaar, `7` geweigerd).

<a id="relationship-to-other-channels"></a>
## Verhouding tot andere kanalen

- De runtime-mailbox tussen de eigenaar en de plugin binnen het proces (memory-mapped, 64 KiB, single-flight, sessiegebonden) is een afzonderlijk, privé kanaal; het control plane stuurt alleen via `IOmsiLaunch.ExecuteRuntimeAsync` naar die mailbox door.
- De installatielease (`Local\OmsiLaunch.Installation.<sha256(root)>`) is een benoemde semafoor en geen onderdeel van dit protocol; deze garandeert één eigenaar per installatie per aanmeldsessie.
- Het item "End session" (sessie beëindigen) van het systeemvakpictogram en `session.stop` van het control plane sturen hetzelfde stopverzoek naar de eigenaar; zie [Windows-systeemvak](windows-tray.md).
