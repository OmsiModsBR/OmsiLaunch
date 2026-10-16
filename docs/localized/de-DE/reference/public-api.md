# Referenz der öffentlichen API (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> Übersetzung der [englischen Originalseite](../../../reference/public-api.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite ist die normative Referenz für die verwaltete öffentliche API von OmsiLaunch 0.1.0-beta4: die Assembly `OmsiLaunch.Api` (Verträge) und der Einstiegspunkt für Integratoren `OmsiLaunchService` in `OmsiLaunch.Core`. Sie dokumentiert ausschließlich, was der aktuelle Code tut. Alles, was ein Integrator aufrufen, empfangen oder beobachten kann, ist hier mit seiner Stabilitätsstufe aufgeführt; alles, was hier nicht aufgeführt ist, ist keine Integrationsschnittstelle.

Das generierte [Inventar der öffentlichen API](public-api-inventory.md) listet jeden öffentlichen Typ und jedes öffentliche Mitglied von `OmsiLaunch.Api`, `OmsiLaunch.Core` und `OmsiLaunch.Process` mit Signatur und Stabilität auf; ein Dokumentations-Gate schlägt fehl, wenn Inventar und Assemblies voneinander abweichen. Diese Seite erläutert die Semantik.

Verwandte Seiten: [LaunchSpec-Referenz](launchspec.md), [Fehlercodes](errors.md), [Sitzungslebenszyklus](../concepts/session-lifecycle.md), [Transaktionen und Recovery](../concepts/transactions-and-recovery.md), [Runtime-Steuerung](runtime-control.md), [Capabilities](capabilities.md), [lokale Steuerungsebene](local-control.md), [Exitcodes](exit-codes.md), [Status der Runtime-Validierung](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Stabilitätsvokabular

| Stufe | Bedeutung auf dieser Seite |
| --- | --- |
| `STABLE_BETA` | Der Vertrag ist für die Protokolllinie 0.1 eingefroren, und der Pfad ist in `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md` zur Laufzeit validiert. |
| `EXPERIMENTAL` | Aufrufbar und getestet, aber der Vertrag oder der Runtime-Nachweis kann sich ändern, bevor er stabil wird. |
| `PARTIAL` | Im Vertrag vorhanden; nur ein Teil des Verhaltens ist implementiert oder validiert (der Text gibt an, welcher Teil). |
| `INTERNAL` | Aus technischen Gründen in der Assembly öffentlich (die Bridge verwendet den Typ gemeinsam), aber keine Integrationsschnittstelle; kann sich ohne Ankündigung ändern. |
| `UNAVAILABLE` | Im Vertrag vorhanden, wird aber vom aktuellen Build abgelehnt. |

<a id="assembly-overview"></a>
## Überblick über die Assemblies

| Assembly | Rolle für Integratoren |
| --- | --- |
| `OmsiLaunch.Api` | Reine Verträge: Records, Enums, `IOmsiLaunch`, Capability-Registry, Fehlerkatalog, Wire-Formate, D3D-Hilfsfunktionen. Enthält kein `IntPtr`, kein `nint`, kein Win32-Handle, keine native Adresse und kein Prozessobjekt. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (die Implementierung von `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` und `CurrentWindowsX64Platform` (der einzige Plattformadapter), `InstallationLease`. Wird zum Erzeugen des Service benötigt. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Implementierungs-Assemblies. Ihre öffentlichen Typen sind für Integratoren `INTERNAL`. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Einstiegspunkt: `OmsiLaunchService` und `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parameter | Gültiger Wert | Ungültig / Standard |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (Namespace `OmsiLaunch.Process`). Erkennt die Plattform, erzeugt den OMSI-Prozess mit `CreateProcessW`, wartet auf ihn und beendet ihn. | Es wird keine andere Implementierung ausgeliefert. Eine eigene `IRuntimePlatform` ist `INTERNAL`. |
| `PluginBuildDirectory` | Verzeichnis, das die Referenzdateien der permanenten Plugin-Gesamtheit (Closure) enthält: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` und jede `OmsiLaunch.*.dll` der verwalteten Closure (muss `OmsiLaunch.Plugin.dll` enthalten). In einem installierten Paket ist dies `<package>\plugins`. | Fehlendes Verzeichnis oder fehlende Datei: `PlanSessionAsync` liefert einen nicht ausführbaren Plan mit `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Pfad von `OmsiLaunch.Native.x86.dll` (im Paket: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Wie oben. |
| `ReleaseManifestPath` | `release-manifest.json` neben `OmsiLaunch.exe`, sofern vorhanden. Liefert den erwarteten SHA-256 jeder Datei in `plugins/` (`plugin.integrity.reference = manifest`). | `null` (Entwicklungslayout): Die installierten Dateien werden nur auf Vorhandensein und Selbstkonsistenz gegenüber der Referenz-Closure geprüft (`plugin.integrity.reference = self`). Fehlerhaftes Manifest: `OL_E_RELEASE_MANIFEST_INVALID`. |

Der Service liest diese Pfade bei jedem `PlanSessionAsync` und `StartSessionAsync`; er kopiert, stellt bereit oder entfernt niemals Plugin-Dateien (siehe [permanentes Plugin](../concepts/permanent-plugin.md)). Die CLI erzeugt den Service genau so (`tools/OmsiLaunch.Cli/Program.cs`):

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

Erzeugen Sie einen Service pro Prozess und verwenden Sie ihn gemeinsam. Stabilität: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Regeln zur Sitzungseigentümerschaft

| Regel | Detail |
| --- | --- |
| Ein Eigentümer pro Installation | `StartSessionAsync` erwirbt die Installations-Lease, einen benannten Semaphor `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, und hält sie, bis der Supervisor die Installation wiederhergestellt hat. Ein zweiter Start auf demselben Stammverzeichnis aus einem beliebigen Prozess derselben Anmeldesitzung schlägt mit `OL_E_INSTALLATION_BUSY` fehl (gemeldet als `Failed`-Sitzung, siehe `StartSessionAsync`). Die Lease gilt pro Anmeldesitzung, nicht anmeldeübergreifend, und wird nicht freigegeben, solange ein anderer Prozess ein Handle darauf hält (akzeptiertes Risiko). |
| Handles sind prozesslokal | `SessionHandle` kapselt die `Guid` der Sitzung. Es ist nur für die `OmsiLaunchService`-Instanz aussagekräftig, die es zurückgegeben hat. Ein Handle, das in einem anderen Prozess (oder einer anderen Service-Instanz) aus einer bekannten `Guid` gebildet wird, führt zu `KeyNotFoundException`. Prozessübergreifende Steuerung läuft über die [lokale Steuerungsebene](local-control.md), nicht über Handles. |
| Rufen Sie immer `CloseAsync` auf | Ab `StartSessionAsync` besitzt der Prozess eine dauerhafte Transaktion. `CloseAsync` fordert bei Bedarf den kanonischen Stopp an, wartet auf den Supervisor (Prozessende, exakte Wiederherstellung, Freigabe der Lease) und vergisst die Sitzung. Es muss auf jedem Ausstiegspfad aufgerufen werden, auch nach einem `Failed`-Zustand. Ohne diesen Aufruf bleibt der Sitzungseintrag im Speicher; die Wiederherstellung selbst führt der Supervisor unabhängig davon durch. |
| Fehlgeschlagene Sitzungen sind trotzdem Sitzungen | Ein Start, der fehlschlägt, nachdem `StartSessionAsync` zurückgekehrt ist, meldet `SessionState.Failed`; das Handle bleibt für `GetStatusAsync`/`WaitForAsync` gültig, bis `CloseAsync` aufgerufen wird. |
| Pläne werden erneut geprüft | `StartSessionAsync` berechnet den Hash von `Omsi.exe` neu und plant die Spezifikation erneut; ein Plan, der nicht mehr ausführbar ist, wird mit `OL_E_PLAN_NOT_RUNNABLE` abgelehnt. |

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

Gemeinsame Fakten für jede Methode:

- Unbekannte oder bereits geschlossene Handles lösen `KeyNotFoundException` aus („Unknown OmsiLaunch session.“).
- Keine Methode außer `ExecuteRuntimeAsync` setzt eine laufende Sitzung (Running) voraus.
- Ausnahmen, die einen OmsiLaunch-Code tragen, stellen den Code an den Anfang von `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). Die CLI extrahiert Codes auf dieselbe Weise aus Meldungen (`CliProgram.Classify`).
- Unterstützte Installation: OMSI 2. Native Capabilities werden pro Build im Prozess aufgelöst und verifiziert; eine auf einem Build nicht verfügbare Capability schlägt mit `OL_E_CAPABILITY_UNRESOLVED` fehl. Siehe [Kompatibilität](compatibility.md).

<a id="complete-minimal-example"></a>
### Vollständiges Minimalbeispiel

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

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Zweck | Kompiliert eine `LaunchSpec` zu einem `SessionPlan`, ohne OMSI zu starten: validiert die Spezifikation, erkennt die Plattform, erstellt den Fingerabdruck von `Omsi.exe`, löst Inhaltsidentitäten auf, berechnet geplante Dateiänderungen, listet benötigte und nicht unterstützte Capabilities auf und entscheidet über `IsRunnable`. Öffentliche Capability `session.plan`. |
| Parameter | `spec`: eine vollständig befüllte `LaunchSpec` (siehe [LaunchSpec-Referenz](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (alle acht Dictionaries) und `Behavior` dürfen nicht null sein; die optionalen Mitglieder dürfen `null` sein. `RootPath` sollte ein absolutes Verzeichnis sein; ein leeres Stammverzeichnis wird als `OL_E_INSTALLATION_NOT_FOUND` erfasst, die Plattformprüfung auf einem leeren Pfad löst jedoch `ArgumentException` aus, bevor der Plan zurückgegeben wird – übergeben Sie daher niemals ein leeres Stammverzeichnis. |
| Rückgabe | `SessionPlan` mit einer neuen `SessionId`, `BuildProfileId` = das bekannte Layout (`"Omsi23004"`) oder `"unrecognized"` für einen OMSI-2-Build ohne bekanntes Layout, der Eingabe-`Spec`, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (Zielpfade `plugins\OmsiLaunch.*` sowie `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (ein `PlannedModule` pro angefordertem oder durch Inhalte aktiviertem [Runtime-Modul](../../../reference/runtime-modules.md); Accessor `EffectivePlannedModules`). `IsRunnable` ist genau dann `true`, wenn kein Diagnosecode mit `OL_E_` beginnt. Informative Diagnosen (`plugin.integrity.reference` mit der Meldung `self` oder `manifest`, `session_profile.selected`) machen einen Plan niemals nicht ausführbar. |
| Im Ergebnis übermittelte Fehler | Jeder Planungsfehler ist eine Diagnose, keine Ausnahme: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (die Meldung trägt den Startbild-/ITX-Code), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (die installierte Plugin-Closure in `plugins\` wird zum Planungszeitpunkt gegen das Release-Manifest geprüft), `OL_E_RUNTIME_ARTIFACT_MISSING` (die Meldung kann `OL_E_RELEASE_MANIFEST_INVALID` enthalten). Vollständige Bedingungen: [LaunchSpec-Validierungsregeln](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Ausgelöst | `OperationCanceledException`, wenn das Token beim Eintritt bereits abgebrochen ist (der einzige Prüfpunkt); `ArgumentException`/`NotSupportedException` bei syntaktisch ungültigen Stammpfaden; `NullReferenceException`/`ArgumentNullException` bei null in erforderlichen Mitgliedern; `System.Text.Json.JsonException` bei einem syntaktisch ungültigen Release-Manifest. |
| Abbruch | Wird einmal beim Eintritt geprüft. Danach ist die Planung synchrone Dateisystemarbeit. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert den OMSI-Zustand | Nein. |
| Verändert das Dateisystem | Nein (liest `Omsi.exe`, Inhaltsdateien, Plugin-Closure, Manifest). Einstellungswerte werden hier nicht validiert (nur Existenz und Beschreibbarkeit des Schlüssels); ein ungültiger Wert schlägt beim Start mit `OL_E_INVALID_SETTING_VALUE` fehl. |
| Transaktion / Wiederherstellung | Keine. |
| Einschränkungen | Die Anforderung eines anderen `Date`/`Time`/`Year`-Modus als `Unset`, eines anderen `Weather`-Modus als `Unset`, eines beliebigen `PlayerVehicle`-Felds, von `Input`-Dokumenten, von `EntrypointIdentity` oder von `WorldMode.LastMapState` erzeugt auf diesem Build `OL_E_CAPABILITY_UNAVAILABLE` und einen nicht ausführbaren Plan (Einträge in `UnsupportedRequestedFeatures`, die ihren Katalog-Nachweis `UNAVAILABLE` oder `PARTIAL` tragen). |
| Stabilität | `STABLE_BETA`. |
| Beispiel | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Zweck | Startet aus einem ausführbaren Plan eine transaktionale, verwaltete OMSI-Sitzung: erwirbt die Installations-Lease, führt die Recovery eines veralteten Journals durch, validiert die permanente Plugin-Closure, erstellt Snapshots und Overlays der Sitzungsdateien, erzeugt die Start-Übergabe (Handoff), den Telemetrie-Slot und die Runtime-Mailbox, startet `Omsi.exe`, erfasst den Prozess im Journal und übergibt die Sitzung an einen Supervisor im Hintergrund. Öffentliche Capability `session.start`. |
| Parameter | `plan`: ein `SessionPlan` mit `IsRunnable == true`. Die Spezifikation im Plan wird erneut geplant; aus dem Plan des Aufrufers wird nur `plan.SessionId` übernommen. `plan.Spec.Behavior.StartupTimeoutSeconds` muss im Bereich 1..600 liegen. |
| Rückgabe | `SessionHandle(plan.SessionId)`, sobald `Omsi.exe` erzeugt und erfasst wurde (Zustand `WaitingForPlugin`) oder sobald der Startpfad fehlgeschlagen ist (Zustand `Failed`). Es wird nicht auf das Gameplay gewartet; verwenden Sie `WaitForAsync(session, SessionState.Running, ...)`. |
| Ausgelöst | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")`, wenn `plan.IsRunnable` false ist; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")`, wenn die erneute Planung nicht ausführbar ist (z. B. `Omsi.exe` geändert, Inhalt entfernt, Plugin-Closure fehlt); `InvalidOperationException("Duplicate session id.")`, wenn eine Sitzung mit derselben ID noch registriert ist (rufen Sie zuerst `CloseAsync` auf); `ArgumentOutOfRangeException`, wenn `StartupTimeoutSeconds` außerhalb von 1..600 liegt; `OperationCanceledException` bei Abbruch vor oder während der erneuten Planung; zusätzlich alles, was `PlanSessionAsync` auslöst. In allen Fällen mit ausgelöster Ausnahme wird keine Sitzung registriert. |
| Im Ergebnis übermittelte Fehler | Jeder Fehler nach der erneuten Planung wird innerhalb des Startpfads abgefangen: Die Sitzung wird registriert, ihr Zustand ist `Failed`, und ihre Diagnosen enthalten `OL_E_START_SESSION`, dessen Meldung die innere Meldung ist (beginnend mit dem inneren Code, sofern einer vorhanden ist): `OL_E_INSTALLATION_BUSY` (Lease gehalten oder ein im Journal erfasster OMSI-Prozess noch aktiv), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (nur wenn der zurückgestellte erneute Versuch mit den Overlays dieser Sitzung die Eigentümerschaft weiterhin nicht nachweisen kann), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. Die Bereinigung kann `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (OMSI-Ende nicht bestätigt; Journal bleibt erhalten) oder `OL_E_RESTORE_FAILED` hinzufügen. Spätere Fehler meldet der Supervisor (siehe [Sitzungslebenszyklus](../concepts/session-lifecycle.md)). |
| Abbruch | Vor/während der erneuten Planung: löst eine Ausnahme aus. Danach wird das Token an die Transaktion und die Prozesserzeugung weitergegeben; ein Abbruch dort wird wie jeder andere Startfehler behandelt (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), der Prozess (falls erzeugt) wird beendet und die Installation wiederhergestellt. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert den OMSI-Zustand | Ja: erzeugt den OMSI-Prozess mit den Umgebungsvariablen `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Verändert das Dateisystem | Ja, innerhalb des Installationsstammverzeichnisses: `.omsilaunch\diagnostics\<sessionId>-host.log` (Aufbewahrung: die 50 neuesten Sitzungen), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (einmalig für das verwaltete Startbild kopiert), Sitzungs-Overlays (`options.cfg`-Patches, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), Sitzungslöschungen (ITX-Ziele, `Texture\standard.ipr`, `closecheck`) sowie das dauerhafte Entfernen einer bereits vorhandenen veralteten `closecheck`-Datei, wenn `SuppressStaleClosecheckWarning` true ist (Diagnose `closecheck.stale-removed`). |
| Transaktion / Wiederherstellung | Öffnet die Transaktion (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Jeder Pfad aus der Sitzung heraus endet in der Wiederherstellung. Siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md). |
| Einschränkungen | Nur `WorldMode.NewMap` mit `PresentedEntrypointIndex` und `WorldMode.SavedSituation` erreichen das Gameplay. `WorldMode.LastMapState` ist `UNAVAILABLE`. Anforderungen des Spielerfahrzeugs erreichen diese Methode nie, weil sie bereits zum Planungszeitpunkt nicht ausführbar sind. |
| Stabilität | `STABLE_BETA` (die Lebenszyklen NEW_MAP und SAVED_SITUATION sind zur Laufzeit validiert). |
| Beispiel | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Zweck | Liest den semantischen Lebenszykluszustand, die bisher gesammelten Diagnosen und die begrenzte Liste der Runtime-Ereignisse. Öffentliche Capability `session.status`. |
| Parameter | `session`: ein von `StartSessionAsync` zurückgegebenes, noch nicht geschlossenes Handle. |
| Rückgabe | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: ein unveränderlicher Snapshot (Arrays werden unter der Sitzungssperre kopiert). `RuntimeEvents` ist für eine aktive Sitzung nie `null`. |
| Ausgelöst | `KeyNotFoundException` bei unbekannten/geschlossenen Handles. Löst sonst nie eine Ausnahme aus. |
| Abbruch | Das Token wird ignoriert (der Aufruf wird synchron abgeschlossen). |
| Laufende Sitzung erforderlich | Nein. |
| Verändert OMSI / Dateisystem / Transaktion | Nein / Nein / Keine. |
| Stabilität | `STABLE_BETA`. |
| Beispiel | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Zweck | Fragt (alle 100 ms) ab, bis sich die Sitzung in `state` oder in einem Endzustand (`Completed`, `Failed`) befindet oder das Timeout abläuft; gibt dann den aktuellen Status zurück. |
| Parameter | `state`: ein beliebiger `SessionState`. Das Warten auf einen Übergangszustand, der bereits durchlaufen wurde (oder nie gesetzt wird, siehe [Sitzungslebenszyklus](../concepts/session-lifecycle.md)), wartet bis zu einem Endzustand oder bis zum Timeout. `timeout`: ein beliebiger nicht negativer `TimeSpan` oder `Timeout.InfiniteTimeSpan`. |
| Rückgabe | Der Status zum Zeitpunkt, an dem das Warten endete. Bei einem Timeout wird der Status zurückgegeben, keine Ausnahme: Prüfen Sie `State` selbst. Ein Warten auf `Running`, das in `Failed` endet, kehrt sofort mit den Fehlerdiagnosen zurück. |
| Ausgelöst | `KeyNotFoundException`; `OperationCanceledException`, wenn das Token des Aufrufers abgebrochen wird (nur der Abbruch durch den Aufrufer wird weitergegeben, das interne Timeout nicht). |
| Abbruch | Das Token des Aufrufers wird bei jedem 100-ms-Takt berücksichtigt. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert OMSI / Dateisystem / Transaktion | Nein / Nein / Keine. |
| Stabilität | `STABLE_BETA`. |
| Beispiel | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Zweck | Fordert den kanonischen Stopp an. Die Methode setzt das Stopp-Flag und kehrt sofort zurück; der Supervisor bemerkt das Flag innerhalb seiner 100-ms-Schleife, ruft `TerminateProcess` für `Omsi.exe` auf, wartet auf das Prozessende, markiert das Journal mit `ProcessExited`, stellt jede der Sitzung gehörende Datei wieder her und gibt die Lease frei. Dies ist eine erzwungene Beendigung: Die eigene Beenden-Routine von OMSI läuft nicht, und OMSI schreibt `options.cfg` beim Beenden nicht neu (beabsichtigt, schützt die Transaktion). Ein kooperatives Herunterfahren über `WM_CLOSE` ist nicht implementiert (Produktentscheidung; im Runtime-Abschluss wurde OMSI innerhalb von 30 s nach `WM_CLOSE` nicht geschlossen, `L05b`). Öffentliche Capability `session.stop`. |
| Parameter | `session`. |
| Rückgabe | Abgeschlossener Task; wartet weder auf die Beendigung noch auf die Wiederherstellung. Verwenden Sie `WaitForAsync(session, SessionState.Completed, ...)`, um den Abschluss zu beobachten. |
| Ausgelöst | `KeyNotFoundException`. |
| Abbruch | Token wird ignoriert. |
| Laufende Sitzung erforderlich | Nein. Idempotent; ein Stopp, der angefordert wird, bevor der Supervisor startet, wird berücksichtigt, sobald er startet; ein Stopp für eine Sitzung im Endzustand hat keine Wirkung. |
| Verändert den OMSI-Zustand | Ja: beendet den OMSI-Prozess (Exitcode 1). |
| Verändert das Dateisystem | Indirekt: löst Wiederherstellung, Löschen des Journals und Entfernen der Backups durch den Supervisor aus. |
| Transaktion / Wiederherstellung | Löst `ProcessExited` → `Restoring` → `Restored` aus. Runtime-seitige Änderungen, die über `ExecuteRuntimeAsync` vorgenommen wurden (Uhrzeitänderungen, erzeugte Fahrzeuge, Skriptvariablen, D3D-Texturen), werden nicht wiederhergestellt; sie verschwinden mit dem Prozess. |
| Stabilität | `STABLE_BETA`. |
| Beispiel | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Zweck | Gibt das Handle des Verbrauchers frei, ohne die Transaktion zurückzulassen: Befindet sich die Sitzung nicht im Endzustand, wird der kanonische Stopp angefordert; anschließend wird auf den Lebenszyklus-Task des Supervisors gewartet (Prozessende, Wiederherstellung, Freigabe der Lease); danach wird die Sitzung vergessen. |
| Parameter | `session`. |
| Rückgabe | Wird abgeschlossen, wenn sich die Sitzung im Endzustand befindet und entfernt wurde. Nach der Rückkehr ist das Handle unbekannt (`KeyNotFoundException` bei jedem weiteren Aufruf, auch bei einem zweiten `CloseAsync`). |
| Ausgelöst | `KeyNotFoundException`; `OperationCanceledException`, wenn der Aufrufer während des Wartens auf den Supervisor abbricht. In diesem Fall wird die Sitzung nicht entfernt und der Supervisor läuft weiter; rufen Sie `CloseAsync` erneut auf. |
| Abbruch | Gilt nur für das Warten; die Wiederherstellung wird niemals abgebrochen. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert den OMSI-Zustand | Ja, wenn die Sitzung noch aktiv ist (wie bei `StopAsync`). |
| Verändert das Dateisystem | Indirekt (Wiederherstellung durch den Supervisor). |
| Transaktion / Wiederherstellung | Garantiert, dass die Transaktion zu Ende geführt wird, bevor das Handle freigegeben wird (sofern der Supervisor gestartet wurde). Bei einer Sitzung, die fehlgeschlagen ist, bevor der Supervisor gestartet wurde, hat der Startpfad bereits wiederhergestellt oder `OL_E_RESTORE_DEFERRED` gemeldet. |
| Stabilität | `STABLE_BETA`. |
| Beispiel | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Zweck | Führt eine öffentliche Runtime-Operation innerhalb des laufenden OMSI-Prozesses über die Single-Flight-Mailbox der Sitzung aus (speicherabgebildet, 64 KiB, Anforderung an Sitzungs-ID und Anforderungs-ID gebunden). Das Plugin führt die Operation auf dem UI-Thread von OMSI aus. Operationskatalog: [Runtime-Steuerung](runtime-control.md) und [Capabilities](capabilities.md). |
| Parameter | `command.SessionId` muss gleich `session.SessionId` sein. `command.RequestId`: vom Aufrufer gewählter `ulong`; verwenden Sie einen streng monoton steigenden Zähler pro Prozess (die D3D-Hilfsfunktionen beginnen bei 30 000, der CLI-Eigentümer bei 10 001/50 000). `command.Operation`: eine öffentliche Operations-ID aus `PublicCapabilityRegistry.PublicRuntimeOperationIds` (z. B. `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: Zeichenfolgenwerte mit ordinal verglichenen Namen als Schlüssel; die pro Operation erforderlichen Namen liefert `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: gemessen ab dem Zeitpunkt, an dem die Anforderung in der Mailbox bereitgestellt wird (Warten hinter einem anderen laufenden Befehl zählt nicht). Die CLI verwendet als Eigentümer 5 s (15 s für `road-vehicles.spawn`) und als Client 8 s / 30 s. |
| Reihenfolge der Prüfungen | 1. Registry-Validierung (vor der Sitzungssuche): unbekannte oder `internal.*`-Operation → Ergebnis `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; fehlendes erforderliches Argument (nicht vorhanden oder nur Leerraum) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Sitzungssuche → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Zustand nicht `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Mailbox-Anforderung. 6. Ergebniswerte, deren Schlüssel mit `internal_` beginnt oder mit `_address`, `_pointer`, `_vmt` endet, werden entfernt. |
| Rückgabe | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. Bei Erfolg enthält `Values` die semantischen Zeichenfolgen der Operation (pro Operation in [Runtime-Steuerung](runtime-control.md) dokumentiert). |
| Im Ergebnis übermittelte Fehler | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (Registry); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (das Plugin-Ergebnis hat die Mailbox überschritten; Ergebnisse begrenzter Listen werden stattdessen mit `truncated=true` gekürzt); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` mit einem abgeleiteten Schlüssel); jeder `OL_E_D3D_*`-Code (mit `Values["detail"]` und `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (die native Capability der Operation ist auf dem laufenden Build nicht aufgelöst); und jeder Plugin-seitige typisierte Fehler mit eigenem Code in `ErrorCode` und der Meldung in `Values["detail"]` (Beta 4; nur ein Fehler ohne katalogisierten Code ist `OL_E_RUNTIME_OPERATION_FAILED`). Codes, die auf diesem Weg ankommen: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (Plugin-seitige Prüfungen), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Siehe [Fehlercodes](errors.md). |
| Ausgelöst | `KeyNotFoundException`; `InvalidOperationException` mit `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (Mailbox bereits vom Supervisor freigegeben), `OL_E_RUNTIME_CHANNEL_BUSY` (Slot enthält noch eine aufgegebene Anforderung), `OL_E_RUNTIME_REQUEST_ID_REUSED` (eine veraltete Antwort für dieselbe Anforderungs-ID liegt noch im Slot); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (beschädigte, fremde oder nicht passende Antwort); `ArgumentOutOfRangeException`, wenn die serialisierte Anforderung die Mailbox überschreitet; `OperationCanceledException`. |
| Abbruch | Wird beim Warten auf das Gate pro Sitzung und alle 20 ms beim Abfragen der Antwort berücksichtigt. Ein Abbruch während einer laufenden Anforderung setzt den Slot nicht zurück: Der nächste Aufruf für diese Sitzung kann mit `OL_E_RUNTIME_CHANNEL_BUSY` fehlschlagen, bis das Plugin seine Antwort veröffentlicht (die dann als veraltet verworfen wird). Verwenden Sie bevorzugt das Timeout; ein Timeout setzt den Slot zurück, und eine verspätete Antwort wird erkannt und verworfen. |
| Laufende Sitzung erforderlich | Ja (`SessionState.Running`); andernfalls wird `OL_E_SESSION_NOT_RUNNING` ausgelöst. Die Mailbox existiert, bis der Supervisor sie während der Wiederherstellung freigibt. |
| Verändert den OMSI-Zustand | Abhängig von der Operation: `Read`-Operationen nicht; `Write`-/`Action`-Operationen (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) verändern prozessinternen Zustand, der nicht wiederhergestellt wird. |
| Verändert das Dateisystem | Keine Schreibvorgänge des Hosts. OMSI kann infolgedessen eigene Dateien schreiben (nicht nachverfolgt). |
| Transaktion / Wiederherstellung | Keine. |
| Einschränkungen | Ein laufender Befehl pro Sitzung (Aufrufe für dieselbe Sitzung werden serialisiert). Anforderung und Antwort sind jeweils auf 64 KiB minus 8 Byte begrenzt; D3D-Pixelnutzdaten auf 48 KiB. `internal.road-vehicles.make-basic` ist `INTERNAL` und nicht erreichbar. `weather.set` und `weather.metar.set` sind `EXPERIMENTAL`. `timetable.logs.read` ist nicht begrenzt und kann bei großen Fahrplänen `OL_E_RUNTIME_RESPONSE_TOO_LARGE` zurückgeben. `camera.lock` ist `EXPERIMENTAL`; es benötigt ein Spielerfahrzeug und ist zur Laufzeit validiert (`CAM01`), obwohl der `RuntimeValidation`-String der Registry weiterhin `STATICALLY_VALIDATED` lautet. Handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) gelten nur innerhalb der Sitzung. |
| Stabilität | Transport und Vertrag `STABLE_BETA`; die Stabilität pro Operation folgt `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`) mit den oben genannten Ausnahmen. |
| Beispiel | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Zweck | Gibt den für eine Installation ausgewerteten Capability-Katalog zurück: je ein `Capability(Name, Available, EvidenceState, Reason)` pro Eintrag von `PublicCapabilityRegistry`, der nicht `InternalOnly` ist. `Name` ist die Katalog-ID, `EvidenceState` der Katalog-Nachweis (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` wird aus der Plattform, der OMSI-2-Prüfung, der Klassifizierung und den vom Eintrag benötigten nativen Capabilities berechnet (siehe [Capabilities](capabilities.md#getcapabilitiesasync)). |
| Parameter | `installation.RootPath`: die geprüfte Installation (Plattform, `Omsi.exe`). `ExpectedExecutableSha256` wird ignoriert. |
| Rückgabe | Ein Eintrag pro nicht internem Katalogeintrag, z. B. `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` auf dem bekannten Layout), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Beziehung zu `PublicCapabilityRegistry` | Daraus abgeleitet: Die Registry deklariert die Capabilities, ihre Operationen und ihre Nachweise; dieser Aufruf wertet sie für eine Installation aus. |
| Ausgelöst | `OperationCanceledException` beim Eintritt; `ArgumentException` bei einem leeren Stammpfad. |
| Abbruch | Wird einmal beim Eintritt geprüft. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert OMSI / Dateisystem / Transaktion | Nein / Nein / Keine. |
| Stabilität | Aufrufvertrag `STABLE_BETA`; der Listeninhalt ist ein von Hand gepflegtes Inventar: `PARTIAL`. |
| Beispiel | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Zweck | Zählt installierte Inhalte auf und gibt kanonische Identitäten zurück, die in einer `LaunchSpec` verwendbar sind. Die Erkennung überspringt Analysepunkte (Reparse Points; Junction-Zyklen können sie nicht aufhängen), liest OMSI-Dateien als Windows-1252 (UTF-8/UTF-16 mit BOM wird berücksichtigt) und folgt niemals symbolischen Links. |
| Parameter | `query` und `scope` gemäß der folgenden Tabelle. `scope` ist erforderlich für `Entrypoints` (Kartenidentität), `Repaints`, `FleetNumbers`, `Registrations` (Fahrzeugidentität). |
| Rückgabe | Sortierte Liste von `ContentIdentity(Identity, Kind, DisplayName)`. Identitäten sind installationsrelative Pfade mit umgekehrten Schrägstrichen; Vergleiche unterscheiden nicht zwischen Groß- und Kleinschreibung. |
| Ausgelöst | `OperationCanceledException` beim Eintritt; `ArgumentException`, wenn `Entrypoints` ohne Scope abgefragt wird oder das Stammverzeichnis leer ist; `FileNotFoundException` (kein `OL_E_`-Code; die CLI bildet sie auf `OL_E_NOT_FOUND` ab), wenn die Karte oder das Fahrzeug des Scopes nicht installiert ist. Ein fehlendes Stamm- oder Inhaltsverzeichnis ergibt eine leere Liste, keinen Fehler. |
| Abbruch | Wird einmal beim Eintritt geprüft. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert OMSI / Dateisystem / Transaktion | Nein / Nein / Keine. |
| Stabilität | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (jeder zur Laufzeit validierte Plan wird über sie aufgelöst). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (nur statische Nachweise). |
| Beispiel | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Werte von `ContentQueryKind` und Ergebnisse:

| Wert | Scope | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | keiner | `maps\<dir>\global.cfg` | `map` | Name des Kartenverzeichnisses |
| `Situations` | keiner | `situations\...\<file>.osn` | `situation` | von der `.osn` referenzierte Kartenidentität (kann `null` sein) |
| `Vehicles` | keiner | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` oder Dateiname |
| `Repaints` | Fahrzeugidentität (erforderlich; ohne sie: leere Liste) | `<cti path>#item:<ordinal>` | `repaint` | Name aus `[item]` |
| `Hofs` | keiner | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | Fahrzeugidentität (erforderlich; ohne sie: leere Liste) | fahrzeugrelativer Quellpfad von `[number]` | `fleet-number` | `null` |
| `Registrations` | Fahrzeugidentität (erforderlich; ohne sie: leere Liste) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | erste Wertzeile (`null` bei free) |
| `Addons` | keiner | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | Kartenidentität (erforderlich; ohne sie: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | Bezeichnung des Einstiegspunkts |
| `SessionProfiles` | keiner | Name des Paketverzeichnisses (die `/predefined-profile`-ID) | `session-profile` oder `session-profile-invalid`, wenn das Paket nicht kompiliert | `name` des Profils oder der `OL_E_SESSION_PROFILE_*`-Code eines ungültigen Pakets |
| `RuntimeModules` | keiner | Modul-ID | `runtime-module` oder `runtime-module-invalid`, wenn es nicht geladen werden kann | `name` des Moduls oder der `OL_E_MODULE_*`-Code |

Ein Einstiegspunkt wird über `PresentedEntrypointIndex` oder über `EntrypointIdentity` (eine Erkennungsidentität oder eine exakte Bezeichnung) ausgewählt. Der Planer löst eine Identität zu der Bezeichnung auf, die OMSI anzeigt (OMSI zeigt nur den ersten Datensatz jeder exakten Bezeichnung an), und der Plan führt sie unter `ResolvedContent` mit der Art `entrypoint` auf; `world.entrypoint-identity` ist `EXPERIMENTAL`, bis es zur Laufzeit validiert ist.

### `ListSessionProfilesAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Zweck | Listet jedes Sitzungsprofil-Paket unter `<root>\.omsilaunch\session-profiles\` mit seinen Voreinstellungen auf, sodass ein Frontend sie anbieten kann, ohne YAML zu parsen. CLI `/list:SessionProfiles`. |
| Rückgabe | Ein `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` pro Paketverzeichnis, sortiert nach `Id`. `Presets` sind `SessionProfilePresetSummary(Index, Id, Name)` in Indexreihenfolge. Jedes Paket wird mit denselben strengen Regeln wie bei einem Start kompiliert (`SessionProfileCompiler`); ein Paket, das nicht kompiliert, wird mit `IsValid = false`, seinem `OL_E_SESSION_PROFILE_*`-Code und seiner Meldung und ohne Voreinstellungen aufgeführt. Kein Profilverzeichnis: leere Liste. |
| Ausgelöst | `OperationCanceledException` beim Eintritt. Kompilierungsfehler werden im Ergebnis gemeldet, nicht ausgelöst. |
| Laufende Sitzung erforderlich | Nein. |
| Verändert OMSI / Dateisystem / Transaktion | Nein / Nein / Keine. |
| Stabilität | `STABLE_BETA` (Offline-Gate `session-profiles.discovery`). |
| Beispiel | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Zweck | Erkennt die unter `plugins\OmsiLaunch.Modules` installierten [Runtime-Module](../../../reference/runtime-modules.md) mit ihrer Integrität und ihren Einstellungen. Nur lesend. Das Laden ist offen: Kein Modul wird gegen eine Liste oder Signatur geprüft. |
| Rückgabe | Ein `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` pro Modulverzeichnis, sortiert nach Verzeichnisname. `Activation` ist `requested`, `content` oder `default`; `IsValid` ist false mit einem `OL_E_MODULE_*`-Code, wenn das Modul nicht geladen werden kann; `Settings` listet die Einstellungen, die das Modul deklariert, als `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)` auf. |
| Ausgelöst | `OperationCanceledException` beim Eintritt. |
| Laufende Sitzung erforderlich | Nein. CLI: `/list:RuntimeModules` (Envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspekt | Detail |
| --- | --- |
| Signatur | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Zweck | Meldet oder vollendet eine veraltete dauerhafte Transaktion (`<root>\.omsilaunch\journal.json`), die ein abgestürzter Eigentümer hinterlassen hat. Hält die Installations-Lease für die Dauer des Aufrufs, sodass niemals unter einer startenden Sitzung wiederhergestellt wird. Öffentliche Capability `session.recover`; CLI `/recovery-status` und `/recover`. |
| Parameter | `installation.RootPath`: das Installationsstammverzeichnis (normalisiert mit `Path.GetFullPath`). `restore`: `false` = nur melden; `true` = wiederherstellen, verifizieren, Journal und Backups löschen. |
| Rückgabe | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = beim Start des Aufrufs existierte ein Journal; `Recovered` = eine Wiederherstellung wurde angefordert, ist gelaufen, und es ist kein Journal mehr vorhanden; `Diagnostics` = Hinweise zur Wiederherstellung (`restore.session-artifact-removed` mit `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), leer, wenn nichts wiederhergestellt wurde. |
| Ausgelöst | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")`, wenn die Lease gehalten wird; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")`, wenn PID + Erstellungszeit + Pfad der ausführbaren Datei aus dem Journal noch zu einem aktiven Prozess passen oder (Journal nach `HandoffCreated` ohne PID) irgendeine `Omsi.exe` aus diesem Stammverzeichnis läuft; `IOException` mit `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` oder einer Verifizierungsmeldung („Restore hash mismatch: ...“, „Restore presence mismatch: ...“); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` bei einem beschädigten Journal; `ArgumentException` bei einem leeren Stammverzeichnis; `OperationCanceledException`. Immer wenn nach Beginn einer Wiederherstellung eine Ausnahme ausgelöst wird, bleibt das Journal erhalten, und der nächste Aufruf wiederholt den Vorgang idempotent. |
| Abbruch | Wird an die Schreibvorgänge für Journal/Backups weitergegeben; ein Abbruch während der Wiederherstellung lässt das Journal ausstehend. |
| Laufende Sitzung erforderlich | Nein (die Methode verweigert den Vorgang, solange ein Eigentümer aktiv ist). |
| Verändert den OMSI-Zustand | Nein. |
| Verändert das Dateisystem | Nur mit `restore == true`: schreibt Originale aus verifizierten Backups zurück (Bytes, Zeitpunkt des letzten Schreibzugriffs, Erstellungszeit, Attribute; schreibgeschützte Originale werden behandelt; Write-Through + Flush; keine `*.omsilaunch.tmp` bleibt zurück), entfernt Sitzungsartefakte, löscht `journal.json` und `backup\<sessionId>`. |
| Transaktion / Wiederherstellung | Vollendet die ausstehende Transaktion (`Restoring` → `Restored` → Journal entfernt). |
| Stabilität | `STABLE_BETA`: Meldepfad und Recovery nach vorzeitigem Beenden (Matrix RV-008), Recovery nach einer fehlgeschlagenen Wiederherstellung (Runtime-Abschluss `F01`), Verweigerung bei aktivem Eigentümer und im Fenster vor der PID (`S04`) sowie zurückgestellte Recovery vor dem Fingerabdruck beim Start (`S05`); siehe [Status der Runtime-Validierung](../status/runtime-validation-status.md). |
| Beispiel | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Vertragstypen

<a id="optional-values-and-semantic-primitives"></a>
### Optionale Werte und semantische Grundtypen

| Typ | Definition | Hinweise |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, statisch `Unset`, statisch `Set(T)` | Unterscheidet „nicht angefordert“ von „mit einem Wert angefordert“. Die JSON-Form ist in der [LaunchSpec-Referenz](launchspec.md) dokumentiert. |
| `Presence` | `Unset` = 0, `Set` = 1 | Byte-Enum. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Wird nur bei `DateTimeMode.Explicit` validiert (Monat 1..12, Tag 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Wird nur bei `DateTimeMode.Explicit` validiert (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### LaunchSpec-Familie

Alle folgenden Records sind in der [LaunchSpec-Referenz](launchspec.md) Eigenschaft für Eigenschaft dokumentiert; diese Tabelle legt das Typinventar fest.

| Typ | Zweck | Stabilität |
| --- | --- | --- |
| `LaunchSpec` | Stamm-Record der Anforderung mit den Accessoren `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, die für optionale Mitglieder mit `null` Standardwerte einsetzen. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (optionale Festlegung von `Omsi.exe`, wird bei der Planung geprüft). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Weltauswahl. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (veralteter Alias von `LastMapState`; niemals „die neueste .osn“). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (berechnet aus `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `EXPERIMENTAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Jeder andere Modus als `Unset` macht den Plan nicht ausführbar. | `PARTIAL` (Plan-Nachweis `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Jeder andere Modus als `Unset` macht den Plan nicht ausführbar. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Jedes gesetzte Feld macht den Plan nicht ausführbar. | `PARTIAL` |
| `EnvironmentSpec` | Acht `IReadOnlyDictionary<string, OptionalValue<string>>`-Gruppen semantischer `options.cfg`-Einstellungen. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; jeder gesetzte Wert macht den Plan nicht ausführbar. | `PARTIAL` |
| `DiagnosticsSpec` | Sechs boolesche Werte; mitgeführt, nicht ausgewertet. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (Alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Herkunft eines kompilierten Sitzungsprofils (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | Sitzungsbezogene [HOF-Verwaltung](hof-management.md): `Operations` (Liste von `HofOperationSpec`). Feld `LaunchSpec.HofManagement`; Accessor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Runtime-Module](../../../reference/runtime-modules.md) für die Sitzung: `Enable` (Modul-IDs), `Disable` (ausgeschlossene Modul-IDs), `Settings` (`"<module id>.<key>"` auf Wert); Accessoren `EffectiveDisable`, `EffectiveSettings`. Feld `LaunchSpec.Modules`; Accessor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | Die Planbewertung eines Moduls: `Id`, `Version`, `Status` (`ELIGIBLE` oder `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` oder `default`), `Settings`; `IsEligible`, `EffectiveSettings`. Konstanten `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Eine wirksame Moduleinstellung: `Key`, `Value`, `Source` (`request`, `settings-file` oder `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Erkennungsansicht eines installierten Moduls; siehe `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Eine Einstellung, die ein Modul deklariert: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Eine HOF-Operation (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Wird von der HOF-Planung ausgelöst (`OmsiLaunch.Core`); `Code` ist ein `OL_E_HOF_*`-Code. Die öffentliche API meldet sie als Plandiagnosen. | `STABLE_BETA` |
| `SessionProfileSummary` | Erkennungsansicht eines Pakets (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Siehe `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Eine Voreinstellung in einer Zusammenfassung (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (mitgeführt, die Wiederherstellung erfolgt immer), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, Standard 180), `ShutdownTimeoutSeconds` (mitgeführt, nicht ausgewertet). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Plan- und Statustypen

| Typ | Felder | Hinweise |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (neue `Guid` pro Plan), `BuildProfileId` (`"Omsi23004"` oder `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (`ContentIdentity`-Liste: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (eindeutige relative Pfade aus `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` mit `STATICALLY_VALIDATED` oder `UNAVAILABLE`; native Start-Capabilities mit `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` oder `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (`Capability`-Einträge für angeforderte, aber nicht unterstützte Funktionen), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Ein öffentlicher Record: Er kann bearbeitet werden oder veralten, weshalb `StartSessionAsync` erneut plant. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, x64-Betriebssystem und x64-Host), `LegacyPlatform` (immer `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (alle gleich `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` ist ein `PublicEvidence`-Wert (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), außer in `SessionPlan.RequiredCapabilities`: Eine Plattform- oder Inhaltsanforderung ist `STATICALLY_VALIDATED` oder `UNAVAILABLE`, eine native Capability `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` oder `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Darstellungsänderungen verwenden die Schlüssel `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (optionale String-Map). | Codes, die mit `OL_E_` beginnen, sind Fehler, `OL_W_` Warnungen, alles andere ist informativ. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Sitzungsdiagnosen enthalten keine Plandiagnosen. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (Empfangszeit beim Host), `Sequence` (ab 1, pro Sitzung), `Data`, `SessionTickMilliseconds` (die monotone Sitzungsuhr des Plugins, als es das Ereignis ausgab; null bei Host-Ereignissen). | Auf die 256 neuesten Ereignisse begrenzt (die ältesten werden verworfen). Plugin-Ereignisse treffen in Reihenfolge über den Telemetrie-Ring ein; durch einen Ring-Überlauf verlorene Ereignisse werden von `telemetry.lost` gezählt. |
| `SessionHandle` | `SessionId`. | Prozesslokal. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Siehe `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Siehe `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Byte-Enum in Deklarationsreihenfolge: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` und `EnteringGameplay` werden vom aktuellen Service nie gesetzt; `Snapshotting` ist ein Übergangszustand und praktisch nicht beobachtbar. Endzustände: `Completed`, `Failed`. Vollständige Semantik: [Sitzungslebenszyklus](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Typen der Runtime-Steuerung

| Typ | Definition | Stabilität |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Statischer Codec, den Host und Plugin für das Mailbox-Envelope verwenden: Magic `0x4F4C5243` („OLRC“), Version 1, 72-Byte-Header in Little-Endian (Magic, Version, Art 1 = Anforderung / 2 = Antwort, Gesamtlänge, Sitzungs-`Guid`, Anforderungs-ID, Nutzdatenlänge, SHA-256 der Nutzdaten), gefolgt von UTF-8-JSON-Nutzdaten. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: öffentlich, weil beide Enden der Bridge ihn gemeinsam verwenden; keine Integrationsschnittstelle; das Format kann sich mit der Protokollversion ändern. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` – was der Host dem Plugin in der speicherabgebildeten Datei `OmsiLaunch.Handoff.<sessionId>` bereitstellt. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)`: ein von der Übergabe getragenes geplantes Modul (`StartupHandoff.Modules`). | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: die aufgelöste Startuhr einer NEW_MAP-Sitzung, die von der Übergabe getragen wird (`StartupHandoff.Start`); ein Null-Feld behält den eigenen Wert von OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Codec: Magic `0x4F4C5348`, Version 7 (liest 3 bis 7; Version 5 hängt die geplanten Module an, Version 6 die wirksamen Einstellungen jedes Moduls, Version 7 Startdatum/-zeit; die Flag-Bits 0x04 und 0x08 tragen `PluginTrace` und `NativeTrace`), 64-Byte-Header mit SHA-256-Integritätsprüfung der Nutzdaten. | `INTERNAL` |
| `TelemetryRingWire` | Ereignisring vom Plugin zum Host in der Abbildung `OMSILAUNCH_TELEMETRY_NAME`: Magic `0x52544C4F` („OLTR“), Version 1, 64 nummerierte Slots zu je 2048 Byte. `Initialize`, `Write`, `IsRing`, `ReadSince` (gibt die Ereignisse nach einer Sequenznummer zurück und wie viele verloren gingen). | `INTERNAL` |

Das Plugin lehnt eine Übergabe ab (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`), sofern nicht `WorldMode` `NewMap` oder `SavedSituation` ist, `HeadlessStart` true ist, `PlayerVehicleEnabled` false ist, beide Datums-/Zeitmodi `Unset` sind und eine gespeicherte Situation eine nicht leere Identität hat. Der Planer erzwingt dieselben Bedingungen früher, sodass ein ausführbarer Plan dies nie auslöst.

<a id="capability-registry-types"></a>
### Typen der Capability-Registry

| Typ | Zweck |
| --- | --- |
| `PublicCapabilityRegistry` | Der kanonische Capability-Katalog. `ProtocolVersion` (`"0.1"`), `All` (jeder `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (die 48 konkreten Operations-IDs, die ein Frontend weiterleiten darf), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (Befehlsroute auf Operations-ID), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (gibt `PublicRuntimeArgumentValidation` zurück), `IsInternalResultKey`. Durchgesetzt von `ExecuteRuntimeAsync`, der CLI, der lokalen Steuerungsebene und dem Plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (ein `PublicEvidence`-Wert), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; abgeleitet `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (nimmt `cursor`/`limit` entgegen, siehe [Paginierung](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Was eine Runtime-Operation ändert: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | Das Nachweisvokabular: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Vollständiger Katalog: [Capabilities](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Erweiterungsmethoden von `D3DRuntimeApi`

Typisierte Wrapper um `ExecuteRuntimeAsync` für die `d3d.*`-Operationen (`EXPERIMENTAL`, `PublicExperimental`-Capability `d3d.texture`). Sie vergeben Anforderungs-IDs aus einem prozessweiten Zähler, der bei 30 000 beginnt, und verwenden standardmäßig ein Timeout von 5 s (außer `GetD3DStatusAsync`, das eines verlangt).

| Methode | Operation | Argumente und Grenzen |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | keine |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | Breite/Höhe 1..4096, Levels 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | Level 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, Breite/Höhe 1..4096, Pixel ≤ 48 KiB (in der Übertragung Base64-kodiert) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | eine wiederholte Freigabe wird mit `OL_E_D3D_RESOURCE_RELEASED` abgelehnt |

Typen: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` mit `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Fehler: Ein fehlgeschlagenes Ergebnis wird als `OmsiRuntimeException(Code, detail)` erneut ausgelöst, wobei `Code` der `ErrorCode` des Ergebnisses ist (oder `OL_E_RUNTIME_OPERATION_FAILED`, wenn dieser fehlt) und die Meldung `"<code>: <Values["detail"]>"` lautet; ein erfolgreiches Ergebnis ohne Werte oder ein unbekannter Gerätezustands-String löst `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)` aus. Alles, was `ExecuteRuntimeAsync` auslöst, wird unverändert weitergegeben. Die Behandlung des Geräte-Resets ist zur Laufzeit validiert: Ein Reset führt das Gerät über `Resetting` zurück zu `Ready` und macht jede aktive Textur ungültig (`OL_E_D3D_STALE_RESOURCE_HANDLE`, Runtime-Abschluss `D01`); der Katalogeintrag `d3d.texture` hält es fest. Der Übergang `Lost` lässt sich von außerhalb des Produkts nicht herbeiführen und ist nur offline abgedeckt.

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
### Typen des Prozessvertrags

| Typ | Inhalt |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Wird nur von der CLI verwendet ([Exitcodes](exit-codes.md)); die API beendet niemals den Prozess. |
| `PublicErrorCategory` | String-Konstanten, die in CLI-/Steuerungs-Fehler-Envelopes verwendet werden: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Eine `const string` pro Code (143: 142 `OL_E_`-Fehler und 1 `OL_W_`-Warnung) sowie `All`, der Katalog aus `PublicErrorDescriptor(Code, Category)`. Kategorien: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Referenz: [Fehlercodes](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Eigenschaft `Code` plus Meldung; wird nur von `D3DRuntimeApi` ausgelöst. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (Installationsidentität und Pfadeingrenzung)

Stabilität: `STABLE_BETA` (reine Funktionen, keine E/A, kein OMSI-Zustand, keine Änderung des Dateisystems, keine Beteiligung an Transaktionen, keine laufende Sitzung erforderlich). Dies ist die einzige Definition, die von der Installations-Lease, dem Namen der lokalen Steuerungs-Pipe, der Eingrenzung der Sitzungsprofil-Assets, der Validierung von Internettexturen-Zielen und dem Modellpfad für den Runtime-Spawn verwendet wird.

| Mitglied | Verhalten |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` ohne abschließende Trennzeichen, außer bei einem Laufwerksstamm (`C:\`), der erhalten bleibt. Löst `.`- und `..`-Segmente auf, behandelt `/` und `\` gleich und fasst wiederholte Trennzeichen zusammen. Löst Junctions oder symbolische Links **nicht** auf. Löst `ArgumentException` bei einem null- oder leeren Stammverzeichnis aus. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` in Großbuchstaben. Lexikalisch gleichwertige Schreibweisen eines Stammverzeichnisses (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) teilen sich einen Schlüssel; verschiedene Stammverzeichnisse (`C:\OMSI-A`, `C:\OMSI-B`) niemals. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Löst `candidate` auf (relativ zu `root` oder absolut) und gibt nur dann `true` zurück, wenn es strikt unterhalb von `root` liegt; `relativePath` ist die kanonische Schreibweise mit `\`. Verwendet Segmente von `Path.GetRelativePath`, sodass ein Geschwisterverzeichnis wie `C:\OMSI-A\x` niemals innerhalb von `C:\OMSI` liegt; das Stammverzeichnis selbst, andere Volumes und `..`-Ausbrüche ergeben `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Teilt an `/` und `\` und verwirft leere Segmente. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Sitzungsprofile (`OmsiLaunch.Core`)

Stabilität: `EXPERIMENTAL`. Diese Typen kompilieren ein YAML-[Sitzungsprofil](session-profiles.md) (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, Schema `omsilaunch.session-profile/v1`) zu einer `LaunchSpec`. Die CLI-Option `/predefined-profile:<id> /predefined-profile-index:<n>` verwendet genau diese Aufrufe; ein Integrator kann sie verwenden, um ein Profil über die API zu starten.

| Mitglied | Verhalten |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Liest und validiert das Paket. `id` muss ein einfacher Verzeichnisname sein (sonst `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` liegt im Bereich 1..99 (`MinPresetIndex`..`MaxPresetIndex`; sonst `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Fehlende Datei: `OL_E_SESSION_PROFILE_NOT_FOUND`; größer als `MaxBytes` (256 KiB), ungültiges YAML, Anker, unbekannte Schlüssel oder eine `id`, die vom Verzeichnisnamen abweicht: `OL_E_SESSION_PROFILE_INVALID`; anderes `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Asset-Pfade sind auf das Paket beschränkt (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Jeder Fehler ist eine `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Gibt `baseline` mit angewendetem Profil zurück: für `NewMap` den `new`-Block des Profils (Karte, Einstiegspunkt sowie etwaige Datums-/Zeit-/Jahres-/Wetterangaben); die `settings` der Voreinstellung über `Environment.General` zusammengeführt; `Presentation`, `InternetTextures` und `Behavior` der Voreinstellung, sofern vorhanden; und `SessionProfile` = `profile.Metadata`. Für `NewMap` prüft die Methode außerdem die Karte gegen die `compatibility`-Liste des Profils (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | Die Kompatibilitätsprüfung für die anderen Modi (für `SavedSituation` wird die Karte aus der `.osn` gelesen). Die CLI ruft sie auf, nachdem die endgültige `WorldSpec` erstellt wurde. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` und die zulässigen Schlüssel pro YAML-Mapping. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | Das geladene Paket; `Preset` ist nur die ausgewählte Voreinstellung. |
| `SessionProfileException(string code, string message)` | `IOException` mit `Code` (einer der `OL_E_SESSION_PROFILE_*`-Codes); die Meldung lautet `"<code>: <message>"`. |

Die CLI lehnt zusätzlich Befehlszeilen-Flags ab, die mit dem Profil in Konflikt stehen (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); diese Prüfung ist nicht Teil des Compilers. Die vollständige Zusammenführungsreihenfolge finden Sie unter [Sitzungsprofile](session-profiles.md#precedence-and-override-conflicts).

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
### Plattformtypen (`OmsiLaunch.Process`)

| Typ | Stabilität | Verwendung |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` als Parametertyp des Konstruktors von `OmsiLaunchService` | Erkennt die Plattform, prüft die Beschreibbarkeit, startet, beobachtet, beendet `Omsi.exe` und wartet darauf. Übergeben Sie `new CurrentWindowsX64Platform()`; eine eigene Implementierung wird nicht unterstützt. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | Die einzige Implementierung: Windows-x64-Host, `CreateProcessW` für `Omsi.exe`, `TerminateProcess` für den kanonischen Stopp. Ihre Methoden werden vom Service aufgerufen; Integratoren erzeugen sie nur. Mitglieder (gemeinsam mit `IRuntimePlatform`): `Detect(root)` liefert die `RuntimePlatformInfo` des Plans; `ValidateCurrent(info)` löst `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` aus, wenn der Host keine Sitzung ausführen kann; `IsInstallationWritable(root)` liegt `OL_E_INSTALLATION_NOT_WRITABLE` zugrunde; `StartAsync(request, sha256)` erzeugt `Omsi.exe` und erfasst die Prozessidentität (PID, Erstellungszeit, Pfad und den vom Service berechneten Hash; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` und `Terminate` beobachten und beenden den Prozess. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | In der Assembly öffentlich, weil der Service und die Tests sie gemeinsam verwenden. Keine Integrationsschnittstelle; `LaunchedProcess` kapselt intern die Prozess- und Thread-Handles von OMSI (sie sind keine öffentlichen Mitglieder) und wird niemals von `IOmsiLaunch` zurückgegeben. |

<a id="thread-safety"></a>
## Threadsicherheit

- `OmsiLaunchService` ist für gleichzeitige Aufrufe auf verschiedenen Sitzungen sicher: Sitzungen liegen in einem `ConcurrentDictionary`, und jede sitzungsbezogene Änderung erfolgt unter der privaten Sperre der Sitzung.
- Gleichzeitige Aufrufe auf derselben Sitzung sind sicher, werden aber dort serialisiert, wo es darauf ankommt: `ExecuteRuntimeAsync` nimmt ein Gate pro Sitzung, sodass ein zweiter Befehl auf den ersten wartet (sein Timeout beginnt, wenn er bereitgestellt wird).
- Der Supervisor läuft als Thread-Pool-Task (`Task.Run`) ab dem Moment, in dem `StartSessionAsync` zurückkehrt, bis sich die Sitzung im Endzustand befindet; er fragt Telemetrie und Prozess alle 100 ms ab. Aufrufer führen niemals Supervisor-Code aus.
- `StopAsync` und `GetStatusAsync` werden synchron abgeschlossen und dürfen von jedem Thread aufgerufen werden, auch innerhalb eines `ProcessExit`-Handlers (die CLI tut dies mit einem Budget von 4 s).
- Kein API-Aufruf ist an einen Thread gebunden; keiner benötigt einen Synchronisierungskontext.

<a id="what-is-not-in-the-api"></a>
## Was nicht Teil der API ist

- Kein `IntPtr`, kein `nint`, keine Win32-Handles, keine nativen Adressen, keine VMT-Zeiger und keine Prozessobjekte. Ergebniswerte, deren Schlüssel mit `internal_` beginnen oder mit `_address`, `_pointer`, `_vmt` enden, werden entfernt, bevor ein Ergebnis `ExecuteRuntimeAsync` verlässt.
- Keine `internal.*`-Runtime-Operationen: `internal.road-vehicles.make-basic` ist in der Registry `InternalOnly` und liefert über API und CLI `OL_E_RUNTIME_OPERATION_UNKNOWN`.
- Keine direkten Lese- oder Schreibzugriffe auf den OMSI-Speicher, kein Dateizugriff auf die Installation über das hinaus, was eine `LaunchSpec` deklariert.
- Keine prozessübergreifenden Handles: Die [lokale Steuerungsebene](local-control.md) ist der einzige prozessübergreifende Weg, und sie akzeptiert ausschließlich `session.status`, `session.events`, `session.stop` und `runtime.execute`.
- Keine Anwendung des Spielerfahrzeugs beim Start. Kooperatives Herunterfahren, `LAST_MAP_STATE`, Startdatum/-zeit/-wetter und Overlays für Tastatur-/Controller-Dokumente sind `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Stabilitätsübersicht

| Schnittstelle | Stabilität |
| --- | --- |
| Konstruktor von `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transport von `ExecuteRuntimeAsync`; `PublicStableBeta`-Operationen | `STABLE_BETA` |
| `PublicExperimental`-Operationen, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| Datums-/Zeit-/Wetter-/Eingabemitglieder der Spezifikation, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| Mitglieder der Spezifikation für das Spielerfahrzeug | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, Implementierungen von `IRuntimePlatform`, alle Implementierungs-Assemblies | `INTERNAL` |
| `WorldMode.LastSituation`, `internal.*`-Operationen | `UNAVAILABLE` |
