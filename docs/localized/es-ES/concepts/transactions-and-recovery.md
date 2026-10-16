# Transacciones y recuperación

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> Traducción de la [página original en inglés](../../../concepts/transactions-and-recovery.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Toda sesión de OmsiLaunch que modifica un archivo de OMSI lo hace dentro de una transacción duradera y registrada en un diario: los bytes originales se copian antes de sustituirse, el diario registra hasta dónde llegó la sesión y la restauración verifica cada copia de seguridad antes de volver a escribirla. Esta página describe esa transacción tal como la implementa `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) y como la dirigen `OmsiLaunchService.StartAsync`, `SuperviseAsync` y `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), junto con las entradas de archivos calculadas por `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). Está escrita para los usuarios que necesitan saber qué cambia una sesión y qué hace la recuperación, y para los integradores que necesitan las garantías exactas.

<a id="what-a-session-changes"></a>
## Qué cambia una sesión

En la transacción solo entran **overlays temporales**. Se calculan antes de abrir la transacción y se restauran cuando esta se cierra.

| Entrada de la sesión | Archivo(s) | Tipo |
| --- | --- | --- |
| `/set:<key>=<value>`, `settings` del perfil, `LaunchSpec.Environment.*` | `options.cfg` (parches semánticos de tokens; se conservan los bytes CP1252 y se respetan UTF-8/UTF-16 marcados con BOM) | overlay |
| Splash gestionado (`SplashMode.Managed`, el predeterminado) | `GUI\NewSplashscreen_ENG.bmp` y `GUI\NewSplashscreen_<language>.bmp` | overlay (el archivo localizado lo crea la transacción cuando la instalación no tiene ninguno) |
| Texturas de Internet `Override` | `Texture\standard.itx` | overlay |
| Texturas de Internet `Override` | cada destino listado en el `.itx`, más `Texture\standard.ipr` | eliminación de sesión |
| Siempre | `closecheck` (cuando no existe antes de la sesión) | eliminación de sesión |

Los archivos permanentes del producto **no** participan en la transacción: el conjunto de archivos del plugin en `plugins\OmsiLaunch.*` (solo se valida, consulta [plugin permanente](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (se copia una vez y nunca se elimina), los diagnósticos en `.omsilaunch\diagnostics`, los paquetes de perfiles de sesión, y la documentación y los ejemplos de la versión. Los plugins de terceros y cualquier otro archivo de OMSI nunca se enumeran, copian, eliminan ni restauran.

El propio OMSI sigue escribiendo su propio estado mientras se ejecuta una sesión, exactamente igual que en un inicio normal de OMSI: `options.cfg` (por ejemplo `[last_map]` cuando la sesión carga un mapa distinto, reescrito al entrar en la fase de juego), `Texture\standard.ipr`, las cachés de horarios y de mapas de luz (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, el perfil de conductor en `Drivers\` y sus registros. Una escritura en una ruta propiedad de la sesión (véase arriba) se deshace con la restauración; cualquier otra escritura de OMSI persiste después de la sesión, igual que ocurriría tras ejecutar OMSI directamente. Evidencia de la ronda de cierre de runtime: una sesión de situación guardada en otro mapa dejó `[last_map]` modificado porque no aplicaba un overlay a `options.cfg` (`CAM01`), mientras que las sesiones con `/set` restauraron `options.cfg` exactamente (`S12a`, `S12b`, `C01`).

<a id="transaction-states"></a>
## Estados de la transacción

`TransactionState` se guarda en el diario después de cada transición. `System.Text.Json` serializa los valores como enteros.

| Valor | Estado | Se escribe cuando |
| --- | --- | --- |
| 0 | `Prepared` | Se han tomado los snapshots de todas las rutas de overlay y de eliminación y sus copias de seguridad se han volcado al disco. Todavía no ha cambiado nada en la instalación. Esta es la obligación de recuperación: a partir de aquí, un fallo brusco deja un diario recuperable. |
| 1 | `Applied` | Todos los overlays se han escrito de forma atómica y todas las eliminaciones se han realizado. |
| 2 | `RuntimeDeployed` | Se ha validado la integridad del plugin permanente para este inicio (no se despliega nada; el nombre es histórico). |
| 3 | `HandoffCreated` | El handoff de arranque, el slot de telemetría y el buzón de runtime existen como memoria compartida con nombre. |
| 4 | `ProcessStarted` | Se ha creado `Omsi.exe`. El diario incluye ahora también `ProcessId`, `ProcessStartFileTimeUtc` (hora de creación, ticks UTC) y `ExecutablePath`. |
| 5 | `ProcessExited` | El supervisor ha confirmado la salida del proceso (salida natural o `TerminateProcess`). |
| 6 | `Restoring` | Ha comenzado la restauración. |
| 7 | `Restored` | Todos los archivos propios se han restaurado y verificado. Inmediatamente después se elimina el diario y se quita `backup\<session>`. |
| 8 | `Completed` | Declarado en el enum, pero nunca se guarda; una transacción completada no tiene diario. |

El ciclo de vida de una sesión normal es, por tanto: snapshot -> `Prepared` -> overlays escritos / eliminaciones realizadas -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> diario eliminado -> `backup\<session>` quitado. Los valores públicos de `SessionState` `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` y `Completed` siguen la misma progresión desde fuera (consulta [ciclo de vida de la sesión](session-lifecycle.md)).

Una sesión sin modificaciones de archivos propios sigue escribiendo un diario para su ciclo de vida; restaurarla es una operación nula verificada.

<a id="journal-file"></a>
## Archivo del diario

Ruta: `<root>\.omsilaunch\journal.json`. Hay como máximo un diario por instalación; su presencia significa «hay una transacción pendiente».

Campos de `TransactionJournal`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `SessionId` | GUID | Sesión propietaria del diario; también es el nombre del directorio de copias de seguridad (formato `N`). |
| `State` | entero | `TransactionState` descrito arriba. |
| `Files` | array de `JournalFile` | Una entrada por cada ruta propia. |
| `ProcessId` | entero o null | PID de OMSI, a partir de `ProcessStarted`. |
| `ProcessStartFileTimeUtc` | long o null | Hora de creación de OMSI (ticks UTC), a partir de `ProcessStarted`. |
| `ExecutablePath` | string o null | Ruta completa del `Omsi.exe` lanzado, a partir de `ProcessStarted`. |

Campos de `JournalFile`:

| Campo | Tipo | Significado |
| --- | --- | --- |
| `RelativePath` | string | Ruta relativa a la raíz de la instalación (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Si el archivo existía antes de la sesión. |
| `Sha256` | string hexadecimal | SHA-256 de los bytes originales (de un array de bytes vacío cuando `Existed` es false). |
| `BackupPath` | string | Ruta absoluta de la copia de seguridad (solo se escribe cuando `Existed`). |
| `AppliedSha256` | string hexadecimal o null | SHA-256 de los bytes del overlay que la sesión escribió en esta ruta; null para las eliminaciones de sesión. Es la huella de propiedad para los archivos originalmente ausentes. |
| `LastWriteTimeUtcTicks` | long o null | Hora original de la última escritura. |
| `CreationTimeUtcTicks` | long o null | Hora original de creación. |
| `Attributes` | entero o null | `FileAttributes` originales (incluido `ReadOnly`). |
| `SessionDeletion` | bool | True para las rutas que la sesión pidió mantener ausentes (destinos del `.itx`, `Texture\standard.ipr`, `closecheck`). |

Los diarios escritos por builds anteriores sin `AppliedSha256` ni los campos de metadatos siguen pudiendo leerse; consulta [Archivos originalmente ausentes](#originally-absent-files-and-ownership).

<a id="backup-layout"></a>
## Estructura de las copias de seguridad

| Ruta | Contenido |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | Un directorio por sesión, creado junto con el diario `Prepared`. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Bytes originales exactos de un archivo propio existente. Los archivos originalmente ausentes no tienen copia de seguridad. |

Las copias de seguridad y el diario se escriben mediante un archivo temporal (`<path>.omsilaunch.tmp`), con escritura directa más un `Flush(true)` explícito, y después un `File.Move` atómico con sobrescritura. El archivo temporal se elimina siempre, incluso si hay un error. La misma ruta de escritura se utiliza para los overlays y para los originales restaurados, por lo que ningún archivo `*.omsilaunch.tmp` sobrevive a una operación completada.

Las copias de seguridad solo se eliminan después de haber eliminado el diario que las referenciaba. Un error al eliminar `backup\<session>` es estético y nunca deshace una restauración verificada.

<a id="restore"></a>
## Restauración

`RestoreAsync` se ejecuta después de `ProcessExited` (o durante la recuperación). Para cada ruta registrada en el diario:

| Estado original | Action |
| --- | --- |
| Existía | Se calcula el hash de los bytes de la copia de seguridad y se compara con `Sha256`; una discrepancia aborta con `OL_E_RECOVERY_BACKUP_CORRUPT` antes de escribir nada. Después, los bytes se escriben de forma atómica (a un archivo actual de solo lectura se le quita antes ese atributo) y se restauran la hora de creación, la hora de la última escritura y los atributos (`RestoreMetadata`; los errores de metadatos se ignoran para que un problema de permisos no pueda bloquear una restauración exacta byte a byte). |
| Ausente, ahora presente, `AppliedSha256` conocido | Se calcula el hash de los bytes actuales. Si coinciden con `AppliedSha256`, el archivo es el propio overlay de la sesión y se elimina. En caso contrario, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` aborta la restauración y se conserva el diario. |
| Ausente, ahora presente, eliminación de sesión, el diario llegó a `ProcessStarted` | El archivo es un subproducto de la sesión (OMSI se ejecutó con el lease de la instalación tomado y se pidió que esta ruta permaneciera ausente). Se elimina y se notifica como diagnóstico `restore.session-artifact-removed` con el SHA-256 del contenido eliminado. |
| Ausente, ahora presente, eliminación de sesión, el proceso nunca se inició | El archivo procede de fuera de la sesión. Se conserva, se notifica como `OL_W_RESTORE_FOREIGN_FILE_RETAINED` con su SHA-256 y la transacción se completa igualmente. |
| Ausente, ahora presente, sin evidencia de propiedad (diario anterior a las huellas) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; se conserva el diario. |
| Ausente, sigue ausente | No hay nada que hacer. |

Una vez procesados todos los archivos, `VerifyRestoredSnapshots` vuelve a leer cada ruta: el hash de los originales existentes debe coincidir con `Sha256` y las rutas originalmente ausentes deben estar ausentes, salvo que se hayan conservado explícitamente. Solo entonces se guarda `Restored`, se elimina el diario (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` si sobrevive) y se quita el directorio de copias de seguridad. Un fallo brusco entre `Restored` y la eliminación del diario solo provoca una repetición idempotente.

Las notas de restauración (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) aparecen como entradas `LaunchDiagnostic` en `SessionStatus.Diagnostics` (con `sha256` en `Data`) y en `RecoveryStatus.Diagnostics`, de modo que nada se elimina ni se conserva de forma silenciosa.

<a id="originally-absent-files-and-ownership"></a>
### Archivos originalmente ausentes y propiedad

Un overlay escrito en una ruta que no existía se elimina en la restauración **solo si su contenido sigue coincidiendo con lo que aplicó la sesión** (`AppliedSha256`). Si otra cosa lo sustituyó durante la sesión, la restauración falla con `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` y el diario se conserva para su inspección.

Una ruta de eliminación de sesión (destinos del `.itx`, `Texture\standard.ipr`, `closecheck`) que no existía antes pero sí existe después se juzga según si OMSI se ejecutó bajo esta transacción: si el diario llegó a `ProcessStarted`, el archivo es un subproducto de la sesión y se elimina (`restore.session-artifact-removed`); si el proceso nunca se inició, el archivo se conserva y se notifica como `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, y el diario se completa igualmente.

### `closecheck`

`closecheck` es el marcador de fallo brusco propio de OMSI (presente cuando OMSI no se cerró limpiamente). Se aplican dos reglas:

- Si existe **antes** de la sesión y `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` es `true` (el valor predeterminado), se elimina de forma permanente antes de abrir la transacción y se registra como diagnóstico `closecheck.stale-removed` con su SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` si la eliminación falla). Es un cambio permanente documentado, no un participante de la transacción. Con el flag a `false` el marcador permanece y OMSI muestra su advertencia.
- Si **no** existe antes de la sesión, `closecheck` se añade como eliminación de sesión. Como la sesión termina con `TerminateProcess` (la rutina de cierre de OMSI no se ejecuta), el marcador que OMSI crea al iniciarse sigue siempre ahí después; se elimina en la restauración como artefacto de la sesión.

<a id="early-recovery-order"></a>
## Orden de la recuperación temprana

En cada `StartSessionAsync`, después de tomar el lease de la instalación y antes de que nada lea la instalación activa:

1. Una transacción solo de recuperación comprueba si existe `journal.json`. Si existe, `RestorePendingAsync` se ejecuta inmediatamente, de modo que los overlays y el idioma del splash de la nueva sesión se derivan de los archivos **originales**, nunca de los restos de una sesión anterior.
2. Si esa recuperación falla con `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (un diario anterior a las huellas), la recuperación se **aplaza**: la nueva sesión construye sus overlays y la nueva transacción reintenta la recuperación usando sus propios bytes planificados como evidencia de propiedad (un archivo originalmente ausente cuyo contenido es igual al nuevo overlay se acepta como propiedad de OmsiLaunch). Cualquier otro fallo de recuperación hace fallar el inicio.
3. Solo entonces se valida el conjunto de archivos del plugin, se calcula el hash de `Omsi.exe`, se trata `closecheck` y se prepara y aplica la nueva transacción.

La traza del host registra `PENDING_JOURNAL_RECOVERED` o `PENDING_JOURNAL_RECOVERY_DEFERRED`.

<a id="crash-recovery-and-owner-liveness"></a>
## Recuperación tras un fallo brusco y comprobación de que el propietario sigue vivo

La recuperación nunca sustituye archivos por debajo de un OMSI en ejecución. `RestorePendingAsync` se niega con `OL_E_INSTALLATION_BUSY` mientras el propietario registrado en el diario sigue vivo:

| Contenido del diario | Prueba de actividad |
| --- | --- |
| `ProcessId` y `ProcessStartFileTimeUtc` registrados | El proceso con ese PID debe estar en ejecución, su hora de inicio debe coincidir (rechaza la reutilización de PID) y, cuando `ExecutablePath` está registrado, su módulo principal debe ser esa ruta (un proceso vivo no relacionado no puede retener la transacción). |
| Sin PID, estado entre `HandoffCreated` (incluido) y `ProcessExited` (excluido) | El host murió entre `CreateProcess` y la escritura del diario. Cualquier `Omsi.exe` cuyo módulo principal sea `<root>\Omsi.exe` se trata como el propietario. |
| Sin PID, otros estados | No está vivo; la recuperación continúa. |

La recuperación explícita se expone como `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)`, que devuelve `RecoveryStatus(Pending, Recovered, Diagnostics)`; toma primero el lease de la instalación (`OL_E_INSTALLATION_BUSY` cuando otro propietario lo tiene). En la CLI, `/recovery-status` informa sin restaurar y `/recover` restaura; se devuelve el código de salida 8 (`TransactionRecoveryFailed`) cuando se solicitó una restauración y el diario sigue pendiente después. Consulta [CLI](../reference/cli.md) y [API pública](../reference/public-api.md).

<a id="deferred-restore-at-session-end"></a>
### Restauración aplazada al final de la sesión

Si el supervisor no puede confirmar que OMSI ha terminado (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED` o un fallo de limpieza notificado como `OL_E_PROCESS_CLEANUP_FAILED`), la sesión falla con `OL_E_RESTORE_DEFERRED` y el diario se conserva deliberadamente: sustituir archivos de la instalación mientras OMSI todavía podría leerlos no es seguro. El siguiente inicio (o `/recover`) restaura una vez que el proceso ha desaparecido. Una restauración que falla por cualquier otro motivo finaliza la sesión con `OL_E_RESTORE_FAILED`; el diario permanece hasta que cada original propio se haya restaurado y verificado.

<a id="the-installation-lease"></a>
## El lease de la instalación

El lease es un semáforo con nombre `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` con contador 1. La raíz se normaliza mediante `InstallationLease.NormalizeRoot` (ruta completa, separadores finales eliminados salvo en la raíz de una unidad), de modo que `C:\OMSI`, `C:\OMSI\` y `c:\omsi\sub\..` comparten un mismo lease; el nombre de la canalización de control local usa la misma normalización. Lo toman `StartSessionAsync` (estado `AcquiringInstallationLock`) y `RecoverPendingAsync`, y se libera cuando la tarea del ciclo de vida de la sesión termina o cuando la llamada de recuperación retorna. `OL_E_INSTALLATION_BUSY` se genera inmediatamente cuando no se puede tomar (sin espera).

Limitaciones aceptadas (documentadas, sin cambio previsto):

- Ámbito `Local\`: un propietario por instalación **por sesión de inicio de Windows**. Dos usuarios interactivos en el mismo ordenador no se excluyen mutuamente.
- Un fallo brusco no libera un semáforo mientras cualquier otro proceso siga teniendo un handle abierto sobre él; a diferencia de un mutex abandonado, no tiene propietario. Un poseedor obsoleto deja la instalación en `OL_E_INSTALLATION_BUSY` hasta que ese handle se cierra.
- Cualquier proceso del mismo usuario de Windows puede crear el nombre primero y retenerlo.

<a id="omsilaunch-directory"></a>
## Directorio `.omsilaunch`

| Entrada | Duración | Propietario |
| --- | --- | --- |
| `journal.json` | Temporal; solo existe mientras hay una transacción pendiente | Transacción |
| `backup\<sessionId>\*.bin` | Temporal; se elimina después del diario | Transacción |
| `diagnostics\<sessionId>-host.log` | Permanente; la retención conserva las 50 sesiones más recientes (los archivos más antiguos con prefijo de sesión se eliminan cuando se inicia una sesión nueva) | Traza del host |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanente (misma retención) | CLI |
| `diagnostics\tray-host.log` | Permanente | Host de la bandeja de Windows |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Recursos permanentes del producto; se copian una vez desde el paquete y nunca se sobrescriben ni se eliminan | Recursos visuales de la sesión |
| `session-profiles\<id>\` | Permanente; lo instala el usuario o el autor del contenido | Usuario |
| `profiles\` | El código actual no lo crea ni lo lee; reservado | ninguno |
| `docs\`, `examples\` | Permanente; lo incluye el paquete de la versión | Paquete |

Ningún dato sale del ordenador; los diagnósticos son exclusivamente archivos locales. Consulta también [directorio `.omsilaunch`](../../../concepts/omsilaunch-directory.md).

<a id="runtime-mutations-are-not-journaled"></a>
## Las modificaciones de runtime no se registran en el diario

Las operaciones de control de runtime (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, texturas D3D) solo cambian el estado en memoria de OMSI. No se registran en el diario ni se restauran; desaparecen con el proceso. Consulta [control de runtime](../reference/runtime-control.md).

<a id="failure-modes-and-error-codes"></a>
## Modos de fallo y códigos de error

| Código | Significado | Diario después |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease retenido por otro propietario, o el proceso de OMSI registrado en el diario sigue vivo | se conserva |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Se solicitó la restauración de una transacción con snapshots pero sin diario en el disco | n/a |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | El hash de una copia de seguridad difiere de la huella del snapshot; no se escribió nada | se conserva |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | Una ruta de overlay originalmente ausente contiene ahora contenido que la sesión no escribió | se conserva |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Diario anterior a las huellas con una ruta originalmente ausente que ahora existe; solo una nueva sesión con bytes planificados idénticos puede cerrarlo | se conserva (aplazado) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | No se pudo eliminar `journal.json` tras una restauración verificada | se conserva (la repetición es idempotente) |
| `OL_E_RESTORE_DEFERRED` | Salida de OMSI no confirmada; restauración aplazada al siguiente inicio | se conserva |
| `OL_E_RESTORE_FAILED` | Cualquier otro fallo de restauración (discrepancia de presencia o de hash tras la restauración, error de E/S) | se conserva |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | No se pudo eliminar un `closecheck` obsoleto antes de la transacción | todavía no existe |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Advertencia: se ha conservado un archivo ajeno en una ruta de eliminación de sesión | completado |
| `OL_E_PLAN_NOT_RUNNABLE` | Al volver a planificar en el inicio se ha visto que la especificación ya no es ejecutable (por ejemplo, un `Omsi.exe` modificado); no se abre ninguna transacción | ninguno |

La CLI asigna `OL_E_RECOVERY_*` y `OL_E_RESTORE_FAILED` al código de salida 8 y `OL_E_INSTALLATION_BUSY` al código de salida 7; consulta [códigos de salida](../reference/exit-codes.md).

<a id="evidence"></a>
## Evidencia

Las pruebas offline de `tools/OmsiLaunch.TestHost` cubren las rutas de la transacción: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Evidencia de runtime (matriz de validación): RV-005 y RV-006 (overlay aplicado y restauración exacta byte a byte, sesiones `1e8e0548-...` y el lote de presentación), RV-008 superado para la salida prematura (sesión `0dc40570-...`).

Evidencia de runtime (ronda de cierre de runtime, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- eliminación de artefactos de sesión de los destinos del `.itx` con el descargador real de OMSI, en la detención normal y tras la interrupción del propietario seguida de `/recover` (S-01, `I01`, `I02`);
- recuperación temprana antes de construir los overlays (S-05, `S05`);
- restauración de metadatos y de solo lectura, más limpieza de las copias de seguridad (S-12, `S12a`, `S12b`);
- `/recover` bajo el lease, con un OMSI huérfano y en la ventana anterior al PID (S-04, `S04`, `S04b`);
- fallos de arranque y una restauración fallida seguida de `/recover` (resto de RV-008, `SF01`, `SF02`, `F01`);
- conservación de CP1252 (S-07, `C01`).

La rama de recuperación aplazada para diarios anteriores a las huellas sigue validándose solo offline. Consulta [estado de la validación en runtime](../status/runtime-validation-status.md).
