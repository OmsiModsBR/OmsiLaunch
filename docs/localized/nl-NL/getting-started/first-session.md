# Eerste sessie

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../getting-started/first-session.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina doorloopt de eerste beheerde OMSI-sessie met OmsiLaunch `0.1.0-beta4`: plannen zonder OMSI te starten, starten met expliciete vlaggen, starten met een vooraf gedefinieerd sessieprofiel, de sessie besturen en stoppen, en daarna de diagnosegegevens vinden. Er wordt van uitgegaan dat het pakket is geïnstalleerd zoals beschreven in [installatie](installation.md). Elke vlag is gespecificeerd in de [CLI-referentie](../reference/cli.md); meer aanroepen staan in [CLI-voorbeelden](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## Wat een sessie doet

Een sessie is een transactie rond één OMSI-proces: OmsiLaunch maakt een snapshot van de bestanden die het gaat aanraken (standaard de twee bitmaps van het opstartscherm onder `GUI\`, plus `options.cfg` als er `/set`-overlays worden gevraagd), schrijft een duurzaam journal onder `.omsilaunch\`, past de overlays toe, start `Omsi.exe` met de permanente plugin, wacht tot de gameplay is bereikt (`Running`), houdt de sessie bestuurbaar, en beëindigt aan het eind OMSI en herstelt elk aangeraakt bestand byte voor byte. `/new` kiest nooit stilzwijgend een kaart of een instappunt: beide moeten worden opgegeven, of uit een `/spec`-bestand of een sessieprofiel komen.

<a id="1-plan-nothing-is-started"></a>
## 1. Plannen (er wordt niets gestart)

Voer uit vanuit de OMSI-hoofdmap; de installatie is standaard de map die `OmsiLaunch.exe` bevat.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

Het plan moet `"IsRunnable": true` vermelden (exit `0`). Het bevat `TouchedFiles` en `PlannedMutations`, zodat je precies ziet wat de sessie met een overlay zal vervangen. Los elke `OL_E_`-diagnose op voordat je verdergaat; er is nog niets geschreven.

De meegeleverde voorbeeldspec doet hetzelfde met een bestand:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Starten met expliciete vlaggen

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Wat er gebeurt, in volgorde:

1. Het plan wordt afgedrukt (`Plan: READY profile=Omsi23004` op de bekende indeling).
2. Recovery van een eventueel ouder openstaand journal, verkrijgen van de lease, snapshot, journal, overlays, integriteitscontrole van de plugin, start van `Omsi.exe`.
3. Het systeemvakpictogram verschijnt (`OmsiLaunch is running`); zie [Windows-systeemvak](../reference/windows-tray.md).
4. Zodra de gameplay is bereikt, wordt de status `Running` als JSON afgedrukt (`"State": 14`). De standaard opstart-timeout is 180 s (wijzig deze met `/startup-timeout:<1..600>`).
5. De console blijft gekoppeld totdat de sessie eindigt. Sluit het consolevenster niet om te stoppen: gebruik een van de stopmethoden hieronder.

Optionele toevoegingen voor de eerste keer:

- `/set:graphics.maxFPS=60` (een overlay van `options.cfg`, die aan het eind wordt hersteld);
- `/splash:Unset` om het opstartscherm van OMSI ongemoeid te laten, of `/splash-language:DEU` om het gelokaliseerde beheerde opstartscherm te kiezen;
- `/observe-seconds:30` om automatisch te stoppen 30 s na `Running` (handig voor een smoketest);
- `--json` voor gestructureerde uitvoer.

Vlaggen die een datum, tijd, jaar, weer of spelersvoertuig vragen (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) worden geaccepteerd, maar kunnen door deze build niet worden toegepast: het plan wordt `NOT RUNNABLE` met `OL_E_CAPABILITY_UNAVAILABLE`. Laat ze weg.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Starten met een vooraf gedefinieerd sessieprofiel

Een sessieprofiel is een YAML-bestand onder `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` dat de kaart, het instappunt en maximaal 99 presets met instellingen vastlegt (schema `omsilaunch.session-profile/v1`; volledige referentie in [sessieprofielen](../reference/session-profiles.md)). Maak `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml` aan:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

Daarna:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Regels om te onthouden: de `id` moet gelijk zijn aan de mapnaam; de index is `1..99`; expliciete vlaggen die een veld zouden overschrijven dat het profiel beheert (`/map`, `/entrypoint-index`, een `/set`-sleutel die de preset beheert, vlaggen voor het opstartscherm als de preset `presentation` heeft) worden geweigerd met `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (exit `2`); het blok `new:` geldt alleen met `/new`; met `/saved:<file.osn>` moet de kaart van de situatie onder `compatibility.maps` staan. Het meegeleverde `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` illustreert het volledige schema, maar het blok `new:` daarin stelt `date`, `time` en `weather` in, die deze build niet kan toepassen; kopieer het dus pas nadat je die sleutels hebt verwijderd.

<a id="4-control-the-running-session"></a>
## 4. De lopende sessie besturen

Vanuit een tweede console in dezelfde map (zonder installatieargument):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Deze opdrachten gaan via de lokale besturingspipe van deze installatie ([local control](../reference/local-control.md)); exit `4` betekent dat hier geen eigenaar draait.

<a id="5-stop"></a>
## 5. Stoppen

Elk van deze methoden beëindigt de sessie op dezelfde manier (OMSI wordt beëindigd, daarna wordt elk aangeraakt bestand hersteld, daarna worden het journal en de back-up verwijderd):

| Methode | Opmerkingen |
|---|---|
| Systeemvakpictogram → `End session` → bevestigen | Beschikbaar in sessies van zowel `OmsiLaunch.exe` als `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | Vanuit een andere console; keert direct terug, de eigenaar voltooit het herstel. |
| Ctrl+C in de console van de eigenaar | Vraagt de stop aan; de eigenaar wacht op het herstel voordat hij afsluit. |
| `/observe-seconds:<n>` | Automatische stop `n` seconden na `Running`. |
| OMSI sluit zelf af | De eigenaar detecteert `ProcessExited` en herstelt. |

De eigen afsluitroutine van OMSI wordt niet uitgevoerd, dus OMSI herschrijft `options.cfg` niet bij het afsluiten; dat is bewust zo, zodat het herstel exact is. Als je het consolevenster van de eigenaar met de X-knop sluit, krijgt het herstel maar 4 s; als het dan niet klaar is, voltooit de volgende start (of `OmsiLaunch.exe /recover`) het vanuit het journal. De exitcode van de eigenaar is `0` als de sessie in `Completed` is geëindigd.

<a id="6-where-to-look-afterwards"></a>
## 6. Waar je daarna kijkt

| Locatie | Inhoud |
|---|---|
| Console / `--json`-uitvoer | Plan, status `Running`, eindstatus (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | De host-trace van de sessie (transactiegrenzen, processtart, plugin-handoff, gameplay bereikt, herstel). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Resultaat van een `/runtime:`-operatie die door de eigenaar is uitgevoerd. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Gebeurtenissen van de systeemvakindicator. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` na een schone afsluiting. `true` betekent dat er een journal is achtergebleven; voer `OmsiLaunch.exe /recover` uit. |

Als de sessie de gameplay niet heeft bereikt, bevat de eindstatus de falende `OL_E_`-diagnose (bijvoorbeeld `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), is de exitcode `1` en zijn de bestanden toch hersteld. Zie [exitcodes](../reference/exit-codes.md), [fouten](../reference/errors.md) en [bekende beperkingen](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Uitvoeren zonder console

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delegeert naar `OmsiLaunchW.exe` en geeft direct `0` terug. De sessie heeft geen console; fouten verschijnen als berichtvensters en het systeemvakpictogram is het enige zichtbare element. Gebruik `session status`, `events watch` en de diagnostiekmap om de sessie te volgen. Het volledige gedrag van de Windows-host staat in de [referentie van OmsiLaunchW.exe](../reference/omsilaunchw.md).
