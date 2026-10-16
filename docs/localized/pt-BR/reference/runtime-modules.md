# Módulos de runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=faf61b29436d1f69013b0c49667534ef079e31f7b34300b538935788654a55df -->
> Tradução da [página original em inglês](../../../reference/runtime-modules.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Um módulo de runtime adiciona comportamento a uma sessão gerenciada do OMSI: um recurso que roda junto com a sessão (por exemplo, o HUD de um mapa ou otimizações de carregamento), sem se tornar parte do próprio OmsiLaunch. A ponte o carrega, entrega a ele somente o que ele declara e remove tudo o que ele mantém quando a sessão termina ou o módulo falha. Esta página é a referência normativa dos módulos de runtime como implementados por `src/OmsiLaunch.Runtime.Modules` (manifesto, configurações, elegibilidade, host de ciclo de vida), pelo planejador, pelos programas de sessão do controlador (`ModuleSessionPrograms`, `ModuleSessionProcess`), pelo ambiente de módulos do plugin e pelas flags de módulo da [CLI](cli.md).

Estabilidade: `EXPERIMENTAL`. O protocolo de ponte 1 foi validado em runtime na rodada B (módulos gerenciados, ativação por conteúdo, operações, `disable-module` e `fail-session`; componentes nativos com hooks de IAT; veja o [status de validação em runtime](../status/runtime-validation-status.md)). O protocolo de ponte 2 (configurações, ativação padrão, o arquivo de configurações, programas de sessão, conflitos de plugin, avisos, `get_value`) é validado somente offline. Gates offline: os gates `module.*` de `tests/OmsiLaunch.ModuleTests` (manifesto, integridade, seleção e ativação, configurações, o arquivo de configurações, conflitos de plugin, programas de sessão iniciados por meio do seu job, uma falha injetada em cada fase sob cada política de falha, ciclo de vida do componente nativo, limpeza, handoff), `plugin-runtime.modules-lifecycle` (um módulo gerenciado real carregado pelo ambiente do plugin), `runtime-modules.native-component` (um componente nativo real fazendo hook em uma importação e lendo valores pela ponte), `runtime-modules.plugin-conflict` e `native-hooks.self-check` (o mecanismo de hooks).

O carregamento é aberto: qualquer módulo instalado abaixo de `plugins\OmsiLaunch.Modules` pode ser carregado, seja quem for o autor. O OmsiLaunch não garante o que um módulo faz dentro do OMSI. O que ele garante é o [limite de segurança](#security-boundary): os módulos nunca rodam dentro do processo do OmsiLaunch, e nada do que o OmsiLaunch oferece permite que um módulo ultrapasse os direitos do próprio OMSI.

<a id="where-modules-live"></a>
## Onde ficam os módulos

Cada módulo é um diretório abaixo da pasta do plugin permanente:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

O nome do diretório deve ser igual ao `id` do módulo. Os módulos são instalados por quem os distribui; uma sessão nunca grava, faz snapshot nem restaura arquivos ali, e os módulos nunca alteram arquivos da instalação por meio do OmsiLaunch (um manifesto não tem como declarar um overlay de instalação). Os dados graváveis de um módulo ficam no seu [diretório de dados](#data-directory).

<a id="manifest-modulejson"></a>
## Manifesto (`module.json`)

Esquema `omsilaunch.runtime-module/v1`. Chaves desconhecidas são rejeitadas, de modo que um manifesto escrito para um esquema posterior é recusado em vez de ser entendido pela metade. Todos os caminhos são relativos ao diretório do módulo (os caminhos de conteúdo vinculado são relativos à raiz do OMSI), usam `\` ou `/` e não podem conter `..` nem uma unidade.

| Chave | Obrigatória | Significado |
| --- | --- | --- |
| `schema` | sim | `omsilaunch.runtime-module/v1`. |
| `id` | sim | Id do módulo: letras minúsculas, dígitos e `-`, de 2 a 48 caracteres, começando com letra ou dígito. É também o nome do seu diretório e o seu namespace de operações. |
| `version` | sim | `major.minor.patch`, opcionalmente `-suffix`. |
| `name` | não | Nome de exibição (no máximo 128 caracteres). |
| `bridge_protocol` | sim | `{ "min": n, "max": n }`: os protocolos de ponte com os quais o módulo funciona. Este build atende aos protocolos `1` e `2`; um módulo é aceito quando seu intervalo inclui um deles. |
| `activation` | não | `requested` (padrão): carregado quando a sessão o solicita. `content`: também carregado quando a sessão executa o mapa vinculado (exige `content_binding`). `default` (protocolo 2): carregado em toda sessão, a menos que seja desativado (veja [escolha dos módulos de uma sessão](#choosing-modules-for-a-session)); não pode ter `content_binding`. |
| `required_capabilities` | não | Capacidades sem as quais o módulo não funciona (veja [capacidades](#capabilities)). |
| `optional_capabilities` | não | Capacidades que o módulo usa quando disponíveis; ele as verifica com `IsCapabilityAvailable`. |
| `content_binding` | não | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: o módulo só roda nesse mapa, com esses arquivos inalterados. |
| `assets` | não | Arquivos que o módulo lê, cada um `{ "path", "sha256" }`. |
| `components` | sim | Exatamente um componente `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; opcionalmente um componente `native` (`.dll`), componentes `auxiliary` (`.exe`) e, com o protocolo 2, um componente `host` (`.exe`, o [programa de sessão](#session-programs) do módulo). |
| `failure_policy` | sim | `fail-session`, `disable-module` ou `degrade` (veja [falhas](#failures)). |
| `operations` | não | Operações que o módulo expõe enquanto a sessão roda: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | não | Protocolo 2. As configurações que o módulo aceita (veja [configurações](#settings)). |
| `conflicts` | não | Protocolo 2. `{ "plugins": [ "<file>.dll" ] }`: DLLs de plugin do OMSI que não devem rodar junto com o módulo (veja [conflitos](#conflicts-with-omsi-plugins)). |
| `timeouts` | não | Orçamentos em milissegundos: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (padrão 5000 cada, no máximo 30000); com o protocolo 2, também `prepare_ms` (padrão 120000, no máximo 1800000) e `host_stop_ms` (padrão 10000, no máximo 60000) para o programa de sessão. As operações têm padrão de 2000 ms. |
| `diagnostics` | não | `{ "trace": true\|false }`. |

Um manifesto que usa um recurso do protocolo 2 (`settings`, `conflicts`, ativação `default`, um componente `host`, `prepare_ms` ou `host_stop_ms`) deve declarar `bridge_protocol.min` 2.

Todo arquivo citado em `components` e `assets` é fixado pelo seu SHA-256. O `module.json` não pode fixar a si mesmo.

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## Integridade

Todo arquivo fixado deve existir, não pode ser alcançado por uma junction ou link simbólico e deve ter o SHA-256 fixado; caso contrário, o módulo é recusado com `OL_E_MODULE_INTEGRITY`. Isso detecta um módulo corrompido ou atualizado pela metade. Não é uma decisão de confiança: um módulo não é verificado contra nenhuma lista, assinatura ou publicador.

A verificação é repetida no ponto em que o código do módulo é iniciado: o plugin só carrega um módulo se o seu `module.json` ainda tiver o SHA-256 que o plano avaliou e todas as fixações ainda forem válidas, e o controlador só inicia um programa de sessão depois de verificar a fixação desse programa.

<a id="security-boundary"></a>
## Limite de segurança

O OmsiLaunch não controla o que um módulo faz dentro do OMSI: seus componentes gerenciados e nativos rodam no processo do OMSI com os mesmos direitos de qualquer plugin do OMSI. O OmsiLaunch garante o seguinte:

1. O processo do OmsiLaunch nunca carrega código de módulo. O código do módulo roda dentro do OMSI, em um programa auxiliar iniciado de dentro do OMSI ou no programa de sessão do módulo.
2. Os programas que o OmsiLaunch inicia para os módulos são arquivos do diretório do próprio módulo, verificados contra a sua fixação e iniciados com uma linha de comando que o OmsiLaunch determina. Um programa de sessão roda em um objeto job que o encerra junto com a sessão, não permite um segundo processo (ele não pode iniciar outros programas) e recusa alterações em parâmetros do sistema, configurações de vídeo, na área de trabalho e em logoff ou desligamento. Ele herda apenas os seus dois handles de pipe.
3. Tudo o que um módulo declara permanece dentro do seu diretório, e seus dados graváveis dentro de `.omsilaunch\module-data\<id>`.
4. Os canais do OmsiLaunch (controle local, startup handoff, telemetria, comandos de runtime) permanecem restritos ao usuário atual e vinculados à sessão; nenhum deles permite que um chamador inicie um programa, leia ou grave arquivos fora da instalação ou altere configurações do sistema.
5. O OmsiLaunch nunca eleva privilégios. Um módulo roda com os direitos do usuário que iniciou a sessão.
6. Texto que vem de um módulo (avisos, eventos, valores) é tratado como dado: limitado, sem caracteres de controle e nunca executado, aberto ou seguido como link.

<a id="capabilities"></a>
## Capacidades

`required_capabilities` e `optional_capabilities` nomeiam uma capacidade nativa do build em execução (a coluna de capacidade nativa do [catálogo de capacidades](capabilities.md#capability-catalog), por exemplo `time.read` ou `d3d.texture`) ou um recurso da ponte:

| Recurso da ponte | O que o módulo recebe |
| --- | --- |
| `bridge.events` | `Emit`: eventos de runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: threads em segundo plano rastreados. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: processos auxiliares vinculados à sessão. |
| `bridge.hooks.iat` | Componente nativo: hooks em importações do `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Componente nativo: hooks em métodos do dispositivo Direct3D 9 em execução (`d3d9:device.<method>`). Exige a capacidade nativa `d3d.texture`. |

Um nome que não seja nenhum dos dois é recusado no planejamento (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). Em um build do OMSI 2 sem layout conhecido, uma capacidade nativa que o arquivo executável não resolve é decidida em processo; se não for resolvida ali, o módulo não é carregado.

<a id="choosing-modules-for-a-session"></a>
## Escolha dos módulos de uma sessão

Um módulo entra em uma sessão de uma de quatro maneiras; `PlannedModule.Activation` informa qual:

| Ativação | Quando |
| --- | --- |
| `requested` | A sessão o solicita: `LaunchSpec.Modules.Enable`, `/module:<id>` ou `enabled: true` na predefinição de perfil de sessão selecionada. |
| `settings-file` | `enabled=true` na sua seção do [arquivo de configurações](#the-settings-file). |
| `content` | Sua ativação é `content` e a sessão executa o mapa vinculado a ele. |
| `default` | Sua ativação é `default`. |

Um módulo é mantido fora da sessão por `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` na predefinição ou `enabled=false` no arquivo de configurações (diagnóstico do plano `module.disabled`). Uma solicitação explícita prevalece sobre o arquivo de configurações. Ativar e desativar o mesmo módulo em uma única solicitação é `OL_E_MODULE_SETTING_INVALID`.

Regras de planejamento: um módulo solicitado que não pode ser carregado torna o plano não apto para execução, com o seu código `OL_E_MODULE_*`. Um módulo que entrou por conta própria (arquivo de configurações, conteúdo, padrão) e não pode ser carregado não interrompe a sessão: o plano informa `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Configurações

Um módulo declara suas configurações em `settings`; cada entrada:

| Chave | Obrigatória | Significado |
| --- | --- | --- |
| `key` | sim | `[a-z][a-z0-9_]{0,31}`, diferente de `enabled`. |
| `type` | sim | `boolean`, `integer`, `choice` ou `text`. |
| `default` | sim | O valor quando nada mais o define: um booleano, um inteiro, uma string ou `"auto"` quando `auto` é true. |
| `auto` | não | `true`: o valor `auto` é aceito e repassado ao módulo, que decide em tempo de execução. |
| `min`, `max` | não | Limites inteiros. |
| `choices` | para `choice` | Os valores aceitos (`[a-z0-9][a-z0-9_.-]{0,31}`, diferente de `auto`). |
| `max_length` | não | Limite de comprimento do texto (1..4096, padrão 256). Caracteres de controle são recusados. |
| `flag` | não | Uma flag de linha de comando para esta configuração, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, ou apenas `/<flag>` para `true` em um booleano. |
| `description` | não | No máximo 256 caracteres. |

Os valores são normalizados: booleanos aceitam `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; as opções (choices) são comparadas sem diferenciar maiúsculas de minúsculas; os inteiros são verificados contra seus limites.

O valor efetivo de cada configuração é, em ordem:

1. a solicitação: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, uma flag própria do módulo ou `modules.<id>.settings` na predefinição de perfil de sessão selecionada. Como para qualquer outro campo de predefinição, a linha de comando não pode também definir uma chave que a predefinição define (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. o [arquivo de configurações](#the-settings-file);
3. o padrão do manifesto.

Um valor solicitado inválido, ou uma chave solicitada que o módulo não declara, torna o plano não apto para execução (`OL_E_MODULE_SETTING_INVALID`). Uma entrada do arquivo de configurações que não pode ser usada é ignorada com `OL_W_MODULE_SETTING_IGNORED`, assim como as configurações solicitadas para um módulo que a sessão não carrega. O plano lista cada valor efetivo com a sua origem (`request`, `settings-file` ou `default`) em `PlannedModule.Settings`.

O módulo recebe todas as chaves declaradas com o seu valor efetivo: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` em um componente nativo e o objeto `settings` do contexto de um programa de sessão.

<a id="the-settings-file"></a>
### O arquivo de configurações

`<OMSI root>\.omsilaunch\modules.ini`, ao lado dos perfis de sessão. O OmsiLaunch apenas o lê.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

As seções são ids de módulos; linhas `key=value` definem configurações, `enabled` liga ou desliga o módulo em toda sessão; linhas que começam com `;` ou `#` são comentários. O arquivo pode ter no máximo 256 KiB. Tudo o que ele não consegue usar (uma seção que não é um id de módulo, uma linha fora de uma seção, uma chave desconhecida, um valor inválido) é informado com `OL_W_MODULE_SETTING_IGNORED` e ignorado.

<a id="session-programs"></a>
## Programas de sessão

Um módulo pode declarar um componente `host`: o seu programa de sessão, um executável do seu próprio diretório. Quando o módulo está no plano, o controlador:

1. verifica a fixação do programa e cria o diretório de dados do módulo;
2. o inicia antes do OMSI, a partir do diretório do módulo, com a linha de comando `"<program>" --omsilaunch-session <session id>`, suspenso até estar em seu job (veja o [limite de segurança](#security-boundary));
3. grava o contexto como a primeira linha de sua entrada padrão;
4. espera até que o programa grave `{"type":"ready"}`, no máximo `prepare_ms`; só então inicia o OMSI;
5. depois que o OMSI foi criado, grava `{"type":"omsi-started","pid":<process id>}`;
6. ao fim da sessão, depois que o OMSI saiu e antes de qualquer arquivo ser restaurado, grava `{"type":"stop"}`, espera no máximo `host_stop_ms` e então encerra o programa.

Os dois sentidos usam JSON em UTF-8, um objeto por linha. O contexto:

| Campo | Significado |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identidade. |
| `installation_root`, `module_directory`, `data_directory` | Caminhos completos. |
| `channel` | O named pipe que o programa pode servir para os componentes do módulo no OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. O programa o cria, restrito ao usuário atual, e verifica que o cliente é o processo do OMSI. Os componentes o leem com `get_value("session_channel")` ou `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | As configurações efetivas do módulo. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` da sessão. |
| `session_files` | Arquivos relativos à instalação que pertencem à transação da sessão. O programa não deve mantê-los abertos nem mapeados: eles são restaurados quando a sessão termina. |

O que o programa pode gravar além de `ready`:

| Linha | Efeito |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Evento de runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Evento de runtime `module.<id>.notice` (veja [avisos](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Diagnóstico da sessão com esse código catalogado (`OL_E_MODULE_OPERATION_FAILED` caso contrário). |

Outras linhas, linhas maiores que 64 KiB e tudo o que vier após 10,000 linhas são ignorados. O controlador registra `module.<id>.host.started`, `host.ready` (com `prepare_ms`), `host.failed`, `host.exited` e `host.killed`.

Um programa que não fica pronto dentro de `prepare_ms`, sai antes de ficar pronto ou cuja fixação não é válida faz a preparação do seu módulo falhar: sob `fail-session`, a sessão não inicia (`OL_E_MODULE_SESSION_FAILED`); sob `disable-module`, o programa é encerrado e o módulo fica fora do OMSI; sob `degrade`, o módulo permanece (e o programa continua rodando, se ainda puder). O `StartupTimeoutSeconds` da sessão conta a partir do momento em que o OMSI é iniciado, portanto a preparação não o consome.

<a id="conflicts-with-omsi-plugins"></a>
## Conflitos com plugins do OMSI

`conflicts.plugins` nomeia DLLs de plugin do OMSI que não devem rodar junto com o módulo, por exemplo um plugin mais antigo que instala os mesmos hooks. No planejamento, o OmsiLaunch lê a seção `[dll]` de cada `plugins\*.opl`; se algum carrega uma DLL em conflito, o módulo é recusado com `OL_E_MODULE_CONFLICT` e o motivo nomeia o `.opl`. Dentro do OMSI, um módulo cuja DLL em conflito foi carregada mesmo assim não é carregado. O OmsiLaunch nunca altera arquivos de outro plugin; desativá-lo é uma decisão do usuário.

<a id="notices"></a>
## Avisos

Um evento de módulo chamado `notice` (de `Emit("notice", ...)`, do `emit` de um componente nativo ou da linha de aviso de um programa de sessão) é exibido pela bandeja do OmsiLaunch como um balão cujo título nomeia o módulo. Somente `title` (no máximo 63 caracteres) e `text` (no máximo 255 caracteres) são mantidos, os caracteres de controle são removidos e nada nele é aberto ou executado. Um programa de sessão que precisa informar o usuário antes de o OMSI iniciar (quando a bandeja ainda não é exibida) usa a sua própria janela.

<a id="data-directory"></a>
## Diretório de dados

`<OMSI root>\.omsilaunch\module-data\<id>\` é o diretório gravável do módulo para esta instalação: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` e o `data_directory` do programa de sessão. Ele é criado no primeiro uso. Não faz parte da transação da sessão: o que um módulo grava ali permanece depois da sessão. `/clear-module-data:<id>` o exclui (e nada mais); é recusado enquanto uma sessão possui a instalação, e um diretório alcançado por um link é removido como link, nunca seguido.

<a id="lifecycle"></a>
## Ciclo de vida

| Etapa | Onde | O que acontece |
| --- | --- | --- |
| 1. Descoberta | controlador | `plugins\OmsiLaunch.Modules\*` é lido: manifesto e integridade (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Elegibilidade | controlador, no planejamento | Os módulos que [entram na sessão](#choosing-modules-for-a-session) são avaliados: utilizável, protocolo de ponte, capacidades, vínculo de conteúdo, conflitos de plugin, configurações. O resultado é `SessionPlan.PlannedModules`. |
| 3. Preparação | controlador, no início | Os [programas de sessão](#session-programs) dos módulos são iniciados e aguardados. Em seguida, os módulos elegíveis seguem para o plugin no startup handoff (versão 6): seus diretórios, o SHA-256 de cada `module.json` avaliado e suas configurações efetivas. |
| 4. Armar | plugin, antes de o mundo iniciar | Cada módulo é reverificado e carregado (componente gerenciado em seu próprio contexto de carga). Seu componente nativo, se houver, é carregado e iniciado primeiro; depois `Arm(context)` é chamado. |
| 5. Gameplay pronto | plugin | Após `gameplay.entered`, quando um módulo carregado declara `bridge.hooks.d3d9`, a ponte começa a observar o dispositivo Direct3D (`d3d.ready`); depois chama o `gameplay_ready` do componente nativo e `GameplayReady()`. Em uma sessão NEW_MAP, o mapa vinculado é verificado novamente contra o mapa que o início carregou. |
| 6. Runtime | plugin | As operações `module.<id>.<operation>` chegam a `Execute`. |
| 7. Parada | plugin, depois controlador | Quando a sessão termina (ou o módulo falha), `Stop()` é chamado. Depois que o OMSI saiu, os programas de sessão são parados. |
| 8. Limpeza | plugin | Tudo o que o módulo rastreou é liberado em ordem inversa e verificado como removido; o componente nativo é parado por último e seus hooks são removidos; sua DLL permanece carregada, inerte, até o OMSI sair (um thread ainda pode estar retornando por um detour). |

Todo callback em processo roda no thread de UI do OMSI, como as operações de runtime do próprio produto. A ponte mede cada callback contra o seu orçamento: um callback que retorna tarde faz a sua fase falhar (`OL_E_MODULE_PHASE_TIMEOUT`). A ponte não consegue interromper um callback que nunca retorna; um módulo assim trava o OMSI, e a sessão termina no seu timeout de inicialização ou por `session stop`, como qualquer outra sessão.

<a id="failures"></a>
## Falhas

Uma falha é uma exceção ou um estouro de orçamento em qualquer fase ou operação, uma capacidade não resolvida em processo, um vínculo de conteúdo que não se sustenta em processo, um plugin em conflito carregado no OMSI ou um programa de sessão que não fica pronto. A `failure_policy` do módulo decide o que se segue. Em todos os casos, os recursos do módulo são liberados.

| Política | Falha em preparação, carga, armar ou gameplay pronto | Falha em uma operação | Falha na parada |
| --- | --- | --- | --- |
| `fail-session` | Módulo liberado; `module.session-failure`; a sessão falha com `OL_E_MODULE_SESSION_FAILED`, o OMSI é parado e os arquivos são restaurados. Uma falha antes de o mundo iniciar impede o início. | Igual. | Informada (`module.failed`); a sessão continua até o fim. |
| `disable-module` | Módulo liberado e desativado; a sessão continua. | Igual. | Informada; módulo liberado. |
| `degrade` | Como `disable-module`, exceto que a falha de um programa de sessão deixa o módulo na sessão. | A chamada que falhou retorna o seu código; o módulo permanece ativo; `module.degraded` é informado uma vez por código. | Informada; módulo liberado. |

Um módulo desativado ou com falha responde às suas operações com `OL_E_MODULE_DISABLED`. As falhas de módulo aparecem como diagnósticos da sessão (`SessionStatus.Diagnostics`) com o seu código `OL_E_MODULE_*` e como eventos de runtime.

<a id="cleanup"></a>
## Limpeza

O que um módulo adquire por meio do seu contexto é rastreado: recursos registrados com `Track`, threads iniciados com `RunBackground`, processos iniciados com `StartAuxiliaryProcess`. Na parada ou falha, a ponte cancela `Stopping` e então os libera em ordem inversa. Os processos auxiliares também são colocados em um objeto job de encerramento ao fechar, de modo que terminam junto com o OMSI mesmo que o OMSI seja encerrado à força; o job de um programa de sessão o encerra junto com a sessão do OmsiLaunch, mesmo que o próprio OmsiLaunch seja encerrado à força. Um recurso que continua vivo após a liberação (um processo que não saiu, um thread que não terminou) é informado como `OL_E_MODULE_CLEANUP_INCOMPLETE`. Threads ou processos que um módulo cria sem usar o seu contexto não são rastreados.

Após uma falha abrupta (crash), não há nada a recuperar para os módulos: eles não mantêm arquivos da instalação, e sua memória, seus threads e seus processos vinculados ao job terminam junto com o processo do OMSI ou do OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Componentes nativos e hooks

Um componente nativo é uma DLL de 32 bits que implementa a interface C em `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Exportação | Chamada |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | Ao armar, antes do `Arm` gerenciado. Um valor diferente de zero faz a fase falhar. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Opcional; depois que o gameplay foi iniciado. |
| `void omsilaunch_module_native_v1_stop(void)` | Na parada ou falha, antes de seus hooks serem removidos. Depois disso, a tabela da ponte recusa toda chamada e a DLL permanece carregada, inerte, até o OMSI sair. |

A tabela da ponte oferece ao componente `request_hook`, `emit` (eventos de runtime `module.<id>.<name>` com um objeto JSON de strings) e, a partir do protocolo 2, `get_value`. Um componente lê o `size` da tabela antes de usar `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copia um valor em UTF-8 com um zero terminador e define `*length` como o número de bytes necessários sem ele; retorna `0`, `1` (argumento), `2` (nome desconhecido), `11` (após a parada) ou `12` (buffer pequeno demais). Nomes:

| Nome | Valor |
| --- | --- |
| `setting.<key>` | O valor efetivo de uma configuração declarada. |
| `data_directory` | O [diretório de dados](#data-directory) do módulo. |
| `module_directory` | O diretório do próprio módulo. |
| `session_id` | O id da sessão. |
| `module_version` | A versão do manifesto. |
| `product_version` | A versão do produto OmsiLaunch. |
| `session_channel` | O pipe do [programa de sessão](#session-programs); desconhecido para um módulo que não tem um. |

Os hooks são solicitados somente por destino simbólico, durante `start` ou `gameplay_ready`, para um destino cuja capacidade o manifesto declara:

| Destino | Hooks | Capacidade |
| --- | --- | --- |
| `iat:<dll>!<function>` | Uma importação do `Omsi.exe`, por exemplo `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` ou `EndScene` do dispositivo Direct3D 9 em execução (solicite em `gameplay_ready`, quando o dispositivo existe). | `bridge.hooks.d3d9` |

`request_hook` devolve em `*original` a função para continuar a cadeia (um detour deve chamá-la). Seu resultado: `0` sucesso, `1` argumento, `2` destino desconhecido, `3` o ponto não contém o que deveria, `4` o ponto tem hook de alguém de fora da ponte, `5` proteção de memória, `6` registro cheio, `7` ordem, `8` memória, `9` um thread estava executando os bytes a alterar, `10` ainda não há dispositivo Direct3D, `11` não permitido agora ou capacidade não declarada.

Todo hook que a ponte instala, para o produto ou para um módulo, é registrado em um único registro com o seu proprietário (`module:<id>` para um módulo):

- Um hook só é instalado sobre o valor que o ponto deveria conter (a função importada; uma função de um módulo chamado `d3d9.dll`, do runtime do sistema ou de um wrapper), ou empilhado sobre o hook do topo do próprio registro naquele ponto. Um ponto já alterado por outra parte é recusado (`4`), nunca sobrescrito.
- Os hooks em um mesmo ponto são removidos do último para o primeiro (LIFO); um hook que tem outro sobre ele permanece até que esse seja removido.
- O próprio hook de `Reset` do Direct3D do produto está no registro, de modo que os hooks de dispositivo dos módulos se empilham sobre ele.
- Na limpeza, a ponte remove todos os hooks do módulo depois que `stop` retorna. Se algum não puder ser removido (outra parte alterou o ponto), a limpeza informa `OL_E_MODULE_CLEANUP_INCOMPLETE`. A DLL de um componente nativo nunca é descarregada enquanto o OMSI roda, de modo que um detour pelo qual ainda se está retornando permanece válido.

O mecanismo também oferece suporte a hooks em prólogos de função com um trampolim (os outros threads são suspensos enquanto os bytes mudam, e os bytes esperados exatos são verificados antes), mas nenhum destino de prólogo é oferecido aos módulos ainda: tais destinos vêm do catálogo de destinos do build, com bytes verificados.

<a id="operations"></a>
## Operações

Uma operação de módulo é `module.<id>.<operation>`. Ela só existe para uma sessão cujo plano carregou o módulo (`PlannedModule.Operations`); caso contrário, `ExecuteRuntimeAsync` responde `OL_E_MODULE_NOT_LOADED` ou `OL_E_MODULE_OPERATION_UNKNOWN`. Os argumentos obrigatórios são verificados, e argumentos não declarados são recusados (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Uma falha tipada de módulo mantém o seu código quando é um código público catalogado; qualquer outro código se torna `OL_E_MODULE_OPERATION_FAILED`, com o código do módulo em `Values["module_code"]`. Chaves de resultado que seguem a convenção de nomes interna são removidas, como em toda operação de runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="writing-a-module-contracts"></a>
## Escrevendo um módulo (contratos)

O componente gerenciado referencia `OmsiLaunch.Runtime.Modules` e `OmsiLaunch.Api` e implementa `IRuntimeModule`. A ponte vincula essas duas referências aos seus próprios assemblies carregados (seja qual for o contexto de carga em que o plugin roda), de modo que uma cópia distribuída com o módulo é ignorada:

| Membro | Chamado |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Etapa 4. Adquira recursos por meio do contexto. |
| `void GameplayReady()` | Etapa 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Etapa 6, para cada operação declarada (sem o prefixo `module.<id>.`). Lance `ModuleOperationException(code, message)` para uma falha tipada. |
| `void Stop()` | Etapa 7. |

`IRuntimeModuleContext` oferece `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `SessionChannelName`, `Stopping` (um token de cancelamento), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground` e `StartAuxiliaryProcess`. Nada nele expõe endereços, o executável ou a sua identidade. O componente deve poder ser carregado pelo plugin de 32 bits (AnyCPU ou x86, .NET 6). Fixe todos os arquivos que os programas do módulo carregam (para um programa .NET: a sua `.dll` ao lado do `.exe`), para que a integridade os cubra.

<a id="known-limitations"></a>
## Limitações conhecidas

- Componentes nativos podem fazer hook em importações do `Omsi.exe` e em métodos do dispositivo Direct3D; hooks em prólogos de função e em pontos de chamada do OMSI ainda não são oferecidos aos módulos.
- A verificação de vínculo de conteúdo em processo usa o mapa que o início solicitou (NEW_MAP); para uma situação salva, o vínculo é verificado no planejamento contra o mapa da situação.
- Os hooks de dispositivo são hooks de vtable. Onde outro componente regrava os métodos originais do dispositivo (observado na instalação de regressão logo após o início do gameplay), um hook de dispositivo deixa de ser chamado e não pode ser removido; a limpeza então informa `OL_E_MODULE_CLEANUP_INCOMPLETE` e o componente permanece carregado. Um método de dispositivo já com hook de algo fora do registro é recusado (4); o evento de recusa nomeia o módulo que possui o método.
- Os módulos param em uma saída normal do OMSI; uma parada de sessão encerra o OMSI, de modo que a limpeza do módulo dentro de uma sessão acontece somente por meio de uma política de falha.
- Os avisos são exibidos assim que a bandeja existe, depois que o OMSI foi iniciado; um programa de sessão exibe a sua própria janela para tudo o que precisa dizer antes disso.
