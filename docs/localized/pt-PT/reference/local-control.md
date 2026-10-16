# Plano de controlo local

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Tradução da [página original em inglês](../../../reference/local-control.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

O plano de controlo local é o endpoint de named pipe através do qual um proprietário do OmsiLaunch em execução (o processo que iniciou uma sessão) aceita comandos semânticos de sessão de outros processos na mesma máquina: estado, eventos, paragem e operações de runtime públicas. Esta página especifica o endpoint tal como está implementado em `tools\OmsiLaunch.Cli\LocalControlPlane.cs` e o tratador de pedidos em `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): nomenclatura do pipe, framing, versão do protocolo, comandos, vinculação à sessão, códigos de erro, modelo de confiança, janela de disponibilidade e como comunicar com ele a partir de outras ferramentas. Os comandos do cliente CLI (`session status`, `session stop`, `events read`, `events watch`, rotas como `time get`) são invólucros finos sobre este protocolo; ver a [referência da CLI](cli.md). As operações de runtime propriamente ditas estão especificadas em [controlo de runtime](runtime-control.md); a alternativa dentro do processo para integradores é a [API pública](public-api.md).

<a id="summary"></a>
## Resumo

| Propriedade | Valor |
|---|---|
| Transporte | Named pipe do Windows, `PipeDirection.InOut`, modo de bytes, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, buffers de entrada e de saída com o tamanho de um frame (65,540 bytes) |
| Nome do pipe | `OmsiLaunch.Control.0.1.<key>`, em que `<key>` são os primeiros 16 caracteres hexadecimais do SHA-256 sobre os bytes UTF-8 do caminho completo da raiz da instalação, sem `\` final, em maiúsculas (`LocalControlPlane.PipeNameFor`) |
| Framing | Prefixo de comprimento `int32` little-endian de 4 bytes, seguido desse número de bytes de JSON em UTF-8; um pedido e uma resposta por ligação |
| Mensagem máxima | 65,536 bytes para um pedido e para uma resposta (`MaxMessageBytes`) |
| Versão do protocolo | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); uma discrepância é respondida com `OL_E_CONTROL_PROTOCOL` |
| Comandos | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Vinculação à sessão | `session.stop` e `runtime.execute` exigem `Arguments.session_id` igual ao id da sessão ativa |
| Disponibilidade | Desde o momento em que a sessão atingiu `Running` (após qualquer lote de validação) até a sessão atingir `Completed` ou `Failed` e o proprietário descartar o endpoint |
| Âmbito | Um endpoint por raiz de instalação; duas instalações do mesmo utilizador nunca partilham um pipe |
| Estabilidade | STABLE_BETA (testes offline em `OmsiLaunch.WindowsUiTests`; evidência de runtime: RV-003, estado concorrente durante uma chamada nativa de vários segundos, Ronda A RA-008, e a bateria de frames brutos do fecho de runtime `R04`: 17 frames malformados, demasiado grandes, de protocolo errado, de comando desconhecido, não vinculados e de sessão errada, cada um respondido com o seu erro tipado e seguido de um `session.status` bem-sucedido; um cliente bloqueado não bloqueia os outros; uma paragem vinculada durante um spawn em curso termina a sessão) |

<a id="pipe-name-derivation"></a>
## Derivação do nome do pipe

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` é a definição única da identidade da instalação, também usada pela lease da instalação: resolve o caminho completo (incluindo segmentos `.` e `..`, `/` ou `\`, separadores repetidos) e remove os separadores finais, exceto numa raiz de unidade. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` e `d:\omsi 2` produzem o mesmo nome; `D:\OMSI 2` e `E:\OMSI 2` produzem nomes diferentes. O cliente deriva sempre o nome a partir da instalação que contém o executável que está a executar (`AppContext.BaseDirectory`), salvo se o chamador da API passar uma raiz explícita. O `0.1` dentro do nome é a versão do protocolo, pelo que um protocolo futuro pode coexistir na mesma máquina.

<a id="framing-and-encoding"></a>
## Framing e codificação

- Escritor: serializar o registo com as opções predefinidas de `System.Text.Json`, verificar `length <= 65536` (caso contrário, `OL_E_CONTROL_MESSAGE_TOO_LARGE`), escrever `BitConverter.GetBytes((int)length)` (4 bytes, little-endian no Windows), escrever o payload, fazer flush.
- Leitor: ler exatamente 4 bytes; rejeitar `length < 0` ou `length > 65536` com `OL_E_CONTROL_MESSAGE_INVALID`; ler exatamente `length` bytes; desserializar. Uma ligação que se feche antes de ter sido lido um frame completo é descartada silenciosamente pelo proprietário (sem resposta). Um frame rejeitado é respondido mesmo quando o cliente já escreveu mais bytes do que o proprietário lê: os buffers do pipe comportam um frame inteiro, pelo que a escrita do cliente termina e este pode ler a resposta (fecho de runtime BUG-02; antes da correção, esse cliente e o proprietário ficavam ambos bloqueados nas suas escritas).
- O proprietário escreve cada resposta com um prazo de 5 s: um cliente que se liga, envia um pedido e nunca lê a resposta não consegue reter uma tarefa do proprietário durante mais tempo.
- Os nomes das propriedades são **PascalCase e sensíveis a maiúsculas e minúsculas** no pedido (`ProtocolVersion`, `Command`, `Arguments`), porque o proprietário desserializa com as opções predefinidas. As respostas usam `Ok`, `Result`, `ErrorCode`, `Message`. Os valores de enumerações são serializados como inteiros; os `Guid`s como strings.
- Um pedido por ligação: o proprietário lê um pedido, escreve uma resposta e fecha o pipe. Abrir uma nova ligação para cada pedido.

<a id="request"></a>
### Pedido

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` tem de ser o id devolvido por `session.status` para a sessão em execução (o valor acima provém de uma sessão real).

`Arguments` é opcional (`null` ou omitido) para `session.status` e `session.events`. As chaves dos argumentos são comparadas de forma ordinal (sensível a maiúsculas e minúsculas).

<a id="response"></a>
### Resposta

Frames capturados de um proprietário real (fecho de runtime `R04`, pacote final). Um `runtime.execute` bem-sucedido:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Um `session.stop` rejeitado por não incluir o `session_id` ativo:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Comandos

| Comando | Argumentos | Resultado com `Ok=true` | Notas |
|---|---|---|---|
| `session.status` | nenhum | `SessionStatus`: `SessionId` (GUID em string), `State` (`SessionState` inteiro; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (array de `{Code, Message, Data}`), `RuntimeEvents` (array) | Só de leitura. Os clientes leem-no primeiro para obter o `SessionId`. |
| `session.events` | nenhum | Array de `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (`int64` monotónico), `Data` (mapa de strings) | Só de leitura, lista limitada; `events watch` consulta-o periodicamente e imprime as entradas com um `Sequence` superior ao último visto. |
| `session.stop` | `session_id` (obrigatório) | `{"accepted": true, "session_id": "..."}` | Pede a paragem canónica (`TerminateProcess` sobre o OMSI e, em seguida, restauro). Regressa imediatamente; o estado da sessão pode ser observado através de `session.status` até o endpoint desaparecer. |
| `runtime.execute` | `operation` (obrigatório), `session_id` (obrigatório), mais os argumentos próprios da operação (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (atribuído pelo proprietário, a partir de `50001`), `Succeeded`, `ErrorCode`, `Values` (mapa de strings) | Validado com `PublicCapabilityRegistry.ValidateRuntimeArguments` antes da execução: operação desconhecida ou interna → `OL_E_RUNTIME_OPERATION_UNKNOWN`; valor obrigatório em falta → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout de 8 s, 30 s para `road-vehicles.spawn`. Uma falha de runtime é respondida como `Ok=false`, com `Result` definido como o `RuntimeCommandResult`, `ErrorCode` com o respetivo código e `Message` `Runtime operation was rejected.` As chaves de resultado que começam por `internal_` ou terminam em `_address`, `_pointer`, `_vmt` são removidas pela API antes de chegarem ao pipe. |
| qualquer outro | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

O proprietário serve as ligações em simultâneo (cada cliente aceite é servido na sua própria tarefa), pelo que `session.status` continua a responder enquanto está em curso uma chamada nativa de vários segundos, como `road-vehicles.spawn`.

<a id="session-id-binding"></a>
## Vinculação ao id da sessão

Os comandos que alteram estado têm de indicar a sessão sobre a qual atuam. A CLI implementa `TryRequestBoundAsync`: envia `session.status` (750 ms), obtém `Result.SessionId` e repete o pedido com `session_id` acrescentado a `Arguments`. Um id em falta, impossível de interpretar ou diferente é respondido com `OL_E_CONTROL_SESSION_MISMATCH`. Se o proprietário não comunicar um id de sessão, o cliente comunica `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Metadados da resposta e histórico de eventos truncado

`Metadata` é `null`, salvo se a resposta transportar um facto sobre si própria. `session.status` e `session.events` devolvem o histórico de eventos do proprietário (no máximo 256 eventos, o mais recente em último). Quando esse histórico não cabe num frame de 64 KiB, os eventos mais antigos são removidos até caber e `Metadata` indica-o:

| Chave | Significado |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Eventos nesta resposta |
| `events_dropped_count` | Eventos mais antigos removidos para caber no frame |
| `events_available_count` | Eventos que o proprietário detinha para esta resposta |
| `events_first_returned_sequence` | `Sequence` do primeiro evento devolvido |

Sem `Metadata`, a resposta não foi truncada pelo plano de controlo. `Sequence` é monotónico em ambos os casos, pelo que um cliente também consegue detetar eventos que o próprio proprietário descartou para além do seu histórico de 256 eventos. A CLI imprime estes metadados no envelope `--json` e uma nota no modo de texto.

<a id="error-codes"></a>
## Códigos de erro

| Código | Origem | Significado |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | proprietário / cliente | O `ProtocolVersion` do pedido não é `0.1`; a resposta do proprietário não pôde ser descodificada ou estava vazia; o proprietário fechou a ligação sem responder; a ligação quebrou depois de ter sido estabelecida; ou o proprietário não comunicou nenhum id de sessão. |
| `OL_E_CONTROL_MESSAGE_INVALID` | proprietário / cliente | Prefixo de comprimento fora do intervalo (um frame de pedido demasiado grande é recusado desta forma antes de o seu payload ser lido), frame vazio, JSON `null`, pedido sem `Command`, ou JSON que não pôde ser descodificado. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | escritor do cliente | O próprio pedido do cliente excede 65,536 bytes; é comunicado ao cliente e nada é enviado. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | escritor do proprietário | A resposta do proprietário excede 65,536 bytes; o proprietário responde com este erro tipado. `session.status` e `session.events` reduzem o seu histórico de eventos (os mais antigos primeiro) para caber, pelo que não o produzem. |
| `OL_E_CONTROL_SESSION_MISMATCH` | proprietário | `session.stop` / `runtime.execute` sem o `session_id` ativo. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | proprietário | Nome de comando não suportado. |
| `OL_E_CONTROL_HANDLER_FAILED` | proprietário | A resposta do tratador não pôde ser serializada, ou o tratador lançou uma exceção sem um código `OL_E_` na mensagem; com um código, é esse código que é devolvido (por exemplo `OL_E_SESSION_NOT_RUNNING` de `ExecuteRuntimeAsync` depois de o OMSI ter saído). |
| `OL_E_CONTROL_FAILED` | cliente CLI | Código predefinido quando uma resposta é `Ok=false` sem `ErrorCode`. |
| `OL_E_TIMEOUT` | cliente | O cliente estava ligado a um proprietário que não respondeu dentro do timeout. |
| `OL_E_NO_ACTIVE_SESSION` | cliente CLI | Não foi possível alcançar nenhum endpoint de proprietário (`TryRequestAsync` só devolve `null` quando a própria ligação falha). Uma vez ligado, todas as falhas são um dos códigos tipados acima. Saída `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | cliente e proprietário | Validação de `runtime.execute` pela superfície pública (saída `2` na CLI). |

Todas as falhas conhecidas são respondidas com um erro tipado, para que um cliente nunca confunda uma falha com a ausência de proprietário. Dois casos terminam sem frame de resposta: um cliente que se desliga antes de enviar um pedido completo (não há ninguém a quem responder), e um pedido ainda em curso quando a sessão termina (o proprietário cancela as respostas pendentes enquanto encerra o endpoint; o cliente vê o fim do stream, que o cliente CLI comunica como `OL_E_CONTROL_PROTOCOL` "The owner closed the connection without a reply"). Um cliente que enviou um pedido e se desliga antes de ler a resposta não afeta o proprietário. O arranque de um proprietário recusa-se a prosseguir quando alguma resposta (incluindo um erro tipado) provém do endpoint da instalação, porque uma resposta prova que está presente um proprietário. A CLI faz corresponder as respostas a códigos de saída conforme descrito em [códigos de saída](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Modelo de confiança (risco aceite)

- `PipeOptions.CurrentUserOnly` restringe o pipe ao utilizador do Windows (e nível de integridade) que é proprietário da sessão; o lado do cliente da mesma opção verifica que o servidor pertence ao mesmo utilizador.
- **Qualquer processo em execução com o mesmo utilizador do Windows pode ler o estado, parar a sessão e executar operações de runtime públicas.** Não existe autenticação adicional, token nem autorização por cliente. Este é um risco documentado e aceite para a beta; não se devem executar sessões do OmsiLaunch numa conta partilhada com software não fidedigno.
- O pipe nunca expõe endereços nativos, handles nem operações sobre memória bruta; só são aceites os ids de operação públicos de `PublicCapabilityRegistry`, e as primitivas de investigação internas (`internal.*`) são rejeitadas antes de qualquer pesquisa de sessão.
- As mensagens são limitadas (64 KiB) e têm framing; um frame malformado ou demasiado grande é respondido ou descartado sem afetar a sessão.
- Se o nome do pipe já pertencer a outro processo quando o proprietário começa a escutar (`IOException`/`UnauthorizedAccessException` na criação), o proprietário continua a executar a sessão sem endpoint e regista a falha em `LocalControlPlane.ListenFault`; os clientes veem então `OL_E_NO_ACTIVE_SESSION`. Usar o ícone da área de notificação ou Ctrl+C para terminar uma sessão nessas condições.

<a id="availability-window"></a>
## Janela de disponibilidade

1. `StartSessionAsync` é executado; o endpoint ainda não existe. Um segundo lançamento de `OmsiLaunch.exe` para a mesma raiz sonda `session.status` durante 250 ms antes de iniciar e só falha com `OL_E_SESSION_ALREADY_ACTIVE` quando um proprietário já responde; dois proprietários em concorrência durante o arranque são serializados pela lease da instalação (`OL_E_INSTALLATION_BUSY`).
2. A sessão atinge `Running`; os lotes de validação (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), quando pedidos, terminam.
3. `LocalControlPlane.Start()`: o endpoint aceita ligações. A operação `/runtime:` do próprio proprietário, se existir, é executada depois deste ponto.
4. O endpoint mantém-se ativo durante `ProcessExited`, `Restoring` e `CleaningRuntime` (o estado comunica esses estados), até a sessão estar `Completed` ou `Failed`.
5. `DisposeAsync` cancela o listener, aguarda as tarefas de cliente em curso, e o proprietário chama `CloseAsync`. A partir daí, o nome do pipe deixa de existir.

Os clientes devem usar timeouts de ligação curtos (a CLI usa 750 ms para status/stop/events) e tratar «sem endpoint» como «nenhuma sessão ativa para esta instalação».

<a id="using-the-protocol-from-other-tooling"></a>
## Utilizar o protocolo a partir de outras ferramentas

<a id="c-net-6-or-later"></a>
### C# (.NET 6 ou posterior)

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

Os objetos anónimos são serializados como `ProtocolVersion`/`Command`/`Arguments`, exatamente como o proprietário espera. Uma `TimeoutException`/`OperationCanceledException` em `ConnectAsync` significa que não há proprietário para essa raiz.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` e `Convert.ToHexString` requerem .NET 5 ou posterior, pelo que este exemplo precisa do PowerShell 7; o Windows PowerShell 5.1 (.NET Framework) não consegue definir `CurrentUserOnly`.

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

`ConvertTo-Json` mantém as maiúsculas e minúsculas das chaves da hashtable, pelo que `ProtocolVersion`, `Command` e `Arguments` são emitidos tal como estão escritos.

<a id="using-the-cli-as-the-client"></a>
### Utilizar a CLI como cliente

Quando não é necessário um cliente próprio, basta invocar `OmsiLaunch.exe` a partir do diretório da instalação: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. A CLI realiza o handshake de estado/vinculação e faz corresponder as respostas a [códigos de saída](exit-codes.md) (`0` ok, `2` validação de argumentos, `4` sem proprietário, `7` rejeitado).

<a id="relationship-to-other-channels"></a>
## Relação com outros canais

- A mailbox de runtime entre o proprietário e o plugin dentro do processo (mapeada em memória, 64 KiB, de pedido único, vinculada à sessão) é um canal separado e privado; o plano de controlo apenas reencaminha para ela através de `IOmsiLaunch.ExecuteRuntimeAsync`.
- A lease da instalação (`Local\OmsiLaunch.Installation.<sha256(root)>`) é um semáforo com nome, que não faz parte deste protocolo; garante um único proprietário por instalação por sessão de início de sessão do Windows.
- A opção «End session» do ícone da área de notificação e o `session.stop` do plano de controlo sinalizam o mesmo pedido de paragem do lado do proprietário; ver [Indicador na área de notificação do Windows](windows-tray.md).
