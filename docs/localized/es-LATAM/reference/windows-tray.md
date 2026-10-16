# Indicador de la bandeja de Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Traducción de la [página original en inglés](../../../reference/windows-tray.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

Cada sesión de propietario independiente (iniciada con `OmsiLaunch.exe` o `OmsiLaunchW.exe`) muestra un ícono en el área de notificación que informa sobre la sesión y permite al usuario finalizarla. Esta página especifica el indicador tal como lo implementan `SessionTrayIndicator`, `StatusWindow` y `StopConfirmationWindow` en `tools\OmsiLaunch.Cli\WindowsHost.cs`, el presentador de solo lectura `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) y el registro de cadenas `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), junto con los diálogos de error de `OmsiLaunchW.exe` de `WindowsHost`. La bandeja es solo un adaptador de presentación: no es propietaria ni de OMSI ni de la recuperación, y su acción de detención señaliza la misma ruta de detención canónica del propietario que `session stop` (consulte la [referencia de la CLI](cli.md) y el [control local](local-control.md)).

<a id="when-the-icon-exists"></a>
## Cuándo existe el ícono

| Paso | Comportamiento |
|---|---|
| Creación | Inmediatamente después de que `StartSessionAsync` retorna, antes de que la sesión llegue a `Running`, salvo que `SuppressTrayIcon` esté definido. Por lo tanto, el ícono existe durante `StartingProcess`, `WaitingForPlugin`, `StartingWorld` y `EnteringGameplay`. |
| Presupuesto de arranque | El thread de la interfaz (`STA`, en segundo plano, con el nombre `OmsiLaunch tray`) debe publicar el ícono en 2 s. De lo contrario, o ante cualquier excepción durante la creación, el indicador se libera y la sesión continúa **sin** ícono; se registra `startup-timeout` o la excepción. Un inicio lento nunca deja un ícono visible huérfano. |
| Eliminación | En el bloque `finally` del propietario, después de que la sesión se completó o falló y antes de `CloseAsync`. La liberación envía el apagado al thread de la interfaz (cierra el menú, la confirmación y la ventana de estado, y luego termina el bucle de mensajes), espera la finalización del thread (join) hasta 2 s (se registra `dispose-timeout` si se excede), y oculta y libera el `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (un campo de `LaunchSpec`, `Presentation.SuppressTrayIcon`, valor predeterminado `false`). Se puede definir mediante `/spec` y la API; no hay ningún flag de la CLI. Los integradores que muestran su propio elemento de interfaz para la sesión lo definen en `true`; nada más cambia en la sesión. |
| Hosts | Tanto `OmsiLaunch.exe` (consola) como `OmsiLaunchW.exe` (subsistema de Windows) muestran el ícono; las sesiones de `OmsiLaunchW.exe` iniciadas con `/silent` no tienen ninguna otra superficie visible. |

<a id="icon-and-tooltip"></a>
## Ícono y tooltip

- Ícono: el ícono asociado al archivo ejecutable en ejecución (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, el ícono de OmsiLaunch incrustado), con `SystemIcons.Application` como alternativa.
- Texto del tooltip (texto emergente): `Tray.Running` (`OmsiLaunch is running`, es decir, "OmsiLaunch se está ejecutando"). El texto no cambia durante la detención (todavía no existe una cadena de "deteniendo"; el ícono queda igual hasta que el propietario lo quita).
- Los estilos visuales están habilitados (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interacción

| Action | Resultado |
|---|---|
| Clic derecho | Convierte la ventana de la bandeja en la ventana de primer plano (necesario para los menús de íconos de notificación; sin esto, el menú puede ignorar los clics y no cerrarse nunca mientras OMSI está al frente) y luego abre el menú contextual en la posición real del cursor (`Cursor.Position`, no las coordenadas del evento, porque `NotifyIcon` puede reportar `(0,0)` para eventos alojados por el shell). El menú se ajusta al área de trabajo de la pantalla que está bajo el cursor. |
| Doble clic | Abre la ventana de estado (igual que el elemento de menú `Status`). |
| Clic izquierdo | Ninguna acción. |
| Elemento de menú `Status` (`Tray.Status`) | Abre (o activa, si ya está abierta) la ventana de estado de solo lectura. |
| Separador | |
| Elemento de menú `End session` (`Tray.EndSession`, descripción accesible `Tray.EndSessionDescription`) | Abre el diálogo de confirmación. |

<a id="status-window-read-only-snapshot"></a>
### Ventana de estado (snapshot de solo lectura)

Se abre con el elemento de menú `Status` (estado) o con un doble clic en el ícono (`SessionTrayIndicator.ShowStatus`). Es un diálogo fijo, centrado y de tamaño automático, sin entrada en la barra de tareas y con un único botón `Close` (`Status.Close`, cerrar; `Escape` también la cierra). Elegir `Status` mientras la ventana ya está abierta activa esa ventana sin reconstruirla (conserva el snapshot de su primera apertura). Una falla al construir la ventana se escribe en `tray-host.log`; no afecta a la sesión.

**Es un snapshot, no una vista en vivo.** `SessionStatusPresenter.Create(plan, status, ui)` se ejecuta una vez al abrir la ventana: lee el `SessionPlan` resuelto de la sesión (la especificación que realmente se planificó) y un `SessionStatus` (solo su `State`). Nada se actualiza mientras la ventana permanece abierta, y nunca consulta a OMSI (ninguna operación de runtime, ningún valor de telemetría). Ciérrela y vuelva a abrirla para ver un estado más reciente.

Título y encabezado de la ventana: el mismo texto, `Status.SessionRunning` (`Session is running`, "la sesión se está ejecutando") cuando `SessionStatus.State` es `Running`; en caso contrario, el nombre sin procesar de `SessionState` (por ejemplo, `WaitingForPlugin` si se abre durante el arranque, ya que el ícono existe antes de `Running`). `Status.Title` (`OmsiLaunch session status`) está definido en la tabla de cadenas, pero esta release no lo usa.

Las secciones aparecen en este orden, y una sección se omite cuando no tiene campos. Cada valor proviene del `LaunchSpec`/`SessionPlan` planificado, nunca de OMSI.

| Sección (etiqueta en inglés) | Campo (etiqueta en inglés) | Se muestra cuando | Valor | Origen (equivalente público) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | siempre | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (cualquier otro modo; nunca se alcanza porque `LastMapState` no es ejecutable) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | el plan resolvió una identidad de contenido `map` | el `DisplayName` del mapa (el nombre del directorio del mapa, por ejemplo `Grundorf`); si no, el nombre de archivo de la identidad sin extensión | Entrada de `SessionPlan.ResolvedContent` con `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` da el mismo `DisplayName` |
| `Session` | `Situation` | `SavedSituation` con una identidad de situación | nombre de archivo sin extensión (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | se solicitó un punto de entrada | la identidad del punto de entrada si está definida (nunca ejecutable en esta release); si no, el índice presentado como entero (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | se usó un perfil de sesión (`/predefined-profile`) | `name` del perfil | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | igual que arriba, cuando el preset tiene nombre | `name` del preset | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | se solicitó una fecha, hora o clima explícito o del sistema | `DD/MM/YYYY` o `System`; `HH:MM:SS` o `System`; código ICAO, nombre del preset o `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | se solicitó un campo del vehículo del jugador | la identidad o el valor solicitado | `Spec.PlayerVehicle` |
| `Configuration` | un campo por cada ajuste semántico | se definió un ajuste (`/set`, `settings` del perfil, `LaunchSpec.Environment.*`) y `ConfigurationCatalog` lo conoce | el valor solicitado; se agrega `%` a las claves que terminan en `Percent` y ` m` a las que terminan en `DistanceMeters`. La etiqueta es la clave del ajuste con cada parte separada por puntos en mayúscula inicial (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; los mismos valores son las `PlannedMutations` del plan |
| `Presentation` | `Splash` | siempre | `Managed` u `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | siempre | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

La sección `Environment` aparece en una sesión de Beta 4 en ejecución cuando se solicitó una fecha, hora, año o clima (esas solicitudes son experimentales, consulte [limitaciones conocidas](known-limitations.md)). La sección `Vehicle` nunca puede aparecer en una sesión en ejecución: solicitar cualquier campo del vehículo del jugador hace que el plan no sea ejecutable, por lo que no se inicia ninguna sesión de ese tipo. Existe para un build futuro y está cubierta por la prueba offline del presentador.

Evidencia de runtime (interfaz pt-BR, closure de runtime `T01`): una sesión NEW_MAP de Grundorf mostró `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Los mismos datos están disponibles para herramientas sin la bandeja: `session status` a través del [plano de control local](local-control.md) proporciona `SessionId`, `State`, `Diagnostics` y `RuntimeEvents` (en vivo), y la salida `/plan --json` del propietario (o `PlanSessionAsync`) proporciona la especificación planificada, el contenido resuelto y las mutaciones planificadas que la ventana resume.

<a id="end-session-with-confirmation"></a>
### Finalizar la sesión (con confirmación)

1. `StopConfirmationWindow`: título `End session?` ("¿Finalizar la sesión?"), mensaje `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (OMSI 2 se cerrará y la sesión administrada por OmsiLaunch finalizará), botones `End session` (predeterminado, `DialogResult.OK`) y `Cancel` (`Escape`). Una segunda solicitud mientras el diálogo está abierto lo activa en lugar de apilar otro.
2. Con `OK`, la bandeja llama a `requestCanonicalStop`, que completa la señal `controlStopped` del propietario; luego el propietario llama a `StopAsync`: OMSI se termina con `TerminateProcess` y se restaura cada archivo que pertenece a la sesión. La bandeja nunca termina OMSI por sí misma.
3. Si la solicitud lanza una excepción, el error se registra y se muestra `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`, es decir, no se pudo finalizar la sesión y OMSI y su sesión administrada siguen activos).
4. La bandeja no confirma el éxito; el ícono desaparece cuando el propietario termina la restauración y libera el indicador (closure de runtime `T01`: el propietario terminó 607 ms después de confirmar `End session`).
5. `Cancel` (o cerrar el diálogo) no hace nada: la sesión sigue ejecutándose (`T01`).
6. Una detención que llega desde otro lugar (`session stop`, Ctrl+C, `/observe-seconds`, la salida de OMSI) mientras la ventana de estado o el diálogo de confirmación están abiertos los cierra como parte de la liberación del indicador; el propietario no espera al usuario (`T02`: el propietario terminó 725 ms después de la detención por pipe con ambas ventanas abiertas).

<a id="explorer-restart"></a>
## Reinicio del Explorador de Windows

`TrayWindow` es una ventana nativa oculta que registra el mensaje de ventana `TaskbarCreated`. Cuando el Explorador (el shell) se reinicia, difunde ese mensaje y el indicador vuelve a agregar el ícono (`Visible = false; Visible = true`). Closure de runtime `T01`: después de que `explorer.exe` se terminó y Windows lo reinició, el ícono volvió a estar en `Shell_TrayWnd` y el menú y la ventana de estado siguieron funcionando.

<a id="localization"></a>
## Localización

`WindowsUiStrings.Resolve` sigue la **cultura de la interfaz de Windows** (`CultureInfo.CurrentUICulture`), nunca el idioma del contenido de OMSI ni el idioma de un perfil de sesión. Orden de resolución: nombre exacto de la cultura, luego idioma de dos letras, luego inglés. Cada clave recurre al inglés cuando una traducción no la tiene.

| Claves de cultura | Idioma |
|---|---|
| `en`, `en-US`, `en-GB` | Inglés (predeterminado y de respaldo) |
| `pt-BR` | Portugués de Brasil. `pt-PT` (y `pt` sin región) recurre deliberadamente al inglés. |
| `de`, `de-DE` | Alemán |
| `fr`, `fr-FR` | Francés |
| `pl`, `pl-PL` | Polaco |

Las cadenas localizadas abarcan el tooltip, los dos elementos de menú, la ventana de estado (encabezado, títulos de sección, etiquetas de campo, `Close`), los valores de modo y de presentación, y el diálogo de confirmación. El glosario se mantiene en `docs\windows-ui-localization.md`; la prueba offline `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifica la resolución y el presentador.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Diálogos de error de `OmsiLaunchW.exe`

Cuando `OMSILAUNCH_WINDOWS_HOST=1` (definido por `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` reemplaza la salida de errores de la consola por un cuadro de mensaje modal con el título `OmsiLaunch` (ícono de error): `<message>`, línea en blanco, `Code: OL_E_...`, línea en blanco, `See .omsilaunch\diagnostics for details.` (consulte `.omsilaunch\diagnostics` para más detalles). Lo muestra cada `CliInput.WriteError` (errores de argumentos, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, excepciones clasificadas), cuando un plan de inicio no es ejecutable (texto de respaldo `The session plan is not runnable.`; auditoría de documentación BUG-06) y cuando la sesión no llega a `Running` (`The OMSI session did not reach gameplay.` con el último diagnóstico `OL_E_`, u `OL_E_SESSION_START_FAILED` si no hay ninguno). El comportamiento completo de `OmsiLaunchW.exe` se describe en la [referencia de OmsiLaunchW.exe](omsilaunchw.md). Con `OmsiLaunch.exe`, la misma función no hace nada. Una falla de inicio del host .NET (códigos del shim `100`..`106`) la muestra el propio shim nativo; consulte [códigos de salida](exit-codes.md).

<a id="log-location"></a>
## Ubicación del log

`<root>\.omsilaunch\diagnostics\tray-host.log`, una línea por entrada: marca de tiempo UTC ISO-8601, una tabulación y luego la entrada. Entradas: `created`, `removed`, `startup-timeout`, `startup-cancelled` (una liberación compitió con el arranque y se omitió el bucle), `dispose-timeout`, y los textos completos de las excepciones para fallas de la interfaz. El registro se hace con el mejor esfuerzo y nunca lanza excepciones. Los logs del host de la sesión (`<sessionId>-host.log`) los escribe el propietario en el mismo directorio; el log de la bandeja no lleva el prefijo de la sesión y no se depura con la retención de 50 sesiones.

<a id="lifecycle-guarantees"></a>
## Garantías del ciclo de vida

- La bandeja nunca es propietaria de la sesión: no puede iniciar OMSI, no puede restaurar archivos y no puede eludir la ruta de detención del propietario.
- Cada ruta de salida del propietario (finalización normal, salida de OMSI, Ctrl+C, cierre de la consola, detención por pipe, excepción, `/observe-seconds`) libera el indicador antes de `CloseAsync`, por lo que ningún ícono sobrevive a su sesión, salvo cuando el proceso propietario se termina de forma forzada (Windows quita los íconos huérfanos al pasar el mouse por encima la próxima vez).
- La creación y la liberación se serializan bajo un lock: una liberación que gana la carrera hace que el thread de la interfaz omita su bucle de mensajes y limpie de inmediato.
- Todo el trabajo de Windows Forms ocurre en el thread STA dedicado; los threads ajenos solo le envían trabajo a través de un control de marshalling oculto.

<a id="residual-caveats-from-the-code-comments"></a>
## Salvedades residuales (según los comentarios del código)

- No existe un texto de tooltip de "deteniendo"; el ícono muestra `OmsiLaunch is running` hasta que se quita.
- `NotifyIcon` puede reportar coordenadas del mouse `(0,0)` para eventos alojados por el shell; en su lugar se lee la posición del cursor.
- Los mensajes de la bandeja siguen llegando mientras el diálogo de confirmación es modal; no se apila una segunda confirmación.
- Si el thread de la interfaz no termina dentro del presupuesto de liberación de 2 s, el propietario continúa sin esperar (`dispose-timeout`).
- La ventana de estado es un snapshot de los valores planificados tomado al abrirla; no se actualiza y nunca lee datos de OMSI.

<a id="runtime-evidence"></a>
## Evidencia de runtime

Observado en sesiones reales sobre la instalación autorizada en la ronda de closure de runtime (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, interfaz de Windows en pt-BR; consulte el [estado de la validación en runtime](../status/runtime-validation-status.md)):

- El ícono se registra en el área de notificación real (`Shell_TrayWnd`) bajo `OmsiLaunchW.exe` y se quita después de la restauración (`T01`..`T04`).
- "End session" con confirmación ejecuta la detención canónica y la restauración exacta; `Cancel` mantiene la sesión en ejecución (`T01`, `T03`).
- El ícono se vuelve a crear después de un reinicio del Explorador de Windows (`TaskbarCreated`, `T01`).
- El propietario cierra las ventanas de estado y de confirmación cuando llega una detención mientras están abiertas (`T02`).
- Diálogos de error de `OmsiLaunchW.exe` para un error de argumento (`OL_E_INVALID_ARGUMENT`), la ausencia de sesión activa (`OL_E_NO_ACTIVE_SESSION`) y una sesión que falla antes de llegar al juego (`OL_E_WORLD_START_FAILED`) (`T04`). En el último caso, el diálogo muestra como mensaje los datos de la falla enviados por el plugin.
- `/silent` se desvincula: el lanzador retorna mientras el host de Windows mantiene la sesión (`T04`).
- El presupuesto de 4 s de `ProcessExit` al cerrar la consola (`L04`, propietario de consola).

No producido: los diálogos de códigos de salida del shim de arranque (`100`..`106`).
