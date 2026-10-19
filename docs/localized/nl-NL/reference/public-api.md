# Referentie van de openbare API (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=03d2b00af2df06cdc28d5fbbec294f9f480584c4817b1dfe8b805a914621f734 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/public-api.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina is de normatieve referentie voor de beheerde openbare API van OmsiLaunch 0.1.0-beta4: de assembly `OmsiLaunch.Api` (contracten) en het instappunt voor integrators `OmsiLaunchService` in `OmsiLaunch.Core`. Ze documenteert alleen wat de huidige code doet. Alles wat een integrator kan aanroepen, ontvangen of waarnemen, staat hier vermeld met het bijbehorende stabiliteitsniveau; wat hier niet staat, is geen integratieoppervlak.

De gegenereerde [inventaris van de openbare API](public-api-inventory.md) vermeldt elk openbaar type en elk openbaar lid van `OmsiLaunch.Api`, `OmsiLaunch.Core` en `OmsiLaunch.Process` met signatuur en stabiliteit; een documentatiegate faalt wanneer de inventaris en de assembly's van elkaar verschillen. Deze pagina legt de semantiek uit.

Gerelateerde pagina's: [LaunchSpec-referentie](launchspec.md), [foutcodes](errors.md), [levenscyclus van de sessie](../concepts/session-lifecycle.md), [transacties en recovery](../concepts/transactions-and-recovery.md), [runtimebesturing](runtime-control.md), [capabilities](capabilities.md), [lokaal control plane](local-control.md), [exitcodes](exit-codes.md), [status van de runtimevalidatie](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Stabiliteitsbegrippen

| Niveau | Betekenis op deze pagina |
| --- | --- |
| `STABLE_BETA` | Het contract is bevroren voor de 0.1-protocollijn en het pad is runtime-gevalideerd in `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Aanroepbaar en getest, maar het contract of het runtimebewijs kan nog veranderen voordat het stabiel wordt. |
| `PARTIAL` | Aanwezig in het contract; slechts een deel van het gedrag is geïmplementeerd of gevalideerd (de tekst vermeldt welk deel). |
| `INTERNAL` | Om technische redenen openbaar in de assembly (de brug deelt het type), maar geen integratieoppervlak; kan zonder aankondiging veranderen. |
| `UNAVAILABLE` | Aanwezig in het contract, maar door de huidige build geweigerd. |

<a id="assembly-overview"></a>
## Overzicht van de assembly's

| Assembly | Rol voor integrators |
| --- | --- |
| `OmsiLaunch.Api` | Zuivere contracten: records, enums, `IOmsiLaunch`, capabilityregister, foutcatalogus, wire-formaten, D3D-hulpfuncties. Bevat geen `IntPtr`, `nint`, Win32-handle, native adres of procesobject. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (de implementatie van `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` en `CurrentWindowsX64Platform` (de enige platformadapter), `InstallationLease`. Nodig om de service te construeren. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Implementatie-assembly's. Hun openbare types zijn `INTERNAL` voor integrators. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Instappunt: `OmsiLaunchService` en `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parameter | Geldige waarde | Ongeldig / standaard |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (naamruimte `OmsiLaunch.Process`). Detecteert het platform, maakt het OMSI-proces aan met `CreateProcessW`, wacht erop en beëindigt het. | Er wordt geen andere implementatie meegeleverd. Een eigen `IRuntimePlatform` is `INTERNAL`. |
| `PluginBuildDirectory` | Map met de referentiebestanden van de permanente plugin-closure: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` en elke `OmsiLaunch.*.dll` van de beheerde closure (moet `OmsiLaunch.Plugin.dll` bevatten). In een geïnstalleerd pakket is dit `<package>\plugins`. | Map of bestand ontbreekt: `PlanSessionAsync` retourneert een niet-uitvoerbaar plan met `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Pad van `OmsiLaunch.Native.x86.dll` (in het pakket: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Zoals hierboven. |
| `ReleaseManifestPath` | `release-manifest.json` naast `OmsiLaunch.exe`, indien aanwezig. Levert de verwachte SHA-256 van elk bestand in `plugins/` (`plugin.integrity.reference = manifest`). | `null` (ontwikkelindeling): de geïnstalleerde bestanden worden alleen gecontroleerd op aanwezigheid en op interne consistentie ten opzichte van de referentie-closure (`plugin.integrity.reference = self`). Misvormd manifest: `OL_E_RELEASE_MANIFEST_INVALID`. |

De service leest deze paden bij elke `PlanSessionAsync` en `StartSessionAsync`; ze kopieert, plaatst of verwijdert nooit pluginbestanden (zie [permanente plugin](../concepts/permanent-plugin.md)). De CLI construeert de service precies zo (`tools/OmsiLaunch.Cli/Program.cs`):

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

Maak één service per proces aan en deel die. Stabiliteit: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Regels voor sessie-eigendom

| Regel | Toelichting |
| --- | --- |
| Eén eigenaar per installatie | `StartSessionAsync` verkrijgt de installatielease, een benoemde semafoor `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, en houdt die vast totdat de supervisor de installatie heeft hersteld. Een tweede start op dezelfde root vanuit een willekeurig proces van dezelfde aanmeldsessie mislukt met `OL_E_INSTALLATION_BUSY` (gemeld als een sessie in `Failed`, zie `StartSessionAsync`). De lease geldt per aanmeldsessie, niet over aanmeldsessies heen, en wordt niet vrijgegeven zolang een ander proces er een handle naar vasthoudt (geaccepteerd risico). |
| Handles zijn proceslokaal | `SessionHandle` omhult de `Guid` van de sessie. Hij heeft alleen betekenis voor de instantie van `OmsiLaunchService` die hem heeft geretourneerd. Een handle die in een ander proces (of een andere service-instantie) uit een bekende `Guid` wordt opgebouwd, levert `KeyNotFoundException` op. Besturing over procesgrenzen heen verloopt via het [lokale control plane](local-control.md), niet via handles. |
| Roep altijd `CloseAsync` aan | Vanaf `StartSessionAsync` bezit het proces een duurzame transactie. `CloseAsync` vraagt zo nodig de canonieke stop aan, wacht op de supervisor (procesbeëindiging, exact herstel, vrijgave van de lease) en vergeet de sessie. Het moet op elk afsluitpad worden aangeroepen, ook na een toestand `Failed`. Zonder deze aanroep blijft de sessievermelding in het geheugen; het herstel zelf voert de supervisor hoe dan ook uit. |
| Mislukte sessies zijn nog steeds sessies | Een start die mislukt nadat `StartSessionAsync` is teruggekeerd, meldt `SessionState.Failed`; de handle blijft geldig voor `GetStatusAsync`/`WaitForAsync` tot `CloseAsync`. |
| Plannen worden opnieuw gecontroleerd | `StartSessionAsync` berekent de hash van `Omsi.exe` opnieuw en plant de spec opnieuw; een plan dat niet meer uitvoerbaar is, wordt geweigerd met `OL_E_PLAN_NOT_RUNNABLE`. |

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

Gemeenschappelijke feiten voor elke methode:

- Onbekende of al gesloten handles werpen `KeyNotFoundException` ("Unknown OmsiLaunch session.").
- Geen enkele methode vereist een actieve sessie (Running), behalve `ExecuteRuntimeAsync`.
- Uitzonderingen die een OmsiLaunch-code dragen, plaatsen die code aan het begin van `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). De CLI haalt codes op dezelfde manier uit berichten (`CliProgram.Classify`).
- Ondersteunde installatie: OMSI 2. Native capabilities worden per build in het proces opgelost en geverifieerd; een capability die op een build niet beschikbaar is, mislukt met `OL_E_CAPABILITY_UNRESOLVED`. Zie [compatibiliteit](compatibility.md).

<a id="complete-minimal-example"></a>
### Volledig minimaal voorbeeld

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

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Doel | Een `LaunchSpec` compileren tot een `SessionPlan` zonder OMSI te starten: de spec valideren, het platform detecteren, de vingerafdruk van `Omsi.exe` bepalen, contentidentiteiten omzetten, geplande bestandswijzigingen berekenen, vereiste en niet-ondersteunde capabilities opsommen en `IsRunnable` bepalen. Openbare capability `session.plan`. |
| Parameters | `spec`: een volledig ingevulde `LaunchSpec` (zie [LaunchSpec-referentie](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (alle acht woordenboeken) en `Behavior` mogen niet null zijn; de optionele leden mogen `null` zijn. `RootPath` hoort een absolute map te zijn; een lege root wordt vastgelegd als `OL_E_INSTALLATION_NOT_FOUND`, maar de platformcontrole op een leeg pad werpt `ArgumentException` voordat het plan wordt geretourneerd, dus geef nooit een lege root door. |
| Retourneert | `SessionPlan` met een nieuwe `SessionId`, `BuildProfileId` = de bekende indeling (`"Omsi23004"`) of `"unrecognized"` voor een OMSI 2-build zonder bekende indeling, de invoer-`Spec`, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (doelpaden `plugins\OmsiLaunch.*` plus `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (één `PlannedModule` per aangevraagde of door content geactiveerde [runtimemodule](runtime-modules.md); accessor `EffectivePlannedModules`). `IsRunnable` is `true` precies wanneer geen enkele diagnosecode met `OL_E_` begint. Informatieve diagnosemeldingen (`plugin.integrity.reference` met bericht `self` of `manifest`, `session_profile.selected`) maken een plan nooit niet-uitvoerbaar. |
| Fouten in het resultaat | Elke planningsfout is een diagnosemelding, geen uitzondering: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (het bericht bevat de splash-/ITX-code), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (de geïnstalleerde plugin-closure in `plugins\` wordt tijdens het plannen tegen het releasemanifest gecontroleerd), `OL_E_RUNTIME_ARTIFACT_MISSING` (het bericht kan `OL_E_RELEASE_MANIFEST_INVALID` bevatten). Volledige voorwaarden: [validatieregels van LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Geworpen | `OperationCanceledException` als het token bij binnenkomst al is geannuleerd (het enige controlepunt); `ArgumentException`/`NotSupportedException` voor syntactisch ongeldige rootpaden; `NullReferenceException`/`ArgumentNullException` voor vereiste leden die null zijn; `System.Text.Json.JsonException` voor een syntactisch ongeldig releasemanifest. |
| Annulering | Eenmaal gecontroleerd bij binnenkomst. Daarna is plannen synchroon bestandssysteemwerk. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI-toestand | Nee. |
| Wijzigt het bestandssysteem | Nee (leest `Omsi.exe`, contentbestanden, plugin-closure, manifest). Instellingswaarden worden hier niet gevalideerd (alleen het bestaan en de beschrijfbaarheid van de sleutel); een ongeldige waarde mislukt bij de start met `OL_E_INVALID_SETTING_VALUE`. |
| Transactie / herstel | Geen. |
| Beperkingen | Het aanvragen van een andere modus dan `Unset` voor `Date`/`Time`/`Year`, een andere `Weather`-modus dan `Unset`, een willekeurig veld van `PlayerVehicle`, `Input`-documenten, `EntrypointIdentity` of `WorldMode.LastMapState` levert op deze build `OL_E_CAPABILITY_UNAVAILABLE` en een niet-uitvoerbaar plan op (vermeldingen in `UnsupportedRequestedFeatures` met hun catalogusbewijs, `UNAVAILABLE` of `PARTIAL`). |
| Stabiliteit | `STABLE_BETA`. |
| Voorbeeld | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Doel | Een transactionele, beheerde OMSI-sessie starten vanuit een uitvoerbaar plan: de installatielease verkrijgen, een verouderd journal herstellen, de permanente plugin-closure valideren, snapshots en overlays van sessiebestanden maken, de opstart-handoff, het telemetrieslot en de runtimemailbox aanmaken, `Omsi.exe` starten, het proces in het journal vastleggen en de sessie overdragen aan een supervisor op de achtergrond. Openbare capability `session.start`. |
| Parameters | `plan`: een `SessionPlan` met `IsRunnable == true`. De spec in het plan wordt opnieuw gepland; alleen `plan.SessionId` wordt uit het plan van de aanroeper overgenomen. `plan.Spec.Behavior.StartupTimeoutSeconds` moet 1..600 zijn. |
| Retourneert | `SessionHandle(plan.SessionId)` zodra `Omsi.exe` is aangemaakt en vastgelegd (toestand `WaitingForPlugin`), of zodra het startpad is mislukt (toestand `Failed`). Wacht niet op gameplay; gebruik `WaitForAsync(session, SessionState.Running, ...)`. |
| Geworpen | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` wanneer `plan.IsRunnable` false is; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` wanneer het opnieuw geplande plan niet uitvoerbaar is (bijvoorbeeld omdat `Omsi.exe` is gewijzigd, content is verwijderd of de plugin-closure ontbreekt); `InvalidOperationException("Duplicate session id.")` wanneer een sessie met dezelfde id nog geregistreerd is (roep eerst `CloseAsync` aan); `ArgumentOutOfRangeException` wanneer `StartupTimeoutSeconds` buiten 1..600 ligt; `OperationCanceledException` bij annulering vóór of tijdens het opnieuw plannen; plus alles wat `PlanSessionAsync` werpt. In al deze gevallen wordt geen sessie geregistreerd. |
| Fouten in het resultaat | Elke fout na het opnieuw plannen wordt binnen het startpad opgevangen: de sessie wordt geregistreerd, de toestand is `Failed` en de diagnosemeldingen bevatten `OL_E_START_SESSION`, met als bericht het interne bericht (dat met de interne code begint als die er is): `OL_E_INSTALLATION_BUSY` (lease bezet of een in het journal vastgelegd OMSI-proces leeft nog), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (alleen wanneer de uitgestelde nieuwe poging met de overlays van deze sessie het eigendom nog steeds niet kan aantonen), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. Het opruimen kan `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (beëindiging van OMSI niet bevestigd; journal behouden) of `OL_E_RESTORE_FAILED` toevoegen. Latere fouten meldt de supervisor (zie [levenscyclus van de sessie](../concepts/session-lifecycle.md)). |
| Annulering | Vóór/tijdens het opnieuw plannen: werpt een uitzondering. Daarna wordt het token doorgegeven aan de transactie en het aanmaken van het proces; een annulering op dat punt wordt behandeld als elke andere startfout (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), het proces (indien aangemaakt) wordt beëindigd en de installatie hersteld. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI-toestand | Ja: maakt het OMSI-proces aan met de omgevingsvariabelen `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Wijzigt het bestandssysteem | Ja, binnen de installatiemap: `.omsilaunch\diagnostics\<sessionId>-host.log` (bewaard: de 50 nieuwste sessies), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (eenmalig gekopieerd voor het beheerde opstartscherm), sessie-overlays (patches van `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), sessieverwijderingen (ITX-doelen, `Texture\standard.ipr`, `closecheck`) en het permanent verwijderen van een al bestaande, verouderde `closecheck` wanneer `SuppressStaleClosecheckWarning` true is (diagnosemelding `closecheck.stale-removed`). |
| Transactie / herstel | Opent de transactie (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Elk pad uit de sessie eindigt in herstel. Zie [transacties en recovery](../concepts/transactions-and-recovery.md). |
| Beperkingen | Alleen `WorldMode.NewMap` met `PresentedEntrypointIndex` en `WorldMode.SavedSituation` bereiken gameplay. `WorldMode.LastMapState` is `UNAVAILABLE`. Aanvragen voor het spelersvoertuig bereiken deze methode nooit, omdat ze bij het plannen al niet uitvoerbaar zijn. |
| Stabiliteit | `STABLE_BETA` (de levenscycli NEW_MAP en SAVED_SITUATION zijn runtime-gevalideerd). |
| Voorbeeld | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Doel | De semantische levenscyclustoestand, de tot nu toe verzamelde diagnosemeldingen en de begrensde lijst met runtime-events lezen. Openbare capability `session.status`. |
| Parameters | `session`: een handle die door `StartSessionAsync` is geretourneerd en nog niet is gesloten. |
| Retourneert | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: een onveranderlijke snapshot (arrays worden onder de sessielock gekopieerd). `RuntimeEvents` is voor een levende sessie nooit `null`. |
| Geworpen | `KeyNotFoundException` voor onbekende/gesloten handles. Werpt verder nooit een uitzondering. |
| Annulering | Het token wordt genegeerd (de aanroep wordt synchroon voltooid). |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI / bestandssysteem / transactie | Nee / Nee / Geen. |
| Stabiliteit | `STABLE_BETA`. |
| Voorbeeld | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Doel | Pollen (elke 100 ms) totdat de sessie in `state` of in een eindtoestand (`Completed`, `Failed`) staat, of de timeout verloopt; daarna de huidige status retourneren. |
| Parameters | `state`: elke `SessionState`. Wachten op een tussentoestand die al gepasseerd is (of nooit wordt ingesteld, zie [levenscyclus van de sessie](../concepts/session-lifecycle.md)) wacht tot een eindtoestand of de timeout. `timeout`: elke niet-negatieve `TimeSpan` of `Timeout.InfiniteTimeSpan`. |
| Retourneert | De status op het moment dat het wachten eindigde. Bij een timeout wordt de status geretourneerd, geen uitzondering: controleer `State` zelf. Een wachtactie op `Running` die in `Failed` eindigt, keert direct terug met de foutdiagnoses. |
| Geworpen | `KeyNotFoundException`; `OperationCanceledException` wanneer het token van de aanroeper wordt geannuleerd (alleen annulering door de aanroeper wordt doorgegeven; de interne timeout niet). |
| Annulering | Het token van de aanroeper wordt bij elke tik van 100 ms gerespecteerd. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI / bestandssysteem / transactie | Nee / Nee / Geen. |
| Stabiliteit | `STABLE_BETA`. |
| Voorbeeld | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Doel | De canonieke stop aanvragen. Zet de stopvlag en keert direct terug; de supervisor ziet de vlag binnen zijn lus van 100 ms, roept `TerminateProcess` aan op `Omsi.exe`, wacht tot het proces is beëindigd, markeert het journal als `ProcessExited`, herstelt elk bestand in bezit van de sessie en geeft de lease vrij. Dit is een geforceerde beëindiging: de eigen afsluitroutine van OMSI wordt niet uitgevoerd en OMSI herschrijft `options.cfg` niet bij het afsluiten (bewust, beschermt de transactie). Coöperatief afsluiten via `WM_CLOSE` is niet geïmplementeerd (productbeslissing; bij de runtime-afsluitronde sloot OMSI niet binnen 30 s na `WM_CLOSE`, `L05b`). Openbare capability `session.stop`. |
| Parameters | `session`. |
| Retourneert | Een voltooide taak; wacht niet op beëindiging of herstel. Gebruik `WaitForAsync(session, SessionState.Completed, ...)` om de voltooiing waar te nemen. |
| Geworpen | `KeyNotFoundException`. |
| Annulering | Token genegeerd. |
| Actieve sessie vereist | Nee. Idempotent; een stop die wordt aangevraagd voordat de supervisor start, wordt uitgevoerd zodra die start; een stop op een sessie in een eindtoestand doet niets. |
| Wijzigt OMSI-toestand | Ja: beëindigt het OMSI-proces (exitcode 1). |
| Wijzigt het bestandssysteem | Indirect: activeert herstel, verwijdering van het journal en verwijdering van back-ups door de supervisor. |
| Transactie / herstel | Activeert `ProcessExited` → `Restoring` → `Restored`. Wijzigingen aan de runtimekant via `ExecuteRuntimeAsync` (geschreven kloktijden, gespawnde voertuigen, scriptvariabelen, D3D-textures) worden niet hersteld; ze verdwijnen met het proces. |
| Stabiliteit | `STABLE_BETA`. |
| Voorbeeld | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Doel | De handle van de consument vrijgeven zonder de transactie achter te laten: als de sessie niet in een eindtoestand is, de canonieke stop aanvragen; daarna wachten op de levenscyclustaak van de supervisor (procesbeëindiging, herstel, vrijgave van de lease); daarna de sessie vergeten. |
| Parameters | `session`. |
| Retourneert | Voltooit wanneer de sessie in een eindtoestand is en verwijderd is. Daarna is de handle onbekend (`KeyNotFoundException` bij elke verdere aanroep, ook een tweede `CloseAsync`). |
| Geworpen | `KeyNotFoundException`; `OperationCanceledException` als de aanroeper annuleert tijdens het wachten op de supervisor. In dat geval wordt de sessie niet verwijderd en blijft de supervisor draaien; roep `CloseAsync` opnieuw aan. |
| Annulering | Geldt alleen voor het wachten; annuleert nooit het herstel. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI-toestand | Ja, als de sessie nog leeft (zoals `StopAsync`). |
| Wijzigt het bestandssysteem | Indirect (herstel door de supervisor). |
| Transactie / herstel | Garandeert dat de transactie wordt voltooid voordat de handle wordt vrijgegeven (wanneer de supervisor is gestart). Voor een sessie die mislukte voordat de supervisor startte, heeft het startpad al hersteld of `OL_E_RESTORE_DEFERRED` gemeld. |
| Stabiliteit | `STABLE_BETA`. |
| Voorbeeld | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Doel | Eén openbare runtime-operatie uitvoeren binnen het actieve OMSI-proces via de single-flight-mailbox van de sessie (memory-mapped, 64 KiB, verzoek gebonden aan sessie-id en verzoek-id). De plugin voert de operatie uit op de UI-thread van OMSI. Catalogus van operaties: [runtimebesturing](runtime-control.md) en [capabilities](capabilities.md). |
| Parameters | `command.SessionId` moet gelijk zijn aan `session.SessionId`. `command.RequestId`: een door de aanroeper gekozen `ulong`; gebruik een strikt oplopende teller per proces (de D3D-hulpfuncties beginnen bij 30 000, de CLI als eigenaar bij 10 001/50 000). `command.Operation`: een openbare operatie-id uit `PublicCapabilityRegistry.PublicRuntimeOperationIds` (bijvoorbeeld `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: tekenreekswaarden met ordinale namen als sleutel; de vereiste namen per operatie komen uit `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: gemeten vanaf het moment dat het verzoek in de mailbox wordt klaargezet (wachten achter een ander lopende opdracht telt niet mee). De CLI gebruikt als eigenaar 5 s (15 s voor `road-vehicles.spawn`) en als client 8 s / 30 s. |
| Volgorde van controles | 1. Registervalidatie (vóór het opzoeken van de sessie): onbekende of `internal.*`-operatie → resultaat `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; ontbrekend vereist argument (afwezig of alleen witruimte) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Sessie opzoeken → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Toestand niet `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Mailboxverzoek. 6. Resultaatwaarden waarvan de sleutel met `internal_` begint of op `_address`, `_pointer`, `_vmt` eindigt, worden verwijderd. |
| Retourneert | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. Bij succes bevat `Values` de semantische tekenreeksen van de operatie (per operatie gedocumenteerd in [runtimebesturing](runtime-control.md)). |
| Fouten in het resultaat | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (register); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (het pluginresultaat overschreed de mailbox; resultaten van begrensde lijsten worden in plaats daarvan ingekort met `truncated=true`); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` met een afgeleide sleutel); elke code `OL_E_D3D_*` (met `Values["detail"]` en `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (de native capability van de operatie is op de draaiende build niet opgelost); en elke getypeerde fout aan de pluginkant met zijn eigen code in `ErrorCode` en het bericht in `Values["detail"]` (Beta 4; alleen een fout zonder gecatalogiseerde code is `OL_E_RUNTIME_OPERATION_FAILED`). Codes die zo binnenkomen: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (controles aan de pluginkant), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Zie [foutcodes](errors.md). |
| Geworpen | `KeyNotFoundException`; `InvalidOperationException` met `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (mailbox al door de supervisor vrijgegeven), `OL_E_RUNTIME_CHANNEL_BUSY` (het slot bevat nog een verlaten verzoek), `OL_E_RUNTIME_REQUEST_ID_REUSED` (er staat nog een verouderd antwoord voor dezelfde verzoek-id in het slot); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (beschadigd, vreemd of niet-overeenkomend antwoord); `ArgumentOutOfRangeException` wanneer het geserialiseerde verzoek groter is dan de mailbox; `OperationCanceledException`. |
| Annulering | Gerespecteerd tijdens het wachten op de gate per sessie en elke 20 ms tijdens het pollen op het antwoord. Annuleren terwijl een opdracht loopt, zet het slot niet terug: de volgende aanroep op die sessie kan mislukken met `OL_E_RUNTIME_CHANNEL_BUSY` totdat de plugin zijn antwoord publiceert (dat dan als verouderd wordt verworpen). Gebruik liever de timeout; een timeout zet het slot terug en een laat antwoord wordt gedetecteerd en verworpen. |
| Actieve sessie vereist | Ja (`SessionState.Running`); anders wordt `OL_E_SESSION_NOT_RUNNING` geworpen. De mailbox bestaat totdat de supervisor die tijdens het herstel vrijgeeft. |
| Wijzigt OMSI-toestand | Hangt af van de operatie: `Read`-operaties niet; `Write`/`Action`-operaties (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) wijzigen toestand in het proces die niet wordt hersteld. |
| Wijzigt het bestandssysteem | De host schrijft niets. OMSI kan als gevolg daarvan zijn eigen bestanden schrijven (niet bijgehouden). |
| Transactie / herstel | Geen. |
| Beperkingen | Eén lopende opdracht per sessie (aanroepen op dezelfde sessie worden geserialiseerd). Verzoek en antwoord zijn elk beperkt tot 64 KiB min 8 bytes; D3D-pixelpayloads tot 48 KiB. `internal.road-vehicles.make-basic` is `INTERNAL` en onbereikbaar. `weather.set` en `weather.metar.set` zijn `EXPERIMENTAL`. `timetable.logs.read` is niet begrensd en kan bij grote dienstregelingen `OL_E_RUNTIME_RESPONSE_TOO_LARGE` retourneren. `camera.lock` is `EXPERIMENTAL`; het vereist een spelersvoertuig en is runtime-gevalideerd (`CAM01`), hoewel de `RuntimeValidation`-tekenreeks van het register nog steeds `STATICALLY_VALIDATED` luidt. Handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) gelden per sessie. |
| Stabiliteit | Transport en contract `STABLE_BETA`; de stabiliteit per operatie volgt `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`), met de hierboven genoemde uitzonderingen. |
| Voorbeeld | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Doel | De capabilitycatalogus retourneren, geëvalueerd voor een installatie: één `Capability(Name, Available, EvidenceState, Reason)` per vermelding van `PublicCapabilityRegistry` die niet `InternalOnly` is. `Name` is de catalogus-id, `EvidenceState` het catalogusbewijs (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` wordt berekend uit het platform, de OMSI 2-controle, de classificatie en de native capabilities die de vermelding nodig heeft (zie [capabilities](capabilities.md#getcapabilitiesasync)). |
| Parameters | `installation.RootPath`: de onderzochte installatie (platform, `Omsi.exe`). `ExpectedExecutableSha256` wordt genegeerd. |
| Retourneert | Eén vermelding per niet-interne catalogusvermelding, bijvoorbeeld `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` op de bekende indeling), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relatie met `PublicCapabilityRegistry` | Ervan afgeleid: het register declareert de capabilities, hun operaties en hun bewijs; deze aanroep evalueert ze voor één installatie. |
| Geworpen | `OperationCanceledException` bij binnenkomst; `ArgumentException` voor een leeg rootpad. |
| Annulering | Eenmaal gecontroleerd bij binnenkomst. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI / bestandssysteem / transactie | Nee / Nee / Geen. |
| Stabiliteit | Aanroepcontract `STABLE_BETA`; de inhoud van de lijst is een handmatig bijgehouden inventaris: `PARTIAL`. |
| Voorbeeld | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Doel | Geïnstalleerde content opsommen en canonieke identiteiten retourneren die in een `LaunchSpec` bruikbaar zijn. De detectie slaat reparse points over (junctioncycli kunnen haar niet laten vastlopen), leest OMSI-bestanden als Windows-1252 (UTF-8/UTF-16 met BOM wordt gerespecteerd) en volgt nooit symbolische koppelingen. |
| Parameters | `query` en `scope` volgens de onderstaande tabel. `scope` is vereist voor `Entrypoints` (kaartidentiteit), `Repaints`, `FleetNumbers`, `Registrations` (voertuigidentiteit). |
| Retourneert | Gesorteerde lijst van `ContentIdentity(Identity, Kind, DisplayName)`. Identiteiten zijn paden relatief aan de installatie met backslashes; vergelijkingen zijn hoofdletterongevoelig. |
| Geworpen | `OperationCanceledException` bij binnenkomst; `ArgumentException` wanneer `Entrypoints` zonder scope wordt opgevraagd of de root leeg is; `FileNotFoundException` (geen `OL_E_`-code; de CLI zet dit om naar `OL_E_NOT_FOUND`) wanneer de kaart of het voertuig van de scope niet is geïnstalleerd. Een ontbrekende root of contentmap levert een lege lijst op, geen fout. |
| Annulering | Eenmaal gecontroleerd bij binnenkomst. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI / bestandssysteem / transactie | Nee / Nee / Geen. |
| Stabiliteit | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (elk runtime-gevalideerd plan wordt via deze typen omgezet). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (alleen statisch bewijs). |
| Voorbeeld | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Waarden van `ContentQueryKind` en resultaten:

| Waarde | Scope | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | geen | `maps\<dir>\global.cfg` | `map` | naam van de kaartmap |
| `Situations` | geen | `situations\...\<file>.osn` | `situation` | kaartidentiteit waarnaar de `.osn` verwijst (kan `null` zijn) |
| `Vehicles` | geen | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` of bestandsnaam |
| `Repaints` | voertuigidentiteit (vereist; zonder: lege lijst) | `<cti path>#item:<ordinal>` | `repaint` | naam uit `[item]` |
| `Hofs` | geen | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | voertuigidentiteit (vereist; zonder: lege lijst) | bronpad van `[number]` relatief aan het voertuig | `fleet-number` | `null` |
| `Registrations` | voertuigidentiteit (vereist; zonder: lege lijst) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | eerste waarderegel (`null` voor free) |
| `Addons` | geen | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | kaartidentiteit (vereist; zonder: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | label van het instappunt |
| `SessionProfiles` | geen | mapnaam van het pakket (de id voor `/predefined-profile`) | `session-profile`, of `session-profile-invalid` wanneer het pakket niet compileert | `name` van het profiel, of de code `OL_E_SESSION_PROFILE_*` van een ongeldig pakket |
| `RuntimeModules` | geen | module-id | `runtime-module`, of `runtime-module-invalid` wanneer het niet kan worden geladen | `name` van de module, of de code `OL_E_MODULE_*` |

Een instappunt wordt geselecteerd met `PresentedEntrypointIndex` of met `EntrypointIdentity` (een detectie-identiteit of een exact label). De planner zet een identiteit om naar het label dat OMSI toont (OMSI toont alleen het eerste record van elk exact label) en het plan vermeldt het onder `ResolvedContent` met soort `entrypoint`; `world.entrypoint-identity` is `EXPERIMENTAL` totdat het runtime-gevalideerd is.

### `ListSessionProfilesAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Doel | Elk sessieprofielpakket onder `<root>\.omsilaunch\session-profiles\` met zijn presets opsommen, zodat een frontend ze kan aanbieden zonder YAML te parseren. CLI `/list:SessionProfiles`. |
| Retourneert | Eén `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` per pakketmap, gesorteerd op `Id`. `Presets` zijn `SessionProfilePresetSummary(Index, Id, Name)` in volgorde van index. Elk pakket wordt gecompileerd met dezelfde strikte regels als bij een start (`SessionProfileCompiler`); een pakket dat niet compileert, wordt vermeld met `IsValid = false`, zijn code `OL_E_SESSION_PROFILE_*` en bericht, en zonder presets. Geen profielenmap: lege lijst. |
| Geworpen | `OperationCanceledException` bij binnenkomst. Compilatiefouten worden in het resultaat gemeld, niet geworpen. |
| Actieve sessie vereist | Nee. |
| Wijzigt OMSI / bestandssysteem / transactie | Nee / Nee / Geen. |
| Stabiliteit | `STABLE_BETA` (offline gate `session-profiles.discovery`). |
| Voorbeeld | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Doel | De [runtimemodules](runtime-modules.md) ontdekken die onder `plugins\OmsiLaunch.Modules` zijn geïnstalleerd, met hun integriteit en instellingen. Alleen lezen. Het laden is open: geen enkele module wordt gecontroleerd tegen een lijst of handtekening. |
| Retourneert | Eén `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` per modulemap, gesorteerd op mapnaam. `Activation` is `requested`, `content` of `default`; `IsValid` is false met een code `OL_E_MODULE_*` wanneer de module niet kan worden geladen; `Settings` somt de instellingen op die de module declareert als `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Geworpen | `OperationCanceledException` bij binnenkomst. |
| Actieve sessie vereist | Nee. CLI: `/list:RuntimeModules` (envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspect | Toelichting |
| --- | --- |
| Signatuur | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Doel | Een verouderde duurzame transactie (`<root>\.omsilaunch\journal.json`) die door een gecrashte eigenaar is achtergelaten, melden of voltooien. Neemt de installatielease voor de duur van de aanroep, zodat er nooit wordt hersteld onder een sessie die aan het starten is. Openbare capability `session.recover`; CLI `/recovery-status` en `/recover`. |
| Parameters | `installation.RootPath`: de installatiemap (genormaliseerd met `Path.GetFullPath`). `restore`: `false` = alleen melden; `true` = herstellen, verifiëren, het journal en de back-ups verwijderen. |
| Retourneert | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = er bestond een journal toen de aanroep begon; `Recovered` = er is om herstel gevraagd, het is uitgevoerd en er is geen journal meer; `Diagnostics` = herstelnotities (`restore.session-artifact-removed` met `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), leeg wanneer niets is hersteld. |
| Geworpen | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` wanneer de lease bezet is; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` wanneer PID + aanmaaktijd + pad van het uitvoerbare bestand uit het journal nog overeenkomen met een levend proces, of (journal voorbij `HandoffCreated` zonder PID) er een `Omsi.exe` uit die root draait; `IOException` met `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` of een verificatiebericht ("Restore hash mismatch: ...", "Restore presence mismatch: ..."); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` voor een beschadigd journal; `ArgumentException` voor een lege root; `OperationCanceledException`. Wanneer er een uitzondering wordt geworpen nadat een herstel is begonnen, blijft het journal behouden en speelt de volgende aanroep het idempotent opnieuw af. |
| Annulering | Doorgegeven aan het schrijven van journal/back-ups; annuleren midden in een herstel laat het journal openstaan. |
| Actieve sessie vereist | Nee (weigert zolang er een eigenaar actief is). |
| Wijzigt OMSI-toestand | Nee. |
| Wijzigt het bestandssysteem | Alleen met `restore == true`: herschrijft originelen vanuit geverifieerde back-ups (bytes, tijdstip van laatste schrijfactie, aanmaaktijd, kenmerken; alleen-lezen originelen worden afgehandeld; write-through + flush; er blijft geen `*.omsilaunch.tmp` achter), verwijdert sessieartefacten, verwijdert `journal.json` en `backup\<sessionId>`. |
| Transactie / herstel | Voltooit de openstaande transactie (`Restoring` → `Restored` → journal verwijderd). |
| Stabiliteit | `STABLE_BETA`: meldpad en recovery na vroegtijdige beëindiging (matrix RV-008), recovery na een mislukt herstel (runtime-afsluitronde `F01`), weigering onder een levende eigenaar en in het venster vóór de PID (`S04`) en uitgestelde recovery vóór de vingerafdruk bij de start (`S05`); zie [status van de runtimevalidatie](../status/runtime-validation-status.md). |
| Voorbeeld | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Contracttypen

<a id="optional-values-and-semantic-primitives"></a>
### Optionele waarden en semantische primitieven

| Type | Definitie | Opmerkingen |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, statisch `Unset`, statisch `Set(T)` | Onderscheidt "niet aangevraagd" van "aangevraagd met een waarde". De JSON-vorm staat beschreven in de [LaunchSpec-referentie](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Byte-enum. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Alleen gevalideerd bij `DateTimeMode.Explicit` (maand 1..12, dag 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Alleen gevalideerd bij `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### LaunchSpec-familie

Alle onderstaande records worden eigenschap voor eigenschap gedocumenteerd in de [LaunchSpec-referentie](launchspec.md); deze tabel legt de inventaris van typen vast.

| Type | Doel | Stabiliteit |
| --- | --- | --- |
| `LaunchSpec` | Hoofdrecord van het verzoek met de accessors `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, die standaardwaarden invullen voor optionele leden die `null` zijn. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (optionele pin van `Omsi.exe`, gecontroleerd bij het plannen). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Wereldselectie. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (verouderde alias van `LastMapState`; nooit "de nieuwste .osn"). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (berekend uit `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Elke andere modus dan `Unset` maakt het plan niet uitvoerbaar. | `PARTIAL` (planbewijs `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Elke andere modus dan `Unset` maakt het plan niet uitvoerbaar. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Elk ingesteld veld maakt het plan niet uitvoerbaar. | `PARTIAL` |
| `EnvironmentSpec` | Acht groepen `IReadOnlyDictionary<string, OptionalValue<string>>` met semantische instellingen van `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; elke ingestelde waarde maakt het plan niet uitvoerbaar. | `PARTIAL` |
| `DiagnosticsSpec` | Zes booleans; meegenomen, niet gebruikt. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Herkomst van een gecompileerd sessieprofiel (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [HOF-beheer](hof-management.md) voor de sessie: `Operations` (lijst van `HofOperationSpec`). Veld `LaunchSpec.HofManagement`; accessor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Runtimemodules](runtime-modules.md) voor de sessie: `Enable` (module-id's), `Disable` (module-id's die buiten blijven), `Settings` (`"<module id>.<key>"` naar waarde); accessors `EffectiveDisable`, `EffectiveSettings`. Veld `LaunchSpec.Modules`; accessor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | De planevaluatie van een module: `Id`, `Version`, `Status` (`ELIGIBLE` of `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` of `default`), `Settings`, `RuntimeOperations` (catalogusoperaties die de module in het proces uitvoert, elk met zijn mutatiebeleid wanneer ze OMSI wijzigt, bijvoorbeeld `camera.set (omsi-state)`), `TickMilliseconds`, `SnapshotSha256` (de digest van de bestanden van de module ten tijde van het plan, zie [runtimemodules](runtime-modules.md#integrity)); `IsEligible`, `EffectiveSettings`, `EffectiveRuntimeOperations`. Constanten `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Eén effectieve moduleinstelling: `Key`, `Value`, `Source` (`request`, `settings-file` of `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Detectieweergave van een geïnstalleerde module; zie `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Een instelling die een module declareert: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Eén HOF-bewerking (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Geworpen door de HOF-planning (`OmsiLaunch.Core`); `Code` is een code `OL_E_HOF_*`. De openbare API meldt haar als plandiagnosemeldingen. | `STABLE_BETA` |
| `SessionProfileSummary` | Detectieweergave van één pakket (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Zie `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Eén preset in een samenvatting (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (meegenomen, herstel vindt altijd plaats), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, standaard 180), `ShutdownTimeoutSeconds` (meegenomen, niet gebruikt). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Plan- en statustypen

| Type | Velden | Opmerkingen |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (nieuwe `Guid` per plan), `BuildProfileId` (`"Omsi23004"` of `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (lijst van `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (unieke relatieve paden van `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` met `STATICALLY_VALIDATED` of `UNAVAILABLE`; native startcapabilities met `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` of `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (`Capability`-vermeldingen voor aangevraagde maar niet-ondersteunde functies), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Een openbaar record: het kan worden bewerkt of verouderen; daarom plant `StartSessionAsync` opnieuw. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, x64-besturingssysteem en x64-host), `LegacyPlatform` (altijd `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (allemaal gelijk aan `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` is een waarde van `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), behalve in `SessionPlan.RequiredCapabilities`: een platform- of contentvereiste is `STATICALLY_VALIDATED` of `UNAVAILABLE`, een native capability `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` of `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Presentatiewijzigingen gebruiken de sleutels `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (optionele tekenreeksmap). | Codes die met `OL_E_` beginnen zijn fouten, `OL_W_` waarschuwingen, al het andere is informatief. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Sessiediagnoses bevatten geen plandiagnoses. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (ontvangsttijd bij de host), `Sequence` (vanaf 1, per sessie), `Data`, `SessionTickMilliseconds` (de monotone sessieklok van de plugin op het moment dat die het event uitzond; null voor hostevents). | Begrensd tot de 256 meest recente events (de oudste vervallen). Pluginevents komen in volgorde binnen via de telemetriering; events die door een overloop van de ring verloren gaan, worden geteld door `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Proceslokaal. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Zie `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Zie `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Byte-enum in declaratievolgorde: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` en `EnteringGameplay` worden door de huidige service nooit ingesteld; `Snapshotting` is kortstondig en in de praktijk niet waarneembaar. Eindtoestanden: `Completed`, `Failed`. Volledige semantiek: [levenscyclus van de sessie](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Typen voor runtimebesturing

| Type | Definitie | Stabiliteit |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Statische codec die de host en de plugin gebruiken voor de envelope van de mailbox: magic `0x4F4C5243` ("OLRC"), versie 1, header van 72 bytes in little-endian (magic, versie, soort 1 = verzoek / 2 = antwoord, totale lengte, sessie-`Guid`, verzoek-id, payloadlengte, SHA-256 van de payload), gevolgd door een UTF-8-JSON-payload. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: openbaar omdat beide kanten van de brug het delen; geen integratieoppervlak; het formaat kan met de protocolversie veranderen. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — wat de host aan de plugin publiceert in het memory-mapped bestand `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256, string Settings, string SnapshotSha256)`: een geplande module die de handoff meedraagt (`StartupHandoff.Modules`); `SnapshotSha256` (wireversie 8) is de digest van de bestanden van de module die bij het plannen is genomen, leeg wanneer het plan er geen meedroeg. | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: de bepaalde startklok van een NEW_MAP-sessie die de handoff meedraagt (`StartupHandoff.Start`); een null-veld behoudt de eigen waarde van OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Codec: magic `0x4F4C5348`, versie 8 (leest 3 tot en met 8; versie 5 voegt de geplande modules toe, versie 6 de effectieve instellingen van elke module, versie 7 de start-datum/-tijd, versie 8 de bestandssnapshot van elke module ten tijde van het plan; de vlagbits 0x04 en 0x08 dragen `PluginTrace` en `NativeTrace`), header van 64 bytes met SHA-256-integriteit van de payload. | `INTERNAL` |
| `TelemetryRingWire` | Eventring van plugin naar host in de mapping `OMSILAUNCH_TELEMETRY_NAME`: magic `0x52544C4F` ("OLTR"), versie 1, 64 genummerde slots van 2048 bytes. `Initialize`, `Write`, `IsRing`, `ReadSince` (retourneert de events na een volgnummer en hoeveel er verloren zijn gegaan). | `INTERNAL` |

De plugin weigert een handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`), tenzij `WorldMode` gelijk is aan `NewMap` of `SavedSituation`, `HeadlessStart` true is, `PlayerVehicleEnabled` false is, beide datum-/tijdmodi `Unset` zijn en een opgeslagen situatie een niet-lege identiteit heeft. De planner dwingt dezelfde voorwaarden eerder af, zodat een uitvoerbaar plan dit nooit veroorzaakt.

<a id="capability-registry-types"></a>
### Typen van het capabilityregister

| Type | Doel |
| --- | --- |
| `PublicCapabilityRegistry` | De canonieke capabilitycatalogus. `ProtocolVersion` (`"0.1"`), `All` (elke `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (de 48 concrete operatie-id's die een frontend mag doorsturen), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (opdrachtroute naar operatie-id), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (retourneert `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Afgedwongen door `ExecuteRuntimeAsync`, de CLI, het lokale control plane en de plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (een waarde van `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; afgeleid `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (neemt `cursor`/`limit`, zie [paginering](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Wat een runtime-operatie wijzigt: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | De bewijsvocabulaire: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Volledige catalogus: [capabilities](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Extensiemethoden van `D3DRuntimeApi`

Getypeerde wrappers rond `ExecuteRuntimeAsync` voor de `d3d.*`-operaties (`EXPERIMENTAL`, `PublicExperimental`-capability `d3d.texture`). Ze wijzen verzoek-id's toe uit een procesbrede teller die bij 30 000 begint en gebruiken standaard een timeout van 5 s (behalve `GetD3DStatusAsync`, dat er een vereist).

| Methode | Operatie | Argumenten en limieten |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | geen |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | breedte/hoogte 1..4096, niveaus 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | niveau 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, breedte/hoogte 1..4096, pixels ≤ 48 KiB (Base64-gecodeerd tijdens overdracht) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | herhaald vrijgeven wordt geweigerd met `OL_E_D3D_RESOURCE_RELEASED` |

Typen: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` met `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Fouten: een mislukt resultaat wordt opnieuw geworpen als `OmsiRuntimeException(Code, detail)`, waarbij `Code` de `ErrorCode` van het resultaat is (of `OL_E_RUNTIME_OPERATION_FAILED` als die ontbreekt) en het bericht `"<code>: <Values["detail"]>"` is; een geslaagd resultaat zonder waarden, of een onbekende tekenreeks voor de apparaattoestand, werpt `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Alles wat `ExecuteRuntimeAsync` werpt, wordt ongewijzigd doorgegeven. De afhandeling van een apparaatreset is runtime-gevalideerd: een reset brengt het apparaat via `Resetting` terug naar `Ready` en maakt elke levende texture ongeldig (`OL_E_D3D_STALE_RESOURCE_HANDLE`, runtime-afsluitronde `D01`); de catalogusvermelding `d3d.texture` legt het vast. De overgang `Lost` kan niet van buiten het product worden veroorzaakt en is alleen offline gedekt.

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
### Typen van het procescontract

| Type | Inhoud |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Alleen door de CLI gebruikt ([exitcodes](exit-codes.md)); de API beëindigt het proces nooit. |
| `PublicErrorCategory` | Tekenreeksconstanten die in foutenvelopes van CLI/besturing worden gebruikt: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Eén `const string` per code (143: 142 `OL_E_`-fouten en 1 `OL_W_`-waarschuwing) en `All`, de catalogus van `PublicErrorDescriptor(Code, Category)`. Categorieën: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Referentie: [foutcodes](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Eigenschap `Code` plus bericht; wordt alleen door `D3DRuntimeApi` geworpen. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (installatie-identiteit en padinsluiting)

Stabiliteit: `STABLE_BETA` (zuivere functies, geen I/O, geen OMSI-toestand, geen wijziging van het bestandssysteem, geen deelname aan transacties, geen actieve sessie vereist). Het is de enige definitie die wordt gebruikt door de installatielease, de pipenaam van local control, de insluiting van assets van sessieprofielen, de validatie van doelen van internettextures en het modelpad voor spawnen tijdens runtime.

| Lid | Gedrag |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` zonder afsluitende scheidingstekens, behalve bij een stationsroot (`C:\`), die behouden blijft. Lost segmenten `.` en `..` op, behandelt `/` en `\` gelijk en voegt herhaalde scheidingstekens samen. Lost junctions of symbolische koppelingen **niet** op. Werpt `ArgumentException` voor een root die null of leeg is. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` in hoofdletters. Lexicaal gelijkwaardige schrijfwijzen van één root (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) delen één sleutel; verschillende roots (`C:\OMSI-A`, `C:\OMSI-B`) nooit. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Lost `candidate` op (relatief aan `root`, of absoluut) en retourneert alleen `true` wanneer het strikt onder `root` ligt; `relativePath` is de canonieke schrijfwijze met `\`. Gebruikt segmenten van `Path.GetRelativePath`, zodat een naastgelegen map zoals `C:\OMSI-A\x` nooit binnen `C:\OMSI` ligt; de root zelf, andere volumes en ontsnappingen via `..` retourneren `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Splitst op `/` en `\` en laat lege segmenten weg. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Sessieprofielen (`OmsiLaunch.Core`)

Stabiliteit: `EXPERIMENTAL`. Deze typen compileren een YAML-[sessieprofiel](session-profiles.md) (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, schema `omsilaunch.session-profile/v1`) tot een `LaunchSpec`. De CLI `/predefined-profile:<id> /predefined-profile-index:<n>` gebruikt precies deze aanroepen; een integrator kan ze gebruiken om een profiel via de API te starten.

| Lid | Gedrag |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Leest en valideert het pakket. `id` moet een gewone mapnaam zijn (anders `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` is 1..99 (`MinPresetIndex`..`MaxPresetIndex`; anders `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Ontbrekend bestand: `OL_E_SESSION_PROFILE_NOT_FOUND`; groter dan `MaxBytes` (256 KiB), ongeldige YAML, ankers, onbekende sleutels of een `id` die afwijkt van de mapnaam: `OL_E_SESSION_PROFILE_INVALID`; ander `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Assetpaden zijn beperkt tot het pakket (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Elke fout is een `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Retourneert `baseline` met het profiel toegepast: voor `NewMap` het `new`-blok van het profiel (kaart, instappunt en eventuele datum/tijd/jaar/weer); de `settings` van de preset samengevoegd over `Environment.General`; de `Presentation`, `InternetTextures` en `Behavior` van de preset, indien aanwezig; en `SessionProfile` = `profile.Metadata`. Voor `NewMap` controleert het ook de kaart tegen de `compatibility`-lijst van het profiel (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | De compatibiliteitscontrole voor de andere modi (voor `SavedSituation` wordt de kaart uit de `.osn` gelezen). De CLI roept dit aan nadat de uiteindelijke `WorldSpec` is opgebouwd. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` en de geaccepteerde sleutels per YAML-mapping. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | Het geladen pakket; `Preset` is alleen de geselecteerde preset. |
| `SessionProfileException(string code, string message)` | `IOException` met `Code` (een van de `OL_E_SESSION_PROFILE_*`-codes); het bericht is `"<code>: <message>"`. |

De CLI weigert daarnaast opdrachtregelvlaggen die met het profiel conflicteren (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); die controle maakt geen deel uit van de compiler. Zie [sessieprofielen](session-profiles.md#precedence-and-override-conflicts) voor de volledige samenvoegvolgorde.

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
### Platformtypen (`OmsiLaunch.Process`)

| Type | Stabiliteit | Gebruik |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` als parametertype van de constructor van `OmsiLaunchService` | Detecteert het platform, controleert de beschrijfbaarheid, start, observeert, beëindigt en wacht op `Omsi.exe`. Geef `new CurrentWindowsX64Platform()` door; een eigen implementatie wordt niet ondersteund. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | De enige implementatie: Windows-x64-host, `CreateProcessW` voor `Omsi.exe`, `TerminateProcess` voor de canonieke stop. De methoden worden door de service aangeroepen; integrators construeren alleen de instantie. Leden (gedeeld met `IRuntimePlatform`): `Detect(root)` retourneert de `RuntimePlatformInfo` van het plan; `ValidateCurrent(info)` werpt `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` wanneer de host geen sessie kan uitvoeren; `IsInstallationWritable(root)` ligt ten grondslag aan `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` maakt `Omsi.exe` aan en legt de procesidentiteit vast (PID, aanmaaktijd, pad en de door de service berekende hash; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` en `Terminate` observeren en beëindigen het proces. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Openbaar in de assembly omdat de service en de tests ze delen. Geen integratieoppervlak; `LaunchedProcess` omhult intern de handles van het OMSI-proces en de OMSI-thread (dat zijn geen openbare leden) en wordt nooit door `IOmsiLaunch` geretourneerd. |

<a id="thread-safety"></a>
## Threadveiligheid

- `OmsiLaunchService` is veilig voor gelijktijdige aanroepen op verschillende sessies: sessies staan in een `ConcurrentDictionary` en elke wijziging per sessie gebeurt onder de eigen lock van de sessie.
- Gelijktijdige aanroepen op dezelfde sessie zijn veilig, maar worden geserialiseerd waar dat van belang is: `ExecuteRuntimeAsync` neemt een gate per sessie, zodat een tweede opdracht op de eerste wacht (de timeout ervan begint wanneer de opdracht wordt klaargezet).
- De supervisor draait als taak in de threadpool (`Task.Run`) vanaf het moment dat `StartSessionAsync` terugkeert totdat de sessie een eindtoestand bereikt; hij pollt de telemetrie en het proces elke 100 ms. Aanroepers voeren nooit supervisorcode uit.
- `StopAsync` en `GetStatusAsync` worden synchroon voltooid en kunnen vanuit elke thread worden aangeroepen, ook binnen een `ProcessExit`-handler (de CLI doet dit met een budget van 4 s).
- Geen enkele API-aanroep is aan een thread gebonden; geen enkele vereist een synchronisatiecontext.

<a id="what-is-not-in-the-api"></a>
## Wat niet in de API zit

- Geen `IntPtr`, `nint`, Win32-handles, native adressen, VMT-pointers of procesobjecten. Resultaatwaarden waarvan de sleutels met `internal_` beginnen of op `_address`, `_pointer`, `_vmt` eindigen, worden verwijderd voordat een resultaat `ExecuteRuntimeAsync` verlaat.
- Geen `internal.*`-runtime-operaties: `internal.road-vehicles.make-basic` is `InternalOnly` in het register en retourneert `OL_E_RUNTIME_OPERATION_UNKNOWN` vanuit de API en de CLI.
- Geen directe lees- of schrijfacties in het OMSI-geheugen, geen toegang op bestandsniveau tot de installatie buiten wat een `LaunchSpec` declareert.
- Geen handles over procesgrenzen heen: het [lokale control plane](local-control.md) is de enige route tussen processen en accepteert alleen `session.status`, `session.events`, `session.stop` en `runtime.execute`.
- Geen toepassing van het spelersvoertuig bij de start. Coöperatief afsluiten, `LAST_MAP_STATE`, start-datum/-tijd/-weer en overlays van toetsenbord-/controllerdocumenten zijn `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Samenvatting van de stabiliteit

| Oppervlak | Stabiliteit |
| --- | --- |
| Constructor van `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transport van `ExecuteRuntimeAsync`; `PublicStableBeta`-operaties | `STABLE_BETA` |
| `PublicExperimental`-operaties, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| specleden voor datum/tijd/weer/invoer, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| specleden voor het spelersvoertuig | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implementaties van `IRuntimePlatform`, alle implementatie-assembly's | `INTERNAL` |
| `WorldMode.LastSituation`, `internal.*`-operaties | `UNAVAILABLE` |
