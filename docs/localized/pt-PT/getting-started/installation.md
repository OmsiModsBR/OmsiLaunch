# Instalação

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Tradução da [página original em inglês](../../../getting-started/installation.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página explica o que o OmsiLaunch `0.1.0-beta4` requer, que build do OMSI suporta, como o pacote de lançamento é instalado numa raiz de instalação do OMSI e como verificar a instalação com `/version` e `/plan` antes de iniciar uma sessão. O conteúdo do pacote é especificado em [empacotamento](../reference/packaging.md); o primeiro lançamento é descrito em [primeira sessão](first-session.md).

<a id="requirements"></a>
## Requisitos

| Requisito | Detalhe | Falha quando em falta |
|---|---|---|
| Windows 10 ou posterior, 64 bits | O controlador verifica `Environment.OSVersion.Version.Major >= 10`, um sistema operativo x64 e um processo x64. | Plano não executável com `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (ou `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), saída `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` requer `Microsoft.NETCore.App` 6.0 e `Microsoft.WindowsDesktop.App` 6.0 (o indicador da área de notificação usa Windows Forms). Os shims localizam-no com `nethost.dll`. | `OmsiLaunch.exe` termina com o código de shim `102`..`106` antes de qualquer saída; `OmsiLaunchW.exe` mostra `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` requer `Microsoft.NETCore.App` 6.0 para x86, porque o plugin é executado dentro do `Omsi.exe` de 32 bits. O pacote x86 do .NET 6 Desktop Runtime também satisfaz este requisito. | O plugin não arranca dentro do OMSI; a sessão não chega a `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), saída `1`, ficheiros restaurados. |
| OMSI 2 instalado | Um `Omsi.exe` de 32 bits com uma versão do OMSI 2 (2.x), e `maps\` e `Vehicles\` na raiz. Não é exigido nenhum hash de executável em particular; o OMSI 2.3.004 é a disposição conhecida e validada em runtime e, noutras builds, cada capacidade é resolvida e verificada em processo. | `OL_E_UNSUPPORTED_BUILD`; plano não executável, saída `1`. Uma capacidade indisponível na build falha com `OL_E_CAPABILITY_UNRESOLVED`. Ver [compatibilidade](../reference/compatibility.md). |
| Raiz de instalação com permissão de escrita | A transação escreve `.omsilaunch\` e overlays em `GUI\`, `Texture\` e `options.cfg`, e restaura-os; a raiz tem de ter permissão de escrita para o utilizador atual (evitar `Program Files` sem as permissões adequadas). | `OL_E_INSTALLATION_NOT_WRITABLE`, saída `1`. |
| Um utilizador, um proprietário por instalação | O lease da instalação `Local\OmsiLaunch.Installation.<sha256(root)>` e o pipe de controlo são por sessão de início de sessão (logon) do Windows. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, saída `7`. |

Ambos os runtimes são transferências separadas a partir da Microsoft; deve instalar-se o x64 Desktop Runtime e o x86 Runtime (ou o x86 Desktop Runtime) do .NET 6. Não é necessário nenhum outro componente. Nenhum dado sai da máquina.

<a id="confirm-the-omsi-build"></a>
## Confirmar a build do OMSI

Substituir `<OMSI_PATH>` pelo diretório do OMSI 2 (por exemplo `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

O hash tem de ser um dos dois indicados acima. Após a instalação, `OmsiLaunch.exe profiles` imprime a mesma lista com o respetivo estado de validação.

<a id="install-the-package"></a>
## Instalar o pacote

1. Transferir `OmsiLaunch-0.1.0-beta4.zip` e `OmsiLaunch-0.1.0-beta4.zip.sha256`; verificar a soma de controlo (`Get-FileHash` tem de ser igual ao valor no ficheiro `.sha256`).
2. Extrair o ZIP **diretamente para a raiz de instalação do OMSI** (a pasta que contém `Omsi.exe`). O ZIP está organizado para essa raiz:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` e os restantes assemblies do controlador `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` na raiz;
   - o conjunto fechado do plugin permanente `plugins\OmsiLaunch.*` (9 ficheiros) ao lado dos plugins já existentes, que nunca são tocados;
   - `.omsilaunch\` com os recursos do ecrã de abertura (splash screen), a documentação offline e os exemplos.
3. Manter `release-manifest.json` ao lado de `OmsiLaunch.exe`. É este ficheiro que permite a cada início verificar os ficheiros do plugin instalados por SHA-256 (`plugin.integrity.reference = manifest`); sem ele, apenas a presença e a coerência interna são verificadas (`plugin.integrity.reference = self`).
4. Não mover nem mudar o nome de nada em `plugins\OmsiLaunch.*` e não colocar binários do plugin em `.omsilaunch\`.

A atualização é a mesma operação: extrair o novo pacote por cima dos ficheiros antigos enquanto nenhuma sessão está a executar e nenhuma recuperação está pendente (`OmsiLaunch.exe /recovery-status`). Os hashes do plugin e o manifesto têm sempre de vir do mesmo pacote (caso contrário, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Verificar

Executar a partir da raiz do OMSI (o argumento de instalação tem como predefinição o diretório que contém `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Esperado: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; saída `0`. A saída `102`..`106` significa que o runtime x64 do .NET 6 está em falta ou que o pacote está incompleto.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Esperado: os hashes suportados e, em seguida, os mapas encontrados nesta instalação; saída `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Esperado: `Plan: READY profile=Omsi23004` (na disposição conhecida; `unrecognized` nos restantes casos) e saída `0` (pode usar-se qualquer identidade de mapa de `/list:Maps`; o índice do ponto de entrada tem de ser um índice apresentado desse mapa, ver `/list:Entrypoints /map:<identity>`). Com `--json`, o plano lista `TouchedFiles` (os overlays do splash), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED` e `RESOLVED_KNOWN_LAYOUT` para as capacidades nativas de início na disposição conhecida), `Diagnostics` (incluindo `plugin.integrity.reference`) e `IsRunnable`. `Plan: NOT RUNNABLE` com saída `1` indica o motivo em `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). O planeamento nunca inicia o OMSI e nunca escreve na instalação.

<a id="where-things-live-afterwards"></a>
## Onde ficam as coisas depois

| Caminho | Conteúdo |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Trace do host de cada sessão (são mantidas as 50 sessões mais recentes) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Registo (log) do indicador da área de notificação |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Presentes apenas enquanto uma transação está pendente; ver [transações e recuperação](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Os perfis de sessão predefinidos do utilizador; ver [perfis de sessão](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Recursos de splash geridos |
| `<root>\.omsilaunch\docs\` | Esta documentação, offline (começar em `README.md`; a referência da CLI é `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | LaunchSpec e perfil de sessão de exemplo |

<a id="uninstall"></a>
## Desinstalar

Parar qualquer sessão, executar `OmsiLaunch.exe /recovery-status` (e `/recover` se houver algo pendente) e, em seguida, eliminar os ficheiros do produto na raiz, `plugins\OmsiLaunch.*` e `.omsilaunch\`. Detalhes em [empacotamento](../reference/packaging.md).

<a id="next"></a>
## Seguinte

[Primeira sessão](first-session.md) · [Referência da CLI](../reference/cli.md) · [limitações conhecidas](../reference/known-limitations.md)
