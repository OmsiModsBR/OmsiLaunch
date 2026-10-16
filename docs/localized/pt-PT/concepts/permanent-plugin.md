# O plugin permanente

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Tradução da [página original em inglês](../../../concepts/permanent-plugin.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

O OmsiLaunch controla o OMSI a partir do interior do processo do OMSI através de um plugin que é instalado uma única vez, em `plugins\OmsiLaunch.*`, como parte do produto. Nunca é preparado, copiado, incluído num snapshot, restaurado nem removido por uma sessão. Esta página explica o que o conjunto de ficheiros contém, como o OMSI o carrega, como o anfitrião o valida antes de cada arranque, como o anfitrião e o plugin comunicam (handoff, telemetria, mailbox de runtime) e o que o plugin faz quando o OMSI é iniciado sem o OmsiLaunch. Fontes: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, os três armazenamentos de memória partilhada), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` e `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## O conjunto de ficheiros (9 ficheiros)

| Ficheiro em `plugins\` | Função |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Descritor de plugin do OMSI. O seu conteúdo é `[dll]` seguido de `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Shim nativo x86 de exportações gerado pelo DNNE 2.0.6. Exporta a ABI de plugins do OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) e aloja o runtime .NET. Contém o recurso de versão do produto. |
| `OmsiLaunch.Plugin.dll` | Plugin gerido (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | Manifesto de dependências .NET do plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | Configuração do runtime .NET (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Formatos de comunicação e registos públicos partilhados com o anfitrião. |
| `OmsiLaunch.Builds.Omsi23004.dll` | O perfil de build: impressão digital do executável, variáveis globais, estruturas de objetos, endereços de métodos. |
| `OmsiLaunch.Interop.dll` | Leitores e escritores de memória dentro do processo, construídos sobre o perfil. |
| `OmsiLaunch.Native.x86.dll` | Ponte nativa (C++): validação da build, hook de arranque headless, aplicação da hora, MakeVehicle, PlaceRandomBus, supressão das texturas da Internet, acesso ao dispositivo D3D9. |

`RuntimeArtifactSet.Load` deriva esta lista do próprio diretório `plugins\` do controlador: os quatro ficheiros com nome fixo (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), todos os `OmsiLaunch.*.dll` desse diretório exceto `OmsiLaunch.PluginNE.dll`, e a ponte nativa. `OmsiLaunch.Plugin.dll` tem de estar entre eles. DLLs arbitrárias nunca são arrastadas para o OMSI. O empacotador da versão (`tools/New-ReleasePackage.ps1`) escreve exatamente os nove ficheiros acima.

<a id="how-omsi-loads-it"></a>
## Como o OMSI o carrega

1. O OMSI enumera `plugins\*.opl` e carrega a DLL indicada em `OmsiLaunch.Plugin.opl`: `OmsiLaunch.PluginNE.dll`.
2. O shim DNNE inicia o runtime .NET 6 x86 descrito por `OmsiLaunch.Plugin.runtimeconfig.json` dentro do processo do OMSI e resolve as exportações geridas em `OmsiLaunch.Plugin.dll`.
3. O OMSI chama `PluginStart`. O OMSI pode chamá-lo mais de uma vez durante o arranque; só a primeira chamada é respeitada (proteção `Interlocked.Exchange`), porque um arranque bem-sucedido detém um hook nativo de utilização única. As chamadas posteriores retornam imediatamente.
4. `PluginStart` verifica primeiro `OMSILAUNCH_INTERNET_TEXTURES_MODE`: quando é `Disabled`, é aplicada a supressão do downloader nativo (`internet-textures.suppressed` ou `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` lê o handoff (abaixo), valida a build dentro do processo, arma o hook de arranque headless, abre a mailbox de runtime e agenda o arranque do mundo na thread de UI do OMSI através de um callback de `SetTimer`. Nenhum comando de runtime é alguma vez executado numa thread de trabalho de IPC; tudo é executado no callback do temporizador, na thread de UI original do OMSI.
6. `PluginFinalize` elimina o temporizador, encerra o runtime (`NativeD3DShutdown`) e restaura o patch das texturas da Internet.

As exportações `AccessVariable`, `AccessTrigger`, `AccessStringVariable` e `AccessSystemVariable` estão vazias; o OmsiLaunch não usa o canal de plugins de variáveis de script do OMSI.

<a id="integrity-validation-before-every-start"></a>
## Validação da integridade antes de cada arranque

`RuntimeArtifactSet.ValidateInstalled` é executado durante `StartSessionAsync` (depois da recuperação antecipada, antes de a transação ser preparada) e durante `PlanSessionAsync` (apenas presença, através de `LoadArtifacts`). O diagnóstico do plano `plugin.integrity.reference` indica que referência foi usada.

| Situação | Referência | Verificação por ficheiro | Erros |
| --- | --- | --- | --- |
| `release-manifest.json` presente ao lado de `OmsiLaunch.exe` (pacote instalado) | `manifest` | O ficheiro instalado tem de existir e o seu SHA-256 tem de ser igual à entrada do manifesto para `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (o manifesto não tem entrada para um ficheiro obrigatório), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Sem manifesto (estrutura de desenvolvimento) | `self` | Presença mais autoconsistência: o hash do ficheiro instalado tem de ser igual ao do ficheiro no próprio diretório `plugins\` do controlador | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifesto ilegível ou malformado (array `files` em falta, entrada sem `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Conjunto incompleto no diretório do controlador | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (plano não executável); a CLI comunica adicionalmente `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` quando `plugins\OmsiLaunch.Plugin.opl` ou `OmsiLaunch.Native.x86.dll` está ausente |

Só são consumidas as entradas `plugins/` do manifesto; o manifesto é um dado, nunca uma política. O manifesto é gerado pelo empacotador da versão com o SHA-256 de cada ficheiro preparado. Quando o controlador é executado a partir da raiz do OMSI (a estrutura da versão), a origem e o destino são o mesmo diretório; sem manifesto, a verificação reduz-se portanto a presença e autoconsistência, e é por isso que os pacotes publicados incluem sempre `release-manifest.json`. Correção para uma discrepância: reinstalar o pacote para que `plugins\` e `release-manifest.json` estejam em concordância.

Antes de modificar o que quer que seja, o plugin resolve e verifica todos os destinos nativos do catálogo de builds face à imagem carregada e entrega a tabela verificada a `OmsiLaunch.Native.x86.dll`, que não contém nenhum endereço do OMSI (telemetria `native.capabilities`). Um arranque cuja capacidade `boot.headless-start` ou de modo de mundo esteja indisponível falha com `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); um formulário Start em memória num estado inesperado falha com `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Ver [compatibilidade](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Do anfitrião para o plugin: variáveis de ambiente

`CreateProcessW` inicia `Omsi.exe` com o ambiente do processo pai mais:

| Variável | Valor | Consumidor |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | GUID da sessão (formato `D`) | `CurrentRuntimeControl` marca os handles D3D com ele (formato `N`) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` abre este mapeamento só de leitura |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` ou `Override` | `PluginStart` (só `Disabled` tem efeito dentro do processo) |

Os três mapeamentos são criados pelo anfitrião antes de o processo ser iniciado (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) e libertados quando a tarefa do ciclo de vida da sessão termina. São objetos do kernel com nome, com a DACL predefinida do utilizador que os lança; qualquer processo do mesmo utilizador os pode abrir (modelo de confiança aceite para o mesmo utilizador, ver [limitações conhecidas](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## O handoff de arranque

Um registo mapeado em memória, só de leitura e com estrutura fixa (`StartupHandoffWire`, magic `OLSH`, versão 4; a versão 3 continua a ser aceite pelo leitor). O cabeçalho de 64 bytes contém o magic, a versão, o tamanho do cabeçalho, o tamanho total, o GUID da sessão, o tamanho do payload e o SHA-256 do payload. O payload contém `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 com prefixo de comprimento), `PresentedEntrypointIndex`, `WorldMode`, flags (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` e `TimeMode`. O plugin volta a calcular o hash do payload e rejeita qualquer discrepância (`plugin.handoff.invalid`, erro do anfitrião `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Um mapeamento maior do que 1 MiB ou com um tamanho inconsistente é rejeitado da mesma forma.

O plugin só aceita um handoff quando `WorldMode` é `NewMap` ou `SavedSituation`, `HeadlessStart` está definido, `PlayerVehicleEnabled` não está definido, os modos de data e de hora são ambos `Unset` e uma situação guardada indica o seu `.osn`. Tudo o resto é `plugin.request.unsupported` (erro do anfitrião `OL_E_CAPABILITY_UNAVAILABLE`). O anfitrião define sempre `HeadlessStart`.

<a id="telemetry-slot"></a>
## Slot de telemetria

`OmsiLaunch.Telemetry.<session>` é um anel de eventos (Beta 4, `TelemetryRingWire`): um cabeçalho de 64 bytes (número mágico `OLTR`, versão 1, número de slots, tamanho do slot, sequência confirmada) e 64 slots numerados de 2048 bytes, cada um com um evento JSON UTF-8 `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` é o relógio de sessão monotónico do plugin). O plugin é o único escritor: invalida um slot, escreve-o, confirma a sequência do slot e depois publica a sequência do anel. O anfitrião lê todos os eventos confirmados desde a sua última leitura, por ordem, a cada 100 ms; um slot reescrito enquanto estava a ser copiado é contado como perdido, nunca entregue inconsistente. Um anfitrião que fique mais de 64 eventos para trás recebe o evento de runtime `telemetry.lost` com a contagem exata. Um evento maior do que um slot é substituído por `telemetry.oversized`, que o identifica. Os eventos são acrescentados a `SessionStatus.RuntimeEvents` (limitado aos últimos 256) e conduzem o ciclo de vida semântico (`plugin.started`, `world.starting`, `gameplay.entered`, eventos de módulos e falhas).

<a id="runtime-command-mailbox"></a>
## Mailbox de comandos de runtime

`OmsiLaunch.Runtime.<session>` é uma mailbox (caixa de correio) de 64 KiB com um único pedido de cada vez: `state` (int32 em 0: 0 inativo, 1 pedido, 2 respondido), `length` (int32 em 4), envelope em 8. Os envelopes são registos `RuntimeCommandWire` (magic `OLRC`, versão 1, cabeçalho de 72 bytes com tipo, comprimento total, GUID da sessão, id do pedido, comprimento do payload e SHA-256 do payload JSON UTF-8). O plugin consulta a mailbox a partir do temporizador da sua thread de UI (50 ms depois de o mundo estar carregado), executa o comando nessa thread e só publica a resposta se o slot ainda contiver o mesmo id de pedido; um pedido que o anfitrião abandonou por timeout nunca é respondido. Uma resposta demasiado grande é substituída por um erro tipado `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Os detalhes e os timeouts estão em [controlo de runtime](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Política de pesquisa de DLLs

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` e os assemblies da CLI declaram `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. As importações nativas (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) são resolvidas apenas a partir do próprio diretório do assembly (`plugins\`) ou do diretório de sistema do Windows; a raiz do OMSI e o `PATH` nunca são sondados. `OmsiLaunch.Native.x86.dll` é portanto carregado a partir de `plugins\` e de nenhum outro local.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Quando o OMSI é iniciado sem o OmsiLaunch

Como o conjunto de ficheiros é permanente, o OMSI carrega `OmsiLaunch.PluginNE.dll` em cada arranque, incluindo arranques a partir do Steam ou do ambiente de trabalho. Nesse caso:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` está ausente, pelo que não é aplicado nenhum patch ao downloader.
- `OMSILAUNCH_HANDOFF_NAME` está ausente, pelo que `PluginRuntime.Start` emite `plugin.handoff.invalid` e devolve `false`. `CurrentTelemetrySink.Emit` retorna imediatamente quando `OMSILAUNCH_TELEMETRY_NAME` não está definido, pelo que nada é escrito em lado nenhum.
- Sem validação da build, sem hook nativo, sem mailbox, sem temporizador. O plugin permanece carregado, mas inerte; o OMSI comporta-se como se o plugin não existisse.
- `PluginFinalize`, à saída do OMSI, chama a rotina nativa de restauro e `NativeD3DShutdown`, ambas sem efeito quando nada foi instalado.

Portanto, não é necessário remover o `.opl` para executar o OMSI normalmente.

<a id="non-interference-with-third-party-plugins"></a>
## Não interferência com plugins de terceiros

As sessões nunca enumeram, calculam o hash, copiam, removem nem restauram outros ficheiros em `plugins\`. O plugin não usa o canal `AccessVariable` do OMSI e não toca no estado de outros plugins. Os únicos patches dentro do processo são o hook de arranque headless definido no perfil (um redirecionamento de VMT de utilização única armado para a sessão), a supressão opcional das texturas da Internet (restaurada em `PluginFinalize`) e a interceção de `Reset` do dispositivo D3D9 usada para acompanhar o ciclo de vida das texturas.

<a id="difference-from-omsihook"></a>
## Diferença em relação ao OmsiHook

O OmsiLaunch **não tem nenhuma dependência de runtime** do OmsiHook nem de qualquer binário do OmsiHook: os únicos pacotes referenciados são `DNNE` 2.0.6 e `YamlDotNet` 15.1.2, e não existe nenhum `using OmsiHook` nem P/Invoke para DLLs do OmsiHook em parte alguma do produto. O que o OmsiLaunch partilha com o OmsiHook é conhecimento derivado: as estruturas de objetos e vários wrappers de leitura foram reconciliados a partir do checkout fixado do OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) com o executável exato `Omsi23004_692EBFBF`. A atribuição e os termos de licença estão em `THIRD-PARTY-NOTICES.md` e a matriz de reutilização por ficheiro em `third_party/OMSIHOOK-REUSE-MATRIX.md`. O OmsiHook injeta a partir de um processo separado e expõe ponteiros em bruto; o OmsiLaunch é executado dentro do processo, expõe apenas handles opacos com âmbito de sessão e retira qualquer endereço nativo dos resultados públicos (ver [capacidades](../reference/capabilities.md)).
