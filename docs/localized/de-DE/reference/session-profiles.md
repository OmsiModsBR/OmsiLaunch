# Sitzungsprofile

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> Übersetzung der [englischen Originalseite](../../../reference/session-profiles.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Ein Sitzungsprofil ist ein deklaratives YAML-Paket, das ein Inhaltsautor mit einer Karte oder einem Add-on ausliefert, damit Endanwender mit einem einzigen Befehl eine reproduzierbare OmsiLaunch-Sitzung starten können (`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`). Diese Seite ist die normative Referenz für das Format `omsilaunch.session-profile/v1`, wie es von `SessionProfileCompiler` in `src/OmsiLaunch.Core/SessionProfiles.cs` implementiert wird, für die von der CLI angewendeten Rangfolgeregeln (`CliInput.BuildSpecAsync` und `RejectProfileConflicts` in `tools/OmsiLaunch.Cli/Program.cs`) sowie für den Einstellungskatalog, den ein Profil schreiben darf (`ConfigurationCatalog`). Alles, was ein Profil kann, können auch die CLI-Flags und die [LaunchSpec](launchspec.md); ein Profil bündelt diese Entscheidungen lediglich.

Stabilität: `STABLE_BETA` für Parsing, Validierung, Konflikterkennung und die Blöcke `settings` / `presentation` / `internet-textures` / `behavior` (Offline-Test `session-profiles.strict-compiler`; der Overlay- und Wiederherstellungspfad ist durch RV-005 und RV-006 zur Laufzeit validiert, siehe [Status der Runtime-Validierung](../status/runtime-validation-status.md)). Die Schlüssel `new.date`, `new.time`, `new.year` und `new.weather` sind in diesem Build `UNAVAILABLE` (siehe [Der `new`-Block](#new)).

<a id="package-location-and-naming"></a>
## Ablageort und Benennung des Pakets

| Element | Regel |
| --- | --- |
| Paketverzeichnis | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| Profildatei | `<package>\profile.yaml` (exakter Name, eine Datei) |
| Assets | Beliebige Dateien oder Verzeichnisse innerhalb des Paketverzeichnisses, auf die über relative Pfade aus `presentation.splash.assets` und `internet-textures.profile` verwiesen wird |
| `id` | Muss ein einfacher Verzeichnisname sein: Er darf nicht leer sein oder nur aus Leerraum bestehen, darf weder `\`, `/` noch `:` enthalten und darf die Zeichenfolge `..` nicht enthalten. Verstöße ergeben `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Der in `profile.yaml` deklarierte `id`-Wert muss Byte für Byte mit dem Verzeichnisnamen übereinstimmen (unter Beachtung der Groß-/Kleinschreibung); andernfalls `OL_E_SESSION_PROFILE_INVALID`. |
| Auswahl | `/predefined-profile:<id>` zusammen mit `/predefined-profile-index:<n>`. Der Index ist obligatorisch: `/predefined-profile` ohne `/predefined-profile-index` scheitert mit `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| Fehlendes Paket | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| Release-Layout | Das Release-Paket liefert ein Beispiel unter `.omsilaunch\examples\session-profiles\rmg-leste\` aus (siehe [Paketierung](packaging.md)). Beispiele sind keine Profile: Kopieren Sie ein Paket nach `.omsilaunch\session-profiles\<id>\`, um es auswählbar zu machen. |

Ein Profil wird vom Anwender oder vom Inhaltsautor installiert und entfernt. OmsiLaunch schreibt nie in ein Paket, kopiert es nie und löscht es nie. Das Paketverzeichnis ist nicht Teil einer Transaktion.

<a id="parsing-rules"></a>
## Parsing-Regeln

| Regel | Verhalten | Fehler |
| --- | --- | --- |
| Größenlimit | `profile.yaml` darf 256 KiB (262,144 Bytes) nicht überschreiten | `OL_E_SESSION_PROFILE_INVALID` |
| Dokumentform | Genau ein YAML-Dokument, dessen Wurzelknoten ein Mapping ist | `OL_E_SESSION_PROFILE_INVALID` |
| Schema | `schema` muss exakt `omsilaunch.session-profile/v1` lauten (unter Beachtung der Groß-/Kleinschreibung) | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| Anker und Aliase | Jeder Knoten, der irgendwo im Dokument einen YAML-Anker (`&name`) trägt, wird vor der Validierung abgelehnt; Aliase (`*name`) können daher nicht vorkommen | `OL_E_SESSION_PROFILE_INVALID` („YAML anchors are not supported.“) |
| Unbekannte Schlüssel | Jedes Mapping ist geschlossen: Ein Schlüssel, der in den folgenden Tabellen für seinen Kontext nicht aufgeführt ist, wird abgelehnt („Unknown property in `<context>`: `<key>`“). Schlüssel werden unter Beachtung der Groß-/Kleinschreibung verglichen (`Schema:` ist ein unbekannter Schlüssel). Das einzige offene Mapping ist `settings`, dessen Schlüssel stattdessen gegen den Einstellungskatalog validiert werden. | `OL_E_SESSION_PROFILE_INVALID` |
| Skalare | Jeder Blattwert muss ein Skalar sein; Sequenzen und Mappings an Stellen, an denen ein Skalar erwartet wird, werden abgelehnt („`<field>` must be a scalar.“) | `OL_E_SESSION_PROFILE_INVALID` |
| Zahlen | Ganzzahlen werden mit der invarianten Kultur geparst (`1`, `30`); Dezimalzahlen in `settings` verwenden `.` als Trennzeichen | `OL_E_SESSION_PROFILE_INVALID` |
| Datum und Uhrzeit | `new.date.value` wird von `DateOnly.Parse` und `new.time.value` von `TimeOnly.Parse` geparst, beide mit invarianter Kultur; verwenden Sie die ISO-Formen `yyyy-MM-dd` und `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| YAML-Syntaxfehler | Werden mit der Meldung des Parsers gemeldet | `OL_E_SESSION_PROFILE_INVALID` („Invalid YAML: ...“) |
| Ausführbarer Inhalt | YAML wird mit `YamlDotNet` nur in einen Repräsentationsbaum geparst; Tags, benutzerdefinierte Typen oder Codeausführung werden nicht unterstützt |

Backslashes in einfachen (nicht in Anführungszeichen gesetzten) Skalaren sind literale Zeichen. Schreiben Sie Windows-Pfade mit einem einzelnen Backslash (`maps\Grundorf\global.cfg`). Ein doppelter Backslash in einem einfachen Skalar bleibt im Wert doppelt; siehe [Das mitgelieferte Beispiel](#the-packaged-example).

<a id="key-reference"></a>
## Schlüsselreferenz

Kontexte werden genau so benannt, wie der Compiler sie benennt. Jeder hier aufgeführte Schlüssel wird akzeptiert, sonst keiner.

<a id="profile-root-mapping"></a>
### `profile` (Wurzel-Mapping)

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `schema` | string | ja | Literal `omsilaunch.session-profile/v1`. |
| `id` | string | ja | Paketkennung; muss dem Verzeichnisnamen entsprechen. |
| `name` | string | ja | Anzeigename; gemeldet in `SessionProfileMetadata.Name`. |
| `author` | string | ja | Autor; gemeldet in `SessionProfileMetadata.Author`. |
| `version` | string | ja | Versionszeichenfolge des Pakets (frei formuliert, in Anführungszeichen setzen: `"1.0"`); gemeldet in `SessionProfileMetadata.Version`. |
| `compatibility` | Mapping | nein | Siehe `compatibility`. |
| `new` | Mapping | nein | NEW_MAP-Standardwerte. Siehe `new`. |
| `presets` | Sequenz von Mappings | ja | 1 bis 99 Voreinstellungseinträge. Null, mehr als 99 oder ein Wert, der keine Sequenz ist, ergibt `OL_E_SESSION_PROFILE_INVALID`. |

### `compatibility`

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `maps` | Sequenz von Strings | nein | Kartenidentitäten (`maps\<Map>\global.cfg`), für die dieses Profil gültig ist. `/` wird zu `\` normalisiert; der Vergleich erfolgt ohne Beachtung der Groß-/Kleinschreibung. Eine fehlende oder leere Liste bedeutet „jede Karte“. Ist sie nicht leer, wird sie für `WorldMode.NewMap` (gegen die wirksame `new.map` oder `/map`) und für `WorldMode.SavedSituation` (gegen die Karte, auf die die ausgewählte `.osn` verweist, aufgelöst über den Inhaltskatalog) erzwungen. Für `WorldMode.LastMapState` lässt sich keine Karte ableiten, daher scheitert eine nicht leere Liste immer. Fehler: `OL_E_SESSION_PROFILE_MAP_MISMATCH`. |

### `new`

Der Block wird gelesen und validiert, sobald er vorhanden ist, aber nur dann auf die Spec angewendet, wenn der ausgewählte Weltmodus NEW_MAP ist (`/new`, der CLI-Standard). Unter `/saved:<file.osn>` wird der Block ignoriert.

| Schlüssel | Typ | Erforderlich | Angewendet | Beschreibung |
| --- | --- | --- | --- | --- |
| `map` | string | nein | ja | Kartenidentität in der normalisierten Form `maps\<Map>\global.cfg` (die Planung verlangt genau diese Form: beginnt mit `maps\`, endet mit `\global.cfg`, kein `..`). Setzt `WorldSpec.MapIdentity`. |
| `entrypoint-index` | Ganzzahl | nein | ja | Index des angezeigten Einstiegspunkts (0-basierte Position in der Einstiegspunktliste von OMSI). Setzt `PresentedEntrypointIndex` und löscht eine etwaige Einstiegspunktidentität. |
| `entrypoint` | string | nein | ja | Rohe Einstiegspunktidentität. Setzt `EntrypointIdentity` und löscht den angezeigten Index. Sind sowohl `entrypoint-index` als auch `entrypoint` vorhanden, gewinnt `entrypoint`, weil es zuletzt angewendet wird. Der Wert ist eine exakte (Groß-/Kleinschreibung wird beachtet) Bezeichnung oder eine Discovery-Identität; OMSI zeigt nur den ersten Datensatz jeder Bezeichnung an (`world.entrypoint-identity`, `EXPERIMENTAL`). |
| `date` | Mapping | nein | nein (`UNAVAILABLE`) | Siehe `new.date`. |
| `time` | Mapping | nein | nein (`UNAVAILABLE`) | Siehe `new.time`. |
| `year` | Ganzzahl | nein | nein (`UNAVAILABLE`) | Explizites Jahr. |
| `weather` | Mapping | nein | nein (`UNAVAILABLE`) | Siehe `new.weather`. |

`date`, `time`, `year` und `weather` werden in `DateSpec`, `TimeSpec`, `YearSpec` und `WeatherSpec` mit `DateTimeMode.Explicit` / dem ausgewählten `WeatherMode` kompiliert. Der Sitzungsplaner (`src/OmsiLaunch.Core/SessionPlanner.cs`) verlangt `world.explicit-date`, `world.explicit-time`, `world.explicit-year` und `world.weather`; alle vier sind `EXPERIMENTAL` (statisch abgeschlossen, Runtime-Validierung steht aus). Startdatum und -zeit gelten nur für NEW_MAP; das Wetter gilt für jeden Weltmodus.

#### `new.date`

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `mode` | string | ja | Muss `explicit` sein (ohne Beachtung der Groß-/Kleinschreibung). Jeder andere Wert ergibt `OL_E_SESSION_PROFILE_INVALID` („date must use explicit mode.“). |
| `value` | string | ja | `yyyy-MM-dd`. |

#### `new.time`

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `mode` | string | ja | Muss `explicit` sein. |
| `value` | string | ja | `HH:mm` oder `HH:mm:ss`. |

#### `new.weather`

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `mode` | string | ja | `preset`, `icao` oder `real` (ohne Beachtung der Groß-/Kleinschreibung). Alles andere: `OL_E_SESSION_PROFILE_INVALID` („Unsupported weather mode“). |
| `preset` | string | bei `mode: preset` | Name der Wettervoreinstellung. |
| `icao` | string | bei `mode: icao` | ICAO-Stationscode. |

<a id="preset-each-entry-of-presets"></a>
### `preset` (jeder Eintrag von `presets`)

| Schlüssel | Typ | Erforderlich | Standardwert | Beschreibung |
| --- | --- | --- | --- | --- |
| `index` | Ganzzahl | ja | | 1 bis 99, eindeutig innerhalb des Profils; die Indizes müssen nicht lückenlos sein. Wird mit `/predefined-profile-index` ausgewählt. Doppelt oder außerhalb des Bereichs: `OL_E_SESSION_PROFILE_INVALID`; ein Index, der im Profil nirgends existiert: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`. |
| `id` | string | ja | | Kennung der Voreinstellung; gemeldet als `SessionProfileMetadata.PresetId`. |
| `name` | string | ja | | Anzeigename der Voreinstellung; gemeldet als `SessionProfileMetadata.PresetName`. |
| `settings` | Mapping | nein | keiner | Semantische `options.cfg`-Einstellungen, siehe [Einstellungen](#settings). Schlüssel werden ohne Beachtung der Groß-/Kleinschreibung mit dem Katalog verglichen. |
| `presentation` | Mapping | nein | geerbt | Startbilddarstellung, siehe `presentation`. Fehlt der Block, erbt die Voreinstellung die Ausgangsbasis (`/spec`-Wert oder den CLI-Standard, `Managed`). |
| `internet-textures` | Mapping | nein | geerbt | Siehe `internet-textures`. |
| `behavior` | Mapping | nein | geerbt | Timeouts, siehe `behavior`. |
| `hofs` | Sequenz von Mappings | nein | geerbt | HOF-Operationen der Sitzung, siehe `hofs`. Wenn vorhanden, ersetzen sie das `HofManagement` der Ausgangsbasis. |
| `modules` | Mapping | nein | geerbt | Runtime-Module, siehe `modules`. Werden mit dem zusammengeführt, was eine `/spec`-Datei anfordert. |

Nur die ausgewählte Voreinstellung wird angewendet. Dennoch wird jede Voreinstellung geparst und validiert, sodass ein Fehler in Voreinstellung 3 eine Anforderung für Voreinstellung 1 scheitern lässt.

### `presentation`

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `splash` | Mapping | ja | Erforderlich, wenn `presentation` vorhanden ist („Presentation requires splash.“). Siehe `presentation.splash`. |

#### `presentation.splash`

| Schlüssel | Typ | Erforderlich | Standardwert | Beschreibung |
| --- | --- | --- | --- | --- |
| `mode` | string | ja | | `managed` installiert für die Sitzung die Startbild-Bitmaps von OmsiLaunch (`SplashMode.Managed`). `unset` oder `native` belässt die eigenen Startbilddateien von OMSI (`SplashMode.Unset`; `Native` ist ein Alias). Ohne Beachtung der Groß-/Kleinschreibung. Alles andere: `OL_E_SESSION_PROFILE_INVALID`. |
| `language` | string | nein | `ENG` | Sprache des zweiten Startbildziels: `PTB`, `ENG`, `DEU`, `FRA` (Aliase `PT-BR`, `EN`, `DE`, `FR`; alles Unbekannte wird beim Aufbau der Sitzung zu `ENG` aufgelöst). Mit `mode: managed` legt die Sitzung `GUI\NewSplashscreen_ENG.bmp` und `GUI\NewSplashscreen_<language>.bmp` als Overlay an. |
| `assets` | string | nein | mitgelieferte Assets | Verzeichnis **relativ zum Paket**, das `ENG.bmp` und, bei einer nicht englischen `language`, `<language>.bmp` enthält; jede Datei muss ein 640x480-BMP mit 24 Bit sein. Das Verzeichnis muss beim Laden des Profils existieren (`OL_E_SESSION_PROFILE_ASSET_MISSING`); die Dateien werden beim Sitzungsstart validiert (`OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`). Die Regeln zur Pfadbeschränkung gelten. Wird der Schlüssel weggelassen, wird `.omsilaunch\assets\splash` der Installation (oder die mitgelieferten Standarddateien) verwendet. |

Ein Profil kann `SessionPresentationSpec.SuppressTrayIcon` nicht setzen; der Wert bleibt `false`, sofern ihn nicht eine `/spec` setzt.

### `internet-textures`

| Schlüssel | Typ | Erforderlich | Beschreibung |
| --- | --- | --- | --- |
| `mode` | string | ja | `native` (`InternetTexturesMode.Native`, OMSI verhält sich normal), `disabled` (`Disabled`, der profilierte prozessinterne Downloader wird für die Sitzung unterdrückt), `override` (`Override`, ein sitzungsbezogenes `.itx`-Profil wird als `Texture\standard.itx` installiert). Ohne Beachtung der Groß-/Kleinschreibung; alles andere: `OL_E_SESSION_PROFILE_INVALID`. |
| `profile` | string | erforderlich für `override` | Pfad **relativ zum Paket** zur `.itx`-Datei. Fehlender Schlüssel bei `override`: `OL_E_SESSION_PROFILE_INVALID`; fehlende Datei: `OL_E_SESSION_PROFILE_ASSET_MISSING`. Die Regeln zur Pfadbeschränkung gelten. Die Datei muss aus `URL`- / `target`-Zeilenpaaren mit `http://`- oder `https://`-URLs bestehen (andernfalls `OL_E_ITX_PROFILE_INVALID`), und jedes Ziel muss unterhalb des `Texture\`-Verzeichnisses der Installation aufgelöst werden, ohne einen Analysepunkt (Reparse Point) zu durchlaufen (`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`). Die aufgeführten Ziele und `Texture\standard.ipr` werden zu Sitzungslöschungen (siehe [Transaktionen und Recovery](../concepts/transactions-and-recovery.md)). |

### `behavior`

| Schlüssel | Typ | Erforderlich | Standardwert | Beschreibung |
| --- | --- | --- | --- | --- |
| `startup-timeout` | Ganzzahl (Sekunden) | nein | 180 | Zulässige Zeit vom Prozessstart bis `Running`. Muss beim Laden des Profils positiv sein; die Sitzung verlangt beim Start zusätzlich 1 bis 600 (andernfalls `OL_E_START_SESSION`). Entspricht `LaunchBehaviorSpec.StartupTimeoutSeconds`. |
| `shutdown-timeout` | Ganzzahl (Sekunden) | nein | 30 | Entspricht `LaunchBehaviorSpec.ShutdownTimeoutSeconds`. ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: Der Supervisor beendet OMSI direkt und liest diesen Wert nie. |

Ist der `behavior`-Block vorhanden, werden beide Timeouts gesetzt (angegebener Wert oder Standardwert) und ersetzen die `LaunchBehaviorSpec` der Ausgangsbasis vollständig, einschließlich `RestoreConfiguration` und `SuppressStaleClosecheckWarning`, die auf ihre Standardwerte (`true`, `true`) zurückfallen.

### `hofs`

Jeder Eintrag ist eine Operation der [HOF-Verwaltung](hof-management.md) und nennt genau einen Quellschlüssel (`package`, `collection`, `directory`, `vehicle` oder `file`); null oder mehrere: `OL_E_SESSION_PROFILE_INVALID`. Die Existenz von Quellen und Zielen wird bei der Planung der Sitzung geprüft (`OL_E_HOF_*`), außer bei `package`.

| Schlüssel | Typ | Erforderlich | Standard | Beschreibung |
| --- | --- | --- | --- | --- |
| `package` | string | ein Quellschlüssel | | Verzeichnis **relativ zum Paket**, das die `.hof`-Dateien enthält, die das Profil mitliefert. Muss beim Laden des Profils existieren (`OL_E_SESSION_PROFILE_ASSET_MISSING`); es gelten die Regeln zur Pfadbeschränkung. Wird zu einer `Directory`-Quelle. |
| `collection` | string | ein Quellschlüssel | | Name einer Collection unter `.omsilaunch\hof\`. |
| `directory` | string | ein Quellschlüssel | | Installationsrelatives Verzeichnis, zum Beispiel neben Karteninhalten. |
| `vehicle` | string | ein Quellschlüssel | | Fahrzeugidentität oder Ordner, deren HOFs kopiert werden. |
| `file` | string | ein Quellschlüssel | | Installationsrelative `.hof`-Datei. |
| `targets` | Sequenz von Strings | ja | | Zielfahrzeuge (Identitäten oder Ordner unterhalb von `Vehicles\`). Fehlend oder leer: `OL_E_SESSION_PROFILE_INVALID`. |
| `files` | Sequenz von Strings | nein | alle | Dateinamen, die aus der Quelle übernommen werden. |
| `mode` | string | nein | `add` | `add` oder `mirror` (Groß-/Kleinschreibung wird nicht beachtet); alles andere: `OL_E_SESSION_PROFILE_INVALID`. |

Eine Voreinstellung mit `hofs` in Kombination mit einem `/hof-source`-Flag: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`.

### `modules`

Ein Eintrag pro [Runtime-Modul](runtime-modules.md)-ID (`EXPERIMENTAL`). Jeder Eintrag ist ein Mapping mit diesen Schlüsseln (Kontext `module`):

| Schlüssel | Typ | Erforderlich | Standard | Beschreibung |
| --- | --- | --- | --- | --- |
| `enabled` | `true` oder `false` | nein | nicht gesetzt | `true` fordert das Modul an (`LaunchSpec.Modules.Enable`); `false` hält es aus der Sitzung heraus, auch ein standardmäßig, durch seine Karte oder durch die Einstellungsdatei geladenes Modul (`LaunchSpec.Modules.Disable`). |
| `settings` | Mapping | nein | keine | Moduleinstellungen nach Schlüssel (`LaunchSpec.Modules.Settings` als `<id>.<key>`). Die Werte werden bei der Planung der Sitzung gegen die vom Modul deklarierten Einstellungen geprüft (`OL_E_MODULE_SETTING_INVALID`). |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

Eine Modul-ID, die nicht `[a-z0-9][a-z0-9-]{1,47}` entspricht, ein anderer `enabled`-Wert als `true` oder `false`, oder ein Einstellungsschlüssel, der nicht `[a-z][a-z0-9_]{0,31}` entspricht: `OL_E_SESSION_PROFILE_INVALID`. Ein Modul, das nicht installiert ist, wird bei der Planung der Sitzung gemeldet (`OL_E_MODULE_NOT_FOUND` bei `enabled: true`, `OL_W_MODULE_SETTING_IGNORED` bei reinen Einstellungen).

<a id="settings"></a>
## Einstellungen

`settings`-Schlüssel sind die semantischen Namen aus `ConfigurationCatalog` (`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`). Der Compiler akzeptiert einen Schlüssel nur, wenn er existiert (`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`) und beschreibbar ist (`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`). Werte werden als Strings gespeichert und in einen `options.cfg`-Patch umgewandelt, wenn die Sitzung ihre Overlays aufbaut; ein ungültiger Wert wird daher erst bei `StartSessionAsync` erkannt, nicht beim Laden des Profils, und lässt die Sitzung mit `OL_E_START_SESSION` scheitern, dessen Meldung `OL_E_INVALID_SETTING_VALUE: <key>` enthält. Jede der folgenden Einstellungen schreibt in `options.cfg`; alle sind sitzungsbezogen und werden nach der Sitzung exakt wiederhergestellt.

Wertformen:

- **bool** ist `true` oder `false` (ohne Beachtung der Groß-/Kleinschreibung). Bei Präsenz-Tokens wird das Token hinzugefügt oder entfernt; bei invertierten Tokens (`no_*`) entfernt `true` das negative Token.
- **int** / **decimal** werden gegen den Bereich validiert; Werte mit einem Divisor werden geteilt gespeichert (z. B. schreibt `graphics.minObjectScreenPercent: 5` den Wert `0.05`).
- **string** wird unverändert geschrieben.

| Einstellungsschlüssel | `options.cfg`-Token | Typ | Bereich / Werte | Nachweis |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | beliebig | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | beliebig | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | beliebig | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10, gespeichert /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50, gespeichert /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool, geschrieben als `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` oder `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems` (4-zeiliger Block) | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool (invertierte Präsenz) | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool (beide Präsenz-Tokens) | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool (Präsenz) | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` Komponente 0 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 zur Laufzeit) |
| `traffic.humans` | `AIMaxCountRandom` Komponente 1 | int | 0..1000 | STATICALLY_VALIDATED (RV-005 zur Laufzeit) |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

Katalogeinträge, die existieren, aber **nicht beschreibbar** sind (abgelehnt mit `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`): `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad` (abgelöst durch `advanced.reducedMultithreading`), `graphics.texture`, `graphics.textureFilter`.

<a id="path-confinement"></a>
## Pfadbeschränkung

`presentation.splash.assets` und `internet-textures.profile` werden durch `Confined(package root, value)` aufgelöst:

1. Absolute Pfade (`C:\...`), Pfade, die mit `\` beginnen, und jede Pfadkomponente, die `..` entspricht, werden abgelehnt.
2. Der vollständige Pfad wird berechnet und muss mit dem Paketverzeichnis beginnen.
3. Jede existierende Komponente unterhalb des Paketstamms bis einschließlich des endgültigen Pfads wird auf das Attribut `ReparsePoint` geprüft. Eine Junction, ein symbolischer Verzeichnislink oder ein symbolischer Dateilink irgendwo auf diesem Pfad wird abgelehnt, ebenso eine Komponente, die sich nicht prüfen lässt (`IOException` / `UnauthorizedAccessException`).

Alle drei Fehler ergeben `OL_E_SESSION_PROFILE_PATH_ESCAPE`. Dieselbe Regel für Analysepunkte wird beim Aufbau der Sitzung auf `.itx`-Ziele unter `Texture\` angewendet.

<a id="precedence-and-override-conflicts"></a>
## Rangfolge und Überschreibungskonflikte

`CliInput.BuildSpecAsync` setzt die Spec in dieser Reihenfolge zusammen:

1. **Standardwerte** (NEW_MAP, alles nicht gesetzt, Timeouts 180 s / 30 s).
2. **`/spec:<file.json>`** ersetzt, falls angegeben, die Standardwerte vollständig.
3. **Installationsstammverzeichnis**: Ein explizites Installationsargument hat Vorrang vor dem `RootPath` der Spec; `.` bedeutet das Verzeichnis, das die ausführbare Datei enthält.
4. **Profil** (`/predefined-profile` + `/predefined-profile-index`): Das Paket wird geladen, und `RejectProfileConflicts` läuft gegen die rohen CLI-Argumente, **bevor** irgendetwas zusammengeführt wird. Anschließend wird der Weltblock der Ausgangsbasis auf eine leere `WorldSpec` des ausgewählten Modus zurückgesetzt (eine `/spec`-Welt wird verworfen, wenn ein Profil verwendet wird), und `SessionProfileCompiler.Apply` legt das Profil über die Ausgangsbasis: `new` (nur NEW_MAP), `settings` (über `Environment.General` der Ausgangsbasis zusammengeführt, das Profil gewinnt je Schlüssel) sowie `presentation`, `internet-textures`, `behavior` (jeweils ersetzen sie den Block der Ausgangsbasis nur, wenn die Voreinstellung ihn definiert).
5. **Übrige CLI-Argumente** werden darübergelegt: `/map`, `/entrypoint`, `/entrypoint-index`, `/date`, `/time`, `/year`, Wetter-Flags, Fahrzeug-Flags, `/set`, Startbild-Flags, Internettextur-Flags, `/startup-timeout`, `/shutdown-timeout`. Timeouts aus der CLI gelten nur, wenn sie angegeben sind; andernfalls bleibt der Wert aus Spec/Profil/Standard bestehen.
6. **Kompatibilitätsprüfung** für Modi außer NEW_MAP (`ValidateCompatibility`).

Ein CLI-Argument, das auf ein Feld zielt, das dem ausgewählten Profil gehört, ist ein Konflikt und wird mit `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` abgelehnt (Exitcode 2, Kategorie `invalid_argument`). Die Prüfung erfolgt pro Feld, nicht pro Wert: Auch die Wiederholung des profileigenen Werts ist ein Konflikt.

| CLI-Argument | Konflikt, wenn das Profil Folgendes definiert | Nur im Modus |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` oder `/entrypoint-index` | `new.entrypoint` oder `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | denselben `<key>` in den `settings` der Voreinstellung (ohne Beachtung der Groß-/Kleinschreibung) | jeder |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation` (beliebig) | jeder |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures` (beliebig) | jeder |
| `/startup-timeout`, `/shutdown-timeout` | `behavior` (beliebig) | jeder |
| `/module:<id>` | `modules.<id>.enabled: false` | jeder |
| `/no-module:<id>` | `modules.<id>.enabled: true` | jeder |
| `/module-setting:<id>.<key>=...` oder ein Modul-Flag | `modules.<id>.settings.<key>` | jeder |

Keine Konflikte sind: `/set`-Schlüssel, die die Voreinstellung nicht definiert (sie werden hinzugefügt), Fahrzeug-Flags (`/vehicle`, `/repaint`, `/hof`, `/fleet`, `/registration`, `/no-vehicle`; ein Profil kann kein Spielerfahrzeug definieren) und jedes Weltargument unter `/saved` (der `new`-Block wird dort nicht angewendet). `/map`, `/entrypoint` und `/entrypoint-index` sind zusammen mit `/saved` unabhängig von Profilen ungültig (`OL_E_INVALID_ARGUMENT`).

<a id="error-codes"></a>
## Fehlercodes

| Code | Ausgelöst, wenn | CLI-Exitcode |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` nicht existiert | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` kein einfacher Verzeichnisname ist; `assets` / `profile` das Paket verlässt oder einen Analysepunkt durchläuft | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` nicht `omsilaunch.session-profile/v1` ist | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | Größenlimit, Dokumentform, Anker, unbekannter Schlüssel, fehlender erforderlicher Schlüssel, nicht skalarer Wert, ungültige Zahl/ungültiges Datum/ungültige Uhrzeit, `id`-Abweichung, Regeln für Anzahl/Index der Voreinstellungen, nicht unterstützte Moduswörter, nicht positives Timeout, `presentation` ohne `splash`, `override` ohne `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` fehlt, außerhalb von 1..99 liegt oder es keine Voreinstellung mit diesem `index` gibt | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | ein `settings`-Schlüssel nicht im Katalog steht | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | ein `settings`-Schlüssel katalogisiert, aber schreibgeschützt ist | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | das `assets`-Verzeichnis oder die `profile`-Datei innerhalb des Pakets nicht existiert | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` nicht leer ist und die wirksame Karte nicht aufgeführt ist (oder sich nicht ableiten lässt) | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | ein explizites CLI-Argument auf ein profileigenes Feld zielt | 2 |

Alle diese Fehler werden ausgelöst, während die Befehlszeile kompiliert wird, also vor der Planung. Es handelt sich um `SessionProfileException` (bzw. `ArgumentException` für den Konflikt), und sie starten nie eine Sitzung. Der vollständige Katalog steht unter [Fehler](errors.md), die Exitcodes unter [Exitcodes](exit-codes.md).

<a id="discovering-installed-profiles"></a>
## Installierte Profile erkennen

`OmsiLaunch.exe /list:SessionProfiles` (API `ListSessionProfilesAsync`) kompiliert jedes Paket unter `.omsilaunch\session-profiles\` und gibt pro Paket einen Eintrag aus: `Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity` und seine `Presets` (`Index`, `Id`, `Name`) in Indexreihenfolge. Ein Paket, das nicht kompiliert, wird dennoch aufgeführt, mit `IsValid: false` und demselben `OL_E_SESSION_PROFILE_*`-Code, den ein Start melden würde. Es wird nichts geschrieben.

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## Wie ein Profil in der API erscheint

Nach einem erfolgreichen Laden trägt die Spec einen `SessionProfileMetadata`-Datensatz in `LaunchSpec.SessionProfile`:

| Feld | Quelle |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | `id` der ausgewählten Voreinstellung |
| `PresetIndex` | `index` der ausgewählten Voreinstellung |
| `PresetName` | `name` der ausgewählten Voreinstellung |
| `PackagePath` | absolutes Paketverzeichnis |

Der Planer fügt jedem `SessionPlan`, der aus einer solchen Spec erstellt wird, die informative Diagnose `session_profile.selected` hinzu, mit den Datenschlüsseln `session_profile.id`, `session_profile.name`, `session_profile.version`, `session_profile.author`, `session_profile.preset_id`, `session_profile.preset_index`, `session_profile.preset_name` und `session_profile.path`. Sie beeinflusst die Ausführbarkeit nicht. Integratoren, die die [öffentliche API](public-api.md) direkt verwenden, können `SessionProfileCompiler.Load` und `SessionProfileCompiler.Apply` aus `OmsiLaunch.Core` aufrufen; die YAML-Repräsentation gelangt nie in `OmsiLaunch.Api`.

<a id="examples"></a>
## Beispiele

<a id="example-1-settings-only-profile-one-preset"></a>
### Beispiel 1: Profil nur mit Einstellungen, eine Voreinstellung

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

Ausführen: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`. Karte und Einstiegspunkt stammen aus der Befehlszeile, weil das Profil keinen `new`-Block definiert; das Hinzufügen von `/set:graphics.maxFPS=60` ist erlaubt, das Hinzufügen von `/set:traffic.humans=10` ist ein Konflikt.

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### Beispiel 2: kartengebundenes Profil mit drei Voreinstellungen und mitgelieferten Assets

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`, mit `assets\splash\ENG.bmp`, `assets\splash\DEU.bmp` und `textures\offline.itx` innerhalb des Pakets:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

Ausführen: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`. Mit `/saved:situations\mytrip.osn` wird der `new`-Block übersprungen, und die `.osn` muss auf `maps\Grundorf\global.cfg` verweisen.

<a id="the-packaged-example"></a>
### Das mitgelieferte Beispiel

Das Release liefert `docs/examples/session-profiles/rmg-leste/profile.yaml` aus ([ansehen](../../../examples/session-profiles/rmg-leste/profile.yaml)). Es ist syntaktisch gültig, entspricht dem Schema und würde ohne Fehler geladen. Zwei Eigenschaften verhindern, dass es in diesem Build unverändert eine Sitzung startet:

1. Es setzt `new.date`, `new.time` und `new.weather`, wodurch der Plan nicht ausführbar wird (siehe [Der `new`-Block](#new)).
2. Seine Pfadwerte sind einfache Skalare mit doppelten Backslashes (`maps\\RMG Leste\\global.cfg`). YAML behält sie doppelt bei, und Kartenidentitäten werden textuell verglichen (nur nach der Normalisierung von `/` zu `\`), sodass `new.map` und `compatibility.maps` nicht mit der Katalogidentität `maps\RMG Leste\global.cfg` übereinstimmen würden (`OL_E_MAP_NOT_FOUND` bei der Planung). Der `assets`-Wert wird dennoch aufgelöst, weil die Windows-Pfadnormalisierung doppelte Trennzeichen zusammenfasst.

Die in diesem Build ausführbare Form lautet:

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## Verwandte Seiten

- [CLI-Referenz](cli.md) zu `/predefined-profile`, `/predefined-profile-index`, `/set` und den Welt-Flags
- [LaunchSpec](launchspec.md) zu dem Datensatz, in den ein Profil kompiliert wird
- [Transaktionen und Recovery](../concepts/transactions-and-recovery.md) dazu, wie `settings`-, Startbild- und `.itx`-Overlays angewendet und wiederhergestellt werden
- [Capabilities](capabilities.md) und [Status der Runtime-Validierung](../status/runtime-validation-status.md)
