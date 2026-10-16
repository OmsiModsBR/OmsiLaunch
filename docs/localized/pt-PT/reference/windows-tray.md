# Indicador na área de notificação do Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Tradução da [página original em inglês](../../../reference/windows-tray.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Cada sessão de proprietário autónoma (iniciada com `OmsiLaunch.exe` ou `OmsiLaunchW.exe`) mostra um ícone na área de notificação que informa sobre a sessão e permite ao utilizador terminá-la. Esta página especifica o indicador tal como implementado por `SessionTrayIndicator`, `StatusWindow` e `StopConfirmationWindow` em `tools\OmsiLaunch.Cli\WindowsHost.cs`, pelo apresentador só de leitura `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) e pelo registo de strings `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), juntamente com as caixas de diálogo de falha de `OmsiLaunchW.exe` em `WindowsHost`. O ícone da área de notificação é apenas um adaptador de apresentação: não é proprietário do OMSI nem da recuperação, e a sua ação de paragem sinaliza o mesmo caminho de paragem canónico do proprietário que `session stop` (ver [referência da CLI](cli.md) e [controlo local](local-control.md)).

<a id="when-the-icon-exists"></a>
## Quando o ícone existe

| Passo | Comportamento |
|---|---|
| Criação | Imediatamente após o retorno de `StartSessionAsync`, antes de a sessão chegar a `Running`, a menos que `SuppressTrayIcon` esteja definido. Por isso, o ícone existe durante `StartingProcess`, `WaitingForPlugin`, `StartingWorld` e `EnteringGameplay`. |
| Orçamento de arranque | A thread de UI (`STA`, em segundo plano, com o nome `OmsiLaunch tray`) tem de publicar o ícone dentro de 2 s. Caso contrário, ou perante qualquer exceção durante a criação, o indicador é descartado e a sessão continua **sem** ícone; `startup-timeout` ou a exceção fica registado no log. Um arranque lento nunca deixa um ícone visível órfão. |
| Remoção | No bloco `finally` do proprietário, depois de a sessão ter terminado ou falhado e antes de `CloseAsync`. O descarte envia o encerramento para a thread de UI (fecha o menu, a confirmação e a janela de estado e depois termina o ciclo de mensagens), aguarda a thread durante até 2 s (`dispose-timeout` registado quando excedido), oculta e descarta o `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (um campo de `LaunchSpec`, `Presentation.SuppressTrayIcon`, predefinição `false`). Pode ser definido através de `/spec` e da API; não existe flag da CLI. Os integradores que apresentam o seu próprio elemento de sessão definem-no como `true`; nada mais muda na sessão. |
| Hosts | Tanto `OmsiLaunch.exe` (consola) como `OmsiLaunchW.exe` (subsistema Windows) mostram o ícone; as sessões de `OmsiLaunchW.exe` iniciadas com `/silent` não têm nenhuma outra superfície visível. |

<a id="icon-and-tooltip"></a>
## Ícone e descrição (tooltip)

- Ícone: o ícone associado ao executável em execução (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, o ícone incorporado do OmsiLaunch), recorrendo a `SystemIcons.Application` como alternativa.
- Texto da tooltip: `Tray.Running` (`OmsiLaunch is running`, «o OmsiLaunch está em execução»). O texto não muda durante a paragem (ainda não existe uma string de "a parar"; o ícone mantém-se como está até o proprietário o remover).
- Os estilos visuais estão ativados (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interação

| Action | Resultado |
|---|---|
| Clique com o botão direito | Torna a janela do ícone a janela em primeiro plano (necessário para os menus de ícones de notificação; sem isso o menu pode ignorar cliques e nunca fechar enquanto o OMSI estiver à frente) e depois abre o menu de contexto na posição real do cursor (`Cursor.Position`, não as coordenadas do evento, porque `NotifyIcon` pode comunicar `(0,0)` para eventos alojados pela shell). O menu é ajustado à área de trabalho do ecrã sob o cursor. |
| Duplo clique | Abre a janela de estado (o mesmo que o item de menu `Status`). |
| Clique com o botão esquerdo | Nenhuma ação. |
| Item de menu `Status` (`Tray.Status`) | Abre (ou ativa, se já estiver aberta) a janela de estado só de leitura. |
| Separador | |
| Item de menu `End session` (`Tray.EndSession`, descrição acessível `Tray.EndSessionDescription`) | Abre a caixa de diálogo de confirmação. |

<a id="status-window-read-only-snapshot"></a>
### Janela de estado (snapshot só de leitura)

Aberta pelo item de menu `Status` ou por um duplo clique no ícone (`SessionTrayIndicator.ShowStatus`). É uma caixa de diálogo fixa, centrada e de tamanho automático, sem entrada na barra de tarefas e com um único botão `Close` (`Status.Close`, que fecha a janela; `Escape` também a fecha). Escolher `Status` quando a janela já está aberta ativa essa janela sem a reconstruir (mantém o snapshot da sua primeira abertura). Uma falha na construção da janela é escrita em `tray-host.log`; não afeta a sessão.

**É um snapshot, não uma vista em tempo real.** `SessionStatusPresenter.Create(plan, status, ui)` é executado uma vez quando a janela abre: lê o `SessionPlan` resolvido da sessão (a especificação que foi efetivamente planeada) e um `SessionStatus` (apenas o seu `State`). Nada é atualizado enquanto a janela está aberta, e ela nunca consulta o OMSI (nenhuma operação de runtime, nenhum valor de telemetria). Para ver um estado mais recente, é preciso fechá-la e reabri-la.

Título e cabeçalho da janela: o mesmo texto, `Status.SessionRunning` (`Session is running`, «a sessão está em execução») quando `SessionStatus.State` é `Running`; caso contrário, o nome em bruto de `SessionState` (por exemplo `WaitingForPlugin` quando aberta durante o arranque, uma vez que o ícone existe antes de `Running`). `Status.Title` (`OmsiLaunch session status`) está definido na tabela de strings, mas não é usado nesta versão.

As secções aparecem por esta ordem, e uma secção é omitida quando não tem campos. Todos os valores vêm do `LaunchSpec`/`SessionPlan` planeado, nunca do OMSI.

| Secção (rótulo inglês) | Campo (rótulo inglês) | Mostrado quando | Valor | Origem (equivalente público) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | sempre | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (qualquer outro modo; nunca alcançado porque `LastMapState` não é executável) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | o plano resolveu uma identidade de conteúdo `map` | o `DisplayName` do mapa (o nome do diretório do mapa, por exemplo `Grundorf`), caso contrário o nome de ficheiro da identidade sem extensão | Entrada de `SessionPlan.ResolvedContent` com `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` devolve o mesmo `DisplayName` |
| `Session` | `Situation` | `SavedSituation` com uma identidade de situação | nome de ficheiro sem extensão (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | foi pedido um ponto de entrada | a identidade do ponto de entrada, se definida (nunca executável nesta versão), caso contrário o índice apresentado como inteiro (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | foi usado um perfil de sessão (`/predefined-profile`) | `name` do perfil | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | como acima, quando a predefinição tem nome | `name` da predefinição | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | foi pedida uma data, hora ou meteorologia explícita/do sistema | `DD/MM/YYYY` ou `System`; `HH:MM:SS` ou `System`; código ICAO, nome da predefinição ou `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | foi pedido um campo do veículo do jogador | a identidade/o valor pedido | `Spec.PlayerVehicle` |
| `Configuration` | um campo por definição semântica | foi definida uma definição (`/set`, `settings` do perfil, `LaunchSpec.Environment.*`) conhecida de `ConfigurationCatalog` | o valor pedido; `%` acrescentado para chaves que terminam em `Percent`, ` m` para chaves que terminam em `DistanceMeters`. O rótulo é a chave da definição com cada parte separada por pontos iniciada por maiúscula (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; os mesmos valores são as `PlannedMutations` do plano |
| `Presentation` | `Splash` | sempre | `Managed` ou `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | sempre | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

A secção `Environment` aparece numa sessão Beta 4 em execução quando foi pedida uma data, hora, ano ou meteorologia (esses pedidos são experimentais, ver [limitações conhecidas](known-limitations.md)). A secção `Vehicle` nunca pode aparecer numa sessão em execução: pedir qualquer campo do veículo do jogador torna o plano não executável, pelo que nenhuma sessão desse tipo é iniciada. Existe para uma build futura e está coberta pelo teste offline do apresentador.

Evidência de runtime (UI pt-BR, closure de runtime `T01`): uma sessão NEW_MAP em Grundorf mostrou `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Os mesmos dados estão disponíveis para ferramentas sem o ícone da área de notificação: `session status` através do [plano de controlo local](local-control.md) devolve `SessionId`, `State`, `Diagnostics` e `RuntimeEvents` (em tempo real), e a saída `/plan --json` do proprietário (ou `PlanSessionAsync`) devolve a especificação planeada, o conteúdo resolvido e as mutações planeadas que a janela resume.

<a id="end-session-with-confirmation"></a>
### Terminar a sessão (com confirmação)

1. `StopConfirmationWindow`: título `End session?`, mensagem `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (o OMSI 2 será fechado e a sessão gerida pelo OmsiLaunch terminará), botões `End session` (predefinido, `DialogResult.OK`) e `Cancel` (`Escape`). Um segundo pedido enquanto a caixa de diálogo está aberta ativa-a em vez de empilhar outra.
2. Em `OK`, o ícone da área de notificação chama `requestCanonicalStop`, que conclui o sinal `controlStopped` do proprietário; o proprietário chama então `StopAsync`: o OMSI é terminado com `TerminateProcess` e todos os ficheiros pertencentes à sessão são restaurados. O ícone da área de notificação nunca termina o próprio OMSI.
3. Se o pedido lançar uma exceção, o erro é registado no log e é mostrado `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`, ou seja, não foi possível terminar a sessão e o OMSI e a sua sessão gerida continuam ativos).
4. O ícone da área de notificação não confirma o êxito; o ícone desaparece quando o proprietário termina o restauro e descarta o indicador (closure de runtime `T01`: o proprietário terminou 607 ms depois de `End session` ter sido confirmado).
5. `Cancel` (ou fechar a caixa de diálogo) não faz nada: a sessão continua em execução (`T01`).
6. Uma paragem que chegue de outro lado (`session stop`, Ctrl+C, `/observe-seconds`, saída do OMSI) enquanto a janela de estado ou a caixa de diálogo de confirmação estão abertas fecha-as como parte do descarte do indicador; o proprietário não espera pelo utilizador (`T02`: o proprietário terminou 725 ms após a paragem pelo pipe, com ambas as janelas abertas).

<a id="explorer-restart"></a>
## Reinício do Explorador

`TrayWindow` é uma janela nativa oculta que regista a mensagem de janela `TaskbarCreated`. Quando o Explorador (a shell) reinicia, difunde essa mensagem e o indicador volta a adicionar o ícone (`Visible = false; Visible = true`). Closure de runtime `T01`: depois de `explorer.exe` ter sido terminado e reiniciado pelo Windows, o ícone voltou a estar em `Shell_TrayWnd` e o menu e a janela de estado continuaram a funcionar.

<a id="localization"></a>
## Localização

`WindowsUiStrings.Resolve` segue a **cultura da UI do Windows** (`CultureInfo.CurrentUICulture`), nunca o idioma do conteúdo do OMSI nem o idioma de um perfil de sessão. Ordem de resolução: nome exato da cultura, depois o idioma de duas letras, depois inglês. Qualquer chave recorre ao inglês quando uma tradução não a tem.

| Chaves de cultura | Idioma |
|---|---|
| `en`, `en-US`, `en-GB` | Inglês (predefinição e alternativa) |
| `pt-BR` | Português do Brasil. `pt-PT` (e `pt` sem região) recorre deliberadamente ao inglês. |
| `de`, `de-DE` | Alemão |
| `fr`, `fr-FR` | Francês |
| `pl`, `pl-PL` | Polaco |

As strings localizadas abrangem a tooltip, os dois itens de menu, a janela de estado (cabeçalho, títulos das secções, rótulos dos campos, `Close`), os valores de modo e de apresentação, e a caixa de diálogo de confirmação. O glossário é mantido em `docs\windows-ui-localization.md`; o teste offline `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifica a resolução e o apresentador.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Caixas de diálogo de falha de `OmsiLaunchW.exe`

Quando `OMSILAUNCH_WINDOWS_HOST=1` (definido por `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` substitui a saída de erro da consola por uma caixa de mensagem modal com o título `OmsiLaunch` (ícone de erro): `<message>`, linha em branco, `Code: OL_E_...`, linha em branco, `See .omsilaunch\diagnostics for details.` É mostrada por cada `CliInput.WriteError` (erros de argumentos, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, exceções classificadas), quando um plano de lançamento não é executável (alternativa `The session plan is not runnable.`; auditoria de documentação BUG-06) e quando a sessão não chega a `Running` (`The OMSI session did not reach gameplay.` com o último diagnóstico `OL_E_`, ou `OL_E_SESSION_START_FAILED` quando não existe nenhum). O comportamento completo de `OmsiLaunchW.exe` está na [referência de OmsiLaunchW.exe](omsilaunchw.md). Com `OmsiLaunch.exe`, a mesma função não faz nada. Uma falha de arranque do host .NET (códigos do shim `100`..`106`) é mostrada pelo próprio shim nativo; ver [códigos de saída](exit-codes.md).

<a id="log-location"></a>
## Localização do registo

`<root>\.omsilaunch\diagnostics\tray-host.log`, uma linha por entrada: carimbo temporal UTC ISO-8601, uma tabulação e depois a entrada. Entradas: `created`, `removed`, `startup-timeout`, `startup-cancelled` (um descarte entrou em corrida com o arranque e o ciclo foi ignorado), `dispose-timeout` e os textos completos das exceções para falhas da UI. O registo é feito na medida do possível e nunca lança exceções. Os registos do host da sessão (`<sessionId>-host.log`) são escritos no mesmo diretório pelo proprietário; o registo do ícone da área de notificação não tem prefixo de sessão e não é eliminado pela retenção de 50 sessões.

<a id="lifecycle-guarantees"></a>
## Garantias do ciclo de vida

- O ícone da área de notificação nunca é proprietário da sessão: não pode iniciar o OMSI, não pode restaurar ficheiros e não pode contornar o caminho de paragem do proprietário.
- Todos os caminhos de saída do proprietário (conclusão normal, saída do OMSI, Ctrl+C, fecho da consola, paragem pelo pipe, exceção, `/observe-seconds`) descartam o indicador antes de `CloseAsync`, pelo que nenhum ícone sobrevive à sua sessão, exceto quando o processo proprietário é terminado à força (o Windows remove os ícones órfãos na passagem seguinte do rato).
- A criação e o descarte são serializados sob um bloqueio: um descarte que ganhe a corrida faz com que a thread de UI ignore o seu ciclo de mensagens e faça a limpeza imediatamente.
- Todo o trabalho de Windows Forms acontece na thread STA dedicada; as threads externas apenas lhe enviam trabalho através de um controlo de marshalling oculto.

<a id="residual-caveats-from-the-code-comments"></a>
## Ressalvas residuais (dos comentários do código)

- Não existe texto de tooltip de "a parar"; o ícone mostra `OmsiLaunch is running` até ser removido.
- `NotifyIcon` pode comunicar coordenadas de rato `(0,0)` para eventos alojados pela shell; em vez disso, é lida a posição do cursor.
- As mensagens do ícone da área de notificação continuam a chegar enquanto a caixa de diálogo de confirmação é modal; não é empilhada uma segunda confirmação.
- Se a thread de UI não terminar dentro do orçamento de descarte de 2 s, o proprietário continua sem esperar (`dispose-timeout`).
- A janela de estado é um snapshot dos valores planeados tirado quando abre; não é atualizada e nunca lê o OMSI.

<a id="runtime-evidence"></a>
## Evidência de runtime

Observado em sessões reais na instalação autorizada durante a ronda de closure de runtime (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, UI do Windows em pt-BR; ver o [estado da validação em runtime](../status/runtime-validation-status.md)):

- O ícone é registado na área de notificação real (`Shell_TrayWnd`) sob `OmsiLaunchW.exe` e removido após o restauro (`T01`..`T04`).
- "End session" com confirmação conduz à paragem canónica e ao restauro exato; `Cancel` mantém a sessão em execução (`T01`, `T03`).
- O ícone é recriado após um reinício do Explorador (`TaskbarCreated`, `T01`).
- As janelas de estado e de confirmação são fechadas pelo proprietário quando chega uma paragem enquanto estão abertas (`T02`).
- Caixas de diálogo de falha de `OmsiLaunchW.exe` para um erro de argumento (`OL_E_INVALID_ARGUMENT`), ausência de sessão ativa (`OL_E_NO_ACTIVE_SESSION`) e uma sessão que falha antes de entrar no jogo (`OL_E_WORLD_START_FAILED`) (`T04`). No último caso, a caixa de diálogo mostra como mensagem o payload de falha do plugin.
- `/silent` desanexa: o lançador retorna enquanto o host Windows mantém a sessão (`T04`).
- O orçamento de 4 s de `ProcessExit` ao fechar a consola (`L04`, proprietário de consola).

Não produzido: as caixas de diálogo de códigos de saída do shim de arranque (`100`..`106`).
