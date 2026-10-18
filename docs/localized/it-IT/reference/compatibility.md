# Compatibilità

<!-- l10n: source=reference/compatibility.md source-sha256=9f3a7654d57410845e20c92eccc746868cbac665b4d937581f16c3818d7b7b88 -->
> Traduzione della [pagina originale in inglese](../../../reference/compatibility.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

OmsiLaunch funziona con un **OMSI 2** installato. Non richiede un particolare file `Omsi.exe`: ogni presupposto nativo che compie all'interno di OMSI (una funzione, un punto di patch, una classe, uno slot VMT, una variabile globale, un offset di campo) viene risolto e verificato singolarmente, nel processo, prima di essere usato, e una capability è disponibile solo quando tutti i suoi presupposti sono verificati. Questa pagina indica che cosa significa «OMSI 2 installato», come vengono identificati i build, come vengono risolte le capability e quali sono i requisiti di sistema operativo e di runtime. Fonti: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Requisito: OMSI 2 installato

Un piano viene rifiutato con `OL_E_UNSUPPORTED_BUILD` solo quando l'installazione non è un'installazione di OMSI 2:

| Controllo | Rifiutato quando |
| --- | --- |
| `Omsi.exe` | manca dalla radice dell'installazione |
| Immagine | non è un'immagine PE x86 a 32 bit |
| Versione | la sua risorsa di versione non riporta una versione di OMSI 2 (2.x) |
| Installazione | la radice non ha una directory `maps\` o `Vehicles\` |

L'SHA-256, la dimensione e la versione esatta dell'eseguibile **non** sono criteri di compatibilità. Vengono registrati come diagnostica (`omsi.build.detected`, `OmsiLaunch.exe profiles`) e servono come evidenza.

<a id="build-identity-and-capability-resolution"></a>
## Identità del build e risoluzione delle capability

| Concetto | Significato |
| --- | --- |
| Identità del codice | SHA-256 della sezione di codice principale dell'eseguibile. Un'identità nota seleziona un layout noto come percorso rapido. Le varianti che differiscono solo nelle intestazioni (per esempio una copia Large Address Aware) condividono l'identità del codice. |
| Layout noto | `Omsi23004`: OMSI 2.3.004 (versione del file 2.2.032), identità del codice `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Ogni validazione a runtime della [pagina di stato](../status/runtime-validation-status.md) è stata eseguita su questo layout. |
| Target nativo | Un presupposto del confine nativo, dichiarato nel catalogo dei build con la sua guardia. |
| Capability | Un gruppo di target, per esempio `world.new-map` o `time.read`. Disponibile solo quando ogni target è risolto **e** verificato. |

Come viene risolto un target, nel plugin, prima che OmsiLaunch applichi qualsiasi patch:

1. **Identità nota:** viene provato per primo l'indirizzo di riferimento del layout noto, che deve comunque superare la guardia del target.
2. **Altrimenti, oppure quando la guardia fallisce**, vengono provate strategie indipendenti dal build:
   - nome di classe RTTI di Delphi (con `vmtSelfPtr` controllato);
   - metodi, campi e proprietà published di una classe (tabelle RTTI);
   - slot VMT che contiene un metodo risolto;
   - una firma di codice mascherata che deve corrispondere esattamente una volta (gli operandi che dipendono dal layout sono caratteri jolly), acquisendo un indirizzo, una variabile globale (direttamente o tramite la sua cella puntatore) o uno spostamento di campo dall'interno di un metodo della classe del campo stesso.
3. **Nulla corrisponde in modo univoco e supera la guardia:** il target resta non risolto. **Non esiste alcun ripiego su un indirizzo fisso.**

Guardie: byte iniziali mascherati per funzioni e punti di patch, la destinazione della chiamata per una chiamata sottoposta a patch, `vmtSelfPtr` e il nome di classe RTTI per le classi, il metodo contenuto per gli slot VMT e una sezione dati scrivibile per le variabili globali. I layout degli oggetti letti dai lettori runtime (veicolo, human, meteo, record dell'orario e così via) non hanno ancora una validazione strutturale indipendente dal build. Vengono verificati solo per un'identità del codice nota, quindi le capability che li leggono non sono disponibili su un build con identità sconosciuta.

Sul layout noto ogni capability viene risolta. Su un build con identità sconosciuta, le capability i cui target si risolvono tramite le strategie sopra descritte (l'avvio headless, NEW_MAP, le situazioni salvate, la soppressione di Internet Textures, la lettura e l'impostazione dell'ora, D3D) possono essere disponibili, e ciascuna viene verificata singolarmente; le letture che dipendono dal layout non sono disponibili.

<a id="where-compatibility-is-decided"></a>
## Dove viene decisa la compatibilità

| Fase | Che cosa accade |
| --- | --- |
| Pianificazione (`PlanSessionAsync`, `/plan`) | Controllo di OMSI 2 (`omsi.omsi2`, altrimenti `OL_E_UNSUPPORTED_BUILD`). L'immagine del file viene risolta come **previsione**: le capability native richieste riportano `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` oppure `PENDING_IN_PROCESS_RESOLUTION` con l'avviso `OL_W_CAPABILITY_PENDING`. Un'identità del codice sconosciuta aggiunge l'avviso `OL_W_BUILD_UNRECOGNIZED`. Gli avvisi non rendono un piano non eseguibile. |
| Avvio (`StartSessionAsync`) | La specifica viene ripianificata; un piano che non è più eseguibile viene rifiutato con `OL_E_PLAN_NOT_RUNNABLE`. |
| Nel processo (avvio del plugin) | L'immagine caricata viene risolta (fa fede; un eseguibile protetto da DRM può differire dal proprio file). Il rapporto viene inviato come telemetria `native.capabilities`. Se `boot.headless-start` e la capability della modalità mondo (`world.new-map` o `world.saved-situation`) non sono disponibili, la sessione fallisce con `OL_E_CAPABILITY_UNRESOLVED` prima di qualsiasi hook e i file vengono ripristinati. Se sono disponibili ma il form Start attivo non si trova nello stato atteso (per esempio un altro plugin lo ha modificato con patch), la sessione fallisce con `OL_E_BUILD_VALIDATION_FAILED`. |
| Operazioni runtime | Un'operazione la cui capability non è disponibile viene rifiutata con `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) senza toccare la memoria di OMSI. |

`OmsiLaunch.exe profiles --json` riporta l'installazione (`is_omsi2`, `build_id`, `identity`, hash del file e del codice, `file_variant`), i layout noti e le capability previste a partire dal file.

<a id="operating-system-and-architecture"></a>
## Sistema operativo e architettura

`CurrentWindowsX64Platform.Detect` calcola `RuntimePlatformInfo`. La piattaforma corrente è supportata solo quando sono soddisfatte tutte le condizioni seguenti:

| Requisito | Verifica | Errore in caso di violazione |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 o successivo | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows a 64 bit e processo host a 64 bit | `OSArchitecture == X64` e `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Installazione scrivibile | La directory radice esiste, non è di sola lettura e contiene `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` riporta inoltre `OmsiArchitecture` e `PluginArchitecture` come `X86` (OMSI è un processo a 32 bit; la chiusura del plugin è x86 e viene eseguita sotto WOW64), `LegacyPlatform=false` e `Wow64Available`. Windows ARM64 non è supportato, anche dove esiste l'emulazione x64, perché il processo host deve essere esso stesso x64.

<a id="net-requirements"></a>
## Requisiti .NET

| Componente | Runtime | Note |
| --- | --- | --- |
| Controller (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | Il bootstrapper nativo individua il runtime tramite `hostfxr` attraverso il `nethost.dll` incluso nel pacchetto. Un runtime mancante viene segnalato dallo shim (codici di uscita 100-106; vedere [CLI](cli.md) e [codici di uscita](exit-codes.md)). |
| Chiusura del plugin (`plugins\OmsiLaunch.Plugin.dll` tramite `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), ospitato da DNNE 2.0.6 all'interno di `Omsi.exe` | Richiede che sul computer sia installato il runtime .NET 6 Desktop/Core x86; il solo runtime a 64 bit non è sufficiente per il plugin. |
| Bridge nativo (`plugins\OmsiLaunch.Native.x86.dll`) | nativo x86 | Caricato solo da `plugins\` (vedere [plugin permanente](../concepts/permanent-plugin.md)). Non contiene alcun indirizzo di OMSI: riceve dal plugin la tabella dei target verificati. |

<a id="legacy-platforms"></a>
## Piattaforme legacy

Windows 7, Windows 8.x, Windows XP e gli altri sistemi NT 6 e precedenti sono al di fuori dell'attuale perimetro di supporto. `RuntimePlatformInfo.LegacyPlatform` è sempre `false` e non esiste alcun adattatore legacy; il campo e il punto di separazione `IPluginNativeServices` esistono solo affinché un futuro adattatore legacy possa essere aggiunto senza modificare l'API pubblica (vedere `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nulla in questa release viene eseguito su tali sistemi.

<a id="steam-and-large-address-aware-notes"></a>
## Note su Steam e Large Address Aware

- Una copia Large Address Aware che differisce solo nelle intestazioni ha la stessa identità del codice dell'originale e usa lo stesso layout noto.
- L'eseguibile Steam è protetto da DRM su disco, quindi il suo file può differire dall'immagine caricata: il piano può riportare `OL_W_CAPABILITY_PENDING` e decide la risoluzione nel processo.
- Steam avvia OMSI autonomamente; una sessione deve essere avviata tramite `OmsiLaunch.exe` affinché esista l'handoff. Se OMSI viene avviato da Steam, il plugin permanente resta inerte (nessun handoff, nessun hook).

<a id="related-pages"></a>
## Pagine correlate

- [Limitazioni note](known-limitations.md)
- [Stato della validazione a runtime](../status/runtime-validation-status.md)
- [Installazione](../getting-started/installation.md)
