# Plano de controle local

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Tradução da [página original em inglês](../../../reference/local-control.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O plano de controle local é o endpoint de named pipe (pipe nomeado) pelo qual um proprietário do OmsiLaunch em execução (o processo que iniciou uma sessão) aceita comandos semânticos de sessão de outros processos na mesma máquina: status, eventos, parada e operações públicas de runtime. Esta página especifica o endpoint conforme implementado em `tools\OmsiLaunch.Cli\LocalControlPlane.cs` e o manipulador de requisições em `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): nomenclatura do pipe, framing, versão do protocolo, comandos, vínculo com a sessão, códigos de erro, modelo de confiança, janela de disponibilidade e como se comunicar com ele a partir de outras ferramentas. Os comandos de cliente da CLI (`session status`, `session stop`, `events read`, `events watch`, rotas como `time get`) são wrappers finos sobre este protocolo; veja a [referência da CLI](cli.md). As operações de runtime em si são especificadas em [controle de runtime](runtime-control.md); a alternativa dentro do processo para integradores é a [API pública](public-api.md).

<a id="summary"></a>
## Resumo

| Propriedade | Valor |
|---|---|
| Transporte | Named pipe do Windows, `PipeDirection.InOut`, modo byte, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, buffers de entrada e saída de um frame (65,540 bytes) |
| Nome do pipe | `OmsiLaunch.Control.0.1.<key>`, em que `<key>` são os primeiros 16 caracteres hexadecimais do SHA-256 sobre os bytes UTF-8 do caminho completo da raiz da instalação, sem o `\` final, em maiúsculas (`LocalControlPlane.PipeNameFor`) |
| Framing | Prefixo de comprimento `int32` little-endian de 4 bytes seguido desse número de bytes de JSON UTF-8; uma requisição e uma resposta por conexão |
| Mensagem máxima | 65,536 bytes para uma requisição e para uma resposta (`MaxMessageBytes`) |
| Versão do protocolo | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); uma divergência é respondida com `OL_E_CONTROL_PROTOCOL` |
| Comandos | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Vínculo com a sessão | `session.stop` e `runtime.execute` exigem `Arguments.session_id` igual ao id da sessão ativa |
| Disponibilidade | Desde o momento em que a sessão chegou a `Running` (após qualquer batch de validação) até a sessão chegar a `Completed` ou `Failed` e o proprietário descartar o endpoint |
| Escopo | Um endpoint por raiz de instalação; duas instalações do mesmo usuário nunca compartilham um pipe |
| Estabilidade | STABLE_BETA (testes offline em `OmsiLaunch.WindowsUiTests`; evidência de runtime: RV-003, status concorrente durante uma chamada nativa de vários segundos, Rodada A RA-008, e a bateria de frames brutos `R04` do fechamento de runtime: 17 frames malformados, grandes demais, de protocolo errado, de comando desconhecido, não vinculados e de sessão errada, cada um respondido com seu erro tipado e seguido de um `session.status` bem-sucedido; um cliente travado não bloqueia os demais; uma parada vinculada durante um spawn em andamento encerra a sessão) |

<a id="pipe-name-derivation"></a>
## Derivação do nome do pipe

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` é a definição única da identidade da instalação, também usada pelo lease da instalação: ela resolve o caminho completo (incluindo segmentos `.` e `..`, `/` ou `\`, separadores repetidos) e remove os separadores finais, exceto no caso de uma raiz de unidade. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` e `d:\omsi 2` produzem o mesmo nome; `D:\OMSI 2` e `E:\OMSI 2` produzem nomes diferentes. O cliente sempre deriva o nome da instalação que contém o executável que ele executa (`AppContext.BaseDirectory`), a menos que o chamador da API passe uma raiz explícita. O `0.1` dentro do nome é a versão do protocolo, de modo que um protocolo futuro pode coexistir na mesma máquina.

<a id="framing-and-encoding"></a>
## Framing e codificação

- Escrita: serialize o registro com as opções padrão de `System.Text.Json`, verifique `length <= 65536` (caso contrário, `OL_E_CONTROL_MESSAGE_TOO_LARGE`), grave `BitConverter.GetBytes((int)length)` (4 bytes, little-endian no Windows), grave o payload e faça flush.
- Leitura: leia exatamente 4 bytes; rejeite `length < 0` ou `length > 65536` com `OL_E_CONTROL_MESSAGE_INVALID`; leia exatamente `length` bytes; desserialize. Uma conexão que é fechada antes de um frame completo ter sido lido é descartada silenciosamente pelo proprietário (sem resposta). Um frame rejeitado é respondido mesmo quando o cliente já gravou mais bytes do que o proprietário lê: os buffers do pipe comportam um frame inteiro, então a escrita do cliente é concluída e ele consegue ler a resposta (fechamento de runtime BUG-02; antes da correção, tal cliente e o proprietário ficavam ambos bloqueados em suas escritas).
- O proprietário grava cada resposta com um prazo de 5 s: um cliente que se conecta, envia uma requisição e nunca lê a resposta não consegue prender uma tarefa do proprietário por mais tempo que isso.
- Os nomes de propriedades são **PascalCase e diferenciam maiúsculas de minúsculas** na requisição (`ProtocolVersion`, `Command`, `Arguments`), porque o proprietário desserializa com as opções padrão. As respostas usam `Ok`, `Result`, `ErrorCode`, `Message`. Valores de enum são serializados como inteiros; `Guid`s, como strings.
- Uma requisição por conexão: o proprietário lê uma requisição, grava uma resposta e fecha o pipe. Abra uma nova conexão para cada requisição.

<a id="request"></a>
### Requisição

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` deve ser o id retornado por `session.status` para a sessão em execução (o valor acima vem de uma sessão real).

`Arguments` é opcional (`null` ou omitido) para `session.status` e `session.events`. As chaves de argumento são comparadas ordinalmente (diferenciando maiúsculas de minúsculas).

<a id="response"></a>
### Resposta

Frames capturados de um proprietário real (fechamento de runtime `R04`, pacote final). Um `runtime.execute` bem-sucedido:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Um `session.stop` rejeitado sem o `session_id` ativo:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Comandos

| Comando | Argumentos | Resultado com `Ok=true` | Observações |
|---|---|---|---|
| `session.status` | nenhum | `SessionStatus`: `SessionId` (GUID em string), `State` (`SessionState` inteiro; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (array de `{Code, Message, Data}`), `RuntimeEvents` (array) | Somente leitura. Os clientes leem isto primeiro para obter o `SessionId`. |
| `session.events` | nenhum | Array de `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (`int64` monotônico), `Data` (mapa de strings) | Somente leitura, lista limitada; `events watch` faz polling dele e imprime as entradas com `Sequence` acima da última vista. |
| `session.stop` | `session_id` (obrigatório) | `{"accepted": true, "session_id": "..."}` | Solicita a parada canônica (`TerminateProcess` no OMSI, depois restauração). Retorna imediatamente; o estado da sessão pode ser observado por meio de `session.status` até o endpoint desaparecer. |
| `runtime.execute` | `operation` (obrigatório), `session_id` (obrigatório), mais os argumentos próprios da operação (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (atribuído pelo proprietário, começando em `50001`), `Succeeded`, `ErrorCode`, `Values` (mapa de strings) | Validado com `PublicCapabilityRegistry.ValidateRuntimeArguments` antes da execução: operação desconhecida ou interna → `OL_E_RUNTIME_OPERATION_UNKNOWN`; valor obrigatório ausente → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout de 8 s, 30 s para `road-vehicles.spawn`. Uma falha de runtime é respondida como `Ok=false`, com `Result` definido como o `RuntimeCommandResult`, `ErrorCode` como o seu código e `Message` como `Runtime operation was rejected.` Chaves de resultado que começam com `internal_` ou terminam com `_address`, `_pointer`, `_vmt` são removidas pela API antes de chegarem ao pipe. |
| qualquer outro | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

O proprietário atende conexões de forma concorrente (cada cliente aceito é atendido em sua própria tarefa), de modo que `session.status` continua respondendo enquanto uma chamada nativa de vários segundos, como `road-vehicles.spawn`, está em andamento.

<a id="session-id-binding"></a>
## Vínculo com o id da sessão

Comandos que alteram estado precisam nomear a sessão sobre a qual atuam. A CLI implementa `TryRequestBoundAsync`: ela envia `session.status` (750 ms), pega `Result.SessionId` e repete a requisição com `session_id` acrescentado a `Arguments`. Um id ausente, impossível de interpretar ou diferente é respondido com `OL_E_CONTROL_SESSION_MISMATCH`. Se o proprietário não informar um id de sessão, o cliente informa `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Metadados da resposta e histórico de eventos truncado

`Metadata` é `null`, a menos que a resposta traga uma informação sobre si mesma. `session.status` e `session.events` retornam o histórico de eventos do proprietário (no máximo 256 eventos, o mais recente por último). Quando esse histórico não cabe em um frame de 64 KiB, os eventos mais antigos são removidos até ele caber, e `Metadata` informa isso:

| Chave | Significado |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Eventos nesta resposta |
| `events_dropped_count` | Eventos mais antigos removidos para caber no frame |
| `events_available_count` | Eventos que o proprietário tinha para esta resposta |
| `events_first_returned_sequence` | `Sequence` do primeiro evento retornado |

Sem `Metadata`, a resposta não foi truncada pelo plano de controle. `Sequence` é monotônico nos dois casos, de modo que um cliente também consegue detectar eventos que o próprio proprietário descartou além do seu histórico de 256 eventos. A CLI imprime esses metadados no envelope `--json` e uma observação no modo texto.

<a id="error-codes"></a>
## Códigos de erro

| Código | Origem | Significado |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | proprietário / cliente | O `ProtocolVersion` da requisição não é `0.1`; a resposta do proprietário não pôde ser decodificada ou estava vazia; o proprietário fechou a conexão sem responder; a conexão caiu depois de estabelecida; ou o proprietário não informou nenhum id de sessão. |
| `OL_E_CONTROL_MESSAGE_INVALID` | proprietário / cliente | Prefixo de comprimento fora do intervalo (um frame de requisição grande demais é recusado dessa forma antes de o payload ser lido), frame vazio, JSON `null`, requisição sem `Command` ou JSON que não pôde ser decodificado. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | escrita do cliente | A própria requisição do cliente excede 65,536 bytes; isso é informado ao cliente e nada é enviado. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | escrita do proprietário | A resposta do proprietário excede 65,536 bytes; o proprietário responde com este erro tipado. `session.status` e `session.events` reduzem o histórico de eventos (os mais antigos primeiro) para caber, portanto não o produzem. |
| `OL_E_CONTROL_SESSION_MISMATCH` | proprietário | `session.stop` / `runtime.execute` sem o `session_id` ativo. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | proprietário | Nome de comando não suportado. |
| `OL_E_CONTROL_HANDLER_FAILED` | proprietário | A resposta do manipulador não pôde ser serializada, ou o manipulador lançou uma exceção sem um código `OL_E_` na mensagem; com um código, é esse código que é retornado (por exemplo `OL_E_SESSION_NOT_RUNNING` de `ExecuteRuntimeAsync` depois que o OMSI saiu). |
| `OL_E_CONTROL_FAILED` | cliente da CLI | Código padrão quando uma resposta é `Ok=false` sem `ErrorCode`. |
| `OL_E_TIMEOUT` | cliente | O cliente estava conectado a um proprietário que não respondeu dentro do timeout. |
| `OL_E_NO_ACTIVE_SESSION` | cliente da CLI | Nenhum endpoint de proprietário pôde ser alcançado (`TryRequestAsync` só retorna `null` quando a própria conexão falha). Uma vez conectado, toda falha é um dos códigos tipados acima. Saída `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | cliente e proprietário | Validação da superfície pública de `runtime.execute` (saída `2` na CLI). |

Toda falha conhecida é respondida com um erro tipado, para que um cliente nunca confunda uma falha com a ausência de proprietário. Dois casos terminam sem frame de resposta: um cliente que se desconecta antes de enviar uma requisição completa (não há a quem responder) e uma requisição ainda em andamento quando a sessão termina (o proprietário cancela as respostas pendentes enquanto desativa o endpoint; o cliente vê o fim do stream, que o cliente da CLI informa como `OL_E_CONTROL_PROTOCOL` "The owner closed the connection without a reply"). Um cliente que enviou uma requisição e se desconecta antes de ler a resposta não afeta o proprietário. O início de um proprietário se recusa a prosseguir quando qualquer resposta (incluindo um erro tipado) vem do endpoint da instalação, porque uma resposta prova que há um proprietário presente. A CLI mapeia as respostas para códigos de saída conforme descrito em [códigos de saída](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Modelo de confiança (risco aceito)

- `PipeOptions.CurrentUserOnly` restringe o pipe ao usuário do Windows (e ao nível de integridade) que é proprietário da sessão; no lado do cliente, a mesma opção verifica se o servidor pertence ao mesmo usuário.
- **Qualquer processo executado como o mesmo usuário do Windows pode ler o status, parar a sessão e executar operações públicas de runtime.** Não há autenticação adicional, token nem autorização por cliente. Este é um risco documentado e aceito para o beta; não execute sessões do OmsiLaunch em uma conta compartilhada com software não confiável.
- O pipe nunca expõe endereços nativos, handles ou operações brutas de memória; somente os ids de operação públicos de `PublicCapabilityRegistry` são aceitos, e as primitivas internas de pesquisa (`internal.*`) são rejeitadas antes de qualquer consulta de sessão.
- As mensagens são limitadas (64 KiB) e enquadradas em frames; um frame malformado ou grande demais é respondido ou descartado sem afetar a sessão.
- Se o nome do pipe já pertencer a outro processo quando o proprietário começar a escutar (`IOException`/`UnauthorizedAccessException` na criação), o proprietário continua executando a sessão sem endpoint e registra a falha em `LocalControlPlane.ListenFault`; os clientes então veem `OL_E_NO_ACTIVE_SESSION`. Use o ícone da bandeja ou Ctrl+C para encerrar uma sessão assim.

<a id="availability-window"></a>
## Janela de disponibilidade

1. `StartSessionAsync` é executado; o endpoint ainda não existe. Uma segunda inicialização de `OmsiLaunch.exe` para a mesma raiz sonda `session.status` por 250 ms antes de iniciar e falha com `OL_E_SESSION_ALREADY_ACTIVE` somente quando um proprietário já responde; dois proprietários concorrendo durante a inicialização são serializados pelo lease da instalação (`OL_E_INSTALLATION_BUSY`).
2. A sessão chega a `Running`; os batches de validação (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), quando solicitados, são concluídos.
3. `LocalControlPlane.Start()`: o endpoint aceita conexões. A operação `/runtime:` do próprio proprietário, se houver, é executada depois deste ponto.
4. O endpoint permanece ativo durante `ProcessExited`, `Restoring` e `CleaningRuntime` (o status informa esses estados), até a sessão estar `Completed` ou `Failed`.
5. `DisposeAsync` cancela o listener, aguarda as tarefas de clientes em andamento, e o proprietário chama `CloseAsync`. Depois disso, o nome do pipe deixa de existir.

Os clientes devem usar timeouts de conexão curtos (a CLI usa 750 ms para status/stop/events) e tratar "nenhum endpoint" como "nenhuma sessão ativa para esta instalação".

<a id="using-the-protocol-from-other-tooling"></a>
## Uso do protocolo a partir de outras ferramentas

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

Os objetos anônimos são serializados como `ProtocolVersion`/`Command`/`Arguments` exatamente como o proprietário espera. Um `TimeoutException`/`OperationCanceledException` em `ConnectAsync` significa que não há proprietário para essa raiz.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` e `Convert.ToHexString` exigem .NET 5 ou posterior, por isso este exemplo precisa do PowerShell 7; o Windows PowerShell 5.1 (.NET Framework) não consegue definir `CurrentUserOnly`.

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

`ConvertTo-Json` mantém a grafia das chaves da hashtable, de modo que `ProtocolVersion`, `Command` e `Arguments` são emitidos exatamente como escritos.

<a id="using-the-cli-as-the-client"></a>
### Uso da CLI como cliente

Quando não é necessário um cliente personalizado, invoque o `OmsiLaunch.exe` a partir do diretório da instalação: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. A CLI realiza o handshake de status/vínculo e mapeia as respostas para [códigos de saída](exit-codes.md) (`0` ok, `2` validação de argumentos, `4` nenhum proprietário, `7` rejeitado).

<a id="relationship-to-other-channels"></a>
## Relação com outros canais

- O mailbox de runtime entre o proprietário e o plugin dentro do processo (mapeado em memória, 64 KiB, single-flight, vinculado à sessão) é um canal separado e privado; o plano de controle apenas encaminha para ele por meio de `IOmsiLaunch.ExecuteRuntimeAsync`.
- O lease da instalação (`Local\OmsiLaunch.Installation.<sha256(root)>`) é um semáforo nomeado, não faz parte deste protocolo; ele garante um único proprietário por instalação por sessão de logon.
- A opção "End session" do ícone da bandeja e o `session.stop` do plano de controle sinalizam a mesma requisição de parada do lado do proprietário; veja [Bandeja do Windows](windows-tray.md).
