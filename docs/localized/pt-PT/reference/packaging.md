# Empacotamento e estrutura da versão

<!-- l10n: source=reference/packaging.md source-sha256=6fff256ee354401567ab45240b0f155822e6321212dc63ea33ed6bc938b945ad -->
> Tradução da [página original em inglês](../../../reference/packaging.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página descreve o pacote de lançamento do OmsiLaunch `0.1.0-beta4`: o que `tools\New-ReleasePackage.ps1` produz, os campos de `release-manifest.json`, como o controlador usa o manifesto em runtime para verificar o conjunto de ficheiros do plugin permanente, como o pacote é instalado numa raiz de instalação do OMSI e removido dela, o que o diretório `.omsilaunch` contém após utilização e os scripts de validação (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). A identidade do produto provém de `OmsiLaunch.Version.props`. Os passos de instalação para utilizadores estão em [instalação](../getting-started/installation.md); o papel em runtime do conjunto de ficheiros do plugin está em [plugin permanente](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Identidade do produto (`OmsiLaunch.Version.props`)

| Propriedade | Valor | Utilizado para |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | `product` do manifesto, `ProductName` do Windows |
| `OmsiLaunchCompanyName` | `LMonteiro` | `CompanyName` do Windows |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/OmsiModsBR/OmsiLaunch` | `LegalCopyright` do Windows |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | `product_version` do manifesto, `ProductVersion` do Windows, versão informativa do assembly (`/version`), nome do ZIP público |
| `OmsiLaunchManagedVersion` | `0.1.0` | Base da versão dos assemblies geridos |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Versão do assembly e versão de ficheiro do Windows |
| `OmsiLaunchPackageAlias` | `current` | `package_alias` do manifesto, pasta de preparação e nome do ZIP com alias |

`Directory.Build.props` define `InformationalVersion` como `OmsiLaunchProductVersion` sem revisão da fonte, pelo que `OmsiLaunch.exe /version` imprime exatamente `0.1.0-beta4`.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (saída predefinida `artifacts\release`) prepara um pacote a partir de artefactos já compilados. A escrita em `artifacts\release` quando `OmsiLaunch-<product_version>.zip` já existe é recusada, a menos que seja indicado `-AllowOverwritePublished`; os pacotes candidatos vão para outro diretório (a validação offline usa `artifacts\candidate\post-round-a`).

Antes da preparação, verifica-se se cada resultado de build está desatualizado.

- **`OmsiLaunch.Native.x86.dll`: pelo conteúdo, não pela data/hora.** A build nativa escreve `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (alvo `WriteOmsiLaunchNativeBuildReceipt` no `.vcxproj`): o SHA-256 da DLL que produziu (`output=`) e de cada ficheiro-fonte a partir do qual foi compilada (`source=<sha256>|<path>`: o `.cpp`, o `.rc`, o `.vcxproj` e `OmsiLaunch.Version.props`). O empacotamento recusa a DLL quando o seu hash não é o resultado registado (`does not match its build receipt`: uma cópia desatualizada ou alheia, seja qual for a sua data/hora), quando uma fonte registada foi alterada (`Native source changed after the recorded build`), quando uma fonte nativa não está coberta pelo recibo ou quando o recibo está em falta.
- **Shims e assemblies geridos: pela data/hora.** Cada um não pode ser mais antigo do que as fontes do seu próprio projeto (`.cpp`/`.rc`/`.vcxproj` de cada shim; o próprio projeto de cada assembly gerido). Uma entrada desatualizada aborta o empacotamento com `Stale build artifact`.

O pacote é depois montado num diretório de preparação totalmente novo e com nome único (`.staging-<guid>` no diretório de saída), para que nenhum ficheiro de uma execução anterior possa entrar no conjunto. A pasta `OmsiLaunch-current` anterior e os ficheiros ZIP anteriores só são substituídos depois de todas as verificações abaixo terem sido aprovadas.

| Origem | Destino no pacote |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`, `nethost.dll` | `OmsiLaunch.exe`, `nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\` (x64): `OmsiLaunch.Controller.dll`, `.deps.json`, `.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Configuration.dll`, `OmsiLaunch.Content.dll`, `OmsiLaunch.Core.dll`, `OmsiLaunch.Process.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `YamlDotNet.dll` | raiz |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json`, `OmsiLaunch.Api.dll`, `OmsiLaunch.Builds.Omsi23004.dll`, `OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| `assets\splash\*.bmp` da CLI (`PTB`, `ENG`, `DEU`, `FRA`) | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`, `THIRD-PARTY-NOTICES.md` | raiz |
| todos os ficheiros em `docs\` exceto `docs\localized\` (a documentação em inglês, com a mesma estrutura de diretórios) | `.omsilaunch\docs\` (para que exista `.omsilaunch\docs\reference\cli.md`, o caminho impresso pelo texto de utilização da CLI; auditoria da documentação BUG-08). As ligações de `docs\README.md` para os resumos na raiz do repositório (`PUBLIC-API.md` e outros) só funcionam no repositório de origem. |
| `docs\localized\LOCALIZATION-MANIFEST.md` e `docs\localized\<locale>\**` para cada locale listado nesse manifesto (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` e `ja-JP`) | `.omsilaunch\docs\localized\` (mesma estrutura); um locale listado que esteja em falta aborta o script |

Em seguida, o script calcula o hash de cada ficheiro preparado, escreve `release-manifest.json` na raiz do pacote em UTF-8 **sem** BOM (o resultado deixa de depender da edição do PowerShell), executa `Test-ReleasePackageIntegrity.ps1` sobre a preparação, volta a comparar cada ficheiro do plugin preparado e `OmsiLaunch.Native.x86.dll` com o respetivo resultado de build, comprime a preparação, **extrai o ficheiro ZIP para um diretório temporário novo e valida o conjunto extraído contra o mesmo manifesto** (o manifesto contido no ZIP tem de ser idêntico byte a byte ao validado), depois publica a preparação como `OmsiLaunch-current` e o ficheiro ZIP como `OmsiLaunch-current.zip`, copia-o para `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) e escreve `OmsiLaunch-0.1.0-beta4.zip.sha256` com o conteúdo `<SHA-256>  <file name>`. Qualquer artefacto em falta aborta o script. O script não compila; deve-se executar primeiro `Invoke-OfflineValidation.ps1` (ou os passos individuais `dotnet build` / MSBuild).

<a id="package-layout"></a>
## Estrutura do pacote

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

Apenas os ficheiros do produto na raiz, `plugins\OmsiLaunch.*` e `.omsilaunch\` pertencem ao produto. Os plugins de terceiros em `plugins\` nunca são enumerados, copiados, sujeitos a hash, removidos nem restaurados pelo OmsiLaunch.

## `release-manifest.json`

| Campo | Tipo | Significado |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`; tem de corresponder a `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | `build_id` e `code_sha256` de cada disposição conhecida (`Omsi23004`, `13ec110a...c028`); um caminho rápido, não um requisito |
| `configuration` | string | `Release` ou `Debug` |
| `generated_utc` | string | Hora da build em ISO-8601 |
| `files[]` | object[] | `path` (barras normais, relativo à raiz do pacote), `bytes`, `sha256` (hexadecimal em maiúsculas) para cada ficheiro empacotado |

O manifesto é um dado, nunca uma política executável: o controlador lê apenas as entradas `plugins/`. O leitor aceita o ficheiro com ou sem BOM UTF-8 (os manifestos escritos pelo Windows PowerShell 5.1 antes desta correção têm BOM).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Utilização do manifesto em runtime (integridade do plugin)

Antes de cada plano e de cada arranque, `OmsiLaunchService.LoadArtifacts` constrói o conjunto esperado de ficheiros do plugin (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. O controlador procura `release-manifest.json` ao lado de `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Quando existe, `ReleaseManifest.TryReadPluginHashes` extrai os hashes de `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID` se não for possível ler o ficheiro como manifesto).
2. É calculado o hash (SHA-256) de cada ficheiro instalado `<root>\plugins\OmsiLaunch.*`, que é comparado:
   - com manifesto: com o hash do manifesto; é registado o diagnóstico do plano `plugin.integrity.reference = manifest`. Ficheiro em falta → `OL_E_PERMANENT_PLUGIN_MISSING`; ficheiro presente mas não listado → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; hash diferente → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - sem manifesto (estrutura de desenvolvimento, ou uma instalação que omitiu o manifesto): só é possível verificar a presença e a autoconsistência em relação à cópia empacotada ao lado do controlador; `plugin.integrity.reference = self`.
3. Uma falha marca o plano como não executável (`OL_E_RUNTIME_ARTIFACT_MISSING` com o detalhe) ou rejeita o arranque (saída `7`).

Os ficheiros do plugin nunca são preparados, incluídos num snapshot, restaurados nem removidos por uma sessão; o conjunto é uma parte permanente da instalação. O `OmsiLaunch.Native.x86.dll` x86 é carregado apenas a partir de `plugins\`; os assemblies geridos declaram `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Instalação na raiz do OMSI

1. Verificar o ficheiro ZIP: comparar `OmsiLaunch-0.1.0-beta4.zip` com `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Extrair o ficheiro ZIP **diretamente para a raiz da instalação do OMSI** (o diretório que contém `Omsi.exe`). Isto coloca os ficheiros da raiz, `plugins\OmsiLaunch.*` (ao lado de quaisquer plugins de terceiros) e `.omsilaunch\`.
3. Manter `release-manifest.json` ao lado de `OmsiLaunch.exe`: permite a verificação da integridade do plugin baseada no manifesto. O manifesto e os binários têm de provir do mesmo pacote: binários novos sobre um manifesto mais antigo (ou o inverso) fazem falhar todos os arranques com `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` copia agora o manifesto juntamente com os ficheiros do produto; antes ignorava-o, o que deixava um manifesto mais antigo ao lado de binários mais recentes (Round A RA-007).
4. Verificar a coerência em modo só de leitura com `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` tem de ser `true`.
5. Não mover os binários do plugin para `.omsilaunch\` e não mudar o nome de `plugins\OmsiLaunch.*`.
6. Verificar com `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` e um `/plan` (ver [primeira sessão](../getting-started/first-session.md)).

Os ficheiros `.omsilaunch\assets\splash\*.bmp` existentes nunca são substituídos por uma sessão (um conjunto de recursos gerido explicitamente persiste); substituí-los extraindo um novo pacote é uma ação deliberada do utilizador.

<a id="the-omsilaunch-directory-after-use"></a>
## O diretório `.omsilaunch` após utilização

| Caminho | Criado por | Duração |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | pacote, ou copiado na primeira sessão com splash gerido | persistente |
| `docs\`, `examples\` | pacote | persistente |
| `session-profiles\<id>\profile.yaml` | utilizador | persistente; ver [perfis de sessão](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | cada sessão | mantido para as 50 sessões mais recentes; os ficheiros mais antigos com prefixo de sessão são eliminados quando uma nova sessão é iniciada |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, harnesses de validação | mesma retenção (prefixo de sessão) |
| `diagnostics\tray-host.log` | indicador na área de notificação | persistente, acrescentado |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | persistente (sem prefixo de sessão) |
| `journal.json` | transação | existe desde `Prepared` até `Restored`; um resto significa que há uma recuperação pendente (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | transação | snapshots dos ficheiros tocados; removidos após o restauro |

Nenhum dado sai da máquina. Ver [transações e recuperação](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Desinstalação

1. Garantir que nenhuma sessão está a executar (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) e que não há nenhuma recuperação pendente (`OmsiLaunch.exe /recovery-status`; executar `/recover` se `pending` for `true`), para que os ficheiros do OMSI já estejam restaurados.
2. Eliminar `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Não tocar nos outros plugins.
3. Eliminar os ficheiros do produto na raiz listados na estrutura acima (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Eliminar `.omsilaunch\` (isto remove os perfis de sessão e os diagnósticos do utilizador). Nunca o eliminar enquanto existir `journal.json`.

Uma sessão concluída restaura todos os ficheiros que lhe pertenciam, pelo que não é necessária mais nenhuma limpeza. Os ficheiros que o próprio OMSI escreve enquanto está a executar (por exemplo, `[last_map]` em `options.cfg`, caches, `laststn.osn`, registos) são o estado normal do OMSI e não são revertidos; ver [transações e recuperação](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Scripts de validação

| Script | Finalidade | Toca no OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Compila `OmsiLaunch.sln` com os avisos tratados como erros e os três projetos nativos (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) através do MSBuild; depois executa todas as suites offline: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` e `OmsiLaunch.DocumentationTests`, exceto se ignorada; por fim, o teste de regressão do empacotamento `Test-PackagingPipeline.ps1` (ignorado com `-SkipNative`). Imprime `OFFLINE VALIDATION PASSED`/`FAILED`. | Não |
| `tools\New-ReleasePackage.ps1` | Proteção contra artefactos desatualizados, preparação, manifesto, autoverificação de integridade, ZIP, checksum (acima). | Não |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifica se o manifesto lista exatamente os ficheiros empacotados com tamanho e SHA-256 correspondentes, se o conjunto obrigatório (três executáveis, o controlador, os nove ficheiros do plugin permanente incluindo `OmsiLaunch.Native.x86.dll`) está presente e se a configuração é `Release`. Com `-InstallationRoot`, compara os ficheiros do produto da instalação com o pacote **em modo só de leitura**. Saída `0` = coerente. | Não (só de leitura) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produz um pacote candidato a partir de uma preparação limpa em `artifacts\candidate\post-round-a` e exige que o conjunto preparado e o ficheiro ZIP novamente extraído passem na verificação de integridade. Prova que o gate de integridade rejeita uma DLL adulterada, um `Native.x86` adulterado ou antigo, uma cópia desatualizada do plugin, um ficheiro listado eliminado, um ficheiro inesperado, um hash do manifesto alterado ou malformado, entradas duplicadas (exatas, por maiúsculas/minúsculas, por separador), caminhos para o diretório pai e absolutos e JSON inválido; que o empacotador rejeita um `Native.x86` antigo colocado no resultado da build (mesmo com uma data/hora mais recente) e um recibo cujas fontes foram alteradas; e que o ficheiro ZIP publicado nunca é substituído. Os resultados de build são restaurados byte a byte. Executado por `Invoke-OfflineValidation.ps1`. | Não |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Extrai o ZIP para `artifacts\release\identity-verification`, verifica `product`/`product_version`/`package_alias` e verifica `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` de cada `.exe`/`.dll` exceto `nethost.dll` e `YamlDotNet.dll`, o `InternalName`/`OriginalFilename` de ambos os shims e que `OmsiLaunch.exe` tem um ícone incorporado. | Não |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Valida o executável Release empacotado contra uma instalação real: o manifesto tem de ser `Release`, não pode conter caminhos `Debug` nem `runtime/plugin/` e tem de instalar `plugins/OmsiLaunch.*`; os quatro recursos do splash têm de existir. Executa três casos `/plan` (predefinição gerida, recursos personalizados geridos, `/splash:Unset`). Com `-RunOmsi` (requer `-InstallPackage`), lança cada caso com `/observe-seconds`, observa `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_PTB.bmp` durante a sessão e confirma o restauro exato, a ausência de `Omsi.exe`, a ausência de `journal.json`, a saída `0`, os hashes inalterados dos plugins de terceiros e um conjunto inalterado do plugin permanente. Escreve `.omsilaunch\diagnostics\release-presentation-validation.json`. | Sim com `-RunOmsi` (no âmbito da sessão, restaurado) |

Ambos os scripts `Test-*` leem `OmsiLaunch.Version.props` para saber a versão esperada.
