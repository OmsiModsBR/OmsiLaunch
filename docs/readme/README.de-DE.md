<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Sitzungssteuerung für OMSI 2.</strong></p>
<p align="center">Open Source · Programmierbar · Von der Community getragen</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <strong>Deutsch</strong> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Dies ist eine Übersetzung der maßgeblichen [englischen README (US)](../../README.md). Bei Abweichungen ist die englische README (US) verbindlich.

# OmsiLaunch

**OmsiLaunch** ist eine Open-Source-Schicht, mit der sich OMSI 2 programmgesteuert
starten, Sitzungen verwalten und die laufende Simulation steuern lassen. Es plant
eine Sitzung anhand einer deklarativen Beschreibung, wendet jede temporäre
Konfigurationsänderung innerhalb einer protokollierten Transaktion (Journal) an,
startet OMSI, beobachtet es bis zum Spielbetrieb, ermöglicht Tools über eine
öffentliche API das Lesen und Ändern der laufenden Simulation und stellt beim
Ende der Sitzung jede Datei wieder her, die es verändert hat.

Es ist Infrastruktur für Launcher, Tools, Automatisierung und
Community-Integrationen. Es ist kein grafischer Launcher.

> **Definieren Sie die Sitzung, nicht die Klicks.**

## Status: 0.1.0-beta4

Die aktuelle öffentliche Beta ist **`0.1.0-beta4`**. Beta 4 ergänzt
Compatibility pro Capability, Runtime Modules, Sitzungsprofile 1..99 sowie eine
Reihe von Start- und Runtime-Funktionen. Compatibility, der Capability-Katalog,
Paginierung, typisierte Fehler und die Modules-Bridge wurden in echten
OMSI-Sitzungen beobachtet (Validierungsrunden A und B). Die nach diesen Runden
hinzugekommenen Funktionen sind `EXPERIMENTAL`: durch statische Analyse
abgeschlossen und nur offline getestet, die Runtime-Validierung steht noch aus.
Einige bleiben `STATICALLY_VALIDATED`, `PARTIAL` oder `UNAVAILABLE`. Zwei
Runtime-Punkte sind noch offen, weil sie sich nicht sicher herbeiführen lassen:
der Spielbetrieb mit Steam LAA und das natürliche Entfernen von
Straßenfahrzeugen und Menschen (RV-002). Die
Seite [Status der Runtime-Validierung](../localized/de-DE/status/runtime-validation-status.md) ist
der maßgebliche Nachweis darüber, was unter OMSI und was nur offline ausgeführt wurde.

Es handelt sich um eine Beta: Die öffentliche API, die CLI und die Dateiformate
sind pro Member als `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` oder
`UNAVAILABLE` gekennzeichnet und können sich vor 1.0 noch ändern.

## Unterstützter OMSI-Umfang

OmsiLaunch setzt ein installiertes OMSI 2 voraus. Es hängt nicht von einer
bestimmten `Omsi.exe`-Datei ab: Jede native Capability wird vor ihrer Verwendung
einzeln innerhalb von OMSI aufgelöst und verifiziert, und eine Capability, die
sich auf einem Build nicht verifizieren lässt, ist auf diesem Build nicht
verfügbar.

| Element | Umfang |
| --- | --- |
| Voraussetzung | OMSI 2 installiert: eine 32-Bit-`Omsi.exe` mit einer OMSI-2-Version (2.x) sowie die Verzeichnisse `maps\` und `Vehicles\`. Alles andere wird mit `OL_E_UNSUPPORTED_BUILD` abgelehnt. |
| Bekanntes Layout | OMSI 2.3.004 (`Omsi23004`). Jede Capability wird aufgelöst, und jede Runtime-Validierung lief mit diesem Layout. Hashes identifizieren es als schnellen Pfad; sie sind keine Voraussetzung. |
| Andere OMSI-2-Builds | Jede Capability wird über build-unabhängige Strategien (Delphi-RTTI, eindeutige Code-Signaturen, VMT-Slots) aufgelöst und verifiziert. Capabilities, die Objektlayouts lesen, benötigen ein bekanntes Layout. |
| Steam-Executable | Wird im Prozess aufgelöst; eine echte Steam-Sitzung wurde **nicht** zur Laufzeit validiert. |
| Betriebssystem | Windows 10 oder neuer, x64. |
| Runtimes | .NET 6 Desktop Runtime **x64** (Controller) und .NET 6 Runtime **x86** (das Plugin läuft im 32-Bit-Prozess `Omsi.exe`). |

Details: [Kompatibilität](../localized/de-DE/reference/compatibility.md) und
[Installation](../localized/de-DE/getting-started/installation.md).

## Was es bietet

**Sitzungen.** Eine Sitzung wird anhand einer `LaunchSpec` geplant (CLI-Flags,
eine JSON-Datei, ein Sitzungsprofil oder die API), ohne Seiteneffekte validiert
(`/plan`), anschließend gestartet, über `SessionState`-Übergänge beobachtet und
beendet. Beim Beenden einer Sitzung wird OMSI zwangsweise beendet, damit OMSI die
Dateien, die gleich wiederhergestellt werden, nicht überschreiben kann. Siehe
[Sitzungslebenszyklus](../localized/de-DE/concepts/session-lifecycle.md).

**Transaktionen und Recovery.** Jede Konfigurationsüberschreibung ist auf die
Sitzung beschränkt. OmsiLaunch erstellt für jede Datei, die es ändert, einen
Snapshot, protokolliert sie im Journal, wendet die Änderung an, überprüft sie und
stellt die Datei wieder her, auch nach einem Absturz (`/recovery-status`,
`/recover`, `RecoverPendingAsync`). Eine dauerhafte Bearbeitung der Konfiguration
bietet es nicht. Siehe
[Transaktionen und Recovery](../localized/de-DE/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Ein Referenz-Frontend über derselben öffentlichen API,
ohne eigene OMSI-Logik: Erkennung, Planung, Starten von Sitzungen und
Client-Befehle gegen eine laufende Sitzung. Siehe die [CLI-Referenz](../localized/de-DE/reference/cli.md)
und die [CLI-Beispiele](../localized/de-DE/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** Der Host im Windows-Subsystem für Verknüpfungen. Er
akzeptiert dieselbe Befehlszeile ohne Konsolenfenster und meldet Fehler in
Meldungsfenstern. Siehe [OmsiLaunchW.exe](../localized/de-DE/reference/omsilaunchw.md).

**Windows-Tray.** Jede Eigentümersitzung zeigt ein Symbol im Infobereich mit
einem Statusfenster und der Aktion „End session“ (`Sitzung beenden`). Die Aktion
nimmt denselben Stopppfad wie `session stop`. Siehe [Windows-Tray](../localized/de-DE/reference/windows-tray.md).

**Sitzungsprofile.** Deklarative `profile.yaml`-Pakete
(`omsilaunch.session-profile/v1`) unter
`.omsilaunch\session-profiles\<id>\`, damit Inhaltsautoren reproduzierbare
Sitzungen ausliefern können, die mit einem einzigen Befehl starten. Siehe
[Sitzungsprofile](../localized/de-DE/reference/session-profiles.md).

**Öffentliche API und Runtime-Steuerung.** `OmsiLaunch.Api` (`IOmsiLaunch`) ist
die bevorzugte Produktschnittstelle. Runtime-Operationen wie Zeit, Wetter, Karte,
Kamera, Fahrplan, Fahrzeuge, Menschen, Skriptvariablen und D3D-Texturen sind auf
die Sitzung beschränkt, werden gegen das Build-Profil validiert und über opake
semantische Handles adressiert, niemals über native Zeiger. Ergebnisse sind durch
einen Runtime-Slot von 64 KiB begrenzt. Siehe die [Referenz der öffentlichen API](../localized/de-DE/reference/public-api.md) und
[Runtime-Steuerung](../localized/de-DE/reference/runtime-control.md).

**Lokale Steuerung.** Eine Named Pipe pro Installation, die an die aktive
`session_id` gebunden ist, ermöglicht anderen Prozessen desselben Benutzers,
Status und Ereignisse zu lesen, die Sitzung zu beenden und öffentliche
Runtime-Operationen auszuführen. Siehe
[Lokale Steuerung / IPC](../localized/de-DE/reference/local-control.md).

**Capabilities.** Jede Capability und jede öffentliche Runtime-Operation ist mit
ihrer Stabilität katalogisiert. Experimentelle und nicht verfügbare Capabilities
werden aufgeführt, nicht versteckt (`OmsiLaunch.exe capabilities`). Siehe
[Capabilities](../localized/de-DE/reference/capabilities.md).

**Permanentes Plugin.** Die prozessinterne Plugin-Gesamtheit (Closure) wird
einmalig unter `plugins\OmsiLaunch.*` installiert. Vor jedem Start wird sie gegen
die SHA-256-Einträge von `release-manifest.json` geprüft. Plugins von
Drittanbietern werden nie angetastet. Siehe
[Modell des permanenten Plugins](../localized/de-DE/concepts/permanent-plugin.md).

## Schnellstart

Entpacken Sie das Release-Paket in das Installationsstammverzeichnis von OMSI 2
und führen Sie dann in diesem Verzeichnis die folgenden Befehle aus:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Während diese Sitzung läuft, kann eine zweite Konsole im selben Verzeichnis sie
abfragen oder beenden:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Schritt-für-Schritt-Anleitung: [Erste Sitzung](../localized/de-DE/getting-started/first-session.md). Für
.NET-Integratoren: [Schnellstart mit der öffentlichen API](../localized/de-DE/getting-started/api-quick-start.md).

## Dokumentation

Die vollständige Dokumentation ist in [`docs/localized/de-DE/README.md`](../localized/de-DE/README.md) verzeichnet.
Die englischen Seiten (US) unter `docs/` sind maßgeblich und normativ.

| Thema | Seite |
| --- | --- |
| Öffentliche API | [docs/localized/de-DE/reference/public-api.md](../localized/de-DE/reference/public-api.md) |
| CLI | [docs/localized/de-DE/reference/cli.md](../localized/de-DE/reference/cli.md) |
| CLI-Beispiele | [docs/localized/de-DE/reference/cli-examples.md](../localized/de-DE/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/de-DE/reference/omsilaunchw.md](../localized/de-DE/reference/omsilaunchw.md) |
| Windows-Tray | [docs/localized/de-DE/reference/windows-tray.md](../localized/de-DE/reference/windows-tray.md) |
| Sitzungsprofile | [docs/localized/de-DE/reference/session-profiles.md](../localized/de-DE/reference/session-profiles.md) |
| Runtime-Steuerung | [docs/localized/de-DE/reference/runtime-control.md](../localized/de-DE/reference/runtime-control.md) |
| Lokale Steuerung / IPC | [docs/localized/de-DE/reference/local-control.md](../localized/de-DE/reference/local-control.md) |
| Capabilities | [docs/localized/de-DE/reference/capabilities.md](../localized/de-DE/reference/capabilities.md) |
| Fehler und Exitcodes | [docs/localized/de-DE/reference/errors.md](../localized/de-DE/reference/errors.md), [docs/localized/de-DE/reference/exit-codes.md](../localized/de-DE/reference/exit-codes.md) |
| Paketierung | [docs/localized/de-DE/reference/packaging.md](../localized/de-DE/reference/packaging.md) |
| Bekannte Einschränkungen | [docs/localized/de-DE/reference/known-limitations.md](../localized/de-DE/reference/known-limitations.md) |
| Status der Runtime-Validierung | [docs/localized/de-DE/status/runtime-validation-status.md](../localized/de-DE/status/runtime-validation-status.md) |

### Dokumentation in anderen Sprachen

Die Dokumentation ist in 14 Gebietsschemas unter
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md) übersetzt. Die
Übersetzungen wurden aus den maßgeblichen englischen Seiten (US) erstellt und
maschinell gegen diese validiert. Ein redaktionelles Review durch
Muttersprachler war nicht Teil des Beta-4-Releases und kann nach der
Veröffentlichung folgen. Wenn eine Übersetzung und die englische Seite
voneinander abweichen, ist die englische Seite verbindlich.

## Einschränkungen und offene Punkte

Dies sind die wichtigsten Einschränkungen. Die vollständige Liste finden Sie unter
[Bekannte Einschränkungen](../localized/de-DE/reference/known-limitations.md).

- **Steam.** Steam LAA ist `PARTIAL`: Spielbetrieb, Lesezugriffe, Befehle
  und Beenden erfordern eine echte Steam-Installation und wurden nicht zur Laufzeit validiert.
- **In dieser Beta experimentell:** Starten aus dem letzten Kartenstatus (`/last`)
  sowie explizites Datum, explizite Uhrzeit, explizites Jahr oder Wetter beim Start
  sind statisch abgeschlossen und warten auf die Runtime-Validierung. **Nicht
  verfügbar:** die Zuweisung des Spielerfahrzeugs beim Start; wird sie angefordert,
  ist der Plan nicht ausführbar, statt dass die Anforderung stillschweigend ignoriert wird.
- **Runtime-Schreibzugriffe sind begrenzt.** `weather.set`, `weather.metar.set`,
  Schreibzugriffe auf String-Variablen und Skript-Trigger sind experimentell, bis
  sie zur Laufzeit validiert sind; Schreibzugriffe auf den Kalender und das
  Versetzen von Fahrzeugen sind nicht verfügbar. Runtime-Änderungen werden nicht
  im Journal protokolliert und nicht wiederhergestellt.
- **Lebensdauer von Handles.** Die Erkennung veralteter Handles für
  Straßenfahrzeuge und Menschen, die auf natürliche Weise verschwinden (RV-002),
  hat keinen sicheren Runtime-Auslöser und ist nur offline validiert.
- **Seitenweise Ergebnisse.** Runtime-Listen werden mit `cursor` und `limit`
  seitenweise geliefert und mit `next_cursor` fortgesetzt; eine Seite, die nicht in
  die Mailbox von 64 KiB passt, wird gekürzt und mit `truncated=true` gekennzeichnet.
- **Das Beenden erfolgt standardmäßig zwangsweise.** OMSI wird beendet, das eigene
  Herunterfahren von OMSI läuft nicht ab, und nicht gespeicherter OMSI-Zustand geht
  verloren. Das optionale kooperative Herunterfahren (`/cooperative-shutdown`,
  experimentell) führt zuvor die Beenden-Routine von OMSI aus.
- **Vertrauensmodell auf Benutzerebene.** Jeder Prozess desselben Windows-Benutzers
  kann die lokale Steuerungsebene (Control Plane) erreichen.

## Download

Laden Sie **`OmsiLaunch-0.1.0-beta4.zip`** und die zugehörige `.sha256`-Datei von der
[Releases-Seite](https://github.com/OmsiModsBR/OmsiLaunch/releases) herunter. Entpacken
Sie das Archiv direkt in das unterstützte OMSI-Stammverzeichnis. Das Paket enthält den Controller
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), seine Abhängigkeiten, die permanente Plugin-Closure
mit `release-manifest.json`, Startbild-Assets, ein Sitzungsbeispiel und die
Offline-Dokumentation unter `.omsilaunch\docs\`. Paketstruktur und rückstandsfreies
Entfernen: [Paketierung](../localized/de-DE/reference/packaging.md).

## Aus dem Quellcode bauen

Voraussetzungen:

- Windows 10 oder neuer, x64.
- .NET 6 SDK, mit den .NET-6-Runtimes für x64 und x86 zum Ausführen der Tests.
- Visual Studio mit der MSBuild-C++-Workload (Plattformtoolset `v145`) und einem
  Windows 10 SDK, für die drei nativen Projekte: `OmsiLaunch.Native.x86`
  (Win32) sowie `OmsiLaunch.Bootstrapper` und `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` enthält die verwalteten Projekte und Testsuiten. Der einzige
Offline-Einstiegspunkt baut alles und führt jede Offline-Testsuite aus; er
startet OMSI nie:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` überspringt die nativen Projekte und den Paketierungs-Regressionstest.
`-SkipDocs` überspringt die Dokumentations-Gates. Die Build-Ausgabe landet in `artifacts\`,
das nicht versioniert wird. `tools\New-ReleasePackage.ps1` stellt aus einem vorhandenen
Build ein Release-Paket zusammen, berechnet die Hashes, validiert und komprimiert es. Siehe
[Paketierung](../localized/de-DE/reference/packaging.md) und
[Tests und Validierung](../project/TESTING-AND-VALIDATION.md).

| Pfad | Inhalt |
| --- | --- |
| `src/` | Produktbibliotheken: API, Core, Konfiguration, Inhalte, Interop, Prozess, Plugin, Build-Profil, native x86-Grenzschicht |
| `tools/` | CLI und Windows-Host (`OmsiLaunch.Cli`), native Shims (`OmsiLaunch.Bootstrapper`), Offline-Testhost, Paketierungs- und Validierungsskripte, Lokalisierungswerkzeuge |
| `tests/` | Testsuiten für Unit-, Integrations-, Profil-, Windows-UI- und Dokumentationstests |
| `docs/` | Maßgebliche Dokumentation und ihre Übersetzungen unter `docs/localized/` |
| `examples/` | LaunchSpec- und Sitzungsbeispiele |
| `assets/` | Branding, Symbole und Paket-Assets |
| `third_party/` | Hinweise zur Herkunft von Upstream-Code |

Zusammenfassungen für Maintainer: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Community und Lizenz

OmsiLaunch ist ein Community-orientiertes Open-Source-Projekt. Es ist unabhängig von
anderen OMSI-Launchern, und kompatible Community-Tools können darauf aufbauen.

OmsiLaunch steht unter der Lizenz [LGPL-3.0-only](../../LICENSE). Siehe die
[Hinweise zu Drittanbietern](../../THIRD-PARTY-NOTICES.md) zur Herkunft des
übernommenen Quellcodes und zu den geltenden Hinweisen.
