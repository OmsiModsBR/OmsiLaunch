# Perfis de sessão

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Tradução da [página original em inglês](../../../reference/session-profiles.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Um perfil de sessão é um pacote YAML declarativo que um autor de conteúdos distribui com um mapa ou um add-on para que os utilizadores finais possam iniciar uma sessão reprodutível do OmsiLaunch com um único comando (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Esta página é a referência normativa do formato `omsilaunch.session-profile/v1` tal como implementado por `SessionProfileCompiler` em `src/OmsiLaunch.Core/SessionProfiles.cs`, das regras de precedência aplicadas pela CLI (`CliInput.BuildSpecAsync` e `RejectProfileConflicts` em `tools/OmsiLaunch.Cli/Program.cs`) e do catálogo de definições que um perfil pode escrever (`ConfigurationCatalog`). Tudo o que um perfil pode fazer também o podem fazer as flags da CLI e o [LaunchSpec](launchspec.md); um perfil limita-se a empacotar essas escolhas.

Estabilidade: `STABLE_BETA` para a análise, a validação, a deteção de conflitos e os blocos `settings` / `presentation` / `internet-textures` / `behavior` (teste offline `session-profiles.strict-compiler`; o caminho de overlay e restauro está validado em runtime por RV-005 e RV-006, ver o [estado da validação em runtime](../status/runtime-validation-status.md)). As chaves `new.date`, `new.time`, `new.year` e `new.weather` são `UNAVAILABLE` neste build (ver [O bloco `new`](#new)).

<a id="package-location-and-naming"></a>
## Localização e nomenclatura do pacote

| Item | Regra |
| --- | --- |
| Diretório do pacote | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Ficheiro do perfil | `<package>\profile.yaml` (nome exato, um único ficheiro) |
| Recursos | Quaisquer ficheiros ou diretórios dentro do diretório do pacote, referenciados por caminho relativo a partir de `presentation.splash.assets` e `internet-textures.profile` |
| `id` | Tem de ser um nome de diretório simples: não pode ser vazio nem só espaços, não pode conter `\`, `/` ou `:` e não pode conter a sequência `..`. As violações dão `OL_E_SESSION_PROFILE_PATH_ESCAPE`. O valor `id` declarado dentro de `profile.yaml` tem de ser igual ao nome do diretório byte a byte (distinguindo maiúsculas de minúsculas); caso contrário `OL_E_SESSION_PROFILE_INVALID`. |
| Seleção | `/predefined-profile:<id>` juntamente com `/predefined-profile-index:<n>`. O índice é obrigatório: `/predefined-profile` sem `/predefined-profile-index` falha com `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Pacote em falta | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Estrutura no lançamento | O pacote de lançamento inclui um exemplo em `.omsilaunch\examples\session-profiles\rmg-leste\` (ver [empacotamento](packaging.md)). Os exemplos não são perfis: para tornar um pacote selecionável, é preciso copiá-lo para `.omsilaunch\session-profiles\<id>\`. |

Um perfil é instalado e removido pelo utilizador ou pelo autor de conteúdos. O OmsiLaunch nunca escreve num pacote, nunca o copia e nunca o elimina. O diretório do pacote não faz parte de nenhuma transação.

<a id="parsing-rules"></a>
## Regras de análise

| Regra | Comportamento | Erro |
| --- | --- | --- |
| Limite de tamanho | `profile.yaml` não pode exceder 256 KiB (262,144 bytes) | `OL_E_SESSION_PROFILE_INVALID` |
| Forma do documento | Exatamente um documento YAML cujo nó raiz é um mapeamento | `OL_E_SESSION_PROFILE_INVALID` |
| Esquema | `schema` tem de ser exatamente `omsilaunch.session-profile/v1` (distinguindo maiúsculas de minúsculas) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Âncoras e aliases | Qualquer nó com uma âncora YAML (`&name`) em qualquer parte do documento é rejeitado antes da validação; por isso, os aliases (`*name`) não podem ocorrer | `OL_E_SESSION_PROFILE_INVALID` ("YAML anchors are not supported.") |
| Chaves desconhecidas | Todos os mapeamentos são fechados: uma chave que não esteja listada para o seu contexto nas tabelas abaixo é rejeitada ("Unknown property in `<context>`: `<key>`"). As chaves são comparadas distinguindo maiúsculas de minúsculas (`Schema:` é uma chave desconhecida). O único mapeamento aberto é `settings`, cujas chaves são validadas contra o catálogo de definições. | `OL_E_SESSION_PROFILE_INVALID` |
| Escalares | Todos os valores folha têm de ser escalares; sequências e mapeamentos onde se espera um escalar são rejeitados ("`<field>` must be a scalar.") | `OL_E_SESSION_PROFILE_INVALID` |
| Números | Os inteiros são analisados com a cultura invariante (`1`, `30`); os decimais em `settings` usam `.` como separador | `OL_E_SESSION_PROFILE_INVALID` |
| Datas e horas | `new.date.value` é analisado por `DateOnly.Parse` e `new.time.value` por `TimeOnly.Parse`, ambos com a cultura invariante; usar as formas ISO `yyyy-MM-dd` e `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| Erros de sintaxe YAML | Reportados com a mensagem do parser | `OL_E_SESSION_PROFILE_INVALID` ("Invalid YAML: ...") |
| Conteúdo executável | O YAML é analisado com `YamlDotNet` apenas para uma árvore de representação; não são suportadas tags, tipos personalizados nem execução de código |

As barras invertidas em escalares simples (sem aspas) são caracteres literais. Os caminhos do Windows escrevem-se com uma única barra invertida (`maps\Grundorf\global.cfg`). Uma barra invertida duplicada num escalar simples fica duplicada no valor; ver [O exemplo incluído no pacote](#the-packaged-example).

<a id="key-reference"></a>
## Referência das chaves

Os contextos têm exatamente os nomes que o compilador lhes dá. Todas as chaves aqui listadas são aceites; nenhuma outra o é.

<a id="profile-root-mapping"></a>
### `profile` (mapeamento raiz)

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `schema` | string | sim | Literal `omsilaunch.session-profile/v1`. |
| `id` | string | sim | Identificador do pacote; tem de ser igual ao nome do diretório. |
| `name` | string | sim | Nome de apresentação; reportado em `SessionProfileMetadata.Name`. |
| `author` | string | sim | Autor; reportado em `SessionProfileMetadata.Author`. |
| `version` | string | sim | String de versão do pacote (forma livre; colocá-la entre aspas: `"1.0"`); reportada em `SessionProfileMetadata.Version`. |
| `compatibility` | mapeamento | não | Ver `compatibility`. |
| `new` | mapeamento | não | Predefinições de NEW_MAP. Ver `new`. |
| `presets` | sequência de mapeamentos | sim | 1 a 99 entradas de predefinição. Zero, mais de 99 ou um valor que não seja uma sequência dá `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `maps` | sequência de strings | não | Identidades de mapa (`maps\<Map>\global.cfg`) para as quais este perfil é válido. `/` é normalizado para `\`; a comparação não distingue maiúsculas de minúsculas. Uma lista ausente ou vazia significa "qualquer mapa". Quando não está vazia, é imposta para `WorldMode.NewMap` (contra o `new.map` efetivo ou `/map`) e para `WorldMode.SavedSituation` (contra o mapa referenciado pelo `.osn` selecionado, resolvido através do catálogo de conteúdos). Para `WorldMode.LastMapState` não é possível derivar nenhum mapa, pelo que uma lista não vazia falha sempre. Falha: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

O bloco é lido e validado sempre que está presente, mas só é aplicado à especificação quando o modo de mundo selecionado é NEW_MAP (`/new`, a predefinição da CLI). Com `/saved:<file.osn>`, o bloco é ignorado.

| Chave | Tipo | Obrigatória | Aplicada | Descrição |
| --- | --- | --- | --- | --- |
| `map` | string | não | sim | Identidade de mapa na forma normalizada `maps\<Map>\global.cfg` (o planeamento exige exatamente esta forma: começa por `maps\`, termina em `\global.cfg`, sem `..`). Define `WorldSpec.MapIdentity`. |
| `entrypoint-index` | inteiro | não | sim | Índice do ponto de entrada apresentado (posição, a partir de 0, na lista de pontos de entrada do OMSI). Define `PresentedEntrypointIndex` e limpa qualquer identidade de ponto de entrada. |
| `entrypoint` | string | não | sim | Identidade bruta do ponto de entrada. Define `EntrypointIdentity` e limpa o índice apresentado. Se `entrypoint-index` e `entrypoint` estiverem ambos presentes, prevalece `entrypoint`, porque é aplicado em último lugar. O valor é uma etiqueta exata (com distinção de maiúsculas/minúsculas) ou uma identidade de descoberta; o OMSI apresenta apenas o primeiro registo de cada etiqueta (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | mapeamento | não | não (`UNAVAILABLE`) | Ver `new.date`. |
| `time` | mapeamento | não | não (`UNAVAILABLE`) | Ver `new.time`. |
| `year` | inteiro | não | não (`UNAVAILABLE`) | Ano explícito. |
| `weather` | mapeamento | não | não (`UNAVAILABLE`) | Ver `new.weather`. |

`date`, `time`, `year` e `weather` são compilados em `DateSpec`, `TimeSpec`, `YearSpec` e `WeatherSpec` com `DateTimeMode.Explicit` / o `WeatherMode` selecionado. O planeador de sessões (`src/OmsiLaunch.Core/SessionPlanner.cs`) exige `world.explicit-date`, `world.explicit-time`, `world.explicit-year` e `world.weather`; as quatro são `EXPERIMENTAL` (fechadas estaticamente, validação em runtime pendente). A data/hora de arranque aplica-se apenas a NEW_MAP; a meteorologia aplica-se a todos os modos de mundo.

#### `new.date`

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `mode` | string | sim | Tem de ser `explicit` (sem distinção de maiúsculas/minúsculas). Qualquer outro valor dá `OL_E_SESSION_PROFILE_INVALID` ("date must use explicit mode."). |
| `value` | string | sim | `yyyy-MM-dd`. |

#### `new.time`

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `mode` | string | sim | Tem de ser `explicit`. |
| `value` | string | sim | `HH:mm` ou `HH:mm:ss`. |

#### `new.weather`

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `mode` | string | sim | `preset`, `icao` ou `real` (sem distinção de maiúsculas/minúsculas). Qualquer outro valor: `OL_E_SESSION_PROFILE_INVALID` ("Unsupported weather mode"). |
| `preset` | string | quando `mode: preset` | Nome da predefinição meteorológica. |
| `icao` | string | quando `mode: icao` | Código ICAO da estação. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (cada entrada de `presets`)

| Chave | Tipo | Obrigatória | Predefinição | Descrição |
| --- | --- | --- | --- | --- |
| `index` | inteiro | sim | | 1 a 99, único dentro do perfil; os índices não precisam de ser contíguos. Selecionado com `/predefined-profile-index`. Duplicado ou fora do intervalo: `OL_E_SESSION_PROFILE_INVALID`; um índice que não existe em nenhum ponto do perfil: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | string | sim | | Identificador da predefinição; reportado como `SessionProfileMetadata.PresetId`. |
| `name` | string | sim | | Nome de apresentação da predefinição; reportado como `SessionProfileMetadata.PresetName`. |
| `settings` | mapeamento | não | nenhuma | Definições semânticas de `options.cfg`, ver [Definições](#settings). As chaves são comparadas com o catálogo sem distinção de maiúsculas/minúsculas. |
| `presentation` | mapeamento | não | herdada | Apresentação do splash, ver `presentation`. Quando ausente, a predefinição herda a base (valor de `/spec` ou a predefinição da CLI, `Managed`). |
| `internet-textures` | mapeamento | não | herdada | Ver `internet-textures`. |
| `behavior` | mapeamento | não | herdada | Timeouts, ver `behavior`. |
| `hofs` | sequência de mapeamentos | não | herdada | Operações de HOF da sessão, ver `hofs`. Quando presentes, substituem o `HofManagement` de base. |
| `modules` | mapeamento | não | herdada | Módulos de runtime, ver `modules`. Combinado com o que um ficheiro `/spec` pede. |

Só a predefinição selecionada é aplicada. Todas as predefinições são, ainda assim, analisadas e validadas, pelo que um erro na predefinição 3 faz falhar um pedido da predefinição 1.

### `presentation`

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `splash` | mapeamento | sim | Obrigatória quando `presentation` está presente ("Presentation requires splash."). Ver `presentation.splash`. |

#### `presentation.splash`

| Chave | Tipo | Obrigatória | Predefinição | Descrição |
| --- | --- | --- | --- | --- |
| `mode` | string | sim | | `managed` instala os bitmaps de ecrã de abertura (splash screen) do OmsiLaunch durante a sessão (`SplashMode.Managed`). `unset` ou `native` preserva os ficheiros de splash do próprio OMSI (`SplashMode.Unset`; `Native` é um alias). Sem distinção de maiúsculas/minúsculas. Qualquer outro valor: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | string | não | `ENG` | Idioma do segundo destino do splash: `PTB`, `ENG`, `DEU`, `FRA` (aliases `PT-BR`, `EN`, `DE`, `FR`; qualquer valor desconhecido é resolvido para `ENG` na construção da sessão). Com `mode: managed`, a sessão aplica um overlay a `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<language>.bmp`. |
| `assets` | string | não | recursos incluídos no pacote de lançamento | Diretório **relativo ao pacote** que contém `ENG.bmp` e, para um `language` que não seja inglês, `<language>.bmp`; cada um tem de ser um BMP de 640x480 e 24 bits. O diretório tem de existir no carregamento do perfil (`OL_E_SESSION_PROFILE_ASSET_MISSING`); os ficheiros são validados no arranque da sessão (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Aplicam-se as regras de confinamento de caminhos. Quando omitido, é usado o `.omsilaunch\assets\splash` da instalação (ou as predefinições incluídas no pacote de lançamento). |

Um perfil não pode definir `SessionPresentationSpec.SuppressTrayIcon`; mantém-se `false`, a menos que um `/spec` o defina.

### `internet-textures`

| Chave | Tipo | Obrigatória | Descrição |
| --- | --- | --- | --- |
| `mode` | string | sim | `native` (`InternetTexturesMode.Native`, o OMSI comporta-se normalmente), `disabled` (`Disabled`, o mecanismo de transferência interno do processo, com perfil de build, é suprimido durante a sessão), `override` (`Override`, um perfil `.itx` com âmbito de sessão é instalado como `Texture\standard.itx`). Sem distinção de maiúsculas/minúsculas; qualquer outro valor: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | string | obrigatória para `override` | Caminho **relativo ao pacote** do ficheiro `.itx`. Chave em falta com `override`: `OL_E_SESSION_PROFILE_INVALID`; ficheiro em falta: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Aplicam-se as regras de confinamento de caminhos. O ficheiro tem de consistir em pares de linhas `URL` / `target` com URLs `http://` ou `https://` (caso contrário `OL_E_ITX_PROFILE_INVALID`) e cada destino tem de ser resolvido abaixo do diretório `Texture\` da instalação sem atravessar um ponto de reanálise (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Os destinos listados e `Texture\standard.ipr` tornam-se eliminações da sessão (ver [transações e recuperação](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Chave | Tipo | Obrigatória | Predefinição | Descrição |
| --- | --- | --- | --- | --- |
| `startup-timeout` | inteiro (segundos) | não | 180 | Tempo permitido desde o arranque do processo até `Running`. Tem de ser positivo no carregamento do perfil; a sessão exige, além disso, 1 a 600 no arranque (caso contrário `OL_E_START_SESSION`). Corresponde a `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | inteiro (segundos) | não | 30 | Corresponde a `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: o supervisor termina o OMSI diretamente e nunca lê este valor. |

Quando o bloco `behavior` está presente, ambos os timeouts são definidos (valor indicado ou predefinição) e substituem por completo o `LaunchBehaviorSpec` de base, incluindo `RestoreConfiguration` e `SuppressStaleClosecheckWarning`, que voltam às respetivas predefinições (`true`, `true`).

### `hofs`

Cada entrada é uma operação de [gestão de HOF](hof-management.md) e indica exatamente uma chave de origem (`package`, `collection`, `directory`, `vehicle` ou `file`); zero ou várias: `OL_E_SESSION_PROFILE_INVALID`. A existência das origens e dos destinos é verificada quando a sessão é planeada (`OL_E_HOF_*`), exceto para `package`.

| Chave | Tipo | Obrigatória | Predefinição | Descrição |
| --- | --- | --- | --- | --- |
| `package` | string | uma chave de origem | | Diretório **relativo ao pacote** que contém os ficheiros `.hof` que o perfil inclui. Tem de existir no carregamento do perfil (`OL_E_SESSION_PROFILE_ASSET_MISSING`); aplicam-se as regras de confinamento de caminhos. Torna-se uma origem `Directory`. |
| `collection` | string | uma chave de origem | | Nome de uma coleção em `.omsilaunch\hof\`. |
| `directory` | string | uma chave de origem | | Diretório relativo à instalação, por exemplo junto ao conteúdo do mapa. |
| `vehicle` | string | uma chave de origem | | Identidade ou pasta de veículo cujos HOF são copiados. |
| `file` | string | uma chave de origem | | Ficheiro `.hof` relativo à instalação. |
| `targets` | sequência de strings | sim | | Veículos de destino (identidades ou pastas abaixo de `Vehicles\`). Em falta ou vazia: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | sequência de strings | não | todos | Nomes de ficheiro a tirar da origem. |
| `mode` | string | não | `add` | `add` ou `mirror` (sem distinção de maiúsculas/minúsculas); qualquer outro valor: `OL_E_SESSION_PROFILE_INVALID`. |

Uma predefinição com `hofs` combinada com uma flag `/hof-source`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Uma entrada por id de [módulo de runtime](../../../reference/runtime-modules.md) (`EXPERIMENTAL`). Cada entrada é um mapeamento com estas chaves (contexto `module`):

| Chave | Tipo | Obrigatória | Predefinição | Descrição |
| --- | --- | --- | --- | --- |
| `enabled` | `true` ou `false` | não | não definida | `true` pede o módulo (`LaunchSpec.Modules.Enable`); `false` mantém-no fora da sessão, incluindo um módulo carregado por predefinição, pelo seu mapa ou pelo ficheiro de definições (`LaunchSpec.Modules.Disable`). |
| `settings` | mapeamento | não | nenhuma | Definições do módulo por chave (`LaunchSpec.Modules.Settings` como `<id>.<key>`). Os valores são verificados face às definições declaradas do módulo quando a sessão é planeada (`OL_E_MODULE_SETTING_INVALID`). |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

Um id de módulo que não seja `[a-z0-9][a-z0-9-]{1,47}`, um `enabled` diferente de `true` ou `false`, ou uma chave de definição que não seja `[a-z][a-z0-9_]{0,31}`: `OL_E_SESSION_PROFILE_INVALID`. Um módulo que não está instalado é comunicado quando a sessão é planeada (`OL_E_MODULE_NOT_FOUND` para `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` apenas para definições).

<a id="settings"></a>
## Definições

As chaves de `settings` são os nomes semânticos de `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). O compilador só aceita uma chave se ela existir (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) e for gravável (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Os valores são guardados como strings e convertidos num patch de `options.cfg` quando a sessão constrói os seus overlays; um valor inválido é, por isso, detetado em `StartSessionAsync`, não no carregamento do perfil, e faz falhar a sessão com `OL_E_START_SESSION`, cuja mensagem contém `OL_E_INVALID_SETTING_VALUE: <key>`. Todas as definições abaixo escrevem em `options.cfg`; todas têm âmbito de sessão e são restauradas com exatidão após a sessão.

Formas dos valores:

- **bool** é `true` ou `false` (sem distinção de maiúsculas/minúsculas). Nos tokens de presença, o token é acrescentado ou removido; nos tokens invertidos (`no_*`), `true` remove o token negativo.
- **int** / **decimal** são validados contra o intervalo; os valores com divisor são guardados divididos (por exemplo, `graphics.minObjectScreenPercent: 5` escreve `0.05`).
- **string** é escrita tal como está.

| Chave da definição | Token de `options.cfg` | Tipo | Intervalo / valores | Evidência |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | qualquer | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | qualquer | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (presença) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (presença) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (presença) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (presença) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (presença) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (presença) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | qualquer | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, guardado /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, guardado /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (presença) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (presença) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, escrito como `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` ou `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (bloco de 4 linhas) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (presença) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (presença invertida) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (ambos tokens de presença) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (presença) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (presença) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (presença) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (presença) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` componente 0 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 em runtime) |
| `traffic.humans` | `AIMaxCountRandom` componente 1 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 em runtime) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Entradas do catálogo que existem mas **não são graváveis** (rejeitadas com `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (substituídas por `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Confinamento de caminhos

`presentation.splash.assets` e `internet-textures.profile` são resolvidos por `Confined(package root, value)`:

1. São rejeitados os caminhos com raiz (`C:\...`), os caminhos que começam por `\` e qualquer componente de caminho igual a `..`.
2. O caminho completo é calculado e tem de começar pelo diretório do pacote.
3. Cada componente existente abaixo da raiz do pacote, até ao caminho final inclusive, é inspecionado quanto ao atributo `ReparsePoint`. Uma junção, uma ligação simbólica de diretório ou uma ligação simbólica de ficheiro em qualquer ponto desse caminho é rejeitada, tal como um componente que não possa ser inspecionado (`IOException` / `UnauthorizedAccessException`).

As três falhas dão `OL_E_SESSION_PROFILE_PATH_ESCAPE`. A mesma regra de pontos de reanálise é aplicada aos destinos `.itx` sob `Texture\` na construção da sessão.

<a id="precedence-and-override-conflicts"></a>
## Precedência e conflitos de substituição

`CliInput.BuildSpecAsync` compõe a especificação por esta ordem:

1. **Predefinições** (NEW_MAP, tudo por definir, timeouts 180 s / 30 s).
2. **`/spec:<file.json>`**, se indicado, substitui por completo as predefinições.
3. **Raiz da instalação**: um argumento de instalação explícito prevalece sobre o `RootPath` da especificação; `.` significa o diretório que contém o executável.
4. **Perfil** (`/predefined-profile` + `/predefined-profile-index`): o pacote é carregado e `RejectProfileConflicts` é executado contra os argumentos brutos da CLI **antes** de qualquer fusão. O bloco de mundo da semente é então reposto num `WorldSpec` vazio do modo selecionado (um mundo de `/spec` é descartado quando se usa um perfil) e `SessionProfileCompiler.Apply` sobrepõe o perfil à semente: `new` (apenas NEW_MAP), `settings` (fundidas sobre o `Environment.General` da semente, prevalecendo o perfil chave a chave) e `presentation`, `internet-textures`, `behavior` (cada um substitui o bloco da semente apenas quando a predefinição o define).
5. **Restantes argumentos da CLI** são sobrepostos: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, flags de meteorologia, flags de veículo, `/set`, flags de splash, flags de internet-textures, `/startup-timeout`, `/shutdown-timeout`. Os timeouts da CLI só se aplicam quando indicados; caso contrário mantém-se o valor da especificação/perfil/predefinição.
6. **Verificação de compatibilidade** para modos que não sejam NEW_MAP (`ValidateCompatibility`).

Um argumento da CLI que vise um campo pertencente ao perfil selecionado é um conflito, rejeitado com `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (código de saída 2, categoria `invalid_argument`). A verificação é feita por campo, não por valor: repetir o próprio valor do perfil continua a ser um conflito.

| Argumento da CLI | Entra em conflito quando o perfil define | Apenas no modo |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` ou `/entrypoint-index` | `new.entrypoint` ou `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | a mesma `<key>` nas `settings` da predefinição (sem distinção de maiúsculas/minúsculas) | qualquer |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (qualquer) | qualquer |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (qualquer) | qualquer |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (qualquer) | qualquer |
| `/module:<id>` | `modules.<id>.enabled: false` | qualquer |
| `/no-module:<id>` | `modules.<id>.enabled: true` | qualquer |
| `/module-setting:<id>.<key>=...` ou uma flag de módulo | `modules.<id>.settings.<key>` | qualquer |

Não são conflitos: chaves `/set` que a predefinição não define (são acrescentadas), flags de veículo (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; um perfil não pode definir um veículo do jogador) e qualquer argumento de mundo com `/saved` (o bloco `new` não é aplicado nesse caso). `/map`, `/entrypoint` e `/entrypoint-index` são inválidos em conjunto com `/saved`, independentemente dos perfis (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Códigos de erro

| Código | Gerado quando | Saída da CLI |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` não existe | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` não é um nome de diretório simples; `assets` / `profile` sai do pacote ou atravessa um ponto de reanálise | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` não é `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | limite de tamanho, forma do documento, âncoras, chave desconhecida, chave obrigatória em falta, valor não escalar, número/data/hora inválido, `id` não coincidente, regras de número/índice das predefinições, palavras de modo não suportadas, timeout não positivo, `presentation` sem `splash`, `override` sem `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` em falta, fora de 1..99, ou nenhuma predefinição com esse `index` | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | uma chave de `settings` não está no catálogo | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | uma chave de `settings` está no catálogo mas é só de leitura | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | o diretório `assets` ou o ficheiro `profile` não existe dentro do pacote | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` não está vazio e o mapa efetivo não está listado (ou não pode ser derivado) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | um argumento explícito da CLI visa um campo pertencente ao perfil | 2 |

Todos estes erros são gerados durante a compilação da linha de comandos, antes do planeamento. São `SessionProfileException` (ou `ArgumentException` no caso do conflito) e nunca iniciam uma sessão. O catálogo completo está em [erros](errors.md); os códigos de saída estão em [códigos de saída](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Descobrir os perfis instalados

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) compila todos os pacotes em `.omsilaunch\session-profiles\` e imprime uma entrada por pacote: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` e as suas `Presets` (`Index`, `Id`, `Name`) por ordem de índice. Um pacote que não compila continua a ser listado, com `IsValid: false` e o mesmo código `OL_E_SESSION_PROFILE_*` que um lançamento comunicaria. Nada é escrito.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Como um perfil aparece na API

Após um carregamento bem-sucedido, a especificação transporta um registo `SessionProfileMetadata` em `LaunchSpec.SessionProfile`:

| Campo | Origem |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` da predefinição selecionada |
| `PresetIndex` | `index` da predefinição selecionada |
| `PresetName` | `name` da predefinição selecionada |
| `PackagePath` | diretório absoluto do pacote |

O planeador acrescenta um diagnóstico informativo `session_profile.selected` a cada `SessionPlan` construído a partir de uma tal especificação, com as chaves de dados `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` e `session_profile.path`. Não afeta a executabilidade. Os integradores que usam diretamente a [API pública](public-api.md) podem chamar `SessionProfileCompiler.Load` e `SessionProfileCompiler.Apply` a partir de `OmsiLaunch.Core`; a representação YAML nunca atravessa para `OmsiLaunch.Api`.

<a id="examples"></a>
## Exemplos

<a id="example-1-settings-only-profile-one-preset"></a>
### Exemplo 1: perfil só com definições, uma predefinição

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

Executar: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. O mapa e o ponto de entrada vêm da linha de comandos porque o perfil não define nenhum bloco `new`; acrescentar `/set:graphics.maxFPS=60` é permitido, acrescentar `/set:traffic.humans=10` é um conflito.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Exemplo 2: perfil associado a um mapa, com três predefinições e recursos incluídos no pacote

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, com `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` e `textures\offline.itx` dentro do pacote:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

Executar: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Com `/saved:situations\mytrip.osn`, o bloco `new` é ignorado e o `.osn` tem de referenciar `maps\Grundorf\global.cfg`.

<a id="the-packaged-example"></a>
### O exemplo incluído no pacote

O lançamento inclui `docs/examples/session-profiles/rmg-leste/profile.yaml` ([ver](../../../examples/session-profiles/rmg-leste/profile.yaml)). É sintaticamente válido, corresponde ao esquema e seria carregado sem erros. Duas propriedades impedem-no de iniciar uma sessão sem alterações neste build:

1. Define `new.date`, `new.time` e `new.weather`, que tornam o plano não executável (ver [O bloco `new`](#new)).
2. Os seus valores de caminho são escalares simples com barras invertidas duplicadas (`maps\\RMG Leste\\global.cfg`). O YAML mantém-nas duplicadas e as identidades de mapa são comparadas textualmente (apenas após a normalização de `/` para `\`), pelo que `new.map` e `compatibility.maps` não corresponderiam à identidade do catálogo `maps\RMG Leste\global.cfg` (`OL_E_MAP_NOT_FOUND` no planeamento). O valor `assets` continua a ser resolvido porque a normalização de caminhos do Windows reduz os separadores duplicados.

A forma executável para este build é:

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## Páginas relacionadas

- [Referência da CLI](cli.md) para `/predefined-profile`, `/predefined-profile-index`, `/set` e as flags de mundo
- [LaunchSpec](launchspec.md) para o registo em que um perfil é compilado
- [Transações e recuperação](../concepts/transactions-and-recovery.md) para a forma como os overlays de `settings`, de splash e de `.itx` são aplicados e restaurados
- [Capacidades](capabilities.md) e [estado da validação em runtime](../status/runtime-validation-status.md)
