# Indicador na bandeja do Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Tradução da [página original em inglês](../../../reference/windows-tray.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Toda sessão de proprietário autônoma (iniciada com `OmsiLaunch.exe` ou `OmsiLaunchW.exe`) exibe um ícone na área de notificação que informa o estado da sessão e permite que o usuário a encerre. Esta página especifica o indicador conforme implementado por `SessionTrayIndicator`, `StatusWindow` e `StopConfirmationWindow` em `tools\OmsiLaunch.Cli\WindowsHost.cs`, pelo apresentador somente leitura `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) e pelo registro de strings `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), junto com as caixas de diálogo de falha do `OmsiLaunchW.exe` de `WindowsHost`. A bandeja é apenas um adaptador de apresentação: ela não é proprietária do OMSI nem da recuperação, e sua ação de parada sinaliza o mesmo caminho canônico de parada do proprietário que `session stop` (consulte a [referência da CLI](cli.md) e o [controle local](local-control.md)).

<a id="when-the-icon-exists"></a>
## Quando o ícone existe

| Etapa | Comportamento |
|---|---|
| Criação | Imediatamente depois que `StartSessionAsync` retorna, antes de a sessão chegar a `Running`, a menos que `SuppressTrayIcon` esteja definido. Portanto, o ícone existe durante `StartingProcess`, `WaitingForPlugin`, `StartingWorld` e `EnteringGameplay`. |
| Orçamento de inicialização | O thread de UI (`STA`, em segundo plano, chamado `OmsiLaunch tray`) precisa publicar o ícone em até 2 s. Caso contrário, ou diante de qualquer exceção durante a criação, o indicador é descartado e a sessão continua **sem** ícone; `startup-timeout` ou a exceção é registrado no log. Uma inicialização lenta nunca deixa um ícone visível órfão. |
| Remoção | No bloco `finally` do proprietário, depois que a sessão foi concluída ou falhou e antes de `CloseAsync`. O descarte envia o encerramento ao thread de UI (fecha o menu, a confirmação e a janela de status, depois termina o loop de mensagens), aguarda o thread (join) por até 2 s (`dispose-timeout` registrado no log quando excedido), oculta e descarta o `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (um campo de `LaunchSpec`, `Presentation.SuppressTrayIcon`, padrão `false`). Pode ser definido por `/spec` e pela API; não há flag da CLI. Integradores que exibem sua própria indicação da sessão o definem como `true`; nada mais na sessão muda. |
| Hosts | Tanto `OmsiLaunch.exe` (console) quanto `OmsiLaunchW.exe` (subsistema Windows) exibem o ícone; sessões do `OmsiLaunchW.exe` iniciadas com `/silent` não têm nenhuma outra superfície visível. |

<a id="icon-and-tooltip"></a>
## Ícone e dica de ferramenta

- Ícone: o ícone associado ao executável em execução (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, o ícone do OmsiLaunch embutido), com fallback para `SystemIcons.Application`.
- Texto da dica de ferramenta: `Tray.Running` (`OmsiLaunch is running`) (`OmsiLaunch em execução`). O texto não muda durante a parada (ainda não existe uma string de "parando"; o ícone permanece como está até que o proprietário o remova).
- Os estilos visuais são habilitados (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interação

| Action | Resultado |
|---|---|
| Clique com o botão direito | Torna a janela da bandeja a janela em primeiro plano (necessário para menus de ícones de notificação; sem isso, o menu pode ignorar cliques e nunca fechar enquanto o OMSI estiver na frente), depois abre o menu de contexto na posição real do cursor (`Cursor.Position`, não as coordenadas do evento, porque `NotifyIcon` pode informar `(0,0)` para eventos hospedados pelo shell). O menu é ajustado para caber na área de trabalho da tela sob o cursor. |
| Clique duplo | Abre a janela de status (o mesmo que o item de menu `Status`). |
| Clique com o botão esquerdo | Nenhuma ação. |
| Item de menu `Status` (`Tray.Status`) | Abre (ou ativa, se já estiver aberta) a janela de status somente leitura. |
| Separador | |
| Item de menu `End session` (`Tray.EndSession`, descrição acessível `Tray.EndSessionDescription`) | Abre a caixa de diálogo de confirmação. |

<a id="status-window-read-only-snapshot"></a>
### Janela de status (snapshot somente leitura)

Aberta pelo item de menu `Status` ou por um clique duplo no ícone (`SessionTrayIndicator.ShowStatus`). É uma caixa de diálogo fixa, centralizada e dimensionada automaticamente, sem entrada na barra de tarefas e com um único botão `Close` (`Fechar`) (`Status.Close`; `Escape` também a fecha). Escolher `Status` enquanto a janela já está aberta ativa essa janela sem reconstruí-la (ela mantém o snapshot da primeira abertura). Uma falha ao construir a janela é gravada em `tray-host.log`; isso não afeta a sessão.

**É um snapshot, não uma visualização ao vivo.** `SessionStatusPresenter.Create(plan, status, ui)` é executado uma vez, quando a janela é aberta: ele lê o `SessionPlan` resolvido da sessão (a especificação que foi efetivamente planejada) e um `SessionStatus` (apenas seu `State`). Nada é atualizado enquanto a janela permanece aberta, e ela nunca consulta o OMSI (nenhuma operação de runtime, nenhum valor de telemetria). Feche-a e abra-a novamente para ver um estado mais recente.

Título e cabeçalho da janela: o mesmo texto, `Status.SessionRunning` (`Session is running`) (`Sessão em execução`) quando `SessionStatus.State` é `Running`; caso contrário, o nome bruto de `SessionState` (por exemplo, `WaitingForPlugin` quando aberta durante a inicialização, já que o ícone existe antes de `Running`). `Status.Title` (`OmsiLaunch session status`) está definido na tabela de strings, mas não é usado por esta versão.

As seções aparecem nesta ordem, e uma seção é omitida quando não tem campos. Todo valor vem do `LaunchSpec`/`SessionPlan` planejado, nunca do OMSI.

| Seção (rótulo em inglês) | Campo (rótulo em inglês) | Exibido quando | Valor | Origem (equivalente público) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | sempre | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (qualquer outro modo; nunca alcançado porque `LastMapState` não é apto para execução) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | o plano resolveu uma identidade de conteúdo `map` | o `DisplayName` do mapa (o nome do diretório do mapa, por exemplo `Grundorf`); caso contrário, o nome de arquivo da identidade sem extensão | Entrada de `SessionPlan.ResolvedContent` com `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` fornece o mesmo `DisplayName` |
| `Session` | `Situation` | `SavedSituation` com uma identidade de situação | nome do arquivo sem extensão (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | um ponto de entrada foi solicitado | a identidade do ponto de entrada, se definida (nunca apta para execução nesta versão); caso contrário, o índice apresentado como inteiro (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | um perfil de sessão foi usado (`/predefined-profile`) | `name` do perfil | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | como acima, quando a predefinição tem nome | `name` da predefinição | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | foi solicitada uma data, hora ou clima explícito/do sistema | `DD/MM/YYYY` ou `System`; `HH:MM:SS` ou `System`; código ICAO, nome da predefinição ou `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | foi solicitado um campo do veículo do jogador | a identidade/o valor solicitado | `Spec.PlayerVehicle` |
| `Configuration` | um campo por configuração semântica | uma configuração foi definida (`/set`, `settings` do perfil, `LaunchSpec.Environment.*`) e é conhecida por `ConfigurationCatalog` | o valor solicitado; `%` acrescentado para chaves que terminam em `Percent`, ` m` para chaves que terminam em `DistanceMeters`. O rótulo é a chave de configuração com cada parte separada por ponto iniciando em maiúscula (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; os mesmos valores são as `PlannedMutations` do plano |
| `Presentation` | `Splash` | sempre | `Managed` ou `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | sempre | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

A seção `Environment` aparece em uma sessão em execução da Beta 4 quando uma data, hora, ano ou clima foi solicitado (essas solicitações são experimentais, consulte [limitações conhecidas](known-limitations.md)). A seção `Vehicle` nunca pode aparecer em uma sessão em execução: solicitar qualquer campo do veículo do jogador torna o plano não apto para execução, portanto nenhuma sessão desse tipo é iniciada. Ela existe para um build futuro e é coberta pelo teste offline do apresentador.

Evidência de runtime (UI pt-BR, closure de runtime `T01`): uma sessão NEW_MAP em Grundorf exibiu `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Os mesmos dados estão disponíveis para ferramentas sem a bandeja: `session status` pelo [plano de controle local](local-control.md) fornece `SessionId`, `State`, `Diagnostics` e `RuntimeEvents` (ao vivo), e a saída de `/plan --json` do proprietário (ou `PlanSessionAsync`) fornece a especificação planejada, o conteúdo resolvido e as mutações planejadas que a janela resume.

<a id="end-session-with-confirmation"></a>
### Encerrar a sessão (com confirmação)

1. `StopConfirmationWindow`: título `End session?` (`Encerrar sessão?`), mensagem `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (`O OMSI 2 será encerrado e a sessão gerenciada pelo OmsiLaunch será finalizada.`), botões `End session` (`Encerrar sessão`) (padrão, `DialogResult.OK`) e `Cancel` (`Cancelar`) (`Escape`). Uma segunda solicitação enquanto a caixa de diálogo está aberta a ativa em vez de empilhar outra.
2. Em `OK`, a bandeja chama `requestCanonicalStop`, que conclui o sinal `controlStopped` do proprietário; o proprietário então chama `StopAsync`: o OMSI é encerrado com `TerminateProcess` e todo arquivo pertencente à sessão é restaurado. A bandeja nunca encerra o OMSI por conta própria.
3. Se a solicitação lançar uma exceção, o erro é registrado no log e `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`) é exibido.
4. A bandeja não confirma o sucesso; o ícone desaparece quando o proprietário termina a restauração e descarta o indicador (closure de runtime `T01`: o proprietário terminou 607 ms depois que `End session` foi confirmado).
5. `Cancel` (ou fechar a caixa de diálogo) não faz nada: a sessão continua em execução (`T01`).
6. Uma parada que chega de outro lugar (`session stop`, Ctrl+C, `/observe-seconds`, término do OMSI) enquanto a janela de status ou a caixa de diálogo de confirmação está aberta as fecha como parte do descarte do indicador; o proprietário não espera pelo usuário (`T02`: o proprietário terminou 725 ms depois da parada pelo pipe, com as duas janelas abertas).

<a id="explorer-restart"></a>
## Reinício do Explorer

`TrayWindow` é uma janela nativa oculta que registra a mensagem de janela `TaskbarCreated`. Quando o Explorer (o shell) reinicia, ele transmite essa mensagem e o indicador adiciona o ícone novamente (`Visible = false; Visible = true`). Closure de runtime `T01`: depois que o `explorer.exe` foi encerrado e reiniciado pelo Windows, o ícone voltou a `Shell_TrayWnd` e o menu e a janela de status continuaram funcionando.

<a id="localization"></a>
## Localização

`WindowsUiStrings.Resolve` segue a **cultura de UI do Windows** (`CultureInfo.CurrentUICulture`), nunca o idioma do conteúdo do OMSI nem um idioma de perfil de sessão. Ordem de resolução: nome exato da cultura, depois o idioma de duas letras, depois inglês. Toda chave recorre ao inglês quando uma tradução não a possui.

| Chaves de cultura | Idioma |
|---|---|
| `en`, `en-US`, `en-GB` | Inglês (padrão e fallback) |
| `pt-BR` | Português do Brasil. `pt-PT` (e `pt` sozinho) recorre deliberadamente ao inglês. |
| `de`, `de-DE` | Alemão |
| `fr`, `fr-FR` | Francês |
| `pl`, `pl-PL` | Polonês |

As strings localizadas cobrem a dica de ferramenta, os dois itens de menu, a janela de status (cabeçalho, títulos das seções, rótulos dos campos, `Close`), os valores de modo e de apresentação e a caixa de diálogo de confirmação. O glossário é mantido em `docs\windows-ui-localization.md`; o teste offline `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifica a resolução e o apresentador.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Caixas de diálogo de falha do `OmsiLaunchW.exe`

Quando `OMSILAUNCH_WINDOWS_HOST=1` (definido pelo `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` substitui a saída de erro do console por uma caixa de mensagem modal com o título `OmsiLaunch` (ícone de erro): `<message>`, linha em branco, `Code: OL_E_...`, linha em branco, `See .omsilaunch\diagnostics for details.` Ela é exibida por todo `CliInput.WriteError` (erros de argumento, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, exceções classificadas), quando um plano de inicialização não é apto para execução (fallback `The session plan is not runnable.`; auditoria de documentação BUG-06) e quando a sessão não consegue chegar a `Running` (`The OMSI session did not reach gameplay.` com o último diagnóstico `OL_E_`, ou `OL_E_SESSION_START_FAILED` quando não há nenhum). O comportamento completo do `OmsiLaunchW.exe` está na [referência do OmsiLaunchW.exe](omsilaunchw.md). Sob o `OmsiLaunch.exe`, a mesma função não faz nada. Uma falha de inicialização do host .NET (códigos do shim `100`..`106`) é exibida pelo próprio shim nativo; consulte [códigos de saída](exit-codes.md).

<a id="log-location"></a>
## Local do log

`<root>\.omsilaunch\diagnostics\tray-host.log`, uma linha por entrada: timestamp UTC em ISO-8601, uma tabulação e depois a entrada. Entradas: `created`, `removed`, `startup-timeout`, `startup-cancelled` (um descarte concorreu com a inicialização e o loop foi ignorado), `dispose-timeout` e os textos completos das exceções de falhas de UI. O registro em log é feito na base do melhor esforço e nunca lança exceções. Os logs de host da sessão (`<sessionId>-host.log`) são gravados no mesmo diretório pelo proprietário; o log da bandeja não tem prefixo de sessão e não é removido pela retenção de 50 sessões.

<a id="lifecycle-guarantees"></a>
## Garantias do ciclo de vida

- A bandeja nunca é proprietária da sessão: ela não pode iniciar o OMSI, não pode restaurar arquivos e não pode contornar o caminho de parada do proprietário.
- Todo caminho de saída do proprietário (conclusão normal, término do OMSI, Ctrl+C, fechamento do console, parada pelo pipe, exceção, `/observe-seconds`) descarta o indicador antes de `CloseAsync`, de modo que nenhum ícone sobrevive à sua sessão, exceto quando o processo proprietário é encerrado à força (o Windows remove ícones órfãos na próxima passagem do mouse).
- A criação e o descarte são serializados sob um lock: um descarte que vence a corrida faz o thread de UI pular seu loop de mensagens e fazer a limpeza imediatamente.
- Todo o trabalho de Windows Forms acontece no thread STA dedicado; threads externos apenas enviam mensagens a ele por meio de um controle de marshalling oculto.

<a id="residual-caveats-from-the-code-comments"></a>
## Ressalvas residuais (dos comentários do código)

- Não existe texto de dica de ferramenta para "parando"; o ícone exibe `OmsiLaunch is running` até ser removido.
- `NotifyIcon` pode informar coordenadas de mouse `(0,0)` para eventos hospedados pelo shell; em vez delas, é lida a posição do cursor.
- As mensagens da bandeja continuam chegando enquanto a caixa de diálogo de confirmação está modal; uma segunda confirmação não é empilhada.
- Se o thread de UI não terminar dentro do orçamento de descarte de 2 s, o proprietário continua sem esperar (`dispose-timeout`).
- A janela de status é um snapshot dos valores planejados, tirado quando ela é aberta; ela não é atualizada e nunca lê o OMSI.

<a id="runtime-evidence"></a>
## Evidência de runtime

Observado em sessões reais na instalação autorizada durante a rodada de closure de runtime (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, UI do Windows em pt-BR; consulte o [status da validação em runtime](../status/runtime-validation-status.md)):

- O ícone é registrado na área de notificação real (`Shell_TrayWnd`) sob o `OmsiLaunchW.exe` e removido após a restauração (`T01`..`T04`).
- "Encerrar sessão" com confirmação aciona a parada canônica e a restauração exata; `Cancel` mantém a sessão em execução (`T01`, `T03`).
- O ícone é recriado após um reinício do Explorer (`TaskbarCreated`, `T01`).
- As janelas de status e de confirmação são fechadas pelo proprietário quando uma parada chega enquanto elas estão abertas (`T02`).
- Caixas de diálogo de falha do `OmsiLaunchW.exe` para um erro de argumento (`OL_E_INVALID_ARGUMENT`), ausência de sessão ativa (`OL_E_NO_ACTIVE_SESSION`) e uma sessão que falha antes da jogabilidade (`OL_E_WORLD_START_FAILED`) (`T04`). Na última, a caixa de diálogo exibe como mensagem o payload de falha do plugin.
- `/silent` se desvincula: o inicializador retorna enquanto o host do Windows mantém a sessão (`T04`).
- O orçamento de 4 s de `ProcessExit` ao fechar o console (`L04`, proprietário de console).

Não produzido: as caixas de diálogo de código de saída do shim de bootstrap (`100`..`106`).
