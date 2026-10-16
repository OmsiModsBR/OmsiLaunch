# Профили сеанса

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Перевод [исходной страницы на английском языке](../../../reference/session-profiles.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Профиль сеанса — это декларативный YAML-пакет, который автор контента поставляет вместе с картой или дополнением, чтобы конечные пользователи могли одной командой запустить воспроизводимый сеанс OmsiLaunch (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Эта страница — нормативный справочник по формату `omsilaunch.session-profile/v1` в том виде, в каком его реализует `SessionProfileCompiler` в `src/OmsiLaunch.Core/SessionProfiles.cs`, по правилам приоритета, которые применяет CLI (`CliInput.BuildSpecAsync` и `RejectProfileConflicts` в `tools/OmsiLaunch.Cli/Program.cs`), а также по каталогу параметров, которые может записывать профиль (`ConfigurationCatalog`). Всё, что умеет профиль, умеют и флаги CLI, и [LaunchSpec](launchspec.md); профиль лишь упаковывает этот выбор.

Стабильность: `STABLE_BETA` для разбора, проверки, обнаружения конфликтов и блоков `settings` / `presentation` / `internet-textures` / `behavior` (офлайн-тест `session-profiles.strict-compiler`; путь overlay (временная замена файла на время сеанса) и восстановления проверен в runtime в рамках RV-005 и RV-006, см. [статус проверки в runtime](../status/runtime-validation-status.md)). Ключи `new.date`, `new.time`, `new.year` и `new.weather` в этой сборке имеют статус `UNAVAILABLE` (см. [Блок `new`](#new)).

<a id="package-location-and-naming"></a>
## Расположение и именование пакета

| Элемент | Правило |
| --- | --- |
| Каталог пакета | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Файл профиля | `<package>\profile.yaml` (точное имя, один файл) |
| Ресурсы | Любые файлы или каталоги внутри каталога пакета, на которые ссылаются относительными путями `presentation.splash.assets` и `internet-textures.profile` |
| `id` | Должен быть простым именем каталога: он не должен быть пустым или состоять из пробелов, не должен содержать `\`, `/` или `:` и не должен содержать последовательность `..`. Нарушения дают `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Значение `id`, объявленное внутри `profile.yaml`, должно побайтно совпадать с именем каталога (с учётом регистра); иначе `OL_E_SESSION_PROFILE_INVALID`. |
| Выбор | `/predefined-profile:<id>` вместе с `/predefined-profile-index:<n>`. Индекс обязателен: `/predefined-profile` без `/predefined-profile-index` завершается ошибкой `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Пакет отсутствует | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Структура релиза | Релизный пакет содержит пример в `.omsilaunch\examples\session-profiles\rmg-leste\` (см. [упаковку](packaging.md)). Примеры не являются профилями: чтобы пакет можно было выбрать, скопируйте его в `.omsilaunch\session-profiles\<id>\`. |

Профиль устанавливает и удаляет пользователь или автор контента. OmsiLaunch никогда не пишет в пакет, никогда его не копирует и никогда не удаляет. Каталог пакета не входит ни в одну транзакцию.

<a id="parsing-rules"></a>
## Правила разбора

| Правило | Поведение | Ошибка |
| --- | --- | --- |
| Ограничение размера | `profile.yaml` не должен превышать 256 KiB (262 144 байт) | `OL_E_SESSION_PROFILE_INVALID` |
| Структура документа | Ровно один YAML-документ, корневой узел которого — отображение (mapping) | `OL_E_SESSION_PROFILE_INVALID` |
| Схема | `schema` должно быть ровно `omsilaunch.session-profile/v1` (с учётом регистра) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Якоря и псевдонимы | Любой узел с YAML-якорем (`&name`) в любом месте документа отклоняется до проверки; поэтому псевдонимы (`*name`) встретиться не могут | `OL_E_SESSION_PROFILE_INVALID` («YAML anchors are not supported.») |
| Неизвестные ключи | Каждое отображение закрыто: ключ, не указанный для своего контекста в таблицах ниже, отклоняется («Unknown property in `<context>`: `<key>`»). Ключи сопоставляются с учётом регистра (`Schema:` — неизвестный ключ). Единственное открытое отображение — `settings`, ключи которого вместо этого проверяются по каталогу параметров. | `OL_E_SESSION_PROFILE_INVALID` |
| Скалярные значения | Каждое конечное значение должно быть скаляром; последовательности и отображения там, где ожидается скаляр, отклоняются («`<field>` must be a scalar.») | `OL_E_SESSION_PROFILE_INVALID` |
| Числа | Целые числа разбираются с инвариантной культурой (`1`, `30`); в дробных числах в `settings` разделителем служит `.` | `OL_E_SESSION_PROFILE_INVALID` |
| Даты и время | `new.date.value` разбирается методом `DateOnly.Parse`, а `new.time.value` — методом `TimeOnly.Parse`, оба с инвариантной культурой; используйте формы ISO `yyyy-MM-dd` и `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| Синтаксические ошибки YAML | Сообщаются с текстом сообщения парсера | `OL_E_SESSION_PROFILE_INVALID` («Invalid YAML: ...») |
| Исполняемое содержимое | YAML разбирается с помощью `YamlDotNet` только в дерево представления; теги, пользовательские типы и выполнение кода не поддерживаются |

Обратные косые черты в простых (не заключённых в кавычки) скалярах являются обычными символами. Пути Windows записываются с одинарной обратной косой чертой (`maps\Grundorf\global.cfg`). Удвоенная обратная косая черта в простом скаляре остаётся в значении удвоенной; см. [Пример из релизного пакета](#the-packaged-example).

<a id="key-reference"></a>
## Справочник по ключам

Контексты названы точно так же, как их называет компилятор. Каждый перечисленный здесь ключ принимается; ничего другого не принимается.

<a id="profile-root-mapping"></a>
### `profile` (корневое отображение)

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `schema` | строка | да | Литерал `omsilaunch.session-profile/v1`. |
| `id` | строка | да | Идентификатор пакета; должен совпадать с именем каталога. |
| `name` | строка | да | Отображаемое имя; передаётся в `SessionProfileMetadata.Name`. |
| `author` | строка | да | Автор; передаётся в `SessionProfileMetadata.Author`. |
| `version` | строка | да | Строка версии пакета (в свободной форме, заключайте в кавычки: `"1.0"`); передаётся в `SessionProfileMetadata.Version`. |
| `compatibility` | отображение | нет | См. `compatibility`. |
| `new` | отображение | нет | Значения по умолчанию для NEW_MAP. См. `new`. |
| `presets` | последовательность отображений | да | От 1 до 99 записей пресетов. Ноль, более 99 записей или значение, не являющееся последовательностью, дают `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `maps` | последовательность строк | нет | Идентификаторы карт (`maps\<Map>\global.cfg`), для которых действителен этот профиль. `/` нормализуется в `\`; сравнение без учёта регистра. Отсутствующий или пустой список означает «любая карта». Непустой список применяется для `WorldMode.NewMap` (к итоговому `new.map` или `/map`) и для `WorldMode.SavedSituation` (к карте, на которую ссылается выбранный `.osn`, разрешаемой через каталог контента). Для `WorldMode.LastMapState` карту определить невозможно, поэтому непустой список всегда приводит к ошибке. Ошибка: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

Блок читается и проверяется всякий раз, когда он присутствует, но применяется к спецификации только тогда, когда выбранный режим мира — NEW_MAP (`/new`, значение CLI по умолчанию). При `/saved:<file.osn>` блок игнорируется.

| Ключ | Тип | Обязательно | Применяется | Описание |
| --- | --- | --- | --- | --- |
| `map` | строка | нет | да | Идентификатор карты в нормализованной форме `maps\<Map>\global.cfg` (планирование требует именно такой формы: начинается с `maps\`, заканчивается на `\global.cfg`, без `..`). Устанавливает `WorldSpec.MapIdentity`. |
| `entrypoint-index` | целое | нет | да | Индекс точки входа из показываемого списка (позиция с отсчётом от 0 в списке точек входа OMSI). Устанавливает `PresentedEntrypointIndex` и сбрасывает идентификатор точки входа, если он был. |
| `entrypoint` | строка | нет | да | Необработанный идентификатор точки входа. Устанавливает `EntrypointIdentity` и сбрасывает индекс из показываемого списка. Если присутствуют и `entrypoint-index`, и `entrypoint`, побеждает `entrypoint`, потому что он применяется последним. Значение — точная метка (с учётом регистра) или идентификатор из обнаружения контента; OMSI показывает только первую запись для каждой метки (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | отображение | нет | нет (`UNAVAILABLE`) | См. `new.date`. |
| `time` | отображение | нет | нет (`UNAVAILABLE`) | См. `new.time`. |
| `year` | целое | нет | нет (`UNAVAILABLE`) | Явно заданный год. |
| `weather` | отображение | нет | нет (`UNAVAILABLE`) | См. `new.weather`. |

`date`, `time`, `year` и `weather` компилируются в `DateSpec`, `TimeSpec`, `YearSpec` и `WeatherSpec` с `DateTimeMode.Explicit` / выбранным `WeatherMode`. Затем планировщик сеанса (`src/OmsiLaunch.Core/SessionPlanner.cs`) требует `world.explicit-date`, `world.explicit-time`, `world.explicit-year` и `world.weather`; все четыре имеют статус `EXPERIMENTAL` (закрыты статически, проверка в runtime ожидается). Дата и время запуска применяются только для NEW_MAP; погода применяется в любом режиме мира.

#### `new.date`

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `mode` | строка | да | Должно быть `explicit` (без учёта регистра). Любое другое значение даёт `OL_E_SESSION_PROFILE_INVALID` («date must use explicit mode.»). |
| `value` | строка | да | `yyyy-MM-dd`. |

#### `new.time`

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `mode` | строка | да | Должно быть `explicit`. |
| `value` | строка | да | `HH:mm` или `HH:mm:ss`. |

#### `new.weather`

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `mode` | строка | да | `preset`, `icao` или `real` (без учёта регистра). Всё остальное: `OL_E_SESSION_PROFILE_INVALID` («Unsupported weather mode»). |
| `preset` | строка | при `mode: preset` | Имя пресета погоды. |
| `icao` | строка | при `mode: icao` | Код станции ICAO. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (каждая запись `presets`)

| Ключ | Тип | Обязательно | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `index` | целое | да | | От 1 до 99, уникален в пределах профиля; индексы не обязаны идти подряд. Выбирается с помощью `/predefined-profile-index`. Дубликат или значение вне диапазона: `OL_E_SESSION_PROFILE_INVALID`; индекс, которого нет нигде в профиле: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | строка | да | | Идентификатор пресета; передаётся как `SessionProfileMetadata.PresetId`. |
| `name` | строка | да | | Отображаемое имя пресета; передаётся как `SessionProfileMetadata.PresetName`. |
| `settings` | отображение | нет | нет | Семантические параметры `options.cfg`, см. [Параметры](#settings). Ключи сопоставляются с каталогом без учёта регистра. |
| `presentation` | отображение | нет | наследуется | Оформление заставки, см. `presentation`. Если блок отсутствует, пресет наследует базовое значение (значение из `/spec` или значение CLI по умолчанию, `Managed`). |
| `internet-textures` | отображение | нет | наследуется | См. `internet-textures`. |
| `behavior` | отображение | нет | наследуется | Тайм-ауты, см. `behavior`. |
| `hofs` | последовательность отображений | нет | наследуется | HOF-операции сеанса, см. `hofs`. Если присутствуют, заменяют базовый `HofManagement`. |
| `modules` | отображение | нет | наследуется | Runtime-модули, см. `modules`. Объединяются с тем, что запрашивает файл `/spec`. |

Применяется только выбранный пресет. Тем не менее разбираются и проверяются все пресеты, поэтому ошибка в пресете 3 приводит к отказу запроса на пресет 1.

### `presentation`

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `splash` | отображение | да | Обязательно, если присутствует `presentation` («Presentation requires splash.»). См. `presentation.splash`. |

#### `presentation.splash`

| Ключ | Тип | Обязательно | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `mode` | строка | да | | `managed` устанавливает на время сеанса растровые изображения заставки OmsiLaunch (`SplashMode.Managed`). `unset` или `native` сохраняет собственные файлы заставки OMSI (`SplashMode.Unset`; `Native` — псевдоним). Без учёта регистра. Всё остальное: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | строка | нет | `ENG` | Язык второй целевой заставки: `PTB`, `ENG`, `DEU`, `FRA` (псевдонимы `PT-BR`, `EN`, `DE`, `FR`; любое неизвестное значение при построении сеанса разрешается в `ENG`). При `mode: managed` сеанс накладывает overlay на `GUI\NewSplashscreen_ENG.bmp` и `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | строка | нет | ресурсы из пакета | Каталог **относительно пакета**, содержащий `ENG.bmp` и, для языка `language`, отличного от английского, `<language>.bmp`; каждый файл должен быть 24-битным BMP размером 640x480. Каталог должен существовать при загрузке профиля (`OL_E_SESSION_PROFILE_ASSET_MISSING`); файлы проверяются при запуске сеанса (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Действуют правила ограничения путей. Если опущено, используется `.omsilaunch\assets\splash` установки (или значения по умолчанию из пакета). |

Профиль не может задать `SessionPresentationSpec.SuppressTrayIcon`; значение остаётся `false`, если его не задаёт `/spec`.

### `internet-textures`

| Ключ | Тип | Обязательно | Описание |
| --- | --- | --- | --- |
| `mode` | строка | да | `native` (`InternetTexturesMode.Native`, OMSI работает как обычно), `disabled` (`Disabled`, профилированный встроенный в процесс загрузчик подавляется на время сеанса), `override` (`Override`, профиль `.itx` для сеанса устанавливается как `Texture\standard.itx`). Без учёта регистра; всё остальное: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | строка | обязательно для `override` | Путь к файлу `.itx` **относительно пакета**. Ключ отсутствует при `override`: `OL_E_SESSION_PROFILE_INVALID`; файл отсутствует: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Действуют правила ограничения путей. Файл должен состоять из пар строк `URL` / `target` с URL `http://` или `https://` (иначе `OL_E_ITX_PROFILE_INVALID`), и каждая цель должна разрешаться внутри каталога `Texture\` установки без прохождения через точку повторной обработки (reparse point) (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Перечисленные цели и `Texture\standard.ipr` становятся удалениями сеанса (см. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Ключ | Тип | Обязательно | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `startup-timeout` | целое (секунды) | нет | 180 | Время, отведённое от запуска процесса до состояния `Running`. При загрузке профиля должно быть положительным; при запуске сеанс дополнительно требует от 1 до 600 (иначе `OL_E_START_SESSION`). Соответствует `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | целое (секунды) | нет | 30 | Соответствует `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT (принимается для совместимости, сейчас ни на что не влияет): супервизор напрямую завершает OMSI и никогда не читает это значение. |

Если блок `behavior` присутствует, задаются оба тайм-аута (указанное значение или значение по умолчанию), и они полностью заменяют базовый `LaunchBehaviorSpec`, включая `RestoreConfiguration` и `SuppressStaleClosecheckWarning`, которые возвращаются к значениям по умолчанию (`true`, `true`).

### `hofs`

Каждая запись — одна операция [управления HOF](hof-management.md) и называет ровно один ключ источника (`package`, `collection`, `directory`, `vehicle` или `file`); ноль или несколько: `OL_E_SESSION_PROFILE_INVALID`. Существование источников и целей проверяется при планировании сеанса (`OL_E_HOF_*`), кроме `package`.

| Ключ | Тип | Обязателен | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `package` | строка | один ключ источника | | Каталог **относительно пакета** с файлами `.hof`, которые поставляет профиль. Должен существовать при загрузке профиля (`OL_E_SESSION_PROFILE_ASSET_MISSING`); действуют правила ограничения путей. Становится источником `Directory`. |
| `collection` | строка | один ключ источника | | Имя коллекции в `.omsilaunch\hof\`. |
| `directory` | строка | один ключ источника | | Каталог относительно установки, например рядом с содержимым карты. |
| `vehicle` | строка | один ключ источника | | Идентичность ТС или каталог, HOF которого копируются. |
| `file` | строка | один ключ источника | | Файл `.hof` относительно установки. |
| `targets` | последовательность строк | да | | Целевые ТС (идентичности или каталоги ниже `Vehicles\`). Отсутствует или пуст: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | последовательность строк | нет | все | Имена файлов, которые берутся из источника. |
| `mode` | строка | нет | `add` | `add` или `mirror` (без учёта регистра); любое другое значение: `OL_E_SESSION_PROFILE_INVALID`. |

Пресет с `hofs` в сочетании с флагом `/hof-source`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

По одной записи на идентификатор [runtime-модуля](../../../reference/runtime-modules.md) (`EXPERIMENTAL`). Каждая запись — отображение со следующими ключами (контекст `module`):

| Ключ | Тип | Обязателен | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `enabled` | `true` или `false` | нет | не задано | `true` запрашивает модуль (`LaunchSpec.Modules.Enable`); `false` не допускает его в сеанс, включая модуль, загружаемый по умолчанию, по его карте или по файлу параметров (`LaunchSpec.Modules.Disable`). |
| `settings` | отображение | нет | нет | Параметры модуля по ключам (`LaunchSpec.Modules.Settings` как `<id>.<key>`). Значения проверяются по объявленным параметрам модуля при планировании сеанса (`OL_E_MODULE_SETTING_INVALID`). |

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

Идентификатор модуля, не соответствующий `[a-z0-9][a-z0-9-]{1,47}`, значение `enabled`, отличное от `true` или `false`, или ключ параметра, не соответствующий `[a-z][a-z0-9_]{0,31}`: `OL_E_SESSION_PROFILE_INVALID`. Не установленный модуль сообщается при планировании сеанса (`OL_E_MODULE_NOT_FOUND` для `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` только для параметров).

<a id="settings"></a>
## Параметры

Ключи `settings` — это семантические имена из `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). Компилятор принимает ключ, только если он существует (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) и доступен для записи (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Значения хранятся как строки и преобразуются в изменения `options.cfg`, когда сеанс строит свои overlay; поэтому недопустимое значение обнаруживается в `StartSessionAsync`, а не при загрузке профиля, и приводит к сбою сеанса с `OL_E_START_SESSION`, сообщение которого содержит `OL_E_INVALID_SETTING_VALUE: <key>`. Каждый параметр ниже записывает в `options.cfg`; все они действуют только в пределах сеанса и после сеанса точно восстанавливаются.

Формы значений:

- **bool** — `true` или `false` (без учёта регистра). Для токенов присутствия токен добавляется или удаляется; для инвертированных токенов (`no_*`) `true` удаляет отрицательный токен.
- **int** / **decimal** проверяются по диапазону; значения с делителем сохраняются делёнными (например, `graphics.minObjectScreenPercent: 5` записывает `0.05`).
- **string** записывается дословно.

| Ключ параметра | Токен `options.cfg` | Тип | Диапазон / значения | Подтверждение |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | любое | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | любое | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (присутствие) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (присутствие) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (присутствие) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (присутствие) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (присутствие) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (присутствие) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | любое | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, сохраняется /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, сохраняется /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (присутствие) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (присутствие) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, записывается как `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` или `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (блок из 4 строк) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (присутствие) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (инвертированное присутствие) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (оба токена присутствия) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (присутствие) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (присутствие) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (присутствие) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (присутствие) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | компонент 0 `AIMaxCountRandom` | int | 0..1000 | STATICALLY_VALIDATED (RV-005 в runtime) |
| `traffic.humans` | компонент 1 `AIMaxCountRandom` | int | 0..1000 | STATICALLY_VALIDATED (RV-005 в runtime) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Записи каталога, которые существуют, но **недоступны для записи** (отклоняются с `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (заменены на `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Ограничение путей

`presentation.splash.assets` и `internet-textures.profile` разрешаются с помощью `Confined(package root, value)`:

1. Корневые пути (`C:\...`), пути, начинающиеся с `\`, и любые пути с компонентом, равным `..`, отклоняются.
2. Вычисляется полный путь, и он должен начинаться с каталога пакета.
3. Каждый существующий компонент ниже корня пакета вплоть до конечного пути включительно проверяется на наличие атрибута `ReparsePoint`. Junction, символическая ссылка на каталог или символическая ссылка на файл в любом месте этого пути отклоняются, как и компонент, который невозможно проверить (`IOException` / `UnauthorizedAccessException`).

Все три вида ошибок дают `OL_E_SESSION_PROFILE_PATH_ESCAPE`. То же правило для точек повторной обработки применяется к целям `.itx` внутри `Texture\` при построении сеанса.

<a id="precedence-and-override-conflicts"></a>
## Приоритет и конфликты переопределения

`CliInput.BuildSpecAsync` составляет спецификацию в следующем порядке:

1. **Значения по умолчанию** (NEW_MAP, ничего не задано, тайм-ауты 180 s / 30 s).
2. **`/spec:<file.json>`**, если указан, полностью заменяет значения по умолчанию.
3. **Корневой каталог установки**: явный аргумент установки имеет приоритет над `RootPath` из спецификации; `.` означает каталог, содержащий исполняемый файл.
4. **Профиль** (`/predefined-profile` + `/predefined-profile-index`): пакет загружается, и `RejectProfileConflicts` проверяет необработанные аргументы CLI **до** какого-либо объединения. Затем блок мира в основе сбрасывается в пустой `WorldSpec` выбранного режима (мир из `/spec` отбрасывается, если используется профиль), и `SessionProfileCompiler.Apply` накладывает профиль на основу: `new` (только NEW_MAP), `settings` (объединяются поверх `Environment.General` основы, по каждому ключу побеждает профиль), а также `presentation`, `internet-textures`, `behavior` (каждый заменяет блок основы, только если его определяет пресет).
5. **Остальные аргументы CLI** накладываются сверху: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, флаги погоды, флаги ТС, `/set`, флаги заставки, флаги интернет-текстур, `/startup-timeout`, `/shutdown-timeout`. Тайм-ауты из CLI применяются, только если указаны; иначе остаётся значение из спецификации/профиля/по умолчанию.
6. **Проверка совместимости** для режимов, отличных от NEW_MAP (`ValidateCompatibility`).

Аргумент CLI, направленный на поле, которым владеет выбранный профиль, является конфликтом и отклоняется с `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (код выхода 2, категория `invalid_argument`). Проверка выполняется по полю, а не по значению: повтор собственного значения профиля тоже является конфликтом.

| Аргумент CLI | Конфликтует, если профиль определяет | Только в режиме |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` или `/entrypoint-index` | `new.entrypoint` или `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | тот же `<key>` в `settings` пресета (без учёта регистра) | любом |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (любой) | любом |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (любой) | любом |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (любой) | любом |
| `/module:<id>` | `modules.<id>.enabled: false` | любом |
| `/no-module:<id>` | `modules.<id>.enabled: true` | любом |
| `/module-setting:<id>.<key>=...` или флаг модуля | `modules.<id>.settings.<key>` | любом |

Не являются конфликтами: ключи `/set`, которые пресет не определяет (они добавляются), флаги ТС (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; профиль не может определить ТС игрока) и любой аргумент мира при `/saved` (блок `new` там не применяется). `/map`, `/entrypoint` и `/entrypoint-index` недопустимы вместе с `/saved` независимо от профилей (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Коды ошибок

| Код | Когда возникает | Код выхода CLI |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` не существует | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` не является простым именем каталога; `assets` / `profile` выходит за пределы пакета или проходит через точку повторной обработки | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` не равно `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | ограничение размера, структура документа, якоря, неизвестный ключ, отсутствующий обязательный ключ, нескалярное значение, неверное число/дата/время, несовпадение `id`, правила количества/индексов пресетов, неподдерживаемые слова режимов, неположительный тайм-аут, `presentation` без `splash`, `override` без `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` отсутствует, вне диапазона 1..99 или нет пресета с таким `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | ключа `settings` нет в каталоге | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | ключ `settings` есть в каталоге, но доступен только для чтения | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | каталог `assets` или файл `profile` не существует внутри пакета | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` не пуст, а итоговой карты в нём нет (или её невозможно определить) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | явный аргумент CLI направлен на поле, которым владеет профиль | 2 |

Все эти ошибки возникают во время компиляции командной строки, до планирования. Это `SessionProfileException` (или `ArgumentException` для конфликта), и они никогда не запускают сеанс. Полный каталог приведён в разделе [ошибки](errors.md); коды выхода — в разделе [коды выхода](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Обнаружение установленных профилей

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) компилирует каждый пакет в `.omsilaunch\session-profiles\` и выводит по одной записи на пакет: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` и его `Presets` (`Index`, `Id`, `Name`) в порядке индексов. Пакет, который не компилируется, всё равно перечисляется, с `IsValid: false` и тем же кодом `OL_E_SESSION_PROFILE_*`, который сообщил бы запуск. Ничего не записывается.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Как профиль отображается в API

После успешной загрузки спецификация содержит запись `SessionProfileMetadata` в `LaunchSpec.SessionProfile`:

| Поле | Источник |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` выбранного пресета |
| `PresetIndex` | `index` выбранного пресета |
| `PresetName` | `name` выбранного пресета |
| `PackagePath` | абсолютный путь к каталогу пакета |

Планировщик добавляет информационное диагностическое сообщение `session_profile.selected` в каждый `SessionPlan`, построенный из такой спецификации, с ключами данных `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` и `session_profile.path`. Оно не влияет на исполнимость. Интеграторы, напрямую использующие [публичный API](public-api.md), могут вызывать `SessionProfileCompiler.Load` и `SessionProfileCompiler.Apply` из `OmsiLaunch.Core`; YAML-представление никогда не попадает в `OmsiLaunch.Api`.

<a id="examples"></a>
## Примеры

<a id="example-1-settings-only-profile-one-preset"></a>
### Пример 1: профиль только с параметрами, один пресет

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

Запуск: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. Карта и точка входа берутся из командной строки, потому что профиль не определяет блок `new`; добавить `/set:graphics.maxFPS=60` можно, а добавление `/set:traffic.humans=10` является конфликтом.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Пример 2: профиль, привязанный к карте, с тремя пресетами и ресурсами в пакете

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, с `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` и `textures\offline.itx` внутри пакета:

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

Запуск: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. С `/saved:situations\mytrip.osn` блок `new` пропускается, и `.osn` должен ссылаться на `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### Пример из релизного пакета

Релиз содержит `docs/examples/session-profiles/rmg-leste/profile.yaml` ([просмотр](../../../examples/session-profiles/rmg-leste/profile.yaml)). Он синтаксически корректен, соответствует схеме и загрузился бы без ошибок. Две особенности не позволяют ему в неизменном виде запустить сеанс в этой сборке:

1. Он задаёт `new.date`, `new.time` и `new.weather`, из-за чего план становится неисполнимым (см. [Блок `new`](#new)).
2. Его значения путей — простые скаляры с удвоенными обратными косыми чертами (`maps\\RMG Leste\\global.cfg`). YAML сохраняет их удвоенными, а идентификаторы карт сравниваются как текст (только после нормализации `/` в `\`), поэтому `new.map` и `compatibility.maps` не совпали бы с идентификатором из каталога `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` при планировании). Значение `assets` при этом разрешается, потому что нормализация путей Windows схлопывает удвоенные разделители.

Исполнимая форма для этой сборки:

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
## Связанные страницы

- [Справочник по CLI](cli.md) — о `/predefined-profile`, `/predefined-profile-index`, `/set` и флагах мира
- [LaunchSpec](launchspec.md) — о записи, в которую компилируется профиль
- [Транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md) — о том, как применяются и восстанавливаются overlay для `settings`, заставки и `.itx`
- [Возможности](capabilities.md) и [статус проверки в runtime](../status/runtime-validation-status.md)
