# Perfiles de sesión

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Traducción de la [página original en inglés](../../../reference/session-profiles.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Un perfil de sesión es un paquete YAML declarativo que un autor de contenido distribuye con un mapa o un complemento para que los usuarios finales puedan iniciar una sesión de OmsiLaunch reproducible con un solo comando (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Esta página es la referencia normativa del formato `omsilaunch.session-profile/v1` tal como lo implementa `SessionProfileCompiler` en `src/OmsiLaunch.Core/SessionProfiles.cs`, de las reglas de precedencia que aplica la CLI (`CliInput.BuildSpecAsync` y `RejectProfileConflicts` en `tools/OmsiLaunch.Cli/Program.cs`) y del catálogo de ajustes que un perfil puede escribir (`ConfigurationCatalog`). Todo lo que puede hacer un perfil también pueden hacerlo los flags de la CLI y el [LaunchSpec](launchspec.md); un perfil solo empaqueta esas elecciones.

Estabilidad: `STABLE_BETA` para el análisis, la validación, la detección de conflictos y los bloques `settings` / `presentation` / `internet-textures` / `behavior` (prueba offline `session-profiles.strict-compiler`; la ruta de overlay y restauración está validada en runtime por RV-005 y RV-006; consulta el [estado de la validación en runtime](../status/runtime-validation-status.md)). Las claves `new.date`, `new.time`, `new.year` y `new.weather` son `UNAVAILABLE` en esta build (consulta [El bloque `new`](#new)).

<a id="package-location-and-naming"></a>
## Ubicación y nombre del paquete

| Elemento | Regla |
| --- | --- |
| Directorio del paquete | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Archivo del perfil | `<package>\profile.yaml` (nombre exacto, un solo archivo) |
| Recursos | Cualquier archivo o directorio dentro del directorio del paquete, referenciado mediante ruta relativa desde `presentation.splash.assets` e `internet-textures.profile` |
| `id` | Debe ser un nombre de directorio simple: no puede estar vacío ni contener solo espacios en blanco, no puede contener `\`, `/` ni `:` y no puede contener la secuencia `..`. Las infracciones producen `OL_E_SESSION_PROFILE_PATH_ESCAPE`. El valor `id` declarado dentro de `profile.yaml` debe ser igual al nombre del directorio byte a byte (distinguiendo mayúsculas de minúsculas); en caso contrario, `OL_E_SESSION_PROFILE_INVALID`. |
| Selección | `/predefined-profile:<id>` junto con `/predefined-profile-index:<n>`. El índice es obligatorio: `/predefined-profile` sin `/predefined-profile-index` falla con `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Paquete inexistente | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Estructura de la versión | El paquete de la versión incluye un ejemplo en `.omsilaunch\examples\session-profiles\rmg-leste\` (consulta [empaquetado](packaging.md)). Los ejemplos no son perfiles: copia un paquete en `.omsilaunch\session-profiles\<id>\` para que se pueda seleccionar. |

Un perfil lo instala y lo quita el usuario o el autor del contenido. OmsiLaunch nunca escribe en un paquete, nunca lo copia y nunca lo elimina. El directorio del paquete no forma parte de ninguna transacción.

<a id="parsing-rules"></a>
## Reglas de análisis

| Regla | Comportamiento | Error |
| --- | --- | --- |
| Límite de tamaño | `profile.yaml` no debe superar 256 KiB (262,144 bytes) | `OL_E_SESSION_PROFILE_INVALID` |
| Forma del documento | Exactamente un documento YAML cuyo nodo raíz es un mapeo | `OL_E_SESSION_PROFILE_INVALID` |
| Esquema | `schema` debe ser exactamente `omsilaunch.session-profile/v1` (distingue mayúsculas de minúsculas) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Anclas y alias | Cualquier nodo que lleve un ancla YAML (`&name`) en cualquier parte del documento se rechaza antes de la validación; por tanto, no pueden aparecer alias (`*name`) | `OL_E_SESSION_PROFILE_INVALID` ("YAML anchors are not supported.") |
| Claves desconocidas | Todos los mapeos son cerrados: una clave que no figura para su contexto en las tablas siguientes se rechaza ("Unknown property in `<context>`: `<key>`"). Las claves distinguen mayúsculas de minúsculas (`Schema:` es una clave desconocida). El único mapeo abierto es `settings`, cuyas claves se validan en su lugar contra el catálogo de ajustes. | `OL_E_SESSION_PROFILE_INVALID` |
| Escalares | Todo valor hoja debe ser un escalar; se rechazan secuencias y mapeos donde se espera un escalar ("`<field>` must be a scalar.") | `OL_E_SESSION_PROFILE_INVALID` |
| Números | Los enteros se analizan con la referencia cultural invariable (`1`, `30`); los decimales de `settings` usan `.` como separador | `OL_E_SESSION_PROFILE_INVALID` |
| Fechas y horas | `new.date.value` se analiza con `DateOnly.Parse` y `new.time.value` con `TimeOnly.Parse`, ambos con la referencia cultural invariable; usa las formas ISO `yyyy-MM-dd` y `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| Errores de sintaxis YAML | Se notifican con el mensaje del analizador | `OL_E_SESSION_PROFILE_INVALID` ("Invalid YAML: ...") |
| Contenido ejecutable | El YAML se analiza con `YamlDotNet` solo en un árbol de representación; no se admiten etiquetas, tipos personalizados ni ejecución de código |

Las barras invertidas de los escalares simples (sin comillas) son caracteres literales. Escribe las rutas de Windows con una sola barra invertida (`maps\Grundorf\global.cfg`). Una barra invertida duplicada en un escalar simple permanece duplicada en el valor; consulta [El ejemplo incluido en el paquete](#the-packaged-example).

<a id="key-reference"></a>
## Referencia de claves

Los contextos se nombran exactamente como los nombra el compilador. Se acepta toda clave que figure aquí; nada más.

<a id="profile-root-mapping"></a>
### `profile` (mapeo raíz)

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `schema` | string | sí | Literal `omsilaunch.session-profile/v1`. |
| `id` | string | sí | Identificador del paquete; debe ser igual al nombre del directorio. |
| `name` | string | sí | Nombre para mostrar; se notifica en `SessionProfileMetadata.Name`. |
| `author` | string | sí | Autor; se notifica en `SessionProfileMetadata.Author`. |
| `version` | string | sí | Cadena de versión del paquete (formato libre, entre comillas: `"1.0"`); se notifica en `SessionProfileMetadata.Version`. |
| `compatibility` | mapeo | no | Consulta `compatibility`. |
| `new` | mapeo | no | Valores predeterminados de NEW_MAP. Consulta `new`. |
| `presets` | secuencia de mapeos | sí | De 1 a 99 entradas de preset. Cero, más de 99 o un valor que no sea una secuencia produce `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `maps` | secuencia de cadenas | no | Identidades de mapa (`maps\<Map>\global.cfg`) para las que este perfil es válido. `/` se normaliza a `\`; la comparación no distingue mayúsculas de minúsculas. Una lista ausente o vacía significa «cualquier mapa». Cuando no está vacía, se impone para `WorldMode.NewMap` (frente al `new.map` o `/map` efectivo) y para `WorldMode.SavedSituation` (frente al mapa referenciado por el `.osn` seleccionado, resuelto mediante el catálogo de contenido). Para `WorldMode.LastMapState` no se puede derivar ningún mapa, por lo que una lista no vacía siempre falla. Fallo: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

El bloque se lee y se valida siempre que está presente, pero solo se aplica a la especificación cuando el modo de mundo seleccionado es NEW_MAP (`/new`, el predeterminado de la CLI). Con `/saved:<file.osn>` el bloque se ignora.

| Clave | Tipo | Obligatoria | Aplicada | Descripción |
| --- | --- | --- | --- | --- |
| `map` | string | no | sí | Identidad de mapa en la forma normalizada `maps\<Map>\global.cfg` (la planificación exige exactamente esta forma: empieza por `maps\`, termina en `\global.cfg`, sin `..`). Establece `WorldSpec.MapIdentity`. |
| `entrypoint-index` | integer | no | sí | Índice de punto de entrada presentado (posición basada en 0 en la lista de puntos de entrada de OMSI). Establece `PresentedEntrypointIndex` y borra cualquier identidad de punto de entrada. |
| `entrypoint` | string | no | sí | Identidad de punto de entrada sin procesar. Establece `EntrypointIdentity` y borra el índice presentado. Si están presentes tanto `entrypoint-index` como `entrypoint`, prevalece `entrypoint` porque se aplica en último lugar. El valor es una etiqueta exacta (con distinción de mayúsculas y minúsculas) o una identidad de descubrimiento; OMSI presenta solo el primer registro de cada etiqueta (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapeo | no | no (`UNAVAILABLE`) | Consulta `new.date`. |
| `time` | mapeo | no | no (`UNAVAILABLE`) | Consulta `new.time`. |
| `year` | integer | no | no (`UNAVAILABLE`) | Año explícito. |
| `weather` | mapeo | no | no (`UNAVAILABLE`) | Consulta `new.weather`. |

`date`, `time`, `year` y `weather` se compilan en `DateSpec`, `TimeSpec`, `YearSpec` y `WeatherSpec` con `DateTimeMode.Explicit` / el `WeatherMode` seleccionado. El planificador de sesiones (`src/OmsiLaunch.Core/SessionPlanner.cs`) exige `world.explicit-date`, `world.explicit-time`, `world.explicit-year` y `world.weather`; las cuatro son `EXPERIMENTAL` (cerradas estáticamente, validación en runtime pendiente). La fecha y la hora de inicio se aplican solo a NEW_MAP; la meteorología se aplica a todos los modos de mundo.

#### `new.date`

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `mode` | string | sí | Debe ser `explicit` (sin distinguir mayúsculas de minúsculas). Cualquier otro valor produce `OL_E_SESSION_PROFILE_INVALID` ("date must use explicit mode."). |
| `value` | string | sí | `yyyy-MM-dd`. |

#### `new.time`

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `mode` | string | sí | Debe ser `explicit`. |
| `value` | string | sí | `HH:mm` o `HH:mm:ss`. |

#### `new.weather`

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `mode` | string | sí | `preset`, `icao` o `real` (sin distinguir mayúsculas de minúsculas). Cualquier otro valor: `OL_E_SESSION_PROFILE_INVALID` ("Unsupported weather mode"). |
| `preset` | string | con `mode: preset` | Nombre del preset meteorológico. |
| `icao` | string | con `mode: icao` | Código ICAO de la estación. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (cada entrada de `presets`)

| Clave | Tipo | Obligatoria | Valor predeterminado | Descripción |
| --- | --- | --- | --- | --- |
| `index` | integer | sí | | De 1 a 99, único dentro del perfil; los índices no tienen por qué ser contiguos. Se selecciona con `/predefined-profile-index`. Duplicado o fuera de intervalo: `OL_E_SESSION_PROFILE_INVALID`; un índice que no existe en ninguna parte del perfil: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | string | sí | | Identificador del preset; se notifica como `SessionProfileMetadata.PresetId`. |
| `name` | string | sí | | Nombre para mostrar del preset; se notifica como `SessionProfileMetadata.PresetName`. |
| `settings` | mapeo | no | ninguno | Ajustes semánticos de `options.cfg`; consulta [Ajustes](#settings). Las claves se comparan con el catálogo sin distinguir mayúsculas de minúsculas. |
| `presentation` | mapeo | no | heredado | Presentación del splash; consulta `presentation`. Cuando falta, el preset hereda la base (valor de `/spec` o el predeterminado de la CLI, `Managed`). |
| `internet-textures` | mapeo | no | heredado | Consulta `internet-textures`. |
| `behavior` | mapeo | no | heredado | Timeouts; consulta `behavior`. |
| `hofs` | secuencia de mapeos | no | heredado | Operaciones de HOF de la sesión; consulta `hofs`. Cuando están presentes, sustituyen el `HofManagement` base. |
| `modules` | mapeo | no | heredado | Módulos de runtime; consulta `modules`. Se unen a lo que pide un archivo `/spec`. |

Solo se aplica el preset seleccionado. Aun así, todos los presets se analizan y se validan, de modo que un error en el preset 3 hace fallar una solicitud del preset 1.

### `presentation`

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `splash` | mapeo | sí | Obligatoria cuando `presentation` está presente ("Presentation requires splash."). Consulta `presentation.splash`. |

#### `presentation.splash`

| Clave | Tipo | Obligatoria | Valor predeterminado | Descripción |
| --- | --- | --- | --- | --- |
| `mode` | string | sí | | `managed` instala los mapas de bits de splash de OmsiLaunch durante la sesión (`SplashMode.Managed`). `unset` o `native` conserva los archivos de splash propios de OMSI (`SplashMode.Unset`; `Native` es un alias). Sin distinguir mayúsculas de minúsculas. Cualquier otro valor: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | string | no | `ENG` | Idioma del segundo destino de splash: `PTB`, `ENG`, `DEU`, `FRA` (alias `PT-BR`, `EN`, `DE`, `FR`; cualquier valor desconocido se resuelve a `ENG` al construir la sesión). Con `mode: managed` la sesión aplica como overlay `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | string | no | recursos incluidos en el paquete | Directorio **relativo al paquete** que contiene `ENG.bmp` y, para un `language` distinto del inglés, `<language>.bmp`; cada uno debe ser un BMP de 640x480 y 24 bits. El directorio debe existir al cargar el perfil (`OL_E_SESSION_PROFILE_ASSET_MISSING`); los archivos se validan al iniciar la sesión (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Se aplican las reglas de confinamiento de rutas. Cuando se omite, se usa `.omsilaunch\assets\splash` de la instalación (o los valores predeterminados incluidos en el paquete). |

Un perfil no puede establecer `SessionPresentationSpec.SuppressTrayIcon`; permanece en `false` salvo que un `/spec` lo establezca.

### `internet-textures`

| Clave | Tipo | Obligatoria | Descripción |
| --- | --- | --- | --- |
| `mode` | string | sí | `native` (`InternetTexturesMode.Native`, OMSI se comporta con normalidad), `disabled` (`Disabled`, el descargador en proceso perfilado se suprime durante la sesión), `override` (`Override`, se instala un perfil `.itx` limitado a la sesión como `Texture\standard.itx`). Sin distinguir mayúsculas de minúsculas; cualquier otro valor: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | string | obligatoria para `override` | Ruta **relativa al paquete** del archivo `.itx`. Clave ausente con `override`: `OL_E_SESSION_PROFILE_INVALID`; archivo inexistente: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Se aplican las reglas de confinamiento de rutas. El archivo debe constar de pares de líneas `URL` / `target` con URL `http://` o `https://` (en caso contrario, `OL_E_ITX_PROFILE_INVALID`) y cada destino debe resolverse por debajo del directorio `Texture\` de la instalación sin atravesar un punto de reanálisis (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Los destinos enumerados y `Texture\standard.ipr` pasan a ser eliminaciones de sesión (consulta [transacciones y recuperación](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Clave | Tipo | Obligatoria | Valor predeterminado | Descripción |
| --- | --- | --- | --- | --- |
| `startup-timeout` | integer (segundos) | no | 180 | Tiempo permitido desde el inicio del proceso hasta `Running`. Debe ser positivo al cargar el perfil; además, la sesión exige un valor de 1 a 600 en el inicio (en caso contrario, `OL_E_START_SESSION`). Se corresponde con `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | integer (segundos) | no | 30 | Se corresponde con `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: el supervisor termina OMSI directamente y nunca lee este valor. |

Cuando el bloque `behavior` está presente, se establecen ambos timeouts (valor indicado o predeterminado) y sustituyen por completo el `LaunchBehaviorSpec` base, incluidos `RestoreConfiguration` y `SuppressStaleClosecheckWarning`, que vuelven a sus valores predeterminados (`true`, `true`).

### `hofs`

Cada entrada es una operación de [gestión de HOF](hof-management.md) y nombra exactamente una clave de origen (`package`, `collection`, `directory`, `vehicle` o `file`); cero o varias: `OL_E_SESSION_PROFILE_INVALID`. La existencia de los orígenes y los destinos se comprueba al planificar la sesión (`OL_E_HOF_*`), excepto para `package`.

| Clave | Tipo | Obligatoria | Valor predeterminado | Descripción |
| --- | --- | --- | --- | --- |
| `package` | string | una clave de origen | | Directorio **relativo al paquete** que contiene los archivos `.hof` que distribuye el perfil. Debe existir al cargar el perfil (`OL_E_SESSION_PROFILE_ASSET_MISSING`); se aplican las reglas de confinamiento de rutas. Se convierte en un origen `Directory`. |
| `collection` | string | una clave de origen | | Nombre de una colección bajo `.omsilaunch\hof\`. |
| `directory` | string | una clave de origen | | Directorio relativo a la instalación, por ejemplo junto al contenido del mapa. |
| `vehicle` | string | una clave de origen | | Identidad o carpeta del vehículo cuyos HOF se copian. |
| `file` | string | una clave de origen | | Archivo `.hof` relativo a la instalación. |
| `targets` | secuencia de strings | sí | | Vehículos de destino (identidades o carpetas bajo `Vehicles\`). Ausente o vacía: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | secuencia de strings | no | todos | Nombres de archivo que se toman del origen. |
| `mode` | string | no | `add` | `add` o `mirror` (sin distinguir mayúsculas de minúsculas); cualquier otro valor: `OL_E_SESSION_PROFILE_INVALID`. |

Un preset con `hofs` combinado con un flag `/hof-source`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Una entrada por id de [módulo de runtime](runtime-modules.md) (`EXPERIMENTAL`). Cada entrada es un mapeo con estas claves (contexto `module`):

| Clave | Tipo | Obligatoria | Valor predeterminado | Descripción |
| --- | --- | --- | --- | --- |
| `enabled` | `true` o `false` | no | sin establecer | `true` solicita el módulo (`LaunchSpec.Modules.Enable`); `false` lo mantiene fuera de la sesión, incluido un módulo cargado de forma predeterminada, por su mapa o por el archivo de configuración (`LaunchSpec.Modules.Disable`). |
| `settings` | mapeo | no | ninguno | Ajustes del módulo por clave (`LaunchSpec.Modules.Settings` como `<id>.<key>`). Los valores se comprueban con los ajustes declarados del módulo al planificar la sesión (`OL_E_MODULE_SETTING_INVALID`). |

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

Un id de módulo que no sea `[a-z0-9][a-z0-9-]{1,47}`, un `enabled` distinto de `true` o `false`, o una clave de ajuste que no sea `[a-z][a-z0-9_]{0,31}`: `OL_E_SESSION_PROFILE_INVALID`. Un módulo que no está instalado se notifica al planificar la sesión (`OL_E_MODULE_NOT_FOUND` para `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` solo para ajustes).

<a id="settings"></a>
## Ajustes

Las claves de `settings` son los nombres semánticos de `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). El compilador acepta una clave solo si existe (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) y se puede escribir (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Los valores se almacenan como cadenas y se convierten en una modificación de `options.cfg` cuando la sesión construye sus overlays; por tanto, un valor no válido se detecta en `StartSessionAsync`, no al cargar el perfil, y hace fallar la sesión con `OL_E_START_SESSION`, cuyo mensaje incluye `OL_E_INVALID_SETTING_VALUE: <key>`. Todos los ajustes siguientes escriben en `options.cfg`; todos están limitados a la sesión y se restauran exactamente después de ella.

Formas de valor:

- **bool** es `true` o `false` (sin distinguir mayúsculas de minúsculas). Para los tokens de presencia, el token se añade o se quita; para los tokens invertidos (`no_*`), `true` quita el token negativo.
- **int** / **decimal** se validan frente al intervalo; los valores con divisor se almacenan divididos (por ejemplo, `graphics.minObjectScreenPercent: 5` escribe `0.05`).
- **string** se escribe literalmente.

| Clave del ajuste | Token de `options.cfg` | Tipo | Intervalo / valores | Evidencia |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | cualquiera | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | cualquiera | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (presencia) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (presencia) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (presencia) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (presencia) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (presencia) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (presencia) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | cualquiera | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, se almacena /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, se almacena /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (presencia) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (presencia) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, se escribe como `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` o `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (bloque de 4 líneas) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (presencia) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (presencia invertida) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (ambos tokens de presencia) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (presencia) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (presencia) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (presencia) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (presencia) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | componente 0 de `AIMaxCountRandom` | int | 0..1000 | STATICALLY_VALIDATED (RV-005 en runtime) |
| `traffic.humans` | componente 1 de `AIMaxCountRandom` | int | 0..1000 | STATICALLY_VALIDATED (RV-005 en runtime) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Entradas del catálogo que existen pero **no se pueden escribir** (se rechazan con `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (sustituidas por `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Confinamiento de rutas

`presentation.splash.assets` e `internet-textures.profile` se resuelven mediante `Confined(package root, value)`:

1. Se rechazan las rutas con raíz (`C:\...`), las rutas que empiezan por `\` y cualquier componente de ruta igual a `..`.
2. Se calcula la ruta completa, que debe empezar por el directorio del paquete.
3. Cada componente existente por debajo de la raíz del paquete, hasta la ruta final incluida, se inspecciona en busca del atributo `ReparsePoint`. Se rechaza una unión, un enlace simbólico de directorio o un enlace simbólico de archivo en cualquier punto de esa ruta, así como un componente que no se pueda inspeccionar (`IOException` / `UnauthorizedAccessException`).

Los tres fallos producen `OL_E_SESSION_PROFILE_PATH_ESCAPE`. La misma regla de puntos de reanálisis se aplica a los destinos `.itx` bajo `Texture\` al construir la sesión.

<a id="precedence-and-override-conflicts"></a>
## Precedencia y conflictos de sobrescritura

`CliInput.BuildSpecAsync` compone la especificación en este orden:

1. **Valores predeterminados** (NEW_MAP, todo sin establecer, timeouts de 180 s / 30 s).
2. **`/spec:<file.json>`**, si se indica, sustituye por completo los valores predeterminados.
3. **Raíz de la instalación**: un argumento de instalación explícito prevalece sobre el `RootPath` de la especificación; `.` significa el directorio que contiene el ejecutable.
4. **Perfil** (`/predefined-profile` + `/predefined-profile-index`): se carga el paquete y se ejecuta `RejectProfileConflicts` sobre los argumentos sin procesar de la CLI **antes** de combinar nada. Después, el bloque de mundo de la semilla se restablece a un `WorldSpec` vacío del modo seleccionado (el mundo de un `/spec` se descarta cuando se usa un perfil) y `SessionProfileCompiler.Apply` superpone el perfil a la semilla: `new` (solo NEW_MAP), `settings` (combinado sobre el `Environment.General` de la semilla; el perfil prevalece por clave) y `presentation`, `internet-textures`, `behavior` (cada uno sustituye el bloque de la semilla solo cuando el preset lo define).
5. **Argumentos restantes de la CLI**: se superponen encima: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, flags meteorológicos, flags de vehículo, `/set`, flags de splash, flags de texturas de Internet, `/startup-timeout`, `/shutdown-timeout`. Los timeouts de la CLI solo se aplican cuando se indican; en caso contrario se mantiene el valor de la especificación, del perfil o el predeterminado.
6. **Comprobación de compatibilidad** para los modos distintos de NEW_MAP (`ValidateCompatibility`).

Un argumento de la CLI que afecta a un campo propiedad del perfil seleccionado es un conflicto y se rechaza con `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (código de salida 2, categoría `invalid_argument`). La comprobación es por campo, no por valor: repetir el mismo valor del perfil sigue siendo un conflicto.

| Argumento de la CLI | Entra en conflicto cuando el perfil define | Solo en el modo |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` o `/entrypoint-index` | `new.entrypoint` o `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | la misma `<key>` en los `settings` del preset (sin distinguir mayúsculas de minúsculas) | cualquiera |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (cualquiera) | cualquiera |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (cualquiera) | cualquiera |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (cualquiera) | cualquiera |
| `/module:<id>` | `modules.<id>.enabled: false` | cualquiera |
| `/no-module:<id>` | `modules.<id>.enabled: true` | cualquiera |
| `/module-setting:<id>.<key>=...` o un flag de módulo | `modules.<id>.settings.<key>` | cualquiera |

No son conflictos: las claves de `/set` que el preset no define (se añaden), los flags de vehículo (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; un perfil no puede definir un vehículo del jugador) y cualquier argumento de mundo con `/saved` (allí no se aplica el bloque `new`). `/map`, `/entrypoint` y `/entrypoint-index` no son válidos junto con `/saved`, con independencia de los perfiles (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Códigos de error

| Código | Se produce cuando | Salida de la CLI |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` no existe | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` no es un nombre de directorio simple; `assets` / `profile` sale del paquete o atraviesa un punto de reanálisis | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` no es `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | límite de tamaño, forma del documento, anclas, clave desconocida, falta una clave obligatoria, valor no escalar, número/fecha/hora incorrectos, discrepancia de `id`, reglas de número o índice de presets, palabras de modo no admitidas, timeout no positivo, `presentation` sin `splash`, `override` sin `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | falta `/predefined-profile-index`, está fuera de 1..99 o no hay ningún preset con ese `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | una clave de `settings` no está en el catálogo | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | una clave de `settings` está en el catálogo pero es de solo lectura | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | el directorio `assets` o el archivo `profile` no existe dentro del paquete | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` no está vacío y el mapa efectivo no figura en la lista (o no se puede derivar) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | un argumento explícito de la CLI afecta a un campo propiedad del perfil | 2 |

Todos ellos se producen mientras se compila la línea de comandos, antes de la planificación. Son `SessionProfileException` (o `ArgumentException` para el conflicto) y nunca inician una sesión. El catálogo completo está en [errores](errors.md); los códigos de salida, en [códigos de salida](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Descubrimiento de los perfiles instalados

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) compila cada paquete bajo `.omsilaunch\session-profiles\` e imprime una entrada por paquete: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` y sus `Presets` (`Index`, `Id`, `Name`) en orden de índice. Un paquete que no compila se enumera igualmente, con `IsValid: false` y el mismo código `OL_E_SESSION_PROFILE_*` que notificaría un lanzamiento. No se escribe nada.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Cómo aparece un perfil en la API

Tras una carga correcta, la especificación incluye un registro `SessionProfileMetadata` en `LaunchSpec.SessionProfile`:

| Campo | Origen |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` del preset seleccionado |
| `PresetIndex` | `index` del preset seleccionado |
| `PresetName` | `name` del preset seleccionado |
| `PackagePath` | directorio absoluto del paquete |

El planificador añade un diagnóstico informativo `session_profile.selected` a cada `SessionPlan` construido a partir de una especificación así, con las claves de datos `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` y `session_profile.path`. No afecta a si el plan es ejecutable. Los integradores que usan directamente la [API pública](public-api.md) pueden llamar a `SessionProfileCompiler.Load` y `SessionProfileCompiler.Apply` desde `OmsiLaunch.Core`; la representación YAML nunca llega a `OmsiLaunch.Api`.

<a id="examples"></a>
## Ejemplos

<a id="example-1-settings-only-profile-one-preset"></a>
### Ejemplo 1: perfil solo con ajustes, un preset

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

Ejecución: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. El mapa y el punto de entrada proceden de la línea de comandos porque el perfil no define ningún bloque `new`; añadir `/set:graphics.maxFPS=60` está permitido, añadir `/set:traffic.humans=10` es un conflicto.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Ejemplo 2: perfil vinculado a un mapa con tres presets y recursos incluidos en el paquete

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, con `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` y `textures\offline.itx` dentro del paquete:

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

Ejecución: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Con `/saved:situations\mytrip.osn` el bloque `new` se omite y el `.osn` debe referenciar `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### El ejemplo incluido en el paquete

La versión incluye `docs/examples/session-profiles/rmg-leste/profile.yaml` ([ver](../../../examples/session-profiles/rmg-leste/profile.yaml)). Es sintácticamente válido, cumple el esquema y se cargaría sin errores. Dos propiedades impiden que inicie una sesión sin cambios en esta build:

1. Establece `new.date`, `new.time` y `new.weather`, que hacen que el plan no sea ejecutable (consulta [El bloque `new`](#new)).
2. Sus valores de ruta son escalares simples con barras invertidas duplicadas (`maps\\RMG Leste\\global.cfg`). YAML las mantiene duplicadas y las identidades de mapa se comparan textualmente (solo tras normalizar `/` a `\`), por lo que `new.map` y `compatibility.maps` no coincidirían con la identidad del catálogo `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` durante la planificación). El valor de `assets` sí se resuelve porque la normalización de rutas de Windows reduce los separadores duplicados.

La forma ejecutable para esta build es:

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
## Páginas relacionadas

- [Referencia de la CLI](cli.md) para `/predefined-profile`, `/predefined-profile-index`, `/set` y los flags de mundo
- [LaunchSpec](launchspec.md) para el registro en el que se compila un perfil
- [Transacciones y recuperación](../concepts/transactions-and-recovery.md) para saber cómo se aplican y restauran los overlays de `settings`, splash y `.itx`
- [Capacidades](capabilities.md) y [estado de la validación en runtime](../status/runtime-validation-status.md)
