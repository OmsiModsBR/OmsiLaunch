# Documentação do OmsiLaunch

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Tradução da [página original em inglês](../../README.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta é a documentação normativa em inglês do OmsiLaunch `0.1.0-beta4`, a
linha de base pós-endurecimento (post-hardening). O OmsiLaunch oferece inicialização programável,
propriedade de sessão e controle de runtime para um OMSI 2 instalado; cada capacidade nativa
é resolvida e verificada por build (veja [compatibilidade](reference/compatibility.md)). Cada página em `docs/` descreve o que o código atual
faz; quando uma página e o código divergem, vale o código e a página contém um bug.

Vocabulário de estabilidade usado em toda a documentação: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Flags que são interpretadas, mas não fazem nada, são marcadas como
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Nada é considerado
validado em runtime, a menos que o
[status de validação em runtime](status/runtime-validation-status.md) o diga.

<a id="who-reads-what"></a>
## Quem lê o quê

| Público | Comece aqui | Depois |
| --- | --- | --- |
| Usuários (CLI, atalhos, perfis de sessão) | [Instalação](getting-started/installation.md), [Primeira sessão](getting-started/first-session.md) | [Referência da CLI](reference/cli.md), [Exemplos da CLI](reference/cli-examples.md), [Perfis de sessão](reference/session-profiles.md), [Bandeja do Windows](reference/windows-tray.md), [Códigos de saída](reference/exit-codes.md) |
| Integradores (`OmsiLaunch.Api`, IPC local) | [Início rápido da API pública](getting-started/api-quick-start.md), [Referência da API pública](reference/public-api.md), [Referência do LaunchSpec](reference/launchspec.md) | [Ciclo de vida da sessão](concepts/session-lifecycle.md), [Controle de runtime](reference/runtime-control.md), [Capacidades](reference/capabilities.md), [Controle local / IPC](reference/local-control.md), [Referência de erros](reference/errors.md) |
| Mantenedores (release, validação, limites) | [Empacotamento](reference/packaging.md), [Modelo de plugin permanente](concepts/permanent-plugin.md) | [Transações e recuperação](concepts/transactions-and-recovery.md), [Compatibilidade](reference/compatibility.md), [Limitações conhecidas](reference/known-limitations.md), [Status de validação em runtime](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Navegação

| Página | Finalidade |
| --- | --- |
| [Primeiros passos](getting-started/first-session.md) | Planejar, iniciar, observar e parar uma sessão a partir da raiz do OMSI. |
| [Instalação](getting-started/installation.md) | Pré-requisitos, extração do pacote na raiz do OMSI, verificação com `/version`, remoção limpa. |
| [Início rápido da API pública](getting-started/api-quick-start.md) | Um programa .NET completo que planeja, inicia, lê e para uma sessão. |
| [Referência da CLI](reference/cli.md) | Todas as flags, palavras de comando e rotas hierárquicas de `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [Exemplos da CLI](reference/cli-examples.md) | Linhas de comando prontas para copiar e colar para tarefas comuns. |
| [Referência do LaunchSpec](reference/launchspec.md) | Todas as propriedades e valores de enum de `LaunchSpec`, regras de carregamento de JSON para `/spec`. |
| [Gerenciamento de HOF](reference/hof-management.md) | Cópia e espelhamento de HOF com escopo de sessão para as pastas de veículos: origens, destinos, conflitos, restauração. |
| [Módulos de runtime](reference/runtime-modules.md) | Módulos que executam dentro do OMSI junto com uma sessão: manifesto, confiança, ciclo de vida, políticas de falha, operações. |
| [Referência de perfis de sessão](reference/session-profiles.md) | Esquema `omsilaunch.session-profile/v1` de `profile.yaml`, chaves, limites, precedência. |
| [Referência da API pública](reference/public-api.md) | `IOmsiLaunch`, records e enums públicos, estabilidade por membro. |
| [Inventário da API pública](reference/public-api-inventory.md) | Lista gerada de todos os tipos e membros públicos, com assinatura e estabilidade. |
| [Controle de runtime](reference/runtime-control.md) | Canal de comandos de runtime, timeouts, handles, semântica de parada. |
| [Referência de capacidades](reference/capabilities.md) | Catálogo de capacidades e todos os ids de operação de runtime públicos com sua classificação. |
| [Ciclo de vida da sessão](concepts/session-lifecycle.md) | Transições de `SessionState`, o que `StartSessionAsync` promete, como uma sessão termina. |
| [Transações e recuperação](concepts/transactions-and-recovery.md) | Estados do journal, backups, verificação da restauração, exclusões da sessão, recuperação após falha. |
| [Modelo de plugin permanente](concepts/permanent-plugin.md) | O conjunto de arquivos `plugins\OmsiLaunch.*`, integridade baseada no manifesto, o que uma sessão nunca toca. |
| [Controle local / IPC](reference/local-control.md) | Protocolo de named pipe `0.1`, endpoint por instalação, vinculação a `session_id`, modelo de confiança. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | O host Windows (sem console): diferenças em relação ao `OmsiLaunch.exe`, `/silent`, caixas de diálogo, códigos de saída. |
| [Bandeja do Windows](reference/windows-tray.md) | Indicador na área de notificação: ícone, menu, janela de status campo a campo, encerrar a sessão, reinício do Explorer. |
| [Referência de erros](reference/errors.md) | Todos os códigos `OL_E_*` / `OL_W_*` com categoria e significado. |
| [Códigos de saída](reference/exit-codes.md) | Valores de `PublicExitCode` de 0 a 10 e códigos do shim de bootstrap de 100 a 106. |
| [Empacotamento / layout da instalação](reference/packaging.md) | Arquivos no ZIP de release, `release-manifest.json`, layout de `.omsilaunch\`. |
| [Compatibilidade / builds do OMSI suportados](reference/compatibility.md) | O único hash de `Omsi.exe` suportado, o hash Steam LAA aceito, requisitos de plataforma. |
| [Limitações conhecidas](reference/known-limitations.md) | O que não é suportado, é parcial ou é um risco aceito nesta beta. |
| [Status de validação em runtime](status/runtime-validation-status.md) | O que foi executado sob o OMSI, o que foi executado somente offline, o que ainda precisa de uma sessão real. |

Páginas que continuam normativas para os mantenedores: o
[`README.md`](../../../README.md) do repositório, as especificações em `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) e as páginas do projeto em
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Elas resumem; as
páginas acima são a referência detalhada. As páginas históricas estão listadas no
[manifesto da documentação](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Como esta documentação é mantida sincronizada

Um gate de documentação, `tests\OmsiLaunch.DocumentationTests`, é compilado contra
`OmsiLaunch.Api` e `OmsiLaunch.Core` e compara as páginas acima com o
código que define a superfície pública:

| Gate | Verifica |
| --- | --- |
| `docs.cli-flags` | Toda entrada de `CliInput.KnownFlags` aparece na referência da CLI como `` `/flag` `` ou `` `/flag:` ``; toda entrada de `CliInput.AcceptedNoEffectFlags` é marcada como `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` em sua linha; toda palavra de comando e toda rota de `CliInput.HierarchicalRoutes` aparece com sua operação de runtime; todo valor de `PublicExitCode` tem uma linha `| n |` na tabela de códigos de saída. |
| `docs.capabilities` | Todo id de `PublicCapabilityRegistry.All`, toda entrada de `PublicCapabilityRegistry.PublicRuntimeOperationIds` e todo nome de `PublicCapabilityClassification` aparece na referência de capacidades. |
| `docs.errors` | Todo código de `PublicErrorCodes.All` aparece na referência de erros, e nenhum literal `OL_E_*` / `OL_W_*` em `src\` ou `tools\OmsiLaunch.Cli\` está ausente de `PublicErrorCodes`. |
| `docs.public-api` | Todo tipo exportado de `OmsiLaunch.Api`, todo valor de enum e todo membro de `IOmsiLaunch` aparece na referência da API pública, e as cinco palavras de estabilidade são usadas. |
| `docs.launchspec` | Toda propriedade pública alcançável a partir de `LaunchSpec` e todo valor de seus enums aparece na referência do LaunchSpec. |
| `docs.session-profiles` | Toda chave de `SessionProfileCompiler.SchemaKeys`, o identificador do esquema e o limite de `256 KiB` aparecem na referência de perfis de sessão. |
| `docs.structure` | Toda página da tabela de navegação existe. |
| `docs.links` | Todo link relativo em `docs\**\*.md` (exceto `docs\localized\`) e nos arquivos `*.md` da raiz aponta para um arquivo ou diretório existente. |
| `docs.localization` | Toda localidade listada em `docs\localized\LOCALIZATION-MANIFEST.md` tem todas as páginas do conjunto localizado; cada página mantém os títulos, as tabelas e os blocos de código da página em inglês, todo trecho de código inline (flags, ids de capacidade e de operação, códigos de erro, chaves, identificadores) e todo link, e seus links relativos apontam para destinos existentes. |

O gate é uma das suítes executadas por `tools\Invoke-OfflineValidation.ps1`
(ignore-o com `-SkipDocs`). Ele é executado offline, nunca inicia o OMSI e faz o
build falhar quando uma flag, rota, capacidade, código de erro, valor de enum ou tipo público
não está documentado ou quando um link está quebrado. Ele não verifica a prosa, então uma página ainda pode
estar errada sobre o comportamento; relate isso como um bug da página.

<a id="translations"></a>
## Traduções

`docs\localized\<locale>\` contém traduções completas desta documentação `0.1.0-beta4`
para `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` e `ja-JP`. O conjunto de páginas, as raízes das localidades e as
páginas intencionalmente não traduzidas estão listados em
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
As traduções mantêm inalterados todos os comandos, flags, identificadores, códigos de erro e exemplos
das páginas em inglês, e o gate `docs.localization` verifica isso.
As páginas em inglês continuam sendo a fonte normativa: onde uma tradução divergir
delas, a página em inglês e o código prevalecem, e a tradução
contém um bug.
