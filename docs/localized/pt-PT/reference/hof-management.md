# Gestão de HOF

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> Tradução da [página original em inglês](../../../reference/hof-management.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

O OMSI lê os ficheiros HOF (`*.hof`: destinos, números de linha e listas de paragens apresentados pelo IBIS/indicador de destino do veículo) da pasta de cada veículo. A gestão de HOF copia ficheiros HOF para as pastas dos veículos **durante uma sessão**: um mapa ou add-on pode incluir os HOF de que as suas linhas precisam, e um utilizador pode reutilizar os HOF de um veículo noutros, sem alterar permanentemente nenhum veículo. Esta página é a referência normativa, tal como implementada por `HofPlanner` em `src/OmsiLaunch.Core/HofPlanner.cs`, pelo campo `HofManagement` do [LaunchSpec](launchspec.md), pelas flags `/hof-source`, `/hof-target`, `/hof-file` e `/hof-mode` da [CLI](cli.md) e pelo bloco `hofs` dos [perfis de sessão](session-profiles.md).

Estabilidade: `STABLE_BETA` para o planeamento, as regras de conflito, o confinamento e o restauro transacional (gates offline `hof.plan-sources-and-modes`, `hof.plan-rejects-unsafe-input`, `hof.transaction-exact-restore`, `hof.recovery-after-crash`, `session-profiles.hofs`). Validado em runtime: uma sessão de espelhamento com restauro exato e recuperação após falha através de `/recover` (ver [estado da validação em runtime](../status/runtime-validation-status.md)).

A gestão de HOF não é o mesmo que `PlayerVehicle.Hof` / `/hof:<id>`, que seleciona o HOF do veículo do jogador no arranque e é `UNAVAILABLE` nesta build.

<a id="model"></a>
## Modelo

Um pedido é uma lista de operações (`HofManagementSpec.Operations`). Cada `HofOperationSpec` tem:

| Campo | Significado |
| --- | --- |
| `SourceKind`, `Source` | De onde vêm os ficheiros HOF (tabela abaixo). |
| `TargetVehicles` | Um ou mais veículos instalados: a identidade de um ficheiro `.bus` ou `.ovh` (`Vehicles\MAN_SD200\MAN_SD77.bus`) ou de uma pasta de veículo (`Vehicles\MAN_SD200`), ambas abaixo de `Vehicles\`. Várias identidades na mesma pasta designam essa pasta uma só vez. |
| `Mode` | `Add` (predefinição): copia os HOF de origem para cada pasta de destino; os outros HOF aí existentes mantêm-se. `Mirror`: copia os HOF de origem e remove todos os outros `.hof` da pasta de destino durante a sessão, de modo que o conjunto de HOF da pasta fique igual ao conjunto de origem. |
| `Files` | Lista opcional de nomes de ficheiro a tirar da origem (sem distinção de maiúsculas/minúsculas). Omitida: todos os `*.hof` diretamente na origem. Um nome listado que a origem não contenha: `OL_E_HOF_SOURCE_NOT_FOUND`. |

| `SourceKind` | `Source` | Resolve para |
| --- | --- | --- |
| `Collection` | um nome simples, por exemplo `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`: uma coleção mantida pelo utilizador ou por uma ferramenta. |
| `Directory` | um diretório relativo à instalação, por exemplo `maps\RMG Leste\hof` | `*.hof` nesse diretório: HOF distribuídos junto ao conteúdo de um mapa ou add-on. |
| `Vehicle` | uma identidade ou pasta de veículo, como nos destinos | `*.hof` na pasta desse veículo: reutilizar os HOF de um veículo noutros. |
| `File` | um ficheiro `.hof` relativo à instalação | esse único ficheiro. |

Só são usados os ficheiros diretamente no diretório de origem (sem recursão). As ligações simbólicas e as junctions nunca são seguidas (ver [confinamento](#confinement)).

<a id="what-a-session-does"></a>
## O que faz uma sessão

1. **Planeamento** (`PlanSessionAsync`, `/plan`). Cada operação é resolvida: as origens são lidas, os destinos são verificados, os conflitos são detetados. Cada ficheiro que vai mudar aparece em `SessionPlan.PlannedMutations` com `SemanticKey` `hof.add` ou `hof.mirror`, `RequestedValue` = a identidade da origem e `Operation` `exact-file-overlay` (escrito) ou `session-deletion` (removido por um espelhamento); os caminhos fazem parte de `TouchedFiles`. Um problema torna o plano não executável com os códigos abaixo.
2. **Arranque**. Depois de um journal obsoleto ser recuperado, as operações são resolvidas de novo a partir dos ficheiros em uso e entregues à transação da sessão: os ficheiros escritos são overlays, os ficheiros afastados por espelhamento são eliminações de sessão. Um ficheiro de destino cujos bytes já são iguais à origem é deixado como está e não é uma entrada da transação.
3. **Fim da sessão, falha, falha do processo.** A transação restaura cada pasta de destino com exatidão: os ficheiros que existiam recuperam os seus bytes, datas e atributos originais; os ficheiros que a sessão criou são removidos; os ficheiros afastados por espelhamento regressam. Após uma falha do proprietário, o mesmo restauro corre no arranque seguinte ou através de `/recover` (ver [transações e recuperação](../concepts/transactions-and-recovery.md)).

Nada fora das pastas de destino é escrito. Os ficheiros de origem só são lidos. `.omsilaunch\hof\` nunca é modificado por uma sessão.

<a id="conflict-rules"></a>
## Regras de conflito

As operações são aplicadas por ordem e têm de estar de acordo:

- Duas operações que escrevem o mesmo ficheiro de destino com os mesmos bytes fundem-se; com bytes diferentes: `OL_E_HOF_CONFLICT`.
- Um espelhamento que removeria um ficheiro que uma operação anterior escreve, ou uma operação que escreve um ficheiro que um espelhamento anterior remove: `OL_E_HOF_CONFLICT`.
- Um espelhamento abrange o conjunto de HOF da sessão da pasta: os ficheiros que existem mais os ficheiros que as operações anteriores aí escrevem.
- Um destino de HOF que é também escrito pela apresentação da sessão (splash ou Internet Textures): `OL_E_HOF_CONFLICT` (não pode acontecer com os caminhos de apresentação atuais, que estão fora de `Vehicles\`).
- Quando um destino já tem um ficheiro com o mesmo nome em maiúsculas/minúsculas diferentes, mantém-se a grafia existente.

<a id="confinement"></a>
## Confinamento

Toda a origem e todo o destino têm de ficar dentro da instalação: caminhos absolutos, segmentos `..` e caminhos que saem da raiz são `OL_E_HOF_PATH_ESCAPE`; o mesmo acontece com destinos (e origens `Vehicle`) fora de `Vehicles\`, nomes de coleção que não são um nome de diretório simples e qualquer origem ou pasta de destino alcançada através de uma junction ou ligação simbólica. Os ficheiros `.hof` que são eles próprios pontos de reanálise (reparse points) são ignorados.

<a id="limits"></a>
## Limites

| Limite | Valor | Erro |
| --- | --- | --- |
| Tamanho de um ficheiro HOF | 8 MiB | `OL_E_HOF_INVALID` |
| Ficheiros por operação | 256 | `OL_E_HOF_INVALID` |
| Total de bytes escritos por sessão | 256 MiB | `OL_E_HOF_INVALID` |
| Destinos por operação | pelo menos 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## Códigos de erro

| Código | Causa |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | A coleção, o diretório, o veículo ou o ficheiro não existe, não contém nenhum `.hof`, ou falta-lhe um ficheiro indicado em `Files`. |
| `OL_E_HOF_TARGET_NOT_FOUND` | Um ficheiro ou pasta de veículo de destino não existe. |
| `OL_E_HOF_PATH_ESCAPE` | Um caminho sai da instalação, um destino não está abaixo de `Vehicles\`, um nome de coleção não é um nome simples, ou é atravessada uma junction/ligação simbólica. |
| `OL_E_HOF_CONFLICT` | Duas operações discordam sobre um ficheiro de destino (ver [regras de conflito](#conflict-rules)). |
| `OL_E_HOF_INVALID` | Sem destinos, sem origem, uma origem `File` que não é `.hof`, um destino que é um ficheiro diferente de `.bus`/`.ovh`, ou um [limite](#limits) excedido. |

<a id="using-it"></a>
## Utilização

Linha de comandos (uma operação; várias operações exigem `/spec` ou um perfil de sessão):

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec (JSON de `/spec`, enumerações como inteiros):

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

Predefinição (preset) de perfil de sessão (uma origem `package` é um diretório dentro do pacote do perfil):

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: definir `LaunchSpec.HofManagement` e chamar `PlanSessionAsync` / `StartSessionAsync` como habitualmente; `/list:Hofs` (`ContentQueryKind.Hofs`) lista os ficheiros HOF instalados.

Relacionado: [LaunchSpec](launchspec.md), [perfis de sessão](session-profiles.md), [CLI](cli.md), [erros](errors.md), [transações e recuperação](../concepts/transactions-and-recovery.md).
