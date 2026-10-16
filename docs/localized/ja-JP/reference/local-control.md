# ローカルコントロールプレーン

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> このページは OmsiLaunch 0.1.0-beta3 の[英語版の原文ページ](../../../reference/local-control.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

ローカルコントロールプレーンは、実行中の OmsiLaunch オーナー（セッションを開始したプロセス）が、同じマシン上の他のプロセスからセマンティックなセッションコマンド（状態取得、イベント、停止、公開ランタイム操作）を受け付けるための名前付きパイプのエンドポイントです。このページでは、`tools\OmsiLaunch.Cli\LocalControlPlane.cs` に実装されたエンドポイントと、`OwnerSession.RunAsync`（`tools\OmsiLaunch.Cli\Program.cs`）内の要求ハンドラーについて、パイプの命名、フレーミング、プロトコルバージョン、コマンド、セッションへのバインド、エラーコード、信頼モデル、利用可能な期間、および他のツールからの通信方法を規定します。CLI クライアントのコマンド（`session status`、`session stop`、`events read`、`events watch`、`time get` などのルート）は、このプロトコルの薄いラッパーです。[CLI リファレンス](cli.md)を参照してください。ランタイム操作そのものは[ランタイム制御](runtime-control.md)で規定されています。インテグレーター向けのプロセス内の代替手段は[公開 API](public-api.md)です。

<a id="summary"></a>
## 概要

| 項目 | 値 |
|---|---|
| トランスポート | Windows の名前付きパイプ、`PipeDirection.InOut`、バイトモード、`PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`、`MaxAllowedServerInstances`、入力・出力バッファーはそれぞれ 1 フレーム分（65,540 バイト） |
| パイプ名 | `OmsiLaunch.Control.0.1.<key>`。`<key>` は、インストールルートのフルパス（末尾の `\` を除去し、大文字化したもの）の UTF-8 バイト列に対する SHA-256 の先頭 16 桁の 16 進文字です（`LocalControlPlane.PipeNameFor`） |
| フレーミング | 4 バイトのリトルエンディアン `int32` の長さプレフィックスに続き、そのバイト数の UTF-8 JSON。1 回の接続につき要求 1 つと応答 1 つ |
| 最大メッセージ | 要求・応答ともに 65,536 バイト（`MaxMessageBytes`） |
| プロトコルバージョン | `"0.1"`（`PublicCapabilityRegistry.ProtocolVersion`）。不一致の場合は `OL_E_CONTROL_PROTOCOL` で応答します |
| コマンド | `session.status`、`session.events`、`session.stop`、`runtime.execute` |
| セッションへのバインド | `session.stop` と `runtime.execute` には、アクティブなセッション id と等しい `Arguments.session_id` が必要です |
| 利用可能な期間 | セッションが `Running` に達した時点（検証バッチがある場合はその後）から、セッションが `Completed` または `Failed` に達してオーナーがエンドポイントを破棄するまで |
| スコープ | インストールルートごとに 1 つのエンドポイント。同じユーザーの 2 つのインストール環境がパイプを共有することはありません |
| 安定性 | STABLE_BETA（`OmsiLaunch.WindowsUiTests` のオフラインテスト。ランタイムのエビデンス: RV-003 の数秒かかるネイティブ呼び出し中の同時状態取得、Round A RA-008、およびランタイムクロージャーの生フレーム検証セット `R04`: 不正な形式、サイズ超過、誤ったプロトコル、不明なコマンド、未バインド、誤ったセッションの 17 個のフレームにそれぞれ型付きエラーで応答し、その後の `session.status` が成功。停滞したクライアントが他のクライアントをブロックしないこと。スポーン実行中のバインド済み停止でセッションが終了すること） |

<a id="pipe-name-derivation"></a>
## パイプ名の導出

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` はインストール環境の識別の唯一の定義であり、インストールリースでも使用されます。フルパスを解決し（`.` と `..` のセグメント、`/` または `\`、連続した区切り文字を含む）、ドライブルートを除いて末尾の区切り文字を削除します。`D:\OMSI 2`、`D:\OMSI 2\`、`D:\OMSI 2\.`、`D:\x\..\OMSI 2`、`d:\omsi 2` は同じ名前になり、`D:\OMSI 2` と `E:\OMSI 2` は異なる名前になります。API の呼び出し元が明示的なルートを渡さない限り、クライアントは常に、実行している実行ファイルを含むインストール環境（`AppContext.BaseDirectory`）から名前を導出します。名前に含まれる `0.1` はプロトコルバージョンであり、将来のプロトコルが同じマシン上で共存できるようになっています。

<a id="framing-and-encoding"></a>
## フレーミングとエンコーディング

- 書き込み側: `System.Text.Json` の既定のオプションでレコードをシリアル化し、`length <= 65536` を確認し（満たさない場合は `OL_E_CONTROL_MESSAGE_TOO_LARGE`）、`BitConverter.GetBytes((int)length)`（4 バイト、Windows ではリトルエンディアン）を書き込み、ペイロードを書き込んでフラッシュします。
- 読み取り側: ちょうど 4 バイトを読み取り、`length < 0` または `length > 65536` の場合は `OL_E_CONTROL_MESSAGE_INVALID` で拒否し、ちょうど `length` バイトを読み取ってデシリアル化します。完全なフレームを読み取る前に閉じられた接続は、オーナーによって黙って破棄されます（応答なし）。拒否されたフレームには、オーナーが読み取る以上のバイトをクライアントが既に書き込んでいた場合でも応答します。パイプのバッファーは 1 フレーム全体を保持できるため、クライアントの書き込みは完了し、応答を読み取れます（ランタイムクロージャー BUG-02。修正前は、そのようなクライアントとオーナーの両方が書き込みでブロックしていました）。
- オーナーは各応答を 5 s の期限内に書き込みます。接続して要求を送信した後に応答を読み取らないクライアントが、それ以上長くオーナーのタスクを占有することはできません。
- 要求のプロパティ名は **PascalCase で、大文字と小文字が区別されます**（`ProtocolVersion`、`Command`、`Arguments`）。オーナーが既定のオプションでデシリアル化するためです。応答は `Ok`、`Result`、`ErrorCode`、`Message` を使用します。列挙値は整数として、`Guid` は文字列としてシリアル化されます。
- 1 回の接続につき要求は 1 つです。オーナーは要求を 1 つ読み取り、応答を 1 つ書き込んでパイプを閉じます。要求ごとに新しい接続を開いてください。

<a id="request"></a>
### 要求

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` は、実行中のセッションについて `session.status` が返した id である必要があります（上記の値は実際のセッションのものです）。

`session.status` と `session.events` では `Arguments` は省略可能です（`null` または省略）。引数のキーは序数比較されます（大文字と小文字を区別）。

<a id="response"></a>
### 応答

実際のオーナーから取得したフレームです（ランタイムクロージャー `R04`、最終パッケージ）。成功した `runtime.execute`:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

アクティブな `session_id` を含まずに拒否された `session.stop`:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## コマンド

| コマンド | 引数 | `Ok=true` の場合の結果 | 備考 |
|---|---|---|---|
| `session.status` | なし | `SessionStatus`: `SessionId`（文字列の GUID）、`State`（整数の `SessionState`。`14` = `Running`、`15` = `ProcessExited`、`16` = `Restoring`、`18` = `Completed`、`19` = `Failed`）、`Diagnostics`（`{Code, Message, Data}` の配列）、`RuntimeEvents`（配列） | 読み取り専用。クライアントは最初にこれを読み取って `SessionId` を取得します。 |
| `session.events` | なし | `RuntimeEvent` の配列: `Type`、`TimestampUtc`、`Sequence`（単調増加する `int64`）、`Data`（文字列マップ） | 読み取り専用、上限付きリスト。`events watch` はこれをポーリングし、最後に確認した値より大きい `Sequence` を持つエントリを出力します。 |
| `session.stop` | `session_id`（必須） | `{"accepted": true, "session_id": "..."}` | 正規停止（OMSI に対する `TerminateProcess`、その後の復元）を要求します。すぐに戻ります。エンドポイントが消えるまで、セッションの状態は `session.status` で確認できます。 |
| `runtime.execute` | `operation`（必須）、`session_id`（必須）、および操作自体の引数（`handle`、`name`、`value`、`model`、`family`、...） | `RuntimeCommandResult`: `SessionId`、`RequestId`（オーナーが割り当て、`50001` から開始）、`Succeeded`、`ErrorCode`、`Values`（文字列マップ） | 実行前に `PublicCapabilityRegistry.ValidateRuntimeArguments` で検証されます。不明な操作または内部操作 → `OL_E_RUNTIME_OPERATION_UNKNOWN`、必須の値の欠落 → `OL_E_RUNTIME_ARGUMENT_REQUIRED`。タイムアウトは 8 s、`road-vehicles.spawn` では 30 s です。ランタイムの失敗には `Ok=false` で応答し、`Result` に `RuntimeCommandResult`、`ErrorCode` にそのコード、`Message` に `Runtime operation was rejected.` を設定します。`internal_` で始まるか `_address`、`_pointer`、`_vmt` で終わる結果キーは、パイプに届く前に API によって除去されます。 |
| それ以外 | | `Ok=false`、`OL_E_CONTROL_COMMAND_UNKNOWN` | |

オーナーは接続を並行して処理します（受け付けた各クライアントを専用のタスクで処理します）。そのため、`road-vehicles.spawn` のような数秒かかるネイティブ呼び出しの実行中でも、`session.status` は応答し続けます。

<a id="session-id-binding"></a>
## セッション id のバインド

変更を伴うコマンドは、対象とするセッションを指定する必要があります。CLI は `TryRequestBoundAsync` を実装しています。`session.status` を送信し（750 ms）、`Result.SessionId` を取得して、`Arguments` に `session_id` を追加した要求を再送します。id が欠落している、解析できない、または異なる場合は `OL_E_CONTROL_SESSION_MISMATCH` で応答します。オーナーがセッション id を報告しない場合、クライアントは `OL_E_CONTROL_PROTOCOL` を報告します。

<a id="reply-metadata-and-truncated-event-history"></a>
### 応答のメタデータと切り詰められたイベント履歴

`Metadata` は、応答がそれ自体に関する事実を伝える場合を除いて `null` です。`session.status` と `session.events` はオーナーのイベント履歴（最大 256 件、最新が最後）を返します。その履歴が 1 つの 64 KiB フレームに収まらない場合、収まるまで最も古いイベントが削除され、`Metadata` にその旨が示されます。

| キー | 意味 |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | この応答に含まれるイベント数 |
| `events_dropped_count` | フレームに収めるために削除された最も古いイベントの数 |
| `events_available_count` | この応答のためにオーナーが保持していたイベント数 |
| `events_first_returned_sequence` | 最初に返されたイベントの `Sequence` |

`Metadata` がない場合、その応答はコントロールプレーンによって切り詰められていません。どちらの場合も `Sequence` は単調増加するため、クライアントは、オーナー自身が 256 件の履歴を超えて破棄したイベントも検出できます。CLI はこのメタデータを `--json` エンベロープに出力し、テキストモードでは注記を出力します。

<a id="error-codes"></a>
## エラーコード

| コード | 発生元 | 意味 |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | オーナー / クライアント | 要求の `ProtocolVersion` が `0.1` ではない、オーナーの応答をデコードできないか空だった、オーナーが応答せずに接続を閉じた、確立後に接続が切断された、またはオーナーがセッション id を報告しなかった。 |
| `OL_E_CONTROL_MESSAGE_INVALID` | オーナー / クライアント | 長さプレフィックスが範囲外（サイズ超過の要求フレームは、ペイロードを読み取る前にこの方法で拒否されます）、空のフレーム、JSON の `null`、`Command` のない要求、またはデコードできない JSON。 |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | クライアントの書き込み側 | クライアント自身の要求が 65,536 バイトを超えています。クライアントに報告され、何も送信されません。 |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | オーナーの書き込み側 | オーナーの応答が 65,536 バイトを超えています。オーナーはこの型付きエラーで応答します。`session.status` と `session.events` はイベント履歴を（古いものから）削って収めるため、このエラーは発生しません。 |
| `OL_E_CONTROL_SESSION_MISMATCH` | オーナー | アクティブな `session_id` を含まない `session.stop` / `runtime.execute`。 |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | オーナー | サポートされていないコマンド名。 |
| `OL_E_CONTROL_HANDLER_FAILED` | オーナー | ハンドラーの応答をシリアル化できなかった、またはハンドラーがメッセージに `OL_E_` コードを含まない例外をスローした。コードを含む場合は、代わりにそのコードが返されます（例: OMSI の終了後の `ExecuteRuntimeAsync` からの `OL_E_SESSION_NOT_RUNNING`）。 |
| `OL_E_CONTROL_FAILED` | CLI クライアント | 応答が `ErrorCode` なしの `Ok=false` だった場合の既定のコード。 |
| `OL_E_TIMEOUT` | クライアント | クライアントは、タイムアウト内に応答しないオーナーに接続していました。 |
| `OL_E_NO_ACTIVE_SESSION` | CLI クライアント | オーナーのエンドポイントに到達できませんでした（`TryRequestAsync` が `null` を返すのは接続自体が失敗した場合だけです）。接続後の失敗はすべて上記の型付きコードのいずれかになります。終了コード `4`。 |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | クライアントとオーナー | `runtime.execute` の公開サーフェスの検証（CLI では終了コード `2`）。 |

既知の失敗にはすべて型付きエラーで応答するため、クライアントが障害をオーナーの不在と取り違えることはありません。応答フレームなしで終わるケースは 2 つあります。完全な要求を送信する前に切断したクライアント（応答する相手がいない）と、セッション終了時にまだ処理中の要求（オーナーはエンドポイントを閉じる際に保留中の応答をキャンセルします。クライアントはストリームの終端を受け取り、CLI クライアントはこれを `OL_E_CONTROL_PROTOCOL`（メッセージ `The owner closed the connection without a reply`）として報告します）です。要求を送信した後、応答を読み取る前に切断したクライアントは、オーナーに影響を与えません。インストール環境のエンドポイントから何らかの応答（型付きエラーを含む）が返された場合、オーナーの開始処理は続行を拒否します。応答があることはオーナーが存在することの証明だからです。CLI による応答から終了コードへの対応付けは[終了コード](exit-codes.md)に記載しています。

<a id="trust-model-accepted-risk"></a>
## 信頼モデル（受容済みリスク）

- `PipeOptions.CurrentUserOnly` は、パイプをセッションを所有する Windows ユーザー（および整合性レベル）に制限します。同じオプションのクライアント側は、サーバーが同じユーザーによって所有されていることを検証します。
- **同じ Windows ユーザーとして実行されている任意のプロセスが、状態の読み取り、セッションの停止、公開ランタイム操作の実行を行えます。** 追加の認証、トークン、クライアントごとの認可はありません。これはベータ版において文書化された受容済みリスクです。信頼できないソフトウェアと共有しているアカウントで OmsiLaunch のセッションを実行しないでください。
- パイプがネイティブアドレス、ハンドル、生のメモリ操作を公開することはありません。`PublicCapabilityRegistry` の公開操作 id だけが受け付けられ、内部の調査用プリミティブ（`internal.*`）はセッションを検索する前に拒否されます。
- メッセージにはサイズ上限（64 KiB）があり、フレーム化されています。不正な形式やサイズ超過のフレームは、セッションに影響を与えずに応答されるか破棄されます。
- オーナーが待ち受けを開始する時点でパイプ名が既に別のプロセスに所有されている場合（作成時の `IOException`/`UnauthorizedAccessException`）、オーナーはエンドポイントなしでセッションの実行を続け、障害を `LocalControlPlane.ListenFault` に記録します。この場合、クライアントには `OL_E_NO_ACTIVE_SESSION` が返されます。そのようなセッションを終了するには、トレイアイコンまたは Ctrl+C を使用してください。

<a id="availability-window"></a>
## 利用可能な期間

1. `StartSessionAsync` が実行されます。この時点ではエンドポイントはまだ存在しません。同じルートに対する 2 回目の `OmsiLaunch.exe` の起動は、開始前に 250 ms の間 `session.status` を試行し、既にオーナーが応答した場合にのみ `OL_E_SESSION_ALREADY_ACTIVE` で失敗します。起動中に競合する 2 つのオーナーは、インストールリースによって直列化されます（`OL_E_INSTALLATION_BUSY`）。
2. セッションが `Running` に達します。要求された場合は、検証バッチ（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`）が完了します。
3. `LocalControlPlane.Start()`: エンドポイントが接続を受け付けます。オーナー自身の `/runtime:` 操作がある場合は、この時点以降に実行されます。
4. エンドポイントは `ProcessExited`、`Restoring`、`CleaningRuntime` の間も維持され（状態取得ではこれらの状態が報告されます）、セッションが `Completed` または `Failed` になるまで存在します。
5. `DisposeAsync` がリスナーをキャンセルし、処理中のクライアントタスクを待機した後、オーナーが `CloseAsync` を呼び出します。その後、パイプ名は存在しなくなります。

クライアントは短い接続タイムアウトを使用し（CLI は status/stop/events に 750 ms を使用します）、「エンドポイントがない」ことを「このインストール環境にアクティブなセッションがない」ことと見なしてください。

<a id="using-the-protocol-from-other-tooling"></a>
## 他のツールからのプロトコルの使用

<a id="c-net-6-or-later"></a>
### C#（.NET 6 以降）

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

匿名オブジェクトは、オーナーが期待するとおりの `ProtocolVersion`/`Command`/`Arguments` にシリアル化されます。`ConnectAsync` での `TimeoutException`/`OperationCanceledException` は、そのルートにオーナーが存在しないことを意味します。

<a id="powershell-7-pwsh"></a>
### PowerShell 7（`pwsh`）

`PipeOptions.CurrentUserOnly`、`SHA256.HashData`、`Convert.ToHexString` には .NET 5 以降が必要なため、この例には PowerShell 7 が必要です。Windows PowerShell 5.1（.NET Framework）では `CurrentUserOnly` を設定できません。

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

`ConvertTo-Json` はハッシュテーブルのキーの大文字・小文字を保持するため、`ProtocolVersion`、`Command`、`Arguments` は記述したとおりに出力されます。

<a id="using-the-cli-as-the-client"></a>
### CLI をクライアントとして使用する

独自のクライアントが不要な場合は、インストールディレクトリから `OmsiLaunch.exe` を実行します: `session status --json`、`session stop`、`events read --json`、`events watch`、`time get --json`、`/runtime:d3d.status --json`。CLI は status/バインドのハンドシェイクを行い、応答を[終了コード](exit-codes.md)に対応付けます（`0` 成功、`2` 引数の検証エラー、`4` オーナーなし、`7` 拒否）。

<a id="relationship-to-other-channels"></a>
## 他のチャネルとの関係

- オーナーとプロセス内プラグインの間のランタイムメールボックス（メモリマップト、64 KiB、シングルフライト、セッションにバインド）は独立した非公開のチャネルです。コントロールプレーンは `IOmsiLaunch.ExecuteRuntimeAsync` を通じてそこへ転送するだけです。
- インストールリース（`Local\OmsiLaunch.Installation.<sha256(root)>`）は名前付きセマフォであり、このプロトコルの一部ではありません。ログオンセッションごと、インストール環境ごとにオーナーが 1 つだけであることを保証します。
- トレイアイコンの "End session"（セッションを終了する項目）とコントロールプレーンの `session.stop` は、オーナー側の同じ停止要求を通知します。[Windows トレイ](windows-tray.md)を参照してください。
