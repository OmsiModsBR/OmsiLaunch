# Installazione

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> Traduzione della [pagina originale in inglese](../../../getting-started/installation.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina spiega che cosa richiede OmsiLaunch `0.1.0-beta4`, quale build di OMSI supporta, come si installa il pacchetto di rilascio nella radice di un'installazione di OMSI e come verificare l'installazione con `/version` e `/plan` prima di avviare una sessione. Il contenuto del pacchetto è specificato in [pacchettizzazione](../reference/packaging.md); il primo avvio è descritto in [prima sessione](first-session.md).

<a id="requirements"></a>
## Requisiti

| Requisito | Dettaglio | Errore in caso di mancanza |
|---|---|---|
| Windows 10 o successivo, 64 bit | Il controller verifica `Environment.OSVersion.Version.Major >= 10`, un sistema operativo x64 e un processo x64. | Piano non eseguibile con `OL_E_UNSUPPORTED_OPERATING_SYSTEM` (o `OL_E_UNSUPPORTED_OS_ARCHITECTURE`), uscita `1`/`3`. |
| .NET 6 Desktop Runtime, **x64** | `OmsiLaunch.Controller.runtimeconfig.json` richiede `Microsoft.NETCore.App` 6.0 e `Microsoft.WindowsDesktop.App` 6.0 (Windows Forms è usato dall'indicatore nell'area di notifica). Gli shim lo individuano tramite `nethost.dll`. | `OmsiLaunch.exe` termina con il codice shim `102`..`106` prima di qualsiasi output; `OmsiLaunchW.exe` mostra `OmsiLaunch could not start the .NET host (code N).` |
| .NET 6 Runtime, **x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` richiede `Microsoft.NETCore.App` 6.0 per x86, perché il plugin viene eseguito all'interno di `Omsi.exe` a 32 bit. Anche il pacchetto .NET 6 Desktop Runtime x86 soddisfa il requisito. | Il plugin non si avvia all'interno di OMSI; la sessione non raggiunge `Running` (`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`), uscita `1`, file ripristinati. |
| OMSI 2 installato | Un `Omsi.exe` a 32 bit con una versione di OMSI 2 (2.x) e le cartelle `maps\` e `Vehicles\` nella radice. Non è richiesto alcun hash particolare dell'eseguibile; OMSI 2.3.004 è il layout noto e validato a runtime, mentre sugli altri build ogni capability viene risolta e verificata nel processo. | `OL_E_UNSUPPORTED_BUILD`; piano non eseguibile, uscita `1`. Una capability non disponibile sul build fallisce con `OL_E_CAPABILITY_UNRESOLVED`. Vedere [compatibilità](../reference/compatibility.md). |
| Radice dell'installazione scrivibile | La transazione scrive `.omsilaunch\`, gli overlay sotto `GUI\`, `Texture\` e `options.cfg` e li ripristina; la radice deve essere scrivibile dall'utente corrente (evitare `Program Files` senza le autorizzazioni appropriate). | `OL_E_INSTALLATION_NOT_WRITABLE`, uscita `1`. |
| Un utente, un owner per installazione | Il lease dell'installazione (blocco esclusivo) `Local\OmsiLaunch.Installation.<sha256(root)>` e la pipe di controllo sono per sessione di accesso (logon). | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`, uscita `7`. |

Entrambi i runtime si scaricano separatamente da Microsoft; installare il Desktop Runtime x64 e il Runtime x86 (o il Desktop Runtime x86) per .NET 6. Non è richiesto nessun altro componente. Nessun dato lascia il computer.

<a id="confirm-the-omsi-build"></a>
## Confermare il build di OMSI

Sostituire `<OMSI_PATH>` con la directory di OMSI 2 (per esempio `C:\OMSI 2`).

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

L'hash deve essere uno dei due elencati sopra. Dopo l'installazione, `OmsiLaunch.exe profiles` stampa lo stesso elenco con il relativo stato di validazione.

<a id="install-the-package"></a>
## Installare il pacchetto

1. Scaricare `OmsiLaunch-0.1.0-beta4.zip` e `OmsiLaunch-0.1.0-beta4.zip.sha256`; verificare il checksum (`Get-FileHash` deve essere uguale al valore contenuto nel file `.sha256`).
2. Estrarre l'archivio **direttamente nella radice dell'installazione di OMSI** (la cartella che contiene `Omsi.exe`). L'archivio è strutturato per tale radice:
   - `OmsiLaunch.exe`, `OmsiLaunchW.exe`, `nethost.dll`, `OmsiLaunch.Controller.dll` e gli altri assembly del controller `OmsiLaunch.*.dll`, `YamlDotNet.dll`, `release-manifest.json`, `LICENSE`, `THIRD-PARTY-NOTICES.md` nella radice;
   - la chiusura del plugin permanente `plugins\OmsiLaunch.*` (9 file) accanto ai plugin esistenti, che non vengono mai toccati;
   - `.omsilaunch\` con le risorse della splash screen, la documentazione offline e gli esempi.
3. Mantenere `release-manifest.json` accanto a `OmsiLaunch.exe`. È ciò che consente a ogni avvio di verificare i file del plugin installati tramite SHA-256 (`plugin.integrity.reference = manifest`); senza di esso vengono verificate solo la presenza e la coerenza interna (`plugin.integrity.reference = self`).
4. Non spostare né rinominare nulla sotto `plugins\OmsiLaunch.*` e non collocare binari del plugin in `.omsilaunch\`.

L'aggiornamento è la stessa operazione: estrarre il nuovo pacchetto sopra i vecchi file mentre nessuna sessione è in esecuzione e nessun recupero è in sospeso (`OmsiLaunch.exe /recovery-status`). Gli hash del plugin e il manifest devono provenire sempre dallo stesso pacchetto (altrimenti `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`).

<a id="verify"></a>
## Verificare

Eseguire dalla radice di OMSI (l'argomento dell'installazione ha come valore predefinito la directory che contiene `OmsiLaunch.exe`):

```text
OmsiLaunch.exe /version
```
Risultato atteso: `"version": "0.1.0-beta4"`, `"protocol_version": "0.1"`, `"supported_family": "OMSI_2"`; uscita `0`. L'uscita `102`..`106` indica che il runtime .NET 6 x64 manca o che il pacchetto è incompleto.

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
Risultato atteso: gli hash supportati, poi le mappe individuate in questa installazione; uscita `0`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Risultato atteso: `Plan: READY profile=Omsi23004` (sul layout noto; altrimenti `unrecognized`) e uscita `0` (usare una qualsiasi identità di mappa da `/list:Maps`; l'indice del punto di ingresso deve essere un indice presentato di quella mappa, vedere `/list:Entrypoints /map:<identity>`). Con `--json` il piano elenca `TouchedFiles` (gli overlay della splash screen), `PlannedMutations`, `RequiredCapabilities` (`STATICALLY_VALIDATED` e, per le capability native di avvio sul layout noto, `RESOLVED_KNOWN_LAYOUT`), `Diagnostics` (incluso `plugin.integrity.reference`) e `IsRunnable`. `Plan: NOT RUNNABLE` con uscita `1` indica il motivo in `Diagnostics` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_RUNTIME_ARTIFACT_MISSING`, ...). La pianificazione non avvia mai OMSI e non scrive mai nell'installazione.

<a id="where-things-live-afterwards"></a>
## Dove si trovano i file in seguito

| Percorso | Contenuto |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | Traccia dell'host di ogni sessione (vengono conservate le 50 sessioni più recenti) |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Log dell'indicatore nell'area di notifica |
| `<root>\.omsilaunch\journal.json`, `backup\<sessionId>\` | Presenti solo mentre una transazione è in sospeso; vedere [transazioni e recupero](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | I profili di sessione predefiniti dall'utente; vedere [profili di sessione](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | Risorse della splash screen gestite |
| `<root>\.omsilaunch\docs\` | Questa documentazione, offline (iniziare da `README.md`; il riferimento CLI è `reference\cli.md`) |
| `<root>\.omsilaunch\examples\` | LaunchSpec e profilo di sessione di esempio |

<a id="uninstall"></a>
## Disinstallazione

Arrestare qualsiasi sessione, eseguire `OmsiLaunch.exe /recovery-status` (e `/recover` se c'è un recupero in sospeso), quindi eliminare i file del prodotto nella radice, `plugins\OmsiLaunch.*` e `.omsilaunch\`. Dettagli in [pacchettizzazione](../reference/packaging.md).

<a id="next"></a>
## Passi successivi

[Prima sessione](first-session.md) · [Riferimento CLI](../reference/cli.md) · [limitazioni note](../reference/known-limitations.md)
