# Plano de control local

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Traducción de la [página original en inglés](../../../reference/local-control.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

El plano de control local es el endpoint de canalización con nombre a través del cual un propietario de OmsiLaunch en ejecución (el proceso que inició una sesión) acepta comandos semánticos de sesión de otros procesos del mismo ordenador: estado, eventos, detención y operaciones de runtime públicas. Esta página especifica el endpoint tal como está implementado en `tools\OmsiLaunch.Cli\LocalControlPlane.cs` y el manejador de solicitudes de `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): nomenclatura de la canalización, tramas, versión del protocolo, comandos, vinculación a la sesión, códigos de error, modelo de confianza, ventana de disponibilidad y cómo comunicarse con él desde otras herramientas. Los comandos cliente de la CLI (`session status`, `session stop`, `events read`, `events watch`, rutas como `time get`) son envoltorios finos sobre este protocolo; consulta la [referencia de la CLI](cli.md). Las operaciones de runtime propiamente dichas se especifican en [control de runtime](runtime-control.md); la alternativa dentro del proceso para integradores es la [API pública](public-api.md).

<a id="summary"></a>
## Resumen

| Propiedad | Valor |
|---|---|
| Transporte | Canalización con nombre de Windows, `PipeDirection.InOut`, modo byte, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, búferes de entrada y salida de una trama (65,540 bytes) |
| Nombre de la canalización | `OmsiLaunch.Control.0.1.<key>`, donde `<key>` son los primeros 16 caracteres hexadecimales del SHA-256 de los bytes UTF-8 de la ruta completa de la raíz de la instalación, sin el `\` final y en mayúsculas (`LocalControlPlane.PipeNameFor`) |
| Tramas | Prefijo de longitud `int32` little-endian de 4 bytes seguido de ese número de bytes de JSON UTF-8; una solicitud y una respuesta por conexión |
| Mensaje máximo | 65,536 bytes para una solicitud y para una respuesta (`MaxMessageBytes`) |
| Versión del protocolo | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); una discrepancia se responde con `OL_E_CONTROL_PROTOCOL` |
| Comandos | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Vinculación a la sesión | `session.stop` y `runtime.execute` requieren que `Arguments.session_id` sea igual al id de la sesión activa |
| Disponibilidad | Desde el momento en que la sesión llega a `Running` (después de cualquier lote de validación) hasta que la sesión llega a `Completed` o `Failed` y el propietario libera el endpoint |
| Alcance | Un endpoint por raíz de instalación; dos instalaciones del mismo usuario nunca comparten una canalización |
| Estabilidad | STABLE_BETA (pruebas offline en `OmsiLaunch.WindowsUiTests`; evidencia de runtime: RV-003, estado concurrente durante una llamada nativa de varios segundos, la ronda A RA-008 y la batería de tramas en bruto del cierre de runtime `R04`: 17 tramas mal formadas, sobredimensionadas, con protocolo incorrecto, con comando desconocido, sin vincular y con sesión incorrecta, cada una respondida con su error tipado y seguida de un `session.status` correcto; un cliente bloqueado no bloquea a los demás; una detención vinculada durante una generación en curso finaliza la sesión) |

<a id="pipe-name-derivation"></a>
## Derivación del nombre de la canalización

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` es la definición única de la identidad de la instalación, que también usa el lease de la instalación: resuelve la ruta completa (incluidos los segmentos `.` y `..`, `/` o `\` y los separadores repetidos) y elimina los separadores finales, salvo en la raíz de una unidad. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` y `d:\omsi 2` producen el mismo nombre; `D:\OMSI 2` y `E:\OMSI 2` producen nombres distintos. El cliente siempre deriva el nombre de la instalación que contiene el ejecutable que está ejecutando (`AppContext.BaseDirectory`), salvo que el llamador de la API pase una raíz explícita. El `0.1` dentro del nombre es la versión del protocolo, de modo que un protocolo futuro pueda coexistir en el mismo ordenador.

<a id="framing-and-encoding"></a>
## Tramas y codificación

- Escritor: serializa el registro con las opciones predeterminadas de `System.Text.Json`, comprueba `length <= 65536` (en caso contrario, `OL_E_CONTROL_MESSAGE_TOO_LARGE`), escribe `BitConverter.GetBytes((int)length)` (4 bytes, little-endian en Windows), escribe el payload y vacía el búfer.
- Lector: lee exactamente 4 bytes; rechaza `length < 0` o `length > 65536` con `OL_E_CONTROL_MESSAGE_INVALID`; lee exactamente `length` bytes; deserializa. El propietario descarta en silencio (sin respuesta) una conexión que se cierra antes de haberse leído una trama completa. Una trama rechazada se responde incluso cuando el cliente ya ha escrito más bytes de los que lee el propietario: los búferes de la canalización contienen una trama completa, de modo que la escritura del cliente se completa y este puede leer la respuesta (cierre de runtime BUG-02; antes de la corrección, ese cliente y el propietario se bloqueaban ambos en sus escrituras).
- El propietario escribe cada respuesta con un plazo de 5 s: un cliente que se conecta, envía una solicitud y nunca lee la respuesta no puede retener una tarea del propietario durante más tiempo.
- Los nombres de propiedad de la solicitud están en **PascalCase y distinguen mayúsculas de minúsculas** (`ProtocolVersion`, `Command`, `Arguments`), porque el propietario deserializa con las opciones predeterminadas. Las respuestas usan `Ok`, `Result`, `ErrorCode`, `Message`. Los valores de enumeración se serializan como enteros; los `Guid`s, como cadenas.
- Una solicitud por conexión: el propietario lee una solicitud, escribe una respuesta y cierra la canalización. Abre una conexión nueva para cada solicitud.

<a id="request"></a>
### Solicitud

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` debe ser el id que devuelve `session.status` para la sesión en ejecución (el valor anterior procede de una sesión real).

`Arguments` es opcional (`null` u omitido) para `session.status` y `session.events`. Las claves de los argumentos se comparan de forma ordinal (distinguiendo mayúsculas de minúsculas).

<a id="response"></a>
### Respuesta

Tramas capturadas de un propietario real (cierre de runtime `R04`, paquete final). Un `runtime.execute` correcto:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Un `session.stop` rechazado por no llevar el `session_id` activo:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Comandos

| Comando | Argumentos | Resultado con `Ok=true` | Notas |
|---|---|---|---|
| `session.status` | ninguno | `SessionStatus`: `SessionId` (GUID como cadena), `State` (`SessionState` entero; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (array de `{Code, Message, Data}`), `RuntimeEvents` (array) | Solo lectura. Los clientes lo leen primero para obtener el `SessionId`. |
| `session.events` | ninguno | Array de `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (`int64` monótono), `Data` (mapa de cadenas) | Solo lectura, lista acotada; `events watch` lo sondea e imprime las entradas con un `Sequence` superior al último visto. |
| `session.stop` | `session_id` (obligatorio) | `{"accepted": true, "session_id": "..."}` | Solicita la detención canónica (`TerminateProcess` sobre OMSI y después restauración). Vuelve inmediatamente; el estado de la sesión se puede observar a través de `session.status` hasta que desaparece el endpoint. |
| `runtime.execute` | `operation` (obligatorio), `session_id` (obligatorio), más los argumentos propios de la operación (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (asignado por el propietario, a partir de `50001`), `Succeeded`, `ErrorCode`, `Values` (mapa de cadenas) | Se valida con `PublicCapabilityRegistry.ValidateRuntimeArguments` antes de ejecutarse: operación desconocida o interna → `OL_E_RUNTIME_OPERATION_UNKNOWN`; falta un valor obligatorio → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout de 8 s, 30 s para `road-vehicles.spawn`. Un fallo de runtime se responde como `Ok=false`, con `Result` igual al `RuntimeCommandResult`, `ErrorCode` igual a su código y `Message` igual a `Runtime operation was rejected.` La API elimina las claves del resultado que empiezan por `internal_` o terminan en `_address`, `_pointer`, `_vmt` antes de que lleguen a la canalización. |
| cualquier otro | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

El propietario atiende las conexiones de forma concurrente (cada cliente aceptado se atiende en su propia tarea), de modo que `session.status` sigue respondiendo mientras está en curso una llamada nativa de varios segundos como `road-vehicles.spawn`.

<a id="session-id-binding"></a>
## Vinculación al id de sesión

Los comandos que modifican deben nombrar la sesión sobre la que actúan. La CLI implementa `TryRequestBoundAsync`: envía `session.status` (750 ms), toma `Result.SessionId` y repite la solicitud añadiendo `session_id` a `Arguments`. Un id ausente, no analizable o distinto se responde con `OL_E_CONTROL_SESSION_MISMATCH`. Si el propietario no informa de un id de sesión, el cliente notifica `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Metadatos de la respuesta e historial de eventos truncado

`Metadata` es `null` salvo que la respuesta contenga un hecho sobre sí misma. `session.status` y `session.events` devuelven el historial de eventos del propietario (256 eventos como máximo, el más reciente al final). Cuando ese historial no cabe en una trama de 64 KiB, se eliminan los eventos más antiguos hasta que cabe y `Metadata` lo indica:

| Clave | Significado |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Eventos incluidos en esta respuesta |
| `events_dropped_count` | Eventos más antiguos eliminados para que quepa la trama |
| `events_available_count` | Eventos que tenía el propietario para esta respuesta |
| `events_first_returned_sequence` | `Sequence` del primer evento devuelto |

Sin `Metadata`, la respuesta no ha sido truncada por el plano de control. `Sequence` es monótono en ambos casos, por lo que un cliente también puede detectar los eventos que el propio propietario expulsó más allá de su historial de 256 eventos. La CLI imprime estos metadatos en el envelope `--json` y una nota en el modo de texto.

<a id="error-codes"></a>
## Códigos de error

| Código | Origen | Significado |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | propietario / cliente | El `ProtocolVersion` de la solicitud no es `0.1`; la respuesta del propietario no se pudo decodificar o estaba vacía; el propietario cerró la conexión sin responder; la conexión se rompió después de establecerse; o el propietario no informó de ningún id de sesión. |
| `OL_E_CONTROL_MESSAGE_INVALID` | propietario / cliente | Prefijo de longitud fuera de rango (una trama de solicitud sobredimensionada se rechaza así antes de leer su payload), trama vacía, JSON `null`, solicitud sin `Command` o JSON que no se pudo decodificar. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | escritor del cliente | La propia solicitud del cliente supera 65,536 bytes; se notifica al cliente y no se envía nada. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | escritor del propietario | La respuesta del propietario supera 65,536 bytes; el propietario responde con este error tipado. `session.status` y `session.events` recortan su historial de eventos (empezando por los más antiguos) para que quepa, por lo que no lo producen. |
| `OL_E_CONTROL_SESSION_MISMATCH` | propietario | `session.stop` / `runtime.execute` sin el `session_id` activo. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | propietario | Nombre de comando no admitido. |
| `OL_E_CONTROL_HANDLER_FAILED` | propietario | La respuesta del manejador no se pudo serializar, o el manejador lanzó una excepción sin un código `OL_E_` en su mensaje; si lleva un código, se devuelve ese código en su lugar (por ejemplo `OL_E_SESSION_NOT_RUNNING` de `ExecuteRuntimeAsync` después de que OMSI haya terminado). |
| `OL_E_CONTROL_FAILED` | cliente de la CLI | Código predeterminado cuando una respuesta es `Ok=false` sin `ErrorCode`. |
| `OL_E_TIMEOUT` | cliente | El cliente estaba conectado a un propietario que no respondió dentro del timeout. |
| `OL_E_NO_ACTIVE_SESSION` | cliente de la CLI | No se pudo alcanzar ningún endpoint de propietario (`TryRequestAsync` devuelve `null` solo cuando falla la propia conexión). Una vez establecida la conexión, todo fallo es uno de los códigos tipados anteriores. Salida `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | cliente y propietario | Validación de la superficie pública de `runtime.execute` (salida `2` en la CLI). |

Todo fallo conocido se responde con un error tipado, de modo que un cliente nunca confunde un fallo con la ausencia de un propietario. Dos casos terminan sin trama de respuesta: un cliente que se desconecta antes de enviar una solicitud completa (no hay nadie a quien responder) y una solicitud que sigue en curso cuando la sesión finaliza (el propietario cancela las respuestas pendientes mientras cierra el endpoint; el cliente ve el fin del flujo, que el cliente de la CLI notifica como `OL_E_CONTROL_PROTOCOL` «The owner closed the connection without a reply»). Un cliente que envió una solicitud y se desconecta antes de leer la respuesta no afecta al propietario. El inicio de un propietario se niega a continuar cuando cualquier respuesta (incluido un error tipado) procede del endpoint de la instalación, porque una respuesta demuestra que hay un propietario presente. La CLI asigna las respuestas a códigos de salida como se describe en [códigos de salida](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Modelo de confianza (riesgo aceptado)

- `PipeOptions.CurrentUserOnly` restringe la canalización al usuario de Windows (y al nivel de integridad) que es propietario de la sesión; en el lado del cliente, la misma opción verifica que el servidor pertenece al mismo usuario.
- **Cualquier proceso que se ejecute con el mismo usuario de Windows puede leer el estado, detener la sesión y ejecutar operaciones de runtime públicas.** No hay autenticación adicional, token ni autorización por cliente. Es un riesgo documentado y aceptado para la beta; no ejecutes sesiones de OmsiLaunch con una cuenta compartida con software que no sea de confianza.
- La canalización nunca expone direcciones nativas, handles ni operaciones de memoria en bruto; solo se aceptan los ids de operación públicos de `PublicCapabilityRegistry`, y las primitivas internas de investigación (`internal.*`) se rechazan antes de buscar ninguna sesión.
- Los mensajes están acotados (64 KiB) y enmarcados en tramas; una trama mal formada o sobredimensionada se responde o se descarta sin afectar a la sesión.
- Si otro proceso ya posee el nombre de la canalización cuando el propietario empieza a escuchar (`IOException`/`UnauthorizedAccessException` al crearla), el propietario sigue ejecutando la sesión sin endpoint y registra el fallo en `LocalControlPlane.ListenFault`; los clientes ven entonces `OL_E_NO_ACTIVE_SESSION`. Usa el icono de la bandeja o Ctrl+C para finalizar una sesión así.

<a id="availability-window"></a>
## Ventana de disponibilidad

1. Se ejecuta `StartSessionAsync`; el endpoint aún no existe. Un segundo lanzamiento de `OmsiLaunch.exe` para la misma raíz sondea `session.status` durante 250 ms antes de iniciarse y falla con `OL_E_SESSION_ALREADY_ACTIVE` solo cuando ya responde un propietario; el lease de la instalación serializa a dos propietarios que compiten durante el arranque (`OL_E_INSTALLATION_BUSY`).
2. La sesión llega a `Running`; los lotes de validación (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), si se han solicitado, terminan.
3. `LocalControlPlane.Start()`: el endpoint acepta conexiones. La propia operación `/runtime:` del propietario, si la hay, se ejecuta después de este punto.
4. El endpoint sigue activo durante `ProcessExited`, `Restoring` y `CleaningRuntime` (el estado informa de esos estados), hasta que la sesión está en `Completed` o `Failed`.
5. `DisposeAsync` cancela el listener, espera a las tareas de cliente en curso y el propietario llama a `CloseAsync`. A partir de ahí, el nombre de la canalización deja de existir.

Los clientes deberían usar timeouts de conexión cortos (la CLI usa 750 ms para status/stop/events) y tratar «no hay endpoint» como «no hay ninguna sesión activa para esta instalación».

<a id="using-the-protocol-from-other-tooling"></a>
## Uso del protocolo desde otras herramientas

<a id="c-net-6-or-later"></a>
### C# (.NET 6 o posterior)

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

Los objetos anónimos se serializan como `ProtocolVersion`/`Command`/`Arguments`, exactamente como espera el propietario. Una `TimeoutException`/`OperationCanceledException` en `ConnectAsync` significa que no hay ningún propietario para esa raíz.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` y `Convert.ToHexString` requieren .NET 5 o posterior, por lo que este ejemplo necesita PowerShell 7; Windows PowerShell 5.1 (.NET Framework) no puede establecer `CurrentUserOnly`.

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

`ConvertTo-Json` conserva las mayúsculas y minúsculas de las claves de la tabla hash, de modo que `ProtocolVersion`, `Command` y `Arguments` se emiten tal como están escritas.

<a id="using-the-cli-as-the-client"></a>
### Uso de la CLI como cliente

Cuando no se necesita un cliente propio, invoca `OmsiLaunch.exe` desde el directorio de la instalación: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. La CLI realiza el handshake de estado/vinculación y asigna las respuestas a [códigos de salida](exit-codes.md) (`0` correcto, `2` validación de argumentos, `4` sin propietario, `7` rechazado).

<a id="relationship-to-other-channels"></a>
## Relación con otros canales

- El buzón de runtime entre el propietario y el plugin dentro del proceso (mapeado en memoria, 64 KiB, de un solo vuelo, vinculado a la sesión) es un canal privado e independiente; el plano de control solo reenvía hacia él a través de `IOmsiLaunch.ExecuteRuntimeAsync`.
- El lease de la instalación (`Local\OmsiLaunch.Installation.<sha256(root)>`) es un semáforo con nombre, no forma parte de este protocolo; garantiza un único propietario por instalación y por sesión de inicio de sesión de Windows.
- El elemento «End session» (finalizar la sesión) del icono de la bandeja y el `session.stop` del plano de control señalizan la misma solicitud de detención en el lado del propietario; consulta [bandeja de Windows](windows-tray.md).
