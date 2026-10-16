# O plugin permanente

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Tradução da [página original em inglês](../../../concepts/permanent-plugin.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O OmsiLaunch controla o OMSI de dentro do processo do OMSI por meio de um plugin instalado uma única vez, em `plugins\OmsiLaunch.*`, como parte do produto. Ele nunca é preparado, copiado, incluído em snapshot, restaurado nem removido por uma sessão. Esta página explica o que o conjunto de arquivos do plugin (closure) contém, como o OMSI o carrega, como o host o valida antes de cada inicialização, como host e plugin se comunicam (handoff, telemetria, mailbox de runtime) e o que o plugin faz quando o OMSI é iniciado sem o OmsiLaunch. Fontes: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, os três stores de memória compartilhada), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` e `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## O conjunto de arquivos (9 arquivos)

| Arquivo em `plugins\` | Função |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Descritor de plugin do OMSI. Seu conteúdo é `[dll]` seguido de `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Shim nativo x86 de exports gerado pelo DNNE 2.0.6. Exporta a ABI de plugins do OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) e hospeda o runtime .NET. Contém o recurso de versão do produto. |
| `OmsiLaunch.Plugin.dll` | Plugin gerenciado (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Manifesto de dependências .NET do plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Configuração do runtime .NET (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Formatos de transmissão e records públicos compartilhados com o host. |
| `OmsiLaunch.Builds.Omsi23004.dll` | O perfil de build: fingerprint do executável, globais, layouts de objetos, endereços de métodos. |
| `OmsiLaunch.Interop.dll` | Leitores e gravadores de memória dentro do processo, construídos sobre o perfil. |
| `OmsiLaunch.Native.x86.dll` | Ponte nativa (C++): validação do build, hook de início headless, aplicação de horário, MakeVehicle, PlaceRandomBus, supressão das texturas da internet, acesso ao dispositivo D3D9. |

`RuntimeArtifactSet.Load` deriva essa lista do próprio diretório `plugins\` do controlador: os quatro arquivos nomeados (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), todo `OmsiLaunch.*.dll` desse diretório exceto `OmsiLaunch.PluginNE.dll`, e a ponte nativa. `OmsiLaunch.Plugin.dll` precisa estar entre eles. DLLs arbitrárias nunca são levadas para dentro do OMSI. O empacotador de release (`tools/New-ReleasePackage.ps1`) grava exatamente os nove arquivos acima.

<a id="how-omsi-loads-it"></a>
## Como o OMSI o carrega

1. O OMSI enumera `plugins\*.opl` e carrega a DLL nomeada em `OmsiLaunch.Plugin.opl`: `OmsiLaunch.PluginNE.dll`.
2. O shim DNNE inicia o runtime .NET 6 x86 descrito por `OmsiLaunch.Plugin.runtimeconfig.json` dentro do processo do OMSI e resolve os exports gerenciados em `OmsiLaunch.Plugin.dll`.
3. O OMSI chama `PluginStart`. O OMSI pode chamá-lo mais de uma vez durante a inicialização; somente a primeira chamada é considerada (guarda `Interlocked.Exchange`), porque um início bem-sucedido é dono de um hook nativo de uso único. Chamadas posteriores retornam imediatamente.
4. `PluginStart` verifica primeiro `OMSILAUNCH_INTERNET_TEXTURES_MODE`: quando é `Disabled`, a supressão nativa do downloader é aplicada (`internet-textures.suppressed` ou `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` lê o handoff (abaixo), valida o build dentro do processo, arma o hook de início headless, abre o mailbox de runtime e agenda o início do mundo no thread de UI do OMSI por meio de um callback de `SetTimer`. Nenhum comando de runtime é jamais executado em um thread de trabalho de IPC; tudo é executado no callback do timer, no thread de UI original do OMSI.
6. `PluginFinalize` elimina o timer, desliga o runtime (`NativeD3DShutdown`) e restaura o patch das texturas da internet.

Os exports `AccessVariable`, `AccessTrigger`, `AccessStringVariable` e `AccessSystemVariable` são vazios; o OmsiLaunch não usa o canal de plugins de variáveis de script do OMSI.

<a id="integrity-validation-before-every-start"></a>
## Validação de integridade antes de cada inicialização

`RuntimeArtifactSet.ValidateInstalled` é executado durante `StartSessionAsync` (após a recuperação antecipada, antes de a transação ser preparada) e durante `PlanSessionAsync` (somente presença, via `LoadArtifacts`). O diagnóstico de plano `plugin.integrity.reference` informa qual referência foi usada.

| Situação | Referência | Verificação por arquivo | Erros |
| --- | --- | --- | --- |
| `release-manifest.json` presente ao lado de `OmsiLaunch.exe` (pacote instalado) | `manifest` | O arquivo instalado precisa existir e seu SHA-256 precisa ser igual à entrada do manifesto para `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (o manifesto não tem entrada para um arquivo obrigatório), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Sem manifesto (layout de desenvolvimento) | `self` | Presença mais autoconsistência: o hash do arquivo instalado precisa ser igual ao do arquivo no próprio diretório `plugins\` do controlador | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifesto ilegível ou malformado (array `files` ausente, entrada sem `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Conjunto de arquivos incompleto no diretório do controlador | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (plano não apto para execução); a CLI também relata `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` quando `plugins\OmsiLaunch.Plugin.opl` ou `OmsiLaunch.Native.x86.dll` está ausente |

Somente as entradas `plugins/` do manifesto são consumidas; o manifesto é dado, nunca política. O manifesto é gerado pelo empacotador de release com o SHA-256 de cada arquivo preparado. Quando o controlador é executado a partir da raiz do OMSI (o layout de release), origem e destino são o mesmo diretório; sem um manifesto, a verificação se reduz, portanto, a presença e autoconsistência, e é por isso que os pacotes publicados sempre trazem `release-manifest.json`. Correção para uma divergência: reinstale o pacote para que `plugins\` e `release-manifest.json` estejam de acordo.

Antes de modificar qualquer coisa, o plugin resolve e verifica cada destino nativo do catálogo de builds contra a imagem carregada e entrega a tabela verificada ao `OmsiLaunch.Native.x86.dll`, que não contém nenhum endereço do OMSI (telemetria `native.capabilities`). Um início cuja capacidade `boot.headless-start` ou de modo de mundo está indisponível falha com `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); um formulário Start ativo em um estado inesperado falha com `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Veja [compatibilidade](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Do host para o plugin: variáveis de ambiente

`CreateProcessW` inicia o `Omsi.exe` com o ambiente do processo pai mais:

| Variável | Valor | Consumidor |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID da sessão (formato `D`) | `CurrentRuntimeControl` marca os handles D3D com ele (formato `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` abre este mapeamento como somente leitura |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` ou `Override` | `PluginStart` (somente `Disabled` tem efeito dentro do processo) |

Os três mapeamentos são criados pelo host antes de o processo iniciar (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) e descartados quando a tarefa de ciclo de vida da sessão termina. São objetos nomeados do kernel com a DACL padrão do usuário que fez a inicialização; qualquer processo do mesmo usuário pode abri-los (modelo de confiança aceito para o mesmo usuário, veja [limitações conhecidas](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## O handoff de inicialização

Um registro mapeado em memória, somente leitura e de layout fixo (`StartupHandoffWire`, magic `OLSH`, versão 4; a versão 3 ainda é aceita pelo leitor). O cabeçalho de 64 bytes contém o magic, a versão, o tamanho do cabeçalho, o tamanho total, o GUID da sessão, o tamanho do payload e o SHA-256 do payload. O payload contém `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 com prefixo de comprimento), `PresentedEntrypointIndex`, `WorldMode`, flags (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` e `TimeMode`. O plugin recalcula o hash do payload e rejeita qualquer divergência (`plugin.handoff.invalid`, erro do host `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Um mapeamento maior que 1 MiB ou com tamanho inconsistente é rejeitado da mesma forma.

O plugin aceita um handoff somente quando `WorldMode` é `NewMap` ou `SavedSituation`, `HeadlessStart` está definido, `PlayerVehicleEnabled` está limpo, ambos os modos de data e hora são `Unset` e uma situação salva indica seu `.osn`. Qualquer outra coisa é `plugin.request.unsupported` (erro do host `OL_E_CAPABILITY_UNAVAILABLE`). O host sempre define `HeadlessStart`.

<a id="telemetry-slot"></a>
## Slot de telemetria

`OmsiLaunch.Telemetry.<session>` é um anel de eventos (Beta 4, `TelemetryRingWire`): um cabeçalho de 64 bytes (magic `OLTR`, versão 1, quantidade de slots, tamanho do slot, sequência confirmada) e 64 slots numerados de 2048 bytes, cada um contendo um evento JSON UTF-8 `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` é o relógio monotônico de sessão do plugin). O plugin é o único escritor: ele invalida um slot, o grava, confirma a sequência do slot e depois publica a sequência do anel. O host lê todo evento confirmado desde sua última leitura, em ordem, a cada 100 ms; um slot sobrescrito enquanto estava sendo copiado é contado como perdido, nunca entregue corrompido. Um host que fica mais de 64 eventos para trás recebe o evento de runtime `telemetry.lost` com a contagem exata. Um evento maior que um slot é substituído por `telemetry.oversized`, que o nomeia. Os eventos são anexados a `SessionStatus.RuntimeEvents` (limitado aos últimos 256) e conduzem o ciclo de vida semântico (`plugin.started`, `world.starting`, `gameplay.entered`, eventos de módulo e de falha).

<a id="runtime-command-mailbox"></a>
## Mailbox de comandos de runtime

`OmsiLaunch.Runtime.<session>` é um mailbox de 64 KiB com uma única requisição em andamento por vez: `state` (int32 em 0: 0 ocioso, 1 requisitado, 2 respondido), `length` (int32 em 4), envelope em 8. Os envelopes são registros `RuntimeCommandWire` (magic `OLRC`, versão 1, cabeçalho de 72 bytes com tipo, comprimento total, GUID da sessão, id da requisição, comprimento do payload e SHA-256 do payload JSON UTF-8). O plugin consulta o mailbox a partir do timer do seu thread de UI (50 ms depois que o mundo foi carregado), executa o comando nesse thread e publica a resposta somente se o slot ainda contiver o mesmo id de requisição; uma requisição que o host abandonou por timeout nunca é respondida. Uma resposta grande demais é substituída por um erro tipado `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Detalhes e timeouts estão em [controle de runtime](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Política de busca de DLLs

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` e os assemblies da CLI declaram `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. As importações nativas (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) são resolvidas somente a partir do próprio diretório do assembly (`plugins\`) ou do diretório de sistema do Windows; a raiz do OMSI e o `PATH` nunca são sondados. `OmsiLaunch.Native.x86.dll` é, portanto, carregado de `plugins\` e de nenhum outro lugar.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Quando o OMSI é iniciado sem o OmsiLaunch

Como o conjunto de arquivos é permanente, o OMSI carrega `OmsiLaunch.PluginNE.dll` em toda inicialização, inclusive inicializações pela Steam ou pela área de trabalho. Nesse caso:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` está ausente, portanto nenhum patch do downloader é aplicado.
- `OMSILAUNCH_HANDOFF_NAME` está ausente, portanto `PluginRuntime.Start` emite `plugin.handoff.invalid` e retorna `false`. `CurrentTelemetrySink.Emit` retorna imediatamente quando `OMSILAUNCH_TELEMETRY_NAME` não está definido, portanto nada é gravado em lugar nenhum.
- Nenhuma validação de build, nenhum hook nativo, nenhum mailbox, nenhum timer. O plugin permanece carregado, mas inerte; o OMSI se comporta como se o plugin não estivesse lá.
- `PluginFinalize` na saída do OMSI chama a rotina nativa de restauração e `NativeD3DShutdown`, ambas sem efeito quando nada foi instalado.

O `.opl` não precisa, portanto, ser removido para executar o OMSI normalmente.

<a id="non-interference-with-third-party-plugins"></a>
## Não interferência com plugins de terceiros

As sessões nunca enumeram, calculam hash, copiam, removem nem restauram outros arquivos em `plugins\`. O plugin não usa o canal `AccessVariable` do OMSI e não toca no estado de outros plugins. Os únicos patches dentro do processo são o hook de início headless perfilado (um redirecionamento de VMT de uso único armado para a sessão), a supressão opcional das texturas da internet (restaurada em `PluginFinalize`) e a interceptação de `Reset` do dispositivo D3D9, usada para acompanhar o ciclo de vida das texturas.

<a id="difference-from-omsihook"></a>
## Diferença em relação ao OmsiHook

O OmsiLaunch **não tem dependência de runtime** do OmsiHook nem de qualquer binário do OmsiHook: os únicos pacotes referenciados são `DNNE` 2.0.6 e `YamlDotNet` 15.1.2, e não há `using OmsiHook` nem P/Invoke para DLLs do OmsiHook em nenhum ponto do produto. O que o OmsiLaunch compartilha com o OmsiHook é conhecimento derivado: layouts de objetos e vários wrappers de leitura foram reconciliados a partir do checkout fixado do OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) com o executável exato `Omsi23004_692EBFBF`. A atribuição e os termos de licença estão em `THIRD-PARTY-NOTICES.md` e a matriz de reutilização por arquivo em `third_party/OMSIHOOK-REUSE-MATRIX.md`. O OmsiHook injeta um processo separado e expõe ponteiros brutos; o OmsiLaunch é executado dentro do processo, expõe somente handles opacos com escopo de sessão e remove qualquer endereço nativo dos resultados públicos (veja [capacidades](../reference/capabilities.md)).
