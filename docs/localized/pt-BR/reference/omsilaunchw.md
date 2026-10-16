# Referência do OmsiLaunchW.exe

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> Tradução da [página original em inglês](../../../reference/omsilaunchw.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O `OmsiLaunchW.exe` é o host do subsistema Windows (GUI) do controlador do OmsiLaunch. Ele aceita a mesma linha de comando que o `OmsiLaunch.exe` e executa o mesmo código de controlador (`OmsiLaunch.Controller.dll`). A única diferença é a forma como ele informa os resultados: não há janela de console, as falhas são mostradas como caixas de mensagem e uma sessão em execução só fica visível por meio do seu [ícone da bandeja](windows-tray.md).

Fontes de referência: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp` (o shim nativo), `tools\OmsiLaunch.Cli\WindowsHost.cs` (`WindowsHost`, `SessionTrayIndicator`) e `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Write*`).

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## Comparação entre OmsiLaunch.exe e OmsiLaunchW.exe

| Aspecto | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| Subsistema | Console. Abre uma janela de console quando iniciado pelo Explorer. | Windows (GUI). Sem janela de console. |
| Shim nativo | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| Ambiente | inalterado | define `OMSILAUNCH_WINDOWS_HOST=1` para o processo do controlador antes de o .NET iniciar |
| Argumentos | divididos em tokens com `CommandLineToArgvW` e repassados sem alteração ao controlador | o mesmo, portanto os dois hosts aceitam exatamente as mesmas flags e os mesmos comandos ([referência da CLI](cli.md)) |
| Saída de texto | gravada em stdout | suprimida, a menos que `--json` seja informado (nesse caso, os envelopes JSON são gravados em stdout, que o chamador pode redirecionar) |
| Erros | `OL_E_...: message` ou um envelope de erro JSON | as mesmas regras de saída do console, **além de** uma caixa de mensagem para cada erro (veja [Caixas de diálogo de falha](#failure-dialogs)) |
| `/silent` | inicia o `OmsiLaunchW.exe` com os demais argumentos e retorna `0` | ignorado: o comando já é executado no host Windows |
| Ícone da bandeja | mostrado para uma sessão de proprietário | mostrado para uma sessão de proprietário |
| Caminhos de parada | bandeja, `session stop`, saída do OMSI, `/observe-seconds`, Ctrl+C, fechamento do console | bandeja, `session stop`, saída do OMSI, `/observe-seconds` (não há console, então Ctrl+C e fechamento do console não se aplicam) |
| Códigos de saída | [`PublicExitCode`](exit-codes.md) `0`..`10`, códigos do shim `100`..`106` | os mesmos códigos |

<a id="how-it-starts"></a>
## Como ele inicia

1. O shim resolve o próprio caminho (`GetModuleFileNameW`) e espera encontrar `OmsiLaunch.Controller.dll` no mesmo diretório.
2. Ele divide a linha de comando em tokens (`CommandLineToArgvW`) e define `OMSILAUNCH_WINDOWS_HOST=1`.
3. Ele localiza o `hostfxr` por meio do `nethost.dll` incluído no pacote, carrega-o, inicializa o controlador com os argumentos (o caminho do controlador não faz parte da lista de argumentos que o parser da CLI vê) e o executa.
4. O shim retorna o código de saída do controlador sem alteração.

Se qualquer etapa anterior à execução do controlador falhar, o shim mostra uma caixa de mensagem com o título `OmsiLaunch` e o texto `OmsiLaunch could not start the .NET host (code N).` e sai com esse código:

| Código | Etapa que falhou |
| --- | --- |
| `100` | não foi possível resolver o caminho do executável |
| `101` | não foi possível dividir a linha de comando em tokens |
| `102` | a sondagem de localização do `hostfxr` falhou (normalmente: o runtime .NET 6 x64 não está instalado) |
| `103` | não foi possível obter o caminho do `hostfxr` |
| `104` | não foi possível carregar o `hostfxr` |
| `105` | exportações obrigatórias do `hostfxr` estão ausentes |
| `106` | não foi possível inicializar o host gerenciado (por exemplo, `OmsiLaunch.Controller.dll` ou sua configuração de runtime está ausente, ou o runtime Windows Desktop não está presente) |

O `OmsiLaunch.exe` usa a mesma tabela, mas não imprime nada. Essas caixas de diálogo não foram produzidas em runtime (veja [status de validação em runtime](../status/runtime-validation-status.md)).

<a id="starting-it"></a>
## Como iniciá-lo

Início direto, a partir de um atalho, de um script ou de outro programa:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

Por meio do `OmsiLaunch.exe` com `/silent`:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

Coloque os executáveis na instalação do OMSI 2 (o layout do pacote, veja [empacotamento](packaging.md)); sem argumento de instalação, a instalação é o diretório que contém o executável. Uma instalação explícita é passada como primeiro argumento, como no `OmsiLaunch.exe` (`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`).

Por ser um programa GUI, o `cmd.exe` e o Explorer não esperam por ele. Para esperar e ler o código de saída a partir de um script, use `start /wait OmsiLaunchW.exe ...` no `cmd.exe` ou `Start-Process -Wait -PassThru` no PowerShell:

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## Delegação com /silent

`OmsiLaunch.exe ... /silent` (ou `--silent`), quando ainda não está em execução sob o `OmsiLaunchW.exe`, faz o seguinte (`CliProgram.RunAsync`, `CliProgram.SilentDelegation`):

1. Procura o `OmsiLaunchW.exe` no diretório do `OmsiLaunch.exe`. Se ele estiver ausente: `OL_E_WINDOWS_HOST_MISSING`, saída `7`.
2. Inicia o `OmsiLaunchW.exe` por meio de `ShellExecute` (`UseShellExecute = true`) com o diretório atual e todos os argumentos, exceto `/silent`/`--silent`, na ordem original. `ShellExecute` não repassa os handles do chamador ao novo processo, então um chamador que captura a saída de `OmsiLaunch.exe /silent` não fica bloqueado durante toda a vida da sessão (fechamento em runtime BUG-03). Se nenhum processo for retornado: `OL_E_WINDOWS_HOST_START_FAILED`, saída `7`.
3. Grava o envelope `silent` e sai imediatamente com `0`:

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

A saída `0` significa apenas que o `OmsiLaunchW.exe` foi iniciado. O resultado da sessão (erros de planejamento, um proprietário já ativo, um início com falha) é informado pelo `OmsiLaunchW.exe` com suas próprias caixas de diálogo e diagnósticos, e os dois processos ficam independentes a partir daí: o `OmsiLaunch.exe` terminou e o `OmsiLaunchW.exe` é o proprietário da sessão. Use `OmsiLaunch.exe session status` para ver a sessão.

`/silent` é aplicado antes de qualquer outro comando, então `OmsiLaunch.exe /silent session status` também executa `session status` dentro do `OmsiLaunchW.exe`, onde sua saída é suprimida. Use `/silent` somente para inicializações.

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## O que cada comando faz sob o OmsiLaunchW.exe

| Linha de comando | Resultado |
| --- | --- |
| sem argumentos | executa `detect` silenciosamente e sai com `0` (nada é mostrado) |
| uma inicialização (`/new`, `/saved:...`, `/spec:...`, um perfil de sessão) com um plano apto para execução e sem proprietário | torna-se o proprietário da sessão: ícone da bandeja durante a inicialização e a execução; sai quando a sessão termina (`0` concluída, `1` com falha) |
| uma inicialização cujo plano não está apto para execução | caixa de diálogo com o último diagnóstico `OL_E_` do plano (fallback `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`), saída `1` (auditoria de documentação BUG-06; antes da correção, ele saía silenciosamente) |
| uma inicialização enquanto já há um proprietário ativo para a instalação | caixa de diálogo `OL_E_SESSION_ALREADY_ACTIVE`, saída `7`; a sessão em execução não é afetada |
| uma inicialização que não chega a `Running` | caixa de diálogo `The OMSI session did not reach gameplay.` com o último diagnóstico `OL_E_`, saída `1`. O OMSI é encerrado e os arquivos são restaurados pelo supervisor da sessão enquanto a caixa de diálogo está aberta; o processo sai depois que a caixa de diálogo é fechada e `CloseAsync` termina |
| um argumento inválido, uma flag desconhecida ou um perfil de sessão inválido | caixa de diálogo com `OL_E_INVALID_ARGUMENT` ou com o código `OL_E_SESSION_PROFILE_*`, saída `2` |
| um comando de cliente (`session status`, `session stop`, `events read`, `time get`, `/runtime:...`) sem proprietário | caixa de diálogo `OL_E_NO_ACTIVE_SESSION`, saída `4` |
| um comando de cliente que o proprietário rejeita | caixa de diálogo com o código da rejeição (por exemplo `OL_E_CONTROL_SESSION_MISMATCH` ou um código de erro de runtime), saída `7` (`2` para uma operação desconhecida ou um argumento ausente) |
| um comando de cliente ou de descoberta bem-sucedido (`session status`, `/list:...`, `help`, `capabilities`, `/version`, `/recovery-status`) | nenhuma saída visível, a menos que `--json` seja informado e stdout seja redirecionado; código de saída igual ao do `OmsiLaunch.exe` |
| `/plan` ou `/validate` | nenhuma caixa de diálogo, mesmo para um plano não apto para execução; saída `0` ou `1` |
| qualquer outra falha do controlador | caixa de diálogo com o código `OL_E_` classificado; código de saída conforme [códigos de saída](exit-codes.md) |

<a id="failure-dialogs"></a>
## Caixas de diálogo de falha

Todo erro que o `OmsiLaunch.exe` imprimiria também é mostrado como uma caixa de mensagem modal (`WindowsHost.ShowFailure`), mesmo quando `--json` é informado:

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` é a mensagem de erro ou, para uma falha de sessão, a mensagem do último diagnóstico `OL_E_`. Limitação conhecida: para uma falha informada pelo plugin, a mensagem é o payload bruto da falha do plugin (por exemplo `{"name":"world.failed",...}`); a linha `Code:` está correta (veja [limitações conhecidas](known-limitations.md)). A caixa de diálogo é modal e o processo sai depois que ela é fechada. Evidência de runtime: erro de argumento, nenhuma sessão ativa e uma falha antes do gameplay (fechamento em runtime `T04`).

<a id="session-tray-and-exit"></a>
## Sessão, bandeja e saída

Uma sessão cujo proprietário é o `OmsiLaunchW.exe` se comporta exatamente como uma cujo proprietário é o `OmsiLaunch.exe` (veja [ciclo de vida da sessão](../concepts/session-lifecycle.md)):

- O ícone da bandeja aparece assim que a sessão é iniciada, antes de o OMSI chegar ao gameplay, a menos que `Presentation.SuppressTrayIcon` esteja definido em um arquivo `/spec`. Com `SuppressTrayIcon` não há nenhuma superfície visível; pare a sessão com `OmsiLaunch.exe session stop` ou fechando o OMSI.
- A sessão termina quando o OMSI sai, quando `End session` (`Encerrar sessão`) é confirmado na bandeja, quando um cliente envia `session stop` ou quando `/observe-seconds` se esgota. Em seguida, o OmsiLaunch encerra o OMSI se ele ainda estiver em execução, restaura todos os arquivos que alterou, libera o lease da instalação, remove o ícone da bandeja e sai.
- Se o próprio `OmsiLaunchW.exe` for finalizado à força, o próximo início do OmsiLaunch para essa instalação recupera a transação pendente (veja [transações e recuperação](../concepts/transactions-and-recovery.md)).

<a id="quick-start"></a>
## Início rápido

1. Instale o pacote no diretório do OMSI 2 ([instalação](../getting-started/installation.md)).
2. Crie um atalho para o `OmsiLaunchW.exe` com os argumentos da sessão, por exemplo `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`.
3. Inicie-o. O OMSI inicia sem janela de console; o ícone do OmsiLaunch aparece na área de notificação.
4. Clique com o botão direito no ícone → `Status` para ver a sessão, ou `End session` → `End session` (`Encerrar sessão` → `Encerrar sessão`) para encerrá-la.
5. Se algo der errado, a caixa de diálogo mostra o código de erro; os detalhes estão em `<OMSI_PATH>\.omsilaunch\diagnostics`.
