# Códigos de saída

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> Tradução da [página original em inglês](../../../reference/exit-codes.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

Esta página lista todos os códigos de saída de processo que `OmsiLaunch.exe` e `OmsiLaunchW.exe` podem devolver: o contrato gerido público `PublicExitCode` (`src\OmsiLaunch.Api\PublicControlContract.cs`), os códigos do shim nativo `100`..`106` (`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` e `OmsiLaunch.WindowsHost.cpp`) e as regras de classificação que `CliProgram.Classify` aplica a qualquer exceção que escape (`tools\OmsiLaunch.Cli\Program.cs`). Os chamadores devem inferir a semântica a partir do código e do envelope de erro estruturado, nunca a partir do texto da mensagem. Os códigos de erro estão catalogados em [erros](errors.md); os comandos que produzem cada código estão na [referência da CLI](cli.md).

<a id="public-exit-codes-publicexitcode"></a>
## Códigos de saída públicos (`PublicExitCode`)

| Código | Nome da enumeração | Significado | Quando |
|---|---|---|---|
| 0 | `Success` | O comando foi concluído. | `/version`, `capabilities`, `help`, `profiles`, `detect`, `/list`, `/recovery-status`; `/plan`/`/validate` com um plano executável; uma sessão que terminou em `Completed`; `/silent` depois de `OmsiLaunchW.exe` ter sido iniciado; um comando de cliente reencaminhado a que o proprietário respondeu com `Ok=true`; `/recover` quando nada estava pendente ou o restauro foi concluído. |
| 1 | `SessionFailed` | Um plano não era executável, ou uma sessão própria terminou em `Failed`. | `/plan` a reportar `NOT RUNNABLE`; um lançamento cujo plano não é executável (`OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_PERMANENT_PLUGIN_*`, ...; `OmsiLaunchW.exe` mostra também o último diagnóstico `OL_E_` numa caixa de mensagem); `OL_E_PLAN_NOT_RUNNABLE` gerado por `StartSessionAsync` (novo planeamento no início); a sessão não chegou a `Running` dentro do timeout de arranque; a sessão terminou em `Failed`. |
| 2 | `InvalidArguments` | A linha de comando, a especificação, o perfil ou os argumentos de runtime foram rejeitados antes ou durante o despacho. | Flag ou rota desconhecida, valor em falta, valor fora do intervalo; `SessionProfileException` (`OL_E_SESSION_PROFILE_*`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY`; `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`; `OL_E_ITX_PROFILE_REQUIRED`; `OL_E_RUNTIME_OPERATION_UNKNOWN` e `OL_E_RUNTIME_ARGUMENT_REQUIRED` (localmente ou devolvidos pelo proprietário); utilização (usage) impressa para um comando que não pode ser despachado; qualquer `ArgumentException`, `FormatException`, `InvalidDataException` ou `OverflowException`. |
| 3 | `UnsupportedProfile` | A plataforma ou a build do OMSI não é suportada. | Uma exceção que escapou cujo código começa por `OL_E_UNSUPPORTED_` (`OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_OS_ARCHITECTURE`). Note-se que as mesmas condições detetadas durante o planeamento tornam o plano não executável e devolvem `1` em vez disso. |
| 4 | `NoActiveSession` | Um comando de cliente não encontrou nenhum proprietário. | `session status`, `session stop`, `events read`, `events watch` ou uma operação de runtime reencaminhada quando o endpoint de controlo local desta instalação não responde (`OL_E_NO_ACTIVE_SESSION`). |
| 5 | `RuntimeUnavailable` | Um timeout escapou como exceção. | Qualquer `TimeoutException` (`OL_E_TIMEOUT` quando a mensagem não contém código; caso contrário, o código incorporado, como `OL_E_RUNTIME_REQUEST_TIMEOUT`). Os timeouts de clientes reencaminhados são respondidos pelo proprietário como `Ok=false` e devolvem `7`, não `5`. |
| 6 | `NotFound` | Um ficheiro ou diretório não foi encontrado. | `FileNotFoundException` / `DirectoryNotFoundException` (`OL_E_NOT_FOUND` por predefinição), por exemplo `OL_E_SPEC_NOT_FOUND`, `OL_E_ITX_PROFILE_MISSING` quando gerado como exceção, um diretório de instalação em falta durante `/list`. |
| 7 | `OperationRejected` | O comando era válido mas foi recusado, ou um comando reencaminhado falhou no proprietário. | `OL_E_SESSION_ALREADY_ACTIVE`, `OL_E_INSTALLATION_BUSY`, `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED`, `OL_E_CANCELLED`; todas as respostas de controlo `Ok=false` exceto os dois códigos de argumento (`OL_E_CONTROL_*`, `OL_E_RUNTIME_*`, `OL_E_SESSION_NOT_RUNNING`); qualquer outra exceção que escape com um código `OL_E_` não classificado noutro sítio (`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_PROCESS_*`, ...). |
| 8 | `TransactionRecoveryFailed` | Uma transação persistente não pôde ser restaurada. | `/recover` quando o journal estava pendente e continua pendente; qualquer exceção que escape cujo código comece por `OL_E_RECOVERY_` ou seja `OL_E_RESTORE_FAILED` (por exemplo `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` durante o restauro da própria sessão). |
| 10 | `InternalError` | Uma exceção inesperada sem código `OL_E_`. | Reportada como `OL_E_INTERNAL` com a categoria `internal`; a mensagem é o texto da exceção. |

O código `9` não está atribuído.

<a id="native-shim-exit-codes"></a>
## Códigos de saída do shim nativo

Devolvidos por `OmsiLaunch.exe` / `OmsiLaunchW.exe` antes de o controlador gerido ser executado. São disjuntos de `PublicExitCode`, para que um chamador consiga distinguir uma falha de arranque do host de um resultado do controlador. `OmsiLaunchW.exe` mostra adicionalmente `OmsiLaunch could not start the .NET host (code N).` numa caixa de mensagem.

| Código | Significado | Causa |
|---|---|---|
| 100 | Não foi possível resolver o caminho do executável | `GetModuleFileNameW` falhou. |
| 101 | Não foi possível dividir a linha de comando em tokens | `CommandLineToArgvW` devolveu null. |
| 102 | A sondagem da localização de `hostfxr` falhou | A consulta de tamanho de `get_hostfxr_path` falhou: não está instalado nenhum runtime .NET correspondente (é necessário o runtime x64 do .NET 6). |
| 103 | Não foi possível obter o caminho de `hostfxr` | A segunda chamada a `get_hostfxr_path` falhou. |
| 104 | Não foi possível carregar a biblioteca `hostfxr` | `LoadLibraryW` sobre o `hostfxr.dll` resolvido falhou. |
| 105 | Faltam exportações obrigatórias de `hostfxr` | `hostfxr_initialize_for_dotnet_command_line`, `hostfxr_run_app` ou `hostfxr_close` não encontrado. |
| 106 | Não foi possível inicializar o host gerido | `hostfxr_initialize_for_dotnet_command_line` falhou para `OmsiLaunch.Controller.dll` (`OmsiLaunch.Controller.runtimeconfig.json` em falta, `Microsoft.WindowsDesktop.App` 6.0 x64 em falta, ou pacote danificado). |

<a id="classification-rules-cliprogramclassify"></a>
## Regras de classificação (`CliProgram.Classify`)

Cada exceção que escapa de `CliProgram.RunAsync` é convertida num envelope de erro (`CliInput.WriteError`) e num código de saída por `CliProgram.ReportFailure`, que chama `Classify`. As falhas de interpretação da linha de comando são tratadas da mesma forma antes do despacho (saída `2`). As regras aplicam-se por esta ordem:

1. É extraído o primeiro token `OL_E_` da mensagem da exceção (`ExtractCode`): o código é a sequência máxima de letras ASCII, dígitos e `_` que começa em `OL_E_`. Os códigos são apresentados literalmente em `error.code`.
2. `SessionProfileException` → o seu próprio `Code`, categoria `invalid_argument`, saída `2`.
3. `ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException` → código extraído ou `OL_E_INVALID_ARGUMENT`, categoria `invalid_argument`, saída `2`.
4. `FileNotFoundException`, `DirectoryNotFoundException` → código extraído ou `OL_E_NOT_FOUND`, categoria `not_found`, saída `6`.
5. `TimeoutException` → código extraído ou `OL_E_TIMEOUT`, categoria `runtime`, saída `5`.
6. `OperationCanceledException` → `OL_E_CANCELLED`, categoria `session`, saída `7`.
7. Caso contrário, quando foi extraído um código:
   - começa por `OL_E_RECOVERY_` ou é igual a `OL_E_RESTORE_FAILED` → categoria `transaction`, saída `8`;
   - `OL_E_INSTALLATION_BUSY`, `OL_E_SESSION_ALREADY_ACTIVE` → categoria `session`, saída `7`;
   - `OL_E_PLAN_NOT_RUNNABLE` → categoria `session`, saída `1`;
   - `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_ITX_PROFILE_REQUIRED` → categoria `invalid_argument`, saída `2`;
   - começa por `OL_E_UNSUPPORTED_` → categoria `unsupported_profile`, saída `3`;
   - qualquer outro código → categoria `runtime` para `InvalidOperationException` e `IOException`, caso contrário `internal`; saída `7`.
8. Nenhum código → `OL_E_INTERNAL`, categoria `internal`, saída `10`.

As respostas de clientes reencaminhadas não passam por `Classify`: `CliProgram.ReportForwarded` devolve `4` quando não há endpoint, `2` para `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED`, `7` para qualquer outra resposta `Ok=false` e `0` para `Ok=true`.

<a id="scripting-guidance"></a>
## Orientações para scripts

- Tratar `0` como êxito e tudo o resto como falha; ramificar pelo código numérico e, depois, por `error.code` do envelope `--json`.
- Um lançamento de sessão só regressa depois de a sessão ter terminado e os seus ficheiros terem sido restaurados; `1` significa que a transação foi executada mas o OMSI falhou ou o plano foi rejeitado, não que ficaram ficheiros modificados (um journal residual é reportado por `/recovery-status`).
- `100`..`106` significam que o pacote ou o runtime .NET está danificado; ver [instalação](../getting-started/installation.md) e [empacotamento](packaging.md).
