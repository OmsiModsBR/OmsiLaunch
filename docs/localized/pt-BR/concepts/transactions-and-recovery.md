# Transações e recuperação

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Tradução da [página original em inglês](../../../concepts/transactions-and-recovery.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Toda sessão do OmsiLaunch que altera um arquivo do OMSI faz isso dentro de uma transação durável, registrada no journal: os bytes originais são copiados para backup antes de serem substituídos, o journal registra até onde a sessão chegou e a restauração verifica cada backup antes de gravá-lo de volta. Esta página descreve essa transação conforme implementada por `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) e conduzida por `OmsiLaunchService.StartAsync`, `SuperviseAsync` e `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), junto com as entradas de arquivo calculadas por `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Ela foi escrita para usuários que precisam saber o que uma sessão altera e o que a recuperação faz, e para integradores que precisam das garantias exatas.

<a id="what-a-session-changes"></a>
## O que uma sessão altera

Somente **overlays temporários** entram na transação. Eles são calculados antes de a transação ser aberta e restaurados quando ela é fechada.

| Entrada da sessão | Arquivo(s) | Tipo |
| --- | --- | --- |
| `/set:<key>=<value>`, `settings` do perfil, `LaunchSpec.Environment.*` | `options.cfg` (patches semânticos de tokens; bytes CP1252 preservados, UTF-8/UTF-16 marcados com BOM respeitados) | overlay |
| Splash gerenciado (`SplashMode.Managed`, o padrão) | `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<language>.bmp` | overlay (o arquivo localizado é criado pela transação quando a instalação não tem nenhum) |
| Texturas da internet `Override` | `Texture\standard.itx` | overlay |
| Texturas da internet `Override` | todo alvo listado no `.itx`, mais `Texture\standard.ipr` | exclusão da sessão |
| Sempre | `closecheck` (quando ausente antes da sessão) | exclusão da sessão |

Os arquivos permanentes do produto **não** são participantes da transação: o conjunto de arquivos do plugin em `plugins\OmsiLaunch.*` (apenas validado, veja [plugin permanente](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (copiados uma vez, nunca removidos), os diagnósticos em `.omsilaunch\diagnostics`, os pacotes de perfil de sessão e a documentação e os exemplos do release. Plugins de terceiros e todos os demais arquivos do OMSI nunca são enumerados, copiados, removidos nem restaurados.

O próprio OMSI continua gravando seu próprio estado enquanto uma sessão está em execução, exatamente como em uma inicialização normal do OMSI: `options.cfg` (por exemplo `[last_map]` quando a sessão carrega um mapa diferente, regravado na entrada no gameplay), `Texture\standard.ipr`, caches de horários e de lightmaps (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, o perfil de motorista em `Drivers\` e seus logs. Uma gravação em um caminho pertencente à sessão (acima) é desfeita pela restauração; todas as demais gravações do OMSI persistem após a sessão, exatamente como persistiriam após executar o OMSI diretamente. Evidência da rodada de fechamento de runtime: uma sessão de situação salva em outro mapa deixou `[last_map]` alterado porque não aplicou overlay em `options.cfg` (`CAM01`), enquanto sessões com `/set` restauraram `options.cfg` exatamente (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Estados da transação

`TransactionState` é persistido no journal após cada transição. Os valores são serializados como inteiros por `System.Text.Json`.

| Valor | Estado | Gravado quando |
| --- | --- | --- |
| 0 | `Prepared` | Os snapshots de todos os caminhos de overlay e de exclusão foram feitos e seus backups gravados em disco. Nada na instalação mudou ainda. Esta é a obrigação de recuperação: a partir daqui, uma falha grave deixa um journal recuperável. |
| 1 | `Applied` | Todo overlay foi gravado atomicamente e toda exclusão foi removida. |
| 2 | `RuntimeDeployed` | A integridade do plugin permanente foi validada para esta inicialização (nada é implantado; o nome é histórico). |
| 3 | `HandoffCreated` | O handoff de inicialização, o slot de telemetria e o mailbox de runtime existem como memória compartilhada nomeada. |
| 4 | `ProcessStarted` | `Omsi.exe` foi criado. O journal agora também contém `ProcessId`, `ProcessStartFileTimeUtc` (hora de criação, ticks UTC) e `ExecutablePath`. |
| 5 | `ProcessExited` | O supervisor confirmou a saída do processo (saída natural ou `TerminateProcess`). |
| 6 | `Restoring` | A restauração começou. |
| 7 | `Restored` | Todo arquivo pertencente à sessão foi restaurado e verificado. Imediatamente depois, o journal é excluído e `backup\<session>` é removido. |
| 8 | `Completed` | Declarado no enum, mas nunca persistido; uma transação concluída não tem journal. |

O ciclo de vida de uma sessão normal é, portanto: snapshot -> `Prepared` -> overlays gravados / exclusões removidas -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal excluído -> `backup\<session>` removido. Os valores públicos de `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` e `Completed` acompanham a mesma progressão vista de fora (veja [ciclo de vida da sessão](session-lifecycle.md)).

Uma sessão sem nenhuma alteração em arquivos próprios ainda grava um journal para seu ciclo de vida; restaurá-la é uma operação nula verificada.

<a id="journal-file"></a>
## Arquivo de journal

Caminho: `<root>\.omsilaunch\journal.json`. Há no máximo um journal por instalação; sua presença significa "uma transação está pendente".

Campos de `TransactionJournal`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `SessionId` | GUID | Sessão proprietária do journal; também é o nome do diretório de backup (formato `N`). |
| `State` | inteiro | `TransactionState` acima. |
| `Files` | array de `JournalFile` | Uma entrada por caminho pertencente à sessão. |
| `ProcessId` | inteiro ou null | PID do OMSI, a partir de `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long ou null | Hora de criação do OMSI (ticks UTC), a partir de `ProcessStarted`. |
| `ExecutablePath` | string ou null | Caminho completo do `Omsi.exe` iniciado, a partir de `ProcessStarted`. |

Campos de `JournalFile`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `RelativePath` | string | Caminho relativo à raiz da instalação (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Se o arquivo existia antes da sessão. |
| `Sha256` | string hexadecimal | SHA-256 dos bytes originais (de um array de bytes vazio quando `Existed` é false). |
| `BackupPath` | string | Caminho absoluto da cópia de backup (gravado somente quando `Existed`). |
| `AppliedSha256` | string hexadecimal ou null | SHA-256 dos bytes de overlay que a sessão gravou neste caminho; null para exclusões da sessão. É o fingerprint de propriedade para arquivos originalmente ausentes. |
| `LastWriteTimeUtcTicks` | long ou null | Hora original da última gravação. |
| `CreationTimeUtcTicks` | long ou null | Hora original de criação. |
| `Attributes` | inteiro ou null | `FileAttributes` originais (incluindo `ReadOnly`). |
| `SessionDeletion` | bool | True para caminhos que a sessão pediu para manter ausentes (alvos do `.itx`, `Texture\standard.ipr`, `closecheck`). |

Journals gravados por builds anteriores sem `AppliedSha256` e sem os campos de metadados continuam legíveis; veja [Arquivos originalmente ausentes](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Estrutura dos backups

| Caminho | Conteúdo |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Um diretório por sessão, criado junto com o journal `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Bytes originais exatos de um arquivo existente pertencente à sessão. Arquivos originalmente ausentes não têm backup. |

Backups e o journal são gravados com um arquivo temporário (`<path>.omsilaunch.tmp`), com write-through mais um `Flush(true)` explícito, e depois um `File.Move` atômico com sobrescrita. O arquivo temporário é sempre excluído, mesmo em caso de falha. O mesmo caminho de gravação é usado para overlays e para originais restaurados, portanto nenhum arquivo `*.omsilaunch.tmp` sobrevive a uma operação concluída.

Os backups só são removidos depois que o journal que os referenciava foi excluído. Uma falha ao excluir `backup\<session>` é apenas cosmética e nunca desfaz uma restauração verificada.

<a id="restore"></a>
## Restauração

`RestoreAsync` é executado após `ProcessExited` (ou durante a recuperação). Para cada caminho registrado no journal:

| Estado original | Action |
| --- | --- |
| Existia | O hash dos bytes do backup é calculado e comparado com `Sha256`; uma divergência aborta com `OL_E_RECOVERY_BACKUP_CORRUPT` antes que qualquer coisa seja gravada. Em seguida os bytes são gravados atomicamente (se o arquivo atual for somente leitura, esse atributo é removido antes), e a hora de criação, a hora da última gravação e os atributos são restaurados (`RestoreMetadata`; falhas de metadados são ignoradas para que um problema de permissão não possa bloquear uma restauração byte a byte exata). |
| Ausente, agora presente, `AppliedSha256` conhecido | O hash dos bytes atuais é calculado. Se for igual a `AppliedSha256`, o arquivo é o próprio overlay da sessão e é excluído. Caso contrário, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` aborta a restauração e o journal é mantido. |
| Ausente, agora presente, exclusão da sessão, journal chegou a `ProcessStarted` | O arquivo é um subproduto da sessão (o OMSI foi executado com o lease da instalação em posse e este caminho deveria permanecer ausente). Ele é excluído e relatado como diagnóstico `restore.session-artifact-removed` com o SHA-256 do conteúdo removido. |
| Ausente, agora presente, exclusão da sessão, processo nunca iniciado | O arquivo veio de fora da sessão. Ele é mantido, relatado como `OL_W_RESTORE_FOREIGN_FILE_RETAINED` com seu SHA-256, e a transação ainda é concluída. |
| Ausente, agora presente, sem evidência de propriedade (journal anterior aos fingerprints) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; o journal é mantido. |
| Ausente, ainda ausente | Nada a fazer. |

Depois que todos os arquivos são processados, `VerifyRestoredSnapshots` relê cada caminho: os originais existentes devem ter hash igual a `Sha256`, e os caminhos originalmente ausentes devem estar ausentes, a menos que tenham sido mantidos explicitamente. Só então `Restored` é persistido, o journal é excluído (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` se ele sobreviver) e o diretório de backup é removido. Uma falha grave entre `Restored` e a exclusão do journal causa apenas uma reexecução idempotente.

As notas de restauração (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecem como entradas `LaunchDiagnostic` em `SessionStatus.Diagnostics` (com `sha256` em `Data`) e em `RecoveryStatus.Diagnostics`, portanto nada é removido ou mantido silenciosamente.

<a id="originally-absent-files-and-ownership"></a>
### Arquivos originalmente ausentes e propriedade

Um overlay gravado em um caminho que não existia é removido na restauração **somente se seu conteúdo ainda corresponder ao que a sessão aplicou** (`AppliedSha256`). Se outra coisa o tiver substituído durante a sessão, a restauração falha com `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` e o journal é mantido para inspeção.

Um caminho de exclusão da sessão (alvos do `.itx`, `Texture\standard.ipr`, `closecheck`) que não existia antes, mas existe depois, é avaliado conforme o OMSI tenha ou não sido executado sob esta transação: se o journal chegou a `ProcessStarted`, o arquivo é um subproduto da sessão e é removido (`restore.session-artifact-removed`); se o processo nunca foi iniciado, o arquivo é mantido e relatado como `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, e o journal ainda é concluído.

### `closecheck`

`closecheck` é o próprio marcador de falha do OMSI (presente quando o OMSI não foi encerrado corretamente). Duas regras se aplicam:

- Se ele existir **antes** da sessão e `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` for `true` (o padrão), ele é removido permanentemente antes de a transação ser aberta, registrado como diagnóstico `closecheck.stale-removed` com seu SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` se a exclusão falhar). Trata-se de uma alteração permanente documentada, não de um participante da transação. Com a flag em `false`, o marcador permanece e o OMSI mostra seu aviso.
- Se ele **não** existir antes da sessão, `closecheck` é adicionado como exclusão da sessão. Como a sessão termina com `TerminateProcess` (a rotina de desligamento do OMSI não é executada), o marcador criado pelo OMSI na inicialização sempre continua lá depois; ele é removido na restauração como artefato da sessão.

<a id="early-recovery-order"></a>
## Ordem da recuperação antecipada

Em todo `StartSessionAsync`, depois que o lease da instalação é obtido e antes que qualquer coisa leia a instalação ativa:

1. Uma transação somente de recuperação verifica se existe `journal.json`. Se existir, `RestorePendingAsync` é executado imediatamente, de modo que os overlays e o idioma do splash da nova sessão derivem dos arquivos **originais**, nunca de sobras de uma sessão anterior.
2. Se essa recuperação falhar com `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (um journal anterior aos fingerprints), a recuperação é **adiada**: a nova sessão monta seus overlays, e a nova transação tenta a recuperação de novo usando seus próprios bytes planejados como evidência de propriedade (um arquivo originalmente ausente cujo conteúdo é igual ao novo overlay é aceito como pertencente ao OmsiLaunch). Qualquer outra falha de recuperação faz a inicialização falhar.
3. Só então o conjunto de arquivos do plugin é validado, o hash do `Omsi.exe` é calculado, `closecheck` é tratado e a nova transação é preparada e aplicada.

O trace do host registra `PENDING_JOURNAL_RECOVERED` ou `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Recuperação após falha grave e atividade do proprietário

A recuperação nunca substitui arquivos por baixo de um OMSI em execução. `RestorePendingAsync` recusa com `OL_E_INSTALLATION_BUSY` enquanto o proprietário registrado no journal estiver ativo:

| Conteúdo do journal | Teste de atividade |
| --- | --- |
| `ProcessId` e `ProcessStartFileTimeUtc` registrados | O processo com esse PID precisa estar em execução, sua hora de início precisa corresponder (rejeita reutilização de PID) e, quando `ExecutablePath` estiver registrado, seu módulo principal precisa ser esse caminho (um processo ativo sem relação não consegue reter a transação). |
| Sem PID, estado entre `HandoffCreated` (inclusive) e `ProcessExited` (exclusive) | O host morreu entre `CreateProcess` e a gravação do journal. Qualquer `Omsi.exe` cujo módulo principal seja `<root>\Omsi.exe` é tratado como o proprietário. |
| Sem PID, outros estados | Não ativo; a recuperação prossegue. |

A recuperação explícita é exposta como `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, que retorna `RecoveryStatus(Pending, Recovered, Diagnostics)`; ela obtém primeiro o lease da instalação (`OL_E_INSTALLATION_BUSY` quando outro proprietário o detém). Na CLI, `/recovery-status` informa sem restaurar e `/recover` restaura; o código de saída 8 (`TransactionRecoveryFailed`) é retornado quando uma restauração foi solicitada e o journal continua pendente depois. Veja [CLI](../reference/cli.md) e [API pública](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Restauração adiada no fim da sessão

Se o supervisor não conseguir confirmar que o OMSI terminou (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` ou uma falha de limpeza relatada como `OL_E_PROCESS_CLEANUP_FAILED`), a sessão falha com `OL_E_RESTORE_DEFERRED` e o journal é mantido deliberadamente: substituir arquivos da instalação enquanto o OMSI ainda pode lê-los não é seguro. A próxima inicialização (ou `/recover`) restaura assim que o processo tiver terminado. Uma restauração que falhe por qualquer outro motivo encerra a sessão com `OL_E_RESTORE_FAILED`; o journal permanece até que todo original pertencente à sessão seja restaurado e verificado.

<a id="the-installation-lease"></a>
## O lease da instalação

O lease é um semáforo nomeado `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` com contagem 1. A raiz é normalizada por `InstallationLease.NormalizeRoot` (caminho completo, separadores finais removidos, exceto para a raiz de uma unidade), de modo que `C:\OMSI`, `C:\OMSI\` e `c:\omsi\sub\..` compartilham um único lease; o nome do pipe de controle local usa a mesma normalização. Ele é obtido por `StartSessionAsync` (estado `AcquiringInstallationLock`) e por `RecoverPendingAsync`, e liberado quando a tarefa de ciclo de vida da sessão termina ou quando a chamada de recuperação retorna. `OL_E_INSTALLATION_BUSY` é gerado imediatamente quando ele não pode ser obtido (sem espera).

Limitações aceitas (documentadas, sem alteração prevista):

- Escopo `Local\`: um proprietário por instalação **por sessão de logon**. Dois usuários interativos na mesma máquina não são mutuamente excluídos.
- Um semáforo não é liberado por uma falha grave enquanto qualquer outro processo ainda mantiver um handle para ele; ao contrário de um mutex abandonado, ele não tem proprietário. Um detentor obsoleto deixa a instalação em `OL_E_INSTALLATION_BUSY` até que esse handle seja fechado.
- Qualquer processo do mesmo usuário do Windows pode criar o nome primeiro e mantê-lo.

<a id="omsilaunch-directory"></a>
## Diretório `.omsilaunch`

| Entrada | Tempo de vida | Proprietário |
| --- | --- | --- |
| `journal.json` | Temporário; existe apenas enquanto uma transação está pendente | Transação |
| `backup\<sessionId>\*.bin` | Temporário; removido depois do journal | Transação |
| `diagnostics\<sessionId>-host.log` | Permanente; a retenção mantém as 50 sessões mais recentes (arquivos mais antigos com prefixo de sessão são excluídos quando uma nova sessão é iniciada) | Trace do host |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanente (mesma retenção) | CLI |
| `diagnostics\tray-host.log` | Permanente | Host da bandeja do Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Assets permanentes do produto; copiados uma vez do pacote, nunca sobrescritos nem removidos | Assets visuais da sessão |
| `session-profiles\<id>\` | Permanente; instalado pelo usuário ou pelo autor do conteúdo | Usuário |
| `profiles\` | Não é criado nem lido pelo código atual; reservado | nenhum |
| `docs\`, `examples\` | Permanente; distribuído pelo pacote de release | Pacote |

Nenhum dado sai da máquina; os diagnósticos são apenas arquivos locais. Veja também [diretório `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Mutações de runtime não são registradas no journal

As operações de controle de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, texturas D3D) alteram apenas o estado em memória do OMSI. Elas não são registradas no journal e não são restauradas; desaparecem com o processo. Veja [controle de runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Modos de falha e códigos de erro

| Código | Significado | Journal depois |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease detido por outro proprietário, ou o processo do OMSI registrado no journal ainda está ativo | mantido |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Restauração solicitada para uma transação com snapshots, mas sem journal em disco | n/a |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | O hash de um backup difere do fingerprint do snapshot; nada foi gravado | mantido |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Um caminho de overlay originalmente ausente agora contém conteúdo que a sessão não gravou | mantido |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal anterior aos fingerprints com um caminho originalmente ausente que agora existe; somente uma nova sessão com bytes planejados idênticos pode encerrá-lo | mantido (adiado) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | Não foi possível excluir `journal.json` após uma restauração verificada | mantido (a reexecução é idempotente) |
| `OL_E_RESTORE_DEFERRED` | Saída do OMSI não confirmada; restauração adiada para a próxima inicialização | mantido |
| `OL_E_RESTORE_FAILED` | Qualquer outra falha de restauração (divergência de presença ou de hash após a restauração, erro de E/S) | mantido |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Não foi possível excluir o `closecheck` obsoleto antes da transação | nenhum ainda |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Aviso: um arquivo externo em um caminho de exclusão da sessão foi mantido | concluído |
| `OL_E_PLAN_NOT_RUNNABLE` | O replanejamento na inicialização constatou que a especificação não está mais apta para execução (por exemplo, um `Omsi.exe` alterado); nenhuma transação é aberta | nenhum |

A CLI mapeia `OL_E_RECOVERY_*` e `OL_E_RESTORE_FAILED` para o código de saída 8 e `OL_E_INSTALLATION_BUSY` para o código de saída 7; veja [códigos de saída](../reference/exit-codes.md).

<a id="evidence"></a>
## Evidências

Testes offline em `tools/OmsiLaunch.TestHost` cobrem os caminhos da transação: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Evidência de runtime (matriz de validação): RV-005 e RV-006 (overlay aplicado e restauração byte a byte exata, sessões `1e8e0548-...` e o lote de apresentação), RV-008 aprovação de saída antecipada (sessão `0dc40570-...`).

Evidência de runtime (rodada de fechamento de runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- remoção como artefato de sessão dos alvos do `.itx` com o downloader real do OMSI, em parada normal e após interrupção do proprietário seguida de `/recover` (S-01, `I01`, `I02`);
- recuperação antecipada antes da montagem dos overlays (S-05, `S05`);
- restauração de metadados e de arquivos somente leitura, mais limpeza dos backups (S-12, `S12a`, `S12b`);
- `/recover` sob o lease, com um OMSI órfão e na janela anterior ao PID (S-04, `S04`, `S04b`);
- falhas de inicialização e uma restauração com falha seguida de `/recover` (restante de RV-008, `SF01`, `SF02`, `F01`);
- preservação de CP1252 (S-07, `C01`).

O ramo de recuperação adiada para journals anteriores aos fingerprints continua somente offline. Veja [status da validação em runtime](../status/runtime-validation-status.md).
