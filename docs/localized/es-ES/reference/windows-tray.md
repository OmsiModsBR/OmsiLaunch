# Indicador de la bandeja de Windows

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Traducción de la [página original en inglés](../../../reference/windows-tray.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Toda sesión de propietario independiente (iniciada con `OmsiLaunch.exe` o `OmsiLaunchW.exe`) muestra un icono en el área de notificación que informa sobre la sesión y permite al usuario finalizarla. Esta página especifica el indicador tal como lo implementan `SessionTrayIndicator`, `StatusWindow` y `StopConfirmationWindow` en `tools\OmsiLaunch.Cli\WindowsHost.cs`, el presentador de solo lectura `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) y el registro de cadenas `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`), junto con los cuadros de diálogo de error de `OmsiLaunchW.exe` de `WindowsHost`. La bandeja es solo un adaptador de presentación: no es propietaria ni de OMSI ni de la recuperación, y su acción de detención señaliza la misma ruta de detención canónica del propietario que `session stop` (consulta la [referencia de la CLI](cli.md) y el [control local](local-control.md)).

<a id="when-the-icon-exists"></a>
## Cuándo existe el icono

| Paso | Comportamiento |
|---|---|
| Creación | Inmediatamente después de que `StartSessionAsync` devuelva el control, antes de que la sesión alcance `Running`, salvo que esté establecido `SuppressTrayIcon`. Por tanto, el icono existe durante `StartingProcess`, `WaitingForPlugin`, `StartingWorld` y `EnteringGameplay`. |
| Presupuesto de arranque | El thread de la UI (`STA`, en segundo plano, con el nombre `OmsiLaunch tray`) debe publicar el icono en un plazo de 2 s. En caso contrario, o ante cualquier excepción durante la creación, el indicador se desecha y la sesión continúa **sin** icono; se registra `startup-timeout` o la excepción. Un inicio lento nunca deja huérfano un icono visible. |
| Eliminación | En el bloque `finally` del propietario, después de que la sesión se haya completado o haya fallado y antes de `CloseAsync`. El desecho envía el cierre al thread de la UI (cierra el menú, la confirmación y la ventana de estado y, después, termina el bucle de mensajes), espera al thread durante un máximo de 2 s (se registra `dispose-timeout` si se supera), oculta y desecha el `NotifyIcon`. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (un campo de `LaunchSpec`, `Presentation.SuppressTrayIcon`, valor predeterminado `false`). Se puede establecer mediante `/spec` y la API; no existe ningún flag de la CLI. Los integradores que muestran su propio elemento de interfaz para la sesión lo establecen en `true`; nada más de la sesión cambia. |
| Hosts | Tanto `OmsiLaunch.exe` (consola) como `OmsiLaunchW.exe` (subsistema de Windows) muestran el icono; las sesiones de `OmsiLaunchW.exe` iniciadas con `/silent` no tienen ninguna otra superficie visible. |

<a id="icon-and-tooltip"></a>
## Icono y tooltip

- Icono: el icono asociado al ejecutable en ejecución (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, el icono incrustado de OmsiLaunch), con `SystemIcons.Application` como alternativa.
- Texto del tooltip: `Tray.Running` (`OmsiLaunch is running`, «OmsiLaunch se está ejecutando»). El texto no cambia durante la detención (todavía no existe ninguna cadena de «deteniendo»; el icono permanece como está hasta que el propietario lo elimina).
- Los estilos visuales están habilitados (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interacción

| Action | Resultado |
|---|---|
| Clic derecho | Convierte la ventana de la bandeja en la ventana en primer plano (necesario para los menús de iconos de notificación; sin ello el menú puede ignorar los clics y no cerrarse nunca mientras OMSI está delante) y, después, abre el menú contextual en la posición real del cursor (`Cursor.Position`, no las coordenadas del evento, porque `NotifyIcon` puede notificar `(0,0)` para eventos alojados por el shell). El menú se ajusta al área de trabajo de la pantalla que hay bajo el cursor. |
| Doble clic | Abre la ventana de estado (igual que el elemento de menú `Status`). |
| Clic izquierdo | Ninguna acción. |
| Elemento de menú `Status` (`Tray.Status`) | Abre (o activa, si ya está abierta) la ventana de estado de solo lectura. |
| Separador | |
| Elemento de menú `End session` (`Tray.EndSession`, descripción accesible `Tray.EndSessionDescription`) | Abre el cuadro de diálogo de confirmación. |

<a id="status-window-read-only-snapshot"></a>
### Ventana de estado (snapshot de solo lectura)

Se abre con el elemento de menú `Status` (estado) o con un doble clic en el icono (`SessionTrayIndicator.ShowStatus`). Es un cuadro de diálogo fijo, centrado y de tamaño automático, sin entrada en la barra de tareas y con un único botón `Close` (cerrar; `Status.Close`; `Escape` también lo cierra). Si se elige `Status` mientras la ventana ya está abierta, se activa esa ventana sin reconstruirla (conserva el snapshot de su primera apertura). Un fallo al construir la ventana se escribe en `tray-host.log`; no afecta a la sesión.

**Es un snapshot, no una vista en directo.** `SessionStatusPresenter.Create(plan, status, ui)` se ejecuta una sola vez cuando se abre la ventana: lee el `SessionPlan` resuelto de la sesión (la spec que realmente se planificó) y un `SessionStatus` (solo su `State`). No se actualiza nada mientras la ventana permanece abierta, y nunca consulta a OMSI (ninguna operación de runtime, ningún valor de telemetría). Ciérrala y vuelve a abrirla para ver un estado más reciente.

Título y encabezado de la ventana: el mismo texto, `Status.SessionRunning` (`Session is running`, «la sesión se está ejecutando») cuando `SessionStatus.State` es `Running`; en caso contrario, el nombre sin formato de `SessionState` (por ejemplo, `WaitingForPlugin` si se abre durante el arranque, ya que el icono existe antes de `Running`). `Status.Title` (`OmsiLaunch session status`) está definido en la tabla de cadenas, pero esta versión no lo usa.

Las secciones aparecen en este orden, y una sección se omite cuando no tiene campos. Todos los valores proceden del `LaunchSpec`/`SessionPlan` planificado, nunca de OMSI.

| Sección (etiqueta en inglés) | Campo (etiqueta en inglés) | Se muestra cuando | Valor | Origen (equivalente público) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | siempre | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (cualquier otro modo; nunca se alcanza porque `LastMapState` no es ejecutable) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | el plan resolvió una identidad de contenido `map` | el `DisplayName` del mapa (el nombre del directorio del mapa, por ejemplo `Grundorf`); si no, el nombre de archivo de la identidad sin extensión | Entrada de `SessionPlan.ResolvedContent` con `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` da el mismo `DisplayName` |
| `Session` | `Situation` | `SavedSituation` con una identidad de situación | nombre de archivo sin extensión (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | se solicitó un punto de entrada | la identidad del punto de entrada si está establecida (nunca es ejecutable en esta versión); si no, el índice presentado como entero (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | se usó un perfil de sesión (`/predefined-profile`) | `name` del perfil | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | como arriba, cuando el preset tiene nombre | `name` del preset | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | se solicitó una fecha, hora o meteorología explícita/del sistema | `DD/MM/YYYY` o `System`; `HH:MM:SS` o `System`; código ICAO, nombre de preset o `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | se solicitó un campo del vehículo del jugador | la identidad/el valor solicitado | `Spec.PlayerVehicle` |
| `Configuration` | un campo por cada ajuste semántico | se estableció un ajuste (`/set`, `settings` del perfil, `LaunchSpec.Environment.*`) y `ConfigurationCatalog` lo conoce | el valor solicitado; se añade `%` a las claves que terminan en `Percent` y ` m` a las que terminan en `DistanceMeters`. La etiqueta es la clave del ajuste con cada parte separada por puntos en mayúscula inicial (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; los mismos valores son las `PlannedMutations` del plan |
| `Presentation` | `Splash` | siempre | `Managed` u `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | siempre | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

La sección `Environment` aparece en una sesión de Beta 4 en ejecución cuando se solicitó una fecha, hora, año o meteorología (esas solicitudes son experimentales, consulta las [limitaciones conocidas](known-limitations.md)). La sección `Vehicle` nunca puede aparecer en una sesión en ejecución: solicitar cualquier campo del vehículo del jugador hace que el plan no sea ejecutable, por lo que no se inicia ninguna sesión de ese tipo. Existe para un build futuro y la cubre la prueba offline del presentador.

Evidencia de runtime (UI en pt-BR, cierre de runtime `T01`): una sesión NEW_MAP de Grundorf mostró `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Los mismos datos están disponibles para las herramientas sin la bandeja: `session status` a través del [plano de control local](local-control.md) proporciona `SessionId`, `State`, `Diagnostics` y `RuntimeEvents` (en directo), y la salida `/plan --json` del propietario (o `PlanSessionAsync`) proporciona la spec planificada, el contenido resuelto y las mutaciones planificadas que resume la ventana.

<a id="end-session-with-confirmation"></a>
### Finalizar la sesión (con confirmación)

1. `StopConfirmationWindow`: título `End session?` («¿finalizar la sesión?»), mensaje `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (se cerrará OMSI 2 y terminará la sesión gestionada por OmsiLaunch), botones `End session` (finalizar la sesión; predeterminado, `DialogResult.OK`) y `Cancel` (cancelar; `Escape`). Una segunda solicitud mientras el cuadro de diálogo está abierto lo activa en lugar de apilar otro.
2. Con `OK`, la bandeja llama a `requestCanonicalStop`, que completa la señal `controlStopped` del propietario; a continuación, el propietario llama a `StopAsync`: OMSI se termina con `TerminateProcess` y se restauran todos los archivos que pertenecen a la sesión. La bandeja nunca termina OMSI por sí misma.
3. Si la solicitud lanza una excepción, el error se registra y se muestra `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`, es decir, no se pudo finalizar la sesión y OMSI y su sesión gestionada siguen activos).
4. La bandeja no confirma el éxito; el icono desaparece cuando el propietario termina la restauración y desecha el indicador (cierre de runtime `T01`: el propietario terminó 607 ms después de confirmar `End session`).
5. `Cancel` (o cerrar el cuadro de diálogo) no hace nada: la sesión sigue ejecutándose (`T01`).
6. Una detención que llega desde otro lugar (`session stop`, Ctrl+C, `/observe-seconds`, la salida de OMSI) mientras la ventana de estado o el cuadro de diálogo de confirmación están abiertos los cierra como parte del desecho del indicador; el propietario no espera al usuario (`T02`: el propietario terminó 725 ms después de la detención por la canalización con ambas ventanas abiertas).

<a id="explorer-restart"></a>
## Reinicio del Explorador

`TrayWindow` es una ventana nativa oculta que registra el mensaje de ventana `TaskbarCreated`. Cuando el Explorador (el shell) se reinicia, difunde ese mensaje y el indicador vuelve a añadir el icono (`Visible = false; Visible = true`). Cierre de runtime `T01`: después de que `explorer.exe` se terminara y Windows lo reiniciara, el icono volvió a estar en `Shell_TrayWnd` y el menú y la ventana de estado siguieron funcionando.

<a id="localization"></a>
## Localización

`WindowsUiStrings.Resolve` sigue la **referencia cultural de la UI de Windows** (`CultureInfo.CurrentUICulture`), nunca el idioma del contenido de OMSI ni el idioma de un perfil de sesión. Orden de resolución: nombre exacto de la referencia cultural, después idioma de dos letras y, por último, inglés. Cada clave recurre al inglés cuando una traducción no la incluye.

| Claves de referencia cultural | Idioma |
|---|---|
| `en`, `en-US`, `en-GB` | Inglés (predeterminado y alternativa) |
| `pt-BR` | Portugués de Brasil. `pt-PT` (y `pt` sin más) recurre deliberadamente al inglés. |
| `de`, `de-DE` | Alemán |
| `fr`, `fr-FR` | Francés |
| `pl`, `pl-PL` | Polaco |

Las cadenas localizadas abarcan el tooltip, los dos elementos de menú, la ventana de estado (encabezado, títulos de sección, etiquetas de campo, `Close`), los valores de modo y de presentación, y el cuadro de diálogo de confirmación. El glosario se mantiene en `docs\windows-ui-localization.md`; la prueba offline `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifica la resolución y el presentador.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Cuadros de diálogo de error de `OmsiLaunchW.exe`

Cuando `OMSILAUNCH_WINDOWS_HOST=1` (lo establece `OmsiLaunchW.exe`), `WindowsHost.ShowFailure` sustituye la salida de errores de la consola por un cuadro de mensaje modal con el título `OmsiLaunch` (icono de error): `<message>`, línea en blanco, `Code: OL_E_...`, línea en blanco, `See .omsilaunch\diagnostics for details.` (consulta `.omsilaunch\diagnostics` para más detalles). Lo muestra cada `CliInput.WriteError` (errores de argumentos, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, excepciones clasificadas), cuando un plan de lanzamiento no es ejecutable (texto de reserva `The session plan is not runnable.`; auditoría de documentación BUG-06) y cuando la sesión no llega a `Running` (`The OMSI session did not reach gameplay.` con el último diagnóstico `OL_E_`, o `OL_E_SESSION_START_FAILED` cuando no hay ninguno). El comportamiento completo de `OmsiLaunchW.exe` se describe en la [referencia de OmsiLaunchW.exe](omsilaunchw.md). Con `OmsiLaunch.exe`, la misma función no hace nada. Un fallo de inicio del host de .NET (códigos del shim `100`..`106`) lo muestra el propio shim nativo; consulta [códigos de salida](exit-codes.md).

<a id="log-location"></a>
## Ubicación del registro

`<root>\.omsilaunch\diagnostics\tray-host.log`, una línea por entrada: marca de tiempo UTC ISO-8601, un tabulador y, después, la entrada. Entradas: `created`, `removed`, `startup-timeout`, `startup-cancelled` (un desecho compitió con el arranque y se omitió el bucle), `dispose-timeout` y los textos completos de las excepciones de los fallos de la UI. El registro se hace con el mejor esfuerzo y nunca lanza excepciones. El propietario escribe los registros del host de la sesión (`<sessionId>-host.log`) en el mismo directorio; el registro de la bandeja no lleva el prefijo de la sesión y no lo depura la retención de 50 sesiones.

<a id="lifecycle-guarantees"></a>
## Garantías del ciclo de vida

- La bandeja nunca es propietaria de la sesión: no puede iniciar OMSI, no puede restaurar archivos y no puede eludir la ruta de detención del propietario.
- Todas las rutas de salida del propietario (finalización normal, salida de OMSI, Ctrl+C, cierre de la consola, detención por la canalización, excepción, `/observe-seconds`) desechan el indicador antes de `CloseAsync`, por lo que ningún icono sobrevive a su sesión salvo cuando el proceso propietario se mata directamente (Windows elimina los iconos huérfanos la siguiente vez que se pasa el ratón por encima).
- La creación y el desecho se serializan con un bloqueo: un desecho que gana la carrera hace que el thread de la UI omita su bucle de mensajes y limpie de inmediato.
- Todo el trabajo de Windows Forms se realiza en el thread STA dedicado; los threads ajenos solo le envían trabajo a través de un control de serialización oculto.

<a id="residual-caveats-from-the-code-comments"></a>
## Salvedades residuales (según los comentarios del código)

- No existe ningún texto de tooltip de «deteniendo»; el icono muestra `OmsiLaunch is running` hasta que se elimina.
- `NotifyIcon` puede notificar coordenadas de ratón `(0,0)` para eventos alojados por el shell; en su lugar se lee la posición del cursor.
- Los mensajes de la bandeja siguen llegando mientras el cuadro de diálogo de confirmación es modal; no se apila una segunda confirmación.
- Si el thread de la UI no termina dentro del presupuesto de desecho de 2 s, el propietario continúa sin esperar (`dispose-timeout`).
- La ventana de estado es un snapshot de los valores planificados tomado al abrirla; no se actualiza y nunca lee de OMSI.

<a id="runtime-evidence"></a>
## Evidencia de runtime

Observado en sesiones reales en la instalación autorizada durante la ronda de cierre de runtime (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, UI de Windows en pt-BR; consulta el [estado de la validación en runtime](../status/runtime-validation-status.md)):

- El icono se registra en el área de notificación real (`Shell_TrayWnd`) con `OmsiLaunchW.exe` y se elimina después de la restauración (`T01`..`T04`).
- «End session» con confirmación ejecuta la detención canónica y la restauración exacta; `Cancel` mantiene la sesión en ejecución (`T01`, `T03`).
- El icono se vuelve a crear después de un reinicio del Explorador (`TaskbarCreated`, `T01`).
- El propietario cierra las ventanas de estado y de confirmación cuando llega una detención mientras están abiertas (`T02`).
- Cuadros de diálogo de error de `OmsiLaunchW.exe` para un error de argumento (`OL_E_INVALID_ARGUMENT`), ausencia de sesión activa (`OL_E_NO_ACTIVE_SESSION`) y una sesión que falla antes de la fase de juego (`OL_E_WORLD_START_FAILED`) (`T04`). En este último caso, el cuadro de diálogo muestra como mensaje la carga útil de error del plugin.
- `/silent` se desacopla: el lanzador vuelve mientras el host de Windows mantiene la sesión (`T04`).
- El presupuesto de `ProcessExit` de 4 s al cerrar la consola (`L04`, propietario de consola).

No producido: los cuadros de diálogo de códigos de salida del shim bootstrapper (`100`..`106`).
