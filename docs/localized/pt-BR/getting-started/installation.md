# Instalação

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Tradução da [página original em inglês](../../../getting-started/installation.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página explica o que o OmsiLaunch `0.1.0-beta4` exige, qual build do OMSI ele suporta, como o pacote de release é instalado em uma raiz de instalação do OMSI e como verificar a instalação com `/version` e `/plan` antes de iniciar uma sessão. O conteúdo do pacote é especificado em [empacotamento](../reference/packaging.md); a primeira inicialização é descrita em [primeira sessão](first-session.md).

<a id="requirements"></a>
## Requisitos

| Requisito | Detalhe | Falha quando ausente |
|---|---|---|
| Windows 10 ou posterior, 64 bits | O controlador verifica `Environment.OSVersion.Version.Major >= 10`, um sistema operacional x64 e um processo x64. | Plano não apto para execução com `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (ou `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), saída `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` exige `Microsoft.NETCore.App` 6.0 e `Microsoft.WindowsDesktop.App` 6.0 (o Windows Forms é usado pelo indicador na bandeja). Os shims o localizam com `nethost.dll`. | O `OmsiLaunch.exe` sai com o código de shim `102`..`106` antes de qualquer saída; o `OmsiLaunchW.exe` mostra `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` exige `Microsoft.NETCore.App` 6.0 para x86, porque o plugin é executado dentro do `Omsi.exe` de 32 bits. O pacote .NET 6 Desktop Runtime x86 também atende a esse requisito. | O plugin não inicia dentro do OMSI; a sessão não chega a `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), saída `1`, arquivos restaurados. |
| OMSI 2 instalado | Um `Omsi.exe` de 32 bits com versão do OMSI 2 (2.x), e `maps\` e `Vehicles\` na raiz. Nenhum hash específico do executável é exigido; o OMSI 2.3.004 é o layout conhecido e validado em runtime, e em outros builds cada capacidade é resolvida e verificada no processo. | `OL_E_UNSUPPORTED_BUILD`; plano não apto para execução, saída `1`. Uma capacidade indisponível no build falha com `OL_E_CAPABILITY_UNRESOLVED`. Veja [compatibilidade](../reference/compatibility.md). |
| Raiz da instalação gravável | A transação grava `.omsilaunch\`, overlays em `GUI\`, `Texture\` e `options.cfg` e os restaura; a raiz precisa ser gravável pelo usuário atual (evite `Program Files` sem as permissões adequadas). | `OL_E_INSTALLATION_NOT_WRITABLE`, saída `1`. |
| Um usuário, um proprietário por instalação | O lease da instalação `Local\OmsiLaunch.Installation.<sha256(root)>` e o pipe de controle são por sessão de logon. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, saída `7`. |

Os dois runtimes são downloads separados da Microsoft; instale o Desktop Runtime x64 e o Runtime x86 (ou o Desktop Runtime x86) do .NET 6. Nenhum outro componente é necessário. Nenhum dado sai da máquina.

<a id="confirm-the-omsi-build"></a>
## Confirme o build do OMSI

Substitua `<OMSI_PATH>` pelo seu diretório do OMSI 2 (por exemplo `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

O hash precisa ser um dos dois listados acima. Depois da instalação, `OmsiLaunch.exe profiles` imprime a mesma lista com o respectivo status de validação.

<a id="install-the-package"></a>
## Instale o pacote

1. Baixe `OmsiLaunch-0.1.0-beta4.zip` e `OmsiLaunch-0.1.0-beta4.zip.sha256`; verifique o checksum (`Get-FileHash` precisa ser igual ao valor do arquivo `.sha256`).
2. Extraia o arquivo compactado **diretamente na raiz da instalação do OMSI** (a pasta que contém `Omsi.exe`). O arquivo compactado está organizado para essa raiz:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` e os demais assemblies do controlador `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` na raiz;
   - o conjunto de arquivos do plugin permanente `plugins\OmsiLaunch.*` (9 arquivos) ao lado dos seus plugins existentes, que nunca são tocados;
   - `.omsilaunch\` com os recursos de splash, a documentação offline e os exemplos.
3. Mantenha `release-manifest.json` ao lado de `OmsiLaunch.exe`. É ele que permite que cada início verifique os arquivos do plugin instalados por SHA-256 (`plugin.integrity.reference = manifest`); sem ele, apenas a presença e a consistência interna são verificadas (`plugin.integrity.reference = self`).
4. Não mova nem renomeie nada em `plugins\OmsiLaunch.*` e não coloque binários do plugin em `.omsilaunch\`.

A atualização é a mesma operação: extraia o novo pacote sobre os arquivos antigos enquanto nenhuma sessão estiver em execução e nenhuma recuperação estiver pendente (`OmsiLaunch.exe /recovery-status`). Os hashes do plugin e o manifesto precisam sempre vir do mesmo pacote (caso contrário, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Verifique

Execute a partir da raiz do OMSI (o argumento de instalação tem como padrão o diretório que contém `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Esperado: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; saída `0`. A saída `102`..`106` significa que o runtime .NET 6 x64 está ausente ou que o pacote está incompleto.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Esperado: os hashes suportados e, em seguida, os mapas encontrados nesta instalação; saída `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Esperado: `Plan: READY profile=Omsi23004` (no layout conhecido; `unrecognized` nos demais) e saída `0` (use qualquer identidade de mapa de `/list:Maps`; o índice do ponto de entrada precisa ser um índice apresentado daquele mapa, veja `/list:Entrypoints /map:<identity>`). Com `--json`, o plano lista `TouchedFiles` (os overlays do splash), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED` e, para as capacidades nativas de início no layout conhecido, `RESOLVED_KNOWN_LAYOUT`), `Diagnostics` (incluindo `plugin.integrity.reference`) e `IsRunnable`. `Plan: NOT RUNNABLE` com saída `1` indica o motivo em `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). O planejamento nunca inicia o OMSI e nunca grava na instalação.

<a id="where-things-live-afterwards"></a>
## Onde ficam as coisas depois

| Caminho | Conteúdo |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Trace do host de cada sessão (as 50 sessões mais recentes são mantidas) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Log do indicador na bandeja |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Presentes somente enquanto uma transação está pendente; veja [transações e recuperação](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Seus perfis de sessão predefinidos; veja [perfis de sessão](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Recursos do splash gerenciado |
| `<root>\.omsilaunch\docs\` | Esta documentação, offline (comece por `README.md`; a referência da CLI é `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Exemplo de LaunchSpec e de perfil de sessão |

<a id="uninstall"></a>
## Desinstalar

Pare qualquer sessão, execute `OmsiLaunch.exe /recovery-status` (e `/recover` se houver pendência) e, em seguida, exclua os arquivos do produto na raiz, `plugins\OmsiLaunch.*` e `.omsilaunch\`. Detalhes em [empacotamento](../reference/packaging.md).

<a id="next"></a>
## Próximos passos

[Primeira sessão](first-session.md) · [Referência da CLI](../reference/cli.md) · [limitações conhecidas](../reference/known-limitations.md)
