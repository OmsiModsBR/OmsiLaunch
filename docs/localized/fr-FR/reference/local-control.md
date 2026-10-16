# Plan de contrôle local

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> Traduction de la [page originale en anglais](../../../reference/local-control.md) d’OmsiLaunch 0.1.0-beta3. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Le plan de contrôle local est le point de terminaison à canal nommé (named pipe) par lequel un propriétaire OmsiLaunch en cours d'exécution (le processus qui a démarré une session) accepte des commandes de session sémantiques provenant d'autres processus de la même machine : état, événements, arrêt et opérations runtime publiques. Cette page spécifie le point de terminaison tel qu'il est implémenté dans `tools\OmsiLaunch.Cli\LocalControlPlane.cs` et le gestionnaire de requêtes dans `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`) : nommage du pipe, tramage, version du protocole, commandes, liaison à la session, codes d'erreur, modèle de confiance, fenêtre de disponibilité et manière de dialoguer avec lui depuis d'autres outils. Les commandes du client CLI (`session status`, `session stop`, `events read`, `events watch`, routes comme `time get`) sont de fines surcouches de ce protocole ; voir la [référence de la CLI](cli.md). Les opérations runtime elles-mêmes sont spécifiées dans [contrôle runtime](runtime-control.md) ; l'alternative dans le processus pour les intégrateurs est l'[API publique](public-api.md).

<a id="summary"></a>
## Résumé

| Propriété | Valeur |
|---|---|
| Transport | Canal nommé Windows, `PipeDirection.InOut`, mode octet, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, tampons d'entrée et de sortie d'une trame (65,540 octets) |
| Nom du pipe | `OmsiLaunch.Control.0.1.<key>`, où `<key>` correspond aux 16 premiers caractères hexadécimaux du SHA-256 calculé sur les octets UTF-8 du chemin complet de la racine de l'installation, sans `\` final, en majuscules (`LocalControlPlane.PipeNameFor`) |
| Tramage | Préfixe de longueur `int32` little-endian de 4 octets, suivi d'autant d'octets de JSON UTF-8 ; une requête et une réponse par connexion |
| Message maximal | 65,536 octets pour une requête comme pour une réponse (`MaxMessageBytes`) |
| Version du protocole | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`) ; une non-concordance reçoit la réponse `OL_E_CONTROL_PROTOCOL` |
| Commandes | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Liaison à la session | `session.stop` et `runtime.execute` exigent un `Arguments.session_id` égal à l'identifiant de la session active |
| Disponibilité | Du moment où la session a atteint `Running` (après tout lot de validation) jusqu'à ce que la session atteigne `Completed` ou `Failed` et que le propriétaire libère le point de terminaison |
| Portée | Un point de terminaison par racine d'installation ; deux installations du même utilisateur ne partagent jamais un pipe |
| Stabilité | STABLE_BETA (tests hors ligne dans `OmsiLaunch.WindowsUiTests` ; preuves d'exécution : RV-003, état consulté en parallèle pendant un appel natif de plusieurs secondes, série A RA-008, et la batterie de trames brutes de la clôture runtime `R04` : 17 trames malformées, trop grandes, de mauvais protocole, de commande inconnue, non liées et de mauvaise session, chacune ayant reçu son erreur typée et suivie d'un `session.status` réussi ; un client bloqué ne bloque pas les autres ; un arrêt lié pendant une apparition en cours termine la session) |

<a id="pipe-name-derivation"></a>
## Dérivation du nom du pipe

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` est la définition unique de l'identité d'une installation, également utilisée par le bail d'installation : elle résout le chemin complet (y compris les segments `.` et `..`, `/` ou `\`, les séparateurs répétés) et supprime les séparateurs finaux, sauf pour une racine de lecteur. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` et `d:\omsi 2` produisent le même nom ; `D:\OMSI 2` et `E:\OMSI 2` produisent des noms différents. Le client dérive toujours le nom à partir de l'installation qui contient l'exécutable qu'il lance (`AppContext.BaseDirectory`), sauf si l'appelant de l'API transmet une racine explicite. Le `0.1` contenu dans le nom est la version du protocole, de sorte qu'un futur protocole peut coexister sur la même machine.

<a id="framing-and-encoding"></a>
## Tramage et encodage

- Écriture : sérialiser l'enregistrement avec les options par défaut de `System.Text.Json`, vérifier `length <= 65536` (sinon `OL_E_CONTROL_MESSAGE_TOO_LARGE`), écrire `BitConverter.GetBytes((int)length)` (4 octets, little-endian sous Windows), écrire la charge utile, vider le tampon.
- Lecture : lire exactement 4 octets ; rejeter `length < 0` ou `length > 65536` avec `OL_E_CONTROL_MESSAGE_INVALID` ; lire exactement `length` octets ; désérialiser. Une connexion qui se ferme avant qu'une trame complète ait été lue est abandonnée silencieusement par le propriétaire (aucune réponse). Une trame rejetée reçoit une réponse même lorsque le client a déjà écrit plus d'octets que le propriétaire n'en lit : les tampons du pipe contiennent une trame entière, donc l'écriture du client se termine et il peut lire la réponse (clôture runtime BUG-02 ; avant la correction, un tel client et le propriétaire restaient tous deux bloqués dans leurs écritures).
- Le propriétaire écrit chaque réponse avec une échéance de 5 s : un client qui se connecte, envoie une requête et ne lit jamais la réponse ne peut pas monopoliser une tâche du propriétaire plus longtemps.
- Les noms de propriété sont **en PascalCase et sensibles à la casse** dans la requête (`ProtocolVersion`, `Command`, `Arguments`), car le propriétaire désérialise avec les options par défaut. Les réponses utilisent `Ok`, `Result`, `ErrorCode`, `Message`. Les valeurs d'énumération sont sérialisées sous forme d'entiers ; les `Guid` sous forme de chaînes.
- Une requête par connexion : le propriétaire lit une requête, écrit une réponse et ferme le pipe. Ouvrez une nouvelle connexion pour chaque requête.

<a id="request"></a>
### Requête

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` doit être l'identifiant renvoyé par `session.status` pour la session en cours (la valeur ci-dessus provient d'une session réelle).

`Arguments` est facultatif (`null` ou omis) pour `session.status` et `session.events`. Les clés d'argument sont comparées de manière ordinale (sensible à la casse).

<a id="response"></a>
### Réponse

Trames capturées à partir d'un propriétaire réel (clôture runtime `R04`, paquet final). Un `runtime.execute` réussi :

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

Un `session.stop` rejeté faute du `session_id` actif :

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## Commandes

| Commande | Arguments | Résultat quand `Ok=true` | Remarques |
|---|---|---|---|
| `session.status` | aucun | `SessionStatus` : `SessionId` (GUID sous forme de chaîne), `State` (entier `SessionState` ; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (tableau de `{Code, Message, Data}`), `RuntimeEvents` (tableau) | Lecture seule. Les clients lisent d'abord cette commande pour obtenir le `SessionId`. |
| `session.events` | aucun | Tableau de `RuntimeEvent` : `Type`, `TimestampUtc`, `Sequence` (`int64` monotone), `Data` (table de chaînes) | Lecture seule, liste bornée ; `events watch` l'interroge et affiche les entrées dont le `Sequence` est supérieur au dernier vu. |
| `session.stop` | `session_id` (requis) | `{"accepted": true, "session_id": "..."}` | Demande l'arrêt canonique (`TerminateProcess` sur OMSI, puis restauration). Rend la main immédiatement ; l'état de la session reste observable via `session.status` jusqu'à la disparition du point de terminaison. |
| `runtime.execute` | `operation` (requis), `session_id` (requis), plus les arguments propres à l'opération (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult` : `SessionId`, `RequestId` (attribué par le propriétaire, à partir de `50001`), `Succeeded`, `ErrorCode`, `Values` (table de chaînes) | Validée avec `PublicCapabilityRegistry.ValidateRuntimeArguments` avant l'exécution : opération inconnue ou interne → `OL_E_RUNTIME_OPERATION_UNKNOWN` ; valeur requise manquante → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout de 8 s, 30 s pour `road-vehicles.spawn`. Un échec runtime reçoit une réponse `Ok=false` avec `Result` défini sur le `RuntimeCommandResult`, `ErrorCode` sur son code et `Message` sur `Runtime operation was rejected.` Les clés de résultat commençant par `internal_` ou se terminant par `_address`, `_pointer`, `_vmt` sont supprimées par l'API avant d'atteindre le pipe. |
| toute autre commande | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

Le propriétaire traite les connexions en parallèle (chaque client accepté est servi dans sa propre tâche), de sorte que `session.status` continue de répondre pendant qu'un appel natif de plusieurs secondes tel que `road-vehicles.spawn` est en cours.

<a id="session-id-binding"></a>
## Liaison à l'identifiant de session

Les commandes qui modifient l'état doivent nommer la session sur laquelle elles agissent. La CLI implémente `TryRequestBoundAsync` : elle envoie `session.status` (750 ms), récupère `Result.SessionId` et répète la requête en ajoutant `session_id` à `Arguments`. Un identifiant manquant, impossible à analyser ou différent reçoit la réponse `OL_E_CONTROL_SESSION_MISMATCH`. Si le propriétaire ne rapporte pas d'identifiant de session, le client signale `OL_E_CONTROL_PROTOCOL`.

<a id="reply-metadata-and-truncated-event-history"></a>
### Métadonnées de réponse et historique d'événements tronqué

`Metadata` vaut `null`, sauf si la réponse comporte une information sur elle-même. `session.status` et `session.events` renvoient l'historique des événements du propriétaire (au plus 256 événements, le plus récent en dernier). Lorsque cet historique ne tient pas dans une trame de 64 KiB, les événements les plus anciens sont supprimés jusqu'à ce qu'il tienne, et `Metadata` l'indique :

| Clé | Signification |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Événements de cette réponse |
| `events_dropped_count` | Événements les plus anciens supprimés pour tenir dans la trame |
| `events_available_count` | Événements que le propriétaire détenait pour cette réponse |
| `events_first_returned_sequence` | `Sequence` du premier événement renvoyé |

Sans `Metadata`, la réponse n'est pas tronquée par le plan de contrôle. `Sequence` est monotone dans les deux cas, de sorte qu'un client peut aussi détecter les événements que le propriétaire lui-même a évincés au-delà de son historique de 256 événements. La CLI affiche ces métadonnées dans l'enveloppe `--json` et une remarque en mode texte.

<a id="error-codes"></a>
## Codes d'erreur

| Code | Origine | Signification |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | propriétaire / client | Le `ProtocolVersion` de la requête n'est pas `0.1` ; la réponse du propriétaire n'a pas pu être décodée ou était vide ; le propriétaire a fermé la connexion sans répondre ; la connexion a été rompue après son établissement ; ou le propriétaire n'a rapporté aucun identifiant de session. |
| `OL_E_CONTROL_MESSAGE_INVALID` | propriétaire / client | Préfixe de longueur hors plage (une trame de requête trop grande est refusée ainsi avant la lecture de sa charge utile), trame vide, JSON `null`, requête sans `Command`, ou JSON impossible à décoder. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | écriture côté client | La propre requête du client dépasse 65,536 octets ; l'erreur est signalée au client et rien n'est envoyé. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | écriture côté propriétaire | La réponse du propriétaire dépasse 65,536 octets ; le propriétaire répond avec cette erreur typée. `session.status` et `session.events` réduisent leur historique d'événements (les plus anciens d'abord) pour tenir, et ne la produisent donc pas. |
| `OL_E_CONTROL_SESSION_MISMATCH` | propriétaire | `session.stop` / `runtime.execute` sans le `session_id` actif. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | propriétaire | Nom de commande non pris en charge. |
| `OL_E_CONTROL_HANDLER_FAILED` | propriétaire | La réponse du gestionnaire n'a pas pu être sérialisée, ou le gestionnaire a levé une exception sans code `OL_E_` dans son message ; lorsqu'un code est présent, c'est ce code qui est renvoyé à la place (par exemple `OL_E_SESSION_NOT_RUNNING` depuis `ExecuteRuntimeAsync` après la fermeture d'OMSI). |
| `OL_E_CONTROL_FAILED` | client CLI | Code par défaut lorsqu'une réponse est `Ok=false` sans `ErrorCode`. |
| `OL_E_TIMEOUT` | client | Le client était connecté à un propriétaire qui n'a pas répondu dans le délai du timeout. |
| `OL_E_NO_ACTIVE_SESSION` | client CLI | Aucun point de terminaison de propriétaire n'a pu être atteint (`TryRequestAsync` ne renvoie `null` que lorsque la connexion elle-même échoue). Une fois connecté, tout échec correspond à l'un des codes typés ci-dessus. Sortie `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | client et propriétaire | Validation de la surface publique de `runtime.execute` (sortie `2` dans la CLI). |

Chaque échec connu reçoit une erreur typée, afin qu'un client ne confonde jamais une panne avec l'absence de propriétaire. Deux cas se terminent sans trame de réponse : un client qui se déconnecte avant d'envoyer une requête complète (personne à qui répondre), et une requête encore en vol à la fin de la session (le propriétaire annule les réponses en attente pendant qu'il ferme le point de terminaison ; le client voit une fin de flux, que le client CLI signale comme `OL_E_CONTROL_PROTOCOL` « The owner closed the connection without a reply »). Un client qui a envoyé une requête et se déconnecte avant de lire la réponse n'affecte pas le propriétaire. Le démarrage d'un propriétaire refuse de se poursuivre dès qu'une réponse (y compris une erreur typée) provient du point de terminaison de l'installation, car une réponse prouve qu'un propriétaire est présent. La CLI fait correspondre les réponses aux codes de sortie comme décrit dans [codes de sortie](exit-codes.md).

<a id="trust-model-accepted-risk"></a>
## Modèle de confiance (risque accepté)

- `PipeOptions.CurrentUserOnly` restreint le pipe à l'utilisateur Windows (et au niveau d'intégrité) propriétaire de la session ; côté client, la même option vérifie que le serveur appartient au même utilisateur.
- **Tout processus exécuté sous le même utilisateur Windows peut lire l'état, arrêter la session et exécuter des opérations runtime publiques.** Il n'existe aucune authentification supplémentaire, aucun jeton ni aucune autorisation par client. Il s'agit d'un risque documenté et accepté pour la bêta ; n'exécutez pas de sessions OmsiLaunch sous un compte partagé avec des logiciels non fiables.
- Le pipe n'expose jamais d'adresses natives, de handles ni d'opérations sur la mémoire brute ; seuls les identifiants d'opération publics de `PublicCapabilityRegistry` sont acceptés, et les primitives de recherche internes (`internal.*`) sont rejetées avant toute recherche de session.
- Les messages sont bornés (64 KiB) et tramés ; une trame malformée ou trop grande reçoit une réponse ou est abandonnée sans affecter la session.
- Si le nom du pipe appartient déjà à un autre processus lorsque le propriétaire commence à écouter (`IOException`/`UnauthorizedAccessException` à la création), le propriétaire continue d'exécuter la session sans point de terminaison et enregistre la panne dans `LocalControlPlane.ListenFault` ; les clients voient alors `OL_E_NO_ACTIVE_SESSION`. Utilisez l'icône de la zone de notification ou Ctrl+C pour terminer une telle session.

<a id="availability-window"></a>
## Fenêtre de disponibilité

1. `StartSessionAsync` s'exécute ; le point de terminaison n'existe pas encore. Un second lancement de `OmsiLaunch.exe` pour la même racine sonde `session.status` pendant 250 ms avant de démarrer et échoue avec `OL_E_SESSION_ALREADY_ACTIVE` uniquement lorsqu'un propriétaire répond déjà ; deux propriétaires en concurrence pendant le démarrage sont sérialisés par le bail d'installation (`OL_E_INSTALLATION_BUSY`).
2. La session atteint `Running` ; les lots de validation (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), lorsqu'ils sont demandés, se terminent.
3. `LocalControlPlane.Start()` : le point de terminaison accepte les connexions. L'opération `/runtime:` du propriétaire lui-même, le cas échéant, s'exécute après ce point.
4. Le point de terminaison reste actif pendant `ProcessExited`, `Restoring` et `CleaningRuntime` (l'état rapporte ces états), jusqu'à ce que la session soit `Completed` ou `Failed`.
5. `DisposeAsync` annule l'écouteur, attend les tâches client en cours, puis le propriétaire appelle `CloseAsync`. Après cela, le nom du pipe n'existe plus.

Les clients doivent utiliser des timeouts de connexion courts (la CLI utilise 750 ms pour status/stop/events) et interpréter « aucun point de terminaison » comme « aucune session active pour cette installation ».

<a id="using-the-protocol-from-other-tooling"></a>
## Utilisation du protocole depuis d'autres outils

<a id="c-net-6-or-later"></a>
### C# (.NET 6 ou ultérieur)

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

Les objets anonymes sont sérialisés en `ProtocolVersion`/`Command`/`Arguments` exactement comme le propriétaire l'attend. Une `TimeoutException`/`OperationCanceledException` sur `ConnectAsync` signifie qu'il n'existe aucun propriétaire pour cette racine.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` et `Convert.ToHexString` nécessitent .NET 5 ou ultérieur ; cet exemple requiert donc PowerShell 7. Windows PowerShell 5.1 (.NET Framework) ne peut pas définir `CurrentUserOnly`.

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

`ConvertTo-Json` conserve la casse des clés de la table de hachage, de sorte que `ProtocolVersion`, `Command` et `Arguments` sont émis tels qu'ils sont écrits.

<a id="using-the-cli-as-the-client"></a>
### Utiliser la CLI comme client

Lorsqu'aucun client personnalisé n'est nécessaire, invoquez `OmsiLaunch.exe` depuis le répertoire d'installation : `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. La CLI effectue la négociation état/liaison et fait correspondre les réponses aux [codes de sortie](exit-codes.md) (`0` succès, `2` validation des arguments, `4` aucun propriétaire, `7` rejet).

<a id="relationship-to-other-channels"></a>
## Relation avec les autres canaux

- La boîte aux lettres runtime entre le propriétaire et le plugin dans le processus (mappée en mémoire, 64 KiB, requête unique en vol, liée à la session) est un canal privé distinct ; le plan de contrôle se contente de lui transmettre les requêtes via `IOmsiLaunch.ExecuteRuntimeAsync`.
- Le bail d'installation (`Local\OmsiLaunch.Installation.<sha256(root)>`) est un sémaphore nommé, qui ne fait pas partie de ce protocole ; il garantit un propriétaire unique par installation et par session de connexion Windows.
- L'élément « End session » (`Terminer la session`) de l'icône de la zone de notification et le `session.stop` du plan de contrôle signalent la même demande d'arrêt côté propriétaire ; voir [zone de notification Windows](windows-tray.md).
