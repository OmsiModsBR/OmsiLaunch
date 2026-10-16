# Referência do OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Tradução da [página original em inglês](../../../reference/omsilaunchw.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

`OmsiLaunchW.exe` é o host do subsistema Windows (GUI) do controlador do OmsiLaunch. Aceita a mesma linha de comando que `OmsiLaunch.exe` e executa o mesmo código do controlador (`OmsiLaunch.Controller.dll`). A única diferença está na forma como reporta: não há janela de consola, as falhas são mostradas como caixas de mensagem e uma sessão em execução só é visível através do seu [ícone da área de notificação](windows-tray.md).

Fontes de referência: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (o shim nativo), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) e `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Comparação entre OmsiLaunch.exe e OmsiLaunchW.exe

| Aspeto | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsistema | Consola. Abre uma janela de consola quando é iniciado a partir do Explorador de Ficheiros. | Windows (GUI). Sem janela de consola. |
| Shim nativo | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Ambiente | inalterado | define `OMSILAUNCH_WINDOWS_HOST=1` para o processo do controlador antes de o .NET arrancar |
| Argumentos | divididos em tokens com `CommandLineToArgvW` e passados inalterados ao controlador | o mesmo, pelo que ambos os hosts aceitam exatamente as mesmas flags e comandos ([referência da CLI](cli.md)) |
| Saída de texto | escrita em stdout | suprimida, exceto quando é indicado `--json` (nesse caso, os envelopes JSON são escritos em stdout, que um chamador pode redirecionar) |
| Erros | `OL_E_...: message` ou um envelope de erro JSON | as mesmas regras de saída da consola, **mais** uma caixa de mensagem para cada erro (ver [Caixas de diálogo de falha](#failure-dialogs)) |
| `/silent` | inicia `OmsiLaunchW.exe` com os restantes argumentos e devolve `0` | ignorado: o comando já está a executar no host Windows |
| Ícone da área de notificação | mostrado para uma sessão de proprietário | mostrado para uma sessão de proprietário |
| Caminhos de paragem | área de notificação, `session stop`, saída do OMSI, `/observe-seconds`, Ctrl+C, fecho da consola | área de notificação, `session stop`, saída do OMSI, `/observe-seconds` (não há consola, pelo que Ctrl+C e o fecho da consola não se aplicam) |
| Códigos de saída | [`PublicExitCode`](exit-codes.md) `0`..`10`, códigos de shim `100`..`106` | os mesmos códigos |

<a id="how-it-starts"></a>
## Como arranca

1. O shim resolve o seu próprio caminho (`GetModuleFileNameW`) e espera encontrar `OmsiLaunch.Controller.dll` no mesmo diretório.
2. Divide a linha de comando em tokens (`CommandLineToArgvW`) e define `OMSILAUNCH_WINDOWS_HOST=1`.
3. Localiza `hostfxr` através do `nethost.dll` incluído no pacote, carrega-o, inicializa o controlador com os argumentos (o caminho do controlador não faz parte da lista de argumentos que o parser da CLI vê) e executa-o.
4. O shim devolve inalterado o código de saída do controlador.

Se algum passo anterior à execução do controlador falhar, o shim mostra uma caixa de mensagem com o título `OmsiLaunch` e o texto `OmsiLaunch could not start the .NET host (code N).` e termina com esse código:

| Código | Passo que falhou |
| --- | --- |
| `100` | não foi possível resolver o caminho do executável |
| `101` | não foi possível dividir a linha de comando em tokens |
| `102` | a sondagem da localização de `hostfxr` falhou (normalmente: o runtime x64 do .NET 6 não está instalado) |
| `103` | não foi possível obter o caminho de `hostfxr` |
| `104` | não foi possível carregar `hostfxr` |
| `105` | faltam exportações obrigatórias de `hostfxr` |
| `106` | não foi possível inicializar o host gerido (por exemplo, `OmsiLaunch.Controller.dll` ou a sua configuração de runtime está em falta, ou o Windows Desktop Runtime está ausente) |

`OmsiLaunch.exe` usa a mesma tabela, mas não imprime nada. Estas caixas de diálogo não foram produzidas em runtime (ver [estado da validação em runtime](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Iniciá-lo

Início direto, a partir de um atalho, de um script ou de outro programa:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Através de `OmsiLaunch.exe` com `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Os executáveis devem ser colocados na instalação do OMSI 2 (a estrutura do pacote, ver [empacotamento](packaging.md)); sem argumento de instalação, a instalação é o diretório que contém o executável. Uma instalação explícita é passada como primeiro argumento, tal como com `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Por ser um programa GUI, o `cmd.exe` e o Explorador de Ficheiros não esperam por ele. Para esperar e ler o código de saída a partir de um script, usar `start /wait OmsiLaunchW.exe ...` no `cmd.exe` ou `Start-Process -Wait -PassThru` no PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delegação /silent

`OmsiLaunch.exe ... /silent` (ou `--silent`), quando não está já a executar sob `OmsiLaunchW.exe`, faz o seguinte (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Procura `OmsiLaunchW.exe` no diretório de `OmsiLaunch.exe`. Se estiver em falta: `OL_E_WINDOWS_HOST_MISSING`, saída `7`.
2. Inicia `OmsiLaunchW.exe` através de `ShellExecute` (`UseShellExecute = true`) com o diretório atual e todos os argumentos exceto `/silent`/`--silent`, pela ordem original. `ShellExecute` não passa os handles do chamador ao novo processo, pelo que um chamador que capture a saída de `OmsiLaunch.exe /silent` não fica bloqueado durante toda a vida da sessão (fecho de runtime BUG-03). Se nenhum processo for devolvido: `OL_E_WINDOWS_HOST_START_FAILED`, saída `7`.
3. Escreve o envelope `silent` e termina imediatamente com `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

A saída `0` significa apenas que `OmsiLaunchW.exe` foi iniciado. O resultado da sessão (erros de planeamento, um proprietário já ativo, um início falhado) é reportado por `OmsiLaunchW.exe` com as suas próprias caixas de diálogo e diagnósticos, e os dois processos são depois independentes: `OmsiLaunch.exe` terminou e `OmsiLaunchW.exe` é o proprietário da sessão. Usar `OmsiLaunch.exe session status` para ver a sessão.

`/silent` é aplicado antes de qualquer outro comando, pelo que `OmsiLaunch.exe /silent session status` também executa `session status` dentro de `OmsiLaunchW.exe`, onde a sua saída é suprimida. Usar `/silent` apenas para lançamentos.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## O que cada comando faz sob o OmsiLaunchW.exe

| Linha de comando | Resultado |
| --- | --- |
| sem argumentos | executa `detect` silenciosamente e termina com `0` (nada é mostrado) |
| um lançamento (`/new`, `/saved:...`, `/spec:...`, um perfil de sessão) com um plano executável e sem proprietário | torna-se o proprietário da sessão: ícone da área de notificação durante o arranque e a execução; termina quando a sessão termina (`0` concluída, `1` falhada) |
| um lançamento cujo plano não é executável | caixa de diálogo com o último diagnóstico `OL_E_` do plano (alternativa `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), saída `1` (auditoria da documentação BUG-06; antes da correção terminava silenciosamente) |
| um lançamento enquanto já está ativo um proprietário para a instalação | caixa de diálogo `OL_E_SESSION_ALREADY_ACTIVE`, saída `7`; a sessão em execução não é afetada |
| um lançamento que não chega a `Running` | caixa de diálogo `The OMSI session did not reach gameplay.` com o último diagnóstico `OL_E_`, saída `1`. O OMSI é terminado e os ficheiros são restaurados pelo supervisor da sessão enquanto a caixa de diálogo está aberta; o processo termina depois de a caixa de diálogo ser fechada e de `CloseAsync` ter terminado |
| um argumento inválido, uma flag desconhecida ou um perfil de sessão inválido | caixa de diálogo com `OL_E_INVALID_ARGUMENT` ou o código `OL_E_SESSION_PROFILE_*`, saída `2` |
| um comando de cliente (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) sem proprietário | caixa de diálogo `OL_E_NO_ACTIVE_SESSION`, saída `4` |
| um comando de cliente que o proprietário rejeita | caixa de diálogo com o código de rejeição (por exemplo `OL_E_CONTROL_SESSION_MISMATCH` ou um código de erro de runtime), saída `7` (`2` para uma operação desconhecida ou um argumento em falta) |
| um comando de cliente ou de descoberta bem-sucedido (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | nenhuma saída visível, exceto se for indicado `--json` e o stdout for redirecionado; código de saída igual ao de `OmsiLaunch.exe` |
| `/plan` ou `/validate` | nenhuma caixa de diálogo, mesmo para um plano não executável; saída `0` ou `1` |
| qualquer outra falha do controlador | caixa de diálogo com o código `OL_E_` classificado; código de saída conforme [códigos de saída](exit-codes.md) |

<a id="failure-dialogs"></a>
## Caixas de diálogo de falha

Cada erro que `OmsiLaunch.exe` imprimiria é também mostrado numa caixa de mensagem modal (`WindowsHost.ShowFailure`), mesmo quando é indicado `--json`:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` é a mensagem de erro ou, no caso de uma falha de sessão, a mensagem do último diagnóstico `OL_E_`. Limitação conhecida: numa falha reportada pelo plugin, a mensagem é o payload de falha em bruto do plugin (por exemplo `{"name":"world.failed",...}`); a linha `Code:` está correta (ver [limitações conhecidas](known-limitations.md)). A caixa de diálogo é modal e o processo termina depois de ser fechada. Evidência de runtime: erro de argumento, nenhuma sessão ativa e uma falha antes da entrada no jogo (fecho de runtime `T04`).

<a id="session-tray-and-exit"></a>
## Sessão, área de notificação e saída

Uma sessão pertencente a `OmsiLaunchW.exe` comporta-se exatamente como uma pertencente a `OmsiLaunch.exe` (ver [ciclo de vida da sessão](../concepts/session-lifecycle.md)):

- O ícone da área de notificação aparece assim que a sessão é iniciada, antes de o OMSI entrar no jogo, exceto se `Presentation.SuppressTrayIcon` estiver definido num ficheiro `/spec`. Com `SuppressTrayIcon` não existe qualquer superfície visível; a sessão deve ser parada com `OmsiLaunch.exe session stop` ou fechando o OMSI.
- A sessão termina quando o OMSI sai, quando `End session` (terminar a sessão) é confirmado na área de notificação, quando um cliente envia `session stop` ou quando `/observe-seconds` expira. O OmsiLaunch termina então o OMSI, se ainda estiver a executar, restaura cada ficheiro que alterou, liberta o lease da instalação, remove o ícone da área de notificação e termina.
- Se o próprio `OmsiLaunchW.exe` for terminado à força, o início seguinte do OmsiLaunch para essa instalação recupera a transação pendente (ver [transações e recuperação](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Início rápido

1. Instalar o pacote no diretório do OMSI 2 ([instalação](../getting-started/installation.md)).
2. Criar um atalho para `OmsiLaunchW.exe` com os argumentos da sessão, por exemplo `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Iniciá-lo. O OMSI arranca sem janela de consola; o ícone do OmsiLaunch aparece na área de notificação.
4. Clicar com o botão direito do rato no ícone → `Status` para ver a sessão (estado), ou `End session` → `End session` para a terminar.
5. Se algo correr mal, a caixa de diálogo mostra o código de erro; os detalhes estão em `<OMSI_PATH>\.omsilaunch\diagnostics`.
