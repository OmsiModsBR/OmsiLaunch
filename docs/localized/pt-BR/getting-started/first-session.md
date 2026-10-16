# Primeira sessão

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Tradução da [página original em inglês](../../../getting-started/first-session.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página percorre a primeira sessão gerenciada do OMSI com o OmsiLaunch `0.1.0-beta4`: planejar sem iniciar o OMSI, iniciar com flags explícitas, iniciar com um perfil de sessão predefinido, controlar e parar a sessão e, depois, encontrar os diagnósticos. Ela pressupõe que o pacote está instalado conforme descrito em [instalação](installation.md). Todas as flags são especificadas na [referência da CLI](../reference/cli.md); mais invocações estão em [exemplos da CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## O que uma sessão faz

Uma sessão é uma transação em torno de um processo do OMSI: o OmsiLaunch faz um snapshot dos arquivos que vai tocar (por padrão, os dois bitmaps do splash em `GUI\`, além de `options.cfg` quando overlays `/set` são solicitados), grava um journal durável em `.omsilaunch\`, aplica os overlays, inicia o `Omsi.exe` com o plugin permanente, espera até que o gameplay seja iniciado (`Running`), mantém a sessão controlável e, no final, encerra o OMSI e restaura byte a byte todos os arquivos tocados. `/new` nunca seleciona um mapa ou um ponto de entrada silenciosamente: ambos precisam ser informados ou vir de um arquivo `/spec` ou de um perfil de sessão.

<a id="1-plan-nothing-is-started"></a>
## 1. Planejar (nada é iniciado)

Execute a partir da raiz do OMSI; a instalação tem como padrão o diretório que contém `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

O plano precisa indicar `"IsRunnable": true` (saída `0`). Ele lista `TouchedFiles` e `PlannedMutations`, para que você veja exatamente o que a sessão vai sobrepor. Corrija qualquer diagnóstico `OL_E_` antes de continuar; nada foi gravado.

A spec de exemplo incluída no pacote faz o mesmo com um arquivo:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Iniciar com flags explícitas

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

O que acontece, em ordem:

1. O plano é impresso (`Plan: READY profile=Omsi23004` no layout conhecido).
2. Recuperação de qualquer journal pendente mais antigo, aquisição do lease, snapshot, journal, overlays, verificação de integridade do plugin, início do `Omsi.exe`.
3. O ícone da bandeja aparece (`OmsiLaunch is running` (`OmsiLaunch em execução`)); veja [bandeja do Windows](../reference/windows-tray.md).
4. Quando o gameplay é iniciado, o status `Running` é impresso como JSON (`"State": 14`). O timeout de inicialização padrão é 180 s (use `/startup-timeout:<1..600>` para alterá-lo).
5. O console permanece conectado até o fim da sessão. Não feche a janela do console para parar: use um dos métodos de parada abaixo.

Adições opcionais para a primeira execução:

- `/set:graphics.maxFPS=60` (um overlay de `options.cfg`, restaurado no final);
- `/splash:Unset` para deixar a tela de abertura (splash) do OMSI intacta, ou `/splash-language:DEU` para escolher o splash gerenciado localizado;
- `/observe-seconds:30` para parar automaticamente 30 s depois de `Running` (útil para um smoke test);
- `--json` para saída estruturada.

Flags que solicitam data, hora, ano, clima ou um veículo do jogador (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) são aceitas, mas não podem ser aplicadas por este build: o plano se torna `NOT RUNNABLE` com `OL_E_CAPABILITY_UNAVAILABLE`. Não as use.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Iniciar com um perfil de sessão predefinido

Um perfil de sessão é um arquivo YAML em `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` que fixa o mapa, o ponto de entrada e até 99 predefinições de configurações (esquema `omsilaunch.session-profile/v1`; referência completa em [perfis de sessão](../reference/session-profiles.md)). Crie `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

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

Depois:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Regras para lembrar: o `id` precisa ser igual ao nome do diretório; o índice é `1..99`; flags explícitas que substituiriam um campo pertencente ao perfil (`/map`, `/entrypoint-index`, uma chave `/set` que pertence à predefinição, flags de splash quando a predefinição tem `presentation`) são rejeitadas com `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (saída `2`); o bloco `new:` só se aplica com `/new`; com `/saved:<file.osn>`, o mapa da situação precisa estar listado em `compatibility.maps`. O `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` incluído no pacote ilustra o esquema completo, mas seu bloco `new:` define `date`, `time` e `weather`, que este build não consegue aplicar; portanto, copie-o somente depois de remover essas chaves.

<a id="4-control-the-running-session"></a>
## 4. Controlar a sessão em execução

A partir de um segundo console no mesmo diretório (sem argumento de instalação):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Esses comandos passam pelo pipe de controle local desta instalação ([controle local](../reference/local-control.md)); a saída `4` significa que nenhum proprietário está em execução aqui.

<a id="5-stop"></a>
## 5. Parar

Qualquer um destes métodos encerra a sessão da mesma forma (o OMSI é encerrado, depois todos os arquivos tocados são restaurados e, em seguida, o journal e o backup são excluídos):

| Método | Observações |
|---|---|
| Ícone da bandeja → `End session` (`Encerrar sessão`) → confirmar | Disponível tanto em sessões do `OmsiLaunch.exe` quanto do `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | A partir de outro console; retorna imediatamente, e o proprietário conclui a restauração. |
| Ctrl+C no console do proprietário | Solicita a parada; o proprietário espera a restauração antes de sair. |
| `/observe-seconds:<n>` | Parada automática `n` segundos depois de `Running`. |
| O OMSI sai sozinho | O proprietário detecta `ProcessExited` e restaura. |

A rotina de encerramento do próprio OMSI não é executada, então o OMSI não regrava `options.cfg` ao sair; isso é intencional, para que a restauração seja exata. Fechar a janela do console do proprietário com o botão X dá à restauração apenas 4 s; se ela não terminar, o próximo início (ou `OmsiLaunch.exe /recover`) a conclui a partir do journal. O código de saída do proprietário é `0` quando a sessão terminou em `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Onde procurar depois

| Local | Conteúdo |
|---|---|
| Saída do console / `--json` | Plano, status `Running`, status final (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | O trace do host da sessão (limites da transação, início do processo, handoff para o plugin, gameplay iniciado, restauração). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Resultado de uma operação `/runtime:` executada pelo proprietário. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Eventos do indicador na bandeja. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` depois de um encerramento limpo. `true` significa que restou um journal; execute `OmsiLaunch.exe /recover`. |

Se a sessão não chegou ao gameplay, o status final traz o diagnóstico `OL_E_` da falha (por exemplo `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), o código de saída é `1` e os arquivos foram restaurados mesmo assim. Veja [códigos de saída](../reference/exit-codes.md), [erros](../reference/errors.md) e [limitações conhecidas](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Execução sem console

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delega para o `OmsiLaunchW.exe` e retorna `0` imediatamente. A sessão não tem console; as falhas aparecem como caixas de mensagem e o ícone da bandeja é a única superfície visível. Use `session status`, `events watch` e o diretório de diagnósticos para acompanhá-la. O comportamento completo do host Windows está na [referência do OmsiLaunchW.exe](../reference/omsilaunchw.md).
