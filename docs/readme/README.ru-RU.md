<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Управление сеансами для OMSI 2.</strong></p>
<p align="center">Открытый исходный код · Программируемость · Развитие силами сообщества</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <strong>Русский</strong> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Это перевод канонического [README на английском языке (US)](../../README.md). При расхождениях приоритет имеет английский (US) README.

# OmsiLaunch

**OmsiLaunch** — слой с открытым исходным кодом для программного запуска OMSI 2,
управления сеансами и управления работающей симуляцией. Он планирует сеанс
по декларативному описанию, применяет все временные изменения конфигурации
в рамках журналируемой транзакции, запускает OMSI, наблюдает за ней до начала
игрового процесса, позволяет инструментам читать и изменять работающую симуляцию
через публичный API и по завершении сеанса восстанавливает все файлы, которых
касался.

Это инфраструктура для лаунчеров, инструментов, автоматизации и интеграций
сообщества. Это не графический лаунчер.

> **Описывайте сеанс, а не щелчки мышью.**

## Статус: 0.1.0-beta4

Текущая публичная бета-версия — **`0.1.0-beta4`**. Beta 4 добавляет
совместимость по отдельным возможностям (capability), Runtime Modules, профили
сеанса 1..99 и набор функций запуска и runtime. Совместимость, каталог
возможностей, постраничная выдача, типизированные ошибки и мост модулей
наблюдались в реальных сеансах OMSI (раунды проверки A и B). Функции,
добавленные после этих раундов, имеют статус `EXPERIMENTAL`: они закрыты
статическим анализом и проверены только офлайн-тестами, runtime-проверка
ожидается. Часть функций остаётся `STATICALLY_VALIDATED`, `PARTIAL` или
`UNAVAILABLE`.
Два runtime-пункта остаются открытыми, поскольку их нельзя безопасно
воспроизвести: игровой процесс Steam LAA и естественное удаление дорожных
транспортных средств и людей (RV-002). Страница
[статуса проверки в runtime](../localized/ru-RU/status/runtime-validation-status.md) является
авторитетным источником сведений о том, что выполнялось под OMSI, а что — только офлайн.

Это бета-версия: публичный API, CLI и форматы файлов помечены для каждого члена
как `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` или `UNAVAILABLE` и ещё
могут измениться до версии 1.0.

## Поддерживаемые версии OMSI

OmsiLaunch требует установленной OMSI 2. Он не зависит от какого-то одного
файла `Omsi.exe`: каждая нативная возможность разрешается и проверяется по
отдельности внутри OMSI до её использования, а возможность, которую нельзя
проверить на данной сборке, на этой сборке недоступна.

| Пункт | Область поддержки |
| --- | --- |
| Требование | Установленная OMSI 2: 32-битный `Omsi.exe` с версией OMSI 2 (2.x) и каталоги `maps\` и `Vehicles\`. Всё остальное отклоняется с кодом `OL_E_UNSUPPORTED_BUILD`. |
| Известная раскладка | OMSI 2.3.004 (`Omsi23004`). Разрешаются все возможности, и все runtime-проверки выполнялись на этой раскладке. Хеши определяют её как быстрый путь; они не являются требованием. |
| Другие сборки OMSI 2 | Каждая возможность разрешается независимыми от сборки стратегиями (Delphi RTTI, уникальные сигнатуры кода, слоты VMT) и проверяется. Возможностям, читающим раскладку объектов, нужна известная раскладка. |
| Исполняемый файл Steam | Разрешается в процессе; подлинный сеанс Steam **не** проверен в runtime. |
| Операционная система | Windows 10 или новее, x64. |
| Среды выполнения | .NET 6 Desktop Runtime **x64** (контроллер) и .NET 6 Runtime **x86** (плагин работает внутри 32-битного `Omsi.exe`). |

Подробнее: [Совместимость](../localized/ru-RU/reference/compatibility.md) и
[Установка](../localized/ru-RU/getting-started/installation.md).

## Возможности продукта

**Сеансы.** Сеанс планируется по `LaunchSpec` (флаги CLI, JSON-файл,
профиль сеанса или API), проверяется без побочных эффектов (`/plan`), затем
запускается, отслеживается по переходам `SessionState` и завершается. Остановка
сеанса принудительно завершает OMSI, чтобы OMSI не могла перезаписать файлы,
которые предстоит восстановить. См. [Жизненный цикл сеанса](../localized/ru-RU/concepts/session-lifecycle.md).

**Транзакции и восстановление после сбоя.** Каждое переопределение конфигурации
действует только в рамках сеанса. OmsiLaunch создаёт снимок, журналирует,
применяет, проверяет и восстанавливает каждый изменяемый файл, в том числе после
аварийного завершения (`/recovery-status`, `/recover`,
`RecoverPendingAsync`). Постоянное редактирование конфигурации не предусмотрено. См.
[Транзакции и восстановление после сбоя](../localized/ru-RU/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Эталонный интерфейс поверх того же публичного API,
без собственной логики OMSI: обнаружение, планирование, запуск сеансов и команды
в режиме клиента для работающего сеанса. См. [справочник CLI](../localized/ru-RU/reference/cli.md)
и [примеры CLI](../localized/ru-RU/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** Хост подсистемы Windows для ярлыков. Он принимает ту же
командную строку без окна консоли и сообщает об ошибках в окнах сообщений (message box).
См. [OmsiLaunchW.exe](../localized/ru-RU/reference/omsilaunchw.md).

**Значок в трее Windows.** Каждый сеанс владельца показывает значок в области
уведомлений с окном состояния и действием «End session» (завершить сеанс). Это
действие идёт тем же путём остановки, что и `session stop`. См. [Значок в трее Windows](../localized/ru-RU/reference/windows-tray.md).

**Профили сеанса.** Декларативные пакеты `profile.yaml`
(`omsilaunch.session-profile/v1`) в каталоге
`.omsilaunch\session-profiles\<id>\`, благодаря которым авторы контента могут
распространять воспроизводимые сеансы, запускаемые одной командой. См.
[Профили сеанса](../localized/ru-RU/reference/session-profiles.md).

**Публичный API и runtime-управление.** `OmsiLaunch.Api` (`IOmsiLaunch`) —
предпочтительная поверхность продукта. Runtime-операции, такие как время, погода,
карта, камера, расписание, транспортные средства, люди, переменные скриптов и
текстуры D3D, действуют в рамках сеанса, проверяются по профилю сборки и
адресуются через непрозрачные семантические handle, а не через нативные указатели.
Размер результатов ограничен runtime-слотом 64 KiB. См. [справочник публичного API](../localized/ru-RU/reference/public-api.md) и
[Runtime-управление](../localized/ru-RU/reference/runtime-control.md).

**Локальное управление.** Именованный канал (named pipe) для каждой установки,
привязанный к активному `session_id`, позволяет другим процессам того же
пользователя читать состояние и события, останавливать сеанс и выполнять
публичные runtime-операции. См.
[Локальное управление / IPC](../localized/ru-RU/reference/local-control.md).

**Возможности.** Каждая возможность (capability) и каждая публичная
runtime-операция занесены в каталог с указанием стабильности. Экспериментальные
и недоступные возможности перечислены, а не скрыты (`OmsiLaunch.exe capabilities`). См.
[Возможности](../localized/ru-RU/reference/capabilities.md).

**Постоянный плагин.** Замыкание внутрипроцессного плагина (набор его файлов)
устанавливается один раз в `plugins\OmsiLaunch.*`. Перед каждым запуском оно
сверяется с записями SHA-256 в `release-manifest.json`. Сторонние плагины никогда
не затрагиваются. См.
[Модель постоянного плагина](../localized/ru-RU/concepts/permanent-plugin.md).

## Быстрый старт

Распакуйте релизный пакет в корневой каталог установки OMSI 2, затем выполните
следующие команды из этого каталога:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Пока этот сеанс работает, из второй консоли в том же каталоге можно запросить
его состояние или завершить его:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Пошаговое руководство: [Первый сеанс](../localized/ru-RU/getting-started/first-session.md). Для
интеграторов .NET: [Быстрый старт с публичным API](../localized/ru-RU/getting-started/api-quick-start.md).

## Документация

Полный указатель документации находится в [`docs/localized/ru-RU/README.md`](../localized/ru-RU/README.md).
Английские (US) страницы в `docs/` являются каноническими и нормативными.

| Тема | Страница |
| --- | --- |
| Публичный API | [docs/localized/ru-RU/reference/public-api.md](../localized/ru-RU/reference/public-api.md) |
| CLI | [docs/localized/ru-RU/reference/cli.md](../localized/ru-RU/reference/cli.md) |
| Примеры CLI | [docs/localized/ru-RU/reference/cli-examples.md](../localized/ru-RU/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/ru-RU/reference/omsilaunchw.md](../localized/ru-RU/reference/omsilaunchw.md) |
| Значок в трее Windows | [docs/localized/ru-RU/reference/windows-tray.md](../localized/ru-RU/reference/windows-tray.md) |
| Профили сеанса | [docs/localized/ru-RU/reference/session-profiles.md](../localized/ru-RU/reference/session-profiles.md) |
| Runtime-управление | [docs/localized/ru-RU/reference/runtime-control.md](../localized/ru-RU/reference/runtime-control.md) |
| Локальное управление / IPC | [docs/localized/ru-RU/reference/local-control.md](../localized/ru-RU/reference/local-control.md) |
| Возможности | [docs/localized/ru-RU/reference/capabilities.md](../localized/ru-RU/reference/capabilities.md) |
| Ошибки и коды выхода | [docs/localized/ru-RU/reference/errors.md](../localized/ru-RU/reference/errors.md), [docs/localized/ru-RU/reference/exit-codes.md](../localized/ru-RU/reference/exit-codes.md) |
| Упаковка | [docs/localized/ru-RU/reference/packaging.md](../localized/ru-RU/reference/packaging.md) |
| Известные ограничения | [docs/localized/ru-RU/reference/known-limitations.md](../localized/ru-RU/reference/known-limitations.md) |
| Статус проверки в runtime | [docs/localized/ru-RU/status/runtime-validation-status.md](../localized/ru-RU/status/runtime-validation-status.md) |

### Документация на других языках

Документация переведена на 14 локалей в каталоге
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). Переводы
выполнены на основе канонических английских (US) страниц и механически
сверены с ними. Редакторская вычитка носителями языка не входила в выпуск Beta 4
и может последовать после публикации. Если перевод расходится с английской
страницей, приоритет имеет английская страница.

## Ограничения и открытые вопросы

Ниже перечислены наиболее важные ограничения. Полный список приведён в разделе
[Известные ограничения](../localized/ru-RU/reference/known-limitations.md).

- **Steam.** Steam LAA имеет статус `PARTIAL`: игровой процесс, чтение, команды и остановка
  требуют подлинной установки Steam и не проверены в runtime.
- **Экспериментально в этой бета-версии:** запуск с последнего состояния карты (`/last`)
  и явное указание даты, времени, года или погоды при запуске закрыты статически
  и ожидают runtime-проверки. **Недоступно:** назначение транспортного средства
  игрока при запуске; запрос этой функции делает план неисполнимым, а не
  игнорируется молча.
- **Запись в runtime ограничена.** `weather.set`, `weather.metar.set`, запись строковых
  переменных и триггеры скриптов экспериментальны до проверки в runtime; запись
  календаря и перемещение транспортных средств недоступны. Изменения в runtime не
  журналируются и не восстанавливаются.
- **Время жизни handle.** Для обнаружения устаревших handle дорожных транспортных
  средств и людей, исчезающих естественным образом (RV-002), нет безопасного способа
  воспроизведения в runtime; оно проверено только офлайн-тестами.
- **Постраничные результаты.** Runtime-списки выдаются постранично с помощью `cursor` и
  `limit` и продолжаются с `next_cursor`; страница, не помещающаяся в почтовый ящик
  (mailbox) 64 KiB, сокращается и помечается `truncated=true`.
- **Остановка по умолчанию принудительная.** OMSI завершается принудительно, её
  собственная процедура завершения не выполняется, и несохранённое состояние OMSI
  теряется. Необязательное кооперативное завершение (`/cooperative-shutdown`,
  экспериментально) сначала выполняет процедуру выхода OMSI.
- **Модель доверия в пределах одного пользователя.** Любой процесс того же
  пользователя Windows может обратиться к локальной плоскости управления.

## Загрузка

Загрузите **`OmsiLaunch-0.1.0-beta4.zip`** и соответствующий файл `.sha256` со
[страницы Releases](https://github.com/lmonteirotech/OmsiLaunch/releases). Распакуйте
архив непосредственно в поддерживаемый корневой каталог OMSI. Пакет содержит контроллер
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), его зависимости, замыкание постоянного
плагина с `release-manifest.json`, ресурсы заставки, пример сеанса и
офлайн-документацию в `.omsilaunch\docs\`. Структура пакета и чистое
удаление: [Упаковка](../localized/ru-RU/reference/packaging.md).

## Сборка из исходного кода

Требования:

- Windows 10 или новее, x64.
- .NET 6 SDK, а также среды выполнения .NET 6 x64 и x86 для запуска тестов.
- Visual Studio с рабочей нагрузкой MSBuild C++ (набор инструментов платформы `v145`) и
  Windows 10 SDK для трёх нативных проектов: `OmsiLaunch.Native.x86`
  (Win32), а также `OmsiLaunch.Bootstrapper` и `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` содержит управляемые проекты и наборы тестов. Единая
точка входа для офлайн-проверки собирает всё и запускает все офлайн-наборы тестов;
она никогда не запускает OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` пропускает нативные проекты и регрессионную проверку упаковки.
`-SkipDocs` пропускает гейты документации. Результаты сборки помещаются в `artifacts\`,
этот каталог не отслеживается. `tools\New-ReleasePackage.ps1` подготавливает, хеширует, проверяет
и сжимает релизный пакет из существующей сборки. См.
[Упаковка](../localized/ru-RU/reference/packaging.md) и
[Тестирование и проверка](../project/TESTING-AND-VALIDATION.md).

| Путь | Содержимое |
| --- | --- |
| `src/` | Библиотеки продукта: API, ядро, конфигурация, контент, взаимодействие с OMSI (interop), процессы, плагин, профиль сборки, нативная граница x86 |
| `tools/` | CLI и хост Windows (`OmsiLaunch.Cli`), нативные shim-загрузчики (`OmsiLaunch.Bootstrapper`), офлайн-хост для тестов, скрипты упаковки и проверки, инструменты локализации |
| `tests/` | Наборы модульных, интеграционных тестов, тестов профилей, пользовательского интерфейса Windows и документации |
| `docs/` | Каноническая документация и её переводы в `docs/localized/` |
| `examples/` | Примеры LaunchSpec и сеансов |
| `assets/` | Фирменная символика, значки и ресурсы пакета |
| `third_party/` | Сведения о происхождении кода из вышестоящих проектов |

Сводки для сопровождающих: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Сообщество и лицензия

OmsiLaunch — проект с открытым исходным кодом, ориентированный на сообщество. Он не зависит от
других лаунчеров OMSI, и совместимые инструменты сообщества могут строиться на его основе.

OmsiLaunch распространяется по лицензии [LGPL-3.0-only](../../LICENSE). Сведения о происхождении
включённого исходного кода и применимые уведомления приведены в
[уведомлениях о стороннем ПО](../../THIRD-PARTY-NOTICES.md).
