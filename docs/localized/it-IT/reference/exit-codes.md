# Codici di uscita

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Traduzione della [pagina originale in inglese](../../../reference/exit-codes.md) di OmsiLaunch 0.1.0-beta3. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina elenca ogni codice di uscita di processo che `OmsiLaunch.exe` e `OmsiLaunchW.exe` possono restituire: il contratto gestito pubblico `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), i codici dello shim nativo `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` e `OmsiLaunch.WindowsHost.cpp`) e le regole di classificazione che `CliProgram.Classify` applica a qualsiasi eccezione non gestita (`tools\OmsiLaunch.Cli\Program.cs`). I chiamanti devono dedurre la semantica dal codice e dall'envelope di errore strutturato, mai dal testo del messaggio. I codici di errore sono catalogati in [errori](errors.md); i comandi che producono ciascun codice si trovano nel [riferimento CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Codici di uscita pubblici (`PublicExitCode`)

| Codice | Nome enum | Significato | Quando |
|---|---|---|---|
| 0 | `Success` | Il comando è stato completato. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` con un piano eseguibile; una sessione terminata in `Completed`; `/silent` una volta avviato `OmsiLaunchW.exe`; un comando client inoltrato a cui l'owner ha risposto con `Ok=true`; `/recover` quando non c'era nulla in sospeso o il ripristino è stato completato. |
| 1 | `SessionFailed` | Un piano non era eseguibile, oppure una sessione di proprietà è terminata in `Failed`. | `/plan` che riporta `NOT RUNNABLE`; un avvio il cui piano non è eseguibile (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` mostra inoltre l'ultima voce di diagnostica `OL_E_` in una finestra di messaggio); `OL_E_PLAN_NOT_RUNNABLE` sollevato da `StartSessionAsync` (ripianificazione all'avvio); la sessione non ha raggiunto `Running` entro il timeout di avvio; la sessione è terminata in `Failed`. |
| 2 | `InvalidArguments` | La riga di comando, la spec, il profilo o gli argomenti runtime sono stati rifiutati prima o durante l'inoltro (dispatch). | Flag o route sconosciuti, valore mancante, valore fuori intervallo; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` e `OL_E_RUNTIME_ARGUMENT_REQUIRED` (localmente o restituiti dall'owner); istruzioni d'uso stampate per un comando non inoltrabile; qualsiasi `ArgumentException`, `FormatException`, `InvalidDataException` o `OverflowException`. |
| 3 | `UnsupportedProfile` | La piattaforma o il build di OMSI non sono supportati. | Un'eccezione non gestita il cui codice inizia con `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Si noti che le stesse condizioni, se rilevate durante la pianificazione, rendono il piano non eseguibile e restituiscono invece `1`. |
| 4 | `NoActiveSession` | Un comando client non ha trovato alcun owner. | `session status`, `session stop`, `events read`, `events watch` o un'operazione runtime inoltrata quando l'endpoint di controllo locale di questa installazione non risponde (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Un timeout è sfuggito come eccezione. | Qualsiasi `TimeoutException` (`OL_E_TIMEOUT` quando il messaggio non contiene alcun codice, altrimenti il codice incorporato, come `OL_E_RUNTIME_REQUEST_TIMEOUT`). Ai timeout dei client inoltrati l'owner risponde con `Ok=false` e restituiscono `7`, non `5`. |
| 6 | `NotFound` | Un file o una directory non è stato trovato. | `FileNotFoundException` / `DirectoryNotFoundException` (predefinito `OL_E_NOT_FOUND`), per esempio `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` quando sollevato come eccezione, una directory di installazione mancante durante `/list`. |
| 7 | `OperationRejected` | Il comando era valido ma è stato rifiutato, oppure un comando inoltrato non è riuscito presso l'owner. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; ogni risposta di controllo `Ok=false` diversa dai due codici relativi agli argomenti (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); qualsiasi altra eccezione non gestita che riporta un codice `OL_E_` non classificato altrove (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Non è stato possibile ripristinare una transazione persistente. | `/recover` quando il journal era in sospeso e rimane in sospeso; qualsiasi eccezione non gestita il cui codice inizia con `OL_E_RECOVERY_` o è `OL_E_RESTORE_FAILED` (per esempio `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` durante il ripristino proprio di una sessione). |
| 10 | `InternalError` | Un'eccezione imprevista senza codice `OL_E_`. | Segnalata come `OL_E_INTERNAL` con categoria `internal`; il messaggio è il testo dell'eccezione. |

Il codice `9` non è assegnato.

<a id="native-shim-exit-codes"></a>
## Codici di uscita dello shim nativo

Restituiti da `OmsiLaunch.exe` / `OmsiLaunchW.exe` prima che venga eseguito il controller gestito. Sono disgiunti da `PublicExitCode`, così un chiamante può distinguere un errore di avvio dell'host da un risultato del controller. `OmsiLaunchW.exe` mostra inoltre `OmsiLaunch could not start the .NET host (code N).` in una finestra di messaggio.

| Codice | Significato | Causa |
|---|---|---|
| 100 | Impossibile risolvere il percorso dell'eseguibile | `GetModuleFileNameW` non è riuscita. |
| 101 | Impossibile suddividere in token la riga di comando | `CommandLineToArgvW` ha restituito null. |
| 102 | Rilevamento della posizione di `hostfxr` non riuscito | La richiesta di dimensione di `get_hostfxr_path` non è riuscita: non è installato alcun runtime .NET corrispondente (è richiesto il runtime .NET 6 x64). |
| 103 | Impossibile recuperare il percorso di `hostfxr` | La seconda chiamata a `get_hostfxr_path` non è riuscita. |
| 104 | Impossibile caricare la libreria `hostfxr` | `LoadLibraryW` sul file `hostfxr.dll` risolto non è riuscita. |
| 105 | Mancano export `hostfxr` necessari | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` o `hostfxr_close` non trovati. |
| 106 | Impossibile inizializzare l'host gestito | `hostfxr_initialize_for_dotnet_command_line` non è riuscita per `OmsiLaunch.Controller.dll` (`OmsiLaunch.Controller.runtimeconfig.json` mancante, `Microsoft.WindowsDesktop.App` 6.0 x64 mancante o pacchetto danneggiato). |

<a id="classification-rules-cliprogramclassify"></a>
## Regole di classificazione (`CliProgram.Classify`)

Ogni eccezione che sfugge a `CliProgram.RunAsync` viene trasformata in un envelope di errore (`CliInput.WriteError`) e in un codice di uscita da `CliProgram.ReportFailure`, che chiama `Classify`. Gli errori di analisi della riga di comando vengono gestiti allo stesso modo prima dell'inoltro (uscita `2`). Le regole si applicano in questo ordine:

1. Viene estratto il primo token `OL_E_` nel messaggio dell'eccezione (`ExtractCode`): il codice è la sequenza massima di lettere ASCII, cifre e `_` che inizia con `OL_E_`. I codici vengono riportati testualmente in `error.code`.
2. `SessionProfileException` → il suo `Code`, categoria `invalid_argument`, uscita `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → codice estratto oppure `OL_E_INVALID_ARGUMENT`, categoria `invalid_argument`, uscita `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → codice estratto oppure `OL_E_NOT_FOUND`, categoria `not_found`, uscita `6`.
5. `TimeoutException` → codice estratto oppure `OL_E_TIMEOUT`, categoria `runtime`, uscita `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, categoria `session`, uscita `7`.
7. Altrimenti, quando è stato estratto un codice:
   - inizia con `OL_E_RECOVERY_` o è uguale a `OL_E_RESTORE_FAILED` → categoria `transaction`, uscita `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → categoria `session`, uscita `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → categoria `session`, uscita `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → categoria `invalid_argument`, uscita `2`;
   - inizia con `OL_E_UNSUPPORTED_` → categoria `unsupported_profile`, uscita `3`;
   - qualsiasi altro codice → categoria `runtime` per `InvalidOperationException` e `IOException`, altrimenti `internal`; uscita `7`.
8. Nessun codice → `OL_E_INTERNAL`, categoria `internal`, uscita `10`.

Le risposte client inoltrate aggirano `Classify`: `CliProgram.ReportForwarded` restituisce `4` in assenza di endpoint, `2` per `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` per qualsiasi altra risposta `Ok=false` e `0` per `Ok=true`.

<a id="scripting-guidance"></a>
## Indicazioni per gli script

- Considerare `0` come successo e qualsiasi altro valore come errore; diramare in base al codice numerico, poi in base a `error.code` dell'envelope `--json`.
- L'avvio di una sessione ritorna solo dopo che la sessione è terminata e i suoi file sono stati ripristinati; `1` indica che la transazione è stata eseguita ma OMSI non è riuscito o il piano è stato rifiutato, non che dei file siano rimasti modificati (un journal residuo viene segnalato da `/recovery-status`).
- `100`..`106` indicano che il pacchetto o il runtime .NET è danneggiato; vedere [installazione](../getting-started/installation.md) e [pacchettizzazione](packaging.md).
