# Referência da API pública (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=03d2b00af2df06cdc28d5fbbec294f9f480584c4817b1dfe8b805a914621f734 -->
> Tradução da [página original em inglês](../../../reference/public-api.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página é a referência normativa da API pública gerida do OmsiLaunch 0.1.0-beta4: o assembly `OmsiLaunch.Api` (contratos) e o ponto de entrada para integradores `OmsiLaunchService` em `OmsiLaunch.Core`. Documenta apenas o que o código atual faz. Tudo o que um integrador pode chamar, receber ou observar está aqui listado com o respetivo nível de estabilidade; o que não estiver listado não é uma superfície de integração.

O [inventário da API pública](public-api-inventory.md) gerado lista todos os tipos e membros públicos de `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process` com a respetiva assinatura e estabilidade; um gate de documentação (verificação obrigatória) falha quando o inventário e os assemblies divergem. Esta página explica a semântica.

Páginas relacionadas: [referência do LaunchSpec](launchspec.md), [códigos de erro](errors.md), [ciclo de vida da sessão](../concepts/session-lifecycle.md), [transações e recuperação](../concepts/transactions-and-recovery.md), [controlo de runtime](runtime-control.md), [capacidades](capabilities.md), [plano de controlo local](local-control.md), [códigos de saída](exit-codes.md), [estado da validação em runtime](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Vocabulário de estabilidade

| Nível | Significado nesta página |
| --- | --- |
| `STABLE_BETA` | O contrato está congelado para a linha de protocolo 0.1 e o caminho está validado em runtime em `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Pode ser chamado e está testado, mas o contrato ou a evidência de runtime podem mudar antes de se tornar estável. |
| `PARTIAL` | Presente no contrato; apenas parte do comportamento está implementada ou validada (o texto indica qual parte). |
| `INTERNAL` | Público no assembly por razões técnicas (a ponte partilha o tipo), mas não é uma superfície de integração; pode mudar sem aviso. |
| `UNAVAILABLE` | Presente no contrato, mas rejeitado pela build atual. |

<a id="assembly-overview"></a>
## Visão geral dos assemblies

| Assembly | Papel para os integradores |
| --- | --- |
| `OmsiLaunch.Api` | Contratos puros: records, enums, `IOmsiLaunch`, registo de capacidades, catálogo de erros, formatos de transmissão, auxiliares D3D. Não contém nenhum `IntPtr`, `nint`, handle Win32, endereço nativo nem objeto de processo. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (a implementação de `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` e `CurrentWindowsX64Platform` (o único adaptador de plataforma), `InstallationLease`. Necessários para construir o serviço. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Assemblies de implementação. Os seus tipos públicos são `INTERNAL` para os integradores. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Ponto de entrada: `OmsiLaunchService` e `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parâmetro | Valor válido | Inválido / predefinição |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (namespace `OmsiLaunch.Process`). Deteta a plataforma, cria o processo do OMSI com `CreateProcessW`, aguarda-o e termina-o. | Não é distribuída nenhuma outra implementação. Um `IRuntimePlatform` personalizado é `INTERNAL`. |
| `PluginBuildDirectory` | Diretório que contém os ficheiros de referência do fecho (closure) do plugin permanente: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` e todos os `OmsiLaunch.*.dll` do fecho gerido (deve incluir `OmsiLaunch.Plugin.dll`). Num pacote instalado, é `<package>\plugins`. | Diretório ou ficheiro em falta: `PlanSessionAsync` devolve um plano não executável com `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Caminho de `OmsiLaunch.Native.x86.dll` (no pacote: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Igual ao anterior. |
| `ReleaseManifestPath` | `release-manifest.json` ao lado de `OmsiLaunch.exe`, quando existe. Fornece o SHA-256 esperado de cada ficheiro de `plugins/` (`plugin.integrity.reference = manifest`). | `null` (estrutura de desenvolvimento): os ficheiros instalados são verificados apenas quanto à presença e à coerência interna face ao fecho de referência (`plugin.integrity.reference = self`). Manifesto malformado: `OL_E_RELEASE_MANIFEST_INVALID`. |

O serviço lê estes caminhos em cada `PlanSessionAsync` e `StartSessionAsync`; nunca copia, prepara nem remove ficheiros do plugin (ver [plugin permanente](../concepts/permanent-plugin.md)). A CLI constrói o serviço exatamente assim (`tools/OmsiLaunch.Cli/Program.cs`):

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

Deve criar-se um serviço por processo e partilhá-lo. Estabilidade: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Regras de propriedade da sessão

| Regra | Detalhe |
| --- | --- |
| Um proprietário por instalação | `StartSessionAsync` adquire a concessão (lease) da instalação, um semáforo nomeado `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, e mantém-na até o supervisor ter restaurado a instalação. Um segundo arranque na mesma raiz, a partir de qualquer processo da mesma sessão de início de sessão (logon), falha com `OL_E_INSTALLATION_BUSY` (comunicado como uma sessão `Failed`, ver `StartSessionAsync`). A concessão é por sessão de logon, não abrange várias sessões de logon, e não é libertada enquanto outro processo mantiver um handle para ela (risco aceite). |
| Os handles são locais ao processo | `SessionHandle` encapsula o `Guid` da sessão. Só tem significado para a instância de `OmsiLaunchService` que o devolveu. Um handle construído a partir de um `Guid` conhecido noutro processo (ou noutra instância do serviço) produz `KeyNotFoundException`. O controlo entre processos passa pelo [plano de controlo local](local-control.md), não pelos handles. |
| Chamar sempre `CloseAsync` | A partir de `StartSessionAsync`, o processo é proprietário de uma transação durável. `CloseAsync` pede a paragem canónica quando necessário, aguarda o supervisor (saída do processo, restauro exato, libertação da concessão) e esquece a sessão. Deve ser chamado em todos os caminhos de saída, incluindo após um estado `Failed`. Sem ele, a entrada da sessão permanece em memória; o restauro em si é efetuado pelo supervisor de qualquer forma. |
| As sessões falhadas continuam a ser sessões | Um arranque que falha depois de `StartSessionAsync` ter retornado comunica `SessionState.Failed`; o handle continua válido para `GetStatusAsync`/`WaitForAsync` até `CloseAsync`. |
| Os planos são verificados novamente | `StartSessionAsync` volta a calcular o hash de `Omsi.exe` e volta a planear a especificação; um plano que já não seja executável é rejeitado com `OL_E_PLAN_NOT_RUNNABLE`. |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

Factos comuns a todos os métodos:

- Handles desconhecidos ou já fechados lançam `KeyNotFoundException` ("Unknown OmsiLaunch session.").
- Nenhum método exige uma sessão em execução (Running), exceto `ExecuteRuntimeAsync`.
- As exceções que transportam um código OmsiLaunch colocam o código no início de `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). A CLI extrai os códigos das mensagens da mesma forma (`CliProgram.Classify`).
- Instalação suportada: OMSI 2. As capacidades nativas são resolvidas e verificadas por build em processo; uma capacidade indisponível numa build falha com `OL_E_CAPABILITY_UNRESOLVED`. Ver [compatibilidade](compatibility.md).

<a id="complete-minimal-example"></a>
### Exemplo mínimo completo

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Finalidade | Compilar um `LaunchSpec` num `SessionPlan` sem iniciar o OMSI: validar a especificação, detetar a plataforma, obter a impressão digital de `Omsi.exe`, resolver as identidades de conteúdo, calcular as mutações de ficheiros planeadas, listar as capacidades necessárias e não suportadas e decidir `IsRunnable`. Capacidade pública `session.plan`. |
| Parâmetros | `spec`: um `LaunchSpec` totalmente preenchido (ver [referência do LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (os oito dicionários) e `Behavior` não podem ser nulos; os membros opcionais podem ser `null`. `RootPath` deve ser um diretório absoluto; uma raiz vazia é registada como `OL_E_INSTALLATION_NOT_FOUND`, mas a sondagem da plataforma num caminho vazio lança `ArgumentException` antes de o plano ser devolvido, pelo que nunca se deve passar uma raiz vazia. |
| Devolve | `SessionPlan` com um `SessionId` novo, `BuildProfileId` = a disposição conhecida (`"Omsi23004"`) ou `"unrecognized"` para uma build do OMSI 2 sem disposição conhecida, o `Spec` de entrada, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (caminhos de destino `plugins\OmsiLaunch.*` mais `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (um `PlannedModule` por [módulo de runtime](runtime-modules.md) pedido ou ativado por conteúdo; acessor `EffectivePlannedModules`). `IsRunnable` é `true` exatamente quando nenhum código de diagnóstico começa por `OL_E_`. Os diagnósticos informativos (`plugin.integrity.reference` com a mensagem `self` ou `manifest`, `session_profile.selected`) nunca tornam um plano não executável. |
| Erros transportados no resultado | Todos os erros de planeamento são diagnósticos, não exceções: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (a mensagem transporta o código de splash/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (o fecho do plugin instalado em `plugins\` é verificado face ao manifesto de lançamento no momento do planeamento), `OL_E_RUNTIME_ARTIFACT_MISSING` (a mensagem pode transportar `OL_E_RELEASE_MANIFEST_INVALID`). Condições completas: [regras de validação do LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Lançadas | `OperationCanceledException` se o token já estiver cancelado à entrada (o único ponto de verificação); `ArgumentException`/`NotSupportedException` para caminhos de raiz sintaticamente inválidos; `NullReferenceException`/`ArgumentNullException` para membros obrigatórios nulos; `System.Text.Json.JsonException` para um manifesto de lançamento sintaticamente inválido. |
| Cancelamento | Verificado uma vez à entrada. Depois disso, o planeamento é trabalho síncrono no sistema de ficheiros. |
| Requer sessão em execução | Não. |
| Altera o estado do OMSI | Não. |
| Altera o sistema de ficheiros | Não (lê `Omsi.exe`, ficheiros de conteúdo, o fecho do plugin e o manifesto). Os valores das definições não são validados aqui (apenas a existência da chave e a possibilidade de escrita); um valor inválido falha no arranque com `OL_E_INVALID_SETTING_VALUE`. |
| Transação / restauro | Nenhum. |
| Limitações | Pedir qualquer modo de `Date`/`Time`/`Year` diferente de `Unset`, qualquer modo de `Weather` diferente de `Unset`, qualquer campo de `PlayerVehicle`, documentos de `Input`, `EntrypointIdentity` ou `WorldMode.LastMapState` produz `OL_E_CAPABILITY_UNAVAILABLE` e um plano não executável nesta build (entradas em `UnsupportedRequestedFeatures` com a sua evidência de catálogo, `UNAVAILABLE` ou `PARTIAL`). |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Finalidade | Iniciar uma sessão gerida e transacional do OMSI a partir de um plano executável: adquirir a concessão da instalação, recuperar um journal obsoleto, validar o fecho do plugin permanente, fazer snapshot e overlay dos ficheiros da sessão, criar o handoff de arranque, o slot de telemetria e a mailbox de runtime (caixa de correio), iniciar `Omsi.exe`, registar o processo no journal e entregar a sessão a um supervisor em segundo plano. Capacidade pública `session.start`. |
| Parâmetros | `plan`: um `SessionPlan` com `IsRunnable == true`. A especificação dentro do plano é planeada novamente; do plano do chamador só se mantém `plan.SessionId`. `plan.Spec.Behavior.StartupTimeoutSeconds` tem de estar entre 1..600. |
| Devolve | `SessionHandle(plan.SessionId)` logo que `Omsi.exe` tenha sido criado e registado (estado `WaitingForPlugin`), ou logo que o caminho de arranque tenha falhado (estado `Failed`). Não aguarda pela jogabilidade; deve usar-se `WaitForAsync(session, SessionState.Running, ...)`. |
| Lançadas | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` quando `plan.IsRunnable` é falso; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` quando o novo planeamento não é executável (por exemplo, `Omsi.exe` foi alterado, conteúdo removido, fecho do plugin em falta); `InvalidOperationException("Duplicate session id.")` quando uma sessão com o mesmo id ainda está registada (chamar primeiro `CloseAsync`); `ArgumentOutOfRangeException` quando `StartupTimeoutSeconds` está fora de 1..600; `OperationCanceledException` quando cancelado antes ou durante o novo planeamento; e tudo o que `PlanSessionAsync` lança. Em todos os casos com exceção, nenhuma sessão é registada. |
| Erros transportados no resultado | Qualquer falha após o novo planeamento é capturada dentro do caminho de arranque: a sessão é registada, o seu estado é `Failed` e os seus diagnósticos contêm `OL_E_START_SESSION`, cuja mensagem é a mensagem interna (a começar pelo código interno, quando existe): `OL_E_INSTALLATION_BUSY` (concessão detida ou um processo OMSI registado no journal ainda vivo), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (apenas quando a nova tentativa diferida com os overlays desta sessão ainda não consegue provar a propriedade), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. A limpeza pode acrescentar `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (saída do OMSI não confirmada; journal mantido) ou `OL_E_RESTORE_FAILED`. As falhas posteriores são comunicadas pelo supervisor (ver [ciclo de vida da sessão](../concepts/session-lifecycle.md)). |
| Cancelamento | Antes/durante o novo planeamento: lança exceção. Depois disso, o token é passado à transação e à criação do processo; um cancelamento nessa fase é tratado como qualquer falha de arranque (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), o processo (se tiver sido criado) é terminado e a instalação restaurada. |
| Requer sessão em execução | Não. |
| Altera o estado do OMSI | Sim: cria o processo do OMSI com as variáveis de ambiente `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Altera o sistema de ficheiros | Sim, dentro da raiz da instalação: `.omsilaunch\diagnostics\<sessionId>-host.log` (retenção: as 50 sessões mais recentes), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (copiado uma vez para o splash gerido), overlays da sessão (patches de `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), eliminações da sessão (alvos ITX, `Texture\standard.ipr`, `closecheck`) e remoção permanente de um `closecheck` obsoleto preexistente quando `SuppressStaleClosecheckWarning` é verdadeiro (diagnóstico `closecheck.stale-removed`). |
| Transação / restauro | Abre a transação (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Todos os caminhos de saída da sessão terminam em restauro. Ver [transações e recuperação](../concepts/transactions-and-recovery.md). |
| Limitações | Apenas `WorldMode.NewMap` com `PresentedEntrypointIndex` e `WorldMode.SavedSituation` chegam à jogabilidade. `WorldMode.LastMapState` é `UNAVAILABLE`. Os pedidos de veículo do jogador nunca chegam a este método, porque são não executáveis no momento do planeamento. |
| Estabilidade | `STABLE_BETA` (os ciclos de vida NEW_MAP e SAVED_SITUATION estão validados em runtime). |
| Exemplo | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidade | Ler o estado semântico do ciclo de vida, os diagnósticos recolhidos até ao momento e a lista limitada de eventos de runtime. Capacidade pública `session.status`. |
| Parâmetros | `session`: um handle devolvido por `StartSessionAsync` e ainda não fechado. |
| Devolve | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: um snapshot imutável (as matrizes são copiadas sob o lock da sessão). `RuntimeEvents` nunca é `null` para uma sessão ativa. |
| Lançadas | `KeyNotFoundException` para handles desconhecidos/fechados. Caso contrário, nunca lança exceções. |
| Cancelamento | O token é ignorado (a chamada conclui-se de forma síncrona). |
| Requer sessão em execução | Não. |
| Altera OMSI / sistema de ficheiros / transação | Não / Não / Nenhuma. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Finalidade | Consultar periodicamente (a cada 100 ms) até a sessão estar em `state`, ou num estado terminal (`Completed`, `Failed`), ou até expirar o timeout; em seguida, devolver o estado atual. |
| Parâmetros | `state`: qualquer `SessionState`. Aguardar um estado transitório que já foi ultrapassado (ou que nunca é definido, ver [ciclo de vida da sessão](../concepts/session-lifecycle.md)) aguarda até um estado terminal ou até ao timeout. `timeout`: qualquer `TimeSpan` não negativo ou `Timeout.InfiniteTimeSpan`. |
| Devolve | O estado no momento em que a espera terminou. Em caso de timeout, é devolvido o estado e não uma exceção: o próprio chamador deve verificar `State`. Uma espera por `Running` que termine em `Failed` retorna imediatamente com os diagnósticos da falha. |
| Lançadas | `KeyNotFoundException`; `OperationCanceledException` quando o token do chamador é cancelado (apenas o cancelamento pelo chamador se propaga; o timeout interno não). |
| Cancelamento | O token do chamador é respeitado a cada intervalo de 100 ms. |
| Requer sessão em execução | Não. |
| Altera OMSI / sistema de ficheiros / transação | Não / Não / Nenhuma. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidade | Pedir a paragem canónica. Define a flag de paragem e retorna imediatamente; o supervisor observa a flag no seu ciclo de 100 ms, chama `TerminateProcess` sobre `Omsi.exe`, aguarda a saída, marca o journal como `ProcessExited`, restaura todos os ficheiros pertencentes à sessão e liberta a concessão. Trata-se de uma terminação forçada: a rotina de encerramento do próprio OMSI não é executada e o OMSI não reescreve `options.cfg` à saída (deliberado, protege a transação). O encerramento cooperativo por `WM_CLOSE` não está implementado (decisão de produto; no fecho de runtime, o OMSI não fechou no prazo de 30 s após `WM_CLOSE`, `L05b`). Capacidade pública `session.stop`. |
| Parâmetros | `session`. |
| Devolve | Tarefa concluída; não aguarda pela terminação nem pelo restauro. Deve usar-se `WaitForAsync(session, SessionState.Completed, ...)` para observar a conclusão. |
| Lançadas | `KeyNotFoundException`. |
| Cancelamento | Token ignorado. |
| Requer sessão em execução | Não. Idempotente; uma paragem pedida antes de o supervisor arrancar é respeitada logo que ele arranca; uma paragem numa sessão terminal não tem efeito. |
| Altera o estado do OMSI | Sim: termina o processo do OMSI (código de saída 1). |
| Altera o sistema de ficheiros | Indiretamente: desencadeia o restauro, a eliminação do journal e a remoção das cópias de segurança pelo supervisor. |
| Transação / restauro | Desencadeia `ProcessExited` → `Restoring` → `Restored`. As alterações do lado do runtime efetuadas através de `ExecuteRuntimeAsync` (escritas no relógio, veículos criados, variáveis de script, texturas D3D) não são restauradas; desaparecem com o processo. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidade | Libertar o handle do consumidor sem deixar a transação abandonada: se a sessão não for terminal, pede a paragem canónica; em seguida, aguarda a tarefa de ciclo de vida do supervisor (saída do processo, restauro, libertação da concessão); por fim, esquece a sessão. |
| Parâmetros | `session`. |
| Devolve | Conclui-se quando a sessão é terminal e removida. Depois de retornar, o handle é desconhecido (`KeyNotFoundException` em qualquer chamada posterior, incluindo um segundo `CloseAsync`). |
| Lançadas | `KeyNotFoundException`; `OperationCanceledException` se o chamador cancelar enquanto aguarda o supervisor. Nesse caso, a sessão não é removida e o supervisor continua em execução; deve chamar-se `CloseAsync` novamente. |
| Cancelamento | Aplica-se apenas à espera; nunca cancela o restauro. |
| Requer sessão em execução | Não. |
| Altera o estado do OMSI | Sim, quando a sessão ainda está ativa (igual a `StopAsync`). |
| Altera o sistema de ficheiros | Indiretamente (restauro pelo supervisor). |
| Transação / restauro | Garante que a transação é levada até à conclusão antes de o handle ser libertado (quando o supervisor foi iniciado). Para uma sessão que falhou antes de o supervisor arrancar, o caminho de arranque já restaurou ou comunicou `OL_E_RESTORE_DEFERRED`. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Finalidade | Executar uma operação de runtime pública dentro do processo OMSI em execução, através da mailbox da sessão de pedido único em curso (single-flight) (mapeada em memória, 64 KiB, pedido associado ao id de sessão e ao id de pedido). O plugin executa a operação na thread de UI do OMSI. Catálogo de operações: [controlo de runtime](runtime-control.md) e [capacidades](capabilities.md). |
| Parâmetros | `command.SessionId` tem de ser igual a `session.SessionId`. `command.RequestId`: `ulong` escolhido pelo chamador; deve usar-se um contador estritamente crescente por processo (os auxiliares D3D começam em 30 000, o proprietário da CLI em 10 001/50 000). `command.Operation`: um id de operação pública de `PublicCapabilityRegistry.PublicRuntimeOperationIds` (por exemplo `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: valores de cadeia indexados por nomes ordinais; os nomes obrigatórios por operação vêm de `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: medido a partir do momento em que o pedido é colocado na mailbox (a espera em fila atrás de outro comando em curso não é contabilizada). A CLI usa 5 s (15 s para `road-vehicles.spawn`) como proprietário e 8 s / 30 s como cliente. |
| Ordem das verificações | 1. Validação no registo (antes da procura da sessão): operação desconhecida ou `internal.*` → resultado `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; argumento obrigatório em falta (ausente ou apenas espaços em branco) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Procura da sessão → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Estado diferente de `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Pedido à mailbox. 6. Os valores do resultado cuja chave começa por `internal_` ou termina em `_address`, `_pointer`, `_vmt` são removidos. |
| Devolve | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. Em caso de sucesso, `Values` contém as cadeias semânticas da operação (documentadas por operação em [controlo de runtime](runtime-control.md)). |
| Erros transportados no resultado | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (registo); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (o resultado do plugin excedeu a mailbox; os resultados de listas limitadas são, em vez disso, encurtados com `truncated=true`); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` com uma chave derivada); todos os códigos `OL_E_D3D_*` (com `Values["detail"]` e `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (a capacidade nativa da operação não está resolvida na build em execução); e toda a falha tipada do lado do plugin, com o seu próprio código em `ErrorCode` e a mensagem em `Values["detail"]` (Beta 4; apenas uma falha sem código catalogado é `OL_E_RUNTIME_OPERATION_FAILED`). Códigos que chegam desta forma: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (verificações do lado do plugin), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Ver [códigos de erro](errors.md). |
| Lançadas | `KeyNotFoundException`; `InvalidOperationException` com `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (mailbox já descartada pelo supervisor), `OL_E_RUNTIME_CHANNEL_BUSY` (o slot ainda contém um pedido abandonado), `OL_E_RUNTIME_REQUEST_ID_REUSED` (uma resposta obsoleta para o mesmo id de pedido ainda está no slot); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (resposta corrompida, alheia ou não correspondente); `ArgumentOutOfRangeException` quando o pedido serializado excede a mailbox; `OperationCanceledException`. |
| Cancelamento | Respeitado durante a espera pelo gate por sessão e a cada 20 ms durante a consulta periódica da resposta. Cancelar a meio de um pedido não repõe o slot: a chamada seguinte nessa sessão pode falhar com `OL_E_RUNTIME_CHANNEL_BUSY` até o plugin publicar a sua resposta (que é então descartada como obsoleta). É preferível usar o timeout; um timeout repõe o slot e uma resposta tardia é detetada e descartada. |
| Requer sessão em execução | Sim (`SessionState.Running`); caso contrário, é lançado `OL_E_SESSION_NOT_RUNNING`. A mailbox existe até o supervisor a descartar durante o restauro. |
| Altera o estado do OMSI | Depende da operação: as operações `Read` não alteram; as operações `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) alteram estado dentro do processo que não é restaurado. |
| Altera o sistema de ficheiros | Nenhuma escrita do anfitrião. O OMSI pode escrever os seus próprios ficheiros em consequência (não monitorizado). |
| Transação / restauro | Nenhum. |
| Limitações | Um comando em curso por sessão (as chamadas na mesma sessão são serializadas). O pedido e a resposta estão cada um limitados a 64 KiB menos 8 bytes; os payloads de píxeis D3D a 48 KiB. `internal.road-vehicles.make-basic` é `INTERNAL` e inacessível. `weather.set` e `weather.metar.set` são `EXPERIMENTAL`. `timetable.logs.read` não é limitada e pode devolver `OL_E_RUNTIME_RESPONSE_TOO_LARGE` em horários grandes. `camera.lock` é `EXPERIMENTAL`; requer um veículo do jogador e está validada em runtime (`CAM01`), embora a cadeia `RuntimeValidation` do registo ainda indique `STATICALLY_VALIDATED`. Os handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) têm âmbito de sessão. |
| Estabilidade | Transporte e contrato `STABLE_BETA`; a estabilidade por operação segue `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`), com as exceções acima. |
| Exemplo | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidade | Devolver o catálogo de capacidades avaliado para uma instalação: uma `Capability(Name, Available, EvidenceState, Reason)` por entrada de `PublicCapabilityRegistry` que não seja `InternalOnly`. `Name` é o id do catálogo, `EvidenceState` a evidência do catálogo (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` é calculado a partir da plataforma, da verificação de OMSI 2, da classificação e das capacidades nativas de que a entrada precisa (ver [capacidades](capabilities.md#getcapabilitiesasync)). |
| Parâmetros | `installation.RootPath`: a instalação sondada (plataforma, `Omsi.exe`). `ExpectedExecutableSha256` é ignorado. |
| Devolve | Uma entrada por entrada de catálogo não interna, por exemplo `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` na disposição conhecida), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relação com `PublicCapabilityRegistry` | Derivada dele: o registo declara as capacidades, as suas operações e a sua evidência; esta chamada avalia-as para uma instalação. |
| Lançadas | `OperationCanceledException` à entrada; `ArgumentException` para um caminho de raiz vazio. |
| Cancelamento | Verificado uma vez à entrada. |
| Requer sessão em execução | Não. |
| Altera OMSI / sistema de ficheiros / transação | Não / Não / Nenhuma. |
| Estabilidade | Contrato de chamada `STABLE_BETA`; o conteúdo da lista é um inventário mantido manualmente: `PARTIAL`. |
| Exemplo | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Finalidade | Enumerar o conteúdo instalado e devolver identidades canónicas utilizáveis num `LaunchSpec`. A descoberta ignora pontos de reanálise (reparse points) (ciclos de junções não a podem bloquear), lê os ficheiros do OMSI como Windows-1252 (UTF-8/UTF-16 marcados com BOM são respeitados) e nunca segue ligações simbólicas. |
| Parâmetros | `query` e `scope` conforme a tabela abaixo. `scope` é obrigatório para `Entrypoints` (identidade do mapa), `Repaints`, `FleetNumbers`, `Registrations` (identidade do veículo). |
| Devolve | Lista ordenada de `ContentIdentity(Identity, Kind, DisplayName)`. As identidades são caminhos relativos à instalação com barras invertidas; as comparações não distinguem maiúsculas de minúsculas. |
| Lançadas | `OperationCanceledException` à entrada; `ArgumentException` quando `Entrypoints` é consultado sem âmbito ou a raiz está vazia; `FileNotFoundException` (sem código `OL_E_`; a CLI mapeia-a para `OL_E_NOT_FOUND`) quando o mapa ou veículo do âmbito não está instalado. Uma raiz ou um diretório de conteúdo em falta produz uma lista vazia, não um erro. |
| Cancelamento | Verificado uma vez à entrada. |
| Requer sessão em execução | Não. |
| Altera OMSI / sistema de ficheiros / transação | Não / Não / Nenhuma. |
| Estabilidade | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (todos os planos validados em runtime são resolvidos através deles). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (apenas evidência estática). |
| Exemplo | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Valores e resultados de `ContentQueryKind`:

| Valor | Âmbito | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | nenhum | `maps\<dir>\global.cfg` | `map` | nome do diretório do mapa |
| `Situations` | nenhum | `situations\...\<file>.osn` | `situation` | identidade do mapa referenciada pelo `.osn` (pode ser `null`) |
| `Vehicles` | nenhum | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` ou nome do ficheiro |
| `Repaints` | identidade do veículo (obrigatória; sem ela: lista vazia) | `<cti path>#item:<ordinal>` | `repaint` | nome de `[item]` |
| `Hofs` | nenhum | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | identidade do veículo (obrigatória; sem ela: lista vazia) | caminho de origem de `[number]` relativo ao veículo | `fleet-number` | `null` |
| `Registrations` | identidade do veículo (obrigatória; sem ela: lista vazia) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | primeira linha de valor (`null` para livre) |
| `Addons` | nenhum | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | identidade do mapa (obrigatória; sem ela: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | etiqueta do ponto de entrada |
| `SessionProfiles` | nenhum | nome do diretório do pacote (o id de `/predefined-profile`) | `session-profile`, ou `session-profile-invalid` quando o pacote não compila | `name` do perfil, ou o código `OL_E_SESSION_PROFILE_*` de um pacote inválido |
| `RuntimeModules` | nenhum | id do módulo | `runtime-module`, ou `runtime-module-invalid` quando não pode ser carregado | `name` do módulo, ou o código `OL_E_MODULE_*` |

Um ponto de entrada é selecionado por `PresentedEntrypointIndex` ou por `EntrypointIdentity` (uma identidade de descoberta ou uma etiqueta exata). O planeador resolve uma identidade para a etiqueta que o OMSI apresenta (o OMSI apresenta apenas o primeiro registo de cada etiqueta exata) e o plano lista-o em `ResolvedContent` com o tipo `entrypoint`; `world.entrypoint-identity` é `EXPERIMENTAL` até estar validada em runtime.

### `ListSessionProfilesAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidade | Listar todos os pacotes de perfil de sessão em `<root>\.omsilaunch\session-profiles\` com as suas predefinições (presets), para que um frontend os possa oferecer sem analisar YAML. CLI `/list:SessionProfiles`. |
| Devolve | Um `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` por diretório de pacote, ordenado por `Id`. `Presets` são `SessionProfilePresetSummary(Index, Id, Name)` por ordem de índice. Cada pacote é compilado com as mesmas regras estritas de um lançamento (`SessionProfileCompiler`); um pacote que não compila é listado com `IsValid = false`, o seu código `OL_E_SESSION_PROFILE_*` e mensagem, e sem predefinições. Sem diretório de perfis: lista vazia. |
| Lançadas | `OperationCanceledException` à entrada. Os erros de compilação são comunicados no resultado, não lançados. |
| Requer sessão em execução | Não. |
| Altera OMSI / sistema de ficheiros / transação | Não / Não / Nenhuma. |
| Estabilidade | `STABLE_BETA` (gate offline `session-profiles.discovery`). |
| Exemplo | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidade | Descobrir os [módulos de runtime](runtime-modules.md) instalados em `plugins\OmsiLaunch.Modules` com a sua integridade e definições. Apenas leitura. O carregamento é aberto: nenhum módulo é verificado face a uma lista ou assinatura. |
| Devolve | Um `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` por diretório de módulo, ordenado por nome de diretório. `Activation` é `requested`, `content` ou `default`; `IsValid` é falso com um código `OL_E_MODULE_*` quando o módulo não pode ser carregado; `Settings` lista as definições que o módulo declara como `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Lançadas | `OperationCanceledException` à entrada. |
| Requer sessão em execução | Não. CLI: `/list:RuntimeModules` (envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspeto | Detalhe |
| --- | --- |
| Assinatura | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Finalidade | Comunicar ou concluir uma transação durável obsoleta (`<root>\.omsilaunch\journal.json`) deixada por um proprietário que falhou abruptamente. Obtém a concessão da instalação durante a chamada, para nunca restaurar por baixo de uma sessão em arranque. Capacidade pública `session.recover`; CLI `/recovery-status` e `/recover`. |
| Parâmetros | `installation.RootPath`: a raiz da instalação (normalizada com `Path.GetFullPath`). `restore`: `false` = apenas comunicar; `true` = restaurar, verificar, eliminar o journal e as cópias de segurança. |
| Devolve | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = existia um journal quando a chamada começou; `Recovered` = foi pedido um restauro, este foi executado e não resta nenhum journal; `Diagnostics` = notas de restauro (`restore.session-artifact-removed` com `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), vazio quando nada foi restaurado. |
| Lançadas | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` quando a concessão está detida; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` quando o PID + hora de criação + caminho do executável do journal ainda correspondem a um processo vivo, ou (journal para além de `HandoffCreated` sem PID) qualquer `Omsi.exe` dessa raiz está em execução; `IOException` com `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` ou uma mensagem de verificação ("Restore hash mismatch: ...", "Restore presence mismatch: ..."); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` para um journal corrompido; `ArgumentException` para uma raiz vazia; `OperationCanceledException`. Sempre que lança uma exceção depois de um restauro ter começado, o journal é mantido e a chamada seguinte repete-o de forma idempotente. |
| Cancelamento | Passado às escritas do journal/das cópias de segurança; cancelar a meio do restauro deixa o journal pendente. |
| Requer sessão em execução | Não (recusa enquanto houver um proprietário ativo). |
| Altera o estado do OMSI | Não. |
| Altera o sistema de ficheiros | Apenas com `restore == true`: reescreve os originais a partir de cópias de segurança verificadas (bytes, hora da última escrita, hora de criação, atributos; originais só de leitura tratados; write-through + flush; nenhum `*.omsilaunch.tmp` deixado), remove os artefactos da sessão, elimina `journal.json` e `backup\<sessionId>`. |
| Transação / restauro | Conclui a transação pendente (`Restoring` → `Restored` → journal removido). |
| Estabilidade | `STABLE_BETA`: caminho de comunicação e recuperação após saída antecipada (matriz RV-008), recuperação após um restauro falhado (fecho de runtime `F01`), recusa com um proprietário ativo e na janela anterior ao PID (`S04`) e recuperação diferida anterior à impressão digital no arranque (`S05`); ver [estado da validação em runtime](../status/runtime-validation-status.md). |
| Exemplo | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Tipos de contrato

<a id="optional-values-and-semantic-primitives"></a>
### Valores opcionais e primitivas semânticas

| Tipo | Definição | Notas |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, `Unset` estático, `Set(T)` estático | Distingue "não pedido" de "pedido com um valor". A forma JSON está documentada na [referência do LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Enum de byte. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Validado apenas quando `DateTimeMode.Explicit` (mês 1..12, dia 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Validado apenas quando `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Família LaunchSpec

Todos os records abaixo estão documentados propriedade a propriedade na [referência do LaunchSpec](launchspec.md); esta tabela fixa o inventário de tipos.

| Tipo | Finalidade | Estabilidade |
| --- | --- | --- |
| `LaunchSpec` | Record de pedido raiz com os acessores `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, que substituem os membros opcionais `null` por predefinições. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (fixação opcional do `Omsi.exe`, verificada no planeamento). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Seleção do mundo. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto de `LastMapState`; nunca "o .osn mais recente"). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (calculado a partir de `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Qualquer modo diferente de `Unset` torna o plano não executável. | `PARTIAL` (evidência do plano `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Qualquer modo diferente de `Unset` torna o plano não executável. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Qualquer campo definido torna o plano não executável. | `PARTIAL` |
| `EnvironmentSpec` | Oito grupos `IReadOnlyDictionary<string, OptionalValue<string>>` de definições semânticas de `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; qualquer valor definido torna o plano não executável. | `PARTIAL` |
| `DiagnosticsSpec` | Seis booleanos; transportados, não consumidos. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Proveniência de um perfil de sessão compilado (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [Gestão de HOF](hof-management.md) da sessão: `Operations` (lista de `HofOperationSpec`). Campo `LaunchSpec.HofManagement`; acessor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Módulos de runtime](runtime-modules.md) da sessão: `Enable` (ids de módulo), `Disable` (ids de módulo mantidos de fora), `Settings` (`"<module id>.<key>"` para valor); acessores `EffectiveDisable`, `EffectiveSettings`. Campo `LaunchSpec.Modules`; acessor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | A avaliação do plano de um módulo: `Id`, `Version`, `Status` (`ELIGIBLE` ou `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` ou `default`), `Settings`, `RuntimeOperations` (operações do catálogo que o módulo executa em processo, cada uma com a sua política de mutação quando altera o OMSI, por exemplo `camera.set (omsi-state)`), `TickMilliseconds`, `SnapshotSha256` (o digest, no momento do plano, dos ficheiros do módulo, ver [módulos de runtime](runtime-modules.md#integrity)); `IsEligible`, `EffectiveSettings`, `EffectiveRuntimeOperations`. Constantes `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Uma definição efetiva de módulo: `Key`, `Value`, `Source` (`request`, `settings-file` ou `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Vista de descoberta de um módulo instalado; ver `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Uma definição que um módulo declara: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Uma operação de HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Lançada pelo planeamento de HOF (`OmsiLaunch.Core`); `Code` é um código `OL_E_HOF_*`. A API pública comunica-a como diagnósticos do plano. | `STABLE_BETA` |
| `SessionProfileSummary` | Vista de descoberta de um pacote (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Ver `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Uma predefinição (preset) num resumo (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (transportado; o restauro acontece sempre), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, predefinição 180), `ShutdownTimeoutSeconds` (transportado, não consumido). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Tipos de plano e de estado

| Tipo | Campos | Notas |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (novo `Guid` por plano), `BuildProfileId` (`"Omsi23004"` ou `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (lista de `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (caminhos relativos distintos de `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` com `STATICALLY_VALIDATED` ou `UNAVAILABLE`; capacidades nativas de arranque com `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (entradas `Capability` para funcionalidades pedidas mas não suportadas), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Um record público: pode ser editado ou ficar desatualizado, razão pela qual `StartSessionAsync` volta a planear. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, SO x64 e anfitrião x64), `LegacyPlatform` (sempre `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (todos iguais a `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` é um valor de `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), exceto em `SessionPlan.RequiredCapabilities`: um requisito de plataforma ou de conteúdo é `STATICALLY_VALIDATED` ou `UNAVAILABLE`, uma capacidade nativa `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Chaves usadas pelas mutações de apresentação: `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (mapa de cadeias opcional). | Os códigos que começam por `OL_E_` são erros, `OL_W_` avisos, e tudo o resto é informativo. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Os diagnósticos da sessão não incluem os diagnósticos do plano. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (hora de receção no anfitrião), `Sequence` (a partir de 1, por sessão), `Data`, `SessionTickMilliseconds` (o relógio de sessão monotónico do plugin quando emitiu o evento; nulo para eventos do anfitrião). | Limitado aos 256 eventos mais recentes (os mais antigos são descartados). Os eventos do plugin chegam por ordem através do anel de telemetria; os eventos perdidos por um excesso do anel são contados por `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Local ao processo. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Ver `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Ver `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Enum de byte, pela ordem de declaração: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` e `EnteringGameplay` nunca são definidos pelo serviço atual; `Snapshotting` é transitório e praticamente inobservável. Estados terminais: `Completed`, `Failed`. Semântica completa: [ciclo de vida da sessão](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Tipos de controlo de runtime

| Tipo | Definição | Estabilidade |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Codec estático usado pelo anfitrião e pelo plugin para o envelope da mailbox: número mágico `0x4F4C5243` ("OLRC"), versão 1, cabeçalho little-endian de 72 bytes (número mágico, versão, tipo 1 = pedido / 2 = resposta, comprimento total, `Guid` da sessão, id de pedido, comprimento do payload, SHA-256 do payload) seguido de um payload JSON em UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: público porque ambas as extremidades da ponte o partilham; não é uma superfície de integração; o formato pode mudar com a versão do protocolo. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — o que o anfitrião publica para o plugin no ficheiro mapeado em memória `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256, string Settings, string SnapshotSha256)`: um módulo planeado transportado pelo handoff (`StartupHandoff.Modules`); `SnapshotSha256` (versão 8 do formato) é o digest dos ficheiros do módulo obtido no planeamento, vazio quando o plano não trouxe nenhum. | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: o relógio de arranque resolvido de uma sessão NEW_MAP transportado pelo handoff (`StartupHandoff.Start`); um campo nulo mantém o valor do próprio OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Codec: número mágico `0x4F4C5348`, versão 8 (lê 3 a 8; a versão 5 acrescenta os módulos planeados, a versão 6 as definições efetivas de cada módulo, a versão 7 a data/hora de arranque, a versão 8 o snapshot de ficheiros de cada módulo no momento do plano; os bits de flag 0x04 e 0x08 transportam `PluginTrace` e `NativeTrace`), cabeçalho de 64 bytes com integridade do payload por SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Anel de eventos do plugin para o anfitrião no mapeamento `OMSILAUNCH_TELEMETRY_NAME`: número mágico `0x52544C4F` ("OLTR"), versão 1, 64 slots numerados de 2048 bytes. `Initialize`, `Write`, `IsRing`, `ReadSince` (devolve os eventos posteriores a um número de sequência e quantos se perderam). | `INTERNAL` |

O plugin rejeita um handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`), a menos que `WorldMode` seja `NewMap` ou `SavedSituation`, `HeadlessStart` seja verdadeiro, `PlayerVehicleEnabled` seja falso, ambos os modos de data/hora sejam `Unset` e uma situação guardada tenha uma identidade não vazia. O planeador impõe as mesmas restrições mais cedo, pelo que um plano executável nunca desencadeia esta rejeição.

<a id="capability-registry-types"></a>
### Tipos do registo de capacidades

| Tipo | Finalidade |
| --- | --- |
| `PublicCapabilityRegistry` | O catálogo canónico de capacidades. `ProtocolVersion` (`"0.1"`), `All` (todos os `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (os 48 ids de operação concretos que um frontend pode reencaminhar), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (rota de comando para id de operação), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (devolve `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Imposto por `ExecuteRuntimeAsync`, pela CLI, pelo plano de controlo local e pelo plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (um valor de `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; derivadas `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (aceita `cursor`/`limit`, ver [paginação](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | O que uma operação de runtime altera: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | O vocabulário de evidência: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Catálogo completo: [capacidades](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Métodos de extensão `D3DRuntimeApi`

Wrappers tipados sobre `ExecuteRuntimeAsync` para as operações `d3d.*` (`EXPERIMENTAL`, capacidade `PublicExperimental` `d3d.texture`). Atribuem ids de pedido a partir de um contador global do processo que começa em 30 000 e usam por predefinição um timeout de 5 s (exceto `GetD3DStatusAsync`, que exige um).

| Método | Operação | Argumentos e limites |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | nenhum |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | largura/altura 1..4096, níveis 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | nível 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, largura/altura 1..4096, píxeis ≤ 48 KiB (codificados em Base64 na transmissão) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | uma libertação repetida é rejeitada com `OL_E_D3D_RESOURCE_RELEASED` |

Tipos: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` com `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Erros: um resultado falhado é relançado como `OmsiRuntimeException(Code, detail)`, em que `Code` é o `ErrorCode` do resultado (ou `OL_E_RUNTIME_OPERATION_FAILED` quando ausente) e a mensagem é `"<code>: <Values["detail"]>"`; um resultado bem-sucedido sem valores, ou uma cadeia de estado do dispositivo desconhecida, lança `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Tudo o que `ExecuteRuntimeAsync` lança propaga-se sem alterações. O tratamento da reposição do dispositivo (reset) está validado em runtime: uma reposição faz o dispositivo passar por `Resetting` de volta a `Ready` e invalida todas as texturas ativas (`OL_E_D3D_STALE_RESOURCE_HANDLE`, fecho de runtime `D01`); a entrada de catálogo `d3d.texture` regista-o. A transição `Lost` não pode ser produzida a partir do exterior do produto e está coberta apenas offline.

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### Tipos de contrato do processo

| Tipo | Conteúdo |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Usado apenas pela CLI ([códigos de saída](exit-codes.md)); a API nunca termina o processo. |
| `PublicErrorCategory` | Constantes de cadeia usadas nos envelopes de erro da CLI/do controlo: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Uma `const string` por código (143: 142 erros `OL_E_` e 1 aviso `OL_W_`) e `All`, o catálogo `PublicErrorDescriptor(Code, Category)`. Categorias: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Referência: [códigos de erro](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Propriedade `Code` mais mensagem; lançada apenas por `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (identidade da instalação e contenção de caminhos)

Estabilidade: `STABLE_BETA` (funções puras, sem I/O, sem estado do OMSI, sem alterações ao sistema de ficheiros, sem participação em transações, sem necessidade de sessão em execução). É a definição única usada pela concessão da instalação, pelo nome do pipe de controlo local, pelo confinamento de recursos dos perfis de sessão, pela validação dos alvos de Internet Textures e pelo caminho do modelo de criação (spawn) em runtime.

| Membro | Comportamento |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` sem separadores finais, exceto numa raiz de unidade (`C:\`), que é mantida. Resolve os segmentos `.` e `..`, trata `/` e `\` da mesma forma e reduz separadores repetidos. **Não** resolve junções nem ligações simbólicas. Lança `ArgumentException` para uma raiz nula ou em branco. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` convertido para maiúsculas. Grafias lexicalmente equivalentes de uma mesma raiz (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) partilham uma chave; raízes diferentes (`C:\OMSI-A`, `C:\OMSI-B`) nunca a partilham. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Resolve `candidate` (relativo a `root`, ou absoluto) e devolve `true` apenas quando se encontra estritamente abaixo de `root`; `relativePath` é a grafia canónica com `\`. Usa os segmentos de `Path.GetRelativePath`, pelo que um irmão como `C:\OMSI-A\x` nunca está dentro de `C:\OMSI`; a própria raiz, outros volumes e fugas com `..` devolvem `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Divide por `/` e `\`, descartando segmentos vazios. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Perfis de sessão (`OmsiLaunch.Core`)

Estabilidade: `EXPERIMENTAL`. Estes tipos compilam um [perfil de sessão](session-profiles.md) YAML (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, esquema `omsilaunch.session-profile/v1`) num `LaunchSpec`. A CLI `/predefined-profile:<id> /predefined-profile-index:<n>` usa exatamente estas chamadas; um integrador pode usá-las para iniciar um perfil através da API.

| Membro | Comportamento |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Lê e valida o pacote. `id` tem de ser um nome de diretório simples (caso contrário, `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` está entre 1..99 (`MinPresetIndex`..`MaxPresetIndex`; caso contrário, `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Ficheiro em falta: `OL_E_SESSION_PROFILE_NOT_FOUND`; maior do que `MaxBytes` (256 KiB), YAML inválido, âncoras, chaves desconhecidas ou um `id` diferente do nome do diretório: `OL_E_SESSION_PROFILE_INVALID`; outro `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Os caminhos de recursos estão confinados ao pacote (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Todas as falhas são uma `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Devolve `baseline` com o perfil aplicado: para `NewMap`, o bloco `new` do perfil (mapa, ponto de entrada e qualquer data/hora/ano/meteorologia); as `settings` da predefinição (preset) fundidas sobre `Environment.General`; `Presentation`, `InternetTextures` e `Behavior` da predefinição, quando presentes; e `SessionProfile` = `profile.Metadata`. Para `NewMap`, verifica também o mapa face à lista `compatibility` do perfil (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | A verificação de compatibilidade para os outros modos (para `SavedSituation`, o mapa é lido do `.osn`). A CLI chama-a depois de o `WorldSpec` final ser construído. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` e as chaves aceites por mapeamento YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | O pacote carregado; `Preset` é apenas a predefinição selecionada. |
| `SessionProfileException(string code, string message)` | `IOException` com `Code` (um dos códigos `OL_E_SESSION_PROFILE_*`); a mensagem é `"<code>: <message>"`. |

A CLI rejeita adicionalmente flags da linha de comandos que entrem em conflito com o perfil (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); essa verificação não faz parte do compilador. Ver [perfis de sessão](session-profiles.md#precedence-and-override-conflicts) para a ordem de fusão completa.

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### Tipos de plataforma (`OmsiLaunch.Process`)

| Tipo | Estabilidade | Utilização |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` como tipo do parâmetro do construtor de `OmsiLaunchService` | Deteta a plataforma, verifica a possibilidade de escrita, inicia, observa, termina e aguarda `Omsi.exe`. Deve passar-se `new CurrentWindowsX64Platform()`; implementá-lo por conta própria não é suportado. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | A única implementação: anfitrião Windows x64, `CreateProcessW` para `Omsi.exe`, `TerminateProcess` para a paragem canónica. Os seus métodos são chamados pelo serviço; os integradores apenas o constroem. Membros (partilhados com `IRuntimePlatform`): `Detect(root)` devolve o `RuntimePlatformInfo` do plano; `ValidateCurrent(info)` lança `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` quando o anfitrião não consegue executar uma sessão; `IsInstallationWritable(root)` suporta `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` cria `Omsi.exe` e regista a identidade do processo (PID, hora de criação, caminho e o hash calculado pelo serviço; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` e `Terminate` observam-no e terminam-no. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Públicos no assembly porque o serviço e os testes os partilham. Não são uma superfície de integração; `LaunchedProcess` encapsula internamente os handles do processo e da thread do OMSI (não são membros públicos) e nunca é devolvido por `IOmsiLaunch`. |

<a id="thread-safety"></a>
## Segurança de threads

- `OmsiLaunchService` é seguro para chamadas concorrentes em sessões diferentes: as sessões vivem num `ConcurrentDictionary` e cada alteração por sessão ocorre sob o lock privado da sessão.
- As chamadas concorrentes na mesma sessão são seguras, mas serializadas onde é relevante: `ExecuteRuntimeAsync` obtém um gate por sessão, pelo que um segundo comando aguarda pelo primeiro (o seu timeout começa quando é colocado na mailbox).
- O supervisor é executado numa tarefa do thread pool (`Task.Run`) desde o momento em que `StartSessionAsync` retorna até a sessão ser terminal; consulta a telemetria e o processo a cada 100 ms. Os chamadores nunca executam código do supervisor.
- `StopAsync` e `GetStatusAsync` concluem-se de forma síncrona e podem ser chamados a partir de qualquer thread, incluindo dentro de um handler `ProcessExit` (a CLI faz isto com um orçamento de 4 s).
- Nenhuma chamada da API está vinculada a uma thread; nenhuma requer um contexto de sincronização.

<a id="what-is-not-in-the-api"></a>
## O que não está na API

- Nenhum `IntPtr`, `nint`, handles Win32, endereços nativos, ponteiros de VMT ou objetos de processo. Os valores do resultado cujas chaves começam por `internal_` ou terminam em `_address`, `_pointer`, `_vmt` são removidos antes de um resultado sair de `ExecuteRuntimeAsync`.
- Nenhuma operação de runtime `internal.*`: `internal.road-vehicles.make-basic` é `InternalOnly` no registo e devolve `OL_E_RUNTIME_OPERATION_UNKNOWN` a partir da API e da CLI.
- Nenhuma leitura ou escrita direta na memória do OMSI, nenhum acesso ao nível de ficheiros à instalação para além do que um `LaunchSpec` declara.
- Nenhum handle entre processos: o [plano de controlo local](local-control.md) é a única via entre processos e aceita apenas `session.status`, `session.events`, `session.stop` e `runtime.execute`.
- Nenhuma aplicação do veículo do jogador no arranque. O encerramento cooperativo, `LAST_MAP_STATE`, a data/hora/meteorologia de arranque e os overlays de documentos de teclado/controlador são `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Resumo da estabilidade

| Superfície | Estabilidade |
| --- | --- |
| Construtor de `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transporte de `ExecuteRuntimeAsync`; operações `PublicStableBeta` | `STABLE_BETA` |
| Operações `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| membros de especificação de data/hora/meteorologia/entrada, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| membros de especificação do veículo do jogador | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implementações de `IRuntimePlatform`, todos os assemblies de implementação | `INTERNAL` |
| `WorldMode.LastSituation`, operações `internal.*` | `UNAVAILABLE` |
