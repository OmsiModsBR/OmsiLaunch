# Gestione HOF

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Traduzione della [pagina originale in inglese](../../../reference/hof-management.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

OMSI legge i file HOF (`*.hof`: destinazioni, numeri di linea ed elenchi di fermate mostrati dal display IBIS/delle destinazioni del veicolo) dalla cartella di ciascun veicolo. La gestione HOF copia i file HOF nelle cartelle dei veicoli **per la durata di una sessione**: una mappa o un add-on può distribuire i file HOF di cui le sue linee hanno bisogno, e un utente può riutilizzare i file HOF di un veicolo su altri, senza modificare permanentemente alcun veicolo. Questa pagina è il riferimento normativo, secondo l'implementazione di `HofPlanner` in `src/OmsiLaunch.Core/HofPlanner.cs`, del campo `HofManagement` del [LaunchSpec](launchspec.md), dei flag `/hof-source`, `/hof-target`, `/hof-file` e `/hof-mode` della [CLI](cli.md) e del blocco `hofs` dei [profili di sessione](session-profiles.md).

Stabilità: `STABLE_BETA` per la pianificazione, le regole sui conflitti, il confinamento e il ripristino transazionale (gate offline `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Validata a runtime: una sessione con mirror con ripristino esatto e recupero dopo un crash tramite `/recover` (vedere [stato della validazione a runtime](../status/runtime-validation-status.md)).

La gestione HOF non coincide con `PlayerVehicle.Hof` / `/hof:<id>`, che seleziona il file HOF del veicolo del giocatore all'avvio ed è `UNAVAILABLE` in questo build.

<a id="model"></a>
## Modello

Una richiesta è un elenco di operazioni (`HofManagementSpec.Operations`). Ogni `HofOperationSpec` ha:

| Campo | Significato |
| --- | --- |
| `SourceKind`, `Source` | Da dove provengono i file HOF (tabella seguente). |
| `TargetVehicles` | Uno o più veicoli installati: l'identità di un file `.bus` o `.ovh` (`Vehicles\MAN_SD200\MAN_SD77.bus`) oppure di una cartella di veicolo (`Vehicles\MAN_SD200`), entrambe sotto `Vehicles\`. Più identità nella stessa cartella indirizzano quella cartella una sola volta. |
| `Mode` | `Add` (predefinito): copia i file HOF della sorgente in ogni cartella di destinazione; gli altri file HOF presenti restano. `Mirror`: copia i file HOF della sorgente e rimuove per la sessione ogni altro `.hof` della cartella di destinazione, in modo che l'insieme dei file HOF della cartella sia uguale a quello della sorgente. |
| `Files` | Elenco facoltativo di nomi di file da prelevare dalla sorgente (senza distinzione tra maiuscole e minuscole). Omesso: ogni `*.hof` presente direttamente nella sorgente. Un nome elencato che la sorgente non contiene: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Si risolve in |
| --- | --- | --- |
| `Collection` | un nome semplice, per esempio `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: una collezione mantenuta dall'utente o da uno strumento. |
| `Directory` | una directory relativa all'installazione, per esempio `maps\RMG Leste\hof` | i `*.hof` in quella directory: file HOF distribuiti accanto al contenuto di una mappa o di un add-on. |
| `Vehicle` | un'identità o una cartella di veicolo, come per le destinazioni | i `*.hof` nella cartella di quel veicolo: riutilizzo dei file HOF di un veicolo su altri. |
| `File` | un file `.hof` relativo all'installazione | quell'unico file. |

Vengono usati solo i file presenti direttamente nella directory sorgente (nessuna ricorsione). I collegamenti simbolici e le junction non vengono mai seguiti (vedere [confinamento](#confinement)).

<a id="what-a-session-does"></a>
## Che cosa fa una sessione

1. **Pianificazione** (`PlanSessionAsync`, `/plan`). Ogni operazione viene risolta: le sorgenti vengono lette, le destinazioni controllate, i conflitti rilevati. Ogni file che cambierà compare in `SessionPlan.PlannedMutations` con `SemanticKey` `hof.add` o `hof.mirror`, `RequestedValue` = l'identità della sorgente e `Operation` `exact-file-overlay` (scritto) o `session-deletion` (rimosso da un mirror); i percorsi fanno parte di `TouchedFiles`. Un problema rende il piano non eseguibile con i codici riportati sotto.
2. **Avvio**. Dopo il recupero di un journal obsoleto, le operazioni vengono risolte di nuovo a partire dai file attivi e consegnate alla transazione della sessione: i file scritti sono overlay, i file eliminati da un mirror sono eliminazioni di sessione. Un file di destinazione i cui byte sono già uguali alla sorgente viene lasciato com'è e non è una voce della transazione.
3. **Fine della sessione, errore, crash.** La transazione ripristina esattamente ogni cartella di destinazione: i file esistenti riottengono i byte, i timestamp e gli attributi originali; i file creati dalla sessione vengono rimossi; i file eliminati da un mirror tornano al loro posto. Dopo un crash dell'owner lo stesso ripristino viene eseguito al successivo avvio o tramite `/recover` (vedere [transazioni e recupero](../concepts/transactions-and-recovery.md)).

Non viene scritto nulla al di fuori delle cartelle di destinazione. I file sorgente vengono solo letti. `.omsilaunch\hof\` non viene mai modificato da una sessione.

<a id="conflict-rules"></a>
## Regole sui conflitti

Le operazioni vengono applicate in ordine e devono essere concordi:

- Due operazioni che scrivono lo stesso file di destinazione con gli stessi byte vengono unite; con byte diversi: `OL_E_HOF_CONFLICT`.
- Un mirror che rimuoverebbe un file scritto da un'operazione precedente, oppure un'operazione che scrive un file rimosso da un mirror precedente: `OL_E_HOF_CONFLICT`.
- Un mirror copre l'insieme dei file HOF di sessione della cartella: i file esistenti più i file che le operazioni precedenti vi scrivono.
- Una destinazione HOF che è scritta anche dalla presentazione della sessione (splash o Internet Textures): `OL_E_HOF_CONFLICT` (non può accadere con i percorsi di presentazione attuali, che sono al di fuori di `Vehicles\`).
- Quando una destinazione ha già un file con lo stesso nome in maiuscole e minuscole diverse, viene mantenuta la grafia esistente.

<a id="confinement"></a>
## Confinamento

Ogni sorgente e ogni destinazione devono restare all'interno dell'installazione: percorsi radicati, segmenti `..` e percorsi che escono dalla radice sono `OL_E_HOF_PATH_ESCAPE`; lo sono anche le destinazioni (e le sorgenti `Vehicle`) al di fuori di `Vehicles\`, i nomi di collezione che non sono un semplice nome di directory e qualsiasi cartella sorgente o di destinazione raggiunta attraverso una junction o un collegamento simbolico. I file `.hof` che sono essi stessi reparse point vengono ignorati.

<a id="limits"></a>
## Limiti

| Limite | Valore | Errore |
| --- | --- | --- |
| Dimensione di un file HOF | 8 MiB | `OL_E_HOF_INVALID` |
| File per operazione | 256 | `OL_E_HOF_INVALID` |
| Byte totali scritti per sessione | 256 MiB | `OL_E_HOF_INVALID` |
| Destinazioni per operazione | almeno 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Codici di errore

| Codice | Causa |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | La collezione, la directory, il veicolo o il file non esiste, non contiene alcun `.hof` oppure manca un file indicato in `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Un file o una cartella di veicolo di destinazione non esiste. |
| `OL_E_HOF_PATH_ESCAPE` | Un percorso esce dall'installazione, una destinazione non è sotto `Vehicles\`, il nome di una collezione non è un nome semplice, oppure viene attraversata una junction/symlink. |
| `OL_E_HOF_CONFLICT` | Due operazioni sono in disaccordo su un file di destinazione (vedere [regole sui conflitti](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Nessuna destinazione, nessuna sorgente, una sorgente `File` che non è `.hof`, una destinazione che è un file diverso da `.bus`/`.ovh`, oppure un [limite](#limits) superato. |

<a id="using-it"></a>
## Utilizzo

Riga di comando (una sola operazione; più operazioni richiedono `/spec` o un profilo di sessione):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (JSON di `/spec`, enum come interi):

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

Preset di un profilo di sessione (una sorgente `package` è una directory all'interno del pacchetto del profilo):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: impostare `LaunchSpec.HofManagement` e chiamare `PlanSessionAsync` / `StartSessionAsync` come di consueto; `/list:Hofs` (`ContentQueryKind.Hofs`) elenca i file HOF installati.

Correlati: [LaunchSpec](launchspec.md), [profili di sessione](session-profiles.md), [CLI](cli.md), [errori](errors.md), [transazioni e recupero](../concepts/transactions-and-recovery.md).
