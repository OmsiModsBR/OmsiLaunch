# Pacchetto e struttura del rilascio

<!-- l10n: source=reference/packaging.md source-sha256=6fff256ee354401567ab45240b0f155822e6321212dc63ea33ed6bc938b945ad -->
> Traduzione della [pagina originale in inglese](../../../reference/packaging.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina descrive il pacchetto di rilascio di OmsiLaunch `0.1.0-beta4`: che cosa produce `tools\New-ReleasePackage.ps1`, i campi di `release-manifest.json`, come il controller usa il manifest a runtime per verificare la chiusura del plugin permanente, come il pacchetto viene installato in una radice dell'installazione di OMSI e rimosso da essa, che cosa contiene la directory `.omsilaunch` dopo l'uso e gli script di validazione (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). L'identità del prodotto proviene da `OmsiLaunch.Version.props`. I passaggi di installazione per gli utenti sono in [installazione](../getting-started/installation.md); il ruolo a runtime della chiusura del plugin è descritto in [plugin permanente](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Identità del prodotto (`OmsiLaunch.Version.props`)

| Proprietà | Valore | Usata per |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | `product` del manifest, `ProductName` di Windows |
| `OmsiLaunchCompanyName` | `LMonteiro` | `CompanyName` di Windows |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/OmsiModsBR/OmsiLaunch` | `LegalCopyright` di Windows |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | `product_version` del manifest, `ProductVersion` di Windows, versione informativa dell'assembly (`/version`), nome dello ZIP pubblico |
| `OmsiLaunchManagedVersion` | `0.1.0` | Base della versione degli assembly gestiti |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Versione dell'assembly e versione del file Windows |
| `OmsiLaunchPackageAlias` | `current` | `package_alias` del manifest, cartella di staging e nome dello ZIP alias |

`Directory.Build.props` imposta `InformationalVersion` su `OmsiLaunchProductVersion` senza una revisione del codice sorgente, quindi `OmsiLaunch.exe /version` stampa esattamente `0.1.0-beta4`.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (output predefinito `artifacts\release`) prepara un pacchetto a partire da artefatti già compilati. La scrittura in `artifacts\release` quando `OmsiLaunch-<product_version>.zip` esiste già viene rifiutata a meno che non venga specificato `-AllowOverwritePublished`; i pacchetti candidati vanno in un'altra directory (la validazione offline usa `artifacts\candidate\post-round-a`).

Prima della preparazione, ogni output del build viene controllato per verificare che non sia obsoleto.

- **`OmsiLaunch.Native.x86.dll`: in base al contenuto, non al timestamp.** Il build nativo scrive `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (target `WriteOmsiLaunchNativeBuildReceipt` nel `.vcxproj`): lo SHA-256 della DLL prodotta (`output=`) e di ogni sorgente da cui è stata compilata (`source=<sha256>|<path>`: il `.cpp`, il `.rc`, il `.vcxproj` e `OmsiLaunch.Version.props`). La creazione del pacchetto rifiuta la DLL quando il suo hash non corrisponde all'output registrato (`does not match its build receipt`: una copia obsoleta o estranea, qualunque sia il suo timestamp), quando un sorgente registrato è cambiato (`Native source changed after the recorded build`), quando un sorgente nativo non è coperto dalla ricevuta o quando la ricevuta manca.
- **Shim e assembly gestiti: in base al timestamp.** Ciascuno non deve essere più vecchio dei sorgenti del proprio progetto (`.cpp`/`.rc`/`.vcxproj` di ogni shim; il progetto di ogni assembly gestito). Un input obsoleto interrompe la creazione del pacchetto con `Stale build artifact`.

Il pacchetto viene quindi assemblato in una directory di staging nuova e con nome univoco (`.staging-<guid>` nella directory di output), in modo che nessun file di un'esecuzione precedente possa entrare nella chiusura. La cartella `OmsiLaunch-current` e gli archivi precedenti vengono sostituiti solo dopo che tutti i controlli descritti di seguito sono stati superati.

| Origine | Destinazione nel pacchetto |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | radice |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| `assets\splash\*.bmp` della CLI (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | radice |
| ogni file in `docs\` tranne `docs\localized\` (la documentazione in inglese, con la stessa struttura di directory) | `.omsilaunch\docs\` (in modo che `.omsilaunch\docs\reference\cli.md`, il percorso stampato dal testo di utilizzo della CLI, esista; audit della documentazione BUG-08). I collegamenti da `docs\README.md` ai riepiloghi nella radice del repository (`PUBLIC-API.md` e altri) si risolvono solo nel repository dei sorgenti. |
| `docs\localized\LOCALIZATION-MANIFEST.md` e `docs\localized\<locale>\**` per ogni locale elencata in quel manifest (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` e `ja-JP`) | `.omsilaunch\docs\localized\` (stessa struttura); una locale elencata ma mancante interrompe lo script |

Lo script calcola quindi l'hash di ogni file preparato, scrive `release-manifest.json` nella radice del pacchetto come UTF-8 **senza** BOM (il risultato non dipende più dall'edizione di PowerShell), esegue `Test-ReleasePackageIntegrity.ps1` sullo staging, confronta di nuovo ogni file del plugin preparato e `OmsiLaunch.Native.x86.dll` con il relativo output del build, comprime lo staging, **estrae l'archivio in una directory temporanea nuova e valida la chiusura estratta rispetto allo stesso manifest** (il manifest archiviato deve essere identico byte per byte a quello validato), quindi pubblica lo staging come `OmsiLaunch-current` e l'archivio come `OmsiLaunch-current.zip`, lo copia in `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) e scrive `OmsiLaunch-0.1.0-beta4.zip.sha256` contenente `<SHA-256>  <file name>`. Qualsiasi artefatto mancante interrompe lo script. Lo script non esegue il build; eseguire prima `Invoke-OfflineValidation.ps1` (o i singoli passaggi `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Struttura del pacchetto

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Solo i file del prodotto nella radice, `plugins\OmsiLaunch.*` e `.omsilaunch\` sono di proprietà del prodotto. I plugin di terze parti in `plugins\` non vengono mai enumerati, copiati, sottoposti a hash, rimossi o ripristinati da OmsiLaunch.

## `release-manifest.json`

| Campo | Tipo | Significato |
|---|---|---|
| `product` | stringa | `OmsiLaunch` |
| `product_version` | stringa | `0.1.0-beta4` |
| `package_alias` | stringa | `current` |
| `control_protocol` | stringa | `0.1`; deve corrispondere a `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | stringa | `OMSI 2` |
| `known_layouts` | object[] | `build_id` e `code_sha256` di ciascun layout noto (`Omsi23004`, `13ec110a...c028`); un percorso rapido, non un requisito |
| `configuration` | stringa | `Release` o `Debug` |
| `generated_utc` | stringa | Ora del build in formato ISO-8601 |
| `files[]` | object[] | `path` (barre in avanti, relativo alla radice del pacchetto), `bytes`, `sha256` (esadecimale maiuscolo) per ogni file del pacchetto |

Il manifest è costituito da dati, mai da criteri eseguibili: il controller legge solo le voci `plugins/`. Il lettore accetta il file con o senza BOM UTF-8 (i manifest scritti da Windows PowerShell 5.1 prima di questa correzione ne contengono uno).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Uso del manifest a runtime (integrità del plugin)

Prima di ogni piano e di ogni avvio, `OmsiLaunchService.LoadArtifacts` costruisce la chiusura del plugin attesa (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. Il controller cerca `release-manifest.json` accanto a `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Se presente, `ReleaseManifest.TryReadPluginHashes` estrae gli hash di `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID` se il file non può essere letto come manifest).
2. Di ogni file installato `<root>\plugins\OmsiLaunch.*` viene calcolato l'hash (SHA-256), che viene confrontato:
   - con un manifest: con l'hash del manifest; viene registrata la voce di diagnostica del piano `plugin.integrity.reference = manifest`. File mancante → `OL_E_PERMANENT_PLUGIN_MISSING`; file presente ma non elencato → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; hash diverso → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - senza un manifest (layout di sviluppo, oppure un'installazione che ha omesso il manifest): è possibile controllare solo la presenza e la coerenza interna rispetto alla copia del pacchetto accanto al controller; `plugin.integrity.reference = self`.
3. Un errore rende il piano non eseguibile (`OL_E_RUNTIME_ARTIFACT_MISSING` con il dettaglio) oppure rifiuta l'avvio (uscita `7`).

I file del plugin non vengono mai preparati, sottoposti a snapshot, ripristinati o rimossi da una sessione; la chiusura è una parte permanente dell'installazione. La DLL x86 `OmsiLaunch.Native.x86.dll` viene caricata solo da `plugins\`; gli assembly gestiti dichiarano `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Installazione nella radice di OMSI

1. Verificare l'archivio: confrontare `OmsiLaunch-0.1.0-beta4.zip` con `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Estrarre l'archivio **direttamente nella radice dell'installazione di OMSI** (la directory che contiene `Omsi.exe`). In questo modo vengono collocati i file della radice, `plugins\OmsiLaunch.*` (accanto a eventuali plugin di terze parti) e `.omsilaunch\`.
3. Mantenere `release-manifest.json` accanto a `OmsiLaunch.exe`: abilita l'integrità del plugin basata sul manifest. Il manifest e i binari devono provenire dallo stesso pacchetto: binari nuovi con un manifest più vecchio (o viceversa) fanno fallire ogni avvio con `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` ora copia il manifest insieme ai file del prodotto; in precedenza lo tralasciava, lasciando un manifest più vecchio accanto a binari più recenti (Round A RA-007).
4. Controllare la coerenza in sola lettura con `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` deve essere `true`.
5. Non spostare i binari del plugin in `.omsilaunch\` e non rinominare `plugins\OmsiLaunch.*`.
6. Verificare con `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` e un `/plan` (vedere [prima sessione](../getting-started/first-session.md)).

I file `.omsilaunch\assets\splash\*.bmp` esistenti non vengono mai sovrascritti da una sessione (un insieme di asset gestito esplicitamente rimane); sovrascriverli estraendo un nuovo pacchetto è un'azione deliberata dell'utente.

<a id="the-omsilaunch-directory-after-use"></a>
## La directory `.omsilaunch` dopo l'uso

| Percorso | Creato da | Durata |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | pacchetto, oppure copiato alla prima sessione con splash screen gestita | persistente |
| `docs\`, `examples\` | pacchetto | persistente |
| `session-profiles\<id>\profile.yaml` | utente | persistente; vedere [profili di sessione](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | ogni sessione | conservato per le 50 sessioni più recenti; i file più vecchi con prefisso di sessione vengono eliminati all'avvio di una nuova sessione |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, harness di validazione | stessa conservazione (prefisso di sessione) |
| `diagnostics\tray-host.log` | indicatore nell'area di notifica | persistente, in accodamento |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | persistente (senza prefisso di sessione) |
| `journal.json` | transazione | esiste da `Prepared` fino a `Restored`; un residuo significa che un recupero è in sospeso (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | transazione | snapshot dei file modificati; rimossi dopo il ripristino |

Nessun dato lascia la macchina. Vedere [transazioni e recupero](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Disinstallazione

1. Assicurarsi che nessuna sessione sia in esecuzione (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) e che nessun recupero sia in sospeso (`OmsiLaunch.exe /recovery-status`; eseguire `/recover` se `pending` è `true`), in modo che i file di OMSI siano già ripristinati.
2. Eliminare `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Lasciare intatti gli altri plugin.
3. Eliminare i file del prodotto nella radice elencati nella struttura sopra (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Eliminare `.omsilaunch\` (in questo modo vengono rimossi i profili di sessione e la diagnostica). Non eliminarla mai finché esiste `journal.json`.

Una sessione completata ripristina ogni file di sua proprietà, quindi non è necessaria alcuna ulteriore pulizia. I file che OMSI stesso scrive durante l'esecuzione (ad esempio `[last_map]` in `options.cfg`, le cache, `laststn.osn`, i log) sono il normale stato di OMSI e non vengono riportati allo stato precedente; vedere [transazioni e recupero](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Script di validazione

| Script | Scopo | Modifica OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Compila `OmsiLaunch.sln` con gli avvisi trattati come errori e i tre progetti nativi (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) tramite MSBuild, quindi esegue tutte le suite offline: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` e `OmsiLaunch.DocumentationTests` se non viene saltata, poi il test di regressione del pacchetto `Test-PackagingPipeline.ps1` (saltato con `-SkipNative`). Stampa `OFFLINE VALIDATION PASSED`/`FAILED`. | No |
| `tools\New-ReleasePackage.ps1` | Controllo degli artefatti obsoleti, staging, manifest, autoverifica dell'integrità, ZIP, checksum (vedere sopra). | No |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifica che il manifest elenchi esattamente i file del pacchetto con dimensione e SHA-256 corrispondenti, che la chiusura richiesta (tre eseguibili, il controller, i nove file del plugin permanente incluso `OmsiLaunch.Native.x86.dll`) sia presente e che la configurazione sia `Release`. Con `-InstallationRoot` confronta i file del prodotto dell'installazione con il pacchetto **in sola lettura**. Uscita `0` = coerente. | No (sola lettura) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produce un pacchetto candidato da uno staging pulito in `artifacts\candidate\post-round-a` e richiede che la chiusura preparata e l'archivio estratto nuovamente superino il controllo di integrità. Dimostra che il gate di integrità rifiuta una DLL manomessa, un `Native.x86` manomesso o vecchio, una copia obsoleta del plugin, un file elencato eliminato, un file inatteso, un hash del manifest modificato o malformato, voci duplicate (esatte, per maiuscole/minuscole, per separatore), percorsi padre e assoluti e JSON non valido; che il creatore del pacchetto rifiuta un vecchio `Native.x86` collocato nell'output del build (anche con un timestamp più recente) e una ricevuta i cui sorgenti sono cambiati; e che l'archivio pubblicato non viene mai sovrascritto. Gli output del build vengono ripristinati byte per byte. Eseguito da `Invoke-OfflineValidation.ps1`. | No |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Estrae lo ZIP in `artifacts\release\identity-verification`, controlla `product`/`product_version`/`package_alias` e verifica `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` di ogni `.exe`/`.dll` tranne `nethost.dll` e `YamlDotNet.dll`, `InternalName`/`OriginalFilename` di entrambi gli shim e che `OmsiLaunch.exe` contenga un'icona incorporata. | No |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Valida l'eseguibile Release del pacchetto rispetto a un'installazione reale: il manifest deve essere `Release`, non deve contenere percorsi `Debug` o `runtime/plugin/` e deve installare `plugins/OmsiLaunch.*`; i quattro asset della splash screen devono esistere. Esegue tre casi `/plan` (gestita predefinita, gestita con asset personalizzati, `/splash:Unset`). Con `-RunOmsi` (richiede `-InstallPackage`) avvia ciascun caso con `/observe-seconds`, osserva `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_PTB.bmp` durante la sessione e verifica ripristino esatto, assenza di `Omsi.exe`, assenza di `journal.json`, uscita `0`, hash dei plugin di terze parti invariati e insieme del plugin permanente invariato. Scrive `.omsilaunch\diagnostics\release-presentation-validation.json`. | Sì con `-RunOmsi` (limitato alla sessione, ripristinato) |

Entrambi gli script `Test-*` leggono `OmsiLaunch.Version.props` per conoscere la versione attesa.
