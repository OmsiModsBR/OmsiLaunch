# Transações e recuperação

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Tradução da [página original em inglês](../../../concepts/transactions-and-recovery.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Cada sessão do OmsiLaunch que toca num ficheiro do OMSI fá-lo dentro de uma transação durável com journal (registo de transação): os bytes originais são copiados para uma cópia de segurança antes de serem substituídos, o journal regista até onde a sessão chegou e o restauro verifica cada cópia de segurança antes de a escrever de volta. Esta página descreve essa transação tal como está implementada por `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) e conduzida por `OmsiLaunchService.StartAsync`, `SuperviseAsync` e `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), juntamente com os ficheiros de entrada calculados por `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Destina-se aos utilizadores que precisam de saber o que uma sessão altera e o que a recuperação faz, e aos integradores que precisam das garantias exatas.

<a id="what-a-session-changes"></a>
## O que uma sessão altera

Apenas os **overlays temporários** entram na transação. São calculados antes de a transação ser aberta e restaurados quando esta é fechada.

| Entrada da sessão | Ficheiro(s) | Tipo |
| --- | --- | --- |
| `/set:<key>=<value>`, `settings` do perfil, `LaunchSpec.Environment.*` | `options.cfg` (patches semânticos de tokens; bytes CP1252 preservados, UTF-8/UTF-16 com BOM respeitados) | overlay |
| Splash gerido (`SplashMode.Managed`, a predefinição) | `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<language>.bmp` | overlay (o ficheiro localizado é criado pela transação quando a instalação não tem nenhum) |
| Internet Textures `Override` | `Texture\standard.itx` | overlay |
| Internet Textures `Override` | cada alvo listado no `.itx`, mais `Texture\standard.ipr` | eliminação da sessão |
| Sempre | `closecheck` (quando ausente antes da sessão) | eliminação da sessão |

Os ficheiros permanentes do produto **não** participam na transação: o conjunto do plugin em `plugins\OmsiLaunch.*` (apenas validado, ver [plugin permanente](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (copiados uma vez, nunca removidos), os diagnósticos em `.omsilaunch\diagnostics`, os pacotes de perfis de sessão e a documentação e os exemplos da versão. Os plugins de terceiros e todos os outros ficheiros do OMSI nunca são enumerados, copiados, removidos nem restaurados.

O próprio OMSI continua a escrever o seu estado enquanto uma sessão está a executar, exatamente como num arranque normal do OMSI: `options.cfg` (por exemplo, `[last_map]` quando a sessão carrega um mapa diferente, reescrito à entrada na jogabilidade), `Texture\standard.ipr`, as caches de horários e de lightmaps (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, o perfil de motorista em `Drivers\` e os seus registos. Uma escrita num caminho que pertence à sessão (acima) é desfeita pelo restauro; todas as outras escritas do OMSI persistem depois da sessão, tal como persistiriam depois de executar o OMSI diretamente. Evidência do fecho de runtime: uma sessão de situação guardada noutro mapa deixou `[last_map]` alterado porque não aplicou overlay a `options.cfg` (`CAM01`), enquanto as sessões com `/set` restauraram `options.cfg` exatamente (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Estados da transação

`TransactionState` é persistido no journal após cada transição. Os valores são serializados como inteiros por `System.Text.Json`.

| Valor | Estado | Escrito quando |
| --- | --- | --- |
| 0 | `Prepared` | Foram tirados snapshots de todos os caminhos de overlay e de eliminação e as respetivas cópias de segurança foram escritas em disco. Nada na instalação foi ainda alterado. Esta é a obrigação de recuperação: a partir daqui, uma falha abrupta deixa um journal recuperável. |
| 1 | `Applied` | Todos os overlays foram escritos atomicamente e todas as eliminações foram removidas. |
| 2 | `RuntimeDeployed` | A integridade do plugin permanente foi validada para este arranque (nada é implementado; o nome é histórico). |
| 3 | `HandoffCreated` | O handoff de arranque, o slot de telemetria e a mailbox de runtime existem como memória partilhada com nome. |
| 4 | `ProcessStarted` | `Omsi.exe` foi criado. O journal passa também a conter `ProcessId`, `ProcessStartFileTimeUtc` (hora de criação, ticks UTC) e `ExecutablePath`. |
| 5 | `ProcessExited` | O supervisor confirmou a saída do processo (saída natural ou `TerminateProcess`). |
| 6 | `Restoring` | O restauro começou. |
| 7 | `Restored` | Todos os ficheiros pertencentes à sessão foram restaurados e verificados. Imediatamente a seguir, o journal é eliminado e `backup\<session>` é removido. |
| 8 | `Completed` | Declarado no enum, mas nunca persistido; uma transação concluída não tem journal. |

O ciclo de vida de uma sessão normal é, portanto: snapshot -> `Prepared` -> overlays escritos / eliminações removidas -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal eliminado -> `backup\<session>` removido. Os valores públicos de `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` e `Completed` acompanham a mesma progressão vista de fora (ver [ciclo de vida da sessão](session-lifecycle.md)).

Uma sessão sem alterações a ficheiros próprios continua a escrever um journal para o seu ciclo de vida; o seu restauro é uma operação nula verificada.

<a id="journal-file"></a>
## Ficheiro do journal

Caminho: `<root>\.omsilaunch\journal.json`. Existe no máximo um journal por instalação; a sua presença significa «há uma transação pendente».

Campos de `TransactionJournal`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `SessionId` | GUID | Sessão proprietária do journal; também é o nome do diretório de cópias de segurança (formato `N`). |
| `State` | inteiro | `TransactionState` acima. |
| `Files` | array de `JournalFile` | Uma entrada por caminho pertencente à sessão. |
| `ProcessId` | inteiro ou null | PID do OMSI, a partir de `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long ou null | Hora de criação do OMSI (ticks UTC), a partir de `ProcessStarted`. |
| `ExecutablePath` | string ou null | Caminho completo do `Omsi.exe` lançado, a partir de `ProcessStarted`. |

Campos de `JournalFile`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `RelativePath` | string | Caminho relativo à raiz da instalação (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Se o ficheiro existia antes da sessão. |
| `Sha256` | string hexadecimal | SHA-256 dos bytes originais (de um array de bytes vazio quando `Existed` é falso). |
| `BackupPath` | string | Caminho absoluto da cópia de segurança (só é escrita quando `Existed`). |
| `AppliedSha256` | string hexadecimal ou null | SHA-256 dos bytes de overlay que a sessão escreveu neste caminho; null para eliminações da sessão. É a impressão digital de propriedade para ficheiros originalmente ausentes. |
| `LastWriteTimeUtcTicks` | long ou null | Hora original da última escrita. |
| `CreationTimeUtcTicks` | long ou null | Hora original de criação. |
| `Attributes` | inteiro ou null | `FileAttributes` originais (incluindo `ReadOnly`). |
| `SessionDeletion` | bool | Verdadeiro para caminhos que a sessão pediu para manter ausentes (alvos `.itx`, `Texture\standard.ipr`, `closecheck`). |

Os journals escritos por builds anteriores sem `AppliedSha256` e sem os campos de metadados continuam a ser legíveis; ver [Ficheiros originalmente ausentes](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Estrutura das cópias de segurança

| Caminho | Conteúdo |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Um diretório por sessão, criado com o journal `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Bytes originais exatos de um ficheiro existente pertencente à sessão. Os ficheiros originalmente ausentes não têm cópia de segurança. |

As cópias de segurança e o journal são escritos através de um ficheiro temporário (`<path>.omsilaunch.tmp`), com write-through e um `Flush(true)` explícito, seguido de um `File.Move` atómico com substituição. O ficheiro temporário é sempre eliminado, mesmo em caso de falha. O mesmo caminho de escrita é usado para os overlays e para os originais restaurados, pelo que nenhum ficheiro `*.omsilaunch.tmp` sobrevive a uma operação concluída.

As cópias de segurança só são removidas depois de o journal que as referenciava ter sido eliminado. Uma falha ao eliminar `backup\<session>` é cosmética e nunca desfaz um restauro verificado.

<a id="restore"></a>
## Restauro

`RestoreAsync` é executado depois de `ProcessExited` (ou durante a recuperação). Para cada caminho registado no journal:

| Estado original | Action |
| --- | --- |
| Existia | É calculado o hash dos bytes da cópia de segurança, que é comparado com `Sha256`; uma discrepância aborta com `OL_E_RECOVERY_BACKUP_CORRUPT` antes de qualquer escrita. Os bytes são depois escritos atomicamente (se o ficheiro atual for só de leitura, o atributo é primeiro limpo), e a hora de criação, a hora da última escrita e os atributos são restaurados (`RestoreMetadata`; as falhas nos metadados são ignoradas, para que um problema de permissões não possa bloquear um restauro exato ao byte). |
| Ausente, agora presente, `AppliedSha256` conhecido | É calculado o hash dos bytes atuais. Se forem iguais a `AppliedSha256`, o ficheiro é o overlay da própria sessão e é eliminado. Caso contrário, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` aborta o restauro e o journal é mantido. |
| Ausente, agora presente, eliminação da sessão, o journal chegou a `ProcessStarted` | O ficheiro é um subproduto da sessão (o OMSI foi executado com o lease da instalação detido e foi pedido que este caminho se mantivesse ausente). É eliminado e comunicado como diagnóstico `restore.session-artifact-removed` com o SHA-256 do conteúdo removido. |
| Ausente, agora presente, eliminação da sessão, o processo nunca foi iniciado | O ficheiro veio de fora da sessão. É mantido, comunicado como `OL_W_RESTORE_FOREIGN_FILE_RETAINED` com o seu SHA-256, e a transação conclui na mesma. |
| Ausente, agora presente, sem evidência de propriedade (journal anterior às impressões digitais) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; o journal é mantido. |
| Ausente, continua ausente | Nada a fazer. |

Depois de todos os ficheiros terem sido processados, `VerifyRestoredSnapshots` volta a ler cada caminho: os originais existentes têm de ter o hash `Sha256` e os caminhos originalmente ausentes têm de estar ausentes, exceto se tiverem sido explicitamente mantidos. Só então `Restored` é persistido, o journal é eliminado (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` se sobreviver) e o diretório de cópias de segurança é removido. Uma falha abrupta entre `Restored` e a eliminação do journal só provoca uma repetição idempotente.

As notas de restauro (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecem como entradas `LaunchDiagnostic` em `SessionStatus.Diagnostics` (com `sha256` em `Data`) e em `RecoveryStatus.Diagnostics`, pelo que nada é removido nem mantido silenciosamente.

<a id="originally-absent-files-and-ownership"></a>
### Ficheiros originalmente ausentes e propriedade

Um overlay escrito num caminho que não existia é removido no restauro **apenas se o seu conteúdo ainda corresponder ao que a sessão aplicou** (`AppliedSha256`). Se outra coisa o tiver substituído durante a sessão, o restauro falha com `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` e o journal é mantido para inspeção.

Um caminho de eliminação da sessão (alvos `.itx`, `Texture\standard.ipr`, `closecheck`) que não existia antes, mas existe depois, é avaliado consoante o OMSI tenha sido executado sob esta transação: se o journal chegou a `ProcessStarted`, o ficheiro é um subproduto da sessão e é removido (`restore.session-artifact-removed`); se o processo nunca foi iniciado, o ficheiro é mantido e comunicado como `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, e o journal conclui na mesma.

### `closecheck`

`closecheck` é o marcador de falha abrupta do próprio OMSI (presente quando o OMSI não encerrou corretamente). Aplicam-se duas regras:

- Se existir **antes** da sessão e `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` for `true` (a predefinição), é removido permanentemente antes de a transação ser aberta, registado como diagnóstico `closecheck.stale-removed` com o seu SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` se a eliminação falhar). Trata-se de uma alteração permanente documentada, não de um participante da transação. Com a flag a `false`, o marcador mantém-se e o OMSI mostra o seu aviso.
- Se **não** existir antes da sessão, `closecheck` é adicionado como eliminação da sessão. Como a sessão termina com `TerminateProcess` (a rotina de encerramento do OMSI não é executada), o marcador criado pelo OMSI no arranque continua sempre presente no final; é removido no restauro como artefacto da sessão.

<a id="early-recovery-order"></a>
## Ordem da recuperação antecipada

Em cada `StartSessionAsync`, depois de o lease da instalação ter sido obtido e antes de qualquer leitura da instalação ativa:

1. Uma transação apenas de recuperação verifica se existe `journal.json`. Se existir, `RestorePendingAsync` é executado imediatamente, para que os overlays e o idioma do splash da nova sessão derivem dos ficheiros **originais** e nunca de restos de uma sessão anterior.
2. Se essa recuperação falhar com `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (um journal anterior às impressões digitais), a recuperação é **adiada**: a nova sessão constrói os seus overlays e a nova transação tenta de novo a recuperação usando os seus próprios bytes planeados como evidência de propriedade (um ficheiro originalmente ausente cujo conteúdo seja igual ao novo overlay é aceite como pertencente ao OmsiLaunch). Qualquer outra falha de recuperação faz falhar o arranque.
3. Só então o conjunto do plugin é validado, é calculado o hash de `Omsi.exe`, `closecheck` é tratado e a nova transação é preparada e aplicada.

O rastreio do anfitrião regista `PENDING_JOURNAL_RECOVERED` ou `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Recuperação após falha abrupta e vivacidade do proprietário

A recuperação nunca substitui ficheiros por baixo de um OMSI em execução. `RestorePendingAsync` recusa com `OL_E_INSTALLATION_BUSY` enquanto o proprietário registado no journal estiver vivo:

| Conteúdo do journal | Teste de vivacidade |
| --- | --- |
| `ProcessId` e `ProcessStartFileTimeUtc` registados | O processo com esse PID tem de estar em execução, a sua hora de início tem de coincidir (rejeita a reutilização de PIDs) e, quando `ExecutablePath` está registado, o seu módulo principal tem de ser esse caminho (um processo vivo não relacionado não pode reter a transação). |
| Sem PID, estado entre `HandoffCreated` (inclusive) e `ProcessExited` (exclusive) | O anfitrião morreu entre `CreateProcess` e a escrita do journal. Qualquer `Omsi.exe` cujo módulo principal seja `<root>\Omsi.exe` é tratado como proprietário. |
| Sem PID, outros estados | Não está vivo; a recuperação prossegue. |

A recuperação explícita é exposta como `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, que devolve `RecoveryStatus(Pending, Recovered, Diagnostics)`; obtém primeiro o lease da instalação (`OL_E_INSTALLATION_BUSY` quando outro proprietário o detém). Na CLI, `/recovery-status` comunica o estado sem restaurar e `/recover` restaura; o código de saída 8 (`TransactionRecoveryFailed`) é devolvido quando foi pedido um restauro e o journal continua pendente depois disso. Ver [CLI](../reference/cli.md) e [API pública](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Restauro adiado no fim da sessão

Se o supervisor não conseguir confirmar que o OMSI terminou (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` ou uma falha de limpeza comunicada como `OL_E_PROCESS_CLEANUP_FAILED`), a sessão falha com `OL_E_RESTORE_DEFERRED` e o journal é mantido deliberadamente: substituir ficheiros da instalação enquanto o OMSI ainda os pode ler não é seguro. O próximo arranque (ou `/recover`) restaura assim que o processo tiver desaparecido. Um restauro que falhe por qualquer outro motivo termina a sessão com `OL_E_RESTORE_FAILED`; o journal permanece até todos os originais pertencentes à sessão estarem restaurados e verificados.

<a id="the-installation-lease"></a>
## O lease da instalação

O lease é um semáforo com nome `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` com contagem 1. A raiz é normalizada por `InstallationLease.NormalizeRoot` (caminho completo, separadores finais removidos exceto na raiz de uma unidade), pelo que `C:\OMSI`, `C:\OMSI\` e `c:\omsi\sub\..` partilham o mesmo lease; o nome do pipe de controlo local usa a mesma normalização. É obtido por `StartSessionAsync` (estado `AcquiringInstallationLock`) e por `RecoverPendingAsync`, e libertado quando a tarefa do ciclo de vida da sessão termina ou quando a chamada de recuperação retorna. `OL_E_INSTALLATION_BUSY` é emitido imediatamente quando não é possível obtê-lo (sem espera).

Limitações aceites (documentadas, sem alteração prevista):

- Âmbito `Local\`: um proprietário por instalação **por sessão de início de sessão do Windows**. Dois utilizadores interativos na mesma máquina não são mutuamente excluídos.
- Um semáforo não é libertado por uma falha abrupta enquanto qualquer outro processo ainda detiver um handle para ele; ao contrário de um mutex abandonado, não tem proprietário. Um detentor obsoleto deixa a instalação em `OL_E_INSTALLATION_BUSY` até esse handle ser fechado.
- Qualquer processo do mesmo utilizador do Windows pode criar o nome primeiro e detê-lo.

<a id="omsilaunch-directory"></a>
## Diretório `.omsilaunch`

| Entrada | Duração | Proprietário |
| --- | --- | --- |
| `journal.json` | Temporário; só existe enquanto uma transação está pendente | Transação |
| `backup\<sessionId>\*.bin` | Temporário; removido depois do journal | Transação |
| `diagnostics\<sessionId>-host.log` | Permanente; a retenção mantém as 50 sessões mais recentes (os ficheiros mais antigos com prefixo de sessão são eliminados quando uma nova sessão é iniciada) | Rastreio do anfitrião |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanente (mesma retenção) | CLI |
| `diagnostics\tray-host.log` | Permanente | Anfitrião da área de notificação do Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Recursos permanentes do produto; copiados uma vez a partir do pacote, nunca substituídos nem removidos | Recursos visuais da sessão |
| `session-profiles\<id>\` | Permanente; instalado pelo utilizador ou pelo autor de conteúdos | Utilizador |
| `profiles\` | Não é criado nem lido pelo código atual; reservado | nenhum |
| `docs\`, `examples\` | Permanente; fornecido pelo pacote de lançamento | Pacote |

Nenhum dado sai da máquina; os diagnósticos são apenas ficheiros locais. Ver também [diretório `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## As alterações de runtime não são registadas no journal

As operações de controlo de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, texturas D3D) alteram apenas o estado em memória do OMSI. Não são registadas no journal nem são restauradas; desaparecem com o processo. Ver [controlo de runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Modos de falha e códigos de erro

| Código | Significado | Journal depois |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease detido por outro proprietário, ou o processo do OMSI registado no journal ainda está vivo | mantido |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Restauro pedido para uma transação com snapshots, mas sem journal em disco | n/a |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | O hash de uma cópia de segurança difere da impressão digital do snapshot; nada foi escrito | mantido |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Um caminho de overlay originalmente ausente contém agora conteúdo que a sessão não escreveu | mantido |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal anterior às impressões digitais com um caminho originalmente ausente que agora existe; só uma nova sessão com bytes planeados idênticos o pode fechar | mantido (adiado) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | Não foi possível eliminar `journal.json` depois de um restauro verificado | mantido (a repetição é idempotente) |
| `OL_E_RESTORE_DEFERRED` | Saída do OMSI não confirmada; restauro adiado para o próximo arranque | mantido |
| `OL_E_RESTORE_FAILED` | Qualquer outra falha de restauro (discrepância de presença ou de hash após o restauro, erro de E/S) | mantido |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Não foi possível eliminar um `closecheck` obsoleto antes da transação | ainda nenhum |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Aviso: foi mantido um ficheiro alheio num caminho de eliminação da sessão | concluído |
| `OL_E_PLAN_NOT_RUNNABLE` | O novo planeamento no arranque concluiu que a especificação já não é executável (por exemplo, um `Omsi.exe` alterado); nenhuma transação é aberta | nenhum |

A CLI mapeia `OL_E_RECOVERY_*` e `OL_E_RESTORE_FAILED` para o código de saída 8 e `OL_E_INSTALLATION_BUSY` para o código de saída 7; ver [códigos de saída](../reference/exit-codes.md).

<a id="evidence"></a>
## Evidência

Os testes offline em `tools/OmsiLaunch.TestHost` cobrem os caminhos da transação: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Evidência de runtime (matriz de validação): RV-005 e RV-006 (overlay aplicado e restauro exato ao byte, sessões `1e8e0548-...` e o lote de apresentação), aprovação de saída antecipada RV-008 (sessão `0dc40570-...`).

Evidência de runtime (ronda de fecho de runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- remoção de artefactos da sessão dos alvos `.itx` com o verdadeiro downloader do OMSI, em paragem normal e após interrupção do proprietário seguida de `/recover` (S-01, `I01`, `I02`);
- recuperação antecipada antes da construção dos overlays (S-05, `S05`);
- restauro de metadados e de ficheiros só de leitura, mais limpeza das cópias de segurança (S-12, `S12a`, `S12b`);
- `/recover` sob o lease, com um OMSI órfão e na janela anterior ao PID (S-04, `S04`, `S04b`);
- falhas no arranque e um restauro falhado seguido de `/recover` (restante de RV-008, `SF01`, `SF02`, `F01`);
- preservação de CP1252 (S-07, `C01`).

O ramo de recuperação adiada de journals anteriores às impressões digitais continua coberto apenas offline. Ver [estado da validação em runtime](../status/runtime-validation-status.md).
