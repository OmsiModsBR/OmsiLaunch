# Códigos de saída

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Tradução da [página original em inglês](../../../reference/exit-codes.md) do OmsiLaunch 0.1.0-beta3. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página lista todos os códigos de saída de processo que o `OmsiLaunch.exe` e o `OmsiLaunchW.exe` podem retornar: o contrato gerenciado público `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), os códigos do shim nativo `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` e `OmsiLaunch.WindowsHost.cpp`) e as regras de classificação que `CliProgram.Classify` aplica a qualquer exceção que escape (`tools\OmsiLaunch.Cli\Program.cs`). Os chamadores precisam inferir a semântica a partir do código e do envelope de erro estruturado, nunca a partir do texto da mensagem. Os códigos de erro estão catalogados em [erros](errors.md); os comandos que produzem cada código estão na [referência da CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Códigos de saída públicos (`PublicExitCode`)

| Código | Nome do enum | Significado | Quando |
|---|---|---|---|
| 0 | `Success` | O comando foi concluído. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` com um plano apto para execução; uma sessão que terminou em `Completed`; `/silent` assim que o `OmsiLaunchW.exe` foi iniciado; um comando de cliente encaminhado que o proprietário respondeu com `Ok=true`; `/recover` quando nada estava pendente ou a restauração foi concluída. |
| 1 | `SessionFailed` | Um plano não estava apto para execução, ou uma sessão da qual o processo é proprietário terminou em `Failed`. | `/plan` informando `NOT RUNNABLE`; uma inicialização cujo plano não está apto para execução (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; o `OmsiLaunchW.exe` também mostra o último diagnóstico `OL_E_` em uma caixa de mensagem); `OL_E_PLAN_NOT_RUNNABLE` gerado por `StartSessionAsync` (replanejamento no início); a sessão não chegou a `Running` dentro do timeout de inicialização; a sessão terminou em `Failed`. |
| 2 | `InvalidArguments` | A linha de comando, a spec, o perfil ou os argumentos de runtime foram rejeitados antes ou durante o despacho. | Flag ou rota desconhecida, valor ausente, valor fora do intervalo; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` e `OL_E_RUNTIME_ARGUMENT_REQUIRED` (localmente ou retornados pelo proprietário); instruções de uso impressas para um comando que não pode ser despachado; qualquer `ArgumentException`, `FormatException`, `InvalidDataException` ou `OverflowException`. |
| 3 | `UnsupportedProfile` | A plataforma ou o build do OMSI não é suportado. | Uma exceção que escapa e cujo código começa com `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Observe que as mesmas condições, quando encontradas durante o planejamento, tornam o plano não apto para execução e retornam `1`. |
| 4 | `NoActiveSession` | Um comando de cliente não encontrou nenhum proprietário. | `session status`, `session stop`, `events read`, `events watch` ou uma operação de runtime encaminhada quando o endpoint de controle local desta instalação não responde (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Um timeout escapou como exceção. | Qualquer `TimeoutException` (`OL_E_TIMEOUT` quando a mensagem não traz código; caso contrário, o código embutido, como `OL_E_RUNTIME_REQUEST_TIMEOUT`). Timeouts de clientes encaminhados são respondidos pelo proprietário como `Ok=false` e retornam `7`, não `5`. |
| 6 | `NotFound` | Um arquivo ou diretório não foi encontrado. | `FileNotFoundException` / `DirectoryNotFoundException` (padrão `OL_E_NOT_FOUND`), por exemplo `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` quando gerado como exceção, um diretório de instalação ausente durante `/list`. |
| 7 | `OperationRejected` | O comando era válido, mas foi recusado, ou um comando encaminhado falhou no proprietário. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; toda resposta de controle `Ok=false` diferente dos dois códigos de argumento (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); qualquer outra exceção que escape com um código `OL_E_` não classificado em outra regra (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Uma transação durável não pôde ser restaurada. | `/recover` quando o journal estava pendente e continua pendente; qualquer exceção que escape cujo código começa com `OL_E_RECOVERY_` ou é `OL_E_RESTORE_FAILED` (por exemplo `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` durante a restauração da própria sessão). |
| 10 | `InternalError` | Uma exceção inesperada sem código `OL_E_`. | Informada como `OL_E_INTERNAL` com categoria `internal`; a mensagem é o texto da exceção. |

O código `9` não é atribuído.

<a id="native-shim-exit-codes"></a>
## Códigos de saída do shim nativo

Retornados pelo `OmsiLaunch.exe` / `OmsiLaunchW.exe` antes da execução do controlador gerenciado. Eles são disjuntos de `PublicExitCode`, para que um chamador possa distinguir uma falha na inicialização do host de um resultado do controlador. O `OmsiLaunchW.exe` também mostra `OmsiLaunch could not start the .NET host (code N).` em uma caixa de mensagem.

| Código | Significado | Causa |
|---|---|---|
| 100 | Não foi possível resolver o caminho do executável | `GetModuleFileNameW` falhou. |
| 101 | Não foi possível dividir a linha de comando em tokens | `CommandLineToArgvW` retornou null. |
| 102 | A sondagem de localização do `hostfxr` falhou | A consulta de tamanho de `get_hostfxr_path` falhou: nenhum runtime .NET compatível está instalado (o runtime .NET 6 x64 é obrigatório). |
| 103 | Não foi possível obter o caminho do `hostfxr` | A segunda chamada a `get_hostfxr_path` falhou. |
| 104 | Não foi possível carregar a biblioteca `hostfxr` | `LoadLibraryW` no `hostfxr.dll` resolvido falhou. |
| 105 | Exportações obrigatórias do `hostfxr` estão ausentes | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` ou `hostfxr_close` não encontrado. |
| 106 | Não foi possível inicializar o host gerenciado | `hostfxr_initialize_for_dotnet_command_line` falhou para `OmsiLaunch.Controller.dll` (`OmsiLaunch.Controller.runtimeconfig.json` ausente, `Microsoft.WindowsDesktop.App` 6.0 x64 ausente ou pacote danificado). |

<a id="classification-rules-cliprogramclassify"></a>
## Regras de classificação (`CliProgram.Classify`)

Toda exceção que escapa de `CliProgram.RunAsync` é transformada em um envelope de erro (`CliInput.WriteError`) e em um código de saída por `CliProgram.ReportFailure`, que chama `Classify`. Falhas de parsing são tratadas da mesma forma antes do despacho (saída `2`). As regras são aplicadas nesta ordem:

1. O primeiro token `OL_E_` na mensagem da exceção é extraído (`ExtractCode`): o código é a maior sequência de letras ASCII, dígitos e `_` que começa em `OL_E_`. Os códigos são expostos literalmente em `error.code`.
2. `SessionProfileException` → seu próprio `Code`, categoria `invalid_argument`, saída `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → código extraído ou `OL_E_INVALID_ARGUMENT`, categoria `invalid_argument`, saída `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → código extraído ou `OL_E_NOT_FOUND`, categoria `not_found`, saída `6`.
5. `TimeoutException` → código extraído ou `OL_E_TIMEOUT`, categoria `runtime`, saída `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, categoria `session`, saída `7`.
7. Caso contrário, quando um código foi extraído:
   - começa com `OL_E_RECOVERY_` ou é igual a `OL_E_RESTORE_FAILED` → categoria `transaction`, saída `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → categoria `session`, saída `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → categoria `session`, saída `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → categoria `invalid_argument`, saída `2`;
   - começa com `OL_E_UNSUPPORTED_` → categoria `unsupported_profile`, saída `3`;
   - qualquer outro código → categoria `runtime` para `InvalidOperationException` e `IOException`, caso contrário `internal`; saída `7`.
8. Nenhum código → `OL_E_INTERNAL`, categoria `internal`, saída `10`.

As respostas de cliente encaminhadas não passam por `Classify`: `CliProgram.ReportForwarded` retorna `4` quando não há endpoint, `2` para `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` para qualquer outra resposta `Ok=false` e `0` para `Ok=true`.

<a id="scripting-guidance"></a>
## Orientações para scripts

- Trate `0` como sucesso e todo o resto como falha; ramifique pelo código numérico e, depois, por `error.code` do envelope `--json`.
- Uma inicialização de sessão só retorna depois que a sessão terminou e seus arquivos foram restaurados; `1` significa que a transação foi executada, mas o OMSI falhou ou o plano foi rejeitado, e não que arquivos ficaram modificados (um journal residual é informado por `/recovery-status`).
- `100`..`106` significam que o pacote ou o runtime .NET está danificado; veja [instalação](../getting-started/installation.md) e [empacotamento](packaging.md).
