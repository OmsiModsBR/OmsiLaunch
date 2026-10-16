# Exitcodes

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Übersetzung der [englischen Originalseite](../../../reference/exit-codes.md) für OmsiLaunch 0.1.0-beta3. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite listet jeden Prozess-Exitcode auf, den `OmsiLaunch.exe` und `OmsiLaunchW.exe` zurückgeben können: den öffentlichen verwalteten Vertrag `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), die nativen Shim-Codes `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` und `OmsiLaunch.WindowsHost.cpp`) sowie die Klassifizierungsregeln, die `CliProgram.Classify` auf jede durchschlagende Ausnahme anwendet (`tools\OmsiLaunch.Cli\Program.cs`). Aufrufer müssen die Semantik aus dem Code und aus dem strukturierten Fehler-Envelope ableiten, niemals aus dem Meldungstext. Fehlercodes sind unter [Fehler](errors.md) katalogisiert; die Befehle, die jeden Code erzeugen, finden Sie in der [CLI-Referenz](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Öffentliche Exitcodes (`PublicExitCode`)

| Code | Enum-Name | Bedeutung | Wann |
|---|---|---|---|
| 0 | `Success` | Der Befehl wurde abgeschlossen. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` mit einem ausführbaren Plan; eine Sitzung, die in `Completed` endete; `/silent`, sobald `OmsiLaunchW.exe` gestartet wurde; ein weitergeleiteter Client-Befehl, den der Eigentümer mit `Ok=true` beantwortet hat; `/recover`, wenn nichts ausstand oder die Wiederherstellung abgeschlossen wurde. |
| 1 | `SessionFailed` | Ein Plan war nicht ausführbar, oder eine eigene Sitzung endete in `Failed`. | `/plan` meldet `NOT RUNNABLE`; ein Start, dessen Plan nicht ausführbar ist (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` zeigt zusätzlich die letzte `OL_E_`-Diagnose in einem Meldungsfenster an); `OL_E_PLAN_NOT_RUNNABLE`, ausgelöst von `StartSessionAsync` (Neuplanung beim Start); die Sitzung hat `Running` nicht innerhalb des Start-Timeouts erreicht; die Sitzung wurde in `Failed` beendet. |
| 2 | `InvalidArguments` | Die Befehlszeile, die Spezifikation, das Profil oder die Runtime-Argumente wurden vor oder während der Verteilung abgelehnt. | Unbekanntes Flag oder unbekannte Route, fehlender Wert, Wert außerhalb des Bereichs; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` und `OL_E_RUNTIME_ARGUMENT_REQUIRED` (lokal oder vom Eigentümer zurückgegeben); Nutzungshinweis für einen nicht verteilbaren Befehl ausgegeben; jede `ArgumentException`, `FormatException`, `InvalidDataException` oder `OverflowException`. |
| 3 | `UnsupportedProfile` | Die Plattform oder der OMSI-Build wird nicht unterstützt. | Eine durchschlagende Ausnahme, deren Code mit `OL_E_UNSUPPORTED_` beginnt (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Beachten Sie, dass dieselben Bedingungen, wenn sie bei der Planung festgestellt werden, den Plan nicht ausführbar machen und stattdessen `1` zurückgeben. |
| 4 | `NoActiveSession` | Ein Client-Befehl hat keinen Eigentümer gefunden. | `session status`, `session stop`, `events read`, `events watch` oder eine weitergeleitete Runtime-Operation, wenn der lokale Steuerungsendpunkt dieser Installation nicht antwortet (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Ein Timeout ist als Ausnahme durchgeschlagen. | Jede `TimeoutException` (`OL_E_TIMEOUT`, wenn die Meldung keinen Code enthält, andernfalls der eingebettete Code wie `OL_E_RUNTIME_REQUEST_TIMEOUT`). Timeouts weitergeleiteter Client-Befehle beantwortet der Eigentümer mit `Ok=false`; sie geben `7` zurück, nicht `5`. |
| 6 | `NotFound` | Eine Datei oder ein Verzeichnis wurde nicht gefunden. | `FileNotFoundException` / `DirectoryNotFoundException` (Standard `OL_E_NOT_FOUND`), zum Beispiel `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING`, wenn als Ausnahme ausgelöst, ein fehlendes Installationsverzeichnis bei `/list`. |
| 7 | `OperationRejected` | Der Befehl war gültig, wurde aber abgewiesen, oder ein weitergeleiteter Befehl ist beim Eigentümer fehlgeschlagen. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; jede Steuerungsantwort mit `Ok=false` außer den beiden Argumentcodes (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); jede andere durchschlagende Ausnahme mit einem `OL_E_`-Code, der nicht anderweitig klassifiziert ist (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Eine dauerhafte Transaktion konnte nicht wiederhergestellt werden. | `/recover`, wenn das Journal ausstand und weiterhin aussteht; jede durchschlagende Ausnahme, deren Code mit `OL_E_RECOVERY_` beginnt oder `OL_E_RESTORE_FAILED` ist (zum Beispiel `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` während der eigenen Wiederherstellung einer Sitzung). |
| 10 | `InternalError` | Eine unerwartete Ausnahme ohne `OL_E_`-Code. | Wird als `OL_E_INTERNAL` mit der Kategorie `internal` gemeldet; die Meldung ist der Ausnahmetext. |

Code `9` ist nicht vergeben.

<a id="native-shim-exit-codes"></a>
## Native Shim-Exitcodes

Werden von `OmsiLaunch.exe` / `OmsiLaunchW.exe` zurückgegeben, bevor der verwaltete Controller läuft. Sie überschneiden sich nicht mit `PublicExitCode`, sodass ein Aufrufer einen Fehler beim Hoststart von einem Ergebnis des Controllers unterscheiden kann. `OmsiLaunchW.exe` zeigt zusätzlich `OmsiLaunch could not start the .NET host (code N).` in einem Meldungsfenster an.

| Code | Bedeutung | Ursache |
|---|---|---|
| 100 | Pfad der Programmdatei konnte nicht aufgelöst werden | `GetModuleFileNameW` ist fehlgeschlagen. |
| 101 | Befehlszeile konnte nicht in Token zerlegt werden | `CommandLineToArgvW` hat null zurückgegeben. |
| 102 | Ermittlung des `hostfxr`-Speicherorts fehlgeschlagen | Größenabfrage von `get_hostfxr_path` fehlgeschlagen: Keine passende .NET-Runtime ist installiert (die x64-.NET-6-Runtime ist erforderlich). |
| 103 | `hostfxr`-Pfad konnte nicht abgerufen werden | Zweiter Aufruf von `get_hostfxr_path` fehlgeschlagen. |
| 104 | `hostfxr`-Bibliothek konnte nicht geladen werden | `LoadLibraryW` auf die aufgelöste `hostfxr.dll` fehlgeschlagen. |
| 105 | Erforderliche `hostfxr`-Exporte fehlen | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` oder `hostfxr_close` nicht gefunden. |
| 106 | Verwalteter Host konnte nicht initialisiert werden | `hostfxr_initialize_for_dotnet_command_line` ist für `OmsiLaunch.Controller.dll` fehlgeschlagen (fehlende `OmsiLaunch.Controller.runtimeconfig.json`, fehlendes `Microsoft.WindowsDesktop.App` 6.0 x64 oder ein beschädigtes Paket). |

<a id="classification-rules-cliprogramclassify"></a>
## Klassifizierungsregeln (`CliProgram.Classify`)

Jede Ausnahme, die aus `CliProgram.RunAsync` durchschlägt, wird von `CliProgram.ReportFailure`, das `Classify` aufruft, in einen Fehler-Envelope (`CliInput.WriteError`) und einen Exitcode umgewandelt. Parsing-Fehler werden vor der Verteilung auf dieselbe Weise behandelt (Exit `2`). Die Regeln gelten in dieser Reihenfolge:

1. Das erste `OL_E_`-Token in der Ausnahmemeldung wird extrahiert (`ExtractCode`): Der Code ist die längstmögliche Folge von ASCII-Buchstaben, Ziffern und `_`, beginnend bei `OL_E_`. Codes werden unverändert in `error.code` ausgegeben.
2. `SessionProfileException` → ihr eigener `Code`, Kategorie `invalid_argument`, Exit `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → extrahierter Code oder `OL_E_INVALID_ARGUMENT`, Kategorie `invalid_argument`, Exit `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → extrahierter Code oder `OL_E_NOT_FOUND`, Kategorie `not_found`, Exit `6`.
5. `TimeoutException` → extrahierter Code oder `OL_E_TIMEOUT`, Kategorie `runtime`, Exit `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, Kategorie `session`, Exit `7`.
7. Andernfalls, wenn ein Code extrahiert wurde:
   - beginnt mit `OL_E_RECOVERY_` oder ist gleich `OL_E_RESTORE_FAILED` → Kategorie `transaction`, Exit `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → Kategorie `session`, Exit `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → Kategorie `session`, Exit `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → Kategorie `invalid_argument`, Exit `2`;
   - beginnt mit `OL_E_UNSUPPORTED_` → Kategorie `unsupported_profile`, Exit `3`;
   - jeder andere Code → Kategorie `runtime` für `InvalidOperationException` und `IOException`, andernfalls `internal`; Exit `7`.
8. Überhaupt kein Code → `OL_E_INTERNAL`, Kategorie `internal`, Exit `10`.

Weitergeleitete Client-Antworten umgehen `Classify`: `CliProgram.ReportForwarded` gibt `4` zurück, wenn kein Endpunkt vorhanden ist, `2` für `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` für jede andere Antwort mit `Ok=false` und `0` für `Ok=true`.

<a id="scripting-guidance"></a>
## Hinweise für Skripte

- Behandeln Sie `0` als Erfolg und alles andere als Fehler; verzweigen Sie anhand des numerischen Codes und dann anhand von `error.code` aus dem `--json`-Envelope.
- Ein Sitzungsstart kehrt erst zurück, nachdem die Sitzung beendet und ihre Dateien wiederhergestellt wurden; `1` bedeutet, dass die Transaktion lief, aber OMSI fehlgeschlagen ist oder der Plan abgelehnt wurde – nicht, dass Dateien verändert zurückgelassen wurden (ein verbliebenes Journal wird von `/recovery-status` gemeldet).
- `100`..`106` bedeuten, dass das Paket oder die .NET-Runtime defekt ist; siehe [Installation](../getting-started/installation.md) und [Paketierung](packaging.md).
