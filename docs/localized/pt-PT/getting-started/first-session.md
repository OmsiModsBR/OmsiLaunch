# Primeira sessão

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> Tradução da [página original em inglês](../../../getting-started/first-session.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página percorre a primeira sessão OMSI gerida com o OmsiLaunch `0.1.0-beta4`: planear sem iniciar o OMSI, iniciar com flags explícitas, iniciar com um perfil de sessão predefinido, controlar e parar a sessão e, por fim, encontrar os diagnósticos. Pressupõe que o pacote está instalado conforme descrito em [instalação](installation.md). Todas as flags estão especificadas na [referência da CLI](../reference/cli.md); há mais invocações em [exemplos da CLI](../reference/cli-examples.md).

<a id="what-a-session-does"></a>
## O que faz uma sessão

Uma sessão é uma transação em torno de um processo do OMSI: o OmsiLaunch tira um snapshot dos ficheiros que vai tocar (por predefinição, os dois bitmaps do ecrã de abertura (splash screen) em `GUI\`, mais `options.cfg` quando são pedidos overlays `/set`), escreve um journal (registo de transação) persistente em `.omsilaunch\`, aplica os overlays, inicia `Omsi.exe` com o plugin permanente, aguarda até que se entre no jogo (`Running`), mantém a sessão controlável e, no fim, termina o OMSI e restaura cada ficheiro tocado byte a byte. `/new` nunca seleciona um mapa ou um ponto de entrada de forma silenciosa: ambos têm de ser indicados, ou vir de um ficheiro `/spec` ou de um perfil de sessão.

<a id="1-plan-nothing-is-started"></a>
## 1. Planear (nada é iniciado)

Executar a partir da raiz do OMSI; a instalação tem como predefinição o diretório que contém `OmsiLaunch.exe`.

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

O plano tem de indicar `"IsRunnable": true` (saída `0`). Lista `TouchedFiles` e `PlannedMutations`, para que seja possível ver exatamente o que a sessão vai sobrepor. Deve corrigir-se qualquer diagnóstico `OL_E_` antes de continuar; nada foi escrito.

A especificação de exemplo incluída no pacote faz o mesmo a partir de um ficheiro:

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. Iniciar com flags explícitas

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

O que acontece, por ordem:

1. O plano é impresso (`Plan: READY profile=Omsi23004` na disposição conhecida).
2. Recuperação de qualquer journal pendente anterior, aquisição do lease, snapshot, journal, overlays, verificação da integridade do plugin, início de `Omsi.exe`.
3. O ícone da área de notificação aparece (`OmsiLaunch is running`, ou seja, o OmsiLaunch está a executar); ver [área de notificação do Windows](../reference/windows-tray.md).
4. Quando se entra no jogo, o estado `Running` é impresso em JSON (`"State": 14`). O timeout de arranque predefinido é de 180 s (`/startup-timeout:<1..600>` para o alterar).
5. A consola permanece associada até ao fim da sessão. Não fechar a janela da consola para parar: usar um dos métodos de paragem indicados abaixo.

Adições opcionais para a primeira execução:

- `/set:graphics.maxFPS=60` (um overlay de `options.cfg`, restaurado no fim);
- `/splash:Unset` para deixar intacto o ecrã de abertura do OMSI, ou `/splash-language:DEU` para escolher o splash gerido localizado;
- `/observe-seconds:30` para parar automaticamente 30 s após `Running` (útil para um teste rápido (smoke test));
- `--json` para saída estruturada.

As flags que pedem uma data, hora, ano, meteorologia ou um veículo do jogador (`/date`, `/time`, `/year`, `/weather*`, `/vehicle`, ...) são aceites, mas não podem ser aplicadas por esta build: o plano torna-se `NOT RUNNABLE` com `OL_E_CAPABILITY_UNAVAILABLE`. Devem ser omitidas.

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. Iniciar com um perfil de sessão predefinido

Um perfil de sessão é um ficheiro YAML em `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` que fixa o mapa, o ponto de entrada e até 99 predefinições (presets) de definições (esquema `omsilaunch.session-profile/v1`; referência completa em [perfis de sessão](../reference/session-profiles.md)). Criar `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

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

Em seguida:

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

Regras a recordar: o `id` tem de ser igual ao nome do diretório; o índice é `1..99`; as flags explícitas que substituiriam um campo pertencente ao perfil (`/map`, `/entrypoint-index`, uma chave `/set` que pertence à predefinição, flags de splash quando a predefinição tem `presentation`) são rejeitadas com `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (saída `2`); o bloco `new:` aplica-se apenas com `/new`; com `/saved:<file.osn>`, o mapa da situação tem de estar listado em `compatibility.maps`. O ficheiro incluído no pacote `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` ilustra o esquema completo, mas o seu bloco `new:` define `date`, `time` e `weather`, que esta build não consegue aplicar, pelo que só deve ser copiado depois de essas chaves serem removidas.

<a id="4-control-the-running-session"></a>
## 4. Controlar a sessão em execução

A partir de uma segunda consola no mesmo diretório (sem argumento de instalação):

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

Estes comandos passam pelo pipe de controlo local desta instalação ([controlo local](../reference/local-control.md)); a saída `4` significa que não há nenhum proprietário a executar aqui.

<a id="5-stop"></a>
## 5. Parar

Qualquer um destes métodos termina a sessão da mesma forma (o OMSI é terminado, depois cada ficheiro tocado é restaurado e, por fim, o journal e a cópia de segurança são eliminados):

| Método | Notas |
|---|---|
| Ícone da área de notificação → `End session` → confirmar | Disponível tanto em sessões de `OmsiLaunch.exe` como de `OmsiLaunchW.exe`. |
| `OmsiLaunch.exe session stop` | A partir de outra consola; regressa imediatamente, e o proprietário conclui o restauro. |
| Ctrl+C na consola do proprietário | Pede a paragem; o proprietário aguarda pelo restauro antes de terminar. |
| `/observe-seconds:<n>` | Paragem automática `n` segundos após `Running`. |
| O OMSI termina por si próprio | O proprietário deteta `ProcessExited` e restaura. |

A rotina de encerramento do próprio OMSI não é executada, pelo que o OMSI não reescreve `options.cfg` ao sair; isto é intencional, para que o restauro seja exato. Fechar a janela da consola do proprietário com o botão X dá ao restauro apenas 4 s; se não terminar, o início seguinte (ou `OmsiLaunch.exe /recover`) conclui-o a partir do journal. O código de saída do proprietário é `0` quando a sessão terminou em `Completed`.

<a id="6-where-to-look-afterwards"></a>
## 6. Onde procurar depois

| Localização | Conteúdo |
|---|---|
| Saída da consola / `--json` | Plano, estado `Running`, estado final (`"State": 18` = `Completed`). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | O trace do host da sessão (limites da transação, início do processo, entrega ao plugin (handoff), entrada no jogo, restauro). |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | Resultado de uma operação `/runtime:` executada pelo proprietário. |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | Eventos do indicador da área de notificação. |
| `OmsiLaunch.exe /recovery-status` | `"pending": false` após um fim limpo. `true` significa que ficou um journal; executar `OmsiLaunch.exe /recover`. |

Se a sessão não chegou ao jogo, o estado final contém o diagnóstico `OL_E_` da falha (por exemplo `OL_E_STARTUP_TIMEOUT`, `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PROCESS_EXITED_EARLY`), o código de saída é `1` e os ficheiros foram restaurados mesmo assim. Ver [códigos de saída](../reference/exit-codes.md), [erros](../reference/errors.md) e [limitações conhecidas](../reference/known-limitations.md).

<a id="running-without-a-console"></a>
## Executar sem consola

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

delega em `OmsiLaunchW.exe` e devolve `0` imediatamente. A sessão não tem consola; as falhas aparecem como caixas de mensagem e o ícone da área de notificação é a única superfície visível. Para a acompanhar, usar `session status`, `events watch` e o diretório de diagnóstico. O comportamento completo do host Windows está na [referência do OmsiLaunchW.exe](../reference/omsilaunchw.md).
