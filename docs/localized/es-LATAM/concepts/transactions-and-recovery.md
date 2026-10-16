# Transacciones y recuperación

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Traducción de la [página original en inglés](../../../concepts/transactions-and-recovery.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Toda sesión de OmsiLaunch que modifica un archivo de OMSI lo hace dentro de una transacción duradera y registrada en un journal: los bytes originales se respaldan antes de reemplazarse, el journal (registro de la transacción) indica hasta dónde llegó la sesión y la restauración verifica cada copia de seguridad antes de volver a escribirla. Esta página describe esa transacción tal como la implementa `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) y como la conducen `OmsiLaunchService.StartAsync`, `SuperviseAsync` y `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), junto con las entradas de archivos calculadas por `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Está escrita para los usuarios que necesitan saber qué cambia una sesión y qué hace la recuperación, y para los integradores que necesitan las garantías exactas.

<a id="what-a-session-changes"></a>
## Qué cambia una sesión

Solo los **overlays temporales** entran en la transacción. Se calculan antes de abrir la transacción y se restauran cuando esta se cierra.

| Entrada de la sesión | Archivo(s) | Tipo |
| --- | --- | --- |
| `/set:<key>=<value>`, `settings` del perfil, `LaunchSpec.Environment.*` | `options.cfg` (parches semánticos de tokens; se conservan los bytes CP1252 y se respeta UTF-8/UTF-16 marcado con BOM) | overlay |
| Splash administrado (`SplashMode.Managed`, el predeterminado) | `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_<language>.bmp` | overlay (el archivo localizado lo crea la transacción cuando la instalación no tiene uno) |
| Texturas de Internet `Override` | `Texture\standard.itx` | overlay |
| Texturas de Internet `Override` | cada destino listado en el `.itx`, más `Texture\standard.ipr` | eliminación de la sesión |
| Siempre | `closecheck` (cuando no existe antes de la sesión) | eliminación de la sesión |

Los archivos permanentes del producto **no** participan en la transacción: el conjunto de archivos del plugin en `plugins\OmsiLaunch.*` (solo se valida; consulte [plugin permanente](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (se copia una vez y nunca se elimina), los diagnósticos en `.omsilaunch\diagnostics`, los paquetes de perfiles de sesión y la documentación y los ejemplos de la release. Los plugins de terceros y cualquier otro archivo de OMSI nunca se enumeran, copian, eliminan ni restauran.

OMSI sigue escribiendo su propio estado mientras se ejecuta una sesión, exactamente igual que en un inicio normal de OMSI: `options.cfg` (por ejemplo `[last_map]` cuando la sesión carga un mapa diferente, reescrito al entrar al gameplay), `Texture\standard.ipr`, las cachés de horarios y de lightmaps (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, el perfil del conductor en `Drivers\` y sus logs. Una escritura en una ruta que pertenece a la sesión (arriba) la deshace la restauración; cualquier otra escritura de OMSI persiste después de la sesión, tal como ocurriría después de ejecutar OMSI directamente. Evidencia de la ronda de cierre de runtime: una sesión de situación guardada en otro mapa dejó `[last_map]` modificado porque no aplicaba un overlay sobre `options.cfg` (`CAM01`), mientras que las sesiones con `/set` restauraron `options.cfg` exactamente (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Estados de la transacción

`TransactionState` se persiste en el journal después de cada transición. `System.Text.Json` serializa los valores como enteros.

| Valor | Estado | Se escribe cuando |
| --- | --- | --- |
| 0 | `Prepared` | Se tomaron snapshots de todas las rutas de overlay y de eliminación, y sus copias de seguridad se volcaron al disco. Todavía no cambió nada en la instalación. Esta es la obligación de recuperación: a partir de aquí, un fallo deja un journal recuperable. |
| 1 | `Applied` | Todos los overlays se escribieron de forma atómica y todas las eliminaciones se realizaron. |
| 2 | `RuntimeDeployed` | Se validó la integridad del plugin permanente para este inicio (no se despliega nada; el nombre es histórico). |
| 3 | `HandoffCreated` | El handoff de arranque, el slot de telemetría y el buzón de runtime existen como memoria compartida con nombre. |
| 4 | `ProcessStarted` | Se creó `Omsi.exe`. A partir de ahora el journal también contiene `ProcessId`, `ProcessStartFileTimeUtc` (hora de creación, ticks UTC) y `ExecutablePath`. |
| 5 | `ProcessExited` | El supervisor confirmó la salida del proceso (salida natural o `TerminateProcess`). |
| 6 | `Restoring` | Comenzó la restauración. |
| 7 | `Restored` | Todos los archivos propios se restauraron y verificaron. Inmediatamente después se elimina el journal y se borra `backup\<session>`. |
| 8 | `Completed` | Declarado en el enum pero nunca persistido; una transacción completada no tiene journal. |

Por lo tanto, el ciclo de vida de una sesión normal es: snapshot -> `Prepared` -> overlays escritos / eliminaciones realizadas -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal eliminado -> `backup\<session>` eliminado. Los valores públicos de `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` y `Completed` siguen la misma progresión desde afuera (consulte [ciclo de vida de la sesión](session-lifecycle.md)).

Una sesión sin mutaciones de archivos propios igualmente escribe un journal para su ciclo de vida; restaurarla es una operación nula verificada.

<a id="journal-file"></a>
## Archivo journal

Ruta: `<root>\.omsilaunch\journal.json`. Existe como máximo un journal por instalación; su presencia significa "hay una transacción pendiente".

Campos de `TransactionJournal`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `SessionId` | GUID | Sesión propietaria del journal; también es el nombre del directorio de copias de seguridad (formato `N`). |
| `State` | entero | `TransactionState` de arriba. |
| `Files` | arreglo de `JournalFile` | Una entrada por cada ruta propia. |
| `ProcessId` | entero o null | PID de OMSI, a partir de `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long o null | Hora de creación de OMSI (ticks UTC), a partir de `ProcessStarted`. |
| `ExecutablePath` | string o null | Ruta completa del `Omsi.exe` iniciado, a partir de `ProcessStarted`. |

Campos de `JournalFile`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `RelativePath` | string | Ruta relativa a la raíz de la instalación (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Si el archivo existía antes de la sesión. |
| `Sha256` | string hexadecimal | SHA-256 de los bytes originales (de un arreglo de bytes vacío cuando `Existed` es false). |
| `BackupPath` | string | Ruta absoluta de la copia de seguridad (solo se escribe cuando `Existed`). |
| `AppliedSha256` | string hexadecimal o null | SHA-256 de los bytes del overlay que la sesión escribió en esta ruta; null para las eliminaciones de la sesión. Es la huella de propiedad de los archivos que originalmente no existían. |
| `LastWriteTimeUtcTicks` | long o null | Hora original de la última escritura. |
| `CreationTimeUtcTicks` | long o null | Hora original de creación. |
| `Attributes` | entero o null | `FileAttributes` originales (incluido `ReadOnly`). |
| `SessionDeletion` | bool | True para las rutas que la sesión pidió mantener ausentes (destinos de `.itx`, `Texture\standard.ipr`, `closecheck`). |

Los journals escritos por builds anteriores sin `AppliedSha256` ni los campos de metadatos todavía se pueden leer; consulte [Archivos originalmente ausentes](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Estructura de las copias de seguridad

| Ruta | Contenido |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Un directorio por sesión, creado junto con el journal `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Bytes originales exactos de un archivo propio existente. Los archivos originalmente ausentes no tienen copia de seguridad. |

Las copias de seguridad y el journal se escriben con un archivo temporal (`<path>.omsilaunch.tmp`), con escritura directa (write-through) más un `Flush(true)` explícito y luego un `File.Move` atómico con sobrescritura. El archivo temporal siempre se elimina, incluso ante una falla. La misma ruta de escritura se usa para los overlays y para los originales restaurados, por lo que ningún archivo `*.omsilaunch.tmp` sobrevive a una operación completada.

Las copias de seguridad solo se eliminan después de que se eliminó el journal que las referenciaba. Una falla al eliminar `backup\<session>` es cosmética y nunca deshace una restauración verificada.

<a id="restore"></a>
## Restauración

`RestoreAsync` se ejecuta después de `ProcessExited` (o durante la recuperación). Para cada ruta registrada en el journal:

| Estado original | Action |
| --- | --- |
| Existía | Se calcula el hash de los bytes de la copia de seguridad y se compara con `Sha256`; una discrepancia aborta con `OL_E_RECOVERY_BACKUP_CORRUPT` antes de escribir nada. Luego los bytes se escriben de forma atómica (si el archivo actual es de solo lectura, primero se quita ese atributo) y se restauran la hora de creación, la hora de la última escritura y los atributos (`RestoreMetadata`; las fallas de metadatos se ignoran para que un problema de permisos no pueda bloquear una restauración exacta byte a byte). |
| Ausente, ahora presente, `AppliedSha256` conocido | Se calcula el hash de los bytes actuales. Si son iguales a `AppliedSha256`, el archivo es el propio overlay de la sesión y se elimina. De lo contrario, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` aborta la restauración y el journal se conserva. |
| Ausente, ahora presente, eliminación de la sesión, el journal llegó a `ProcessStarted` | El archivo es un subproducto de la sesión (OMSI se ejecutó con el lease de la instalación tomado y se había pedido que esta ruta permaneciera ausente). Se elimina y se informa como diagnóstico `restore.session-artifact-removed` con el SHA-256 del contenido eliminado. |
| Ausente, ahora presente, eliminación de la sesión, el proceso nunca se inició | El archivo provino de fuera de la sesión. Se conserva, se informa como `OL_W_RESTORE_FOREIGN_FILE_RETAINED` con su SHA-256, y la transacción igualmente se completa. |
| Ausente, ahora presente, sin evidencia de propiedad (journal anterior a las huellas de propiedad) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; el journal se conserva. |
| Ausente, sigue ausente | No hay nada que hacer. |

Después de procesar todos los archivos, `VerifyRestoredSnapshots` vuelve a leer cada ruta: los originales existentes deben tener el hash `Sha256` y las rutas originalmente ausentes deben estar ausentes, salvo que se hayan conservado explícitamente. Solo entonces se persiste `Restored`, se elimina el journal (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` si sobrevive) y se borra el directorio de copias de seguridad. Un fallo entre `Restored` y la eliminación del journal solo provoca una repetición idempotente.

Las notas de restauración (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecen como entradas `LaunchDiagnostic` en `SessionStatus.Diagnostics` (con `sha256` en `Data`) y en `RecoveryStatus.Diagnostics`, de modo que nada se elimina ni se conserva en silencio.

<a id="originally-absent-files-and-ownership"></a>
### Archivos originalmente ausentes y propiedad

Un overlay escrito en una ruta que no existía se elimina en la restauración **solo si su contenido todavía coincide con lo que aplicó la sesión** (`AppliedSha256`). Si algo más lo reemplazó durante la sesión, la restauración falla con `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` y el journal se conserva para su inspección.

Una ruta de eliminación de la sesión (destinos de `.itx`, `Texture\standard.ipr`, `closecheck`) que no existía antes pero existe después se evalúa según si OMSI se ejecutó bajo esta transacción: si el journal llegó a `ProcessStarted`, el archivo es un subproducto de la sesión y se elimina (`restore.session-artifact-removed`); si el proceso nunca se inició, el archivo se conserva y se informa como `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, y el journal igualmente se completa.

### `closecheck`

`closecheck` es el propio marcador de fallos de OMSI (presente cuando OMSI no se cerró correctamente). Se aplican dos reglas:

- Si existe **antes** de la sesión y `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` es `true` (el predeterminado), se elimina de forma permanente antes de abrir la transacción y se registra como diagnóstico `closecheck.stale-removed` con su SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` si la eliminación falla). Es un cambio permanente documentado, no un participante de la transacción. Con el flag en `false`, el marcador permanece y OMSI muestra su advertencia.
- Si **no** existe antes de la sesión, `closecheck` se agrega como eliminación de la sesión. Como la sesión termina con `TerminateProcess` (la rutina de apagado de OMSI no se ejecuta), el marcador que OMSI crea al iniciar siempre sigue presente después; se elimina en la restauración como artefacto de la sesión.

<a id="early-recovery-order"></a>
## Orden de la recuperación temprana

En cada `StartSessionAsync`, después de tomar el lease de la instalación y antes de que algo lea la instalación activa:

1. Una transacción solo de recuperación comprueba si existe `journal.json`. Si existe, `RestorePendingAsync` se ejecuta de inmediato, de modo que los overlays y el idioma del splash de la nueva sesión se derivan de los archivos **originales**, nunca de los restos de una sesión anterior.
2. Si esa recuperación falla con `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (un journal anterior a las huellas de propiedad), la recuperación se **posterga**: la nueva sesión construye sus overlays y la nueva transacción reintenta la recuperación usando sus propios bytes planificados como evidencia de propiedad (un archivo originalmente ausente cuyo contenido es igual al nuevo overlay se acepta como perteneciente a OmsiLaunch). Cualquier otra falla de recuperación hace fallar el inicio.
3. Solo entonces se valida el conjunto de archivos del plugin, se calcula el hash de `Omsi.exe`, se trata `closecheck` y se prepara y aplica la nueva transacción.

La traza del host registra `PENDING_JOURNAL_RECOVERED` o `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Recuperación tras fallos y vitalidad del propietario

La recuperación nunca reemplaza archivos por debajo de un OMSI en ejecución. `RestorePendingAsync` se niega con `OL_E_INSTALLATION_BUSY` mientras el propietario registrado en el journal esté vivo:

| Contenido del journal | Prueba de vitalidad |
| --- | --- |
| `ProcessId` y `ProcessStartFileTimeUtc` registrados | El proceso con ese PID debe estar en ejecución, su hora de inicio debe coincidir (rechaza la reutilización de PID) y, cuando se registró `ExecutablePath`, su módulo principal debe ser esa ruta (un proceso vivo no relacionado no puede retener la transacción). |
| Sin PID, estado entre `HandoffCreated` (inclusive) y `ProcessExited` (exclusive) | El host murió entre `CreateProcess` y la escritura del journal. Cualquier `Omsi.exe` cuyo módulo principal sea `<root>\Omsi.exe` se trata como propietario. |
| Sin PID, otros estados | No está vivo; la recuperación continúa. |

La recuperación explícita se expone como `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, que devuelve `RecoveryStatus(Pending, Recovered, Diagnostics)`; primero toma el lease de la instalación (`OL_E_INSTALLATION_BUSY` cuando otro propietario lo tiene). En la CLI, `/recovery-status` informa sin restaurar y `/recover` restaura; se devuelve el código de salida 8 (`TransactionRecoveryFailed`) cuando se solicitó una restauración y el journal sigue pendiente después. Consulte [CLI](../reference/cli.md) y [API pública](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Restauración postergada al final de la sesión

Si el supervisor no puede confirmar que OMSI terminó (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` o una falla de limpieza informada como `OL_E_PROCESS_CLEANUP_FAILED`), la sesión falla con `OL_E_RESTORE_DEFERRED` y el journal se conserva deliberadamente: reemplazar archivos de la instalación mientras OMSI todavía podría leerlos no es seguro. El siguiente inicio (o `/recover`) restaura una vez que el proceso ya no existe. Una restauración que falla por cualquier otro motivo finaliza la sesión con `OL_E_RESTORE_FAILED`; el journal permanece hasta que cada original propio se restaure y verifique.

<a id="the-installation-lease"></a>
## El lease de la instalación

El lease es un semáforo con nombre `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` con contador 1. La raíz se normaliza mediante `InstallationLease.NormalizeRoot` (ruta completa, sin separadores finales excepto en la raíz de una unidad), de modo que `C:\OMSI`, `C:\OMSI\` y `c:\omsi\sub\..` comparten un mismo lease; el nombre del pipe de control local usa la misma normalización. Lo toman `StartSessionAsync` (estado `AcquiringInstallationLock`) y `RecoverPendingAsync`, y se libera cuando termina la tarea del ciclo de vida de la sesión o cuando retorna la llamada de recuperación. `OL_E_INSTALLATION_BUSY` se genera de inmediato cuando no se puede tomar (sin espera).

Limitaciones aceptadas (documentadas, sin cambio previsto):

- Alcance `Local\`: un propietario por instalación **por sesión de inicio de sesión de Windows**. Dos usuarios interactivos en la misma computadora no se excluyen mutuamente.
- Un semáforo no se libera por un fallo mientras cualquier otro proceso todavía tenga un handle hacia él; a diferencia de un mutex abandonado, no tiene propietario. Un poseedor obsoleto deja la instalación en `OL_E_INSTALLATION_BUSY` hasta que ese handle se cierre.
- Cualquier proceso del mismo usuario de Windows puede crear el nombre primero y retenerlo.

<a id="omsilaunch-directory"></a>
## Directorio `.omsilaunch`

| Entrada | Duración | Propietario |
| --- | --- | --- |
| `journal.json` | Temporal; existe solo mientras hay una transacción pendiente | Transacción |
| `backup\<sessionId>\*.bin` | Temporal; se elimina después del journal | Transacción |
| `diagnostics\<sessionId>-host.log` | Permanente; la retención conserva las 50 sesiones más recientes (los archivos más antiguos con prefijo de sesión se eliminan cuando se inicia una sesión nueva) | Traza del host |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanente (misma retención) | CLI |
| `diagnostics\tray-host.log` | Permanente | Host de la bandeja de Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Assets permanentes del producto; se copian una vez desde el paquete, nunca se sobrescriben ni se eliminan | Assets visuales de la sesión |
| `session-profiles\<id>\` | Permanente; lo instala el usuario o el autor del contenido | Usuario |
| `profiles\` | El código actual no lo crea ni lo lee; reservado | ninguno |
| `docs\`, `examples\` | Permanente; lo incluye el paquete de release | Paquete |

Ningún dato sale de la computadora; los diagnósticos son solo archivos locales. Consulte también [directorio `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Las mutaciones de runtime no se registran en el journal

Las operaciones de control de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, texturas D3D) cambian solo el estado en memoria de OMSI. No se registran en el journal y no se restauran; desaparecen con el proceso. Consulte [control de runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Modos de falla y códigos de error

| Código | Significado | Journal después |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Otro propietario tiene el lease, o el proceso de OMSI registrado en el journal sigue vivo | se conserva |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Se solicitó la restauración de una transacción con snapshots pero sin journal en el disco | n/a |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | El hash de una copia de seguridad difiere de la huella del snapshot; no se escribió nada | se conserva |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Una ruta de overlay originalmente ausente ahora contiene contenido que la sesión no escribió | se conserva |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Journal anterior a las huellas de propiedad con una ruta originalmente ausente que ahora existe; solo una sesión nueva con bytes planificados idénticos puede cerrarlo | se conserva (postergado) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | No se pudo eliminar `journal.json` después de una restauración verificada | se conserva (la repetición es idempotente) |
| `OL_E_RESTORE_DEFERRED` | No se confirmó la salida de OMSI; la restauración se posterga hasta el siguiente inicio | se conserva |
| `OL_E_RESTORE_FAILED` | Cualquier otra falla de restauración (discrepancia de presencia o de hash después de restaurar, error de E/S) | se conserva |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | No se pudo eliminar un `closecheck` obsoleto antes de la transacción | todavía ninguno |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Advertencia: se conservó un archivo ajeno en una ruta de eliminación de la sesión | completado |
| `OL_E_PLAN_NOT_RUNNABLE` | Al volver a planificar en el inicio se determinó que la especificación ya no es ejecutable (por ejemplo, un `Omsi.exe` modificado); no se abre ninguna transacción | ninguno |

La CLI asigna `OL_E_RECOVERY_*` y `OL_E_RESTORE_FAILED` al código de salida 8 y `OL_E_INSTALLATION_BUSY` al código de salida 7; consulte [códigos de salida](../reference/exit-codes.md).

<a id="evidence"></a>
## Evidencia

Las pruebas offline en `tools/OmsiLaunch.TestHost` cubren las rutas de la transacción: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Evidencia de runtime (matriz de validación): RV-005 y RV-006 (overlay aplicado y restauración exacta byte a byte, sesiones `1e8e0548-...` y el lote de presentación), RV-008 con resultado aprobado para la salida temprana (sesión `0dc40570-...`).

Evidencia de runtime (ronda de cierre de runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- eliminación de artefactos de la sesión para los destinos de `.itx` con el descargador real de OMSI, en una detención normal y después de la interrupción del propietario seguida de `/recover` (S-01, `I01`, `I02`);
- recuperación temprana antes de construir los overlays (S-05, `S05`);
- restauración de metadatos y de archivos de solo lectura, más la limpieza de las copias de seguridad (S-12, `S12a`, `S12b`);
- `/recover` bajo el lease, con un OMSI huérfano y en la ventana anterior al PID (S-04, `S04`, `S04b`);
- fallas de arranque y una restauración fallida seguida de `/recover` (resto de RV-008, `SF01`, `SF02`, `F01`);
- conservación de CP1252 (S-07, `C01`).

La rama de recuperación postergada para journals anteriores a las huellas de propiedad sigue siendo solo offline. Consulte [estado de la validación en runtime](../status/runtime-validation-status.md).
