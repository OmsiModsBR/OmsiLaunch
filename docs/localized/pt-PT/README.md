# Documentação do OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Tradução da [página original em inglês](../../README.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta é a documentação normativa em inglês do OmsiLaunch `0.1.0-beta4`, a
base de referência posterior ao reforço (post-hardening). O OmsiLaunch fornece lançamento programável, gestão
da propriedade da sessão e controlo de runtime para um OMSI 2 instalado; cada capacidade nativa
é resolvida e verificada por build (ver [compatibilidade](reference/compatibility.md)). Cada página em `docs/` descreve o que o código atual
faz; quando uma página e o código divergem, prevalece o código e a página contém um erro.

Vocabulário de estabilidade usado em toda a documentação: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. As flags que são interpretadas mas não fazem nada estão marcadas como
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Nada é considerado
validado em runtime, a menos que o
[estado da validação em runtime](status/runtime-validation-status.md) o indique.

<a id="who-reads-what"></a>
## Quem lê o quê

| Público | Começar aqui | Depois |
| --- | --- | --- |
| Utilizadores (CLI, atalhos, perfis de sessão) | [Instalação](getting-started/installation.md), [Primeira sessão](getting-started/first-session.md) | [Referência da CLI](reference/cli.md), [Exemplos da CLI](reference/cli-examples.md), [Perfis de sessão](reference/session-profiles.md), [Área de notificação do Windows](reference/windows-tray.md), [Códigos de saída](reference/exit-codes.md) |
| Integradores (`OmsiLaunch.Api`, IPC local) | [Início rápido da API pública](getting-started/api-quick-start.md), [Referência da API pública](reference/public-api.md), [Referência do LaunchSpec](reference/launchspec.md) | [Ciclo de vida da sessão](concepts/session-lifecycle.md), [Controlo de runtime](reference/runtime-control.md), [Capacidades](reference/capabilities.md), [Controlo local / IPC](reference/local-control.md), [Referência de erros](reference/errors.md) |
| Responsáveis pela manutenção (lançamento, validação, limites) | [Empacotamento](reference/packaging.md), [Modelo de plugin permanente](concepts/permanent-plugin.md) | [Transações e recuperação](concepts/transactions-and-recovery.md), [Compatibilidade](reference/compatibility.md), [Limitações conhecidas](reference/known-limitations.md), [Estado da validação em runtime](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Navegação

| Página | Finalidade |
| --- | --- |
| [Primeiros passos](getting-started/first-session.md) | Planear, iniciar, observar e parar uma sessão a partir da raiz do OMSI. |
| [Instalação](getting-started/installation.md) | Pré-requisitos, extração do pacote para a raiz do OMSI, verificação com `/version`, remoção limpa. |
| [Início rápido da API pública](getting-started/api-quick-start.md) | Um programa .NET completo que planeia, inicia, lê e para uma sessão. |
| [Referência da CLI](reference/cli.md) | Todas as flags, palavras de comando e rotas hierárquicas de `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Exemplos da CLI](reference/cli-examples.md) | Linhas de comando prontas a copiar e colar para tarefas comuns. |
| [Referência do LaunchSpec](reference/launchspec.md) | Todas as propriedades e valores de enumeração de `LaunchSpec`, regras de carregamento JSON para `/spec`. |
| [Gestão de HOF](reference/hof-management.md) | Cópia de HOF com âmbito de sessão e espelhamento nas pastas de veículos: origens, destinos, conflitos, restauro. |
| [Módulos de runtime](../../reference/runtime-modules.md) | Módulos que correm dentro do OMSI com uma sessão: manifesto, confiança, ciclo de vida, políticas de falha, operações. |
| [Referência dos perfis de sessão](reference/session-profiles.md) | Esquema `omsilaunch.session-profile/v1` de `profile.yaml`, chaves, limites, precedência. |
| [Referência da API pública](reference/public-api.md) | `IOmsiLaunch`, records e enumerações públicos, estabilidade por membro. |
| [Inventário da API pública](reference/public-api-inventory.md) | Lista gerada de todos os tipos e membros públicos, com assinatura e estabilidade. |
| [Controlo de runtime](reference/runtime-control.md) | Canal de comandos de runtime, timeouts, handles, semântica de paragem. |
| [Referência de capacidades](reference/capabilities.md) | Catálogo de capacidades e todos os ids públicos de operações de runtime, com a respetiva classificação. |
| [Ciclo de vida da sessão](concepts/session-lifecycle.md) | Transições de `SessionState`, o que `StartSessionAsync` garante, como termina uma sessão. |
| [Transações e recuperação](concepts/transactions-and-recovery.md) | Estados do journal, cópias de segurança, verificação do restauro, eliminações da sessão, recuperação após falha. |
| [Modelo de plugin permanente](concepts/permanent-plugin.md) | O conjunto fechado `plugins\OmsiLaunch.*`, integridade baseada no manifesto, o que uma sessão nunca toca. |
| [Controlo local / IPC](reference/local-control.md) | Protocolo de named pipe `0.1`, endpoint por instalação, vinculação a `session_id`, modelo de confiança. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | O host Windows (sem consola): diferenças em relação a `OmsiLaunch.exe`, `/silent`, caixas de diálogo, códigos de saída. |
| [Área de notificação do Windows](reference/windows-tray.md) | Indicador na área de notificação: ícone, menu, janela de estado campo a campo, End session, reinício do Explorador. |
| [Referência de erros](reference/errors.md) | Todos os códigos `OL_E_*` / `OL_W_*`, com categoria e significado. |
| [Códigos de saída](reference/exit-codes.md) | Valores de `PublicExitCode` de 0 a 10 e códigos do shim do bootstrapper de 100 a 106. |
| [Empacotamento / estrutura da instalação](reference/packaging.md) | Ficheiros no ZIP de lançamento, `release-manifest.json`, estrutura de `.omsilaunch\`. |
| [Compatibilidade / builds do OMSI suportadas](reference/compatibility.md) | O único hash de `Omsi.exe` suportado, o hash Steam LAA aceite, requisitos de plataforma. |
| [Limitações conhecidas](reference/known-limitations.md) | O que não é suportado, é parcial ou constitui um risco aceite nesta beta. |
| [Estado da validação em runtime](status/runtime-validation-status.md) | O que foi executado sob o OMSI, o que só foi executado offline, o que ainda precisa de uma sessão real. |

Páginas que se mantêm normativas para os responsáveis pela manutenção: o
[`README.md`](../../../README.md) do repositório, as especificações em `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) e as páginas do projeto em
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Estas resumem; as
páginas acima são a referência detalhada. As páginas históricas estão listadas no
[manifesto da documentação](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Como esta documentação é mantida sincronizada

Um gate de documentação (verificação obrigatória), `tests\OmsiLaunch.DocumentationTests`, é compilado contra
`OmsiLaunch.Api` e `OmsiLaunch.Core` e compara as páginas acima com o
código que define a superfície pública:

| Gate | Verifica |
| --- | --- |
| `docs.cli-flags` | Cada entrada de `CliInput.KnownFlags` aparece na referência da CLI como `` `/flag` `` ou `` `/flag:` ``; cada entrada de `CliInput.AcceptedNoEffectFlags` está marcada como `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` na sua linha; cada palavra de comando e cada rota de `CliInput.HierarchicalRoutes` aparece com a respetiva operação de runtime; cada valor de `PublicExitCode` tem uma linha `| n |` na tabela de códigos de saída. |
| `docs.capabilities` | Cada id de `PublicCapabilityRegistry.All`, cada entrada de `PublicCapabilityRegistry.PublicRuntimeOperationIds` e cada nome de `PublicCapabilityClassification` aparece na referência de capacidades. |
| `docs.errors` | Cada código de `PublicErrorCodes.All` aparece na referência de erros, e nenhum literal `OL_E_*` / `OL_W_*` em `src\` ou `tools\OmsiLaunch.Cli\` está ausente de `PublicErrorCodes`. |
| `docs.public-api` | Cada tipo exportado de `OmsiLaunch.Api`, cada valor de enumeração e cada membro de `IOmsiLaunch` aparece na referência da API pública, e as cinco palavras de estabilidade são todas usadas. |
| `docs.launchspec` | Cada propriedade pública alcançável a partir de `LaunchSpec` e cada valor das suas enumerações aparece na referência do LaunchSpec. |
| `docs.session-profiles` | Cada chave de `SessionProfileCompiler.SchemaKeys`, o identificador do esquema e o limite de `256 KiB` aparecem na referência dos perfis de sessão. |
| `docs.structure` | Cada página da tabela de navegação existe. |
| `docs.links` | Cada ligação relativa em `docs\**\*.md` (excluindo `docs\localized\`) e nos ficheiros `*.md` da raiz resolve para um ficheiro ou diretório. |
| `docs.localization` | Cada locale listado em `docs\localized\LOCALIZATION-MANIFEST.md` tem todas as páginas do conjunto localizado; cada página mantém os cabeçalhos, as tabelas e os blocos de código da página inglesa, todos os spans de código inline (flags, ids de capacidades e de operações, códigos de erro, chaves, identificadores) e todas as ligações, e as suas ligações relativas resolvem. |

O gate é uma das suites executadas por `tools\Invoke-OfflineValidation.ps1`
(pode ser ignorado com `-SkipDocs`). É executado offline, nunca lança o OMSI e faz falhar a
build quando uma flag, rota, capacidade, código de erro, valor de enumeração ou tipo público não está
documentado ou quando uma ligação está partida. Não verifica a prosa, pelo que uma página pode ainda
estar errada quanto ao comportamento; nesse caso, deve reportar-se um erro contra a página.

<a id="translations"></a>
## Traduções

`docs\localized\<locale>\` contém traduções completas desta documentação `0.1.0-beta4`
para `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` e `ja-JP`. O conjunto de páginas, as raízes dos locales e as
páginas que intencionalmente não são traduzidas estão listados em
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
As traduções mantêm inalterados todos os comandos, flags, identificadores, códigos de erro e exemplos
das páginas inglesas, e o gate `docs.localization` verifica-o.
As páginas inglesas continuam a ser a fonte normativa: quando uma tradução diverge
delas, a página inglesa e o código prevalecem, e a tradução
contém um erro.
