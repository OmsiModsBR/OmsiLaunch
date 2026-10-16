# Справочник по LaunchSpec

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> Перевод [исходной страницы на английском языке](../../../reference/launchspec.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Эта страница — нормативный справочник по `LaunchSpec`, записи запроса, которая описывает один сеанс OmsiLaunch. Здесь описаны её форма в C# (`OmsiLaunch.Api`), её форма в виде JSON-файла, который загружает CLI (`/spec:<path>`, `tools/OmsiLaunch.Cli/LaunchSpecJson.cs`), каждое свойство с типом, значением по умолчанию, правилом проверки и текущим действием, правила проверки, делающие план неисполнимым, а также приоритет между флагами CLI, файлами спецификации и профилями сеанса. Документируется только то, что делает текущий код.

Связанные страницы: [публичный API](public-api.md), [справочник по CLI](cli.md), [профили сеанса](session-profiles.md), [коды ошибок](errors.md), [жизненный цикл сеанса](../concepts/session-lifecycle.md), [возможности](capabilities.md).

<a id="where-a-launchspec-comes-from"></a>
## Откуда берётся LaunchSpec

| Источник | Как он превращается в `LaunchSpec` |
| --- | --- |
| API | Интегратор создаёт запись и передаёт её в `PlanSessionAsync`. |
| Флаги CLI | `CliInput.BuildSpecAsync` начинает со встроенных значений по умолчанию (NEW_MAP, ничего не задано, значения `Behavior` по умолчанию) и применяет флаги. |
| JSON-файл `/spec:<path>` | Загружается методом `LaunchSpecJson.LoadAsync`, затем используется как исходная основа, которую переопределяют флаги CLI (см. [приоритет](#precedence-cli-flags-vs-spec-file-vs-session-profile)). |
| Профиль сеанса (`/predefined-profile:<id> /predefined-profile-index:<n>`) | `SessionProfileCompiler.Apply` записывает в основу мир, параметры, оформление, интернет-текстуры и поведение из профиля и сохраняет метаданные `SessionProfile`. |

Приведённый ниже [полный пример](#complete-example) сверен с формой записи. Копия минимального примера поставляется как `examples/release-session.example.json` (и `.omsilaunch\examples\release-session.example.json` в релизном пакете).

<a id="json-form"></a>
## Форма JSON

| Правило | Подробности |
| --- | --- |
| Сериализатор | `System.Text.Json` с `PropertyNameCaseInsensitive = true`, `ReadCommentHandling = Skip`, `AllowTrailingCommas = true`; конвертеры не регистрируются. |
| Имена свойств | Имена свойств C# (`Installation`, `RootPath`, ...). При загрузке сопоставление не зависит от регистра; CLI записывает их в PascalCase. |
| Перечисления | Целые числа (строкового конвертера перечислений нет). `"Mode": 0` допустимо; `"Mode": "NewMap"` отклоняется как некорректный JSON. Значения перечислены в разделе [Перечисления](#enumerations). |
| `OptionalValue<T>` | Объект `{ "Presence": 0 | 1, "Value": <T or null> }`. `Presence` 0 = `Unset` (значение игнорируется), 1 = `Set` (значение должно присутствовать и не быть null; `Set` со значением null не проверяется и ведёт себя как недопустимое значение). Опущенный член `OptionalValue` означает `Unset`. Член только для чтения `IsSet` появляется в выводе, который записывает CLI, и при загрузке принимается и игнорируется. |
| Необязательные записи | `Year`, `Weather`, `Input`, `Diagnostics`, `Presentation`, `InternetTextures`, `SessionProfile`, `HofManagement`, `Modules` могут быть `null` или опущены; методы доступа `Effective*` подставляют значения по умолчанию. |
| Обязательные записи | `Installation`, `World`, `Date`, `Time`, `Environment` (со всеми восемью словарями, используйте `{}`), `Behavior` должны быть заданы как объекты. Они не проверяются: значение `null` или отсутствующая запись приводит к более позднему сбою из-за нулевой ссылки, о котором CLI сообщает как `OL_E_INTERNAL` (код выхода 10) или `OL_E_INVALID_ARGUMENT` (код выхода 2). |
| Неизвестные свойства | Отклоняются до привязки: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` (в пути используются имена членов в том виде, в каком они записаны в файле). Содержимое словарей (`Environment.*`) не проверяется как свойства. |
| Корень | Должен быть JSON-объектом: `OL_E_SPEC_INVALID`. Максимальная глубина вложенности — 32. |
| Размер файла | Не более 1 MiB (1 048 576 байт): `OL_E_SPEC_TOO_LARGE`. Файл отсутствует: `OL_E_SPEC_NOT_FOUND`. |
| Комментарии и завершающие запятые | Комментарии `//` и `/* */`, а также завершающие запятые допускаются. |
| Некорректный JSON | Исключение парсера не преобразуется: CLI сообщает `OL_E_INTERNAL` с кодом выхода 10. |
| Кодировка | UTF-8 (BOM читатель допускает). Обратные косые черты в идентификаторах должны экранироваться (`"maps\\Grundorf\\global.cfg"`); для идентификаторов карт, ситуаций, ТС и HOF допускаются прямые косые черты. |

<a id="complete-example"></a>
## Полный пример

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

Явная дата, если сборка её поддерживает, записывается как `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }`, а время — как `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`. И то и другое применяется только к сеансам NEW_MAP (`EXPERIMENTAL`; см. ниже).

<a id="property-reference"></a>
## Справочник по свойствам

Столбец «Использование» указывает, что текущий код делает со значением. Для стабильности используется терминология со [страницы публичного API](public-api.md#stability-vocabulary).

<a id="launchspec-root"></a>
### `LaunchSpec` (корень)

| Свойство | Тип JSON | Обязательно | Значение по умолчанию, если опущено | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Installation` | объект `InstallationSpec` | да | нет | да | `STABLE_BETA` |
| `World` | объект `WorldSpec` | да | нет | да | `STABLE_BETA` |
| `Date` | объект `DateSpec` | да | нет | да, только NEW_MAP (см. [`DateSpec`](#datespec-timespec-yearspec)) | `EXPERIMENTAL` |
| `Time` | объект `TimeSpec` | да | нет | да, только NEW_MAP | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | нет | `Unset` | разрешается для диагностики; любое заданное поле делает план неисполнимым | `PARTIAL` |
| `Environment` | объект `EnvironmentSpec` | да | нет | да (семантический overlay (временная замена файла на время сеанса) файла `options.cfg`) | `STABLE_BETA` |
| `Behavior` | объект `LaunchBehaviorSpec` | да | нет | частично (см. запись) | `STABLE_BETA` / `PARTIAL` |
| `Year` | объект `YearSpec` или null | нет | `null` → `EffectiveYear` = режим `Unset` | да, только NEW_MAP | `EXPERIMENTAL` |
| `Weather` | объект `WeatherSpec` или null | нет | `null` → `EffectiveWeather` = режим `Unset` | да, в любом режиме мира, применяется сразу после запуска мира (см. [`WeatherSpec`](#weatherspec)) | `EXPERIMENTAL` |
| `Input` | объект `InputSpec` или null | нет | `null` → `EffectiveInput` = оба не заданы | да (см. [`InputSpec`](#inputspec)) | `EXPERIMENTAL` |
| `Diagnostics` | объект `DiagnosticsSpec` или null | нет | `null` → `EffectiveDiagnostics` = значения по умолчанию | только передаётся | `PARTIAL` |
| `Presentation` | объект `SessionPresentationSpec` или null | нет | `null` → `EffectivePresentation` = управляемая заставка, язык не задан, пользовательский каталог не задан, значок в трее показывается | да | `STABLE_BETA` |
| `InternetTextures` | объект `InternetTexturesSpec` или null | нет | `null` → `EffectiveInternetTextures` = `Native` | да | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | объект `SessionProfileMetadata` или null | нет | `null` | только сведения о происхождении (диагностическое сообщение плана `session_profile.selected`) | `STABLE_BETA` |
| `HofManagement` | объект `HofManagementSpec` или null | нет | `null` → `EffectiveHofManagement` = нет операций | да (HOF-overlay сеанса, см. [управление HOF](hof-management.md)) | `STABLE_BETA` |
| `Modules` | объект `RuntimeModulesSpec` (`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`) или null | нет | `null` → `EffectiveModules` = ничего не запрошено, не отключено и не задано | нет (модули никогда не затрагивают файлы установки через OmsiLaunch; см. [runtime-модули](../../../reference/runtime-modules.md)) | `EXPERIMENTAL` |

Методы доступа только для чтения (присутствуют в JSON-выводе CLI, при загрузке игнорируются): `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, `EffectiveHofManagement`, `EffectiveModules`.

### `InstallationSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | строка | обязательно | Каталог, содержащий `Omsi.exe` и `plugins\`. См. [правила путей](#path-rules). Пустое значение или только пробелы → `OL_E_INSTALLATION_NOT_FOUND`. | да | `STABLE_BETA` |
| `ExpectedExecutableSha256` | строка или null | `null` | `null` или 64 шестнадцатеричные цифры (иначе `OL_E_INVALID_ARGUMENT`). | Необязательная привязка: если задано, план неисполним, пока SHA-256 файла `Omsi.exe` не совпадёт с ним (`OL_E_UNSUPPORTED_BUILD`, данные `expected_sha256` и `file_sha256`). | `STABLE_BETA` |

### `WorldSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | обязательно | `0` `NewMap`, `1` `SavedSituation`, `2` `LastMapState` (`LastSituation` — устаревший псевдоним с тем же значением 2). | да; для `LastMapState` нужны `MapIdentity` и `laststn.osn` карты (`OL_E_MAP_NOT_FOUND`, `OL_E_SITUATION_NOT_FOUND`) | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | Для `NewMap`: обязательно, в форме `maps\<dir>\global.cfg` (без учёта регистра, `/` допускается, без `..`) и установлено. Для `SavedSituation` игнорируется (карту задаёт файл `.osn`). | да (передача при запуске) | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | Для `SavedSituation`: обязательно, идентификатор установленной ситуации `situations\...\<file>.osn` (в том виде, в каком его возвращают `DiscoverAsync(Situations)` / `/list:situations`). | да (передача при запуске) | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | Для `NewMap` без `EntrypointIdentity`: обязательно, `>= 0`, индекс в списке точек входа карты, который показывает OMSI. Если не задано, передаётся плагину как `-1`. | да (передача при запуске) | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | Точная (с учётом регистра) метка точки входа или идентификатор из обнаружения контента. Планировщик определяет по нему метку, которую показывает OMSI, и при запуске выбирается эта метка (`world.entrypoint-identity`); запись, которую OMSI не показывает, отклоняется (`OL_E_ENTRYPOINT_NOT_PRESENTED`), как и неизвестная (`OL_E_ENTRYPOINT_NOT_FOUND`). | передаётся | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec` (только для чтения) | вычисляется | `Mode` = `Identity`, если задан `EntrypointIdentity`, иначе `PresentedIndex`, если задан индекс, иначе `Unset`; `PresentedIndex`, `Identity` повторяют входные значения. | производное | `STABLE_BETA` |

### `DateSpec`, `TimeSpec`, `YearSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | обязательно (`Year`: `0`, если запись равна null) | `0` `Unset`, `1` `Explicit`, `2` `System`. | NEW_MAP: `Explicit` использует `Value`, `System` — часы ПК на момент планирования сеанса; `Unset` сохраняет собственное значение OMSI (её параметры и форма Start). Контроллер превращает запрос в стартовые часы OMSI (год, день года, час, минута, секунда; возможности `world.explicit-date`, `world.explicit-time`, `world.explicit-year`), данные передачи при запуске (версия 7) несут их, а плагин записывает их непосредственно перед `NewSituation` OMSI, поэтому OMSI выводит сезон и календарь из запрошенной даты до загрузки карты. После запуска читаются идущие часы; несовпадение или использование OMSI фиксированного времени запуска завершает запуск с `OL_E_DATE_TIME_APPLY_FAILED`. `SavedSituation`: режим, отличный от `Unset`, — `OL_E_CAPABILITY_UNAVAILABLE` (у ситуации своё время). | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: реальная календарная дата (`Day` допустим для месяца и года); `SemanticTime`: `Hour` 0..23, `Minute` 0..59, `Second` 0..59; годы 1900..2200. `YearSpec` задаёт год (и год `Date`, если заданы оба). 31 декабря високосного года отклоняется: календарь OMSI не может представить день 366. Недопустимые значения: `OL_E_DATE_TIME_APPLY_FAILED`. Должно быть задано, если `Mode` равен `Explicit`, и не задано в остальных случаях (`OL_E_INVALID_ARGUMENT`). | да | `EXPERIMENTAL` |

### `WeatherSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | `0`, если запись равна null | `0` `Unset`, `1` `Preset`, `2` `Icao`, `3` `RealCurrent`. | Любой режим, кроме `Unset`, требует `world.weather`; плагин применяет его сразу после запуска мира (в любом режиме мира; он переопределяет погоду сохранённой ситуации). Ошибка завершает запуск с `OL_E_WEATHER_APPLY_FAILED`. | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | Файл из каталога `weather` установки, по имени или `weather\<name>.owt`; проверяется при планировании (`OL_E_WEATHER_INVALID`). | данные передачи | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | Четыре буквы или цифры (иначе `OL_E_WEATHER_INVALID`); используется режимом `Icao`. | данные передачи | `EXPERIMENTAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec` (внутри `PlayerVehicle`)

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | Идентификатор установленного `Vehicles\...\<file>.bus`, иначе `OL_E_VEHICLE_NOT_FOUND`. | разрешается в `ResolvedContent`; затем `player-vehicle.model` как неподдерживаемая возможность → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | Идентификатор окраски (repaint) для `Model` (`<cti>#item:<n>`), иначе `OL_E_REPAINT_NOT_FOUND`; проверяется только если задан `Model`. | так же | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | Установленный `Vehicles\...\<file>.hof`, иначе `OL_E_HOF_NOT_FOUND`. | так же | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | Любая строка. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | Любая строка. | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool (только для чтения) | вычисляется | `true`, если задан `Model`. В данных передачи при запуске как `PlayerVehicleEnabled` передаётся значение `PlayerVehicle.IsSet`. | производное | `PARTIAL` |

`PlayerVehicle` с `Presence` 1 и всеми незаданными полями принимается и ни на что не влияет. Любое заданное поле в этой сборке делает план неисполнимым (`UNAVAILABLE`).

### `EnvironmentSpec`

| Свойство | Тип | По умолчанию | Использование | Стабильность |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | каждое — `IReadOnlyDictionary<string, OptionalValue<string>>`, обязательно (`{}`, если пусто) | нет | да | `STABLE_BETA` |

Восемь групп объединяются; то, в какую группу помещён ключ, ни на что не влияет. Каждая запись с `Presence` 1 — это семантический параметр из `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`); ключ определяет целевой файл (`options.cfg` для всех текущих ключей) и токен. При планировании проверяется, что ключ существует (`OL_E_UNKNOWN_SETTING`) и доступен для записи (`OL_E_SETTING_NOT_WRITABLE`); значение проверяется только при запуске (`OL_E_INVALID_SETTING_VALUE`, сообщается как сеанс в состоянии `Failed` с `OL_E_START_SESSION`). Ключи не зависят от регистра. Незаданные записи игнорируются. Флаг CLI `/set:<key>=<value>` записывает в `General`; параметры `settings` профиля сеанса также объединяются в `General`.

| Ключ | Значение | Примечания |
| --- | --- | --- |
| `general.language` | строка | токен `[language]` |
| `general.radio` | строка | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | токены присутствия (`autoSave` — инверсия `noAutoSave`) |
| `graphics.screenRatio` | строка | |
| `graphics.maxFPS` | целое 10..200 | |
| `graphics.tileDistance` | целое 1..20 | |
| `graphics.maxObjectDistanceMeters` | число 20..5000 | |
| `graphics.minObjectScreenPercent` | число 0..10 | сохраняется делённым на 100 |
| `graphics.minReflectionObjectScreenPercent` | число 0..50 | сохраняется делённым на 100 |
| `graphics.maxObjectComplexity` | целое 0..3 | |
| `graphics.maxMapComplexity` | целое 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | токены присутствия |
| `graphics.stencilShadows` | `true` / `false` | записывается как `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | один блок `smokesystems` |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | токены присутствия |
| `simulation.ticketSelling` | целое 0..2 | |
| `simulation.maintenance` | целое 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | сразу два токена OMSI (`RUNTIME_PROVEN`) |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | токены присутствия |
| `traffic.randomVehicles` | целое 0..1000 | компонент 0 многострочного блока `AIMaxCountRandom` (проверено в runtime, матрица RV-005) |
| `traffic.humans` | целое 0..1000 | компонент 1 блока `AIMaxCountRandom` |
| `traffic.factorPercent` | число 1..300 | |
| `traffic.parkedVehiclesPercent` | число 0..100 | |
| `traffic.scheduledVehicles` | число 0..1000 | |
| `traffic.scheduledLinePriority` | число 1..4 | |
| `traffic.passengerFactorPercent` | число 0..200 | |
| `sound.stereo` | число 0..100 | |
| `sound.maxSimultaneousSounds` | число 5..1000 | |
| `sound.masterVolume` | число 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | отклоняются | известны, но недоступны для записи → `OL_E_SETTING_NOT_WRITABLE` |

Изменяемые файлы сохраняют свою кодировку (байты Windows-1252 сохраняются; UTF-8/UTF-16 с меткой BOM учитываются) и окончания строк.

### `LaunchBehaviorSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | только `true` | Точное восстановление обязательно: `false` отклоняется с `OL_E_INVALID_ARGUMENT`. | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | любые | `true`: файл `closecheck`, существующий до сеанса, безвозвратно удаляется при запуске (диагностическое сообщение `closecheck.stale-removed` с его SHA-256; при сбое — `OL_E_CLOSECHECK_REMOVE_FAILED`). `false`: существующий `closecheck` не трогается и не считается удалением сеанса. Файл `closecheck`, который OMSI записывает во время сеанса, всегда удаляется при восстановлении. | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600 (иначе `ArgumentOutOfRangeException` из `StartSessionAsync`; флаг CLI `/startup-timeout` требует 1..600; профили требуют > 0). Бюджет времени от запуска супервизора до состояния `Running`; по его истечении сеанс завершается сбоем с `OL_E_STARTUP_TIMEOUT` (плагин запущен) или `OL_E_PLUGIN_NOT_LOADED`. | да | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600 (флаг CLI `/shutdown-timeout`) | При `CooperativeShutdown`: время, которое получает OMSI, чтобы выйти самостоятельно, прежде чем её завершат принудительно. Без него OMSI завершается сразу. | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | любое (флаг CLI `/cooperative-shutdown`) | `true`: завершение работающего сеанса просит OMSI выйти через её собственную процедуру выхода (`TForm_main.CloseOMSI`: сохраняются параметры и `laststn.osn` карты, плагины финализируются) вместо принудительного завершения; по истечении `ShutdownTimeoutSeconds` процесс завершается принудительно (`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`). Восстановление выполняется после выхода, поэтому файлы, которыми владеет сеанс, по-прежнему возвращаются к исходным байтам. | `EXPERIMENTAL` |

### `InputSpec`

| Свойство | Тип | По умолчанию | Использование | Стабильность |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | Путь (абсолютный или относительный к установке) к файлу клавиатуры в формате OMSI. Сеанс использует его вместо `Inputs\keyboard.cfg` (overlay транзакции: файл установки в конце побайтово восстанавливается). Файл должен существовать, быть не более 1 MiB и содержать хотя бы одну привязку `[entry]`; иначе `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | Путь к файлу контроллера в формате OMSI, который на время сеанса используется вместо `Inputs\gamectrler.cfg` таким же образом. Он должен содержать хотя бы одно устройство `[ctrl]`; иначе `OL_E_INVALID_ARGUMENT`. | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| Свойство | Тип | По умолчанию | Использование | Стабильность |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | В `src/` не используется. Трассировка хоста `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` записывается всегда. | `PARTIAL` (передаётся, сейчас ни на что не влияет) |
| `Verbose` | bool | `false` | Не используется. | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | Не используется. | `PARTIAL` |
| `ProcessTrace` | bool | `false` | Не используется. | `PARTIAL` |
| `PluginTrace` | bool | `false` | Флаг данных передачи при запуске: плагин записывает события `plugin.trace.operation` (операция, исход, код, длительность в микросекундах) для каждой runtime-операции; они попадают в `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` через кольцо телеметрии. | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | Флаг данных передачи при запуске: нативный слой сообщает каждую установку и снятие хука (`native.trace`: граница, владелец, цель, результат) в ту же трассировку хоста. | `EXPERIMENTAL` |

Флаги CLI `/log`, `/logall`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process`, `/trace-plugin`, `/trace-native` заполняют эти логические значения (`/logall` устанавливает `Verbose`, `ProcessTrace`, `PluginTrace`, `NativeTrace`); они объединяются со значениями спецификации через логическое ИЛИ.

### `SessionPresentationSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1` (`Managed`) | `0` `Unset` (псевдоним `Native`): собственные файлы заставки OMSI не затрагиваются. `1` `Managed`: OmsiLaunch на время сеанса накладывает overlay на `GUI\NewSplashscreen_ENG.bmp` и `GUI\NewSplashscreen_<LANG>.bmp` (после сеанса они точно восстанавливаются). | да | `STABLE_BETA` (матрица RV-006) |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`, `ENG`/`EN`, `DEU`/`DE`, `FRA`/`FR` (без учёта регистра); любое другое значение нормализуется в `ENG`. Если не задано, читается значение `[language]` из `options.cfg` и нормализуется так же. | да (только для управляемой заставки) | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | Каталог, содержащий `ENG.bmp` и `<LANG>.bmp` (640×480, 24-битный BMP). См. [правила путей](#path-rules). Каталог отсутствует: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`; файл отсутствует: `OL_E_SPLASH_ASSET_MISSING`; неверный формат: `OL_E_SPLASH_FORMAT_UNSUPPORTED`. Если не задано, используется `<root>\.omsilaunch\assets\splash` (однократно заполняемый из пакета), иначе — `assets\splash` из пакета. | да (только для управляемой заставки) | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` скрывает отдельный индикатор в трее Windows у владельца CLI. | Только владелец CLI; у API нет трея. Флага CLI нет; значение может прийти только из файла спецификации. | `STABLE_BETA` |

### `InternetTexturesSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Использование | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0` (`Native`) | `0` `Native`: ничего не меняется. `1` `Disabled`: плагин подавляет встроенный в процесс загрузчик OMSI (телеметрия `internet-textures.suppressed` / `internet-textures.suppression.failed`). `2` `Override`: профиль `.itx` накладывается как overlay в виде `Texture\standard.itx`; его целевые файлы и `Texture\standard.ipr` становятся удалениями сеанса. | да | `Native`: `STABLE_BETA`; `Disabled`, `Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | Обязательно для `Override` (`OL_E_ITX_PROFILE_REQUIRED`). Текстовый файл с парами строк: абсолютный URL `http`/`https`, затем целевой путь относительно корневого каталога установки, который содержит компонент `Texture\`, не является корневым, не содержит `..`, не начинается с `\` и не проходит через junction/symlink (`OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). См. [правила путей](#path-rules). | да | `EXPERIMENTAL` |

### `HofManagementSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Используется | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Operations` | массив `HofOperationSpec` | нет | Применяются по порядку; о разрешении, правилах конфликтов и ограничениях см. [управление HOF](hof-management.md). | да | `STABLE_BETA` |

Члены `HofOperationSpec`: `SourceKind` (`HofSourceKind`, int), `Source` (строка), `TargetVehicles` (массив идентичностей ТС или каталогов ниже `Vehicles\`, не менее одного), `Mode` (`HofApplyMode`, int, по умолчанию `0` `Add`), `Files` (массив имён файлов `.hof` или null для всех). Проблемы — это диагностические сообщения плана `OL_E_HOF_*`.

### `RuntimeModulesSpec`

| Свойство | Тип | По умолчанию | Допустимые значения | Используется | Стабильность |
| --- | --- | --- | --- | --- | --- |
| `Enable` | массив идентификаторов модулей | нет | Идентификаторы модулей, установленных ниже `plugins\OmsiLaunch.Modules`; см. [runtime-модули](../../../reference/runtime-modules.md). Запрошенный модуль, который не удаётся загрузить, — диагностическое сообщение плана `OL_E_MODULE_*` (план неисполним). | да | `EXPERIMENTAL` |
| `Disable` | массив идентификаторов модулей или null | нет | Не допускает в сеанс модуль по умолчанию, модуль из содержимого или из файла параметров (диагностическое сообщение плана `module.disabled`). Модуль, одновременно включённый и отключённый, — `OL_E_MODULE_SETTING_INVALID`. Метод доступа только для чтения `EffectiveDisable`. | да | `EXPERIMENTAL` |
| `Settings` | объект со строками или null | нет | `"<module id>.<key>": "<value>"`. Имеет приоритет над файлом параметров `.omsilaunch\modules.ini` и значением по умолчанию из манифеста. Неверное значение или ключ, которого модуль не объявляет, — `OL_E_MODULE_SETTING_INVALID` (план неисполним); параметры модуля, который сеанс не загружает, — `OL_W_MODULE_SETTING_IGNORED`. Эффективные значения находятся в `PlannedModule.Settings`. Метод доступа только для чтения `EffectiveSettings`. | да | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| Свойство | Тип | Использование | Стабильность |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | строки / int | Записываются в диагностическое сообщение плана `session_profile.selected` (`Data["session_profile.*"]`). Больше нигде не используются; обычно заполняются компилятором профилей сеанса, а не вручную. | `STABLE_BETA` |

<a id="enumerations"></a>
## Перечисления

| Перечисление | Значения (целое число JSON) |
| --- | --- |
| `WorldMode` | `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (устаревший псевдоним; это собственная ветвь OMSI для состояния последней карты, а никогда не самый новый файл `.osn`) |
| `DateTimeMode` | `Unset` = 0, `Explicit` = 1, `System` = 2 |
| `WeatherMode` | `Unset` = 0, `Preset` = 1, `Icao` = 2, `RealCurrent` = 3 |
| `HofSourceKind` | `Collection` = 0, `Directory` = 1, `Vehicle` = 2, `File` = 3 |
| `HofApplyMode` | `Add` = 0, `Mirror` = 1 |
| `SplashMode` | `Unset` = 0, `Native` = 0 (псевдоним), `Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0, `Disabled` = 1, `Override` = 2 |
| `Presence` | `Unset` = 0, `Set` = 1 |
| `EntrypointMode` (только для чтения, `Entrypoint.Mode`) | `Unset` = 0, `PresentedIndex` = 1, `Identity` = 2 |

Целые числа вне объявленного диапазона сохраняются сериализатором как есть и ведут себя как неизвестные значения (например, неизвестный `WorldMode` не является ни NEW_MAP, ни SAVED_SITUATION и даёт план без возможности мира; плагин бы его отклонил, но CLI в любом случае заменяет режим, см. раздел о приоритете).

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## Правила проверки и диагностика неисполнимого плана

`PlanSessionAsync` выполняет `LaunchValidation.Validate`, а затем `SessionPlanner.PlanAsync`. План исполним ровно тогда, когда ни один код диагностики не начинается с `OL_E_`. Полный набор:

| Диагностика | Условие | Источник |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` пустое или состоит только из пробелов | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` без заданного значения или с месяцем/днём вне диапазона; `Time.Mode` = `Explicit` без заданного значения или с часом/минутой/секундой вне диапазона | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | `Date.Value` или `Time.Value` задано, хотя режим не `Explicit` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` с незаданным `MapIdentity` или не в форме `maps\...\global.cfg` (проверка); `NewMap` с идентификатором, который не установлен (планировщик) | оба |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` без `EntrypointIdentity` и с незаданным или отрицательным `PresentedEntrypointIndex` | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`, карта установлена, `EntrypointIdentity` нет, `PresentedEntrypointIndex` не задан (`world.presented-entrypoint` недоступно) | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` без `SituationIdentity` (проверка) или с идентификатором, который не установлен (планировщик) | оба |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: карта, указанная внутри `.osn`, не установлена | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | не Windows 10+ на x64 ОС с x64-процессом хоста (`runtime.current-windows-x64`) | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | корневой каталог отсутствует, установлен атрибут «только для чтения» или нет подкаталога `plugins\` (`transaction.exact-restore`) | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | Не установка OMSI 2: `Omsi.exe` отсутствует, не является 32-разрядным образом PE x86, нет версии OMSI 2 (2.x) или нет каталога `maps\` / `Vehicles\` (`omsi.omsi2`). Хеш критерием не является. | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | задан `EntrypointIdentity`; режим `Date`/`Time`/`Year` не `Unset` при `SavedSituation`; режим `Weather` не `Unset`; задано любое поле `PlayerVehicle` | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` не установлен (в дополнение к `OL_E_CAPABILITY_UNAVAILABLE`) | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | ключ `Environment`, которого нет в каталоге / который недоступен для записи | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | при построении плана заставки/ITX возникло исключение; сообщение содержит `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` или `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | не удаётся загрузить ссылку на замыкание плагина (набор его файлов) (`OmsiLaunchRuntimePaths`) или релизный манифест (сообщение может содержать `OL_E_RELEASE_MANIFEST_INVALID`) | `OmsiLaunchService.PlanSessionAsync` |

Не проверяется на этапе планирования (приводит к сбою при запуске: сеанс в состоянии `Failed` с `OL_E_START_SESSION`): значения параметров (`OL_E_INVALID_SETTING_VALUE`), целостность постоянного плагина (`OL_E_PERMANENT_PLUGIN_*`), доступность аренды установки (`OL_E_INSTALLATION_BUSY`), диапазон `StartupTimeoutSeconds` (исключение из `StartSessionAsync`).

Информационные диагностические сообщения плана: `plugin.integrity.reference` (сообщение `manifest` или `self`), `session_profile.selected`.

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## Приоритет: флаги CLI, файл спецификации и профиль сеанса

`CliInput.BuildSpecAsync` (`tools/OmsiLaunch.Cli/Program.cs`) строит итоговую спецификацию в следующем порядке:

1. Основа = встроенные значения по умолчанию или файл `/spec`, если он указан.
2. Корневой каталог установки = явный аргумент установки, если он указан, иначе `RootPath` из основы; затем `.`/пустое значение → каталог исполняемого файла, `Path.GetFullPath`. Явный аргумент установки всегда имеет приоритет над `RootPath` из спецификации.
3. Профиль сеанса (`/predefined-profile` + `/predefined-profile-index`): явные аргументы CLI, затрагивающие поле, которым владеет профиль, отклоняются с `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (поля мира — только в режиме NEW_MAP; ключи `/set`, присутствующие в пресете; флаги заставки, если в пресете есть `presentation`; флаги интернет-текстур, если в нём есть `internet-textures`; тайм-ауты, если в нём есть `behavior`). `World` из основы заменяется новым (сохраняется только режим мира из CLI), затем применяются блок `new:` профиля (только NEW_MAP), `settings` (в `General`), `presentation`, `internet-textures`, `behavior` и метаданные `SessionProfile`. `compatibility.maps` применяется для NEW_MAP и SAVED_SITUATION.
4. Мир: режим мира из CLI всегда имеет приоритет (`/new` по умолчанию, `/saved:<osn>`, `/last`); `World.Mode` из файла спецификации заменяется. Чтобы запустить сохранённую ситуацию из спецификации, передайте `/saved:`. `/map` и `/entrypoint`/`/entrypoint-index` переопределяют основу; идентификатор `/entrypoint` из CLI сбрасывает индекс; `/saved` вместе с `/map` или флагами точки входа даёт `OL_E_INVALID_ARGUMENT`.
5. `/date`, `/time`, `/year`, `/weather*` переопределяют основу, если указаны (`system` выбирает `DateTimeMode.System`).
6. `/no-vehicle` сбрасывает `PlayerVehicle`; отдельные флаги `/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration` переопределяют отдельные поля ТС игрока из основы.
7. Записи `/set:<key>=<value>` добавляются в `Environment.General` (ключ проверяется, значение — нет); остальные семь групп берутся из основы без изменений.
8. `/startup-timeout` и `/shutdown-timeout` переопределяют основу, только если указаны; иначе действуют значение из спецификации, затем из профиля, затем значения по умолчанию 180 s / 30 s. `ShutdownTimeoutSeconds` действует вместе с `/cooperative-shutdown` (который объединяется через OR со значением из спецификации).
9. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` переопределяют основу, если указаны; `SuppressTrayIcon` берётся только из основы.
10. Флаги диагностики объединяются с основой через логическое ИЛИ.

Итог: явный флаг CLI > профиль сеанса > файл спецификации > встроенное значение по умолчанию, за исключением того, что флаг CLI, конфликтующий с полем, которым владеет профиль, является ошибкой, а не переопределением.

<a id="path-rules"></a>
## Правила путей

| Путь | Поведение в API | Поведение в CLI |
| --- | --- | --- |
| `Installation.RootPath` | Используется как есть: в файловых операциях относительные пути разрешаются относительно рабочего каталога процесса. Передавайте абсолютный путь. Аренда установки, журнал и каталог контента нормализуют его с помощью `Path.GetFullPath`. | `.` или пустое значение = каталог, содержащий `OmsiLaunch.exe`, и никогда не рабочий каталог вызывающей стороны; явный аргумент установки имеет приоритет над спецификацией; результат преобразуется в абсолютный путь. |
| `Presentation.CustomAssetDirectory` | Абсолютный или относительно `Installation.RootPath`. Должен существовать. | Так же (`/splash-assets`). Путь `assets` профиля сеанса ограничен пакетом профиля и сохраняется как абсолютный. |
| `InternetTextures.OverrideProfilePath` | Разрешается с помощью `Path.GetFullPath`, т. е. относительно рабочего каталога процесса, а не корневого каталога установки. Должен существовать. | Так же (`/internet-textures-profile`). Путь `profile` профиля сеанса ограничен пакетом и сохраняется как абсолютный. |
| Целевые строки ITX | Относительно корневого каталога установки; должны содержать компонент `Texture\`; без корня, без `..`, без начального `\`, без компонентов junction/symlink. | Так же. |
| Идентификаторы контента (`MapIdentity`, `SituationIdentity`, `PlayerVehicle.*`) | Относительно установки, без учёта регистра, `/` допускается; никогда не абсолютные. | Так же. |

<a id="carried-but-not-applied"></a>
## Передаётся, но не применяется

| Поле | Текущее действие | Стабильность |
| --- | --- | --- |
| `Diagnostics.Log`, `Verbose`, `OmsiLogAll`, `ProcessTrace` | нет (трассировка хоста записывается всегда) | `PARTIAL` |
| `PlayerVehicle.*` (любое заданное поле) | контент разрешается для диагностики, план неисполним (`UNAVAILABLE`) | `PARTIAL` |
| `SessionProfile` | только диагностика происхождения | `STABLE_BETA` |
