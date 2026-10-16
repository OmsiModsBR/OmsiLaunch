# Riferimento dell'API pubblica (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> Traduzione della [pagina originale in inglese](../../../reference/public-api.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina è il riferimento normativo per l'API pubblica gestita di OmsiLaunch 0.1.0-beta4: l'assembly `OmsiLaunch.Api` (contratti) e il punto di ingresso per gli integratori `OmsiLaunchService` in `OmsiLaunch.Core`. Documenta soltanto ciò che fa il codice attuale. Tutto ciò che un integratore può chiamare, ricevere oppure osservare è elencato qui con il relativo livello di stabilità; ciò che non è elencato non è una superficie di integrazione.

L'[inventario dell'API pubblica](public-api-inventory.md), generato automaticamente, elenca ogni tipo e membro pubblico di `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process` con la relativa firma e stabilità; un gate della documentazione fallisce quando l'inventario e gli assembly differiscono. Questa pagina ne spiega la semantica.

Pagine correlate: [riferimento LaunchSpec](launchspec.md), [codici di errore](errors.md), [ciclo di vita della sessione](../concepts/session-lifecycle.md), [transazioni e recupero](../concepts/transactions-and-recovery.md), [controllo runtime](runtime-control.md), [capability](capabilities.md), [piano di controllo locale](local-control.md), [codici di uscita](exit-codes.md), [stato della validazione a runtime](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Vocabolario della stabilità

| Livello | Significato in questa pagina |
| --- | --- |
| `STABLE_BETA` | Il contratto è congelato per la linea di protocollo 0.1 e il percorso è validato a runtime in `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Richiamabile e testato, ma il contratto o l'evidenza di runtime possono cambiare prima che diventi stabile. |
| `PARTIAL` | Presente nel contratto; solo una parte del comportamento è implementata o validata (il testo indica quale parte). |
| `INTERNAL` | Pubblico nell'assembly per ragioni tecniche (il bridge condivide il tipo) ma non è una superficie di integrazione; può cambiare senza preavviso. |
| `UNAVAILABLE` | Presente nel contratto ma rifiutato dal build attuale. |

<a id="assembly-overview"></a>
## Panoramica degli assembly

| Assembly | Ruolo per gli integratori |
| --- | --- |
| `OmsiLaunch.Api` | Contratti puri: record, enum, `IOmsiLaunch`, registro delle capability, catalogo degli errori, formati di trasmissione, helper D3D. Non contiene alcun `IntPtr`, `nint`, handle Win32, indirizzo nativo o oggetto processo. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (l'implementazione di `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` e `CurrentWindowsX64Platform` (l'unico adattatore di piattaforma), `InstallationLease`. Necessari per costruire il servizio. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Assembly di implementazione. I loro tipi pubblici sono `INTERNAL` per gli integratori. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Punto di ingresso: `OmsiLaunchService` e `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parametro | Valore valido | Non valido / predefinito |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (namespace `OmsiLaunch.Process`). Rileva la piattaforma, crea il processo OMSI con `CreateProcessW`, ne attende la terminazione e lo termina. | Non viene distribuita nessun'altra implementazione. Un `IRuntimePlatform` personalizzato è `INTERNAL`. |
| `PluginBuildDirectory` | Directory che contiene i file di riferimento della chiusura del plugin permanente: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` e ogni `OmsiLaunch.*.dll` della chiusura gestita (deve includere `OmsiLaunch.Plugin.dll`). In un pacchetto installato è `<package>\plugins`. | Directory o file mancante: `PlanSessionAsync` restituisce un piano non eseguibile con `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Percorso di `OmsiLaunch.Native.x86.dll` (nel pacchetto: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Come sopra. |
| `ReleaseManifestPath` | `release-manifest.json` accanto a `OmsiLaunch.exe`, se presente. Fornisce lo SHA-256 atteso di ciascun file di `plugins/` (`plugin.integrity.reference = manifest`). | `null` (layout di sviluppo): dei file installati vengono verificate solo la presenza e la coerenza interna rispetto alla chiusura di riferimento (`plugin.integrity.reference = self`). Manifest malformato: `OL_E_RELEASE_MANIFEST_INVALID`. |

Il servizio legge questi percorsi a ogni `PlanSessionAsync` e `StartSessionAsync`; non copia, non prepara e non rimuove mai file del plugin (vedere [plugin permanente](../concepts/permanent-plugin.md)). La CLI costruisce il servizio esattamente in questo modo (`tools/OmsiLaunch.Cli/Program.cs`):

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

Creare un solo servizio per processo e condividerlo. Stabilità: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Regole di proprietà della sessione

| Regola | Dettaglio |
| --- | --- |
| Un solo proprietario della sessione (owner) per installazione | `StartSessionAsync` acquisisce il lease dell'installazione (blocco esclusivo), un semaforo con nome `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, e lo mantiene finché il supervisore non ha ripristinato l'installazione. Un secondo avvio sulla stessa radice da qualsiasi processo della stessa sessione di logon fallisce con `OL_E_INSTALLATION_BUSY` (segnalato come sessione `Failed`, vedere `StartSessionAsync`). Il lease vale per sessione di logon, non tra sessioni di logon diverse, e non viene rilasciato finché un altro processo mantiene un handle su di esso (rischio accettato). |
| Gli handle sono locali al processo | `SessionHandle` incapsula il `Guid` della sessione. Ha significato solo per l'istanza di `OmsiLaunchService` che l'ha restituito. Un handle costruito a partire da un `Guid` noto in un altro processo (o in un'altra istanza del servizio) produce `KeyNotFoundException`. Il controllo tra processi passa per il [piano di controllo locale](local-control.md), non per gli handle. |
| Chiamare sempre `CloseAsync` | Da `StartSessionAsync` in poi il processo possiede una transazione durevole. `CloseAsync` richiede l'arresto canonico quando necessario, attende il supervisore (uscita del processo, ripristino esatto, rilascio del lease) e dimentica la sessione. Deve essere chiamato su ogni percorso di uscita, anche dopo uno stato `Failed`. Senza di esso la voce della sessione resta in memoria; il ripristino vero e proprio viene comunque eseguito dal supervisore. |
| Le sessioni fallite sono comunque sessioni | Un avvio che fallisce dopo che `StartSessionAsync` ha restituito il controllo segnala `SessionState.Failed`; l'handle resta valido per `GetStatusAsync`/`WaitForAsync` fino a `CloseAsync`. |
| I piani vengono ricontrollati | `StartSessionAsync` ricalcola l'hash di `Omsi.exe` e ripianifica la specifica; un piano che non è più eseguibile viene rifiutato con `OL_E_PLAN_NOT_RUNNABLE`. |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

Fatti comuni a tutti i metodi:

- Gli handle sconosciuti o già chiusi generano `KeyNotFoundException` ("Unknown OmsiLaunch session.").
- Nessun metodo richiede una sessione in stato Running, tranne `ExecuteRuntimeAsync`.
- Le eccezioni che trasportano un codice OmsiLaunch collocano il codice all'inizio di `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). La CLI estrae i codici dai messaggi nello stesso modo (`CliProgram.Classify`).
- Installazione supportata: OMSI 2. Le capability native vengono risolte e verificate per ciascun build nel processo; una capability non disponibile su un build fallisce con `OL_E_CAPABILITY_UNRESOLVED`. Vedere [compatibilità](compatibility.md).

<a id="complete-minimal-example"></a>
### Esempio minimo completo

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Scopo | Compilare una `LaunchSpec` in un `SessionPlan` senza avviare OMSI: validare la specifica, rilevare la piattaforma, calcolare l'impronta di `Omsi.exe`, risolvere le identità dei contenuti, calcolare le modifiche pianificate ai file, elencare le capability richieste e non supportate e decidere `IsRunnable`. Capability pubblica `session.plan`. |
| Parametri | `spec`: una `LaunchSpec` completamente popolata (vedere [riferimento LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (tutti e otto i dizionari) e `Behavior` devono essere non null; i membri facoltativi possono essere `null`. `RootPath` dovrebbe essere una directory assoluta; una radice vuota viene registrata come `OL_E_INSTALLATION_NOT_FOUND`, ma il probe della piattaforma su un percorso vuoto genera `ArgumentException` prima che il piano venga restituito, quindi non passare mai una radice vuota. |
| Valore restituito | `SessionPlan` con un nuovo `SessionId`, `BuildProfileId` = il layout noto (`"Omsi23004"`) oppure `"unrecognized"` per un build di OMSI 2 senza layout noto, la `Spec` in ingresso, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (percorsi di destinazione `plugins\OmsiLaunch.*` più `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (un `PlannedModule` per ogni [modulo runtime](../../../reference/runtime-modules.md) richiesto o attivato dal contenuto; accessor `EffectivePlannedModules`). `IsRunnable` è `true` esattamente quando nessun codice di diagnostica inizia con `OL_E_`. Le voci di diagnostica informative (`plugin.integrity.reference` con messaggio `self` o `manifest`, `session_profile.selected`) non rendono mai un piano non eseguibile. |
| Errori trasportati nel risultato | Ogni errore di pianificazione è una voce di diagnostica, non un'eccezione: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (il messaggio contiene il codice splash/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (la chiusura del plugin installata in `plugins\` viene verificata rispetto al manifest di rilascio in fase di pianificazione), `OL_E_RUNTIME_ARTIFACT_MISSING` (il messaggio può contenere `OL_E_RELEASE_MANIFEST_INVALID`). Condizioni complete: [regole di validazione della LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Eccezioni generate | `OperationCanceledException` se il token è già annullato all'ingresso (l'unico punto di controllo); `ArgumentException`/`NotSupportedException` per percorsi radice sintatticamente non validi; `NullReferenceException`/`ArgumentNullException` per membri obbligatori null; `System.Text.Json.JsonException` per un manifest di rilascio sintatticamente non valido. |
| Annullamento | Verificato una volta all'ingresso. Successivamente la pianificazione è lavoro sincrono sul file system. |
| Sessione in esecuzione richiesta | No. |
| Modifica lo stato di OMSI | No. |
| Modifica il file system | No (legge `Omsi.exe`, i file di contenuto, la chiusura del plugin, il manifest). I valori delle impostazioni non vengono validati qui (solo l'esistenza della chiave e la scrivibilità); un valore non valido fallisce all'avvio con `OL_E_INVALID_SETTING_VALUE`. |
| Transazione / ripristino | Nessuna. |
| Limitazioni | Richiedere per `Date`/`Time`/`Year` una modalità diversa da `Unset`, per `Weather` una modalità diversa da `Unset`, un qualsiasi campo di `PlayerVehicle`, documenti `Input`, `EntrypointIdentity` oppure `WorldMode.LastMapState` produce `OL_E_CAPABILITY_UNAVAILABLE` e un piano non eseguibile su questo build (voci in `UnsupportedRequestedFeatures` che riportano la loro evidenza di catalogo, `UNAVAILABLE` o `PARTIAL`). |
| Stabilità | `STABLE_BETA`. |
| Esempio | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Scopo | Avviare una sessione OMSI gestita e transazionale a partire da un piano eseguibile: acquisire il lease dell'installazione, recuperare un journal obsoleto, validare la chiusura del plugin permanente, eseguire lo snapshot dei file di sessione e applicarne l'overlay, creare l'handoff di avvio, lo slot di telemetria e la mailbox runtime, avviare `Omsi.exe`, registrare il processo nel journal e affidare la sessione a un supervisore in background. Capability pubblica `session.start`. |
| Parametri | `plan`: un `SessionPlan` con `IsRunnable == true`. La specifica contenuta nel piano viene ripianificata; del piano del chiamante viene mantenuto solo `plan.SessionId`. `plan.Spec.Behavior.StartupTimeoutSeconds` deve essere compreso in 1..600. |
| Valore restituito | `SessionHandle(plan.SessionId)` non appena `Omsi.exe` è stato creato e registrato (stato `WaitingForPlugin`), oppure non appena il percorso di avvio è fallito (stato `Failed`). Non attende il gameplay; usare `WaitForAsync(session, SessionState.Running, ...)`. |
| Eccezioni generate | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` quando `plan.IsRunnable` è false; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` quando la ripianificazione non è eseguibile (per esempio `Omsi.exe` modificato, contenuto rimosso, chiusura del plugin mancante); `InvalidOperationException("Duplicate session id.")` quando una sessione con lo stesso id è ancora registrata (chiamare prima `CloseAsync`); `ArgumentOutOfRangeException` quando `StartupTimeoutSeconds` è fuori dall'intervallo 1..600; `OperationCanceledException` in caso di annullamento prima o durante la ripianificazione; più tutto ciò che genera `PlanSessionAsync`. In tutti i casi di eccezione non viene registrata alcuna sessione. |
| Errori trasportati nel risultato | Qualsiasi errore successivo alla ripianificazione viene intercettato all'interno del percorso di avvio: la sessione viene registrata, il suo stato è `Failed` e le sue voci di diagnostica contengono `OL_E_START_SESSION`, il cui messaggio è il messaggio interno (che inizia con il codice interno quando esiste): `OL_E_INSTALLATION_BUSY` (lease detenuto oppure un processo OMSI registrato nel journal ancora attivo), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (solo quando il nuovo tentativo differito con gli overlay di questa sessione non riesce ancora a dimostrare la proprietà), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. La pulizia può aggiungere `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (uscita di OMSI non confermata; journal conservato) oppure `OL_E_RESTORE_FAILED`. Gli errori successivi vengono segnalati dal supervisore (vedere [ciclo di vita della sessione](../concepts/session-lifecycle.md)). |
| Annullamento | Prima o durante la ripianificazione: genera un'eccezione. Dopo, il token viene passato alla transazione e alla creazione del processo; un annullamento in quella fase viene trattato come qualsiasi errore di avvio (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), il processo (se creato) viene terminato e l'installazione ripristinata. |
| Sessione in esecuzione richiesta | No. |
| Modifica lo stato di OMSI | Sì: crea il processo OMSI con le variabili d'ambiente `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Modifica il file system | Sì, all'interno della radice dell'installazione: `.omsilaunch\diagnostics\<sessionId>-host.log` (conservazione: le 50 sessioni più recenti), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (copiato una sola volta per lo splash screen gestito), overlay di sessione (patch di `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), eliminazioni di sessione (destinazioni ITX, `Texture\standard.ipr`, `closecheck`) e rimozione permanente di un `closecheck` obsoleto preesistente quando `SuppressStaleClosecheckWarning` è true (voce di diagnostica `closecheck.stale-removed`). |
| Transazione / ripristino | Apre la transazione (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Ogni percorso di uscita dalla sessione termina con un ripristino. Vedere [transazioni e recupero](../concepts/transactions-and-recovery.md). |
| Limitazioni | Solo `WorldMode.NewMap` con `PresentedEntrypointIndex` e `WorldMode.SavedSituation` raggiungono il gameplay. `WorldMode.LastMapState` è `UNAVAILABLE`. Le richieste di veicolo del giocatore non raggiungono mai questo metodo, perché sono non eseguibili già in fase di pianificazione. |
| Stabilità | `STABLE_BETA` (i cicli di vita NEW_MAP e SAVED_SITUATION sono validati a runtime). |
| Esempio | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Scopo | Leggere lo stato semantico del ciclo di vita, le voci di diagnostica raccolte finora e l'elenco limitato degli eventi runtime. Capability pubblica `session.status`. |
| Parametri | `session`: un handle restituito da `StartSessionAsync` e non ancora chiuso. |
| Valore restituito | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: uno snapshot immutabile (gli array vengono copiati sotto il lock della sessione). `RuntimeEvents` non è mai `null` per una sessione attiva. |
| Eccezioni generate | `KeyNotFoundException` per handle sconosciuti o chiusi. In tutti gli altri casi non genera mai eccezioni. |
| Annullamento | Il token viene ignorato (la chiamata si completa in modo sincrono). |
| Sessione in esecuzione richiesta | No. |
| Modifica OMSI / file system / transazione | No / No / Nessuna. |
| Stabilità | `STABLE_BETA`. |
| Esempio | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Scopo | Interrogare periodicamente (ogni 100 ms) finché la sessione non si trova in `state`, oppure in uno stato terminale (`Completed`, `Failed`), oppure finché non scade il timeout; quindi restituire lo stato corrente. |
| Parametri | `state`: qualsiasi `SessionState`. L'attesa di uno stato transitorio già superato (o che non viene mai impostato, vedere [ciclo di vita della sessione](../concepts/session-lifecycle.md)) dura fino a uno stato terminale o al timeout. `timeout`: qualsiasi `TimeSpan` non negativo oppure `Timeout.InfiniteTimeSpan`. |
| Valore restituito | Lo stato nel momento in cui l'attesa è terminata. Allo scadere del timeout viene restituito lo stato, non un'eccezione: il chiamante deve verificare `State`. Un'attesa di `Running` che termina in `Failed` ritorna immediatamente con le voci di diagnostica dell'errore. |
| Eccezioni generate | `KeyNotFoundException`; `OperationCanceledException` quando il token del chiamante viene annullato (si propaga solo l'annullamento del chiamante; il timeout interno no). |
| Annullamento | Il token del chiamante viene rispettato a ogni ciclo di 100 ms. |
| Sessione in esecuzione richiesta | No. |
| Modifica OMSI / file system / transazione | No / No / Nessuna. |
| Stabilità | `STABLE_BETA`. |
| Esempio | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Scopo | Richiedere l'arresto canonico. Imposta il flag di arresto e ritorna immediatamente; il supervisore rileva il flag entro il proprio ciclo di 100 ms, chiama `TerminateProcess` su `Omsi.exe`, attende l'uscita, marca il journal come `ProcessExited`, ripristina ogni file di proprietà della sessione e rilascia il lease. Si tratta di una terminazione forzata: la routine di chiusura propria di OMSI non viene eseguita e OMSI non riscrive `options.cfg` all'uscita (scelta deliberata, protegge la transazione). La chiusura cooperativa tramite `WM_CLOSE` non è implementata (decisione di prodotto; nella chiusura di runtime OMSI non si è chiuso entro 30 s da `WM_CLOSE`, `L05b`). Capability pubblica `session.stop`. |
| Parametri | `session`. |
| Valore restituito | Task completato; non attende la terminazione né il ripristino. Usare `WaitForAsync(session, SessionState.Completed, ...)` per osservare il completamento. |
| Eccezioni generate | `KeyNotFoundException`. |
| Annullamento | Token ignorato. |
| Sessione in esecuzione richiesta | No. Idempotente; un arresto richiesto prima dell'avvio del supervisore viene eseguito non appena il supervisore parte; un arresto su una sessione terminale non ha effetto. |
| Modifica lo stato di OMSI | Sì: termina il processo OMSI (codice di uscita 1). |
| Modifica il file system | Indirettamente: attiva il ripristino, l'eliminazione del journal e la rimozione dei backup da parte del supervisore. |
| Transazione / ripristino | Attiva `ProcessExited` → `Restoring` → `Restored`. Le modifiche lato runtime effettuate tramite `ExecuteRuntimeAsync` (scritture dell'orologio, veicoli generati, variabili di script, texture D3D) non vengono ripristinate; scompaiono con il processo. |
| Stabilità | `STABLE_BETA`. |
| Esempio | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Scopo | Rilasciare l'handle del consumatore senza lasciare la transazione in sospeso: se la sessione non è terminale, richiedere l'arresto canonico; quindi attendere il task del ciclo di vita del supervisore (uscita del processo, ripristino, rilascio del lease); infine dimenticare la sessione. |
| Parametri | `session`. |
| Valore restituito | Si completa quando la sessione è terminale e rimossa. Dopo il ritorno, l'handle è sconosciuto (`KeyNotFoundException` a ogni chiamata successiva, compreso un secondo `CloseAsync`). |
| Eccezioni generate | `KeyNotFoundException`; `OperationCanceledException` se il chiamante annulla durante l'attesa del supervisore. In tal caso la sessione non viene rimossa e il supervisore continua a funzionare; chiamare di nuovo `CloseAsync`. |
| Annullamento | Si applica solo all'attesa; non annulla mai il ripristino. |
| Sessione in esecuzione richiesta | No. |
| Modifica lo stato di OMSI | Sì, quando la sessione è ancora attiva (come `StopAsync`). |
| Modifica il file system | Indirettamente (ripristino da parte del supervisore). |
| Transazione / ripristino | Garantisce che la transazione venga portata a completamento prima che l'handle venga rilasciato (quando il supervisore è stato avviato). Per una sessione fallita prima dell'avvio del supervisore, il percorso di avvio ha già eseguito il ripristino oppure segnalato `OL_E_RESTORE_DEFERRED`. |
| Stabilità | `STABLE_BETA`. |
| Esempio | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Scopo | Eseguire un'operazione runtime pubblica all'interno del processo OMSI in esecuzione tramite la mailbox di sessione a richiesta singola (memory-mapped, 64 KiB, richiesta legata all'id di sessione e all'id di richiesta). Il plugin esegue l'operazione sul thread dell'interfaccia utente di OMSI. Catalogo delle operazioni: [controllo runtime](runtime-control.md) e [capability](capabilities.md). |
| Parametri | `command.SessionId` deve essere uguale a `session.SessionId`. `command.RequestId`: `ulong` scelto dal chiamante; usare un contatore strettamente crescente per processo (gli helper D3D partono da 30 000, l'owner CLI da 10 001/50 000). `command.Operation`: un id di operazione pubblica da `PublicCapabilityRegistry.PublicRuntimeOperationIds` (per esempio `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: valori stringa indicizzati per nome con confronto ordinale; i nomi obbligatori per ciascuna operazione provengono da `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: misurato dal momento in cui la richiesta viene depositata nella mailbox (l'attesa in coda dietro un altro comando in corso non viene conteggiata). La CLI usa 5 s (15 s per `road-vehicles.spawn`) come owner e 8 s / 30 s come client. |
| Ordine dei controlli | 1. Validazione del registro (prima della ricerca della sessione): operazione sconosciuta o `internal.*` → risultato `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; argomento obbligatorio mancante (assente o composto solo da spazi) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Ricerca della sessione → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Stato diverso da `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Richiesta alla mailbox. 6. I valori del risultato la cui chiave inizia con `internal_` o termina con `_address`, `_pointer`, `_vmt` vengono rimossi. |
| Valore restituito | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. In caso di successo `Values` contiene le stringhe semantiche dell'operazione (documentate per ciascuna operazione in [controllo runtime](runtime-control.md)). |
| Errori trasportati nel risultato | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (registro); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (il risultato del plugin ha superato la mailbox; i risultati degli elenchi limitati vengono invece accorciati con `truncated=true`); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` con una chiave derivata); ogni codice `OL_E_D3D_*` (con `Values["detail"]` e `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (la capability nativa dell'operazione non è risolta sul build in esecuzione); e ogni errore tipizzato lato plugin con il proprio codice in `ErrorCode` e il messaggio in `Values["detail"]` (Beta 4; solo un errore privo di un codice di catalogo è `OL_E_RUNTIME_OPERATION_FAILED`). Codici che arrivano in questo modo: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (controlli lato plugin), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Vedere [codici di errore](errors.md). |
| Eccezioni generate | `KeyNotFoundException`; `InvalidOperationException` con `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (mailbox già rilasciata dal supervisore), `OL_E_RUNTIME_CHANNEL_BUSY` (lo slot contiene ancora una richiesta abbandonata), `OL_E_RUNTIME_REQUEST_ID_REUSED` (una risposta obsoleta per lo stesso id di richiesta si trova ancora nello slot); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (risposta corrotta, estranea o non corrispondente); `ArgumentOutOfRangeException` quando la richiesta serializzata supera la mailbox; `OperationCanceledException`. |
| Annullamento | Rispettato durante l'attesa del gate per sessione e ogni 20 ms durante il polling della risposta. L'annullamento a richiesta in corso non reimposta lo slot: la chiamata successiva su quella sessione può fallire con `OL_E_RUNTIME_CHANNEL_BUSY` finché il plugin non pubblica la propria risposta (che viene poi scartata come obsoleta). Preferire il timeout; un timeout reimposta lo slot e una risposta tardiva viene rilevata e scartata. |
| Sessione in esecuzione richiesta | Sì (`SessionState.Running`); altrimenti viene generato `OL_E_SESSION_NOT_RUNNING`. La mailbox esiste finché il supervisore non la rilascia durante il ripristino. |
| Modifica lo stato di OMSI | Dipende dall'operazione: le operazioni `Read` no; le operazioni `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) modificano lo stato interno al processo, che non viene ripristinato. |
| Modifica il file system | Nessuna scrittura da parte dell'host. OMSI può scrivere i propri file di conseguenza (non tracciati). |
| Transazione / ripristino | Nessuna. |
| Limitazioni | Un solo comando in corso per sessione (le chiamate sulla stessa sessione vengono serializzate). Richiesta e risposta sono limitate ciascuna a 64 KiB meno 8 byte; i payload di pixel D3D a 48 KiB. `internal.road-vehicles.make-basic` è `INTERNAL` e non raggiungibile. `weather.set` e `weather.metar.set` sono `EXPERIMENTAL`. `timetable.logs.read` non è limitato e può restituire `OL_E_RUNTIME_RESPONSE_TOO_LARGE` con orari di grandi dimensioni. `camera.lock` è `EXPERIMENTAL`; richiede un veicolo del giocatore ed è validato a runtime (`CAM01`), anche se la stringa `RuntimeValidation` del registro riporta ancora `STATICALLY_VALIDATED`. Gli handle (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) hanno validità limitata alla sessione. |
| Stabilità | Trasporto e contratto `STABLE_BETA`; la stabilità di ciascuna operazione segue `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`) con le eccezioni indicate sopra. |
| Esempio | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Scopo | Restituire il catalogo delle capability valutato per un'installazione: un `Capability(Name, Available, EvidenceState, Reason)` per ogni voce di `PublicCapabilityRegistry` che non è `InternalOnly`. `Name` è l'id di catalogo, `EvidenceState` l'evidenza di catalogo (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` viene calcolato dalla piattaforma, dal controllo di OMSI 2, dalla classificazione e dalle capability native di cui la voce ha bisogno (vedere [capability](capabilities.md#getcapabilitiesasync)). |
| Parametri | `installation.RootPath`: l'installazione sottoposta a probe (piattaforma, `Omsi.exe`). `ExpectedExecutableSha256` viene ignorato. |
| Valore restituito | Una voce per ogni voce di catalogo non interna, per esempio `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` sul layout noto), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relazione con `PublicCapabilityRegistry` | Derivato da esso: il registro dichiara le capability, le loro operazioni e le loro evidenze; questa chiamata le valuta per una singola installazione. |
| Eccezioni generate | `OperationCanceledException` all'ingresso; `ArgumentException` per un percorso radice vuoto. |
| Annullamento | Verificato una volta all'ingresso. |
| Sessione in esecuzione richiesta | No. |
| Modifica OMSI / file system / transazione | No / No / Nessuna. |
| Stabilità | Contratto di chiamata `STABLE_BETA`; il contenuto dell'elenco è un inventario mantenuto manualmente: `PARTIAL`. |
| Esempio | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Scopo | Enumerare i contenuti installati e restituire identità canoniche utilizzabili in una `LaunchSpec`. L'individuazione salta i reparse point (i cicli di junction non possono bloccarla), legge i file di OMSI come Windows-1252 (rispettando UTF-8/UTF-16 contrassegnati da BOM) e non segue mai i collegamenti simbolici. |
| Parametri | `query` e `scope` secondo la tabella seguente. `scope` è obbligatorio per `Entrypoints` (identità della mappa), `Repaints`, `FleetNumbers`, `Registrations` (identità del veicolo). |
| Valore restituito | Elenco ordinato di `ContentIdentity(Identity, Kind, DisplayName)`. Le identità sono percorsi relativi all'installazione con barre rovesciate; i confronti non fanno distinzione tra maiuscole e minuscole. |
| Eccezioni generate | `OperationCanceledException` all'ingresso; `ArgumentException` quando si interroga `Entrypoints` senza scope oppure la radice è vuota; `FileNotFoundException` (nessun codice `OL_E_`; la CLI lo mappa su `OL_E_NOT_FOUND`) quando la mappa o il veicolo indicati come scope non sono installati. Una radice o una directory dei contenuti mancante produce un elenco vuoto, non un errore. |
| Annullamento | Verificato una volta all'ingresso. |
| Sessione in esecuzione richiesta | No. |
| Modifica OMSI / file system / transazione | No / No / Nessuna. |
| Stabilità | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (ogni piano validato a runtime viene risolto tramite essi). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (solo evidenza statica). |
| Esempio | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Valori di `ContentQueryKind` e risultati:

| Valore | Scope | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | nessuno | `maps\<dir>\global.cfg` | `map` | nome della directory della mappa |
| `Situations` | nessuno | `situations\...\<file>.osn` | `situation` | identità della mappa referenziata dal `.osn` (può essere `null`) |
| `Vehicles` | nessuno | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` o nome del file |
| `Repaints` | identità del veicolo (obbligatoria; senza di essa: elenco vuoto) | `<cti path>#item:<ordinal>` | `repaint` | nome `[item]` |
| `Hofs` | nessuno | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | identità del veicolo (obbligatoria; senza di essa: elenco vuoto) | percorso sorgente `[number]` relativo al veicolo | `fleet-number` | `null` |
| `Registrations` | identità del veicolo (obbligatoria; senza di essa: elenco vuoto) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | prima riga di valore (`null` per free) |
| `Addons` | nessuno | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | identità della mappa (obbligatoria; senza di essa: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | etichetta del punto di ingresso |
| `SessionProfiles` | nessuno | nome della directory del pacchetto (l'id di `/predefined-profile`) | `session-profile`, oppure `session-profile-invalid` quando il pacchetto non compila | `name` del profilo, oppure il codice `OL_E_SESSION_PROFILE_*` di un pacchetto non valido |
| `RuntimeModules` | nessuno | id del modulo | `runtime-module`, oppure `runtime-module-invalid` quando non può essere caricato | `name` del modulo, oppure il codice `OL_E_MODULE_*` |

Un punto di ingresso viene selezionato tramite `PresentedEntrypointIndex` oppure tramite `EntrypointIdentity` (un'identità di individuazione o un'etichetta esatta). Il pianificatore risolve un'identità nell'etichetta che OMSI presenta (OMSI presenta solo il primo record di ciascuna etichetta esatta) e il piano la elenca sotto `ResolvedContent` con il tipo `entrypoint`; `world.entrypoint-identity` è `EXPERIMENTAL` finché non è validata a runtime.

### `ListSessionProfilesAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Scopo | Elencare ogni pacchetto di profilo di sessione sotto `<root>\.omsilaunch\session-profiles\` con i suoi preset, in modo che un frontend possa offrirli senza analizzare YAML. CLI `/list:SessionProfiles`. |
| Valore restituito | Un `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` per ogni directory di pacchetto, ordinato per `Id`. I `Presets` sono `SessionProfilePresetSummary(Index, Id, Name)` in ordine di indice. Ogni pacchetto viene compilato con le stesse regole rigorose di un avvio (`SessionProfileCompiler`); un pacchetto che non compila viene elencato con `IsValid = false`, il suo codice `OL_E_SESSION_PROFILE_*` e il messaggio, e senza preset. Nessuna directory dei profili: elenco vuoto. |
| Eccezioni generate | `OperationCanceledException` all'ingresso. Gli errori di compilazione vengono riportati nel risultato, non generati. |
| Sessione in esecuzione richiesta | No. |
| Modifica OMSI / file system / transazione | No / No / Nessuna. |
| Stabilità | `STABLE_BETA` (gate offline `session-profiles.discovery`). |
| Esempio | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Scopo | Individuare i [moduli runtime](../../../reference/runtime-modules.md) installati sotto `plugins\OmsiLaunch.Modules` con la loro integrità e le loro impostazioni. Sola lettura. Il caricamento è aperto: nessun modulo viene controllato rispetto a un elenco o a una firma. |
| Valore restituito | Un `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` per ogni directory di modulo, ordinato per nome di directory. `Activation` è `requested`, `content` o `default`; `IsValid` è false con un codice `OL_E_MODULE_*` quando il modulo non può essere caricato; `Settings` elenca le impostazioni che il modulo dichiara come `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Eccezioni generate | `OperationCanceledException` all'ingresso. |
| Sessione in esecuzione richiesta | No. CLI: `/list:RuntimeModules` (envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspetto | Dettaglio |
| --- | --- |
| Firma | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Scopo | Segnalare o completare una transazione durevole obsoleta (`<root>\.omsilaunch\journal.json`) lasciata da un owner terminato in modo anomalo. Acquisisce il lease dell'installazione per la durata della chiamata, così da non eseguire mai un ripristino sotto una sessione in fase di avvio. Capability pubblica `session.recover`; CLI `/recovery-status` e `/recover`. |
| Parametri | `installation.RootPath`: la radice dell'installazione (normalizzata con `Path.GetFullPath`). `restore`: `false` = solo segnalazione; `true` = ripristinare, verificare, eliminare il journal e i backup. |
| Valore restituito | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = esisteva un journal all'inizio della chiamata; `Recovered` = è stato richiesto un ripristino, è stato eseguito e non rimane alcun journal; `Diagnostics` = note di ripristino (`restore.session-artifact-removed` con `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), vuoto quando non è stato ripristinato nulla. |
| Eccezioni generate | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` quando il lease è detenuto; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` quando PID + ora di creazione + percorso dell'eseguibile del journal corrispondono ancora a un processo attivo oppure (journal oltre `HandoffCreated` senza PID) è in esecuzione un qualsiasi `Omsi.exe` di quella radice; `IOException` con `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` oppure un messaggio di verifica ("Restore hash mismatch: ...", "Restore presence mismatch: ..."); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` per un journal corrotto; `ArgumentException` per una radice vuota; `OperationCanceledException`. Ogni volta che genera un'eccezione dopo l'inizio di un ripristino, il journal viene conservato e la chiamata successiva ripete l'operazione in modo idempotente. |
| Annullamento | Passato alle scritture di journal e backup; l'annullamento a metà ripristino lascia il journal in sospeso. |
| Sessione in esecuzione richiesta | No (rifiuta l'operazione mentre un owner è attivo). |
| Modifica lo stato di OMSI | No. |
| Modifica il file system | Solo con `restore == true`: riscrive gli originali a partire da backup verificati (byte, ora dell'ultima scrittura, ora di creazione, attributi; originali di sola lettura gestiti; write-through + flush; nessun `*.omsilaunch.tmp` residuo), rimuove gli artefatti di sessione, elimina `journal.json` e `backup\<sessionId>`. |
| Transazione / ripristino | Completa la transazione in sospeso (`Restoring` → `Restored` → journal rimosso). |
| Stabilità | `STABLE_BETA`: percorso di segnalazione e recupero dopo uscita anticipata (matrice RV-008), recupero dopo un ripristino fallito (chiusura di runtime `F01`), rifiuto con un owner attivo e nella finestra precedente al PID (`S04`) e recupero differito precedente all'impronta all'avvio (`S05`); vedere [stato della validazione a runtime](../status/runtime-validation-status.md). |
| Esempio | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Tipi del contratto

<a id="optional-values-and-semantic-primitives"></a>
### Valori facoltativi e primitive semantiche

| Tipo | Definizione | Note |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, `Unset` statico, `Set(T)` statico | Distingue «non richiesto» da «richiesto con un valore». La forma JSON è documentata nel [riferimento LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Enum di tipo byte. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Validato solo con `DateTimeMode.Explicit` (mese 1..12, giorno 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Validato solo con `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Famiglia LaunchSpec

Tutti i record seguenti sono documentati proprietà per proprietà nel [riferimento LaunchSpec](launchspec.md); questa tabella fissa l'inventario dei tipi.

| Tipo | Scopo | Stabilità |
| --- | --- | --- |
| `LaunchSpec` | Record radice della richiesta, con gli accessori `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures` che sostituiscono i valori predefiniti ai membri facoltativi `null`. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (pin facoltativo di `Omsi.exe`, controllato durante la pianificazione). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Selezione del mondo. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto di `LastMapState`; non significa mai «il .osn più recente»). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (calcolato da `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Qualsiasi modalità diversa da `Unset` rende il piano non eseguibile. | `PARTIAL` (evidenza del piano `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Qualsiasi modalità diversa da `Unset` rende il piano non eseguibile. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Qualsiasi campo impostato rende il piano non eseguibile. | `PARTIAL` |
| `EnvironmentSpec` | Otto gruppi `IReadOnlyDictionary<string, OptionalValue<string>>` di impostazioni semantiche di `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; qualsiasi valore impostato rende il piano non eseguibile. | `PARTIAL` |
| `DiagnosticsSpec` | Sei valori booleani; trasportati, non utilizzati. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Provenienza di un profilo di sessione compilato (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [Gestione HOF](hof-management.md) di sessione: `Operations` (elenco di `HofOperationSpec`). Campo `LaunchSpec.HofManagement`; accessor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Moduli runtime](../../../reference/runtime-modules.md) per la sessione: `Enable` (id dei moduli), `Disable` (id dei moduli tenuti fuori), `Settings` (`"<module id>.<key>"` verso valore); accessor `EffectiveDisable`, `EffectiveSettings`. Campo `LaunchSpec.Modules`; accessor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | La valutazione di un modulo nel piano: `Id`, `Version`, `Status` (`ELIGIBLE` o `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` o `default`), `Settings`; `IsEligible`, `EffectiveSettings`. Costanti `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Un'impostazione effettiva di un modulo: `Key`, `Value`, `Source` (`request`, `settings-file` o `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Vista di individuazione di un modulo installato; vedere `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Un'impostazione dichiarata da un modulo: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Un'operazione HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Generata dalla pianificazione HOF (`OmsiLaunch.Core`); `Code` è un codice `OL_E_HOF_*`. L'API pubblica la riporta come diagnostica del piano. | `STABLE_BETA` |
| `SessionProfileSummary` | Vista di individuazione di un pacchetto (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Vedere `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Un preset in un riepilogo (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (trasportato, il ripristino avviene sempre), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, predefinito 180), `ShutdownTimeoutSeconds` (trasportato, non utilizzato). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Tipi di piano e di stato

| Tipo | Campi | Note |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (nuovo `Guid` per ogni piano), `BuildProfileId` (`"Omsi23004"` oppure `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (elenco di `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (percorsi relativi distinti di `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` con `STATICALLY_VALIDATED` o `UNAVAILABLE`; capability native di avvio con `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (voci `Capability` per funzionalità richieste ma non supportate), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Un record pubblico: può essere modificato o diventare obsoleto, motivo per cui `StartSessionAsync` ripianifica. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, sistema operativo x64 e host x64), `LegacyPlatform` (sempre `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (tutti uguali a `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` è un valore `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), tranne in `SessionPlan.RequiredCapabilities`: un requisito di piattaforma o di contenuto è `STATICALLY_VALIDATED` o `UNAVAILABLE`, una capability nativa è `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Le modifiche di presentazione usano le chiavi `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (mappa di stringhe facoltativa). | I codici che iniziano con `OL_E_` sono errori, quelli con `OL_W_` avvisi, tutto il resto è informativo. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Le voci di diagnostica della sessione non includono quelle del piano. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (ora di ricezione da parte dell'host), `Sequence` (a partire da 1, per sessione), `Data`, `SessionTickMilliseconds` (il clock di sessione monotono del plugin al momento dell'emissione dell'evento; null per gli eventi dell'host). | Limitato ai 256 eventi più recenti (i più vecchi vengono scartati). Gli eventi del plugin arrivano in ordine attraverso l'event ring di telemetria; gli eventi persi per un overrun del ring vengono contati da `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Locale al processo. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Vedere `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Vedere `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Enum di tipo byte, nell'ordine di dichiarazione: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` ed `EnteringGameplay` non vengono mai impostati dal servizio attuale; `Snapshotting` è transitorio e in pratica non osservabile. Stati terminali: `Completed`, `Failed`. Semantica completa: [ciclo di vita della sessione](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Tipi del controllo runtime

| Tipo | Definizione | Stabilità |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Codec statico usato dall'host e dal plugin per l'envelope della mailbox: magic `0x4F4C5243` ("OLRC"), versione 1, intestazione little-endian di 72 byte (magic, versione, tipo 1 = richiesta / 2 = risposta, lunghezza totale, `Guid` della sessione, id di richiesta, lunghezza del payload, SHA-256 del payload) seguita da un payload JSON UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: pubblico perché entrambe le estremità del bridge lo condividono; non è una superficie di integrazione; il formato può cambiare con la versione del protocollo. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — ciò che l'host pubblica per il plugin nel file memory-mapped `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)`: un modulo pianificato trasportato dall'handoff (`StartupHandoff.Modules`). | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: l'orologio di avvio risolto di una sessione NEW_MAP trasportato dall'handoff (`StartupHandoff.Start`); un campo null mantiene il valore proprio di OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Codec: magic `0x4F4C5348`, versione 7 (legge da 3 a 7; la versione 5 aggiunge i moduli pianificati, la versione 6 le impostazioni effettive di ciascun modulo, la versione 7 la data/ora di avvio; i bit di flag 0x04 e 0x08 trasportano `PluginTrace` e `NativeTrace`), intestazione di 64 byte con integrità del payload tramite SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Event ring dal plugin all'host nel mapping `OMSILAUNCH_TELEMETRY_NAME`: magic `0x52544C4F` ("OLTR"), versione 1, 64 slot numerati di 2048 byte. `Initialize`, `Write`, `IsRing`, `ReadSince` (restituisce gli eventi successivi a un numero di sequenza e quanti ne sono andati persi). | `INTERNAL` |

Il plugin rifiuta un handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`) a meno che `WorldMode` sia `NewMap` o `SavedSituation`, `HeadlessStart` sia true, `PlayerVehicleEnabled` sia false, entrambe le modalità di data e ora siano `Unset` e una situazione salvata abbia un'identità non vuota. Il pianificatore impone gli stessi vincoli in precedenza, quindi un piano eseguibile non attiva mai questo rifiuto.

<a id="capability-registry-types"></a>
### Tipi del registro delle capability

| Tipo | Scopo |
| --- | --- |
| `PublicCapabilityRegistry` | Il catalogo canonico delle capability. `ProtocolVersion` (`"0.1"`), `All` (ogni `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (i 48 id di operazione concreti che un frontend può inoltrare), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (route di comando verso id di operazione), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (restituisce `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Fatto rispettare da `ExecuteRuntimeAsync`, dalla CLI, dal piano di controllo locale e dal plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (un valore `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; derivati `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (accetta `cursor`/`limit`, vedere [paginazione](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Ciò che un'operazione runtime modifica: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | Il vocabolario delle evidenze: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Catalogo completo: [capability](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Metodi di estensione `D3DRuntimeApi`

Wrapper tipizzati su `ExecuteRuntimeAsync` per le operazioni `d3d.*` (`EXPERIMENTAL`, capability `PublicExperimental` `d3d.texture`). Allocano gli id di richiesta da un contatore a livello di processo che parte da 30 000 e usano come timeout predefinito 5 s (tranne `GetD3DStatusAsync`, che ne richiede uno esplicito).

| Metodo | Operazione | Argomenti e limiti |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | nessuno |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | width/height 1..4096, levels 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | level 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, width/height 1..4096, pixel ≤ 48 KiB (codificati in Base64 nella trasmissione) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | un rilascio ripetuto viene rifiutato con `OL_E_D3D_RESOURCE_RELEASED` |

Tipi: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` con `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Errori: un risultato fallito viene rilanciato come `OmsiRuntimeException(Code, detail)`, dove `Code` è l'`ErrorCode` del risultato (oppure `OL_E_RUNTIME_OPERATION_FAILED` se assente) e il messaggio è `"<code>: <Values["detail"]>"`; un risultato riuscito senza valori, oppure una stringa di stato del dispositivo sconosciuta, genera `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Tutto ciò che genera `ExecuteRuntimeAsync` si propaga invariato. La gestione del reset del dispositivo è validata a runtime: un reset porta il dispositivo attraverso `Resetting` di nuovo a `Ready` e invalida ogni texture attiva (`OL_E_D3D_STALE_RESOURCE_HANDLE`, chiusura di runtime `D01`); la voce di catalogo `d3d.texture` lo registra. La transizione `Lost` non può essere prodotta dall'esterno del prodotto ed è coperta solo offline.

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### Tipi del contratto di processo

| Tipo | Contenuto |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Usato solo dalla CLI ([codici di uscita](exit-codes.md)); l'API non termina mai il processo. |
| `PublicErrorCategory` | Costanti stringa usate negli envelope di errore della CLI e del controllo: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Una `const string` per ciascun codice (143: 142 errori `OL_E_` e 1 avviso `OL_W_`) e `All`, il catalogo `PublicErrorDescriptor(Code, Category)`. Categorie: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Riferimento: [codici di errore](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Proprietà `Code` più messaggio; generata solo da `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (identità dell'installazione e contenimento dei percorsi)

Stabilità: `STABLE_BETA` (funzioni pure, nessun I/O, nessuno stato di OMSI, nessuna modifica del file system, nessuna partecipazione alla transazione, nessuna sessione in stato Running richiesta). È la definizione unica usata dal lease dell'installazione, dal nome della pipe del controllo locale, dal confinamento degli asset dei profili di sessione, dalla validazione delle destinazioni di Internet Textures (texture da Internet) e dal percorso del modello per lo spawn a runtime.

| Membro | Comportamento |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` senza separatori finali, tranne per la radice di un'unità (`C:\`), che viene mantenuta. Risolve i segmenti `.` e `..`, tratta `/` e `\` allo stesso modo e comprime i separatori ripetuti. **Non** risolve junction né collegamenti simbolici. Genera `ArgumentException` per una radice null o vuota. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` convertito in maiuscolo. Grafie lessicalmente equivalenti della stessa radice (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) condividono un'unica chiave; radici diverse (`C:\OMSI-A`, `C:\OMSI-B`) mai. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Risolve `candidate` (relativo a `root`, oppure assoluto) e restituisce `true` solo quando si trova strettamente al di sotto di `root`; `relativePath` è la grafia canonica con `\`. Usa i segmenti di `Path.GetRelativePath`, quindi un percorso adiacente come `C:\OMSI-A\x` non è mai all'interno di `C:\OMSI`; la radice stessa, altri volumi e le fughe tramite `..` restituiscono `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Divide su `/` e `\`, scartando i segmenti vuoti. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Profili di sessione (`OmsiLaunch.Core`)

Stabilità: `EXPERIMENTAL`. Questi tipi compilano un [profilo di sessione](session-profiles.md) YAML (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, schema `omsilaunch.session-profile/v1`) in una `LaunchSpec`. L'opzione CLI `/predefined-profile:<id> /predefined-profile-index:<n>` usa esattamente queste chiamate; un integratore può usarle per avviare un profilo tramite l'API.

| Membro | Comportamento |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Legge e valida il pacchetto. `id` deve essere un semplice nome di directory (altrimenti `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` è compreso in 1..99 (`MinPresetIndex`..`MaxPresetIndex`; altrimenti `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). File mancante: `OL_E_SESSION_PROFILE_NOT_FOUND`; dimensione superiore a `MaxBytes` (256 KiB), YAML non valido, ancore, chiavi sconosciute oppure un `id` diverso dal nome della directory: `OL_E_SESSION_PROFILE_INVALID`; altro `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. I percorsi degli asset sono confinati al pacchetto (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Ogni errore è una `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Restituisce `baseline` con il profilo applicato: per `NewMap` il blocco `new` del profilo (mappa, punto di ingresso ed eventuali data/ora/anno/meteo); le `settings` del preset unite sopra `Environment.General`; `Presentation`, `InternetTextures` e `Behavior` del preset, se presenti; e `SessionProfile` = `profile.Metadata`. Per `NewMap` verifica inoltre la mappa rispetto all'elenco `compatibility` del profilo (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | Il controllo di compatibilità per le altre modalità (per `SavedSituation` la mappa viene letta dal `.osn`). La CLI lo chiama dopo aver costruito la `WorldSpec` definitiva. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` e le chiavi accettate per ciascuna mappatura YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | Il pacchetto caricato; `Preset` è soltanto il preset selezionato. |
| `SessionProfileException(string code, string message)` | `IOException` con `Code` (uno dei codici `OL_E_SESSION_PROFILE_*`); il messaggio è `"<code>: <message>"`. |

La CLI rifiuta inoltre i flag della riga di comando in conflitto con il profilo (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); questo controllo non fa parte del compilatore. Vedere [profili di sessione](session-profiles.md#precedence-and-override-conflicts) per l'ordine di unione completo.

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### Tipi di piattaforma (`OmsiLaunch.Process`)

| Tipo | Stabilità | Uso |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` come tipo del parametro del costruttore di `OmsiLaunchService` | Rileva la piattaforma, verifica la scrivibilità, avvia, osserva, termina e attende `Omsi.exe`. Passare `new CurrentWindowsX64Platform()`; implementarla in proprio non è supportato. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | L'unica implementazione: host Windows x64, `CreateProcessW` per `Omsi.exe`, `TerminateProcess` per l'arresto canonico. I suoi metodi vengono chiamati dal servizio; gli integratori si limitano a costruirla. Membri (condivisi con `IRuntimePlatform`): `Detect(root)` restituisce il `RuntimePlatformInfo` del piano; `ValidateCurrent(info)` genera `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` quando l'host non può eseguire una sessione; `IsInstallationWritable(root)` è alla base di `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` crea `Omsi.exe` e registra l'identità del processo (PID, ora di creazione, percorso e hash calcolato dal servizio; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` e `Terminate` lo osservano e lo terminano. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Pubblici nell'assembly perché il servizio e i test li condividono. Non sono una superficie di integrazione; `LaunchedProcess` incapsula internamente gli handle del processo e del thread di OMSI (non sono membri pubblici) e non viene mai restituito da `IOmsiLaunch`. |

<a id="thread-safety"></a>
## Sicurezza rispetto ai thread

- `OmsiLaunchService` è sicuro per chiamate concorrenti su sessioni diverse: le sessioni risiedono in un `ConcurrentDictionary` e ogni modifica relativa a una sessione avviene sotto il lock privato di quella sessione.
- Le chiamate concorrenti sulla stessa sessione sono sicure ma serializzate dove necessario: `ExecuteRuntimeAsync` acquisisce un gate per sessione, quindi un secondo comando attende il primo (il suo timeout parte quando viene depositato nella mailbox).
- Il supervisore viene eseguito su un task del thread pool (`Task.Run`) dal momento in cui `StartSessionAsync` ritorna finché la sessione non è terminale; interroga la telemetria e il processo ogni 100 ms. I chiamanti non eseguono mai codice del supervisore.
- `StopAsync` e `GetStatusAsync` si completano in modo sincrono e possono essere chiamati da qualsiasi thread, anche all'interno di un handler `ProcessExit` (la CLI lo fa con un margine di 4 s).
- Nessuna chiamata dell'API è legata a un thread specifico; nessuna richiede un contesto di sincronizzazione.

<a id="what-is-not-in-the-api"></a>
## Ciò che non fa parte dell'API

- Nessun `IntPtr`, `nint`, handle Win32, indirizzo nativo, puntatore VMT o oggetto processo. I valori dei risultati le cui chiavi iniziano con `internal_` o terminano con `_address`, `_pointer`, `_vmt` vengono rimossi prima che un risultato esca da `ExecuteRuntimeAsync`.
- Nessuna operazione runtime `internal.*`: `internal.road-vehicles.make-basic` è `InternalOnly` nel registro e restituisce `OL_E_RUNTIME_OPERATION_UNKNOWN` dall'API e dalla CLI.
- Nessuna lettura o scrittura diretta della memoria di OMSI, nessun accesso a livello di file all'installazione al di fuori di quanto dichiarato da una `LaunchSpec`.
- Nessun handle tra processi: il [piano di controllo locale](local-control.md) è l'unica via tra processi e accetta solo `session.status`, `session.events`, `session.stop` e `runtime.execute`.
- Nessuna applicazione del veicolo del giocatore all'avvio. La chiusura cooperativa, `LAST_MAP_STATE`, la data/ora/meteo di avvio e gli overlay di documenti di tastiera/controller sono `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Riepilogo della stabilità

| Superficie | Stabilità |
| --- | --- |
| Costruttore di `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Trasporto di `ExecuteRuntimeAsync`; operazioni `PublicStableBeta` | `STABLE_BETA` |
| Operazioni `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| membri della specifica per data/ora/meteo/input, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| membri della specifica per il veicolo del giocatore | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implementazioni di `IRuntimePlatform`, tutti gli assembly di implementazione | `INTERNAL` |
| `WorldMode.LastSituation`, operazioni `internal.*` | `UNAVAILABLE` |
