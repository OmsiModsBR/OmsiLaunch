# Referentie voor OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/omsilaunchw.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

`OmsiLaunchW.exe` is de host van de OmsiLaunch-controller voor het Windows-subsysteem (GUI). Het accepteert dezelfde opdrachtregel als `OmsiLaunch.exe` en voert dezelfde controllercode uit (`OmsiLaunch.Controller.dll`). Het enige verschil is de manier van rapporteren: er is geen consolevenster, fouten worden als berichtvensters getoond en een actieve sessie is alleen zichtbaar via het bijbehorende [systeemvakpictogram](windows-tray.md).

Gezaghebbende bronnen: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (de native shim), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) en `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## OmsiLaunch.exe en OmsiLaunchW.exe vergeleken

| Aspect | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsysteem | Console. Opent een consolevenster wanneer het vanuit Verkenner wordt gestart. | Windows (GUI). Geen consolevenster. |
| Native shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Omgeving | ongewijzigd | stelt `OMSILAUNCH_WINDOWS_HOST=1` in voor het controllerproces voordat .NET start |
| Argumenten | in tokens opgesplitst met `CommandLineToArgvW` en ongewijzigd doorgegeven aan de controller | hetzelfde, dus beide hosts accepteren precies dezelfde vlaggen en opdrachten ([CLI-referentie](cli.md)) |
| Tekstuitvoer | naar stdout geschreven | onderdrukt, tenzij `--json` is opgegeven (dan worden de JSON-envelopes naar stdout geschreven, dat een aanroeper kan omleiden) |
| Fouten | `OL_E_...: message` of een JSON-foutenvelope | dezelfde regels voor console-uitvoer, **plus** een berichtvenster voor elke fout (zie [Foutdialoogvensters](#failure-dialogs)) |
| `/silent` | start `OmsiLaunchW.exe` met de overige argumenten en retourneert `0` | genegeerd: de opdracht wordt al in de Windows-host uitgevoerd |
| Systeemvakpictogram | getoond voor een eigenaarsessie | getoond voor een eigenaarsessie |
| Stoppaden | systeemvak, `session stop`, afsluiten van OMSI, `/observe-seconds`, Ctrl+C, sluiten van de console | systeemvak, `session stop`, afsluiten van OMSI, `/observe-seconds` (er is geen console, dus Ctrl+C en het sluiten van de console zijn niet van toepassing) |
| Exitcodes | [`PublicExitCode`](exit-codes.md) `0`..`10`, shimcodes `100`..`106` | dezelfde codes |

<a id="how-it-starts"></a>
## Hoe het start

1. De shim bepaalt zijn eigen pad (`GetModuleFileNameW`) en verwacht `OmsiLaunch.Controller.dll` in dezelfde map.
2. De shim splitst de opdrachtregel op in tokens (`CommandLineToArgvW`) en stelt `OMSILAUNCH_WINDOWS_HOST=1` in.
3. De shim zoekt `hostfxr` via de meegeleverde `nethost.dll`, laadt het, initialiseert de controller met de argumenten (het controllerpad maakt geen deel uit van de argumentenlijst die de CLI-parser ziet) en voert hem uit.
4. De shim retourneert de exitcode van de controller ongewijzigd.

Als een stap vóór het uitvoeren van de controller mislukt, toont de shim een berichtvenster met de titel `OmsiLaunch` en de tekst `OmsiLaunch could not start the .NET host (code N).` en sluit af met die code:

| Code | Mislukte stap |
| --- | --- |
| `100` | het pad van het uitvoerbare bestand kon niet worden bepaald |
| `101` | de opdrachtregel kon niet in tokens worden opgesplitst |
| `102` | het zoeken naar de locatie van `hostfxr` is mislukt (meestal: de .NET 6 x64-runtime is niet geïnstalleerd) |
| `103` | het pad van `hostfxr` kon niet worden opgehaald |
| `104` | `hostfxr` kon niet worden geladen |
| `105` | vereiste exports van `hostfxr` ontbreken |
| `106` | de beheerde host kon niet worden geïnitialiseerd (bijvoorbeeld omdat `OmsiLaunch.Controller.dll` of de runtimeconfiguratie ervan ontbreekt, of omdat de Windows Desktop-runtime afwezig is) |

`OmsiLaunch.exe` gebruikt dezelfde tabel, maar drukt niets af. Deze dialoogvensters zijn niet tijdens runtime opgewekt (zie [status van de runtimevalidatie](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Starten

Rechtstreeks starten, vanuit een snelkoppeling, een script of een ander programma:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Via `OmsiLaunch.exe` met `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Plaats de uitvoerbare bestanden in de OMSI 2-installatie (de pakketindeling, zie [packaging](packaging.md)); zonder installatieargument is de installatie de map die het uitvoerbare bestand bevat. Een expliciete installatie wordt als eerste argument doorgegeven, net als bij `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Omdat het een GUI-programma is, wachten `cmd.exe` en Verkenner er niet op. Om vanuit een script te wachten en de exitcode te lezen, gebruik je `start /wait OmsiLaunchW.exe ...` in `cmd.exe` of `Start-Process -Wait -PassThru` in PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delegatie met /silent

`OmsiLaunch.exe ... /silent` (of `--silent`) doet het volgende wanneer het niet al onder `OmsiLaunchW.exe` wordt uitgevoerd (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Het zoekt `OmsiLaunchW.exe` in de map van `OmsiLaunch.exe`. Als dat ontbreekt: `OL_E_WINDOWS_HOST_MISSING`, exit `7`.
2. Het start `OmsiLaunchW.exe` via `ShellExecute` (`UseShellExecute = true`) met de huidige map en alle argumenten behalve `/silent`/`--silent`, in de oorspronkelijke volgorde. `ShellExecute` geeft de handles van de aanroeper niet door aan het nieuwe proces, zodat een aanroeper die de uitvoer van `OmsiLaunch.exe /silent` opvangt, niet gedurende de hele levensduur van de sessie wordt geblokkeerd (runtimeafsluiting BUG-03). Als er geen proces wordt geretourneerd: `OL_E_WINDOWS_HOST_START_FAILED`, exit `7`.
3. Het schrijft de envelope `silent` en sluit onmiddellijk af met `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

Exit `0` betekent alleen dat `OmsiLaunchW.exe` is gestart. De uitkomst van de sessie (planningsfouten, een eigenaar die al actief is, een mislukte start) wordt door `OmsiLaunchW.exe` gemeld met eigen dialoogvensters en diagnoses, en de twee processen zijn daarna onafhankelijk: `OmsiLaunch.exe` is beëindigd en `OmsiLaunchW.exe` is de sessie-eigenaar. Gebruik `OmsiLaunch.exe session status` om de sessie te bekijken.

`/silent` wordt vóór elke andere opdracht toegepast, dus `OmsiLaunch.exe /silent session status` voert `session status` ook binnen `OmsiLaunchW.exe` uit, waar de uitvoer ervan wordt onderdrukt. Gebruik `/silent` alleen voor starts.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## Wat elke opdracht doet onder OmsiLaunchW.exe

| Opdrachtregel | Resultaat |
| --- | --- |
| geen argumenten | voert `detect` stil uit en sluit af met `0` (er wordt niets getoond) |
| een start (`/new`, `/saved:...`, `/spec:...`, een sessieprofiel) met een uitvoerbaar plan en zonder eigenaar | wordt de sessie-eigenaar: systeemvakpictogram tijdens het starten en uitvoeren; sluit af wanneer de sessie eindigt (`0` voltooid, `1` mislukt) |
| een start waarvan het plan niet uitvoerbaar is | dialoogvenster met de laatste diagnose met `OL_E_` van het plan (terugvaltekst `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), exit `1` (documentatie-audit BUG-06; vóór de correctie sloot het stil af) |
| een start terwijl er al een eigenaar actief is voor de installatie | dialoogvenster `OL_E_SESSION_ALREADY_ACTIVE`, exit `7`; de actieve sessie wordt niet beïnvloed |
| een start die `Running` niet bereikt | dialoogvenster `The OMSI session did not reach gameplay.` met de laatste diagnose met `OL_E_`, exit `1`. OMSI wordt beëindigd en de bestanden worden door de sessiesupervisor hersteld terwijl het dialoogvenster open is; het proces sluit af nadat het dialoogvenster is gesloten en `CloseAsync` is voltooid |
| een ongeldig argument, onbekende vlag of ongeldig sessieprofiel | dialoogvenster met `OL_E_INVALID_ARGUMENT` of de code `OL_E_SESSION_PROFILE_*`, exit `2` |
| een clientopdracht (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) zonder eigenaar | dialoogvenster `OL_E_NO_ACTIVE_SESSION`, exit `4` |
| een clientopdracht die de eigenaar weigert | dialoogvenster met de weigeringscode (bijvoorbeeld `OL_E_CONTROL_SESSION_MISMATCH` of een runtimefoutcode), exit `7` (`2` voor een onbekende operatie of een ontbrekend argument) |
| een geslaagde client- of discovery-opdracht (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | geen zichtbare uitvoer, tenzij `--json` is opgegeven en stdout wordt omgeleid; exitcode zoals bij `OmsiLaunch.exe` |
| `/plan` of `/validate` | geen dialoogvenster, zelfs niet voor een plan dat niet uitvoerbaar is; exit `0` of `1` |
| elke andere controllerfout | dialoogvenster met de geclassificeerde code `OL_E_`; exitcode volgens [exitcodes](exit-codes.md) |

<a id="failure-dialogs"></a>
## Foutdialoogvensters

Elke fout die `OmsiLaunch.exe` zou afdrukken, wordt ook als modaal berichtvenster getoond (`WindowsHost.ShowFailure`), zelfs wanneer `--json` is opgegeven:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` is het foutbericht, of bij een sessiefout het bericht van de laatste diagnose met `OL_E_`. Bekende beperking: bij een fout die door de plugin wordt gemeld, is het bericht de onbewerkte foutpayload van de plugin (bijvoorbeeld `{"name":"world.failed",...}`); de regel `Code:` is correct (zie [bekende beperkingen](known-limitations.md)). Het dialoogvenster is modaal en het proces sluit af nadat het is gesloten. Runtimebewijs: argumentfout, geen actieve sessie en een fout vóór de gameplay (runtimeafsluiting `T04`).

<a id="session-tray-and-exit"></a>
## Sessie, systeemvak en afsluiten

Een sessie waarvan `OmsiLaunchW.exe` de eigenaar is, gedraagt zich precies zoals een sessie waarvan `OmsiLaunch.exe` de eigenaar is (zie [levenscyclus van een sessie](../concepts/session-lifecycle.md)):

- Het systeemvakpictogram verschijnt zodra de sessie is gestart, voordat OMSI de gameplay bereikt, tenzij `Presentation.SuppressTrayIcon` is ingesteld in een bestand voor `/spec`. Met `SuppressTrayIcon` is er helemaal geen zichtbaar oppervlak; stop de sessie met `OmsiLaunch.exe session stop` of door OMSI te sluiten.
- De sessie eindigt wanneer OMSI afsluit, wanneer `End session` (sessie beëindigen) in het systeemvak wordt bevestigd, wanneer een client `session stop` verstuurt of wanneer `/observe-seconds` is verstreken. OmsiLaunch beëindigt dan OMSI als het nog draait, herstelt elk bestand dat het heeft gewijzigd, geeft de installatielease vrij, verwijdert het systeemvakpictogram en sluit af.
- Als `OmsiLaunchW.exe` zelf geforceerd wordt beëindigd (kill), herstelt de volgende OmsiLaunch-start voor die installatie de openstaande transactie (zie [transacties en recovery](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Snel aan de slag

1. Installeer het pakket in de OMSI 2-map ([installatie](../getting-started/installation.md)).
2. Maak een snelkoppeling naar `OmsiLaunchW.exe` met de argumenten van de sessie, bijvoorbeeld `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Start deze. OMSI start zonder consolevenster; het OmsiLaunch-pictogram verschijnt in het systeemvak (meldingsgebied).
4. Klik met de rechtermuisknop op het pictogram → `Status` om de sessie te bekijken, of `End session` → `End session` om de sessie te beëindigen.
5. Als er iets misgaat, toont het dialoogvenster de foutcode; details staan in `<OMSI_PATH>\.omsilaunch\diagnostics`.
