# Упаковка и структура релиза

<!-- l10n: source=reference/packaging.md source-sha256=8d5d5b27225188339094529a2435a976635969dd53d614c53d768169087e7ffa -->
> Перевод [исходной страницы на английском языке](../../../reference/packaging.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

На этой странице описан релизный пакет OmsiLaunch `0.1.0-beta4`: что создаёт `tools\New-ReleasePackage.ps1`, поля `release-manifest.json`, как контроллер использует манифест во время работы для проверки замыкания постоянного плагина (набора его файлов), как пакет устанавливается в корневой каталог установки OMSI и удаляется из него, что содержит каталог `.omsilaunch` после использования, а также скрипты проверки (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). Идентичность продукта берётся из `OmsiLaunch.Version.props`. Шаги установки для пользователей приведены на странице [установка](../getting-started/installation.md); роль замыкания плагина во время работы описана на странице [постоянный плагин](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Идентичность продукта (`OmsiLaunch.Version.props`)

| Свойство | Значение | Где используется |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | `product` в манифесте, `ProductName` в Windows |
| `OmsiLaunchCompanyName` | `LMonteiro` | `CompanyName` в Windows |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/lmonteirotech/OmsiLaunch` | `LegalCopyright` в Windows |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | `product_version` в манифесте, `ProductVersion` в Windows, информационная версия сборки (`/version`), имя публичного ZIP |
| `OmsiLaunchManagedVersion` | `0.1.0` | Основа версии управляемых сборок |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Версия сборки и версия файла Windows |
| `OmsiLaunchPackageAlias` | `current` | `package_alias` в манифесте, имя промежуточного каталога и ZIP-псевдонима |

`Directory.Build.props` устанавливает `InformationalVersion` равным `OmsiLaunchProductVersion` без ревизии исходного кода, поэтому `OmsiLaunch.exe /version` выводит ровно `0.1.0-beta4`.

<a id="build-toolsnew-releasepackageps1"></a>
## Сборка (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (выходной каталог по умолчанию — `artifacts\release`) собирает пакет из уже собранных артефактов. Запись в `artifacts\release`, когда `OmsiLaunch-<product_version>.zip` уже существует, отклоняется, если не указан `-AllowOverwritePublished`; пакеты-кандидаты помещаются в другой каталог (офлайн-проверка использует `artifacts\candidate\post-round-a`).

Перед размещением каждый результат сборки проверяется на устаревание.

- **`OmsiLaunch.Native.x86.dll`: по содержимому, а не по метке времени.** Нативная сборка записывает `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (цель `WriteOmsiLaunchNativeBuildReceipt` в `.vcxproj`): SHA-256 созданной DLL (`output=`) и каждого исходного файла, из которого она собрана (`source=<sha256>|<path>`: `.cpp`, `.rc`, `.vcxproj` и `OmsiLaunch.Version.props`). Упаковка отклоняет DLL, если её хеш не совпадает с записанным выходным (`does not match its build receipt`: устаревшая или посторонняя копия, какой бы ни была её метка времени), если записанный исходный файл изменился (`Native source changed after the recorded build`), если нативный исходный файл не учтён в квитанции или если квитанция отсутствует.
- **Прослойки и управляемые сборки: по метке времени.** Каждая из них должна быть не старше исходных файлов своего проекта (`.cpp`/`.rc`/`.vcxproj` каждой прослойки; собственный проект каждой управляемой сборки). Устаревший входной файл прерывает упаковку с `Stale build artifact`.

Затем пакет собирается в совершенно новом промежуточном каталоге с уникальным именем (`.staging-<guid>` в выходном каталоге), поэтому ни один файл от предыдущего запуска не может попасть в замыкание. Предыдущая папка `OmsiLaunch-current` и архивы заменяются только после прохождения всех перечисленных ниже проверок.

| Источник | Назначение в пакете |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | корень |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| `assets\splash\*.bmp` из CLI (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | корень |
| каждый файл в `docs\`, кроме `docs\localized\` (английская документация, та же структура каталогов) | `.omsilaunch\docs\` (поэтому существует `.omsilaunch\docs\reference\cli.md` — путь, который выводит справка CLI; аудит документации BUG-08). Ссылки из `docs\README.md` на сводки в корне репозитория (`PUBLIC-API.md` и другие) работают только в исходном репозитории. |
| `docs\localized\LOCALIZATION-MANIFEST.md` и `docs\localized\<locale>\**` для каждой локали, указанной в этом манифесте (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` и `ja-JP`) | `.omsilaunch\docs\localized\` (та же структура); отсутствие указанной локали прерывает скрипт |

Затем скрипт вычисляет хеш каждого размещённого файла, записывает `release-manifest.json` в корень пакета в кодировке UTF-8 **без** BOM (результат больше не зависит от редакции PowerShell), запускает `Test-ReleasePackageIntegrity.ps1` для промежуточного каталога, повторно сравнивает каждый размещённый файл плагина и `OmsiLaunch.Native.x86.dll` с результатом сборки, сжимает промежуточный каталог, **распаковывает архив в новый временный каталог и проверяет распакованное замыкание по тому же манифесту** (манифест в архиве должен быть побайтово идентичен проверенному), затем публикует промежуточный каталог как `OmsiLaunch-current`, архив — как `OmsiLaunch-current.zip`, копирует его в `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) и записывает `OmsiLaunch-0.1.0-beta4.zip.sha256`, содержащий `<SHA-256>  <file name>`. Отсутствие любого артефакта прерывает скрипт. Скрипт не выполняет сборку; сначала следует запустить `Invoke-OfflineValidation.ps1` (или отдельные шаги `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Структура пакета

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

Продукту принадлежат только корневые файлы продукта, `plugins\OmsiLaunch.*` и `.omsilaunch\`. Сторонние плагины в `plugins\` OmsiLaunch никогда не перечисляет, не копирует, не хеширует, не удаляет и не восстанавливает.

## `release-manifest.json`

| Поле | Тип | Значение |
|---|---|---|
| `product` | строка | `OmsiLaunch` |
| `product_version` | строка | `0.1.0-beta4` |
| `package_alias` | строка | `current` |
| `control_protocol` | строка | `0.1`; должно совпадать с `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | строка | `OMSI 2` |
| `known_layouts` | object[] | `build_id` и `code_sha256` каждой известной раскладки (`Omsi23004`, `13ec110a...c028`); быстрый путь, а не требование |
| `configuration` | строка | `Release` или `Debug` |
| `generated_utc` | строка | Время сборки в формате ISO-8601 |
| `files[]` | object[] | `path` (прямые косые черты, относительно корня пакета), `bytes`, `sha256` (шестнадцатеричные цифры в верхнем регистре) для каждого файла пакета |

Манифест — это данные, а не исполняемая политика: контроллер читает только записи `plugins/`. Читатель принимает файл как с UTF-8 BOM, так и без него (манифесты, записанные Windows PowerShell 5.1 до этого исправления, содержат BOM).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Использование манифеста во время работы (целостность плагина)

Перед каждым планированием и запуском `OmsiLaunchService.LoadArtifacts` строит ожидаемое замыкание плагина (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. Контроллер ищет `release-manifest.json` рядом с `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Если он есть, `ReleaseManifest.TryReadPluginHashes` извлекает хеши `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID`, если файл не удаётся прочитать как манифест).
2. Для каждого установленного файла `<root>\plugins\OmsiLaunch.*` вычисляется хеш (SHA-256), который сравнивается:
   - при наличии манифеста — с хешем из манифеста; записывается диагностическое сообщение плана `plugin.integrity.reference = manifest`. Файл отсутствует → `OL_E_PERMANENT_PLUGIN_MISSING`; файл есть, но не указан в манифесте → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; хеш отличается → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - без манифеста (раскладка разработки или установка без манифеста) — можно проверить только наличие и самосогласованность относительно копии из пакета рядом с контроллером; `plugin.integrity.reference = self`.
3. Ошибка делает план неисполнимым (`OL_E_RUNTIME_ARTIFACT_MISSING` с подробностями) или приводит к отклонению запуска (код выхода `7`).

Сеанс никогда не размещает, не включает в снимок, не восстанавливает и не удаляет файлы плагина; замыкание — постоянная часть установки. x86-библиотека `OmsiLaunch.Native.x86.dll` загружается только из `plugins\`; управляемые сборки объявляют `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Установка в корневой каталог OMSI

1. Проверьте архив: сравните `OmsiLaunch-0.1.0-beta4.zip` с `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Распакуйте архив **непосредственно в корневой каталог установки OMSI** (каталог, содержащий `Omsi.exe`). При этом размещаются корневые файлы, `plugins\OmsiLaunch.*` (рядом со сторонними плагинами, если они есть) и `.omsilaunch\`.
3. Оставьте `release-manifest.json` рядом с `OmsiLaunch.exe`: он включает проверку целостности плагина по манифесту. Манифест и двоичные файлы должны быть из одного пакета: новые двоичные файлы поверх более старого манифеста (или наоборот) приводят к тому, что каждый запуск завершается ошибкой `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` теперь копирует манифест вместе с файлами продукта; раньше он его пропускал, из-за чего рядом с более новыми двоичными файлами оставался старый манифест (Round A RA-007).
4. Проверьте согласованность в режиме только для чтения с помощью `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` должно быть `true`.
5. Не перемещайте двоичные файлы плагина в `.omsilaunch\` и не переименовывайте `plugins\OmsiLaunch.*`.
6. Проверьте установку с помощью `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` и `/plan` (см. [первый сеанс](../getting-started/first-session.md)).

Существующие файлы `.omsilaunch\assets\splash\*.bmp` сеанс никогда не перезаписывает (явно управляемый набор ресурсов сохраняется); их перезапись при распаковке нового пакета — намеренное действие пользователя.

<a id="the-omsilaunch-directory-after-use"></a>
## Каталог `.omsilaunch` после использования

| Путь | Кем создаётся | Время жизни |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | пакетом или копируется при первом сеансе с управляемой заставкой | постоянно |
| `docs\`, `examples\` | пакетом | постоянно |
| `session-profiles\<id>\profile.yaml` | пользователем | постоянно; см. [профили сеанса](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | каждым сеансом | хранится для 50 самых новых сеансов; более старые файлы с префиксом сеанса удаляются при запуске нового сеанса |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, средствами проверки | та же политика хранения (префикс сеанса) |
| `diagnostics\tray-host.log` | индикатором в трее | постоянно, с дозаписью |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | постоянно (без префикса сеанса) |
| `journal.json` | транзакцией | существует от `Prepared` до `Restored`; оставшийся файл означает, что ожидается восстановление после сбоя (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | транзакцией | снимки затронутых файлов; удаляются после восстановления |

Никакие данные не покидают компьютер. См. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Удаление

1. Убедитесь, что ни один сеанс не запущен (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) и нет незавершённого восстановления (`OmsiLaunch.exe /recovery-status`; выполните `/recover`, если `pending` равно `true`), чтобы файлы OMSI уже были восстановлены.
2. Удалите `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Другие плагины не трогайте.
3. Удалите корневые файлы продукта, перечисленные в структуре выше (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Удалите `.omsilaunch\` (при этом удаляются ваши профили сеанса и диагностика). Никогда не удаляйте его, пока существует `journal.json`.

Завершённый сеанс восстанавливает каждый файл, которым владел, поэтому дополнительная очистка не требуется. Файлы, которые сама OMSI записывает во время работы (например, `[last_map]` в `options.cfg`, кеши, `laststn.osn`, логи), — это обычное состояние OMSI, и они не откатываются; см. [транзакции и восстановление после сбоя](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Скрипты проверки

| Скрипт | Назначение | Затрагивает OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Собирает `OmsiLaunch.sln` с предупреждениями, трактуемыми как ошибки, и три нативных проекта (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) через MSBuild, затем запускает все офлайн-наборы тестов: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` и `OmsiLaunch.DocumentationTests` (если не пропущен), затем регрессионный тест упаковки `Test-PackagingPipeline.ps1` (пропускается при `-SkipNative`). Выводит `OFFLINE VALIDATION PASSED`/`FAILED`. | Нет |
| `tools\New-ReleasePackage.ps1` | Защита от устаревших файлов, размещение, манифест, самопроверка целостности, ZIP, контрольная сумма (см. выше). | Нет |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Проверяет, что манифест перечисляет ровно файлы пакета с совпадающими размером и SHA-256, что обязательное замыкание (три исполняемых файла, контроллер, девять файлов постоянного плагина, включая `OmsiLaunch.Native.x86.dll`) присутствует и что конфигурация — `Release`. С `-InstallationRoot` сравнивает файлы продукта в установке с пакетом **только для чтения**. Код выхода `0` = согласовано. | Нет (только чтение) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Создаёт пакет-кандидат из чистого промежуточного каталога в `artifacts\candidate\post-round-a` и требует, чтобы размещённое замыкание и повторно распакованный архив прошли проверку целостности. Доказывает, что гейт целостности отклоняет подменённую DLL, подменённый или старый `Native.x86`, устаревшую копию плагина, удалённый перечисленный файл, неожиданный файл, изменённый или некорректный хеш в манифесте, дублирующиеся записи (точные, по регистру, по разделителю), родительские и абсолютные пути и некорректный JSON; что упаковщик отклоняет старый `Native.x86`, помещённый в результаты сборки (даже с более новой меткой времени), и квитанцию, исходные файлы которой изменились; и что опубликованный архив никогда не перезаписывается. Результаты сборки восстанавливаются побайтово. Запускается из `Invoke-OfflineValidation.ps1`. | Нет |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Распаковывает ZIP в `artifacts\release\identity-verification`, проверяет `product`/`product_version`/`package_alias`, а также `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` каждого `.exe`/`.dll`, кроме `nethost.dll` и `YamlDotNet.dll`, `InternalName`/`OriginalFilename` обеих прослоек и наличие встроенного значка в `OmsiLaunch.exe`. | Нет |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Проверяет упакованный исполняемый файл Release на реальной установке: манифест должен иметь конфигурацию `Release`, не содержать путей `Debug` или `runtime/plugin/` и устанавливать `plugins/OmsiLaunch.*`; все четыре ресурса заставки должны существовать. Выполняет три случая `/plan` (управляемая заставка по умолчанию, управляемая заставка с пользовательскими ресурсами, `/splash:Unset`). С `-RunOmsi` (требует `-InstallPackage`) запускает каждый случай с `/observe-seconds`, наблюдает за `GUI\NewSplashscreen_ENG.bmp` и `GUI\NewSplashscreen_PTB.bmp` во время сеанса и проверяет точное восстановление, отсутствие `Omsi.exe`, отсутствие `journal.json`, код выхода `0`, неизменность хешей сторонних плагинов и неизменность набора файлов постоянного плагина. Записывает `.omsilaunch\diagnostics\release-presentation-validation.json`. | Да, с `-RunOmsi` (в рамках сеанса, с восстановлением) |

Оба скрипта `Test-*` читают `OmsiLaunch.Version.props`, чтобы узнать ожидаемую версию.
