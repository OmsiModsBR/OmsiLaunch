# Compatibiliteit

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/compatibility.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

OmsiLaunch werkt met een geïnstalleerde **OMSI 2**. Het vereist geen bepaald bestand `Omsi.exe`: elke native aanname die het binnen OMSI doet (een functie, een patchlocatie, een klasse, een VMT-slot, een global, een veldoffset) wordt afzonderlijk, in het proces, opgelost en geverifieerd voordat ze wordt gebruikt, en een capability is alleen beschikbaar wanneer al haar aannames zijn geverifieerd. Deze pagina beschrijft wat "OMSI 2 geïnstalleerd" betekent, hoe builds worden geïdentificeerd, hoe capabilities worden opgelost en welke eisen het besturingssysteem en de runtime stellen. Bronnen: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Vereiste: OMSI 2 geïnstalleerd

Een plan wordt alleen met `OL_E_UNSUPPORTED_BUILD` geweigerd wanneer de installatie geen OMSI 2-installatie is:

| Controle | Geweigerd wanneer |
| --- | --- |
| `Omsi.exe` | ontbreekt in de installatiemap |
| Image | geen 32-bits x86 PE-image |
| Versie | de versieresource draagt geen OMSI 2-versie (2.x) |
| Installatie | de hoofdmap heeft geen map `maps\` of `Vehicles\` |

De SHA-256, de grootte en de exacte versie van het uitvoerbare bestand zijn **geen** compatibiliteitscriteria. Ze worden vastgelegd als diagnose (`omsi.build.detected`, `OmsiLaunch.exe profiles`) en dienen als bewijs.

<a id="build-identity-and-capability-resolution"></a>
## Buildidentiteit en oplossing van capabilities

| Concept | Betekenis |
| --- | --- |
| Code-identiteit | SHA-256 van de hoofdcodesectie van het uitvoerbare bestand. Een bekende identiteit selecteert een bekende indeling als snelle route. Varianten die alleen in de headers verschillen (bijvoorbeeld een Large Address Aware-kopie) delen de code-identiteit. |
| Bekende indeling | `Omsi23004`: OMSI 2.3.004 (bestandsversie 2.2.032), code-identiteit `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Elke runtimevalidatie op de [statuspagina](../status/runtime-validation-status.md) is op deze indeling uitgevoerd. |
| Native doel | Eén aanname van de native grens, gedeclareerd in de buildcatalogus met haar guard. |
| Capability | Een groep doelen, bijvoorbeeld `world.new-map` of `time.read`. Alleen beschikbaar wanneer elk doel is opgelost **en** geverifieerd. |

Hoe een doel wordt opgelost, in de plugin, voordat OmsiLaunch iets patcht:

1. **Bekende identiteit:** het referentieadres van de bekende indeling wordt eerst geprobeerd en moet de guard van het doel nog steeds doorstaan.
2. **Anders, of wanneer de guard mislukt**, worden buildonafhankelijke strategieën geprobeerd:
   - Delphi-RTTI-klassenaam (`vmtSelfPtr` gecontroleerd);
   - gepubliceerde methoden, velden en eigenschappen van een klasse (RTTI-tabellen);
   - VMT-slot dat een opgeloste methode bevat;
   - een gemaskeerde codesignatuur die precies één keer moet overeenkomen (operanden die van de indeling afhangen, zijn jokertekens), waarbij een adres, een global (rechtstreeks of via zijn pointercel) of een veldverplaatsing wordt vastgelegd vanuit een methode van de eigen klasse van het veld.
3. **Niets komt eenduidig overeen en doorstaat de guard:** het doel blijft onopgelost. Er is **geen terugval op een vast adres**.

Guards: gemaskeerde instapbytes voor functies en patchlocaties, de aanroepbestemming voor een gepatchte aanroep, `vmtSelfPtr` en de RTTI-klassenaam voor klassen, de vastgehouden methode voor VMT-slots en een beschrijfbare datasectie voor globals. Objectindelingen die de runtimereaders lezen (voertuig-, mens-, weer-, dienstregelingrecords enzovoort) hebben nog geen buildonafhankelijke structurele validatie. Ze worden alleen voor een bekende code-identiteit geverifieerd, dus de capabilities die ze lezen, zijn op een build met een onbekende identiteit niet beschikbaar.

Op de bekende indeling wordt elke capability opgelost. Op een build met een onbekende identiteit kunnen de capabilities waarvan de doelen via de bovenstaande strategieën worden opgelost (de headless start, NEW_MAP, opgeslagen situaties, onderdrukking van internettextures, tijd lezen en instellen, D3D) beschikbaar zijn, en elke wordt afzonderlijk geverifieerd; leesacties die van de indeling afhangen, zijn niet beschikbaar.

<a id="where-compatibility-is-decided"></a>
## Waar compatibiliteit wordt bepaald

| Fase | Wat er gebeurt |
| --- | --- |
| Plannen (`PlanSessionAsync`, `/plan`) | OMSI 2-controle (`omsi.omsi2`, anders `OL_E_UNSUPPORTED_BUILD`). Het bestandsimage wordt opgelost als **voorspelling**: vereiste native capabilities dragen `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` of `PENDING_IN_PROCESS_RESOLUTION` met waarschuwing `OL_W_CAPABILITY_PENDING`. Een onbekende code-identiteit voegt waarschuwing `OL_W_BUILD_UNRECOGNIZED` toe. Waarschuwingen maken een plan niet niet-uitvoerbaar. |
| Start (`StartSessionAsync`) | De spec wordt opnieuw gepland; een plan dat niet langer uitvoerbaar is, wordt geweigerd met `OL_E_PLAN_NOT_RUNNABLE`. |
| In het proces (start van de plugin) | Het geladen image wordt opgelost (gezaghebbend; een DRM-beschermd uitvoerbaar bestand kan van zijn bestand verschillen). Het rapport wordt als telemetrie `native.capabilities` verzonden. Als `boot.headless-start` en de capability voor de wereldmodus (`world.new-map` of `world.saved-situation`) niet beschikbaar zijn, mislukt de sessie met `OL_E_CAPABILITY_UNRESOLVED` vóór enige hook en worden de bestanden hersteld. Als ze beschikbaar zijn maar het live Start-formulier niet in de verwachte toestand is (bijvoorbeeld doordat een andere plugin het patchte), mislukt de sessie met `OL_E_BUILD_VALIDATION_FAILED`. |
| Runtime-operaties | Een operatie waarvan de capability niet beschikbaar is, wordt geweigerd met `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) zonder het geheugen van OMSI aan te raken. |

`OmsiLaunch.exe profiles --json` rapporteert de installatie (`is_omsi2`, `build_id`, `identity`, bestands- en codehashes, `file_variant`), de bekende indelingen en de capabilities die uit het bestand worden voorspeld.

<a id="operating-system-and-architecture"></a>
## Besturingssysteem en architectuur

`CurrentWindowsX64Platform.Detect` berekent `RuntimePlatformInfo`. Het huidige platform wordt alleen ondersteund als aan alle volgende voorwaarden wordt voldaan:

| Vereiste | Controle | Fout bij overtreding |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 of later | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64-bits Windows en een 64-bits hostproces | `OSArchitecture == X64` en `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Beschrijfbare installatie | De hoofdmap bestaat, is niet alleen-lezen en bevat `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` meldt daarnaast `OmsiArchitecture` en `PluginArchitecture` als `X86` (OMSI is een 32-bits proces; de plugin-closure is x86 en draait onder WOW64), `LegacyPlatform=false` en `Wow64Available`. ARM64-Windows wordt niet ondersteund, ook niet waar x64-emulatie bestaat, omdat het hostproces zelf x64 moet zijn.

<a id="net-requirements"></a>
## .NET-vereisten

| Component | Runtime | Opmerkingen |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | De native bootstrapper zoekt de runtime op via `hostfxr` met behulp van de meegeleverde `nethost.dll`. Een ontbrekende runtime wordt door de shim gemeld (exitcodes 100-106; zie [CLI](cli.md) en [exitcodes](exit-codes.md)). |
| Plugin-closure (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), gehost door DNNE 2.0.6 binnen `Omsi.exe` | Vereist dat de x86-versie van de .NET 6 Desktop/Core-runtime op de computer is geïnstalleerd; de 64-bits runtime alleen is niet voldoende voor de plugin. |
| Native brug (`plugins\OmsiLaunch.Native.x86.dll`) | native x86 | Wordt alleen geladen vanuit `plugins\` (zie [permanente plugin](../concepts/permanent-plugin.md)). Het bevat geen OMSI-adres: het ontvangt de geverifieerde doeltabel van de plugin. |

## Legacy-platforms

Windows 7, Windows 8.x, Windows XP en andere systemen met NT 6 en ouder vallen buiten de huidige ondersteuningsgrens. `RuntimePlatformInfo.LegacyPlatform` is altijd `false` en er bestaat geen legacy-adapter; het veld en de naad `IPluginNativeServices` bestaan alleen zodat in de toekomst een legacy-adapter kan worden toegevoegd zonder de openbare API te wijzigen (zie `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Niets in deze release draait op die systemen.

<a id="steam-and-large-address-aware-notes"></a>
## Opmerkingen over Steam en Large Address Aware

- Een Large Address Aware-kopie die alleen in haar headers verschilt, heeft dezelfde code-identiteit als haar origineel en gebruikt dezelfde bekende indeling.
- Het Steam-uitvoerbare bestand is op schijf DRM-beschermd, dus zijn bestand kan van het geladen image verschillen: het plan kan `OL_W_CAPABILITY_PENDING` melden en de oplossing in het proces beslist. Een sessie met het echte Steam-uitvoerbare bestand is niet runtime-gevalideerd (zie [status van de runtimevalidatie](../status/runtime-validation-status.md)).
- Steam start OMSI zelf; een sessie moet via `OmsiLaunch.exe` worden gestart, zodat de handoff bestaat. Bij een start vanuit Steam blijft de permanente plugin inactief (geen handoff, geen hooks).

<a id="related-pages"></a>
## Gerelateerde pagina's

- [Bekende beperkingen](known-limitations.md)
- [Status van de runtimevalidatie](../status/runtime-validation-status.md)
- [Installatie](../getting-started/installation.md)
