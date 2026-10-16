# Gerenciamento de HOF

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Tradução da [página original em inglês](../../../reference/hof-management.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O OMSI lê os arquivos HOF (`*.hof`: destinos, números de linha e listas de paradas exibidos pelo IBIS/letreiro de destino do veículo) da pasta de cada veículo. O gerenciamento de HOF copia arquivos HOF para pastas de veículos **durante uma sessão**: um mapa ou add-on pode distribuir os HOFs de que suas linhas precisam, e um usuário pode reutilizar os HOFs de um veículo em outros, sem alterar permanentemente nenhum veículo. Esta página é a referência normativa, conforme implementado por `HofPlanner` em `src/OmsiLaunch.Core/HofPlanner.cs`, pelo campo `HofManagement` do [LaunchSpec](launchspec.md), pelas flags `/hof-source`, `/hof-target`, `/hof-file` e `/hof-mode` da [CLI](cli.md) e pelo bloco `hofs` dos [perfis de sessão](session-profiles.md).

Estabilidade: `STABLE_BETA` para planejamento, regras de conflito, confinamento e restauração transacional (gates offline `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Validado em runtime: uma sessão com mirror, com restauração exata e recuperação após crash por meio de `/recover` (ver [status da validação em runtime](../status/runtime-validation-status.md)).

O gerenciamento de HOF não é o mesmo que `PlayerVehicle.Hof` / `/hof:<id>`, que seleciona o HOF do veículo do jogador no início e é `UNAVAILABLE` neste build.

<a id="model"></a>
## Modelo

Uma requisição é uma lista de operações (`HofManagementSpec.Operations`). Cada `HofOperationSpec` tem:

| Campo | Significado |
| --- | --- |
| `SourceKind`, `Source` | De onde vêm os arquivos HOF (tabela abaixo). |
| `TargetVehicles` | Um ou mais veículos instalados: a identidade de um arquivo `.bus` ou `.ovh` (`Vehicles\MAN_SD200\MAN_SD77.bus`) ou uma pasta de veículo (`Vehicles\MAN_SD200`), ambos abaixo de `Vehicles\`. Várias identidades em uma mesma pasta endereçam essa pasta uma única vez. |
| `Mode` | `Add` (padrão): copia os HOFs de origem para cada pasta de destino; os outros HOFs dessa pasta permanecem. `Mirror`: copia os HOFs de origem e remove todos os outros `.hof` da pasta de destino durante a sessão, de modo que o conjunto de HOFs da pasta seja igual ao conjunto de origem. |
| `Files` | Lista opcional de nomes de arquivo a obter da origem (sem diferenciar maiúsculas de minúsculas). Omitida: todos os `*.hof` diretamente na origem. Um nome listado que a origem não contém: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Resolve para |
| --- | --- | --- |
| `Collection` | um nome simples, por exemplo `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: uma coleção mantida pelo usuário ou por uma ferramenta. |
| `Directory` | um diretório relativo à instalação, por exemplo `maps\RMG Leste\hof` | `*.hof` nesse diretório: HOFs distribuídos junto com o conteúdo do mapa ou do add-on. |
| `Vehicle` | uma identidade ou pasta de veículo, como para os destinos | `*.hof` na pasta desse veículo: reutiliza os HOFs de um veículo em outros. |
| `File` | um arquivo `.hof` relativo à instalação | esse único arquivo. |

Somente arquivos diretamente no diretório de origem são usados (sem recursão). Links simbólicos e junctions nunca são seguidos (ver [confinamento](#confinement)).

<a id="what-a-session-does"></a>
## O que uma sessão faz

1. **Planejamento** (`PlanSessionAsync`, `/plan`). Toda operação é resolvida: as origens são lidas, os destinos são verificados, os conflitos são detectados. Cada arquivo que será alterado aparece em `SessionPlan.PlannedMutations` com `SemanticKey` `hof.add` ou `hof.mirror`, `RequestedValue` = a identidade da origem, e `Operation` `exact-file-overlay` (gravado) ou `session-deletion` (removido por um mirror); os caminhos fazem parte de `TouchedFiles`. Um problema torna o plano não apto para execução com os códigos abaixo.
2. **Início**. Depois que um journal obsoleto é recuperado, as operações são resolvidas novamente a partir dos arquivos atuais e entregues à transação da sessão: os arquivos gravados são overlays, os arquivos removidos pelo mirror são exclusões de sessão. Um arquivo de destino cujos bytes já são iguais aos da origem não é tocado e não é uma entrada da transação.
3. **Fim da sessão, falha, crash.** A transação restaura exatamente cada pasta de destino: os arquivos que existiam recebem de volta seus bytes, timestamps e atributos originais; os arquivos criados pela sessão são removidos; os arquivos removidos pelo mirror voltam. Após um crash do proprietário, a mesma restauração é executada no próximo início ou por meio de `/recover` (ver [transações e recuperação](../concepts/transactions-and-recovery.md)).

Nada fora das pastas de destino é gravado. Os arquivos de origem são apenas lidos. `.omsilaunch\hof\` nunca é modificado por uma sessão.

<a id="conflict-rules"></a>
## Regras de conflito

As operações são aplicadas em ordem e precisam concordar:

- Duas operações que gravam o mesmo arquivo de destino com os mesmos bytes são mescladas; com bytes diferentes: `OL_E_HOF_CONFLICT`.
- Um mirror que removeria um arquivo que uma operação anterior grava, ou uma operação que grava um arquivo que um mirror anterior remove: `OL_E_HOF_CONFLICT`.
- Um mirror abrange o conjunto de HOFs da sessão da pasta: os arquivos existentes mais os arquivos que operações anteriores gravam nela.
- Um destino de HOF que também é gravado pela apresentação da sessão (splash ou texturas da internet): `OL_E_HOF_CONFLICT` (não pode ocorrer com os caminhos de apresentação atuais, que ficam fora de `Vehicles\`).
- Quando um destino já tem um arquivo com o mesmo nome em outra combinação de maiúsculas e minúsculas, a grafia existente é mantida.

<a id="confinement"></a>
## Confinamento

Toda origem e todo destino devem permanecer dentro da instalação: caminhos absolutos, segmentos `..` e caminhos que saem da raiz geram `OL_E_HOF_PATH_ESCAPE`; o mesmo vale para destinos (e origens `Vehicle`) fora de `Vehicles\`, nomes de coleção que não são um nome de diretório simples e qualquer pasta de origem ou de destino alcançada por meio de uma junction ou de um link simbólico. Arquivos `.hof` que são eles próprios reparse points são ignorados.

<a id="limits"></a>
## Limites

| Limite | Valor | Erro |
| --- | --- | --- |
| Tamanho de um arquivo HOF | 8 MiB | `OL_E_HOF_INVALID` |
| Arquivos por operação | 256 | `OL_E_HOF_INVALID` |
| Total de bytes gravados por sessão | 256 MiB | `OL_E_HOF_INVALID` |
| Destinos por operação | pelo menos 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Códigos de erro

| Código | Causa |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | A coleção, o diretório, o veículo ou o arquivo não existe, não contém nenhum `.hof` ou não tem um arquivo indicado em `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Um arquivo ou pasta de veículo de destino não existe. |
| `OL_E_HOF_PATH_ESCAPE` | Um caminho sai da instalação, um destino não está abaixo de `Vehicles\`, um nome de coleção não é um nome simples ou uma junction/um link simbólico é atravessado. |
| `OL_E_HOF_CONFLICT` | Duas operações discordam sobre um arquivo de destino (ver [regras de conflito](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Nenhum destino, nenhuma origem, uma origem `File` que não é `.hof`, um destino que é um arquivo diferente de `.bus`/`.ovh`, ou um [limite](#limits) excedido. |

<a id="using-it"></a>
## Como usar

Linha de comando (uma operação; várias operações exigem `/spec` ou um perfil de sessão):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (JSON de `/spec`, enums como inteiros):

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

Predefinição de perfil de sessão (uma origem `package` é um diretório dentro do pacote do perfil):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: defina `LaunchSpec.HofManagement` e chame `PlanSessionAsync` / `StartSessionAsync` normalmente; `/list:Hofs` (`ContentQueryKind.Hofs`) lista os arquivos HOF instalados.

Relacionado: [LaunchSpec](launchspec.md), [perfis de sessão](session-profiles.md), [CLI](cli.md), [erros](errors.md), [transações e recuperação](../concepts/transactions-and-recovery.md).
