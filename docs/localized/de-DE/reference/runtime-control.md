# Runtime-Steuerung

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> Übersetzung der [englischen Originalseite](../../../reference/runtime-control.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Die Runtime-Steuerung umfasst die Lese-, Schreib- und Aktionsoperationen, die OmsiLaunch innerhalb einer laufenden OMSI-Sitzung ausführt. Diese Seite erklärt die drei Zugangswege (Eigentümer-API, CLI-Client, lokale Steuerungsebene), wie Anfragen vom Aufrufer zum Plugin und zurück gelangen, wie Handles und Anfrageidentitäten funktionieren, welche Timeouts gelten, was bewusst nicht im Journal erfasst wird, sowie die Konventionen für Argumente und Ergebnisse, jeweils mit einem ausgearbeiteten Beispiel pro Familie. Das Verzeichnis der Operationen selbst, mit Argumenten, Ergebnisschlüsseln und Fehlern, steht unter [Capabilities](capabilities.md). Quellen: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` und `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` und `CliInput` (`tools/OmsiLaunch.Cli/`).

<a id="three-entry-points"></a>
## Drei Einstiegspunkte

| Einstiegspunkt | Wer | Pfad | Timeout |
| --- | --- | --- | --- |
| Eigentümer-API | Ein Integrator, der die Sitzung im eigenen Prozess mit `IOmsiLaunch.StartSessionAsync` gestartet hat | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> Registry-Validierung -> Sitzungssuche -> Mailbox | vom Aufrufer übergebene `TimeSpan` |
| Eigentümer-CLI (`/runtime:<op>`) | Der `OmsiLaunch.exe`-Prozess, dem die Sitzung gehört | eine Operation, die direkt nach `Running` ausgeführt wird; das Ergebnis wird nach `.omsilaunch\diagnostics\<session>-runtime-operation.json` und auf die Konsole geschrieben; die Sitzung läuft weiter | 5 s (15 s für `road-vehicles.spawn`) |
| CLI-Client | Jeder Aufruf von `OmsiLaunch.exe` **ohne** Installationsargument, z. B. `OmsiLaunch.exe time get` | Named Pipe `runtime.execute` zum Eigentümer der Installation, in der sich die ausführbare Datei befindet -> der Eigentümer ruft `ExecuteRuntimeAsync` auf | 8 s (30 s für `road-vehicles.spawn`) sowohl auf Client- als auch auf Eigentümerseite |
| Lokale Steuerungsebene (Control Plane) | Jeder Prozess desselben Windows-Benutzers | dasselbe Pipe-Protokoll wie der CLI-Client; siehe [lokale Steuerung](local-control.md) | wie oben |

Jeder Pfad endet in `ExecuteRuntimeAsync`, das die öffentliche Grenze in dieser Reihenfolge durchsetzt:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: Eine Operation, die nicht in `PublicRuntimeOperationIds` enthalten ist (einschließlich jeder `internal.*`-Operation), liefert `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; ein fehlendes oder leeres erforderliches Argument liefert `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Keiner der beiden Fälle berührt die Sitzung.
2. Sitzungssuche (`KeyNotFoundException` bei einem unbekannten Handle), `OL_E_RUNTIME_SESSION_MISMATCH`, wenn `RuntimeCommand.SessionId` vom Handle abweicht, `OL_E_SESSION_NOT_RUNNING`, sofern der Zustand nicht `Running` ist.
3. Mailbox-Anfrage (siehe unten), danach entfernt `ScrubInternalValues` jeden Ergebnisschlüssel, der mit `internal_` beginnt oder auf `_address`, `_pointer`, `_vmt` endet.

Der CLI-Client und die Steuerungsebene führen Schritt 1 selbst aus, bevor sie den Eigentümer kontaktieren, sodass ein ungültiger Befehlsname auch dann mit Exitcode 2 gemeldet wird, wenn keine Sitzung aktiv ist (`OL_E_RUNTIME_OPERATION_UNKNOWN` und `OL_E_RUNTIME_ARGUMENT_REQUIRED` werden auf `InvalidArguments` abgebildet; andere Ablehnungen auf 7; kein Eigentümer auf 4).

Der Endpunkt der Steuerungsebene existiert nur, solange der Eigentümer im Zustand `Running` ist, nach dem Start und nachdem ein eventuelles `/runtime-batch`-, `/runtime-write-batch`- oder `/d3d-batch`-Harness abgeschlossen ist. Verändernde Befehle (`runtime.execute`, `session.stop`) müssen die aktive `session_id` mitführen; die CLI ermittelt sie automatisch über `session.status` (andernfalls `OL_E_CONTROL_SESSION_MISMATCH`).

<a id="request-identity-and-the-mailbox"></a>
## Anfrageidentität und die Mailbox

Ein `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` wird in ein `RuntimeCommandWire`-Envelope serialisiert (Magic `OLRC`, Version 1, 72-Byte-Header, SHA-256 der JSON-Nutzdaten) und in der Single-Flight-Mailbox der Sitzung von 64 KiB (`OmsiLaunch.Runtime.<sessionId>`) bereitgestellt. Das Plugin fragt die Mailbox alle 50 ms auf OMSIs UI-Thread ab, führt die Operation dort aus und schreibt die Antwort.

Regeln, die den Kanal robust machen:

| Regel | Wirkung |
| --- | --- |
| Single Flight | Eine Anfrage gleichzeitig pro Sitzung; der Host serialisiert Aufrufer mit einem Semaphor. Ein Slot, der beim Eintreffen einer neuen Anfrage noch `requested` ist, ergibt `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Sitzungsbindung | Das Plugin ignoriert (und leert) eine Anfrage, deren Sitzungs-GUID nicht seine eigene ist; der Host lehnt eine Antwort ab, deren Sitzungs- oder Anfrage-ID nicht übereinstimmt (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | Wenn die Frist abläuft, setzt der Host den Slot auf idle zurück und wirft `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Verspätete Antwort | Das Plugin veröffentlicht eine Antwort nur, wenn der Slot noch seine Anfrage-ID enthält; eine Antwort auf eine aufgegebene Anfrage wird verworfen. Landet dennoch eine, findet die nächste Host-Anfrage einen veralteten `responded`-Slot vor, verwirft ihn und fährt fort; trägt diese veraltete Antwort **dieselbe** Anfrage-ID wie die neue Anfrage, wirft der Host `OL_E_RUNTIME_REQUEST_ID_REUSED`. Aufrufer dürfen daher eine Anfrage-ID innerhalb einer Sitzung niemals wiederverwenden. |
| Größe | Eine Anfrage, die größer als der Slot ist, wird vor der Bereitstellung abgelehnt (`ArgumentOutOfRangeException`). Eine Listenseite, die nicht passt, wird vom Plugin gekürzt (letzte Zeilen verworfen, `truncated=true`, kleineres `returned_count`, `next_cursor` bei der ersten verworfenen Zeile); jede andere übergroße Antwort wird durch einen typisierten `OL_E_RUNTIME_RESPONSE_TOO_LARGE`-Fehler ersetzt. |
| Kanal geschlossen | Nach dem Ende der Sitzung `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Anfrage-IDs sind vom Aufrufer gewählte `ulong`-Werte. Die CLI verwendet feste Bereiche: `10_001` für `/runtime:`, `50_001+` für weitergeleitete Anfragen der Steuerungsebene, `1+` für den Lese-Batch, `20_000+` für den D3D-Batch, `30_000+` für `D3DRuntimeApi`. Integratoren sollten pro Sitzung einen monoton steigenden Zähler verwenden.

<a id="handles-and-stale-detection"></a>
## Handles und Erkennung veralteter Handles

| Präfix | Typ | Ausgegeben von | Format |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + sechs Dezimalziffern (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + sechs Dezimalziffern |
| `d3dtex-` | D3D-Textur | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Handles sind opak und an die Sitzung gebunden: Sie werden vom Plugin erzeugt, codieren nie eine Adresse und sind in einer anderen Sitzung bedeutungslos. Wird ein Handle aufgelöst, prüft das Plugin, ob die zugeordnete Adresse noch in der aktiven OMSI-Sammlung liegt (Stand des letzten Listenlesevorgangs; `road-vehicles.list` und `humans.list` aktualisieren diese Sicht), und liest einen Objekt-Fingerabdruck erneut (die Delphi-VMT plus den Zeiger auf die Fahrzeugdefinition bzw. den Modellindex des Menschen). Eine Abweichung bedeutet, dass das native Objekt zerstört und seine Adresse wiederverwendet wurde: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. Für dasselbe aktive Objekt wird bei wiederholten Listenlesevorgängen erneut dasselbe Token zurückgegeben. Verbleibender blinder Fleck: Ein Objekt derselben Klasse und Definition, das zwischen zwei Listenlesevorgängen an derselben Adresse neu erzeugt wird, lässt sich nicht unterscheiden. D3D-Handles werden von der nativen Bridge validiert: Ein unbekanntes Handle oder eines aus einer fremden Sitzung ergibt `OL_E_D3D_STALE_RESOURCE_HANDLE`, ein freigegebenes `OL_E_D3D_RESOURCE_RELEASED`, und ein Wechsel der Gerätegeneration markiert Texturen als `STALE`.

Handles sind niemals native Adressen und dürfen niemals geparst, numerisch verglichen, über Sitzungen hinweg gespeichert oder an eine andere Sitzung übergeben werden: Behandeln Sie sie als opake Zeichenfolgen, die für die ausgebende Sitzung gültig sind.

Runtime-Nachweis: Ein freigegebenes D3D-Handle bleibt abgelehnt, nachdem neue Texturen erzeugt wurden, und ein Handle aus einer vorherigen Sitzung wird von der nächsten abgelehnt (Runtime-Abschluss `H02`); ein D3D-Geräte-Reset invalidiert jede aktive Textur (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). Für die Erkennung veralteter RoadVehicle- und Human-Handles nach natürlichem Entfernen (RV-002) gibt es keinen sicheren Runtime-Erzeuger (OMSI hat in den Beobachtungsfenstern keine Objekte entfernt, und keine öffentliche Operation entfernt eines); sie ist offline abgedeckt.

<a id="what-is-not-journaled"></a>
## Was nicht im Journal erfasst wird

Runtime-Änderungen verändern nur OMSIs Speicher. **Sie werden nicht im Transaktionsjournal erfasst und nicht wiederhergestellt**, wenn die Sitzung endet: `time.set`, `camera.set`, `camera.lock` (die Richtlinie endet, wenn das Plugin herunterfährt), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` und jede `d3d.texture.*`-Ressource. Sie verschwinden mit dem OMSI-Prozess, den OmsiLaunch am Ende der Sitzung beendet, ohne OMSI etwas speichern zu lassen (siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md)). Nichts in der Runtime-Familie berührt das Dateisystem.

<a id="argument-and-result-conventions"></a>
## Konventionen für Argumente und Ergebnisse

- Argumente sind Schlüssel/Wert-Paare aus Zeichenfolgen. In der CLI wird `--key=value` nach den Befehlswörtern zu einem Runtime-Argument (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` ist das Äquivalent für `/runtime:<op>`. Zahlen verwenden die invariante Kultur (`.` als Dezimaltrennzeichen); boolesche Werte sind `true`/`false`.
- Befehlswörter werden über `CliInput.HierarchicalRoutes` auf Operations-IDs abgebildet (z. B. `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). Die vollständige Routentabelle steht in der [CLI-Referenz](cli.md). D3D-Operationen haben keine Befehlswort-Route; verwenden Sie `/runtime:d3d.status` oder `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Ergebnisse sind flache Dictionaries aus Zeichenfolgen. Listen verwenden Schlüssel der Form `<row>.<n>.<field>` (`vehicle.0.handle`, `track.3.filename`, `name.12`) mit den Seitenfeldern `count`, `returned_count`, `truncated` und `next_cursor` (siehe [Paginierung](capabilities.md#pagination)).
- Fehlschläge tragen `Succeeded=false`, einen `OL_E_`-`ErrorCode` und bei Fehlschlägen auf Plugin-Seite `detail` (sowie `native_status` bei D3D, `exception` bei unerwarteten Fehlern).

### CLI-Envelope (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

Ein Fehler-Envelope hat die Form `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Ohne `--json` gibt die CLI das Ergebnisobjekt als eingerücktes JSON oder als `CODE: message` aus.

### API-Envelope

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` wirft bei Grenzverletzungen nach der Registry-Validierung eine Ausnahme (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) und gibt bei Ablehnungen durch die Registry und Fehlern auf Plugin-Seite `Succeeded=false` zurück.

<a id="worked-examples"></a>
## Ausgearbeitete Beispiele

Alle CLI-Beispiele setzen voraus, dass für die Installation, die `OmsiLaunch.exe` enthält, ein Eigentümer läuft (gestartet z. B. mit `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1` oder mit `OmsiLaunch.exe "/saved:situations\Linie 5.osn"`, wenn ein Beispiel ein Spielerfahrzeug benötigt), und werden aus einer zweiten Konsole in derselben Installation ausgeführt.

| Familie | Befehl | Was er bewirkt |
| --- | --- | --- |
| Sitzung | `OmsiLaunch.exe session status --json` | Liest `SessionId`, `State`, Diagnosen und die begrenzte Liste der Runtime-Ereignisse. |
| Zeit | `OmsiLaunch.exe time get`, danach `OmsiLaunch.exe time set --hour=7 --minute=30` | Liest die Uhr; stellt sie über das profilierte `SetTime` und gibt den zurückgelesenen Wert zurück. |
| Wetter | `OmsiLaunch.exe weather get` und `OmsiLaunch.exe weather actual get` | Liest den aktuellen Wetterzustand und den Echtwetter-/ICAO-Zustand. `OmsiLaunch.exe weather set --wind_speed=5` setzt manuelles Wetter (der Quelldatensatz von OMSI; Live-METAR endet) und `OmsiLaunch.exe weather metar set --icao=EDDF` wechselt zu Live-METAR-Wetter. Beides ist EXPERIMENTAL, bis es zur Laufzeit validiert ist. |
| Karte | `OmsiLaunch.exe map get` | Kartenidentität, Name, Beschreibung, Kachelanzahl, Jahresbereich und Verkehrsseite. |
| Kamera | `OmsiLaunch.exe camera set --field_of_view=50`, danach `OmsiLaunch.exe camera lock --family=0 --preset=1` und `OmsiLaunch.exe camera unlock` | Schreibt das FOV; fixiert die Fahrerfamilie mit Preset 1 (erfordert ein Spielerfahrzeug, z. B. aus einer gespeicherten Situation); gibt die Richtlinie frei. |
| Fahrzeuge | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Anzahlen; Handles; ein Snapshot. |
| Spawn | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Erzeugt ein KI-RoadVehicle (Timeout 30 s); gibt `created_handle` zurück. `OmsiLaunch.exe vehicles place-random --group=1` ruft `PlaceRandomBus` auf. |
| Spieler | `OmsiLaunch.exe player get` | `present=false` bei einem Headless-Start, andernfalls der Snapshot des Spielers. |
| Menschen | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Anzahlen; Handles; ein Snapshot. |
| Fahrplan | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Anzahlen des Managers; begrenzte Track-Zeilen; Fahrplan-Logs. |
| Skripte | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Numerische Variablen auflisten/lesen/schreiben; String-Variable lesen/schreiben; Skript- und Sound-Trigger (Schreibzugriffe auf Strings und Trigger sind EXPERIMENTAL, bis sie zur Laufzeit validiert sind). |
| Konstanten und Kurven | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Fahrzeugkonstanten und Kurvenauswertung. Die Namen sind modellspezifisch: Verwenden Sie die vom `list`-Befehl zurückgegebenen Namen (diese wurden für den Spielerbus von `situations\Linie 5.osn` aufgelistet). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | HOF-Metadaten der Fahrzeugdefinition. |
| Fahrer und Fahrscheine | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Fahrerdatensätze; Fahrscheinpaket. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`, danach `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` und `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Texturlebenszyklus auf dem Render-Thread; `<HANDLE>` ist das von `create` ausgegebene `handle`; `--pixels_base64` muss zu `width * height * 4` Bytes (32-Bit-Formate) und höchstens 48 KiB dekodieren. |
| Ereignisse | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Begrenzte Ereignisliste; abfragende Beobachtung (250 ms) bis Ctrl+C. |
| Stopp | `OmsiLaunch.exe session stop` | Fordert den kanonischen Stopp an: OMSI wird beendet und die Transaktion vom Eigentümer wiederhergestellt. |

<a id="stability"></a>
## Stabilität

Der Kanal selbst (`runtime.command-channel`) ist `STABLE_BETA`: Wire-Integrität, Sitzungsbindung, Verwerfen verspäteter Antworten und typisierte Ablehnung übergroßer Daten sind offline durch `runtime-command.wire-guard`, `runtime-command.session-binding` und `runtime-command.late-response-ignored` abgedeckt und zur Laufzeit durch RV-003 (Sitzung `5f641c8d`), Round A RA-019 (ein Client gab `road-vehicles.spawn` nach 250 ms auf; die nächste Anfrage war erfolgreich) und die Kanal-Testreihe `R03` des Runtime-Abschlusses (Timeout, Abbruch, wiederverwendete Anfrage-ID, Ablehnung durch das Plugin, interne Operation, falsche Sitzung und fehlendes Argument, jeweils gefolgt von einer erfolgreichen Anfrage). Die Stabilität pro Operation steht unter [Capabilities](capabilities.md).

<a id="channel-reuse-after-failures"></a>
## Wiederverwendung des Kanals nach Fehlschlägen

Jeder terminale Pfad einer Runtime-Anfrage hinterlässt die Mailbox wiederverwendbar: Erfolg, ein typisierter Fehler, eine fehlerhaft aufgebaute, übergroße, sitzungsfremde oder mit falscher Anfrage-ID versehene Antwort, Timeout, Abbruch durch den Aufrufer und Dekodierfehler enden alle in einer einzigen Bereinigung, die den Slot auf idle zurücksetzt und Länge und Envelope-Header leert, sodass nichts aus einer früheren Anfrage von der nächsten gelesen werden kann. Eine übrig gebliebene Anfrage oder Antwort, die beim Start einer neuen Anfrage vorgefunden wird, wird zuerst geleert (`OL_E_RUNTIME_REQUEST_ID_REUSED`, wenn sie die ID der neuen Anfrage trägt). Auf Plugin-Seite wird eine Ausnahme innerhalb einer Operation mit `OL_E_RUNTIME_OPERATION_FAILED` beantwortet, ein Ergebnis, das größer als der Slot ist, mit `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (ein festes, kleines Envelope), eine Anfrage für eine andere Sitzung mit `OL_E_RUNTIME_SESSION_MISMATCH`, und eine Anfrage, die der Host aufgegeben hat, wird nie beantwortet. Diese Regeln sind offline abgedeckt (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); die Pfade, die ein Aufrufer herbeiführen kann (Timeout, Abbruch, wiederverwendete ID, typisierte Ablehnungen, verspätete Antwort), haben zusätzlich einen Runtime-Nachweis (RA-019, `R03`). Beschädigte, sitzungsfremde oder übergroße Antworten lassen sich von außerhalb des Produkts nicht erzeugen und sind nur offline abgedeckt.
