# Referência da CLI

<!-- l10n: source=reference/cli.md source-sha256=cb5d0ea7b7ffe1315a6690239af6e0ae85d483e06995f7092e4d92cb75af3737 -->
> Tradução da [página original em inglês](../../../reference/cli.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página é a referência completa e normativa da linha de comando do OmsiLaunch `0.1.0-beta4`: os três executáveis, a gramática de argumentos, a ordem de despacho, cada palavra de comando, cada rota hierárquica, cada flag, os envelopes de saída e o comportamento de erro de cada comando. Ela é gerada a partir de `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), de `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` e dos dois shims nativos em `tools\OmsiLaunch.Bootstrapper`. Os resultados de processo estão listados em [códigos de saída](exit-codes.md); os códigos de erro em [erros](errors.md); invocações comentadas em [exemplos da CLI](cli-examples.md).

<a id="executables"></a>
## Executáveis

| Arquivo | Subsistema | Função | Diferenças |
|---|---|---|---|
| `OmsiLaunch.exe` | Console | Bootstrapper nativo (`OmsiLaunch.Bootstrapper.cpp`): resolve o próprio diretório, divide a linha de comando em tokens com `CommandLineToArgvW`, localiza o `hostfxr` por meio de `nethost.dll` e executa `OmsiLaunch.Controller.dll` com os mesmos argumentos. | A saída de console é escrita; o código de saída do processo é o código de saída do controlador gerenciado, ou um código do shim `100`..`106` se o host .NET não pôde ser iniciado. |
| `OmsiLaunchW.exe` | Windows (GUI) | O mesmo shim (`OmsiLaunch.WindowsHost.cpp`) compilado para o subsistema Windows. Ele define a variável de ambiente `OMSILAUNCH_WINDOWS_HOST=1` antes de iniciar o controlador. | Sem console: a saída de console é suprimida, a menos que `--json` seja informado (`WindowsHost.SuppressConsole`), as falhas são exibidas em caixas de mensagem (`WindowsHost.ShowFailure`: mensagem, `Code: OL_E_...` e a dica `See .omsilaunch\diagnostics for details.`), e uma falha do shim `100`..`106` é exibida como `OmsiLaunch could not start the .NET host (code N).` Comportamento completo: [referência do OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Gerenciado (x64, `net6.0-windows`, Windows Forms) | O próprio controlador. Nunca é invocado diretamente pelos usuários; os dois shims passam o caminho do controlador como primeiro argumento do host, de modo que ele nunca aparece na lista pública de argumentos. | Requer o runtime .NET 6 x64 com `Microsoft.WindowsDesktop.App`; veja [instalação](../getting-started/installation.md). |

O `nethost.dll` precisa ficar ao lado dos shims. Os shims não leem nenhum argumento por conta própria; todo argumento chega inalterado a `CliInput.Parse`, portanto `OmsiLaunch.exe` e `OmsiLaunchW.exe` aceitam exatamente a mesma sintaxe.

<a id="invocation-model"></a>
## Modelo de invocação

<a id="argument-grammar-cliinputparse"></a>
### Gramática de argumentos (`CliInput.Parse`)

| Forma | Significado |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Uma flag. A chave não diferencia maiúsculas de minúsculas; o valor é tudo o que vem depois do primeiro `:`. Chaves desconhecidas falham com `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), saída `2`. |
| `--key=value` | Um argumento de runtime para a operação de runtime selecionada (por exemplo `--handle=rv-000001`). Todo token `--` que contém `=` é um argumento de runtime, nunca uma flag. |
| `--json`, `/json` | Saída estruturada (veja [Formatos de saída](#output-formats)). `--json` é o único token `--` sem `=` que tem significado; ele é interpretado como a flag `/json`. |
| palavra solta | Se nenhuma palavra de comando foi vista ainda e a palavra é uma das [palavras de comando](#command-words), ela se torna o comando. Quando já há uma palavra de comando, toda palavra solta posterior é uma palavra de comando (a rota). Caso contrário, a primeira palavra solta é a raiz da instalação e qualquer palavra solta posterior é anexada à rota. |

Consequências: uma rota hierárquica (`time get`) não pode ser combinada com um argumento de instalação colocado depois dela (`time get D:\OMSI` é a rota desconhecida `time get d:\omsi`, saída `2`). `D:\OMSI time get` é aceito, mas é **modo proprietário** (uma nova sessão é iniciada e a operação é executada uma vez dentro dela). Erros de análise (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) e erros de perfil de sessão (`SessionProfileException`) são relatados antes que qualquer coisa seja executada, sempre com saída `2`.

<a id="installation-root"></a>
### Raiz da instalação

- Um argumento de instalação explícito em forma de palavra solta prevalece sobre `RootPath` em um arquivo `/spec` (`CliInput.BuildSpecAsync`).
- `.` significa o diretório que contém o executável (`AppContext.BaseDirectory`), nunca o diretório de trabalho do chamador (`CliInput.ResolveInstallationRoot`). Um pacote portátil depende disso.
- Quando o argumento é omitido, as operações em modo proprietário (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) também usam o diretório do executável. O caminho é normalizado com `Path.GetFullPath`.
- Comandos em modo cliente nunca recebem um argumento de instalação: eles se dirigem ao endpoint de controle local da instalação em que o executável está (`AppContext.BaseDirectory`). Veja [controle local](local-control.md).

<a id="owner-and-client"></a>
### Proprietário e cliente

- **Proprietário**: o processo que planeja, inicia, supervisiona e restaura uma sessão (`OwnerSession.RunAsync`). Ele detém o lease da instalação (`Local\OmsiLaunch.Installation.<sha256(root)>`) e a transação de configuração, expõe o endpoint de controle local enquanto a sessão está ativa e exibe o [ícone da bandeja](windows-tray.md). Exatamente um proprietário por instalação: se um proprietário já responde a `session.status` no endpoint de controle, uma segunda inicialização falha com `OL_E_SESSION_ALREADY_ACTIVE` (saída `7`).
- **Cliente**: qualquer invocação sem argumento de instalação que envia `session status`, `session stop`, `events read`, `events watch` ou uma operação de runtime. Ela é encaminhada pelo pipe de controle local; sem um proprietário, falha com `OL_E_NO_ACTIVE_SESSION` (saída `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Ordem de despacho (`CliProgram.RunAsync`)

1. `/silent` (quando ainda não está sendo executado sob o `OmsiLaunchW.exe`): inicia o `OmsiLaunchW.exe` a partir do diretório do executável por meio de `ShellExecute` (sem herança de handles) com os mesmos argumentos, exceto `/silent`/`--silent`, escreve o envelope `silent` (`delegated`, `host_process_id`) e retorna `0`. O processo de console não espera pela sessão; veja [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` retornam `7`.
2. `/version`: envelope `version` com `product`, `version` (versão informativa do assembly, carimbada a partir de `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); saída `0`.
3. `capabilities`: envelope com todos os descritores `PublicStableBeta` ou `PublicExperimental` do catálogo canônico `PublicCapabilityRegistry` (com suas operações, capacidades nativas, evidência e nota); saída `0`.
4. `help [family]`: envelope `help` com `usage`, `product_version`, `protocol_version`, `family` e os `commands` públicos (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), opcionalmente filtrados por família; saída `0`.
5. `profiles`: envelope com `requirement` (`OMSI 2`), o relatório `installation` da raiz de destino (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), os `known_layouts` e as `predicted_capabilities` resolvidas a partir do arquivo executável (a resolução no processo, na inicialização, é a que vale); saída `0`.
6. Operação de runtime do cliente (sem argumento de instalação e com uma rota ou `/runtime:`): os argumentos são validados com `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, saída `2`) e então `runtime.execute` é encaminhado com um timeout de 8 s (30 s para `road-vehicles.spawn`).
7. `session status` do cliente (750 ms), `session stop` (vinculado ao id da sessão ativa, 750 ms), `events read` (750 ms), `events watch` (consulta a cada 250 ms até Ctrl+C).
8. `detect`, ou **nenhum argumento** (nenhuma instalação, nenhum comando, nenhum `/?`, nenhum `/spec`, nenhuma flag de inicialização, nenhuma flag de recuperação, nenhum `/list`): enumera os processos `Omsi` e sonda o endpoint de controle (250 ms); envelope `detect`; saída `0`.
9. `/?` ou `/help`: imprime o texto de uso, saída `0`. Qualquer outra invocação que tenha uma palavra de comando mas nenhuma rota despachável (por exemplo `d3d` sozinho ou `session status D:\OMSI`) imprime o texto de uso e sai com `2`.
10. Modo proprietário. Pré-condições: `plugins\OmsiLaunch.Plugin.opl` e `plugins\OmsiLaunch.Native.x86.dll` precisam existir ao lado do executável (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, saída `7`). O `release-manifest.json` ao lado do executável, quando presente, fornece os hashes esperados dos plugins.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; envelope `recover` com `pending`, `recovered`, `diagnostics`; saída `8` somente quando uma restauração foi solicitada e não foi concluída; caso contrário, `0`.
12. `/list:<category>`: `DiscoverAsync`; envelope `content.list`; saída `0`. `/list:SessionProfiles` usa `ListSessionProfilesAsync`; envelope `session-profiles.list`.
13. Monta a `LaunchSpec` (`BuildSpecAsync`), planeja-a (`PlanSessionAsync`) e imprime o plano. `/plan` ou `/validate`: saída `0` se `IsRunnable`, senão `1`. Um plano não apto para execução nunca inicia o OMSI (saída `1`); sob o `OmsiLaunchW.exe`, uma inicialização com um plano não apto para execução exibe seu último diagnóstico `OL_E_` em uma caixa de mensagem (auditoria de documentação BUG-06). O planejamento também verifica o conjunto de arquivos instalado do plugin permanente contra o `release-manifest.json`, de modo que um plugin ausente ou alterado torna o plano não apto para execução (`OL_E_PERMANENT_PLUGIN_*`).
14. Sonda a existência de um proprietário (`OL_E_SESSION_ALREADY_ACTIVE`, saída `7`) e então executa `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Ciclo de vida do proprietário (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. A partir daqui, todo caminho de saída chega a `CloseAsync` em um bloco `finally`: exceções, Ctrl+C (`Console.CancelKeyPress`), fechamento do console / logoff (`AppDomain.ProcessExit` com um orçamento de 4 s para parada + restauração; o que restar é recuperado pelo journal na próxima inicialização), "End session" da bandeja, `session.stop` pelo pipe e `/observe-seconds`.
2. O ícone da bandeja é criado, a menos que `Presentation.SuppressTrayIcon` esteja definido na especificação.
3. Espera por `Running` durante `StartupTimeoutSeconds + 5` segundos. O status é impresso. Se o estado não for `Running`, saída `1` (o `OmsiLaunchW.exe` exibe `The OMSI session did not reach gameplay.` com o último diagnóstico `OL_E_` ou `OL_E_SESSION_START_FAILED`).
4. Os lotes de validação (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) são executados e escrevem seus artefatos.
5. O endpoint de controle local é iniciado.
6. `/runtime:<operation>` é executado uma vez (5 s, 15 s para `road-vehicles.spawn`); o resultado é escrito em `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` e impresso. Um comando de runtime que falha nunca encerra a sessão (em vez disso, `runtime_error` é impresso).
7. Espera: com `/observe-seconds:n`, a sessão é parada após `n` segundos **ou** antes, em uma parada pela bandeja/pipe, ou quando o OMSI termina; sem essa flag, o proprietário espera até o OMSI terminar ou até uma parada ser solicitada.
8. O status final é impresso; saída `0` se `Completed`, `1` caso contrário.

`session.stop`, "End session" da bandeja, Ctrl+C e `CloseAsync` solicitam todos a parada canônica: o OMSI é encerrado com `TerminateProcess` (a rotina de encerramento do próprio OMSI não é executada e o `options.cfg` não é reescrito pelo OMSI) e, em seguida, todo arquivo pertencente à sessão é restaurado. Veja [ciclo de vida da sessão](../concepts/session-lifecycle.md) e [transações e recuperação](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Palavras de comando

Todas as palavras aceitas na primeira posição (`CliInput.CommandWordsAccepted`):

| Palavra | Finalidade | Modo | Observações |
|---|---|---|---|
| `capabilities` | Listar as capacidades públicas | Local, sem sessão | Envelope `capabilities`. |
| `profiles` | Listar as variantes suportadas do `Omsi.exe` | Local, sem sessão | Envelope `profiles`. |
| `detect` | Relatar os processos `Omsi.exe` e um proprietário ativo | Local, sem sessão | Também é o padrão quando nenhum argumento é informado. Estados: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, `UNKNOWN_BINARY_FOUND` por processo quando o binário não pode ser inspecionado; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Uso e catálogo público de comandos | Local, sem sessão | `help <family>` filtra por família de capacidades (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Cliente | Exatamente uma palavra depois; qualquer outra coisa imprime o uso, saída `2`. `session plan`/`session start` são nomes de rota da API, não palavras da CLI: use `/plan` e `/new`. |
| `events` | `events read`, `events watch` | Cliente | `read` retorna a lista limitada de eventos uma vez; `watch` imprime cada novo evento (por `Sequence`) como um envelope `events.watch` a cada 250 ms até Ctrl+C (saída `0`), `4` quando nenhum proprietário responde, `7` em um erro de controle. |
| `time` | `time get`, `time set` | Rota de cliente | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Rota de cliente | |
| `map` | `map get` | Rota de cliente | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Rota de cliente | |
| `ui` | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action` | Rota de cliente | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Rota de cliente | |
| `player` | `player get` | Rota de cliente | |
| `humans` | `humans list`, `humans get`, `humans summary` | Rota de cliente | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Rota de cliente | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Rota de cliente | |
| `constants` | `constants list`, `constants get` | Rota de cliente | |
| `curves` | `curves list`, `curves evaluate` | Rota de cliente | |
| `hof` | `hof get` | Rota de cliente | |
| `drivers` | `drivers list` | Rota de cliente | |
| `tickets` | `tickets get` | Rota de cliente | |
| `d3d` | Palavra de família reservada | Nenhum | `d3d` **não tem rota hierárquica**: `d3d texture ...` é uma rota desconhecida (saída `2`) e `d3d` sozinho imprime o uso (saída `2`). As operações D3D são acessadas com `/runtime:d3d.status`, `/runtime:d3d.texture.create` e assim por diante (veja [Operações sem rota](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Rotas hierárquicas

`CliInput.HierarchicalRoutes` mapeia uma rota em minúsculas para um id de operação de runtime. Todas as rotas exigem uma sessão `Running` e são executadas pelo mailbox de runtime (`ExecuteRuntimeAsync`). As escritas de runtime alteram apenas o estado em memória do OMSI: nunca tocam em arquivos, não fazem parte da transação de configuração e **não** são revertidas na parada (o OMSI é encerrado). A estabilidade segue o `PublicCapabilityRegistry` e a [matriz de validação](../status/runtime-validation-status.md); detalhes e campos de resultado estão em [controle de runtime](runtime-control.md).

| Rota | Operação de runtime | Tipo | Exige Running | Altera o OMSI | Participação na restauração | Estabilidade | Observações |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Campos de relógio e calendário. |
| `time set` | `time.set` | Write | Sim | Sim (relógio em memória) | Nenhuma, não revertida | EXPERIMENTAL | Por exemplo `--minute=<0..59>`; escrita, releitura e restauração validadas em 2026-09-20. |
| `weather get` | `weather.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Sim | Sim (clima em memória) | Nenhuma, não revertida | EXPERIMENTAL | Por exemplo `--wind_speed=5 --cloud_type="Cumulus 1"`. Clima manual: as atualizações de METAR ao vivo param. Validação em runtime pendente. |
| `weather metar set` | `weather.metar.set` | Write | Sim | Sim (clima em memória; o OMSI salva o ICAO em `options.cfg`, que a sessão restaura) | Nenhuma, não revertida | EXPERIMENTAL | `--icao=<ICAO>`. Clima METAR ao vivo, como a caixa de diálogo de clima do OMSI o define. Validação em runtime pendente. |
| `weather actual get` | `weather.actual.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Estado do controlador de clima real/ICAO. |
| `map get` | `map.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Nome, arquivo e descrição do mapa, quantidade de tiles, intervalo de anos e lado do tráfego; revalidado em runtime no slot de mapa corrigido. |
| `camera get` | `camera.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Sim | Sim (escalares da câmera, p. ex. `--field_of_view=`) | Nenhuma, não revertida | EXPERIMENTAL | Escrita/releitura de FOV validada. |
| `camera lock` | `camera.lock` | Action | Sim | Sim (política com escopo de sessão) | Nenhuma | EXPERIMENTAL | Exige `--family=<0..3>` (motorista=0, passageiro=1, externa=2, mapa=3), `--preset=<n>` opcional (família 0 ou 1). Precisa de um veículo do jogador (por exemplo, uma situação salva). Validado em runtime no fechamento de runtime (`CAM01`); a string `RuntimeValidation` do registro ainda diz `STATICALLY_VALIDATED` (veja [capacidades](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Sim | Sim | Nenhuma | EXPERIMENTAL | Libera a política definida por `camera lock` (`CAM01`). |
| `ui pause-state` | `ui.pause.state` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | O byte de pausa do OMSI e o estado do menu de pausa nativo. |
| `ui pause-take-over` | `ui.pause.take-over` | Action | Sim | Sim (oculta o menu de pausa nativo pela VCL; o OMSI continua pausado) | Nenhuma, não revertida | EXPERIMENTAL | Exige o OMSI pausado com o menu exibido. Ainda não observado em uma sessão real. |
| `ui pause-restore` | `ui.pause.restore-native` | Action | Sim | Sim (exibe o menu de pausa nativo novamente) | Nenhuma | EXPERIMENTAL | Devolve o menu nativo. |
| `ui pause-action` | `ui.pause.action` | Action | Sim | Sim (a ação própria do menu nativo) | Nenhuma, não revertida | EXPERIMENTAL | Exige `--action=<resume|new-game|save|options|quit>`; executa depois da resposta, leia o resultado em `ui pause-state`. |
| `vehicles list` | `road-vehicles.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Retorna handles `rv-NNNNNN` com escopo de sessão. |
| `vehicles get` | `road-vehicle.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Exige `--handle=`. Handle obsoleto: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Contagens e estado do jogador, sem handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Sim | Sim (adiciona um RoadVehicle) | Nenhuma, não removido | EXPERIMENTAL | Exige `--model=Vehicles\...\*.bus`. Timeout de 30 s no cliente, 15 s no proprietário. Não define o veículo do jogador. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Sim | Sim | Nenhuma | EXPERIMENTAL | `PlaceRandomBus` perfilado. |
| `player get` | `player-vehicle.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Null semântico quando não há veículo do jogador. |
| `humans list` | `humans.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Retorna handles `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Exige `--handle=`. |
| `humans summary` | `humans.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Somente contagens. |
| `timetable get` | `timetable.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Estado do gerenciador da tabela de horários. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Parte da capacidade `timetable.read`; evidência de leitura em lote de 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable lines list` | `timetable.lines.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable tours list` | `timetable.tours.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable logs list` | `timetable.logs.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Números de veículo que o OMSI mantém fora do uso livre pela IA (reservados por tours da tabela de horários ou por listas fixas de IA). Paginado (`cursor`, `limit`, no máximo 256 por página). |
| `map tiles list` | `map.tiles.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Os tiles do mapa: posição na grade, índice próprio, arquivo e estado de carga, contagens de objetos, caminhos e splines. Paginado (`cursor`, `limit`, no máximo 256 por página). |
| `drivers list` | `drivers.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Registros de motoristas. |
| `tickets get` | `tickets.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Registros de pacotes de bilhetes. |
| `hof get` | `vehicle.hofs.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Exige `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Exige `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Sim | Não | Nenhuma | STABLE_BETA | Exige `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Exige `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Sim | Não | Nenhuma | STABLE_BETA | Exige `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Exige `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Exige `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Sim | Sim (variável de script) | Nenhuma, não revertida | EXPERIMENTAL | Exige `--handle=`, `--name=`, `--value=` (número finito). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Exige `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Exige `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Sim | Sim (estado de script em memória) | Nenhuma, não revertida | EXPERIMENTAL | Exige `--handle=`, `--name=`, `--value=`. `SetStringVariable` do próprio OMSI. Validação em runtime pendente. |
| `scripts trigger` | `vehicle.trigger` | Action | Sim | Sim (executa código de script do veículo) | Nenhuma, não revertida | EXPERIMENTAL | Exige `--handle=`, `--name=`, `--value=` (`1` pressionar, `0` soltar, `2` arrastar). Validação em runtime pendente. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Sim | Sim (reproduz sons do veículo) | Nenhuma, não revertida | EXPERIMENTAL | Exige `--handle=`, `--trigger=`; opcional `--file=`. Validação em runtime pendente. |

<a id="operations-without-a-route"></a>
### Operações sem rota

Estes ids de operação públicos (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) não têm rota hierárquica e são invocados com `/runtime:<operation>` mais `--key=value` ou `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` obrigatórios; `levels` opcional), `d3d.texture.describe` (`handle`; `level` opcional), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` obrigatórios; `level`, `x`, `y` opcionais), `d3d.texture.release` (`handle`). As operações D3D são EXPERIMENTAL; o ciclo de vida das texturas e a invalidação por reset do dispositivo são validados em runtime (fechamento de runtime `H02`, `D01`; veja [capacidades](capabilities.md)). `timetable.track-entries.list` e `timetable.tour-entries.list` são listas limitadas: um resultado que não cabe no slot de runtime é encurtado (`truncated=true`). `internal.road-vehicles.make-basic` é INTERNAL e é rejeitado com `OL_E_RUNTIME_OPERATION_UNKNOWN` tanto pela CLI quanto pela API.

## Flags

Todas as flags de `CliInput.KnownFlags`. A "Fase" é *de inicialização* (molda a `LaunchSpec`/o plano de uma nova sessão), *de runtime* (atua sobre uma sessão em execução) ou *de controle* (altera o comportamento da própria CLI). As flags interpretadas apenas por compatibilidade (`CliInput.AcceptedNoEffectFlags`) estão marcadas em sua linha.

<a id="control-and-output"></a>
### Controle e saída

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/?` | `/?` | desligada | controle | STABLE_BETA | Imprime o texto de uso, saída `0`. |
| `/help` | `/help` | desligada | controle | STABLE_BETA | Igual a `/?`. (A palavra solta `help` retorna, em vez disso, o catálogo estruturado.) |
| `/version` | `/version` | desligada | controle | STABLE_BETA | Envelope `version`, saída `0`. Avaliada antes de todos os outros comandos, exceto `/silent`. |
| `/json` | `/json` ou `--json` | desligada | controle | STABLE_BETA | Emite envelopes JSON; também força a saída de console mesmo sob o `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | desligada | controle | STABLE_BETA | Suprime a saída de sucesso legível por humanos. A saída JSON (`/json`) e os erros continuam sendo gravados; o código de saída não muda. |
| `/silent` | `/silent` (também `--silent`) | desligada | controle | EXPERIMENTAL | Delega a linha de comando inteira ao `OmsiLaunchW.exe` e retorna `0` assim que o processo host é iniciado. O resultado da sessão é relatado pelo `OmsiLaunchW.exe` (caixas de mensagem, ícone da bandeja), por `.omsilaunch\diagnostics` e pelo endpoint de controle local. A delegação e as caixas de diálogo de falha são validadas em runtime (fechamento de runtime `T04`); veja [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | controle | REMOVED | Removida na 0.1.0-beta4: rejeitada com `OL_E_INVALID_ARGUMENT` (código de saída `2`). O proprietário da sessão sempre serve o endpoint de controle local. |
| `/verbose` | `/verbose` | desligada | inicialização | PARTIAL | `DiagnosticsSpec.Verbose`. Os valores são levados na especificação; seu efeito se limita ao trace do host em `.omsilaunch\diagnostics`. |
| `/log` | `/log` | ligada (`DiagnosticsSpec.Log` tem padrão `true`) | inicialização | PARTIAL | `DiagnosticsSpec.Log`. Na prática, sempre ligada. |
| `/logall` | `/logall` | desligada | inicialização | PARTIAL | Define `Verbose`, `ProcessTrace`, `PluginTrace` e `NativeTrace` de uma só vez. |
| `/omsi-logall` | `/omsi-logall` | desligada | inicialização | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | desligada | inicialização | PARTIAL | Alias de `/trace-process`. |
| `/trace-process` | `/trace-process` | desligada | inicialização | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | desligada | inicialização | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: o plugin relata cada operação de runtime (`plugin.trace.operation`: operação, resultado, código, duração) ao trace do host da sessão. |
| `/trace-native` | `/trace-native` | desligada | inicialização | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: a camada nativa relata cada instalação e remoção de hook (`native.trace`) ao trace do host da sessão. |

<a id="planning-validation-and-harnesses"></a>
### Planejamento, validação e harnesses

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/plan` | `/plan` | desligada | inicialização | STABLE_BETA | Monta e imprime o `SessionPlan`, sem iniciar o OMSI. Saída `0` quando `IsRunnable`, `1` caso contrário. Exige uma seleção de inicialização (`/new`, `/saved`, `/spec` ou um argumento de instalação); `/plan` sozinho, sem mais nada, executa `detect`. |
| `/validate` | `/validate` | desligada | inicialização | STABLE_BETA | Idêntica a `/plan` neste build. |
| `/runtime-batch` | `/runtime-batch` | desligada | runtime (proprietário) | INTERNAL | Harness de validação: depois de `Running`, executa o conjunto de operações de leitura e escreve `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | desligada | runtime (proprietário) | INTERNAL | Harness de validação: leituras mais `time.set`, `camera.set` e `vehicle.variable.set` com restauração; escreve `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | desligada | runtime (proprietário) | INTERNAL | Harness de validação do ciclo de vida das texturas D3D; escreve `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | nenhum | runtime | STABLE_BETA (despacho) | Seleciona uma operação de runtime pública pelo id. Modo cliente (sem argumento de instalação): encaminhada ao proprietário. Modo proprietário: executada uma vez depois de `Running`. Ids desconhecidos: `OL_E_RUNTIME_OPERATION_UNKNOWN`, saída `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (repetível) | nenhum | runtime | STABLE_BETA (despacho) | Argumento de runtime; equivalente a `--key=value`. Sem `=`: `/runtime-arg requires key=value`, saída `2`. |

<a id="world-selection"></a>
### Seleção do mundo

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` é o modo padrão, mas uma inicialização só é solicitada quando uma de `/new`, `/saved`, `/last`, `/spec` está presente | inicialização | STABLE_BETA | NEW_MAP. Exige `/map` e `/entrypoint-index` (um plano sem um índice de ponto de entrada apresentado relata `OL_E_ENTRYPOINT_REQUIRED`; sem `/map`, nenhum mapa é resolvido). `/new` nunca seleciona um mapa silenciosamente. |
| `/saved` | `/saved:<file.osn>` | nenhum | inicialização | STABLE_BETA | SAVED_SITUATION. Mapa e posição vêm do `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` são rejeitados com `/saved` (saída `2`). Situação ausente: `OL_E_SITUATION_NOT_FOUND`; mapa dela ausente: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | nenhum | inicialização | EXPERIMENTAL | LAST_MAP_STATE: carrega o `laststn.osn` do próprio mapa (gravado pelo OMSI quando o mapa foi fechado) pelo ramo de último estado do formulário Start. Exige `/map`; um mapa sem `laststn.osn` gera `OL_E_SITUATION_NOT_FOUND`. Nunca uma heurística do `.osn` mais recente. Validação em runtime pendente. |
| `/map` | `/map:<identity>` (por exemplo `maps\Grundorf\global.cfg`) | nenhum | inicialização | STABLE_BETA | Identidade do mapa para `/new`, ou o escopo de `/list:Entrypoints`. Desconhecido: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | nenhum | inicialização | EXPERIMENTAL | Ponto de entrada pelo seu rótulo exato (com diferenciação de maiúsculas/minúsculas) ou por uma identidade de `/list:entrypoints`. O OMSI apresenta apenas o primeiro registro de cada rótulo; um registro posterior que o compartilha é recusado (`OL_E_ENTRYPOINT_NOT_PRESENTED`), e um desconhecido também (`OL_E_ENTRYPOINT_NOT_FOUND`). Validação em runtime pendente. Mutuamente exclusivo com `/entrypoint-index` (a identidade prevalece e limpa o índice). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | nenhum | inicialização | STABLE_BETA | Índice do ponto de entrada na lista apresentada (começando em 1, como o OMSI o apresenta). Obrigatório para um plano NEW_MAP apto para execução. |

<a id="date-time-and-weather"></a>
### Data, hora e clima

Estas flags são aceitas e levadas para a `LaunchSpec`. A data, a hora e o ano de início se aplicam apenas a sessões NEW_MAP: com `/saved` ou `/last`, o planejador os registra como `UNAVAILABLE` **e adiciona `OL_E_CAPABILITY_UNAVAILABLE`, de modo que o plano NÃO É APTO PARA EXECUÇÃO (saída `1`)**, porque uma situação salva mantém o próprio relógio. O clima se aplica a todos os modos de mundo. Todos são `EXPERIMENTAL`: o caminho nativo de inicialização está fechado estaticamente e a validação em runtime está pendente. Um arquivo `/spec` ou um perfil de sessão que os defina tem o mesmo efeito.

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` ou `/date:system` | não definido | inicialização | EXPERIMENTAL | `DateSpec` explícito/do sistema: a data de início de uma sessão NEW_MAP (veja [launchspec](launchspec.md#datespec-timespec-yearspec)). Valor que não pode ser interpretado: `OL_E_INVALID_ARGUMENT`, saída `2`; uma data impossível: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` ou `/time:system` | não definido | inicialização | EXPERIMENTAL | `TimeSpec` explícito/do sistema: a hora de início de uma sessão NEW_MAP. |
| `/year` | `/year:<n>` ou `/year:system` | não definido | inicialização | EXPERIMENTAL | `YearSpec`: o ano de início de uma sessão NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | não definido | inicialização | EXPERIMENTAL | `WeatherMode.Preset`: um arquivo da pasta `weather` da instalação, por nome (`Regen`) ou caminho (`weather\Regen.owt`); um arquivo inexistente gera `OL_E_WEATHER_INVALID` em tempo de planejamento. Carregado pelo próprio `loadWeather` do OMSI logo depois que o mundo inicia; o METAR ao vivo permanece desligado. Validação em runtime pendente. |
| `/weather-icao` | `/weather-icao:<code>` | não definido | inicialização | EXPERIMENTAL | `WeatherMode.Icao`: clima METAR ao vivo para uma estação ICAO de quatro caracteres (caso contrário, `OL_E_WEATHER_INVALID`). O OMSI salva a estação em `options.cfg`, que a sessão restaura. Validação em runtime pendente. |
| `/weather-real` | `/weather-real` | não definido | inicialização | EXPERIMENTAL | `WeatherMode.RealCurrent`: clima METAR ao vivo para a estação que o OMSI já tem configurada; sem uma, a inicialização falha com `OL_E_WEATHER_APPLY_FAILED`. Prevalece a última entre `/weather`, `/weather-icao`, `/weather-real`. Qualquer clima de início substitui o clima de uma situação salva. |

<a id="player-vehicle"></a>
### Veículo do jogador

Aceitas e resolvidas contra a instalação, mas não aplicadas pelo runtime: cada campo definido é `UNAVAILABLE` e adiciona `OL_E_CAPABILITY_UNAVAILABLE` (plano NÃO APTO PARA EXECUÇÃO, saída `1`).

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | não definido | inicialização | UNAVAILABLE | Resolvida primeiro (`OL_E_VEHICLE_NOT_FOUND` se desconhecido). |
| `/repaint` | `/repaint:<id>` | não definido | inicialização | UNAVAILABLE | Resolvida somente junto com `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | não definido | inicialização | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` se desconhecido. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | nenhum | inicialização | STABLE_BETA | Uma operação de [gerenciamento de HOF](hof-management.md) da sessão: `HofSourceKind` e `Source`. Exige pelo menos um `/hof-target`. Substitui o `HofManagement` de um `/spec`; com uma predefinição de perfil de sessão que tem `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Malformada: saída `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (repetível) | nenhum | inicialização | STABLE_BETA | Um veículo de destino abaixo de `Vehicles\`. Sem `/hof-source`: saída `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (repetível) | todos os arquivos | inicialização | STABLE_BETA | Restringe a origem a estes nomes de arquivo. Sem `/hof-source`: saída `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | inicialização | STABLE_BETA | `HofApplyMode`. Sem `/hof-source`: saída `2`. |
| `/module` | `/module:<id>` (repetível) | nenhum | inicialização | EXPERIMENTAL | Solicita um [módulo de runtime](runtime-modules.md) para a sessão (`LaunchSpec.Modules.Enable`, somado aos módulos de um arquivo `/spec`). Um módulo solicitado que não pode ser carregado torna o plano não apto para execução com seu código `OL_E_MODULE_*` (saída `1`). Suas operações são acessadas com `/runtime:module.<id>.<operation>` e `/runtime-arg`. Com uma predefinição de perfil de sessão que define `enabled: false` para ele: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (repetível) | nenhum | inicialização | EXPERIMENTAL | Mantém um módulo fora da sessão, inclusive um módulo carregado por padrão, por seu mapa ou pelo arquivo de configurações (`LaunchSpec.Modules.Disable`). Com uma predefinição que define `enabled: true` para ele: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Sem valor: saída `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (repetível) | nenhum | inicialização | EXPERIMENTAL | Define uma configuração de módulo (`LaunchSpec.Modules.Settings`); prevalece sobre o arquivo `/spec`, o arquivo de configurações e o padrão. Uma chave que a predefinição selecionada também define: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Fora do formato `<id>.<key>=<value>`: saída `2`. Valor inválido ou chave não declarada: diagnóstico de plano `OL_E_MODULE_SETTING_INVALID` (saída `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` sozinha significa `true` para uma configuração booleana | nenhum | inicialização | EXPERIMENTAL | Uma flag que um módulo instalado declara para uma de suas configurações (`flag` em seu manifesto; listada por `/list:RuntimeModules`). É resolvida quando uma sessão é planejada e equivale a `/module-setting:<id>.<key>=<value>`. Uma flag que nenhum módulo instalado declara, ou usada fora de uma inicialização: `Unknown argument`, saída `2`. Uma flag declarada por dois módulos: `OL_E_MODULE_SETTING_INVALID`, saída `2`. Uma flag de módulo nunca substitui uma flag integrada. |
| `/fleet` | `/fleet:<n>` | não definido | inicialização | UNAVAILABLE | Número de frota. |
| `/registration` | `/registration:<text>` | não definido | inicialização | UNAVAILABLE | Matrícula (placa). |
| `/no-vehicle` | `/no-vehicle` | desligada | inicialização | STABLE_BETA | Remove qualquer veículo do jogador da base (`/spec` ou perfil). Inofensiva. |

<a id="configuration-overlays"></a>
### Overlays de configuração

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (repetível; chaves sem diferenciação de maiúsculas/minúsculas) | nenhum | inicialização | STABLE_BETA | Overlay semântico de `options.cfg` a partir do `ConfigurationCatalog` (por exemplo `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Chave desconhecida: `OL_E_UNKNOWN_SETTING` (saída `2`); chave somente leitura (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (saída `2`); valor fora do intervalo ou malformado: `OL_E_INVALID_SETTING_VALUE` quando o overlay é montado. O overlay é uma mutação da sessão: é registrado no snapshot, aplicado antes de o OMSI iniciar e restaurado byte a byte na parada (RV-005 `RUNTIME_VALIDATED`). Conflito com uma chave pertencente à predefinição de um perfil selecionado: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Apresentação da tela de abertura

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (sem diferenciação de maiúsculas/minúsculas) | `Managed` | inicialização | STABLE_BETA | `Managed`: os BMPs empacotados de 640x480 e 24 bits são copiados uma vez para `<root>\.omsilaunch\assets\splash` e `GUI\NewSplashscreen_ENG.bmp` mais `GUI\NewSplashscreen_<lang>.bmp` são sobrepostos de forma transacional e restaurados exatamente (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (aliases): arquivos do OMSI intocados. Valor ausente: `/splash requires Unset, Native, or Managed`, saída `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (também `pt-BR`, `de`, `fr`, `en`; qualquer outro valor recai em `ENG`) | `[language]` do `options.cfg`, senão `ENG` | inicialização | STABLE_BETA | Seleciona o arquivo de destino localizado. |
| `/splash-assets` | `/splash-assets:<directory>` (caminhos relativos são resolvidos abaixo da raiz da instalação) | `<root>\.omsilaunch\assets\splash`, senão o conjunto empacotado | inicialização | STABLE_BETA | Diretório de assets personalizado; precisa conter `ENG.bmp` e, para um idioma diferente do inglês, `<lang>.bmp`. Erros: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (apresentados como `OL_E_SESSION_PRESENTATION_INVALID` no plano; não apto para execução). |

<a id="internet-textures"></a>
### Texturas da internet

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | inicialização | EXPERIMENTAL | `Native`: intocado. `Disabled`: o downloader perfilado dentro do processo é suprimido. `Override`: o perfil `.itx` informado é sobreposto como `Texture\standard.itx`; todo destino HTTP(S) listado nele, mais `Texture\standard.ipr`, se tornam exclusões da sessão (removidos durante a sessão, restaurados na parada). Valor ausente: saída `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | nenhum | inicialização | EXPERIMENTAL | Obrigatória com `Override` (`OL_E_ITX_PROFILE_REQUIRED`, saída `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (precisa ser composto de pares de linhas URL/destino com URLs `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (os destinos precisam ser resolvidos dentro de `Texture\`, sem caminhos absolutos, `..` ou reparse points). |

<a id="session-profiles"></a>
### Perfis de sessão

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | nenhum | inicialização | STABLE_BETA (compilação; `OmsiLaunch.ProfileTests` offline) | Carrega `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (veja [perfis de sessão](session-profiles.md)). Exige `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, saída `2`). O bloco `new:` só se aplica com `/new`; `compatibility.maps` é imposto para `/new` e `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Flags explícitas que colidem com um campo pertencente ao perfil são rejeitadas com `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): mapa/ponto de entrada/data/hora/ano/clima quando o bloco `new:` os define, chaves `/set` pertencentes à predefinição, flags de splash quando a predefinição tem `presentation`, flags de texturas da internet quando tem `internet-textures`, timeouts quando tem `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | nenhum | inicialização | STABLE_BETA | Seleciona a predefinição pelo `index`. Fora do intervalo: saída `2`. |

<a id="launchspec-file"></a>
### Arquivo LaunchSpec

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | nenhum | inicialização | STABLE_BETA (carregador testado offline; semântica da sessão idêntica à das flags) | Carrega um arquivo JSON `LaunchSpec` como base (veja [LaunchSpec](launchspec.md)) e marca uma inicialização como solicitada. Regras (`LaunchSpecJson`): o arquivo precisa existir (`OL_E_SPEC_NOT_FOUND`, saída `6`); no máximo 1 MiB (`OL_E_SPEC_TOO_LARGE`, saída `2`); a raiz precisa ser um objeto (`OL_E_SPEC_INVALID`); nomes de propriedades sem diferenciação de maiúsculas/minúsculas; comentários `//` e vírgulas finais permitidos; profundidade de no máximo 32; toda propriedade desconhecida é rejeitada com seu caminho JSON (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, saída `2`). |

**Precedência** (`CliInput.BuildSpecAsync`): padrões → arquivo `/spec` → `/predefined-profile` (substitui `Installation` e `World` e depois aplica o perfil) → flags explícitas. Um argumento de instalação explícito prevalece sobre o `RootPath` da especificação. `/no-vehicle` remove o veículo do jogador da especificação; `/vehicle` e as flags relacionadas são mescladas a ele campo a campo. As chaves de `/set` são mescladas em `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` só sobrescrevem quando informadas. `/startup-timeout` e `/shutdown-timeout` só sobrescrevem quando informadas; `Presentation.SuppressTrayIcon` vem somente da especificação (não há flag). As flags de diagnóstico são combinadas por OR com o `Diagnostics` da especificação.

<a id="content-discovery"></a>
### Descoberta de conteúdo

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; as categorias são os valores de `ContentQueryKind` `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (sem diferenciação de maiúsculas/minúsculas) | nenhum | local, sem sessão | STABLE_BETA | `DiscoverAsync` sobre a instalação; envelope `content.list` com entradas `Identity`, `Kind`, `DisplayName`; saída `0`. `SessionProfiles` responde com `ListSessionProfilesAsync` (envelope `session-profiles.list`) e `RuntimeModules` com `ListRuntimeModulesAsync` (envelope `runtime-modules.list`: id, versão, ativação, operações e validade de cada módulo, e suas configurações com tipo, padrão e flag). Categoria desconhecida: `Unknown discovery category`, saída `2`. Reparse points (junctions/symlinks) são ignorados, e os arquivos do OMSI são lidos como Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | nenhum | local | STABLE_BETA | Escopo encaminhado para todas as categorias, exceto `Entrypoints`, que usa `/map` como escopo. |

<a id="timeouts-and-observation"></a>
### Timeouts e observação

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` segundos | valor da especificação/do perfil, senão `180` | inicialização | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. O proprietário espera por `Running` durante esse valor mais 5 s; `OL_E_STARTUP_TIMEOUT` encerra a sessão com saída `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` segundos | valor da especificação/do perfil, senão `30` | inicialização | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: com `/cooperative-shutdown`, por quanto tempo o supervisor espera o OMSI sair antes de encerrá-lo à força; sem ela, o OMSI é encerrado imediatamente e o valor não tem efeito. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | desligada | inicialização | EXPERIMENTAL | `Behavior.CooperativeShutdown`: encerrar uma sessão em execução pede ao OMSI que saia pela sua própria rotina de saída (a que roda depois que o usuário confirma a saída: ela salva `options.cfg` e o `laststn.osn` do mapa, e depois fecha) e espera `ShutdownTimeoutSeconds` antes de encerrá-lo à força. Opcional até ser validada em runtime; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` quando o OMSI precisou ser encerrado à força. Os arquivos da sessão são restaurados depois da saída em ambos os casos. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | nenhum (executa até o OMSI terminar ou uma parada ser solicitada) | runtime (proprietário) | STABLE_BETA | Limite superior da fase de execução: após `n` segundos em `Running`, a parada canônica é solicitada. Uma parada pela bandeja ou pelo pipe, ou o término do OMSI, a encerra antes. `0` para imediatamente depois de `Running`. |

<a id="recovery"></a>
### Recuperação

| Flag | Sintaxe e valores | Padrão | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | desligada | local | STABLE_BETA | Informa se `<root>\.omsilaunch\journal.json` está pendente (`pending`), nunca restaura; saída `0`. Obtém o lease da instalação: `OL_E_INSTALLATION_BUSY` (saída `7`) enquanto um proprietário o detém. |
| `/recover` | `/recover` | desligada | local | STABLE_BETA | Restaura um journal pendente (os backups são verificados antes contra o SHA-256 do snapshot; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` relatados em `diagnostics`). Saída `0` quando nada estava pendente ou a restauração foi concluída; `8` quando um journal estava pendente e continua pendente. Recusada com `OL_E_INSTALLATION_BUSY` enquanto o processo do OMSI registrado no journal (PID, hora de criação, caminho do exe) ou, para um journal além de `HandoffCreated` sem PID, qualquer `Omsi.exe` daquela raiz estiver ativo. Toda inicialização de sessão executa automaticamente a mesma recuperação antes de ler a instalação. |
| `/clear-module-data` | `/clear-module-data:<module id>` | nenhum | local, sem sessão | EXPERIMENTAL | Exclui o diretório de dados do módulo `<root>\.omsilaunch\module-data\<id>` e nada mais; envelope `module-data.clear` com `module` e `cleared` (`false` quando não havia nada a excluir); saída `0`. Obtém o lease da instalação: `OL_E_INSTALLATION_BUSY` (saída `7`) enquanto uma sessão possui a instalação. Uma pasta de dados de módulo alcançada por um link é recusada (`OL_E_MODULE_INTEGRITY`); um link dentro dela é removido como link. |
| `/pack-module` | `/pack-module:<module directory>` | nenhum | local, sem sessão | EXPERIMENTAL | Grava os pins SHA-256 dos arquivos que o `module.json` do diretório nomeia (componentes e assets) e reescreve somente esses valores; envelope `module.pack` (`module`, `directory`, `written`, `intact`, `files`); saída `0`, `2` para um manifesto que não pode ser interpretado ou um arquivo ausente. Nunca toca em uma instalação. Consulte [empacotar um módulo](runtime-modules.md#packing-a-module). |
| `/verify-module` | `/verify-module:<module directory>` | nenhum | local, sem sessão | EXPERIMENTAL | Informa o mesmo sem gravar (`module.verify`); saída `0` quando todo arquivo com pin corresponde (um arquivo sem pin não é uma divergência), `7` quando um pin não corresponde, `2` para um manifesto que não pode ser interpretado ou um arquivo ausente. |

<a id="output-formats"></a>
## Formatos de saída

- **Envelope de sucesso** (`CliInput.WriteEnvelope`, com `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, indentado. As respostas encaminhadas de `session status` e `events read` adicionam um membro `metadata` quando eventos mais antigos foram omitidos para caber no frame de controle (`events_dropped_count`, veja [controle local](local-control.md)). Sem `--json`, apenas `<object>` é impresso como JSON indentado, seguido de `Note: <n> older events were omitted to fit the control frame.` quando eventos foram descartados.
- **Envelope de erro** (`CliInput.WriteError`, com `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Sem `--json`: `OL_E_<CODE>: message` em uma linha. Categorias: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Sob o `OmsiLaunchW.exe`, o mesmo código e a mesma mensagem são exibidos em uma caixa de mensagem.
- **Plano e status** (`CliInput.Write`): os registros `SessionPlan`, `SessionStatus` e `RuntimeCommandResult` são impressos como JSON indentado **sem** envelope. Sem `--json`, um plano é resumido como `Plan: READY profile=Omsi23004_692EBFBF` ou `Plan: NOT RUNNABLE profile=...`; os outros registros continuam sendo impressos como JSON. Os valores de enum são serializados como inteiros (`SessionState.Running` é `14`, `Completed` é `18`, `Failed` é `19`).
- Nomes de comando usados nos envelopes: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile` e o id da operação de runtime para comandos de runtime encaminhados.
- Sob o `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`), nada é escrito no console, a menos que `--json` seja informado.

<a id="errors-per-command"></a>
## Erros por comando

| Comando | Códigos de erro típicos | Saída |
|---|---|---|
| Qualquer falha de análise | `OL_E_INVALID_ARGUMENT`, códigos de perfil de sessão (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Rota de cliente, `/runtime` (cliente) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` retornados pelo proprietário, p. ex. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Verificação prévia do proprietário | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); pendente mas não recuperado (`8`) | `7`, `8` |
| `/list` | categoria desconhecida (`2`); `OL_E_INSTALLATION_NOT_FOUND`/diretórios ausentes (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, inicialização | diagnósticos do plano: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (conjunto de arquivos instalado do plugin), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informativo) | `1` |
| Início da sessão | `OL_E_PLAN_NOT_RUNNABLE` (replanejamento no início, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normalmente relatados pelo planejamento como diagnóstico do plano, saída `1`; `7` somente se os arquivos do plugin mudarem entre o planejamento e o início), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (sessão `Failed`, `1`) | `1`, `7` |
| Exceção não tratada em qualquer ponto | classificada por `CliProgram.Classify` (veja [códigos de saída](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Ambiente

| Variável | Definida por | Efeito |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: saída de console suprimida, falhas em caixas de mensagem, `/silent` não é delegado novamente. |

<a id="see-also"></a>
## Veja também

[Exemplos da CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [códigos de saída](exit-codes.md) · [erros](errors.md) · [controle local](local-control.md) · [bandeja do Windows](windows-tray.md) · [controle de runtime](runtime-control.md) · [capacidades](capabilities.md) · [LaunchSpec](launchspec.md) · [perfis de sessão](session-profiles.md) · [empacotamento](packaging.md) · [compatibilidade](compatibility.md) · [limitações conhecidas](known-limitations.md) · [API pública](public-api.md)
