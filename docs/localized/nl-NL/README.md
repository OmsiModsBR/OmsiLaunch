# OmsiLaunch-documentatie

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../README.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Dit is de normatieve Engelstalige documentatie van OmsiLaunch `0.1.0-beta4`, de
basislijn na de hardeningronde. OmsiLaunch biedt programmeerbaar starten,
sessie-eigendom en runtimebesturing voor een geïnstalleerde OMSI 2; elke native capability
wordt per build opgelost en geverifieerd (zie [compatibiliteit](reference/compatibility.md)). Elke pagina onder `docs/` beschrijft wat de huidige code
doet; als een pagina en de code van elkaar afwijken, is de code leidend en is de pagina een bug.

Stabiliteitsbegrippen die overal worden gebruikt: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`,
`INTERNAL`, `UNAVAILABLE`. Vlaggen die worden geparsed maar niets doen, zijn gemarkeerd als
`ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`. Niets wordt
runtime-gevalideerd genoemd tenzij de
[runtimevalidatiestatus](status/runtime-validation-status.md) dat vermeldt.

<a id="who-reads-what"></a>
## Wie leest wat

| Doelgroep | Begin hier | Daarna |
| --- | --- | --- |
| Gebruikers (CLI, snelkoppelingen, sessieprofielen) | [Installatie](getting-started/installation.md), [Eerste sessie](getting-started/first-session.md) | [CLI-referentie](reference/cli.md), [CLI-voorbeelden](reference/cli-examples.md), [Sessieprofielen](reference/session-profiles.md), [Windows-systeemvak](reference/windows-tray.md), [Exitcodes](reference/exit-codes.md) |
| Integrators (`OmsiLaunch.Api`, lokale IPC) | [Snelstart publieke API](getting-started/api-quick-start.md), [Referentie publieke API](reference/public-api.md), [LaunchSpec-referentie](reference/launchspec.md) | [Sessielevenscyclus](concepts/session-lifecycle.md), [Runtimebesturing](reference/runtime-control.md), [Capabilities](reference/capabilities.md), [Local control / IPC](reference/local-control.md), [Foutreferentie](reference/errors.md) |
| Beheerders (release, validatie, grenzen) | [Packaging](reference/packaging.md), [Model van de permanente plugin](concepts/permanent-plugin.md) | [Transacties en recovery](concepts/transactions-and-recovery.md), [Compatibiliteit](reference/compatibility.md), [Bekende beperkingen](reference/known-limitations.md), [Runtimevalidatiestatus](status/runtime-validation-status.md) |

<a id="navigation"></a>
## Navigatie

| Pagina | Doel |
| --- | --- |
| [Aan de slag](getting-started/first-session.md) | Eén sessie plannen, starten, observeren en stoppen vanuit de OMSI-hoofdmap. |
| [Installatie](getting-started/installation.md) | Vereisten, het pakket uitpakken in de OMSI-hoofdmap, controleren met `/version`, schoon verwijderen. |
| [Snelstart publieke API](getting-started/api-quick-start.md) | Een volledig .NET-programma dat één sessie plant, start, uitleest en stopt. |
| [CLI-referentie](reference/cli.md) | Elke vlag, elk opdrachtwoord en elke hiërarchische route van `OmsiLaunch.exe` / `OmsiLaunchW.exe`. |
| [CLI-voorbeelden](reference/cli-examples.md) | Opdrachtregels om te kopiëren en te plakken voor veelvoorkomende taken. |
| [LaunchSpec-referentie](reference/launchspec.md) | Elke `LaunchSpec`-eigenschap en elke enumwaarde, JSON-laadregels voor `/spec`. |
| [HOF-beheer](reference/hof-management.md) | HOF-kopie en -mirror per sessie naar voertuigmappen: bronnen, doelen, conflicten, herstel. |
| [Runtimemodules](../../reference/runtime-modules.md) | Modules die met een sessie in OMSI draaien: manifest, vertrouwen, levenscyclus, faalbeleid, operaties. |
| [Referentie sessieprofielen](reference/session-profiles.md) | `profile.yaml`-schema `omsilaunch.session-profile/v1`, sleutels, limieten, voorrang. |
| [Referentie publieke API](reference/public-api.md) | `IOmsiLaunch`, publieke records en enums, stabiliteit per member. |
| [Inventaris publieke API](reference/public-api-inventory.md) | Gegenereerde lijst van elk publiek type en elke member met signatuur en stabiliteit. |
| [Runtimebesturing](reference/runtime-control.md) | Runtimeopdrachtkanaal, timeouts, handles, stopsemantiek. |
| [Referentie capabilities](reference/capabilities.md) | Capabilitycatalogus en elke publieke runtime-operatie-id met zijn classificatie. |
| [Sessielevenscyclus](concepts/session-lifecycle.md) | `SessionState`-overgangen, wat `StartSessionAsync` belooft, hoe een sessie eindigt. |
| [Transacties en recovery](concepts/transactions-and-recovery.md) | Journalstatussen, back-ups, herstelverificatie, sessieverwijderingen, crashrecovery. |
| [Model van de permanente plugin](concepts/permanent-plugin.md) | De plugin-closure `plugins\OmsiLaunch.*`, integriteit op basis van het manifest, wat een sessie nooit aanraakt. |
| [Local control / IPC](reference/local-control.md) | Named-pipe-protocol `0.1`, endpoint per installatie, `session_id`-binding, vertrouwensmodel. |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | De Windows-host (zonder console): verschillen met `OmsiLaunch.exe`, `/silent`, dialoogvensters, exitcodes. |
| [Windows-systeemvak](reference/windows-tray.md) | Indicator in het systeemvak (meldingsgebied): pictogram, menu, statusvenster veld voor veld, sessie beëindigen, herstart van Verkenner (Explorer). |
| [Foutreferentie](reference/errors.md) | Elke `OL_E_*`- / `OL_W_*`-code met categorie en betekenis. |
| [Exitcodes](reference/exit-codes.md) | `PublicExitCode`-waarden 0 tot en met 10 en shimcodes 100 tot en met 106 van de bootstrapper. |
| [Packaging / installatie-indeling](reference/packaging.md) | Bestanden in de release-ZIP, `release-manifest.json`, indeling van `.omsilaunch\`. |
| [Compatibiliteit / ondersteunde OMSI-builds](reference/compatibility.md) | De ene ondersteunde `Omsi.exe`-hash, de geaccepteerde Steam-LAA-hash, platformvereisten. |
| [Bekende beperkingen](reference/known-limitations.md) | Wat in deze bèta niet ondersteund, gedeeltelijk of een geaccepteerd risico is. |
| [Runtimevalidatiestatus](status/runtime-validation-status.md) | Wat onder OMSI is uitgevoerd, wat alleen offline is uitgevoerd, wat nog een echte sessie nodig heeft. |

Pagina's die voor beheerders normatief blijven: de
[`README.md`](../../../README.md) van de repository, de specificaties onder `docs/specs`
([`PUBLIC-API.md`](../../specs/PUBLIC-API.md),
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md),
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md),
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)) en de projectpagina's onder
`docs/project` ([`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md),
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md),
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)). Zij vatten samen; de
pagina's hierboven vormen de gedetailleerde referentie. Historische pagina's staan in het
[documentatiemanifest](../../DOCUMENTATION-MANIFEST.md).

<a id="how-this-documentation-is-kept-in-sync"></a>
## Hoe deze documentatie synchroon wordt gehouden

Een documentatiegate, `tests\OmsiLaunch.DocumentationTests`, compileert tegen
`OmsiLaunch.Api` en `OmsiLaunch.Core` en vergelijkt de pagina's hierboven met de
code die het publieke oppervlak definieert:

| Gate | Controleert |
| --- | --- |
| `docs.cli-flags` | Elk item van `CliInput.KnownFlags` staat in de CLI-referentie als `` `/flag` `` of `` `/flag:` ``; elk item van `CliInput.AcceptedNoEffectFlags` is op zijn regel gemarkeerd als `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`; elk opdrachtwoord en elke route van `CliInput.HierarchicalRoutes` staat erin met zijn runtime-operatie; elke `PublicExitCode`-waarde heeft een rij `| n |` in de exitcodetabel. |
| `docs.capabilities` | Elke id van `PublicCapabilityRegistry.All`, elk item van `PublicCapabilityRegistry.PublicRuntimeOperationIds` en elke naam van `PublicCapabilityClassification` staat in de capabilityreferentie. |
| `docs.errors` | Elke code van `PublicErrorCodes.All` staat in de foutreferentie, en geen enkele `OL_E_*`- / `OL_W_*`-literal in `src\` of `tools\OmsiLaunch.Cli\` ontbreekt in `PublicErrorCodes`. |
| `docs.public-api` | Elk geëxporteerd type van `OmsiLaunch.Api`, elke enumwaarde en elke member van `IOmsiLaunch` staat in de referentie van de publieke API, en alle vijf stabiliteitsbegrippen worden gebruikt. |
| `docs.launchspec` | Elke publieke eigenschap die vanuit `LaunchSpec` bereikbaar is en elke waarde van de bijbehorende enums staat in de LaunchSpec-referentie. |
| `docs.session-profiles` | Elke sleutel van `SessionProfileCompiler.SchemaKeys`, de schema-identifier en de limiet van `256 KiB` staan in de referentie van de sessieprofielen. |
| `docs.structure` | Elke pagina in de navigatietabel bestaat. |
| `docs.links` | Elke relatieve link in `docs\**\*.md` (met uitzondering van `docs\localized\`) en in de `*.md`-bestanden in de hoofdmap verwijst naar een bestaand bestand of een bestaande map. |
| `docs.localization` | Elke locale die in `docs\localized\LOCALIZATION-MANIFEST.md` staat, heeft elke pagina van de gelokaliseerde set; elke pagina behoudt de koppen, tabellen en codeblokken van de Engelse pagina, elke inline-codespan (vlaggen, capability- en operatie-id's, foutcodes, sleutels, identifiers) en elke link, en haar relatieve links zijn geldig. |

De gate is een van de suites die door `tools\Invoke-OfflineValidation.ps1` worden uitgevoerd
(overslaan met `-SkipDocs`). Hij draait offline, start OMSI nooit en laat de
build mislukken als een vlag, route, capability, foutcode, enumwaarde of publiek type
ongedocumenteerd is of als een link niet werkt. Hij controleert geen lopende tekst, dus een pagina kan
het gedrag nog steeds verkeerd beschrijven; meld dat als een bug in die pagina.

<a id="translations"></a>
## Vertalingen

`docs\localized\<locale>\` bevat volledige vertalingen van deze `0.1.0-beta4`-documentatie
voor `pt-BR`, `pt-PT`, `en-GB`, `fr-FR`, `de-DE`, `es-ES`, `es-LATAM`, `it-IT`, `pl-PL`, `nl-NL`, `ru-RU`, `zh-CN`, `zh-TW` en `ja-JP`. De paginaset, de locale-hoofdmappen en de
pagina's die bewust niet worden vertaald, staan in
[`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md).
Vertalingen laten elke opdracht, vlag, identifier, foutcode en elk voorbeeld van
de Engelse pagina's ongewijzigd, en de gate `docs.localization` controleert dat.
De Engelse pagina's blijven de normatieve bron: waar een vertaling ervan afwijkt,
zijn de Engelse pagina en de code gezaghebbend, en is de vertaling
een bug.
