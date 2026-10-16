# Códigos de erro e de diagnóstico

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> Tradução da [página original em inglês](../../../reference/errors.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página é a referência normativa para todos os códigos em `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 códigos de erro `OL_E_` e 4 avisos `OL_W_`, agrupados por categoria do catálogo, além dos códigos de diagnóstico informativos que não são erros. Para cada código, ela indica onde o código atual o gera, o que ele significa, como ele chega até você (exceção lançada, campo de resultado, diagnóstico, resposta do plano de controle ou envelope da CLI) e o que fazer. Os significados foram extraídos dos pontos onde os códigos são gerados; quando um código está definido mas não tem caminho de geração atual, a página informa isso.

Páginas relacionadas: [API pública](public-api.md), [códigos de saída](exit-codes.md), [LaunchSpec](launchspec.md), [ciclo de vida da sessão](../concepts/session-lifecycle.md), [transações e recuperação](../concepts/transactions-and-recovery.md), [plano de controle local](local-control.md), [controle de runtime](runtime-control.md), [perfis de sessão](session-profiles.md), [plugin permanente](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Como os códigos chegam até você

| Superfície | Significado |
| --- | --- |
| Lançado | Uma exceção cujo `Message` começa com o código (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). A CLI extrai o código da mensagem e o mapeia para um código de saída (`CliProgram.Classify`). |
| Diagnóstico do plano | Um `LaunchDiagnostic` em `SessionPlan.Diagnostics`; qualquer código `OL_E_` torna `IsRunnable` falso (CLI: plano `NOT RUNNABLE`, código de saída 1). |
| Diagnóstico da sessão | Um `LaunchDiagnostic` em `SessionStatus.Diagnostics`; o estado da sessão é `Failed` (CLI: código de saída 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` e `OL_E_RESTORE_FAILED` envolvem um código interno em sua mensagem. |
| Resultado de runtime | `RuntimeCommandResult.ErrorCode` com `Succeeded = false`. |
| Detalhe de runtime | `RuntimeCommandResult.ErrorCode` é o código específico e `Values["detail"]` a mensagem (com `Values["exception"]`, o tipo .NET). Beta 4: um código tipado do lado do plugin chega à API e à CLI sem alteração; somente uma falha sem código catalogado se torna `OL_E_RUNTIME_OPERATION_FAILED`. |
| Resposta de controle | `ErrorCode` de uma resposta do plano de controle local (`LocalControlResponse`). |
| Envelope da CLI | `error.code` no envelope `--json`, ou `<code>: <message>` no console; o código de saída é informado. |
| Telemetria | Um nome de evento de runtime que o host mapeia para um diagnóstico da sessão. |

## Cli

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Uma `OperationCanceledException` escapou (Ctrl+C ou uma espera de cliente cancelada). | Envelope da CLI, código de saída 7 | Repita o comando. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Uma exceção sem código `OL_E_` escapou: JSON de `/spec` malformado, um membro obrigatório da especificação nulo, uma falha inesperada. | Envelope da CLI, código de saída 10 | Leia a mensagem e `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; corrija a entrada; relate o problema se não houver explicação. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Uma `TimeoutException` sem código escapou; ou o cliente de controle local se conectou a um proprietário que não respondeu dentro do timeout (o proprietário existe, portanto isso não é relatado como `OL_E_NO_ACTIVE_SESSION`). (Timeouts do mailbox trazem `OL_E_RUNTIME_REQUEST_TIMEOUT` em vez disso.) | Envelope da CLI, resposta de controle; código de saída 5 a partir de `Classify`, código de saída 7 para uma resposta de controle | Tente novamente; verifique se o OMSI e o proprietário estão respondendo. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` não está ao lado de `OmsiLaunch.exe`. | Envelope da CLI, código de saída 7 | Reinstale o pacote. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` de `OmsiLaunchW.exe` não retornou nenhum processo. | Envelope da CLI, código de saída 7 | Verifique os arquivos do pacote e as permissões; execute sem `/silent` para ver a falha. |

<a id="compatibility"></a>
## Compatibilidade

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` em `plugin.build.invalid` | As capacidades de início foram resolvidas, mas o formulário Start ativo ou um de seus pontos de hook não está no estado esperado quando o plugin arma o início headless (por exemplo, outro plugin já o modificou). | Diagnóstico da sessão (`Failed`) | Remova o plugin conflitante; consulte [compatibilidade](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` no início; operações de runtime) | Uma capacidade nativa de que a requisição precisa não pôde ser resolvida e verificada neste build do OMSI, portanto está indisponível. No início, a sessão falha antes de qualquer hook e os arquivos são restaurados; uma operação de runtime é rejeitada sem tocar na memória do OMSI. `Data["capability"]` (runtime) ou o valor `capabilities` da telemetria a nomeia. | Diagnóstico da sessão (`Failed`); resultado de runtime | Consulte [compatibilidade](compatibility.md): a capacidade não está disponível neste build. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` indisponível) | A instalação não é uma instalação do OMSI 2: `Omsi.exe` está ausente, não é uma imagem PE x86 de 32 bits, não tem versão do OMSI 2 (2.x), ou a raiz não tem o diretório `maps\` nem `Vehicles\`. O hash do executável não é um critério. | Diagnóstico do plano | Aponte o OmsiLaunch para a raiz de uma instalação do OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` indisponível); também lançado por `CurrentWindowsX64Platform.ValidateCurrent`, que o serviço não chama | Não é Windows 10 ou posterior, ou o sistema operacional ou o processo host não é x64. | Diagnóstico do plano (código de saída 3 da CLI quando lançado) | Execute em Windows 10 ou posterior de 64 bits. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Somente `CurrentWindowsX64Platform.ValidateCurrent` | A arquitetura do sistema operacional ou do host não é x64. O serviço não chama `ValidateCurrent`; não há caminho de geração atual. | Lançado (`PlatformNotSupportedException`) somente por esse método | Consulte o código-fonte `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

<a id="content"></a>
## Conteúdo

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validação: NEW_MAP sem `EntrypointIdentity` e com `PresentedEntrypointIndex` não definido ou negativo. Planejador: o mapa não tem ponto de entrada com a identidade ou o rótulo exato (com diferenciação de maiúsculas/minúsculas) solicitado. | Diagnóstico do plano | Defina `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) ou uma identidade de `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | O clima de início não pode ser aplicado: uma predefinição que não é um arquivo da pasta `weather` da instalação, ou um ICAO que não tem quatro letras ou dígitos. | Diagnóstico do plano | Use uma predefinição instalada ou um código de estação válido. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | O ponto de entrada solicitado compartilha o rótulo com um registro anterior do mapa; o OMSI apresenta apenas o primeiro registro de cada rótulo, portanto ele não pode ser selecionado no início. | Diagnóstico do plano | Escolha o primeiro registro com esse rótulo, ou outro ponto de entrada. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` indisponível) | O mapa do NEW_MAP foi resolvido, mas não há índice apresentado nem identidade. Sempre acompanha `OL_E_ENTRYPOINT_NOT_FOUND`. | Diagnóstico do plano | O mesmo. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` não é um `Vehicles\...\*.hof` instalado. | Diagnóstico do plano | Use uma identidade de `/list:hofs`. (De qualquer forma, os campos do veículo do jogador não são aptos para execução neste build.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Uma origem de [gerenciamento de HOF](hof-management.md) (coleção, diretório, veículo ou arquivo) não existe, não tem `.hof`, ou falta um arquivo citado em `Files`. | Diagnóstico do plano; falha de inicialização | Corrija a origem ou a lista de arquivos. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Um arquivo ou pasta de veículo de destino de HOF não está instalado. | Diagnóstico do plano; falha de inicialização | Use uma identidade de veículo de `/list:vehicles` ou uma pasta existente abaixo de `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Um caminho de HOF sai da instalação, um destino não está abaixo de `Vehicles\`, um nome de coleção não é um nome simples, ou uma junction/symlink é atravessada. | Diagnóstico do plano; falha de inicialização | Use caminhos relativos à instalação, sem `..` e sem links. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Duas operações de HOF gravam bytes diferentes em um mesmo arquivo, ou um espelhamento remove um arquivo que outra operação grava. | Diagnóstico do plano; falha de inicialização | Reordene, divida ou remova as operações em conflito. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Operação de HOF sem destinos ou sem origem, uma origem de arquivo que não é `.hof`, um arquivo de destino que não é `.bus`/`.ovh`, ou um limite de tamanho/quantidade (8 MiB por arquivo, 256 arquivos por operação, 256 MiB por sessão). | Diagnóstico do plano; falha de inicialização | Corrija a operação. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validação: NEW_MAP com `MapIdentity` não definido ou fora do formato `maps\<dir>\global.cfg`. Planejador: o mapa não está instalado. | Diagnóstico do plano | Use uma identidade de `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Uma `FileNotFoundException`/`DirectoryNotFoundException` sem código escapou, por exemplo `/list:repaints` com um `/vehicle-scope` desconhecido, ou `/list:entrypoints` com um `/map` desconhecido. | Envelope da CLI, código de saída 6 | Corrija a identidade. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` não é um item `.cti` do modelo (verificado somente quando `Model` está definido). | Diagnóstico do plano | Use uma identidade de `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | O mapa referenciado dentro do `.osn` selecionado não está instalado. | Diagnóstico do plano | Instale o mapa ou escolha outra situação. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION sem `SituationIdentity`, ou o `.osn` não está instalado. | Diagnóstico do plano | Use uma identidade de `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` não é um `Vehicles\...\*.bus` instalado. | Diagnóstico do plano | Use uma identidade de `/list:vehicles`. |

<a id="installation"></a>
## Instalação

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | O lease da instalação (`Local\OmsiLaunch.Installation.<hash>`) está em poder de outro proprietário nesta sessão de logon, ou um processo do OMSI registrado no journal (PID + horário de criação + caminho do executável; ou qualquer `Omsi.exe` da raiz, para um journal além de `HandoffCreated` sem PID) ainda está ativo. | Início: diagnóstico da sessão via `OL_E_START_SESSION`. Recuperação: lançado (`InvalidOperationException` / `IOException`). Código de saída 7 da CLI. | Pare o outro proprietário (`session stop`) ou espere o OMSI terminar, depois tente novamente ou use `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` está vazio. | Diagnóstico do plano | Informe o diretório da instalação. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` indisponível); também `ValidateCurrent` | A raiz não existe, tem o atributo somente leitura ou não tem o diretório `plugins\`. | Diagnóstico do plano | Aponte para uma instalação real e gravável do OMSI. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (plano e início) | Um arquivo `plugins\OmsiLaunch.*` instalado difere do hash em `release-manifest.json` (ou da closure de referência, quando não há manifesto). | Diagnóstico do plano (plano não apto para execução, código de saída 1 da CLI); diagnóstico da sessão via `OL_E_START_SESSION` somente se os arquivos mudarem entre o planejamento e o início | Reinstale o pacote do OmsiLaunch para que `plugins\` e o manifesto estejam de acordo. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (plano e início) | O manifesto não tem entrada para um arquivo obrigatório do plugin. | Diagnóstico do plano; diagnóstico da sessão via `OL_E_START_SESSION` na mesma condição de corrida descrita acima | Reinstale o pacote. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (plano e início) | Um arquivo obrigatório `plugins\OmsiLaunch.*` está ausente da instalação do OMSI, ou um arquivo `plugins/` listado em `release-manifest.json` não está instalado. | Diagnóstico do plano; diagnóstico da sessão via `OL_E_START_SESSION` na mesma condição de corrida descrita acima | Instale o conjunto de arquivos do plugin permanente ([instalação](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Somente `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` é falso. Não é chamado pelo serviço; não há caminho de geração atual. | Lançado somente por esse método | Consulte o código-fonte. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` está vazio, não é JSON, não tem array `files`, ou tem uma entrada sem `path`/`sha256`, um hash que não tem 64 dígitos hexadecimais, um caminho absoluto, que contém `:`, um segmento vazio, `.` ou `..`, ou um caminho listado duas vezes (comparação sem diferenciar maiúsculas de minúsculas, com `/` e `\` equivalentes). Um BOM UTF-8 é aceito. | Plano: envolvido em `OL_E_RUNTIME_ARTIFACT_MISSING`; início: via `OL_E_START_SESSION` | Reinstale o pacote. |

## InvalidArgument

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validação: `Date.Value`/`Time.Value` definido enquanto o modo não é `Explicit`. CLI: flag desconhecida, valor ausente, inteiro ou intervalo inválido, `/saved` combinado com `/map`/`/entrypoint`, rota de comando desconhecida, qualquer `ArgumentException`/`FormatException` sem código. | Diagnóstico do plano; envelope da CLI, código de saída 2 | Corrija o argumento. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (início) | Um valor de configuração semântica está fora do intervalo, não é booleano, não está no conjunto permitido ou está malformado (`graphics.particles` precisa de quatro campos). Os valores não são validados no momento do planejamento. | Diagnóstico da sessão via `OL_E_START_SESSION` | Use um valor da [tabela de configurações](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | A chave existe, mas não é gravável (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Diagnóstico do plano; código de saída 2 da CLI | Remova a chave. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | A chave não está em `ConfigurationCatalog`. | Diagnóstico do plano; código de saída 2 da CLI | Use uma chave do catálogo. |

## LaunchSpec

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | A raiz não é um objeto JSON, ou a desserialização não produziu nenhum registro. | Lançado (`InvalidDataException`), código de saída 2 da CLI | Corrija o arquivo ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | O arquivo de `/spec` não existe. | Lançado (`FileNotFoundException`), código de saída 6 da CLI | Verifique o caminho. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | O arquivo excede 1 MiB. | Lançado (`InvalidDataException`), código de saída 2 da CLI | Reduza o arquivo. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Um membro que não é propriedade pública do registro naquela posição; mensagem `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Lançado (`InvalidDataException`), código de saída 2 da CLI | Remova ou renomeie o membro. |

## LocalControl

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Handler do proprietário (`OwnerSession`) | O comando não é `session.status`, `session.events`, `session.stop` nem `runtime.execute` com um argumento `operation`. | Resposta de controle; código de saída 7 da CLI | Use um comando suportado. |
| `OL_E_CONTROL_FAILED` | Cliente da CLI (`ReportForwarded`, `CliEventWatch`) | O proprietário respondeu `Ok = false` sem código de erro. | Envelope da CLI, código de saída 7 | Leia a mensagem; verifique o console/os diagnósticos do proprietário. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | O handler do proprietário lançou uma exceção cuja mensagem não traz código `OL_E_` (por exemplo, a sessão já estava fechada), ou a resposta do handler não pôde ser serializada. | Resposta de controle | Leia `session status`; reinicie o proprietário se ele não existir mais. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (ambas as pontas) | Prefixo de comprimento negativo ou acima de 64 KiB (incluindo um frame de requisição grande demais), frame vazio, JSON `null`, uma requisição sem `Command`, ou JSON que não pôde ser decodificado. | Resposta de controle / envelope da CLI | Use o protocolo documentado ([plano de controle local](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (cliente) | A própria requisição serializada do cliente excede 64 KiB. Relatado ao chamador; nada é enviado. | Resposta de controle / envelope da CLI | Reduza a requisição. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (proprietário) | A resposta do proprietário não cabe no frame de 64 KiB. O proprietário responde com este erro tipado em vez de descartar a resposta. `session.status` e `session.events` nunca chegam a ele: o histórico de eventos deles é cortado a partir dos mais antigos para caber. | Resposta de controle / envelope da CLI | Tente novamente; para eventos, leia com mais frequência. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | O `ProtocolVersion` da requisição não é `0.1`; a resposta do proprietário não pôde ser decodificada ou estava vazia; o proprietário fechou a conexão sem responder ou a conexão caiu depois de estabelecida; o proprietário não informou nenhum `SessionId`. | Resposta de controle / envelope da CLI | Use versões correspondentes de cliente e proprietário; leia `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Handler do proprietário | `session.stop` ou `runtime.execute` sem um `session_id` igual ao da sessão ativa. | Resposta de controle; código de saída 7 da CLI | Leia `session.status` primeiro e vincule a requisição (a CLI faz isso automaticamente). |

<a id="other"></a>
## Outros

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | O plano recebido tem `IsRunnable = false`, ou o replanejamento no início não é apto para execução (`Omsi.exe` mudou, conteúdo removido, conjunto de arquivos do plugin ausente); a mensagem lista os códigos `OL_E_` atuais. | Lançado (`InvalidOperationException`); código de saída 1 da CLI | Planeje novamente e corrija os diagnósticos listados. |

<a id="presentation"></a>
## Apresentação

Todos gerados por `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). No momento do planejamento, eles são envolvidos em `OL_E_SESSION_PRESENTATION_INVALID` (a mensagem traz o código); no início, aparecem via `OL_E_START_SESSION`.

| Código | Significado / causa típica | O que fazer |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | O arquivo `.itx` está vazio, tem um número ímpar de linhas não vazias, ou uma linha de URL não é uma URL `http`/`https` absoluta. | Use pares de linhas URL/destino. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (resolvido em relação ao diretório de trabalho do processo) não existe. Lançado como `FileNotFoundException`. | Informe um caminho `.itx` existente. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` é `Override` sem `OverrideProfilePath`. Código de saída 2 da CLI quando lançado. | Forneça `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Uma linha de destino é um caminho absoluto, contém `..`, começa com `\`, resolve para fora da instalação, não tem um componente `Texture\` ou atravessa uma junction/um link simbólico. | Use destinos relativos `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` não existe. | Corrija o diretório. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` ou `<LANG>.bmp` está ausente do diretório de assets, ou um `assets\splash\<LANG>.bmp` empacotado está ausente ao preencher `.omsilaunch\assets\splash`. | Forneça os BMPs / reinstale o pacote. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Um BMP de splash não é um bitmap `BM` de 640×480 e 24 bits. | Converta a imagem. |

<a id="process"></a>
## Processo

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (caminhos de falha no início e de falha do supervisor) | Encerrar ou aguardar o OMSI durante a limpeza após erro lançou uma exceção; a mensagem interna vem em seguida. | Diagnóstico da sessão (adicionado a uma sessão `Failed`) | Certifique-se de que nenhum `Omsi.exe` permaneça, depois use `/recover` se houver um journal pendente. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` falhou logo após `CreateProcessW` (`Win32=<code>`); o processo é encerrado. | Diagnóstico da sessão via `OL_E_START_SESSION` | Tente novamente; verifique antivírus/permissões. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | O OMSI terminou antes de `gameplay.entered` (travamento, uma caixa de diálogo de erro do OMSI foi fechada, a janela foi fechada). | Diagnóstico da sessão (`Failed`); a restauração é executada | Verifique os logs do próprio OMSI e `logfile.txt`; veja em `RuntimeEvents` o último evento do plugin. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` falhou (`Win32=<code>` na mensagem). | Diagnóstico da sessão via `OL_E_START_SESSION` | Resolva o erro Win32 (arquivo ausente, acesso negado, política). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | O loop do supervisor lançou uma exceção (leitura de telemetria, espera/encerramento do processo, gravação do journal); o OMSI é encerrado e a restauração é tentada. | Diagnóstico da sessão (`Failed`) | Leia a mensagem interna e o log do host. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` falhou (`Win32=<code>`). | Dentro das mensagens de `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Encerre o OMSI manualmente, depois use `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` no handle do processo falhou. | Dentro das mensagens de `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | O mesmo. |

## Runtime

"Detalhe de runtime" significa o código específico em `ErrorCode`, com a mensagem em `Values["detail"]` e o tipo da exceção em `Values["exception"]`.

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` com `preset` enquanto `family` é 2 (externa) ou 3 (mapa); predefinições existem apenas para motorista (0) e passageiro (1). | Detalhe de runtime | Omita `preset` ou use a família 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Modo `Explicit` de `Date`/`Time` sem valor ou com componentes fora do intervalo. O nome é histórico; é um erro de validação no momento do planejamento. | Diagnóstico do plano | Corrija o valor (e observe que data/hora explícitas não são aptas para execução neste build). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: o caminho `.bus` não existe no diretório de trabalho do OMSI (verificado antes da chamada nativa para que o OMSI não possa substituí-lo por um fallback). | Detalhe de runtime | Use uma identidade de `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | o mesmo | O MakeVehicle nativo alterou a coleção de veículos rodoviários em mais de um objeto. | Detalhe de runtime (`native_status`, contagens na mensagem) | Relate o problema; os objetos criados permanecem até o fim da sessão. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | o mesmo | A coleção não mudou; o OMSI recusou o veículo silenciosamente. | Detalhe de runtime | Verifique o arquivo `.bus`; tente outro modelo. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | o mesmo | Qualquer outro status nativo diferente de zero. | Detalhe de runtime | Relate o problema com as contagens da mensagem. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | A chamada perfilada de PlaceRandomBus retornou um status de falha. | Detalhe de runtime | Tente novamente quando a jogabilidade estiver estável; relate o problema. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; verificações do lado do plugin (`time.set` sem `hour`/`minute`/`second`; `camera.set` sem `family`/`field_of_view`; `camera.lock` sem um `family` interpretável; operações de veículo/curva) | Um argumento obrigatório está ausente ou em branco. | Resultado de runtime (registro; código de saída 2 da CLI) ou detalhe de runtime (plugin) | Forneça o argumento ([controle de runtime](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | O diretório/arquivo de referência do conjunto de arquivos do plugin ou a ponte nativa indicados em `OmsiLaunchRuntimePaths` não podem ser carregados (pode envolver `OL_E_RELEASE_MANIFEST_INVALID`). | Diagnóstico do plano | Execute a partir de um pacote íntegro. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, harness INTERNAL) | A leitura de base `time.read`/`camera.read` falhou, então o teste de gravação foi ignorado. | Somente artefato do lote | Não é voltado ao usuário. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` está vazio, tem mais de 240 caracteres, contém NUL ou `..`, não começa com `Vehicles\` ou não termina com `.bus`. | Detalhe de runtime | Informe `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | nenhum (mantido por compatibilidade) | Não é mais emitido. Builds anteriores o geravam quando uma requisição cancelada permanecia no slot; todo caminho terminal de uma requisição agora redefine o slot, e uma requisição ou resposta remanescente encontrada no início de uma nova requisição é limpa. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | O mailbox foi descartado porque a sessão está terminando. | Lançado (`InvalidOperationException`) | Nenhuma ação; a sessão terminou. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | O slot do mailbox continha um valor de estado que não é ocioso, requisitado nem respondido (corrupção). O slot é redefinido e o erro é gerado; a próxima requisição funciona normalmente. | Lançado (`InvalidDataException`) | Tente novamente; relate o problema se persistir. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | O ponteiro do bloco de constantes do veículo é nulo. | Detalhe de runtime | O veículo não tem constantes; nada a fazer. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` não está na tabela de constantes do veículo. | Detalhe de runtime | Liste as constantes primeiro. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | O objeto criado pelo spawn tem uma VMT fora do intervalo da imagem do OMSI. | Detalhe de runtime | Relate o problema. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | o mesmo | O objeto criado não está na coleção de veículos rodoviários. | Detalhe de runtime | Relate o problema. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Dois pontos consecutivos da curva têm o mesmo X. | Detalhe de runtime | Problema de conteúdo na curva do veículo. |
| `OL_E_RUNTIME_CURVE_EMPTY` | o mesmo | A curva não tem pontos. | Detalhe de runtime | O mesmo. |
| `OL_E_RUNTIME_CURVE_INVALID` | o mesmo | Nenhum segmento da curva contém `x`. | Detalhe de runtime | Avalie dentro do domínio da curva. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | o mesmo | `name` é desconhecido ou seu ponteiro de função é nulo. | Detalhe de runtime | Liste as curvas primeiro. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | O ponteiro da definição do veículo é nulo. | Detalhe de runtime | O handle se refere a um veículo sem dados de definição. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (modo proprietário) | `plugins\OmsiLaunch.Plugin.opl` ou `plugins\OmsiLaunch.Native.x86.dll` está ausente ao lado do executável. | Envelope da CLI, código de saída 7 | Reinstale o pacote. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` ausente ou em branco para `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (normalmente o registro os rejeita antes com `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Detalhe de runtime | Forneça o handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | O handle é desconhecido, o objeto saiu da coleção, a geração do endereço avançou, ou a impressão digital do objeto (VMT + identidade de definição/modelo) mudou porque o endereço foi reutilizado. Ponto cego residual: mesma classe e mesmo modelo recriados no mesmo endereço entre duas leituras da lista. | Detalhe de runtime | Execute `road-vehicles.list`/`humans.list` novamente e use o novo handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; fallback de `D3DRuntimeApi` | Wrapper genérico de falha do lado do plugin; `Values["detail"]` contém a mensagem (muitas vezes um código mais específico) e `Values["exception"]` o tipo da exceção. Uma exceção que escapa de uma operação dentro do dispatcher do mailbox também é respondida com este código (sem valores), em vez de deixar a requisição sem resposta. | Resultado de runtime | Leia `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | O plugin não tem implementação para uma operação que o registro permitiu (divergência de versão entre registro e plugin). | Detalhe de runtime | Reinstale um pacote consistente. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | A operação não está em `PublicRuntimeOperationIds`, incluindo toda operação `internal.*`. Verificado antes da busca da sessão. | Resultado de runtime; resposta de controle; código de saída 2 da CLI | Use um id de operação público. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` com `preset` enquanto não há veículo do jogador (sessões headless não têm nenhum). Também relatado como `code` do evento `camera.lock.degraded` quando a reaplicação falha. | Detalhe de runtime / evento de runtime | Trave sem predefinição, ou use uma sessão com veículo do jogador. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Um resultado D3D bem-sucedido não tinha valores, ou tinha uma string de estado do dispositivo desconhecida. | Lançado (`OmsiRuntimeException`) | Use versões correspondentes de host e plugin. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | O slot contém uma resposta obsoleta com o mesmo id de requisição da nova requisição. A resposta obsoleta é limpa antes de o erro ser gerado. | Lançado (`InvalidOperationException`) | Use ids de requisição estritamente crescentes. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Nenhuma resposta dentro de `timeout`; o slot é redefinido e uma resposta tardia é descartada. | Lançado (`TimeoutException`); código de saída 5 da CLI | Tente novamente com um timeout maior; verifique se o OMSI não está bloqueado (caixa de diálogo modal, carregamento). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | O envelope de resposta está corrompido, tem um comprimento inválido (negativo, zero ou maior que o slot), um id de sessão alheio ou um id de requisição diferente. O slot é redefinido antes de o erro ser gerado, então a próxima requisição funciona normalmente. | Lançado (`InvalidDataException`) | Tente novamente; relate o problema se persistir. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | O `cursor` de uma operação de lista está malformado ou pertence a outro tipo de lista. | Detalhe de runtime | Passe o `next_cursor` da página anterior da mesma operação, ou omita-o. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | O resultado serializado excede o mailbox de 64 KiB. Os resultados de listas paginadas são, em vez disso, encurtados para caber e continuam com `next_cursor`; nenhum resultado de lista chega a este código. | Resultado de runtime | Use uma operação mais restrita ou um `limit` menor. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | O ponteiro da definição ou do estado do script do veículo é nulo. | Detalhe de runtime | O veículo não tem objetos de script. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; mailbox do plugin | `RuntimeCommand.SessionId` difere do id de sessão do handle (lançado pelo host), ou uma requisição chegou a um plugin vinculado a outra sessão (retornado pelo plugin como resultado tipado). | Lançado (`InvalidOperationException`) / resultado de runtime | Construa o comando com `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` do evento `world.failed`) | O clima de início não pôde ser aplicado depois que o mundo iniciou: o OMSI não tem estação ICAO configurada para `/weather-real`, ou seus objetos de clima não estão disponíveis. A sessão falha. | Falha da sessão | Configure uma estação no OMSI ou use `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Uma chave de `weather.set` nomeia um valor que o OMSI deriva a cada ciclo de clima (umidade, fatores de luz, transparência de nuvens, flag de precipitação, solo molhado); nada é gravado. | Resultado de runtime | Defina a chave de origem indicada em `detail` (por exemplo, `dew_point` em vez de `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Chave de `weather.set` desconhecida; nada é gravado. | Resultado de runtime | Use uma chave listada em [capacidades](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` não está na tabela de variáveis de string. | Detalhe de runtime | Liste as variáveis de string primeiro. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Um booleano de clima não é `true`/`false`/`1`/`0`. Atualmente inalcançável (veja acima). | Detalhe de runtime (definido) | Consulte o código-fonte. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` não é um inteiro, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` pode ser -1), `scheduled` 0..1; `vehicle.variable.set`: `value` não finito; `vehicle.curve.evaluate`: `x` não finito. | Detalhe de runtime | Use um valor dentro do intervalo. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` não está na tabela de variáveis numéricas. | Detalhe de runtime | Liste as variáveis primeiro. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | O slot da variável ou o endereço do valor é nulo. | Detalhe de runtime | A variável não está materializada para este veículo. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Os escalares do relógio foram gravados, mas a chamada nativa perfilada de SetTime retornou falha. | Detalhe de runtime | Tente novamente; leia o valor de volta com `time.read`. |

## RuntimeD3D

Todos gerados por `CurrentRuntimeControl` (mapeamento `ThrowD3D` do status nativo; `detail` traz a operação e o HRESULT, `native_status` o status numérico). Superfície: resultado de runtime com o código em `ErrorCode`; `D3DRuntimeApi` relança como `OmsiRuntimeException`.

| Código | Status nativo / causa | O que fazer |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: o dispositivo Direct3D foi perdido. | Aguarde `d3d.restored`; recrie as texturas (a geração mudou). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: `width`, `height`, `level`, `x`, `y`, `format` ou `handle` ausente ou inválido (intervalos: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Corrija os argumentos. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` não é Base64 válido ou excede 48 KiB. | Envie retângulos menores. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, ou um nome de `format` desconhecido (válidos: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Use um formato listado. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Qualquer outro status; o HRESULT está em `detail`. | Relate o problema com o HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: o dispositivo não está pronto (antes do primeiro frame ou durante a parada). | Tente novamente após `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: um reset do dispositivo está em andamento. | Tente novamente após `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: o handle da textura já foi liberado. | Não reutilize handles liberados. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: o handle pertence a uma geração anterior do dispositivo; ou a string do handle não é `d3dtex-<session>-<hex>` / é zero. | Recrie a textura. |

<a id="session"></a>
## Sessão

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` em `plugin.request.unsupported` | Plano: foram solicitados `LastMapState`, `EntrypointIdentity`, modo de data/hora/ano, modo de clima, campos do veículo do jogador ou documentos de entrada (`Requested capability unavailable: <name>`). Telemetria: o plugin rejeitou o handoff (não pode acontecer com um plano apto para execução). | Diagnóstico do plano; diagnóstico da sessão | Remova a solicitação não suportada ([limitações conhecidas](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` em `headless.arm.failed` | O plugin não conseguiu armar no OMSI o hook de início headless de disparo único. | Diagnóstico da sessão (`Failed`) | Verifique o build; relate o problema. |
| `OL_E_NO_ACTIVE_SESSION` | Cliente da CLI (`ReportForwarded`, `CliEventWatch`) | Nenhum proprietário responde no pipe de controle da instalação (não há sessão, ou o proprietário ainda está iniciando/validando). | Envelope da CLI, código de saída 4 | Inicie uma sessão, ou espere até que ela esteja `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` se esgotou antes de `plugin.started` (o OMSI não carregou `plugins\OmsiLaunch.Plugin.opl`, ou está travado antes da inicialização do plugin). | Diagnóstico da sessão (`Failed`) | Verifique o conjunto de arquivos do plugin, `plugins\OmsiLaunch.Plugin.opl` e o `logfile.txt` do OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` em `plugin.handoff.invalid` ou JSON de telemetria não interpretável | O plugin não conseguiu ler/verificar o handoff de inicialização (versões 3 a 6, SHA-256), ou enviou telemetria inválida. | Diagnóstico da sessão (`Failed`) | Use versões correspondentes de host e plugin (reinstale o pacote). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Um proprietário já responde a `session.status` para esta instalação. | Envelope da CLI, código de saída 7 | Use comandos de cliente (`session status`, `session stop`, comandos de runtime). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | O estado da sessão não é `Running`. | Lançado (`InvalidOperationException`) | Use `WaitForAsync(session, SessionState.Running, ...)` primeiro. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | O plano de splash/ITX não pôde ser construído; a mensagem traz o código de apresentação. | Diagnóstico do plano | Consulte [Apresentação](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (caixa de diálogo do OmsiLaunchW) | Código de fallback exibido quando um plano de inicialização não é apto para execução ou a sessão não chegou à jogabilidade, e não existe nenhum diagnóstico `OL_E_`. | Somente caixa de mensagem | Leia `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` em `world.situation.failed` | O início nativo da situação salva retornou uma falha (`native_status` no evento). | Diagnóstico da sessão (`Failed`) | Verifique o `.osn` e seu mapa. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | O plugin foi iniciado, mas `Running` não foi alcançado dentro de `StartupTimeoutSeconds`. | Diagnóstico da sessão (`Failed`) | Aumente `/startup-timeout` para mapas grandes; veja em `RuntimeEvents` o último evento de mundo. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Qualquer exceção no caminho de início; a mensagem é a mensagem interna (normalmente começando com o código interno). | Diagnóstico da sessão (`Failed`) | Aja de acordo com o código interno. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` em `world.failed` | O início nativo do NEW_MAP retornou uma falha (`native_status` no evento). | Diagnóstico da sessão (`Failed`) | Verifique o mapa, o índice do ponto de entrada e os logs do OMSI. |

## SessionProfile

Todos gerados por `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) ou `CliInput`, lançados como `SessionProfileException` (uma `IOException` com `Code`), código de saída 2 da CLI. Consulte [perfis de sessão](session-profiles.md).

| Código | Significado / causa típica | O que fazer |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | O diretório `assets` de splash da predefinição ou o arquivo `profile` de texturas da internet não existe dentro do pacote. | Adicione o asset. |
| `OL_E_SESSION_PROFILE_INVALID` | Violação estrutural ou de limite: mais de 256 KiB, não exatamente um mapeamento raiz, âncoras YAML, chave desconhecida, chave obrigatória ausente, valor não escalar onde um escalar é exigido, `id` diferente do nome do diretório, predefinições fora de 1..99 ou `index` duplicado, timeouts não positivos, modo de clima/splash/texturas da internet não suportado, data/hora diferente de `explicit`, YAML inválido, erros de interpretação de número/data. | Corrija o YAML conforme a mensagem. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` não está vazio e não contém o mapa selecionado (NEW_MAP) ou o mapa da situação selecionada (SAVED_SITUATION). | Escolha um mundo compatível. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` não existe. | Verifique o id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Um argumento explícito da CLI tem como alvo um campo que pertence ao perfil/à predefinição selecionados. | Remova a flag ou escolha outra predefinição. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | O id contém `\`, `/`, `:` ou `..`; ou um caminho de asset é absoluto, escapa do pacote ou atravessa uma junction/um link simbólico. | Mantenha os caminhos dentro do pacote. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` ausente, fora de 1..99 ou não declarado pelo perfil. | Use um índice de predefinição declarado. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` não é `omsilaunch.session-profile/v1`. | Use o schema suportado. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Uma chave de configuração da predefinição é conhecida, mas não é gravável. | Remova a chave. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Uma chave de configuração da predefinição não está no catálogo. | Use uma chave do catálogo. |

<a id="transaction"></a>
## Transação

Consulte [transações e recuperação](../concepts/transactions-and-recovery.md).

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Um `closecheck` obsoleto ainda existe após `File.Delete`. | Diagnóstico da sessão via `OL_E_START_SESSION` | Remova `<root>\closecheck` manualmente (permissões). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Um caminho que não existia antes da sessão agora tem conteúdo diferente do que a sessão aplicou; ele não é removido e o journal é mantido. | Lançado (`IOException`); dentro de `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; código de saída 8 da CLI | Inspecione o arquivo; remova-o ou mova-o, depois use `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (journal anterior à impressão digital) | O journal não tem impressão digital do conteúdo aplicado para um caminho originalmente ausente que não é de exclusão, então a propriedade não pode ser comprovada. No início da sessão, a recuperação é adiada e tentada novamente com os bytes planejados desta sessão; via `RecoverPendingAsync`, o erro é lançado. | Lançado (`IOException`); código de saída 8 da CLI | Inicie uma sessão com a mesma especificação (que fornece os bytes), ou inspecione e remova o arquivo, depois use `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | O SHA-256 de um backup não corresponde ao snapshot registrado no journal; nada é gravado. | Lançado; dentro de `OL_E_RESTORE_FAILED`; código de saída 8 da CLI | Restaure o arquivo a partir do seu próprio backup; só então exclua o journal, e apenas quando tiver certeza. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Existem snapshots em memória, mas `journal.json` sumiu (foi excluído durante a sessão). | Lançado; dentro de `OL_E_RESTORE_FAILED` | Verifique os arquivos da sessão manualmente. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` ainda existe após a exclusão (a restauração em si foi bem-sucedida e verificada). | Lançado; dentro de `OL_E_RESTORE_FAILED`; código de saída 8 da CLI | Exclua `<root>\.omsilaunch\journal.json` (permissões) ou execute `/recover` novamente (idempotente). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (falha no início / supervisor) | O término do OMSI não pôde ser confirmado, então os arquivos não foram substituídos enquanto ele ainda poderia usá-los; o journal é mantido. | Diagnóstico da sessão (`Failed`) | Depois que o `Omsi.exe` terminar, execute `/recover` (ou o próximo início recupera automaticamente). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (falha no início / supervisor) | `RestoreAsync` lançou uma exceção; a mensagem traz o código interno; o journal é mantido. | Diagnóstico da sessão (`Failed`); código de saída 8 da CLI quando lançado a partir de `/recover` | Aja de acordo com o código interno, depois use `/recover`. |

<a id="warning"></a>
## Aviso

| Código | Gerado por | Significado | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | O executável do OMSI 2 não tem layout conhecido (sua identidade de código não está no catálogo de builds). O plano continua apto para execução: toda capacidade nativa é resolvida e verificada no processo, e uma que não possa ser verificada fica indisponível. | Diagnóstico do plano (a aptidão para execução não é afetada) | Nada a fazer; consulte `/plan` e `profiles` para ver as capacidades previstas. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (supervisor) | `Behavior.CooperativeShutdown` foi solicitado, mas não foi possível pedir ao OMSI que saísse (sua rotina de saída não está resolvida neste build, ou o OMSI já está fechando), ou ele não saiu dentro de `ShutdownTimeoutSeconds`: o OMSI foi encerrado à força. A restauração é executada normalmente. | diagnóstico da sessão | Nada a fazer: a sessão termina como em um encerramento forçado. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Uma capacidade de que o início precisa não pôde ser resolvida a partir do arquivo executável (por exemplo, um executável protegido por DRM cuja imagem carregada difere do arquivo). Ela é resolvida no processo no início. | Diagnóstico do plano (a aptidão para execução não é afetada) | Nada a fazer; um início falha com `OL_E_CAPABILITY_UNRESOLVED` se ela também não puder ser resolvida no processo. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Um módulo que entrou na sessão por conta própria (ativação por arquivo de configurações, conteúdo ou padrão) não pode ser carregado (`Data["code"]` informa o motivo, `Data["activation"]` como ele entrou). | Diagnóstico do plano (a aptidão para execução não é afetada) | Corrija, desative ou remova o módulo. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Uma entrada de `.omsilaunch\modules.ini` não pode ser usada (não é uma seção de módulo, está fora de uma seção, chave desconhecida, valor inválido; o padrão é usado), ou foram solicitadas configurações para um módulo que a sessão não carrega. | Diagnóstico do plano (a aptidão para execução não é afetada) | Corrija a entrada ou a requisição. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Um caminho de exclusão da sessão (destino ITX, `Texture\standard.ipr`, `closecheck`) não existia antes da sessão e existe agora, mas nenhum processo do OMSI chegou a ser iniciado sob este journal, então o arquivo não pode ser um subproduto da sessão. Ele é mantido e relatado (mensagem = caminho relativo, `Data["sha256"]`); a transação ainda assim é concluída. | Diagnóstico da sessão / `RecoveryStatus.Diagnostics` (o estado não é afetado) | Inspecione o arquivo; remova-o você mesmo se não o quiser. |

<a id="module"></a>
## Módulo

Módulos de runtime (consulte [módulos de runtime](runtime-modules.md)). No planejamento, um código é um diagnóstico do plano: para um módulo solicitado, torna o plano não apto para execução; para um módulo que entrou por conta própria (arquivo de configurações, conteúdo, padrão), o plano relata `OL_W_MODULE_NOT_LOADED`. Em uma sessão, um código é um diagnóstico da sessão (a sessão continua, a menos que o código seja `OL_E_MODULE_SESSION_FAILED`) e, para uma operação de módulo, é o resultado de runtime.

| Código | Gerado por | Significado | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` está ausente, não é um `omsilaunch.runtime-module/v1` válido, ou seu `id` difere do nome do diretório. A mensagem nomeia o campo. | Diagnóstico do plano; `ListRuntimeModulesAsync` | Corrija o manifesto. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Um id de módulo solicitado não está instalado em `plugins\OmsiLaunch.Modules`. | Diagnóstico do plano | Verifique `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Um arquivo fixado está ausente, difere do SHA-256 fixado ou é alcançado por um reparse point; ou, no processo, `module.json` mudou depois do planejamento. | Diagnóstico do plano; diagnóstico da sessão | Reinstale o módulo. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | O intervalo `bridge_protocol` do módulo não inclui nenhum dos protocolos que este build atende (1 e 2). | Diagnóstico do plano | Use uma versão do módulo feita para este OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Uma capacidade exigida é desconhecida para este build, indisponível nesta instalação, ou não resolvida no processo. | Diagnóstico do plano; diagnóstico da sessão | Consulte [capacidades](capabilities.md) e `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | A sessão não executa o mapa vinculado do módulo, ou um arquivo vinculado difere do SHA-256 fixado. | Diagnóstico do plano; diagnóstico da sessão | Inicie o mapa vinculado com o conteúdo vinculado. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | O módulo pediu para iniciar um arquivo que não é um de seus componentes auxiliares. | Resultado de runtime dentro do módulo | Defeito do módulo. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | O componente gerenciado não pôde ser carregado ou seu tipo de entrada não pôde ser criado; ou o programa de sessão do módulo não pôde ser iniciado ou terminou antes de estar pronto. | Diagnóstico da sessão | Verifique o build do módulo (compatível com x86, o tipo de entrada implementa `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` ou `Stop` lançou uma exceção. | Diagnóstico da sessão | Defeito do módulo; sua `failure_policy` foi aplicada. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Uma fase ou uma operação retornou depois do seu orçamento de tempo (`timeouts`, `timeout_ms`), ou um programa de sessão não ficou pronto dentro de `prepare_ms`. | Diagnóstico da sessão; resultado de runtime | Defeito do módulo; sua `failure_policy` foi aplicada. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | O módulo foi desativado ou falhou antes na sessão. | Resultado de runtime | Consulte os diagnósticos da sessão para a primeira falha. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | A sessão não carregou este módulo, ou ele ainda não está pronto (antes do gameplay). | Resultado de runtime | Solicite o módulo no início; aguarde `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | O módulo não declara essa operação. | Resultado de runtime | Verifique as `operations` do módulo. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | A operação falhou sem um código catalogado; o código próprio do módulo está em `Values["module_code"]`. | Resultado de runtime | Específico do módulo. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Depois da parada ou da falha, um recurso rastreado ainda estava ativo (um processo auxiliar que não saiu, uma thread em segundo plano que não terminou) ou sua liberação lançou uma exceção. | Diagnóstico da sessão | Relate o módulo; o processo termina junto com o OMSI de qualquer forma. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Um módulo cuja `failure_policy` é `fail-session` falhou; a sessão é parada e restaurada. | Diagnóstico da sessão (estado `Failed`) | Consulte o código de módulo anterior. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Uma configuração de módulo solicitada é inválida ou não é declarada pelo módulo, um módulo está ao mesmo tempo ativado e desativado, o nome de uma configuração não é `<module-id>.<key>`, ou uma flag de módulo é declarada por vários módulos. | Diagnóstico do plano (não apto para execução); erro da CLI | Corrija a requisição; `/list:RuntimeModules` mostra as configurações de cada módulo. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Um plugin do OMSI que o módulo lista em `conflicts.plugins` está ativado (`plugins\*.opl`) ou carregado no OMSI. O motivo nomeia o `.opl`. | Diagnóstico do plano; diagnóstico da sessão | Desative ou remova esse plugin. O OmsiLaunch nunca o altera. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | reservado | Reservado; um componente nativo recebe, em vez disso, os resultados numéricos de hook descritos em [módulos de runtime](runtime-modules.md#native-components-and-hooks). | nenhuma | nenhum |

<a id="non-error-diagnostic-codes"></a>
## Códigos de diagnóstico que não são erros

| Código | Emitido por | Mensagem / dados | Significado |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | mensagem = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` foi criado e sua identidade foi registrada. Diagnóstico da sessão. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | mensagem = SHA-256 do arquivo removido | Um `closecheck` que existia antes da sessão foi removido permanentemente (`SuppressStaleClosecheckWarning = true`). Diagnóstico da sessão. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | mensagem = caminho relativo; `Data["sha256"]` | Um caminho de exclusão da sessão foi recriado pelo OMSI durante uma sessão cujo processo havia sido iniciado; ele foi removido para restaurar a ausência original. Diagnóstico da sessão / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | mensagem = `manifest` ou `self` | Qual referência a validação do plugin permanente usa. Diagnóstico do plano. |
| `module.planned` | `SessionPlanner` | mensagem = id do módulo; `Data["module"]`, `Data["status"]`, `Data["activation"]` | O módulo está no plano. Diagnóstico do plano. |
| `module.disabled` | `SessionPlanner` | mensagem = id do módulo | Um módulo de conteúdo ou padrão foi mantido de fora por uma desativação (requisição ou arquivo de configurações). Diagnóstico do plano. |
| `session_profile.selected` | `SessionPlanner` | mensagem = id do perfil; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Procedência de uma sessão compilada a partir de um perfil de sessão. Diagnóstico do plano. |

Os nomes de eventos de runtime (`RuntimeEvent.Type`, não diagnósticos) estão listados em [ciclo de vida da sessão](../concepts/session-lifecycle.md#telemetry-events).
