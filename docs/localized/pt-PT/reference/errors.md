# Códigos de erro e de diagnóstico

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> Tradução da [página original em inglês](../../../reference/errors.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página é a referência normativa de todos os códigos em `PublicErrorCodes` (`src/OmsiLaunch.Api/PublicErrorCodes.cs`): 167 códigos de erro `OL_E_` e 4 avisos `OL_W_`, agrupados por categoria do catálogo, mais os códigos de diagnóstico informativos que não são erros. Para cada código indica onde o código atual o gera, o que significa, como chega ao utilizador (exceção lançada, campo de resultado, diagnóstico, resposta do plano de controlo ou envelope da CLI) e o que fazer. Os significados foram retirados dos pontos onde os códigos são gerados; quando um código está definido mas não tem nenhum caminho de geração atual, a página indica-o.

Páginas relacionadas: [API pública](public-api.md), [códigos de saída](exit-codes.md), [LaunchSpec](launchspec.md), [ciclo de vida da sessão](../concepts/session-lifecycle.md), [transações e recuperação](../concepts/transactions-and-recovery.md), [plano de controlo local](local-control.md), [controlo de runtime](runtime-control.md), [perfis de sessão](session-profiles.md), [plugin permanente](../concepts/permanent-plugin.md).

<a id="how-codes-reach-you"></a>
## Como os códigos chegam ao utilizador

| Superfície | Significado |
| --- | --- |
| Lançado | Uma exceção cuja `Message` começa pelo código (`InvalidOperationException`, `IOException`, `TimeoutException`, `InvalidDataException`, `FileNotFoundException`, `ArgumentException`, `SessionProfileException`). A CLI extrai o código da mensagem e mapeia-o para um código de saída (`CliProgram.Classify`). |
| Diagnóstico do plano | Um `LaunchDiagnostic` em `SessionPlan.Diagnostics`; qualquer código `OL_E_` torna `IsRunnable` falso (CLI: plano `NOT RUNNABLE`, saída 1). |
| Diagnóstico da sessão | Um `LaunchDiagnostic` em `SessionStatus.Diagnostics`; o estado da sessão é `Failed` (CLI: saída 1). `OL_E_START_SESSION`, `OL_E_PROCESS_SUPERVISION` e `OL_E_RESTORE_FAILED` envolvem um código interno na sua mensagem. |
| Resultado de runtime | `RuntimeCommandResult.ErrorCode` com `Succeeded = false`. |
| Detalhe de runtime | `RuntimeCommandResult.ErrorCode` é o código específico e `Values["detail"]` a mensagem (com `Values["exception"]`, o tipo .NET). Beta 4: um código tipado do lado do plugin chega inalterado à API e à CLI; apenas uma falha sem código catalogado passa a `OL_E_RUNTIME_OPERATION_FAILED`. |
| Resposta de controlo | `ErrorCode` de uma resposta do plano de controlo local (`LocalControlResponse`). |
| Envelope da CLI | `error.code` no envelope `--json`, ou `<code>: <message>` na consola; o código de saída é indicado. |
| Telemetria | Um nome de evento de runtime que o host mapeia para um diagnóstico da sessão. |

## Cli

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | Escapou uma `OperationCanceledException` (Ctrl+C ou uma espera de cliente cancelada). | Envelope da CLI, saída 7 | Repetir o comando. |
| `OL_E_INTERNAL` | `CliProgram.Classify` | Escapou uma exceção sem código `OL_E_`: JSON de `/spec` malformado, um membro obrigatório da especificação nulo, uma falha inesperada. | Envelope da CLI, saída 10 | Ler a mensagem e `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`; corrigir a entrada; comunicar o problema se não tiver explicação. |
| `OL_E_TIMEOUT` | `CliProgram.Classify`; `LocalControlPlane.TryRequestAsync` | Escapou uma `TimeoutException` sem código; ou o cliente de controlo local estava ligado a um proprietário que não respondeu dentro do timeout (o proprietário existe, pelo que isto não é comunicado como `OL_E_NO_ACTIVE_SESSION`). (Os timeouts da mailbox trazem, em vez disso, `OL_E_RUNTIME_REQUEST_TIMEOUT`.) | Envelope da CLI, resposta de controlo; saída 5 a partir de `Classify`, saída 7 para uma resposta de controlo | Repetir; verificar se o OMSI e o proprietário estão a responder. |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync` (`/silent`) | `OmsiLaunchW.exe` não está ao lado de `OmsiLaunch.exe`. | Envelope da CLI, saída 7 | Reinstalar o pacote. |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync` (`/silent`) | `Process.Start` de `OmsiLaunchW.exe` não devolveu nenhum processo. | Envelope da CLI, saída 7 | Verificar os ficheiros e as permissões do pacote; executar sem `/silent` para ver a falha. |

<a id="compatibility"></a>
## Compatibilidade

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry` em `plugin.build.invalid` | As capacidades de arranque foram resolvidas, mas o formulário Start em memória, ou um dos seus pontos de hook, não está no estado esperado quando o plugin arma o arranque sem interface (por exemplo, outro plugin já o modificou). | Diagnóstico da sessão (`Failed`) | Remover o plugin em conflito; ver [compatibilidade](compatibility.md). |
| `OL_E_CAPABILITY_UNRESOLVED` | Plugin (`plugin.capability.unresolved` no arranque; operações de runtime) | Uma capacidade nativa de que o pedido precisa não pôde ser resolvida e verificada nesta build do OMSI, pelo que está indisponível. No arranque, a sessão falha antes de qualquer hook e os ficheiros são restaurados; uma operação de runtime é rejeitada sem tocar na memória do OMSI. `Data["capability"]` (runtime) ou o valor `capabilities` da telemetria indica-a. | Diagnóstico da sessão (`Failed`); resultado de runtime | Ver [compatibilidade](compatibility.md): a capacidade não está disponível nesta build. |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner` (`omsi.omsi2` indisponível) | A instalação não é uma instalação do OMSI 2: `Omsi.exe` está em falta, não é uma imagem PE x86 de 32 bits, não tem versão do OMSI 2 (2.x), ou a raiz não tem o diretório `maps\` ou `Vehicles\`. O hash do executável não é um critério. | Diagnóstico do plano | Apontar o OmsiLaunch para a raiz de uma instalação do OMSI 2. |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner` (`runtime.current-windows-x64` indisponível); também lançado por `CurrentWindowsX64Platform.ValidateCurrent`, que o serviço não chama | Não é Windows 10 ou posterior, ou o sistema operativo ou o processo host não é x64. | Diagnóstico do plano (CLI: saída 3 quando lançado) | Executar em Windows 10 ou posterior de 64 bits. |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | Apenas `CurrentWindowsX64Platform.ValidateCurrent` | A arquitetura do sistema operativo ou do host não é x64. O serviço não chama `ValidateCurrent`; não existe caminho de geração atual. | Lançado (`PlatformNotSupportedException`) apenas por esse método | Ver o código-fonte `src/OmsiLaunch.Process/RuntimePlatform.cs`. |

<a id="content"></a>
## Conteúdo

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validação: NEW_MAP sem `EntrypointIdentity` e com `PresentedEntrypointIndex` não definido ou negativo. Planeador: o mapa não tem nenhum ponto de entrada com a identidade pedida ou com a etiqueta exata (com distinção de maiúsculas/minúsculas). | Diagnóstico do plano | Definir `PresentedEntrypointIndex` (`/entrypoint-index:<n>`) ou uma identidade de `/list:entrypoints /map:<id>`. |
| `OL_E_WEATHER_INVALID` | `SessionPlanner` (`StartWeatherResolver`) | A meteorologia de arranque não pode ser aplicada: uma predefinição que não é um ficheiro da pasta `weather` da instalação, ou um ICAO que não tem quatro letras ou dígitos. | Diagnóstico do plano | Usar uma predefinição instalada ou um código de estação válido. |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | O ponto de entrada pedido partilha a sua etiqueta com um registo anterior do mapa; o OMSI apresenta apenas o primeiro registo de cada etiqueta, pelo que não pode ser selecionado no arranque. | Diagnóstico do plano | Escolher o primeiro registo com essa etiqueta, ou outro ponto de entrada. |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner` (`world.presented-entrypoint` indisponível) | O mapa NEW_MAP foi resolvido, mas não há índice apresentado nem identidade. Acompanha sempre `OL_E_ENTRYPOINT_NOT_FOUND`. | Diagnóstico do plano | O mesmo. |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` não é um `Vehicles\...\*.hof` instalado. | Diagnóstico do plano | Usar uma identidade de `/list:hofs`. (Os campos do veículo do jogador são, de qualquer modo, não executáveis nesta build.) |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | Uma origem da [gestão de HOF](hof-management.md) (coleção, diretório, veículo ou ficheiro) não existe, não tem nenhum `.hof`, ou falta-lhe um ficheiro indicado em `Files`. | Diagnóstico do plano; falha de arranque | Corrigir a origem ou a lista de ficheiros. |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | Um ficheiro ou pasta de veículo de destino de HOF não está instalado. | Diagnóstico do plano; falha de arranque | Usar uma identidade de veículo de `/list:vehicles` ou uma pasta existente abaixo de `Vehicles\`. |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | Um caminho de HOF sai da instalação, um destino não está abaixo de `Vehicles\`, um nome de coleção não é um nome simples, ou é atravessada uma junction/ligação simbólica. | Diagnóstico do plano; falha de arranque | Usar caminhos relativos à instalação, sem `..` e sem ligações. |
| `OL_E_HOF_CONFLICT` | `HofPlanner`, `OmsiLaunchService` | Duas operações de HOF escrevem bytes diferentes no mesmo ficheiro, ou um espelhamento remove um ficheiro que outra operação escreve. | Diagnóstico do plano; falha de arranque | Reordenar, dividir ou remover as operações em conflito. |
| `OL_E_HOF_INVALID` | `HofPlanner` | Operação de HOF sem destinos ou sem origem, uma origem de ficheiro que não é `.hof`, um ficheiro de destino que não é `.bus`/`.ovh`, ou um limite de tamanho/contagem (8 MiB por ficheiro, 256 ficheiros por operação, 256 MiB por sessão). | Diagnóstico do plano; falha de arranque | Corrigir a operação. |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | Validação: NEW_MAP com `MapIdentity` não definido ou fora da forma `maps\<dir>\global.cfg`. Planeador: o mapa não está instalado. | Diagnóstico do plano | Usar uma identidade de `/list:maps`. |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | Escapou uma `FileNotFoundException`/`DirectoryNotFoundException` sem código, por exemplo `/list:repaints` com um `/vehicle-scope` desconhecido, ou `/list:entrypoints` com um `/map` desconhecido. | Envelope da CLI, saída 6 | Corrigir a identidade. |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` não é um item `.cti` do modelo (verificado apenas quando `Model` está definido). | Diagnóstico do plano | Usar uma identidade de `/list:repaints /vehicle-scope:<bus>`. |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | O mapa referenciado dentro do `.osn` selecionado não está instalado. | Diagnóstico do plano | Instalar o mapa ou escolher outra situação. |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`; `SessionPlanner` | SAVED_SITUATION sem `SituationIdentity`, ou o `.osn` não está instalado. | Diagnóstico do plano | Usar uma identidade de `/list:situations`. |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` não é um `Vehicles\...\*.bus` instalado. | Diagnóstico do plano | Usar uma identidade de `/list:vehicles`. |

<a id="installation"></a>
## Instalação

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`; `OmsiLaunchService.RecoverPendingAsync`; `FileConfigurationTransaction.RestorePendingAsync` | A concessão da instalação (`Local\OmsiLaunch.Installation.<hash>`) está detida por outro proprietário nesta sessão de início de sessão do Windows, ou um processo OMSI registado no journal (PID + hora de criação + caminho do executável; ou qualquer `Omsi.exe` da raiz, para um journal para além de `HandoffCreated` sem PID) ainda está vivo. | Início: diagnóstico da sessão através de `OL_E_START_SESSION`. Recuperação: lançado (`InvalidOperationException` / `IOException`). CLI: saída 7. | Parar o outro proprietário (`session stop`) ou esperar que o OMSI termine e depois repetir ou executar `/recover`. |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` está vazio. | Diagnóstico do plano | Indicar o diretório da instalação. |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner` (`transaction.exact-restore` indisponível); também `ValidateCurrent` | A raiz não existe, tem o atributo só de leitura, ou não tem diretório `plugins\`. | Diagnóstico do plano | Apontar para uma instalação real do OMSI com permissão de escrita. |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled` (plano e início) | Um ficheiro `plugins\OmsiLaunch.*` instalado difere do hash em `release-manifest.json` (ou do fecho de referência, sem manifesto). | Diagnóstico do plano (plano não executável, CLI: saída 1); diagnóstico da sessão através de `OL_E_START_SESSION` apenas se os ficheiros mudarem entre o planeamento e o início | Reinstalar o pacote OmsiLaunch para que `plugins\` e o manifesto coincidam. |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled` (plano e início) | O manifesto não tem entrada para um ficheiro obrigatório do plugin. | Diagnóstico do plano; diagnóstico da sessão através de `OL_E_START_SESSION` na mesma condição de corrida descrita acima | Reinstalar o pacote. |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled` (plano e início) | Um ficheiro `plugins\OmsiLaunch.*` obrigatório está ausente da instalação do OMSI, ou um ficheiro `plugins/` listado em `release-manifest.json` não está instalado. | Diagnóstico do plano; diagnóstico da sessão através de `OL_E_START_SESSION` na mesma condição de corrida descrita acima | Instalar o fecho do plugin permanente ([instalação](../getting-started/installation.md)). |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | Apenas `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` falso. Não é chamado pelo serviço; não existe caminho de geração atual. | Lançado apenas por esse método | Ver o código-fonte. |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` está vazio, não é JSON, não tem uma matriz `files`, ou tem uma entrada sem `path`/`sha256`, um hash que não tem 64 dígitos hexadecimais, um caminho absoluto, que contém `:`, um segmento vazio, `.` ou `..`, ou um caminho listado duas vezes (comparação sem distinção entre maiúsculas e minúsculas, `/` e `\` equivalentes). É aceite um BOM UTF-8. | Plano: envolvido em `OL_E_RUNTIME_ARTIFACT_MISSING`; início: através de `OL_E_START_SESSION` | Reinstalar o pacote. |

## InvalidArgument

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`; `CliInput.Parse`/`Classify` | Validação: `Date.Value`/`Time.Value` definido quando o modo não é `Explicit`. CLI: flag desconhecida, valor em falta, inteiro ou intervalo inválido, `/saved` combinado com `/map`/`/entrypoint`, rota de comando desconhecida, qualquer `ArgumentException`/`FormatException` sem código. | Diagnóstico do plano; envelope da CLI, saída 2 | Corrigir o argumento. |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch` (início) | Um valor de definição semântica está fora do intervalo, não é booleano, não pertence ao conjunto permitido, ou está malformado (`graphics.particles` precisa de quatro campos). Os valores não são validados no momento do planeamento. | Diagnóstico da sessão através de `OL_E_START_SESSION` | Usar um valor da [tabela de definições](launchspec.md#environmentspec). |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | A chave existe mas não permite escrita (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`). | Diagnóstico do plano; CLI: saída 2 | Remover a chave. |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`; `CliInput.BuildSpecAsync`; `BuildTransactionalOverlays` | A chave não está em `ConfigurationCatalog`. | Diagnóstico do plano; CLI: saída 2 | Usar uma chave do catálogo. |

## LaunchSpec

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | A raiz não é um objeto JSON, ou a desserialização não produziu nenhum registo. | Lançado (`InvalidDataException`), CLI: saída 2 | Corrigir o ficheiro ([LaunchSpec](launchspec.md)). |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | O ficheiro `/spec` não existe. | Lançado (`FileNotFoundException`), CLI: saída 6 | Verificar o caminho. |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | O ficheiro excede 1 MiB. | Lançado (`InvalidDataException`), CLI: saída 2 | Reduzir o ficheiro. |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | Um membro que não é uma propriedade pública do registo nessa posição; mensagem `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`. | Lançado (`InvalidDataException`), CLI: saída 2 | Remover ou mudar o nome do membro. |

## LocalControl

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | Handler do proprietário (`OwnerSession`) | O comando não é `session.status`, `session.events`, `session.stop` nem `runtime.execute` com um argumento `operation`. | Resposta de controlo; CLI: saída 7 | Usar um comando suportado. |
| `OL_E_CONTROL_FAILED` | Cliente da CLI (`ReportForwarded`, `CliEventWatch`) | O proprietário respondeu `Ok = false` sem código de erro. | Envelope da CLI, saída 7 | Ler a mensagem; verificar a consola/os diagnósticos do proprietário. |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | O handler do proprietário lançou uma exceção cuja mensagem não traz nenhum código `OL_E_` (por exemplo, a sessão já estava fechada), ou a resposta do handler não pôde ser serializada. | Resposta de controlo | Ler `session status`; reiniciar o proprietário se já não existir. |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane` (ambas as extremidades) | Prefixo de comprimento negativo ou superior a 64 KiB (incluindo um frame de pedido demasiado grande), frame vazio, JSON `null`, um pedido sem `Command`, ou JSON que não pôde ser descodificado. | Resposta de controlo / envelope da CLI | Usar o protocolo documentado ([plano de controlo local](local-control.md)). |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync` (cliente) | O próprio pedido serializado do cliente excede 64 KiB. É comunicado ao chamador; nada é enviado. | Resposta de controlo / envelope da CLI | Reduzir o pedido. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync` (proprietário) | A resposta do proprietário não cabe no frame de 64 KiB. O proprietário responde com este erro tipado em vez de descartar a resposta. `session.status` e `session.events` nunca o atingem: o seu histórico de eventos é aparado a partir dos mais antigos até caber. | Resposta de controlo / envelope da CLI | Repetir; para eventos, ler com mais frequência. |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`, `TryRequestBoundAsync` | O `ProtocolVersion` do pedido não é `0.1`; a resposta do proprietário não pôde ser descodificada ou estava vazia; o proprietário fechou a ligação sem responder ou a ligação quebrou depois de estabelecida; o proprietário não comunicou nenhum `SessionId`. | Resposta de controlo / envelope da CLI | Fazer coincidir as versões do cliente e do proprietário; ler `session status`. |
| `OL_E_CONTROL_SESSION_MISMATCH` | Handler do proprietário | `session.stop` ou `runtime.execute` sem um `session_id` igual ao da sessão ativa. | Resposta de controlo; CLI: saída 7 | Ler primeiro `session.status` e associar o pedido à sessão (a CLI fá-lo automaticamente). |

<a id="other"></a>
## Outros

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | O plano recebido tem `IsRunnable = false`, ou o novo planeamento no início não é executável (`Omsi.exe` alterado, conteúdo removido, fecho do plugin em falta); a mensagem lista os códigos `OL_E_` atuais. | Lançado (`InvalidOperationException`); CLI: saída 1 | Planear novamente e corrigir os diagnósticos listados. |

<a id="presentation"></a>
## Apresentação

Todos gerados por `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). No momento do planeamento são envolvidos em `OL_E_SESSION_PRESENTATION_INVALID` (a mensagem traz o código); no início surgem através de `OL_E_START_SESSION`.

| Código | Significado / causa típica | O que fazer |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | O ficheiro `.itx` está vazio, tem um número ímpar de linhas não vazias, ou uma linha de URL não é um URL `http`/`https` absoluto. | Usar pares de linhas URL/destino. |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath` (resolvido relativamente ao diretório de trabalho do processo) não existe. Lançado como `FileNotFoundException`. | Indicar um caminho `.itx` existente. |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` é `Override` sem `OverrideProfilePath`. CLI: saída 2 quando lançado. | Indicar `/internet-textures-profile:<file.itx>`. |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | Uma linha de destino é absoluta, contém `..`, começa por `\`, resolve-se para fora da instalação, não tem um componente `Texture\`, ou atravessa uma junção/ligação simbólica. | Usar destinos relativos `Texture\...`. |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` não existe. | Corrigir o diretório. |
| `OL_E_SPLASH_ASSET_MISSING` | `ENG.bmp` ou `<LANG>.bmp` está em falta no diretório de recursos, ou um `assets\splash\<LANG>.bmp` do pacote está em falta ao preencher `.omsilaunch\assets\splash`. | Fornecer os BMP / reinstalar o pacote. |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | Um BMP de splash não é um bitmap `BM` de 640×480 a 24 bits. | Converter a imagem. |

<a id="process"></a>
## Processo

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService` (caminhos de falha de início e de falha do supervisor) | Terminar o OMSI ou esperar por ele durante a limpeza após erro lançou uma exceção; segue-se a mensagem interna. | Diagnóstico da sessão (adicionado a uma sessão `Failed`) | Garantir que não resta nenhum `Omsi.exe` e depois executar `/recover` se houver um journal pendente. |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `GetProcessTimes` falhou logo após `CreateProcessW` (`Win32=<code>`); o processo é terminado. | Diagnóstico da sessão através de `OL_E_START_SESSION` | Repetir; verificar o antivírus/as permissões. |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | O OMSI terminou antes de `gameplay.entered` (falha abrupta, uma caixa de diálogo de erro do OMSI foi fechada, a janela foi fechada). | Diagnóstico da sessão (`Failed`); o restauro é executado | Consultar os registos do próprio OMSI e `logfile.txt`; ver `RuntimeEvents` para o último evento do plugin. |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` falhou (`Win32=<code>` na mensagem). | Diagnóstico da sessão através de `OL_E_START_SESSION` | Resolver o erro Win32 (ficheiro em falta, acesso negado, política). |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | O ciclo do supervisor lançou uma exceção (leitura de telemetria, espera/terminação do processo, escrita do journal); o OMSI é terminado e tenta-se o restauro. | Diagnóstico da sessão (`Failed`) | Ler a mensagem interna e o registo do host. |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` falhou (`Win32=<code>`). | Dentro das mensagens de `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | Terminar o OMSI manualmente e depois executar `/recover`. |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | `WaitForSingleObject` sobre o handle do processo falhou. | Dentro das mensagens de `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` | O mesmo. |

## Runtime

"Detalhe de runtime" significa o código específico em `ErrorCode`, com a mensagem em `Values["detail"]` e o tipo da exceção em `Values["exception"]`.

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` com `preset` quando `family` é 2 (exterior) ou 3 (mapa); as predefinições existem apenas para motorista (0) e passageiro (1). | Detalhe de runtime | Omitir `preset` ou usar a família 0/1. |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | Modo `Explicit` de `Date`/`Time` sem valor ou com componentes fora do intervalo. O nome é histórico; é um erro de validação no momento do planeamento. | Diagnóstico do plano | Corrigir o valor (e ter em conta que a data/hora explícita não é executável nesta build). |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: o caminho `.bus` não existe sob o diretório de trabalho do OMSI (verificado antes da chamada nativa para que o OMSI não possa substituir por uma alternativa). | Detalhe de runtime | Usar uma identidade de `vehicles list`/`/list:vehicles`. |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | idem | O MakeVehicle nativo alterou a coleção de veículos rodoviários em mais de um objeto. | Detalhe de runtime (`native_status`, contagens na mensagem) | Comunicar o problema; os objetos criados permanecem até ao fim da sessão. |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | idem | A coleção não mudou; o OMSI recusou o veículo silenciosamente. | Detalhe de runtime | Verificar o ficheiro `.bus`; experimentar outro modelo. |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | idem | Qualquer outro estado nativo diferente de zero. | Detalhe de runtime | Comunicar o problema com as contagens da mensagem. |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | A chamada perfilada PlaceRandomBus devolveu um estado de falha. | Detalhe de runtime | Repetir quando o jogo estiver estável; comunicar o problema. |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`; verificações do lado do plugin (`time.set` sem `hour`/`minute`/`second`; `camera.set` sem `family`/`field_of_view`; `camera.lock` sem uma `family` interpretável; operações de veículos/curvas) | Falta um argumento obrigatório ou está em branco. | Resultado de runtime (registo; CLI: saída 2) ou detalhe de runtime (plugin) | Fornecer o argumento ([controlo de runtime](runtime-control.md)). |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | O diretório/ficheiro de referência do fecho do plugin ou a ponte nativa indicados em `OmsiLaunchRuntimePaths` não podem ser carregados (pode envolver `OL_E_RELEASE_MANIFEST_INVALID`). | Diagnóstico do plano | Executar a partir de um pacote intacto. |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch` (`/runtime-write-batch`, harness INTERNAL) | O `time.read`/`camera.read` de base falhou, pelo que o teste de escrita foi ignorado. | Apenas artefacto do lote | Não se destina ao utilizador. |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` está vazio, tem mais de 240 caracteres, contém NUL ou `..`, não começa por `Vehicles\` ou não termina em `.bus`. | Detalhe de runtime | Indicar `Vehicles\<dir>\<file>.bus`. |
| `OL_E_RUNTIME_CHANNEL_BUSY` | nenhum (mantido por compatibilidade) | Já não é emitido. Builds anteriores geravam-no quando um pedido cancelado ficava no slot; agora todos os caminhos terminais de um pedido repõem o slot, e um pedido ou resposta remanescente encontrado no início de um novo pedido é limpo. | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | A mailbox foi descartada porque a sessão está a terminar. | Lançado (`InvalidOperationException`) | Nada; a sessão terminou. |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | O slot da mailbox continha um valor de estado que não é inativo, pedido nem respondido (corrupção). O slot é reposto e o erro é gerado; o pedido seguinte funciona normalmente. | Lançado (`InvalidDataException`) | Repetir; comunicar o problema se persistir. |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | O ponteiro do bloco de constantes do veículo é nulo. | Detalhe de runtime | O veículo não tem constantes; não há nada a fazer. |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` não está na tabela de constantes do veículo. | Detalhe de runtime | Listar primeiro as constantes. |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | O objeto criado pelo spawn tem uma VMT fora do intervalo da imagem do OMSI. | Detalhe de runtime | Comunicar o problema. |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | idem | O objeto criado não está na coleção de veículos rodoviários. | Detalhe de runtime | Comunicar o problema. |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | Dois pontos consecutivos da curva têm o mesmo X. | Detalhe de runtime | Problema de conteúdo na curva do veículo. |
| `OL_E_RUNTIME_CURVE_EMPTY` | idem | A curva não tem pontos. | Detalhe de runtime | O mesmo. |
| `OL_E_RUNTIME_CURVE_INVALID` | idem | Nenhum segmento da curva contém `x`. | Detalhe de runtime | Avaliar dentro do domínio da curva. |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | idem | `name` é desconhecido ou o seu ponteiro de função é nulo. | Detalhe de runtime | Listar primeiro as curvas. |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | O ponteiro da definição do veículo é nulo. | Detalhe de runtime | O handle refere-se a um veículo sem dados de definição. |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync` (modo proprietário) | `plugins\OmsiLaunch.Plugin.opl` ou `plugins\OmsiLaunch.Native.x86.dll` está em falta ao lado do executável. | Envelope da CLI, saída 7 | Reinstalar o pacote. |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `handle` em falta ou em branco para `road-vehicle.read`, `human.read`, `vehicle.variables.list`, `vehicle.string-variables.list`, `vehicle.constants.list`, `vehicle.curves.list` (normalmente o registo rejeita-os primeiro com `OL_E_RUNTIME_ARGUMENT_REQUIRED`). | Detalhe de runtime | Fornecer o handle. |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | O handle é desconhecido, o objeto saiu da coleção, a geração do endereço avançou, ou a impressão digital do objeto (VMT + identidade de definição/modelo) mudou porque o endereço foi reutilizado. Ponto cego residual: mesma classe e mesmo modelo recriados no mesmo endereço entre duas leituras da lista. | Detalhe de runtime | Executar novamente `road-vehicles.list`/`humans.list` e usar o novo handle. |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`; `CurrentRuntimeCommandMailbox.TryDispatch`; alternativa de `D3DRuntimeApi` | Invólucro genérico de falhas do lado do plugin; `Values["detail"]` contém a mensagem (muitas vezes um código mais específico) e `Values["exception"]` o tipo da exceção. Uma exceção que escapa de uma operação dentro do despachante da mailbox também é respondida com este código (sem valores), em vez de deixar o pedido sem resposta. | Resultado de runtime | Ler `detail`. |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | O plugin não tem implementação para uma operação que o registo permitiu (divergência de versões entre registo e plugin). | Detalhe de runtime | Reinstalar um pacote consistente. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | A operação não está em `PublicRuntimeOperationIds`, incluindo todas as operações `internal.*`. Verificado antes da procura da sessão. | Resultado de runtime; resposta de controlo; CLI: saída 2 | Usar um id de operação pública. |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` com `preset` quando não há veículo do jogador (as sessões headless não têm nenhum). Também comunicado como `code` do evento `camera.lock.degraded` quando a reaplicação falha. | Detalhe de runtime / evento de runtime | Bloquear sem predefinição, ou usar uma sessão com veículo do jogador. |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | Um resultado D3D bem-sucedido não tinha valores, ou tinha uma string de estado do dispositivo desconhecida. | Lançado (`OmsiRuntimeException`) | Fazer coincidir as versões do host e do plugin. |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | O slot contém uma resposta obsoleta com o mesmo id de pedido que o novo pedido. A resposta obsoleta é limpa antes de o erro ser gerado. | Lançado (`InvalidOperationException`) | Usar ids de pedido estritamente crescentes. |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | Nenhuma resposta dentro de `timeout`; o slot é reposto e uma resposta tardia é descartada. | Lançado (`TimeoutException`); CLI: saída 5 | Repetir com um timeout maior; verificar se o OMSI não está bloqueado (caixa de diálogo modal, carregamento). |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | O envelope da resposta está corrompido, tem um comprimento inválido (negativo, zero ou maior do que o slot), um id de sessão alheio ou um id de pedido diferente. O slot é reposto antes de o erro ser gerado, pelo que o pedido seguinte funciona normalmente. | Lançado (`InvalidDataException`) | Repetir; comunicar o problema se persistir. |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage` (plugin) | O `cursor` de uma operação de lista está malformado ou pertence a outro tipo de lista. | Detalhe de runtime | Passar o `next_cursor` da página anterior da mesma operação, ou omiti-lo. |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | O resultado serializado excede a mailbox de 64 KiB. Os resultados de listas paginadas são, em vez disso, encurtados até caberem e continuam com `next_cursor`; nenhum resultado de lista chega a este código. | Resultado de runtime | Usar uma operação mais restrita ou um `limit` menor. |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | O ponteiro da definição ou do estado do script do veículo é nulo. | Detalhe de runtime | O veículo não tem objetos de script. |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`; `CurrentRuntimeCommandStore`; mailbox do plugin | `RuntimeCommand.SessionId` difere do id de sessão do handle (lançado pelo host), ou um pedido chegou a um plugin associado a outra sessão (devolvido pelo plugin como resultado tipado). | Lançado (`InvalidOperationException`) / resultado de runtime | Construir o comando com `session.SessionId`. |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime` (`code` do evento `world.failed`) | A meteorologia de arranque não pôde ser aplicada depois de o mundo arrancar: o OMSI não tem nenhuma estação ICAO configurada para `/weather-real`, ou os seus objetos de meteorologia não estão disponíveis. A sessão falha. | Falha da sessão | Configurar uma estação no OMSI ou usar `/weather-icao`. |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | Uma chave de `weather.set` designa um valor que o OMSI deriva em cada ciclo meteorológico (humidade, fatores de luz, transparência das nuvens, flag de precipitação, solo molhado); nada é escrito. | Resultado de runtime | Definir a chave de origem indicada em `detail` (por exemplo `dew_point` em vez de `relative_humidity`). |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | Chave de `weather.set` desconhecida; nada é escrito. | Resultado de runtime | Usar uma chave listada em [capacidades](capabilities.md#weather). |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` não está na tabela de variáveis de string. | Detalhe de runtime | Listar primeiro as variáveis de string. |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | Um booleano meteorológico não é `true`/`false`/`1`/`0`. Atualmente inalcançável (ver acima). | Detalhe de runtime (definido) | Ver o código-fonte. |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`, `OmsiCameraWriter`, `OmsiCameraLockWriter`, `OmsiRuntimeReaders`, `OmsiWeatherWriter` | `time.set`: `hour` 0..23, `minute` 0..59, `second` 0..59.999; `camera.set`: `family` 0..3, `field_of_view` 10..170; `camera.lock`: `preset` não é um inteiro, `family` 0..3, `preset` 0..255; `road-vehicles.place-random`: `ai_type` 0..255, `group`/`type`/`tour`/`line` 0..65535 (`type` pode ser -1), `scheduled` 0..1; `vehicle.variable.set`: `value` não finito; `vehicle.curve.evaluate`: `x` não finito. | Detalhe de runtime | Usar um valor dentro do intervalo. |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` não está na tabela de variáveis numéricas. | Detalhe de runtime | Listar primeiro as variáveis. |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | O slot da variável ou o endereço do valor é nulo. | Detalhe de runtime | A variável não está materializada para este veículo. |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | Os escalares do relógio foram escritos, mas a chamada nativa perfilada SetTime devolveu falha. | Detalhe de runtime | Repetir; confirmar por leitura com `time.read`. |

## RuntimeD3D

Todos gerados por `CurrentRuntimeControl` (mapeamento `ThrowD3D` do estado nativo; `detail` traz a operação e o HRESULT, `native_status` o estado numérico). Superfície: resultado de runtime com o código em `ErrorCode`; `D3DRuntimeApi` relança-o como `OmsiRuntimeException`.

| Código | Estado nativo / causa | O que fazer |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: o dispositivo Direct3D foi perdido. | Esperar por `d3d.restored`; recriar as texturas (a geração mudou). |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: `width`, `height`, `level`, `x`, `y`, `format` ou `handle` em falta ou inválido (intervalos: width/height 1..4096, levels 0..16, level 0..15, x/y 0..4095). | Corrigir os argumentos. |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` não é Base64 válido ou excede 48 KiB. | Enviar retângulos mais pequenos. |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6, ou um nome de `format` desconhecido (válidos: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`). | Usar um formato listado. |
| `OL_E_D3D_NATIVE_CALL_FAILED` | Qualquer outro estado; o HRESULT está em `detail`. | Comunicar o problema com o HRESULT. |
| `OL_E_D3D_NOT_READY` | 7: o dispositivo não está pronto (antes do primeiro frame ou durante a paragem). | Repetir após `d3d.ready`. |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: está em curso uma reposição do dispositivo (reset). | Repetir após `d3d.restored`. |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: o handle da textura já foi libertado. | Não reutilizar handles libertados. |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: o handle pertence a uma geração anterior do dispositivo; ou a string do handle não é `d3dtex-<session>-<hex>` / é zero. | Recriar a textura. |

<a id="session"></a>
## Sessão

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`; `ApplyTelemetry` em `plugin.request.unsupported` | Plano: foram pedidos `LastMapState`, `EntrypointIdentity`, modo de data/hora/ano, modo meteorológico, campos do veículo do jogador ou documentos de entrada (`Requested capability unavailable: <name>`). Telemetria: o plugin rejeitou o handoff (não pode acontecer para um plano executável). | Diagnóstico do plano; diagnóstico da sessão | Remover o pedido não suportado ([limitações conhecidas](known-limitations.md)). |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry` em `headless.arm.failed` | O plugin não conseguiu armar no OMSI o hook de arranque headless de utilização única. | Diagnóstico da sessão (`Failed`) | Verificar a build; comunicar o problema. |
| `OL_E_NO_ACTIVE_SESSION` | Cliente da CLI (`ReportForwarded`, `CliEventWatch`) | Nenhum proprietário responde no pipe de controlo da instalação (não há sessão, ou o proprietário ainda está a iniciar/validar). | Envelope da CLI, saída 4 | Iniciar uma sessão, ou esperar até que esteja em `Running`. |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `StartupTimeoutSeconds` esgotou-se antes de `plugin.started` (o OMSI não carregou `plugins\OmsiLaunch.Plugin.opl`, ou está bloqueado antes da inicialização do plugin). | Diagnóstico da sessão (`Failed`) | Verificar o fecho do plugin, `plugins\OmsiLaunch.Plugin.opl` e o `logfile.txt` do OMSI. |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry` em `plugin.handoff.invalid` ou JSON de telemetria não interpretável | O plugin não conseguiu ler/verificar o handoff de arranque (versões 3 a 6, SHA-256), ou enviou telemetria inválida. | Diagnóstico da sessão (`Failed`) | Fazer coincidir as versões do host e do plugin (reinstalar o pacote). |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | Um proprietário já responde a `session.status` para esta instalação. | Envelope da CLI, saída 7 | Usar comandos de cliente (`session status`, `session stop`, comandos de runtime). |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | O estado da sessão não é `Running`. | Lançado (`InvalidOperationException`) | Chamar primeiro `WaitForAsync(session, SessionState.Running, ...)`. |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | Não foi possível construir o plano de splash/ITX; a mensagem traz o código de apresentação. | Diagnóstico do plano | Ver [Apresentação](#presentation). |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure` (caixa de diálogo do OmsiLaunchW) | Código alternativo mostrado quando um plano de lançamento não é executável ou a sessão não chegou ao jogo, e não existe nenhum diagnóstico `OL_E_`. | Apenas caixa de mensagem | Ler `.omsilaunch\diagnostics`. |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry` em `world.situation.failed` | O início nativo da situação guardada devolveu uma falha (`native_status` no evento). | Diagnóstico da sessão (`Failed`) | Verificar o `.osn` e o respetivo mapa. |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | O plugin iniciou, mas `Running` não foi alcançado dentro de `StartupTimeoutSeconds`. | Diagnóstico da sessão (`Failed`) | Aumentar `/startup-timeout` para mapas grandes; ver `RuntimeEvents` para o último evento do mundo. |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | Qualquer exceção no caminho de início; a mensagem é a mensagem interna (normalmente a começar pelo código interno). | Diagnóstico da sessão (`Failed`) | Agir de acordo com o código interno. |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry` em `world.failed` | O início nativo NEW_MAP devolveu uma falha (`native_status` no evento). | Diagnóstico da sessão (`Failed`) | Verificar o mapa, o índice do ponto de entrada e os registos do OMSI. |

## SessionProfile

Todos gerados por `SessionProfileCompiler` (`src/OmsiLaunch.Core/SessionProfiles.cs`) ou `CliInput`, lançados como `SessionProfileException` (uma `IOException` com `Code`), CLI: saída 2. Ver [perfis de sessão](session-profiles.md).

| Código | Significado / causa típica | O que fazer |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | O diretório `assets` de splash da predefinição ou o ficheiro `profile` de Internet Textures não existe dentro do pacote. | Adicionar o recurso. |
| `OL_E_SESSION_PROFILE_INVALID` | Violação estrutural ou de limite: mais de 256 KiB, não exatamente um mapeamento raiz, âncoras YAML, chave desconhecida, chave obrigatória em falta, valor não escalar onde é exigido um escalar, `id` diferente do nome do diretório, predefinições fora de 1..99 ou `index` duplicado, timeouts não positivos, modo meteorológico/de splash/de Internet Textures não suportado, data/hora diferente de `explicit`, YAML inválido, erros de interpretação de números/datas. | Corrigir o YAML de acordo com a mensagem. |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` não está vazio e não contém o mapa selecionado (NEW_MAP) nem o mapa da situação selecionada (SAVED_SITUATION). | Escolher um mundo compatível. |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` não existe. | Verificar o id. |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | Um argumento explícito da CLI visa um campo pertencente ao perfil/predefinição selecionado. | Retirar a flag ou escolher outra predefinição. |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | O id contém `\`, `/`, `:` ou `..`; ou um caminho de recurso é absoluto, sai do pacote ou atravessa uma junção/ligação simbólica. | Manter os caminhos dentro do pacote. |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` em falta, fora de 1..99, ou não declarado pelo perfil. | Usar um índice de predefinição declarado. |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` não é `omsilaunch.session-profile/v1`. | Usar o esquema suportado. |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | Uma chave de definição da predefinição é conhecida mas não permite escrita. | Remover a chave. |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | Uma chave de definição da predefinição não está no catálogo. | Usar uma chave do catálogo. |

<a id="transaction"></a>
## Transação

Ver [transações e recuperação](../concepts/transactions-and-recovery.md).

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | Um `closecheck` obsoleto ainda existe após `File.Delete`. | Diagnóstico da sessão através de `OL_E_START_SESSION` | Remover `<root>\closecheck` manualmente (permissões). |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | Um caminho que não existia antes da sessão contém agora conteúdo diferente do que a sessão aplicou; não é removido e o journal é mantido. | Lançado (`IOException`); dentro de `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION`; CLI: saída 8 | Inspecionar o ficheiro; removê-lo ou movê-lo e depois executar `/recover`. |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync` (journal anterior à impressão digital) | O journal não tem impressão digital do conteúdo aplicado para um caminho originalmente ausente que não é de eliminação, pelo que a propriedade não pode ser provada. No início da sessão a recuperação é adiada e repetida com os bytes planeados desta sessão; através de `RecoverPendingAsync` é lançado. | Lançado (`IOException`); CLI: saída 8 | Iniciar uma sessão com a mesma especificação (fornece os bytes), ou inspecionar e remover o ficheiro e depois executar `/recover`. |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | O SHA-256 de uma cópia de segurança não corresponde ao snapshot registado no journal; nada é escrito. | Lançado; dentro de `OL_E_RESTORE_FAILED`; CLI: saída 8 | Restaurar o ficheiro a partir de uma cópia de segurança própria; depois eliminar o journal apenas quando houver certeza. |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | Existem snapshots em memória, mas `journal.json` desapareceu (eliminado durante a sessão). | Lançado; dentro de `OL_E_RESTORE_FAILED` | Verificar manualmente os ficheiros da sessão. |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | `journal.json` ainda existe após a eliminação (o próprio restauro foi bem-sucedido e verificado). | Lançado; dentro de `OL_E_RESTORE_FAILED`; CLI: saída 8 | Eliminar `<root>\.omsilaunch\journal.json` (permissões) ou executar `/recover` novamente (idempotente). |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService` (falha de início / supervisor) | Não foi possível confirmar a saída do OMSI, pelo que os ficheiros não foram substituídos enquanto o OMSI ainda os pode estar a usar; o journal é mantido. | Diagnóstico da sessão (`Failed`) | Depois de `Omsi.exe` ter terminado, executar `/recover` (ou o próximo início recupera automaticamente). |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService` (falha de início / supervisor) | `RestoreAsync` lançou uma exceção; a mensagem traz o código interno; o journal é mantido. | Diagnóstico da sessão (`Failed`); CLI: saída 8 quando lançado a partir de `/recover` | Agir de acordo com o código interno e depois executar `/recover`. |

<a id="warning"></a>
## Aviso

| Código | Gerado por | Significado | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | O executável do OMSI 2 não tem disposição conhecida (a sua identidade de código não está no catálogo de builds). O plano continua executável: cada capacidade nativa é resolvida e verificada em processo, e uma que não possa ser verificada fica indisponível. | Diagnóstico do plano (a executabilidade não é afetada) | Nada a fazer; consultar `/plan` e `profiles` para as capacidades previstas. |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService` (supervisor) | Foi pedido `Behavior.CooperativeShutdown`, mas não foi possível pedir ao OMSI que saísse (a sua rotina de saída não está resolvida nesta build, ou o OMSI já está a fechar), ou não saiu dentro de `ShutdownTimeoutSeconds`: o OMSI foi terminado à força. O restauro corre como habitualmente. | diagnóstico da sessão | Nada: a sessão termina como numa terminação forçada. |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | Uma capacidade de que o arranque precisa não pôde ser resolvida a partir do ficheiro executável (por exemplo, um executável protegido por DRM cuja imagem carregada difere do ficheiro). É resolvida em processo no arranque. | Diagnóstico do plano (a executabilidade não é afetada) | Nada a fazer; um arranque falha com `OL_E_CAPABILITY_UNRESOLVED` se também não puder ser resolvida em processo. |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | Um módulo que aderiu à sessão por si próprio (ativação por ficheiro de definições, conteúdo ou predefinição) não pode ser carregado (`Data["code"]` indica porquê, `Data["activation"]` como aderiu). | Diagnóstico do plano (a executabilidade não é afetada) | Corrigir, desativar ou remover o módulo. |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`, `ModuleSettingsFile`, `ModuleSettings` | Uma entrada de `.omsilaunch\modules.ini` não pode ser usada (não é uma secção de módulo, está fora de uma secção, chave desconhecida, valor inválido; é usada a predefinição), ou foram pedidas definições para um módulo que a sessão não carrega. | Diagnóstico do plano (a executabilidade não é afetada) | Corrigir a entrada ou o pedido. |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | Um caminho de eliminação da sessão (destino ITX, `Texture\standard.ipr`, `closecheck`) não existia antes da sessão e existe agora, mas nenhum processo OMSI chegou a iniciar sob este journal, pelo que o ficheiro não pode ser um subproduto da sessão. É mantido e comunicado (mensagem = caminho relativo, `Data["sha256"]`); a transação conclui-se na mesma. | Diagnóstico da sessão / `RecoveryStatus.Diagnostics` (o estado não é afetado) | Inspecionar o ficheiro; removê-lo manualmente se não for desejado. |

<a id="module"></a>
## Módulo

Módulos de runtime (ver [módulos de runtime](../../../reference/runtime-modules.md)). No planeamento, um código é um diagnóstico do plano: para um módulo pedido, torna o plano não executável; para um módulo que aderiu por si próprio (ficheiro de definições, conteúdo, predefinição), o plano comunica `OL_W_MODULE_NOT_LOADED` em vez disso. Numa sessão, um código é um diagnóstico da sessão (a sessão continua, a menos que o código seja `OL_E_MODULE_SESSION_FAILED`) e, para uma operação de módulo, é o resultado de runtime.

| Código | Gerado por | Significado / causa típica | Superfície | O que fazer |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`, `ModuleCatalog` | `module.json` está em falta, não é um `omsilaunch.runtime-module/v1` válido, ou o seu `id` difere do nome do seu diretório. A mensagem indica o campo. | Diagnóstico do plano; `ListRuntimeModulesAsync` | Corrigir o manifesto. |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | Um id de módulo pedido não está instalado em `plugins\OmsiLaunch.Modules`. | Diagnóstico do plano | Verificar `/list:RuntimeModules`. |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`, `ModuleHost` | Um ficheiro fixado está em falta, difere do SHA-256 fixado ou é alcançado através de um ponto de reanálise (reparse point); ou, em processo, `module.json` mudou após o planeamento. | Diagnóstico do plano; diagnóstico da sessão | Reinstalar o módulo. |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | O intervalo `bridge_protocol` do módulo não inclui nenhum dos protocolos que esta build serve (1 e 2). | Diagnóstico do plano | Usar uma versão do módulo feita para este OmsiLaunch. |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`, `ModuleHost` | Uma capacidade exigida é desconhecida desta build, está indisponível nesta instalação, ou não foi resolvida em processo. | Diagnóstico do plano; diagnóstico da sessão | Ver [capacidades](capabilities.md) e `profiles`. |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`, `ModuleHost` | A sessão não executa o mapa associado ao módulo, ou um ficheiro associado difere do SHA-256 fixado. | Diagnóstico do plano; diagnóstico da sessão | Iniciar o mapa associado com o conteúdo associado. |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | O módulo pediu para iniciar um ficheiro que não é um dos seus componentes auxiliares. | Resultado de runtime dentro do módulo | Defeito do módulo. |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`, `ModuleSessionPrograms` | O componente gerido não pôde ser carregado ou o seu tipo de entrada criado; ou o programa de sessão do módulo não pôde ser iniciado ou terminou antes de estar pronto. | Diagnóstico da sessão | Verificar a build do módulo (compatível com x86, o tipo de entrada implementa `IRuntimeModule`). |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`, `GameplayReady` ou `Stop` lançou uma exceção. | Diagnóstico da sessão | Defeito do módulo; aplicou-se o seu `failure_policy`. |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`, `ModuleSessionPrograms` | Uma fase ou uma operação regressou depois do seu orçamento (`timeouts`, `timeout_ms`), ou um programa de sessão não ficou pronto dentro de `prepare_ms`. | Diagnóstico da sessão; resultado de runtime | Defeito do módulo; aplicou-se o seu `failure_policy`. |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | O módulo foi desativado ou falhou anteriormente na sessão. | Resultado de runtime | Ver os diagnósticos da sessão para a primeira falha. |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | A sessão não carregou este módulo, ou ele ainda não está pronto (antes da jogabilidade). | Resultado de runtime | Pedir o módulo no arranque; esperar por `Running`. |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`, `ModuleHost` | O módulo não declara essa operação. | Resultado de runtime | Verificar as `operations` do módulo. |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | A operação falhou sem um código catalogado; o código próprio de um módulo está em `Values["module_code"]`. | Resultado de runtime | Específico do módulo. |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | Após a paragem ou falha, um recurso rastreado ainda estava ativo (um processo auxiliar que não terminou, uma thread em segundo plano que não terminou) ou a sua libertação lançou uma exceção. | Diagnóstico da sessão | Comunicar o módulo; o processo termina com o OMSI em qualquer caso. |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | Um módulo cujo `failure_policy` é `fail-session` falhou; a sessão é parada e restaurada. | Diagnóstico da sessão (estado `Failed`) | Ver o código de módulo anterior. |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`, `ModuleSettings`, CLI | Uma definição de módulo pedida é inválida ou não é declarada pelo módulo, um módulo está simultaneamente ativado e desativado, o nome de uma definição não é `<module-id>.<key>`, ou uma flag de módulo é declarada por vários módulos. | Diagnóstico do plano (não executável); erro da CLI | Corrigir o pedido; `/list:RuntimeModules` mostra as definições de cada módulo. |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`, `ModuleHost` | Um plugin do OMSI que o módulo lista em `conflicts.plugins` está ativado (`plugins\*.opl`), ou carregado no OMSI. O motivo indica o `.opl`. | Diagnóstico do plano; diagnóstico da sessão | Desativar ou remover esse plugin. O OmsiLaunch nunca o altera. |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | reservado | Reservado; um componente nativo recebe, em vez disso, os resultados numéricos de hook descritos em [módulos de runtime](../../../reference/runtime-modules.md#native-components-and-hooks). | nenhuma | nenhuma |

<a id="non-error-diagnostic-codes"></a>
## Códigos de diagnóstico que não são erros

| Código | Emitido por | Mensagem / dados | Significado |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | mensagem = PID; `Data["thread_id"]`, `Data["creation_utc"]` (ISO 8601) | `Omsi.exe` foi criado e a sua identidade foi registada. Diagnóstico da sessão. |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | mensagem = SHA-256 do ficheiro removido | Um `closecheck` que existia antes da sessão foi removido permanentemente (`SuppressStaleClosecheckWarning = true`). Diagnóstico da sessão. |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | mensagem = caminho relativo; `Data["sha256"]` | Um caminho de eliminação da sessão foi recriado pelo OMSI durante uma sessão cujo processo tinha iniciado; foi removido para restaurar a ausência original. Diagnóstico da sessão / `RecoveryStatus.Diagnostics`. |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | mensagem = `manifest` ou `self` | Que referência a validação do plugin permanente usa. Diagnóstico do plano. |
| `module.planned` | `SessionPlanner` | mensagem = id do módulo; `Data["module"]`, `Data["status"]`, `Data["activation"]` | O módulo está no plano. Diagnóstico do plano. |
| `module.disabled` | `SessionPlanner` | mensagem = id do módulo | Um módulo de conteúdo ou predefinido foi mantido de fora por uma desativação (pedido ou ficheiro de definições). Diagnóstico do plano. |
| `session_profile.selected` | `SessionPlanner` | mensagem = id do perfil; `Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | Proveniência de uma sessão compilada a partir de um perfil de sessão. Diagnóstico do plano. |

Os nomes dos eventos de runtime (`RuntimeEvent.Type`, que não são diagnósticos) estão listados no [ciclo de vida da sessão](../concepts/session-lifecycle.md#telemetry-events).
