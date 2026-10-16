# Documentazione di OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Traduzione della [pagina originale in inglese](../../README.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa è la documentazione normativa in inglese di OmsiLaunch `0.1.0-beta4`, la
baseline successiva al consolidamento (post-hardening). OmsiLaunch fornisce avvio programmabile, proprietà della
sessione e controllo a runtime per un'installazione di OMSI 2; ogni capability nativa
viene risolta e verificata per ciascun build (vedere [compatibilità](reference/compatibility.md)). Ogni pagina sotto `docs/` descrive ciò che fa il codice
attuale; quando una pagina e il codice non concordano, prevale il codice e la pagina contiene un bug.

Vocabolario di stabilità usato in tutta la documentazione: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. I flag che vengono analizzati ma non hanno alcun effetto sono contrassegnati come
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Nulla viene definito
validato a runtime a meno che lo
[stato della validazione a runtime](status/runtime-validation-status.md) non lo indichi.

<a id="who-reads-what"></a>
## Chi legge cosa

| Destinatari | Iniziare da | Poi |
| --- | --- | --- |
| Utenti (CLI, collegamenti, profili di sessione) | [Installazione](getting-started/installation.md), [Prima sessione](getting-started/first-session.md) | [Riferimento CLI](reference/cli.md), [Esempi CLI](reference/cli-examples.md), [Profili di sessione](reference/session-profiles.md), [Area di notifica di Windows](reference/windows-tray.md), [Codici di uscita](reference/exit-codes.md) |
| Integratori (`OmsiLaunch.Api`, IPC locale) | [Guida rapida all'API pubblica](getting-started/api-quick-start.md), [Riferimento dell'API pubblica](reference/public-api.md), [Riferimento LaunchSpec](reference/launchspec.md) | [Ciclo di vita della sessione](concepts/session-lifecycle.md), [Controllo a runtime](reference/runtime-control.md), [Capability](reference/capabilities.md), [Controllo locale / IPC](reference/local-control.md), [Riferimento degli errori](reference/errors.md) |
| Manutentori (rilascio, validazione, confini) | [Pacchettizzazione](reference/packaging.md), [Modello del plugin permanente](concepts/permanent-plugin.md) | [Transazioni e recupero](concepts/transactions-and-recovery.md), [Compatibilità](reference/compatibility.md), [Limitazioni note](reference/known-limitations.md), [Stato della validazione a runtime](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Navigazione

| Pagina | Scopo |
| --- | --- |
| [Per iniziare](getting-started/first-session.md) | Pianificare, avviare, osservare e arrestare una sessione dalla radice di OMSI. |
| [Installazione](getting-started/installation.md) | Prerequisiti, estrazione del pacchetto nella radice di OMSI, verifica con `/version`, rimozione pulita. |
| [Guida rapida all'API pubblica](getting-started/api-quick-start.md) | Un programma .NET completo che pianifica, avvia, legge e arresta una sessione. |
| [Riferimento CLI](reference/cli.md) | Ogni flag, parola di comando e route gerarchica di `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Esempi CLI](reference/cli-examples.md) | Righe di comando pronte da copiare e incollare per le attività comuni. |
| [Riferimento LaunchSpec](reference/launchspec.md) | Ogni proprietà e valore enum di `LaunchSpec`, regole di caricamento JSON per `/spec`. |
| [Gestione HOF](reference/hof-management.md) | Copia e mirror delle HOF nelle cartelle dei veicoli con ambito di sessione: sorgenti, destinazioni, conflitti, ripristino. |
| [Moduli runtime](../../reference/runtime-modules.md) | Moduli che vengono eseguiti all'interno di OMSI insieme a una sessione: manifest, attendibilità, ciclo di vita, criteri di errore, operazioni. |
| [Riferimento dei profili di sessione](reference/session-profiles.md) | Schema `profile.yaml` `omsilaunch.session-profile/v1`, chiavi, limiti, precedenza. |
| [Riferimento dell'API pubblica](reference/public-api.md) | `IOmsiLaunch`, record ed enum pubblici, stabilità per membro. |
| [Inventario dell'API pubblica](reference/public-api-inventory.md) | Elenco generato di ogni tipo e membro pubblico con firma e stabilità. |
| [Controllo a runtime](reference/runtime-control.md) | Canale dei comandi runtime, timeout, handle, semantica dell'arresto. |
| [Riferimento delle capability](reference/capabilities.md) | Catalogo delle capability e ogni id di operazione runtime pubblica con la relativa classificazione. |
| [Ciclo di vita della sessione](concepts/session-lifecycle.md) | Transizioni di `SessionState`, cosa garantisce `StartSessionAsync`, come termina una sessione. |
| [Transazioni e recupero](concepts/transactions-and-recovery.md) | Stati del journal, backup, verifica del ripristino, eliminazioni di sessione, recupero dopo un crash. |
| [Modello del plugin permanente](concepts/permanent-plugin.md) | La chiusura del plugin `plugins\OmsiLaunch.*`, integrità basata sul manifest, ciò che una sessione non tocca mai. |
| [Controllo locale / IPC](reference/local-control.md) | Protocollo named pipe `0.1`, endpoint per installazione, associazione a `session_id`, modello di fiducia. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | L'host Windows (senza console): differenze rispetto a `OmsiLaunch.exe`, `/silent`, finestre di dialogo, codici di uscita. |
| [Area di notifica di Windows](reference/windows-tray.md) | Indicatore nell'area di notifica: icona, menu, finestra di stato campo per campo, End session, riavvio di Esplora risorse. |
| [Riferimento degli errori](reference/errors.md) | Ogni codice `OL_E_*` / `OL_W_*` con categoria e significato. |
| [Codici di uscita](reference/exit-codes.md) | Valori di `PublicExitCode` da 0 a 10 e codici dello shim di bootstrap da 100 a 106. |
| [Pacchettizzazione / struttura dell'installazione](reference/packaging.md) | File nello ZIP di rilascio, `release-manifest.json`, struttura di `.omsilaunch\`. |
| [Compatibilità / build di OMSI supportati](reference/compatibility.md) | L'unico hash di `Omsi.exe` supportato, l'hash Steam LAA accettato, requisiti di piattaforma. |
| [Limitazioni note](reference/known-limitations.md) | Ciò che non è supportato, è parziale o costituisce un rischio accettato in questa beta. |
| [Stato della validazione a runtime](status/runtime-validation-status.md) | Cosa è stato eseguito sotto OMSI, cosa è stato eseguito solo offline, cosa richiede ancora una sessione reale. |

Pagine che restano normative per i manutentori: il
[`README.md`](../../../README.md) del repository, le specifiche in `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) e le pagine di progetto in
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Queste pagine riassumono; le
pagine sopra elencate sono il riferimento dettagliato. Le pagine storiche sono elencate nel
[manifest della documentazione](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Come questa documentazione viene mantenuta sincronizzata

Un gate della documentazione, `tests\OmsiLaunch.DocumentationTests`, viene compilato con
`OmsiLaunch.Api` e `OmsiLaunch.Core` e confronta le pagine sopra elencate con il
codice che definisce la superficie pubblica:

| Gate | Verifiche |
| --- | --- |
| `docs.cli-flags` | Ogni voce di `CliInput.KnownFlags` compare nel riferimento CLI come `` `/flag` `` o `` `/flag:` ``; ogni voce di `CliInput.AcceptedNoEffectFlags` è contrassegnata come `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` sulla propria riga; ogni parola di comando e ogni route di `CliInput.HierarchicalRoutes` compare con la relativa operazione runtime; ogni valore di `PublicExitCode` ha una riga `| n |` nella tabella dei codici di uscita. |
| `docs.capabilities` | Ogni id di `PublicCapabilityRegistry.All`, ogni voce di `PublicCapabilityRegistry.PublicRuntimeOperationIds` e ogni nome di `PublicCapabilityClassification` compare nel riferimento delle capability. |
| `docs.errors` | Ogni codice di `PublicErrorCodes.All` compare nel riferimento degli errori, e nessun letterale `OL_E_*` / `OL_W_*` in `src\` o `tools\OmsiLaunch.Cli\` manca da `PublicErrorCodes`. |
| `docs.public-api` | Ogni tipo esportato di `OmsiLaunch.Api`, ogni valore enum e ogni membro di `IOmsiLaunch` compare nel riferimento dell'API pubblica, e vengono usate tutte e cinque le parole di stabilità. |
| `docs.launchspec` | Ogni proprietà pubblica raggiungibile da `LaunchSpec` e ogni valore dei suoi enum compare nel riferimento LaunchSpec. |
| `docs.session-profiles` | Ogni chiave di `SessionProfileCompiler.SchemaKeys`, l'identificatore dello schema e il limite di `256 KiB` compaiono nel riferimento dei profili di sessione. |
| `docs.structure` | Ogni pagina della tabella di navigazione esiste. |
| `docs.links` | Ogni link relativo in `docs\**\*.md` (escluso `docs\localized\`) e nei file `*.md` della radice si risolve in un file o in una directory. |
| `docs.localization` | Ogni locale elencata in `docs\localized\LOCALIZATION-MANIFEST.md` dispone di ogni pagina dell'insieme localizzato; ogni pagina conserva i titoli, le tabelle e i blocchi di codice della pagina inglese, ogni span di codice inline (flag, id di capability e di operazione, codici di errore, chiavi, identificatori) e ogni link, e i suoi link relativi si risolvono. |

Il gate è una delle suite eseguite da `tools\Invoke-OfflineValidation.ps1`
(lo si salta con `-SkipDocs`). Viene eseguito offline, non avvia mai OMSI e fa fallire il
build quando un flag, una route, una capability, un codice di errore, un valore enum o un tipo pubblico non è
documentato o un link è interrotto. Non verifica la prosa, quindi una pagina può comunque
descrivere in modo errato il comportamento; in tal caso segnalarlo come bug della pagina.

<a id="translations"></a>
## Traduzioni

`docs\localized\<locale>\` contiene traduzioni complete di questa documentazione `0.1.0-beta4`
per `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` e `ja-JP`. L'insieme delle pagine, le radici delle locale e le
pagine intenzionalmente non tradotte sono elencati in
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
Le traduzioni mantengono invariati ogni comando, flag, identificatore, codice di errore ed esempio delle
pagine inglesi, e il gate `docs.localization` lo verifica.
Le pagine inglesi restano la fonte normativa: dove una traduzione non concorda
con esse, fanno fede la pagina inglese e il codice, e la traduzione
contiene un bug.
