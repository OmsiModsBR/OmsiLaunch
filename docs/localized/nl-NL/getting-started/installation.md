# Installatie

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../getting-started/installation.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina legt uit wat OmsiLaunch `0.1.0-beta4` vereist, welke OMSI-build het ondersteunt, hoe het releasepakket in de installatiemap van een OMSI-installatie wordt geïnstalleerd en hoe je de installatie met `/version` en `/plan` controleert voordat je een sessie start. De inhoud van het pakket is beschreven in [packaging](../reference/packaging.md); de eerste start staat in [eerste sessie](first-session.md).

<a id="requirements"></a>
## Vereisten

| Vereiste | Details | Fout als deze ontbreekt |
|---|---|---|
| Windows 10 of later, 64-bits | De controller controleert `Environment.OSVersion.Version.Major >= 10`, een x64-besturingssysteem en een x64-proces. | Plan niet uitvoerbaar met `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (of `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), exit `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` vereist `Microsoft.NETCore.App` 6.0 en `Microsoft.WindowsDesktop.App` 6.0 (Windows Forms wordt gebruikt door de systeemvakindicator). De shims vinden de runtime via `nethost.dll`. | `OmsiLaunch.exe` sluit af met shimcode `102`..`106` voordat er uitvoer is; `OmsiLaunchW.exe` toont `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` vereist `Microsoft.NETCore.App` 6.0 voor x86, omdat de plugin binnen het 32-bits `Omsi.exe` draait. De x86-bundel van de .NET 6 Desktop Runtime voldoet ook. | De plugin start niet binnen OMSI; de sessie bereikt `Running` niet (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), exit `1`, bestanden hersteld. |
| OMSI 2 geïnstalleerd | Een 32-bits `Omsi.exe` met een OMSI 2-versie (2.x), en `maps\` en `Vehicles\` in de hoofdmap. Er is geen specifieke hash van het uitvoerbare bestand vereist; OMSI 2.3.004 is de bekende, runtime-gevalideerde indeling, en op andere builds wordt elke capability in het proces opgelost en geverifieerd. | `OL_E_UNSUPPORTED_BUILD`; plan niet uitvoerbaar, exit `1`. Een capability die op de build niet beschikbaar is, mislukt met `OL_E_CAPABILITY_UNRESOLVED`. Zie [compatibiliteit](../reference/compatibility.md). |
| Beschrijfbare installatiemap | De transactie schrijft `.omsilaunch\`, overlays onder `GUI\`, `Texture\` en `options.cfg` en herstelt ze; de huidige gebruiker moet schrijfrechten op de installatiemap hebben (vermijd `Program Files` zonder de juiste machtigingen). | `OL_E_INSTALLATION_NOT_WRITABLE`, exit `1`. |
| Eén gebruiker, één eigenaar per installatie | De installatielease `Local\OmsiLaunch.Installation.<sha256(root)>` en de besturingspipe gelden per aanmeldsessie. | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, exit `7`. |

Beide runtimes zijn afzonderlijke downloads van Microsoft; installeer voor .NET 6 de x64 Desktop Runtime en de x86 Runtime (of x86 Desktop Runtime). Er is geen ander onderdeel nodig. Er verlaten geen gegevens de computer.

<a id="confirm-the-omsi-build"></a>
## De OMSI-build controleren

Vervang `<OMSI_PATH>` door je OMSI 2-map (bijvoorbeeld `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

De hash moet een van de twee hierboven genoemde zijn. Na de installatie drukt `OmsiLaunch.exe profiles` dezelfde lijst af, met de validatiestatus.

<a id="install-the-package"></a>
## Het pakket installeren

1. Download `OmsiLaunch-0.1.0-beta4.zip` en `OmsiLaunch-0.1.0-beta4.zip.sha256`; controleer de checksum (`Get-FileHash` moet gelijk zijn aan de waarde in het `.sha256`-bestand).
2. Pak het archief **rechtstreeks uit in de installatiemap van OMSI** (de map die `Omsi.exe` bevat). Het archief is voor die map ingedeeld:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` en de andere `OmsiLaunch.*.dll`-controllerassembly's, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` in de hoofdmap;
   - de permanente plugin-closure `plugins\OmsiLaunch.*` (9 bestanden) naast je bestaande plugins, die nooit worden aangeraakt;
   - `.omsilaunch\` met de bestanden voor het opstartscherm, offline documentatie en voorbeelden.
3. Laat `release-manifest.json` naast `OmsiLaunch.exe` staan. Daarmee kan elke start de geïnstalleerde pluginbestanden via SHA-256 verifiëren (`plugin.integrity.reference = manifest`); zonder dit bestand worden alleen aanwezigheid en interne consistentie gecontroleerd (`plugin.integrity.reference = self`).
4. Verplaats of hernoem niets onder `plugins\OmsiLaunch.*` en plaats geen pluginbinaries in `.omsilaunch\`.

Upgraden is dezelfde handeling: pak het nieuwe pakket uit over de oude bestanden terwijl er geen sessie draait en er geen recovery openstaat (`OmsiLaunch.exe /recovery-status`). De pluginhashes en het manifest moeten altijd uit hetzelfde pakket komen (anders `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Controleren

Voer uit vanuit de OMSI-hoofdmap (het installatieargument is standaard de map die `OmsiLaunch.exe` bevat):

```text
OmsiLaunch.exe /version
```
Verwacht: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; exit `0`. Exit `102`..`106` betekent dat de x64-runtime van .NET 6 ontbreekt of dat het pakket onvolledig is.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Verwacht: de ondersteunde hashes, daarna de kaarten die in deze installatie zijn gevonden; exit `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Verwacht: `Plan: READY profile=Omsi23004` (op de bekende indeling; anders `unrecognized`) en exit `0` (gebruik een willekeurige kaartidentiteit uit `/list:Maps`; de instappuntindex moet een getoonde index van die kaart zijn, zie `/list:Entrypoints /map:<identity>`). Met `--json` bevat het plan `TouchedFiles` (de overlays van het opstartscherm), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED`, en `RESOLVED_KNOWN_LAYOUT` voor de native startcapabilities op de bekende indeling), `Diagnostics` (inclusief `plugin.integrity.reference`) en `IsRunnable`. `Plan: NOT RUNNABLE` met exit `1` noemt de reden in `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). Plannen start OMSI nooit en schrijft nooit naar de installatie.

<a id="where-things-live-afterwards"></a>
## Waar alles daarna staat

| Pad | Inhoud |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Host-trace van elke sessie (de 50 nieuwste sessies worden bewaard) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Log van de systeemvakindicator |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Alleen aanwezig zolang er een transactie openstaat; zie [transacties en recovery](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | Je vooraf gedefinieerde sessieprofielen; zie [sessieprofielen](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Bestanden voor het beheerde opstartscherm |
| `<root>\.omsilaunch\docs\` | Deze documentatie, offline (begin bij `README.md`; de CLI-referentie is `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | Voorbeeld van een LaunchSpec en een sessieprofiel |

<a id="uninstall"></a>
## Verwijderen

Stop een eventuele sessie, voer `OmsiLaunch.exe /recovery-status` uit (en `/recover` als er iets openstaat) en verwijder daarna de productbestanden in de hoofdmap, `plugins\OmsiLaunch.*` en `.omsilaunch\`. Details staan in [packaging](../reference/packaging.md).

<a id="next"></a>
## Volgende stap

[Eerste sessie](first-session.md) · [CLI-referentie](../reference/cli.md) · [bekende beperkingen](../reference/known-limitations.md)
