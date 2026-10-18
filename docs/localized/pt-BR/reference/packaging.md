# Empacotamento e estrutura do release

<!-- l10n: source=reference/packaging.md source-sha256=56dbb36369fb47d7531d84d9ff8e6e2050d220420d970194822e6b13afd3d812 -->
> Tradução da [página original em inglês](../../../reference/packaging.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página descreve o pacote de release `0.1.0-beta4` do OmsiLaunch: o que `tools\New-ReleasePackage.ps1` produz, os campos de `release-manifest.json`, como o controlador usa o manifesto em runtime para verificar o conjunto de arquivos do plugin permanente, como o pacote é instalado em uma raiz de instalação do OMSI e removido dela, o que o diretório `.omsilaunch` contém após o uso e os scripts de validação (`tools\Test-ReleaseIdentity.ps1`, `tools\Test-ReleasePresentation.ps1`, `tools\Invoke-OfflineValidation.ps1`). A identidade do produto vem de `OmsiLaunch.Version.props`. As etapas de instalação para usuários estão em [instalação](../getting-started/installation.md); a função do conjunto de arquivos do plugin em runtime está em [plugin permanente](../concepts/permanent-plugin.md).

<a id="product-identity-omsilaunchversionprops"></a>
## Identidade do produto (`OmsiLaunch.Version.props`)

| Propriedade | Valor | Usado para |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | `product` do manifesto, `ProductName` do Windows |
| `OmsiLaunchCompanyName` | `LMonteiro and OmsiModsBR Team` | `CompanyName` do Windows |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - github.com/lmonteirotech and OmsiModsBR Team - github.com/OmsiModsBR` | `LegalCopyright` do Windows |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | `product_version` do manifesto, `ProductVersion` do Windows, versão informativa do assembly (`/version`), nome do ZIP público |
| `OmsiLaunchManagedVersion` | `0.1.0` | Base da versão dos assemblies gerenciados |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | Versão do assembly e versão de arquivo do Windows |
| `OmsiLaunchPackageAlias` | `current` | `package_alias` do manifesto, pasta de preparação e nome do ZIP de alias |

`Directory.Build.props` define `InformationalVersion` como `OmsiLaunchProductVersion` sem revisão de código-fonte, de modo que `OmsiLaunch.exe /version` imprime exatamente `0.1.0-beta4`.

## Build (`tools\New-ReleasePackage.ps1`)

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]` (saída padrão `artifacts\release`) prepara um pacote a partir de artefatos já compilados. A gravação em `artifacts\release` quando `OmsiLaunch-<product_version>.zip` já existe é recusada, a menos que `-AllowOverwritePublished` seja informado; pacotes candidatos vão para outro diretório (a validação offline usa `artifacts\candidate\post-round-a`).

Antes da preparação, toda saída de build é verificada quanto a estar desatualizada.

- **`OmsiLaunch.Native.x86.dll`: pelo conteúdo, não pelo timestamp.** O build nativo grava `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt` (target `WriteOmsiLaunchNativeBuildReceipt` no `.vcxproj`): o SHA-256 da DLL que produziu (`output=`) e de cada fonte a partir da qual ela foi compilada (`source=<sha256>|<path>`: o `.cpp`, o `.rc`, o `.vcxproj` e `OmsiLaunch.Version.props`). O empacotamento recusa a DLL quando seu hash não é a saída registrada (`does not match its build receipt`: uma cópia desatualizada ou alheia, qualquer que seja seu timestamp), quando uma fonte registrada mudou (`Native source changed after the recorded build`), quando uma fonte nativa não está coberta pelo recibo ou quando o recibo está ausente.
- **Shims e assemblies gerenciados: pelo timestamp.** Cada um não pode ser mais antigo que as fontes de seu próprio projeto (`.cpp`/`.rc`/`.vcxproj` de cada shim; o próprio projeto de cada assembly gerenciado). Uma entrada desatualizada aborta o empacotamento com `Stale build artifact`.

O pacote é então montado em um diretório de preparação totalmente novo e com nome exclusivo (`.staging-<guid>` no diretório de saída), de modo que nenhum arquivo de uma execução anterior possa entrar no conjunto de arquivos. A pasta `OmsiLaunch-current` e os arquivos compactados anteriores só são substituídos depois que todas as verificações abaixo forem aprovadas.

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
| todo arquivo em `docs\` exceto `docs\localized\` (a documentação em inglês, mesma estrutura de diretórios) | `.omsilaunch\docs\` (assim existe `.omsilaunch\docs\reference\cli.md`, o caminho impresso pelo texto de uso da CLI; auditoria de documentação BUG-08). Os links de `docs\README.md` para os resumos na raiz do repositório (`PUBLIC-API.md` e outros) só funcionam no repositório de código-fonte. |
| `docs\localized\LOCALIZATION-MANIFEST.md` e `docs\localized\<locale>\**` para cada locale listado nesse manifesto (`pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` e `ja-JP`) | `.omsilaunch\docs\localized\` (mesma estrutura); um locale listado que esteja ausente aborta o script |

Em seguida, o script calcula o hash de cada arquivo preparado, grava `release-manifest.json` na raiz do pacote como UTF-8 **sem** BOM (o resultado não depende mais da edição do PowerShell), executa `Test-ReleasePackageIntegrity.ps1` sobre a preparação, compara novamente cada arquivo de plugin preparado e `OmsiLaunch.Native.x86.dll` com sua saída de build, compacta a preparação, **extrai o arquivo compactado em um diretório temporário novo e valida o conjunto de arquivos extraído contra o mesmo manifesto** (o manifesto dentro do arquivo compactado precisa ser idêntico byte a byte ao validado), depois publica a preparação como `OmsiLaunch-current`, o arquivo compactado como `OmsiLaunch-current.zip`, copia-o para `OmsiLaunch-<product_version>.zip` (`OmsiLaunch-0.1.0-beta4.zip`) e grava `OmsiLaunch-0.1.0-beta4.zip.sha256` contendo `<SHA-256>  <file name>`. Qualquer artefato ausente aborta o script. O script não compila; execute antes `Invoke-OfflineValidation.ps1` (ou as etapas individuais de `dotnet build` / MSBuild).

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

Somente os arquivos do produto na raiz, `plugins\OmsiLaunch.*` e `.omsilaunch\` pertencem ao produto. Plugins de terceiros em `plugins\` nunca são enumerados, copiados, submetidos a hash, removidos nem restaurados pelo OmsiLaunch.

## `release-manifest.json`

| Campo | Tipo | Significado |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`; precisa corresponder a `PublicCapabilityRegistry.ProtocolVersion` |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | `build_id` e `code_sha256` de cada layout conhecido (`Omsi23004`, `13ec110a...c028`); um caminho rápido, não um requisito |
| `configuration` | string | `Release` ou `Debug` |
| `generated_utc` | string | Hora do build em ISO-8601 |
| `files[]` | object[] | `path` (barras normais, relativo à raiz do pacote), `bytes`, `sha256` (hexadecimal em maiúsculas) para cada arquivo empacotado |

O manifesto é dado, nunca política executável: o controlador lê somente as entradas `plugins/`. O leitor aceita o arquivo com ou sem BOM UTF-8 (manifestos gravados pelo Windows PowerShell 5.1 antes desta correção contêm um).

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## Uso do manifesto em runtime (integridade do plugin)

Antes de cada plano e de cada inicialização, `OmsiLaunchService.LoadArtifacts` monta o conjunto de arquivos esperado do plugin (`RuntimeArtifactSet.Load`, `src\OmsiLaunch.Process\RuntimeDeployment.cs`):

1. O controlador procura `release-manifest.json` ao lado de `OmsiLaunch.exe` (`AppContext.BaseDirectory`). Quando presente, `ReleaseManifest.TryReadPluginHashes` extrai os hashes de `plugins/*` (`OL_E_RELEASE_MANIFEST_INVALID` se o arquivo não puder ser lido como manifesto).
2. O hash de cada arquivo instalado `<root>\plugins\OmsiLaunch.*` é calculado (SHA-256) e comparado:
   - com um manifesto: com o hash do manifesto; o diagnóstico de plano `plugin.integrity.reference = manifest` é registrado. Arquivo ausente → `OL_E_PERMANENT_PLUGIN_MISSING`; arquivo presente, mas não listado → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`; hash diferente → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` (`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`).
   - sem um manifesto (layout de desenvolvimento, ou uma instalação que omitiu o manifesto): somente a presença e a autoconsistência em relação à cópia empacotada ao lado do controlador podem ser verificadas; `plugin.integrity.reference = self`.
3. Uma falha marca o plano como não apto para execução (`OL_E_RUNTIME_ARTIFACT_MISSING` com o detalhe) ou rejeita a inicialização (saída `7`).

Os arquivos do plugin nunca são preparados, incluídos em snapshot, restaurados nem removidos por uma sessão; o conjunto de arquivos é parte permanente da instalação. O `OmsiLaunch.Native.x86.dll` x86 é carregado somente de `plugins\`; os assemblies gerenciados declaram `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`.

<a id="installation-into-the-omsi-root"></a>
## Instalação na raiz do OMSI

1. Verifique o arquivo compactado: compare `OmsiLaunch-0.1.0-beta4.zip` com `OmsiLaunch-0.1.0-beta4.zip.sha256`.
2. Extraia o arquivo compactado **diretamente na raiz da instalação do OMSI** (o diretório que contém `Omsi.exe`). Isso coloca os arquivos da raiz, `plugins\OmsiLaunch.*` (ao lado de eventuais plugins de terceiros) e `.omsilaunch\`.
3. Mantenha `release-manifest.json` ao lado de `OmsiLaunch.exe`: ele habilita a integridade do plugin baseada no manifesto. O manifesto e os binários precisam vir do mesmo pacote: binários novos sobre um manifesto mais antigo (ou o contrário) fazem toda inicialização falhar com `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`. `Test-ReleasePresentation.ps1 -InstallPackage` agora copia o manifesto junto com os arquivos do produto; antes ele o ignorava, o que deixava um manifesto mais antigo ao lado de binários mais novos (Round A RA-007).
4. Verifique a coerência em modo somente leitura com `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>`: `installation_comparison.coherent_with_package` precisa ser `true`.
5. Não mova binários do plugin para `.omsilaunch\` e não renomeie `plugins\OmsiLaunch.*`.
6. Verifique com `OmsiLaunch.exe /version`, `OmsiLaunch.exe profiles` e um `/plan` (veja [primeira sessão](../getting-started/first-session.md)).

Arquivos `.omsilaunch\assets\splash\*.bmp` existentes nunca são sobrescritos por uma sessão (um conjunto de assets gerenciado explicitamente persiste); sobrescrevê-los extraindo um novo pacote é uma ação deliberada do usuário.

<a id="the-omsilaunch-directory-after-use"></a>
## O diretório `.omsilaunch` após o uso

| Caminho | Criado por | Tempo de vida |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | pacote, ou copiado na primeira sessão com splash gerenciado | persistente |
| `docs\`, `examples\` | pacote | persistente |
| `session-profiles\<id>\profile.yaml` | usuário | persistente; veja [perfis de sessão](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | toda sessão | mantido para as 50 sessões mais recentes; arquivos mais antigos com prefixo de sessão são excluídos quando uma nova sessão é iniciada |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | `/runtime`, harnesses de validação | mesma retenção (prefixo de sessão) |
| `diagnostics\tray-host.log` | indicador na bandeja | persistente, com acréscimos |
| `diagnostics\release-presentation-*.out`, `release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | persistente (sem prefixo de sessão) |
| `journal.json` | transação | existe de `Prepared` até `Restored`; um que tenha sobrado significa que há recuperação pendente (`/recovery-status`) |
| `backup\<sessionId>\<sha256(path)>.bin` | transação | snapshots dos arquivos alterados; removidos após a restauração |

Nenhum dado sai da máquina. Veja [transações e recuperação](../concepts/transactions-and-recovery.md).

<a id="uninstall"></a>
## Desinstalação

1. Certifique-se de que nenhuma sessão esteja em execução (`OmsiLaunch.exe detect`, `OmsiLaunch.exe session status`) e de que nenhuma recuperação esteja pendente (`OmsiLaunch.exe /recovery-status`; execute `/recover` se `pending` for `true`), para que os arquivos do OMSI já estejam restaurados.
2. Exclua `plugins\OmsiLaunch.Plugin.opl`, `plugins\OmsiLaunch.PluginNE.dll`, `plugins\OmsiLaunch.Plugin.dll`, `plugins\OmsiLaunch.Plugin.deps.json`, `plugins\OmsiLaunch.Plugin.runtimeconfig.json`, `plugins\OmsiLaunch.Api.dll`, `plugins\OmsiLaunch.Builds.Omsi23004.dll`, `plugins\OmsiLaunch.Interop.dll`, `plugins\OmsiLaunch.Native.x86.dll`. Não toque nos outros plugins.
3. Exclua os arquivos do produto na raiz listados na estrutura acima (`OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.*.dll`, `OmsiLaunch.Controller.*.json`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md`).
4. Exclua `.omsilaunch\` (isso remove seus perfis de sessão e diagnósticos). Nunca o exclua enquanto `journal.json` existir.

Uma sessão concluída restaura todos os arquivos que lhe pertenciam, portanto nenhuma limpeza adicional é necessária. Os arquivos que o próprio OMSI grava enquanto é executado (por exemplo `[last_map]` em `options.cfg`, caches, `laststn.osn`, logs) são o estado normal do OMSI e não são revertidos; veja [transações e recuperação](../concepts/transactions-and-recovery.md).

<a id="validation-scripts"></a>
## Scripts de validação

| Script | Finalidade | Altera o OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | Compila `OmsiLaunch.sln` com avisos tratados como erros e os três projetos nativos (`OmsiLaunch.Native.x86` Win32, `OmsiLaunch.Bootstrapper` x64, `OmsiLaunch.WindowsHost` x64) via MSBuild, depois executa todas as suítes offline: `OmsiLaunch.TestHost`, `OmsiLaunch.UnitTests`, `OmsiLaunch.IntegrationTests`, `OmsiLaunch.ProfileTests`, `OmsiLaunch.WindowsUiTests` e `OmsiLaunch.DocumentationTests`, a menos que ignorada, e em seguida a regressão de empacotamento `Test-PackagingPipeline.ps1` (ignorada com `-SkipNative`). Imprime `OFFLINE VALIDATION PASSED`/`FAILED`. | Não |
| `tools\New-ReleasePackage.ps1` | Proteção contra artefatos desatualizados, preparação, manifesto, autoverificação de integridade, ZIP, checksum (acima). | Não |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | Verifica que o manifesto lista exatamente os arquivos empacotados com tamanho e SHA-256 correspondentes, que o conjunto de arquivos obrigatório (três executáveis, o controlador, os nove arquivos do plugin permanente incluindo `OmsiLaunch.Native.x86.dll`) está presente e que a configuração é `Release`. Com `-InstallationRoot`, compara os arquivos do produto da instalação com o pacote em modo **somente leitura**. Saída `0` = coerente. | Não (somente leitura) |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | Produz um pacote candidato a partir de uma preparação limpa em `artifacts\candidate\post-round-a` e exige que o conjunto de arquivos preparado e o arquivo compactado reextraído passem na verificação de integridade. Prova que o gate de integridade rejeita uma DLL adulterada, um `Native.x86` adulterado ou antigo, uma cópia desatualizada do plugin, um arquivo listado excluído, um arquivo inesperado, um hash do manifesto alterado ou malformado, entradas duplicadas (exatas, por maiúsculas/minúsculas, por separador), caminhos com diretório pai e absolutos e JSON inválido; que o empacotador rejeita um `Native.x86` antigo colocado na saída do build (mesmo com um timestamp mais novo) e um recibo cujas fontes mudaram; e que o arquivo compactado publicado nunca é sobrescrito. As saídas de build são restauradas byte a byte. Executado por `Invoke-OfflineValidation.ps1`. | Não |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | Extrai o ZIP para `artifacts\release\identity-verification`, verifica `product`/`product_version`/`package_alias` e confere `ProductName`, `CompanyName`, `LegalCopyright`, `FileVersion`, `ProductVersion` de cada `.exe`/`.dll` exceto `nethost.dll` e `YamlDotNet.dll`, o `InternalName`/`OriginalFilename` de ambos os shims e que `OmsiLaunch.exe` contém um ícone incorporado. | Não |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | Valida o executável Release empacotado em uma instalação real: o manifesto precisa ser `Release`, não conter caminhos `Debug` nem `runtime/plugin/` e instalar `plugins/OmsiLaunch.*`; os quatro assets de splash precisam existir. Executa três casos de `/plan` (gerenciado padrão, gerenciado com assets personalizados, `/splash:Unset`). Com `-RunOmsi` (exige `-InstallPackage`), inicia cada caso com `/observe-seconds`, observa `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_PTB.bmp` durante a sessão e confirma restauração exata, nenhum `Omsi.exe`, nenhum `journal.json`, saída `0`, hashes inalterados dos plugins de terceiros e conjunto inalterado do plugin permanente. Grava `.omsilaunch\diagnostics\release-presentation-validation.json`. | Sim com `-RunOmsi` (no escopo da sessão, restaurado) |

Ambos os scripts `Test-*` leem `OmsiLaunch.Version.props` para saber a versão esperada.
