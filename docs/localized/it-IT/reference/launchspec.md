# Riferimento di LaunchSpec

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> Traduzione della [pagina originale in inglese](../../../reference/launchspec.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina è il riferimento normativo per `LaunchSpec`, il record di richiesta che descrive una sessione OmsiLaunch: la sua forma C# (`OmsiLaunch.Api`), la sua forma di file JSON così come viene caricata dalla CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), ogni proprietà con tipo, valore predefinito, regola di validazione ed effetto attuale, le regole di validazione che rendono un piano non eseguibile e la precedenza tra flag della CLI, file di specifica e profili di sessione. Viene documentato solo ciò che fa il codice attuale.

Pagine correlate: [API pubblica](public-api.md), [riferimento CLI](cli.md), [profili di sessione](session-profiles.md), [codici di errore](errors.md), [ciclo di vita della sessione](../concepts/session-lifecycle.md), [capability](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## Da dove proviene un LaunchSpec

| Origine | Come diventa un `LaunchSpec` |
| --- | --- |
| API | L'integratore costruisce il record e lo passa a `PlanSessionAsync`. |
| Flag della CLI | `CliInput.BuildSpecAsync` parte dai valori predefiniti incorporati (NEW_MAP, tutto non impostato, valori predefiniti di `Behavior`) e applica i flag. |
| File JSON `/spec:<path>` | Caricato da `LaunchSpecJson.LoadAsync`, poi usato come base che i flag della CLI sovrascrivono (vedere [precedenza](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Profilo di sessione (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` scrive nella base il mondo, le impostazioni, la presentazione, le internet-textures e il comportamento del profilo e registra i metadati `SessionProfile`. |

L'[esempio completo](#complete-example) riportato sotto è verificato rispetto alla forma del record. Una copia dell'esempio minimo è distribuita come `examples/release-session.example.json` (e `.omsilaunch\examples\release-session.example.json` nel pacchetto di rilascio).

<a id="json-form"></a>
## Forma JSON

| Regola | Dettaglio |
| --- | --- |
| Serializzatore | `System.Text.Json` con `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; non è registrato alcun converter. |
| Nomi delle proprietà | I nomi delle proprietà C# (`Installation`, `RootPath`, ...). In caricamento il confronto non distingue maiuscole e minuscole; la CLI li scrive in PascalCase. |
| Enumerazioni | Interi (non esiste un converter per enumerazioni come stringhe). `"Mode": 0` è valido; `"Mode": "NewMap"` viene rifiutato come JSON malformato. I valori sono elencati in [Enumerazioni](#enumerations). |
| `OptionalValue<T>` | Un oggetto `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (il valore viene ignorato), 1 = `Set` (il valore deve essere presente e non null; un `Set` con valore null non viene validato e si comporta come un valore non valido). Un membro `OptionalValue` omesso è `Unset`. Il membro di sola lettura `IsSet` compare nell'output scritto dalla CLI e in caricamento viene accettato e ignorato. |
| Record facoltativi | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile`, `HofManagement`, `Modules` possono essere `null` od omessi; le funzioni di accesso `Effective*` sostituiscono i valori predefiniti. |
| Record obbligatori | `Installation`, `World`, `Date`, `Time`, `Environment` (con tutti e otto i dizionari, usare `{}`), `Behavior` devono essere oggetti presenti. Non vengono validati: uno `null` o mancante fallisce più tardi con un riferimento null, che la CLI segnala come `OL_E_INTERNAL` (uscita 10) o `OL_E_INVALID_ARGUMENT` (uscita 2). |
| Proprietà sconosciute | Rifiutate prima del binding: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (il percorso usa i nomi dei membri così come sono scritti nel file). Il contenuto dei dizionari (`Environment.*`) non viene controllato come proprietà. |
| Radice | Deve essere un oggetto JSON: `OL_E_SPEC_INVALID`. Profondità massima di annidamento 32. |
| Dimensione del file | Al massimo 1 MiB (1 048 576 byte): `OL_E_SPEC_TOO_LARGE`. File mancante: `OL_E_SPEC_NOT_FOUND`. |
| Commenti e virgole finali | Sono accettati i commenti `//` e `/* */` e le virgole finali. |
| JSON malformato | L'eccezione del parser non viene tradotta: la CLI segnala `OL_E_INTERNAL` con codice di uscita 10. |
| Codifica | UTF-8 (il lettore tollera un BOM). Le barre rovesciate nelle identità devono essere precedute da escape (`"maps\\Grundorf\\global.cfg"`); le barre normali sono accettate per le identità di mappe, situazioni, veicoli e HOF. |

<a id="complete-example"></a>
## Esempio completo

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // optional pin of Omsi.exe
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // must be true
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30,             // with CooperativeShutdown
    "CooperativeShutdown": false              // opt-in
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

Una data esplicita, quando il build la supporta, si scrive come `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` e un orario come `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. Entrambi si applicano solo alle sessioni NEW_MAP (`EXPERIMENTAL`; vedere sotto).

<a id="property-reference"></a>
## Riferimento delle proprietà

La colonna «Utilizzo» indica che cosa fa il codice attuale con il valore. La stabilità usa il vocabolario della [pagina dell'API pubblica](public-api.md#stability-vocabulary).

### `LaunchSpec` (root)

| Proprietà | Tipo JSON | Obbligatoria | Valore predefinito se omessa | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Installation` | oggetto `InstallationSpec` | sì | nessuno | sì | `STABLE_BETA` |
| `World` | oggetto `WorldSpec` | sì | nessuno | sì | `STABLE_BETA` |
| `Date` | oggetto `DateSpec` | sì | nessuno | sì, solo NEW_MAP (vedere [`DateSpec`](#datespec-timespec-yearspec)) | `EXPERIMENTAL` |
| `Time` | oggetto `TimeSpec` | sì | nessuno | sì, solo NEW_MAP | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | no | `Unset` | risolto per la diagnostica; qualsiasi campo impostato è non eseguibile | `PARTIAL` |
| `Environment` | oggetto `EnvironmentSpec` | sì | nessuno | sì (overlay semantico di `options.cfg`) | `STABLE_BETA` |
| `Behavior` | oggetto `LaunchBehaviorSpec` | sì | nessuno | in parte (vedere il record) | `STABLE_BETA` / `PARTIAL` |
| `Year` | oggetto `YearSpec` o null | no | `null` → `EffectiveYear` = modalità `Unset` | sì, solo NEW_MAP | `EXPERIMENTAL` |
| `Weather` | oggetto `WeatherSpec` o null | no | `null` → `EffectiveWeather` = modalità `Unset` | sì, ogni modalità di mondo, applicato subito dopo l'avvio del mondo (vedere [`WeatherSpec`](#weatherspec)) | `EXPERIMENTAL` |
| `Input` | oggetto `InputSpec` o null | no | `null` → `EffectiveInput` = entrambi non impostati | sì (vedere [`InputSpec`](#inputspec)) | `EXPERIMENTAL` |
| `Diagnostics` | oggetto `DiagnosticsSpec` o null | no | `null` → `EffectiveDiagnostics` = valori predefiniti | solo trasportato | `PARTIAL` |
| `Presentation` | oggetto `SessionPresentationSpec` o null | no | `null` → `EffectivePresentation` = splash gestita, nessuna lingua, nessuna directory personalizzata, icona nell'area di notifica visibile | sì | `STABLE_BETA` |
| `InternetTextures` | oggetto `InternetTexturesSpec` o null | no | `null` → `EffectiveInternetTextures` = `Native` | sì | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | oggetto `SessionProfileMetadata` o null | no | `null` | solo provenienza (diagnostica del piano `session_profile.selected`) | `STABLE_BETA` |
| `HofManagement` | oggetto `HofManagementSpec` o null | no | `null` → `EffectiveHofManagement` = nessuna operazione | sì (overlay HOF di sessione, vedere [gestione HOF](hof-management.md)) | `STABLE_BETA` |
| `Modules` | oggetto `RuntimeModulesSpec` (`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`) o null | no | `null` → `EffectiveModules` = nulla di richiesto, disabilitato o impostato | no (i moduli non toccano mai i file dell'installazione tramite OmsiLaunch; vedere [moduli runtime](../../../reference/runtime-modules.md)) | `EXPERIMENTAL` |

Funzioni di accesso di sola lettura (presenti nell'output JSON della CLI, ignorate in caricamento): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, `EffectiveHofManagement`, `EffectiveModules`.

### `InstallationSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | stringa | obbligatorio | Directory che contiene `Omsi.exe` e `plugins\`. Vedere [regole dei percorsi](#path-rules). Vuoto/solo spazi → `OL_E_INSTALLATION_NOT_FOUND`. | sì | `STABLE_BETA` |
| `ExpectedExecutableSha256` | stringa o null | `null` | `null`, oppure 64 cifre esadecimali (altrimenti `OL_E_INVALID_ARGUMENT`). | Pin facoltativo: quando è impostato, il piano non è eseguibile a meno che lo SHA-256 di `Omsi.exe` sia uguale ad esso (`OL_E_UNSUPPORTED_BUILD`, dati `expected_sha256` e `file_sha256`). | `STABLE_BETA` |

### `WorldSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Mode` | int `WorldMode` | obbligatorio | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` è un alias obsoleto con lo stesso valore 2). | sì; `LastMapState` richiede `MapIdentity` e il file `laststn.osn` della mappa (`OL_E_MAP_NOT_FOUND`, `OL_E_SITUATION_NOT_FOUND`) | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Per `NewMap`: obbligatorio, nella forma `maps\<dir>\global.cfg` (senza distinzione tra maiuscole e minuscole, `/` accettato, nessun `..`) e installato. Ignorato per `SavedSituation` (la mappa è fornita dal file `.osn`). | sì (handoff) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Per `SavedSituation`: obbligatorio, un'identità `situations\...\<file>.osn` installata (come restituita da `DiscoverAsync(Situations)` / `/list:situations`). | sì (handoff) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Per `NewMap` senza `EntrypointIdentity`: obbligatorio, `>= 0`, un indice nell'elenco dei punti di ingresso presentato da OMSI per la mappa. Inviato al plugin come `-1` quando non è impostato. | sì (handoff) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Un'etichetta esatta (con distinzione tra maiuscole e minuscole) del punto di ingresso o un'identità di discovery. Il pianificatore la risolve nell'etichetta che OMSI presenta e l'avvio seleziona quell'etichetta (`world.entrypoint-identity`); un record che OMSI non presenta viene rifiutato (`OL_E_ENTRYPOINT_NOT_PRESENTED`), così come uno sconosciuto (`OL_E_ENTRYPOINT_NOT_FOUND`). | trasportato | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec` (sola lettura) | calcolato | `Mode` = `Identity` quando `EntrypointIdentity` è impostato, altrimenti `PresentedIndex` quando l'indice è impostato, altrimenti `Unset`; `PresentedIndex`, `Identity` rispecchiano gli input. | derivato | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Mode` | int `DateTimeMode` | obbligatorio (`Year`: `0` quando il record è null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | NEW_MAP: `Explicit` usa `Value`, `System` l'orologio del PC al momento della pianificazione della sessione; `Unset` mantiene il valore proprio di OMSI (le sue opzioni e il form Start). Il controller risolve la richiesta nell'orologio di avvio di OMSI (anno, giorno dell'anno, ora, minuto, secondo; capability `world.explicit-date`, `world.explicit-time`, `world.explicit-year`), l'handoff di avvio (versione 7) lo trasporta e il plugin lo scrive subito prima di `NewSituation` di OMSI, così OMSI deriva stagione e calendario dalla data richiesta prima del caricamento della mappa. Dopo l'avvio l'orologio in esecuzione viene riletto; una discrepanza, oppure OMSI che usa un orario di avvio fisso, fa fallire l'avvio con `OL_E_DATE_TIME_APPLY_FAILED`. `SavedSituation`: una modalità diversa da `Unset` è `OL_E_CAPABILITY_UNAVAILABLE` (una situazione porta con sé il proprio orario). | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: una data di calendario reale (`Day` valido per il mese e l'anno); `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59; anni 1900..2200. `YearSpec` imposta l'anno (e l'anno di `Date`, quando sono presenti entrambi). Il 31 dicembre di un anno bisestile viene rifiutato: il calendario di OMSI non può rappresentare il giorno 366. Valori non validi: `OL_E_DATE_TIME_APPLY_FAILED`. Deve essere impostato quando `Mode` è `Explicit` e non impostato negli altri casi (`OL_E_INVALID_ARGUMENT`). | sì | `EXPERIMENTAL` |

### `WeatherSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Mode` | int `WeatherMode` | `0` quando il record è null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Qualsiasi modalità diversa da `Unset` richiede `world.weather`; il plugin la applica subito dopo l'avvio del mondo (ogni modalità di mondo; sovrascrive il meteo di una situazione salvata). Un errore fa fallire l'avvio con `OL_E_WEATHER_APPLY_FAILED`. | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Un file della cartella `weather` dell'installazione, per nome o come `weather\<name>.owt`; controllato in fase di pianificazione (`OL_E_WEATHER_INVALID`). | handoff | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Quattro lettere o cifre (altrimenti `OL_E_WEATHER_INVALID`); usato dalla modalità `Icao`. | handoff | `EXPERIMENTAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (all'interno di `PlayerVehicle`)

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Identità `Vehicles\...\<file>.bus` installata, altrimenti `OL_E_VEHICLE_NOT_FOUND`. | risolto in `ResolvedContent`; poi `player-vehicle.model` unsupported → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Un'identità di repaint (livrea) di `Model` (`<cti>#item:<n>`), altrimenti `OL_E_REPAINT_NOT_FOUND`; controllata solo quando `Model` è impostato. | come sopra | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | `Vehicles\...\<file>.hof` installato, altrimenti `OL_E_HOF_NOT_FOUND`. | come sopra | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Qualsiasi stringa. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Qualsiasi stringa. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (sola lettura) | calcolato | `true` quando `Model` è impostato. `PlayerVehicle.IsSet` è ciò che l'handoff trasporta come `PlayerVehicleEnabled`. | derivato | `PARTIAL` |

Un `PlayerVehicle` con `Presence` 1 e tutti i campi non impostati viene accettato e non ha alcun effetto. Qualsiasi campo impostato rende il piano non eseguibile in questo build (`UNAVAILABLE`).

### `EnvironmentSpec`

| Proprietà | Tipo | Predefinito | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | `IReadOnlyDictionary<string, OptionalValue<string>>` ciascuno, obbligatorio (`{}` se vuoto) | nessuno | sì | `STABLE_BETA` |

Gli otto gruppi vengono concatenati; il gruppo in cui è collocata una chiave non ha alcun effetto. Ogni voce con `Presence` 1 è un'impostazione semantica di `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); la chiave seleziona il file di destinazione (`options.cfg` per tutte le chiavi attuali) e il token. La pianificazione verifica che la chiave esista (`OL_E_UNKNOWN_SETTING`) e sia scrivibile (`OL_E_SETTING_NOT_WRITABLE`); il valore viene validato solo all'avvio (`OL_E_INVALID_SETTING_VALUE`, segnalato come sessione `Failed` con `OL_E_START_SESSION`). Le chiavi non distinguono maiuscole e minuscole. Le voci non impostate vengono ignorate. Il flag della CLI `/set:<key>=<value>` scrive in `General`; anche le `settings` dei profili di sessione vengono unite in `General`.

| Chiave | Valore | Note |
| --- | --- | --- |
| `general.language` | stringa | token `[language]` |
| `general.radio` | stringa | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | token di presenza (`autoSave` è l'inverso di `noAutoSave`) |
| `graphics.screenRatio` | stringa | |
| `graphics.maxFPS` | intero 10..200 | |
| `graphics.tileDistance` | intero 1..20 | |
| `graphics.maxObjectDistanceMeters` | numero 20..5000 | |
| `graphics.minObjectScreenPercent` | numero 0..10 | memorizzato diviso per 100 |
| `graphics.minReflectionObjectScreenPercent` | numero 0..50 | memorizzato diviso per 100 |
| `graphics.maxObjectComplexity` | intero 0..3 | |
| `graphics.maxMapComplexity` | intero 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | token di presenza |
| `graphics.stencilShadows` | `true` / `false` | scritto come `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | un blocco `smokesystems` |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | token di presenza |
| `simulation.ticketSelling` | intero 0..2 | |
| `simulation.maintenance` | intero 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | due token OMSI contemporaneamente (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | token di presenza |
| `traffic.randomVehicles` | intero 0..1000 | componente 0 del blocco multiriga `AIMaxCountRandom` (validato a runtime, matrice RV-005) |
| `traffic.humans` | intero 0..1000 | componente 1 di `AIMaxCountRandom` |
| `traffic.factorPercent` | numero 1..300 | |
| `traffic.parkedVehiclesPercent` | numero 0..100 | |
| `traffic.scheduledVehicles` | numero 0..1000 | |
| `traffic.scheduledLinePriority` | numero 1..4 | |
| `traffic.passengerFactorPercent` | numero 0..200 | |
| `sound.stereo` | numero 0..100 | |
| `sound.maxSimultaneousSounds` | numero 5..1000 | |
| `sound.masterVolume` | numero 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | rifiutate | note ma non scrivibili → `OL_E_SETTING_NOT_WRITABLE` |

I file modificati mantengono la loro codifica (byte Windows-1252 preservati; UTF-8/UTF-16 con BOM rispettati) e i terminatori di riga.

### `LaunchBehaviorSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | solo `true` | Il ripristino esatto è obbligatorio: `false` viene rifiutato con `OL_E_INVALID_ARGUMENT`. | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | qualsiasi | `true`: un file `closecheck` che esiste prima della sessione viene rimosso definitivamente all'avvio (diagnostica `closecheck.stale-removed` con il suo SHA-256; errore `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: un `closecheck` esistente viene lasciato intatto e non costituisce un'eliminazione della sessione. Il `closecheck` che OMSI scrive durante la sessione viene sempre rimosso al ripristino. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (altrimenti `ArgumentOutOfRangeException` da `StartSessionAsync`; il `/startup-timeout` della CLI impone 1..600; i profili richiedono > 0). Tempo concesso dall'avvio del supervisore a `Running`; alla scadenza la sessione fallisce con `OL_E_STARTUP_TIMEOUT` (plugin avviato) o `OL_E_PLUGIN_NOT_LOADED`. | sì | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600 (`/shutdown-timeout` della CLI) | Con `CooperativeShutdown`: il tempo di cui dispone OMSI per terminare da solo prima di essere terminato forzatamente. Senza di esso OMSI viene terminato subito. | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | qualsiasi (`/cooperative-shutdown` della CLI) | `true`: la terminazione di una sessione in esecuzione chiede a OMSI di uscire tramite la propria routine di uscita (`TForm_main.CloseOMSI`: le opzioni e il file `laststn.osn` della mappa vengono salvati, i plugin finalizzati) invece di terminarlo; dopo `ShutdownTimeoutSeconds` viene terminato forzatamente (`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`). Il ripristino viene eseguito dopo l'uscita, quindi i file di proprietà della sessione tornano comunque ai byte originali. | `EXPERIMENTAL` |

### `InputSpec`

| Proprietà | Tipo | Predefinito | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Percorso (assoluto, oppure relativo all'installazione) di un file di tastiera nel formato di OMSI. La sessione lo usa al posto di `Inputs\keyboard.cfg` (un overlay di transazione: il file dell'installazione viene ripristinato byte per byte al termine). Il file deve esistere, avere al massimo 1 MiB e contenere almeno un'associazione `[entry]`; altrimenti `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Percorso di un file di controller nel formato di OMSI, usato nello stesso modo al posto di `Inputs\gamectrler.cfg` per la sessione. Deve contenere almeno un dispositivo `[ctrl]`; altrimenti `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| Proprietà | Tipo | Predefinito | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | Nessun consumatore in `src/`. La traccia dell'host `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` viene sempre scritta. | `PARTIAL` (trasportato, attualmente senza effetto) |
| `Verbose` | bool | `false` | Nessun consumatore. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Nessun consumatore. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Nessun consumatore. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Flag dell'handoff di avvio: il plugin scrive eventi `plugin.trace.operation` (operazione, esito, codice, durata in microsecondi) per ogni operazione runtime; raggiungono `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` attraverso l'event ring di telemetria. | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | Flag dell'handoff di avvio: il livello nativo riporta ogni installazione e rimozione di hook (`native.trace`: confine, proprietario, destinazione, risultato) nella stessa traccia dell'host. | `EXPERIMENTAL` |

I flag della CLI `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` valorizzano questi booleani (`/logall` imposta `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); vengono combinati in OR con i valori della specifica.

### `SessionPresentationSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Splash` | int `SplashMode` | `1` (`Managed`) | `0` `Unset` (alias `Native`): i file della splash screen di OMSI non vengono toccati. `1` `Managed`: OmsiLaunch applica in overlay `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<LANG>.bmp` per la sessione (ripristinati esattamente al termine). | sì | `STABLE_BETA` (matrice RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (senza distinzione tra maiuscole e minuscole); qualsiasi altro valore viene normalizzato in `ENG`. Quando non è impostato, viene letto il valore `[language]` di `options.cfg` e normalizzato allo stesso modo. | sì (solo splash gestita) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Directory che contiene `ENG.bmp` e `<LANG>.bmp` (640×480, BMP a 24 bit). Vedere [regole dei percorsi](#path-rules). Directory mancante: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; file mancante: `OL_E_SPLASH_ASSET_MISSING`; formato errato: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Quando non è impostato, viene usato `<root>\.omsilaunch\assets\splash` (popolato una sola volta dal pacchetto), altrimenti `assets\splash` del pacchetto. | sì (solo splash gestita) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` sopprime l'indicatore autonomo nell'area di notifica di Windows dell'owner CLI. | Solo owner CLI; l'API non ha un'icona nell'area di notifica. Non esiste un flag della CLI; può provenire solo da un file di specifica. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Utilizzo | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Mode` | int `InternetTexturesMode` | `0` (`Native`) | `0` `Native`: non cambia nulla. `1` `Disabled`: il plugin sopprime il downloader in-process di OMSI (telemetria `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: il profilo `.itx` viene applicato in overlay come `Texture\standard.itx`; i suoi file di destinazione e `Texture\standard.ipr` diventano eliminazioni della sessione. | sì | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Obbligatorio per `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Un file di testo con coppie di righe: URL assoluto `http`/`https`, poi un percorso di destinazione relativo alla radice dell'installazione che contiene un componente `Texture\`, non è radicato, non contiene `..`, non inizia con `\` e non attraversa alcuna junction/symlink (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Vedere [regole dei percorsi](#path-rules). | sì | `EXPERIMENTAL` |

### `HofManagementSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Consumato | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Operations` | array di `HofOperationSpec` | nessuno | Applicate in ordine; vedere [gestione HOF](hof-management.md) per risoluzione, regole sui conflitti e limiti. | sì | `STABLE_BETA` |

Membri di `HofOperationSpec`: `SourceKind` (int `HofSourceKind`), `Source` (stringa), `TargetVehicles` (array di identità di veicoli o di cartelle sotto `Vehicles\`, almeno una), `Mode` (int `HofApplyMode`, predefinito `0` `Add`), `Files` (array di nomi di file `.hof`, oppure null per tutti). I problemi sono diagnostica del piano `OL_E_HOF_*`.

### `RuntimeModulesSpec`

| Proprietà | Tipo | Predefinito | Valori validi | Consumato | Stabilità |
| --- | --- | --- | --- | --- | --- |
| `Enable` | array di id di modulo | nessuno | Id di moduli installati sotto `plugins\OmsiLaunch.Modules`; vedere [moduli runtime](../../../reference/runtime-modules.md). Un modulo richiesto che non può essere caricato è una diagnostica del piano `OL_E_MODULE_*` (piano non eseguibile). | sì | `EXPERIMENTAL` |
| `Disable` | array di id di modulo o null | nessuno | Tiene fuori dalla sessione un modulo predefinito, di contenuto o del file di impostazioni (diagnostica del piano `module.disabled`). Un modulo sia abilitato sia disabilitato è `OL_E_MODULE_SETTING_INVALID`. Accessor di sola lettura `EffectiveDisable`. | sì | `EXPERIMENTAL` |
| `Settings` | oggetto di stringhe o null | nessuno | `"<module id>.<key>": "<value>"`. Prevale sul file di impostazioni `.omsilaunch\modules.ini` e sul valore predefinito del manifest. Un valore non valido o una chiave che il modulo non dichiara è `OL_E_MODULE_SETTING_INVALID` (piano non eseguibile); le impostazioni per un modulo che la sessione non carica sono `OL_W_MODULE_SETTING_IGNORED`. I valori effettivi sono in `PlannedModule.Settings`. Accessor di sola lettura `EffectiveSettings`. | sì | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Proprietà | Tipo | Utilizzo | Stabilità |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | stringhe / int | Registrati nella diagnostica del piano `session_profile.selected` (`Data["session_profile.*"]`). Non utilizzati in altro modo; normalmente compilati dal compilatore dei profili di sessione, non a mano. | `STABLE_BETA` |

<a id="enumerations"></a>
## Enumerazioni

| Enumerazione | Valori (intero JSON) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto; è il ramo nativo dello stato dell'ultima mappa di OMSI, mai il file `.osn` più recente) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (alias), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `HofSourceKind` | `Collection` = 0, `Directory` = 1, `Vehicle` = 2, `File` = 3 |
| `HofApplyMode` | `Add` = 0, `Mirror` = 1 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (`Entrypoint.Mode` di sola lettura) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Gli interi al di fuori dell'intervallo dichiarato vengono memorizzati così come sono dal serializzatore e si comportano come valori sconosciuti (ad esempio un `WorldMode` sconosciuto non è né NEW_MAP né SAVED_SITUATION e produce un piano senza capability di mondo; il plugin lo rifiuterebbe, ma la CLI sostituisce comunque la modalità, vedere la precedenza).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Regole di validazione e diagnostica di non eseguibilità

`PlanSessionAsync` esegue `LaunchValidation.Validate` e poi `SessionPlanner.PlanAsync`. Un piano è eseguibile esattamente quando nessun codice di diagnostica inizia con `OL_E_`. L'insieme completo:

| Diagnostica | Condizione | Origine |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` vuoto o composto solo da spazi | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` senza un valore impostato o con mese/giorno fuori intervallo; `Time.Mode` = `Explicit` senza un valore impostato o con ora/minuto/secondo fuori intervallo | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` o `Time.Value` impostato mentre la modalità non è `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` con `MapIdentity` non impostato o non nella forma `maps\...\global.cfg` (validazione); `NewMap` con un'identità non installata (planner) | entrambi |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` senza `EntrypointIdentity` e con `PresentedEntrypointIndex` non impostato o negativo | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, mappa installata, nessun `EntrypointIdentity`, `PresentedEntrypointIndex` non impostato (`world.presented-entrypoint` non disponibile) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` senza `SituationIdentity` (validazione) o con un'identità non installata (planner) | entrambi |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: la mappa indicata nel file `.osn` non è installata | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | non Windows 10+ su un sistema operativo x64 con un processo host x64 (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | directory radice mancante, attributo di sola lettura impostato o nessuna sottodirectory `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | Non è un'installazione di OMSI 2: `Omsi.exe` mancante, non è un'immagine PE x86 a 32 bit, nessuna versione di OMSI 2 (2.x), oppure assenza della directory `maps\` / `Vehicles\` (`omsi.omsi2`). L'hash non è un criterio. | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `EntrypointIdentity` impostato; modalità di `Date`/`Time`/`Year` diversa da `Unset` con `SavedSituation`; modalità di `Weather` diversa da `Unset`; qualsiasi campo di `PlayerVehicle` impostato | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` non installato (in aggiunta a `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | una chiave di `Environment` che non è nel catalogo / non è scrivibile | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | la costruzione del piano splash/ITX ha generato un'eccezione; il messaggio contiene `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` o `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | il riferimento alla chiusura del plugin (`OmsiLaunchRuntimePaths`) o il manifest di rilascio non possono essere caricati (il messaggio può contenere `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Non validati in fase di pianificazione (falliscono all'avvio come sessione `Failed` con `OL_E_START_SESSION`): valori delle impostazioni (`OL_E_INVALID_SETTING_VALUE`), integrità del plugin permanente (`OL_E_PERMANENT_PLUGIN_*`), disponibilità del lease (`OL_E_INSTALLATION_BUSY`), intervallo di `StartupTimeoutSeconds` (eccezione generata da `StartSessionAsync`).

Diagnostica informativa del piano: `plugin.integrity.reference` (messaggio `manifest` o `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Precedenza: flag della CLI, file di specifica e profilo di sessione

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) costruisce la specifica effettiva in questo ordine:

1. Base = valori predefiniti incorporati, oppure il file `/spec` quando indicato.
2. Radice dell'installazione = l'argomento di installazione esplicito se indicato, altrimenti il `RootPath` della base; poi `.`/vuoto → directory dell'eseguibile, `Path.GetFullPath`. Un argomento di installazione esplicito prevale sempre sul `RootPath` della specifica.
3. Profilo di sessione (`/predefined-profile` + `/predefined-profile-index`): gli argomenti espliciti della CLI che toccano un campo di proprietà del profilo vengono rifiutati con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (campi del mondo solo in modalità NEW_MAP; chiavi `/set` presenti nel preset; flag della splash quando il preset ha `presentation`; flag delle internet-textures quando ha `internet-textures`; timeout quando ha `behavior`). Il `World` della base viene sostituito da uno nuovo (sopravvive solo la modalità del mondo della CLI), poi vengono applicati il blocco `new:` del profilo (solo NEW_MAP), `settings` (in `General`), `presentation`, `internet-textures`, `behavior` e i metadati `SessionProfile`. `compatibility.maps` viene imposto per NEW_MAP e SAVED_SITUATION.
4. Mondo: la modalità del mondo della CLI prevale sempre (`/new` predefinito, `/saved:<osn>`, `/last`); il `World.Mode` del file di specifica viene sostituito. Per eseguire una situazione salvata da una specifica, passare `/saved:`. `/map` e `/entrypoint`/`/entrypoint-index` sovrascrivono la base; un'identità `/entrypoint` della CLI azzera l'indice; `/saved` con `/map` o con flag del punto di ingresso è `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` sovrascrivono la base quando indicati (`system` seleziona `DateTimeMode.System`).
6. `/no-vehicle` azzera `PlayerVehicle`; i singoli `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` sovrascrivono i singoli campi del veicolo del giocatore della base.
7. Le voci `/set:<key>=<value>` vengono aggiunte a `Environment.General` (chiave controllata, valore no); gli altri sette gruppi provengono dalla base senza modifiche.
8. `/startup-timeout` e `/shutdown-timeout` sovrascrivono la base solo quando indicati; altrimenti si applicano la specifica, poi il profilo, poi i valori predefiniti 180 s / 30 s. `ShutdownTimeoutSeconds` ha effetto con `/cooperative-shutdown` (combinato in OR con il valore della specifica).
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` sovrascrivono la base quando indicati; `SuppressTrayIcon` proviene solo dalla base.
10. I flag di diagnostica vengono combinati in OR con la base.

Risultato: flag esplicito della CLI > profilo di sessione > file di specifica > valore predefinito incorporato, con l'eccezione che un flag della CLI in conflitto con un campo di proprietà del profilo è un errore anziché una sovrascrittura.

<a id="path-rules"></a>
## Regole dei percorsi

| Percorso | Comportamento dell'API | Comportamento della CLI |
| --- | --- | --- |
| `Installation.RootPath` | Usato così come fornito: nelle operazioni sui file i percorsi relativi vengono risolti rispetto alla directory di lavoro del processo. Passare un percorso assoluto. Il lease, il journal e il catalogo dei contenuti lo normalizzano con `Path.GetFullPath`. | `.` o vuoto = la directory che contiene `OmsiLaunch.exe`, mai la cartella di lavoro del chiamante; un argomento di installazione esplicito prevale sulla specifica; il risultato viene reso assoluto. |
| `Presentation.CustomAssetDirectory` | Assoluto o relativo a `Installation.RootPath`. Deve esistere. | Uguale (`/splash-assets`). Un percorso `assets` di un profilo di sessione è confinato al pacchetto del profilo e memorizzato come assoluto. |
| `InternetTextures.OverrideProfilePath` | Risolto con `Path.GetFullPath`, cioè relativo alla directory di lavoro del processo, non alla radice dell'installazione. Deve esistere. | Uguale (`/internet-textures-profile`). Un percorso `profile` di un profilo di sessione è confinato al pacchetto e memorizzato come assoluto. |
| Righe di destinazione ITX | Relative alla radice dell'installazione; devono contenere un componente `Texture\`; nessuna radice, nessun `..`, nessun `\` iniziale, nessun componente junction/symlink. | Uguale. |
| Identità dei contenuti (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Relative all'installazione, senza distinzione tra maiuscole e minuscole, `/` accettato; mai assolute. | Uguale. |

<a id="carried-but-not-applied"></a>
## Trasportati ma non applicati

| Campo | Effetto attuale | Stabilità |
| --- | --- | --- |
| `Diagnostics.Log`, `Verbose`, `OmsiLogAll`, `ProcessTrace` | nessuno (la traccia dell'host viene sempre scritta) | `PARTIAL` |
| `PlayerVehicle.*` (qualsiasi campo impostato) | contenuto risolto per la diagnostica, piano non eseguibile (`UNAVAILABLE`) | `PARTIAL` |
| `SessionProfile` | solo diagnostica di provenienza | `STABLE_BETA` |
