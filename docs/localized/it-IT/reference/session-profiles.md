# Profili di sessione

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Traduzione della [pagina originale in inglese](../../../reference/session-profiles.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Un profilo di sessione è un pacchetto YAML dichiarativo che un autore di contenuti distribuisce con una mappa o un add-on affinché gli utenti finali possano avviare una sessione OmsiLaunch riproducibile con un solo comando (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Questa pagina è il riferimento normativo per il formato `omsilaunch.session-profile/v1` così come è implementato da `SessionProfileCompiler` in `src/OmsiLaunch.Core/SessionProfiles.cs`, per le regole di precedenza applicate dalla CLI (`CliInput.BuildSpecAsync` e `RejectProfileConflicts` in `tools/OmsiLaunch.Cli/Program.cs`) e per il catalogo delle impostazioni che un profilo può scrivere (`ConfigurationCatalog`). Tutto ciò che un profilo può fare è possibile anche con i flag della CLI e con [LaunchSpec](launchspec.md); un profilo si limita a impacchettare quelle scelte.

Stabilità: `STABLE_BETA` per l'analisi, la validazione, il rilevamento dei conflitti e i blocchi `settings` / `presentation` / `internet-textures` / `behavior` (test offline `session-profiles.strict-compiler`; il percorso di overlay e ripristino è validato a runtime da RV-005 e RV-006, vedere [stato della validazione a runtime](../status/runtime-validation-status.md)). Le chiavi `new.date`, `new.time`, `new.year` e `new.weather` sono `UNAVAILABLE` in questo build (vedere [Il blocco `new`](#new)).

<a id="package-location-and-naming"></a>
## Posizione e denominazione del pacchetto

| Elemento | Regola |
| --- | --- |
| Directory del pacchetto | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| File del profilo | `<package>\profile.yaml` (nome esatto, un solo file) |
| Asset | Qualsiasi file o directory all'interno della directory del pacchetto, referenziati tramite percorso relativo da `presentation.splash.assets` e `internet-textures.profile` |
| `id` | Deve essere un semplice nome di directory: non deve essere vuoto o composto solo da spazi, non deve contenere `\`, `/` o `:` e non deve contenere la sequenza `..`. Le violazioni producono `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Il valore `id` dichiarato in `profile.yaml` deve essere uguale al nome della directory byte per byte (con distinzione tra maiuscole e minuscole); altrimenti `OL_E_SESSION_PROFILE_INVALID`. |
| Selezione | `/predefined-profile:<id>` insieme a `/predefined-profile-index:<n>`. L'indice è obbligatorio: `/predefined-profile` senza `/predefined-profile-index` fallisce con `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Pacchetto mancante | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Struttura del rilascio | Il pacchetto di rilascio include un esempio in `.omsilaunch\examples\session-profiles\rmg-leste\` (vedere [packaging](packaging.md)). Gli esempi non sono profili: copiare un pacchetto in `.omsilaunch\session-profiles\<id>\` per renderlo selezionabile. |

Un profilo viene installato e rimosso dall'utente o dall'autore dei contenuti. OmsiLaunch non scrive mai in un pacchetto, non lo copia mai e non lo elimina mai. La directory del pacchetto non fa parte di alcuna transazione.

<a id="parsing-rules"></a>
## Regole di analisi

| Regola | Comportamento | Errore |
| --- | --- | --- |
| Limite di dimensione | `profile.yaml` non deve superare 256 KiB (262,144 byte) | `OL_E_SESSION_PROFILE_INVALID` |
| Forma del documento | Esattamente un documento YAML il cui nodo radice è un mapping | `OL_E_SESSION_PROFILE_INVALID` |
| Schema | `schema` deve essere esattamente `omsilaunch.session-profile/v1` (con distinzione tra maiuscole e minuscole) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Anchor e alias | Qualsiasi nodo che porta un anchor YAML (`&name`) in qualunque punto del documento viene rifiutato prima della validazione; di conseguenza gli alias (`*name`) non possono comparire | `OL_E_SESSION_PROFILE_INVALID` ("YAML anchors are not supported.") |
| Chiavi sconosciute | Ogni mapping è chiuso: una chiave non elencata per il suo contesto nelle tabelle seguenti viene rifiutata ("Unknown property in `<context>`: `<key>`"). Le chiavi vengono confrontate con distinzione tra maiuscole e minuscole (`Schema:` è una chiave sconosciuta). L'unico mapping aperto è `settings`, le cui chiavi vengono invece validate rispetto al catalogo delle impostazioni. | `OL_E_SESSION_PROFILE_INVALID` |
| Scalari | Ogni valore foglia deve essere uno scalare; sequenze e mapping dove è atteso uno scalare vengono rifiutati ("`<field>` must be a scalar.") | `OL_E_SESSION_PROFILE_INVALID` |
| Numeri | Gli interi vengono analizzati con la cultura invariante (`1`, `30`); i decimali in `settings` usano `.` come separatore | `OL_E_SESSION_PROFILE_INVALID` |
| Date e orari | `new.date.value` viene analizzato da `DateOnly.Parse` e `new.time.value` da `TimeOnly.Parse`, entrambi con cultura invariante; usare le forme ISO `yyyy-MM-dd` e `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| Errori di sintassi YAML | Segnalati con il messaggio del parser | `OL_E_SESSION_PROFILE_INVALID` ("Invalid YAML: ...") |
| Contenuto eseguibile | Il YAML viene analizzato con `YamlDotNet` solo in un albero di rappresentazione; non sono supportati tag, tipi personalizzati o esecuzione di codice |

Le barre rovesciate negli scalari plain (senza virgolette) sono caratteri letterali. Scrivere i percorsi Windows con una sola barra rovesciata (`maps\Grundorf\global.cfg`). Una barra rovesciata doppia in uno scalare plain resta doppia nel valore; vedere [L'esempio incluso nel pacchetto](#the-packaged-example).

<a id="key-reference"></a>
## Riferimento delle chiavi

I contesti sono denominati esattamente come li denomina il compilatore. Ogni chiave elencata qui è accettata; nient'altro lo è.

<a id="profile-root-mapping"></a>
### `profile` (mapping radice)

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `schema` | stringa | sì | Letterale `omsilaunch.session-profile/v1`. |
| `id` | stringa | sì | Identificatore del pacchetto; deve essere uguale al nome della directory. |
| `name` | stringa | sì | Nome visualizzato; riportato in `SessionProfileMetadata.Name`. |
| `author` | stringa | sì | Autore; riportato in `SessionProfileMetadata.Author`. |
| `version` | stringa | sì | Stringa di versione del pacchetto (formato libero, racchiuderla tra virgolette: `"1.0"`); riportata in `SessionProfileMetadata.Version`. |
| `compatibility` | mapping | no | Vedere `compatibility`. |
| `new` | mapping | no | Valori predefiniti per NEW_MAP. Vedere `new`. |
| `presets` | sequenza di mapping | sì | Da 1 a 99 voci di preset. Zero, più di 99 o un valore che non è una sequenza producono `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `maps` | sequenza di stringhe | no | Identità delle mappe (`maps\<Map>\global.cfg`) per cui questo profilo è valido. `/` viene normalizzato in `\`; il confronto non distingue maiuscole e minuscole. Un elenco assente o vuoto significa «qualsiasi mappa». Quando non è vuoto viene imposto per `WorldMode.NewMap` (rispetto al `new.map` effettivo o a `/map`) e per `WorldMode.SavedSituation` (rispetto alla mappa referenziata dal file `.osn` selezionato, risolta tramite il catalogo dei contenuti). Per `WorldMode.LastMapState` non è possibile derivare alcuna mappa, quindi un elenco non vuoto fallisce sempre. Errore: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

Il blocco viene letto e validato ogni volta che è presente, ma viene applicato alla specifica solo quando la modalità del mondo selezionata è NEW_MAP (`/new`, il valore predefinito della CLI). Con `/saved:<file.osn>` il blocco viene ignorato.

| Chiave | Tipo | Obbligatoria | Applicata | Descrizione |
| --- | --- | --- | --- | --- |
| `map` | stringa | no | sì | Identità della mappa nella forma normalizzata `maps\<Map>\global.cfg` (la pianificazione richiede esattamente questa forma: inizia con `maps\`, termina con `\global.cfg`, nessun `..`). Imposta `WorldSpec.MapIdentity`. |
| `entrypoint-index` | intero | no | sì | Indice del punto di ingresso presentato (posizione a base 0 nell'elenco dei punti di ingresso di OMSI). Imposta `PresentedEntrypointIndex` e azzera qualsiasi identità del punto di ingresso. |
| `entrypoint` | stringa | no | sì | Identità grezza del punto di ingresso. Imposta `EntrypointIdentity` e azzera l'indice presentato. Se sono presenti sia `entrypoint-index` sia `entrypoint`, prevale `entrypoint` perché viene applicato per ultimo. Il valore è un'etichetta esatta (con distinzione tra maiuscole e minuscole) o un'identità di discovery; OMSI presenta solo il primo record di ciascuna etichetta (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapping | no | no (`UNAVAILABLE`) | Vedere `new.date`. |
| `time` | mapping | no | no (`UNAVAILABLE`) | Vedere `new.time`. |
| `year` | intero | no | no (`UNAVAILABLE`) | Anno esplicito. |
| `weather` | mapping | no | no (`UNAVAILABLE`) | Vedere `new.weather`. |

`date`, `time`, `year` e `weather` vengono compilati in `DateSpec`, `TimeSpec`, `YearSpec` e `WeatherSpec` con `DateTimeMode.Explicit` / il `WeatherMode` selezionato. Il planner di sessione (`src/OmsiLaunch.Core/SessionPlanner.cs`) richiede `world.explicit-date`, `world.explicit-time`, `world.explicit-year` e `world.weather`; tutte e quattro sono `EXPERIMENTAL` (chiuse staticamente, validazione a runtime in sospeso). La data/ora di avvio si applica solo a NEW_MAP; il meteo si applica a ogni modalità di mondo.

#### `new.date`

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `mode` | stringa | sì | Deve essere `explicit` (senza distinzione tra maiuscole e minuscole). Qualsiasi altro valore produce `OL_E_SESSION_PROFILE_INVALID` ("date must use explicit mode."). |
| `value` | stringa | sì | `yyyy-MM-dd`. |

#### `new.time`

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `mode` | stringa | sì | Deve essere `explicit`. |
| `value` | stringa | sì | `HH:mm` o `HH:mm:ss`. |

#### `new.weather`

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `mode` | stringa | sì | `preset`, `icao` o `real` (senza distinzione tra maiuscole e minuscole). Qualsiasi altro valore: `OL_E_SESSION_PROFILE_INVALID` ("Unsupported weather mode"). |
| `preset` | stringa | quando `mode: preset` | Nome del preset meteo. |
| `icao` | stringa | quando `mode: icao` | Codice ICAO della stazione. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (ogni voce di `presets`)

| Chiave | Tipo | Obbligatoria | Predefinito | Descrizione |
| --- | --- | --- | --- | --- |
| `index` | intero | sì | | Da 1 a 99, univoco all'interno del profilo; gli indici non devono essere contigui. Selezionato con `/predefined-profile-index`. Duplicato o fuori intervallo: `OL_E_SESSION_PROFILE_INVALID`; un indice che non esiste in alcun punto del profilo: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | stringa | sì | | Identificatore del preset; riportato come `SessionProfileMetadata.PresetId`. |
| `name` | stringa | sì | | Nome visualizzato del preset; riportato come `SessionProfileMetadata.PresetName`. |
| `settings` | mapping | no | nessuno | Impostazioni semantiche di `options.cfg`, vedere [Impostazioni](#settings). Le chiavi vengono confrontate con il catalogo senza distinzione tra maiuscole e minuscole. |
| `presentation` | mapping | no | ereditato | Presentazione della splash screen, vedere `presentation`. Se assente, il preset eredita la base (valore di `/spec` o il valore predefinito della CLI, `Managed`). |
| `internet-textures` | mapping | no | ereditato | Vedere `internet-textures`. |
| `behavior` | mapping | no | ereditato | Timeout, vedere `behavior`. |
| `hofs` | sequenza di mapping | no | ereditato | Operazioni HOF di sessione, vedere `hofs`. Quando presenti sostituiscono l'`HofManagement` di base. |
| `modules` | mapping | no | ereditato | Moduli runtime, vedere `modules`. Uniti a ciò che richiede un file `/spec`. |

Viene applicato solo il preset selezionato. Ogni preset viene comunque analizzato e validato, quindi un errore nel preset 3 fa fallire una richiesta per il preset 1.

### `presentation`

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `splash` | mapping | sì | Obbligatoria quando `presentation` è presente ("Presentation requires splash."). Vedere `presentation.splash`. |

#### `presentation.splash`

| Chiave | Tipo | Obbligatoria | Predefinito | Descrizione |
| --- | --- | --- | --- | --- |
| `mode` | stringa | sì | | `managed` installa le bitmap della splash screen di OmsiLaunch per la sessione (`SplashMode.Managed`). `unset` o `native` preserva i file della splash screen di OMSI (`SplashMode.Unset`; `Native` è un alias). Senza distinzione tra maiuscole e minuscole. Qualsiasi altro valore: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | stringa | no | `ENG` | Lingua della seconda destinazione della splash screen: `PTB`, `ENG`, `DEU`, `FRA` (alias `PT-BR`, `EN`, `DE`, `FR`; qualsiasi valore sconosciuto viene risolto in `ENG` alla costruzione della sessione). Con `mode: managed` la sessione applica in overlay `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | stringa | no | asset del pacchetto | Directory **relativa al pacchetto**, contenente `ENG.bmp` e, per una `language` diversa dall'inglese, `<language>.bmp`; ciascuno deve essere un BMP 640x480 a 24 bit. La directory deve esistere al caricamento del profilo (`OL_E_SESSION_PROFILE_ASSET_MISSING`); i file vengono validati all'avvio della sessione (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Si applicano le regole di confinamento dei percorsi. Se omessa, vengono usati `.omsilaunch\assets\splash` dell'installazione (o i valori predefiniti del pacchetto). |

Un profilo non può impostare `SessionPresentationSpec.SuppressTrayIcon`; resta `false` a meno che non lo imposti un `/spec`.

### `internet-textures`

| Chiave | Tipo | Obbligatoria | Descrizione |
| --- | --- | --- | --- |
| `mode` | stringa | sì | `native` (`InternetTexturesMode.Native`, OMSI si comporta normalmente), `disabled` (`Disabled`, il downloader in-process profilato viene soppresso per la sessione), `override` (`Override`, un profilo `.itx` limitato alla sessione viene installato come `Texture\standard.itx`). Senza distinzione tra maiuscole e minuscole; qualsiasi altro valore: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | stringa | obbligatoria per `override` | Percorso **relativo al pacchetto** del file `.itx`. Chiave mancante con `override`: `OL_E_SESSION_PROFILE_INVALID`; file mancante: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Si applicano le regole di confinamento dei percorsi. Il file deve essere composto da coppie di righe `URL` / `target` con URL `http://` o `https://` (altrimenti `OL_E_ITX_PROFILE_INVALID`) e ogni destinazione deve risolversi al di sotto della directory `Texture\` dell'installazione senza attraversare un reparse point (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Le destinazioni elencate e `Texture\standard.ipr` diventano eliminazioni della sessione (vedere [transazioni e recupero](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Chiave | Tipo | Obbligatoria | Predefinito | Descrizione |
| --- | --- | --- | --- | --- |
| `startup-timeout` | intero (secondi) | no | 180 | Tempo concesso dall'avvio del processo a `Running`. Deve essere positivo al caricamento del profilo; la sessione richiede inoltre un valore da 1 a 600 all'avvio (altrimenti `OL_E_START_SESSION`). Corrisponde a `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | intero (secondi) | no | 30 | Corrisponde a `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: il supervisore termina OMSI direttamente e non legge mai questo valore. |

Quando il blocco `behavior` è presente, entrambi i timeout vengono impostati (valore indicato o predefinito) e sostituiscono interamente il `LaunchBehaviorSpec` di base, inclusi `RestoreConfiguration` e `SuppressStaleClosecheckWarning`, che tornano ai loro valori predefiniti (`true`, `true`).

### `hofs`

Ogni voce è un'operazione di [gestione HOF](hof-management.md) e indica esattamente una chiave di sorgente (`package`, `collection`, `directory`, `vehicle` o `file`); zero o più di una: `OL_E_SESSION_PROFILE_INVALID`. L'esistenza di sorgenti e destinazioni viene controllata quando la sessione viene pianificata (`OL_E_HOF_*`), tranne per `package`.

| Chiave | Tipo | Obbligatoria | Predefinito | Descrizione |
| --- | --- | --- | --- | --- |
| `package` | stringa | una chiave di sorgente | | Directory **relativa al pacchetto** che contiene i file `.hof` distribuiti dal profilo. Deve esistere al caricamento del profilo (`OL_E_SESSION_PROFILE_ASSET_MISSING`); si applicano le regole di confinamento dei percorsi. Diventa una sorgente `Directory`. |
| `collection` | stringa | una chiave di sorgente | | Nome di una collezione sotto `.omsilaunch\hof\`. |
| `directory` | stringa | una chiave di sorgente | | Directory relativa all'installazione, per esempio accanto al contenuto di una mappa. |
| `vehicle` | stringa | una chiave di sorgente | | Identità o cartella di veicolo di cui vengono copiati i file HOF. |
| `file` | stringa | una chiave di sorgente | | File `.hof` relativo all'installazione. |
| `targets` | sequenza di stringhe | sì | | Veicoli di destinazione (identità o cartelle sotto `Vehicles\`). Mancante o vuota: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | sequenza di stringhe | no | tutti | Nomi di file da prelevare dalla sorgente. |
| `mode` | stringa | no | `add` | `add` o `mirror` (senza distinzione tra maiuscole e minuscole); qualsiasi altro valore: `OL_E_SESSION_PROFILE_INVALID`. |

Un preset con `hofs` combinato con un flag `/hof-source`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Una voce per ogni id di [modulo runtime](../../../reference/runtime-modules.md) (`EXPERIMENTAL`). Ogni voce è un mapping con queste chiavi (contesto `module`):

| Chiave | Tipo | Obbligatoria | Predefinito | Descrizione |
| --- | --- | --- | --- | --- |
| `enabled` | `true` o `false` | no | non impostato | `true` richiede il modulo (`LaunchSpec.Modules.Enable`); `false` lo tiene fuori dalla sessione, compreso un modulo caricato per impostazione predefinita, dalla sua mappa o dal file di impostazioni (`LaunchSpec.Modules.Disable`). |
| `settings` | mapping | no | nessuna | Impostazioni del modulo per chiave (`LaunchSpec.Modules.Settings` come `<id>.<key>`). I valori vengono controllati rispetto alle impostazioni dichiarate dal modulo quando la sessione viene pianificata (`OL_E_MODULE_SETTING_INVALID`). |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

Un id di modulo che non è `[a-z0-9][a-z0-9-]{1,47}`, un `enabled` diverso da `true` o `false`, oppure una chiave di impostazione che non è `[a-z][a-z0-9_]{0,31}`: `OL_E_SESSION_PROFILE_INVALID`. Un modulo non installato viene segnalato quando la sessione viene pianificata (`OL_E_MODULE_NOT_FOUND` per `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` per le sole impostazioni).

<a id="settings"></a>
## Impostazioni

Le chiavi di `settings` sono i nomi semantici di `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). Il compilatore accetta una chiave solo se esiste (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) ed è scrivibile (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). I valori vengono memorizzati come stringhe e convertiti in una modifica di `options.cfg` quando la sessione costruisce i propri overlay; un valore non valido viene quindi rilevato in `StartSessionAsync`, non al caricamento del profilo, e fa fallire la sessione con `OL_E_START_SESSION`, il cui messaggio contiene `OL_E_INVALID_SETTING_VALUE: <key>`. Ogni impostazione seguente scrive `options.cfg`; tutte sono limitate alla sessione e vengono ripristinate in modo esatto dopo la sessione.

Forme dei valori:

- **bool** è `true` o `false` (senza distinzione tra maiuscole e minuscole). Per i token di presenza il token viene aggiunto o rimosso; per i token invertiti (`no_*`) `true` rimuove il token negativo.
- **int** / **decimal** vengono validati rispetto all'intervallo; i valori con un divisore vengono memorizzati divisi (ad esempio `graphics.minObjectScreenPercent: 5` scrive `0.05`).
- **string** viene scritto testualmente.

| Chiave dell'impostazione | Token di `options.cfg` | Tipo | Intervallo / valori | Evidenza |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | qualsiasi | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | qualsiasi | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (presenza) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (presenza) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (presenza) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (presenza) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (presenza) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (presenza) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | qualsiasi | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, memorizzato /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, memorizzato /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (presenza) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (presenza) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, scritto come `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` o `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (blocco di 4 righe) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (presenza) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (presenza invertita) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (entrambi token di presenza) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (presenza) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (presenza) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (presenza) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (presenza) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` componente 0 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 runtime) |
| `traffic.humans` | `AIMaxCountRandom` componente 1 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 runtime) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Voci del catalogo che esistono ma **non sono scrivibili** (rifiutate con `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (sostituite da `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Confinamento dei percorsi

`presentation.splash.assets` e `internet-textures.profile` vengono risolti da `Confined(package root, value)`:

1. I percorsi radicati (`C:\...`), i percorsi che iniziano con `\` e qualsiasi componente del percorso uguale a `..` vengono rifiutati.
2. Viene calcolato il percorso completo, che deve iniziare con la directory del pacchetto.
3. Ogni componente esistente al di sotto della radice del pacchetto, fino al percorso finale incluso, viene ispezionato per verificare l'attributo `ReparsePoint`. Una junction, un collegamento simbolico a directory o un collegamento simbolico a file in qualunque punto di quel percorso viene rifiutato, così come un componente che non può essere ispezionato (`IOException` / `UnauthorizedAccessException`).

Tutti e tre gli errori producono `OL_E_SESSION_PROFILE_PATH_ESCAPE`. La stessa regola sui reparse point viene applicata alle destinazioni `.itx` sotto `Texture\` alla costruzione della sessione.

<a id="precedence-and-override-conflicts"></a>
## Precedenza e conflitti di sovrascrittura

`CliInput.BuildSpecAsync` compone la specifica in questo ordine:

1. **Valori predefiniti** (NEW_MAP, tutto non impostato, timeout 180 s / 30 s).
2. **`/spec:<file.json>`**, se indicato, sostituisce interamente i valori predefiniti.
3. **Radice dell'installazione**: un argomento di installazione esplicito prevale sul `RootPath` della specifica; `.` indica la directory che contiene l'eseguibile.
4. **Profilo** (`/predefined-profile` + `/predefined-profile-index`): il pacchetto viene caricato e `RejectProfileConflicts` viene eseguito sugli argomenti grezzi della CLI **prima** che venga unito qualsiasi elemento. Il blocco del mondo della base viene quindi reimpostato a un `WorldSpec` vuoto della modalità selezionata (un mondo di `/spec` viene scartato quando si usa un profilo) e `SessionProfileCompiler.Apply` sovrappone il profilo alla base: `new` (solo NEW_MAP), `settings` (unite sopra l'`Environment.General` della base, il profilo prevale per ogni chiave) e `presentation`, `internet-textures`, `behavior` (ciascuno sostituisce il blocco della base solo quando il preset lo definisce).
5. **Argomenti della CLI rimanenti** vengono sovrapposti: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, flag meteo, flag del veicolo, `/set`, flag della splash, flag delle internet-textures, `/startup-timeout`, `/shutdown-timeout`. I timeout della CLI si applicano solo quando indicati; altrimenti resta valido il valore della specifica/del profilo/predefinito.
6. **Controllo di compatibilità** per le modalità diverse da NEW_MAP (`ValidateCompatibility`).

Un argomento della CLI che punta a un campo di proprietà del profilo selezionato è un conflitto, rifiutato con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (codice di uscita 2, categoria `invalid_argument`). Il controllo è per campo, non per valore: ripetere lo stesso valore del profilo è comunque un conflitto.

| Argomento della CLI | È in conflitto quando il profilo definisce | Solo in modalità |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` o `/entrypoint-index` | `new.entrypoint` o `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | la stessa `<key>` nelle `settings` del preset (senza distinzione tra maiuscole e minuscole) | qualsiasi |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (qualsiasi) | qualsiasi |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (qualsiasi) | qualsiasi |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (qualsiasi) | qualsiasi |
| `/module:<id>` | `modules.<id>.enabled: false` | qualsiasi |
| `/no-module:<id>` | `modules.<id>.enabled: true` | qualsiasi |
| `/module-setting:<id>.<key>=...` o un flag di modulo | `modules.<id>.settings.<key>` | qualsiasi |

Non sono conflitti: le chiavi `/set` che il preset non definisce (vengono aggiunte), i flag del veicolo (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; un profilo non può definire un veicolo del giocatore) e qualsiasi argomento del mondo con `/saved` (lì il blocco `new` non viene applicato). `/map`, `/entrypoint` e `/entrypoint-index` non sono validi insieme a `/saved` indipendentemente dai profili (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Codici di errore

| Codice | Generato quando | Uscita CLI |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` non esiste | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` non è un semplice nome di directory; `assets` / `profile` esce dal pacchetto o attraversa un reparse point | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` non è `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | limite di dimensione, forma del documento, anchor, chiave sconosciuta, chiave obbligatoria mancante, valore non scalare, numero/data/orario non valido, `id` non corrispondente, regole su numero/indice dei preset, parole di modalità non supportate, timeout non positivo, `presentation` senza `splash`, `override` senza `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` mancante, fuori da 1..99 o nessun preset con quell'`index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | una chiave di `settings` non è nel catalogo | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | una chiave di `settings` è nel catalogo ma è di sola lettura | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | la directory `assets` o il file `profile` non esiste all'interno del pacchetto | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` non è vuoto e la mappa effettiva non è elencata (o non può essere derivata) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | un argomento esplicito della CLI punta a un campo di proprietà del profilo | 2 |

Tutti questi errori vengono generati durante la compilazione della riga di comando, prima della pianificazione. Sono `SessionProfileException` (o `ArgumentException` per il conflitto) e non avviano mai una sessione. Il catalogo completo si trova in [errori](errors.md); i codici di uscita in [codici di uscita](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Individuazione dei profili installati

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) compila ogni pacchetto sotto `.omsilaunch\session-profiles\` e stampa una voce per pacchetto: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` e i suoi `Presets` (`Index`, `Id`, `Name`) in ordine di indice. Un pacchetto che non compila viene comunque elencato, con `IsValid: false` e lo stesso codice `OL_E_SESSION_PROFILE_*` che segnalerebbe un avvio. Non viene scritto nulla.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Come appare un profilo nell'API

Dopo un caricamento riuscito la specifica contiene un record `SessionProfileMetadata` in `LaunchSpec.SessionProfile`:

| Campo | Origine |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` del preset selezionato |
| `PresetIndex` | `index` del preset selezionato |
| `PresetName` | `name` del preset selezionato |
| `PackagePath` | directory assoluta del pacchetto |

Il planner aggiunge una voce di diagnostica informativa `session_profile.selected` a ogni `SessionPlan` costruito da una tale specifica, con le chiavi di dati `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` e `session_profile.path`. Non influisce sull'eseguibilità. Gli integratori che usano direttamente l'[API pubblica](public-api.md) possono chiamare `SessionProfileCompiler.Load` e `SessionProfileCompiler.Apply` da `OmsiLaunch.Core`; la rappresentazione YAML non attraversa mai il confine verso `OmsiLaunch.Api`.

<a id="examples"></a>
## Esempi

<a id="example-1-settings-only-profile-one-preset"></a>
### Esempio 1: profilo con sole impostazioni, un preset

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

Esecuzione: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. La mappa e il punto di ingresso provengono dalla riga di comando perché il profilo non definisce alcun blocco `new`; aggiungere `/set:graphics.maxFPS=60` è consentito, aggiungere `/set:traffic.humans=10` è un conflitto.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Esempio 2: profilo legato a una mappa con tre preset e asset nel pacchetto

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, con `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` e `textures\offline.itx` all'interno del pacchetto:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

Esecuzione: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Con `/saved:situations\mytrip.osn` il blocco `new` viene saltato e il file `.osn` deve referenziare `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### L'esempio incluso nel pacchetto

Il rilascio include `docs/examples/session-profiles/rmg-leste/profile.yaml` ([visualizza](../../../examples/session-profiles/rmg-leste/profile.yaml)). È sintatticamente valido, rispetta lo schema e verrebbe caricato senza errori. Due caratteristiche gli impediscono di avviare una sessione senza modifiche in questo build:

1. Imposta `new.date`, `new.time` e `new.weather`, che rendono il piano non eseguibile (vedere [Il blocco `new`](#new)).
2. I suoi valori di percorso sono scalari plain con barre rovesciate doppie (`maps\\RMG Leste\\global.cfg`). YAML le mantiene doppie e le identità delle mappe vengono confrontate testualmente (solo dopo la normalizzazione da `/` a `\`), quindi `new.map` e `compatibility.maps` non corrisponderebbero all'identità di catalogo `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` in fase di pianificazione). Il valore `assets` viene comunque risolto perché la normalizzazione dei percorsi di Windows comprime i separatori doppi.

La forma eseguibile per questo build è:

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## Pagine correlate

- [Riferimento CLI](cli.md) per `/predefined-profile`, `/predefined-profile-index`, `/set` e i flag del mondo
- [LaunchSpec](launchspec.md) per il record in cui viene compilato un profilo
- [Transazioni e recupero](../concepts/transactions-and-recovery.md) per il modo in cui gli overlay di `settings`, della splash screen e `.itx` vengono applicati e ripristinati
- [Capability](capabilities.md) e [stato della validazione a runtime](../status/runtime-validation-status.md)
