# Lokale Steuerungsebene (Local Control Plane)

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Übersetzung der [englischen Originalseite](../../../reference/local-control.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Die lokale Steuerungsebene ist der Named-Pipe-Endpunkt, über den ein laufender OmsiLaunch-Eigentümer (der Prozess, der eine Sitzung gestartet hat) semantische Sitzungsbefehle von anderen Prozessen auf demselben Rechner entgegennimmt: Status, Ereignisse, Stopp und öffentliche Runtime-Operationen. Diese Seite spezifiziert den Endpunkt, wie er in `tools\OmsiLaunch.Cli\LocalControlPlane.cs` implementiert ist, und den Request-Handler in `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): Pipe-Benennung, Framing, Protokollversion, Befehle, Sitzungsbindung, Fehlercodes, Vertrauensmodell, Verfügbarkeitsfenster und die Ansprache aus anderen Werkzeugen. Die Befehle des CLI-Clients (`session status`, `session stop`, `events read`, `events watch`, Routen wie `time get`) sind dünne Wrapper über diesem Protokoll; siehe die [CLI-Referenz](cli.md). Die Runtime-Operationen selbst sind unter [Runtime-Steuerung](runtime-control.md) spezifiziert; die In-Process-Alternative für Integratoren ist die [öffentliche API](public-api.md).

<a id="summary"></a>
## Übersicht

| Eigenschaft | Wert |
|---|---|
| Transport | Windows-Named-Pipe, `PipeDirection.InOut`, Byte-Modus, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, Ein- und Ausgabepuffer von je einem Frame (65.540 Bytes) |
| Pipe-Name | `OmsiLaunch.Control.0.1.<key>`, wobei `<key>` die ersten 16 Hex-Zeichen des SHA-256 über die UTF-8-Bytes des vollständigen Pfads des Installationsstammverzeichnisses sind, ohne abschließendes `\`, in Großbuchstaben (`LocalControlPlane.PipeNameFor`) |
| Framing | 4-Byte-Längenpräfix `int32` in Little-Endian, gefolgt von ebenso vielen Bytes UTF-8-JSON; eine Anfrage und eine Antwort pro Verbindung |
| Maximale Nachricht | 65.536 Bytes für eine Anfrage und für eine Antwort (`MaxMessageBytes`) |
| Protokollversion | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); eine Abweichung wird mit `OL_E_CONTROL_PROTOCOL` beantwortet |
| Befehle | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Sitzungsbindung | `session.stop` und `runtime.execute` erfordern `Arguments.session_id` gleich der aktiven Sitzungs-ID |
| Verfügbarkeit | Ab dem Zeitpunkt, an dem die Sitzung `Running` erreicht hat (nach einem eventuellen Validierungs-Batch), bis die Sitzung `Completed` oder `Failed` erreicht und der Eigentümer den Endpunkt freigibt |
| Geltungsbereich | Ein Endpunkt pro Installationsstammverzeichnis; zwei Installationen desselben Benutzers teilen sich nie eine Pipe |
| Stabilität | STABLE_BETA (Offline-Tests in `OmsiLaunch.WindowsUiTests`; Runtime-Nachweis: RV-003 gleichzeitige Statusabfrage während eines mehrsekündigen nativen Aufrufs, Round A RA-008 und die Rohframe-Testreihe `R04` des Runtime-Abschlusses: 17 fehlerhaft aufgebaute, übergroße, protokollfremde, unbekannte, ungebundene und sitzungsfremde Frames, jeweils mit ihrem typisierten Fehler beantwortet und gefolgt von einem erfolgreichen `session.status`; ein hängender Client blockiert keine anderen; ein gebundener Stopp während eines laufenden Spawns beendet die Sitzung) |

<a id="pipe-name-derivation"></a>
## Ableitung des Pipe-Namens

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` ist die einzige Definition der Installationsidentität und wird auch von der Installations-Lease verwendet: Sie löst den vollständigen Pfad auf (einschließlich der Segmente `.` und `..`, `/` oder `\`, wiederholter Trennzeichen) und entfernt abschließende Trennzeichen außer bei einem Laufwerksstamm. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` und `d:\omsi 2` ergeben denselben Namen; `D:\OMSI 2` und `E:\OMSI 2` ergeben unterschiedliche Namen. Der Client leitet den Namen immer aus der Installation ab, die die von ihm ausgeführte Datei enthält (`AppContext.BaseDirectory`), sofern der API-Aufrufer kein explizites Stammverzeichnis übergibt. Das `0.1` im Namen ist die Protokollversion, sodass ein künftiges Protokoll auf demselben Rechner parallel existieren kann.

<a id="framing-and-encoding"></a>
## Framing und Kodierung

- Schreiber: Serialisieren Sie den Datensatz mit den Standardoptionen von `System.Text.Json`, prüfen Sie `length <= 65536` (andernfalls `OL_E_CONTROL_MESSAGE_TOO_LARGE`), schreiben Sie `BitConverter.GetBytes((int)length)` (4 Bytes, unter Windows Little-Endian), schreiben Sie die Nutzdaten, führen Sie einen Flush aus.
- Leser: Lesen Sie genau 4 Bytes; lehnen Sie `length < 0` oder `length > 65536` mit `OL_E_CONTROL_MESSAGE_INVALID` ab; lesen Sie genau `length` Bytes; deserialisieren Sie. Eine Verbindung, die geschlossen wird, bevor ein vollständiger Frame gelesen wurde, wird vom Eigentümer stillschweigend verworfen (keine Antwort). Ein abgelehnter Frame wird auch dann beantwortet, wenn der Client bereits mehr Bytes geschrieben hat, als der Eigentümer liest: Die Pipe-Puffer fassen einen ganzen Frame, sodass der Schreibvorgang des Clients abgeschlossen wird und er die Antwort lesen kann (Runtime-Abschluss BUG-02; vor der Korrektur blockierten ein solcher Client und der Eigentümer beide in ihren Schreibvorgängen).
- Der Eigentümer schreibt jede Antwort mit einer Frist von 5 s: Ein Client, der sich verbindet, eine Anfrage sendet und die Antwort nie liest, kann einen Task des Eigentümers nicht länger belegen.
- Eigenschaftsnamen sind in der Anfrage **PascalCase und case-sensitiv** (`ProtocolVersion`, `Command`, `Arguments`), weil der Eigentümer mit Standardoptionen deserialisiert. Antworten verwenden `Ok`, `Result`, `ErrorCode`, `Message`. Enum-Werte werden als Ganzzahlen serialisiert, `Guid`s als Zeichenfolgen.
- Eine Anfrage pro Verbindung: Der Eigentümer liest eine Anfrage, schreibt eine Antwort und schließt die Pipe. Öffnen Sie für jede Anfrage eine neue Verbindung.

<a id="request"></a>
### Anfrage

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` muss die von `session.status` für die laufende Sitzung zurückgegebene ID sein (der obige Wert stammt aus einer echten Sitzung).

`Arguments` ist für `session.status` und `session.events` optional (`null` oder weggelassen). Argumentschlüssel werden ordinal verglichen (case-sensitiv).

<a id="response"></a>
### Antwort

Von einem echten Eigentümer erfasste Frames (Runtime-Abschluss `R04`, finales Paket). Ein erfolgreiches `runtime.execute`:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Ein abgelehntes `session.stop` ohne die aktive `session_id`:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Befehle

| Befehl | Argumente | Ergebnis bei `Ok=true` | Hinweise |
|---|---|---|---|
| `session.status` | keine | `SessionStatus`: `SessionId` (GUID als Zeichenfolge), `State` (Ganzzahl `SessionState`; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (Array aus `{Code, Message, Data}`), `RuntimeEvents` (Array) | Nur lesend. Clients lesen dies zuerst, um die `SessionId` zu erhalten. |
| `session.events` | keine | Array aus `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (monotones `int64`), `Data` (String-Map) | Nur lesend, begrenzte Liste; `events watch` fragt es ab und gibt Einträge mit einer `Sequence` oberhalb der zuletzt gesehenen aus. |
| `session.stop` | `session_id` (erforderlich) | `{"accepted": true, "session_id": "..."}` | Fordert den kanonischen Stopp an (`TerminateProcess` auf OMSI, danach Wiederherstellung). Kehrt sofort zurück; der Sitzungszustand ist über `session.status` beobachtbar, bis der Endpunkt verschwindet. |
| `runtime.execute` | `operation` (erforderlich), `session_id` (erforderlich) sowie die eigenen Argumente der Operation (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (vom Eigentümer vergeben, beginnend bei `50001`), `Succeeded`, `ErrorCode`, `Values` (String-Map) | Wird vor der Ausführung mit `PublicCapabilityRegistry.ValidateRuntimeArguments` validiert: unbekannte oder interne Operation → `OL_E_RUNTIME_OPERATION_UNKNOWN`; fehlender erforderlicher Wert → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout 8 s, 30 s für `road-vehicles.spawn`. Ein Runtime-Fehlschlag wird als `Ok=false` beantwortet, wobei `Result` auf das `RuntimeCommandResult`, `ErrorCode` auf dessen Code und `Message` auf `Runtime operation was rejected.` gesetzt ist. Ergebnisschlüssel, die mit `internal_` beginnen oder auf `_address`, `_pointer`, `_vmt` enden, werden von der API entfernt, bevor sie die Pipe erreichen. |
| alles andere | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

Der Eigentümer bedient Verbindungen parallel (jeder angenommene Client wird in einem eigenen Task bedient), sodass `session.status` weiterhin antwortet, während ein mehrsekündiger nativer Aufruf wie `road-vehicles.spawn` läuft.

<a id="session-id-binding"></a>
## Bindung an die Sitzungs-ID

Verändernde Befehle müssen die Sitzung benennen, auf die sie wirken. Die CLI implementiert `TryRequestBoundAsync`: Sie sendet `session.status` (750 ms), übernimmt `Result.SessionId` und wiederholt die Anfrage mit `session_id`, ergänzt in `Arguments`. Eine fehlende, nicht parsebare oder abweichende ID wird mit `OL_E_CONTROL_SESSION_MISMATCH` beantwortet. Meldet der Eigentümer keine Sitzungs-ID, meldet der Client `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Antwort-Metadaten und gekürzte Ereignishistorie

`Metadata` ist `null`, sofern die Antwort keine Angabe über sich selbst enthält. `session.status` und `session.events` geben die Ereignishistorie des Eigentümers zurück (höchstens 256 Ereignisse, neuestes zuletzt). Passt diese Historie nicht in einen Frame von 64 KiB, werden die ältesten Ereignisse entfernt, bis sie passt, und `Metadata` gibt dies an:

| Schlüssel | Bedeutung |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Ereignisse in dieser Antwort |
| `events_dropped_count` | Älteste Ereignisse, die entfernt wurden, damit der Frame passt |
| `events_available_count` | Ereignisse, die der Eigentümer für diese Antwort vorhielt |
| `events_first_returned_sequence` | `Sequence` des ersten zurückgegebenen Ereignisses |

Ohne `Metadata` wurde die Antwort von der Steuerungsebene nicht gekürzt. `Sequence` ist in beiden Fällen monoton, sodass ein Client auch Ereignisse erkennen kann, die der Eigentümer selbst jenseits seiner Historie von 256 Ereignissen verdrängt hat. Die CLI gibt diese Metadaten im `--json`-Envelope und im Textmodus als Hinweis aus.

<a id="error-codes"></a>
## Fehlercodes

| Code | Ursprung | Bedeutung |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | Eigentümer / Client | `ProtocolVersion` der Anfrage ist nicht `0.1`; die Antwort des Eigentümers konnte nicht dekodiert werden oder war leer; der Eigentümer hat die Verbindung ohne Antwort geschlossen; die Verbindung ist nach dem Aufbau abgebrochen; oder der Eigentümer hat keine Sitzungs-ID gemeldet. |
| `OL_E_CONTROL_MESSAGE_INVALID` | Eigentümer / Client | Längenpräfix außerhalb des Bereichs (ein übergroßer Anfrage-Frame wird auf diese Weise abgelehnt, bevor seine Nutzdaten gelesen werden), leerer Frame, JSON `null`, Anfrage ohne `Command` oder JSON, das nicht dekodiert werden konnte. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | Client-Schreiber | Die eigene Anfrage des Clients überschreitet 65.536 Bytes; dies wird dem Client gemeldet, und es wird nichts gesendet. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | Eigentümer-Schreiber | Die Antwort des Eigentümers überschreitet 65.536 Bytes; der Eigentümer antwortet mit diesem typisierten Fehler. `session.status` und `session.events` kürzen ihre Ereignishistorie (älteste zuerst), damit sie passt, und erzeugen ihn daher nicht. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Eigentümer | `session.stop` / `runtime.execute` ohne die aktive `session_id`. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Eigentümer | Nicht unterstützter Befehlsname. |
| `OL_E_CONTROL_HANDLER_FAILED` | Eigentümer | Die Antwort des Handlers konnte nicht serialisiert werden, oder der Handler hat eine Ausnahme ohne `OL_E_`-Code in ihrer Meldung geworfen; mit einem Code wird stattdessen dieser Code zurückgegeben (z. B. `OL_E_SESSION_NOT_RUNNING` aus `ExecuteRuntimeAsync`, nachdem OMSI beendet wurde). |
| `OL_E_CONTROL_FAILED` | CLI-Client | Standardcode, wenn eine Antwort `Ok=false` ohne `ErrorCode` ist. |
| `OL_E_TIMEOUT` | Client | Der Client war mit einem Eigentümer verbunden, der nicht innerhalb des Timeouts geantwortet hat. |
| `OL_E_NO_ACTIVE_SESSION` | CLI-Client | Es konnte kein Endpunkt eines Eigentümers erreicht werden (`TryRequestAsync` gibt `null` nur zurück, wenn der Verbindungsaufbau selbst fehlschlägt). Sobald eine Verbindung besteht, ist jeder Fehlschlag einer der obigen typisierten Codes. Exit `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | Client und Eigentümer | Validierung von `runtime.execute` an der öffentlichen Oberfläche (Exit `2` in der CLI). |

Jeder bekannte Fehlschlag wird mit einem typisierten Fehler beantwortet, sodass ein Client einen Fehler nie mit einem fehlenden Eigentümer verwechselt. Zwei Fälle enden ohne Antwort-Frame: ein Client, der die Verbindung trennt, bevor er eine vollständige Anfrage gesendet hat (niemand, dem geantwortet werden könnte), und eine Anfrage, die noch in Bearbeitung ist, wenn die Sitzung endet (der Eigentümer bricht ausstehende Antworten ab, während er den Endpunkt herunterfährt; der Client sieht das Ende des Streams, das der CLI-Client als `OL_E_CONTROL_PROTOCOL` „The owner closed the connection without a reply“ meldet). Ein Client, der eine Anfrage gesendet hat und die Verbindung trennt, bevor er die Antwort liest, beeinträchtigt den Eigentümer nicht. Der Start eines Eigentümers wird verweigert, sobald irgendeine Antwort (einschließlich eines typisierten Fehlers) vom Endpunkt der Installation kommt, weil eine Antwort beweist, dass ein Eigentümer vorhanden ist. Die CLI bildet Antworten wie unter [Exitcodes](exit-codes.md) beschrieben auf Exitcodes ab.

<a id="trust-model-accepted-risk"></a>
## Vertrauensmodell (akzeptiertes Risiko)

- `PipeOptions.CurrentUserOnly` beschränkt die Pipe auf den Windows-Benutzer (und die Integritätsstufe), dem die Sitzung gehört; auf der Client-Seite prüft dieselbe Option, dass der Server demselben Benutzer gehört.
- **Jeder Prozess, der unter demselben Windows-Benutzer läuft, kann den Status lesen, die Sitzung beenden und öffentliche Runtime-Operationen ausführen.** Es gibt keine zusätzliche Authentifizierung, kein Token und keine Autorisierung pro Client. Dies ist ein dokumentiertes, akzeptiertes Risiko für die Beta; führen Sie OmsiLaunch-Sitzungen nicht unter einem Konto aus, das mit nicht vertrauenswürdiger Software geteilt wird.
- Die Pipe legt niemals native Adressen, Handles oder rohe Speicheroperationen offen; nur die öffentlichen Operations-IDs von `PublicCapabilityRegistry` werden akzeptiert, und interne Forschungsprimitive (`internal.*`) werden vor jeder Sitzungssuche abgelehnt.
- Nachrichten sind begrenzt (64 KiB) und in Frames gefasst; ein fehlerhaft aufgebauter oder übergroßer Frame wird beantwortet oder verworfen, ohne die Sitzung zu beeinträchtigen.
- Gehört der Pipe-Name bereits einem anderen Prozess, wenn der Eigentümer mit dem Lauschen beginnt (`IOException`/`UnauthorizedAccessException` beim Erstellen), führt der Eigentümer die Sitzung ohne Endpunkt weiter und hält den Fehler in `LocalControlPlane.ListenFault` fest; Clients sehen dann `OL_E_NO_ACTIVE_SESSION`. Verwenden Sie das Tray-Symbol oder Ctrl+C, um eine solche Sitzung zu beenden.

<a id="availability-window"></a>
## Verfügbarkeitsfenster

1. `StartSessionAsync` läuft; der Endpunkt existiert noch nicht. Ein zweiter Start von `OmsiLaunch.exe` für dasselbe Stammverzeichnis fragt vor dem Start 250 ms lang `session.status` ab und schlägt nur dann mit `OL_E_SESSION_ALREADY_ACTIVE` fehl, wenn bereits ein Eigentümer antwortet; zwei Eigentümer, die beim Start konkurrieren, werden durch die Installations-Lease serialisiert (`OL_E_INSTALLATION_BUSY`).
2. Die Sitzung erreicht `Running`; Validierungs-Batches (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) werden, sofern angefordert, abgeschlossen.
3. `LocalControlPlane.Start()`: Der Endpunkt nimmt Verbindungen an. Die eigene `/runtime:`-Operation des Eigentümers wird, falls vorhanden, nach diesem Punkt ausgeführt.
4. Der Endpunkt bleibt während `ProcessExited`, `Restoring` und `CleaningRuntime` bestehen (der Status meldet diese Zustände), bis die Sitzung `Completed` oder `Failed` ist.
5. `DisposeAsync` bricht den Listener ab, wartet auf laufende Client-Tasks, und der Eigentümer ruft `CloseAsync` auf. Danach existiert der Pipe-Name nicht mehr.

Clients sollten kurze Verbindungs-Timeouts verwenden (die CLI verwendet 750 ms für Status/Stopp/Ereignisse) und „kein Endpunkt“ als „keine aktive Sitzung für diese Installation“ behandeln.

<a id="using-the-protocol-from-other-tooling"></a>
## Verwendung des Protokolls aus anderen Werkzeugen

<a id="c-net-6-or-later"></a>
### C# (.NET 6 oder höher)

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

Die anonymen Objekte werden genau so zu `ProtocolVersion`/`Command`/`Arguments` serialisiert, wie der Eigentümer es erwartet. Eine `TimeoutException`/`OperationCanceledException` bei `ConnectAsync` bedeutet, dass es für dieses Stammverzeichnis keinen Eigentümer gibt.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` und `Convert.ToHexString` erfordern .NET 5 oder höher, daher benötigt dieses Beispiel PowerShell 7; Windows PowerShell 5.1 (.NET Framework) kann `CurrentUserOnly` nicht setzen.

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

`ConvertTo-Json` behält die Groß-/Kleinschreibung der Hashtable-Schlüssel bei, sodass `ProtocolVersion`, `Command` und `Arguments` wie geschrieben ausgegeben werden.

<a id="using-the-cli-as-the-client"></a>
### Die CLI als Client verwenden

Wenn kein eigener Client benötigt wird, rufen Sie `OmsiLaunch.exe` aus dem Installationsverzeichnis auf: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. Die CLI führt den Status-/Bindungs-Handshake durch und bildet Antworten auf [Exitcodes](exit-codes.md) ab (`0` ok, `2` Argumentvalidierung, `4` kein Eigentümer, `7` abgelehnt).

<a id="relationship-to-other-channels"></a>
## Beziehung zu anderen Kanälen

- Die Runtime-Mailbox zwischen dem Eigentümer und dem In-Process-Plugin (Memory-Mapped, 64 KiB, Single Flight, an die Sitzung gebunden) ist ein separater, privater Kanal; die Steuerungsebene leitet nur über `IOmsiLaunch.ExecuteRuntimeAsync` an sie weiter.
- Die Installations-Lease (`Local\OmsiLaunch.Installation.<sha256(root)>`) ist ein benanntes Semaphor und nicht Teil dieses Protokolls; sie garantiert einen einzigen Eigentümer pro Installation und Anmeldesitzung.
- „End session“ im Tray-Symbol und `session.stop` der Steuerungsebene signalisieren dieselbe Stopp-Anforderung auf Eigentümerseite; siehe [Windows-Tray](windows-tray.md).
