# Dokumentacja publicznego API (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=03d2b00af2df06cdc28d5fbbec294f9f480584c4817b1dfe8b805a914621f734 -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/public-api.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona jest normatywną dokumentacją zarządzanego publicznego API OmsiLaunch 0.1.0-beta4: zestawu `OmsiLaunch.Api` (kontrakty) oraz punktu wejścia dla integratorów `OmsiLaunchService` w `OmsiLaunch.Core`. Opisuje wyłącznie to, co robi bieżący kod. Wszystko, co integrator może wywołać, otrzymać lub zaobserwować, jest tu wymienione wraz z poziomem stabilności; to, czego tu nie wymieniono, nie jest powierzchnią integracji.

Generowany [spis publicznego API](public-api-inventory.md) wymienia każdy publiczny typ i składową `OmsiLaunch.Api`, `OmsiLaunch.Core` i `OmsiLaunch.Process` wraz z sygnaturą i stabilnością; bramka dokumentacji kończy się niepowodzeniem, gdy spis i zestawy się różnią. Ta strona objaśnia semantykę.

Powiązane strony: [dokumentacja LaunchSpec](launchspec.md), [kody błędów](errors.md), [cykl życia sesji](../concepts/session-lifecycle.md), [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md), [sterowanie runtime](runtime-control.md), [możliwości](capabilities.md), [lokalna płaszczyzna sterowania](local-control.md), [kody wyjścia](exit-codes.md), [stan weryfikacji w runtime](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Słownik stabilności

| Poziom | Znaczenie na tej stronie |
| --- | --- |
| `STABLE_BETA` | Kontrakt jest zamrożony dla linii protokołu 0.1, a ścieżka jest zweryfikowana w runtime w `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Możliwy do wywołania i przetestowany, ale kontrakt lub dowody z runtime mogą się zmienić, zanim stanie się stabilny. |
| `PARTIAL` | Obecny w kontrakcie; zaimplementowana lub zweryfikowana jest tylko część zachowania (tekst określa, która). |
| `INTERNAL` | Publiczny w zestawie z przyczyn technicznych (mostek współdzieli typ), ale nie jest powierzchnią integracji; może się zmienić bez uprzedzenia. |
| `UNAVAILABLE` | Obecny w kontrakcie, ale odrzucany przez bieżący build. |

<a id="assembly-overview"></a>
## Przegląd zestawów

| Zestaw | Rola dla integratorów |
| --- | --- |
| `OmsiLaunch.Api` | Czyste kontrakty: rekordy, typy wyliczeniowe, `IOmsiLaunch`, rejestr możliwości, katalog błędów, formaty przesyłowe (wire), funkcje pomocnicze D3D. Nie zawiera `IntPtr`, `nint`, uchwytów Win32, adresów natywnych ani obiektów procesów. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (implementacja `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` i `CurrentWindowsX64Platform` (jedyny adapter platformy), `InstallationLease`. Potrzebne do utworzenia usługi. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Zestawy implementacyjne. Ich typy publiczne są dla integratorów `INTERNAL`. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Punkt wejścia: `OmsiLaunchService` i `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parametr | Poprawna wartość | Niepoprawna / domyślna |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (przestrzeń nazw `OmsiLaunch.Process`). Wykrywa platformę, tworzy proces OMSI za pomocą `CreateProcessW`, czeka na niego i go kończy. | Nie jest dostarczana żadna inna implementacja. Własna implementacja `IRuntimePlatform` ma status `INTERNAL`. |
| `PluginBuildDirectory` | Katalog zawierający pliki referencyjne zestawu plików stałej wtyczki (closure): `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` oraz każdy `OmsiLaunch.*.dll` zarządzanego zestawu plików (musi obejmować `OmsiLaunch.Plugin.dll`). W zainstalowanym pakiecie jest to `<package>\plugins`. | Brak katalogu lub pliku: `PlanSessionAsync` zwraca plan niemożliwy do uruchomienia z `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Ścieżka pliku `OmsiLaunch.Native.x86.dll` (w pakiecie: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Jak wyżej. |
| `ReleaseManifestPath` | `release-manifest.json` obok `OmsiLaunch.exe`, jeśli istnieje. Dostarcza oczekiwany SHA-256 każdego pliku w `plugins/` (`plugin.integrity.reference = manifest`). | `null` (układ deweloperski): zainstalowane pliki są sprawdzane jedynie pod kątem obecności i spójności z referencyjnym zestawem plików (`plugin.integrity.reference = self`). Niepoprawny manifest: `OL_E_RELEASE_MANIFEST_INVALID`. |

Usługa odczytuje te ścieżki przy każdym wywołaniu `PlanSessionAsync` i `StartSessionAsync`; nigdy nie kopiuje, nie przygotowuje ani nie usuwa plików wtyczki (zob. [stała wtyczka](../concepts/permanent-plugin.md)). CLI tworzy usługę dokładnie w ten sposób (`tools/OmsiLaunch.Cli/Program.cs`):

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

Należy utworzyć jedną usługę na proces i ją współdzielić. Stabilność: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Zasady własności sesji

| Zasada | Szczegóły |
| --- | --- |
| Jeden właściciel na instalację | `StartSessionAsync` pozyskuje dzierżawę instalacji – nazwany semafor `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>` – i utrzymuje ją, dopóki nadzorca nie przywróci instalacji. Drugie uruchomienie na tym samym katalogu głównym z dowolnego procesu tej samej sesji logowania kończy się błędem `OL_E_INSTALLATION_BUSY` (zgłaszanym jako sesja `Failed`, zob. `StartSessionAsync`). Dzierżawa obowiązuje w obrębie sesji logowania, nie między sesjami logowania, i nie jest zwalniana, dopóki inny proces ma do niej uchwyt (zaakceptowane ryzyko). |
| Uchwyty są lokalne dla procesu | `SessionHandle` opakowuje `Guid` sesji. Ma znaczenie wyłącznie dla instancji `OmsiLaunchService`, która go zwróciła. Uchwyt zbudowany ze znanego `Guid` w innym procesie (lub w innej instancji usługi) powoduje `KeyNotFoundException`. Sterowanie międzyprocesowe odbywa się przez [lokalną płaszczyznę sterowania](local-control.md), a nie przez uchwyty. |
| Zawsze wywoływać `CloseAsync` | Od wywołania `StartSessionAsync` proces jest właścicielem trwałej transakcji. `CloseAsync` w razie potrzeby żąda kanonicznego zatrzymania, czeka na nadzorcę (zakończenie procesu, dokładne przywrócenie, zwolnienie dzierżawy) i zapomina sesję. Należy je wywołać na każdej ścieżce wyjścia, także po stanie `Failed`. Bez niego wpis sesji pozostaje w pamięci; samo przywrócenie jest wykonywane przez nadzorcę niezależnie od tego. |
| Nieudane sesje nadal są sesjami | Uruchomienie, które kończy się niepowodzeniem po powrocie z `StartSessionAsync`, zgłasza `SessionState.Failed`; uchwyt pozostaje ważny dla `GetStatusAsync`/`WaitForAsync` aż do `CloseAsync`. |
| Plany są sprawdzane ponownie | `StartSessionAsync` ponownie oblicza skrót `Omsi.exe` i ponownie planuje specyfikację; plan, który nie jest już możliwy do uruchomienia, jest odrzucany z `OL_E_PLAN_NOT_RUNNABLE`. |

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

Wspólne zasady dla każdej metody:

- Nieznane lub już zamknięte uchwyty zgłaszają `KeyNotFoundException` („Unknown OmsiLaunch session.”).
- Żadna metoda poza `ExecuteRuntimeAsync` nie wymaga sesji w stanie Running.
- Wyjątki niosące kod OmsiLaunch umieszczają ten kod na początku `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). CLI wyodrębnia kody z komunikatów w ten sam sposób (`CliProgram.Classify`).
- Obsługiwana instalacja: OMSI 2. Możliwości natywne są rozwiązywane i weryfikowane w procesie dla każdego buildu; możliwość niedostępna w danym buildzie kończy się błędem `OL_E_CAPABILITY_UNRESOLVED`. Zob. [zgodność](compatibility.md).

<a id="complete-minimal-example"></a>
### Kompletny minimalny przykład

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

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Kompiluje `LaunchSpec` do `SessionPlan` bez uruchamiania OMSI: waliduje specyfikację, wykrywa platformę, oblicza odcisk `Omsi.exe`, rozwiązuje tożsamości zawartości, wyznacza planowane modyfikacje plików, wymienia wymagane i nieobsługiwane możliwości oraz rozstrzyga `IsRunnable`. Publiczna możliwość `session.plan`. |
| Parametry | `spec`: w pełni wypełniony `LaunchSpec` (zob. [dokumentacja LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (wszystkie osiem słowników) i `Behavior` muszą być różne od null; składowe opcjonalne mogą mieć wartość `null`. `RootPath` powinien być ścieżką bezwzględną katalogu; pusty katalog główny jest rejestrowany jako `OL_E_INSTALLATION_NOT_FOUND`, ale sonda platformy dla pustej ścieżki zgłasza `ArgumentException`, zanim plan zostanie zwrócony, dlatego nigdy nie należy przekazywać pustego katalogu głównego. |
| Zwraca | `SessionPlan` z nowym `SessionId`, `BuildProfileId` = znany układ (`"Omsi23004"`) albo `"unrecognized"` dla buildu OMSI 2 bez znanego układu, wejściowym `Spec`, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (ścieżki docelowe `plugins\OmsiLaunch.*` oraz `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (jeden `PlannedModule` na każdy zażądany lub aktywowany przez zawartość [moduł runtime](runtime-modules.md); akcesor `EffectivePlannedModules`). `IsRunnable` ma wartość `true` dokładnie wtedy, gdy żaden kod komunikatu diagnostycznego nie zaczyna się od `OL_E_`. Informacyjne komunikaty diagnostyczne (`plugin.integrity.reference` z komunikatem `self` lub `manifest`, `session_profile.selected`) nigdy nie czynią planu niemożliwym do uruchomienia. |
| Błędy przenoszone w wyniku | Każdy błąd planowania jest komunikatem diagnostycznym, a nie wyjątkiem: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (komunikat zawiera kod ekranu startowego/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (zainstalowany zestaw plików wtyczki w `plugins\` jest weryfikowany względem manifestu wydania na etapie planowania), `OL_E_RUNTIME_ARTIFACT_MISSING` (komunikat może zawierać `OL_E_RELEASE_MANIFEST_INVALID`). Pełne warunki: [reguły walidacji LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Zgłaszane wyjątki | `OperationCanceledException`, jeśli token jest już anulowany przy wejściu (jedyny punkt kontrolny); `ArgumentException`/`NotSupportedException` dla składniowo niepoprawnych ścieżek katalogu głównego; `NullReferenceException`/`ArgumentNullException` dla wymaganych składowych o wartości null; `System.Text.Json.JsonException` dla składniowo niepoprawnego manifestu wydania. |
| Anulowanie | Sprawdzane raz, przy wejściu. Dalsze planowanie to synchroniczne operacje na systemie plików. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje stan OMSI | Nie. |
| Modyfikuje system plików | Nie (odczytuje `Omsi.exe`, pliki zawartości, zestaw plików wtyczki, manifest). Wartości ustawień nie są tu walidowane (tylko istnienie klucza i możliwość zapisu); niepoprawna wartość powoduje błąd przy uruchomieniu z `OL_E_INVALID_SETTING_VALUE`. |
| Transakcja / przywracanie | Brak. |
| Ograniczenia | Zażądanie dowolnego trybu `Date`/`Time`/`Year` innego niż `Unset`, dowolnego trybu `Weather` innego niż `Unset`, dowolnego pola `PlayerVehicle`, dokumentów `Input`, `EntrypointIdentity` lub `WorldMode.LastMapState` powoduje w tym buildzie `OL_E_CAPABILITY_UNAVAILABLE` i plan niemożliwy do uruchomienia (wpisy w `UnsupportedRequestedFeatures` niosące dowody ze swojego wpisu katalogu, `UNAVAILABLE` lub `PARTIAL`). |
| Stabilność | `STABLE_BETA`. |
| Przykład | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Uruchamia transakcyjną, zarządzaną sesję OMSI na podstawie planu możliwego do uruchomienia: pozyskuje dzierżawę instalacji, odzyskuje pozostawiony dziennik, waliduje zestaw plików stałej wtyczki, tworzy migawkę (snapshot) i nakłada nakładki (overlay) na pliki sesji, tworzy przekazanie startowe (startup handoff), slot telemetrii i skrzynkę runtime (mailbox), uruchamia `Omsi.exe`, zapisuje proces w dzienniku i przekazuje sesję nadzorcy działającemu w tle. Publiczna możliwość `session.start`. |
| Parametry | `plan`: `SessionPlan` z `IsRunnable == true`. Specyfikacja zawarta w planie jest planowana ponownie; z planu wywołującego zachowywany jest tylko `plan.SessionId`. `plan.Spec.Behavior.StartupTimeoutSeconds` musi mieścić się w zakresie 1..600. |
| Zwraca | `SessionHandle(plan.SessionId)`, gdy tylko `Omsi.exe` zostanie utworzony i zapisany (stan `WaitingForPlugin`) albo gdy tylko ścieżka uruchamiania zakończy się niepowodzeniem (stan `Failed`). Nie czeka na rozgrywkę; należy użyć `WaitForAsync(session, SessionState.Running, ...)`. |
| Zgłaszane wyjątki | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")`, gdy `plan.IsRunnable` ma wartość false; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")`, gdy ponowny plan nie jest możliwy do uruchomienia (na przykład zmienił się `Omsi.exe`, usunięto zawartość, brakuje zestawu plików wtyczki); `InvalidOperationException("Duplicate session id.")`, gdy sesja o tym samym identyfikatorze jest nadal zarejestrowana (najpierw należy wywołać `CloseAsync`); `ArgumentOutOfRangeException`, gdy `StartupTimeoutSeconds` wykracza poza 1..600; `OperationCanceledException` przy anulowaniu przed ponownym planowaniem lub w jego trakcie; a także wszystko, co zgłasza `PlanSessionAsync`. We wszystkich przypadkach zgłoszenia wyjątku żadna sesja nie jest rejestrowana. |
| Błędy przenoszone w wyniku | Każdy błąd po ponownym planowaniu jest przechwytywany wewnątrz ścieżki uruchamiania: sesja jest rejestrowana, jej stan to `Failed`, a jej komunikaty diagnostyczne zawierają `OL_E_START_SESSION`, którego komunikatem jest komunikat wewnętrzny (rozpoczynający się od wewnętrznego kodu, jeśli taki istnieje): `OL_E_INSTALLATION_BUSY` (dzierżawa zajęta lub zapisany w dzienniku proces OMSI nadal działa), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (tylko gdy odroczona ponowna próba z nakładkami tej sesji nadal nie może udowodnić własności), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. Sprzątanie może dodać `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (zakończenie OMSI niepotwierdzone; dziennik zachowany) lub `OL_E_RESTORE_FAILED`. Późniejsze błędy zgłasza nadzorca (zob. [cykl życia sesji](../concepts/session-lifecycle.md)). |
| Anulowanie | Przed ponownym planowaniem lub w jego trakcie: zgłasza wyjątek. Później token jest przekazywany do transakcji i tworzenia procesu; anulowanie na tym etapie jest traktowane jak każdy błąd uruchamiania (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), proces (jeśli został utworzony) jest kończony, a instalacja przywracana. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje stan OMSI | Tak: tworzy proces OMSI ze zmiennymi środowiskowymi `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Modyfikuje system plików | Tak, wewnątrz katalogu głównego instalacji: `.omsilaunch\diagnostics\<sessionId>-host.log` (retencja: 50 najnowszych sesji), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (kopiowane jednorazowo dla zarządzanego ekranu startowego), nakładki sesji (poprawki `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), usunięcia na czas sesji (cele ITX, `Texture\standard.ipr`, `closecheck`) oraz trwałe usunięcie istniejącego wcześniej, pozostawionego pliku `closecheck`, gdy `SuppressStaleClosecheckWarning` ma wartość true (komunikat diagnostyczny `closecheck.stale-removed`). |
| Transakcja / przywracanie | Otwiera transakcję (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Każda ścieżka wyjścia z sesji kończy się przywróceniem. Zob. [transakcje i odzyskiwanie](../concepts/transactions-and-recovery.md). |
| Ograniczenia | Do rozgrywki docierają tylko `WorldMode.NewMap` z `PresentedEntrypointIndex` oraz `WorldMode.SavedSituation`. `WorldMode.LastMapState` ma status `UNAVAILABLE`. Żądania pojazdu gracza nigdy nie docierają do tej metody, ponieważ już na etapie planowania są niemożliwe do uruchomienia. |
| Stabilność | `STABLE_BETA` (cykle życia NEW_MAP i SAVED_SITUATION są zweryfikowane w runtime). |
| Przykład | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Odczytuje semantyczny stan cyklu życia, zebrane dotąd komunikaty diagnostyczne i ograniczoną listę zdarzeń runtime. Publiczna możliwość `session.status`. |
| Parametry | `session`: uchwyt zwrócony przez `StartSessionAsync` i jeszcze niezamknięty. |
| Zwraca | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: niezmienna migawka (tablice są kopiowane pod blokadą sesji). `RuntimeEvents` nigdy nie ma wartości `null` dla aktywnej sesji. |
| Zgłaszane wyjątki | `KeyNotFoundException` dla nieznanych/zamkniętych uchwytów. Poza tym nigdy nie zgłasza wyjątków. |
| Anulowanie | Token jest ignorowany (wywołanie kończy się synchronicznie). |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje OMSI / system plików / transakcję | Nie / Nie / Brak. |
| Stabilność | `STABLE_BETA`. |
| Przykład | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Odpytuje (co 100 ms), dopóki sesja nie znajdzie się w stanie `state` lub w stanie końcowym (`Completed`, `Failed`) albo nie upłynie limit czasu; następnie zwraca bieżący stan. |
| Parametry | `state`: dowolny `SessionState`. Oczekiwanie na stan przejściowy, który już minął (lub nigdy nie jest ustawiany, zob. [cykl życia sesji](../concepts/session-lifecycle.md)), trwa do stanu końcowego lub upływu limitu czasu. `timeout`: dowolny nieujemny `TimeSpan` lub `Timeout.InfiniteTimeSpan`. |
| Zwraca | Stan w chwili zakończenia oczekiwania. Po przekroczeniu limitu czasu zwracany jest stan, a nie wyjątek: `State` należy sprawdzić samodzielnie. Oczekiwanie na `Running`, które kończy się stanem `Failed`, zwraca natychmiast wynik z komunikatami diagnostycznymi błędu. |
| Zgłaszane wyjątki | `KeyNotFoundException`; `OperationCanceledException`, gdy token wywołującego zostanie anulowany (propaguje się tylko anulowanie przez wywołującego; wewnętrzny limit czasu nie). |
| Anulowanie | Token wywołującego jest respektowany przy każdym takcie co 100 ms. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje OMSI / system plików / transakcję | Nie / Nie / Brak. |
| Stabilność | `STABLE_BETA`. |
| Przykład | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Żąda kanonicznego zatrzymania. Ustawia flagę zatrzymania i natychmiast wraca; nadzorca zauważa flagę w swojej pętli 100 ms, wywołuje `TerminateProcess` na `Omsi.exe`, czeka na zakończenie, oznacza dziennik jako `ProcessExited`, przywraca każdy plik należący do sesji i zwalnia dzierżawę. Jest to wymuszone zakończenie: własna procedura zamykania OMSI nie jest wykonywana, a OMSI nie zapisuje ponownie `options.cfg` przy wyjściu (celowo – chroni to transakcję). Kooperacyjne zamykanie przez `WM_CLOSE` nie jest zaimplementowane (decyzja produktowa; w domknięciu weryfikacji w runtime OMSI nie zamknął się w ciągu 30 s od `WM_CLOSE`, `L05b`). Publiczna możliwość `session.stop`. |
| Parametry | `session`. |
| Zwraca | Zakończone zadanie; nie czeka na zakończenie procesu ani przywrócenie. Do zaobserwowania zakończenia należy użyć `WaitForAsync(session, SessionState.Completed, ...)`. |
| Zgłaszane wyjątki | `KeyNotFoundException`. |
| Anulowanie | Token jest ignorowany. |
| Wymagana sesja w stanie Running | Nie. Operacja idempotentna; zatrzymanie zażądane przed uruchomieniem nadzorcy jest respektowane, gdy tylko nadzorca się uruchomi; zatrzymanie sesji w stanie końcowym nic nie robi. |
| Modyfikuje stan OMSI | Tak: kończy proces OMSI (kod wyjścia 1). |
| Modyfikuje system plików | Pośrednio: wyzwala przywrócenie, usunięcie dziennika i usunięcie kopii zapasowych przez nadzorcę. |
| Transakcja / przywracanie | Wyzwala `ProcessExited` → `Restoring` → `Restored`. Zmiany po stronie runtime wprowadzone przez `ExecuteRuntimeAsync` (zapisy zegara, dodane pojazdy, zmienne skryptów, tekstury D3D) nie są przywracane; znikają razem z procesem. |
| Stabilność | `STABLE_BETA`. |
| Przykład | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Zwalnia uchwyt konsumenta bez porzucania transakcji: jeśli sesja nie jest w stanie końcowym, żąda kanonicznego zatrzymania; następnie czeka na zadanie cyklu życia nadzorcy (zakończenie procesu, przywrócenie, zwolnienie dzierżawy); potem zapomina sesję. |
| Parametry | `session`. |
| Zwraca | Kończy się, gdy sesja jest w stanie końcowym i została usunięta. Po powrocie uchwyt jest nieznany (`KeyNotFoundException` przy każdym kolejnym wywołaniu, w tym przy drugim `CloseAsync`). |
| Zgłaszane wyjątki | `KeyNotFoundException`; `OperationCanceledException`, jeśli wywołujący anuluje oczekiwanie na nadzorcę. W takim przypadku sesja nie jest usuwana, a nadzorca działa dalej; należy ponownie wywołać `CloseAsync`. |
| Anulowanie | Dotyczy tylko oczekiwania; nigdy nie anuluje przywracania. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje stan OMSI | Tak, gdy sesja jest nadal aktywna (tak samo jak `StopAsync`). |
| Modyfikuje system plików | Pośrednio (przywracanie przez nadzorcę). |
| Transakcja / przywracanie | Gwarantuje, że transakcja zostanie doprowadzona do końca, zanim uchwyt zostanie zwolniony (gdy nadzorca został uruchomiony). W przypadku sesji, która zakończyła się niepowodzeniem przed uruchomieniem nadzorcy, ścieżka uruchamiania już wykonała przywrócenie lub zgłosiła `OL_E_RESTORE_DEFERRED`. |
| Stabilność | `STABLE_BETA`. |
| Przykład | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Wykonuje jedną publiczną operację runtime wewnątrz działającego procesu OMSI przez jednozadaniową (single-flight) skrzynkę runtime sesji (plik mapowany w pamięci, 64 KiB, żądanie powiązane z identyfikatorem sesji i identyfikatorem żądania). Wtyczka wykonuje operację w wątku interfejsu użytkownika OMSI. Katalog operacji: [sterowanie runtime](runtime-control.md) i [możliwości](capabilities.md). |
| Parametry | `command.SessionId` musi być równy `session.SessionId`. `command.RequestId`: `ulong` wybierany przez wywołującego; należy używać ściśle rosnącego licznika w obrębie procesu (funkcje pomocnicze D3D zaczynają od 30 000, właściciel CLI od 10 001/50 000). `command.Operation`: publiczny identyfikator operacji z `PublicCapabilityRegistry.PublicRuntimeOperationIds` (na przykład `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: wartości tekstowe indeksowane nazwami porządkowymi; wymagane nazwy dla każdej operacji pochodzą z `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: mierzony od chwili umieszczenia żądania w skrzynce (oczekiwanie w kolejce za innym trwającym poleceniem nie jest wliczane). CLI używa 5 s (15 s dla `road-vehicles.spawn`) jako właściciel oraz 8 s / 30 s jako klient. |
| Kolejność sprawdzeń | 1. Walidacja w rejestrze (przed wyszukaniem sesji): nieznana operacja lub operacja `internal.*` → wynik `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; brak wymaganego argumentu (nieobecny lub złożony z białych znaków) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Wyszukanie sesji → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Stan inny niż `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Żądanie przez skrzynkę runtime. 6. Wartości wyniku, których klucz zaczyna się od `internal_` lub kończy na `_address`, `_pointer`, `_vmt`, są usuwane. |
| Zwraca | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. W razie powodzenia `Values` zawiera semantyczne ciągi znaków operacji (opisane dla każdej operacji w [sterowaniu runtime](runtime-control.md)). |
| Błędy przenoszone w wyniku | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (rejestr); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (wynik wtyczki przekroczył rozmiar skrzynki; wyniki w postaci list ograniczonych są zamiast tego skracane z `truncated=true`); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` z kluczem pochodnym); każdy kod `OL_E_D3D_*` (z `Values["detail"]` i `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (natywna możliwość operacji nie jest rozwiązana w działającym buildzie); oraz każdy typowany błąd po stronie wtyczki z własnym kodem w `ErrorCode` i komunikatem w `Values["detail"]` (Beta 4; tylko błąd bez skatalogowanego kodu to `OL_E_RUNTIME_OPERATION_FAILED`). Kody przekazywane w ten sposób: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (sprawdzenia po stronie wtyczki), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Zob. [kody błędów](errors.md). |
| Zgłaszane wyjątki | `KeyNotFoundException`; `InvalidOperationException` z `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (skrzynka już zwolniona przez nadzorcę), `OL_E_RUNTIME_CHANNEL_BUSY` (slot nadal zawiera porzucone żądanie), `OL_E_RUNTIME_REQUEST_ID_REUSED` (w slocie nadal znajduje się nieaktualna odpowiedź dla tego samego identyfikatora żądania); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (uszkodzona, obca lub niepasująca odpowiedź); `ArgumentOutOfRangeException`, gdy zserializowane żądanie przekracza rozmiar skrzynki; `OperationCanceledException`. |
| Anulowanie | Respektowane podczas oczekiwania na bramkę sesji oraz co 20 ms podczas odpytywania o odpowiedź. Anulowanie w trakcie wykonywania nie resetuje slotu: następne wywołanie w tej sesji może kończyć się błędem `OL_E_RUNTIME_CHANNEL_BUSY`, dopóki wtyczka nie opublikuje odpowiedzi (która jest wtedy odrzucana jako nieaktualna). Zalecany jest limit czasu; przekroczenie limitu czasu resetuje slot, a spóźniona odpowiedź jest wykrywana i odrzucana. |
| Wymagana sesja w stanie Running | Tak (`SessionState.Running`); w przeciwnym razie zgłaszany jest `OL_E_SESSION_NOT_RUNNING`. Skrzynka istnieje, dopóki nadzorca nie zwolni jej podczas przywracania. |
| Modyfikuje stan OMSI | Zależy od operacji: operacje `Read` nie modyfikują; operacje `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) modyfikują stan wewnątrz procesu, który nie jest przywracany. |
| Modyfikuje system plików | Brak zapisów po stronie hosta. OMSI może w konsekwencji zapisywać własne pliki (nieśledzone). |
| Transakcja / przywracanie | Brak. |
| Ograniczenia | Jedno trwające polecenie na sesję (wywołania w tej samej sesji są serializowane). Żądanie i odpowiedź są każde ograniczone do 64 KiB minus 8 bajtów; dane pikseli D3D do 48 KiB. `internal.road-vehicles.make-basic` ma status `INTERNAL` i jest nieosiągalna. `weather.set` i `weather.metar.set` mają status `EXPERIMENTAL`. `timetable.logs.read` nie jest ograniczona i przy dużych rozkładach jazdy może zwrócić `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. `camera.lock` ma status `EXPERIMENTAL`; wymaga pojazdu gracza i jest zweryfikowana w runtime (`CAM01`), choć ciąg `RuntimeValidation` w rejestrze nadal brzmi `STATICALLY_VALIDATED`. Uchwyty (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) mają zasięg sesji. |
| Stabilność | Transport i kontrakt: `STABLE_BETA`; stabilność poszczególnych operacji wynika z `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`) z powyższymi wyjątkami. |
| Przykład | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Zwraca katalog możliwości oceniony dla instalacji: jeden `Capability(Name, Available, EvidenceState, Reason)` na każdy wpis `PublicCapabilityRegistry`, który nie jest `InternalOnly`. `Name` to identyfikator katalogu, `EvidenceState` to dowody z katalogu (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` jest obliczane na podstawie platformy, sprawdzenia OMSI 2, klasyfikacji oraz możliwości natywnych, których wpis wymaga (zob. [możliwości](capabilities.md#getcapabilitiesasync)). |
| Parametry | `installation.RootPath`: sondowana instalacja (platforma, `Omsi.exe`). `ExpectedExecutableSha256` jest ignorowany. |
| Zwraca | Jeden wpis na każdy nieinternalny wpis katalogu, na przykład `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` w znanym układzie), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relacja do `PublicCapabilityRegistry` | Jest z niego wyprowadzane: rejestr deklaruje możliwości, ich operacje i dowody; to wywołanie ocenia je dla jednej instalacji. |
| Zgłaszane wyjątki | `OperationCanceledException` przy wejściu; `ArgumentException` dla pustej ścieżki katalogu głównego. |
| Anulowanie | Sprawdzane raz, przy wejściu. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje OMSI / system plików / transakcję | Nie / Nie / Brak. |
| Stabilność | Kontrakt wywołania: `STABLE_BETA`; zawartość listy jest ręcznie utrzymywanym spisem: `PARTIAL`. |
| Przykład | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Wylicza zainstalowaną zawartość i zwraca kanoniczne tożsamości, których można użyć w `LaunchSpec`. Wykrywanie pomija punkty ponownej analizy (reparse points) – cykle połączeń (junction) nie mogą go zawiesić – odczytuje pliki OMSI jako Windows-1252 (respektując UTF-8/UTF-16 oznaczone BOM) i nigdy nie podąża za dowiązaniami symbolicznymi. |
| Parametry | `query` i `scope` zgodnie z poniższą tabelą. `scope` jest wymagany dla `Entrypoints` (tożsamość mapy), `Repaints`, `FleetNumbers`, `Registrations` (tożsamość pojazdu). |
| Zwraca | Posortowaną listę `ContentIdentity(Identity, Kind, DisplayName)`. Tożsamości są ścieżkami względnymi wobec instalacji z ukośnikami odwrotnymi; porównania nie rozróżniają wielkości liter. |
| Zgłaszane wyjątki | `OperationCanceledException` przy wejściu; `ArgumentException`, gdy `Entrypoints` jest odpytywany bez zakresu lub katalog główny jest pusty; `FileNotFoundException` (bez kodu `OL_E_`; CLI mapuje go na `OL_E_NOT_FOUND`), gdy mapa lub pojazd wskazany w zakresie nie jest zainstalowany. Brak katalogu głównego lub katalogu zawartości daje pustą listę, a nie błąd. |
| Anulowanie | Sprawdzane raz, przy wejściu. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje OMSI / system plików / transakcję | Nie / Nie / Brak. |
| Stabilność | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (każdy plan zweryfikowany w runtime jest rozwiązywany za ich pośrednictwem). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (wyłącznie dowody statyczne). |
| Przykład | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Wartości `ContentQueryKind` i wyniki:

| Wartość | Zakres | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | brak | `maps\<dir>\global.cfg` | `map` | nazwa katalogu mapy |
| `Situations` | brak | `situations\...\<file>.osn` | `situation` | tożsamość mapy wskazanej w `.osn` (może mieć wartość `null`) |
| `Vehicles` | brak | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` lub nazwa pliku |
| `Repaints` | tożsamość pojazdu (wymagana; bez niej: pusta lista) | `<cti path>#item:<ordinal>` | `repaint` | nazwa `[item]` |
| `Hofs` | brak | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | tożsamość pojazdu (wymagana; bez niej: pusta lista) | ścieżka źródłowa `[number]` względna wobec pojazdu | `fleet-number` | `null` |
| `Registrations` | tożsamość pojazdu (wymagana; bez niej: pusta lista) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | pierwszy wiersz wartości (`null` dla trybu free) |
| `Addons` | brak | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | tożsamość mapy (wymagana; bez niej: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | etykieta punktu wejścia |
| `SessionProfiles` | brak | nazwa katalogu pakietu (identyfikator `/predefined-profile`) | `session-profile` lub `session-profile-invalid`, gdy pakiet się nie kompiluje | `name` profilu albo kod `OL_E_SESSION_PROFILE_*` niepoprawnego pakietu |
| `RuntimeModules` | brak | identyfikator modułu | `runtime-module` lub `runtime-module-invalid`, gdy nie można go wczytać | `name` modułu albo kod `OL_E_MODULE_*` |

Punkt wejścia jest wybierany przez `PresentedEntrypointIndex` albo przez `EntrypointIdentity` (tożsamość wykrywania lub dokładną etykietę). Planista rozwiązuje tożsamość do etykiety prezentowanej przez OMSI (OMSI prezentuje tylko pierwszy rekord każdej dokładnej etykiety), a plan wymienia ją w `ResolvedContent` z rodzajem `entrypoint`; `world.entrypoint-identity` ma status `EXPERIMENTAL`, dopóki nie zostanie zweryfikowane w runtime.

### `ListSessionProfilesAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Wypisuje każdy pakiet profilu sesji w `<root>\.omsilaunch\session-profiles\` wraz z jego ustawieniami wstępnymi, aby frontend mógł je oferować bez parsowania YAML. CLI `/list:SessionProfiles`. |
| Zwraca | Jeden `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` na każdy katalog pakietu, posortowany według `Id`. `Presets` to `SessionProfilePresetSummary(Index, Id, Name)` w kolejności indeksów. Każdy pakiet jest kompilowany tymi samymi rygorystycznymi regułami co przy uruchomieniu (`SessionProfileCompiler`); pakiet, który się nie kompiluje, jest wypisywany z `IsValid = false`, kodem `OL_E_SESSION_PROFILE_*` i komunikatem oraz bez ustawień wstępnych. Brak katalogu profili: pusta lista. |
| Zgłaszane | `OperationCanceledException` na wejściu. Błędy kompilacji są raportowane w wyniku, nie zgłaszane jako wyjątki. |
| Wymagana sesja w stanie Running | Nie. |
| Modyfikuje OMSI / system plików / transakcję | Nie / Nie / Brak. |
| Stabilność | `STABLE_BETA` (bramka offline `session-profiles.discovery`). |
| Przykład | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Wykrywa [moduły runtime](runtime-modules.md) zainstalowane poniżej `plugins\OmsiLaunch.Modules` wraz z ich integralnością i ustawieniami. Tylko odczyt. Wczytywanie jest otwarte: żaden moduł nie jest sprawdzany względem listy ani podpisu. |
| Zwraca | Jeden `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` na każdy katalog modułu, posortowany według nazwy katalogu. `Activation` to `requested`, `content` lub `default`; `IsValid` ma wartość false z kodem `OL_E_MODULE_*`, gdy modułu nie można wczytać; `Settings` wymienia ustawienia deklarowane przez moduł jako `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Zgłaszane | `OperationCanceledException` na wejściu. |
| Wymagana sesja w stanie Running | Nie. CLI: `/list:RuntimeModules` (koperta `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspekt | Szczegóły |
| --- | --- |
| Sygnatura | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Przeznaczenie | Zgłasza lub dokańcza pozostawioną trwałą transakcję (`<root>\.omsilaunch\journal.json`) po awarii właściciela. Na czas wywołania pozyskuje dzierżawę instalacji, dzięki czemu nigdy nie przywraca plików pod uruchamiającą się sesją. Publiczna możliwość `session.recover`; w CLI `/recovery-status` i `/recover`. |
| Parametry | `installation.RootPath`: katalog główny instalacji (normalizowany za pomocą `Path.GetFullPath`). `restore`: `false` = tylko raport; `true` = przywrócenie, weryfikacja, usunięcie dziennika i kopii zapasowych. |
| Zwraca | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = w chwili rozpoczęcia wywołania istniał dziennik; `Recovered` = zażądano przywrócenia, zostało ono wykonane i nie pozostał żaden dziennik; `Diagnostics` = uwagi dotyczące przywracania (`restore.session-artifact-removed` z `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), puste, gdy niczego nie przywrócono. |
| Zgłaszane wyjątki | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")`, gdy dzierżawa jest zajęta; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")`, gdy PID + czas utworzenia + ścieżka pliku wykonywalnego z dziennika nadal odpowiadają działającemu procesowi lub (dziennik po `HandoffCreated` bez PID) działa jakikolwiek `Omsi.exe` z tego katalogu głównego; `IOException` z `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` lub komunikatem weryfikacji („Restore hash mismatch: ...”, „Restore presence mismatch: ...”); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` dla uszkodzonego dziennika; `ArgumentException` dla pustego katalogu głównego; `OperationCanceledException`. Ilekroć wyjątek zostanie zgłoszony po rozpoczęciu przywracania, dziennik jest zachowywany, a następne wywołanie powtarza operację idempotentnie. |
| Anulowanie | Przekazywane do zapisów dziennika/kopii zapasowych; anulowanie w trakcie przywracania pozostawia dziennik jako oczekujący. |
| Wymagana sesja w stanie Running | Nie (odmawia działania, gdy właściciel jest aktywny). |
| Modyfikuje stan OMSI | Nie. |
| Modyfikuje system plików | Tylko przy `restore == true`: odtwarza oryginały ze zweryfikowanych kopii zapasowych (bajty, czas ostatniego zapisu, czas utworzenia, atrybuty; obsługiwane oryginały tylko do odczytu; zapis bezpośredni (write-through) + opróżnienie bufora; bez pozostawiania `*.omsilaunch.tmp`), usuwa artefakty sesji, usuwa `journal.json` i `backup\<sessionId>`. |
| Transakcja / przywracanie | Dokańcza oczekującą transakcję (`Restoring` → `Restored` → usunięcie dziennika). |
| Stabilność | `STABLE_BETA`: ścieżka raportowania i odzyskiwanie po wczesnym wyjściu (macierz RV-008), odzyskiwanie po nieudanym przywróceniu (domknięcie weryfikacji w runtime `F01`), odmowa przy aktywnym właścicielu i w oknie przed zapisaniem PID (`S04`) oraz odroczone odzyskiwanie sprzed obliczenia odcisku przy uruchomieniu (`S05`); zob. [stan weryfikacji w runtime](../status/runtime-validation-status.md). |
| Przykład | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Typy kontraktu

<a id="optional-values-and-semantic-primitives"></a>
### Wartości opcjonalne i prymitywy semantyczne

| Typ | Definicja | Uwagi |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, statyczne `Unset`, statyczne `Set(T)` | Odróżnia „nie zażądano” od „zażądano z wartością”. Postać JSON jest opisana w [dokumentacji LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Typ wyliczeniowy oparty na bajcie. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Walidowany tylko przy `DateTimeMode.Explicit` (miesiąc 1..12, dzień 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Walidowany tylko przy `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Rodzina LaunchSpec

Wszystkie poniższe rekordy są opisane właściwość po właściwości w [dokumentacji LaunchSpec](launchspec.md); ta tabela ustala spis typów.

| Typ | Przeznaczenie | Stabilność |
| --- | --- | --- |
| `LaunchSpec` | Główny rekord żądania z akcesorami `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, które podstawiają wartości domyślne za opcjonalne składowe o wartości `null`. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (opcjonalne przypięcie `Omsi.exe`, sprawdzane przy planowaniu). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Wybór świata. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (przestarzały alias `LastMapState`; nigdy nie oznacza „najnowszego pliku .osn”). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (obliczany z `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Dowolny tryb inny niż `Unset` czyni plan niemożliwym do uruchomienia. | `PARTIAL` (dowody planu `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Dowolny tryb inny niż `Unset` czyni plan niemożliwym do uruchomienia. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Każde ustawione pole czyni plan niemożliwym do uruchomienia. | `PARTIAL` |
| `EnvironmentSpec` | Osiem grup `IReadOnlyDictionary<string, OptionalValue<string>>` semantycznych ustawień `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; każda ustawiona wartość czyni plan niemożliwym do uruchomienia. | `PARTIAL` |
| `DiagnosticsSpec` | Sześć wartości logicznych; przenoszone, nieużywane. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Pochodzenie skompilowanego profilu sesji (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | Sesyjne [zarządzanie HOF](hof-management.md): `Operations` (lista `HofOperationSpec`). Pole `LaunchSpec.HofManagement`; akcesor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Moduły runtime](runtime-modules.md) dla sesji: `Enable` (identyfikatory modułów), `Disable` (identyfikatory modułów wyłączonych), `Settings` (`"<module id>.<key>"` na wartość); akcesory `EffectiveDisable`, `EffectiveSettings`. Pole `LaunchSpec.Modules`; akcesor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | Ocena modułu w planie: `Id`, `Version`, `Status` (`ELIGIBLE` lub `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` lub `default`), `Settings`, `RuntimeOperations` (operacje katalogu wykonywane przez moduł w procesie, każda z polityką mutacji, gdy zmienia OMSI, na przykład `camera.set (omsi-state)`), `TickMilliseconds`, `SnapshotSha256` (skrót plików modułu z czasu planowania, zob. [moduły runtime](runtime-modules.md#integrity)); `IsEligible`, `EffectiveSettings`, `EffectiveRuntimeOperations`. Stałe `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Jedno efektywne ustawienie modułu: `Key`, `Value`, `Source` (`request`, `settings-file` lub `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Widok wykrywania zainstalowanego modułu; zob. `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Ustawienie deklarowane przez moduł: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Jedna operacja HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Zgłaszany przez planowanie HOF (`OmsiLaunch.Core`); `Code` to kod `OL_E_HOF_*`. Publiczne API raportuje go jako komunikaty diagnostyczne planu. | `STABLE_BETA` |
| `SessionProfileSummary` | Widok wykrywania jednego pakietu (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Zob. `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Jedno ustawienie wstępne w podsumowaniu (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (przenoszony, przywrócenie następuje zawsze), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, domyślnie 180), `ShutdownTimeoutSeconds` (przenoszony, nieużywany). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Typy planu i stanu

| Typ | Pola | Uwagi |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (nowy `Guid` dla każdego planu), `BuildProfileId` (`"Omsi23004"` lub `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (lista `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (unikatowe ścieżki względne z `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` z `STATICALLY_VALIDATED` lub `UNAVAILABLE`; natywne możliwości uruchamiania z `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` lub `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (wpisy `Capability` dla zażądanych, ale nieobsługiwanych funkcji), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Rekord publiczny: może zostać zmodyfikowany lub stać się nieaktualny, dlatego `StartSessionAsync` planuje ponownie. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, system x64 i host x64), `LegacyPlatform` (zawsze `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (wszystkie równe `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` jest wartością `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), z wyjątkiem `SessionPlan.RequiredCapabilities`: wymaganie platformy lub zawartości ma `STATICALLY_VALIDATED` lub `UNAVAILABLE`, a możliwość natywna `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` lub `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Modyfikacje prezentacji używają kluczy `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (opcjonalna mapa ciągów znaków). | Kody zaczynające się od `OL_E_` są błędami, `OL_W_` ostrzeżeniami, wszystkie pozostałe – informacyjne. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Komunikaty diagnostyczne sesji nie obejmują komunikatów diagnostycznych planu. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (czas odebrania przez hosta), `Sequence` (numerowany od 1, w obrębie sesji), `Data`, `SessionTickMilliseconds` (monotoniczny zegar sesji wtyczki w chwili emisji zdarzenia; null dla zdarzeń hosta). | Ograniczony do 256 najnowszych zdarzeń (najstarsze są odrzucane). Zdarzenia wtyczki docierają po kolei przez pierścień telemetrii; zdarzenia utracone wskutek przepełnienia pierścienia są zliczane przez `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Lokalny dla procesu. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Zob. `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Zob. `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Typ wyliczeniowy oparty na bajcie, w kolejności deklaracji: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` i `EnteringGameplay` nigdy nie są ustawiane przez bieżącą usługę; `Snapshotting` jest stanem przejściowym i praktycznie niemożliwym do zaobserwowania. Stany końcowe: `Completed`, `Failed`. Pełna semantyka: [cykl życia sesji](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Typy sterowania runtime

| Typ | Definicja | Stabilność |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Statyczny kodek używany przez hosta i wtyczkę dla koperty skrzynki runtime: sygnatura (magic) `0x4F4C5243` („OLRC”), wersja 1, 72-bajtowy nagłówek little-endian (sygnatura, wersja, rodzaj 1 = żądanie / 2 = odpowiedź, długość całkowita, `Guid` sesji, identyfikator żądania, długość danych, SHA-256 danych), po którym następują dane JSON w UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: publiczny, ponieważ współdzielą go oba końce mostka; nie jest powierzchnią integracji; format może się zmienić wraz z wersją protokołu. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` – to, co host publikuje dla wtyczki w pliku mapowanym w pamięci `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256, string Settings, string SnapshotSha256)`: zaplanowany moduł przenoszony w przekazaniu (`StartupHandoff.Modules`); `SnapshotSha256` (wersja formatu 8) to skrót plików modułu wykonany podczas planowania, pusty, gdy plan go nie zawierał. | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: rozwiązany zegar startowy sesji NEW_MAP przenoszony w przekazaniu (`StartupHandoff.Start`); pole null zachowuje własną wartość OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Kodek: sygnatura `0x4F4C5348`, wersja 8 (odczytuje 3 do 8; wersja 5 dodaje zaplanowane moduły, wersja 6 efektywne ustawienia każdego modułu, wersja 7 datę/czas startu, wersja 8 migawkę plików każdego modułu z czasu planowania; bity flag 0x04 i 0x08 niosą `PluginTrace` i `NativeTrace`), 64-bajtowy nagłówek z kontrolą integralności danych SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Pierścień zdarzeń od wtyczki do hosta w mapowaniu `OMSILAUNCH_TELEMETRY_NAME`: sygnatura `0x52544C4F` („OLTR”), wersja 1, 64 numerowane sloty po 2048 bajtów. `Initialize`, `Write`, `IsRing`, `ReadSince` (zwraca zdarzenia po numerze sekwencyjnym oraz liczbę utraconych). | `INTERNAL` |

Wtyczka odrzuca przekazanie (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`), chyba że `WorldMode` to `NewMap` lub `SavedSituation`, `HeadlessStart` ma wartość true, `PlayerVehicleEnabled` ma wartość false, oba tryby daty/czasu to `Unset`, a zapisana sytuacja ma niepustą tożsamość. Planista egzekwuje te same ograniczenia wcześniej, więc plan możliwy do uruchomienia nigdy tego nie wyzwala.

<a id="capability-registry-types"></a>
### Typy rejestru możliwości

| Typ | Przeznaczenie |
| --- | --- |
| `PublicCapabilityRegistry` | Kanoniczny katalog możliwości. `ProtocolVersion` (`"0.1"`), `All` (każdy `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (48 konkretnych identyfikatorów operacji, które frontend może przekazywać), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (ścieżka polecenia do identyfikatora operacji), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (zwraca `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Egzekwowany przez `ExecuteRuntimeAsync`, CLI, lokalną płaszczyznę sterowania i wtyczkę. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (wartość `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; pochodne `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (przyjmuje `cursor`/`limit`, zob. [paginacja](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | Co zmienia operacja runtime: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | Słownik dowodów: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Pełny katalog: [możliwości](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Metody rozszerzające `D3DRuntimeApi`

Typowane opakowania `ExecuteRuntimeAsync` dla operacji `d3d.*` (`EXPERIMENTAL`, możliwość `PublicExperimental` `d3d.texture`). Przydzielają identyfikatory żądań z licznika wspólnego dla całego procesu, zaczynającego się od 30 000, i domyślnie ustawiają limit czasu na 5 s (z wyjątkiem `GetD3DStatusAsync`, która go wymaga).

| Metoda | Operacja | Argumenty i ograniczenia |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | brak |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | szerokość/wysokość 1..4096, poziomy 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | poziom 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, szerokość/wysokość 1..4096, piksele ≤ 48 KiB (w transmisji kodowane w Base64) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | ponowne zwolnienie jest odrzucane z `OL_E_D3D_RESOURCE_RELEASED` |

Typy: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` z `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Błędy: nieudany wynik jest ponownie zgłaszany jako `OmsiRuntimeException(Code, detail)`, gdzie `Code` to `ErrorCode` wyniku (lub `OL_E_RUNTIME_OPERATION_FAILED`, gdy go brak), a komunikat ma postać `"<code>: <Values["detail"]>"`; udany wynik bez wartości albo nieznany ciąg stanu urządzenia powoduje zgłoszenie `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Wszystko, co zgłasza `ExecuteRuntimeAsync`, propaguje się bez zmian. Obsługa resetu urządzenia jest zweryfikowana w runtime: reset przeprowadza urządzenie przez `Resetting` z powrotem do `Ready` i unieważnia każdą aktywną teksturę (`OL_E_D3D_STALE_RESOURCE_HANDLE`, domknięcie weryfikacji w runtime `D01`); odnotowuje to wpis katalogu `d3d.texture`. Przejścia `Lost` nie da się wywołać spoza produktu i jest ono pokryte wyłącznie testami offline.

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
### Typy kontraktu procesu

| Typ | Zawartość |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Używany wyłącznie przez CLI ([kody wyjścia](exit-codes.md)); API nigdy nie kończy procesu. |
| `PublicErrorCategory` | Stałe tekstowe używane w kopertach błędów CLI/sterowania: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Jedna `const string` na kod (143: 142 błędy `OL_E_` i 1 ostrzeżenie `OL_W_`) oraz `All`, katalog `PublicErrorDescriptor(Code, Category)`. Kategorie: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Dokumentacja: [kody błędów](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Właściwość `Code` oraz komunikat; zgłaszany wyłącznie przez `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (tożsamość instalacji i zawieranie ścieżek)

Stabilność: `STABLE_BETA` (funkcje czyste, bez operacji we/wy, bez stanu OMSI, bez modyfikacji systemu plików, bez udziału w transakcji, bez wymogu sesji w stanie Running). Jest to jedyna definicja używana przez dzierżawę instalacji, nazwę potoku lokalnego sterowania, ograniczanie zasobów profilu sesji do pakietu, walidację celów tekstur internetowych oraz ścieżkę modelu przy dodawaniu pojazdów w runtime.

| Składowa | Zachowanie |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` bez końcowych separatorów, z wyjątkiem katalogu głównego dysku (`C:\`), który jest zachowywany. Rozwiązuje segmenty `.` i `..`, traktuje `/` i `\` jednakowo i scala powtórzone separatory. **Nie** rozwiązuje połączeń (junction) ani dowiązań symbolicznych. Zgłasza `ArgumentException` dla katalogu głównego o wartości null lub pustego. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` zamienione na wielkie litery. Leksykalnie równoważne zapisy tego samego katalogu głównego (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) mają jeden wspólny klucz; różne katalogi główne (`C:\OMSI-A`, `C:\OMSI-B`) nigdy. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Rozwiązuje `candidate` (względną wobec `root` lub bezwzględną) i zwraca `true` tylko wtedy, gdy leży ona ściśle poniżej `root`; `relativePath` to kanoniczny zapis z `\`. Używa segmentów `Path.GetRelativePath`, więc katalog równoległy, taki jak `C:\OMSI-A\x`, nigdy nie znajduje się wewnątrz `C:\OMSI`; sam katalog główny, inne woluminy i ucieczki przez `..` zwracają `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Dzieli według `/` i `\`, pomijając puste segmenty. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Profile sesji (`OmsiLaunch.Core`)

Stabilność: `EXPERIMENTAL`. Te typy kompilują [profil sesji](session-profiles.md) w formacie YAML (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, schemat `omsilaunch.session-profile/v1`) do `LaunchSpec`. Polecenie CLI `/predefined-profile:<id> /predefined-profile-index:<n>` używa dokładnie tych wywołań; integrator może ich użyć, aby uruchomić profil przez API.

| Składowa | Zachowanie |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Odczytuje i waliduje pakiet. `id` musi być zwykłą nazwą katalogu (w przeciwnym razie `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` mieści się w zakresie 1..99 (`MinPresetIndex`..`MaxPresetIndex`; w przeciwnym razie `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Brak pliku: `OL_E_SESSION_PROFILE_NOT_FOUND`; rozmiar większy niż `MaxBytes` (256 KiB), niepoprawny YAML, kotwice, nieznane klucze lub `id` różny od nazwy katalogu: `OL_E_SESSION_PROFILE_INVALID`; inny `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Ścieżki zasobów są ograniczone do pakietu (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Każdy błąd jest zgłaszany jako `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Zwraca `baseline` z zastosowanym profilem: dla `NewMap` blok `new` profilu (mapa, punkt wejścia oraz ewentualna data/czas/rok/pogoda); `settings` ustawienia wstępnego (preset) scalone ponad `Environment.General`; `Presentation`, `InternetTextures` i `Behavior` ustawienia wstępnego, jeśli są obecne; oraz `SessionProfile` = `profile.Metadata`. Dla `NewMap` sprawdza także mapę względem listy `compatibility` profilu (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | Sprawdzenie zgodności dla pozostałych trybów (dla `SavedSituation` mapa jest odczytywana z pliku `.osn`). CLI wywołuje je po zbudowaniu ostatecznego `WorldSpec`. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` oraz akceptowane klucze dla każdego mapowania YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | Wczytany pakiet; `Preset` to wyłącznie wybrane ustawienie wstępne. |
| `SessionProfileException(string code, string message)` | `IOException` z `Code` (jednym z kodów `OL_E_SESSION_PROFILE_*`); komunikat ma postać `"<code>: <message>"`. |

CLI dodatkowo odrzuca flagi wiersza poleceń sprzeczne z profilem (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); to sprawdzenie nie jest częścią kompilatora. Pełna kolejność scalania: [profile sesji](session-profiles.md#precedence-and-override-conflicts).

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
### Typy platformy (`OmsiLaunch.Process`)

| Typ | Stabilność | Zastosowanie |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` jako typ parametru konstruktora `OmsiLaunchService` | Wykrywa platformę, sprawdza możliwość zapisu, uruchamia, obserwuje, kończy i czeka na `Omsi.exe`. Należy przekazać `new CurrentWindowsX64Platform()`; samodzielna implementacja nie jest obsługiwana. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | Jedyna implementacja: host Windows x64, `CreateProcessW` dla `Omsi.exe`, `TerminateProcess` dla kanonicznego zatrzymania. Jej metody wywołuje usługa; integratorzy jedynie ją tworzą. Składowe (wspólne z `IRuntimePlatform`): `Detect(root)` zwraca `RuntimePlatformInfo` planu; `ValidateCurrent(info)` zgłasza `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING`, gdy host nie może uruchomić sesji; `IsInstallationWritable(root)` stoi za `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` tworzy `Omsi.exe` i zapisuje tożsamość procesu (PID, czas utworzenia, ścieżkę oraz skrót obliczony przez usługę; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` i `Terminate` obserwują i kończą proces. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Publiczne w zestawie, ponieważ współdzielą je usługa i testy. Nie są powierzchnią integracji; `LaunchedProcess` wewnętrznie opakowuje uchwyty procesu i wątku OMSI (nie są to publiczne składowe) i nigdy nie jest zwracany przez `IOmsiLaunch`. |

<a id="thread-safety"></a>
## Bezpieczeństwo wątków

- `OmsiLaunchService` jest bezpieczna dla współbieżnych wywołań dotyczących różnych sesji: sesje są przechowywane w `ConcurrentDictionary`, a każda modyfikacja dotycząca sesji odbywa się pod prywatną blokadą tej sesji.
- Współbieżne wywołania dotyczące tej samej sesji są bezpieczne, ale tam, gdzie ma to znaczenie, są serializowane: `ExecuteRuntimeAsync` pobiera bramkę sesji, więc drugie polecenie czeka na pierwsze (jego limit czasu zaczyna biec w chwili umieszczenia w skrzynce).
- Nadzorca działa jako zadanie puli wątków (`Task.Run`) od chwili powrotu z `StartSessionAsync` aż do osiągnięcia przez sesję stanu końcowego; co 100 ms odpytuje telemetrię i proces. Kod wywołujący nigdy nie wykonuje kodu nadzorcy.
- `StopAsync` i `GetStatusAsync` kończą się synchronicznie i mogą być wywoływane z dowolnego wątku, także wewnątrz procedury obsługi `ProcessExit` (CLI robi to z budżetem 4 s).
- Żadne wywołanie API nie jest powiązane z konkretnym wątkiem; żadne nie wymaga kontekstu synchronizacji.

<a id="what-is-not-in-the-api"></a>
## Czego nie ma w API

- Brak `IntPtr`, `nint`, uchwytów Win32, adresów natywnych, wskaźników VMT i obiektów procesów. Wartości wyniku, których klucze zaczynają się od `internal_` lub kończą na `_address`, `_pointer`, `_vmt`, są usuwane, zanim wynik opuści `ExecuteRuntimeAsync`.
- Brak operacji runtime `internal.*`: `internal.road-vehicles.make-basic` ma w rejestrze klasyfikację `InternalOnly` i zwraca `OL_E_RUNTIME_OPERATION_UNKNOWN` zarówno z API, jak i z CLI.
- Brak surowych odczytów i zapisów pamięci OMSI oraz brak dostępu do plików instalacji innego niż zadeklarowany w `LaunchSpec`.
- Brak uchwytów międzyprocesowych: [lokalna płaszczyzna sterowania](local-control.md) jest jedyną drogą międzyprocesową i akceptuje wyłącznie `session.status`, `session.events`, `session.stop` i `runtime.execute`.
- Brak stosowania pojazdu gracza przy starcie. Kooperacyjne zamykanie, `LAST_MAP_STATE`, data/czas/pogoda startowa oraz nakładki dokumentów klawiatury/kontrolera mają status `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Podsumowanie stabilności

| Powierzchnia | Stabilność |
| --- | --- |
| Konstruktor `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transport `ExecuteRuntimeAsync`; operacje `PublicStableBeta` | `STABLE_BETA` |
| Operacje `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| składowe specyfikacji daty/czasu/pogody/wejścia, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| składowe specyfikacji pojazdu gracza | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implementacje `IRuntimePlatform`, wszystkie zestawy implementacyjne | `INTERNAL` |
| `WorldMode.LastSituation`, operacje `internal.*` | `UNAVAILABLE` |
