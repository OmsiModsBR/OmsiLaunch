# Документация OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Перевод [исходной страницы на английском языке](../../README.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Это нормативная английская документация OmsiLaunch `0.1.0-beta4` — базовой версии после этапа укрепления (post-hardening). OmsiLaunch обеспечивает программируемый запуск, владение сеансом и runtime-управление для установленной OMSI 2; каждая нативная возможность определяется и проверяется отдельно для каждой сборки (см. [совместимость](reference/compatibility.md)). Каждая страница в `docs/` описывает то, что делает текущий код; если страница и код расходятся, прав код, а страница содержит ошибку.

Во всей документации используется следующая шкала стабильности: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL`, `UNAVAILABLE`. Флаги, которые разбираются, но ничего не делают, помечены как `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` (принимается для совместимости, сейчас ни на что не влияет). Ничто не считается проверенным в runtime, если этого не подтверждает [статус проверки в runtime](status/runtime-validation-status.md).

<a id="who-reads-what"></a>
## Кому что читать

| Аудитория | С чего начать | Далее |
| --- | --- | --- |
| Пользователи (CLI, ярлыки, профили сеанса) | [Установка](getting-started/installation.md), [Первый сеанс](getting-started/first-session.md) | [Справочник CLI](reference/cli.md), [Примеры CLI](reference/cli-examples.md), [Профили сеанса](reference/session-profiles.md), [Значок в трее Windows](reference/windows-tray.md), [Коды выхода](reference/exit-codes.md) |
| Интеграторы (`OmsiLaunch.Api`, локальный IPC) | [Быстрый старт с публичным API](getting-started/api-quick-start.md), [Справочник публичного API](reference/public-api.md), [Справочник LaunchSpec](reference/launchspec.md) | [Жизненный цикл сеанса](concepts/session-lifecycle.md), [Runtime-управление](reference/runtime-control.md), [Возможности](reference/capabilities.md), [Локальное управление / IPC](reference/local-control.md), [Справочник ошибок](reference/errors.md) |
| Сопровождающие (выпуск, проверка, границы) | [Упаковка](reference/packaging.md), [Модель постоянного плагина](concepts/permanent-plugin.md) | [Транзакции и восстановление после сбоя](concepts/transactions-and-recovery.md), [Совместимость](reference/compatibility.md), [Известные ограничения](reference/known-limitations.md), [Статус проверки в runtime](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Навигация

| Страница | Назначение |
| --- | --- |
| [Начало работы](getting-started/first-session.md) | Спланировать, запустить, наблюдать и остановить один сеанс из корневого каталога OMSI. |
| [Установка](getting-started/installation.md) | Предварительные требования, распаковка пакета в корневой каталог OMSI, проверка с помощью `/version`, чистое удаление. |
| [Быстрый старт с публичным API](getting-started/api-quick-start.md) | Законченная программа на .NET, которая планирует, запускает, читает и останавливает один сеанс. |
| [Справочник CLI](reference/cli.md) | Все флаги, командные слова и иерархические маршруты `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Примеры CLI](reference/cli-examples.md) | Готовые к копированию командные строки для типовых задач. |
| [Справочник LaunchSpec](reference/launchspec.md) | Все свойства `LaunchSpec` и значения перечислений, правила загрузки JSON для `/spec`. |
| [Управление HOF](reference/hof-management.md) | Копирование HOF на время сеанса и его зеркалирование в каталоги ТС: источники, цели, конфликты, восстановление. |
| [Runtime-модули](reference/runtime-modules.md) | Модули, которые работают внутри OMSI вместе с сеансом: манифест, доверие, жизненный цикл, политики сбоев, операции. |
| [Справочник профилей сеанса](reference/session-profiles.md) | Схема `profile.yaml` `omsilaunch.session-profile/v1`, ключи, ограничения, приоритет. |
| [Справочник публичного API](reference/public-api.md) | `IOmsiLaunch`, публичные записи и перечисления, стабильность каждого члена. |
| [Перечень публичного API](reference/public-api-inventory.md) | Сгенерированный список всех публичных типов и членов с сигнатурами и стабильностью. |
| [Runtime-управление](reference/runtime-control.md) | Канал runtime-команд, тайм-ауты, handle, семантика остановки. |
| [Справочник возможностей](reference/capabilities.md) | Каталог возможностей и все идентификаторы публичных runtime-операций с их классификацией. |
| [Жизненный цикл сеанса](concepts/session-lifecycle.md) | Переходы `SessionState`, что обещает `StartSessionAsync`, как завершается сеанс. |
| [Транзакции и восстановление после сбоя](concepts/transactions-and-recovery.md) | Состояния журнала, резервные копии, проверка восстановления, удаления в рамках сеанса, восстановление после аварийного завершения. |
| [Модель постоянного плагина](concepts/permanent-plugin.md) | Замыкание плагина `plugins\OmsiLaunch.*`, контроль целостности по манифесту, чего сеанс никогда не касается. |
| [Локальное управление / IPC](reference/local-control.md) | Протокол именованного канала (named pipe) `0.1`, конечная точка для каждой установки, привязка к `session_id`, модель доверия. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | Оконный хост Windows (без консоли): отличия от `OmsiLaunch.exe`, `/silent`, диалоги, коды выхода. |
| [Значок в трее Windows](reference/windows-tray.md) | Индикатор в области уведомлений: значок, меню, окно состояния поле за полем, End session (завершение сеанса), перезапуск Проводника (Explorer). |
| [Справочник ошибок](reference/errors.md) | Все коды `OL_E_*` / `OL_W_*` с категорией и значением. |
| [Коды выхода](reference/exit-codes.md) | Значения `PublicExitCode` от 0 до 10 и коды shim-загрузчика от 100 до 106. |
| [Упаковка / структура установки](reference/packaging.md) | Файлы в релизном ZIP-архиве, `release-manifest.json`, структура `.omsilaunch\`. |
| [Совместимость / поддерживаемые сборки OMSI](reference/compatibility.md) | Единственный поддерживаемый хеш `Omsi.exe`, принимаемый хеш Steam LAA, требования к платформе. |
| [Известные ограничения](reference/known-limitations.md) | Что в этой бета-версии не поддерживается, поддерживается частично или является принятым риском. |
| [Статус проверки в runtime](status/runtime-validation-status.md) | Что выполнялось под OMSI, что выполнялось только офлайн и что ещё требует реального сеанса. |

Страницы, которые остаются нормативными для сопровождающих: `README.md` репозитория
[`README.md`](../../../README.md), спецификации в `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) и страницы проекта в
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Они дают сводку; подробным справочником служат перечисленные выше страницы. Исторические страницы перечислены в [манифесте документации](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Как документация поддерживается в актуальном состоянии

Гейт документации `tests\OmsiLaunch.DocumentationTests` компилируется с `OmsiLaunch.Api` и `OmsiLaunch.Core` и сравнивает перечисленные выше страницы с кодом, который определяет публичную поверхность:

| Гейт | Что проверяет |
| --- | --- |
| `docs.cli-flags` | Каждый элемент `CliInput.KnownFlags` присутствует в справочнике CLI в виде `` `/flag` `` или `` `/flag:` ``; каждый элемент `CliInput.AcceptedNoEffectFlags` помечен в своей строке как `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`; каждое командное слово и каждый маршрут `CliInput.HierarchicalRoutes` приведены вместе со своей runtime-операцией; для каждого значения `PublicExitCode` есть строка `| n |` в таблице кодов выхода. |
| `docs.capabilities` | Каждый идентификатор `PublicCapabilityRegistry.All`, каждый элемент `PublicCapabilityRegistry.PublicRuntimeOperationIds` и каждое имя `PublicCapabilityClassification` присутствуют в справочнике возможностей. |
| `docs.errors` | Каждый код `PublicErrorCodes.All` присутствует в справочнике ошибок, и ни один литерал `OL_E_*` / `OL_W_*` в `src\` или `tools\OmsiLaunch.Cli\` не отсутствует в `PublicErrorCodes`. |
| `docs.public-api` | Каждый экспортируемый тип `OmsiLaunch.Api`, каждое значение перечисления и каждый член `IOmsiLaunch` присутствуют в справочнике публичного API, и используются все пять слов стабильности. |
| `docs.launchspec` | Каждое публичное свойство, достижимое из `LaunchSpec`, и каждое значение его перечислений присутствуют в справочнике LaunchSpec. |
| `docs.session-profiles` | Каждый ключ `SessionProfileCompiler.SchemaKeys`, идентификатор схемы и ограничение `256 KiB` присутствуют в справочнике профилей сеанса. |
| `docs.structure` | Каждая страница из таблицы навигации существует. |
| `docs.links` | Каждая относительная ссылка в `docs\**\*.md` (за исключением `docs\localized\`) и в корневых файлах `*.md` указывает на существующий файл или каталог. |
| `docs.localization` | Для каждой локали, указанной в `docs\localized\LOCALIZATION-MANIFEST.md`, есть все страницы локализуемого набора; каждая страница сохраняет заголовки, таблицы и блоки кода английской страницы, каждый встроенный фрагмент кода (флаги, идентификаторы возможностей и операций, коды ошибок, ключи, идентификаторы) и каждую ссылку, а её относительные ссылки разрешаются. |

Гейт — один из наборов тестов, которые запускает `tools\Invoke-OfflineValidation.ps1` (его можно пропустить с помощью `-SkipDocs`). Он работает офлайн, никогда не запускает OMSI и прерывает сборку, если флаг, маршрут, возможность, код ошибки, значение перечисления или публичный тип не задокументированы либо ссылка не работает. Прозу он не проверяет, поэтому страница всё равно может неверно описывать поведение; о таком случае следует сообщать как об ошибке в странице.

<a id="translations"></a>
## Переводы

`docs\localized\<locale>\` содержит полные переводы этой документации `0.1.0-beta4` для `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` и `ja-JP`. Набор страниц, корневые каталоги локалей и страницы, которые намеренно не переводятся, перечислены в [`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md). Переводы сохраняют без изменений все команды, флаги, идентификаторы, коды ошибок и примеры английских страниц, и гейт `docs.localization` это проверяет. Нормативным источником остаются английские страницы: если перевод расходится с ними, приоритет имеют английская страница и код, а перевод содержит ошибку.
