# Compatibilidade

<!-- l10n: source=reference/compatibility.md source-sha256=9f3a7654d57410845e20c92eccc746868cbac665b4d937581f16c3818d7b7b88 -->
> Tradução da [página original em inglês](../../../reference/compatibility.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

O OmsiLaunch funciona com um **OMSI 2** instalado. Ele não exige um arquivo `Omsi.exe` específico: toda suposição nativa que faz dentro do OMSI (uma função, um ponto de patch, uma classe, um slot de VMT, uma global, um deslocamento de campo) é resolvida e verificada individualmente, no processo, antes de ser usada, e uma capacidade só está disponível quando todas as suas suposições estão verificadas. Esta página indica o que significa "OMSI 2 instalado", como os builds são identificados, como as capacidades são resolvidas e quais são os requisitos de sistema operacional e de runtime. Fontes: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Requisito: OMSI 2 instalado

Um plano é recusado com `OL_E_UNSUPPORTED_BUILD` somente quando a instalação não é uma instalação do OMSI 2:

| Verificação | Recusado quando |
| --- | --- |
| `Omsi.exe` | ausente da raiz da instalação |
| Imagem | não é uma imagem PE x86 de 32 bits |
| Versão | seu recurso de versão não traz uma versão do OMSI 2 (2.x) |
| Instalação | a raiz não tem o diretório `maps\` nem `Vehicles\` |

O SHA-256, o tamanho e a versão exata do executável **não** são critérios de compatibilidade. Eles são registrados como diagnósticos (`omsi.build.detected`, `OmsiLaunch.exe profiles`) e servem como evidência.

<a id="build-identity-and-capability-resolution"></a>
## Identidade do build e resolução de capacidades

| Conceito | Significado |
| --- | --- |
| Identidade de código | SHA-256 da seção de código principal do executável. Uma identidade conhecida seleciona um layout conhecido como caminho rápido. Variantes que diferem apenas nos cabeçalhos (por exemplo, uma cópia Large Address Aware) compartilham a identidade de código. |
| Layout conhecido | `Omsi23004`: OMSI 2.3.004 (versão de arquivo 2.2.032), identidade de código `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Toda validação em runtime da [página de status](../status/runtime-validation-status.md) foi executada neste layout. |
| Destino nativo | Uma suposição da fronteira nativa, declarada no catálogo de builds com sua guarda. |
| Capacidade | Um grupo de destinos, por exemplo `world.new-map` ou `time.read`. Disponível somente quando todo destino está resolvido **e** verificado. |

Como um destino é resolvido, no plugin, antes que o OmsiLaunch aplique qualquer patch:

1. **Identidade conhecida:** o endereço de referência do layout conhecido é tentado primeiro, e ele ainda precisa passar pela guarda do destino.
2. **Caso contrário, ou quando a guarda falha**, são tentadas estratégias independentes de build:
   - nome de classe RTTI do Delphi (`vmtSelfPtr` verificado);
   - métodos, campos e propriedades publicados de uma classe (tabelas RTTI);
   - slot de VMT que contém um método resolvido;
   - uma assinatura de código com máscara que precisa corresponder exatamente uma vez (os operandos que dependem do layout são curingas), capturando um endereço, uma global (diretamente ou pela sua célula de ponteiro) ou um deslocamento de campo a partir de dentro de um método da própria classe do campo.
3. **Nada corresponde de forma única e passa pela guarda:** o destino permanece não resolvido. **Não há fallback para um endereço fixo.**

Guardas: bytes de entrada com máscara para funções e pontos de patch, o destino da chamada para uma chamada com patch, `vmtSelfPtr` e o nome de classe RTTI para classes, o método mantido para slots de VMT e uma seção de dados gravável para globais. Os layouts de objetos lidos pelos leitores de runtime (registros de veículo, humano, clima, tabela de horários e assim por diante) ainda não têm validação estrutural independente de build. Eles são verificados somente para uma identidade de código conhecida, portanto as capacidades que os leem ficam indisponíveis em um build com identidade desconhecida.

No layout conhecido, toda capacidade é resolvida. Em um build com identidade desconhecida, as capacidades cujos destinos são resolvidos pelas estratégias acima (o início headless, NEW_MAP, situações salvas, supressão de Internet Textures, leitura e definição de hora, D3D) podem estar disponíveis, e cada uma é verificada individualmente; as leituras que dependem de layout ficam indisponíveis.

<a id="where-compatibility-is-decided"></a>
## Onde a compatibilidade é decidida

| Etapa | O que acontece |
| --- | --- |
| Planejamento (`PlanSessionAsync`, `/plan`) | Verificação de OMSI 2 (`omsi.omsi2`, caso contrário `OL_E_UNSUPPORTED_BUILD`). A imagem do arquivo é resolvida como uma **previsão**: as capacidades nativas exigidas trazem `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION` com o aviso `OL_W_CAPABILITY_PENDING`. Uma identidade de código desconhecida acrescenta o aviso `OL_W_BUILD_UNRECOGNIZED`. Os avisos não tornam um plano não apto para execução. |
| Início (`StartSessionAsync`) | A spec é replanejada; um plano que deixou de estar apto para execução é rejeitado com `OL_E_PLAN_NOT_RUNNABLE`. |
| No processo (início do plugin) | A imagem carregada é resolvida (autoritativa; um executável protegido por DRM pode diferir do seu arquivo). O relatório é enviado como telemetria `native.capabilities`. Se `boot.headless-start` e a capacidade de modo de mundo (`world.new-map` ou `world.saved-situation`) não estão disponíveis, a sessão falha com `OL_E_CAPABILITY_UNRESOLVED` antes de qualquer hook, e os arquivos são restaurados. Se elas estão disponíveis, mas o formulário Start ativo não está no estado esperado (por exemplo, outro plugin o modificou), a sessão falha com `OL_E_BUILD_VALIDATION_FAILED`. |
| Operações de runtime | Uma operação cuja capacidade está indisponível é rejeitada com `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) sem tocar na memória do OMSI. |

`OmsiLaunch.exe profiles --json` informa a instalação (`is_omsi2`, `build_id`, `identity`, hashes de arquivo e de código, `file_variant`), os layouts conhecidos e as capacidades previstas a partir do arquivo.

<a id="operating-system-and-architecture"></a>
## Sistema operacional e arquitetura

`CurrentWindowsX64Platform.Detect` calcula `RuntimePlatformInfo`. A plataforma atual só é suportada quando todas as condições a seguir são atendidas:

| Requisito | Verificação | Erro quando violado |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 ou posterior | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows de 64 bits e um processo host de 64 bits | `OSArchitecture == X64` e `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Instalação gravável | O diretório raiz existe, não é somente leitura e contém `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` também informa `OmsiArchitecture` e `PluginArchitecture` como `X86` (o OMSI é um processo de 32 bits; o conjunto de arquivos do plugin é x86 e é executado sob WOW64), `LegacyPlatform=false` e `Wow64Available`. O Windows ARM64 não é suportado, mesmo onde existe emulação x64, porque o próprio processo host precisa ser x64.

<a id="net-requirements"></a>
## Requisitos do .NET

| Componente | Runtime | Observações |
| --- | --- | --- |
| Controlador (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | O bootstrapper nativo localiza o runtime por meio do `hostfxr`, usando o `nethost.dll` incluído no pacote. Um runtime ausente é informado pelo shim (códigos de saída 100-106; veja [CLI](cli.md) e [códigos de saída](exit-codes.md)). |
| Conjunto de arquivos do plugin (`plugins\OmsiLaunch.Plugin.dll` via `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), hospedado pelo DNNE 2.0.6 dentro do `Omsi.exe` | Exige que o runtime .NET 6 Desktop/Core x86 esteja instalado na máquina; o runtime de 64 bits sozinho não é suficiente para o plugin. |
| Ponte nativa (`plugins\OmsiLaunch.Native.x86.dll`) | x86 nativo | Carregada somente a partir de `plugins\` (veja [plugin permanente](../concepts/permanent-plugin.md)). Ela não contém nenhum endereço do OMSI: recebe do plugin a tabela de destinos verificados. |

<a id="legacy-platforms"></a>
## Plataformas legadas

Windows 7, Windows 8.x, Windows XP e outros sistemas NT 6 e anteriores estão fora do limite de suporte atual. `RuntimePlatformInfo.LegacyPlatform` é sempre `false` e não existe nenhum adaptador legado; o campo e a interface `IPluginNativeServices` existem apenas para que um futuro adaptador legado possa ser adicionado sem alterar a API pública (veja `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nada nesta release é executado nesses sistemas.

<a id="steam-and-large-address-aware-notes"></a>
## Observações sobre Steam e Large Address Aware

- Uma cópia Large Address Aware que difere apenas nos cabeçalhos tem a mesma identidade de código que o original e usa o mesmo layout conhecido.
- O executável da Steam é protegido por DRM em disco, portanto seu arquivo pode diferir da imagem carregada: o plano pode informar `OL_W_CAPABILITY_PENDING`, e a resolução no processo decide.
- A Steam inicia o próprio OMSI; uma sessão precisa ser iniciada pelo `OmsiLaunch.exe` para que o handoff exista. Quando o OMSI é iniciado pela Steam, o plugin permanente permanece inerte (sem handoff, sem hooks).

<a id="related-pages"></a>
## Páginas relacionadas

- [Limitações conhecidas](known-limitations.md)
- [Status de validação em runtime](../status/runtime-validation-status.md)
- [Instalação](../getting-started/installation.md)
