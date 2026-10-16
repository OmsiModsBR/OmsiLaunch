<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>Controllo delle sessioni per OMSI 2.</strong></p>
<p align="center">Open source · Programmabile · Guidato dalla community</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <strong>Italiano</strong> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> Questa è una traduzione del [README canonico in inglese (US)](../../README.md). In caso di discrepanze, fa fede il README in inglese (US).

# OmsiLaunch

**OmsiLaunch** è un livello open source per avviare OMSI 2 in modo programmatico,
gestire le sessioni e controllare la simulazione in esecuzione. Pianifica una sessione
a partire da una descrizione dichiarativa, applica ogni modifica temporanea della
configurazione all'interno di una transazione registrata nel journal, avvia OMSI, lo
osserva fino all'inizio del gameplay, consente agli strumenti di leggere e modificare la
simulazione in esecuzione tramite un'API pubblica e ripristina ogni file che ha
modificato quando la sessione termina.

È un'infrastruttura per launcher, strumenti, automazione e integrazioni della
community. Non è un launcher grafico.

> **Definire la sessione, non i clic.**

## Stato: 0.1.0-beta3

La beta pubblica attuale è **`0.1.0-beta3`**. La Beta 3 è la base successiva al
consolidamento (hardening). La maggior parte delle sue funzionalità è `RUNTIME_VALIDATED`:
sono state osservate in sessioni OMSI reali, incluso il ciclo di chiusura runtime del
2026-09-23. Alcune restano `STATICALLY_VALIDATED` (solo test offline), `PARTIAL` o `UNAVAILABLE`.
Due voci runtime sono ancora aperte perché non possono essere prodotte in modo sicuro: il
gameplay con Steam LAA e la rimozione naturale di veicoli stradali e human (RV-002). La pagina
[stato della validazione a runtime](../localized/it-IT/status/runtime-validation-status.md) è
il riferimento autorevole di ciò che è stato eseguito sotto OMSI e di ciò che è stato eseguito solo offline.

Si tratta di una beta: l'API pubblica, la CLI e i formati dei file sono contrassegnati come `STABLE_BETA`,
`EXPERIMENTAL`, `PARTIAL`, `INTERNAL` o `UNAVAILABLE` per singolo membro e possono ancora
cambiare prima della 1.0.

## Ambito OMSI supportato

OmsiLaunch supporta esattamente un build di OMSI 2 e rifiuta di avviare qualsiasi build che
non riconosce.

| Voce | Ambito |
| --- | --- |
| Build di OMSI | Profilo `Omsi23004_692EBFBF`: `Omsi.exe` con SHA-256 `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243` (OMSI 2.3.004). `STABLE_BETA`; ogni validazione a runtime è stata eseguita su questo file. |
| Eseguibile Steam LAA | Lo SHA-256 `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759` è accettato tramite allow-list. Sono stati validati solo l'impronta e la pianificazione; il gameplay **non** è stato validato a runtime. `PARTIAL`. |
| Build sconosciuti | Rifiutati con `OL_E_UNSUPPORTED_BUILD`. L'hash viene ricontrollato a ogni pianificazione e a ogni avvio. |
| Sistema operativo | Windows 10 o successivo, x64. |
| Runtime | .NET 6 Desktop Runtime **x64** (controller) e .NET 6 Runtime **x86** (il plugin viene eseguito all'interno di `Omsi.exe` a 32 bit). |

Dettagli: [Compatibilità](../localized/it-IT/reference/compatibility.md) e
[Installazione](../localized/it-IT/getting-started/installation.md).

## Cosa offre

**Sessioni.** Una sessione viene pianificata a partire da una `LaunchSpec` (flag della CLI, un file JSON,
un profilo di sessione o l'API), validata senza effetti collaterali (`/plan`), quindi
avviata, osservata tramite le transizioni di `SessionState` e terminata. L'arresto di una
sessione termina OMSI in modo forzato, così OMSI non può sovrascrivere i file che stanno
per essere ripristinati. Vedere [Ciclo di vita della sessione](../localized/it-IT/concepts/session-lifecycle.md).

**Transazioni e recupero.** Ogni override della configurazione è limitato alla
sessione. OmsiLaunch crea uno snapshot, registra nel journal, applica, verifica e ripristina ogni
file che modifica, anche dopo un crash (`/recovery-status`, `/recover`,
`RecoverPendingAsync`). Non offre alcuna modifica permanente della configurazione. Vedere
[Transazioni e recupero](../localized/it-IT/concepts/transactions-and-recovery.md).

**CLI (`OmsiLaunch.exe`).** Un frontend di riferimento basato sulla stessa API pubblica, senza
alcuna logica OMSI propria: rilevamento, pianificazione, avvio di sessioni e comandi
client verso una sessione in esecuzione. Vedere il [riferimento della CLI](../localized/it-IT/reference/cli.md)
e gli [esempi della CLI](../localized/it-IT/reference/cli-examples.md).

**`OmsiLaunchW.exe`.** L'host del sottosistema Windows per i collegamenti. Accetta la
stessa riga di comando senza finestra di console e segnala gli errori in finestre di messaggio.
Vedere [OmsiLaunchW.exe](../localized/it-IT/reference/omsilaunchw.md).

**Area di notifica di Windows.** Ogni sessione owner mostra un'icona nell'area di notifica con una
finestra di stato e un'azione «End session». L'azione segue lo stesso percorso di arresto
di `session stop`. Vedere [Area di notifica di Windows](../localized/it-IT/reference/windows-tray.md).

**Profili di sessione.** Pacchetti dichiarativi `profile.yaml`
(`omsilaunch.session-profile/v1`) in
`.omsilaunch\session-profiles\<id>\`, affinché gli autori di contenuti possano distribuire
sessioni riproducibili che si avviano con un solo comando. Vedere
[Profili di sessione](../localized/it-IT/reference/session-profiles.md).

**API pubblica e controllo runtime.** `OmsiLaunch.Api` (`IOmsiLaunch`) è la
superficie di prodotto preferita. Le operazioni runtime come ora, meteo, mappa,
telecamera, orario, veicoli, human, variabili di script e texture D3D sono
limitate alla sessione, validate rispetto al profilo del build e indirizzate tramite handle
semantici opachi, mai tramite puntatori nativi. I risultati sono limitati da uno slot runtime
di 64 KiB. Vedere il [riferimento dell'API pubblica](../localized/it-IT/reference/public-api.md) e
[Controllo runtime](../localized/it-IT/reference/runtime-control.md).

**Controllo locale.** Una named pipe per installazione, legata al `session_id`
attivo, consente ad altri processi dello stesso utente di leggere stato ed eventi, arrestare la
sessione ed eseguire operazioni runtime pubbliche. Vedere
[Controllo locale / IPC](../localized/it-IT/reference/local-control.md).

**Capability.** Ogni capability (funzionalità) e ogni operazione runtime pubblica è catalogata
con la relativa stabilità. Le capability sperimentali e non disponibili sono elencate, non
nascoste (`OmsiLaunch.exe capabilities`). Vedere
[Capability](../localized/it-IT/reference/capabilities.md).

**Plugin permanente.** La chiusura del plugin in-process viene installata una sola volta in
`plugins\OmsiLaunch.*`. Prima di ogni avvio viene verificata rispetto alle voci SHA-256
di `release-manifest.json`. I plugin di terze parti non vengono mai toccati. Vedere
[Modello del plugin permanente](../localized/it-IT/concepts/permanent-plugin.md).

## Avvio rapido

Estrarre il pacchetto di rilascio nella radice dell'installazione di OMSI 2, quindi eseguire i
seguenti comandi da quella directory:

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Mentre la sessione è in esecuzione, una seconda console nella stessa directory può interrogarla o
terminarla:

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

Guida passo passo: [Prima sessione](../localized/it-IT/getting-started/first-session.md). Per gli integratori
.NET: [Avvio rapido con l'API pubblica](../localized/it-IT/getting-started/api-quick-start.md).

## Documentazione

L'intera documentazione è indicizzata in [`docs/localized/it-IT/README.md`](../localized/it-IT/README.md).
Le pagine in inglese (US) in `docs/` sono canoniche e normative.

| Argomento | Pagina |
| --- | --- |
| API pubblica | [docs/localized/it-IT/reference/public-api.md](../localized/it-IT/reference/public-api.md) |
| CLI | [docs/localized/it-IT/reference/cli.md](../localized/it-IT/reference/cli.md) |
| Esempi della CLI | [docs/localized/it-IT/reference/cli-examples.md](../localized/it-IT/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/it-IT/reference/omsilaunchw.md](../localized/it-IT/reference/omsilaunchw.md) |
| Area di notifica di Windows | [docs/localized/it-IT/reference/windows-tray.md](../localized/it-IT/reference/windows-tray.md) |
| Profili di sessione | [docs/localized/it-IT/reference/session-profiles.md](../localized/it-IT/reference/session-profiles.md) |
| Controllo runtime | [docs/localized/it-IT/reference/runtime-control.md](../localized/it-IT/reference/runtime-control.md) |
| Controllo locale / IPC | [docs/localized/it-IT/reference/local-control.md](../localized/it-IT/reference/local-control.md) |
| Capability | [docs/localized/it-IT/reference/capabilities.md](../localized/it-IT/reference/capabilities.md) |
| Errori e codici di uscita | [docs/localized/it-IT/reference/errors.md](../localized/it-IT/reference/errors.md), [docs/localized/it-IT/reference/exit-codes.md](../localized/it-IT/reference/exit-codes.md) |
| Pacchetti | [docs/localized/it-IT/reference/packaging.md](../localized/it-IT/reference/packaging.md) |
| Limitazioni note | [docs/localized/it-IT/reference/known-limitations.md](../localized/it-IT/reference/known-limitations.md) |
| Stato della validazione a runtime | [docs/localized/it-IT/status/runtime-validation-status.md](../localized/it-IT/status/runtime-validation-status.md) |

### Documentazione in altre lingue

La documentazione è tradotta in 14 locale in
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md). Le traduzioni
sono state prodotte a partire dalle pagine canoniche in inglese (US) e validate meccanicamente
rispetto a esse. La revisione editoriale da parte di madrelingua non ha fatto parte del rilascio
della Beta 3 e potrà seguire dopo la pubblicazione. Quando una traduzione e la pagina inglese
non concordano, fa fede la pagina inglese.

## Limitazioni e voci aperte

Queste sono le limitazioni più importanti. L'elenco completo si trova in
[Limitazioni note](../localized/it-IT/reference/known-limitations.md).

- **Un solo build di OMSI.** Steam LAA è `PARTIAL`: gameplay, letture, comandi e arresto
  richiedono un'installazione Steam autentica e non sono stati validati a runtime.
- **Non disponibile in questa beta:** l'avvio dall'ultimo stato della mappa (`/last`),
  data, ora, anno o meteo espliciti all'avvio e l'assegnazione del veicolo del giocatore
  all'avvio. La richiesta di uno qualsiasi di questi rende il piano non eseguibile anziché essere
  ignorata silenziosamente.
- **Le scritture runtime sono limitate.** `weather.set`, le scritture del calendario, le scritture
  delle variabili stringa e il riposizionamento dei veicoli non sono disponibili. Le modifiche runtime non sono
  registrate nel journal e non vengono ripristinate.
- **Durata degli handle.** Il rilevamento degli handle obsoleti per veicoli stradali e human che
  scompaiono naturalmente (RV-002) non ha un produttore runtime sicuro ed è validato
  solo offline.
- **Risultati limitati.** Gli elenchi lunghi vengono troncati (`truncated=true`). Non è prevista
  la paginazione.
- **L'arresto è forzato.** La procedura di chiusura propria di OMSI non viene eseguita e lo stato di OMSI non salvato
  va perso.
- **Modello di fiducia basato sullo stesso utente.** Qualsiasi processo dello stesso utente Windows può raggiungere il
  piano di controllo locale.

## Download

Scaricare **`OmsiLaunch-0.1.0-beta3.zip`** e il relativo file `.sha256` dalla
[pagina Releases](https://github.com/lmonteirotech/OmsiLaunch/releases). Estrarlo
direttamente nella radice di OMSI supportata. Il pacchetto contiene il controller
(`OmsiLaunch.exe`, `OmsiLaunchW.exe`), le sue dipendenze, la chiusura del plugin permanente
con `release-manifest.json`, le risorse dello splash screen, un esempio di sessione e la
documentazione offline in `.omsilaunch\docs\`. Struttura del pacchetto e rimozione
pulita: [Pacchetti](../localized/it-IT/reference/packaging.md).

## Compilazione dai sorgenti

Requisiti:

- Windows 10 o successivo, x64.
- .NET 6 SDK, con i runtime .NET 6 x64 e x86 per l'esecuzione dei test.
- Visual Studio con il workload MSBuild C++ (set di strumenti della piattaforma `v145`) e un
  Windows 10 SDK, per i tre progetti nativi: `OmsiLaunch.Native.x86`
  (Win32), e `OmsiLaunch.Bootstrapper` e `OmsiLaunch.WindowsHost` (x64).

`OmsiLaunch.sln` contiene i progetti gestiti e le suite di test. L'unico punto di
ingresso offline compila tutto ed esegue ogni suite offline; non
avvia mai OMSI:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` salta i progetti nativi e il test di regressione del packaging.
`-SkipDocs` salta i gate della documentazione. L'output della compilazione va in `artifacts\`,
che non è tracciato. `tools\New-ReleasePackage.ps1` prepara, calcola gli hash, valida
e comprime un pacchetto di rilascio a partire da una compilazione esistente. Vedere
[Pacchetti](../localized/it-IT/reference/packaging.md) e
[Test e validazione](../project/TESTING-AND-VALIDATION.md).

| Percorso | Contenuto |
| --- | --- |
| `src/` | Librerie del prodotto: API, core, configurazione, contenuti, interop, processi, plugin, profilo del build, boundary nativo x86 |
| `tools/` | CLI e host Windows (`OmsiLaunch.Cli`), shim nativi (`OmsiLaunch.Bootstrapper`), host di test offline, script di packaging e validazione, strumenti di localizzazione |
| `tests/` | Suite di test unitari, di integrazione, dei profili, dell'interfaccia Windows e della documentazione |
| `docs/` | Documentazione canonica e relative traduzioni in `docs/localized/` |
| `examples/` | Esempi di LaunchSpec e di sessione |
| `assets/` | Branding, icone e risorse del pacchetto |
| `third_party/` | Note sulla provenienza upstream |

Riepiloghi per i maintainer: [PUBLIC-API.md](../specs/PUBLIC-API.md),
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md),
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md),
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md),
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md),
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md).

## Community e licenza

OmsiLaunch è un progetto open source orientato alla community. È indipendente dagli
altri launcher di OMSI e gli strumenti compatibili della community possono basarsi su di esso.

OmsiLaunch è distribuito con licenza [LGPL-3.0-only](../../LICENSE). Vedere le
[note di terze parti](../../THIRD-PARTY-NOTICES.md) per la provenienza del
codice sorgente incorporato e le note applicabili.
