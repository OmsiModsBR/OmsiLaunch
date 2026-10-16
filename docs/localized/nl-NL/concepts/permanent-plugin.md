# De permanente plugin

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../concepts/permanent-plugin.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

OmsiLaunch bestuurt OMSI van binnen het OMSI-proces via een plugin die één keer, onder `plugins\OmsiLaunch.*`, als onderdeel van het product wordt geïnstalleerd. Een sessie plaatst, kopieert, maakt snapshots van, herstelt of verwijdert deze plugin nooit. Deze pagina legt uit wat de closure bevat, hoe OMSI deze laadt, hoe de host deze vóór elke start valideert, hoe host en plugin met elkaar communiceren (handoff, telemetrie, runtime-mailbox) en wat de plugin doet als OMSI zonder OmsiLaunch wordt gestart. Bronnen: `src/OmsiLaunch.Process/RuntimeDeployment.cs` (`RuntimeArtifactSet`, `ReleaseManifest`, de drie stores in gedeeld geheugen), `src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`, `src/OmsiLaunch.Plugin/PluginRuntime.cs`, `src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`, `src/OmsiLaunch.Api/StartupHandoff.cs` en `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`.

<a id="the-closure-9-files"></a>
## De closure (9 bestanden)

| Bestand onder `plugins\` | Rol |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | Plugindescriptor voor OMSI. De inhoud is `[dll]` gevolgd door `OmsiLaunch.PluginNE.dll`. |
| `OmsiLaunch.PluginNE.dll` | Native x86-exportshim, gegenereerd door DNNE 2.0.6. Exporteert de plugin-ABI van OMSI (`PluginStart`, `PluginFinalize`, `AccessVariable`, `AccessTrigger`, `AccessStringVariable`, `AccessSystemVariable`) en host de .NET-runtime. Bevat de versieresource van het product. |
| `OmsiLaunch.Plugin.dll` | Managed plugin (`net6.0-windows`, x86): `CurrentDnneAdapter`, `PluginRuntime`, `CurrentRuntimeControl`, `CurrentTelemetrySink`. |
| `OmsiLaunch.Plugin.deps.json` | .NET-afhankelijkheidsmanifest voor de plugin. |
| `OmsiLaunch.Plugin.runtimeconfig.json` | .NET-runtimeconfiguratie (framework `Microsoft.NETCore.App` 6.0, `win-x86`). |
| `OmsiLaunch.Api.dll` | Wire-formaten en openbare records die met de host worden gedeeld. |
| `OmsiLaunch.Builds.Omsi23004.dll` | Het buildprofiel: vingerafdruk van het uitvoerbare bestand, globals, objectindelingen, methodeadressen. |
| `OmsiLaunch.Interop.dll` | Lezers en schrijvers voor geheugen binnen het proces, gebouwd op het profiel. |
| `OmsiLaunch.Native.x86.dll` | Native brug (C++): buildvalidatie, hook voor de headless start, toepassen van de tijd, MakeVehicle, PlaceRandomBus, onderdrukking van internettextures, toegang tot het D3D9-apparaat. |

`RuntimeArtifactSet.Load` leidt deze lijst af uit de eigen map `plugins\` van de controller: de vier benoemde bestanden (`.opl`, `PluginNE.dll`, `deps.json`, `runtimeconfig.json`), elke `OmsiLaunch.*.dll` in die map behalve `OmsiLaunch.PluginNE.dll`, en de native brug. `OmsiLaunch.Plugin.dll` moet daartoe behoren. Willekeurige DLL's worden nooit in OMSI meegenomen. De releasepackager (`tools/New-ReleasePackage.ps1`) schrijft precies de negen bestanden hierboven.

<a id="how-omsi-loads-it"></a>
## Hoe OMSI de plugin laadt

1. OMSI somt `plugins\*.opl` op en laadt de DLL die in `OmsiLaunch.Plugin.opl` wordt genoemd: `OmsiLaunch.PluginNE.dll`.
2. De DNNE-shim start binnen het OMSI-proces de x86-.NET 6-runtime die door `OmsiLaunch.Plugin.runtimeconfig.json` wordt beschreven en resolvet de managed exports in `OmsiLaunch.Plugin.dll`.
3. OMSI roept `PluginStart` aan. OMSI kan deze functie tijdens het opstarten meer dan eens aanroepen; alleen de eerste aanroep wordt gehonoreerd (bewaking met `Interlocked.Exchange`), omdat een geslaagde start eigenaar is van een eenmalige native hook. Latere aanroepen keren onmiddellijk terug.
4. `PluginStart` controleert eerst `OMSILAUNCH_INTERNET_TEXTURES_MODE`: als deze `Disabled` is, wordt de native onderdrukking van de downloader toegepast (`internet-textures.suppressed` of `internet-textures.suppression.failed`).
5. `PluginRuntime.Start` leest de handoff (zie hieronder), valideert de build binnen het proces, activeert de hook voor de headless start, opent de runtime-mailbox en plant de wereldstart op de UI-thread van OMSI via een `SetTimer`-callback. Er wordt nooit een runtimeopdracht op een IPC-workerthread uitgevoerd; alles draait in de timercallback, op de oorspronkelijke UI-thread van OMSI.
6. `PluginFinalize` stopt de timer, sluit de runtime af (`NativeD3DShutdown`) en herstelt de patch voor internettextures.

De exports `AccessVariable`, `AccessTrigger`, `AccessStringVariable` en `AccessSystemVariable` zijn leeg; OmsiLaunch gebruikt het pluginkanaal voor scriptvariabelen van OMSI niet.

<a id="integrity-validation-before-every-start"></a>
## Integriteitsvalidatie vóór elke start

`RuntimeArtifactSet.ValidateInstalled` wordt uitgevoerd tijdens `StartSessionAsync` (na de vroege recovery, voordat de transactie wordt voorbereid) en tijdens `PlanSessionAsync` (alleen aanwezigheid, via `LoadArtifacts`). De plandiagnose `plugin.integrity.reference` meldt welke referentie is gebruikt.

| Situatie | Referentie | Controle per bestand | Fouten |
| --- | --- | --- | --- |
| `release-manifest.json` aanwezig naast `OmsiLaunch.exe` (geïnstalleerd pakket) | `manifest` | Het geïnstalleerde bestand moet bestaan en de SHA-256 ervan moet gelijk zijn aan het manifestitem voor `plugins/<name>` | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (manifest heeft geen item voor een vereist bestand), `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Geen manifest (ontwikkelindeling) | `self` | Aanwezigheid plus zelfconsistentie: het geïnstalleerde bestand moet dezelfde hash hebben als het bestand in de eigen map `plugins\` van de controller | `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| Manifest onleesbaar of onjuist gevormd (array `files` ontbreekt, item zonder `path`/`sha256`) | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| Closure onvolledig in de map van de controller | | | `OL_E_RUNTIME_ARTIFACT_MISSING` (plan niet uitvoerbaar); de CLI meldt daarnaast `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` als `plugins\OmsiLaunch.Plugin.opl` of `OmsiLaunch.Native.x86.dll` ontbreekt |

Alleen de `plugins/`-items van het manifest worden gebruikt; het manifest is data, nooit beleid. Het manifest wordt door de releasepackager gegenereerd met de SHA-256 van elk geplaatst bestand. Wanneer de controller vanuit de OMSI-hoofdmap draait (de releaseindeling), zijn bron en doel dezelfde map; zonder manifest wordt de controle daarom teruggebracht tot aanwezigheid en zelfconsistentie, en daarom bevatten releasepakketten altijd `release-manifest.json`. Oplossing bij een afwijking: installeer het pakket opnieuw, zodat `plugins\` en `release-manifest.json` met elkaar overeenkomen.

Voordat er iets wordt gepatcht, lost de plugin elk native doel van de buildcatalogus op en verifieert het tegen het geladen image, en geeft de geverifieerde tabel door aan `OmsiLaunch.Native.x86.dll`, dat geen OMSI-adres bevat (telemetrie `native.capabilities`). Een start waarvan de capability `boot.headless-start` of de wereldmodus niet beschikbaar is, mislukt met `plugin.capability.unresolved` (`OL_E_CAPABILITY_UNRESOLVED`); een live Start-formulier in een onverwachte toestand mislukt met `plugin.build.invalid` (`OL_E_BUILD_VALIDATION_FAILED`). Zie [compatibiliteit](../reference/compatibility.md).

<a id="host-to-plugin-environment-variables"></a>
## Van host naar plugin: omgevingsvariabelen

`CreateProcessW` start `Omsi.exe` met de omgeving van het bovenliggende proces plus:

| Variabele | Waarde | Gebruiker |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | Sessie-GUID (`D`-notatie) | `CurrentRuntimeControl` markeert D3D-handles ermee (`N`-notatie) |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` opent deze mapping alleen-lezen |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`, `Disabled` of `Override` | `PluginStart` (alleen `Disabled` heeft effect binnen het proces) |

De drie mappings worden door de host aangemaakt voordat het proces start (`CurrentStartupHandoffStore`, `CurrentTelemetryStore`, `CurrentRuntimeCommandStore`) en vrijgegeven wanneer de levenscyclustaak van de sessie eindigt. Het zijn benoemde kernelobjecten met de standaard-DACL van de startende gebruiker; elk proces van dezelfde gebruiker kan ze openen (geaccepteerd vertrouwensmodel voor dezelfde gebruiker, zie [bekende beperkingen](../reference/known-limitations.md)).

<a id="the-startup-handoff"></a>
## De opstart-handoff

Een memory-mapped, alleen-lezen record met vaste indeling (`StartupHandoffWire`, magic `OLSH`, versie 4; versie 3 wordt door de lezer nog geaccepteerd). De header van 64 bytes bevat de magic, de versie, de headergrootte, de totale grootte, de sessie-GUID, de payloadgrootte en de SHA-256 van de payload. De payload bevat `BuildProfileId`, `MapIdentity`, `EntrypointIdentity`, `SituationIdentity` (UTF-8 met lengtevoorvoegsel), `PresentedEntrypointIndex`, `WorldMode`, vlaggen (`HeadlessStart`, `PlayerVehicleEnabled`), `DateMode` en `TimeMode`. De plugin berekent de hash van de payload opnieuw en wijst elke afwijking af (`plugin.handoff.invalid`, hostfout `OL_E_PLUGIN_PROTOCOL_MISMATCH`). Een mapping groter dan 1 MiB of met een inconsistente grootte wordt op dezelfde manier afgewezen.

De plugin accepteert een handoff alleen als `WorldMode` `NewMap` of `SavedSituation` is, `HeadlessStart` is ingesteld, `PlayerVehicleEnabled` niet is ingesteld, zowel de datum- als de tijdmodus `Unset` zijn en een opgeslagen situatie haar `.osn` noemt. Al het andere is `plugin.request.unsupported` (hostfout `OL_E_CAPABILITY_UNAVAILABLE`). De host stelt `HeadlessStart` altijd in.

<a id="telemetry-slot"></a>
## Telemetrieslot

`OmsiLaunch.Telemetry.<session>` is een eventring (Beta 4, `TelemetryRingWire`): een header van 64 bytes (magic `OLTR`, versie 1, aantal slots, slotgrootte, vastgelegd volgnummer) en 64 genummerde slots van 2048 bytes, elk met één UTF-8-JSON-event `{ "name": ..., "data": { ... }, "tick": <ms> }` (`tick` is de monotone sessieklok van de plugin). De plugin is de enige schrijver: die maakt een slot ongeldig, schrijft het, legt het volgnummer van het slot vast en publiceert daarna het volgnummer van de ring. De host leest elke 100 ms elk event dat sinds de laatste leesactie is vastgelegd, in volgorde; een slot dat tijdens het kopiëren werd overschreven, telt als verloren en wordt nooit half geschreven geleverd. Een host die meer dan 64 events achterloopt, ontvangt het runtime-event `telemetry.lost` met het exacte aantal. Een event dat groter is dan een slot, wordt vervangen door `telemetry.oversized`, dat het event noemt. Events worden toegevoegd aan `SessionStatus.RuntimeEvents` (begrensd tot de laatste 256) en sturen de semantische levenscyclus aan (`plugin.started`, `world.starting`, `gameplay.entered`, module- en foutevents).

<a id="runtime-command-mailbox"></a>
## Runtime-mailbox voor opdrachten

`OmsiLaunch.Runtime.<session>` is een mailbox van 64 KiB voor één aanvraag tegelijk: `state` (int32 op 0: 0 inactief, 1 aangevraagd, 2 beantwoord), `length` (int32 op 4), envelope op 8. Envelopes zijn `RuntimeCommandWire`-records (magic `OLRC`, versie 1, header van 72 bytes met soort, totale lengte, sessie-GUID, aanvraag-id, payloadlengte en SHA-256 van de UTF-8-JSON-payload). De plugin pollt de mailbox vanuit zijn timer op de UI-thread (50 ms zodra de wereld is geladen), voert de opdracht op die thread uit en publiceert het antwoord alleen als het slot nog dezelfde aanvraag-id bevat; een aanvraag die de host bij een timeout heeft opgegeven, wordt nooit beantwoord. Een te groot antwoord wordt vervangen door een getypeerde fout `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Details en timeouts staan in [runtimebesturing](../reference/runtime-control.md).

<a id="dll-search-policy"></a>
## Zoekbeleid voor DLL's

`OmsiLaunch.Plugin`, `OmsiLaunch.Interop`, `OmsiLaunch.Process` en de CLI-assembly's declareren `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`. Native imports (`OmsiLaunch.Native.x86.dll`, `user32.dll`, `kernel32.dll`) worden alleen opgezocht in de eigen map van de assembly (`plugins\`) of in de systeemmap van Windows; de OMSI-hoofdmap en `PATH` worden nooit doorzocht. `OmsiLaunch.Native.x86.dll` wordt daarom uit `plugins\` geladen en nergens anders vandaan.

<a id="when-omsi-is-started-without-omsilaunch"></a>
## Als OMSI zonder OmsiLaunch wordt gestart

Omdat de closure permanent is, laadt OMSI `OmsiLaunch.PluginNE.dll` bij elke start, ook bij starts vanuit Steam of vanaf het bureaublad. In dat geval:

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` ontbreekt, dus wordt er geen downloaderpatch toegepast.
- `OMSILAUNCH_HANDOFF_NAME` ontbreekt, dus verzendt `PluginRuntime.Start` `plugin.handoff.invalid` en retourneert het `false`. `CurrentTelemetrySink.Emit` keert onmiddellijk terug als `OMSILAUNCH_TELEMETRY_NAME` niet is ingesteld, zodat er nergens iets wordt geschreven.
- Geen buildvalidatie, geen native hook, geen mailbox, geen timer. De plugin blijft geladen maar inactief; OMSI gedraagt zich alsof de plugin er niet is.
- `PluginFinalize` roept bij het afsluiten van OMSI de native herstelroutine en `NativeD3DShutdown` aan; beide zijn no-ops als er niets was geïnstalleerd.

De `.opl` hoeft daarom niet te worden verwijderd om OMSI normaal uit te voeren.

<a id="non-interference-with-third-party-plugins"></a>
## Geen interferentie met plugins van derden

Sessies sommen nooit andere bestanden in `plugins\` op, berekenen er geen hash van en kopiëren, verwijderen of herstellen ze nooit. De plugin gebruikt het `AccessVariable`-kanaal van OMSI niet en raakt de toestand van andere plugins niet aan. De enige patches binnen het proces zijn de geprofileerde hook voor de headless start (een eenmalige VMT-omleiding die voor de sessie wordt geactiveerd), de optionele onderdrukking van internettextures (hersteld in `PluginFinalize`) en de onderschepping van `Reset` op het D3D9-apparaat, die wordt gebruikt om de levenscyclus van textures te volgen.

<a id="difference-from-omsihook"></a>
## Verschil met OmsiHook

OmsiLaunch heeft **geen runtime-afhankelijkheid** van OmsiHook of van een binair bestand van OmsiHook: de enige pakketten waarnaar wordt verwezen, zijn `DNNE` 2.0.6 en `YamlDotNet` 15.1.2, en nergens in het product staat een `using OmsiHook` of een P/Invoke naar DLL's van OmsiHook. Wat OmsiLaunch wel met OmsiHook deelt, is afgeleide kennis: objectindelingen en verschillende leeswrappers zijn afgestemd op basis van de vastgepinde checkout van OmsiHook (`space928/Omsi-Extensions`, commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`, LGPL-3.0-only) tegen het exacte uitvoerbare bestand `Omsi23004_692EBFBF`. De naamsvermelding en licentievoorwaarden staan in `THIRD-PARTY-NOTICES.md` en de hergebruikmatrix per bestand in `third_party/OMSIHOOK-REUSE-MATRIX.md`. OmsiHook injecteert in een afzonderlijk proces en stelt ruwe pointers beschikbaar; OmsiLaunch draait binnen het proces, stelt alleen ondoorzichtige handles met sessiebereik beschikbaar en verwijdert elk native adres uit openbare resultaten (zie [capabilities](../reference/capabilities.md)).
