# Установка

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Перевод [исходной страницы на английском языке](../../../getting-started/installation.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице описано, что требуется для OmsiLaunch `0.1.0-beta4`, какую сборку OMSI он поддерживает, как релизный пакет устанавливается в корневой каталог установки OMSI и как проверить установку с помощью `/version` и `/plan` перед запуском сеанса. Состав пакета описан в разделе [упаковка](../reference/packaging.md); первый запуск описан на странице [первый сеанс](first-session.md).

<a id="requirements"></a>
## Требования

| Требование | Подробности | Что происходит при невыполнении |
|---|---|---|
| Windows 10 или новее, 64-разрядная | Контроллер проверяет `Environment.OSVersion.Version.Major >= 10`, 64-разрядную (x64) операционную систему и x64-процесс. | План неисполним с `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (или `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), код выхода `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` требует `Microsoft.NETCore.App` 6.0 и `Microsoft.WindowsDesktop.App` 6.0 (индикатор в трее использует Windows Forms). Shim-загрузчики находят среду выполнения с помощью `nethost.dll`. | `OmsiLaunch.exe` завершается с кодом shim-загрузчика `102`..`106` до какого-либо вывода; `OmsiLaunchW.exe` показывает `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` требует `Microsoft.NETCore.App` 6.0 для x86, потому что плагин работает внутри 32-разрядного `Omsi.exe`. Это требование также удовлетворяет пакет x86 .NET 6 Desktop Runtime. | Плагин не запускается внутри OMSI; сеанс не достигает состояния `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), код выхода `1`, файлы восстановлены. |
| Установленная OMSI 2 | 32-разрядный `Omsi.exe` с версией OMSI 2 (2.x), а также `maps\` и `Vehicles\` в корневом каталоге. Конкретный хеш исполняемого файла не требуется; OMSI 2.3.004 — известная раскладка, проверенная в runtime, а на других сборках каждая возможность определяется и проверяется в процессе. | `OL_E_UNSUPPORTED_BUILD`; план неисполним, код выхода `1`. Возможность, недоступная на сборке, завершается с `OL_E_CAPABILITY_UNRESOLVED`. См. [совместимость](../reference/compatibility.md). |
| Корневой каталог установки доступен для записи | Транзакция записывает `.omsilaunch\`, overlay (временная замена файла на время сеанса) в `GUI\`, `Texture\` и `options.cfg` и затем восстанавливает их; корневой каталог должен быть доступен для записи текущему пользователю (избегайте `Program Files` без соответствующих прав). | `OL_E_INSTALLATION_NOT_WRITABLE`, код выхода `1`. |
| Один пользователь, один владелец на установку | Аренда установки (lease) `Local\OmsiLaunch.Installation.<sha256(root)>` и управляющий канал (pipe) действуют в пределах сеанса входа в систему. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, код выхода `7`. |

Обе среды выполнения загружаются отдельно с сайта Microsoft; установите x64 Desktop Runtime и x86 Runtime (или x86 Desktop Runtime) для .NET 6. Никакие другие компоненты не требуются. Никакие данные не покидают компьютер.

<a id="confirm-the-omsi-build"></a>
## Проверка сборки OMSI

Замените `<OMSI_PATH>` на свой каталог OMSI 2 (например, `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

Хеш должен совпадать с одним из двух, приведённых выше. После установки `OmsiLaunch.exe profiles` выводит тот же список вместе со статусом проверки.

<a id="install-the-package"></a>
## Установка пакета

1. Загрузите `OmsiLaunch-0.1.0-beta4.zip` и `OmsiLaunch-0.1.0-beta4.zip.sha256`; проверьте контрольную сумму (результат `Get-FileHash` должен совпадать со значением в файле `.sha256`).
2. Распакуйте архив **непосредственно в корневой каталог установки OMSI** (каталог, в котором находится `Omsi.exe`). Структура архива рассчитана на этот корневой каталог:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` и остальные сборки контроллера `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` — в корне;
   - замыкание постоянного плагина (набор его файлов) `plugins\OmsiLaunch.*` (9 файлов) — рядом с уже установленными плагинами, которые никогда не затрагиваются;
   - `.omsilaunch\` с ресурсами заставки (splash), офлайн-документацией и примерами.
3. Оставьте `release-manifest.json` рядом с `OmsiLaunch.exe`. Именно он позволяет при каждом запуске проверять установленные файлы плагина по SHA-256 (`plugin.integrity.reference = manifest`); без него проверяются только наличие файлов и их внутренняя согласованность (`plugin.integrity.reference = self`).
4. Не перемещайте и не переименовывайте ничего в `plugins\OmsiLaunch.*` и не размещайте двоичные файлы плагина в `.omsilaunch\`.

Обновление выполняется так же: распакуйте новый пакет поверх старых файлов, пока ни один сеанс не запущен и нет незавершённого восстановления после сбоя (`OmsiLaunch.exe /recovery-status`). Хеши плагина и манифест всегда должны быть из одного и того же пакета (иначе `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Проверка

Запустите из корневого каталога OMSI (аргумент установки по умолчанию равен каталогу, в котором находится `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Ожидаемый результат: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; код выхода `0`. Код выхода `102`..`106` означает, что отсутствует x64-среда выполнения .NET 6 или пакет неполон.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Ожидаемый результат: поддерживаемые хеши, затем карты, обнаруженные в этой установке; код выхода `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Ожидаемый результат: `Plan: READY profile=Omsi23004` (на известной раскладке; в остальных случаях `unrecognized`) и код выхода `0` (можно использовать любой идентификатор карты из `/list:Maps`; индекс точки входа должен быть одним из индексов, которые предоставляет эта карта, см. `/list:Entrypoints /map:<identity>`). С `--json` план содержит `TouchedFiles` (overlay заставки), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED`, а для нативных возможностей запуска на известной раскладке — `RESOLVED_KNOWN_LAYOUT`), `Diagnostics` (включая `plugin.integrity.reference`) и `IsRunnable`. При `Plan: NOT RUNNABLE` с кодом выхода `1` причина указана в `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). Планирование никогда не запускает OMSI и никогда не записывает в установку.

<a id="where-things-live-afterwards"></a>
## Что где находится после установки

| Путь | Содержимое |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Трассировка хоста для каждого сеанса (хранятся 50 последних сеансов) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Лог индикатора в трее |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Присутствуют только пока есть незавершённая транзакция; см. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Ваши предопределённые профили сеанса; см. [профили сеанса](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Ресурсы управляемой заставки |
| `<root>\.omsilaunch\docs\` | Эта документация в офлайн-виде (начните с `README.md`; справочник CLI — `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Пример LaunchSpec и профиля сеанса |

<a id="uninstall"></a>
## Удаление

Остановите все сеансы, выполните `OmsiLaunch.exe /recovery-status` (и `/recover`, если есть незавершённое восстановление), затем удалите файлы продукта в корне, `plugins\OmsiLaunch.*` и `.omsilaunch\`. Подробности — в разделе [упаковка](../reference/packaging.md).

<a id="next"></a>
## Далее

[Первый сеанс](first-session.md) · [Справочник CLI](../reference/cli.md) · [известные ограничения](../reference/known-limitations.md)
