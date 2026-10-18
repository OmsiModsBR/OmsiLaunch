# Bekannte Einschränkungen

<!-- l10n: source=reference/known-limitations.md source-sha256=fe1eccc6b9e4b3d93fff33deeecda6b97abd139d7295dba1d6542fb6e57231a7 -->
> Übersetzung der [englischen Originalseite](../../../reference/known-limitations.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite führt, abgeleitet aus dem Code, alles in OmsiLaunch 0.1.0-beta4 auf, was `UNAVAILABLE`, `PARTIAL` oder ein akzeptiertes Risiko ist, damit Anwender und Integratoren nicht auf Verhalten aufbauen, das das Produkt nicht bietet. Jede Zeile nennt die Einschränkung, ihre Stabilität, den Grund für ihr Bestehen und die Stelle, an der sie ausführlich dokumentiert ist. Die englische Dokumentation ist normativ; lokalisierte Fassungen unter `docs/localized/` werden nicht auf demselben Stand gepflegt und können hinterherhinken (siehe den letzten Abschnitt).

<a id="compatibility"></a>
## Kompatibilität

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Capabilities, die Objektlayouts lesen (Fahrzeuge, Humans, Wetter, Karte, Kamera, Fahrplan, Skripte, Fahrer, Fahrscheine), werden nur auf einem bekannten Layout (`Omsi23004`) verifiziert; auf einem anderen OMSI-2-Build sind sie nicht verfügbar (`OL_E_CAPABILITY_UNRESOLVED`), bis strukturelle Validatoren existieren | `PARTIAL` auf unbekannten Builds | [Kompatibilität](compatibility.md) |
| Nur das bekannte Layout wurde zur Laufzeit validiert; die build-unabhängige Auflösung wird offline gegen die Referenz-Programmdatei verifiziert, wobei ihre Identität zurückgehalten wird | `EXPERIMENTAL` auf unbekannten Builds | [Status der Runtime-Validierung](../status/runtime-validation-status.md) |
| Nur Windows 10+ x64; kein Windows 7/8, kein XP, kein ARM64 | `UNAVAILABLE` | [Kompatibilität](compatibility.md) |
| Das Plugin benötigt zusätzlich zur vom Controller verwendeten x64-Runtime die **x86**-.NET-6-Runtime | | [Kompatibilität](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## Weltstart und Startoptionen

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) ist anhand der statischen Analyse des Last-State-Zweigs von OMSI implementiert und nur offline validiert | `EXPERIMENTAL` | [CLI](cli.md) |
| Explizites oder System-Datum, -Uhrzeit und -Jahr (`/date`, `/time`, `/year`, Profil `new.date`/`new.time`/`new.year`) gelten nur für NEW_MAP-Starts und sind offline validiert; die Runtime-Validierung steht aus. Der 31. Dezember eines Schaltjahrs kann nicht angefordert werden (der Kalender von OMSI läuft bei Tag 366 über) | `EXPERIMENTAL` | [LaunchSpec](launchspec.md#datespec-timespec-yearspec) |
| Wettervoreinstellung, ICAO und aktuelles reales Wetter beim Start (`/weather*`, `new.weather`) werden unmittelbar nach dem Start der Welt über das eigene Wetterladen und die METAR-Flags von OMSI angewendet (statisch abgeschlossen, BI-003); die Runtime-Validierung steht aus. Das Wetter, das OMSI im allerersten Frame zeigt, kann noch das Standardwetter der Karte sein | `EXPERIMENTAL` | [CLI](cli.md) |
| Modell, Lackierung, HOF, Fuhrparknummer und Kennzeichen des Spielerfahrzeugs beim Start (`/vehicle`-Familie, `PlayerVehicleSpec`) werden gegen den Inhaltskatalog aufgelöst, aber nicht angewendet; werden sie angefordert, ist der Plan nicht ausführbar. Die native Zuweisungssequenz (Platzierung über den Positionsdialog von OMSI, `MakeVehicle`, die Finalisierungsschritte pro Einheit, `CopyTempListIntoMainList`, der Spielerindex) ist statisch abgeschlossen, erzeugt und registriert aber ein aktives Fahrzeug und ist nicht implementiert, bevor sie zur Laufzeit validiert werden kann; sie ist für eine spätere Beta geplant. Eine gespeicherte Situation oder `/last` stellt ihr eigenes Spielerfahrzeug wieder her | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [Capabilities](capabilities.md) |
| Ein Einstiegspunkt per Identität (`/entrypoint:<identity>`) wählt die exakte Bezeichnung, die OMSI anzeigt (statisch abgeschlossen, BI-001); ein späterer Datensatz mit derselben Bezeichnung kann nicht ausgewählt werden, wie im eigenen Dialog von OMSI. Die Runtime-Validierung steht aus | `EXPERIMENTAL` | Plan-Capability `world.entrypoint-identity` |
| Tastatur- und Controller-Einstellungen per Schlüssel (`Environment.Keyboard`, `Environment.Controllers`) haben keine katalogisierten Schlüssel: Stattdessen werden mit `InputSpec` ganze Dateien ersetzt | `UNAVAILABLE` | [LaunchSpec](launchspec.md#inputspec) |
| Kooperatives Herunterfahren (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) ist Opt-in und nur offline validiert; standardmäßig wird OMSI beim Ende einer Sitzung terminiert und `ShutdownTimeoutSeconds` hat keine Wirkung | `EXPERIMENTAL` | [CLI](cli.md) |
| Die Diagnose-Flags `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` füllen `DiagnosticsSpec` ohne weitere Wirkung (der Host-Trace unter `.omsilaunch\diagnostics` wird immer geschrieben); `/trace-plugin` und `/trace-native` fügen ihm Plugin- und Native-Trace-Ereignisse hinzu | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` sind Validierungs-Harnesses | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## Sitzungsende und Prozesssteuerung

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Das Beenden der Sitzung ist eine erzwungene Beendigung: `session.stop`, „End session“ im Tray, Ctrl+C und `CloseAsync` führen alle zu `TerminateProcess`. Die Beendigungsroutine von OMSI wird nicht ausgeführt, OMSI schreibt `options.cfg` und seine Logs beim Beenden nicht neu, und jeder nicht gespeicherte OMSI-Zustand geht verloren. Das ist Absicht: Es verhindert, dass OMSI wiederhergestellte Dateien überschreibt. | beabsichtigt | [Sitzungslebenszyklus](../concepts/session-lifecycle.md) |
| Ein kooperatives Herunterfahren per WM_CLOSE mit Timeout und Rückfall auf erzwungene Beendigung ist nicht implementiert | `UNAVAILABLE` (Produktentscheidung, S-11; OMSI ignorierte in der Runtime-Closure-Runde `WM_CLOSE` an sein Hauptfenster) | [Status der Runtime-Validierung](../status/runtime-validation-status.md) |
| Beim Schließen der Konsole oder bei der Abmeldung hat der Eigentümer ein Budget von 4 s zum Beenden und Wiederherstellen; alles Verbleibende wird beim nächsten Start über das Journal wiederhergestellt | Schließen der Konsole zur Laufzeit validiert; Abmeldung nicht erprobt | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## Transaktion, Recovery und Lease

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Die Installations-Lease ist ein `Local\`-Semaphor: ein Eigentümer pro Installation **pro Anmeldesitzung**; nicht benutzerübergreifend durchgesetzt; nicht freigegeben, solange ein anderer Prozess ein Handle hält; jeder Prozess desselben Benutzers kann den Namen belegen | akzeptiertes Risiko (S-18) | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| Die Recovery (Absturzwiederherstellung) wird abgelehnt (`OL_E_INSTALLATION_BUSY`), solange der im Journal erfasste OMSI-Prozess – oder bei einem Journal ohne PID irgendein `Omsi.exe` aus diesem Stammverzeichnis – läuft | beabsichtigt | wie oben |
| Ein ursprünglich nicht vorhandener Overlay-Pfad, dessen Inhalt sich während der Sitzung geändert hat, blockiert die Wiederherstellung (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`), bis er geprüft wurde | beabsichtigt | wie oben |
| Journale aus der Zeit vor den Eigentums-Fingerprints können nur von einer Sitzung mit identischen geplanten Bytes abgeschlossen werden (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | wie oben |
| Nur sitzungseigene Pfade werden wiederhergestellt. Eigene Schreibvorgänge von OMSI während einer Sitzung (`options.cfg` `[last_map]`, wenn keine Einstellung `options.cfg` überlagert, `Texture\standard.ipr`, Caches, `laststn.osn`, Fahrerprofil, Logs) bleiben bestehen, wie nach einem direkten OMSI-Start | beabsichtigt | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| Das Entfernen eines veralteten `closecheck` vor einer Sitzung ist dauerhaft (protokolliert, nicht wiederhergestellt), wenn `SuppressStaleClosecheckWarning` true ist | beabsichtigt | wie oben |

<a id="runtime-control"></a>
## Runtime-Steuerung

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| `weather.set` und `weather.metar.set` schreiben den Quell-Wetterdatensatz von OMSI (statisch abgeschlossen); Persistenz über den Wetter-Tick, Auflösung des Wolkentyps und der METAR-Download stehen noch zur Runtime-Validierung aus. Abgeleitete Werte (Luftfeuchtigkeit, Lichtfaktoren, tatsächlicher Wind) können nicht gesetzt werden; solange OMSI pausiert ist, werden sie erst nach dem Fortsetzen aktualisiert | `EXPERIMENTAL` | [Capabilities](capabilities.md) |
| `SetActualDateTime` kopiert nur die PC-Uhr in die Startuhr von OMSI (statische Analyse); OmsiLaunch ruft es nicht auf und setzt die Startuhr selbst | beabsichtigt | `calendar.set-actual-date-time` |
| Schreiben von String-Variablen, benannte Trigger und Sound-Trigger laufen über die eigenen Methoden von OMSI (statisch abgeschlossen, BI-004); ihre Wirkung auf Fahrzeugskripte steht noch zur Runtime-Validierung aus. Ziele sind nur Straßenfahrzeuge | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| Keine Fahrzeugverlagerung, keine kachelübergreifende räumliche Neubindung, keine ODE-sichere Transformationshoheit; Positionsfelder sind nur lesbar | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` benötigen ein PlayerVehicle; der Headless-Start mit NEW_MAP hat keines (eine gespeicherte Situation liefert eines) | beabsichtigt (BI-007) | RV-004 |
| Runtime-Änderungen (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, Spawn, Place-Random, D3D-Texturen) werden nicht im Journal erfasst und nicht wiederhergestellt | beabsichtigt | [Runtime-Steuerung](runtime-control.md) |
| Die Veraltung von Handles wird anhand der eigenen, nie wiederverwendeten Instanznummer (`IDCode`) von OMSI und, bei Humans, anhand von Despawn und Respawn entschieden; sie ist offline validiert. Das natürliche Entfernen (RV-002: KI-Autos, die das geladene Wegenetz verlassen) muss noch zur Laufzeit beobachtet werden, und es wird noch kein Ereignis `entity.removed` ausgegeben | `PARTIAL` | [Runtime-Steuerung](runtime-control.md) |
| Ergebnisse sind durch die Mailbox von 64 KiB begrenzt: Lange Listen werden gekürzt (`truncated=true`); Pixel-Nutzdaten sind auf 48 KiB pro `d3d.texture.update` begrenzt | beabsichtigt | [Capabilities](capabilities.md) |
| Single-Flight-Kanal: eine Anforderung gleichzeitig pro Sitzung; ein belegter Slot ergibt `OL_E_RUNTIME_CHANNEL_BUSY`; Anforderungs-IDs dürfen nicht wiederverwendet werden | beabsichtigt | [Runtime-Steuerung](runtime-control.md) |
| Die Telemetrie ist ein Ring mit 64 Ereignissen: Ein Host, der mehr als 64 Ereignisse zurückfällt, verliert die ältesten und wird darüber informiert, wie viele (`telemetry.lost`) | `EXPERIMENTAL` | [Permanentes Plugin](../concepts/permanent-plugin.md) |
| Die Direct3D-Device-Hooks eines Moduls sind Vtable-Hooks: Auf einer Installation, auf der eine andere Komponente die Originalmethoden des Devices zurückschreibt (auf der Regressionsinstallation kurz nach dem Beginn des Gameplays beobachtet), werden sie nicht mehr aufgerufen und können bei der Bereinigung nicht entfernt werden (`OL_E_MODULE_CLEANUP_INCOMPLETE`; die Komponente bleibt geladen) | `PARTIAL` | [Runtime-Module](runtime-modules.md#native-components-and-hooks) |
| Das Laden von Runtime-Modulen ist offen: OmsiLaunch prüft nicht, wer ein Modul erstellt hat; es garantiert nur, dass Module nie im OmsiLaunch-Prozess laufen und dass ein Sitzungsprogramm auf seine Aufgabe beschränkt ist (kein zweiter Prozess, mit der Sitzung beendet) | beabsichtigt (E21) | [Runtime-Module](runtime-modules.md#security-boundary) |
| Modulprotokoll 2 (Einstellungen, Standardaktivierung, die Einstellungsdatei, Sitzungsprogramme, Plugin-Konflikte, Hinweise, `get_value`) ist nur offline validiert | `EXPERIMENTAL` | [Runtime-Module](runtime-modules.md) |
| Ein D3D-Geräte-Reset wurde zur Laufzeit beobachtet (`resetting`, `restored`, Invalidierung der Generation); ein eigenständiger `lost`-Übergang wurde nicht erzeugt, weil das Device von OMSI direkt zu `DEVICENOTRESET` überging | `PARTIAL` (RV-007) | [Status der Runtime-Validierung](../status/runtime-validation-status.md) |
| Runtime-Listen werden seitenweise geliefert (`cursor`, `limit`, `next_cursor`): Eine Seite enthält höchstens die maximale Zeilenzahl der Liste und das, was in den Runtime-Slot von 64 KiB passt | `EXPERIMENTAL` | [Capabilities](capabilities.md#pagination) |
| Einige erweiterte Felder des Karten-/Kachel-/Pfad-/Objektgraphen werden nicht bereitgestellt; Runtime-Reader sind profilgebundene typisierte Snapshots, niemals beliebiger Speicherzugriff | beabsichtigt | [Capabilities](capabilities.md) |
| In-Process-Speicherlesevorgänge folgen dem Muster „erst prüfen, dann verwenden“ gegen ein laufendes OMSI; eine gleichzeitige Änderung durch OMSI zwischen Prüfung und Lesevorgang kann einen inkonsistenten Snapshot ergeben (`OL_E_RUNTIME_OPERATION_FAILED`) | akzeptiertes Risiko (S-33) | |

<a id="local-control-plane-and-trust-model"></a>
## Lokale Steuerungsebene und Vertrauensmodell

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Vertrauensmodell „gleicher Benutzer“: Die Named Pipe (`CurrentUserOnly`), die Speicherzuordnungen für Handoff, Telemetrie und Runtime sowie das Lease-Semaphor sind für jeden Prozess desselben Windows-Benutzers zugänglich. Ein solcher Prozess kann den Status lesen, die Sitzung beenden oder Runtime-Operationen ausführen, sobald er die `session_id` gelesen hat. | akzeptiertes Risiko (S-06, S-30) | [Lokale Steuerung](local-control.md) |
| Der Steuerungsendpunkt existiert nur, solange der Eigentümer `Running` ist; ein Client erhält während des Starts und nach dem Ende der Sitzung `OL_E_NO_ACTIVE_SESSION` (Exitcode 4) | beabsichtigt | [Lokale Steuerung](local-control.md) |
| Wenn bereits ein anderer Prozess den Pipe-Namen besitzt, läuft der Eigentümer ohne Endpunkt weiter (`ListenFault`), und ein zweiter Start kann fälschlicherweise `OL_E_SESSION_ALREADY_ACTIVE` melden | akzeptiertes Risiko | [Lokale Steuerung](local-control.md) |
| `.omsilaunch\` erbt die ACL des OMSI-Stammverzeichnisses; es wird keine explizite Zugriffssteuerung angewendet | akzeptiertes Risiko (S-31) | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## Diagnose und Ausgabe

| Einschränkung | Stabilität | Details |
| --- | --- | --- |
| Diagnosedaten sind ausschließlich lokale Dateien (`.omsilaunch\diagnostics`); nichts wird hochgeladen, und es gibt keine Fernmeldung | beabsichtigt | [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) |
| Die Aufbewahrung behält die 50 neuesten Sitzungen; ältere Diagnosedaten mit Sitzungspräfix werden gelöscht, wenn eine neue Sitzung startet | beabsichtigt | wie oben |
| JSON-Ausgabe und Diagnosedaten enthalten Installationspfade (`RootPath`, Asset-Verzeichnisse, `.itx`-Pfade) | beabsichtigt (lokale Daten) | |
| Der Sitzungsfehlerdialog von `OmsiLaunchW.exe` zeigt als Meldung die Fehlernutzdaten des Plugins (z. B. `{"name":"world.failed",...}`) statt eines Satzes; die `Code:`-Zeile ist korrekt | kosmetisch | [Windows-Tray](windows-tray.md) |
| Eine D3D-Anforderung, die von der nativen Bridge vor jedem Direct3D-Aufruf abgelehnt wird, meldet `native_status` korrekt, ihr `detail`-Text lautet jedoch `HRESULT 0x00000000` | kosmetisch | [Capabilities](capabilities.md) |
| Das Tray-Statusfenster ist ein beim Öffnen erstellter Snapshot der geplanten Sitzung; es wird nicht aktualisiert und zeigt keine Live-Werte von OMSI | beabsichtigt | [Windows-Tray](windows-tray.md) |

<a id="documentation"></a>
## Dokumentation

Die englischen Seiten unter `docs/` sind die normative Dokumentation für dieses Release. `docs/localized/<locale>/` enthält Übersetzungen derselben `0.1.0-beta4`-Seiten (siehe [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); wo eine Übersetzung vom englischen Text abweicht, sind der englische Text und der Code maßgeblich. Die dort aufgeführten historischen und Legacy-Seiten sind nur auf Englisch verfügbar.

Siehe auch: [Capabilities](capabilities.md), [Status der Runtime-Validierung](../status/runtime-validation-status.md), [Fehler](errors.md).
