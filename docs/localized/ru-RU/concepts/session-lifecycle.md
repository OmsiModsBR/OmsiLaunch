# Жизненный цикл сеанса

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> Перевод [исходной страницы на английском языке](../../../concepts/session-lifecycle.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице описано, как сеанс OmsiLaunch проходит через состояния `SessionState` от `Created` до `Completed` или `Failed`: какой компонент устанавливает каждое состояние, какие события телеметрии плагина вызывают переходы, как работает тайм-аут запуска, что означает остановка (принудительное завершение), что возвращает `WaitForAsync`, какие состояния являются терминальными, какие состояния никогда или почти никогда не наблюдаются и какие гарантии даёт владелец в CLI. Всё изложенное взято из `OmsiLaunchService.StartAsync`, `SuperviseAsync` и `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) и `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Связанные страницы: [публичный API](../reference/public-api.md), [коды ошибок](../reference/errors.md), [транзакции и восстановление после сбоя](transactions-and-recovery.md), [постоянный плагин](permanent-plugin.md), [runtime-управление](../reference/runtime-control.md), [локальная плоскость управления](../reference/local-control.md), [трей Windows](../reference/windows-tray.md), [справочник CLI](../reference/cli.md), [статус проверки в runtime](../status/runtime-validation-status.md), [каталог `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="overview"></a>
## Обзор

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## Справочник `SessionState`

Значения приведены в порядке объявления. Столбец «Кем устанавливается» называет код, который вызывает `Move`/`Fail`; столбец «Наблюдаемость» показывает, могут ли `GetStatusAsync`/`WaitForAsync` увидеть это состояние на практике.

| № | Состояние | Кем устанавливается | Наблюдаемость | Значение |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (начальное значение активного сеанса) | Кратковременно | Сеанс зарегистрирован; ещё ничего не произошло. |
| 1 | `ValidatingPlatform` | никем | Нет | Объявлено, но текущий сервис его никогда не устанавливает (проверка платформы выполняется в `PlanSessionAsync`, у которого нет состояния сеанса). |
| 2 | `Planning` | никем | Нет | Объявлено, никогда не устанавливается (планирование происходит до появления сеанса; повторное планирование в `StartSessionAsync` также предшествует регистрации). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Да | Захватывается аренда установки (lease). Ошибка: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Да | Незавершённый `journal.json` восстанавливается до чтения активной установки; проверяется замыкание постоянного плагина (набор его файлов) (`plugin.integrity.reference`), вычисляется хеш `Omsi.exe`, подготавливаются ресурсы заставки, удаляется устаревший `closecheck`. Ошибки: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (процесс из журнала жив). |
| 5 | `Snapshotting` | `StartAsync` | Практически нет | Устанавливается непосредственно перед `ApplyingConfiguration` без await между ними; сам снимок (snapshot) делается внутри `ApplyAsync`. Кратковременное и ненаблюдаемое состояние. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Да | Записывается `journal.json` (`Prepared`), создаются резервные копии оригиналов, записываются overlay (временная замена файла на время сеанса), удаляются файлы, помеченные на удаление на время сеанса (`Applied`). Ошибки: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, ошибки ввода-вывода. |
| 7 | `DeployingRuntime` | `StartAsync` | Да | Состояние журнала `RuntimeDeployed`. Никакие файлы не развёртываются: замыкание плагина постоянно. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Да | Существуют handoff (`OmsiLaunch.Handoff.<id>`), слот телеметрии (`OmsiLaunch.Telemetry.<id>`) и почтовый ящик (mailbox) runtime (`OmsiLaunch.Runtime.<id>`); состояние журнала `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Да | `CreateProcessW` для `<root>\Omsi.exe` с рабочим каталогом `<root>`. Ошибки: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Да | Процесс существует, `process.started` записано, журнал в состоянии `ProcessStarted`, супервизор запущен, и `StartSessionAsync` возвращает управление. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` по `plugin.started` | Да | Постоянный плагин прочитал корректный handoff для этого сеанса. С этого момента тайм-аут означает `OL_E_STARTUP_TIMEOUT`, а не `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` по `world.starting` | Да (только NEW_MAP) | Плагин вызвал нативный запуск NEW_MAP в UI-потоке OMSI. Сохранённые ситуации отправляют `world.situation.starting`, которое не сопоставлено ни с каким состоянием, поэтому сеанс SAVED_SITUATION переходит из `PluginBootstrap` сразу в `Running`. |
| 13 | `EnteringGameplay` | никем | Нет | Объявлено, никогда не устанавливается: `gameplay.entered` переводит сеанс прямо в `Running`. |
| 14 | `Running` | `ApplyTelemetry` по `gameplay.entered` | Да | Игровой процесс достигнут. Разрешён `ExecuteRuntimeAsync`; владелец в CLI открывает локальную плоскость управления; трей показывает работающий сеанс. |
| 15 | `ProcessExited` | `SuperviseAsync` | Да (только для успешных сеансов) | OMSI завершилась (штатно или принудительно), журнал в состоянии `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (и путь обработки ошибки запуска) | Да (только для успешных сеансов) | Каждый файл, которым владеет сеанс, восстанавливается из проверенной резервной копии; артефакты сеанса удаляются. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Да (только для успешных сеансов) | Восстановление проверено, журнал и резервные копии удалены; хранилища runtime вот-вот будут освобождены. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Да, терминальное | Хранилища освобождены, почтовый ящик закрыт, аренда освобождена, ошибка не зафиксирована. |
| 19 | `Failed` | `LiveSession.Fail` из `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Да, терминальное | Зафиксировано диагностическое сообщение об ошибке. Состояние «липкое»: последующие вызовы `Move` игнорируются, поэтому сеанс с ошибкой никогда не показывает `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`, хотя завершение процесса и восстановление всё равно выполняются. |

Терминальные состояния: `Completed` и `Failed`. После любого из них `WaitForAsync` возвращает управление немедленно, а `CloseAsync` возвращает управление, не запрашивая остановку.

Проверка утверждения «никогда не устанавливается»: поиск по кодовой базе `SessionState.ValidatingPlatform`, `SessionState.Planning` и `SessionState.EnteringGameplay` находит только объявление перечисления; `SessionState.Snapshotting` встречается один раз, и сразу за ним следует `Move(SessionState.ApplyingConfiguration)`.

<a id="start-phase-startsessionasync"></a>
## Этап запуска (`StartSessionAsync`)

1. Неисполнимый план отклоняется (`OL_E_PLAN_NOT_RUNNABLE`), спецификация планируется повторно (повторно вычисляется хеш `Omsi.exe`, заново разрешается контент, повторно проверяется замыкание плагина) и снова отклоняется, если она больше не исполнима. Регистрируется активный сеанс (`Created`).
2. Создаётся трассировка хоста `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (более старые файлы с префиксом сеанса сверх 50 самых новых сеансов удаляются). `StartupTimeoutSeconds` вне диапазона 1..600 отклоняется (`ArgumentOutOfRangeException`; регистрация сеанса отменяется).
3. `AcquiringInstallationLock` → аренда. `RecoveringPreviousTransaction` → восстанавливается незавершённый журнал (журнал, созданный до появления отпечатков и не способный доказать владение, откладывается и повторяется, когда overlay этого сеанса уже существуют), проверяется замыкание постоянного плагина, вычисляется хеш исполняемого файла, удаляется устаревший `closecheck`, строится транзакция (overlay: патчи `options.cfg`, BMP управляемой заставки, `Texture\standard.itx`; удаления: цели ITX, `Texture\standard.ipr`, `closecheck`, если он не существует).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`, затем запускается задача супервизора и возвращается handle.
5. Любое исключение на шагах 3–4 перехватывается: сеанс переходит в `Failed` с `OL_E_START_SESSION` (с внутренним сообщением), созданный процесс завершается и ожидается, хранилища освобождаются, транзакция восстанавливается (`OL_E_RESTORE_FAILED` при неудаче) или, если завершение OMSI не удалось подтвердить, остаётся незавершённой с `OL_E_RESTORE_DEFERRED`; аренда освобождается. В этом случае `StartSessionAsync` всё равно возвращает handle; состояние следует читать через `GetStatusAsync`.

Повторное планирование сохраняет `SessionId` вызывающей стороны, поэтому идентификатор в handle равен `plan.SessionId`.

<a id="supervision-superviseasync"></a>
## Супервизия (`SuperviseAsync`)

Супервизор работает в задаче пула потоков и повторяет цикл каждые 100 ms, пока OMSI не завершится или не будет запрошена остановка:

1. Считывается каждое событие телеметрии, зафиксированное плагином с момента последнего чтения, по порядку (кольцо событий; читатель, который отстал, получает `telemetry.lost` с количеством потерянных событий). Каждое событие добавляется в `RuntimeEvents` и сопоставляется через `ApplyTelemetry`.
2. Если сеанс в состоянии `Failed`, цикл завершается.
3. Если сеанс ещё не в `Running` и крайний срок (`StartupTimeoutSeconds` после входа в супервизор) истёк: `Fail` с `OL_E_STARTUP_TIMEOUT`, если `PluginBootstrap` был достигнут, иначе `OL_E_PLUGIN_NOT_LOADED`; цикл завершается.

После цикла: если OMSI завершилась до `Running` и ошибка не была зафиксирована, выполняется `Fail` с `OL_E_PROCESS_EXITED_EARLY`. Затем, независимо от того, завершился ли сеанс с ошибкой: OMSI принудительно завершается, если она ещё жива, ожидается выход, журнал помечается `ProcessExited`, выполняется переход в `ProcessExited`, восстановление (`Restoring` → `CleaningRuntime`) или фиксация `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, освобождаются дескриптор процесса, handoff, слот телеметрии и почтовый ящик runtime, освобождается аренда, и выполняется переход в `Completed`, если состояние не `Failed`. Сбой внутри самого супервизора фиксируется как `OL_E_PROCESS_SUPERVISION` (проблемы очистки — как `OL_E_PROCESS_CLEANUP_FAILED`), и выполняется тот же путь завершения процесса и восстановления.

Поскольку `Failed` — «липкое» состояние, единственное подтверждение того, что сеанс с ошибкой был восстановлен, — это отсутствие `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` в его диагностике (и отсутствие `journal.json`); примечания о восстановлении (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) появляются в обоих случаях.

<a id="telemetry-events"></a>
## События телеметрии

Плагин публикует образцы JSON `{ "name": ..., "data": {...} }` в слот телеметрии; хост записывает каждый новый образец как `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Кем отправляется | Действие хоста |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` после чтения корректного handoff | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: нет `OMSILAUNCH_HANDOFF_NAME`, handoff нечитаем или не проходит проверку | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: handoff запрашивает режим мира, отличный от NEW_MAP/SAVED_SITUATION, запуск не в headless-режиме, транспортное средство игрока, режимы даты/времени или пустую идентичность ситуации | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: проверка сборки внутри процесса не пройдена | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | только записывается |
| `headless.arm.failed` | `PluginRuntime.Start`: не удалось взвести нативный хук headless-запуска | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | только записывается |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart`, когда `InternetTextures.Mode` равно `Disabled` | только записывается |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 или 4) | NEW_MAP: OMSI ещё не готова; запуск повторяется на следующем тике UI-таймера | только записывается |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | Успешный путь NEW_MAP | только записывается |
| `world.failed` (`native_status`) | NEW_MAP: нативный запуск вернул ошибку | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | Путь SAVED_SITUATION | только записывается (без смены состояния) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: нативный запуск вернул ошибку | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: поля выбора точки входа или `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | конец запуска мира | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle`, после того как любая операция `d3d.*` активировала пробу | только записывается |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle`, когда повторное применение активной `camera.lock` выбрасывает исключение (сообщается один раз для каждой отдельной ошибки) | только записывается |
| Некорректный JSON | любой | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Оговорки: слот хранит один образец, поэтому события, отправленные в пределах одного опроса хоста длительностью 100 ms, могут быть потеряны (плагин подавляет события жизненного цикла в течение 2 s после `gameplay.entered` и никогда не публикует событие D3D в том же тике, в котором публикуется `gameplay.entered`, поэтому граница `Running` не пропускается). `RuntimeEvents` хранит 256 самых последних событий; более старые отбрасываются. Это не лог без потерь. События следует читать через `GetStatusAsync`, `session.events` в плоскости управления или `events read|watch` в CLI.

<a id="startup-timeout"></a>
## Тайм-аут запуска

| Элемент | Значение |
| --- | --- |
| Источник | `LaunchSpec.Behavior.StartupTimeoutSeconds` (по умолчанию 180; 1..600; в CLI `/startup-timeout`, в профиле `behavior.startup-timeout`). |
| Начало отсчёта | Когда задача супервизора входит в свой цикл (после возврата handle). |
| Истечение до `PluginBootstrap` | `Failed` с `OL_E_PLUGIN_NOT_LOADED`. |
| Истечение после `PluginBootstrap`, до `Running` | `Failed` с `OL_E_STARTUP_TIMEOUT`. |
| После `Running` | Тайм-аут не применяется; сеанс длится, пока OMSI не завершится или не будет запрошена остановка. |
| Владелец в CLI | Ожидает `Running` в течение `StartupTimeoutSeconds + 5` секунд; при неудаче выводит статус, в `OmsiLaunchW.exe` показывает диалог с последним диагностическим сообщением `OL_E_` (резервный код `OL_E_SESSION_START_FAILED`) и завершается с кодом 1 после `CloseAsync`. |

`ShutdownTimeoutSeconds` передаётся в спецификации, но не используется: ожидания штатного завершения нет.

<a id="stop-semantics"></a>
## Семантика остановки

Каждый запрос остановки — это один и тот же канонический запрос: `StopAsync(handle)` из API, `CloseAsync` для нетерминального сеанса, `session.stop` в локальной плоскости управления (привязан к идентификатору активного сеанса), пункт «End session» (завершить сеанс) в трее, Ctrl+C или закрытие консоли у владельца в CLI, а также окончание `/observe-seconds`.

| Шаг | Подробности |
| --- | --- |
| 1 | В активном сеансе устанавливается `StopRequested`; вызывающая сторона сразу получает управление обратно. |
| 2 | В течение 100 ms супервизор выходит из цикла и вызывает `TerminateProcess(Omsi.exe, 1)`. Это принудительное завершение: процедура завершения OMSI не выполняется, OMSI не перезаписывает `options.cfg`, диалог сохранения не появляется. Так сделано намеренно, чтобы OMSI не могла перезаписать файлы, которые транзакция собирается восстановить. |
| 3 | Супервизор ожидает завершения процесса, фиксирует `ProcessExited`, точно восстанавливает каждый файл, которым владеет сеанс (включая маркер `closecheck`, записанный OMSI во время сеанса; он превращается в примечание `restore.session-artifact-removed`), удаляет журнал и резервные копии, освобождает хранилища runtime (последующие вызовы `ExecuteRuntimeAsync` выбрасывают `OL_E_RUNTIME_CHANNEL_CLOSED` или `OL_E_SESSION_NOT_RUNNING`), освобождает аренду и переходит в `Completed`. |
| Штатный выход | Если OMSI завершается сама после `Running` (пользователь закрывает OMSI), выполняется тот же путь без принудительного завершения, и сеанс завершается нормально. До `Running` это `OL_E_PROCESS_EXITED_EARLY`. |
| Кооперативное завершение | Не реализовано. Отправка `WM_CLOSE` и ожидание `ShutdownTimeoutSeconds` не реализованы (решение по продукту; в раунде завершающей проверки в runtime OMSI проигнорировала `WM_CLOSE`, отправленное её главному окну, `L05b`) ([статус проверки в runtime](../status/runtime-validation-status.md)). |
| Состояние на стороне runtime | Всё, что изменено через runtime-операции (часы, камера, созданные ТС, переменные скриптов, текстуры D3D), является состоянием внутри процесса и исчезает вместе с процессом; оно никогда не восстанавливается и не сохраняется. |

<a id="waitforasync-semantics"></a>
## Семантика `WaitForAsync`

| Ситуация | Результат |
| --- | --- |
| Сеанс достигает запрошенного состояния | Возвращает статус с `State == requested`. |
| Сеанс раньше достигает терминального состояния | Немедленно возвращает `Completed` или `Failed` (проверьте `Diagnostics`). |
| Истекает тайм-аут | Возвращает текущий статус (без исключения). Сравните `State` с запрошенным состоянием. |
| Запрошенное состояние уже пройдено (или никогда не устанавливается: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, фактически `Snapshotting`) | Ожидает терминального состояния или тайм-аута. |
| Вызывающая сторона отменяет операцию | `OperationCanceledException`. |
| Неизвестный или закрытый handle | `KeyNotFoundException`. |

Интервал опроса — 100 ms, поэтому наблюдаемые переходы отстают от реальных не более чем на 100 ms.

<a id="owner-lifecycle-guarantees-cli"></a>
## Гарантии жизненного цикла владельца (CLI)

`OwnerSession.RunAsync` в `tools/OmsiLaunch.Cli/Program.cs` — эталонный владелец.

| Гарантия | Подробности |
| --- | --- |
| Единственный владелец | Перед запуском CLI опрашивает канал управления; если владелец отвечает, запуск отклоняется с `OL_E_SESSION_ALREADY_ACTIVE` (код выхода 7). Аренда обеспечивает то же правило между процессами. |
| Каждый путь выхода доходит до `CloseAsync` | Начиная с `StartSessionAsync`, исключения, Ctrl+C (`CancelKeyPress`), закрытие консоли или выход из системы (`ProcessExit`: запрашивается остановка, и владелец ждёт `Completed` до 4 s; всё оставшееся восстанавливается по журналу при следующем запуске), остановка из трея, `session.stop` в плоскости управления, истечение `/observe-seconds` и штатное завершение — все заканчиваются в блоке `finally`, который освобождает плоскость управления и трей и ожидает `CloseAsync`. |
| `/observe-seconds` — верхняя граница | Запросы остановки из трея или плоскости управления по-прежнему завершают сеанс раньше. |
| Плоскость управления только в состоянии Running | Конечная точка именованного канала (named pipe) создаётся после `Running` (и после всех пакетов проверки `INTERNAL`) и освобождается до `CloseAsync`; в остальное время клиенты получают `OL_E_NO_ACTIVE_SESSION`. |
| Код выхода | 0, если конечное состояние `Completed`; 1, если оно `Failed` или игровой процесс не был достигнут; 8, если запрошенное восстановление после сбоя не завершилось ([коды выхода](../reference/exit-codes.md)). |
| Диагностика | Трассировка хоста и артефакты runtime-операций в `<root>\.omsilaunch\diagnostics`, лог трея `tray-host.log`; никакие данные не покидают компьютер. |

Интеграторы, которые пишут собственного владельца, должны воспроизвести первые две гарантии: один `StartSessionAsync` на установку в каждый момент времени и `CloseAsync` на каждом пути.

<a id="failure-map"></a>
## Карта ошибок

| Этап | Состояние при ошибке | Диагностика, которую вы увидите |
| --- | --- | --- |
| План | нет (сеанса нет) | `OL_E_PLAN_NOT_RUNNABLE`, выбрасываемое `StartSessionAsync`; собственные коды `OL_E_` плана ([проверка LaunchSpec](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Запуск (от аренды до создания процесса) | `Failed` | `OL_E_START_SESSION` с внутренним кодом; возможно `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Начальная загрузка плагина | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| Запуск мира | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Работа | `Failed` только при сбоях супервизора | `OL_E_PROCESS_SUPERVISION`; ошибки runtime-операций никогда не переводят сеанс в состояние ошибки. |
| Завершение процесса и восстановление | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Каждый путь ошибки всё равно пытается завершить процесс и выполнить восстановление; оставшийся журнал восстанавливается при следующем запуске или через `RecoverPendingAsync` / `/recover` ([транзакции и восстановление после сбоя](transactions-and-recovery.md)).
