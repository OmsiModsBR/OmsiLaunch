# Referência da CLI

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> Tradução da [página original em inglês](../../../reference/cli.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página é a referência completa e normativa da linha de comandos do OmsiLaunch `0.1.0-beta4`: os três executáveis, a gramática de argumentos, a ordem de despacho, todas as palavras de comando, todas as rotas hierárquicas, todas as flags, os envelopes de saída e o comportamento de erro de cada comando. É gerada a partir de `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), de `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` e dos dois shims nativos em `tools\OmsiLaunch.Bootstrapper`. Os resultados de processo estão listados em [códigos de saída](exit-codes.md); os códigos de erro em [erros](errors.md); invocações comentadas em [exemplos da CLI](cli-examples.md).

<a id="executables"></a>
## Executáveis

| Ficheiro | Subsistema | Função | Diferenças |
|---|---|---|---|
| `OmsiLaunch.exe` | Consola | Bootstrapper nativo (`OmsiLaunch.Bootstrapper.cpp`): determina o seu próprio diretório, divide a linha de comandos em tokens com `CommandLineToArgvW`, localiza o `hostfxr` através de `nethost.dll` e executa `OmsiLaunch.Controller.dll` com os mesmos argumentos. | A saída de consola é escrita; o código de saída do processo é o código de saída do controlador gerido, ou um código do shim `100`..`106` se não tiver sido possível iniciar o host .NET. |
| `OmsiLaunchW.exe` | Windows (GUI) | O mesmo shim (`OmsiLaunch.WindowsHost.cpp`) compilado para o subsistema Windows. Define a variável de ambiente `OMSILAUNCH_WINDOWS_HOST=1` antes de iniciar o controlador. | Sem consola: a saída de consola é suprimida, a menos que seja indicado `--json` (`WindowsHost.SuppressConsole`), as falhas são apresentadas em caixas de mensagem (`WindowsHost.ShowFailure`: mensagem, `Code: OL_E_...` e a indicação `See .omsilaunch\diagnostics for details.`), e uma falha do shim `100`..`106` é apresentada como `OmsiLaunch could not start the .NET host (code N).` Comportamento completo: [referência de OmsiLaunchW.exe](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Gerido (x64, `net6.0-windows`, Windows Forms) | O próprio controlador. Nunca é invocado diretamente pelos utilizadores; ambos os shims passam o caminho do controlador como primeiro argumento do host, pelo que este nunca aparece na lista pública de argumentos. | Requer o runtime .NET 6 x64 com `Microsoft.WindowsDesktop.App`; ver [instalação](../getting-started/installation.md). |

O `nethost.dll` tem de estar junto dos shims. Os próprios shims não leem nenhum argumento; todos os argumentos chegam inalterados a `CliInput.Parse`, pelo que `OmsiLaunch.exe` e `OmsiLaunchW.exe` aceitam exatamente a mesma sintaxe.

<a id="invocation-model"></a>
## Modelo de invocação

<a id="argument-grammar-cliinputparse"></a>
### Gramática de argumentos (`CliInput.Parse`)

| Forma | Significado |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | Uma flag. A chave não distingue maiúsculas de minúsculas; o valor é tudo o que vem depois do primeiro `:`. Chaves desconhecidas falham com `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), saída `2`. |
| `--key=value` | Um argumento de runtime para a operação de runtime selecionada (por exemplo `--handle=rv-000001`). Qualquer token `--` que contenha `=` é um argumento de runtime, nunca uma flag. |
| `--json`, `/json` | Saída estruturada (ver [Formatos de saída](#output-formats)). `--json` é o único token `--` sem `=` com significado; é interpretado como a flag `/json`. |
| palavra simples | Se ainda não tiver sido vista nenhuma palavra de comando e a palavra for uma das [palavras de comando](#command-words), torna-se o comando. Uma vez presente uma palavra de comando, todas as palavras simples seguintes são palavras de comando (a rota). Caso contrário, a primeira palavra simples é a raiz da instalação e qualquer palavra simples posterior é acrescentada à rota. |

Consequências: uma rota hierárquica (`time get`) não pode ser combinada com um argumento de instalação colocado depois dela (`time get D:\OMSI` é a rota desconhecida `time get d:\omsi`, saída `2`). `D:\OMSI time get` é aceite, mas é **modo proprietário** (é iniciada uma nova sessão e a operação é executada uma vez dentro dela). Os erros de interpretação (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) e os erros de perfil de sessão (`SessionProfileException`) são comunicados antes de qualquer execução, sempre com saída `2`.

<a id="installation-root"></a>
### Raiz da instalação

- Um argumento de instalação explícito, sob a forma de palavra simples, prevalece sobre `RootPath` num ficheiro `/spec` (`CliInput.BuildSpecAsync`).
- `.` significa o diretório que contém o executável (`AppContext.BaseDirectory`), nunca o diretório de trabalho do chamador (`CliInput.ResolveInstallationRoot`). Um pacote portátil depende disto.
- Quando o argumento é omitido, as operações em modo proprietário (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) também usam o diretório do executável. O caminho é normalizado com `Path.GetFullPath`.
- Os comandos em modo cliente nunca recebem um argumento de instalação: dirigem-se ao endpoint de controlo local da instalação onde se encontra o executável (`AppContext.BaseDirectory`). Ver [controlo local](local-control.md).

<a id="owner-and-client"></a>
### Proprietário e cliente

- **Proprietário**: o processo que planeia, inicia, supervisiona e restaura uma sessão (`OwnerSession.RunAsync`). Detém o lease da instalação (`Local\OmsiLaunch.Installation.<sha256(root)>`) e a transação de configuração, expõe o endpoint de controlo local enquanto a sessão está ativa e mostra o [ícone da área de notificação](windows-tray.md). Exatamente um proprietário por instalação: se um proprietário já responder a `session.status` no endpoint de controlo, um segundo lançamento falha com `OL_E_SESSION_ALREADY_ACTIVE` (saída `7`).
- **Cliente**: qualquer invocação sem argumento de instalação que envie `session status`, `session stop`, `events read`, `events watch` ou uma operação de runtime. É reencaminhada pelo pipe de controlo local; sem proprietário, falha com `OL_E_NO_ACTIVE_SESSION` (saída `4`).

<a id="dispatch-order-cliprogramrunasync"></a>
### Ordem de despacho (`CliProgram.RunAsync`)

1. `/silent` (quando ainda não está a ser executado sob `OmsiLaunchW.exe`): iniciar `OmsiLaunchW.exe` a partir do diretório do executável através de `ShellExecute` (sem herança de handles) com os mesmos argumentos exceto `/silent`/`--silent`, escrever o envelope `silent` (`delegated`, `host_process_id`) e devolver `0`. O processo de consola não espera pela sessão; ver [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` devolvem `7`.
2. `/version`: envelope `version` com `product`, `version` (versão informativa do assembly, carimbada a partir de `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); saída `0`.
3. `capabilities`: envelope com todos os descritores `PublicStableBeta` ou `PublicExperimental` do catálogo canónico `PublicCapabilityRegistry` (com as suas operações, capacidades nativas, evidência e nota); saída `0`.
4. `help [family]`: envelope `help` com `usage`, `product_version`, `protocol_version`, `family` e os `commands` públicos (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), opcionalmente filtrados por família; saída `0`.
5. `profiles`: envelope com `requirement` (`OMSI 2`), o relatório `installation` da raiz de destino (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), as `known_layouts` e as `predicted_capabilities` resolvidas a partir do ficheiro executável (a resolução em processo no arranque é a que faz fé); saída `0`.
6. Operação de runtime do cliente (sem argumento de instalação e com uma rota ou `/runtime:`): argumentos validados com `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, saída `2`); depois `runtime.execute` é reencaminhado com um timeout de 8 s (30 s para `road-vehicles.spawn`).
7. `session status` do cliente (750 ms), `session stop` (associado ao id da sessão ativa, 750 ms), `events read` (750 ms), `events watch` (consulta a cada 250 ms até Ctrl+C).
8. `detect`, ou **nenhum argumento** (sem instalação, sem comando, sem `/?`, sem `/spec`, sem flag de lançamento, sem flag de recuperação, sem `/list`): enumerar os processos `Omsi` e sondar o endpoint de controlo (250 ms); envelope `detect`; saída `0`.
9. `/?` ou `/help`: imprimir o texto de utilização, saída `0`. Qualquer outra invocação que tenha uma palavra de comando mas nenhuma rota despachável (por exemplo `d3d` sozinho ou `session status D:\OMSI`) imprime o texto de utilização e sai com `2`.
10. Modo proprietário. Pré-condições: `plugins\OmsiLaunch.Plugin.opl` e `plugins\OmsiLaunch.Native.x86.dll` têm de existir junto do executável (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, saída `7`). O `release-manifest.json` junto do executável, quando presente, fornece os hashes esperados dos plugins.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; envelope `recover` com `pending`, `recovered`, `diagnostics`; saída `8` apenas quando foi pedido um restauro que não foi concluído; caso contrário `0`.
12. `/list:<category>`: `DiscoverAsync`; envelope `content.list`; saída `0`. `/list:SessionProfiles` usa em vez disso `ListSessionProfilesAsync`; envelope `session-profiles.list`.
13. Construir o `LaunchSpec` (`BuildSpecAsync`), planeá-lo (`PlanSessionAsync`) e imprimir o plano. `/plan` ou `/validate`: saída `0` se `IsRunnable`, caso contrário `1`. Um plano não executável nunca inicia o OMSI (saída `1`); sob `OmsiLaunchW.exe`, um lançamento com um plano não executável mostra o seu último diagnóstico `OL_E_` numa caixa de mensagem (auditoria de documentação BUG-06). O planeamento também verifica o conjunto de plugins permanentes instalados face ao `release-manifest.json`, pelo que um plugin em falta ou alterado torna o plano não executável (`OL_E_PERMANENT_PLUGIN_*`).
14. Sondar a existência de um proprietário (`OL_E_SESSION_ALREADY_ACTIVE`, saída `7`) e depois `OwnerSession.RunAsync`.

<a id="owner-lifecycle-ownersessionrunasync"></a>
### Ciclo de vida do proprietário (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. A partir daqui, todos os caminhos de saída chegam a `CloseAsync` num bloco `finally`: exceções, Ctrl+C (`Console.CancelKeyPress`), fecho da consola / fim de sessão do Windows (`AppDomain.ProcessExit` com um orçamento de 4 s para paragem + restauro; o que ficar por fazer é recuperado pelo journal no arranque seguinte), "End session" na área de notificação, `session.stop` pelo pipe e `/observe-seconds`.
2. O ícone da área de notificação é criado, a menos que `Presentation.SuppressTrayIcon` esteja definido na especificação.
3. Esperar por `Running` durante `StartupTimeoutSeconds + 5` segundos. O estado é impresso. Se o estado não for `Running`, saída `1` (`OmsiLaunchW.exe` mostra `The OMSI session did not reach gameplay.` com o último diagnóstico `OL_E_` ou `OL_E_SESSION_START_FAILED`).
4. Os lotes de validação (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) são executados e escrevem os seus artefactos.
5. O endpoint de controlo local é iniciado.
6. `/runtime:<operation>` é executado uma vez (5 s, 15 s para `road-vehicles.spawn`); o resultado é escrito em `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` e impresso. Um comando de runtime que falhe nunca termina a sessão (em vez disso é impresso `runtime_error`).
7. Espera: com `/observe-seconds:n`, a sessão é parada ao fim de `n` segundos **ou** antes, numa paragem pela área de notificação/pipe, ou quando o OMSI termina; sem essa flag, o proprietário espera até o OMSI terminar ou até ser pedida uma paragem.
8. O estado final é impresso; saída `0` se `Completed`, `1` caso contrário.

`session.stop`, "End session" na área de notificação, Ctrl+C e `CloseAsync` pedem todos a paragem canónica: o OMSI é terminado com `TerminateProcess` (a rotina de encerramento do próprio OMSI não é executada e o `options.cfg` não é reescrito pelo OMSI) e, em seguida, todos os ficheiros pertencentes à sessão são restaurados. Ver [ciclo de vida da sessão](../concepts/session-lifecycle.md) e [transações e recuperação](../concepts/transactions-and-recovery.md).

<a id="command-words"></a>
## Palavras de comando

Todas as palavras aceites na primeira posição (`CliInput.CommandWordsAccepted`):

| Palavra | Finalidade | Modo | Notas |
|---|---|---|---|
| `capabilities` | Listar as capacidades públicas | Local, sem sessão | Envelope `capabilities`. |
| `profiles` | Listar as variantes suportadas de `Omsi.exe` | Local, sem sessão | Envelope `profiles`. |
| `detect` | Comunicar os processos `Omsi.exe` e um proprietário ativo | Local, sem sessão | É também a predefinição quando não são indicados argumentos. Estados: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, `UNKNOWN_BINARY_FOUND` por processo quando o binário não pode ser inspecionado; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Utilização e catálogo público de comandos | Local, sem sessão | `help <family>` filtra por família de capacidades (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Cliente | Exatamente uma palavra a seguir; qualquer outra coisa imprime a utilização, saída `2`. `session plan`/`session start` são nomes de rotas da API, não palavras da CLI: usar `/plan` e `/new`. |
| `events` | `events read`, `events watch` | Cliente | `read` devolve uma vez a lista limitada de eventos; `watch` imprime cada novo evento (por `Sequence`) como um envelope `events.watch` a cada 250 ms até Ctrl+C (saída `0`), `4` quando nenhum proprietário responde, `7` num erro de controlo. |
| `time` | `time get`, `time set` | Rota de cliente | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Rota de cliente | |
| `map` | `map get` | Rota de cliente | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Rota de cliente | |
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
| `d3d` | Palavra de família reservada | Nenhum | `d3d` **não tem rota hierárquica**: `d3d texture ...` é uma rota desconhecida (saída `2`) e `d3d` sozinho imprime a utilização (saída `2`). As operações D3D são acedidas com `/runtime:d3d.status`, `/runtime:d3d.texture.create` e assim por diante (ver [Operações sem rota](#operations-without-a-route)). |

<a id="hierarchical-routes"></a>
## Rotas hierárquicas

`CliInput.HierarchicalRoutes` faz corresponder uma rota em minúsculas a um id de operação de runtime. Todas as rotas requerem uma sessão `Running` e são executadas através da mailbox de runtime (`ExecuteRuntimeAsync`). As escritas de runtime alteram apenas o estado em memória do OMSI: nunca tocam em ficheiros, não fazem parte da transação de configuração e **não** são revertidas na paragem (o OMSI é terminado). A estabilidade segue o `PublicCapabilityRegistry` e a [matriz de validação](../status/runtime-validation-status.md); os detalhes e os campos de resultado estão em [controlo de runtime](runtime-control.md).

| Rota | Operação de runtime | Tipo | Requer Running | Altera o OMSI | Participação no restauro | Estabilidade | Notas |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Campos de relógio e calendário. |
| `time set` | `time.set` | Write | Sim | Sim (relógio em memória) | Nenhuma, não revertida | EXPERIMENTAL | Por exemplo `--minute=<0..59>`; escrita, releitura e restauro validados a 2026-09-20. |
| `weather get` | `weather.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Sim | Sim (meteorologia em memória) | Nenhuma, não revertida | EXPERIMENTAL | Por exemplo `--wind_speed=5 --cloud_type="Cumulus 1"`. Meteorologia manual: as atualizações METAR em direto param. Validação em runtime pendente. |
| `weather metar set` | `weather.metar.set` | Write | Sim | Sim (meteorologia em memória; o OMSI guarda o ICAO em `options.cfg`, que a sessão restaura) | Nenhuma, não revertida | EXPERIMENTAL | `--icao=<ICAO>`. Meteorologia METAR em direto, tal como a define a caixa de diálogo de meteorologia do OMSI. Validação em runtime pendente. |
| `weather actual get` | `weather.actual.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Estado do controlador de meteorologia real/ICAO. |
| `map get` | `map.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Nome do mapa, ficheiro, descrição, número de tiles, intervalo de anos e lado de circulação; revalidado em runtime no slot de mapa corrigido. |
| `camera get` | `camera.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Sim | Sim (escalares da câmara, p. ex. `--field_of_view=`) | Nenhuma, não revertida | EXPERIMENTAL | Escrita/releitura do FOV validada. |
| `camera lock` | `camera.lock` | Action | Sim | Sim (política de âmbito da sessão) | Nenhuma | EXPERIMENTAL | Requer `--family=<0..3>` (motorista=0, passageiro=1, exterior=2, mapa=3), `--preset=<n>` opcional (família 0 ou 1). Precisa de um veículo do jogador (por exemplo, uma situação guardada). Validado em runtime no fecho de runtime (`CAM01`); a string `RuntimeValidation` do registo continua a indicar `STATICALLY_VALIDATED` (ver [capacidades](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Sim | Sim | Nenhuma | EXPERIMENTAL | Liberta a política definida por `camera lock` (`CAM01`). |
| `vehicles list` | `road-vehicles.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Devolve handles `rv-NNNNNN` de âmbito da sessão. |
| `vehicles get` | `road-vehicle.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Requer `--handle=`. Handle obsoleto: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Contagens e estado do jogador, sem handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Sim | Sim (acrescenta um RoadVehicle) | Nenhuma, não removido | EXPERIMENTAL | Requer `--model=Vehicles\...\*.bus`. Timeout de 30 s no cliente, 15 s no proprietário. Não atribui o veículo do jogador. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Sim | Sim | Nenhuma | EXPERIMENTAL | `PlaceRandomBus` perfilado. |
| `player get` | `player-vehicle.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Nulo semântico quando não há veículo do jogador. |
| `humans list` | `humans.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Devolve handles `hb-NNNNNN`. |
| `humans get` | `human.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Requer `--handle=`. |
| `humans summary` | `humans.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Apenas contagens. |
| `timetable get` | `timetable.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Estado do gestor de horários. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Faz parte da capacidade `timetable.read`; evidência de leitura em lote de 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable lines list` | `timetable.lines.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable tours list` | `timetable.tours.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable logs list` | `timetable.logs.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Como acima. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Números de veículo que o OMSI mantém fora da utilização livre pela IA (reservados por serviços (tours) de horário ou por listas fixas de IA). Paginada (`cursor`, `limit`, no máximo 256 por página). |
| `map tiles list` | `map.tiles.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Os tiles do mapa: posição na grelha, índice próprio, ficheiro e estado de carregamento, contagens de objetos, caminhos e splines. Paginada (`cursor`, `limit`, no máximo 256 por página). |
| `drivers list` | `drivers.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Registos de motoristas. |
| `tickets get` | `tickets.read` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Registos de pacotes de bilhetes. |
| `hof get` | `vehicle.hofs.read` | Read | Sim | Não | Nenhuma | STABLE_BETA | Requer `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Requer `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Sim | Não | Nenhuma | STABLE_BETA | Requer `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Sim | Não | Nenhuma | STABLE_BETA | Requer `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Sim | Não | Nenhuma | STABLE_BETA | Requer `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Requer `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Requer `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Sim | Sim (variável de script) | Nenhuma, não revertida | EXPERIMENTAL | Requer `--handle=`, `--name=`, `--value=` (número finito). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Requer `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Sim | Não | Nenhuma | EXPERIMENTAL | Requer `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Sim | Sim (estado de script em memória) | Nenhuma, não revertida | EXPERIMENTAL | Requer `--handle=`, `--name=`, `--value=`. O próprio `SetStringVariable` do OMSI. Validação em runtime pendente. |
| `scripts trigger` | `vehicle.trigger` | Action | Sim | Sim (executa código de script do veículo) | Nenhuma, não revertida | EXPERIMENTAL | Requer `--handle=`, `--name=`, `--value=` (`1` premir, `0` largar, `2` arrastar). Validação em runtime pendente. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Sim | Sim (reproduz sons do veículo) | Nenhuma, não revertida | EXPERIMENTAL | Requer `--handle=`, `--trigger=`; `--file=` opcional. Validação em runtime pendente. |

<a id="operations-without-a-route"></a>
### Operações sem rota

Estes ids de operação públicos (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) não têm rota hierárquica e são invocados com `/runtime:<operation>` mais `--key=value` ou `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` obrigatórios; `levels` opcional), `d3d.texture.describe` (`handle`; `level` opcional), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` obrigatórios; `level`, `x`, `y` opcionais), `d3d.texture.release` (`handle`). As operações D3D são EXPERIMENTAL; o ciclo de vida das texturas e a invalidação por reposição do dispositivo (reset) estão validados em runtime (fecho de runtime `H02`, `D01`; ver [capacidades](capabilities.md)). `timetable.track-entries.list` e `timetable.tour-entries.list` são listas limitadas: um resultado que não caiba no slot de runtime é encurtado (`truncated=true`). `internal.road-vehicles.make-basic` é INTERNAL e é rejeitado com `OL_E_RUNTIME_OPERATION_UNKNOWN` tanto pela CLI como pela API.

## Flags

Todas as flags de `CliInput.KnownFlags`. A "fase" é *de lançamento* (determina o `LaunchSpec`/plano de uma nova sessão), *de runtime* (atua sobre uma sessão em execução) ou *de controlo* (altera o comportamento da própria CLI). As flags interpretadas apenas por compatibilidade (`CliInput.AcceptedNoEffectFlags`) estão assinaladas na respetiva linha.

<a id="control-and-output"></a>
### Controlo e saída

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/?` | `/?` | desligada | controlo | STABLE_BETA | Imprimir o texto de utilização, saída `0`. |
| `/help` | `/help` | desligada | controlo | STABLE_BETA | Igual a `/?`. (A palavra simples `help` devolve, em vez disso, o catálogo estruturado.) |
| `/version` | `/version` | desligada | controlo | STABLE_BETA | Envelope `version`, saída `0`. Avaliada antes de qualquer outro comando, exceto `/silent`. |
| `/json` | `/json` ou `--json` | desligada | controlo | STABLE_BETA | Emitir envelopes JSON; também força a saída de consola, mesmo sob `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | desligada | controlo | STABLE_BETA | Suprime a saída de sucesso legível por humanos. A saída JSON (`/json`) e os erros continuam a ser escritos; o código de saída não muda. |
| `/silent` | `/silent` (também `--silent`) | desligada | controlo | EXPERIMENTAL | Delegar toda a linha de comandos em `OmsiLaunchW.exe` e devolver `0` assim que o processo host tiver arrancado. O resultado da sessão é comunicado pelo `OmsiLaunchW.exe` (caixas de mensagem, ícone da área de notificação), por `.omsilaunch\diagnostics` e pelo endpoint de controlo local. A delegação e as caixas de diálogo de falha estão validadas em runtime (fecho de runtime `T04`); ver [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | controlo | REMOVED | Removida na 0.1.0-beta4: rejeitada com `OL_E_INVALID_ARGUMENT` (código de saída `2`). O proprietário da sessão serve sempre o endpoint de controlo local. |
| `/verbose` | `/verbose` | desligada | lançamento | PARTIAL | `DiagnosticsSpec.Verbose`. Os valores são transportados na especificação; o seu efeito limita-se ao trace do host em `.omsilaunch\diagnostics`. |
| `/log` | `/log` | ligada (`DiagnosticsSpec.Log` tem como predefinição `true`) | lançamento | PARTIAL | `DiagnosticsSpec.Log`. Na prática, sempre ligada. |
| `/logall` | `/logall` | desligada | lançamento | PARTIAL | Define em conjunto `Verbose`, `ProcessTrace`, `PluginTrace` e `NativeTrace`. |
| `/omsi-logall` | `/omsi-logall` | desligada | lançamento | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | desligada | lançamento | PARTIAL | Alias de `/trace-process`. |
| `/trace-process` | `/trace-process` | desligada | lançamento | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | desligada | lançamento | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: o plugin comunica cada operação de runtime (`plugin.trace.operation`: operação, resultado, código, duração) ao trace do host da sessão. |
| `/trace-native` | `/trace-native` | desligada | lançamento | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: a camada nativa comunica cada instalação e remoção de hook (`native.trace`) ao trace do host da sessão. |

<a id="planning-validation-and-harnesses"></a>
### Planeamento, validação e harnesses

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/plan` | `/plan` | desligada | lançamento | STABLE_BETA | Construir e imprimir o `SessionPlan`, sem iniciar o OMSI. Saída `0` quando `IsRunnable`, `1` caso contrário. Requer uma seleção de lançamento (`/new`, `/saved`, `/spec` ou um argumento de instalação); `/plan` sozinho, sem mais nada, executa `detect`. |
| `/validate` | `/validate` | desligada | lançamento | STABLE_BETA | Idêntica a `/plan` nesta build. |
| `/runtime-batch` | `/runtime-batch` | desligada | runtime (proprietário) | INTERNAL | Harness de validação: após `Running`, executa o conjunto de operações de leitura e escreve `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | desligada | runtime (proprietário) | INTERNAL | Harness de validação: leituras mais `time.set`, `camera.set` e `vehicle.variable.set` com restauro; escreve `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | desligada | runtime (proprietário) | INTERNAL | Harness de validação para o ciclo de vida das texturas D3D; escreve `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | nenhuma | runtime | STABLE_BETA (despacho) | Selecionar uma operação de runtime pública pelo id. Modo cliente (sem argumento de instalação): reencaminhada para o proprietário. Modo proprietário: executada uma vez após `Running`. Ids desconhecidos: `OL_E_RUNTIME_OPERATION_UNKNOWN`, saída `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (repetível) | nenhuma | runtime | STABLE_BETA (despacho) | Argumento de runtime; equivalente a `--key=value`. Sem `=`: `/runtime-arg requires key=value`, saída `2`. |

<a id="world-selection"></a>
### Seleção do mundo

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` é o modo predefinido, mas só é pedido um lançamento quando está presente uma de `/new`, `/saved`, `/last`, `/spec` | lançamento | STABLE_BETA | NEW_MAP. Requer `/map` e `/entrypoint-index` (um plano sem um índice de ponto de entrada apresentado comunica `OL_E_ENTRYPOINT_REQUIRED`; sem `/map` nenhum mapa é resolvido). `/new` nunca seleciona um mapa silenciosamente. |
| `/saved` | `/saved:<file.osn>` | nenhuma | lançamento | STABLE_BETA | SAVED_SITUATION. O mapa e a posição vêm do `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` são rejeitadas com `/saved` (saída `2`). Situação em falta: `OL_E_SITUATION_NOT_FOUND`; o respetivo mapa em falta: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | nenhuma | lançamento | EXPERIMENTAL | LAST_MAP_STATE: carrega o `laststn.osn` do próprio mapa (escrito pelo OMSI quando o mapa foi fechado) através do ramo de último estado do formulário Start. Requer `/map`; um mapa sem `laststn.osn` é `OL_E_SITUATION_NOT_FOUND`. Nunca uma heurística do `.osn` mais recente. Validação em runtime pendente. |
| `/map` | `/map:<identity>` (por exemplo `maps\Grundorf\global.cfg`) | nenhuma | lançamento | STABLE_BETA | Identidade do mapa para `/new`, ou o âmbito para `/list:Entrypoints`. Desconhecida: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | nenhuma | lançamento | EXPERIMENTAL | Ponto de entrada pela sua etiqueta exata (com distinção de maiúsculas/minúsculas) ou por uma identidade de `/list:entrypoints`. O OMSI apresenta apenas o primeiro registo de cada etiqueta; um registo posterior que a partilhe é recusado (`OL_E_ENTRYPOINT_NOT_PRESENTED`), tal como um desconhecido (`OL_E_ENTRYPOINT_NOT_FOUND`). Validação em runtime pendente. Mutuamente exclusiva com `/entrypoint-index` (a identidade prevalece e limpa o índice). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | nenhuma | lançamento | STABLE_BETA | Índice do ponto de entrada na lista apresentada (a partir de 1, tal como o OMSI o apresenta). Obrigatório para um plano NEW_MAP executável. |

<a id="date-time-and-weather"></a>
### Data, hora e meteorologia

Estas flags são aceites e transportadas para o `LaunchSpec`. A data, a hora e o ano de início aplicam-se apenas a sessões NEW_MAP: com `/saved` ou `/last` o planeador regista-os como `UNAVAILABLE` **e acrescenta `OL_E_CAPABILITY_UNAVAILABLE`, pelo que o plano é NÃO EXECUTÁVEL (saída `1`)**, porque uma situação guardada mantém o seu próprio relógio. A meteorologia aplica-se a todos os modos de mundo. Todas são `EXPERIMENTAL`: o caminho de arranque nativo está fechado estaticamente e a validação em runtime está pendente. Um ficheiro `/spec` ou um perfil de sessão que as defina tem o mesmo efeito.

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` ou `/date:system` | não definida | lançamento | EXPERIMENTAL | `DateSpec` explícita/do sistema: a data de início de uma sessão NEW_MAP (ver [launchspec](launchspec.md#datespec-timespec-yearspec)). Valor impossível de interpretar: `OL_E_INVALID_ARGUMENT`, saída `2`; uma data impossível: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` ou `/time:system` | não definida | lançamento | EXPERIMENTAL | `TimeSpec` explícita/do sistema: a hora de início de uma sessão NEW_MAP. |
| `/year` | `/year:<n>` ou `/year:system` | não definida | lançamento | EXPERIMENTAL | `YearSpec`: o ano de início de uma sessão NEW_MAP (1900..2200). |
| `/weather` | `/weather:<preset>` | não definida | lançamento | EXPERIMENTAL | `WeatherMode.Preset`: um ficheiro da pasta `weather` da instalação, por nome (`Regen`) ou caminho (`weather\Regen.owt`); um ficheiro em falta é `OL_E_WEATHER_INVALID` na altura do plano. Carregada através do próprio `loadWeather` do OMSI logo após o arranque do mundo; o METAR em direto permanece desligado. Validação em runtime pendente. |
| `/weather-icao` | `/weather-icao:<code>` | não definida | lançamento | EXPERIMENTAL | `WeatherMode.Icao`: meteorologia METAR em direto para uma estação ICAO de quatro carateres (caso contrário `OL_E_WEATHER_INVALID`). O OMSI guarda a estação em `options.cfg`, que a sessão restaura. Validação em runtime pendente. |
| `/weather-real` | `/weather-real` | não definida | lançamento | EXPERIMENTAL | `WeatherMode.RealCurrent`: meteorologia METAR em direto para a estação que o OMSI já tem configurada; sem uma, o arranque falha com `OL_E_WEATHER_APPLY_FAILED`. Prevalece a última de `/weather`, `/weather-icao`, `/weather-real`. Qualquer meteorologia de arranque substitui a meteorologia de uma situação guardada. |

<a id="player-vehicle"></a>
### Veículo do jogador

Aceites e resolvidas face à instalação, mas não aplicadas pelo runtime: cada campo definido fica `UNAVAILABLE` e acrescenta `OL_E_CAPABILITY_UNAVAILABLE` (plano NÃO EXECUTÁVEL, saída `1`).

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | não definida | lançamento | UNAVAILABLE | Resolvida primeiro (`OL_E_VEHICLE_NOT_FOUND` se desconhecida). |
| `/repaint` | `/repaint:<id>` | não definida | lançamento | UNAVAILABLE | Resolvida apenas em conjunto com `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | não definida | lançamento | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` se desconhecida. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | nenhuma | lançamento | STABLE_BETA | Uma operação de [gestão de HOF](hof-management.md) da sessão: `HofSourceKind` e `Source`. Requer pelo menos um `/hof-target`. Substitui o `HofManagement` de um `/spec`; com uma predefinição de perfil de sessão que tenha `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Malformada: saída `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (repetível) | nenhuma | lançamento | STABLE_BETA | Um veículo de destino abaixo de `Vehicles\`. Sem `/hof-source`: saída `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (repetível) | todos os ficheiros | lançamento | STABLE_BETA | Restringe a origem a estes nomes de ficheiro. Sem `/hof-source`: saída `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | lançamento | STABLE_BETA | `HofApplyMode`. Sem `/hof-source`: saída `2`. |
| `/module` | `/module:<id>` (repetível) | nenhuma | lançamento | EXPERIMENTAL | Pede um [módulo de runtime](../../../reference/runtime-modules.md) para a sessão (`LaunchSpec.Modules.Enable`, acrescentado aos módulos de um ficheiro `/spec`). Um módulo pedido que não possa ser carregado torna o plano não executável com o seu código `OL_E_MODULE_*` (saída `1`). As suas operações são alcançadas com `/runtime:module.<id>.<operation>` e `/runtime-arg`. Com uma predefinição de perfil de sessão que defina `enabled: false` para ele: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (repetível) | nenhuma | lançamento | EXPERIMENTAL | Mantém um módulo fora da sessão, incluindo um módulo carregado por predefinição, pelo seu mapa ou pelo ficheiro de definições (`LaunchSpec.Modules.Disable`). Com uma predefinição que defina `enabled: true` para ele: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Sem valor: saída `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (repetível) | nenhuma | lançamento | EXPERIMENTAL | Define uma definição de módulo (`LaunchSpec.Modules.Settings`); prevalece sobre o ficheiro `/spec`, o ficheiro de definições e a predefinição. Uma chave que a predefinição selecionada também defina: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Que não seja `<id>.<key>=<value>`: saída `2`. Valor inválido ou chave não declarada: diagnóstico do plano `OL_E_MODULE_SETTING_INVALID` (saída `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` isolada significa `true` para uma definição booleana | nenhuma | lançamento | EXPERIMENTAL | Uma flag que um módulo instalado declara para uma das suas definições (`flag` no seu manifesto; listada por `/list:RuntimeModules`). É resolvida quando uma sessão é planeada e equivale a `/module-setting:<id>.<key>=<value>`. Uma flag que nenhum módulo instalado declare, ou uma usada fora de um lançamento: `Unknown argument`, saída `2`. Uma flag declarada por dois módulos: `OL_E_MODULE_SETTING_INVALID`, saída `2`. Uma flag de módulo nunca substitui uma flag incorporada. |
| `/fleet` | `/fleet:<n>` | não definida | lançamento | UNAVAILABLE | Número de frota. |
| `/registration` | `/registration:<text>` | não definida | lançamento | UNAVAILABLE | Matrícula. |
| `/no-vehicle` | `/no-vehicle` | desligada | lançamento | STABLE_BETA | Remove qualquer veículo do jogador da base (`/spec` ou perfil). Inofensiva. |

<a id="configuration-overlays"></a>
### Overlays de configuração

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (repetível; chaves sem distinção de maiúsculas/minúsculas) | nenhuma | lançamento | STABLE_BETA | Overlay semântico de `options.cfg` a partir de `ConfigurationCatalog` (por exemplo `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Chave desconhecida: `OL_E_UNKNOWN_SETTING` (saída `2`); chave só de leitura (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (saída `2`); valor fora do intervalo ou malformado: `OL_E_INVALID_SETTING_VALUE` quando o overlay é construído. O overlay é uma mutação da sessão: é capturado em snapshot, aplicado antes de o OMSI arrancar e restaurado byte a byte na paragem (RV-005 `RUNTIME_VALIDATED`). Conflitos com uma chave pertencente a uma predefinição (preset) de um perfil selecionado: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

<a id="splash-presentation"></a>
### Apresentação do splash

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (sem distinção de maiúsculas/minúsculas) | `Managed` | lançamento | STABLE_BETA | `Managed`: os BMP de 640x480 e 24 bits incluídos no pacote são copiados uma vez para `<root>\.omsilaunch\assets\splash`, e `GUI\NewSplashscreen_ENG.bmp` mais `GUI\NewSplashscreen_<lang>.bmp` são sobrepostos de forma transacional e restaurados exatamente (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (aliases): ficheiros do OMSI intactos. Valor em falta: `/splash requires Unset, Native, or Managed`, saída `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (também `pt-BR`, `de`, `fr`, `en`; qualquer outro valor recorre a `ENG`) | `[language]` de `options.cfg`, senão `ENG` | lançamento | STABLE_BETA | Seleciona o ficheiro de destino localizado. |
| `/splash-assets` | `/splash-assets:<directory>` (os caminhos relativos são resolvidos abaixo da raiz da instalação) | `<root>\.omsilaunch\assets\splash`, senão o conjunto incluído no pacote | lançamento | STABLE_BETA | Diretório de recursos personalizado; tem de conter `ENG.bmp` e, para uma língua que não o inglês, `<lang>.bmp`. Erros: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (apresentados como `OL_E_SESSION_PRESENTATION_INVALID` no plano; não executável). |

### Internet Textures

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | lançamento | EXPERIMENTAL | `Native`: intacto. `Disabled`: o mecanismo de transferência perfilado dentro do processo é suprimido. `Override`: o perfil `.itx` indicado é sobreposto como `Texture\standard.itx`; todos os destinos HTTP(S) nele listados, mais `Texture\standard.ipr`, tornam-se eliminações da sessão (removidos durante a sessão, restaurados na paragem). Valor em falta: saída `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | nenhuma | lançamento | EXPERIMENTAL | Obrigatória com `Override` (`OL_E_ITX_PROFILE_REQUIRED`, saída `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (tem de conter pares de linhas URL/destino com URLs `http`/`https`), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (os destinos têm de ser resolvidos dentro de `Texture\`, sem caminhos absolutos, `..` ou pontos de reanálise). |

<a id="session-profiles"></a>
### Perfis de sessão

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | nenhuma | lançamento | STABLE_BETA (compilação; `OmsiLaunch.ProfileTests` offline) | Carrega `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (ver [perfis de sessão](session-profiles.md)). Requer `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, saída `2`). O bloco `new:` só se aplica com `/new`; `compatibility.maps` é imposto para `/new` e `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). As flags explícitas que colidam com um campo pertencente ao perfil são rejeitadas com `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): mapa/ponto de entrada/data/hora/ano/meteorologia quando o bloco `new:` os define, chaves `/set` pertencentes à predefinição, flags de splash quando a predefinição tem `presentation`, flags de Internet Textures quando tem `internet-textures`, timeouts quando tem `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | nenhuma | lançamento | STABLE_BETA | Seleciona a predefinição por `index`. Fora do intervalo: saída `2`. |

<a id="launchspec-file"></a>
### Ficheiro LaunchSpec

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | nenhuma | lançamento | STABLE_BETA (carregador testado offline; semântica da sessão idêntica à das flags) | Carrega um ficheiro JSON `LaunchSpec` como base (ver [LaunchSpec](launchspec.md)) e marca um lançamento como pedido. Regras (`LaunchSpecJson`): o ficheiro tem de existir (`OL_E_SPEC_NOT_FOUND`, saída `6`); no máximo 1 MiB (`OL_E_SPEC_TOO_LARGE`, saída `2`); a raiz tem de ser um objeto (`OL_E_SPEC_INVALID`); nomes de propriedades sem distinção de maiúsculas/minúsculas; comentários `//` e vírgulas finais permitidos; profundidade máxima de 32; qualquer propriedade desconhecida é rejeitada com o respetivo caminho JSON (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, saída `2`). |

**Precedência** (`CliInput.BuildSpecAsync`): predefinições → ficheiro `/spec` → `/predefined-profile` (substitui `Installation` e `World` e depois aplica o perfil) → flags explícitas. Um argumento de instalação explícito prevalece sobre `RootPath` na especificação. `/no-vehicle` remove o veículo do jogador da especificação; `/vehicle` e as flags afins fundem-se com ele campo a campo. As chaves `/set` fundem-se em `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` só sobrepõem valores quando indicadas. `/startup-timeout` e `/shutdown-timeout` só sobrepõem valores quando indicadas; `Presentation.SuppressTrayIcon` provém apenas da especificação (não há flag). As flags de diagnóstico são combinadas por OU com o `Diagnostics` da especificação.

<a id="content-discovery"></a>
### Descoberta de conteúdos

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; as categorias são os valores de `ContentQueryKind` `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (sem distinção de maiúsculas/minúsculas) | nenhuma | local, sem sessão | STABLE_BETA | `DiscoverAsync` sobre a instalação; envelope `content.list` com entradas `Identity`, `Kind`, `DisplayName`; saída `0`. `SessionProfiles` responde com `ListSessionProfilesAsync` (envelope `session-profiles.list`) e `RuntimeModules` com `ListRuntimeModulesAsync` (envelope `runtime-modules.list`: o id, a versão, a ativação, as operações e a validade de cada módulo e as suas definições com tipo, predefinição e flag). Categoria desconhecida: `Unknown discovery category`, saída `2`. Os pontos de reanálise (junctions/ligações simbólicas) são ignorados; os ficheiros do OMSI são lidos como Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | nenhuma | local | STABLE_BETA | Âmbito reencaminhado para todas as categorias exceto `Entrypoints`, que usa `/map` como âmbito. |

<a id="timeouts-and-observation"></a>
### Timeouts e observação

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` segundos | valor da especificação/perfil, senão `180` | lançamento | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. O proprietário espera por `Running` durante este valor mais 5 s; `OL_E_STARTUP_TIMEOUT` termina a sessão com saída `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` segundos | valor da especificação/perfil, senão `30` | lançamento | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: com `/cooperative-shutdown`, o tempo que o supervisor espera que o OMSI termine antes de o terminar à força; sem ela, o OMSI é terminado de imediato e o valor não tem efeito. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | desligada | lançamento | EXPERIMENTAL | `Behavior.CooperativeShutdown`: terminar uma sessão em execução pede ao OMSI que saia através da sua própria rotina de saída (a que corre depois de o utilizador confirmar a saída: guarda `options.cfg` e o `laststn.osn` do mapa e depois fecha) e espera `ShutdownTimeoutSeconds` antes de o terminar à força. Opt-in até estar validada em runtime; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` quando o OMSI teve de ser terminado. Os ficheiros da sessão são restaurados após a saída em qualquer dos casos. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | nenhuma (executar até o OMSI terminar ou até ser pedida uma paragem) | runtime (proprietário) | STABLE_BETA | Limite superior da fase de execução: após `n` segundos em `Running`, é pedida a paragem canónica. Uma paragem pela área de notificação ou pelo pipe, ou o fim do OMSI, termina-a mais cedo. `0` para imediatamente após `Running`. |

<a id="recovery"></a>
### Recuperação

| Flag | Sintaxe e valores | Predefinição | Fase | Estabilidade | Comportamento |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | desligada | local | STABLE_BETA | Indicar se `<root>\.omsilaunch\journal.json` está pendente (`pending`); nunca restaura; saída `0`. Obtém o lease da instalação: `OL_E_INSTALLATION_BUSY` (saída `7`) enquanto um proprietário o detiver. |
| `/recover` | `/recover` | desligada | local | STABLE_BETA | Restaurar um journal pendente (as cópias de segurança são primeiro verificadas face ao SHA-256 do snapshot; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` comunicados em `diagnostics`). Saída `0` quando nada estava pendente ou o restauro foi concluído; `8` quando havia um journal pendente e este se mantém. Recusado com `OL_E_INSTALLATION_BUSY` enquanto o processo OMSI registado no journal (PID, hora de criação, caminho do executável) estiver ativo ou, para um journal posterior a `HandoffCreated` sem PID, enquanto estiver ativo qualquer `Omsi.exe` dessa raiz. Cada arranque de sessão executa automaticamente a mesma recuperação antes de ler a instalação. |
| `/clear-module-data` | `/clear-module-data:<module id>` | nenhuma | local, sem sessão | EXPERIMENTAL | Elimina o diretório de dados do módulo `<root>\.omsilaunch\module-data\<id>` e mais nada; envelope `module-data.clear` com `module` e `cleared` (`false` quando não havia nada a eliminar); saída `0`. Obtém o lease da instalação: `OL_E_INSTALLATION_BUSY` (saída `7`) enquanto uma sessão for proprietária da instalação. Uma pasta de dados de módulo alcançada através de uma ligação é recusada (`OL_E_MODULE_INTEGRITY`); uma ligação no seu interior é removida como ligação. |

<a id="output-formats"></a>
## Formatos de saída

- **Envelope de sucesso** (`CliInput.WriteEnvelope`, com `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, indentado. As respostas reencaminhadas de `session status` e `events read` acrescentam um membro `metadata` quando foram omitidos eventos mais antigos para caber no frame de controlo (`events_dropped_count`, ver [controlo local](local-control.md)). Sem `--json` só é impresso `<object>` como JSON indentado, seguido de `Note: <n> older events were omitted to fit the control frame.` quando foram descartados eventos.
- **Envelope de erro** (`CliInput.WriteError`, com `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Sem `--json`: `OL_E_<CODE>: message` numa linha. Categorias: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Sob `OmsiLaunchW.exe`, o mesmo código e mensagem são apresentados numa caixa de mensagem.
- **Plano e estado** (`CliInput.Write`): os registos `SessionPlan`, `SessionStatus` e `RuntimeCommandResult` são impressos como JSON indentado **sem** envelope. Sem `--json`, um plano é resumido como `Plan: READY profile=Omsi23004_692EBFBF` ou `Plan: NOT RUNNABLE profile=...`; os outros registos continuam a ser impressos como JSON. Os valores de enumeração são serializados como inteiros (`SessionState.Running` é `14`, `Completed` é `18`, `Failed` é `19`).
- Nomes de comando usados nos envelopes: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile` e o id da operação de runtime para comandos de runtime reencaminhados.
- Sob `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) nada é escrito na consola, a menos que seja indicado `--json`.

<a id="errors-per-command"></a>
## Erros por comando

| Comando | Códigos de erro típicos | Saída |
|---|---|---|
| Qualquer falha de interpretação | `OL_E_INVALID_ARGUMENT`, códigos de perfil de sessão (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Rota de cliente, `/runtime` (cliente) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` devolvidos pelo proprietário, p. ex. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Verificação prévia do proprietário | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); pendente mas não recuperado (`8`) | `7`, `8` |
| `/list` | categoria desconhecida (`2`); `OL_E_INSTALLATION_NOT_FOUND`/diretórios em falta (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, lançamento | diagnósticos do plano: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (conjunto de plugins instalados), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informativo) | `1` |
| Arranque da sessão | `OL_E_PLAN_NOT_RUNNABLE` (novo planeamento no arranque, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normalmente comunicados pelo planeamento como diagnóstico do plano, saída `1`; `7` apenas se os ficheiros dos plugins mudarem entre o planeamento e o arranque), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (sessão `Failed`, `1`) | `1`, `7` |
| Exceção não tratada em qualquer ponto | classificada por `CliProgram.Classify` (ver [códigos de saída](exit-codes.md)) | `2`..`10` |

<a id="environment"></a>
## Ambiente

| Variável | Definida por | Efeito |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: saída de consola suprimida, falhas em caixas de mensagem, `/silent` não é novamente delegado. |

<a id="see-also"></a>
## Ver também

[Exemplos da CLI](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [códigos de saída](exit-codes.md) · [erros](errors.md) · [controlo local](local-control.md) · [área de notificação do Windows](windows-tray.md) · [controlo de runtime](runtime-control.md) · [capacidades](capabilities.md) · [LaunchSpec](launchspec.md) · [perfis de sessão](session-profiles.md) · [empacotamento](packaging.md) · [compatibilidade](compatibility.md) · [limitações conhecidas](known-limitations.md) · [API pública](public-api.md)
