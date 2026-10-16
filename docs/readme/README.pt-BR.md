<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Controle de sessões para o OMSI 2.</strong></p>
<p align="center">Código aberto · Programável · Movido pela comunidade</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <strong>Português (Brasil)</strong> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
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

> Esta é uma tradução do [README canônico em inglês (EUA)](../../README.md). Se os dois divergirem, vale o README em inglês (EUA).

# OmsiLaunch

O **OmsiLaunch** é uma camada de código aberto para iniciar o OMSI 2 de forma programática,
gerenciar sessões e controlar a simulação em execução. Ele planeja uma sessão
a partir de uma descrição declarativa, aplica cada alteração temporária de configuração
dentro de uma transação registrada no journal, inicia o OMSI, acompanha-o até o gameplay, permite
que ferramentas leiam e alterem a simulação em execução por meio de uma API pública e restaura
todos os arquivos que modificou quando a sessão termina.

É infraestrutura para launchers, ferramentas, automação e integrações da
comunidade. Não é um launcher gráfico.

> **Defina a sessão, não os cliques.**

## Status: 0.1.0-beta4

O beta público atual é o **`0.1.0-beta4`**. O Beta 4 adiciona compatibilidade
por capacidade, Runtime Modules, perfis de sessão 1..99 e um conjunto de recursos
de inicialização e de runtime. A compatibilidade, o catálogo de capacidades, a paginação,
os erros tipados e a ponte de módulos foram observados em sessões reais do OMSI (rodadas
de validação A e B). Os recursos adicionados depois dessas rodadas são `EXPERIMENTAL`:
fechados por análise estática e testados somente offline, com a validação em runtime
pendente. Alguns continuam `STATICALLY_VALIDATED`, `PARTIAL` ou `UNAVAILABLE`.
Dois itens de runtime continuam em aberto porque não podem ser produzidos com segurança: o gameplay
com o Steam LAA e a remoção natural de veículos rodoviários e humanos (RV-002). A página de
[status de validação em runtime](../localized/pt-BR/status/runtime-validation-status.md) é
o registro oficial do que foi executado sob o OMSI e do que foi executado somente offline.

É um beta: a API pública, a CLI e os formatos de arquivo são marcados como `STABLE_BETA`,
`EXPERIMENTAL`, `PARTIAL`, `INTERNAL` ou `UNAVAILABLE` por membro e ainda podem
mudar antes da 1.0.

## Escopo do OMSI suportado

O OmsiLaunch exige um OMSI 2 instalado. Ele não depende de um arquivo `Omsi.exe`
específico: cada capacidade nativa é resolvida e verificada individualmente
dentro do OMSI antes de ser usada, e uma capacidade que não pode ser verificada em
um build fica indisponível nesse build.

| Item | Escopo |
| --- | --- |
| Requisito | OMSI 2 instalado: um `Omsi.exe` de 32 bits com versão do OMSI 2 (2.x) e os diretórios `maps\` e `Vehicles\`. Qualquer outra coisa é recusada com `OL_E_UNSUPPORTED_BUILD`. |
| Layout conhecido | OMSI 2.3.004 (`Omsi23004`). Todas as capacidades são resolvidas, e todas as validações em runtime foram executadas neste layout. Os hashes o identificam como um caminho rápido; não são um requisito. |
| Outros builds do OMSI 2 | Cada capacidade é resolvida por estratégias independentes do build (RTTI do Delphi, assinaturas de código únicas, slots de VMT) e verificada. Capacidades que leem layouts de objetos precisam de um layout conhecido. |
| Executável Steam | Resolvido no processo; uma sessão Steam genuína **não** foi validada em runtime. |
| Sistema operacional | Windows 10 ou posterior, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (controlador) e .NET 6 Runtime **x86** (o plugin é executado dentro do `Omsi.exe` de 32 bits). |

Detalhes: [Compatibilidade](../localized/pt-BR/reference/compatibility.md) e
[Instalação](../localized/pt-BR/getting-started/installation.md).

## O que ele oferece

**Sessões.** Uma sessão é planejada a partir de um `LaunchSpec` (flags da CLI, um arquivo JSON,
um perfil de sessão ou a API), validada sem efeitos colaterais (`/plan`) e, em seguida,
iniciada, acompanhada pelas transições de `SessionState` e encerrada. Parar uma
sessão encerra o OMSI à força, para que o OMSI não possa sobrescrever os arquivos que
estão prestes a ser restaurados. Veja [Ciclo de vida da sessão](../localized/pt-BR/concepts/session-lifecycle.md).

**Transações e recuperação.** Toda substituição de configuração é restrita à
sessão. O OmsiLaunch faz snapshot, registra no journal, aplica, verifica e restaura cada
arquivo que altera, inclusive após uma falha (`/recovery-status`, `/recover`,
`RecoverPendingAsync`). Ele não oferece edição permanente de configuração. Veja
[Transações e recuperação](../localized/pt-BR/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Um frontend de referência sobre a mesma API pública, sem
lógica própria do OMSI: descoberta, planejamento, início de sessões e comandos de
cliente contra uma sessão em execução. Veja a [referência da CLI](../localized/pt-BR/reference/cli.md)
e os [exemplos da CLI](../localized/pt-BR/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** O host do subsistema Windows para atalhos. Ele aceita a
mesma linha de comando, sem janela de console, e informa falhas em caixas de mensagem.
Veja [OmsiLaunchW.exe](../localized/pt-BR/reference/omsilaunchw.md).

**Bandeja do Windows.** Toda sessão de proprietário exibe um ícone na área de notificação com uma
janela de status e uma ação "End session" ("Encerrar sessão"). A ação segue o mesmo caminho de parada
que `session stop`. Veja [Bandeja do Windows](../localized/pt-BR/reference/windows-tray.md).

**Perfis de sessão.** Pacotes declarativos `profile.yaml`
(`omsilaunch.session-profile/v1`) em
`.omsilaunch\session-profiles\<id>\`, para que autores de conteúdo possam distribuir
sessões reproduzíveis que iniciam com um único comando. Veja
[Perfis de sessão](../localized/pt-BR/reference/session-profiles.md).

**API pública e controle de runtime.** O `OmsiLaunch.Api` (`IOmsiLaunch`) é a
superfície de produto preferencial. Operações de runtime como hora, clima, mapa,
câmera, tabela de horários, veículos, humanos, variáveis de script e texturas D3D são
restritas à sessão, validadas em relação ao perfil do build e endereçadas por handles
semânticos opacos, nunca por ponteiros nativos. Os resultados são limitados por um slot de runtime
de 64 KiB. Veja a [referência da API pública](../localized/pt-BR/reference/public-api.md) e
[Controle de runtime](../localized/pt-BR/reference/runtime-control.md).

**Controle local.** Um named pipe por instalação, vinculado ao
`session_id` ativo, permite que outros processos do mesmo usuário leiam status e eventos, parem a
sessão e executem operações públicas de runtime. Veja
[Controle local / IPC](../localized/pt-BR/reference/local-control.md).

**Capacidades.** Toda capacidade e toda operação pública de runtime são catalogadas
com sua estabilidade. Capacidades experimentais e indisponíveis são listadas, não
ocultadas (`OmsiLaunch.exe capabilities`). Veja
[Capacidades](../localized/pt-BR/reference/capabilities.md).

**Plugin permanente.** O conjunto de arquivos do plugin executado no processo é instalado uma única vez em
`plugins\OmsiLaunch.*`. Antes de cada início, ele é verificado em relação às entradas SHA-256
do `release-manifest.json`. Plugins de terceiros nunca são tocados. Veja
[Modelo de plugin permanente](../localized/pt-BR/concepts/permanent-plugin.md).

## Início rápido

Extraia o pacote de release na raiz da instalação do OMSI 2 e, em seguida, execute os
seguintes comandos a partir desse diretório:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Enquanto essa sessão estiver em execução, um segundo console no mesmo diretório pode consultá-la ou
encerrá-la:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Passo a passo: [Primeira sessão](../localized/pt-BR/getting-started/first-session.md). Para integradores
.NET: [Início rápido da API pública](../localized/pt-BR/getting-started/api-quick-start.md).

## Documentação

O conjunto completo da documentação está indexado em [`docs/localized/pt-BR/README.md`](../localized/pt-BR/README.md).
As páginas em inglês (EUA) em `docs/` são canônicas e normativas.

| Tópico | Página |
| --- | --- |
| API pública | [docs/localized/pt-BR/reference/public-api.md](../localized/pt-BR/reference/public-api.md) |
| CLI | [docs/localized/pt-BR/reference/cli.md](../localized/pt-BR/reference/cli.md) |
| Exemplos da CLI | [docs/localized/pt-BR/reference/cli-examples.md](../localized/pt-BR/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/pt-BR/reference/omsilaunchw.md](../localized/pt-BR/reference/omsilaunchw.md) |
| Bandeja do Windows | [docs/localized/pt-BR/reference/windows-tray.md](../localized/pt-BR/reference/windows-tray.md) |
| Perfis de sessão | [docs/localized/pt-BR/reference/session-profiles.md](../localized/pt-BR/reference/session-profiles.md) |
| Controle de runtime | [docs/localized/pt-BR/reference/runtime-control.md](../localized/pt-BR/reference/runtime-control.md) |
| Controle local / IPC | [docs/localized/pt-BR/reference/local-control.md](../localized/pt-BR/reference/local-control.md) |
| Capacidades | [docs/localized/pt-BR/reference/capabilities.md](../localized/pt-BR/reference/capabilities.md) |
| Erros e códigos de saída | [docs/localized/pt-BR/reference/errors.md](../localized/pt-BR/reference/errors.md), [docs/localized/pt-BR/reference/exit-codes.md](../localized/pt-BR/reference/exit-codes.md) |
| Empacotamento | [docs/localized/pt-BR/reference/packaging.md](../localized/pt-BR/reference/packaging.md) |
| Limitações conhecidas | [docs/localized/pt-BR/reference/known-limitations.md](../localized/pt-BR/reference/known-limitations.md) |
| Status de validação em runtime | [docs/localized/pt-BR/status/runtime-validation-status.md](../localized/pt-BR/status/runtime-validation-status.md) |

### Documentação em outros idiomas

A documentação está traduzida para 14 localidades em
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). As traduções
foram produzidas a partir das páginas canônicas em inglês (EUA) e validadas mecanicamente
em relação a elas. A revisão editorial por falantes nativos não fez parte do release do Beta 4
e poderá ocorrer após a publicação. Quando uma tradução e a página em inglês
divergirem, vale a página em inglês.

## Limitações e itens em aberto

Estas são as limitações mais importantes. A lista completa está em
[Limitações conhecidas](../localized/pt-BR/reference/known-limitations.md).

- **Steam.** O Steam LAA é `PARTIAL`: gameplay, leituras, comandos e parada
  precisam de uma instalação genuína do Steam e não foram validados em runtime.
- **Experimental neste beta:** iniciar a partir do último estado do mapa (`/last`)
  e data, hora, ano ou clima explícitos no início estão fechados estaticamente e
  aguardam validação em runtime. **Indisponível:** atribuição do veículo do jogador
  no início; solicitá-la torna o plano não apto para execução, em vez de ser
  ignorada silenciosamente.
- **As gravações em runtime são limitadas.** `weather.set`, `weather.metar.set`,
  gravações de variáveis de string e gatilhos de script são experimentais até serem
  validados em runtime; gravações de calendário e realocação de veículos estão
  indisponíveis. Alterações em runtime não são registradas no journal e não são
  restauradas.
- **Tempo de vida dos handles.** A detecção de handles obsoletos para veículos rodoviários e humanos que
  desaparecem naturalmente (RV-002) não tem produtor seguro em runtime e é validada
  somente offline.
- **Resultados paginados.** As listas de runtime são paginadas com `cursor` e `limit` e
  continuam em `next_cursor`; uma página que não cabe no mailbox de 64 KiB é
  encurtada e sinalizada com `truncated=true`.
- **A parada é forçada por padrão.** O OMSI é encerrado, o próprio encerramento do OMSI
  não é executado, e o estado não salvo do OMSI é perdido. O encerramento cooperativo opcional
  (`/cooperative-shutdown`, experimental) executa primeiro a rotina de saída do OMSI.
- **Modelo de confiança do mesmo usuário.** Qualquer processo do mesmo usuário do Windows pode acessar o
  plano de controle local.

## Download

Baixe o **`OmsiLaunch-0.1.0-beta4.zip`** e seu arquivo `.sha256` na
[página de Releases](https://github.com/lmonteirotech/OmsiLaunch/releases). Extraia-o
diretamente na raiz do OMSI suportada. O pacote contém o controlador
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), suas dependências, o conjunto de arquivos do plugin
permanente com o `release-manifest.json`, recursos de splash, um exemplo de sessão e a
documentação offline em `.omsilaunch\docs\`. Estrutura do pacote e remoção
limpa: [Empacotamento](../localized/pt-BR/reference/packaging.md).

## Compilação a partir do código-fonte

Requisitos:

- Windows 10 ou posterior, x64.
- SDK do .NET 6, com os runtimes .NET 6 x64 e x86 para executar os testes.
- Visual Studio com a carga de trabalho MSBuild C++ (conjunto de ferramentas de plataforma `v145`) e um
  Windows 10 SDK, para os três projetos nativos: `OmsiLaunch.Native.x86`
  (Win32), e `OmsiLaunch.Bootstrapper` e `OmsiLaunch.WindowsHost` (x64).

O `OmsiLaunch.sln` contém os projetos gerenciados e as suítes de teste. O único
ponto de entrada offline compila tudo e executa todas as suítes offline; ele nunca
inicia o OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` ignora os projetos nativos e a regressão de empacotamento.
`-SkipDocs` ignora os gates de documentação. A saída do build vai para `artifacts\`,
que não é versionado. `tools\New-ReleasePackage.ps1` prepara, calcula os hashes, valida
e compacta um pacote de release a partir de um build existente. Veja
[Empacotamento](../localized/pt-BR/reference/packaging.md) e
[Testes e validação](../project/TESTING-AND-VALIDATION.md).

| Caminho | Conteúdo |
| --- | --- |
| `src/` | Bibliotecas do produto: API, núcleo, configuração, conteúdo, interoperabilidade, processo, plugin, perfil de build, fronteira nativa x86 |
| `tools/` | CLI e host do Windows (`OmsiLaunch.Cli`), shims nativos (`OmsiLaunch.Bootstrapper`), host de teste offline, scripts de empacotamento e validação, ferramentas de localização |
| `tests/` | Suítes de teste unitário, de integração, de perfil, de UI do Windows e de documentação |
| `docs/` | Documentação canônica e suas traduções em `docs/localized/` |
| `examples/` | Exemplos de LaunchSpec e de sessão |
| `assets/` | Identidade visual, ícones e recursos do pacote |
| `third_party/` | Notas de procedência upstream |

Resumos para mantenedores: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Comunidade e licença

O OmsiLaunch é um projeto de código aberto voltado para a comunidade. Ele é independente de
outros launchers do OMSI, e ferramentas compatíveis da comunidade podem ser construídas sobre ele.

O OmsiLaunch é licenciado sob a [LGPL-3.0-only](../../LICENSE). Veja os
[avisos de terceiros](../../THIRD-PARTY-NOTICES.md) para a procedência do
código-fonte incorporado e os avisos aplicáveis.
