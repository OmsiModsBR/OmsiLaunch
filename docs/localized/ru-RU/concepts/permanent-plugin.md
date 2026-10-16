# Постоянный плагин

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Перевод [исходной страницы на английском языке](../../../concepts/permanent-plugin.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

OmsiLaunch управляет OMSI изнутри процесса OMSI с помощью плагина, который устанавливается один раз в `plugins\OmsiLaunch.*` как часть продукта. Сеанс никогда не размещает, не копирует, не включает в снимок, не восстанавливает и не удаляет его. На этой странице объясняется, что входит в замыкание плагина (набор его файлов), как OMSI его загружает, как хост проверяет его перед каждым запуском, как хост и плагин взаимодействуют (handoff, телеметрия, почтовый ящик (mailbox) runtime) и что делает плагин, когда OMSI запускается без OmsiLaunch. Источники: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, три хранилища в общей памяти), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` и `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## Замыкание (9 файлов)

| Файл в `plugins\` | Роль |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Дескриптор плагина OMSI. Его содержимое — `[dll]`, за которым следует `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Нативная x86-прослойка экспорта, сгенерированная DNNE 2.0.6. Экспортирует ABI плагинов OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) и размещает в процессе среду выполнения .NET. Содержит ресурс версии продукта. |
| `OmsiLaunch.Plugin.dll` | Управляемый плагин (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Манифест зависимостей .NET для плагина. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Конфигурация среды выполнения .NET (платформа `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Форматы передачи данных и публичные записи, общие с хостом. |
| `OmsiLaunch.Builds.Omsi23004.dll` | Профиль сборки: отпечаток исполняемого файла, глобальные переменные, раскладки объектов, адреса методов. |
| `OmsiLaunch.Interop.dll` | Средства чтения и записи памяти внутри процесса, построенные на профиле. |
| `OmsiLaunch.Native.x86.dll` | Нативный мост (C++): проверка сборки, хук headless-запуска, применение времени, MakeVehicle, PlaceRandomBus, подавление интернет-текстур, доступ к устройству D3D9. |

`RuntimeArtifactSet.Load` выводит этот список из собственного каталога `plugins\` контроллера: четыре именованных файла (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), каждый `OmsiLaunch.*.dll` в этом каталоге, кроме `OmsiLaunch.PluginNE.dll`, и нативный мост. Среди них обязательно должен быть `OmsiLaunch.Plugin.dll`. Произвольные DLL никогда не попадают в OMSI. Упаковщик релиза (`tools/New-ReleasePackage.ps1`) записывает ровно девять перечисленных выше файлов.

<a id="how-omsi-loads-it"></a>
## Как OMSI его загружает

1. OMSI перечисляет `plugins\*.opl` и загружает DLL, указанную в `OmsiLaunch.Plugin.opl`: `OmsiLaunch.PluginNE.dll`.
2. Прослойка DNNE запускает внутри процесса OMSI среду выполнения x86 .NET 6, описанную в `OmsiLaunch.Plugin.runtimeconfig.json`, и разрешает управляемые экспорты в `OmsiLaunch.Plugin.dll`.
3. OMSI вызывает `PluginStart`. Во время запуска OMSI может вызвать его несколько раз; учитывается только первый вызов (защита `Interlocked.Exchange`), поскольку успешный запуск владеет одноразовым нативным хуком. Последующие вызовы сразу возвращают управление.
4. `PluginStart` сначала проверяет `OMSILAUNCH_INTERNET_TEXTURES_MODE`: если значение равно `Disabled`, применяется нативное подавление загрузчика (`internet-textures.suppressed` или `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` читает handoff (см. ниже), проверяет сборку внутри процесса, взводит хук headless-запуска, открывает почтовый ящик runtime и планирует запуск мира в UI-потоке OMSI через обратный вызов `SetTimer`. Ни одна runtime-команда никогда не выполняется в рабочем потоке IPC; всё выполняется в обратном вызове таймера, в исходном UI-потоке OMSI.
6. `PluginFinalize` уничтожает таймер, завершает работу runtime (`NativeD3DShutdown`) и восстанавливает патч интернет-текстур.

Экспорты `AccessVariable`, `AccessTrigger`, `AccessStringVariable` и `AccessSystemVariable` пусты; OmsiLaunch не использует канал плагинов OMSI для переменных скриптов.

<a id="integrity-validation-before-every-start"></a>
## Проверка целостности перед каждым запуском

`RuntimeArtifactSet.ValidateInstalled` выполняется во время `StartSessionAsync` (после раннего восстановления после сбоя, до подготовки транзакции) и во время `PlanSessionAsync` (только проверка наличия, через `LoadArtifacts`). Диагностическое сообщение плана `plugin.integrity.reference` сообщает, какой эталон был использован.

| Ситуация | Эталон | Проверка каждого файла | Ошибки |
| --- | --- | --- | --- |
| `release-manifest.json` находится рядом с `OmsiLaunch.exe` (установленный пакет) | `manifest` | Установленный файл должен существовать, а его SHA-256 должен совпадать с записью манифеста для `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (в манифесте нет записи для обязательного файла), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Манифеста нет (раскладка разработки) | `self` | Наличие и самосогласованность: хеш установленного файла должен совпадать с хешем файла в собственном каталоге `plugins\` контроллера | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Манифест нечитаем или повреждён (нет массива `files`, запись без `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Замыкание в каталоге контроллера неполное | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (план неисполним); CLI дополнительно сообщает `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, если отсутствует `plugins\OmsiLaunch.Plugin.opl` или `OmsiLaunch.Native.x86.dll` |

Используются только записи манифеста из `plugins/`; манифест — это данные, а не политика. Манифест генерируется упаковщиком релиза с SHA-256 каждого размещённого файла. Когда контроллер работает из корневого каталога OMSI (раскладка релиза), источник и назначение — один и тот же каталог; поэтому без манифеста проверка сводится к наличию и самосогласованности, и именно поэтому релизные пакеты всегда содержат `release-manifest.json`. Способ устранения несовпадения: переустановить пакет, чтобы `plugins\` и `release-manifest.json` согласовывались.

Перед любым патчем плагин определяет и проверяет каждую нативную цель из каталога сборок по загруженному образу и передаёт проверенную таблицу в `OmsiLaunch.Native.x86.dll`, который не содержит ни одного адреса OMSI (телеметрия `native.capabilities`). Запуск, для которого недоступна возможность `boot.headless-start` или возможность режима мира, завершается с `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); живая форма Start в неожиданном состоянии завершается с `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). См. [совместимость](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## От хоста к плагину: переменные окружения

`CreateProcessW` запускает `Omsi.exe` с окружением родительского процесса, дополненным следующими переменными:

| Переменная | Значение | Потребитель |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID сеанса (формат `D`) | `CurrentRuntimeControl` помечает им handle D3D (формат `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` открывает это отображение только для чтения |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` или `Override` | `PluginStart` (эффект внутри процесса имеет только `Disabled`) |

Три отображения создаются хостом до запуска процесса (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) и освобождаются, когда завершается задача жизненного цикла сеанса. Это именованные объекты ядра с DACL по умолчанию для запускающего пользователя; любой процесс того же пользователя может их открыть (принятая модель доверия в пределах одного пользователя, см. [известные ограничения](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## Startup handoff

Отображаемая в память запись только для чтения с фиксированной раскладкой (`StartupHandoffWire`, сигнатура `OLSH`, версия 4; версия 3 по-прежнему принимается читателем). 64-байтовый заголовок содержит сигнатуру, версию, размер заголовка, общий размер, GUID сеанса, размер полезной нагрузки и SHA-256 полезной нагрузки. Полезная нагрузка содержит `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 с префиксом длины), `PresentedEntrypointIndex`, `WorldMode`, флаги (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` и `TimeMode`. Плагин заново вычисляет хеш полезной нагрузки и отклоняет любое несовпадение (`plugin.handoff.invalid`, ошибка хоста `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Отображение размером более 1 MiB или с несогласованным размером отклоняется так же.

Плагин принимает handoff, только если `WorldMode` равно `NewMap` или `SavedSituation`, `HeadlessStart` установлен, `PlayerVehicleEnabled` сброшен, оба режима даты и времени равны `Unset`, а сохранённая ситуация указывает свой `.osn`. Всё остальное — `plugin.request.unsupported` (ошибка хоста `OL_E_CAPABILITY_UNAVAILABLE`). Хост всегда устанавливает `HeadlessStart`.

<a id="telemetry-slot"></a>
## Слот телеметрии

`OmsiLaunch.Telemetry.<session>` — кольцо событий (Beta 4, `TelemetryRingWire`): заголовок 64 байта (сигнатура `OLTR`, версия 1, число слотов, размер слота, зафиксированный номер последовательности) и 64 нумерованных слота по 2048 байт, каждый из которых содержит одно событие в формате UTF-8 JSON `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` — монотонные часы сеанса плагина). Плагин — единственный записывающий: он делает слот недействительным, записывает его, фиксирует номер последовательности слота, затем публикует номер последовательности кольца. Хост каждые 100 ms читает все события, зафиксированные с момента последнего чтения, по порядку; слот, перезаписанный во время копирования, считается потерянным и никогда не доставляется «разорванным». Хост, отставший более чем на 64 события, получает runtime-событие `telemetry.lost` с точным количеством. Событие, превышающее слот, заменяется на `telemetry.oversized` с его названием. События добавляются в `SessionStatus.RuntimeEvents` (ограничено последними 256) и управляют семантическим жизненным циклом (`plugin.started`, `world.starting`, `gameplay.entered`, события модулей и ошибки).

<a id="runtime-command-mailbox"></a>
## Почтовый ящик runtime-команд

`OmsiLaunch.Runtime.<session>` — почтовый ящик на 64 KiB с одним запросом в полёте: `state` (int32 по смещению 0: 0 — простой, 1 — запрошено, 2 — дан ответ), `length` (int32 по смещению 4), конверт по смещению 8. Конверты — это записи `RuntimeCommandWire` (сигнатура `OLRC`, версия 1, 72-байтовый заголовок с видом, общей длиной, GUID сеанса, идентификатором запроса, длиной полезной нагрузки и SHA-256 полезной нагрузки UTF-8 JSON). Плагин опрашивает почтовый ящик из своего таймера в UI-потоке (50 ms после загрузки мира), выполняет команду в этом потоке и публикует ответ, только если слот всё ещё содержит тот же идентификатор запроса; на запрос, от которого хост отказался по тайм-ауту, ответ никогда не даётся. Слишком большой ответ заменяется типизированной ошибкой `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Подробности и тайм-ауты приведены в разделе [runtime-управление](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Политика поиска DLL

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` и сборки CLI объявляют `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Нативные импорты (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) разрешаются только из собственного каталога сборки (`plugins\`) или из системного каталога Windows; корневой каталог OMSI и `PATH` никогда не просматриваются. Поэтому `OmsiLaunch.Native.x86.dll` загружается из `plugins\` и ниоткуда больше.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Когда OMSI запускается без OmsiLaunch

Поскольку замыкание постоянно, OMSI загружает `OmsiLaunch.PluginNE.dll` при каждом запуске, включая запуски из Steam или с рабочего стола. В этом случае:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` отсутствует, поэтому патч загрузчика не применяется.
- `OMSILAUNCH_HANDOFF_NAME` отсутствует, поэтому `PluginRuntime.Start` отправляет `plugin.handoff.invalid` и возвращает `false`. `CurrentTelemetrySink.Emit` сразу возвращает управление, если `OMSILAUNCH_TELEMETRY_NAME` не задана, поэтому ничего никуда не записывается.
- Нет проверки сборки, нет нативного хука, нет почтового ящика, нет таймера. Плагин остаётся загруженным, но бездействует; OMSI ведёт себя так, будто плагина нет.
- `PluginFinalize` при завершении OMSI вызывает нативную процедуру восстановления и `NativeD3DShutdown`; обе ничего не делают, если ничего не было установлено.

Поэтому для обычного запуска OMSI удалять `.opl` не нужно.

<a id="non-interference-with-third-party-plugins"></a>
## Невмешательство в сторонние плагины

Сеансы никогда не перечисляют, не хешируют, не копируют, не удаляют и не восстанавливают другие файлы в `plugins\`. Плагин не использует канал `AccessVariable` OMSI и не затрагивает состояние других плагинов. Единственные патчи внутри процесса — профилированный хук headless-запуска (одноразовое перенаправление VMT, взводимое для сеанса), необязательное подавление интернет-текстур (восстанавливается в `PluginFinalize`) и перехват `Reset` устройства D3D9, используемый для отслеживания жизненного цикла текстур.

<a id="difference-from-omsihook"></a>
## Отличие от OmsiHook

OmsiLaunch **не имеет зависимости в runtime** от OmsiHook или от каких-либо двоичных файлов OmsiHook: единственные подключённые пакеты — `DNNE` 2.0.6 и `YamlDotNet` 15.1.2, и нигде в продукте нет ни `using OmsiHook`, ни P/Invoke в DLL OmsiHook. Общее у OmsiLaunch с OmsiHook — производные знания: раскладки объектов и несколько обёрток чтения были сверены по зафиксированной копии OmsiHook (`space928/Omsi-Extensions`, коммит `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) с точным исполняемым файлом `Omsi23004_692EBFBF`. Указание авторства и условия лицензии приведены в `THIRD-PARTY-NOTICES.md`, а пофайловая матрица повторного использования — в `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook использует внедрение отдельного процесса и предоставляет сырые указатели; OmsiLaunch работает внутри процесса, предоставляет только непрозрачные handle с областью действия сеанса и удаляет любые нативные адреса из публичных результатов (см. [возможности](../reference/capabilities.md)).
