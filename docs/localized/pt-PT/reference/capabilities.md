# Capacidades

<!-- l10n: source=reference/capabilities.md source-sha256=04677728db933477292f4f89ce9c5d128ca7a18bb336d117f29647ba70e77d2b -->
> Tradução da [página original em inglês](../../../reference/capabilities.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página documenta o que o OmsiLaunch consegue fazer. A sua fonte é o catálogo canónico de capacidades, `PublicCapabilityRegistry` (`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`): cada capacidade, cada operação de runtime pública com os seus argumentos, rotas da CLI e capacidade nativa, e um estado de evidência por capacidade são declarados aí uma única vez. O conjunto de operações de runtime, a validação de argumentos, as rotas de comando da CLI, o gate nativo por operação do plugin, `GetCapabilitiesAsync` e a [tabela do catálogo](#capability-catalog) abaixo derivam dele. Para cada operação de runtime, a página indica os argumentos, as chaves de resultado e os erros. Como invocar uma operação (rotas da CLI, envelope da API, timeouts, handles) está em [controlo de runtime](runtime-control.md); a evidência por sessão está em [estado da validação em runtime](../status/runtime-validation-status.md).

<a id="classification-and-stability"></a>
## Classificação e estabilidade

`PublicCapabilityClassification` tem quatro valores. Correspondem ao vocabulário de estabilidade da seguinte forma, com despromoções onde a evidência é incompleta:

| Classificação | Significado | Estabilidade |
| --- | --- | --- |
| `PublicStableBeta` | Pública, no contrato Beta, validada em runtime | `STABLE_BETA` (despromovida para `PARTIAL` onde indicado) |
| `PublicExperimental` | Pública, pode mudar, validada em runtime ou validada estaticamente | `EXPERIMENTAL` (despromovida para `PARTIAL` onde indicado) |
| `InternalOnly` | Primitiva de investigação; não acessível através da API pública nem da CLI | `INTERNAL` |
| `Unsupported` | Reconhecida para efeitos de diagnóstico, rejeitada ou ausente | `UNAVAILABLE` |

`PublicCapabilityKind` distingue `Read`, `Write`, `Action` e `Event`. Toda a capacidade de runtime exige uma sessão `Running` (`RequiresSession` no registo) e a sua capacidade nativa resolvida e verificada na build em execução; uma operação cuja capacidade nativa está indisponível é rejeitada com `OL_E_CAPABILITY_UNRESOLVED`. `RequiresExactProfile` assinala capacidades que dependem de destinos nativos (ver [compatibilidade](compatibility.md)).

Os resultados de runtime são dicionários de strings. As chaves que começam por `internal_` ou terminam em `_address`, `_pointer` ou `_vmt` são removidas na fronteira da API (`ScrubInternalValues`) e não são listadas aqui. As chaves de resultado nas tabelas de operações abaixo são os conjuntos exatos de chaves devolvidos pelo produto: foram capturadas através da execução de todas as operações públicas numa sessão real (captura de documentação do fecho de runtime, sessão `f51dcb59-a723-4570-b6c5-b61e628a7993`, `situations\Linie 5.osn`; as linhas das listas escrevem-se `<row>.<n>.<field>`).

Como é comunicada a falha de uma operação (`CurrentRuntimeControl.Execute`):

| Falha | `ErrorCode` | `Values` |
| --- | --- | --- |
| Rejeição pelo registo (operação desconhecida ou `internal.*`, argumento obrigatório em falta) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | nenhum |
| Uma rejeição deliberada do produto (uma chave meteorológica derivada em `weather.set`) | o código específico (`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`) | `detail` (frase) |
| Uma falha de D3D | o código `OL_E_D3D_*` específico | `detail`, `native_status` |
| Uma falha tipada do lado do plugin (handle obsoleto, nome desconhecido, valor fora do intervalo, sem veículo do jogador, ...) | o código específico, por exemplo `OL_E_RUNTIME_OBJECT_HANDLE_STALE` ou `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail` (a mensagem), `exception` (nome do tipo .NET) |
| Uma operação cuja capacidade nativa não está resolvida na build em execução | `OL_E_CAPABILITY_UNRESOLVED` | `detail`, `capability` (a capacidade nativa) |
| Uma exceção inesperada do lado do plugin sem código | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`, `exception` |
| Um resultado que não cabe na mailbox de 64 KiB | `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (as listas paginadas são encurtadas em vez disso e continuam com `next_cursor`, ver [paginação](#pagination)) | nenhum |

A coluna `Errors` («Erros») de cada tabela indica os códigos específicos; chegam em `ErrorCode` através do plugin, da ponte, do plano de controlo, da API e da CLI sem alterações.

<a id="capability-catalog"></a>
## Catálogo de capacidades

Uma linha por entrada do catálogo, gerada a partir de `PublicCapabilityRegistry.All` (o gate de documentação falha quando esta tabela está desatualizada). `Session` é `Running` para as capacidades que precisam de uma sessão em execução. `Operations` são os ids concretos das operações de runtime (linhas de contrato em [operações de runtime públicas](#public-runtime-operations)). `Native capability` designa as entradas do catálogo de destinos nativos (ver [compatibilidade](compatibility.md)) que têm de estar resolvidas e verificadas na build em execução. `Evidence` usa um só vocabulário:

| Evidência | Significado |
| --- | --- |
| RUNTIME_VALIDATED | Observada numa sessão real do OMSI; os ids de sessão estão em [estado da validação em runtime](../status/runtime-validation-status.md). |
| STATICALLY_VALIDATED | Implementada e verificada offline; o efeito em runtime não foi observado. |
| PARTIAL | Funciona para parte do seu contrato; a nota indica a parte em aberto. |
| RUNTIME_VALIDATION_REQUIRED | Implementada, a aguardar a sua validação em runtime. |
| UNAVAILABLE | Reconhecida para efeitos de diagnóstico e rejeitada, ou não implementada; pedi-la no arranque torna o plano não executável. |

Uma operação que altera algo indica a sua política de mutação a seguir ao seu id (`PublicRuntimeOperation.MutationPolicy`); nenhuma operação de runtime é registada no journal:

| Política de mutação | Significado |
| --- | --- |
| `none` | Apenas leituras (não apresentada). |
| `omsi-state` | Altera o estado em memória do OMSI; nada o desfaz; termina com o processo do OMSI. |
| `session-resource` | Cria ou altera um recurso pertencente à sessão, libertado explicitamente ou quando a sessão termina. |
| `session-policy` | Instala uma política que o plugin volta a aplicar até ser removida ou a sessão terminar. |

<!-- generated:capability-catalog:begin -->
| Capacidade | Classificação | Tipo | Sessão | Operações | Capacidade nativa | Evidência | CLI | Nota |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: os eventos do plugin viajam por um anel numerado; um leitor que fique para trás recebe telemetry.lost com a contagem. |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4, ronda A: disposição conhecida. |
| `boot.headless-start` | `PublicStableBeta` | Action | no |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | no |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | no |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4 (BI-001): o OMSI apresenta o primeiro registo de cada etiqueta exata; o arranque seleciona-o por essa etiqueta. Fechada estaticamente; validação em runtime pendente. |
| `world.saved-situation` | `PublicStableBeta` | Action | no |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | no |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: o ramo nativo está fechado estaticamente (BI-006); validação em runtime pendente. Nunca uma heurística do .osn mais recente. |
| `world.explicit-date` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: escrita no relógio de arranque do OMSI antes de NewSituation (análise estática); validação em runtime pendente. |
| `world.explicit-time` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: escrita no relógio de arranque do OMSI antes de NewSituation (análise estática); validação em runtime pendente. |
| `world.explicit-year` | `PublicExperimental` | Action | no |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: escrita no relógio de arranque do OMSI antes de NewSituation (análise estática); validação em runtime pendente. |
| `world.weather` | `PublicExperimental` | Action | no |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4 (BI-003): aplicada logo após o arranque do mundo, através do próprio loadWeather do OMSI ou das suas flags METAR; substitui a meteorologia de uma situação guardada. Fechada estaticamente; validação em runtime pendente. |
| `player-vehicle.model` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007; resolvida para diagnóstico, não aplicada. |
| `player-vehicle.repaint` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | no |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | present=false num arranque headless; o snapshot completo com uma situação guardada. |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: a sequência nativa de atribuição está fechada estaticamente (research/reports/beta4-static/entrypoint-last-player-vehicle.md secção 3), mas não é implementada antes da validação em runtime; prevista para uma beta posterior. |
| `configuration.options.semantic` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transacional, validado offline. |
| `input.controller` | `PublicExperimental` | Write | no |  |  | STATICALLY_VALIDATED |  | Beta 4: overlay transacional, validado offline. |
| `presentation.splash` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | no |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4, ronda B: a supressão é aplicada e comunicada (internet-textures.suppressed) através do anel de telemetria. |
| `internet-textures.override` | `PublicExperimental` | Write | no |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | Fecho de runtime I01. |
| `session-profiles` | `PublicStableBeta` | Action | no |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | Arranque de perfil RA-009; 1..99 e descoberta offline. |
| `hof.management` | `PublicStableBeta` | Write | no |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4, ronda A, C01, C02. |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` (paged) | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | no |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | no |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 fechado estaticamente: SetActualDateTime apenas copia o relógio do PC para o relógio de arranque; as datas de arranque usam world.explicit-date. Substituída: a ABI nativa e as pós-condições não estão fechadas. |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: escreve actWeather (Weather+0x08), o registo de que deriva o ciclo meteorológico do OMSI; fechada estaticamente. Os valores derivados (humidade, fatores de luz, vento atual) permanecem só de leitura (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Validação em runtime pendente. |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` (paged) | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | Precisa de um PlayerVehicle (uma situação guardada fornece um); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE num NEW_MAP headless. |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` (paged) | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | A deteção de obsolescência após remoção natural (RV-002) não tem produtor seguro em runtime e é validada offline. |
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
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4 (BI-004): o OMSI copia uma string com a forma de uma constante; fechada estaticamente. Validação em runtime pendente. |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4 (BI-004): TriggerXML/SoundTrigger virtuais através dos slots perfilados da VMT; fechada estaticamente. Validação em runtime pendente. |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` (paged), `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` (paged), `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` (paged) | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` (paged) | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | Reposição do dispositivo observada (resetting, restored, invalidação de geração); não foi produzida uma transição lost distinta (RV-007). |
<!-- generated:capability-catalog:end -->

`internal.make-basic` é `InternalOnly`: existe no plugin para investigação (devolve um endereço nativo) e é rejeitada na fronteira da API e pelo plano de controlo local, que validam ambos primeiro o nome da operação face a `PublicRuntimeOperationIds`; a CLI não tem rota para ela. `session.stop` termina o OMSI; o encerramento cooperativo não está implementado.

<a id="pagination"></a>
## Paginação

Toda a operação de lista (marcada «(paged)» no [catálogo de capacidades](#capability-catalog)) aceita dois argumentos opcionais e responde com quatro campos de página:

| Argumento | Significado |
| --- | --- |
| `cursor` | Onde a página começa: o `next_cursor` da página anterior. Omitido: a primeira página. Um cursor de outra lista, ou malformado, é `OL_E_RUNTIME_CURSOR_INVALID`. |
| `limit` | Linhas pretendidas (um inteiro positivo). Superior à página máxima da lista: usa-se o máximo. Não sendo um inteiro positivo: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`. |

| Chave de resultado | Significado |
| --- | --- |
| `count` | Todos os registos que o OMSI mantém. |
| `returned_count` | Linhas nesta resposta. |
| `truncated` | `true` quando há registos a seguir a esta página. |
| `next_cursor` | Presente apenas quando há registos a seguir: passá-lo como `cursor` na página seguinte. |

O cursor é uma chave estável, não uma posição. Nos arrays de registos (horário, nomes de scripts, constantes, curvas, HOF, motoristas, bilhetes, registos) as linhas têm a chave `<row>.<n>.<field>` (ou `name.<n>`) segundo o índice absoluto do registo e o cursor indica o índice seguinte, pelo que a primeira página é igual à resposta da Beta 3. Nas coleções de handles (`road-vehicles.list`, `humans.list`) as linhas são devolvidas por ordem de handle e o cursor indica o último handle devolvido: um objeto que surja entre duas páginas recebe um handle posterior e aparece numa página posterior; nenhum é repetido. Os valores de cursor devem ser tratados como opacos.

Tamanhos máximos de página: 1000 para `road-vehicles.list` e `humans.list`; 512 para `timetable.track-entries.list`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list` e `vehicle.curves.list`; 256 para `timetable.tour-entries.list`, `timetable.logs.read` e `vehicle.hofs.read`; 128 para as outras listas do horário, `drivers.read` e `tickets.read`. Quando as linhas de uma página ainda excedem a mailbox de 64 KiB, o plugin descarta as últimas linhas até a resposta caber e `next_cursor` continua na primeira linha descartada, pelo que nenhuma lista falha por causa do seu tamanho.

<a id="public-runtime-operations"></a>
## Operações de runtime públicas

Esta é a lista exata de ids de operação que um frontend público pode reencaminhar (`PublicCapabilityRegistry.PublicRuntimeOperationIds`). Todas exigem `SessionState.Running`; nenhuma é registada no journal nem restaurada. Os argumentos obrigatórios são impostos pelo registo antes de a mailbox ser usada (`OL_E_RUNTIME_ARGUMENT_REQUIRED`); os argumentos opcionais são validados pelo plugin. Códigos de falha comuns a todas as operações: `OL_E_RUNTIME_OPERATION_UNKNOWN` (não consta desta lista), `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_RUNTIME_CHANNEL_BUSY`, `OL_E_RUNTIME_CHANNEL_CLOSED`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_REQUEST_ID_REUSED`, `OL_E_RUNTIME_RESPONSE_INVALID`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` (o plugin não tem implementação), `OL_E_RUNTIME_OPERATION_FAILED` (exceção inesperada dentro do processo; valores `detail` e `exception`).

<a id="time"></a>
### Hora

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `time.read` | Read | nenhum | `hour`, `minute`, `second`, `day`, `month`, `year` | |
| `time.set` | Write | opcionais `hour` (0..23), `minute` (0..59), `second` (0..59.999, decimal); pelo menos um | as chaves de `time.read` após o `SetTime` perfilado | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### Meteorologia

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | nenhum | `fog_density`, `lightness`, `primary_light_factor`, `secondary_light_factor`, `ambient_light_factor`, `cloud_type`, `cloud_transparency`, `precipitation_set`, `wet_ground`, `wind_speed`, `wind_direction`, `relative_humidity`, `absolute_humidity`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, e os valores de origem `set.fog_density`, `set.lightness`, `cloud_type_name`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. `fog_density`, `lightness`, `wind_speed`, `wind_direction`, os fatores de luz e as humidades são os valores por ciclo do OMSI; `cloud_type` é o número do tipo de nuvem resolvido | |
| `weather.set` | Write | pelo menos uma de `fog_density`, `lightness`, `wind_speed`, `wind_direction`, `temperature`, `dew_point`, `pressure`, `precipitation`, `precipitation_rate`, `cloud_type`, `cloud_height`, `ground_moisture`, `ground_moisture_cover`, `ground_wetness`, `snow`, `snow_on_roads`. Intervalos: nevoeiro 0..100000, luminosidade e valores do solo 0..1, velocidade do vento 0..200, direção do vento 0..360, temperatura e ponto de orvalho -60..60, pressão 850..1100, precipitação 0..2, taxa 0..255, altura das nuvens 0..20000, `cloud_type` 1..64 carateres da página de código 1252 (`-1` = sem nuvens), booleanos `true`/`false`/`1`/`0` | as chaves de `weather.read`; com `cloud_type` também `cloud_type_resolved` (`false` quando o OMSI não conhece o tipo de nuvem: não desenha nenhuma) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (uma chave derivada: `relative_humidity`, `absolute_humidity`, os fatores de luz, `cloud_transparency`, `precipitation_set`, `wet_ground`; o detalhe indica a chave a definir), `OL_E_RUNTIME_SETTING_UNAVAILABLE` (chave desconhecida), `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao` (4 letras ou dígitos) | as chaves de `weather.actual.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | nenhum | `active`, `icao`, `last_downloaded`, `invalid_icao`, `counter`, `process` | |

<a id="map"></a>
### Mapa

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `map.read` | Read | nenhum | `loaded`, `loaded_tiles`, `left_hand_traffic`, `name`, `filename`, `friendly_name`, `description`, `max_speed`, `year_start`, `year_end` | |

<a id="camera"></a>
### Câmara

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | nenhum | `family` (0 motorista, 1 passageiro, 2 exterior, 3 mapa), `name`, `field_of_view`, `normal_field_of_view`, `distance` | |
| `camera.set` | Write | opcionais `family` (0..3), `field_of_view` (10..170); pelo menos um | as chaves de `camera.read` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | obrigatório `family` (0..3); opcional `preset` (0..255, apenas com a família 0 ou 1) | `locked` (`true`), `family`, `preset`, `head_look` (`preserved`), `field_of_view` (`preserved`) | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`. A política é reaplicada a cada 100 ms até `camera.unlock`; uma reaplicação falhada emite o evento `camera.lock.degraded` uma vez por cada erro distinto. |
| `camera.unlock` | Action | nenhum | `locked` (`false`) | |

<a id="road-vehicles-and-player"></a>
### Veículos rodoviários e jogador

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | nenhum | `count`, `ai_collection`, `player_index` (índice bruto do veículo do jogador no OMSI, comunicado tal como é lido; usar `present` de `player-vehicle.read` para saber se existe um veículo do jogador) | |
| `road-vehicles.list` | Read | opcionais `cursor`, `limit` | campos de página; `vehicle.<n>.handle` (`rv-NNNNNN`; `<n>` is OMSI's list position) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | obrigatório `handle` | `handle`, `runtime_index`, `tile`, `marked_for_killing`, `traffic_type`, `position_x`, `position_y`, `position_z`, `rotation_x`, `rotation_y`, `rotation_z`, `rotation_w`, `steering`, `tacho`, `ground_speed`, `kilometres`, `throttle`, `brake`, `clutch`, `ai_enabled`, `ai_mode`, `ai_light`, `ai_interior_light`, `ai_indicator_left`, `ai_indicator_right`, `ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | nenhum | `present` (`false` quando o OMSI não tem veículo do jogador); quando `true`: `player_index` mais todas as chaves de `road-vehicle.read` | |
| `road-vehicles.spawn` | Action | obrigatório `model` (canónico `Vehicles\...\*.bus`, no máximo 240 caracteres, sem `..`, tem de existir abaixo da instalação) | `bus`, `created_handle`, `before_count`, `after_count`, `delta_count`, `raw_native_return`, `identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`. Não atribui o PlayerVehicle. Demora vários segundos; usar o timeout de cliente de 30 s. |
| `road-vehicles.place-random` | Action | opcionais `ai_type` (0..255, predefinição 0), `group` (0..65535, predefinição 1), `type` (-1..65535, predefinição -1), `scheduled` (0..1, predefinição 0), `tour` (0..65535, predefinição 0), `line` (0..65535, predefinição 0) | `raw_return`, `before_count`, `after_count`, `delta_count`, `identity_validation` (`native-placement-return-is-diagnostic-only`) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### Humanos

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | nenhum | `count` | |
| `humans.list` | Read | opcionais `cursor`, `limit` | campos de página; `human.<n>.handle` (`hb-NNNNNN`; `<n>` is OMSI's list position) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | obrigatório `handle` | `handle`, `runtime_index`, `human_index`, `tile`, `marked_for_killing`, `collision_type`, `position_x`, `position_y`, `position_z`, `target_x`, `target_y`, `target_z`, `target_station`, `pre_target_station`, `departure`, `enter_bus_at`, `seat_bus`, `seat_station`, `ticket_type`, `ticket_index`, `ticket_ready`, `state`, `speed`, `bus_index`, `station`, `ai_mode`, `ai_mode_ex`, `ai_sub_mode`, `collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### Horário, motoristas, bilhetes

As listas do horário, `drivers.read` e `tickets.read` são paginadas (ver [paginação](#pagination)); «campos de página» abaixo significa `count`, `returned_count`, `truncated` e `next_cursor`, e toda a lista aceita os `cursor` e `limit` opcionais.

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | nenhum | `invalid` (`true`/`false`) e as contagens de registos `tracks`, `trips`, `bus_stops`, `station_links`, `lines`, `rv_files` | |
| `timetable.tracks.list` | Read | opcionais `cursor`, `limit` | campos de página; `track.<n>.filename`, `track.<n>.path`, `track.<n>.entries`, `track.<n>.length` | |
| `timetable.trips.list` | Read | opcionais `cursor`, `limit` | campos de página; `trip.<n>.filename`, `trip.<n>.chrono_origin`, `trip.<n>.target`, `trip.<n>.line`, `trip.<n>.track_index`, `trip.<n>.track_name`, `trip.<n>.bus_stops`, `trip.<n>.profiles`, `trip.<n>.train_reverse`, `trip.<n>.invalid` | |
| `timetable.lines.list` | Read | opcionais `cursor`, `limit` | campos de página; `line.<n>.name`, `line.<n>.chrono_origin`, `line.<n>.priority`, `line.<n>.tours`, `line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | opcionais `cursor`, `limit` | campos de página; `rv_file.<n>.line`, `rv_file.<n>.number_tours`, `rv_file.<n>.start_date_rel_2000`, `rv_file.<n>.end_date_rel_2000`, `rv_file.<n>.type_line_files`, `rv_file.<n>.type_line_probability`, `rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | opcionais `cursor`, `limit` | campos de página; `track_entry.<n>.track_index`, `track_entry.<n>.id`, `track_entry.<n>.path_index_on_object`, `track_entry.<n>.path_tile`, `track_entry.<n>.path_index`, `track_entry.<n>.relative_distance`, `track_entry.<n>.distance`, `track_entry.<n>.valid`, `track_entry.<n>.path_order_check`, `track_entry.<n>.allowed_fstrn`, `track_entry.<n>.chrono_origin`, `track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | opcionais `cursor`, `limit` | campos de página; `bus_stop.<n>.name`, `bus_stop.<n>.supplement`, `bus_stop.<n>.tile`, `bus_stop.<n>.id`, `bus_stop.<n>.parent_id`, `bus_stop.<n>.preset_alighting`, `bus_stop.<n>.index`, `bus_stop.<n>.starting_links`, `bus_stop.<n>.ending_links`, `bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | opcionais `cursor`, `limit` | campos de página; `station_link.<n>.length`, `station_link.<n>.start_bus_stop_id`, `station_link.<n>.end_bus_stop_id`, `station_link.<n>.start_bus_stop`, `station_link.<n>.end_bus_stop`, `station_link.<n>.chrono_origin`, `station_link.<n>.valid`, `station_link.<n>.visible`, `station_link.<n>.track_entries`, `station_link.<n>.start_track_entry`, `station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | opcionais `cursor`, `limit` | campos de página; `tour.<n>.line_index`, `tour.<n>.name`, `tour.<n>.ai_group`, `tour.<n>.ai_group_index`, `tour.<n>.ai_type`, `tour.<n>.completed_day`, `tour.<n>.entries`, `tour.<n>.has_normal_vehicle`, `tour.<n>.invalid`, `tour.<n>.vehicle_indices`, `tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | opcionais `cursor`, `limit` | campos de página; `profile.<n>.trip_index`, `profile.<n>.name`, `profile.<n>.service_trip`, `profile.<n>.stop_times`, `profile.<n>.total_time`, `profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | opcionais `cursor`, `limit` | campos de página; `tour_entry.<n>.line_index`, `tour_entry.<n>.tour_index`, `tour_entry.<n>.trip`, `tour_entry.<n>.trip_index`, `tour_entry.<n>.profile_index`, `tour_entry.<n>.start_time`, `tour_entry.<n>.end_time`, `tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | opcionais `cursor`, `limit` | campos de página; `log.<n>.bus_stop`, `log.<n>.estimated_arrival`, `log.<n>.estimated_departure`, `log.<n>.actual_arrival`, `log.<n>.actual_departure`, `log.<n>.arrival_ok`, `log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | `cursor`, `limit` opcionais | campos de página; `no_rv.<n>.group`, `no_rv.<n>.number`: os números de veículo que o OMSI mantém fora da utilização livre pela IA (a lista de strings de números reservados do OMSI, lida através do seu próprio layout; `OL_E_RUNTIME_VALUE_INVALID` quando a lista não tem a forma esperada). No máximo 256 por página. Análise estática; validação em runtime pendente. |
| `map.tiles.list` | Read | `cursor`, `limit` opcionais | campos de página; `tile.<n>.x`, `tile.<n>.y` (posição na grelha), `tile.<n>.tile_index` (o índice próprio do tile na lista de tiles do mapa), `tile.<n>.filename`, `tile.<n>.version`, `tile.<n>.unsaved`, `tile.<n>.loaded`, `tile.<n>.load_requested`, `tile.<n>.failed`, `tile.<n>.thread_loading`, `tile.<n>.prepared_for_ode`, `tile.<n>.width_south`, `tile.<n>.width_north`, `tile.<n>.objects`, `tile.<n>.path_groups`, `tile.<n>.path_segments`, `tile.<n>.surface_objects`, `tile.<n>.spline_segments`; `tile.<n>.state` = `unavailable` com `tile.<n>.error` para um tile que não pode ser lido. No máximo 256 por página. Posição e índice por análise estática; validação em runtime pendente. |
| `drivers.read` | Read | opcionais `cursor`, `limit` | `count`, `selected_index`; `driver.<n>.filename`, `driver.<n>.name`, `driver.<n>.gender`, `driver.<n>.bus_stops`, `driver.<n>.crashes`, `driver.<n>.passengers`, `driver.<n>.tickets`, `driver.<n>.cash` | |
| `tickets.read` | Read | opcionais `cursor`, `limit` | `filename`, `voice_path`, `stamper_factor`, `buy_factor`, `chattiness`, `whinge_factor`, `count`; `ticket.<n>.name`, `ticket.<n>.display_name`, `ticket.<n>.value`, `ticket.<n>.maximum_stations`, `ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### Scripts, constantes, curvas e HOF de veículos (por handle)

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | obrigatório `handle`; opcionais `cursor`, `limit` | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | obrigatórios `handle`, `name`, `value` (float finito) | `handle`, `name`, `requested_value`, `value` (relido) | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | obrigatório `handle`; opcionais `cursor`, `limit` | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | obrigatórios `handle`, `name` (com distinção de maiúsculas/minúsculas, como o script o declara, até 128 carateres), `value` (1..1024 carateres) | `handle`, `name`, `value` relido após a escrita | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (o próprio OMSI ignoraria silenciosamente um nome desconhecido), `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | obrigatórios `handle`, `name` (página de código 1252, até 128 carateres), `value` (`1` executa `name`, `0` executa `name_off`, `2` executa `name_drag`) | `handle`, `name`, `value`, `dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE`. A entrada do script executa de forma síncrona; um nome que o script não define não faz nada. Um premir é normalmente seguido de um largar |
| `vehicle.sound-trigger` | Action | obrigatórios `handle`, `trigger` (página de código 1252, até 128 carateres); opcional `file` (até 260 carateres) | `handle`, `trigger`, `file`, `dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` (também: o veículo não tem pacote de sons), `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_CAPABILITY_UNRESOLVED`, `OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | obrigatório `handle`; opcionais `cursor`, `limit` | `handle`, campos de página, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | obrigatórios `handle`, `name` | `handle`, `name`, `value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | obrigatório `handle`; opcionais `cursor`, `limit` | `handle`, campos de página, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | obrigatórios `handle`, `name`, `x` (float finito; um `x` em falta ou não numérico dá `OL_E_RUNTIME_ARGUMENT_REQUIRED`) | `handle`, `name`, `x`, `value` (interpolação linear) | `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | obrigatório `handle`; opcionais `cursor`, `limit` | `handle`, campos de página, `hof.<n>.name`, `hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

As operações D3D são executadas na thread principal/de renderização do OMSI através da ponte nativa. Os handles de textura têm a forma `d3dtex-<sessionId N>-<16 hex digits>`.

| Operação | Tipo | Argumentos | Chaves de resultado | Erros |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | nenhum | `available`, `native_status`, `query_interface_hresult`, `cooperative_level_hresult`, `execution_thread_id`, `owned_device_references`, `state` (`NOT_READY`, `READY`, `LOST`, `RESETTING`, `STOPPING`, `STOPPED`), `transition`, `generation`, `live_textures`, `reset_hook_installed`, `last_reset_thread_id`, `binding` | |
| `d3d.texture.create` | Action | obrigatórios `width` (1..4096), `height` (1..4096), `format` (`A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`); opcional `levels` (0..16, predefinição 1) | `handle`, `state` (`LIVE`, `RELEASED`, `STALE`), `device_state`, `generation`, `width`, `height`, `format`, `levels`, `level`, `level_width`, `level_height`, `hresult`, `execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`, `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`, `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | obrigatório `handle`; opcional `level` (0..15, predefinição 0) | as 13 chaves de `d3d.texture.create`, para o nível pedido | `OL_E_D3D_STALE_RESOURCE_HANDLE`, `OL_E_D3D_RESOURCE_RELEASED`, mais os erros de create |
| `d3d.texture.update` | Action | obrigatórios `handle`, `width` (1..4096), `height` (1..4096), `pixels_base64` (no máximo 48 KiB descodificados); opcionais `level` (0..15, predefinição 0), `x` (0..4095, predefinição 0), `y` (0..4095, predefinição 0) | as 13 chaves de `d3d.texture.create` após a atualização | `OL_E_D3D_INVALID_PIXEL_BUFFER`, mais os erros de describe |
| `d3d.texture.release` | Action | obrigatório `handle` | as 13 chaves de `d3d.texture.create` com `state` = `RELEASED` | `OL_E_D3D_RESOURCE_RELEASED` numa libertação repetida, `OL_E_D3D_STALE_RESOURCE_HANDLE` |

As operações D3D falhadas devolvem os valores `detail` e `native_status`. Os métodos de extensão de `D3DRuntimeApi` (`GetD3DStatusAsync`, `CreateD3DTextureAsync`, `DescribeD3DTextureAsync`, `UpdateD3DTextureAsync`, `ReleaseD3DTextureAsync`) encapsulam estas operações para os consumidores da API.

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` devolve uma `Capability(Name, Available, EvidenceState, Reason)` por entrada do catálogo que não seja `InternalOnly`. `Name` é o id do catálogo e `EvidenceState` a sua evidência de catálogo. `Available` é avaliado para a instalação:

- As entradas `Unsupported` nunca estão disponíveis; `Reason` transporta a nota do catálogo.
- Toda a entrada precisa da plataforma atual, e toda a entrada exceto `runtime.current-windows-x64` precisa de uma instalação do OMSI 2.
- Uma entrada com capacidades nativas na disposição conhecida comunica `native: RESOLVED_KNOWN_LAYOUT`. Noutra build do OMSI 2, o ficheiro executável é resolvido estaticamente: `native: STATICALLY_RESOLVED`, ou `native: PENDING_IN_PROCESS_RESOLUTION` indicando o que o ficheiro não resolveu. Uma entrada pendente continua a ser comunicada como disponível, porque a resolução em processo decide; se falhar, a operação ou o arranque falha com `OL_E_CAPABILITY_UNRESOLVED`.

O planeador comunica as capacidades por plano em `SessionPlan.RequiredCapabilities` e `SessionPlan.UnsupportedRequestedFeatures` com os mesmos ids de catálogo (`runtime.current-windows-x64`, `transaction.exact-restore`, `omsi.omsi2`, `world.*`, `boot.headless-start`, `internet-textures.disabled`, `player-vehicle.*`, `input.*`, `content.*`). Uma funcionalidade pedida e não suportada transporta a sua evidência de catálogo; uma capacidade nativa exigida transporta `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`.

<a id="known-limitations"></a>
## Limitações conhecidas

- Os handles têm âmbito de sessão e são comunicados como `OL_E_RUNTIME_OBJECT_HANDLE_STALE` quando o seu objeto desaparece. O handle de um veículo está ligado à sua VMT, à sua definição e ao `IDCode` do OMSI (um número único por processo que todo o objeto do mapa recebe na construção e que nunca é reutilizado), pelo que um objeto recriado no mesmo endereço é sempre distinguido. Os humanos são slots em pool que vivem durante toda a sessão do mapa: o handle de um humano deixa de ser resolvido quando o humano desaparece (`RenderMe` 0) ou o slot é criado de novo (nova paragem de destino ou nova partida).
- Os resultados estão limitados à mailbox de 64 KiB; todas as listas são paginadas (ver [paginação](#pagination)).
- Sem relocalização de veículos entre tiles, sem escrita de calendário, sem atribuição headless do PlayerVehicle. As escritas de variáveis de string, os acionadores de script e as escritas de meteorologia são `EXPERIMENTAL` (validação em runtime pendente). Ver [limitações conhecidas](known-limitations.md).
