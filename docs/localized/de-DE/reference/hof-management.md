# HOF-Verwaltung

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Übersetzung der [englischen Originalseite](../../../reference/hof-management.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

OMSI liest die HOF-Dateien (`*.hof`: Ziele, Liniennummern und Haltestellenlisten, die das IBIS bzw. die Zielanzeige des Fahrzeugs anzeigt) aus dem Ordner jedes Fahrzeugs. Die HOF-Verwaltung kopiert HOF-Dateien **für die Dauer einer Sitzung** in Fahrzeugordner: Eine Karte oder ein Add-on kann die HOFs mitliefern, die ihre Linien benötigen, und ein Anwender kann die HOFs eines Fahrzeugs für andere Fahrzeuge wiederverwenden, ohne ein Fahrzeug dauerhaft zu verändern. Diese Seite ist die normative Referenz, wie sie durch `HofPlanner` in `src/OmsiLaunch.Core/HofPlanner.cs`, das Feld `HofManagement` der [LaunchSpec](launchspec.md), die [CLI](cli.md)-Flags `/hof-source`, `/hof-target`, `/hof-file` und `/hof-mode` sowie den `hofs`-Block der [Sitzungsprofile](session-profiles.md) implementiert wird.

Stabilität: `STABLE_BETA` für Planung, Konfliktregeln, Eingrenzung und transaktionale Wiederherstellung (Offline-Gates `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Zur Laufzeit validiert: eine Spiegelungssitzung mit exakter Wiederherstellung und Absturz-Recovery über `/recover` (siehe [Status der Runtime-Validierung](../status/runtime-validation-status.md)).

Die HOF-Verwaltung ist nicht dasselbe wie `PlayerVehicle.Hof` / `/hof:<id>`; dieses wählt beim Start den HOF des Spielerfahrzeugs aus und ist in diesem Build `UNAVAILABLE`.

<a id="model"></a>
## Modell

Eine Anfrage ist eine Liste von Operationen (`HofManagementSpec.Operations`). Jede `HofOperationSpec` hat:

| Feld | Bedeutung |
| --- | --- |
| `SourceKind`, `Source` | Woher die HOF-Dateien stammen (Tabelle unten). |
| `TargetVehicles` | Ein oder mehrere installierte Fahrzeuge: die Identität einer `.bus`- oder `.ovh`-Datei (`Vehicles\MAN_SD200\MAN_SD77.bus`) oder eines Fahrzeugordners (`Vehicles\MAN_SD200`), jeweils unterhalb von `Vehicles\`. Mehrere Identitäten in einem Ordner adressieren diesen Ordner nur einmal. |
| `Mode` | `Add` (Standard): Die Quell-HOFs werden in jeden Zielordner kopiert; andere HOFs dort bleiben erhalten. `Mirror`: Die Quell-HOFs werden kopiert und jede andere `.hof`-Datei des Zielordners wird für die Sitzung entfernt, sodass die HOF-Menge des Ordners der Quellmenge entspricht. |
| `Files` | Optionale Liste von Dateinamen, die aus der Quelle übernommen werden (ohne Beachtung der Groß-/Kleinschreibung). Fehlt sie: jede `*.hof`-Datei direkt in der Quelle. Ein aufgeführter Name, den die Quelle nicht enthält: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Wird aufgelöst zu |
| --- | --- | --- |
| `Collection` | ein einfacher Name, z. B. `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: eine vom Anwender oder einem Werkzeug gepflegte Sammlung. |
| `Directory` | ein installationsrelatives Verzeichnis, z. B. `maps\RMG Leste\hof` | `*.hof` in diesem Verzeichnis: HOFs, die neben Karten- oder Add-on-Inhalten ausgeliefert werden. |
| `Vehicle` | eine Fahrzeugidentität oder ein Fahrzeugordner, wie bei Zielen | `*.hof` im Ordner dieses Fahrzeugs: Die HOFs eines Fahrzeugs werden für andere wiederverwendet. |
| `File` | eine installationsrelative `.hof`-Datei | genau diese eine Datei. |

Es werden nur Dateien verwendet, die direkt im Quellverzeichnis liegen (keine Rekursion). Symbolischen Links und Junctions wird niemals gefolgt (siehe [Eingrenzung](#confinement)).

<a id="what-a-session-does"></a>
## Was eine Sitzung tut

1. **Planung** (`PlanSessionAsync`, `/plan`). Jede Operation wird aufgelöst: Quellen werden gelesen, Ziele werden geprüft, Konflikte werden erkannt. Jede Datei, die sich ändern wird, erscheint in `SessionPlan.PlannedMutations` mit `SemanticKey` `hof.add` oder `hof.mirror`, `RequestedValue` = der Quellidentität und `Operation` `exact-file-overlay` (geschrieben) oder `session-deletion` (durch eine Spiegelung entfernt); die Pfade sind Teil von `TouchedFiles`. Ein Problem macht den Plan mit den unten aufgeführten Codes nicht ausführbar.
2. **Start**. Nachdem ein veraltetes Journal wiederhergestellt wurde, werden die Operationen erneut anhand der aktuellen Dateien aufgelöst und an die Sitzungstransaktion übergeben: Geschriebene Dateien sind Overlays, durch Spiegelung entfernte Dateien sind Sitzungslöschungen. Eine Zieldatei, deren Bytes bereits der Quelle entsprechen, bleibt unberührt und ist kein Transaktionseintrag.
3. **Sitzungsende, Fehler, Absturz.** Die Transaktion stellt jeden Zielordner exakt wieder her: Vorhandene Dateien erhalten ihre ursprünglichen Bytes, Zeitstempel und Attribute zurück; von der Sitzung erstellte Dateien werden entfernt; durch Spiegelung entfernte Dateien kehren zurück. Nach einem Absturz des Eigentümers läuft dieselbe Wiederherstellung beim nächsten Start oder über `/recover` (siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md)).

Außerhalb der Zielordner wird nichts geschrieben. Quelldateien werden nur gelesen. `.omsilaunch\hof\` wird von einer Sitzung niemals verändert.

<a id="conflict-rules"></a>
## Konfliktregeln

Operationen werden der Reihe nach angewendet und müssen übereinstimmen:

- Zwei Operationen, die dieselbe Zieldatei mit denselben Bytes schreiben, werden zusammengeführt; bei unterschiedlichen Bytes: `OL_E_HOF_CONFLICT`.
- Eine Spiegelung, die eine Datei entfernen würde, die eine frühere Operation schreibt, oder eine Operation, die eine Datei schreibt, die eine frühere Spiegelung entfernt: `OL_E_HOF_CONFLICT`.
- Eine Spiegelung umfasst die HOF-Menge des Ordners für die Sitzung: die vorhandenen Dateien plus die Dateien, die frühere Operationen dort schreiben.
- Ein HOF-Ziel, das auch von der Sitzungsdarstellung (Startbild oder Internettexturen) geschrieben wird: `OL_E_HOF_CONFLICT` (kann mit den aktuellen Darstellungspfaden, die außerhalb von `Vehicles\` liegen, nicht auftreten).
- Wenn ein Ziel bereits eine Datei mit demselben Namen in anderer Groß-/Kleinschreibung enthält, wird die vorhandene Schreibweise beibehalten.

<a id="confinement"></a>
## Eingrenzung

Jede Quelle und jedes Ziel muss innerhalb der Installation bleiben: Absolute Pfade, `..`-Segmente und Pfade, die das Stammverzeichnis verlassen, ergeben `OL_E_HOF_PATH_ESCAPE`; ebenso Ziele (und `Vehicle`-Quellen) außerhalb von `Vehicles\`, Sammlungsnamen, die kein einfacher Verzeichnisname sind, sowie jeder Quell- oder Zielordner, der über eine Junction oder einen symbolischen Link erreicht wird. `.hof`-Dateien, die selbst Reparse-Points sind, werden ignoriert.

<a id="limits"></a>
## Grenzwerte

| Grenzwert | Wert | Fehler |
| --- | --- | --- |
| Größe einer HOF-Datei | 8 MiB | `OL_E_HOF_INVALID` |
| Dateien pro Operation | 256 | `OL_E_HOF_INVALID` |
| Insgesamt geschriebene Bytes pro Sitzung | 256 MiB | `OL_E_HOF_INVALID` |
| Ziele pro Operation | mindestens 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Fehlercodes

| Code | Ursache |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | Die Sammlung, das Verzeichnis, das Fahrzeug oder die Datei existiert nicht, enthält keine `.hof`-Datei oder es fehlt eine in `Files` genannte Datei. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Eine Zielfahrzeugdatei oder ein Zielordner existiert nicht. |
| `OL_E_HOF_PATH_ESCAPE` | Ein Pfad verlässt die Installation, ein Ziel liegt nicht unterhalb von `Vehicles\`, ein Sammlungsname ist kein einfacher Name, oder eine Junction bzw. ein symbolischer Link wird durchlaufen. |
| `OL_E_HOF_CONFLICT` | Zwei Operationen widersprechen sich bezüglich einer Zieldatei (siehe [Konfliktregeln](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Keine Ziele, keine Quelle, eine `File`-Quelle, die keine `.hof`-Datei ist, ein Ziel, das eine andere Datei als `.bus`/`.ovh` ist, oder ein überschrittener [Grenzwert](#limits). |

<a id="using-it"></a>
## Verwendung

Befehlszeile (eine Operation; mehrere Operationen erfordern `/spec` oder ein Sitzungsprofil):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (`/spec`-JSON, Enums als Ganzzahlen):

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

Voreinstellung eines Sitzungsprofils (eine `package`-Quelle ist ein Verzeichnis innerhalb des Profilpakets):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: Setzen Sie `LaunchSpec.HofManagement` und rufen Sie wie gewohnt `PlanSessionAsync` / `StartSessionAsync` auf; `/list:Hofs` (`ContentQueryKind.Hofs`) listet die installierten HOF-Dateien auf.

Siehe auch: [LaunchSpec](launchspec.md), [Sitzungsprofile](session-profiles.md), [CLI](cli.md), [Fehler](errors.md), [Transaktionen und Recovery](../concepts/transactions-and-recovery.md).
