# Exemplos da CLI

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> Tradução da [página original em inglês](../../../reference/cli-examples.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Invocações mínimas e corretas de `OmsiLaunch.exe` para o OmsiLaunch `0.1.0-beta4`, cada uma com o código de saída esperado do processo e uma nota sobre o que é alterado e restaurado. Salvo indicação em contrário, todos os exemplos são executados a partir da raiz da instalação do OMSI (`<OMSI_PATH>`); a sintaxe está definida na [referência da CLI](cli.md) e os códigos de saída em [códigos de saída](exit-codes.md). É possível acrescentar `--json` a qualquer comando para obter o envelope estruturado.

<a id="conventions"></a>
## Convenções

- **Altera**: ficheiros ou estado do OMSI modificados pelo comando. "overlay da sessão" significa um ficheiro capturado em snapshot na transação, aplicado antes de o OMSI arrancar e restaurado byte a byte quando a sessão termina.
- **Restaurado**: o que é desfeito quando a sessão termina (paragem normal, Ctrl+C, área de notificação, `session stop`, `/observe-seconds`) ou pela recuperação.
- As escritas de runtime (`time set`, `camera set`, `scripts variable set`, `vehicles spawn`) alteram apenas a memória do OMSI; nunca são revertidas, porque o OMSI é terminado na paragem.
- Marcadores de posição: `<OMSI_PATH>` é a instalação do OMSI 2 que contém o pacote OmsiLaunch (por exemplo `C:\OMSI 2`); `<OTHER_OMSI_PATH>` é outra instalação; `<SPEC_PATH>` e `<ITX_PATH>` são um ficheiro LaunchSpec e um perfil de Internet Textures do próprio utilizador; `<HANDLE>` é um handle impresso pelo comando `list` ou `create` anterior e `<BASE64>` são dados de píxeis em Base64. Todos os outros valores são literais que funcionam numa instalação normal do OMSI 2 (`grundorf-quick` é o perfil de exemplo definido nesta página).
- Colocar entre aspas um caminho que contenha espaços e não terminar um caminho entre aspas com `\` (a interpretação de argumentos do Windows transforma `\"` numa aspa literal): `"C:\OMSI 2"`, e não `"C:\OMSI 2\"`.
- Todas as linhas de comando desta página são analisadas pelo gate de documentação (`tests/OmsiLaunch.DocumentationTests`, gate `examples`); os exemplos de identidade, descoberta, planeamento e cliente foram também executados numa instalação real (`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`).

<a id="identity-and-discovery-no-session"></a>
## Identidade e descoberta (sem sessão)

```text
OmsiLaunch.exe /version
```
Saída `0`. Imprime `product`, `version` (`0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family`. Não altera nada.

```text
OmsiLaunch.exe profiles --json
```
Saída `0`. Lista os hashes de `Omsi.exe` suportados e o respetivo estado de validação. Não altera nada.

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
Saída `0`. Catálogo público de capacidades; `help <family>` filtra-o. Não altera nada.

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
Saída `0` (as duas formas são idênticas). Indica os processos `Omsi.exe` em execução e se um proprietário OmsiLaunch responde por esta instalação. Não altera nada.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
Saída `0` (`2` para uma categoria desconhecida). Descoberta só de leitura; os ciclos de junctions são ignorados. Não altera nada. Os valores `Identity` aqui impressos são as strings exatas que `/map`, `/saved`, `/vehicle-scope` e um `LaunchSpec` esperam (por exemplo `maps\Grundorf\global.cfg`, `situations\Linie 5.osn`).

<a id="planning-and-validation"></a>
## Planeamento e validação

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
Saída `0` quando o plano está `READY`, `1` quando está `NOT RUNNABLE` (por exemplo `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`). Não altera nada; o OMSI não é iniciado.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
Saída `0`/`1` como acima; `/validate` é um alias de `/plan`. O JSON é o `SessionPlan` em bruto (`TouchedFiles`, `PlannedMutations`, `Diagnostics`, `IsRunnable`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
Saída `1`. `/date`, `/time`, `/year`, `/weather*` e as flags do veículo do jogador são aceites, mas não aplicadas por esta build; o plano contém `OL_E_CAPABILITY_UNAVAILABLE` e não é executável.

```text
OmsiLaunch.exe /last /plan
```
Saída `1`. `LAST_MAP_STATE` está indisponível para este perfil (`OL_E_CAPABILITY_UNAVAILABLE`).

<a id="starting-sessions-owner-mode"></a>
## Iniciar sessões (modo proprietário)

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Saída `0` quando a sessão termina em `Completed`, `1` em `Failed` ou com um plano não executável. Altera: overlays da sessão `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_<lang>.bmp` (o splash gerido é a predefinição), o tratamento de `closecheck`, o handoff de arranque. Restaurado: todos os overlays, byte a byte, quando a sessão termina. A consola permanece associada até o OMSI terminar, até ser confirmado "End session" na área de notificação, até um cliente enviar `session stop` ou até ser premido Ctrl+C.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
Saída `0`. Igual ao anterior, mas a sessão é parada 8 s depois de atingir `Running` (antes, numa paragem pela área de notificação/pipe). Usado por scripts de validação.

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
Saída `0`/`1`. SAVED_SITUATION: o mapa, a hora e a posição vêm do `.osn` (`situations\Linie 5.osn` é fornecido com o OMSI 2 e começa em Berlin-Spandau com um autocarro do jogador). O valor é a identidade da situação impressa por `/list:Situations` (relativa à instalação, sem distinção de maiúsculas/minúsculas); um nome de ficheiro simples como `Linie 5.osn` não é resolvido (`OL_E_SITUATION_NOT_FOUND`, saída `1`). `/map` ou `/entrypoint-index` com `/saved` é rejeitado com saída `2`. Alterações e restauro como para NEW_MAP. O próprio OMSI regista o mapa da situação em `options.cfg` `[last_map]`; essa escrita do OMSI não é revertida, a menos que um `/set` sobreponha `options.cfg` (ver [transações e recuperação](../concepts/transactions-and-recovery.md)).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
Saída `0`. Altera: `options.cfg` (overlay da sessão, patch semântico de tokens/vetores; bytes CP1252 preservados) mais os overlays do splash. Restaurado: `options.cfg` e os ficheiros do splash, exatamente (RV-005, RV-006). `/set:graphics.texture=...` sai com `2` (`OL_E_SETTING_NOT_WRITABLE`); `/set:foo=1` sai com `2` (`OL_E_UNKNOWN_SETTING`).

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
Saída `0`. Altera: nenhum overlay do splash; apenas o handoff de arranque e o tratamento de `closecheck`. Restaurado: nada a restaurar quanto ao splash.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
Saída `0` (`1` com `OL_E_SESSION_PRESENTATION_INVALID` quando o diretório ou um BMP está em falta ou não tem 640x480 e 24 bits). Altera: `GUI\NewSplashscreen_ENG.bmp` e `GUI\NewSplashscreen_PTB.bmp` a partir do diretório personalizado (overlay da sessão). Restaurado: ambos os ficheiros, exatamente.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
Saída `0`. Altera: nada no disco além dos overlays do splash; o mecanismo de transferência dentro do processo é suprimido durante a sessão.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
Saída `0` (`2` com `OL_E_ITX_PROFILE_REQUIRED` se o perfil for omitido; `1` para um perfil inválido ou um destino fora de `Texture\`). Altera: `Texture\standard.itx` (overlay da sessão); todos os destinos listados no perfil e `Texture\standard.ipr` são eliminações da sessão. Restaurado: overlay removido, originais eliminados restaurados; os ficheiros que o OMSI criou nesses caminhos durante a sessão são removidos como subprodutos da sessão.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
Saída `0`. Espera até 300 s (+5 s) por `Running`, em vez dos 180 s predefinidos. `/shutdown-timeout:60` é aceite, mas não tem efeito nesta build.

<a id="predefined-session-profile"></a>
### Perfil de sessão predefinido

Ficheiro de perfil `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
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
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
Saída `0`. Altera: `options.cfg` (definições da predefinição, overlay da sessão) e os overlays do splash. Restaurado: todos eles. Acrescentar `/map:...` ou `/set:graphics.maxFPS=60` sai com `2` (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); omitir `/predefined-profile-index` sai com `2` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`). O exemplo incluído no pacote `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` mostra o esquema completo, mas, tal como é fornecido, o seu bloco `new:` pede `date`, `time` e `weather`, que esta build não consegue aplicar: planeá-lo com `/new` resulta em `NOT RUNNABLE` (`OL_E_CAPABILITY_UNAVAILABLE`); remover essas chaves antes de o utilizar.

<a id="launchspec-file"></a>
### Ficheiro LaunchSpec

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
Saída `0`/`1`. O exemplo incluído no pacote seleciona Grundorf, índice de ponto de entrada `1`, splash gerido e Internet Textures nativas; `RootPath: "."` é resolvido para o diretório do executável. Alterações como no exemplo NEW_MAP explícito. Uma especificação com uma propriedade desconhecida sai com `2` (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`); um ficheiro em falta sai com `6` (`OL_E_SPEC_NOT_FOUND`); um ficheiro acima de 1 MiB sai com `2` (`OL_E_SPEC_TOO_LARGE`).

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
Saída `0`/`1`. A instalação explícita `<OTHER_OMSI_PATH>` prevalece sobre o `RootPath` da especificação; `/startup-timeout` sobrepõe-se a `Behavior.StartupTimeoutSeconds` da especificação apenas porque foi indicado.

<a id="silent-detached-start"></a>
### Arranque silencioso (desanexado)

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
Saída `0` assim que `OmsiLaunchW.exe` tiver sido iniciado (`{"delegated": true, "host_process_id": <pid>}`); `7` se `OmsiLaunchW.exe` estiver em falta (`OL_E_WINDOWS_HOST_MISSING`) ou não tiver sido possível iniciá-lo. O lançador regressa imediatamente e não mantém abertos a consola nem os pipes do chamador: um script que capture a sua saída recebe fim de ficheiro de imediato (fecho de runtime `T04`). A própria sessão é executada em `OmsiLaunchW.exe`: sem saída de consola, falhas em caixas de mensagem, ícone da área de notificação disponível. Acompanhar o progresso com `session status`, `events watch` e `.omsilaunch\diagnostics\<sessionId>-host.log`. Referência completa: [OmsiLaunchW.exe](omsilaunchw.md).

<a id="controlling-a-running-session-client-mode"></a>
## Controlar uma sessão em execução (modo cliente)

Executar estes comandos a partir do mesmo diretório de instalação enquanto um proprietário está em execução. Cada um sai com `4` (`OL_E_NO_ACTIVE_SESSION`) quando nenhum proprietário responde e com `7` num erro de controlo.

```text
OmsiLaunch.exe session status --json
```
Saída `0`. Devolve `SessionId`, `State` (`14` = `Running`), `Diagnostics`, `RuntimeEvents`. Não altera nada.

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
Saída `0` (`events watch` é executado até Ctrl+C). Eventos de runtime limitados (`gameplay.entered`, eventos do ciclo de vida D3D, ...). Não altera nada.

```text
OmsiLaunch.exe session stop
```
Saída `0` (`{"accepted": true, "session_id": "..."}`). Pede a paragem canónica: o OMSI é terminado, os overlays são restaurados pelo proprietário e o journal é eliminado. O cliente regressa imediatamente; o processo proprietário termina após o restauro.

<a id="runtime-reads"></a>
## Leituras de runtime

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
Saída `0` com o `RuntimeCommandResult` (`Succeeded`, `Values`) no envelope. Não altera nada. Timeout de 8 s (`OL_E_RUNTIME_REQUEST_TIMEOUT`, saída `7`).

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
Saída `0`; `2` quando falta um argumento obrigatório (`OL_E_RUNTIME_ARGUMENT_REQUIRED`, comunicado antes de o pedido ser enviado); `7` quando o plugin rejeita o pedido: `OL_E_RUNTIME_OPERATION_FAILED` com o motivo específico em `Values.detail`, por exemplo `OL_E_RUNTIME_OBJECT_HANDLE_STALE` para um handle que já não identifica o mesmo objeto, ou `OL_E_RUNTIME_CONSTANT_NOT_FOUND`. Os handles têm âmbito da sessão e provêm do `list` anterior. Os nomes das variáveis, constantes e curvas são definidos por cada modelo de veículo: devem ser obtidos a partir do resultado de `list`. Os nomes acima foram listados para `rv-000001`, o autocarro do jogador de uma sessão `situations\Linie 5.osn`. Não altera nada.

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
Saída `0`. As operações sem rota hierárquica, ou qualquer rota, podem ser endereçadas pelo id da operação. `timetable.track-entries.list` é uma lista limitada: em `situations\Linie 5.osn` devolveu 137 de 825 entradas com `truncated=true` (novo teste de runtime da auditoria de documentação). Não altera nada.

<a id="runtime-writes"></a>
## Escritas de runtime

```text
OmsiLaunch.exe time set --minute=30
```
Saída `0`. Altera o relógio em memória do OMSI (validado: escrita, releitura, restauro através de um segundo `time set`). Não é revertido na paragem.

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
Saída `0` (`2` quando falta `--family` para `camera lock`). Altera o estado da câmara durante a sessão. `camera lock` precisa de um PlayerVehicle (por exemplo, uma sessão `/saved`); numa sessão `/new` sem veículo do jogador falha (`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` em `Values.detail`). O bloqueio e o desbloqueio foram validados em runtime com uma situação guardada (famílias 0, 2 e 1, com releitura da câmara). Não é revertido na paragem; a política de bloqueio termina com a sessão.

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
Saída `0` (`2` se faltar `handle`, `name` ou `value`). Altera uma variável de script numérica desse veículo. Não é revertido.

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
Saída `0`. Escreve o registo de meteorologia de origem do OMSI e pára as atualizações METAR em direto; o OMSI deriva daí o nevoeiro, a luz, o vento atual e a humidade no seu próximo ciclo meteorológico sem pausa. Cada valor é verificado antes de se escrever o que quer que seja (`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, saída `7`). Uma chave derivada, como `relative_humidity`, devolve `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` e indica a chave a definir em seu lugar. Não é revertido. EXPERIMENTAL: validação em runtime pendente.

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
Saída `0`. Passa para meteorologia METAR em direto para essa estação; o OMSI transfere-a na sua próxima atualização meteorológica. EXPERIMENTAL: validação em runtime pendente.

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
Saída `0`. `scripts string set` passa pelo próprio `SetStringVariable` do OMSI e devolve o valor lido de volta; um nome que o script do veículo não declare devolve `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` (saída `7`). `scripts trigger` aciona um trigger como o fazem os tratadores de entrada do OMSI (`1` premir, `0` largar); o script do veículo executa-o de imediato. Os nomes de triggers e de variáveis são definidos por cada modelo de veículo; os acima são exemplos. Não é revertido. EXPERIMENTAL: validação em runtime pendente.

## Spawn

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
Saída `0` com o novo handle `rv-NNNNNN` em `Values` (`2` quando falta `--model`; `7` em `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`). Timeout de 30 s no cliente. Altera a coleção de veículos rodoviários (é acrescentado um veículo); não atribui o veículo do jogador. Não é revertido; o veículo desaparece com o OMSI na paragem.

<a id="d3d-textures"></a>
## Texturas D3D

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` é o valor `handle` impresso por `create` (`d3dtex-<session id>-<16 hex digits>`). `<BASE64>` tem de ser descodificado para `width * height * 4` bytes nos formatos de 32 bits (8 x 8 x 4 = 256 bytes) e para no máximo 48 KiB. Saída `0`; `2` para argumentos obrigatórios em falta; `7` para `OL_E_D3D_INVALID_TEXTURE_FORMAT`, `OL_E_D3D_INVALID_PIXEL_BUFFER`, `OL_E_D3D_RESOURCE_RELEASED` (segunda libertação, ou describe após a libertação), `OL_E_D3D_STALE_RESOURCE_HANDLE` (um handle anterior a uma reposição do dispositivo, ou de outra sessão), `OL_E_D3D_RESET_IN_PROGRESS`, `OL_E_D3D_NOT_READY`, `OL_E_D3D_DEVICE_LOST`. Cria recursos de GPU pertencentes à sessão; são libertados explicitamente ou quando o OMSI termina. Nenhum ficheiro é tocado.

<a id="owner-side-single-runtime-operation"></a>
## Operação de runtime única do lado do proprietário

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
Saída `0`. Inicia uma sessão, executa `time.read` uma vez após `Running` (timeout de 5 s), escreve `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`, continua em execução durante 5 s, para e restaura. Uma falha de runtime é impressa como `runtime_error` e não termina a sessão.

<a id="recovery"></a>
## Recuperação

```text
OmsiLaunch.exe /recovery-status --json
```
Saída `0`: `{"pending": false, ...}` quando não existe journal, `{"pending": true, "recovered": false}` quando existe. Saída `7` (`OL_E_INSTALLATION_BUSY`) enquanto um proprietário detiver a instalação. Não altera nada.

```text
OmsiLaunch.exe /recover --json
```
Saída `0` quando nada estava pendente ou o restauro foi concluído (`recovered: true`; `diagnostics` pode conter `restore.session-artifact-removed` e `OL_W_RESTORE_FOREIGN_FILE_RETAINED`); saída `8` quando o journal estava pendente e continua pendente (`OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`); saída `7` enquanto um proprietário ou o OMSI registado no journal detiver a instalação (`OL_E_INSTALLATION_BUSY`); saída `10` (`OL_E_INTERNAL`) quando os bytes restaurados falham a verificação (`Restore hash mismatch` ou `Restore presence mismatch`; o journal permanece pendente). Altera: restaura todos os ficheiros registados no journal a partir de `.omsilaunch\backup\<sessionId>\` depois de verificar o respetivo SHA-256 e, em seguida, elimina o journal e o diretório de cópias de segurança. Recusado com `OL_E_INSTALLATION_BUSY` enquanto o `Omsi.exe` registado no journal ainda estiver ativo (fecho de runtime `S04`, `S04b`, `F01`).

<a id="exit-code-quick-check-powershell"></a>
## Verificação rápida do código de saída (PowerShell)

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
