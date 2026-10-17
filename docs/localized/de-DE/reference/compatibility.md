# Kompatibilität

<!-- l10n: source=reference/compatibility.md source-sha256=9f3a7654d57410845e20c92eccc746868cbac665b4d937581f16c3818d7b7b88 -->
> Übersetzung der [englischen Originalseite](../../../reference/compatibility.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

OmsiLaunch arbeitet mit einem installierten **OMSI 2**. Es setzt keine bestimmte `Omsi.exe`-Datei voraus: Jede native Annahme, die es innerhalb von OMSI trifft (eine Funktion, eine Patch-Stelle, eine Klasse, ein VMT-Slot, eine globale Variable, ein Feld-Offset), wird vor ihrer Verwendung einzeln, im Prozess, aufgelöst und verifiziert, und eine Capability ist nur verfügbar, wenn alle ihre Annahmen verifiziert sind. Diese Seite beschreibt, was „OMSI 2 installiert“ bedeutet, wie Builds identifiziert werden, wie Capabilities aufgelöst werden und welche Betriebssystem- und Runtime-Anforderungen gelten. Quellen: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Voraussetzung: OMSI 2 installiert

Ein Plan wird nur dann mit `OL_E_UNSUPPORTED_BUILD` abgelehnt, wenn die Installation keine OMSI-2-Installation ist:

| Prüfung | Abgelehnt, wenn |
| --- | --- |
| `Omsi.exe` | im Installationsstammverzeichnis fehlt |
| Image | kein 32-Bit-x86-PE-Image ist |
| Version | seine Versionsressource keine OMSI-2-Version (2.x) trägt |
| Installation | das Stammverzeichnis kein Verzeichnis `maps\` oder `Vehicles\` hat |

SHA-256, Größe und exakte Version der Programmdatei sind **keine** Kompatibilitätskriterien. Sie werden als Diagnosen (`omsi.build.detected`, `OmsiLaunch.exe profiles`) festgehalten und dienen als Nachweis.

<a id="build-identity-and-capability-resolution"></a>
## Build-Identität und Auflösung von Capabilities

| Konzept | Bedeutung |
| --- | --- |
| Code-Identität | SHA-256 des Hauptcodeabschnitts der Programmdatei. Eine bekannte Identität wählt als schneller Pfad ein bekanntes Layout aus. Varianten, die sich nur in den Headern unterscheiden (z. B. eine Large-Address-Aware-Kopie), teilen sich die Code-Identität. |
| Bekanntes Layout | `Omsi23004`: OMSI 2.3.004 (Dateiversion 2.2.032), Code-Identität `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Jede Runtime-Validierung auf der [Statusseite](../status/runtime-validation-status.md) lief auf diesem Layout. |
| Native Ziel | Eine Annahme der nativen Grenze, im Build-Katalog mit ihrem Guard deklariert. |
| Capability | Eine Gruppe von Zielen, z. B. `world.new-map` oder `time.read`. Nur verfügbar, wenn jedes Ziel aufgelöst **und** verifiziert ist. |

So wird ein Ziel im Plugin aufgelöst, bevor OmsiLaunch irgendetwas patcht:

1. **Bekannte Identität:** Zuerst wird die Referenzadresse des bekannten Layouts versucht, und sie muss weiterhin den Guard des Ziels bestehen.
2. **Andernfalls, oder wenn der Guard fehlschlägt**, werden build-unabhängige Strategien versucht:
   - Delphi-RTTI-Klassenname (`vmtSelfPtr` geprüft);
   - veröffentlichte Methoden, Felder und Eigenschaften einer Klasse (RTTI-Tabellen);
   - VMT-Slot, der eine aufgelöste Methode enthält;
   - eine maskierte Codesignatur, die genau einmal passen muss (Operanden, die vom Layout abhängen, sind Wildcards) und eine Adresse, eine globale Variable (direkt oder über ihre Zeigerzelle) oder eine Feldverschiebung innerhalb einer Methode der eigenen Klasse des Felds erfasst.
3. **Nichts passt eindeutig und besteht den Guard:** Das Ziel bleibt nicht aufgelöst. Es gibt **keinen Rückgriff auf eine feste Adresse**.

Guards: maskierte Einstiegsbytes für Funktionen und Patch-Stellen, das Aufrufziel für einen gepatchten Aufruf, `vmtSelfPtr` und der RTTI-Klassenname für Klassen, die gehaltene Methode für VMT-Slots und ein beschreibbarer Datenabschnitt für globale Variablen. Objektlayouts, die die Runtime-Reader lesen (Fahrzeug-, Human-, Wetter-, Fahrplandatensätze usw.), haben noch keine build-unabhängige strukturelle Validierung. Sie werden nur für eine bekannte Code-Identität verifiziert, sodass die Capabilities, die sie lesen, auf einem Build mit unbekannter Identität nicht verfügbar sind.

Auf dem bekannten Layout wird jede Capability aufgelöst. Auf einem Build mit unbekannter Identität können die Capabilities verfügbar sein, deren Ziele über die obigen Strategien aufgelöst werden (der Headless-Start, NEW_MAP, gespeicherte Situationen, Unterdrückung der Internettexturen, Lesen und Setzen der Zeit, D3D), und jede wird einzeln verifiziert; layoutabhängige Lesezugriffe sind nicht verfügbar.

<a id="where-compatibility-is-decided"></a>
## Wo über die Kompatibilität entschieden wird

| Phase | Was geschieht |
| --- | --- |
| Planung (`PlanSessionAsync`, `/plan`) | OMSI-2-Prüfung (`omsi.omsi2`, sonst `OL_E_UNSUPPORTED_BUILD`). Das Dateiimage wird als **Vorhersage** aufgelöst: Erforderliche native Capabilities tragen `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` oder `PENDING_IN_PROCESS_RESOLUTION` mit der Warnung `OL_W_CAPABILITY_PENDING`. Eine unbekannte Code-Identität fügt die Warnung `OL_W_BUILD_UNRECOGNIZED` hinzu. Warnungen führen nicht dazu, dass ein Plan nicht ausführbar wird. |
| Start (`StartSessionAsync`) | Die Spezifikation wird erneut geplant; ein Plan, der nicht mehr ausführbar ist, wird mit `OL_E_PLAN_NOT_RUNNABLE` abgelehnt. |
| Im Prozess (Plugin-Start) | Das geladene Image wird aufgelöst (maßgeblich; eine DRM-geschützte Programmdatei kann von ihrer Datei abweichen). Der Bericht wird als Telemetrie `native.capabilities` gesendet. Sind `boot.headless-start` und die Weltmodus-Capability (`world.new-map` oder `world.saved-situation`) nicht verfügbar, schlägt die Sitzung vor jedem Hook mit `OL_E_CAPABILITY_UNRESOLVED` fehl, und die Dateien werden wiederhergestellt. Sind sie verfügbar, aber das aktive Start-Formular ist nicht im erwarteten Zustand (z. B. weil ein anderes Plugin es gepatcht hat), schlägt die Sitzung mit `OL_E_BUILD_VALIDATION_FAILED` fehl. |
| Runtime-Operationen | Eine Operation, deren Capability nicht verfügbar ist, wird mit `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) abgelehnt, ohne den OMSI-Speicher zu berühren. |

`OmsiLaunch.exe profiles --json` meldet die Installation (`is_omsi2`, `build_id`, `identity`, Datei- und Code-Hashes, `file_variant`), die bekannten Layouts und die aus der Datei vorhergesagten Capabilities.

<a id="operating-system-and-architecture"></a>
## Betriebssystem und Architektur

`CurrentWindowsX64Platform.Detect` berechnet `RuntimePlatformInfo`. Die aktuelle Plattform wird nur unterstützt, wenn alle folgenden Bedingungen erfüllt sind:

| Voraussetzung | Prüfung | Fehler bei Verletzung |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 oder neuer | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-Bit-Windows und ein 64-Bit-Hostprozess | `OSArchitecture == X64` und `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Beschreibbare Installation | Das Stammverzeichnis existiert, ist nicht schreibgeschützt und enthält `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` meldet außerdem `OmsiArchitecture` und `PluginArchitecture` als `X86` (OMSI ist ein 32-Bit-Prozess; die Plugin-Gesamtheit (Closure) ist x86 und läuft unter WOW64), `LegacyPlatform=false` sowie `Wow64Available`. ARM64-Windows wird nicht unterstützt, selbst wenn eine x64-Emulation vorhanden ist, da der Hostprozess selbst x64 sein muss.

<a id="net-requirements"></a>
## .NET-Anforderungen

| Komponente | Runtime | Hinweise |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Der native Bootstrapper findet die Runtime über `hostfxr` mithilfe der mitgelieferten `nethost.dll`. Eine fehlende Runtime wird vom Shim gemeldet (Exitcodes 100-106; siehe [CLI](cli.md) und [Exitcodes](exit-codes.md)). |
| Plugin-Closure (`plugins\OmsiLaunch.Plugin.dll` über `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), gehostet von DNNE 2.0.6 innerhalb von `Omsi.exe` | Erfordert, dass die x86-.NET-6-Desktop-/Core-Runtime auf dem Rechner installiert ist; die 64-Bit-Runtime allein reicht für das Plugin nicht aus. |
| Native Bridge (`plugins\OmsiLaunch.Native.x86.dll`) | natives x86 | Wird nur aus `plugins\` geladen (siehe [permanentes Plugin](../concepts/permanent-plugin.md)). Sie enthält keine OMSI-Adresse: Sie erhält die verifizierte Zieltabelle vom Plugin. |

<a id="legacy-platforms"></a>
## Legacy-Plattformen

Windows 7, Windows 8.x, Windows XP und andere Systeme mit NT 6 und älter liegen außerhalb der aktuellen Supportgrenze. `RuntimePlatformInfo.LegacyPlatform` ist immer `false`, und es existiert kein Legacy-Adapter; das Feld und die `IPluginNativeServices`-Schnittstelle existieren nur, damit ein künftiger Legacy-Adapter ohne Änderung der öffentlichen API hinzugefügt werden könnte (siehe `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nichts in diesem Release läuft auf diesen Systemen.

<a id="steam-and-large-address-aware-notes"></a>
## Hinweise zu Steam und Large Address Aware

- Eine Large-Address-Aware-Kopie, die sich nur in ihren Headern unterscheidet, hat dieselbe Code-Identität wie ihr Original und verwendet dasselbe bekannte Layout.
- Die Steam-Programmdatei ist auf dem Datenträger DRM-geschützt, sodass ihre Datei von ihrem geladenen Image abweichen kann: Der Plan kann `OL_W_CAPABILITY_PENDING` melden, und die Auflösung im Prozess entscheidet.
- Steam startet OMSI selbst; eine Sitzung muss über `OmsiLaunch.exe` gestartet werden, damit die Übergabe existiert. Beim Start über Steam bleibt das permanente Plugin inaktiv (keine Übergabe, keine Hooks).

<a id="related-pages"></a>
## Verwandte Seiten

- [Bekannte Einschränkungen](known-limitations.md)
- [Status der Runtime-Validierung](../status/runtime-validation-status.md)
- [Installation](../getting-started/installation.md)
