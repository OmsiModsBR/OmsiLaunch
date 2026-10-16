# HOF-beheer

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/hof-management.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

OMSI leest de HOF-bestanden (`*.hof`: bestemmingen, lijnnummers en haltelijsten die door het IBIS/de bestemmingsaanduiding van het voertuig worden getoond) uit de map van elk voertuig. HOF-beheer kopieert HOF-bestanden **voor de duur van één sessie** naar voertuigmappen: een kaart of add-on kan de HOF's meeleveren die haar lijnen nodig hebben, en een gebruiker kan de HOF's van één voertuig voor andere voertuigen hergebruiken, zonder dat een voertuig permanent wordt gewijzigd. Deze pagina is de normatieve referentie, zoals geïmplementeerd door `HofPlanner` in `src/OmsiLaunch.Core/HofPlanner.cs`, het veld `HofManagement` van de [LaunchSpec](launchspec.md), de [CLI](cli.md)-vlaggen `/hof-source`, `/hof-target`, `/hof-file` en `/hof-mode` en het blok `hofs` van [sessieprofielen](session-profiles.md).

Stabiliteit: `STABLE_BETA` voor planning, conflictregels, afbakening en transactioneel herstel (offline gates `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Runtime-gevalideerd: een mirror-sessie met exact herstel en crashrecovery via `/recover` (zie [status van de runtimevalidatie](../status/runtime-validation-status.md)).

HOF-beheer is niet hetzelfde als `PlayerVehicle.Hof` / `/hof:<id>`, dat bij de start de HOF van het spelersvoertuig selecteert en in deze build `UNAVAILABLE` is.

## Model

Een aanvraag is een lijst met operaties (`HofManagementSpec.Operations`). Elke `HofOperationSpec` heeft:

| Veld | Betekenis |
| --- | --- |
| `SourceKind`, `Source` | Waar de HOF-bestanden vandaan komen (tabel hieronder). |
| `TargetVehicles` | Een of meer geïnstalleerde voertuigen: de identiteit van een `.bus`- of `.ovh`-bestand (`Vehicles\MAN_SD200\MAN_SD77.bus`) of een voertuigmap (`Vehicles\MAN_SD200`), beide onder `Vehicles\`. Meerdere identiteiten in één map adresseren die map één keer. |
| `Mode` | `Add` (standaard): de bron-HOF's worden naar elke doelmap gekopieerd; andere HOF's daar blijven staan. `Mirror`: de bron-HOF's worden gekopieerd en elk ander `.hof` van de doelmap wordt voor de sessie verwijderd, zodat de HOF-set van de map gelijk is aan de bronset. |
| `Files` | Optionele lijst met bestandsnamen die uit de bron worden genomen (niet hoofdlettergevoelig). Weggelaten: elk `*.hof` direct in de bron. Een vermelde naam die de bron niet bevat: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Wordt omgezet naar |
| --- | --- | --- |
| `Collection` | een gewone naam, bijvoorbeeld `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: een collectie die door de gebruiker of een tool wordt bijgehouden. |
| `Directory` | een map relatief aan de installatie, bijvoorbeeld `maps\RMG Leste\hof` | `*.hof` in die map: HOF's die naast kaart- of add-on-content worden meegeleverd. |
| `Vehicle` | een voertuigidentiteit of -map, zoals bij doelen | `*.hof` in de map van dat voertuig: de HOF's van één voertuig voor andere hergebruiken. |
| `File` | een `.hof`-bestand relatief aan de installatie | precies dat ene bestand. |

Alleen bestanden direct in de bronmap worden gebruikt (geen recursie). Symbolische koppelingen en junctions worden nooit gevolgd (zie [afbakening](#confinement)).

<a id="what-a-session-does"></a>
## Wat een sessie doet

1. **Planning** (`PlanSessionAsync`, `/plan`). Elke operatie wordt omgezet: bronnen worden gelezen, doelen worden gecontroleerd, conflicten worden gedetecteerd. Elk bestand dat zal veranderen, verschijnt in `SessionPlan.PlannedMutations` met `SemanticKey` `hof.add` of `hof.mirror`, `RequestedValue` = de bronidentiteit, en `Operation` `exact-file-overlay` (geschreven) of `session-deletion` (door een mirror verwijderd); de paden maken deel uit van `TouchedFiles`. Een probleem maakt het plan niet uitvoerbaar met de onderstaande codes.
2. **Start**. Nadat een verouderd journal is hersteld, worden de operaties opnieuw omgezet op basis van de actuele bestanden en aan de sessietransactie overgedragen: geschreven bestanden zijn overlays, door een mirror verwijderde bestanden zijn sessieverwijderingen. Een doelbestand waarvan de bytes al gelijk zijn aan de bron, blijft ongemoeid en is geen transactie-item.
3. **Einde van de sessie, fout, crash.** De transactie herstelt elke doelmap exact: bestanden die bestonden, krijgen hun oorspronkelijke bytes, tijdstempels en kenmerken terug; bestanden die de sessie heeft gemaakt, worden verwijderd; door een mirror verwijderde bestanden komen terug. Na een crash van de eigenaar wordt hetzelfde herstel uitgevoerd bij de volgende start of via `/recover` (zie [transacties en herstel](../concepts/transactions-and-recovery.md)).

Buiten de doelmappen wordt niets geschreven. Bronbestanden worden alleen gelezen. `.omsilaunch\hof\` wordt nooit door een sessie gewijzigd.

<a id="conflict-rules"></a>
## Conflictregels

Operaties worden op volgorde toegepast en moeten met elkaar overeenstemmen:

- Twee operaties die hetzelfde doelbestand met dezelfde bytes schrijven, worden samengevoegd; met verschillende bytes: `OL_E_HOF_CONFLICT`.
- Een mirror die een bestand zou verwijderen dat een eerdere operatie schrijft, of een operatie die een bestand schrijft dat een eerdere mirror verwijdert: `OL_E_HOF_CONFLICT`.
- Een mirror omvat de sessie-HOF-set van de map: de bestanden die bestaan plus de bestanden die eerdere operaties daar schrijven.
- Een HOF-doel dat ook door de sessiepresentatie (opstartscherm of internettextures) wordt geschreven: `OL_E_HOF_CONFLICT` (kan met de huidige presentatiepaden, die buiten `Vehicles\` liggen, niet voorkomen).
- Als een doel al een bestand met dezelfde naam in een andere hoofdlettervorm heeft, blijft de bestaande schrijfwijze behouden.

<a id="confinement"></a>
## Afbakening

Elke bron en elk doel moet binnen de installatie blijven: gewortelde paden, `..`-segmenten en paden die de hoofdmap verlaten, geven `OL_E_HOF_PATH_ESCAPE`; hetzelfde geldt voor doelen (en `Vehicle`-bronnen) buiten `Vehicles\`, collectienamen die geen gewone mapnaam zijn, en elke bron- of doelmap die via een junction of symbolische koppeling wordt bereikt. `.hof`-bestanden die zelf reparsepunten zijn, worden genegeerd.

<a id="limits"></a>
## Limieten

| Limiet | Waarde | Fout |
| --- | --- | --- |
| Grootte van één HOF-bestand | 8 MiB | `OL_E_HOF_INVALID` |
| Bestanden per operatie | 256 | `OL_E_HOF_INVALID` |
| Totaal aantal geschreven bytes per sessie | 256 MiB | `OL_E_HOF_INVALID` |
| Doelen per operatie | ten minste 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Foutcodes

| Code | Oorzaak |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | De collectie, map, het voertuig of bestand bestaat niet, bevat geen `.hof` of mist een bestand dat in `Files` wordt genoemd. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Een doelvoertuigbestand of -map bestaat niet. |
| `OL_E_HOF_PATH_ESCAPE` | Een pad verlaat de installatie, een doel ligt niet onder `Vehicles\`, een collectienaam is geen gewone naam, of er wordt een junction/symlink doorlopen. |
| `OL_E_HOF_CONFLICT` | Twee operaties zijn het oneens over één doelbestand (zie [conflictregels](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Geen doelen, geen bron, een `File`-bron die geen `.hof` is, een doel dat een ander bestand is dan `.bus`/`.ovh`, of een overschreden [limiet](#limits). |

<a id="using-it"></a>
## Gebruik

Opdrachtregel (één operatie; voor meerdere operaties zijn `/spec` of een sessieprofiel nodig):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (`/spec`-JSON, enums als gehele getallen):

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

Preset van een sessieprofiel (een `package`-bron is een map binnen het profielpakket):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: stel `LaunchSpec.HofManagement` in en roep zoals gewoonlijk `PlanSessionAsync` / `StartSessionAsync` aan; `/list:Hofs` (`ContentQueryKind.Hofs`) toont de geïnstalleerde HOF-bestanden.

Zie ook: [LaunchSpec](launchspec.md), [sessieprofielen](session-profiles.md), [CLI](cli.md), [fouten](errors.md), [transacties en herstel](../concepts/transactions-and-recovery.md).
