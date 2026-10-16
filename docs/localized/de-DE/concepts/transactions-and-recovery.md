# Transaktionen und Recovery

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Übersetzung der [englischen Originalseite](../../../concepts/transactions-and-recovery.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Jede OmsiLaunch-Sitzung, die eine OMSI-Datei berührt, tut dies innerhalb einer dauerhaften, im Journal protokollierten Transaktion: Die Originalbytes werden gesichert, bevor sie ersetzt werden, das Journal hält fest, wie weit die Sitzung gekommen ist, und die Wiederherstellung verifiziert jedes Backup, bevor sie es zurückschreibt. Diese Seite beschreibt diese Transaktion, wie sie von `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) implementiert und von `OmsiLaunchService.StartAsync`, `SuperviseAsync` und `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`) gesteuert wird, zusammen mit den Dateieingaben, die von `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`) berechnet werden. Sie richtet sich an Benutzer, die wissen müssen, was eine Sitzung ändert und was die Recovery (Absturzwiederherstellung) tut, und an Integratoren, die die genauen Garantien benötigen.

<a id="what-a-session-changes"></a>
## Was eine Sitzung ändert

Nur **temporäre Overlays** werden Teil der Transaktion. Sie werden berechnet, bevor die Transaktion geöffnet wird, und wiederhergestellt, wenn sie geschlossen wird.

| Sitzungseingabe | Datei(en) | Art |
| --- | --- | --- |
| `/set:<key>=<value>`, Profil-`settings`, `LaunchSpec.Environment.*` | `options.cfg` (semantische Token-Patches; CP1252-Bytes bleiben erhalten, UTF-8/UTF-16 mit BOM wird berücksichtigt) | Overlay |
| Verwaltetes Startbild (`SplashMode.Managed`, der Standard) | `GUI\NewSplashscreen_ENG.bmp` und `GUI\NewSplashscreen_<language>.bmp` | Overlay (die lokalisierte Datei wird von der Transaktion angelegt, wenn die Installation keine hat) |
| Internettexturen `Override` | `Texture\standard.itx` | Overlay |
| Internettexturen `Override` | jedes in der `.itx` aufgeführte Ziel sowie `Texture\standard.ipr` | Sitzungslöschung |
| Immer | `closecheck` (wenn vor der Sitzung nicht vorhanden) | Sitzungslöschung |

Permanente Produktdateien sind **keine** Teilnehmer der Transaktion: die Plugin-Gesamtheit (Closure) unter `plugins\OmsiLaunch.*` (wird nur validiert, siehe [permanentes Plugin](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (einmal kopiert, nie entfernt), Diagnosen unter `.omsilaunch\diagnostics`, Sitzungsprofil-Pakete sowie die Release-Dokumentation und die Beispiele. Plugins von Drittanbietern und alle anderen OMSI-Dateien werden nie aufgezählt, kopiert, entfernt oder wiederhergestellt.

OMSI selbst schreibt während einer laufenden Sitzung weiterhin seinen eigenen Zustand, genau wie bei einem normalen OMSI-Start: `options.cfg` (z. B. `[last_map]`, wenn die Sitzung eine andere Karte lädt, beim Eintritt ins Spielgeschehen neu geschrieben), `Texture\standard.ipr`, Fahrplan- und Lightmap-Caches (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, das Fahrerprofil unter `Drivers\` sowie seine Protokolle. Ein Schreibvorgang auf einen Pfad, der der Sitzung gehört (siehe oben), wird durch die Wiederherstellung rückgängig gemacht; jeder andere Schreibvorgang von OMSI bleibt nach der Sitzung bestehen, genau wie nach einem direkten Start von OMSI. Runtime-Nachweis aus der Abschlussrunde: Eine Sitzung mit gespeicherter Situation auf einer anderen Karte hinterließ `[last_map]` geändert, weil sie `options.cfg` nicht mit einem Overlay versah (`CAM01`), während `/set`-Sitzungen `options.cfg` exakt wiederherstellten (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Transaktionszustände

`TransactionState` wird nach jedem Übergang im Journal persistiert. Die Werte werden von `System.Text.Json` als Ganzzahlen serialisiert.

| Wert | Zustand | Geschrieben, wenn |
| --- | --- | --- |
| 0 | `Prepared` | Snapshots aller Overlay- und Löschpfade wurden erstellt und ihre Backups auf den Datenträger geschrieben. In der Installation hat sich noch nichts geändert. Dies ist die Recovery-Verpflichtung: Ab hier hinterlässt ein Absturz ein wiederherstellbares Journal. |
| 1 | `Applied` | Jedes Overlay wurde atomar geschrieben und jede Löschung entfernt. |
| 2 | `RuntimeDeployed` | Die Integrität des permanenten Plugins wurde für diesen Start validiert (es wird nichts bereitgestellt; der Name ist historisch bedingt). |
| 3 | `HandoffCreated` | Die Start-Übergabe, der Telemetrie-Slot und die Runtime-Mailbox existieren als benannter gemeinsamer Speicher. |
| 4 | `ProcessStarted` | `Omsi.exe` wurde erstellt. Das Journal enthält nun zusätzlich `ProcessId`, `ProcessStartFileTimeUtc` (Erstellungszeit, UTC-Ticks) und `ExecutablePath`. |
| 5 | `ProcessExited` | Der Supervisor hat das Beenden des Prozesses bestätigt (reguläres Beenden oder `TerminateProcess`). |
| 6 | `Restoring` | Die Wiederherstellung hat begonnen. |
| 7 | `Restored` | Jede eigene Datei wurde wiederhergestellt und verifiziert. Unmittelbar danach wird das Journal gelöscht und `backup\<session>` entfernt. |
| 8 | `Completed` | Im Enum deklariert, aber nie persistiert; eine abgeschlossene Transaktion hat kein Journal. |

Der Lebenszyklus einer normalen Sitzung ist daher: Snapshot -> `Prepared` -> Overlays geschrieben / Löschungen entfernt -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> Journal gelöscht -> `backup\<session>` entfernt. Die öffentlichen `SessionState`-Werte `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` und `Completed` bilden denselben Fortschritt von außen ab (siehe [Sitzungslebenszyklus](session-lifecycle.md)).

Eine Sitzung ohne Änderungen an eigenen Dateien schreibt dennoch ein Journal für ihren Lebenszyklus; dessen Wiederherstellung ist eine verifizierte Nulloperation.

<a id="journal-file"></a>
## Journal-Datei

Pfad: `<root>\.omsilaunch\journal.json`. Es gibt höchstens ein Journal pro Installation; sein Vorhandensein bedeutet „eine Transaktion steht aus“.

Felder von `TransactionJournal`:

| Feld | Typ | Bedeutung |
| --- | --- | --- |
| `SessionId` | GUID | Sitzung, der das Journal gehört; zugleich der Name des Backup-Verzeichnisses (`N`-Format). |
| `State` | Ganzzahl | `TransactionState` wie oben. |
| `Files` | Array von `JournalFile` | Ein Eintrag pro eigenem Pfad. |
| `ProcessId` | Ganzzahl oder null | OMSI-PID, ab `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long oder null | Erstellungszeit von OMSI (UTC-Ticks), ab `ProcessStarted`. |
| `ExecutablePath` | Zeichenfolge oder null | Vollständiger Pfad der gestarteten `Omsi.exe`, ab `ProcessStarted`. |

Felder von `JournalFile`:

| Feld | Typ | Bedeutung |
| --- | --- | --- |
| `RelativePath` | Zeichenfolge | Pfad relativ zum Installationsstammverzeichnis (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Ob die Datei vor der Sitzung existierte. |
| `Sha256` | Hex-Zeichenfolge | SHA-256 der Originalbytes (eines leeren Byte-Arrays, wenn `Existed` false ist). |
| `BackupPath` | Zeichenfolge | Absoluter Pfad der Sicherungskopie (nur bei `Existed` geschrieben). |
| `AppliedSha256` | Hex-Zeichenfolge oder null | SHA-256 der Overlay-Bytes, die die Sitzung auf diesen Pfad geschrieben hat; null bei Sitzungslöschungen. Dies ist der Eigentümerschafts-Fingerprint für ursprünglich nicht vorhandene Dateien. |
| `LastWriteTimeUtcTicks` | long oder null | Ursprüngliche Zeit des letzten Schreibzugriffs. |
| `CreationTimeUtcTicks` | long oder null | Ursprüngliche Erstellungszeit. |
| `Attributes` | Ganzzahl oder null | Ursprüngliche `FileAttributes` (einschließlich `ReadOnly`). |
| `SessionDeletion` | bool | True für Pfade, die die Sitzung als nicht vorhanden halten wollte (`.itx`-Ziele, `Texture\standard.ipr`, `closecheck`). |

Journale, die von früheren Builds ohne `AppliedSha256` und die Metadatenfelder geschrieben wurden, sind weiterhin lesbar; siehe [Ursprünglich nicht vorhandene Dateien](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Backup-Struktur

| Pfad | Inhalt |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Ein Verzeichnis pro Sitzung, angelegt mit dem `Prepared`-Journal. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Exakte Originalbytes einer existierenden eigenen Datei. Ursprünglich nicht vorhandene Dateien haben kein Backup. |

Backups und das Journal werden über eine temporäre Datei (`<path>.omsilaunch.tmp`) geschrieben, mit Write-Through plus einem expliziten `Flush(true)`, danach folgt ein atomares `File.Move` mit Überschreiben. Die temporäre Datei wird immer gelöscht, auch bei einem Fehler. Derselbe Schreibpfad wird für Overlays und für wiederhergestellte Originale verwendet, sodass keine `*.omsilaunch.tmp`-Datei einen abgeschlossenen Vorgang überdauert.

Backups werden erst entfernt, nachdem das Journal, das auf sie verwies, gelöscht wurde. Ein Fehler beim Löschen von `backup\<session>` ist rein kosmetisch und macht eine verifizierte Wiederherstellung nie rückgängig.

<a id="restore"></a>
## Wiederherstellung

`RestoreAsync` läuft nach `ProcessExited` (oder während der Recovery). Für jeden im Journal erfassten Pfad gilt:

| Ursprünglicher Zustand | Action |
| --- | --- |
| Existierte | Die Backup-Bytes werden gehasht und mit `Sha256` verglichen; eine Abweichung bricht mit `OL_E_RECOVERY_BACKUP_CORRUPT` ab, bevor etwas geschrieben wird. Danach werden die Bytes atomar geschrieben (bei einer schreibgeschützten aktuellen Datei wird das Attribut zuerst entfernt), und Erstellungszeit, Zeit des letzten Schreibzugriffs und Attribute werden wiederhergestellt (`RestoreMetadata`; Metadatenfehler werden ignoriert, damit ein Berechtigungsproblem eine bytegenaue Wiederherstellung nicht blockieren kann). |
| Nicht vorhanden, jetzt vorhanden, `AppliedSha256` bekannt | Die aktuellen Bytes werden gehasht. Entsprechen sie `AppliedSha256`, ist die Datei das eigene Overlay der Sitzung und wird gelöscht. Andernfalls bricht `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` die Wiederherstellung ab, und das Journal bleibt erhalten. |
| Nicht vorhanden, jetzt vorhanden, Sitzungslöschung, Journal hat `ProcessStarted` erreicht | Die Datei ist ein Nebenprodukt der Sitzung (OMSI lief, während die Installations-Lease gehalten wurde, und für diesen Pfad war verlangt, dass er nicht vorhanden bleibt). Sie wird gelöscht und als Diagnose `restore.session-artifact-removed` mit dem SHA-256 des entfernten Inhalts gemeldet. |
| Nicht vorhanden, jetzt vorhanden, Sitzungslöschung, Prozess nie gestartet | Die Datei stammt von außerhalb der Sitzung. Sie wird beibehalten, als `OL_W_RESTORE_FOREIGN_FILE_RETAINED` mit ihrem SHA-256 gemeldet, und die Transaktion wird dennoch abgeschlossen. |
| Nicht vorhanden, jetzt vorhanden, kein Eigentümerschaftsnachweis (Journal aus der Zeit vor dem Fingerprint) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; das Journal bleibt erhalten. |
| Nicht vorhanden, weiterhin nicht vorhanden | Nichts zu tun. |

Nachdem alle Dateien verarbeitet sind, liest `VerifyRestoredSnapshots` jeden Pfad erneut: Existierende Originale müssen den Hash `Sha256` ergeben, ursprünglich nicht vorhandene Pfade dürfen nicht vorhanden sein, sofern sie nicht ausdrücklich beibehalten wurden. Erst dann wird `Restored` persistiert, das Journal gelöscht (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, falls es bestehen bleibt) und das Backup-Verzeichnis entfernt. Ein Absturz zwischen `Restored` und dem Löschen des Journals führt lediglich zu einer idempotenten Wiederholung.

Wiederherstellungshinweise (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) erscheinen als `LaunchDiagnostic`-Einträge in `SessionStatus.Diagnostics` (mit `sha256` in `Data`) und in `RecoveryStatus.Diagnostics`, sodass nichts stillschweigend entfernt oder beibehalten wird.

<a id="originally-absent-files-and-ownership"></a>
### Ursprünglich nicht vorhandene Dateien und Eigentümerschaft

Ein Overlay, das auf einen zuvor nicht existierenden Pfad geschrieben wurde, wird bei der Wiederherstellung **nur dann entfernt, wenn sein Inhalt noch dem entspricht, was die Sitzung angewendet hat** (`AppliedSha256`). Hat etwas anderes es während der Sitzung ersetzt, schlägt die Wiederherstellung mit `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` fehl, und das Journal wird zur Untersuchung beibehalten.

Ein Pfad einer Sitzungslöschung (`.itx`-Ziele, `Texture\standard.ipr`, `closecheck`), der vorher nicht existierte, danach aber existiert, wird danach beurteilt, ob OMSI unter dieser Transaktion lief: Hat das Journal `ProcessStarted` erreicht, ist die Datei ein Nebenprodukt der Sitzung und wird entfernt (`restore.session-artifact-removed`); wurde der Prozess nie gestartet, wird die Datei beibehalten und als `OL_W_RESTORE_FOREIGN_FILE_RETAINED` gemeldet, und das Journal wird dennoch abgeschlossen.

### `closecheck`

`closecheck` ist OMSIs eigene Absturzmarkierung (vorhanden, wenn OMSI nicht sauber heruntergefahren wurde). Es gelten zwei Regeln:

- Existiert sie **vor** der Sitzung und ist `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` gleich `true` (der Standard), wird sie dauerhaft entfernt, bevor die Transaktion geöffnet wird, und als Diagnose `closecheck.stale-removed` mit ihrem SHA-256 erfasst (`OL_E_CLOSECHECK_REMOVE_FAILED`, wenn das Löschen fehlschlägt). Dies ist eine dokumentierte dauerhafte Änderung, kein Teilnehmer der Transaktion. Ist das Flag `false`, bleibt die Markierung bestehen, und OMSI zeigt seine Warnung an.
- Existiert sie vor der Sitzung **nicht**, wird `closecheck` als Sitzungslöschung hinzugefügt. Da die Sitzung mit `TerminateProcess` endet (die Herunterfahrroutine von OMSI läuft nicht), ist die beim Start gesetzte Markierung von OMSI danach immer noch vorhanden; sie wird bei der Wiederherstellung als Sitzungsartefakt entfernt.

<a id="early-recovery-order"></a>
## Reihenfolge der frühen Recovery

Bei jedem `StartSessionAsync`, nachdem die Installations-Lease erworben wurde und bevor irgendetwas die aktive Installation liest:

1. Eine reine Recovery-Transaktion prüft, ob `journal.json` vorhanden ist. Ist das der Fall, läuft `RestorePendingAsync` sofort, sodass Overlays und die Startbildsprache für die neue Sitzung aus den **Original**dateien abgeleitet werden, nie aus Überresten einer vorherigen Sitzung.
2. Schlägt diese Recovery mit `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` fehl (ein Journal aus der Zeit vor dem Fingerprint), wird die Recovery **zurückgestellt**: Die neue Sitzung erstellt ihre Overlays, und die neue Transaktion versucht die Recovery erneut mit ihren eigenen geplanten Bytes als Eigentümerschaftsnachweis (eine ursprünglich nicht vorhandene Datei, deren Inhalt dem neuen Overlay entspricht, wird als Eigentum von OmsiLaunch akzeptiert). Jeder andere Recovery-Fehler lässt den Start fehlschlagen.
3. Erst dann werden die Plugin-Closure validiert, `Omsi.exe` gehasht, `closecheck` behandelt und die neue Transaktion vorbereitet und angewendet.

Der Host-Trace erfasst `PENDING_JOURNAL_RECOVERED` oder `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Absturz-Recovery und Lebendigkeit des Eigentümers

Die Recovery ersetzt nie Dateien unter einem laufenden OMSI. `RestorePendingAsync` verweigert mit `OL_E_INSTALLATION_BUSY`, solange der im Journal erfasste Eigentümer lebt:

| Journal-Inhalt | Lebendigkeitsprüfung |
| --- | --- |
| `ProcessId` und `ProcessStartFileTimeUtc` erfasst | Der Prozess mit dieser PID muss laufen, seine Startzeit muss übereinstimmen (schließt PID-Wiederverwendung aus), und wenn `ExecutablePath` erfasst ist, muss sein Hauptmodul dieser Pfad sein (ein lebender, nicht zugehöriger Prozess kann die Transaktion nicht festhalten). |
| Keine PID, Zustand zwischen `HandoffCreated` (einschließlich) und `ProcessExited` (ausschließlich) | Der Host ist zwischen `CreateProcess` und dem Schreiben des Journals abgestürzt. Jede `Omsi.exe`, deren Hauptmodul `<root>\Omsi.exe` ist, wird als Eigentümer behandelt. |
| Keine PID, andere Zustände | Nicht lebendig; die Recovery wird fortgesetzt. |

Die explizite Recovery wird als `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)` bereitgestellt und gibt `RecoveryStatus(Pending, Recovered, Diagnostics)` zurück; sie erwirbt zuerst die Installations-Lease (`OL_E_INSTALLATION_BUSY`, wenn ein anderer Eigentümer sie hält). In der CLI meldet `/recovery-status` den Status, ohne wiederherzustellen, und `/recover` stellt wieder her; Exitcode 8 (`TransactionRecoveryFailed`) wird zurückgegeben, wenn eine Wiederherstellung angefordert wurde und das Journal danach weiterhin aussteht. Siehe [CLI](../reference/cli.md) und [öffentliche API](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Zurückgestellte Wiederherstellung am Sitzungsende

Kann der Supervisor nicht bestätigen, dass OMSI beendet wurde (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` oder ein als `OL_E_PROCESS_CLEANUP_FAILED` gemeldeter Bereinigungsfehler), schlägt die Sitzung mit `OL_E_RESTORE_DEFERRED` fehl, und das Journal wird absichtlich beibehalten: Installationsdateien zu ersetzen, während OMSI sie möglicherweise noch liest, ist unsicher. Der nächste Start (oder `/recover`) stellt wieder her, sobald der Prozess nicht mehr existiert. Eine Wiederherstellung, die aus einem anderen Grund fehlschlägt, beendet die Sitzung mit `OL_E_RESTORE_FAILED`; das Journal bleibt bestehen, bis jedes eigene Original wiederhergestellt und verifiziert ist.

<a id="the-installation-lease"></a>
## Die Installations-Lease

Die Lease ist ein benanntes Semaphor `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` mit dem Zähler 1. Das Stammverzeichnis wird von `InstallationLease.NormalizeRoot` normalisiert (vollständiger Pfad, abschließende Trennzeichen entfernt, außer bei einem Laufwerksstammverzeichnis), sodass `C:\OMSI`, `C:\OMSI\` und `c:\omsi\sub\..` eine Lease teilen; der Name der lokalen Steuerungs-Pipe verwendet dieselbe Normalisierung. Sie wird von `StartSessionAsync` (Zustand `AcquiringInstallationLock`) und von `RecoverPendingAsync` erworben und freigegeben, wenn der Lebenszyklus-Task der Sitzung abgeschlossen ist oder der Recovery-Aufruf zurückkehrt. `OL_E_INSTALLATION_BUSY` wird sofort ausgelöst, wenn sie nicht erworben werden kann (kein Warten).

Akzeptierte Einschränkungen (dokumentiert, keine Änderung geplant):

- Geltungsbereich `Local\`: ein Eigentümer pro Installation **pro Anmeldesitzung**. Zwei interaktive Benutzer auf demselben Rechner werden nicht gegenseitig ausgeschlossen.
- Ein Semaphor wird durch einen Absturz nicht freigegeben, solange ein anderer Prozess noch ein Handle darauf hält; anders als ein verwaister Mutex hat es keinen Eigentümer. Ein veralteter Halter lässt die Installation im Zustand `OL_E_INSTALLATION_BUSY`, bis dieses Handle geschlossen wird.
- Jeder Prozess desselben Windows-Benutzers kann den Namen zuerst erstellen und halten.

<a id="omsilaunch-directory"></a>
## `.omsilaunch`-Verzeichnis

| Eintrag | Lebensdauer | Eigentümer |
| --- | --- | --- |
| `journal.json` | Temporär; existiert nur, solange eine Transaktion aussteht | Transaktion |
| `backup\<sessionId>\*.bin` | Temporär; nach dem Journal entfernt | Transaktion |
| `diagnostics\<sessionId>-host.log` | Permanent; die Aufbewahrung behält die 50 neuesten Sitzungen (ältere Dateien mit Sitzungspräfix werden gelöscht, wenn eine neue Sitzung startet) | Host-Trace |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanent (gleiche Aufbewahrung) | CLI |
| `diagnostics\tray-host.log` | Permanent | Windows-Tray-Host |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Permanente Produkt-Assets; einmal aus dem Paket kopiert, nie überschrieben oder entfernt | Visuelle Sitzungs-Assets |
| `session-profiles\<id>\` | Permanent; vom Benutzer oder Inhaltsautor installiert | Benutzer |
| `profiles\` | Vom aktuellen Code weder angelegt noch gelesen; reserviert | keiner |
| `docs\`, `examples\` | Permanent; mit dem Release-Paket ausgeliefert | Paket |

Keine Daten verlassen den Rechner; Diagnosen sind ausschließlich lokale Dateien. Siehe auch [`.omsilaunch`-Verzeichnis](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Runtime-Änderungen werden nicht im Journal erfasst

Runtime-Steuerungsoperationen (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, D3D-Texturen) ändern nur den In-Memory-Zustand von OMSI. Sie werden nicht im Journal erfasst und nicht wiederhergestellt; sie verschwinden mit dem Prozess. Siehe [Runtime-Steuerung](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Fehlerfälle und Fehlercodes

| Code | Bedeutung | Journal danach |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease wird von einem anderen Eigentümer gehalten, oder der im Journal erfasste OMSI-Prozess lebt noch | beibehalten |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Wiederherstellung für eine Transaktion mit Snapshots angefordert, aber kein Journal auf dem Datenträger | n. z. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | Der Hash eines Backups weicht vom Snapshot-Fingerprint ab; es wurde nichts geschrieben | beibehalten |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Ein ursprünglich nicht vorhandener Overlay-Pfad enthält jetzt Inhalt, den die Sitzung nicht geschrieben hat | beibehalten |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal aus der Zeit vor dem Fingerprint mit einem ursprünglich nicht vorhandenen Pfad, der jetzt existiert; nur eine neue Sitzung mit identischen geplanten Bytes kann es abschließen | beibehalten (zurückgestellt) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `journal.json` konnte nach einer verifizierten Wiederherstellung nicht gelöscht werden | beibehalten (Wiederholung ist idempotent) |
| `OL_E_RESTORE_DEFERRED` | Beenden von OMSI nicht bestätigt; Wiederherstellung auf den nächsten Start verschoben | beibehalten |
| `OL_E_RESTORE_FAILED` | Jeder andere Wiederherstellungsfehler (Abweichung bei Vorhandensein oder Hash nach der Wiederherstellung, E/A-Fehler) | beibehalten |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Veraltetes `closecheck` konnte vor der Transaktion nicht gelöscht werden | noch keines |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Warnung: Eine fremde Datei auf einem Pfad einer Sitzungslöschung wurde beibehalten | abgeschlossen |
| `OL_E_PLAN_NOT_RUNNABLE` | Die erneute Planung beim Start ergab, dass die Spezifikation nicht mehr ausführbar ist (z. B. eine geänderte `Omsi.exe`); es wird keine Transaktion geöffnet | keines |

Die CLI ordnet `OL_E_RECOVERY_*` und `OL_E_RESTORE_FAILED` dem Exitcode 8 und `OL_E_INSTALLATION_BUSY` dem Exitcode 7 zu; siehe [Exitcodes](../reference/exit-codes.md).

<a id="evidence"></a>
## Nachweise

Offline-Tests in `tools/OmsiLaunch.TestHost` decken die Transaktionspfade ab: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Runtime-Nachweis (Validierungsmatrix): RV-005 und RV-006 (Overlay angewendet und bytegenaue Wiederherstellung, Sitzungen `1e8e0548-...` und der Präsentations-Batch), RV-008 Early-Exit bestanden (Sitzung `0dc40570-...`).

Runtime-Nachweis (Runtime-Abschlussrunde, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- Entfernen von Sitzungsartefakten bei `.itx`-Zielen mit dem echten Downloader von OMSI, bei normalem Stopp und nach Unterbrechung des Eigentümers plus `/recover` (S-01, `I01`, `I02`);
- frühe Recovery vor dem Aufbau der Overlays (S-05, `S05`);
- Wiederherstellung von Metadaten und Schreibschutz plus Bereinigung der Backups (S-12, `S12a`, `S12b`);
- `/recover` unter der Lease, mit einem verwaisten OMSI und im Zeitfenster vor der PID (S-04, `S04`, `S04b`);
- Startfehler und eine fehlgeschlagene Wiederherstellung, gefolgt von `/recover` (RV-008 Rest, `SF01`, `SF02`, `F01`);
- Erhalt von CP1252 (S-07, `C01`).

Der zurückgestellte Recovery-Zweig für Journale aus der Zeit vor dem Fingerprint ist weiterhin nur offline validiert. Siehe [Runtime-Validierungsstatus](../status/runtime-validation-status.md).
