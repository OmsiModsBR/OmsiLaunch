# Compatibilidade

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> Tradução da [página original em inglês](../../../reference/compatibility.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: em caso de divergência, prevalecem a página em inglês e o código.

O OmsiLaunch funciona com um **OMSI 2** instalado. Não exige um ficheiro `Omsi.exe` em particular: cada pressuposto nativo que faz dentro do OMSI (uma função, um ponto de patch, uma classe, um slot de VMT, uma global, um offset de campo) é resolvido e verificado individualmente, em processo, antes de ser usado, e uma capacidade só está disponível quando todos os seus pressupostos estão verificados. Esta página indica o que significa «OMSI 2 instalado», como as builds são identificadas, como as capacidades são resolvidas e quais são os requisitos de sistema operativo e de runtime. Fontes: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Requisito: OMSI 2 instalado

Um plano só é recusado com `OL_E_UNSUPPORTED_BUILD` quando a instalação não é uma instalação do OMSI 2:

| Verificação | Recusado quando |
| --- | --- |
| `Omsi.exe` | em falta na raiz da instalação |
| Imagem | não é uma imagem PE x86 de 32 bits |
| Versão | o seu recurso de versão não traz uma versão do OMSI 2 (2.x) |
| Instalação | a raiz não tem o diretório `maps\` nem `Vehicles\` |

O SHA-256, o tamanho e a versão exata do executável **não** são critérios de compatibilidade. São registados como diagnóstico (`omsi.build.detected`, `OmsiLaunch.exe profiles`) e servem de evidência.

<a id="build-identity-and-capability-resolution"></a>
## Identidade da build e resolução de capacidades

| Conceito | Significado |
| --- | --- |
| Identidade de código | SHA-256 da secção de código principal do executável. Uma identidade conhecida seleciona uma disposição conhecida como caminho rápido. As variantes que diferem apenas nos cabeçalhos (por exemplo uma cópia Large Address Aware) partilham a identidade de código. |
| Disposição conhecida | `Omsi23004`: OMSI 2.3.004 (versão de ficheiro 2.2.032), identidade de código `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Todas as validações em runtime da [página de estado](../status/runtime-validation-status.md) correram nesta disposição. |
| Destino nativo | Um pressuposto da fronteira nativa, declarado no catálogo de builds com a sua guarda. |
| Capacidade | Um grupo de destinos, por exemplo `world.new-map` ou `time.read`. Disponível apenas quando todos os destinos estão resolvidos **e** verificados. |

Como um destino é resolvido, no plugin, antes de o OmsiLaunch modificar o que quer que seja:

1. **Identidade conhecida:** tenta-se primeiro o endereço de referência da disposição conhecida, que tem de continuar a passar a guarda do destino.
2. **Caso contrário, ou quando a guarda falha**, tentam-se estratégias independentes da build:
   - nome de classe RTTI do Delphi (`vmtSelfPtr` verificado);
   - métodos, campos e propriedades publicados de uma classe (tabelas RTTI);
   - slot de VMT que contém um método resolvido;
   - uma assinatura de código mascarada que tem de coincidir exatamente uma vez (os operandos que dependem da disposição são carateres universais), capturando um endereço, uma global (diretamente ou através da sua célula de ponteiro) ou um deslocamento de campo a partir do interior de um método da própria classe do campo.
3. **Nada coincide de forma única e passa a guarda:** o destino permanece por resolver. **Não há recurso a um endereço fixo.**

Guardas: bytes de entrada mascarados para funções e pontos de patch, o destino da chamada para uma chamada modificada, `vmtSelfPtr` e o nome de classe RTTI para classes, o método contido para slots de VMT e uma secção de dados gravável para globais. As disposições de objetos lidas pelos leitores de runtime (registos de veículo, humano, meteorologia, horário e assim por diante) ainda não têm validação estrutural independente da build. São verificadas apenas para uma identidade de código conhecida, pelo que as capacidades que as leem estão indisponíveis numa build com identidade desconhecida.

Na disposição conhecida, todas as capacidades são resolvidas. Numa build com identidade desconhecida, as capacidades cujos destinos são resolvidos pelas estratégias acima (o arranque headless, NEW_MAP, situações guardadas, supressão de Internet Textures, leitura e definição da hora, D3D) podem estar disponíveis, cada uma verificada individualmente; as leituras que dependem da disposição ficam indisponíveis.

<a id="where-compatibility-is-decided"></a>
## Onde a compatibilidade é decidida

| Fase | O que acontece |
| --- | --- |
| Planeamento (`PlanSessionAsync`, `/plan`) | Verificação de OMSI 2 (`omsi.omsi2`, caso contrário `OL_E_UNSUPPORTED_BUILD`). A imagem do ficheiro é resolvida como **previsão**: as capacidades nativas exigidas transportam `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` ou `PENDING_IN_PROCESS_RESOLUTION` com o aviso `OL_W_CAPABILITY_PENDING`. Uma identidade de código desconhecida acrescenta o aviso `OL_W_BUILD_UNRECOGNIZED`. Os avisos não tornam um plano não executável. |
| Arranque (`StartSessionAsync`) | A especificação é planeada de novo; um plano que já não é executável é rejeitado com `OL_E_PLAN_NOT_RUNNABLE`. |
| Em processo (arranque do plugin) | A imagem carregada é resolvida (autoritativa; um executável protegido por DRM pode diferir do seu ficheiro). O relatório é enviado como telemetria `native.capabilities`. Se `boot.headless-start` e a capacidade de modo de mundo (`world.new-map` ou `world.saved-situation`) não estiverem disponíveis, a sessão falha com `OL_E_CAPABILITY_UNRESOLVED` antes de qualquer hook, e os ficheiros são restaurados. Se estiverem disponíveis, mas o formulário Start em memória não estiver no estado esperado (por exemplo, outro plugin o modificou), a sessão falha com `OL_E_BUILD_VALIDATION_FAILED`. |
| Operações de runtime | Uma operação cuja capacidade está indisponível é rejeitada com `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) sem tocar na memória do OMSI. |

`OmsiLaunch.exe profiles --json` comunica a instalação (`is_omsi2`, `build_id`, `identity`, hashes do ficheiro e do código, `file_variant`), as disposições conhecidas e as capacidades previstas a partir do ficheiro.

<a id="operating-system-and-architecture"></a>
## Sistema operativo e arquitetura

`CurrentWindowsX64Platform.Detect` calcula `RuntimePlatformInfo`. A plataforma atual só é suportada quando todas as condições seguintes se verificam:

| Requisito | Verificação | Erro quando não é cumprido |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 ou posterior | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows de 64 bits e um processo host de 64 bits | `OSArchitecture == X64` e `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Instalação com permissão de escrita | O diretório raiz existe, não é só de leitura e contém `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` também reporta `OmsiArchitecture` e `PluginArchitecture` como `X86` (o OMSI é um processo de 32 bits; o conjunto fechado do plugin é x86 e é executado sob WOW64), `LegacyPlatform=false` e `Wow64Available`. O Windows ARM64 não é suportado, mesmo onde existe emulação x64, porque o próprio processo host tem de ser x64.

<a id="net-requirements"></a>
## Requisitos do .NET

| Componente | Runtime | Notas |
| --- | --- | --- |
| Controlador (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | O bootstrapper nativo localiza o runtime através de `hostfxr`, por meio do `nethost.dll` incluído no pacote. Um runtime em falta é reportado pelo shim (códigos de saída 100-106; ver [CLI](cli.md) e [códigos de saída](exit-codes.md)). |
| Conjunto fechado do plugin (`plugins\OmsiLaunch.Plugin.dll` através de `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), alojado pelo DNNE 2.0.6 dentro de `Omsi.exe` | Requer que o runtime x86 do .NET 6 Desktop/Core esteja instalado na máquina; o runtime de 64 bits por si só não é suficiente para o plugin. |
| Ponte nativa (`plugins\OmsiLaunch.Native.x86.dll`) | x86 nativo | Carregada apenas a partir de `plugins\` (ver [plugin permanente](../concepts/permanent-plugin.md)). Não contém nenhum endereço do OMSI: recebe do plugin a tabela de destinos verificada. |

<a id="legacy-platforms"></a>
## Plataformas legadas

O Windows 7, o Windows 8.x, o Windows XP e outros sistemas NT 6 e anteriores estão fora do limite de suporte atual. `RuntimePlatformInfo.LegacyPlatform` é sempre `false` e não existe nenhum adaptador legado; o campo e o ponto de extensão `IPluginNativeServices` existem apenas para que um futuro adaptador legado possa ser acrescentado sem alterar a API pública (ver `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nada nesta versão é executado nesses sistemas.

<a id="steam-and-large-address-aware-notes"></a>
## Notas sobre Steam e Large Address Aware

- Uma cópia Large Address Aware que difere apenas nos cabeçalhos tem a mesma identidade de código do original e usa a mesma disposição conhecida.
- O executável Steam está protegido por DRM em disco, pelo que o seu ficheiro pode diferir da imagem carregada: o plano pode comunicar `OL_W_CAPABILITY_PENDING` e a resolução em processo decide. Uma sessão com o executável Steam genuíno não foi validada em runtime (ver [estado da validação em runtime](../status/runtime-validation-status.md)).
- O Steam lança o próprio OMSI; uma sessão tem de ser iniciada através de `OmsiLaunch.exe` para que o handoff exista. Quando o OMSI é iniciado a partir do Steam, o plugin permanente permanece inerte (sem handoff, sem hooks).

<a id="related-pages"></a>
## Páginas relacionadas

- [Limitações conhecidas](known-limitations.md)
- [Estado da validação em runtime](../status/runtime-validation-status.md)
- [Instalação](../getting-started/installation.md)
