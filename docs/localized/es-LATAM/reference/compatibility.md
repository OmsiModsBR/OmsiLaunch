# Compatibilidad

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> Traducción de la [página original en inglés](../../../reference/compatibility.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si hay diferencias, prevalecen la página en inglés y el código.

OmsiLaunch funciona con un **OMSI 2** instalado. No exige un archivo `Omsi.exe` en particular: cada suposición nativa que hace dentro de OMSI (una función, un sitio de parche, una clase, un slot de VMT, un global, un desplazamiento de campo) se resuelve y verifica individualmente, en el proceso, antes de usarse, y una capacidad solo está disponible cuando todas sus suposiciones están verificadas. Esta página explica qué significa "OMSI 2 instalado", cómo se identifican los builds, cómo se resuelven las capacidades y los requisitos de sistema operativo y de runtime. Fuentes: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`, `src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`, `src/OmsiLaunch.NativeResolution/`, `src/OmsiLaunch.Plugin/PluginNativeTargets.cs`, `src/OmsiLaunch.Core/SessionPlanner.cs`, `src/OmsiLaunch.Process/RuntimePlatform.cs`.

<a id="requirement-omsi-2-installed"></a>
## Requisito: OMSI 2 instalado

Un plan se rechaza con `OL_E_UNSUPPORTED_BUILD` solo cuando la instalación no es una instalación de OMSI 2:

| Comprobación | Se rechaza cuando |
| --- | --- |
| `Omsi.exe` | falta en la raíz de la instalación |
| Imagen | no es una imagen PE x86 de 32 bits |
| Versión | su recurso de versión no lleva una versión de OMSI 2 (2.x) |
| Instalación | la raíz no tiene el directorio `maps\` ni `Vehicles\` |

El SHA-256, el tamaño y la versión exacta del archivo ejecutable **no** son criterios de compatibilidad. Se registran como diagnósticos (`omsi.build.detected`, `OmsiLaunch.exe profiles`) y sirven como evidencia.

<a id="build-identity-and-capability-resolution"></a>
## Identidad del build y resolución de capacidades

| Concepto | Significado |
| --- | --- |
| Identidad de código | SHA-256 de la sección de código principal del archivo ejecutable. Una identidad conocida selecciona un diseño conocido como vía rápida. Las variantes que difieren solo en los encabezados (por ejemplo, una copia Large Address Aware) comparten la identidad de código. |
| Diseño conocido | `Omsi23004`: OMSI 2.3.004 (versión de archivo 2.2.032), identidad de código `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`. Toda validación en runtime de la [página de estado](../status/runtime-validation-status.md) se ejecutó sobre este diseño. |
| Destino nativo | Una suposición del límite nativo, declarada en el catálogo de builds con su guarda. |
| Capacidad | Un grupo de destinos, por ejemplo `world.new-map` o `time.read`. Solo está disponible cuando todos los destinos están resueltos **y** verificados. |

Cómo se resuelve un destino, en el plugin, antes de que OmsiLaunch parche nada:

1. **Identidad conocida:** primero se prueba la dirección de referencia del diseño conocido, que debe seguir superando la guarda del destino.
2. **En caso contrario, o cuando falla la guarda**, se prueban estrategias independientes del build:
   - nombre de clase RTTI de Delphi (se comprueba `vmtSelfPtr`);
   - métodos, campos y propiedades publicados de una clase (tablas RTTI);
   - slot de VMT que contiene un método resuelto;
   - una firma de código enmascarada que debe coincidir exactamente una vez (los operandos que dependen del diseño son comodines), que captura una dirección, un global (directamente o a través de su celda de puntero) o un desplazamiento de campo desde dentro de un método de la propia clase del campo.
3. **Nada coincide de forma única y supera la guarda:** el destino queda sin resolver. **No hay respaldo a una dirección fija.**

Guardas: bytes de entrada enmascarados para funciones y sitios de parche, el destino de la llamada para una llamada parchada, `vmtSelfPtr` y el nombre de clase RTTI para las clases, el método contenido para los slots de VMT y una sección de datos escribible para los globales. Los diseños de objetos que leen los lectores de runtime (registros de vehículo, humano, clima, horario, etc.) todavía no tienen una validación estructural independiente del build. Solo se verifican para una identidad de código conocida, por lo que las capacidades que los leen no están disponibles en un build con una identidad desconocida.

En el diseño conocido se resuelven todas las capacidades. En un build con una identidad desconocida, las capacidades cuyos destinos se resuelven mediante las estrategias anteriores (el inicio headless, NEW_MAP, las situaciones guardadas, la supresión de Internet Textures, la lectura y el ajuste de la hora, D3D) pueden estar disponibles, y cada una se verifica individualmente; las lecturas que dependen del diseño no están disponibles.

<a id="where-compatibility-is-decided"></a>
## Dónde se decide la compatibilidad

| Etapa | Qué sucede |
| --- | --- |
| Planificación (`PlanSessionAsync`, `/plan`) | Comprobación de OMSI 2 (`omsi.omsi2`, de lo contrario `OL_E_UNSUPPORTED_BUILD`). La imagen del archivo se resuelve como una **predicción**: las capacidades nativas requeridas llevan `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` o `PENDING_IN_PROCESS_RESOLUTION` con la advertencia `OL_W_CAPABILITY_PENDING`. Una identidad de código desconocida agrega la advertencia `OL_W_BUILD_UNRECOGNIZED`. Las advertencias no hacen que un plan no sea ejecutable. |
| Inicio (`StartSessionAsync`) | La especificación se vuelve a planificar; un plan que ya no es ejecutable se rechaza con `OL_E_PLAN_NOT_RUNNABLE`. |
| En el proceso (inicio del plugin) | Se resuelve la imagen cargada (es la que prevalece; un ejecutable protegido con DRM puede diferir de su archivo). El informe se envía como telemetría `native.capabilities`. Si `boot.headless-start` y la capacidad del modo de mundo (`world.new-map` o `world.saved-situation`) no están disponibles, la sesión falla con `OL_E_CAPABILITY_UNRESOLVED` antes de cualquier hook y los archivos se restauran. Si están disponibles pero el formulario Start en vivo no está en el estado esperado (por ejemplo, otro plugin lo parchó), la sesión falla con `OL_E_BUILD_VALIDATION_FAILED`. |
| Operaciones de runtime | Una operación cuya capacidad no está disponible se rechaza con `OL_E_CAPABILITY_UNRESOLVED` (`Data["capability"]`) sin tocar la memoria de OMSI. |

`OmsiLaunch.exe profiles --json` informa la instalación (`is_omsi2`, `build_id`, `identity`, hashes del archivo y del código, `file_variant`), los diseños conocidos y las capacidades predichas a partir del archivo.

<a id="operating-system-and-architecture"></a>
## Sistema operativo y arquitectura

`CurrentWindowsX64Platform.Detect` calcula `RuntimePlatformInfo`. La plataforma actual es compatible solo cuando se cumplen todas las condiciones siguientes:

| Requisito | Comprobación | Error cuando no se cumple |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 o posterior | `Environment.OSVersion.Version.Major >= 10` (Windows 10, Windows 11, Server 2016+) | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| Windows de 64 bits y un proceso host de 64 bits | `OSArchitecture == X64` y `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| Instalación con permiso de escritura | El directorio raíz existe, no es de solo lectura y contiene `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` también informa `OmsiArchitecture` y `PluginArchitecture` como `X86` (OMSI es un proceso de 32 bits; el conjunto del plugin es x86 y se ejecuta bajo WOW64), `LegacyPlatform=false` y `Wow64Available`. Windows ARM64 no es compatible, aunque exista emulación x64, porque el propio proceso host debe ser x64.

<a id="net-requirements"></a>
## Requisitos de .NET

| Componente | Runtime | Notas |
| --- | --- | --- |
| Controlador (`OmsiLaunch.exe`, `OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`) | .NET 6, x64 | El bootstrapper nativo localiza el runtime a través de `hostfxr` mediante el `nethost.dll` incluido en el paquete. El shim informa la ausencia del runtime (códigos de salida 100-106; consulte [CLI](cli.md) y [códigos de salida](exit-codes.md)). |
| Conjunto del plugin (`plugins\OmsiLaunch.Plugin.dll` mediante `OmsiLaunch.PluginNE.dll`) | .NET 6, **x86** (`net6.0-windows`, `win-x86`), alojado por DNNE 2.0.6 dentro de `Omsi.exe` | Requiere que el runtime Desktop/Core de .NET 6 x86 esté instalado en la computadora; el runtime de 64 bits por sí solo no es suficiente para el plugin. |
| Puente nativo (`plugins\OmsiLaunch.Native.x86.dll`) | x86 nativo | Se carga solo desde `plugins\` (consulte [plugin permanente](../concepts/permanent-plugin.md)). No contiene ninguna dirección de OMSI: recibe del plugin la tabla de destinos verificados. |

<a id="legacy-platforms"></a>
## Plataformas heredadas

Windows 7, Windows 8.x, Windows XP y otros sistemas NT 6 y anteriores quedan fuera del límite de compatibilidad actual. `RuntimePlatformInfo.LegacyPlatform` siempre es `false` y no existe ningún adaptador heredado; el campo y el punto de extensión `IPluginNativeServices` existen solo para que en el futuro se pueda agregar un adaptador heredado sin cambiar la API pública (consulte `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`). Nada de esta release se ejecuta en esos sistemas.

<a id="steam-and-large-address-aware-notes"></a>
## Notas sobre Steam y Large Address Aware

- Una copia Large Address Aware que difiere solo en sus encabezados tiene la misma identidad de código que su original y usa el mismo diseño conocido.
- El ejecutable de Steam está protegido con DRM en disco, por lo que su archivo puede diferir de su imagen cargada: el plan puede informar `OL_W_CAPABILITY_PENDING` y decide la resolución en el proceso. Una sesión con el ejecutable genuino de Steam no se ha validado en runtime (consulte el [estado de validación en runtime](../status/runtime-validation-status.md)).
- Steam inicia OMSI por su cuenta; una sesión debe iniciarse mediante `OmsiLaunch.exe` para que exista el handoff. Si se inicia desde Steam, el plugin permanente permanece inactivo (sin handoff, sin hooks).

<a id="related-pages"></a>
## Páginas relacionadas

- [Limitaciones conocidas](known-limitations.md)
- [Estado de validación en runtime](../status/runtime-validation-status.md)
- [Instalación](../getting-started/installation.md)
