# Transacties en recovery

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../concepts/transactions-and-recovery.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Elke OmsiLaunch-sessie die een OMSI-bestand wijzigt, doet dat binnen een duurzame transactie met journal: van de oorspronkelijke bytes wordt een back-up gemaakt voordat ze worden vervangen, het journal legt vast hoe ver de sessie is gekomen, en bij het herstellen wordt elke back-up geverifieerd voordat deze wordt teruggeschreven. Deze pagina beschrijft die transactie zoals geïmplementeerd door `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) en aangestuurd door `OmsiLaunchService.StartAsync`, `SuperviseAsync` en `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), samen met de bestandsinvoer die door `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`) wordt berekend. De pagina is bedoeld voor gebruikers die willen weten wat een sessie wijzigt en wat recovery doet, en voor integrators die de exacte garanties nodig hebben.

<a id="what-a-session-changes"></a>
## Wat een sessie wijzigt

Alleen **tijdelijke overlays** maken deel uit van de transactie. Ze worden berekend voordat de transactie wordt geopend en hersteld wanneer deze wordt gesloten.

| Sessie-invoer | Bestand(en) | Soort |
| --- | --- | --- |
| `/set:<key>=<value>`, profiel `settings`, `LaunchSpec.Environment.*` | `options.cfg` (semantische tokenpatches; CP1252-bytes behouden, UTF-8/UTF-16 met BOM gerespecteerd) | overlay |
| Beheerd opstartscherm (`SplashMode.Managed`, de standaard) | `GUI\NewSplashscreen_ENG.bmp` en `GUI\NewSplashscreen_<language>.bmp` | overlay (het gelokaliseerde bestand wordt door de transactie aangemaakt als de installatie er geen heeft) |
| Internettextures in modus `Override` | `Texture\standard.itx` | overlay |
| Internettextures in modus `Override` | elk doel dat in de `.itx` wordt vermeld, plus `Texture\standard.ipr` | sessieverwijdering |
| Altijd | `closecheck` (als het vóór de sessie ontbrak) | sessieverwijdering |

Permanente productbestanden zijn **geen** deelnemers aan de transactie: de plugin-closure onder `plugins\OmsiLaunch.*` (alleen gevalideerd, zie [permanente plugin](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (één keer gekopieerd, nooit verwijderd), diagnostiek onder `.omsilaunch\diagnostics`, sessieprofielpakketten en de releasedocumentatie en voorbeelden. Plugins van derden en alle andere OMSI-bestanden worden nooit opgesomd, gekopieerd, verwijderd of hersteld.

OMSI zelf blijft tijdens een sessie zijn eigen toestand schrijven, precies zoals bij een normale OMSI-start: `options.cfg` (bijvoorbeeld `[last_map]` wanneer de sessie een andere kaart laadt, herschreven bij het betreden van de gameplay), `Texture\standard.ipr`, caches voor dienstregelingen en lightmaps (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, het chauffeursprofiel onder `Drivers\` en zijn logs. Een schrijfactie naar een pad dat de sessie bezit (zie hierboven) wordt door het herstel ongedaan gemaakt; elke andere schrijfactie van OMSI blijft na de sessie bestaan, net zoals na het rechtstreeks uitvoeren van OMSI. Runtimebewijs uit de runtime-closure: een sessie met een opgeslagen situatie op een andere kaart liet `[last_map]` gewijzigd achter, omdat die sessie geen overlay op `options.cfg` had (`CAM01`), terwijl `/set`-sessies `options.cfg` exact herstelden (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Transactietoestanden

`TransactionState` wordt na elke overgang in het journal bewaard. De waarden worden door `System.Text.Json` als gehele getallen geserialiseerd.

| Waarde | Toestand | Geschreven wanneer |
| --- | --- | --- |
| 0 | `Prepared` | Er zijn snapshots gemaakt van elk overlay- en verwijderingspad en hun back-ups zijn naar schijf weggeschreven. Er is nog niets in de installatie gewijzigd. Dit is de herstelverplichting: vanaf hier laat een crash een herstelbaar journal achter. |
| 1 | `Applied` | Elke overlay is atomair geschreven en elke verwijdering is uitgevoerd. |
| 2 | `RuntimeDeployed` | De integriteit van de permanente plugin is voor deze start gevalideerd (er wordt niets geïmplementeerd; de naam is historisch). |
| 3 | `HandoffCreated` | De opstart-handoff, het telemetrieslot en de runtime-mailbox bestaan als benoemd gedeeld geheugen. |
| 4 | `ProcessStarted` | `Omsi.exe` is aangemaakt. Het journal bevat nu ook `ProcessId`, `ProcessStartFileTimeUtc` (aanmaaktijd, UTC-ticks) en `ExecutablePath`. |
| 5 | `ProcessExited` | De supervisor heeft bevestigd dat het proces is afgesloten (natuurlijk afsluiten of `TerminateProcess`). |
| 6 | `Restoring` | Het herstel is begonnen. |
| 7 | `Restored` | Elk eigen bestand is hersteld en geverifieerd. Direct daarna wordt het journal verwijderd en `backup\<session>` verwijderd. |
| 8 | `Completed` | In de enum gedeclareerd, maar nooit bewaard; een voltooide transactie heeft geen journal. |

De levenscyclus van een normale sessie is daarom: snapshot -> `Prepared` -> overlays geschreven / verwijderingen uitgevoerd -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal verwijderd -> `backup\<session>` verwijderd. De openbare `SessionState`-waarden `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` en `Completed` volgen dezelfde voortgang van buitenaf (zie [sessielevenscyclus](session-lifecycle.md)).

Een sessie zonder wijzigingen aan eigen bestanden schrijft toch een journal voor haar levenscyclus; het herstellen daarvan is een geverifieerde no-op.

<a id="journal-file"></a>
## Journalbestand

Pad: `<root>\.omsilaunch\journal.json`. Er is maximaal één journal per installatie; de aanwezigheid ervan betekent "er staat een transactie open".

Velden van `TransactionJournal`:

| Veld | Type | Betekenis |
| --- | --- | --- |
| `SessionId` | GUID | Sessie die eigenaar is van het journal; ook de naam van de back-upmap (`N`-notatie). |
| `State` | geheel getal | `TransactionState` hierboven. |
| `Files` | array van `JournalFile` | Eén item per eigen pad. |
| `ProcessId` | geheel getal of null | PID van OMSI, vanaf `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long of null | Aanmaaktijd van OMSI (UTC-ticks), vanaf `ProcessStarted`. |
| `ExecutablePath` | string of null | Volledig pad van de gestarte `Omsi.exe`, vanaf `ProcessStarted`. |

Velden van `JournalFile`:

| Veld | Type | Betekenis |
| --- | --- | --- |
| `RelativePath` | string | Pad relatief ten opzichte van de installatiemap (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Of het bestand vóór de sessie bestond. |
| `Sha256` | hex-string | SHA-256 van de oorspronkelijke bytes (van een lege bytearray als `Existed` false is). |
| `BackupPath` | string | Absoluut pad van de back-upkopie (alleen geschreven als `Existed`). |
| `AppliedSha256` | hex-string of null | SHA-256 van de overlaybytes die de sessie naar dit pad heeft geschreven; null voor sessieverwijderingen. Dit is de eigendomsvingerafdruk voor bestanden die oorspronkelijk ontbraken. |
| `LastWriteTimeUtcTicks` | long of null | Oorspronkelijk tijdstip van de laatste schrijfactie. |
| `CreationTimeUtcTicks` | long of null | Oorspronkelijk aanmaaktijdstip. |
| `Attributes` | geheel getal of null | Oorspronkelijke `FileAttributes` (inclusief `ReadOnly`). |
| `SessionDeletion` | bool | True voor paden die de sessie afwezig wilde houden (`.itx`-doelen, `Texture\standard.ipr`, `closecheck`). |

Journals die door eerdere builds zijn geschreven zonder `AppliedSha256` en de metadatavelden, zijn nog steeds leesbaar; zie [Oorspronkelijk ontbrekende bestanden](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Indeling van de back-ups

| Pad | Inhoud |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Eén map per sessie, aangemaakt met het `Prepared`-journal. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Exacte oorspronkelijke bytes van één bestaand eigen bestand. Bestanden die oorspronkelijk ontbraken, hebben geen back-up. |

Back-ups en het journal worden geschreven via een tijdelijk bestand (`<path>.omsilaunch.tmp`), met write-through plus een expliciete `Flush(true)`, gevolgd door een atomaire `File.Move` met overschrijven. Het tijdelijke bestand wordt altijd verwijderd, ook bij een fout. Hetzelfde schrijfpad wordt gebruikt voor overlays en voor herstelde originelen, zodat er na een voltooide bewerking geen `*.omsilaunch.tmp`-bestand overblijft.

Back-ups worden pas verwijderd nadat het journal dat ernaar verwees, is verwijderd. Een mislukte verwijdering van `backup\<session>` is cosmetisch en maakt een geverifieerd herstel nooit ongedaan.

<a id="restore"></a>
## Herstel

`RestoreAsync` wordt uitgevoerd na `ProcessExited` (of tijdens recovery). Voor elk pad in het journal:

| Oorspronkelijke toestand | Action |
| --- | --- |
| Bestond | Van de back-upbytes wordt de hash berekend en vergeleken met `Sha256`; bij een afwijking wordt afgebroken met `OL_E_RECOVERY_BACKUP_CORRUPT` voordat er iets wordt geschreven. Daarna worden de bytes atomair geschreven (bij een huidig bestand dat alleen-lezen is, wordt dat kenmerk eerst gewist) en worden het aanmaaktijdstip, het tijdstip van de laatste schrijfactie en de kenmerken hersteld (`RestoreMetadata`; fouten bij metadata worden genegeerd, zodat een machtigingsprobleem een byte-exact herstel niet kan blokkeren). |
| Ontbrak, nu aanwezig, `AppliedSha256` bekend | Van de huidige bytes wordt de hash berekend. Als deze gelijk is aan `AppliedSha256`, is het bestand de eigen overlay van de sessie en wordt het verwijderd. Anders breekt `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` het herstel af en blijft het journal behouden. |
| Ontbrak, nu aanwezig, sessieverwijdering, journal heeft `ProcessStarted` bereikt | Het bestand is een bijproduct van de sessie (OMSI draaide terwijl de installatielease werd vastgehouden en voor dit pad was gevraagd het afwezig te houden). Het wordt verwijderd en gemeld als diagnose `restore.session-artifact-removed` met de SHA-256 van de verwijderde inhoud. |
| Ontbrak, nu aanwezig, sessieverwijdering, proces nooit gestart | Het bestand kwam van buiten de sessie. Het blijft behouden, wordt gemeld als `OL_W_RESTORE_FOREIGN_FILE_RETAINED` met zijn SHA-256, en de transactie wordt toch voltooid. |
| Ontbrak, nu aanwezig, geen eigendomsbewijs (journal van vóór de vingerafdrukken) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; het journal blijft behouden. |
| Ontbrak, ontbreekt nog steeds | Niets te doen. |

Nadat alle bestanden zijn verwerkt, leest `VerifyRestoredSnapshots` elk pad opnieuw: bestaande originelen moeten de hash `Sha256` hebben, oorspronkelijk ontbrekende paden moeten ontbreken, tenzij ze expliciet zijn behouden. Pas dan wordt `Restored` bewaard, het journal verwijderd (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` als het blijft bestaan) en de back-upmap verwijderd. Een crash tussen `Restored` en het verwijderen van het journal leidt alleen tot een idempotente herhaling.

Herstelnotities (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) verschijnen als `LaunchDiagnostic`-items in `SessionStatus.Diagnostics` (met `sha256` in `Data`) en in `RecoveryStatus.Diagnostics`, zodat niets stilzwijgend wordt verwijderd of behouden.

<a id="originally-absent-files-and-ownership"></a>
### Oorspronkelijk ontbrekende bestanden en eigendom

Een overlay die is geschreven naar een pad dat niet bestond, wordt bij het herstel **alleen verwijderd als de inhoud nog overeenkomt met wat de sessie heeft toegepast** (`AppliedSha256`). Als iets anders het bestand tijdens de sessie heeft vervangen, mislukt het herstel met `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` en blijft het journal behouden voor inspectie.

Een pad voor sessieverwijdering (`.itx`-doelen, `Texture\standard.ipr`, `closecheck`) dat vooraf niet bestond maar achteraf wel, wordt beoordeeld op de vraag of OMSI onder deze transactie heeft gedraaid: als het journal `ProcessStarted` heeft bereikt, is het bestand een bijproduct van de sessie en wordt het verwijderd (`restore.session-artifact-removed`); als het proces nooit is gestart, blijft het bestand behouden en wordt het gemeld als `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, en wordt het journal toch voltooid.

### `closecheck`

`closecheck` is de eigen crashmarkering van OMSI (aanwezig als OMSI niet netjes is afgesloten). Er gelden twee regels:

- Als het bestand **vóór** de sessie bestaat en `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` `true` is (de standaard), wordt het permanent verwijderd voordat de transactie wordt geopend, en vastgelegd als diagnose `closecheck.stale-removed` met zijn SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` als het verwijderen mislukt). Dit is een gedocumenteerde permanente wijziging, geen deelnemer aan de transactie. Met de vlag op `false` blijft de markering staan en toont OMSI zijn waarschuwing.
- Als het bestand **niet** vóór de sessie bestaat, wordt `closecheck` toegevoegd als sessieverwijdering. Omdat de sessie eindigt met `TerminateProcess` (de afsluitroutine van OMSI wordt niet uitgevoerd), is de markering die OMSI bij het starten schrijft daarna altijd nog aanwezig; ze wordt bij het herstel verwijderd als sessie-artefact.

<a id="early-recovery-order"></a>
## Volgorde van vroege recovery

Bij elke `StartSessionAsync`, nadat de installatielease is verkregen en voordat iets de live installatie leest:

1. Een transactie die alleen voor recovery dient, controleert of `journal.json` bestaat. Zo ja, dan wordt `RestorePendingAsync` onmiddellijk uitgevoerd, zodat overlays en de taal van het opstartscherm voor de nieuwe sessie worden afgeleid van de **oorspronkelijke** bestanden, nooit van restanten van een vorige sessie.
2. Als die recovery mislukt met `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (een journal van vóór de vingerafdrukken), wordt recovery **uitgesteld**: de nieuwe sessie bouwt haar overlays op en de nieuwe transactie probeert de recovery opnieuw met haar eigen geplande bytes als eigendomsbewijs (een oorspronkelijk ontbrekend bestand waarvan de inhoud gelijk is aan de nieuwe overlay, wordt geaccepteerd als eigendom van OmsiLaunch). Elke andere mislukte recovery laat de start mislukken.
3. Pas daarna worden de plugin-closure gevalideerd, de hash van `Omsi.exe` berekend, `closecheck` afgehandeld en de nieuwe transactie voorbereid en toegepast.

De host-trace legt `PENDING_JOURNAL_RECOVERED` of `PENDING_JOURNAL_RECOVERY_DEFERRED` vast.

<a id="crash-recovery-and-owner-liveness"></a>
## Crashrecovery en levendigheid van de eigenaar

Recovery vervangt nooit bestanden onder een draaiende OMSI. `RestorePendingAsync` weigert met `OL_E_INSTALLATION_BUSY` zolang de in het journal vastgelegde eigenaar leeft:

| Inhoud van het journal | Levendigheidstest |
| --- | --- |
| `ProcessId` en `ProcessStartFileTimeUtc` vastgelegd | Het proces met die PID moet draaien, zijn starttijd moet overeenkomen (wijst hergebruik van een PID af) en, als `ExecutablePath` is vastgelegd, moet zijn hoofdmodule dat pad zijn (een levend, niet-gerelateerd proces kan de transactie niet vasthouden). |
| Geen PID, toestand tussen `HandoffCreated` (inclusief) en `ProcessExited` (exclusief) | De host is gestopt tussen `CreateProcess` en het schrijven van het journal. Elke `Omsi.exe` waarvan de hoofdmodule `<root>\Omsi.exe` is, wordt als de eigenaar behandeld. |
| Geen PID, andere toestanden | Niet levend; recovery gaat door. |

Expliciete recovery is beschikbaar als `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, dat `RecoveryStatus(Pending, Recovered, Diagnostics)` retourneert; deze aanroep verkrijgt eerst de installatielease (`OL_E_INSTALLATION_BUSY` als een andere eigenaar die vasthoudt). In de CLI rapporteert `/recovery-status` zonder te herstellen en herstelt `/recover`; exitcode 8 (`TransactionRecoveryFailed`) wordt geretourneerd als om een herstel is gevraagd en het journal daarna nog steeds openstaat. Zie [CLI](../reference/cli.md) en [openbare API](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Uitgesteld herstel aan het einde van de sessie

Als de supervisor niet kan bevestigen dat OMSI is afgesloten (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` of een fout bij het opruimen die wordt gemeld als `OL_E_PROCESS_CLEANUP_FAILED`), mislukt de sessie met `OL_E_RESTORE_DEFERRED` en wordt het journal bewust behouden: installatiebestanden vervangen terwijl OMSI ze mogelijk nog leest, is onveilig. De volgende start (of `/recover`) herstelt zodra het proces weg is. Een herstel dat om een andere reden mislukt, beëindigt de sessie met `OL_E_RESTORE_FAILED`; het journal blijft bestaan totdat elk eigen origineel is hersteld en geverifieerd.

<a id="the-installation-lease"></a>
## De installatielease

De lease is een benoemde semafoor `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` met telling 1. De installatiemap wordt genormaliseerd door `InstallationLease.NormalizeRoot` (volledig pad, afsluitende scheidingstekens verwijderd behalve bij een stationshoofdmap), zodat `C:\OMSI`, `C:\OMSI\` en `c:\omsi\sub\..` één lease delen; de naam van de local-control-pipe gebruikt dezelfde normalisatie. De lease wordt verkregen door `StartSessionAsync` (toestand `AcquiringInstallationLock`) en door `RecoverPendingAsync`, en vrijgegeven wanneer de levenscyclustaak van de sessie is voltooid of de recovery-aanroep terugkeert. `OL_E_INSTALLATION_BUSY` wordt onmiddellijk gemeld als de lease niet kan worden verkregen (er wordt niet gewacht).

Geaccepteerde beperkingen (gedocumenteerd, geen wijziging gepland):

- Bereik `Local\`: één eigenaar per installatie **per aanmeldsessie**. Twee interactieve gebruikers op dezelfde computer sluiten elkaar niet wederzijds uit.
- Een semafoor wordt niet vrijgegeven door een crash zolang een ander proces nog een handle ernaar vasthoudt; anders dan een verlaten mutex heeft een semafoor geen eigenaar. Een verouderde houder laat de installatie op `OL_E_INSTALLATION_BUSY` staan totdat die handle wordt gesloten.
- Elk proces van dezelfde Windows-gebruiker kan de naam als eerste aanmaken en vasthouden.

<a id="omsilaunch-directory"></a>
## Map `.omsilaunch`

| Item | Levensduur | Eigenaar |
| --- | --- | --- |
| `journal.json` | Tijdelijk; bestaat alleen zolang er een transactie openstaat | Transactie |
| `backup\<sessionId>\*.bin` | Tijdelijk; verwijderd na het journal | Transactie |
| `diagnostics\<sessionId>-host.log` | Permanent; de bewaartermijn houdt de 50 nieuwste sessies aan (oudere bestanden met sessievoorvoegsel worden verwijderd wanneer een nieuwe sessie start) | Host-trace |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanent (zelfde bewaartermijn) | CLI |
| `diagnostics\tray-host.log` | Permanent | Host van het Windows-systeemvak |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Permanente productassets; één keer uit het pakket gekopieerd, nooit overschreven of verwijderd | Visuele assets van de sessie |
| `session-profiles\<id>\` | Permanent; geïnstalleerd door de gebruiker of de contentauteur | Gebruiker |
| `profiles\` | Wordt door de huidige code niet aangemaakt of gelezen; gereserveerd | geen |
| `docs\`, `examples\` | Permanent; meegeleverd met het releasepakket | Pakket |

Er verlaten geen gegevens de computer; diagnostiek bestaat alleen uit lokale bestanden. Zie ook [map `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Runtimewijzigingen worden niet in het journal vastgelegd

Runtimebesturingsoperaties (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, D3D-textures) wijzigen alleen de toestand van OMSI in het geheugen. Ze worden niet in het journal vastgelegd en niet hersteld; ze verdwijnen met het proces. Zie [runtimebesturing](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Foutsituaties en foutcodes

| Code | Betekenis | Journal daarna |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease vastgehouden door een andere eigenaar, of het in het journal vastgelegde OMSI-proces leeft nog | behouden |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Herstel aangevraagd voor een transactie met snapshots maar zonder journal op schijf | n.v.t. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | De hash van een back-up wijkt af van de vingerafdruk van de snapshot; er is niets geschreven | behouden |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Een oorspronkelijk ontbrekend overlaypad bevat nu inhoud die de sessie niet heeft geschreven | behouden |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal van vóór de vingerafdrukken met een oorspronkelijk ontbrekend pad dat nu bestaat; alleen een nieuwe sessie met identieke geplande bytes kan het afsluiten | behouden (uitgesteld) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `journal.json` kon na een geverifieerd herstel niet worden verwijderd | behouden (herhaling is idempotent) |
| `OL_E_RESTORE_DEFERRED` | Afsluiten van OMSI niet bevestigd; herstel uitgesteld tot de volgende start | behouden |
| `OL_E_RESTORE_FAILED` | Elke andere herstelfout (afwijkende aanwezigheid of hash na herstel, I/O-fout) | behouden |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Verouderde `closecheck` kon vóór de transactie niet worden verwijderd | nog geen |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Waarschuwing: een vreemd bestand op een pad voor sessieverwijdering is behouden | voltooid |
| `OL_E_PLAN_NOT_RUNNABLE` | Bij het opnieuw plannen tijdens de start bleek de specificatie niet langer uitvoerbaar (bijvoorbeeld een gewijzigde `Omsi.exe`); er wordt geen transactie geopend | geen |

De CLI wijst `OL_E_RECOVERY_*` en `OL_E_RESTORE_FAILED` toe aan exitcode 8 en `OL_E_INSTALLATION_BUSY` aan exitcode 7; zie [exitcodes](../reference/exit-codes.md).

<a id="evidence"></a>
## Bewijs

Offline tests in `tools/OmsiLaunch.TestHost` dekken de transactiepaden: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Runtimebewijs (validatiematrix): RV-005 en RV-006 (overlay toegepast en byte-exact herstel, sessies `1e8e0548-...` en de presentatiebatch), geslaagde early-exit-test van RV-008 (sessie `0dc40570-...`).

Runtimebewijs (runtime-closure-ronde, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- verwijdering als sessie-artefact van `.itx`-doelen met de echte downloader van OMSI, bij een normale stop en na onderbreking van de eigenaar plus `/recover` (S-01, `I01`, `I02`);
- vroege recovery vóór het opbouwen van de overlays (S-05, `S05`);
- herstel van metadata en alleen-lezen bestanden plus opruimen van back-ups (S-12, `S12a`, `S12b`);
- `/recover` onder de lease, met een verweesde OMSI en in het venster vóór de PID (S-04, `S04`, `S04b`);
- opstartfouten en een mislukt herstel gevolgd door `/recover` (rest van RV-008, `SF01`, `SF02`, `F01`);
- behoud van CP1252 (S-07, `C01`).

De tak voor uitgestelde recovery van journals van vóór de vingerafdrukken blijft alleen offline gevalideerd. Zie [status van de runtimevalidatie](../status/runtime-validation-status.md).
