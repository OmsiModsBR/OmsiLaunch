# Módulos de runtime

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> Tradução da [página original em inglês](../../../reference/runtime-modules.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Um módulo de runtime acrescenta comportamento a uma sessão gerida do OMSI: uma funcionalidade que corre com a sessão (por exemplo o HUD de um mapa ou otimizações de carregamento), sem passar a fazer parte do próprio OmsiLaunch. A ponte carrega-o, dá-lhe apenas aquilo que ele declara e remove tudo o que ele detém quando a sessão termina ou o módulo falha. Esta página é a referência normativa dos módulos de runtime tal como implementados por `src/OmsiLaunch.Runtime.Modules` (manifesto, definições, elegibilidade, host do ciclo de vida), pelo planeador, pelos programas de sessão do controlador (`ModuleSessionPrograms`, `ModuleSessionProcess`), pelo ambiente de módulos do plugin e pelas flags de módulos da [CLI](cli.md).

Estabilidade: `EXPERIMENTAL`. O protocolo de ponte 1 foi validado em runtime na ronda B (módulos geridos, ativação por conteúdo, operações, `disable-module` e `fail-session`; componentes nativos com hooks IAT; ver o [estado da validação em runtime](../status/runtime-validation-status.md)). O protocolo de ponte 2 (definições, ativação predefinida, o ficheiro de definições, programas de sessão, conflitos de plugins, avisos, `get_value`) está validado apenas offline. Gates offline: os gates `module.*` de `tests/OmsiLaunch.ModuleTests` (manifesto, integridade, seleção e ativação, definições, o ficheiro de definições, conflitos de plugins, programas de sessão iniciados através do respetivo job, uma falha injetada em cada fase sob cada política de falha, ciclo de vida do componente nativo, limpeza, handoff), `plugin-runtime.modules-lifecycle` (um módulo gerido real carregado pelo ambiente do plugin), `runtime-modules.native-component` (um componente nativo real que interceta uma importação e lê valores através da ponte), `runtime-modules.plugin-conflict` e `native-hooks.self-check` (o motor de hooks). O protocolo de ponte 3 (Beta 5: operações do catálogo em processo, o tick, a captura de entrada, as entradas de conteúdo da comunidade, as fixações opcionais com o snapshot no momento do plano, `module pack`, opções de job de programas auxiliares, a versão do OMSI) está validado apenas offline: os gates `module.*` de `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` e `ui-input.hook-self-check` dos testes de integração do plugin, e `omsi-version.literal` dos testes unitários. A tomada de controlo do menu de pausa e a captura de entrada não foram observadas numa sessão real do OMSI.

O carregamento é aberto: qualquer módulo instalado em `plugins\OmsiLaunch.Modules` pode ser carregado, seja quem for o seu autor. O OmsiLaunch não garante o que um módulo faz dentro do OMSI. O que garante é a [fronteira](#security-boundary): os módulos nunca correm dentro do processo do OmsiLaunch e nada do que o OmsiLaunch oferece permite a um módulo ir além dos direitos do próprio OMSI.

<a id="where-modules-live"></a>
## Onde residem os módulos

Cada módulo é um diretório dentro da pasta do plugin permanente:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

O nome do diretório tem de ser igual ao `id` do módulo. Os módulos são instalados por quem os distribui; uma sessão nunca escreve, faz snapshot nem restaura ficheiros aí, e os módulos nunca alteram ficheiros da instalação através do OmsiLaunch (um manifesto não tem forma de declarar um overlay de instalação). Os dados graváveis de um módulo residem no seu [diretório de dados](#data-directory).

<a id="manifest-modulejson"></a>
## Manifesto (`module.json`)

Esquema `omsilaunch.runtime-module/v1`. As chaves desconhecidas são rejeitadas, pelo que um manifesto escrito para um esquema posterior é recusado em vez de ser compreendido pela metade. Todos os caminhos são relativos ao diretório do módulo (os caminhos de conteúdo associados são relativos à raiz do OMSI), usam `\` ou `/` e não podem conter `..` nem uma unidade.

| Chave | Obrigatória | Significado |
| --- | --- | --- |
| `schema` | sim | `omsilaunch.runtime-module/v1`. |
| `id` | sim | Id do módulo: letras minúsculas, dígitos e `-`, de 2 a 48 carateres, começando por uma letra ou dígito. É também o nome do seu diretório e o seu espaço de nomes de operações. |
| `version` | sim | `major.minor.patch`, opcionalmente `-suffix`. |
| `name` | não | Nome de apresentação (no máximo 128 carateres). |
| `bridge_protocol` | sim | `{ "min": n, "max": n }`: os protocolos de ponte com que o módulo funciona. Esta build serve os protocolos `1`, `2` e `3`; um módulo é aceite quando o seu intervalo inclui um deles. |
| `activation` | não | `requested` (predefinição): carregado quando a sessão o pede. `content`: carregado também quando a sessão executa o mapa associado (requer `content_binding`). `default` (protocolo 2): carregado em todas as sessões, a menos que esteja desativado (ver [escolha dos módulos de uma sessão](#choosing-modules-for-a-session)); não pode ter `content_binding`. |
| `required_capabilities` | não | Capacidades sem as quais o módulo não consegue funcionar (ver [capacidades](#capabilities)). |
| `optional_capabilities` | não | Capacidades que o módulo usa quando estão disponíveis; verifica-as com `IsCapabilityAvailable`. |
| `content_binding` | não | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: o módulo só corre nesse mapa, com esses ficheiros inalterados. |
| `assets` | não | Ficheiros que o módulo lê, cada um `{ "path", "sha256" }`. A partir do protocolo 3 o `sha256` é opcional (ver [integridade](#integrity)). |
| `components` | sim | Exatamente um componente `managed` `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; opcionalmente um componente `native` (`.dll`), componentes `auxiliary` (`.exe`) e, com o protocolo 2, um componente `host` (`.exe`, o [programa de sessão](#session-programs) do módulo). A partir do protocolo 3 o `sha256` de um componente é opcional, e um componente `auxiliary` pode ter `priority` (`normal`, `below-normal` ou `idle`) e `memory_limit_mb` (64 a 65536): o job object do programa aplica-os desde a sua primeira instrução (ver [programas auxiliares](#auxiliary-programs)). |
| `failure_policy` | sim | `fail-session`, `disable-module` ou `degrade` (ver [falhas](#failures)). |
| `operations` | não | Operações que o módulo expõe enquanto a sessão decorre: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | não | Protocolo 2. As definições que o módulo aceita (ver [definições](#settings)). |
| `conflicts` | não | Protocolo 2. `{ "plugins": [ "<file>.dll" ] }`: DLLs de plugins do OMSI que não podem executar-se ao lado do módulo (ver [conflitos com plugins do OMSI](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | não | Protocolo 3. Operações do catálogo público de capacidades que o módulo executa em processo, por exemplo `["time.read", "camera.set"]` (ver [operações em processo](#operations-in-process)). Requer a capacidade `bridge.runtime`. |
| `tick_ms` | não | Protocolo 3. O módulo quer um callback periódico a cada `tick_ms` milissegundos: um múltiplo de 50, de 50 a 1000 (ver [tick](#tick)). Requer a capacidade `bridge.tick`. |
| `content_inputs` | não | Protocolo 3. Conteúdo da comunidade que o módulo lê, localizado pela ponte: `[ { "kind": "realmap-package", "required": false } ]` (ver [conteúdo da comunidade](#community-content)). |
| `timeouts` | não | Orçamentos em milissegundos: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (predefinição 5000 cada, no máximo 30000); com o protocolo 2 também `prepare_ms` (predefinição 120000, no máximo 1800000) e `host_stop_ms` (predefinição 10000, no máximo 60000) para o programa de sessão. As operações têm por predefinição 2000 ms. |
| `diagnostics` | não | `{ "trace": true\|false }`. |

Um manifesto que usa uma funcionalidade do protocolo 2 (`settings`, `conflicts`, ativação `default`, um componente `host`, `prepare_ms` ou `host_stop_ms`) tem de declarar `bridge_protocol.min` 2. Um manifesto que usa uma funcionalidade do protocolo 3 (`runtime_operations`, `tick_ms`, `content_inputs`, uma capacidade `bridge.runtime`, `bridge.tick` ou `bridge.input`, um `priority` ou `memory_limit_mb` auxiliar, ou um componente ou recurso sem `sha256`) tem de declarar `bridge_protocol.min` 3.

Todos os ficheiros nomeados por `components` e `assets` podem ser fixados pelo respetivo SHA-256 (obrigatório antes do protocolo 3, opcional a partir dele: ver [integridade](#integrity)). O `module.json` não consegue fixar-se a si próprio.

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

Todos os ficheiros que o manifesto nomeia têm de existir e não podem ser alcançados através de uma junção ou ligação simbólica, e todos os ficheiros que o autor fixou têm de ter o SHA-256 fixado; caso contrário, o módulo é recusado com `OL_E_MODULE_INTEGRITY`. Isto deteta um módulo corrompido ou atualizado a meio. Não é uma decisão de confiança: um módulo não é verificado contra nenhuma lista, assinatura ou editor.

A partir do protocolo 3 uma fixação é opcional, pelo que um autor nunca tem de manter hashes à mão, e o plano toma sempre um **snapshot**: um SHA-256 sobre todos os ficheiros que o manifesto nomeia e o seu conteúdo, registado no plano (`PlannedModule.SnapshotSha256`) e transportado para o plugin no handoff de arranque (versão 8 do formato). O snapshot é o que garante que os ficheiros que o plano avaliou são os ficheiros que correm: o plugin só carrega um módulo quando o seu `module.json` tem o SHA-256 que o plano avaliou e os seus ficheiros ainda têm o conteúdo do snapshot, e o controlador e a ponte comparam o snapshot de novo imediatamente antes de um programa de sessão, um componente nativo ou um programa auxiliar arrancar. Um módulo cujos ficheiros mudaram depois do planeamento não é carregado (`OL_E_MODULE_INTEGRITY`, "the module's files changed after planning"). O snapshot cobre todos os módulos, fixados ou não; uma fixação permite além disso a quem publica um módulo detetar uma cópia corrompida ou atualizada a meio logo que é descoberta.

A verificação é repetida no ponto onde o código do módulo arranca: o plugin só carrega um módulo se o seu `module.json` ainda tiver o SHA-256 que o plano avaliou e todas as fixações e o snapshot continuarem a ser válidos, e o controlador só inicia um programa de sessão depois de verificar a fixação desse programa e o snapshot.

<a id="packing-a-module"></a>
### Empacotar um módulo

`OmsiLaunch.exe /pack-module:<module directory>` escreve as fixações dos ficheiros que um `module.json` nomeia (os seus componentes e recursos), pelo que um autor nunca edita um SHA-256 à mão. Reescreve apenas os valores `sha256` (o resto do manifesto é mantido; o ficheiro é reindentado), volta a ler o resultado com o analisador de manifestos antes de escrever o que quer que seja, e comunica cada ficheiro (envelope `module.pack`: `module`, `directory`, `written`, `intact`, `files` com `path`, `sha256`, `pinned`, `mismatch`, `written`). Funciona sobre o diretório que lhe é dado e nunca toca numa instalação. `/verify-module:<module directory>` indica o mesmo sem escrever (`module.verify`); termina com `7` quando um ficheiro fixado não coincide com a sua fixação (um ficheiro sem fixação não é uma divergência). Ambos terminam com `2` para um manifesto que não é analisável ou um ficheiro em falta.

<a id="security-boundary"></a>
## Fronteira de segurança

O OmsiLaunch não controla o que um módulo faz dentro do OMSI: os seus componentes geridos e nativos executam-se no processo do OMSI com os mesmos direitos que qualquer plugin do OMSI. O OmsiLaunch garante o seguinte:

1. O processo do OmsiLaunch nunca carrega código de módulos. O código do módulo executa-se dentro do OMSI, num programa auxiliar iniciado a partir de dentro do OMSI, ou no programa de sessão do módulo.
2. Os programas que o OmsiLaunch inicia para módulos são ficheiros do próprio diretório do módulo, verificados contra a respetiva fixação, iniciados com uma linha de comandos que o OmsiLaunch fixa. Um programa de sessão corre num job object que o termina com a sessão, não permite um segundo processo (não pode iniciar outros programas) e recusa alterações a parâmetros do sistema, definições de ecrã, ao ambiente de trabalho e ao fim de sessão ou encerramento. Herda apenas os seus dois handles de pipe.
3. Tudo o que um módulo declara fica dentro do seu diretório, e os seus dados graváveis dentro de `.omsilaunch\module-data\<id>`.
4. Os canais do OmsiLaunch (controlo local, startup handoff, telemetria, comandos de runtime) permanecem restritos ao utilizador atual e associados à sessão; nenhum deles permite a um chamador iniciar um programa, ler ou escrever ficheiros fora da instalação, ou alterar definições do sistema.
5. O OmsiLaunch nunca eleva privilégios. Um módulo corre com os direitos do utilizador que iniciou a sessão.
6. O texto proveniente de um módulo (avisos, eventos, valores) é tratado como dados: limitado, sem carateres de controlo, nunca executado, aberto nem seguido como ligação.

<a id="capabilities"></a>
## Capacidades

`required_capabilities` e `optional_capabilities` nomeiam uma capacidade nativa da build em execução (a coluna de capacidade nativa do [catálogo de capacidades](capabilities.md#capability-catalog), por exemplo `time.read` ou `d3d.texture`) ou uma funcionalidade da ponte:

| Funcionalidade da ponte | O que o módulo obtém |
| --- | --- |
| `bridge.events` | `Emit`: eventos de runtime `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: threads em segundo plano rastreadas. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: processos auxiliares associados à sessão. |
| `bridge.hooks.iat` | Componente nativo: hooks em importações de `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Componente nativo: hooks em métodos do dispositivo Direct3D 9 em uso (`d3d9:device.<method>`). Requer a capacidade nativa `d3d.texture`. |
| `bridge.runtime` | Protocolo 3. `IRuntimeModuleContext.Runtime` e o `run_operation` nativo: as operações do catálogo que o manifesto lista em `runtime_operations`. |
| `bridge.tick` | Protocolo 3. Um callback periódico: `ITickingModule.Tick` e o export nativo `omsilaunch_module_native_v1_tick`. |
| `bridge.input` | Protocolo 3. `IRuntimeModuleContext.Input`: captura das mensagens de teclado e rato do jogo para o menu próprio do módulo. Requer a capacidade nativa `ui.input.capture`. |

Um nome que não seja nenhuma das duas é recusado no planeamento (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). Numa build do OMSI 2 sem layout conhecido, uma capacidade nativa que o ficheiro executável não resolve é decidida em processo; se não for resolvida aí, o módulo não é carregado.

<a id="choosing-modules-for-a-session"></a>
## Escolha dos módulos de uma sessão

Um módulo junta-se a uma sessão de uma de quatro formas; `PlannedModule.Activation` indica qual:

| Ativação | Quando |
| --- | --- |
| `requested` | A sessão pede-o: `LaunchSpec.Modules.Enable`, `/module:<id>` ou `enabled: true` na predefinição do perfil de sessão selecionada. |
| `settings-file` | `enabled=true` na sua secção do [ficheiro de definições](#the-settings-file). |
| `content` | A sua ativação é `content` e a sessão executa o mapa associado. |
| `default` | A sua ativação é `default`. |

Um módulo é mantido fora da sessão por `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` na predefinição ou `enabled=false` no ficheiro de definições (diagnóstico do plano `module.disabled`). Um pedido explícito prevalece sobre o ficheiro de definições. Ativar e desativar o mesmo módulo no mesmo pedido é `OL_E_MODULE_SETTING_INVALID`.

Regras de planeamento: um módulo pedido que não pode ser carregado torna o plano não executável com o respetivo código `OL_E_MODULE_*`. Um módulo que se juntou por si próprio (ficheiro de definições, conteúdo, predefinição) e que não pode ser carregado não impede a sessão: o plano comunica `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## Definições

Um módulo declara as suas definições em `settings`; cada entrada:

| Chave | Obrigatória | Significado |
| --- | --- | --- |
| `key` | sim | `[a-z][a-z0-9_]{0,31}`, diferente de `enabled`. |
| `type` | sim | `boolean`, `integer`, `choice` ou `text`. |
| `default` | sim | O valor quando nada mais o define: um booleano, um inteiro, uma cadeia de texto, ou `"auto"` quando `auto` é verdadeiro. |
| `auto` | não | `true`: o valor `auto` é aceite e passado ao módulo, que decide em tempo de execução. |
| `min`, `max` | não | Limites inteiros. |
| `choices` | para `choice` | Os valores aceites (`[a-z0-9][a-z0-9_.-]{0,31}`, diferentes de `auto`). |
| `max_length` | não | Limite de comprimento do texto (1..4096, predefinição 256). Os carateres de controlo são recusados. |
| `flag` | não | Uma flag de linha de comandos para esta definição, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, ou apenas `/<flag>` para `true` num booleano. |
| `description` | não | No máximo 256 carateres. |

Os valores são normalizados: os booleanos aceitam `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; as escolhas são comparadas sem distinção de maiúsculas e minúsculas; os inteiros são verificados contra os seus limites.

O valor efetivo de cada definição é, por ordem:

1. o pedido: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, uma flag própria do módulo, ou `modules.<id>.settings` na predefinição do perfil de sessão selecionada. Tal como para qualquer outro campo de predefinição, a linha de comandos não pode definir também uma chave que a predefinição define (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. o [ficheiro de definições](#the-settings-file);
3. a predefinição do manifesto.

Um valor pedido inválido, ou uma chave pedida que o módulo não declara, torna o plano não executável (`OL_E_MODULE_SETTING_INVALID`). Uma entrada do ficheiro de definições que não pode ser usada é ignorada com `OL_W_MODULE_SETTING_IGNORED`, tal como as definições pedidas para um módulo que a sessão não carrega. O plano lista cada valor efetivo com a sua origem (`request`, `settings-file` ou `default`) em `PlannedModule.Settings`.

O módulo recebe todas as chaves declaradas com o respetivo valor efetivo: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` num componente nativo e o objeto `settings` do contexto de um programa de sessão.

<a id="the-settings-file"></a>
### O ficheiro de definições

`<OMSI root>\.omsilaunch\modules.ini`, junto dos perfis de sessão. O OmsiLaunch apenas o lê.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

As secções são ids de módulos; as linhas `key=value` definem definições, `enabled` liga ou desliga o módulo para todas as sessões; as linhas que começam por `;` ou `#` são comentários. O ficheiro pode ter no máximo 256 KiB. Tudo o que não pode usar (uma secção que não é um id de módulo, uma linha fora de uma secção, uma chave desconhecida, um valor inválido) é comunicado com `OL_W_MODULE_SETTING_IGNORED` e ignorado.

<a id="session-programs"></a>
## Programas de sessão

Um módulo pode declarar um componente `host`: o seu programa de sessão, um executável do seu próprio diretório. Quando o módulo está no plano, o controlador:

1. verifica a fixação do programa e cria o diretório de dados do módulo;
2. inicia-o antes do OMSI, a partir do diretório do módulo, com a linha de comandos `"<program>" --omsilaunch-session <session id>`, suspenso até estar no seu job (ver a [fronteira de segurança](#security-boundary));
3. escreve o contexto como primeira linha na sua entrada padrão;
4. aguarda até que o programa escreva `{"type":"ready"}`, no máximo `prepare_ms`; só então inicia o OMSI;
5. depois de o OMSI ter sido criado, escreve `{"type":"omsi-started","pid":<process id>}`;
6. no fim da sessão, depois de o OMSI ter terminado e antes de qualquer ficheiro ser restaurado, escreve `{"type":"stop"}`, aguarda no máximo `host_stop_ms` e depois termina o programa.

Ambos os sentidos usam JSON em UTF-8, um objeto por linha. O contexto:

| Campo | Significado |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identidade. |
| `installation_root`, `module_directory`, `data_directory` | Caminhos completos. |
| `channel` | O named pipe que o programa pode servir para os componentes do módulo no OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. O programa cria-o, restrito ao utilizador atual, e verifica que o cliente é o processo do OMSI. Os componentes leem-no com `get_value("session_channel")` ou `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | As definições efetivas do módulo. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` da sessão. |
| `session_files` | Ficheiros relativos à instalação que a transação da sessão detém. O programa não os pode manter abertos nem mapeados: são restaurados quando a sessão termina. |

O que o programa pode escrever além de `ready`:

| Linha | Efeito |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Evento de runtime `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Evento de runtime `module.<id>.notice` (ver [avisos](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Diagnóstico da sessão com esse código catalogado (`OL_E_MODULE_OPERATION_FAILED` caso contrário). |

As outras linhas, as linhas com mais de 64 KiB e tudo o que vier depois de 10,000 linhas são ignorados. O controlador regista `module.<id>.host.started`, `host.ready` (com `prepare_ms`), `host.failed`, `host.exited` e `host.killed`.

Um programa que não esteja pronto dentro de `prepare_ms`, que termine antes de estar pronto, ou cuja fixação não seja válida, faz falhar a preparação do seu módulo: sob `fail-session` a sessão não arranca (`OL_E_MODULE_SESSION_FAILED`); sob `disable-module` o programa é terminado e o módulo fica fora do OMSI; sob `degrade` o módulo permanece (e o programa continua a executar-se, se ainda puder). O `StartupTimeoutSeconds` da sessão conta a partir do momento em que o OMSI é iniciado, pelo que a preparação não o consome.

<a id="conflicts-with-omsi-plugins"></a>
## Conflitos com plugins do OMSI

`conflicts.plugins` nomeia DLLs de plugins do OMSI que não podem executar-se ao lado do módulo, por exemplo um plugin mais antigo que instala os mesmos hooks. No planeamento, o OmsiLaunch lê a secção `[dll]` de cada `plugins\*.opl`; se algum carregar uma DLL em conflito, o módulo é recusado com `OL_E_MODULE_CONFLICT` e o motivo nomeia o `.opl`. Dentro do OMSI, um módulo cuja DLL em conflito seja carregada mesmo assim não é carregado. O OmsiLaunch nunca altera os ficheiros de outro plugin; desligá-lo é decisão do utilizador.

<a id="notices"></a>
## Avisos

Um evento de módulo com o nome `notice` (de `Emit("notice", ...)`, do `emit` de um componente nativo ou da linha de aviso de um programa de sessão) é mostrado pelo indicador na área de notificação do OmsiLaunch como um balão cujo título nomeia o módulo. Apenas `title` (no máximo 63 carateres) e `text` (no máximo 255 carateres) são mantidos, os carateres de controlo são removidos e nada do conteúdo é aberto nem executado. Um programa de sessão que tenha de informar o utilizador antes de o OMSI arrancar (quando o indicador ainda não é mostrado) usa a sua própria janela.

<a id="data-directory"></a>
## Diretório de dados

`<OMSI root>\.omsilaunch\module-data\<id>\` é o diretório gravável do módulo para esta instalação: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")` e o `data_directory` do programa de sessão. É criado na primeira utilização. Não faz parte da transação da sessão: o que um módulo aí escreve permanece depois da sessão. `/clear-module-data:<id>` elimina-o (e mais nada); é recusado enquanto uma sessão detiver a instalação, e um diretório alcançado através de uma ligação é removido como ligação, nunca seguido.

<a id="lifecycle"></a>
## Ciclo de vida

| Passo | Onde | O que acontece |
| --- | --- | --- |
| 1. Descoberta | controlador | `plugins\OmsiLaunch.Modules\*` é lido: manifesto e integridade (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Elegibilidade | controlador, no planeamento | Os módulos que [se juntam à sessão](#choosing-modules-for-a-session) são avaliados: utilizável, protocolo de ponte, capacidades, associação de conteúdo, conflitos de plugins, definições. O resultado é `SessionPlan.PlannedModules`. |
| 3. Preparação | controlador, no arranque | Os [programas de sessão](#session-programs) dos módulos são iniciados e aguardados. Depois, os módulos elegíveis seguem para o plugin no startup handoff (versão 6): os seus diretórios, o SHA-256 de cada `module.json` avaliado e as suas definições efetivas. |
| 4. Armar | plugin, antes de o mundo arrancar | Cada módulo é reverificado e carregado (componente gerido no seu próprio contexto de carregamento). O seu componente nativo, se existir, é carregado e iniciado primeiro e depois é chamado `Arm(context)`. |
| 5. Gameplay pronto | plugin | Depois de `gameplay.entered`, quando um módulo carregado declara `bridge.hooks.d3d9`, a ponte começa a observar o dispositivo Direct3D (`d3d.ready`); depois chama o `gameplay_ready` do componente nativo e `GameplayReady()`. Para uma sessão NEW_MAP, o mapa associado é verificado de novo contra o mapa que o arranque carregou. |
| 6. Runtime | plugin | As operações `module.<id>.<operation>` chegam a `Execute`. Um módulo com `tick_ms` recebe ticks (ver [tick](#tick)); um módulo com `runtime_operations` executa-as em processo (ver [operações em processo](#operations-in-process)). |
| 7. Paragem | plugin, depois controlador | Quando a sessão termina (ou o módulo falha), é chamado `Stop()`. Depois de o OMSI ter terminado, os programas de sessão são parados. |
| 8. Limpeza | plugin | Tudo o que o módulo rastreou é libertado por ordem inversa e verificado como desaparecido; o componente nativo é parado em último lugar e os seus hooks são removidos; a sua DLL permanece carregada, inerte, até o OMSI terminar (uma thread pode ainda estar a regressar através de um detour). |

Cada callback em processo corre na thread de UI do OMSI, tal como as operações de runtime do próprio produto. A ponte mede cada callback contra o seu orçamento: um callback que regressa tarde faz falhar a sua fase (`OL_E_MODULE_PHASE_TIMEOUT`). A ponte não consegue interromper um callback que nunca regressa; um módulo assim bloqueia o OMSI e a sessão termina no seu timeout de arranque ou por `session stop`, como qualquer outra sessão.

<a id="failures"></a>
## Falhas

Uma falha é uma exceção ou um excesso de tempo em qualquer fase ou operação, uma capacidade não resolvida em processo, uma associação de conteúdo que não se verifica em processo, um plugin em conflito carregado no OMSI, ou um programa de sessão que não fica pronto. A `failure_policy` do módulo decide o que se segue. Em todos os casos os recursos do módulo são libertados. Um tick que lança uma exceção, devolve um valor diferente de zero ou excede o seu intervalo é uma falha, tal como a de uma operação.

| Política | Falha na preparação, carregamento, armar ou gameplay pronto | Falha numa operação | Falha na paragem |
| --- | --- | --- | --- |
| `fail-session` | Módulo libertado; `module.session-failure`; a sessão falha com `OL_E_MODULE_SESSION_FAILED`, o OMSI é parado e os ficheiros são restaurados. Uma falha antes de o mundo arrancar impede o arranque. | Igual. | Comunicada (`module.failed`); a sessão continua até ao fim. |
| `disable-module` | Módulo libertado e desativado; a sessão continua. | Igual. | Comunicada; módulo libertado. |
| `degrade` | Como `disable-module`, exceto que a falha de um programa de sessão deixa o módulo na sessão. | A chamada que falha devolve o seu código; o módulo permanece ativo; `module.degraded` é comunicado uma vez por código. | Comunicada; módulo libertado. |

Um módulo desativado ou falhado responde às suas operações com `OL_E_MODULE_DISABLED`. As falhas de módulos surgem como diagnósticos da sessão (`SessionStatus.Diagnostics`) com o respetivo código `OL_E_MODULE_*` e como eventos de runtime.

<a id="cleanup"></a>
## Limpeza

O que um módulo adquire através do seu contexto é rastreado: recursos registados com `Track`, threads iniciadas com `RunBackground`, processos iniciados com `StartAuxiliaryProcess`. Na paragem ou falha, a ponte cancela `Stopping` e depois liberta-os por ordem inversa. Os processos auxiliares são também colocados num job object com terminação ao fechar, pelo que terminam com o OMSI mesmo que o OMSI seja terminado à força; o job de um programa de sessão termina-o com a sessão do OmsiLaunch, mesmo que o próprio OmsiLaunch seja terminado à força. Um recurso que continua vivo após a libertação (um processo que não terminou, uma thread que não acabou) é comunicado como `OL_E_MODULE_CLEANUP_INCOMPLETE`. As threads ou processos que um módulo cria sem o seu contexto não são rastreados.

Depois de uma falha abrupta não há nada a recuperar para os módulos: não detêm ficheiros da instalação e a sua memória, threads e processos associados ao job terminam com o processo do OMSI ou com o processo do OmsiLaunch.

<a id="native-components-and-hooks"></a>
## Componentes nativos e hooks

Um componente nativo é uma DLL de 32 bits que implementa a interface C em `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Exportação | Chamada |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | Ao armar, antes do `Arm` gerido. Um valor diferente de zero faz falhar a fase. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Opcional; depois de o gameplay ter sido iniciado. |
| `void omsilaunch_module_native_v1_stop(void)` | Na paragem ou falha, antes de os seus hooks serem removidos. Depois disso a tabela da ponte recusa todas as chamadas e a DLL permanece carregada, inerte, até o OMSI terminar. |

A tabela da ponte dá ao componente `request_hook`, `emit` (eventos de runtime `module.<id>.<name>` com um objeto JSON de cadeias de texto) e, a partir do protocolo 2, `get_value`. Um componente lê o `size` da tabela antes de usar `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copia um valor em UTF-8 com um zero terminador e define `*length` para os bytes necessários sem ele; devolve `0`, `1` (argumento), `2` (nome desconhecido), `11` (após a paragem) ou `12` (buffer demasiado pequeno). A partir do protocolo 3 a tabela contém também `run_operation` (ver [operações em processo](#operations-in-process)) e `d3d_is_game_device(context, device)`: se um ponteiro de dispositivo recebido por um hook Direct3D é o dispositivo do próprio jogo (`1`), outro (`0`; o formulário de seleção de veículo cria um segundo dispositivo com a mesma tabela de métodos, pelo que um detour de `Present` é chamado para ambos e só deve desenhar para o do jogo), `10` quando ainda não é observado nenhum dispositivo, `11` quando `bridge.hooks.d3d9` não está declarado ou após a paragem. O ponteiro é apenas comparado, nunca usado, e a ponte nunca entrega nenhum. Um módulo que mantém recursos Direct3D no pool predefinido interceta ele próprio `d3d9:device.Reset` (o que faz antes de chamar o original é «dispositivo perdido», depois de este devolver sucesso é «dispositivo restaurado»). Um componente verifica o `size` da tabela antes de usar um membro do protocolo 3 (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Dois exports opcionais completam o componente: `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (ver [tick](#tick)) e `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, a forma como o componente gerido chama o nativo através de `IRuntimeModuleContext.NativeComponent.Execute` (JSON à entrada, JSON à saída, a mesma convenção de buffer que `run_operation`). Os nomes que `get_value` responde:

| Nome | Valor |
| --- | --- |
| `setting.<key>` | O valor efetivo de uma definição declarada. |
| `data_directory` | O [diretório de dados](#data-directory) do módulo. |
| `module_directory` | O diretório do próprio módulo. |
| `session_id` | O id da sessão. |
| `module_version` | A versão do manifesto. |
| `product_version` | A versão do produto OmsiLaunch. |
| `session_channel` | O pipe do [programa de sessão](#session-programs); desconhecido para um módulo que não tenha um. |
| `omsi_version` | A versão do produto OMSI que o próprio jogo comunica (protocolo 3; ver [a versão do OMSI](#the-omsi-version)). |

Os hooks são pedidos apenas por alvo simbólico, durante `start` ou `gameplay_ready`, para um alvo cuja capacidade o manifesto declara:

| Alvo | Hooks | Capacidade |
| --- | --- | --- |
| `iat:<dll>!<function>` | Uma importação de `Omsi.exe`, por exemplo `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` ou `EndScene` do dispositivo Direct3D 9 em uso (pedir em `gameplay_ready`, quando o dispositivo existe). | `bridge.hooks.d3d9` |

`request_hook` devolve em `*original` a função para continuar a cadeia (um detour tem de a chamar). O seu resultado: `0` sucesso, `1` argumento, `2` alvo desconhecido, `3` o local não contém o que deveria, `4` o local está intercetado por alguém fora da ponte, `5` proteção de memória, `6` registo cheio, `7` ordem, `8` memória, `9` uma thread estava a executar os bytes a modificar, `10` ainda sem dispositivo Direct3D, `11` não permitido agora ou capacidade não declarada.

Todos os hooks que a ponte instala, para o produto ou para um módulo, ficam registados num único registo com o respetivo proprietário (`module:<id>` para um módulo):

- Um hook só é instalado sobre o valor que o local deveria conter (a função importada; uma função de um módulo chamado `d3d9.dll`, do runtime do sistema ou de um wrapper), ou empilhado sobre o hook do topo do próprio registo nesse local. Um local já alterado por outrem é recusado (`4`), nunca sobrescrito.
- Os hooks num mesmo local são removidos pela ordem último a entrar, primeiro a sair; um hook que tenha outro por cima permanece até esse ser removido.
- O hook `Reset` Direct3D do próprio produto está no registo, pelo que os hooks de dispositivo dos módulos se empilham sobre ele.
- Na limpeza, a ponte remove todos os hooks do módulo depois de `stop` regressar. Se algum não puder ser removido (alguém alterou o local), a limpeza comunica `OL_E_MODULE_CLEANUP_INCOMPLETE`. A DLL de um componente nativo nunca é descarregada enquanto o OMSI corre, pelo que um detour através do qual ainda se está a regressar permanece válido.

O motor suporta também hooks em prólogos de funções com um trampolim (as outras threads são suspensas enquanto os bytes mudam e os bytes esperados exatos são verificados primeiro), mas ainda não é oferecido aos módulos nenhum alvo de prólogo: esses alvos provêm do catálogo de alvos da build com bytes verificados.

<a id="operations"></a>
## Operações

Uma operação de módulo é `module.<id>.<operation>`. Existe apenas para uma sessão cujo plano carregou o módulo (`PlannedModule.Operations`); caso contrário, `ExecuteRuntimeAsync` responde `OL_E_MODULE_NOT_LOADED` ou `OL_E_MODULE_OPERATION_UNKNOWN`. Os argumentos obrigatórios são verificados e os argumentos não declarados são recusados (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). Uma falha tipada de um módulo mantém o seu código quando é um código público catalogado; qualquer outro código passa a `OL_E_MODULE_OPERATION_FAILED` com o código do módulo em `Values["module_code"]`. As chaves de resultado que seguem a convenção de nomes interna são removidas, como em todas as operações de runtime.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## Programas auxiliares

Um módulo inicia um dos seus componentes `auxiliary` (um `.exe` fixado do seu próprio diretório) com `StartAuxiliaryProcess`. A partir do protocolo 3 a ponte cria o programa **suspenso**, coloca-o no job object da sessão e só depois o retoma, pelo que um filho que ele inicie de imediato não pode correr fora do job. O job termina o programa e tudo o que ele iniciou quando o módulo pára ou o OMSI termina (kill on close) e aplica o `priority` (`below-normal`, `idle`) e o `memory_limit_mb` do manifesto desde a primeira instrução do programa. Ao contrário de um [programa de sessão](#session-programs), o job de um programa auxiliar permite processos filho (um programa que incorpora uma web view precisa deles); todos eles estão no job. Antes de arrancar, o programa é verificado de novo: a sua fixação quando a tem e, sempre, o snapshot do plano.

<a id="operations-in-process"></a>
## Operações em processo

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (gerido) e `run_operation` (nativo) executam em processo, na thread de UI do OMSI, uma operação do [catálogo público de capacidades](capabilities.md#public-runtime-operations), com o mesmo registo, os mesmos resultados `OL_E_*` tipados, as mesmas verificações de argumentos e a mesma política de mutação que o canal de controlo de runtime, e sem o canal. Só são permitidas as operações que o manifesto lista em `runtime_operations`: qualquer outra é respondida com `OL_E_MODULE_OPERATION_NOT_DECLARED` antes de chegar ao OMSI, e as operações `internal.*` e `module.*` nunca podem ser listadas. Uma operação cuja capacidade nativa a build não resolve responde `OL_E_CAPABILITY_UNRESOLVED`, como em todo o lado.

A lista é informação de transparência e de planeamento, não uma fronteira de segurança: um módulo já corre dentro do OMSI com os direitos do utilizador. O plano mostra-a (`PlannedModule.RuntimeOperations`), e uma operação que altera o estado do OMSI leva aí a sua política de mutação, por exemplo `camera.set (omsi-state)`. Um módulo pode executar operações a partir de `Arm` (o mundo pode ainda não existir; a operação responde então o seu próprio erro tipado). Os resultados são limitados (1 MiB); um resultado maior é `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Depois de o módulo parar, cada chamada responde `OL_E_MODULE_DISABLED`.

O `run_operation(context, operation, arguments_json, buffer, capacity, length)` nativo recebe o id do catálogo e um objeto JSON de cadeias de texto (ou nulo) e escreve `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. Devolve `0` quando respondeu (a falha própria da operação está dentro do JSON), `1` para um argumento, `11` após a paragem e `12` quando o buffer é demasiado pequeno (`*length` é o tamanho necessário).

## Tick

Um módulo que declara `tick_ms` e a capacidade `bridge.tick` é chamado aproximadamente a cada `tick_ms` milissegundos, na thread de UI do OMSI, a partir do timer de UI de 50 ms do plugin: `ITickingModule.Tick(TimeSpan elapsed)` no componente gerido e o export nativo opcional `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. O intervalo é um múltiplo de 50 entre 50 e 1000. Um tick não é por frame: um módulo que desenha interceta o método Direct3D `Present` e usa o tick para ler dados. Os ticks começam depois de `gameplay.entered` e terminam com o módulo. Um tick que lança uma exceção, devolve um valor diferente de zero (nativo) ou demora mais do que o seu intervalo é uma falha desse módulo: sob `degrade` é comunicado uma vez por código (`module.degraded`) e o módulo continua a executar (os ticks perdidos são saltados, nunca enfileirados); sob `disable-module` e `fail-session` aplica-se a política. Um ciclo modal aninhado do OMSI mantém o timer de UI a disparar, pelo que um tick nunca volta a entrar em si próprio.

<a id="community-content"></a>
## Conteúdo da comunidade

`content_inputs` declara conteúdo que o módulo lê e que o seu autor não pode conhecer quando o módulo é construído, porque é a comunidade que o produz, para um mapa, depois do módulo. Não há SHA-256 para fixar; a ponte apenas o localiza e verifica que lê-lo é seguro. O único tipo hoje é `realmap-package`: a pasta `openomsi-realmap` de um mapa, `<OMSI root>\maps\<folder>\openomsi-realmap`, escrita pelas ferramentas de pacotes de mapas do openOMSI.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` responde se o mapa tem um pacote (não ter um não é um erro) e, quando tem, o seu diretório, o número de ficheiros, o tamanho total e um `Fingerprint`. A impressão digital é um detetor de alterações sobre o caminho relativo, o tamanho e a hora de escrita de cada ficheiro: usar como chave de cache; nunca é uma verificação de integridade nem uma decisão de confiança. O pacote tem de estar dentro da instalação, nenhuma parte dele pode ser um ponto de reanálise (reparse point), e é limitado (50,000 ficheiros, 4 GiB); caso contrário a resposta é `OL_E_MODULE_CONTENT_INPUT_INVALID`. Um módulo que não declarou o tipo recebe `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. O pacote são dados produzidos por terceiros: lê-lo em código gerido, tratar cada valor como não fidedigno e degradar por recurso. Uma entrada `required: true` faz o plano recusar o módulo (`OL_E_MODULE_CONTENT_BINDING`) quando o mapa da sessão não tem pacote.

<a id="input-capture"></a>
## Captura de entrada

Um módulo que substitui o menu de pausa do OMSI pelo seu próprio precisa do teclado e do rato do jogo. O OMSI lê as suas teclas de simulação através do DirectInput e o rato através de mensagens de janela para a janela de vista do formulário principal; enquanto a sua própria pausa está ativa (o byte de pausa nativo), a simulação, a renderização, os cliques 3D e os comandos DirectInput são retidos pelo próprio OMSI. `IRuntimeModuleContext.Input` (capacidade `bridge.input`, capacidade nativa `ui.input.capture`) acrescenta o resto: `Capture(true)` instala um único hook `WH_GETMESSAGE` local à thread na thread de UI, e as mensagens de teclado e rato que pertencem à árvore de janelas do jogo (o formulário principal, os seus filhos e o formulário de pausa nativo oculto) são registadas como simples valores `ModuleInputEvent` (tipo, tecla virtual, coordenadas de cliente da janela de vista, botão, delta da roda, modificadores) e removidas da fila, pelo que nem o OMSI nem a VCL atuam sobre elas. `Drain()` devolve os eventos (são mantidos no máximo 256). Nada é subclassificado, nenhuma função de estado de teclas da user32 é intercetada e nada é enviado.

Só um módulo captura de cada vez. A captura é libertada quando o módulo pára ou falha, faça `Stop` o que fizer. Os diálogos modais nativos (guardar, opções, novo jogo) precisam de entrada real: libertar a captura antes de pedir um. A mecânica do hook está validada offline com uma janela de substituição (`ui-input.hook-self-check`); o seu comportamento contra a janela real do jogo ainda não foi observado.

<a id="the-omsi-version"></a>
## A versão do OMSI

`IRuntimeModuleContext.OmsiVersion` e `get_value("omsi_version")` dão a versão do produto OMSI que o próprio jogo comunica, por exemplo `2.3.004`: o texto que o OMSI escreve depois de `Version:` na linha 7 do seu `logfile.txt`, lido a partir da imagem em execução. **Nunca** é a versão de ficheiro PE do executável (a build de referência tem `FileVersion` `2.2.032`). Quando a imagem não tem um único texto desse tipo, é usada a versão do perfil de build quando a identidade do código é conhecida, caso contrário `unknown`. O hash do executável, o tamanho do seu código, os endereços e o nome do perfil não são expostos. Usar a versão para texto e para escolher comportamento; as capacidades (`IsCapabilityAvailable`) continuam a ser a fonte de verdade sobre o que funciona.

<a id="writing-a-module-contracts"></a>
## Escrever um módulo (contratos)

O componente gerido referencia `OmsiLaunch.Runtime.Modules` e `OmsiLaunch.Api` e implementa `IRuntimeModule`. A ponte associa estas duas referências aos seus próprios assemblies carregados (seja qual for o contexto de carregamento em que o plugin corre), pelo que uma cópia distribuída com o módulo é ignorada:

| Membro | Chamado |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Passo 4. Adquirir recursos através do contexto. |
| `void GameplayReady()` | Passo 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Passo 6, para cada operação declarada (sem o prefixo `module.<id>.`). Lançar `ModuleOperationException(code, message)` para uma falha tipada. |
| `void Stop()` | Passo 7. |

`IRuntimeModuleContext` oferece `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (um token de cancelamento), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess` e, a partir do protocolo 3, `Runtime` (ver [operações em processo](#operations-in-process)), `Content` (ver [conteúdo da comunidade](#community-content)), `Input` (ver [captura de entrada](#input-capture)) e `NativeComponent` (o componente nativo carregado, ou nulo). Um componente gerido que queira ticks implementa também `ITickingModule`. Nada no contexto expõe endereços, o hash do executável ou o seu layout de código. O componente tem de poder ser carregado pelo plugin de 32 bits (AnyCPU ou x86, .NET 6); uma cópia de `OmsiLaunch.Runtime.Modules` ou `OmsiLaunch.Api` ao lado dele é ignorada. Fixar todos os ficheiros que os programas do módulo carregam (para um programa .NET: a sua `.dll` ao lado do `.exe`), de modo que a integridade os cubra.

<a id="known-limitations"></a>
## Limitações conhecidas

- Os componentes nativos podem intercetar importações de `Omsi.exe` e métodos do dispositivo Direct3D; ainda não são oferecidos aos módulos hooks em prólogos de funções do OMSI nem em pontos de chamada.
- A tomada de controlo do menu de pausa (`ui.pause.*`) e a captura de entrada estão desenhadas estaticamente e validadas offline; não foram observadas numa sessão real do OMSI, e `ui.pause-menu.replace` só é resolvida numa identidade de código conhecida (alguns dos seus campos não têm derivação independente da build).
- A verificação da associação de conteúdo em processo usa o mapa que o arranque pediu (NEW_MAP); para uma situação guardada, a associação é verificada no planeamento contra o mapa da situação.
- Os hooks de dispositivo são hooks de vtable. Onde outro componente volta a escrever os métodos originais do dispositivo (observado na instalação de regressão pouco depois de o gameplay arrancar), um hook de dispositivo deixa de ser chamado e não pode ser removido; a limpeza comunica então `OL_E_MODULE_CLEANUP_INCOMPLETE` e o componente permanece carregado. Um método de dispositivo já intercetado por algo fora do registo é recusado (4); o evento de recusa nomeia o módulo que detém o método.
- Os módulos param numa saída normal do OMSI; uma paragem de sessão termina o OMSI, pelo que a limpeza de módulos dentro de uma sessão acontece apenas através de uma política de falha.
- Os avisos são mostrados quando o indicador na área de notificação existe, depois de o OMSI ter sido iniciado; um programa de sessão mostra a sua própria janela para tudo o que tenha de dizer antes disso.
