# Capacidades

<!-- l10n: source=reference/capabilities.md source-sha256=e6d9d063c2365c007efc79c0057a4843d1f91b88ba68448cc8b8d7579308de23 -->
> Tradução da [página original em inglês](../../../reference/capabilities.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página documenta o que o OmsiLaunch consegue fazer. Sua fonte é o catálogo canônico de capacidades, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): toda capacidade, toda operação pública de runtime com seus argumentos, rotas da CLI e capacidade nativa, e um estado de evidência por capacidade são declarados ali uma única vez. O conjunto de operações de runtime, a validação de argumentos, as rotas de comando da CLI, o gate nativo por operação do plugin, `GetCapabilitiesAsync` e a [tabela do catálogo](#capability-catalog) abaixo são derivados dele. Para cada operação de runtime, a página informa os argumentos, as chaves de resultado e os erros. Como invocar uma operação (rotas da CLI, envelope da API, timeouts, handles) está em [controle de runtime](runtime-control.md); a evidência por sessão está em [status de validação em runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificação e estabilidade

`PublicCapabilityClassification` tem quatro valores. Eles correspondem ao vocabulário de estabilidade da seguinte forma, com rebaixamentos onde a evidência está incompleta:

| Classificação | Significado | Estabilidade |
| --- | --- | --- |
| `PublicStableBeta` | Pública, no contrato Beta, validada em runtime | `STABLE_BETA` (rebaixada para `PARTIAL` onde indicado) |
| `PublicExperimental` | Pública, pode mudar, validada em runtime ou validada estaticamente | `EXPERIMENTAL` (rebaixada para `PARTIAL` onde indicado) |
| `InternalOnly` | Primitiva de pesquisa; não acessível pela API pública nem pela CLI | `INTERNAL` |
| `Unsupported` | Reconhecida para diagnóstico, rejeitada ou ausente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` e `Event`. Toda capacidade de runtime exige uma sessão `Running` (`RequiresSession` no registro) e sua capacidade nativa resolvida e verificada no build em execução; uma operação cuja capacidade nativa está indisponível é rejeitada com `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` marca as capacidades que dependem de destinos nativos (veja [compatibilidade](compatibility.md)).

Os resultados de runtime são dicionários de strings. Chaves que começam com `internal_` ou terminam com `_address`, `_pointer` ou `_vmt` são removidas na fronteira da API (`ScrubInternalValues`) e não são listadas aqui. As chaves de resultado nas tabelas de operações abaixo são os conjuntos exatos de chaves retornados pelo produto: elas foram capturadas executando cada operação pública em uma sessão real (captura de documentação do fechamento de runtime, sessão `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; as linhas de listas são escritas como `<row>.<n>.<field>`).

Como uma falha de operação é informada (`CurrentRuntimeControl.Execute`):

| Falha | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rejeição pelo registro (operação desconhecida ou `internal.*`, argumento obrigatório ausente) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | nenhum |
| Uma rejeição deliberada do produto (uma chave de clima derivada em `weather.set`) | o código específico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (frase) |
| Uma falha de D3D | o código `OL_E_D3D_*` específico | `detail`, `native_status` |
| Uma falha tipada do lado do plugin (handle obsoleto, nome desconhecido, valor fora do intervalo, nenhum veículo do jogador, ...) | o código específico, por exemplo `OL_E_RUNTIME_OBJECT_HANDLE_STALE` ou `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (a mensagem), `exception` (nome do tipo .NET) |
| Uma operação cuja capacidade nativa não está resolvida no build em execução | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (a capacidade nativa) |
| Uma exceção inesperada do lado do plugin, sem código | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Um resultado que não cabe no mailbox de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (listas paginadas são encurtadas em vez disso e continuam com `next_cursor`, veja [paginação](#pagination)) | nenhum |

A coluna `Errors` de cada tabela nomeia os códigos específicos; eles chegam em `ErrorCode` pelo plugin, pela ponte, pelo plano de controle, pela API e pela CLI sem alteração.

<a id="capability-catalog"></a>
## Catálogo de capacidades

Uma linha por entrada do catálogo, gerada a partir de `PublicCapabilityRegistry.All` (o gate de documentação falha quando esta tabela está desatualizada). `Session` é `Running` para as capacidades que exigem uma sessão em execução. `Operations` são os ids concretos das operações de runtime (linhas de contrato em [operações públicas de runtime](#public-runtime-operations)). `Native capability` nomeia as entradas do catálogo de destinos nativos (veja [compatibilidade](compatibility.md)) que precisam estar resolvidas e verificadas no build em execução. `Evidence` usa um único vocabulário:

| Evidência | Significado |
| --- | --- |
| RUNTIME_VALIDATED | Observada em uma sessão real do OMSI; os ids das sessões estão em [status de validação em runtime](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implementada e verificada offline; o efeito em runtime não foi observado. |
| PARTIAL | Funciona para parte de seu contrato; a nota nomeia a parte em aberto. |
| RUNTIME_VALIDATION_REQUIRED | Implementada, aguardando sua validação em runtime. |
| UNAVAILABLE | Reconhecida para diagnóstico e rejeitada, ou não implementada; solicitá-la no início torna o plano não apto para execução. |

Uma operação que altera algo nomeia sua política de mutação depois do seu id (`PublicRuntimeOperation.MutationPolicy`); nenhuma operação de runtime é registrada no journal:

| Política de mutação | Significado |
| --- | --- |
| `none` | Somente leitura (não exibida). |
| `omsi-state` | Altera o estado em memória do OMSI; nada o desfaz; ele termina com o processo do OMSI. |
| `session-resource` | Cria ou altera um recurso pertencente à sessão, liberado explicitamente ou quando a sessão termina. |
| `session-policy` | Instala uma política que o plugin reaplica até que seja removida ou a sessão termine. |

<!-- generated:capability-catalog:begin -->
| Capacidade | Classificação | Tipo | Sessão | Operações | Capacidade nativa | Evidência | CLI | Nota |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | não |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | não |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | não |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: os eventos do plugin trafegam por um anel numerado; um leitor que fica para trás recebe telemetry.lost com a contagem. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | não |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | não |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | não |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4, rodada A: layout conhecido. |
| `boot.headless-start` | `PublicStableBeta` | Action | não |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | não |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | não |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | não |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): o OMSI apresenta o primeiro registro de cada rótulo exato; a inicialização o seleciona por esse rótulo. Fechada estaticamente; validação em runtime pendente. |
| `world.saved-situation` | `PublicStableBeta` | Action | não |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | não |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: o ramo nativo está fechado estaticamente (BI-006); validação em runtime pendente. Nunca uma heurística do .osn mais recente. |
| `world.explicit-date` | `PublicExperimental` | Action | não |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: gravada no relógio de início do OMSI antes de NewSituation (análise estática); validação em runtime pendente. |
| `world.explicit-time` | `PublicExperimental` | Action | não |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: gravada no relógio de início do OMSI antes de NewSituation (análise estática); validação em runtime pendente. |
| `world.explicit-year` | `PublicExperimental` | Action | não |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: gravada no relógio de início do OMSI antes de NewSituation (análise estática); validação em runtime pendente. |
| `world.weather` | `PublicExperimental` | Action | não |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): aplicado logo depois que o mundo inicia, pelo próprio loadWeather do OMSI ou por suas flags de METAR; substitui o clima de uma situação salva. Fechada estaticamente; validação em runtime pendente. |
| `player-vehicle.model` | `Unsupported` | Action | não |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; resolvido para diagnóstico, não aplicado. |
| `player-vehicle.repaint` | `Unsupported` | Action | não |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | não |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | não |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | não |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false em um início headless; o snapshot completo com uma situação salva. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: a sequência nativa de atribuição está fechada estaticamente (research/reports/beta4-static/entrypoint-last-player-vehicle.md seção 3), mas não é implementada antes da validação em runtime; prevista para um beta posterior. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | não |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | não |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transacional, validado offline. |
| `input.controller` | `PublicExperimental` | Write | não |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transacional, validado offline. |
| `presentation.splash` | `PublicStableBeta` | Write | não |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | não |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4, rodada B: a supressão é aplicada e relatada (internet-textures.suppressed) pelo anel de telemetria. |
| `internet-textures.override` | `PublicExperimental` | Write | não |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Fechamento de runtime I01. |
| `session-profiles` | `PublicStableBeta` | Action | não |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | Início por perfil RA-009; 1..99 e descoberta offline. |
| `hof.management` | `PublicStableBeta` | Write | não |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4, rodada A: C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | não |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | não |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | não |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | não |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | não |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | não |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | não |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 fechado estaticamente: SetActualDateTime apenas copia o relógio do PC para o relógio de início; as datas de início usam world.explicit-date. Substituído: a ABI nativa e as pós-condições não estão fechadas. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: grava actWeather (Weather+0x08), o registro do qual o ciclo de clima do OMSI deriva; fechada estaticamente. Os valores derivados (umidade, fatores de luz, vento atual) permanecem somente leitura (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Validação em runtime pendente. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Exige um PlayerVehicle (uma situação salva fornece um); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE em um NEW_MAP headless. |
| `ui.pause` | `PublicExperimental` | Action | Running | `ui.pause.state`, `ui.pause.take-over` (omsi-state), `ui.pause.restore-native` (omsi-state), `ui.pause.action` (omsi-state) | `ui.pause-menu.replace` | RUNTIME_VALIDATION_REQUIRED | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action --action=<resume|new-game|save|options|quit>` | Beta 5: design validado estaticamente (research beta4-static pause-menu-and-input.md); as chamadas nativas ainda não foram observadas em uma sessão real. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | A detecção de obsolescência após remoção natural (RV-002) não tem produtor seguro em runtime e é validada offline. |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` (paged) | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` (paged), `timetable.trips.list` (paged), `timetable.lines.list` (paged), `timetable.rv-files.list` (paged), `timetable.track-entries.list` (paged), `timetable.bus-stops.list` (paged), `timetable.station-links.list` (paged), `timetable.tours.list` (paged), `timetable.profiles.list` (paged), `timetable.tour-entries.list` (paged), `timetable.logs.read` (paged), `timetable.no-rv-numbers.list` (paged) | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` (paged), `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` (paged), `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | Escritas: scripts.string.write. |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): o OMSI copia uma string no formato de constante; fechada estaticamente. Validação em runtime pendente. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): TriggerXML/SoundTrigger virtuais pelos slots de VMT perfilados; fechada estaticamente. Validação em runtime pendente. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Reset do dispositivo observado (resetting, restored, invalidação de generation); uma transição lost distinta não foi produzida (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` é `InternalOnly`: ela existe no plugin para pesquisa (retorna um endereço nativo) e é rejeitada na fronteira da API e pelo plano de controle local, que validam primeiro o nome da operação contra `PublicRuntimeOperationIds`; a CLI não tem rota para ela. `session.stop` encerra o OMSI; o desligamento cooperativo não está implementado.

<a id="pagination"></a>
## Paginação

Toda operação de lista (marcada com "(paged)" no [catálogo de capacidades](#capability-catalog)) recebe dois argumentos opcionais e responde com quatro campos de página:

| Argumento | Significado |
| --- | --- |
| `cursor` | Onde a página começa: o `next_cursor` da página anterior. Omitido: a primeira página. Um cursor de outra lista, ou malformado, gera `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Linhas desejadas (um inteiro positivo). Maior que a página máxima da lista: usa-se o máximo. Não sendo um inteiro positivo: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Chave de resultado | Significado |
| --- | --- |
| `count` | Todos os registros que o OMSI mantém. |
| `returned_count` | Linhas nesta resposta. |
| `truncated` | `true` quando há registros após esta página. |
| `next_cursor` | Presente somente quando há registros adiante: passe-o como `cursor` para a próxima página. |

O cursor é uma chave estável, não uma posição. Para arrays de registros (tabela de horários, nomes de scripts, constantes, curvas, HOFs, motoristas, bilhetes, logs), as linhas são identificadas como `<row>.<n>.<field>` (ou `name.<n>`) pelo índice absoluto do registro e o cursor nomeia o próximo índice, de modo que a primeira página é igual à resposta da Beta 3. Para coleções de handles (`road-vehicles.list`, `humans.list`), as linhas são retornadas em ordem de handle e o cursor nomeia o último handle retornado: um objeto que aparece entre duas páginas recebe um handle posterior e vem em uma página posterior; nenhum é repetido. Trate os valores de cursor como opacos.

Tamanhos máximos de página: 1000 para `road-vehicles.list` e `humans.list`; 512 para `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` e `vehicle.curves.list`; 256 para `timetable.tour-entries.list`, `timetable.logs.read` e `vehicle.hofs.read`; 128 para as demais listas da tabela de horários, `drivers.read` e `tickets.read`. Quando as linhas de uma página ainda excedem o mailbox de 64 KiB, o plugin descarta as últimas linhas até a resposta caber e `next_cursor` continua na primeira linha descartada, de modo que nenhuma lista falha por causa de seu tamanho.

<a id="public-runtime-operations"></a>
## Operações públicas de runtime

Esta é a lista exata de ids de operação que um frontend público pode encaminhar (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Todas exigem `SessionState.Running`; nenhuma é registrada no journal nem restaurada. Os argumentos obrigatórios são verificados pelo registro antes de o mailbox ser usado (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); os argumentos opcionais são validados pelo plugin. Códigos de falha comuns a todas as operações: `OL_E_RUNTIME_OPERATION_UNKNOWN` (fora desta lista), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (o plugin não tem implementação), `OL_E_RUNTIME_OPERATION_FAILED` (exceção inesperada dentro do processo; valores `detail` e `exception`).

<a id="time"></a>
### Hora

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `time.read` | Read | nenhum | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcionais `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); pelo menos um | as chaves de `time.read` após o `SetTime` perfilado | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Clima

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | nenhum | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate` e os valores de origem `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, os fatores de luz e as umidades são os valores por ciclo do OMSI; `cloud_type` é o número do tipo de nuvem resolvido | |
| `weather.set` | Write | pelo menos um entre `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Intervalos: neblina 0..100000, lightness e valores de solo 0..1, velocidade do vento 0..200, direção do vento 0..360, temperatura e ponto de orvalho -60..60, pressão 850..1100, precipitação 0..2, taxa 0..255, altura das nuvens 0..20000, `cloud_type` 1..64 caracteres da página de código 1252 (`-1` = sem nuvens), booleanos `true`/`false`/`1`/`0` | as chaves de `weather.read`; com `cloud_type` também `cloud_type_resolved` (`false` quando o OMSI não conhece o tipo de nuvem: ele não desenha nenhuma) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (uma chave derivada: `relative_humidity`, `absolute_humidity`, os fatores de luz, `cloud_transparency`, `precipitation_set`, `wet_ground`; o detalhe nomeia a chave a definir), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (chave desconhecida), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 letras ou dígitos) | as chaves de `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | nenhum | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `map.read` | Read | nenhum | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Câmera

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | nenhum | `family` (0 motorista, 1 passageiro, 2 externa, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcionais `family` (0..3), `field_of_view` (10..170); pelo menos um | as chaves de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obrigatório `family` (0..3); opcional `preset` (0..255, somente com a família 0 ou 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. A política é reaplicada a cada 100 ms até `camera.unlock`; uma reaplicação com falha emite o evento `camera.lock.degraded` uma vez por erro distinto. |
| `camera.unlock` | Action | nenhum | `locked` (`false`) | |

<a id="user-interface-pause-menu"></a>
### Interface do usuário (menu de pausa)

Assume o menu de pausa nativo do OMSI para o menu próprio de um módulo (capacidade `ui.pause-menu.replace`, resolvida somente em uma identidade de código conhecida). O byte de pausa do próprio OMSI permanece definido, de modo que a simulação, a renderização, os cliques 3D e os comandos DirectInput continuam retidos; o formulário nativo é ocultado pela VCL e suas ações são chamadas diretamente (sem clique, sem mensagem postada, sem janela movida). `ui.pause.action` executa o handler nativo a partir de um timer de disparo único na thread de UI depois que a resposta é enviada, porque o handler pode entrar em uma caixa de diálogo modal nativa: leia o resultado em `ui.pause.state` (`busy` `false`, depois `last_result`).

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `ui.pause.state` | Read | nenhum | `paused`, `native_menu_visible`, `main_form_ready`, `in_frame`, `taken_over`, `busy`, `pending_action`, `last_action`, `last_result` (`none`, `resumed`, `game-started`, `returned`, `quit-requested`, `exception`, `refused`) | `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.take-over` | Action | nenhum | as chaves de `ui.pause.state` após o take-over | `OL_E_UI_NOT_PAUSED`, `OL_E_UI_BUSY`, `OL_E_UI_ALREADY_TAKEN_OVER`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.restore-native` | Action | nenhum | as chaves de `ui.pause.state`; exibe o menu nativo novamente (seu próprio `FormShow` é executado: o OMSI continua pausado) | `OL_E_UI_BUSY`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.action` | Action | obrigatório `action`: `resume`, `new-game`, `save`, `options` ou `quit` | as chaves de `ui.pause.state` mais `scheduled` (`true`) e `action` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_UI_NOT_PAUSED`, `OL_E_UI_BUSY`, `OL_E_RUNTIME_OPERATION_FAILED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_CAPABILITY_UNRESOLVED` |

<a id="road-vehicles-and-player"></a>
### Veículos rodoviários e jogador

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | nenhum | `count`, `ai_collection`, `player_index` (índice bruto do veículo do jogador no OMSI, informado como lido; use `present` de `player-vehicle.read` para saber se existe um veículo do jogador) | |
| `road-vehicles.list` | Read | `cursor`, `limit` opcionais | campos de página; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` é a posição na lista do OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | obrigatório `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | nenhum | `present` (`false` quando o OMSI não tem veículo do jogador); quando `true`: `player_index` mais todas as chaves de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obrigatório `model` (`Vehicles\...\*.bus` canônico, no máximo 240 caracteres, sem `..`, deve existir abaixo da instalação) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Não atribui o PlayerVehicle. Leva vários segundos; use o timeout de cliente de 30 s. |
| `road-vehicles.place-random` | Action | opcionais `ai_type` (0..255, padrão 0), `group` (0..65535, padrão 1), `type` (-1..65535, padrão -1), `scheduled` (0..1, padrão 0), `tour` (0..65535, padrão 0), `line` (0..65535, padrão 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humanos

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | nenhum | `count` | |
| `humans.list` | Read | `cursor`, `limit` opcionais | campos de página; `human.<n>.handle` (`hb-NNNNNN`; `<n>` é a posição na lista do OMSI) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | obrigatório `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Tabela de horários, motoristas, bilhetes

As listas da tabela de horários, `drivers.read` e `tickets.read` são paginadas (veja [paginação](#pagination)); "page fields" abaixo significa `count`, `returned_count`, `truncated` e `next_cursor`, e toda lista recebe os `cursor` e `limit` opcionais.

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | nenhum | `invalid` (`true`/`false`) e as contagens de registros `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | `cursor`, `limit` opcionais | campos de página; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | `cursor`, `limit` opcionais | campos de página; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | `cursor`, `limit` opcionais | campos de página; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | `cursor`, `limit` opcionais | campos de página; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | `cursor`, `limit` opcionais | campos de página; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | `cursor`, `limit` opcionais | campos de página; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | `cursor`, `limit` opcionais | campos de página; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | `cursor`, `limit` opcionais | campos de página; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | `cursor`, `limit` opcionais | campos de página; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | `cursor`, `limit` opcionais | campos de página; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | `cursor`, `limit` opcionais | campos de página; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | `cursor`, `limit` opcionais | campos de página; `no_rv.<n>.group`, `no_rv.<n>.number`: os números de veículo que o OMSI mantém fora do uso livre pela IA (a lista de strings de números reservados do OMSI, lida pelo próprio layout dele; `OL_E_RUNTIME_VALUE_INVALID` quando a lista não tem o formato esperado). No máximo 256 por página. Análise estática; validação em runtime pendente. |
| `map.tiles.list` | Read | `cursor`, `limit` opcionais | campos de página; `tile.<n>.x`, `tile.<n>.y` (posição na grade), `tile.<n>.tile_index` (o índice próprio do tile na lista de tiles do mapa), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` com `tile.<n>.error` para um tile que não pode ser lido. No máximo 256 por página. Posição e índice a partir de análise estática; validação em runtime pendente. |
| `drivers.read` | Read | `cursor`, `limit` opcionais | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | `cursor`, `limit` opcionais | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts de veículo, constantes, curvas, HOF (por handle)

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | obrigatório `handle`; `cursor`, `limit` opcionais | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obrigatórios `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (relido) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | obrigatório `handle`; `cursor`, `limit` opcionais | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | obrigatórios `handle`, `name` (com diferenciação de maiúsculas/minúsculas, como o script o declara, até 128 caracteres), `value` (1..1024 caracteres) | `handle`, `name`, `value` relido após a escrita | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (o próprio OMSI ignoraria silenciosamente um nome desconhecido), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | obrigatórios `handle`, `name` (página de código 1252, até 128 caracteres), `value` (`1` executa `name`, `0` executa `name_off`, `2` executa `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. A entrada do script é executada de forma síncrona; um nome que o script não define não faz nada. Um pressionar normalmente é seguido de um soltar |
| `vehicle.sound-trigger` | Action | obrigatórios `handle`, `trigger` (página de código 1252, até 128 caracteres); opcional `file` (até 260 caracteres) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (também: o veículo não tem pacote de sons), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | obrigatório `handle`; `cursor`, `limit` opcionais | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | obrigatório `handle`; `cursor`, `limit` opcionais | `handle`, campos de página, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obrigatórios `handle`, `name`, `x` (float finito; um `x` ausente ou não numérico gera `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolação linear) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | obrigatório `handle`; `cursor`, `limit` opcionais | `handle`, campos de página, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

As operações D3D são executadas no thread principal/de renderização do OMSI por meio da ponte nativa. Os handles de textura são `d3dtex-<sessionId N>-<16 hex digits>`.

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | nenhum | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obrigatórios `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcional `levels` (0..16, padrão 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obrigatório `handle`; opcional `level` (0..15, padrão 0) | as 13 chaves de `d3d.texture.create`, para o nível solicitado | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, mais os erros de create |
| `d3d.texture.update` | Action | obrigatórios `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (no máximo 48 KiB decodificados); opcionais `level` (0..15, padrão 0), `x` (0..4095, padrão 0), `y` (0..4095, padrão 0) | as 13 chaves de `d3d.texture.create` após a atualização | `OL_E_D3D_INVALID_PIXEL_BUFFER`, mais os erros de describe |
| `d3d.texture.release` | Action | obrigatório `handle` | as 13 chaves de `d3d.texture.create` com `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` em liberação repetida, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

Operações D3D com falha retornam os valores `detail` e `native_status`. Os métodos de extensão de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulam essas operações para consumidores da API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` retorna uma `Capability(Name, Available, EvidenceState, Reason)` por entrada do catálogo que não seja `InternalOnly`. `Name` é o id do catálogo e `EvidenceState` sua evidência no catálogo. `Available` é avaliado para a instalação:

- As entradas `Unsupported` nunca estão disponíveis; `Reason` traz a nota do catálogo.
- Toda entrada exige a plataforma atual, e toda entrada, exceto `runtime.current-windows-x64`, exige uma instalação do OMSI 2.
- Uma entrada com capacidades nativas no layout conhecido informa `native: RESOLVED_KNOWN_LAYOUT`. Em outro build do OMSI 2, o arquivo executável é resolvido estaticamente: `native: STATICALLY_RESOLVED`, ou `native: PENDING_IN_PROCESS_RESOLUTION` nomeando o que o arquivo não resolveu. Uma entrada pendente ainda é informada como disponível, porque a resolução no processo decide; se ela falhar, a operação ou o início falha com `OL_E_CAPABILITY_UNRESOLVED`.

O planejador informa capacidades por plano em `SessionPlan.RequiredCapabilities` e `SessionPlan.UnsupportedRequestedFeatures` com os mesmos ids do catálogo (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Um recurso solicitado e não suportado traz sua evidência do catálogo; uma capacidade nativa exigida traz `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Limitações conhecidas

- Os handles têm escopo de sessão e são informados como `OL_E_RUNTIME_OBJECT_HANDLE_STALE` quando seu objeto deixa de existir. Um handle de veículo é vinculado à sua VMT, à sua definição e ao `IDCode` do OMSI (um número único no processo que todo objeto do mapa recebe na construção e que nunca é reutilizado), de modo que um objeto recriado no mesmo endereço é sempre distinguido. Os humanos são slots em pool que vivem durante toda a sessão do mapa: um handle de humano deixa de resolver quando o humano some (`RenderMe` 0) ou o slot é gerado novamente (nova estação de destino ou partida).
- Os resultados são limitados ao mailbox de 64 KiB; toda lista é paginada (veja [paginação](#pagination)).
- Sem realocação de veículos entre tiles, sem escrita de calendário, sem atribuição headless do PlayerVehicle. As escritas de variáveis string, os gatilhos de script e as escritas de clima são `EXPERIMENTAL` (validação em runtime pendente). Veja [limitações conhecidas](known-limitations.md).
