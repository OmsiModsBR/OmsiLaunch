# Capabilities

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Übersetzung der [englischen Originalseite](../../../reference/capabilities.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite dokumentiert, was OmsiLaunch kann. Ihre Quelle ist der kanonische Capability-Katalog `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): Jede Capability, jede öffentliche Runtime-Operation mit ihren Argumenten, CLI-Routen und nativer Capability sowie ein Nachweisstatus pro Capability sind dort einmal deklariert. Die Menge der Runtime-Operationen, die Argumentvalidierung, die CLI-Befehlsrouten, das native Gate pro Operation im Plugin, `GetCapabilitiesAsync` und die [Katalogtabelle](#capability-catalog) unten werden daraus abgeleitet. Für jede Runtime-Operation gibt die Seite die Argumente, Ergebnisschlüssel und Fehler an. Wie eine Operation aufgerufen wird (CLI-Routen, API-Envelope, Timeouts, Handles), steht unter [Runtime-Steuerung](runtime-control.md); der Nachweis pro Sitzung steht unter [Status der Runtime-Validierung](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Klassifizierung und Stabilität

`PublicCapabilityClassification` hat vier Werte. Sie werden wie folgt auf das Stabilitätsvokabular abgebildet, mit Herabstufungen, wo der Nachweis unvollständig ist:

| Klassifizierung | Bedeutung | Stabilität |
| --- | --- | --- |
| `PublicStableBeta` | Öffentlich, Teil des Beta-Vertrags, zur Laufzeit validiert | `STABLE_BETA` (herabgestuft auf `PARTIAL`, wo vermerkt) |
| `PublicExperimental` | Öffentlich, kann sich ändern, zur Laufzeit validiert oder statisch validiert | `EXPERIMENTAL` (herabgestuft auf `PARTIAL`, wo vermerkt) |
| `InternalOnly` | Forschungsprimitiv; nicht über die öffentliche API oder CLI erreichbar | `INTERNAL` |
| `Unsupported` | Für Diagnosezwecke erkannt, abgelehnt oder nicht vorhanden | `UNAVAILABLE` |

`PublicCapabilityKind` unterscheidet `Read`, `Write`, `Action` und `Event`. Jede Runtime-Capability erfordert eine Sitzung im Zustand `Running` (`RequiresSession` in der Registry) und ihre native Capability, aufgelöst und verifiziert auf dem laufenden Build; eine Operation, deren native Capability nicht verfügbar ist, wird mit `OL_E_CAPABILITY_UNRESOLVED` abgelehnt. `RequiresExactProfile` kennzeichnet Capabilities, die von nativen Zielen abhängen (siehe [Kompatibilität](compatibility.md)).

Runtime-Ergebnisse sind Dictionaries aus Zeichenfolgen. Schlüssel, die mit `internal_` beginnen oder auf `_address`, `_pointer` oder `_vmt` enden, werden an der API-Grenze entfernt (`ScrubInternalValues`) und sind hier nicht aufgeführt. Die Ergebnisschlüssel in den folgenden Operationstabellen sind exakt die Schlüsselmengen, die das Produkt zurückgibt: Sie wurden durch Ausführen jeder öffentlichen Operation in einer echten Sitzung erfasst (Dokumentationserfassung des Runtime-Abschlusses, Sitzung `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; Listenzeilen werden als `<row>.<n>.<field>` geschrieben).

Wie ein Fehlschlag einer Operation gemeldet wird (`CurrentRuntimeControl.Execute`):

| Fehlschlag | `ErrorCode` | `Values` |
| --- | --- | --- |
| Ablehnung durch die Registry (unbekannte oder `internal.*`-Operation, fehlendes erforderliches Argument) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | keine |
| Eine bewusste Ablehnung durch das Produkt (ein abgeleiteter Wetterschlüssel in `weather.set`) | der spezifische Code (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (Satz) |
| Ein D3D-Fehlschlag | der spezifische `OL_E_D3D_*`-Code | `detail`, `native_status` |
| Ein typisierter Fehlschlag auf Plugin-Seite (veraltetes Handle, unbekannter Name, Wert außerhalb des Bereichs, kein Spielerfahrzeug, ...) | der spezifische Code, zum Beispiel `OL_E_RUNTIME_OBJECT_HANDLE_STALE` oder `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (die Meldung), `exception` (.NET-Typname) |
| Eine Operation, deren native Capability auf dem laufenden Build nicht aufgelöst ist | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (die native Capability) |
| Eine unerwartete Ausnahme auf Plugin-Seite ohne Code | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Ein Ergebnis, das nicht in die Mailbox von 64 KiB passt | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (seitenweise Listen werden stattdessen gekürzt und mit `next_cursor` fortgesetzt, siehe [Paginierung](#pagination)) | keine |

Die Spalte `Errors` (Fehler) jeder Tabelle nennt die spezifischen Codes; sie kommen unverändert in `ErrorCode` über das Plugin, die Bridge, die Steuerungsebene, die API und die CLI an.

<a id="capability-catalog"></a>
## Capability-Katalog

Eine Zeile pro Katalogeintrag, erzeugt aus `PublicCapabilityRegistry.All` (das Dokumentations-Gate schlägt fehl, wenn diese Tabelle veraltet ist). `Session` ist `Running` bei Capabilities, die eine laufende Sitzung benötigen. `Operations` sind die konkreten Runtime-Operations-IDs (Vertragszeilen unter [öffentliche Runtime-Operationen](#public-runtime-operations)). `Native capability` nennt die Einträge des nativen Zielkatalogs (siehe [Kompatibilität](compatibility.md)), die auf dem laufenden Build aufgelöst und verifiziert sein müssen. `Evidence` verwendet ein einheitliches Vokabular:

| Nachweis | Bedeutung |
| --- | --- |
| RUNTIME_VALIDATED | In einer echten OMSI-Sitzung beobachtet; die Sitzungs-IDs stehen unter [Status der Runtime-Validierung](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implementiert und offline verifiziert; die Wirkung zur Laufzeit wurde nicht beobachtet. |
| PARTIAL | Funktioniert für einen Teil des Vertrags; der Hinweis nennt den offenen Teil. |
| RUNTIME_VALIDATION_REQUIRED | Implementiert, wartet auf seine Runtime-Validierung. |
| UNAVAILABLE | Für Diagnosen erkannt und abgelehnt, oder nicht implementiert; wird es beim Start angefordert, ist der Plan nicht ausführbar. |

Eine Operation, die etwas ändert, nennt ihre Mutationsrichtlinie nach ihrer ID (`PublicRuntimeOperation.MutationPolicy`); keine Runtime-Operation wird im Journal erfasst:

| Mutationsrichtlinie | Bedeutung |
| --- | --- |
| `none` | Nur Lesezugriffe (nicht dargestellt). |
| `omsi-state` | Ändert den Zustand von OMSI im Arbeitsspeicher; nichts macht es rückgängig; es endet mit dem OMSI-Prozess. |
| `session-resource` | Erzeugt oder ändert eine Ressource, die der Sitzung gehört; wird explizit oder am Ende der Sitzung freigegeben. |
| `session-policy` | Installiert eine Richtlinie, die das Plugin erneut anwendet, bis sie entfernt wird oder die Sitzung endet. |

<!-- generated:capability-catalog:begin -->
| Capability | Classification | Kind | Session | Operations | Native capability | Evidence | CLI | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | nein |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | nein |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | nein |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: Plugin-Ereignisse laufen über einen nummerierten Ring; ein Leser, der zurückfällt, erhält telemetry.lost mit der Anzahl. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | nein |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | nein |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | nein |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4, Runde A: bekanntes Layout. |
| `boot.headless-start` | `PublicStableBeta` | Action | nein |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | nein |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | nein |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | nein |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): OMSI zeigt den ersten Datensatz jeder exakten Bezeichnung an; der Start wählt ihn über diese Bezeichnung aus. Statisch abgeschlossen; Runtime-Validierung steht aus. |
| `world.saved-situation` | `PublicStableBeta` | Action | nein |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | nein |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: Der native Zweig ist statisch abgeschlossen (BI-006); Runtime-Validierung steht aus. Niemals eine Heuristik nach der neuesten .osn. |
| `world.explicit-date` | `PublicExperimental` | Action | nein |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: wird vor NewSituation in die Startuhr von OMSI geschrieben (statische Analyse); Runtime-Validierung steht aus. |
| `world.explicit-time` | `PublicExperimental` | Action | nein |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: wird vor NewSituation in die Startuhr von OMSI geschrieben (statische Analyse); Runtime-Validierung steht aus. |
| `world.explicit-year` | `PublicExperimental` | Action | nein |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: wird vor NewSituation in die Startuhr von OMSI geschrieben (statische Analyse); Runtime-Validierung steht aus. |
| `world.weather` | `PublicExperimental` | Action | nein |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): wird unmittelbar nach dem Start der Welt angewendet, über das eigene loadWeather von OMSI oder seine METAR-Flags; überschreibt das Wetter einer gespeicherten Situation. Statisch abgeschlossen; Runtime-Validierung steht aus. |
| `player-vehicle.model` | `Unsupported` | Action | nein |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; für Diagnosen aufgelöst, nicht angewendet. |
| `player-vehicle.repaint` | `Unsupported` | Action | nein |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | nein |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | nein |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | nein |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false bei einem Headless-Start; der vollständige Snapshot bei einer gespeicherten Situation. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: Die native Zuweisungssequenz ist statisch abgeschlossen (research/reports/beta4-static/entrypoint-last-player-vehicle.md Abschnitt 3), aber vor der Runtime-Validierung nicht implementiert; für eine spätere Beta geplant. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | nein |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | nein |  |  | STATICALLY_VALIDATED |  | Beta 4: transaktionales Overlay, offline validiert. |
| `input.controller` | `PublicExperimental` | Write | nein |  |  | STATICALLY_VALIDATED |  | Beta 4: transaktionales Overlay, offline validiert. |
| `presentation.splash` | `PublicStableBeta` | Write | nein |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | nein |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4, Runde B: Die Unterdrückung wird angewendet und über den Telemetrie-Ring gemeldet (internet-textures.suppressed). |
| `internet-textures.override` | `PublicExperimental` | Write | nein |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Runtime-Closure I01. |
| `session-profiles` | `PublicStableBeta` | Action | nein |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009-Profilstart; 1..99 und Erkennung offline. |
| `hof.management` | `PublicStableBeta` | Write | nein |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4, Runde A, C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | nein |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | nein |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | nein |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | nein |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | nein |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | nein |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | nein |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 statisch abgeschlossen: SetActualDateTime kopiert nur die PC-Uhr in die Startuhr; Startdaten verwenden world.explicit-date. Überholt: Native ABI und Nachbedingungen sind nicht abgeschlossen. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: schreibt actWeather (Weather+0x08), den Datensatz, aus dem der Wetter-Tick von OMSI ableitet; statisch abgeschlossen. Abgeleitete Werte (Luftfeuchtigkeit, Lichtfaktoren, tatsächlicher Wind) bleiben schreibgeschützt (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Runtime-Validierung steht aus. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Benötigt ein PlayerVehicle (eine gespeicherte Situation liefert eines); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE bei einem Headless-NEW_MAP. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | Die Erkennung veralteter Handles nach natürlichem Entfernen (RV-002) hat keinen sicheren Runtime-Produzenten und ist offline validiert. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paged) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paged), `timetable.trips.list` (paged), `timetable.lines.list` (paged), `timetable.rv-files.list` (paged), `timetable.track-entries.list` (paged), `timetable.bus-stops.list` (paged), `timetable.station-links.list` (paged), `timetable.tours.list` (paged), `timetable.profiles.list` (paged), `timetable.tour-entries.list` (paged), `timetable.logs.read` (paged), `timetable.no-rv-numbers.list` (paged) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paged), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paged), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Schreiben: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): OMSI kopiert einen String konstanter Form; statisch abgeschlossen. Runtime-Validierung steht aus. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): virtuelles TriggerXML/SoundTrigger über die profilierten VMT-Slots; statisch abgeschlossen. Runtime-Validierung steht aus. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Device-Reset beobachtet (resetting, restored, Invalidierung der Generation); ein eigenständiger lost-Übergang wurde nicht erzeugt (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` ist `InternalOnly`: Sie existiert im Plugin für Forschungszwecke (sie gibt eine native Adresse zurück) und wird an der API-Grenze und von der lokalen Steuerungsebene (Control Plane) abgelehnt, die beide den Operationsnamen zuerst gegen `PublicRuntimeOperationIds` prüfen; die CLI hat keine Route dafür. `session.stop` terminiert OMSI; kooperatives Herunterfahren ist nicht implementiert.

<a id="pagination"></a>
## Paginierung

Jede Listenoperation (im [Capability-Katalog](#capability-catalog) mit „(paged)“ gekennzeichnet) nimmt zwei optionale Argumente entgegen und beantwortet vier Seitenfelder:

| Argument | Bedeutung |
| --- | --- |
| `cursor` | Wo die Seite beginnt: der `next_cursor` der vorherigen Seite. Weggelassen: die erste Seite. Ein Cursor aus einer anderen Liste oder ein fehlerhafter ergibt `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Gewünschte Zeilen (eine positive Ganzzahl). Größer als die maximale Seite der Liste: Das Maximum wird verwendet. Keine positive Ganzzahl: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Ergebnisschlüssel | Bedeutung |
| --- | --- |
| `count` | Alle Datensätze, die OMSI hält. |
| `returned_count` | Zeilen in dieser Antwort. |
| `truncated` | `true`, wenn auf diese Seite weitere Datensätze folgen. |
| `next_cursor` | Nur vorhanden, wenn weitere Datensätze folgen: Übergeben Sie ihn als `cursor` für die nächste Seite. |

Der Cursor ist ein stabiler Schlüssel, keine Position. Bei Datensatz-Arrays (Fahrplan, Skriptnamen, Konstanten, Kurven, HOFs, Fahrer, Fahrscheine, Logs) sind die Zeilen nach dem absoluten Index des Datensatzes mit `<row>.<n>.<field>` (oder `name.<n>`) benannt, und der Cursor nennt den nächsten Index, sodass die erste Seite der Antwort von Beta 3 entspricht. Bei Handle-Sammlungen (`road-vehicles.list`, `humans.list`) werden die Zeilen in Handle-Reihenfolge zurückgegeben, und der Cursor nennt das zuletzt zurückgegebene Handle: Ein Objekt, das zwischen zwei Seiten erscheint, erhält ein späteres Handle und kommt auf einer späteren Seite; keines wird wiederholt. Behandeln Sie Cursor-Werte als undurchsichtig.

Maximale Seitengrößen: 1000 für `road-vehicles.list` und `humans.list`; 512 für `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` und `vehicle.curves.list`; 256 für `timetable.tour-entries.list`, `timetable.logs.read` und `vehicle.hofs.read`; 128 für die übrigen Fahrplanlisten, `drivers.read` und `tickets.read`. Überschreiten die Zeilen einer Seite dennoch die Mailbox von 64 KiB, verwirft das Plugin die letzten Zeilen, bis die Antwort passt, und `next_cursor` setzt bei der ersten verworfenen Zeile fort, sodass keine Liste wegen ihrer Größe fehlschlägt.

<a id="public-runtime-operations"></a>
## Öffentliche Runtime-Operationen

Dies ist die exakte Liste der Operations-IDs, die ein öffentliches Frontend weiterleiten darf (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Jede davon erfordert `SessionState.Running`; keine wird im Journal erfasst oder wiederhergestellt. Erforderliche Argumente werden von der Registry erzwungen, bevor die Mailbox verwendet wird (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); optionale Argumente werden vom Plugin validiert. Gemeinsame Fehlercodes aller Operationen: `OL_E_RUNTIME_OPERATION_UNKNOWN` (nicht in dieser Liste), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (das Plugin hat keine Implementierung), `OL_E_RUNTIME_OPERATION_FAILED` (unerwartete Ausnahme im Prozess; Werte `detail` und `exception`).

<a id="time"></a>
### Zeit

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `time.read` | Read | keine | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | optional `hour` (0..23), `minute` (0..59), `second` (0..59.999, Dezimalzahl); mindestens eines | die `time.read`-Schlüssel nach dem profilierten `SetTime` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Wetter

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | keine | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` sowie die Quellwerte `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, die Lichtfaktoren und die Luftfeuchtigkeiten sind die Werte pro Tick von OMSI; `cloud_type` ist die aufgelöste Wolkentyp-Nummer | |
| `weather.set` | Write | mindestens eines von `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Bereiche: Nebel 0..100000, Helligkeit und Bodenwerte 0..1, Windgeschwindigkeit 0..200, Windrichtung 0..360, Temperatur und Taupunkt -60..60, Druck 850..1100, Niederschlag 0..2, Rate 0..255, Wolkenhöhe 0..20000, `cloud_type` 1..64 Zeichen der Codepage 1252 (`-1` = keine Wolken), Booleans `true`/`false`/`1`/`0` | die Schlüssel von `weather.read`; mit `cloud_type` zusätzlich `cloud_type_resolved` (`false`, wenn OMSI den Wolkentyp nicht kennt: Es zeichnet keine) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (ein abgeleiteter Schlüssel: `relative_humidity`, `absolute_humidity`, die Lichtfaktoren, `cloud_transparency`, `precipitation_set`, `wet_ground`; das Detail nennt den stattdessen zu setzenden Schlüssel), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (unbekannter Schlüssel), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 Buchstaben oder Ziffern) | die Schlüssel von `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | keine | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Karte

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `map.read` | Read | keine | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Kamera

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | keine | `family` (0 Fahrer, 1 Fahrgast, 2 Außen, 3 Karte), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | optional `family` (0..3), `field_of_view` (10..170); mindestens eines | die `camera.read`-Schlüssel | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | erforderlich `family` (0..3); optional `preset` (0..255, nur mit Familie 0 oder 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. Die Richtlinie wird alle 100 ms erneut angewendet, bis `camera.unlock` erfolgt; eine fehlschlagende erneute Anwendung löst das Ereignis `camera.lock.degraded` einmal pro unterschiedlichem Fehler aus. |
| `camera.unlock` | Action | keine | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Straßenfahrzeuge und Spieler

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | keine | `count`, `ai_collection`, `player_index` (OMSIs roher Spielerfahrzeug-Index, so gemeldet wie gelesen; verwenden Sie `present` aus `player-vehicle.read`, um zu erfahren, ob ein Spielerfahrzeug existiert) | |
| `road-vehicles.list` | Read | optional `cursor`, `limit` | Seitenfelder; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` ist die Listenposition in OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | erforderlich `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | keine | `present` (`false`, wenn OMSI kein Spielerfahrzeug hat); bei `true`: `player_index` plus jeder `road-vehicle.read`-Schlüssel | |
| `road-vehicles.spawn` | Action | erforderlich `model` (kanonisch `Vehicles\...\*.bus`, höchstens 240 Zeichen, kein `..`, muss unterhalb der Installation existieren) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Weist das PlayerVehicle nicht zu. Dauert mehrere Sekunden; verwenden Sie das Client-Timeout von 30 s. |
| `road-vehicles.place-random` | Action | optional `ai_type` (0..255, Standard 0), `group` (0..65535, Standard 1), `type` (-1..65535, Standard -1), `scheduled` (0..1, Standard 0), `tour` (0..65535, Standard 0), `line` (0..65535, Standard 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Menschen

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | keine | `count` | |
| `humans.list` | Read | optional `cursor`, `limit` | Seitenfelder; `human.<n>.handle` (`hb-NNNNNN`; `<n>` ist die Listenposition in OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | erforderlich `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Fahrplan, Fahrer, Fahrscheine

Fahrplanlisten, `drivers.read` und `tickets.read` werden seitenweise geliefert (siehe [Paginierung](#pagination)); „Seitenfelder“ bedeutet unten `count`, `returned_count`, `truncated` und `next_cursor`, und jede Liste nimmt die optionalen Argumente `cursor` und `limit` entgegen.

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | keine | `invalid` (`true`/`false`) sowie die Datensatzanzahlen `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | optional `cursor`, `limit` | Seitenfelder; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | optional `cursor`, `limit` | Seitenfelder; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | optional `cursor`, `limit` | Seitenfelder; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | optional `cursor`, `limit` | Seitenfelder; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | optional `cursor`, `limit` | Seitenfelder; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | optional `cursor`, `limit` | Seitenfelder; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | optional `cursor`, `limit` | Seitenfelder; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | optional `cursor`, `limit` | Seitenfelder; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | optional `cursor`, `limit` | Seitenfelder; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | optional `cursor`, `limit` | Seitenfelder; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | optional `cursor`, `limit` | Seitenfelder; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | optional `cursor`, `limit` | Seitenfelder; `no_rv.<n>.group`, `no_rv.<n>.number`: die Fahrzeugnummern, die OMSI aus der freien KI-Nutzung heraushält (die Liste reservierter Nummern von OMSI als String-Liste, über sein eigenes Layout gelesen; `OL_E_RUNTIME_VALUE_INVALID`, wenn die Liste nicht die erwartete Form hat). Höchstens 256 pro Seite. Statische Analyse; Runtime-Validierung steht aus. |
| `map.tiles.list` | Read | optional `cursor`, `limit` | Seitenfelder; `tile.<n>.x`, `tile.<n>.y` (Rasterposition), `tile.<n>.tile_index` (der eigene Index der Kachel in der Kachelliste der Karte), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` mit `tile.<n>.error` für eine Kachel, die nicht gelesen werden kann. Höchstens 256 pro Seite. Position und Index aus statischer Analyse; Runtime-Validierung steht aus. |
| `drivers.read` | Read | optional `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | optional `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Fahrzeugskripte, Konstanten, Kurven, HOF (an ein Handle gebunden)

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | erforderlich `handle`; optional `cursor`, `limit` | `handle`, Seitenfelder, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | erforderlich `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | erforderlich `handle`, `name`, `value` (endlicher Float) | `handle`, `name`, `requested_value`, `value` (zurückgelesen) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | erforderlich `handle`; optional `cursor`, `limit` | `handle`, Seitenfelder, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | erforderlich `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | erforderlich `handle`, `name` (Groß-/Kleinschreibung wird beachtet, wie das Skript es deklariert, bis zu 128 Zeichen), `value` (1..1024 Zeichen) | `handle`, `name`, `value` nach dem Schreiben zurückgelesen | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (OMSI selbst würde einen unbekannten Namen stillschweigend ignorieren), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | erforderlich `handle`, `name` (Codepage 1252, bis zu 128 Zeichen), `value` (`1` führt `name` aus, `0` führt `name_off` aus, `2` führt `name_drag` aus) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. Der Skripteinstieg läuft synchron; ein Name, den das Skript nicht definiert, bewirkt nichts. Auf einen Druck folgt normalerweise ein Loslassen |
| `vehicle.sound-trigger` | Action | erforderlich `handle`, `trigger` (Codepage 1252, bis zu 128 Zeichen); optional `file` (bis zu 260 Zeichen) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (auch: das Fahrzeug hat kein Sound-Paket), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | erforderlich `handle`; optional `cursor`, `limit` | `handle`, Seitenfelder, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | erforderlich `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | erforderlich `handle`; optional `cursor`, `limit` | `handle`, Seitenfelder, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | erforderlich `handle`, `name`, `x` (endlicher Float; ein fehlendes oder nicht numerisches `x` ergibt `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (lineare Interpolation) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | erforderlich `handle`; optional `cursor`, `limit` | `handle`, Seitenfelder, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D-Operationen werden über die native Bridge auf OMSIs Haupt-/Render-Thread ausgeführt. Textur-Handles haben die Form `d3dtex-<sessionId N>-<16 hex digits>`.

| Operation | Art | Argumente | Ergebnisschlüssel | Fehler |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | keine | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | erforderlich `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); optional `levels` (0..16, Standard 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | erforderlich `handle`; optional `level` (0..15, Standard 0) | die 13 `d3d.texture.create`-Schlüssel für die angeforderte Ebene | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED` sowie die Fehler von create |
| `d3d.texture.update` | Action | erforderlich `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (dekodiert höchstens 48 KiB); optional `level` (0..15, Standard 0), `x` (0..4095, Standard 0), `y` (0..4095, Standard 0) | die 13 `d3d.texture.create`-Schlüssel nach der Aktualisierung | `OL_E_D3D_INVALID_PIXEL_BUFFER` sowie die Fehler von describe |
| `d3d.texture.release` | Action | erforderlich `handle` | die 13 `d3d.texture.create`-Schlüssel mit `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` bei wiederholter Freigabe, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Fehlgeschlagene D3D-Operationen geben die Werte `detail` und `native_status` zurück. Die Erweiterungsmethoden von `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) kapseln diese Operationen für API-Nutzer.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` gibt pro Katalogeintrag, der nicht `InternalOnly` ist, ein `Capability(Name, Available, EvidenceState, Reason)` zurück. `Name` ist die Katalog-ID und `EvidenceState` ihr Katalog-Nachweis. `Available` wird für die Installation ausgewertet:

- `Unsupported`-Einträge sind nie verfügbar; `Reason` trägt den Katalog-Hinweis.
- Jeder Eintrag benötigt die aktuelle Plattform, und jeder Eintrag außer `runtime.current-windows-x64` benötigt eine OMSI-2-Installation.
- Ein Eintrag mit nativen Capabilities meldet auf dem bekannten Layout `native: RESOLVED_KNOWN_LAYOUT`. Auf einem anderen OMSI-2-Build wird die Programmdatei statisch aufgelöst: `native: STATICALLY_RESOLVED` oder `native: PENDING_IN_PROCESS_RESOLUTION`, wobei genannt wird, was die Datei nicht aufgelöst hat. Ein ausstehender Eintrag wird weiterhin als verfügbar gemeldet, weil die Auflösung im Prozess entscheidet; schlägt sie fehl, schlägt die Operation oder der Start mit `OL_E_CAPABILITY_UNRESOLVED` fehl.

Der Planer meldet Capabilities pro Plan in `SessionPlan.RequiredCapabilities` und `SessionPlan.UnsupportedRequestedFeatures` mit denselben Katalog-IDs (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Eine nicht unterstützte angeforderte Funktion trägt ihren Katalog-Nachweis; eine erforderliche native Capability trägt `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` oder `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Bekannte Einschränkungen

- Handles sind an die Sitzung gebunden und werden als `OL_E_RUNTIME_OBJECT_HANDLE_STALE` gemeldet, sobald ihr Objekt verschwunden ist. Ein Fahrzeug-Handle ist an seine VMT, seine Definition und den `IDCode` von OMSI gebunden (eine prozessweit eindeutige Nummer, die jedes Kartenobjekt bei der Konstruktion erhält und die nie wiederverwendet wird), sodass ein an derselben Adresse neu erzeugtes Objekt immer unterschieden wird. Humans sind Pool-Slots, die für die gesamte Kartensitzung bestehen: Ein Human-Handle wird nicht mehr aufgelöst, wenn der Human despawnt (`RenderMe` 0) oder der Slot erneut gespawnt wird (neue Zielstation oder Abfahrt).
- Ergebnisse sind auf die Mailbox von 64 KiB begrenzt; jede Liste wird seitenweise geliefert (siehe [Paginierung](#pagination)).
- Kein kachelübergreifendes Versetzen von Fahrzeugen, kein Schreiben des Kalenders, keine Headless-Zuweisung des PlayerVehicle. Das Schreiben von String-Variablen, Skript-Trigger und das Schreiben des Wetters sind `EXPERIMENTAL` (Runtime-Validierung steht aus). Siehe [bekannte Einschränkungen](known-limitations.md).
