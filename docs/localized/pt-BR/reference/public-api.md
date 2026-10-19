# Referência da API pública (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=03d2b00af2df06cdc28d5fbbec294f9f480584c4817b1dfe8b805a914621f734 -->
> Tradução da [página original em inglês](../../../reference/public-api.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página é a referência normativa da API pública gerenciada do OmsiLaunch 0.1.0-beta4: o assembly `OmsiLaunch.Api` (contratos) e o ponto de entrada para integradores `OmsiLaunchService` em `OmsiLaunch.Core`. Ela documenta somente o que o código atual faz. Tudo o que um integrador pode chamar, receber ou observar está listado aqui com seu nível de estabilidade; o que não está listado não é uma superfície de integração.

O [inventário da API pública](public-api-inventory.md) gerado lista todo tipo e membro público de `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process` com sua assinatura e estabilidade; um gate de documentação falha quando o inventário e os assemblies divergem. Esta página explica a semântica.

Páginas relacionadas: [referência do LaunchSpec](launchspec.md), [códigos de erro](errors.md), [ciclo de vida da sessão](../concepts/session-lifecycle.md), [transações e recuperação](../concepts/transactions-and-recovery.md), [controle de runtime](runtime-control.md), [capacidades](capabilities.md), [plano de controle local](local-control.md), [códigos de saída](exit-codes.md), [status da validação em runtime](../status/runtime-validation-status.md).

<a id="stability-vocabulary"></a>
## Vocabulário de estabilidade

| Nível | Significado nesta página |
| --- | --- |
| `STABLE_BETA` | O contrato está congelado para a linha de protocolo 0.1 e o caminho está validado em runtime em `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Pode ser chamado e é testado, mas o contrato ou a evidência de runtime podem mudar antes de se tornar estável. |
| `PARTIAL` | Presente no contrato; somente parte do comportamento está implementada ou validada (o texto diz qual parte). |
| `INTERNAL` | Público no assembly por razões técnicas (a ponte compartilha o tipo), mas não é uma superfície de integração; pode mudar sem aviso. |
| `UNAVAILABLE` | Presente no contrato, mas rejeitado pela build atual. |

<a id="assembly-overview"></a>
## Visão geral dos assemblies

| Assembly | Papel para integradores |
| --- | --- |
| `OmsiLaunch.Api` | Contratos puros: records, enums, `IOmsiLaunch`, registro de capacidades, catálogo de erros, formatos de transmissão (wire formats), helpers de D3D. Não contém `IntPtr`, `nint`, handle Win32, endereço nativo nem objeto de processo. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (a implementação de `IOmsiLaunch`), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` e `CurrentWindowsX64Platform` (o único adaptador de plataforma), `InstallationLease`. Necessários para construir o serviço. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Assemblies de implementação. Seus tipos públicos são `INTERNAL` para integradores. |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## Ponto de entrada: `OmsiLaunchService` e `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parâmetro | Valor válido | Inválido / padrão |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (namespace `OmsiLaunch.Process`). Detecta a plataforma, cria o processo do OMSI com `CreateProcessW`, aguarda seu término e o encerra. | Nenhuma outra implementação é distribuída. Um `IRuntimePlatform` personalizado é `INTERNAL`. |
| `PluginBuildDirectory` | Diretório que contém os arquivos de referência do conjunto de arquivos do plugin permanente (closure do plugin): `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` e todo `OmsiLaunch.*.dll` da closure gerenciada (deve incluir `OmsiLaunch.Plugin.dll`). Em um pacote instalado, é `<package>\plugins`. | Diretório ou arquivo ausente: `PlanSessionAsync` retorna um plano não apto para execução com `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Caminho de `OmsiLaunch.Native.x86.dll` (no pacote: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Igual ao anterior. |
| `ReleaseManifestPath` | `release-manifest.json` ao lado de `OmsiLaunch.exe`, quando presente. Fornece o SHA-256 esperado de cada arquivo de `plugins/` (`plugin.integrity.reference = manifest`). | `null` (layout de desenvolvimento): os arquivos instalados são verificados apenas quanto à presença e à autoconsistência em relação à closure de referência (`plugin.integrity.reference = self`). Manifesto malformado: `OL_E_RELEASE_MANIFEST_INVALID`. |

O serviço lê esses caminhos em toda chamada de `PlanSessionAsync` e `StartSessionAsync`; ele nunca copia, prepara nem remove arquivos do plugin (consulte [plugin permanente](../concepts/permanent-plugin.md)). A CLI constrói o serviço exatamente assim (`tools/OmsiLaunch.Cli/Program.cs`):

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

Crie um serviço por processo e compartilhe-o. Estabilidade: `STABLE_BETA`.

<a id="session-ownership-rules"></a>
## Regras de propriedade da sessão

| Regra | Detalhe |
| --- | --- |
| Um proprietário por instalação | `StartSessionAsync` adquire o lease da instalação, um semáforo nomeado `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, e o mantém até que o supervisor tenha restaurado a instalação. Um segundo início na mesma raiz, a partir de qualquer processo da mesma sessão de logon, falha com `OL_E_INSTALLATION_BUSY` (relatado como uma sessão `Failed`, consulte `StartSessionAsync`). O lease vale por sessão de logon, não entre sessões de logon diferentes, e não é liberado enquanto outro processo mantiver um handle para ele (risco aceito). |
| Handles são locais ao processo | `SessionHandle` encapsula o `Guid` da sessão. Ele só tem significado para a instância de `OmsiLaunchService` que o retornou. Um handle construído a partir de um `Guid` conhecido em outro processo (ou em outra instância do serviço) resulta em `KeyNotFoundException`. O controle entre processos passa pelo [plano de controle local](local-control.md), não por handles. |
| Sempre chame `CloseAsync` | A partir de `StartSessionAsync`, o processo possui uma transação durável. `CloseAsync` solicita a parada canônica quando necessário, aguarda o supervisor (saída do processo, restauração exata, liberação do lease) e esquece a sessão. Deve ser chamado em todo caminho de saída, inclusive após um estado `Failed`. Sem ele, a entrada da sessão permanece na memória; a restauração em si é executada pelo supervisor de qualquer forma. |
| Sessões com falha continuam sendo sessões | Um início que falha depois que `StartSessionAsync` retornou relata `SessionState.Failed`; o handle continua válido para `GetStatusAsync`/`WaitForAsync` até `CloseAsync`. |
| Planos são verificados novamente | `StartSessionAsync` recalcula o hash de `Omsi.exe` e replaneja a especificação; um plano que não está mais apto para execução é rejeitado com `OL_E_PLAN_NOT_RUNNABLE`. |

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

Fatos comuns a todos os métodos:

- Handles desconhecidos ou já fechados lançam `KeyNotFoundException` ("Unknown OmsiLaunch session.").
- Nenhum método exige uma sessão em execução (Running), exceto `ExecuteRuntimeAsync`.
- Exceções que carregam um código do OmsiLaunch colocam o código no início de `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). A CLI extrai códigos das mensagens da mesma forma (`CliProgram.Classify`).
- Instalação suportada: OMSI 2. As capacidades nativas são resolvidas e verificadas por build no processo; uma capacidade indisponível em um build falha com `OL_E_CAPABILITY_UNRESOLVED`. Consulte [compatibilidade](compatibility.md).

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

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Finalidade | Compilar um `LaunchSpec` em um `SessionPlan` sem iniciar o OMSI: validar a especificação, detectar a plataforma, calcular a impressão digital de `Omsi.exe`, resolver as identidades de conteúdo, calcular as mutações de arquivo planejadas, listar as capacidades exigidas e as não suportadas e decidir `IsRunnable`. Capacidade pública `session.plan`. |
| Parâmetros | `spec`: um `LaunchSpec` totalmente preenchido (consulte a [referência do LaunchSpec](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (todos os oito dicionários) e `Behavior` não podem ser nulos; os membros opcionais podem ser `null`. `RootPath` deve ser um diretório absoluto; uma raiz vazia é registrada como `OL_E_INSTALLATION_NOT_FOUND`, mas a sondagem da plataforma em um caminho vazio lança `ArgumentException` antes que o plano seja retornado, portanto nunca passe uma raiz vazia. |
| Retorno | `SessionPlan` com um `SessionId` novo, `BuildProfileId` = o layout conhecido (`"Omsi23004"`) ou `"unrecognized"` para um build do OMSI 2 sem layout conhecido, o `Spec` de entrada, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (caminhos de destino `plugins\OmsiLaunch.*` mais `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (um `PlannedModule` por [módulo de runtime](runtime-modules.md) solicitado ou ativado por conteúdo; acessor `EffectivePlannedModules`). `IsRunnable` é `true` exatamente quando nenhum código de diagnóstico começa com `OL_E_`. Diagnósticos informativos (`plugin.integrity.reference` com mensagem `self` ou `manifest`, `session_profile.selected`) nunca tornam um plano não apto para execução. |
| Erros no resultado | Todo erro de planejamento é um diagnóstico, não uma exceção: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (a mensagem carrega o código de splash/ITX), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (a closure do plugin instalada em `plugins\` é verificada em relação ao manifesto de release no momento do planejamento), `OL_E_RUNTIME_ARTIFACT_MISSING` (a mensagem pode carregar `OL_E_RELEASE_MANIFEST_INVALID`). Condições completas: [regras de validação do LaunchSpec](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Exceções lançadas | `OperationCanceledException` se o token já estiver cancelado na entrada (o único ponto de verificação); `ArgumentException`/`NotSupportedException` para caminhos de raiz sintaticamente inválidos; `NullReferenceException`/`ArgumentNullException` para membros obrigatórios nulos; `System.Text.Json.JsonException` para um manifesto de release sintaticamente inválido. |
| Cancelamento | Verificado uma vez na entrada. Depois disso, o planejamento é trabalho síncrono no sistema de arquivos. |
| Exige sessão em execução | Não. |
| Altera o estado do OMSI | Não. |
| Altera o sistema de arquivos | Não (lê `Omsi.exe`, arquivos de conteúdo, a closure do plugin, o manifesto). Os valores de configuração não são validados aqui (somente a existência da chave e a possibilidade de escrita); um valor inválido falha no início com `OL_E_INVALID_SETTING_VALUE`. |
| Transação / restauração | Nenhuma. |
| Limitações | Solicitar qualquer modo de `Date`/`Time`/`Year` diferente de `Unset`, qualquer modo de `Weather` diferente de `Unset`, qualquer campo de `PlayerVehicle`, documentos de `Input`, `EntrypointIdentity` ou `WorldMode.LastMapState` produz `OL_E_CAPABILITY_UNAVAILABLE` e um plano não apto para execução nesta build (entradas em `UnsupportedRequestedFeatures` com a evidência do catálogo, `UNAVAILABLE` ou `PARTIAL`). |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Finalidade | Iniciar uma sessão gerenciada e transacional do OMSI a partir de um plano apto para execução: adquirir o lease da instalação, recuperar um journal obsoleto, validar a closure do plugin permanente, fazer snapshot dos arquivos da sessão e aplicar overlays sobre eles, criar o handoff de inicialização, o slot de telemetria e o mailbox de runtime, iniciar `Omsi.exe`, registrar o processo no journal e entregar a sessão a um supervisor em segundo plano. Capacidade pública `session.start`. |
| Parâmetros | `plan`: um `SessionPlan` com `IsRunnable == true`. A especificação dentro do plano é replanejada; do plano do chamador, somente `plan.SessionId` é mantido. `plan.Spec.Behavior.StartupTimeoutSeconds` deve estar em 1..600. |
| Retorno | `SessionHandle(plan.SessionId)` assim que `Omsi.exe` for criado e registrado (estado `WaitingForPlugin`), ou assim que o caminho de início falhar (estado `Failed`). Não aguarda o gameplay; use `WaitForAsync(session, SessionState.Running, ...)`. |
| Exceções lançadas | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` quando `plan.IsRunnable` é false; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` quando o replanejamento não está apto para execução (por exemplo, `Omsi.exe` foi alterado, conteúdo foi removido, closure do plugin ausente); `InvalidOperationException("Duplicate session id.")` quando uma sessão com o mesmo id ainda está registrada (chame `CloseAsync` antes); `ArgumentOutOfRangeException` quando `StartupTimeoutSeconds` está fora de 1..600; `OperationCanceledException` quando cancelado antes ou durante o replanejamento; e tudo o que `PlanSessionAsync` lança. Em todos os casos com exceção, nenhuma sessão é registrada. |
| Erros no resultado | Qualquer falha após o replanejamento é capturada dentro do caminho de início: a sessão é registrada, seu estado é `Failed` e seus diagnósticos contêm `OL_E_START_SESSION`, cuja mensagem é a mensagem interna (começando pelo código interno, quando houver): `OL_E_INSTALLATION_BUSY` (lease ocupado ou um processo do OMSI registrado no journal ainda vivo), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (somente quando a nova tentativa adiada com os overlays desta sessão ainda não consegue provar a propriedade), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. A limpeza pode acrescentar `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (saída do OMSI não confirmada; journal mantido) ou `OL_E_RESTORE_FAILED`. Falhas posteriores são relatadas pelo supervisor (consulte [ciclo de vida da sessão](../concepts/session-lifecycle.md)). |
| Cancelamento | Antes/durante o replanejamento: lança exceção. Depois disso, o token é repassado à transação e à criação do processo; um cancelamento nesse ponto é tratado como qualquer falha de início (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), o processo (se criado) é encerrado e a instalação é restaurada. |
| Exige sessão em execução | Não. |
| Altera o estado do OMSI | Sim: cria o processo do OMSI com as variáveis de ambiente `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Altera o sistema de arquivos | Sim, dentro da raiz da instalação: `.omsilaunch\diagnostics\<sessionId>-host.log` (retenção: as 50 sessões mais recentes), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (copiado uma vez para o splash gerenciado), overlays da sessão (patches de `options.cfg`, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), exclusões da sessão (destinos ITX, `Texture\standard.ipr`, `closecheck`) e remoção permanente de um `closecheck` obsoleto preexistente quando `SuppressStaleClosecheckWarning` é true (diagnóstico `closecheck.stale-removed`). |
| Transação / restauração | Abre a transação (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Todo caminho de saída da sessão termina em restauração. Consulte [transações e recuperação](../concepts/transactions-and-recovery.md). |
| Limitações | Somente `WorldMode.NewMap` com `PresentedEntrypointIndex` e `WorldMode.SavedSituation` chegam ao gameplay. `WorldMode.LastMapState` é `UNAVAILABLE`. Solicitações de veículo do jogador nunca chegam a este método, porque já são não aptas para execução no momento do planejamento. |
| Estabilidade | `STABLE_BETA` (os ciclos de vida NEW_MAP e SAVED_SITUATION são validados em runtime). |
| Exemplo | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidade | Ler o estado semântico do ciclo de vida, os diagnósticos coletados até o momento e a lista limitada de eventos de runtime. Capacidade pública `session.status`. |
| Parâmetros | `session`: um handle retornado por `StartSessionAsync` e ainda não fechado. |
| Retorno | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: um snapshot imutável (os arrays são copiados sob o lock da sessão). `RuntimeEvents` nunca é `null` para uma sessão ativa. |
| Exceções lançadas | `KeyNotFoundException` para handles desconhecidos/fechados. Fora isso, nunca lança exceção. |
| Cancelamento | O token é ignorado (a chamada é concluída de forma síncrona). |
| Exige sessão em execução | Não. |
| Altera OMSI / sistema de arquivos / transação | Não / Não / Nenhuma. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Finalidade | Consultar periodicamente (a cada 100 ms) até que a sessão esteja em `state`, ou em um estado terminal (`Completed`, `Failed`), ou até que o timeout expire; então retornar o status atual. |
| Parâmetros | `state`: qualquer `SessionState`. Aguardar um estado transitório que já passou (ou que nunca é definido, consulte [ciclo de vida da sessão](../concepts/session-lifecycle.md)) aguarda até um estado terminal ou até o timeout. `timeout`: qualquer `TimeSpan` não negativo ou `Timeout.InfiniteTimeSpan`. |
| Retorno | O status no momento em que a espera terminou. No timeout, o status é retornado, não uma exceção: verifique `State` você mesmo. Uma espera por `Running` que termina em `Failed` retorna imediatamente com os diagnósticos da falha. |
| Exceções lançadas | `KeyNotFoundException`; `OperationCanceledException` quando o token do chamador é cancelado (somente o cancelamento do chamador se propaga; o timeout interno não). |
| Cancelamento | O token do chamador é respeitado a cada tick de 100 ms. |
| Exige sessão em execução | Não. |
| Altera OMSI / sistema de arquivos / transação | Não / Não / Nenhuma. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidade | Solicitar a parada canônica. Define a flag de parada e retorna imediatamente; o supervisor observa a flag dentro do seu loop de 100 ms, chama `TerminateProcess` em `Omsi.exe`, aguarda a saída, marca o journal como `ProcessExited`, restaura todo arquivo de propriedade da sessão e libera o lease. Trata-se de um encerramento forçado: a rotina de desligamento do próprio OMSI não é executada e o OMSI não reescreve `options.cfg` ao sair (deliberado, protege a transação). O desligamento cooperativo via `WM_CLOSE` não está implementado (decisão de produto; no fechamento de runtime, o OMSI não fechou em até 30 s após `WM_CLOSE`, `L05b`). Capacidade pública `session.stop`. |
| Parâmetros | `session`. |
| Retorno | Tarefa concluída; não aguarda o encerramento nem a restauração. Use `WaitForAsync(session, SessionState.Completed, ...)` para observar a conclusão. |
| Exceções lançadas | `KeyNotFoundException`. |
| Cancelamento | Token ignorado. |
| Exige sessão em execução | Não. Idempotente; uma parada solicitada antes que o supervisor inicie é atendida assim que ele iniciar; uma parada em uma sessão terminal não tem efeito. |
| Altera o estado do OMSI | Sim: encerra o processo do OMSI (código de saída 1). |
| Altera o sistema de arquivos | Indiretamente: dispara a restauração, a exclusão do journal e a remoção dos backups pelo supervisor. |
| Transação / restauração | Dispara `ProcessExited` → `Restoring` → `Restored`. Alterações do lado do runtime feitas por meio de `ExecuteRuntimeAsync` (escritas no relógio, veículos gerados, variáveis de script, texturas D3D) não são restauradas; elas desaparecem com o processo. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Finalidade | Liberar o handle do consumidor sem deixar a transação abandonada: se a sessão não for terminal, solicitar a parada canônica; em seguida, aguardar a tarefa de ciclo de vida do supervisor (saída do processo, restauração, liberação do lease); depois, esquecer a sessão. |
| Parâmetros | `session`. |
| Retorno | É concluído quando a sessão está terminal e foi removida. Depois que retorna, o handle é desconhecido (`KeyNotFoundException` em qualquer chamada posterior, inclusive um segundo `CloseAsync`). |
| Exceções lançadas | `KeyNotFoundException`; `OperationCanceledException` se o chamador cancelar enquanto aguarda o supervisor. Nesse caso, a sessão não é removida e o supervisor continua em execução; chame `CloseAsync` novamente. |
| Cancelamento | Aplica-se somente à espera; nunca cancela a restauração. |
| Exige sessão em execução | Não. |
| Altera o estado do OMSI | Sim, quando a sessão ainda está ativa (igual a `StopAsync`). |
| Altera o sistema de arquivos | Indiretamente (restauração pelo supervisor). |
| Transação / restauração | Garante que a transação seja levada até a conclusão antes que o handle seja liberado (quando o supervisor foi iniciado). Para uma sessão que falhou antes do início do supervisor, o caminho de início já restaurou ou relatou `OL_E_RESTORE_DEFERRED`. |
| Estabilidade | `STABLE_BETA`. |
| Exemplo | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Finalidade | Executar uma operação de runtime pública dentro do processo do OMSI em execução por meio do mailbox da sessão, de voo único (mapeado em memória, 64 KiB, requisição vinculada ao id da sessão e ao id da requisição). O plugin executa a operação no thread de UI do OMSI. Catálogo de operações: [controle de runtime](runtime-control.md) e [capacidades](capabilities.md). |
| Parâmetros | `command.SessionId` deve ser igual a `session.SessionId`. `command.RequestId`: `ulong` escolhido pelo chamador; use um contador estritamente crescente por processo (os helpers de D3D começam em 30 000, o proprietário da CLI em 10 001/50 000). `command.Operation`: um id de operação pública de `PublicCapabilityRegistry.PublicRuntimeOperationIds` (por exemplo, `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: valores string indexados por nomes ordinais; os nomes obrigatórios por operação vêm de `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: medido a partir do momento em que a requisição é colocada no mailbox (a espera na fila atrás de outro comando em andamento não é contada). A CLI usa 5 s (15 s para `road-vehicles.spawn`) como proprietário e 8 s / 30 s como cliente. |
| Ordem das verificações | 1. Validação no registro (antes da busca da sessão): operação desconhecida ou `internal.*` → resultado `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; argumento obrigatório ausente (inexistente ou só com espaços em branco) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Busca da sessão → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. Estado diferente de `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Requisição ao mailbox. 6. Valores do resultado cuja chave começa com `internal_` ou termina com `_address`, `_pointer`, `_vmt` são removidos. |
| Retorno | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. Em caso de sucesso, `Values` contém as strings semânticas da operação (documentadas por operação em [controle de runtime](runtime-control.md)). |
| Erros no resultado | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (registro); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (o resultado do plugin excedeu o mailbox; resultados de listas limitadas são, em vez disso, encurtados com `truncated=true`); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` com uma chave derivada); todo código `OL_E_D3D_*` (com `Values["detail"]` e `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (a capacidade nativa da operação não está resolvida no build em execução); e toda falha tipada do lado do plugin com seu próprio código em `ErrorCode` e a mensagem em `Values["detail"]` (Beta 4; somente uma falha sem código catalogado é `OL_E_RUNTIME_OPERATION_FAILED`). Códigos que chegam dessa forma: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (verificações do lado do plugin), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. Consulte [códigos de erro](errors.md). |
| Exceções lançadas | `KeyNotFoundException`; `InvalidOperationException` com `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (mailbox já descartado pelo supervisor), `OL_E_RUNTIME_CHANNEL_BUSY` (o slot ainda contém uma requisição abandonada), `OL_E_RUNTIME_REQUEST_ID_REUSED` (uma resposta obsoleta para o mesmo id de requisição ainda está no slot); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (resposta corrompida, estranha ou não correspondente); `ArgumentOutOfRangeException` quando a requisição serializada excede o mailbox; `OperationCanceledException`. |
| Cancelamento | Respeitado durante a espera pelo gate por sessão e a cada 20 ms durante a consulta periódica da resposta. Cancelar com a requisição em andamento não reinicia o slot: a próxima chamada nessa sessão pode falhar com `OL_E_RUNTIME_CHANNEL_BUSY` até que o plugin publique sua resposta (que é então descartada como obsoleta). Prefira o timeout; um timeout reinicia o slot, e uma resposta tardia é detectada e descartada. |
| Exige sessão em execução | Sim (`SessionState.Running`); caso contrário, `OL_E_SESSION_NOT_RUNNING` é lançado. O mailbox existe até que o supervisor o descarte durante a restauração. |
| Altera o estado do OMSI | Depende da operação: operações `Read` não alteram; operações `Write`/`Action` (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) alteram estado dentro do processo que não é restaurado. |
| Altera o sistema de arquivos | Nenhuma escrita pelo host. O OMSI pode escrever seus próprios arquivos como consequência (não rastreado). |
| Transação / restauração | Nenhuma. |
| Limitações | Um comando em andamento por sessão (chamadas na mesma sessão são serializadas). Requisição e resposta são limitadas, cada uma, a 64 KiB menos 8 bytes; payloads de pixels D3D, a 48 KiB. `internal.road-vehicles.make-basic` é `INTERNAL` e inacessível. `weather.set` e `weather.metar.set` são `EXPERIMENTAL`. `timetable.logs.read` não é limitada e pode retornar `OL_E_RUNTIME_RESPONSE_TOO_LARGE` em tabelas de horários grandes. `camera.lock` é `EXPERIMENTAL`; exige um veículo do jogador e está validada em runtime (`CAM01`), embora a string `RuntimeValidation` do registro ainda indique `STATICALLY_VALIDATED`. Os handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) têm escopo de sessão. |
| Estabilidade | Transporte e contrato `STABLE_BETA`; a estabilidade por operação segue `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`), com as exceções acima. |
| Exemplo | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidade | Retornar o catálogo de capacidades avaliado para uma instalação: uma `Capability(Name, Available, EvidenceState, Reason)` por entrada de `PublicCapabilityRegistry` que não seja `InternalOnly`. `Name` é o id do catálogo e `EvidenceState` a evidência do catálogo (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` é calculado a partir da plataforma, da verificação de OMSI 2, da classificação e das capacidades nativas de que a entrada precisa (consulte [capacidades](capabilities.md#getcapabilitiesasync)). |
| Parâmetros | `installation.RootPath`: a instalação sondada (plataforma, `Omsi.exe`). `ExpectedExecutableSha256` é ignorado. |
| Retorno | Uma entrada por entrada não interna do catálogo, por exemplo `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` no layout conhecido), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relação com `PublicCapabilityRegistry` | Derivada dele: o registro declara as capacidades, suas operações e sua evidência; esta chamada as avalia para uma instalação. |
| Exceções lançadas | `OperationCanceledException` na entrada; `ArgumentException` para um caminho de raiz vazio. |
| Cancelamento | Verificado uma vez na entrada. |
| Exige sessão em execução | Não. |
| Altera OMSI / sistema de arquivos / transação | Não / Não / Nenhuma. |
| Estabilidade | Contrato da chamada `STABLE_BETA`; o conteúdo da lista é um inventário mantido manualmente: `PARTIAL`. |
| Exemplo | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Finalidade | Enumerar o conteúdo instalado e retornar identidades canônicas utilizáveis em um `LaunchSpec`. A descoberta ignora pontos de nova análise (reparse points) (ciclos de junções não conseguem travá-la), lê os arquivos do OMSI como Windows-1252 (UTF-8/UTF-16 marcados com BOM são respeitados) e nunca segue links simbólicos. |
| Parâmetros | `query` e `scope` conforme a tabela abaixo. `scope` é obrigatório para `Entrypoints` (identidade do mapa), `Repaints`, `FleetNumbers`, `Registrations` (identidade do veículo). |
| Retorno | Lista ordenada de `ContentIdentity(Identity, Kind, DisplayName)`. As identidades são caminhos relativos à instalação com barras invertidas; as comparações não diferenciam maiúsculas de minúsculas. |
| Exceções lançadas | `OperationCanceledException` na entrada; `ArgumentException` quando `Entrypoints` é consultado sem escopo ou a raiz está vazia; `FileNotFoundException` (sem código `OL_E_`; a CLI o mapeia para `OL_E_NOT_FOUND`) quando o mapa ou veículo do escopo não está instalado. Uma raiz ou diretório de conteúdo ausente produz uma lista vazia, não um erro. |
| Cancelamento | Verificado uma vez na entrada. |
| Exige sessão em execução | Não. |
| Altera OMSI / sistema de arquivos / transação | Não / Não / Nenhuma. |
| Estabilidade | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (todo plano validado em runtime é resolvido por meio deles). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (somente evidência estática). |
| Exemplo | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

Valores e resultados de `ContentQueryKind`:

| Valor | Escopo | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | nenhum | `maps\<dir>\global.cfg` | `map` | nome do diretório do mapa |
| `Situations` | nenhum | `situations\...\<file>.osn` | `situation` | identidade do mapa referenciado pelo `.osn` (pode ser `null`) |
| `Vehicles` | nenhum | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` ou nome do arquivo |
| `Repaints` | identidade do veículo (obrigatória; sem ela: lista vazia) | `<cti path>#item:<ordinal>` | `repaint` | nome de `[item]` |
| `Hofs` | nenhum | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | identidade do veículo (obrigatória; sem ela: lista vazia) | caminho de origem de `[number]` relativo ao veículo | `fleet-number` | `null` |
| `Registrations` | identidade do veículo (obrigatória; sem ela: lista vazia) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | primeira linha de valor (`null` para free) |
| `Addons` | nenhum | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | identidade do mapa (obrigatória; sem ela: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | rótulo do ponto de entrada |
| `SessionProfiles` | nenhum | nome do diretório do pacote (o id de `/predefined-profile`) | `session-profile`, ou `session-profile-invalid` quando o pacote não compila | `name` do perfil, ou o código `OL_E_SESSION_PROFILE_*` de um pacote inválido |
| `RuntimeModules` | nenhum | id do módulo | `runtime-module`, ou `runtime-module-invalid` quando não pode ser carregado | `name` do módulo, ou o código `OL_E_MODULE_*` |

Um ponto de entrada é selecionado por `PresentedEntrypointIndex` ou por `EntrypointIdentity` (uma identidade de descoberta ou um rótulo exato). O planejador resolve uma identidade para o rótulo que o OMSI apresenta (o OMSI apresenta apenas o primeiro registro de cada rótulo exato) e o plano o lista em `ResolvedContent` com o tipo `entrypoint`; `world.entrypoint-identity` é `EXPERIMENTAL` até ser validada em runtime.

### `ListSessionProfilesAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidade | Listar todo pacote de perfil de sessão em `<root>\.omsilaunch\session-profiles\` com suas predefinições, para que um frontend possa oferecê-los sem interpretar YAML. CLI `/list:SessionProfiles`. |
| Retorno | Um `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` por diretório de pacote, ordenado por `Id`. `Presets` são `SessionProfilePresetSummary(Index, Id, Name)` na ordem do índice. Cada pacote é compilado com as mesmas regras estritas de uma inicialização (`SessionProfileCompiler`); um pacote que não compila é listado com `IsValid = false`, seu código `OL_E_SESSION_PROFILE_*` e sua mensagem, e sem predefinições. Sem diretório de perfis: lista vazia. |
| Exceções lançadas | `OperationCanceledException` na entrada. Os erros de compilação são relatados no resultado, não lançados. |
| Exige sessão em execução | Não. |
| Altera OMSI / sistema de arquivos / transação | Não / Não / Nenhuma. |
| Estabilidade | `STABLE_BETA` (gate offline `session-profiles.discovery`). |
| Exemplo | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Finalidade | Descobrir os [módulos de runtime](runtime-modules.md) instalados em `plugins\OmsiLaunch.Modules` com sua integridade e suas configurações. Somente leitura. O carregamento é aberto: nenhum módulo é conferido contra uma lista ou assinatura. |
| Retorno | Um `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` por diretório de módulo, ordenado pelo nome do diretório. `Activation` é `requested`, `content` ou `default`; `IsValid` é falso com um código `OL_E_MODULE_*` quando o módulo não pode ser carregado; `Settings` lista as configurações que o módulo declara como `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Exceções lançadas | `OperationCanceledException` na entrada. |
| Exige sessão em execução | Não. CLI: `/list:RuntimeModules` (envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspecto | Detalhe |
| --- | --- |
| Assinatura | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Finalidade | Relatar ou concluir uma transação durável obsoleta (`<root>\.omsilaunch\journal.json`) deixada por um proprietário que travou. Adquire o lease da instalação durante a chamada, para nunca restaurar por baixo de uma sessão que está iniciando. Capacidade pública `session.recover`; CLI `/recovery-status` e `/recover`. |
| Parâmetros | `installation.RootPath`: a raiz da instalação (normalizada com `Path.GetFullPath`). `restore`: `false` = somente relatar; `true` = restaurar, verificar, excluir o journal e os backups. |
| Retorno | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = havia um journal quando a chamada começou; `Recovered` = uma restauração foi solicitada, foi executada e nenhum journal permanece; `Diagnostics` = notas da restauração (`restore.session-artifact-removed` com `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), vazio quando nada foi restaurado. |
| Exceções lançadas | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` quando o lease está ocupado; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` quando o PID + horário de criação + caminho do executável registrados no journal ainda correspondem a um processo ativo ou (journal além de `HandoffCreated` sem PID) quando qualquer `Omsi.exe` dessa raiz está em execução; `IOException` com `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` ou uma mensagem de verificação ("Restore hash mismatch: ...", "Restore presence mismatch: ..."); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` para um journal corrompido; `ArgumentException` para uma raiz vazia; `OperationCanceledException`. Sempre que lança exceção depois que uma restauração começou, o journal é mantido e a próxima chamada reexecuta de forma idempotente. |
| Cancelamento | Repassado às escritas do journal/backup; cancelar no meio da restauração deixa o journal pendente. |
| Exige sessão em execução | Não (recusa enquanto um proprietário está ativo). |
| Altera o estado do OMSI | Não. |
| Altera o sistema de arquivos | Somente com `restore == true`: reescreve os originais a partir de backups verificados (bytes, horário da última escrita, horário de criação, atributos; originais somente leitura são tratados; write-through + flush; nenhum `*.omsilaunch.tmp` é deixado), remove os artefatos da sessão, exclui `journal.json` e `backup\<sessionId>`. |
| Transação / restauração | Conclui a transação pendente (`Restoring` → `Restored` → journal removido). |
| Estabilidade | `STABLE_BETA`: caminho de relatório e recuperação após saída antecipada (matriz RV-008), recuperação após uma restauração com falha (fechamento de runtime `F01`), recusa sob um proprietário ativo e na janela anterior ao PID (`S04`) e recuperação adiada anterior à impressão digital no início (`S05`); consulte [status da validação em runtime](../status/runtime-validation-status.md). |
| Exemplo | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## Tipos de contrato

<a id="optional-values-and-semantic-primitives"></a>
### Valores opcionais e primitivos semânticos

| Tipo | Definição | Observações |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, `Unset` estático, `Set(T)` estático | Distingue "não solicitado" de "solicitado com um valor". O formato JSON está documentado na [referência do LaunchSpec](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Enum de byte. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Validado somente quando `DateTimeMode.Explicit` (mês 1..12, dia 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Validado somente quando `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

<a id="launchspec-family"></a>
### Família LaunchSpec

Todos os records abaixo estão documentados propriedade por propriedade na [referência do LaunchSpec](launchspec.md); esta tabela fixa o inventário de tipos.

| Tipo | Finalidade | Estabilidade |
| --- | --- | --- |
| `LaunchSpec` | Record raiz da requisição com os acessores `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures`, que substituem membros opcionais `null` por valores padrão. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (fixação opcional do `Omsi.exe`, verificada no planejamento). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | Seleção do mundo. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (alias obsoleto de `LastMapState`; nunca "o .osn mais recente"). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (calculado a partir de `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Qualquer modo diferente de `Unset` torna o plano não apto para execução. | `PARTIAL` (evidência do plano `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Qualquer modo diferente de `Unset` torna o plano não apto para execução. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Qualquer campo definido torna o plano não apto para execução. | `PARTIAL` |
| `EnvironmentSpec` | Oito grupos `IReadOnlyDictionary<string, OptionalValue<string>>` de configurações semânticas de `options.cfg`. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; qualquer valor definido torna o plano não apto para execução. | `PARTIAL` |
| `DiagnosticsSpec` | Seis booleanos; transportados, não consumidos. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Procedência de um perfil de sessão compilado (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | [Gerenciamento de HOF](hof-management.md) da sessão: `Operations` (lista de `HofOperationSpec`). Campo `LaunchSpec.HofManagement`; acessor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Módulos de runtime](runtime-modules.md) da sessão: `Enable` (ids de módulos), `Disable` (ids de módulos mantidos de fora), `Settings` (`"<module id>.<key>"` para valor); acessores `EffectiveDisable`, `EffectiveSettings`. Campo `LaunchSpec.Modules`; acessor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | A avaliação de plano de um módulo: `Id`, `Version`, `Status` (`ELIGIBLE` ou `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` ou `default`), `Settings`, `RuntimeOperations` (operações do catálogo que o módulo executa em processo, cada uma com sua política de mutação quando altera o OMSI, por exemplo `camera.set (omsi-state)`), `TickMilliseconds`, `SnapshotSha256` (o digest, calculado no momento do plano, dos arquivos do módulo, consulte [módulos de runtime](runtime-modules.md#integrity)); `IsEligible`, `EffectiveSettings`, `EffectiveRuntimeOperations`. Constantes `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | Uma configuração efetiva de módulo: `Key`, `Value`, `Source` (`request`, `settings-file` ou `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Visão de descoberta de um módulo instalado; consulte `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | Uma configuração que um módulo declara: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | Uma operação de HOF (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Lançada pelo planejamento de HOF (`OmsiLaunch.Core`); `Code` é um código `OL_E_HOF_*`. A API pública a relata como diagnósticos do plano. | `STABLE_BETA` |
| `SessionProfileSummary` | Visão de descoberta de um pacote (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). Consulte `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | Uma predefinição em um resumo (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (transportado; a restauração sempre acontece), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, padrão 180), `ShutdownTimeoutSeconds` (transportado, não consumido). | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### Tipos de plano e de status

| Tipo | Campos | Observações |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (novo `Guid` por plano), `BuildProfileId` (`"Omsi23004"` ou `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (lista de `ContentIdentity`: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (caminhos relativos distintos de `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` com `STATICALLY_VALIDATED` ou `UNAVAILABLE`; capacidades nativas de início com `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (entradas `Capability` para recursos solicitados, mas não suportados), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | Um record público: pode ser editado ou ficar desatualizado, e é por isso que `StartSessionAsync` replaneja. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, SO x64 e host x64), `LegacyPlatform` (sempre `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (todos iguais a `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` é um valor de `PublicEvidence` (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), exceto em `SessionPlan.RequiredCapabilities`: um requisito de plataforma ou de conteúdo é `STATICALLY_VALIDATED` ou `UNAVAILABLE`, e uma capacidade nativa é `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Mutações de apresentação usam estas chaves: `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (mapa de strings opcional). | Códigos que começam com `OL_E_` são erros, `OL_W_` são avisos e qualquer outro é informativo. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Os diagnósticos da sessão não incluem os diagnósticos do plano. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (horário de recebimento pelo host), `Sequence` (começando em 1, por sessão), `Data`, `SessionTickMilliseconds` (o relógio monotônico de sessão do plugin quando emitiu o evento; nulo para eventos do host). | Limitado aos 256 eventos mais recentes (os mais antigos são descartados). Os eventos do plugin chegam em ordem pelo anel de telemetria; os eventos perdidos por estouro do anel são contados por `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Local ao processo. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | Consulte `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | Consulte `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Enum de byte na ordem de declaração: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` e `EnteringGameplay` nunca são definidos pelo serviço atual; `Snapshotting` é transitório e, na prática, não observável. Estados terminais: `Completed`, `Failed`. Semântica completa: [ciclo de vida da sessão](../concepts/session-lifecycle.md).

<a id="runtime-control-types"></a>
### Tipos de controle de runtime

| Tipo | Definição | Estabilidade |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Codec estático usado pelo host e pelo plugin para o envelope do mailbox: magic `0x4F4C5243` ("OLRC"), versão 1, cabeçalho little-endian de 72 bytes (magic, versão, tipo 1 = requisição / 2 = resposta, comprimento total, `Guid` da sessão, id da requisição, comprimento do payload, SHA-256 do payload) seguido de um payload JSON em UTF-8. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: público porque as duas extremidades da ponte o compartilham; não é uma superfície de integração; o formato pode mudar com a versão do protocolo. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — o que o host publica para o plugin no arquivo mapeado em memória `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256, string Settings, string SnapshotSha256)`: um módulo planejado levado pelo handoff (`StartupHandoff.Modules`); `SnapshotSha256` (versão de wire 8) é o digest dos arquivos do módulo calculado no planejamento, vazio quando o plano não trouxe nenhum. | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: o relógio de início resolvido de uma sessão NEW_MAP, levado pelo handoff (`StartupHandoff.Start`); um campo nulo mantém o valor do próprio OMSI. | `INTERNAL` |
| `StartupHandoffWire` | Codec: magic `0x4F4C5348`, versão 8 (lê de 3 a 8; a versão 5 acrescenta os módulos planejados, a versão 6 as configurações efetivas de cada módulo, a versão 7 a data/hora de início, a versão 8 o snapshot de arquivos de cada módulo no momento do plano; os bits de flag 0x04 e 0x08 levam `PluginTrace` e `NativeTrace`), cabeçalho de 64 bytes com integridade do payload por SHA-256. | `INTERNAL` |
| `TelemetryRingWire` | Anel de eventos do plugin para o host no mapeamento `OMSILAUNCH_TELEMETRY_NAME`: magic `0x52544C4F` ("OLTR"), versão 1, 64 slots numerados de 2048 bytes. `Initialize`, `Write`, `IsRing`, `ReadSince` (retorna os eventos após um número de sequência e quantos foram perdidos). | `INTERNAL` |

O plugin rejeita um handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`), a menos que `WorldMode` seja `NewMap` ou `SavedSituation`, `HeadlessStart` seja true, `PlayerVehicleEnabled` seja false, ambos os modos de data/hora sejam `Unset` e uma situação salva tenha uma identidade não vazia. O planejador aplica as mesmas restrições antes, portanto um plano apto para execução nunca provoca essa rejeição.

<a id="capability-registry-types"></a>
### Tipos do registro de capacidades

| Tipo | Finalidade |
| --- | --- |
| `PublicCapabilityRegistry` | O catálogo canônico de capacidades. `ProtocolVersion` (`"0.1"`), `All` (todo `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (os 48 ids de operação concretos que um frontend pode encaminhar), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (rota de comando para id de operação), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (retorna `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Aplicado por `ExecuteRuntimeAsync`, pela CLI, pelo plano de controle local e pelo plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (um valor de `PublicEvidence`), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; derivados `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (recebe `cursor`/`limit`, consulte [paginação](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | O que uma operação de runtime altera: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | O vocabulário de evidência: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Catálogo completo: [capacidades](capabilities.md).

<a id="d3druntimeapi-extension-methods"></a>
### Métodos de extensão `D3DRuntimeApi`

Wrappers tipados sobre `ExecuteRuntimeAsync` para as operações `d3d.*` (`EXPERIMENTAL`, capacidade `PublicExperimental` `d3d.texture`). Eles alocam ids de requisição a partir de um contador global do processo que começa em 30 000 e usam 5 s como timeout padrão (exceto `GetD3DStatusAsync`, que exige um timeout).

| Método | Operação | Argumentos e limites |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | nenhum |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | largura/altura 1..4096, níveis 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | nível 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, largura/altura 1..4096, pixels ≤ 48 KiB (codificados em Base64 na transmissão) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | uma liberação repetida é rejeitada com `OL_E_D3D_RESOURCE_RELEASED` |

Tipos: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` com `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Erros: um resultado com falha é relançado como `OmsiRuntimeException(Code, detail)`, em que `Code` é o `ErrorCode` do resultado (ou `OL_E_RUNTIME_OPERATION_FAILED` quando ausente) e a mensagem é `"<code>: <Values["detail"]>"`; um resultado bem-sucedido sem valores, ou uma string de estado do dispositivo desconhecida, lança `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Tudo o que `ExecuteRuntimeAsync` lança se propaga sem alteração. O tratamento do reset do dispositivo está validado em runtime: um reset leva o dispositivo por `Resetting` de volta a `Ready` e invalida toda textura ativa (`OL_E_D3D_STALE_RESOURCE_HANDLE`, fechamento de runtime `D01`); a entrada do catálogo `d3d.texture` o registra. A transição para `Lost` não pode ser produzida de fora do produto e é coberta somente offline.

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
### Tipos de contrato de processo

| Tipo | Conteúdo |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Usado somente pela CLI ([códigos de saída](exit-codes.md)); a API nunca encerra o processo. |
| `PublicErrorCategory` | Constantes string usadas nos envelopes de erro da CLI/controle: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | Uma `const string` por código (143: 142 erros `OL_E_` e 1 aviso `OL_W_`) e `All`, o catálogo `PublicErrorDescriptor(Code, Category)`. Categorias: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Referência: [códigos de erro](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | Propriedade `Code` mais a mensagem; lançada somente por `D3DRuntimeApi`. |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths` (identidade da instalação e contenção de caminhos)

Estabilidade: `STABLE_BETA` (funções puras, sem E/S, sem estado do OMSI, sem alteração do sistema de arquivos, sem participação em transação, sem exigir sessão em execução). É a definição única usada pelo lease da instalação, pelo nome do pipe de controle local, pelo confinamento de assets do perfil de sessão, pela validação de destinos das texturas da internet e pelo caminho do modelo para spawn em runtime.

| Membro | Comportamento |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` sem separadores finais, exceto na raiz de uma unidade (`C:\`), que é mantida. Resolve segmentos `.` e `..`, trata `/` e `\` da mesma forma e reduz separadores repetidos. **Não** resolve junções nem links simbólicos. Lança `ArgumentException` para uma raiz nula ou em branco. |
| `string IdentityKey(string root)` | `NormalizeRoot(root)` em maiúsculas. Grafias lexicalmente equivalentes de uma mesma raiz (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) compartilham uma chave; raízes diferentes (`C:\OMSI-A`, `C:\OMSI-B`) nunca compartilham. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Resolve `candidate` (relativo a `root`, ou absoluto) e retorna `true` somente quando ele está estritamente abaixo de `root`; `relativePath` é a grafia canônica com `\`. Usa os segmentos de `Path.GetRelativePath`, de modo que um irmão como `C:\OMSI-A\x` nunca está dentro de `C:\OMSI`; a própria raiz, outros volumes e escapes com `..` retornam `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Divide em `/` e `\`, descartando segmentos vazios. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### Perfis de sessão (`OmsiLaunch.Core`)

Estabilidade: `EXPERIMENTAL`. Estes tipos compilam um [perfil de sessão](session-profiles.md) YAML (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, schema `omsilaunch.session-profile/v1`) em um `LaunchSpec`. O comando da CLI `/predefined-profile:<id> /predefined-profile-index:<n>` usa exatamente estas chamadas; um integrador pode usá-las para iniciar um perfil pela API.

| Membro | Comportamento |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Lê e valida o pacote. `id` deve ser um nome de diretório simples (caso contrário, `OL_E_SESSION_PROFILE_PATH_ESCAPE`); `presetIndex` é 1..99 (`MinPresetIndex`..`MaxPresetIndex`; caso contrário, `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). Arquivo ausente: `OL_E_SESSION_PROFILE_NOT_FOUND`; maior que `MaxBytes` (256 KiB), YAML inválido, âncoras, chaves desconhecidas ou um `id` diferente do nome do diretório: `OL_E_SESSION_PROFILE_INVALID`; outro `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Os caminhos de assets ficam confinados ao pacote (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Toda falha é uma `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Retorna `baseline` com o perfil aplicado: para `NewMap`, o bloco `new` do perfil (mapa, ponto de entrada e qualquer data/hora/ano/clima); as `settings` da predefinição mescladas sobre `Environment.General`; `Presentation`, `InternetTextures` e `Behavior` da predefinição, quando presentes; e `SessionProfile` = `profile.Metadata`. Para `NewMap`, também verifica o mapa em relação à lista `compatibility` do perfil (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | A verificação de compatibilidade para os demais modos (para `SavedSituation`, o mapa é lido do `.osn`). A CLI a chama depois que o `WorldSpec` final é construído. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144` e as chaves aceitas por mapeamento YAML. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | O pacote carregado; `Preset` é somente a predefinição selecionada. |
| `SessionProfileException(string code, string message)` | `IOException` com `Code` (um dos códigos `OL_E_SESSION_PROFILE_*`); a mensagem é `"<code>: <message>"`. |

A CLI também rejeita flags de linha de comando que conflitam com o perfil (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); essa verificação não faz parte do compilador. Consulte [perfis de sessão](session-profiles.md#precedence-and-override-conflicts) para a ordem completa de mesclagem.

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

| Tipo | Estabilidade | Uso |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` como tipo do parâmetro do construtor de `OmsiLaunchService` | Detecta a plataforma, verifica a possibilidade de escrita, inicia, observa, encerra e aguarda `Omsi.exe`. Passe `new CurrentWindowsX64Platform()`; implementá-lo por conta própria não é suportado. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | A única implementação: host Windows x64, `CreateProcessW` para `Omsi.exe`, `TerminateProcess` para a parada canônica. Seus métodos são chamados pelo serviço; integradores apenas o constroem. Membros (compartilhados com `IRuntimePlatform`): `Detect(root)` retorna o `RuntimePlatformInfo` do plano; `ValidateCurrent(info)` lança `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` quando o host não consegue executar uma sessão; `IsInstallationWritable(root)` fundamenta `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` cria `Omsi.exe` e registra a identidade do processo (PID, horário de criação, caminho e o hash calculado pelo serviço; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` e `Terminate` o observam e o encerram. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Públicos no assembly porque o serviço e os testes os compartilham. Não são uma superfície de integração; `LaunchedProcess` encapsula internamente os handles de processo e de thread do OMSI (eles não são membros públicos) e nunca é retornado por `IOmsiLaunch`. |

<a id="thread-safety"></a>
## Segurança de threads

- `OmsiLaunchService` é seguro para chamadas simultâneas em sessões diferentes: as sessões ficam em um `ConcurrentDictionary` e toda alteração por sessão acontece sob o lock privado da sessão.
- Chamadas simultâneas na mesma sessão são seguras, mas serializadas onde isso importa: `ExecuteRuntimeAsync` adquire um gate por sessão, de modo que um segundo comando aguarda o primeiro (seu timeout começa quando ele é colocado no mailbox).
- O supervisor é executado em uma tarefa do pool de threads (`Task.Run`) desde o momento em que `StartSessionAsync` retorna até que a sessão seja terminal; ele consulta a telemetria e o processo a cada 100 ms. Os chamadores nunca executam código do supervisor.
- `StopAsync` e `GetStatusAsync` são concluídos de forma síncrona e podem ser chamados de qualquer thread, inclusive dentro de um handler de `ProcessExit` (a CLI faz isso com um orçamento de 4 s).
- Nenhuma chamada da API tem afinidade de thread; nenhuma exige um contexto de sincronização.

<a id="what-is-not-in-the-api"></a>
## O que não está na API

- Nenhum `IntPtr`, `nint`, handle Win32, endereço nativo, ponteiro de VMT ou objeto de processo. Valores de resultado cujas chaves começam com `internal_` ou terminam com `_address`, `_pointer`, `_vmt` são removidos antes que um resultado saia de `ExecuteRuntimeAsync`.
- Nenhuma operação de runtime `internal.*`: `internal.road-vehicles.make-basic` é `InternalOnly` no registro e retorna `OL_E_RUNTIME_OPERATION_UNKNOWN` pela API e pela CLI.
- Nenhuma leitura ou escrita bruta da memória do OMSI, nenhum acesso em nível de arquivo à instalação além do que um `LaunchSpec` declara.
- Nenhum handle entre processos: o [plano de controle local](local-control.md) é a única rota entre processos, e ele aceita somente `session.status`, `session.events`, `session.stop` e `runtime.execute`.
- Nenhuma aplicação de veículo do jogador na inicialização. O desligamento cooperativo, `LAST_MAP_STATE`, data/hora/clima de início e os overlays de documento de teclado/controle são `EXPERIMENTAL`.

<a id="stability-summary"></a>
## Resumo de estabilidade

| Superfície | Estabilidade |
| --- | --- |
| Construtor de `OmsiLaunchService`, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| Transporte de `ExecuteRuntimeAsync`; operações `PublicStableBeta` | `STABLE_BETA` |
| Operações `PublicExperimental`, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| membros de especificação de data/hora/clima/entrada, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| membros de especificação de veículo do jogador | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, implementações de `IRuntimePlatform`, todos os assemblies de implementação | `INTERNAL` |
| `WorldMode.LastSituation`, operações `internal.*` | `UNAVAILABLE` |
