# Windows-Tray-Anzeige

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> Übersetzung der [englischen Originalseite](../../../reference/windows-tray.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Jede eigenständige Eigentümersitzung (gestartet mit `OmsiLaunch.exe` oder `OmsiLaunchW.exe`) zeigt ein Symbol im Infobereich (Benachrichtigungsbereich), das die Sitzung meldet und es dem Benutzer ermöglicht, sie zu beenden. Diese Seite spezifiziert die Anzeige so, wie sie durch `SessionTrayIndicator`, `StatusWindow` und `StopConfirmationWindow` in `tools\OmsiLaunch.Cli\WindowsHost.cs`, den schreibgeschützten Presenter `SessionStatusPresenter` (`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`) und die Zeichenfolgen-Registry `WindowsUiStrings` (`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`) implementiert ist, zusammen mit den Fehlerdialogen von `OmsiLaunchW.exe` aus `WindowsHost`. Der Tray ist ausschließlich ein Darstellungsadapter: Er besitzt weder OMSI noch die Recovery (Absturzwiederherstellung), und seine Stopp-Aktion signalisiert denselben kanonischen Stopppfad des Eigentümers wie `session stop` (siehe [CLI-Referenz](cli.md) und [lokale Steuerung](local-control.md)).

<a id="when-the-icon-exists"></a>
## Wann das Symbol existiert

| Schritt | Verhalten |
|---|---|
| Erstellung | Unmittelbar nachdem `StartSessionAsync` zurückkehrt, bevor die Sitzung `Running` erreicht, sofern `SuppressTrayIcon` nicht gesetzt ist. Das Symbol existiert daher während `StartingProcess`, `WaitingForPlugin`, `StartingWorld` und `EnteringGameplay`. |
| Startbudget | Der UI-Thread (`STA`, Hintergrund, Name `OmsiLaunch tray`) muss das Symbol innerhalb von 2 s veröffentlichen. Andernfalls, oder bei einer beliebigen Exception während der Erstellung, wird die Anzeige verworfen und die Sitzung läuft **ohne** Symbol weiter; `startup-timeout` bzw. die Exception wird protokolliert. Ein langsamer Start hinterlässt nie ein verwaistes sichtbares Symbol. |
| Entfernung | Im `finally`-Block des Eigentümers, nachdem die Sitzung abgeschlossen oder fehlgeschlagen ist und vor `CloseAsync`. Beim Verwerfen wird das Herunterfahren an den UI-Thread übergeben (schließt das Menü, die Bestätigung und das Statusfenster und beendet dann die Nachrichtenschleife), auf den Thread wird bis zu 2 s gewartet (bei Überschreitung wird `dispose-timeout` protokolliert), und das `NotifyIcon` wird ausgeblendet und verworfen. |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon` (ein `LaunchSpec`-Feld, `Presentation.SuppressTrayIcon`, Standardwert `false`). Über `/spec` und die API einstellbar; es gibt kein CLI-Flag. Integratoren, die ihr eigenes Sitzungselement darstellen, setzen es auf `true`; sonst ändert sich an der Sitzung nichts. |
| Hosts | Sowohl `OmsiLaunch.exe` (Konsole) als auch `OmsiLaunchW.exe` (Windows-Subsystem) zeigen das Symbol; mit `/silent` gestartete `OmsiLaunchW.exe`-Sitzungen haben keine andere sichtbare Oberfläche. |

<a id="icon-and-tooltip"></a>
## Symbol und Tooltip

- Symbol: das der laufenden ausführbaren Datei zugeordnete Symbol (`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`, das eingebettete OmsiLaunch-Symbol), ersatzweise `SystemIcons.Application`.
- Tooltip-Text: `Tray.Running` (`OmsiLaunch is running`; deutsch `OmsiLaunch wird ausgeführt`). Der Text ändert sich während des Beendens nicht (es gibt noch keine Zeichenfolge für „wird beendet“; das Symbol bleibt unverändert, bis der Eigentümer es entfernt).
- Visuelle Stile sind aktiviert (`Application.EnableVisualStyles`).

<a id="interaction"></a>
## Interaktion

| Action | Ergebnis |
|---|---|
| Rechtsklick | Macht das Tray-Fenster zum Vordergrundfenster (für Menüs von Infobereichssymbolen erforderlich; ohne dies kann das Menü Klicks ignorieren und sich nie schließen, solange OMSI im Vordergrund ist) und öffnet dann das Kontextmenü an der tatsächlichen Cursorposition (`Cursor.Position`, nicht die Ereigniskoordinaten, da `NotifyIcon` bei von der Shell gehosteten Ereignissen `(0,0)` melden kann). Das Menü wird in den Arbeitsbereich des Bildschirms unter dem Cursor eingepasst. |
| Doppelklick | Öffnet das Statusfenster (wie der Menüeintrag `Status`). |
| Linksklick | Keine Aktion. |
| Menüeintrag `Status` (`Tray.Status`) | Öffnet das schreibgeschützte Statusfenster (oder aktiviert es, falls es bereits geöffnet ist). |
| Trennlinie | |
| Menüeintrag `End session` (`Sitzung beenden`) (`Tray.EndSession`, barrierefreie Beschreibung `Tray.EndSessionDescription`) | Öffnet den Bestätigungsdialog. |

<a id="status-window-read-only-snapshot"></a>
### Statusfenster (schreibgeschützter Snapshot)

Wird über den Menüeintrag `Status` oder einen Doppelklick auf das Symbol geöffnet (`SessionTrayIndicator.ShowStatus`). Es ist ein fester, zentrierter Dialog mit automatischer Größe, ohne Taskleisteneintrag und mit einer einzigen Schaltfläche `Close` (`Schließen`) (`Status.Close`; auch `Escape` schließt ihn). Wird `Status` gewählt, während das Fenster bereits geöffnet ist, wird dieses Fenster aktiviert, ohne es neu aufzubauen (es behält den Snapshot seines ersten Öffnens). Ein Fehler beim Aufbau des Fensters wird in `tray-host.log` geschrieben; er beeinträchtigt die Sitzung nicht.

**Es ist ein Snapshot, keine Live-Ansicht.** `SessionStatusPresenter.Create(plan, status, ui)` wird einmal beim Öffnen des Fensters ausgeführt: Es liest den aufgelösten `SessionPlan` der Sitzung (die tatsächlich geplante Spec) und einen `SessionStatus` (nur dessen `State`). Solange das Fenster geöffnet bleibt, wird nichts aktualisiert, und es fragt nie OMSI ab (keine Runtime-Operation, keine Telemetriewerte). Schließen und öffnen Sie es erneut, um einen neueren Zustand zu sehen.

Fenstertitel und Überschrift: derselbe Text, nämlich `Status.SessionRunning` (`Session is running`; deutsch `Sitzung wird ausgeführt`), wenn `SessionStatus.State` den Wert `Running` hat, andernfalls der unveränderte `SessionState`-Name (z. B. `WaitingForPlugin`, wenn das Fenster während des Starts geöffnet wird, da das Symbol schon vor `Running` existiert). `Status.Title` (`OmsiLaunch session status`) ist in der Zeichenfolgentabelle definiert, wird in diesem Release aber nicht verwendet.

Die Abschnitte erscheinen in dieser Reihenfolge; ein Abschnitt entfällt, wenn er keine Felder hat. Jeder Wert stammt aus der geplanten `LaunchSpec`/dem geplanten `SessionPlan`, nie aus OMSI.

| Abschnitt (englische Beschriftung) | Feld (englische Beschriftung) | Angezeigt, wenn | Wert | Quelle (öffentliche Entsprechung) |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | immer | `New session` (`WorldMode.NewMap`), `Saved situation` (`WorldMode.SavedSituation`), `Last map state` (jeder andere Modus; wird nie erreicht, da `LastMapState` nicht ausführbar ist) | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | der Plan hat eine `map`-Inhaltsidentität aufgelöst | der `DisplayName` der Karte (der Name des Kartenverzeichnisses, z. B. `Grundorf`), andernfalls der Dateiname der Identität ohne Erweiterung | `SessionPlan.ResolvedContent`-Eintrag mit `Kind = "map"` (NEW_MAP); `DiscoverAsync(Maps)` liefert denselben `DisplayName` |
| `Session` | `Situation` | `SavedSituation` mit einer Situationsidentität | Dateiname ohne Erweiterung (`situations\Linie 5.osn` → `Linie 5`) | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | ein Einstiegspunkt wurde angefordert | die Identität des Einstiegspunkts, falls gesetzt (in diesem Release nie ausführbar), andernfalls der angezeigte Index als Ganzzahl (`1`) | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | ein Sitzungsprofil wurde verwendet (`/predefined-profile`) | `name` des Profils | `Spec.SessionProfile.Name` (`SessionProfileMetadata`) |
| `Session profile` | `Preset` | wie oben, wenn die Voreinstellung einen Namen hat | `name` der Voreinstellung | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`, `Time`, `Weather` | ein explizites Datum bzw. Systemdatum, eine Uhrzeit oder ein Wetter wurde angefordert | `DD/MM/YYYY` oder `System`; `HH:MM:SS` oder `System`; ICAO-Code, Name der Voreinstellung oder `Real/current` | `Spec.Date`, `Spec.Time`, `Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`, `Repaint`, `HOF`, `Fleet number`, `Registration` | ein Feld des Spielerfahrzeugs wurde angefordert | die angeforderte Identität bzw. der angeforderte Wert | `Spec.PlayerVehicle` |
| `Configuration` | ein Feld pro semantischer Einstellung | eine Einstellung wurde gesetzt (`/set`, Profil-`settings`, `LaunchSpec.Environment.*`) und ist `ConfigurationCatalog` bekannt | der angeforderte Wert; `%` wird bei Schlüsseln angehängt, die auf `Percent` enden, ` m` bei Schlüsseln, die auf `DistanceMeters` enden. Die Beschriftung ist der Einstellungsschlüssel, bei dem jeder durch Punkte getrennte Teil großgeschrieben beginnt (`graphics.maxFPS` → `Graphics MaxFPS`) | `Spec.Environment.*`; dieselben Werte sind die `PlannedMutations` des Plans |
| `Presentation` | `Splash` | immer | `Managed` oder `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | immer | `Original OMSI` (`Native`), `Disabled`, `Override` | `Spec.EffectiveInternetTextures.Mode` |

Der Abschnitt `Environment` erscheint in einer laufenden Beta-4-Sitzung, wenn ein Datum, eine Uhrzeit, ein Jahr oder ein Wetter angefordert wurde (diese Anforderungen sind experimentell, siehe [bekannte Einschränkungen](known-limitations.md)). Der Abschnitt `Vehicle` kann in einer laufenden Sitzung nie erscheinen: Die Anforderung eines beliebigen Felds des Spielerfahrzeugs macht den Plan nicht ausführbar, sodass keine solche Sitzung startet. Er existiert für einen zukünftigen Build und ist durch den Offline-Presenter-Test abgedeckt.

Runtime-Nachweis (pt-BR-UI, Runtime-Closure `T01`): Eine NEW_MAP-Sitzung auf Grundorf zeigte `Sessão em execução`; `Sessão`: `Modo: Nova sessão`, `Mapa: Grundorf`, `Ponto de entrada: 1`; `Apresentação`: `Splash: Gerenciado`, `Texturas da internet: OMSI original`.

Dieselben Daten stehen Werkzeugen auch ohne den Tray zur Verfügung: `session status` über die [lokale Steuerungsebene](local-control.md) liefert `SessionId`, `State`, `Diagnostics` und `RuntimeEvents` (live), und die `/plan --json`-Ausgabe des Eigentümers (oder `PlanSessionAsync`) liefert die geplante Spec, die aufgelösten Inhalte und die geplanten Änderungen, die das Fenster zusammenfasst.

<a id="end-session-with-confirmation"></a>
### Sitzung beenden (mit Bestätigung)

1. `StopConfirmationWindow`: Titel `End session?` (`Sitzung beenden?`), Meldung `OMSI 2 will be closed and the OmsiLaunch managed session will end.` (`OMSI 2 wird geschlossen und die von OmsiLaunch verwaltete Sitzung wird beendet.`), Schaltflächen `End session` (`Sitzung beenden`) (Standard, `DialogResult.OK`) und `Cancel` (`Abbrechen`) (`Escape`). Eine zweite Anforderung, während der Dialog geöffnet ist, aktiviert ihn, statt einen weiteren darüber zu legen.
2. Bei `OK` ruft der Tray `requestCanonicalStop` auf, wodurch das Signal `controlStopped` des Eigentümers ausgelöst wird; der Eigentümer ruft dann `StopAsync` auf: OMSI wird mit `TerminateProcess` beendet, und jede der Sitzung gehörende Datei wird wiederhergestellt. Der Tray beendet OMSI nie selbst.
3. Wenn die Anforderung eine Exception auslöst, wird der Fehler protokolliert und `Stop.Failed` (`The session could not be ended. OMSI and its managed session remain active.`; deutsch `Die Sitzung konnte nicht beendet werden. OMSI und die verwaltete Sitzung bleiben aktiv.`) angezeigt.
4. Der Tray bestätigt den Erfolg nicht; das Symbol verschwindet, wenn der Eigentümer die Wiederherstellung abgeschlossen und die Anzeige verworfen hat (Runtime-Closure `T01`: Der Eigentümer endete 607 ms nach der Bestätigung von `End session`).
5. `Cancel` (oder das Schließen des Dialogs) bewirkt nichts: Die Sitzung läuft weiter (`T01`).
6. Ein Stopp, der von anderer Stelle eintrifft (`session stop`, Ctrl+C, `/observe-seconds`, Beenden von OMSI), während das Statusfenster oder der Bestätigungsdialog geöffnet ist, schließt diese im Rahmen des Verwerfens der Anzeige; der Eigentümer wartet nicht auf den Benutzer (`T02`: Der Eigentümer endete 725 ms nach dem Stopp über die Pipe, während beide Fenster geöffnet waren).

<a id="explorer-restart"></a>
## Neustart des Explorers

`TrayWindow` ist ein verborgenes natives Fenster, das die Fensternachricht `TaskbarCreated` registriert. Wenn der Explorer (die Shell) neu startet, sendet er diese Nachricht an alle Fenster, und die Anzeige fügt das Symbol erneut hinzu (`Visible = false; Visible = true`). Runtime-Closure `T01`: Nachdem `explorer.exe` beendet und von Windows neu gestartet worden war, war das Symbol wieder in `Shell_TrayWnd` vorhanden, und Menü sowie Statusfenster funktionierten weiterhin.

<a id="localization"></a>
## Lokalisierung

`WindowsUiStrings.Resolve` folgt der **Windows-UI-Kultur** (`CultureInfo.CurrentUICulture`), nie der Inhaltssprache von OMSI oder einer Sprache aus einem Sitzungsprofil. Auflösungsreihenfolge: exakter Kulturname, dann zweibuchstabiger Sprachcode, dann Englisch. Jeder Schlüssel fällt auf Englisch zurück, wenn eine Übersetzung ihn nicht enthält.

| Kulturschlüssel | Sprache |
|---|---|
| `en`, `en-US`, `en-GB` | Englisch (Standard und Rückfallsprache) |
| `pt-BR` | Brasilianisches Portugiesisch. `pt-PT` (und reines `pt`) fällt bewusst auf Englisch zurück. |
| `de`, `de-DE` | Deutsch |
| `fr`, `fr-FR` | Französisch |
| `pl`, `pl-PL` | Polnisch |

Die lokalisierten Zeichenfolgen umfassen den Tooltip, die beiden Menüeinträge, das Statusfenster (Überschrift, Abschnittstitel, Feldbeschriftungen, `Close`), die Modus- und Darstellungswerte sowie den Bestätigungsdialog. Das Glossar wird in `docs\windows-ui-localization.md` gepflegt; der Offline-Test `windows-ui.localization-and-status` (`tests\OmsiLaunch.WindowsUiTests`) verifiziert die Auflösung und den Presenter.

<a id="omsilaunchwexe-failure-dialogs"></a>
## Fehlerdialoge von `OmsiLaunchW.exe`

Wenn `OMSILAUNCH_WINDOWS_HOST=1` gesetzt ist (durch `OmsiLaunchW.exe`), ersetzt `WindowsHost.ShowFailure` die Fehlerausgabe auf der Konsole durch ein modales Meldungsfenster mit dem Titel `OmsiLaunch` (Fehlersymbol): `<message>`, Leerzeile, `Code: OL_E_...`, Leerzeile, `See .omsilaunch\diagnostics for details.` Es wird bei jedem `CliInput.WriteError` angezeigt (Argumentfehler, `OL_E_NO_ACTIVE_SESSION`, `OL_E_SESSION_ALREADY_ACTIVE`, klassifizierte Exceptions), wenn ein Startplan nicht ausführbar ist (Ersatztext `The session plan is not runnable.`; Dokumentationsaudit BUG-06) und wenn die Sitzung `Running` nicht erreicht (`The OMSI session did not reach gameplay.` mit der letzten `OL_E_`-Diagnose, oder `OL_E_SESSION_START_FAILED`, wenn keine vorliegt). Das vollständige Verhalten von `OmsiLaunchW.exe` ist in der [OmsiLaunchW.exe-Referenz](omsilaunchw.md) beschrieben. Unter `OmsiLaunch.exe` bewirkt dieselbe Funktion nichts. Ein Startfehler des .NET-Hosts (Shim-Codes `100`..`106`) wird vom nativen Shim selbst angezeigt; siehe [Exitcodes](exit-codes.md).

<a id="log-location"></a>
## Speicherort des Logs

`<root>\.omsilaunch\diagnostics\tray-host.log`, eine Zeile pro Eintrag: ISO-8601-UTC-Zeitstempel, ein Tabulator, dann der Eintrag. Einträge: `created`, `removed`, `startup-timeout`, `startup-cancelled` (ein Verwerfen kam dem Start zuvor, und die Schleife wurde übersprungen), `dispose-timeout` sowie vollständige Exception-Texte bei UI-Fehlern. Die Protokollierung erfolgt nach bestem Bemühen und löst nie eine Exception aus. Host-Logs der Sitzung (`<sessionId>-host.log`) werden vom Eigentümer in dasselbe Verzeichnis geschrieben; das Tray-Log hat kein Sitzungspräfix und wird von der Aufbewahrungsregel für 50 Sitzungen nicht bereinigt.

<a id="lifecycle-guarantees"></a>
## Lebenszyklusgarantien

- Der Tray besitzt die Sitzung nie: Er kann OMSI nicht starten, keine Dateien wiederherstellen und den Stopppfad des Eigentümers nicht umgehen.
- Jeder Beendigungspfad des Eigentümers (normaler Abschluss, Beenden von OMSI, Ctrl+C, Schließen der Konsole, Stopp über die Pipe, Exception, `/observe-seconds`) verwirft die Anzeige vor `CloseAsync`, sodass kein Symbol seine Sitzung überdauert – außer wenn der Eigentümerprozess hart beendet wird (Windows entfernt verwaiste Symbole beim nächsten Überfahren mit der Maus).
- Erstellung und Verwerfen werden unter einer Sperre serialisiert: Ein Verwerfen, das den Wettlauf gewinnt, bewirkt, dass der UI-Thread seine Nachrichtenschleife überspringt und sofort aufräumt.
- Alle Windows-Forms-Arbeiten finden auf dem dedizierten STA-Thread statt; fremde Threads übergeben Aufgaben nur über ein verborgenes Marshalling-Steuerelement an ihn.

<a id="residual-caveats-from-the-code-comments"></a>
## Verbleibende Vorbehalte (aus den Code-Kommentaren)

- Es gibt keinen Tooltip-Text für „wird beendet“; das Symbol zeigt `OmsiLaunch is running` an, bis es entfernt wird.
- `NotifyIcon` kann bei von der Shell gehosteten Ereignissen die Mauskoordinaten `(0,0)` melden; stattdessen wird die Cursorposition gelesen.
- Tray-Nachrichten treffen weiterhin ein, während der Bestätigungsdialog modal ist; eine zweite Bestätigung wird nicht darüber gelegt.
- Wenn der UI-Thread nicht innerhalb des Verwerfungsbudgets von 2 s fertig wird, fährt der Eigentümer fort, ohne zu warten (`dispose-timeout`).
- Das Statusfenster ist ein beim Öffnen erstellter Snapshot der geplanten Werte; es wird nicht aktualisiert und liest nie aus OMSI.

<a id="runtime-evidence"></a>
## Runtime-Nachweis

Beobachtet in echten Sitzungen auf der autorisierten Installation in der Runde der Runtime-Closure (`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`, pt-BR-Windows-UI; siehe den [Status der Runtime-Validierung](../status/runtime-validation-status.md)):

- Das Symbol wird im echten Infobereich (`Shell_TrayWnd`) unter `OmsiLaunchW.exe` registriert und nach der Wiederherstellung entfernt (`T01`..`T04`).
- „End session“ mit Bestätigung löst den kanonischen Stopp und die exakte Wiederherstellung aus; `Cancel` lässt die Sitzung weiterlaufen (`T01`, `T03`).
- Das Symbol wird nach einem Neustart des Explorers neu erstellt (`TaskbarCreated`, `T01`).
- Das Statusfenster und der Bestätigungsdialog werden vom Eigentümer geschlossen, wenn ein Stopp eintrifft, während sie geöffnet sind (`T02`).
- Fehlerdialoge von `OmsiLaunchW.exe` für einen Argumentfehler (`OL_E_INVALID_ARGUMENT`), keine aktive Sitzung (`OL_E_NO_ACTIVE_SESSION`) und eine Sitzung, die vor dem Spielgeschehen fehlschlägt (`OL_E_WORLD_START_FAILED`) (`T04`). Beim letzten Fall zeigt der Dialog die Fehlerdaten des Plugins als Meldung an.
- `/silent` löst sich ab: Der Launcher kehrt zurück, während der Windows-Host die Sitzung behält (`T04`).
- Das `ProcessExit`-Budget von 4 s beim Schließen der Konsole (`L04`, Konsolen-Eigentümer).

Nicht erzeugt: die Exitcode-Dialoge des Bootstrapper-Shims (`100`..`106`).
