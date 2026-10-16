<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Controlo de sessões para o OMSI 2.</strong></p>
<p align="center">Código aberto · Programável · Orientado pela comunidade</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <strong>Português (Portugal)</strong> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Esta é uma tradução do [README canónico em inglês (US)](../../README.md). Em caso de divergência, prevalece o README em inglês (US).

# OmsiLaunch

O **OmsiLaunch** é uma camada de código aberto para lançar o OMSI 2 de forma
programática, gerir sessões e controlar a simulação em execução. Planeia uma
sessão a partir de uma descrição declarativa, aplica cada alteração temporária
de configuração dentro de uma transação registada em journal, inicia o OMSI,
observa-o até ao início do jogo, permite que ferramentas leiam e alterem a
simulação em execução através de uma API pública e restaura todos os ficheiros
em que tocou quando a sessão termina.

É infraestrutura para launchers, ferramentas, automação e integrações da
comunidade. Não é um launcher gráfico.

> **Defina a sessão, não os cliques.**

## Estado: 0.1.0-beta3

A versão beta pública atual é a **`0.1.0-beta3`**. A Beta 3 é a base posterior
ao reforço (hardening). A maioria das suas funcionalidades está `RUNTIME_VALIDATED`:
foram observadas em sessões reais do OMSI, incluindo a ronda de fecho de runtime
de 2026-09-23. Algumas continuam `STATICALLY_VALIDATED` (apenas testes offline),
`PARTIAL` ou `UNAVAILABLE`. Dois itens de runtime continuam em aberto porque não
é possível produzi-los em segurança: o jogo com o Steam LAA e a remoção natural
de veículos rodoviários e humanos (RV-002). A página de
[estado da validação em runtime](../localized/pt-PT/status/runtime-validation-status.md) é
o registo de referência do que foi executado sob o OMSI e do que foi executado apenas offline.

Trata-se de uma beta: a API pública, a CLI e os formatos de ficheiro estão
marcados como `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` ou
`UNAVAILABLE` por membro e ainda podem mudar antes da 1.0.

## Âmbito do OMSI suportado

O OmsiLaunch suporta exatamente um build do OMSI 2 e recusa-se a iniciar tudo o
que não reconhece.

| Elemento | Âmbito |
| --- | --- |
| Build do OMSI | Perfil `Omsi23004_692EBFBF`: `Omsi.exe` com SHA-256 `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` (OMSI 2.3.004). `STABLE_BETA`; todas as validações em runtime foram executadas com este ficheiro. |
| Executável Steam LAA | O SHA-256 `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` é aceite por lista de permissões. Apenas a impressão digital e o planeamento foram validados; o jogo **não** foi validado em runtime. `PARTIAL`. |
| Builds desconhecidos | Rejeitados com `OL_E_UNSUPPORTED_BUILD`. O hash é verificado novamente em cada plano e em cada início. |
| Sistema operativo | Windows 10 ou posterior, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (controlador) e .NET 6 Runtime **x86** (o plugin é executado dentro do `Omsi.exe` de 32 bits). |

Detalhes: [Compatibilidade](../localized/pt-PT/reference/compatibility.md) e
[Instalação](../localized/pt-PT/getting-started/installation.md).

## O que oferece

**Sessões.** Uma sessão é planeada a partir de uma `LaunchSpec` (flags da CLI,
um ficheiro JSON, um perfil de sessão ou a API), validada sem efeitos
secundários (`/plan`), depois iniciada, observada através das transições de
`SessionState` e terminada. Parar uma sessão termina o OMSI à força, para que o
OMSI não possa sobrescrever os ficheiros que estão prestes a ser restaurados.
Consultar [Ciclo de vida da sessão](../localized/pt-PT/concepts/session-lifecycle.md).

**Transações e recuperação.** Cada substituição de configuração está limitada à
sessão. O OmsiLaunch cria um snapshot, regista em journal, aplica, verifica e
restaura cada ficheiro que altera, incluindo após uma falha (`/recovery-status`,
`/recover`, `RecoverPendingAsync`). Não oferece edição permanente da
configuração. Consultar
[Transações e recuperação](../localized/pt-PT/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Um frontend de referência sobre a mesma API pública,
sem lógica própria do OMSI: descoberta, planeamento, início de sessões e
comandos de cliente contra uma sessão em execução. Consultar a
[referência da CLI](../localized/pt-PT/reference/cli.md)
e os [exemplos da CLI](../localized/pt-PT/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** O anfitrião do subsistema Windows para atalhos. Aceita a
mesma linha de comandos sem janela de consola e comunica as falhas em caixas de
mensagem. Consultar [OmsiLaunchW.exe](../localized/pt-PT/reference/omsilaunchw.md).

**Área de notificação do Windows.** Cada sessão de proprietário mostra um ícone
na área de notificação com uma janela de estado e uma ação "End session"
(terminar a sessão). A ação segue o mesmo caminho de paragem que
`session stop`. Consultar
[Área de notificação do Windows](../localized/pt-PT/reference/windows-tray.md).

**Perfis de sessão.** Pacotes declarativos `profile.yaml`
(`omsilaunch.session-profile/v1`) em
`.omsilaunch\session-profiles\<id>\`, para que os autores de conteúdos possam
distribuir sessões reprodutíveis que se iniciam com um único comando. Consultar
[Perfis de sessão](../localized/pt-PT/reference/session-profiles.md).

**API pública e controlo de runtime.** `OmsiLaunch.Api` (`IOmsiLaunch`) é a
superfície de produto preferida. As operações de runtime, como hora,
meteorologia, mapa, câmara, horário, veículos, humanos, variáveis de script e
texturas D3D, estão limitadas à sessão, são validadas face ao perfil de build e
endereçadas através de handles semânticos opacos, nunca de ponteiros nativos. Os
resultados estão limitados por um slot de runtime de 64 KiB. Consultar a
[referência da API pública](../localized/pt-PT/reference/public-api.md) e
[Controlo de runtime](../localized/pt-PT/reference/runtime-control.md).

**Controlo local.** Um named pipe por instalação, associado ao `session_id`
ativo, permite que outros processos do mesmo utilizador leiam o estado e os
eventos, parem a sessão e executem operações públicas de runtime. Consultar
[Controlo local / IPC](../localized/pt-PT/reference/local-control.md).

**Capacidades.** Cada capacidade e operação pública de runtime está catalogada
com a respetiva estabilidade. As capacidades experimentais e indisponíveis são
listadas, não ocultadas (`OmsiLaunch.exe capabilities`). Consultar
[Capacidades](../localized/pt-PT/reference/capabilities.md).

**Plugin permanente.** O conjunto do plugin em processo é instalado uma única vez
em `plugins\OmsiLaunch.*`. Antes de cada início, é verificado face às entradas
SHA-256 de `release-manifest.json`. Os plugins de terceiros nunca são tocados.
Consultar [Modelo de plugin permanente](../localized/pt-PT/concepts/permanent-plugin.md).

## Início rápido

Extrair o pacote de lançamento para a raiz da instalação do OMSI 2 e, em
seguida, executar os seguintes comandos a partir desse diretório:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Enquanto essa sessão está a executar, uma segunda consola no mesmo diretório
pode consultá-la ou terminá-la:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Guia passo a passo: [Primeira sessão](../localized/pt-PT/getting-started/first-session.md). Para
integradores .NET: [Início rápido da API pública](../localized/pt-PT/getting-started/api-quick-start.md).

## Documentação

O conjunto completo da documentação está indexado em [`docs/localized/pt-PT/README.md`](../localized/pt-PT/README.md).
As páginas em inglês (US) em `docs/` são canónicas e normativas.

| Tema | Página |
| --- | --- |
| API pública | [docs/localized/pt-PT/reference/public-api.md](../localized/pt-PT/reference/public-api.md) |
| CLI | [docs/localized/pt-PT/reference/cli.md](../localized/pt-PT/reference/cli.md) |
| Exemplos da CLI | [docs/localized/pt-PT/reference/cli-examples.md](../localized/pt-PT/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/pt-PT/reference/omsilaunchw.md](../localized/pt-PT/reference/omsilaunchw.md) |
| Área de notificação do Windows | [docs/localized/pt-PT/reference/windows-tray.md](../localized/pt-PT/reference/windows-tray.md) |
| Perfis de sessão | [docs/localized/pt-PT/reference/session-profiles.md](../localized/pt-PT/reference/session-profiles.md) |
| Controlo de runtime | [docs/localized/pt-PT/reference/runtime-control.md](../localized/pt-PT/reference/runtime-control.md) |
| Controlo local / IPC | [docs/localized/pt-PT/reference/local-control.md](../localized/pt-PT/reference/local-control.md) |
| Capacidades | [docs/localized/pt-PT/reference/capabilities.md](../localized/pt-PT/reference/capabilities.md) |
| Erros e códigos de saída | [docs/localized/pt-PT/reference/errors.md](../localized/pt-PT/reference/errors.md), [docs/localized/pt-PT/reference/exit-codes.md](../localized/pt-PT/reference/exit-codes.md) |
| Empacotamento | [docs/localized/pt-PT/reference/packaging.md](../localized/pt-PT/reference/packaging.md) |
| Limitações conhecidas | [docs/localized/pt-PT/reference/known-limitations.md](../localized/pt-PT/reference/known-limitations.md) |
| Estado da validação em runtime | [docs/localized/pt-PT/status/runtime-validation-status.md](../localized/pt-PT/status/runtime-validation-status.md) |

### Documentação noutras línguas

A documentação está traduzida para 14 locales em
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). As traduções
foram produzidas a partir das páginas canónicas em inglês (US) e validadas
mecanicamente face a estas. A revisão editorial por falantes nativos não fez
parte da versão Beta 3 e poderá ser feita após a publicação. Quando uma tradução
e a página em inglês divergem, prevalece a página em inglês.

## Limitações e itens em aberto

Estas são as limitações mais importantes. A lista completa encontra-se em
[Limitações conhecidas](../localized/pt-PT/reference/known-limitations.md).

- **Um único build do OMSI.** O Steam LAA está `PARTIAL`: o jogo, as leituras,
  os comandos e a paragem necessitam de uma instalação Steam genuína e não foram
  validados em runtime.
- **Indisponível nesta beta:** iniciar a partir do último estado do mapa
  (`/last`), data, hora, ano ou meteorologia explícitos no início e atribuição do
  veículo do jogador no início. Pedir qualquer um destes torna o plano não
  executável, em vez de ser ignorado silenciosamente.
- **As escritas em runtime são limitadas.** `weather.set`, as escritas de
  calendário, as escritas de variáveis de texto e a relocalização de veículos
  estão indisponíveis. As alterações em runtime não são registadas em journal nem
  restauradas.
- **Tempo de vida dos handles.** A deteção de handles obsoletos para veículos
  rodoviários e humanos que desaparecem naturalmente (RV-002) não tem um produtor
  de runtime seguro e está validada apenas offline.
- **Resultados limitados.** As listas longas são truncadas (`truncated=true`).
  Não existe paginação.
- **A paragem é forçada.** O encerramento próprio do OMSI não é executado e o
  estado do OMSI não guardado perde-se.
- **Modelo de confiança do mesmo utilizador.** Qualquer processo do mesmo
  utilizador do Windows pode aceder ao plano de controlo local.

## Transferência

Transferir **`OmsiLaunch-0.1.0-beta3.zip`** e o respetivo ficheiro `.sha256` a
partir da [página de versões](https://github.com/lmonteirotech/OmsiLaunch/releases). Extraí-lo
diretamente para a raiz do OMSI suportada. O pacote contém o controlador
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), as respetivas dependências, o conjunto do
plugin permanente com `release-manifest.json`, recursos do ecrã de abertura
(splash), um exemplo de sessão e a documentação offline em `.omsilaunch\docs\`.
Estrutura do pacote e remoção limpa: [Empacotamento](../localized/pt-PT/reference/packaging.md).

## Compilar a partir do código-fonte

Requisitos:

- Windows 10 ou posterior, x64.
- SDK do .NET 6, com os runtimes .NET 6 x64 e x86 para executar os testes.
- Visual Studio com a carga de trabalho C++ do MSBuild (conjunto de ferramentas
  de plataforma `v145`) e um SDK do Windows 10, para os três projetos nativos:
  `OmsiLaunch.Native.x86` (Win32), e `OmsiLaunch.Bootstrapper` e
  `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` contém os projetos geridos e os conjuntos de testes. O ponto de
entrada offline único compila tudo e executa todos os conjuntos offline; nunca
lança o OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` ignora os projetos nativos e a regressão de empacotamento.
`-SkipDocs` ignora os gates de documentação. O resultado da compilação vai para
`artifacts\`, que não é controlado pelo versionamento. `tools\New-ReleasePackage.ps1`
prepara, calcula os hashes, valida e comprime um pacote de lançamento a partir
de uma compilação existente. Consultar
[Empacotamento](../localized/pt-PT/reference/packaging.md) e
[Testes e validação](../project/TESTING-AND-VALIDATION.md).

| Caminho | Conteúdo |
| --- | --- |
| `src/` | Bibliotecas do produto: API, núcleo, configuração, conteúdos, interop, processo, plugin, perfil de build, fronteira nativa x86 |
| `tools/` | CLI e anfitrião Windows (`OmsiLaunch.Cli`), shims nativos (`OmsiLaunch.Bootstrapper`), anfitrião de testes offline, scripts de empacotamento e validação, ferramentas de localização |
| `tests/` | Conjuntos de testes unitários, de integração, de perfil, de UI do Windows e de documentação |
| `docs/` | Documentação canónica e as respetivas traduções em `docs/localized/` |
| `examples/` | Exemplos de LaunchSpec e de sessão |
| `assets/` | Identidade visual, ícones e recursos do pacote |
| `third_party/` | Notas de proveniência a montante |

Resumos para mantenedores: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Comunidade e licença

O OmsiLaunch é um projeto de código aberto orientado para a comunidade. É
independente de outros launchers do OMSI, e ferramentas compatíveis da
comunidade podem ser construídas sobre ele.

O OmsiLaunch está licenciado sob [LGPL-3.0-only](../../LICENSE). Consultar os
[avisos de terceiros](../../THIRD-PARTY-NOTICES.md) para a proveniência do código-fonte
incorporado e os avisos aplicáveis.
