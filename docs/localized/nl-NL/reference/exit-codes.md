# Exitcodes

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/exit-codes.md) voor OmsiLaunch 0.1.0-beta3. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina vermeldt elke procesexitcode die `OmsiLaunch.exe` en `OmsiLaunchW.exe` kunnen retourneren: het openbare beheerde contract `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), de codes `100`..`106` van de native shim (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` en `OmsiLaunch.WindowsHost.cpp`) en de classificatieregels die `CliProgram.Classify` toepast op elke ontsnapte exception (`tools\OmsiLaunch.Cli\Program.cs`). Aanroepers moeten de betekenis afleiden uit de code en uit de gestructureerde fout-envelope, nooit uit de berichttekst. Foutcodes staan in de catalogus [fouten](errors.md); de opdrachten die elke code opleveren staan in de [CLI-referentie](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Openbare exitcodes (`PublicExitCode`)

| Code | Enumnaam | Betekenis | Wanneer |
|---|---|---|---|
| 0 | `Success` | De opdracht is voltooid. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` met een uitvoerbaar plan; een sessie die in `Completed` is geëindigd; `/silent` zodra `OmsiLaunchW.exe` is gestart; een doorgestuurde clientopdracht die de eigenaar met `Ok=true` heeft beantwoord; `/recover` wanneer er niets openstond of het herstel is voltooid. |
| 1 | `SessionFailed` | Een plan was niet uitvoerbaar, of een eigen sessie is in `Failed` geëindigd. | `/plan` dat `NOT RUNNABLE` meldt; een start waarvan het plan niet uitvoerbaar is (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` toont daarnaast de laatste diagnose met `OL_E_` in een berichtvenster); `OL_E_PLAN_NOT_RUNNABLE` gegenereerd door `StartSessionAsync` (opnieuw plannen bij de start); de sessie heeft `Running` niet binnen de opstart-timeout bereikt; de sessie is beëindigd in `Failed`. |
| 2 | `InvalidArguments` | De opdrachtregel, spec, het profiel of de runtime-argumenten zijn vóór of tijdens de dispatch geweigerd. | Onbekende vlag of route, ontbrekende waarde, waarde buiten bereik; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` en `OL_E_RUNTIME_ARGUMENT_REQUIRED` (lokaal of door de eigenaar geretourneerd); gebruiksinformatie afgedrukt voor een opdracht die niet kan worden gedispatcht; elke `ArgumentException`, `FormatException`, `InvalidDataException` of `OverflowException`. |
| 3 | `UnsupportedProfile` | Het platform of de OMSI-build wordt niet ondersteund. | Een ontsnapte exception waarvan de code begint met `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Let op: dezelfde situaties die tijdens het plannen worden gevonden, maken het plan niet uitvoerbaar en retourneren in plaats daarvan `1`. |
| 4 | `NoActiveSession` | Een clientopdracht heeft geen eigenaar gevonden. | `session status`, `session stop`, `events read`, `events watch` of een doorgestuurde runtime-operatie wanneer het local control-eindpunt van deze installatie niet antwoordt (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Een timeout is als exception ontsnapt. | Elke `TimeoutException` (`OL_E_TIMEOUT` wanneer het bericht geen code bevat, anders de ingesloten code zoals `OL_E_RUNTIME_REQUEST_TIMEOUT`). Doorgestuurde client-timeouts worden door de eigenaar beantwoord als `Ok=false` en retourneren `7`, niet `5`. |
| 6 | `NotFound` | Een bestand of map is niet gevonden. | `FileNotFoundException` / `DirectoryNotFoundException` (standaard `OL_E_NOT_FOUND`), bijvoorbeeld `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` wanneer als exception gegenereerd, een ontbrekende installatiemap tijdens `/list`. |
| 7 | `OperationRejected` | De opdracht was geldig maar is geweigerd, of een doorgestuurde opdracht is bij de eigenaar mislukt. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; elk control-antwoord met `Ok=false` behalve de twee argumentcodes (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); elke andere ontsnapte exception met een code `OL_E_` die niet elders wordt geclassificeerd (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Een duurzame transactie kon niet worden hersteld. | `/recover` wanneer het journal openstond en blijft openstaan; elke ontsnapte exception waarvan de code begint met `OL_E_RECOVERY_` of gelijk is aan `OL_E_RESTORE_FAILED` (bijvoorbeeld `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` tijdens het eigen herstel van een sessie). |
| 10 | `InternalError` | Een onverwachte exception zonder code `OL_E_`. | Gemeld als `OL_E_INTERNAL` met categorie `internal`; het bericht is de tekst van de exception. |

Code `9` is niet toegewezen.

<a id="native-shim-exit-codes"></a>
## Exitcodes van de native shim

Worden door `OmsiLaunch.exe` / `OmsiLaunchW.exe` geretourneerd voordat de beheerde controller wordt uitgevoerd. Ze overlappen niet met `PublicExitCode`, zodat een aanroeper een fout bij het opstarten van de host kan onderscheiden van een resultaat van de controller. `OmsiLaunchW.exe` toont daarnaast `OmsiLaunch could not start the .NET host (code N).` in een berichtvenster.

| Code | Betekenis | Oorzaak |
|---|---|---|
| 100 | Het pad van het uitvoerbare bestand kon niet worden bepaald | `GetModuleFileNameW` is mislukt. |
| 101 | De opdrachtregel kon niet in tokens worden opgesplitst | `CommandLineToArgvW` heeft null geretourneerd. |
| 102 | Het zoeken naar de locatie van `hostfxr` is mislukt | De groottequery van `get_hostfxr_path` is mislukt: er is geen passende .NET-runtime geïnstalleerd (de x64 .NET 6-runtime is vereist). |
| 103 | Het pad van `hostfxr` kon niet worden opgehaald | De tweede aanroep van `get_hostfxr_path` is mislukt. |
| 104 | De bibliotheek `hostfxr` kon niet worden geladen | `LoadLibraryW` op de gevonden `hostfxr.dll` is mislukt. |
| 105 | Vereiste exports van `hostfxr` ontbreken | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` of `hostfxr_close` niet gevonden. |
| 106 | De beheerde host kon niet worden geïnitialiseerd | `hostfxr_initialize_for_dotnet_command_line` is mislukt voor `OmsiLaunch.Controller.dll` (ontbrekende `OmsiLaunch.Controller.runtimeconfig.json`, ontbrekende `Microsoft.WindowsDesktop.App` 6.0 x64 of een beschadigd pakket). |

<a id="classification-rules-cliprogramclassify"></a>
## Classificatieregels (`CliProgram.Classify`)

Elke exception die uit `CliProgram.RunAsync` ontsnapt, wordt door `CliProgram.ReportFailure`, dat `Classify` aanroept, omgezet in een fout-envelope (`CliInput.WriteError`) en een exitcode. Parseerfouten worden vóór de dispatch op dezelfde manier afgehandeld (exit `2`). De regels worden in deze volgorde toegepast:

1. Het eerste token `OL_E_` in het bericht van de exception wordt geëxtraheerd (`ExtractCode`): de code is de langst mogelijke reeks ASCII-letters, cijfers en `_` die begint bij `OL_E_`. Codes worden letterlijk doorgegeven in `error.code`.
2. `SessionProfileException` → de eigen `Code`, categorie `invalid_argument`, exit `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → geëxtraheerde code of `OL_E_INVALID_ARGUMENT`, categorie `invalid_argument`, exit `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → geëxtraheerde code of `OL_E_NOT_FOUND`, categorie `not_found`, exit `6`.
5. `TimeoutException` → geëxtraheerde code of `OL_E_TIMEOUT`, categorie `runtime`, exit `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, categorie `session`, exit `7`.
7. Anders, wanneer een code is geëxtraheerd:
   - begint met `OL_E_RECOVERY_` of is gelijk aan `OL_E_RESTORE_FAILED` → categorie `transaction`, exit `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → categorie `session`, exit `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → categorie `session`, exit `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → categorie `invalid_argument`, exit `2`;
   - begint met `OL_E_UNSUPPORTED_` → categorie `unsupported_profile`, exit `3`;
   - elke andere code → categorie `runtime` voor `InvalidOperationException` en `IOException`, anders `internal`; exit `7`.
8. Helemaal geen code → `OL_E_INTERNAL`, categorie `internal`, exit `10`.

Doorgestuurde clientantwoorden gaan niet via `Classify`: `CliProgram.ReportForwarded` retourneert `4` als er geen eindpunt is, `2` voor `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` voor elk ander antwoord met `Ok=false` en `0` voor `Ok=true`.

<a id="scripting-guidance"></a>
## Richtlijnen voor scripts

- Beschouw `0` als succes en al het andere als fout; vertak op de numerieke code en daarna op `error.code` uit de envelope van `--json`.
- Een sessiestart keert pas terug nadat de sessie is geëindigd en de bestanden ervan zijn hersteld; `1` betekent dat de transactie is uitgevoerd maar OMSI is mislukt of het plan is geweigerd, niet dat bestanden gewijzigd zijn achtergebleven (een resterend journal wordt gemeld door `/recovery-status`).
- `100`..`106` betekenen dat het pakket of de .NET-runtime defect is; zie [installatie](../getting-started/installation.md) en [packaging](packaging.md).
